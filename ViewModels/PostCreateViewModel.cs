using System.ComponentModel.DataAnnotations;

namespace PROJECT2106.ViewModels;

public sealed class PostCreateViewModel
{
    [Required(ErrorMessage = "Post content is required")]
    [StringLength(5000, ErrorMessage = "Post content cannot exceed 5000 characters")]
    public string Content { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters")]
    public string Tags { get; set; } = string.Empty;
}
