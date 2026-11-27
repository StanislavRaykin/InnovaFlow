using Client.Notifications;
using Contracts.Notifications;
using Microsoft.AspNetCore.Components;

namespace Client.Ideas;

/// <summary>
/// Base for pages that list the user's ideas (@inherits IdeaBoardPage): loads <see cref="Cards"/>, and reloads
/// when a module or a whole analysis finishes (push notification), so scores and progress update by themselves.
/// </summary>
public abstract class IdeaBoardPage : ComponentBase, IDisposable
{
    [Inject] protected IdeaBoard Board { get; set; } = default!;
    [Inject] protected NotificationHubClient Notifications { get; set; } = default!;
    [Inject] protected ILogger<IdeaBoardPage> Logger { get; set; } = default!;

    protected List<IdeaCard>? Cards { get; private set; }
    protected bool LoadFailed { get; private set; }
    protected bool Reloading { get; private set; }

    private bool reloadAgain;      // a notification came in while loading
    private readonly CancellationTokenSource disposed = new();

    protected override async Task OnInitializedAsync()
    {
        Notifications.Received += OnNotification;
        await ReloadAsync();
    }

    protected async Task ReloadAsync()
    {
        if (Reloading) { reloadAgain = true; return; }

        Reloading = true;
        try
        {
            do
            {
                reloadAgain = false;
                Cards = await Board.LoadAsync(disposed.Token);
                LoadFailed = false;
            } while (reloadAgain);
        }
        catch (OperationCanceledException) when (disposed.IsCancellationRequested) { return; }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
        {
            Logger.LogWarning(ex, "Ideas could not be loaded");
            LoadFailed = true;           // keep what's shown; the page offers a retry
        }
        finally
        {
            Reloading = false;
        }
        StateHasChanged();
    }

    private void OnNotification(PushNotification n)
    {
        if (n.Type is "ModuleCompleted" or "AnalysisCompleted")
            _ = InvokeAsync(ReloadAsync);
    }

    public virtual void Dispose()
    {
        Notifications.Received -= OnNotification;
        disposed.Cancel();
        disposed.Dispose();
    }
}
