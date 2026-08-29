using PROJECT2106.Models;

namespace PROJECT2106.ViewModels;

public sealed class PlaceSearchViewModel
{
    public string? Query { get; init; }

    public SystemCategory? SystemCategory { get; init; }

    public int? CustomCategoryId { get; init; }

    public int? MinimumRating { get; init; }

    public string Sort { get; init; } = "newest";

    public IReadOnlyList<CustomCategory> CustomCategories { get; init; }
        = Array.Empty<CustomCategory>();

    public PagedResult<PlaceSearchResultViewModel> Results { get; init; }
        = new();
}
