namespace InnovaFlow.Projects.Data;

public class IdeaMember
{
    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public Guid UserId { get; set; }
    public MemberRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
