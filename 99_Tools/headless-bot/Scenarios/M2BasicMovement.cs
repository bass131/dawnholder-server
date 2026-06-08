using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Dawnholder.Client.Net;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Tools.HeadlessBot.Scenarios;

// M2 회귀 안전망 — 1000 intent + 봇 자체 시뮬 + 종료 시 위치 desync 측정.
//
// **목적**: 서버 권위 결과(snapshot)와 봇 클라 권위
// 결정 시뮬(98_Shared/Physics.Step) 결과가 일정 범위 안에 있는지.
//
// **결정론 시퀀스** (5 phase × 200 tick = 1000 tick):
//   0 ~ 199 :  우 이동
//   200~399 :  좌 이동
//   400~599 :  우 이동 + 매 50tick 점프
//   600~799 :  좌 이동
//   800~999 :  정지 + 매 100tick 점프
//
// **봇 자체 시뮬과 서버의 차이가 작은 이유**:
//   - 양쪽 동일 Physics.Step (헌법 #4 + Y2 ADR-012 정합).
//   - 양쪽 동일 dt = Constants.TickDuration (50ms 고정).
//   - 환경 의존 차이: 봇 Task.Delay(50)의 OS scheduling 지터, 패킷 in-flight 시점, 서버
//     한 tick 안에 0/1/2 intent 처리될 수 있음 — 누적 desync는 환경마다 다름.
//   - 따라서 tolerance ε는 보수적 (px 단위, 캐릭터 한 크기 안).
public class M2BasicMovement
{
    public const int DefaultIntentCount = 1000;
    public const double DefaultTolerancePixels = 5.0;

    public class Result
    {
        public bool Success;
        public string Reason = "";
        public int IntentsSent;
        public int SnapshotsReceived;
        public double FinalDesyncX;
        public double FinalDesyncY;
        public Vector2 BotSimFinal;
        public Vector2 ServerFinal;
    }

    public static async Task<Result> Run(
        string host, int port,
        int intentCount = DefaultIntentCount,
        double tolerancePixels = DefaultTolerancePixels,
        CancellationToken ct = default)
    {
        Result result = new();

        ManualResetEventSlim connectedEv = new();
        ManualResetEventSlim handshakeResultEv = new();
        ManualResetEventSlim enterMapEv = new();
        ManualResetEventSlim disconnectedEv = new();

        Vector2 spawnPos = Vector2.Zero;
        S_Snapshot? lastSnapshot = null;
        int snapshotCount = 0;
        bool handshakeOk = false;
        string handshakeReason = "";
        BotSession? session = null;

        Connector connector = new();
        connector.Connect(
            new IPEndPoint(IPAddress.Parse(host), port),
            sessionFactory: () =>
            {
                BotSession s = new()
                {
                    OnConnectedCallback = _ => connectedEv.Set(),
                    OnDisconnectedCallback = _ => disconnectedEv.Set(),
                    OnPacketCallback = buffer =>
                    {
                        // [size:2][id:2][payload] — id는 byte 2~3.
                        ushort id = BinaryPrimitives.ReadUInt16LittleEndian(
                            buffer.AsSpan(2, 2));
                        switch (id)
                        {
                            case (ushort)PacketID.S_HandshakeResult:
                                S_HandshakeResult hr = new();
                                hr.Read(buffer);
                                handshakeOk = hr.ok;
                                handshakeReason = hr.reason;
                                handshakeResultEv.Set();
                                break;
                            case (ushort)PacketID.S_EnterMap:
                                S_EnterMap em = new();
                                em.Read(buffer);
                                spawnPos = new Vector2(em.spawnX, em.spawnY);
                                enterMapEv.Set();
                                break;
                            case (ushort)PacketID.S_Snapshot:
                                S_Snapshot sn = new();
                                sn.Read(buffer);
                                lastSnapshot = sn;
                                Interlocked.Increment(ref snapshotCount);
                                break;
                        }
                    }
                };
                session = s;
                return s;
            });

        if (!connectedEv.Wait(TimeSpan.FromSeconds(5)))
        {
            result.Reason = "connect timeout (5s)";
            return result;
        }

        // 헌법 #2: 첫 패킷 = 반드시 C_Handshake. 다른 거 먼저 보내면 서버가 Disconnect.
        C_Handshake handshake = new() { clientVersion = ProtocolVersion.Current };
        session?.Send(handshake.Write());

        if (!handshakeResultEv.Wait(TimeSpan.FromSeconds(5)))
        {
            result.Reason = "S_HandshakeResult timeout (5s)";
            session?.Disconnect();
            return result;
        }
        if (!handshakeOk)
        {
            result.Reason = $"handshake rejected: {handshakeReason}";
            session?.Disconnect();
            return result;
        }

        // handshake 후 C_CharacterSelect 의무 송신.
        // 서버가 class 선택 없이 월드 진입을 차단하므로 S_EnterMap은 이 패킷 후에야 옴.
        // stats는 CharacterSelect와 동일 출처 — 봇 시뮬 스탯이 선택 직업과 일치해야 desync 검증 유효.
        PlayerStats stats = PlayerStats.Knight();
        C_CharacterSelect charSelect = new() { characterClass = (byte)CharacterClass.Knight };
        session?.Send(charSelect.Write());

        if (!enterMapEv.Wait(TimeSpan.FromSeconds(5)))
        {
            result.Reason = "S_EnterMap timeout (5s) — CharacterSelect 후 서버 응답 없음?";
            session?.Disconnect();
            return result;
        }

        // 봇 자체 시뮬 초기화 (서버 spawn과 동일 시작점).
        // 초기 맵 = Town(0). terrain 로드 실패 시 fail loud (BotTerrainLoader 정책).
        MapTerrain? terrain = BotTerrainLoader.Load(mapId: 0);
        PhysicsState botState = new(spawnPos, Vector2.Zero, onGround: true);
        uint clientTick = 0;

        for (int i = 0; i < intentCount; i++)
        {
            ct.ThrowIfCancellationRequested();

            (sbyte inputX, bool jump) = GenerateInput(i);
            byte inputByte = InputBits.Encode(inputX, jump);
            clientTick++;

            C_MoveIntent pkt = new() { input = inputByte, clientTick = clientTick };
            session?.Send(pkt.Write());

            // 봇 측 자체 시뮬 (서버와 동일 입력·동일 dt·동일 terrain·동일 직업 스탯).
            // terrain 오버로드 사용 — 서버가 지형 경로를 타면 봇도 같은 경로. desync 검증 유효성 유지.
            PhysicsInput physInput = new(inputX, jump, Constants.TickDuration);
            botState = Physics.Step(botState, physInput, terrain, new MoveParams(stats.MoveSpeed, stats.JumpVel));

            await Task.Delay(Constants.TickIntervalMs, ct);
        }
        result.IntentsSent = intentCount;

        // 마지막 snapshot 받기 위해 추가 500ms 대기.
        await Task.Delay(500, ct);
        result.SnapshotsReceived = snapshotCount;

        if (lastSnapshot == null)
        {
            result.Reason = "no S_Snapshot received";
            session?.Disconnect();
            return result;
        }

        result.BotSimFinal = botState.Position;
        result.ServerFinal = new Vector2(lastSnapshot.x, lastSnapshot.y);
        result.FinalDesyncX = Math.Abs(result.ServerFinal.X - result.BotSimFinal.X);
        result.FinalDesyncY = Math.Abs(result.ServerFinal.Y - result.BotSimFinal.Y);
        result.Success =
            result.FinalDesyncX < tolerancePixels &&
            result.FinalDesyncY < tolerancePixels;
        if (!result.Success)
        {
            result.Reason =
                $"desync exceeded (tolerance={tolerancePixels}px): " +
                $"dx={result.FinalDesyncX:F2}, dy={result.FinalDesyncY:F2}, " +
                $"bot=({result.BotSimFinal.X:F2},{result.BotSimFinal.Y:F2}), " +
                $"server=({result.ServerFinal.X:F2},{result.ServerFinal.Y:F2})";
        }

        session?.Disconnect();
        disconnectedEv.Wait(TimeSpan.FromSeconds(2));

        return result;
    }

    // 결정론적 입력 시퀀스 (0-indexed tick → input).
    public static (sbyte inputX, bool jump) GenerateInput(int tickIndex)
    {
        return tickIndex switch
        {
            < 200 => ((sbyte)1, false),
            < 400 => ((sbyte)-1, false),
            < 600 => ((sbyte)1, tickIndex % 50 == 0),
            < 800 => ((sbyte)-1, false),
            _ => ((sbyte)0, tickIndex % 100 == 0),
        };
    }
}
