namespace InnovaFlow.Analysis.Data;

/// <summary>
/// One row per scored dimension. Explanation is required by requirement 14 -
/// the score is never shown on its own.
/// </summary>
public class AnalysisScore
{
    public Guid Id { get; set; }

    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public ScoreType ScoreType { get; set; }
    public decimal Score { get; set; }
    public string? Explanation { get; set; }
}
