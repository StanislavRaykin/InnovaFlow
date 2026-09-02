namespace InnovaFlow.Projects.Data;

/// <summary>
/// Direct grant on a single idea. Independent of team membership - a user can
/// hold both, and the higher one wins.
/// </summary>
public class IdeaMember
{
    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public Guid UserId { get; set; }
    public MemberRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}
