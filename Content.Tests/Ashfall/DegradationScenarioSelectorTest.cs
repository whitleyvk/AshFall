#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Ashfall.Server.Degradation;
using Ashfall.Shared.Degradation;

namespace Content.Tests.Ashfall;

[TestFixture]
[TestOf(typeof(DegradationScenarioSelector))]
public sealed class DegradationScenarioSelectorTest
{
    [Test]
    public void SameSeedProducesSameManifest()
    {
        var profile = Profile(5,
            Fault("major-a", 3, tags: ["major"]),
            Fault("major-b", 3, tags: ["major"]),
            Fault("minor-a", 1),
            Fault("minor-b", 1));
        profile.RequiredTags.Add("major");

        var first = DegradationScenarioSelector.Select(profile, 30, 1488);
        var second = DegradationScenarioSelector.Select(profile, 30, 1488);

        Assert.That(first.Faults.Select(fault => fault.Id),
            Is.EqualTo(second.Faults.Select(fault => fault.Id)));
    }

    [Test]
    public void PopulationSelectsTheCorrectBudgetAndEligibility()
    {
        var profile = Profile(5,
            Fault("major", 3, tags: ["major"]),
            Fault("high-pop", 2, minPlayers: 16));
        profile.LowPopulationBudget = 3;
        profile.LowPopulationThreshold = 15;
        profile.RequiredTags.Add("major");

        var lowPopulation = DegradationScenarioSelector.Select(profile, 10, 42);
        var standardPopulation = DegradationScenarioSelector.Select(profile, 20, 42);

        Assert.Multiple(() =>
        {
            Assert.That(lowPopulation.Budget, Is.EqualTo(3));
            Assert.That(lowPopulation.Faults.Select(fault => fault.Id), Is.EqualTo(["major"]));
            Assert.That(standardPopulation.Budget, Is.EqualTo(5));
            Assert.That(standardPopulation.Faults.Select(fault => fault.Id),
                Is.EquivalentTo(new[] { "major", "high-pop" }));
        });
    }

    [Test]
    public void IncompatibleFaultsAreNeverSelectedTogether()
    {
        var first = Fault("first", 1);
        first.IncompatibleWith.Add("second");
        var profile = Profile(2, first, Fault("second", 1));

        var scenario = DegradationScenarioSelector.Select(profile, 30, 7);

        Assert.That(scenario.Faults, Has.Count.EqualTo(1));
    }

    [Test]
    public void UnsatisfiedRequiredTagIsReported()
    {
        var profile = Profile(2, Fault("cosmetic", 1, tags: ["cosmetic"]));
        profile.RequiredTags.Add("major");

        var scenario = DegradationScenarioSelector.Select(profile, 30, 1);

        Assert.That(scenario.MissingRequiredTags, Is.EqualTo(["major"]));
    }

    private static DegradationProfilePrototype Profile(
        int standardBudget,
        params DegradationFaultEntry[] faults)
    {
#pragma warning disable RA0039 // Pure selector tests intentionally use an in-memory profile fixture.
        return new DegradationProfilePrototype
        {
            LowPopulationBudget = standardBudget,
            StandardBudget = standardBudget,
            Faults = new List<DegradationFaultEntry>(faults),
        };
#pragma warning restore RA0039
    }

    private static DegradationFaultEntry Fault(
        string id,
        int cost,
        int minPlayers = 0,
        string[]? tags = null)
    {
        return new DegradationFaultEntry
        {
            Id = id,
            Rule = "AshfallExtendedRule",
            Cost = cost,
            MinPlayers = minPlayers,
            Tags = tags == null ? new HashSet<string>() : new HashSet<string>(tags),
        };
    }
}
