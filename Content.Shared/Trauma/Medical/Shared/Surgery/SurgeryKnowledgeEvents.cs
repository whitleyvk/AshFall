namespace Content.Medical.Shared.Surgery;

/// <summary>
/// Raised before a surgery step begins to allow knowledge and other systems to modify speed.
/// </summary>
[ByRefEvent]
public record struct BeforeSurgeryStepDurationEvent(EntityUid User, EntityUid SurgeryStep, EntityUid Target, float Speed);
