#nullable enable
using Dawnholder.Client.State;
using TMPro;
using UnityEngine;

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

        CanvasGroup? _rootGroup;
        bool _subscribed;

        // **자기-비활성 함정 회피**: _root는 이 컴포넌트가 붙은 GameObject(Party_Info) 자신이다.
        // 예전엔 Refresh에서 _root.SetActive(false)로 껐는데, 그러면 컴포넌트 자신이 비활성화돼
        // Update(재바인딩 루프)가 멈추고 OnDisable이 구독을 끊는다 → 이후 OnPartyUpdated를 못 받음.
        // 그래서 "마을에서 파티 수락해도 안 뜨고, 씬 전환으로 UI가 재로드돼 새로 Awake돼야 떴다".
        // 해결: GameObject는 항상 활성 유지하고 CanvasGroup.alpha로만 표시/숨김.
        void Awake()
        {
            if (_root != null)
            {
                _rootGroup = _root.GetComponent<CanvasGroup>() ?? _root.AddComponent<CanvasGroup>();
                _rootGroup.alpha = 0f; // 바인딩 전 빈 패널 깜빡임 방지
            }
        }

        void OnEnable() => TryBind();
        void Start()    => TryBind();

        void Update()
        {
            if (!_subscribed) TryBind();
        }

        void TryBind()
        {
            if (_subscribed || PartyState.Instance == null) return;
            PartyState.Instance.OnPartyUpdated += Refresh;
            _subscribed = true;
            Refresh();
        }

        void OnDisable()
        {
            if (_subscribed && PartyState.Instance != null)
                PartyState.Instance.OnPartyUpdated -= Refresh;
            _subscribed = false;
        }

        void Refresh()
        {
            PartyState ps = PartyState.Instance;
            if (ps == null) return;

            bool inParty = ps.InParty;
            SetVisible(inParty);
            if (!inParty) return;

            ApplySlot(_member0Group, _member0Text, ps.Member0EntityId, ps.Member0Class);
            ApplySlot(_member1Group, _member1Text, ps.Member1EntityId, ps.Member1Class);
        }

        void SetVisible(bool visible)
        {
            if (_rootGroup == null) return;
            _rootGroup.alpha          = visible ? 1f : 0f;
            _rootGroup.blocksRaycasts = visible;
            _rootGroup.interactable   = visible;
        }

        static void ApplySlot(GameObject? group, TMP_Text? text, int entityId, byte cls)
        {
            bool active = entityId != 0;
            if (group != null) group.SetActive(active);
            if (active && text != null)
                text.text = $"{ClassName(cls)} ({entityId})";
        }

        // CharacterClass: Knight=0, Mage=1 (enum 의존 회피 위해 byte 직접 비교).
        static string ClassName(byte cls) => cls == 1 ? "마법사" : "기사";
    }
}
