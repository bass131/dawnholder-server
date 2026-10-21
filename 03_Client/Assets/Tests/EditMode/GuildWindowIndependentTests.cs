using System;
using System.Linq;
using System.Text.RegularExpressions;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Independent verification of goal 2026-10-15-guild-client-ui PR1 (goal C1, api-contract 2-5 and 8), written by
    // the verifier after the product. Real path: the product's BuildRuntime, Bind, SetOpen, SetInputAllowed and each
    // Button's own onClick, with Unity's messages sent in order by ViewLifecycle (EditMode sends none by itself).
    // Stand-in: this test is the supplier and puts every value into the product's GuildDisplayModel; nothing is sent.
    // Expected values come from the supplied inputs and the requirement text, never from the window's own strings or
    // layout. Both windows share one model as the api-contract fixes. Real frames and pointers are in PlayMode.
    public sealed class GuildWindowIndependentTests
    {
        const string Guild = "guild";
        const string Blacksmith = "blacksmith";
        static readonly string[] ValueLabels =
            { "GuildName", "MemberCount", "OnlineCount", "Treasury", "BaseLevel", "LevelUpCost",
              "CharacterClass", "EnhanceLevel", "AttackBonus", "GoldCost", "MaterialCost" };
        // Digits with any grouping separator a culture may use (comma, period, no-break or thin spaces).
        static readonly Regex Number = new Regex("\\d(?:[\\d,.   ]*\\d)?");

        GameObject _root;
        GuildDisplayModel _model;
        GuildWindow _guild;
        BlacksmithWindow _smith;
        int _levelUps;
        int _enhances;

        [SetUp]
        public void Setup()
        {
            _levelUps = 0;
            _enhances = 0;
            _root = new GameObject("GuildIndependentTestRoot");
            _model = new GuildDisplayModel();
            _guild = GuildWindow.BuildRuntime(_root.transform);
            _smith = BlacksmithWindow.BuildRuntime(_root.transform);
            _guild.LevelUpRequested += () => _levelUps++;
            _smith.EnhanceRequested += () => _enhances++;
            ViewLifecycle.Begin(_root);
            Frame();
            _guild.Bind(_model);
            _smith.Bind(_model);
            _guild.SetOpen(true);
            _smith.SetOpen(true);
            _guild.SetInputAllowed(true);
            _smith.SetInputAllowed(true);
            Frame();
        }

        [TearDown]
        public void Teardown()
        {
            if (_root != null) ViewLifecycle.Destroy(_root);
            _root = null;
        }

        // C1/api-contract 2: availability is the supplier's, never inferred from the shown treasury and cost. A treasury
        // that covers the cost but is reported Insufficient sends nothing and reads like any other short state; a
        // treasury below the cost but reported Available sends one intent.
        [Test]
        public void LevelUp_FollowsTheSuppliedAvailability_NotTheShownTreasuryAndCost()
        {
            _model.SetGuild(GuildValue(treasury: 4321, cost: 1200, levelUp: GuildActionAvailability.Insufficient));
            Frame();
            string coveredButShortState = StateText(_guild);
            Press(_guild);
            int intentsWhenCoveredButShort = _levelUps;

            _model.SetGuild(GuildValue(treasury: 10, cost: 1200, levelUp: GuildActionAvailability.Insufficient));
            Frame();
            string uncoveredAndShortState = StateText(_guild);

            _model.SetGuild(GuildValue(treasury: 10, cost: 1200, levelUp: GuildActionAvailability.Available));
            Frame();
            string uncoveredButAvailableState = StateText(_guild);
            Press(_guild);
            int intentsWhenUncoveredButAvailable = _levelUps - intentsWhenCoveredButShort;

            Assert.AreEqual(0, intentsWhenCoveredButShort, "C1 Insufficient from the supplier blocks level-up although the shown treasury covers the cost");
            Assert.AreEqual(uncoveredAndShortState, coveredButShortState, "C1 the state text follows the supplied availability, not the treasury number");
            Assert.AreNotEqual(uncoveredAndShortState, uncoveredButAvailableState, "C1 a different supplied availability reads differently");
            Assert.AreEqual(1, intentsWhenUncoveredButAvailable, "C1 Available from the supplier allows one level-up although the shown treasury is below the cost");
        }

        // api-contract 2/3.4: at the maximum there is no next price, so a cost the supplier left in the value is not
        // shown anywhere as a number and the press sends nothing.
        [TestCase(Guild)]
        [TestCase(Blacksmith)]
        public void MaximumLevel_ShowsNoLeftoverCost_AndSendsNothing(string kind)
        {
            long[] leftoverCosts;
            if (kind == Guild)
            {
                leftoverCosts = new long[] { 1777 };
                _model.SetGuild(GuildValue(level: 5, maxLevel: 5, cost: 1777, levelUp: GuildActionAvailability.MaxLevel));
            }
            else
            {
                leftoverCosts = new long[] { 777, 13 };
                _model.SetBlacksmith(SmithValue(level: 1, maxLevel: 1, bonus: 5, gold: 777, material: 13,
                    enhance: GuildActionAvailability.MaxLevel));
            }
            Frame();
            long[] shownNumbers = ShownNumbers(WindowOf(kind));
            Press(WindowOf(kind));

            CollectionAssert.IsNotEmpty(shownNumbers, "precondition: the synchronized maximum shows its level");
            foreach (long cost in leftoverCosts)
                CollectionAssert.DoesNotContain(shownNumbers, cost, $"C1 the maximum shows no next cost, not even the leftover {cost}");
            Assert.AreEqual(0, IntentsOf(kind), "C1 the maximum sends nothing");
        }

        // C1: a value that goes back to unsynchronized (the supplier's null) hides every number it showed and sends
        // nothing, so a stale state never reads as current.
        [TestCase(Guild)]
        [TestCase(Blacksmith)]
        public void ValueReturningToUnsynchronized_ShowsNoNumberAgain_AndSendsNothing(string kind)
        {
            SupplyAvailable(kind);
            Frame();
            long[] synchronized = ShownNumbers(WindowOf(kind));
            if (kind == Guild) _model.SetGuild(null);
            else _model.SetBlacksmith(null);
            Frame();
            string[] shownWithDigits = ShownTextsWithDigits(WindowOf(kind));
            Press(WindowOf(kind));

            CollectionAssert.IsNotEmpty(synchronized, "precondition: the synchronized value shows numbers");
            Assert.IsEmpty(shownWithDigits, "C1 back to unsynchronized: no shown text has a number");
            Assert.AreEqual(0, IntentsOf(kind), "C1 back to unsynchronized: the press sends nothing");
        }

        // api-contract 5 / verify contract 「중복 클릭/처리 중 차단」: when the supplier marks the request pending inside the
        // intent, as a request owner does, a second press of the same frame sends nothing; once settled one press works.
        [TestCase(Guild)]
        [TestCase(Blacksmith)]
        public void PendingSetByTheSupplierInsideTheIntent_BlocksTheSecondPressOfTheSameFrame(string kind)
        {
            if (kind == Guild)
            {
                _model.SetGuild(GuildValue(levelUp: GuildActionAvailability.Available));
                _guild.LevelUpRequested += () => _model.SetGuild(GuildValue(levelUp: GuildActionAvailability.Available, pending: true));
            }
            else
            {
                _model.SetBlacksmith(SmithValue(enhance: GuildActionAvailability.Available));
                _smith.EnhanceRequested += () => _model.SetBlacksmith(SmithValue(enhance: GuildActionAvailability.Available, pending: true));
            }
            Frame();
            Press(WindowOf(kind), frame: false);
            Press(WindowOf(kind), frame: false);
            int intentsOfTwoPresses = IntentsOf(kind);
            Frame();

            if (kind == Guild) _model.SetGuild(GuildValue(levelUp: GuildActionAvailability.Available, result: GuildActionResult.Refused));
            else _model.SetBlacksmith(SmithValue(enhance: GuildActionAvailability.Available, result: GuildActionResult.Refused));
            Frame();
            Press(WindowOf(kind));
            int intentsAfterSettling = IntentsOf(kind);

            Assert.AreEqual(1, intentsOfTwoPresses, "two presses in one frame send one intent once the supplier marked it pending");
            Assert.AreEqual(2, intentsAfterSettling, "control: the settled value allows the next press");
        }

        // api-contract 1: the two windows share one model, and each button reports only its own intent.
        [Test]
        public void TwoWindowsOnOneModel_EachButtonSendsOnlyItsOwnIntent()
        {
            SupplyAvailable(Guild);
            SupplyAvailable(Blacksmith);
            Frame();
            Press(_guild);
            int levelUpsAfterGuildPress = _levelUps;
            int enhancesAfterGuildPress = _enhances;
            Press(_smith);

            Assert.AreEqual(1, levelUpsAfterGuildPress, "the guild button sends one level-up");
            Assert.AreEqual(0, enhancesAfterGuildPress, "the guild button sends no enhance");
            Assert.AreEqual(1, _enhances, "the blacksmith button sends one enhance");
            Assert.AreEqual(1, _levelUps, "the blacksmith button sends no level-up");
        }

        // api-contract 8: after binding a replacement source, changes of the old source neither show nor decide the
        // press; the replacement's values do.
        [TestCase(Guild)]
        [TestCase(Blacksmith)]
        public void AfterRebinding_TheOldSourceChangesNothing_AndTheReplacementDecides(string kind)
        {
            var replacement = new GuildDisplayModel();
            if (kind == Guild)
            {
                _model.SetGuild(GuildValue(name: "옛 길드", treasury: 111, levelUp: GuildActionAvailability.Available));
                replacement.SetGuild(GuildValue(name: "새 길드", treasury: 222, levelUp: GuildActionAvailability.Insufficient));
                _guild.Bind(replacement);
                Frame();
                _model.SetGuild(GuildValue(name: "옛 길드", treasury: 333, levelUp: GuildActionAvailability.Available));
            }
            else
            {
                _model.SetBlacksmith(SmithValue(gold: 111, enhance: GuildActionAvailability.Available));
                replacement.SetBlacksmith(SmithValue(gold: 222, enhance: GuildActionAvailability.Insufficient));
                _smith.Bind(replacement);
                Frame();
                _model.SetBlacksmith(SmithValue(gold: 333, enhance: GuildActionAvailability.Available));
            }
            Frame();
            Component window = WindowOf(kind);
            long[] shownAfterOldChange = ShownNumbers(window);
            string nameAfterOldChange = kind == Guild ? Label(window, "GuildName").text : string.Empty;
            Press(window);
            int intentsUnderReplacementShort = IntentsOf(kind);

            if (kind == Guild) replacement.SetGuild(GuildValue(name: "새 길드", treasury: 444, levelUp: GuildActionAvailability.Available));
            else replacement.SetBlacksmith(SmithValue(gold: 444, enhance: GuildActionAvailability.Available));
            Frame();
            long[] shownAfterReplacementChange = ShownNumbers(window);
            Press(window);

            CollectionAssert.Contains(shownAfterOldChange, 222L, "api-contract 8 the replacement's value is shown");
            CollectionAssert.DoesNotContain(shownAfterOldChange, 333L, "api-contract 8 the old source's change is not shown");
            if (kind == Guild) StringAssert.Contains("새 길드", nameAfterOldChange, "api-contract 8 the replacement's name is shown");
            Assert.AreEqual(0, intentsUnderReplacementShort, "api-contract 8 the replacement's Insufficient decides, not the old Available");
            CollectionAssert.Contains(shownAfterReplacementChange, 444L, "api-contract 8 the replacement's next value is shown");
            Assert.AreEqual(1, IntentsOf(kind), "api-contract 8 the replacement's Available allows one press");
        }

        // C1/api-contract 2: the largest values the World contract allows (World goal 2026-10-14-guild-server lines 345
        // and 350: a 1-16 UTF-16 code unit name, int counts/treasury/costs/bonus and byte levels) are shown without
        // losing digits.
        [Test]
        public void UpperBoundValues_AreShownWithoutLosingDigits()
        {
            const string sixteenCodeUnits = "가나다라마바사아자차카타파하거너";
            _model.SetGuild(new GuildStatusDisplay(guildName: sixteenCodeUnits, memberCount: int.MaxValue,
                onlineCount: int.MaxValue - 1, treasury: int.MaxValue, baseLevel: byte.MaxValue - 1, maxBaseLevel: byte.MaxValue,
                blacksmithUnlocked: true, levelUpCost: int.MaxValue - 2, levelUp: GuildActionAvailability.Available,
                levelUpPending: false, levelUpResult: GuildActionResult.None));
            _model.SetBlacksmith(new BlacksmithDisplay(characterClass: CharacterClass.Mage, enhanceLevel: byte.MaxValue - 1,
                maxEnhanceLevel: byte.MaxValue, attackBonus: int.MaxValue, goldCost: int.MaxValue - 3, materialCost: int.MaxValue - 4,
                enhance: GuildActionAvailability.Available, enhancePending: false, enhanceResult: GuildActionResult.None));
            Frame();

            Assert.AreEqual(16, sixteenCodeUnits.Length, "fixture: the name has 16 UTF-16 code units");
            StringAssert.Contains(sixteenCodeUnits, Label(_guild, "GuildName").text, "the whole 16-unit name");
            CollectionAssert.Contains(NumbersIn(Label(_guild, "MemberCount").text), (long)int.MaxValue, "member count");
            CollectionAssert.Contains(NumbersIn(Label(_guild, "OnlineCount").text), (long)int.MaxValue - 1, "online count");
            CollectionAssert.Contains(NumbersIn(Label(_guild, "Treasury").text), (long)int.MaxValue, "treasury");
            CollectionAssert.IsSubsetOf(new long[] { byte.MaxValue - 1, byte.MaxValue }, NumbersIn(Label(_guild, "BaseLevel").text), "base level and maximum");
            CollectionAssert.Contains(NumbersIn(Label(_guild, "LevelUpCost").text), (long)int.MaxValue - 2, "next level-up cost");
            CollectionAssert.IsSubsetOf(new long[] { byte.MaxValue - 1, byte.MaxValue }, NumbersIn(Label(_smith, "EnhanceLevel").text), "enhance level and maximum");
            CollectionAssert.Contains(NumbersIn(Label(_smith, "AttackBonus").text), (long)int.MaxValue, "attack bonus");
            CollectionAssert.Contains(NumbersIn(Label(_smith, "GoldCost").text), (long)int.MaxValue - 3, "next gold cost");
            CollectionAssert.Contains(NumbersIn(Label(_smith, "MaterialCost").text), (long)int.MaxValue - 4, "next material cost");
        }

        // api-contract 6: a value supplied while the window is closed is what the reopened window shows, and only the
        // reopened window acts on it.
        [Test]
        public void ValueSuppliedWhileClosed_IsShownOnReopening_AndOnlyThenActs()
        {
            _model.SetGuild(GuildValue(treasury: 500, levelUp: GuildActionAvailability.Available));
            _guild.SetOpen(false);
            Frame();
            _model.SetGuild(GuildValue(treasury: 650, levelUp: GuildActionAvailability.Available));
            Frame();
            Press(_guild);
            int intentsWhileClosed = _levelUps;
            bool drawnWhileClosed = ShownTextsUnder(_guild).Length > 0;
            _guild.SetOpen(true);
            Frame();
            long[] treasuryOnReopening = NumbersIn(Label(_guild, "Treasury").text);
            Press(_guild);

            Assert.IsFalse(drawnWhileClosed, "api-contract 6 the closed window draws nothing");
            Assert.AreEqual(0, intentsWhileClosed, "api-contract 6 the closed window sends nothing");
            CollectionAssert.Contains(treasuryOnReopening, 650L, "api-contract 6 the reopened window shows the value supplied while closed");
            CollectionAssert.DoesNotContain(treasuryOnReopening, 500L, "api-contract 6 the earlier value is gone");
            Assert.AreEqual(1, _levelUps, "api-contract 6 the reopened window acts once");
        }

        static GuildStatusDisplay GuildValue(string name = "등불 연합", long treasury = 4321, long cost = 1200,
            int level = 3, int maxLevel = 5, GuildActionAvailability levelUp = GuildActionAvailability.Available,
            bool pending = false, GuildActionResult result = GuildActionResult.None) =>
            new GuildStatusDisplay(guildName: name, memberCount: 7, onlineCount: 3, treasury: treasury, baseLevel: level,
                maxBaseLevel: maxLevel, blacksmithUnlocked: true, levelUpCost: cost, levelUp: levelUp,
                levelUpPending: pending, levelUpResult: result);

        static BlacksmithDisplay SmithValue(int level = 0, int maxLevel = 1, int bonus = 0, long gold = 60, int material = 2,
            GuildActionAvailability enhance = GuildActionAvailability.Available, bool pending = false,
            GuildActionResult result = GuildActionResult.None) =>
            new BlacksmithDisplay(characterClass: CharacterClass.Knight, enhanceLevel: level, maxEnhanceLevel: maxLevel,
                attackBonus: bonus, goldCost: gold, materialCost: material, enhance: enhance, enhancePending: pending,
                enhanceResult: result);

        void SupplyAvailable(string kind)
        {
            if (kind == Guild) _model.SetGuild(GuildValue(levelUp: GuildActionAvailability.Available));
            else _model.SetBlacksmith(SmithValue(enhance: GuildActionAvailability.Available));
        }

        Component WindowOf(string kind) => kind == Guild ? (Component)_guild : _smith;

        int IntentsOf(string kind) => kind == Guild ? _levelUps : _enhances;

        void Frame()
        {
            if (_root != null) ViewLifecycle.Frame(_root);
        }

        // The button's own onClick, as a click reaches it; frame: false keeps two presses in one frame.
        void Press(Component window, bool frame = true)
        {
            string name = window is GuildWindow ? "LevelUp" : "Enhance";
            Button[] buttons = window.GetComponentsInChildren<Button>(true).Where(button => button.name == name).ToArray();
            Assert.AreEqual(1, buttons.Length, $"fixture: exactly one Button named '{name}' (api-contract 3.1)");
            buttons[0].onClick.Invoke();
            if (frame) Frame();
        }

        static TMP_Text Label(Component window, string name)
        {
            TMP_Text[] found = window.GetComponentsInChildren<TMP_Text>(true).Where(text => text.name == name).ToArray();
            Assert.AreEqual(1, found.Length, $"fixture: exactly one TMP_Text named '{name}' (api-contract 3.1)");
            return found[0];
        }

        // Shown is the rule the PlayMode ScreenUiProbe also uses; GuildWindowDriver owns the EditMode copy.
        static TMP_Text[] ShownTextsUnder(Component window) =>
            window.GetComponentsInChildren<TMP_Text>(true).Where(GuildWindowDriver.IsShown).ToArray();

        static long[] ShownNumbers(Component window) => ShownTextsUnder(window).SelectMany(text => NumbersIn(text.text)).ToArray();

        static string[] ShownTextsWithDigits(Component window) => ShownTextsUnder(window)
            .Where(text => NumbersIn(text.text).Length > 0)
            .Select(text => $"{text.name}='{text.text}'")
            .ToArray();

        // Every shown text except the named value labels, so it changes only with what the window says about the state.
        static string StateText(Component window) => string.Join(" | ", ShownTextsUnder(window)
            .Where(text => !ValueLabels.Contains(text.name) && !string.IsNullOrEmpty(text.text))
            .Select(text => text.name + "=" + text.text)
            .OrderBy(text => text, StringComparer.Ordinal));

        static long[] NumbersIn(string text) => Number.Matches(text ?? string.Empty)
            .Cast<Match>()
            .Select(match => long.Parse(new string(match.Value.Where(char.IsDigit).ToArray()), System.Globalization.CultureInfo.InvariantCulture))
            .ToArray();
    }
}
