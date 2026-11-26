using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
namespace Notifier.Events;

/// <summary>
/// Push notifications (toasts), separate from <see cref="AnalysisHub"/>'s high-volume live analysis stream.
/// Server → client only: "Notification" with a <see cref="Contracts.Notifications.PushNotification"/>.
/// Users are matched by <see cref="SubUserIdProvider"/>, same as the analysis hub.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub { }
