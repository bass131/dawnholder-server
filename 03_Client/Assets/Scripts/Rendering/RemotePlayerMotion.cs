#nullable enable
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // 타인 플레이어의 IMotionState 공급원.
    // animState — S_Snapshot.animState(서버 권위) 그대로 노출.
    //
    // Facing 우선순위 (LocalPlayerMotion 거울):
    //   1. Hit   — 넉백 vx의 *반대* 방향 = 공격자를 향함.
    //   2. Attack — S_PlayerAttack / S_SkillCast 에서 latch된 서버 권위 facing.
    //              없으면(0) vx 폴백.
    //   3. 평상시 — vx 부호. vx≈0(정지)이면 마지막 값 유지(jitter 차단).
    [DisallowMultipleComponent]
    public class RemotePlayerMotion : MonoBehaviour, IMotionState
    {
        AnimState _animState;
        float _vx;
        int _attackFacing;  // 0=미설정, 1=오른쪽, -1=왼쪽
        int _facing = 1;

        public AnimState CurrentAnimState => _animState;
        public int Facing => _facing;

        // RemoteEntityRegistry.UpdateSnapshot 경로에서 호출.
        public void SetAnimState(byte raw)
        {
            _animState = (AnimState)raw;
            ResolveFacing();
        }

        // RemoteEntityRegistry.UpdateSnapshot 경로에서 호출.
        public void SetVelocityX(float vx)
        {
            _vx = vx;
            ResolveFacing();
        }

        // S_PlayerAttack / S_SkillCast 수신 시 RemoteEntityRegistry.SetAttackFacing 경유 호출.
        // Attack 상태 동안 서버가 확정한 타겟 방향을 latch — 스냅샷 vx(이동 방향)와 분리.
        public void SetAttackFacing(int facing)
        {
            _attackFacing = facing;
            ResolveFacing();
        }

        // facing 결정 순수 함수 — LocalPlayerMotion.LateUpdate facing 블록의 거울.
        void ResolveFacing()
        {
            // 피격: 넉백은 공격자 *반대* 방향 → vx 반대로 바라보면 공격자를 향함.
            if (_animState == AnimState.Hit)
            {
                if (Mathf.Abs(_vx) > MotionConstants.RemoteFacingVelocityEpsilon)
                    _facing = _vx > 0f ? -1 : 1;
                return;
            }

            // 공격: 서버 권위 타겟 방향 latch. 미설정(0)이면 vx 폴백으로 낙하.
            if (_animState == AnimState.Attack && _attackFacing != 0)
            {
                _facing = _attackFacing;
                return;
            }

            // 평상시(이동/Idle/Jump): vx 부호. vx≈0이면 마지막 값 유지.
            if (Mathf.Abs(_vx) > MotionConstants.RemoteFacingVelocityEpsilon)
                _facing = _vx > 0f ? 1 : -1;
        }
    }
}
