#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>Guild-only scenery and information bands inside the existing half-screen window.</summary>
    internal sealed class GuildHousingLayout
    {
        readonly Image _foreground;
        readonly Sprite? _levelOne;
        readonly Sprite? _levelTwo;

        internal GuildHousingLayout(RectTransform panel)
        {
            _levelOne = Resources.Load<Sprite>("UI/GuildHousing/GuildBaseLevel1");
            _levelTwo = Resources.Load<Sprite>("UI/GuildHousing/GuildBaseLevel2");
            Place(UiWindowAppearance.CreateInset("GuildInfoBand", panel).rectTransform,
                new Vector2(416f, 84f), new Vector2(12f, -52f));
            var scenery = new GameObject("HousingForeground", typeof(RectTransform));
            scenery.transform.SetParent(panel, worldPositionStays: false);
            _foreground = scenery.AddComponent<Image>();
            _foreground.type = Image.Type.Simple;
            _foreground.preserveAspect = true;
            _foreground.raycastTarget = false;
            _foreground.enabled = false;
            RectTransform sceneryRect = _foreground.rectTransform;
            sceneryRect.anchorMin = sceneryRect.anchorMax = new Vector2(0f, 1f);
            sceneryRect.pivot = new Vector2(0.5f, 0.5f);
            sceneryRect.sizeDelta = new Vector2(416f, 214f);
            sceneryRect.anchoredPosition = new Vector2(220f, -247f);
            // Only the foreground path sits behind this band; buildings and their flags stay above it.
            Place(UiWindowAppearance.CreateHeader("TreasuryBand", panel).rectTransform,
                new Vector2(416f, 32f), new Vector2(12f, -322f));
        }

        internal static void PlaceActions(Button levelUp, Button refresh)
        {
            PlaceButton(levelUp, new Vector2(238f, 42f), new Vector2(16f, -510f));
            PlaceButton(refresh, new Vector2(158f, 42f), new Vector2(266f, -510f));
        }

        internal void SetBaseLevel(int? baseLevel)
        {
            // The projection owns confirmation. Binding/query/reconnect select a still image, never an effect.
            Sprite? sprite = baseLevel == 1 ? _levelOne : baseLevel == 2 ? _levelTwo : null;
            _foreground.sprite = sprite;
            _foreground.enabled = sprite != null;
        }

        static void PlaceButton(Button button, Vector2 size, Vector2 position)
        {
            Place((RectTransform)button.transform, size, position);
            RectTransform label = button.GetComponentInChildren<TMP_Text>().rectTransform;
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.offsetMin = label.offsetMax = Vector2.zero;
        }

        static void Place(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
