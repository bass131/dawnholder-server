#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Dawnholder.Client.State
{
    // 타인 entity Spawn/Despawn/Snapshot dispatch 매니저.
    // 본인 entity는 등록 X — SnapshotHandler가 entityId 분기로 막음.
    //
    // **지연 spawn 패턴**:
    //   PlayerJoin 도착 *전* Snapshot이 먼저 도착해도 UpdateSnapshot이 *그 자리에서* Spawn 호출.
    //   이후 PlayerJoin 도착 시 이미 있으면 noop (idempotent — initial roster 재전송 안전).
    [DisallowMultipleComponent]
    public class RemoteEntityRegistry : MonoBehaviour
    {
        public static RemoteEntityRegistry? Instance { get; private set; }

        // Inspector로 RemotePlayer.prefab 드래그. null이면 spawn 시 에러 로그.
        [SerializeField] GameObject? _remotePlayerPrefab;

        readonly Dictionary<int, RemoteEntity> _entities = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[Registry] RemoteEntityRegistry 중복 박힘 — 씬에 여러 인스턴스 있음. 본인 셋업 확인.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // CombatBootstrap.BuildRemoteEntityRegistry가 코드 주도로 생성할 때
        // Inspector 드래그 대신 Resources.Load로 prefab을 주입하기 위한 공개 메서드.
        // Inspector 드래그 경로도 여전히 동작 — [SerializeField] 유지.
        public void SetRemotePlayerPrefab(GameObject prefab)
        {
            _remotePlayerPrefab = prefab;
        }

        void OnDestroy()
        {
            Clear();
            if (Instance == this) Instance = null;
        }

        // S_PlayerJoin 핸들러에서 호출. 이미 있으면 noop (idempotent — initial roster 재전송 안전).
        public void Spawn(int entityId, float spawnX, float spawnY)
        {
            if (_entities.ContainsKey(entityId))
            {
                return;
            }
            if (_remotePlayerPrefab == null)
            {
                Debug.LogError($"[Registry] _remotePlayerPrefab null — Inspector에 prefab 드래그 누락. entity {entityId} spawn 실패.");
                return;
            }

            GameObject go = Instantiate(_remotePlayerPrefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);
            go.name = $"RemotePlayer_{entityId}";
            RemoteEntity? entity = go.GetComponent<RemoteEntity>();
            if (entity == null)
            {
                Debug.LogError($"[Registry] RemotePlayer.prefab에 RemoteEntity 컴포넌트 없음. entity {entityId} spawn 실패.");
                Destroy(go);
                return;
            }
            entity.Initialize(entityId, spawnX, spawnY);
            _entities[entityId] = entity;
            Debug.Log($"[Registry] Spawned entity {entityId} at ({spawnX:F2}, {spawnY:F2})");
        }

        // S_PlayerLeave 핸들러에서 호출. 없으면 noop.
        public void Despawn(int entityId)
        {
            if (!_entities.TryGetValue(entityId, out RemoteEntity? entity))
            {
                return;
            }
            entity.ClearBuffer();
            _entities.Remove(entityId);
            Destroy(entity.gameObject);
            Debug.Log($"[Registry] Despawned entity {entityId}");
        }

        // S_Snapshot 핸들러에서 호출 (entityId가 본인이 아닌 경우만).
        // 지연 spawn — entity 없으면 Snapshot 좌표(이미 이동한 좌표)로 Spawn 후 buffer push.
        public void UpdateSnapshot(int entityId, float x, float y)
        {
            if (!_entities.TryGetValue(entityId, out RemoteEntity? entity))
            {
                Spawn(entityId, x, y);
                if (!_entities.TryGetValue(entityId, out entity))
                {
                    // Spawn 실패 (prefab null 등) — 위 Debug.LogError가 이미 박힘.
                    return;
                }
            }
            entity.EnqueueSnapshot(x, y);
        }

        // OnDisconnected에서 호출 — 메모리 누수 차단.
        public void Clear()
        {
            foreach (RemoteEntity entity in _entities.Values)
            {
                if (entity != null) Destroy(entity.gameObject);
            }
            _entities.Clear();
        }
    }
}
