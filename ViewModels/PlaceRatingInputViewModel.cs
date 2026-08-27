using System.ComponentModel.DataAnnotations;

namespace PROJECT2106.ViewModels;

public sealed class PlaceRatingInputViewModel
{
    [Required]
    public int PlaceId { get; set; }

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Value { get; set; }
}
