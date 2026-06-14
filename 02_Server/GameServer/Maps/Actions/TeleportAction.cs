using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps;
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
        // 4방향 목적지 산출 (M4.15 P07) — 서버 권위 (헌법 #1). ctx.VerticalDir는 핸들러 §3 정규화 완료값(0/1/2).
        //   0(수평): X축 FacingDir 방향 이동, Y 유지 (기존 거동).
        //   1(위)  : Y += 거리, X 유지.
        //   2(아래): Y -= 거리, X 유지.
        // 수직 이동은 X 그대로라 좌우 FacingDir과 충돌 없음 — 수평/수직 상호 배타.
        float destX;
        float destY;
        if (ctx.VerticalDir == 1)
        {
            // 위: Y 증가. MapBoundsY 상한으로 clamp(맵 밖 허공 이탈 차단).
            destX = caster.Position.X;
            float rawDestY = caster.Position.Y + CombatConstants.TeleportDistance;
            (float yMin, float yMax) = map.MapBoundsY;
            destY = MathF.Max(yMin, MathF.Min(yMax, rawDestY));
        }
        else if (ctx.VerticalDir == 2)
        {
            // 아래: Y 감소. MapBoundsY 하한으로 clamp(맵 밖 지하 이탈 차단).
            destX = caster.Position.X;
            float rawDestY = caster.Position.Y - CombatConstants.TeleportDistance;
            (float yMin, float yMax) = map.MapBoundsY;
            destY = MathF.Max(yMin, MathF.Min(yMax, rawDestY));
        }
        else
        {
            // 수평(기존 거동): X축 FacingDir 방향, MapBoundsX clamp, Y 유지.
            float rawDestX = caster.Position.X + CombatConstants.TeleportDistance * caster.FacingDir;
            (float xMin, float xMax) = map.MapBoundsX;
            destX = MathF.Max(xMin, MathF.Min(xMax, rawDestX));
            destY = caster.Position.Y;
        }

        // 영구 끼임(stranding) 1차 방어 = 위 clamp. 도착 후 solid 침투가 남으면
        //   다음 틱부터 기존 물리(Physics.Step: 중력/충돌 resolve)가 K틱 내 non-solid로 수렴.
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
