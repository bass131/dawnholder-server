#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 보스 attackPattern별 이펙트 재생 — 시각 전용 (헌법 #1, 판정 0).
    // Resources.Load 경로 약속:
    //   pattern 0 (P1) → "Effects/BossAttackPattern0"
    //   pattern 1 (P2) → "Effects/BossAttackPattern1"
    // prefab 미존재 시 경고 1회 + silent skip (null-safe).
    public static class BossAttackEffectSpawner
    {
        static readonly string[] _effectPaths = new[]
        {
            "Effects/BossAttackPattern0",
            "Effects/BossAttackPattern1",
        };

        static readonly bool[] _warnedMissing = new bool[_effectPaths.Length];

        // attackPattern: 0=P1 / 1=P2. spawnPos: 이펙트를 Instantiate할 월드 좌표 (EffectAnchor 권장).
        // facing: 1=우향 / -1=좌향 — localScale.x 부호로 flip (부모 없는 독립 Transform).
        public static void Spawn(byte attackPattern, Vector3 spawnPos, int facing = 1)
        {
            int idx = (int)attackPattern;
            if (idx < 0 || idx >= _effectPaths.Length)
            {
                Debug.LogWarning($"[BossAttackEffectSpawner] 알 수 없는 attackPattern={attackPattern} — skip.");
                return;
            }

            GameObject? prefab = Resources.Load<GameObject>(_effectPaths[idx]);
            if (prefab == null)
            {
                if (!_warnedMissing[idx])
                {
                    Debug.LogWarning(
                        $"[BossAttackEffectSpawner] 이펙트 prefab 미존재: Resources/{_effectPaths[idx]}. " +
                        "Assets/Resources/Effects/ 폴더에 prefab을 추가하면 자동 적용됩니다.");
                    _warnedMissing[idx] = true;
                }
                return;
            }

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
