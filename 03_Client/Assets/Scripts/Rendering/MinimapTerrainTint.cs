#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace Dawnholder.Client.Rendering
{
    // 미니맵에서 "밟는 지형"(타일맵 + 바닥)이 어두워 안 보이는 문제 해결.
    // URP는 Camera.SetReplacementShader 미지원 → RenderPipelineManager per-camera 콜백으로
    // 미니맵 카메라가 그릴 "직전"에만 walkable 지형의 머티리얼을 평면 단색(Dawnholder/MinimapFlat)으로
    // 바꾸고 "직후" 원복. 카메라별 순차 렌더라 메인 카메라에는 영향 없음.
    //
    // 데코는 제외(이미 보임 — 영호) — 타일맵 + 이름이 Ground/Platform/Floor인 스프라이트만.
    [DisallowMultipleComponent]
    public class MinimapTerrainTint : MonoBehaviour
    {
        // 영호 튜닝 지점 — 미니맵 walkable 지형 평면 색.
        static readonly Color WalkableColor = new Color(0.85f, 0.7f, 0.45f, 1f); // 밝은 흙/모래색

        const int MinimapLayer = 8; // MinimapTerrain

        Material? _flatMat;
        readonly List<Renderer> _walkable = new();
        readonly List<Material> _orig = new();
        bool _hooked;

        public static MinimapTerrainTint Install(Transform parent)
        {
            GameObject go = new GameObject("_MinimapTerrainTint");
            go.transform.SetParent(parent, worldPositionStays: false);
            return go.AddComponent<MinimapTerrainTint>();
        }

        void Start()
        {
            Shader sh = Shader.Find("Dawnholder/MinimapFlat");
            if (sh == null)
            {
                Debug.LogWarning("[MinimapTerrainTint] 'Dawnholder/MinimapFlat' 셰이더 없음 — 지형 틴트 비활성.");
                enabled = false;
                return;
            }
            _flatMat = new Material(sh);
            _flatMat.SetColor("_FlatColor", WalkableColor);
            Collect();
        }

        void Collect()
        {
            _walkable.Clear();
            _orig.Clear();

            foreach (TilemapRenderer tr in FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None))
                if (tr.gameObject.layer == MinimapLayer)
                {
                    _walkable.Add(tr);
                    _orig.Add(tr.sharedMaterial);
                }

            foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (sr.gameObject.layer != MinimapLayer) continue;
                string n = sr.gameObject.name;
                if (n.Contains("Ground") || n.Contains("Platform") || n.Contains("Floor"))
                {
                    _walkable.Add(sr);
                    _orig.Add(sr.sharedMaterial);
                }
            }
        }

        void OnEnable()
        {
            if (_hooked) return;
            RenderPipelineManager.beginCameraRendering += OnBegin;
            RenderPipelineManager.endCameraRendering   += OnEnd;
            _hooked = true;
        }

        void OnDisable()
        {
            if (!_hooked) return;
            RenderPipelineManager.beginCameraRendering -= OnBegin;
            RenderPipelineManager.endCameraRendering   -= OnEnd;
            _hooked = false;
            Restore(); // 예외로 swap된 채 남는 것 방지
        }

        bool IsMinimap(Camera cam) => MinimapCamera.Instance != null && cam == MinimapCamera.Instance.Cam;

        void OnBegin(ScriptableRenderContext ctx, Camera cam)
        {
            if (_flatMat == null || !IsMinimap(cam)) return;
            for (int i = 0; i < _walkable.Count; i++)
                if (_walkable[i] != null) _walkable[i].sharedMaterial = _flatMat;
        }

        void OnEnd(ScriptableRenderContext ctx, Camera cam)
        {
            if (!IsMinimap(cam)) return;
            Restore();
        }

        void Restore()
        {
            for (int i = 0; i < _walkable.Count; i++)
                if (_walkable[i] != null) _walkable[i].sharedMaterial = _orig[i];
        }
    }
}
