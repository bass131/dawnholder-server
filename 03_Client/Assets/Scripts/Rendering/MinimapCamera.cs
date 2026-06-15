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
    //
    // **MinimapMarkers 접근용 Instance**: dot 마커 컴포넌트가 WorldToViewportPoint 좌표 변환에
    //   이 카메라를 써야 하므로 싱글톤 노출. BuildRuntime에서 set, OnDestroy에서 해제.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class MinimapCamera : MonoBehaviour
    {
        public static MinimapCamera? Instance { get; private set; }

        const float DefaultOrthoSize = 12f; // 메인(~5)보다 줌아웃 — 영호 Phase05 조정 지점
        const float CamZ = -10f;            // 2D 표준 카메라 z

        Camera _cam = null!;

        // MinimapMarkers 등 외부 컴포넌트가 좌표 변환에 사용하는 카메라 참조.
        public Camera Cam => _cam;

        public static MinimapCamera BuildRuntime(Transform parent, RenderTexture rt)
        {
            GameObject go = new GameObject("_MinimapCamera");
            go.transform.SetParent(parent, worldPositionStays: false);

            Camera cam = go.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = DefaultOrthoSize;
            cam.targetTexture    = rt;
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = Color.black;
            cam.allowHDR         = false;
            cam.allowMSAA        = false;

            // MinimapTerrain Layer만 렌더 — 지형/데코를 이 Layer로 배정하면 깔끔한 미니맵 배경.
            // 메인 세션이 TagManager에 "MinimapTerrain" Layer를 추가하고 씬 지형에 배정하기 전까지는
            // GetMask가 0을 반환해 화면이 전부 검정이 된다. 그 전에도 뭔가 보이도록 fallback 적용.
            int terrainMask = LayerMask.GetMask("MinimapTerrain");
            if (terrainMask == 0)
            {
                // MinimapTerrain Layer 미존재 — UI 제외 전체를 임시 렌더(Layer 배정 전 개발 확인용).
                Debug.LogWarning("[MinimapCamera] 'MinimapTerrain' Layer 미존재 — UI 제외 전체 렌더 fallback 적용. TagManager에 Layer 추가 후 지형 GameObject에 배정 필요.");
                cam.cullingMask = ~LayerMask.GetMask("UI");
            }
            else
            {
                cam.cullingMask = terrainMask;
            }

            MinimapCamera mm = go.AddComponent<MinimapCamera>();
            mm._cam = cam;
            Instance = mm;
            return mm;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
