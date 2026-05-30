---
owner: youngho
phase: 08b
title: 애니 상태머신 — 클라 구조 (IMotionState + AnimatorDriver + 소스 3종 + enemy 위치 보간)
status: done
completed: 2026-05-30
grade: 복잡
summary: M4.3 Phase 08b 애니 상태머신 클라 구조 완료. 전략 패턴(IMotionState + AnimatorDriver 공통 + 소스 3종)으로 서버 animState를 Animator에 반영하는 골격 + enemy 위치를 기존 RemoteEntity 보간 재사용으로 부드럽게(스냅→보간). LocalPlayer는 PlayerAnimatorSync 공존(D1), 컴포넌트는 런타임 AddComponent(D2, prefab 편집 0). dotnet build 0/0, reviewer 🔴0, Unity Play 실측(enemy 보간+flip 정상, 콘솔 클린). 클라 렌더 골격 완성 — 실제 Animator 클립은 11.
---

# Phase 08b 박제: 애니 상태머신 — 클라 구조

**소요**: ~1 세션 (client SubAgent 위임 + reviewer + Unity Play 실측)

## TL;DR

08a가 서버에서 보내주는 `animState`(byte)를, 클라가 **전략 패턴(Strategy)** 으로 Animator에 반영하는 골격을 세웠다. "Animator를 구동하는 일"은 공통 컴포넌트(`AnimatorDriver`)가, "상태를 *공급*하는 소스"만 객체별로 갈아끼운다(`IMotionState` 구현 3종: Local=입력 / Remote=`S_Snapshot.animState` / Enemy=`S_EntityState.animState`). 동시에 enemy 위치를 **기존 `RemoteEntity` 보간 컴포넌트 재사용**으로 부드럽게(스냅→150ms 지연 보간) 만들었다. 잘 도는 코드를 추출하지 않고 **부품처럼 런타임 AddComponent**(조합 > 추출). 실제 Animator 클립/전이는 Phase 11(본인 외관 분담).

## 5단계 보고

- **무엇을 만들었나** — `IMotionState` interface + `AnimatorDriver` 공통 컴포넌트(Animator/SpriteRenderer null 가드, `SetInteger("AnimState")` + flipX) + 소스 3종(`LocalPlayerMotion`/`RemotePlayerMotion`/`EnemyMotion`). enemy를 `EnemyRegistry.UpdatePosition` 직접 transform 세팅 → `RemoteEntity.EnqueueSnapshot` 보간 전환. 패킷 핸들러 wiring(`S_EntityState.animState`→EnemyMotion, `S_Snapshot.animState`→RemotePlayerMotion).
- **왜 필요한가** — Jump/Attack/Hit/Death는 위치로 추론 불가 → 서버 권위 byte를 클라가 *읽어 표시만*(헌법 #1). 세 객체가 "애니 구동"이라는 같은 일을 하되 "상태 출처"만 다름 → 전략 패턴으로 driver 1개 재사용, 복붙 0. enemy 위치도 위치만 옮기고 보간을 빼먹으면 20TPS 스냅(뚝뚝)으로 보임.
- **어떻게 만들었나** — `AnimatorDriver`는 소스를 모르고 `IMotionState`만 의존. enemy/remote 컴포넌트는 **런타임 AddComponent**(D2 — prefab/scene/asset YAML 편집 0, unity-asset 위험 깃발 회피 + Phase 08 prefab 사고 학습). LocalPlayer는 `PlayerAnimatorSync` **공존**(D1 — 흡수 시 flipX 이중 세팅 충돌, 발표 핵심 회귀 0 우선). enemy 보간은 기존 `RemoteEntity` 그대로 부착(조합 > 추출).
- **테스트 결과** — `dotnet build Dawnholder.slnx --no-incremental` 경고 0/오류 0. reviewer Tier 2-A 🔴0(머지 차단 없음, 🟡 3건 전부 봉합). Unity Play 실측: enemy 보간 부드러움 ✅ / enemy flip 가는 방향 ✅ / 콘솔 08b 관련 에러·경고 0(뜬 폰트 경고는 선재 코스메틱).
- **다음 스텝** — Phase 11(애니 외관 완성, 본인): Animator Controller에 `AnimatorDriver.AnimStateParam`(`"AnimState"`, int) 파라미터 + 6상태 클립/전이 셋업. (병렬) Phase 09 boss가 08a/08b 위에 boss attack animState. **선행 Phase 10(움직임 polish)**: 로컬 플레이어 reconcile rubber-band(선재 이슈, 아래 참조).

## 신설 / 변경 파일

**신설** (`03_Client/Assets/Scripts/Rendering/`)
- `IMotionState.cs` — `CurrentAnimState` + `Facing` 추상화 (전략 패턴 창구)
- `AnimatorDriver.cs` — 공통 구동기. `SetInteger("AnimState")` + flipX. Animator null 가드 + controller 미연결 가드 + `SpriteDefaultFacesLeft` 토글(sprite 기본 방향 보정)
- `RemotePlayerMotion.cs` — `S_Snapshot.animState` 노출, Facing은 보간 델타
- `EnemyMotion.cs` — `S_EntityState.animState` 노출(⚠️ `state`(AI FSM) 아님), Facing은 보간 델타
- `LocalPlayerMotion.cs` — prediction transform 델타로 Idle/Walk 도출. 08b는 구조만(D1, GO 미부착)

**수정**
- `Combat/EnemyViewFactory.cs` — enemy GO에 `RemoteEntity`+`EnemyMotion`+`AnimatorDriver` AddComponent(순서: Motion→Driver) + `out visualFootOffset` + `SpriteDefaultFacesLeft=true`
- `Combat/EnemyRegistry.cs` — `EnemyEntry` struct(RemoteEnemy+RemoteEntity+EnemyMotion) dict, `UpdatePosition(...,byte animState)` → `EnqueueSnapshot`(footOffset 포함) + `SetAnimState`, Despawn/Clear에 `ClearBuffer`
- `Combat/RemoteEnemy.cs` — `VisualFootOffset` 프로퍼티 + `Initialize` 인자 추가
- `State/RemoteEntityRegistry.cs` — `_motions` dict, Spawn에서 `RemotePlayerMotion`+`AnimatorDriver` AddComponent, `UpdateSnapshot(...,byte animState)`
- `Network/ClientPacketHandlers.cs` — `SnapshotHandler`(animState 캡처+전달), `EntityStateHandler`(`animState` 읽어 전달)

## AC 검증 결과

```
$ dotnet build Dawnholder.slnx --no-incremental
  빌드했습니다. 경고 0개 / 오류 0개

Unity Play 실측 (서버 20TPS 가동):
  - enemy 위치 보간 부드러움(스냅 아님) ✅
  - enemy flip 이동 방향 정합 ✅ (SpriteDefaultFacesLeft 보정 후)
  - 콘솔: 08b 관련 에러/경고 0 (NRE/RemoteEntity 에러 없음)
  - 잔존 경고 = StageClearUI 폰트(선재 코스메틱, 08b 무관)
```

- reviewer Tier 2-A: 🔴0. 헌법 #1(animState byte 읽기만, 클라 추측 0) 모범 준수. EnemyEntry struct가 TryGetNearest/ApplyHit/Despawn/Clear 전 경로 일관 반영(회귀 0). ClearBuffer로 보간 buffer 누수 함정 봉합.
- AnimatorDriver 파라미터 계약: `AnimStateParam = "AnimState"`(int). Phase 11이 맞춰야 할 단일 진실.

## 결정 흐름 (회고 참고용)

- **D1 LocalPlayer 공존 vs 흡수** → 공존 채택. `PlayerAnimatorSync`(IsMoving+flipX) 그대로 두고 `LocalPlayerMotion`은 구조만. 흡수 시 두 컴포넌트가 flipX 이중 세팅 충돌 + 발표 핵심 캐릭터 회귀 위험. 실제 연결은 11에서 결정.
- **D2 런타임 AddComponent vs prefab 편집** → AddComponent 채택. prefab YAML 손대면 unity-asset 위험 깃발 + 본인 외관 분담 침범 + Phase 08 BackGround prefab 사고 학습. enemy는 원래 코드 생성이라 자연스럽고 RemotePlayer도 Spawn()에서 부착.
- **enemy 중복 vs RemoteEntity 재사용** → 재사용(조합). 잘 도는 보간 컴포넌트를 *붙임*. 공유 클래스 추출보다 결합도 낮음(Unity 컴포넌트 철학).
- **RemotePlayerMotion / EnemyMotion 분리 유지** → reviewer 판정 정합. 로직이 우연히 같을 뿐 변경 이유(프로토콜 출처)가 다름 → Rule of Three 2번째라 추상화 시점 아님. 09/11에서 갈라질 축선.
- **flip = sprite별 토글(SpriteDefaultFacesLeft)** → Mushroom/ToxicFrog placeholder가 좌향 기본이라 전체 반전 X(RemotePlayer 깨짐). enemy에만 토글. 보스 sprite 다르면 11에서 분리.

## 막혔던 지점 / 이월

- **🔴 로컬 플레이어 reconcile rubber-band (선재, 08b 무관 — Phase 10 처리)**: Play 중 본인 캐릭터가 뒤로 튀고 덜덜 떨림 관측. 원인 = 클라 가변 dt 예측 vs 서버 고정 50ms 시뮬 궤적 차이 + 예측 lead(~1.5칸)가 SnapThreshold(1.5)와 동일 → 잦은 false reconcile + 하드 snap. 서버(`GameMap.cs:284` position↔ack 동일 틱)는 정합 확인. **예측 시스템은 08b가 안 건드림** — 기존 이슈. 사용자와 합의: 08b 먼저 마감 → Phase 10에서 "replay 후 잔차 비교 + smooth blend"(레버 1)로 제대로. `[Reconcile]` 로그 dx ±1.5~1.7 / dy ±1.6(점프) 실측 근거 보유.
- **AnimatorDriver controllerless 경고**: RemotePlayer.prefab에 Animator는 있으나 controller 미연결(`m_Controller: 0`) → 매 프레임 SetInteger 시 콘솔 경고 우려. `runtimeAnimatorController != null` 가드 추가(reviewer 🟡 봉합). 11에서 controller 붙으면 자동 동작.
- **animState 가시 검증 보류**: enemy는 Animator 없음, RemotePlayer는 controller 미연결 → 08b에선 `AnimState` 파라미터 값 변화를 Inspector로 못 봄. 배선은 완성, 실제 파라미터 구동은 11(controller 부착 후) 검증.

## 학습 일지 후보 키워드

전략 패턴(Strategy) / 조합 > 상속·추출(composition over extraction) / 우연한 중복 vs 본질적 중복(Rule of Three) / 보간 vs 외삽(RemoteEntity 재사용) / 표현·데이터 레이어 분리(위치 EnqueueSnapshot ↔ 애니 SetInteger 별 경로) / Animator 파라미터 계약(Phase 간 인터페이스) / sprite 기본 방향 × flipX / 헌법 #1(클라=렌더) / prediction reconcile(가변 dt vs 고정 dt drift — Phase 10 복선)

## 다음 Phase

- **Phase 11** — 애니 외관 완성 (본인): Animator Controller `"AnimState"` int 파라미터 + 6상태 클립/전이.
- **(병렬) Phase 09** — boss behavior(+attack animState).
- **(선행) Phase 10** — 움직임 polish: 로컬 플레이어 reconcile rubber-band 봉합(레버 1: replay 후 잔차 비교 + smooth blend).
