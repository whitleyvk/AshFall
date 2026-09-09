// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Medical.Common.Body;
using Content.Medical.Shared.Surgery.Tools;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Medical.Shared.Body;

public sealed partial class SeveredHeadExtractionSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<OrganComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<OrganComponent, SeveredHeadExtractDoAfterEvent>(OnDoAfter);
    }

    private bool IsSeveredHead(EntityUid uid, out EntityUid headUid)
    {
        headUid = uid;
        if (TryComp<OrganComponent>(uid, out var organ))
        {
            if (organ.Category?.Id != "Head" &&
                (!TryComp<BodyPartComponent>(uid, out var part) || part.PartType != BodyPartType.Head))
            {
                return false;
            }

            // Must not be attached to a living mob
            if (organ.Body != null && HasComp<MobStateComponent>(organ.Body.Value))
                return false;

            return true;
        }

        if (TryComp<BodyComponent>(uid, out var body) && !HasComp<MobStateComponent>(uid))
        {
            if (_body.GetOrgan(uid, "Head") is { } head)
            {
                headUid = head;
                return true;
            }
        }

        return false;
    }

    private void OnGetVerbs(Entity<OrganComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!IsSeveredHead(ent.Owner, out var headUid))
            return;

        var user = args.User;
        var held = _hands.GetActiveItem(user);
        if (held == null || !HasComp<ScalpelComponent>(held.Value))
            return;

        var head = headUid;
        var tool = held.Value;

        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("head-extraction-verb"),
            Act = () => StartExtraction(user, head, tool),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/cut.svg.192dpi.png")),
            Priority = 1,
        };
        args.Verbs.Add(verb);
    }

    private void OnInteractUsing(Entity<OrganComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<ScalpelComponent>(args.Used))
            return;

        if (!IsSeveredHead(ent.Owner, out var headUid))
            return;

        args.Handled = StartExtraction(args.User, headUid, args.Used);
    }

    private bool StartExtraction(EntityUid user, EntityUid headUid, EntityUid tool)
    {
        if (HasComp<HeadOrgansExtractedComponent>(headUid))
        {
            _popup.PopupClient(Loc.GetString("head-extraction-already-empty"), headUid, user);
            return false;
        }

        return _doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            user,
            TimeSpan.FromSeconds(3.5),
            new SeveredHeadExtractDoAfterEvent(),
            headUid,
            target: headUid,
            used: tool)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        });
    }

    private void OnDoAfter(Entity<OrganComponent> ent, ref SeveredHeadExtractDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var target = ent.Owner;
        if (HasComp<HeadOrgansExtractedComponent>(target))
            return;

        EnsureComp<HeadOrgansExtractedComponent>(target);

        var extractedAny = false;

        if (_container.TryGetContainer(target, "body_part_organs", out var partContainer) && partContainer.ContainedEntities.Count > 0)
        {
            foreach (var contained in partContainer.ContainedEntities.ToArray())
            {
                _container.Remove(contained, partContainer);
                _transform.DropNextTo(contained, target);
                extractedAny = true;
            }
        }

        if (!extractedAny && TryComp<ParentOrganComponent>(target, out var parentComp) && parentComp.Children.Count > 0)
        {
            foreach (var child in parentComp.Children.ToArray())
            {
                if (Exists(child) && !Deleted(child))
                {
                    _transform.DropNextTo(child, target);
                    extractedAny = true;
                }
            }
        }

        if (!extractedAny)
        {
            var brain = Spawn("OrganHumanBrain", Transform(target).Coordinates);
            _transform.DropNextTo(brain, target);
            var eyes = Spawn("OrganHumanEyes", Transform(target).Coordinates);
            _transform.DropNextTo(eyes, target);
        }

        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/flesh_squish.ogg"), target, args.User);
        _popup.PopupPredicted(Loc.GetString("head-extraction-complete", ("user", args.User), ("head", target)), target, args.User, PopupType.Medium);
    }
}

[RegisterComponent, NetworkedComponent]
public sealed partial class HeadOrgansExtractedComponent : Component;

[Serializable, NetSerializable]
public sealed partial class SeveredHeadExtractDoAfterEvent : SimpleDoAfterEvent;
