using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Net;
using Dawnholder.Client.State;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Network
{
    /// <summary>
    /// ClientNet의 <see cref="PacketSession"/>을 Unity 컨텍스트로 wrap.
    ///
    /// **Phase 05 변경**: Phase 04의 <see cref="ClientSession"/> 직접 상속에서
    /// PacketSession 상속으로 교체. framing 자동, OnRecvPacket은 *완전한 한 패킷*.
    /// **Phase 03 변경**: S_EnterMap 핸들러 추가 — 서버 결정 spawn 좌표 적용.
    /// **Phase 04 변경**: S_Snapshot 핸들러 + Instance singleton (LocalPlayerController가 Send용으로 참조).
    /// **M4.1 Phase 02 변경**: OnHandshakeOkEvent 추가 — NetworkService가 event 기반으로
    ///   C_CharacterSelect 송신 (race 봉합, 옵션 A). (ADR-027: NetworkBootstrap→NetworkService 재정의)
    /// **M4.3R Phase 02 변경**: inline switch(12 패킷) → IClientPacketHandler + dispatch 테이블 (§3.2).
    ///   RosterTransitionBuffer / SceneRouter 추출. 컨테이너는 framing + dispatch + main-thread 마샬링만 잔류.
    ///
    /// 콜백 모두 socket 워커 스레드 → Unity API는 main-thread queue 경유.
    /// </summary>
    public class UnityClientSession : PacketSession
    {
        // Phase 04: LocalPlayerController가 매 frame C_MoveIntent를 Send하려면 정적 접근점 필요.
        // 일회 설정. NetworkService가 connect 콜백에서 본 객체를 만들 때 등록.
        public static UnityClientSession Instance { get; private set; }

        // M3 Phase 02 (Codex review #2): handshake 완료 게이트.
        // OnConnected가 socket 워커 스레드에서 C_Handshake를 자동 Send하지만,
        // *main thread Update*가 그 사이 LocalPlayerController.SendIntent를 호출할 race window가 짧게 존재.
        // 본 플래그는 main thread에서 HandshakeResultHandler가 박음(dispatcher 큐 안) → 같은 thread의
        // SendIntent에서 visibility 보장. ok 회신 도착 전 송신은 drop (헌법 #2 first-packet 정합).
        public bool HandshakeOk { get; private set; }

        // M4.1 Phase 02 5-B: handshake OK event. NetworkService가 등록 후 S_HandshakeResult(ok=true)
        // 수신 시 main thread에서 호출됨. C_CharacterSelect 송신 race 봉합 핵심.
        // event 패턴: 구독자 없어도 null check로 안전 (PersistentServices 미생성 씬 단독 Play 방어).
        public event Action OnHandshakeOkEvent;

        // M3 Phase 05: 본인 entityId. HandleEnterMap에서 박음 (main thread).
        // HandleSnapshot이 entityId 비교로 본인/타인 분기. null이면 (EnterMap 도착 전 Snapshot race)
        // 해당 Snapshot drop — 다음 Snapshot에서 정상화.
        public int? LocalEntityId { get; private set; }

        // M4.1 Phase 06 (lag comp 3단계): 마지막으로 수신한 S_Snapshot의 serverTick.
        // C_Attack 송신 시 attackerClientTick 필드에 박아 서버 rewind 기준점을 제공.
        // HandleSnapshot(main thread)에서 갱신. 초기값 0 — 첫 Snapshot 전 공격은 서버가
        // silent drop(검증 규칙: currentServerTick - attackerClientTick > 4)하므로 실전 영향 없음.
        // 본인/타인 Snapshot 모두 갱신 (어느 것이든 서버 현재 tick을 표현하므로 기준점으로 유효).
        public int LastReceivedServerTick { get; private set; }

        // ========================================================================
        // P1 봉합: roster buffer (추출 → RosterTransitionBuffer).
        // 컨테이너는 버퍼 인스턴스만 보유. 로직은 RosterTransitionBuffer 안에 있음.
        // ========================================================================

        // internal: ClientPacketHandlers.cs(같은 어셈블리)에서 직접 접근.
        internal RosterTransitionBuffer RosterBuffer { get; } = new RosterTransitionBuffer();

        // ========================================================================
        // dispatch 테이블 (§3.2 IClientPacketHandler 미러).
        // 새 패킷 추가 = 핸들러 1개 신설 + 여기 1줄 등록만.
        // ========================================================================

        static readonly IReadOnlyDictionary<PacketID, IClientPacketHandler> _handlers =
            new Dictionary<PacketID, IClientPacketHandler>
            {
                { PacketID.S_HandshakeResult, new HandshakeResultHandler() },
                { PacketID.S_Pong,            new PongHandler() },
                { PacketID.S_EnterMap,        new EnterMapHandler() },
                { PacketID.S_Snapshot,        new SnapshotHandler() },
                { PacketID.S_PlayerJoin,      new PlayerJoinHandler() },
                { PacketID.S_PlayerLeave,     new PlayerLeaveHandler() },
                { PacketID.S_EntitySpawn,     new EntitySpawnHandler() },
                { PacketID.S_HitResult,       new HitResultHandler() },
                { PacketID.S_EntityDeath,     new EntityDeathHandler() },
                { PacketID.S_StageClear,      new StageClearHandler() },
                { PacketID.S_MapTransition,   new MapTransitionHandler() },
                { PacketID.S_EntityState,     new EntityStateHandler() },
            };

        // Phase 05: Editor only 송신 latency 시뮬레이션.
        //   0이면 직통 (Release/일반 Play 동작).
        //   >0이면 SendIntent 경로에 한해 N ms 지연 후 실제 Send.
        //   값 변경은 코드 수정 후 Play 재시작 (Inspector 노출은 미래 옵션).
        //   완료 조건 ②③ 검증: 0 → snap 분당 5회 미만 / 200 → snap 빈도 증가 + 점프 시각 확인.
#if UNITY_EDITOR
        public static int SimulatedLatencyMs = 0;
#endif

        public UnityClientSession()
        {
            Instance = this;
        }

        /// <summary>
        /// Phase 05: 입력 intent 송신용 wrapper. Editor에선 SimulatedLatencyMs 적용.
        /// Release/일반 Play에선 Send 직통 — 컴파일 시 분기 사라짐(<c>#if UNITY_EDITOR</c>).
        ///
        /// 본 Phase에선 LocalPlayerController가 C_MoveIntent를 이 경로로 보냄.
        /// 다른 패킷(Ping 등)은 그대로 Send 직통 — RTT 측정 시 latency 영향 분리 가능.
        /// </summary>
        public void SendIntent(ArraySegment<byte> buf)
        {
            // M3 Phase 02 (Codex review #2): handshake 통과 전 송신은 drop.
            // 정상 흐름에선 OnConnected의 C_Handshake → S_HandshakeResult OK가 첫 Update tick 안에 박혀서 영향 X.
            // race window (handshake 결과 도착 *이전*에 LocalPlayerController.Update가 SendIntent 호출)에서만 발동.
            if (!HandshakeOk)
            {
                // 폭주 차단 위해 main thread에서 한 줄만. 정상 흐름엔 거의 0회 박힘.
                return;
            }
#if UNITY_EDITOR
            if (SimulatedLatencyMs > 0)
            {
                // buf는 GenPackets.Write()가 매번 새로 할당한 byte[]라 큐 보존 안전(corruption X).
                ArraySegment<byte> captured = buf;
                MainThreadDispatcher.EnqueueDelayed(() => Send(captured), SimulatedLatencyMs / 1000f);
                return;
            }
#endif
            Send(buf);
        }

        public override void OnConnected(EndPoint endPoint)
        {
            EndPoint ep = endPoint;
            MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] OnConnected to {ep}"));

            // M3 Phase 02 (헌법 #2 봉합): 첫 패킷 = 반드시 C_Handshake.
            // 서버가 first-packet 강제 패턴 박혀있어 다른 패킷 먼저 보내면 즉시 Disconnect.
            // Send 자체는 thread-safe(Session.m_lock) — socket 워커 스레드에서 직접 호출 OK.
            C_Handshake handshake = new C_Handshake { clientVersion = ProtocolVersion.Current };
            Send(handshake.Write());
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            EndPoint ep = endPoint;
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log($"[Unity] OnDisconnected from {ep}");
                // M3 Phase 05: 모든 타인 entity cleanup — 메모리 누수 차단.
                if (RemoteEntityRegistry.Instance != null)
                    RemoteEntityRegistry.Instance.Clear();
                // M3 Phase 08c: enemy/boss도 동일 cleanup. StageClearUI는 누적 표시 OK라 유지.
                if (EnemyRegistry.Instance != null)
                    EnemyRegistry.Instance.Clear();
                // M4.3R Phase β: sceneLoaded 구독 해제 — stale 구독 누수 차단.
                RosterBuffer.Teardown();
                if (Instance == this) Instance = null;
            });
        }

        public override void OnSend(int numOfBytes)
        {
            int n = numOfBytes;
            // Phase 04: intent를 매 frame 보내면 OnSend가 60/s 흘러 console 폭주.
            // 12 bytes 미만(=C_MoveIntent의 [size:2][id:2][inputX:1][padding:3][clientTick:4]?)은 무시.
            // 실제로는 size+id+1+4=9 bytes지만 Write에서 패딩 없음 — 단순히 *짧은 패킷은 조용히*.
            if (n <= 12) return;
            MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] OnSend {n} bytes"));
        }

        /// <summary>
        /// dispatch 테이블 lookup — inline switch 대체 (§3.2).
        /// 미등록 PacketID는 방어 로그 후 drop (동작 보존).
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
                MainThreadDispatcher.Enqueue(() =>
                    Debug.LogWarning($"[Unity] Unknown PacketId {unknownId} — dropped"));
            }
        }

        // ========================================================================
        // 핸들러가 호출하는 내부 상태 변경 메서드 (internal — 같은 어셈블리).
        // 핸들러가 session 내부 field를 직접 건드리지 않도록 캡슐화
        // (서버 GameSession의 CompleteHandshakeAndEnter / RespondPong 패턴 미러).
        // ========================================================================

        /// <summary>S_HandshakeResult(ok=true) 수신 시 HandshakeResultHandler가 호출.</summary>
        internal void SetHandshakeOk() => HandshakeOk = true;

        /// <summary>OnHandshakeOkEvent 발화. C# event는 선언 클래스만 raise 가능(CS0070)이라
        /// 외부 핸들러(HandshakeResultHandler)는 이 메서드를 통해 호출.</summary>
        internal void RaiseHandshakeOk() => OnHandshakeOkEvent?.Invoke();

        /// <summary>S_EnterMap 수신 시 EnterMapHandler가 호출.</summary>
        internal void SetLocalEntityId(int entityId) => LocalEntityId = entityId;

        /// <summary>S_Snapshot 수신 시 SnapshotHandler가 호출.</summary>
        internal void SetLastReceivedServerTick(int tick) => LastReceivedServerTick = tick;

        // ========================================================================
        // M4.2 Phase 04: 씬 로드 완료 후 새 LocalPlayerController가 참조하는 pending spawn 좌표.
        // UnityClientSession은 DontDestroyOnLoad 없이 IOCP 스레드에서 계속 살아있으므로 static 공유.
        // LocalPlayerController.Awake()에서 HasPendingSpawn 확인 → SetServerPosition 호출 → Clear.
        //
        // §0.3 잔류 이유: LocalPlayerController.Awake가 직접 소비. 다른 클래스로 옮기면
        //   호출 경로만 늘고 가독 이득 0 (스펙 §0.3 분리 금지 명문).
        // ========================================================================

        public static float PendingSpawnX { get; internal set; }
        public static float PendingSpawnY { get; internal set; }
        public static bool HasPendingSpawn { get; internal set; }

        // LocalPlayerController.Start()에서 pending spawn 소비 후 호출.
        public static void ConsumePendingSpawn()
        {
            HasPendingSpawn = false;
            PendingSpawnX = 0f;
            PendingSpawnY = 0f;
        }
    }
}
