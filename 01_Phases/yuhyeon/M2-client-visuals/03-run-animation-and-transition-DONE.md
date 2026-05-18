---
summary: Player_Run.anim (7 frame Walking_KG_1, FPS 12, Loop True, position 키 0) + Player.controller에 Run State 추가 + IsMoving bool Parameter + Idle↔Run 양방향 Transition (Has Exit Time=false, Duration=0). 본인 Animator Window 토글로 즉시 전환 검증 통과. 드래그 함정 재발(Walking_KG_1_0.controller 부산물)은 Phase 02 학습 즉시 적용해 MCP MoveAssetToTrash로 자동 정리.
phase: 03-run-animation-and-transition
work-id: yuhyeon-m2-phase03-run-animation-and-transition
status: done
completed_at: 2026-05-19
commit: TBD
---

# Phase 03 — Run 애니메이션 + Idle↔Run 전환 완료 박제

**소요 시간**: ~1.5시간 (드래그 작업 5분 + drift fix 자동화 30분 + 검증 박제 25분)

## TL;DR

Walking_KG_1 7 frame을 드래그 워크플로우로 Player_Run.anim 자동 생성 + 같은 Player.controller에 Run State 추가. *Phase 02 학습 즉시 적용* — 드래그가 또 부산물 Controller(Walking_KG_1_0.controller) 박는 함정 재발했으나 MCP MoveAssetToTrash로 즉시 정리 + IsMoving bool Parameter + 양방향 Transition(Has Exit Time=false, Duration=0, IsMoving=If/IfNot)을 AnimatorController API로 자동 추가. 본인이 Animator Window 토글로 *즉시 전환* 시각 확인 통과. Phase 04에서 SetBool("IsMoving", ...) 호출 시 정합 보장.

## 5단계 보고

- **무엇을 만들었나** — `Assets/Animations/Player/Player_Run.anim` (length 0.583s, frameRate 12, isLooping True, position 키 0, sprite swap 7 keys). Player.controller 확장: Parameters 0→1 (`IsMoving` Bool default False), States 1→2 (Player_Idle 유지 + Player_Run 추가), Transitions 0→2 (양방향). 부산물 `Walking_KG_1_0.controller` 휴지통 처리(MoveAssetToTrash).
- **왜 필요한가** — Phase 04 이동 wiring 시 `Animator.SetBool("IsMoving", true)`로 *즉시* Run으로 전환되는 시각 효과 필수. *이동 결과가 즉시 보임* = 면담 데모의 핵심 시각. Has Exit Time=false + Duration=0이 픽셀아트의 *블렌딩 0 / 즉시 반응* 컨벤션. 같은 Controller 안에서 두 State 묶기는 Mecanim State Machine의 *기본 패턴* — Phase 04 LocalPlayerController 연동의 토대.
- **어떻게 만들었나** — 결정 1: 드래그 워크플로우 시 *Player.controller 덮어쓰기 회피* — 본인이 Player GameObject 위로 정확히 드래그(Phase 02 함정 학습 적용). 결정 2: 드래그가 *새 Controller(Walking_KG_1_0)*를 부산물로 박은 사고 재발 — Rule of Three 확정(M1 P04, M2 P01/P02와 합쳐). MCP MoveAssetToTrash로 자동 정리(AssetDatabase.DeleteAsset 시 user interaction 에러 → MoveAssetToTrash 사용 패턴). 결정 3: Parameter + Transitions은 *드래그가 자동 안 만듦* — AnimatorController API(`AddParameter` / `AnimatorState.AddTransition` / `AddCondition`)로 코드 자동화. 새 개념 = AnimatorConditionMode.If/IfNot (Bool Parameter의 true/false 조건 표현 방식).
- **테스트 결과** — MCP 검증 다축:
  - AnimatorController 개수: 1 (Walking_KG_1_0 휴지통 처리 확인)
  - Player.controller: Parameters 1 (IsMoving Bool), States 2 (Idle default + Run), Transitions 2 (양방향 exit=False dur=0 cond IsMoving)
  - 두 클립: position 키 0 (Phase 04 텔레포트 회피), sprite keys 4/7
  - Player Animator: controller=Player, updateMode=Normal, valid=True
  - Scene: Player + Square 둘만 (중복 0)
  - Console 에러/경고 0건
  - 본인 Animator Window 토글 회귀: "잘 진행되네" 보고 — IsMoving on/off 즉시 전환 시각 확인 통과
- **다음 스텝** — Phase 04 진입 (캐릭터 이동 wiring, ★ 면담 데모 도착점). 진입 전 *Phase 04 파일 갱신 필수*: (a) `Scripts/Input/LocalPlayerController.cs` + `Scripts/Prediction/PlayerPredictor.cs` *이미 존재*하므로 새 PlayerController 만들기 X, *기존 코드에 SetBool + flipX 추가* 방향 / (b) `03_Client/CLAUDE.md` "새 Input System 패키지 / 레거시 Input.GetKey 금지" 규정 정합 — Input.GetAxisRaw 대신 PlayerInput Send Messages 콜백(OnMove) 사용. 본인과 Phase 04 시작 시 코드 read + 변경 방향 합의 후 작업.

## AC 검증 결과

```bash
# 1. AnimatorController 개수 (Walking_KG_1_0 부산물 정리 후)
AnimatorController count: 1
  Assets/Animations/Player/Player.controller

# 2. Player.controller 최종 구조
Parameters: 1
  IsMoving (Bool, default=False)
States: 2, default=Player_Idle
  State 'Player_Idle' clip=Player_Idle, speed=1
    -> Player_Run: exit=False, dur=0, cond=[IsMoving=If]
  State 'Player_Run' clip=Player_Run, speed=1
    -> Player_Idle: exit=False, dur=0, cond=[IsMoving=IfNot]

# 3. 두 클립 검증
Clip 'Player_Idle': len=0.333s, fps=12, loop=True, position keys=0, sprite keys=4
Clip 'Player_Run':  len=0.583s, fps=12, loop=True, position keys=0, sprite keys=7

# 4. Player GameObject Animator
Player Animator: controller=Player, updateMode=Normal, runtimeController valid=True

# 5. Scene root sprite GameObjects (중복 검사)
Player (sprite=knight14_idle_00, pos=(0,0,0), scale=(1,1,1))
Square (sprite=Square, pos=(0,-2,0), scale=(20,0.5,1))

# 6. Console
Errors: 0, Warnings: 0

# 7. 본인 Animator Window 토글 회귀 (Play 모드 + Parameters 탭 IsMoving 체크/해제)
"잘 진행되네" — 즉시 Idle↔Run 전환 시각 확인 통과
```

## 결정 흐름

- 드래그 워크플로우 vs 수동 AnimationClip 생성 → 자동(Phase 02 패턴 재사용).
- 드래그가 새 Controller(Walking_KG_1_0) 박은 부산물 → Phase 02 패턴 그대로 자동 정리. MCP `AssetDatabase.DeleteAsset`이 user interaction 에러 → `AssetDatabase.MoveAssetToTrash` 사용 → 휴지통 이동 + 다이얼로그 없음. **새 패턴 학습**.
- Parameter + Transitions 자동 박기 → AnimatorController API 사용(`AddParameter` / `AnimatorState.AddTransition` / `AddCondition`). 본인 손작업 없음. 학습 가치 < 효율(Phase 04 면담 데모 압박).
- Has Exit Time = **false** (즉시 전환), Duration = **0** (블렌딩 X) → 픽셀아트 컨벤션. 부드러운 3D 캐릭터면 0.1~0.3 권장이지만 픽셀은 sprite 겹침 흐림 발생 → 0.

## 막혔던 지점

- **드래그 부산물 Controller 재발** — Phase 02 함정 그대로. *Rule of Three 확정* (M1 Phase 04 worldPositionStays + M2 Phase 01 36 PNG drift + M2 Phase 02 drag 중복 GameObject + 본 Phase Walking 부산물 controller). 같은 패턴 = "한 워크플로우 의도 ≠ 실제 결과 일관 X — 매번 검증" 시니어 마인드셋.
- **MCP `AssetDatabase.DeleteAsset` user interaction 에러** — confirmation 다이얼로그 시도 → MCP 호출 차단. `MoveAssetToTrash`로 우회. 새 패턴: *MCP에서 destructive 작업은 trash 이동이 안전*. Phase 02 Square 삭제는 `result.DestroyObject` (Scene Object 전용)이라 OK였음 — Asset 삭제와 다른 API 분기.

## 학습 일지 후보 키워드

- `unity-drag-workflow-traps` — Phase 02에서 박은 키워드, 본 Phase에서 재발 ★. *Rule of Three* 도달.
- `unit-correctness-vs-end-to-end-revisited` — 시드 키워드, M1 P04 + M2 P01 + M2 P02 + M2 P03 *4 사건 누적*. 면접 결정타.
- `animator-controller-api-automation` — AddParameter / AddTransition / AddCondition / AnimatorConditionMode.If(IfNot) 패턴. 학부생이 *손작업 학습 끝나면* API로 자동화 가성비.
- `mcp-asset-deletion-patterns` — `DeleteAsset` vs `MoveAssetToTrash` vs `DestroyObject` 분기. MCP의 user interaction 제약 이해.
- `mecanim-transition-parameters` — Bool Parameter + If/IfNot Condition Mode + Has Exit Time 의미 + 픽셀아트 Duration 0 컨벤션. `/journal:concept` 후보.
