#nullable enable
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.Protocol;

namespace Dawnholder.Client.Tests
{
    // RemoteEntityRegistry.NeedsVisualSwap 단위 테스트 — 순수 함수, Unity 런타임 의존 없음.
    //
    // NeedsVisualSwap 계약:
    //   incoming=null(정보 없음)          → false (강등 교체 금지).
    //   recorded=null(미상) + incoming 有 → true (교체).
    //   recorded == incoming              → false (noop).
    //   recorded != incoming              → true (교체).
    public class RemoteEntityRegistryTests
    {
        [Test]
        public void NeedsVisualSwap_RecordedNull_ReturnsTrue()
        {
            // 직업 미상(Snapshot 선도착) → PlayerJoin 수신 시 반드시 비주얼 교체.
            bool result = RemoteEntityRegistry.NeedsVisualSwap(null, CharacterClass.Knight);
            Assert.IsTrue(result);
        }

        [Test]
        public void NeedsVisualSwap_SameClass_ReturnsFalse()
        {
            // 동일 직업 재전송(PlayerJoin idempotent 재전송) → noop.
            bool result = RemoteEntityRegistry.NeedsVisualSwap(CharacterClass.Knight, CharacterClass.Knight);
            Assert.IsFalse(result);
        }

        [Test]
        public void NeedsVisualSwap_DifferentClass_ReturnsTrue()
        {
            // 직업 변경(Knight → Mage) → 올바른 직업 비주얼로 교체.
            bool result = RemoteEntityRegistry.NeedsVisualSwap(CharacterClass.Knight, CharacterClass.Mage);
            Assert.IsTrue(result);
        }

        [Test]
        public void NeedsVisualSwap_MageToMage_ReturnsFalse()
        {
            bool result = RemoteEntityRegistry.NeedsVisualSwap(CharacterClass.Mage, CharacterClass.Mage);
            Assert.IsFalse(result);
        }

        [Test]
        public void NeedsVisualSwap_IncomingNull_ReturnsFalse()
        {
            // 직업 정보 없는 호출(지연 spawn 류)은 기존 variant를 base로 강등시키면 안 됨.
            Assert.IsFalse(RemoteEntityRegistry.NeedsVisualSwap(CharacterClass.Knight, null));
            Assert.IsFalse(RemoteEntityRegistry.NeedsVisualSwap(null, null));
        }
    }
}
