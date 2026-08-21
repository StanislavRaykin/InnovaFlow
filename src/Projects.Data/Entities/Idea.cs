namespace InnovaFlow.Projects.Data;

public class Idea
{
    public Guid Id { get; set; }

    /// <summary>
    /// Single unambiguous owner. Kept alongside the membership row so deletion
    /// and ownership transfer never hit "zero owners" or "two owners".
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>Optional team grant. Independent of direct membership.</summary>
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }

    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? ProblemStatement { get; set; }
    public string? TargetAudience { get; set; }
    public string? Industry { get; set; }

    public IdeaCategory Category { get; set; }
    public IdeaStatus Status { get; set; } = IdeaStatus.Draft;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }

    public List<IdeaMember> Members { get; set; } = [];
    public List<IdeaFile> Files { get; set; } = [];
}
