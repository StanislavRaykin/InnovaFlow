namespace InnovaFlow.Analysis.Data;

// ---------------------------------------------------------------------------
// Provider call log
// ---------------------------------------------------------------------------

/// <summary>
/// One row per call to the AI provider. Gives cost per analysis, latency per
/// node type, and a failure trail when a node retries. With no per-node table,
/// this is also the only record of which nodes actually ran.
/// </summary>
public class AIRequest
{
    public Guid Id { get; set; }

    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public NodeType AgentType { get; set; }
    public string Status { get; set; } = null!;

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public string? Model { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public string? ErrorMessage { get; set; }
}
