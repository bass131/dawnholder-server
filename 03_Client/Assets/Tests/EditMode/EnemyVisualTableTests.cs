#nullable enable
using Dawnholder.Client.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests
{
    // EnemyVisualTable lookup 단위 테스트.
    // CreateInstance로 인메모리 구성 — Resources 의존 없음.
    public class EnemyVisualTableTests
    {
        EnemyVisualTable CreateTable(params EnemyVisualTable.Entry[] entries)
        {
            var table = ScriptableObject.CreateInstance<EnemyVisualTable>();
            table.SetEntriesForTest(entries);
            return table;
        }

        GameObject MakeDummyPrefab(string name)
        {
            // 실제 prefab 에셋 없이 GO 참조만 필요. TearDown 정리 규약과 일치하게 prefix 강제.
            return new GameObject(name.StartsWith("TestPrefab_") ? name : $"TestPrefab_{name}");
        }

        [TearDown]
        public void TearDown()
        {
            // MakeDummyPrefab으로 만든 씬 내 GO 정리.
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.name.StartsWith("TestPrefab_")) Object.DestroyImmediate(go);
        }

        [Test]
        public void GetPrefab_RegisteredKind_ReturnsPrefab()
        {
            GameObject normalPrefab = MakeDummyPrefab("TestPrefab_Normal");
            var table = CreateTable(new EnemyVisualTable.Entry
            {
                Kind = RemoteEnemy.EnemyKind.Normal,
                Prefab = normalPrefab
            });

            GameObject? result = table.GetPrefab(RemoteEnemy.EnemyKind.Normal);

            Assert.IsNotNull(result);
            Assert.AreSame(normalPrefab, result);
        }

        [Test]
        public void GetPrefab_UnregisteredKind_LogsErrorAndFallsBackToNormal()
        {
            GameObject normalPrefab = MakeDummyPrefab("TestPrefab_Normal");
            var table = CreateTable(new EnemyVisualTable.Entry
            {
                Kind = RemoteEnemy.EnemyKind.Normal,
                Prefab = normalPrefab
            });

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[EnemyVisualTable\].*Boss.*미등록"));

            GameObject? result = table.GetPrefab(RemoteEnemy.EnemyKind.Boss);

            Assert.IsNotNull(result, "Normal prefab 폴백 결과가 null이면 안 됩니다.");
            Assert.AreSame(normalPrefab, result);
        }

        [Test]
        public void GetPrefab_UnregisteredKindWithNoNormal_LogsErrorAndReturnsNull()
        {
            // Normal도 없는 빈 테이블.
            var table = CreateTable();

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[EnemyVisualTable\].*Boss.*미등록"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[EnemyVisualTable\].*Normal.*없습니다"));

            GameObject? result = table.GetPrefab(RemoteEnemy.EnemyKind.Boss);

            Assert.IsNull(result);
        }

        [Test]
        public void GetPrefab_BothKindsRegistered_ReturnsCorrectPrefab()
        {
            GameObject normalPrefab = MakeDummyPrefab("TestPrefab_Normal");
            GameObject bossPrefab = MakeDummyPrefab("TestPrefab_Boss");
            var table = CreateTable(
                new EnemyVisualTable.Entry { Kind = RemoteEnemy.EnemyKind.Normal, Prefab = normalPrefab },
                new EnemyVisualTable.Entry { Kind = RemoteEnemy.EnemyKind.Boss, Prefab = bossPrefab }
            );

            Assert.AreSame(normalPrefab, table.GetPrefab(RemoteEnemy.EnemyKind.Normal));
            Assert.AreSame(bossPrefab, table.GetPrefab(RemoteEnemy.EnemyKind.Boss));
        }
    }
}
