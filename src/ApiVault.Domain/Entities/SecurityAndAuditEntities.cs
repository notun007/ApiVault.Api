using ApiVault.Domain.Common;
using ApiVault.Domain.Enums;

namespace ApiVault.Domain.Entities;

public sealed class AppUser : AuditableEntity
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }
}

public sealed class TestExecution : AuditableEntity
{
    public Guid ApiEndpointId { get; set; }
    public ApiEndpoint ApiEndpoint { get; set; } = null!;
    public Guid ApiEnvironmentId { get; set; }
    public ApiEnvironment ApiEnvironment { get; set; } = null!;
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
}

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; set; }
    public string UserName { get; set; } = "system";
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? ChangesJson { get; set; }
}
