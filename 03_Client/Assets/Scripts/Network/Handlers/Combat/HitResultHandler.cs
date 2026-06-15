using System;
using Dawnholder.Client.Audio;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Net;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.Rendering;
using Dawnholder.Client.Scenes;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Dawnholder.Client.Network
{
    // S_HitResult (ID 13) — damage 적용 + HP 갱신 표시 + hitEffect별 VFX.
    // 헌법 #1: 클라는 서버 결과 표시만. 판정 X.
    // hitEffect: 0=근접(기존), 1=투사체 도착 임팩트, 2=낙뢰(썬더볼트), 3=Dash 임팩트.
    internal sealed class HitResultHandler : IClientPacketHandler
    {
        // 투사체 도착 임팩트 VFX 경로. 기존 Resources/Effects/ 규칙 정합.
        const string ProjectileImpactPath = "Effects/ProjectileImpact";
        // 낙뢰 VFX 경로. 에셋 미존재 시 placeholder 로그 후 skip.
        const string LightningVfxPath = "Effects/LightningStrike";
        // Dash 임팩트 VFX 경로. 영호가 Assets/Resources/Effects/DashHit.prefab 추가 시 자동 적용.
        const string DashHitVfxPath = "Effects/DashHit";
        static bool _warnedMissingImpact;
        static bool _warnedMissingLightning;
        static bool _warnedMissingDashHit;

        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_HitResult pkt = new S_HitResult();
            pkt.Read(buffer);

            int attackerId = pkt.attackerEntityId;
            int targetId = pkt.targetEntityId;
            int dmg = pkt.damage;
            int hp = pkt.currentHp;
            int maxHp = pkt.maxHp;
            HitEffect hitEffect = (HitEffect)pkt.hitEffect;

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Unity] Hit: attacker={attackerId} target={targetId} dmg={dmg} hp={hp}/{maxHp} effect={hitEffect}");
                if (EnemyRegistry.Instance == null) return;

                // 데미지 텍스트 + 적 HP 갱신 — 모든 hitEffect 공통.
                EnemyRegistry.Instance.ApplyHit(targetId, hp, maxHp);

                // 피격 사운드 — Lightning은 전용, 그 외엔 적 종류별 분리.
                if (hitEffect == HitEffect.Lightning)
                {
                    AudioManager.Instance?.PlaySfx(SoundKeys.HitLightning, 1f, 0.05f);
                }
                else
                {
                    string hitKey = SoundKeys.HitGeneric;
                    if (EnemyRegistry.Instance.TryGetKind(targetId, out EnemyKind hitKind))
                        hitKey = hitKind == EnemyKind.Normal ? SoundKeys.HitSlime
                               : hitKind == EnemyKind.Golem ? SoundKeys.HitGolem
                               : SoundKeys.HitGeneric;
                    AudioManager.Instance?.PlaySfx(hitKey, 1f, 0.05f);
                }

                // hitEffect별 VFX — target 위치에 스폰.
                if (hitEffect == HitEffect.Projectile || hitEffect == HitEffect.Lightning || hitEffect == HitEffect.Dash)
                {
                    Vector3 fxPos = Vector3.zero;
                    bool hasFxPos = false;
                    if (EnemyRegistry.Instance.TryGetTransform(targetId, out Transform? targetTf) && targetTf != null)
                    {
                        fxPos = EffectAnchor.ResolvePosition(targetTf);
                        hasFxPos = true;
                    }

                    if (hasFxPos)
                    {
                        string vfxPath = hitEffect == HitEffect.Projectile ? ProjectileImpactPath
                                       : hitEffect == HitEffect.Lightning ? LightningVfxPath
                                       : DashHitVfxPath;
                        GameObject? vfxPrefab = Resources.Load<GameObject>(vfxPath);
                        if (vfxPrefab != null)
                        {
                            GameObject fx = Object.Instantiate(vfxPrefab, fxPos, Quaternion.identity);
                            if (fx.GetComponent<EffectLifetime>() == null)
                                fx.AddComponent<EffectLifetime>();
                        }
                        else
                        {
                            if (hitEffect == HitEffect.Projectile && !_warnedMissingImpact)
                            {
                                Debug.LogWarning($"[HitResultHandler] 투사체 임팩트 VFX 미존재: Resources/{ProjectileImpactPath}");
                                _warnedMissingImpact = true;
                            }
                            else if (hitEffect == HitEffect.Lightning && !_warnedMissingLightning)
                            {
                                Debug.LogWarning($"[HitResultHandler] 낙뢰 VFX 미존재: Resources/{LightningVfxPath}");
                                _warnedMissingLightning = true;
                            }
                            else if (hitEffect == HitEffect.Dash && !_warnedMissingDashHit)
                            {
                                Debug.LogWarning($"[HitResultHandler] Dash 임팩트 VFX 미존재: Resources/{DashHitVfxPath}");
                                _warnedMissingDashHit = true;
                            }
                        }
                    }
                }
            });
        }
    }
}
