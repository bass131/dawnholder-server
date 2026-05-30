using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// HUD 표시 핸들러. HP 슬라이더 + 자원 텍스트(HP/Gold) 표시.
    ///
    /// **헌법 #1 (Server Authority)**: HUD는 *서버가 알려준* 값만 표시합니다.
    /// 현재는 mock(클라 자체값)이지만, 패킷 수신 핸들러가
    /// <see cref="UpdateHP"/> / <see cref="UpdateGold"/>를 호출해 데이터 갱신합니다.
    /// 데미지/획득 *계산* 로직은 절대 이 클래스에 들어오지 않습니다.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [Header("HP")]
        [FormerlySerializedAs("hpSlider")]
        [SerializeField] Slider _hpSlider;
        [FormerlySerializedAs("hpText")]
        [SerializeField] TMP_Text _hpText;

        [Header("Resources")]
        [FormerlySerializedAs("goldText")]
        [SerializeField] TMP_Text _goldText;

        [Header("Mock Initial Values (Phase 03)")]
        [FormerlySerializedAs("mockHpCurrent")]
        [SerializeField] int _mockHpCurrent = 100;
        [FormerlySerializedAs("mockHpMax")]
        [SerializeField] int _mockHpMax = 100;
        [FormerlySerializedAs("mockGold")]
        [SerializeField] int _mockGold = 0;

        void Start()
        {
            UpdateHP(_mockHpCurrent, _mockHpMax);
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

        public void UpdateGold(int amount)
        {
            if (_goldText != null)
            {
                _goldText.text = $"Gold: {amount}";
            }
        }
    }
}
