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
}
