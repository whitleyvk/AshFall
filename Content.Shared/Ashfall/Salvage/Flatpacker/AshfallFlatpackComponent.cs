using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Ashfall.Salvage.Flatpacker;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AshfallFlatpackComponent : Component
{
    [DataField, AutoNetworkedField]
    public string SlotId = "packed_machine";

    [DataField, AutoNetworkedField]
    public float UnpackDelay = 2.5f;

    [DataField, AutoNetworkedField]
    public string PackedName = string.Empty;

    [DataField]
    public SoundSpecifier UnpackSound = new SoundPathSpecifier("/Audio/Machines/boltsup.ogg");
}
