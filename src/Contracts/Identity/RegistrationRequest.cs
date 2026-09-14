using System.ComponentModel.DataAnnotations;

namespace Contracts.Identity;

public class RegistrationRequest
{
    [Required]
    public required string FullName { get; set; }

    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required, MinLength(8)]
    public required string Password { get; set; }

    [Required, Compare(nameof(Password))]
    public required string ConfirmPassword { get; set; }
}
