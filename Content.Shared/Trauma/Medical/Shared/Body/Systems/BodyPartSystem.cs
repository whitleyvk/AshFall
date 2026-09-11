using System.Linq;
using Content.Medical.Common.Body;
using Content.Shared.Body;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Gibbing;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Medical.Shared.Body;

/// <summary>
/// System that handles bodypart logic and provides API for working with them.
/// </summary>
public sealed partial class BodyPartSystem : CommonBodyPartSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BodyCacheSystem _cache = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private EntityQuery<BodyPartComponent> _query = default!;
    [Dependency] private EntityQuery<ChildOrganComponent> _childQuery = default!;
    [Dependency] private EntityQuery<OrganComponent> _organQuery = default!;

    private static readonly SoundSpecifier GibSound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));
    private static readonly ProtoId<ReagentPrototype> BloodReagent = "Blood";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyPartComponent, OrganGotInsertedEvent>(OnPartInserted);
        SubscribeLocalEvent<BodyPartComponent, OrganGotRemovedEvent>(OnPartRemoved);
        SubscribeLocalEvent<BodyPartComponent, BeingGibbedEvent>(OnBeingGibbed);
    }

    private void OnPartInserted(Entity<BodyPartComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        // fresh part, no organs inside
        if (GetSeveredOrgansContainer(ent.AsNullable()) is not {} container)
            return;

        Entity<BodyComponent?> body = args.Target;
        var organs = new List<EntityUid>(container.ContainedEntities); // no CME
        foreach (var organ in organs)
        {
            if (_body.InsertOrgan(body, organ))
                continue;

            Log.Error($"Couldn't insert {ToPrettyString(organ)} from {ToPrettyString(ent)} back into {ToPrettyString(args.Target)} after being attached, ejecting it");
            if (!_container.Remove(organ, container))
                Log.Error($"Organ {ToPrettyString(organ)} got stuck inside of {ToPrettyString(ent)} after being inserted into {ToPrettyString(args.Target)}");
        }
    }

    private void OnPartRemoved(Entity<BodyPartComponent> ent, ref OrganGotRemovedEvent args)
    {
        // don't transfer parts if the body is being deleted
        // note that this will still transfer if the part is being deleted, so its organs will go away too
        if (TerminatingOrDeleted(args.Target) || _timing.ApplyingState)
            return;

        Entity<BodyComponent?> body = args.Target;
        if (TerminatingOrDeleted(ent))
        {
            // this part is being deleted so detach the children
            foreach (var organ in ent.Comp.Children.Values.ToArray())
            {
                _body.RemoveOrgan(body, organ);
            }
            return;
        }

        var container = EnsureSeveredOrgansContainer(ent);
        foreach (var (category, organ) in ent.Comp.Children.ToArray())
        {
            // slot has an organ so try to put it in the container
            if (!_container.Insert(organ, container))
            {
                // probably from failing to be removed, suspicious
                Log.Error($"Failed to store {ToPrettyString(ent)}'s {category} organ {ToPrettyString(organ)}!");
                continue;
            }
        }
    }

    private void OnBeingGibbed(Entity<BodyPartComponent> ent, ref BeingGibbedEvent args)
    {
        var organsToSpill = new List<EntityUid>();

        if (GetSeveredOrgansContainer(ent.AsNullable()) is {} container)
        {
            foreach (var organ in container.ContainedEntities)
            {
                organsToSpill.Add(organ);
            }
        }

        foreach (var (category, organ) in ent.Comp.Children.ToArray())
        {
            if (Deleted(organ) || organsToSpill.Contains(organ))
                continue;

            if (_organQuery.TryComp(organ, out var organComp) && organComp.Body is { } body)
            {
                _body.RemoveOrgan(body, organ);
            }
            organsToSpill.Add(organ);
        }

        if (organsToSpill.Count == 0)
            return;

        _audio.PlayPvs(GibSound, ent.Owner);

        var bloodSolution = new Solution();
        bloodSolution.AddReagent(BloodReagent, FixedPoint2.New(15));
        _puddle.TrySpillAt(ent.Owner, bloodSolution, out _, sound: false);

        var rand = new System.Random();
        foreach (var organ in organsToSpill)
        {
            args.Giblets.Add(organ);

            _transform.DropNextTo(organ, ent.Owner);

            var angle = rand.NextSingle() * MathF.PI * 2f;
            var dir = new System.Numerics.Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var dist = 1.0f + rand.NextSingle() * 1.5f;
            _throwing.TryThrow(organ, dir * dist, 1.5f, pushbackRatio: 0.2f);
        }
    }

    internal void OrganInserted(Entity<BodyPartComponent?> part, Entity<OrganComponent?> organ)
    {
        DebugTools.Assert(part.Owner != organ.Owner);
        if (!_query.Resolve(part, ref part.Comp) ||
            _body.GetCategory(organ) is not {} category ||
            !CanInsertOrgan(part, category)) // just incase
            return;

        part.Comp.Children[category] = organ;
        DirtyField(part, part.Comp, nameof(BodyPartComponent.Children));

        var ev = new OrganInsertedIntoPartEvent(organ, category);
        RaiseLocalEvent(part, ref ev);
    }

    internal void OrganRemoved(Entity<BodyPartComponent?> part, Entity<OrganComponent?> organ)
    {
        DebugTools.Assert(part.Owner != organ.Owner);
        if (!_query.Resolve(part, ref part.Comp, logMissing: false) ||
            _body.GetCategory(organ) is not {} category)
            return;

        part.Comp.Children.Remove(category);
        DirtyField(part, part.Comp, nameof(BodyPartComponent.Children));

        var ev = new OrganRemovedFromPartEvent(organ, category);
        RaiseLocalEvent(part, ref ev);
    }
}
