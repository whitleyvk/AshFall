// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Trauma.Perception.Components;

/// <summary>
/// Provides dynamic visibility reduction based on ambient lighting and shadow coverage.
/// Enables stealth opportunities in dark areas and deep shadows.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class PerceptionStealthComponent : Component
{
    /// <summary>
    /// Whether shadow stealth is currently active on this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Minimum sprite visibility alpha when in pitch darkness.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinVisibility = 0.15f;

    /// <summary>
    /// Maximum sprite visibility alpha when fully illuminated in light.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxVisibility = 1.0f;

    /// <summary>
    /// Light level threshold at or below which visibility reaches MinVisibility.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LightThresholdLow = 0.10f;

    /// <summary>
    /// Light level threshold at or above which visibility reaches MaxVisibility.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LightThresholdHigh = 0.45f;

    /// <summary>
    /// Current calculated base visibility alpha from lighting.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float CurrentVisibility = 1.0f;

    /// <summary>
    /// Visibility penalty added when running or sprinting while in shadows.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RunningVisibilityPenalty = 0.35f;

    /// <summary>
    /// Whether attacking an entity reveals the stealth concealment temporarily.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RevealOnAttack = true;

    /// <summary>
    /// Whether taking damage reveals the stealth concealment temporarily.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RevealOnDamage = true;

    /// <summary>
    /// Minimum damage required to trigger reveal on damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DamageRevealThreshold = 5.0f;

    /// <summary>
    /// How long the entity remains revealed after attacking or taking damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RevealDuration = TimeSpan.FromSeconds(3.0);

    /// <summary>
    /// Time until which the entity remains revealed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan RevealedUntil = TimeSpan.Zero;

    /// <summary>
    /// Modifier key used when registering visibility with SpriteVisibilitySystem.
    /// </summary>
    [DataField]
    public string StealthKey = "PerceptionStealth";

    public bool IsRevealed(TimeSpan curTime) => curTime < RevealedUntil;
}
