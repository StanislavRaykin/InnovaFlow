namespace Contracts.Ideas;

public sealed record CreateIdeaRequest(
    Guid Id,
    string Title,
    string Description,
    string? ProblemStatement,
    string? TargetAudience,
    IdeaCategory Category,
    string? Industry);

// autosaved every 2secs
public record SaveDraftRequest(
    string? Title,
    string? Description,
    string? ProblemStatement,
    string? TargetAudience,
    IdeaCategory Category,
    string? Industry);

public record IdeaDto(
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
