using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Ashfall.Weapons.Ranged.Attachments.Components;

/// <summary>
/// Used to hold data for guns which can have attachments mounted onto them.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AttachableGunComponent : Component
{
    /// <summary>
    /// The slots that can have attachments mounted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<GunAttachmentSlot> Slots = new();
}

[Serializable, NetSerializable]
[DataDefinition]
public partial record struct GunAttachmentSlot
{
    /// <summary>
    /// The localized name of this slot (e.g. muzzle, underbarrel, optic).
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Container associated with the slot where the item is stored.
    /// </summary>
    [DataField(required: true)]
    public string ContainerId;

    /// <summary>
    /// Whitelist used to define which attachments can be mounted in this slot.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();
}
