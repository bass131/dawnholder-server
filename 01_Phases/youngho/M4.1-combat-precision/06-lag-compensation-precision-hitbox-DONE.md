---
owner: youngho
milestone: M4.1
phase: 06
title: lag compensation 200ms rewind + AABB hitbox + B1/B3 sweep (P1)
status: done
grade: 복잡
risk: trust-boundary
summary: M3 응급 박힌 `dist² < range²` hitbox + lag compensation 부재 결함을 정밀화로 승격. PlayerEntity position ring buffer(4 tick=200ms) + C_Attack.attackerClientTick PDL append-only(ProtocolVersion 4→5) + GameMap.ProcessAttack rewind(음수/미래/4tick초과 silent drop) + AABB hitbox(dist² 교체) + 클라 B1 sweep(TargetingRangeSquared 분리) + B3 sweep(ProtoVer 문서 v5 정정). 신규 단위 테스트 8건(LagComp 5 + Hitbox 3) + 통합 테스트 + 봇 lag 시뮬. 빌드 0/0, 회귀 221통과/0실패/3skip. reviewer Tier 2-A 5축 PASS + race audit 통과 + trust-boundary commit 게이트 통과. HTML 학습 문서(LagComp_Report/ 8페이지) 동반.
---

# Phase 06 — DONE

**완료 일자**: 2026-05-24
**소요**: ~1시간 (shared ~2.5분 / server ~15분 / client ~1.7분 / qa ~19분 / reviewer Tier 2-A ~2분 / HTML 학습문서 ~8.6분 / 의논·검증 잔여)

---

## TL;DR

200ms 인터넷 지연 환경에서도 **공정한 hit 판정 + cheat 차단 양립**을 달성. 핵심 = **클라는 *위치*가 아니라 *시점 번호*(attackerClientTick)만 보내고, 서버가 자기 ring buffer에서 그 시점의 attacker 위치를 꺼내 rewind**. 클라가 좌표를 보냈다면 텔레포트 핵(헌법 #1 위반)이 됐을 것 — 시점 인덱스만 신뢰하는 설계가 상용 FPS(Valve lag compensation) 정석 패턴. 그 시점 번호조차 untrusted라 음수/미래/200ms초과 3분기로 fail-closed silent drop(헌법 #3). hitbox는 옛 원형 `dist² < 9.0` → AABB(attacker 3×3 ∩ 적 1×1) 박스 교차로 교체. **reviewer Tier 2-A 5축 전부 PASS + 🔴 blocker 0건**.

---

## 📦 산출물 분담 (메인 세션 직접 오케스트레이션 → 4 도메인 직렬 + reviewer)

dependency 사슬(shared→server→client→qa)이라 순차 위임. 빌드 락 충돌 방지 + 통합 검증을 메인 세션이 최종 박음.

### shared SubAgent (2단계 — 프로토콜 기반, 먼저)
- `99_Tools/PacketGenerator/PDL.xml` — `C_Attack`에 `int attackerClientTick` append-only (wire 위치 2)
- PacketGenerator 재생성 → `98_Shared/Protocol/Generated/GenPackets.cs` 갱신
- `dotnet build` → `Shared.dll` 재빌드 + `03_Client/Assets/Plugins/Shared/` 자동 복사 (ADR-002 후속 3종 의무)
- `98_Shared/Protocol/ProtocolVersion.cs` Current **4→5** + v5 이력 주석
- **B3 sweep**: `98_Shared/CLAUDE.md` + `00_Document/ARCHITECTURE.md` ProtocolVersion stale → v5 정정 (실재 stale만)

### server SubAgent (1·4·5·6단계 — 핵심 로직)
- **1단계 ring buffer**: `PlayerEntity` — `Vector2[4] _posHistory` + `long[4] _posHistoryTick` + head index. `RecordPosition(serverTick, pos)`(Tick의 Physics.Step 직후 호출) / `GetPositionAtTick(serverTick)`(못 찾으면 현재 Position fallback)
- **4단계 rewind**: `GameMap._currentTick` 필드(Tick 맨 앞 갱신) + `ProcessAttack(attackerEntityId, targetEntityId, attackerClientTick)` 시그니처 확장 + rewind 검증 3분기(음수<0 / 미래>_currentTick / 초과>4) silent drop. `SubmitAttack`/`AttackHandler` wiring 동반
- **5단계 AABB**: `02_Server/GameServer/Combat/Hitbox.cs` 신설 (`readonly struct AABB` + `Contains`/`Intersects`). 적 1×1 / attacker 3×3(halfExtent 1.5, `CombatConstants.AttackHalfExtent`). `dist² < AttackRangeSquared` → `GetAttackHitbox(rewindedPos).Intersects(target.Hitbox)` 교체. `AttackRangeSquared`는 박스 크기 산출 참고용 보존
- **6단계 테스트**: `LagCompensationTests.cs` 5건 + `HitboxTests.cs` 3건 신설
- **회귀 봉합**: `AttackHandlerTests` + `BossStageClearTests`가 `attackerClientTick = tick`(zero-lag, diff=0) 넘기도록 갱신

### client SubAgent (3단계)
- `UnityClientSession.cs` — `LastReceivedServerTick` 프로퍼티 신설 + `HandleSnapshot`에서 갱신(본인/타인 구분 전 먼저)
- `LocalPlayerController.cs` — `C_Attack.attackerClientTick = session.LastReceivedServerTick` 박음
- **B1 sweep**: `const float AttackRangeSq = 9.0f` → `TargetingRangeSquared` 명명 + 주석(클라 타게팅 힌트, 서버 AABB 권위 판정과 의도적 분리 = 헌법 #4 정합)

### qa SubAgent (7단계)
- **봇 회귀 봉합**: `EmergencyCombatSmoke.cs`/`BossStageClearSmoke.cs` — `volatile int _lastReceivedServerTick`(S_Snapshot 추적) + `SendAttack`에 `attackerClientTick` 박음 + `WaitForFirstSnapshot` 헬퍼
- `--simulated-latency <ms>` 옵션: `attackerClientTick = max(0, lastServerTick - ms/50)` (지연 tick 시뮬)
- `LagSimIntegrationTests.cs` 신설(zero-lag 자동 2건 + lag 100/250ms LongRunning Skip 2건) + `M2BasicMovementIntegrationTests` collection fixture(GameWorld 싱글톤 위반 방지)

### reviewer SubAgent (Tier 2-A, trust-boundary 필수)
- 5축(헌법/ADR/ARCHITECTURE/테스트/도메인) 전부 PASS + race audit (a)(b)(c) 통과 + trust-boundary commit 게이트 통과. 🔴 blocker 0 / 🟡 권고 0

### 메인 세션 직접 (HTML 학습 문서)
- `LagComp_Report/` 8페이지(index + 7섹션) + `style.css` + 인라인 SVG 9개. 외부 의존성 0, file:// 동작. 사용자 요청 산출물(캡스톤 평가 자산)

---

## AC 검증 결과

| AC | 결과 | 검증 |
|---|---|---|
| `PlayerEntity` ring buffer 4 tick + RecordPosition/GetPositionAtTick | ✅ | PlayerEntity.cs (parallel 배열 + head 회전) |
| `C_Attack.attackerClientTick` PDL append-only + ProtocolVersion 4→5 | ✅ | PDL.xml + GenPackets.cs(wire 위치 2) + ProtocolVersion.cs `Current=5` |
| `GameMap.ProcessAttack` rewind + 범위 검증(≤4tick) silent drop | ✅ | GameMap.cs 3분기(음수/미래/초과) + `_currentTick` |
| precision hitbox = AABB + `dist²` 교체 | ✅ | Hitbox.cs `Intersects` + ProcessAttack step 5 교체 |
| **B1 sweep** AttackRangeSq → TargetingRangeSquared | ✅ | LocalPlayerController.cs 명명+주석(서버 const 복붙 X) |
| **B3 sweep** ProtocolVersion v3→v5 문서 정정 | ✅ | 98_Shared/CLAUDE.md + ARCHITECTURE.md 실재 stale 정정 |
| 단위 테스트 8건+ 통과 | ✅ | LagComp 5 + Hitbox 3 |
| 봇 lag 200ms hit 일관성 + 250ms silent drop | ✅ | deterministic 단위(diff=4 hit/diff=5 drop) + 봇 시뮬 옵션 + zero-lag 통합. 종단간 200/250은 timing race window라 Skip(reviewer 충분 판정) |
| 빌드 green (경고 0 오류 0) | ✅ | Dawnholder.slnx |
| 회귀 0 (221통과/0실패/3skip) | ✅ | dotnet test 02_Server.Tests 59s |
| Shared.dll 재빌드 자동 복사 | ✅ | `M 03_Client/Assets/Plugins/Shared/Shared.dll` |
| reviewer Tier 2-A 통과 + race audit | ✅ | 5축 PASS + (a)(b)(c) 통과 + trust-boundary 게이트 통과 |
| -DONE.md 박음 (복잡 등급) | ✅ | 본 파일 |

---

## 결정 흐름

1. **등급 정정 (대규모 → 복잡)** — 진입 시 work-pin "다음 액션"엔 대규모로 적혀있었으나 Phase 정의 frontmatter는 `grade: 복잡`. plan-auditor가 trust-boundary+irreversible 위험 깃발까지 흡수해 산정한 결과 = 복잡 확정. -DONE.md 박음(5단계 보고/HTML 의무는 대규모만 → 면제. 단 HTML은 *사용자 요청*으로 별도 박음).
2. **진행 방식 = 전체 위임 + HTML 학습 문서** — 사용자 가닥. 단계별 호흡(학습 깊이↑/속도↓) 대신 전체 위임(속도↑) 후 잘 만든 HTML로 학습 깊이를 *지연 회수*. 캡스톤 포트폴리오 자산 양립.
3. **ring buffer 슬롯에 tick 동반 저장** — 위치만 박으면 슬롯 재사용 시 "이 슬롯이 몇 번 tick 것인지" 구분 불가. `long[4] _posHistoryTick` 병행 배열로 정확한 lookup + future/stale 슬롯 구분.
4. **clientTick = 시점 인덱스만 (위치 X)** — 헌법 #1/#3 핵심. 클라가 "공격 당시 내 좌표"를 보냈다면 좌표 조작 핵. 대신 "몇 tick 전이었나"만 받고 위치는 서버 ring buffer에서. reviewer 칭찬 = Valve lag compensation 정석.
5. **rewind vs Physics 시점 정합** — ProcessAttack은 tick N의 job 단계(맨 앞), RecordPosition은 같은 tick의 Physics 직후(뒤) → tick N 시점엔 버퍼에 N-1까지만. clientTick은 항상 과거 S_Snapshot tick(≤N-1)이라 정상 lookup. diff=0(현재tick 미기록) 케이스는 GetPositionAtTick의 현재 Position fallback이 보수적으로 흡수(cheat 이득 0). reviewer race audit (b) 통과.
6. **AABB default (capsule은 M4.3 backlog)** — AABB = 축 정렬 박스, 단순·빠름(~5 비교). capsule = 점프/회전 정밀, 비용↑(~20). 학부생 호흡 = AABB 먼저, capsule 미룸. capsule 선택 시 등급 자동 상향 + scope creep 위험.
7. **봇 종단간 lag 테스트 Skip 판정** — 실시간 서버에서 diff=5 정확 보장 불가(S_Snapshot 수신→공격 송신→서버 처리 사이 1 tick race window). deterministic 단위 테스트가 경계값(diff=4/5)을 환경 무관 검증 + zero-lag 통합이 wire 경로 회귀 방어. reviewer "실질 충족" 판정.

---

## 학습 일지 후보 키워드

### 1. `lag-comp-trust-only-tick-index` (★★★)
Lag compensation의 신뢰 경계 핵심 = **클라가 보내는 건 *위치*가 아니라 *과거 시점 번호***. 위치를 받으면 텔레포트 핵(좌표 조작) = 헌법 #1 정면 위반. 대신 "몇 tick 전이었나"만 받고 *위치는 서버가 자기 기록(ring buffer)에서 꺼냄*. 그리고 그 시점 번호조차 untrusted라서 음수/미래/너무 오래 전(>4tick=200ms)을 전부 잘라냄(fail-closed silent drop) = 헌법 #3 교과서 적용. 상용 FPS(Valve Source engine, Quake, Mirror, NGO) 공통 패턴 = **한국 게임 회사 백엔드 면접 결정타 키워드**. "공정성 vs 권위 trade-off를 *되감기*로 양립" 1줄 설명 가능해야 함.

### 2. `ring-buffer-fixed-array-gc-zero` (★★)
position history = 고정 크기 원형 배열(ring buffer) + head index 회전. 매번 new 안 함 → **GC 부담 0 + 메모리 일정**. 게임 엔진 표준(NGO NetworkTransform, Mirror SnapshotInterpolator 정합). 슬롯에 "그 슬롯이 몇 번 tick 것인지"(`long[] tick`)를 위치와 *병행* 저장해야 슬롯 재사용 시 stale 구분 가능. hot path(매 tick + 매 공격)이라 `readonly struct`(AABB) + 고정 배열 = heap 할당 0이 정합.

### 3. `new-race-dimension-not-covered-by-old` (★★)
"한 차원 race 봉합 ≠ 다른 차원 race 안전 보장"(M3.8 본인 통찰). 본 Phase = ring buffer 갱신(tick thread) + rewind lookup(tick thread) + attackerClientTick 수신(network thread→EnqueueJob dispatch)이라는 *새 race 차원* 도입. audit = (a) network thread가 ring buffer 직접 접근 X(EnqueueJob 경유만) (b) RecordPosition·GetPositionAtTick 모두 tick thread 단독 invariant → lock 불필요 (c) 봇 `volatile int`로 network write/main read 가시성. reviewer Tier 2-A에 race audit 항목 명시 호출이 안전망.

---

## false-promise 점검 결과 (ADR-024 cadence)

본 Phase에서 **B3 sweep으로 ProtocolVersion 문서 stale 정정**(=false-promise 변종 선제 봉합):
- `98_Shared/CLAUDE.md` ProtocolVersion 줄 v4 → v5 (M4.1 Phase 06 bump 반영)
- `00_Document/ARCHITECTURE.md` ProtocolVersion 줄 미래시제("4→5 bump 예정") → 완료시제("bump됐다") + `Current=4` → `Current=5`

신규 가짜 약속 발본 0건(B3 sweep이 잠재 stale 선제 정정). M4.1 누적은 Phase 05까지 7건.

---

## 작업 로그

- 2026-05-24: Phase 06 진입 — `/session:start` 게이트(main → 새 브랜치 `feature/m4.1-phase06-lag-comp-hitbox`) + 등급 복잡 확정 + 진행방식 사용자 가닥(전체 위임 + HTML) + shared→server→client→qa 직렬 위임 + reviewer Tier 2-A PASS + HTML 학습문서 8페이지 박음 + -DONE.md 박음
- 2026-05-22: Phase 정의 박힘 (M4.1 plan 박는 시점)
- 2026-05-23: M3.8 ★★★ 통찰 2건 흡수(클라 입력 컨트롤 / 새 race 차원) + M4.1 Phase 01 β 발견 흡수(B1/B3 sweep + 완료 조건 2건)

---

## ➡️ 다음 Phase

- **M4.1 마일스톤 완전 마감** — Phase 01~06 ✅ 전부 마감. M4.1 마감 별 -DONE.md(복잡) + false-promise 점검 결과 섹션(ADR-024 cadence) + CHANGELOG [M](PDL 변경 + ProtocolVersion bump = 모든 팀원 클라 빌드 영향) + Phase 06만 별도 PR(사용자 명시 GO 게이트 의무).
- **M4.2 — Map Transition** 진입 (캡스톤 1 후반 6/3~6/10).
