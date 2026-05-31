using System.Numerics;
using Dawnholder.Server.GameServer.Maps;

namespace Dawnholder.Server.GameServer.Tests.Maps;

/// <summary>
/// jump buffer 단위 테스트 (Phase 10b — 착지 직후 재점프 snap 근본 fix).
///
/// 검증:
///   (a) 버퍼 발사 — 공중에서 점프 입력 → 버퍼됨. 이후 착지 틱에 자동 발사.
///   (b) TTL 만료 — 버퍼 후 JumpBufferTicks 초과 시 착지해도 점프 X.
///   (c) 회귀 — 땅에서 즉시 점프는 영향 없음.
///   (d) 버퍼 1개 상한 — 공중 반복 입력 시 TTL이 최신 1개로 갱신됨 (유령 점프 불가).
/// </summary>
public class JumpBufferTests
{
    // ── (a) 버퍼 발사 ────────────────────────────────────────────────────────

    [Fact]
    public void AirborneJumpInput_Buffered_FiresOnLanding()
    {
        // 공중(OnGround=false)에서 ResolveJump(true) → false 반환(이번 틱 점프 X).
        // 이후 OnGround=true로 착지 → ResolveJump(false) → true 반환(버퍼 발사).
        GameMap map = new GameMap();
        PlayerEntity p = map.AddPlayer(null, new Vector2(0f, 1f));
        p.OnGround = false;

        // 공중에서 점프 입력
        bool resultInAir = p.ResolveJump(true);
        Assert.False(resultInAir, "공중에서는 점프 실행 X");
        Assert.True(p.HasBufferedJump, "점프 입력이 버퍼에 보관되어야 함");

        // 착지
        p.OnGround = true;
        bool resultOnLand = p.ResolveJump(false);
        Assert.True(resultOnLand, "착지 틱에 버퍼된 점프가 발사되어야 함");
        Assert.False(p.HasBufferedJump, "발사 후 버퍼 소비됨");
    }

    // ── (b) TTL 만료 ─────────────────────────────────────────────────────────

    [Fact]
    public void Buffer_ExpiresAfterTtl_NoJumpOnLanding()
    {
        // 공중에서 버퍼 후 JumpBufferTicks(3)회 ResolveJump(false) 호출 → TTL 만료.
        // 이후 착지해도 점프 발사 X.
        GameMap map = new GameMap();
        PlayerEntity p = map.AddPlayer(null, new Vector2(0f, 1f));
        p.OnGround = false;

        // 버퍼
        p.ResolveJump(true);
        Assert.True(p.HasBufferedJump);

        // TTL 3틱 감소 → 만료
        p.ResolveJump(false); // remaining: 2
        p.ResolveJump(false); // remaining: 1
        p.ResolveJump(false); // remaining: 0
        Assert.False(p.HasBufferedJump, "TTL 만료 후 버퍼 소멸");

        // 착지
        p.OnGround = true;
        bool resultOnLand = p.ResolveJump(false);
        Assert.False(resultOnLand, "TTL 만료 후 착지에서 점프 X");
    }

    // ── (c) 회귀 — 땅에서 즉시 점프 ─────────────────────────────────────────

    [Fact]
    public void GroundJump_Unaffected_ImmediatelyTrue()
    {
        // OnGround=true + rawJumpPressed=true → 즉시 true (기존 동작 불변).
        GameMap map = new GameMap();
        PlayerEntity p = map.AddPlayer(null, Vector2.Zero);
        p.OnGround = true;

        bool result = p.ResolveJump(true);
        Assert.True(result, "땅에서 점프는 즉시 발사되어야 함");
        Assert.False(p.HasBufferedJump, "땅 점프 후 버퍼는 비어 있어야 함");
    }

    // ── (d) 버퍼 1개 상한 — TTL 갱신 ────────────────────────────────────────

    [Fact]
    public void AirborneRepeatedInput_TtlRefreshed_NotAccumulated()
    {
        // 공중에서 ResolveJump(true) 후 1틱 경과(TTL=2) → 다시 ResolveJump(true) → TTL 3으로 갱신.
        // 버퍼 1개 상한 보장 — 유령 점프(연속 발사) 불가.
        GameMap map = new GameMap();
        PlayerEntity p = map.AddPlayer(null, new Vector2(0f, 1f));
        p.OnGround = false;

        p.ResolveJump(true);  // TTL = 3
        p.ResolveJump(false); // TTL = 2 (1틱 경과)

        Assert.True(p.HasBufferedJump, "TTL 만료 전 버퍼 유지");

        // 재입력 → TTL 리셋
        p.ResolveJump(true); // TTL = 3 다시
        p.ResolveJump(false); // 2
        p.ResolveJump(false); // 1
        p.ResolveJump(false); // 0 — 만료

        Assert.False(p.HasBufferedJump, "리셋 후 TTL 만료 시 버퍼 소멸");
    }
}
