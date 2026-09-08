// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Common.Medical
{
    [ByRefEvent]
    public record struct BeforeAmputationDamageEvent(bool Cancelled = false);
}

namespace Content.Goobstation.Common.DelayedDeath
{
    [ByRefEvent]
    public record struct DelayedDeathEvent(EntityUid User, bool Cancelled = false, bool PreventRevive = true);
}

namespace Content.Goobstation.Common.Examine
{
    [ByRefEvent]
    public record struct ExamineCompletedEvent(
        FormattedMessage Message,
        EntityUid Examined,
        EntityUid Examiner,
        bool IsSecondaryInfo = false);

    [ByRefEvent]
    public record struct UserExaminedEvent(FormattedMessage Message, EntityUid Examined);

    [ByRefEvent]
    public record struct GetExamineNameEvent(Entity<MetaDataComponent> Ent, string? Result = null);
}
