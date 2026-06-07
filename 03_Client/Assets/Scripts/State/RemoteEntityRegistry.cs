#nullable enable
using System.Collections.Generic;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Rendering;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.State
{
    // 타인 entity Spawn/Despawn/Snapshot dispatch 매니저.
    // 본인 entity는 등록 X — SnapshotHandler가 entityId 분기로 막음.
    //
    // **Prefab Variant 구조 (M4.5 Phase 05)**:
    //   직업 정보(CharacterClass?)를 _spawnedClasses에 보관.
    //   null = 직업 미상 (Snapshot 선도착 → 지연 spawn 경로).
    //   Spawn 재호출 시 기록 직업 vs 들어온 직업을 비교해 재생성 여부 결정.
    //
    // **지연 spawn 패턴**:
    //   PlayerJoin 도착 전 Snapshot이 먼저 도착해도 UpdateSnapshot이 그 자리에서 Spawn 호출.
    //   이후 PlayerJoin 도착 시 NeedsRespawn 판정 → 직업 다름/미상이면 재생성, 같으면 noop.
    [DisallowMultipleComponent]
    public class RemoteEntityRegistry : MonoBehaviour
    {
        public static RemoteEntityRegistry? Instance { get; private set; }

        // Inspector로 RemotePlayer.prefab 드래그 (base prefab 폴백).
        [SerializeField] GameObject? _remotePlayerPrefab;

        readonly Dictionary<int, RemoteEntity> _entities = new();
        readonly Dictionary<int, RemotePlayerMotion> _motions = new();

        // null = 직업 미상 (Snapshot 선도착 지연 spawn). Warrior 기본값과 구분 필수.
        readonly Dictionary<int, CharacterClass?> _spawnedClasses = new();

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

        // CombatBootstrap.BuildRemoteEntityRegistry가 코드 주도로 생성할 때 주입.
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

        // S_PlayerJoin 핸들러에서 호출 — 직업 정보 항상 있음 (non-null).
        //
        // **재생성 판단 (NeedsRespawn)**:
        //   기록 없음 → 신규 spawn.
        //   기록 있음 + NeedsRespawn → 현 위치 캡처 → 파괴 → 올바른 variant 재생성.
        //   기록 있음 + !NeedsRespawn → noop (idempotent).
        public void Spawn(int entityId, float spawnX, float spawnY, CharacterClass? characterClass)
        {
            if (_entities.TryGetValue(entityId, out RemoteEntity? existing))
            {
                CharacterClass? recorded = _spawnedClasses.TryGetValue(entityId, out CharacterClass? r) ? r : null;
                if (!NeedsRespawn(recorded, characterClass))
                    return; // 동일 직업 이미 spawn 중 — noop.

                // 직업 다름 또는 미상 → 현 위치 캡처 후 재생성.
                Vector3 currentPos = existing.transform.position;
                existing.ClearBuffer();
                _entities.Remove(entityId);
                _motions.Remove(entityId);
                _spawnedClasses.Remove(entityId);
                Destroy(existing.gameObject);

                SpawnInternal(entityId, currentPos.x, currentPos.y, characterClass);
                return;
            }

            SpawnInternal(entityId, spawnX, spawnY, characterClass);
        }

        // S_PlayerLeave 핸들러에서 호출. 없으면 noop.
        public void Despawn(int entityId)
        {
            if (!_entities.TryGetValue(entityId, out RemoteEntity? entity))
                return;

            entity.ClearBuffer();
            _entities.Remove(entityId);
            _motions.Remove(entityId);
            _spawnedClasses.Remove(entityId);
            Destroy(entity.gameObject);
            Debug.Log($"[Registry] Despawned entity {entityId}");
        }

        // S_Snapshot 핸들러에서 호출 (entityId가 본인이 아닌 경우만).
        // 지연 spawn — entity 없으면 Snapshot 좌표로 Spawn(직업 미상=null).
        public void UpdateSnapshot(int entityId, float x, float y, byte animState)
        {
            if (!_entities.TryGetValue(entityId, out RemoteEntity? entity))
            {
                Spawn(entityId, x, y, null); // 직업 미상
                if (!_entities.TryGetValue(entityId, out entity))
                    return; // Spawn 실패 (prefab null 등) — 위 LogError가 이미 박힘.
            }
            entity.EnqueueSnapshot(x, y);
            if (_motions.TryGetValue(entityId, out RemotePlayerMotion? motion))
                motion.SetAnimState(animState);
        }

        // OnDisconnected에서 호출 — 메모리 누수 차단.
        public void Clear()
        {
            foreach (RemoteEntity entity in _entities.Values)
            {
                if (entity != null) Destroy(entity.gameObject);
            }
            _entities.Clear();
            _motions.Clear();
            _spawnedClasses.Clear();
        }

        // 재생성 판단 순수 함수 — EditMode 테스트 대상.
        // incoming=null(직업 정보 없음) → false — 정보 부재는 재생성 사유가 아님
        //   (기존 variant를 base prefab으로 강등시키는 지뢰 차단).
        // recorded=null(미상) + incoming 있음 → true(재생성).
        // 둘 다 있음 → 다르면 true, 같으면 false(noop).
        public static bool NeedsRespawn(CharacterClass? recorded, CharacterClass? incoming)
        {
            if (incoming == null) return false;
            if (recorded == null) return true;
            return recorded.Value != incoming.Value;
        }

        // 신규/재생성 공통 조립 헬퍼.
        void SpawnInternal(int entityId, float x, float y, CharacterClass? characterClass)
        {
            GameObject? prefabToSpawn = ResolvePrefab(characterClass, entityId);
            if (prefabToSpawn == null)
            {
                Debug.LogError($"[Registry] prefab null — entity {entityId} spawn 실패. Inspector에 RemotePlayer.prefab 드래그 연결 확인.");
                return;
            }

            GameObject go = Instantiate(prefabToSpawn, new Vector3(x, y, 0f), Quaternion.identity);
            go.name = $"RemotePlayer_{entityId}";
            RemoteEntity? entity = go.GetComponent<RemoteEntity>();
            if (entity == null)
            {
                Debug.LogError($"[Registry] RemoteEntity 컴포넌트 없음 — entity {entityId} spawn 실패.");
                Destroy(go);
                return;
            }
            entity.Initialize(entityId, x, y);
            _entities[entityId] = entity;
            _spawnedClasses[entityId] = characterClass;

            RemotePlayerMotion motion = go.GetComponent<RemotePlayerMotion>()
                                        ?? go.AddComponent<RemotePlayerMotion>();
            if (go.GetComponent<AnimatorDriver>() == null)
                go.AddComponent<AnimatorDriver>();
            _motions[entityId] = motion;

            Debug.Log($"[Registry] Spawned entity {entityId} class={characterClass?.ToString() ?? "unknown"} at ({x:F2}, {y:F2})");
        }

        // 직업 → variant prefab 해석. 미연결/미상이면 base prefab 폴백 + 경고 1회.
        GameObject? ResolvePrefab(CharacterClass? characterClass, int entityId)
        {
            if (characterClass.HasValue)
            {
                ClassConfig[] configs = Resources.LoadAll<ClassConfig>("ClassConfigs");
                ClassConfig? config = ClassLoadout.FindConfig(configs, characterClass.Value);
                if (config?.RemotePlayerPrefab != null)
                    return config.RemotePlayerPrefab;

                if (config != null)
                    Debug.LogWarning($"[Registry] entity {entityId} class={characterClass} — ClassConfig.RemotePlayerPrefab 미연결. base prefab 폴백. Inspector에서 variant prefab을 드래그 연결하세요.");
                else
                    Debug.LogWarning($"[Registry] entity {entityId} class={characterClass} — ClassConfig 미발견. base prefab 폴백.");
            }

            return _remotePlayerPrefab;
        }
    }
}
