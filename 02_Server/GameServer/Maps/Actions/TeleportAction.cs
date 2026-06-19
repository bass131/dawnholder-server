using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Entities;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Maps.Actions;

// Mage Teleport 행동. SkillSystem.ProcessTeleport 본체 1:1 이관 — 거동 불변.
// 쿨다운·클래스·rewind 검증은 ActionGate 선행 처리.
internal sealed class TeleportAction : IGameAction
{
    internal static readonly TeleportAction Instance = new();

    public ActionKind Kind => ActionKind.Teleport;
    public int CooldownTicks => CombatConstants.TeleportCooldownTicks;
    public CharacterClass? RequiredClass => CharacterClass.Mage;

    public bool Execute(GameMap map, PlayerEntity caster, in ActionContext ctx)
    {
        // 4방향 목적지 산출 (M4.15 P09 지형 인식) — 서버 권위 (헌법 #1). ctx.VerticalDir는 핸들러 §3 정규화 완료값(0/1/2).
        //   0(수평): X축 FacingDir 방향 이동, Y 유지 (기존 거동).
        //   1(위)  : 위 발판 탐지 → 사거리 내 발판 있으면 발판 표면으로 snap. 없으면 현위치 유지.
        //   2(아래): 아래 발판 탐지 → 동일.
        // 이동 불가(발판 없음·사거리 밖)이어도 S_SkillCast는 무조건 broadcast — 이펙트 신호 보장.
        float destX;
        float destY;
        if (ctx.VerticalDir == 1)
        {
            // 위: 위 방향 발판 탐지. 발판 없거나 사거리 밖이면 현위치 유지.
            map.TryFindVerticalTeleportTarget(
                caster.Position.X, caster.Position.Y,
                up: true, CombatConstants.TeleportVerticalRange,
                out destY);
            destX = caster.Position.X;
        }
        else if (ctx.VerticalDir == 2)
        {
            // 아래: 아래 방향 발판 탐지. 발판 없거나 사거리 밖이면 현위치 유지.
            map.TryFindVerticalTeleportTarget(
                caster.Position.X, caster.Position.Y,
                up: false, CombatConstants.TeleportVerticalRange,
                out destY);
            destX = caster.Position.X;
        }
        else
        {
            // 수평(기존 거동): X축 FacingDir 방향, MapBoundsX clamp, Y 유지.
            float rawDestX = caster.Position.X + CombatConstants.TeleportDistance * caster.FacingDir;
            (float xMin, float xMax) = map.MapBoundsX;
            destX = MathF.Max(xMin, MathF.Min(xMax, rawDestX));
            destY = caster.Position.Y;
        }

        caster.Position = new Vector2(destX, destY);
        caster.RecordPosition(map.CurrentTick, caster.Position);

        S_SkillCast castPkt = new S_SkillCast
        {
            casterEntityId   = caster.EntityId,
            skillId          = (byte)SkillId.Teleport,
            strikeDelayTicks = 0,
            facing           = caster.FacingByte,
        };
        map.BroadcastToAll(castPkt.Write());
        return true;
    }
}
