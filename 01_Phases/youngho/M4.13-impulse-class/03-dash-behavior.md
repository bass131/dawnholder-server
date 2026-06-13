---
owner: youngho
milestone: M4.13
phase: 03-dash-behavior
title: 대쉬 거동 재설계 — 적 밀침(허딩) + 완전 무적 (착수 전 적변위 스파이크 선결)
status: planned
grade: 복잡
slug: 03-dash-behavior
created: 2026-06-13
domains: [server]
prior_phases: [01-action-input-gate, 02-server-impulse-model]
depends_on: [02-server-impulse-model]
risk_flags: [trust-boundary]
---

# M4.13 Phase 03 — 대쉬 거동 재설계 (적 밀침 + 완전 무적)

> 계획서 = `_milestone-plan.md` Phase 분해 표 #3 + "확정 설계 — 대쉬 게임플레이"(영호 6결정 ②b·③). P2 임펄스 모델 위에서 **대쉬 고유 게임플레이**(적 밀침 허딩 + 완전 무적)를 얹는다.
> **⚠️ 착수 직전 적 변위 경로 30분 스파이크 선결**(plan-auditor 봉합 ②). "신규 충돌 시스템 필요"로 나오면 Phase 분리 후보.

---

## Context (왜)

대쉬는 메이플 러시처럼 **경로의 적을 앞으로 밀고(허딩)**, **밀고 가는 동안 완전 무적**(피격 데미지 0)이어야 한다(영호 ②b·③). 현재는 데미지만 주고 몹은 제자리, 무적은 넉백만 무시(`InterruptibleByHit=false`)할 뿐 데미지 적용 여부는 미확인. 적 밀침은 **서버 적 변위 신규 경로**라 미조사 영역 — 착수 전 스파이크로 비용을 확정해야 완료 조건이 정량화된다.

---

## ⚠️ 선결 스파이크 (착수 직전 ≈30분) — 적 변위 경로

**이 Phase 본작업 전에 반드시 수행.** 결과를 `03-...-DONE.md` 또는 본 파일에 박제 후 본작업 진입.

- [ ] **`Combat/EnemyEntity.cs`가 ExternalVelX 채널을 갖는가** — 플레이어의 ExternalVelX 합성(`GameMap.cs:244`)에 대응하는 적 측 임펄스 채널 존재 여부.
- [ ] **충돌 해석 주체** — 적-플레이어 겹침/밀림을 누가 계산하는가(CombatSystem? GameMap 틱? 신규?).
- [ ] **밀림 기댓값** — 칸수(거리), 벽 끼임(맵 경계/타일 충돌), `S_EntityState` 브로드캐스트 영향(SnapshotTickInterval=2틱).
- [ ] **판정**: 기존 채널 재사용 가능 → Phase 03 그대로. **"신규 충돌 시스템 필요"** → Phase 03을 3a(밀침)/3b(무적)로 분리 + 영호 재논의.

---

## 증거 사슬 (현재 코드 실측 — 2026-06-13, 일부 미조사)

| 링크 | 결정적 증거 (`파일:줄`) | 내용 |
|---|---|---|
| 1. 대쉬 진입 | `Maps/Systems/SkillSystem.cs:74-140`(ProcessDash) | P2에서 고정거리 등속으로 전환된 대쉬. |
| 2. 무적 현황 | `Maps/States/ActorState.cs:22`(InterruptibleByHit) · `PlayerCombatStates.cs`(DashState/HitState) | 현재 넉백만 무시 — **데미지 적용 여부 착수 시 확인**. |
| 3. 데미지 적용 경로 | `Maps/Systems/CombatSystem.cs`(ProcessAttack→데미지) | 대쉬 중 데미지 게이트 0으로 만들 지점. |
| 4. 적 변위 (미조사) | `Combat/EnemyEntity.cs` / CombatSystem 경로 | **스파이크 대상** — 적 밀침 신규. |

---

## 설계 방향 (스파이크 후 확정 — 골격)

- **적 밀침(허딩)** — 대쉬 경로상 적을 전방으로 밀기. 서버 권위 적 변위. 클라는 서버가 준 적 위치를 원격 보간으로 미러(내 캐릭터만 예측 — P5).
- **완전 무적(③)** — 대쉬 중(상태 기간) 데미지 게이트 0. `InterruptibleByHit=false`는 넉백, 추가로 **데미지 0**.
- **취소 불가(④)·전방만(⑤)** — P1 게이트가 대쉬 중 행동 거부로 자연 봉합(취소 입력 무시), 방향은 시전 시 facing 고정.

---

## 완료 조건 / 게이트 (정량)

- [ ] **선결 스파이크 결과 박제** + 판정(기존 채널 재사용 / 신규 필요).
- [ ] 대쉬 경로 적이 전방 **N칸 밀림**(서버 EditMode/봇) — 칸수·벽 끼임 동작 명세.
- [ ] 대쉬 중 **피격 데미지 0**(무적 게이트) — 서버 테스트로 데미지 0 검증.
- [ ] 취소 불가(대쉬 중 행동 입력 무시 — P1 게이트 정합) + 전방 고정.
- [ ] 회귀 green: WSL2 build+test 비감소 + 봇 + reviewer 재검증(적 변위 서버 권위·신뢰 경계).

---

## 위험 / 헌법 게이트

- **§1 서버 권위 (trust-boundary)**: 적 밀침·데미지·무적 판정 = 서버 단독. 클라는 결과 미러.
- **§3 신뢰 경계**: 적 변위가 새 입력 경로를 열지 않게 — 대쉬 발동만 클라 요청(P1 입구), 변위 계산은 서버.
- **§2 Protocol**: 적 변위가 `S_EntityState` 형상 건드리면 STOP → 영호 의논(M4.11 P1처럼 serverTick append 전례 = bump 가능성). 스파이크에서 확인.
- **⚠️ 스파이크 미수행 채 본작업 금지** — 완료 조건 정량 불가 상태로 진입하면 plan-auditor 봉합 ② 위반.

---

> Phase 완료 시 `03-...-DONE.md` 박제(복잡 등급). 게이트 통과 후 Phase 04(공유 모델 추출) — 단 P4는 P2 의존이라 P3와 병렬 가능(순서 영호).
