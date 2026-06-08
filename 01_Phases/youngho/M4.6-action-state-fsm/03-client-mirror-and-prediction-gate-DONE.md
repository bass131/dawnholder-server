---
owner: youngho
milestone: M4.6
phase: 03
title: 클라 미러 정합 — commit window 예측 게이트 + 넉백 force-adopt + 시각 상태 검증
status: done
grade: 복잡
risk: unity-asset
summary: 서버 권위 이동 잠금(AttackState/HitState)을 클라 예측이 같은 98_Shared 상수로 거울 → reconcile rubber-band 0. source-gating으로 Predict/송신/replay 일관, 넉백은 force-adopt로 시각화. v9 불변, 회귀 0.
---

# Phase 03 DONE — 클라 미러: commit window 예측 게이트 + 넉백 표시

> 브랜치 `feature/m4.6-03-client-mirror` · 커밋 `4d333ae` · 2026-06-08 (세션25)

## TL;DR

Phase 02가 서버에 깐 **이동 잠금(공격 commit window + 피격 hitstun) + 넉백**을,
클라 예측이 **같은 규칙으로 거울(mirror)** 하게 만들어 reconcile rubber-band(서버가
위치를 되돌려 튕기는 현상)를 **0**으로 만들었다. 헌법 #1: 이 게이트는 **권위가 아니라 예측**
(서버가 어차피 재검증) — 클라는 따라가기만.

- **source-gating** (핵심): 잠금 시 입력을 *Predict 내부가 아니라 근원*에서 0으로 막는다.
  → `Predict` / `C_MoveIntent` 송신 / `InputHistory`(replay) 세 소비자가 **같은 gated 입력**을
  써서 reconcile replay가 서버와 정확히 일치. 서버가 공격을 거부(rate-limit)해도 송신 입력이
  0 → 서버도 0 적용 → **발산 0**. (`ResolveGatedInput` 순수 함수로 추출 → 불변식을 단위 테스트로 박음)
- **공격=로컬 예측 / 피격=서버 신뢰**: 공격은 로컬 `OnAttack` 시점에 commit 타이머 시작
  (`AttackCommitWindowTicks × TickDuration` 초, **98_Shared 단일 상수**) → 서버 확인 전 즉시 "콱 정지".
  피격은 클라가 *언제 맞을지* 예측 불가 → 서버 `animState==Hit`로만 게이트. (예측 가능/불가능의 비대칭을 코드에 반영)
- **넉백 표시 = force-adopt**: 클라는 서버 넉백 임펄스(`ExternalVelX`)를 예측 못 함 → 피격 중엔
  `SnapThreshold`(1.5) 이내여도 서버 위치를 채택(`PlayerPredictor.OnSnapshot forceAdopt`)해
  넉백을 시각화 + sub-threshold offset 누적(영구 어긋남) 방지. `SnapCount`는 진짜 mispredict에만 증가.
- **Plugins Shared.dll 정식 갱신** (ADR-010, 진입 의무): Phase 02가 바꾼 `Constants`/`Physics`
  (`AttackCommitWindowTicks`/넉백 상수/`ExternalVelX`)를 클라가 쓰도록 98_Shared 재빌드 → DLL 복사 → commit.
- **ProtocolVersion 9 불변**(신규 패킷 0). `S_Snapshot.animState`는 기존 필드 소비 확장만.

## AC 검증 결과

| 완료 조건 | 결과 | 근거 |
|---|---|---|
| 공격 중 reconcile rubber-band 0 | ✅ (구조) | source-gating으로 Predict/송신/replay가 동일 gated 입력 → 서버 0과 일치. `ResolveGatedInput`(잠금 시 (0,false)) 단위 테스트로 박음. **Play 실측은 사용자 검증 단계** |
| commit window = 98_Shared 단일 상수 (하드코딩 X) | ✅ | `Constants.AttackCommitWindowTicks × TickDuration` 참조. 클라 재입력 0 (drift 함정 회피) |
| Attack/Hit/Death 애니 서버 동기 | ✅ | `LocalPlayerMotion.ResolveAnimState`(기존) — 서버 animState 우선. `LocalPlayerMotionTests` 회귀 유지 |
| 넉백 시각화 (피격 변위 표시) | ✅ | `PlayerPredictor.forceAdopt` — 피격 중 서버 위치 채택. `OnSnapshot_ForceAdopt_*` 4케이스 |
| 원격 플레이어 공격 중 미끄러짐 0 | ✅ | `RemoteEntity`는 **외삽 안 함**(주석 명시) → 서버 정지 좌표 보간 = 정지. 기존 설계로 충족(코드 무변경) |
| 예측 게이트 순수 함수 테스트 | ✅ | `IsMovementLocked` 8 + `ResolveGatedInput` 2 (EditMode) |
| 기존 클라/공유 테스트 회귀 0 | ✅ | `forceAdopt` 기본인자 false → 기존 `OnSnapshot` 호출/12 회귀 테스트 무손상. `.NET` 풀 테스트 **449/0/4skip** |
| 클라 컴파일 clean | ✅ | Unity 강제 재컴파일 2회(초기+refactor) CS 에러 0 (MCP 콘솔 확인) |
| reviewer 🔴 0 | ✅ | 5축 통과, 🟡 2건(선택, 1건 반영) |

**검증 경계 명시**: rubber-band 0 / 넉백 체감 / 원격 미끄러짐은 **구조적으로 보장**(코드 + 단위 테스트)
했으나, *살아있는 서버 + Play 실측*(공격 연타 제자리, 2클라 원격, 직업 2종)은 사용자 Unity 검증 영역
(`unity-visual-work-user-owned`). EditMode 14케이스는 컴파일 검증 + 로직 확정 — Test Runner green은 원클릭.

## 결정 흐름

1. **source-gating (게이트 위치)**: 잠금을 `Predict()` 안에 넣으면 reconcile replay 경로
   (`OnSnapshot`의 `ReplayFrom`)가 게이트를 안 거쳐 서버와 어긋남. → 입력이 *갈라지기 전 한 곳*
   (`Update` 상단)에서 0으로 막아 세 소비자가 정의상 일치하게 함. client prediction의 가장 흔한 버그
   ("예측 입력 ≠ 송신 입력 ≠ replay 입력")를 구조로 차단.
2. **공격 vs 피격 비대칭**: 클라는 자기 공격 *시점*은 알지만(로컬 입력) *언제 맞을지*는 모름(서버 판정).
   → 공격은 로컬 타이머 선예측(즉시 반응), 피격은 서버 animState만 신뢰. 거부(rate-limit)당해도
   source-gating이라 발산 0이므로 헛스윙만 제외(`TryAttack` 성공 시에만 commit 시작).
3. **게이트 연장(거울 보정)**: 로컬 타이머가 dt drift로 서버 window보다 짧게 끝나면 tail에서 발산 →
   `serverAnimState==Attack`을 OR로 묶어 서버 확인까지 잠금 연장. over-gate는 source-gating이 흡수.
4. **넉백 force-adopt**: 넉백(~1.26유닛)이 `SnapThreshold`(1.5) 이내라 일반 reconcile이 안 잡음 →
   피격 중 영구 offset 누적. 피격(Hit)에만 force-adopt를 걸어 서버 위치 채택(서버 `KnockbackVx`가
   HitState에서만 세팅·decay되는 사실과 1:1 대응). Attack/Death는 force-adopt 불필요(Death는 리스폰
   점프가 임계 초과라 일반 mispredict로 잡힘).
5. **Plugins DLL 정책**: Shared.dll만 정식 갱신(클라가 새 상수 필요), 빌드가 같이 drift시킨
   ClientNet.dll은 복원(소스 무변경). CI 기본=Debug 빌드라 Debug DLL 커밋(미래 churn 최소).
6. **순수 함수 추출 (reviewer 🟡 반영)**: source-gating 일관성이 MonoBehaviour Update라 단위 테스트
   사각지대 → `ResolveGatedInput` static 추출. 회귀 시 *조용히* rubber-band 부활하는 핵심 불변식을 박음.

## 잔여 / 후속 (reviewer 🟡, 선택)

- 🟡 (미반영, Rule-of-Three 후보) 이동 잠금 판정이 클라/서버에 *각자* 존재(`IsMovementLocked` vs
  서버 `ActorState.LocksMovement`). 세 번째 잠금 상태(Stun/Cast 등) 등장 시 98_Shared `AnimState`에
  `LocksMovement()` 순수 함수 단일 출처로 추출 검토(지금은 클라에 서버 State 객체가 없어 enum 재판정이 불가피).
- Phase 06(회귀/마감): 살아있는 서버 + Play 실측(rubber-band/넉백/원격) + 봇 풀 재검(넉백/commit window 적응).
- 본 Phase로 플레이어 수직 슬라이스(01 골격 → 02 전투/넉백 → 03 클라 미러) **완료**. 다음 = Phase 04 몬스터 AI.

## 학습 일지 후보 키워드

- source-gating: 게이트를 입력이 갈라지기 전 한 곳에서 걸면 Predict/송신/replay가 정의상 일치 (rubber-band의 근본 원인 = 세 입력의 미세 불일치)
- 예측 가능/불가능의 비대칭: 클라가 아는 것(자기 입력 시점)은 선예측, 모르는 것(언제 맞을지)은 서버 신뢰
- force-adopt: sub-threshold 변위(넉백)는 일반 reconcile이 못 잡음 → 서버 권위 상태에선 임계 무시 채택
- 단일 진실 상수: commit window 지속을 클라가 하드코딩 재입력하면 drift = 튕김. 98_Shared 참조로 봉합
- 거울(mirror)의 의미: 클라 게이트는 권위가 아니라 예측. 서버가 진실, 클라는 송신 절감 + UX
- Unity 컴파일 검증: CI는 .slnx만 → 클라 Scripts 컴파일은 MCP 강제 재컴파일 + 콘솔 에러 read로 확인
- Plugins DLL 규율: Shared 소스 바뀌면 정식 빌드→복사→commit, 동반 drift(ClientNet)는 복원 분리
