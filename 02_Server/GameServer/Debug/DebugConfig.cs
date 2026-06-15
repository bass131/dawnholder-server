namespace Dawnholder.Server.GameServer.Debug;

// 시연용 디버그 치트 설정 (영호 요청 2026-06-15, M5+).
//
// **헌법 #3 (Trust Boundary)**: C_CheatCommand는 클라가 보내는 untrusted 입력.
//   서버가 이 플래그로 허용 여부를 최종 결정 — 클라가 직접 게임 상태를 바꾸지 않음.
//   AllowCheats=false면 핸들러가 패킷을 무시(빌드 클라가 F8 눌러도 무반응).
//
// 시연 편의로 기본 ON. **프로덕션/정식 배포 시 false로 내려야 함.**
//   const 아닌 static readonly — 게이트 분기(if !AllowCheats)가 const dead-code 경고(CS0162) 안 나게.
public static class DebugConfig
{
    public static readonly bool AllowCheats = true;
}
