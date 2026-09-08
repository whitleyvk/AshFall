// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared.Trauma.Prediction;

/// <summary>
/// Component that marks an entity as having predicted physics during flight.
/// Controls predicting thrown item physics at the right time.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PredictedThrownItemComponent : Component;
