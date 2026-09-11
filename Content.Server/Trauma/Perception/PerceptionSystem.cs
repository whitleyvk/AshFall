// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Disposal.Unit;
using Content.Shared.Ghost.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Trauma.Common.CCVar;
using Content.Shared.Trauma.Perception.Components;
using Content.Shared.Trauma.Perception.Events;
using Content.Shared.Trauma.Perception.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Light;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Trauma.Perception;

public sealed partial class PerceptionSystem : SharedPerceptionSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private LightLevelSystem _lightLevel = default!;
    [Dependency] private EntityQuery<GhostComponent> _ghostQuery = default!;
    [Dependency] private EntityQuery<BeingDisposedComponent> _disposedQuery = default!;
    [Dependency] private EntityQuery<InputMoverComponent> _moverQuery = default!;
    [Dependency] private IRobustRandom _random = default!;

    public float LookupRange = 10f;
    public float UpdateFrequency = 0.35f;
    public float MaximumLightLevel = 10f;
    public bool ShadowStealthEnabled = true;

    private TimeSpan _nextLightUpdate = TimeSpan.Zero;

    private readonly HashSet<Entity<PointLightComponent>> _lightLookup = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, TraumaCVars.PerceptionLightDetectionRange, v => LookupRange = v, true);
        Subs.CVar(_cfg, TraumaCVars.PerceptionLightUpdateFrequency, v => UpdateFrequency = v, true);
        Subs.CVar(_cfg, TraumaCVars.PerceptionLightMaximumLevel, v => MaximumLightLevel = v, true);
        Subs.CVar(_cfg, TraumaCVars.PerceptionShadowStealthEnabled, v => ShadowStealthEnabled = v, true);

        // LightDetection
        SubscribeLocalEvent<LightDetectionComponent, ComponentInit>(OnLightDetectionInit);

        // LightDetectionDamage
        SubscribeLocalEvent<LightDetectionDamageComponent, MapInitEvent>(OnDamageStartup);
        SubscribeLocalEvent<LightDetectionDamageComponent, ComponentShutdown>(OnDamageShutdown);

        // LightImmunity
        SubscribeLocalEvent<LightImmunityComponent, MapInitEvent>(OnImmunityMapInit);
        SubscribeLocalEvent<LightImmunityComponent, LightDamageUpdateAttemptEvent>(OnImmunityCheck);

        // DeleteOnLightExposure
        SubscribeLocalEvent<DeleteOnLightExposureComponent, LightLevelUpdated>(OnLightLevelDeleteCheck);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        // Periodic light level calculation and shadow stealth updates
        if (now >= _nextLightUpdate)
        {
            _nextLightUpdate = now + TimeSpan.FromSeconds(UpdateFrequency);
            UpdateLightingAndStealth();
        }

        UpdateLightDamage(now);
        UpdateLightDeletions(now);
        UpdateLightImmunities(now);
    }

    private void OnLightDetectionInit(EntityUid uid, LightDetectionComponent comp, ComponentInit args)
    {
        // Stagger the periodic recalculation so tracked entities spread across ticks instead of spiking together
        comp.NextUpdate = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat() * UpdateFrequency);
    }

    private void UpdateLightingAndStealth()
    {
        var now = _timing.CurTime;

        // 1. Update LightDetectionComponents
        var lightQuery = EntityQueryEnumerator<LightDetectionComponent, TransformComponent>();
        while (lightQuery.MoveNext(out var uid, out var comp, out var xform))
        {
            if (now < comp.NextUpdate)
                continue;

            if (xform.MapID == Robust.Shared.Map.MapId.Nullspace)
                continue;

            comp.NextUpdate = now + TimeSpan.FromSeconds(UpdateFrequency);

            var oldLevel = comp.CurrentLightLevel;
            var newLevel = CalculateEntityLightLevel(uid, xform);

            if (!MathHelper.CloseTo(oldLevel, newLevel, 0.01f))
            {
                comp.CurrentLightLevel = newLevel;
                Dirty(uid, comp);

                var ev = new LightLevelUpdated(newLevel, oldLevel);
                RaiseLocalEvent(uid, ref ev);
            }
        }

        // 2. Update PerceptionStealthComponents
        var stealthQuery = EntityQueryEnumerator<PerceptionStealthComponent, TransformComponent>();
        while (stealthQuery.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!ShadowStealthEnabled || !comp.Enabled)
            {
                if (comp.CurrentVisibility < 1f)
                {
                    comp.CurrentVisibility = 1f;
                    Dirty(uid, comp);
                }
                continue;
            }

            if (comp.IsRevealed(now))
            {
                if (comp.CurrentVisibility < comp.MaxVisibility)
                {
                    comp.CurrentVisibility = comp.MaxVisibility;
                    Dirty(uid, comp);
                }
                continue;
            }

            // Get light level for this entity
            float light;
            if (TryComp<LightDetectionComponent>(uid, out var lightComp))
            {
                light = lightComp.CurrentLightLevel;
            }
            else
            {
                light = CalculateEntityLightLevel(uid, xform);
            }

            var range = MathF.Max(0.001f, comp.LightThresholdHigh - comp.LightThresholdLow);
            var factor = Math.Clamp((light - comp.LightThresholdLow) / range, 0f, 1f);
            var vis = MathHelper.Lerp(comp.MinVisibility, comp.MaxVisibility, factor);

            // Add running penalty if entity is sprinting in shadows
            if (_moverQuery.TryComp(uid, out var mover)
                && (mover.HeldMoveButtons & MoveButtons.AnyDirection) != 0
                && !mover.HeldMoveButtons.HasFlag(MoveButtons.Walk))
            {
                vis = Math.Clamp(vis + comp.RunningVisibilityPenalty, 0f, 1f);
            }

            if (!MathHelper.CloseTo(comp.CurrentVisibility, vis, 0.01f))
            {
                var oldVis = comp.CurrentVisibility;
                comp.CurrentVisibility = vis;
                Dirty(uid, comp);

                var ev = new PerceptionStealthVisibilityChangedEvent(uid, vis, oldVis);
                RaiseLocalEvent(uid, ref ev);
            }
        }
    }

    private float CalculateEntityLightLevel(EntityUid uid, TransformComponent xform)
    {
        if (_disposedQuery.HasComp(uid) || _ghostQuery.HasComp(uid))
            return 0f;

        var worldPos = _transform.GetWorldPosition(xform);
        var totalLight = 0f;

        // Query point lights in range and cast collision rays through opaque blockers
        _lightLookup.Clear();
        _lookup.GetEntitiesInRange(xform.Coordinates, LookupRange, _lightLookup);
        foreach (var (point, pointLight) in _lightLookup)
        {
            if (!pointLight.Enabled)
                continue;

            var pointXform = Transform(point);
            var lightPos = _transform.GetWorldPosition(pointXform);
            var distance = (lightPos - worldPos).Length();

            if (distance <= 0.01f)
            {
                totalLight += pointLight.Energy;
                continue;
            }

            if (distance > pointLight.Radius)
                continue;

            var direction = (worldPos - lightPos).Normalized();
            var ray = new CollisionRay(lightPos, direction, (int) CollisionGroup.Opaque);
            var rayResults = _physics.IntersectRay(xform.MapID, ray, distance, point);

            var blocked = false;
            foreach (var result in rayResults)
            {
                if (result.HitEntity != uid)
                {
                    blocked = true;
                    break;
                }
            }

            if (blocked)
                continue;

            var t = distance / pointLight.Radius;
            totalLight += pointLight.Energy * (1f - t * t);

            if (totalLight >= MaximumLightLevel)
                return MaximumLightLevel;
        }

        // Also check engine ambient lighting at coordinates
        if (_lightLevel.TryCalculateLightLevel(xform.Coordinates, out var engineLight))
        {
            totalLight = Math.Max(totalLight, engineLight);
        }

        return Math.Clamp(totalLight, 0f, MaximumLightLevel);
    }

    private void UpdateLightDamage(TimeSpan now)
    {
        var query = EntityQueryEnumerator<LightDetectionDamageComponent, LightDetectionComponent>();
        while (query.MoveNext(out var uid, out var comp, out var lightDet))
        {
            if (comp.NextUpdate > now)
                continue;

            comp.NextUpdate = now + comp.UpdateInterval;

            var ev = new LightDamageUpdateAttemptEvent();
            RaiseLocalEvent(uid, ref ev);
            if (ev.Cancelled)
                continue;

            // In light, detection value drains; in darkness, it regenerates
            var detectionDelta = lightDet.OnLight
                ? -lightDet.CurrentLightLevel
                : comp.DetectionValueRegeneration;

            comp.DetectionValue = Math.Clamp(comp.DetectionValue + detectionDelta, 0f, comp.DetectionValueMax);
            Dirty(uid, comp);

            if (comp.DetectionValue <= 0f && comp.TakeDamageOnLight && !_mobState.IsDead(uid))
            {
                _damageable.TryChangeDamage(uid, comp.DamageToDeal * comp.ResistanceModifier, true);
                if (comp.SoundOnDamage != null)
                    _audio.PlayPvs(comp.SoundOnDamage, uid);
                continue;
            }

            if (comp.DetectionValue > 0f && comp.HealOnShadows && !lightDet.OnLight && !_mobState.IsDead(uid))
            {
                _damageable.TryChangeDamage(uid, comp.DamageToHeal, true);
            }
        }
    }

    private void UpdateLightDeletions(TimeSpan now)
    {
        var query = EntityQueryEnumerator<DeleteOnLightExposureComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Active || now < comp.ExpiryTime)
                continue;

            QueueDel(uid);
        }
    }

    private void UpdateLightImmunities(TimeSpan now)
    {
        var query = EntityQueryEnumerator<LightImmunityComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            RemCompDeferred(uid, comp);
        }
    }

    private void OnDamageStartup(EntityUid uid, LightDetectionDamageComponent component, MapInitEvent args)
    {
        if (component.ShowAlert && component.AlertProto != null)
            _alerts.ShowAlert(uid, component.AlertProto.Value);

        component.DetectionValue = component.DetectionValueMax;
    }

    private void OnDamageShutdown(EntityUid uid, LightDetectionDamageComponent component, ComponentShutdown args)
    {
        if (component.AlertProto != null)
            _alerts.ClearAlert(uid, component.AlertProto.Value);
    }

    private void OnImmunityMapInit(Entity<LightImmunityComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.Duration;
    }

    private void OnImmunityCheck(Entity<LightImmunityComponent> ent, ref LightDamageUpdateAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnLightLevelDeleteCheck(Entity<DeleteOnLightExposureComponent> ent, ref LightLevelUpdated args)
    {
        if (args.NewLightLevel <= ent.Comp.LightLevel)
        {
            ent.Comp.Active = false;
            ent.Comp.ExpiryTime = TimeSpan.Zero;
            Dirty(ent);
            return;
        }

        if (ent.Comp.Active)
            return;

        ent.Comp.ExpiryTime = _timing.CurTime + ent.Comp.Duration;
        ent.Comp.Active = true;
        Dirty(ent);
    }
}
