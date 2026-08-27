namespace PROJECT2106.ViewModels;

public sealed class PlaceMarkerViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string Category { get; init; } = string.Empty;

    public double? Rating { get; init; }
}
