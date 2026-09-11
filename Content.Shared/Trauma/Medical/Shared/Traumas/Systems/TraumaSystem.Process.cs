// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Body;
using Content.Medical.Common.Healing;
using Content.Medical.Common.Traumas;
using Content.Medical.Common.Wounds;
using Content.Medical.Shared.Body;
using Content.Medical.Shared.Wounds;
using Content.Shared.Armor;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Medical.Shared.Traumas;

public partial class TraumaSystem
{
    [Dependency] private BodyPartSystem _part = default!;
    [Dependency] private EntityQuery<AmputationTraumaComponent> _amputationQuery = default!;
    [Dependency] private EntityQuery<ArmorComponent> _armorQuery = default!;
    [Dependency] private EntityQuery<GodmodeComponent> _godmodeQuery = default!;
    [Dependency] private EntityQuery<TraumaComponent> _traumaQuery = default!;
    [Dependency] private EntityQuery<TraumaInflicterComponent> _query = default!;
    [Dependency] private EntityQuery<WoundableComponent> _woundableQuery = default!;

    private const string TraumaContainerId = "Traumas";
    // TODO SHITMED: this should be a bool on the trauma entity or something
    public static readonly TraumaType[] TraumasBlockingHealing = { TraumaType.BoneDamage, TraumaType.OrganDamage };

    public static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";
    public static readonly ProtoId<DamageGroupPrototype> Brute = "Brute";
    /// <summary>
    /// Prevent using bruise packs if a part has more than this many bleed stacks from wounds.
    /// Should be replaced by arterial bleeding in the future...
    /// </summary>
    public const float MinBleedToStopHealing = 5f;

    private readonly List<Entity<TraumaComponent>> _traumas = new(8);

    [SubscribeLocalEvent]
    private void OnTraumaInflicterInit(Entity<TraumaInflicterComponent> ent, ref ComponentInit args)
    {
        ent.Comp.TraumaContainer = _container.EnsureContainer<Container>(ent, TraumaContainerId);
    }

    [SubscribeLocalEvent]
    private void OnWoundSeverityPointChanged(
        Entity<TraumaInflicterComponent> wound,
        ref WoundSeverityPointChangedEvent args)
    {
        var part = args.Component.HoldingWoundable;
        if (_godmodeQuery.HasComp(part))
            return;

        // Overflow is only used when we are capping the wound, so we use it over the computed delta
        // which will be useless in this specific scenario.
        var delta = args.Overflow ?? args.NewSeverity - args.OldSeverity;
        if (delta <= 0 || delta < wound.Comp.SeverityThreshold)
            return;

        if (!_woundableQuery.TryComp(part, out var woundable))
            return;

        var traumasToInduce = RandomTraumaChance((part, woundable), wound, delta);
        if (traumasToInduce.Count <= 0)
            return;

        ApplyTraumas((part, woundable), wound, traumasToInduce, delta);
    }

    [SubscribeLocalEvent]
    private void OnWoundHealAttempt(Entity<TraumaInflicterComponent> ent, ref WoundHealAttemptEvent args)
    {
        if (args.IgnoreBlockers)
            return;

        foreach (var trauma in GetAllWoundTraumas(ent.AsNullable()))
        {
            if (TraumasBlockingHealing.Contains(trauma.Comp.TraumaType))
            {
                if (trauma.Comp.TraumaType == TraumaType.BoneDamage &&
                    _boneQuery.TryComp(args.Woundable, out var bone) &&
                    bone.BoneSeverity != BoneSeverity.Broken)
                    continue;

                args.Cancelled = true;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnPartHealAttempt(Entity<WoundableComponent> ent, ref PartHealAttemptEvent args)
    {
        args.Bleeding = ent.Comp.Bleeds > MinBleedToStopHealing;

        var part = ent.AsNullable();
        if (_wound.GetWoundableWounds(part).Any(wound => !_wound.CanHealWound(wound)))
        {
            args.Cancelled = true;
            return;
        }

        if (TraumasBlockingHealing.Any(traumaType => HasWoundableTrauma(ent.AsNullable(), traumaType, false)))
            args.Cancelled = true;
    }

    #region Public API

    public IEnumerable<Entity<TraumaComponent>> GetAllWoundTraumas(Entity<TraumaInflicterComponent?> wound)
    {
        if (!_query.Resolve(wound, ref wound.Comp, false))
            yield break;

        foreach (var trauma in wound.Comp.TraumaContainer.ContainedEntities)
        {
            if (_traumaQuery.TryComp(trauma, out var traumaComp))
                yield return (trauma, traumaComp);
        }
    }

    public bool HasAssociatedTrauma(
        EntityUid part,
        Entity<TraumaInflicterComponent?> wound,
        TraumaType? traumaType = null,
        bool showAll = true)
    {
        var boneBroken = showAll && _boneQuery.TryComp(part, out var bone) && bone.BoneSeverity == BoneSeverity.Broken;
        foreach (var trauma in GetAllWoundTraumas(wound))
        {
            if (traumaType != null && trauma.Comp.TraumaType != traumaType)
                continue;

            if (!showAll)
            {
                // TODO: Fill this with other blocking traumas.
                if (trauma.Comp.TraumaType == TraumaType.BoneDamage && !boneBroken)
                    continue;
            }

            return true;
        }

        return false;
    }

    public void AddWoundTraumas(
        Entity<TraumaInflicterComponent?> wound,
        List<Entity<TraumaComponent>> traumas,
        TraumaType? traumaType = null)
    {
        foreach (var trauma in GetAllWoundTraumas(wound))
        {
            if (traumaType != null && trauma.Comp.TraumaType != traumaType)
                continue;

            traumas.Add(trauma);
        }
    }

    public bool HasWoundableTrauma(
        Entity<WoundableComponent?> part,
        TraumaType? traumaType = null,
        bool showAll = true) // Used to skip certain non-lethal traumas like minor bone fractures.
    {
        if (!_woundableQuery.Resolve(part, ref part.Comp))
            return false;

        foreach (var wound in part.Comp.Wounds.ContainedEntities)
        {
            if (!_query.TryComp(wound, out var inflicter))
                continue;

            if (HasAssociatedTrauma(part, (wound, inflicter), traumaType, showAll))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Get all traumas on a bodypart, optionally of a certain type.
    /// </summary>
    public bool GetPartTraumas(
        Entity<WoundableComponent?> part,
        out List<Entity<TraumaComponent>> traumas,
        TraumaType? traumaType = null)
    {
        traumas = new List<Entity<TraumaComponent>>();
        AddPartTraumas(part, traumas, traumaType);
        return traumas.Count > 0;
    }

    public void AddPartTraumas(
        Entity<WoundableComponent?> part,
        List<Entity<TraumaComponent>> traumas,
        TraumaType? traumaType = null)
    {
        if (!_woundableQuery.Resolve(part, ref part.Comp, false))
            return;

        foreach (var wound in _wound.GetWoundableWounds(part))
        {
            if (!_query.TryComp(wound, out var inflicter))
                continue;

            AddWoundTraumas((wound, inflicter), traumas, traumaType);
        }
    }

    /// <summary>
    /// Get all traumas on a body, optionally of a certain type.
    /// </summary>
    public List<Entity<TraumaComponent>> GetBodyTraumas(
        Entity<BodyComponent?> body,
        TraumaType? traumaType = null)
    {
        var traumas = new List<Entity<TraumaComponent>>();
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            AddPartTraumas(part.AsNullable(), traumas, traumaType);
        }

        return traumas;
    }

    public List<TraumaType> RandomTraumaChance(
        Entity<WoundableComponent?> part,
        Entity<TraumaInflicterComponent> wound,
        FixedPoint2 severity)
    {
        var traumaList = new List<TraumaType>();
        if (!_woundableQuery.Resolve(part, ref part.Comp))
            return traumaList;

        if (severity < wound.Comp.MinTraumaSeverityDelta)
            return traumaList;

        var target = (part, part.Comp);
        if (wound.Comp.AllowedTraumas.Contains(TraumaType.BoneDamage) &&
            RandomBoneTraumaChance(target, wound))
            traumaList.Add(TraumaType.BoneDamage);

        if (wound.Comp.AllowedTraumas.Contains(TraumaType.Dismemberment) &&
            RandomDismembermentTraumaChance(target, wound))
            traumaList.Add(TraumaType.Dismemberment);

        if (wound.Comp.AllowedTraumas.Contains(TraumaType.OrganDamage) &&
            RandomOrganTraumaChance(target, wound))
            traumaList.Add(TraumaType.OrganDamage);

        return traumaList;
    }

    public FixedPoint2 GetArmourChanceDeduction(EntityUid body, Entity<TraumaInflicterComponent> inflicter, TraumaType traumaType, BodyPartType coverage)
    {
        var total = FixedPoint2.Zero;

        foreach (var ent in _inventory.GetHandOrInventoryEntities(body, SlotFlags.WITHOUT_POCKET))
        {
            if (!_armorQuery.TryComp(ent, out var armour))
                continue;

            var deduction = armour.TraumaDeductions.GetValueOrDefault(traumaType, FixedPoint2.Zero);
            if (!inflicter.Comp.AllowArmourDeduction.Contains(traumaType) || deduction == 0)
                continue;

            var covered = armour.ArmorCoverage;
            if (covered.Contains(coverage))
                total += deduction;
        }

        return total;
    }

    public FixedPoint2 GetTraumaChanceDeduction(
        Entity<TraumaInflicterComponent> wound,
        EntityUid body,
        Entity<WoundableComponent> part,
        FixedPoint2 severity,
        TraumaType traumaType,
        BodyPartType coverage)
    {
        var deduction = part.Comp.TraumaDeductions.GetValueOrDefault(traumaType, FixedPoint2.Zero);
        deduction += GetArmourChanceDeduction(body, wound, traumaType, coverage);
        return deduction;
    }

    public void ApplyMangledTraumas(Entity<WoundableComponent> part,
        Entity<TraumaInflicterComponent?> wound,
        FixedPoint2 severity, EntityUid? user = null)
    {
        if (!_query.Resolve(wound, ref wound.Comp) ||
            _body.GetBody(part.Owner) is not {} body)
            return;

        var inflicter = (wound.Owner, wound.Comp);

        if (wound.Comp.AllowedTraumas.Contains(TraumaType.BoneDamage))
            AddTrauma(body, part.Owner, inflicter, TraumaType.BoneDamage, severity);

        if (wound.Comp.AllowedTraumas.Contains(TraumaType.OrganDamage))
        {
            foreach (var organ in _part.GetPartOrgans(part.Owner).Values)
            {
                if (HasComp<InternalChildOrganComponent>(organ))
                    AddTrauma(body, organ.Owner, inflicter, TraumaType.OrganDamage, severity);
            }
        }
    }

    #endregion

    #region Trauma Chance Randoming

    public bool RandomBoneTraumaChance(
        Entity<WoundableComponent> target,
        Entity<TraumaInflicterComponent> woundInflicter)
    {
        if (!_boneQuery.TryComp(target, out var bone) ||
            _body.GetBody(target.Owner) is not {} body ||
            _part.GetPartType(target.Owner) is not {} partType)
            return false;

        var deduction = GetTraumaChanceDeduction(
            woundInflicter,
            body,
            target,
            Comp<WoundComponent>(woundInflicter).WoundSeverityPoint,
            TraumaType.BoneDamage,
            partType);

        if (deduction == 1)
            return false;

        var chance =
            FixedPoint2.Clamp(
                (target.Comp.IntegrityCap - target.Comp.Integrity) / target.Comp.IntegrityCap
                - deduction + woundInflicter.Comp.TraumasChances.GetValueOrDefault(TraumaType.BoneDamage, FixedPoint2.Zero),
                0,
                1);

        return _random.Prob((float) chance);
    }

    public bool RandomOrganTraumaChance(
        Entity<WoundableComponent> target,
        Entity<TraumaInflicterComponent> woundInflicter)
    {
        if (_body.GetBody(target.Owner) is not {} body ||
            _part.GetPartType(target.Owner) is not {} partType)
            return false;

        if (!woundInflicter.Comp.TraumasChances.ContainsKey(TraumaType.OrganDamage))
            return false; // No entity to apply traumas to

        var totalIntegrity = FixedPoint2.Zero;
        foreach (var organ in _part.GetPartOrgans(target.Owner).Values)
        {
            if (!_internalQuery.TryComp(organ, out var organComp))
                continue;

            totalIntegrity += organComp.OrganIntegrity;
        }

        if (totalIntegrity <= 0) // No surviving organs
            return false;

        var deduction = GetTraumaChanceDeduction(
            woundInflicter,
            body,
            target,
            Comp<WoundComponent>(woundInflicter).WoundSeverityPoint,
            TraumaType.OrganDamage,
            partType);

        if (deduction == 1)
            return false;

        var damageRatio = target.Comp.IntegrityCap > 0
            ? (target.Comp.IntegrityCap - target.Comp.Integrity) / target.Comp.IntegrityCap
            : FixedPoint2.Zero;

        var inflicterChance = woundInflicter.Comp.TraumasChances.GetValueOrDefault(TraumaType.OrganDamage, FixedPoint2.Zero);
        var chance = FixedPoint2.Clamp(damageRatio * inflicterChance - deduction, 0, 1);

        return _random.Prob((float) chance);
    }

    public bool RandomDismembermentTraumaChance(
        Entity<WoundableComponent> target,
        Entity<TraumaInflicterComponent> woundInflicter)
    {
        // Can't sever if already severed
        if (_body.GetBody(target.Owner) is not {} body ||
            _part.GetPartType(target.Owner) is not {} partType ||
            // can't dismember the root part
            !target.Comp.CanRemove ||
            _part.GetParentPart(target.Owner) == null)
            return false;

        var deduction = GetTraumaChanceDeduction(
            woundInflicter,
            body,
            target,
            Comp<WoundComponent>(woundInflicter).WoundSeverityPoint,
            TraumaType.Dismemberment,
            partType);

        if (deduction == 1)
            return false;

        // Healthy bones decrease the chance of your limb getting delimbed
        var multiplier = 1f;
        if (_boneQuery.TryComp(target, out var bone))
        {
            multiplier = bone.BoneSeverity switch
            {
                BoneSeverity.Normal => 0.3f, // decreases delimb change by 70%
                BoneSeverity.Damaged => 0.6f, // 40%
                BoneSeverity.Cracked => 1f, // 0%,
                BoneSeverity.Broken => 1.2f, // increases by 20%
                _ => 1f
            };
        }

        var integrityRatio = target.Comp.IntegrityCap > 0
            ? target.Comp.Integrity.Float() / target.Comp.IntegrityCap.Float()
            : 0f;
        var damageRatio = Math.Clamp(1f - integrityRatio, 0f, 1f);
        var baseChance = MathF.Pow(damageRatio, 1.5f);
        var inflicterChance = woundInflicter.Comp.TraumasChances.GetValueOrDefault(TraumaType.Dismemberment, FixedPoint2.Zero).Float();

        float chance = (baseChance * inflicterChance * multiplier) - deduction.Float();
        return _random.Prob(Math.Clamp(chance, 0f, 1f));
    }

    public EntityUid AddTrauma(
        EntityUid target,
        EntityUid part,
        Entity<TraumaInflicterComponent> wound,
        TraumaType traumaType,
        FixedPoint2 severity,
        [ForbidLiteral] ProtoId<OrganCategoryPrototype>? source = null)
    {
        if (TerminatingOrDeleted(wound))
            return EntityUid.Invalid;

        foreach (var trauma in wound.Comp.TraumaContainer.ContainedEntities)
        {
            if (!_traumaQuery.TryComp(trauma, out var containedTraumaComp))
                continue;

            if (containedTraumaComp.TraumaType != traumaType)
                continue;

            // Allows us to create multiple dismemberment traumas on the same body part.
            if (source != null && _amputationQuery.CompOrNull(trauma)?.Source != source)
                continue;

            containedTraumaComp.TraumaSeverity = severity;
            return trauma;
        }

        if (!wound.Comp.TraumaPrototypes.TryGetValue(traumaType, out var id))
            return EntityUid.Invalid;

        var traumaEnt = PredictedSpawnInContainerOrDrop(id, wound, TraumaContainerId);
        var traumaComp = EnsureComp<TraumaComponent>(traumaEnt);

        traumaComp.TraumaSeverity = severity;
        traumaComp.TraumaTarget = target;
        traumaComp.Wound = wound;
        traumaComp.HoldingWoundable = part;
        Dirty(traumaEnt, traumaComp);

        if (source != null)
        {
            var amputation = EnsureComp<AmputationTraumaComponent>(traumaEnt);
            amputation.Source = source.Value;
            Dirty(traumaEnt, amputation);
        }

        return traumaEnt;
    }

    public void RemoveTraumas(Entity<WoundableComponent?> part, TraumaType type)
    {
        if (!GetPartTraumas(part, out var traumas, type))
            return;

        foreach (var trauma in traumas)
        {
            RemoveTrauma(trauma);
        }
    }

    public void RemoveTrauma(Entity<TraumaComponent> trauma)
    {
        var ev = new TraumaBeingRemovedEvent(trauma);
        RaiseLocalEvent(trauma.Comp.Wound, ref ev);

        PredictedDel(trauma.Owner);
    }

    #endregion

    #region Private API

    private void ApplyTraumas(Entity<WoundableComponent> target, Entity<TraumaInflicterComponent> inflicter,
        List<TraumaType> traumas, FixedPoint2 severity, EntityUid? user = null)
    {
        if (!_organQuery.TryComp(target, out var organ) || organ.Body is not { } body)
            return;

        var category = organ.Category;
        foreach (var trauma in traumas)
        {
            EntityUid? targetChosen = null;
            switch (trauma)
            {
                case TraumaType.BoneDamage:
                    targetChosen = target;
                    break;

                case TraumaType.OrganDamage:
                    var organs = new List<EntityUid>();
                    foreach (var partOrgan in _part.GetPartOrgans(target.Owner).Values)
                    {
                        if (_internalQuery.HasComp(partOrgan))
                            organs.Add(partOrgan);
                    }

                    var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(target), GetNetEntity(inflicter));
                    rand.Shuffle(organs);

                    if (organs.FirstOrNull() is {} chosenOrgan)
                        targetChosen = chosenOrgan;

                    break;
                case TraumaType.Dismemberment:
                    targetChosen = _part.GetParentPart(target.Owner);
                    break;
            }

            if (targetChosen == null)
                continue;

            switch (trauma)
            {
                case TraumaType.BoneDamage:
                    ApplyBoneTrauma(target.Owner, inflicter, severity);
                    break;

                case TraumaType.OrganDamage:
                    var traumaEnt = AddTrauma(targetChosen.Value, target, inflicter, TraumaType.OrganDamage, severity);

                    if (traumaEnt != EntityUid.Invalid
                        && !TryChangeOrganDamageModifier(targetChosen.Value, severity, traumaEnt, "WoundableDamage"))
                    {
                        TryCreateOrganDamageModifier(targetChosen.Value, severity, traumaEnt, "WoundableDamage");
                    }

                    break;

                case TraumaType.Dismemberment:
                    if (_part.GetParentPart(target.Owner) != null && // can't amputate a torso
                        _wound.TryCreateWound(targetChosen.Value, Blunt.Id, 0, out var woundCreated, Brute)) // We need this to add the trauma into.
                    {
                        AddTrauma(
                            targetChosen.Value,
                            targetChosen.Value,
                            (woundCreated.Value.Owner, EnsureComp<TraumaInflicterComponent>(woundCreated.Value.Owner)),
                            TraumaType.Dismemberment,
                            severity,
                            source: category);

                        _wound.AmputateWoundable(targetChosen.Value, target.AsNullable(), user, crude: true);
                    }
                    break;
            }

        }

        // TODO: veins, would have been very lovely to integrate this into vascular system
        //if (RandomVeinsTraumaChance(woundable))
        //{
        //    traumaApplied = ApplyDamageToVeins(woundable.Veins!.ContainedEntities[0], severity * _veinsDamageMultipliers[woundable.WoundableSeverity]);
        //    _sawmill.Info(traumaApplied
        //        ? $"A new trauma (Raw Severity: {severity}) was created on target: {target} of type Vein damage"
        //        : $"Tried to create a trauma on target: {target}, but no trauma was applied. Type: Vein damage.");
        //}
    }


    #endregion
}
