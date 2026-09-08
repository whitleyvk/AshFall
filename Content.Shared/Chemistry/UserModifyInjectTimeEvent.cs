// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Chemistry;

[ByRefEvent]
public record struct UserModifyInjectTimeEvent(EntityUid User, EntityUid Injector, TimeSpan Delay);
