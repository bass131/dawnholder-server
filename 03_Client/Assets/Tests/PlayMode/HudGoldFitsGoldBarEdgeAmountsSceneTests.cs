using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Dungeon goal R10 beyond the five amounts HudGoldFitsGoldBarSceneTests uses
    // (01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md 「결함 PR의 요구」). The HUD gold font's digits have
    // different advances ('4' widest, '1' narrowest in Pretendard SDF Proper), so 444,444 is wider than 999,999 and
    // the auto size depends on which digits are drawn, not only on how many. Each amount must still be one line,
    // every drawn glyph inside Gold_bar, the panel's digits, size 18 up to six digits and 12 or more from seven.
    // The amounts are picked from the font asset; the expected values are only R10's numbers above.
    public sealed class HudGoldFitsGoldBarEdgeAmountsSceneTests
    {
        const int LongestSizeEighteenDigits = 6;
        const float SizeUpToSixDigits = 18f;
        const float SmallestLongSize = 12f;
        // Float noise only: world units for the ink check, points for the size check.
        const float InkTolerance = 0.01f;
        const float SizeTolerance = 0.01f;

        MapEntryPlayFixture _fixture;

        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();
        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        // 444,444 is the widest six-digit amount and 999,999 → 1,000,000 crosses six to seven digits upward;
        // 1,111,111 is the narrowest seven-digit amount, 4,444,444 the widest, 444,444,444 the widest nine-digit one.
        [UnityTest]
        public IEnumerator RealUiHudGold_WidestAndNarrowestAmountsPerDigitCount_KeepR10()
        {
            yield return ObserveAndAssert(444_444, 999_999, 1_000_000, 1_111_111, 4_444_444, 444_444_444);
        }

        // The server cap draws the smallest size; the amounts after it go back down, so a short amount must not keep
        // a long amount's smaller size.
        [UnityTest]
        public IEnumerator RealUiHudGold_AfterTheServerCap_ShorterAmountsKeepR10()
        {
            Assert.AreEqual(1_000_000_000, InventoryLimits.MaxCurrency,
                "fixture: the first amount below is the server cap");
            yield return ObserveAndAssert(1_000_000_000, 444_444, 4_444_444, 0);
        }

        // Enters the scripted Town, sends each amount as the next server snapshot, records how the product's
        // HudController drew it and checks every R10 condition for every amount, listing each amount that breaks one.
        IEnumerator ObserveAndAssert(params int[] amounts)
        {
            yield return _fixture.EnterScriptedTown();
            yield return MapEntryPlayFixture.Wait(() => HudController.Instance != null && HudController.Instance.isActiveAndEnabled,
                "additive HUD", 10);
            TMP_Text gold = MapEntryPlayFixture.Field<TMP_Text>(HudController.Instance, "_goldText");
            Assert.IsNotNull(gold, "fixture: the UI scene's HUD has its gold label");
            RectTransform goldBar = GoldBar(gold);
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 1,
                "C_InventoryRequest #1", 3);

            var layouts = new List<GoldLayout>();
            uint revision = 0;
            foreach (int amount in amounts)
            {
                string serverDigits = amount.ToString(CultureInfo.InvariantCulture);
                _fixture.Peer.Send(new S_InventorySnapshot { revision = ++revision, currency = amount }.Write());
                yield return MapEntryPlayFixture.Wait(() => Digits(InventoryPanelProbe.Currency) == serverDigits,
                    "panel shows " + serverDigits, 3);
                yield return _fixture.Frames(2);
                GoldLayout layout = Measure(gold, goldBar, amount, InventoryPanelProbe.Currency);
                Debug.Log("[R10 HUD gold] " + layout);
                layouts.Add(layout);
            }

            string table = string.Join("\n", layouts);
            Assert.IsEmpty(Amounts(layouts, layout => layout.DrawnDigits != layout.ServerDigits ||
                Digits(layout.PanelText) != layout.ServerDigits),
                "R10 the HUD draws every digit of the server amount, the same digits as the panel\n" + table);
            Assert.IsEmpty(Amounts(layouts, layout => layout.Lines != 1), "R10 the HUD gold is one line\n" + table);
            Assert.IsEmpty(Amounts(layouts, layout => layout.Glyphs == 0 || layout.GlyphsOutsideBar != 0),
                "R10 every drawn glyph lies inside Gold_bar (no drawn glyph counts as a failure)\n" + table);
            Assert.IsEmpty(Amounts(layouts, layout => layout.ServerDigits.Length <= LongestSizeEighteenDigits &&
                (Mathf.Abs(layout.MinSize - SizeUpToSixDigits) > SizeTolerance ||
                Mathf.Abs(layout.MaxSize - SizeUpToSixDigits) > SizeTolerance)),
                "R10 up to six digits every glyph is drawn at size 18\n" + table);
            Assert.IsEmpty(Amounts(layouts, layout => layout.ServerDigits.Length > LongestSizeEighteenDigits &&
                layout.MinSize < SmallestLongSize - SizeTolerance),
                "R10 from seven digits every glyph is drawn at size 12 or more\n" + table);
        }

        // The label as drawn now, read the same way as HudGoldFitsGoldBarSceneTests.Measure: ForceMeshUpdate lays it out
        // with its current settings (auto size included), ink is each visible glyph's quad, and the size is pointSize on
        // the root canvas scale. isTextOverflowing and textBounds are not used (goal 「R10의 알려진 함정」).
        static GoldLayout Measure(TMP_Text gold, RectTransform goldBar, int amount, string panelText)
        {
            gold.ForceMeshUpdate();
            TMP_TextInfo info = gold.textInfo;
            var barCorners = new Vector3[4];
            goldBar.GetWorldCorners(barCorners);
            Canvas canvas = gold.GetComponentInParent<Canvas>();
            Assert.IsNotNull(canvas, "fixture: the HUD gold label is on a canvas");
            float scaleToCanvas = gold.transform.lossyScale.y / canvas.rootCanvas.transform.lossyScale.y;
            var layout = new GoldLayout
            {
                Amount = amount,
                Text = gold.text,
                PanelText = panelText,
                Lines = info.lineCount,
                BarMin = barCorners[0],
                BarMax = barCorners[2],
                Settings = $"fontSize {gold.fontSize:F2} autoSize {gold.enableAutoSizing} min {gold.fontSizeMin:F2} " +
                    $"max {gold.fontSizeMax:F2} wrap {gold.textWrappingMode} rect {((RectTransform)gold.transform).rect.size:F2}",
            };
            var drawn = new StringBuilder();
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible) continue;
                Vector2 inkMin = gold.transform.TransformPoint(character.bottomLeft);
                Vector2 inkMax = gold.transform.TransformPoint(character.topRight);
                float size = character.pointSize * scaleToCanvas;
                drawn.Append(character.character);
                layout.Glyphs++;
                layout.InkMin = Vector2.Min(layout.InkMin, inkMin);
                layout.InkMax = Vector2.Max(layout.InkMax, inkMax);
                layout.MinSize = Mathf.Min(layout.MinSize, size);
                layout.MaxSize = Mathf.Max(layout.MaxSize, size);
                if (inkMin.x < layout.BarMin.x - InkTolerance || inkMin.y < layout.BarMin.y - InkTolerance ||
                    inkMax.x > layout.BarMax.x + InkTolerance || inkMax.y > layout.BarMax.y + InkTolerance)
                {
                    layout.GlyphsOutsideBar++;
                }
            }
            layout.DrawnDigits = Digits(drawn.ToString());
            return layout;
        }

        static int[] Amounts(List<GoldLayout> layouts, Func<GoldLayout, bool> breaks) =>
            layouts.Where(breaks).Select(layout => layout.Amount).ToArray();

        static string Digits(string text) => new string((text ?? string.Empty).Where(char.IsDigit).ToArray());

        // The gold bar R10 names: the nearest ancestor called Gold_bar.
        static RectTransform GoldBar(TMP_Text gold)
        {
            for (Transform node = gold.transform.parent; node != null; node = node.parent)
            {
                if (node.name == "Gold_bar") return (RectTransform)node;
            }
            Assert.Fail("fixture: the HUD gold label sits inside Gold_bar");
            return null;
        }

        // One amount as the HUD drew it. ToString is the observation line for the log and the failure messages.
        sealed class GoldLayout
        {
            public int Amount { get; set; }
            public string Text { get; set; }
            public string PanelText { get; set; }
            public string DrawnDigits { get; set; } = string.Empty;
            public string Settings { get; set; }
            public int Lines { get; set; }
            public int Glyphs { get; set; }
            public int GlyphsOutsideBar { get; set; }
            public float MinSize { get; set; } = float.PositiveInfinity;
            public float MaxSize { get; set; } = float.NegativeInfinity;
            public Vector2 InkMin { get; set; } = Vector2.positiveInfinity;
            public Vector2 InkMax { get; set; } = Vector2.negativeInfinity;
            public Vector2 BarMin { get; set; }
            public Vector2 BarMax { get; set; }
            public string ServerDigits => Amount.ToString(CultureInfo.InvariantCulture);

            public override string ToString() =>
                $"amount {Amount} text '{Text}' panel '{PanelText}' drawnDigits {DrawnDigits} lines {Lines} glyphs {Glyphs} " +
                $"outsideBar {GlyphsOutsideBar} size {MinSize:F2}..{MaxSize:F2} ink {InkMin:F2}-{InkMax:F2} " +
                $"bar {BarMin:F2}-{BarMax:F2} | {Settings}";
        }
    }
}
