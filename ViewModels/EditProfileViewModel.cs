using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PROJECT2106.ViewModels;

public sealed class EditProfileViewModel
{
    [StringLength(1000, ErrorMessage = "Biography cannot exceed 1000 characters.")]
    [Display(Name = "Biography")]
    public string? Bio { get; set; }

    [Display(Name = "Profile photo")]
    public IFormFile? Avatar { get; set; }

    public string? CurrentAvatarUrl { get; set; }

    public string Username { get; set; } = string.Empty;
}
