using Content.Shared.Ashfall.CharacterGen;
using Content.Shared.Preferences;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Ashfall;
using Content.Shared.Ashfall.CharacterGen.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking;
using Robust.Server.Player;
using System.Linq;

namespace Content.Server.Ashfall.CharacterGen;

/// <summary>
///     Server-side manager maintaining candidate character pools per round for connected players.
/// </summary>
public sealed partial class AshfallCharacterPoolSystem : EntitySystem
{
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;
    [Dependency] private MarkingManager _markingManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private NamingSystem _namingSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private AshfallCharacterGenerator _generator = default!;
    private AshfallCharacterLoreGenerator _loreGenerator = default!;

    private static readonly ProtoId<CharacterGenConstraintsPrototype> HumanConstraints = "HumanDefaultConstraints";

    private readonly Dictionary<NetUserId, PlayerCandidatePool> _playerPools = new();

    private sealed class PlayerCandidatePool
    {
        public List<AshfallCharacterCandidate> Candidates { get; set; } = new();
        public int Revision { get; set; }
        public int SelectedIndex { get; set; } = -1;
        public ProtoId<JobPrototype>? SelectedJob { get; set; }
        public TimeSpan LastRefreshTime { get; set; } = TimeSpan.Zero;
        public int RefreshesUsed { get; set; } = 0;
    }

    public override void Initialize()
    {
        base.Initialize();

        _generator = new AshfallCharacterGenerator(_protoManager, _markingManager, _namingSystem);
        _loreGenerator = new AshfallCharacterLoreGenerator(_protoManager);

        _netManager.RegisterNetMessage<MsgAshfallRequestPool>(OnRequestPool);
        _netManager.RegisterNetMessage<MsgAshfallSelectCandidate>(OnSelectCandidate);
        _netManager.RegisterNetMessage<MsgAshfallPoolResponse>();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _playerPools.Clear();
    }

    private void OnRequestPool(MsgAshfallRequestPool msg)
    {
        var player = msg.MsgChannel.UserId;
        var pool = GetOrCreatePool(player);

        if (msg.Refresh)
        {
            var cooldown = TimeSpan.FromSeconds(_cfg.GetCVar(AshfallCCVars.CharacterPoolRefreshCooldown));
            var maxRefreshes = _cfg.GetCVar(AshfallCCVars.CharacterPoolMaxRefreshes);

            if (_timing.CurTime - pool.LastRefreshTime >= cooldown &&
                (maxRefreshes < 0 || pool.RefreshesUsed < maxRefreshes))
            {
                pool.Candidates = GeneratePoolForPlayer();
                pool.Revision++;
                pool.SelectedIndex = -1;
                pool.SelectedJob = null;
                pool.LastRefreshTime = _timing.CurTime;
                pool.RefreshesUsed++;

                if (_playerManager.TryGetSessionById(player, out var session))
                    EntityManager.System<GameTicker>().ToggleReady(session, false);
            }
        }

        SendPoolResponse(msg.MsgChannel, pool);
    }

    private void OnSelectCandidate(MsgAshfallSelectCandidate msg)
    {
        var player = msg.MsgChannel.UserId;
        var pool = GetOrCreatePool(player);

        if (msg.PoolRevision == pool.Revision &&
            msg.SelectedIndex >= 0 &&
            msg.SelectedIndex < pool.Candidates.Count &&
            _protoManager.TryIndex<JobPrototype>(msg.SelectedJob, out var job) &&
            job.SetPreference &&
            pool.Candidates[msg.SelectedIndex].CompatibleJobs.Contains(job.ID) &&
            IsJobAllowed(player, job.ID, pool.Candidates[msg.SelectedIndex].Profile))
        {
            pool.SelectedIndex = msg.SelectedIndex;
            pool.SelectedJob = job.ID;
        }

        SendPoolResponse(msg.MsgChannel, pool);
    }

    public HumanoidCharacterProfile? GetSelectedProfile(NetUserId userId)
    {
        if (_playerPools.TryGetValue(userId, out var pool) &&
            pool.SelectedIndex >= 0 &&
            pool.SelectedIndex < pool.Candidates.Count &&
            pool.SelectedJob is { } selectedJob)
        {
            var candidate = pool.Candidates[pool.SelectedIndex];
            var priorities = candidate.CompatibleJobs.ToDictionary(
                job => job,
                job => job == selectedJob ? JobPriority.High : JobPriority.Medium);

            return candidate.Profile
                .WithJobPriorities(priorities)
                .WithPreferenceUnavailable(PreferenceUnavailableMode.StayInLobby);
        }

        return null;
    }

    public bool TryGetSelectedProfile(NetUserId userId, out HumanoidCharacterProfile profile)
    {
        profile = GetSelectedProfile(userId)!;
        return profile != null;
    }

    public bool HasCompleteSelection(NetUserId userId)
    {
        return _playerPools.TryGetValue(userId, out var pool) &&
               pool.SelectedIndex >= 0 &&
               pool.SelectedIndex < pool.Candidates.Count &&
               pool.SelectedJob != null;
    }

    public bool IsJobCompatible(NetUserId userId, ProtoId<JobPrototype> job)
    {
        return _playerPools.TryGetValue(userId, out var pool) &&
               pool.SelectedIndex >= 0 &&
               pool.SelectedIndex < pool.Candidates.Count &&
               pool.Candidates[pool.SelectedIndex].CompatibleJobs.Contains(job);
    }

    private PlayerCandidatePool GetOrCreatePool(NetUserId userId)
    {
        if (!_playerPools.TryGetValue(userId, out var pool))
        {
            pool = new PlayerCandidatePool
            {
                Candidates = GeneratePoolForPlayer(),
                SelectedIndex = -1
            };
            _playerPools[userId] = pool;
        }

        return pool;
    }

    private static readonly ProtoId<CharacterGenConstraintsPrototype> ReptilianConstraints = "ReptilianDefaultConstraints";
    private static readonly ProtoId<CharacterGenConstraintsPrototype> MothConstraints = "MothDefaultConstraints";
    private static readonly ProtoId<CharacterGenConstraintsPrototype> ArachnidConstraints = "ArachnidDefaultConstraints";
    private static readonly ProtoId<CharacterGenConstraintsPrototype> VeiruConstraints = "VeiruDefaultConstraints";

    private List<AshfallCharacterCandidate> GeneratePoolForPlayer()
    {
        if (!_protoManager.TryIndex(HumanConstraints, out var humanConstraints))
            throw new InvalidOperationException($"Missing character generation constraints {HumanConstraints}.");

        var repConstraints = _protoManager.TryIndex(ReptilianConstraints, out var rC) ? rC : humanConstraints;
        var mothConstraints = _protoManager.TryIndex(MothConstraints, out var mC) ? mC : humanConstraints;
        var arachConstraints = _protoManager.TryIndex(ArachnidConstraints, out var aC) ? aC : humanConstraints;
        var veiruConstraints = _protoManager.TryIndex(VeiruConstraints, out var vC) ? vC : humanConstraints;

        var families = _loreGenerator.GetProfessionalFamilies().ToList();
        _random.Shuffle(families);

        var candidates = new List<AshfallCharacterCandidate>(8);

        // Left Column (Slots 0..3): 4 Guaranteed Humans
        for (var i = 0; i < 4; i++)
        {
            var (profile, culture, birthplace, morphology) = _generator.GenerateProfile(humanConstraints, _random);
            var family = i < families.Count ? families[i] : null;
            candidates.Add(_loreGenerator.GenerateCandidate(profile, _random, family, culture, birthplace, morphology));
        }

        // Right Column (Slots 4..7): 4 non-human candidates, one per species.
        var nonHumanSpeciesList = new List<CharacterGenConstraintsPrototype>
        {
            repConstraints,
            mothConstraints,
            arachConstraints,
            veiruConstraints
        };
        _random.Shuffle(nonHumanSpeciesList);

        for (var i = 0; i < 4; i++)
        {
            var constraints = nonHumanSpeciesList[i];
            var (profile, culture, birthplace, morphology) = _generator.GenerateProfile(constraints, _random);
            var family = i < families.Count ? families[i] : null;
            candidates.Add(_loreGenerator.GenerateCandidate(profile, _random, family, culture, birthplace, morphology));
        }

        return candidates;
    }

    private void SendPoolResponse(INetChannel channel, PlayerCandidatePool pool)
    {
        var cooldownSec = _cfg.GetCVar(AshfallCCVars.CharacterPoolRefreshCooldown);
        var maxRefreshes = _cfg.GetCVar(AshfallCCVars.CharacterPoolMaxRefreshes);

        var timeSinceLast = _timing.CurTime - pool.LastRefreshTime;
        var remainingCooldown = (float)Math.Max(0, cooldownSec - timeSinceLast.TotalSeconds);
        var remainingRefreshes = maxRefreshes < 0 ? -1 : Math.Max(0, maxRefreshes - pool.RefreshesUsed);

        var response = new MsgAshfallPoolResponse
        {
            Candidates = pool.Candidates,
            PoolRevision = pool.Revision,
            SelectedIndex = pool.SelectedIndex,
            SelectedJob = pool.SelectedJob?.Id,
            CooldownSecondsRemaining = remainingCooldown,
            RemainingRefreshes = remainingRefreshes
        };

        _netManager.ServerSendMessage(response, channel);
    }

    private bool IsJobAllowed(
        NetUserId userId,
        ProtoId<JobPrototype> job,
        HumanoidCharacterProfile profile)
    {
        if (!_playerManager.TryGetSessionById(userId, out var session))
            return false;

        var jobs = new List<ProtoId<JobPrototype>> { job };
        var ev = new IsRoleAllowedEvent(session, jobs, null, profile);
        RaiseLocalEvent(ref ev);
        return !ev.Cancelled;
    }
}
