using Content.IntegrationTests.Fixtures;
using Content.Server.Ashfall.CharacterGen;
using Content.Shared.Ashfall.CharacterGen;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.IntegrationTests.Tests.Ashfall;

/// <summary>
///     Wake-up priority slots: pin candidate + job pairs, verify they survive pool rerolls,
///     verify moving a pin between slots, and that the confirmed selection keeps the pinned
///     candidate's identity instead of a pool index.
/// </summary>
[TestFixture]
public sealed class AshfallPrioritySlotTests : GameTest
{
    public override PoolSettings PoolSettings => new() { InLobby = true };

    [Test]
    public async Task PinSurvivesRerollAndConfirmUsesPinnedIdentity()
    {
        var pair = Pair;
        var server = pair.Server;
        var user = pair.Client.User!.Value;

        await server.WaitPost(() =>
            server.Resolve<IConfigurationManager>().SetCVar(Content.Shared.Ashfall.AshfallCCVars.CharacterPoolRefreshCooldown, 0f));

        var revision = -1;
        var firstId = Guid.Empty;
        var firstName = "";
        ProtoId<JobPrototype> job = default!;

        await server.WaitAssertion(() =>
        {
            var sys = server.EntMan.System<AshfallCharacterPoolSystem>();
            var pool = sys.GetOrCreatePool(user);
            var first = pool.Candidates[0];
            firstId = first.CandidateId;
            firstName = first.Profile.Name;
            Assert.That(first.CompatibleJobs, Is.Not.Empty);
            job = first.CompatibleJobs[0];
            revision = pool.Revision;
        });
        await pair.RunTicksSync(5);

        // Pin candidate 0 + one concrete compatible job into slot 0 (priority 1).
        await server.WaitAssertion(() =>
        {
            var sys = server.EntMan.System<AshfallCharacterPoolSystem>();
            Assert.That(sys.TryPin(user, 0, firstId, job, revision), Is.True);

            // Guard rails: unknown candidate and stale revision are rejected.
            Assert.That(sys.TryPin(user, 1, Guid.NewGuid(), job, revision), Is.False);
            Assert.That(sys.TryPin(user, 1, firstId, job, revision + 100), Is.False);
            Assert.That(server.EntMan.System<AshfallCharacterPoolSystem>().GetOrCreatePool(user).PrioritySlots[1], Is.Null);

            // Re-pinning the same candidate into another slot moves the combination there
            // (a candidate may still never occupy two slots at once).
            Assert.That(sys.TryPin(user, 2, firstId, job, revision), Is.True);
            Assert.That(sys.GetOrCreatePool(user).PrioritySlots[0], Is.Null);
            Assert.That(sys.GetOrCreatePool(user).PrioritySlots[2], Is.Not.Null);
            Assert.That(sys.GetOrCreatePool(user).ConfirmedPriorityIndex, Is.EqualTo(2));

            Assert.That(sys.TryPin(user, 0, firstId, job, revision), Is.True);
            Assert.That(sys.GetOrCreatePool(user).PrioritySlots[2], Is.Null);
            Assert.That(sys.GetOrCreatePool(user).ConfirmedPriorityIndex, Is.EqualTo(0));
        });
        await pair.RunTicksSync(5);

        // Reroll via a real client refresh message: the regular pool is replaced, the pin survives.
        await pair.Client.WaitPost(() =>
            pair.Client.Resolve<INetManager>().ClientSendMessage(new MsgAshfallRequestPool { Refresh = true }));
        await PoolManager.WaitUntil(server, () =>
            server.EntMan.System<AshfallCharacterPoolSystem>().GetOrCreatePool(user).Revision > revision, maxTicks: 60);

        await server.WaitAssertion(() =>
        {
            var sys = server.EntMan.System<AshfallCharacterPoolSystem>();
            var pool = sys.GetOrCreatePool(user);

            Assert.That(pool.Candidates[0].CandidateId, Is.Not.EqualTo(firstId),
                "a rerolled pool mints fresh ids; the pinned person is not regenerated into it");

            var pinned = pool.PrioritySlots[0];
            Assert.That(pinned, Is.Not.Null, "pinned slot must survive a reroll");
            Assert.That(pinned!.Candidate.CandidateId, Is.EqualTo(firstId));
            Assert.That(pinned.Candidate.Profile.Name, Is.EqualTo(firstName));
            Assert.That(pinned.Job, Is.EqualTo(job));

            // Pinning is the confirmation: the first occupied slot resolves immediately and
            // keeps the pinned identity, not an index into the (now different) regular pool.
            Assert.That(pool.ConfirmedPriorityIndex, Is.EqualTo(0));
            var profile = sys.GetSelectedProfile(user);
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile!.Name, Is.EqualTo(firstName));
            Assert.That(profile.JobPriorities[job], Is.EqualTo(JobPriority.High));
            Assert.That(sys.HasCompleteSelection(user), Is.True);

            // Replacing an occupied slot unpins the old combination and takes the new pair.
            var replacement = pool.Candidates[0];
            Assert.That(
                sys.TryPin(user, 0, replacement.CandidateId, replacement.CompatibleJobs[0], pool.Revision),
                Is.True);
            Assert.That(pool.PrioritySlots[0]!.Candidate.CandidateId, Is.EqualTo(replacement.CandidateId));
            Assert.That(sys.GetSelectedProfile(user)!.Name, Is.EqualTo(replacement.Profile.Name),
                "the confirmed slot now resolves to the replacement pair, not the old pinned person");

            // Clearing leaves other slots untouched and drops the confirmed selection.
            Assert.That(sys.ClearPrioritySlot(user, 0), Is.True);
            Assert.That(pool.PrioritySlots[0], Is.Null);
            Assert.That(sys.HasCompleteSelection(user), Is.False);
        });
    }

    [Test]
    public async Task PoolOrdersHumansLeftNonHumansRight()
    {
        var pair = Pair;
        var server = pair.Server;
        var user = pair.Client.User!.Value;

        await server.WaitAssertion(() =>
        {
            var pool = server.EntMan.System<AshfallCharacterPoolSystem>().GetOrCreatePool(user);
            Assert.That(pool.Candidates, Has.Count.EqualTo(8));

            // Left column (indexes 0..3): humans only.
            for (var i = 0; i < 4; i++)
                Assert.That(pool.Candidates[i].Profile.Species, Is.EqualTo("Human"),
                    $"left column slot {i} must be a human");

            // Right column (indexes 4..7): non-humans, one candidate per species.
            var right = pool.Candidates.Skip(4).Select(c => c.Profile.Species).ToList();
            Assert.That(right, Is.All.Not.EqualTo("Human"), "right column must be non-humans");
            Assert.That(right, Is.Unique, "each non-human species appears exactly once");
        });
        await pair.RunTicksSync(5);
    }

    [Test]
    public async Task MovePrioritySlotSwapsAndReorders()
    {
        var pair = Pair;
        var server = pair.Server;
        var user = pair.Client.User!.Value;

        Guid firstId = default, secondId = default;
        ProtoId<JobPrototype> firstJob = default!, secondJob = default!;

        await server.WaitAssertion(() =>
        {
            var sys = server.EntMan.System<AshfallCharacterPoolSystem>();
            var pool = sys.GetOrCreatePool(user);
            firstId = pool.Candidates[0].CandidateId;
            secondId = pool.Candidates[1].CandidateId;
            firstJob = pool.Candidates[0].CompatibleJobs[0];
            secondJob = pool.Candidates[1].CompatibleJobs[0];

            Assert.That(sys.TryPin(user, 0, firstId, firstJob, pool.Revision), Is.True);
            Assert.That(sys.TryPin(user, 1, secondId, secondJob, pool.Revision), Is.True);
        });

        await server.WaitAssertion(() =>
        {
            var sys = server.EntMan.System<AshfallCharacterPoolSystem>();
            var pool = sys.GetOrCreatePool(user);

            // Occupied target: the two pins swap places, both survive.
            Assert.That(sys.MovePrioritySlot(user, 0, 1), Is.True);
            Assert.That(pool.PrioritySlots[0]!.Candidate.CandidateId, Is.EqualTo(secondId));
            Assert.That(pool.PrioritySlots[1]!.Candidate.CandidateId, Is.EqualTo(firstId));
            Assert.That(pool.ConfirmedPriorityIndex, Is.EqualTo(0));

            // Empty target: the pin moves, the source slot is freed.
            Assert.That(sys.MovePrioritySlot(user, 1, 4), Is.True);
            Assert.That(pool.PrioritySlots[1], Is.Null);
            Assert.That(pool.PrioritySlots[4]!.Candidate.CandidateId, Is.EqualTo(firstId));

            // Guard rails: no-op, moving an empty slot, out-of-range target.
            Assert.That(sys.MovePrioritySlot(user, 4, 4), Is.False);
            Assert.That(sys.MovePrioritySlot(user, 2, 3), Is.False);
            Assert.That(sys.MovePrioritySlot(user, 4, 9), Is.False);
            Assert.That(pool.PrioritySlots[4], Is.Not.Null);
        });
        await pair.RunTicksSync(5);
    }

    [Test]
    public async Task PrioritySlotCardMeasuresIdenticallyEmptyAndPinned()
    {
        var pair = Pair;
        var client = pair.Client;

        await client.WaitAssertion(() =>
        {
            var candidate = new AshfallCharacterCandidate
            {
                CandidateId = Guid.NewGuid(),
                Profile = HumanoidCharacterProfile.Random(),
            };
            var card = new Content.Client.Ashfall.CharacterGen.UI.AshfallPrioritySlotCard(0);

            card.Measure(new System.Numerics.Vector2(float.MaxValue, float.MaxValue));
            var emptyHeight = card.DesiredSize.Y;

            card.SetPin(new Content.Client.Ashfall.CharacterGen.AshfallClientPinnedSlot(
                0, candidate.CandidateId, candidate, new ProtoId<JobPrototype>("Passenger")));

            card.Measure(new System.Numerics.Vector2(float.MaxValue, float.MaxValue));
            var pinnedHeight = card.DesiredSize.Y;

            Assert.That(pinnedHeight, Is.EqualTo(emptyHeight),
                $"pinned slot card measures {pinnedHeight}px, empty one {emptyHeight}px: " +
                "pinned content must fit the height-locked card exactly like the empty state");
        });
        await pair.RunTicksSync(5);
    }
}
