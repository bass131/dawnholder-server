# Phase 04: 캐릭터 이동 시각 wiring (PlayerAnimatorSync)

> **상태**: pending
> **마일스톤**: M2-client-visuals
> **예상 소요**: 30분~1시간 (대폭 축소 — 기존 prediction layer 활용)
> **담당 에이전트**: client

> **⚠️ 2026-05-19 갱신**: 기존 코드 read 후 *전면 rewrite*. LocalPlayerController.cs(팀장 M2 prediction 골격)가 *이미 transform.position 갱신*하고 있음. Phase 04는 *시각 wiring(Animator + flipX)만* 추가 — 위치 변경 코드는 *건드리지 X*.

---

## 🎯 목표

`PlayerAnimatorSync.cs` 새 컴포넌트 신설 — Player의 transform.position 변화를 감지해 `Animator.SetBool("IsMoving", ...)` + `SpriteRenderer.flipX` 갱신.

**LocalPlayerController.cs는 안 만짐** (공유 영역 + 팀장 prediction 골격 보존 + 권한 분리). 별도 컴포넌트가 같은 Player GameObject 위에서 *시각 책임만* 분담.

**끝나면 데모 가능한 것**: ★ 5/20 수 교수님 면담 데모 도착점 ★ — GameplayTest에서 A/D 키로 캐릭터 좌우 이동 (이미 prediction 동작) + Idle↔Run 애니 즉시 전환 + 방향 따라 flipX 좌우 반전.

---

## ⏪ 사전 조건

- [ ] Phase 01~03 완료 (sprite + Idle/Run 클립 + Animator + IsMoving Parameter + 양방향 Transition)
- [ ] LocalPlayerController.cs + PlayerPredictor.cs read 완료 — *팀장 prediction 골격 이해*
- [ ] **InputSystem_Actions의 Default Map 점검** — Inspector에 `<None>`이면 OnMove 콜백 안 와서 캐릭터 정지 (Pre-step 0)
- [ ] A/D 이동이 prediction transform 갱신까지 도달 — Pre-step 0.5에서 회귀

---

## 📝 작업 내용

### Pre-step 0: Input System Default Map 점검 (5분)

본인이 GameplayTest 씬 → Player GameObject → **PlayerInput 컴포넌트** → Inspector:
- **Default Map** 슬롯이 `<None>`이면 → drop-down에서 `Player` (또는 InputSystem_Actions의 Action Map 이름) 선택
- 이미 설정돼 있으면 → 스킵

### Pre-step 0.5: A/D 이동 작동 회귀 (5분)

본인 Play → GameplayTest → A/D 키 눌러 캐릭터 좌우 이동 확인:
- 이동 OK → Step 1 진입
- 이동 X → "Move" InputAction binding 확인 (Vector2 Composite — WASD + LeftStick 기본)

### Step 1: PlayerAnimatorSync.cs 신설 (10분)

- [ ] `03_Client/Assets/Scripts/Rendering/PlayerAnimatorSync.cs` 신설:

```csharp
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // M2 Phase 04: 캐릭터 이동 시각 wiring.
    // LocalPlayerController(prediction)가 갱신한 transform.position 변화를
    // 감지해 Animator.SetBool("IsMoving", ...) + flipX 적용.
    //
    // 위치 변경 자체는 LocalPlayerController가 처리 (헌법 #1 prediction layer).
    // 본 컴포넌트는 *시각만* 갱신 — 권한 분리 + 본인 영역 자체 완결.
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public class PlayerAnimatorSync : MonoBehaviour
    {
        const float MoveEpsilon = 0.001f;

        Animator _anim;
        SpriteRenderer _sr;
        float _lastX;

        void Awake()
        {
            _anim = GetComponent<Animator>();
            _sr = GetComponent<SpriteRenderer>();
            _lastX = transform.position.x;
        }

        void LateUpdate()
        {
            float dx = transform.position.x - _lastX;
            bool moving = Mathf.Abs(dx) > MoveEpsilon;
            _anim.SetBool("IsMoving", moving);
            if (moving) _sr.flipX = (dx < 0f);
            _lastX = transform.position.x;
        }
    }
}
```

### Step 2: Player GameObject에 컴포넌트 추가 (1분)

- [ ] GameplayTest 씬 → Player GameObject → Inspector → Add Component → `PlayerAnimatorSync`

### Step 3: Play 회귀 (5~10분)

- [ ] Play → 시작 → GameplayTest
- [ ] A/D 키 → 캐릭터 좌우 이동 + **Run 애니 즉시 전환** + **방향 따라 flipX**
- [ ] 멈춤 → **Idle 애니 즉시 복귀**, flipX 마지막 방향 유지
- [ ] ESC 일시정지 → 캐릭터 이동/애니/sync 모두 멈춤 (timeScale=0 정합)
- [ ] 재개 → 복구

---

## ✅ 완료 조건

- [ ] `Scripts/Rendering/PlayerAnimatorSync.cs` 신설 + git 추적
- [ ] Player GameObject에 PlayerAnimatorSync 컴포넌트 박힘
- [ ] A/D 키로 캐릭터 좌우 이동 (prediction 작동) + Run 애니 즉시 전환 + flipX 정상
- [ ] 멈춤 → Idle 즉시 복귀, flipX 마지막 방향 유지
- [ ] M1 회귀 6 시나리오 + M2 추가 (이동 + flipX + 애니 전환) 모두 정상
- [ ] LocalPlayerController.cs *수정 0줄* (공유 영역 보존)

---

## 🧪 테스트

**수동 테스트**:
- 위 회귀 흐름 5회 반복
- A→D 빠른 전환: 캐릭터 즉시 방향 전환 (flipX 토글 매끄러움)
- 정지 중 ESC: 일시정지 정합

**자동 테스트**: 없음 (Phase 08에서 통합 회귀)

---

## 📚 학습 포인트

- **권한 분리 + 책임 분담**: LocalPlayerController(공유 영역, 팀장 prediction)는 *건드리지 X*. 같은 Player GameObject에 *별도 컴포넌트*(본인 영역) 신설해 시각 책임만 분담. 시니어 마인드셋 — *팀장 코드는 read first, 추가만*.
- **변화 감지 패턴 (delta detection)**: transform.position 절대값 X, 매 LateUpdate *이전 X와 차이*로 moving 판단. 이벤트 기반(LocalPlayerController에서 직접 콜백) 대안보다 *느슨한 결합* — LocalPlayerController가 본 컴포넌트의 존재 모름.
- **Update vs LateUpdate**: PlayerAnimatorSync는 *LateUpdate*. LocalPlayerController.Update가 transform 갱신한 *뒤* 읽어야 정확. 카메라 follow와 같은 패턴.
- **MoveEpsilon (부동소수점 비교)**: 정확 0과 비교 X — 0.001 임계값. PPU 64 기준 1픽셀의 1/16 ≈ 무시 가능 jitter.
- **헌법 #1 정합**: 본인 코드는 위치 변경 X (LocalPlayerController가 prediction layer로 이미 처리). 시각만 = 권한 우회 0.
- **`[RequireComponent]` 사용**: Animator + SpriteRenderer가 필수 — 컴포넌트 추가 시 자동 보장. 의존성 명시 패턴.

---

## ⚠️ 함정 / 주의사항

- **Default Map `<None>` 함정** (Pre-step 0): PlayerInput Inspector의 Default Map 미설정이면 OnMove 콜백 안 와서 _moveInput=(0,0) 고정 → 캐릭터 정지 → 본 Phase wiring도 효과 X. 점검 필수.
- **transform.position 변화 = 입력 + reconcile snap**: M3 서버 합류 후 reconcile snap 시 *입력 없는데 dx > epsilon* 발생 가능 → 잠시 Run 애니 false positive. M3+ 정밀화 검토 (LocalPlayerController.Instance._moveInput 직접 접근으로 전환).
- **MoveEpsilon 튜닝**: 0.001 너무 작으면 picky / 너무 크면 둔감. 0.01까지 OK. M2 마감 후 면담 시연 보고 조정.
- **flipX `if (moving)` gate**: x=0일 때 변경하면 멈춤 직전 방향이 의도와 다를 수 있음. 가드 박혀있음.
- **Animator/SpriteRenderer 누락**: `[RequireComponent]`가 자동 추가하지만 Controller 슬롯/Sprite 슬롯이 빈 채로 추가됨 → 효과 X. Phase 02·03 박힌 wiring 확인.
- **헌법 #1 우회 *없음* — 임시 약속 불필요**: 기존 Phase 04 파일에 박혔던 "임시 우회 약속"은 LocalPlayerController가 이미 prediction layer로 박혀있어 *부정확*. 본 Phase 작업은 시각만이라 권한 깔끔.

---

## ➡️ 다음 Phase

- **Phase 05 — 정적 배경 1장 + Sorting Layer**: Kenney 또는 다른 자원 배경 추가 + Sorting Layer 셋업.

---

## 작업 로그

- 2026-05-19: 기존 Phase 04 파일 read 후 *전면 rewrite* (LocalPlayerController가 이미 완전한 prediction 클라이언트임 발견 — 시각 wiring만 추가 방향)
- YYYY-MM-DD: 시작
- YYYY-MM-DD: 완료. 학습한 것: ...
