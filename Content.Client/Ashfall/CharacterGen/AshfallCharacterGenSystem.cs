using Content.Shared.Ashfall.CharacterGen;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client.Ashfall.CharacterGen;

/// <summary>
///     Client-side system communicating with the server for candidate pool requests and selection.
/// </summary>
public sealed partial class AshfallCharacterGenSystem : EntitySystem
{
    [Dependency] private INetManager _netManager = default!;

    public List<AshfallCharacterCandidate> Candidates { get; private set; } = new();
    public int PoolRevision { get; private set; }
    public int SelectedIndex { get; private set; } = -1;
    public ProtoId<JobPrototype>? SelectedJob { get; private set; }
    public float CooldownSecondsRemaining { get; private set; }
    public int RemainingRefreshes { get; private set; } = -1;

    public AshfallCharacterCandidate? SelectedCandidate =>
        SelectedIndex >= 0 && SelectedIndex < Candidates.Count ? Candidates[SelectedIndex] : null;

    public Content.Shared.Preferences.HumanoidCharacterProfile? SelectedProfile => SelectedCandidate?.Profile;

    public event Action? PoolUpdated;

    public void HandlePoolResponse(MsgAshfallPoolResponse msg)
    {
        Candidates = msg.Candidates;
        PoolRevision = msg.PoolRevision;
        SelectedIndex = msg.SelectedIndex;
        SelectedJob = msg.SelectedJob;
        CooldownSecondsRemaining = msg.CooldownSecondsRemaining;
        RemainingRefreshes = msg.RemainingRefreshes;

        PoolUpdated?.Invoke();
    }

    /// <summary>
    ///     Requests the current candidate pool from the server, optionally requesting a reroll if allowed.
    /// </summary>
    public void RequestPool(bool refresh = false)
    {
        if (!_netManager.IsConnected)
            return;

        var msg = new MsgAshfallRequestPool { Refresh = refresh };
        _netManager.ClientSendMessage(msg);
    }

    /// <summary>
    ///     Selects a candidate by index.
    /// </summary>
    public void SelectCandidate(int index, ProtoId<JobPrototype> job)
    {
        if (index < 0 || index >= Candidates.Count)
            return;

        if (_netManager.IsConnected)
        {
            var msg = new MsgAshfallSelectCandidate
            {
                SelectedIndex = index,
                PoolRevision = PoolRevision,
                SelectedJob = job,
            };
            _netManager.ClientSendMessage(msg);
        }
    }
}
