using Content.Medical.Common.Body;
using Content.Shared.Body.Components;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Pointing;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public sealed partial class BrainSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mindSystem = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainComponent, OrganGotInsertedEvent>(OnBrainInserted);
        SubscribeLocalEvent<BrainComponent, OrganGotRemovedEvent>(OnBrainRemoved);
        SubscribeLocalEvent<BrainComponent, PointAttemptEvent>(OnPointAttempt);
    }

    private void OnBrainInserted(EntityUid uid, BrainComponent comp, ref OrganGotInsertedEvent args)
    {
        HandleMind(args.Target, uid);

        if (!_timing.ApplyingState && !TerminatingOrDeleted(args.Target))
            RemComp<DebrainedComponent>(args.Target);
    }

    private void OnBrainRemoved(EntityUid uid, BrainComponent comp, ref OrganGotRemovedEvent args)
    {
        HandleMind(uid, args.Target);

        if (!_timing.ApplyingState && !TerminatingOrDeleted(args.Target))
            EnsureComp<DebrainedComponent>(args.Target);
    }

    private void HandleMind(EntityUid newEntity, EntityUid oldEntity)
    {
        if (TerminatingOrDeleted(newEntity) || TerminatingOrDeleted(oldEntity))
            return;

        EnsureComp<MindContainerComponent>(newEntity);
        EnsureComp<MindContainerComponent>(oldEntity);

        var ghostOnMove = EnsureComp<GhostOnMoveComponent>(newEntity);
        ghostOnMove.MustBeDead = HasComp<MobStateComponent>(newEntity); // Don't ghost living players out of their bodies.

        if (!_mindSystem.TryGetMind(oldEntity, out var mindId, out var mind))
            return;

        _mindSystem.TransferTo(mindId, newEntity, mind: mind);
    }

    private void OnPointAttempt(Entity<BrainComponent> ent, ref PointAttemptEvent args)
    {
        args.Cancel();
    }
}
