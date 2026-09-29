using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Net;
using Dawnholder.Client.State;
using Dawnholder.Client.Network.Handlers;
using Dawnholder.Client.Network.Handlers.Combat;
using Dawnholder.Client.Network.Handlers.Party;
using Dawnholder.Client.Network.Handlers.Quest;
using Dawnholder.Client.Network.Handlers.Roster;
using Dawnholder.Client.Network.Handlers.Session;
using Dawnholder.Client.Network.Handlers.Skill;
using Dawnholder.Client.Network.Handlers.Sync;
using Dawnholder.Client.Network.Handlers.Zone;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    /// <summary>
    /// ClientNet의 <see cref="PacketSession"/>을 Unity 컨텍스트로 wrap.
    /// framing 자동, OnRecvPacket은 *완전한 한 패킷*. 패킷 dispatch는 IClientPacketHandler 테이블.
    /// 컨테이너는 framing + dispatch + main-thread 마샬링만 담당.
    ///
    /// transport 콜백은 스레드 보장이 없으므로 Unity 적용은 세션 gate가 있는 main queue 경유.
    /// </summary>
    public class UnityClientSession : PacketSession, IClientConnectionSession
    {
        // LocalPlayerMovement가 매 frame C_MoveIntent를 Send하려면 정적 접근점 필요.
        // Main-thread owner가 transport 시작과 최초 handshake 송신 후 게시.
        public static UnityClientSession Instance { get; private set; }
        readonly Func<bool> _isCurrent;
        readonly Action<Action> _post;
        readonly Action<Action, float> _postDelayed;
        int _closed;
        bool _cleaned;
        public bool IsClosed => Volatile.Read(ref _closed) != 0 || IsDisconnected;
        bool CanApply => !IsClosed && _isCurrent();
        public event Action Closed;
        public event Action HandshakeSucceeded
        {
            add => OnHandshakeOkEvent += value;
            remove => OnHandshakeOkEvent -= value;
        }

        // 서버 handshake 응답 전 intent를 보내지 않는다. main-thread handler만 갱신한다.
        // 최초 handshake 송신 자체는 owner가 Instance 게시 전에 마친다.
        public bool HandshakeOk { get; private set; }

        // handshake OK event. NetworkService가 등록 후 S_HandshakeResult(ok=true) 수신 시 main thread에서
        // 호출됨. C_CharacterSelect 송신 race 봉합 핵심. 구독자 없어도 null check로 안전.
        public event Action OnHandshakeOkEvent;

        // 본인 entityId. EnterMapHandler에서 박음 (main thread).
        // SnapshotHandler가 entityId 비교로 본인/타인 분기. null이면 (EnterMap 도착 전 Snapshot race)
        // 해당 Snapshot drop — 다음 Snapshot에서 정상화.
        public int? LocalEntityId { get; private set; }

        // 마지막으로 수신한 S_Snapshot의 serverTick. C_Attack 송신 시 attackerClientTick 필드에 박아
        // 서버 rewind 기준점을 제공. 초기값 0 — 첫 Snapshot 전 공격은 서버가
        // silent drop(검증 규칙: currentServerTick - attackerClientTick > 4)하므로 실전 영향 없음.
        // 본인/타인 Snapshot 모두 갱신 (어느 것이든 서버 현재 tick을 표현하므로 기준점으로 유효).
        public int LastReceivedServerTick { get; private set; }

        // roster buffer. 컨테이너는 버퍼 인스턴스만 보유, 로직은 RosterTransitionBuffer 안에 있음.
        // internal: ClientPacketHandlers.cs(같은 어셈블리)에서 직접 접근.
        internal RosterTransitionBuffer RosterBuffer { get; }

        // ========================================================================
        // dispatch 테이블 (IClientPacketHandler 미러).
        // 새 패킷 추가 = 핸들러 1개 신설 + 여기 1줄 등록만.
        // ========================================================================

        static readonly IReadOnlyDictionary<PacketID, IClientPacketHandler> _handlers =
            new Dictionary<PacketID, IClientPacketHandler>
            {
                { PacketID.S_HandshakeResult,   new HandshakeResultHandler() },
                { PacketID.S_Pong,              new PongHandler() },
                { PacketID.S_EnterMap,          new EnterMapHandler() },
                { PacketID.S_Snapshot,          new SnapshotHandler() },
                { PacketID.S_PlayerJoin,        new PlayerJoinHandler() },
                { PacketID.S_PlayerLeave,       new PlayerLeaveHandler() },
                { PacketID.S_EntitySpawn,       new EntitySpawnHandler() },
                { PacketID.S_HitResult,         new HitResultHandler() },
                { PacketID.S_EntityDeath,       new EntityDeathHandler() },
                { PacketID.S_StageClear,        new StageClearHandler() },
                { PacketID.S_MapTransition,     new MapTransitionHandler() },
                { PacketID.S_EntityState,       new EntityStateHandler() },
                { PacketID.S_EnemyAttack,       new EnemyAttackHandler() },
                { PacketID.S_PlayerHp,          new PlayerHpHandler() },
                { PacketID.S_PlayerAttack,      new PlayerAttackHandler() },
                { PacketID.S_ProjectileLaunch,  new ProjectileLaunchHandler() },
                { PacketID.S_SkillCast,         new SkillCastHandler() },
                { PacketID.S_PartyInviteRecv,   new PartyInviteRecvHandler() },
                { PacketID.S_PartyUpdate,        new PartyUpdateHandler() },
                { PacketID.S_PartyError,         new PartyErrorHandler() },
                { PacketID.S_PortalLocked,       new PortalLockedHandler() },
                { PacketID.S_QuestUpdate,        new QuestUpdateHandler() },
            };

        // Editor only 송신 latency 시뮬레이션.
        //   0이면 직통 (Release/일반 Play 동작).
        //   >0이면 SendIntent 경로에 한해 N ms 지연 후 실제 Send.
        //   값 변경은 코드 수정 후 Play 재시작 (Inspector 노출은 미래 옵션).
#if UNITY_EDITOR
        public static int SimulatedLatencyMs = 0;
#endif

        public UnityClientSession(Func<bool> isCurrent = null, Action<Action> post = null,
            Action<Action, float> postDelayed = null)
        {
            _isCurrent = isCurrent ?? (() => ReferenceEquals(Instance, this));
            _post = post ?? MainThreadDispatcher.Enqueue;
            _postDelayed = postDelayed ?? MainThreadDispatcher.EnqueueDelayed;
            RosterBuffer = new RosterTransitionBuffer(() => CanApply);
        }

        public void Activate(Socket socket)
        {
            EndPoint endPoint = socket.RemoteEndPoint;
            Start(socket);
            if (!IsClosed) OnConnected(endPoint);
        }

        public void Publish()
        {
            if (!IsClosed) Instance = this;
        }

        public void SendCharacterSelect(byte characterClass)
        {
            if (!CanApply || !HandshakeOk) return;
            Send(new C_CharacterSelect { characterClass = characterClass }.Write());
        }

        /// <summary>Check at execution time: queued work may outlive its connection.</summary>
        public void EnqueueApply(Action action) => _post(() =>
        {
            if (CanApply) action();
        });

        // Main-thread cleanup of this session only. The owner resets shared mirrors.
        public void Cleanup()
        {
            if (_cleaned) return;
            _cleaned = true;
            Interlocked.Exchange(ref _closed, 1);
            HandshakeOk = false;
            LocalEntityId = null;
            LastReceivedServerTick = 0;
            RosterBuffer.Teardown();
            OnHandshakeOkEvent = null;
            Closed = null;
            if (ReferenceEquals(Instance, this)) Instance = null;
        }

        /// <summary>
        /// 입력 intent 송신용 wrapper. Editor에선 SimulatedLatencyMs 적용.
        /// Release/일반 Play에선 Send 직통 — 컴파일 시 분기 사라짐(<c>#if UNITY_EDITOR</c>).
        ///
        /// LocalPlayerMovement가 C_MoveIntent를 이 경로로 보냄.
        /// 다른 패킷(Ping 등)은 그대로 Send 직통 — RTT 측정 시 latency 영향 분리 가능.
        /// </summary>
        public void SendIntent(ArraySegment<byte> buf)
        {
            // handshake 통과 전 송신은 drop (헌법 #2 first-packet). 정상 흐름에선 C_Handshake →
            // S_HandshakeResult OK가 첫 Update tick 안에 박혀 영향 X. race window에서만 발동.
            if (!HandshakeOk || !CanApply)
            {
                // 폭주 차단 위해 main thread에서 한 줄만. 정상 흐름엔 거의 0회 박힘.
                return;
            }
#if UNITY_EDITOR
            if (SimulatedLatencyMs > 0)
            {
                // buf는 GenPackets.Write()가 매번 새로 할당한 byte[]라 큐 보존 안전(corruption X).
                ArraySegment<byte> captured = buf;
                _postDelayed(() =>
                {
                    if (CanApply && HandshakeOk) Send(captured);
                }, SimulatedLatencyMs / 1000f);
                return;
            }
#endif
            Send(buf);
        }

        public override void OnConnected(EndPoint endPoint)
        {
            EndPoint ep = endPoint;
            EnqueueApply(() => Debug.Log($"[Unity] OnConnected to {ep}"));

            // 최초 송신을 마친 뒤에만 owner가 Instance/Connected를 외부에 공개한다.
            C_Handshake handshake = new C_Handshake { clientVersion = ProtocolVersion.Current };
            Send(handshake.Write());
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            Interlocked.Exchange(ref _closed, 1);
            EndPoint ep = endPoint;
            // Mandatory close notification bypasses the ordinary packet gate.
            try { Closed?.Invoke(); }
            finally
            {
                _post(() =>
                {
                    Cleanup();
                    Debug.Log($"[Unity] OnDisconnected from {ep}");
                });
            }
        }

        public override void OnSend(int numOfBytes)
        {
            int n = numOfBytes;
            // intent를 매 frame 보내면 OnSend 로그가 console 폭주 → 짧은 패킷(C_MoveIntent급)은 무시.
            if (n <= 12) return;
            EnqueueApply(() => Debug.Log($"[Unity] OnSend {n} bytes"));
        }

        /// <summary>
        /// dispatch 테이블 lookup. 미등록 PacketID는 방어 로그 후 drop.
        /// </summary>
        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
                new ReadOnlySpan<byte>(buffer.Array!, buffer.Offset + 2, 2));

            if (_handlers.TryGetValue((PacketID)packetId, out IClientPacketHandler handler))
            {
                handler.Handle(this, buffer);
            }
            else
            {
                int unknownId = packetId;
                EnqueueApply(() =>
                    Debug.LogWarning($"[Unity] Unknown PacketId {unknownId} — dropped"));
            }
        }

        // ========================================================================
        // 핸들러가 호출하는 내부 상태 변경 메서드 (internal — 같은 어셈블리).
        // 핸들러가 session 내부 field를 직접 건드리지 않도록 캡슐화.
        // ========================================================================

        internal void SetHandshakeOk() => HandshakeOk = true;

        // C# event는 선언 클래스만 raise 가능(CS0070)이라 외부 핸들러는 이 메서드를 통해 호출.
        internal void RaiseHandshakeOk() => OnHandshakeOkEvent?.Invoke();

        internal void SetLocalEntityId(int entityId) => LocalEntityId = entityId;

        internal void SetLastReceivedServerTick(int tick) => LastReceivedServerTick = tick;

        // ========================================================================
        // 씬 로드 완료 후 새 LocalPlayerMovement가 참조하는 pending spawn 좌표.
        // 씬을 넘어 유지하며, 연결 종료 때 owner가 전체 값을 초기화한다.
        // LocalPlayerMovement.Awake()에서 HasPendingSpawn 확인 → SetServerPosition 호출 → Clear.
        // ========================================================================

        public static float PendingSpawnX { get; internal set; }
        public static float PendingSpawnY { get; internal set; }
        public static bool HasPendingSpawn { get; internal set; }

        // terrain 주입용 mapId. EnterMapHandler는 0(Town 고정), MapTransitionHandler는 destMapId 박음.
        // LocalPlayerMovement.Awake()에서 pending spawn 소비 시 함께 읽어 ClientTerrainStore.Load 호출.
        public static int PendingMapId { get; internal set; }

        // LocalPlayerMovement.Awake()에서 pending spawn 소비 후 호출.
        public static void ConsumePendingSpawn()
        {
            HasPendingSpawn = false;
            PendingSpawnX = 0f;
            PendingSpawnY = 0f;
            PendingMapId = 0;
        }
    }
}
