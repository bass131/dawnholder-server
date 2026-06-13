---
owner: youngho
milestone: M4.13
phase: 03-dash-behavior
title: 대쉬 거동 재설계 — 적 밀침(허딩) + 완전 무적 (착수 전 적변위 스파이크 선결)
status: in-progress
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

## ✅ 선결 스파이크 결과 (2026-06-13 메인 실측 — 적 변위 경로)

**스파이크 완료. 판정 = 기존 채널 재사용 가능 → 신규 충돌 시스템·wire bump 불필요 → STOP 미발동, Phase 03 그대로(3a/3b 분리 안 함).** plan의 "적 변위 = 신규" 전제는 오판 — 채널이 이미 존재.

- ✅ **적 ExternalVelX 채널 존재** — `Combat/EnemyEntity.cs:101`(`KnockbackVx`) + `EnterHitState(dirX)`(`:149`, Boss 면역) + `Maps/States/EnemyStates.cs:170-184`(`EnemyHitState.Tick`: `enemy.X += KnockbackVx*TickDuration` + 감쇠 + ε 클램프). 적은 지형 물리 없이 순수 X 적분.
- ✅ **충돌 해석** — 별도 충돌 시스템 불필요. 대쉬 시전 시 `DashAction`이 경로 적(`ResolveImpactTargets`)에 `EnterHitState(대쉬방향)`만 호출하면 기존 감쇠+적분이 변위 처리.
- ✅ **클라 전달** — `S_EntityState.x`(PDL:244, v12)가 적 위치를 이미 실어 나름(넉백이 이미 사용 중). **wire 무변경.**
- ✅ **판정**: 기존 채널 재사용 → Phase 03 그대로, §2 무손상.

---

## 증거 사슬 (현재 코드 실측 — 2026-06-13, post-P1/P2 브랜치)

> ⚠️ plan 시점(main 2433ab5) 앵커는 P1/P2로 이동(stale). 아래는 현재 브랜치 재실측.

| 링크 | 결정적 증거 (`파일:줄`) | 내용 |
|---|---|---|
| 1. 대쉬 진입 | `Maps/Actions/DashAction.cs`(P1 이관, P2 등속) — `Execute`가 경로 적에 데미지+`S_HitResult`만, **push 안 줌** | 여기에 허딩 push 추가. |
| 2. 적 변위 채널 | `Combat/EnemyEntity.cs:101`(KnockbackVx)/`:149`(EnterHitState) · `Maps/States/EnemyStates.cs:170-184`(EnemyHitState 감쇠+적분) | **재사용** — 허딩 = `EnterHitState(대쉬방향)`. |
| 3. 플레이어 데미지 적용 | `Maps/States/BossStates.cs:45-52`(`ApplyBossAttack`: `player.Hp -= damage`[:49] → `EnterHitState`[:52]) | **무적 게이트 삽입 지점.** Worker가 *모든* 플레이어 데미지 경로 grep해 빠짐없이 게이트(trust-boundary). |
| 4. 무적 현황 | `Maps/States/ActorState.cs`(InterruptibleByHit) · AttackState.InterruptibleByHit=false | 넉백/hitstun은 이미 차단 — **데미지 0 게이트만** 추가. |

---

## 확정 설계 (스파이크 후 — 2026-06-13 영호 GO)

- **적 밀침(허딩)** — `DashAction.Execute`가 경로 적(`ResolveImpactTargets`)에 `EnterHitState(대쉬 facing 방향)` 호출 → 기존 `KnockbackVx` 채널이 전방 변위 처리. 서버 권위, 클라는 `S_EntityState.x` 미러. **세기 시작 = 기존 넉백(`KnockbackInitialVx=7`, 실거리 ~1.3u)**, Play 튜닝 대상(더 센 허딩 원하면 대쉬 전용 push 상수 분리 — Worker 판단). 부수효과 = 적 짧은 hitstun(허딩에 자연스러움).
- **완전 무적(③)** — 대쉬 전용 **invuln window**(`PlayerEntity` 서버 필드, 예 `InvulnUntilTick`; `DashAction`이 `현재 tick + 대쉬 지속`으로 세팅) → 플레이어 데미지 적용 지점(§증거 3 + 전수 grep)에서 `if 무적이면 데미지 0 + 넉백 skip`. **dash≠melee 구분**: melee는 이 필드 안 건드림(평소대로 피격). wire 무변경(서버 데미지 판정만).
- **취소 불가(④)·전방만(⑤)** — P1 게이트(`AcceptsAction=false`)가 대쉬 중 행동 입력 거부로 자연 봉합, 방향은 시전 시 facing 고정.
- **wire v12 무변경** — 허딩(`S_EntityState.x` 재사용) + 무적(서버 데미지 게이트) 둘 다 패킷 형상 안 건드림.

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
