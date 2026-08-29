namespace PROJECT2106.Models;

public class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }

    public string ActorUserId { get; set; } = string.Empty;
    public AppUser? ActorUser { get; set; }

    public NotificationType Type { get; set; }

    public int? PostId { get; set; }
    public Post? Post { get; set; }

    public int? PlaceId { get; set; }
    public Place? Place { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
