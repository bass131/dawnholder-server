#nullable enable
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dawnholder.Client.Combat
{
    // 이펙트 스폰 공통 로직 — 4개 사이트 중복 제거 (BossAttackEffectSpawner / SkillCastHandler × 3).
    // 동작 동치 보장: Instantiate 인자·flip 결과·localPosition·EffectLifetime 주입·warn 조건 변경 없음.
    // 시각 연출 전용 — 판정·데미지·상태 변경 0 (헌법 §1).
    public static class EffectSpawnService
    {
        // 핵심 후처리: Instantiate 이후 localOffset·flip·EffectLifetime 공통 적용.
        //   localOffset != null이면 localPosition + localRotation=identity 세팅.
        //   facingSign=0이면 flip 생략. facingSign!=0이면 (facingSign<0)^spriteDefaultFacesLeft.
        static void Configure(GameObject fx, int facingSign, bool spriteDefaultFacesLeft, Vector3? localOffset)
        {
            if (localOffset.HasValue)
            {
                fx.transform.localPosition = localOffset.Value;
                fx.transform.localRotation = Quaternion.identity;
            }
            if (facingSign != 0 && ((facingSign < 0) ^ spriteDefaultFacesLeft))
            {
                Vector3 s = fx.transform.localScale;
                s.x = -Mathf.Abs(s.x);
                fx.transform.localScale = s;
            }
            if (fx.GetComponent<EffectLifetime>() == null)
                fx.AddComponent<EffectLifetime>();
        }

        // 이미 로드된 prefab 스폰 — BossAttackEffectSpawner용.
        // parent != null이면 worldPos를 world 위치로 사용하며 parent 자식으로 묶음.
        // localOffset != null이면 Configure에서 localPosition 세팅.
        // facingSign=0이면 flip 없음. spriteDefaultFacesLeft=false=우향 기본.
        // facingSign 기본 0 = flip 없음 (SpawnFromPath와 동일 기본 — 동의미 파라미터 기본값 일치).
        // 모든 호출부는 facingSign을 명시 전달하므로 기본값은 footgun 방지 목적.
        public static void SpawnPrefab(GameObject prefab, Vector3 worldPos, Transform? parent = null,
                                       int facingSign = 0, bool spriteDefaultFacesLeft = false,
                                       Vector3? localOffset = null)
        {
            GameObject fx = parent != null
                ? Object.Instantiate(prefab, worldPos, Quaternion.identity, parent)
                : Object.Instantiate(prefab, worldPos, Quaternion.identity);
            Configure(fx, facingSign, spriteDefaultFacesLeft, localOffset);
        }

        // 경로 로드 + warn-once + 스폰 — SkillCastHandler 3 사이트용.
        // prefab 미존재 시 warnedFlag=false인 첫 1회만 경고, 이후 silent skip.
        // 나머지 인자는 SpawnPrefab과 동형.
        public static void SpawnFromPath(string resourcePath, Vector3 worldPos,
                                         ref bool warnedFlag, string displayName,
                                         Transform? parent = null, int facingSign = 0,
                                         bool spriteDefaultFacesLeft = false,
                                         Vector3? localOffset = null)
        {
            GameObject? prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                if (!warnedFlag)
                {
                    Debug.LogWarning($"[EffectSpawnService] {displayName} 미존재: Resources/{resourcePath} — 연출 생략.");
                    warnedFlag = true;
                }
                return;
            }
            SpawnPrefab(prefab, worldPos, parent, facingSign, spriteDefaultFacesLeft, localOffset);
        }
    }
}
