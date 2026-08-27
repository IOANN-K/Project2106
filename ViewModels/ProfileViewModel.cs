using PROJECT2106.Models;

namespace PROJECT2106.ViewModels;

public sealed class ProfileViewModel
{
    public required AppUser User { get; init; }

    public IReadOnlyList<Post> Contributions { get; init; }
        = Array.Empty<Post>();

    public IReadOnlyList<Place> CreatedPlaces { get; init; }
        = Array.Empty<Place>();

    public int ContributionsCount { get; init; }

    public int CreatedPlacesCount { get; init; }

    public int FollowersCount { get; init; }

    public int FollowingCount { get; init; }

    public int LikesReceived { get; init; }

    public int CommentsCreated { get; init; }

    public int Reputation { get; init; }

    public string ExplorerLevel { get; init; } = "New Explorer";

    public bool IsFollowing { get; init; }

    public bool IsOwnProfile { get; init; }
}
