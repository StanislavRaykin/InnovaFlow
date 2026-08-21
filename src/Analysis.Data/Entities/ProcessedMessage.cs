namespace InnovaFlow.Analysis.Data;

/// <summary>
/// Consumer-side idempotency. A message id already present here has been
/// handled; redelivery is acknowledged and dropped.
/// </summary>
public class ProcessedMessage
{
    public Guid MessageId { get; set; }
    public string Consumer { get; set; } = null!;
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
