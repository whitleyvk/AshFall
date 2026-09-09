// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Medical.Common.Targeting;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Medical.Shared.Damage;

public sealed partial class PartDamageSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<OrganComponent, DamageDealtEvent>(OnDamageDealt, after: new[] { typeof(DamageableSystem) });
        SubscribeLocalEvent<DamageableComponent, OrganRemovedFromEvent>(OnOrganRemoved);
        SubscribeLocalEvent<DamageableComponent, OrganInsertedIntoEvent>(OnOrganInserted);
    }

    private void OnMapInit(Entity<DamageableComponent> ent, ref MapInitEvent args)
    {
        if (!_body.HasExternalOrgans(ent.Owner))
            return;

        var damage = _damage.GetAllDamage(ent.AsNullable());
        if (damage.GetTotal() == 0)
            return;

        // update e.g. unidentified corpse part damage when they spawn
        _damage.ApplyDamageToBodyParts(ent, damage, origin: null,
            ignoreResistances: true, interruptsDoAfters: false, partMultiplier: 1f, targetPart: TargetBodyPart.Chest, canMiss: false);
    }

    private void OnDamageDealt(Entity<OrganComponent> ent, ref DamageDealtEvent args)
    {
        if (ent.Comp.Body is not { } body)
            return;

        _damage.UpdateParentDamageFromBodyParts(
            body,
            args.Damage,
            args.InterruptsDoAfters,
            args.Origin);
    }

    private void OnOrganRemoved(Entity<DamageableComponent> ent, ref OrganRemovedFromEvent args)
    {
        _damage.UpdateParentDamageFromBodyParts(ent.Owner);
    }

    private void OnOrganInserted(Entity<DamageableComponent> ent, ref OrganInsertedIntoEvent args)
    {
        _damage.UpdateParentDamageFromBodyParts(ent.Owner);
    }
}
