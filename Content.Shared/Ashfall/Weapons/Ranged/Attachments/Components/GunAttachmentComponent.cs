using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Ashfall.Weapons.Ranged.Attachments.Components;

/// <summary>
/// Marker component for items that can be mounted onto guns as attachments.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunAttachmentComponent : Component
{
    /// <summary>
    /// Sprite specifier to display on the gun when attached.
    /// If null, will fallback to the entity's SpriteComponent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? AttachedSprite;

    /// <summary>
    /// Offset applied to the sprite while it is displayed on a gun.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 AttachedOffset;

    /// <summary>
    /// Rotation applied to the sprite while it is displayed on a gun.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Angle AttachedRotation;

    /// <summary>
    /// Whether this attachment has an unshaded layer (e.g. laser beam/dot) with state "{state}_unshaded".
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasUnshaded;
}
