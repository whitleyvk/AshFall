// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Events;

[ByRefEvent]
public record struct AmmoShotUserEvent(EntityUid Gun, List<EntityUid> FiredProjectiles);

[ByRefEvent]
public record struct GetRecoilModifiersEvent(EntityUid Gun, EntityUid User, float Modifier = 1f);
