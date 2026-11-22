using InnovaFlow.Analysis.Data;

namespace Analysis.Data.Entities;

/// <summary>
/// One row per (analysis, module). Inserted as Running when the module starts;
/// a rerun of the same module resets the row instead of adding a new one.
/// </summary>
public class ModuleRun
{
    public Guid AnalysisId { get; set; }

    public string Module { get; set; } = "";      // = IAnalysisModule.Name
    public string Status { get; set; } = nameof(ModuleRunStatus.Pending); // Pending | Running | Completed | Failed
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}
