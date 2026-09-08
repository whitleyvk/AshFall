using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Common.Body;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Surgery.Tools;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.FixedPoint;
using Content.Shared.Trauma.Cybernetics;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Ashfall;

public sealed class TraumaMedicalTests : GameTest
{
    private static readonly ProtoId<OrganCategoryPrototype> Head = "Head";
    private static readonly ProtoId<OrganCategoryPrototype> Heart = "Heart";

    [TestCase("Scalpel")]
    [TestCase("Hemostat")]
    [TestCase("Retractor")]
    [TestCase("Cautery")]
    [TestCase("Saw")]
    [RunOnSide(Side.Server)]
    public void SurgicalToolsHaveAudio(string prototype)
    {
        var tool = SSpawn(prototype);
        var component = SComp<SurgeryToolComponent>(tool);
        Assert.That(component.StartSound, Is.Not.Null);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void CyberneticOrganDisablesUntilEmpEnds()
    {
        var bodySystem = SEntMan.System<BodySystem>();
        var human = SSpawn("MobHuman");
        var heart = bodySystem.GetOrgan(human, Heart)!.Value;
        var cybernetics = SEntMan.AddComponent<CyberneticsComponent>(heart);
        cybernetics.EmpDamage = new DamageSpecifier();
        Assert.That(SEntMan.HasComponent<EnabledOrganComponent>(heart), Is.True);

        var pulse = new EmpPulseEvent(0, false, false, TimeSpan.FromSeconds(5), null);
        SEntMan.EventBus.RaiseLocalEvent(heart, ref pulse);
        Assert.That(pulse.Disabled, Is.True);
        Assert.That(bodySystem.EnableOrgan(heart), Is.False);

        var ended = new EmpDisabledRemovedEvent();
        SEntMan.EventBus.RaiseLocalEvent(heart, ref ended);
        Assert.That(cybernetics.Disabled, Is.False);
        Assert.That(SEntMan.HasComponent<EnabledOrganComponent>(heart), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void TargetedWoundCanBeHealed()
    {
        var human = SSpawn("MobHuman");
        var body = SEntMan.System<BodySystem>();
        var wounds = SEntMan.System<WoundSystem>();
        var damage = SEntMan.System<DamageableSystem>();
        var head = body.GetOrgan(human, Head)!.Value;
        var amount = new DamageSpecifier { DamageDict = new() { { "Heat", FixedPoint2.New(20) } } };
        damage.ChangeDamage(human, amount, targetPart: TargetBodyPart.Head, canMiss: false);
        Assert.That(wounds.GetWoundableWounds(head), Has.Count.EqualTo(1));
        Assert.That(damage.GetTotalDamage(human), Is.EqualTo(FixedPoint2.New(20)));
        damage.ChangeDamage(human, -amount, targetPart: TargetBodyPart.Head, canMiss: false);
        Assert.That(wounds.GetWoundableWounds(head), Is.Empty);
        Assert.That(damage.GetTotalDamage(human), Is.EqualTo(FixedPoint2.Zero));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void SurgeryAndStatusMessagesResolve()
    {
        var human = SSpawn("MobHuman");
        var tool = SSpawn("Scalpel");
        var head = SEntMan.System<BodySystem>().GetOrgan(human, Head)!.Value;
        var message = Loc.GetString("surgery-popup-step-SurgeryStepOpenIncisionScalpel",
            ("user", human), ("target", Loc.GetString("surgery-popup-self")), ("part", head), ("tool", tool));
        Assert.That(message, Does.Not.Contain("{$"));
        Assert.That(message, Does.Contain(SEntMan.GetComponent<MetaDataComponent>(tool).EntityName));
        Assert.That(Loc.GetString("inspect-part-status-title"), Does.Not.Contain("inspect-part-status-title"));
        Assert.That(Loc.GetString("inspect-part-status-line", ("part", "голова"), ("status", "без повреждений"), ("possessive", "")), Does.Contain("голова"));
        Assert.That(Loc.GetString("examine-border-line"), Does.Not.Contain("examine-border-line"));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void JobKnowledgeUsesFloorsWithoutBreakingMasteryGains()
    {
        var human = SSpawn("MobHuman");
        var knowledge = SEntMan.System<SharedKnowledgeSystem>();
        var container = knowledge.EnsureKnowledgeContainer(human);
        EntProtoId skill = "StrengthKnowledge";

        knowledge.RaiseMastery(container, skill, 1, popup: false);
        knowledge.RaiseMastery(container, skill, 1, popup: false);
        Assert.That(knowledge.GetMastery(knowledge.GetKnowledge(container, skill)!.Value.Owner), Is.EqualTo(2));

        knowledge.ApplyJobFloors(human, new Dictionary<EntProtoId, int> { [skill] = 1 });
        Assert.That(knowledge.GetMastery(knowledge.GetKnowledge(container, skill)!.Value.Owner), Is.EqualTo(2));

        knowledge.ApplyJobFloors(human, new Dictionary<EntProtoId, int> { [skill] = 3 });
        Assert.That(knowledge.GetMastery(knowledge.GetKnowledge(container, skill)!.Value.Owner), Is.EqualTo(3));
    }
}
