using System.ComponentModel.DataAnnotations;
using ApiVault.Domain.Enums;

namespace ApiVault.Application.DTOs;

public sealed class CreateProjectRequest
{
    [Required, MaxLength(50)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(4000)] public string? Description { get; set; }
    public ProjectCriticality Criticality { get; set; } = ProjectCriticality.Medium;
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    [Required] public Guid BusinessAreaId { get; set; }
    [Required] public Guid OwnerTeamId { get; set; }
    [Required] public ApiOwnershipType OwnershipType { get; set; }
    public Guid? VendorId { get; set; }
}

public sealed class LinkProjectApiVersionRequest
{
    [Required] public Guid ApiVersionId { get; set; }
    [MaxLength(2000)] public string? Purpose { get; set; }
    public bool IsRequired { get; set; } = true;
}

public class ProjectSummaryResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectCriticality Criticality { get; set; }
    public ProjectStatus Status { get; set; }
    public string BusinessArea { get; set; } = string.Empty;
    public string OwnerTeam { get; set; } = string.Empty;
    public Guid? BusinessAreaId { get; set; }
    public Guid? OwnerTeamId { get; set; }
    public ApiOwnershipType OwnershipType { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public int LinkedApiVersionCount { get; set; }
    public int PublishedApiCount { get; set; }
    public bool PublishesApis => PublishedApiCount > 0;
    public bool ConsumesApis => LinkedApiVersionCount > 0;
}

public sealed class ProjectDetailResponse : ProjectSummaryResponse
{
    public IReadOnlyList<ProjectApiLinkResponse> ApiVersions { get; set; } = Array.Empty<ProjectApiLinkResponse>();
    public IReadOnlyList<ApplicationPublishedApiResponse> PublishedApis { get; set; } = Array.Empty<ApplicationPublishedApiResponse>();
}

public sealed class ApplicationPublishedApiResponse
{
    public Guid ApiId { get; set; }
    public string ApiName { get; set; } = string.Empty;
    public int VersionCount { get; set; }
}

public sealed class ProjectApiLinkResponse
{
    public Guid LinkId { get; set; }
    public Guid ApiId { get; set; }
    public string ApiName { get; set; } = string.Empty;
    public Guid ApiVersionId { get; set; }
    public string Version { get; set; } = string.Empty;
    public ApiLifecycleStatus LifecycleStatus { get; set; }
    public string? Purpose { get; set; }
    public bool IsRequired { get; set; }
}
