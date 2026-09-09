using Content.Shared.Alert;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Ashfall.Interaction.OfferItem;

public abstract partial class SharedOfferItemSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly HashSet<EntityUid> _transferringItems = new();

    protected ProtoId<AlertPrototype> OfferAlert = "Offer";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OfferItemComponent, InteractUsingEvent>(SetInReceiveMode);
        SubscribeLocalEvent<OfferItemComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<OfferItemComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<OfferItemComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<ItemComponent, HandDeselectedEvent>(OnOfferedItemDeselected);
        SubscribeLocalEvent<ItemComponent, GotUnequippedHandEvent>(OnOfferedItemUnequipped);

        InitializeInteractions();

        SubscribeLocalEvent<OfferItemComponent, AcceptOfferAlertEvent>(OnClickAlertEvent);
    }

    private void OnClickAlertEvent(Entity<OfferItemComponent> ent, ref AcceptOfferAlertEvent ev)
    {
        if (ev.Handled || ev.AlertId != OfferAlert)
            return;

        ev.Handled = true;
        Receive(ent.AsNullable());
    }

    private void OnInteractHand(EntityUid uid, OfferItemComponent component, InteractHandEvent args)
    {
        if (args.Handled ||
            !TryComp<OfferItemComponent>(args.User, out var receiver) ||
            !receiver.IsInReceiveMode ||
            receiver.Target != uid)
        {
            return;
        }

        args.Handled = true;
        Receive((args.User, receiver));
    }

    public void Receive(Entity<OfferItemComponent?> receiver)
    {
        if (!_timing.IsFirstTimePredicted || !Resolve(receiver, ref receiver.Comp))
            return;

        if (receiver.Comp.Target is not { } giver ||
            !TryComp<OfferItemComponent>(giver, out var offer) ||
            !IsOfferPairValid(giver, offer, receiver, receiver.Comp, out var item))
        {
            CancelPair(receiver, receiver.Comp);
            return;
        }

        if (!TryComp<HandsComponent>(receiver, out var hands))
        {
            CancelPair(receiver, receiver.Comp);
            return;
        }

        var pickedUp = false;
        _transferringItems.Add(item);
        try
        {
            pickedUp = _hand.TryPickup(receiver, item, handsComp: hands);
        }
        finally
        {
            _transferringItems.Remove(item);
        }

        if (!pickedUp)
        {
            _popup.PopupEntity(Loc.GetString("offer-item-full-hand"), receiver, receiver);
            return;
        }

        _popup.PopupEntity(Loc.GetString("offer-item-give",
            ("item", Identity.Entity(item, EntityManager)),
            ("target", Identity.Entity(receiver, EntityManager))), giver, giver);

        _popup.PopupEntity(Loc.GetString("offer-item-give-other",
                ("user", Identity.Entity(giver, EntityManager)),
                ("item", Identity.Entity(item, EntityManager)),
                ("target", Identity.Entity(receiver, EntityManager))),
            giver,
            receiver);

        ClearPair(giver, offer, receiver, receiver.Comp);
    }

    private void SetInReceiveMode(EntityUid uid, OfferItemComponent component, InteractUsingEvent args)
    {
        if (args.Handled ||
            args.User == uid ||
            component.IsInReceiveMode ||
            !TryComp<OfferItemComponent>(args.User, out var offer) ||
            !offer.IsInOfferMode ||
            !TryGetStoredItem(args.User, offer, out var item) ||
            args.Used != item ||
            !_actionBlocker.CanInteract(uid, null) ||
            !_actionBlocker.CanInteract(args.User, null) ||
            !_interaction.InRangeUnobstructed(args.User, uid, offer.MaxOfferDistance))
        {
            return;
        }

        component.IsInReceiveMode = true;
        component.Target = args.User;
        Dirty(uid, component);

        offer.Target = uid;
        offer.IsInOfferMode = false;
        Dirty(args.User, offer);

        _popup.PopupEntity(Loc.GetString("offer-item-try-give",
            ("item", Identity.Entity(item, EntityManager)),
            ("target", Identity.Entity(uid, EntityManager))), args.User, args.User);
        _popup.PopupEntity(Loc.GetString("offer-item-try-give-target",
            ("user", Identity.Entity(args.User, EntityManager)),
            ("item", Identity.Entity(item, EntityManager))), args.User, uid);

        args.Handled = true;
    }

    private void OnMove(EntityUid uid, OfferItemComponent component, MoveEvent args)
    {
        if (component.Target is not { } target)
            return;

        if (!Exists(target) ||
            !_transform.InRange(args.NewPosition, Transform(target).Coordinates, component.MaxOfferDistance))
        {
            CancelPair(uid, component);
        }
    }

    private void OnTerminating(Entity<OfferItemComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Target is { } target && TryComp<OfferItemComponent>(target, out var other))
            ClearState(target, other);

        ent.Comp.IsInOfferMode = false;
        ent.Comp.IsInReceiveMode = false;
        ent.Comp.Hand = null;
        ent.Comp.Target = null;
        ent.Comp.Item = null;
    }

    private void OnOfferedItemDeselected(Entity<ItemComponent> ent, ref HandDeselectedEvent args)
    {
        CancelIfOfferedItemChanged(ent.Owner, args.User);
    }

    private void OnOfferedItemUnequipped(Entity<ItemComponent> ent, ref GotUnequippedHandEvent args)
    {
        CancelIfOfferedItemChanged(ent.Owner, args.User);
    }

    private void CancelIfOfferedItemChanged(EntityUid item, EntityUid giver)
    {
        if (_transferringItems.Contains(item) ||
            !TryComp<OfferItemComponent>(giver, out var offer) ||
            offer.Item != item)
            return;

        CancelPair(giver, offer);
    }

    protected bool TryGetStoredItem(EntityUid giver, OfferItemComponent offer, out EntityUid item)
    {
        item = default;
        if (offer.Hand is null ||
            offer.Item is not { } stored ||
            !Exists(stored) ||
            !TryComp<HandsComponent>(giver, out var hands) ||
            hands.ActiveHandId != offer.Hand ||
            _hand.GetActiveItem((giver, hands)) != stored)
        {
            return false;
        }

        item = stored;
        return true;
    }

    private bool IsOfferPairValid(
        EntityUid giver,
        OfferItemComponent offer,
        EntityUid receiver,
        OfferItemComponent receive,
        out EntityUid item)
    {
        item = default;
        return offer.Target == receiver &&
               receive.IsInReceiveMode &&
               receive.Target == giver &&
               TryGetStoredItem(giver, offer, out item) &&
               _actionBlocker.CanInteract(giver, null) &&
               _actionBlocker.CanInteract(receiver, null) &&
               _interaction.InRangeUnobstructed(giver, receiver, offer.MaxOfferDistance);
    }

    protected bool IsCurrentOfferStateValid(EntityUid uid, OfferItemComponent component)
    {
        if (component.Target is not { } target)
            return component.IsInOfferMode && TryGetStoredItem(uid, component, out _);

        if (!Exists(target) || !TryComp<OfferItemComponent>(target, out var other))
            return false;

        var giver = component.Item is not null ? uid : target;
        var receiver = giver == uid ? target : uid;
        var offer = giver == uid ? component : other;
        var receive = receiver == uid ? component : other;
        return IsOfferPairValid(giver, offer, receiver, receive, out _);
    }

    protected void CancelPair(EntityUid uid, OfferItemComponent component)
    {
        if (component.Target is { } target && TryComp<OfferItemComponent>(target, out var other))
        {
            var giver = component.Item is not null ? uid : target;
            var receiver = giver == uid ? target : uid;
            var item = component.Item ?? other.Item;

            if (item is { } offered && Exists(offered))
            {
                _popup.PopupEntity(Loc.GetString("offer-item-no-give",
                    ("item", Identity.Entity(offered, EntityManager)),
                    ("target", Identity.Entity(receiver, EntityManager))), giver, giver);
                _popup.PopupEntity(Loc.GetString("offer-item-no-give-target",
                    ("user", Identity.Entity(giver, EntityManager)),
                    ("item", Identity.Entity(offered, EntityManager))), giver, receiver);
            }

            ClearPair(uid, component, target, other);
            return;
        }

        ClearState(uid, component);
    }

    private void ClearPair(
        EntityUid firstUid,
        OfferItemComponent first,
        EntityUid secondUid,
        OfferItemComponent second)
    {
        ClearState(firstUid, first);
        ClearState(secondUid, second);
    }

    private void ClearState(EntityUid uid, OfferItemComponent component)
    {
        component.IsInOfferMode = false;
        component.IsInReceiveMode = false;
        component.Hand = null;
        component.Target = null;
        component.Item = null;
        Dirty(uid, component);
        _alerts.ClearAlert(uid, OfferAlert);
    }

    protected void UnOffer(EntityUid uid, OfferItemComponent component)
        => CancelPair(uid, component);

    protected void UnReceive(EntityUid uid, OfferItemComponent? component = null, OfferItemComponent? offerItem = null)
    {
        if (component is null && !TryComp(uid, out component))
            return;

        CancelPair(uid, component);
    }

    protected bool IsInOfferMode(EntityUid? entity, OfferItemComponent? component = null)
        => entity is not null && Resolve(entity.Value, ref component, false) && component.IsInOfferMode;
}
