using Client.Analyses;
using Microsoft.AspNetCore.Components;

namespace Client.Pages.AnalysisResult;

/// <summary>
/// Base for the module result pages (@inherits LiveModulePage). Follows the analysis in the route through the
/// shared <see cref="AnalysisLiveState"/> and re-renders on every change; the page only reads its card.
/// </summary>
public class LiveModulePage : ComponentBase, IDisposable
{
    [Inject] protected AnalysisLiveState Live { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;

    [Parameter] public Guid AnalysisId { get; set; }

    protected override void OnInitialized() => Live.Changed += OnChanged;

    protected override async Task OnParametersSetAsync()
    {
        await Live.WatchAsync(AnalysisId);
        if (Live.Status is null) Nav.NavigateTo("/not-found");   // missing or not yours
    }

    private void OnChanged() => _ = InvokeAsync(StateHasChanged);

    public void Dispose() => Live.Changed -= OnChanged;
}
