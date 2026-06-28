namespace PROJECT2106.Models;

public class Follow
{
    public int Id { get; set; }
    public string FollowerId { get; set; } = string.Empty;
    public AppUser? Follower { get; set; }
    public string FollowingId { get; set; } = string.Empty;
    public AppUser? Following { get; set; }

    public DateTime CreatedAt { get; set; }
}