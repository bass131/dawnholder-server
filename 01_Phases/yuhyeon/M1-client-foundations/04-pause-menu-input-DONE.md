# Phase 04 — DONE (완료)

> **상태**: 완료 — 5개 완료 조건 모두 통과
> **마일스톤**: M1-client-foundations
> **완료일**: 2026-05-17 (부분 마감 후 같은 날 시각 fix → 진짜 마감)
> **소요 시간**: 약 4시간 (코드 30분 + Unity 손작업 + 시각 디버깅 1.5시간 + 다음 세션 MCP 진단·fix 30분)
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
  - 진단 Debug.Log 잔류 (`OnEnable` / `Toggle()`) — 안정 확인 후 Phase 05 마감 시 제거 예정

### Unity 산출물
- **InputSystem_Actions** — UI Action Map에 `TogglePauseMenu` 액션 + `<Keyboard>/escape` Binding 추가
- **`Assets/Scenes/UI.unity`** — PauseMenuCanvas 신설 + 시각 fix 반영:
  - PauseMenuCanvas (Sort Order 20, HUD 위) ← PauseMenuController 부착
  - MenuRoot (PauseMenuCanvas 자식, 풀스크린 stretch, 시작 SetActive(false), 토글 대상)
  - Backdrop (풀스크린 stretch, 검은 Image α=0.7)
  - MenuPanel (anchor 중앙, 300×240, Vertical Layout Group)
  - ResumeButton / MainMenuButton / QuitButton + 각자 LayoutElement / TMP 텍스트
- **PauseMenuController** 부착 + Inspector 필드 드래그 + 버튼 OnClick 이벤트 3개 연결

### 부수 작업 (PR #19 머지 후 정리됨)
- `Dawnholder.Client.asmdef` TMP 참조 — 팀장 PR #19로 영구 fix (본인 PR엔 없음)
- PDL int↔uint 임시 캐스트 — 팀장 PR #19 `.gitignore`(Shared.dll 화이트리스트) + Shared.dll commit으로 영구 해소 → 본인은 merge `665934d`에서 임시 캐스트 제거

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
- **함정**: 한글 글리프 LiberationSans에 없음 → 일단 영어(`Resume`/`MainMenu`/`quit`)로 박음. TMP 한글 폰트 도입은 별도 작업 큐(`2026-05-16-tmp-korean-font-todo.md`).

### LayoutElement로 버튼 Height 60 통제
- **이유**: VerticalLayoutGroup이 자식 RectTransform Height를 driven할 때, 자식이 "내가 원하는 크기는 이거"라고 hint 주는 표준 메커니즘.
- **trade-off**: 컴포넌트 +1개 vs Control Child Size: Height 해제 후 RectTransform 직접 입력 (간단하지만 우회).

### 시각 fix — MenuRoot RT 정상화 (다음 세션 MCP 진단)
- **증상**: 부분 마감 시점 — Game 뷰 *좌하단*에 메뉴가 잘려 보였음 (이전 진단에선 "안 보임"으로 잘못 인지됨)
- **원인**: 본인이 부분 마감 후 Editor에서 MenuRoot를 sibling → PauseMenuCanvas 자식으로 손작업 드래그. Unity 기본 reparent가 `worldPositionStays=true`로 동작 → 원래 부모(씬 루트, rect=0×0)의 stretch 결과 *크기 0*을 보존하려고 sizeDelta=(-1211, -681), anchoredPos=(-605.5, -340.5) 음수로 박힘 → MenuRoot가 화면 (0,0)에 박힌 점 → MenuPanel이 좌하단에 매달림
- **fix**: MenuRoot RT 정상화 — `anchorMin=(0,0)` `anchorMax=(1,1)` `anchoredPos=(0,0)` `sizeDelta=(0,0)` → 부모(화면 전체) stretch fill → MenuPanel이 정중앙 (BL≈(455, 220) 실측 일치)
- **도구**: Unity MCP `Unity_RunCommand`로 진단 + fix + `EditorSceneManager.SaveScene` 한 번에 수행. 손작업 30분이 5분 안 걸림.

---

## 🛠️ 어떻게 — 구조

```
UI.unity
├── HUD Canvas (Phase 03, Sort Order 0)
├── EventSystem
└── PauseMenuCanvas (Sort Order 20) ← PauseMenuController 부착
    └── MenuRoot ⬜ (풀스크린 stretch, 시작 SetActive(false), 토글 대상)
        ├── Backdrop (풀스크린 stretch, 검은색 Image α=0.7)
        └── MenuPanel (중앙 300×240, Vertical Layout Group)
            ├── ResumeButton (LayoutElement Preferred Height 60) → OnResumeClicked
            ├── MainMenuButton (동일) → OnMainMenuClicked
            └── QuitButton (동일) → OnQuitClicked
```

**InputSystem_Actions** → UI 맵 → `TogglePauseMenu` 액션 (Binding: `<Keyboard>/escape`, Type: Button) → PauseMenuController.OnEnable에서 `performed += OnTogglePerformed` 등록.

---

## 🧪 테스트 — 5/5 통과

| # | 항목 | 결과 |
|---|------|------|
| 1 | Gameplay 중 ESC → 메뉴 정중앙 표시 + 캐릭터 정지 (timeScale 0) | ✅ |
| 2 | 재개 클릭 → 메뉴 닫힘 + 캐릭터 재개 (timeScale 1) | ✅ |
| 3 | 메인 메뉴 클릭 → MainMenu 씬 전환 (timeScale 1 복원 선행) | ✅ |
| 4 | 게임 종료 클릭 → 에디터 Play 중지 (`#if UNITY_EDITOR` 분기) | ✅ |
| 5 | ESC 5회 빠른 토글 → 깨짐 없이 정확히 켜짐/꺼짐 | ✅ |

### MCP 진단 보강 (다음 세션)
부분 마감 직후 잔류했던 5종 ②~⑦ 모두 통과. 부분 마감 시 추측한 가능 원인(α=0 / Canvas disabled / Sort Order 덮임 / Canvas Scaler 충돌)은 **전부 빗나감** — 진짜 원인은 위의 *MenuRoot RT 음수 sizeDelta*. 손작업 reparent 한 번이 만든 *조용한 데이터 손상*.

---

## ➡️ 다음

- **Phase 05** — 씬 전환 폴리시 (페이드 인/아웃, 2h)
- **Phase 06** — Regression + 데모 영상 (1~2h)
- (선택) PauseMenuController의 진단 Debug.Log 제거 — Phase 05 시작 시 또는 Phase 06 regression 시
- (선택) TMP 한글 폰트 도입 (별도 작업 큐) — Phase 05/06 중 여유 시 PauseMenu 한글화 동반

---

## 📝 학습 보존

### 새로 익힌 것 (Phase 04 본편)
- **InputActionReference 패턴**: `[SerializeField]`로 Inspector에서 InputSystem asset의 sub-asset 드래그 → `OnEnable`/`OnDisable`에서 `performed += /-=` + `Enable()`/`Disable()`
- **Vertical Layout Group + LayoutElement**: Layout이 자식 크기를 통제할 때 자식이 *선호 크기*를 hint로 주는 표준 메커니즘
- **두 층 분리 패턴**: Controller가 부착된 GameObject는 항상 ON, 토글 대상은 *자식*. InputAction 콜백 보존을 위함.
- **`Button - TextMeshPro` vs legacy Button**: SDF 렌더링 vs 비트맵
- **`#if UNITY_EDITOR` 분기**: `Application.Quit()`는 에디터에서 무동작 → `EditorApplication.isPlaying = false`로 분기
- **Time.timeScale 0**: Update는 호출되나 deltaTime 0. *Realtime* 기반 입력(InputSystem)은 영향 X.

### 사건성 학습 (★★★ — `/journal:bug` 후보)

**"Unity UI 손작업의 함정 — 코드는 정상인데 조용히 잘못된 자리에 그려지는 디버깅 지옥"**

본 Phase의 *두 단계 사건*:

**1단계 (부분 마감 시점, 1.5h 소비)** — 부분 마감 보고서엔 "안 보임"으로 적혔지만 실제론 *그려지긴 했고 화면 좌하단에 잘려 있었음*. 시각 디버깅 도구가 약해서 "안 보임" vs "잘못된 자리에 보임"을 구분 못 했음. 학부생 입장에서 흔한 첫 함정.

**2단계 (다음 세션, 30분)** — Unity MCP `Unity_RunCommand`로 PauseMenuCanvas + MenuRoot의 RectTransform + worldCorners를 직접 dump. 한 번의 dump로 *MenuRoot의 sizeDelta가 (-1211, -681) 음수*라는 정확한 데이터가 잡힘 → 즉시 원인 한 점으로 좁혀짐.

**진짜 원인**: 본인이 부분 마감 후 Editor에서 *MenuRoot를 PauseMenuCanvas의 자식으로 끌어넣는 손작업*을 했음. 옳은 방향(자식 만들기)이었지만 Unity 기본 SetParent의 `worldPositionStays=true` 동작이 *RT 값을 보존*하려고 음수 sizeDelta를 박음. 이전 부모(씬 루트)의 rect가 0×0이라 자식의 stretch 결과도 0×0이었고, 새 부모(화면 전체)에서도 *그 0×0을 유지*하기 위해 *부모 크기만큼 줄어드는 음수*가 박힘.

면접 가치:
- **단위 정상성 ≠ End-to-end 정상성** — 컴포넌트 모두 OK여도 *조합 한 점*이 깨지면 조용히 잘못됨
- **데이터 손상은 사용자 행동 흔적** — `worldPositionStays` 같은 *기본값의 의도*를 모르면 무해해 보이는 손작업이 데이터 깨뜨림
- **MCP/자동화의 *왜*** — 손작업 1.5시간 → 스크립트 5분. 추측 디버깅이 *데이터 직접 조회*로 바뀌면 정확도·속도 둘 다 점프

**또 한 가지 — `worldPositionStays` 자체**:
- `Transform.SetParent(parent, worldPositionStays=true)` = world 위치/크기 보존 (기본값)
- `Transform.SetParent(parent, worldPositionStays=false)` = local 좌표 보존 (이번처럼 UI stretch 같은 *부모 기준 비례 배치*엔 이게 옳음)
- Inspector 드래그는 *true* 동작 — UI 작업 시 *드래그 후 RT 값 점검 필수*

### 부수 학습
- **CONTEXT/핀의 정보가 옛 정보일 수 있음**: 첫 dump에서 "MenuRoot가 sibling"이라고 본 게 실은 *디스크 UI.unity 상태*였고, 메모리 상태는 다름. Edit 모드에서 *씬 modified 미저장* 상태 흔함 → 다음부터 `EditorSceneManager.SaveScene` 명시적 체크 습관

---

## 🔗 산출물 (Commit 예정)

부분 마감(commit 853cf02) 위에 시각 fix commit 1개 추가 (같은 PR #21):

- `03_Client/Assets/Scenes/UI.unity` (MenuRoot RT 정상화 — anchorMin/Max stretch + anchoredPos/sizeDelta 0)
- `01_Phases/yuhyeon/M1-client-foundations/04-pause-menu-input-DONE.md` (본 파일, 부분 → 완전)
- (선택) `01_Phases/yuhyeon/M1-client-foundations/04-pause-menu-input.md` (작업 로그 1줄 추가 — 시각 fix 완료)

---

## 작업 로그

- 2026-05-17 14:00 — main pull + 새 브랜치 `feature/yuhyeon-m1-phase04-pause-menu` 생성, 핀 Phase 04 좌표로 갱신
- 2026-05-17 14:10 — PauseMenuController.cs 작성, Phase 04 정의 사후 정정 (UI 씬)
- 2026-05-17 14:30 — Unity Safe Mode 진입 (TMP 누락) → asmdef fix + PDL int↔uint 캐스트 임시 패치
- 2026-05-17 15:00 — InputSystem_Actions TogglePauseMenu 액션 추가
- 2026-05-17 15:30 — UI.unity에 PauseMenuCanvas + MenuRoot + Backdrop + MenuPanel + 버튼 3개 구조 신설
- 2026-05-17 16:00 — PauseMenuController 부착 + Inspector 필드 드래그 + OnClick 3개 연결
- 2026-05-17 16:30~18:00 — 시각 디버깅 (MenuRoot/Backdrop RectTransform 풀스크린 stretch 보정, 그래도 Game 뷰 미표시) → 부분 마감 결정
- 2026-05-17 18:00 — Debug.Log 추가 + Phase 04 부분 마감 박제 (commit 853cf02)
- 2026-05-17 (다음 세션) — Claude Code 재시작 + Unity MCP 활성 확인 + 본인 손으로 MenuRoot를 PauseMenuCanvas 자식으로 reparent (의도는 옳음, 다만 worldPositionStays=true로 RT 값 깨짐)
- 2026-05-17 (다음 세션) — MCP `Unity_RunCommand`로 4회 진단 dump → MenuRoot sizeDelta=(-1211, -681) 음수 발견 → RT 정상화 + UI.unity 저장 (5분)
- 2026-05-17 (다음 세션) — Play 모드 검증 5/5 통과 → Phase 04 진짜 마감
