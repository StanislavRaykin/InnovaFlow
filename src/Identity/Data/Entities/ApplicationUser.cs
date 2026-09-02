using Microsoft.AspNetCore.Identity;

namespace InnovaFlow.Identity.Data;

/// <summary>
/// Guid keys rather than the default string keys, so user ids are the same
/// shape as every other id in the system. Projects and Analysis store bare
/// Guid user references with no foreign key - they live in another schema.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; set; }

    public Profile? Profile { get; set; }
}
