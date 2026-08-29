using System.ComponentModel.DataAnnotations;

namespace PROJECT2106.ViewModels;

public sealed class CommentInputViewModel
{
    [Required(ErrorMessage = "Comment content is required")]
    [StringLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters")]
    public string Content { get; set; } = string.Empty;
}
