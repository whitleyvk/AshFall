using Content.Shared.Ashfall.Particles;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Ashfall.Particles.Visuals;

public sealed partial class GunMuzzleParticleSystem : EntitySystem
{
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<ParticleEffectPrototype> SmokeEffect = "AshfallMuzzleSmoke";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<MuzzleFlashEvent>(OnMuzzleFlash);
    }

    private void OnMuzzleFlash(MuzzleFlashEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var gunUid = GetEntity(args.Uid);
        if (!gunUid.IsValid())
            return;

        var root = gunUid;
        if (_container.TryGetContainingContainer(gunUid, out var container))
            root = container.Owner;

        var coords = _transform.GetMapCoordinates(root);
        var emitAngle = -args.Angle + Angle.FromDegrees(90);

        _particles.SpawnEffect(
            SmokeEffect,
            coords,
            root,
            overrides: new ParticleRuntimeOverrides { EmitAngle = emitAngle });
    }
}
