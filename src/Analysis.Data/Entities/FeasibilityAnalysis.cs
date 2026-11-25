namespace InnovaFlow.Analysis.Data;

public class FeasibilityAnalysis
{
    public Guid Id { get; set; }
    public Guid AnalysisId { get; set; }
    public Analysis Analysis { get; set; } = null!;

    public string? Summary { get; set; }

    public decimal TechnicalScore { get; set; }
    public decimal ResourceScore { get; set; }
    public decimal ComplexityScore { get; set; }
    public decimal ScalabilityScore { get; set; }

    public List<FeasibilityTechnology> Technologies { get; set; } = [];
    public List<FeasibilityResource> Resources { get; set; } = [];
    public List<FeasibilityChallenge> Challenges { get; set; } = [];
}

public class FeasibilityTechnology
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public TechnologyStatus Status { get; set; }
}

public class FeasibilityResource
{
    public string Name { get; set; } = null!;
    public SeverityLevel Level { get; set; }
    public int Intensity { get; set; }
}

public class FeasibilityChallenge
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public SeverityLevel Severity { get; set; }
    public ChallengeCategory Category { get; set; }
}

public enum TechnologyStatus { Available, NeedsWork }
public enum SeverityLevel { Low, Medium, High, Critical }
public enum ChallengeCategory { Technical, Hardware, Energy, Durability, Operational, Financial, Legal, Market, Other }
