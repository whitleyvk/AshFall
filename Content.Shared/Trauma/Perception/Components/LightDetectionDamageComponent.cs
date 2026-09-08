// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Trauma.Perception.Components;

/// <summary>
/// Controls damage taken on light and healing gained in shadows.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(false, true), AutoGenerateComponentPause]
public sealed partial class LightDetectionDamageComponent : Component
{
    /// <summary>
    /// Current detection / health balance value. At 0 or below, light damage occurs.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float DetectionValue = 10f;

    /// <summary>
    /// Maximum detection value.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DetectionValueMax = 10f;

    /// <summary>
    /// Detection value regeneration per tick in darkness.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DetectionValueRegeneration = 0.5f;

    /// <summary>
    /// Whether to take damage when detection value is depleted in light.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TakeDamageOnLight = true;

    /// <summary>
    /// Whether to heal when in shadows.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HealOnShadows = true;

    /// <summary>
    /// Damage resistance multiplier against light damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ResistanceModifier = 1.0f;

    /// <summary>
    /// Damage dealt when exposed to bright light.
    /// </summary>
    [DataField]
    public DamageSpecifier DamageToDeal = new()
    {
        DamageDict = new()
        {
            ["Heat"] = 10,
        }
    };

    /// <summary>
    /// Damage healed when recovering in shadows.
    /// </summary>
    [DataField]
    public DamageSpecifier DamageToHeal = new()
    {
        DamageDict = new()
        {
            ["Heat"] = -5,
            ["Blunt"] = -5,
            ["Slash"] = -5,
            ["Piercing"] = -5,
        }
    };

    [DataField]
    public ProtoId<AlertPrototype>? AlertProto;

    [DataField]
    public int AlertMaxSeverity = 9;

    [DataField]
    public SoundSpecifier? SoundOnDamage = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/energy_meat1.ogg");

    [DataField, AutoNetworkedField]
    public bool ShowAlert;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1.0);
}
