#nullable enable
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    // 파티 초대 수락/거절 팝업 — PartyState.OnInviteReceived 구독 → 팝업 표시.
    //
    // **헌법 §1 (Server Authority)**:
    //   클라이언트는 Accept/Reject *의도*만 송신(C_PartyRespond). 실제 파티 구성 판정은 서버.
    //   S_PartyUpdate 수신 시 PartyState.Instance가 갱신됨 → P3~P5 HUD가 구독해 반영.
    //
    // **CombatBootstrap 패턴**:
    //   씬 YAML 편집 없이 BuildRuntime()으로 런타임 생성 (StageClearUI/ToastUI 동형).
    //   싱글톤 Instance — 패킷 핸들러(PartyUpdateHandler)가 OnPartyUpdated 이벤트에서
    //   팝업 숨김을 트리거하기 위해 정적 접근점 필요.
    //
    // **이벤트 구독 해제**:
    //   OnDestroy에서 PartyState 이벤트 구독 해제 → 씬 전환 시 누수 방지.
    [DisallowMultipleComponent]
    public class PartyInvitePopup : MonoBehaviour
    {
        public static PartyInvitePopup? Instance { get; private set; }

        [SerializeField] CanvasGroup? _group;
        [SerializeField] TMP_Text? _inviteText;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[PartyInvitePopup] 중복 박힘 — CombatBootstrap 중복 호출 확인.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_group != null)
            {
                _group.alpha = 0f;
                _group.interactable = false;
                _group.blocksRaycasts = false;
            }
        }

        void OnEnable()
        {
            // OnEnable: Awake보다 늦지만 BuildRuntime 이후 AddComponent 순서상 안전.
            // PartyState는 DontDestroyOnLoad → 씬 전환 후에도 Instance 유지.
            if (PartyState.Instance != null)
            {
                PartyState.Instance.OnInviteReceived += OnInviteReceived;
                PartyState.Instance.OnPartyUpdated   += OnPartyUpdated;
            }
        }

        void OnDisable()
        {
            if (PartyState.Instance != null)
            {
                PartyState.Instance.OnInviteReceived -= OnInviteReceived;
                PartyState.Instance.OnPartyUpdated   -= OnPartyUpdated;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // PartyState.OnInviteReceived 핸들러 — main thread 보장 (PartyInviteRecvHandler → Enqueue).
        void OnInviteReceived()
        {
            PartyState state = PartyState.Instance;
            if (!state.HasPendingInvite) return;

            string className = ((CharacterClass)state.PendingInviterClass).ToString();
            if (_inviteText != null)
                _inviteText.text = $"{className} 파티 초대";

            ShowPopup();
            Debug.Log($"[PartyInvitePopup] 팝업 표시 — inviter={state.PendingInviterEntityId} class={className}");
        }

        // S_PartyUpdate 수신(파티 결성/해산) 시 팝업 숨김.
        // 수락 후 S_PartyUpdate가 오면 자동으로 닫힘 — 별도 타이머 불필요.
        void OnPartyUpdated()
        {
            HidePopup();
        }

        // Accept 버튼 onClick — BuildRuntime에서 AddListener로 연결.
        void OnAcceptClicked()
        {
            SendRespond(accept: 1);
            HidePopup();
        }

        // Reject 버튼 onClick — BuildRuntime에서 AddListener로 연결.
        void OnRejectClicked()
        {
            SendRespond(accept: 0);
            HidePopup();
        }

        void SendRespond(byte accept)
        {
            // PartyState는 DontDestroyOnLoad — 런타임 null 방어.
            if (PartyState.Instance == null) return;
            PartyState state = PartyState.Instance;
            if (!state.HasPendingInvite)
            {
                Debug.LogWarning("[PartyInvitePopup] HasPendingInvite=false — 응답 취소.");
                return;
            }

            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null || !session.HandshakeOk)
            {
                Debug.LogWarning("[PartyInvitePopup] 세션 없음 또는 Handshake 미완료 — 응답 송신 불가.");
                return;
            }

            int inviterId = state.PendingInviterEntityId;
            var pkt = new C_PartyRespond { inviterEntityId = inviterId, accept = accept };
            session.SendIntent(pkt.Write());

            state.ClearPendingInvite();
            Debug.Log($"[PartyInvitePopup] C_PartyRespond 송신 — inviterId={inviterId} accept={accept}");
        }

        void ShowPopup()
        {
            if (_group == null) return;
            _group.alpha = 1f;
            _group.interactable = true;
            _group.blocksRaycasts = true;
        }

        void HidePopup()
        {
            if (_group == null) return;
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

        // ==============================================================
        // 런타임 빌드 (CombatBootstrap이 호출). StageClearUI/ToastUI 패턴 동형.
        // Canvas (Screen Space - Overlay) + 패널 + 텍스트 + Accept/Reject 버튼.
        // ==============================================================
        public static PartyInvitePopup BuildRuntime(Transform parent)
        {
            // ── Canvas root ──────────────────────────────────────────
            GameObject root = new GameObject("PartyInvitePopup");
            root.transform.SetParent(parent, worldPositionStays: false);

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1200; // ToastUI(1100) 위

            root.AddComponent<CanvasScaler>();
            root.AddComponent<GraphicRaycaster>();

            CanvasGroup group = root.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            // ── 중앙 패널 ──────────────────────────────────────────
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot     = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(400f, 180f);

            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.85f); // 반투명 검정 배경

            // ── 초대 텍스트 ──────────────────────────────────────────
            GameObject textGo = new GameObject("InviteText");
            textGo.transform.SetParent(panel.transform, worldPositionStays: false);
            RectTransform textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0.5f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.pivot     = new Vector2(0.5f, 0.5f);
            textRt.offsetMin = new Vector2(10f, 0f);
            textRt.offsetMax = new Vector2(-10f, -10f);

            TMP_Text tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = "파티 초대";
            tmp.fontSize  = 28f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            // TMP 폰트 로드 (ToastUI 패턴 동형).
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
#if UNITY_EDITOR
            if (font == null)
            {
                font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            }
#endif
            if (font != null) tmp.font = font;

            // ── Accept 버튼 ──────────────────────────────────────────
            GameObject acceptGo = BuildButton(panel.transform, "AcceptButton", "수락",
                new Vector2(-70f, -50f), new Color(0.2f, 0.7f, 0.2f, 1f), font);

            // ── Reject 버튼 ──────────────────────────────────────────
            GameObject rejectGo = BuildButton(panel.transform, "RejectButton", "거절",
                new Vector2(70f, -50f), new Color(0.8f, 0.2f, 0.2f, 1f), font);

            // ── PartyInvitePopup 컴포넌트 ──────────────────────────
            PartyInvitePopup popup = root.AddComponent<PartyInvitePopup>();

            // SerializeField reflection 주입 (StageClearUI 패턴 동형).
            var t_popup = typeof(PartyInvitePopup);
            var flags   = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            t_popup.GetField("_group",      flags)!.SetValue(popup, group);
            t_popup.GetField("_inviteText", flags)!.SetValue(popup, tmp);

            // 버튼 onClick 연결 — AddListener로 popup 메서드 연결.
            Button acceptBtn = acceptGo.GetComponent<Button>();
            Button rejectBtn = rejectGo.GetComponent<Button>();
            acceptBtn.onClick.AddListener(popup.OnAcceptClicked);
            rejectBtn.onClick.AddListener(popup.OnRejectClicked);

            return popup;
        }

        // Accept/Reject 공통 버튼 빌더.
        static GameObject BuildButton(Transform parent, string name, string label,
            Vector2 anchoredPos, Color bgColor, TMP_FontAsset? font)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin       = new Vector2(0.5f, 0f);
            rt.anchorMax       = new Vector2(0.5f, 0f);
            rt.pivot           = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta       = new Vector2(120f, 40f);

            Image img = go.AddComponent<Image>();
            img.color = bgColor;

            Button btn = go.AddComponent<Button>();
            // 클릭 시 살짝 어두워지는 Color Tint (Unity 기본 Transition).
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(bgColor.r + 0.1f, bgColor.g + 0.1f, bgColor.b + 0.1f, 1f);
            cb.pressedColor     = new Color(bgColor.r - 0.2f, bgColor.g - 0.2f, bgColor.b - 0.2f, 1f);
            btn.colors = cb;

            // 버튼 레이블 텍스트.
            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, worldPositionStays: false);
            RectTransform labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin  = Vector2.zero;
            labelRt.anchorMax  = Vector2.one;
            labelRt.offsetMin  = Vector2.zero;
            labelRt.offsetMax  = Vector2.zero;

            TMP_Text labelTmp  = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text      = label;
            labelTmp.fontSize  = 22f;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color     = Color.white;
            if (font != null) labelTmp.font = font;

            return go;
        }
    }
}
