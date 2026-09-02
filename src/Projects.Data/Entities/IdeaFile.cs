namespace InnovaFlow.Projects.Data;

public class IdeaFile
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    /// <summary>
    /// Recorded for display only. Deletion is governed by the caller's role on
    /// the idea, not by who uploaded the file - any editor can remove any file.
    /// </summary>
    public Guid UploadedById { get; set; }

    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = null!;
    public DateTimeOffset UploadedAt { get; set; }
}
