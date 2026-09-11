// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Medical.Shared.Traumas;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Rejuvenate;

namespace Content.Medical.Shared.Wounds;

/// <summary>
/// This class is responsible for managing wound healing in the shared game code.
/// It contains methods for halting all bleeding on a given entity.
/// </summary>
public partial class WoundSystem
{
    private List<Entity<WoundComponent>> _woundsToHeal = new(4);

    [SubscribeLocalEvent]
    private void OnRejuvenate(Entity<WoundableComponent> ent, ref RejuvenateEvent args)
    {
        _container.CleanContainer(ent.Comp.Wounds); // no more wounds
    }

    #region Public API

    /// <summary>
    /// Heals bleeding wounds on a body entity, starting with the most severely bleeding woundable
    /// and cascading any leftover healing to the next most severe bleeding woundable.
    /// </summary>
    /// <param name="body">The body entity to check for bleeding wounds</param>
    /// <param name="healAmount">The amount of healing to apply</param>
    /// <param name="healed">The total amount of bleeding that was healed</param>
    /// <param name="component">Optional body component if already resolved</param>
    /// <returns>True if any bleeding was healed, false otherwise</returns>
    public bool TryHealMostSevereBleedingWoundables(EntityUid body, float healAmount, out FixedPoint2 healed, BodyComponent? component = null)
    {
        healed = FixedPoint2.Zero;
        if (!Resolve(body, ref component) || healAmount == 0)
            return false;

        if (healAmount < 0)
        {
            Log.Error($"Negative healing amount {healAmount} passed to heal {ToPrettyString(body)}, Stack trace: {Environment.StackTrace}");
            return false;
        }

        // Collect all woundables and their total bleeding amounts
        var bleedingWoundables = new List<(EntityUid Woundable, FixedPoint2 BleedAmount)>();
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            var totalBleedAmount = FixedPoint2.Zero;
            var hasBleedingWounds = false;
            foreach (var wound in GetWoundableWounds(part.AsNullable()))
            {
                if (!_bleedQuery.TryComp(wound, out var bleeds) || !bleeds.IsBleeding)
                    continue;

                hasBleedingWounds = true;
                totalBleedAmount += bleeds.BleedingAmount;
            }

            if (hasBleedingWounds)
                bleedingWoundables.Add((part.Owner, totalBleedAmount));
        }

        // Sort woundables by bleeding amount (descending)
        var sortedWoundables = bleedingWoundables
            .OrderByDescending(x => x.BleedAmount)
            .Select(x => x.Woundable)
            .ToList();

        var remainingHealAmount = (FixedPoint2) healAmount;
        var anyHealed = false;

        // Apply healing to each woundable in order
        foreach (var woundable in sortedWoundables)
        {
            if (remainingHealAmount <= FixedPoint2.Zero)
                break;

            if (TryHealBleedingWounds(woundable, remainingHealAmount, out var modifiedBleed) && modifiedBleed > FixedPoint2.Zero)
            {
                anyHealed = true;
                healed += (float) modifiedBleed;
                remainingHealAmount -= modifiedBleed;
            }
        }

        return anyHealed;
    }

    public bool TryHealBleedingWounds(Entity<WoundableComponent?> part, FixedPoint2 bleedStopAbility, out FixedPoint2 modifiedBleed)
    {
        modifiedBleed = FixedPoint2.Zero;
        if (!_woundableQuery.Resolve(part, ref part.Comp) || bleedStopAbility <= FixedPoint2.Zero)
            return false;

        foreach (var wound in GetWoundableWounds(part))
        {
            if (!_bleedQuery.TryComp(wound, out var bleeds) || !bleeds.IsBleeding)
                continue;

            DirtyField(wound, bleeds, nameof(BleedInflicterComponent.BleedingAmountRaw));

            if (bleedStopAbility <= bleeds.BleedingAmount)
            {
                bleeds.BleedingAmountRaw = FixedPoint2.Max(FixedPoint2.Zero, bleeds.BleedingAmountRaw - bleedStopAbility);
                modifiedBleed += bleedStopAbility;
                bleedStopAbility = FixedPoint2.Zero;
                if (bleeds.BleedingAmountRaw <= FixedPoint2.Zero)
                {
                    bleeds.IsBleeding = false;
                    bleeds.Scaling = 0;
                    DirtyFields(wound, bleeds, null, nameof(BleedInflicterComponent.IsBleeding), nameof(BleedInflicterComponent.Scaling));
                }
                break; // cant heal anymore
            }

            var healedThisWound = (FixedPoint2) bleeds.BleedingAmount;
            bleedStopAbility -= healedThisWound;
            modifiedBleed += healedThisWound;
            bleeds.BleedingAmountRaw = 0;
            bleeds.IsBleeding = false;
            bleeds.Scaling = 0;
            DirtyFields(wound, bleeds, null, nameof(BleedInflicterComponent.IsBleeding), nameof(BleedInflicterComponent.Scaling));
        }

        return modifiedBleed > FixedPoint2.Zero;
    }

    public bool TryHealWounds(Entity<WoundableComponent?> part,
        FixedPoint2 healAmount,
        out FixedPoint2 healed,
        [ForbidLiteral] ProtoId<DamageGroupPrototype>? damageGroup = null,
        bool ignoreBlockers = false)
    {
        healed = FixedPoint2.Zero;
        if (!_woundableQuery.Resolve(part, ref part.Comp) || healAmount == FixedPoint2.Zero)
            return false;

        if (healAmount < FixedPoint2.Zero)
        {
            Log.Error($"Negative healing amount {healAmount} passed to heal {ToPrettyString(part)} of group {damageGroup}, Stack trace: {Environment.StackTrace}");
            return false;
        }

        _woundsToHeal.Clear();
        foreach (var wound in part.Comp.Wounds.ContainedEntities)
        {
            var woundComp = _query.Comp(wound);
            if (damageGroup != null && damageGroup != woundComp.DamageGroup ||
                !CanHealWound((wound, woundComp), ignoreBlockers))
                continue;

            _woundsToHeal.Add((wound, woundComp));
        }

        if (_woundsToHeal.Count == 0)
            return false;

        var heal = healAmount / _woundsToHeal.Count;
        foreach (var wound in _woundsToHeal)
        {
            healed += -ChangeWoundSeverity(wound, -heal);
        }

        UpdateWoundableIntegrity(part);
        CheckWoundableSeverityThresholds(part);

        return healed > 0;
    }

    public bool TryHealWoundsOfType(Entity<WoundableComponent?> part,
        FixedPoint2 healAmount,
        [ForbidLiteral] ProtoId<DamageTypePrototype> damageType,
        out FixedPoint2 healed,
        bool ignoreBlockers = false)
    {
        healed = 0;
        if (!_woundableQuery.Resolve(part, ref part.Comp) || healAmount == FixedPoint2.Zero)
            return false;

        if (healAmount < FixedPoint2.Zero)
        {
            Log.Error($"Negative healing amount {healAmount} passed to heal {ToPrettyString(part)} of type {damageType}, Stack trace: {Environment.StackTrace}");
            return false;
        }

        _woundsToHeal.Clear();
        foreach (var wound in part.Comp.Wounds.ContainedEntities)
        {
            var woundComp = _query.Comp(wound);
            if (damageType != woundComp.DamageType ||
                !CanHealWound((wound, woundComp), ignoreBlockers))
                continue;

            _woundsToHeal.Add((wound, woundComp));
        }

        if (_woundsToHeal.Count == 0)
            return false;

        var heal = -healAmount / _woundsToHeal.Count;
        foreach (var wound in _woundsToHeal)
        {
            healed += -ChangeWoundSeverity(wound, heal);
        }

        UpdateWoundableIntegrity(part);
        CheckWoundableSeverityThresholds(part);

        return healed > 0;
    }

    public bool TryHealWounds(Entity<WoundableComponent?> part,
        DamageSpecifier damage,
        out Dictionary<string, FixedPoint2> healed,
        bool ignoreMultipliers = false)
    {
        healed = [];
        if (!_woundableQuery.Resolve(part, ref part.Comp))
            return false;

        foreach (var (type, amount) in damage.DamageDict)
        {
            if (TryHealWoundsOfType(part, amount, type, out var typeHealed))
            {
                healed.Add(type, typeHealed);
                continue;
            }
        }

        return healed.Any();
    }

    public bool TryGetWoundableWithMostDamage(
        EntityUid body,
        [NotNullWhen(true)] out Entity<WoundableComponent>? woundable,
        [ForbidLiteral] ProtoId<DamageGroupPrototype>? damageGroup = null,
        bool healable = false)
    {
        var biggestDamage = FixedPoint2.Zero;

        woundable = null;
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            var woundableDamage = GetWoundableSeverityPoint(part.AsNullable(), damageGroup, healable);
            if (woundableDamage <= biggestDamage)
                continue;

            biggestDamage = woundableDamage;
            woundable = part;
        }

        return woundable != null;
    }

    public bool HasDamageOfGroup(
        EntityUid woundable,
        [ForbidLiteral] ProtoId<DamageGroupPrototype> damageGroup)
    {
        var wounds = GetWoundableWounds(woundable);
        return wounds.Any(wound => wound.Comp.DamageGroup == damageGroup);
    }

    public bool CanHealWound(Entity<WoundComponent> wound, bool ignoreBlockers = false)
    {
        if (!ignoreBlockers && !wound.Comp.CanBeHealed)
            return false;

        var holdingWoundable = wound.Comp.HoldingWoundable;
        if (!_woundableQuery.TryComp(holdingWoundable, out var woundableComp))
            return false;

        var ev = new WoundHealAttemptOnWoundableEvent(wound);
        RaiseLocalEvent(holdingWoundable, ref ev);

        if (ev.Cancelled)
            return false;

        var ev1 = new WoundHealAttemptEvent((holdingWoundable, woundableComp), ignoreBlockers);
        RaiseLocalEvent(wound, ref ev1);

        return !ev1.Cancelled;
    }

    /// <summary>
    /// Method to get all wounds of some entity
    /// </summary>
    public bool TryGetBodyWounds(EntityUid body, out List<Entity<WoundComponent>> wounds)
    {
        wounds = new List<Entity<WoundComponent>>();
        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            AddWounds(part, wounds);
        }

        return wounds.Count > 0;
    }

    private void AddWounds(WoundableComponent part, List<Entity<WoundComponent>> wounds)
    {
        foreach (var wound in part.Wounds.ContainedEntities)
        {
            if (_query.TryComp(wound, out var comp))
                wounds.Add((wound, comp));
        }
    }

    /// <summary>
    /// Method to get all wounded parts of entity
    /// </summary>
    public bool TryGetBodyWoundedParts(EntityUid body, out List<Entity<WoundableComponent>> woundables)
    {
        woundables = [];

        foreach (var part in _body.GetOrgans<WoundableComponent>(body))
        {
            if (part.Comp.Wounds.Count > 0)
                woundables.Add(part);
        }

        return woundables.Count > 0;
    }

    /// <summary>
    /// Method to heal all wounds on entity by specific healing amount.
    /// </summary>
    public bool TryHealWoundsOnOwner(EntityUid body, DamageSpecifier healing, bool ignoreBlockers = false)
    {
        if (!TryGetBodyWoundedParts(body, out var woundables) || !TryGetBodyWounds(body, out var wounds))
            return false;

        var woundCountByType = new Dictionary<string, int>();
        foreach (var w in wounds)
        {
            if (CanHealWound(w, ignoreBlockers))
            {
                var type = w.Comp.DamageType;
                woundCountByType[type] = woundCountByType.GetValueOrDefault(type) + 1;
            }
        }

        var healed = false;
        foreach (var w in wounds)
        {
            if (!CanHealWound(w, ignoreBlockers))
                continue;

            var type = w.Comp.DamageType;
            if (!healing.DamageDict.TryGetValue(type, out var totalAmount))
                continue;

            var count = woundCountByType.GetValueOrDefault(type);
            if (count == 0)
                continue;

            var healPerWound = totalAmount / count;
            if (healPerWound <= FixedPoint2.Zero)
                continue;

            var changed = ChangeWoundSeverity(w, -healPerWound);
            if (changed != FixedPoint2.Zero)
                healed = true;
        }

        if (healed)
        {
            foreach (var woundable in woundables)
            {
                UpdateWoundableIntegrity(woundable.AsNullable());
                CheckWoundableSeverityThresholds(woundable.AsNullable());
            }
        }

        return healed;
    }

    #endregion
}
