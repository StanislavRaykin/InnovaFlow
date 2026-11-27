using System.ComponentModel.DataAnnotations;

namespace Contracts.Identity;

public class ProfileResponse
{
    public required Guid UserId { get; set; }
    public required string Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
}

// Email is not editable here: it is the sign-in name and is baked into the issued token.
public class UpdateProfileRequest
{
    [MaxLength(100, ErrorMessage = "Името не може да бъде по-дълго от 100 символа.")]
    public string? FirstName { get; set; }

    [MaxLength(100, ErrorMessage = "Фамилията не може да бъде по-дълга от 100 символа.")]
    public string? LastName { get; set; }

    [MaxLength(2000, ErrorMessage = "Биографията не може да бъде по-дълга от 2000 символа.")]
    public string? Bio { get; set; }
}
