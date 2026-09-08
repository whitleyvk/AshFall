// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// Provides API for working with <see cref="KnowledgeProfile"/>.
/// </summary>
public abstract partial class SharedKnowledgeSystem
{
    private readonly List<EntProtoId> _invalid = new();

    public override void EnsureProfileValid([ForbidLiteral] ProtoId<KnowledgeProfilePrototype> parentId, ref KnowledgeProfile profile)
    {
        if (!ProtoMan.TryIndex(parentId, out var parent))
            return;

        _invalid.Clear();
        foreach (var (id, mastery) in profile.Mastery)
        {
            var net = mastery + parent.Profile.Mastery.GetValueOrDefault(id);
            if (net < 0 || SkillCost(id, net) == null)
                _invalid.Add(id);
        }

        foreach (var id in _invalid)
        {
            profile.Mastery.Remove(id);
        }
    }

    public override void ApplyProfile(EntityUid target, [ForbidLiteral] ProtoId<KnowledgeProfilePrototype> parentId, KnowledgeProfile profile)
    {
        var ent = EnsureKnowledgeContainer(target);
        if (!ProtoMan.TryIndex(parentId, out var parent))
            return;

        ApplyProfile(ent, parent.Profile);
        ApplyProfile(ent, profile, parent.PointsLimit);
    }

    /// <summary>
    /// Applies a knowledge profile to a given knowledge container, not using points.
    /// </summary>
    public void ApplyProfile(Entity<KnowledgeContainerComponent> ent, KnowledgeProfile profile)
    {
        foreach (var (id, mastery) in profile.Mastery)
        {
            if (RaiseMastery(ent, id, mastery, popup: false) == null)
            {
                Log.Error($"Failed to give {ToPrettyString(ent.Comp.Holder)} knowledge {id}!");
                continue;
            }
        }
    }

    /// <summary>
    /// Applies a knowledge profile to a given knowledge container, using limited points.
    /// </summary>
    public void ApplyProfile(Entity<KnowledgeContainerComponent> ent, KnowledgeProfile profile, int points)
    {
        foreach (var (id, mastery) in profile.Mastery)
        {
            if (SkillCost(id, mastery) is not { } cost || points < cost)
                return;

            if (RaiseMastery(ent, id, mastery, popup: false) == null)
            {
                Log.Error($"Failed to give {ToPrettyString(ent.Comp.Holder)} knowledge {id}!");
                continue;
            }

            points -= cost;
        }
    }

    public override int ProfileCost(KnowledgeProfile profile)
    {
        var total = 0;
        foreach (var (id, mastery) in profile.Mastery)
        {
            total += SkillCost(id, mastery) ?? 0;
        }
        return total;
    }

    /// <summary>
    /// Gets the costs to have a skill at each allowed mastery level.
    /// Returns null if the skill cannot be picked.
    /// </summary>
    public int[]? SkillCosts(EntProtoId id)
        => AllKnowledges.TryGetValue(id, out var comp) && comp.Costs is { } costs
            ? costs
            : null;

    /// <summary>
    /// Gets the cost to have a skill at a given mastery level.
    /// Returns null if the skill cannot be picked or the mastery is invalid.
    /// </summary>
    public int? SkillCost(EntProtoId id, int mastery)
        => SkillCosts(id) is { } costs && mastery >= 0 && mastery < costs.Length
            ? costs[mastery]
            : null;
}
