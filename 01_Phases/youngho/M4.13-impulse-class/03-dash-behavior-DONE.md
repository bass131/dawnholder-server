---
owner: youngho
milestone: M4.13
phase: 03-dash-behavior
title: 대쉬 거동 재설계 — 적 밀침(허딩) + 완전 무적
status: done
completed: 2026-06-13
grade: 복잡
summary: M4.13 P3 완료. 대쉬가 경로 적을 전방으로 밀고(허딩) 대쉬 중 완전 무적(데미지0)이 되도록. 허딩 = DashAction이 생존 적에 EnterHitState(대쉬 facing) 호출 → 기존 EnemyEntity.KnockbackVx 채널(EnemyHitState X 적분+감쇠) 재사용, Boss는 latch-only 면역. 무적 = PlayerEntity.InvulnUntilTick(기본 long.MinValue) + IsInvulnerable(tick), DashAction이 CurrentTick+DashTravelTicks로 세팅, BossStates.ApplyBossAttack(유일 플레이어 데미지 경로 — 전수 grep)이 데미지 전 게이트. 착수 전 스파이크로 "적 변위 = 신규 시스템" plan 전제가 오판(채널 이미 존재)임을 정정 → 신규 충돌시스템·wire bump 불필요, STOP 미발동. wire v12 무변경, 98_Shared write 0. 선택적 Opus B 첫 적용(구현 Worker=Opus). 검증 = reviewer 🔴0(신뢰 경계 전수성 명시논증) + WSL2 568/0(P2 562 비감소) + 봇 DashSmoke PASS.
---

# Phase 03 박제: 대쉬 거동 재설계 (적 밀침 + 완전 무적)

**소요**: 착수 전 스파이크(메인 실측 — 적 변위 채널 존재 확인, STOP 미발동) → P3-server 구현(**Opus Worker, 선택적 Opus B 첫 적용**) → 메인 diff 실측(무적 게이트 전수성 독립 grep) → reviewer 🔴0(🟡2) → WSL2 568/0 + 봇 DashSmoke → 🟡1 주석 정정. commit `860f5a2`(코드).

## TL;DR

대쉬를 메이플 러시처럼 **경로의 적을 앞으로 밀고(허딩)**, **미는 동안 완전 무적**(피격 데미지 0)으로 만들었다(영호 6결정 ②b·③). 핵심은 **둘 다 신규 시스템 없이 기존 인프라 재사용**:
- **허딩** — 적 변위 채널이 *이미 있었다*(`EnemyEntity.KnockbackVx` + `EnemyHitState`가 매 틱 `X += KnockbackVx·dt` 적분 + 감쇠). `DashAction`이 경로 생존 적에 `EnterHitState(대쉬 facing)` 한 줄 호출하면 그 채널이 전방 변위 처리. Boss는 `EnterHitState`가 latch-only(`EnemyEntity.cs:152` early-return)라 자동 면역.
- **무적** — `AttackState.InterruptibleByHit=false`가 넉백·hitstun은 이미 막으니 **데미지 0 게이트만** 추가. 대쉬 전용 `InvulnUntilTick`(서버 필드) + 데미지 적용 지점 게이트.

**plan 전제 정정**: plan은 "적 밀침 = 서버 적 변위 신규"로 STOP 위험을 우려했으나, 착수 전 스파이크(메인 file:line 실측)로 채널이 이미 존재함을 확인 → 신규 충돌시스템·wire bump 불필요, 3a/3b 분리 안 함, STOP 미발동.

## 박제 사실 (어떻게)

| 영역 | 산출 |
|---|---|
| 허딩 | `DashAction.Execute`: 경로 적 중 *생존* 적에 `target.EnterHitState(caster.FacingDir)` 호출(죽는 적은 기존 `HandleEnemyDeath`). 기존 `KnockbackVx` 채널 재사용 = 신규 충돌시스템 0. 세기 시작 = `KnockbackInitialVx`(~1.3u, Play 튜닝). Boss latch-only 면역 |
| 무적 필드 | `PlayerEntity.InvulnUntilTick`(long, 기본 `long.MinValue` = 비무적) + `IsInvulnerable(currentTick) => currentTick <= InvulnUntilTick`. `DashAction`이 `map.CurrentTick + DashTravelTicks`로 세팅 |
| 무적 게이트 | `BossStates.ApplyBossAttack`: AABB intersect 직후·데미지 전 `if (player.IsInvulnerable(map.CurrentTick)) continue` → 데미지·`Hp -=`·`EnterHitState`(넉백)·broadcast 전부 skip |
| dash≠melee | `InvulnUntilTick`은 `DashAction`만 세팅. `MeleeAction`은 미세팅(기본값 비무적) → melee는 평소대로 피격. 구분의 단일 지점 |
| wire | **v12 무변경** — 허딩은 `S_EntityState.x`(PDL:244) 재사용, 무적은 순수 서버 데미지 판정. 98_Shared write 0 |

## AC 검증 결과

- **착수 전 스파이크 (메인 실측)**: 적 변위 채널 존재 확인 — `EnemyEntity.KnockbackVx`(:101)/`EnterHitState`(:149)/`EnemyHitState.Tick`(`EnemyStates.cs:170-184`) + `S_EntityState.x`(PDL:244, v12). 판정 = 기존 채널 재사용 → 신규 충돌시스템·wire bump 불필요, STOP 미발동.
- **메인 diff 실측 (무적 게이트 전수성, ★trust-boundary)**: `02_Server` 전체 `\.Hp -=` / `EnterHitState` 독립 grep — 플레이어 데미지 경로는 `BossStates.cs:54`(+`:57` EnterHitState) **단 하나**. 나머지(`DashAction`/`MeleeAction`/`DeferredDamageSystem`)는 전부 `target`=적. → 게이트 1곳으로 무적 완성, 구멍 0.
- **reviewer (Tier 2-A, trust-boundary)**: 🔴0 / 🟡2 / 통과. 신뢰 경계 전수성 명시 논증으로 확정(`DeferredDamageSystem.target`=`EnemyEntity` — 보스/적→플레이어 지연 데미지 경로 없음 확인). tick window gap 0(over-coverage +1틱 = 안전 방향). 🟡2 = ①invuln window 주석 부정확(정정 완료) ②DashAction→무적→보스 end-to-end 통합 테스트(후속 후보).
- **WSL2 회귀 (ADR-029)**: build 0/0 + `dotnet test` **568 / Failed 0 / Skipped 4**. P2 baseline 562 → +6(P3 허딩·무적·Boss면역 테스트) 비감소.
- **봇 DashSmoke (Release, fresh)**: exit 0 (PASS) — 기존 대쉬 거동(skillCast/path hit/cooldown/class gate) 회귀 0. 허딩·무적은 단위 테스트가 결정적으로 커버.

## 결정 흐름

- **스파이크 우선 (plan-auditor 봉합 ②)** — "적 변위 신규?"를 본작업 전 30분 스파이크로 확정. 결과 = 채널 존재(오판 정정). plan의 전제를 *코드 실측*으로 검증한 게 STOP 위험 회피 + 범위 확정의 핵심.
- **허딩 = `EnterHitState` 재사용 vs 전용 push** — 재사용 채택(YAGNI). 기존 넉백 채널이 정확히 "외부 X 변위 + 감쇠"라 허딩과 동형. 부수효과(짧은 hitstun)는 허딩에 자연스러움. 더 센 허딩 원하면 대쉬 전용 push 상수 분리(Play 튜닝 후속).
- **무적 = invuln window + 데미지 게이트 (단일 경로)** — `InterruptibleByHit=false`가 이미 넉백/hitstun 차단 → 데미지 0만 추가. 플레이어 데미지 경로가 보스 하나뿐임을 *전수 grep*으로 확정해 게이트 1곳으로 완성(trust-boundary는 누락 0이 생명).
- **i-frame over-coverage (+1틱) 의도** — 무적(T..T+8, 9틱)이 대쉬 모션(T..T+7, 8틱)보다 1틱 길다. under-coverage(모션 중 노출)는 신뢰 경계 결함이지만 over-coverage는 익스플로잇 0 + 관대 — i-frame은 *애매하면 길게*가 정석(`<=` 보수적). reviewer가 주석 부정확("정확히 정합") 적발 → 코드는 유지(더 안전), 주석만 정정.
- **선택적 Opus B 첫 적용** — 복잡·trust-boundary라 구현 Worker를 Opus로(2026-06-13 라우팅 개편). 그럼에도 메인 file:line 실측 게이트 유지 — reviewer가 Opus Worker의 주석 부정확을 잡았듯, 모델 무관 검증이 작동(B 규칙의 "불변" 조항 실증).

## 막혔던 지점 / 이월

- **🟡1 주석 정정 완료** — invuln window가 모션보다 +1틱 길다는 걸 "정확히 정합"으로 잘못 적었던 주석(DashAction/PlayerEntity) → 실제(over-coverage 안전 방향)로 정정.
- **🟡2 통합 테스트 (후속 후보)** — 현재 무적 게이트 테스트는 `InvulnUntilTick`을 직접 세팅해 검증(단위). `DashAction 발동 → 무적 → 보스 공격 데미지 0`의 end-to-end 통합 테스트 1개 있으면 회귀 안전망 ↑. 단위 + diff 실측으로 wiring 확인됨 → 비차단, 여유 시 추가.
- **working tree dll 잔여** — `03_Client/Assets/Plugins/{Shared,ClientNet}.dll`(P1부터). P3도 서버 단독(98_Shared write 0)이라 dll commit 불필요 — sync는 PR 시점.
- **허딩 세기 Play 튜닝** — 시작값 ~1.3u(기존 넉백)는 "몹몰이" 체감엔 약할 수 있음. 2클라 Play에서 영호 튜닝(대쉬 전용 push 상수 분리 여부 포함).

## 학습 일지 후보 키워드

스파이크가 plan 전제 정정(적 변위 "신규"=오판, 채널 존재 — file:line 실측 가치) / 기존 채널 재사용(허딩=EnterHitState, 신규 충돌시스템 0) / 무적 게이트 전수성(trust-boundary는 데미지 경로 전수 grep = 누락 0이 생명) / i-frame over-coverage 안전 방향(under=노출 결함, over=관대, 애매하면 길게 `<=`) / dash≠melee 구분(invuln 필드를 dash만 세팅) / 선택적 Opus B 첫 적용(Opus Worker도 주석 부정확 — 메인 실측 게이트 모델 무관 실증) / wire 무변경(서버 권위 거동은 기존 패킷 재사용).

## 다음 Phase

- **P4 — 공유 추출** (`04-shared-extract.md`, 보통, depends:P2). 대쉬/임펄스 공식을 98_Shared 단일 출처로(헌법 §4, `CombatConstants` 서버전용 → `Constants`). wire v12 무변경. **★P5(클라 예측 B) 핵심 리스크의 안전망**(복붙 = silent drift 차단).
- 단방향: P1✅→P2✅→**P4**→P5→P6 / P3✅는 P2 위(P5와 독립). 순서·타이밍 영호.
