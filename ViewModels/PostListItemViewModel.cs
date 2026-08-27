namespace PROJECT2106.ViewModels;

public sealed class PostListItemViewModel
{
    public int Id { get; init; }

    public string? AuthorId { get; init; }

    public required string AuthorUsername { get; init; }

    public required string Content { get; init; }

    public DateTime CreatedAt { get; init; }

    public int CommentCount { get; init; }

    public IReadOnlyList<string> Tags { get; init; }
        = Array.Empty<string>();
}
