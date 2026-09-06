using System.IO;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Ashfall.CharacterGen;
using Content.Shared.Ashfall.CharacterGen.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.IntegrationTests.Tests.Ashfall;

[TestFixture]
public sealed class AshfallCharacterGenTests : GameTest
{
    private static readonly ProtoId<CharacterGenConstraintsPrototype> HumanConstraints = "HumanDefaultConstraints";

    [SidedDependency(Side.Server)] private IPrototypeManager _protoMan = default!;
    [SidedDependency(Side.Server)] private MarkingManager _markingMan = default!;
    [SidedDependency(Side.Server)] private NamingSystem _namingSys = default!;
    [SidedDependency(Side.Server)] private IRobustRandom _random = default!;
    [SidedDependency(Side.Server)] private IRobustSerializer _serializer = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void FuzzHumanCharacterGenerationTest()
    {
        var generator = new AshfallCharacterGenerator(_protoMan, _markingMan, _namingSys);

        Assert.That(_protoMan.TryIndex(HumanConstraints, out var constraints), Is.True);
        Assert.That(constraints, Is.Not.Null);

        var species = _protoMan.Index<SpeciesPrototype>(constraints!.Species);
        Assert.That(_protoMan.TryIndex(species.SkinColoration, out var colorationProto), Is.True);
        var strategy = colorationProto!.Strategy;

        var youngCount = 0;
        var youngGrayCount = 0;
        var oldCount = 0;
        var oldGrayCount = 0;

        for (var i = 0; i < 2000; i++)
        {
            var profile = generator.GenerateProfile(constraints, _random);

            // 1. Species is Human
            Assert.That(profile.Species.Id, Is.EqualTo("Human"));

            // 2. Age is within valid range
            Assert.That(profile.Age, Is.GreaterThanOrEqualTo(18));
            Assert.That(profile.Age, Is.LessThanOrEqualTo(75));

            // 3. Name is generated
            Assert.That(string.IsNullOrWhiteSpace(profile.Name), Is.False);

            // 4. Skin tone satisfies natural human melanin curve
            Assert.That(strategy.VerifySkinColor(profile.Appearance.SkinColor, out var reason), Is.True,
                $"Skin color {profile.Appearance.SkinColor} failed verification: {reason}");

            // 5. Eye color is not pure black or zero alpha
            Assert.That(profile.Appearance.EyeColor.A, Is.EqualTo(1.0f));

            // 6. Hair marking exists
            Assert.That(profile.Appearance.Markings.ContainsKey("Head"), Is.True);
            var headMarkings = profile.Appearance.Markings["Head"];
            Assert.That(headMarkings.ContainsKey(HumanoidVisualLayers.Hair), Is.True);
            Assert.That(headMarkings[HumanoidVisualLayers.Hair].Count, Is.GreaterThan(0));

            // 7. Track age vs gray hair distribution
            var hairColor = headMarkings[HumanoidVisualLayers.Hair][0].MarkingColors[0];
            var hsv = Color.ToHsv(hairColor);
            var isGray = hsv.Y < 0.15f && hsv.Z > 0.35f;

            if (profile.Age < 30)
            {
                youngCount++;
                if (isGray) youngGrayCount++;
            }
            else if (profile.Age >= 60)
            {
                oldCount++;
                if (isGray) oldGrayCount++;
            }

            // 8. Facial hair check for males
            if (profile.Sex == Sex.Male && headMarkings.TryGetValue(HumanoidVisualLayers.FacialHair, out var facialList) && facialList.Count > 0)
            {
                var facialColor = facialList[0].MarkingColors[0];
                // Facial hair color should be close to hair color
                Assert.That(Math.Abs(facialColor.R - hairColor.R), Is.LessThan(0.15f));
            }
        }

        // Statistical assertions on age graying curve
        if (youngCount > 0 && oldCount > 0)
        {
            var youngGrayRate = (float)youngGrayCount / youngCount;
            var oldGrayRate = (float)oldGrayCount / oldCount;
            Assert.That(oldGrayRate, Is.GreaterThan(youngGrayRate),
                $"Elder gray rate ({oldGrayRate:P1}) should be significantly higher than young gray rate ({youngGrayRate:P1})");
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void GeneratedProfileCanBeNetworkSerializedTest()
    {
        Assert.That(_protoMan.TryIndex(HumanConstraints, out var constraints), Is.True);

        var generator = new AshfallCharacterGenerator(_protoMan, _markingMan, _namingSys);
        var expected = generator.GenerateProfile(constraints!, _random);

        using var stream = new MemoryStream();
        _serializer.SerializeDirect(stream, expected);
        stream.Position = 0;
        _serializer.DeserializeDirect(stream, out HumanoidCharacterProfile actual);

        Assert.That(actual.Name, Is.EqualTo(expected.Name));
        Assert.That(actual.Appearance, Is.EqualTo(expected.Appearance));
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void LoreLibraryHasRequiredCoverageTest()
    {
        var fragments = _protoMan.EnumeratePrototypes<AshfallCharacterLoreFragmentPrototype>().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(fragments.Count(x => x.Category == AshfallCharacterLoreCategory.Origin), Is.GreaterThanOrEqualTo(20));
            Assert.That(fragments.Count(x => x.Category is AshfallCharacterLoreCategory.Education or AshfallCharacterLoreCategory.Qualification), Is.GreaterThanOrEqualTo(24));
            Assert.That(fragments.Count(x => x.Category == AshfallCharacterLoreCategory.Career), Is.GreaterThanOrEqualTo(36));
            Assert.That(fragments.Count(x => x.Category == AshfallCharacterLoreCategory.Personality), Is.GreaterThanOrEqualTo(32));
            Assert.That(fragments.Count(x => x.Category == AshfallCharacterLoreCategory.Evaluation), Is.GreaterThanOrEqualTo(32));
            Assert.That(fragments.Count(x => x.Category == AshfallCharacterLoreCategory.PersonalHook), Is.GreaterThanOrEqualTo(44));
            Assert.That(fragments.Count(x => x.Category == AshfallCharacterLoreCategory.PreCryo), Is.GreaterThanOrEqualTo(16));
        });

        var qualifications = fragments.Where(x => x.Category == AshfallCharacterLoreCategory.Qualification).ToArray();
        Assert.That(qualifications.Select(x => x.ProfessionalFamily).Distinct().Count(), Is.GreaterThanOrEqualTo(4));
        foreach (var qualification in qualifications)
        {
            Assert.That(qualification.Jobs, Has.Count.InRange(2, 5), qualification.ID);
            foreach (var jobId in qualification.Jobs)
            {
                Assert.That(_protoMan.TryIndex(jobId, out JobPrototype job), Is.True, $"{qualification.ID}: {jobId}");
                Assert.That(job!.SetPreference, Is.True, $"{qualification.ID}: {jobId}");
                Assert.That(job.JobEntity, Is.Null, $"{qualification.ID}: {jobId}");
            }
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void LoreGenerationIsCompleteAndDeterministicTest()
    {
        var generator = new AshfallCharacterLoreGenerator(_protoMan);
        var profile = new HumanoidCharacterProfile { Age = 38, Name = "Test Employee" };
        var firstRandom = new RobustRandom();
        var secondRandom = new RobustRandom();
        firstRandom.SetSeed(1488);
        secondRandom.SetSeed(1488);

        var first = generator.GenerateCandidate(profile, firstRandom);
        var second = generator.GenerateCandidate(profile, secondRandom);
        Assert.That(DossierIds(first), Is.EqualTo(DossierIds(second)));

        var random = new RobustRandom();
        random.SetSeed(42);
        for (var i = 0; i < 3000; i++)
        {
            profile = profile.WithAge(18 + i % 58);
            var candidate = generator.GenerateCandidate(profile, random);
            Assert.Multiple(() =>
            {
                Assert.That(candidate.ProfessionalFamily, Is.Not.Empty);
                Assert.That(candidate.CompatibleJobs, Is.Not.Empty);
                Assert.That(candidate.Dossier.Qualifications, Is.Not.Empty);
                Assert.That(candidate.Dossier.Career, Is.Not.Empty);
                Assert.That(candidate.Dossier.Evaluations, Is.Not.Empty);
            });
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void CandidateCanBeNetworkSerializedTest()
    {
        Assert.That(_protoMan.TryIndex(HumanConstraints, out var constraints), Is.True);
        var profileGenerator = new AshfallCharacterGenerator(_protoMan, _markingMan, _namingSys);
        var loreGenerator = new AshfallCharacterLoreGenerator(_protoMan);
        var expected = loreGenerator.GenerateCandidate(profileGenerator.GenerateProfile(constraints!, _random), _random);

        using var stream = new MemoryStream();
        _serializer.SerializeDirect(stream, expected);
        stream.Position = 0;
        _serializer.DeserializeDirect(stream, out AshfallCharacterCandidate actual);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Profile.Name, Is.EqualTo(expected.Profile.Name));
            Assert.That(actual.ProfessionalFamily, Is.EqualTo(expected.ProfessionalFamily));
            Assert.That(actual.CompatibleJobs, Is.EqualTo(expected.CompatibleJobs));
            Assert.That(DossierIds(actual), Is.EqualTo(DossierIds(expected)));
        });
    }

    private static string[] DossierIds(AshfallCharacterCandidate candidate)
    {
        return new[]
            {
                candidate.Dossier.Origin.Id,
                candidate.Dossier.Education.Id,
                candidate.Dossier.Personality.Id,
                candidate.Dossier.PersonalHook.Id,
                candidate.Dossier.PreCryo.Id,
            }
            .Concat(candidate.Dossier.Qualifications.Select(x => x.Id))
            .Concat(candidate.Dossier.Career.Select(x => x.Id))
            .Concat(candidate.Dossier.Evaluations.Select(x => x.Id))
            .ToArray();
    }
}
