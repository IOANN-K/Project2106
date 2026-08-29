using PROJECT2106.Models;

namespace PROJECT2106.ViewModels;

public sealed class FeedMediaPreviewViewModel
{
    public required string RelativePath { get; init; }

    public required string OriginalFileName { get; init; }

    public required string MimeType { get; init; }

    public PostMediaType MediaType { get; init; }
}
