namespace PROJECT2106.ViewModels;

public sealed class NearbyPlaceViewModel
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public double DistanceMeters { get; init; }
}
