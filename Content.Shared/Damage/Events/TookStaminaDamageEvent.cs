// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Damage.Events;

/// <summary>
/// Raised on an entity after it has taken immediate stamina damage.
/// </summary>
[ByRefEvent]
public record struct TookStaminaDamageEvent(EntityUid Target, EntityUid? Source, float Amount);
