using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Ashfall.Weapons.Ranged.Attachments.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Localizations;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Ashfall.Weapons.Ranged.Attachments;

public sealed partial class GunAttachmentsSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IGameTiming _timing = default!;

    private EntityQuery<GunAttachmentComponent> _attachmentQuery;

    public override void Initialize()
    {
        base.Initialize();

        _attachmentQuery = GetEntityQuery<GunAttachmentComponent>();

        SubscribeLocalEvent<AttachableGunComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<AttachableGunComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);
        SubscribeLocalEvent<AttachableGunComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
        SubscribeLocalEvent<AttachableGunComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(ItemSlotsSystem)]);
        SubscribeLocalEvent<AttachableGunComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<AttachableGunComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);

        SubscribeLocalEvent<GunSoundAttachmentComponent, GunRefreshModifiersEvent>(OnGunSoundRefreshModifiers);
        SubscribeLocalEvent<GunRecoilAttachmentComponent, GunRefreshModifiersEvent>(OnGunRecoilRefreshModifiers);
        SubscribeLocalEvent<GunRecoilAttachmentComponent, ExaminedEvent>(OnAttachmentExamined);

        SubscribeLocalEvent<GunComponentAttachmentComponent, GunRefreshModifiersEvent>(OnCompAttachmentEquip);
        SubscribeLocalEvent<GunComponentAttachmentComponent, EntGotRemovedFromContainerMessage>(OnCompAttachmentUnequip);
    }

    private void OnEntInsertedIntoContainer(Entity<AttachableGunComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        var containerId = args.Container.ID;
        var found = false;
        foreach (var slot in ent.Comp.Slots)
        {
            if (slot.ContainerId == containerId)
            {
                found = true;
                break;
            }
        }

        if (found)
        {
            _gun.RefreshModifiers(ent.Owner);
            var ev = new GunAttachmentVisualsChangedEvent();
            RaiseLocalEvent(ent, ref ev);
        }
    }

    private void OnEntRemovedFromContainer(Entity<AttachableGunComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        var containerId = args.Container.ID;
        var found = false;
        foreach (var slot in ent.Comp.Slots)
        {
            if (slot.ContainerId == containerId)
            {
                found = true;
                break;
            }
        }

        if (found)
        {
            _gun.RefreshModifiers(ent.Owner);
            var ev = new GunAttachmentVisualsChangedEvent();
            RaiseLocalEvent(ent, ref ev);
        }
    }

    private void OnGunRefreshModifiers(Entity<AttachableGunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        foreach (var attachment in EnumerateAttachments(ent))
        {
            RaiseLocalEvent(attachment, ref args);
        }
    }

    private void OnInteractUsing(Entity<AttachableGunComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryFindEmptyValidSlot(ent.AsNullable(), args.Used, out var slot))
            return;

        args.Handled = TryInsertAttachment(ent.AsNullable(), args.Used, slot.Value);
    }

    private void OnExamined(Entity<AttachableGunComponent> ent, ref ExaminedEvent args)
    {
        var attachments = EnumerateAttachments(ent).ToList();
        if (attachments.Count == 0)
            return;

        var attachmentNames = ContentLocalizationManager.FormatList(attachments.Select(e => Name(e)).ToList());
        args.PushMarkup(Loc.GetString("gun-attachment-examine-text", ("attachments", attachmentNames)));
    }

    private void OnGetVerbs(Entity<AttachableGunComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        foreach (var slot in ent.Comp.Slots)
        {
            if (!TryGetAttachment(ent, slot, out var attachment))
                continue;

            var attachmentUid = attachment.Value.Owner;
            var attachmentName = Name(attachmentUid);
            var slotName = Loc.GetString(slot.Name);
            var containerId = slot.ContainerId;

            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("gun-attachment-verb-remove", ("attachment", attachmentName), ("slot", slotName)),
                Act = () =>
                {
                    if (!_container.TryGetContainer(ent, containerId, out var container))
                        return;

                    _container.Remove(attachmentUid, container);
                    _hands.TryPickupAnyHand(user, attachmentUid);
                },
                Priority = 2
            });
        }
    }

    private void OnGunSoundRefreshModifiers(Entity<GunSoundAttachmentComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (ent.Comp.Sound != null)
            args.SoundGunshot = ent.Comp.Sound;
    }

    private void OnGunRecoilRefreshModifiers(Entity<GunRecoilAttachmentComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.AngleIncrease *= ent.Comp.RecoilIncreaseModifier;
        args.AngleDecay *= ent.Comp.RecoilRecoveryModifier;
        args.MinAngle *= ent.Comp.MinSpreadModifier;
        args.MaxAngle *= ent.Comp.MaxSpreadModifier;
    }

    private void OnCompAttachmentEquip(EntityUid uid, GunComponentAttachmentComponent component, GunRefreshModifiersEvent args)
    {
        if (_timing.ApplyingState)
            return;

        EntityManager.AddComponents(args.Gun.Owner, component.Components);
    }

    private void OnCompAttachmentUnequip(EntityUid uid, GunComponentAttachmentComponent component, EntGotRemovedFromContainerMessage args)
    {
        EntityManager.RemoveComponents(args.Container.Owner, component.Components);
    }

    private void OnAttachmentExamined(EntityUid uid, GunRecoilAttachmentComponent component, ref ExaminedEvent args)
    {
        if (component.MinSpreadModifier < 1.0f || component.MaxSpreadModifier < 1.0f)
        {
            var spreadReduction = MathF.Round((1.0f - component.MinSpreadModifier) * 100f);
            args.PushMarkup(Loc.GetString("gun-attachment-examine-spread-reduced", ("percent", spreadReduction)));
        }
        else if (component.MinSpreadModifier > 1.0f || component.MaxSpreadModifier > 1.0f)
        {
            var spreadIncrease = MathF.Round((component.MinSpreadModifier - 1.0f) * 100f);
            args.PushMarkup(Loc.GetString("gun-attachment-examine-spread-increased", ("percent", spreadIncrease)));
        }

        if (component.RecoilIncreaseModifier < 1.0f)
        {
            var recoilReduction = MathF.Round((1.0f - component.RecoilIncreaseModifier) * 100f);
            args.PushMarkup(Loc.GetString("gun-attachment-examine-recoil-reduced", ("percent", recoilReduction)));
        }
    }

    public bool HasAttachment(Entity<AttachableGunComponent> ent, GunAttachmentSlot slot)
    {
        return TryGetAttachment(ent, slot, out _);
    }

    public bool TryGetAttachment(Entity<AttachableGunComponent> ent, GunAttachmentSlot slot, [NotNullWhen(true)] out Entity<GunAttachmentComponent>? attachment)
    {
        attachment = null;
        if (!_container.TryGetContainer(ent, slot.ContainerId, out var container))
            return false;

        foreach (var contained in container.ContainedEntities)
        {
            if (!IsAttachmentValid(contained, slot))
                continue;

            attachment = (contained, _attachmentQuery.Get(contained));
            return true;
        }

        return false;
    }

    public bool IsAttachmentValid(Entity<GunAttachmentComponent?> ent, GunAttachmentSlot slot)
    {
        if (!_attachmentQuery.Resolve(ent, ref ent.Comp))
            return false;

        return _whitelist.IsWhitelistPass(slot.Whitelist, ent);
    }

    public bool TryFindEmptyValidSlot(
        Entity<AttachableGunComponent?> gun,
        Entity<GunAttachmentComponent?> attachment,
        [NotNullWhen(true)] out GunAttachmentSlot? outSlot)
    {
        outSlot = null;
        if (!Resolve(gun, ref gun.Comp) || !Resolve(attachment, ref attachment.Comp, false))
            return false;

        foreach (var slot in gun.Comp.Slots)
        {
            if (HasAttachment((gun, gun.Comp), slot))
                continue;

            if (!IsAttachmentValid(attachment, slot))
                continue;

            outSlot = slot;
            break;
        }

        return outSlot != null;
    }

    public bool TryInsertAttachment(Entity<AttachableGunComponent?> gun, Entity<GunAttachmentComponent?> attachment, GunAttachmentSlot slot)
    {
        if (!Resolve(gun, ref gun.Comp) || !Resolve(attachment, ref attachment.Comp, false))
            return false;

        if (HasAttachment((gun, gun.Comp), slot) || !IsAttachmentValid(attachment, slot))
            return false;

        InsertAttachment(gun, attachment, slot);
        return true;
    }

    public void InsertAttachment(Entity<AttachableGunComponent?> gun, Entity<GunAttachmentComponent?> attachment, GunAttachmentSlot slot)
    {
        if (!Resolve(gun, ref gun.Comp) || !Resolve(attachment, ref attachment.Comp))
            return;

        var container = _container.EnsureContainer<ContainerSlot>(gun.Owner, slot.ContainerId);
        _container.Insert(attachment.Owner, container);
    }

    public IEnumerable<Entity<GunAttachmentComponent>> EnumerateAttachments(Entity<AttachableGunComponent> ent)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            if (TryGetAttachment(ent, slot, out var attachment))
                yield return attachment.Value;
        }
    }
}

[ByRefEvent]
public record struct GunAttachmentVisualsChangedEvent;
