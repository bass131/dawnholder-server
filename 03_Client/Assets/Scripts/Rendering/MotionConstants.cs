#nullable enable
namespace Dawnholder.Client.Rendering
{
    internal static class MotionConstants
    {
        // 방향 전환 판정 최솟값 — 세 Motion 클래스 공유 (단위: 거리/frame).
        internal const float FacingEpsilon = 0.001f;

        // 원격 플레이어 facing 판정 데드존 — 서버 권위 vx 부호로 결정 (단위: units/s).
        // MoveSpeed ≈ 5.0 units/s → 이동 중 |vx| ≈ 5, 정지 시 vx = 0. 0.1 데드존으로 충분.
        internal const float RemoteFacingVelocityEpsilon = 0.1f;
    }
}
