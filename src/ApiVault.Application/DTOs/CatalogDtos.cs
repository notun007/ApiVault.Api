using System.ComponentModel.DataAnnotations;
using ApiVault.Domain.Enums;

namespace ApiVault.Application.DTOs;

public enum ApiCatalogSortField
{
    Name,
    Ownership,
    Protocol,
    Business,
    CurrentRelease,
    Versions
}

public sealed class ApiSearchQuery
{
    public string? Search { get; set; }
    public ApiOwnershipType? OwnershipType { get; set; }
    public ApiProtocol? Protocol { get; set; }
    public ApiLifecycleStatus? LifecycleStatus { get; set; }
    public Guid? BusinessAreaId { get; set; }
    public Guid? DevelopmentTeamId { get; set; }
    public Guid? PublishingApplicationId { get; set; }
    public ApiCatalogSortField SortBy { get; set; } = ApiCatalogSortField.Name;
    public bool SortDescending { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 200)] public int PageSize { get; set; } = 25;
}

public sealed class CreateApiRequest
{
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [Required] public Guid PublishingApplicationId { get; set; }
    [MaxLength(4000)] public string? Description { get; set; }
    [Required] public ApiProtocol Protocol { get; set; }
    [Url, MaxLength(1000)] public string? ExternalReferenceUrl { get; set; }
}

public sealed class ApiSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PublishingApplicationId { get; set; }
    public LookupResponse PublishingApplication { get; set; } = new();
    public ApiOwnershipType OwnershipType { get; set; }
    public ApiProtocol Protocol { get; set; }
    public string BusinessArea { get; set; } = string.Empty;
    public string DevelopmentTeam { get; set; } = string.Empty;
    public string? CurrentVersion { get; set; }
    public ApiLifecycleStatus? CurrentLifecycleStatus { get; set; }
    public int VersionCount { get; set; }
}

public sealed class ApiDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PublishingApplicationId { get; set; }
    public LookupResponse PublishingApplication { get; set; } = new();
    public string? Description { get; set; }
    public ApiOwnershipType OwnershipType { get; set; }
    public ApiProtocol Protocol { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string? CreatorEmail { get; set; }
    public string? VendorName { get; set; }
    public string? ExternalReferenceUrl { get; set; }
    public LookupResponse BusinessArea { get; set; } = new();
    public LookupResponse DevelopmentTeam { get; set; } = new();
    public IReadOnlyList<ApiVersionResponse> Versions { get; set; } = Array.Empty<ApiVersionResponse>();
}

public sealed class CreateApiVersionRequest
{
    [Required, MaxLength(50)] public string Version { get; set; } = string.Empty;
    [MaxLength(200)] public string? ReleaseName { get; set; }
    public ApiLifecycleStatus LifecycleStatus { get; set; } = ApiLifecycleStatus.Draft;
    public DateTime? ReleaseDateUtc { get; set; }
    [MaxLength(8000)] public string? ChangeLog { get; set; }
    public AuthenticationType AuthenticationType { get; set; }
    [MaxLength(8000)] public string? AuthenticationInstructions { get; set; }
    public string? AuthenticationConfigJson { get; set; }
    [Range(1, 50_000_000)] public int MaxRequestBytes { get; set; } = 1_048_576;
    [Range(1, 100_000_000)] public int MaxResponseBytes { get; set; } = 5_242_880;
    [Range(1, 300)] public int TimeoutSeconds { get; set; } = 30;
    public bool IsCurrent { get; set; } = true;
}



public sealed class UpdateApiVersionRequest
{
    [MaxLength(200)] public string? ReleaseName { get; set; }
    public DateTime? ReleaseDateUtc { get; set; }
    [MaxLength(8000)] public string? ChangeLog { get; set; }
    public AuthenticationType AuthenticationType { get; set; }
    [MaxLength(8000)] public string? AuthenticationInstructions { get; set; }
    public string? AuthenticationConfigJson { get; set; }
    [Range(1, 50_000_000)] public int MaxRequestBytes { get; set; } = 1_048_576;
    [Range(1, 100_000_000)] public int MaxResponseBytes { get; set; } = 5_242_880;
    [Range(1, 300)] public int TimeoutSeconds { get; set; } = 30;
    public bool IsCurrent { get; set; }
}

public sealed class ChangeLifecycleRequest
{
    [Required] public ApiLifecycleStatus LifecycleStatus { get; set; }
}

public sealed class ApiVersionResponse
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? ReleaseName { get; set; }
    public ApiLifecycleStatus LifecycleStatus { get; set; }
    public DateTime? ReleaseDateUtc { get; set; }
    public DateTime? DeprecatedAtUtc { get; set; }
    public DateTime? RetiredAtUtc { get; set; }
    public string? ChangeLog { get; set; }
    public AuthenticationType AuthenticationType { get; set; }
    public string? AuthenticationInstructions { get; set; }
    public string? AuthenticationConfigJson { get; set; }
    public int MaxRequestBytes { get; set; }
    public int MaxResponseBytes { get; set; }
    public int TimeoutSeconds { get; set; }
    public bool IsCurrent { get; set; }
    public IReadOnlyList<EndpointResponse> Endpoints { get; set; } = Array.Empty<EndpointResponse>();
    public IReadOnlyList<EnvironmentResponse> Environments { get; set; } = Array.Empty<EnvironmentResponse>();
}

public sealed class CreateEndpointRequest
{
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(1000)] public string RelativePath { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string HttpMethod { get; set; } = "GET";
    [MaxLength(4000)] public string? Description { get; set; }
    public string? RequestHeadersJson { get; set; }
    public string? QueryParametersJson { get; set; }
    public string? PathParametersJson { get; set; }
    public string? RequestPayloadSample { get; set; }
    public string? ResponseHeadersSampleJson { get; set; }
    public string? ResponseBodySample { get; set; }
    public string? SuccessStatusCodesJson { get; set; }
    [MaxLength(1000)] public string? SoapAction { get; set; }
}

public sealed class EndpointResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RequestHeadersJson { get; set; }
    public string? QueryParametersJson { get; set; }
    public string? PathParametersJson { get; set; }
    public string? RequestPayloadSample { get; set; }
    public string? ResponseHeadersSampleJson { get; set; }
    public string? ResponseBodySample { get; set; }
    public string? SuccessStatusCodesJson { get; set; }
    public string? SoapAction { get; set; }
}

public sealed class CreateEnvironmentRequest
{
    [Required] public DeploymentEnvironment EnvironmentType { get; set; }
    [Required, Url, MaxLength(1000)] public string BaseUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    [MaxLength(4000)] public string? Notes { get; set; }
}

public sealed class EnvironmentResponse
{
    public Guid Id { get; set; }
    public DeploymentEnvironment EnvironmentType { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? Notes { get; set; }
    public IReadOnlyList<string> SecretNames { get; set; } = Array.Empty<string>();
}

public sealed class SetEnvironmentSecretRequest
{
    [Required, RegularExpression("^[A-Za-z0-9_.-]{1,100}$")] public string Name { get; set; } = string.Empty;
    [Required, MinLength(1), MaxLength(8000)] public string Value { get; set; } = string.Empty;
}

public sealed class LookupResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
