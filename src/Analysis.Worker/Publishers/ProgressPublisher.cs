using MassTransit;
using Contracts.AnalysisDtos;
namespace Analysis.Worker.Publishers;

public class ProgressPublisher(IPublishEndpoint bus)
{
    public Task ReportAsync(AnalysisRequested req, string module, ProgressStage stage,
        string? message = null, CancellationToken ct = default)
    {
        return bus.Publish(new AnalysisEvent(req.AnalysisId, req.UserId, module, stage, message), ct);

    }
}