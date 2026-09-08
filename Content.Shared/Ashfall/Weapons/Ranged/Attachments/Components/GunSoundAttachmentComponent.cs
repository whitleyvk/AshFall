using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Ashfall.Weapons.Ranged.Attachments.Components;

/// <summary>
/// An attachment component that alters the gunshot sound (e.g. silencer/suppressor).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GunSoundAttachmentComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier? Sound;
}
