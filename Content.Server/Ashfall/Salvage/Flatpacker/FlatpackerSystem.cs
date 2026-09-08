using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Shared.Ashfall.Salvage.Flatpacker;

namespace Content.Server.Ashfall.Salvage.Flatpacker;

public sealed partial class FlatpackerSystem : SharedFlatpackerSystem
{
    protected override bool IsServerPackable(EntityUid target)
    {
        return HasComp<MachineComponent>(target) ||
               HasComp<ApcComponent>(target) ||
               HasComp<PowerNetworkBatteryComponent>(target);
    }
}
