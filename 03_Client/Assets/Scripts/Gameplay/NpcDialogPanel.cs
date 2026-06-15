#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dawnholder.Client.Gameplay
{
    // NPC 대화 박스 UI (텍스트 + 초상화 출력).
    //
    // **헌법 §1**: 상태 변경 없는 텍스트 표시 전용 → 클라 단독 OK.
    //   퀘스트 진행·보상 지급 등 상태 변경은 서버 권위 의무.
    //
    // **BuildRuntime 패턴**: CombatBootstrap이 씬 진입 시 자동 생성.
    //   Scene 수동 배치 불필요 → _instance null 경고가 발동할 일 없음.
    //
    // **새 InputSystem**: ESC / E 재입력 → Hide. 레거시 Input.GetKey 금지(헌법 정합).
    [DisallowMultipleComponent]
    public class NpcDialogPanel : MonoBehaviour
    {
        const string BgSpritePath = "UI/Menu_Button";

        static NpcDialogPanel? _instance;

        public static NpcDialogPanel? Instance => _instance;

        [SerializeField] GameObject? _root;
        [SerializeField] TMP_Text?   _dialogText;
        [SerializeField] TMP_Text?   _closeHintText;
        [SerializeField] Image?      _portraitImage;
        [SerializeField] string      _defaultCloseHint = "[E] 또는 [ESC] — 닫기";

        bool        _isShown;
        InputAction? _closeAction;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogError("[NpcDialogPanel] 중복 박힘 — CombatBootstrap 중복 호출 확인.");
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (_root != null) _root.SetActive(false);
            if (_closeHintText != null) _closeHintText.text = _defaultCloseHint;
            if (_portraitImage != null) _portraitImage.gameObject.SetActive(false);

            _closeAction = new InputAction("NpcDialogClose", InputActionType.Button);
            _closeAction.AddBinding("<Keyboard>/escape");
            _closeAction.AddBinding("<Keyboard>/e");
            _closeAction.performed += OnClosePerformed;
        }

        void OnDestroy()
        {
            if (_closeAction != null)
            {
                _closeAction.performed -= OnClosePerformed;
                _closeAction.Disable();
                _closeAction.Dispose();
            }
            if (_instance == this) _instance = null;
        }

        void OnClosePerformed(InputAction.CallbackContext _)
        {
            if (_isShown) Hide();
        }

        public static void Show(string text) => Show(text, null);

        /// <summary>
        /// 대화 박스 표시. portrait != null이면 초상화도 표시.
        /// 이미 열려 있으면 silent skip (중복 표시 가드).
        /// </summary>
        public static void Show(string text, Sprite? portrait)
        {
            if (_instance == null)
            {
                // BuildRuntime으로 항상 생성되므로 정상 플레이에서 발동 X.
                Debug.LogWarning("[NpcDialogPanel] Instance is null — CombatBootstrap 누락 확인.");
                return;
            }
            if (_instance._isShown) return;

            _instance._isShown = true;

            if (_instance._dialogText != null)
                _instance._dialogText.text = text;

            if (_instance._portraitImage != null)
            {
                bool hasPortrait = portrait != null;
                _instance._portraitImage.gameObject.SetActive(hasPortrait);
                if (hasPortrait) _instance._portraitImage.sprite = portrait;
            }

            if (_instance._root != null) _instance._root.SetActive(true);
            _instance._closeAction?.Enable();
        }

        public static void Hide()
        {
            if (_instance == null) return;
            _instance._isShown = false;
            if (_instance._root != null) _instance._root.SetActive(false);
            _instance._closeAction?.Disable();
        }

        public static bool IsShown => _instance != null && _instance._isShown;

        // ============================================================
        // 런타임 빌드 (CombatBootstrap이 호출). PartyMemberHud/QuestProgressHud 패턴 동형.
        // Canvas sortingOrder 920 — PartyMemberHud(900)/QuestProgressHud(910) 위,
        // StageClearUI(1000) 아래.
        // ============================================================
        public static NpcDialogPanel BuildRuntime(Transform parent)
        {
            // idempotent — 이미 있으면 기존 반환.
            if (_instance != null) return _instance;

            GameObject root = new GameObject("NpcDialogPanel");
            root.transform.SetParent(parent, worldPositionStays: false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 920;

            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            // 대화창 — 화면 하단 중앙.
            // 영호 Phase 05 조정 지점: anchoredPosition / sizeDelta.
            GameObject panelGo = new GameObject("DialogPanel");
            panelGo.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin        = new Vector2(0.5f, 0f);
            panelRt.anchorMax        = new Vector2(0.5f, 0f);
            panelRt.pivot            = new Vector2(0.5f, 0f);
            panelRt.anchoredPosition = new Vector2(0f, 40f);
            panelRt.sizeDelta        = new Vector2(700f, 160f);

            Image panelImg = panelGo.AddComponent<Image>();
            Sprite? bgSprite = Resources.Load<Sprite>(BgSpritePath);
            if (bgSprite != null)
            {
                panelImg.sprite                  = bgSprite;
                panelImg.type                    = Image.Type.Sliced;
                panelImg.pixelsPerUnitMultiplier = 3f;
                panelImg.color                   = Color.white;
            }
            else
            {
                panelImg.color = new Color(0f, 0f, 0f, 0.75f);
            }

            // 초상화 Image — 패널 좌측.
            // 영호 Phase 05 조정 지점: 초상화 크기(sizeDelta) / 위치.
            GameObject portraitGo = new GameObject("Portrait");
            portraitGo.transform.SetParent(panelGo.transform, worldPositionStays: false);
            RectTransform portraitRt = portraitGo.AddComponent<RectTransform>();
            portraitRt.anchorMin        = new Vector2(0f, 0.5f);
            portraitRt.anchorMax        = new Vector2(0f, 0.5f);
            portraitRt.pivot            = new Vector2(0f, 0.5f);
            portraitRt.anchoredPosition = new Vector2(16f, 0f);
            portraitRt.sizeDelta        = new Vector2(128f, 128f);

            Image portraitImg = portraitGo.AddComponent<Image>();
            portraitImg.preserveAspect = true;
            portraitGo.SetActive(false);   // sprite 주입 전까지 숨김

            // TMP 폰트 로드 — 전역 폴백으로 한글 처리.
            TMP_FontAsset? font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
#if UNITY_EDITOR
            if (font == null)
            {
                font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
#endif

            // 대사 TMP_Text — 초상화 우측, 패널 나머지 영역.
            GameObject textGo = new GameObject("DialogText");
            textGo.transform.SetParent(panelGo.transform, worldPositionStays: false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin        = new Vector2(0f, 0f);
            textRt.anchorMax        = new Vector2(1f, 1f);
            // 좌측 160 = 초상화(128) + 좌 패딩(16) + 간격(16).
            textRt.offsetMin        = new Vector2(160f, 32f);
            textRt.offsetMax        = new Vector2(-16f, -16f);

            TMP_Text dialogTmp = textGo.AddComponent<TextMeshProUGUI>();
            dialogTmp.text      = string.Empty;
            dialogTmp.fontSize  = 22f;
            dialogTmp.alignment = TextAlignmentOptions.TopLeft;
            dialogTmp.color     = Color.white;
            if (font != null) dialogTmp.font = font;

            // 닫기 안내 TMP_Text — 패널 하단 우측.
            GameObject hintGo = new GameObject("CloseHintText");
            hintGo.transform.SetParent(panelGo.transform, worldPositionStays: false);
            RectTransform hintRt = hintGo.AddComponent<RectTransform>();
            hintRt.anchorMin        = new Vector2(1f, 0f);
            hintRt.anchorMax        = new Vector2(1f, 0f);
            hintRt.pivot            = new Vector2(1f, 0f);
            hintRt.anchoredPosition = new Vector2(-16f, 8f);
            hintRt.sizeDelta        = new Vector2(280f, 24f);

            TMP_Text hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            hintTmp.text      = string.Empty;   // Awake에서 _defaultCloseHint로 세팅
            hintTmp.fontSize  = 14f;
            hintTmp.alignment = TextAlignmentOptions.Right;
            hintTmp.color     = new Color(1f, 1f, 1f, 0.7f);
            if (font != null) hintTmp.font = font;

            // 패널 시작 시 숨김.
            panelGo.SetActive(false);

            NpcDialogPanel panel = root.AddComponent<NpcDialogPanel>();

            var type  = typeof(NpcDialogPanel);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            type.GetField("_root",          flags)!.SetValue(panel, panelGo);
            type.GetField("_dialogText",    flags)!.SetValue(panel, dialogTmp);
            type.GetField("_closeHintText", flags)!.SetValue(panel, hintTmp);
            type.GetField("_portraitImage", flags)!.SetValue(panel, portraitImg);

            return panel;
        }
    }
}
