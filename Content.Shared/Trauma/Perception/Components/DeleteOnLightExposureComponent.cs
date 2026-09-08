// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Trauma.Perception.Components;

/// <summary>
/// Deletes an entity after continuous exposure to light for a specific duration.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(false, true), AutoGenerateComponentPause]
public sealed partial class DeleteOnLightExposureComponent : Component
{
    /// <summary>
    /// Minimum light level required to trigger the deletion countdown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float LightLevel = 0.5f;

    /// <summary>
    /// Whether the entity is currently active and counting down on light.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// How long the entity must remain on light before being deleted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5.0);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpiryTime = TimeSpan.Zero;
}
