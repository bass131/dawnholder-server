#nullable enable
using System;
using Shared.GameData;
using UnityEngine;

namespace Dawnholder.Client.UI
{
    /// <summary>Scene-owned, nonblocking presentation of confirmed guild growth results. Call on the Unity main thread.</summary>
    [DisallowMultipleComponent]
    public sealed class GuildGrowthEffectView : MonoBehaviour
    {
        GuildGrowthEffectPresentation? _presentation;
        bool _destroyed;

        public static GuildGrowthEffectView BuildRuntime(Transform parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            var root = new GameObject("GuildGrowthEffect", typeof(RectTransform));
            root.SetActive(false);
            root.transform.SetParent(parent, worldPositionStays: false);
            var view = root.AddComponent<GuildGrowthEffectView>();
            try
            {
                view._presentation = GuildGrowthEffectPresentation.Build(root);
                root.SetActive(true);
                return view;
            }
            catch
            {
                // A partially built presentation still belongs only to this caller's new root.
                if (Application.isPlaying) Destroy(root);
                else DestroyImmediate(root);
                throw;
            }
        }

        /// <summary>
        /// Fit both whole effects and their labels inside a normalized rectangle of this view's canvas; null restores
        /// the original layout. Accept finite, positive rectangles entirely inside 0..1. Invalid input returns false
        /// without changing the layout or running effects. Text readability is not guaranteed in extremely small areas.
        /// </summary>
        public bool TrySetAvailableArea(Rect? normalizedAvailableArea)
        {
            if (_destroyed || _presentation == null) return false;
            if (normalizedAvailableArea.HasValue)
            {
                Rect area = normalizedAvailableArea.Value;
                if (!IsFinite(area.x) || !IsFinite(area.y) || !IsFinite(area.width) || !IsFinite(area.height)
                    || area.width <= 0f || area.height <= 0f || area.x < 0f || area.y < 0f
                    || area.x >= 1f || area.y >= 1f
                    || (double)area.x + area.width > 1d || (double)area.y + area.height > 1d) return false;
            }

            _presentation.SetAvailableArea(normalizedAvailableArea);
            return true;
        }

        /// <summary>
        /// Accept only a confirmed result level, never a pending intent or a mutable State reference.
        /// The caller owns event deduplication, session/Town boundaries and higher-priority window cancellation.
        /// </summary>
        public bool TryPlayConfirmed(GuildChangeKind kind, int confirmedLevel)
        {
            if (_destroyed || !isActiveAndEnabled || _presentation == null || confirmedLevel <= 0) return false;
            if (kind != GuildChangeKind.StrongholdUpgrade && kind != GuildChangeKind.WeaponEnhance) return false;

            _presentation.Play(kind, confirmedLevel, Time.realtimeSinceStartupAsDouble);
            return true;
        }

        /// <summary>Clear both presentations immediately. Reactivation accepts new requests without replaying old ones.</summary>
        public void CancelAll() => _presentation?.CancelAll();

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        void Update() => _presentation?.Tick(Time.realtimeSinceStartupAsDouble);

        void OnDisable() => CancelAll();

        void OnDestroy()
        {
            _destroyed = true;
            _presentation?.Dispose();
            _presentation = null;
        }
    }
}
