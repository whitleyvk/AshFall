// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Trauma.Common.Knowledge;

/// <summary>
/// Event to update experience / mastery UI in character window.
/// </summary>
[ByRefEvent]
public record struct UpdateExperienceEvent;

/// <summary>
/// Raised to modify the block fraction of a shield.
/// </summary>
[ByRefEvent]
public record struct GetBlockFractionEvent(EntityUid User, EntityUid Blocker, float Fraction);

/// <summary>
/// Raised to modify thrown speed.
/// </summary>
[ByRefEvent]
public record struct ModifyThrownSpeedEvent(EntityUid User, float BaseThrowSpeed, float Distance);

/// <summary>
/// Raised to modify throw insertion chance.
/// </summary>
[ByRefEvent]
public record struct ModifyThrowInsertChanceEvent(float Chance);
