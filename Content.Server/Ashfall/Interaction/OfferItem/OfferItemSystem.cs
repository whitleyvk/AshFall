using Content.Shared.Ashfall.Interaction.OfferItem;
using Content.Shared.Alert;

namespace Content.Server.Ashfall.Interaction.OfferItem;

public sealed partial class OfferItemSystem : SharedOfferItemSystem
{
    [Dependency] private AlertsSystem _alertsSystem = default!;

    private float _offerAcc = 0;
    private const float OfferAccMax = 0.5f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _offerAcc += frameTime;

        if (_offerAcc >= OfferAccMax)
            _offerAcc -= OfferAccMax;
        else
            return;

        var query = EntityQueryEnumerator<OfferItemComponent>();
        while (query.MoveNext(out var uid, out var offerItem))
        {
            if ((offerItem.IsInOfferMode || offerItem.IsInReceiveMode || offerItem.Target is not null) &&
                !IsCurrentOfferStateValid(uid, offerItem))
            {
                CancelPair(uid, offerItem);
                continue;
            }

            if (!offerItem.IsInReceiveMode)
            {
                _alertsSystem.ClearAlert(uid, OfferAlert);
                continue;
            }

            _alertsSystem.ShowAlert(uid, OfferAlert);
        }
    }
}
