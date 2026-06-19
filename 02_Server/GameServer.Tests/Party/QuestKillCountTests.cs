using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Party;
using Dawnholder.Server.GameServer.Quest;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Party;

// 퀘스트 Q2 킬카운트 적립 + S_QuestUpdate 송신 테스트.
//
// 검증 범위:
//   1. 파티원이 킬 → PartyState.KillCount 누적 + 멤버 전원에게 S_QuestUpdate 송신
//   2. 솔로 킬 → GetSoloProgress 증가 + 본인에게 S_QuestUpdate 송신
//   3. ResetAllQuestProgress → 파티 KillCount 0 + 솔로 progress 0
//   4. targetCount = QuestConstants.BossUnlockKillCount(20) SSOT 검증
//   5. (통합) GameMap.HandleEnemyDeath → OnEnemyKilled → Quest.EnqueueJob 드레인 → OnKill 적립
//
// 싱글톤 관리: GameWorld 단일 인스턴스 → [Collection] 직렬화 + IDisposable.
[Collection("QuestKillCountTests")]
public class QuestKillCountTests : IDisposable
{
    readonly GameWorld _world;
    readonly GameMap _huntingGround;

    public QuestKillCountTests()
    {
        _world = new GameWorld(new Dictionary<MapId, (MapTerrain?, MapContent?)>());
        _huntingGround = _world.GetMap(MapId.HuntingGround)!;
    }

    public void Dispose() => _world.Stop();

    // ── 1. 파티원 킬 → KillCount 누적 + 멤버 전원 S_QuestUpdate ───────────────

    [Fact]
    public void PartyKill_Increments_SharedKillCount_And_NotifiesAllMembers()
    {
        TrackingSession sessionA = new TrackingSession();
        TrackingSession sessionB = new TrackingSession();

        int entityA = _world.NextEntityId();
        int entityB = _world.NextEntityId();

        _huntingGround.AddPlayerWithId(entityA, sessionA, Vector2.Zero, PlayerStats.Knight(), currentHp: 100);
        _huntingGround.AddPlayerWithId(entityB, sessionB, Vector2.Zero, PlayerStats.Knight(), currentHp: 100);

        // 파티 결성 (tick thread 직접 호출)
        _world.Party.CreateParty(entityA, entityB);

        // entityA가 Normal 적을 킬
        _world.Quest.OnKill(entityA, _world);

        Assert.Equal(1, _world.Party.GetPartyByEntity(entityA)!.KillCount);

        // SendToEntity가 EnqueueJob 경유 → 맵 Tick 드레인 필요
        _huntingGround.Tick(tickNumber: 1);

        S_QuestUpdate? pktA = ExtractQuestUpdate(sessionA);
        S_QuestUpdate? pktB = ExtractQuestUpdate(sessionB);

        Assert.NotNull(pktA);
        Assert.NotNull(pktB);
        Assert.Equal(1, pktA!.currentCount);
        Assert.Equal(QuestConstants.BossUnlockKillCount, pktA.targetCount);
        Assert.Equal(1, pktB!.currentCount);
        Assert.Equal(QuestConstants.BossUnlockKillCount, pktB.targetCount);
    }

    [Fact]
    public void PartyKill_BothMembersKill_AccumulatesSharedCount()
    {
        TrackingSession sessionA = new TrackingSession();
        TrackingSession sessionB = new TrackingSession();

        int entityA = _world.NextEntityId();
        int entityB = _world.NextEntityId();

        _huntingGround.AddPlayerWithId(entityA, sessionA, Vector2.Zero, PlayerStats.Knight(), currentHp: 100);
        _huntingGround.AddPlayerWithId(entityB, sessionB, Vector2.Zero, PlayerStats.Knight(), currentHp: 100);

        _world.Party.CreateParty(entityA, entityB);

        _world.Quest.OnKill(entityA, _world);
        _world.Quest.OnKill(entityB, _world);
        _world.Quest.OnKill(entityA, _world);

        Assert.Equal(3, _world.Party.GetPartyByEntity(entityA)!.KillCount);

        _huntingGround.Tick(tickNumber: 1);

        // 마지막 패킷 = currentCount=3
        S_QuestUpdate? lastA = ExtractLastQuestUpdate(sessionA);
        Assert.NotNull(lastA);
        Assert.Equal(3, lastA!.currentCount);
    }

    // ── 2. 솔로 킬 → GetSoloProgress 증가 + 본인에게 S_QuestUpdate ─────────────

    [Fact]
    public void SoloKill_IncreasesSoloProgress_And_NotifiesSelf()
    {
        TrackingSession session = new TrackingSession();
        int entityId = _world.NextEntityId();
        _huntingGround.AddPlayerWithId(entityId, session, Vector2.Zero, PlayerStats.Knight(), currentHp: 100);

        // 파티 없는 솔로
        _world.Quest.OnKill(entityId, _world);

        Assert.Equal(1, _world.Quest.GetSoloProgress(entityId));

        _huntingGround.Tick(tickNumber: 1);

        S_QuestUpdate? pkt = ExtractQuestUpdate(session);
        Assert.NotNull(pkt);
        Assert.Equal(1, pkt!.currentCount);
        Assert.Equal(QuestConstants.BossUnlockKillCount, pkt.targetCount);
    }

    [Fact]
    public void SoloKill_MultipleKills_Accumulates()
    {
        int entityId = _world.NextEntityId();
        _huntingGround.AddPlayerWithId(entityId, null, Vector2.Zero, PlayerStats.Knight(), currentHp: 100);

        _world.Quest.OnKill(entityId, _world);
        _world.Quest.OnKill(entityId, _world);
        _world.Quest.OnKill(entityId, _world);

        Assert.Equal(3, _world.Quest.GetSoloProgress(entityId));
    }

    // ── 3. ResetAllQuestProgress → 공유·솔로 둘 다 0 ─────────────────────────

    [Fact]
    public void ResetAllQuestProgress_ClearsPartyKillCount_And_SoloProgress()
    {
        int entityA = _world.NextEntityId();
        int entityB = _world.NextEntityId();
        int solo = _world.NextEntityId();

        _huntingGround.AddPlayerWithId(entityA, null, Vector2.Zero, PlayerStats.Knight(), currentHp: 100);
        _huntingGround.AddPlayerWithId(entityB, null, Vector2.Zero, PlayerStats.Knight(), currentHp: 100);
        _huntingGround.AddPlayerWithId(solo, null, Vector2.Zero, PlayerStats.Knight(), currentHp: 100);

        _world.Party.CreateParty(entityA, entityB);
        _world.Quest.OnKill(entityA, _world);
        _world.Quest.OnKill(entityB, _world);
        _world.Quest.OnKill(solo, _world);

        Assert.Equal(2, _world.Party.GetPartyByEntity(entityA)!.KillCount);
        Assert.Equal(1, _world.Quest.GetSoloProgress(solo));

        _world.Quest.ResetAllQuestProgress();

        Assert.Equal(0, _world.Party.GetPartyByEntity(entityA)!.KillCount);
        Assert.Equal(0, _world.Quest.GetSoloProgress(solo));
    }

    // ── 3b. 영구 해금: 임계 달성 후 리셋(보스 킬)에도 게이트 통과 유지(재그라인드 X) ──────

    [Fact]
    public void BossUnlock_Persists_AfterReset_NoRegrind()
    {
        int solo = _world.NextEntityId();

        // 솔로로 임계(20)까지 킬 → 영구 해금.
        for (int i = 0; i < QuestConstants.BossUnlockKillCount; i++)
            _world.Quest.OnKill(solo, _world);

        Assert.True(_world.Quest.IsBossUnlocked(solo));
        Assert.Equal(QuestConstants.BossUnlockKillCount, _world.Quest.GetKillCount(solo));

        // 보스 킬 = ResetAllQuestProgress → raw progress는 0이 되지만 해금 latch는 유지.
        _world.Quest.ResetAllQuestProgress();

        Assert.Equal(0, _world.Quest.GetSoloProgress(solo));                                // raw 카운트 리셋
        Assert.True(_world.Quest.IsBossUnlocked(solo));                                     // 해금 유지
        Assert.Equal(QuestConstants.BossUnlockKillCount, _world.Quest.GetKillCount(solo));  // 게이트 통과 유지(재그라인드 없음)
    }

    // ── 4. targetCount SSOT 검증 ─────────────────────────────────────────────

    [Fact]
    public void QuestUpdate_TargetCount_MatchesServerConstant()
    {
        TrackingSession session = new TrackingSession();
        int entityId = _world.NextEntityId();
        _huntingGround.AddPlayerWithId(entityId, session, Vector2.Zero, PlayerStats.Knight(), currentHp: 100);

        _world.Quest.OnKill(entityId, _world);
        _huntingGround.Tick(tickNumber: 1);

        S_QuestUpdate? pkt = ExtractQuestUpdate(session);
        Assert.NotNull(pkt);
        // targetCount는 서버 QuestConstants.BossUnlockKillCount에서 옴 — 클라 하드코딩 X (SSOT=서버 상수)
        Assert.Equal(QuestConstants.BossUnlockKillCount, pkt!.targetCount);
    }

    // ── 5. 통합: HandleEnemyDeath → OnEnemyKilled → Quest.EnqueueJob 드레인 → OnKill ─

    [Fact]
    public void HandleEnemyDeath_Normal_TriggersOnKill_ViaEnqueueJob()
    {
        TrackingSession session = new TrackingSession();
        int killerId = _world.NextEntityId();
        _huntingGround.AddPlayerWithId(killerId, session, Vector2.Zero, PlayerStats.Knight(), currentHp: 100);

        // 테스트용 Normal 적 직접 spawn (internal — 같은 어셈블리)
        EnemyEntity enemy = _huntingGround.SpawnEnemy(EnemyKind.Normal, x: 0f, y: 0f, maxHp: 10);

        // HandleEnemyDeath는 내부 → SpawnEnemyForTest로 접근
        _huntingGround.HandleEnemyDeath(enemy, killerId);

        // OnEnemyKilled가 _quest.EnqueueJob을 push — Quest.Tick으로 드레인
        _world.Quest.Tick(currentTick: 1);

        Assert.Equal(1, _world.Quest.GetSoloProgress(killerId));

        // SendToEntity도 맵 EnqueueJob → 맵 Tick 드레인
        _huntingGround.Tick(tickNumber: 2);

        S_QuestUpdate? pkt = ExtractQuestUpdate(session);
        Assert.NotNull(pkt);
        Assert.Equal(1, pkt!.currentCount);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    static S_QuestUpdate? ExtractQuestUpdate(TrackingSession session)
    {
        foreach (byte[] raw in session.SentPayloads)
        {
            if (raw.Length < 4) continue;
            ushort id = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(2, 2));
            if (id == (ushort)PacketID.S_QuestUpdate)
            {
                S_QuestUpdate pkt = new S_QuestUpdate();
                pkt.Read(new ArraySegment<byte>(raw));
                return pkt;
            }
        }
        return null;
    }

    static S_QuestUpdate? ExtractLastQuestUpdate(TrackingSession session)
    {
        S_QuestUpdate? last = null;
        foreach (byte[] raw in session.SentPayloads)
        {
            if (raw.Length < 4) continue;
            ushort id = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(2, 2));
            if (id == (ushort)PacketID.S_QuestUpdate)
            {
                S_QuestUpdate pkt = new S_QuestUpdate();
                pkt.Read(new ArraySegment<byte>(raw));
                last = pkt;
            }
        }
        return last;
    }

    // Send 호출을 캡처하는 fake GameSession — GameWorldPartyIntegrationTests와 동일 패턴.
    class TrackingSession : GameSession
    {
        public List<byte[]> SentPayloads { get; } = new();

        public override void Send(ArraySegment<byte> payload)
        {
            byte[] copy = new byte[payload.Count];
            Buffer.BlockCopy(payload.Array!, payload.Offset, copy, 0, payload.Count);
            SentPayloads.Add(copy);
        }

        public override void OnSend(int numOfBytes) { }
        public override void Disconnect() { }
    }
}

[CollectionDefinition("QuestKillCountTests", DisableParallelization = true)]
public class QuestKillCountTestsCollection { }
