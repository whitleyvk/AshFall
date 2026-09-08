namespace Content.Shared.Ashfall.Weapons.Ranged.Defects.Components;

/// <summary>
/// Abstract base for all defect components on second-hand / worn items.
/// When the entity also has RandomDefectsComponent, DefectSystem
/// rolls Prob at MapInit and removes defects that fail.
/// </summary>
public abstract partial class DefectComponent : Component
{
    /// <summary>
    /// Probability that this defect is present at spawn.
    /// </summary>
    [DataField]
    public float Prob = 1.0f;

    /// <summary>
    /// Localization key appended to the item description by DefectSystem after rolling.
    /// </summary>
    [DataField]
    public LocId DefectLabel = string.Empty;
}
