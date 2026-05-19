namespace Dawnholder.Server.GameServer.Combat;

// M3 Phase 06 Step 2 (응급 전투 인프라):
// 적의 종류 식별자. byte로 박아 S_EntitySpawn 패킷의 entityKind 필드와 1:1 매핑 가능.
//
// Phase 07 보스 재사용 결정 (Codex β 사전 검증 봉합): 별도 BossEntity 만들지 않고 본 enum
// `Boss` 값 + 같은 EnemyEntity 사용. AI/state machine 미구현 응급 단계라 model 분리 가치 X.
// 보스 특수 동작(스테이지 클리어 broadcast 등)은 *Kind* 분기로 처리 (Phase 07 작업).
//
// stability 약속: 값은 영원히 고정. 새 종류는 *append-only* (Normal=0, Boss=1, 다음 = 2).
// 클라/서버 양쪽이 같은 byte 표를 봐야 하므로 enum 자체는 server-only지만 wire값 = byte.
public enum EnemyKind : byte
{
    Normal = 0,
    Boss = 1,
}
