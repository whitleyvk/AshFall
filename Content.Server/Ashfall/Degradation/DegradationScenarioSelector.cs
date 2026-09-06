using System.Linq;
using Robust.Shared.Random;
using Ashfall.Shared.Degradation;

namespace Ashfall.Server.Degradation;

public sealed record DegradationScenario(
    int Seed,
    int Budget,
    int ReadyPlayers,
    IReadOnlyList<DegradationFaultEntry> Faults,
    IReadOnlyList<string> MissingRequiredTags);

/// <summary>
/// Builds a deterministic, budgeted set of compatible station faults.
/// </summary>
public static class DegradationScenarioSelector
{
    public static DegradationScenario Select(
        DegradationProfilePrototype profile,
        int readyPlayers,
        int seed)
    {
        var budget = readyPlayers <= profile.LowPopulationThreshold
            ? profile.LowPopulationBudget
            : profile.StandardBudget;

        var random = new RobustRandom();
        random.SetSeed(seed);

        var selected = new List<DegradationFaultEntry>();
        var selectedIds = new HashSet<string>(StringComparer.Ordinal);
        var missingTags = new List<string>();
        var spent = 0;

        foreach (var tag in profile.RequiredTags)
        {
            var candidates = Eligible(profile, selected, selectedIds, readyPlayers, budget - spent, tag);
            var picked = PickWeighted(candidates, random);
            if (picked == null)
            {
                missingTags.Add(tag);
                continue;
            }

            selected.Add(picked);
            selectedIds.Add(picked.Id);
            spent += picked.Cost;
        }

        while (spent < budget)
        {
            var candidates = Eligible(profile, selected, selectedIds, readyPlayers, budget - spent);
            var picked = PickWeighted(candidates, random);
            if (picked == null)
                break;

            selected.Add(picked);
            selectedIds.Add(picked.Id);
            spent += picked.Cost;
        }

        return new DegradationScenario(seed, budget, readyPlayers, selected, missingTags);
    }

    private static List<DegradationFaultEntry> Eligible(
        DegradationProfilePrototype profile,
        List<DegradationFaultEntry> selected,
        HashSet<string> selectedIds,
        int readyPlayers,
        int remainingBudget,
        string? requiredTag = null)
    {
        var result = new List<DegradationFaultEntry>();

        foreach (var fault in profile.Faults)
        {
            if (selectedIds.Contains(fault.Id) || fault.Cost <= 0 || fault.Cost > remainingBudget)
                continue;

            if (fault.Weight <= 0f ||
                readyPlayers < fault.MinPlayers ||
                fault.MaxPlayers is { } maxPlayers && readyPlayers > maxPlayers)
                continue;

            if (requiredTag != null && !fault.Tags.Contains(requiredTag))
                continue;

            if (selected.Any(existing =>
                    existing.IncompatibleWith.Contains(fault.Id) ||
                    fault.IncompatibleWith.Contains(existing.Id)))
            {
                continue;
            }

            result.Add(fault);
        }

        return result;
    }

    private static DegradationFaultEntry? PickWeighted(
        IReadOnlyList<DegradationFaultEntry> candidates,
        IRobustRandom random)
    {
        if (candidates.Count == 0)
            return null;

        var totalWeight = candidates.Sum(candidate => candidate.Weight);
        var roll = random.NextFloat(totalWeight);

        foreach (var candidate in candidates)
        {
            roll -= candidate.Weight;
            if (roll <= 0f)
                return candidate;
        }

        return candidates[^1];
    }
}
