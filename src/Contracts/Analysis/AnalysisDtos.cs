namespace Contracts.AnalysisDtos;

public sealed record StartAnalysisRequest(Guid IdeaId);

/// <summary>Returned by the 202 on POST /api/analyses.</summary>
public sealed record AnalysisAcceptedDto(Guid Id, AnalysisStatus Status);

/// <summary>Returned by GET /api/analyses/{id} — what the client polls.</summary>
public sealed record AnalysisStatusDto(
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
    IReadOnlyList<ScoreDto> Scores);

public sealed record ScoreDto(string ScoreType, decimal Score, string? Explanation);

/// <summary>Market Analyzer output — one row per analysis.</summary>
public sealed record MarketAnalysisDto(
    string? MarketSize,
    string? MarketTrend,
    decimal MarketOpportunity,
    string Summary);
