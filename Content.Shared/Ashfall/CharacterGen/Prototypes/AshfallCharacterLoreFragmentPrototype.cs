using Content.Shared.Roles;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ashfall.CharacterGen.Prototypes;

[Serializable, NetSerializable]
public enum AshfallCharacterLoreCategory : byte
{
    Origin,
    Education,
    Qualification,
    Career,
    Personality,
    Evaluation,
    PersonalHook,
    PreCryo,
}

/// <summary>
///     A curated fragment used to assemble an Ashfall personnel record.
/// </summary>
[Prototype]
public sealed partial class AshfallCharacterLoreFragmentPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public AshfallCharacterLoreCategory Category { get; private set; }

    [DataField(required: true)]
    public LocId Title { get; private set; } = string.Empty;

    [DataField(required: true)]
    public LocId Text { get; private set; } = string.Empty;

    [DataField]
    public float Weight { get; private set; } = 1f;

    [DataField]
    public int MinAge { get; private set; } = 18;

    [DataField]
    public int MaxAge { get; private set; } = 120;

    [DataField]
    public string? ProfessionalFamily { get; private set; }

    [DataField]
    public HashSet<string> RequiredTags { get; private set; } = new();

    [DataField]
    public HashSet<string> ExcludedTags { get; private set; } = new();

    [DataField]
    public HashSet<string> ProvidedTags { get; private set; } = new();

    [DataField]
    public List<AshfallLoreWeightModifier> WeightModifiers { get; private set; } = new();

    [DataField]
    public List<ProtoId<JobPrototype>> Jobs { get; private set; } = new();
}

[DataDefinition]
public sealed partial class AshfallLoreWeightModifier
{
    [DataField(required: true)]
    public string Tag { get; private set; } = string.Empty;

    [DataField]
    public float Multiplier { get; private set; } = 1f;
}
