// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Examine;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Weapons.Classes;

/// <summary>
/// Handles examining weapon's classes and their effects on combat.
/// </summary>
public sealed partial class WeaponClassSystem : EntitySystem
{
    [Dependency] private readonly SharedKnowledgeSystem _knowledge = default!;
    [Dependency] private readonly EntityQuery<WeaponClassComponent> _query = default!;

    public static readonly ProtoId<WeaponClassPrototype> Unarmed = "Unarmed";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeaponClassComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<WeaponClassComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<WeaponClassComponent, GetRecoilModifiersEvent>(OnGetRecoilModifiers);
    }

    private void OnExamined(Entity<WeaponClassComponent> ent, ref ExaminedEvent args)
    {
        if (!_knowledge.SkillsEnabled || !args.IsInDetailsRange || !ent.Comp.Examinable)
            return;

        var name = Loc.GetString(ProtoMan.Index(ent.Comp.Class).Name);
        args.PushMarkup(Loc.GetString("weapon-class-examined", ("name", name)));
    }

    private void OnGetMeleeDamage(Entity<WeaponClassComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (!_knowledge.SkillsEnabled)
            return;

        var proto = ProtoMan.Index(ent.Comp.Class);
        var level = GetSkillLevel(proto, args.User);
        args.Damage *= proto.MeleeDamage.GetCurve(level);
    }

    private void OnGetRecoilModifiers(Entity<WeaponClassComponent> ent, ref GetRecoilModifiersEvent args)
    {
        if (args.User == ent.Owner || !_knowledge.SkillsEnabled)
            return;

        var proto = ProtoMan.Index(ent.Comp.Class);
        var level = GetSkillLevel(proto, args.User);
        args.Modifier /= proto.AimSpeed.GetCurve(level);
    }

    /// <summary>
    /// Whether an attack counts as unarmed, either bare handed or using an unarmed class weapon like gloves.
    /// </summary>
    public bool IsUnarmed(EntityUid user, EntityUid weapon)
        => user == weapon || _query.TryComp(weapon, out var comp) && comp.Class == Unarmed;

    public int GetSkillLevel(Entity<WeaponClassComponent> ent, EntityUid user)
        => GetSkillLevel(ProtoMan.Index(ent.Comp.Class), user);

    public int GetSkillLevel(WeaponClassPrototype proto, EntityUid user)
        => _knowledge.GetKnowledgeLevel(user, proto.Knowledge);
}
