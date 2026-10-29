#nullable enable
using UnityEngine;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// Plays <see cref="UiWindowOpenMotion"/> on a panel the owner places every frame. Only the drawing moves: the
    /// owner's placement, visibility and input decisions stay where they are, and closing is immediate.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class UiWindowOpenEffect : MonoBehaviour
    {
        RectTransform _target = null!;
        CanvasGroup _fade = null!;
        float _startTime;
        bool _playing;
        bool _applied;
        Vector3 _placedScale;
        Vector3 _writtenScale;

        internal static UiWindowOpenEffect Attach(RectTransform target)
        {
            var effect = target.gameObject.AddComponent<UiWindowOpenEffect>();
            effect._target = target;
            // A group of its own only fades; the window's root group keeps showing, hiding and input.
            effect._fade = target.gameObject.AddComponent<CanvasGroup>();
            effect.enabled = false;
            return effect;
        }

        internal void Play()
        {
            // Unscaled, so a window opened while the game is paused still finishes opening.
            _startTime = Time.unscaledTime;
            _playing = true;
            enabled = true;
        }

        internal void Stop()
        {
            _playing = false;
            enabled = false;
            _fade.alpha = 1f;
            if (_applied && _target.localScale == _writtenScale) _target.localScale = _placedScale;
            _applied = false;
        }

        // LateUpdate runs after the owner's Update placed the panel, and the owner's next placement replaces this
        // frame's scale, so the effect multiplies the current placement instead of keeping a copy of it.
        void LateUpdate()
        {
            if (!_playing)
            {
                enabled = false;
                return;
            }
            Vector3 placed = _target.localScale;
            // The owner skipped placing this frame (no canvas size yet): start from its last placement, never compound.
            if (_applied && placed == _writtenScale) placed = _placedScale;
            _placedScale = placed;
            float elapsed = Time.unscaledTime - _startTime;
            if (UiWindowOpenMotion.IsFinished(elapsed))
            {
                _target.localScale = placed;
                _applied = false;
                Stop();
                return;
            }
            float scale = UiWindowOpenMotion.ScaleAt(elapsed);
            _writtenScale = new Vector3(placed.x * scale, placed.y * scale, placed.z);
            _target.localScale = _writtenScale;
            _applied = true;
            _fade.alpha = UiWindowOpenMotion.AlphaAt(elapsed);
        }
    }
}
