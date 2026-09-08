using System.Numerics;
using Content.Shared.Ashfall.Particles;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client.Ashfall.Particles;

/// <summary>
/// A running particle emitter and its live particle pool.
/// </summary>
public sealed class ActiveEmitter
{
    public ParticleEffectPrototype Proto = default!;

    public MapCoordinates MapCoords;
    public EntityUid? AttachedEntity;
    public TimeSpan Age;
    public float EmitAccum;
    public bool Exhausted;
    public uint Handle;
    public Color? ColorOverride;
    public float Intensity = 1f;
    public ParticleRuntimeOverrides? Overrides;

    public Vector2 PreviousPosition;
    public Vector2 EmitterVelocity;
    public bool VelocityInitialized;

    public EntityUid? TargetEntity;
    public Vector2? TargetPosition;
    public float EffectiveEmitAngle;

    public readonly List<bool> FiredBursts = new();

    public Texture[] Frames = Array.Empty<Texture>();
    public float[] Delays = Array.Empty<float>();
    public int AnimFrame;
    public float AnimTimer;

    public readonly List<ParticleData> Particles = new();
    public readonly Queue<ParticleData> FreePool = new();

    public bool HasLiveParticles()
    {
        foreach (var p in Particles)
        {
            if (p.Alive)
                return true;
        }
        return false;
    }
}
