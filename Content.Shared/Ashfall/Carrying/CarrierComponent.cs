using Robust.Shared.GameStates;

namespace Content.Shared.Ashfall.Carrying;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CarrierComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Carried;

    [DataField, AutoNetworkedField]
    public EntityUid? VirtualItem;

    [DataField, AutoNetworkedField]
    public float SpeedModifier = 0.75f;
}
