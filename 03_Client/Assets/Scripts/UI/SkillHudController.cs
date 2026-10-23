#nullable enable
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Input;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// 스킬 쿨다운 HUD — Q/E 슬롯 fill 표시 전담.
    ///
    /// **헌법 #1 (Server Authority)**: 이 컨트롤러는 LocalPlayerMovement의 *예측 타이머*를
    /// 시각화할 뿐입니다. 시전 허용/거부 권위는 서버 — 클라 타이머가 어긋나도 서버가 최종 판정.
    ///
    /// **갱신 패턴**: 쿨다운은 로컬 예측 타이머(frame dt 감쇠) → 매 프레임 폴링이 정석.
    /// HP처럼 서버 패킷 푸시를 기다리지 않음.
    /// </summary>
    public class SkillHudController : MonoBehaviour
    {
        public static SkillHudController? Instance { get; private set; }

        [Header("Q 슬롯 (클래스별 — Mage: Thunderbolt / Knight: Dash)")]
        [FormerlySerializedAs("_qSlotImage")]
        [SerializeField] Image? _qCooldownImage;
        [SerializeField] Image? _qIconImage;
        [SerializeField] TMP_Text? _qCooldownText;

        [Header("E 슬롯 (Mage: Teleport / Knight: 미사용 — 숨김)")]
        [FormerlySerializedAs("_eSlotImage")]
        [SerializeField] Image? _eCooldownImage;
        [SerializeField] Image? _eIconImage;
        [SerializeField] TMP_Text? _eCooldownText;

        [Header("Actual slot roots and key labels")]
        [SerializeField] GameObject? _qSlotRoot;
        [SerializeField] GameObject? _eSlotRoot;
        [SerializeField] TMP_Text? _qKeyText;
        [SerializeField] TMP_Text? _eKeyText;

        [Header("스킬 아이콘 매핑 에셋")]
        [SerializeField] SkillIconSet? _iconSet;

        // Presentation cache only; the connection owner still owns class authority.
        CharacterClass? _renderedClass;
        bool _hasResolvedClass;
        SkillId _qSkill;
        SkillId _eSkill;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SkillHudController] 중복 인스턴스 감지 — 신규 인스턴스 파괴.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            ClassLoadout.AuthorityChanged -= ResolveSkillSlots;
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            ResolveSkillSlots();
        }

        void OnEnable()
        {
            ClassLoadout.AuthorityChanged -= ResolveSkillSlots;
            ClassLoadout.AuthorityChanged += ResolveSkillSlots;
            ResolveSkillSlots();
        }

        void OnDisable()
        {
            ClassLoadout.AuthorityChanged -= ResolveSkillSlots;
            ApplySlots(SkillId.None, SkillId.None);
            _hasResolvedClass = false;
        }

        static CharacterClass? CurrentClass()
        {
            var session = UnityClientSession.Instance;
            if (session != null && (session.IsClosed || !session.CharacterLoad.IsSuccess)) return null;
            return ClassLoadout.ServerClass;
        }

        // Only confirmed authority selects skills; menu intent never fills a slot.
        void ResolveSkillSlots()
        {
            _renderedClass = CurrentClass();
            _hasResolvedClass = true;
            if (_renderedClass.HasValue &&
                ClientSkillCatalog.SkillKeyMap.TryGetValue(_renderedClass.Value, out (SkillId q, SkillId e) qe))
            {
                ApplySlots(qe.q, qe.e);
            }
            else
            {
                // 미등록 클래스 방어 — 슬롯 전부 None.
                ApplySlots(SkillId.None, SkillId.None);
            }
        }

        void ApplySlots(SkillId q, SkillId e)
        {
            _qSkill = q;
            _eSkill = e;
            ApplyIcon(_qIconImage, q);
            ApplyIcon(_eIconImage, e);
            SetSlotVisible(_qSlotRoot, _qCooldownImage, _qCooldownText, _qKeyText, q != SkillId.None, "Q");
            SetSlotVisible(_eSlotRoot, _eCooldownImage, _eCooldownText, _eKeyText, e != SkillId.None, "E");
        }

        static void SetSlotVisible(GameObject? root, Image? cooldown, TMP_Text? timer, TMP_Text? key,
            bool visible, string keyName)
        {
            if (root != null) root.SetActive(visible);
            if (cooldown != null) cooldown.gameObject.SetActive(visible);
            if (timer != null) timer.gameObject.SetActive(visible);
            if (key != null)
            {
                key.text = keyName;
                key.gameObject.SetActive(visible);
            }
        }

        // SkillIconSet에서 sprite를 꺼내 icon Image에 적용. 매핑 없으면 Image 숨김.
        void ApplyIcon(Image? iconImage, SkillId skill)
        {
            if (iconImage == null) return;
            Sprite? s = _iconSet != null ? _iconSet.GetIcon(skill) : null;
            iconImage.sprite = s;
            iconImage.enabled = s != null;
        }

        void Update()
        {
            // The owner's reset has no event, and transport close precedes its main-thread cleanup.
            if (!_hasResolvedClass || _renderedClass != CurrentClass()) ResolveSkillSlots();
            LocalPlayerMovement? movement = LocalPlayerMovement.Instance;
            if (movement == null) return;

            UpdateSlot(_qCooldownImage, _qCooldownText, movement, _qSkill);
            UpdateSlot(_eCooldownImage, _eCooldownText, movement, _eSkill);
        }

        // fill 방향 = remaining/total (쿨다운 중 fill이 가득 찼다 0으로 비워지는 "잠금 표시" 컨벤션).
        // 준비도(아이콘이 채워지는 방향)로 표시하려면 `1f - remaining / total`로 flip — 영호 외관 의도에 맞춰.
        // remaining > total 방어: Clamp01이 1f로 수렴.
        public static float ComputeFill(float remaining, float total) =>
            total > 0f ? Mathf.Clamp01(remaining / total) : 0f;

        // 슬롯 1개 갱신.
        static void UpdateSlot(Image? image, TMP_Text? text, LocalPlayerMovement movement, SkillId skill)
        {
            if (image == null) return;
            if (skill == SkillId.None) return;

            (float remaining, float total) = movement.GetCooldown(skill);

            image.fillAmount = ComputeFill(remaining, total);

            if (text != null)
            {
                text.text = remaining > 0f ? $"{remaining:F1}" : string.Empty;
            }
        }
    }
}
