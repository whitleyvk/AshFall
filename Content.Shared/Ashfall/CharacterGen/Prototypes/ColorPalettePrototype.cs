using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ashfall.CharacterGen.Prototypes;

/// <summary>
///     Data-driven prototype for curated color palettes (e.g. natural hair, eyes, skin tones).
/// </summary>
[Prototype]
public sealed partial class ColorPalettePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     List of named anchor colors in this palette with their base RGB hex and selection weights.
    /// </summary>
    [DataField(required: true)]
    public List<PaletteColorEntry> Colors { get; private set; } = new();

    /// <summary>
    ///     Maximum random jitter applied to RGB channels to create natural organic variation.
    /// </summary>
    [DataField]
    public float ChannelJitter { get; private set; } = 0.05f;

    /// <summary>
    ///     Minimum value bound to prevent clipping into pure black.
    /// </summary>
    [DataField]
    public float MinValue { get; private set; } = 0.05f;

    /// <summary>
    ///     Maximum value bound to prevent clipping into pure white.
    /// </summary>
    [DataField]
    public float MaxValue { get; private set; } = 0.98f;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class PaletteColorEntry
{
    [DataField(required: true)]
    public string Name { get; private set; } = string.Empty;

    [DataField(required: true)]
    public Color Color { get; private set; } = Color.Black;

    [DataField]
    public float Weight { get; private set; } = 1.0f;
}
