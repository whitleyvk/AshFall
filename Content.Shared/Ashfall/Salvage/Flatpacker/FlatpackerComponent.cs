using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ashfall.Salvage.Flatpacker;

[RegisterComponent, NetworkedComponent]
public sealed partial class FlatpackerComponent : Component
{
    /// <summary>
    /// Duration of packing process in seconds.
    /// </summary>
    [DataField]
    public float PackDelay = 4.5f;

    /// <summary>
    /// Sound played when packing a machine into a flatpack.
    /// </summary>
    [DataField]
    public SoundSpecifier PackSound = new SoundPathSpecifier("/Audio/Machines/hydraulic_1.ogg");

    /// <summary>
    /// Prototype ID of the flatpack crate entity spawned.
    /// </summary>
    [DataField]
    public EntProtoId FlatpackPrototype = "AshfallFlatpack";
}
