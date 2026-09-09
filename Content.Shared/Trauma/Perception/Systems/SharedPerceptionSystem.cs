// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Trauma.Perception.Components;
using Content.Shared.Trauma.Perception.Events;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Shared.Trauma.Perception.Systems;

public abstract partial class SharedPerceptionSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedStealthSystem _stealth = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Speed modifier based on lighting
        SubscribeLocalEvent<LightSpeedModifierComponent, LightLevelUpdated>(OnLightLevelUpdatedSpeed);
        SubscribeLocalEvent<LightSpeedModifierComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);

        // Status effect relays for light level updates
        SubscribeLocalEvent<StatusEffectContainerComponent, LightLevelUpdated>(_status.RelayEvent);
        SubscribeLocalEvent<DarknessStealthStatusEffectComponent, StatusEffectRelayedEvent<LightLevelUpdated>>(OnDarknessStealthRelayedLight);
        SubscribeLocalEvent<DarknessStealthStatusEffectComponent, StatusEffectAppliedEvent>(OnDarknessStealthApplied);
        SubscribeLocalEvent<DarknessStealthStatusEffectComponent, StatusEffectRemovedEvent>(OnDarknessStealthRemoved);

        // Combat reveals for perception stealth
        SubscribeLocalEvent<PerceptionStealthComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<PerceptionStealthComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnLightLevelUpdatedSpeed(Entity<LightSpeedModifierComponent> ent, ref LightLevelUpdated args)
    {
        var wasOnLight = ent.Comp.OnLight;
        ent.Comp.OnLight = args.NewLightLevel > ent.Comp.RequiredLightLevel;
        if (wasOnLight != ent.Comp.OnLight)
        {
            Dirty(ent);
            _movement.RefreshMovementSpeedModifiers(ent.Owner);
        }
    }

    private void OnRefreshMovementSpeed(Entity<LightSpeedModifierComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        // If ApplyInDarkness is true, apply when not on light (i.e. in shadow/darkness).
        // If false, apply when on light.
        var active = ent.Comp.ApplyInDarkness ? !ent.Comp.OnLight : ent.Comp.OnLight;
        if (!active)
            return;

        args.ModifySpeed(ent.Comp.WalkModifier, ent.Comp.SprintModifier);
    }

    private void OnDarknessStealthRelayedLight(Entity<DarknessStealthStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LightLevelUpdated> args)
    {
        var target = args.AppliedTo;
        if (args.Args.NewLightLevel < ent.Comp.TriggerAt)
        {
            _stealth.SetVisibility(target, ent.Comp.Visibility);
        }
        else
        {
            _stealth.SetVisibility(target, 1f);
        }
    }

    private void OnDarknessStealthApplied(Entity<DarknessStealthStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<LightDetectionComponent>(args.Target);
        EnsureComp<StealthComponent>(args.Target);
    }

    private void OnDarknessStealthRemoved(Entity<DarknessStealthStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        RemCompDeferred<LightDetectionComponent>(args.Target);
        RemCompDeferred<StealthComponent>(args.Target);
    }

    private void OnDamageChanged(Entity<PerceptionStealthComponent> ent, ref DamageChangedEvent args)
    {
        if (!ent.Comp.RevealOnDamage || !args.DamageIncreased || args.DamageDelta == null)
            return;

        if (args.DamageDelta.GetTotal() < ent.Comp.DamageRevealThreshold)
            return;

        RevealEntity(ent.Owner, ent.Comp.RevealDuration, ent.Comp);
    }

    private void OnMeleeHit(Entity<PerceptionStealthComponent> ent, ref MeleeHitEvent args)
    {
        if (!ent.Comp.RevealOnAttack || args.HitEntities.Count == 0)
            return;

        RevealEntity(ent.Owner, ent.Comp.RevealDuration, ent.Comp);
    }

    /// <summary>
    /// Temporarily reveals an entity, disabling shadow stealth concealment.
    /// </summary>
    public void RevealEntity(EntityUid uid, TimeSpan duration, PerceptionStealthComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.RevealedUntil = _timing.CurTime + duration;
        comp.CurrentVisibility = comp.MaxVisibility;
        Dirty(uid, comp);
    }

    /// <summary>
    /// Checks if an entity is temporarily immune to light damage and light effects.
    /// </summary>
    public bool IsImmuneToLight(EntityUid uid)
    {
        return HasComp<LightImmunityComponent>(uid);
    }
}
