using Robust.Shared.GameStates;

namespace Content.Shared.Ashfall.Weapons.Ranged.Defects.Components;

/// <summary>
/// Gives a gun a per-shot chance to backfire, triggering a small explosion at the
/// weapon's location affecting the shooter's tile.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BackfireDefectComponent : DefectComponent
{
    public BackfireDefectComponent()
    {
        Prob = 0.20f;
        DefectLabel = "defect-label-cracked-chamber";
    }

    /// <summary>
    /// Per-shot probability of a backfire occurring.
    /// </summary>
    [DataField]
    public float BackfireChance = 0.04f;

    [DataField]
    public string ExplosionTypeId = "Default";

    [DataField]
    public float TotalIntensity = 1.5f;

    [DataField]
    public float MaxIntensity = 1.0f;

    [DataField]
    public float IntensitySlope = 8.0f;
}
