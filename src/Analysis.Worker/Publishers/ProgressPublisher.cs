using MassTransit;
using Contracts.AnalysisDtos;
namespace Analysis.Worker.Publishers;

public class ProgressPublisher(IPublishEndpoint bus, ILogger<ProgressPublisher> logger)
{
    public Task ReportAsync(AnalysisRequested req, string module, ProgressStage stage,
        string? message = null, CancellationToken ct = default)
    {
        return bus.Publish(new AnalysisEvent(req.AnalysisId, req.UserId, module, stage, message), ct);

    }

    public async Task LogAsync(AnalysisRequested req, string module, string text, CancellationToken ct = default)
    {
        try
        {
            await bus.Publish(new AnalysisEvent(req.AnalysisId, req.UserId, module, ProgressStage.Log, text),
                ctx => { ctx.Durable = false; ctx.TimeToLive = TimeSpan.FromSeconds(30); }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish log line for {Module}", module);
        }
    }

}
