using System.ComponentModel.DataAnnotations;

namespace PROJECT2106.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter email")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter password")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}