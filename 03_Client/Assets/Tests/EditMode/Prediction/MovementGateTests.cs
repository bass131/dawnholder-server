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

    // ShouldForceAdopt 순수 함수 단위 테스트.
    //
    // **M4.13 P5b 거동 변경**: Attack(대쉬/lunge)은 더 이상 force-adopt하지 않는다 —
    //   5a에서 클라가 P4 공유 공식(Physics.DecayImpulse)으로 임펄스를 직접 예측하므로 크러치 불요.
    //   진짜 mispredict(벽 충돌 등)는 OnSnapshot SnapThreshold가 잡음. force-adopt는 이제
    //   Teleport(teleportSnap) + 넉백(Hit)만. 옛 "Attack + serverVx≥ε → force-adopt" vx-게이팅과
    //   serverVx 인자는 제거됨(옛 ε 경계 케이스는 그 게이팅을 검증하던 것이라 함께 제거).
    public class ShouldForceAdoptTests
    {
        [Test]
        public void ShouldForceAdopt_TeleportSnap_ReturnsTrue()
        {
            // Teleport: teleportSnap 플래그 → 항상 force-adopt (즉시 스냅).
            Assert.IsTrue(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: true, serverAnimState: AnimState.Idle));
        }

        [Test]
        public void ShouldForceAdopt_Hit_ReturnsTrue()
        {
            // 넉백(Hit): 아직 클라 예측 안 함(1차 범위 밖) → force-adopt 유지.
            Assert.IsTrue(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Hit));
        }

        [Test]
        public void ShouldForceAdopt_Attack_ReturnsFalse()
        {
            // ★P5b: 대쉬/lunge(Attack)는 5a에서 클라가 직접 예측 → force-adopt 불요.
            //   옛 "Attack + 전방 임펄스 → force-adopt"가 대쉬 스터터(매 스냅샷 버퍼 리셋)의 원인이었음.
            //   5a 예측으로 대체 — 임계 이내 서버 위치를 더는 채택하지 않는다.
            Assert.IsFalse(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Attack));
        }

        [Test]
        public void ShouldForceAdopt_Idle_ReturnsFalse()
        {
            // Idle: 일반 이동 중 → force-adopt 없음.
            Assert.IsFalse(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Idle));
        }

        [Test]
        public void ShouldForceAdopt_Walk_ReturnsFalse()
        {
            // Walk → force-adopt 없음.
            Assert.IsFalse(LocalPlayerMovement.ShouldForceAdopt(
                teleportSnap: false, serverAnimState: AnimState.Walk));
        }
    }
}
