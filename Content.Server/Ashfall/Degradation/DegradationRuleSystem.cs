using Content.Server.GameTicking.Rules;
using Content.Server.Station.Events;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;
using Ashfall.Server.Degradation.Components;
using Ashfall.Shared.Degradation;

namespace Ashfall.Server.Degradation;

/// <summary>
/// Selects degradation passes when the rule is added and records their manifest on each initialized station.
/// </summary>
public sealed partial class DegradationRuleSystem : GameRuleSystem<DegradationRuleComponent>
{
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationPostInitEvent>(OnStationPostInit);
    }

    protected override void Added(
        EntityUid uid,
        DegradationRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleAddedEvent args)
    {
        var profile = _prototype.Index(component.Profile);
        var seed = component.Seed ?? RobustRandom.Next();
        var scenario = DegradationScenarioSelector.Select(profile, GameTicker.ReadyPlayerCount(), seed);
        component.Scenario = scenario;

        if (scenario.MissingRequiredTags.Count > 0)
        {
            Log.Error(
                $"Degradation profile {profile.ID} cannot satisfy required tags: " +
                string.Join(", ", scenario.MissingRequiredTags));
            GameTicker.EndGameRule(uid, gameRule);
            return;
        }

        foreach (var fault in scenario.Faults)
        {
            if (GameTicker.AddFilteredGameRule(fault.Rule) == null)
            {
                Log.Warning($"Degradation fault {fault.Id} ({fault.Rule}) was ignored and will not be applied.");
                continue;
            }

            component.ActivatedFaults.Add(fault.Id);
            component.ActivatedRules.Add(fault.Rule);
        }

        Log.Info(
            $"Built degradation scenario {profile.ID}, seed {scenario.Seed}, " +
            $"players {scenario.ReadyPlayers}, budget {scenario.Budget}: " +
            string.Join(", ", component.ActivatedFaults));
    }

    private void OnStationPostInit(ref StationPostInitEvent ev)
    {
        var query = EntityQueryEnumerator<DegradationRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var rule, out _))
        {
            if (HasComp<EndedGameRuleComponent>(uid) || rule.Scenario == null)
                continue;

            var state = EnsureComp<DegradationStateComponent>(ev.Station);
            state.Profile = rule.Profile.Id;
            state.Seed = rule.Scenario.Seed;
            state.Budget = rule.Scenario.Budget;
            state.ReadyPlayers = rule.Scenario.ReadyPlayers;
            state.Faults = new List<string>(rule.ActivatedFaults);
            state.Rules = new List<EntProtoId>(rule.ActivatedRules);
            return;
        }
    }
}
