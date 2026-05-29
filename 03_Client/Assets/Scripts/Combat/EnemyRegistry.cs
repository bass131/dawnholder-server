#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // M3 Phase 08c: Enemy/Boss 전담 registry. Player(RemoteEntityRegistry)와 영역 분리.
    //
    // **분리 이유** (응급 결정 — 5/19):
    //   1. lookup 분리 — Player entityId와 enemy entityId가 서버 같은 풀이라
    //      RemoteEntityRegistry에 섞으면 type 분기 위해 매 frame switch 필요.
    //   2. 컴포넌트 타입 다름 — RemoteEntity (보간 buffer) vs RemoteEnemy (HP+kind).
    //   3. 정유현 영역 격리 — RemoteEntityRegistry는 정유현이 Prefab variant 박는 영역.
    //
    // **prefab 파일 안 쓰는 이유** (응급 placeholder 약속):
    //   디자인 0 + 정유현 prefab 영역 보존 + 씬 YAML 편집 회피 위해 *런타임 코드 생성*.
    //   spawn 시점에 EnemyViewFactory.BuildPlaceholder로 GameObject 조립.
    //   미래 정유현 prefab 박으면 _normalPrefab/_bossPrefab SerializeField로 교체.
    //
    // **M4.3R Phase 05 (rank 3) 리팩토링**:
    //   BuildPlaceholder + sprite 로딩 로직(GetWhiteSquare/TryLoadEnemySprites/LoadFirstSpriteAt
    //   + static 캐시 4개)를 EnemyViewFactory 정적 클래스로 추출.
    //   본 클래스는 dict 관리(Spawn/ApplyHit/Despawn/TryGetNearest/Clear) + factory 호출만 담당.
    //
    // **시그니처 약속** (UnityClientSession 호출):
    //   - Spawn(int entityId, byte entityKind, float x, float y, int currentHp, int maxHp)
    //   - ApplyHit(int targetEntityId, int currentHp, int maxHp)
    //   - Despawn(int entityId)
    //   - TryGetNearest(Vector3 origin, float maxRangeSq, out int targetEntityId)
    //   - Clear()
    [DisallowMultipleComponent]
    public class EnemyRegistry : MonoBehaviour
    {
        public static EnemyRegistry? Instance { get; private set; }

        // 미래 prefab 교체 약속 (정유현 영역 — M4.3R Phase 05 주석 보존):
        // 정유현이 prefab 박으면 아래 SerializeField를 활성화하고
        // Spawn에서 EnemyViewFactory.BuildPlaceholder 대신 Instantiate 분기로 전환.
        // [SerializeField] GameObject _normalPrefab;
        // [SerializeField] GameObject _bossPrefab;

        readonly Dictionary<int, RemoteEnemy> _enemies = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[EnemyRegistry] 중복 박힘 — 씬에 여러 인스턴스. 본인 셋업 확인.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            Clear();
            if (Instance == this) Instance = null;
        }

        // S_EntitySpawn 핸들러에서 호출.
        public void Spawn(int entityId, byte entityKind, float x, float y, int currentHp, int maxHp)
        {
            if (_enemies.ContainsKey(entityId))
            {
                Debug.LogWarning($"[EnemyRegistry] entity {entityId} 이미 spawn — 중복 패킷 drop.");
                return;
            }

            RemoteEnemy.EnemyKind kind = (RemoteEnemy.EnemyKind)entityKind;
            GameObject go = EnemyViewFactory.BuildPlaceholder(entityId, kind, x, y,
                                                               out RemoteEnemy comp,
                                                               out Transform hpFill,
                                                               out float fullWidth);
            comp.Initialize(entityId, kind, currentHp, maxHp);
            comp.SetHpBar(hpFill, fullWidth);
            _enemies[entityId] = comp;
            Debug.Log($"[EnemyRegistry] Spawned {kind} entity {entityId} at ({x:F2}, {y:F2}) hp={currentHp}/{maxHp}");
        }

        // S_EntityState 핸들러에서 호출 — 서버 권위 위치 갱신.
        // 최소 봉합 (M4.3R Phase β): 직접 transform 세팅.
        // 보간 버퍼(RemoteEntity 패턴)는 Phase 08 몫 — §0.3 과분할 금지.
        public void UpdatePosition(int entityId, float x, float y)
        {
            if (!_enemies.TryGetValue(entityId, out RemoteEnemy? enemy)) return;
            if (enemy == null) return;
            enemy.transform.position = new Vector3(x, y, 0f);
        }

        // S_HitResult 핸들러에서 호출.
        public void ApplyHit(int targetEntityId, int currentHp, int maxHp)
        {
            if (!_enemies.TryGetValue(targetEntityId, out RemoteEnemy? enemy))
            {
                // Death packet이 먼저 도착했거나 spawn 전 race — 응급 단순: silent drop.
                return;
            }
            enemy.ApplyHpUpdate(currentHp, maxHp);
        }

        // S_EntityDeath 핸들러에서 호출.
        public void Despawn(int entityId)
        {
            if (!_enemies.TryGetValue(entityId, out RemoteEnemy? enemy)) return;
            _enemies.Remove(entityId);
            if (enemy != null) Destroy(enemy.gameObject);
            Debug.Log($"[EnemyRegistry] Despawned entity {entityId}");
        }

        // LocalPlayerController 공격 입력에서 호출.
        // origin 기준 maxRangeSq 안 가장 가까운 enemy/boss entityId 반환. 없으면 false.
        // 헌법 #1 — *클라는 target 추천*만, 서버가 최종 검증 (range/cooldown/dead 모두 서버 재검사).
        public bool TryGetNearest(Vector3 origin, float maxRangeSq, out int targetEntityId)
        {
            targetEntityId = 0;
            float bestSq = maxRangeSq;
            bool found = false;
            foreach (KeyValuePair<int, RemoteEnemy> kv in _enemies)
            {
                RemoteEnemy enemy = kv.Value;
                if (enemy == null) continue;
                Vector3 diff = enemy.transform.position - origin;
                float dSq = diff.x * diff.x + diff.y * diff.y; // 2D — z 무시
                if (dSq <= bestSq)
                {
                    bestSq = dSq;
                    targetEntityId = kv.Key;
                    found = true;
                }
            }
            return found;
        }

        public void Clear()
        {
            foreach (RemoteEnemy enemy in _enemies.Values)
            {
                if (enemy != null) Destroy(enemy.gameObject);
            }
            _enemies.Clear();
        }
    }
}
