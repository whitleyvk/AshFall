using Content.Client.Ashfall.CharacterGen.UI;
using Content.Client.Trauma.Knowledge;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.IntegrationTests.Tests.Ashfall;

public sealed class AshfallSkillsWindowTests : GameTest
{
    [Test]
    [RunOnSide(Side.Client)]
    public void DetailsWindowOpensWithSkills()
    {
        using var window = new AshfallSkillsDetailWindow(CProtoMan, CEntMan.System<KnowledgeSystem>());
        window.Populate(HumanoidCharacterProfile.DefaultWithSpecies(), CProtoMan.Index<JobPrototype>("MedicalDoctor"));
        window.OpenCentered();
        Assert.That(window.IsOpen, Is.True);
        Assert.That(window.FindControl<BoxContainer>("CategoriesContainer").ChildCount, Is.GreaterThan(0));
        window.Close();
        Assert.That(window.IsOpen, Is.False);
    }
}
