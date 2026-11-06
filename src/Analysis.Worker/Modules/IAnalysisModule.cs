using Contracts.AnalysisDtos;
using Contracts;
using InnovaFlow.Analysis.Data;

namespace Analysis.Worker.Modules;

public interface IAnalysisModule
{
       /// <summary>
    /// The name of this module
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Execution stage. Modules in the same stage run in parallel; stages run in order.
    /// 1 = independent analyzers, 2 = aggregation, 3 = summary (overall score, recommendations)
    /// </summary>
    int Stage { get; }

    /// <summary>Runs the module</summary>
    Task RunAsync(AnalysisRequested ctx, CancellationToken ct);
}