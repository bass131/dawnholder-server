---
summary: MainMenu Start 버튼이 새 GameplayTest.unity 씬으로 라우팅되도록 MainMenuController.OnStartClicked()에 임시 hardcode + TODO 마커 박음. 본인 학습 sandbox 씬 확보로 Phase 01~07 sprite/애니/배경 작업이 공유 영역 Gameplay.unity 오염 X.
phase: ad-hoc-20260519-gameplay-test-scene
work-id: yuhyeon-m2-adhoc-gameplay-test-scene
status: done
completed_at: 2026-05-19
commit: TBD
---

# ad-hoc 2026-05-19 — GameplayTest 씬 신설 + MainMenu Start 라우팅 임시 변경 완료 박제

**소요 시간**: ~30분

## TL;DR

M2-client-visuals Phase 01 시작 직전 발견 — sprite 배치할 씬이 *공유 영역 Gameplay.unity*라 본인 학습이 다른 팀원 영역 오염 우려. M1 ad-hoc UI Scene 분리(ADR-021)와 같은 패턴으로 *Phase 01 scope 늘리지 않고* 별도 ad-hoc으로 떼어 GameplayTest.unity 신설 + MainMenuController.cs 본인 영역(CODEOWNERS @jungyoohyun0105 단독) 안에서 코드 hardcode로 라우팅 변경. MainMenu.unity 자체는 안 만짐(공유 영역 회피).

## 5단계 보고

- **무엇을 만들었나** — `Assets/Scenes/GameplayTest.unity` 신설(Gameplay.unity 복제) + Build Settings 등록(로컬, commit은 면담 후) + `MainMenuController.cs:17-22` OnStartClicked() 임시 hardcode + TODO(yuhyeon, M2 학습 마감 시 복원) 주석 2줄.
- **왜 필요한가** — Phase 01~07이 sprite/애니/배경/한글화를 *어딘가에* 배치해야 하는데 Gameplay.unity는 공유 영역. 본인 학습 자원이 다른 팀원 작업과 충돌하면 git 머지 분쟁 + 정식 게임 씬 오염. Sandbox 씬으로 격리.
- **어떻게 만들었나** — 결정 흐름: ① Phase 01에 흡수 vs ② ad-hoc 분리 vs ③ Editor 직접 Play → 본인이 *MainMenu→시작→테스트* 데모 흐름 원함 + 헌법 "scope 늘리지 말기" 정합 → ②. 라우팅 변경 방법: ① MainMenu.unity Inspector 변경 vs ② 코드 hardcode → 후자가 본인 영역 단독 + 팀장 PR 불필요 → ②. 새 개념 = *SerializeField Inspector override* (Inspector 값이 코드 기본값을 항상 덮음 → 코드만 바꿔서는 효과 X, 직접 LoadScene 인자 hardcode 필요).
- **테스트 결과** — 본인 수동 Play 회귀 5/5 통과: ① MainMenu→시작 페이드 전환 / ② GameplayTest 로드 / ③ ESC→일시정지 메뉴 표시 / ④ 재개+메인 메뉴 복귀 / ⑤ 5회 왕복 시 Console 에러 0건·페이드 깨짐 0건. UI.unity Additive Load가 GameplayTest에서도 트리거되어 PauseMenuController가 모든 씬 정합 유지 확인.
- **다음 스텝** — Phase 01 진입(Knight spritesheet 임포트 + Import 컨벤션 박제 + Sprite Editor grid slice + GameplayTest 씬에 정적 배치). 핀을 Phase 01로 갱신. M2 마감 시 hardcode 복원 의무 — `grep "TODO(yuhyeon" 03_Client/Assets/Scripts/UI/` 한 방으로 회수 가능.

## AC 검증 결과

```bash
# 1. 파일 존재 확인 (본인 Unity Editor에서 Save 후)
$ ls 03_Client/Assets/Scenes/GameplayTest.unity 2>&1
   존재 (본인 보고)

# 2. MainMenuController 변경 적용 확인
$ grep -n 'LoadScene("GameplayTest")' 03_Client/Assets/Scripts/UI/MainMenuController.cs
   20:            SceneTransition.Instance.LoadScene("GameplayTest");

# 3. TODO 마커 박힘 확인
$ grep -n 'TODO(yuhyeon' 03_Client/Assets/Scripts/UI/MainMenuController.cs
   18:            // TODO(yuhyeon, M2 학습 마감 시 복원): Inspector 값("Gameplay") override 회피용 임시 hardcode.

# 4. 본인 수동 Play 회귀 (5/5 통과 — 본인 보고 "문제없이 진행됨")
   - MainMenu→시작→GameplayTest 페이드 전환 ✓
   - ESC→일시정지 메뉴 ✓
   - 재개 → 메뉴 닫힘 ✓
   - 메인 메뉴 클릭 → MainMenu 복귀 ✓
   - 5회 왕복 시 Console 에러 0 + 페이드 깨짐 0 ✓
```

## 결정 흐름

- ad-hoc vs Phase 01 흡수 vs 흡수 안 함 → **ad-hoc 분리**. 이유: 30분~1h 자리라 1~3h Phase 경계 흐림 + 헌법 "scope 늘리지 말기" 정합 + 면접 가치(scope 신호 알아채기).
- MainMenu.unity Inspector 변경 vs 코드 hardcode → **코드 hardcode**. 이유: MainMenu.unity = 공유 영역(팀장 PR), MainMenuController.cs = 본인 단독. 면담 2일 압박 시 본인 영역 내 처리가 빠름. 단점 = SerializeField 의도(Inspector 노출)와 어긋남 → TODO 마커로 *임시* 명시.
- GameplayTest = Gameplay.unity 복제 vs Basic 2D 빈 씬 → **복제**. 이유: SceneBootstrap 등 M1 결정 셋팅이 자동 따라옴 → 일시정지·HUD 회귀 즉시 동작 → 학습 sandbox 시작점 단순화.
- GameplayTest + EditorBuildSettings.asset commit 시점 → **면담 후로 미룸**. 이유: 두 파일 공유 영역. 학습 단계엔 로컬만 OK. 면담 영상은 별도 학습 일지 폴더(본인 영역) commit.

## 막혔던 지점 (있다면)

없음. ad-hoc 자체가 발견-결정-실행 한 사이클로 30분 안에 끝남. *대신* 같은 사이클에서 부가 발견 2건:
- `Scripts/Input/LocalPlayerController.cs` + `Scripts/Prediction/PlayerPredictor.cs` 이미 존재 — 팀장이 prediction 골격 박아둠. Phase 04 작업이 *새 PlayerController 만들기*가 아니라 *기존 LocalPlayerController에 sprite 시각 wiring 추가* 방향이 맞을 가능성. Phase 04 진입 *전* Phase 04 파일 갱신 필요.
- `03_Client/CLAUDE.md`의 "새 Input System 패키지 사용. 레거시 Input.GetKey 금지" 규정 — Phase 04 파일이 `Input.GetAxisRaw` 사용 기준이라 갱신 필요. 두 사항 모두 핀 "Phase 04 영향" 메모로 박힘.

## 학습 일지 후보 키워드

- `codeowners-permission-model` — Unity 프로젝트의 *같은 기능이 코드(개인 영역)와 씬 파일(공유)에 나뉘어* 권한 다른 패턴. 시니어 면접 어필 가능.
- `serializefield-inspector-override` — `[SerializeField] string foo = "default"`에서 Inspector 값이 코드 기본값 override. 학부생 단계 학습 가치 명확.
- `temporary-hardcode-with-todo-marker` — sandbox 학습 단계의 임시 우회 + 복원 약속 패턴. `grep "TODO(<owner>"` 한 방 회수.
- `scope-discipline-ad-hoc-split` — Phase scope 늘리지 않고 ad-hoc으로 떼어내는 헌법 실천 사례. M1 ad-hoc UI Scene 분리와 같은 패턴이라 *Rule of Two*.
- `unity-build-settings-shared-asset` — `EditorBuildSettings.asset` = 공유 자원, 팀 commit 결정 필요. 학습용 sandbox는 로컬 변경만으로 우회 가능.
