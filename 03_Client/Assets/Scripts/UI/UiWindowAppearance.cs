#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>Applies shared artwork. Callers retain geometry, text, callbacks and window lifetime.</summary>
    internal static class UiWindowAppearance
    {
        internal enum ButtonStyle { Default, Accept, Decline }

        static UiWindowSkin? s_skin;
        static UiWindowSkin? s_brightSkin;

        static UiWindowSkin Skin => Load(ref s_skin, "UI/Components/UiWindowSkin");

        /// <summary>The bright kit used only by the inventory and character select; other windows keep the default skin.</summary>
        internal static UiWindowSkin BrightSkin => Load(ref s_brightSkin, "UI/BrightComponents/UiWindowSkin");

        static UiWindowSkin Load(ref UiWindowSkin? cache, string path)
        {
            if (cache == null)
                cache = Resources.Load<UiWindowSkin>(path);
            if (cache == null)
                throw new InvalidOperationException($"Missing {path}.");
            return cache;
        }

        internal static Image CreatePanel(string name, Transform parent, bool dialog = false, bool raycastTarget = false,
            UiWindowSkin? skin = null)
        {
            UiWindowSkin source = skin ?? Skin;
            Image image = CreateImage(source.PanelPrefab, name, parent);
            if (dialog) image.sprite = source.DialogPanel;
            image.raycastTarget = raycastTarget;
            return image;
        }

        internal static Image CreateEmphasisPanel(string name, Transform parent)
        {
            Image image = CreatePanel(name, parent);
            image.sprite = Skin.EmphasisPanel;
            return image;
        }

        // Scene-owned roots keep their component, RectTransform and raycast ownership.
        internal static void ApplyPanel(Image image)
        {
            Image template = Skin.PanelPrefab;
            image.sprite = template.sprite;
            image.overrideSprite = null;
            image.type = template.type;
            image.preserveAspect = template.preserveAspect;
            image.fillCenter = template.fillCenter;
            image.pixelsPerUnitMultiplier = template.pixelsPerUnitMultiplier;
            image.color = Color.white;
        }

        internal static Image CreateInset(string name, Transform parent, UiWindowSkin? skin = null) =>
            CreateImage((skin ?? Skin).InsetPrefab, name, parent);

        internal static Image CreateHeader(string name, Transform parent, UiWindowSkin? skin = null) =>
            CreateImage((skin ?? Skin).HeaderPrefab, name, parent);

        internal static Button CreateButton(string name, Transform parent, ButtonStyle style = ButtonStyle.Default,
            UiWindowSkin? skin = null)
        {
            UiWindowSkin source = skin ?? Skin;
            Button button = UnityEngine.Object.Instantiate(source.ButtonPrefab, parent, false);
            button.name = name;
            var image = (Image)button.targetGraphic;
            if (style == ButtonStyle.Accept)
            {
                image.sprite = source.AcceptNormal;
                button.spriteState = source.AcceptStates;
            }
            else if (style == ButtonStyle.Decline)
            {
                image.sprite = source.DeclineNormal;
                button.spriteState = source.DeclineStates;
            }
            return button;
        }

        internal static void AddInsetBehind(RectTransform content)
        {
            Image inset = CreateInset("MemberInset", content.parent);
            RectTransform rect = inset.rectTransform;
            rect.anchorMin = content.anchorMin;
            rect.anchorMax = content.anchorMax;
            rect.pivot = content.pivot;
            rect.sizeDelta = content.sizeDelta;
            rect.anchoredPosition = content.anchoredPosition;
            rect.localRotation = content.localRotation;
            rect.localScale = content.localScale;
            rect.SetSiblingIndex(content.GetSiblingIndex());
        }

        internal static void AddPortraitFrame(RectTransform portrait)
        {
            // The frame follows the content object's existing no-portrait active state.
            Image frame = CreateImage(Skin.PortraitPrefab, "PortraitFrame", portrait);
            RectTransform rect = frame.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static Image CreateDivider(string name, Transform parent, UiWindowSkin? skin = null) =>
            CreateSliced(name, parent, (skin ?? Skin).Divider);

        /// <summary>A 9-slice piece drawn at the kit's density, white and outside the raycast.</summary>
        internal static Image CreateSliced(string name, Transform parent, Sprite sprite)
        {
            Image image = CreatePlainImage(name, parent, sprite);
            image.type = Image.Type.Sliced;
            // Sprite PPU 200 / Canvas reference PPU 100 already gives density 2.
            image.pixelsPerUnitMultiplier = 1f;
            return image;
        }

        /// <summary>A piece drawn whole with its own aspect (icons, pedestal, glow, class art), outside the raycast.</summary>
        internal static Image CreateSimple(string name, Transform parent, Sprite sprite)
        {
            Image image = CreatePlainImage(name, parent, sprite);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            return image;
        }

        static Image CreatePlainImage(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        static Image CreateImage(Image prefab, string name, Transform parent)
        {
            Image image = UnityEngine.Object.Instantiate(prefab, parent, false);
            image.name = name;
            image.raycastTarget = false;
            return image;
        }
    }
}
