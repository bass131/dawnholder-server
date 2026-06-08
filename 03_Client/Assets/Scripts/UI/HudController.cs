using TMPro;
using Dawnholder.Client.Scenes;
using Shared.GameData;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// HUD 표시 핸들러. HP/MP 슬라이더 + 자원 텍스트(HP/Gold) 표시.
    ///
    /// **헌법 #1 (Server Authority)**: HUD는 *서버가 알려준* 값만 표시합니다.
    /// EnemyAttackHandler가 <see cref="UpdateHP"/>를 호출해 피격 결과를 반영합니다.
    /// 데미지/획득 *계산* 로직은 절대 이 클래스에 들어오지 않습니다.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        public static HudController? Instance { get; private set; }

        [Header("HP")]
        [FormerlySerializedAs("hpSlider")]
        [SerializeField] Slider _hpSlider;
        [FormerlySerializedAs("hpText")]
        [SerializeField] TMP_Text _hpText;

        [Header("MP")]
        [SerializeField] Slider _mpSlider;

        [Header("Resources")]
        [FormerlySerializedAs("goldText")]
        [SerializeField] TMP_Text _goldText;

        [Header("Mock Initial Values (MP/Gold — 서버 채널 미박힘)")]
        [FormerlySerializedAs("mockGold")]
        [SerializeField] int _mockGold = 0;
        [SerializeField] int _mockMpCurrent = 50;
        [SerializeField] int _mockMpMax = 50;

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
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            // HP 초기값 = 선택 직업 full HP — 서버 S_PlayerHp 도착 전 임시 표시 (곧 서버 권위값이 덮음).
            int classValue = PlayerPrefs.GetInt(
                CharacterSelectController.SelectedClassPrefsKey,
                (int)CharacterClass.Knight);
            PlayerStats stats = PlayerStats.ForClass((CharacterClass)classValue);
            UpdateHP(stats.MaxHp, stats.MaxHp);

            UpdateMP(_mockMpCurrent, _mockMpMax);
            UpdateGold(_mockGold);
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
