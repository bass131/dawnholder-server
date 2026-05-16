#nullable enable
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Prediction
{
    // Phase 05 (M2): Client-side prediction + snap reconcile.
    //
    // **순수 C# 클래스 (MonoBehaviour 아님)** — 미래 EditMode 테스트 가능성 보존.
    // Unity 의존은 UnityEngine.Vector2 + Mathf.Abs 두 가지로 한정.
    //
    // **흐름**:
    //   1. spawn 시점: LocalPlayerController가 SetInitialPosition(spawnPos)
    //   2. 매 frame: LocalPlayerController가 Predict(inputX, Time.deltaTime)
    //                → _predictedPosition 누적 → transform.position = Position
    //   3. S_Snapshot 도착 시: UnityClientSession이 OnSnapshot(serverX, serverY)
    //                → |serverX - predictedX| > SnapThreshold면 강제 덮어쓰기 (snap)
    //                → 작으면 prediction 그대로 신뢰
    //
    // **양쪽 공식 일치 (헌법 #1 / ADR-010)**:
    //   MoveSpeed는 오직 Shared.GameData.Constants. 클라 별도 const 박으면 무한 drift.
    //
    // **의도된 한계 (Phase 05)**:
    //   Time.deltaTime은 가변(60/144Hz), 서버는 50ms 고정 → 미세 drift 필연.
    //   snap이 가끔 발생하는 게 정상 — 학습 포인트. Phase 06 input replay로 해소.
    //
    // **비교 축**: 현재는 X만 (좌우 이동만). Phase 07 점프 도입 시 Y도 비교.
    public class PlayerPredictor
    {
        // Phase 05 튜닝 결과 (2026-05-11 1차 검증):
        // 0.5f였을 때 latency 0 상태에서 분당 ~8회 snap (명세 "분당 5회 미만" 초과).
        // Time.deltaTime 가변(60~240Hz) vs 서버 50ms 고정 → 5 tick 누적 시 0.5 유닛 직상 drift가 자연 발생.
        // 1.0f로 올려 정상 drift는 흡수, 진짜 cheat/lag만 잡도록 조정.
        // Phase 06 fixed simulation 도입 후엔 다시 좁힐 여지.
        public const float SnapThreshold = 1.0f;

        public Vector2 Position { get; private set; }
        public int SnapCount { get; private set; }

        // Phase 06: 송신된 입력의 (clientTick, inputX) 보관 → snapshot의 ackedTick 받으면
        // 미-ack 입력만 replay (Step 5에서 OnSnapshot 알고리즘 확장 시 사용).
        // Codex 응집도 가이드: prediction 도메인 상태 = predictor 소유.
        readonly InputHistory _history = new InputHistory();

        public void SetInitialPosition(Vector2 pos)
        {
            Position = pos;
            _history.Clear();
        }

        // Phase 06 Step 4: LocalPlayerController가 C_MoveIntent 송신 직후 호출.
        // *송신 직후*에 push해야 ack 받기 전 비는 위험(Phase 06 정의 #83) 회피.
        public void NotifySent(uint clientTick, sbyte inputX)
        {
            _history.Push(clientTick, inputX);
        }

        // 매 frame 호출. inputX는 -1/0/1 (LocalPlayerController가 EncodeInputX로 인코딩한 값).
        // deltaTime은 Unity Time.deltaTime — 큰 프레임 스파이크 시 서버(50ms 고정)와 drift.
        public void Predict(sbyte inputX, float deltaTime)
        {
            if (inputX == 0) return;
            float dx = inputX * Constants.MoveSpeed * deltaTime;
            Position = new Vector2(Position.x + dx, Position.y);
        }

        // Phase 06 Step 5: S_Snapshot 도착 시 호출 — replay reconcile.
        // 반환값: reconcile 발생 여부 (true면 LocalPlayerController가 "[Reconcile] dx=..." 로깅).
        //
        // **알고리즘 (Phase 06 정의 파일 #34-43)**:
        //   1. mispredict 검사 — 서버 위치 vs 현재 예측 위치, threshold 비교 (Phase 05 단순화 유지)
        //   2. mispredict 시: 서버 위치(=ackedClientTick 시점 권위 좌표)에서 출발해 미-ack 입력만 재시뮬
        //                     → 결과 = "서버 인정 + 클라 미-ack 흡수" 부드러운 정정
        //   3. 항상 InputHistory.EvictUpTo(ackedClientTick)로 정리 (메모리 위생)
        //
        // **Phase 05와의 차이**:
        //   - 옛: mispredict → 서버 위치로 즉시 snap (텔레포트 점프)
        //   - 새: mispredict → 서버 위치에서 출발 + 미-ack replay → 자연 위치
        //
        // **헌법 #1 (Server Authority) 유지**: 클라 cheat 시뮬(dx=-1000)도 여전히 즉시 보정 — replay는
        //                                       *서버 권위 좌표 기준*에서 출발하므로 cheat 흡수 X.
        public bool OnSnapshot(float serverX, float serverY, uint ackedClientTick)
        {
            float dx = serverX - Position.x;
            bool mispredict = Mathf.Abs(dx) > SnapThreshold;

            if (mispredict)
            {
                // 서버 권위 좌표에서 출발 → 미-ack 입력 재시뮬.
                // ReplayFrom은 ackedClientTick *초과*만 반환 (동일 tick은 이미 처리됨).
                Vector2 replayed = new Vector2(serverX, serverY);
                foreach (InputRecord input in _history.ReplayFrom(ackedClientTick))
                {
                    if (input.InputX == 0) continue;
                    // 양쪽 공식 일치 (헌법 #1 / ADR-010): Constants.MoveSpeed, TickDuration.
                    // 50ms 고정 시뮬레이션 — 클라 송신이 throttle된 cadence와 동일.
                    float dxr = input.InputX * Constants.MoveSpeed * Constants.TickDuration;
                    replayed = new Vector2(replayed.x + dxr, replayed.y);
                }
                Position = replayed;
                SnapCount++; // 카운터 이름은 호환성 유지 (옛 "snap" 의미 X, 이제 "reconcile" 의미).
            }

            // 항상 ack된 입력 정리 — 더 이상 replay 대상 X (메모리 위생).
            _history.EvictUpTo(ackedClientTick);
            return mispredict;
        }
    }
}
