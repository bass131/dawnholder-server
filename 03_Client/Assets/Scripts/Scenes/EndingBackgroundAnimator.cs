#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.Scenes
{
    /// <summary>
    /// 엔딩 배경 프레임 사이클러 — 스프라이트 시트 프레임을 UI Image에 루핑 재생.
    ///
    /// EndingController가 런타임에 붙이고 <see cref="Play"/>로 구동(씬 편집 불필요).
    /// 프레임 수 무관 — 로드된 개수만큼 순환. 프레임이 없으면 no-op(정적 배경 유지).
    ///
    /// 헌법 #1: 순수 연출, 서버 상태 무관. Time.deltaTime 기반.
    /// </summary>
    [DisallowMultipleComponent]
    public class EndingBackgroundAnimator : MonoBehaviour
    {
        Image?    _target;
        Sprite[]? _frames;
        float     _fps = 10f;
        float     _clock;

        public void Play(Image target, Sprite[] frames, float fps)
        {
            _target = target;
            _frames = frames;
            _fps    = Mathf.Max(1f, fps);
            _clock  = 0f;
            if (_target != null && _frames != null && _frames.Length > 0)
                _target.sprite = _frames[0];
        }

        void Update()
        {
            if (_target == null || _frames == null || _frames.Length == 0) return;
            _clock += Time.deltaTime;
            _target.sprite = _frames[(int)(_clock * _fps) % _frames.Length];
        }
    }
}
