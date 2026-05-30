#nullable enable
using Shared.GameData;

namespace Dawnholder.Client.Rendering
{
    // Animator 구동에 필요한 "상태 공급원" 추상화.
    // AnimatorDriver가 이 인터페이스만 의존 — LocalPlayer/RemotePlayer/Enemy가 같은 driver를 재사용.
    public interface IMotionState
    {
        AnimState CurrentAnimState { get; }

        // -1 = 왼쪽, +1 = 오른쪽.
        int Facing { get; }
    }
}
