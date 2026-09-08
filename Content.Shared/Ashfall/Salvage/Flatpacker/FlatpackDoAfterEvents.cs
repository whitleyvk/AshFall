using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Ashfall.Salvage.Flatpacker;

[Serializable, NetSerializable]
public sealed partial class FlatpackPackDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class FlatpackUnpackDoAfterEvent : SimpleDoAfterEvent
{
}
