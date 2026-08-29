namespace PROJECT2106.Models;

public class ActivityLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}