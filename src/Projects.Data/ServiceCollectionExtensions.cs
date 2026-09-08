using Microsoft.Extensions.DependencyInjection;
using InnovaFlow.Projects.Data;
using Microsoft.EntityFrameworkCore;
namespace Projects.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectsData<TCurrentUser>(
        this IServiceCollection services,
        string connectionString)
        where TCurrentUser : class, ICurrentUser
    {
        services.AddScoped<ICurrentUser, TCurrentUser>();

        return services.AddDbContext<ProjectsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "Projects");
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }));
    }
}
