// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Medical.CPR;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedCPRSystem))]
[AutoGenerateComponentState]
public sealed partial class ActiveCPRComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Performer;

    [DataField]
    public EntityUid? Sound;
}
