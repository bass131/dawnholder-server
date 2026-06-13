#nullable enable
using System.Collections.Generic;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.Rendering;
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
    // **스킬 키(Q/E)**: Input System 액션이 아닌 Update 폴링 — 임시 바인딩(리바인딩 UI는 범위 밖).
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(LocalPlayerMovement))]
    public class LocalPlayerInput : MonoBehaviour
    {
        IAttackStrategy _attackStrategy = null!;

        LocalPlayerMovement _movement = null!;
        LocalPlayerMotion? _motion;

        // 클래스별 스킬 키 매핑 단일 진실.
        // 키 배치 변경 시 이 테이블 한 곳만 수정. Phase 04/06에서 연출 추가 시도 여기서 SkillId 조회.
        //
        // | 클래스 | Q 키         | E 키      |
        // |--------|--------------|-----------|
        // | Mage   | Thunderbolt  | Teleport  |
        // | Knight | Dash         | (없음)    |
        //
        // SkillCatalog.CanCast 게이트로 내 클래스가 못 쓰는 스킬은 송신 차단 (UX + 트래픽 절감).
        // 헌법 §1: 클라 게이트는 편의, 진짜 권위는 서버 — 서버도 별도 검증.
        internal static readonly Dictionary<CharacterClass, (SkillId q, SkillId e)> SkillKeyMap =
            new Dictionary<CharacterClass, (SkillId q, SkillId e)>
            {
                { CharacterClass.Mage,   (SkillId.Thunderbolt, SkillId.Teleport) },
                { CharacterClass.Knight, (SkillId.Dash,        SkillId.None)     },
            };

        void Awake()
        {
            _movement = GetComponent<LocalPlayerMovement>();
            _motion = GetComponent<LocalPlayerMotion>();
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
            if (_movement.IsActionLocked) return; // 서버 ActionGate 클라 거울 — Attack/Hit/Death 중 차단.
            // TryAttack: 세션 준비 시 C_Attack 송신(타겟 없으면 0 sentinel = 허공 스윙).
            //   송신 성공 시 NotifyAttack — 허공 스윙도 commit window + 쿨다운 예측 시작.
            //   세션 미준비(false) 시에만 생략 — 연결 전 입력은 아무 예측도 안 함.
            if (_attackStrategy.TryAttack(transform.position))
            {
                _movement.NotifyAttack();
                TryFaceNearestTarget();
            }
        }

        // 공격 발동 후 가장 가까운 타겟 방향으로 facing 보정 — 로컬 연출 전용 (헌법 #1).
        // 타겟 없으면 현재 이동 방향 유지. 전략별 TargetingRangeSquared 재사용 — 타겟 잡히는 적 = 바라보는 적.
        void TryFaceNearestTarget()
        {
            if (_motion == null || EnemyRegistry.Instance == null) return;
            if (!EnemyRegistry.Instance.TryGetNearest(transform.position, _attackStrategy.TargetingRangeSquared, out int tid)) return;
            if (tid == 0) return;
            if (!EnemyRegistry.Instance.TryGetTransform(tid, out Transform? et) || et == null) return;
            _motion.FaceToward(et.position.x);
        }

        // 스킬 키 Q / E — 임시 바인딩 (정식 리바인딩 UI는 범위 밖). down 에지 폴링.
        // 클래스별 SkillKeyMap에서 SkillId 조회 → SkillCatalog.CanCast 게이트 → C_SkillUse 송신.
        void Update()
        {
            if (Keyboard.current == null) return;

            bool qDown = Keyboard.current.qKey.wasPressedThisFrame;
            bool eDown = Keyboard.current.eKey.wasPressedThisFrame;
            if (!qDown && !eDown) return;

            CharacterClass myClass = ClassLoadout.SessionSelectedClass
                ?? (CharacterClass)ClassLoadout.GetSelectedClassValue((int)CharacterClass.Knight);

            if (!SkillKeyMap.TryGetValue(myClass, out (SkillId q, SkillId e) mapping)) return;

            SkillId skillId = qDown ? mapping.q : mapping.e;
            TrySendSkill(skillId, myClass);
        }

        // 스킬 송신 공통 경로.
        // 게이트 순서: None 필터 → 클래스 자격(CanCast) → 행동 잠금 → 스킬별 쿨다운 게이트 → 세션 준비.
        void TrySendSkill(SkillId skillId, CharacterClass myClass)
        {
            if (skillId == SkillId.None) return;
            if (!SkillCatalog.CanCast(myClass, skillId)) return;
            if (_movement.IsActionLocked) return; // 서버 ActionGate 클라 거울 — Attack/Hit/Death 중 차단.

            // 스킬별 쿨다운 게이트 — Constants 상수 거울. 서버도 별도 검증(헌법 §1). 클라 게이트는 UX + 트래픽 절감.
            switch (skillId)
            {
                case SkillId.Thunderbolt:
                    if (!_movement.CanUseSkill) return;
                    break;
                case SkillId.Dash:
                    if (!_movement.CanUseDash) return;
                    break;
                case SkillId.Teleport:
                    if (!_movement.CanUseTeleport) return;
                    break;
            }

            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null) return;

            // facing(M4.13 v13): 클라 화면 방향(0=left/1=right) — 서버가 대쉬 방향 권위로 사용.
            //   대쉬 예측(NotifyDash→StartImpulse)과 동일 출처(_motion.Facing)라 클라/서버 대쉬 방향 일치 →
            //   방향전환 직후 대쉬가 서버 입력 큐 지연으로 반대로 튀던 reconcile 클러스터 봉합.
            int facingSign = _motion != null ? _motion.Facing : 1;
            C_SkillUse pkt = new C_SkillUse
            {
                skillId = (byte)skillId,
                attackerClientTick = session.LastReceivedServerTick,
                facing = (byte)(facingSign == 1 ? 1 : 0)
            };
            session.SendIntent(pkt.Write());
            Debug.Log($"[Skill] → {skillId} clientTick={pkt.attackerClientTick}");

            // 선예측 커밋: 스킬별로 분기.
            // Thunderbolt: 채널링 모션 + 쿨다운 예측.
            // Dash: 쿨다운만 예측. 이동/모션은 서버 S_SkillCast(Dash) + S_Snapshot(Attack) 수신 연출로 충분.
            // Teleport: 쿨다운 예측 + 다음 Snapshot force-adopt 플래그(보간 끊기).
            switch (skillId)
            {
                case SkillId.Thunderbolt:
                    _movement.NotifyChannel();
                    break;
                case SkillId.Dash:
                    _movement.NotifyDash();
                    break;
                case SkillId.Teleport:
                    _movement.NotifyTeleport();
                    break;
            }
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
