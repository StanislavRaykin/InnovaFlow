namespace InnovaFlow.Projects.Data;

// ---------------------------------------------------------------------------
// AI chat
//
// Lives in this schema, not in Analysis, because a conversation is visible to
// every member of its idea - and membership is only queryable here. Chat does
// not run through the job pipeline: no queue message, no background work. It
// is a synchronous streaming call to the provider, so keep its controller
// separate from the analysis controllers.
// ---------------------------------------------------------------------------

public class AIConversation
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    /// <summary>Who started the thread. Does not restrict who can read it.</summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Derived from the first user message. Nullable so a thread can exist
    /// before the title is generated.
    /// </summary>
    public string? Title { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<AIMessage> Messages { get; set; } = [];
}
