using System.Collections.Generic;
using NUnit.Framework;
using Shared.Protocol;

namespace Dawnholder.Client.Tests
{
    // Blacksmith window of goal 2026-10-15-guild-client-ui PR1 (contract-v1 req 3, 4 and 8; goal C1), written before the
    // product. Real path: the product's BlacksmithWindow.BuildRuntime and Unity's message order (ViewLifecycle), values
    // only through the product's GuildDisplayModel. Stand-in: this test supplies the personal state the server will
    // send; each value is an explicit input from the approved World values (BlacksmithInput). Nothing is sent here.
    public sealed class GuildBlacksmithWindowTests
    {
        GuildModelDriver _model;
        GuildWindowDriver _window;

        [SetUp]
        public void Setup()
        {
            _model = new GuildModelDriver();
            _window = new GuildWindowDriver(GuildWindowDriver.Blacksmith);
            _window.Bind(_model);
            _window.SetOpen(true);
            _window.SetInputAllowed(true);
        }

        [TearDown]
        public void Teardown() => _window?.Dispose();

        // req3/C1: before the first synchronization the open window shows no number, and enhance sends nothing.
        [Test]
        public void BeforeSynchronization_ShowsNoNumber_AndEnhanceSendsNothing()
        {
            bool drawn = _window.View().Shown;
            string level = _window.Text("EnhanceLevel");
            string gold = _window.Text("GoldCost");
            string[] shownWithDigits = _window.ShownTextsWithDigits();
            _window.PressAction();

            Assert.IsTrue(drawn, "req5 precondition: the open window is drawn");
            Assert.IsEmpty(GuildWindowDriver.NumbersIn(level), $"req3 unsynchronized level shows no number: '{level}'");
            Assert.IsEmpty(GuildWindowDriver.NumbersIn(gold), $"req3 unsynchronized gold cost shows no number: '{gold}'");
            Assert.IsEmpty(shownWithDigits, "req3 unsynchronized: no shown text has a number");
            Assert.AreEqual(0, _window.Intents, "req4 unsynchronized: pressing enhance sends no intent");
        }

        // req3: the enhancement level with its maximum, the attack bonus and the next gold and Material costs are shown
        // as given.
        [Test]
        public void ShowsTheGivenLevelBonusAndNextCosts()
        {
            BlacksmithInput ready = BlacksmithInput.Ready();
            _model.SetBlacksmith(ready);
            long[] level = _window.Numbers("EnhanceLevel");
            long[] bonus = _window.Numbers("AttackBonus");
            long[] gold = _window.Numbers("GoldCost");
            long[] material = _window.Numbers("MaterialCost");

            CollectionAssert.IsSubsetOf(new[] { (long)ready.EnhanceLevel, ready.MaxEnhanceLevel }, level,
                "req3 enhancement level and its maximum");
            CollectionAssert.Contains(bonus, (long)ready.AttackBonus, "req3 attack bonus");
            CollectionAssert.Contains(gold, ready.GoldCost, "req3 next gold cost");
            CollectionAssert.Contains(material, (long)ready.MaterialCost, "req3 next Material cost");
        }

        // req3/req8: enhancement 0 -> 1 is shown with bonus +5 once the model supplies it; at the maximum there is no
        // next cost to show.
        [Test]
        public void EnhancedState_ShowsTheNewLevelAndBonus_AndNoNextCost()
        {
            _model.SetBlacksmith(BlacksmithInput.Enhanced());
            long[] level = _window.Numbers("EnhanceLevel");
            long[] bonus = _window.Numbers("AttackBonus");
            string gold = _window.Text("GoldCost");
            string material = _window.Text("MaterialCost");

            CollectionAssert.Contains(level, 1L, "req8 enhancement 1 is shown");
            CollectionAssert.Contains(bonus, 5L, "req3 attack bonus +5 is shown");
            Assert.IsEmpty(GuildWindowDriver.NumbersIn(gold), $"req3 the maximum shows no next gold cost: '{gold}'");
            Assert.IsEmpty(GuildWindowDriver.NumbersIn(material), $"req3 the maximum shows no next Material cost: '{material}'");
        }

        // req3: the current class is shown, and another class reads differently.
        [Test]
        public void ShowsTheCurrentClass()
        {
            _model.SetBlacksmith(BlacksmithInput.Ready());
            string knight = _window.Text("CharacterClass");
            _model.SetBlacksmith(BlacksmithInput.Ready().With(smith => smith.CharacterClass = CharacterClass.Mage));
            string mage = _window.Text("CharacterClass");

            Assert.IsNotEmpty(knight.Trim(), "req3 the current class is shown");
            Assert.AreNotEqual(knight, mage, "req3 another class reads differently");
        }

        // req3/C1: available, locked, short, maximum, in progress, refused and completed read differently.
        [Test]
        public void EnhanceStates_ReadDistinctly()
        {
            var states = new Dictionary<string, BlacksmithInput>
            {
                ["available"] = BlacksmithInput.Ready(),
                ["locked"] = BlacksmithInput.Locked(),
                ["short"] = BlacksmithInput.Short(),
                ["in progress"] = BlacksmithInput.Ready().With(smith => smith.EnhancePending = true),
                ["refused"] = BlacksmithInput.Ready().With(smith => smith.EnhanceResult = "Refused"),
                ["maximum"] = BlacksmithInput.Enhanced().With(smith => smith.EnhanceResult = "None"),
                ["completed"] = BlacksmithInput.Enhanced(),
            };
            var texts = new Dictionary<string, string>();
            foreach (KeyValuePair<string, BlacksmithInput> state in states)
            {
                _model.SetBlacksmith(state.Value);
                texts[state.Key] = _window.StateText();
            }
            string[] sameText = GuildWindowTests.SameTexts(texts);

            Assert.IsEmpty(sameText, "req3/C1 each enhance state reads differently");
        }

        // req4: one press of an allowed enhance sends exactly one intent and changes no number; the level and bonus
        // change only when the model supplies the confirmed state.
        [Test]
        public void AllowedEnhance_SendsOneIntent_AndOnlyTheSuppliedResultChangesTheNumbers()
        {
            _model.SetBlacksmith(BlacksmithInput.Ready());
            string levelBefore = _window.Text("EnhanceLevel");
            string bonusBefore = _window.Text("AttackBonus");
            _window.PressAction();
            int intentsAfterPress = _window.Intents;
            string levelAfterPress = _window.Text("EnhanceLevel");
            string bonusAfterPress = _window.Text("AttackBonus");
            _model.SetBlacksmith(BlacksmithInput.Enhanced());
            long[] bonusAfterResult = _window.Numbers("AttackBonus");

            Assert.AreEqual(1, intentsAfterPress, "req4 one press sends exactly one enhance intent");
            Assert.AreEqual(levelBefore, levelAfterPress, "req4 the press itself raises no level");
            Assert.AreEqual(bonusBefore, bonusAfterPress, "req4 the press itself adds no bonus");
            CollectionAssert.Contains(bonusAfterResult, 5L, "req4 the supplied result shows the new bonus");
            Assert.AreEqual(1, _window.Intents, "req4 the supplied result sends no further intent");
        }

        // req4: unsynchronized, locked, short, maximum, in progress, closed, input not allowed and a released source
        // send nothing even when the button's onClick runs directly. The allowed press above is the control.
        [TestCase("unsynchronized")]
        [TestCase("locked")]
        [TestCase("short")]
        [TestCase("maximum")]
        [TestCase("in progress")]
        [TestCase("closed")]
        [TestCase("input not allowed")]
        [TestCase("released source")]
        public void Enhance_SendsNothing_WhenNotAllowed(string state)
        {
            switch (state)
            {
                case "unsynchronized":
                    break;
                case "locked":
                    _model.SetBlacksmith(BlacksmithInput.Locked());
                    break;
                case "short":
                    _model.SetBlacksmith(BlacksmithInput.Short());
                    break;
                case "maximum":
                    _model.SetBlacksmith(BlacksmithInput.Enhanced());
                    break;
                case "in progress":
                    _model.SetBlacksmith(BlacksmithInput.Ready().With(smith => smith.EnhancePending = true));
                    break;
                case "closed":
                    _model.SetBlacksmith(BlacksmithInput.Ready());
                    _window.SetOpen(false);
                    break;
                case "input not allowed":
                    _model.SetBlacksmith(BlacksmithInput.Ready());
                    _window.SetInputAllowed(false);
                    break;
                case "released source":
                    _model.SetBlacksmith(BlacksmithInput.Ready());
                    _window.Bind(null);
                    break;
                default:
                    Assert.Fail("fixture: unknown state " + state);
                    break;
            }
            _window.PressAction();

            Assert.AreEqual(0, _window.Intents, $"req4 {state}: pressing enhance sends no intent");
        }
    }
}
