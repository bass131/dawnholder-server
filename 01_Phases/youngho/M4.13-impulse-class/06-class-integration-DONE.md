---
owner: youngho
milestone: M4.13
phase: 06-class-integration
summary: 임펄스 클래스 통일 완성 — 넉백은 server-reactive라 forceAdopt 영구 채택(안 B), 대쉬/lunge는 P5에서 이미 예측 기계 탑승. 코드 거동 0 변경(명문화 주석 3곳)으로 마일스톤 마감.
title: 클래스 통합 + 전체 회귀 — 임펄스 통일 서사 완성
status: done
grade: 복잡
slug: 06-class-integration
created: 2026-06-13
completed: 2026-06-14
prior_phases: [01-action-input-gate, 02-server-impulse-model, 03-dash-behavior, 04-shared-extract, 05-client-prediction-b]
---

# M4.13 Phase 06 — 클래스 통합 + 전체 회귀 — ✅ DONE

> 복잡 등급. 브랜치 `feature/m4.13-shared-extract`, P5 위 명문화 commit 1개.
> **착수 후 실측으로 작업 범위가 재정의됨** — lunge·대쉬는 P5에서 이미 통일 완료, 남은 건 넉백 하나의 처리 방침 결정(영호 안 B). 마일스톤 전체 5단계 보고는 `_milestone-DONE.{md,html}`.

---

## TL;DR

마일스톤의 본래 목적("클라가 예측 못 하는 서버 임펄스 동작 클래스를 통일")은 P6 착수 시점에 이미 **서버측 완성**(`EnterAttackState`/`EnterHitState` → `DecayImpulse` 단일 경로, P2·P4) + **클라 lunge·대쉬 예측 탑승**(P5) 상태였다. 남은 건 넉백 하나 — 영호가 **안 B**(넉백은 server-reactive 임펄스라 클라가 시작점을 몰라 `forceAdopt` 채택이 표준)를 결정. 코드 거동 0 변경(명문화 주석 3곳)으로 통일 서사를 닫고 전체 회귀(WSL2 569/0 + EditMode 122/0)로 마감.

---

## AC 검증 결과

| 완료 조건 (정의서 대조) | 결과 |
|---|---|
| 넉백·임펄스공격이 대쉬와 같은 임펄스 기계 경유 (grep 분기 통일) | ✅ 서버 `AttackState.Tick`+`HitState.Tick`이 `player.DecayImpulse()`(=`Physics.DecayImpulse`) 단일 헬퍼 |
| 넉백/lunge forceAdopt 빈도 | ✅ lunge=0(P5 예측 탑승) / 넉백=forceAdopt 표준(안 B, server-reactive) |
| 전체 회귀 green (WSL2 561 비감소) | ✅ build 0 err + test **569/0** |
| EditMode 전체 green | ✅ **122/0** (state=Passed) |
| Unity 콘솔 error 0 | ✅ 0 (error CS 필터) |
| 봇 회귀 | ✅ 서버 거동 무변경 → P5 검증(DashSmoke/BossFight 등) 유효 — 재실행 불요 |
| 2클라 Play 종합 | ✅ 안 B는 거동 무변경 → P5 Play 검증(대쉬/lunge/원격모션) 유효 |
| 마일스톤 `_milestone-DONE.md` 박제 | ✅ `_milestone-DONE.{md,html}` |

검증 명령: WSL2 `dotnet test Dawnholder.slnx --no-build` → `Failed: 0, Passed: 569, Skipped: 4, Total: 573`. EditMode = TestRunnerApi `[EDITMODE-DONE] passed=122 failed=0 state=Passed`. Unity ReadConsole `error CS` 필터 = 0건.

---

## 결정 흐름

1. **실측이 작업을 재정의**: P6 착수 시 "넉백·lunge에 같은 기계 적용"이 정의서 골격이었으나, 실측 결과 lunge는 P5 `NotifyAttack`에서 이미 `QueueImpulse`→`StartImpulse` 탑승 완료, 서버는 P2·P4에서 단일 경로 완성. **남은 건 넉백 하나**.
2. **넉백 방향 재평가**: P5 추적 약속("S_EnemyAttack에 방향 없어 패킷 변경 필요")을 다시 보니, 클라가 이미 attacker 위치를 알아(`EnemyAttackHandler`가 피격 이펙트 방향을 `attacker.x >= player.x ? 1 : -1`로 계산 中) **패킷 추가 없이 방향 추론 가능** — v14 bump 불필요로 정정.
3. **안 A vs 안 B (영호 결정 B)**: 넉백은 server-reactive(피격 신호 RTT 후 도착 → 시작 틱 정렬 불가 + 방향 추론 근사 + hitstun 지속 서버 전용)라 예측 이득이 작고 방향추론 시각버그(대쉬 facing 클러스터 동류) 위험. 반면 `forceAdopt`는 서버 권위 100%·위험 0·현재 잘 동작. → **넉백 forceAdopt 영구 채택**. 통일 서사 = "self-initiated(대쉬/lunge)=예측 / server-reactive(넉백)=채택, '예측이냐 채택이냐'는 클라가 시작점을 아느냐의 원리".
4. **추적 약속 해소**: reviewer 🟡 "넉백 예측 시 Hit 분기 재검토" = 별도 Phase 불필요, 영구 forceAdopt 결정으로 종결.

---

## 학습 일지 후보 키워드

- **착수 후 실측이 Phase 범위를 재정의** — 정의서 골격("넉백·lunge 통일")이 선행 Phase(P5) 진행으로 이미 대부분 충족 → 실측 없이 정의서대로 갔으면 중복 작업. "plan은 현재 코드 실측 먼저" 정합.
- **`impulse-class-prediction-boundary`** — 임펄스 예측 가능성의 경계 = *클라가 시작점(틱·방향·지속)을 아느냐*. self-initiated(대쉬/lunge)는 예측, server-reactive(넉백)는 채택(forceAdopt)이 원리적 정석. 서버 단일 경로(`ExternalImpulseVx`+`DecayImpulse`) 위에서 클라만 갈린다.
- **패킷 추가 전 추론 가능성 점검** — "방향 정보 없어 wire 변경 필요"가 항상 참은 아님. 클라가 이미 가진 정보(attacker 위치)로 추론 가능하면 §2 bump 회피.

---

> Phase 06 완료 = **M4.13 마일스톤 마감**. 마일스톤 전체 5단계 보고 = `_milestone-DONE.{md,html}`.
