using Content.Shared.Ashfall.Weapons.Ranged.Defects.Components;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Ashfall.Weapons.Ranged.Defects;

/// <summary>
/// Handles rolling defect probabilities at MapInit for entities with RandomDefectsComponent.
/// Strips failed defects, renames the entity with a condition prefix, and appends
/// surviving defect descriptions to the examine text.
/// </summary>
public sealed partial class DefectSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomDefectsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomDefectsComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        // Collect candidate optional defects
        var optionalDefects = new List<DefectComponent>();
        foreach (var comp in AllComps(ent.Owner))
        {
            if (comp is DefectComponent defect && defect.Prob < 1.0f)
            {
                optionalDefects.Add(defect);
            }
        }

        // Determine target number of defects:
        // Highest probability for worst (rusty, 3+ defects), descending towards best (like-new, 0 defects)
        // Distribution: Rusty 50%, Worn 30%, Used 15%, Like-new 5%
        var roll = _random.NextFloat();
        var targetCount = roll switch
        {
            < 0.50f => 3,
            < 0.80f => 2,
            < 0.95f => 1,
            _ => 0,
        };

        if (targetCount < optionalDefects.Count)
        {
            _random.Shuffle(optionalDefects);
            var removeCount = optionalDefects.Count - targetCount;
            for (var i = 0; i < removeCount; i++)
            {
                RemComp(ent.Owner, optionalDefects[i].GetType());
            }
        }

        // The condition tier only bounds how many defect slots the gun gets; every surviving
        // optional defect still rolls its own spawn probability, so per-defect prob tuning
        // (rare backfire vs common jam) stays meaningful within every tier.
        var surviving = new List<DefectComponent>();
        foreach (var comp in AllComps(ent.Owner))
        {
            if (comp is DefectComponent defect && defect.Prob < 1.0f)
                surviving.Add(defect);
        }

        foreach (var defect in surviving)
        {
            if (!_random.Prob(defect.Prob))
                RemComp(ent.Owner, defect.GetType());
        }

        // Collect surviving defect labels
        var labels = new List<string>();
        foreach (var comp in AllComps(ent.Owner))
        {
            if (comp is DefectComponent defect && !string.IsNullOrEmpty(defect.DefectLabel))
            {
                var label = Loc.GetString(defect.DefectLabel);
                if (!string.IsNullOrWhiteSpace(label))
                    labels.Add(label);
            }
        }

        var prefixKey = labels.Count switch
        {
            0 => "defect-prefix-like-new",
            1 => "defect-prefix-used",
            2 => "defect-prefix-worn",
            _ => "defect-prefix-rusty"
        };

        var prefix = Loc.GetString(prefixKey);
        var meta = MetaData(ent.Owner);
        _metaData.SetEntityName(ent.Owner, $"{prefix} {meta.EntityName}", meta);

        if (labels.Count > 0)
        {
            var header = Loc.GetString("defect-description-header");
            var defectLine = $"\n\n{header} {string.Join(", ", labels)}.";
            _metaData.SetEntityDescription(ent.Owner, meta.EntityDescription + defectLine, meta);
        }
    }
}
