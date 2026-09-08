// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Projectiles;
using Robust.Client.Physics;

namespace Content.Client.Trauma.Prediction;

/// <summary>
/// Marks projectiles as participating in client-side physics prediction.
/// </summary>
public sealed partial class PredictedProjectileSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProjectileComponent, UpdateIsPredictedEvent>(OnUpdateIsPredicted);
    }

    private void OnUpdateIsPredicted(Entity<ProjectileComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }

}
