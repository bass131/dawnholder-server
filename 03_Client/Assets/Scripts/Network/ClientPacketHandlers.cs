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
    // S2C 패킷 핸들러 — IClientPacketHandler 구현체. 서버 02_Server/GameServer/Handlers/ 미러.
    //
    // **Handle 진입 시점**: socket 워커 스레드.
    //   Unity API 직접 접근 금지. MainThreadDispatcher.Enqueue 경유 의무.
    // ========================================================================

    // S_HandshakeResult (ID 1)
    // ok=true → HandshakeOk 박음 + OnHandshakeOkEvent 호출. ok=false → 에러 로그 + Disconnect.
    internal sealed class HandshakeResultHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_HandshakeResult pkt = new S_HandshakeResult();
            pkt.Read(buffer);

            bool ok = pkt.ok;
            ushort sv = pkt.serverVersion;
            string reason = pkt.reason;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (ok)
                {
                    session.SetHandshakeOk();
                    Debug.Log($"[Unity] Handshake OK (server version={sv})");
                    session.RaiseHandshakeOk();
                }
                else
                {
                    Debug.LogError($"[Unity] Handshake FAILED — {reason} (server version={sv}). Disconnecting.");
                    session.Disconnect();
                }
            });
        }
    }

    // S_Pong (ID 2)
    internal sealed class PongHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_Pong pong = new S_Pong();
            pong.Read(buffer);

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long rtt = now - pong.clientTimestampMs;
            long oneWayLatencyEstimate = rtt / 2;
            long serverTs = pong.serverTimestampMs;

            MainThreadDispatcher.Enqueue(() =>
                Debug.Log($"[Unity] Pong! RTT = {rtt}ms (one-way ≈ {oneWayLatencyEstimate}ms, serverTs={serverTs})"));
        }
    }

    // S_EnterMap (ID 3)
    // 서버가 정한 spawn 좌표로 Player GameObject 배치 (헌법 #1).
    internal sealed class EnterMapHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_EnterMap pkt = new S_EnterMap();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            float x = pkt.spawnX;
            float y = pkt.spawnY;

            MainThreadDispatcher.Enqueue(() =>
            {
                session.SetLocalEntityId(eid);
                Debug.Log($"[Unity] EnterMap as entity {eid} at server spawn ({x}, {y})");
                MapNameDisplay.SetMapId(0); // S_EnterMap = Town 고정
                if (LocalPlayerMovement.Instance != null)
                {
                    // 이 분기도 terrain 주입 — ADR-027 (첫 진입 race 두 순서 모두 관측,
                    // movement가 먼저 깨어난 경우 pending 경로를 안 타서 주입 누락됨).
                    LocalPlayerMovement.Instance.InjectTerrain(0); // S_EnterMap = Town 고정
                    LocalPlayerMovement.Instance.SetServerPosition(new Vector3(x, y, 0f));
                }
                else
                {
                    // LocalPlayerSpawner가 아직 Instantiate 전(초기 진입 race) →
                    // PendingSpawn에 보관 → 곧 spawn될 LocalPlayerMovement.Awake()가 소비.
                    // S_EnterMap에 mapId 없음 → Town(0) 고정. MapTransition 경로는 destMapId 박음.
                    UnityClientSession.PendingSpawnX = x;
                    UnityClientSession.PendingSpawnY = y;
                    UnityClientSession.PendingMapId = 0;
                    UnityClientSession.HasPendingSpawn = true;
                }
            });
        }
    }

    // S_Snapshot (ID 4)
    // entityId 분기 — 본인 → reconcile, 타인 → RemoteEntityRegistry 보간 buffer push.
    internal sealed class SnapshotHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_Snapshot pkt = new S_Snapshot();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            float x = pkt.x;
            float y = pkt.y;
            float vx = pkt.vx;
            float vy = pkt.vy;
            int sTick = pkt.serverTick;
            uint ackedTick = pkt.lastAckedClientTick;
            byte animState = pkt.animState;

            MainThreadDispatcher.Enqueue(() =>
            {
                // 본인/타인 무관 최신 serverTick 갱신 (lag comp 기준점).
                session.SetLastReceivedServerTick(sTick);

                // LocalEntityId 모르면 (EnterMap 전 Snapshot race) drop.
                if (session.LocalEntityId == null) return;

                if (eid == session.LocalEntityId.Value)
                {
                    // 본인 path — reconcile(+넉백 force-adopt) + 서버 animState 전달.
                    // animState는 두 소비자: 이동 게이트(LocalPlayerMovement) + 시각 애니(LocalPlayerMotion).
                    if (LocalPlayerMovement.Instance != null)
                    {
                        LocalPlayerMovement.Instance.OnServerSnapshot(x, y, vx, vy, sTick, ackedTick, animState);
                        LocalPlayerMovement.Instance.GetComponent<LocalPlayerMotion>()
                            ?.SetServerAnimState(animState);
                    }
                }
                else
                {
                    // 타인 path — 전환 중이면 roster buffer 캐싱.
                    float capturedX = x;
                    float capturedY = y;
                    int capturedEid = eid;
                    byte capturedAnimState = animState;
                    if (session.RosterBuffer.TryBuffer(
                            $"S_Snapshot entity={eid}",
                            () =>
                            {
                                if (RemoteEntityRegistry.Instance != null)
                                    RemoteEntityRegistry.Instance.UpdateSnapshot(capturedEid, capturedX, capturedY, capturedAnimState);
                            }))
                        return;

                    if (RemoteEntityRegistry.Instance != null)
                        RemoteEntityRegistry.Instance.UpdateSnapshot(eid, x, y, animState);
                }
            });
        }
    }

    // S_PlayerJoin (ID 9)
    // 타인 entity spawn. 전환 중이면 roster buffer 캐싱.
    internal sealed class PlayerJoinHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_PlayerJoin pkt = new S_PlayerJoin();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            float x = pkt.spawnX;
            float y = pkt.spawnY;
            CharacterClass cls = ClassLoadout.ByteToClass(pkt.characterClass);

            MainThreadDispatcher.Enqueue(() =>
            {
                if (session.LocalEntityId != null && eid == session.LocalEntityId.Value) return;

                // 전환 중이면 roster buffer 캐싱.
                if (session.RosterBuffer.TryBuffer(
                        $"S_PlayerJoin entity={eid}",
                        () =>
                        {
                            if (RemoteEntityRegistry.Instance != null)
                                RemoteEntityRegistry.Instance.Spawn(eid, x, y, cls);
                        }))
                    return;

                if (RemoteEntityRegistry.Instance != null)
                    RemoteEntityRegistry.Instance.Spawn(eid, x, y, cls);
            });
        }
    }

    // S_PlayerLeave (ID 6)
    // 타인 entity despawn.
    internal sealed class PlayerLeaveHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_PlayerLeave pkt = new S_PlayerLeave();
            pkt.Read(buffer);

            int eid = pkt.entityId;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (RemoteEntityRegistry.Instance != null)
                    RemoteEntityRegistry.Instance.Despawn(eid);
            });
        }
    }

    // S_EntitySpawn (ID 12) — enemy/boss spawn. entityKind 분기. 전환 중 roster buffer 캐싱.
    internal sealed class EntitySpawnHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_EntitySpawn pkt = new S_EntitySpawn();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            byte kind = pkt.entityKind;
            float x = pkt.x;
            float y = pkt.y;
            int hp = pkt.currentHp;
            int maxHp = pkt.maxHp;

            MainThreadDispatcher.Enqueue(() =>
            {
                // 전환 중이면 roster buffer 캐싱.
                int capturedEid = eid;
                byte capturedKind = kind;
                float capturedX = x;
                float capturedY = y;
                int capturedHp = hp;
                int capturedMaxHp = maxHp;
                if (session.RosterBuffer.TryBuffer(
                        $"S_EntitySpawn entity={eid}",
                        () =>
                        {
                            if (EnemyRegistry.Instance == null)
                            {
                                Debug.LogWarning($"[Unity] EnemyRegistry 미박힘 (roster drain) — entity {capturedEid} spawn drop.");
                                return;
                            }
                            EnemyRegistry.Instance.Spawn(capturedEid, capturedKind, capturedX, capturedY, capturedHp, capturedMaxHp);
                        }))
                    return;

                if (EnemyRegistry.Instance == null)
                {
                    Debug.LogWarning($"[Unity] EnemyRegistry 미박힘 — entity {eid} spawn drop. CombatBootstrap 누락?");
                    return;
                }
                EnemyRegistry.Instance.Spawn(eid, kind, x, y, hp, maxHp);
            });
        }
    }

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
            byte hitEffect = pkt.hitEffect;

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Unity] Hit: attacker={attackerId} target={targetId} dmg={dmg} hp={hp}/{maxHp} effect={hitEffect}");
                if (EnemyRegistry.Instance == null) return;

                // 데미지 텍스트 + 적 HP 갱신 — 모든 hitEffect 공통.
                EnemyRegistry.Instance.ApplyHit(targetId, hp, maxHp);

                // hitEffect별 VFX — target 위치에 스폰.
                if (hitEffect == 1 || hitEffect == 2 || hitEffect == 3)
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
                        string vfxPath = hitEffect == 1 ? ProjectileImpactPath
                                       : hitEffect == 2 ? LightningVfxPath
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
                            if (hitEffect == 1 && !_warnedMissingImpact)
                            {
                                Debug.LogWarning($"[HitResultHandler] 투사체 임팩트 VFX 미존재: Resources/{ProjectileImpactPath}");
                                _warnedMissingImpact = true;
                            }
                            else if (hitEffect == 2 && !_warnedMissingLightning)
                            {
                                Debug.LogWarning($"[HitResultHandler] 낙뢰 VFX 미존재: Resources/{LightningVfxPath}");
                                _warnedMissingLightning = true;
                            }
                            else if (hitEffect == 3 && !_warnedMissingDashHit)
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

    // S_EntityDeath (ID 14) — entity 사라짐.
    internal sealed class EntityDeathHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_EntityDeath pkt = new S_EntityDeath();
            pkt.Read(buffer);

            int eid = pkt.entityId;

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Unity] Entity {eid} died");
                if (EnemyRegistry.Instance == null) return;
                EnemyRegistry.Instance.Despawn(eid);
            });
        }
    }

    // S_StageClear (ID 15) — 보스 처치 → UI 표시.
    internal sealed class StageClearHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_StageClear pkt = new S_StageClear();
            pkt.Read(buffer);

            int bossId = pkt.bossEntityId;

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Unity] StageClear! (boss entity {bossId})");
                if (StageClearUI.Instance == null)
                {
                    Debug.LogWarning("[Unity] StageClearUI 미박힘 — UI drop. CombatBootstrap 누락?");
                    return;
                }
                StageClearUI.Instance.Show(bossId);
            });
        }
    }

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

                // 이펙트 위치: 본인 피격 = LocalPlayer 앵커, 그 외 = 공격자(보스) 앵커.
                // EffectAnchor = 발-pivot 보정 컨벤션 (없으면 root 폴백).
                Vector3 fxPos = Vector3.zero;
                int fxFacing = 1;
                bool hasFxPos = false;
                if (isLocalPlayer && targetTf != null)
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
                    BossAttackEffectSpawner.Spawn(attackPattern, fxPos, fxFacing);

                if (!isLocalPlayer) return;

                // 본인 피격 *즉시* 신호 → hit-bridge 게이트 시작 (animState==Hit 스냅샷 전 입력 예측 갭 축소).
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

    // S_PlayerHp (ID 21) — 서버 권위 플레이어 HP 통지.
    // 헌법 #1: 클라는 이 값을 신뢰해 HUD에 표시만. HP 직접 계산 X.
    // entityId == LocalEntityId일 때만 소비 (원격 플레이어 HP 바는 미래 범위).
    internal sealed class PlayerHpHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_PlayerHp pkt = new S_PlayerHp();
            pkt.Read(buffer);

            int entityId = pkt.entityId;
            int currentHp = pkt.currentHp;
            int maxHp = pkt.maxHp;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (session.LocalEntityId == null) return;
                if (entityId != session.LocalEntityId.Value) return;

                HudController.Instance?.UpdateHP(currentHp, maxHp);
            });
        }
    }

    // S_EntityState (ID 19) — 적 AI 위치/상태 주기적 갱신.
    // 서버가 SnapshotTickInterval(=2틱=100ms)마다 broadcast.
    internal sealed class EntityStateHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_EntityState pkt = new S_EntityState();
            pkt.Read(buffer);

            int eid = pkt.entityId;
            float x = pkt.x;
            float y = pkt.y;
            // state(byte) = 서버 AI FSM 상태 — 시각 미사용.
            // animState(byte) = 시각 애니 상태 — AnimatorDriver 경로로 전달.
            byte animState = pkt.animState;

            MainThreadDispatcher.Enqueue(() =>
            {
                if (EnemyRegistry.Instance == null) return;
                // spawn 전 도착(race)이면 EnemyRegistry.UpdatePosition이 silent skip.
                EnemyRegistry.Instance.UpdatePosition(eid, x, y, animState);
            });
        }
    }

    // S_MapTransition (ID 18) — 맵 전환.
    // 헌법 #1: S_MapTransition 도착 후 비로소 scene 전환. 클라 자체 portal 판정 X.
    internal sealed class MapTransitionHandler : IClientPacketHandler
    {
        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_MapTransition pkt = new S_MapTransition();
            pkt.Read(buffer);

            byte destMapId = pkt.destMapId;
            float spawnX = pkt.spawnX;
            float spawnY = pkt.spawnY;

            MainThreadDispatcher.Enqueue(() =>
            {
                string sceneName = SceneRouter.MapIdToSceneName(destMapId);
                Debug.Log($"[Unity] MapTransition → destMapId={destMapId} scene='{sceneName}' spawn=({spawnX:F2},{spawnY:F2})");

                if (string.IsNullOrEmpty(sceneName))
                {
                    Debug.LogError($"[Unity] S_MapTransition: 알 수 없는 destMapId={destMapId} — 전환 취소.");
                    return;
                }

                MapNameDisplay.SetMapId(destMapId);

                // roster buffer 활성화 — 전환 중 도착하는 roster 패킷 캐싱.
                session.RosterBuffer.BeginTransition(sceneName);

                // prediction 버퍼 리셋: 이전 맵 입력이 새 맵 좌표계에서 replay되면 캐릭터가 튐.
                if (LocalPlayerMovement.Instance != null)
                    LocalPlayerMovement.Instance.ResetPredictionForMapTransition();

                // spawn 좌표 + mapId 보관 — 씬 로드 완료 후 새 LocalPlayerMovement.Awake()가 읽어 적용.
                // PendingMapId: Awake에서 ClientTerrainStore.Load(mapId) 호출 → predictor terrain 주입.
                // 갱신 누락 시 이전 맵 지형으로 예측 → 드리프트 폭증.
                UnityClientSession.PendingSpawnX = spawnX;
                UnityClientSession.PendingSpawnY = spawnY;
                UnityClientSession.PendingMapId = destMapId;
                UnityClientSession.HasPendingSpawn = true;

                // SceneTransition(페이드) 경유 씬 전환. Instance null 시 직접 LoadScene으로 fallback.
                if (SceneTransition.Instance != null)
                    SceneTransition.Instance.LoadScene(sceneName);
                else
                {
                    Debug.LogWarning("[Unity] SceneTransition.Instance null — direct LoadScene fallback (페이드 없음).");
                    SceneManager.LoadScene(sceneName);
                }
            });
        }
    }

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

    // S_SkillCast (ID 25) — 스킬 캐스팅 연출 통보.
    // skillId 분기: 1=Thunderbolt, 2=Dash, 3=Teleport.
    // 데미지/판정은 S_HitResult가 담당 — 이 핸들러는 연출 + (Teleport) 보간 끊기만.
    internal sealed class SkillCastHandler : IClientPacketHandler
    {
        // Thunderbolt: 기존 캐스팅 이펙트.
        const string ThunderboltCastPath = "Effects/SkillCast";
        // Dash: 시전자에 재생. 영호가 Assets/Resources/Effects/DashSkill.prefab 추가 시 자동 적용.
        const string DashSkillPath = "Effects/DashSkill";
        // Teleport: 출발/도착 이펙트. 에셋 1종(Mage_Teleport)을 영호가 2경로로 복제 배치 예정.
        const string TeleportDepartPath = "Effects/TeleportDepart";
        const string TeleportArrivePath = "Effects/TeleportArrive";

        static bool _warnedMissingThunderbolt;
        static bool _warnedMissingDash;
        static bool _warnedMissingTeleportDepart;
        static bool _warnedMissingTeleportArrive;

        public void Handle(UnityClientSession session, ArraySegment<byte> buffer)
        {
            S_SkillCast pkt = new S_SkillCast();
            pkt.Read(buffer);

            int casterId = pkt.casterEntityId;
            byte skillId = pkt.skillId;
            byte facing = pkt.facing; // 0=좌, 1=우

            MainThreadDispatcher.Enqueue(() =>
            {
                if (session.LocalEntityId == null) return;

                bool isLocal = casterId == session.LocalEntityId.Value;

                // caster 위치 조회 (로컬/원격 분기).
                Transform? casterTf = null;
                if (isLocal)
                    casterTf = LocalPlayerMovement.Instance?.transform;
                else if (RemoteEntityRegistry.Instance != null)
                    RemoteEntityRegistry.Instance.TryGetTransform(casterId, out casterTf);

                if (casterTf == null) return;

                Debug.Log($"[Unity] SkillCast: caster={casterId} skill={skillId} facing={facing} local={isLocal}");

                SkillId skill = (SkillId)skillId;
                switch (skill)
                {
                    case SkillId.Thunderbolt:
                        SpawnEffect(ThunderboltCastPath, EffectAnchor.ResolvePosition(casterTf),
                            facingSign: 0, ref _warnedMissingThunderbolt, "Thunderbolt 캐스팅 VFX");
                        break;

                    case SkillId.Dash:
                        HandleDash(casterTf, facing);
                        break;

                    case SkillId.Teleport:
                        HandleTeleport(isLocal, casterId, casterTf);
                        break;
                }
            });
        }

        // Dash 연출: Dash 이펙트 스폰 + facing 반영.
        // 공격 모션은 서버 S_Snapshot(animState=Attack) force-adopt 경로에서 자동 처리됨.
        // 이동 자체도 S_Snapshot force-adopt(Attack 채널) — 예측 불필요.
        static void HandleDash(Transform casterTf, byte facing)
        {
            int facingSign = facing == 1 ? 1 : -1;
            Vector3 fxPos = EffectAnchor.ResolvePosition(casterTf);
            SpawnEffect(DashSkillPath, fxPos, facingSign, ref _warnedMissingDash, "Dash 이펙트");
        }

        // Teleport 연출: 출발 이펙트 → 보간 끊기 → 도착 이펙트 콜백 등록.
        // 도착 이펙트는 새 위치가 확정되는 시점(스냅 채택 직후)에 발동 — 출발 위치 placeholder 제거.
        static void HandleTeleport(bool isLocal, int casterId, Transform casterTf)
        {
            Vector3 departPos = casterTf.position; // 출발 위치 — casterTf가 아직 갱신 전.

            SpawnEffect(TeleportDepartPath, departPos, 0, ref _warnedMissingTeleportDepart, "Teleport 출발 이펙트");

            if (isLocal)
            {
                LocalPlayerMovement.Instance?.NotifyTeleport(arriveCallback: () =>
                {
                    if (LocalPlayerMovement.Instance != null)
                        SpawnArriveEffect(LocalPlayerMovement.Instance.transform);
                });
            }
            else
            {
                // 원격: 보간 끊기 + 다음 snapshot 확정 시 1회 발동할 콜백 등록.
                if (RemoteEntityRegistry.Instance != null)
                {
                    int capturedId = casterId;
                    RemoteEntityRegistry.Instance.SetTeleportArriveCallback(capturedId, () =>
                    {
                        if (RemoteEntityRegistry.Instance != null &&
                            RemoteEntityRegistry.Instance.TryGetTransform(capturedId, out Transform? tf) && tf != null)
                            SpawnArriveEffect(tf);
                    });
                    RemoteEntityRegistry.Instance.SnapEntity(casterId);
                }
            }
        }

        // 도착 위치에서 TeleportArrive 이펙트 스폰.
        static void SpawnArriveEffect(Transform entityTf)
        {
            SpawnEffect(TeleportArrivePath, EffectAnchor.ResolvePosition(entityTf),
                0, ref _warnedMissingTeleportArrive, "Teleport 도착 이펙트");
        }

        // 공통 이펙트 스폰 helper. facingSign=0이면 flip 없음.
        static void SpawnEffect(string resourcePath, Vector3 pos, int facingSign,
                                ref bool warnedFlag, string displayName)
        {
            GameObject? prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null)
            {
                GameObject fx = Object.Instantiate(prefab, pos, Quaternion.identity);
                if (facingSign < 0)
                {
                    Vector3 s = fx.transform.localScale;
                    s.x = -Mathf.Abs(s.x);
                    fx.transform.localScale = s;
                }
                if (fx.GetComponent<EffectLifetime>() == null)
                    fx.AddComponent<EffectLifetime>();
            }
            else if (!warnedFlag)
            {
                Debug.LogWarning($"[SkillCastHandler] {displayName} 미존재: Resources/{resourcePath} — 연출 생략.");
                warnedFlag = true;
            }
        }
    }
}
