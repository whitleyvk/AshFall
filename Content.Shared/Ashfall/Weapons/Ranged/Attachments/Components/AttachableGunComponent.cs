using System.Numerics;
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

    /// <summary>
    /// Per-gun visual overrides, keyed by the slot container ID.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, GunAttachmentVisual> Visuals = new();
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

[Serializable, NetSerializable]
[DataDefinition]
public partial record struct GunAttachmentVisual
{
    /// <summary>
    /// Offset applied to every attachment mounted in this slot on this gun.
    /// </summary>
    [DataField]
    public Vector2 Offset = Vector2.Zero;

    /// <summary>
    /// Rotation applied to every attachment mounted in this slot on this gun.
    /// </summary>
    [DataField]
    public Angle Rotation = Angle.Zero;

    /// <summary>
    /// Sprite layer map key used as the insertion anchor. "top" and "bottom" are special values.
    /// </summary>
    [DataField]
    public string LayerAnchor = "top";

    /// <summary>
    /// Whether the attachment is inserted before or after the anchor layer.
    /// </summary>
    [DataField]
    public GunAttachmentLayerPosition LayerPosition = GunAttachmentLayerPosition.After;

    /// <summary>
    /// Ordering for attachments which share the same anchor and position.
    /// </summary>
    [DataField]
    public int DrawOrder;
}

[Serializable, NetSerializable]
public enum GunAttachmentLayerPosition : byte
{
    Before,
    After,
}
