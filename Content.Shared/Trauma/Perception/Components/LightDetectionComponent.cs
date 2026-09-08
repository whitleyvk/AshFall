// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared.Trauma.Perception.Components;

/// <summary>
/// Tracks the ambient and point light levels reaching an entity for perception and stealth calculations.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(false, true)]
public sealed partial class LightDetectionComponent : Component
{
    /// <summary>
    /// Current calculated light level reaching this entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float CurrentLightLevel;

    /// <summary>
    /// Minimum light level for entity to be considered in light rather than shadow.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float OnLightLevel = 0.25f;

    /// <summary>
    /// Whether the entity is currently illuminated in light.
    /// </summary>
    public bool OnLight => CurrentLightLevel > OnLightLevel;

    /// <summary>
    /// Whether the entity is currently in shadow/darkness.
    /// </summary>
    public bool InShadow => CurrentLightLevel <= OnLightLevel;
}
