namespace InnovaFlow.Analysis.Data;

/// <summary>
/// Supplies the caller identity to the global query filter. Implemented in
/// Analysis.Api from the JWT 'sub' claim; in Analysis.Worker by a
/// SystemUserContext that bypasses filtering (workers act on behalf of no one).
/// </summary>
public interface ICurrentUser
{
    Guid? Id { get; }
    bool IsSystem { get; }
}
