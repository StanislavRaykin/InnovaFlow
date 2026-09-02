namespace InnovaFlow.Projects.Data;

public class Idea
{
    public Guid Id { get; set; }

    /// <summary>Belongs to the Identity service. No FK - different schema.</summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Optional. An idea can live inside a team or stand alone. Teams and
    /// per-idea sharing are separate mechanisms, not layers of one another.
    /// </summary>
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ProblemStatement { get; set; }
    public string? TargetAudience { get; set; }

    public IdeaCategory Category { get; set; }
    public string? Industry { get; set; }
    public IdeaStatus Status { get; set; } = IdeaStatus.Draft;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Soft delete. Analyses in the other schema reference this id.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    public List<IdeaMember> Members { get; set; } = [];
    public List<IdeaFile> Files { get; set; } = [];
    public List<IdeaVersion> Versions { get; set; } = [];
    public List<Strategy> Strategies { get; set; } = [];
    public List<AIConversation> Conversations { get; set; } = [];
}
