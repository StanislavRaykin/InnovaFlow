namespace InnovaFlow.Projects.Data;

/// <summary>
/// Stored as int, ordered ascending by privilege, so effective role is
/// MAX(idea role, team role) in both C# and SQL. Do not reorder these.
/// </summary>
public enum MemberRole
{
    Viewer = 0,
    Editor = 1,
    Owner  = 2
}

public enum IdeaCategory
{
    Business, Brand, Startup, Product, Service,
    Application, SocialInitiative, Research, Other
}

public enum IdeaStatus { Draft, Analyzing, Analyzed, Archived }
