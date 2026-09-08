using Content.Medical.Shared.Surgery;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Shared.Knowledge.Components;

namespace Content.Trauma.Shared.Knowledge.Systems;

/// <summary>
/// Handles surgery skill scaling surgery step duration and granting experience.
/// </summary>
public sealed partial class SurgeryKnowledgeSystem : EntitySystem
{
    [Dependency] private SharedKnowledgeSystem _knowledge = default!;

    public static readonly EntProtoId SurgeryKnowledge = "SurgeryKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, BeforeSurgeryStepDurationEvent>(OnHolderSurgeryDuration);
        SubscribeLocalEvent<SurgerySpeedKnowledgeComponent, BeforeSurgeryStepDurationEvent>(OnBeforeSurgeryStepDuration);
        SubscribeLocalEvent<SurgeryStepEvent>(OnSurgeryStep);
    }

    private void OnHolderSurgeryDuration(Entity<KnowledgeHolderComponent> ent, ref BeforeSurgeryStepDurationEvent args)
    {
        _knowledge.RelayEvent(ent, ref args);

        if (_knowledge.GetContainer(ent.Owner) is not { } container ||
            _knowledge.GetKnowledge(container, SurgeryKnowledge) == null)
        {
            args.Speed *= 0.15f;
        }
    }

    private void OnBeforeSurgeryStepDuration(Entity<SurgerySpeedKnowledgeComponent> ent, ref BeforeSurgeryStepDurationEvent args)
    {
        var level = _knowledge.GetLevel(ent.Owner);
        args.Speed *= ent.Comp.Curve.GetCurve(level);
    }

    private void OnSurgeryStep(ref SurgeryStepEvent args)
    {
        _knowledge.AddExperience(args.User, SurgeryKnowledge, 1);
    }
}
