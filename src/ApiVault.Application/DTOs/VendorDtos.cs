using System.ComponentModel.DataAnnotations;

namespace ApiVault.Application.DTOs;

public sealed class SaveVendorRequest
{
    [Required, MaxLength(50)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(4000)] public string? Description { get; set; }
    [MaxLength(200)] public string? ContactPerson { get; set; }
    [EmailAddress, MaxLength(320)] public string? SupportEmail { get; set; }
    [MaxLength(100)] public string? SupportPhone { get; set; }
    [Url, MaxLength(1000)] public string? WebsiteUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class VendorResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactPerson { get; set; }
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool IsActive { get; set; }
    public int SystemCount { get; set; }
}
