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
        readonly Dictionary<int, RemotePlayerMotion> _motions = new();

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

        // S_PlayerJoin 핸들러에서 호출 — 직업 정보 포함.
        //
        // **idempotent 보강 (핵심 함정 봉합)**:
        //   Snapshot이 PlayerJoin보다 먼저 도착하면 UpdateSnapshot의 지연 spawn이
        //   characterClass=Warrior(기본) 상태로 GO를 생성한다.
        //   이후 PlayerJoin 도착 시 이미 ContainsKey → **Animator controller만 갱신**하고 return.
        //   RemoteEntity/RemotePlayerMotion/AnimatorDriver는 절대 교체/제거 X.
        public void Spawn(int entityId, float spawnX, float spawnY,
                          CharacterClass characterClass = CharacterClass.Warrior)
        {
            if (_entities.ContainsKey(entityId))
            {
                // 이미 spawn됨 (Snapshot 선도착 지연 spawn 경로). controller만 갱신.
                ApplyAnimatorController(entityId, characterClass);
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

            // animState 공급원 + driver — prefab에 없으면 AddComponent로 런타임 주입.
            RemotePlayerMotion motion = go.GetComponent<RemotePlayerMotion>()
                                        ?? go.AddComponent<RemotePlayerMotion>();
            if (go.GetComponent<AnimatorDriver>() == null)
                go.AddComponent<AnimatorDriver>();
            _motions[entityId] = motion;

            // 직업 Animator controller 장착.
            ApplyAnimatorController(entityId, characterClass);

            Debug.Log($"[Registry] Spawned entity {entityId} class={characterClass} at ({spawnX:F2}, {spawnY:F2})");
        }

        // 직업 → Animator controller 교체. prefab 기본 controller 유지가 항상 fail-soft.
        // GO나 controller 찾기 실패 시 경고 1회 + skip (원격 표시 안전 폴백).
        void ApplyAnimatorController(int entityId, CharacterClass characterClass)
        {
            if (!_entities.TryGetValue(entityId, out RemoteEntity? entity) || entity == null) return;

            ClassConfig[] configs = Resources.LoadAll<ClassConfig>("ClassConfigs");
            ClassConfig? config = ClassLoadout.FindConfig(configs, characterClass);
            if (config == null || config.Controller == null)
            {
                Debug.LogWarning(
                    $"[Registry] entity {entityId} class={characterClass} — ClassConfig/Controller 미발견. prefab 기본 controller 유지.");
                return;
            }

            Animator? animator = entity.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"[Registry] entity {entityId} — Animator 없음. controller 장착 스킵.");
                return;
            }
            animator.runtimeAnimatorController = config.Controller;
            Debug.Log($"[Registry] entity {entityId} Animator controller 갱신 → {config.Controller.name} (class={characterClass})");
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
            _motions.Remove(entityId);
            Destroy(entity.gameObject);
            Debug.Log($"[Registry] Despawned entity {entityId}");
        }

        // S_Snapshot 핸들러에서 호출 (entityId가 본인이 아닌 경우만).
        // 지연 spawn — entity 없으면 Snapshot 좌표(이미 이동한 좌표)로 Spawn 후 buffer push.
        public void UpdateSnapshot(int entityId, float x, float y, byte animState)
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
        }
    }
}
