using Robust.Shared.Prototypes;

namespace Content.Shared.Ashfall.CharacterGen.Prototypes;

/// <summary>
///     Playable-job side of the hidden career model. Declares which competencies (with which
///     career levels) qualify a generated employee for the job; eligibility is computed from the
///     finished person structure, never rolled.
/// </summary>
[Prototype]
public sealed partial class AshfallJobCareerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Domain of the job (pool spreading and scoring context only).
    /// </summary>
    [DataField(required: true)]
    public string Domain { get; private set; } = string.Empty;

    /// <summary>
    ///     Competency levels the employee must have reached.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<AshfallCompetencyPrototype>, AshfallCareerLevel> RequiredCompetencies { get; private set; } = new();

    /// <summary>
    ///     Competency levels that raise the assignment score when reached.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AshfallCompetencyPrototype>, AshfallCareerLevel> PreferredCompetencies { get; private set; } = new();

    /// <summary>
    ///     Competency level ceilings. An employee established above the ceiling in that competency
    ///     is overqualified and no longer eligible (a senior doctor cannot become an intern).
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AshfallCompetencyPrototype>, AshfallCareerLevel> MaximumCompetencies { get; private set; } = new();

    /// <summary>
    ///     Requires actual leadership history (a leadership-role stint or equivalent), not just the
    ///     Command competency.
    /// </summary>
    [DataField]
    public bool RequiresLeadershipHistory { get; private set; }

    /// <summary>
    ///     Fallback assignments (Passenger and similar) are only offered when no non-fallback job
    ///     is valid for the person.
    /// </summary>
    [DataField]
    public bool FallbackOnly { get; private set; }
}
