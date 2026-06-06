using Dawnholder.Client.Prediction;
using NUnit.Framework;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Tests.Prediction
{
    // M4.4 Phase 04: PlayerPredictor reconcile 알고리즘 EditMode 단위 테스트.
    //
    // **테스트 범위**:
    //   - mispredict 판정 (|dX| 또는 |dY| > SnapThreshold)
    //   - mispredict 시 서버 권위 상태 적용 + SnapCount 증가
    //   - mispredict 시 미-ack 입력 replay (Physics.Step 재실행)
    //   - 정상 범위 내 snapshot (no-mispredict) → Position 그대로 유지
    //   - 항상 InputHistory 정리 (mispredict 여부 무관)
    //   - 직업 MoveParams 비례 검증 (Warrior 4 vs Ranger 6 거리 비례)
    //
    // **헌법 #1 유지 검증**: mispredict 시 클라 예측 위치가 아닌
    //   서버 권위 좌표에서 replay 출발 (cheat 흡수 X).
    //
    // **생성자 규칙**: new PlayerPredictor(MoveParams) 필수 인자 — fail-loud 정합.
    //   기존 reconcile 의미론 테스트는 MoveParams(5f, 8f) 명시 주입으로 수치 보존.
    //
    // **Unity 의존**: UnityEngine.Vector2 (PlayerPredictor 타입) 사용.
    //   noEngineReferences: false이므로 EditMode에서 허용.
    public class PlayerPredictorTests
    {
        // 기존 reconcile 테스트의 물리 기준. 직업값 테스트 아님 — reconcile 의미론 테스트.
        static readonly MoveParams DefaultMove = new MoveParams(5f, 8f);

        // === 기본 초기화 ===

        [Test]
        public void SetInitialPosition_SetsPositionAndResetsVelocity()
        {
            var predictor = new PlayerPredictor(DefaultMove);

            predictor.SetInitialPosition(new Vector2(3f, 0f));

            Assert.AreEqual(new Vector2(3f, 0f), predictor.Position);
            Assert.AreEqual(Vector2.zero, predictor.Velocity);
            // 지형 모드(M4.4-03)부터 spawn 직후는 항상 공중 출발 — 서버도 같은 Step에서
            // 중력을 적용해 함께 낙하하므로 첫 틱 drift는 reconcile이 흡수.
            Assert.IsFalse(predictor.OnGround, "spawn 직후는 항상 OnGround false (지형 모드 낙하 출발)");
        }

        [Test]
        public void SetInitialPosition_AboveGround_OnGroundFalse()
        {
            var predictor = new PlayerPredictor(DefaultMove);

            predictor.SetInitialPosition(new Vector2(0f, 5f));

            Assert.IsFalse(predictor.OnGround, "y=5 → OnGround false");
        }

        // === Predict ===

        [Test]
        public void Predict_HorizontalMove_UpdatesPositionX()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(Vector2.zero);
            float dt = Constants.TickDuration;

            predictor.Predict(inputX: 1, jumpPressed: false, dt: dt);

            // 예상: x = MoveSpeed * dt (DefaultMove.MoveSpeed = 5f)
            float expectedX = 5f * dt;
            Assert.AreEqual(expectedX, predictor.Position.x, 0.0001f);
        }

        [Test]
        public void Predict_NoInput_PositionUnchangedX()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(5f, 0f));
            float dt = Constants.TickDuration;

            predictor.Predict(inputX: 0, jumpPressed: false, dt: dt);

            Assert.AreEqual(5f, predictor.Position.x, 0.0001f);
        }

        // === OnSnapshot — no-mispredict ===

        [Test]
        public void OnSnapshot_WithinThreshold_ReturnsFalse_PositionUnchanged()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(10f, 0f));

            // 서버 위치가 threshold 이내 — mispredict 아님
            float serverX = 10f + (PlayerPredictor.SnapThreshold * 0.5f);
            float serverY = 0f;
            bool mispredict = predictor.OnSnapshot(serverX, serverY, 0f, 0f, ackedClientTick: 0);

            Assert.IsFalse(mispredict, "threshold 이내 → mispredict 아님");
            // no-mispredict 시 Position은 클라 예측값 그대로
            Assert.AreEqual(10f, predictor.Position.x, 0.0001f);
        }

        [Test]
        public void OnSnapshot_WithinThresholdY_ReturnsFalse()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(0f, 3f));

            bool mispredict = predictor.OnSnapshot(0f, 3f + (PlayerPredictor.SnapThreshold * 0.4f),
                                                    0f, 0f, ackedClientTick: 0);

            Assert.IsFalse(mispredict);
        }

        // === OnSnapshot — mispredict X ===

        [Test]
        public void OnSnapshot_ExceedThresholdX_ReturnsTrue_SnapCountIncremented()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(0f, 0f));

            float serverX = PlayerPredictor.SnapThreshold + 1f; // 명백히 초과
            bool mispredict = predictor.OnSnapshot(serverX, 0f, 0f, 0f, ackedClientTick: 0);

            Assert.IsTrue(mispredict, "X 초과 → mispredict");
            Assert.AreEqual(1, predictor.SnapCount);
        }

        [Test]
        public void OnSnapshot_MispredictX_PositionSnapsToServerAuthority()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(0f, 0f));

            float serverX = 99f; // 클라 위치(0)와 큰 차이
            predictor.OnSnapshot(serverX, 0f, 0f, 0f, ackedClientTick: 0);

            // 미-ack 입력 없음 → replay 없음 → 서버 권위 위치 그대로
            Assert.AreEqual(serverX, predictor.Position.x, 0.0001f,
                "헌법 #1: 서버 권위 좌표에서 출발");
        }

        // === OnSnapshot — mispredict Y ===

        [Test]
        public void OnSnapshot_ExceedThresholdY_ReturnsTrue()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(0f, 0f));

            float serverY = PlayerPredictor.SnapThreshold + 0.5f; // Y 초과
            bool mispredict = predictor.OnSnapshot(0f, serverY, 0f, 5f, ackedClientTick: 0);

            Assert.IsTrue(mispredict, "Y 초과 → mispredict");
        }

        [Test]
        public void OnSnapshot_MispredictY_VelocityApplied()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(Vector2.zero);

            predictor.OnSnapshot(0f, 5f, 0f, serverVy: 3f, ackedClientTick: 0);

            // 미-ack 입력 없음 → replay 없음 → 서버 속도 그대로
            Assert.AreEqual(3f, predictor.Velocity.y, 0.0001f);
        }

        // === OnSnapshot — mispredict + 미-ack 입력 replay ===

        [Test]
        public void OnSnapshot_Mispredict_WithUnackedInputs_ReplaysInputs()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(0f, 0f));

            // tick 1, 2, 3 입력 보냄 (우측 이동)
            predictor.NotifySent(1, inputX: 1, jumpPressed: false);
            predictor.NotifySent(2, inputX: 1, jumpPressed: false);
            predictor.NotifySent(3, inputX: 1, jumpPressed: false);

            // 서버는 tick 1까지 ack, 서버 위치는 (999, 0) — 명백한 mispredict
            // 미-ack 입력: tick 2, 3 (각각 우측 이동 1 step)
            predictor.OnSnapshot(serverX: 999f, serverY: 0f,
                                  serverVx: 0f, serverVy: 0f,
                                  ackedClientTick: 1);

            // replay 후 위치: 서버 999 + tick2 step + tick3 step (DefaultMove.MoveSpeed=5f)
            float stepDx = 5f * Constants.TickDuration;
            float expectedX = 999f + stepDx + stepDx;
            Assert.AreEqual(expectedX, predictor.Position.x, 0.001f,
                "미-ack 입력(tick 2, 3) replay로 서버 권위 기준에서 누적 이동");
        }

        [Test]
        public void OnSnapshot_Mispredict_AllInputsAcked_NoReplay()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(0f, 0f));

            predictor.NotifySent(1, inputX: 1, jumpPressed: false);
            predictor.NotifySent(2, inputX: 1, jumpPressed: false);

            // tick 2까지 ack → 미-ack 입력 없음
            predictor.OnSnapshot(serverX: 999f, serverY: 0f,
                                  serverVx: 0f, serverVy: 0f,
                                  ackedClientTick: 2);

            // replay 없음 → 서버 권위 위치 그대로
            Assert.AreEqual(999f, predictor.Position.x, 0.0001f,
                "모든 입력 ack 시 replay 없음 — 서버 권위 좌표 그대로");
        }

        // === InputHistory 정리 (mispredict 여부 무관) ===

        [Test]
        public void OnSnapshot_NoMispredict_StillEvictsHistory()
        {
            // no-mispredict여도 EvictUpTo는 호출돼야 함 (메모리 위생).
            // PlayerPredictor 내부 _history.Count는 노출 안 됨 → 간접 검증:
            // 이전 ack된 입력이 다음 snapshot replay에서 포함되지 않음을 확인.
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(0f, 0f));

            predictor.NotifySent(1, inputX: 1, jumpPressed: false);
            predictor.NotifySent(2, inputX: 1, jumpPressed: false);

            // no-mispredict snapshot (threshold 이내), ackedClientTick=2
            predictor.OnSnapshot(0f, 0f, 0f, 0f, ackedClientTick: 2);

            // 이제 mispredict snapshot: ackedClientTick=2 이하 입력이 정리됐으면
            // replay 시 추가 이동 없음
            predictor.OnSnapshot(serverX: 999f, serverY: 0f,
                                  serverVx: 0f, serverVy: 0f,
                                  ackedClientTick: 2);

            // replay 없음 → 서버 999f 그대로 (정리됐으면)
            Assert.AreEqual(999f, predictor.Position.x, 0.0001f,
                "no-mispredict snapshot에서 EvictUpTo 완료 → 다음 mispredict replay에서 stale 입력 없음");
        }

        // === SnapCount 누적 ===

        [Test]
        public void OnSnapshot_MultipleSnapshots_SnapCountAccumulates()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(Vector2.zero);

            predictor.OnSnapshot(999f, 0f, 0f, 0f, ackedClientTick: 0);
            predictor.OnSnapshot(999f, 999f, 0f, 0f, ackedClientTick: 0);

            Assert.AreEqual(2, predictor.SnapCount,
                "mispredict 2회 → SnapCount=2");
        }

        // === 직업 MoveParams 비례 검증 (M4.4 Phase 04 신설) ===
        //
        // Warrior(MoveSpeed=4) vs Ranger(MoveSpeed=6) — 같은 입력 1 tick Predict 시
        // X 이동 거리가 moveSpeed 비율(4:6 = 2:3)에 정확히 비례해야 함.
        // 헌법 #4: 클라 예측이 PlayerStats factory 단일 출처를 경유하는지 간접 검증.

        [Test]
        public void Predict_WarriorVsRanger_XDistanceProportionalToMoveSpeed()
        {
            // PlayerStats factory 단일 출처 — 클라 로컬 하드코딩 금지 (헌법 #4).
            var warrior = PlayerStats.Warrior();
            var ranger  = PlayerStats.Ranger();
            var warriorMove = new MoveParams(warrior.MoveSpeed, warrior.JumpVel);
            var rangerMove  = new MoveParams(ranger.MoveSpeed, ranger.JumpVel);

            var predictorW = new PlayerPredictor(warriorMove);
            var predictorR = new PlayerPredictor(rangerMove);
            predictorW.SetInitialPosition(Vector2.zero);
            predictorR.SetInitialPosition(Vector2.zero);

            float dt = Constants.TickDuration;
            predictorW.Predict(inputX: 1, jumpPressed: false, dt: dt);
            predictorR.Predict(inputX: 1, jumpPressed: false, dt: dt);

            // Warrior MoveSpeed=4, Ranger MoveSpeed=6 → 비율 4:6 = 2:3
            float ratio = predictorR.Position.x / predictorW.Position.x;
            Assert.AreEqual(ranger.MoveSpeed / warrior.MoveSpeed, ratio, 0.0001f,
                "Ranger/Warrior X 이동 비율이 MoveSpeed 비율(6/4=1.5)과 일치해야 함");
        }

        // === IsGroundedAt — reconcile 시 접지 판정 (M4.4-03 reviewer 🟡) ===
        //
        // IsGroundedAt은 private — mispredict OnSnapshot 경로(서버 권위 리셋 시
        // OnGround = IsGroundedAt(serverX, serverY, serverVy))로 간접 검증.
        // 미-ack 입력이 없으면 replay가 돌지 않아 판정 결과가 OnGround에 그대로 보존됨.
        // 4분기: 평지(terrain null) / 솔리드 윗면 / one-way 발판 / 상승 중(vy>0).

        static MapTerrain SolidOnlyTerrain()
            => new MapTerrain(
                new[] { new TerrainAabb(0f, 0f, 10f, 2f) },
                System.Array.Empty<TerrainPlatform>());

        static MapTerrain PlatformOnlyTerrain()
            => new MapTerrain(
                System.Array.Empty<TerrainAabb>(),
                new[] { new TerrainPlatform(3f, 0f, 10f) });

        [Test]
        public void OnSnapshot_FlatFallback_AtGroundLevel_OnGroundTrue()
        {
            var predictor = new PlayerPredictor(DefaultMove); // terrain 미주입 = 평지 fallback
            predictor.SetInitialPosition(Vector2.zero);

            bool mispredict = predictor.OnSnapshot(10f, 0f, 0f, 0f, ackedClientTick: 0);

            Assert.IsTrue(mispredict, "전제: mispredict 경로 진입");
            Assert.IsTrue(predictor.OnGround, "평지 모드 y=0 → 접지");
        }

        [Test]
        public void OnSnapshot_FlatFallback_InAir_OnGroundFalse()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(Vector2.zero);

            bool mispredict = predictor.OnSnapshot(10f, 5f, 0f, 0f, ackedClientTick: 0);

            Assert.IsTrue(mispredict, "전제: mispredict 경로 진입");
            Assert.IsFalse(predictor.OnGround, "평지 모드 y=5 → 공중");
        }

        [Test]
        public void OnSnapshot_OnSolidTop_OnGroundTrue()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetTerrain(SolidOnlyTerrain());
            predictor.SetInitialPosition(Vector2.zero);

            // 솔리드 (0,0)-(10,2) 윗면 y=2 위 — 착지 스냅 정확값.
            bool mispredict = predictor.OnSnapshot(5f, 2f, 0f, 0f, ackedClientTick: 0);

            Assert.IsTrue(mispredict, "전제: mispredict 경로 진입");
            Assert.IsTrue(predictor.OnGround, "솔리드 윗면 y=MaxY → 접지");
        }

        [Test]
        public void OnSnapshot_BesideSolid_SameHeight_OnGroundFalse()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetTerrain(SolidOnlyTerrain());
            predictor.SetInitialPosition(Vector2.zero);

            // 같은 높이(y=2)지만 솔리드 x 범위(0..10) 밖 — 받침 없음.
            bool mispredict = predictor.OnSnapshot(20f, 2f, 0f, 0f, ackedClientTick: 0);

            Assert.IsTrue(mispredict, "전제: mispredict 경로 진입");
            Assert.IsFalse(predictor.OnGround, "솔리드 수평 범위 밖 → 공중");
        }

        [Test]
        public void OnSnapshot_AboveSolid_OnGroundFalse()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetTerrain(SolidOnlyTerrain());
            predictor.SetInitialPosition(Vector2.zero);

            // 솔리드 위 공중 (y=4 > MaxY=2) — 지형 모드에서 평지 y<=0 가정이 사라졌는지도 겸검증.
            bool mispredict = predictor.OnSnapshot(5f, 4f, 0f, 0f, ackedClientTick: 0);

            Assert.IsTrue(mispredict, "전제: mispredict 경로 진입");
            Assert.IsFalse(predictor.OnGround, "솔리드 위 공중 → 비접지");
        }

        [Test]
        public void OnSnapshot_OnPlatform_OnGroundTrue()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetTerrain(PlatformOnlyTerrain());
            predictor.SetInitialPosition(Vector2.zero);

            // one-way 발판 y=3 위 — 착지 스냅 정확값.
            bool mispredict = predictor.OnSnapshot(5f, 3f, 0f, 0f, ackedClientTick: 0);

            Assert.IsTrue(mispredict, "전제: mispredict 경로 진입");
            Assert.IsTrue(predictor.OnGround, "발판 윗면 y=P.Y → 접지");
        }

        [Test]
        public void OnSnapshot_BelowPlatform_OnGroundFalse()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetTerrain(PlatformOnlyTerrain());
            predictor.SetInitialPosition(new Vector2(50f, 0f)); // x 차이로 mispredict 유도

            // 발판 아래 (y=1) — one-way 발판은 그 높이가 아니면 받침 아님.
            bool mispredict = predictor.OnSnapshot(5f, 1f, 0f, 0f, ackedClientTick: 0);

            Assert.IsTrue(mispredict, "전제: mispredict 경로 진입");
            Assert.IsFalse(predictor.OnGround, "발판 아래 → 공중");
        }

        [Test]
        public void OnSnapshot_RisingOnSolidTop_OnGroundFalse()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetTerrain(SolidOnlyTerrain());
            predictor.SetInitialPosition(Vector2.zero);

            // 좌표는 솔리드 윗면과 일치하지만 vy>0 (점프 상승 통과 중) — 접지 아님.
            // 상승 분기가 면 일치 검사보다 먼저 끊어야 replay 점프 입력이 서버와 정합.
            bool mispredict = predictor.OnSnapshot(5f, 2f, 0f, 5f, ackedClientTick: 0);

            Assert.IsTrue(mispredict, "전제: mispredict 경로 진입");
            Assert.IsFalse(predictor.OnGround, "vy>0 상승 중 → 면 위라도 비접지");
        }
    }
}
