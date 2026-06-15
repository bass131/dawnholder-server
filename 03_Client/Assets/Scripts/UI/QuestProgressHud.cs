#nullable enable
using Dawnholder.Client.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    // P4 퀘스트 진행 HUD. 화면 상단 중앙 — S_QuestUpdate 수신 시 이름/목표/카운트 3줄 표시.
    //
    // **헌법 §1**: 카운터·목표치 모두 서버 QuestState 미러값만 사용. 40 같은 리터럴 하드코딩 X.
    //   이름·목표 텍스트는 표시용 클라 로컬 콘텐츠 — 서버 권위 대상 아님 (단일 퀘스트).
    //
    // **swap-ready**: 배경 Image는 Resources.Load<Sprite>("UI/Quest_Panel") 경로.
    //   에셋 미배치 시 배경 없이 텍스트만(graceful fallback).
    //
    // **이벤트 구독 해제**: OnEnable/OnDisable 짝 — 씬 전환·비활성화 시 누수 방지.
    [DisallowMultipleComponent]
    public class QuestProgressHud : MonoBehaviour
    {
        // 패널 배경 9-slice sprite (Menu_Button) 경로.
        const string BgSpritePath = "UI/Menu_Button";

        // 영호 Phase05 조정 지점 — 플레이테스트 후 문구 미세조정.
        const string QuestName      = "마을의 위협";
        const string QuestObjective = "사냥터의 몬스터 처치";

        public static QuestProgressHud? Instance { get; private set; }

        [SerializeField] CanvasGroup? _group;
        [SerializeField] TMP_Text?    _infoText;
        [SerializeField] Sprite?      _bgSprite;   // swap 슬롯

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[QuestProgressHud] 중복 박힘 — CombatBootstrap 중복 호출 확인.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (_group != null) _group.alpha = 0f;
        }

        void OnEnable()
        {
            if (QuestState.Instance != null)
                QuestState.Instance.OnQuestUpdated += Refresh;
        }

        void OnDisable()
        {
            if (QuestState.Instance != null)
                QuestState.Instance.OnQuestUpdated -= Refresh;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Refresh()
        {
            if (QuestState.Instance == null || _infoText == null) return;

            // 서버값만 — targetCount 리터럴 금지 (헌법 §1). 이름·목표는 클라 로컬 const.
            _infoText.text = $"{QuestName}\n{QuestObjective}\n{QuestState.Instance.CurrentCount} / {QuestState.Instance.TargetCount}";

            if (_group != null) _group.alpha = 1f;
        }

        // ============================================================
        // 런타임 빌드 (CombatBootstrap이 호출). ToastUI/StageClearUI 패턴 동형.
        // Canvas (Screen Space - Overlay) + 상단 중앙 패널 + TMP_Text.
        // ============================================================
        public static QuestProgressHud BuildRuntime(Transform parent)
        {
            GameObject root = new GameObject("QuestProgressHud");
            root.transform.SetParent(parent, worldPositionStays: false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 910;

            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            // 배경 패널 — 상단 중앙 고정.
            GameObject panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform panelRt = panelGo.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 1f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot     = new Vector2(0.5f, 1f);
            panelRt.anchoredPosition = new Vector2(0f, -20f);
            panelRt.sizeDelta = new Vector2(360f, 120f);

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

            // TMP_Text — 패널 전체 채움. 이름/목표/카운트 3줄 멀티라인.
            GameObject textGo = new GameObject("InfoText");
            textGo.transform.SetParent(panelGo.transform, worldPositionStays: false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TMP_Text tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text           = string.Empty;
            tmp.fontSize       = 20f;
            tmp.lineSpacing    = 4f;
            tmp.alignment      = TextAlignmentOptions.Center;
            tmp.color          = Color.white;
            tmp.enableWordWrapping = false;

            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
#if UNITY_EDITOR
            if (font == null)
            {
                font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
#endif
            if (font != null) tmp.font = font;

            QuestProgressHud hud = root.AddComponent<QuestProgressHud>();

            var type  = typeof(QuestProgressHud);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            type.GetField("_group",     flags)!.SetValue(hud, group);
            type.GetField("_infoText",  flags)!.SetValue(hud, tmp);
            if (bgSprite != null)
                type.GetField("_bgSprite", flags)!.SetValue(hud, bgSprite);

            return hud;
        }
    }
}
