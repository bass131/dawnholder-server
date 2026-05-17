# Phase 05 — DONE (완료)

> **상태**: 완료 — 4 완료 조건 + 회귀 체크까지 7/7 통과
> **마일스톤**: M1-client-foundations
> **완료일**: 2026-05-17
> **소요 시간**: 약 1.5시간 (예상 2h보다 짧음 — Phase 02 작성 시점에 미리 박은 *예고 주석* 덕에 흐름 매끄러움)
> **담당**: 정유현 (@jungyoohyun0105)

---

## 🎯 무엇 — 완료된 것

### Unity 산출물
- **`03_Client/Assets/Prefabs/UI/FadeCanvas.prefab`** (신규)
  - Canvas (Screen Space Overlay, Sort Order 100 — 모든 UI 위)
  - CanvasGroup (alpha 0, blocksRaycasts off — 코드에서 토글)
  - 자식 FadeImage (검은색 RGBA(0,0,0,1), RectTransform stretch all)
- **`Assets/Scenes/MainMenu.unity`**: FadeCanvas 인스턴스 1개 배치 (Connected prefab)

### 코드 산출물
- **`03_Client/Assets/Scripts/UI/SceneTransition.cs`** (신규)
  - `Dawnholder.Client.UI` 네임스페이스, `public static SceneTransition Instance` (Singleton)
  - `Awake()`: 중복 가드 (`if (Instance != null && Instance != this) Destroy`) + `DontDestroyOnLoad`
  - `public void LoadScene(string sceneName)`: `isTransitioning` 가드 + Coroutine 시작
  - `LoadSceneRoutine`: 페이드 아웃 (α 0→1) → `SceneManager.LoadSceneAsync` polling → 페이드 인 (α 1→0)
  - `Fade(from, to, duration)`: **`Time.unscaledDeltaTime`** 사용 (timeScale=0 함정 회피)
  - 페이드 중 `blocksRaycasts = true` (입력 차단)
  - 헌법 #1 (Server Authority) 주석 명시 — 페이드는 *본인 클라 시각* 효과만
- **`03_Client/Assets/Scripts/UI/MainMenuController.cs`** (수정)
  - `OnStartClicked`: `SceneManager.LoadScene` → `SceneTransition.Instance.LoadScene` (안전망 X — MainMenu 씬 한정 호출, FadeCanvas 항상 존재)
  - 옛 *Phase 05 예고* 주석을 *완료* 주석으로 갱신 + unused using 제거
- **`03_Client/Assets/Scripts/UI/PauseMenuController.cs`** (수정)
  - `OnMainMenuClicked`: `SceneManager.LoadScene` → `SceneTransition.Instance.LoadScene` + **null fallback** (에디터에서 Gameplay 직접 Play 진입 대비)
  - `Time.timeScale = 1f` 복원 순서 보존 (Phase 04 함정 회피 그대로)

---

## 🤔 왜 — 결정 흐름

### Singleton 패턴 채택 (Service Locator 등 다른 대안 X)
- **이유**: M1 한정 *단일 인스턴스* + *전역 접근 1줄*이면 충분. Service Locator는 매니저 다수 등장 시점에 도입 (M3+).
- **trade-off**: Singleton은 *테스트 어려움*(전역 상태)이 단점이지만 본 프로젝트 PlayMode test 미도입 — 비용 X.

### DontDestroyOnLoad + Awake 중복 가드는 *짝* — 둘 중 하나만 박으면 함정
- **이유**: DontDestroyOnLoad만 박으면 다른 씬에 또 FadeCanvas 있을 때 *둘 다 살아남음* → 두 Instance 충돌. 중복 가드만 있으면 *씬 전환 시 사라짐* → DontDestroy 의미 X. 둘은 반드시 페어.

### Time.unscaledDeltaTime 사용 (Time.deltaTime 대신)
- **이유**: Phase 04 PauseMenu의 `timeScale = 0` 함정과 짝. PauseMenu에서 메인메뉴 클릭 시 `timeScale=1` 복원 *먼저* 하지만 *만에 하나 timeScale 0 잔류* 시 `Time.deltaTime`은 0이라 페이드 무한 루프.
- **trade-off**: 일관성 차원에서 *모든 UI 애니메이션*은 unscaledDeltaTime 쓰는 게 정답. Time.timeScale은 *게임플레이*만 영향.

### CanvasGroup 1개로 alpha + blocksRaycasts 통제
- **이유**: Image color α + 별도 GraphicRaycaster.enabled 따로 만지면 *두 곳에서 통제* — 디버깅 어려움. CanvasGroup은 *단일 통제점*.
- **학습**: Phase 04에서 본인이 Image color α를 *Backdrop*에 0.698로 박은 패턴은 *정적*. 동적 페이드엔 CanvasGroup이 정석.

### PauseMenu만 null fallback, MainMenu는 X
- **이유**: 호출 컨텍스트의 *FadeCanvas 보장 여부* 차이.
  - MainMenu 씬에서 OnStartClicked → MainMenu에 FadeCanvas 인스턴스 *항상 있음* → fallback 불필요
  - Gameplay 씬에서 OnMainMenuClicked → MainMenu 거쳤으면 O, 에디터 직접 Play면 X → **fallback 필요**
- **학습**: *방어 코드는 호출 컨텍스트에 따라*. 일관성 차원에서 *둘 다* 박으면 노이즈, *필요한 쪽만* 박으면 의도 명확.

### `LoadSceneAsync` (동기 LoadScene 대신)
- **이유**: M1 씬은 작아 차이 안 보이지만 *패턴 학습*. 큰 씬에서 동기 호출 시 freeze.
- **현재 X**: `AsyncOperation.allowSceneActivation` 활용은 *완료 시점 제어* 필요할 때만. Phase 05엔 단순 polling으로 충분.

---

## 🛠️ 어떻게 — 구조

```
[Singleton + DontDestroyOnLoad 매니저]
SceneTransition.Instance (static)
  ↑ Awake에서 등록 + 중복 가드

[프리팹 = 캔버스 + 검은 천]
FadeCanvas.prefab
├── Canvas (Sort 100, ScreenSpaceOverlay)
├── CanvasGroup (α 토글 + blocksRaycasts) ← 코드가 통제
├── SceneTransition.cs (위 Singleton)
└── FadeImage (검은색, stretch all)

[배치 위치]
MainMenu.unity 루트에 FadeCanvas 인스턴스 1개
  → Awake로 Instance 등록 + DontDestroyOnLoad
  → 이후 모든 씬 (Gameplay 등)에서 살아남음

[호출 흐름]
MainMenuController.OnStartClicked
  → SceneTransition.Instance.LoadScene("Gameplay")
       → 페이드 아웃 (α 0→1, 0.5초)
       → LoadSceneAsync("Gameplay") 폴링
       → 페이드 인 (α 1→0, 0.5초)

PauseMenuController.OnMainMenuClicked
  → Time.timeScale = 1f (선행)
  → if (Instance != null) Instance.LoadScene("MainMenu")
    else SceneManager.LoadScene("MainMenu")  ← fallback
```

---

## 🧪 테스트 — 7/7 통과 (Play 모드 수동 회귀)

| # | 항목 | 결과 |
|---|------|------|
| ① | MainMenu → 시작 → 검은 페이드 아웃 → Gameplay 페이드 인 | ✅ |
| ② | Gameplay → ESC → 메인 메뉴 → 검은 페이드 → MainMenu 페이드 인 | ✅ |
| ③ | MainMenu↔Gameplay 3회 반복, 매번 정상 | ✅ |
| ④ | 페이드 중 더블 클릭 차단 (씬 두 번 안 로드) | ✅ |
| ⑤ | Gameplay에서 Hierarchy `DontDestroyOnLoad` 섹션에 FadeCanvas 보임 | ✅ |
| ⑥ | Phase 04 회귀 — ESC → 재개 → 캐릭터 다시 움직임 | ✅ |
| ⑦ | Phase 04 회귀 — ESC → 게임 종료 → 에디터 Play 중지 | ✅ |

---

## ➡️ 다음

- **Phase 06** — Regression + 데모 영상 (1~2h) — **M1 마지막 Phase**
  - 회귀 시나리오 6단계 수동 통과
  - 30~60초 GIF/영상 녹화 (ShareX/OBS)
  - `00_Document/learning-journal/yuhyeon/M1-client-foundations/demo.gif` 박제
  - `/journal:phase`로 M1 마일스톤 통째 회고
  - 6개 -DONE.md 모두 확인

### 선택 작업 (Phase 06 전에 가능)
- TMP 한글 폰트 도입 (`2026-05-16-tmp-korean-font-todo.md`) — Phase 06 데모 박제 전에 PauseMenu/MainMenu 한글화 검토
- ★★★ 학습 일지 박제: `/journal:bug unity-setparent-world-position-stays` (Phase 04 시각 fix 사건) — fresh할 때 박기

---

## 📝 학습 보존

### 새로 익힌 것
- **Singleton 패턴 (Unity 변형)** — `public static Instance { get; private set; }` + Awake 중복 가드. 진입점 1줄, 모든 호출자 `Instance.LoadScene(...)`로 일원화.
- **DontDestroyOnLoad의 짝** — 항상 *중복 가드 + DontDestroy*. 둘 중 하나만 박으면 함정. *루트 GameObject*에만 적용 — 자식이면 부모째.
- **CanvasGroup 1개의 강력함** — alpha + blocksRaycasts + interactable 한 컴포넌트로. Image color α + 별도 Raycaster off보다 깔끔.
- **Time.unscaledDeltaTime** — UI/메뉴 애니메이션엔 *항상* 이걸 쓰는 게 정답. Time.timeScale은 *게임플레이*만 영향. Phase 04 timeScale=0 함정과 짝.
- **Coroutine yield return pattern** — `yield return Fade(...)`로 Coroutine을 또 다른 Coroutine 안에서 *직렬 대기*. `LoadSceneAsync` polling도 같은 패턴 (`while (!op.isDone) yield return null`).
- **LoadSceneAsync** — 비동기 로드 + polling. M1엔 차이 안 보이지만 패턴 학습.

### 사건성 학습 (★★ 학습 가치)

**"방어 코드는 호출 컨텍스트에 따라"** — MainMenuController의 OnStartClicked엔 fallback X (MainMenu 씬에서만 호출, FadeCanvas 항상 존재), PauseMenuController의 OnMainMenuClicked엔 fallback O (에디터에서 Gameplay 직접 Play 진입 가능). *일관성보다 정확성*이 더 가치 — 일관성 차원에서 *둘 다* 박으면 노이즈, *필요한 쪽만* 박으면 의도 명확.

**"Phase 02에서 미리 박은 예고 주석의 가치"** — MainMenuController에 `**Phase 05+에선** SceneManager 직접 호출 대신 SceneTransition Singleton 경유 예정` 박혀있어서 Phase 05 진입 시 *무엇을 바꿔야 하는지* 즉시 보임. 코드 자체가 *작업 진척 추적기*. 면접에서 "future-proofing"이 아니라 "*intent-trail*" 키워드로 설명 가능.

**"Time.unscaledDeltaTime — Phase 04와의 짝"** — 본 Phase 코드 작성 시 *Phase 04에서 timeScale=0을 의도적으로 박은 결정*과 짝 맞추기. 한 Phase의 결정이 *다음 Phase의 함정 회피*로 이어짐. 마일스톤 단위로 보면 *결정 사슬*. 시니어가 마일스톤 설계할 때 *결정 의존성*을 미리 보는 게 이런 흐름.

---

## 🔗 산출물 (Commit 예정)

- `03_Client/Assets/Prefabs/UI/FadeCanvas.prefab` (신규)
- `03_Client/Assets/Prefabs/UI/FadeCanvas.prefab.meta` (신규)
- `03_Client/Assets/Prefabs.meta` (신규)
- `03_Client/Assets/Prefabs/UI.meta` (신규)
- `03_Client/Assets/Scripts/UI/SceneTransition.cs` (신규)
- `03_Client/Assets/Scripts/UI/SceneTransition.cs.meta` (신규)
- `03_Client/Assets/Scripts/UI/MainMenuController.cs` (수정 — LoadScene 교체 + 주석 갱신 + unused using 제거)
- `03_Client/Assets/Scripts/UI/PauseMenuController.cs` (수정 — OnMainMenuClicked LoadScene 교체 + null fallback)
- `03_Client/Assets/Scenes/MainMenu.unity` (수정 — FadeCanvas 인스턴스 1개 추가)
- `01_Phases/yuhyeon/M1-client-foundations/05-scene-transition-fade-DONE.md` (본 파일)

---

## 작업 로그

- 2026-05-17 (Phase 04 마감 직후): main pull (10 commits 받음) + 옛 브랜치 정리 + `feature/yuhyeon-m1-phase05-scene-fade` 신설 + 핀 Phase 05 좌표로 갱신
- 단계 1: FadeCanvas.prefab 신설 (Canvas Sort 100 + CanvasGroup + Image stretch all 검은색) — 손작업
- 단계 2: SceneTransition.cs 작성 (Singleton + DontDestroyOnLoad + Coroutine 페이드 + unscaledDeltaTime) — Claude Write
- 단계 3: 프리팹에 SceneTransition 컴포넌트 부착 + Fade Group 슬롯 wire — 손작업 → MCP 검증 (fadeGroup OK, fadeDuration 0.5)
- 단계 4: MainMenu.unity에 FadeCanvas 인스턴스 1개 배치 — 손작업 → MCP 검증 (Connected prefab + Build Settings 0번 MainMenu)
- 단계 5: MainMenuController.OnStartClicked → SceneTransition.Instance.LoadScene 교체 + 주석 갱신 + unused using 제거 — Edit 3회
- 단계 6: PauseMenuController.OnMainMenuClicked → SceneTransition.Instance.LoadScene + null fallback + timeScale=1 복원 순서 보존 — Edit 1회
- 단계 7: Play 모드 수동 회귀 7/7 통과 (4 완료 조건 + Phase 04 회귀 2종 + 더블 클릭 차단)
- 단계 8: -DONE.md 박제 (본 파일) → 5단계 보고 → commit/PR/노션/CONTEXT 갱신 (다음 응답)
