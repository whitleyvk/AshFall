// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;

namespace Content.Trauma.Common.Popups;

[ByRefEvent]
public record struct ShowPopupAttemptEvent(Vector2 WorldPos, Vector2 ViewerPos, bool Cancelled = false);
