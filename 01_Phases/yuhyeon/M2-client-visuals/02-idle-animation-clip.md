# Phase 02: Idle 애니메이션 클립 + Animator Controller

> **상태**: pending
> **마일스톤**: M2-client-visuals
> **예상 소요**: 1~1.5시간
> **담당 에이전트**: client

---

## 🎯 목표

Phase 01에서 slice한 캐릭터 프레임으로 Idle AnimationClip을 만들고, Animator Controller에 Default State로 박아 캐릭터가 가만히 있어도 깜빡임/숨쉼이 보이게 한다.

**끝나면 데모 가능한 것**: Gameplay 씬에서 캐릭터가 가만히 서 있어도 자연스럽게 움직이는 Idle 애니메이션(반복 재생).

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (spritesheet 임포트 + slice + Player GameObject + SpriteRenderer)
- [ ] Idle용 프레임 개수 파악 (보통 4~8 프레임, spritesheet 페이지에 표시)

---

## 📝 작업 내용

- [ ] **폴더 신설**: `03_Client/Assets/Animations/Player/`
- [ ] **AnimationClip 자동 생성**: Project 뷰에서 spritesheet 펼친 후, Idle용 프레임 4~8개를 *순서대로* 선택 → Hierarchy의 Player GameObject로 드래그
  - Unity가 자동으로 "Save Animation" 다이얼로그 → `Assets/Animations/Player/Player_Idle.anim` 저장
  - 동시에 같은 폴더에 `Player.controller` (Animator Controller) 자동 생성
- [ ] **AnimationClip 설정 확인** (Player_Idle.anim 선택 → Inspector):
  - **Loop Time = 체크** (Idle은 반복)
  - Sample Rate = 12 또는 8 (픽셀아트 캐릭터 표준, 60은 너무 빠름)
- [ ] **Animator Controller 확인** (Player.controller 더블클릭 → Animator Window):
  - Default State = Idle (오렌지색)
  - 다른 State 없음 (Run은 Phase 03)
- [ ] **Player GameObject 확인**: Inspector에 Animator 컴포넌트 자동 추가됨, Controller 슬롯에 Player.controller 박혀있음
- [ ] **Play 진입 → 캐릭터가 Idle 반복 재생되는지 시각 확인**

---

## ✅ 완료 조건

- [ ] `Assets/Animations/Player/Player_Idle.anim` + `Player.controller` 2개 파일 + `.meta` 가 git 추적됨
- [ ] Player_Idle.anim의 Loop Time = true, Sample Rate = 12 (또는 8)
- [ ] Play 진입 시 캐릭터가 4~8 프레임을 자동 반복 (정지 X, 깜빡임 보임)
- [ ] 일시정지 메뉴(M1 Phase 04) 띄우면 애니메이션도 멈춤 (timeScale=0 정합)

---

## 🧪 테스트

**수동 테스트:**
1. Play 진입 → Gameplay 씬에서 캐릭터 시각 확인 (3~5초 반복 보기)
2. ESC → 일시정지 → 애니메이션 멈춤 확인 (timeScale=0이 Animator에도 적용됨)
3. 재개 → 애니메이션 다시 시작 확인

**자동 테스트:** 없음

---

## 📚 학습 포인트

- **AnimationClip**: 시간 축 위에 키프레임(여기선 sprite swap)이 박힌 데이터. `.anim` 파일.
- **Animator Controller**: 여러 클립을 State Machine으로 묶은 것. 어떤 클립이 언제 재생될지 결정. `.controller` 파일.
- **Mecanim (Animator) vs 옛 Animation**: Mecanim = 새 시스템(State Machine + 블렌딩), Animation = 옛 시스템(단순 재생). Mecanim 권장.
- **Loop Time**: 클립 끝나면 처음으로. Idle/Run에 필요, Attack/Jump엔 보통 X.
- **Sample Rate**: 1초당 키프레임 수. 픽셀아트 8~12 표준, 부드러운 3D는 24~30. 높을수록 frame 더 필요.
- **Default State**: Animator 시작 시 자동 재생되는 State. 오렌지색.
- **자동 드래그 워크플로우**: spritesheet 프레임 다중 선택 → GameObject 드래그가 Unity의 *지름길*. 수동으로 AnimationClip 만들고 키프레임 박을 수도 있지만 시간 소모.

---

## ⚠️ 함정 / 주의사항

- **프레임 순서 어긋남**: spritesheet에서 프레임 선택 시 순서대로 클릭(Idle_0, Idle_1, ...). 잘못 선택하면 캐릭터가 거꾸로 움직임. AnimationClip의 Curve Editor에서 키프레임 드래그로 수정 가능.
- **Sample Rate 기본값 60**: 자동 생성 시 60fps로 박힘 → 픽셀아트는 너무 빠름. 8/12로 낮춰야 자연스러움.
- **Loop Time 기본값 off**: 자동 생성 시 비활성 → Idle이 한 번만 재생되고 멈춤. 체크 필수.
- **Animator 컴포넌트 누락**: 드래그 워크플로우 안 쓰면 수동 추가 필요.
- **Player 위치 변경**: Animator가 transform.position 키프레임을 자동 박을 수 있음 (Sprite swap만 의도했는데). AnimationClip Inspector에서 Position 키프레임 보이면 우클릭 → 삭제. **이게 안 되면 Phase 04 이동 시 캐릭터가 원점으로 텔레포트되는 사고 발생** → 확인 필수.
- **timeScale=0과 Animator**: 기본 Update Mode = Normal이라 timeScale 영향 받음. M1 Phase 04 결정(일시정지 = timeScale=0) 정합 — 변경 X.

---

## ➡️ 다음 Phase

- **Phase 03 — Run 애니메이션 + Idle↔Run 전환**: 두 번째 클립 추가하고 Animator Parameter `IsMoving`으로 전환.

---

## 작업 로그

- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
