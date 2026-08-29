using System.ComponentModel.DataAnnotations;

namespace PROJECT2106.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter an explorer name")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter email")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter password")]
    [MinLength(6, ErrorMessage = "Minimum 6 characters")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
