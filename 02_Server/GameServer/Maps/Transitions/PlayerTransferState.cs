using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps.Transitions;

/// <summary>
/// Tick-thread transfer values captured before removing the source entity; not validated external input.
/// Null Stats retains PlayerEntity's Knight fallback. Default can contain ID/HP zero and null Stats.
/// CurrentHp is raw; destination MaxHp comes from the definition, and transient state is recreated.
/// </summary>
public readonly record struct PlayerTransferState(int EntityId, PlayerStats? Stats, int CurrentHp);
