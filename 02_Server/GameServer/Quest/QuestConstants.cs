namespace Dawnholder.Server.GameServer.Quest;

// 서버 권위 퀘스트 상수.
//
// **서버측 배치 근거**: targetCount를 S_QuestUpdate.targetCount 패킷 필드에 실어 보내므로
//   진짜 단일 진실 공급원(SSOT)은 wire — 클라이언트는 패킷 값만 표시(하드코딩 금지, 헌법 §1).
//   98_Shared에 두면 Shared.dll co-review(정유현 CODEOWNERS)가 불필요하게 트리거되므로
//   서버 권위 데이터로 서버측에 둔다.
public static class QuestConstants
{
    // 보스 포탈 해금에 필요한 누적 킬(파티 공유 또는 솔로 개인).
    // S_QuestUpdate.targetCount로 클라에 전달 — 클라 하드코딩 금지.
    public const int BossUnlockKillCount = 20;
}
