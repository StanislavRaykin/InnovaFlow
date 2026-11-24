using Contracts.AnalysisDtos;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
namespace Notifier.Events;

public class AnalysisProgressConsumer(IHubContext<AnalysisHub> hub) : IConsumer<AnalysisEvent>
{
    public Task Consume(ConsumeContext<AnalysisEvent> ctx)
    {
        return hub.Clients.User(ctx.Message.UserId.ToString())
           .SendAsync("AnalysisEvent", ctx.Message, ctx.CancellationToken);
    }
}

public class AnalysisPartialConsumer(IHubContext<AnalysisHub> hub) : IConsumer<AnalysisPartial>
{
    public Task Consume(ConsumeContext<AnalysisPartial> ctx)
    {
        var p = ctx.Message;
        var e = new AnalysisEvent(p.AnalysisId, p.UserId, p.Module, ProgressStage.Partial, Partial: p.Snapshot, Seq: p.Seq);
        return hub.Clients.User(p.UserId.ToString()).SendAsync("AnalysisEvent", e, ctx.CancellationToken);
    }
}
