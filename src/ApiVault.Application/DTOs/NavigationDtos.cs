using System.ComponentModel.DataAnnotations;

namespace ApiVault.Application.DTOs;

public sealed record NavigationEntry(Guid Id, string Name, string? Route, string Icon,
    Guid? ParentId, int DisplayOrder, bool IsActive, bool IsGroup);

public sealed record NavigationLayout(string Revision, IReadOnlyList<NavigationEntry> Items);

public sealed class SaveNavigationRequest
{
    [Required] public string Revision { get; set; } = string.Empty;
    [Required, MaxLength(200)] public List<NavigationEntry> Items { get; set; } = [];
}
