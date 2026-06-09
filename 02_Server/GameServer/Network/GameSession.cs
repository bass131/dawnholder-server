using System.Buffers.Binary;
using System.Net;
using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Handlers;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Maps;
using Dawnholder.Server.GameServer.Network;
using Dawnholder.Server.Network;
using Shared.GameData;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Sessions;

/// <summary>
/// 게임 도메인의 한 클라이언트 세션. ServerCore의 <see cref="PacketSession"/>을 상속.
///
/// first-packet 강제 패턴: handshake 통과 전까지 게임 진입 X, mismatch는 즉시 Disconnect
/// (헌법 #3 — timeout 안 기다림). 패킷 dispatch는 HandlerRegistry로 위임하되 session은
/// lifecycle state(handshake 완료 여부 / entityId / rate-limit window)를 캡슐화한 internal
/// 메서드만 외부 노출. handler = decode + 검증, session = state.
///
/// 콜백은 socket 워커(IOCP) 스레드. GameMap mutation은 직접 X — `EnqueueJob`으로 push.
/// </summary>
public class GameSession : PacketSession
{
    int _entityId = -1;

    // _closing 플래그: connect job과 disconnect handler가 *서로 다른 thread*에서 race할 때,
    // queued AddPlayer가 이미 닫힌 세션을 owner로 박지 못하게 + cleanup이 멱등하게 보장.
    // 0=open, 1=closing/closed. Interlocked.Exchange로 atomic.
    int _closing;

    // handshake 완료 플래그. OnRecvPacket 첫 진입에서 false면 → handshake 패킷만 허용,
    // 다른 패킷 = 즉시 Disconnect. first-packet 강제 = isolation 보장 (version 검증 전 다른 패킷 차단).
    bool _handshakeCompleted;

    // C_CharacterSelect 수신 후 서버가 CharacterClass → PlayerStats 매핑 (헌법 #1).
    // null = 아직 선택 안 함 (handshake 통과 후, 선택 전 상태).
    PlayerStats? _stats;

    // protected internal: 같은 어셈블리(CharacterSelectHandler) + 서브클래스(테스트 TestGameSession) 양쪽 접근.
    protected internal bool HasSelectedClass => _stats != null;

    // 클라가 보낸 characterClass byte를 서버가 PlayerStats로 매핑 (헌법 #1).
    protected internal void SetCharacterClass(byte characterClass)
    {
        _stats = PlayerStats.ForClass((CharacterClass)characterClass);
        Console.WriteLine(
            $"[GameSession] CharacterClass set to {_stats.Class} — Hp:{_stats.Hp} Atk:{_stats.Attack} Def:{_stats.Defense} Spd:{_stats.MoveSpeed}");
    }

    // rate-limit (헌법 #3 fail-closed): 임계 초과 intent는 drop.
    //   카운트는 임계 이상이어도 *계속 증가* (oscillation attack 방지).
    //   drop만 — disconnect는 안 함 (정상 클라가 일시적 framerate spike로 임계 초과해도 게임 잘림 X).
    readonly IntentRateLimiter _rateLimiter = new();

    // 현재 맵 추적 필드.
    //
    // **동시성 가정**:
    //   읽기 = socket thread (GetMap() → SubmitMoveIntent/SubmitAttack/SubmitEnterPortal/OnDisconnected 경로).
    //   쓰기 = tick thread (migration 람다, EnqueueJob 안).
    //   단일 writer + 단순 대입(RMW 아님) → Volatile로 가시성만 보장하면 충분. Interlocked/lock 불필요.
    //   MapId는 int 기반 enum이라 Volatile.Read/Write 직접 불가 → int 백킹 필드(_currentMapIdValue) 패턴.
    int _currentMapIdValue = (int)MapId.Town; // Volatile.Read/Write용 int 백킹 필드

    MapId CurrentMapId
    {
        get => (MapId)Volatile.Read(ref _currentMapIdValue);
        set => Volatile.Write(ref _currentMapIdValue, (int)value);
    }

    // Migration 중간 상태 플래그. 0=정상, 1=이동중(어느 맵에도 없는 순간).
    // 이 사이 도착하는 게임플레이 패킷(attack/move)은 GetMap()이 null 반환 → 핸들러 안전 no-op.
    // **동시성 가정**: 쓰기=tick thread (EnqueueJob 람다), 읽기=socket thread (GetMap() / OnDisconnected).
    // 단일 writer(tick) + 단순 대입 → Volatile.Read/Write로 가시성 보장. Interlocked/lock 불필요.
    int _migrating;

    // MapMigration이 사용하는 migration 상태 조작 internal hooks.
    // _migrating / _closing은 private — MapMigration(같은 어셈블리지만 별 클래스)이 직접
    // ref 접근할 수 없으므로 래퍼 메서드로 캡슐화.
    internal void SetMigrating(int value) => Volatile.Write(ref _migrating, value);
    internal int ReadClosing() => Volatile.Read(ref _closing);
    internal void SetCurrentMapId(MapId mapId) => CurrentMapId = mapId;

    // 테스트가 GameMap을 주입할 수 있는 hook + 셧다운 race null-safe.
    //   _migrating == 1 이면 null 반환 → 핸들러 안전 no-op (transient drop 핵심).
    //   GameWorld.Instance가 null인 race 시에도 null 반환 (셧다운 race 안전망).
    protected virtual GameMap? GetMap()
    {
        if (Volatile.Read(ref _migrating) == 1) return null;
        return GameWorld.Instance?.GetMap(CurrentMapId);
    }

    // 목적지 맵 조회 hook. 테스트가 다중 맵 주입 시 override.
    protected virtual GameMap? GetDestMap(MapId destMapId)
        => GameWorld.Instance?.GetMap(destMapId);

    // GameMap.BroadcastToAll에서 closing 중인 세션 skip 판별용 internal getter.
    // broadcast 발신은 tick thread에서만 호출되므로 Volatile.Read로 memory barrier 보장.
    internal bool IsClosing => Volatile.Read(ref _closing) == 1;

    public override void OnConnected(EndPoint endPoint)
    {
        Console.WriteLine($"[GameSession] OnConnected from {endPoint} — awaiting C_Handshake");
        // AddPlayer는 handshake 통과 후 EnterGameWorld()가 호출.
        // 권한 미부여 상태에서 서버 리소스(맵 entity)를 미리 박지 않음 = trust boundary 강화.
    }

    // handshake 통과 시 게임 월드 진입.
    // protected — TestGameSession이 handshake 우회(mock) 시 직접 호출 가능 (lifecycle 테스트 호환).
    // 최초 진입 맵은 Town으로 명시 고정 (CurrentMapId Town 초기값과 정합).
    protected void EnterGameWorld()
    {
        CurrentMapId = MapId.Town; // 최초 진입은 항상 Town (헌법 #1 서버 권위)
        GameMap? map = GetMap();
        if (map == null)
        {
            // silent no-op은 shutdown race에는 맞지만 startup/config 버그(GameWorld 초기화 누락)를
            // 은폐 가능. 명시 로그로 표면화.
            Console.WriteLine($"[Trust] GameSession.EnterGameWorld: GetMap() returned null — config/shutdown race?");
            return;
        }
        GameSession self = this;
        map.EnqueueJob(() =>
        {
            // job 실행 시점에 이미 disconnect 왔으면 skip.
            // 안 그러면 닫힌 세션을 owner로 가진 player가 맵에 남아 ghost entity 발생.
            // tick thread에서 실행되므로 Volatile.Read로 충분 (memory barrier 보장).
            if (Volatile.Read(ref self._closing) == 1)
            {
                Console.WriteLine("[Map] AddPlayer skipped — session already closing (lifecycle race window)");
                return;
            }

            // 헌법 #1: spawn 좌표를 서버가 정함 (content.bin PlayerSpawn 기준).
            Vector2 spawnPos = map.PlayerSpawnPosition;

            // initial roster 순서: AddPlayer *전에* 기존 player 목록을 snapshot.
            // 자기 자신이 _players에 들어간 다음에 initial roster 만들면 자기에게 자기 PlayerJoin 보내게 됨.
            List<PlayerEntity> existing = new(map.Players);

            // _stats는 EnterGameWorldIfReady 가드로 non-null 보장 (HasSelectedClass=true 충족 후에만 진입).
            PlayerEntity entity = map.AddPlayer(self, spawnPos, self._stats);
            self._entityId = entity.EntityId;

            S_EnterMap pkt = new S_EnterMap
            {
                entityId = entity.EntityId,
                spawnX = entity.Position.X,
                spawnY = entity.Position.Y
            };
            self.Send(pkt.Write());

            // 진입 직후 권위 HP 1회 송신 — 클라 HUD 초기화. Owner 연결 완료 후 즉시.
            map.SendPlayerHp(entity);

            // Initial roster — 자기에게 기존 entity 전원의 S_PlayerJoin 다발 Send.
            // race 안전: closing 중인 owner의 entity는 skip (race window에서 곧 disappear).
            // characterClass: 서버 entity.Stats.Class byte cast (헌법 #3 — 클라 raw byte echo 절대 금지).
            foreach (PlayerEntity existingEntity in existing)
            {
                if (existingEntity.Owner != null && existingEntity.Owner.IsClosing) continue;
                S_PlayerJoin rosterEntry = new S_PlayerJoin
                {
                    entityId = existingEntity.EntityId,
                    spawnX = existingEntity.Position.X,
                    spawnY = existingEntity.Position.Y,
                    characterClass = (byte)existingEntity.Stats.Class,
                };
                self.Send(rosterEntry.Write());
            }

            // 자기 외 모든 player에게 신규 entity broadcast.
            // BroadcastToAll의 IsClosing skip이 race window 방어.
            // characterClass: 서버 entity.Stats.Class byte cast (헌법 #3).
            S_PlayerJoin joinNotice = new S_PlayerJoin
            {
                entityId = entity.EntityId,
                spawnX = entity.Position.X,
                spawnY = entity.Position.Y,
                characterClass = (byte)entity.Stats.Class,
            };
            map.BroadcastToAll(joinNotice.Write(), except: self);

            // 헌법 #1 server-only spawn: 신규 client에게 active enemy roster 다발 전송.
            // 클라가 spawn 요청 보낼 권한 X — 모두 EnterGameWorld 시점 서버 단발.
            // byte cast (EnemyKind → byte) = wire format 1:1 매핑 (S_EntitySpawn.entityKind 약속).
            foreach (EnemyEntity enemy in map.Enemies.Values)
            {
                if (enemy.IsDead) continue;
                S_EntitySpawn enemySpawn = new S_EntitySpawn
                {
                    entityId = enemy.EntityId,
                    entityKind = (byte)enemy.Kind,
                    x = enemy.X,
                    y = enemy.Y,
                    currentHp = enemy.Hp,
                    maxHp = enemy.MaxHp,
                };
                self.Send(enemySpawn.Write());
            }

            Console.WriteLine(
                $"[Map] Player {entity.EntityId} entered at ({entity.Position.X}, {entity.Position.Y}) — roster:{existing.Count}, enemies:{map.Enemies.Count}, broadcasted join");
        });
    }

    // handshake 통과 후 lifecycle 전이 = `_handshakeCompleted` 박힘 + S_HandshakeResult(ok=true) 회신.
    // **handshake = 상태 전이만** — EnterGameWorld() 직접 호출 X. 클라이언트 선택 전 월드 진입 차단(P0-1).
    //   EnterGameWorld 호출은 EnterGameWorldIfReady()로만.
    // protected internal — HandshakeHandler(internal) + 테스트 서브클래스(protected) 양쪽.
    protected internal void CompleteHandshakeAndEnter()
    {
        _handshakeCompleted = true;
        S_HandshakeResult ok = new S_HandshakeResult
        {
            ok = true,
            serverVersion = ProtocolVersion.Current,
            reason = "",
        };
        Send(ok.Write());
        // 월드 진입은 CharacterSelectHandler → EnterGameWorldIfReady() 경로로만 허용.
    }

    // idempotent 월드 진입 게이트.
    // handshake 완료 + class 선택 완료, 두 조건 *모두* 충족 시에만 EnterGameWorld() 호출.
    // **idempotent**: 두 번 호출해도 EnterGameWorld가 한 번만 실행됨 (_enteredWorld flag).
    // **race 안전**: 두 패킷(C_Handshake, C_CharacterSelect)이 다른 순서로 도착해도 두 조건 모두 충족 후에만 진입.
    bool _enteredWorld;

    protected internal void EnterGameWorldIfReady()
    {
        if (!_handshakeCompleted || !HasSelectedClass || _enteredWorld) return;
        _enteredWorld = true;
        EnterGameWorld();
    }

    // handshake version mismatch 거절: S_HandshakeResult(ok=false) Send + Disconnect.
    // 헌법 #3 정합 — timeout 안 기다리고 즉시 Disconnect (rate-limit 무효화 차단).
    internal void RejectHandshake(string reason)
    {
        Console.WriteLine($"[Trust] Handshake rejected — {reason}");

        S_HandshakeResult fail = new S_HandshakeResult
        {
            ok = false,
            serverVersion = ProtocolVersion.Current,
            reason = reason,
        };
        Send(fail.Write());
        Disconnect();
    }

    // move intent: rate-limit + invalid 정규화 + tick 마샬링. 헌법 #3 trust boundary 책임.
    //
    // **매개변수**:
    //   inputX/jumpPressed = InputBits.Decode가 정규화한 값 (invalid 시 inputX=0 자동)
    //   inputBitsValid = decode가 valid 플래그로 반환한 값 (false면 cheat 로그)
    //   rawInput = 원본 byte (cheat 로그에 0x.. 박을 때만 사용)
    //   clientTick = entity.LastClientTick 기록용
    internal void SubmitMoveIntent(sbyte inputX, bool jumpPressed, bool inputBitsValid, byte rawInput, uint clientTick)
    {
        // Rate-limit 검사. trust-boundary invariant: 동일 입력에 동일 거부/허용.
        if (!_rateLimiter.TryConsume(out bool firstWarn))
        {
            // fail-closed drop. 윈도우당 1회만 로그 (폭주 방지).
            if (firstWarn)
            {
                Console.WriteLine(
                    $"[Cheat] Player {_entityId}: intent rate exceeded {IntentRateLimiter.LimitPerSecond}/s — dropping intent (first warning this window)");
            }
            return; // 임계 초과 intent는 tick queue 진입 X.
        }

        if (!inputBitsValid)
        {
            // invalid `11` reserved 패턴 — cheat 또는 protocol mismatch.
            // Decode가 inputX=0으로 정상화했으니 시뮬은 폭주 X. 기록만.
            Console.WriteLine(
                $"[Cheat] Player {_entityId}: invalid input bits 0x{rawInput:X2} — normalized to inputX=0");
        }

        if (_entityId < 0) return; // 아직 EnterMap 안 끝남

        // tick thread로 마샬링: PlayerEntity 갱신.
        GameMap? map = GetMap();
        if (map == null)
        {
            Console.WriteLine($"[Trust] GameSession: GetMap() returned null — config/shutdown race?");
            return;
        }
        int eid = _entityId;
        sbyte capturedInputX = inputX;
        bool capturedJump = jumpPressed;
        uint capturedClientTick = clientTick;
        map.EnqueueJob(() =>
        {
            PlayerEntity? entity = map.GetPlayer(eid);
            if (entity == null) return; // 이미 RemovePlayer 됐을 수도
            entity.EnqueueInput(capturedInputX, capturedJump, capturedClientTick);
            // LastClientTick은 틱 루프에서 실제 적용 시점에 set (ack = 적용 시점).
        });
    }

    // attack: 헌법 #3 trust boundary 진입 게이트 + tick thread 마샬링 책임.
    //
    // **헌법 #3 (Trust Boundary) — attacker 강제**: 패킷에 attacker 필드 *없음*. attacker는 본 메서드가
    // `_entityId`에서 강제 — 다른 entityId 도용 차단. (attacker 필드를 패킷에 추가하면 이 방어가 무너짐.)
    //
    // **attackerClientTick 신뢰 경계**: 클라가 보낸 tick 값 — untrusted. ProcessAttack에서
    //   범위 검증(음수/미래/200ms 초과) 후 silent drop. 여기선 그대로 전달만.
    internal void SubmitAttack(int targetEntityId, long attackerClientTick)
    {
        if (_entityId < 0) return; // 아직 EnterGameWorld 안 끝남 (방어적)

        GameMap? map = GetMap();
        if (map == null)
        {
            Console.WriteLine($"[Trust] GameSession.SubmitAttack: GetMap() returned null — config/shutdown race?");
            return;
        }
        int attackerEntityId = _entityId;
        int targetId = targetEntityId;
        long clientTick = attackerClientTick;
        map.EnqueueJob(() => map.ProcessAttack(attackerEntityId, targetId, clientTick));
    }

    // skill use: C_SkillUse의 tick thread 마샬링 책임.
    //
    // **헌법 #3 (Trust Boundary) — caster 강제**: attacker와 동일하게 caster entityId는
    //   _entityId에서 강제 — 클라가 다른 플레이어를 사칭해 스킬 발동 차단.
    // skillId 범위 검증은 C_SkillUseHandler에서 이미 완료. attackerClientTick은 untrusted — ProcessSkill에서 검증.
    internal void SubmitSkillUse(byte skillId, int attackerClientTick)
    {
        if (_entityId < 0) return; // EnterGameWorld 미완료 race 방어

        GameMap? map = GetMap();
        if (map == null)
        {
            Console.WriteLine($"[Trust] GameSession.SubmitSkillUse: GetMap() returned null — config/shutdown race?");
            return;
        }
        int casterEntityId = _entityId;
        byte capturedSkillId = skillId;
        long capturedClientTick = attackerClientTick;
        map.EnqueueJob(() => map.ProcessSkill(casterEntityId, capturedSkillId, capturedClientTick));
    }

    // Pong 회신: serverTimestampMs 박고 Send.
    internal void RespondPong(long clientTimestampMs)
    {
        S_Pong pong = new S_Pong
        {
            clientTimestampMs = clientTimestampMs,
            serverTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        Send(pong.Write());
    }

    // C_EnterPortal 처리.
    //
    // **헌법 #1 (Server Authority)**: 목적지/spawn 좌표는 PortalTable이 결정. 클라는 portalId만 보냄.
    // **헌법 #3 (Trust Boundary)**: 검증 순서 (tick thread에서 실행):
    //   1. portalId가 현재 맵의 유효 portal인가 (범위 검증) → 아니면 silent drop
    //   2. 플레이어 위치가 portal 좌표 근처인가 (2 unit 임계) → 멀면 silent drop (텔레포트 핵 차단)
    //   3. _entityId < 0 방어 (EnterGameWorld 미완료 race)
    //
    // **맵 간 마샬링 방식** (Map=Actor 원칙):
    //   맵 A의 tick thread에서 RemovePlayer + S_PlayerLeave broadcast.
    //   그 후 맵 B.EnqueueJob으로 AddPlayerWithId (id 유지 — ADR-026) + S_PlayerJoin broadcast.
    //   한 맵의 tick thread가 다른 맵 상태를 직접 mutate하지 않음 (message channel만).
    //
    // **Transient drop 처리**:
    //   RemovePlayer(맵 A) 직후 ~ AddPlayerWithId(맵 B) 완료 직전 사이,
    //   _migrating = 1로 세팅 → GetMap() null 반환 → 이 사이 도착하는 attack/move는 자동 no-op.
    internal void SubmitEnterPortal(int portalId)
    {
        if (_entityId < 0) return; // EnterGameWorld 미완료 race 방어

        GameMap? currentMap = GetMap();
        if (currentMap == null)
        {
            Console.WriteLine($"[Trust] GameSession.SubmitEnterPortal: GetMap() null — config/shutdown/migration race");
            return;
        }

        int eid = _entityId;
        GameSession self = this;

        // EnqueueJob 람다 *안*에서 Execute 호출 → tick thread 동기 처리 유지 (§1.1).
        // trust-boundary invariant 보존 — 동일 입력에 동일 거부/허용 (헌법 #3).
        currentMap.EnqueueJob(() =>
            MapMigration.Execute(
                session: self,
                entityId: eid,
                currentMap: currentMap,
                portalId: portalId,
                getDestMap: self.GetDestMap));
    }

    // *항상* map job 보내고 owner reference 기반으로 cleanup (entityId 모를 때 안전).
    // Interlocked.Exchange로 _closing 박고 이중 호출 멱등성 보장 — 두 번째 OnDisconnected가 와도 enqueue 1회만.
    public override void OnDisconnected(EndPoint endPoint)
    {
        // Exchange 반환값이 1이면 *직전*에 이미 1이었다는 뜻 = 이중 호출 → enqueue 안 함.
        if (Interlocked.Exchange(ref _closing, 1) == 1) return;

        Console.WriteLine($"[GameSession] OnDisconnected from {endPoint}");

        GameMap? map = GetMap();
        if (map == null)
        {
            // _migrating=1이면 "맵 간 이동 중 disconnect" — 맵 A에서 이미 RemovePlayer됨.
            // 맵 B 람다가 _closing=1 보고 AddPlayerWithId skip → entity 어느 맵에도 없이 정리 (ghost 없음).
            // shutdown race는 GetMap이 null 반환하는 다른 경로 (GameWorld.Instance == null).
            if (Volatile.Read(ref _migrating) == 1)
                Console.WriteLine($"[GameSession] OnDisconnected during migration (player={_entityId}) — cleanup handled by migration lambda");
            else
                Console.WriteLine($"[Trust] GameSession.OnDisconnected: GetMap() returned null — config/shutdown race?");
            return;
        }
        GameSession self = this;
        map.EnqueueJob(() =>
        {
            // PlayerLeave broadcast를 위해 *cleanup 전*에 entityId 캡처.
            // 이미 -1이면 (AddPlayer 안 끝남 race) leave broadcast skip.
            int leavingEntityId = self._entityId;

            // owner reference 기반 cleanup — entityId가 race window 안 -1이어도 안전.
            // AddPlayer가 같은 batch에 들어왔으면 그 entity도 같이 제거 (멱등).
            bool removed = map.RemovePlayerBySession(self);

            // 자기 외 남은 player 전원에게 leave broadcast.
            // 자기 자신은 BroadcastToAll의 except로 차단 + 이미 _players에서 빠진 상태(자기 owner X).
            if (removed && leavingEntityId >= 0)
            {
                S_PlayerLeave leaveNotice = new S_PlayerLeave { entityId = leavingEntityId };
                map.BroadcastToAll(leaveNotice.Write(), except: self);
            }

            Console.WriteLine($"[Map] Session cleanup (entityId={leavingEntityId}, removed={removed})");
            // cleanup 후 _entityId reset — 낡은 id가 로그/방어 로직에 남는 것 차단.
            self._entityId = -1;
        });
    }

    public override void OnSend(int numOfBytes)
    {
        // 의도적 no-op: send 로그는 콘솔 spam이라 제거 (호출 빈도 자체는 정상).
    }

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(buffer.Array!, buffer.Offset + 2, 2));
        PacketID id = (PacketID)packetId;

        // 패킷 종류 추적 로그. spam 패킷(C_MoveIntent 20Hz / C_Ping)은 제외 — 드문 패킷만 박음.
        if (id != PacketID.C_MoveIntent && id != PacketID.C_Ping)
        {
            Console.WriteLine($"[GameSession] OnRecv {id} ({buffer.Count} bytes)");
        }

        // 헌법 #2: first-packet 강제. handshake 통과 전엔 다른 dispatch X (게이트는 session 책임).
        if (!_handshakeCompleted)
        {
            if (id == PacketID.C_Handshake && HandlerRegistry.TryGet(id, out IPacketHandler handshake))
            {
                handshake.Handle(this, buffer);
            }
            else
            {
                Console.WriteLine(
                    $"[Trust] First packet was {id} (not C_Handshake) — disconnecting");
                Disconnect();
            }
            return;
        }

        // handshake 통과 후 재-handshake는 protocol violation (헌법 #2).
        // silent drop보다 명시적 거절이 진단 가치 ↑.
        if (id == PacketID.C_Handshake)
        {
            Console.WriteLine($"[Trust] Duplicate C_Handshake after handshake completed — protocol violation, disconnecting");
            Disconnect();
            return;
        }

        // Dictionary dispatch. 새 핸들러 추가 = HandlerRegistry._handlers에 한 줄 등록. 누락 시 unknown drop.
        if (HandlerRegistry.TryGet(id, out IPacketHandler handler))
        {
            handler.Handle(this, buffer);
        }
        else
        {
            Console.WriteLine($"[GameSession] Unknown PacketId {packetId} — dropped");
        }
    }
}
