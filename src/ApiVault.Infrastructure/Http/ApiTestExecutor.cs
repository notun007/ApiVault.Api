using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography.X509Certificates;
using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.Models;
using ApiVault.Domain.Enums;
using ApiVault.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ApiVault.Infrastructure.Http;

public sealed class ApiTestExecutor(
    IHttpClientFactory httpClientFactory,
    SsrfGuard ssrfGuard,
    SecretRedactor redactor,
    SecretTemplateResolver templates,
    IOptions<ExecutionSecurityOptions> options) : IApiTestExecutor
{
    public async Task<ApiExecutionResult> ExecuteAsync(ApiExecutionContext context, CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var requestUrl = context.Environment.BaseUrl;
        Dictionary<string, string> effectiveHeaders = new(StringComparer.OrdinalIgnoreCase);
        var secretValues = context.Authentication is null
            ? context.Secrets.Values.ToArray()
            : context.Secrets.Values.Concat(context.Authentication.Headers.Values).ToArray();
        var requestBody = context.Request.Body ?? context.Endpoint.RequestPayloadSample;

        try
        {
            requestBody = templates.Resolve(requestBody, context.Secrets);
            var uri = BuildTargetUri(context);
            requestUrl = uri.ToString();
            await ssrfGuard.ValidateAsync(uri, cancellationToken);

            var security = options.Value;
            var requestedMaxRequestBytes = context.Endpoint.MaxRequestBytes ?? context.Version.MaxRequestBytes;
            var requestedMaxResponseBytes = context.Endpoint.MaxResponseBytes ?? context.Version.MaxResponseBytes;
            var maxRequestBytes = Math.Min(requestedMaxRequestBytes, security.AbsoluteMaxRequestBytes);
            var maxResponseBytes = Math.Min(requestedMaxResponseBytes, security.AbsoluteMaxResponseBytes);
            var requestedTimeout = context.Request.TimeoutSeconds ??
                                   context.Endpoint.TimeoutSeconds ??
                                   context.Version.TimeoutSeconds;
            var timeoutSeconds = Math.Min(requestedTimeout, security.AbsoluteMaxTimeoutSeconds);

            var requestSize = requestBody is null ? 0 : Encoding.UTF8.GetByteCount(requestBody);
            if (requestSize > maxRequestBytes)
                throw new RequestValidationException($"Request body is {requestSize} bytes; the limit is {maxRequestBytes} bytes.");

            effectiveHeaders = BuildHeaders(context);
            ApplyAuthentication(context, effectiveHeaders);

            using var requestMessage = new HttpRequestMessage(new HttpMethod(context.Endpoint.HttpMethod), uri);
            if (requestBody is not null)
                requestMessage.Content = new StringContent(requestBody, Encoding.UTF8, GetContentType(effectiveHeaders, context));

            foreach (var header in effectiveHeaders)
            {
                if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value))
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            using var certificate = LoadClientCertificate(context.Authentication);
            using var certificateClient = certificate is null ? null : CreateCertificateClient(certificate);
            var client = certificateClient ?? httpClientFactory.CreateClient("ApiVaultExecutor");
            using var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

            if (response.Content.Headers.ContentLength > maxResponseBytes)
                throw new RequestValidationException(
                    $"Response Content-Length is {response.Content.Headers.ContentLength} bytes; the limit is {maxResponseBytes} bytes.");

            var responseBytes = await ReadLimitedAsync(response, maxResponseBytes, timeoutCts.Token);
            var responseBody = DecodeBody(responseBytes, response.Content.Headers.ContentType);
            responseBody = redactor.RedactJsonPaths(responseBody, context.SensitiveResponseJsonPaths);
            var responseHeaders = response.Headers.Concat(response.Content.Headers)
                .ToDictionary(x => x.Key, x => string.Join(", ", x.Value), StringComparer.OrdinalIgnoreCase);

            stopwatch.Stop();
            return new ApiExecutionResult
            {
                StartedAtUtc = startedAt,
                DurationMilliseconds = stopwatch.ElapsedMilliseconds,
                IsSuccess = response.IsSuccessStatusCode,
                ResponseStatusCode = (int)response.StatusCode,
                RequestUrl = requestUrl,
                RequestHeadersJson = redactor.SerializeHeaders(effectiveHeaders, secretValues),
                RequestBody = redactor.RedactText(requestBody, secretValues),
                RequestSizeBytes = requestSize,
                ResponseHeadersJson = redactor.SerializeHeaders(responseHeaders, secretValues),
                ResponseBody = redactor.RedactText(responseBody, secretValues),
                ResponseSizeBytes = responseBytes.LongLength,
                AuthenticationApplied = context.Authentication is not null,
                AuthenticationProfileId = context.Authentication?.ProfileId,
                AuthenticationTokenExpiresAtUtc = context.Authentication?.ExpiresAtUtc,
                AuthenticationTokenFromCache = context.Authentication?.FromCache ?? false
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return Failure("The API test timed out.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            return Failure(ex.Message);
        }

        ApiExecutionResult Failure(string error) => new()
        {
            StartedAtUtc = startedAt,
            DurationMilliseconds = stopwatch.ElapsedMilliseconds,
            IsSuccess = false,
            RequestUrl = requestUrl,
            RequestHeadersJson = redactor.SerializeHeaders(effectiveHeaders, secretValues),
            RequestBody = redactor.RedactText(requestBody, secretValues),
            RequestSizeBytes = requestBody is null ? 0 : Encoding.UTF8.GetByteCount(requestBody),
            ErrorMessage = redactor.RedactText(error, secretValues),
            AuthenticationApplied = context.Authentication is not null,
            AuthenticationProfileId = context.Authentication?.ProfileId,
            AuthenticationTokenExpiresAtUtc = context.Authentication?.ExpiresAtUtc,
            AuthenticationTokenFromCache = context.Authentication?.FromCache ?? false
        };
    }

    private Uri BuildTargetUri(ApiExecutionContext context)
    {
        var path = context.Endpoint.RelativePath;
        foreach (var parameter in context.Request.PathParameters)
            path = path.Replace($"{{{parameter.Key}}}",
                Uri.EscapeDataString(templates.Resolve(parameter.Value, context.Secrets) ?? string.Empty),
                StringComparison.OrdinalIgnoreCase);

        if (Regex.IsMatch(path, @"\{[^{}]+\}"))
            throw new RequestValidationException("One or more path parameters were not supplied.");

        var baseUri = new Uri(context.Environment.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        var target = new Uri(baseUri, path.TrimStart('/'));
        var builder = new UriBuilder(target);
        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(builder.Query)) queryParts.Add(builder.Query.TrimStart('?'));
        queryParts.AddRange(context.Request.QueryParameters.Select(x =>
            $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(templates.Resolve(x.Value, context.Secrets) ?? string.Empty)}"));
        builder.Query = string.Join("&", queryParts.Where(x => !string.IsNullOrWhiteSpace(x)));
        return builder.Uri;
    }

    private Dictionary<string, string> BuildHeaders(ApiExecutionContext context)
    {
        var headers = ParseHeaderObject(context.Endpoint.RequestHeadersJson);
        foreach (var header in context.Request.Headers)
            headers[header.Key] = header.Value;
        if (!string.IsNullOrWhiteSpace(context.Endpoint.SoapAction) && !headers.ContainsKey("SOAPAction"))
            headers["SOAPAction"] = context.Endpoint.SoapAction;

        foreach (var key in headers.Keys.ToList())
            headers[key] = templates.Resolve(headers[key], context.Secrets) ?? string.Empty;
        return headers;
    }

    private static Dictionary<string, string> ParseHeaderObject(string? json)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return result;
            foreach (var property in document.RootElement.EnumerateObject())
                result[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
        }
        catch (JsonException)
        {
            // JSON is validated when the endpoint is registered. Ignore legacy malformed data safely.
        }
        return result;
    }

    private static void ApplyAuthentication(ApiExecutionContext context, IDictionary<string, string> headers)
    {
        if (!context.ApplyVersionAuthentication) return;
        if (context.Authentication is not null)
        {
            foreach (var header in context.Authentication.Headers)
                headers[header.Key] = header.Value;
            return;
        }

        switch (context.Version.AuthenticationType)
        {
            case AuthenticationType.Bearer:
                AddBearer(headers, context.Secrets, "BEARER_TOKEN");
                break;
            case AuthenticationType.OAuth2:
                AddBearer(headers, context.Secrets, "OAUTH_ACCESS_TOKEN");
                break;
            case AuthenticationType.Basic:
                if (!headers.ContainsKey("Authorization") &&
                    context.Secrets.TryGetValue("BASIC_USERNAME", out var username) &&
                    context.Secrets.TryGetValue("BASIC_PASSWORD", out var password))
                {
                    headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                }
                break;
            case AuthenticationType.ApiKey:
                if (context.Secrets.TryGetValue("API_KEY", out var apiKey))
                {
                    var headerName = GetApiKeyHeaderName(context.Version.AuthenticationConfigJson);
                    if (!headers.ContainsKey(headerName)) headers[headerName] = apiKey;
                }
                break;
        }
    }

    private static void AddBearer(IDictionary<string, string> headers, IReadOnlyDictionary<string, string> secrets, string secretName)
    {
        if (!headers.ContainsKey("Authorization") && secrets.TryGetValue(secretName, out var token))
            headers["Authorization"] = $"Bearer {token}";
    }

    private static string GetApiKeyHeaderName(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "X-API-Key";
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("headerName", out var value)
                ? value.GetString() ?? "X-API-Key"
                : "X-API-Key";
        }
        catch (JsonException)
        {
            return "X-API-Key";
        }
    }

    private static string GetContentType(IReadOnlyDictionary<string, string> headers, ApiExecutionContext context)
    {
        if (headers.TryGetValue("Content-Type", out var contentType))
            return contentType.Split(';', 2)[0].Trim();
        return context.Version.ApiAsset.Protocol == ApiProtocol.Soap ? "text/xml" : "application/json";
    }

    private static X509Certificate2? LoadClientCertificate(ResolvedAuthentication? authentication)
    {
        if (authentication?.ClientCertificatePfx is not { Length: > 0 } pfx) return null;
        try
        {
            return X509CertificateLoader.LoadPkcs12(
                pfx,
                authentication.ClientCertificatePassword,
                X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (CryptographicException)
        {
            throw new RequestValidationException("The mutual TLS certificate or its password is invalid.");
        }
    }

    private static HttpClient CreateCertificateClient(X509Certificate2 certificate)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            UseCookies = false,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            SslOptions = { ClientCertificates = new X509CertificateCollection { certificate } }
        };
        return new HttpClient(handler, disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
    }

    private static async Task<byte[]> ReadLimitedAsync(HttpResponseMessage response, int maxBytes, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var memory = new MemoryStream(Math.Min(maxBytes, 64 * 1024));
        var buffer = new byte[8192];
        var total = 0;
        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0) break;
            total += read;
            if (total > maxBytes)
                throw new RequestValidationException($"Response exceeded the {maxBytes}-byte limit.");
            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        return memory.ToArray();
    }

    private static string DecodeBody(byte[] bytes, MediaTypeHeaderValue? contentType)
    {
        var mediaType = contentType?.MediaType ?? string.Empty;
        var isText = mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
                     mediaType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
                     mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
                     mediaType.Contains("javascript", StringComparison.OrdinalIgnoreCase) ||
                     mediaType.Contains("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
        if (!isText)
            return $"[base64; content-type={mediaType}] {Convert.ToBase64String(bytes)}";

        try
        {
            var charset = contentType?.CharSet?.Trim('"');
            var encoding = string.IsNullOrWhiteSpace(charset) ? Encoding.UTF8 : Encoding.GetEncoding(charset);
            return encoding.GetString(bytes);
        }
        catch
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
