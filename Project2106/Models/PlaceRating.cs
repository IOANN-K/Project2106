namespace PROJECT2106.Models;

public class PlaceRating
{
    public int Id { get; set; }

    public int PlaceId { get; set; }
    public Place? Place { get; set; }

    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }

    public int Value { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
