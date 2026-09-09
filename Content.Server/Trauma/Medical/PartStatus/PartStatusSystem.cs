// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text;
using Content.Goobstation.Common.Examine;
using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Medical.Common.Wounds;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.PartStatus;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Server.Chat.Managers;
using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.Damage.Prototypes;
using Content.Shared.HealthExaminable;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Medical.Server.PartStatus;

public sealed partial class PartStatusSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private TraumaSystem _trauma = default!;
    [Dependency] private WoundSystem _wound = default!;
    [Dependency] private EntityQuery<BleedInflicterComponent> _bleedQuery = default!;
    [Dependency] private EntityQuery<BoneComponent> _boneQuery = default!;

    private static readonly BodyPartType[] BodyPartOrder =
    [
        BodyPartType.Head,
        BodyPartType.Torso,
        BodyPartType.Arm,
        BodyPartType.Hand,
        BodyPartType.Leg,
        BodyPartType.Foot,
        BodyPartType.Tail,
        BodyPartType.Wings,
    ];

    private static List<BodyPartSymmetry> _symmetryPriority =
    [
        BodyPartSymmetry.Left,
        BodyPartSymmetry.Right,
        BodyPartSymmetry.None,
    ];

    private const string BleedLocaleStr = "inspect-wound-Bleeding-moderate";
    private const string BoneLocaleStr = "inspect-trauma-BoneDamage";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodyComponent, BodyPartDelimbedEvent>(OnBodyPartDelimbed);
    }

    private void OnBodyPartDelimbed(Entity<BodyComponent> ent, ref BodyPartDelimbedEvent args)
    {
        string msg;
        if (args.User != null && args.User.Value != args.Body)
        {
            msg = Loc.GetString("dismemberment-notification-with-user",
                ("user", Identity.Entity(args.User.Value, EntityManager)),
                ("target", Identity.Entity(args.Body, EntityManager)),
                ("part", Identity.Entity(args.Part, EntityManager)));
        }
        else
        {
            msg = Loc.GetString("dismemberment-notification-passive",
                ("target", Identity.Entity(args.Body, EntityManager)),
                ("part", Identity.Entity(args.Part, EntityManager)));
        }

        var filter = Filter.Pvs(args.Body);
        var color = Color.FromHex("#C62828");
        var wrapped = $"[color=#C62828]{msg}[/color]";
        _chat.ChatMessageToManyFiltered(
            filter,
            ChatChannel.Visual,
            msg,
            wrapped,
            args.Body,
            hideChat: false,
            recordReplay: true,
            colorOverride: color);
    }

    [SubscribeNetworkEvent]
    private void OnGetPartStatus(GetPartStatusEvent message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} entity ||
            _mob.IsIncapacitated(entity)) // fuck you i guess???
            return;

        var partStatusSet = CollectPartStatuses(entity);
        var text = GetExamineText(entity, entity, partStatusSet);

        _chat.ChatMessageToOne(
            ChatChannel.Emotes,
            text.ToMarkup(),
            text.ToMarkup(),
            EntityUid.Invalid,
            false,
            args.SenderSession.Channel,
            recordReplay: false);
    }

    [SubscribeLocalEvent]
    private void OnGetExamineVerbs(EntityUid uid, HealthExaminableComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damage))
            return;

        var detailsRange = _examine.IsInDetailsRange(args.User, uid);

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = CreateMarkup(uid, args.User, component, damage);
                var userEv = new UserExaminedEvent(markup, uid);
                RaiseLocalEvent(args.User, ref userEv);
                markup = userEv.Message;
                _examine.SendExamineTooltip(args.User, uid, markup, false, false);
                var examineCompletedEvent = new Content.Shared.Ashfall.Examine.ExamineCompletedEvent(markup, uid, args.User);
                RaiseLocalEvent(uid, ref examineCompletedEvent);
            },
            Text = Loc.GetString("health-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("health-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    public FormattedMessage CreateMarkup(EntityUid uid, EntityUid examiner, HealthExaminableComponent component, DamageableComponent damage)
    {
        var partStatusSet = CollectPartStatuses(uid);
        var text = GetExamineText(uid, examiner, partStatusSet, true);
        // Anything else want to add on to this?
        RaiseLocalEvent(uid, new HealthBeingExaminedEvent(text), true);

        return text;
    }


    private HashSet<PartStatus> CollectPartStatuses(EntityUid body)
    {
        var partStatusSet = new HashSet<PartStatus>();
        var presentCategories = new HashSet<ProtoId<OrganCategoryPrototype>>();

        foreach (var woundable in _body.GetOrgans<WoundableComponent>(body))
        {
            if (!TryComp<BodyPartComponent>(woundable, out var part) ||
                _body.GetCategory(woundable.Owner) is not {} category)
                continue;

            presentCategories.Add(category);
            var (damageSeverities, isBleeding) = AnalyzeWounds(woundable);
            var boneSev = _boneQuery.CompOrNull(woundable)?.BoneSeverity ?? BoneSeverity.Normal; // fallback for boneless limbs like slimes
            partStatusSet.Add(new PartStatus(
                part.PartType,
                part.Symmetry,
                ProtoMan.Index(category).Name.ToLowerInvariant(), // looks better lowercase
                woundable.Comp.WoundableSeverity,
                damageSeverities,
                boneSev,
                isBleeding));
        }

        // Check for missing limbs
        if (TryComp<InitialBodyComponent>(body, out var initialBody))
        {
            foreach (var (category, proto) in initialBody.Organs)
            {
                if (presentCategories.Contains(category))
                    continue;

                if (!ProtoMan.TryIndex<EntityPrototype>(proto, out var entProto) ||
                    !entProto.TryGetComponent<BodyPartComponent>(out var partComp, EntityManager.ComponentFactory))
                    continue;

                partStatusSet.Add(new PartStatus(
                    partComp.PartType,
                    partComp.Symmetry,
                    ProtoMan.Index(category).Name.ToLowerInvariant(),
                    WoundableSeverity.Severe,
                    new(),
                    BoneSeverity.Normal,
                    false)
                {
                    Missing = true
                });
            }
        }

        return partStatusSet;
    }

    private (Dictionary<string, WoundSeverity> DamageSeverities, bool IsBleeding) AnalyzeWounds(
        Entity<WoundableComponent> part)
    {
        var damageSeverities = new Dictionary<string, WoundSeverity>();
        var isBleeding = false;

        foreach (var wound in _wound.GetWoundableWounds(part.AsNullable()))
        {
            if (wound.Comp.DamageGroup == null
                || wound.Comp.WoundSeverity == WoundSeverity.Healed)
                continue;

            var key = wound.Comp.TextString ?? (ProtoMan.TryIndex<DamageGroupPrototype>(wound.Comp.DamageGroup, out var groupProto)
                ? groupProto.ID
                : wound.Comp.DamageType.Id);

            if (wound.Comp.AlwaysShowInInspects ||
                !damageSeverities.TryGetValue(key, out var existingSeverity) ||
                wound.Comp.WoundSeverity > existingSeverity)
                damageSeverities[key] = wound.Comp.WoundSeverity;

            if (!isBleeding && _bleedQuery.TryComp(wound, out var bleeds) && bleeds.IsBleeding)
                isBleeding = true;
        }

        return (damageSeverities, isBleeding);
    }

    private FormattedMessage GetExamineText(EntityUid entity,
        EntityUid examiner,
        HashSet<PartStatus> partStatusSet,
        bool styling = true)
    {
        var message = new FormattedMessage();
        var titlestring = entity == examiner
            ? "inspect-part-status-title"
            : "inspect-part-status-title-other";

        if (styling)
        {
            message.AddMarkupPermissive("[bold][color=#C5CAC5]" +
                Loc.GetString(titlestring, ("entity", FormattedMessage.EscapeText(Identity.Name(entity, EntityManager)))) +
                "[/color][/bold]");
            message.PushNewline();
            AddLine(message);
        }
        else
        {
            titlestring += "-styleless";
            message.AddMarkupPermissive(Loc.GetString(titlestring,
                ("entity", FormattedMessage.EscapeText(Identity.Name(entity, EntityManager)))));
            message.PushNewline();
        }
        CreateBodyPartMessage(partStatusSet, entity == examiner, ref message, !styling);

        return message;
    }

    private void CreateBodyPartMessage(HashSet<PartStatus> partStatusSet,
        bool inspectingSelf,
        ref FormattedMessage message,
        bool styleless = false)
    {
        var orderedParts = BodyPartOrder
            .SelectMany(partType => partStatusSet.Where(p => p.PartType == partType)
                .ToList()
                .OrderBy(p => _symmetryPriority.IndexOf(p.PartSymmetry)))
            .ToList();

        foreach (var partStatus in orderedParts)
        {
            var possessive = inspectingSelf
                ? Loc.GetString("inspect-part-status-you")
                : Loc.GetString("inspect-part-status-their");

            string locString;
            if (partStatus.Missing)
            {
                locString = styleless ? "inspect-part-status-line-missing-styleless" : "inspect-part-status-line-missing";
            }
            else
            {
                var healthy = IsHealthy(partStatus);
                if (healthy)
                    locString = styleless ? "inspect-part-status-line-styleless" : "inspect-part-status-line-fine";
                else
                    locString = styleless ? "inspect-part-status-line-styleless" : "inspect-part-status-line";
            }

            var statusDescription = partStatus.Missing ? string.Empty : BuildStatusDescription(partStatus, inspectingSelf);

            var line = Loc.GetString(locString,
                ("possessive", possessive),
                ("part", partStatus.PartName),
                ("status", statusDescription));

            if (styleless)
            {
                message.AddMarkupPermissive(line);
            }
            else
            {
                var statusColor = GetStatusColor(partStatus);
                message.AddMarkupPermissive($"[color={statusColor}]{line}[/color]");
            }

            message.PushNewline();
        }
    }

    private static string GetStatusColor(PartStatus status)
    {
        if (status.Missing)
            return "#FF4444";

        if (IsHealthy(status))
            return "#6F7470";

        if (status.Bleeding || status.BoneSeverity > BoneSeverity.Normal)
            return "#E06C75";

        return status.PartSeverity switch
        {
            WoundableSeverity.Minor => "#D7BA7D",
            WoundableSeverity.Moderate => "#E5A45B",
            _ => "#E06C75",
        };
    }

    private static bool IsHealthy(PartStatus status)
    {
        return !status.Missing &&
               status.PartSeverity == WoundableSeverity.Healthy &&
               status.DamageSeverities.Count == 0 &&
               status.BoneSeverity == BoneSeverity.Normal &&
               !status.Bleeding;
    }

    private string BuildStatusDescription(PartStatus partStatus, bool inspectingSelf)
    {
        var sb = new StringBuilder();
        var hasStatus = false;

        // Get overall wound severity
        var overallSeverity = GetOverallWoundSeverity(partStatus.DamageSeverities);
        if (overallSeverity != WoundSeverity.Healed)
        {
            var localeText = $"inspect-wound-{overallSeverity.ToString().ToLower()}";
            sb.Append(Loc.GetString(localeText));
            hasStatus = true;
        }
        else if (partStatus.PartSeverity > WoundableSeverity.Healthy)
        {
            var localeText = $"inspect-wound-{partStatus.PartSeverity.ToString().ToLower()}";
            sb.Append(Loc.GetString(localeText));
            hasStatus = true;
        }

        // Add damage group descriptions
        var damageDescriptions = GetDamageGroupDescriptions(partStatus.DamageSeverities, inspectingSelf);
        if (damageDescriptions.Count > 0)
        {
            if (hasStatus)
                sb.Append(Loc.GetString("inspect-part-status-comma"));
            sb.Append(Loc.GetString("inspect-part-status-conjunction"));
            sb.Append(string.Join(" ", damageDescriptions));
            hasStatus = true;
        }

        // Add trauma descriptions
        var traumaDescriptions = GetTraumaDescriptions(partStatus, inspectingSelf);
        if (traumaDescriptions.Count > 0)
        {
            if (hasStatus)
                sb.Append(Loc.GetString("inspect-part-status-conjunction2"));
            else
                sb.Append(Loc.GetString("inspect-part-status-conjunction3"));
            sb.Append(string.Join(Loc.GetString("inspect-part-status-comma"), traumaDescriptions));
            hasStatus = true;
        }

        if (!hasStatus)
        {
            if (partStatus.PartSeverity > WoundableSeverity.Healthy)
            {
                var localeText = $"inspect-wound-{partStatus.PartSeverity.ToString().ToLower()}";
                sb.Append(Loc.GetString(localeText));
            }
            else
            {
                sb.Append(Loc.GetString("inspect-part-status-fine"));
            }
        }

        return sb.ToString();
    }

    private WoundSeverity GetOverallWoundSeverity(Dictionary<string, WoundSeverity> damageSeverities)
    {
        if (damageSeverities.Count == 0)
            return WoundSeverity.Healed;

        var maxSeverity = WoundSeverity.Healed;
        foreach (var (type, severity) in damageSeverities)
        {
            if (!WoundSeverityCheck(type) || severity <= maxSeverity)
                continue;

            maxSeverity = severity;
        }
        return maxSeverity;
    }

    private List<string> GetDamageGroupDescriptions(Dictionary<string, WoundSeverity> damageSeverities, bool inspectingSelf)
    {
        var descriptions = new List<string>();
        foreach (var (type, severity) in damageSeverities)
        {
            if (!WoundSeverityCheck(type))
                continue;

            var cappedSeverity = severity > WoundSeverity.Severe ? WoundSeverity.Severe : severity;
            var localeText = $"inspect-wound-{type}-{cappedSeverity.ToString().ToLower()}";
            descriptions.Add(Loc.GetString(localeText));
        }

        if (descriptions.Count > 1)
        {
            var lastDescription = descriptions[^1];
            descriptions[^1] = Loc.GetString("inspect-part-status-and") + lastDescription;
        }

        return descriptions;
    }

    private bool WoundSeverityCheck(string type)
    {
        return !ProtoMan.HasIndex<DamageGroupPrototype>(type) || type is "Brute" or "Burn";
    }

    private List<string> GetTraumaDescriptions(PartStatus partStatus, bool inspectingSelf)
    {
        var descriptions = new List<string>();

        // TODO: Dehardcode this guscode from bone traumas when we actually have more organ traumas.

        // Add bone trauma
        if (partStatus.BoneSeverity > BoneSeverity.Normal)
        {
            var localeText = inspectingSelf ? "self-inspect-trauma-BoneDamage" : "inspect-trauma-BoneDamage";
            descriptions.Add(Loc.GetString(localeText));
        }

        // Add bleeding status
        if (partStatus.Bleeding)
        {
            var localeText = "inspect-wound-Bleeding-moderate";
            descriptions.Add(Loc.GetString(localeText));
        }

        // If we have multiple traumas, add "and it" before the last one
        if (descriptions.Count > 1)
        {
            var lastDescription = descriptions[^1];
            descriptions[^1] = Loc.GetString("inspect-part-status-and") + lastDescription;
        }

        return descriptions;
    }

    private void AddLine(FormattedMessage message)
    {
        message.PushColor(Color.FromHex("#878C87"));
        message.AddText(Loc.GetString("examine-border-line"));
        message.PushNewline();
        message.Pop();
    }
}
