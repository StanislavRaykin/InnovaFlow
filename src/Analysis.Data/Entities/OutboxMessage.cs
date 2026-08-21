namespace InnovaFlow.Analysis.Data;

/// <summary>
/// Transactional outbox. Messages are written in the same transaction as the
/// state change that produced them, then published by a background dispatcher.
/// </summary>
public class OutboxMessage
{
    public long Id { get; set; }
    public string MessageType { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public Guid CorrelationId { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }
    public int PublishAttempts { get; set; }
}
