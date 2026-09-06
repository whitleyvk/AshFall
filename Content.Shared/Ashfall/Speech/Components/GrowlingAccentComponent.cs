using Content.Shared.Ashfall.Speech.EntitySystems;
using Content.Shared.Speech.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Ashfall.Speech.Components;

/// <summary>
/// Ррр!
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(GrowlingAccentSystem))]
public sealed partial class GrowlingAccentComponent : BaseAccentComponent;
