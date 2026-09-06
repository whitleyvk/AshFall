using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ashfall.CharacterGen.Prototypes;

/// <summary>
///     A hidden professional competency of generated personnel. Pure vocabulary: adding new
///     competencies requires only a new YAML entry, never generator changes.
/// </summary>
[Prototype]
public sealed partial class AshfallCompetencyPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Lightweight domain groupings this competency belongs to (e.g. Medical, Engineering).
    ///     Domains are generation weights and pool-spread labels only, not a skill hierarchy.
    /// </summary>
    [DataField]
    public HashSet<string> Domains { get; private set; } = new();

    /// <summary>
    ///     Short title for the dossier qualification section, shown for competencies the employee
    ///     is actually established in.
    /// </summary>
    [DataField]
    public LocId QualificationTitle { get; private set; } = string.Empty;

    /// <summary>
    ///     One-line description for the dossier qualification section.
    /// </summary>
    [DataField]
    public LocId QualificationText { get; private set; } = string.Empty;
}
