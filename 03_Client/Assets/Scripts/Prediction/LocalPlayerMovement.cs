#nullable enable
using System;
using Dawnholder.Client.Network;
using Dawnholder.Client.Scenes;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;

namespace Dawnholder.Client.Prediction
{
    // 로컬 플레이어 이동 + 예측 + 서버 응답 흡수 전담 컴포넌트.
    //
    // **흐름**:
    //   - **매 frame**: Predict (Time.deltaTime 가변) + transform 갱신. 시뮬 자체가 부드러움.
    //     클라 가변 dt + 서버 fixed dt 차이는 reconcile로 흡수.
    //   - **50ms cadence** (송신 throttle): C_MoveIntent 송신 + InputHistory push.
    //     fps 의존 차단 = *송신 cadence* 의미. Predict 자체는 가변 OK.
    //   - **OnJump 에지 검출**: "started" phase만 캡처 → 송신 cycle까지 보관 후 reset.
    //
    // **직업 이동값**: PlayerStats.ForClass 단일 출처 (헌법 #4).
    //   PlayerPrefs "SelectedCharacterClass" 읽어 ForClass에 전달.
    public class LocalPlayerMovement : MonoBehaviour
    {
        public static LocalPlayerMovement? Instance { get; private set; }

        PlayerPredictor _predictor = null!; // Awake에서 초기화

        bool _jumpEdgeThisTick; // 송신 cycle까지 jump 에지 보관. 송신 후 reset.

        uint _localTickCounter; // 송신 일련번호 (송신 시점에만 ++). replay reconcile 기준점.
        float _sendAccumulator; // 50ms 송신 throttle 누적기.

        sbyte _currentMoveX; // LocalPlayerInput이 SetMoveX로 갱신.

        void Awake()
        {
            Instance = this;

            MoveParams move = ResolveClassMoveParams();
            _predictor = new PlayerPredictor(move);

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
                int mapId = UnityClientSession.PendingMapId;
                UnityClientSession.ConsumePendingSpawn();

                InjectTerrain(mapId);
                SetServerPosition(new Vector3(x, y, 0f));
                Debug.Log($"[LocalPlayer] spawn 적용: ({x:F2}, {y:F2}) mapId={mapId}");
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // terrain 주입 단일 경로 — Awake pending 소비 + EnterMapHandler instance 분기(ADR-027
        // 첫 진입 두 순서 모두 관측) 둘 다 여기로. fail loud: 파일 부재/CRC 실패 시 예외 전파
        // (이전 맵 terrain으로 예측하는 드리프트보다 시끄러운 실패 우선).
        public void InjectTerrain(int mapId)
        {
            try
            {
                MapTerrain terrain = ClientTerrainStore.Load(mapId);
                _predictor.SetTerrain(terrain);
                Debug.Log($"[LocalPlayer] terrain 주입 mapId={mapId} (solids={terrain.Solids.Length})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalPlayer] terrain 로드 실패 mapId={mapId}: {ex.Message}");
                throw;
            }
        }

        // LocalPlayerInput이 입력 번역 후 호출. sbyte(-1/0/1).
        public void SetMoveX(sbyte x) => _currentMoveX = x;

        // LocalPlayerInput이 점프 입력 시점 접지 게이트 통과 후 호출.
        // 송신 cycle까지 에지를 보관 — Update가 바로 reset하지 않도록 이 메서드가 박기만 함.
        public void RequestJump() => _jumpEdgeThisTick = true;

        // LocalPlayerInput의 점프 게이트용. 입력 *시점* 접지 여부를 정확히 반영.
        public bool OnGround => _predictor.OnGround;

        void Update()
        {
            // 매 frame Predict. 시뮬 자체가 부드러움.
            // jumpEdge는 송신 cycle까지 *보관* (송신 시점에 한 번 더 사용) — Predict는 매 frame이라
            // OnJump 이후 50ms 안 모든 frame에 jumpEdge=true 들어가면 *재점프* 시도. 단 Physics.Step의
            // OnGround 안전망이 1tick만 적용 — 점프 후 즉시 onGround=false라 자연 차단.
            _predictor.Predict(_currentMoveX, _jumpEdgeThisTick, Time.deltaTime);
            transform.position = new Vector3(_predictor.Position.x, _predictor.Position.y, 0f);

            // 50ms 송신 throttle — fps 의존 차단 (고프레임도 20 packet/s).
            _sendAccumulator += Time.deltaTime;
            if (_sendAccumulator < Constants.TickDuration) return;
            _sendAccumulator -= Constants.TickDuration;

            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null) return;

            // 50ms cadence: 송신 + InputHistory push + jumpEdge reset.
            // 점프 게이트는 LocalPlayerInput.OnJump에서 박힘 (입력 시점 OnGround 검사 = 정확).
            bool jumpEdge = _jumpEdgeThisTick;
            _jumpEdgeThisTick = false; // 송신 후 reset — 다음 cycle은 새 OnJump 캡처.

            _localTickCounter++;

            // 비트필드 인코드 (InputBits.Encode 단일 출처).
            byte input = InputBits.Encode(_currentMoveX, jumpEdge);
            C_MoveIntent pkt = new C_MoveIntent
            {
                input = input,
                clientTick = _localTickCounter
            };
            // SendIntent 경유 — Editor에서 SimulatedLatencyMs 적용 가능.
            session.SendIntent(pkt.Write());

            // 송신 *직후* InputHistory push (ack 전 빔 함정 회피). jumpEdge 함께 박아 replay 시 재현.
            _predictor.NotifySent(_localTickCounter, _currentMoveX, jumpEdge);
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

        // PlayerPrefs 선택 클래스 → MoveParams 변환.
        // PlayerStats.ForClass 단일 출처 (헌법 #4) — 직업 분기 책임은 ForClass에 위임.
        static MoveParams ResolveClassMoveParams()
        {
            int classValue = UnityEngine.PlayerPrefs.GetInt(
                CharacterSelectController.SelectedClassPrefsKey, (int)Shared.Protocol.CharacterClass.Warrior);

            PlayerStats stats = PlayerStats.ForClass((Shared.Protocol.CharacterClass)classValue);
            return new MoveParams(stats.MoveSpeed, stats.JumpVel);
        }
    }
}
