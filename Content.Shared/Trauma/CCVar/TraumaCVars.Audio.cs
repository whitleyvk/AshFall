// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Trauma.Common.CCVar;

public sealed partial class TraumaCVars
{
    /// <summary>
    /// Is audio muffle pathfinding behavior enabled?
    /// </summary>
    public static readonly CVarDef<bool> AudioMufflePathfinding =
        CVarDef.Create("trauma.audio_muffle_pathfinding", true, CVar.ARCHIVE | CVar.CLIENTONLY);

    /// <summary>
    /// Is subtle station room acoustic reverb enabled for in-world positional sounds?
    /// </summary>
    public static readonly CVarDef<bool> SubtleReverb =
        CVarDef.Create("trauma.subtle_reverb", true, CVar.ARCHIVE | CVar.CLIENTONLY);
}
