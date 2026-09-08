using Content.Shared.Ashfall.Particles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Ashfall.Particles;

/// <summary>
/// API for <see cref="ParticleSystem"/>.
/// </summary>
public sealed partial class ParticleSystem
{
    public ActiveEmitter? CreateParticle(
        ProtoId<ParticleEffectPrototype> effectId,
        EntityUid entity,
        Color? colorOverride = null,
        bool attach = true,
        ParticleRuntimeOverrides? overrride = null)
    {
        var coords = _transform.GetMapCoordinates(entity);
        return SpawnEffect(effectId, coords, attach ? entity : null, colorOverride, overrride);
    }

    public ActiveEmitter? CreateParticle(
        ProtoId<ParticleEffectPrototype> effectId,
        MapCoordinates coords,
        Color? colorOverride = null,
        ParticleRuntimeOverrides? overrride = null)
    {
        return SpawnEffect(effectId, coords, null, colorOverride, overrride);
    }

    public void RemoveParticle(ActiveEmitter? emitter)
    {
        if (emitter != null)
            StopEffect(emitter);
    }

    public void RemoveParticle(uint handle)
    {
        StopEffect(handle);
    }
}
