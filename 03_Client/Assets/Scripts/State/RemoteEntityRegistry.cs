#nullable enable
using System.Collections.Generic;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.Rendering;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.State
{
    // 타인 entity Spawn/Despawn/Snapshot dispatch 매니저.
    // 본인 entity는 등록 X — SnapshotHandler가 entityId 분기로 막음.
    //
    // **로직/비주얼 분리 (M4.5 Phase 05 v2)**:
    //   root = base prefab(로직: RemoteEntity/보간) 고정, 직업 시각은 "Visual" 자식
    //   (ClassConfig.VisualPrefab — 직업당 1개)을 ClassVisualMount로 장착.
    //   직업 기록(_spawnedClasses, null=미상)을 보관 — 늦은 직업 정보는 *비주얼 자식만 교체*
    //   (root 보존 → RemoteEntity/보간 버퍼/entityId 매핑 자연 유지, 유현 M3 컴포넌트 보존 약속 정합).
    //
    // **지연 spawn 패턴**:
    //   PlayerJoin 도착 전 Snapshot이 먼저 도착해도 UpdateSnapshot이 그 자리에서 Spawn 호출
    //   (직업 미상 → Knight 비주얼 기본). 이후 PlayerJoin 도착 시 NeedsVisualSwap 판정 →
    //   직업 다름/미상이면 비주얼 교체, 같으면 noop.
    [DisallowMultipleComponent]
    public class RemoteEntityRegistry : MonoBehaviour
    {
        public static RemoteEntityRegistry? Instance { get; private set; }

        // Inspector로 RemotePlayer.prefab 드래그 (base prefab 폴백).
        [SerializeField] GameObject? _remotePlayerPrefab;

        readonly Dictionary<int, RemoteEntity> _entities = new();
        readonly Dictionary<int, RemotePlayerMotion> _motions = new();

        // null = 직업 미상 (Snapshot 선도착 지연 spawn). Knight 기본값과 구분 필수.
        readonly Dictionary<int, CharacterClass?> _spawnedClasses = new();

        // 로컬 entity 누수 차단 경고 1회만 (드레인 중 매 패킷 스팸 방지).
        bool _warnedLocalLeak;

        // 로컬 플레이어 entityId 판정. UnityClientSession.LocalEntityId(최초 EnterMap에서 set,
        // 이후 불변)와 비교 — 이게 원격 등록 경로의 단일 self 가드.
        static bool IsLocalEntity(int entityId)
        {
            UnityClientSession session = UnityClientSession.Instance;
            return session != null && session.LocalEntityId.HasValue && session.LocalEntityId.Value == entityId;
        }

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
        // **비주얼 교체 판단 (NeedsVisualSwap)**:
        //   기록 없음 → 신규 spawn.
        //   기록 있음 + NeedsVisualSwap → 비주얼 자식만 교체 (root/보간 버퍼 보존).
        //   기록 있음 + !NeedsVisualSwap → noop (idempotent).
        public void Spawn(int entityId, float spawnX, float spawnY, CharacterClass? characterClass)
        {
            // 로컬 플레이어는 절대 원격 엔티티가 아니다 (불변식). 맵 전환 중 RosterBuffer에
            // 캐싱됐던 self 패킷이 drain되며 누수돼도 이 길목에서 차단 — self가 직업 미상(Knight)
            // 유령으로 spawn돼 진짜 캐릭터 위에 겹쳐 "법사가 전사로 보이는" 버그 방지.
            if (IsLocalEntity(entityId))
            {
                if (!_warnedLocalLeak)
                {
                    _warnedLocalLeak = true;
                    Debug.LogWarning($"[Registry] 로컬 entity {entityId}를 원격으로 spawn 시도 — 차단 (맵 전환 self 누수 가드).");
                }
                return;
            }

            if (_entities.TryGetValue(entityId, out RemoteEntity? existing))
            {
                CharacterClass? recorded = _spawnedClasses.TryGetValue(entityId, out CharacterClass? r) ? r : null;
                if (!NeedsVisualSwap(recorded, characterClass))
                    return; // 동일 직업 이미 spawn 중 — noop.

                // 늦은 직업 정보 — root GameObject는 보존, 비주얼 자식만 교체 (v2).
                // NeedsVisualSwap=true는 incoming non-null 보장 (null이면 false 반환).
                ClassVisualMount.Attach(existing.transform, ResolveVisual(characterClass!.Value, entityId));
                _spawnedClasses[entityId] = characterClass;
                Debug.Log($"[Registry] entity {entityId} 비주얼 교체 → class={characterClass}");
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
        public void UpdateSnapshot(int entityId, int serverTick, float x, float y, float vx, byte animState)
        {
            // self 가드 (Spawn과 동일 불변식) — 전환 중 누수된 본인 Snapshot이 지연 spawn으로
            // Knight 유령을 만드는 경로 차단.
            if (IsLocalEntity(entityId)) return;

            if (!_entities.TryGetValue(entityId, out RemoteEntity? entity))
            {
                Spawn(entityId, x, y, null); // 직업 미상
                if (!_entities.TryGetValue(entityId, out entity))
                    return; // Spawn 실패 (prefab null 등) — 위 LogError가 이미 박힘.
            }
            entity.EnqueueSnapshot(serverTick, x, y);
            if (_motions.TryGetValue(entityId, out RemotePlayerMotion? motion))
            {
                motion.SetAnimState(animState);
                motion.SetVelocityX(vx);
            }
        }

        // S_PlayerAttack 핸들러가 원격 공격자 위치 조회 시 사용.
        // entityId 없거나 GameObject가 이미 파괴됐으면 false.
        public bool TryGetTransform(int entityId, out Transform? t)
        {
            if (_entities.TryGetValue(entityId, out RemoteEntity? entity) && entity != null)
            {
                t = entity.transform;
                return true;
            }
            t = null;
            return false;
        }

        // 원격 공격자의 facing(-1/1) 조회. motion 없으면 1 폴백.
        public bool TryGetFacing(int entityId, out int facing)
        {
            if (_motions.TryGetValue(entityId, out RemotePlayerMotion? motion) && motion != null)
            {
                facing = motion.Facing;
                return true;
            }
            facing = 1;
            return false;
        }

        // S_PlayerAttack / S_SkillCast 수신 시 원격 공격자 몸통 facing을 latch.
        // _motions에는 원격 entity만 등록되므로 self 가드 불필요.
        public void SetAttackFacing(int entityId, int facing)
        {
            if (_motions.TryGetValue(entityId, out RemotePlayerMotion? motion) && motion != null)
                motion.SetAttackFacing(facing);
        }

        // S_SkillCast(Thunderbolt) 수신 시 원격 캐스팅 모션 latch — 서버가 Channeling animState를 안 보내는
        //   분(AttackState 미진입)을 클라가 연출(로컬 NotifyChannel 선예측의 원격 거울).
        public void SetChanneling(int entityId, float seconds)
        {
            if (_motions.TryGetValue(entityId, out RemotePlayerMotion? motion) && motion != null)
                motion.SetChanneling(seconds);
        }

        // Teleport 보간 끊기 — S_SkillCast(Teleport) 수신 시 해당 원격 entity의 보간 버퍼 reset.
        // entity가 아직 등록 안 됐으면(race) noop — 다음 Snapshot에서 지연 spawn하면 buffer가 비어 있으므로 슬라이드 없음.
        public void SnapEntity(int entityId)
        {
            if (_entities.TryGetValue(entityId, out RemoteEntity? entity) && entity != null)
                entity.SnapInterpolation();
        }

        // 텔레포트 도착 이펙트 콜백 등록 — SnapEntity 호출 *전에* 반드시 먼저 등록.
        // entity 미등록(race) 시 noop — 지연 spawn된 entity는 buffer가 비어 있으므로 이펙트 없이 정상 동작.
        public void SetTeleportArriveCallback(int entityId, System.Action? callback)
        {
            if (_entities.TryGetValue(entityId, out RemoteEntity? entity) && entity != null)
                entity.SetTeleportArriveCallback(callback);
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

        // 비주얼 교체 판단 순수 함수 — EditMode 테스트 대상.
        // incoming=null(직업 정보 없음) → false — 정보 부재는 교체 사유가 아님
        //   (기존 직업 비주얼을 기본으로 강등시키는 지뢰 차단).
        // recorded=null(미상) + incoming 있음 → true(교체).
        // 둘 다 있음 → 다르면 true, 같으면 false(noop).
        public static bool NeedsVisualSwap(CharacterClass? recorded, CharacterClass? incoming)
        {
            if (incoming == null) return false;
            if (recorded == null) return true;
            return recorded.Value != incoming.Value;
        }

        // 신규 조립 — base(로직 껍데기) Instantiate 후 직업 비주얼 자식 장착 (v2).
        void SpawnInternal(int entityId, float x, float y, CharacterClass? characterClass)
        {
            if (_remotePlayerPrefab == null)
            {
                Debug.LogError($"[Registry] _remotePlayerPrefab null — entity {entityId} spawn 실패. Inspector에 RemotePlayer.prefab 드래그 연결 확인.");
                return;
            }

            GameObject go = Instantiate(_remotePlayerPrefab, new Vector3(x, y, 0f), Quaternion.identity);
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

            // 비주얼 장착은 driver 준비 *후* — ClassVisualMount가 Rebind까지 수행 (순서 불변식).
            // 직업 미상(null)은 Knight 비주얼 기본 — 투명 잔상 방지. PlayerJoin 도착 시 교체됨.
            ClassVisualMount.Attach(go.transform,
                ResolveVisual(characterClass ?? CharacterClass.Knight, entityId));

            Debug.Log($"[Registry] Spawned entity {entityId} class={characterClass?.ToString() ?? "unknown"} at ({x:F2}, {y:F2})");
        }

        // 직업 → 비주얼 prefab 해석. 미연결/미발견이면 null + 경고 (Attach가 no-op 처리 — fail-soft).
        GameObject? ResolveVisual(CharacterClass characterClass, int entityId)
        {
            ClassConfig[] configs = Resources.LoadAll<ClassConfig>("ClassConfigs");
            ClassConfig? config = ClassLoadout.FindConfig(configs, characterClass);
            if (config?.VisualPrefab != null)
                return config.VisualPrefab;

            if (config != null)
                Debug.LogWarning($"[Registry] entity {entityId} class={characterClass} — ClassConfig.VisualPrefab 미연결. 비주얼 미장착.");
            else
                Debug.LogWarning($"[Registry] entity {entityId} class={characterClass} — ClassConfig 미발견. 비주얼 미장착.");
            return null;
        }
    }
}
