// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.CCVar;
using Robust.Client.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Configuration;

namespace Content.Trauma.Client.AudioMuffle;

/// <summary>
/// Routes in-world positional sounds through the <c>AshfallSubtleReverb</c> auxiliary slot,
/// giving station rooms a faint acoustic tail. Purely client-side: the engine spawns the
/// audio preset entities on the server and replicates the OpenAL EFX auxiliary to us.
/// </summary>
public sealed partial class SubtleReverbSystem : EntitySystem
{
    private const string ReverbPresetId = "AshfallSubtleReverb";

    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    private bool _enabled = true;

    // Entities whose auxiliary slot we have attached on the client
    private readonly HashSet<EntityUid> _bound = new();
    private readonly List<EntityUid> _seen = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, TraumaCVars.SubtleReverb, OnCVarChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        ClearAllAuxiliaries();
    }

    private void OnCVarChanged(bool enabled)
    {
        _enabled = enabled;
        if (!_enabled)
        {
            ClearAllAuxiliaries();
        }
    }

    private void ClearAllAuxiliaries()
    {
        var query = EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out var uid, out var audio))
        {
            if (_bound.Remove(uid) && audio.Auxiliary == null)
            {
                ((IAudioSource) audio).SetAuxiliary(null);
            }
        }

        _bound.Clear();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_enabled)
            return;

        // The preset entity is only created when the server loads presets at round start,
        // so pick it up lazily and survive round restarts where it disappears again.
        if (!_audio.Auxiliaries.TryGetValue(ReverbPresetId, out var auxEntity) ||
            !TryComp<AudioAuxiliaryComponent>(auxEntity, out var auxComp) ||
            auxComp.Auxiliary is not { } auxSlot)
        {
            return;
        }

        var query = EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out var uid, out var audio))
        {
            _seen.Add(uid);

            // Only bind positional in-world sounds that have no server-assigned auxiliary
            var want = !audio.Global &&
                       audio.Loaded &&
                       audio.Auxiliary == null;

            if (want)
            {
                // Re-apply unconditionally: engine state application clears the aux send on any
                // audio parameter delta (playback position, volume, ...), and BaseAudioSource
                // no-ops the call when the send is already correct.
                ((IAudioSource) audio).SetAuxiliary(auxSlot);
                _bound.Add(uid);
            }
            else if (_bound.Remove(uid))
            {
                if (audio.Auxiliary == null)
                {
                    ((IAudioSource) audio).SetAuxiliary(null);
                }
            }
        }

        _bound.IntersectWith(_seen);
        _seen.Clear();
    }
}
