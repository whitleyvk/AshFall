// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trauma.Perception.Components;
using Content.Shared.Trauma.Perception.Events;
using Content.Shared.Trauma.Perception.Systems;
using Content.Trauma.Client.Sprite;
using Content.Trauma.Client.Viewcone;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameStates;

namespace Content.Client.Trauma.Perception;

public sealed partial class ClientPerceptionSystem : SharedPerceptionSystem
{
    [Dependency] private SpriteVisibilitySystem _spriteVis = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityQuery<SpriteComponent> _spriteQuery = default!;
    [Dependency] private EntityQuery<ViewconeOccludedComponent> _occludedQuery = default!;
    [Dependency] private EntityQuery<PerceptionSensoryComponent> _sensoryQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PerceptionStealthComponent, ComponentStartup>(OnStealthStartup);
        SubscribeLocalEvent<PerceptionStealthComponent, ComponentShutdown>(OnStealthShutdown);
        SubscribeLocalEvent<PerceptionStealthComponent, AfterAutoHandleStateEvent>(OnStealthStateHandled);
        SubscribeLocalEvent<PerceptionStealthComponent, PerceptionStealthVisibilityChangedEvent>(OnStealthVisChanged);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var local = _player.LocalEntity;
        if (local == null)
            return;

        // Smoothly update proximity reveal for nearby perception stealth entities
        var query = EntityQueryEnumerator<PerceptionStealthComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (!_spriteQuery.HasComp(uid))
                continue;

            UpdateEntityVisibility(uid, comp, xform, local.Value);
        }
    }

    private void OnStealthStartup(Entity<PerceptionStealthComponent> ent, ref ComponentStartup args)
    {
        UpdateTargetVisibility(ent.Owner, ent.Comp);
    }

    private void OnStealthShutdown(Entity<PerceptionStealthComponent> ent, ref ComponentShutdown args)
    {
        // Reset visibility modifier on removal
        _spriteVis.UpdateVisibilityModifiers(ent.Owner, ent.Comp.StealthKey, 1.0f);
    }

    private void OnStealthStateHandled(Entity<PerceptionStealthComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateTargetVisibility(ent.Owner, ent.Comp);
    }

    private void OnStealthVisChanged(Entity<PerceptionStealthComponent> ent, ref PerceptionStealthVisibilityChangedEvent args)
    {
        UpdateTargetVisibility(ent.Owner, ent.Comp);
    }

    private void UpdateTargetVisibility(EntityUid target, PerceptionStealthComponent comp)
    {
        if (TerminatingOrDeleted(target))
            return;

        var local = _player.LocalEntity;
        if (local == null)
        {
            ApplyVisibility(target, comp, comp.CurrentVisibility);
            return;
        }

        if (TryComp(target, out TransformComponent? xform))
        {
            UpdateEntityVisibility(target, comp, xform, local.Value);
        }
    }

    private void UpdateEntityVisibility(EntityUid target, PerceptionStealthComponent comp, TransformComponent xform, EntityUid viewer)
    {
        // 1. Never double-obscure entities already occluded by Viewcone
        if (_occludedQuery.HasComp(target))
            return;

        // 2. Never touch Viewcone memory entities
        if (MetaData(target).EntityPrototype?.ID == "ViewconeMemory")
            return;

        // 3. If local player is the target, give slight translucency in shadow so they have clear feedback
        if (target == viewer)
        {
            var selfAlpha = comp.CurrentVisibility < 0.95f ? Math.Max(0.65f, comp.CurrentVisibility) : 1.0f;
            ApplyVisibility(target, comp, selfAlpha);
            return;
        }

        // 4. Observer sensory processing
        var effectiveVis = comp.CurrentVisibility;

        if (_sensoryQuery.TryComp(viewer, out var sensory))
        {
            if (sensory.NightVision)
            {
                effectiveVis = 1.0f;
            }
            else
            {
                if (sensory.DarkSightLevel > 0f)
                {
                    effectiveVis = MathHelper.Lerp(effectiveVis, 1.0f, sensory.DarkSightLevel);
                }

                // Proximity detection: cannot remain hidden at point-blank range
                if (sensory.ProximityRevealRadius > 0f && xform.MapID == Transform(viewer).MapID)
                {
                    var dist = (_transform.GetWorldPosition(xform) - _transform.GetWorldPosition(viewer)).Length();
                    if (dist <= sensory.ProximityRevealRadius)
                    {
                        var proximityFactor = 1f - (dist / sensory.ProximityRevealRadius);
                        effectiveVis = Math.Max(effectiveVis, MathHelper.Lerp(effectiveVis, 1.0f, proximityFactor));
                    }
                }
            }
        }

        ApplyVisibility(target, comp, effectiveVis);
    }

    private void ApplyVisibility(EntityUid target, PerceptionStealthComponent comp, float alpha)
    {
        _spriteVis.UpdateVisibilityModifiers(target, comp.StealthKey, alpha);
    }
}
