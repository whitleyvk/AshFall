using Content.Shared.Ashfall.Carrying;

namespace Content.Client.Ashfall.Carrying;

/// <summary>
///     Client side of the player carrying feature; keeps the shared verb and prediction
///     handlers registered locally, behavior itself lives in the shared system.
/// </summary>
public sealed partial class CarryingSystem : SharedCarryingSystem
{
}
