using Content.Medical.Common.Body;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Wounds;
using Content.Shared.Ashfall.Weapons.Melee;
using Content.Shared.Body;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Ashfall.Weapons.Melee;

public sealed partial class ChainsawDismemberSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private WoundSystem _wounds = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private EntityQuery<BodyPartComponent> _partQuery = default!;
    [Dependency] private EntityQuery<ChildOrganComponent> _childQuery = default!;
    [Dependency] private EntityQuery<WoundableComponent> _woundableQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChainsawDismemberComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<ChainsawDismemberComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        var user = args.User;
        if (!TryComp<TargetingComponent>(user, out var targeting))
            return;

        var targetLimb = targeting.Target;
        if (targetLimb == TargetBodyPart.Chest || targetLimb == TargetBodyPart.Groin)
            return; // Cannot saw off torso

        var (partType, symmetry) = _body.ConvertTargetBodyPart(targetLimb);

        foreach (var hitEntity in args.HitEntities)
        {
            if (!HasComp<BodyComponent>(hitEntity))
                continue;

            var isIncap = _mobState.IsIncapacitated(hitEntity) || _standing.IsDown(hitEntity);
            var chance = isIncap ? ent.Comp.CritChance : ent.Comp.NormalChance;

            if (!_random.Prob(chance))
                continue;

            // Find matching body part on victim
            var externalOrgans = _body.GetExternalOrgans(hitEntity);
            EntityUid? targetPart = null;

            foreach (var organ in externalOrgans)
            {
                if (!_partQuery.TryComp(organ, out var partComp))
                    continue;

                if (partComp.PartType == partType && (symmetry == BodyPartSymmetry.None || partComp.Symmetry == symmetry))
                {
                    targetPart = organ;
                    break;
                }
            }

            if (targetPart == null)
                continue;

            if (!_childQuery.TryComp(targetPart.Value, out var childComp) || childComp.Parent == null)
                continue;

            var parent = childComp.Parent.Value;

            if (!_woundableQuery.TryComp(targetPart.Value, out var partWoundable) ||
                !_woundableQuery.TryComp(parent, out var parentWoundable))
                continue;

            // Delimb!
            if (_wounds.AmputateWoundable((parent, parentWoundable), (targetPart.Value, partWoundable), user, crude: true))
            {
                _audio.PlayPvs("/Audio/Weapons/chainsaw_rev.ogg", hitEntity);
                break; // One limb per swing
            }
        }
    }
}
