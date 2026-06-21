namespace PROJECT2106.Models;

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string AuthorId { get; set; } = string.Empty;
    public AppUser? Author { get; set; }
    public int PostId { get; set; }
    public Post? Post { get; set; }
}