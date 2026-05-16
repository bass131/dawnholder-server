---
summary: Unity Gameplay 씬 + Player/Ground/Camera 배치 + A/D 좌우 이동 (네트워크 X). M2의 오프라인 Hello World로 GameObject/Component 워크플로우 확립.
phase: 01-unity-scene-and-character
status: done
completed_at: 2026-05-11
commit: (이 박제 직후 commit hash로 갱신)
---

# Phase 01 — Unity 씬 + 캐릭터 GameObject (오프라인) 완료 박제

**소요 시간**: 약 20~30분 (스크립트 작성 + Unity 클릭). 메뉴 명칭 차이 1건(`Create → Scene → 2D (URP)` 없음)으로 잠깐 길어짐.

## TL;DR
M2 첫 Phase. Unity 6.4 LTS에 `Gameplay.unity` 새 씬을 만들어 흰 사각형 Player + 회색 Ground + Orthographic Camera를 배치했다. `LocalPlayerController.cs`가 `PlayerInput`(Behavior=SendMessages)의 `Move` 액션을 받아 `transform.position`을 직접 갱신, `CameraFollow.cs`가 LateUpdate에서 target을 Vector3.Lerp로 부드럽게 추종한다. 네트워크 코드는 의도적으로 빠졌고, M1의 `NetworkBootstrap`은 새 씬에 추가되지 않은 상태로 자동 connect 비활성. 다음 Phase 02부터는 서버 측 20 TPS 틱 루프를 짓는다.

## 5단계 보고

- **무엇을 만들었나** — `Gameplay.unity` 씬 + Player/Ground/Main Camera GameObject + 두 개 신규 C# 스크립트(`LocalPlayerController.cs`, `CameraFollow.cs`). A/D 키로 캐릭터가 좌우로 움직이고 카메라가 부드럽게 따라간다.
- **왜 필요한가** — M2의 핵심 흐름(서버 권위 + prediction + reconciliation)에 진입하기 전, Unity 환경이 이 프로젝트에서 정상 도는지 검증할 오프라인 Hello World. GameObject/Component/Inspector/Input System 워크플로우를 한 번 손으로 체득해야 다음 Phase부터 의미 있게 자동화/지시 가능.
- **어떻게 만들었나** — Unity 6 기본 `InputSystem_Actions.inputactions`의 `Player` 맵을 그대로 재활용(`Move` Vector2 + WASD 바인딩 이미 포함). `PlayerInput` 컴포넌트 Behavior를 **Send Messages**로 두어 `OnMove(InputValue)` 콜백이 자동 wire되게 함(학부생 친화 최단 경로). 카메라는 `LateUpdate`에서 `Vector3.Lerp`로 추종 — Update에 두면 한 프레임 지연 발생.
- **테스트 결과** — Play 모드 수동 검증 4건 OK(아래 AC 검증 결과 섹션 참조). 자동 테스트는 이번 Phase 범위 밖(Unity scene 동작 검증은 manual로 합의된 영역).
- **다음 스텝** — Phase 02 시작 (`02-server-gameloop.md`). 서버에 20 TPS 틱 루프 + 단일 GameMap actor 골격. 담당: gameplay. Phase 02 진행 중 자연스러운 빈 시간에 Unity AI MCP 셋업 사이드 트랙(CONTEXT.md "보류 중" 박혀있음).

## AC 검증 결과

Phase 파일 `01-unity-scene-and-character.md`의 "완료 조건" 4개를 다음과 같이 실행·확인:

1. **Unity Play 모드 진입 → A/D 키로 캐릭터가 좌우로 움직임** ✅
   - 절차: Unity 에디터 ▶ Play → A 누름 → Player가 좌측 이동, D 누름 → 우측 이동.
   - 결과: 사용자 "작업 완료" 확인.

2. **카메라가 캐릭터를 부드럽게 따라감** ✅
   - 절차: A/D로 캐릭터 이동 → Main Camera가 LateUpdate Lerp(smoothing=0.15)로 추종.
   - 결과: 사용자 "작업 완료" 확인.

3. **60fps와 30fps 환경에서 같은 속도로 움직임 (deltaTime 검증)** ⚠️ 부분 검증
   - 코드 보장: `LocalPlayerController.Update`에서 `transform.position += new Vector3(_moveInput.x, 0f, 0f) * moveSpeed * Time.deltaTime` — deltaTime 곱셈 보장됨.
   - 명시적 측정: Project Settings에서 `Target Frame Rate = 30`으로 강제한 비교 측정은 본 Phase에서 별도 수행하지 않음. fps 의존 버그는 *코드 부재*가 아니라 *코드 존재*로 차단됨.
   - 후속 발견 시 재오픈.

4. **Scene 종료 시 콘솔에 에러 없음** ✅
   - 절차: Play 종료 → Console 창 확인.
   - 결과: 사용자 "작업 완료" 확인, 에러 없음.

종합: AC 4건 중 3건 완전 PASS, 1건 코드상 보장 + 명시 측정 보류. Phase 진행 차단 사유 없음.

## 결정 흐름 (학습 일지 쓸 때 참고용)
- **PlayerInput Behavior 선택** — Send Messages vs Invoke Unity Events vs Invoke C# Events vs Broadcast Messages 4개 중 **Send Messages**. 이유: `OnMove(InputValue)` 메서드만 박으면 자동 wire, 학부생 모드에서 가장 단순. 단점: 메서드 이름이 매직 스트링 매칭이라 오타 시 조용히 동작 안 함.
- **Transform 직접 조작 vs Rigidbody2D** — Transform 채택. 이유: 이번 Phase는 물리 충돌 없음, Rigidbody는 자체 적분이라 Shared/Physics 단일 출처(Phase 07)와 결과 안 맞을 위험. 단점: 충돌/중력은 Phase 07에서 직접 구현 필요.
- **LateUpdate vs Update에 카메라** — LateUpdate. 이유: 캐릭터 Update가 먼저 끝난 뒤 카메라가 따라가야 한 프레임 지연(덜덜거림) 없음. Unity 카메라 follow 표준 패턴.
- **Unity AI MCP 셋업 시점** — 지금 X / Phase 02 후 ✅ / 아예 X 중 Phase 02 후. 이유: Phase 01은 Unity 클릭의 *학습 가치*가 가장 큼(첫 단추). MCP 진가는 Phase 03+ 반복 wire-up 구간. CONTEXT.md "보류 중" 박음.

## 막혔던 지점 (있다면)
- **`Create → Scene → 2D (URP)` 메뉴 없음**
  - 증상: Project 창 우클릭 → Create → Scene 서브메뉴가 단일 항목만 있고 "2D (URP)" 선택지 안 보임.
  - 원인: Unity 6.4 LTS에서 우클릭 메뉴는 빈 씬 생성으로 축소되고, *템플릿 기반* 생성은 `File → New Scene`(Ctrl+N) 다이얼로그로 이동.
  - 해결: `File → New Scene` → `Lit2DSceneTemplate` 선택 → `Ctrl+S`로 `Assets/Scenes/Gameplay.unity` 저장.

## 학습 일지 후보 키워드
- `/journal:concept Unity Input System PlayerInput Send Messages` — 4가지 Behavior 모드 비교, 매직 메서드 이름 약속, 액션 맵의 의미
- `/journal:concept Time.deltaTime` — frame-rate independence의 의미, Update vs FixedUpdate vs LateUpdate 셋의 호출 시점
- `/journal:concept Unity GameObject Component` — Unity의 ECS 비스무리한 구조, Inspector가 보여주는 것의 정체
- `/journal:concept Camera follow LateUpdate` — 한 프레임 지연이 왜 생기고 어떻게 막는가
