namespace InnovaFlow.Projects.Data;

public class StrategyMetric
{
    public Guid Id { get; set; }

    public Guid StrategyId { get; set; }
    public Strategy Strategy { get; set; } = null!;

    public string MetricName { get; set; } = null!;
    public decimal Score { get; set; }
    public string? Explanation { get; set; }
}
