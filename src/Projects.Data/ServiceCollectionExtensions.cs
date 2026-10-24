using Microsoft.Extensions.DependencyInjection;
using InnovaFlow.Projects.Data;
using Microsoft.EntityFrameworkCore;
using ServiceDefaults;
namespace Projects.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectsData(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddDbContext<ProjectsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "Projects");
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }));
        
    }
}
