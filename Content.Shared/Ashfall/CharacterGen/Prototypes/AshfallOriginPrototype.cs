using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ashfall.CharacterGen.Prototypes;

/// <summary>
///     An upbringing origin of a generated employee. Provides tags that bias (never gate)
///     education and career choices, and optionally biases the birthplace pool.
/// </summary>
[Prototype]
public sealed partial class AshfallOriginPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Text { get; private set; }

    [DataField]
    public float Weight { get; private set; } = 1f;

    [DataField]
    public HashSet<string> RequiredTags { get; private set; } = new();

    [DataField]
    public HashSet<string> ExcludedTags { get; private set; } = new();

    [DataField]
    public HashSet<string> ProvidedTags { get; private set; } = new();

    [DataField]
    public List<AshfallTagWeightModifier> WeightModifiers { get; private set; } = new();

    /// <summary>
    ///     Birthplace datasets this origin biases towards (weight 2 in the birthplace pool).
    /// </summary>
    [DataField]
    public List<ProtoId<DatasetPrototype>> BirthplaceDatasets { get; private set; } = new();
}
