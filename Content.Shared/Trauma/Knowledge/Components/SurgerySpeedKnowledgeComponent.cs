// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Scales surgery speed based on surgery skill curve.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SurgeryKnowledgeSystem))]
public sealed partial class SurgerySpeedKnowledgeComponent : Component
{
    /// <summary>
    /// The skill curve to multiply surgery speed by.
    /// </summary>
    [DataField(required: true)]
    public SkillCurve Curve = default!;
}
