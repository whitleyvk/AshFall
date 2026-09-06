using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Ashfall.CharacterGen;
using Content.Shared.Ashfall.CharacterGen.Prototypes;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid;
using Content.Shared.Roles;
using NUnit.Framework;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests.Ashfall;

/// <summary>
///     Career coherence: generated employees must be describable in one short phrase. Tests the
///     career spine (one primary family, adjacent specialization, at most one retraining),
///     current-vs-historical competence, and the resulting focused assignment lists.
/// </summary>
[TestFixture]
public sealed class AshfallCareerCoherenceTests : GameTest
{
    private static readonly ProtoId<CharacterGenConstraintsPrototype> HumanConstraints = "HumanDefaultConstraints";
    private static readonly ProtoId<CharacterGenConstraintsPrototype> ReptilianConstraints = "ReptilianDefaultConstraints";
    private static readonly ProtoId<CharacterGenConstraintsPrototype> MothConstraints = "MothDefaultConstraints";
    private static readonly ProtoId<CharacterGenConstraintsPrototype> ArachnidConstraints = "ArachnidDefaultConstraints";
    private static readonly ProtoId<CharacterGenConstraintsPrototype> VeiruConstraints = "VeiruDefaultConstraints";

    [SidedDependency(Side.Server)] private IPrototypeManager _protoMan = default!;
    [SidedDependency(Side.Server)] private MarkingManager _markingMan = default!;
    [SidedDependency(Side.Server)] private NamingSystem _namingSys = default!;

    private AshfallPersonGenerator PersonGenerator =>
        _personGenerator ??= new AshfallPersonGenerator(
            _protoMan,
            new AshfallCharacterGenerator(_protoMan, _markingMan, _namingSys));

    private AshfallPersonGenerator _personGenerator = default!;

    [Test]
    [RunOnSide(Side.Server)]
    public void HistoricalTrackDoesNotGrantAssignmentsTest()
    {
        // An engineer of 8 years who retrained into medicine and worked 7 more years: the
        // engineering history stays in the dossier, but the current identity is medical.
        var person = MakePerson("AshfallEducationMedical",
            ("Engineering", 15f), ("Electrical", 8f), ("Medical", 4.5f), ("ClinicalMedicine", 4.0f));
        person.ActiveCompetencies.UnionWith(new[]
        {
            new ProtoId<AshfallCompetencyPrototype>("Medical"),
            new ProtoId<AshfallCompetencyPrototype>("ClinicalMedicine"),
        });
        person.HasActiveRestriction = true;

        bool Valid(string jobId) => AshfallJobScorer.Score(person, _protoMan.Index<AshfallJobCareerPrototype>(jobId), _protoMan) >= 0;

        Assert.Multiple(() =>
        {
            Assert.That(Valid("MedicalDoctor"), Is.True, "current medical track: doctor");
            Assert.That(Valid("StationEngineer"), Is.False, "abandoned engineering track must not grant jobs");
            Assert.That(Valid("AtmosphericTechnician"), Is.False, "abandoned engineering track");
            Assert.That(Valid("MedicalIntern"), Is.False, "established doctor is overqualified for intern");
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void GenericMedicalDoesNotUnlockPsychologistTest()
    {
        var medic = MakePerson("AshfallEducationMedical", ("Medical", 9f), ("ClinicalMedicine", 9f));
        bool Valid(string jobId) => AshfallJobScorer.Score(medic, _protoMan.Index<AshfallJobCareerPrototype>(jobId), _protoMan) >= 0;
        Assert.That(Valid("Psychologist"), Is.False, "psychology requires actual psychology training");

        medic.AddExperience("Psychology", 4.5f, AshfallProvenanceSource.Education, "Fixture");
        Assert.That(Valid("Psychologist"), Is.True, "qualified psychologist");
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void IncidentalElectricalDoesNotUnlockEngineerTest()
    {
        // An agricultural worker who picked up some electrical experience incidentally.
        var farmer = MakePerson("AshfallEducationAgricultural",
            ("Agriculture", 9f), ("Botany", 9f), ("Electrical", 6f));
        bool Valid(string jobId) => AshfallJobScorer.Score(farmer, _protoMan.Index<AshfallJobCareerPrototype>(jobId), _protoMan) >= 0;
        Assert.Multiple(() =>
        {
            Assert.That(Valid("Botanist"), Is.True, "botanist");
            Assert.That(Valid("StationEngineer"), Is.False, "electrical alone must not grant engineer");
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void SeasonedMedicIsDoctorNotInternTest()
    {
        // Seven years of clinical work on top of medical college must qualify as a doctor.
        var medic = MakePerson("AshfallEducationMedical", ("Medical", 7f), ("ClinicalMedicine", 6f));
        bool Valid(string jobId) => AshfallJobScorer.Score(medic, _protoMan.Index<AshfallJobCareerPrototype>(jobId), _protoMan) >= 0;
        Assert.Multiple(() =>
        {
            Assert.That(Valid("MedicalDoctor"), Is.True, "7-year medic is a doctor");
            Assert.That(Valid("MedicalIntern"), Is.False, "established medic is overqualified for intern");
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void TramDriverIsNeverGeneratedTest()
    {
        Assert.That(_protoMan.HasIndex<AshfallJobCareerPrototype>("TramDriver"), Is.False,
            "tram driver was deliberately removed from generation");
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void CareerCoherenceDistributionTest()
    {
        var constraintsList = new[]
        {
            HumanConstraints, ReptilianConstraints, MothConstraints, ArachnidConstraints, VeiruConstraints,
        };

        var jobsHistogram = new Dictionary<int, int>();
        var familiesHistogram = new Dictionary<int, int>();
        var stintsHistogram = new Dictionary<int, int>();
        var retrained = 0;
        var passengerOnly = 0;
        var heads = 0;
        var total = 0;
        var sumJobs = 0;

        var headJobs = new HashSet<string>
        {
            "Captain", "HeadOfPersonnel", "HeadOfSecurity", "ChiefEngineer",
            "ChiefMedicalOfficer", "ResearchDirector", "Quartermaster",
        };

        for (var seed = 0; seed < 3000; seed++)
        {
            var random = new RobustRandom();
            random.SetSeed(seed);
            var constraints = _protoMan.Index(constraintsList[seed % constraintsList.Length]);
            var (candidate, structure) = PersonGenerator.GenerateCandidate(constraints, random);
            total++;
            sumJobs += candidate.CompatibleJobs.Count;

            var jobCount = candidate.CompatibleJobs.Count;
            jobsHistogram[jobCount] = jobsHistogram.GetValueOrDefault(jobCount) + 1;

            var familyCount = structure.CareerFamilies.Count;
            familiesHistogram[familyCount] = familiesHistogram.GetValueOrDefault(familyCount) + 1;

            var stintCount = structure.Career.Count;
            stintsHistogram[stintCount] = stintsHistogram.GetValueOrDefault(stintCount) + 1;

            Assert.Multiple(() =>
            {
                // A life trajectory: one primary family, an adjacent specialization, and at most
                // one deliberate retraining. Never a sprawl of unrelated domains.
                Assert.That(structure.RetrainingCount, Is.LessThanOrEqualTo(1), $"seed {seed}");
                Assert.That(structure.CareerFamilies.Count, Is.LessThanOrEqualTo(3), $"seed {seed}");
                Assert.That(structure.Career.Count, Is.LessThanOrEqualTo(4), $"seed {seed}");
                Assert.That(candidate.CompatibleJobs.Count, Is.LessThanOrEqualTo(6), $"seed {seed}");

                // Every career stint stays inside the allowed families.
                foreach (var stint in structure.Career)
                {
                    var role = _protoMan.Index(stint.Role);
                    Assert.That(role.Domains.Overlaps(structure.CareerFamilies), Is.True,
                        $"seed {seed}: stint {role.ID} outside career families");
                }

                // Assignment lists are focused: no candidate ends up with a cross-department sprawl.
                var jobDomains = candidate.CompatibleJobs
                    .Select(j => _protoMan.Index<AshfallJobCareerPrototype>(j).Domain)
                    .Distinct()
                    .ToList();
                Assert.That(jobDomains.Count, Is.LessThanOrEqualTo(4), $"seed {seed}: job domains {string.Join(",", jobDomains)}");

                // Ladder coherence: every stint's requirements are covered by the life history
                // (origin, education and the stints that came before it).
                foreach (var stint in structure.Career)
                {
                    var role = _protoMan.Index(stint.Role);
                    Assert.That(role.RequiredTags.IsSubsetOf(structure.StructureTags), Is.True,
                        $"seed {seed}: stint {role.ID} requires {string.Join(",", role.RequiredTags)} not provided by the history");
                }

                // Formed careers for adults with room to work.
                if (structure.AvailableCareerYears >= 2)
                {
                    Assert.That(structure.Career, Is.Not.Empty, $"seed {seed}: adult candidate without a career");
                }
            });

            if (structure.RetrainingCount > 0)
                retrained++;

            if (candidate.CompatibleJobs.Any(j => headJobs.Contains(j.Id)))
                heads++;


            if (candidate.CompatibleJobs.All(j => _protoMan.Index<AshfallJobCareerPrototype>(j).FallbackOnly))
            {
                passengerOnly++;
                if (passengerOnly <= 10)
                    TestContext.Out.WriteLine($"PASSENGER seed={seed} species={structure.SpeciesId} age={structure.Age} " +
                        $"edu={structure.Education} stints={structure.Career.Count} " +
                        $"comps=[{string.Join(",", structure.Competencies.Select(c => c.Key.Id + ":" + c.Value.Experience.ToString("0.0")))}]");
            }
        }

        TestContext.Out.WriteLine($"candidates={total}");
        TestContext.Out.WriteLine($"jobs histogram: {Format(jobsHistogram)} (avg {sumJobs / (float)total:F2})");
        TestContext.Out.WriteLine($"career families histogram: {Format(familiesHistogram)}");
        TestContext.Out.WriteLine($"stints histogram: {Format(stintsHistogram)}");
        TestContext.Out.WriteLine($"retrained: {retrained / (float)total:P1}, passenger-only: {passengerOnly / (float)total:P1}");

        // The common case: people of one professional sphere with a couple of related roles.
        TestContext.Out.WriteLine($"single-family share: {familiesHistogram.GetValueOrDefault(1) / (float)total:P1}");
        TestContext.Out.WriteLine($"candidates with a head assignment: {heads / (float)total:P1}");
        Assert.That(sumJobs / (float)total, Is.LessThanOrEqualTo(3.0), "average assignment list must stay small");
        Assert.That(FractionAtMost(jobsHistogram, 3), Is.GreaterThanOrEqualTo(0.75f), "nearly all candidates should have 1-3 assignments");
        Assert.That(passengerOnly / (float)total, Is.LessThanOrEqualTo(0.02f), "fresh graduates without any assignment must be rare");
        Assert.That(retrained / (float)total, Is.LessThanOrEqualTo(0.06f), "major career changes must stay rare");
        Assert.That(heads / (float)total, Is.GreaterThanOrEqualTo(0.06f),
            "command roles must appear regularly, not once in a blue moon");
        Assert.That(familiesHistogram.GetValueOrDefault(1) / (float)total, Is.GreaterThanOrEqualTo(0.70f),
            "most people must stay within one professional family");
    }

    private static AshfallPersonStructure MakePerson(string educationId, params (string Competency, float Experience)[] experience)
    {
        var person = new AshfallPersonStructure
        {
            SpeciesId = "Human",
            Age = 40,
            Education = educationId,
            AvailableCareerYears = 20,
            UsedCareerYears = 10,
        };
        foreach (var (competency, exp) in experience)
        {
            person.AddExperience(competency, exp, AshfallProvenanceSource.Career, "FixtureRole");
        }
        return person;
    }

    private static float FractionAtMost(Dictionary<int, int> histogram, int max)
    {
        var total = histogram.Values.Sum();
        var matching = histogram.Where(kvp => kvp.Key <= max).Sum(kvp => kvp.Value);
        return total == 0 ? 0f : matching / (float)total;
    }

    private static string Format(Dictionary<int, int> histogram)
    {
        return string.Join(", ", histogram.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}: {kvp.Value}"));
    }
}
