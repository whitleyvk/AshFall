using System.IO;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Ashfall.CharacterGen;

/// <summary>
///     Client requests the current candidate pool or requests a pool refresh.
/// </summary>
public sealed class MsgAshfallRequestPool : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public bool Refresh { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Refresh = buffer.ReadBoolean();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Refresh);
    }
}

/// <summary>
///     Server sends the candidate pool, current selection, and cooldown state to the client.
/// </summary>
public sealed class MsgAshfallPoolResponse : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public List<AshfallCharacterCandidate> Candidates { get; set; } = new();
    public int PoolRevision { get; set; }
    public int SelectedIndex { get; set; } = -1;
    public string? SelectedJob { get; set; }
    public float CooldownSecondsRemaining { get; set; }
    public int RemainingRefreshes { get; set; } = -1; // -1 = unlimited

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadInt32();
        Candidates = new List<AshfallCharacterCandidate>(count);
        for (var i = 0; i < count; i++)
        {
            var length = buffer.ReadVariableInt32();
            var bytes = buffer.ReadBytes(length);
            using var stream = new MemoryStream(bytes);
            serializer.DeserializeDirect(stream, out AshfallCharacterCandidate candidate);
            Candidates.Add(candidate);
        }

        PoolRevision = buffer.ReadInt32();
        SelectedIndex = buffer.ReadInt32();
        SelectedJob = buffer.ReadBoolean() ? buffer.ReadString() : null;
        CooldownSecondsRemaining = buffer.ReadFloat();
        RemainingRefreshes = buffer.ReadInt32();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Candidates.Count);
        foreach (var candidate in Candidates)
        {
            using var stream = new MemoryStream();
            serializer.SerializeDirect(stream, candidate);
            var bytes = stream.ToArray();
            buffer.WriteVariableInt32(bytes.Length);
            buffer.Write(bytes);
        }

        buffer.Write(PoolRevision);
        buffer.Write(SelectedIndex);
        buffer.Write(SelectedJob != null);
        if (SelectedJob != null)
            buffer.Write(SelectedJob);
        buffer.Write(CooldownSecondsRemaining);
        buffer.Write(RemainingRefreshes);
    }
}

/// <summary>
///     Client informs the server of their selected candidate index.
/// </summary>
public sealed class MsgAshfallSelectCandidate : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Core;

    public int SelectedIndex { get; set; }
    public int PoolRevision { get; set; }
    public string SelectedJob { get; set; } = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        PoolRevision = buffer.ReadInt32();
        SelectedIndex = buffer.ReadInt32();
        SelectedJob = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(PoolRevision);
        buffer.Write(SelectedIndex);
        buffer.Write(SelectedJob);
    }
}
