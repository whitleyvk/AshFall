using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Ashfall.Footprints;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FootprintComponent : Component
{
    /// <summary>
    /// Color of the substance currently sticking to feet/shoes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color? PrintColor;

    /// <summary>
    /// How many footsteps remain before feet/shoes are clean.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int StepCount = 0;

    /// <summary>
    /// Maximum steps made after stepping into puddle/dirt.
    /// </summary>
    [DataField]
    public int MaxSteps = 8;

    /// <summary>
    /// Decal prototype ID used for the footprint.
    /// </summary>
    [DataField]
    public string DecalId = "footprint";

    /// <summary>
    /// Perpendicular offset distance for alternating left/right feet.
    /// </summary>
    [DataField]
    public float FootOffset = 0.15f;

    /// <summary>
    /// Alternating foot tracking.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RightFoot;

    /// <summary>
    /// How long spawned footprints should persist before fading away.
    /// </summary>
    [DataField]
    public TimeSpan Lifetime = TimeSpan.FromSeconds(75);
}
