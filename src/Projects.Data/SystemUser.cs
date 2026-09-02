using InnovaFlow.Projects.Data;
namespace Projects.Data;

/// <summary>
/// No user, all filters bypassed. For migrations, event consumers and tests.
/// </summary>
public sealed class SystemUser : ICurrentUser
{
    public Guid? Id => null;
    public bool IsSystem => true;
}
