using PROJECT2106.Models;

namespace PROJECT2106.ViewModels;

public sealed class PlaceSearchResultViewModel
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public SystemCategory? SystemCategory { get; init; }

    public string? CustomCategoryName { get; init; }

    public DateTime CreatedAt { get; init; }

    public int PostCount { get; init; }

    public int RatingCount { get; init; }

    public double? AverageRating { get; init; }
}
