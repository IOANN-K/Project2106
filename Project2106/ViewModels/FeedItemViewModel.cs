namespace PROJECT2106.ViewModels;

public sealed class FeedItemViewModel
{
    public int PostId { get; init; }

    public required string AuthorUsername { get; init; }

    public string? AuthorAvatarUrl { get; init; }

    public int PlaceId { get; init; }

    public required string PlaceName { get; init; }

    public required string Category { get; init; }

    public required string Excerpt { get; init; }

    public DateTime CreatedAt { get; init; }

    public bool IsEdited { get; init; }

    public int LikeCount { get; init; }

    public int CommentCount { get; init; }

    public FeedMediaPreviewViewModel? Media { get; init; }
}
