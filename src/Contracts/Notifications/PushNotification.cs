namespace Contracts.Notifications;

/// <summary>
/// Sent by Notifier over /hubs/notifications as the "Notification" message; the client shows it as a toast.
/// </summary>
/// <param name="Type">"AnalysisCompleted" | "ModuleCompleted"</param>
/// <param name="Link">Client route to open on click, relative (e.g. "analyses/{id}"); null = nothing to open.</param>
/// <param name="Icon">Material Symbols name; null = the client picks one by <paramref name="Type"/>.</param>
/// <param name="SubjectId">What it's about (the analysis id for both types), so open pages can refresh it.</param>
public sealed record PushNotification(
    Guid Id,
    string Type,
    string Title,
    string? Message,
    string? Link,
    DateTimeOffset CreatedAt,
    string? Icon = null,
    Guid? SubjectId = null);
