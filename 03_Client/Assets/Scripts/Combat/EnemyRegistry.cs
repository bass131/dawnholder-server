#nullable enable
using System.Collections.Generic;
using Dawnholder.Client.Rendering;
using Dawnholder.Client.State;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // Enemy/Boss 전담 registry. Player(RemoteEntityRegistry)와 영역 분리.
    //
    // **분리 이유**:
    //   1. lookup 분기 — Player entityId와 enemy entityId가 서버 같은 풀이라
    //      RemoteEntityRegistry에 섞으면 type 분기 위해 매 frame switch 필요.
    //   2. 컴포넌트 타입 다름 — RemoteEntity (보간 buffer) vs RemoteEnemy (HP+kind).
    //
    // 본 클래스는 dict 관리(Spawn/ApplyHit/Despawn/TryGetNearest/Clear)만 담당,
    // GO 조립은 EnemyViewFactory.Spawn으로 위임.
    [DisallowMultipleComponent]
    public class EnemyRegistry : MonoBehaviour
    {
        public static EnemyRegistry? Instance { get; private set; }

        readonly struct EnemyEntry
        {
            public readonly RemoteEnemy Enemy;
            public readonly RemoteEntity Interp;
            public readonly EnemyMotion? Motion;
            public EnemyEntry(RemoteEnemy enemy, RemoteEntity interp, EnemyMotion? motion)
            {
                Enemy = enemy;
                Interp = interp;
                Motion = motion;
            }
        }

        readonly Dictionary<int, EnemyEntry> _enemies = new();

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

            EnemyKind kind = (EnemyKind)entityKind;
            GameObject? go = EnemyViewFactory.Spawn(entityId, kind, x, y);
            if (go == null)
            {
                Debug.LogError($"[EnemyRegistry] entity {entityId} spawn 실패 — EnemyViewFactory null 반환. prefab/테이블 설정 확인.");
                return;
            }

            RemoteEnemy? comp = go.GetComponent<RemoteEnemy>();
            RemoteEntity? interp = go.GetComponent<RemoteEntity>();
            EnemyMotion? motion = go.GetComponent<EnemyMotion>();

            if (comp == null || interp == null)
            {
                Debug.LogError($"[EnemyRegistry] prefab 필수 컴포넌트 누락 — entity {entityId} 보간 불가.");
                Destroy(go);
                return;
            }

            comp.Initialize(entityId, kind, currentHp, maxHp);
            _enemies[entityId] = new EnemyEntry(comp, interp, motion);
            Debug.Log($"[EnemyRegistry] Spawned {kind} entity {entityId} at ({x:F2}, {y:F2}) hp={currentHp}/{maxHp}");
        }

        // S_EntityState 핸들러에서 호출 — 서버 권위 위치 + 시각 animState 갱신.
        public void UpdatePosition(int entityId, float x, float y, byte animState)
        {
            if (!_enemies.TryGetValue(entityId, out EnemyEntry entry)) return;
            entry.Interp.EnqueueSnapshot(x, y + entry.Enemy.VisualFootOffset);
            entry.Motion?.SetAnimState(animState);
        }

        // S_HitResult 핸들러에서 호출.
        public void ApplyHit(int targetEntityId, int currentHp, int maxHp)
        {
            if (!_enemies.TryGetValue(targetEntityId, out EnemyEntry entry))
            {
                // Death packet이 먼저 도착했거나 spawn 전 race — silent drop.
                return;
            }
            entry.Enemy.ApplyHpUpdate(currentHp, maxHp);
        }

        // S_EntityDeath 핸들러에서 호출.
        public void Despawn(int entityId)
        {
            if (!_enemies.TryGetValue(entityId, out EnemyEntry entry)) return;
            _enemies.Remove(entityId);
            entry.Interp.ClearBuffer();
            Destroy(entry.Enemy.gameObject);
            Debug.Log($"[EnemyRegistry] Despawned entity {entityId}");
        }

        // 이펙트 flip용 — entityId의 시각 facing(1=우/-1=좌) 반환. Motion 없으면 1.
        public bool TryGetFacing(int entityId, out int facing)
        {
            if (_enemies.TryGetValue(entityId, out EnemyEntry entry) && entry.Motion != null)
            {
                facing = entry.Motion.Facing;
                return true;
            }
            facing = 1;
            return false;
        }

        // 투사체 시각 연출용 — entityId에 해당하는 Transform 반환. 없으면 false.
        // 헌법 #1: 판정/데미지와 무관. 순수 시각 경로 전용.
        public bool TryGetTransform(int entityId, out Transform? target)
        {
            if (_enemies.TryGetValue(entityId, out EnemyEntry entry) && entry.Enemy != null)
            {
                target = entry.Enemy.transform;
                return true;
            }
            target = null;
            return false;
        }

        // AttackIntent 공격 입력에서 호출.
        // origin 기준 maxRangeSq 안 가장 가까운 enemy/boss entityId 반환. 없으면 false.
        // 헌법 #1 — *클라는 target 추천*만, 서버가 최종 검증 (range/cooldown/dead 모두 서버 재검사).
        public bool TryGetNearest(Vector3 origin, float maxRangeSq, out int targetEntityId)
        {
            targetEntityId = 0;
            float bestSq = maxRangeSq;
            bool found = false;
            foreach (KeyValuePair<int, EnemyEntry> kv in _enemies)
            {
                RemoteEnemy enemy = kv.Value.Enemy;
                if (enemy == null) continue;
                Vector3 diff = enemy.transform.position - origin;
                float dSq = diff.x * diff.x + diff.y * diff.y;
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
            foreach (EnemyEntry entry in _enemies.Values)
            {
                entry.Interp.ClearBuffer();
                if (entry.Enemy != null) Destroy(entry.Enemy.gameObject);
            }
            _enemies.Clear();
        }
    }
}
