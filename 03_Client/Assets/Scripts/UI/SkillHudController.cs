#nullable enable
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Input;
using Dawnholder.Client.Prediction;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
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
        [SerializeField] Image? _qSlotImage;
        [SerializeField] TMP_Text? _qCooldownText;

        [Header("E 슬롯 (Mage: Teleport / Knight: 미사용 — 숨김)")]
        [SerializeField] Image? _eSlotImage;
        [SerializeField] TMP_Text? _eCooldownText;

        // 세션 내 고정 — 클래스는 로그인 후 바뀌지 않으므로 Start에서 1회 결정.
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
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            ResolveSkillSlots();
        }

        // 선택 클래스 → Q/E 스킬 결정. LocalPlayerInput.SkillKeyMap이 단일 진실.
        void ResolveSkillSlots()
        {
            CharacterClass myClass = ClassLoadout.SessionSelectedClass
                ?? (CharacterClass)ClassLoadout.GetSelectedClassValue((int)CharacterClass.Knight);

            if (LocalPlayerInput.SkillKeyMap.TryGetValue(myClass, out (SkillId q, SkillId e) qe))
            {
                _qSkill = qe.q;
                _eSkill = qe.e;
            }
            else
            {
                // 미등록 클래스 방어 — 슬롯 전부 None.
                _qSkill = SkillId.None;
                _eSkill = SkillId.None;
            }

            // None 슬롯(Knight E)은 즉시 숨김 — GameObject 비활성으로 레이아웃에서도 제거.
            if (_eSkill == SkillId.None)
            {
                if (_eSlotImage != null) _eSlotImage.gameObject.SetActive(false);
                if (_eCooldownText != null) _eCooldownText.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            LocalPlayerMovement? movement = LocalPlayerMovement.Instance;
            if (movement == null) return;

            UpdateSlot(_qSlotImage, _qCooldownText, movement, _qSkill);
            UpdateSlot(_eSlotImage, _eCooldownText, movement, _eSkill);
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
