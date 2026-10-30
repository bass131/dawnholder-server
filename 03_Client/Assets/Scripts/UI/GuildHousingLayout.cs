#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// The landscape guild window's content: the base picture across the top, the information rows in two columns
    /// under it and the action buttons on the last line. Distances are UI units from the panel's top-left corner.
    /// </summary>
    internal sealed class GuildHousingLayout
    {
        // GuildBaseLevel1/2 are 1672x941. The box keeps that aspect, so the preserved-aspect picture fills it.
        const float PictureWidth = 464f;
        const float PictureHeight = PictureWidth * 941f / 1672f;
        const float PictureTop = 41f;
        // Inside the panel frame's side borders, the same inset as the title band, the bands and the buttons.
        const float InnerLeft = 10f;
        const float RowHeight = 20f;
        const float LeftColumn = 18f;
        const float RightColumn = 246f;
        const float ColumnWidth = 222f;
        const float WideWidth = 450f;
        const float ActionTop = 440f;
        // Rows 0-1 stand on the guild band, row 2 on the dark treasury band and rows 3-5 on the inset.
        const int TreasuryRow = 2;
        static readonly float[] s_rowTops = { 307f, 327f, 352f, 376f, 396f, 416f };
        readonly GuildWindowLayout _layout;
        readonly Image _foreground;
        readonly Sprite? _levelOne;
        readonly Sprite? _levelTwo;

        internal GuildHousingLayout(GuildWindowLayout layout, RectTransform panel)
        {
            _layout = layout;
            _levelOne = Resources.Load<Sprite>("UI/GuildHousing/GuildBaseLevel1");
            _levelTwo = Resources.Load<Sprite>("UI/GuildHousing/GuildBaseLevel2");
            var scenery = new GameObject("HousingForeground", typeof(RectTransform));
            scenery.transform.SetParent(panel, worldPositionStays: false);
            _foreground = scenery.AddComponent<Image>();
            _foreground.type = Image.Type.Simple;
            _foreground.preserveAspect = true;
            _foreground.raycastTarget = false;
            _foreground.enabled = false;
            Place(_foreground.rectTransform, new Vector2(PictureWidth, PictureHeight),
                new Vector2(InnerLeft, -PictureTop));
            Place(UiWindowAppearance.CreateInset("GuildInfoBand", panel, layout.Skin).rectTransform,
                new Vector2(PictureWidth, 44f), new Vector2(InnerLeft, -305f));
            Place(UiWindowAppearance.CreateHeader("TreasuryBand", panel, layout.Skin).rectTransform,
                new Vector2(PictureWidth, 22f), new Vector2(InnerLeft, -351f));
            layout.Inset(375f, 62f, InnerLeft, PictureWidth);
        }

        // A value in one of the two columns of a row.
        internal TMP_Text Row(string name, int row, bool right)
        {
            float left = right ? RightColumn : LeftColumn;
            return row == TreasuryRow
                ? _layout.BandLabel(name, s_rowTops[row], RowHeight, left, ColumnWidth)
                : _layout.Label(name, s_rowTops[row], RowHeight, left, ColumnWidth);
        }

        // A value across both columns, for the longest supplied lines.
        internal TMP_Text WideRow(string name, int row) =>
            _layout.Label(name, s_rowTops[row], RowHeight, LeftColumn, WideWidth);

        internal TMP_Text Notice(string name, int row, bool right, string caption) =>
            _layout.Notice(name, s_rowTops[row], RowHeight, caption, right ? RightColumn : LeftColumn, ColumnWidth);

        internal static void PlaceActions(Button levelUp, Button refresh)
        {
            PlaceButton(levelUp, new Vector2(230f, 24f), new Vector2(InnerLeft, -ActionTop));
            PlaceButton(refresh, new Vector2(144f, 24f), new Vector2(InnerLeft + PictureWidth - 144f, -ActionTop));
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
