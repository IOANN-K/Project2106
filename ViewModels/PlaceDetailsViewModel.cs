namespace PROJECT2106.ViewModels;

public sealed class PlaceDetailsViewModel
{
    public required Models.Place Place { get; init; }

    public IReadOnlyList<PlacePostListItemViewModel> Posts { get; init; }
        = Array.Empty<PlacePostListItemViewModel>();

    public int PostCount { get; init; }

    public string Sort { get; init; } = "newest";

    public double? AverageRating { get; init; }

    public int RatingCount { get; init; }
}
