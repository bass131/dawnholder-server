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

        static UiWindowSkin Skin
        {
            get
            {
                if (s_skin == null)
                    s_skin = Resources.Load<UiWindowSkin>("UI/Components/UiWindowSkin");
                if (s_skin == null)
                    throw new InvalidOperationException("Missing UI/Components/UiWindowSkin.");
                return s_skin;
            }
        }

        internal static Image CreatePanel(string name, Transform parent, bool dialog = false, bool raycastTarget = false)
        {
            Image image = CreateImage(Skin.PanelPrefab, name, parent);
            if (dialog) image.sprite = Skin.DialogPanel;
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

        internal static Image CreateInset(string name, Transform parent) => CreateImage(Skin.InsetPrefab, name, parent);

        internal static Image CreateHeader(string name, Transform parent) => CreateImage(Skin.HeaderPrefab, name, parent);

        internal static Button CreateButton(string name, Transform parent, ButtonStyle style = ButtonStyle.Default)
        {
            Button button = UnityEngine.Object.Instantiate(Skin.ButtonPrefab, parent, false);
            button.name = name;
            var image = (Image)button.targetGraphic;
            if (style == ButtonStyle.Accept)
            {
                image.sprite = Skin.AcceptNormal;
                button.spriteState = Skin.AcceptStates;
            }
            else if (style == ButtonStyle.Decline)
            {
                image.sprite = Skin.DeclineNormal;
                button.spriteState = Skin.DeclineStates;
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

        internal static Image CreateDivider(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = Skin.Divider;
            image.type = Image.Type.Sliced;
            // Sprite PPU 200 / Canvas reference PPU 100 already gives density 2.
            image.pixelsPerUnitMultiplier = 1f;
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
