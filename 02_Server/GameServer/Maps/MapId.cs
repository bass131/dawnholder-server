namespace Dawnholder.Server.GameServer.Maps;

// M4.2 Phase 01: 맵 레지스트리 골격 — MapId enum.
//
// 각 값은 stable numeric id (헌법 #2 "Protocol is Sacred" 정합 — 은퇴 id 재사용 X).
// 순서/값은 append-only. 제거 X, 숫자 변경 X.
//
// Town    = 0: 마을. 플레이어 spawn 지점. 적 없음.
// HuntingGround = 1: 전투 구역. Normal enemy 1마리.
// BossRoom = 2: 보스 방. Boss 1마리. Stage Clear 트리거.
// Ending   = 3: 결과 화면 골격. 빈 맵.
public enum MapId
{
    Town = 0,
    HuntingGround = 1,
    BossRoom = 2,
    Ending = 3,
}
