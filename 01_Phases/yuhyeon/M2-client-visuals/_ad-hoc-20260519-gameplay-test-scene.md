# ad-hoc — GameplayTest 씬 신설 + MainMenu Start 라우팅 임시 변경

> **상태**: pending
> **트리거**: Phase 01 시작 직전 발견 — sprite 배치할 *씬*이 필요한데 Gameplay.unity는 공유 영역(팀장+인규+본인). 본인 학습용 sandbox 씬 필요.
> **예상 소요**: 30분~1시간
> **담당 에이전트**: client
> **떼어낸 이유**: Phase 01 scope가 *sprite import 컨벤션*에 박혀있어 *씬 인프라 셋업*까지 흡수하면 헌법 위반("Phase 안에서 scope 늘리지 말기"). M1 ad-hoc UI Scene 분리(ADR-021)와 같은 패턴.

---

## 🎯 목표

MainMenu의 시작 버튼이 새로 만든 `GameplayTest.unity` 씬으로 라우팅되게. 본인 M2 sprite/애니/이동 학습이 *공유 영역 Gameplay.unity*를 오염시키지 않게.

**끝나면 데모 가능한 것**: MainMenu → 시작 → 페이드 → GameplayTest 씬(빈 무대) → ESC → 메인 메뉴 복귀까지 *기존 M1 흐름 그대로* 단 도착지만 GameplayTest.

---

## ⏪ 사전 조건

- [ ] M1-client-foundations 마감 (MainMenu / SceneTransition / PauseMenu 박힘)
- [ ] `Scripts/UI/MainMenuController.cs` 본인 영역 인지 (CODEOWNERS @jungyoohyun0105 ✓)
- [ ] `Scripts/UI/SceneTransition.cs` Singleton 패턴 인지

---

## 📝 작업 내용

- [ ] **`Assets/Scenes/GameplayTest.unity` 신설**:
  - Unity Editor → File → New Scene → 2D template → Save As → `Assets/Scenes/GameplayTest.unity`
  - 또는 `Gameplay.unity` 복제 후 GameplayTest로 rename (필요한 컴포넌트 미리 박혀있음)
  - **권장**: 빈 2D 씬 (캐릭터·배경 0에서 시작이 학습 명확)
  - Main Camera + Directional Light(기본 들어옴)만 두기
- [ ] **Build Settings 등록**:
  - File → Build Settings → 현재 열린 GameplayTest 씬을 *Scenes In Build* 리스트에 추가
  - ⚠️ `ProjectSettings/EditorBuildSettings.asset`는 공유 영역 → 일단 *로컬에만* 박고 commit은 면담 후 별도 PR로 정리 (학습 임시 단계 OK)
- [ ] **`MainMenuController.cs` 임시 hardcode** (본인 영역):

  ```cs
  public void OnStartClicked()
  {
      // M2 학습용 임시 — 면담 후 gameplaySceneName Inspector 값으로 복원
      // 이유: Inspector 박힌 "Gameplay"가 SerializeField 기본값을 override하므로
      //       Inspector 수정 없이 라우팅 변경하려면 hardcode 필요
      //       (Inspector 변경은 MainMenu.unity = 공유 영역 = 팀장 PR 필요)
      SceneTransition.Instance.LoadScene("GameplayTest");
  }
  ```

- [ ] **PauseMenu → MainMenu 복귀 흐름 점검**: PauseMenuController의 "메인 메뉴" 버튼은 그대로 `MainMenu` 씬으로 → 변경 X. 단 GameplayTest에서도 ESC → 일시정지 → 메인 메뉴 흐름이 동작하는지 확인 (UI.unity Additive Load + PauseMenuController가 *모든 씬*에서 작동해야 함)

---

## ✅ 완료 조건

- [ ] `Assets/Scenes/GameplayTest.unity` + `.meta` 파일 존재 (commit 여부는 별도 결정)
- [ ] Build Settings 리스트에 GameplayTest 박힘 (로컬 OK)
- [ ] `MainMenuController.cs:18-21` OnStartClicked()에 hardcode + 주석 박힘 (git 추적)
- [ ] Play 진입 시 시작 클릭 → 페이드 → GameplayTest 로드 → 페이드 인 정상 (검은 화면 멈춤 X)
- [ ] GameplayTest에서 ESC → 일시정지 메뉴 정상 표시 + 재개 + 메인 메뉴 복귀 모두 동작
- [ ] Console에 SceneTransition 에러 로그 0건 ("not in Build Settings" 등 X)

---

## 🧪 테스트

**수동 테스트:**
1. Play 진입 → MainMenu → 시작 클릭 → GameplayTest 씬 로드 (페이드 정상)
2. GameplayTest에서 ESC → 일시정지 메뉴 → 재개 → 정상 복귀
3. 다시 ESC → 메인 메뉴 클릭 → MainMenu로 페이드 복귀
4. 5회 반복 (메뉴 → GameplayTest → 메뉴 → GameplayTest → ...) → 메모리 누수 또는 페이드 깨짐 0

**자동 테스트:** 없음

---

## 📚 학습 포인트

- **CODEOWNERS 권한 모델**: `MainMenu.unity` (씬 파일) = 공유 영역 / `MainMenuController.cs` (코드) = 본인 단독. Unity 프로젝트는 *같은 기능*이 코드와 데이터에 나뉘어 있어 *어느 쪽을 수정하느냐*로 영역 결정.
- **SerializeField Inspector override**: `[SerializeField] string foo = "default"`는 Inspector에 박힌 값이 *기본값을 덮음*. 코드 기본값만 바꿔도 효과 X — Inspector도 같이 바꿔야 함. 단 Inspector = 씬 파일 일부라 권한 다름.
- **Build Settings = 공유 자원**: `EditorBuildSettings.asset` 변경은 팀 차원. 본인 로컬 변경은 *commit 안 하면* 다른 머신에 안 퍼짐 — 정식 PR로 박는 시점 결정 필요.
- **임시 hardcode 패턴**: 학습/sandbox 단계에서 *명확한 주석 + 복원 약속*과 함께 쓰는 임시 변경. 안 박으면 잊고 출시 → 학습 일지·-DONE.md에 박제 의무.

---

## ⚠️ 함정 / 주의사항

- **Build Settings 미등록**: `SceneManager.LoadSceneAsync("GameplayTest")` 호출 시 op == null 반환 → SceneTransition.cs:73 에러 로그 + 페이드 인으로 복구. 즉 검은 화면 멈춤은 *방어 코드 덕에 회피*하지만 씬 전환 자체는 실패. Build Settings 등록 *반드시*.
- **MainMenu.unity Inspector 값 변경 유혹**: Inspector에서 gameplaySceneName 값을 "GameplayTest"로 바꾸는 게 더 깔끔해 보이지만 → MainMenu.unity 수정 = 공유 영역 PR. 시간 압박 시 코드 hardcode가 빠름. 단 *임시*임을 명시.
- **GameplayTest.unity commit 결정**: 공유 영역에 새 씬 추가도 팀장 승인. 본인 로컬만 작업하다 머신 바꾸면 사라짐 → 면담 직전엔 영상 박제 필수 (영상은 본인 영역 = 학습 일지 폴더).
- **PauseMenuController가 모든 씬에서 작동하는지**: UI.unity Additive Load라 *MainMenu / Gameplay / GameplayTest* 모두에서 같은 UI 적용. 단 MainMenu에선 ESC 안 먹어야 정상 — PauseMenuController가 *현재 씬*을 판별하는지 확인 (M1 Phase 04 결과물).
- **복원 약속 잊기**: 학습 끝나고 hardcode 안 되돌리면 출시 후 *플레이어가 GameplayTest로 빠지는 사고*. -DONE.md에 명시 + M2 마감 회고에 박제.

---

## ➡️ 후속

ad-hoc 완료 후:
- **Phase 01 진입** (sprite import). 작업 씬을 *GameplayTest*로 명시
- Phase 01 파일 갱신: "Gameplay 씬에 배치" → "GameplayTest 씬에 배치"
- 핀 다시 Phase 01로 갱신
- 면담 후 또는 M2 마감 시점 = MainMenuController hardcode 되돌리기 + MainMenu.unity Inspector를 "GameplayTest"로 정식 변경 PR (팀장 합의 필요)

---

## 작업 로그

- 2026-05-19: 시작 (Phase 01 시작 직전 발견 — 본인 sandbox 씬 필요성 자각)
- YYYY-MM-DD: 완료. 학습한 것: ...
