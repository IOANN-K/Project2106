using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace PROJECT2106.ViewModels;

public sealed class PostCreateViewModel
{
    [Required(ErrorMessage = "Contribution content is required")]
    [StringLength(5000, ErrorMessage = "Contribution content cannot exceed 5000 characters")]
    public string Content { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Tags cannot exceed 500 characters")]
    public string Tags { get; set; } = string.Empty;

    [Required(ErrorMessage = "Place is required")]
    public int? PlaceId { get; set; }

    public string PlaceName { get; set; } = string.Empty;

    public List<IFormFile> MediaFiles { get; set; } = new();
}
