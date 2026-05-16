# Phase 05: 씬 전환 폴리시 — 페이드 인/아웃

> **상태**: pending
> **마일스톤**: M1-client-foundations
> **예상 소요**: 2시간
> **담당 에이전트**: client

---

## 🎯 목표

씬 전환 시 검은 페이드 인/아웃 효과. **SceneTransition Singleton 매니저**로 모든 전환 일원화 (MainMenu↔Gameplay, PauseMenu→MainMenu 등). 매끄러운 시각 피드백 + 로딩 hitch 가림.

---

## ⏪ 사전 조건

- [ ] Phase 02 완료 (씬 전환 호출 존재)
- [ ] Phase 04 완료 (PauseMenu에서도 씬 전환 호출)

---

## 📝 작업 내용

- [ ] FadeCanvas 프리팹 신설 (`03_Client/Assets/Prefabs/UI/FadeCanvas.prefab`):
  - Canvas (Screen Space Overlay, Sort Order 100 — 모든 UI 위)
  - 자식: Image (검은색, RectTransform stretch all, 알파 0)
- [ ] `Scripts/UI/SceneTransition.cs` 작성 (Singleton):
  - `static SceneTransition Instance`
  - `Awake()`: Instance 중복 체크 + DontDestroyOnLoad
  - `public void LoadScene(string sceneName)`: Coroutine 시작
  - Coroutine: 페이드 아웃 (알파 0→1, 0.5초) → SceneManager.LoadSceneAsync → 페이드 인 (알파 1→0, 0.5초)
- [ ] FadeCanvas 프리팹에 SceneTransition 컴포넌트 부착
- [ ] MainMenu 씬에 FadeCanvas 인스턴스 1개 배치 (게임 시작 시 Awake로 초기화됨)
- [ ] MainMenuController.OnStartClicked: `SceneManager.LoadScene("Gameplay")` → `SceneTransition.Instance.LoadScene("Gameplay")`로 변경
- [ ] PauseMenuController.OnMainMenuClicked: 동일하게 변경

---

## ✅ 완료 조건

- [ ] MainMenu → 시작 → 검은 페이드 아웃 → Gameplay 페이드 인
- [ ] Gameplay → ESC → 메인 메뉴 → 검은 페이드 → MainMenu 페이드 인
- [ ] 페이드 중 클릭 무시 (메뉴 버튼 안 눌림)
- [ ] 페이드 캔버스가 씬 전환 시에도 사라지지 X (DontDestroyOnLoad)

---

## 🧪 테스트

**자동 테스트:**
- 없음

**수동 테스트:**
- MainMenu → Gameplay → MainMenu → Gameplay 3회 반복 → 페이드 매번 정상
- 페이드 중 시작 버튼 다시 클릭 → 무시되거나 큐잉 X (중복 호출 방지)
- DontDestroyOnLoad 작동: Gameplay 씬에 FadeCanvas Hierarchy 검색 → 보여야 함

---

## 📚 학습 포인트

- **Singleton 패턴 (Unity 변형)**: 정적 `Instance` 프로퍼티 + `Awake`에서 중복 체크. *씬 간 데이터 공유*의 가장 흔한 패턴.
- **DontDestroyOnLoad**: 씬 전환 시 GameObject 살아남게. 매니저류 (Audio, Network, Transition)에 사용.
- **Coroutine vs async/await**:
  - *Coroutine*: Unity 내장. 메인 스레드 보장. UI 애니메이션·딜레이에 적합.
  - *async/await*: C# 표준. Unity에서도 가능하지만 Awaitable 도입(Unity 2023+) 전엔 직접 처리 필요.
- **CanvasGroup 활용**: alpha 조작 + blocksRaycasts(입력 차단) 한 컴포넌트로 처리.
- **SceneManager.LoadSceneAsync**: 비동기 로드. 큰 씬에서 freeze 방지. AsyncOperation.allowSceneActivation으로 로드 완료 시점 제어.

---

## ⚠️ 함정 / 주의사항

- Singleton 두 번 생성 방지 (`if (Instance != null) { Destroy(gameObject); return; }`).
- DontDestroyOnLoad는 *루트 GameObject*에만 적용. 자식이면 부모 째로 살아남.
- 페이드 도중 입력 차단 안 하면 더블 클릭으로 씬 두 번 로드되어 깨짐 — CanvasGroup.blocksRaycasts = true.
- 페이드 시간 너무 길면(>1초) 짜증, 너무 짧으면(<0.2초) 의미 X. **0.3~0.5초** 권장.
- SceneTransition 인스턴스가 *각 씬마다* 새로 만들어지면 DontDestroyOnLoad 의미 X — *최초 1개*만 활성, 나머지는 Destroy.

---

## ➡️ 다음 Phase

Phase 06 — Regression + 데모 영상: M1 전체 흐름 회귀 테스트 + 시연 GIF 박제.

---

## 작업 로그

- 2026-05-17: /work:plan으로 생성
