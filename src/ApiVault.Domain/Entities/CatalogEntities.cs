using ApiVault.Domain.Common;
using ApiVault.Domain.Enums;

namespace ApiVault.Domain.Entities;

public sealed class BusinessArea : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<ApiAsset> Apis { get; set; } = new List<ApiAsset>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

public sealed class DevelopmentTeam : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? Description { get; set; }
    public ICollection<ApiAsset> Apis { get; set; } = new List<ApiAsset>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

public sealed class ApiProject : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ApiAsset> Apis { get; set; } = new List<ApiAsset>();
}

//New
public sealed class ApiAsset : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid ApiProjectId { get; set; }
    public ApiProject ApiProject { get; set; } = null!;
    public string? Description { get; set; }
    public ApiOwnershipType OwnershipType { get; set; }
    public ApiProtocol Protocol { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string? CreatorEmail { get; set; }
    public string? VendorName { get; set; }
    public string? ExternalReferenceUrl { get; set; }
    public Guid BusinessAreaId { get; set; }
    public BusinessArea BusinessArea { get; set; } = null!;
    public Guid DevelopmentTeamId { get; set; }
    public DevelopmentTeam DevelopmentTeam { get; set; } = null!;
    public ICollection<ApiVersion> Versions { get; set; } = new List<ApiVersion>();
}
//Commented on 26-07-2026
//public sealed class ApiAsset : AuditableEntity
//{
//    public string Name { get; set; } = string.Empty;
//    public string ApiProjectName { get; set; } = string.Empty;
//    public string? Description { get; set; }
//    public ApiOwnershipType OwnershipType { get; set; }
//    public ApiProtocol Protocol { get; set; }
//    public string CreatorName { get; set; } = string.Empty;
//    public string? CreatorEmail { get; set; }
//    public string? VendorName { get; set; }
//    public string? ExternalReferenceUrl { get; set; }
//    public Guid BusinessAreaId { get; set; }
//    public BusinessArea BusinessArea { get; set; } = null!;
//    public Guid DevelopmentTeamId { get; set; }
//    public DevelopmentTeam DevelopmentTeam { get; set; } = null!;
//    public ICollection<ApiVersion> Versions { get; set; } = new List<ApiVersion>();
//}

public sealed class ApiVersion : AuditableEntity
{
    public Guid ApiAssetId { get; set; }
    public ApiAsset ApiAsset { get; set; } = null!;
    public string Version { get; set; } = string.Empty;
    public string? ReleaseName { get; set; }
    public ApiLifecycleStatus LifecycleStatus { get; set; } = ApiLifecycleStatus.Draft;
    public DateTime? ReleaseDateUtc { get; set; }
    public DateTime? DeprecatedAtUtc { get; set; }
    public DateTime? RetiredAtUtc { get; set; }
    public string? ChangeLog { get; set; }
    public AuthenticationType AuthenticationType { get; set; }
    public string? AuthenticationInstructions { get; set; }
    public string? AuthenticationConfigJson { get; set; }
    public int MaxRequestBytes { get; set; } = 1_048_576;
    public int MaxResponseBytes { get; set; } = 5_242_880;
    public int TimeoutSeconds { get; set; } = 30;
    public bool IsCurrent { get; set; }
    public ICollection<ApiEndpoint> Endpoints { get; set; } = new List<ApiEndpoint>();
    public ICollection<ApiEnvironment> Environments { get; set; } = new List<ApiEnvironment>();
    public ICollection<ProjectApiVersion> ProjectLinks { get; set; } = new List<ProjectApiVersion>();
}

public sealed class ApiEndpoint : AuditableEntity
{
    public Guid ApiVersionId { get; set; }
    public ApiVersion ApiVersion { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "GET";
    public string? Description { get; set; }
    public string? RequestHeadersJson { get; set; }
    public string? QueryParametersJson { get; set; }
    public string? PathParametersJson { get; set; }
    public string? RequestPayloadSample { get; set; }
    public string? ResponseHeadersSampleJson { get; set; }
    public string? ResponseBodySample { get; set; }
    public string? SuccessStatusCodesJson { get; set; }
    public string? SoapAction { get; set; }
    public ICollection<TestExecution> TestExecutions { get; set; } = new List<TestExecution>();
}

public sealed class ApiEnvironment : AuditableEntity
{
    public Guid ApiVersionId { get; set; }
    public ApiVersion ApiVersion { get; set; } = null!;
    public DeploymentEnvironment EnvironmentType { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? Notes { get; set; }
    public ICollection<EnvironmentSecret> Secrets { get; set; } = new List<EnvironmentSecret>();
    public ICollection<TestExecution> TestExecutions { get; set; } = new List<TestExecution>();
}

public sealed class EnvironmentSecret : AuditableEntity
{
    public Guid ApiEnvironmentId { get; set; }
    public ApiEnvironment ApiEnvironment { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string EncryptedValue { get; set; } = string.Empty;
}
