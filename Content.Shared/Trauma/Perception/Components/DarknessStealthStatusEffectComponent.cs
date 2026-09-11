// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared.Trauma.Perception.Components;

/// <summary>
/// Status effect that activates stealth when light level drops below a threshold.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(false, true)]
public sealed partial class DarknessStealthStatusEffectComponent : Component
{
    /// <summary>
    /// Light level below which darkness stealth activates.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TriggerAt = 0.25f;

    /// <summary>
    /// Target visibility alpha while concealed in darkness.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Visibility = 0.15f;

    /// <summary>
    /// Whether this effect instance created the target's LightDetection/Stealth components;
    /// removal must not strip ones granted by other sources.
    /// </summary>
    [DataField]
    public bool OwnsLightDetection;

    [DataField]
    public bool OwnsStealth;
}
