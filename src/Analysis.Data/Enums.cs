namespace InnovaFlow.Analysis.Data;

/// <summary>
/// Lifecycle of a single node. Stored as text so rows are readable in pgAdmin
/// during a demo. Skipped and NotApplicable are deliberately distinct:
///   Skipped        - a dependency failed, so this node never ran
///   NotApplicable  - a predicate excluded it (e.g. brand analysis on a non-brand idea)
/// </summary>
public enum NodeState
{
    Pending,
    Ready,
    Running,
    Succeeded,
    Failed,
    Skipped,
    NotApplicable,
    Cancelled
}

public enum JobState
{
    Running,
    Completed,
    PartiallyCompleted,
    Failed,
    Cancelled
}
