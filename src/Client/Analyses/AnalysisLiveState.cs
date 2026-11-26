using Client.Notifications;
using Client.Views;
using Contracts.Notifications;
using Contracts.AnalysisDtos;
using Microsoft.AspNetCore.SignalR.Client;

namespace Client.Analyses;

/// <summary>
/// Live results of the analysis that's open, shared by all result tabs, so switching tabs keeps what has
/// streamed so far. Scoped = app lifetime in WASM.
/// </summary>
public sealed class AnalysisLiveState(HubClient hubClient, AnalysisClient api, NotificationHubClient notifications) : IAsyncDisposable
{
    public Guid? AnalysisId { get; private set; }
    public AnalysisStatusView? Status { get; private set; }

    public ModuleCard<MarketDraft> Market { get; } = new(id => api.GetMarketAsync(id));
    public ModuleCard<InnovationDraft> Innovation { get; } = new(id => api.GetInnovationAsync(id));
    public ModuleCard<AudienceDraft> Audience { get; } = new(id => api.GetAudienceAsync(id));
    public ModuleCard<FeasibilityDraft> Feasibility { get; } = new(id => api.GetFeasibilityAsync(id));
    public ModuleCard<RiskDraft> Risk { get; } = new(id => api.GetRisksAsync(id));

    /// <summary>Module key (Contracts.AnalysisModules) → card</summary>
    private Dictionary<string, IModuleCard> Cards => cards ??= new()
    {
        ["Market"] = Market,
        ["Innovation"] = Innovation,
        ["Audience"] = Audience,
        ["Feasibility"] = Feasibility,
        ["Risk"] = Risk,
    };
    private Dictionary<string, IModuleCard>? cards;

    /// <summary>Raised after any change; handlers must marshal with InvokeAsync.</summary>
    public event Action? Changed;

    private HubConnection Hub => hubClient.Connection;
    private IDisposable? handler;
    private Task? watching;

    /// <summary>
    /// Called by every tab. Starts following <paramref name="id"/> the first time; later calls
    /// return the same task, so when it completes <see cref="Status"/> is loaded (null = not found).
    /// </summary>
    public Task WatchAsync(Guid id)
    {
        if (AnalysisId == id && watching is not null)
        {
            LoadFinals();                           // retry any that failed, on each tab switch
            return watching;
        }

        AnalysisId = id;
        Status = null;
        foreach (var c in Cards.Values) c.Reset();

        return watching = StartWatchingAsync(id);
    }

    private async Task StartWatchingAsync(Guid id)
    {
        try
        {
            handler ??= Hub.On<AnalysisEvent>("AnalysisEvent", OnEvent);
            Hub.Reconnected -= OnReconnected;
            Hub.Reconnected += OnReconnected;
            notifications.Received -= OnNotification;
            notifications.Received += OnNotification;

            await hubClient.EnsureStartedAsync();   // listen first…
            await LoadStatusAsync();                // …then load the state, so nothing falls in between
        }
        catch when (AnalysisId == id)
        {
            watching = null;                        // let the next call try again
            throw;
        }
    }

    private async Task LoadStatusAsync()
    {
        var id = AnalysisId;
        if (id is null) return;

        var status = await api.GetStatusAsync(id.Value);
        if (AnalysisId != id) return;               // moved to another analysis meanwhile

        Status = status;
        foreach (var m in Status?.Modules ?? [])
        {
            // never undo a Completed/Failed we already got live; only Restarted does that
            if (Cards.TryGetValue(m.Key, out var card) && !card.IsDone)
            {
                card.Status = m.Status;
                card.IsDone = m.Status is "Completed" or "Failed";
            }
        }
        Changed?.Invoke();
        LoadFinals();
    }

    private Task OnReconnected(string? _) => LoadStatusAsync();

    // The overall score is computed after the last module, so it only arrives with the "analysis done" push
    private void OnNotification(PushNotification n)
    {
        if (n.Type == "AnalysisCompleted" && n.SubjectId is { } id && id == AnalysisId)
            _ = ReloadStatusQuietlyAsync();
    }

    private async Task ReloadStatusQuietlyAsync()
    {
        try
        {
            await LoadStatusAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // shown on the next tab switch or reconnect instead
        }
    }

    /// <summary>Swap every completed module's draft for its saved result (clamped scores, and the only data
    /// there is when the analysis is opened after it finished; snapshots aren't stored).</summary>
    private void LoadFinals()
    {
        foreach (var card in Cards.Values) _ = LoadFinalAsync(card);
    }

    private async Task LoadFinalAsync(IModuleCard card)
    {
        if (AnalysisId is not { } id) return;
        try
        {
            if (await card.LoadFinalAsync(id)) Changed?.Invoke();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // keep showing the draft; tried again on the next tab switch or reconnect
        }
    }

    private void OnEvent(AnalysisEvent e)
    {
        if (e.AnalysisId != AnalysisId || !Cards.TryGetValue(e.Module, out var card)) return;
        if (e.Stage == ProgressStage.Log) return;                        // only the processing page shows logs
        if (card.IsDone && e.Stage != ProgressStage.Restarted) return;   // late snapshot after Completed

        switch (e.Stage)
        {
            case ProgressStage.Partial:
                if (e.Partial is null) return;
                if (e.Seq != 0 && e.Seq <= card.LastSeq) return;         // arrived out of order
                card.LastSeq = e.Seq;
                card.Status = "Running";
                card.Apply(e.Partial.Value);
                break;

            case ProgressStage.Restarted:
                card.Reset();
                card.Status = "Running";
                break;

            case ProgressStage.Completed or ProgressStage.Failed:
                card.IsDone = true;
                card.Status = e.Stage.ToString();
                if (Status is not null)
                {
                    Status.Done++;
                    if (Status.Modules.FirstOrDefault(m => m.Key == e.Module) is { } module)
                        module.Status = card.Status;
                }
                _ = LoadFinalAsync(card);
                break;

            default:
                card.Status = "Running";
                card.Label = e.Stage switch
                {
                    ProgressStage.ResearchStarted => "ПРОУЧВАНЕ…",
                    ProgressStage.Searching => "ТЪРСЕНЕ…",
                    ProgressStage.ScoringStarted => "ОЦЕНЯВАНЕ…",
                    _ => card.Label
                };
                break;
        }
        Changed?.Invoke();
    }

    public ValueTask DisposeAsync()
    {
        // the connection itself is shared (HubClient), so only drop what this class added
        handler?.Dispose();
        Hub.Reconnected -= OnReconnected;
        notifications.Received -= OnNotification;
        return ValueTask.CompletedTask;
    }
}
