namespace Dawnholder.Server.GameServer.Maps.Actions;

// 행동 실행 1건의 입력 컨텍스트 — 패킷에서 추출된 per-action 힌트.
//   ClientTick     = rewind 기준 (lag-comp), 전 행동 공통.
//   TargetEntityId = C_Attack 타겟 (평타 전용; 스킬은 공간질의 → -1).
//   Facing         = 클라 화면 방향 (Dash 방향 권위; 그 외 무시).
// readonly struct + in 전달 = 틱 루프 new 0 (헌법 #5).
internal readonly struct ActionContext
{
    internal readonly long ClientTick;
    internal readonly int TargetEntityId;
    internal readonly sbyte Facing;

    internal ActionContext(long clientTick, int targetEntityId, sbyte facing)
    {
        ClientTick = clientTick;
        TargetEntityId = targetEntityId;
        Facing = facing;
    }
}
