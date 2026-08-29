using System.ComponentModel.DataAnnotations;

namespace PROJECT2106.Models;

public class PostMedia
{
    public int Id { get; set; }

    public int PostId { get; set; }

    public Post? Post { get; set; }

    public PostMediaType MediaType { get; set; }

    [Required]
    [StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string RelativePath { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
