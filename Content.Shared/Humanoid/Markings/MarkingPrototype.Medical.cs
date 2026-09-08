// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Humanoid.Markings;

public sealed partial class MarkingPrototype
{
    [DataField]
    public List<string> ChildMarkingsSuffix = new();
}
