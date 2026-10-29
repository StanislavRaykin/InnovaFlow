using System.Collections.Generic;
using Contracts.Ideas;
using Contracts;
using System;

namespace Client.Ideas;

// the idea being built by the create-idea wizard; kept in DI so it survives
// the navigation between /ideas/create and /ideas/create/review
public class IdeaDraft
{
    public static readonly IReadOnlyList<(string Name, string Icon)> Types =
    [
        ("Business", "store"),
        ("Brand", "branding_watermark"),
        ("Startup", "rocket_launch"),
        ("Product", "inventory_2"),
        ("Service", "handshake"),
        ("Application", "apps"),
    ];

    public static readonly IReadOnlyList<string> Industries =
    [
        "Технологии / Носими устройства (Wearables)",
        "Здравеопазване и фитнес",
        "Финанси и финтех",
        "Образование и развитие",
    ];

    // chosen here rather than by the server so every autosave can PUT to the
    // same url, whether or not the draft exists yet
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";
    public string Type { get; set; } = Types[0].Name;
    public string Problem { get; set; } = "";
    public string Industry { get; set; } = Industries[0];
    public string Audience { get; set; } = "";

    // a record, so two snapshots compare equal when every field matches
    public SaveDraftRequest ToRequest() => new(
        Title: Name,
        Description: null,
        ProblemStatement: Problem,
        TargetAudience: Audience,
        Category: Enum.Parse<IdeaCategory>(Type),
        Industry: Industry);

    // the draft's own id, so the server promotes the autosaved draft
    public CreateIdeaRequest ToCreateIdeaRequest() => new CreateIdeaRequest(
    Id: Id,
    Title: Name,
    Description: string.Empty,
    ProblemStatement: Problem,
    TargetAudience: Audience,
    Category: Enum.Parse<IdeaCategory>(Type),
    Industry: Industry);

    // after submitting, so the next run of the wizard starts a new draft
    public void Reset()
    {
        Id = Guid.NewGuid();
        Name = "";
        Type = Types[0].Name;
        Problem = "";
        Industry = Industries[0];
        Audience = "";
    }
}
