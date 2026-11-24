using System.Text.Json;
namespace Contracts.AnalysisDtos;

public record AnalysisAcceptedDto(Guid Id, AnalysisStatus Status);

/// <summary>Returned by GET /api/analyses/{id} — what the client polls.</summary>
public record AnalysisStatusDto(
    Guid Id,
    Guid IdeaId,
    AnalysisStatus Status,
    int CompletedModules,
    int TotalModules,
    decimal? OverallScore,
    string? Summary,
    string? Error,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    List<ScoreDto> Scores);

public sealed record ScoreDto(string ScoreType, decimal Score, string? Explanation);

/// <summary>
/// GET /api/analyses/{id}/market — the saved market result. Same shape (camelCase) as the AI's JSON,
/// so the client reads it into the same draft type it fills from the live snapshots.
/// </summary>
public sealed record MarketAnalysisDto(
    string? MarketSize,
    string? MarketTrend,
    int? MarketOpportunity,
    string? Summary,
    string? MarketOpportunityExplanation,
    int? CompetitionIntensity,
    string? CompetitionExplanation,
    List<CompetitorDto> Competitors);

public sealed record CompetitorDto(
    string Name,
    string? Description,
    string? WebsiteUrl,
    string? Industry,
    string? PricingModel,
    List<string> Strengths,
    List<string> Weaknesses,
    int? SimilarityScore);

/// <summary>GET /api/analyses/{id}/innovation — the saved innovation result, in the AI's JSON shape.</summary>
public sealed record InnovationAnalysisDto(
    int OriginalityScore,
    string OriginalityExplanation,
    int TechnologicalInnovationScore,
    string TechnologicalInnovationExplanation,
    int BusinessInnovationScore,
    string BusinessInnovationExplanation,
    int GrowthPotentialScore,
    string GrowthPotentialExplanation,
    string Summary,
    int? OverallScore,
    string? OverallExplanation);


/// <summary>
/// GET /api/analyses/{id}/audience — the saved audience result. Same shape (camelCase) as the AI's JSON,
/// so the client reads it into the same draft type it fills from the live snapshots.
/// </summary>
public sealed record AudienceAnalysisDto(
    int? OverallScore,
    string? OverallExplanation,
    AudienceSegmentDto? PrimarySegment,
    List<AudienceSegmentDto> SecondarySegments,
    List<PersonaDto> Personas,
    string? Description);

public sealed record AudienceSegmentDto(
    string Name,
    bool IsHypothesis,
    string? Need,
    string? Motivation,
    string? UsageContext,
    string? Objections,
    string? PriceSensitivity,
    string? ReachDifficulty,
    int? AttractivenessScore,
    string? AttractivenessExplanation);

public sealed record PersonaDto(
    string Name,
    string? AgeRange,
    string? Occupation,
    string? Description,
    List<string> Goals,
    List<string> PainPoints,
    List<string> Behaviors);

public sealed record AnalysisRequested(
    Guid AnalysisId,
    Guid IdeaId,
    Guid IdeaVersionId,
    Guid UserId,
    string Title,
    string Description,
    string? ProblemStatement,
    string? TargetAudience,
    IdeaCategory Category,
    string? Industry);

/// <summary>Worker picked the job up. Projects consumes this to set idea_status = Analyzing.</summary>
public sealed record AnalysisStarted(
    Guid AnalysisId,
    Guid IdeaId);

/// <summary>One module finished. Drives the progress UI (SignalR in week 2, polling for now).</summary>
public sealed record AnalysisModuleCompleted(
    Guid AnalysisId,
    Guid IdeaId,
    string Module,
    int Completed,
    int Total);

/// <summary>All modules finished. Projects sets idea_status = Analyzed; Notifier emails the user.</summary>
public sealed record AnalysisCompleted(
    Guid AnalysisId,
    Guid IdeaId,
    decimal OverallScore);

/// <summary>Pipeline failed. Projects returns idea_status to Draft so the user can retry.</summary>
public sealed record AnalysisFailed(
    Guid AnalysisId,
    Guid IdeaId,
    string Reason);

// analysis events

/// <summary>
/// Stages each modules goes through while analysing
/// </summary>


public enum ProgressStage
{
    ResearchStarted,
    Searching,          // Message = the search query
    ResearchCompleted,  // sources are saved → client can load them
    ScoringStarted,
    Partial,            // Partial = snapshot of the AI result so far (Redis only)
    Restarted,          // a retry started - client clears the draft
    Completed,
    Failed,              // Message = short error text
    Log // A log message for the client
}

/// <summary>One event for the client. Milestones go over RabbitMQ, Partial over Redis pub/sub.</summary>
/// <param name="Seq">Only for Partial: the snapshot's number within one run (1, 2, …), so the client can drop
/// snapshots that arrive out of order. 0 = not numbered.</param>
public record AnalysisEvent(
    Guid AnalysisId,
    Guid UserId,
    string Module,
    ProgressStage Stage,
    string? Message = null,
    JsonElement? Partial = null,
    int Seq = 0);

    public record AnalysisPartial(
    Guid AnalysisId,
    Guid UserId,
    string Module,
    int Seq,
    JsonElement Snapshot);