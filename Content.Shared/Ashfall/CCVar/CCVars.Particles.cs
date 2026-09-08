using Robust.Shared.Configuration;

namespace Content.Shared.Ashfall.CCVar;

[CVarDefs]
public sealed partial class AshfallCCVars
{
    /// <summary>
    /// Controls particle effect quality.
    /// 0 = Off, 1 = Low, 2 = Medium, 3 = High
    /// Low:    25% of maxCount per emitter
    /// Medium: 50% of maxCount per emitter
    /// High:   100% of maxCount per emitter
    /// </summary>
    public static readonly CVarDef<int> ParticleQuality =
        CVarDef.Create("ashfall.particles_quality", 3, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Maximum total number of live particles allowed on screen at once across all emitters.
    /// </summary>
    public static readonly CVarDef<int> ParticleGlobalBudget =
        CVarDef.Create("ashfall.particles_global_budget", 8000, CVar.CLIENTONLY | CVar.ARCHIVE);
}
