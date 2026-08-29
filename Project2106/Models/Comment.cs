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
    public bool IsEdited { get; set; } = false;
    public DateTime? EditedAt { get; set; }
    public int? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public List<Comment> Replies { get; set; } = new();
}