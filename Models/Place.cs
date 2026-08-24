using System.ComponentModel.DataAnnotations;

namespace PROJECT2106.Models;

public class Place
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(-90.0, 90.0)]
    public double Latitude { get; set; }

    [Range(-180.0, 180.0)]
    public double Longitude { get; set; }

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    public AppUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public SystemCategory? SystemCategory { get; set; }

    public int? CustomCategoryId { get; set; }

    public CustomCategory? CustomCategory { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public List<Post> Posts { get; set; } = new();
}
