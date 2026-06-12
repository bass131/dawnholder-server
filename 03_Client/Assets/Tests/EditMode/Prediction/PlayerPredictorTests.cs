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
    //   - 직업 MoveParams 비례 검증 (Knight 4 vs Mage 6 거리 비례)
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

            predictor.Predict(inputX: 1, jumpPressed: false);

            // 예상: x = MoveSpeed * TickDuration (DefaultMove.MoveSpeed = 5f)
            float expectedX = 5f * Constants.TickDuration;
            Assert.AreEqual(expectedX, predictor.Position.x, 0.0001f);
        }

        [Test]
        public void Predict_NoInput_PositionUnchangedX()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(5f, 0f));

            predictor.Predict(inputX: 0, jumpPressed: false);

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

        // === OnSnapshot — forceAdopt (HitState 넉백 표시, M4.6 Phase 03) ===
        //
        // 피격 중 클라는 서버 권위 넉백 임펄스(ExternalVelX)를 예측 못 함 → 임계 이내여도
        // 서버 위치를 채택(force-adopt)해 넉백 시각화 + sub-threshold offset 누적 방지.

        [Test]
        public void OnSnapshot_ForceAdopt_WithinThreshold_AdoptsServerPosition()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(10f, 0f));

            // 임계 이내(0.3 < 1.5)지만 forceAdopt=true → 서버 위치 채택해야 함.
            float serverX = 10f + 0.3f;
            bool reconciled = predictor.OnSnapshot(serverX, 0f, 0f, 0f,
                                                    ackedClientTick: 0, forceAdopt: true);

            Assert.IsTrue(reconciled, "forceAdopt=true → 임계 이내여도 reconcile true 반환");
            Assert.AreEqual(serverX, predictor.Position.x, 0.0001f,
                "넉백 표시: 임계 이내여도 서버 위치를 채택해야 함");
        }

        [Test]
        public void OnSnapshot_ForceAdopt_WithinThreshold_SnapCountNotIncremented()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(10f, 0f));

            predictor.OnSnapshot(10f + 0.3f, 0f, 0f, 0f, ackedClientTick: 0, forceAdopt: true);

            Assert.AreEqual(0, predictor.SnapCount,
                "force-adopt(임계 이내)은 진짜 mispredict 아님 — SnapCount 증가 X");
        }

        [Test]
        public void OnSnapshot_ForceAdopt_AlsoMispredict_SnapCountIncremented()
        {
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(0f, 0f));

            // 임계 초과 + forceAdopt — 진짜 mispredict이므로 SnapCount 증가.
            predictor.OnSnapshot(99f, 0f, 0f, 0f, ackedClientTick: 0, forceAdopt: true);

            Assert.AreEqual(1, predictor.SnapCount,
                "임계 초과는 forceAdopt 여부와 무관하게 진짜 mispredict → SnapCount=1");
        }

        [Test]
        public void OnSnapshot_NoForceAdopt_WithinThreshold_PositionUnchanged()
        {
            // forceAdopt 기본값(false) — 기존 동작 보존: 임계 이내면 예측 위치 유지.
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(10f, 0f));

            bool reconciled = predictor.OnSnapshot(10f + 0.3f, 0f, 0f, 0f, ackedClientTick: 0);

            Assert.IsFalse(reconciled, "forceAdopt 없음 + 임계 이내 → reconcile 안 함");
            Assert.AreEqual(10f, predictor.Position.x, 0.0001f, "예측 위치 그대로 유지");
        }

        // === 직업 MoveParams 비례 검증 (M4.4 Phase 04 신설) ===
        //
        // Knight(MoveSpeed=4) vs Mage(MoveSpeed=6) — 같은 입력 1 tick Predict 시
        // X 이동 거리가 moveSpeed 비율(4:6 = 2:3)에 정확히 비례해야 함.
        // 헌법 #4: 클라 예측이 PlayerStats factory 단일 출처를 경유하는지 간접 검증.

        [Test]
        public void Predict_KnightVsMage_XDistanceProportionalToMoveSpeed()
        {
            // PlayerStats factory 단일 출처 — 클라 로컬 하드코딩 금지 (헌법 #4).
            var knight = PlayerStats.Knight();
            var mage  = PlayerStats.Mage();
            var knightMove = new MoveParams(knight.MoveSpeed, knight.JumpVel);
            var mageMove  = new MoveParams(mage.MoveSpeed, mage.JumpVel);

            var predictorW = new PlayerPredictor(knightMove);
            var predictorR = new PlayerPredictor(mageMove);
            predictorW.SetInitialPosition(Vector2.zero);
            predictorR.SetInitialPosition(Vector2.zero);

            predictorW.Predict(inputX: 1, jumpPressed: false);
            predictorR.Predict(inputX: 1, jumpPressed: false);

            // Knight MoveSpeed=4, Mage MoveSpeed=6 → 비율 4:6 = 2:3
            float ratio = predictorR.Position.x / predictorW.Position.x;
            Assert.AreEqual(mage.MoveSpeed / knight.MoveSpeed, ratio, 0.0001f,
                "Mage/Knight X 이동 비율이 MoveSpeed 비율(6/4=1.5)과 일치해야 함");
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

        // ── P3 안전망 — computed-expectation + baseline + 불변식 (M4.11 Phase 03) ──────
        //
        // 두 종류를 명확히 구분해 박는다 (스펙 §안전망 설계):
        //   [P3 baseline]  — P4가 의도적으로 바꿀 수 있는 거동(가변 dt Predict 궤적).
        //                    P4에서 바뀌면 *의식적 갱신 + 사유 박제* 의무.
        //   [P3 invariant] — P4 후에도 절대 green 유지 (replay=고정 dt라 P4 무관).

        // P3-1(baseline): Predict 고정 dt 궤적 computed-expectation.
        //
        // [P3 baseline — P4 재검토 대상]
        // 변경 전: 가변 dt 시퀀스({0.016f, 0.020f, 0.033f, 0.014f, 0.025f})로 Predict를 돌려
        //          각각 다른 dt를 Physics.Step에 직접 전달하는 거동.
        // 변경 후: Predict 시그니처에서 dt 파라미터 제거 — 내부에서 Constants.TickDuration 고정.
        //          "가변 dt가 물리적으로 못 들어가는 구조(illegal state unrepresentable)" 정합.
        //          5회 Predict == TickDuration × 5회 Physics.Step fold. 가변 dt 시나리오는 폐기.
        // 사유: P4 고정 서브스텝 전환 — 클라 Predict와 서버 Step이 동일 dt를 사용해 drift 0.
        //
        // computed-expectation 패턴 유지: "Predict N회 == TickDuration으로 N회 Step fold" 런타임 검증.
        [Test]
        public void P3_Baseline_Predict_FixedDt_EqualsPhysicsStepFold()
        {
            // [P3 baseline — P4 재검토 대상]
            // 변경 전: 가변 dt 시퀀스({0.016f, 0.020f, 0.033f, 0.014f, 0.025f}) 기반 Predict 궤적.
            // 변경 후: 고정 TickDuration × 5회 Predict 궤적. dt 파라미터 제거 → 고정 dt 강제.
            // 사유: P4 고정 서브스텝 전환 — Predict 내부에서 TickDuration 고정 사용.
            const float Eps = 1e-4f;
            const int Steps = 5;

            // Predict 경로 — dt 파라미터 없음, 내부에서 TickDuration 고정.
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(Vector2.zero);
            for (int i = 0; i < Steps; i++)
                predictor.Predict(inputX: 1, jumpPressed: false);

            // Physics.Step 직접 fold 경로 (기댓값) — TickDuration 고정.
            var physState = new Shared.GameData.PhysicsState(
                new System.Numerics.Vector2(0f, 0f),
                System.Numerics.Vector2.Zero,
                onGround: false);
            for (int i = 0; i < Steps; i++)
            {
                physState = Shared.GameData.Physics.Step(
                    physState,
                    new Shared.GameData.PhysicsInput(1, false, Shared.GameData.Constants.TickDuration),
                    DefaultMove);
            }

            Assert.AreEqual(physState.Position.X, predictor.Position.x, Eps,
                "P4 baseline: Predict(fixed dt) x == Physics.Step(TickDuration) fold x");
            Assert.AreEqual(physState.Position.Y, predictor.Position.y, Eps,
                "P4 baseline: Predict(fixed dt) y == Physics.Step(TickDuration) fold y");
        }

        // P3-2(baseline): Predict 점프 포함 고정 dt 궤적 computed-expectation.
        //
        // [P3 baseline — P4 재검토 대상]
        // 변경 전: (inputX, jumpPressed, dt) 튜플 시퀀스 — 각 스텝마다 다른 dt 사용.
        // 변경 후: dt 파라미터 제거. (inputX, jumpPressed) 시퀀스 × TickDuration 고정.
        //          가변 dt 시나리오가 물리적으로 불가능한 구조 — P4 고정 서브스텝 전환 결과.
        // 사유: Predict는 이제 TickDuration 고정 → 점프 포함 궤적도 고정 dt fold와 일치해야 함.
        [Test]
        public void P3_Baseline_Predict_WithJump_EqualsPhysicsStepFold()
        {
            // [P3 baseline — P4 재검토 대상]
            // 변경 전: (ix, jump, dt) 시퀀스 — {(0,true,0.016f), (1,false,0.020f), ...} 가변 dt.
            // 변경 후: (ix, jump) 시퀀스 — dt 파라미터 제거, TickDuration 고정 사용.
            // 사유: P4 Predict 시그니처 변경(dt 제거) — 고정 서브스텝 전환.
            const float Eps = 1e-4f;

            // (inputX, jumpPressed) 튜플 시퀀스 — dt는 TickDuration으로 고정
            var seq = new (sbyte ix, bool jump)[]
            {
                (0, true),  // 점프 시도 (OnGround=false라 점프 불가 → 중력만)
                (1, false), // 우이동
                (1, false),
                (0, false),
            };

            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(Vector2.zero); // OnGround=false
            foreach (var (ix, jump) in seq)
                predictor.Predict(inputX: ix, jumpPressed: jump);

            var physState = new Shared.GameData.PhysicsState(
                System.Numerics.Vector2.Zero,
                System.Numerics.Vector2.Zero,
                onGround: false);
            foreach (var (ix, jump) in seq)
            {
                physState = Shared.GameData.Physics.Step(
                    physState,
                    new Shared.GameData.PhysicsInput(ix, jump, Shared.GameData.Constants.TickDuration),
                    DefaultMove);
            }

            Assert.AreEqual(physState.Position.X, predictor.Position.x, Eps,
                "P4 baseline: 점프 포함 Predict(fixed dt) x == Physics.Step(TickDuration) fold x");
            Assert.AreEqual(physState.Position.Y, predictor.Position.y, Eps,
                "P4 baseline: 점프 포함 Predict(fixed dt) y == Physics.Step(TickDuration) fold y");
        }

        // P3-3(불변식): SetInitialPosition + 미-ack 입력 replay 후 최종 위치
        //   == 서버 모사(고정 TickDuration으로 Physics.Step fold).
        //
        // replay 경로는 Constants.TickDuration 고정 dt를 사용하므로 P4 무관 → 불변식.
        // (PlayerPredictor.cs:135 참조 — replay는 이미 고정 dt)
        [Test]
        public void P3_Invariant_Replay_FixedDt_MatchesServerSim()
        {
            const float Eps = 1e-4f;
            const int ReplaySteps = 3;

            // 서버 모사: 고정 TickDuration으로 Physics.Step fold
            var serverSim = new Shared.GameData.PhysicsState(
                new System.Numerics.Vector2(5f, 0f), // 서버 권위 초기 위치
                System.Numerics.Vector2.Zero,
                onGround: true);
            sbyte[] replayInputs = { 1, 1, -1 };
            foreach (sbyte ix in replayInputs)
            {
                serverSim = Shared.GameData.Physics.Step(
                    serverSim,
                    new Shared.GameData.PhysicsInput(ix, false, Shared.GameData.Constants.TickDuration),
                    DefaultMove);
            }

            // 클라 predictor: 서버 스냅샷 수신 후 미-ack 입력 replay
            var predictor = new PlayerPredictor(DefaultMove);
            predictor.SetInitialPosition(new Vector2(0f, 0f)); // 클라 예측 위치(다름 → mispredict 유도)

            // tick 1,2,3을 미-ack 입력으로 등록
            for (int i = 0; i < ReplaySteps; i++)
                predictor.NotifySent((uint)(i + 1), replayInputs[i], false);

            // 서버 스냅샷: (5,0) 권위 위치 + ackedTick=0 (tick 1,2,3 전부 미-ack)
            predictor.OnSnapshot(serverX: 5f, serverY: 0f, serverVx: 0f, serverVy: 0f, ackedClientTick: 0);

            // replay 후 위치 == 서버 모사 최종 위치 (고정 dt라 P4 무관, 불변식)
            Assert.AreEqual(serverSim.Position.X, predictor.Position.x, Eps,
                "P3 invariant: replay(고정 dt) x == 서버 모사 x");
            Assert.AreEqual(serverSim.Position.Y, predictor.Position.y, Eps,
                "P3 invariant: replay(고정 dt) y == 서버 모사 y");
        }
    }
}
