using Robust.Shared.GameStates;

namespace Content.Shared.Ashfall.Weapons.Ranged.Defects.Components;

/// <summary>
/// When added to an entity with BallisticAmmoProviderComponent, randomizes
/// the loaded round count at MapInit to a fraction of the magazine's capacity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RandomAmmoFillComponent : Component
{
    /// <summary>
    /// Lower bound of the loaded-round fraction (0.0-1.0).
    /// </summary>
    [DataField]
    public float MinFillFraction = 0.35f;

    /// <summary>
    /// Upper bound of the loaded-round fraction (0.0-1.0).
    /// </summary>
    [DataField]
    public float MaxFillFraction = 0.85f;
}
