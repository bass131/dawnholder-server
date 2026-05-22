---
owner: youngho
milestone: M4.1
phase: 03
title: lag compensation 200ms rewind + precision hitbox (AABB/capsule)
status: pending
grade: 복잡
risk: trust-boundary
estimated: 3~5h
domain: server
---

# Phase 03: lag compensation 200ms rewind + precision hitbox

> **상태**: pending
> **마일스톤**: M4.1
> **등급**: 복잡 (1 도메인 server / ~150~200줄 / trust-boundary 위험 깃발)
> **담당**: server SubAgent (Sonnet) — reviewer Tier 2-A 자동 호출 예정 (코드 변경 + trust-boundary)

---

## 🎯 목표

**M3 응급 박힌 단순 `dist² < range²` hitbox + lag compensation 없음 결함을 본 마감 정밀화로 승격**. 200ms 인터넷 지연 환경에서도 *공정한 hit 판정* + cheat 차단 양립.

본 Phase가 끝나면 = (a) `PlayerEntity` position ring buffer (4 tick = 200ms 깊이), (b) `C_Attack` PDL에 `attackerClientTick` 필드 추가 (PDL append-only + ProtocolVersion bump), (c) `GameMap.ProcessAttack`에서 attacker tick 시점으로 rewind, (d) precision hitbox = AABB (응급) 또는 capsule (점프 정합 — Phase 01 Codex 결정), (e) 단위 테스트 8건+ 통과 + 부하 봇 lag 환경 hit 일관성 검증.

---

## ⏪ 사전 조건

- [ ] Phase 02 (Formulas.cs 분리) 마감 — damage apply 분기에서 Formulas 호출 정합
- [x] PDL append-only 패턴 (M3 Phase 02 handshake 학습 정합)
- [x] PacketGenerator 재생성 의무 (PDL 변경 시, ADR-002)
- [x] `GameMap` actor 단일 thread invariant (헌법 #5)

---

## 📝 작업 내용

### 1단계: `PlayerEntity` position history ring buffer

- [ ] `PlayerEntity.PositionHistory` 신설 — `Vector2[4]` ring buffer (4 tick = 200ms 깊이, 응급)
- [ ] `PositionHistoryHead` int (다음 쓰기 index)
- [ ] `RecordPosition(long serverTick, Vector2 pos)` 메서드 — tick thread 안에서만 호출, lock 없음 (actor 정합)
- [ ] `GetPositionAtTick(long serverTick) → Vector2` 메서드 — ring buffer에서 가장 가까운 tick lookup. 범위 밖이면 *현재 위치* fallback (cheat 차단 = silent drop 후속)
- [ ] `GameMap.Tick` 안에서 매 tick `Physics.Step` 후 `RecordPosition(tickNumber, p.Position)` 호출 (헌법 #5 동기)

### 2단계: PDL 변경 — `C_Attack` 필드 추가

- [ ] `98_Shared/Protocol/PDL.xml` — `C_Attack`에 `int attackerClientTick` 필드 append-only 추가
- [ ] `99_Tools/PacketGenerator/` 재생성 (ADR-002 + 99_Tools/CLAUDE.md 정합)
- [ ] `98_Shared/Protocol/Generated/GenPackets.cs` 갱신 확인
- [ ] `Shared.csproj` 재빌드 → `03_Client/Assets/Plugins/Shared/Shared.dll` 자동 복사 + commit
- [ ] `ProtocolVersion.Current` = 3 → 4 bump (헌법 #2 정합 — 필드 추가는 backward compatible이지만 클라 옛 빌드 호환성 위해 bump)
- [ ] `98_Shared/CLAUDE.md` Current=3 → Current=4 정합 갱신 (M3.6 학습 정합)

### 3단계: 클라 측 `C_Attack` 송신 정합

- [ ] `03_Client/Assets/Scripts/Combat/` 또는 `Input/` — `C_Attack` 송신 시점에 `clientTick` 박음
- [ ] 응급 = `clientTick = NetworkBootstrap.LastReceivedServerTick` 또는 `MainThreadDispatcher` 보유 tick 사용 (Phase 01 Codex 결과로 결정)
- [ ] 헤드리스 봇 (`99_Tools/headless-bot/`) 동일 박음

### 4단계: `GameMap.ProcessAttack` lag compensation rewind

- [ ] step 4 (rate-limit) 통과 후 step 5 (range 검증) *전*에 rewind 분기 박음
- [ ] `long attackerClientTick`을 attack 파라미터로 받음 (현재 시그니처 `attackerEntityId/targetEntityId` → `attackerEntityId/targetEntityId/attackerClientTick`)
- [ ] rewind 범위 검증 (헌법 #3 정합) — `currentServerTick - attackerClientTick > 4 tick (= 200ms)`이면 silent drop (cheat 후보)
- [ ] `Vector2 rewindedAttackerPos = attacker.GetPositionAtTick(attackerClientTick);` (target은 *현재* 위치, 응급 — target도 rewind은 M4.3 backlog)
- [ ] range 검증 = `dist² < range²` (`rewindedAttackerPos` vs `target.Position`)

### 5단계: precision hitbox 승격 (AABB 우선, capsule은 M4.3 backlog)

- [ ] **default 결정 = AABB** (응급 우선 — 단순·빠름, 학부생 호흡 정합). Phase 01 Codex β 자문 결과가 *AABB 권장*과 일치 시 본 default 유지. *Codex가 강한 capsule 권장 + 점프 정합 비용 ↑ 사유 박음* 시에만 capsule 검토 — 단 capsule 선택 시 등급 자동 상향 (복잡 → 대규모) + scope creep 위험 ↑이라 **capsule은 M4.3 backlog로 미루기 권장** (본 Phase는 AABB 박음만)
- [ ] `98_Shared/GameData/` 또는 `02_Server/GameServer/Combat/` — `Hitbox.cs` 신설 (`AABB` struct + `Contains(Vector2)` / `Intersects(AABB)` 메서드)
- [ ] `EnemyEntity.Hitbox` (Hitbox 타입, 응급 = 1×1 unit AABB)
- [ ] `PlayerEntity.AttackHitbox` (Hitbox 타입, 응급 = attacker.Position 중심 3×3 AABB)
- [ ] `GameMap.ProcessAttack` step 5 — `dist² < range²` → `attacker.AttackHitbox.Intersects(target.Hitbox)` 교체
- [ ] `CombatConstants.AttackRange` 자체는 *남김* — Hitbox 박스 크기 입력으로 활용

### 6단계: 단위 테스트 박음

- [ ] `02_Server/GameServer.Tests/Combat/LagCompensationTests.cs` 신설 (5건+)
  1. `Rewind_HappyPath` — attacker tick 2 전 위치로 rewind 후 hit 정합
  2. `Rewind_OutOfRange_4Tick` — 4 tick 안 → rewind 작동
  3. `Rewind_BeyondRange_SilentDrop` — 5 tick 전 → silent drop
  4. `Rewind_NegativeTick_SilentDrop` — 음수 attackerClientTick → silent drop
  5. `Rewind_FutureTick_SilentDrop` — currentServerTick보다 큰 attackerClientTick → silent drop
- [ ] `02_Server/GameServer.Tests/Combat/HitboxTests.cs` 신설 (3건+)
  1. `AABB_Intersects_HappyPath`
  2. `AABB_NoIntersect_OutOfRange`
  3. `AABB_EdgeContact` — 경계값 정합
- [ ] dotnet test green (M3 baseline 회귀 0 + 8건+ 추가)

### 7단계: 부하 봇 lag 환경 검증

- [ ] `99_Tools/headless-bot/` — `--simulated-latency 200` 옵션 추가 (또는 기존 정합 활용)
- [ ] 봇 lag 200ms 환경에서 enemy hp 30 → 0 (3 hit) 정합 검증
- [ ] 봇 lag 250ms (범위 밖) 환경에서 silent drop 확인 (cheat 차단 정합)

---

## ✅ 완료 조건

- [ ] `PlayerEntity` ring buffer 4 tick 깊이 박힘 + `RecordPosition` / `GetPositionAtTick` 메서드
- [ ] `C_Attack.attackerClientTick` 필드 PDL append-only 박힘 + ProtocolVersion 4 bump
- [ ] `GameMap.ProcessAttack` lag compensation rewind 박힘 + 범위 검증 (≤ 4 tick) silent drop
- [ ] precision hitbox = AABB 박힘 (`Hitbox.cs` + `Intersects` 메서드) + `dist²` 패턴 교체
- [ ] 단위 테스트 8건+ 통과 (LagCompensationTests 5건 + HitboxTests 3건)
- [ ] 부하 봇 lag 200ms 환경에서 hit 일관성 검증 + lag 250ms 환경에서 silent drop 검증
- [ ] 본 Phase 복잡 등급 = **-DONE.md 박음** (요약 + 사실 박제 + 학습 키워드)
- [ ] reviewer SubAgent Tier 2-A 자동 호출 통과 (5축 점검 + trust-boundary 위험 깃발 검증)
- [ ] CHANGELOG entry 박음 ([M] — PDL 변경 + ProtocolVersion bump + 모든 팀원 클라 빌드 영향)

---

## 🧪 테스트

**자동**:
- `LagCompensationTests` 5건 + `HitboxTests` 3건 + 기존 `AttackHandlerTests` / `BossStageClearTests` 회귀 0

**수동**:
- 헤드리스 봇 lag 시뮬 (`--simulated-latency 200`) → 3 hit kill 정합
- 헤드리스 봇 lag 250ms → silent drop 정합
- Unity 클라 정상 환경에서 attack flow smoke (정유현 협업 또는 본인 단독)
- `unity-bridge` SubAgent batchmode compile green

---

## 📚 학습 포인트

- **lag compensation 본질** — "공정성 vs 권위 trade-off". 발쟁자 화면에선 적이 있었지만 서버는 이미 옮겨졌을 때, *공격 시점으로 rewind*. Source/Quake/Mirror/NGO 모두 같은 패턴 (한국 게임 회사 백엔드 어필 키워드).
- **rewind 범위 제한 = cheat 차단의 핵심** — 4 tick (200ms) 안만 허용. 더 옛 시점은 cheat (또는 정상 lag 초과) — silent drop. 헌법 #3 (Trust Boundary) 정합.
- **AABB vs capsule trade-off** — AABB = 축 정렬 박스, 단순·빠름 (계산 ~5 비교). capsule = 점프·회전 정합, 비용 ↑ (계산 ~20 비교). 학부생 호흡 = AABB 첫 도입, capsule은 본 마감 전 별 Phase.
- **PDL append-only + ProtocolVersion bump** — 필드 추가는 backward compatible (옛 클라가 새 필드 모르고 default 0 박힘)이지만 옛 빌드 호환성 위해 bump. 헌법 #2 가짜 약속 1번째 봉합(M3 Phase 02) 학습 정합 패턴.
- **ring buffer 정합** — fixed-size 배열 + head index 회전 = 메모리 일정 + GC 부담 X. 게임 엔진 표준 패턴 (NGO `NetworkTransform`, Mirror `SnapshotInterpolator` 정합).

---

## ⚠️ 함정 / 주의사항

- **rewind 범위 검증 누락 함정 (트라우마)** — 5 tick 전 attackerClientTick 들어왔는데 silent drop 없으면 cheat 무한 rewind. 단위 테스트 3·4·5번이 검증.
- **target도 rewind 함정** — 응급은 attacker만 rewind, target은 현재 위치. 둘 다 rewind은 M4.3 backlog (보스 AI 도입 후 가치 ↑). 본 Phase에서 둘 다 시도 시 scope creep.
- **clientTick != serverTick 함정** — 클라가 보낸 `attackerClientTick`은 클라 입장 시점. 서버는 *수신 시점 serverTick - rewind 깊이* 계산. 응급 = 클라가 자기 lastReceivedServerTick 박음 → 서버는 그 tick으로 rewind. 헌법 #3 정합.
- **ProtocolVersion 정합 누락 함정 (학습 정합)** — M3.6 Phase 04 학습 = 98_Shared/CLAUDE.md "Current=N" 박힌 stale 봉합 정합. 본 Phase 4 bump 시 동시 정정 의무.
- **Shared.dll commit 누락 함정 (트라우마)** — CHANGELOG 2026-05-17 학습. PDL 변경 시 PacketGenerator 재생성 + Shared.dll commit + Unity 측 자동 복사 확인 3종 의무.
- **trust-boundary 위험 깃발** — 본 Phase frontmatter `risk: trust-boundary` 박힘. risk-detector Hook 자동 검출 + reviewer SubAgent 5축 점검 의무. 검증 통과 전 commit 게이트.

---

## ➡️ 다음 Phase

- **M4.1 마감 의례** — Phase 03 -DONE.md 박은 후 M4.1-마감 별 -DONE.md 박음 (복잡 등급) + false-promise 점검 결과 섹션 (ADR-024 cadence 첫 시범) + CHANGELOG [M] + CONTEXT.md "⏸️ 현재 멈춤 지점" = M4.2 진입 대기
- **M4.2 — Map Transition** 진입 (캡스톤 1 후반 = 6/3~6/10)

---

## 📋 박제 (완료 후)

- 복잡 등급 = **-DONE.md 박음** (요약 + 사실 박제 + 학습 키워드 후보)
- 5단계 보고 X (대규모 등급만)
- HTML X (대규모 등급만)
- trust-boundary 위험 깃발 = reviewer Tier 2-A 자동 호출 의무 (코드 변경 + 위험 깃발)

---

## 작업 로그

- 2026-05-22: Phase 정의 박힘 (M4.1 plan 박는 시점)
