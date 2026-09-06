using Robust.Shared.Configuration;

namespace Content.Shared.Ashfall;

/// <summary>
///     Configuration variables specific to Ashfall.
/// </summary>
[CVarDefs]
public sealed class AshfallCCVars
{
    /// <summary>
    ///     Number of candidates generated per round pool.
    /// </summary>
    public static readonly CVarDef<int> CharacterPoolSize =
        CVarDef.Create("ashfall.character_pool_size", 5, CVar.SERVERONLY);

    /// <summary>
    ///     Cooldown in seconds between pool refreshes ("ЗАПРОСИТЬ ДРУГИЕ ЛИЧНЫЕ ДЕЛА").
    /// </summary>
    public static readonly CVarDef<float> CharacterPoolRefreshCooldown =
        CVarDef.Create("ashfall.character_pool_refresh_cooldown", 3.0f, CVar.SERVERONLY);

    /// <summary>
    ///     Maximum allowed pool refreshes per player per round (-1 for unlimited).
    /// </summary>
    public static readonly CVarDef<int> CharacterPoolMaxRefreshes =
        CVarDef.Create("ashfall.character_pool_max_refreshes", -1, CVar.SERVERONLY);
}
