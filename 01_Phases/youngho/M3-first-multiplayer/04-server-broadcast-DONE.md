---
summary: M3 Phase 04 완료 — 서버 broadcast 인프라. PDL S_PlayerJoin/S_PlayerLeave 신설 + GameMap.BroadcastToAll(closing 세션 skip) + Snapshot 전원 broadcast 전환 + GameSession EnterGameWorld initial roster 패턴 + OnDisconnected PlayerLeave broadcast + GameSession.IsClosing internal getter (Phase 10 race 패턴 일반화). BroadcastTests 5건 신설 (join 양방향/initial roster/leave/snapshot/lifecycle race deterministic). dotnet build green (경고 0 오류 0). **dotnet test 실측 X — SAC On 환경 Code Integrity 차단 (Event 3077, Policy ID 0283ac0f) 사고로 본 머신 dotnet test 불가**. Codex 1차 검증 시 Codex 환경에서 재실측 예정. 다음 = Codex β 검토 끊고 Phase 05 사용자 확인 모드.
phase: 04-server-broadcast
work-id: phase04-server-broadcast
status: done
completed_at: 2026-05-18
commit: TBD
---

# Phase 04 — 서버 Broadcast 인프라 완료 박제

**작업 시간**: ~60분 (응급 모드 예상 2h 대비 단축 — Phase 03 dispatch 분리 덕에 broadcast 패턴이 *깔끔한 자리*에 박힘)

## TL;DR

두 봇 접속 시 서로 알게 되는 broadcast 인프라 박음. PDL `S_PlayerJoin` / `S_PlayerLeave` 신설(PacketID 9/10), `GameMap.BroadcastToAll(payload, except)` 메서드(closing 중인 세션 skip — Phase 10 race 패턴 일반화), Snapshot을 owner unicast → 전원 broadcast 전환, `GameSession.EnterGameWorld`에 initial roster + 신규 join broadcast, `OnDisconnected`에 PlayerLeave broadcast 추가. BroadcastTests 5건 신설 (deterministic lifecycle race 재발 테스트 포함 — Codex Phase 04 risk 1순위). **dotnet build green** (경고 0 / 오류 0). **dotnet test는 SAC On 환경 차단 사고로 본 머신 실측 불가** — Codex 1차 검증 시 재실측 예정.

## 5단계 보고

- **무엇을 만들었나** —
  - **`99_Tools/PacketGenerator/PDL.xml`** + **재생성된 `98_Shared/Protocol/Generated/GenPackets.cs`** + **`98_Shared/bin/.../Shared.dll`** + **`03_Client/Assets/Plugins/Shared/Shared.dll`** (PDL 수정 의무 3종 정합):
    - `S_PlayerJoin { int entityId, float spawnX, float spawnY }` (PacketID 9) — broadcast 받는 측 동작 명세 박힘
    - `S_PlayerLeave { int entityId }` (PacketID 10) — `S_LeaveMap`(기존 미사용)과 의미 구별 박힘
  - **`02_Server/GameServer/Maps/GameMap.cs`**:
    - `BroadcastToAll(ArraySegment<byte> payload, GameSession? except = null)` 메서드 신설 — tick thread invariant, skip 규칙 3종(owner null / except / IsClosing)
    - `Tick`의 snapshot 부분: `p.Owner.Send(pkt.Write())` → `BroadcastToAll(pkt.Write())` (자기 자신 포함 전원). N² 비용 인지 박음 (응급 모드 N≤4 환경 무시 가능 / M4+ 배열 형태 PDL 확장 후보)
  - **`02_Server/GameServer/Network/GameSession.cs`**:
    - `internal bool IsClosing => Volatile.Read(ref _closing) == 1` getter 신설 — broadcast 발신 시 race 안전 판별
    - `EnterGameWorld`: AddPlayer *전에* 기존 player 목록 snapshot → 자기 add → 자기에게 기존 entity 전원 `S_PlayerJoin` 다발 Send (initial roster, closing skip) → 자기 외 모든 player에게 자기 `S_PlayerJoin` broadcast (`BroadcastToAll(joinNotice.Write(), except: self)`)
    - `OnDisconnected`: cleanup *전*에 entityId 캡처 → `RemovePlayerBySession` → removed=true이고 entityId≥0이면 자기 외에 `S_PlayerLeave` broadcast → 로그 → entityId reset
  - **`02_Server/GameServer.Tests/Network/BroadcastTests.cs`** 신설 (5건):
    - `TwoSessions_FirstReceivesPlayerJoin_WhenSecondJoins` — broadcast 도착 검증
    - `SecondSession_ReceivesInitialRoster_OnJoin` — 기존 entity 다발 Send (자기 entity 빠짐)
    - `RemainingSession_ReceivesPlayerLeave_WhenOtherLeaves` — leave broadcast 도착
    - `Snapshot_BroadcastsToAll_IncludingSelf` — 두 entity 각각의 snapshot이 두 session 양쪽 도달 (N²=4 packet)
    - `LifecycleRace_NewJoinBroadcastSkipsClosingSession` — **Codex Phase 04 risk 1순위 deterministic 재현** — s1 disconnect 진행 중 s2 connect 시 broadcast가 s1에 안 감 (IsClosing skip 효과)
- **왜 필요한가** —
  - 5/20 교수 중간 면담 응급 데모의 *핵심 시연 요소* = 두 명 같은 맵. 현재 `GameMap.cs:96` snapshot owner unicast는 *본인 1명 환경*에서만 정합 → 두 명 접속 시 서로 화면에 안 보임
  - Phase 10에서 봉합한 `_closing` + always-enqueue 패턴은 *connect/disconnect race* 만 봉합. broadcast가 들어오면 *N-1명에게 fan-out* 시점에도 race window가 생김 (Codex Phase 04 risk 1순위 명시 박힘). 본 Phase가 그 일반화 — `IsClosing` getter + `BroadcastToAll` skip
  - Initial roster 없으면 *후 입장자가 기존 player 전원을 못 봄* — broadcast는 *입장 직전부터 시점*만 커버. 입장 *전* 상태 동기화 = initial roster 책임
- **어떻게 만들었나** —
  - **PDL 형태 결정**: (A) S_PlayerJoin/S_PlayerLeave 단일 entity vs (B) S_Snapshot에 EntityState[] 배열 형태로 통합. → **(A)** 채택. 이유: PDL 도구가 *배열 타입 미지원* (현재 `<int>`/`<float>`/`<string>`/`<bool>` 등 스칼라만) → 배열 도입은 PacketGenerator 본문 + PacketFormat 변경 동반 = Phase 04 응급 모드 scope 폭발. M4+ 배열 형태 검토는 정착 후
  - **Snapshot broadcast 패턴**: 각 entity별로 *전원에게* broadcast (자기 자신 포함). 자기 entity의 packet은 reconcile 진입(lastAckedClientTick 본인 의미), 남 entity의 packet은 remote view 업데이트 — 분기는 클라(Phase 05) 책임. 서버는 *모두에게 동일 packet*만. N² 비용은 N≤4 데모 환경 무시
  - **race 봉합 위치**: (A) `Send` 안에서 closing 체크 vs (B) broadcast 측에서 `IsClosing` 체크. → **(B)** 채택. 이유: (A)는 `Send`가 매 호출 atomic 읽기 추가 → unicast 정상 흐름에도 부담 + `Send`는 PacketSession base의 virtual이라 *모든 Send 경로*에 박혀야 일관 → 변경 범위 ↑. (B)는 broadcast 측이 *batch*로 한 번 체크 → race window가 *동일 tick 안*에서만 의미라 결과 동등
  - **initial roster 순서**: AddPlayer *전*에 `List<PlayerEntity> existing = new(map.Players)` snapshot → 자기 add → 자기에게 다발 Send → 자기 외 broadcast. 만약 *AddPlayer 후*에 roster 만들면 자기에게 자기 PlayerJoin 보내게 됨 (자기 entity 포함). 분리 명확
  - **PlayerLeave entityId 캡처 순서**: cleanup *전*에 `int leavingEntityId = self._entityId`. cleanup 후엔 entityId reset(=−1)되므로. 단 *AddPlayer 안 끝남 race window*에선 -1이라 broadcast skip
  - **Test mock 패턴**: HandshakeHandlerTests의 SentPackets 캡처 + Disconnect 카운터 패턴 재사용. lifecycle race 테스트는 `OnConnected → AddPlayer 처리 tick X → OnDisconnected → OnConnected(s2) → 두 job 같은 tick` 시퀀스 — *deterministic*하게 race window 재현. 단일 thread tick invariant 활용
- **테스트 결과** —
  - **`dotnet build Dawnholder.slnx`: 경고 0 / 오류 0** (모든 영향 영역 컴파일 정합 — PDL 재생성 후 PDL/Shared/ClientNet/Server/Tests 7 프로젝트 통과)
  - **`dotnet test`: 실측 X (블로커 — 본 머신 환경 사고)**
    - 사고: `xUnit FileLoadException 0x800711C7 ERROR_APPLOCKER_APPLICATION_BLOCKED`
    - 진단 결과: AppLocker 차단 이벤트 0건 → **Smart App Control (SAC) = On** + CodeIntegrity Event ID 3077 박힘 → Policy ID `{0283ac0f-fff1-49ae-ada1-8a933130cad6}` (SAC 정책 ID) 차단
    - 우회 한계: SAC On → Off는 OS 재설치 필요 (Microsoft 의도된 제약). 5/20 면담까지 시간 비현실적
    - 응급 결정 = B (테스트 없이 commit). Phase 03 baseline 회귀 안전망 155 유지, Phase 04 추가 5건은 *Codex 1차 검증 시 Codex 환경에서 재실측*
    - 학습 박제: `~/.claude/.../memory/smart-app-control-dotnet-test-block.md` 진단 절차 박힘 (memory MEMORY.md 채널 등록)
- **다음 스텝** — **Codex β 1차 검증 끊기**. Phase 03 + Phase 04 묶음 검토 요청 (γ 방식 5회차, Codex *코드 직접 + dotnet test 재실측*). 위험 1순위: lifecycle race 일반화 패턴 정합 + initial roster 순서 + broadcast 발신 시점의 동시성. 검증 결과 따라 Phase 05 사용자 확인 모드 진입 (Unity remote entity registry — 정유현 영역 인접)

## AC 검증 결과

```bash
# 1. PDL 변경 의무 3종 완료
$ dotnet run --project 99_Tools/PacketGenerator -- 99_Tools/PacketGenerator/PDL.xml --no-wait --no-manager
   [GEN] GenPackets.cs → 98_Shared/Protocol/Generated/
   [GEN] --no-manager: PacketManager 출력 skip
   [GEN] Packet Generate Success.

$ dotnet build Dawnholder.slnx --nologo --no-incremental
   빌드했습니다.
       경고 0개
       오류 0개

# 2. Handlers/ 폴더 정합 (Phase 03 박힘) + 새 PDL 패킷이 GenPackets에 박혔는지
$ grep -E "S_PlayerJoin|S_PlayerLeave" 98_Shared/Protocol/Generated/GenPackets.cs | head -3
   public enum PacketID { ..., S_PlayerJoin = 9, S_PlayerLeave = 10 }
   public class S_PlayerJoin : IPacket { ... }
   public class S_PlayerLeave : IPacket { ... }

# 3. BroadcastTests 5건 박힘 (코드 정합 — 실행은 SAC 사고로 X)
$ grep -E "\[Fact\]" 02_Server/GameServer.Tests/Network/BroadcastTests.cs | wc -l
   5

# 4. dotnet test 시도 결과 (실측 블로커 박제)
$ dotnet test 02_Server/GameServer.Tests/GameServer.Tests.csproj --nologo --no-build
   Catastrophic failure: FileLoadException 0x800711C7
   Code Integrity (Policy ID {0283ac0f-fff1-49ae-ada1-8a933130cad6}) blocked
   → SAC On 환경 차단. Codex 1차 검증에서 재실측 예정 (별도 환경)
```

## 결정 흐름 (학습 일지 쓸 때 참고용)

- PDL S_Snapshot 배열 형태 도입 vs 단일 entity broadcast: 응급 모드 N≤4 환경에서 **단일 entity broadcast (N² 패킷)** 채택. 배열 도입은 PacketGenerator 본문 변경 동반(scope creep). M4+ 정착 시 검토
- broadcast race 봉합 위치: (A) Send 내부 vs (B) broadcast 측. **(B) 채택** — unicast 정상 흐름 부담 X + batch 한 번 체크로 race window 동일 결과
- initial roster 순서: AddPlayer 후 vs 전. **AddPlayer 전 snapshot** — 자기에게 자기 PlayerJoin 보내는 사고 차단
- 테스트 실측 우회: Unblock-File / Vanguard 종료 / AppLocker 진단 → 모두 우회 실패 → SAC On 확정. **B 옵션(테스트 없이 commit)** 채택. Codex 환경 재실측으로 회귀 안전망 위탁
- dotnet test 사고 대응 위치: 본 Phase commit에 포함 (코드 + 사고 박제 동시) vs 별도 환경 작업 분리. **동시 박제** — 사고 자체가 학습 가치 ★★ + CONTEXT.md 보류 항목 발화

## 막혔던 지점 (있다면)

- **dotnet test 0x800711C7 차단** — Phase 03까지 같은 환경에서 통과했었는데 Phase 04 PDL 재생성으로 다수 dll 새 hash → SAC reputation 미등록 → 차단. 진단 3단계(Unblock-File → AppLocker 이벤트 → SAC 상태) 거쳐 SAC On 확정. 우회는 OS 재설치 비용 → B 채택. memory `smart-app-control-dotnet-test-block.md`에 진단 절차 박제

## 학습 일지 후보 키워드

- **`smart-app-control-dev-environment-trap`** (★★★) — SAC On 환경 unsigned dotnet test dll 차단 사고. 코드 외 *머신 정책 사고* 두 번째 (Riot Vanguard 다음). 한국 PC 환경 특수 + 모든 .NET 개발자가 만날 수 있는 함정. `/journal:concept` 강력 후보
- **`broadcast-race-pattern-generalization`** (★★★) — Phase 10에서 봉합한 `_closing` + always-enqueue 단일 race를 N-1 broadcast로 일반화. `IsClosing` getter + `BroadcastToAll` skip 패턴. deterministic 재현 테스트가 *문서보다 강한* 약속
- **`initial-roster-snapshot-order`** (★★) — AddPlayer 전후의 roster 캡처 순서가 *자기 자신에게 자기 PlayerJoin 보내는 사고*를 결정. 분산 시스템의 *순서 결정의 비용*
- **`n-squared-broadcast-emergency-mode`** (★★) — 응급 모드에서 N² 패킷 vs PDL 배열 형태 trade-off. N≤4 환경에선 후자 비용이 더 큼. 응급 모드 vs 정착 모드 결정 패턴
- **`pdl-modify-3-tier-obligation`** (★) — PDL 수정 → regen → build → Shared.dll commit 3종 의무 정착. 정유현 PR #19 사고 패턴 후속, 이번에 본인이 같은 룰 일관 적용

## 잔존 알려진 결함 (Codex 검증 후속)

- **dotnet test 실측 X** — Codex 1차 검증에서 Codex 환경 재실측 필수. 회귀 0 확정 후 Phase 05 진입
- **S_Snapshot lastAckedClientTick 의미 분기** — 본인 entity packet만 본인 reconcile 진입, 남 entity packet은 무시 → 분기는 Phase 05 클라 책임 박힘. 분기 누락 시 *남 entity의 lastAckedClientTick으로 본인이 reconcile* 사고 가능 — Phase 05 진입 직전 명시 박을 것
- **N² broadcast 비용** — 데모(N≤4) 무시 가능, M4+ 배열 형태 PDL 확장 검토. ad-hoc 후속 후보
- **04_ClientNet / Unity 측 새 패킷 dispatch X** — `S_PlayerJoin`/`S_PlayerLeave` 클래스는 GenPackets에 박혔지만 클라 dispatch에 케이스 없음 → unknown drop. 정유현 pull 시 영향 X (빌드 안 깨짐), 단 *Phase 05 본격 dispatch* 시점에 박힘

