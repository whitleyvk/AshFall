// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Common.Traumas;
using Content.Medical.Shared.DelayedDeath;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Traits.Assorted;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Medical.CPR;

public abstract partial class SharedCPRSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedRottingSystem _rotting = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityQuery<ActiveCPRComponent> _activeQuery = default!;
    [Dependency] private EntityQuery<CPRTrainingComponent> _trainingQuery = default!;
    [Dependency] private EntityQuery<DamageableComponent> _damageQuery = default!;
    [Dependency] private EntityQuery<InternalChildOrganComponent> _organQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobQuery = default!;
    [Dependency] private EntityQuery<RottingComponent> _rottingQuery = default!;
    [Dependency] private EntityQuery<UnrevivableComponent> _unrevivableQuery = default!;

    public static readonly ProtoId<OrganCategoryPrototype> LungsCategory = "Lungs";
    public static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";
    public static readonly ProtoId<OrganCategoryPrototype> BrainCategory = "Brain";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CPRTrainingComponent, GetVerbsEvent<InnateVerb>>(OnGetVerbs);
        SubscribeLocalEvent<CPRTrainingComponent, ComponentShutdown>(OnPerformerShutdown);
        SubscribeLocalEvent<ActiveCPRComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActiveCPRComponent, CPRDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ActiveCPRComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    public bool IsCPRActive(EntityUid uid)
        => _activeQuery.HasComp(uid);

    public void SetResuscitationChance(Entity<CPRTrainingComponent?> ent, float chance)
    {
        if (!_trainingQuery.Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.ResuscitationChance = chance;
        Dirty(ent, ent.Comp);
    }

    private void OnGetVerbs(Entity<CPRTrainingComponent> ent, ref GetVerbsEvent<InnateVerb> args)
    {
        var target = args.Target;
        if (!args.CanInteract ||
            !args.CanAccess ||
            !_mobQuery.TryComp(target, out var state) ||
            state.CurrentState == MobState.Alive)
        {
            return;
        }

        args.Verbs.Add(new InnateVerb
        {
            Act = () => StartCPR(ent, target),
            Text = Loc.GetString("cpr-verb"),
            Icon = new SpriteSpecifier.Rsi(new ResPath("Interface/Alerts/human_alive.rsi"), "health4"),
            Priority = 2
        });
    }

    private void StartCPR(Entity<CPRTrainingComponent> performer, EntityUid target)
    {
        var identity = Identity.Entity(target, EntityManager);
        if (!CanStartCPR(performer, target, identity))
            return;

        var args = new DoAfterArgs(
            EntityManager,
            performer,
            performer.Comp.DoAfterDuration,
            new CPRDoAfterEvent(),
            eventTarget: target,
            target: target)
        {
            BreakOnMove = true,
            BreakOnHandChange = true,
            BreakOnDamage = true,
            NeedHand = true,
            BlockDuplicate = true
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        var active = EnsureComp<ActiveCPRComponent>(target);
        active.Performer = performer;
        if (_audio.PlayPredicted(
            performer.Comp.CPRSound,
            performer,
            performer,
            performer.Comp.CPRSound.Params.WithLoop(true)) is { } stream)
            active.Sound = stream.Entity;

        if (_net.IsServer)
            Dirty(target, active);

        var userIdentity = Identity.Entity(performer, EntityManager);
        _popup.PopupEntity(Loc.GetString("cpr-start-second-person", ("target", identity)), target, performer);
        _popup.PopupEntity(Loc.GetString("cpr-start-second-person-patient", ("user", userIdentity)), target, target);
    }

    private bool CanStartCPR(Entity<CPRTrainingComponent> performer, EntityUid target, EntityUid identity)
    {
        if (_activeQuery.HasComp(target))
        {
            _popup.PopupEntity(Loc.GetString("cpr-already-performing", ("entity", identity)), performer, performer, PopupType.Medium);
            return false;
        }

        return CanPerformCPR(performer, target, identity);
    }

    private bool CanPerformCPR(Entity<CPRTrainingComponent> performer, EntityUid target, EntityUid identity)
    {
        if (_body.GetOrgan(target, "Head") == null)
        {
            _popup.PopupEntity(Loc.GetString("cpr-target-nohead", ("entity", identity)), performer, performer, PopupType.LargeCaution);
            return false;
        }

        if (_rottingQuery.HasComp(target))
        {
            _popup.PopupEntity(Loc.GetString("cpr-target-rotting", ("entity", identity)), performer, performer, PopupType.LargeCaution);
            return false;
        }

        if (!HasHealthyOrgan(target, LungsCategory) || !HasHealthyOrgan(performer, LungsCategory))
        {
            _popup.PopupEntity(Loc.GetString("cpr-target-cantbreathe", ("entity", identity)), performer, performer, PopupType.MediumCaution);
            return false;
        }

        if (_inventory.TryGetSlotEntity(target, "outerClothing", out var outer))
        {
            _popup.PopupEntity(Loc.GetString("cpr-must-remove", ("clothing", outer)), performer, performer, PopupType.Medium);
            return false;
        }

        return _ingestion.HasMouthAvailable(performer, performer) &&
               _ingestion.HasMouthAvailable(target, performer);
    }

    private void OnShutdown(Entity<ActiveCPRComponent> ent, ref ComponentShutdown args)
    {
        _audio.Stop(ent.Comp.Sound);
    }

    private void OnPerformerShutdown(Entity<CPRTrainingComponent> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<ActiveCPRComponent>();
        while (query.MoveNext(out var patient, out var active))
        {
            if (active.Performer == ent.Owner)
            {
                _audio.Stop(active.Sound);
                RemCompDeferred(patient, active);
            }
        }
    }

    private void OnMobStateChanged(Entity<ActiveCPRComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
        {
            _audio.Stop(ent.Comp.Sound);
            RemCompDeferred(ent, ent.Comp);
        }
    }

    private void OnDoAfter(Entity<ActiveCPRComponent> patient, ref CPRDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            _audio.Stop(patient.Comp.Sound);
            RemCompDeferred(patient, patient.Comp);
            return;
        }

        var performer = args.User;
        var identity = Identity.Entity(patient, EntityManager);
        if (!_trainingQuery.TryComp(performer, out var training) ||
            !_mobQuery.TryComp(patient, out var state) ||
            state.CurrentState == MobState.Alive ||
            !CanPerformCPR((performer, training), patient, identity))
        {
            _audio.Stop(patient.Comp.Sound);
            RemCompDeferred(patient, patient.Comp);
            return;
        }

        if (!training.CPRHealing.Empty)
            _damageable.TryChangeDamage(patient.Owner, training.CPRHealing, true, origin: performer);

        if (TryComp<DelayedDeathComponent>(patient, out var delayedDeath))
        {
            delayedDeath.NextDeath += training.DoAfterDuration;
            Dirty(patient, delayedDeath);
        }

        if (training.RotReductionMultiplier > 0)
            _rotting.ReduceAccumulator(patient, training.DoAfterDuration * training.RotReductionMultiplier);

        TryBreathe(patient);

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(patient));
        if (state.CurrentState == MobState.Dead &&
            random.Prob(training.ResuscitationChance) &&
            CanRevive(patient))
        {
            _mob.ChangeMobState(patient, MobState.Critical, state, performer);
        }

        args.Repeat = state.CurrentState != MobState.Alive;
        if (!args.Repeat)
        {
            _audio.Stop(patient.Comp.Sound);
            RemCompDeferred(patient, patient.Comp);
        }
    }

    private bool HasHealthyOrgan(EntityUid body, ProtoId<OrganCategoryPrototype> category)
        => _body.GetOrgan(body, category) is { } organ &&
           _organQuery.TryComp(organ, out var internalOrgan) &&
           internalOrgan.OrganSeverity == OrganSeverity.Normal;

    private bool CanRevive(EntityUid uid)
        => !_unrevivableQuery.HasComp(uid) &&
           !HasComp<DebrainedComponent>(uid) &&
           HasHealthyOrgan(uid, HeartCategory) &&
           HasHealthyOrgan(uid, BrainCategory) &&
           (!TryComp<DelayedDeathComponent>(uid, out var delayedDeath) || !delayedDeath.PreventAllRevives) &&
           _threshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold) &&
           _damageQuery.TryComp(uid, out var damage) &&
           _threshold.CheckVitalDamage((uid, damage)) < threshold;

    protected virtual void TryBreathe(EntityUid uid)
    {
    }
}
