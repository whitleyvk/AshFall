// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared.Trauma.Perception.Components;

/// <summary>
/// Modifies the movement speed of an entity based on ambient/point light levels.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(false, true)]
public sealed partial class LightSpeedModifierComponent : Component
{
    /// <summary>
    /// Required light level for light/shadow state transition.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RequiredLightLevel = 0.25f;

    /// <summary>
    /// Walk speed multiplier when the condition is active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float WalkModifier = 1f;

    /// <summary>
    /// Sprint speed multiplier when the condition is active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SprintModifier = 1f;

    /// <summary>
    /// Whether the entity is currently illuminated in light.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OnLight;

    /// <summary>
    /// If true, speed modifier applies when in shadows (not on light).
    /// If false, speed modifier applies when in light.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ApplyInDarkness = true;
}
