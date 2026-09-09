using System.Numerics;
using Content.Server.Decals;
using Content.Shared.Ashfall.Footprints;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Decals;
using Content.Shared.Fluids.Components;
using Content.Trauma.Common.Movement;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Ashfall.Footprints;

public sealed partial class FootprintSystem : EntitySystem
{
    [Dependency] private DecalSystem _decalSystem = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    private readonly List<(EntityUid Grid, DecalIndex Decal, TimeSpan ExpireTime)> _decayingDecals = new();

    private static readonly Color AshColor = Color.FromHex("#3D3C3B");
    private static readonly Color BloodColor = Color.FromHex("#7D0808");
    private static readonly Color OilColor = Color.FromHex("#1C1B1A");
    private static readonly Color WaterColor = Color.FromHex("#9EC4D5");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FootprintComponent, FootStepEvent>(OnFootStep);
    }

    private void OnFootStep(EntityUid uid, FootprintComponent component, ref FootStepEvent args)
    {
        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid)
            return;

        // Check if walking over a puddle
        CheckPuddles(uid, xform.Coordinates, component);

        if (component.StepCount <= 0 || component.PrintColor == null)
            return;

        // Calculate alternating foot placement
        var forward = args.WorldAngle.ToWorldVec();
        var right = new Vector2(forward.Y, -forward.X);
        var side = component.RightFoot ? 1f : -1f;
        component.RightFoot = !component.RightFoot;

        var offset = right * (component.FootOffset * side);
        var footCoords = xform.Coordinates.Offset(offset).Offset(new Vector2(-0.5f, -0.5f));

        // Calculate fade based on remaining steps
        var alpha = (float) component.StepCount / component.MaxSteps;
        var decalColor = component.PrintColor.Value.WithAlpha(Math.Clamp(alpha, 0.25f, 0.95f));
        var decalAngle = args.WorldAngle - Math.PI;

        if (_decalSystem.TryAddDecal(
            component.DecalId,
            footCoords,
            out var decalIndex,
            decalColor,
            decalAngle,
            zIndex: -1,
            cleanable: true))
        {
            _decayingDecals.Add((gridUid, decalIndex, _timing.CurTime + component.Lifetime));
        }

        component.StepCount--;
        Dirty(uid, component);
    }

    private void CheckPuddles(EntityUid uid, EntityCoordinates coordinates, FootprintComponent component)
    {
        var entities = _lookup.GetEntitiesIntersecting(coordinates);
        foreach (var ent in entities)
        {
            if (!TryComp<PuddleComponent>(ent, out var puddle))
                continue;

            // Only form footprints if the puddle contains enough solution (matches upstream #4762)
            if (!_solutionContainer.TryGetSolution(ent, puddle.SolutionName, out _, out var solution) || solution.Volume < 5)
                continue;

            var color = DeterminePuddleColor(solution);
            component.PrintColor = color;
            component.StepCount = component.MaxSteps;
            Dirty(uid, component);
            break;
        }
    }

    private Color DeterminePuddleColor(Content.Shared.Chemistry.Components.Solution solution)
    {
        foreach (var quantity in solution.Contents)
        {
            var id = quantity.Reagent.Prototype.Id.ToLowerInvariant();
            if (id.Contains("blood"))
                return BloodColor;
            if (id.Contains("oil") || id.Contains("weldingfuel") || id.Contains("hydrocarbon"))
                return OilColor;
            if (id.Contains("ash") || id.Contains("soot") || id.Contains("carbon"))
                return AshColor;
        }

        return solution.GetColor(ProtoMan);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_decayingDecals.Count == 0)
            return;

        var curTime = _timing.CurTime;
        for (var i = _decayingDecals.Count - 1; i >= 0; i--)
        {
            var (grid, decal, expire) = _decayingDecals[i];
            if (!Exists(grid))
            {
                _decayingDecals.RemoveAt(i);
                continue;
            }

            if (curTime >= expire)
            {
                _decalSystem.RemoveDecal(grid, decal);
                _decayingDecals.RemoveAt(i);
            }
        }
    }
}
