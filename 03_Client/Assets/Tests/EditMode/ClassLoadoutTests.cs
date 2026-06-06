#nullable enable
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using NUnit.Framework;
using Shared.Protocol;

namespace Dawnholder.Client.Tests
{
    // ClassLoadout.FindConfig 단위 테스트 — 순수 함수, Unity 런타임 의존 없음.
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
    }
}
