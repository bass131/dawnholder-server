#nullable enable
using Shared.GameData;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dawnholder.Client.Combat
{
    // 보스/적 attackPattern별 이펙트 재생 — 시각 전용 (헌법 #1, 판정 0).
    //
    // Resources 경로 약속:
    //   Boss  pattern 0 (P1) → "Effects/BossAttackPattern0"
    //   Boss  pattern 1 (P2) → "Effects/BossAttackPattern1"
    //   Normal (Slime)       → "Effects/SlimeDamageEffect"    (swap-ready: 아침에 prefab 배치)
    //   Golem                → "Effects/GolemDamageEffect"    (swap-ready: 아침에 prefab 배치)
    //
    // prefab 미존재 시 경고 1회 + silent skip (null-safe) — Slime/Golem prefab 미배치 중 정상 경로.
    public static class BossAttackEffectSpawner
    {
        static readonly string[] _bossEffectPaths = new[]
        {
            "Effects/BossAttackPattern0",
            "Effects/BossAttackPattern1",
        };

        // kind별 단일 Resources 경로 상수 — 아침 prefab 배치 위치와 1:1 대응.
        const string PathSlime = "Effects/SlimeDamageEffect";
        const string PathGolem = "Effects/GolemDamageEffect";

        static readonly bool[] _warnedMissing = new bool[_bossEffectPaths.Length];

        // 보스 전용 오버로드 — 기존 호출 호환 유지 (회귀 방지).
        // attackPattern: 0=P1 / 1=P2.
        public static void Spawn(byte attackPattern, Vector3 spawnPos, int facing = 1)
        {
            int idx = (int)attackPattern;
            if (idx < 0 || idx >= _bossEffectPaths.Length)
            {
                Debug.LogWarning($"[BossAttackEffectSpawner] 알 수 없는 attackPattern={attackPattern} — skip.");
                return;
            }

            GameObject? prefab = Resources.Load<GameObject>(_bossEffectPaths[idx]);
            if (prefab == null)
            {
                if (!_warnedMissing[idx])
                {
                    Debug.LogWarning(
                        $"[BossAttackEffectSpawner] 이펙트 prefab 미존재: Resources/{_bossEffectPaths[idx]}. " +
                        "Assets/Resources/Effects/ 폴더에 prefab을 추가하면 자동 적용됩니다.");
                    _warnedMissing[idx] = true;
                }
                return;
            }

            SpawnPrefab(prefab, spawnPos, facing);
        }

        // kind-aware 오버로드 — EnemyAttackHandler에서 kind 해석 후 호출.
        // Boss → 기존 BossAttackPattern 슬롯. Normal/Golem → kind별 Resources 경로.
        public static void Spawn(EnemyKind kind, byte attackPattern, Vector3 spawnPos, int facing = 1)
        {
            if (kind == EnemyKind.Boss)
            {
                Spawn(attackPattern, spawnPos, facing);
                return;
            }

            string path = kind switch
            {
                EnemyKind.Normal => PathSlime,
                EnemyKind.Golem  => PathGolem,
                _                => PathSlime,
            };

            GameObject? prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                // Slime/Golem prefab은 아직 미배치 — 아침에 prefab 배치 시 자동 적용.
                Debug.LogWarning(
                    $"[BossAttackEffectSpawner] {kind} 이펙트 prefab 미존재: Resources/{path}. " +
                    "Assets/Resources/Effects/ 에 prefab 배치 시 자동 적용됩니다.");
                return;
            }

            SpawnPrefab(prefab, spawnPos, facing);
        }

        static void SpawnPrefab(GameObject prefab, Vector3 spawnPos, int facing)
        {
            GameObject fx = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
            if (facing < 0)
            {
                Vector3 s = fx.transform.localScale;
                s.x = -Mathf.Abs(s.x);
                fx.transform.localScale = s;
            }
            // 수명 자동 파괴 — AutoDestroy가 없으면 EffectLifetime 주입.
            if (fx.GetComponent<EffectLifetime>() == null)
                fx.AddComponent<EffectLifetime>();
        }
    }
}
