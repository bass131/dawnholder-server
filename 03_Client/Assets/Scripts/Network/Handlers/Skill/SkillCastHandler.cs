using System;
using System.Collections.Generic;
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

namespace Dawnholder.Client.Network.Handlers.Skill
{
    // S_SkillCast (ID 25) — 스킬 캐스팅 연출 통보.
    // skillId 분기: 1=Thunderbolt, 2=Dash, 3=Teleport.
    // 데미지/판정은 S_HitResult가 담당 — 이 핸들러는 연출 + (Teleport) 보간 끊기만.
    internal sealed class SkillCastHandler : IClientPacketHandler
    {
        // Thunderbolt: 기존 캐스팅 이펙트.
        const string ThunderboltCastPath = "Effects/SkillCast";
        // Dash: 시전자에 재생. 영호가 Assets/Resources/Effects/DashSkill.prefab 추가 시 자동 적용.
        const string DashSkillPath = "Effects/DashSkill";
        // Teleport: 출발/도착 2지점 이펙트.
        const string TeleportDepartPath = "Effects/TeleportDepart";
        const string TeleportArrivePath = "Effects/TeleportArrive";
        // 텔레포트 출발/도착 이펙트 Y 오프셋 — 캐릭터 root(발밑) 기준 살짝 아래(영호 Play 튜닝). 순수 시각(밸런스 X).
        const float TeleportEffectYOffset = -0.5f;

        static bool _warnedMissingThunderbolt;
        static bool _warnedMissingDash;
        static bool _warnedMissingTeleportDepart;
        static bool _warnedMissingTeleportArrive;

        readonly struct PresentContext
        {
            internal readonly UnityClientSession Session;
            internal readonly int CasterId;
            internal readonly byte Facing;
            internal readonly bool IsLocal;
            internal readonly Transform CasterTf;

            internal PresentContext(UnityClientSession session, int casterId, byte facing, bool isLocal, Transform casterTf)
            {
                Session  = session;
                CasterId = casterId;
                Facing   = facing;
                IsLocal  = isLocal;
                CasterTf = casterTf;
            }
        }

        static readonly Dictionary<SkillId, Action<PresentContext>> _presentTable =
            new Dictionary<SkillId, Action<PresentContext>>
            {
                { SkillId.Thunderbolt, ctx => PresentThunderbolt(ctx) },
                { SkillId.Dash,        ctx => PresentDash(ctx)        },
                { SkillId.Teleport,    ctx => PresentTeleport(ctx)    },
            };

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
                if (_presentTable.TryGetValue(skill, out var present))
                    present(new PresentContext(session, casterId, facing, isLocal, casterTf));
            });
        }

        static void PresentThunderbolt(PresentContext ctx)
        {
            AudioManager.Instance?.PlaySfx(SoundKeys.MagicCast);
            EffectSpawnService.SpawnFromPath(ThunderboltCastPath, EffectAnchor.ResolvePosition(ctx.CasterTf),
                ref _warnedMissingThunderbolt, "Thunderbolt 캐스팅 VFX", facingSign: 0);
            // 원격 캐스팅 모션: 서버는 Channeling을 animState로 안 보냄(ThunderboltAction이 AttackState
            //   미진입) → S_SkillCast로 원격 캐스팅 모션 연출. 로컬은 LocalPlayerInput.NotifyChannel이 선예측.
            if (!ctx.IsLocal)
                RemoteEntityRegistry.Instance?.SetChanneling(ctx.CasterId,
                    Constants.AttackCommitWindowTicks * Constants.TickDuration);
        }

        static void PresentDash(PresentContext ctx)
        {
            // 원격: 서버가 Dash 중 Attack animState를 보냄 → facing latch 갱신.
            // 스킬은 타겟 스냅 없으므로 facing = 이동 방향 = Local 연출과 일치.
            // 직전 평타의 stale facing이 Dash에 잘못 적용되는 것을 방지.
            if (!ctx.IsLocal)
                RemoteEntityRegistry.Instance?.SetAttackFacing(ctx.CasterId, ctx.Facing == 1 ? 1 : -1);
            AudioManager.Instance?.PlaySfx(SoundKeys.Dash);
            HandleDash(ctx.IsLocal, ctx.CasterTf, ctx.Facing);
        }

        static void PresentTeleport(PresentContext ctx)
        {
            HandleTeleport(ctx.IsLocal, ctx.CasterId, ctx.CasterTf);
        }

        // Dash 연출: Dash 이펙트 스폰 + facing 반영.
        // 스폰 위치: Visual 계층에서 Anchor_DashEffect 우선, 없으면 EffectAnchor fallback.
        // 공격 모션은 서버 S_Snapshot(animState=Attack) force-adopt 경로에서 자동 처리됨.
        // 이동 자체도 S_Snapshot force-adopt(Attack 채널) — 예측 불필요.
        //
        // facing 출처: 로컬=화면 진실(LocalPlayerMotion.Facing — flipX 기준) / 원격=패킷.
        //   ProjectileLaunchHandler(685-695)와 동형 — 로컬은 입력 직후 방향 전환 시 패킷이 한 박자
        //   늦으므로 클라 화면 방향을 직접 읽어 어긋남 차단.
        static void HandleDash(bool isLocal, Transform casterTf, byte facing)
        {
            int facingSign;
            if (isLocal)
            {
                LocalPlayerMotion? motion = casterTf.GetComponent<LocalPlayerMotion>();
                facingSign = motion != null ? motion.Facing : (facing == 1 ? 1 : -1);
            }
            else
            {
                facingSign = facing == 1 ? 1 : -1;
            }

            // 대쉬는 빠르게 이동(D=4.0 / 8틱)하므로 이펙트를 시전 위치에 *고정*하면 캐릭터가 떠나
            //   이펙트만 제자리에 남아 끊겨 보인다(영호 실측). → 스폰 위치는 ResolvePosition(앵커 + flipX
            //   위치 미러)로 잡되, casterTf에 *부모로 묶어* 엔티티 Transform을 공유해 따라가게 한다.
            //   facing 고정(대쉬 중 P1 게이트)이라 스폰 시 1회 미러로 충분. (Thunderbolt/Teleport는 정지/
            //   월드 고정이라 무부모 스폰 유지 — 대쉬만 parenting.)
            Vector3 fxPos = EffectAnchor.ResolvePosition(casterTf, "Anchor_DashEffect");
            // DashSkill 스프라이트는 우향 기본 저작(895c3fb 재익스포트) → spriteDefaultFacesLeft=false.
            // facingSign<0(좌향)일 때만 localScale.x 반전 — 부모 flipX는 sprite-only라 transform 미전파.
            EffectSpawnService.SpawnFromPath(DashSkillPath, fxPos,
                ref _warnedMissingDash, "Dash 이펙트",
                parent: casterTf, facingSign: facingSign, spriteDefaultFacesLeft: false);
        }

        // Teleport 연출: 출발 이펙트 → (로컬) snap arming + 도착 콜백 등록 / (원격) 보간 끊기 + 도착 콜백 등록.
        //
        // 로컬 경로:
        //   출발 이펙트 위치 = LocalPlayerMovement.ConsumeTeleportDepartPos() stash (송신 시점 캡처).
        //   S_SkillCast 수신 이 시점에 ArmTeleportSnap()으로 _teleportSnapPending + arriveCallback 등록.
        //   → 텔레포트 반영된 첫 snapshot에서 force-adopt + arrive 발동(도착지에서 파티클).
        //   (P08 버그: 송신 시점 arming → 네트워크 지연 동안 옛 위치 snapshot이 플래그 소비 → arrive가 잘못된 위치에서 발동.)
        //
        // 원격 경로: S_SkillCast 수신 시점 casterTf.position을 출발 위치로 사용 (기존과 동일, 무변경).
        static void HandleTeleport(bool isLocal, int casterId, Transform casterTf)
        {
            AudioManager.Instance?.PlaySfx(SoundKeys.TeleportDepart);
            if (isLocal)
            {
                // 로컬 출발 이펙트: 송신 시점 stash 위치 사용.
                Vector3? departPos = LocalPlayerMovement.Instance?.ConsumeTeleportDepartPos();
                if (departPos.HasValue)
                    EffectSpawnService.SpawnFromPath(TeleportDepartPath,
                        departPos.Value + new Vector3(0f, TeleportEffectYOffset, 0f),
                        ref _warnedMissingTeleportDepart, "Teleport 출발 이펙트", facingSign: 0);

                // S_SkillCast 수신 이 시점에 snap arming — 다음 snapshot(텔레포트 반영)에서 arrive 발동.
                // arrive 콜백: 캐릭터 transform 자식으로 묶어 스폰(도착지에 글루).
                LocalPlayerMovement? lpm = LocalPlayerMovement.Instance;
                if (lpm != null)
                {
                    lpm.ArmTeleportSnap(arriveCallback: () =>
                    {
                        if (LocalPlayerMovement.Instance != null)
                            SpawnTeleportArrive(LocalPlayerMovement.Instance.transform);
                    });
                }
            }
            else
            {
                // 원격: S_SkillCast 수신 시점 casterTf.position을 출발 위치로 사용.
                Vector3 departPos = casterTf.position;
                EffectSpawnService.SpawnFromPath(TeleportDepartPath,
                    departPos + new Vector3(0f, TeleportEffectYOffset, 0f),
                    ref _warnedMissingTeleportDepart, "Teleport 출발 이펙트", facingSign: 0);

                // 보간 끊기 + 다음 snapshot 확정 시 1회 발동할 콜백 등록.
                if (RemoteEntityRegistry.Instance != null)
                {
                    int capturedId = casterId;
                    RemoteEntityRegistry.Instance.SetTeleportArriveCallback(capturedId, () =>
                    {
                        if (RemoteEntityRegistry.Instance != null &&
                            RemoteEntityRegistry.Instance.TryGetTransform(capturedId, out Transform? tf) && tf != null)
                            SpawnTeleportArrive(tf);
                    });
                    RemoteEntityRegistry.Instance.SnapEntity(casterId);
                }
            }
        }

        // 도착 TeleportArrive 이펙트 — 캐릭터 transform 자식 + localPosition (0, YOffset, 0) 글루.
        // ⚠️ EffectAnchor.ResolvePosition 사용 금지: 방향성(+X 오프셋 ~1.05) + flipX 미러라
        //   도착 이펙트가 캐릭터 옆으로 치우침(영호 실측). 도착은 방향 무관 중심 이펙트 — parent 원점.
        //   SpawnFromPath: worldPos=entityTf.position은 localOffset이 덮으므로 최종 localPosition=localOffset.
        //   facingSign=0 → flip 없음. localOffset → Configure가 localPosition+identity 세팅.
        internal static void SpawnTeleportArrive(Transform entityTf)
        {
            AudioManager.Instance?.PlaySfx(SoundKeys.TeleportArrive);
            EffectSpawnService.SpawnFromPath(TeleportArrivePath, entityTf.position,
                ref _warnedMissingTeleportArrive, "Teleport 도착 이펙트",
                parent: entityTf, facingSign: 0,
                localOffset: new Vector3(0f, TeleportEffectYOffset, 0f));
        }
    }
}
