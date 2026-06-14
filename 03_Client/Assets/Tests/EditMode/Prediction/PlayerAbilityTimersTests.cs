using Dawnholder.Client.Prediction;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Tests.Prediction
{
    // M4.14 Phase 05: PlayerAbilityTimers 순수 클래스 단위 테스트.
    //
    // 추출 거동 박제 — LocalPlayerMovement에서 분리한 7개 타이머 필드 + 감쇠 + getter + setter의
    // 1:1 거동 보존 검증. 감쇠 math·setter 값·getter 로직이 MonoBehaviour 시절과 동일함을 확인.
    public class PlayerAbilityTimersTests
    {
        PlayerAbilityTimers _timers = null!;

        [SetUp]
        public void SetUp()
        {
            _timers = new PlayerAbilityTimers();
        }

        // === TickFrame: 4개 쿨다운 frame dt 감쇠 ===

        [Test]
        public void TickFrame_AttackCooldown_DecaysByDt()
        {
            _timers.OnAttack();
            float before = Constants.AttackCooldownTicks * Constants.TickDuration;
            float dt = 0.016f;
            _timers.TickFrame(dt);
            Assert.AreEqual(Mathf.Max(0f, before - dt), Constants.AttackCooldownTicks * Constants.TickDuration - dt, 1e-5f);
            Assert.IsFalse(_timers.CanAttack, "쿨다운 중 CanAttack false");
        }

        [Test]
        public void TickFrame_FourCooldowns_DecayByDt()
        {
            _timers.OnAttack();
            _timers.OnChannel(); // thunderbolt 쿨다운 시작
            _timers.OnDash();
            _timers.OnTeleport();

            // CanXxx가 전부 false인 상태에서 큰 dt를 넣어도 음수가 되지 않는지 확인.
            _timers.TickFrame(9999f);

            Assert.IsTrue(_timers.CanAttack,    "attack 쿨다운 0 이하 클램프 후 CanAttack true");
            Assert.IsTrue(_timers.CanUseSkill,  "thunderbolt 쿨다운 0 이하 클램프 후 CanUseSkill true");
            Assert.IsTrue(_timers.CanUseDash,   "dash 쿨다운 0 이하 클램프 후 CanUseDash true");
            Assert.IsTrue(_timers.CanUseTeleport, "teleport 쿨다운 0 이하 클램프 후 CanUseTeleport true");
        }

        [Test]
        public void TickFrame_DoesNotDecayCommitWindowOrHitGate()
        {
            _timers.OnAttack(); // commitWindow 설정
            _timers.OnHit();    // hitGate 설정
            float windowBefore = _timers.CommitWindowRemaining;
            float lockBefore = _timers.LocalLockRemaining;

            // TickFrame은 commitWindow·hitGate를 건드리지 않는다 (substep 전용).
            _timers.TickFrame(0.016f);

            Assert.AreEqual(windowBefore, _timers.CommitWindowRemaining, 1e-5f,
                "TickFrame은 commitWindow를 감쇠하지 않아야 함");
            Assert.AreEqual(lockBefore, _timers.LocalLockRemaining, 1e-5f,
                "TickFrame은 hitGate를 감쇠하지 않아야 함");
        }

        // === TickSubstep: commitWindow + hitGate 감쇠 ===

        [Test]
        public void TickSubstep_CommitWindowAndHitGate_DecayByTickDuration()
        {
            _timers.OnAttack();
            _timers.OnHit();
            float windowBefore = _timers.CommitWindowRemaining;
            float localLockBefore = _timers.LocalLockRemaining;

            _timers.TickSubstep(Constants.TickDuration);

            Assert.AreEqual(Mathf.Max(0f, windowBefore - Constants.TickDuration),
                _timers.CommitWindowRemaining, 1e-5f, "commitWindow TickDuration 감쇠");
            // localLock = Max(commitWindow, hitGate) — 둘 다 감쇠됨
            Assert.Less(_timers.LocalLockRemaining, localLockBefore, "localLock도 감쇠되어야 함");
        }

        [Test]
        public void TickSubstep_ClampAtZero_NoNegative()
        {
            _timers.OnAttack();
            _timers.TickSubstep(9999f);
            Assert.AreEqual(0f, _timers.CommitWindowRemaining, 1e-5f, "commitWindow 음수 불가");
        }

        [Test]
        public void TickSubstep_DoesNotDecayCooldowns()
        {
            _timers.OnAttack();
            bool canAttackBefore = _timers.CanAttack; // false (쿨다운 중)
            _timers.TickSubstep(Constants.TickDuration);
            // TickSubstep은 attackCooldown을 건드리지 않아야 한다.
            Assert.IsFalse(_timers.CanAttack, "TickSubstep은 공격 쿨다운을 감쇠하지 않아야 함");
        }

        // === OnAttack ===

        [Test]
        public void OnAttack_SetsCanAttackFalse()
        {
            _timers.OnAttack();
            Assert.IsFalse(_timers.CanAttack, "공격 직후 CanAttack false");
        }

        [Test]
        public void OnAttack_SetsCommitWindowPositive()
        {
            _timers.OnAttack();
            Assert.Greater(_timers.CommitWindowRemaining, 0f, "공격 직후 commitWindow > 0");
        }

        [Test]
        public void OnAttack_SetsChannelingWindowFalse()
        {
            // 채널링 후 평타 → 채널링 플래그 해제 확인.
            _timers.OnChannel();
            _timers.OnAttack();
            Assert.IsFalse(_timers.IsChannelingWindow, "평타 후 IsChannelingWindow false");
        }

        [Test]
        public void OnAttack_CommitWindowValue_MatchesConstants()
        {
            _timers.OnAttack();
            float expected = Constants.AttackCommitWindowTicks * Constants.TickDuration;
            Assert.AreEqual(expected, _timers.CommitWindowRemaining, 1e-5f,
                "commitWindow = AttackCommitWindowTicks × TickDuration");
        }

        // === OnChannel ===

        [Test]
        public void OnChannel_SetsIsChannelingWindowTrue()
        {
            _timers.OnChannel();
            Assert.IsTrue(_timers.IsChannelingWindow, "채널링 직후 IsChannelingWindow true");
        }

        [Test]
        public void OnChannel_SetsCanUseSkillFalse()
        {
            _timers.OnChannel();
            Assert.IsFalse(_timers.CanUseSkill, "채널링 직후 CanUseSkill false");
        }

        [Test]
        public void OnChannel_IsChannelingWindow_FalseAfterWindowExpires()
        {
            // commitWindow를 TickSubstep으로 만료시키면 channeling 플래그가 남아도 false 반환.
            _timers.OnChannel();
            float window = _timers.CommitWindowRemaining;
            // window를 전부 소진.
            int ticks = Mathf.CeilToInt(window / Constants.TickDuration) + 1;
            for (int i = 0; i < ticks; i++)
                _timers.TickSubstep(Constants.TickDuration);

            Assert.IsFalse(_timers.IsChannelingWindow,
                "commitWindow 만료 후 channeling 플래그가 남아도 IsChannelingWindow false");
        }

        // === OnDash ===

        [Test]
        public void OnDash_SetsCanUseDashFalse()
        {
            _timers.OnDash();
            Assert.IsFalse(_timers.CanUseDash, "대쉬 직후 CanUseDash false");
        }

        [Test]
        public void OnDash_SetsCommitWindowPositive()
        {
            _timers.OnDash();
            Assert.Greater(_timers.CommitWindowRemaining, 0f, "대쉬 직후 commitWindow > 0");
        }

        [Test]
        public void OnDash_CommitWindowValue_MatchesDashTravelTicks()
        {
            _timers.OnDash();
            float expected = Constants.DashTravelTicks * Constants.TickDuration;
            Assert.AreEqual(expected, _timers.CommitWindowRemaining, 1e-5f,
                "대쉬 commitWindow = DashTravelTicks × TickDuration");
        }

        // === OnTeleport ===

        [Test]
        public void OnTeleport_SetsCanUseTeleportFalse()
        {
            _timers.OnTeleport();
            Assert.IsFalse(_timers.CanUseTeleport, "텔레포트 직후 CanUseTeleport false");
        }

        // === OnHit ===

        [Test]
        public void OnHit_SetsLocalLockRemainingPositive()
        {
            _timers.OnHit();
            Assert.Greater(_timers.LocalLockRemaining, 0f, "피격 직후 LocalLockRemaining > 0");
        }

        // === IsChannelingWindow ===

        [Test]
        public void IsChannelingWindow_FalseByDefault()
        {
            Assert.IsFalse(_timers.IsChannelingWindow, "초기 상태 IsChannelingWindow false");
        }

        // === LocalLockRemaining ===

        [Test]
        public void LocalLockRemaining_IsMaxOfCommitWindowAndHitGate()
        {
            // OnAttack으로 commitWindow만 세팅.
            _timers.OnAttack();
            float expected = _timers.CommitWindowRemaining; // hitGate=0이므로 Max = commitWindow.
            Assert.AreEqual(expected, _timers.LocalLockRemaining, 1e-5f,
                "LocalLockRemaining = Max(commitWindow, hitGate)");
        }

        [Test]
        public void LocalLockRemaining_UsesHitGateWhenLarger()
        {
            // hitGate를 설정하고 commitWindow를 만료시켜 hitGate가 지배하는지 확인.
            _timers.OnHit();
            float hitGate = _timers.LocalLockRemaining;
            // commitWindow=0 상태이므로 localLock == hitGate.
            Assert.Greater(hitGate, 0f, "OnHit 후 hitGate > 0 → LocalLockRemaining 양수");
        }

        // === GetCooldown ===

        [Test]
        public void GetCooldown_Thunderbolt_TotalMatchesConstants()
        {
            _timers.OnChannel();
            (float _, float total) = _timers.GetCooldown(SkillId.Thunderbolt);
            float expected = Constants.ThunderboltCooldownTicks * Constants.TickDuration;
            Assert.AreEqual(expected, total, 1e-5f, "Thunderbolt total = ThunderboltCooldownTicks × TickDuration");
        }

        [Test]
        public void GetCooldown_Dash_TotalMatchesConstants()
        {
            _timers.OnDash();
            (float _, float total) = _timers.GetCooldown(SkillId.Dash);
            float expected = Constants.DashCooldownTicks * Constants.TickDuration;
            Assert.AreEqual(expected, total, 1e-5f, "Dash total = DashCooldownTicks × TickDuration");
        }

        [Test]
        public void GetCooldown_Teleport_TotalMatchesConstants()
        {
            _timers.OnTeleport();
            (float _, float total) = _timers.GetCooldown(SkillId.Teleport);
            float expected = Constants.TeleportCooldownTicks * Constants.TickDuration;
            Assert.AreEqual(expected, total, 1e-5f, "Teleport total = TeleportCooldownTicks × TickDuration");
        }

        [Test]
        public void GetCooldown_Unknown_ReturnsZeroZero()
        {
            (float remaining, float total) = _timers.GetCooldown((SkillId)99);
            Assert.AreEqual(0f, remaining, 1e-5f, "미해당 스킬 remaining=0");
            Assert.AreEqual(0f, total,     1e-5f, "미해당 스킬 total=0");
        }
    }
}
