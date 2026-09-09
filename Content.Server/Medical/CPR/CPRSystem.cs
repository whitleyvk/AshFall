// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Medical.CPR;

namespace Content.Server.Medical.CPR;

public sealed partial class CPRSystem : SharedCPRSystem
{
    [Dependency] private RespiratorSystem _respirator = default!;
    [Dependency] private EntityQuery<RespiratorComponent> _respiratorQuery = default!;

    protected override void TryBreathe(EntityUid uid)
    {
        if (!_respiratorQuery.TryComp(uid, out var respirator))
            return;

        _respirator.Inhale((uid, respirator));
        _respirator.Exhale((uid, respirator));
    }
}
