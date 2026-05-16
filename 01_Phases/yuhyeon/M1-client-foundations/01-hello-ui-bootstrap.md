# Phase 01: Hello UI — MainMenu 씬 부트스트랩

> **상태**: pending
> **마일스톤**: M1-client-foundations
> **예상 소요**: 1~1.5시간
> **담당 에이전트**: client

---

## 🎯 목표

새 MainMenu 씬에 Canvas + TextMeshPro 텍스트 1개를 추가해 "Dawnholder — Welcome" 문구가 화면에 표시된다. Build Settings에 등록되어 Play 모드와 빌드 양쪽에서 동일하게 보인다. **환경 검증용 가장 작은 시작.**

---

## ⏪ 사전 조건

- [x] Unity 6.4 Editor 설치 + 03_Client 프로젝트 열림
- [x] InputSystem_Actions.inputactions 존재 (팀장 셋업)
- [ ] feature/yuhyeon-m1-client-foundations 브랜치에서 작업

---

## 📝 작업 내용

- [ ] `03_Client/Assets/Scenes/` 폴더에 `MainMenu.unity` 새 씬 만들기 (File → New Scene → 2D 템플릿)
- [ ] Hierarchy에 Canvas 추가 (GameObject → UI → Canvas) — Render Mode = Screen Space Overlay
- [ ] EventSystem 자동 생성 확인 (없으면 GameObject → UI → Event System)
- [ ] Canvas 안에 TextMeshPro - Text (UI) 추가 — 입력: `Dawnholder — Welcome`
- [ ] 텍스트 화면 중앙 배치 (Anchor + Pivot 중앙, RectTransform 위치 0,0)
- [ ] 폰트 크기 적절히 (72 이상 권장 — 큰 화면에서도 잘 보이게)
- [ ] Build Settings → Scenes In Build에 MainMenu 추가 (index 0으로 정렬)
- [ ] 씬 저장 (Ctrl+S)

---

## ✅ 완료 조건

- [ ] Unity Editor에서 MainMenu 씬 열면 검은 배경 + 중앙에 텍스트 보임
- [ ] Play 누르면 같은 화면 유지 (스크립트 없으니 동작 무, OK)
- [ ] Console에 Error/Warning 0개
- [ ] git status로 `MainMenu.unity` + `.meta` 파일이 변경 목록에 보임

---

## 🧪 테스트

**자동 테스트:**
- 없음 (UI 셋업이라 자동 검증 어려움)

**수동 테스트:**
- Editor에서 MainMenu 씬 열기 → Play → 텍스트 표시 확인
- 1920x1080 / 1280x720 두 해상도에서 텍스트가 잘림/사라짐 없이 보이는지 (Game 뷰 우상단 해상도 토글)
- 빌드 (File → Build And Run) → 빌드된 실행파일에서도 동일하게 보이는지 (선택, 시간 여유 시)

---

## 📚 학습 포인트

> 이번 Phase에서 새로 만나는 개념.

- **Unity UI 시스템**: Canvas(렌더링 컨테이너) + EventSystem(입력 처리) 한 쌍 필수.
- **Canvas Render Mode 3가지**:
  - *Screen Space - Overlay*: 화면 위에 덮어 그림 (UI 기본). 본 Phase 사용.
  - *Screen Space - Camera*: 특정 카메라에 그림 (3D 효과 혼합 시).
  - *World Space*: 게임 월드 안에 배치 (NPC 머리 위 이름표 등).
- **TextMeshPro (TMP)**: 일반 Text보다 권장. SDF 폰트로 자유롭게 확대/효과.
- **Anchor & Pivot**: Anchor = "부모 어디에 부착되나", Pivot = "내 어디를 기준점으로". 헷갈리는 학부생 1순위 개념.
- **Build Settings의 씬 순서**: `SceneManager.LoadScene(0)` 호출 시 index 0 씬이 로드됨. 이름으로 호출하는 게 안전(`LoadScene("MainMenu")`).

---

## ⚠️ 함정 / 주의사항

- Canvas 추가하면 EventSystem 자동 생성되지만 *간혹 누락*. 없으면 입력 안 됨 → 수동으로 GameObject → UI → Event System.
- 씬을 **Build Settings에 추가 안 하면** `LoadScene` 호출 시 런타임 에러 ("Scene 'MainMenu' couldn't be loaded").
- Text 컴포넌트가 *기존 UI Text*(레거시)와 *TextMeshPro - Text (UI)*(권장) 두 개 있음. TMP 선택.
- TextMeshPro 첫 사용 시 "TMP Essentials Import" 팝업 — Yes 눌러 임포트.
- Canvas Scaler 컴포넌트 기본값 = Constant Pixel Size. 해상도 변하면 UI 사이즈 그대로라 작아 보임. 다음 Phase에서 Scale With Screen Size로 조정.

---

## ➡️ 다음 Phase

Phase 02 — 메인 메뉴 버튼 (시작/종료): MainMenu 씬에 버튼 2개 추가 + 클릭 핸들러로 씬 전환.

---

## 작업 로그

> Phase 진행하면서 발견된 이슈, 결정, 메모를 여기 누적.

- 2026-05-17: /work:plan으로 생성
