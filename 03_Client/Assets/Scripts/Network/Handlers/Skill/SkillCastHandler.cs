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
                        // 원격: 서버가 Dash 중 Attack animState를 보냄 → facing latch 갱신.
                        // 스킬은 타겟 스냅 없으므로 facing = 이동 방향 = Local 연출과 일치.
                        // 직전 평타의 stale facing이 Dash에 잘못 적용되는 것을 방지.
                        if (!isLocal)
                            RemoteEntityRegistry.Instance?.SetAttackFacing(casterId, facing == 1 ? 1 : -1);
                        HandleDash(isLocal, casterTf, facing);
                        break;

                    case SkillId.Teleport:
                        HandleTeleport(isLocal, casterId, casterTf);
                        break;
                }
            });
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
            //   월드 고정이라 무부모 SpawnEffect 유지 — 대쉬만 parenting.)
            Vector3 fxPos = EffectAnchor.ResolvePosition(casterTf, "Anchor_DashEffect");
            // DashSkill 스프라이트는 우향 기본 저작(895c3fb 재익스포트) → spriteDefaultFacesLeft=false.
            // facingSign<0(좌향)일 때만 localScale.x 반전 — 부모 flipX는 sprite-only라 transform 미전파.
            SpawnEffectParented(DashSkillPath, fxPos, casterTf, facingSign, ref _warnedMissingDash,
                "Dash 이펙트", spriteDefaultFacesLeft: false);
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

        // 공통 이펙트 스폰 helper. facingSign=0이면 flip 없음(방향 무관 이펙트).
        // spriteDefaultFacesLeft=true면 좌향 기본 저작 prefab — flip 조건 반전(AnimatorDriver 동형).
        //   기본 false = 우향 기본 전제(facingSign<0=왼쪽일 때만 거울상).
        static void SpawnEffect(string resourcePath, Vector3 pos, int facingSign,
                                ref bool warnedFlag, string displayName,
                                bool spriteDefaultFacesLeft = false)
        {
            GameObject? prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null)
            {
                GameObject fx = Object.Instantiate(prefab, pos, Quaternion.identity);
                // facingSign=0(방향 무관)은 flip 생략. 그 외 (왼쪽) XOR (좌향 기본) = 거울상 여부.
                if (facingSign != 0 && ((facingSign < 0) ^ spriteDefaultFacesLeft))
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

        // 부모에 자식으로 묶어 스폰 — 엔티티 Transform을 따라가는 이펙트(대쉬)용.
        //   worldPos = ResolvePosition(앵커 + flipX 위치 미러) 결과를 그대로 사용 → 미러 보존 +
        //   parent에 묶여 엔티티 따라감(Instantiate가 world 위치를 유지하며 부모의 local로 변환).
        //   flip 규칙은 SpawnEffect와 동형(localScale.x — 부모 flipX는 sprite-only라 transform 미전파).
        //   정지/월드 이펙트는 무부모 SpawnEffect 사용.
        static void SpawnEffectParented(string resourcePath, Vector3 worldPos, Transform parent, int facingSign,
                                        ref bool warnedFlag, string displayName,
                                        bool spriteDefaultFacesLeft = false)
        {
            GameObject? prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null)
            {
                GameObject fx = Object.Instantiate(prefab, worldPos, Quaternion.identity, parent);
                if (facingSign != 0 && ((facingSign < 0) ^ spriteDefaultFacesLeft))
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
