using PROJECT2106.Models;

namespace PROJECT2106.ViewModels;

public sealed class PlacePostListItemViewModel
{
    public required Post Post { get; init; }

    public int LikeCount { get; init; }

    public int DislikeCount { get; set; }

    public int CommentCount { get; init; }
}
