using InnovaFlow.Projects.Data;
namespace Projects.Data;

// ProjectsDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Used only by dotnet ef. The connection string is never opened when
/// generating a migration - the tools just need a constructible context.
/// </summary>
public class ProjectsDbContextFactory : IDesignTimeDbContextFactory<ProjectsDbContext>
{
    public ProjectsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ProjectsDbContext>()
            .UseNpgsql("Host=localhost;Database=innovaflow;Username=postgres;Password=postgres",
                npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "Projects"))
            .Options;

        return new ProjectsDbContext(options, new SystemUser());
    }
}
