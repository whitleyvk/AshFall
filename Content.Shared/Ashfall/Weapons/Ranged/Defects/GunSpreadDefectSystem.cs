using Content.Shared.Ashfall.Weapons.Ranged.Defects.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Ashfall.Weapons.Ranged.Defects;

/// <summary>
/// Applies randomised spread to guns with GunSpreadDefectComponent.
/// Angle deltas are computed at MapInit and applied via GunRefreshModifiersEvent,
/// so they compose correctly with attachments and wield bonuses.
/// </summary>
public sealed partial class GunSpreadDefectSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedGunSystem _gunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunSpreadDefectComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(DefectSystem) });
        SubscribeLocalEvent<GunSpreadDefectComponent, GunRefreshModifiersEvent>(OnRefreshModifiers);
    }

    private void OnMapInit(Entity<GunSpreadDefectComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        var def = ent.Comp;

        if (TryComp<GunComponent>(ent.Owner, out var gun))
        {
            if (def.MinAngleMin.HasValue && def.MinAngleMax.HasValue)
            {
                var sampled = Angle.FromDegrees(
                    SampleGaussian((float) def.MinAngleMin.Value.Degrees, (float) def.MinAngleMax.Value.Degrees));
                ent.Comp.MinAngleDelta = sampled - gun.MinAngle;
            }

            if (def.MaxAngleMin.HasValue && def.MaxAngleMax.HasValue)
            {
                var sampled = Angle.FromDegrees(
                    SampleGaussian((float) def.MaxAngleMin.Value.Degrees, (float) def.MaxAngleMax.Value.Degrees));
                ent.Comp.MaxAngleDelta = sampled - gun.MaxAngle;
            }

            _gunSystem.RefreshModifiers((ent.Owner, gun));
        }

        if (def.SpreadMultiplierMin.HasValue && def.SpreadMultiplierMax.HasValue)
        {
            var spreadComp = EnsureComp<GunSpreadModifierComponent>(ent.Owner);
            spreadComp.Spread = SampleGaussian(def.SpreadMultiplierMin.Value, def.SpreadMultiplierMax.Value);
            Dirty(ent.Owner, spreadComp);
        }
    }

    private void OnRefreshModifiers(Entity<GunSpreadDefectComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.MinAngle += ent.Comp.MinAngleDelta;
        args.MaxAngle += ent.Comp.MaxAngleDelta;

        if (args.MinAngle < Angle.Zero)
            args.MinAngle = Angle.Zero;

        if (args.MaxAngle < args.MinAngle)
            args.MaxAngle = args.MinAngle;
    }

    private float SampleGaussian(float min, float max)
    {
        var mean = (min + max) * 0.5f;
        var stdDev = (max - min) * 0.25f;
        return Math.Clamp((float) _random.NextGaussian(mean, stdDev), min, max);
    }
}
