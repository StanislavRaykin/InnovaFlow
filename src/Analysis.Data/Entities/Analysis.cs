namespace InnovaFlow.Analysis.Data;

// Classes, not records - EF tracks by identity and mutates in place.
public class Analysis
{
    public Guid Id { get; set; }

    /// <summary>Owned by the Projects service. No FK - different schema.</summary>
    public Guid IdeaId { get; set; }
    public Guid? IdeaVersionId { get; set; }

    public JobState State { get; set; } = JobState.Running;

    /// <summary>
    /// Flattened authorization result, resolved by Projects at job creation.
    /// AI Analysis never reads membership tables; it only checks containment.
    /// Kept fresh by consuming MembershipChanged events.
    /// </summary>
    public Guid[] AuthorizedUserIds { get; set; } = [];

    /// <summary>Who pressed the button. Used for notification targeting.</summary>
    public Guid RequestedBy { get; set; }

    /// <summary>Deduplicates double-submits. Unique when present.</summary>
    public string? IdempotencyKey { get; set; }

    public decimal? OverallScore { get; set; }
    public string? Summary { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public List<AIRequest> Nodes { get; set; } = [];
}
