using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    public void SetAvailableModes(Entity<GunComponent> gun, SelectiveFire available, SelectiveFire? selected = null)
    {
        gun.Comp.AvailableModes = available;
        gun.Comp.SelectedMode = selected ?? ((available & gun.Comp.SelectedMode) != 0 ? gun.Comp.SelectedMode : available);
        Dirty(gun);
    }
}
