namespace InnovaFlow.Projects.Data;

/// <summary>
/// An immutable snapshot of the idea's analysable text at a point in time,
/// not a diff. Comparing analyses across versions is meaningless if editing
/// the idea silently rewrites what version 1 said.
/// </summary>
public class IdeaVersion
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public int VersionNumber { get; set; }

    /// <summary>Label for the variant chain: "Original", "Premium", "Mass Market".</summary>
    public string? VariantName { get; set; }

    /// <summary>
    /// Set when this version was produced by selecting a strategy. Gives the
    /// Original -> Premium -> Mass Market chain something to render from.
    /// </summary>
    public Guid? DerivedFromStrategyId { get; set; }
    public Strategy? DerivedFromStrategy { get; set; }

    // --- snapshot ---
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? ProblemStatement { get; set; }
    public string? TargetAudience { get; set; }

    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
