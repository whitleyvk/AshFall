using Content.Shared.Ashfall.CharacterGen.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.Ashfall.CharacterGen;

/// <summary>
///     Assembles a concise personnel record from curated lore fragments.
/// </summary>
public sealed class AshfallCharacterLoreGenerator
{
    private readonly IPrototypeManager _prototypeManager;

    public AshfallCharacterLoreGenerator(IPrototypeManager prototypeManager)
    {
        _prototypeManager = prototypeManager;
    }

    public IReadOnlyList<string> GetProfessionalFamilies()
    {
        return _prototypeManager
            .EnumeratePrototypes<AshfallCharacterLoreFragmentPrototype>()
            .Where(fragment =>
                fragment.Category == AshfallCharacterLoreCategory.Qualification &&
                !string.IsNullOrWhiteSpace(fragment.ProfessionalFamily) &&
                fragment.Jobs.Count > 0)
            .Select(fragment => fragment.ProfessionalFamily!)
            .Distinct()
            .ToArray();
    }

    public AshfallCharacterCandidate GenerateCandidate(
        HumanoidCharacterProfile profile,
        IRobustRandom random,
        string? requiredFamily = null,
        string culture = "PANSLAVIC",
        string birthplace = "HAB-17, HADLEY",
        string morphology = "")
    {
        var tags = new HashSet<string>();
        var selected = new HashSet<string>();

        var origin = Pick(AshfallCharacterLoreCategory.Origin, profile.Age, tags, selected, random);
        Add(origin, tags, selected);

        var qualification = Pick(
            AshfallCharacterLoreCategory.Qualification,
            profile.Age,
            tags,
            selected,
            random,
            requiredFamily);
        Add(qualification, tags, selected);

        var education = Pick(AshfallCharacterLoreCategory.Education, profile.Age, tags, selected, random);
        Add(education, tags, selected);

        var career = new List<ProtoId<AshfallCharacterLoreFragmentPrototype>>();
        var careerCount = profile.Age switch
        {
            < 30 => 1,
            < 45 => 2,
            _ => 3
        };

        for (var i = 0; i < careerCount; i++)
        {
            var entry = Pick(AshfallCharacterLoreCategory.Career, profile.Age, tags, selected, random);
            Add(entry, tags, selected);
            career.Add(entry.ID);
        }

        var personality = Pick(AshfallCharacterLoreCategory.Personality, profile.Age, tags, selected, random);
        Add(personality, tags, selected);

        var evaluation = Pick(AshfallCharacterLoreCategory.Evaluation, profile.Age, tags, selected, random);
        Add(evaluation, tags, selected);

        var hook = Pick(AshfallCharacterLoreCategory.PersonalHook, profile.Age, tags, selected, random);
        Add(hook, tags, selected);

        var preCryo = Pick(AshfallCharacterLoreCategory.PreCryo, profile.Age, tags, selected, random);
        Add(preCryo, tags, selected);

        return new AshfallCharacterCandidate
        {
            Profile = profile,
            ProfessionalFamily = qualification.ProfessionalFamily!,
            CompatibleJobs = qualification.Jobs.Distinct().ToList(),
            Dossier = new AshfallCharacterDossier
            {
                CulturalOrigin = culture,
                Birthplace = birthplace,
                Morphology = morphology,
                Origin = origin.ID,
                Education = education.ID,
                Qualifications = new List<ProtoId<AshfallCharacterLoreFragmentPrototype>> { qualification.ID },
                Career = career,
                Personality = personality.ID,
                Evaluations = new List<ProtoId<AshfallCharacterLoreFragmentPrototype>> { evaluation.ID },
                PersonalHook = hook.ID,
                PreCryo = preCryo.ID,
            },
        };
    }

    private AshfallCharacterLoreFragmentPrototype Pick(
        AshfallCharacterLoreCategory category,
        int age,
        HashSet<string> tags,
        HashSet<string> selected,
        IRobustRandom random,
        string? requiredFamily = null)
    {
        var eligible = _prototypeManager
            .EnumeratePrototypes<AshfallCharacterLoreFragmentPrototype>()
            .Where(fragment =>
                fragment.Category == category &&
                age >= fragment.MinAge &&
                age <= fragment.MaxAge &&
                !selected.Contains(fragment.ID) &&
                fragment.RequiredTags.All(tags.Contains) &&
                !fragment.ExcludedTags.Any(tags.Contains) &&
                (requiredFamily == null || fragment.ProfessionalFamily == requiredFamily))
            .Select(fragment => (Fragment: fragment, Weight: GetWeight(fragment, tags)))
            .Where(entry => entry.Weight > 0f)
            .ToList();

        if (eligible.Count == 0)
            throw new InvalidOperationException($"No valid Ashfall lore fragment for {category} at age {age}.");

        var totalWeight = eligible.Sum(entry => entry.Weight);
        var roll = random.NextFloat(0f, totalWeight);
        var cursor = 0f;

        foreach (var entry in eligible)
        {
            cursor += entry.Weight;
            if (roll <= cursor)
                return entry.Fragment;
        }

        return eligible[^1].Fragment;
    }

    private static float GetWeight(AshfallCharacterLoreFragmentPrototype fragment, HashSet<string> tags)
    {
        var weight = fragment.Weight;
        foreach (var modifier in fragment.WeightModifiers)
        {
            if (tags.Contains(modifier.Tag))
                weight *= modifier.Multiplier;
        }

        return weight;
    }

    private static void Add(
        AshfallCharacterLoreFragmentPrototype fragment,
        HashSet<string> tags,
        HashSet<string> selected)
    {
        selected.Add(fragment.ID);
        tags.UnionWith(fragment.ProvidedTags);
    }
}
