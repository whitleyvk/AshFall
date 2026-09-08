using Content.Shared.Ashfall.Weapons.Ranged.Defects.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Ashfall.Weapons.Ranged.Defects;

/// <summary>
/// Randomizes loaded ammo count for magazines with RandomAmmoFillComponent.
/// Runs after SharedGunSystem so it overrides the default full UnspawnedCount.
/// </summary>
public sealed partial class RandomAmmoFillSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedGunSystem _gunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomAmmoFillComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(SharedGunSystem) });
    }

    private void OnMapInit(Entity<RandomAmmoFillComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<BallisticAmmoProviderComponent>(ent.Owner, out var ballistic))
            return;

        var min = Math.Clamp((int) MathF.Round(ballistic.Capacity * ent.Comp.MinFillFraction), 0, ballistic.Capacity);
        var max = Math.Clamp((int) MathF.Round(ballistic.Capacity * ent.Comp.MaxFillFraction), 0, ballistic.Capacity);
        if (min > max)
            min = max;

        var count = _random.Next(min, max + 1);
        _gunSystem.SetBallisticUnspawned((ent.Owner, ballistic), count);
    }
}
