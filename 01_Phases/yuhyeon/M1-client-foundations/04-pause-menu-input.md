# Phase 04: 일시정지 메뉴 — ESC 토글 + Input System 연동

> **상태**: pending
> **마일스톤**: M1-client-foundations
> **예상 소요**: 2~3시간
> **담당 에이전트**: client

---

## 🎯 목표

Gameplay 씬에서 ESC 키 누르면 일시정지 메뉴 토글. **Resume / Main Menu로 / Quit Game** 3개 버튼. Time.timeScale 0/1로 게임 시간 조작. 기존 InputSystem_Actions.inputactions에 UI 액션 추가.

---

## ⏪ 사전 조건

- [ ] Phase 03 완료 (HUD Canvas 존재)
- [x] InputSystem_Actions.inputactions 존재 (팀장 셋업)

---

## 📝 작업 내용

- [ ] InputSystem_Actions.inputactions 더블클릭 열기
- [ ] UI Action Map 안에 새 액션 `TogglePauseMenu` 추가 — Binding: `<Keyboard>/escape`
- [ ] PauseMenuCanvas 신설: 검은 반투명 배경 (Image, 알파 0.7) + 중앙 메뉴 패널
- [ ] 메뉴 패널 안에 Vertical Layout Group + Button 3개: "재개" / "메인 메뉴" / "게임 종료"
- [ ] `Scripts/UI/PauseMenuController.cs` 작성:
  - `[SerializeField] GameObject pauseCanvas;` (시작 시 비활성)
  - InputAction 참조 (TogglePauseMenu)로 ESC 입력 받기
  - `Toggle()`: 활성화 ↔ 비활성화 + Time.timeScale = 0 또는 1
  - 버튼 핸들러 3개: Resume / MainMenu / Quit
- [ ] Gameplay 씬에 PauseMenuCanvas 인스턴스 + PauseMenuController 부착 (시작 시 SetActive false)
- [ ] 씬 저장

---

## ✅ 완료 조건

- [ ] Gameplay 중 ESC → 메뉴 뜨고 게임 정지 (캐릭터 멈춤)
- [ ] 재개 클릭 → 메뉴 닫히고 게임 재개
- [ ] 메인 메뉴 클릭 → MainMenu 씬으로 전환 (timeScale 1 복구)
- [ ] 게임 종료 클릭 → 에디터 정지 (또는 빌드 시 어플 종료)
- [ ] ESC 다시 → 메뉴 다시 닫힘 (토글)

---

## 🧪 테스트

**자동 테스트:**
- 없음 (Input System + UI 자동 테스트는 PlayMode 도입 후)

**수동 테스트:**
- Gameplay 중 ESC 5회 빠르게 토글 → 깨지지 X
- 메뉴 뜬 상태에서 캐릭터 이동 입력해도 안 움직임 (timeScale 0)
- 메인 메뉴 갔다가 다시 시작 → Gameplay 진입 시 timeScale 1 (캐릭터 정상 움직임)

---

## 📚 학습 포인트

- **Time.timeScale = 0**: 게임 시간 *정지*. Update는 호출되지만 deltaTime 0. 따라서 deltaTime 기반 이동은 멈춤. (단 *Realtime* 기반 — `Time.unscaledDeltaTime` — 은 영향 X)
- **Input System Action Map 분리**: Player 맵 / UI 맵 분리. 메뉴 떴을 때 Player 맵 비활성 → 게임 입력 차단 패턴 (이 Phase는 간단화 위해 미적용, 다음 마일스톤에서 도입).
- **InputAction 참조 방법**: `[SerializeField] InputActionReference togglePauseAction;` 또는 직접 코드에서 Enable/Disable.
- **GameObject.SetActive(true/false)**: UI 활성/비활성 표준 패턴. 메모리는 그대로, 렌더링·업데이트만 정지.
- **헌법 1번**: 일시정지가 *서버 게임*까지 멈추진 X. 멀티 게임에선 본인만 메뉴 보이고 게임은 계속. *싱글/학습 단계*는 OK.

---

## ⚠️ 함정 / 주의사항

- Time.timeScale 0인 상태로 씬 전환하면 새 씬도 *정지된 채* 로드 — 메인 메뉴 진입 전 `Time.timeScale = 1` 복원 필수.
- InputAction Enable/Disable 잊으면 ESC 입력 안 잡힘.
- PauseCanvas Sort Order가 HUD Canvas보다 낮으면 메뉴가 HUD 뒤에 가려짐 — 더 큰 값으로 (예: 20).
- "Time.timeScale로 멀티게임 일시정지하면 다른 플레이어 멈춤" — 헌법 1번 위반. 본인 클라만 멈추면 OK, 서버엔 신호 X.

---

## ➡️ 다음 Phase

Phase 05 — 씬 전환 폴리시: 페이드 인/아웃으로 모든 씬 전환 매끄럽게.

---

## 작업 로그

- 2026-05-17: /work:plan으로 생성
