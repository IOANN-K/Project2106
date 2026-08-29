namespace PROJECT2106.Models;

public class Post
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsEdited { get; set; }
    public DateTime? EditedAt { get; set; }
    public string? AuthorId { get; set; }
    public AppUser? Author { get; set; }
    public List<Comment> Comments { get; set; } = new();
    public List<Tag> Tags { get; set; } = new();
    public int? PlaceId { get; set; }
    public Place? Place { get; set; }

    public List<PostMedia> Media { get; set; } = new();
}