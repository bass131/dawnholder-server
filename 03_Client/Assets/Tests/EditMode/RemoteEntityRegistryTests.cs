#nullable enable
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.Protocol;

namespace Dawnholder.Client.Tests
{
    // RemoteEntityRegistry.NeedsRespawn 단위 테스트 — 순수 함수, Unity 런타임 의존 없음.
    //
    // NeedsRespawn 계약:
    //   incoming=null(정보 없음)          → false (강등 재생성 금지).
    //   recorded=null(미상) + incoming 有 → true (재생성).
    //   recorded == incoming              → false (noop).
    //   recorded != incoming              → true (재생성).
    public class RemoteEntityRegistryTests
    {
        [Test]
        public void NeedsRespawn_RecordedNull_ReturnsTrue()
        {
            // 직업 미상(Snapshot 선도착) → PlayerJoin 수신 시 반드시 재생성.
            bool result = RemoteEntityRegistry.NeedsRespawn(null, CharacterClass.Warrior);
            Assert.IsTrue(result);
        }

        [Test]
        public void NeedsRespawn_SameClass_ReturnsFalse()
        {
            // 동일 직업 재전송(PlayerJoin idempotent 재전송) → noop.
            bool result = RemoteEntityRegistry.NeedsRespawn(CharacterClass.Warrior, CharacterClass.Warrior);
            Assert.IsFalse(result);
        }

        [Test]
        public void NeedsRespawn_DifferentClass_ReturnsTrue()
        {
            // 직업 변경(Warrior → Ranger) → 올바른 variant로 재생성.
            bool result = RemoteEntityRegistry.NeedsRespawn(CharacterClass.Warrior, CharacterClass.Ranger);
            Assert.IsTrue(result);
        }

        [Test]
        public void NeedsRespawn_RangerToRanger_ReturnsFalse()
        {
            bool result = RemoteEntityRegistry.NeedsRespawn(CharacterClass.Ranger, CharacterClass.Ranger);
            Assert.IsFalse(result);
        }

        [Test]
        public void NeedsRespawn_IncomingNull_ReturnsFalse()
        {
            // 직업 정보 없는 호출(지연 spawn 류)은 기존 variant를 base로 강등시키면 안 됨.
            Assert.IsFalse(RemoteEntityRegistry.NeedsRespawn(CharacterClass.Warrior, null));
            Assert.IsFalse(RemoteEntityRegistry.NeedsRespawn(null, null));
        }
    }
}
