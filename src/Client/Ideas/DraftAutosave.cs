using System.Net.Http.Json;
using Contracts.Ideas;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;
using System;

namespace Client.Ideas;

// saves the wizard's IdeaDraft every couple of seconds, but only sends a
// request when something changed since the last successful save
public class DraftAutosave(HttpClient http, IdeaDraft draft, ILogger<DraftAutosave> logger)
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    private readonly HttpClient http = http;
    private readonly IdeaDraft draft = draft;
    private readonly ILogger<DraftAutosave> logger = logger;

    // an untouched draft matches this, so opening the wizard doesn't create one
    private SaveDraftRequest lastSaved = new IdeaDraft().ToRequest();
    private CancellationTokenSource? cts;
    private Task? loop;

    //how many pages are using this service
    private int users;

    // one save at a time, so the request that creates the draft can't race an update
    private readonly SemaphoreSlim saving = new(1, 1);

    public void Start()
    {
        users++;
        if (loop != null)
            return;

        cts = new CancellationTokenSource();
        loop = RunAsync(cts.Token);
    }

    // called when a wizard page goes away; saves whatever the last tick missed,
    // and stops the loop once no wizard page is left
    public async Task StopAsync()
    {
        if (users == 0)
            return;

        users--;
        if (users == 0 && loop != null)
        {
            // cleared before awaiting: a wizard page may call Start meanwhile
            var (stoppedLoop, stoppedCts) = (loop, cts!);
            loop = null;
            cts = null;

            stoppedCts.Cancel();
            await stoppedLoop;
            stoppedCts.Dispose();
        }

        await SaveIfChangedAsync();
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                await SaveIfChangedAsync();
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Draft {DraftId} autosave loop canceled", draft.Id);
        }
    }

    private async Task SaveIfChangedAsync()
    {
        await saving.WaitAsync();
        try
        {
            await SaveAsync();
        }
        finally
        {
            saving.Release();
        }
    }

    private async Task SaveAsync()
    {
        var current = draft.ToRequest();
        if (current == lastSaved)
            return;

        try
        {
            var response = await http.PutAsJsonAsync($"api/idea/drafts/{draft.Id}", current);

            if (response.IsSuccessStatusCode)
            {
                lastSaved = current;
            }
            else if ((int)response.StatusCode < 500)
            {
                // the same payload would be rejected again, so wait for the next edit
                lastSaved = current;
                logger.LogWarning("Draft {DraftId} was rejected: {Status}", draft.Id, response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            // lastSaved stays as it was, so the next tick retries
            logger.LogWarning(ex, "Draft {DraftId} could not be saved", draft.Id);
        }
    }
}
