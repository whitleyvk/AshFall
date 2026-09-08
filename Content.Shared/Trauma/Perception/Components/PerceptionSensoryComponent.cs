// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared.Trauma.Perception.Components;

/// <summary>
/// Configures an observer's sensory capability to perceive entities in shadows and darkness.
/// Can be granted by traits, mutations, cybernetics, or gear.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(false, true)]
public sealed partial class PerceptionSensoryComponent : Component
{
    /// <summary>
    /// If true, the observer can see through darkness stealth as if fully lit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NightVision;

    /// <summary>
    /// Level from 0 to 1 indicating darksight capability, reducing shadow concealment penalties.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DarkSightLevel;

    /// <summary>
    /// Radius around the observer within which shadowed entities are revealed even in deep darkness.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ProximityRevealRadius = 1.75f;

    /// <summary>
    /// Maximum perception distance for resolving stealth targets.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PerceptionRange = 15f;
}
