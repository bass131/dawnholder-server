#nullable enable
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using NUnit.Framework;
using Shared.Protocol;

namespace Dawnholder.Client.Tests
{
    // ClassLoadout.FindConfig + ByteToClass 단위 테스트 — 순수 함수, Unity 런타임 의존 없음.
    public class ClassLoadoutTests
    {
        KnightClassConfig CreateKnight(CharacterClass cls)
        {
            var cfg = UnityEngine.ScriptableObject.CreateInstance<KnightClassConfig>();
            cfg.Class = cls;
            return cfg;
        }

        [Test]
        public void FindConfig_MatchingClass_ReturnsConfig()
        {
            KnightClassConfig knight = CreateKnight(CharacterClass.Knight);
            ClassConfig[] configs = { knight };

            ClassConfig? result = ClassLoadout.FindConfig(configs, CharacterClass.Knight);

            Assert.IsNotNull(result);
            Assert.AreEqual(CharacterClass.Knight, result!.Class);
        }

        [Test]
        public void FindConfig_EmptyArray_ReturnsNull()
        {
            ClassConfig[] configs = System.Array.Empty<ClassConfig>();

            ClassConfig? result = ClassLoadout.FindConfig(configs, CharacterClass.Knight);

            Assert.IsNull(result);
        }

        [Test]
        public void FindConfig_NoMatchingClass_ReturnsNull()
        {
            KnightClassConfig knight = CreateKnight(CharacterClass.Knight);
            ClassConfig[] configs = { knight };

            ClassConfig? result = ClassLoadout.FindConfig(configs, CharacterClass.Mage);

            Assert.IsNull(result);
        }

        [Test]
        public void FindConfig_MultipleConfigs_ReturnsCorrectOne()
        {
            KnightClassConfig knight = CreateKnight(CharacterClass.Knight);
            KnightClassConfig mage = CreateKnight(CharacterClass.Mage);
            ClassConfig[] configs = { knight, mage };

            ClassConfig? result = ClassLoadout.FindConfig(configs, CharacterClass.Mage);

            Assert.IsNotNull(result);
            Assert.AreEqual(CharacterClass.Mage, result!.Class);
        }

        // ── ByteToClass ───────────────────────────────────────────────────────
        // 긍정 화이트리스트 방식 — 0/1은 매핑, 나머지는 Knight fallback + LogWarning.
        // LogWarning은 Unity Test Runner 실패 유발 X — LogAssert.Expect 불필요.

        [Test]
        public void ByteToClass_Zero_ReturnsKnight()
        {
            CharacterClass result = ClassLoadout.ByteToClass(0);
            Assert.AreEqual(CharacterClass.Knight, result);
        }

        [Test]
        public void ByteToClass_One_ReturnsMage()
        {
            CharacterClass result = ClassLoadout.ByteToClass(1);
            Assert.AreEqual(CharacterClass.Mage, result);
        }

        [Test]
        public void ByteToClass_Two_FallsBackToKnight()
        {
            // 2는 현재 정의된 클래스 없음 → Knight fallback + LogWarning.
            CharacterClass result = ClassLoadout.ByteToClass(2);
            Assert.AreEqual(CharacterClass.Knight, result);
        }

        [Test]
        public void ByteToClass_MaxByte_FallsBackToKnight()
        {
            // 255 (byte.MaxValue) — 알 수 없는 값 → Knight fallback + LogWarning.
            CharacterClass result = ClassLoadout.ByteToClass(255);
            Assert.AreEqual(CharacterClass.Knight, result);
        }
    }
}
