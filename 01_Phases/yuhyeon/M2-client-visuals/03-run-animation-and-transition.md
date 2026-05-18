# Phase 03: Run 애니메이션 + Idle↔Run 전환

> **상태**: pending
> **마일스톤**: M2-client-visuals
> **예상 소요**: 1.5~2시간
> **담당 에이전트**: client

---

## 🎯 목표

Phase 02의 Animator Controller에 Run 클립을 추가하고, Animator Parameter `IsMoving` (bool)으로 Idle↔Run 전환을 박는다. Play 모드에서 Animator Window의 파라미터 패널로 IsMoving을 수동 토글해 두 상태 전환이 시각적으로 확인되어야 한다.

**끝나면 데모 가능한 것**: Animator Window에서 IsMoving 체크박스 ON/OFF로 캐릭터가 Idle ↔ Run 즉시 전환. (실제 입력 wiring은 Phase 04)

---

## ⏪ 사전 조건

- [ ] Phase 02 완료 (Player_Idle.anim + Player.controller + Default State = Idle)
- [ ] spritesheet에 Run용 프레임 식별 (보통 4~8 프레임)

---

## 📝 작업 내용

- [ ] **Run AnimationClip 생성**: Project 뷰에서 Run 프레임 4~8개 선택 → Player GameObject로 드래그 → `Player_Run.anim` 저장
  - Loop Time = 체크
  - Sample Rate = 12 (Idle과 동일)
- [ ] **Animator에 Run State 추가**: Player.controller 더블클릭 → Animator Window
  - Project에서 Player_Run.anim을 Animator Window 빈 공간으로 드래그 → State 자동 생성
  - State 이름이 "Player_Run"이면 그대로 OK (가독성)
- [ ] **Parameter 추가**: Animator Window 왼쪽 Parameters 탭 → `+` → Bool → 이름 `IsMoving`
- [ ] **Transition 박기**:
  - Idle State 우클릭 → Make Transition → Run State 클릭 (화살표 생김)
  - 그 화살표 클릭 → Inspector에서:
    - **Has Exit Time = 체크 해제** (즉시 전환)
    - Transition Duration = **0** (픽셀아트는 블렌딩 X)
    - Conditions에 `IsMoving = true` 추가
  - 같은 방식으로 Run → Idle 화살표 박기 (Conditions: `IsMoving = false`)
- [ ] **수동 검증**: Play 진입 → Animator Window 띄운 상태에서 IsMoving 체크박스 토글 → Idle ↔ Run 즉시 전환 확인

---

## ✅ 완료 조건

- [ ] `Assets/Animations/Player/Player_Run.anim` + `.meta` git 추적
- [ ] Player.controller에 Idle, Run 두 State + IsMoving bool Parameter + 양방향 Transition 2개
- [ ] Play 모드에서 IsMoving 토글 시 두 애니메이션 *즉시* 전환 (지연 < 0.1초)
- [ ] Transition 잘못 박혀서 무한 루프 / 한쪽으로 빠지는 현상 없음

---

## 🧪 테스트

**수동 테스트:**
1. Play 진입 → 캐릭터 Idle 재생 확인
2. Animator Window의 IsMoving 체크 → Run으로 *즉시* 전환
3. IsMoving 해제 → Idle로 *즉시* 복귀
4. 빠르게 5회 토글 → 두 클립이 깜빡이며 정상 전환

**자동 테스트:** 없음

---

## 📚 학습 포인트

- **Animator Parameters**: State Machine을 외부에서 제어하는 변수. 종류 4개:
  - `bool` — true/false (IsMoving, IsGrounded)
  - `int` — 정수 (StateId, ComboCount)
  - `float` — 실수 (Speed, Health)
  - `trigger` — 한 번 켜지면 자동 리셋 (Jump, Attack 같은 일회성 액션)
- **Transition Condition**: 화살표가 발화될 조건. 여러 Condition이면 모두 만족(AND).
- **Has Exit Time**: 체크 = 현재 클립이 *몇 % 재생된 뒤* 전환 허용. 체크 해제 = 즉시. Idle↔Run은 즉시(해제), Attack→Idle 같은 건 보통 체크.
- **Transition Duration**: 두 클립 사이 *블렌딩 시간*. 3D 캐릭터는 0.1~0.3초 부드러움, 픽셀아트는 0초(블렌딩하면 두 sprite가 겹쳐 흐림).
- **Interruption Source**: 전환 중 다른 전환 받을지. 기본 None은 안전, 학부생 단계엔 그대로.
- **Sub-State Machine**: 복잡해지면 묶음으로 관리하는 기능. M2 범위 밖.

---

## ⚠️ 함정 / 주의사항

- **Has Exit Time 기본값 true**: 새 Transition 만들면 자동으로 true → IsMoving 켜도 Idle 클립 끝까지 기다림(즉시 반응 X). 픽셀아트 캐릭터엔 거의 항상 해제.
- **Transition Duration 너무 큰 값**: 0.5초 같은 게 박히면 두 sprite가 겹쳐 흐림. 픽셀아트는 0.
- **Condition 누락**: 빈 Condition 화살표는 무조건 발화 → 무한 루프. Condition 1개 이상 박아야 함.
- **양방향 화살표 누락**: Idle → Run만 박고 Run → Idle 안 박으면, 한 번 Run 가면 영원히 Run.
- **이름 typo**: Parameter 이름이 `IsMoving`인데 코드(Phase 04)에서 `isMoving`으로 SetBool하면 안 먹음 (대소문자 구분 + warning 안 뜸). 정확히 박기.
- **Default State 바꾸기**: State 우클릭 → Set as Layer Default State. Run이 Default가 되면 시작부터 Run.

---

## ➡️ 다음 Phase

- **Phase 04 — 캐릭터 이동 wiring**: PlayerController.cs로 입력 → 위치 변경 + IsMoving 자동 설정 + flipX.

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
