using Robust.Shared.GameStates;

namespace Content.Shared.Ashfall.Carrying;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BeingCarriedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Carrier;
}
