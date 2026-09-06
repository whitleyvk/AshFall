using Robust.Shared.Prototypes;

namespace Ashfall.Server.Degradation.Components;

/// <summary>
/// Runtime manifest of the degradation scenario applied to a station.
/// Physical world state remains authoritative for whether individual faults are repaired.
/// </summary>
[RegisterComponent]
public sealed partial class DegradationStateComponent : Component
{
    public string Profile = string.Empty;
    public int Seed;
    public int Budget;
    public int ReadyPlayers;
    public List<string> Faults = new();
    public List<EntProtoId> Rules = new();
}
