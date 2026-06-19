namespace Shared.GameData;

// 플레이어가 수행할 수 있는 행동 종류. 서버 ActionGate + 클라 입력 게이트가 공유하는 단일 진실.
// wire 무관 (직렬화 안 됨) — 패킷 형상 불변(§2 Protocol 무손상).
public enum ActionKind : byte
{
    Melee       = 0,
    Dash        = 1,
    Teleport    = 2,
    Thunderbolt = 3,
}

public static class ActionKindExtensions
{
    // SkillId byte → ActionKind 단일 진실. PlayerEntity + ActionRegistry 양쪽이 여기 위임.
    public static ActionKind? FromSkillId(byte skillId)
        => (SkillId)skillId switch
        {
            SkillId.Dash        => ActionKind.Dash,
            SkillId.Teleport    => ActionKind.Teleport,
            SkillId.Thunderbolt => ActionKind.Thunderbolt,
            _                   => null,
        };
}
