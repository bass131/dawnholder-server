using System;
using Dawnholder.Client.Audio;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Net;
using Dawnholder.Client.Network;
using Dawnholder.Client.Network.Handlers;
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

namespace Dawnholder.Client.Network.Handlers.Skill
{
    // S_ProjectileLaunch (ID 23) — 서버 확정 투사체 발사 연출 통보.
    // 로컬/원격 공통 경로 — 클라 선예측 스폰 제거(M4.8 기둥1) 후 모든 투사체가 이 핸들러로 스폰.
    // travelTicks: 발사~서버 도착 틱 수. 비행 속도를 역산해 클라 투사체 도착 시각과 서버 도착 틱을 맞춤.
    internal sealed class ProjectileLaunchHandler : IClientPacketHandler
    {
        const string LocalProjectilePath = "Effects/Projectile";
        const string RemoteProjectilePath = "Effects/RemoteProjectile";
        static bool _warnedMissingLocal;
        static bool _warnedMissingRemote;

        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_ProjectileLaunch pkt = new S_ProjectileLaunch();
            pkt.Read(buffer);

            int attackerId = pkt.attackerEntityId;
            int targetId = pkt.targetEntityId;
            int travelTicks = pkt.travelTicks;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (session.LocalEntityId == null) return;

                bool isLocal = attackerId == session.LocalEntityId.Value;

                // 발사 위치: 로컬=LocalPlayer, 원격=RemoteEntityRegistry.
                Transform? spawnRoot = null;
                int facing = 1;
                if (isLocal)
                {
                    spawnRoot = LocalPlayerMovement.Instance?.transform;
                    if (spawnRoot != null)
                    {
                        LocalPlayerMotion? motion = spawnRoot.GetComponent<LocalPlayerMotion>();
                        if (motion != null) facing = motion.Facing;
                    }
                }
                else
                {
                    if (RemoteEntityRegistry.Instance != null)
                        RemoteEntityRegistry.Instance.TryGetTransform(attackerId, out spawnRoot);
                }

                // 타겟 Transform (없으면 facing 방향 직진 폴백).
                Transform? target = null;
                if (targetId != 0 && EnemyRegistry.Instance != null)
                    EnemyRegistry.Instance.TryGetTransform(targetId, out target);

                // 발사자 미존재(despawn race) — 스폰 생략.
                if (!isLocal && spawnRoot == null) return;

                string path = isLocal ? LocalProjectilePath : RemoteProjectilePath;
                GameObject? prefab = Resources.Load<GameObject>(path);
                if (prefab == null)
                {
                    // TODO: 투사체 prefab 없음 — 유현/영호가 Assets/Resources/Effects/ 에 추가 필요.
                    bool warned = isLocal ? _warnedMissingLocal : _warnedMissingRemote;
                    if (!warned)
                    {
                        Debug.LogWarning($"[ProjectileLaunchHandler] 투사체 prefab 미존재: Resources/{path}");
                        if (isLocal) _warnedMissingLocal = true;
                        else _warnedMissingRemote = true;
                    }
                    return;
                }

                Vector3 spawnPos = spawnRoot != null
                    ? EffectAnchor.ResolvePosition(spawnRoot)
                    : (target?.position ?? Vector3.zero);

                AudioManager.Instance?.PlaySfx(SoundKeys.ProjectileLaunch);
                GameObject proj = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
                ProjectileVisual visual = proj.GetComponent<ProjectileVisual>()
                                         ?? proj.AddComponent<ProjectileVisual>();

                // travelTicks로 비행 속도 역산 — 도착 ≈ 서버 도착 틱.
                // 거리 / (travelTicks × TickDuration) = 픽셀속도. travelTicks=0이면 즉발(Destroy).
                if (travelTicks > 0 && target != null)
                {
                    float dist = Vector3.Distance(spawnPos, target.position);
                    float duration = travelTicks * Constants.TickDuration;
                    visual.SetTravelDuration(dist, duration);
                }

                if (target != null)
                    visual.Launch(target);
                else
                    visual.LaunchDirection(new Vector3(facing >= 0 ? 1f : -1f, 0f, 0f));
            });
        }
    }
}
