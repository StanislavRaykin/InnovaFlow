using Contracts;
using Contracts.AnalysisDtos;
using Contracts.Notifications;
using InnovaFlow.Notifier.Data;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
namespace Notifier.Events;

/// <summary>
/// Saves an "analysis ready" notification and pushes it to the user's open tabs.
/// The class name sets the queue name (kebab-case), so it must not clash with another service's
/// AnalysisCompleted consumer, or the two would compete for the same messages.
/// </summary>
public class AnalysisCompletedNotificationConsumer(
    NotifierDbContext db,
    IHubContext<NotificationHub> hub,
    ILogger<AnalysisCompletedNotificationConsumer> logger) : IConsumer<AnalysisCompleted>
{
    public async Task Consume(ConsumeContext<AnalysisCompleted> ctx)
    {
        var m = ctx.Message;
        var sourceId = ctx.MessageId ?? m.AnalysisId;

        // At-least-once delivery: a redelivered message must not notify twice
        if (await db.Notifications.AnyAsync(n => n.SourceMessageId == sourceId, ctx.CancellationToken))
        {
            logger.LogInformation("Notification for message {MessageId} already sent; skipping", sourceId);
            return;
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = m.UserId,
            Type = NotificationType.AnalysisCompleted,
            Title = "Анализът е готов",
            Message = $"„{Shorten(m.IdeaTitle, 80)}“ · обща оценка {m.OverallScore:0}/100",
            SubjectId = m.AnalysisId,
            SubjectType = "Analysis",
            CreatedAt = DateTimeOffset.UtcNow,
            SourceMessageId = sourceId
        };
        db.Notifications.Add(notification);

        try
        {
            await db.SaveChangesAsync(ctx.CancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (await db.Notifications.AnyAsync(n => n.SourceMessageId == sourceId, ctx.CancellationToken))
                return;     // another instance saved it between the check and the insert; it pushes
            throw;
        }

        await hub.Clients.User(m.UserId.ToString()).SendAsync("Notification",
            new PushNotification(
                notification.Id,
                notification.Type.ToString(),
                notification.Title,
                notification.Message,
                $"analyses/{m.AnalysisId}",
                notification.CreatedAt,
                SubjectId: m.AnalysisId),
            ctx.CancellationToken);
    }

    private static string Shorten(string text, int max) =>
        text.Length <= max ? text : text[..(max - 1)].TrimEnd() + "…";
}

/// <summary>
/// Pushes "module done" as each analyzer finishes, so the user can open its tab while the rest still run.
/// Not saved: it's progress, the saved notification is the one for the whole analysis.
/// </summary>
public class AnalysisModuleCompletedNotificationConsumer(IHubContext<NotificationHub> hub) : IConsumer<AnalysisModuleCompleted>
{
    public Task Consume(ConsumeContext<AnalysisModuleCompleted> ctx)
    {
        var m = ctx.Message;
        var module = AnalysisModules.Find(m.Module);

        var toast = new PushNotification(
            ctx.MessageId ?? Guid.NewGuid(),     // a redelivery keeps its id, so the client drops the repeat
            "ModuleCompleted",
            $"Готово: {module?.Name ?? m.Module}",
            $"{m.Completed} от {m.Total} анализа са готови",
            module is null ? $"analyses/{m.AnalysisId}" : $"analyses/{m.AnalysisId}/{module.Path}",
            DateTimeOffset.UtcNow,
            module?.MiIcon,
            m.AnalysisId);

        return hub.Clients.User(m.UserId.ToString()).SendAsync("Notification", toast, ctx.CancellationToken);
    }
}
