namespace InnovaFlow.Analysis.Data;

public enum AnalysisStatus { Pending, Running, Completed, Failed }

public enum RiskSeverity { Low, Medium, High, Critical }

public enum Priority { Low, Medium, High }

/// <summary>The twelve DAG nodes. The value is also the queue-visible name.</summary>
public enum NodeType
{
    Classification,
    Market,
    Competitors,
    Audience,
    Personas,
    Innovation,
    Business,
    Brand,
    Feasibility,
    Risks,
    Opportunities,
    Recommendations
}

/// <summary>
/// The six dimensions on the dashboard. Innovation is a score row like the
/// rest - AnalysisScore.Explanation carries the reasoning text, so it needs no
/// table of its own.
/// </summary>
public enum ScoreType
{
    Market, Innovation, Feasibility, Scalability, Competition, Risk
}
