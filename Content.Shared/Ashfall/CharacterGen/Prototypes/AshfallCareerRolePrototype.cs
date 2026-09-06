using Robust.Shared.Prototypes;

namespace Content.Shared.Ashfall.CharacterGen.Prototypes;

/// <summary>
///     A past civilian profession of a generated employee. Deliberately independent from playable
///     JobPrototypes: past careers are far broader than SS14 roles. Job eligibility is derived
///     from the competencies these roles granted, never assigned directly.
/// </summary>
[Prototype]
public sealed partial class AshfallCareerRolePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Role title used in the dossier work history line.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; private set; }

    /// <summary>
    ///     The PRIMARY professional domain of this role: working in it counts as belonging to this
    ///     career family. Extra Domains below only grant bridging competencies and never extend
    ///     the person's career families by themselves.
    /// </summary>
    [DataField(required: true)]
    public string Domain { get; private set; } = string.Empty;

    [DataField]
    public HashSet<string> Domains { get; private set; } = new();

    /// <summary>
    ///     Experience accumulated per year of employment in this role.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<AshfallCompetencyPrototype>, float> ExperiencePerYear { get; private set; } = new();

    /// <summary>
    ///     Industries the employer for this role must belong to.
    /// </summary>
    [DataField(required: true)]
    public HashSet<string> EmployerIndustries { get; private set; } = new();

    /// <summary>
    ///     Whether working in this role counts as leadership experience.
    /// </summary>
    [DataField]
    public bool Leadership { get; private set; }

    /// <summary>
    ///     Entry roles (cadets, trainees) can only be the first career stint: an experienced
    ///     professional does not go back to being a cadet without a career event.
    /// </summary>
    [DataField]
    public bool EntryRole { get; private set; }

    [DataField(required: true)]
    public int YearsMin { get; private set; }

    [DataField(required: true)]
    public int YearsMax { get; private set; }

    /// <summary>
    ///     Minimum age to have held this role (age at the start of the stint).
    /// </summary>
    [DataField]
    public int MinAge { get; private set; } = 18;

    [DataField]
    public float Weight { get; private set; } = 1f;

    /// <summary>
    ///     Tags required in the person's structure for this role to be selectable (career chains).
    /// </summary>
    [DataField]
    public HashSet<string> RequiredTags { get; private set; } = new();

    [DataField]
    public HashSet<string> ProvidedTags { get; private set; } = new();

    [DataField]
    public List<AshfallTagWeightModifier> WeightModifiers { get; private set; } = new();
}
