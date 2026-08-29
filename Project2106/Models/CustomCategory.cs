using System.ComponentModel.DataAnnotations;

namespace PROJECT2106.Models;

public class CustomCategory
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    public AppUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? IconPath { get; set; }

    public List<Place> Places { get; set; } = new();
}
