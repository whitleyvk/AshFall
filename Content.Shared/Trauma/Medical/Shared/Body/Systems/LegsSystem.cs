using Content.Shared.Body;
using Content.Shared.Containers;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Traits.Assorted;
using Content.Trauma.Common.Movement;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Medical.Shared.Body;

// TODO: prevent standing if your bones are broken
public sealed partial class LegsSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    private EntityQuery<LegsComponent> _query;
    private EntityQuery<LegComponent> _legQuery;

    public override void Initialize()
    {
        base.Initialize();

        _query = GetEntityQuery<LegsComponent>();
        _legQuery = GetEntityQuery<LegComponent>();

        SubscribeLocalEvent<LegsComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<LegsComponent, FootStepEvent>(OnFootStep);
        SubscribeLocalEvent<LegsComponent, AttemptStopPullingEvent>(OnAttemptStopPulling);

        SubscribeLocalEvent<LegComponent, OrganGotInsertedEvent>(OnLegAdded);
        SubscribeLocalEvent<LegComponent, OrganGotRemovedEvent>(OnLegRemoved);
        SubscribeLocalEvent<LegsParalyzedComponent, MapInitEvent>(OnParalyzedInit,
            after: [ typeof(InitialBodySystem) ]); // run after the organs are added
        SubscribeLocalEvent<LegsParalyzedComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
        SubscribeLocalEvent<LegsParalyzedComponent, StandUpAttemptEvent>(OnParalyzedStandAttempt);
        SubscribeLocalEvent<LegsParalyzedComponent, MoveEvent>(OnMove);
    }

    private void OnStandAttempt(Entity<LegsComponent> ent, ref StandAttemptEvent args)
    {
        if (ent.Comp.Legs.Count == 0)
            args.Cancel();
    }

    private void OnAttemptStopPulling(Entity<LegsComponent> ent, ref AttemptStopPullingEvent args) // Goobstation
    {
        if (args.User is not {} user || !Exists(user))
            return;

        if (user != ent.Owner)
            return;

        if (ent.Comp.Legs.Count > 0 || ent.Comp.Required == 0)
            return;

        args.Cancelled = true;
    }

    private void OnLegAdded(Entity<LegComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (!_query.TryComp(args.Target, out var comp))
            return;

        comp.Legs.Add(ent.Owner);
        Dirty(args.Target, comp);

        UpdateMovementSpeed((args.Target, comp));
    }

    private void OnLegRemoved(Entity<LegComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Target) || !_query.TryComp(args.Target, out var comp))
            return;

        comp.Legs.Remove(ent.Owner);
        Dirty(args.Target, comp);

        UpdateMovementSpeed((args.Target, comp));

        if (comp.Legs.Count == 0 && !_standing.IsDown(args.Target))
        {
            _standing.Down(args.Target);
        }
    }

    private void OnFootStep(Entity<LegsComponent> ent, ref FootStepEvent args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Legs.Count != 1)
            return;

        if (_standing.IsDown(ent.Owner))
            return;

        var isWalking = false;
        if (TryComp<InputMoverComponent>(ent.Owner, out var mover))
        {
            isWalking = (mover.HeldMoveButtons & MoveButtons.Walk) != MoveButtons.None;
        }

        var stumbleChance = isWalking ? 0.08f : 0.28f;
        if (_random.Prob(stumbleChance))
        {
            _stun.TryKnockdown(ent.Owner, TimeSpan.FromSeconds(1.2), drop: false, force: true);
            _popup.PopupEntity(Loc.GetString("legs-stumble-single-leg"), ent.Owner, ent.Owner);
        }
    }

    private void OnParalyzedInit(Entity<LegsParalyzedComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.Permanent)
            return;

        if (!_query.TryComp(ent, out var legs))
            return;
        foreach (var leg in legs.Legs)
        {
            RemComp<LegComponent>(leg);
        }
        legs.Legs.Clear();
        Dirty(ent, legs);
        UpdateMovementSpeed((ent, legs));
    }

    public void UpdateMovementSpeed(Entity<LegsComponent> ent)
    {
        var walkSpeed = 0f;
        var sprintSpeed = 0f;
        var acceleration = 0f;
        foreach (var leg in ent.Comp.Legs)
        {
            if (!_legQuery.TryComp(leg, out var comp))
                continue;

            walkSpeed += comp.WalkSpeed;
            sprintSpeed += comp.SprintSpeed;
            acceleration += comp.Acceleration;
        }

        // bare minimum speeds for crawling if you have no legs
        // could make it need arms too, but torsolo...
        walkSpeed = Math.Max(walkSpeed, 1f);
        sprintSpeed = Math.Max(sprintSpeed, 1f);
        acceleration = Math.Max(acceleration, 0.5f);

        // missing a leg makes you move at half speed
        // somehow having 3+ legs makes you fast
        var scale = 1f / ent.Comp.Required;
        walkSpeed *= scale;
        sprintSpeed *= scale;
        acceleration *= scale;
        _movement.ChangeBaseSpeed(ent.Owner, walkSpeed, sprintSpeed, acceleration);
    }

    private void OnMove(Entity<LegsParalyzedComponent> ent, ref MoveEvent args)
    {
        EnsureComp<KnockedDownComponent>(ent);
    }

    private void OnParalyzedStandAttempt(Entity<LegsParalyzedComponent> ent, ref StandUpAttemptEvent args)
    {
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancelled = true;
    }

    private void OnRefresh(Entity<LegsParalyzedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Permanent)
            return;

        args.ModifySpeed(ent.Comp.WalkSpeedModifier, ent.Comp.SprintSpeedModifier);
    }
}
