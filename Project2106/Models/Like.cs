namespace PROJECT2106.Models;

public class Like
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }
    public int PostId { get; set; }
    public Post? Post { get; set; }
    public bool IsLike { get; set; } 
}