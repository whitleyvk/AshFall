using Robust.Shared.GameStates;

namespace Content.Shared.Ashfall.Weapons.Ranged.Defects.Components;

/// <summary>
/// Forces the gun into semi-automatic mode at spawn by overwriting AvailableModes
/// and SelectedMode on GunComponent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BentSwitchDefectComponent : DefectComponent
{
    public BentSwitchDefectComponent()
    {
        Prob = 0.25f;
        DefectLabel = "defect-label-bent-switch";
    }
}
