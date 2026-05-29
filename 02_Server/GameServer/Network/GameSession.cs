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
/// **Phase 03 변경**: OnConnected/Disconnected가 GameMap actor로 마샬링.
/// **Phase 04 변경**: C_MoveIntent 핸들러 + 검증(헌법 #3) + rate-limit 골격.
/// **M3 Phase 02 변경**: 헌법 #2 "Protocol is Sacred" 가짜 약속 1번째 봉합 — first-packet 강제 패턴
///   (handshake 통과 전까지 게임 진입 X). 옛 OnConnected의 AddPlayer 흐름을 EnterGameWorld로 이동,
///   HandleHandshake가 검증 통과 시 호출. mismatch는 즉시 Disconnect (헌법 #3 — timeout 안 기다림).
/// **M3 Phase 03 변경**: 헌법 #4 "Shared Code Discipline" 가짜 약속 2번째 봉합 — 02_Server/CLAUDE.md에
///   약속만 박혀있던 `Handlers/` 폴더를 실제 구조로. inline HandleXxx 메서드 3개를 외부 IPacketHandler
///   구현체로 추출 + HandlerRegistry Dictionary dispatch. session은 lifecycle state(handshake 완료
///   여부 / entityId / rate-limit window) 캡슐화한 internal 메서드만 외부 노출
///   (RejectHandshake / SubmitMoveIntent / RespondPong). handler = decode + 검증, session = state.
///
/// 콜백은 socket 워커(IOCP) 스레드. GameMap mutation은 직접 X — `EnqueueJob`으로 push.
/// </summary>
public class GameSession : PacketSession
{
    int _entityId = -1;

    // Phase 10 (M2.5 lifecycle race): _closing 플래그.
    // connect job과 disconnect handler가 *서로 다른 thread*에서 race할 때,
    // queued AddPlayer가 이미 닫힌 세션을 owner로 박지 못하게 + cleanup이 멱등하게 보장.
    // 0=open, 1=closing/closed. Interlocked.Exchange로 atomic.
    int _closing;

    // M3 Phase 02 (헌법 #2 가짜 약속 봉합): handshake 완료 플래그.
    // OnRecvPacket 첫 진입에서 false면 → handshake 패킷만 허용, 다른 패킷 = 즉시 Disconnect.
    // first-packet 강제 패턴 = isolation 보장 (다른 패킷 받기 전 version 검증).
    bool _handshakeCompleted;

    // M3.8 Phase 03 (캐릭터 선택 — 헌법 #1 Server Authority):
    // C_CharacterSelect 수신 후 서버가 CharacterClass → PlayerStats 매핑.
    // null = 아직 선택 안 함 (handshake 통과 후, 선택 전 상태).
    // CharacterSelectHandler가 HasSelectedClass 확인 후 SetCharacterClass 호출.
    PlayerStats? _stats;

    // CharacterSelectHandler가 중복 선택 차단에 사용.
    // protected internal: 같은 어셈블리(CharacterSelectHandler) + 서브클래스(테스트 TestGameSession) 양쪽 접근.
    // CompleteHandshakeAndEnter/RejectHandshake 패턴 정합.
    protected internal bool HasSelectedClass => _stats != null;

    // M3.8 Phase 03 (헌법 #1): 클라가 보낸 characterClass byte를 서버가 PlayerStats로 매핑.
    // 범위 검증은 CharacterSelectHandler에서 이미 완료 (0 또는 1만 도달).
    // 여기서는 매핑만 — 두 번 검증 불필요 (CLAUDE.md "handler = 검증, session = state" 정합).
    // M4.1 Phase 02: internal → protected internal. 테스트 서브클래스(다른 어셈블리)에서 mock 우회 호출 가능.
    // CompleteHandshakeAndEnter / HasSelectedClass 패턴 정합.
    protected internal void SetCharacterClass(byte characterClass)
    {
        _stats = characterClass == (byte)CharacterClass.Warrior
            ? PlayerStats.Warrior()
            : PlayerStats.Ranger();
        Console.WriteLine(
            $"[GameSession] CharacterClass set to {_stats.Class} — Hp:{_stats.Hp} Atk:{_stats.Attack} Def:{_stats.Defense} Spd:{_stats.MoveSpeed}");
    }

    // Phase 04: rate-limit 골격 (헌법 #3). 1초 fixed 윈도우 (sliding 아님 — 학습 노트).
    // Phase 05 조정 (2026-05-11):
    //   - 임계값 100 → 500. 240Hz 모니터 사용자의 정상 wire rate가 ~300-500/s라 100은 너무 빡빡.
    //     framerate-bound 송신이 본질 문제 — Phase 06 fixed simulation에서 ~20/s로 정상화 예정.
    //   - 로그 폭주 차단: 윈도우당 *최초 1회만* 출력. 매 패킷마다 [Cheat] 1500줄 폭주 X.
    // Phase 09 (M2.5 Trust-boundary, 2026-05-18): 임계 초과 intent *drop* (헌법 #3 fail-closed 코드 실현).
    //   - 이전엔 로그만 + 처리 진행 → "주석으로 박힌 약속이 가짜" 패턴. 본 Phase에서 봉합.
    //   - 카운트는 임계 이상이어도 *계속 증가* (oscillation attack 방지).
    //   - drop만 — disconnect는 안 함 (정상 클라가 일시적 framerate spike로 임계 초과해도 게임 잘림 X).
    // M4.3R Phase 04: IntentRateLimiter로 추출 (socket 없이 단독 테스트 가능 — 이 추출의 핵심 이득).
    //   trust-boundary invariant 보존: 동일 입력에 동일 거부/허용 (헌법 #3).
    readonly IntentRateLimiter _rateLimiter = new();

    // M4.2 Phase 03: 현재 맵 추적 필드.
    //
    // **초기값 Town**: EnterGameWorld에서 Town으로 최초 진입 (Phase 01 임시 Town 고정 동작 유지).
    // **_migrating flag**: RemovePlayer~AddPlayer 사이 "어느 맵에도 없는" 순간 transitioning 마킹.
    //   이 사이 도착하는 게임플레이 패킷(attack/move)은 GetMap()이 null 반환 → 핸들러 안전 no-op.
    //
    // **왜 null이 아닌 Town 초기값인가?**
    //   EnterGameWorldIfReady 호출 전 gamePlay 패킷은 이미 _handshakeCompleted/_enteredWorld 게이트가
    //   차단함. CurrentMapId 초기값은 EnterGameWorld에서 Town으로 세팅되므로 초기 Town이 자연스러움.
    //
    // **동시성 가정** (M4.2 Phase 03 리뷰어 🟡 1 봉합):
    //   읽기 = socket thread (GetMap() → SubmitMoveIntent/SubmitAttack/SubmitEnterPortal/OnDisconnected 호출 경로).
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
    // **동시성 가정**: 쓰기=tick thread (EnqueueJob 람다), 읽기=socket thread (GetMap() / OnDisconnected).
    // 단일 writer(tick) + 단순 대입 → Volatile.Read/Write로 가시성 보장. Interlocked/lock 불필요.
    int _migrating;

    // M4.3R Phase 04: MapMigration이 사용하는 migration 상태 조작 internal hooks.
    // _migrating / _closing은 private — MapMigration(같은 어셈블리지만 별 클래스)이 직접
    // ref 접근할 수 없으므로 래퍼 메서드로 캡슐화. 호출 위치(tick thread)가 명확히 드러남.
    internal void SetMigrating(int value) => Volatile.Write(ref _migrating, value);
    internal int ReadClosing() => Volatile.Read(ref _closing);
    internal void SetCurrentMapId(MapId mapId) => CurrentMapId = mapId;

    // Phase 09 (M2.5): 테스트가 GameMap을 주입할 수 있는 hook + 셧다운 race null-safe.
    // M4.2 Phase 03: GameWorld.Instance?.Map(Town 고정 임시) → GetMap(_currentMapId)으로 교체.
    //   _migrating == 1 이면 null 반환 → 핸들러 안전 no-op (transient drop 핵심).
    //   GameWorld.Instance가 null인 race 시에도 null 반환 (기존 셧다운 race 안전망 유지).
    protected virtual GameMap? GetMap()
    {
        if (Volatile.Read(ref _migrating) == 1) return null;
        return GameWorld.Instance?.GetMap(CurrentMapId);
    }

    // M4.2 Phase 03: 목적지 맵 조회 hook. 테스트가 다중 맵 주입 시 override.
    // 운영 시: GameWorld.Instance?.GetMap(destMapId) — 전역 레지스트리에서 조회.
    // 테스트: TestGameSession이 override해 미리 준비한 맵을 반환.
    protected virtual GameMap? GetDestMap(MapId destMapId)
        => GameWorld.Instance?.GetMap(destMapId);

    // M3 Phase 04 (Phase 10 lifecycle race 재발 봉합 패턴 일반화): GameMap.BroadcastToAll에서
    // 발신 시 closing 중인 세션 skip 판별용 internal getter. broadcast 발신은 tick thread에서만
    // 호출되므로 Volatile.Read로 memory barrier 보장.
    internal bool IsClosing => Volatile.Read(ref _closing) == 1;

    public override void OnConnected(EndPoint endPoint)
    {
        Console.WriteLine($"[GameSession] OnConnected from {endPoint} — awaiting C_Handshake");
        // M3 Phase 02: AddPlayer는 handshake 통과 후 EnterGameWorld()가 호출.
        // 권한 미부여 상태에서 서버 리소스(맵 entity)를 미리 박지 않음 = trust boundary 강화.
    }

    // M3 Phase 02 (헌법 #2 봉합): handshake 통과 시 게임 월드 진입.
    // 옛 OnConnected가 직접 호출하던 AddPlayer 흐름을 통째 이동.
    // protected — TestGameSession이 handshake 우회(mock) 시 직접 호출 가능 (lifecycle 테스트 호환).
    // M4.2 Phase 03: 최초 진입 맵은 Town으로 명시 고정 (CurrentMapId Town 초기값과 정합).
    protected void EnterGameWorld()
    {
        CurrentMapId = MapId.Town; // 최초 진입은 항상 Town (헌법 #1 서버 권위)
        GameMap? map = GetMap();
        if (map == null)
        {
            // Codex β 검토 권장(Phase 09): silent no-op은 shutdown race에는 맞지만
            // startup/config 버그(GameWorld 초기화 누락)를 은폐 가능. 명시 로그로 표면화.
            Console.WriteLine($"[Trust] GameSession.EnterGameWorld: GetMap() returned null — config/shutdown race?");
            return;
        }
        GameSession self = this;
        map.EnqueueJob(() =>
        {
            // Phase 10 (M2.5 lifecycle race): job 실행 시점에 이미 disconnect 왔으면 skip.
            // 안 그러면 닫힌 세션을 owner로 가진 player가 맵에 남아 ghost entity 발생.
            // tick thread에서 실행되므로 Volatile.Read로 충분 (memory barrier 보장).
            if (Volatile.Read(ref self._closing) == 1)
            {
                Console.WriteLine("[Map] AddPlayer skipped — session already closing (lifecycle race window)");
                return;
            }

            // 헌법 #1 시연을 위해 spawn 좌표를 서버가 정함. 현재 (0, 0).
            // Phase 03 검증 단계엔 (3, 0)으로 잠시 바꿔 Unity 캐릭터가 그 자리에 뜨는지
            // 캡처로 시각 확인 완료 (DONE.md AC 섹션 참조).
            Vector2 spawnPos = new Vector2(0f, 0f);

            // M3 Phase 04 (initial roster 순서): AddPlayer *전에* 기존 player 목록을 snapshot.
            // 자기 자신이 _players에 들어간 다음에 initial roster 만들면 자기에게 자기 PlayerJoin 보내게 됨.
            // 깔끔하게 분리: 기존 entity 목록 먼저 캡처 → 자기 add → 자기에게 기존 entity 다발 Send → 자기 외 broadcast.
            List<PlayerEntity> existing = new(map.Players);

            // M4.1 Phase 05 (3단계): _stats를 AddPlayer에 전달. EnterGameWorldIfReady 가드로
            // non-null 보장 (HasSelectedClass=true 조건 충족 후에만 EnterGameWorld 진입).
            PlayerEntity entity = map.AddPlayer(self, spawnPos, self._stats);
            self._entityId = entity.EntityId;

            S_EnterMap pkt = new S_EnterMap
            {
                entityId = entity.EntityId,
                spawnX = entity.Position.X,
                spawnY = entity.Position.Y
            };
            self.Send(pkt.Write());

            // M3 Phase 04: Initial roster — 자기에게 기존 entity 전원의 S_PlayerJoin 다발 Send.
            // race 안전: closing 중인 owner의 entity는 skip (race window에서 곧 disappear).
            foreach (PlayerEntity existingEntity in existing)
            {
                if (existingEntity.Owner != null && existingEntity.Owner.IsClosing) continue;
                S_PlayerJoin rosterEntry = new S_PlayerJoin
                {
                    entityId = existingEntity.EntityId,
                    spawnX = existingEntity.Position.X,
                    spawnY = existingEntity.Position.Y,
                };
                self.Send(rosterEntry.Write());
            }

            // M3 Phase 04: 자기 외 모든 player에게 신규 entity broadcast.
            // BroadcastToAll의 IsClosing skip이 race window 방어.
            S_PlayerJoin joinNotice = new S_PlayerJoin
            {
                entityId = entity.EntityId,
                spawnX = entity.Position.X,
                spawnY = entity.Position.Y,
            };
            map.BroadcastToAll(joinNotice.Write(), except: self);

            // M3 Phase 06 Step 4 (응급 전투 — 헌법 #1 server-only spawn 흐름):
            // 신규 client에게 active enemy roster 다발 전송 (Phase 04 initial roster 패턴 정합).
            // - server-only spawn 트리거: 클라가 spawn 요청 보낼 권한 X, 모두 EnterGameWorld 시점 서버 단발
            // - `IsDead` 체크 = idempotent 보장 (현재는 ctor spawn뿐이지만 죽은 enemy 잔류 케이스 대비)
            // - byte cast (EnemyKind → byte) = wire format 1:1 매핑 (S_EntitySpawn.entityKind 약속)
            // - **헌법 #5 정합**: 단순 foreach + Send. await/Task.Delay/Thread.Sleep 없음.
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

    // M3 Phase 02 (Codex review 인사이트 — Phase 03 진입 캡슐화):
    // handshake 통과 후 lifecycle 전이 묶음 = `_handshakeCompleted` 박힘 + S_HandshakeResult(ok=true) 회신.
    // **M4.1 Phase 02 변경**: EnterGameWorld() 직접 호출 *제거*. handshake = 상태 전이만.
    //   클라이언트 선택 전 월드 진입 차단 (P0-1 봉합). EnterGameWorld 호출은 EnterGameWorldIfReady()로만.
    // **테스트 mock 역할도 겸함**: lifecycle/rate-limit 테스트는 handshake + class 선택 양쪽 우회 필요.
    //   TestGameSession에서 CompleteHandshakeAndEnter() + SetCharacterClass(0) + EnterGameWorldIfReady() 연속 호출.
    // **M3 Phase 03 (헌법 #4 봉합)**: protected internal — HandshakeHandler(internal) + 테스트 서브클래스(protected) 양쪽.
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
        // EnterGameWorld() 직접 호출 제거 (M4.1 Phase 02 P0-1 봉합).
        // 월드 진입은 CharacterSelectHandler → EnterGameWorldIfReady() 경로로만 허용.
    }

    // M4.1 Phase 02 (P0-1 + P0-2 봉합): idempotent 월드 진입 게이트.
    // handshake 완료 + class 선택 완료, 두 조건 *모두* 충족 시에만 EnterGameWorld() 호출.
    // **idempotent**: 두 번 호출해도 EnterGameWorld가 한 번만 실행됨 (_enteredWorld flag).
    //   CharacterSelectHandler에서 SetCharacterClass 후 호출, HandshakeHandler에서는 호출 X.
    // **헌법 #5 정합**: sync 코드만, await/Task.Delay 없음 (EnterGameWorld 내부는 EnqueueJob으로 tick thread 마샬링).
    // **race 안전**: 두 패킷(C_Handshake, C_CharacterSelect)이 다른 순서로 도착해도 두 조건 모두 충족 후에만 진입.
    bool _enteredWorld;

    protected internal void EnterGameWorldIfReady()
    {
        if (!_handshakeCompleted || !HasSelectedClass || _enteredWorld) return;
        _enteredWorld = true;
        EnterGameWorld();
    }

    // M3 Phase 03 (헌법 #4 봉합): HandshakeHandler 외부 추출 시 mismatch 거절 경로 캡슐화.
    // 이전엔 GameSession.HandleHandshake 안에서 inline (S_HandshakeResult(ok=false) Send + Disconnect).
    // 변경 후엔 핸들러가 검증 + reason 만 만들고 본 메서드 호출 — Send/Disconnect 두 흐름은 session 안.
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

    // M3 Phase 03 (헌법 #4 봉합): MoveIntentHandler 외부 추출 시 rate-limit + invalid 정규화 +
    // tick 마샬링 캡슐화. 핸들러는 decode + InputBits.Decode만, 본 메서드는 헌법 #3 trust boundary +
    // tick thread 마샬링 책임.
    //
    // **매개변수**:
    //   inputX/jumpPressed = InputBits.Decode가 정규화한 값 (invalid 시 inputX=0 자동)
    //   inputBitsValid = decode가 valid 플래그로 반환한 값 (false면 cheat 로그)
    //   rawInput = 원본 byte (cheat 로그에 0x.. 박을 때만 사용)
    //   clientTick = entity.LastClientTick 기록용
    internal void SubmitMoveIntent(sbyte inputX, bool jumpPressed, bool inputBitsValid, byte rawInput, uint clientTick)
    {
        // Rate-limit 검사 (M4.3R Phase 04: IntentRateLimiter 위임).
        // trust-boundary invariant: 동일 입력에 동일 거부/허용 — 로직은 IntentRateLimiter로 이전됨.
        if (!_rateLimiter.TryConsume(out bool firstWarn))
        {
            // Phase 09 (M2.5): fail-closed drop. 윈도우당 1회만 로그 (폭주 방지).
            if (firstWarn)
            {
                Console.WriteLine(
                    $"[Cheat] Player {_entityId}: intent rate exceeded {IntentRateLimiter.LimitPerSecond}/s — dropping intent (first warning this window)");
            }
            return; // 임계 초과 intent는 tick queue 진입 X.
        }

        if (!inputBitsValid)
        {
            // Codex 함정 #1: invalid `11` reserved 패턴 — cheat 또는 protocol mismatch.
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
            entity.PendingInputX = capturedInputX;
            entity.PendingJumpPressed = capturedJump;
            entity.LastClientTick = capturedClientTick;
        });
    }

    // M3 Phase 06 Step 5 (응급 전투): AttackHandler 외부 추출 시 tick 마샬링 캡슐화.
    // M4.1 Phase 06 (4단계): attackerClientTick 파라미터 추가 — lag compensation rewind 지원.
    //
    // 핸들러는 decode + targetEntityId + attackerClientTick만, 본 메서드는 헌법 #3 trust boundary 진입 게이트 +
    // tick thread 마샬링 책임 (SubmitMoveIntent 패턴 정합).
    //
    // **헌법 #3 (Trust Boundary) — attacker 강제**: 패킷에 attacker 필드 *없음*. attacker는 본 메서드가
    // `_entityId`에서 강제 — 다른 entityId 도용 차단. Codex β 사전 검증 HIGH #2 봉합 정합.
    //
    // **attackerClientTick 신뢰 경계**: 클라가 보낸 tick 값 — untrusted. ProcessAttack step 4.5에서
    //   범위 검증(음수/미래/200ms 초과) 후 silent drop. 여기선 그대로 전달만.
    //
    // **handshake 미완 방어**: 이론상 `OnRecvPacket`의 first-packet 게이트가 잡아 본 메서드 진입
    // 안 되지만, 방어적으로 `_entityId < 0` 검사 (SubmitMoveIntent 정합 패턴). EnterGameWorld
    // 안 끝난 race window에서도 안전.
    //
    // **헌법 #5 정합**: mutation은 EnqueueJob 람다 안 — GameMap.ProcessAttack이 tick thread에서
    // 검증 + S_HitResult/S_EntityDeath broadcast 처리.
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

    // M3 Phase 03 (헌법 #4 봉합): PingHandler 외부 추출 시 Pong Send 캡슐화.
    // 핸들러는 decode + clientTimestampMs만, 본 메서드는 serverTimestampMs 박고 Send.
    internal void RespondPong(long clientTimestampMs)
    {
        S_Pong pong = new S_Pong
        {
            clientTimestampMs = clientTimestampMs,
            serverTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        Send(pong.Write());
    }

    // M4.2 Phase 03: C_EnterPortal 수신 시 EnterPortalHandler가 호출하는 캡슐화 메서드.
    //
    // **헌법 #1 (Server Authority)**: 목적지/spawn 좌표는 PortalTable이 결정. 클라는 portalId만 보냄.
    // **헌법 #3 (Trust Boundary)**: 아래 검증 순서 (tick thread에서 실행):
    //   1. portalId가 현재 맵의 유효 portal인가 (범위 검증) → 아니면 silent drop
    //   2. 플레이어 위치가 portal 좌표 근처인가 (2 unit 임계) → 멀면 silent drop (텔레포트 핵 차단)
    //   3. _entityId < 0 방어 (EnterGameWorld 미완료 race)
    // **헌법 #5 (틱 블로킹 금지)**: EnqueueJob 람다로 tick thread 동기 처리. await/DB/Sleep 금지.
    //
    // **맵 간 마샬링 방식** (Map=Actor 원칙):
    //   맵 A의 tick thread에서 RemovePlayer + S_PlayerLeave broadcast.
    //   그 후 맵 B.EnqueueJob으로 AddPlayerWithId (id 유지 — ADR-026) + S_PlayerJoin broadcast.
    //   한 맵의 tick thread가 다른 맵 상태를 직접 mutate하지 않음 (message channel만).
    //
    // **Transient drop 처리**:
    //   RemovePlayer(맵 A) 직후 ~ AddPlayerWithId(맵 B) 완료 직전 사이,
    //   _migrating = 1로 세팅 → GetMap() null 반환 → 이 사이 도착하는 attack/move는 자동 no-op.
    //   AddPlayerWithId 완료 후 _migrating = 0 reset + _currentMapId 갱신 → 이후 패킷 정상 처리.
    //
    // **portal 근접 임계 2 unit 이유**:
    //   PortalTable 좌표(예: Town x=20)에서 플레이어가 spawn(x=0)에서 걸어옴.
    //   클라이언트는 서버 S_Snapshot으로 보정된 서버 권위 위치를 갖지만,
    //   네트워크 지연 1-2 tick(50-100ms) 내 위치 오차가 최대 ~1.5 unit(MoveSpeed 1.5 × 1tick).
    //   2 unit은 이 오차를 흡수하면서 텔레포트 핵(portal에서 10 unit 이상 떨어진 곳에서 요청)을 차단.
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

        // M4.3R Phase 04: EnqueueJob 람다 본문을 MapMigration.Execute로 추출.
        // 검증 단계(portal lookup·근접)와 transfer 단계(RemovePlayer·AddPlayerWithId·roster 재전송)를
        // 가독 분리. trust-boundary invariant 보존 — 동일 입력에 동일 거부/허용 (헌법 #3).
        // **§1.1 정합**: EnqueueJob 람다 *안*에서 Execute 호출 → tick thread 동기 처리 유지.
        currentMap.EnqueueJob(() =>
            MapMigration.Execute(
                session: self,
                entityId: eid,
                currentMap: currentMap,
                portalId: portalId,
                getDestMap: self.GetDestMap));
    }

    // Phase 10 (M2.5 lifecycle race) 재작성:
    //  - 이전엔 `_entityId < 0` early-return으로 race window cleanup 누락 (γ 감사 위반).
    //  - 이제 *항상* map job 보내고, owner reference 기반으로 cleanup (entityId 모를 때 안전).
    //  - Interlocked.Exchange로 _closing 박고 이중 호출 멱등성 보장.
    //  - 두 번째 OnDisconnected가 와도 enqueue 1회만.
    public override void OnDisconnected(EndPoint endPoint)
    {
        // 이미 닫혔으면 두 번째 호출 — enqueue 안 함 (Codex β 검토 권장: 명시 표현).
        // Exchange 반환값이 1이면 *직전*에 이미 1이었다는 뜻 = 이중 호출.
        if (Interlocked.Exchange(ref _closing, 1) == 1) return;

        Console.WriteLine($"[GameSession] OnDisconnected from {endPoint}");

        GameMap? map = GetMap();
        if (map == null)
        {
            // M4.2 Phase 03: _migrating=1이면 "맵 간 이동 중 disconnect" — 맵 A에서 이미 RemovePlayer됨.
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
            // M3 Phase 04: PlayerLeave broadcast를 위해 *cleanup 전*에 entityId 캡처.
            // 이미 -1이면 (AddPlayer 안 끝남 race) leave broadcast skip.
            int leavingEntityId = self._entityId;

            // owner reference 기반 cleanup — entityId가 race window 안 -1이어도 안전.
            // AddPlayer가 같은 batch에 들어왔으면 그 entity도 같이 제거 (멱등).
            bool removed = map.RemovePlayerBySession(self);

            // M3 Phase 04: 자기 외 남은 player 전원에게 leave broadcast.
            // BroadcastToAll의 IsClosing skip은 *다른* 동시 disconnect 세션 추가 안전망.
            // 자기 자신은 BroadcastToAll의 except로 차단 + 이미 _players에서 빠진 상태(자기 owner X).
            if (removed && leavingEntityId >= 0)
            {
                S_PlayerLeave leaveNotice = new S_PlayerLeave { entityId = leavingEntityId };
                map.BroadcastToAll(leaveNotice.Write(), except: self);
            }

            Console.WriteLine($"[Map] Session cleanup (entityId={leavingEntityId}, removed={removed})");
            // Codex β 권장(Phase 10): cleanup 후 _entityId reset.
            // 기능상 ghost는 막혔지만 낡은 id가 로그/방어 로직에 남는 것 차단.
            self._entityId = -1;
        });
    }

    public override void OnSend(int numOfBytes)
    {
        // M3.8 Phase 05 시연 검증 시점 봉합: 본 로그는 N=2 환경에서 40+/sec 박혀
        // 콘솔 spam → 누적 결함 처럼 보임 (실제는 정상 빈도). 디버그 필요 시 verbose
        // 게이트 또는 M5+ Serilog 도입 시 Trace 레벨에서. 봉합 결정 서버 SubAgent
        // 진단 1순위 정합 (호출 빈도 자체는 정상, 로그 verbose만 결함).
    }

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        ushort packetId = BinaryPrimitives.ReadUInt16LittleEndian(
            new ReadOnlySpan<byte>(buffer.Array!, buffer.Offset + 2, 2));
        PacketID id = (PacketID)packetId;

        // M3.8 Phase 05: 패킷 종류 추적 로그. 단 spam 패킷 (C_MoveIntent 20Hz × N 클라 / C_Ping 자주)은
        // 제외 — OnSend 시각 spam 결함 패턴 반복 차단. Handshake/Attack/Unknown 등 드문 패킷만 박음
        // (디버그 가치 ↑, 콘솔 가독성 유지). M5+ Serilog 도입 시 Trace 레벨에서 모든 패킷 박을 예정.
        if (id != PacketID.C_MoveIntent && id != PacketID.C_Ping)
        {
            Console.WriteLine($"[GameSession] OnRecv {id} ({buffer.Count} bytes)");
        }

        // M3 Phase 02 (헌법 #2 봉합): first-packet 강제. handshake 통과 전엔 다른 dispatch X.
        // **M3 Phase 03**: dispatch는 HandlerRegistry로 위임하되 게이트는 session 책임 (lifecycle 캡슐화).
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

        // M3 Phase 02 (Codex review #5): handshake 통과 후 재-handshake는 protocol violation.
        // 헌법 #2 "Protocol is Sacred" 정합 — silent drop보다 명시적 거절이 진단 가치 ↑.
        // **M3 Phase 03**: dispatch table 진입 전 게이트로 박음 — duplicate 검사가 handler 책임 아님.
        if (id == PacketID.C_Handshake)
        {
            Console.WriteLine($"[Trust] Duplicate C_Handshake after handshake completed — protocol violation, disconnecting");
            Disconnect();
            return;
        }

        // M3 Phase 03 (헌법 #4 봉합): if-else 체인 / switch 제거, Dictionary dispatch.
        // 새 핸들러 추가 = HandlerRegistry._handlers에 한 줄 등록. 누락 시 unknown drop.
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
