using Robust.Shared.Utility;

namespace Content.Shared.Ashfall.Examine;

/// <summary>
/// Raised on an entity after its examine message has been fully assembled by ExamineSystemShared.
/// </summary>
[ByRefEvent]
public readonly record struct ExamineCompletedEvent(FormattedMessage Message, EntityUid Target, EntityUid Examiner);
