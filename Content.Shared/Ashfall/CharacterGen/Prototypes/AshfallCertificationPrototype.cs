using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ashfall.CharacterGen.Prototypes;

public enum AshfallCertificationKind : byte
{
    Specialization,
    Course,
}

/// <summary>
///     A short additional qualification (specialization or course) shown as an extra education
///     line and granting a small experience contribution within its domain.
/// </summary>
[Prototype]
public sealed partial class AshfallCertificationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Career family this certification belongs to; only offered within the person's families.
    /// </summary>
    [DataField(required: true)]
    public string Domain { get; private set; } = string.Empty;

    /// <summary>
    ///     The bracketed type label shown before the certification text in the dossier.
    /// </summary>
    [DataField(required: true)]
    public AshfallCertificationKind Kind { get; private set; }

    [DataField(required: true)]
    public LocId Text { get; private set; }

    [DataField]
    public float Weight { get; private set; } = 1f;

    [DataField(required: true)]
    public Dictionary<ProtoId<AshfallCompetencyPrototype>, float> Experience { get; private set; } = new();
}
