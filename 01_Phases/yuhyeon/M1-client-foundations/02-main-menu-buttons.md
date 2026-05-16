# Phase 02: 메인 메뉴 버튼 — 시작 / 종료

> **상태**: pending
> **마일스톤**: M1-client-foundations
> **예상 소요**: 1.5~2시간
> **담당 에이전트**: client

---

## 🎯 목표

MainMenu 씬에 "시작" / "종료" 버튼 2개 추가. "시작" 클릭 → Gameplay 씬 로드. "종료" 클릭 → 어플 종료 (에디터는 Play 모드 정지). 첫 *상호작용*하는 UI.

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (MainMenu 씬 + Canvas 존재)
- [x] Gameplay.unity 씬 존재 (팀장 M2 작업물)

---

## 📝 작업 내용

- [ ] MainMenu Canvas 안에 Vertical Layout Group 부착된 빈 GameObject "ButtonGroup" 추가 (정렬용)
- [ ] ButtonGroup 안에 Button - TextMeshPro 2개 추가: "시작" / "종료"
- [ ] `03_Client/Assets/Scripts/UI/` 폴더 신설 (없으면)
- [ ] `Scripts/UI/MainMenuController.cs` 작성:
  - `public void OnStartClicked()` → `SceneManager.LoadScene("Gameplay")`
  - `public void OnQuitClicked()` → `Application.Quit()` + 에디터에서는 `UnityEditor.EditorApplication.isPlaying = false` (#if UNITY_EDITOR)
- [ ] MainMenu 씬에 빈 GameObject "UIController" + MainMenuController 컴포넌트 부착
- [ ] 시작 버튼 onClick → UIController.OnStartClicked (Inspector 드래그)
- [ ] 종료 버튼 onClick → UIController.OnQuitClicked
- [ ] Build Settings에 Gameplay 씬도 추가됐는지 확인 (없으면 추가)
- [ ] 씬 저장

---

## ✅ 완료 조건

- [ ] Play 모드에서 시작 클릭 → Gameplay 씬으로 전환됨
- [ ] Play 모드에서 종료 클릭 → Play 모드 정지 (Console에 종료 로그 한 줄)
- [ ] 마우스 호버 시 버튼 색상 변화 (Unity 기본 transition)
- [ ] Console에 Error 0개

---

## 🧪 테스트

**자동 테스트:**
- 없음 (UI 인터랙션 자동 검증은 PlayMode 테스트 도입 후로 미룸)

**수동 테스트:**
- 시작 클릭 → Gameplay 씬 캐릭터 보이는지 확인
- 종료 클릭 → 에디터 정지 확인
- 키보드 Tab으로 버튼 간 포커스 이동 가능한지 (EventSystem 정상 작동)

---

## 📚 학습 포인트

- **SceneManager.LoadScene("name") vs LoadScene(index)**: 이름이 안전 (인덱스 바뀌어도 안 깨짐).
- **Application.Quit()의 에디터 함정**: 에디터에선 동작 X. 빌드된 실행파일에서만 종료. 디버그 편의 위해 `#if UNITY_EDITOR` 분기.
- **Button.onClick 등록 2가지**:
  - *Inspector 드래그* (이번 Phase) — 시각적, 학부생 친화. 단점: 코드만 봐선 연결 안 보임.
  - *코드 AddListener* — 코드로 명시적. 동적 생성 시 필수.
- **Vertical Layout Group**: 자식들을 자동 세로 배치. 버튼 추가/삭제해도 간격 자동 정렬.
- **헌법 1번 (Server Authority)**: 시작 버튼이 *서버 연결*까지 자동으로 트리거하지 않음. 이 Phase는 *씬 로드*만. 서버 연결은 별 마일스톤(M3+).

---

## ⚠️ 함정 / 주의사항

- 씬 이름 오타 (`Gameplay` ≠ `gameplay`) → 런타임 에러. Build Settings의 정확한 이름 복사.
- onClick 이벤트에 Inspector 드래그 안 하면 클릭 무 동작 (조용히 실패).
- Application.Quit이 에디터에서 동작 안 한다고 "버튼 망가졌다" 오해 — 빌드해서 테스트 필요.
- Vertical Layout Group 없이 버튼 직접 배치하면 화면 비율 바뀔 때 어그러짐.
- 헌법 1번 위반 유혹: "시작 누르면 캐릭터 만들어서 인벤토리 채우자" — X. 그건 서버. 클라는 표시만.

---

## ➡️ 다음 Phase

Phase 03 — HUD 골격: Gameplay 씬에 HP바·자원·미니맵 자리잡이 (mock 값).

---

## 작업 로그

- 2026-05-17: /work:plan으로 생성
