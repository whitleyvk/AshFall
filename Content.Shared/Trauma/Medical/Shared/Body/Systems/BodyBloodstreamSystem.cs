using Content.Medical.Common.Body;
using Content.Medical.Common.CCVar;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Surgery.Tools;
using Content.Medical.Shared.Wounds;
using Content.Medical.Shared.Traumas;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Body;

public sealed partial class BodyBloodstreamSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private WoundSystem _wound = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private EntityQuery<BleedInflicterComponent> _bleedQuery = default!;
    [Dependency] private EntityQuery<WoundableComponent> _woundableQuery = default!;

    private float _bleedingSeverity = 1f;
    private float _bleedScaleTime = 1f;

    // Bleeding status and scaling are derived from timestamps, so the rescan can run on an interval instead of every tick
    private static readonly TimeSpan BleedUpdateInterval = TimeSpan.FromSeconds(0.5);

    private readonly HashSet<EntityUid> _dirtyBodies = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, SurgeryCVars.BleedingSeverityTrade, x => _bleedingSeverity = x, true);
        Subs.CVar(_cfg, SurgeryCVars.BleedsScalingTime, x => _bleedScaleTime = x, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Bleeding wounds scale over time, so the body-wide total has to be reconciled on the
        // same interval; the event batches one recompute per body instead of per wound.
        _dirtyBodies.Clear();

        var bleedsQuery = EntityQueryEnumerator<BleedInflicterComponent>();
        var now = _timing.CurTime;
        while (bleedsQuery.MoveNext(out var ent, out var bleeds))
        {
            if (now < bleeds.NextUpdate)
                continue;

            bleeds.NextUpdate = now + BleedUpdateInterval;

            var bleeding = bleeds.BleedingAmount > 0 && CanWoundBleed((ent, bleeds));
            var changed = false;
            if (bleeding != bleeds.IsBleeding)
            {
                bleeds.IsBleeding = bleeding;
                DirtyField(ent, bleeds, nameof(BleedInflicterComponent.IsBleeding));
                changed = true;
            }

            if (!bleeds.IsBleeding)
            {
                if (changed && TryGetWoundBody(ent, out var stoppedBody))
                    _dirtyBodies.Add(stoppedBody);
                continue;
            }

            var totalSeconds = (bleeds.ScalingFinishesAt - bleeds.ScalingStartsAt).TotalSeconds;
            var elapsedSeconds = (now - bleeds.ScalingStartsAt).TotalSeconds;

            if (totalSeconds > 0 && bleeds.Scaling < bleeds.ScalingLimit)
            {
                var progress = Math.Clamp(elapsedSeconds / totalSeconds, 0.0, 1.0);
                var newBleeds = FixedPoint2.Clamp(
                    (float) progress * bleeds.ScalingLimit,
                    0,
                    bleeds.ScalingLimit);

                if (bleeds.Scaling != newBleeds)
                {
                    bleeds.Scaling = newBleeds;
                    DirtyField(ent, bleeds, nameof(BleedInflicterComponent.Scaling));
                    changed = true;
                }
            }

            if (changed && TryGetWoundBody(ent, out var body))
                _dirtyBodies.Add(body);
        }

        foreach (var body in _dirtyBodies)
        {
            var ev = new BloodstreamUpdateEvent();
            RaiseLocalEvent(body, ref ev);
        }
    }

    private bool TryGetWoundBody(EntityUid wound, out EntityUid body)
    {
        if (TryComp<WoundComponent>(wound, out var woundComp) &&
            _body.GetBody(woundComp.HoldingWoundable) is { } found)
        {
            body = found;
            return true;
        }

        body = default;
        return false;
    }

    /// <summary>
    /// Add a bleed-ability modifier on woundable
    /// </summary>
    /// <param name="part">The bodypart to apply the modifiers</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <param name="priority">Priority of the said modifier</param>
    /// <param name="canBleed">Should the wounds bleed?</param>
    /// <param name="force">If forced, won't stop after failing to apply one modifier</param>
    /// <returns>Return true if applied</returns>
    public bool TryAddPartBleedModifier(
        Entity<WoundableComponent?> part,
        string identifier,
        int priority,
        bool canBleed,
        bool force = false)
    {
        foreach (var wound in _wound.GetWoundableWounds(part))
        {
            if (!_bleedQuery.TryComp(wound, out var bleeds))
                continue;

            if (TryAddBleedModifier((wound, bleeds), identifier, priority, canBleed))
                continue;

            if (!force)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Add a bleed-ability modifier
    /// </summary>
    /// <param name="ent">The wound</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <param name="priority">Priority of the said modifier</param>
    /// <param name="canBleed">Should the wound bleed?</param>
    /// <returns>Return true if applied</returns>
    public bool TryAddBleedModifier(
        Entity<BleedInflicterComponent?> ent,
        string identifier,
        int priority,
        bool canBleed)
    {
        if (!_bleedQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.BleedingModifiers.TryAdd(identifier, (priority, canBleed)))
            return false;

        DirtyField(ent, ent.Comp, nameof(BleedInflicterComponent.BleedingModifiers));
        return true;
    }

    /// <summary>
    /// Remove a bleed-ability modifier from a woundable
    /// </summary>
    /// <param name="part">The bodypart</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <param name="force">If forced, won't stop applying modifiers after failing one wound</param>
    /// <returns>Returns true if removed all modifiers ON WOUNDABLE</returns>
    public bool TryRemoveBleedModifier(
        Entity<WoundableComponent?> part,
        string identifier,
        bool force = false)
    {
        foreach (var wound in _wound.GetWoundableWounds(part))
        {
            if (!_bleedQuery.TryComp(wound, out var bleeds))
                continue;

            if (TryRemoveBleedModifier((wound, bleeds), identifier))
                continue;

            if (!force)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Remove a bleed-ability modifier
    /// </summary>
    /// <param name="ent">The wound</param>
    /// <param name="identifier">string identifier of the modifier</param>
    /// <returns>Return true if removed</returns>
    public bool TryRemoveBleedModifier(
        Entity<BleedInflicterComponent?> ent,
        string identifier)
    {
        if (!_bleedQuery.Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.BleedingModifiers.Remove(identifier))
            return false;

        DirtyField(ent, ent.Comp, nameof(BleedInflicterComponent.BleedingModifiers));
        return true;
    }

    /// <summary>
    /// Self-explanatory
    /// </summary>
    /// <returns>Returns whether if the wound can bleed</returns>
    public bool CanWoundBleed(Entity<BleedInflicterComponent?> ent)
    {
        if (!_bleedQuery.Resolve(ent, ref ent.Comp))
            return false;

        var nearestModifier = ent.Comp.BleedingModifiers.FirstOrNull();
        if (nearestModifier == null)
            return true; // No modifiers. return true

        var lastCanBleed = true;
        var lastPriority = 0;
        foreach (var (_, pair) in ent.Comp.BleedingModifiers)
        {
            if (pair.Priority <= lastPriority)
                continue;

            lastPriority = pair.Priority;
            lastCanBleed = pair.CanBleed;
        }

        return lastCanBleed;
    }

    [SubscribeLocalEvent]
    private void OnWoundAdded(EntityUid uid, BleedInflicterComponent component, ref WoundAddedEvent args)
    {
        if (!args.Woundable.CanBleed ||
            !CanWoundBleed((uid, component)) ||
            args.Component.WoundSeverityPoint < component.SeverityThreshold)
            return;

        // wounds that BLEED will not HEAL.
        // wounds that bleed. will you heal them, to me?
        component.BleedingAmountRaw = args.Component.WoundSeverityPoint * _bleedingSeverity;

        var formula = (float) (args.Component.WoundSeverityPoint / _bleedScaleTime * component.ScalingSpeed);
        component.ScalingFinishesAt = _timing.CurTime + TimeSpan.FromSeconds(formula);
        component.ScalingStartsAt = _timing.CurTime;
        component.IsBleeding = true;

        Dirty(uid, component);

        if (_body.GetBody(args.Component.HoldingWoundable) is { } body)
            UpdateBodyBleedAmount(body);
    }

    [SubscribeLocalEvent]
    private void OnWoundHealAttempt(EntityUid uid, BleedInflicterComponent component, ref WoundHealAttemptEvent args)
    {
        if (args.IgnoreBlockers)
            return;

        if (component.IsBleeding)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnBleedInflicterSeverityUpdate(EntityUid uid,
        BleedInflicterComponent component,
        ref WoundSeverityPointChangedEvent args)
    {
        if (!CanWoundBleed((uid, component))
            || !_woundableQuery.TryComp(args.Component.HoldingWoundable, out var woundable)
            || !woundable.CanBleed
            || args.NewSeverity < component.SeverityThreshold
            || args.NewSeverity < args.OldSeverity)
            return;

        var oldBleedsAmount = args.OldSeverity * _bleedingSeverity;
        component.BleedingAmountRaw = args.NewSeverity * _bleedingSeverity;

        var severityPenalty = component.BleedingAmountRaw - oldBleedsAmount / _bleedScaleTime;
        component.SeverityPenalty += severityPenalty;

        var formula = (float) (args.NewSeverity / _bleedScaleTime * component.ScalingSpeed);
        component.ScalingFinishesAt = _timing.CurTime + TimeSpan.FromSeconds(formula);
        component.ScalingStartsAt = _timing.CurTime;

        if (!component.IsBleeding)
        {
            component.ScalingLimit += 0.6;
            component.IsBleeding = true;
            // When bleeding is reopened, the severity is increased
        }

        if (component.BleedingAmountRaw > 0)
            component.Scaling = 1;

        Dirty(uid, component);

        if (_body.GetBody(args.Component.HoldingWoundable) is { } body)
            UpdateBodyBleedAmount(body);
    }

    [SubscribeLocalEvent]
    public void OnBleedRemoverSeverityUpdate(Entity<BleedRemoverComponent> ent, ref WoundSeverityPointChangedEvent args)
    {
        var delta = args.NewSeverity - args.OldSeverity;
        var part = args.Component.HoldingWoundable;
        if (delta < ent.Comp.SeverityThreshold ||
            TerminatingOrDeleted(part) ||
            _body.GetBody(part) is not {} body)
            return;

        var result = _wound.TryHealBleedingWounds(part,
            delta * ent.Comp.BleedingRemovalMultiplier,
            out _);

        if (!result)
            return;

        UpdateBodyBleedAmount(body);

        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/lightburn.ogg"), body, body);
        _popup.PopupEntity(Loc.GetString("bloodstream-component-wounds-cauterized"),
            body,
            body,
            PopupType.MediumCaution);
    }

    public void UpdateBodyBleedAmount(EntityUid body)
    {
        if (!TryComp<BloodstreamComponent>(body, out var blood))
            return;

        var total = FixedPoint2.Zero;
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            var totalPartBleeds = FixedPoint2.Zero;
            foreach (var wound in _wound.GetWoundableWounds(part.AsNullable()))
            {
                if (_bleedQuery.TryComp(wound, out var bleeds) && bleeds.IsBleeding)
                    totalPartBleeds += bleeds.BleedingAmount;
            }
            total += totalPartBleeds;

            part.Comp.Bleeds = totalPartBleeds;
        }

        blood.BleedAmountFromWounds = (float) total;
        blood.BleedAmount = blood.BleedAmountFromWounds + blood.BleedAmountNotFromWounds;
        blood.BleedAmount = Math.Clamp(blood.BleedAmount, 0, blood.MaxBleedAmount);
        DirtyFields(body, blood, null, nameof(BloodstreamComponent.BleedAmount), nameof(BloodstreamComponent.BleedAmountFromWounds));

        if (blood.BleedAmount == 0)
        {
            _alerts.ClearAlert(body, blood.BleedingAlert);
        }
        else
        {
            var severity = (short) Math.Clamp(Math.Round(blood.BleedAmount, MidpointRounding.ToZero), 0, 10);
            _alerts.ShowAlert(body, blood.BleedingAlert, severity);
        }
    }

    [SubscribeLocalEvent]
    private void OnBleedShutdown(EntityUid uid, BleedInflicterComponent component, ComponentShutdown args)
    {
        if (TryComp<WoundComponent>(uid, out var wound) &&
            _body.GetBody(wound.HoldingWoundable) is { } body)
        {
            UpdateBodyBleedAmount(body);
        }
    }

    [SubscribeLocalEvent]
    private void OnBodyUpdate(Entity<BodyComponent> ent, ref BloodstreamUpdateEvent args)
    {
        UpdateBodyBleedAmount(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<BodyComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetActiveCautery(args.Used, out var isImprovised))
            return;

        EntityUid? bleedingPart = null;
        if (TryGetTargetedPart(args.User, ent, out var targetedPart) && HasBleedingWounds(targetedPart))
            bleedingPart = targetedPart;

        if (bleedingPart == null)
        {
            _popup.PopupClient(Loc.GetString("cauterize-no-bleeding-wounds"), ent.Owner, args.User);
            return;
        }

        var delay = isImprovised ? 3.0f : 2.0f;
        var startPopup = isImprovised
            ? Loc.GetString("cauterize-welder-begin")
            : Loc.GetString("cauterize-tool-begin");

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, delay, new FieldCauterizeDoAfterEvent(GetNetEntity(bleedingPart.Value)), ent.Owner, target: ent.Owner, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = true
        };
        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        args.Handled = true;
        _popup.PopupClient(startPopup, ent.Owner, args.User);
    }

    private bool TryGetTargetedPart(EntityUid user, Entity<BodyComponent> body, out EntityUid part)
    {
        part = default;
        if (!TryComp<TargetingComponent>(user, out var targeting))
            return false;

        var (partType, symmetry) = _body.ConvertTargetBodyPart(targeting.Target);
        foreach (var organ in _body.GetOrgans<BodyPartComponent>(body.AsNullable()))
        {
            if (organ.Comp.PartType != partType ||
                (symmetry != BodyPartSymmetry.None && organ.Comp.Symmetry != symmetry))
                continue;

            part = organ.Owner;
            return true;
        }

        return false;
    }

    [SubscribeLocalEvent]
    private void OnCauteryToggled(Entity<CauteryComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated)
            return;

        var query = EntityQueryEnumerator<DoAfterComponent>();
        while (query.MoveNext(out var user, out var doAfters))
        {
            foreach (var (id, doAfter) in doAfters.DoAfters)
            {
                if (!doAfter.Cancelled &&
                    doAfter.Args.Used == ent.Owner &&
                    doAfter.Args.Event is FieldCauterizeDoAfterEvent)
                    _doAfter.Cancel(user, id, doAfters);
            }
        }
    }

    private bool TryGetActiveCautery(EntityUid tool, out bool isImprovised)
    {
        isImprovised = false;
        if (!HasComp<CauteryComponent>(tool))
            return false;

        if (TryComp<ItemToggleComponent>(tool, out var toggle))
        {
            isImprovised = true;
            if (!toggle.Activated)
                return false;
        }

        return !TryComp<WelderComponent>(tool, out var welder) || welder.Enabled;
    }

    private bool HasBleedingWounds(EntityUid organUid)
    {
        foreach (var wound in _wound.GetWoundableWounds(organUid))
        {
            if (_bleedQuery.TryComp(wound, out var bleeds) && bleeds.IsBleeding)
                return true;
        }
        return false;
    }

    [SubscribeLocalEvent]
    private void OnCauterizeDoAfter(Entity<BodyComponent> ent, ref FieldCauterizeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (args.Used is not { } used ||
            !Exists(used) ||
            _hands.GetActiveItem(args.User) != used ||
            !TryGetActiveCautery(used, out var isImprovised))
            return;

        var part = GetEntity(args.TargetPart);
        if (!Exists(part) ||
            TerminatingOrDeleted(part) ||
            _body.GetBody(part) != ent.Owner ||
            !TryGetTargetedPart(args.User, ent, out var targetedPart) ||
            targetedPart != part)
            return;

        EntityUid? worstWound = null;
        var worstBleeding = FixedPoint2.Zero;
        foreach (var wound in _wound.GetWoundableWounds(part))
        {
            if (!_bleedQuery.TryComp(wound, out var bleeds) || !bleeds.IsBleeding)
                continue;

            if (worstWound == null || bleeds.BleedingAmountRaw > worstBleeding)
            {
                worstWound = wound;
                worstBleeding = bleeds.BleedingAmountRaw;
            }
        }

        if (worstWound is not { } woundUid || !_bleedQuery.TryComp(woundUid, out var worstBleed))
            return;

        if (isImprovised)
        {
            var burn = new DamageSpecifier();
            burn.DamageDict["Heat"] = 15;
            _damageable.TryChangeDamage(part, burn, origin: args.User);
        }

        worstBleed.BleedingAmountRaw = 0;
        worstBleed.IsBleeding = false;
        worstBleed.Scaling = 0;
        DirtyFields(woundUid, worstBleed, null,
            nameof(BleedInflicterComponent.BleedingAmountRaw),
            nameof(BleedInflicterComponent.IsBleeding),
            nameof(BleedInflicterComponent.Scaling));

        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/lightburn.ogg"), ent.Owner, args.User);
        var msg = isImprovised
            ? Loc.GetString("cauterize-welder-success")
            : Loc.GetString("cauterize-tool-success");
        _popup.PopupEntity(msg, ent.Owner, args.User, PopupType.Medium);
    }
}

[Serializable, NetSerializable]
public sealed partial class FieldCauterizeDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity TargetPart;

    public FieldCauterizeDoAfterEvent(NetEntity targetPart)
    {
        TargetPart = targetPart;
    }
}
