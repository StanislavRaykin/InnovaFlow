using InnovaFlow.Projects.Data;
namespace Projects.Data;

// ProjectsDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Used only by dotnet ef. The connection string is never opened when
/// generating a migration - the tools just need a constructible context.
/// </summary>
public class ProjectsDbContextFactory : IDesignTimeDbContextFactory<ProjectsDbContext>
{
    public string connectionString { get; set; } = string.Empty;

    public ProjectsDbContext CreateDbContext(string[] args)
    {
         
        var options = new DbContextOptionsBuilder<ProjectsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Username=postgres;Password=9qNx2vCp18~_sc+)Rp+gGH", npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "Projects"))
            .Options;

        return new ProjectsDbContext(options, new SystemUser());
    }

}
