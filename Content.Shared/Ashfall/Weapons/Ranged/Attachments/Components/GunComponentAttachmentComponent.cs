using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ashfall.Weapons.Ranged.Attachments.Components;

/// <summary>
/// An attachment component that dynamically adds/removes components to/from the weapon (e.g. flashlight/laser).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GunComponentAttachmentComponent : Component
{
    [DataField("component", required: true)]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();
}
