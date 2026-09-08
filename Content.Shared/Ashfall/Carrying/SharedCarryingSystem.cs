using System.Numerics;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;

namespace Content.Shared.Ashfall.Carrying;

public abstract partial class SharedCarryingSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CarriableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<CarrierComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<CarrierComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<CarrierComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<CarrierComponent, StunnedEvent>(OnStunned);
        SubscribeLocalEvent<CarrierComponent, KnockedDownEvent>(OnKnockedDown);
        SubscribeLocalEvent<CarrierComponent, EntityTerminatingEvent>(OnCarrierTerminating);

        SubscribeLocalEvent<BeingCarriedComponent, UpdateCanMoveEvent>(OnBeingCarriedMoveAttempt);
        SubscribeLocalEvent<BeingCarriedComponent, StandAttemptEvent>(OnBeingCarriedStandAttempt);
        SubscribeLocalEvent<BeingCarriedComponent, EntityTerminatingEvent>(OnBeingCarriedTerminating);
        SubscribeLocalEvent<BeingCarriedComponent, GetVerbsEvent<AlternativeVerb>>(OnBeingCarriedGetVerbs);
    }

    private void OnBeingCarriedGetVerbs(EntityUid uid, BeingCarriedComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.User != uid)
            return;

        if (!component.Carrier.IsValid())
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("ashfall-verb-carry-get-down"),
            Act = () => DropCarried(component.Carrier),
            Priority = 2,
        });
    }

    private void OnGetVerbs(EntityUid uid, CarriableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // If user is already carrying this target: show drop verb
        if (TryComp<CarrierComponent>(args.User, out var carrierComp) && carrierComp.Carried == uid)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("ashfall-verb-carry-drop"),
                IconEntity = GetNetEntity(uid),
                Act = () => DropCarried(args.User, carrierComp),
                Priority = 2,
            });
            return;
        }

        // If user is not carrying anyone and can carry target
        if (carrierComp == null && CanCarry(args.User, uid))
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("ashfall-verb-carry-fireman"),
                IconEntity = GetNetEntity(uid),
                Act = () => TryCarry(args.User, uid),
                Priority = 2,
            });
        }
    }

    public bool CanCarry(EntityUid carrier, EntityUid target)
    {
        if (carrier == target)
            return false;

        if (HasComp<CarrierComponent>(carrier) || HasComp<BeingCarriedComponent>(carrier))
            return false;

        if (HasComp<BeingCarriedComponent>(target) || HasComp<CarrierComponent>(target))
            return false;

        if (!TryComp<HandsComponent>(carrier, out var hands) || _hands.CountFreeHands((carrier, hands)) == 0)
            return false;

        if (!_interaction.InRangeUnobstructed(carrier, target))
            return false;

        var xform = Transform(target);
        if (xform.Anchored)
            return false;

        return true;
    }

    public bool TryCarry(EntityUid carrier, EntityUid target)
    {
        if (!CanCarry(carrier, target))
            return false;

        // Stop pulling target if we were pulling it
        if (TryComp<PullerComponent>(carrier, out var puller) && puller.Pulling == target)
        {
            _pulling.TryStopPull(target, Comp<PullableComponent>(target));
        }

        // Occupy one hand with virtual item
        if (!_virtualItem.TrySpawnVirtualItemInHand(target, carrier, out var virtualItem))
            return false;

        var carrierComp = EnsureComp<CarrierComponent>(carrier);
        carrierComp.Carried = target;
        carrierComp.VirtualItem = virtualItem;
        Dirty(carrier, carrierComp);

        var beingCarried = EnsureComp<BeingCarriedComponent>(target);
        beingCarried.Carrier = carrier;
        Dirty(target, beingCarried);

        _standing.Down(target, false);
        _transform.SetParent(target, carrier);
        _transform.SetLocalPosition(target, new Vector2(0f, 0.25f));

        _movementSpeed.RefreshMovementSpeedModifiers(carrier);

        _popup.PopupEntity(Loc.GetString("ashfall-carry-started-carrier", ("target", target)), carrier, carrier);
        _popup.PopupEntity(Loc.GetString("ashfall-carry-started-target", ("carrier", carrier)), target, target);

        return true;
    }

    public void DropCarried(EntityUid carrier, CarrierComponent? carrierComp = null)
    {
        if (!Resolve(carrier, ref carrierComp, false) || carrierComp.Carried is not { } carried)
            return;

        if (carried.IsValid() && TryComp<BeingCarriedComponent>(carried, out var beingCarried) && beingCarried.Carrier == carrier)
        {
            RemComp<BeingCarriedComponent>(carried);
            _transform.AttachToGridOrMap(carried);
        }

        if (carrierComp.VirtualItem is { } vItem && Exists(vItem))
        {
            QueueDel(vItem);
        }

        RemComp<CarrierComponent>(carrier);
        _movementSpeed.RefreshMovementSpeedModifiers(carrier);

        _popup.PopupEntity(Loc.GetString("ashfall-carry-dropped"), carrier, carrier);
    }

    private void OnCarrierTerminating(EntityUid uid, CarrierComponent component, ref EntityTerminatingEvent args)
    {
        if (component.Carried is { } carried && Exists(carried))
        {
            RemComp<BeingCarriedComponent>(carried);
            _transform.AttachToGridOrMap(carried);
        }
    }

    private void OnBeingCarriedTerminating(EntityUid uid, BeingCarriedComponent component, ref EntityTerminatingEvent args)
    {
        if (component.Carrier is { } carrier && Exists(carrier) && TryComp<CarrierComponent>(carrier, out var carrierComp))
        {
            if (carrierComp.VirtualItem is { } vItem && Exists(vItem))
                QueueDel(vItem);
            RemComp<CarrierComponent>(carrier);
            _movementSpeed.RefreshMovementSpeedModifiers(carrier);
        }
    }

    private void OnRefreshMovementSpeed(EntityUid uid, CarrierComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.SpeedModifier, component.SpeedModifier);
    }

    private void OnVirtualItemDeleted(EntityUid uid, CarrierComponent component, VirtualItemDeletedEvent args)
    {
        if (component.Carried is { } carried && carried.IsValid() && args.BlockingEntity == carried)
        {
            DropCarried(uid, component);
        }
    }

    private void OnMobStateChanged(EntityUid uid, CarrierComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Critical or MobState.Dead)
        {
            DropCarried(uid, component);
        }
    }

    private void OnStunned(EntityUid uid, CarrierComponent component, ref StunnedEvent args)
    {
        DropCarried(uid, component);
    }

    private void OnKnockedDown(EntityUid uid, CarrierComponent component, ref KnockedDownEvent args)
    {
        DropCarried(uid, component);
    }

    private void OnBeingCarriedMoveAttempt(EntityUid uid, BeingCarriedComponent component, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnBeingCarriedStandAttempt(EntityUid uid, BeingCarriedComponent component, ref StandAttemptEvent args)
    {
        args.Cancel();
    }
}
