#nullable enable
using UnityEngine;

namespace Dawnholder.Client.Rendering
{
    // 미니맵 카메라 — 메인 카메라(로컬 플레이어 추종 중)를 따라가며 줌아웃된 사이드뷰를
    // RenderTexture에 렌더. UI.unity MinimapPlaceholder의 RawImage가 이 RT를 표시.
    //
    // **추종 전략**: 로컬 플레이어를 직접 찾지 않고 Camera.main 위치를 미러 — 메인 카메라가
    //   이미 CameraFollow로 로컬 플레이어를 추적하므로, 그걸 따라가면 항상 플레이어 중심.
    //
    // **CombatBootstrap이 런타임 생성**. RT는 Resources/MinimapRT, OrthoSize는 줌아웃 정도(영호 조정).
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class MinimapCamera : MonoBehaviour
    {
        const float DefaultOrthoSize = 12f; // 메인(~5)보다 줌아웃 — 영호 Phase05 조정 지점
        const float CamZ = -10f;            // 2D 표준 카메라 z

        Camera _cam = null!;

        public static MinimapCamera BuildRuntime(Transform parent, RenderTexture rt)
        {
            GameObject go = new GameObject("_MinimapCamera");
            go.transform.SetParent(parent, worldPositionStays: false);

            Camera cam = go.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = DefaultOrthoSize;
            cam.targetTexture    = rt;
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.06f, 0.06f, 0.10f, 1f);
            cam.cullingMask      = ~LayerMask.GetMask("UI"); // 월드만 — UI 레이어 제외
            cam.allowHDR         = false;
            cam.allowMSAA        = false;

            MinimapCamera mm = go.AddComponent<MinimapCamera>();
            mm._cam = cam;
            return mm;
        }

        void LateUpdate()
        {
            Camera main = Camera.main;
            if (main == null) return;
            Vector3 p = main.transform.position;
            transform.position = new Vector3(p.x, p.y, CamZ);
        }
    }
}
