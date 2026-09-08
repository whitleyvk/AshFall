using Robust.Shared.GameStates;

namespace Content.Shared.Ashfall.Weapons.Ranged.Attachments.Components;

/// <summary>
/// An attachment component that alters weapon recoil and spread (e.g. foregrips).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GunRecoilAttachmentComponent : Component
{
    [DataField]
    public float RecoilRecoveryModifier = 1.0f;

    [DataField]
    public float RecoilIncreaseModifier = 1.0f;

    [DataField]
    public float MinSpreadModifier = 1.0f;

    [DataField]
    public float MaxSpreadModifier = 1.0f;
}
