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

        public void SetInitialPosition(Vector2 pos)
        {
            Position = pos;
        }

        // 매 frame 호출. inputX는 -1/0/1 (LocalPlayerController가 EncodeInputX로 인코딩한 값).
        // deltaTime은 Unity Time.deltaTime — 큰 프레임 스파이크 시 서버(50ms 고정)와 drift.
        public void Predict(sbyte inputX, float deltaTime)
        {
            if (inputX == 0) return;
            float dx = inputX * Constants.MoveSpeed * deltaTime;
            Position = new Vector2(Position.x + dx, Position.y);
        }

        // S_Snapshot 도착 시 호출.
        // 반환값: snap 발생 여부 (true면 LocalPlayerController가 "[Snap] dx=... at tick=..." 로깅).
        public bool OnSnapshot(float serverX, float serverY)
        {
            float dx = serverX - Position.x;
            if (Mathf.Abs(dx) > SnapThreshold)
            {
                Position = new Vector2(serverX, serverY);
                SnapCount++;
                return true;
            }
            return false;
        }
    }
}
