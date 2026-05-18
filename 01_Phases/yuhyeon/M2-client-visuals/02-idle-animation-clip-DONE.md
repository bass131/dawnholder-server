---
summary: Player_Idle.anim AnimationClip(4 frame, FPS 12, Loop True, sprite swap only — position 키프레임 0) + Player.controller AnimatorController(1 layer, default state Player_Idle) + Player GameObject에 Animator 컴포넌트(updateMode Normal, applyRootMotion False) 박음. 드래그 자동 워크플로우 부분 누락(Controller 이름 = sprite 이름 / Animator 컴포넌트 미추가) + sprite 중복 GameObject 사고를 MCP 자동 fix로 봉합. 임시 바닥 Square placeholder는 의도 미확인 후 삭제했다가 원본에서 복원.
phase: 02-idle-animation-clip
work-id: yuhyeon-m2-phase02-idle-animation-clip
status: done
completed_at: 2026-05-19
commit: TBD
---

# Phase 02 — Idle 애니메이션 클립 + Animator Controller 완료 박제

**소요 시간**: ~1.5시간 (드래그 작업 10분 + drift/누락 발견 fix 40분 + Square 복원 20분 + 검증 박제 20분)

## TL;DR

knight14_idle_00~03 4 frame을 *드래그 워크플로우*로 Player_Idle.anim 자동 생성 + AnimationClip 12 FPS / Loop True / position 키 0 (Phase 04 텔레포트 회피)까지 완벽. Controller 자동 생성됐으나 *이름이 sprite 이름(knight14_idle_00)* + *Player GameObject에 Animator 컴포넌트 미추가* 두 누락 발견 → MCP 자동 fix로 rename + 컴포넌트 추가 + updateMode Normal(M1 timeScale=0 일시정지 정합) 박음. 추가로 Hierarchy에 중복 sprite GameObject 발견(드래그 함정) + 의도 미확인 Square placeholder 삭제 사고 → Gameplay.unity 원본에서 Instantiate로 복원. Phase 03 Run 추가 시 같은 Controller에 State 추가만 하면 됨.

## 5단계 보고

- **무엇을 만들었나** — `Assets/Animations/Player/Player_Idle.anim` (length 0.333s, frameRate 12, isLooping True, sprite swap 1 트랙 4 keys, position/property bindings 0) + `Player.controller` (1 layer, 1 state `Player_Idle`, default state 정상). Player GameObject Animator 컴포넌트(runtimeAnimatorController=Player, updateMode=Normal, applyRootMotion=False). GameplayTest 씬에 Player + Square(임시 바닥, scale 20×0.5×1 pos (0,-2,0)) 둘만 남김.
- **왜 필요한가** — 캐릭터가 가만히 있어도 *살아있어 보이는* 시각 효과 = 면담 데모의 기본선. Mecanim State Machine 첫 도입으로 Phase 03(Idle↔Run 전환) + Phase 04(이동 wiring) 토대 마련. M1 Phase 04 결정인 *timeScale=0 일시정지*가 Animator에도 자연 정합(updateMode Normal)되어 흐름 일관.
- **어떻게 만들었나** — 결정 1: 드래그 자동 워크플로우 vs 수동 AnimationClip 생성 → 자동(픽셀아트 표준 패턴). 결정 2: Controller 이름 = sprite 이름(`knight14_idle_00`) 발견 후 → `Player`로 rename(Phase 03 Run 추가 시 동작 무관한 이름이 깔끔). 결정 3: drift 발견 — Animator 컴포넌트 미추가 + Hierarchy 중복 sprite GameObject → MCP 자동 fix. 결정 4: Square 의도 확인 누락(임시 바닥)으로 삭제 사고 → Gameplay.unity Additive 로드 후 Instantiate + MoveGameObjectToScene로 복원. 새 개념 = AnimatorController API + AssetDatabase.RenameAsset + EditorSceneManager.OpenScene(Additive) + MoveGameObjectToScene 패턴.
- **테스트 결과** — MCP 검증 다축:
  - AnimationClip 실측: length=0.333s, frameRate=12, isLooping=True, wrapMode=Default, sprite swap 4 keys
  - Controller: 1 layer / 1 state (Player_Idle) / default state 정상
  - Animator 컴포넌트: runtimeAnimatorController=Player, updateMode=Normal, applyRootMotion=False
  - 씬 정리: 3 sprite GameObject(Player + 중복 knight14_idle_00 + Square) → 정리 → 2(Player + 복원 Square)
  - Scene View 캡처: 캐릭터 + 회색 가로 바닥 정상 시각
  - 본인 Play 회귀 보고: "잘 움직임 + 모두 정상 작동"
- **다음 스텝** — Phase 03 진입(Walking_KG_1 → Player_Run.anim + Animator Run State 추가 + `IsMoving` bool Parameter + Idle↔Run Transition). 같은 드래그 워크플로우 패턴이지만 *Controller에 State 추가만* 하면 됨(rename 학습은 끝남). 핀 갱신.

## AC 검증 결과

```bash
# 1. MCP RunCommand: AnimationClip 실측
Player_Idle.anim: length=0.3333333s, frameRate=12, loop=True, wrapMode=Default
Property bindings: 0, Object bindings (sprite swap): 1
  ObjectBinding: ..m_Sprite (4 keys)

# 2. Controller 검증 (rename 후)
Player.controller: layers=1, states in base layer=1, default state=Player_Idle

# 3. Animator 컴포넌트 검증
Animator: controller=Player, updateMode=Normal, applyRootMotion=False

# 4. 씬 정리 (drift fix)
Before: 3 sprite GameObjects (Player + knight14_idle_00 [duplicate] + Square)
Action: Deleted knight14_idle_00 (drag 함정 잔재) + Square (의도 미확인 삭제 사고)
Restore: Square 의도(임시 바닥) 확인 후 Gameplay.unity 원본에서 Instantiate 복원
After: 2 sprite GameObjects (Player + Square)

# 5. Scene View 캡처 시각 검증
캐릭터(0,0,0) + 회색 가로 바닥(0,-2,0 scale 20×0.5) 정상 위치 + 또렷 픽셀

# 6. 본인 Play 회귀 보고
"잘 움직이고 있고 모두 정상 작동" — 4 frame Idle 반복 + ESC 일시정지 / 재개 흐름 정상
```

## 결정 흐름

- 드래그 자동 vs 수동 AnimationClip 생성 → **자동** (Unity 표준 워크플로우, 시간 절약).
- Controller 이름 `knight14_idle_00` 유지 vs `Player`로 rename → **rename**. Phase 03+ Run/Attack State 추가 시 *동작 무관한 이름*이 의미 명확.
- Animator 컴포넌트 미추가 fix: 본인 손 vs MCP 자동 → **MCP 자동** (Phase 01 drift fix 패턴 재사용, 면담 1일 압박).
- Square 삭제 결정: 의도 미확인 + 본인 답 "둘 다 삭제" → 사실은 *임시 바닥 placeholder* → **복원**. *Gameplay.unity 원본 참조 패턴*으로 정보 복사. → 교훈: 삭제 옵션 던지기 전 *의도 한 번 더 확인*.

## 막혔던 지점

- **드래그 워크플로우 부분 누락 발견** — AnimationClip은 정상 생성됐는데 (a) Controller 이름이 sprite 이름 (b) Player GameObject에 Animator 컴포넌트 미추가. 드래그 위치/타이밍에 따라 *워크플로우 결과 일관 X*. MCP RunCommand로 fix.
- **Hierarchy 중복 sprite GameObject (`knight14_idle_00`)** — sprite를 Hierarchy 빈 공간에 드래그 시 자동 GameObject 신설 함정. Player와 겹쳐 *캐릭터 2개로 보임*. **이번 사건은 M1 Phase 04 worldPositionStays + Phase 01 36 PNG drift와 Rule of Three** — 모두 "의도와 실제 결과 사이 검증 누락" 패턴. 면접 결정타.
- **Square 의도 미확인 삭제 사고** — 직전 응답에서 Claude가 "본인 의도 모르면 삭제 OK" 옵션 던짐 → 본인 *임시 바닥 placeholder* 의도 모른 채 삭제 → 본인이 다음 메시지에서 알려줘서 복원. **새 학습**: destroy 직전 *의도 한 번 더 묻기* 절차가 시니어 멘토링 톤.

## 학습 일지 후보 키워드

- `unity-drag-workflow-traps` — Hierarchy 빈 공간 드래그 시 GameObject 자동 신설 + Controller 이름이 첫 sprite 이름으로 박힘 + Animator 컴포넌트 누락 가능. 의도와 결과 사이 검증 누락 패턴(★★★ Rule of Three).
- `mecanim-state-machine-fundamentals` — AnimationClip vs AnimatorController 분리 (데이터 vs 흐름) + Default State + Loop Time + Sample Rate 학부 수준 첫 개념 정리. `/journal:concept` 후보.
- `delete-decision-intent-check` — ★ 새 키워드. 삭제 권유 전 *의도 한 번 더 확인* 절차. Square 사건. 시니어 멘토링 톤 학습.
- `animator-update-mode-timescale` — updateMode Normal/UnscaledTime/AnimatePhysics 의 trade-off. M1 Phase 04 결정인 일시정지 timeScale=0이 Animator에도 자연 정합되는 패턴.
- `unit-correctness-vs-end-to-end-revisited` — 직전 Phase 01에서 박은 키워드, Phase 02 드래그 중복 사고로 Rule of Three 도달. M2 마감 시 `/journal:concept`로 펼침.
