namespace Client.Views;

public sealed class AnalysisStatusView
{
    public Guid Id { get; set; }
    public int Total { get; set; }
    public int Done { get; set; }
    public List<ModuleView> Modules { get; set; } = [];
}

public sealed class ModuleView
{
    public string Key { get; set; } = "";         
    public string Title { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string? Label { get; set; }             // client only: "ОЦЕНЯВАНЕ…"
}
