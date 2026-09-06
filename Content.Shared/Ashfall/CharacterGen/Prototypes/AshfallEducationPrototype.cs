using Robust.Shared.Prototypes;

namespace Content.Shared.Ashfall.CharacterGen.Prototypes;

/// <summary>
///     An education path of a generated employee. Grants one-time experience to competencies;
///     the resulting career levels are always derived from accumulated experience.
/// </summary>
[Prototype]
public sealed partial class AshfallEducationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Primary domain of this education (generation weight and dossier grouping only).
    /// </summary>
    [DataField(required: true)]
    public string Domain { get; private set; } = string.Empty;

    /// <summary>
    ///     One-time experience granted on completion.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<AshfallCompetencyPrototype>, float> BaseExperience { get; private set; } = new();

    [DataField(required: true)]
    public int YearsMin { get; private set; }

    [DataField(required: true)]
    public int YearsMax { get; private set; }

    /// <summary>
    ///     Minimum total age for this education to have been plausible (education already finished).
    /// </summary>
    [DataField]
    public int MinAge { get; private set; } = 18;

    [DataField]
    public float Weight { get; private set; } = 1f;

    [DataField]
    public List<AshfallTagWeightModifier> WeightModifiers { get; private set; } = new();

    [DataField(required: true)]
    public LocId Text { get; private set; }

    /// <summary>
    ///     Tags provided by completing this education (e.g. a media diploma counts as media work
    ///     experience for career roles that require it).
    /// </summary>
    [DataField]
    public HashSet<string> ProvidedTags { get; private set; } = new();
}
