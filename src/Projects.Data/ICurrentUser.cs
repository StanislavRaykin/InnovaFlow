namespace InnovaFlow.Projects.Data;

public interface ICurrentUser
{
    Guid? Id { get; }
    bool IsSystem { get; }
}
