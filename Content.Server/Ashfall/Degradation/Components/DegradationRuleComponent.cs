using Robust.Shared.Prototypes;
using Ashfall.Shared.Degradation;

namespace Ashfall.Server.Degradation.Components;

/// <summary>
/// Selects and starts the station variation passes for a degradation round.
/// </summary>
[RegisterComponent]
public sealed partial class DegradationRuleComponent : Component
{
    [DataField(required: true)]
    public ProtoId<DegradationProfilePrototype> Profile;

    /// <summary>
    /// An optional fixed seed for replaying a scenario. Random when omitted.
    /// </summary>
    [DataField]
    public int? Seed;

    public DegradationScenario? Scenario;

    public readonly List<string> ActivatedFaults = new();
    public readonly List<EntProtoId> ActivatedRules = new();
}
