#nullable enable
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    // 범용 토스트 알림 UI. S_PortalLocked 등 서버 거부 메시지 표시.
    //
    // **헌법 #1 (Server Authority)**: 메시지 내용(required/current 킬 수)은 서버 패킷값만 사용.
    // 클라가 독자 판단하지 않음.
    //
    // **싱글톤 사용 이유**: PortalLockedHandler 같은 패킷 핸들러가 Instance를 통해
    // Show를 호출. CombatBootstrap이 씬 진입 직후 BuildRuntime → Instance race window 없음.
    [DisallowMultipleComponent]
    public class ToastUI : MonoBehaviour
    {
        public static ToastUI? Instance { get; private set; }

        [SerializeField] CanvasGroup? _group;
        [SerializeField] TMP_Text? _text;

        Coroutine? _activeFade;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[ToastUI] 중복 박힘 — CombatBootstrap 중복 호출 확인.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (_group != null) _group.alpha = 0f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // 서버 메시지 표시 (main thread 호출 전제).
        // 연속 호출 시 진행 중인 코루틴을 중단하고 새 메시지로 재시작.
        public void Show(string message)
        {
            if (_group == null || _text == null)
            {
                Debug.LogWarning("[ToastUI] _group 또는 _text null — BuildRuntime 누락?");
                return;
            }
            _text.text = message;

            if (_activeFade != null) StopCoroutine(_activeFade);
            _activeFade = StartCoroutine(FadeRoutine());
        }

        // 페이드 인(0.3s) → 표시 유지(2s) → 페이드 아웃(0.4s).
        IEnumerator FadeRoutine()
        {
            if (_group == null) yield break;

            const float fadeIn = 0.3f;
            const float hold = 2.0f;
            const float fadeOut = 0.4f;

            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.deltaTime;
                _group.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            _group.alpha = 1f;

            yield return new WaitForSeconds(hold);

            t = 0f;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                _group.alpha = Mathf.Clamp01(1f - t / fadeOut);
                yield return null;
            }
            _group.alpha = 0f;
            _activeFade = null;
        }

        // ============================================================
        // 런타임 빌드 (CombatBootstrap이 호출). StageClearUI 패턴 동형.
        // Canvas (Screen Space - Overlay) + CanvasGroup + TMP_Text.
        // ============================================================
        public static ToastUI BuildRuntime(Transform parent)
        {
            GameObject root = new GameObject("ToastUI");
            root.transform.SetParent(parent, worldPositionStays: false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1100;

            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            // 하단 중앙 배치 — 포탈 거부 알림은 게임 플레이 방해 최소화 위치.
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform rt = textGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.15f);
            rt.anchorMax = new Vector2(0.5f, 0.15f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(700f, 80f);

            TMP_Text tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = string.Empty;
            tmp.fontSize = 32f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.4f, 0.3f, 1f);
            tmp.fontStyle = FontStyles.Bold;

            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
#if UNITY_EDITOR
            if (font == null)
            {
                font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
#endif
            if (font != null) tmp.font = font;

            ToastUI ui = root.AddComponent<ToastUI>();

            var type = typeof(ToastUI);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            type.GetField("_group", flags)!.SetValue(ui, group);
            type.GetField("_text",  flags)!.SetValue(ui, tmp);

            return ui;
        }
    }
}
