using System.ComponentModel.DataAnnotations;
using PROJECT2106.Models;

namespace PROJECT2106.ViewModels;

public sealed class PlaceCreateViewModel
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(-90.0, 90.0)]
    public double Latitude { get; set; }

    [Range(-180.0, 180.0)]
    public double Longitude { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public SystemCategory? SystemCategory { get; set; }

    public int? CustomCategoryId { get; set; }

    public IReadOnlyList<CustomCategory> CustomCategories { get; set; }
        = Array.Empty<CustomCategory>();
}
