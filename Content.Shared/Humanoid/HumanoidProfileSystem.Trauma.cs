// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Systems;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidProfileSystem
{
    [Dependency] private CommonKnowledgeSystem _knowledge = default!;

    public void SetKnowledgeProfile(Entity<HumanoidProfileComponent> ent, KnowledgeProfile profile)
    {
        ent.Comp.Knowledge = profile;

        var parent = ProtoMan.Index(ent.Comp.Species).Knowledge;
        _knowledge.ApplyProfile(ent, parent, profile);
    }
}
