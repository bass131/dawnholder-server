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

    public bool Execute(GameMap map, PlayerEntity caster, long clientTick)
    {
        float rawDestX = caster.Position.X + CombatConstants.TeleportDistance * caster.FacingDir;

        (float boundsMin, float boundsMax) = map.MapBoundsX;
        float destX = MathF.Max(boundsMin, MathF.Min(boundsMax, rawDestX));

        caster.Position = new Vector2(destX, caster.Position.Y);
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
