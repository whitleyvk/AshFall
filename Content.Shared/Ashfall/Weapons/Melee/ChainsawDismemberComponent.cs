using Robust.Shared.GameStates;

namespace Content.Shared.Ashfall.Weapons.Melee;

/// <summary>
/// Allows a melee weapon (such as a chainsaw) to dismember targeted body parts,
/// with increased probability on incapacitated or dead targets.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChainsawDismemberComponent : Component
{
    /// <summary>
    /// Base chance per hit to dismember when target is conscious and standing.
    /// </summary>
    [DataField("normalChance")]
    public float NormalChance = 0.15f;

    /// <summary>
    /// Chance per hit to dismember when target is in crit, dead, or down.
    /// </summary>
    [DataField("critChance")]
    public float CritChance = 0.70f;
}
