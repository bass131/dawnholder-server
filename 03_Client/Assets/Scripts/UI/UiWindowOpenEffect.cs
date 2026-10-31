#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// Plays <see cref="UiWindowOpenMotion"/> on a panel the owner places every frame. Only drawing changes: the panel's
    /// content fades in through a group of its own while a frame the effect draws grows into the panel's border. The
    /// effect never writes the panel's or any other existing transform, nor the owner's visibility and input decisions,
    /// and closing is immediate.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class UiWindowOpenEffect : MonoBehaviour
    {
        RectTransform _target = null!;
        Image _background = null!;
        CanvasGroup _fade = null!;
        RectTransform? _frame;
        float _startTime;
        bool _playing;

        internal static UiWindowOpenEffect Attach(RectTransform target)
        {
            var effect = target.gameObject.AddComponent<UiWindowOpenEffect>();
            effect._target = target;
            effect._background = target.GetComponent<Image>();
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
            if (_fade != null) _fade.alpha = 1f;
            RemoveFrame();
        }

        void LateUpdate()
        {
            if (!_playing)
            {
                enabled = false;
                return;
            }
            float elapsed = Time.unscaledTime - _startTime;
            if (UiWindowOpenMotion.IsFinished(elapsed))
            {
                Stop();
                return;
            }
            if (_frame == null) _frame = CreateFrame();
            float scale = UiWindowOpenMotion.ScaleAt(elapsed);
            _frame.localScale = new Vector3(scale, scale, 1f);
            _fade.alpha = UiWindowOpenMotion.AlphaAt(elapsed);
        }

        // The growing frame is the panel's own border piece without its fill, drawn over the fading content as the
        // panel's last child, so no existing child changes order or path. At full size it lies on the real border.
        RectTransform CreateFrame()
        {
            var frame = new GameObject("OpeningFrame", typeof(RectTransform));
            frame.transform.SetParent(_target, false);
            var rect = (RectTransform)frame.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            // Grows from the window's centre whatever pivot the owner placed the panel with.
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            // The panel's fade would hide the frame with the content; only the frame ignores it. Hiding always stops
            // the effect first, so the frame never outlives an open window.
            var group = frame.AddComponent<CanvasGroup>();
            group.ignoreParentGroups = true;
            group.interactable = false;
            group.blocksRaycasts = false;
            var image = frame.AddComponent<Image>();
            image.sprite = _background.sprite;
            image.type = _background.type;
            image.pixelsPerUnitMultiplier = _background.pixelsPerUnitMultiplier;
            image.color = _background.color;
            image.fillCenter = false;
            image.raycastTarget = false;
            return rect;
        }

        void RemoveFrame()
        {
            if (_frame == null) return;
            // Edit-mode tests drive the effect by hand, where Destroy is not allowed.
            if (Application.isPlaying) Destroy(_frame.gameObject);
            else DestroyImmediate(_frame.gameObject);
            _frame = null;
        }
    }
}
