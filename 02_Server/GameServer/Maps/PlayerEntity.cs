using System.Numerics;
using Dawnholder.Server.GameServer.Sessions;
using Shared.GameData;

namespace Dawnholder.Server.GameServer.Maps;

// Phase 02 (M2): 서버 권위 좌표를 가진 플레이어 1명을 표현하는 entity.
// Phase 03: GameMap.AddPlayer로 생성, OnDisconnected 시 RemovePlayer.
// Phase 04: 이동 intent 누적 필드 추가. tick thread에서만 mutate.
// M3 Phase 06 (응급 전투 Step 1): 전투 상태(Hp/MaxHp/IsDead) + rate-limit 타임스탬프 추가.
//   - 헌법 #1 (Server Authority): Hp는 *서버만* mutate. 클라는 S_HitResult로 받은 값을 표시만.
//   - 응집도 trade-off: combat state(Hp)를 movement state(Position/Velocity)와 같은 entity에
//     박을지 분리할지 — 응급은 같은 entity에 박음(*combat 컴포넌트 분리는 M4+*).
//   - LastAttackTickMs는 Step 5(AttackHandler rate-limit, 500ms silent drop)에서 검사 예정,
//     본 Step에선 0으로 초기화만.
// M4.1 Phase 06 (1단계): position history ring buffer 추가 — lag compensation rewind 지원.
//   - 4-tick 깊이 (= 200ms @20TPS). tick thread invariant — lock 없음 (GameMap actor 정합).
//   - RecordPosition: Physics.Step 후 매 tick 호출 → 그 tick의 실제 위치 박힘.
//   - GetPositionAtTick: rewind lookup. 못 찾으면 현재 Position fallback (cheat 차단 정합).
//
// Position은 System.Numerics.Vector2. Unity의 UnityEngine.Vector2와 메모리 레이아웃은
// 같지만 타입은 다름 — 패킷 직렬화 시 (float x, float y) 두 필드로 풀어서 전송.
public class PlayerEntity
{
    public int EntityId { get; }
    public Vector2 Position { get; set; }
    public GameSession? Owner { get; }

    // M4.1 Phase 05 (2단계): 서버 권위 스탯. GameSession.SetCharacterClass가 생성해 AddPlayer에 전달.
    // null이면 PlayerStats.Warrior() 응급 default 박힘 (M5 영속화 도입 시 DB 로드 스탯으로 교체 backlog).
    // **헌법 #1 (Server Authority)**: 스탯 수치는 서버가 결정 — 클라이언트가 스탯 값 직접 전송 경로 없음.
    public PlayerStats Stats { get; }

    // Phase 07: 결정론 물리 상태 — Shared.GameData.Physics.Step이 매 tick mutation.
    // spawn 시점 Velocity=0 + OnGround=true (ground y=0 가정).
    public Vector2 Velocity { get; set; } = Vector2.Zero;
    public bool OnGround { get; set; } = true;

    // Phase 04: 다음 tick에 적용할 입력. 단일 thread(tick) mutation 보장 +
    // OnRecvPacket이 EnqueueJob으로 set하므로 동시성 안전.
    // Phase 07: jumpPressed 추가 (D4 (a) 클라 에지 — 1tick만 true).
    public sbyte PendingInputX { get; set; }
    public bool PendingJumpPressed { get; set; }
    public uint LastClientTick { get; set; }

    // M3 Phase 06 Step 1 (combat state): 응급 전투 HP. 헌법 #1 — 서버만 mutate.
    // 기본 100/100. `IsDead`는 derived: `Hp <= 0`. 음수 보호는 derived가 흡수
    // (`Hp = -5` 직접 set도 IsDead true이므로 후속 attack job이 idempotent하게 no-op).
    public int Hp { get; set; } = 100;
    public int MaxHp { get; set; } = 100;
    public bool IsDead => Hp <= 0;

    // M3 Phase 06 Step 1 (rate-limit hook): 마지막 공격 발생 tick(ms 단위) 기록.
    // Step 5에서 `AttackHandler`가 (now - LastAttackTickMs >= 500ms) 검사로 silent drop 판정.
    // 본 Step에선 필드 박힘만 — 갱신/검사 로직은 Step 5에서 추가.
    public long LastAttackTickMs { get; set; }

    // M4.1 Phase 06 (1단계): position history ring buffer.
    //
    // **구조 선택 이유** — 별도 (long tick, Vector2 pos) 쌍 배열 vs 단일 struct 배열:
    //   - 단일 struct: 메모리 지역성 ↑ (tick+pos 한 캐시라인), 어느 슬롯이 어느 tick인지 한 번에 파악.
    //   - 단점: struct 정의 추가. 학부생 호흡엔 두 배열 분리가 직관적.
    //   → 여기선 struct 없이 parallel 배열 2개 패턴 채택 (단순함 + 성능 차이 무시 가능, 4 슬롯 고정).
    //
    // **tick thread invariant**: RecordPosition / GetPositionAtTick 은 GameMap.Tick 안에서만 호출.
    //   외부 스레드(network thread)는 ring buffer를 직접 만지지 않음 — EnqueueJob으로 tick thread에 위임.
    //   따라서 lock 불필요 (GameMap actor 모델 정합, 헌법 #5).
    //
    // **깊이 = 4 슬롯** = 4 tick = 200ms @20TPS.
    //   5 tick 전 attackerClientTick은 rewind 범위 검증(ProcessAttack step 4.5)에서 silent drop.
    const int HistorySize = 4;
    readonly Vector2[] _posHistory = new Vector2[HistorySize];
    readonly long[] _posHistoryTick = new long[HistorySize];
    int _posHistoryHead; // 다음 쓰기 index (0~3 회전)

    /// <summary>
    /// M4.1 Phase 06 (1단계): tick thread에서 매 Physics.Step 직후 호출.
    /// head 위치에 (tick, pos) 박고 head를 1 전진.
    /// </summary>
    public void RecordPosition(long serverTick, Vector2 pos)
    {
        _posHistory[_posHistoryHead] = pos;
        _posHistoryTick[_posHistoryHead] = serverTick;
        _posHistoryHead = (_posHistoryHead + 1) % HistorySize;
    }

    /// <summary>
    /// M4.1 Phase 06 (1단계): attackerClientTick 시점의 위치를 ring buffer에서 조회.
    ///
    /// **fallback 정책**: tick을 찾지 못하면 현재 <see cref="Position"/> 반환.
    ///   - 범위 밖(덮어쓰여짐) → 현재 위치 사용 = 보수적 처리.
    ///   - 범위 검증은 ProcessAttack 호출 전에 이미 완료 (attackerClientTick ≤ currentTick - 0..4
    ///     범위만 도달) — fallback 빈도는 낮지만 방어망 유지.
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

    // M4.1 Phase 05 (2단계): stats 옵션 인자 추가.
    // null 시 PlayerStats.Warrior() 응급 default — M3.8 정합 (전사 기본값).
    // GameSession.EnterGameWorld에서 _stats(non-null 보장 — EnterGameWorldIfReady 가드) 전달 예정.
    public PlayerEntity(int entityId, Vector2 position, GameSession? owner = null, PlayerStats? stats = null)
    {
        EntityId = entityId;
        Position = position;
        Owner = owner;
        Stats = stats ?? PlayerStats.Warrior();
    }
}
