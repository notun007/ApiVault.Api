using System.ComponentModel.DataAnnotations;

namespace ApiVault.Application.DTOs;

public sealed class CreateLookupRequest
{
    [Required, MaxLength(50)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(4000)] public string? Description { get; set; }
    [EmailAddress, MaxLength(320)] public string? ContactEmail { get; set; }
}
