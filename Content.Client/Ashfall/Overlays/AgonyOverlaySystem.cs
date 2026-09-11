using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DamageOverlay;
using Content.Shared.Mobs;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Ashfall.Overlays;

public sealed partial class AgonyOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPlayerManager _player = default!;

    private AgonyOverlay _overlay = default!;
    private float _lastPainLevel;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new AgonyOverlay();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        if (_player.LocalEntity != null)
        {
            _overlayManager.AddOverlay(_overlay);
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        _lastPainLevel = 0f;
        if (!_overlayManager.HasOverlay<AgonyOverlay>())
            _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _lastPainLevel = 0f;
        _overlay.PainIntensity = 0f;
        _overlay.ShockIntensity = 0f;
        _overlay.CritIntensity = 0f;
        _overlayManager.RemoveOverlay(_overlay);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity is not { } local || !TryComp<DamageOverlayComponent>(local, out var damageOverlay))
        {
            _lastPainLevel = 0f;
            DecayOverlay(frameTime);
            return;
        }

        if (damageOverlay.PainLevel > _lastPainLevel + 0.002f)
        {
            var painDelta = damageOverlay.PainLevel - _lastPainLevel;
            _overlay.ShockIntensity = MathF.Min(1.0f, _overlay.ShockIntensity + MathF.Max(0.35f, painDelta * 4f));
        }
        _lastPainLevel = damageOverlay.PainLevel;

        if (_overlay.ShockIntensity > 0f)
        {
            _overlay.ShockIntensity = MathF.Max(0f, _overlay.ShockIntensity - frameTime * 2.8f);
        }

        float targetPain = 0f;
        if (damageOverlay.PainLevel > 0.15f && damageOverlay.CurrentState == MobState.Alive)
        {
            targetPain = Math.Clamp((damageOverlay.PainLevel - 0.15f) / 0.85f, 0f, 1f);
        }

        float targetCrit = 0f;
        if (damageOverlay.CurrentState == MobState.Critical)
        {
            targetCrit = Math.Clamp(0.6f + damageOverlay.CritLevel * 0.4f, 0f, 1f);
        }

        var lerpSpeed = MathF.Min(1f, 5.0f * frameTime);
        _overlay.PainIntensity = MathHelper.Lerp(_overlay.PainIntensity, targetPain, lerpSpeed);
        _overlay.CritIntensity = MathHelper.Lerp(_overlay.CritIntensity, targetCrit, lerpSpeed);
    }

    private void DecayOverlay(float deltaSeconds)
    {
        if (_overlay.ShockIntensity <= 0.001f && _overlay.PainIntensity <= 0.001f && _overlay.CritIntensity <= 0.001f)
        {
            _overlay.ShockIntensity = 0f;
            _overlay.PainIntensity = 0f;
            _overlay.CritIntensity = 0f;
            return;
        }

        var decay = deltaSeconds * 3f;
        _overlay.ShockIntensity = MathF.Max(0f, _overlay.ShockIntensity - decay);
        _overlay.PainIntensity = MathF.Max(0f, _overlay.PainIntensity - decay);
        _overlay.CritIntensity = MathF.Max(0f, _overlay.CritIntensity - decay);
    }
}
