// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences;

/// <summary>
/// Trauma - settings for knowledge and skills
/// </summary>
public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    /// Changes to mastery level of every skill for this character, added to the species masteries.
    /// </summary>
    [DataField]
    public KnowledgeProfile Knowledge = new();

    public HumanoidCharacterProfile WithKnowledge(KnowledgeProfile knowledge)
    {
        return new(this) { Knowledge = knowledge };
    }

    private void EnsureValidTrauma(IDependencyCollection collection, IPrototypeManager proto)
    {
        var entMan = collection.Resolve<IEntityManager>();
        var knowledge = entMan.System<CommonKnowledgeSystem>();
        var speciesProto = proto.Index(Species);
        knowledge.EnsureProfileValid(speciesProto.Knowledge, ref Knowledge);
    }
}
