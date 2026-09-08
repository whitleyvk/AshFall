// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVar;

public sealed partial class TraumaCVars
{
    /// <summary>
    /// Disable vision effect spawning like footsteps, used for integration tests.
    /// </summary>
    public static readonly CVarDef<bool> DisableVisionEffects =
        CVarDef.Create("trauma.disable_vision_effects", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Whether to disable vision cone overlays.
    /// </summary>
    public static readonly CVarDef<bool> DisableVisionCones =
        CVarDef.Create("trauma.disable_vision_cones", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Scale for how strong out-of-vision graininess is, 0 is just pure greyscale.
    /// </summary>
    public static readonly CVarDef<float> VisionGrainScale =
        CVarDef.Create("trauma.vision_grain_scale", 0.75f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Whether dynamic light and shadow stealth perception is enabled.
    /// </summary>
    public static readonly CVarDef<bool> PerceptionShadowStealthEnabled =
        CVarDef.Create("trauma.perception_shadow_stealth_enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Range around entities in which to look for point lights for perception calculations.
    /// </summary>
    public static readonly CVarDef<float> PerceptionLightDetectionRange =
        CVarDef.Create("trauma.perception_light_detection_range", 10f, CVar.SERVER);

    /// <summary>
    /// How often light levels update for entities in seconds.
    /// </summary>
    public static readonly CVarDef<float> PerceptionLightUpdateFrequency =
        CVarDef.Create("trauma.perception_light_update_frequency", 0.35f, CVar.SERVER);

    /// <summary>
    /// Maximum light level considered by the perception light system.
    /// </summary>
    public static readonly CVarDef<float> PerceptionLightMaximumLevel =
        CVarDef.Create("trauma.perception_light_max_level", 10f, CVar.SERVER);
}
