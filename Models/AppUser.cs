using Microsoft.AspNetCore.Identity;

namespace PROJECT2106.Models;

public class AppUser : IdentityUser
{
    public string Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<Post> Posts { get; set; } = new();
    public List<Follow> Followers { get; set; } = new();
    public List<Follow> Following { get; set; } = new();
}