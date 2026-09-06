using Content.Shared.Ashfall.CharacterGen.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Ashfall.CharacterGen;

/// <summary>
///     Scores playable jobs against a finished person structure. A job is either invalid (missing
///     required competency, overqualification ceiling, missing leadership history) or has a score
///     of 0..N. Score bands are an interpretation detail: the player never sees raw numbers.
/// </summary>
public static class AshfallJobScorer
{
    public const int InvalidScore = -1;

    public const int PrimaryThreshold = 80;
    public const int SecondaryThreshold = 45;

    /// <summary>
    ///     Generous upper safety cap; career coherence should keep lists far below this.
    /// </summary>
    public const int MaxAssignments = 5;

    private const int RequiredBase = 40;
    private const int RequiredLevelStepBonus = 6;
    private const int PreferredBase = 12;
    private const int PreferredLevelStepBonus = 4;
    private const int EducationDomainBonus = 14;
    private const int CareerDomainBonus = 10;
    private const int LeadershipBonus = 8;

    public static int Score(
        AshfallPersonStructure person,
        AshfallJobCareerPrototype job,
        IPrototypeManager prototypes)
    {
        // Required competencies: must be part of the person's CURRENT professional track, must
        // exist in the structure (some training or work history) and reach the required level.
        // Historical experience from an abandoned track stays in the biography but does not
        // qualify for assignments.
        foreach (var (competencyId, required) in job.RequiredCompetencies)
        {
            if (person.HasActiveRestriction && !person.ActiveCompetencies.Contains(competencyId))
                return InvalidScore;

            if (!person.Competencies.TryGetValue(competencyId, out var state))
                return InvalidScore;

            if (state.Level.StepsAbove(required) < 0)
                return InvalidScore;
        }

        // Ceilings: an established specialist cannot take a junior role in their own field.
        foreach (var (competencyId, ceiling) in job.MaximumCompetencies)
        {
            var actual = person.GetLevel(competencyId);
            if (actual.StepsAbove(ceiling) > 0)
                return InvalidScore;
        }

        if (job.RequiresLeadershipHistory && !person.LeadershipHistory)
            return InvalidScore;

        var score = 0;

        foreach (var (competencyId, required) in job.RequiredCompetencies)
        {
            score += RequiredBase + person.GetLevel(competencyId).StepsAbove(required) * RequiredLevelStepBonus;
        }

        foreach (var (competencyId, preferred) in job.PreferredCompetencies)
        {
            var steps = person.GetLevel(competencyId).StepsAbove(preferred);
            if (steps >= 0)
                score += PreferredBase + steps * PreferredLevelStepBonus;
        }

        var education = prototypes.Index(person.Education);
        if (education.Domain == job.Domain)
            score += EducationDomainBonus;

        foreach (var stint in person.Career)
        {
            var role = prototypes.Index(stint.Role);
            if (role.Domains.Contains(job.Domain))
            {
                score += CareerDomainBonus;
                break;
            }
        }

        if (person.LeadershipHistory)
            score += LeadershipBonus;

        return score;
    }

    /// <summary>
    ///     Returns valid jobs ordered by score (best first). Fallback-only jobs are suppressed
    ///     while any non-fallback job is valid.
    /// </summary>
    public static List<ProtoId<JobPrototype>> ScoreEligibleJobs(
        AshfallPersonStructure person,
        IPrototypeManager prototypes)
    {
        var valid = new List<(ProtoId<JobPrototype> Job, int Score)>();
        var anyNonFallback = false;

        foreach (var jobCareer in prototypes.EnumeratePrototypes<AshfallJobCareerPrototype>())
        {
            var score = Score(person, jobCareer, prototypes);
            if (score < 0)
                continue;

            if (!jobCareer.FallbackOnly)
            {
                // Assignments stay inside the professional families of the person's life: an
                // adjacent competency does not open every department that shares its domain.
                if (person.HasActiveRestriction && !person.CareerFamilies.Contains(jobCareer.Domain))
                    continue;

                anyNonFallback = true;
            }

            valid.Add((new ProtoId<JobPrototype>(jobCareer.ID), score));
        }

        if (!anyNonFallback)
            return valid.OrderByDescending(entry => entry.Score).Select(entry => entry.Job).ToList();

        // Safety cap on the final tail: coherent careers rarely exceed a handful of sensible
        // assignments, and a sprawling list means the narrowing rules failed somewhere.
        return valid
            .Where(entry => !prototypes.Index<AshfallJobCareerPrototype>(entry.Job).FallbackOnly)
            .OrderByDescending(entry => entry.Score)
            .Take(MaxAssignments)
            .Select(entry => entry.Job)
            .ToList();
    }
}
