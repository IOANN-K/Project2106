using System.ComponentModel.DataAnnotations;

namespace PROJECT2106.ViewModels;

public sealed class CustomCategoryCreateViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}
