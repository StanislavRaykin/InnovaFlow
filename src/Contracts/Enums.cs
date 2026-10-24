namespace InnovaFlow.Projects.Data;

public enum IdeaCategory
{
    Business, Brand, Startup, Product, Service,
    Application, SocialInitiative, Research, Other
}

public enum IdeaStatus { Draft, Analyzing, Analyzed, Archived }

/// <summary>
/// One enum for both idea membership and team membership. The values are
/// ordered deliberately: a user's effective role on an idea is the higher of
/// their direct grant and their team grant, which is a plain Math.Max because
/// the underlying values ascend in privilege.
/// </summary>
public enum MemberRole { Viewer = 0, Editor = 1, Owner = 2 }

public enum MessageRole { System, User, Assistant }
