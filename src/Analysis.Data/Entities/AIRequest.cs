using System.ComponentModel.DataAnnotations.Schema;

namespace InnovaFlow.Analysis.Data;

public class AIRequest
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    /// <summary>Matches an INodeExecutor registration: "classify", "market", ...</summary>
    public string NodeKey { get; set; } = null!;

    public NodeState State { get; set; } = NodeState.Pending;

    /// <summary>
    /// Persisted rather than derived from code, so a job that is already running
    /// finishes with the graph shape it started with even if the code changes.
    /// </summary>
    public string[] DependsOn { get; set; } = [];

    /// <summary>SHA-256 over canonical JSON of resolved inputs. Cache key.</summary>
    public string? InputHash { get; set; }

    /// <summary>Validated node output. Null until Succeeded.</summary>
    public string? ResultJson { get; set; }

    /// <summary>Guards the renderer against results produced by an older schema.</summary>
    public int SchemaVersion { get; set; } = 1;

    public int Attempts { get; set; }

    /// <summary>
    /// Set when a worker claims the node. The janitor resets Running nodes whose
    /// lease has expired - this is what stops a worker crash hanging a job forever.
    /// </summary>
    public DateTimeOffset? LeaseExpiresAt { get; set; }

    /// <summary>Node key that caused this one to be skipped. Powers the UI tooltip.</summary>
    public string? SkippedBecause { get; set; }

    // Cost accounting - which tier ran, and what it cost
    public string? Model { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Postgres system column. Optimistic concurrency: two workers cannot both
    /// transition the same node out of Ready.
    /// </summary>
    public uint Version { get; set; }

    [NotMapped]
    public int? TotalTokens =>
        PromptTokens is null && CompletionTokens is null
            ? null
            : (PromptTokens ?? 0) + (CompletionTokens ?? 0);
}
