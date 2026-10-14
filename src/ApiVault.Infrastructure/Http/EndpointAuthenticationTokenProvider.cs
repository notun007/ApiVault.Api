using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.Models;
using ApiVault.Domain.Entities;
using ApiVault.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ApiVault.Infrastructure.Http;

public sealed class EndpointAuthenticationTokenProvider(
    IHttpClientFactory httpClientFactory,
    SsrfGuard ssrfGuard,
    SecretTemplateResolver templates,
    IMemoryCache cache,
    IOptions<ExecutionSecurityOptions> options) : IApiAuthenticationTokenProvider
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.Ordinal);

    public async Task<ResolvedAccessToken> GetTokenAsync(
        ApiAuthenticationProfile profile,
        ApiEnvironment environment,
        IReadOnlyDictionary<string, string> secrets,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(profile.Id, environment.Id);
        if (forceRefresh) cache.Remove(cacheKey);
        if (TryGetCached(cacheKey, out var cached)) return cached;

        var gate = Locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && TryGetCached(cacheKey, out cached)) return cached;

            var acquired = await AcquireAsync(profile, environment, secrets, cancellationToken);
            var cacheUntil = acquired.ExpiresAtUtc.AddSeconds(-profile.RefreshSkewSeconds);
            if (cacheUntil > DateTime.UtcNow)
                cache.Set(cacheKey, acquired, cacheUntil);
            return acquired;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Invalidate(Guid profileId, Guid environmentId) =>
        cache.Remove(BuildCacheKey(profileId, environmentId));

    private bool TryGetCached(string cacheKey, out ResolvedAccessToken token)
    {
        if (cache.TryGetValue<ResolvedAccessToken>(cacheKey, out var cached) && cached is not null)
        {
            token = new ResolvedAccessToken
            {
                Value = cached.Value,
                TokenType = cached.TokenType,
                HeaderName = cached.HeaderName,
                ExpiresAtUtc = cached.ExpiresAtUtc,
                FromCache = true
            };
            return true;
        }

        token = null!;
        return false;
    }

    private async Task<ResolvedAccessToken> AcquireAsync(
        ApiAuthenticationProfile profile,
        ApiEnvironment environment,
        IReadOnlyDictionary<string, string> secrets,
        CancellationToken cancellationToken)
    {
        var endpoint = profile.TokenEndpoint
            ?? throw new RequestValidationException("The authentication profile does not have a token endpoint.");
        var uri = new Uri(
            new Uri(environment.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute),
            endpoint.RelativePath.TrimStart('/'));
        if (uri.AbsolutePath.Contains('{', StringComparison.Ordinal))
            throw new RequestValidationException("The authentication endpoint cannot contain unresolved path parameters.");

        await ssrfGuard.ValidateAsync(uri, cancellationToken);

        using var request = new HttpRequestMessage(new HttpMethod(endpoint.HttpMethod), uri);
        var body = templates.Resolve(endpoint.RequestPayloadSample, secrets);
        var headers = ParseHeaders(endpoint.RequestHeadersJson, secrets);
        if (body is not null)
            request.Content = new StringContent(body, Encoding.UTF8, GetContentType(headers));

        foreach (var header in headers)
        {
            if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var security = options.Value;
        var timeoutSeconds = Math.Min(profile.ApiVersion.TimeoutSeconds, security.AbsoluteMaxTimeoutSeconds);
        var maxRequestBytes = Math.Min(profile.ApiVersion.MaxRequestBytes, security.AbsoluteMaxRequestBytes);
        var maxResponseBytes = Math.Min(profile.ApiVersion.MaxResponseBytes, security.AbsoluteMaxResponseBytes);
        if (body is not null && Encoding.UTF8.GetByteCount(body) > maxRequestBytes)
            throw new RequestValidationException("Authentication request exceeded the configured request-size limit.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var client = httpClientFactory.CreateClient("ApiVaultExecutor");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if (!response.IsSuccessStatusCode)
            throw new RequestValidationException(
                $"Authentication endpoint returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");
        if (response.Content.Headers.ContentLength > maxResponseBytes)
            throw new RequestValidationException("Authentication response exceeded the configured response-size limit.");

        var bytes = await ReadLimitedAsync(response, maxResponseBytes, timeout.Token);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(bytes);
        }
        catch (JsonException)
        {
            throw new RequestValidationException("Authentication endpoint returned invalid JSON.");
        }

        using (document)
        {
            var tokenValue = ReadString(document.RootElement, profile.TokenJsonPath);
            if (string.IsNullOrWhiteSpace(tokenValue))
                throw new RequestValidationException(
                    $"Authentication response did not contain a token at '{profile.TokenJsonPath}'.");

            var tokenType = ReadString(document.RootElement, profile.TokenTypeJsonPath) ?? "Bearer";
            var expiresAt = ResolveExpiration(document.RootElement, profile, tokenValue);
            if (expiresAt <= DateTime.UtcNow)
                throw new RequestValidationException("Authentication endpoint returned an already-expired token.");

            return new ResolvedAccessToken
            {
                Value = tokenValue,
                TokenType = tokenType,
                HeaderName = profile.HeaderName,
                ExpiresAtUtc = expiresAt,
                FromCache = false
            };
        }
    }

    private Dictionary<string, string> ParseHeaders(string? json, IReadOnlyDictionary<string, string> secrets)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return result;
            foreach (var property in document.RootElement.EnumerateObject())
            {
                var value = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : property.Value.ToString();
                result[property.Name] = templates.Resolve(value, secrets) ?? string.Empty;
            }
            return result;
        }
        catch (JsonException)
        {
            throw new RequestValidationException("Authentication endpoint headers contain invalid JSON.");
        }
    }

    private static string GetContentType(IReadOnlyDictionary<string, string> headers) =>
        headers.TryGetValue("Content-Type", out var value)
            ? value.Split(';', 2)[0].Trim()
            : "application/json";

    private static DateTime ResolveExpiration(
        JsonElement root,
        ApiAuthenticationProfile profile,
        string token)
    {
        var expiresAtText = ReadString(root, profile.ExpiresAtJsonPath);
        if (DateTimeOffset.TryParse(expiresAtText, out var expiresAt)) return expiresAt.UtcDateTime;

        var expiresInText = ReadString(root, profile.ExpiresInJsonPath);
        if (double.TryParse(expiresInText, out var expiresInSeconds) && expiresInSeconds > 0)
            return DateTime.UtcNow.AddSeconds(expiresInSeconds);

        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            if (jwt.ValidTo > DateTime.MinValue) return jwt.ValidTo;
        }
        catch (ArgumentException)
        {
            // A non-JWT bearer token is valid; use the conservative fallback lifetime below.
        }

        return DateTime.UtcNow.AddMinutes(5);
    }

    private static string? ReadString(JsonElement root, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var current = root;
        foreach (var segment in path.Trim().TrimStart('$').TrimStart('.')
                     .Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.ValueKind != JsonValueKind.Object) return null;
            var found = false;
            foreach (var property in current.EnumerateObject())
            {
                if (!string.Equals(property.Name, segment, StringComparison.OrdinalIgnoreCase)) continue;
                current = property.Value;
                found = true;
                break;
            }
            if (!found) return null;
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
    }

    private static async Task<byte[]> ReadLimitedAsync(
        HttpResponseMessage response,
        int maxBytes,
        CancellationToken cancellationToken)
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
                throw new RequestValidationException("Authentication response exceeded the configured response-size limit.");
            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        return memory.ToArray();
    }

    private static string BuildCacheKey(Guid profileId, Guid environmentId) =>
        $"api-auth-token:{profileId:N}:{environmentId:N}";
}
