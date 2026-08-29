using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PROJECT2106.ViewModels;

public sealed class CustomCategoryEditViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public IFormFile? Icon { get; set; }

    public string? ExistingIconPath { get; set; }
}
