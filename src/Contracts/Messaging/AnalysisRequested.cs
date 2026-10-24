namespace Contracts.Messaging;



public sealed record AnalysisRequested(
 Guid AnalysisId,
 Guid IdeaId,
 Guid IdeaVersionId,
 Guid UserId,
 // snapshot — the worker must not call back into Projects
 string Title,
 string Description,
 string? ProblemStatement,
 string? TargetAudience,
 IdeaCategory Category,
 string? Industry);

public sealed record AnalysisModuleCompleted(
    Guid AnalysisId, Guid IdeaId, string Module, int Completed, int Total);

public sealed record AnalysisCompleted(Guid AnalysisId, Guid IdeaId, decimal OverallScore);
public sealed record AnalysisFailed(Guid AnalysisId, Guid IdeaId, string Reason);

