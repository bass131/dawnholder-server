# Phase 01: Unity 씬 + 캐릭터 GameObject (오프라인)

> **상태**: pending
> **마일스톤**: M2 First Connection
> **예상 소요**: 1.5~2시간
> **담당 에이전트**: client

---

## 🎯 목표

Unity에 사이드스크롤 캐릭터를 띄우고 키 입력(A/D)으로 화면 안에서 좌우로 움직일 수 있게 한다. **네트워크 코드는 절대 건드리지 않는다.** 이번 Phase는 "Unity 환경이 이 프로젝트에서 도는지" 확인용 Hello World.

---

## ⏪ 사전 조건

- [x] M1 완료 (서버/공유 DLL 파이프라인, PDL 동작)
- [ ] Unity 6.4 LTS로 `03_Client/` 열 수 있음
- [ ] Input System 패키지 설치돼 있음 (없으면 Package Manager에서 설치)

---

## 📝 작업 내용

- [ ] `03_Client/Assets/Scenes/Gameplay.unity` 새 씬 생성
- [ ] `Player` GameObject — Sprite Renderer(임시 흰색 사각형), 시작 위치 (0, 0)
- [ ] `Ground` GameObject — Sprite Renderer(긴 회색 막대), y = -1
- [ ] Main Camera — orthographic, Player follow 스크립트(`Assets/Scripts/Rendering/CameraFollow.cs`)
- [ ] `Assets/Scripts/Input/PlayerInputActions.inputactions` — `Move`(Vector2) 액션, A/D 키 바인딩
- [ ] `Assets/Scripts/Input/LocalPlayerController.cs` — 입력값을 읽어 `transform.position`을 좌우로 이동 (속도 상수 5f, Time.deltaTime 곱하기)
- [ ] **네트워크 코드 어떤 것도 호출하지 않음** (M1에서 만든 NetworkBootstrap 비활성화)

---

## ✅ 완료 조건

- [ ] Unity Play 모드 진입 → A/D 키로 캐릭터가 좌우로 움직임
- [ ] 카메라가 캐릭터를 부드럽게 따라감
- [ ] 60fps와 30fps 환경에서 같은 속도로 움직임 (deltaTime 검증)
- [ ] Scene 종료 시 콘솔에 에러 없음

---

## 🧪 테스트

**수동 테스트:**
- Play 모드 → A 누르고 있으면 왼쪽, D 누르고 있으면 오른쪽 이동
- Player Settings에서 `Target Frame Rate = 30` 설정해도 이동 속도 동일
- 키 안 누르면 정확히 정지

**자동 테스트:**
- 없음 (Unity scene 동작 검증은 이번 Phase에선 수동만)

---

## 📚 학습 포인트

- **GameObject vs Component**: Unity의 ECS 비슷한 구조. Player는 GameObject, SpriteRenderer/Collider는 Component.
- **Update vs FixedUpdate**: Update는 매 프레임, FixedUpdate는 고정 간격. 입력은 Update, 물리는 FixedUpdate가 관례 — 이번 Phase는 Update만.
- **Time.deltaTime**: 프레임 시간 간격(초). 곱해야 fps 무관한 움직임. `position += speed * deltaTime`.
- **Input System (신)**: 옛 `Input.GetKey`가 아닌 `InputAction`. asset 파일에 키 매핑 박고 C# 콜백으로 받음.

---

## ⚠️ 함정 / 주의사항

- `transform.position += new Vector3(x, 0, 0)` 직접 더하면 deltaTime 안 곱한 경우 fps에 따라 속도 달라짐 → 반드시 곱하기.
- Input System 설치 후 `Player Settings → Active Input Handling`을 `Input System Package (New)` 또는 `Both`로 변경 안 하면 입력 안 들어옴.
- Sprite Renderer의 `Order in Layer`를 신경 안 쓰면 Ground가 Player를 덮어버림.
- M1 NetworkBootstrap이 자동 시작되도록 짜였으면 비활성화 안 했을 때 Play 시 connect 시도 → 무관한 오류 발생.

---

## ➡️ 다음 Phase

- Phase 02: 서버 GameLoop — 20 TPS 틱 루프 + 단일 GameMap actor

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
