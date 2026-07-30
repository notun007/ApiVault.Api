using System.ComponentModel.DataAnnotations;
using ApiVault.Domain.Enums;

namespace ApiVault.Application.DTOs;

public sealed class LoginRequest
{
    [Required, MaxLength(100)] public string Username { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

public sealed class ExecuteApiTestRequest
{
    [Required] public Guid ApiEndpointId { get; set; }
    [Required] public Guid ApiEnvironmentId { get; set; }
    public Dictionary<string, string> PathParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> QueryParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Body { get; set; }
    [Range(1, 300)] public int? TimeoutSeconds { get; set; }
}

public sealed class TestExecutionResponse
{
    public Guid Id { get; set; }
    public Guid ApiEndpointId { get; set; }
    public Guid ApiEnvironmentId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public long DurationMilliseconds { get; set; }
    public bool IsSuccess { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string RequestUrl { get; set; } = string.Empty;
    public string? RequestHeadersJson { get; set; }
    public string? RequestBody { get; set; }
    public long RequestSizeBytes { get; set; }
    public string? ResponseHeadersJson { get; set; }
    public string? ResponseBody { get; set; }
    public long ResponseSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
}

public sealed class AuditLogResponse
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? ChangesJson { get; set; }
}


public sealed class CreateUserRequest
{
    [Required, MaxLength(100)] public string Username { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string DisplayName { get; set; } = string.Empty;
    [EmailAddress, MaxLength(320)] public string? Email { get; set; }
    [Required, MinLength(12), MaxLength(200)] public string Password { get; set; } = string.Empty;
    [Required] public UserRole Role { get; set; }
}

public sealed class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}
