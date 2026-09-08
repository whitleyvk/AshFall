using Content.Client.Items.Systems;
using Content.Shared.Ashfall.Weapons.Ranged.Attachments;
using Content.Shared.Ashfall.Weapons.Ranged.Attachments.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Client.Ashfall.Weapons.Ranged.Attachments;

public sealed partial class GunAttachmentsVisualsSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ItemSystem _item = default!;

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

        foreach (var slot in ent.Comp.Slots)
        {
            var layerKey = $"attachment-{slot.ContainerId}";
            if (sprite.LayerMapTryGet(layerKey, out var index))
            {
                sprite.RemoveLayer(index);
            }

            var unshadedKey = $"attachment-unshaded-{slot.ContainerId}";
            if (sprite.LayerMapTryGet(unshadedKey, out var unshadedIndex))
            {
                sprite.RemoveLayer(unshadedIndex);
            }
        }
    }

    private void OnGunState(Entity<AttachableGunComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    public void UpdateVisuals(Entity<AttachableGunComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        foreach (var slot in ent.Comp.Slots)
        {
            var layerKey = $"attachment-{slot.ContainerId}";
            var unshadedKey = $"attachment-unshaded-{slot.ContainerId}";

            if (!_container.TryGetContainer(ent.Owner, slot.ContainerId, out var container) ||
                container.ContainedEntities.Count == 0)
            {
                if (sprite.LayerMapTryGet(layerKey, out var existingIdx))
                    sprite.LayerSetVisible(existingIdx, false);

                if (sprite.LayerMapTryGet(unshadedKey, out var existingUnshadedIdx))
                    sprite.LayerSetVisible(existingUnshadedIdx, false);

                continue;
            }

            var attachedEnt = container.ContainedEntities[0];
            if (!TryComp<GunAttachmentComponent>(attachedEnt, out var attachmentComp) ||
                attachmentComp.AttachedSprite == null)
            {
                if (sprite.LayerMapTryGet(layerKey, out var existingIdx))
                    sprite.LayerSetVisible(existingIdx, false);

                if (sprite.LayerMapTryGet(unshadedKey, out var existingUnshadedIdx))
                    sprite.LayerSetVisible(existingUnshadedIdx, false);

                continue;
            }

            // Normal layer
            if (!sprite.LayerMapTryGet(layerKey, out var layerIndex))
            {
                layerIndex = sprite.LayerMapReserveBlank(layerKey);
            }

            sprite.LayerSetSprite(layerIndex, attachmentComp.AttachedSprite);
            sprite.LayerSetOffset(layerIndex, attachmentComp.AttachedOffset);
            sprite.LayerSetVisible(layerIndex, true);

            // Unshaded layer if applicable
            if (attachmentComp.HasUnshaded)
            {
                if (!sprite.LayerMapTryGet(unshadedKey, out var unshadedLayerIndex))
                {
                    unshadedLayerIndex = sprite.LayerMapReserveBlank(unshadedKey);
                }

                var unshadedSprite = new SpriteSpecifier.Rsi(
                    attachmentComp.AttachedSprite.RsiPath,
                    $"{attachmentComp.AttachedSprite.RsiState}_unshaded");
                sprite.LayerSetSprite(unshadedLayerIndex, unshadedSprite);
                sprite.LayerSetOffset(unshadedLayerIndex, attachmentComp.AttachedOffset);
                sprite.LayerSetShader(unshadedLayerIndex, SpriteSystem.UnshadedId);
                sprite.LayerSetVisible(unshadedLayerIndex, true);
            }
            else if (sprite.LayerMapTryGet(unshadedKey, out var unshadedLayerIndex))
            {
                sprite.LayerSetVisible(unshadedLayerIndex, false);
            }
        }

        _item.VisualsChanged(ent.Owner);
    }
}
