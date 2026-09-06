using Robust.Shared.Serialization;

namespace Content.Shared.Ashfall.CharacterGen.Prototypes;

/// <summary>
///     Multiplies the selection weight of a prototype when the person's structure carries a tag.
///     Used to bias (never hard-gate) education, career roles and events by origin and history.
/// </summary>
[DataDefinition]
public sealed partial class AshfallTagWeightModifier
{
    [DataField(required: true)]
    public string Tag { get; private set; } = string.Empty;

    [DataField]
    public float Multiplier { get; private set; } = 1f;
}
