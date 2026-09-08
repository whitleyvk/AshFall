// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Knowledge.Components;

/// <summary>
/// Grants a set of knowledge units to an entity on MapInit.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class KnowledgeGrantComponent : Component
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Skills = new();
}
