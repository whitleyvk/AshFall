using Content.Shared.Ashfall.CharacterGen.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ashfall.CharacterGen;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class AshfallCharacterDossier
{
    [DataField]
    public string CulturalOrigin { get; set; } = string.Empty;

    [DataField]
    public string Birthplace { get; set; } = string.Empty;

    [DataField]
    public string Morphology { get; set; } = string.Empty;

    [DataField]
    public ProtoId<AshfallCharacterLoreFragmentPrototype> Origin { get; set; }

    [DataField]
    public ProtoId<AshfallCharacterLoreFragmentPrototype> Education { get; set; }

    [DataField]
    public List<ProtoId<AshfallCharacterLoreFragmentPrototype>> Qualifications { get; set; } = new();

    [DataField]
    public List<ProtoId<AshfallCharacterLoreFragmentPrototype>> Career { get; set; } = new();

    [DataField]
    public ProtoId<AshfallCharacterLoreFragmentPrototype> Personality { get; set; }

    [DataField]
    public List<ProtoId<AshfallCharacterLoreFragmentPrototype>> Evaluations { get; set; } = new();

    [DataField]
    public ProtoId<AshfallCharacterLoreFragmentPrototype> PersonalHook { get; set; }

    [DataField]
    public ProtoId<AshfallCharacterLoreFragmentPrototype> PreCryo { get; set; }
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class AshfallCharacterCandidate
{
    [DataField]
    public HumanoidCharacterProfile Profile { get; set; } = new();

    [DataField]
    public AshfallCharacterDossier Dossier { get; set; } = new();

    [DataField]
    public string ProfessionalFamily { get; set; } = string.Empty;

    [DataField]
    public List<ProtoId<JobPrototype>> CompatibleJobs { get; set; } = new();
}
