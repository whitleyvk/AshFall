using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.Ashfall.Interaction.OfferItem;

public abstract partial class SharedOfferItemSystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedHandsSystem _hand = default!;

    private void InitializeInteractions()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OfferItem, InputCmdHandler.FromDelegate(SetInOfferMode, handle: false, outsidePrediction: false))
            .Register<SharedOfferItemSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<SharedOfferItemSystem>();
    }

    private void SetInOfferMode(ICommonSession? session)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (session is null)
            return;

        if (session.AttachedEntity is not { Valid: true } uid)
            return;

        TryStartOffer(uid);
    }

    public bool TryStartOffer(EntityUid uid)
    {
        if (!Exists(uid) || !_actionBlocker.CanInteract(uid, null))
            return false;

        if (!TryComp<OfferItemComponent>(uid, out var offerItem))
            return false;

        if (!TryComp<HandsComponent>(uid, out var hands) || hands.ActiveHandId is null)
            return false;

        if (offerItem.IsInOfferMode || offerItem.IsInReceiveMode || offerItem.Target is not null)
        {
            CancelPair(uid, offerItem);
            return false;
        }

        var item = _hand.GetActiveItem((uid, hands));
        if (item is null)
        {
            _popup.PopupEntity(Loc.GetString("offer-item-empty-hand"), uid, uid);
            return false;
        }

        offerItem.IsInOfferMode = true;
        offerItem.Hand = hands.ActiveHandId;
        offerItem.Item = item;
        Dirty(uid, offerItem);
        return true;
    }
}
