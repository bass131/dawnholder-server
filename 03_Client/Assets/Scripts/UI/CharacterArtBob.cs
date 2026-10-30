#nullable enable
using UnityEngine;

namespace Dawnholder.Client.UI
{
    /// <summary>Moves one slot's standing art by <see cref="CharacterArtBobMotion"/> while that slot is chosen.</summary>
    [DisallowMultipleComponent]
    internal sealed class CharacterArtBob : MonoBehaviour
    {
        RectTransform _art = null!;
        Vector2 _rest;
        float _startTime;

        internal static CharacterArtBob Attach(RectTransform art)
        {
            var bob = art.gameObject.AddComponent<CharacterArtBob>();
            bob._art = art;
            bob._rest = art.anchoredPosition;
            bob.enabled = false;
            return bob;
        }

        internal void SetPlaying(bool playing)
        {
            if (playing == enabled) return;
            if (playing)
            {
                // Unscaled, matching the menu's own input which does not depend on the game clock.
                _startTime = Time.unscaledTime;
                enabled = true;
            }
            else
            {
                enabled = false;
                _art.anchoredPosition = _rest;
            }
        }

        void LateUpdate() =>
            _art.anchoredPosition = _rest + new Vector2(0f, CharacterArtBobMotion.OffsetAt(Time.unscaledTime - _startTime));
    }
}
