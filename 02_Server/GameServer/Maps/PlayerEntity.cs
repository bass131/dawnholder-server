using System.Numerics;
using Dawnholder.Server.GameServer.Combat;
using Dawnholder.Server.GameServer.Maps.States;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps;

// 서버 권위 좌표를 가진 플레이어 1명을 표현하는 entity. tick thread에서만 mutate.
//   - 헌법 #1 (Server Authority): Hp는 *서버만* mutate. 클라는 S_HitResult로 받은 값을 표시만.
//   - position history ring buffer: lag compensation rewind 지원.
//     tick thread invariant — lock 없음 (GameMap actor 정합).
//
// Position은 System.Numerics.Vector2. Unity의 UnityEngine.Vector2와 메모리 레이아웃은
// 같지만 타입은 다름 — 패킷 직렬화 시 (float x, float y) 두 필드로 풀어서 전송.
public class PlayerEntity
{
    public int EntityId { get; }
    public Vector2 Position { get; set; }
    public GameSession? Owner { get; }

    // 서버 권위 스탯. GameSession.SetCharacterClass가 생성해 AddPlayer에 전달.
    // **헌법 #1 (Server Authority)**: 스탯 수치는 서버가 결정 — 클라이언트가 스탯 값 직접 전송 경로 없음.
    public PlayerStats Stats { get; }

    // 결정론 물리 상태 — Shared.GameData.Physics.Step이 매 tick mutation.
    // spawn 시점 Velocity=0 + OnGround=true (ground y=0 가정).
    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public bool OnGround { get; set; } = true;

    // jump buffer: 공중에서 받은 점프 입력을 착지 틱까지 보관 (최대 1개).
    // TTL = JumpBufferTicks 이후 자연 소멸 — max1+유한TTL로 무한/유령 점프 불가 (헌법 #3).
    const int JumpBufferTicks = 3; // ~150ms @20TPS
    int _jumpBufferRemaining;

    // 테스트용 read-only 노출.
    public bool HasBufferedJump => _jumpBufferRemaining > 0;

    // 이번 틱 실제 점프 여부 결정 + 버퍼 상태 갱신.
    // Physics.cs는 수정 금지(공유 공식) — 서버가 Physics.Step에 넘기는 jumpPressed를 여기서 정한다.
    public bool ResolveJump(bool rawJumpPressed)
    {
        if (OnGround)
        {
            bool fire = rawJumpPressed || _jumpBufferRemaining > 0;
            _jumpBufferRemaining = 0;
            return fire;
        }
        if (rawJumpPressed) _jumpBufferRemaining = JumpBufferTicks; // 공중 입력 → 착지까지 보관 (최신 1개)
        else if (_jumpBufferRemaining > 0) _jumpBufferRemaining--;   // TTL 감소, 만료 시 자연 소멸
        return false;
    }

    // 입력 FIFO 큐. EnqueueJob 경유 network thread→tick thread 단방향이라 lock 불필요.
    // 상한 MaxInputQueue: DoS 방어 (헌법 #3) + 위상 지터 누적 상한.
    // 초과 시 oldest drop (drop-oldest): 최신 입력을 우선해 응답성 유지.
    public readonly struct InputCommand
    {
        public readonly sbyte InputX;
        public readonly bool JumpPressed;
        public readonly uint ClientTick;

        public InputCommand(sbyte inputX, bool jumpPressed, uint clientTick)
        {
            InputX = inputX;
            JumpPressed = jumpPressed;
            ClientTick = clientTick;
        }
    }

    const int MaxInputQueue = 6;
    readonly Queue<InputCommand> _inputQueue = new();

    // 큐 크기 read-only 노출 (단위 테스트용).
    public int InputQueueCount => _inputQueue.Count;

    // 입력 enqueue. 상한 초과 시 oldest drop-oldest (DoS 방어).
    public void EnqueueInput(sbyte inputX, bool jumpPressed, uint clientTick)
    {
        if (_inputQueue.Count >= MaxInputQueue)
        {
            _inputQueue.Dequeue(); // oldest drop (DoS 방어, 헌법 #3)
        }
        _inputQueue.Enqueue(new InputCommand(inputX, jumpPressed, clientTick));
    }

    // 틱 루프에서 1개 dequeue. 없으면 neutral(0,false) 반환, hasInput=false.
    // hasInput=false 틱은 ack 불변 — 적용 안 한 입력을 ack하면 클라 reconcile 무력화.
    public bool TryDequeueInput(out InputCommand cmd)
    {
        if (_inputQueue.TryDequeue(out cmd))
            return true;
        cmd = default;
        return false;
    }

    // ack = 적용 시점 clientTick (받은 시점 아님).
    // 빈 틱(starvation)에는 set 안 함 — 클라 replay할 미-ack 입력 보존 (reconcile 정합).
    public uint LastClientTick { get; set; }

    // 서버 권위 전투 HP. 헌법 #1 — 서버만 mutate. 생성자에서 Stats.MaxHp/Hp로 초기화.
    // `IsDead`는 derived: `Hp <= 0`. 음수 보호는 derived가 흡수
    // (`Hp = -5` 직접 set도 IsDead true이므로 후속 attack job이 idempotent하게 no-op).
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public bool IsDead => Hp <= 0;

    // 마지막 공격 발생 tick(ms 단위) 기록. AttackHandler rate-limit(500ms silent drop) 판정용.
    public long LastAttackTickMs { get; set; }

    // 스킬별 마지막 발동 서버 tick 배열. index = SkillId byte 값.
    // tick 기반: 헌법 #5 — DateTime/ms 타이머 의존 차단. blocking call 0 보장.
    // 고정 배열(per-tick 할당 0, 헌법 §5 정합) — Dictionary는 GC pressure 발생.
    // 초기값 = long.MinValue/2: 스폰 직후 첫 발동 허용 + 오버플로우 회피(MinValue는 currentTick-MinValue가 음수).
    // **신뢰 경계(헌법 §3)**: 미등록 skillId(배열 범위 밖)는 SkillSystem 진입 전 C_SkillUseHandler에서 drop.
    const int SkillSlotCount = 4; // SkillId.Teleport=3이 현재 최대값 + 1
    readonly long[] _lastSkillTick;

    public long GetLastSkillTick(byte skillId)
        => skillId < SkillSlotCount ? _lastSkillTick[skillId] : long.MinValue / 2;

    public void SetLastSkillTick(byte skillId, long tick)
    {
        if (skillId < SkillSlotCount) _lastSkillTick[skillId] = tick;
    }

    // 플레이어가 마지막으로 이동한 수평 방향. +1=오른쪽, -1=왼쪽.
    // 초기값은 +1(오른쪽 기본). Physics.Step에서 inputX != 0인 틱마다 갱신.
    // S_PlayerAttack.facing 필드 직렬화용 (공격 연출 방향) — 위치 권위는 Position 기준.
    public sbyte FacingDir { get; set; } = 1;

    // ActionFsm에서 현재 State가 사용하는 남은 틱 카운터.
    // AttackState: 공격 commit window 잔여 틱. HitState: hitstun 잔여 틱.
    // tick thread invariant (헌법 #5).
    public int StateTicksRemaining { get; set; }

    // 피격 넉백 수평 속도 (units/s). HitState.Tick에서 매 틱 감쇠.
    // 양수=오른쪽, 음수=왼쪽. 0이면 넉백 없음.
    // tick thread invariant (헌법 #5).
    public float KnockbackVx { get; set; }

    // 근접 공격 스윙 전방 lunge 수평 속도 (units/s). AttackState.Tick에서 매 틱 감쇠.
    // KnockbackVx와 상호배타(Attack vs Hit state는 동시 진입 불가) — GameMap.Tick이 둘을 합산해
    // Physics.Step의 ExternalVelX로 전달. 양수=오른쪽, 음수=왼쪽. tick thread invariant (헌법 #5).
    public float AttackLungeVx { get; set; }

    // position history ring buffer.
    //
    // **tick thread invariant**: RecordPosition / GetPositionAtTick 은 GameMap.Tick 안에서만 호출.
    //   외부 스레드(network thread)는 ring buffer를 직접 만지지 않음 — EnqueueJob으로 tick thread에 위임.
    //   따라서 lock 불필요 (GameMap actor 모델 정합, 헌법 #5).
    //
    // **깊이 = 4 슬롯** = 4 tick = 200ms @20TPS.
    //   5 tick 전 attackerClientTick은 rewind 범위 검증(ProcessAttack)에서 silent drop.
    const int HistorySize = 4;
    readonly Vector2[] _posHistory = new Vector2[HistorySize];
    readonly long[] _posHistoryTick = new long[HistorySize];
    int _posHistoryHead; // 다음 쓰기 index (0~3 회전)

    /// <summary>
    /// tick thread에서 매 Physics.Step 직후 호출. head 위치에 (tick, pos) 박고 head를 1 전진.
    /// </summary>
    public void RecordPosition(long serverTick, Vector2 pos)
    {
        _posHistory[_posHistoryHead] = pos;
        _posHistoryTick[_posHistoryHead] = serverTick;
        _posHistoryHead = (_posHistoryHead + 1) % HistorySize;
    }

    /// <summary>
    /// attackerClientTick 시점의 위치를 ring buffer에서 조회.
    ///
    /// **fallback 정책**: tick을 찾지 못하면 현재 <see cref="Position"/> 반환 (보수적 fail-safe).
    ///   범위 검증은 ProcessAttack 호출 전에 이미 완료 — fallback 빈도는 낮지만 방어망 유지.
    /// </summary>
    public Vector2 GetPositionAtTick(long serverTick)
    {
        for (int i = 0; i < HistorySize; i++)
        {
            if (_posHistoryTick[i] == serverTick)
                return _posHistory[i];
        }
        // 못 찾음 — 현재 위치 fallback (헌법 #3 보수적 fail-safe).
        return Position;
    }

    // 플레이어 전체 행동(이동 + 전투) State 머신.
    // Phase 02: 이동 계열(Idle/Move/Jump) + 전투 계열(Attack/Hit/Death) 통합.
    // tick thread invariant: StateMachine.Tick은 GameMap.Tick 안에서만 호출.
    public StateMachine<PlayerEntity> ActionFsm { get; private set; } = null!;

    // ── 전투 전이 API ──────────────────────────────────────────────────────

    // 공격 commit window 진입. IsDead면 no-op.
    // CombatSystem이 공격 성공 직후 호출한다.
    public void EnterAttackState()
    {
        if (IsDead) return;
        ActionFsm.ChangeState(PlayerCombatStates.Attack, this);
    }

    // 피격 hitstun 진입. IsDead 또는 불가침 commit 중이면 no-op (넉백도 없음).
    // dirX: 넉백이 날아갈 방향(= 공격자 반대쪽). 양수=오른쪽, 음수=왼쪽, 0=오른쪽 기본.
    //   호출자(BossBehaviorSystem)가 `player.X >= boss.X ? +1 : -1`로 "공격자 반대 방향"을 계산해 넘긴다.
    public void EnterHitState(float dirX)
    {
        if (IsDead) return;
        if (!ActionFsm.CurrentState.InterruptibleByHit) return;
        // KnockbackVx는 dirX와 같은 부호 — dirX 자체가 이미 "공격자 반대 방향"이다.
        KnockbackVx = Constants.KnockbackInitialVx * MathF.Sign(dirX == 0f ? 1f : dirX);
        ActionFsm.ChangeState(PlayerCombatStates.Hit, this);
    }

    // 부활. ActionFsm을 Idle로 초기화 + 카운터 정리.
    // BossBehaviorSystem의 respawn 처리에서 호출한다.
    public void Revive()
    {
        ActionFsm.ChangeState(PlayerMovementStates.Idle, this);
        StateTicksRemaining = 0;
        KnockbackVx = 0f;
        AttackLungeVx = 0f;
    }

    // stats null 시 PlayerStats.Knight() default (전사 기본값).
    public PlayerEntity(int entityId, Vector2 position, GameSession? owner = null, PlayerStats? stats = null)
    {
        EntityId = entityId;
        Position = position;
        Owner = owner;
        Stats = stats ?? PlayerStats.Knight();
        MaxHp = Stats.MaxHp;
        Hp = Stats.Hp;
        // 스킬 쿨다운 배열 초기화: 스폰 직후 첫 발동 허용 + 오버플로우 회피.
        _lastSkillTick = new long[SkillSlotCount];
        long allowFirst = long.MinValue / 2;
        for (int i = 0; i < SkillSlotCount; i++)
            _lastSkillTick[i] = allowFirst;
        ActionFsm = new StateMachine<PlayerEntity>(PlayerMovementStates.Idle, this);
    }
}
