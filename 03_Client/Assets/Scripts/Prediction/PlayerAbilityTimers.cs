#nullable enable
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Prediction
{
    // 로컬 플레이어 능력 타이머 순수 클래스 — 쿨다운 / commit window / hit-bridge 게이트 상태.
    //
    // MonoBehaviour에서 분리해 EditMode 단위 테스트 가능하게 추출 (CODE_CONVENTION §3.1).
    // LocalPlayerMovement는 `_timers` 1필드로 위임 — 타이머 감쇠·getter·setter를 여기서 소유.
    internal sealed class PlayerAbilityTimers
    {
        // 로컬 공격 commit window 예측 잔여 시간(초). 서브스텝 박자(TickDuration씩) 감쇠.
        // 서버 AttackState(이동 잠금)를 같은 98_Shared 상수로 클라가 선예측 → reconcile rubber-band 0.
        float _commitWindowRemaining;

        // 현재 commit window가 스킬 시전(채널링)인지 평타인지 구분. OnChannel→true, OnAttack→false.
        // LocalPlayerMotion이 읽어 Attack 스윙 대신 Channeling 모션을 선예측. window 만료 시 자동 해제.
        bool _channelingWindow;

        // 공격 쿨다운(서버 rate-limit 거울) 잔여 — 0이면 재공격 가능. commit window(8틱)보다 길다(10틱).
        float _attackCooldownRemaining;

        // 스킬별 쿨다운(서버 쿨다운 거울) 잔여.
        // 각각 독립 — 한 스킬 쿨다운 중 다른 스킬은 사용 가능.
        float _thunderboltCooldownRemaining;
        float _dashCooldownRemaining;
        float _teleportCooldownRemaining;

        // 피격 hit-bridge 게이트 잔여(초). S_EnemyAttack(피격 *즉시* 신호) 도착 시 세팅 →
        // animState==Hit 스냅샷이 도착하기 전 갭 동안 입력을 미리 잠가 onset 당김을 줄인다.
        // 짧게만 — 진짜 hitstun 길이는 서버 전용이라 serverAnimState==Hit가 곧 이어받아 잠금 연장.
        float _hitGateRemaining;

        // hit-bridge 지속(틱). S_EnemyAttack~animState==Hit 스냅샷 사이 갭(≤1스냅샷)을 메우는 *클라 휴리스틱*.
        // 게임플레이 규칙 아님(서버 hitstun과 별개) → 98_Shared 아닌 클라 로컬 상수.
        const int HitGateBridgeTicks = 3; // ~150ms

        // === Getters ===

        public float CommitWindowRemaining => _commitWindowRemaining;

        public bool IsChannelingWindow => _channelingWindow && _commitWindowRemaining > 0f;

        public bool CanAttack => _attackCooldownRemaining <= 0f;

        // 하위 호환 프로퍼티 — 기존 Thunderbolt 게이트 코드가 CanUseSkill을 직접 참조.
        public bool CanUseSkill => _thunderboltCooldownRemaining <= 0f;
        public bool CanUseDash => _dashCooldownRemaining <= 0f;
        public bool CanUseTeleport => _teleportCooldownRemaining <= 0f;

        // IsActionLocked + substep localLock이 쓰던 식 — Mathf.Max(commitWindow, hitGate).
        public float LocalLockRemaining => Mathf.Max(_commitWindowRemaining, _hitGateRemaining);

        // HUD 폴링용 쿨다운 읽기 API.
        // 반환: (남은 초, 총 쿨다운 초). 미해당 스킬 또는 쿨다운 없음이면 (0, 0).
        // total은 Constants에서 계산 — 매핑을 한 곳에 두어 HUD가 Constants를 직접 읽지 않게 함(SRP).
        public (float remaining, float total) GetCooldown(SkillId skill)
        {
            return skill switch
            {
                SkillId.Thunderbolt => (_thunderboltCooldownRemaining,
                    Constants.ThunderboltCooldownTicks * Constants.TickDuration),
                SkillId.Dash        => (_dashCooldownRemaining,
                    Constants.DashCooldownTicks * Constants.TickDuration),
                SkillId.Teleport    => (_teleportCooldownRemaining,
                    Constants.TeleportCooldownTicks * Constants.TickDuration),
                _                   => (0f, 0f),
            };
        }

        // === Tick 감쇠 ===

        // frame dt 감쇠 — UI·쿨다운 타이머는 송신 박자와 무관한 표시용.
        public void TickFrame(float dt)
        {
            if (_attackCooldownRemaining > 0f)
                _attackCooldownRemaining = Mathf.Max(0f, _attackCooldownRemaining - dt);
            if (_thunderboltCooldownRemaining > 0f)
                _thunderboltCooldownRemaining = Mathf.Max(0f, _thunderboltCooldownRemaining - dt);
            if (_dashCooldownRemaining > 0f)
                _dashCooldownRemaining = Mathf.Max(0f, _dashCooldownRemaining - dt);
            if (_teleportCooldownRemaining > 0f)
                _teleportCooldownRemaining = Mathf.Max(0f, _teleportCooldownRemaining - dt);
        }

        // source-gating 타이머를 서브스텝 박자로 감쇠 — 게이트 깜빡임 방지.
        public void TickSubstep(float tickDuration)
        {
            if (_commitWindowRemaining > 0f)
                _commitWindowRemaining = Mathf.Max(0f, _commitWindowRemaining - tickDuration);
            if (_hitGateRemaining > 0f)
                _hitGateRemaining = Mathf.Max(0f, _hitGateRemaining - tickDuration);
        }

        // === 능력 이벤트 알림 (NotifyXxx의 타이머 세팅 부분) ===

        // 평타 송신 성공 시 — commit window + 공격 쿨다운 시작. 채널링 플래그 해제(평타).
        public void OnAttack()
        {
            _commitWindowRemaining = Constants.AttackCommitWindowTicks * Constants.TickDuration;
            _attackCooldownRemaining = Constants.AttackCooldownTicks * Constants.TickDuration;
            _channelingWindow = false;
        }

        // 스킬 시전(채널링) 송신 성공 시 — commit window + 썬더볼트 쿨다운 시작. 채널링 플래그 설정.
        public void OnChannel()
        {
            _commitWindowRemaining = Constants.AttackCommitWindowTicks * Constants.TickDuration;
            _thunderboltCooldownRemaining = Constants.ThunderboltCooldownTicks * Constants.TickDuration;
            _channelingWindow = true;
        }

        // Dash 송신 성공 시 — 쿨다운 + 대쉬 이동잠금 window 시작. 채널링 플래그 해제.
        public void OnDash()
        {
            _dashCooldownRemaining = Constants.DashCooldownTicks * Constants.TickDuration;
            _commitWindowRemaining = Constants.DashTravelTicks * Constants.TickDuration;
            _channelingWindow = false;
        }

        // Teleport 송신 성공 시 — 쿨다운 시작.
        public void OnTeleport()
        {
            _teleportCooldownRemaining = Constants.TeleportCooldownTicks * Constants.TickDuration;
        }

        // 피격(S_EnemyAttack) 시 — hit-bridge 게이트 시작.
        public void OnHit()
        {
            _hitGateRemaining = HitGateBridgeTicks * Constants.TickDuration;
        }
    }
}
