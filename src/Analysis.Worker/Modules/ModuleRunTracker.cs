using Analysis.Data.Entities;
using InnovaFlow.Analysis.Data;
using Microsoft.EntityFrameworkCore;

namespace Analysis.Worker.Modules;

/// <summary>
/// Writes the ModuleRuns row for a module. Running on start, Completed/Failed at the end.
/// Scoped, so it shares the module's DbContext.
/// </summary>
public class ModuleRunTracker(AnalysisDbContext db, ILogger<ModuleRunTracker> logger)
{
    
    public async Task StartAsync(Guid analysisId, string module, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var running = ModuleRunStatus.Running.ToString();

        // rerun again
        var reset = await db.ModuleRuns
            .Where(r => r.AnalysisId == analysisId && r.Module == module)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, running)
                .SetProperty(r => r.StartedAt, now)
                .SetProperty(r => r.CompletedAt, (DateTime?)null)
                .SetProperty(r => r.Error, (string?)null), ct);

        if (reset > 0) return;

        var run = new ModuleRun
        {
            AnalysisId = analysisId,
            Module = module,
            Status = running,
            StartedAt = now
        };
        db.ModuleRuns.Add(run);
        await db.SaveChangesAsync(ct);

        db.Entry(run).State = EntityState.Detached;
    }

    public Task CompleteAsync(Guid analysisId, string module) =>
        FinishAsync(analysisId, module, ModuleRunStatus.Completed, null);

    public Task FailAsync(Guid analysisId, string module, Exception ex) =>
        FinishAsync(analysisId, module, ModuleRunStatus.Failed,
            ex is OperationCanceledException ? "canceled" : ex.Message);

  
    private async Task FinishAsync(Guid analysisId, string module, ModuleRunStatus status, string? error)
    {
        try
        {
            var now = DateTime.UtcNow;
            var statusText = status.ToString();

            await db.ModuleRuns
                .Where(r => r.AnalysisId == analysisId && r.Module == module)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, statusText)
                    .SetProperty(r => r.CompletedAt, now)
                    .SetProperty(r => r.Error, error), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to mark module run {Module} as {Status}", module, status);
        }
    }
}
