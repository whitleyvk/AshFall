using Robust.Shared.Prototypes;

namespace Content.Shared.Ashfall.CharacterGen.Prototypes;

/// <summary>
///     An employer of a past career stint. Industries keep previous jobs believable: a greenhouse
///     technician works for an agricultural company, not a weapons manufacturer.
/// </summary>
[Prototype]
public sealed partial class AshfallEmployerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField(required: true)]
    public HashSet<string> Industries { get; private set; } = new();

    [DataField]
    public float Weight { get; private set; } = 1f;
}
