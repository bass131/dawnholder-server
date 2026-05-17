# Phase 04 — DONE (부분 완료)

> **상태**: 부분 완료 — 코드/구조/이벤트 연결 100% 완료, 시각 렌더링 검증 보류
> **마일스톤**: M1-client-foundations
> **완료일**: 2026-05-17
> **소요 시간**: 약 3.5시간 (코드 30분 + Unity 손작업 + 시각 디버깅 1.5시간)
> **담당**: 정유현 (@jungyoohyun0105)

---

## 🎯 무엇 — 완료된 것

### 코드 산출물
- **`03_Client/Assets/Scripts/UI/PauseMenuController.cs`** (신규)
  - `Dawnholder.Client.UI` 네임스페이스
  - `[SerializeField] GameObject pauseCanvas` + `[SerializeField] InputActionReference togglePauseAction`
  - `Toggle()` / `OnResumeClicked()` / `OnMainMenuClicked()` / `OnQuitClicked()` 4 메서드
  - `OnEnable`/`OnDisable`에서 InputAction `performed` 콜백 등록·해제
  - `OnMainMenuClicked`에서 `Time.timeScale = 1f` 복원 후 씬 전환 (함정 회피)
  - 헌법 #1 (Server Authority) 주석 명시 — timeScale 0는 *본인 클라만* 정지

### Unity 산출물
- **InputSystem_Actions** — UI Action Map에 `TogglePauseMenu` 액션 + `<Keyboard>/escape` Binding 추가
- **`Assets/Scenes/UI.unity`** — PauseMenuCanvas 신설:
  - Canvas Sort Order = 20 (HUD Canvas 위)
  - MenuRoot 자식 (풀스크린 stretch, 시작 시 `SetActive(false)`)
  - Backdrop (풀스크린 stretch, 검은색)
  - MenuPanel (중앙 300×240, Vertical Layout Group, Padding 20, Spacing 15)
  - ResumeButton / MainMenuButton / QuitButton + 각자 LayoutElement(Preferred Height 60)
- **PauseMenuController** 부착 + Inspector 필드 드래그 + 버튼 OnClick 이벤트 3개 연결 (Resume/MainMenu/Quit)

### 부수 작업 (팀장 영역 임시 패치 — PR에 명시)
- **`Dawnholder.Client.asmdef`** — `references`에 `Unity.TextMeshPro` 추가 (팀장 누락 복구)
- **`LocalPlayerController.cs:64`** — `(int)_localTickCounter` 캐스트 추가 (PDL int↔uint 불일치 임시 패치)
- **`UnityClientSession.cs:145`** — `(uint)pkt.lastAckedClientTick` 캐스트 추가 (동일 이유)
- 두 캐스트 모두 `TEMP-yuhyeon-20260517:` 마커 주석 — 팀장 PDL 재생성 후 제거 가능

### Phase 정의 정정
- **`04-pause-menu-input.md`** — 작업 내용/로그에 "PauseMenuCanvas는 UI.unity (ADR-021 영역 분리 정책 일관성)" 사후 정정 1줄 추가

---

## 🤔 왜 — 결정 흐름

### PauseMenuCanvas 배치 → UI.unity (Phase 정의의 Gameplay.unity 아님)
- **이유**: ad-hoc UI Scene 분리(ADR-021, 2026-05-17)가 본 Phase 정의 작성 후에 결정됨. UI 컴포넌트 한 씬 집중 + 팀장 영역(.unity)과의 머지 충돌 차단을 위해 영역 분리 정책 일관성 유지.
- **trade-off**: Phase 정의-구현 불일치 발생 → 정의 파일에 사후 정정 1줄 박아 학습 추적 가능.

### PauseMenuController는 *항상 켜진* PauseMenuCanvas에 부착, 토글 대상은 *자식* MenuRoot
- **이유**: Controller가 부착된 GameObject가 OFF되면 `OnEnable`에서 등록한 InputAction 콜백이 풀려서 → ESC 재누름이 안 잡힘.
- **두 층 분리 패턴**:
  - PauseMenuCanvas (항상 ON) ← PauseMenuController
  - MenuRoot (토글 대상) ← Backdrop + MenuPanel

### `Time.timeScale = 0` 채택 (Player Action Map 비활성화 패턴 미적용)
- **이유**: 학습 단계 간단화. 이번 Phase는 *클라 단독*이라 timeScale로 충분.
- **다음 마일스톤**: 멀티게임 진입 시 Player Map/UI Map 분리 패턴 도입 (메뉴 떴을 때 Player 입력 차단).
- **헌법 #1**: timeScale 0는 *본인 클라만* 정지. 서버 권위 타임라인엔 영향 X (멀티게임에서 다른 플레이어는 계속 움직임).

### `Button - TextMeshPro` 채택 (legacy Button 아님)
- **이유**: HudController가 이미 TMP 사용 + SDF 렌더링으로 어느 크기에서도 깨끗.
- **함정**: 한글 글리프 LiberationSans에 없음 → 일단 영어(`Resume`/`Main Menu`/`Quit`)로 박음. TMP 한글 폰트 도입은 별도 작업 큐(`2026-05-16-tmp-korean-font-todo.md`).

### LayoutElement로 버튼 Height 60 통제
- **이유**: VerticalLayoutGroup이 자식 RectTransform Height를 driven할 때, 자식이 "내가 원하는 크기는 이거"라고 hint 주는 표준 메커니즘.
- **trade-off**: 컴포넌트 +1개 vs Control Child Size: Height 해제 후 RectTransform 직접 입력 (간단하지만 우회).

---

## 🛠️ 어떻게 — 구조

```
UI.unity
├── HUD Canvas (Phase 03, Sort Order 0)
├── EventSystem
└── PauseMenuCanvas (Sort Order 20) ← PauseMenuController 부착
    └── MenuRoot ⬜ (풀스크린 stretch, 시작 SetActive(false), 토글 대상)
        ├── Backdrop (풀스크린 stretch, 검은색 Image)
        └── MenuPanel (중앙 300×240, Vertical Layout Group)
            ├── ResumeButton (LayoutElement Preferred Height 60) → OnResumeClicked
            ├── MainMenuButton (동일) → OnMainMenuClicked
            └── QuitButton (동일) → OnQuitClicked
```

**InputSystem_Actions** → UI 맵 → `TogglePauseMenu` 액션 (Binding: `<Keyboard>/escape`, Type: Button) → PauseMenuController.OnEnable에서 `performed += OnTogglePerformed` 등록.

---

## 🧪 테스트 — 부분 완료

| # | 항목 | 결과 |
|---|------|------|
| ① | ESC 콜백 동작 (코드 레벨) | ✅ Hierarchy에서 MenuRoot 토글 ON/OFF 확인 |
| ② | PauseMenuController 컴파일 | ✅ |
| ③ | Inspector 필드/이벤트 연결 | ✅ pauseCanvas, togglePauseAction, OnClick 3개 |
| ④ | 시각 검증 — ESC 누를 때 화면에 메뉴 표시 | ❌ **미통과** — Game 뷰에 메뉴 안 보임 |
| ⑤ | 재개/메인 메뉴/종료 버튼 시각 클릭 | ❌ 메뉴 안 보여서 검증 불가 |
| ⑥ | timeScale 정지/복원 | ⚠️ 코드는 정상, 시각 확인 못 함 |
| ⑦ | 5회 빠른 토글 | ⚠️ 시각 확인 못 함 |

### 미통과 원인 (조사 중)
- RectTransform (MenuRoot/Backdrop/MenuPanel) 모두 풀스크린 stretch 또는 의도된 중앙 배치로 박힘
- ESC 콜백 동작 + Hierarchy에서 MenuRoot 토글 확인됨 → **코드 100% 정상**
- 그럼에도 Game 뷰에 메뉴가 안 그려짐
- 가능 원인 후보:
  - Backdrop Image Color α 값이 실제로 0 (Color picker 확인 안 됨)
  - PauseMenuCanvas의 Canvas 컴포넌트가 Play 중 disabled
  - 다른 Canvas가 Sort Order로 덮음 (가능성 낮음 — HUD는 0)
  - Canvas Scaler / Render Mode 충돌 (가능성 낮음)
- **다음 세션에서 Unity MCP 활성 + Debug.Log 출력 확인으로 30초 안에 진단 가능**

### Debug.Log 추가 (다음 세션 진단용)
PauseMenuController에 다음 로그 박힘:
- `OnEnable` — InputAction 등록 + pauseCanvas 참조 확인
- `Toggle()` — isPaused 토글 + pauseCanvas.activeSelf/activeInHierarchy 변화

---

## ➡️ 다음

### 즉시 (다음 세션)
1. Claude Code 재시작 → Unity MCP 활성화 확인
2. MCP로 시각 디버깅 (Backdrop Color α 직접 확인 + Canvas 상태 + 다른 Canvas 비교)
3. 시각 fix → 검증 5종 통과 → Phase 04 *진짜 마감*

### Phase 04 진짜 마감 후
- **Phase 05** — 씬 전환 폴리시 (페이드 인/아웃, 2h)
- **Phase 06** — Regression + 데모 영상 (1~2h)

---

## 📝 학습 보존

### 새로 익힌 것
- **InputActionReference 패턴**: `[SerializeField]`로 Inspector에서 InputSystem asset의 sub-asset 드래그 → `OnEnable`/`OnDisable`에서 `performed += /-=` + `Enable()`/`Disable()`
- **Vertical Layout Group + LayoutElement**: Layout이 자식 크기를 통제할 때 자식이 *선호 크기*를 hint로 주는 표준 메커니즘
- **두 층 분리 패턴**: Controller가 부착된 GameObject는 항상 ON, 토글 대상은 *자식*. InputAction 콜백 보존을 위함.
- **`Button - TextMeshPro` vs legacy Button**: SDF 렌더링 vs 비트맵
- **`#if UNITY_EDITOR` 분기**: `Application.Quit()`는 에디터에서 무동작 → `EditorApplication.isPlaying = false`로 분기
- **Time.timeScale 0**: Update는 호출되나 deltaTime 0. *Realtime* 기반 입력(InputSystem)은 영향 X.

### 사건성 학습 (★★★ — `/journal:bug` 후보)

**"Unity UI 손작업의 함정 — 코드는 정상인데 조용히 안 보이는 디버깅 지옥"**

- Phase 04에서 코드 작성 + InputSystem 셋업 + Inspector 연결까지 30분
- 그런데 시각 렌더링 디버깅에 **1.5시간**
- 코드는 100% 정상 (Hierarchy 토글로 확인) — *Unit 단위* OK
- 그런데 *End-to-end* (Game 뷰에 그려짐)는 깨짐
- Unity UI의 RectTransform / Anchor / Canvas Scaler / Image Color α / SetActive / Sort Order 조합 중 *어딘가*가 깨지면 *조용히 안 보임*
- 디버깅 도구 약함 — 콘솔 에러도 없고, Scene 뷰에서도 안 보임
- **MCP / 스크립트 자동화의 가치를 직접 체감한 사건**

면접에서 가치 — *Unity 손작업의 한계*를 경험으로 짚고, MCP 도입의 *왜*를 본인 사건으로 말할 수 있음.

### 부수 학습
- **팀장도 헌법 어김 사건** (헌법 #4 — 공유 코드 규율): PDL 재생성 누락한 채 main push → 본인 환경에서 회귀 → 임시 캐스트 패치. CHANGELOG 미박제 사건.
- **Unity asmdef 분리의 함정**: 새 asmdef 도입 시 references 누락하면 *기존에 잘 되던 코드*가 회귀. TMP 같은 흔한 의존성도 명시 필요.

---

## 🔗 산출물 (Commit 예정)

- `03_Client/Assets/Scripts/UI/PauseMenuController.cs` (신규)
- `03_Client/Assets/Scripts/Dawnholder.Client.asmdef` (TMP 참조 추가)
- `03_Client/Assets/Scripts/Input/LocalPlayerController.cs` (임시 캐스트)
- `03_Client/Assets/Scripts/Network/UnityClientSession.cs` (임시 캐스트)
- `03_Client/Assets/Scenes/UI.unity` (PauseMenuCanvas + 자식들 추가)
- `03_Client/Assets/InputSystem_Actions.inputactions` (TogglePauseMenu 액션 추가)
- `01_Phases/yuhyeon/M1-client-foundations/04-pause-menu-input.md` (사후 정정)
- `01_Phases/yuhyeon/M1-client-foundations/04-pause-menu-input-DONE.md` (본 파일)

---

## 작업 로그

- 2026-05-17 14:00 — main pull + 새 브랜치 `feature/yuhyeon-m1-phase04-pause-menu` 생성, 핀 Phase 04 좌표로 갱신
- 2026-05-17 14:10 — PauseMenuController.cs 작성, Phase 04 정의 사후 정정 (UI 씬)
- 2026-05-17 14:30 — Unity Safe Mode 진입 (TMP 누락) → asmdef fix + PDL int↔uint 캐스트 임시 패치
- 2026-05-17 15:00 — InputSystem_Actions TogglePauseMenu 액션 추가
- 2026-05-17 15:30 — UI.unity에 PauseMenuCanvas + MenuRoot + Backdrop + MenuPanel + 버튼 3개 구조 신설
- 2026-05-17 16:00 — PauseMenuController 부착 + Inspector 필드 드래그 + OnClick 3개 연결
- 2026-05-17 16:30~18:00 — 시각 디버깅 (MenuRoot/Backdrop RectTransform 풀스크린 stretch 보정, 그래도 Game 뷰 미표시) → 부분 마감 결정
- 2026-05-17 18:00 — Debug.Log 추가 + Phase 04 부분 마감 박제
