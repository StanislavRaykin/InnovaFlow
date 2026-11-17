using Analysis.Worker.AI;
using Contracts.AnalysisDtos;
using MassTransit;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
namespace Analysis.Worker.Publishers;

public record AiStreamResult(string Json, AiStreamCompleted Info);
public class PartialPublisher(IPublishEndpoint publish, ILogger<PartialPublisher> logger)
{
    private readonly TimeSpan Interval = TimeSpan.FromMilliseconds(300);

    private readonly TimeSpan Ttl = TimeSpan.FromSeconds(15);

    public async Task<AiStreamResult> PumpAsync(
        AnalysisRequested req, string module,
        IAsyncEnumerable<AiStreamEvent> stream, CancellationToken ct)
    {
        var json = new StringBuilder();
        var timer = Stopwatch.StartNew();
        var seq = 0;
        AiStreamCompleted? info = null;

        await foreach (var ev in stream.WithCancellation(ct))
        {
            switch (ev)
            {
                case AiTextDelta d:
                    json.Append(d.Text);
                    if (timer.Elapsed >= Interval)
                    {
                        timer.Restart();
                        await SendSnapshotAsync(req, module, json.ToString(), ++seq, ct);
                    }
                    break;

                case AiTextReset:
                    json.Clear();
                    break;

                case AiStreamCompleted c:
                    info = c;
                    break;
            }
        }

        // Last snapshot to ensure completion
        await SendSnapshotAsync(req, module, json.ToString(), ++seq, ct);

        if (info == null)
        {
            throw new InvalidOperationException("AI stream ended without a completion event.");
        }

        return new AiStreamResult(json.ToString(), info);
    }

    private async Task SendSnapshotAsync(
        AnalysisRequested req, string module, string text, int seq, CancellationToken ct)
    {
        if (!PartialJson.TryParse(text, out var node) || node == null) return;

        var msg = new AnalysisPartial(
            req.AnalysisId, req.UserId, module, seq,
            JsonSerializer.SerializeToElement(node));

        try
        {
            await publish.Publish(msg, ctx =>
            {
                ctx.Durable = false;      // not written to disk
                ctx.TimeToLive = Ttl;     // dropped if nobody reads it
            }, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogDebug(ex, "Partial #{Seq} for {Module} not published", seq, module);
        }
    }

}
