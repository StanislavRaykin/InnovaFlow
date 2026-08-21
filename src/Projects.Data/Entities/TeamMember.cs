namespace InnovaFlow.Projects.Data;

public class TeamMember
{
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public Guid UserId { get; set; }
    public MemberRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
