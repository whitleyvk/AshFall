using System.Linq;
using Content.Shared.Containers;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ashfall.Salvage.Flatpacker;

public abstract partial class SharedFlatpackerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private TagSystem _tag = default!;

    public static readonly ProtoId<TagPrototype> TagPackable = "AshfallPackable";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlatpackerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<FlatpackerComponent, FlatpackPackDoAfterEvent>(OnPackDoAfter);

        SubscribeLocalEvent<AshfallFlatpackComponent, GetVerbsEvent<InteractionVerb>>(OnGetFlatpackVerbs);
        SubscribeLocalEvent<AshfallFlatpackComponent, FlatpackUnpackDoAfterEvent>(OnUnpackDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, FlatpackerComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!IsPackable(target, out var failureReason))
        {
            if (!string.IsNullOrEmpty(failureReason))
                _popup.PopupClient(Loc.GetString(failureReason), args.User, args.User);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.PackDelay, new FlatpackPackDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnPackDoAfter(EntityUid uid, FlatpackerComponent component, FlatpackPackDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target || !Exists(target))
            return;

        if (!IsPackable(target, out _))
            return;

        var targetName = Name(target);
        var targetCoords = _transform.GetMoverCoordinates(target);

        var flatpack = Spawn(component.FlatpackPrototype, targetCoords);
        if (!TryComp<AshfallFlatpackComponent>(flatpack, out var flatpackComp))
        {
            QueueDel(flatpack);
            return;
        }

        var container = _container.EnsureContainer<ContainerSlot>(flatpack, flatpackComp.SlotId);
        if (!_container.Insert(target, container))
        {
            QueueDel(flatpack);
            return;
        }

        flatpackComp.PackedName = targetName;
        _metaData.SetEntityName(flatpack, Loc.GetString("ashfall-flatpack-named", ("name", targetName)));
        Dirty(flatpack, flatpackComp);

        _audio.PlayPredicted(component.PackSound, flatpack, args.User);
        _popup.PopupPredicted(
            Loc.GetString("ashfall-flatpacker-pack-success", ("name", targetName)),
            args.User,
            args.User);

        args.Handled = true;
    }

    private void OnGetFlatpackVerbs(EntityUid uid, AshfallFlatpackComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_container.TryGetContainer(uid, component.SlotId, out var container) || container.Count == 0)
            return;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("ashfall-verb-flatpack-unpack"),
            IconEntity = GetNetEntity(uid),
            Act = () => StartUnpack(args.User, uid, component),
            Priority = 2,
        });
    }

    public void StartUnpack(EntityUid user, EntityUid flatpack, AshfallFlatpackComponent component)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.UnpackDelay, new FlatpackUnpackDoAfterEvent(), flatpack, target: flatpack)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnUnpackDoAfter(EntityUid uid, AshfallFlatpackComponent component, FlatpackUnpackDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!_container.TryGetContainer(uid, component.SlotId, out var container) || container.Count == 0)
            return;

        var unpackCoords = _transform.GetMoverCoordinates(uid);
        var contained = container.ContainedEntities.ToArray();

        foreach (var ent in contained)
        {
            _container.Remove(ent, container);
            _transform.SetCoordinates(ent, unpackCoords);
        }

        _audio.PlayPredicted(component.UnpackSound, unpackCoords, args.User);
        _popup.PopupPredicted(
            Loc.GetString("ashfall-flatpacker-unpack-success"),
            args.User,
            args.User);

        QueueDel(uid);
        args.Handled = true;
    }

    public virtual bool IsPackable(EntityUid target, out string? failureReason)
    {
        failureReason = null;

        if (HasComp<MobStateComponent>(target))
            return false;

        var xform = Transform(target);
        if (xform.Anchored)
        {
            failureReason = "ashfall-flatpacker-must-unanchor";
            return false;
        }

        if (_tag.HasTag(target, TagPackable))
            return true;

        if (IsServerPackable(target))
            return true;

        failureReason = "ashfall-flatpacker-not-packable";
        return false;
    }

    protected virtual bool IsServerPackable(EntityUid target)
    {
        return false;
    }
}
