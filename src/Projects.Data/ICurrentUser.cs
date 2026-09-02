namespace InnovaFlow.Projects.Data;

/// <summary>
/// Supplied per request from the JWT. IsSystem is set for event consumers,
/// which run with no user and must bypass the access filter.
/// </summary>
public interface ICurrentUser
{
    Guid? Id { get; }
    bool IsSystem { get; }
}
