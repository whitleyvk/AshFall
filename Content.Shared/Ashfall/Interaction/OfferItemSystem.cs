using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Shared.Ashfall.Interaction;

public sealed partial class OfferItemSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandsComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(EntityUid uid, HandsComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (args.User == uid)
            return;

        if (!_actionBlocker.CanInteract(uid, null))
            return;

        if (!TryComp<HandsComponent>(args.User, out var userHands))
            return;

        if (_hands.GetActiveItem((args.User, userHands)) is not { } heldItem)
            return;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("ashfall-verb-offer-item", ("item", heldItem)),
            IconEntity = GetNetEntity(heldItem),
            Act = () => TryOfferItem(args.User, uid, heldItem),
            Priority = 1,
        });
    }

    public bool TryOfferItem(EntityUid user, EntityUid target, EntityUid item)
    {
        if (!_actionBlocker.CanInteract(target, null))
            return false;

        if (!_interaction.InRangeUnobstructed(user, target))
            return false;

        if (!TryComp<HandsComponent>(target, out var targetHands) || _hands.CountFreeHands((target, targetHands)) == 0)
        {
            _popup.PopupPredicted(
                Loc.GetString("ashfall-offer-item-no-free-hands", ("target", target)),
                user,
                user);
            return false;
        }

        if (!TryComp<HandsComponent>(user, out var userHands))
            return false;

        if (!_hands.TryDrop((user, userHands), item))
            return false;

        if (!_hands.TryPickupAnyHand(target, item, handsComp: targetHands))
        {
            _hands.TryPickupAnyHand(user, item, handsComp: userHands);
            return false;
        }

        _popup.PopupPredicted(
            Loc.GetString("ashfall-offer-item-success-user", ("item", item), ("target", target)),
            Loc.GetString("ashfall-offer-item-success-target", ("item", item), ("user", user)),
            user,
            target);

        return true;
    }
}
