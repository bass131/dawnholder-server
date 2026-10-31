#nullable enable
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// The landscape guild window's content: the base picture across the top, the information rows in two columns
    /// under it and the action buttons on the last line. Distances are UI units from the panel's top-left corner.
    /// Beside an open blacksmith the same parts take a narrower arrangement (Arrange).
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
        // Beside an open blacksmith the panel is GuildWindowLayout's narrow one at scale 1, so the texts are drawn as
        // large as the blacksmith's and the picture gives way instead (main msg_0ef90a8302b0, Art msg_19ba265fd0eb).
        // The row, title and button heights are the blacksmith's 28, 36 and 40 at its 16:9 scale of 0.749.
        const float BesideRowHeight = 21f;
        const float BesideTitleHeight = 27f;
        const float BesideButtonHeight = 30f;
        const float BesideInnerWidth = GuildWindowLayout.HousingBesideWidth - 2f * InnerLeft;
        const float BesideWideWidth = 316f;
        const float BesideRightColumn = 179f;
        const float BesideColumnWidth = 155f;
        // The picture keeps the height the title band, the rows and the buttons leave, centred under the title band.
        const float BesidePictureTop = 43f;
        const float BesidePictureHeight = 166f;
        const float BesidePictureWidth = BesidePictureHeight * 1672f / 941f;
        const float BesideActionTop = 392f;
        static readonly float[] s_rowTops = { 307f, 327f, 352f, 376f, 396f, 416f };
        // A notice is too long to share a narrow row at that size, so rows 0 and 4 (GuildWindow's two notices) give the
        // value and its notice a full row each, the notice under the value.
        static readonly float[] s_besideRowTops = { 212f, 254f, 279f, 304f, 325f, 367f };
        static readonly bool[] s_besideSplitRows = { true, false, false, false, true, false };
        readonly GuildWindowLayout _layout;
        readonly Image _foreground;
        readonly Sprite? _levelOne;
        readonly Sprite? _levelTwo;
        // Each part's place alone (where it was built) and beside an open blacksmith; Rect x and y are left and top.
        readonly List<(RectTransform Part, Rect Alone, Rect Beside)> _places =
            new List<(RectTransform Part, Rect Alone, Rect Beside)>();
        bool _beside;

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
            RectTransform guildBand = UiWindowAppearance.CreateInset("GuildInfoBand", panel, layout.Skin).rectTransform;
            Place(guildBand, new Vector2(PictureWidth, 44f), new Vector2(InnerLeft, -305f));
            RectTransform treasuryBand =
                UiWindowAppearance.CreateHeader("TreasuryBand", panel, layout.Skin).rectTransform;
            Place(treasuryBand, new Vector2(PictureWidth, 22f), new Vector2(InnerLeft, -351f));
            RectTransform inset = layout.Inset(375f, 62f, InnerLeft, PictureWidth);
            // Beside, the title band is as high as its Close button, like the blacksmith's.
            Track(layout.Header, new Rect(InnerLeft, 10f, BesideInnerWidth, BesideButtonHeight));
            Track(layout.Heading.rectTransform,
                new Rect(22f, 10f + (BesideButtonHeight - BesideTitleHeight) / 2f, 220f, BesideTitleHeight));
            Track(_foreground.rectTransform, new Rect(InnerLeft + (BesideInnerWidth - BesidePictureWidth) / 2f,
                BesidePictureTop, BesidePictureWidth, BesidePictureHeight));
            Track(guildBand, new Rect(InnerLeft, 211f, BesideInnerWidth, 65f));
            Track(treasuryBand, new Rect(InnerLeft, 278f, BesideInnerWidth, 23f));
            Track(inset, new Rect(InnerLeft, 303f, BesideInnerWidth, 86f));
        }

        // A value in one of the two columns of a row; beside, the whole row when its row also holds a notice.
        internal TMP_Text Row(string name, int row, bool right)
        {
            float left = right ? RightColumn : LeftColumn;
            TMP_Text text = row == TreasuryRow
                ? _layout.BandLabel(name, s_rowTops[row], RowHeight, left, ColumnWidth)
                : _layout.Label(name, s_rowTops[row], RowHeight, left, ColumnWidth);
            float besideTop = s_besideRowTops[row];
            Track(text.rectTransform, s_besideSplitRows[row] ? BesideWide(besideTop)
                : new Rect(right ? BesideRightColumn : LeftColumn, besideTop, BesideColumnWidth, BesideRowHeight));
            return text;
        }

        // A value across both columns, for the longest supplied lines.
        internal TMP_Text WideRow(string name, int row)
        {
            TMP_Text text = _layout.Label(name, s_rowTops[row], RowHeight, LeftColumn, WideWidth);
            Track(text.rectTransform, BesideWide(s_besideRowTops[row]));
            return text;
        }

        internal TMP_Text Notice(string name, int row, bool right, string caption)
        {
            TMP_Text text = _layout.Notice(name, s_rowTops[row], RowHeight, caption, right ? RightColumn : LeftColumn,
                ColumnWidth);
            Track(text.rectTransform, BesideWide(s_besideRowTops[row] + BesideRowHeight));
            return text;
        }

        // Close stays where GuildWindowLayout.CloseButton put it alone; beside, it grows with its title band.
        internal void PlaceButtons(Button levelUp, Button refresh, Button close)
        {
            PlaceButton(levelUp, new Vector2(230f, 24f), new Vector2(InnerLeft, -ActionTop));
            PlaceButton(refresh, new Vector2(144f, 24f), new Vector2(InnerLeft + PictureWidth - 144f, -ActionTop));
            FillWithCaption(close);
            Track((RectTransform)levelUp.transform, new Rect(InnerLeft, BesideActionTop, 160f, BesideButtonHeight));
            Track((RectTransform)refresh.transform,
                new Rect(InnerLeft + BesideInnerWidth - 110f, BesideActionTop, 110f, BesideButtonHeight));
            Track((RectTransform)close.transform,
                new Rect(InnerLeft + BesideInnerWidth - 76f, 10f, 70f, BesideButtonHeight));
        }

        // GuildWindow passes the flag it gives GuildWindowLayout.Resize, every LateUpdate.
        internal void Arrange(bool besideOpenWindow)
        {
            if (besideOpenWindow == _beside) return;
            _beside = besideOpenWindow;
            foreach ((RectTransform part, Rect alone, Rect beside) in _places)
            {
                Rect place = _beside ? beside : alone;
                Place(part, place.size, new Vector2(place.x, -place.y));
            }
        }

        internal void SetBaseLevel(int? baseLevel)
        {
            // The projection owns confirmation. Binding/query/reconnect select a still image, never an effect.
            Sprite? sprite = baseLevel == 1 ? _levelOne : baseLevel == 2 ? _levelTwo : null;
            _foreground.sprite = sprite;
            _foreground.enabled = sprite != null;
        }

        // Records where rect stands now, alone, with where it goes beside an open blacksmith.
        void Track(RectTransform rect, Rect beside) => _places.Add((rect,
            new Rect(rect.anchoredPosition.x, -rect.anchoredPosition.y, rect.sizeDelta.x, rect.sizeDelta.y), beside));

        static Rect BesideWide(float top) => new Rect(LeftColumn, top, BesideWideWidth, BesideRowHeight);

        static void PlaceButton(Button button, Vector2 size, Vector2 position)
        {
            Place((RectTransform)button.transform, size, position);
            FillWithCaption(button);
        }

        // The caption follows its button's size in either arrangement.
        static void FillWithCaption(Button button)
        {
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
