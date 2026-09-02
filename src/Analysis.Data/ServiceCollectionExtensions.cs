using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InnovaFlow.Analysis.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalysisData(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddDbContext<AnalysisDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                // Must match in Api and Worker - they share this context and
                // would otherwise disagree about which migrations are applied.
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "Analysis");

                // Neon scales its compute to zero; the first connection after
                // an idle period can time out.
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }));
    }
}