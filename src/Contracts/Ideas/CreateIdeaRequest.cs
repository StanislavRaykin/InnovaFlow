namespace Contracts.Ideas;

public sealed record CreateIdeaRequest(
    Guid Id,
    string Title,
    string Description,
    string? ProblemStatement,
    string? TargetAudience,
    IdeaCategory Category,
    string? Industry);

// autosaved every few seconds while the wizard is open, so every field may
// still be empty; full validation happens on CreateIdeaRequest
public sealed record SaveDraftRequest(
    string? Title,
    string? Description,
    string? ProblemStatement,
    string? TargetAudience,
    IdeaCategory Category,
    string? Industry);

public sealed record IdeaDto(
    Guid Id,
    string Title,
    string Description,
    string? ProblemStatement,
    string? TargetAudience,
    IdeaCategory Category,
    string? Industry,
    IdeaStatus Status,
    Guid CurrentVersionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
