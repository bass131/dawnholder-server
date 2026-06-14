#nullable enable
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    // Stage Clear UI. S_StageClear 수신 → 화면 중앙 연출.
    //
    // **헌법 #1 (Server Authority)**: 본 UI는 *서버 신호를 표시만* 합니다. 보스 사망 판정 / 진행도
    // 산정 *클라가 절대 하지 않음*. 서버 S_StageClear 경로가 본 컴포넌트 Show 호출.
    //
    // **싱글톤 사용 이유**: 패킷 핸들러가 정적 접근으로 Show 호출. Instance가 없으면
    // (씬 진입 race) 큐 없이 silent drop. 정상 흐름엔 CombatBootstrap이 씬 진입
    // 직후 박아서 race window 거의 0.
    //
    // **Image/Animator swap-ready 경로**:
    //   controller → Assets/Resources/UI/StageClearAnim.controller
    //   sprite     → Assets/Resources/UI/StageClearPlaceholder.sprite (또는 .png)
    // 위 에셋을 배치하면 코드 변경 없이 애니 경로로 자동 전환.
    // 미배치 시 TMP 텍스트 폴백 유지(무연출 회귀 없음).
    [DisallowMultipleComponent]
    public class StageClearUI : MonoBehaviour
    {
        // Resources 경로 상수 — swap 지점.
        const string AnimControllerPath = "UI/StageClearAnim";
        const string PlaceholderSpritePath = "UI/StageClearPlaceholder";

        public static StageClearUI? Instance { get; private set; }

        [SerializeField] CanvasGroup? _group;
        [SerializeField] TMP_Text? _text;
        [SerializeField] Image? _image;
        [SerializeField] Animator? _animator;

        Coroutine? _activeFade;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[StageClearUI] 중복 박힘 — 씬에 여러 인스턴스. 본인 셋업 확인.");
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

        // 서버 S_StageClear 경로 (main thread).
        // bossEntityId는 로깅용 — UI에는 단순히 연출만 표시.
        public void Show(int bossEntityId)
        {
            Debug.Log($"[StageClearUI] Stage Clear! (boss entity {bossEntityId})");
            if (_group == null)
            {
                Debug.LogWarning("[StageClearUI] _group null — Canvas wire 누락.");
                return;
            }

            bool imageReady = _image != null && (_animator?.runtimeAnimatorController != null || _image.sprite != null);

            if (imageReady)
            {
                // Image 경로: TMP 숨기고 Image 전면에 표시.
                if (_text != null) _text.gameObject.SetActive(false);
                if (_image != null)
                {
                    // SetActive(true) → Animator도 함께 활성화 → Default State 자동 재생.
                    _image.gameObject.SetActive(true);
                }
            }
            else
            {
                // TMP 폴백: Image 에셋 미배치 시 기존 텍스트 연출 유지.
                if (_text != null)
                {
                    _text.gameObject.SetActive(true);
                    _text.text = "Stage Clear!";
                }
            }

            if (_activeFade != null) StopCoroutine(_activeFade);
            _activeFade = StartCoroutine(FadeRoutine());
        }

        // 페이드 인: 0 → 1 (0.4s) 후 유지 (manual close).
        IEnumerator FadeRoutine()
        {
            if (_group == null) yield break;
            const float fadeIn = 0.4f;
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.deltaTime;
                _group.alpha = Mathf.Clamp01(t / fadeIn);
                yield return null;
            }
            _group.alpha = 1f;
        }

        // ============================================================
        // 런타임 빌드 (CombatBootstrap이 호출).
        // Canvas (Screen Space - Overlay) + Image(애니 경로) + TMP_Text(폴백).
        // ============================================================
        public static StageClearUI BuildRuntime(Transform parent)
        {
            GameObject root = new GameObject("StageClearUI");
            root.transform.SetParent(parent, worldPositionStays: false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            // --- Image GameObject (애니 스프라이트 경로) ---
            GameObject imageGo = new GameObject("StageClearImage");
            imageGo.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform imageRt = imageGo.AddComponent<RectTransform>();
            imageRt.anchorMin = new Vector2(0.5f, 0.5f);
            imageRt.anchorMax = new Vector2(0.5f, 0.5f);
            imageRt.pivot = new Vector2(0.5f, 0.5f);
            imageRt.anchoredPosition = Vector2.zero;
            imageRt.sizeDelta = new Vector2(900f, 300f);

            Image img = imageGo.AddComponent<Image>();
            img.preserveAspect = true;

            // placeholder sprite 로드 시도 — 없으면 null 유지.
            var sprite = Resources.Load<Sprite>(PlaceholderSpritePath);
            if (sprite != null) img.sprite = sprite;

            // Animator 슬롯 — controller 미배치여도 컴포넌트는 존재(swap-ready).
            Animator anim = imageGo.AddComponent<Animator>();
            var controller = Resources.Load<RuntimeAnimatorController>(AnimControllerPath);
            if (controller != null) anim.runtimeAnimatorController = controller;

            // controller/sprite 없으면 ImageGO 비활성 — TMP 폴백 경로 확보.
            imageGo.SetActive(false);

            // --- TMP_Text GameObject (폴백) ---
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform rt = textGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(900f, 200f);

            TMP_Text tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "Stage Clear!";
            tmp.fontSize = 96f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.92f, 0.2f, 1f);
            tmp.fontStyle = FontStyles.Bold;
            // TMP Font Asset 명시 할당 — 자동 fallback 경고 봉합.
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
#if UNITY_EDITOR
            if (font == null)
            {
                font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
#endif
            if (font != null) tmp.font = font;

            StageClearUI ui = root.AddComponent<StageClearUI>();

            // SerializeField reflection 주입 — 기존 _group/_text 패턴 동형.
            var t_ui = typeof(StageClearUI);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            t_ui.GetField("_group",    flags)!.SetValue(ui, group);
            t_ui.GetField("_text",     flags)!.SetValue(ui, tmp);
            t_ui.GetField("_image",    flags)!.SetValue(ui, img);
            t_ui.GetField("_animator", flags)!.SetValue(ui, anim);

            return ui;
        }
    }
}
