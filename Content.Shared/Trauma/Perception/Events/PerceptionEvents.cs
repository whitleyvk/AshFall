// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Trauma.Perception.Events;

/// <summary>
/// Raised on an entity when its calculated light level changes.
/// </summary>
[ByRefEvent]
public readonly record struct LightLevelUpdated(float NewLightLevel, float OldLightLevel);

/// <summary>
/// Raised before light detection damage/healing updates, allowing systems (like light immunity) to cancel it.
/// </summary>
[ByRefEvent]
public record struct LightDamageUpdateAttemptEvent(bool Cancelled = false);

/// <summary>
/// Raised on an entity when its perception stealth visibility factor changes.
/// </summary>
[ByRefEvent]
public readonly record struct PerceptionStealthVisibilityChangedEvent(EntityUid Target, float NewVisibility, float OldVisibility);
