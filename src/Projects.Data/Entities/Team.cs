namespace InnovaFlow.Projects.Data;

public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid OwnerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<TeamMember> Members { get; set; } = [];
    public List<Idea> Ideas { get; set; } = [];
}
