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

namespace Dawnholder.Client.Network
{
    // S_EnemyAttack (ID 20, v9) — 보스/적 → 플레이어 피격 결과.
    // 헌법 #1: 연출(이펙트/플래시/페이드)만 담당. HP 표시는 S_PlayerHp(ID 21)가 권위 통지.
    internal sealed class EnemyAttackHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_EnemyAttack pkt = new S_EnemyAttack();
            pkt.Read(buffer);

            int attackerId = pkt.attackerId;
            int targetId = pkt.targetId;
            int targetCurrentHp = pkt.targetCurrentHp;
            byte attackPattern = pkt.attackPattern;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (session.LocalEntityId == null) return;
                bool isLocalPlayer = targetId == session.LocalEntityId.Value;

                // 보스 찌르기(Stabbing_End)를 권위 타격 순간에 동기 — 텔레그래프 준비동작 유지 후 여기서 발동.
                // animState 클립-시계와 서버 히트 사이 jitter 제거. Strike param 없는 적은 무시.
                EnemyRegistry.Instance?.NotifyStrike(attackerId);

                // targetId 기반 대상 Transform 해석 — 로컬 플레이어면 LocalPlayerMovement,
                // 원격 플레이어면 RemoteEntityRegistry에서 조회. 해석 실패 시 null(폴백).
                Transform? targetTf = ResolveTargetTransform(session, targetId);

                // EnemyMotion facing을 서버 targetId 기반 실제 대상 쪽으로 고정.
                // targetTf=null이면 EnemyMotion이 기존 _facing을 유지(폴백).
                EnemyRegistry.Instance?.SetAttackTarget(attackerId, targetTf);

                // 공격자 kind 먼저 해석 — 앵커 결정 + spawn 분기 공용.
                EnemyKind attackerKind = default;
                bool kindKnown = EnemyRegistry.Instance != null &&
                                 EnemyRegistry.Instance.TryGetKind(attackerId, out attackerKind);

                // 보스 stab은 "검에서 나오는" 공격자 이펙트 → 피격자(플레이어)가 아니라 보스(공격자)에 앵커.
                // slime/golem 등 피격 이펙트는 기존대로 피격자(본인) 앵커.
                bool anchorOnAttacker = kindKnown && attackerKind == EnemyKind.Boss;

                // EffectAnchor = 발-pivot 보정 컨벤션 (없으면 root 폴백).
                Vector3 fxPos = Vector3.zero;
                int fxFacing = 1;
                bool hasFxPos = false;
                if (isLocalPlayer && targetTf != null && !anchorOnAttacker)
                {
                    fxPos = EffectAnchor.ResolvePosition(targetTf);
                    // 피격 이펙트는 공격이 날아온 쪽을 향함 — 공격자 위치 알면 상대 x 부호.
                    if (EnemyRegistry.Instance != null &&
                        EnemyRegistry.Instance.TryGetTransform(attackerId, out Transform atkTf) &&
                        atkTf != null)
                        fxFacing = atkTf.position.x >= targetTf.position.x ? 1 : -1;
                    hasFxPos = true;
                }
                else if (EnemyRegistry.Instance != null &&
                         EnemyRegistry.Instance.TryGetTransform(attackerId, out Transform attackerTf) &&
                         attackerTf != null)
                {
                    fxPos = EffectAnchor.ResolvePosition(attackerTf);
                    EnemyRegistry.Instance.TryGetFacing(attackerId, out fxFacing);
                    hasFxPos = true;
                }

                if (hasFxPos)
                {
                    AudioManager.Instance?.PlaySfx(SoundKeys.EnemyAttack);
                    // kind 해석 성공 시 kind-aware 오버로드, 실패 시 기존 보스 경로 폴백.
                    if (kindKnown)
                        BossAttackEffectSpawner.Spawn(attackerKind, attackPattern, fxPos, fxFacing);
                    else
                        BossAttackEffectSpawner.Spawn(attackPattern, fxPos, fxFacing);
                }

                if (!isLocalPlayer) return;

                // 본인 피격 *즉시* 신호 → hit-bridge 게이트 시작 (animState==Hit 스냅샷 전 입력 예측 갭 축소).
                AudioManager.Instance?.PlaySfx(SoundKeys.HitPlayer);
                LocalPlayerMovement.Instance?.NotifyHit();

                // 피격 플래시 — LocalPlayer GameObject에서 DamageFlash 조회 또는 런타임 주입.
                if (LocalPlayerMovement.Instance != null)
                {
                    DamageFlash flash = LocalPlayerMovement.Instance.GetComponent<DamageFlash>()
                                       ?? LocalPlayerMovement.Instance.gameObject.AddComponent<DamageFlash>();
                    flash.Flash();
                }

                // 사망 처리 — 리스폰 페이드 연출. HP 복구는 S_PlayerHp 권위 통지가 담당.
                if (targetCurrentHp <= 0)
                {
                    if (SceneTransition.Instance != null)
                        SceneTransition.Instance.PlayRespawnFade();
                }
            });
        }

        // targetId → Transform 해석 단일 진입점.
        // 로컬 플레이어면 LocalPlayerMovement.transform, 원격 플레이어면 RemoteEntityRegistry 조회.
        // 해석 실패(미등록/씬 없음) 시 null 반환 → 호출 측이 폴백 처리.
        static Transform? ResolveTargetTransform(UnityClientSession session, int targetId)
        {
            if (session.LocalEntityId.HasValue && targetId == session.LocalEntityId.Value)
                return LocalPlayerMovement.Instance != null
                    ? LocalPlayerMovement.Instance.transform
                    : null;

            if (RemoteEntityRegistry.Instance != null &&
                RemoteEntityRegistry.Instance.TryGetTransform(targetId, out Transform? t) &&
                t != null)
                return t;

            return null;
        }
    }
}
