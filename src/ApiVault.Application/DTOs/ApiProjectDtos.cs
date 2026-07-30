using System.ComponentModel.DataAnnotations;

namespace ApiVault.Application.DTOs;

public sealed class CreateApiProjectRequest
{
    [Required, MaxLength(100)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(4000)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ApiProjectResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ApiCount { get; set; }
}
