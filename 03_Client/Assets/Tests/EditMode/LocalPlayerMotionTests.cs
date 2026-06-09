#nullable enable
using Dawnholder.Client.Rendering;
using NUnit.Framework;
using Shared.GameData;

namespace Dawnholder.Client.Tests
{
    // LocalPlayerMotion.ResolveAnimState 순수 함수 단위 테스트.
    // 서버 우선(Attack/Hit/Death) vs 예측 상태(Jump/Walk/Idle) 우선순위 검증.
    public class LocalPlayerMotionTests
    {
        // 서버 latch 상태 — 예측 상태 무관하게 그대로 반환.

        [Test]
        public void ResolveAnimState_ServerAttack_ReturnsAttack()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Attack, localAttackPredicted: false, localChannelPredicted: false, onGround:true, moving: false);
            Assert.AreEqual(AnimState.Attack, result);
        }

        [Test]
        public void ResolveAnimState_ServerHit_ReturnsHit()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Hit, localAttackPredicted: false, localChannelPredicted: false, onGround:true, moving: true);
            Assert.AreEqual(AnimState.Hit, result);
        }

        [Test]
        public void ResolveAnimState_ServerDeath_ReturnsDeath()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Death, localAttackPredicted: false, localChannelPredicted: false, onGround:false, moving: false);
            Assert.AreEqual(AnimState.Death, result);
        }

        // 예측 우선 — serverState가 Idle/Walk/Jump이면 로컬 상태로 도출.

        [Test]
        public void ResolveAnimState_ServerIdle_NotOnGround_ReturnsJump()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Idle, localAttackPredicted: false, localChannelPredicted: false, onGround:false, moving: false);
            Assert.AreEqual(AnimState.Jump, result);
        }

        [Test]
        public void ResolveAnimState_ServerIdle_OnGround_Moving_ReturnsWalk()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Idle, localAttackPredicted: false, localChannelPredicted: false, onGround:true, moving: true);
            Assert.AreEqual(AnimState.Walk, result);
        }

        [Test]
        public void ResolveAnimState_ServerIdle_OnGround_NotMoving_ReturnsIdle()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Idle, localAttackPredicted: false, localChannelPredicted: false, onGround:true, moving: false);
            Assert.AreEqual(AnimState.Idle, result);
        }

        [Test]
        public void ResolveAnimState_ServerWalk_NotOnGround_ReturnsJump()
        {
            // serverState=Walk도 서버 latch 아님 — 예측 우선.
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Walk, localAttackPredicted: false, localChannelPredicted: false, onGround:false, moving: true);
            Assert.AreEqual(AnimState.Jump, result);
        }

        [Test]
        public void ResolveAnimState_ServerWalk_OnGround_Moving_ReturnsWalk()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Walk, localAttackPredicted: false, localChannelPredicted: false, onGround:true, moving: true);
            Assert.AreEqual(AnimState.Walk, result);
        }

        // 로컬 Attack 선예측 — serverState가 아직 Idle이어도 commit window 동안 Attack 표시 (M4.7 허공 스윙).

        [Test]
        public void ResolveAnimState_LocalAttackPredicted_ServerIdle_ReturnsAttack()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Idle, localAttackPredicted: true, localChannelPredicted: false, onGround: true, moving: false);
            Assert.AreEqual(AnimState.Attack, result);
        }

        [Test]
        public void ResolveAnimState_ServerHit_BeatsLocalAttackPredict()
        {
            // 피격은 로컬 Attack 예측보다 우선 — 서버 Hit 반응이 더 중요.
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Hit, localAttackPredicted: true, localChannelPredicted: false, onGround: true, moving: false);
            Assert.AreEqual(AnimState.Hit, result);
        }

        // 스킬 시전 선예측 — 같은 commit window라 localAttackPredicted도 true지만 Channeling이 Attack보다 우선.

        [Test]
        public void ResolveAnimState_LocalChannelPredicted_ReturnsChanneling()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Idle, localAttackPredicted: true, localChannelPredicted: true, onGround: true, moving: false);
            Assert.AreEqual(AnimState.Channeling, result);
        }

        [Test]
        public void ResolveAnimState_ServerHit_BeatsLocalChannelPredict()
        {
            // 피격은 시전 예측보다 우선 — 서버 Hit이 캐스팅을 끊는 게 정합(헌법 #1).
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Hit, localAttackPredicted: true, localChannelPredicted: true, onGround: true, moving: false);
            Assert.AreEqual(AnimState.Hit, result);
        }
    }
}
