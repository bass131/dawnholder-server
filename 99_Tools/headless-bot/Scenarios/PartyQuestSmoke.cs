using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// 파티 + 공유 퀘스트 킬카운트 e2e 스모크.
//
// 검증 시나리오 (봇 근접전투 없음 — in-process 시드):
//   1. 봇A·봇B 둘 다 Town 진입.
//   2. 파티 결성(wire e2e): A가 B 초대 → B가 S_PartyInviteRecv 수신 후 수락
//      → 양측 S_PartyUpdate(partyId>0, 양 멤버 포함).
//   3. A만 Town→HG 이동. B는 Town 잔류 — cross-map 구도(A=HG, B=Town).
//      HG 스폰(x≈2)에 그대로 머묾 — 적(x=10)과 거리>4=aggro 범위 밖이라 전투 없음.
//   4. seedPartyKills 시드(xUnit 전용): A의 파티 공유 카운트를 2 적립.
//   5. 공유 카운트 검증(핵심 e2e): A(HG)·B(Town) 양측이
//      S_QuestUpdate(currentCount>=2, targetCount==40) 수신 — cross-map 전달 증명.
//   6. 해산(real e2e): B disconnect → A가 S_PartyUpdate(partyId==0) 수신.
//
// standalone vs xUnit:
//   seedPartyKills=null (Program.cs): 파티 결성 + cross-map 이동 + 해산까지만 검증.
//   seedPartyKills=Func (xUnit): 위 전체 + 공유카운트 전달 검증.
public class PartyQuestSmoke
{
    static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
    static readonly TimeSpan PartyTimeout = TimeSpan.FromSeconds(3);
    static readonly TimeSpan MapTransitionTimeout = TimeSpan.FromSeconds(5);
    static readonly TimeSpan QuestUpdateTimeout = TimeSpan.FromSeconds(5);
    static readonly TimeSpan DisbandTimeout = TimeSpan.FromSeconds(3);

    const int SeedKillCount = 2;
    const int ExpectedTargetCount = 40;

    // portal 좌표 상수 — 서버 PortalTable.cs와 정합.
    const float TownPortalX = 20f;
    const int TownPortalId = 1;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int EntityIdA;
        public int EntityIdB;
        public bool PartyFormed;
        public int SharedCountA;
        public int SharedCountB;
        public int TargetCount;
        public bool Disbanded;
    }

    /// <param name="seedPartyKills">
    /// 공유 카운트 시드 훅 (xUnit 전용). A의 entityId를 받아 파티 공유 KillCount를
    /// 2회 적립하고 완료를 알리는 Task를 반환한다.
    /// null이면 공유카운트 시드/검증 단계를 스킵하고 해산으로 진행(standalone 봇 런).
    /// </param>
    public static async Task<Result> Run(
        string host, int port,
        Func<int, Task>? seedPartyKills = null,
        CancellationToken ct = default)
    {
        Result result = new();
        PartyProbe probeA = new("A");
        PartyProbe probeB = new("B");

        try
        {
            // ── 0. 2봇 동시 Connect + 핸드셰이크 + 맵 진입 ─────────────────
            probeA.Connect(host, port);
            probeB.Connect(host, port);

            if (!probeA.WaitConnected(ConnectTimeout))
                return Fail(result, "probeA: connect timeout");
            if (!probeB.WaitConnected(ConnectTimeout))
                return Fail(result, "probeB: connect timeout");

            if (!probeA.WaitHandshake(ConnectTimeout))
                return Fail(result, "probeA: S_HandshakeResult timeout");
            if (!probeB.WaitHandshake(ConnectTimeout))
                return Fail(result, "probeB: S_HandshakeResult timeout");

            if (!probeA.HandshakeOk)
                return Fail(result, $"probeA: handshake rejected: {probeA.HandshakeReason}");
            if (!probeB.HandshakeOk)
                return Fail(result, $"probeB: handshake rejected: {probeB.HandshakeReason}");

            if (!probeA.WaitEnterMap(ConnectTimeout))
                return Fail(result, "probeA: S_EnterMap timeout");
            if (!probeB.WaitEnterMap(ConnectTimeout))
                return Fail(result, "probeB: S_EnterMap timeout");

            result.EntityIdA = probeA.LocalEntityId;
            result.EntityIdB = probeB.LocalEntityId;

            if (result.EntityIdA <= 0)
                return Fail(result, $"probeA: invalid entityId={result.EntityIdA}");
            if (result.EntityIdB <= 0)
                return Fail(result, $"probeB: invalid entityId={result.EntityIdB}");
            if (result.EntityIdA == result.EntityIdB)
                return Fail(result, $"entityId 충돌: A==B=={result.EntityIdA}");

            // ── 1. 파티 결성(wire e2e): A가 B를 초대 ────────────────────────
            probeA.SendPartyInvite(result.EntityIdB);

            bool gotInvite = await probeB.WaitForPartyInviteRecv(result.EntityIdA, PartyTimeout, ct);
            if (!gotInvite)
                return Fail(result, "probeB: S_PartyInviteRecv timeout or inviterEntityId mismatch");

            probeB.SendPartyRespond(result.EntityIdA, accept: 1);

            bool aGotParty = await probeA.WaitForPartyFormed(result.EntityIdA, result.EntityIdB, PartyTimeout, ct);
            if (!aGotParty)
                return Fail(result, "probeA: S_PartyUpdate(partyId>0) timeout");

            bool bGotParty = await probeB.WaitForPartyFormed(result.EntityIdA, result.EntityIdB, PartyTimeout, ct);
            if (!bGotParty)
                return Fail(result, "probeB: S_PartyUpdate(partyId>0) timeout");

            result.PartyFormed = true;

            // ── 2. A: Town → HuntingGround 이동 (B는 Town 잔류) ─────────────
            // HG 스폰(x≈2) 에 도착 후 이동 없음 — 적(x=10)과 거리>4=aggro 밖이라 전투 없음.
            await probeA.MoveToPortal(TownPortalX, ct);
            int expectedTransition = probeA.NextExpectedTransitionCount();
            probeA.SendEnterPortal(TownPortalId);

            bool aTransitioned = await probeA.WaitForMapTransition(expectedTransition, MapTransitionTimeout, ct);
            if (!aTransitioned)
                return Fail(result, "probeA: S_MapTransition timeout — Town→HuntingGround");

            // 서버 tick thread가 맵 진입 후속 처리를 완료하기까지 2틱 대기.
            await Task.Delay(Constants.TickIntervalMs * 2, ct);

            // ── 3. 시드(xUnit 전용): 파티 공유 킬카운트 2회 적립 ──────────────
            // seedPartyKills=null(standalone)이면 이 단계 스킵 → 해산으로 진행.
            if (seedPartyKills != null)
            {
                // 이 시점에 파티가 서버에 존재(S_PartyUpdate 수신 = 서버 파티 생성 완료).
                // OnKill(A.id)가 파티를 찾아 KillCount++ → 양 멤버에게 S_QuestUpdate 발송.
                await seedPartyKills(probeA.LocalEntityId);

                // ── 4. S_QuestUpdate 검증: A(HG)·B(Town) 양측 cross-map 수신 ─
                bool aGotQuest = await probeA.WaitForQuestCount(SeedKillCount, QuestUpdateTimeout, ct);
                if (!aGotQuest)
                    return Fail(result,
                        $"probeA(HG): S_QuestUpdate currentCount>={SeedKillCount} timeout");

                bool bGotQuest = await probeB.WaitForQuestCount(SeedKillCount, QuestUpdateTimeout, ct);
                if (!bGotQuest)
                    return Fail(result,
                        $"probeB(Town): S_QuestUpdate currentCount>={SeedKillCount} timeout — cross-map 전달 결함");

                result.SharedCountA = probeA.LastQuestCurrentCount;
                result.SharedCountB = probeB.LastQuestCurrentCount;
                result.TargetCount = probeA.LastQuestTargetCount;

                if (result.TargetCount != ExpectedTargetCount)
                    return Fail(result,
                        $"S_QuestUpdate.targetCount expected={ExpectedTargetCount}, actual={result.TargetCount}");
            }

            // ── 5. 해산(real e2e): B disconnect → A가 S_PartyUpdate(partyId==0) 수신 ──
            probeB.Disconnect();

            bool aGotDisband = await probeA.WaitForPartyDisband(DisbandTimeout, ct);
            if (!aGotDisband)
                return Fail(result, "probeA: S_PartyUpdate(partyId==0) 해산 통보 timeout");

            result.Disbanded = true;
            result.Success = true;
            return result;
        }
        finally
        {
            probeA.Disconnect();
            probeB.Disconnect();
        }
    }

    static Result Fail(Result result, string reason)
    {
        result.Success = false;
        result.Reason = reason;
        return result;
    }

    sealed class PartyProbe
    {
        readonly string _label;
        readonly object _gate = new();
        readonly Connector _connector = new();
        readonly ManualResetEventSlim _connected = new(false);
        readonly ManualResetEventSlim _handshake = new(false);
        readonly ManualResetEventSlim _enterMap = new(false);

        // 파티
        volatile bool _gotInviteRecv = false;
        int _lastInviterEntityId = -1;
        S_PartyUpdate? _lastPartyUpdate;

        // 퀘스트
        volatile int _lastQuestCurrentCount = 0;
        volatile int _lastQuestTargetCount = 0;

        // 맵 전환
        volatile int _mapTransitionCount = 0;

        // 서버 권위 위치 추적 (MoveToPortal용)
        volatile float _serverX = 0f;

        BotSession? _session;
        uint _clientTick;

        public bool HandshakeOk { get; private set; }
        public string HandshakeReason { get; private set; } = "";
        public int LocalEntityId { get; private set; } = -1;

        public int LastQuestCurrentCount => _lastQuestCurrentCount;
        public int LastQuestTargetCount => _lastQuestTargetCount;

        public PartyProbe(string label) => _label = label;

        public void Connect(string host, int port)
        {
            _connector.Connect(
                new IPEndPoint(IPAddress.Parse(host), port),
                sessionFactory: () =>
                {
                    BotSession s = new();
                    s.OnConnectedCallback = _ =>
                    {
                        _connected.Set();
                        C_Handshake handshake = new() { clientVersion = ProtocolVersion.Current };
                        s.Send(handshake.Write());
                    };
                    s.OnDisconnectedCallback = _ => { };
                    s.OnPacketCallback = HandlePacket;
                    _session = s;
                    return s;
                });
        }

        public bool WaitConnected(TimeSpan timeout) => _connected.Wait(timeout);
        public bool WaitHandshake(TimeSpan timeout) => _handshake.Wait(timeout);
        public bool WaitEnterMap(TimeSpan timeout) => _enterMap.Wait(timeout);

        // ── 파티 송수신 ──────────────────────────────────────────────────────

        public void SendPartyInvite(int targetEntityId)
        {
            C_PartyInvite pkt = new() { targetEntityId = targetEntityId };
            _session?.Send(pkt.Write());
        }

        public void SendPartyRespond(int inviterEntityId, byte accept)
        {
            C_PartyRespond pkt = new() { inviterEntityId = inviterEntityId, accept = accept };
            _session?.Send(pkt.Write());
        }

        public async Task<bool> WaitForPartyInviteRecv(int expectedInviterEntityId, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(
                () => _gotInviteRecv && _lastInviterEntityId == expectedInviterEntityId,
                timeout, ct);

        public async Task<bool> WaitForPartyFormed(int entityIdA, int entityIdB, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() =>
            {
                lock (_gate)
                {
                    if (_lastPartyUpdate == null) return false;
                    if (_lastPartyUpdate.partyId <= 0) return false;
                    bool hasA = _lastPartyUpdate.member0EntityId == entityIdA
                                || _lastPartyUpdate.member1EntityId == entityIdA;
                    bool hasB = _lastPartyUpdate.member0EntityId == entityIdB
                                || _lastPartyUpdate.member1EntityId == entityIdB;
                    return hasA && hasB;
                }
            }, timeout, ct);

        public async Task<bool> WaitForPartyDisband(TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() =>
            {
                lock (_gate)
                    return _lastPartyUpdate != null && _lastPartyUpdate.partyId == 0;
            }, timeout, ct);

        public async Task<bool> WaitForQuestCount(int minCount, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _lastQuestCurrentCount >= minCount, timeout, ct);

        // ── 이동 + 포탈 ──────────────────────────────────────────────────────

        public int NextExpectedTransitionCount() => _mapTransitionCount + 1;

        public async Task<bool> WaitForMapTransition(int expectedCount, TimeSpan timeout, CancellationToken ct)
            => await WaitUntil(() => _mapTransitionCount >= expectedCount, timeout, ct);

        public async Task MoveToPortal(float portalX, CancellationToken ct)
        {
            int ticks = 0;
            const int maxTicks = 400;
            const float reachRadius = 0.5f;

            while (true)
            {
                float sx = _serverX;
                if (Math.Abs(sx - portalX) <= reachRadius) break;
                if (ticks >= maxTicks)
                    throw new TimeoutException(
                        $"[{_label}] MoveToPortal: {maxTicks}틱 내 미도달. portalX={portalX}, serverX={sx}");

                sbyte dir = sx < portalX ? (sbyte)1 : (sbyte)-1;
                SendMove(dir);
                await Task.Delay(Constants.TickIntervalMs, ct);
                ticks++;
            }
            SendMove(0);
            await Task.Delay(150, ct);
        }

        public void SendEnterPortal(int portalId)
        {
            C_EnterPortal pkt = new() { portalId = portalId };
            _session?.Send(pkt.Write());
        }

        public void Disconnect() => _session?.Disconnect();

        // ── 내부 ─────────────────────────────────────────────────────────────

        void SendMove(sbyte inputX)
        {
            _clientTick++;
            C_MoveIntent move = new()
            {
                input = InputBits.Encode(inputX, jumpPressed: false),
                clientTick = _clientTick,
            };
            _session?.Send(move.Write());
        }

        void HandlePacket(ArraySegment<byte> buffer)
        {
            if (buffer.Count < 4) return;

            ushort id = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(2, 2));
            switch ((PacketID)id)
            {
                case PacketID.S_HandshakeResult:
                    S_HandshakeResult handshake = new();
                    handshake.Read(buffer);
                    HandshakeOk = handshake.ok;
                    HandshakeReason = handshake.reason;
                    if (handshake.ok)
                    {
                        C_CharacterSelect charSelect = new() { characterClass = (byte)CharacterClass.Knight };
                        _session?.Send(charSelect.Write());
                    }
                    _handshake.Set();
                    break;

                case PacketID.S_EnterMap:
                    S_EnterMap enterMap = new();
                    enterMap.Read(buffer);
                    LocalEntityId = enterMap.entityId;
                    _serverX = enterMap.spawnX;
                    if (!_enterMap.IsSet) _enterMap.Set();
                    break;

                case PacketID.S_MapTransition:
                    S_MapTransition transition = new();
                    transition.Read(buffer);
                    _serverX = transition.spawnX;
                    System.Threading.Interlocked.Increment(ref _mapTransitionCount);
                    break;

                case PacketID.S_Snapshot:
                    S_Snapshot snapshot = new();
                    snapshot.Read(buffer);
                    if (snapshot.entityId == LocalEntityId)
                        _serverX = snapshot.x;
                    break;

                case PacketID.S_PartyInviteRecv:
                    S_PartyInviteRecv inviteRecv = new();
                    inviteRecv.Read(buffer);
                    lock (_gate)
                    {
                        _lastInviterEntityId = inviteRecv.inviterEntityId;
                        _gotInviteRecv = true;
                    }
                    break;

                case PacketID.S_PartyUpdate:
                    S_PartyUpdate partyUpdate = new();
                    partyUpdate.Read(buffer);
                    lock (_gate) _lastPartyUpdate = partyUpdate;
                    break;

                case PacketID.S_QuestUpdate:
                    S_QuestUpdate questUpdate = new();
                    questUpdate.Read(buffer);
                    _lastQuestCurrentCount = questUpdate.currentCount;
                    _lastQuestTargetCount = questUpdate.targetCount;
                    break;
            }
        }

        static async Task<bool> WaitUntil(Func<bool> predicate, TimeSpan timeout, CancellationToken ct)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (predicate()) return true;
                await Task.Delay(25, ct);
            }
            return predicate();
        }
    }
}
