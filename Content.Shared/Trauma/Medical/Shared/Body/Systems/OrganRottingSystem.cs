// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Medical.Shared.Body;

/// <summary>
/// Prevents perishable organs/bodyparts from rotting inside a living mob.
/// </summary>
public sealed partial class OrganRottingSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganComponent, IsRottingEvent>(OnIsRotting);
    }

    private void OnIsRotting(Entity<OrganComponent> ent, ref IsRottingEvent args)
    {
        if (ent.Comp.Body is { } body &&
            TryComp<MobStateComponent>(body, out var mobState) &&
            !_mobState.IsDead(body, mobState))
        {
            args.Handled = true;
        }
    }
}
