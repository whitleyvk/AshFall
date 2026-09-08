using Robust.Shared.GameStates;

namespace Content.Shared.Ashfall.Weapons.Ranged.Defects.Components;

/// <summary>
/// Marker component that causes DefectSystem to roll each
/// DefectComponent.Prob at MapInit, removing defects that fail.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RandomDefectsComponent : Component
{
}
