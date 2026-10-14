using ApiVault.Application.DTOs;
using ApiVault.Domain.Entities;

namespace ApiVault.Application.Models;

public sealed class ApiExecutionContext
{
    public required ApiEndpoint Endpoint { get; init; }
    public required ApiVersion Version { get; init; }
    public required ApiEnvironment Environment { get; init; }
    public required ExecuteApiTestRequest Request { get; init; }
    public IReadOnlyDictionary<string, string> Secrets { get; init; } = new Dictionary<string, string>();
    public bool ApplyVersionAuthentication { get; init; } = true;
    public ResolvedAuthentication? Authentication { get; init; }
    public IReadOnlyList<string> SensitiveResponseJsonPaths { get; init; } = Array.Empty<string>();
}

public sealed class ResolvedAccessToken
{
    public required string Value { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public string HeaderName { get; init; } = "Authorization";
    public DateTime ExpiresAtUtc { get; init; }
    public bool FromCache { get; init; }
}

public sealed class ResolvedAuthentication
{
    public required Guid ProfileId { get; init; }
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    public DateTime? ExpiresAtUtc { get; init; }
    public bool FromCache { get; init; }
    public string? TokenType { get; init; }
    public string Summary { get; init; } = string.Empty;
    public byte[]? ClientCertificatePfx { get; init; }
    public string? ClientCertificatePassword { get; init; }
}

public sealed class ApiExecutionResult
{
    public DateTime StartedAtUtc { get; init; }
    public long DurationMilliseconds { get; init; }
    public bool IsSuccess { get; init; }
    public int? ResponseStatusCode { get; init; }
    public string RequestUrl { get; init; } = string.Empty;
    public string? RequestHeadersJson { get; init; }
    public string? RequestBody { get; init; }
    public long RequestSizeBytes { get; init; }
    public string? ResponseHeadersJson { get; init; }
    public string? ResponseBody { get; init; }
    public long ResponseSizeBytes { get; init; }
    public string? ErrorMessage { get; init; }
    public bool AuthenticationApplied { get; init; }
    public Guid? AuthenticationProfileId { get; init; }
    public DateTime? AuthenticationTokenExpiresAtUtc { get; init; }
    public bool AuthenticationTokenFromCache { get; init; }
}
