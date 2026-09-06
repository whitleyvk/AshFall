using Robust.Shared.Prototypes;

namespace Ashfall.Shared.Degradation;

/// <summary>
/// Defines the population scaling and compatible fault pool for a degradation scenario.
/// </summary>
[Prototype]
public sealed partial class DegradationProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public int LowPopulationThreshold = 15;

    [DataField(required: true)]
    public int LowPopulationBudget;

    [DataField(required: true)]
    public int StandardBudget;

    [DataField]
    public List<string> RequiredTags = new();

    [DataField(required: true)]
    public List<DegradationFaultEntry> Faults = new();
}

/// <summary>
/// A selectable station variation pass and its scenario-building constraints.
/// </summary>
[DataDefinition]
public sealed partial class DegradationFaultEntry
{
    [DataField(required: true)]
    public string Id = default!;

    [DataField(required: true)]
    public EntProtoId Rule;

    [DataField]
    public int Cost = 1;

    [DataField]
    public float Weight = 1f;

    [DataField]
    public int MinPlayers;

    [DataField]
    public int? MaxPlayers;

    [DataField]
    public HashSet<string> Tags = new();

    [DataField]
    public HashSet<string> IncompatibleWith = new();
}
