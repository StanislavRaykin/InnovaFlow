namespace InnovaFlow.Projects.Data;

public class Team
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<TeamMember> Members { get; set; } = [];
    public List<Idea> Ideas { get; set; } = [];
}
