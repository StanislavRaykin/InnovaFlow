namespace Contracts.Identity;

public class AuthResponse
{
    public required Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string Token { get; set; }

    public required DateTime ExpiresAt { get; set; }

}
