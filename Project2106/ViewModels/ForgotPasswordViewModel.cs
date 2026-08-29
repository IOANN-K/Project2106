using System.ComponentModel.DataAnnotations;

namespace PROJECT2106.ViewModels;

public sealed class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Enter email")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
