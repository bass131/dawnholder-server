using Dawnholder.Client.Prediction;
using NUnit.Framework;
using Shared.GameData;

namespace Dawnholder.Client.Tests.Prediction
{
    // M4.6 Phase 03: LocalPlayerMovement.IsMovementLocked 순수 함수 단위 테스트.
    //
    // 서버 AttackState/HitState/DeathState(LocksMovement)의 클라 거울 게이트 검증:
    //   - commit window 타이머(로컬 공격 예측) — 잔여>0이면 잠금 (서버 확인 전 즉시 "콱 정지")
    //   - 서버 animState Attack/Hit/Death — 클라 예측 불가/연장분을 서버 신뢰로 잠금
    //   - 그 외(Idle/Walk/Jump) + 타이머 0 — 잠금 해제 (정상 이동)
    //
    // **헌법 #1**: 이동 잠금은 서버 게임플레이 규칙. 클라는 같은 98_Shared 상수 + 서버 animState로
    //   거울 게이트해 reconcile rubber-band 0. 화이트리스트 정신(잠금 조건만 명시).
    public class MovementGateTests
    {
        // === commit window 타이머 (로컬 공격 예측) ===

        [Test]
        public void IsMovementLocked_CommitWindowActive_Idle_Locked()
        {
            // 타이머 잔여>0 → 서버 animState 무관 잠금 (로컬 선예측).
            Assert.IsTrue(LocalPlayerMovement.IsMovementLocked(0.2f, AnimState.Idle));
        }

        [Test]
        public void IsMovementLocked_CommitWindowActive_Walk_Locked()
        {
            // 타이머가 우선 — 서버가 아직 Walk(공격 미처리)여도 로컬 예측 잠금.
            Assert.IsTrue(LocalPlayerMovement.IsMovementLocked(0.05f, AnimState.Walk));
        }

        [Test]
        public void IsMovementLocked_CommitWindowExpired_Idle_Unlocked()
        {
            Assert.IsFalse(LocalPlayerMovement.IsMovementLocked(0f, AnimState.Idle));
        }

        // === 서버 animState 게이트 (타이머 만료 후) ===

        [Test]
        public void IsMovementLocked_ServerAttack_Locked()
        {
            // 로컬 타이머는 끝났지만 서버 window가 더 길면 Attack animState가 잠금 연장(거울 보정).
            Assert.IsTrue(LocalPlayerMovement.IsMovementLocked(0f, AnimState.Attack));
        }

        [Test]
        public void IsMovementLocked_ServerHit_Locked()
        {
            // 피격은 클라 예측 불가 — 서버 Hit animState로만 잠금.
            Assert.IsTrue(LocalPlayerMovement.IsMovementLocked(0f, AnimState.Hit));
        }

        [Test]
        public void IsMovementLocked_ServerDeath_Locked()
        {
            Assert.IsTrue(LocalPlayerMovement.IsMovementLocked(0f, AnimState.Death));
        }

        // === 잠금 해제 (정상 이동 상태) ===

        [Test]
        public void IsMovementLocked_ServerWalk_Unlocked()
        {
            Assert.IsFalse(LocalPlayerMovement.IsMovementLocked(0f, AnimState.Walk));
        }

        [Test]
        public void IsMovementLocked_ServerJump_Unlocked()
        {
            Assert.IsFalse(LocalPlayerMovement.IsMovementLocked(0f, AnimState.Jump));
        }

        // === ResolveGatedInput (source-gating 일관성 — 이 Phase 핵심 불변식) ===
        //
        // 잠금 시 (0, false) 산출 → Predict/송신/NotifySent 세 곳이 같은 0 입력을 써서
        // reconcile이 서버와 일치(rubber-band 0). 회귀 시 조용히 튕김이 부활하므로 박아둠.

        [Test]
        public void ResolveGatedInput_Locked_ZeroesAllInput()
        {
            // raw 입력이 이동 +1 + 점프여도 잠금이면 (0, false).
            (sbyte moveX, bool jumpEdge) = LocalPlayerMovement.ResolveGatedInput(
                locked: true, rawMoveX: 1, rawJumpEdge: true);

            Assert.AreEqual((sbyte)0, moveX, "잠금 시 이동 입력 0");
            Assert.IsFalse(jumpEdge, "잠금 시 점프 입력 드롭");
        }

        [Test]
        public void ResolveGatedInput_Unlocked_PassesThrough()
        {
            (sbyte moveX, bool jumpEdge) = LocalPlayerMovement.ResolveGatedInput(
                locked: false, rawMoveX: -1, rawJumpEdge: true);

            Assert.AreEqual((sbyte)-1, moveX, "비잠금 시 이동 입력 그대로");
            Assert.IsTrue(jumpEdge, "비잠금 시 점프 입력 그대로");
        }
    }

    // ShouldForceAdopt 순수 함수 단위 테스트 — M4.9 reconcile 밀림 봉합.
    //
    // 핵심 불변식: Attack이지만 serverVx≈0인 Mage 평타는 force-adopt하지 않는다.
    //   → 스냅샷마다 임계 이내 서버 위치를 채택하면 rubber-band 밀림이 재현된다.
    public class ShouldForceAdoptTests
    {
        [Test]
        public void ShouldForceAdopt_Attack_VxZero_ReturnsFalse()
        {
            // Mage 평타: Attack 상태지만 서버 vx 임펄스 없음 → force-adopt 제외 (밀림 봉합 핵심).
            Assert.IsFalse(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Attack, serverVx: 0f));
        }

        [Test]
        public void ShouldForceAdopt_Attack_VxNonZero_ReturnsTrue()
        {
            // Knight Dash/lunge: Attack 상태 + 전방 임펄스 → force-adopt 유지.
            Assert.IsTrue(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Attack, serverVx: 5f));
        }

        [Test]
        public void ShouldForceAdopt_Attack_VxNegative_ReturnsTrue()
        {
            // 역방향 lunge도 vx≠0이면 force-adopt.
            Assert.IsTrue(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Attack, serverVx: -3f));
        }

        [Test]
        public void ShouldForceAdopt_Hit_VxZero_ReturnsTrue()
        {
            // 넉백: vx 무관 항상 force-adopt.
            Assert.IsTrue(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Hit, serverVx: 0f));
        }

        [Test]
        public void ShouldForceAdopt_TeleportSnap_VxZero_ReturnsTrue()
        {
            // Teleport: teleportSnap 플래그 → 항상 force-adopt.
            Assert.IsTrue(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: true, serverAnimState: AnimState.Idle, serverVx: 0f));
        }

        [Test]
        public void ShouldForceAdopt_Idle_VxZero_ReturnsFalse()
        {
            // Idle + vx 0: 일반 이동 중 → force-adopt 없음.
            Assert.IsFalse(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Idle, serverVx: 0f));
        }

        [Test]
        public void ShouldForceAdopt_Walk_VxZero_ReturnsFalse()
        {
            // Walk + vx 0 → force-adopt 없음.
            Assert.IsFalse(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Walk, serverVx: 0f));
        }

        // === ExternalImpulseEpsilon 경계값 케이스 (M4.11 P2) ===
        //
        // 서버 클램프: |임펄스 vx| < ε → 0f. 따라서 Attack 스냅샷에서
        //   - vx < ε : 서버가 이미 0으로 정리했어야 할 구간 → force-adopt 불필요.
        //   - vx >= ε : 살아남은 임펄스 활성 → force-adopt.

        [Test]
        public void ShouldForceAdopt_Attack_VxBelowEpsilon_ReturnsFalse()
        {
            // 0.049f < ε(0.05f): 서버 클램프 구간 — 정상 경로에서 이 값은 도달 불가(서버가 0으로 정리).
            // 만약 도달하면 force-adopt를 켜지 않아야 rubber-band가 재발하지 않는다.
            Assert.IsFalse(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Attack,
                serverVx: 0.049f));
        }

        [Test]
        public void ShouldForceAdopt_Attack_VxExactlyEpsilon_ReturnsTrue()
        {
            // ε = 살아남은 최소 임펄스. 게이트는 >= ε 이므로 정확히 ε 도 발동.
            Assert.IsTrue(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Attack,
                serverVx: Constants.ExternalImpulseEpsilon));
        }

        [Test]
        public void ShouldForceAdopt_Attack_VxNegativeEpsilon_ReturnsTrue()
        {
            // 역방향 최소 임펄스 — Abs 대칭 확인.
            Assert.IsTrue(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Attack,
                serverVx: -Constants.ExternalImpulseEpsilon));
        }

        [Test]
        public void ShouldForceAdopt_Attack_VxZero_Epsilon_ReturnsFalse()
        {
            // 0f: 서버 클램프 후 완전 소멸 — force-adopt 없음 (기존 케이스 보완).
            Assert.IsFalse(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Attack,
                serverVx: 0f));
        }
    }
}
