#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Combat
{
    // 투사체 시각 스폰 공유 헬퍼 — 로컬(MageRangedAttack) + 원격(PlayerAttackHandler) 공용.
    //
    // 헌법 #1: 판정/데미지는 서버 전용. 이 헬퍼는 순수 시각 연출만 담당.
    public static class ProjectileSpawner
    {
        // projectilePrefab: null이면 스폰 생략 (prefab 미연결 fail-soft).
        // spawnRoot: EffectAnchor 탐색 기준 Transform (공격자 root). null이면 spawnPos 직접 사용.
        // target: 호밍 대상. null이면 facing 방향 직진.
        // facing: target 없을 때 방향 결정 (1=오른쪽, -1=왼쪽).
        public static void Spawn(
            GameObject? projectilePrefab,
            Transform? spawnRoot,
            Transform? target,
            int facing)
        {
            if (projectilePrefab == null) return;

            Vector3 spawnPos = spawnRoot != null
                ? EffectAnchor.ResolvePosition(spawnRoot)
                : (target?.position ?? Vector3.zero);

            GameObject proj = Object.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            ProjectileVisual visual = proj.GetComponent<ProjectileVisual>()
                                     ?? proj.AddComponent<ProjectileVisual>();

            if (target != null)
            {
                visual.Launch(target);
            }
            else
            {
                // 타겟 없음(허공 스윙 또는 원격 연출에서 타겟 조회 실패) — facing 방향 직진 더미 스폰.
                visual.LaunchDirection(new Vector3(facing >= 0 ? 1f : -1f, 0f, 0f));
            }
        }
    }
}
