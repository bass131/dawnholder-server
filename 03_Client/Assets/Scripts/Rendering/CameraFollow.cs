using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("target")]
        [SerializeField, Tooltip("따라갈 대상 (Player Transform)")]
        Transform _target;

        [FormerlySerializedAs("offset")]
        [SerializeField, Tooltip("target 기준 카메라 오프셋. 2D는 z=-10이 표준.")]
        Vector3 _offset = new Vector3(0f, 1f, -10f);

        [FormerlySerializedAs("smoothing")]
        [SerializeField, Range(0.01f, 1f), Tooltip("0.15 정도가 부드러움 적당")]
        float _smoothing = 0.15f;

        // M4.2 Phase 04: 동적 spawn 대상 연결.
        //   LocalPlayer는 런타임 spawn(LocalPlayerSpawner)이라 씬 Inspector에서 target을 미리
        //   연결할 수 없음. spawn 직후 Spawner가 이 메서드로 꽂아줌 ("생성 후 셋업").
        //   연결 즉시 target 위치로 snap — 안 그러면 첫 LateUpdate에서 씬 저장 위치(예 Town 3,7)
        //   부터 Lerp로 주르륵 이동해 "위치 렉"처럼 보임 (맵 전환 직후).
        public void SetTarget(Transform t)
        {
            _target = t;
            if (t != null) transform.position = t.position + _offset; // 즉시 snap
        }

        void LateUpdate()
        {
            if (_target == null) return;
            Vector3 desired = _target.position + _offset;
            transform.position = Vector3.Lerp(transform.position, desired, _smoothing);
        }
    }
}
