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

/// <summary>Market Analyzer output — one row per analysis</summary>
public sealed record MarketAnalysisDto(
    string? MarketSize,
    string? MarketTrend,
    decimal MarketOpportunity,
    string Summary);


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