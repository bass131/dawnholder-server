#nullable enable
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 적 종류(EnemyKind) → prefab 매핑 테이블 SO.
    // 새 적 추가 = 이 SO에 1행 추가 + prefab 신설. 코드 분기 없음.
    [CreateAssetMenu(menuName = "Dawnholder/EnemyVisualTable")]
    public class EnemyVisualTable : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public EnemyKind Kind;
            public GameObject? Prefab;
        }

        [SerializeField] Entry[] _entries = System.Array.Empty<Entry>();

        // kind에 등록된 prefab 반환.
        // 미등록 kind → 에러 로그 + Normal prefab으로 폴백.
        // Normal도 없으면 null 반환 (silent 빈 GameObject 금지 — fail-loud).
        public GameObject? GetPrefab(EnemyKind kind)
        {
            foreach (Entry e in _entries)
                if (e.Kind == kind) return e.Prefab;

            Debug.LogError(
                $"[EnemyVisualTable] kind={kind} 미등록. Normal prefab으로 폴백. " +
                "EnemyVisualTable SO의 Entries에 해당 Kind를 추가하세요.");

            // Normal 폴백
            foreach (Entry e in _entries)
                if (e.Kind == EnemyKind.Normal) return e.Prefab;

            Debug.LogError(
                "[EnemyVisualTable] Normal prefab도 없습니다. " +
                "Assets/Resources/EnemyVisualTable.asset 확인 후 Normal 행을 추가하세요.");
            return null;
        }

        // 테스트 전용 — 인메모리 테이블 구성 헬퍼.
        internal void SetEntriesForTest(Entry[] entries) => _entries = entries;
    }
}
