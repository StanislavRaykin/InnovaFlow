using InnovaFlow.Projects.Data;
namespace Projects.Data;



public interface IActor
{
    Guid? Id { get; }
    bool IsSystem { get; }
}

/// <summary>
/// No user, all filters bypassed. For migrations, event consumers and tests.
/// </summary>
public sealed class SystemUser : IActor
{
    public static readonly SystemUser Instance = new();
    public Guid? Id => null;
    public bool IsSystem => true;
}

public sealed class UserActor(Guid id) : IActor
{
    public Guid? Id => id;
    public bool IsSystem => false;
}