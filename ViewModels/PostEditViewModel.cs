using System.ComponentModel.DataAnnotations;

namespace PROJECT2106.ViewModels;

public sealed class PostEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Post content is required")]
    [StringLength(5000, ErrorMessage = "Post content cannot exceed 5000 characters")]
    public string Content { get; set; } = string.Empty;
}
