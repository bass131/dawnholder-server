using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // Phase 01 (M2): 카메라가 target을 부드럽게 따라간다.
    //
    // LateUpdate 이유:
    //   캐릭터(Update)가 먼저 움직이고 → 같은 프레임에 카메라가 따라가야
    //   한 프레임 늦은 "덜덜거림"이 없다.
    //
    // smoothing은 매 프레임 Lerp 계수. 1에 가까울수록 즉시 따라감, 작을수록 부드러움.
    // fps에 따른 체감 차이를 줄이려면 1 - Mathf.Pow(1 - smoothing, deltaTime / refDt) 같은
    // 보정이 가능하지만, 학습 단계라 단순 Lerp으로 시작.
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField, Tooltip("따라갈 대상 (Player Transform)")]
        Transform target;

        [SerializeField, Tooltip("target 기준 카메라 오프셋. 2D는 z=-10이 표준.")]
        Vector3 offset = new Vector3(0f, 1f, -10f);

        [SerializeField, Range(0.01f, 1f), Tooltip("0.15 정도가 부드러움 적당")]
        float smoothing = 0.15f;

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, smoothing);
        }
    }
}
