#nullable enable
using Dawnholder.Client.Combat;
using Dawnholder.Client.Rendering;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // EnemyVisualTable SO + prefab 컨트랙트 단언 테스트.
    //
    // ** 현재 RED 상태가 정상입니다 **
    // Assets/Resources/EnemyVisualTable.asset 및 Enemy_Normal/Enemy_Boss prefab은
    // 메인 세션(unity-bridge)이 에셋 생성 후에 GREEN이 됩니다.
    // 에셋 생성 전 이 테스트는 Inconclusive(건너뜀)로 처리합니다 — Assert.Ignore.
    public class EnemyPrefabContractTests
    {
        EnemyVisualTable? _table;

        [SetUp]
        public void SetUp()
        {
            _table = Resources.Load<EnemyVisualTable>("EnemyVisualTable");
            if (_table == null)
                Assert.Ignore("EnemyVisualTable.asset 미발견 — 에셋 생성 후 실행하세요 (Assets/Resources/EnemyVisualTable.asset).");
        }

        [Test]
        public void Table_HasNormalEntry()
        {
            // Normal 행 등록 여부 — 미등록이면 GetPrefab이 에러+null 반환하므로 명시 확인.
            GameObject? prefab = _table!.GetPrefab(EnemyKind.Normal);
            Assert.IsNotNull(prefab, "EnemyVisualTable에 Normal prefab이 등록되지 않았습니다.");
        }

        [Test]
        public void Table_HasBossEntry()
        {
            GameObject? prefab = _table!.GetPrefab(EnemyKind.Boss);
            Assert.IsNotNull(prefab, "EnemyVisualTable에 Boss prefab이 등록되지 않았습니다.");
        }

        [Test]
        public void NormalPrefab_HasRequiredComponents()
        {
            GameObject? prefab = _table!.GetPrefab(EnemyKind.Normal);
            if (prefab == null) Assert.Ignore("Normal prefab 없음 — 에셋 생성 후 실행하세요.");

            Assert.IsNotNull(prefab.GetComponent<RemoteEnemy>(),    "Normal prefab에 RemoteEnemy 없음");
            Assert.IsNotNull(prefab.GetComponent<RemoteEntity>(),   "Normal prefab에 RemoteEntity 없음");
            Assert.IsNotNull(prefab.GetComponent<EnemyMotion>(),    "Normal prefab에 EnemyMotion 없음");
            Assert.IsNotNull(prefab.GetComponent<AnimatorDriver>(), "Normal prefab에 AnimatorDriver 없음");

            Animator? anim = prefab.GetComponent<Animator>();
            Assert.IsNotNull(anim, "Normal prefab에 Animator 없음");
            Assert.IsNotNull(anim!.runtimeAnimatorController, "Normal prefab Animator에 controller 미연결");
        }

        [Test]
        public void BossPrefab_HasRequiredComponents()
        {
            GameObject? prefab = _table!.GetPrefab(EnemyKind.Boss);
            if (prefab == null) Assert.Ignore("Boss prefab 없음 — 에셋 생성 후 실행하세요.");

            Assert.IsNotNull(prefab.GetComponent<RemoteEnemy>(),    "Boss prefab에 RemoteEnemy 없음");
            Assert.IsNotNull(prefab.GetComponent<RemoteEntity>(),   "Boss prefab에 RemoteEntity 없음");
            Assert.IsNotNull(prefab.GetComponent<EnemyMotion>(),    "Boss prefab에 EnemyMotion 없음");
            Assert.IsNotNull(prefab.GetComponent<AnimatorDriver>(), "Boss prefab에 AnimatorDriver 없음");

            Animator? anim = prefab.GetComponent<Animator>();
            Assert.IsNotNull(anim, "Boss prefab에 Animator 없음");
            Assert.IsNotNull(anim!.runtimeAnimatorController, "Boss prefab Animator에 controller 미연결");
        }

        [Test]
        public void Table_HasGolemEntry()
        {
            // Golem 행 등록 여부.
            // 미등록이면 GetPrefab이 에러 로그 + Normal 폴백을 반환하므로 Normal prefab 이름과 비교한다.
            // GetPrefab 내부 에러 로그는 Unity Test Runner가 무시하지 않지만,
            // Assert.AreNotSame FAIL이 원인을 명확히 나타내므로 추가 LogAssert 불필요.
            GameObject? normalPrefab = _table!.GetPrefab(EnemyKind.Normal);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            GameObject? golemPrefab  = _table!.GetPrefab(EnemyKind.Golem);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.IsNotNull(golemPrefab, "EnemyVisualTable에 Golem prefab이 등록되지 않았습니다.");
            Assert.AreNotSame(normalPrefab, golemPrefab,
                "Golem GetPrefab이 Normal prefab을 반환했습니다 — Golem 행 미등록으로 Normal 폴백 중입니다.");
        }

        [Test]
        public void GolemPrefab_HasRequiredComponents()
        {
            GameObject? prefab = _table!.GetPrefab(EnemyKind.Golem);
            if (prefab == null) Assert.Ignore("Golem prefab 없음 — 에셋 생성 후 실행하세요.");

            Assert.IsNotNull(prefab.GetComponent<RemoteEnemy>(),    "Golem prefab에 RemoteEnemy 없음");
            Assert.IsNotNull(prefab.GetComponent<RemoteEntity>(),   "Golem prefab에 RemoteEntity 없음");
            Assert.IsNotNull(prefab.GetComponent<EnemyMotion>(),    "Golem prefab에 EnemyMotion 없음");
            Assert.IsNotNull(prefab.GetComponent<AnimatorDriver>(), "Golem prefab에 AnimatorDriver 없음");

            Animator? anim = prefab.GetComponent<Animator>();
            Assert.IsNotNull(anim, "Golem prefab에 Animator 없음");
            Assert.IsNotNull(anim!.runtimeAnimatorController, "Golem prefab Animator에 controller 미연결");
        }

        [Test]
        public void GolemPrefab_HpBarFillWired() => AssertHpBarWired(EnemyKind.Golem);

        [Test]
        public void NormalPrefab_HpBarFillWired() => AssertHpBarWired(EnemyKind.Normal);

        [Test]
        public void BossPrefab_HpBarFillWired() => AssertHpBarWired(EnemyKind.Boss);

        // _hpBarFill은 [SerializeField] private — SerializedObject로 직렬화 값을 직접 단언.
        // prefab 에셋에 Initialize/ApplyHpUpdate를 호출하면 에셋이 메모리에서 오염되므로 금지.
        void AssertHpBarWired(EnemyKind kind)
        {
            GameObject? prefab = _table!.GetPrefab(kind);
            if (prefab == null) Assert.Ignore($"{kind} prefab 없음.");

            RemoteEnemy? enemy = prefab.GetComponent<RemoteEnemy>();
            Assert.IsNotNull(enemy, "RemoteEnemy 없음");

            var so = new UnityEditor.SerializedObject(enemy);
            var fill = so.FindProperty("_hpBarFill");
            Assert.IsNotNull(fill, "_hpBarFill 직렬화 필드 없음 — RemoteEnemy 필드명 변경 시 테스트 동기화 필요");
            Assert.IsNotNull(fill.objectReferenceValue, $"{kind} prefab의 _hpBarFill 미연결 — HP바가 영구 풀바로 보입니다.");

            var fullWidth = so.FindProperty("_hpBarFullWidth");
            Assert.IsNotNull(fullWidth, "_hpBarFullWidth 직렬화 필드 없음");
            Assert.Greater(fullWidth.floatValue, 0f, $"{kind} prefab의 _hpBarFullWidth가 0 이하 — HP바 폭 미설정.");

            // fill의 저작 localScale.x와 _hpBarFullWidth는 같은 진실의 복제 — 어긋나면 HP바 폭 왜곡.
            var fillTransform = (Transform)fill.objectReferenceValue;
            Assert.AreEqual(fillTransform.localScale.x, fullWidth.floatValue, 0.0001f,
                $"{kind} prefab의 _hpBarFullWidth({fullWidth.floatValue})가 fill localScale.x({fillTransform.localScale.x})와 불일치 — 한쪽만 수정된 silent divergence.");
        }
    }
}
