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
            KnightClassConfig knight = CreateKnight(CharacterClass.Warrior);
            ClassConfig[] configs = { knight };

            ClassConfig? result = ClassLoadout.FindConfig(configs, CharacterClass.Warrior);

            Assert.IsNotNull(result);
            Assert.AreEqual(CharacterClass.Warrior, result!.Class);
        }

        [Test]
        public void FindConfig_EmptyArray_ReturnsNull()
        {
            ClassConfig[] configs = System.Array.Empty<ClassConfig>();

            ClassConfig? result = ClassLoadout.FindConfig(configs, CharacterClass.Warrior);

            Assert.IsNull(result);
        }

        [Test]
        public void FindConfig_NoMatchingClass_ReturnsNull()
        {
            KnightClassConfig knight = CreateKnight(CharacterClass.Warrior);
            ClassConfig[] configs = { knight };

            ClassConfig? result = ClassLoadout.FindConfig(configs, CharacterClass.Ranger);

            Assert.IsNull(result);
        }

        [Test]
        public void FindConfig_MultipleConfigs_ReturnsCorrectOne()
        {
            KnightClassConfig warrior = CreateKnight(CharacterClass.Warrior);
            KnightClassConfig ranger = CreateKnight(CharacterClass.Ranger);
            ClassConfig[] configs = { warrior, ranger };

            ClassConfig? result = ClassLoadout.FindConfig(configs, CharacterClass.Ranger);

            Assert.IsNotNull(result);
            Assert.AreEqual(CharacterClass.Ranger, result!.Class);
        }

        // ── ByteToClass ───────────────────────────────────────────────────────
        // 긍정 화이트리스트 방식 — 0/1은 매핑, 나머지는 Warrior fallback + LogWarning.
        // LogWarning은 Unity Test Runner 실패 유발 X — LogAssert.Expect 불필요.

        [Test]
        public void ByteToClass_Zero_ReturnsWarrior()
        {
            CharacterClass result = ClassLoadout.ByteToClass(0);
            Assert.AreEqual(CharacterClass.Warrior, result);
        }

        [Test]
        public void ByteToClass_One_ReturnsRanger()
        {
            CharacterClass result = ClassLoadout.ByteToClass(1);
            Assert.AreEqual(CharacterClass.Ranger, result);
        }

        [Test]
        public void ByteToClass_Two_FallsBackToWarrior()
        {
            // 2는 현재 정의된 클래스 없음 → Warrior fallback + LogWarning.
            CharacterClass result = ClassLoadout.ByteToClass(2);
            Assert.AreEqual(CharacterClass.Warrior, result);
        }

        [Test]
        public void ByteToClass_MaxByte_FallsBackToWarrior()
        {
            // 255 (byte.MaxValue) — 알 수 없는 값 → Warrior fallback + LogWarning.
            CharacterClass result = ClassLoadout.ByteToClass(255);
            Assert.AreEqual(CharacterClass.Warrior, result);
        }
    }
}
