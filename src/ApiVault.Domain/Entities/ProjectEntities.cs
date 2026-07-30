using ApiVault.Domain.Common;
using ApiVault.Domain.Enums;

namespace ApiVault.Domain.Entities;

public sealed class Project : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectCriticality Criticality { get; set; } = ProjectCriticality.Medium;
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public Guid BusinessAreaId { get; set; }
    public BusinessArea BusinessArea { get; set; } = null!;
    public Guid OwnerTeamId { get; set; }
    public DevelopmentTeam OwnerTeam { get; set; } = null!;
    public ICollection<ProjectApiVersion> ApiLinks { get; set; } = new List<ProjectApiVersion>();
}

public sealed class ProjectApiVersion : AuditableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid ApiVersionId { get; set; }
    public ApiVersion ApiVersion { get; set; } = null!;
    public string? Purpose { get; set; }
    public bool IsRequired { get; set; } = true;
}
