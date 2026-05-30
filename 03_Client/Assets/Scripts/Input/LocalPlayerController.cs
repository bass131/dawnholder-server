#nullable enable
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Input
{
    // 입력 → C_MoveIntent 송신 + client-side prediction.
    //
    // **흐름**:
    //   - **매 frame**: Predict (Time.deltaTime 가변) + transform 갱신. 시뮬 자체가 부드러움.
    //     클라 가변 dt + 서버 fixed dt 차이는 reconcile로 흡수.
    //   - **50ms cadence** (송신 throttle): C_MoveIntent 송신 + InputHistory push.
    //     fps 의존 차단 = *송신 cadence* 의미. Predict 자체는 가변 OK.
    //   - **OnJump 에지 검출**: "started" phase만 캡처 → 송신 cycle까지 보관 후 reset.
    //
    // **비트필드 인코드**: InputBits.Encode 단일 출처 — 양쪽 같은 헬퍼 호출.
    [RequireComponent(typeof(PlayerInput))]
    public class LocalPlayerController : MonoBehaviour
    {
        public static LocalPlayerController? Instance { get; private set; }

        readonly PlayerPredictor _predictor = new PlayerPredictor();

        Vector2 _moveInput;
        bool _jumpEdgeThisTick; // 송신 cycle까지 jump 에지 보관. 송신 후 reset.

        uint _localTickCounter; // 송신 일련번호 (송신 시점에만 ++). replay reconcile 기준점.
        float _sendAccumulator; // 50ms 송신 throttle 누적기.

        void Awake()
        {
            Instance = this;

            // 맵 전환 후 pending spawn 좌표 소비. S_MapTransition 핸들러가 박아둔 spawn 좌표를 읽어 위치 설정.
            //
            // **Awake에서 소비하는 이유 (race 봉합)**:
            //   Start()에서 하면 서버의 첫 S_Snapshot이 Start()보다 먼저 처리되는 race가 생김.
            //   그 순간 predictor가 아직 (0,0)이라 서버 spawn 좌표로 reconcile snap이 발생 →
            //   맵 전환 직후 캐릭터가 튐. Awake는 Instantiate 즉시(같은 프레임) 호출 → 첫 snapshot
            //   처리보다 확실히 먼저 위치를 잡아 snap을 제거.
            if (UnityClientSession.HasPendingSpawn)
            {
                float x = UnityClientSession.PendingSpawnX;
                float y = UnityClientSession.PendingSpawnY;
                UnityClientSession.ConsumePendingSpawn();
                SetServerPosition(new Vector3(x, y, 0f));
                Debug.Log($"[LocalPlayer] 맵 전환 spawn 적용: ({x:F2}, {y:F2})");
            }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        // Input System "Move" 액션 콜백.
        void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();

        // "Jump" 액션 콜백 — 클라 에지 ("started" phase만 캡처).
        // PlayerInput component의 Behavior=Send Messages 모드에서 이 메서드명이 자동 wire.
        // 송신 cycle 전에 다시 누르면 같은 에지로 합쳐짐 (정상 — cadence별 1 점프).
        //
        // 공중 점프 차단은 점프 입력 *시점* OnGround 검사 (착지 직후 재점프 OK, 공중 점프 차단).
        // cadence 시점에 검사하면 Predict 후 OnGround=false 박혀서 지면 점프도 차단됨.
        // 헌법 #1 영향 X — 서버가 어차피 권위적으로 재검증, 본 게이트는 UX + 송신 절감용.
        void OnJump(InputValue value)
        {
            if (value.isPressed && _predictor.OnGround) _jumpEdgeThisTick = true;
        }

        // "Attack" 액션 콜백 (Space 또는 좌클릭).
        //
        // **클라 책임 = target 추천 + intent 송신만** (헌법 #1):
        //   - 가장 가까운 enemy/boss → C_Attack { targetEntityId, attackerClientTick } 송신.
        //   - 데미지/range/cooldown *서버가 최종 검사* — 클라 자체 판정 X.
        //   - 자체 rate-limit 없음 (서버가 silent drop).
        //
        // **TargetingRangeSquared = 9.0f** — 클라 측 *타게팅 힌트* (3.0f 사거리의 제곱).
        //   어느 적을 C_Attack target으로 지명할지 결정하는 UX 용도.
        //   서버 권위 판정(AABB hitbox in CombatConstants)과 *의도적으로 분리*된 별개 개념 —
        //   서버가 최종 hit/miss 결정. 헌법 #1/#4 정합 — 밸런스 수식 복붙 X, 클라 UX 힌트.
        //   서버 AABB halfExtent(1.5) + 적 반경(0.5) ≈ 2 units 기준 TargetingRange 3.0f는 여유분 포함.
        const float TargetingRangeSquared = 9.0f;

        void OnAttack(InputValue value)
        {
            if (!value.isPressed) return; // up edge 무시 — down 시점 한 번만.

            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null) return;
            if (EnemyRegistry.Instance == null) return;

            // 본인 position 기준 nearest enemy/boss 결정 (타게팅 힌트 — 서버 판정과 독립).
            Vector3 origin = transform.position;
            if (!EnemyRegistry.Instance.TryGetNearest(origin, TargetingRangeSquared, out int targetEntityId))
            {
                // 타게팅 범위 내 enemy 없음 — silent (서버가 어차피 최종 판정).
                return;
            }

            // attackerClientTick = 마지막으로 수신한 S_Snapshot의 serverTick (lag comp 기준점).
            // 서버 ProcessAttack이 이 tick으로 position history를 rewind해 hitbox 판정.
            // 검증 규칙: tick < 0 || > 현재서버tick || (현재서버tick - tick) > 4 → silent drop.
            // 첫 Snapshot 수신 전(= 0) 공격은 drop되지만 게임 극초반이라 실전 영향 없음.
            C_Attack pkt = new C_Attack
            {
                targetEntityId = targetEntityId,
                attackerClientTick = session.LastReceivedServerTick
            };
            session.SendIntent(pkt.Write());
            Debug.Log($"[Attack] → target entity {targetEntityId} clientTick={pkt.attackerClientTick}");
        }

        void Update()
        {
            // 매 frame Predict. 시뮬 자체가 부드러움.
            // jumpEdge는 송신 cycle까지 *보관* (송신 시점에 한 번 더 사용) — Predict는 매 frame이라
            // OnJump 이후 50ms 안 모든 frame에 jumpEdge=true 들어가면 *재점프* 시도. 단 Physics.Step의
            // OnGround 안전망이 1tick만 적용 — 점프 후 즉시 onGround=false라 자연 차단.
            sbyte encoded = EncodeInputX(_moveInput.x);
            _predictor.Predict(encoded, _jumpEdgeThisTick, Time.deltaTime);
            transform.position = new Vector3(_predictor.Position.x, _predictor.Position.y, 0f);

            // 50ms 송신 throttle — fps 의존 차단 (고프레임도 20 packet/s).
            _sendAccumulator += Time.deltaTime;
            if (_sendAccumulator < Constants.TickDuration) return;
            _sendAccumulator -= Constants.TickDuration;

            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null) return;

            // 50ms cadence: 송신 + InputHistory push + jumpEdge reset.
            // 점프 게이트는 OnJump에서 박힘 (입력 시점 OnGround 검사 = 정확).
            bool jumpEdge = _jumpEdgeThisTick;
            _jumpEdgeThisTick = false; // 송신 후 reset — 다음 cycle은 새 OnJump 캡처.

            _localTickCounter++;

            // 비트필드 인코드 (InputBits.Encode 단일 출처).
            byte input = InputBits.Encode(encoded, jumpEdge);
            C_MoveIntent pkt = new C_MoveIntent
            {
                input = input,
                clientTick = _localTickCounter
            };
            // SendIntent 경유 — Editor에서 SimulatedLatencyMs 적용 가능.
            session.SendIntent(pkt.Write());

            // 송신 *직후* InputHistory push (ack 전 빔 함정 회피). jumpEdge 함께 박아 replay 시 재현.
            _predictor.NotifySent(_localTickCounter, encoded, jumpEdge);
        }

        // S_EnterMap → 서버가 정한 spawn 좌표 적용. predictor 초기화 — 다음 Update에서 transform 자동 동기.
        // 단 spawn 첫 frame 깜빡임 방지를 위해 즉시 transform도 한 번 설정.
        public void SetServerPosition(Vector3 worldPos)
        {
            _predictor.SetInitialPosition(new Vector2(worldPos.x, worldPos.y));
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        }

        // 맵 전환 시 옛 LocalPlayer를 snapshot/Update에서 분리. HandleMapTransition이 씬 전환 시작 전 호출.
        //
        // 이 GameObject는 페이드 동안 아직 살아있어, 위치를 (0,0)으로 박으면 도착한 S_Snapshot이
        // 서버의 새 맵 좌표로 reconcile snap → 전환 직후 캐릭터가 튐. 새 맵 LocalPlayer는 별도 인스턴스 +
        // 깨끗한 predictor라 옛 버퍼 리셋 자체가 불필요. 위치는 건드리지 않고:
        //   1) Instance 등록 해제 — HandleSnapshot의 `Instance != null` 가드로 이후 snapshot이 drop.
        //   2) enabled=false — Update 정지 (predict/transform 갱신 중단).
        // 곧 씬 전환(LoadScene Single)이 이 GameObject를 파괴하고, 새 맵에서 새로 spawn됨.
        public void ResetPredictionForMapTransition()
        {
            if (Instance == this) Instance = null;
            enabled = false;
            Debug.Log("[LocalPlayer] 맵 전환 — 옛 LocalPlayer를 snapshot/Update에서 분리 (곧 파괴).");
        }

        // S_Snapshot → predictor의 reconcile 판단에 위임. Predictor가 X+Y 둘 다 비교.
        public void OnServerSnapshot(float serverX, float serverY,
                                     float serverVx, float serverVy,
                                     int serverTick, uint ackedClientTick)
        {
            float prevX = _predictor.Position.x;
            float prevY = _predictor.Position.y;
            bool reconciled = _predictor.OnSnapshot(
                serverX, serverY, serverVx, serverVy, ackedClientTick);
            if (reconciled)
            {
                float dx = serverX - prevX;
                float dy = serverY - prevY;
                Debug.Log(
                    $"[Reconcile] d=({dx:F2}, {dy:F2}) at serverTick={serverTick} " +
                    $"ack={ackedClientTick} (count={_predictor.SnapCount})");
            }
        }

        // Vector2(아날로그 가능) → sbyte(-1/0/1) 변환.
        // 임계값 0.5 — 게임패드 아날로그 스틱 미세 흔들림 차단.
        static sbyte EncodeInputX(float x)
        {
            if (x > 0.5f) return 1;
            if (x < -0.5f) return -1;
            return 0;
        }
    }
}
