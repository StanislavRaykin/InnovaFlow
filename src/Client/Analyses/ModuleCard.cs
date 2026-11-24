using System.Text.Json;

namespace Client.Analyses;

/// <summary>The non-generic part of <see cref="ModuleCard{TDraft}"/>, so the live state can route events by module key.</summary>
public interface IModuleCard
{
    int LastSeq { get; set; }
    bool IsDone { get; set; }
    string Status { get; set; }         // Pending | Running | Completed | Failed
    string? Label { get; set; }         // "ОЦЕНЯВАНЕ…"
    bool HasFinal { get; }
    void Apply(JsonElement snapshot);
    void Reset();

    /// <summary>Swaps the draft for the saved result if the module has completed. True if it did.</summary>
    Task<bool> LoadFinalAsync(Guid analysisId);
}

/// <summary>
/// Live state of one module: the latest partial draft and which field the AI is writing right now.
/// Once the module completes, the draft is swapped for the saved result (<paramref name="loadFinal"/>).
/// </summary>
/// <param name="loadFinal">Fetches the saved result; null while the module hasn't completed.</param>
public sealed class ModuleCard<TDraft>(Func<Guid, Task<TDraft?>> loadFinal) : IModuleCard where TDraft : class
{
    private int generation;             // bumped by Reset, so a load started before it is dropped
    private bool loadingFinal;

    public TDraft? Draft { get; private set; }

    /// <summary>Path of the field being written, e.g. "personas[1].description". Empty when nothing is streaming.</summary>
    public string ActivePath { get; private set; } = "";

    public int LastSeq { get; set; }
    public bool IsDone { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Label { get; set; }

    /// <summary>The draft is the saved result, not a live snapshot.</summary>
    public bool HasFinal { get; private set; }

    public void Apply(JsonElement snapshot)
    {
        TDraft? draft;
        try
        {
            draft = snapshot.Deserialize<TDraft>(JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return;     // e.g. "score": 7.5 for an int; keep the last good draft
        }

        Draft = draft;
        ActivePath = DraftPath.Active(snapshot);
    }

    public async Task<bool> LoadFinalAsync(Guid analysisId)
    {
        if (Status != "Completed" || HasFinal || loadingFinal) return false;

        var gen = generation;
        loadingFinal = true;
        try
        {
            var final = await loadFinal(analysisId);
            if (final is null || gen != generation) return false;

            Draft = final;
            ActivePath = "";
            IsDone = true;
            HasFinal = true;
            return true;
        }
        finally
        {
            if (gen == generation) loadingFinal = false;
        }
    }

    public void Reset()
    {
        generation++;
        loadingFinal = false;
        Draft = null;
        ActivePath = "";
        LastSeq = 0;
        IsDone = false;
        Status = "Pending";
        Label = null;
        HasFinal = false;
    }

    /// <summary>Not being written any more (it may still be missing; check the value too).</summary>
    public bool Done(string path) => IsDone || !IsActive(path);

    /// <summary>The AI is writing this field, or something inside it, right now.
    /// "marketOpportunity" doesn't match "marketOpportunityExplanation"; "personas" matches "personas[1].name".</summary>
    public bool IsActive(string path) =>
        !IsDone
        && ActivePath.StartsWith(path, StringComparison.OrdinalIgnoreCase)
        && (ActivePath.Length == path.Length || ActivePath[path.Length] is '.' or '[');
}

public static class DraftPath
{
    /// <summary>
    /// The field being written right now, e.g. "personas[1].description".
    /// The AI writes in order, so it's the last property of the last object, all the way down.
    /// </summary>
    public static string Active(JsonElement el, string path = "")
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                JsonProperty? last = null;
                foreach (var p in el.EnumerateObject()) last = p;
                return last is null ? path
                    : Active(last.Value.Value, path == "" ? last.Value.Name : $"{path}.{last.Value.Name}");

            case JsonValueKind.Array:
                var n = el.GetArrayLength();
                return n == 0 ? path : Active(el[n - 1], $"{path}[{n - 1}]");

            default:
                return path;
        }
    }
}
