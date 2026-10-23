using TMPro;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// 현재 연결의 확정 HP·직업과 인벤토리 재화를 표시한다. 배치는 HudLayout이 소유한다.
    ///
    /// **헌법 #1 (Server Authority)**: HUD는 *서버가 알려준* 값만 표시합니다.
    /// Entry owner가 <see cref="ApplyServerHP"/>로 마지막 서버 HP를 표시합니다.
    /// 데미지/획득 *계산* 로직은 절대 이 클래스에 들어오지 않습니다.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        public static HudController? Instance { get; private set; }
        bool _hasServerHp;
        UnityClientSession _boundSession;
        long _boundEpoch;
        InventoryState _inventorySource;

        [Header("HP")]
        [FormerlySerializedAs("hpSlider")]
        [SerializeField] Slider _hpSlider;
        [FormerlySerializedAs("hpText")]
        [SerializeField] TMP_Text _hpText;

        [Header("Confirmed class")]
        [SerializeField] TMP_Text _classText;
        [SerializeField] Image _knightPortrait;
        [SerializeField] Image _magePortrait;

        [Header("MP")]
        [SerializeField] Slider _mpSlider;

        [Header("Resources")]
        [FormerlySerializedAs("goldText")]
        [SerializeField] TMP_Text _goldText;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[HudController] 중복 인스턴스 감지 — 신규 인스턴스 파괴.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            UnbindInventory();
            UnbindHp();
            if (Instance == this) Instance = null;
        }

        void OnDisable()
        {
            UnbindInventory();
            UnbindHp();
        }

        void OnEnable()
        {
            ShowUnconfirmedHp();
            RenderClass();
            TryBindLatestEntry();
            BindInventory();
            RenderGold();
        }

        void Start()
        {
            // An HP callback may arrive before Start; never replace it with a class default.
            if (!_hasServerHp) ShowUnconfirmedHp();
            RenderClass();
            BindInventory();
            RenderGold();
            if (UnityClientSession.Instance != null) TryBindLatestEntry();
        }

        void Update()
        {
            TryBindLatestEntry();
            RenderClass();
            BindInventory();
            if (UnityClientSession.Instance != null && UnityClientSession.Instance.IsClosed) RenderGold();
        }

        void BindInventory()
        {
            var source = InventoryState.Instance;
            if (ReferenceEquals(_inventorySource, source)) return;
            UnbindInventory();
            _inventorySource = source;
            if (source != null) source.OnInventoryChanged += RenderGold;
            RenderGold();
        }

        void UnbindInventory()
        {
            // A destroyed Unity object can still own the managed event we subscribed to.
            if (!ReferenceEquals(_inventorySource, null)) _inventorySource.OnInventoryChanged -= RenderGold;
            _inventorySource = null;
        }

        void RenderGold()
        {
            if (_inventorySource != null && _inventorySource.HasSnapshot &&
                (UnityClientSession.Instance == null || !UnityClientSession.Instance.IsClosed))
                UpdateGold(_inventorySource.Currency);
            else if (_goldText != null)
            {
                // Use an ASCII hyphen supported by the HUD's static font atlas.
                _goldText.text = "Gold: -";
            }
        }

        void TryBindLatestEntry()
        {
            var session = UnityClientSession.Instance;
            if (ReferenceEquals(_boundSession, session) && session != null &&
                _boundEpoch == session.Entry.Epoch && session.IsCurrentEntry(_boundEpoch)) return;
            UnbindHp();
            ShowUnconfirmedHp();
            if (session != null && !session.IsClosed && session.TryBindHud(this))
            {
                _boundSession = session;
                _boundEpoch = session.Entry.Epoch;
            }
        }

        void UnbindHp()
        {
            _boundSession?.UnbindHud(this);
            _boundSession = null;
            _boundEpoch = 0;
        }

        void ShowUnconfirmedHp()
        {
            _hasServerHp = false;
            if (_hpSlider != null) _hpSlider.value = 0f;
            if (_hpText != null) _hpText.text = "HP - / -";
        }

        void RenderClass()
        {
            // ResetServerClass has no event. Read the current authority, including the close→cleanup gap.
            var session = UnityClientSession.Instance;
            CharacterClass? confirmed = ClassLoadout.ServerClass;
            if (session != null && (session.IsClosed || !session.CharacterLoad.IsSuccess)) confirmed = null;
            if (_classText != null)
                _classText.text = confirmed == CharacterClass.Knight ? "Knight" :
                    confirmed == CharacterClass.Mage ? "Mage" : "-";
            if (_knightPortrait != null) _knightPortrait.gameObject.SetActive(confirmed == CharacterClass.Knight);
            if (_magePortrait != null) _magePortrait.gameObject.SetActive(confirmed == CharacterClass.Mage);
        }

        public void ApplyServerHP(int current, int max)
        {
            _hasServerHp = true;
            UpdateHP(current, max);
        }

        public void UpdateHP(int current, int max)
        {
            if (max <= 0) max = 1;
            current = Mathf.Clamp(current, 0, max);

            if (_hpSlider != null)
            {
                _hpSlider.value = (float)current / max;
            }
            if (_hpText != null)
            {
                _hpText.text = $"HP {current} / {max}";
            }
        }

        public void UpdateMP(int current, int max)
        {
            if (max <= 0) max = 1;
            current = Mathf.Clamp(current, 0, max);

            if (_mpSlider != null)
            {
                _mpSlider.value = (float)current / max;
            }
        }

        public void UpdateGold(int amount)
        {
            if (_goldText != null)
            {
                _goldText.text = $"Gold: {amount}";
            }
        }
    }
}
