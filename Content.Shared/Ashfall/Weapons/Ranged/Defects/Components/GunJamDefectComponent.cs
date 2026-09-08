using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Ashfall.Weapons.Ranged.Defects.Components;

/// <summary>
/// Gives a gun a per-shot chance to jam. A jammed gun cannot fire until the player
/// racks the action (Z / Use In Hand), which clears the jam.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunJamDefectComponent : DefectComponent
{
    public GunJamDefectComponent()
    {
        Prob = 0.65f;
        DefectLabel = "defect-label-damaged-bolt";
    }

    /// <summary>
    /// Whether the gun is currently jammed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsJammed;

    /// <summary>
    /// Per-shot probability of jamming after a successful shot.
    /// </summary>
    [DataField]
    public float JamChance = 0.06f;

    /// <summary>
    /// Sound to play when the gun jams.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundJamRack = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/smg_cock.ogg");

    /// <summary>
    /// Minimum time between jam warning popups to prevent spam.
    /// </summary>
    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(1.0);

    public TimeSpan NextPopupTime;
}
