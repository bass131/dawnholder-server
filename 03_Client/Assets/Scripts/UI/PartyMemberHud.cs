#nullable enable
using Dawnholder.Client.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    // P3 파티 멤버 HUD. 화면 상단 중앙 하단 — 파티 결성 시 멤버 슬롯 2개 표시, 해산 시 숨김.
    //
    // **헌법 §1**: 파티 상태는 서버 S_PartyUpdate 통보(PartyState 미러)만 표시.
    //   클라가 파티 멤버를 임의로 추가·제거하지 않음.
    //
    // **swap-ready**: 배경 Image는 Resources.Load<Sprite>("UI/Status_Frame") 경로.
    //   에셋 미배치 시 배경 없이 텍스트만(graceful fallback). 에셋 배치 시 코드 변경 없이 자동 적용.
    //
    // **이벤트 구독 해제**: OnEnable/OnDisable 짝으로 관리해 씬 전환·비활성화 시 누수 방지.
    [DisallowMultipleComponent]
    public class PartyMemberHud : MonoBehaviour
    {
        // 패널 배경 9-slice sprite (Menu_Button) 경로.
        const string BgSpritePath = "UI/Menu_Button";

        public static PartyMemberHud? Instance { get; private set; }

        [SerializeField] CanvasGroup? _group;
        [SerializeField] TMP_Text?    _slot0Text;
        [SerializeField] TMP_Text?    _slot1Text;
        [SerializeField] Sprite?      _bgSprite;   // Inspector 또는 Resources.Load로 주입 가능한 swap 슬롯

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[PartyMemberHud] 중복 박힘 — CombatBootstrap 중복 호출 확인.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            HideHud();
        }

        void OnEnable()
        {
            if (PartyState.Instance != null)
                PartyState.Instance.OnPartyUpdated += Refresh;
        }

        void OnDisable()
        {
            if (PartyState.Instance != null)
                PartyState.Instance.OnPartyUpdated -= Refresh;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Refresh()
        {
            if (PartyState.Instance == null) return;

            if (!PartyState.Instance.InParty)
            {
                HideHud();
                return;
            }

            ShowHud();

            if (_slot0Text != null)
            {
                bool active = PartyState.Instance.Member0EntityId != 0;
                _slot0Text.gameObject.SetActive(active);
                if (active)
                    _slot0Text.text = $"Member: {PartyState.Instance.Member0EntityId} (cls {PartyState.Instance.Member0Class})";
            }

            if (_slot1Text != null)
            {
                bool active = PartyState.Instance.Member1EntityId != 0;
                _slot1Text.gameObject.SetActive(active);
                if (active)
                    _slot1Text.text = $"Member: {PartyState.Instance.Member1EntityId} (cls {PartyState.Instance.Member1Class})";
            }
        }

        void ShowHud()
        {
            if (_group != null) _group.alpha = 1f;
        }

        void HideHud()
        {
            if (_group != null) _group.alpha = 0f;
        }

        // ============================================================
        // 런타임 빌드 (CombatBootstrap이 호출). StageClearUI/ToastUI 패턴 동형.
        // Canvas (Screen Space - Overlay) + 좌상단 패널 + TMP_Text 슬롯 2개.
        // ============================================================
        public static PartyMemberHud BuildRuntime(Transform parent)
        {
            GameObject root = new GameObject("PartyMemberHud");
            root.transform.SetParent(parent, worldPositionStays: false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;

            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            // 배경 패널 — 상단 중앙 하단. 퀘스트 HUD(0,-20, 360×120) 아래 겹침 없이.
            // M6: 영호 Phase05 육안 미세조정 지점
            GameObject panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 1f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot     = new Vector2(0.5f, 1f);
            panelRt.anchoredPosition = new Vector2(0f, -150f);
            panelRt.sizeDelta = new Vector2(280f, 90f);

            Image panelImg = panelGo.AddComponent<Image>();

            // 배경 sprite 있으면 9-slice 패널, 없으면 반투명 폴백.
            Sprite? bgSprite = Resources.Load<Sprite>(BgSpritePath);
            if (bgSprite != null)
            {
                panelImg.sprite = bgSprite;
                panelImg.type   = Image.Type.Sliced;
                panelImg.pixelsPerUnitMultiplier = 3f;
                panelImg.color  = Color.white;
            }
            else
            {
                panelImg.color = new Color(0f, 0f, 0f, 0.55f);
            }

            // TMP 공통 font 로드.
            TMP_FontAsset? font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
#if UNITY_EDITOR
            if (font == null)
            {
                font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
#endif

            TMP_Text slot0 = MakeSlotText(panelGo.transform, font, anchoredY: -15f);
            TMP_Text slot1 = MakeSlotText(panelGo.transform, font, anchoredY: -50f);

            PartyMemberHud hud = root.AddComponent<PartyMemberHud>();

            var type  = typeof(PartyMemberHud);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            type.GetField("_group",     flags)!.SetValue(hud, group);
            type.GetField("_slot0Text", flags)!.SetValue(hud, slot0);
            type.GetField("_slot1Text", flags)!.SetValue(hud, slot1);
            if (bgSprite != null)
                type.GetField("_bgSprite", flags)!.SetValue(hud, bgSprite);

            return hud;
        }

        static TMP_Text MakeSlotText(Transform parent, TMP_FontAsset? font, float anchoredY)
        {
            GameObject go = new GameObject("SlotText");
            go.transform.SetParent(parent, worldPositionStays: false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, anchoredY);
            rt.sizeDelta = new Vector2(0f, 30f);

            TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = string.Empty;
            tmp.fontSize  = 18f;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color     = Color.white;
            if (font != null) tmp.font = font;

            go.SetActive(false);
            return tmp;
        }
    }
}
