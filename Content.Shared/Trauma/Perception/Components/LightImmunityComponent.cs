// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Trauma.Perception.Components;

/// <summary>
/// Grants temporary immunity from light detection damage and light exposure deletion.
/// Useful for newly spawned dark entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(false, true), AutoGenerateComponentPause]
public sealed partial class LightImmunityComponent : Component
{
    /// <summary>
    /// How long the light immunity lasts from initialization.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10.0);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}
