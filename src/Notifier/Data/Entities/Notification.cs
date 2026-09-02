namespace InnovaFlow.Notifier.Data;

/// <summary>
/// Written by the consumer when an event arrives, then pushed to the browser
/// over SignalR. The row is what makes a notification survive the user being
/// offline - SignalR alone delivers nothing to a closed tab.
/// </summary>
public class Notification
{
    public Guid Id { get; set; }

    /// <summary>Recipient. Owned by Identity, so no FK.</summary>
    public Guid UserId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = null!;
    public string? Message { get; set; }

    /// <summary>
    /// What the notification points at - usually an idea, sometimes an
    /// analysis. Kept as a bare Guid plus a discriminator so Notifier stays
    /// ignorant of the other services' models.
    /// </summary>
    public Guid? SubjectId { get; set; }
    public string? SubjectType { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The id of the message that produced this row. Unique, so a redelivered
    /// event cannot create a second copy of the same notification.
    /// </summary>
    public Guid SourceMessageId { get; set; }
}
