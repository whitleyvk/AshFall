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

        // Roll for each possible defect
        var toRemove = new List<Type>();
        foreach (var comp in AllComps(ent.Owner))
        {
            if (comp is not DefectComponent defect)
                continue;

            // 1.0 probability means the defect is guaranteed
            if (defect.Prob >= 1.0f)
                continue;

            if (!_random.Prob(defect.Prob))
                toRemove.Add(comp.GetType());
        }

        foreach (var type in toRemove)
        {
            RemComp(ent.Owner, type);
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
