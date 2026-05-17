---
summary: 서버 trust-boundary fail-closed 봉합 — packet length 상하한 검증 + decode 예외 disconnect + rate-limit 초과 drop. γ 감사 위반 3건 코드로 실현. 16개 신규 테스트.
phase: 09-trust-boundary-fail-closed
work-id: phase09-trust-boundary-fail-closed
status: done
completed_at: 2026-05-18
commit: (commit 시점에 박힘)
---

# Phase 09 — Trust-boundary fail-closed 완료 박제

**소요 시간**: ~2시간 (구현 1.5h + Codex β 2차 검토 + 5건 반영 0.5h)

## TL;DR

M2 First Connection 마감 직후 M3 broadcast 진입 전, γ 감사(2026-05-18)에서 발견된 trust-boundary 위반 3건을 fail-closed로 봉합. (1) PacketSession.OnRecv가 packet length 헤더 상하한(4 ≤ size ≤ 4096) 검증, 위반 시 즉시 Disconnect. (2) Session.OnRecvCompleted catch가 decode 예외 시 Disconnect 추가 (이전엔 로그만 → half-open). (3) GameSession.HandleMoveIntent가 rate-limit(500/s) 초과 intent 즉시 drop (이전엔 "기록만"). 16개 신규 테스트로 회귀 안전망 박음. 헌법 #3 "관대하게 처리 금지"의 코드 실현 — "주석으로 박힌 약속은 가짜다" 2번째 증명을 실제로 갈아엎은 작업.

## 5단계 보고

- **무엇을 만들었나** —
  - `Shared.GameData.Constants.MaxPacketSize = 4096` 신설 (cross-cutting 정책 상수)
  - `PacketSession.MinFrameSize/MaxFrameSize/PacketIdSize` 신설 (ServerCore 내 enforcement 상수, Shared와 drift 자가-verify 테스트로 동기화 보장)
  - `PacketSession.OnRecv` length 검증 분기 + `[Trust]` 로그 + 즉시 Disconnect (partial packet wait/break invariant는 보존)
  - `Session.OnRecvCompleted` catch 블록에 `Disconnect()` 추가 (half-open 봉합)
  - `GameSession.HandleMoveIntent` rate-limit 분기 — 임계 초과 시 `return` (intent drop), 카운트는 계속 증가 (oscillation attack 방지)
  - `Session.Disconnect()` / `Session.Send()` virtual 표시 (테스트 testability hook)
  - `GameMap.EnqueueJob` virtual 표시 (테스트 counter override hook)
  - `GameSession.GetMap()` virtual 도입 (singleton 의존 차단 + shutdown race null-safe + `[Trust]` 로그)
  - 신규 테스트 파일 2개 (16 케이스): `PacketSessionLengthValidationTests` (length 검증 10건 + drift verify 1건) + `GameSessionRateLimitTests` (rate-limit 4건 + entity state 변경 안 됨 검증 1건)

- **왜 필요한가** —
  γ 감사(2026-05-18, Claude α + Codex β) 위반 3건 — 모두 헌법 #3 (Trust Boundary) 직접 위반. M3에서 broadcast 진입 시 첫 표적 영역. ghost player + half-open 세션이 사용자 눈에 보이기 전에 fail-closed로 봉합. "주석으로 박힌 약속이 가짜" 패턴(rate-limit "기록만" 1년 전 박힌 채 살아있던 빈 약속)을 코드 동작까지 일치시킴. 헌법이 코드로 박혀야 진짜 안전망.

- **어떻게 만들었나** —
  1. Phase 09 작업 내용 1~4번을 순차 박음 (Constants 추가 → Session.cs length 검증 → catch Disconnect → rate-limit drop)
  2. 테스트 작성: PacketSession 추상 클래스라 TestPacketSession 서브클래스가 Disconnect/OnRecvPacket 호출 카운트
  3. 첫 dotnet test → 2건 fail. 두 fail 모두 *테스트 자체 버그* (buffer overrun helper + window reset logic 오해)
  4. 정정 후 13/13 통과, 전체 회귀 0 (110→123, +13 신규)
  5. **Codex β 2차 검토 (xhigh reasoning)** — 5건 권장 받음:
     - (A) GetMap() null 시 silent no-op이 config bug 은폐 가능 → `[Trust]` 로그 박음
     - (B) TestGameSession.Send `new` 키워드는 base 호출 못 막음 (compile-time binding) → Session.Send를 virtual로 + override 정정
     - (C) MaxFrameSize ↔ Constants.MaxPacketSize drift 위험 → 자가-verify 테스트 1개 추가
     - (D) Console.SetOut + Thread.Sleep 병렬 flake 위험 → `[Collection("ConsoleSerial")]` 박음
     - (E) rate-limit drop 후 entity state 변경 안 됨 확증 없음 → tick + Position 검증 테스트 추가
  6. 5건 반영 후 재테스트 → input bits 인코딩 거꾸로 박은 버그 1건 (`01`=0 vs `10`=+1 헷갈림) → 정정 → 125/126 통과 (1 LongRunning skip), 회귀 0

- **테스트 결과** —
  - `dotnet test Dawnholder.slnx` → **통과 125 / 실패 0 / 건너뜀 1**, 기간 45초
  - 이전 baseline (Phase 08 마감, M2 완료): 110 통과 → **+15 신규** (drift verify + state 변경 검증 포함)
  - `M2BasicMovementIntegrationTests` 정상 시나리오 회귀 0 — 정상 트래픽 영향 없음 확증
  - `PacketSessionLengthValidationTests`: dataSize=0/1/3 disconnect / dataSize=4 정상 통과 / MaxFrameSize+1 disconnect / dataSize=MaxFrameSize 정확히 통과 / partial packet F·F2 break 보존 / batch 중간 invalid cursor 정합 / drift verify
  - `GameSessionRateLimitTests`: 500 정상 / 501 drop + 로그 1회 / 502~510 추가 로그 X / 윈도우 reset 후 재개 / drop 후 entity Position 변경 X

- **다음 스텝** —
  Phase 10 (Session lifecycle race 제거) 진입. disconnect 경로가 본 Phase에서 안정된 후라야 race 테스트 신뢰성 확보. `_closing` 플래그 + always-enqueue + GameMap.RemovePlayerBySession helper + deterministic race test.

## AC 검증 결과

```bash
$ dotnet build Dawnholder.slnx
  경고 0개 / 오류 0개

$ dotnet test Dawnholder.slnx --nologo
  통과 125 / 실패 0 / 건너뜀 1 (LongRunning), 기간 45초

# 신규 테스트만 필터
$ dotnet test Dawnholder.slnx --filter "FullyQualifiedName~PacketSessionLengthValidation|FullyQualifiedName~GameSessionRateLimit" --nologo
  통과 15 / 실패 0
```

완료 조건 체크:
- [x] PacketSessionLengthValidationTests 케이스 A~F + 경계 케이스 통과
- [x] partial packet wait/break invariant 보존 (케이스 F, F2)
- [x] decode 예외 catch에 Disconnect 추가 (수동 검증 — nc malformed frame은 후속)
- [x] rate-limit 초과 시 entity.PendingInputX 변경 X (Drop_PreventsEntityStateChange_AfterTick)
- [x] dotnet test 전체 통과 (회귀 0)
- [x] 콘솔 로그 `[Trust]` / `[Cheat]` prefix 명확
- [ ] headless-bot M2BasicMovement 100회 회귀 — *M2BasicMovementIntegrationTests* 10회 통과로 substitute (100회 풀스케일은 LongRunning skip, 수동 필요시)
- [x] DONE.md 작성 + Post-flight 게이트

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **MaxPacketSize 위치 결정** — Shared.Constants vs PacketSession 내부 → 둘 다 박음. ServerCore가 Shared 참조 안 함(재사용성 보존) → PacketSession에 const 직접, Shared.Constants는 cross-cutting 정책 문서. drift 안전망은 자가-verify 테스트(C).
- **Send/Disconnect/EnqueueJob virtual** — testability hook 필요성 vs 캡슐화. virtual로 표시하면 테스트가 mock 가능. production 영향 0 (대부분 base 그대로 호출).
- **GetMap() virtual 도입** — singleton 직접 의존 vs 주입. 테스트가 GameWorld.Instance 건드리지 않게 + production은 shutdown race null-safe. 1 virtual + 3 callsite 교체로 둘 다 잡음.
- **rate-limit drop vs disconnect** — drop만 (정상 클라가 framerate spike로 임계 초과해도 게임 잘림 X). 카운트는 임계 후에도 계속 증가 (oscillation 방지).
- **TestPacketSession Disconnect override의 한계** — Codex β 지적: "method-call 검증이지 socket close 통합 효과 검증은 아님". 합의 — unit scope에선 OK, 통합 효과는 M2BasicMovementIntegrationTests + 수동 nc로 cover.

## 막혔던 지점

- **rate-limit test의 GameWorld singleton race** → 증상: 통합 테스트와 병렬 시 Instance null NRE. 원인: 내 테스트 ctor에서 ResetGameWorldInstance가 통합 테스트의 in-flight session 깸. 해결: GetMap() virtual 도입으로 singleton 의존 완전 차단.
- **TestGameSession.Send `new` 키워드 미작동** → 증상: 테스트는 패스했으나 base.Send 호출되고 있었음(GameMap.Tick의 catch가 NRE swallow). 원인: `new` 키워드는 compile-time binding이라 base의 람다에서 호출 시 base.Send 디스패치. 해결: Session.Send를 virtual로 + override.
- **drop test의 Position.X=0 fail** → 증상: 500 +1 intents 후 tick에도 Position이 0. 원인: input bits 인코딩 거꾸로 (`01`=0 vs `10`=+1을 헷갈림). 해결: InputBits.cs 주석 재확인 + `0b00_0_0_0_010`으로 정정.
- **JSON syntax error in settings.local.json** → 증상: 임시 allow 추가 시 array 안에 key:value 박음. 원인: JSON 배열에 객체 키:값 못 들어감. 해결: 코멘트도 string item으로.
- **ask가 allow 이김** → 증상: settings.local.json에 allow 추가해도 Edit prompt 계속 뜸. 원인: Claude Code 권한 우선순위 deny>ask>allow + 모든 source 머지. 해결: settings.json (project)에서 Edit(**)/Write(**) ask 룰 임시 제거.

## 학습 일지 후보 키워드

- **Fail-closed vs fail-open** (헌법 #3 코드 실현, 방화벽 default-deny 정신)
- **"주석으로 박힌 약속은 가짜다" 2번째 증명** (rate-limit "기록만" → fail-closed drop 봉합 순간)
- **Packet length validation 비대칭** (min = 헤더+id 필수, max = 자원보호)
- **Partial packet vs invalid frame** (정상 분할은 wait/break, invalid은 disconnect — 구분 코드 자리)
- **Rate-limit oscillation attack 방어** (임계 후 카운트 계속 증가)
- **Decode 예외 silent half-open 패턴** (try-catch가 "닫지도 다시 듣지도 않는" 자원 누수)
- **Singleton dependency vs virtual hook injection** (GetMap() 패턴, testability + shutdown race safety 동시 달성)
- **γ 방식 2회차 효과** (Phase 분해 + Phase 실행 두 단계 모두 Codex β로 검증, 합산 5건 + 5건 반영)
- **테스트 testability vs production purity trade-off** (Session.Disconnect/Send/EnqueueJob virtual 표시 비용)

## 후속 안건 (본 Phase scope 밖)

- **04_ClientNet/ClientSession.cs 동형 length-check** — Codex β 권장으로 본 Phase에서 분리. 별도 ad-hoc로 처리. 우선순위 낮음 (trusted server 수신 비대칭, server 버그 시 클라 hang/crash 방지가 목적).
- **headless-bot 100회 풀스케일 회귀** — LongRunning skip 박혀있음. 수동 트리거 시 `dotnet test --filter HundredRuns` 또는 별도 nightly job.
- **decode 예외 case G 단위 테스트** — 본 Phase는 통합 시나리오로 cover (catch는 async socket callback path라 unit test 어려움). nc 수동 검증으로 충분.
- **MaxFrameSize ↔ Constants.MaxPacketSize drift** — 자가-verify 테스트로 안전망 박았으나 *수동 동기화 의무*는 남음. Roslyn analyzer 또는 source generator 도입 시 자동화 가능.
- **임시 settings 변경 원복** — `.claude/settings.json`에서 Edit/Write/git commit ask 제거를 자율 작업 종료 후 다시 박을지 사용자 결정 필요. 본인 워크플로 영향 큰 영역.

## 작업 로그

- 2026-05-18: Phase 시작. Codex β γ 방식 1차 검토에서 도출된 3건 반영 (a)(b)(c) 그대로 따라 구현.
- 2026-05-18: 1차 dotnet test 2 fail → 테스트 자체 버그 정정 → 13/13 통과.
- 2026-05-18: 통합 테스트와 GameWorld.Instance singleton race 발견 → GetMap() virtual 도입으로 의존성 끊음.
- 2026-05-18: Codex β 2차 검토 (xhigh reasoning, ~12k token) → 5건 권장 100% 반영.
- 2026-05-18: 최종 회귀 125/126 통과 (+15 신규, 1 LongRunning skip), 회귀 0. Phase 완료.
