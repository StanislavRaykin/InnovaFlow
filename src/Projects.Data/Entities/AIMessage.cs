namespace InnovaFlow.Projects.Data;

public class AIMessage
{
    public Guid Id { get; set; }

    public Guid ConversationId { get; set; }
    public AIConversation Conversation { get; set; } = null!;

    public MessageRole Role { get; set; }
    public string Content { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}
