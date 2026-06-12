#nullable enable
using Dawnholder.Client.Rendering;
using Dawnholder.Client.State;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // EnemyVisualTable SO lookup → prefab Instantiate → 초기화.
    // new GameObject() 런타임 조립 없음. EnemyKind 분기 없음.
    public static class EnemyViewFactory
    {
        // 세션 중 1회만 로드. 씬 전환 시 Resources 참조는 그대로 유효.
        static EnemyVisualTable? _table;

        static EnemyVisualTable? GetTable()
        {
            if (_table != null) return _table;
            _table = Resources.Load<EnemyVisualTable>("EnemyVisualTable");
            if (_table == null)
                Debug.LogError(
                    "[EnemyViewFactory] EnemyVisualTable 로드 실패. " +
                    "Assets/Resources/EnemyVisualTable.asset 이 있는지 확인하세요 " +
                    "(Create > Dawnholder/EnemyVisualTable).");
            return _table;
        }

        // prefab lookup → Instantiate → RemoteEntity.Initialize(entityId, x, y+footOffset).
        // 실패(테이블/prefab 없음) 시 null 반환 + 에러 로그. EnemyRegistry가 null 체크 후 drop.
        public static GameObject? Spawn(int entityId, EnemyKind kind, float x, float y)
        {
            EnemyVisualTable? table = GetTable();
            if (table == null) return null;

            GameObject? prefab = table.GetPrefab(kind);
            if (prefab == null) return null;

            GameObject go = Object.Instantiate(prefab);
            go.name = $"{kind}_{entityId}";

            RemoteEntity? remoteEntity = go.GetComponent<RemoteEntity>();
            if (remoteEntity == null)
            {
                Debug.LogError(
                    $"[EnemyViewFactory] prefab '{prefab.name}'에 RemoteEntity 없음 — " +
                    "prefab 구성을 확인하세요 (RemoteEntity 컴포넌트 필수).");
                Object.Destroy(go);
                return null;
            }

            RemoteEnemy? enemy = go.GetComponent<RemoteEnemy>();
            if (enemy == null)
            {
                Debug.LogError(
                    $"[EnemyViewFactory] prefab '{prefab.name}'에 RemoteEnemy 없음 — " +
                    "prefab 구성을 확인하세요 (RemoteEnemy 컴포넌트 필수).");
                Object.Destroy(go);
                return null;
            }

            // visualFootOffset은 prefab의 RemoteEnemy._visualFootOffset 직렬화 값을 사용.
            float posY = y + enemy.VisualFootOffset;
            remoteEntity.Initialize(entityId, x, posY);
            go.transform.position = new Vector3(x, posY, 0f);

            return go;
        }

        // 테스트/씬 전환 시 캐시 초기화. Resources.Load는 에디터에서 반복 호출 시 캐시되지만
        // 명시 초기화로 테스트 격리를 보장한다.
        internal static void ClearCacheForTest() => _table = null;
    }
}
