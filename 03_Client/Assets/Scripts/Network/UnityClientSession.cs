using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Audio;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.UI;
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
using UnityEngine.SceneManagement;

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
        public MapEntryCoordinator Entry { get; }
        LocalPlayerMovement _entryPlayer;
        long _playerEpoch;
        SceneTransition _sceneLoader;
        long _sceneRequestId;
        HudController _hud;
        public bool CanSendGameplay => CanApply && HandshakeOk && Entry.IsGameplayReady;
        public bool CanControlPlayer(LocalPlayerMovement player) => CanSendGameplay && _entryPlayer == player;
        public bool IsCurrentEntry(long epoch) => CanApply && Entry.IsCurrent(epoch);
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
            Entry = new MapEntryCoordinator(CommitEntry, ApplyEntryHp, FailEntry,
                entry => { if (entry.RequiresPlayer) RosterBuffer.Drain(); });
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

        // The entry boundary is read on main, after preceding EnterMap/MapTransition callbacks.
        public void EnqueueWorldApply(Action action) => EnqueueApply(() =>
        {
            long epoch = Entry.Epoch;
            if (!Entry.IsCurrent(epoch) || !Entry.RequiresPlayer) return;
            Action scoped = () => { if (IsCurrentEntry(epoch)) action(); };
            if (!RosterBuffer.TryBuffer("world update", scoped)) scoped();
        });

        public bool BeginMapEntry(byte mapId, float x, float y, bool initial = false)
        {
            if (!CanApply) return false;
            string sceneName = SceneRouter.MapIdToSceneName(mapId);
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"[Unity] Unknown destination map {mapId}; entry rejected.");
                return false;
            }
            long epoch = Entry.Begin(mapId, x, y, SceneRouter.RequiresPlayer(mapId));
            if (_sceneLoader != null && _sceneRequestId != 0) _sceneLoader.CancelRequest(_sceneRequestId);
            _sceneRequestId = 0;
            _entryPlayer = null;
            _playerEpoch = 0;
            _hud = null;
            LocalPlayerMovement.Instance?.SuspendForMapEntry();
            RemoteEntityRegistry.Instance?.Clear();
            RosterBuffer.BeginTransition(sceneName);

            string bgm = SoundKeys.BgmKeyForMap(mapId);
            if (bgm != null) AudioManager.Instance?.PlayBgm(bgm);
            if (!initial) AudioManager.Instance?.PlaySfx(SoundKeys.PortalEnter);

            Scene existing = SceneManager.GetSceneByName(sceneName);
            _sceneLoader = SceneTransition.Instance;
            if (initial && existing.IsValid() && existing.isLoaded)
            {
                var active = _sceneLoader != null ? _sceneLoader.ActiveRequest : null;
                if (active != null && !active.IsFinished && active.SceneName == sceneName)
                {
                    _sceneRequestId = active.Id;
                    active.ObserveCompletion(result => OnEntrySceneLoaded(epoch, result));
                }
                else BindEntryScene(epoch, existing.handle.GetRawData());
                return true;
            }
            try
            {
                _sceneLoader = SceneTransition.EnsureInstance();
                _sceneRequestId = _sceneLoader.RequestScene(sceneName,
                    result => OnEntrySceneLoaded(epoch, result)).Id;
            }
            catch (Exception error) { Entry.Fail(epoch, error); }
            return Entry.IsCurrent(epoch);
        }

        void OnEntrySceneLoaded(long epoch, SceneLoadResult result)
        {
            if (!IsCurrentEntry(epoch)) return;
            if (result.Status == SceneLoadStatus.Completed) BindEntryScene(epoch, result.SceneHandle);
            else
                Entry.Fail(epoch, result.Error ?? new InvalidOperationException($"Scene request ended: {result.Status}."));
        }

        void BindEntryScene(long epoch, ulong handle)
        {
            if (!IsCurrentEntry(epoch)) return;
            MapNameDisplay.SetMapId(Entry.MapId);
            Entry.BindScene(epoch, handle);
            TryBindEntryViews();
        }

        public void TryBindEntryViews()
        {
            long epoch = Entry.Epoch;
            if (!IsCurrentEntry(epoch) || !Entry.SceneReady || !Entry.RequiresPlayer) return;
            try
            {
                var player = LocalPlayerMovement.Instance;
                if (player != null && player.gameObject.scene.handle.GetRawData() == Entry.SceneHandle &&
                    (_entryPlayer != player || _playerEpoch != epoch))
                {
                    player.InjectTerrain(Entry.MapId);
                    _entryPlayer = player;
                    _playerEpoch = epoch;
                    Entry.BindPlayer(epoch);
                }
                var remote = RemoteEntityRegistry.Instance;
                var enemies = EnemyRegistry.Instance;
                if (remote != null && enemies != null &&
                    remote.gameObject.scene.handle.GetRawData() == Entry.SceneHandle && enemies.gameObject.scene.handle.GetRawData() == Entry.SceneHandle)
                    Entry.BindRegistries(epoch);
            }
            catch (Exception error) { Entry.Fail(epoch, error); }
        }

        void CommitEntry(MapEntryCoordinator entry)
        {
            if (!CanApply) throw new InvalidOperationException("Session ended before scene binding.");
            if (!entry.RequiresPlayer) return; // Ending has no player, terrain, or HP barrier.
            if (_entryPlayer == null) throw new InvalidOperationException("Entry player is unavailable.");
            _entryPlayer.SetServerPosition(new Vector3(entry.SpawnX, entry.SpawnY, 0f));
        }

        public void ReceivePlayerHp(int entityId, int current, int max)
        {
            if (CanApply && LocalEntityId == entityId) Entry.ReceiveHp(Entry.Epoch, current, max);
        }

        public bool TryBindHud(HudController hud)
        {
            if (!CanApply || !Entry.IsGameplayReady || !Entry.HasHp || hud == null) return false;
            _hud = hud;
            hud.ApplyServerHP(Entry.CurrentHp, Entry.MaxHp);
            return true;
        }

        public void UnbindHud(HudController hud) { if (_hud == hud) _hud = null; }

        void ApplyEntryHp(int current, int max)
        {
            if (CanApply && _hud != null) _hud.ApplyServerHP(current, max);
        }

        void FailEntry(Exception error)
        {
            Debug.LogError($"[Unity] Map entry failed; disconnecting: {error}");
            Disconnect();
        }

        // Main-thread cleanup of this session only. The owner resets shared mirrors.
        public void Cleanup()
        {
            if (_cleaned) return;
            _cleaned = true;
            Interlocked.Exchange(ref _closed, 1);
            HandshakeOk = false;
            LocalEntityId = null;
            LastReceivedServerTick = 0;
            Entry.Close();
            if (_sceneLoader != null && _sceneRequestId != 0) _sceneLoader.CancelRequest(_sceneRequestId);
            _sceneRequestId = 0;
            _entryPlayer = null;
            _hud = null;
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
            if (!CanSendGameplay)
            {
                // 폭주 차단 위해 main thread에서 한 줄만. 정상 흐름엔 거의 0회 박힘.
                return;
            }
#if UNITY_EDITOR
            if (SimulatedLatencyMs > 0)
            {
                // buf는 GenPackets.Write()가 매번 새로 할당한 byte[]라 큐 보존 안전(corruption X).
                ArraySegment<byte> captured = buf;
                long epoch = Entry.Epoch;
                _postDelayed(() =>
                {
                    if (CanSendGameplay && Entry.Epoch == epoch) Send(captured);
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

        // Compatibility view only: the entry record is the single source of spawn state.
        public static float PendingSpawnX => Instance != null ? Instance.Entry.SpawnX : 0f;
        public static float PendingSpawnY => Instance != null ? Instance.Entry.SpawnY : 0f;
        public static int PendingMapId => Instance != null ? Instance.Entry.MapId : 0;
        public static bool HasPendingSpawn => Instance != null &&
            Instance.Entry.State == MapEntryState.Entering && Instance.Entry.RequiresPlayer && !Instance.Entry.SpawnApplied;

    }
}
