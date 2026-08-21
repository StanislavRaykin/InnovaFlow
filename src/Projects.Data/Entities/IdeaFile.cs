namespace InnovaFlow.Projects.Data;

public class IdeaFile
{
    public Guid Id { get; set; }
    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    /// <summary>Display and audit only - it does not gate deletion.</summary>
    public Guid UploadedById { get; set; }

    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = null!;

    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }
}
