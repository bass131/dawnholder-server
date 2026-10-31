#nullable enable
using UnityEngine;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// Scale of the growing frame and opacity of the content while a window opens, as a function of the time since
    /// opening. <see cref="UiWindowOpenEffect"/> draws them; the window's own placement never takes this scale.
    /// </summary>
    internal static class UiWindowOpenMotion
    {
        internal const float Duration = 0.12f;
        internal const float StartScale = 0.92f;

        internal static bool IsFinished(float elapsed) => elapsed >= Duration;

        internal static float ScaleAt(float elapsed) => Mathf.LerpUnclamped(StartScale, 1f, EaseOut(Progress(elapsed)));

        internal static float AlphaAt(float elapsed) => Progress(elapsed);

        static float Progress(float elapsed) => Mathf.Clamp01(elapsed / Duration);

        // Fast at first, settling into the final size instead of stopping abruptly.
        static float EaseOut(float progress) => 1f - (1f - progress) * (1f - progress);
    }
}
