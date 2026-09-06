using Robust.Shared.Prototypes;

namespace Content.Shared.Ashfall.CharacterGen.Prototypes;

/// <summary>
///     A significant career event (retraining, demotion, acting supervisor, employer bankruptcy...).
///     Reserved for the second phase of the generator rework: the pipeline supports the layer, but
///     event generation stays disabled until content exists.
/// </summary>
[Prototype]
public sealed partial class AshfallCareerEventPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Text { get; private set; }

    [DataField]
    public float Weight { get; private set; } = 1f;

    [DataField]
    public int MinAge { get; private set; } = 18;

    /// <summary>
    ///     One-time experience adjustment applied when the event happens (may be negative).
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AshfallCompetencyPrototype>, float> BaseExperience { get; private set; } = new();

    [DataField]
    public HashSet<string> RequiredTags { get; private set; } = new();

    [DataField]
    public HashSet<string> ProvidedTags { get; private set; } = new();

    [DataField]
    public List<AshfallTagWeightModifier> WeightModifiers { get; private set; } = new();
}
