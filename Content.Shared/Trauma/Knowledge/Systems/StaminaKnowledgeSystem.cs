// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Events;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Systems;

public sealed partial class StaminaKnowledgeSystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    public static readonly EntProtoId StaminaKnowledge = "StaminaKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeStaminaDamageEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<KnowledgeHolderComponent, TookStaminaDamageEvent>(_knowledge.RelayEvent);
        SubscribeLocalEvent<StaminaKnowledgeComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        SubscribeLocalEvent<StaminaKnowledgeComponent, TookStaminaDamageEvent>(OnTookStaminaDamage);
    }

    private void OnBeforeStaminaDamage(Entity<StaminaKnowledgeComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        var level = _knowledge.GetLevel(ent.Owner);
        args.Value *= ent.Comp.Curve.GetCurve(level);
    }

    private void OnTookStaminaDamage(Entity<StaminaKnowledgeComponent> ent, ref TookStaminaDamageEvent args)
    {
        if (ent.Comp.DamageScale <= 0)
            return;

        var xp = Math.Min((int) args.Amount / ent.Comp.DamageScale, ent.Comp.MaxGain);
        var limit = 60;
        _knowledge.AddExperience(ent.Owner, args.Target, xp, limit);
    }
}
