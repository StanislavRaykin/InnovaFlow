using Contracts.AnalysisDtos;

namespace Analysis.Worker.Modules;

public interface IAnalysisModule
{
       /// <summary>
    /// The name of this module
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Execution stage. Modules in the same stage run in parallel; stages run in order.
    /// 0 = classifier, 1 = independent analyzers, 2 = aggregation (overall score, recommendations)
    /// </summary>
    int Stage { get; }

    /// <summary>
    /// True if this module applies to this idea. For example, brand analysis doesnt apply for a non-brand idea    /// </summary>
    bool AppliesTo(AnalysisRequested ctx) => true;

    /// <summary>Runs the module</summary>
    Task RunAsync(AnalysisRequested ctx, CancellationToken ct);
}