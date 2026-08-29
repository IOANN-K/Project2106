using PROJECT2106.Models;

namespace PROJECT2106.ViewModels;

public sealed class AdminDashboardViewModel
{
    public int UserCount { get; init; }

    public int PostCount { get; init; }

    public int CommentCount { get; init; }

    public required PagedResult<AppUser> Users { get; init; }
}
