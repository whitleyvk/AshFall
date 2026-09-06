using Content.Shared.Dataset;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ashfall.CharacterGen.Prototypes;

/// <summary>
///     A cultural / naming group of a species. Determines given names and surnames only; the
///     birthplace is biased by both culture and origin but never determined by either.
/// </summary>
[Prototype]
public sealed partial class AshfallCulturePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<SpeciesPrototype> Species { get; private set; }

    [DataField]
    public float Weight { get; private set; } = 1f;

    /// <summary>
    ///     Player-facing cultural line shown in the dossier header.
    /// </summary>
    [DataField(required: true)]
    public LocId Label { get; private set; }

    [DataField(required: true)]
    public ProtoId<DatasetPrototype> FirstNamesMale { get; private set; }

    [DataField(required: true)]
    public ProtoId<DatasetPrototype> FirstNamesFemale { get; private set; }

    [DataField(required: true)]
    public ProtoId<DatasetPrototype> LastNames { get; private set; }

    /// <summary>
    ///     Applies Slavic female surname declension (Panslavic cultures).
    /// </summary>
    [DataField]
    public bool SurnameDeclension { get; private set; }

    /// <summary>
    ///     Birthplace datasets this culture biases towards (weight 3 in the birthplace pool).
    /// </summary>
    [DataField]
    public List<ProtoId<DatasetPrototype>> BirthplaceDatasets { get; private set; } = new();
}
