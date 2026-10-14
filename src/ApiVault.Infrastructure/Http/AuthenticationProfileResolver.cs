using System.Text;
using System.Text.Json;
using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.Models;
using ApiVault.Domain.Entities;
using ApiVault.Domain.Enums;

namespace ApiVault.Infrastructure.Http;

public sealed class AuthenticationProfileResolver(
    IApiAuthenticationTokenProvider tokenProvider,
    SecretTemplateResolver templates) : IApiAuthenticationProfileResolver
{
    public async Task<ResolvedAuthentication> ResolveAsync(
        ApiAuthenticationProfile profile,
        ApiEnvironment environment,
        IReadOnlyDictionary<string, string> secrets,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        if (!profile.IsEnabled)
            throw new RequestValidationException("Authentication profile is disabled.");

        return profile.Type switch
        {
            AuthenticationProfileType.TokenEndpoint or AuthenticationProfileType.OAuth2ClientCredentials =>
                await ResolveTokenEndpointAsync(profile, environment, secrets, forceRefresh, cancellationToken),
            AuthenticationProfileType.Basic => ResolveBasic(profile, secrets),
            AuthenticationProfileType.ApiKey => ResolveApiKey(profile, secrets),
            AuthenticationProfileType.StaticBearer => ResolveStaticBearer(profile, secrets),
            AuthenticationProfileType.CustomHeaders => ResolveCustomHeaders(profile, secrets),
            AuthenticationProfileType.MutualTls => ResolveMutualTls(profile, secrets),
            _ => throw new RequestValidationException($"Authentication profile type '{profile.Type}' is not supported.")
        };
    }

    public void Invalidate(Guid profileId, Guid environmentId) =>
        tokenProvider.Invalidate(profileId, environmentId);

    private async Task<ResolvedAuthentication> ResolveTokenEndpointAsync(
        ApiAuthenticationProfile profile,
        ApiEnvironment environment,
        IReadOnlyDictionary<string, string> secrets,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        if (profile.TokenEndpointId is null || profile.TokenEndpoint is null)
            throw new RequestValidationException("A token endpoint is required for this authentication profile type.");

        var token = await tokenProvider.GetTokenAsync(profile, environment, secrets, forceRefresh, cancellationToken);
        return new ResolvedAuthentication
        {
            ProfileId = profile.Id,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [token.HeaderName] = $"{token.TokenType} {token.Value}".Trim()
            },
            ExpiresAtUtc = token.ExpiresAtUtc,
            FromCache = token.FromCache,
            TokenType = token.TokenType,
            Summary = $"{token.TokenType} token acquired and ready for {token.HeaderName}."
        };
    }

    private static ResolvedAuthentication ResolveBasic(
        ApiAuthenticationProfile profile,
        IReadOnlyDictionary<string, string> secrets)
    {
        using var config = ParseConfiguration(profile.ConfigurationJson);
        var usernameSecret = ReadString(config.RootElement, "usernameSecretName") ?? "BASIC_USERNAME";
        var passwordSecret = ReadString(config.RootElement, "passwordSecretName") ?? "BASIC_PASSWORD";
        var username = RequireSecret(secrets, usernameSecret);
        var password = RequireSecret(secrets, passwordSecret);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        return Header(profile, "Authorization", $"Basic {encoded}", "Basic credentials resolved from environment secrets.");
    }

    private static ResolvedAuthentication ResolveApiKey(
        ApiAuthenticationProfile profile,
        IReadOnlyDictionary<string, string> secrets)
    {
        using var config = ParseConfiguration(profile.ConfigurationJson);
        var location = ReadString(config.RootElement, "location") ?? "Header";
        if (!string.Equals(location, "Header", StringComparison.OrdinalIgnoreCase))
            throw new RequestValidationException("Only header-based API keys are currently supported.");
        var secretName = ReadString(config.RootElement, "secretName") ?? "API_KEY";
        var headerName = ReadString(config.RootElement, "headerName") ?? profile.HeaderName;
        return Header(profile, headerName, RequireSecret(secrets, secretName), $"API key resolved for {headerName}.");
    }

    private static ResolvedAuthentication ResolveStaticBearer(
        ApiAuthenticationProfile profile,
        IReadOnlyDictionary<string, string> secrets)
    {
        using var config = ParseConfiguration(profile.ConfigurationJson);
        var secretName = ReadString(config.RootElement, "secretName") ?? "BEARER_TOKEN";
        var scheme = ReadString(config.RootElement, "scheme") ?? "Bearer";
        var value = RequireSecret(secrets, secretName);
        return Header(profile, profile.HeaderName, $"{scheme} {value}".Trim(), $"{scheme} credential resolved from an environment secret.", scheme);
    }

    private ResolvedAuthentication ResolveCustomHeaders(
        ApiAuthenticationProfile profile,
        IReadOnlyDictionary<string, string> secrets)
    {
        using var config = ParseConfiguration(profile.ConfigurationJson);
        if (!config.RootElement.TryGetProperty("headers", out var headersElement) ||
            headersElement.ValueKind != JsonValueKind.Object)
            throw new RequestValidationException("CustomHeaders configuration must contain a 'headers' object.");

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in headersElement.EnumerateObject())
        {
            var template = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()
                : property.Value.ToString();
            headers[property.Name] = templates.Resolve(template, secrets) ?? string.Empty;
        }
        if (headers.Count == 0)
            throw new RequestValidationException("At least one custom authentication header is required.");

        return new ResolvedAuthentication
        {
            ProfileId = profile.Id,
            Headers = headers,
            Summary = $"{headers.Count} custom authentication header(s) resolved."
        };
    }

    private static ResolvedAuthentication ResolveMutualTls(
        ApiAuthenticationProfile profile,
        IReadOnlyDictionary<string, string> secrets)
    {
        using var config = ParseConfiguration(profile.ConfigurationJson);
        var certificateSecretName = ReadString(config.RootElement, "certificateSecretName") ?? "MTLS_CERTIFICATE_PFX";
        var passwordSecretName = ReadString(config.RootElement, "certificatePasswordSecretName");
        var encodedCertificate = RequireSecret(secrets, certificateSecretName);
        byte[] certificateBytes;
        try
        {
            certificateBytes = Convert.FromBase64String(encodedCertificate);
        }
        catch (FormatException)
        {
            throw new RequestValidationException(
                $"Environment secret '{certificateSecretName}' must contain a Base64-encoded PKCS#12/PFX certificate.");
        }

        var password = string.IsNullOrWhiteSpace(passwordSecretName)
            ? null
            : RequireSecret(secrets, passwordSecretName);
        return new ResolvedAuthentication
        {
            ProfileId = profile.Id,
            ClientCertificatePfx = certificateBytes,
            ClientCertificatePassword = password,
            Summary = "Mutual TLS client certificate resolved from environment secrets."
        };
    }

    private static ResolvedAuthentication Header(
        ApiAuthenticationProfile profile,
        string name,
        string value,
        string summary,
        string? tokenType = null) => new()
        {
            ProfileId = profile.Id,
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [name] = value },
            TokenType = tokenType,
            Summary = summary
        };

    private static JsonDocument ParseConfiguration(string? json)
    {
        try
        {
            return JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        }
        catch (JsonException)
        {
            throw new RequestValidationException("Authentication profile configuration is invalid JSON.");
        }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string RequireSecret(IReadOnlyDictionary<string, string> secrets, string name) =>
        secrets.TryGetValue(name, out var value) && !string.IsNullOrEmpty(value)
            ? value
            : throw new RequestValidationException($"Environment secret '{name}' is required by the authentication profile.");
}
