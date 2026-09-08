// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Systems;

/// <summary>
/// Trauma - Provides missing API methods for bloodstream.
/// </summary>
public sealed partial class BloodstreamSystem
{
    [Dependency] private EntityQuery<BloodstreamComponent> _query = default!;

    public void SetRefreshAmount(Entity<BloodstreamComponent> ent, FixedPoint2 amount)
    {
        ent.Comp.BloodRefreshAmount = amount;
        DirtyField(ent.AsNullable(), nameof(BloodstreamComponent.BloodRefreshAmount));
    }

    /// <summary>
    /// Removes a certain amount of all reagents except of excluded ones from the bloodstream.
    /// </summary>
    public Solution? FlushChemicals(Entity<BloodstreamComponent?> ent,
        FixedPoint2 quantity,
        params ProtoId<ReagentPrototype>[] excludedReagents)
    {
        if (!_query.Resolve(ent, ref ent.Comp, logMissing: false)
            || !_solutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
            return null;

        var flushedSolution = new Solution();

        for (var i = bloodSolution.Contents.Count - 1; i >= 0; i--)
        {
            var (reagentId, _) = bloodSolution.Contents[i];
            if (ent.Comp.BloodReferenceSolution.ContainsPrototype(reagentId.Prototype) ||
                excludedReagents.Contains(reagentId.Prototype))
                continue;

            var reagentFlushAmount = _solutionContainer.RemoveReagent(ent.Comp.BloodSolution.Value, reagentId, quantity);
            flushedSolution.AddReagent(reagentId, reagentFlushAmount);
        }

        return flushedSolution.Volume == 0 ? null : flushedSolution;
    }

}
