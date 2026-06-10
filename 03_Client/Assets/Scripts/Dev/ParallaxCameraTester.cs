using UnityEngine;
using UnityEngine.InputSystem;

namespace Dawnholder.Client.Dev
{
    // [임시/테스트용] 패럴랙스 확인을 위한 카메라 수동 패닝.
    //   - 이 씬에는 플레이어가 없어 카메라를 움직일 주체가 없으므로, 키보드로 직접 패닝한다.
    //   - Main Camera의 CameraFollow는 _target == null 이면 LateUpdate에서 즉시 return 하므로 충돌하지 않는다.
    //   - 확인이 끝나면 이 컴포넌트(및 스크립트)는 제거해도 된다.
    public class ParallaxCameraTester : MonoBehaviour
    {
        [SerializeField, Tooltip("패닝 속도 (월드 유닛/초)")]
        float _speed = 8f;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            float x = 0f, y = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;

            if (x == 0f && y == 0f) return;

            transform.position += new Vector3(x, y, 0f) * (_speed * Time.deltaTime);
        }
    }
}
