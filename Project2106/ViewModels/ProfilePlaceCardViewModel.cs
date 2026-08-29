using PROJECT2106.Models;

namespace PROJECT2106.ViewModels;

public sealed class ProfilePlaceCardViewModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public SystemCategory? SystemCategory { get; init; }
    public string? CustomCategoryName { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? PreviewImageUrl { get; init; }
}
