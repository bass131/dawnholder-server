using System;
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
    // S_PlayerAttack (ID 22) — 플레이어(로컬/원격) 공격 연출.
    // 로컬: commit window 선예측이 스윙 모션 이미 처리 → 즉시 return.
    // 원격: attackType=0(Melee) 근접 스윙 이펙트만. attackType=1(Ranged) 투사체는 S_ProjectileLaunch 경로로 이관.
    // 헌법 #1: 연출만. 데미지/판정은 서버(S_HitResult/S_PlayerHp).
    internal sealed class PlayerAttackHandler : IClientPacketHandler
    {
        const string MeleeSwingEffectPath = "Effects/MeleeSwing";
        static bool _warnedMissingMeleeEffect;

        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_PlayerAttack pkt = new S_PlayerAttack();
            pkt.Read(buffer);

            int attackerId = pkt.attackerEntityId;
            byte attackType = pkt.attackType;
            byte facingByte = pkt.facing;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (session.LocalEntityId == null) return;

                // 로컬 플레이어 — commit window 선예측이 이미 스윙 모션 처리. 중복 차단.
                if (attackerId == session.LocalEntityId.Value) return;

                // Ranged(attackType=1) 투사체 연출은 S_ProjectileLaunch 수신 시 처리(ProjectileLaunchHandler).
                // S_PlayerAttack에서는 캐스팅 스윙 모션 플리퍼(원격 Mage 스윙 연출)만 트리거 가능하나
                // 현재 연출 에셋 미정 → Melee(0)만 실처리.
                if (attackType != 0) return;

                Transform? attackerTf = null;
                int facing = facingByte == 1 ? 1 : -1;

                if (RemoteEntityRegistry.Instance != null)
                    RemoteEntityRegistry.Instance.TryGetTransform(attackerId, out attackerTf);

                if (attackerTf == null) return;

                Vector3 fxPos = EffectAnchor.ResolvePosition(attackerTf);
                GameObject? meleePrefab = Resources.Load<GameObject>(MeleeSwingEffectPath);
                if (meleePrefab == null)
                {
                    if (!_warnedMissingMeleeEffect)
                    {
                        Debug.LogWarning(
                            $"[PlayerAttackHandler] 근접 스윙 이펙트 미존재: Resources/{MeleeSwingEffectPath}. " +
                            "Assets/Resources/Effects/ 에 추가하면 자동 적용됩니다.");
                        _warnedMissingMeleeEffect = true;
                    }
                    return;
                }
                GameObject fx = Object.Instantiate(meleePrefab, fxPos, Quaternion.identity);
                if (facing < 0)
                {
                    Vector3 s = fx.transform.localScale;
                    s.x = -Mathf.Abs(s.x);
                    fx.transform.localScale = s;
                }
                if (fx.GetComponent<EffectLifetime>() == null)
                    fx.AddComponent<EffectLifetime>();
            });
        }
    }
}
