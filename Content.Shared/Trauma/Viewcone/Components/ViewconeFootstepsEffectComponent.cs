using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Viewcone;

/// <summary>
/// Spawns a visual effect shown outside your vision cone when this entity makes a footstep sound.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ViewconeFootstepsEffectComponent : Component
{
    [DataField]
    public EntProtoId Effect = "ViewconeEffectFootstep";
}
