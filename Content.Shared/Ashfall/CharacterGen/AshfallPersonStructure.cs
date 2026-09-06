using Content.Shared.Ashfall.CharacterGen.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ashfall.CharacterGen;

public enum AshfallProvenanceSource : byte
{
    Education,
    Career,
    Event,
}

[DataDefinition]
public sealed partial record AshfallCompetencyProvenance
{
    [DataField(required: true)]
    public AshfallProvenanceSource Source { get; set; }

    [DataField(required: true)]
    public string SourceId { get; set; } = string.Empty;
}

/// <summary>
///     Accumulated state of a single competency. Career level is always derived from experience;
///     provenance lists the biography sources that contributed it.
/// </summary>
public sealed class AshfallCompetencyState
{
    public float Experience { get; set; }
    public List<AshfallCompetencyProvenance> Provenance { get; } = new();

    public AshfallCareerLevel Level => AshfallCareerLevels.FromExperience(Experience);
}

/// <summary>
///     A past employment stint of a generated person.
/// </summary>
public sealed class AshfallCareerStint
{
    public ProtoId<AshfallCareerRolePrototype> Role { get; set; }
    public ProtoId<AshfallEmployerPrototype>? Employer { get; set; }
    public int Years { get; set; }

    /// <summary>
    ///     Cached from the role prototype at generation time so the structure needs no prototype
    ///     manager to answer questions about itself.
    /// </summary>
    public bool Leadership { get; set; }
}

/// <summary>
///     The hidden structured person generated BEFORE any dossier text. Everything the dossier
///     says and every eligible job is derived from this structure. Server-side only: never
///     serialized to the client.
/// </summary>
public sealed class AshfallPersonStructure
{
    public string SpeciesId { get; set; } = string.Empty;
    public int Age { get; set; }
    public ProtoId<AshfallCulturePrototype> Culture { get; set; }
    public ProtoId<AshfallOriginPrototype> Origin { get; set; }
    public ProtoId<AshfallEducationPrototype> Education { get; set; }
    public string Birthplace { get; set; } = string.Empty;

    public List<AshfallCareerStint> Career { get; } = new();
    public List<ProtoId<AshfallCertificationPrototype>> Certifications { get; } = new();
    public List<ProtoId<AshfallCareerEventPrototype>> Events { get; } = new();

    public Dictionary<ProtoId<AshfallCompetencyPrototype>, AshfallCompetencyState> Competencies { get; } = new();

    public bool LeadershipHistory { get; set; }

    /// <summary>
    ///     Years of employment shown in the work history; gaps (job searching, relocations,
    ///     unlisted short contracts) are deliberately left unused.
    /// </summary>
    public int UsedCareerYears { get; set; }

    public int AvailableCareerYears { get; set; }

    public HashSet<string> StructureTags { get; } = new();

    /// <summary>
    ///     Professional families (domains) the career spine is allowed to touch: the education's
    ///     main field, domains bridged by a Qualified-level education competency (adjacent
    ///     specialization), and at most one retraining field.
    /// </summary>
    public HashSet<string> CareerFamilies { get; } = new();

    /// <summary>
    ///     Number of major career changes (retrainings) the person went through; at most one.
    /// </summary>
    public int RetrainingCount { get; set; }

    /// <summary>
    ///     Domains of the person's CURRENT professional track: the last career stint's domains, or
    ///     the education's domain when there is no career history yet.
    /// </summary>
    public HashSet<string> CurrentDomains { get; } = new();

    /// <summary>
    ///     Competencies maintained by the current professional track (current-field education and
    ///     recent in-track stints). Historical experience stays in provenance for the biography
    ///     but does not grant assignments.
    /// </summary>
    public HashSet<ProtoId<AshfallCompetencyPrototype>> ActiveCompetencies { get; } = new();

    /// <summary>
    ///     Set by the generator once the person's career is complete. Manually constructed
    ///     structures (test fixtures) leave this off, treating every competency as active.
    /// </summary>
    public bool HasActiveRestriction { get; set; }

    /// <summary>
    ///     Domain the person is most established in; computed by the generator after the history
    ///     is complete (pool spreading and scoring context only).
    /// </summary>
    public string PrimaryDomain { get; set; } = "Service";

    public AshfallCompetencyState GetOrAddCompetency(ProtoId<AshfallCompetencyPrototype> competency)
    {
        if (!Competencies.TryGetValue(competency, out var state))
        {
            state = new AshfallCompetencyState();
            Competencies[competency] = state;
        }

        return state;
    }

    /// <summary>
    ///     Adds experience from a source, recording provenance when the contribution is non-zero.
    /// </summary>
    public void AddExperience(
        ProtoId<AshfallCompetencyPrototype> competency,
        float amount,
        AshfallProvenanceSource source,
        string sourceId)
    {
        if (amount <= 0f)
            return;

        var state = GetOrAddCompetency(competency);
        state.Experience += amount;

        var provenance = new AshfallCompetencyProvenance { Source = source, SourceId = sourceId };
        if (!state.Provenance.Contains(provenance))
            state.Provenance.Add(provenance);
    }

    public AshfallCareerLevel GetLevel(ProtoId<AshfallCompetencyPrototype> competency)
    {
        return Competencies.TryGetValue(competency, out var state)
            ? state.Level
            : AshfallCareerLevel.Trainee;
    }
}
