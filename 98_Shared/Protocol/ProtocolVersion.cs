namespace Shared.Protocol;

/// <summary>
/// 와이어 프로토콜 버전. 패킷 모양이 바뀔 때마다 bump (헌법 #2 "Protocol is Sacred").
///
/// **자리잡이 위치 활용** (98_Shared/CLAUDE.md Layout 표에 박혀있던 `(예정 — Phase M2+ 핸드셰이크)`
/// 자리에 Phase 07 D3 결정으로 박음):
///
/// **버전 이력**:
///   - v1: M2 Phase 04~06 — C_MoveIntent (sbyte inputX), S_Snapshot (x/y만).
///   - v2: M2 Phase 07 — C_MoveIntent (byte input 비트필드), S_Snapshot (vx/vy 추가).
///         InputBits 헬퍼 신설, jumpPressed 에지 패턴(D4).
///   - v3: M3 Phase 06 — Combat 4패킷 (C_Attack/S_EntitySpawn/S_HitResult/S_EntityDeath).
///         additive지만 데모 기능이 신규 패킷 의존이라 stale client 빠른 cutoff 위해 bump
///         (Codex β 사전 검증 MEDIUM #4, handshake exact equality 정합).
///   - v4: M3.8 Phase 03 — C_CharacterSelect (캐릭터 선택, 전사/원거리 분기).
///         backward compatible append-only지만 옛 빌드 클라가 캐릭터 선택 안 보내고 EnterGameWorld
///         시도하면 default stats 미박힘 → server-side null 처리 사고 차단 위해 bump.
///   - v5: M4.1 Phase 06 — C_Attack.attackerClientTick 추가 (lag compensation rewind).
///         backward compatible append-only지만 옛 클라가 tick 안 보내면 attackerClientTick=0 →
///         서버 rewind 범위 검증에서 silent drop 가능 → 빠른 cutoff 위해 bump.
///   - v6: M4.2 Phase 02 — C_EnterPortal / S_MapTransition 2패킷 추가 (맵 전환 프로토콜).
///         새 패킷에 의존하는 맵 이동 기능이 핵심이라 옛 클라 빠른 cutoff 위해 bump.
///         S_MapTransition에 entityId 없음 (ADR-026: 전역 id 풀, 맵 이동 시 id 유지).
///   - v7: M4.3 Phase 07 — S_EntityState 추가 (enemy AI 위치/상태 주기 브로드캐스트).
///         enemy AI FSM이 S_EntityState 의존이라 옛 클라 빠른 cutoff 위해 bump.
///   - v8: M4.3 Phase 08a — animState byte 필드 추가 (S_Snapshot + S_EntityState 각 맨 끝 append).
///         AnimState enum 신설 (Idle/Walk/Jump/Attack/Hit/Death) — 시각 애니 상태 서버 권위 결정.
///         (옛 약속 'Phase 09 S_EnemyAttack도 v8 포함'은 깨짐 — M4.5 Phase 04에서 v9로 신설 정정.)
///         append-only이지만 옛 클라가 animState 없이 파싱하면 오프셋 desync → 빠른 cutoff 위해 bump.
///   - v9: M4.5 Phase 04 — S_EnemyAttack 신설(적→플레이어 권위 데미지) + S_PlayerJoin.characterClass byte
///         맨 끝 append(원격 직업 표시). 두 변경 한 묶음 bump.
///   - v10: M4.7 Phase 01 — S_PlayerHp(플레이어 HP 권위 동기화 전용 이벤트) + S_PlayerAttack(원격 공격
///         발동 이벤트 — 허공 스윙 포함) 두 패킷 신설. 신기능(HP 동기화·원격 투사체·허공 스윙)이 신규
///         패킷 의존이라 옛 클라 빠른 cutoff 위해 한 묶음 bump. C_Attack 모양 불변(targetEntityId 의미만
///         "필수 타겟"→"선택 힌트 0=없음"). PDL append-only, ID 21/22 — 기존 enum 시프트 0.
///   - v11: M4.8 Phase 01 — S_ProjectileLaunch(23) + C_SkillUse(24) + S_SkillCast(25) 신설 +
///         S_HitResult 끝에 hitEffect(byte) append. 원거리 평타(서버 확정 투사체 + 지연 데미지) ·
///         최소 스킬 시스템(C_SkillUse 쿨다운 권위) · 썬더볼트 AoE가 신규 패킷 의존이라 옛 클라 빠른
///         cutoff 위해 한 묶음 bump. S_HitResult는 *끝에* byte append(기존 5필드 오프셋 불변).
///         PDL이 가변 길이 list 미지원이라 썬더볼트 타격은 적별 S_HitResult(hitEffect=2)로 회피.
///         ID 23~25 — 기존 enum 시프트 0.
///   - v12: M4.11 Phase 01 — S_EntityState 끝에 serverTick(int) append. 적 보간 시간축 통일
///         (RemoteEntity가 S_Snapshot의 serverTick 보간을 쓰지만 S_EntityState엔 미박힘 → desync 봉합).
///         S_EntityState ID(19) 불변, 기존 5필드 오프셋 불변. 옛 클라가 serverTick 없이 파싱하면
///         오프셋 desync → 빠른 cutoff 위해 bump.
///   - v13: M4.13 P5 — C_SkillUse 끝에 facing(byte) append. 방향전환 직후 대쉬 시 서버 FacingDir이
///         C_MoveIntent 입력 큐 지연으로 옛 방향이라 클라 예측(화면 방향)과 반대로 튀어 reconcile 클러스터
///         발생 → 클라 화면 방향을 동봉해 서버가 대쉬 임펄스 방향 권위로 사용(부호 정규화, cheat 무해).
///         C_SkillUse ID(24) 불변, skillId/attackerClientTick 오프셋 불변. 옛 클라가 facing 없이 보내면
///         서버 파싱 오프셋 desync → 빠른 cutoff 위해 bump.
///   - v14: M4.15 P06 — C_SkillUse 끝에 verticalDir(byte) append. 텔레포트 4방향(위/아래 추가) — facing은
///         좌우 스프라이트 방향 유지, verticalDir이 수직 의도 전달(whitelist 0=수평/1=위/2=아래, 그 외 서버 0
///         정규화 — 3진 정의역이라 facing의 2진 `==1?1:-1` 패턴 금지). 영호 Option B(전용 필드, 의미 분리) GO.
///         C_SkillUse ID(24) 불변, skillId/attackerClientTick/facing 오프셋 불변(append-only). 옛 클라가
///         verticalDir 없이 보내면 서버 파싱 오프셋 desync → 빠른 cutoff 위해 bump.
///   - v15: M5 A0 — 파티/퀘스트/보스게이트 8패킷 신설(C_PartyInvite/C_PartyRespond/C_PartyLeave/
///         S_PartyInviteRecv/S_PartyUpdate/S_PartyError/S_QuestUpdate/S_PortalLocked, ID 26~33). 프로젝트 첫
///         플레이어 간 협동(파티 초대/수락 2명 + 공유 40킬 + 보스 포탈 잠금)이 신규 패킷 의존이라 옛 클라 빠른
///         cutoff 위해 한 묶음 bump. 기존 ID 25(S_SkillCast)까지 시프트 0(append-only, 맨 아래 추가).
///         PDL 가변 list 미지원 → 파티 정원 2 고정(S_PartyUpdate member0/member1 2슬롯, 빈=0).
///         모든 C_Party* 행위자=GameSession._entityId 강제(패킷에 행위자 필드 X — 도용 차단, 헌법 #3).
///   - v16: M5+ (시연용 디버그) — C_CheatCommand(34) 신설. 클라 F8 → 서버 AllowCheats 게이트 통과 시
///         퀘스트 즉시완료(호출자 killCount=BossUnlockKillCount → S_QuestUpdate + 보스 해금). 빌드 클라 포함
///         모든 빌드에 키+전송(가드 없음), 서버가 허용 결정(헌법 #3 — 클라 입력 untrusted). 시연 편의로
///         AllowCheats 기본 ON(프로덕션 배포 시 false). append-only, ID 34 — 기존 ID 33(S_PortalLocked) 시프트 0.
///
/// **핸드셰이크 봉합 (M3 Phase 02 완료, 2026-05-18)**:
///   - C_Handshake { clientVersion } / S_HandshakeResult { ok, serverVersion, reason } 신설 (PDL).
///   - GameSession.OnRecvPacket first-packet 강제 — handshake 외 첫 패킷은 즉시 Disconnect.
///   - clientVersion == Current → ok=true + EnterGameWorld (AddPlayer).
///   - clientVersion != Current → ok=false + reason 박고 즉시 Disconnect (헌법 #3 정합 — timeout 안 기다림).
///   - 호환 가능 minor version 호환표는 응급 모드 범위 밖 — 본 마감 시 별도 Phase.
///
/// **타입 ushort 이유**: 4 byte uint은 과잉, 1 byte byte는 256 버전 한계로 부족할 수 있어 2 byte ushort.
/// 65535 버전이면 12년간 매일 bump해도 안 떨어짐.
/// </summary>
public static class ProtocolVersion
{
    /// <summary>현재 프로토콜 버전. v16 = M5+ 시연용 디버그 치트 C_CheatCommand 신설 (ID 34).</summary>
    public const ushort Current = 16;
}
