using Content.Shared.Ashfall.CharacterGen.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ashfall.CharacterGen;

/// <summary>
///     A fully rendered dossier section. The server resolves the loc template and arguments; the
///     client only displays the finished strings and never sees the hidden person structure.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class AshfallDossierSection
{
    /// <summary>
    ///     What kind of dossier section this is (origin, education, qualification, career,
    ///     personality, evaluation, hook, precryo) for client-side selection without text matching.
    /// </summary>
    [DataField]
    public string Kind { get; set; } = string.Empty;

    [DataField]
    public string Title { get; set; } = string.Empty;

    [DataField]
    public List<string> Lines { get; set; } = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class AshfallCharacterDossier
{
    [DataField]
    public string CulturalOrigin { get; set; } = string.Empty;

    [DataField]
    public ProtoId<AshfallCulturePrototype>? CultureId { get; set; }

    [DataField]
    public string Birthplace { get; set; } = string.Empty;

    [DataField]
    public string Morphology { get; set; } = string.Empty;

    [DataField]
    public List<AshfallDossierSection> Sections { get; set; } = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class AshfallCharacterCandidate
{
    /// <summary>
    ///     Stable runtime identity of this generated person. A pinned candidate keeps its id
    ///     across rerolls because the object itself is retained; every fresh generation mints
    ///     a new id, so the id is never reused for a different person.
    /// </summary>
    [DataField]
    public Guid CandidateId { get; set; } = Guid.NewGuid();

    [DataField]
    public HumanoidCharacterProfile Profile { get; set; } = new();

    [DataField]
    public AshfallCharacterDossier Dossier { get; set; } = new();

    /// <summary>
    ///     Domain the person is most established in; used for spreading candidate pools.
    /// </summary>
    [DataField]
    public string PrimaryDomain { get; set; } = string.Empty;

    [DataField]
    public List<ProtoId<JobPrototype>> CompatibleJobs { get; set; } = new();
}
