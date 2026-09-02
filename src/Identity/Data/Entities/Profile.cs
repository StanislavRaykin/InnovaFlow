namespace InnovaFlow.Identity.Data;

/// <summary>
/// Everything about a user that is not authentication. Kept out of
/// ApplicationUser so the Identity tables stay close to the framework default
/// and profile fields can grow without touching the auth model.
/// </summary>
public class Profile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
