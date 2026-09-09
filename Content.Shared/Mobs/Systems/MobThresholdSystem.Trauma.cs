// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared.Mobs.Systems;

public sealed partial class MobThresholdSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private EntityQuery<BodyComponent> _bodyQuery = default!;
    [Dependency] private EntityQuery<DamageableComponent> _damageQuery = default!;

    /// <summary>
    /// Returns damage on vital body parts for body-enabled mobs, or total damage for other entities.
    /// </summary>
    public FixedPoint2 CheckVitalDamage(Entity<DamageableComponent?> ent)
    {
        if (!_damageQuery.Resolve(ent, ref ent.Comp, false))
            return FixedPoint2.Zero;

        if (!_bodyQuery.HasComp(ent))
            return _damageable.GetTotalDamage(ent);

        var result = FixedPoint2.Zero;
        foreach (var part in _body.GetVitalParts(ent))
        {
            result += _damageable.GetTotalDamage(part);
        }

        return result;
    }
}
