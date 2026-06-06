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
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Attack, onGround: true, moving: false);
            Assert.AreEqual(AnimState.Attack, result);
        }

        [Test]
        public void ResolveAnimState_ServerHit_ReturnsHit()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Hit, onGround: true, moving: true);
            Assert.AreEqual(AnimState.Hit, result);
        }

        [Test]
        public void ResolveAnimState_ServerDeath_ReturnsDeath()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Death, onGround: false, moving: false);
            Assert.AreEqual(AnimState.Death, result);
        }

        // 예측 우선 — serverState가 Idle/Walk/Jump이면 로컬 상태로 도출.

        [Test]
        public void ResolveAnimState_ServerIdle_NotOnGround_ReturnsJump()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Idle, onGround: false, moving: false);
            Assert.AreEqual(AnimState.Jump, result);
        }

        [Test]
        public void ResolveAnimState_ServerIdle_OnGround_Moving_ReturnsWalk()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Idle, onGround: true, moving: true);
            Assert.AreEqual(AnimState.Walk, result);
        }

        [Test]
        public void ResolveAnimState_ServerIdle_OnGround_NotMoving_ReturnsIdle()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Idle, onGround: true, moving: false);
            Assert.AreEqual(AnimState.Idle, result);
        }

        [Test]
        public void ResolveAnimState_ServerWalk_NotOnGround_ReturnsJump()
        {
            // serverState=Walk도 서버 latch 아님 — 예측 우선.
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Walk, onGround: false, moving: true);
            Assert.AreEqual(AnimState.Jump, result);
        }

        [Test]
        public void ResolveAnimState_ServerWalk_OnGround_Moving_ReturnsWalk()
        {
            AnimState result = LocalPlayerMotion.ResolveAnimState(AnimState.Walk, onGround: true, moving: true);
            Assert.AreEqual(AnimState.Walk, result);
        }
    }
}
