// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Common.Knowledge;

namespace Content.Shared.Humanoid;

/// <summary>
/// Trauma - store the profile's knowledge settings
/// </summary>
public sealed partial class HumanoidProfileComponent
{
    [DataField]
    public KnowledgeProfile Knowledge = new();
}
