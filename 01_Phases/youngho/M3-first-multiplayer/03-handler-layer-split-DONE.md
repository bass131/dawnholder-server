---
summary: M3 Phase 03 완료 — 헌법 #4 "Shared Code Discipline" 가짜 약속 2번째 봉합. `02_Server/CLAUDE.md` Layout에 약속만 박혀있던 `Handlers/` 폴더를 실재로 신설(5파일) + GameSession inline HandleXxx 3개 추출 + Dictionary dispatch + 02_Server/CLAUDE.md Layout 표 *동시 commit*. Codex 인사이트 1:1 준수 = handler는 decode+검증, session은 lifecycle state. 누락 invalid/auth 페어 4건 신설 (MoveIntentHandlerTests 2 + PingHandlerTests 2). dotnet test 155/0/1 green (Phase 02 baseline 151 → +4, 회귀 0). 다음 = Phase 04 서버 broadcast.
phase: 03-handler-layer-split
work-id: phase03-handler-layer-split
status: done
completed_at: 2026-05-18
commit: TBD
---

# Phase 03 — 핸들러 Layer 분리 + 02_Server/CLAUDE.md 정합 완료 박제

**작업 시간**: ~60분 (응급 모드 예상 2h 대비 단축 — 기존 Codex 인사이트로 GameSession `CompleteHandshakeAndEnter()` 캡슐화가 Phase 02에 이미 박혀있어 외부 추출 marginal cost ↓)

## TL;DR

헌법 #4 "Shared Code Discipline"의 *가짜 약속 2번째* 봉합 — `02_Server/CLAUDE.md` Layout 표에 7 Phase째 박혀있던 `Handlers/ PacketId → handler dispatch` 디렉토리를 *실재로* 신설. `GameSession.OnRecvPacket`의 inline `HandleHandshake`/`HandlePing`/`HandleMoveIntent` 3개를 외부 `IPacketHandler` 구현체로 추출, `HandlerRegistry` static Dictionary로 dispatch. Codex 인사이트 (Phase 02 review 박힘) 1:1 준수 — handler는 decode + 검증만, lifecycle state(`_handshakeCompleted` / `_entityId` / rate-limit window)는 GameSession 안 `internal` 메서드로 캡슐화. **`02_Server/CLAUDE.md` Layout 표는 코드 변경과 *동일 commit*** (반쪽 봉합 X — 코드만 바꾸고 문서 안 바꾸면 약속이 다시 가짜가 됨). 누락된 핸들러 단위 테스트 페어 4건 신설(MoveIntentHandlerTests happy + invalid bits / PingHandlerTests happy + auth). dotnet test 155/0/1 (Phase 02 baseline 151 → +4, 회귀 0).

## 5단계 보고

- **무엇을 만들었나** —
  - **`02_Server/GameServer/Handlers/` 폴더 신설 (5파일)**:
    - `IPacketHandler.cs` — `internal interface IPacketHandler { void Handle(GameSession, ArraySegment<byte>); }` (decode + 검증 + session 호출 책임 명시)
    - `HandshakeHandler.cs` — C_Handshake decode → version 검증 → `session.CompleteHandshakeAndEnter()` 또는 `session.RejectHandshake(reason)` 호출
    - `MoveIntentHandler.cs` — C_MoveIntent decode → `InputBits.Decode` → `session.SubmitMoveIntent(inputX, jumpPressed, valid, rawInput, clientTick)` 호출
    - `PingHandler.cs` — C_Ping decode → `session.RespondPong(clientTimestampMs)` 호출
    - `HandlerRegistry.cs` — `static readonly IReadOnlyDictionary<PacketID, IPacketHandler>` + `TryGet(id, out handler)` API
  - **`02_Server/GameServer/Network/GameSession.cs`**:
    - inline `HandleHandshake`/`HandlePing`/`HandleMoveIntent` 3개 제거
    - `CompleteHandshakeAndEnter()` `protected` → `protected internal` (Handlers/ + 테스트 subclass 양쪽 호환)
    - 신설 internal API 3개: `RejectHandshake(string reason)` / `SubmitMoveIntent(sbyte, bool, bool, byte, uint)` / `RespondPong(long)`
    - `OnRecvPacket` 재작성 — first-packet 게이트 유지 + duplicate handshake 거절 게이트 분리 + Dictionary dispatch (`HandlerRegistry.TryGet`)
    - 클래스 헤더 주석에 M3 Phase 03 변경 박제
  - **`02_Server/CLAUDE.md`**:
    - Layout 표 갱신 — `Handlers/` 안 5파일 1:1 표기 + `Network/GameSession.cs` 책임 명시 + `GameServer.Tests/` 구조 보강
    - "M3 Phase 03 (헌법 #4 봉합): 본 표는 *실제 폴더 구조와 1:1 정합*" 주석 박음
    - 금지 사항 1줄 추가 — "핸들러가 GameSession 내부 state를 *직접* 만짐 — session 캡슐화 메서드(internal)만 호출"
    - "새 packet handler 추가" 4단계 정합 (HandlerRegistry 등록 + 핸들러 책임 명시)
  - **테스트 신설 2파일 4건**:
    - `02_Server/GameServer.Tests/Network/MoveIntentHandlerTests.cs` — `Happy_ValidInputBits_AppliesIntent` + `Invalid_ReservedInputBits_NormalizesAndLogsCheat`
    - `02_Server/GameServer.Tests/Network/PingHandlerTests.cs` — `Happy_AfterHandshake_PingResponds_WithPong` + `Auth_BeforeHandshake_PingRejectedByFirstPacketGate`
- **왜 필요한가** —
  - 헌법 #4 "Shared Code Discipline"의 두 번째 *가짜 약속*. Phase 02에서 헌법 #2의 가짜 약속 1번째(핸드셰이크 코드 미구현) 봉합한 직후, 같은 패턴의 두 번째 = `02_Server/CLAUDE.md`에 `Handlers/` 폴더가 약속만 박혀있고 실재 X (7 Phase째 잔존). 헌법 자체에 "복사-붙여넣기 금지 / 양쪽 동일 컴파일된 어셈블리 참조"가 박혀있는데 *서버 본인 도메인에서도 책임 분리 약속을 안 지킨* 상태가 모순
  - dispatch 패턴 자체 학습 가치 — if-else 체인은 새 핸들러 추가 시 누락 위험 (silent drop), switch는 컴파일러 exhaustive 강하지만 본문 수정 필요. Dictionary는 데이터+코드 분리 + 한 줄 등록. M3 5/20 데모 직전 broadcast(Phase 04) / remote entity(Phase 05) 진입하면서 새 핸들러 다수 추가 예정 → 지금 layer 정리하지 않으면 inline 메서드만 비대
  - Codex review 권장(Phase 02 #1) `CompleteHandshakeAndEnter()` 캡슐화 패턴 = "외부 핸들러가 lifecycle state 직접 침범 X" 보장의 *진짜 시연*이 본 Phase. Phase 02에선 캡슐화 *준비*만 박혔고, Phase 03이 그 시연
- **어떻게 만들었나** —
  - **인터페이스 모양 선택지**: (A) `IPacketHandler` 단일 + GameSession 전체 전달 vs (B) `IPacketHandler<TPacket>` 제네릭 + decoder 책임 분리 vs (C) static method + `Dictionary<PacketID, Action<...>>`. → **(A)** 채택. 이유: 응급 모드 2h 단순 + 핸들러 stateless라 instance 크기 무시 + 테스트 시 Send/Disconnect override 패턴 유지 + (B)/(C)는 boxing/static의 trade-off 학습 가치는 있으나 *Phase 03 응급 모드 scope creep*
  - **dispatch 책임 분리**: first-packet 게이트와 duplicate handshake 게이트는 *GameSession.OnRecvPacket 안*에 유지. 핸들러는 *게이트 통과 후* 진입. 이유: handler가 `_handshakeCompleted` getter 호출하는 패턴은 lifecycle state를 외부에 노출 → Codex 인사이트 정신 위반. 게이트가 게이트키퍼 책임 가지고 dispatcher가 dispatcher 책임만 가지는 게 더 깔끔
  - **`protected internal` 선택**: `CompleteHandshakeAndEnter()`는 Phase 02에서 `protected`로 박혔는데, Handlers/는 같은 어셈블리지만 subclass 아님 → `internal` 필요. 단 기존 테스트 subclass는 `protected` 호출 패턴 박혀있음 → 둘 다 만족하는 `protected internal`. 신설 `RejectHandshake`/`SubmitMoveIntent`/`RespondPong`은 테스트 subclass에서 호출 안 하니 `internal`만
  - **`MarkHandshakeCompletedForTest()` 옛 helper 처리**: Phase 02 Codex review #1에서 *제거*된 상태(`CompleteHandshakeAndEnter()` 캡슐화로 대체). Phase 03 시작 시점에 이미 없어진 상태라 Phase 03 작업 X
  - **MoveIntent invalid cheat 로그 위치**: (A) 핸들러가 cheat 로그 박고 session에 정규화 값만 전달 vs (B) 핸들러는 (valid 플래그, rawInput) 그대로 전달, session이 cheat 로그. → **(B)** 채택. 이유: cheat 로그가 `_entityId` 참조 → session 내부 정보 → session이 박는 게 자연. 핸들러가 박으려면 entityId getter 노출 필요(state leak)
  - **누락 테스트 페어 최소화**: HandshakeHandlerTests 이미 4건(Phase 02 + Codex 후속) → 충분. MoveIntent는 happy/invalid bits만 신설(rate-limit drop은 GameSessionRateLimitTests, auth는 HandshakeHandlerTests `NonHandshakeFirstPacket_Rejected_NoEntry`의 C_MoveIntent 케이스). Ping은 핸들러 단위 테스트 0건이라 happy + auth 둘 다 신설. 진짜 핸들러 격리 테스트(internal 직접 호출)는 본 마감 후 InternalsVisibleTo 박을 때 별도 — 응급 모드 통합 테스트 OK
  - **Codex 인사이트 시연**: handler 코드 본문이 *짧음* (HandshakeHandler 17줄 / MoveIntentHandler 12줄 / PingHandler 11줄). lifecycle 분기 / Send / Disconnect / EnqueueJob 어느 것도 핸들러 안에 없음 → "decode + 검증 + session 호출" 약속 *문법적으로 강제* (Send/Disconnect/EnqueueJob 호출이 GameSession internal 메서드 안으로만 들어감)
- **테스트 결과** —
  - `dotnet test Dawnholder.slnx --nologo`: **155 통과 / 0 실패 / 1 skip / 46s** (`Hundred_runs_all_succeed` LongRunning skip 유지)
  - Phase 02 baseline 151 → **+4** (MoveIntentHandlerTests happy + invalid / PingHandlerTests happy + auth). 회귀 0건
  - `dotnet build Dawnholder.slnx`: 경고 0 / 오류 0 (모든 영향 영역 컴파일 정합)
- **다음 스텝** — M3 Phase 04 — 서버 Broadcast 인프라. 자율 모드 계속 — Phase 03~04 묶음 자율 후 Codex 1차 검증(하이브리드 정책). Phase 04 핵심: PDL `S2C_PlayerJoin`/`S2C_PlayerLeave` 신설 + `GameMap.cs:95` snapshot owner unicast → multi-target broadcast + initial roster 패턴 + Phase 10 lifecycle race 재발 risk 봉합

## AC 검증 결과

```bash
# 1. Handlers/ 폴더 실재 — 5파일
$ ls 02_Server/GameServer/Handlers/
   HandlerRegistry.cs HandshakeHandler.cs IPacketHandler.cs MoveIntentHandler.cs PingHandler.cs

# 2. 02_Server/CLAUDE.md Layout 표 정합
$ grep -A 8 "│   ├── Handlers/" 02_Server/CLAUDE.md
   │   ├── Handlers/       IPacketHandler 단위 + dispatch 테이블 (M3 Phase 03 신설)
   │   │   ├── IPacketHandler.cs       internal 인터페이스 (decode + 검증 + session 호출)
   │   │   ├── HandlerRegistry.cs      Dictionary<PacketID, IPacketHandler> (한 줄 등록)
   │   │   ├── HandshakeHandler.cs     C_Handshake → version 검증 → session 캡슐화 메서드
   │   │   ├── MoveIntentHandler.cs    C_MoveIntent → InputBits.Decode → session 캡슐화 메서드
   │   │   └── PingHandler.cs          C_Ping → session 캡슐화 메서드

# 3. 핸들러 단위 테스트 페어 (Phase 03 핵심)
$ dotnet test Dawnholder.slnx -nologo --no-build --filter "FullyQualifiedName~HandlerTests"
   HandshakeHandlerTests (4 total)                      [모두 통과]
   MoveIntentHandlerTests (2 total)                     [모두 통과]
   PingHandlerTests (2 total)                           [모두 통과]

# 4. 전체 회귀 (Phase 02 baseline 151 → +4)
$ dotnet test Dawnholder.slnx --nologo --no-build | tail -3
   통과!  - 실패: 0, 통과: 155, 건너뜀: 1, 전체: 156, 기간: 46s - GameServer.Tests.dll

# 5. GameSession.OnRecvPacket inline 메서드 제거 확인
$ grep -c "void Handle" 02_Server/GameServer/Network/GameSession.cs
   0    (HandleHandshake/HandlePing/HandleMoveIntent 모두 외부 추출됨)
```

## 결정 흐름 (학습 일지 쓸 때 참고용)

- 인터페이스 모양: (A) `IPacketHandler` 단일 + GameSession 전체 전달 vs (B) `IPacketHandler<TPacket>` 제네릭 vs (C) static method + Action. → **(A)** 채택. 응급 모드 2h scope + 핸들러 stateless라 instance 크기 무시 + 기존 테스트 mock 패턴 유지
- dispatch 책임 분리: first-packet 게이트 + duplicate handshake 게이트는 GameSession.OnRecvPacket 안에 유지, 핸들러는 *게이트 통과 후* 진입. → 핸들러가 `_handshakeCompleted` getter 호출하는 패턴 = lifecycle state 외부 노출 → Codex 인사이트 위반
- 접근 한정자: `CompleteHandshakeAndEnter()` `protected` → `protected internal`. → Handlers/(같은 어셈블리 외부 클래스 internal) + 기존 테스트 subclass(protected) 양쪽 호환 동시 만족
- MoveIntent invalid cheat 로그 위치: (A) 핸들러가 박음 vs (B) session이 박음. → **(B)** 채택. cheat 로그가 `_entityId` 참조 → session 내부 정보, 핸들러가 박으려면 state getter 노출 (state leak)
- 누락 테스트 페어 최소화: HandshakeHandlerTests 4건(Phase 02 기준) → 충분, MoveIntent는 happy/invalid bits만 신설(rate-limit/auth는 기존 테스트가 커버), Ping은 0건이라 happy + auth 둘 다 신설
- 격리 단위 테스트 vs OnRecvPacket 통과 통합: (A) InternalsVisibleTo 박고 핸들러 instance 직접 호출 vs (B) OnRecvPacket을 통해 dispatcher 거쳐 검증. → **(B)** 응급 모드 채택, (A)는 본 마감 후 InternalsVisibleTo 박을 때 별도

## 막혔던 지점 (있다면)

- 없음. Phase 02에서 Codex 인사이트로 `CompleteHandshakeAndEnter()` 캡슐화가 *미리* 박혀있어 외부 추출 marginal cost가 최소화됨 → Phase 03 예상 2h가 실제 60분으로 단축. Codex 인사이트의 *선행 투자* 가치 확증

## 학습 일지 후보 키워드

- **`dispatch-pattern-trade-off`** (★★) — if-else / switch / Dictionary 셋의 확장 비용 차이 실측. Dictionary 채택의 hidden cost = TryGetValue + V-table 1단계. `/journal:concept` 후보
- **`false-promise-third-instance`** (★★★) — "주석으로 박힌 약속은 가짜다" 세 번째 봉합 (Phase 02 첫 번째 헌법 #2 봉합 → Phase 03 두 번째 헌법 #4 봉합). Rule of Three 통과 → 약속 가짜화 패턴 정착 단계. `/journal:concept` 강력 후보
- **`document-code-simultaneity`** (★★) — 02_Server/CLAUDE.md Layout 표 동시 commit이 *왜 가짜 약속 봉합의 핵심인지*. 코드만 바꾸고 문서 안 바꾸면 약속이 다시 가짜화. 반쪽 봉합 = 봉합 아님
- **`protected-internal-intentional-use`** (★) — 어셈블리 분리(GameServer.Tests 외부 어셈블리 subclass) + 같은 어셈블리 외부 클래스(Handlers/) 호출 동시 허용. 의도된 사용 사례 첫 시연
- **`handler-stateless-discipline`** (★★) — 핸들러 본문이 *짧음*(11~17줄) → 비대해지는 PR이 들어오면 "이거 session에 들어가야 하는 책임 아니냐" 자연 리뷰 트리거. 약속이 *기계적으로* 강제되는 상태
- **`codex-insight-preliminary-investment`** (★★) — Phase 02 Codex review에서 박은 `CompleteHandshakeAndEnter()` 캡슐화가 Phase 03 외부 추출 cost를 *선행 절감*. 리뷰 권장이 *미래 Phase의 비용*까지 낮추는 효과 첫 실측

## 헌법 #4 봉합 의미

Phase 02가 헌법 #2 "Protocol is Sacred"의 가짜 약속 1번째였다면, Phase 03은 헌법 #4 "Shared Code Discipline"의 가짜 약속 2번째. 두 봉합의 공통 정신:

1. **코드 + 문서 동시 commit** — 코드만 바꾸고 문서(`02_Server/CLAUDE.md` Layout 표) 안 바꾸면 약속이 다시 가짜가 됨. *반쪽 봉합*은 봉합 아님
2. **약속 가짜화의 정량 비용** — Phase 02 시점 헌법 #2 가짜 약속은 *7 Phase째* 잔존. Phase 03 시점 헌법 #4 가짜 약속도 *7 Phase째* 잔존. M2.5 ad-hoc 감사에서 본인이 박은 "주석으로 박힌 약속은 가짜다" 두 번째 증명 패턴이 *문서 약속에도 동일* 적용
3. **봉합 후 약속이 코드로 자체 강제** — IPacketHandler interface signature가 짧고 명확 → 새 핸들러가 lifecycle state 침범하는 코드는 *컴파일러가 reject* (internal 메서드 호출만 가능). 약속이 *기계적으로 강제*되는 상태

## 학습 박제

- **Dispatch 패턴 trade-off 실측 시연** — if-else / switch / Dictionary 셋 다 작동하지만 *확장 비용*은 다름. Dictionary 채택의 hidden cost = TryGetValue 한 호출 + handler instance dispatch (V-table 1단계). 응급 데모 환경에선 무시 가능 마이너스
- **`protected internal` 의도된 사용** — 같은 어셈블리 내부 + subclass 호환 동시 만족. 어셈블리 분리(GameServer.Tests)에서 호출 패턴 보존 + 같은 어셈블리 외부 클래스(Handlers/) 호출 동시 허용
- **handler 책임 = decode + 검증** 정신의 *문법적 시연* — 짧은 본문 자체가 약속의 시각화. handler가 비대해지는 PR이 들어오면 "이거 session에 들어가야 하는 책임 아니냐"는 자연스러운 review 질문 트리거

## 미해소 — Phase 04+ 진입 직전 참조

- `IPacketHandler.cs`는 internal interface — GameServer.Tests에서 *격리 단위* 테스트(핸들러 instance 직접 호출) 박으려면 `InternalsVisibleTo` 추가 필요. 응급 모드 scope 외 — 본 Phase는 *OnRecvPacket 통과* 통합 테스트로 충분
- HandlerRegistry는 *constructor-time static* Dictionary — DI 도입 시 (M4+) `IServiceProvider` 기반으로 전환 후보. 현재는 응급 모드 단순
- Phase 04 Codex 검토(Phase 정의 박힘) — broadcast multi-target 패턴이 *lifecycle race 재발* 1순위 risk. 본 Phase의 dispatch 분리가 그 봉합의 *전제 조건* (broadcast 호출자 책임이 session 안에 있음 = race 안전 패턴 적용 가능)

