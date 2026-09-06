using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ashfall.CharacterGen.Prototypes;

/// <summary>
///     Data-driven constraints and probability weights for procedural character generation.
/// </summary>
[Prototype]
public sealed partial class CharacterGenConstraintsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Target species this constraint set applies to.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SpeciesPrototype> Species { get; private set; } = default!;

    /// <summary>
    ///     Weighted age brackets for character generation.
    /// </summary>
    [DataField]
    public List<AgeWeightBracket> AgeBrackets { get; private set; } = new()
    {
        new() { MinAge = 18, MaxAge = 30, Weight = 4.0f },
        new() { MinAge = 31, MaxAge = 50, Weight = 4.0f },
        new() { MinAge = 51, MaxAge = 65, Weight = 1.5f },
        new() { MinAge = 66, MaxAge = 75, Weight = 0.5f },
    };

    /// <summary>
    ///     Natural hair color palette prototype.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ColorPalettePrototype> HairPalette { get; private set; } = default!;

    /// <summary>
    ///     Natural eye color palette prototype.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ColorPalettePrototype> EyePalette { get; private set; } = default!;

    /// <summary>
    ///     Probability curve of gray/white hair based on age breakpoints.
    /// </summary>
    [DataField]
    public List<AgeGrayingChance> GrayingChances { get; private set; } = new()
    {
        new() { MaxAge = 30, Chance = 0.01f },
        new() { MaxAge = 45, Chance = 0.10f },
        new() { MaxAge = 55, Chance = 0.35f },
        new() { MaxAge = 65, Chance = 0.70f },
        new() { MaxAge = 120, Chance = 0.95f },
    };

    /// <summary>
    ///     Curated whitelist of hairstyles permitted for this constraint set.
    ///     If empty, all valid species hairstyles are allowed.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<MarkingPrototype>> AllowedHairstyles { get; private set; } = new();

    /// <summary>
    ///     Curated whitelist of facial hairstyles permitted for this constraint set.
    ///     If empty, all valid species facial hairs are allowed.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<MarkingPrototype>> AllowedFacialHairstyles { get; private set; } = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class AgeWeightBracket
{
    [DataField]
    public int MinAge { get; set; } = 18;

    [DataField]
    public int MaxAge { get; set; } = 30;

    [DataField]
    public float Weight { get; set; } = 1.0f;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class AgeGrayingChance
{
    [DataField]
    public int MaxAge { get; set; } = 30;

    [DataField]
    public float Chance { get; set; } = 0.05f;
}

