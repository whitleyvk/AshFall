using Content.Client.Items.Systems;
using Content.Shared.Ashfall.Weapons.Ranged.Attachments;
using Content.Shared.Ashfall.Weapons.Ranged.Attachments.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Client.Ashfall.Weapons.Ranged.Attachments;

public sealed partial class GunAttachmentsVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ItemSystem _item = default!;
    [Dependency] private IReflectionManager _reflection = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AttachableGunComponent, GunAttachmentVisualsChangedEvent>(OnVisualsChanged);
        SubscribeLocalEvent<AttachableGunComponent, AfterAutoHandleStateEvent>(OnGunState);
        SubscribeLocalEvent<AttachableGunComponent, ComponentShutdown>(OnGunShutdown);
    }

    private void OnVisualsChanged(Entity<AttachableGunComponent> ent, ref GunAttachmentVisualsChangedEvent args)
    {
        UpdateVisuals(ent);
    }

    private void OnGunShutdown(Entity<AttachableGunComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        RemoveAttachmentLayers((ent.Owner, sprite), ent.Comp);
    }

    private void OnGunState(Entity<AttachableGunComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    public void UpdateVisuals(Entity<AttachableGunComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        Entity<SpriteComponent?> spriteEnt = (ent.Owner, sprite);
        RemoveAttachmentLayers(spriteEnt, ent.Comp);

        var attachments = new List<AttachmentLayerData>();
        for (var slotIndex = 0; slotIndex < ent.Comp.Slots.Count; slotIndex++)
        {
            var slot = ent.Comp.Slots[slotIndex];
            var layerKey = $"attachment-{slot.ContainerId}";

            if (!_container.TryGetContainer(ent.Owner, slot.ContainerId, out var container) ||
                container.ContainedEntities.Count == 0)
                continue;

            var attachedEnt = container.ContainedEntities[0];
            if (!TryComp<GunAttachmentComponent>(attachedEnt, out var attachmentComp) ||
                attachmentComp.AttachedSprite == null)
                continue;

            var visual = ent.Comp.Visuals.TryGetValue(slot.ContainerId, out var configuredVisual)
                ? configuredVisual
                : new GunAttachmentVisual();
            attachments.Add(new AttachmentLayerData(
                layerKey,
                slot.ContainerId,
                attachmentComp,
                visual,
                slotIndex));
        }

        var bottom = attachments
            .Where(data => data.Visual.LayerAnchor.Equals("bottom", StringComparison.OrdinalIgnoreCase))
            .OrderBy(data => data.Visual.DrawOrder)
            .ThenBy(data => data.SlotIndex)
            .ToList();
        var top = attachments
            .Where(data => data.Visual.LayerAnchor.Equals("top", StringComparison.OrdinalIgnoreCase))
            .OrderBy(data => data.Visual.DrawOrder)
            .ThenBy(data => data.SlotIndex)
            .ToList();
        var anchored = attachments
            .Where(data => !data.Visual.LayerAnchor.Equals("top", StringComparison.OrdinalIgnoreCase) &&
                           !data.Visual.LayerAnchor.Equals("bottom", StringComparison.OrdinalIgnoreCase))
            .GroupBy(data => data.Visual.LayerAnchor)
            .ToList();

        var insertIndex = 0;
        foreach (var data in bottom)
            insertIndex += AddAttachmentLayers(spriteEnt, data, insertIndex);

        var resolvedGroups = new List<(int Index, string Key, List<AttachmentLayerData> Data)>();
        foreach (var group in anchored)
        {
            if (!TryGetLayerIndex(spriteEnt, group.Key, out var index))
            {
                Log.Warning($"Attachment layer anchor '{group.Key}' was not found on {ToPrettyString(ent.Owner)}. Drawing the attachment on top.");
                top.AddRange(group);
                continue;
            }

            resolvedGroups.Add((index, group.Key, group.ToList()));
        }

        foreach (var group in resolvedGroups.OrderBy(group => group.Index))
        {
            var before = group.Data
                .Where(data => data.Visual.LayerPosition == GunAttachmentLayerPosition.Before)
                .OrderBy(data => data.Visual.DrawOrder)
                .ThenBy(data => data.SlotIndex);
            foreach (var data in before)
            {
                TryGetLayerIndex(spriteEnt, group.Key, out var anchorIndex);
                AddAttachmentLayers(spriteEnt, data, anchorIndex);
            }

            var after = group.Data
                .Where(data => data.Visual.LayerPosition == GunAttachmentLayerPosition.After)
                .OrderByDescending(data => data.Visual.DrawOrder)
                .ThenByDescending(data => data.SlotIndex);
            foreach (var data in after)
            {
                TryGetLayerIndex(spriteEnt, group.Key, out var anchorIndex);
                AddAttachmentLayers(spriteEnt, data, anchorIndex + 1);
            }
        }

        foreach (var data in top.OrderBy(data => data.Visual.DrawOrder).ThenBy(data => data.SlotIndex))
            AddAttachmentLayers(spriteEnt, data, null);

        _item.VisualsChanged(ent.Owner);
    }

    private int AddAttachmentLayers(Entity<SpriteComponent?> sprite, AttachmentLayerData data, int? index)
    {
        var attachmentComp = data.Component;
        var totalOffset = data.Visual.Offset + attachmentComp.AttachedOffset;
        var totalRotation = data.Visual.Rotation + attachmentComp.AttachedRotation;
        var layerIndex = _sprite.AddLayer(sprite, attachmentComp.AttachedSprite!, index);
        _sprite.LayerMapSet(sprite, data.LayerKey, layerIndex);
        _sprite.LayerSetOffset(sprite, layerIndex, totalOffset);
        _sprite.LayerSetRotation(sprite, layerIndex, totalRotation);
        _sprite.LayerSetVisible(sprite, layerIndex, true);

        if (!attachmentComp.HasUnshaded)
            return 1;

        var unshadedKey = $"attachment-unshaded-{data.ContainerId}";
        var unshadedSprite = new SpriteSpecifier.Rsi(
            attachmentComp.AttachedSprite!.RsiPath,
            $"{attachmentComp.AttachedSprite.RsiState}_unshaded");
        var unshadedIndex = _sprite.AddLayer(sprite, unshadedSprite, layerIndex + 1);
        _sprite.LayerMapSet(sprite, unshadedKey, unshadedIndex);
        _sprite.LayerSetOffset(sprite, unshadedIndex, totalOffset);
        _sprite.LayerSetRotation(sprite, unshadedIndex, totalRotation);
        sprite.Comp!.LayerSetShader(unshadedIndex, SpriteSystem.UnshadedId.Id);
        _sprite.LayerSetVisible(sprite, unshadedIndex, true);
        return 2;
    }

    private void RemoveAttachmentLayers(Entity<SpriteComponent?> sprite, AttachableGunComponent component)
    {
        foreach (var slot in component.Slots)
        {
            _sprite.RemoveLayer(sprite, $"attachment-unshaded-{slot.ContainerId}", false);
            _sprite.RemoveLayer(sprite, $"attachment-{slot.ContainerId}", false);
        }
    }

    private bool TryGetLayerIndex(Entity<SpriteComponent?> sprite, string key, out int index)
    {
        return _reflection.TryParseEnumReference(key, out var @enum)
            ? _sprite.LayerMapTryGet(sprite, @enum, out index, false)
            : _sprite.LayerMapTryGet(sprite, key, out index, false);
    }

    private sealed record AttachmentLayerData(
        string LayerKey,
        string ContainerId,
        GunAttachmentComponent Component,
        GunAttachmentVisual Visual,
        int SlotIndex);
}
