---
owner: youngho
milestone: M4.6
title: ActionState FSM — 플레이어·몬스터·보스 행동을 통일 State 패턴으로 + 서버 권위 commit window
status: planned
grade: 대규모
risk: trust-boundary
estimated: 13~19h (총합, 6 Phase)
domain: server+shared+client+qa
---

# M4.6 — ActionState FSM (행동 상태 머신 통일)

> **상태**: planned — 2026-06-08 세션24 `/work:plan` (M4.5 완전 마감 직후, FSM 리팩토링 의논 확정)
> **선행**: M4.5 완전 마감 (PR #73, main `5613cfa`)
> **배경**: 실측 결과 — 공격 중 이동 가능(`SubmitMoveIntent()`에 공격 체크 0줄), 적 AI=enum+switch 2상태, 보스=필드 분기. 실측 자료 = [`_fsm-design-discussion.html`](_fsm-design-discussion.html)

---

## 🎯 마일스톤 목표

플레이어·몬스터·보스의 **서버 권위 행동**을 **하나의 통일된 State 패턴**(추상 베이스 + 상태별 클래스)으로 리팩토링한다. 핵심 게임플레이 규칙으로 **"공격은 끝까지 커밋 → 공격 중 이동 잠금(commit window)"**을 서버에 도입하고(현재 부재), 클라 Animator는 그 규칙을 따라가는 **거울(미러)**로 정합한다.

**설계 결정 (세션24 의논 확정)**:
- **종착점 = C(완벽한 통일 구조), 길 = A(수직 슬라이스)**: 종착 그림은 셋 다 통일된 State 구조. 단 *구현*은 플레이어 → 몬스터 → 보스 순차로 가며 각 단계가 검증 체크포인트. → **설익은 추상화 회피**: 추상 베이스를 셋 동시에 추측하지 않고 플레이어로 실증 후 일반화.
- **"Exit Time"의 진짜 정체 = 서버 규칙**: 공격 commit window는 게임플레이 규칙(헌법 #1) → 서버가 진실의 원천. 클라 Animator Exit Time은 시각 거울일 뿐, 권위 아님.
- **commit window 지속 = 98_Shared 단일 상수**: 클라 예측이 같은 규칙으로 이동을 게이트해야 reconcile rubber-band가 안 생김 (carry-over 학습 — "서버 상수 단일 진실 + 클라 역산").
- **유지할 경계**: AnimState(시각, `98_Shared/GameData/AnimState.cs`) ↔ EnemyState(서버 AI)의 분리는 리팩토링 후에도 유지.
- **ProtocolVersion 9 불변 목표**: commit window는 서버 내부 규칙 + 기존 reconcile로 전달 → 신규 패킷 0. (플레이어 HP 동기화 + 공격 이벤트 패킷 = v10 구조급은 본 마일스톤 밖, 별도)

---

## 📋 Phase 분해 (6개)

| # | Phase | 등급 | 도메인 | 예상 | risk |
|---|---|---|---|---|---|
| 01 | State 머신 골격 + 플레이어 이동 상태(Idle/Move/Jump) 이주 — **행동 불변** | 복잡 | server | 2~3h | — |
| 02 | 플레이어 전투 상태(Attack/Hit/Death) + **commit window 규칙** + 공유 상수 | 복잡 | server+shared | 3~4h | **trust-boundary** |
| 03 | 클라 미러 정합 — 예측 게이트(reconcile rubber-band 방지) + 시각 상태 검증 | 복잡 | client | 2~3h | — |
| 04 | 몬스터 AI(Patrol/Chase/Attack)를 검증된 State 베이스로 확장 | 복잡 | server | 2~3h | — |
| 05 | 보스를 명시적 State(Idle/Telegraph/Attack/Phase전환)로 정리 + P2 telegraph 상수 | 복잡 | server+shared | 3~4h | — |
| 06 | 회귀 + 마감 (cross-review + 봇 시나리오 + Play 실측 + PR + 5단계 보고) | 보통 | qa | 1~2h | irreversible(PR) |

**총 등급 = 대규모** (서버 3 도메인 관통 + 신뢰 경계 이동 게이트 + 클라 미러).

**선택 동승 (Phase 01)**: CI `actions/*@v5` bump (6/16 마감 임박) — 작은 첫 Phase에 가볍게 태울지 Phase 01 진입 시 판단. 무관 영역이라 분리해도 무방.

---

## 🔗 의존성 그래프

```
01 (골격 + 이동 상태, 행동 불변)
   ↓
02 (전투 상태 + commit window + 공유 상수)   [server+shared, trust-boundary]
   ↓
   ├──────────────┬───────────────┐
   ↓              ↓               
03 (클라 미러)   04 (몬스터 AI State)     ※ 03 ↔ 04 병렬 가능 (client vs server 도메인, 둘 다 02 이후)
                   ↓
                 05 (보스 State + telegraph 상수)
                   ↓
                 06 (회귀 + 마감)  ← 03·05 모두 필요
```

**병렬 가능**: Phase 03(클라) ↔ Phase 04(서버 몬스터) — 02 완료 후 도메인이 갈려 의존성 0. **plan-auditor 흡수: Phase 04에서 몬스터 commit window를 제외(순수 구조 이주만)했으므로 두 Phase가 98_Shared 상수에서 충돌하지 않아 병렬이 완전히 깨끗함.** 단 학습 호흡상 직렬 진행 권장, 일정 압박 시 병렬 옵션.

---

## ✅ 마일스톤 완료 조건

- [ ] 플레이어·몬스터·보스 행동이 **공통 추상 State 베이스 + 상태별 클래스**로 구동 — 서버 행동 분기 if/switch 더미 은퇴
- [ ] **공격 중 이동 잠금**이 서버에서 강제됨 — 조작된 이동 입력으로 우회 불가(테스트로 입증)
- [ ] commit window 지속이 **98_Shared 단일 상수** — 클라 예측이 같은 값으로 게이트 → 공격 시 reconcile rubber-band 0
- [ ] 몬스터 AI(Patrol/Chase)가 동일 State 베이스 위에서 동작 — 기존 행동(aggro 히스테리시스/순찰 경계) 회귀 없음
- [ ] 보스가 명시적 State로 동작 — 페이즈 1/2 + telegraph + 쿨다운 회귀 없음, P2 telegraph 상수가 단일 출처
- [ ] AnimState(시각) ↔ EnemyState(AI) 경계 유지 — 클라는 여전히 AI 상태 모름
- [ ] **ProtocolVersion == 9 불변** (신규 패킷 0)
- [ ] `dotnet test` green + 봇 전 시나리오 PASS (FSM 회귀 단언 포함)
- [ ] Play 실측 — 공격 중 멈춤 체감 + 몬스터/보스 전투 정상 (직업 2종 × 세 씬)
- [ ] CHANGELOG + PR 머지 (사용자 GO) + 5단계 보고 MD/HTML

---

## 🚫 이번에 명시적으로 뺀 것 (세션24 의논 확정)

- **외관/연출** — 배경 애니메이션 UI / Town 진입 컷신 / NPC 배치·스토리텔링 = 나중 외관 마일스톤 (주차장)
- **플레이어 HP 동기화 + 공격 이벤트 패킷 (v10 구조급)** — 원격 Ranger 투사체의 뿌리, 별도 묶음. 본 마일스톤은 v9 불변
- **적 중력 부재 / 하향 점프 등 지형 v2** — M4.4 이월 유지
- **신규 행동/스킬 콘텐츠** — 본 마일스톤은 *기존 행동의 구조 통일 + commit window 규칙*만. 새 상태(예: 방어/구르기)는 통일된 베이스가 굳은 뒤 별도

---

## ➡️ 다음 마일스톤

- **외관/연출 디테일** (배경/컷신/NPC) 또는 **v10 구조급**(HP 동기화 + 공격 이벤트 패킷) — 사용자 가닥
- **M5 Persistence** 는 그 다음 (LocalDB Linux 결정 + GenPackets Write 풀링 + Serilog/DI)

---

## 갱신 이력

- 2026-06-08 — 신설 (세션24 `/work:plan` — M4.5 마감 직후. FSM 리팩토링 방향 = "C 목표 + A 길" 사용자 확정. 실측 자료 `_fsm-design-discussion.html` 기반)
