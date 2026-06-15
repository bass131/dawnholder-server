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
        [SerializeField] GameObject? _root;          // Party_Info — 파티 없으면 통째로 숨김
        [SerializeField] GameObject? _member0Group;  // MemberStatus 0
        [SerializeField] TMP_Text?   _member0Text;
        [SerializeField] GameObject? _member1Group;  // MemberStatus 1
        [SerializeField] TMP_Text?   _member1Text;

        bool _subscribed;

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
            if (_root != null) _root.SetActive(inParty);
            if (!inParty) return;

            ApplySlot(_member0Group, _member0Text, ps.Member0EntityId, ps.Member0Class);
            ApplySlot(_member1Group, _member1Text, ps.Member1EntityId, ps.Member1Class);
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
