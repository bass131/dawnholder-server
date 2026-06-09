#nullable enable
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Input
{
    // 입력 번역 전담 컴포넌트 — Unity 입력 → 게임 의도 번역.
    //
    // **책임 1개**: Unity Input System 콜백을 받아 LocalPlayerMovement / IAttackStrategy에 전달.
    //   상태 보유 최소 — 입력 번역만.
    //
    // **콜백 wire**: PlayerInput component의 Behavior=Send Messages 모드에서
    //   OnMove/OnJump/OnAttack 메서드명이 자동 wire됨.
    // **스킬 키(Q)**: Input System 액션이 아닌 Update 폴링 — 임시 바인딩(리바인딩 UI는 범위 밖).
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(LocalPlayerMovement))]
    public class LocalPlayerInput : MonoBehaviour
    {
        IAttackStrategy _attackStrategy = null!;

        LocalPlayerMovement _movement = null!;

        void Awake()
        {
            _movement = GetComponent<LocalPlayerMovement>();
            // ClassConfig 미장착 시 fallback — Resolve 실패는 ClassLoadout.Resolve()가 fail-loud 처리.
            _attackStrategy = new KnightMeleeAttack();
        }

        // LocalPlayerSpawner가 ClassConfig 장착 시 호출해 전략 교체.
        public void SetAttackStrategy(IAttackStrategy strategy)
        {
            _attackStrategy = strategy;
        }

        // Input System "Move" 액션 콜백.
        void OnMove(InputValue value)
        {
            Vector2 raw = value.Get<Vector2>();
            _movement.SetMoveX(EncodeInputX(raw.x));
        }

        // "Jump" 액션 콜백 — 클라 에지 ("started" phase만 캡처).
        // PlayerInput component의 Behavior=Send Messages 모드에서 이 메서드명이 자동 wire.
        // 송신 cycle 전에 다시 누르면 같은 에지로 합쳐짐 (정상 — cadence별 1 점프).
        //
        // 공중 점프 차단은 점프 입력 *시점* OnGround 검사 (착지 직후 재점프 OK, 공중 점프 차단).
        // cadence 시점에 검사하면 Predict 후 OnGround=false 박혀서 지면 점프도 차단됨.
        // 헌법 #1 영향 X — 서버가 어차피 권위적으로 재검증, 본 게이트는 UX + 송신 절감용.
        void OnJump(InputValue value)
        {
            if (value.isPressed && _movement.OnGround)
                _movement.RequestJump();
        }

        // "Attack" 액션 콜백 (Space 또는 좌클릭) — down 에지만 처리.
        void OnAttack(InputValue value)
        {
            if (!value.isPressed) return; // up edge 무시 — down 시점 한 번만.
            // 공격 쿨다운(서버 rate-limit의 클라 거울, AttackCooldownTicks=500ms) 중이면 재입력 무시 —
            //   "한 번 들어간 공격은 끝까지 커밋". commit window(이동잠금 400ms)보다 긴 쿨다운으로 게이트해
            //   스윙 종료 후 재공격까지 대기 + 유령 스윙(클라 예측-서버 거부 갭) 차단. 서버 상수 단일 진실 거울.
            if (!_movement.CanAttack) return;
            // TryAttack: 세션 준비 시 C_Attack 송신(타겟 없으면 0 sentinel = 허공 스윙).
            //   송신 성공 시 NotifyAttack — 허공 스윙도 commit window + 쿨다운 예측 시작.
            //   세션 미준비(false) 시에만 생략 — 연결 전 입력은 아무 예측도 안 함.
            if (_attackStrategy.TryAttack(transform.position))
                _movement.NotifyAttack();
        }

        // 스킬 키 Q — 임시 바인딩 (정식 리바인딩 UI는 범위 밖). down 에지 폴링.
        // C_SkillUse 송신 + 로컬 캐스팅 commit window(이동 잠금) — 공격 쿨다운 공유.
        // 서버가 쿨다운·사거리·박스 대상 확정 → 쿨다운 중 재발동 시 silent drop.
        void Update()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current.qKey.wasPressedThisFrame) return;
            if (!_movement.CanAttack) return;

            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null) return;

            C_SkillUse pkt = new C_SkillUse
            {
                skillId = (byte)SkillId.Thunderbolt,
                attackerClientTick = session.LastReceivedServerTick
            };
            session.SendIntent(pkt.Write());
            Debug.Log($"[Skill] → Thunderbolt clientTick={pkt.attackerClientTick}");

            // 공격 commit window 재사용 — 캐스팅 중 이동 잠금 (서버 AttackState 정합).
            _movement.NotifyAttack();
        }

        // Vector2(아날로그 가능) → sbyte(-1/0/1) 변환.
        // 임계값 0.5 — 게임패드 아날로그 스틱 미세 흔들림 차단.
        static sbyte EncodeInputX(float x)
        {
            if (x > 0.5f) return 1;
            if (x < -0.5f) return -1;
            return 0;
        }
    }
}
