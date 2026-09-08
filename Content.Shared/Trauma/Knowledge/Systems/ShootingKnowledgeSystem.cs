// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed partial class ShootingKnowledgeSystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public static readonly EntProtoId ShootingKnowledge = "ShootingKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, GetRecoilModifiersEvent>(OnHolderRecoilModifiers);
        SubscribeLocalEvent<AimSpeedKnowledgeComponent, GetRecoilModifiersEvent>(OnGetRecoilModifiers);
        SubscribeLocalEvent<KnowledgeHolderComponent, AmmoShotUserEvent>(OnAddShootingExperience);
        SubscribeLocalEvent<ProjectileComponent, ProjectileHitEvent>(OnHitShootingExperience);
    }

    private void OnHolderRecoilModifiers(Entity<KnowledgeHolderComponent> ent, ref GetRecoilModifiersEvent args)
    {
        _knowledge.RelayEvent(ent, ref args);

        if (_knowledge.GetContainer(ent.Owner) is not { } container ||
            _knowledge.GetKnowledge(container, ShootingKnowledge) == null)
        {
            args.Modifier *= 2.85f;
        }
    }

    private void OnGetRecoilModifiers(Entity<AimSpeedKnowledgeComponent> ent, ref GetRecoilModifiersEvent args)
    {
        if (args.Gun == args.User)
            return;

        var level = _knowledge.GetLevel(ent.Owner);
        args.Modifier /= ent.Comp.Curve.GetCurve(level);
    }

    private void OnAddShootingExperience(Entity<KnowledgeHolderComponent> ent, ref AmmoShotUserEvent args)
    {
        if (_knowledge.GetContainer(ent.Owner) is not { } brain)
            return;

        _knowledge.AddExperience(brain, ShootingKnowledge, 1, 20);
    }

    private void OnHitShootingExperience(Entity<ProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter || _knowledge.GetContainer(shooter) is not { } brain || !_mobState.IsAlive(args.Target))
            return;

        _knowledge.AddExperience(brain, ShootingKnowledge, 1, 50);
    }
}
