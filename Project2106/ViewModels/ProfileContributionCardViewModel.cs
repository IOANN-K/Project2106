namespace PROJECT2106.ViewModels;

public sealed class ProfileContributionCardViewModel
{
    public int Id { get; init; }
    public int? PlaceId { get; init; }
    public string? PlaceName { get; init; }
    public required string Excerpt { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsEdited { get; init; }
    public string? PreviewImageUrl { get; init; }
}
