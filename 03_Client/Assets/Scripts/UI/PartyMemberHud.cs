#nullable enable
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    // 파티 멤버 HUD — UI.unity 씬의 Party_Info 패널에 부착(씬 연결).
    // 서버 PartyState 미러를 구독해 멤버 슬롯을 표시/숨김.
    //
    // **헌법 §1**: 파티 구성은 서버 S_PartyUpdate(PartyState 미러)만 표시. 클라가 임의 변경 X.
    //
    // **UI 씬 재로드 대응**: QuestProgressHud와 동형 — Start/Update 늦은 바인딩 + DontDestroyOnLoad PartyState 구독.
    //
    // **멤버 슬롯**: 정원 2 고정(member0/member1). 빈 슬롯(entityId 0)은 비활성.
    [DisallowMultipleComponent]
    public class PartyMemberHud : MonoBehaviour
    {
        [SerializeField] GameObject? _root;          // Party_Info(이 컴포넌트가 붙은 패널) — CanvasGroup.alpha로 표시 토글
        [SerializeField] GameObject? _member0Group;  // MemberStatus 0
        [SerializeField] TMP_Text?   _member0Text;
        [SerializeField] GameObject? _member1Group;  // MemberStatus 1
        [SerializeField] TMP_Text?   _member1Text;
        // 칸마다 Class_Icon 그림은 그 칸에 들어온 멤버의 직업을 따른다. 고정 그림이면 글자와 어긋난다.
        [SerializeField] Image?      _member0Icon;
        [SerializeField] Image?      _member1Icon;
        [SerializeField] Sprite?     _knightIcon;
        [SerializeField] Sprite?     _mageIcon;

        CanvasGroup? _rootGroup;
        PartyState? _partySource;
        bool _appearanceInitialized;

        // **자기-비활성 함정 회피**: _root는 이 컴포넌트가 붙은 GameObject(Party_Info) 자신이다.
        // 예전엔 Refresh에서 _root.SetActive(false)로 껐는데, 그러면 컴포넌트 자신이 비활성화돼
        // Update(재바인딩 루프)가 멈추고 OnDisable이 구독을 끊는다 → 이후 OnPartyUpdated를 못 받음.
        // 그래서 "마을에서 파티 수락해도 안 뜨고, 씬 전환으로 UI가 재로드돼 새로 Awake돼야 떴다".
        // 해결: GameObject는 항상 활성 유지하고 CanvasGroup.alpha로만 표시/숨김.
        void Awake()
        {
            if (_root != null)
            {
                // ⚠️ `GetComponent ?? AddComponent`는 Unity null 의미(fake-null)와 안 맞아 CanvasGroup이
                //    안 붙는 경우가 있다(MissingComponentException). TryGetComponent로 안전하게.
                if (!_root.TryGetComponent<CanvasGroup>(out var cg))
                    cg = _root.AddComponent<CanvasGroup>();
                _rootGroup = cg;
                _rootGroup.alpha = 0f; // 바인딩 전 빈 패널 깜빡임 방지
                if (!_appearanceInitialized)
                {
                    if (_root.TryGetComponent<Image>(out var background))
                        UiWindowAppearance.ApplyPanel(background);
                    if (_member0Text != null) UiWindowAppearance.AddInsetBehind(_member0Text.rectTransform);
                    if (_member1Text != null) UiWindowAppearance.AddInsetBehind(_member1Text.rectTransform);
                    _appearanceInitialized = true;
                }
            }
        }

        void OnEnable() => TryBind();
        void Start()    => TryBind();

        void Update()
        {
            TryBind();
            // Transport close precedes the owner's queued mirror reset; do not present that roster as current.
            if (UnityClientSession.Instance != null && UnityClientSession.Instance.IsClosed) SetVisible(false);
        }

        void TryBind()
        {
            PartyState? source = PartyState.Instance;
            if (ReferenceEquals(_partySource, source)) return;
            Unbind();
            _partySource = source;
            if (source != null) source.OnPartyUpdated += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            Unbind();
            SetVisible(false);
        }

        void OnDestroy() => Unbind();

        void Unbind()
        {
            // The old mirror may already be a destroyed Unity object; detach from its managed event.
            if (!ReferenceEquals(_partySource, null)) _partySource.OnPartyUpdated -= Refresh;
            _partySource = null;
        }

        void Refresh()
        {
            PartyState? ps = _partySource;
            if (ps == null)
            {
                SetVisible(false);
                return;
            }

            bool inParty = ps.InParty && (UnityClientSession.Instance == null || !UnityClientSession.Instance.IsClosed);
            SetVisible(inParty);
            if (!inParty) return;

            // 서버 순서는 초대한 사람 → 수락한 사람이다. 첫 칸에는 항상 내 캐릭터를 둔다.
            UnityClientSession? session = UnityClientSession.Instance;
            int? self = session != null ? session.LocalEntityId : null;
            bool selfSecond = self.HasValue && ps.Member1EntityId == self.Value;
            ApplySlot(_member0Group, _member0Text, _member0Icon,
                selfSecond ? ps.Member1EntityId : ps.Member0EntityId, selfSecond ? ps.Member1Class : ps.Member0Class);
            ApplySlot(_member1Group, _member1Text, _member1Icon,
                selfSecond ? ps.Member0EntityId : ps.Member1EntityId, selfSecond ? ps.Member0Class : ps.Member1Class);
        }

        void SetVisible(bool visible)
        {
            if (_rootGroup == null) return;
            _rootGroup.alpha          = visible ? 1f : 0f;
            _rootGroup.blocksRaycasts = visible;
            _rootGroup.interactable   = visible;
        }

        void ApplySlot(GameObject? group, TMP_Text? text, Image? icon, int entityId, byte cls)
        {
            bool active = entityId != 0;
            if (group != null) group.SetActive(active);
            if (!active) return;
            if (text != null) text.text = $"{ClassName(cls)} ({entityId})";
            Sprite? sprite = cls == 1 ? _mageIcon : _knightIcon;
            if (icon != null && sprite != null) icon.sprite = sprite;
        }

        // CharacterClass: Knight=0, Mage=1 (enum 의존 회피 위해 byte 직접 비교). 초대 팝업도 같은 이름을 쓴다.
        internal static string ClassName(byte cls) => cls == 1 ? "마법사" : "기사";
    }
}
