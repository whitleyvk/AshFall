using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared.Ashfall.Weapons.Ranged.Defects.Components;

/// <summary>
/// Randomizes a gun's spread at MapInit via GunRefreshModifiersEvent.
/// Sampled angle deltas are added on top of the gun's base angles so they
/// compose correctly with other modifiers (e.g. GunWieldBonus or attachments).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunSpreadDefectComponent : DefectComponent
{
    public GunSpreadDefectComponent()
    {
        Prob = 0.7f;
        DefectLabel = "defect-label-warped-barrel";
    }

    [DataField] public Angle? MinAngleMin;
    [DataField] public Angle? MinAngleMax;
    [DataField] public Angle? MaxAngleMin;
    [DataField] public Angle? MaxAngleMax;

    [DataField, AutoNetworkedField] public Angle MinAngleDelta;
    [DataField, AutoNetworkedField] public Angle MaxAngleDelta;

    [DataField] public float? SpreadMultiplierMin;
    [DataField] public float? SpreadMultiplierMax;
}
