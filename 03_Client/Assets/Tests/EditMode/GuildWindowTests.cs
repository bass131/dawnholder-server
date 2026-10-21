using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Dawnholder.Client.Tests
{
    // Guild status window of goal 2026-10-15-guild-client-ui PR1 (contract-v1 req 2, 4 and 8; goal C1), written before
    // the product. Real path: the product's GuildWindow.BuildRuntime and Unity's message order (ViewLifecycle), values
    // only through the product's GuildDisplayModel. Stand-in: this test supplies the values as the later server state
    // will; each one is an explicit input from the approved World values (GuildInput). Nothing is sent or computed here.
    public sealed class GuildWindowTests
    {
        GuildModelDriver _model;
        GuildWindowDriver _window;

        [SetUp]
        public void Setup()
        {
            _model = new GuildModelDriver();
            _window = new GuildWindowDriver(GuildWindowDriver.Guild);
            _window.Bind(_model);
            _window.SetOpen(true);
            _window.SetInputAllowed(true);
        }

        [TearDown]
        public void Teardown() => _window?.Dispose();

        // req2/C1: before the first synchronization the open window shows no number at all, so no 0, sample or
        // serialized default reads as the guild's state, and level-up sends nothing.
        [Test]
        public void BeforeSynchronization_ShowsNoNumber_AndLevelUpSendsNothing()
        {
            bool drawn = _window.View().Shown;
            string treasury = _window.Text("Treasury");
            string members = _window.Text("MemberCount");
            string[] shownWithDigits = _window.ShownTextsWithDigits();
            _window.PressAction();

            Assert.IsTrue(drawn, "req5 precondition: the open window is drawn");
            Assert.IsEmpty(GuildWindowDriver.NumbersIn(treasury), $"req2 unsynchronized treasury shows no number: '{treasury}'");
            Assert.IsEmpty(GuildWindowDriver.NumbersIn(members), $"req2 unsynchronized members show no number: '{members}'");
            Assert.IsEmpty(shownWithDigits, "req2 unsynchronized: no shown text has a number");
            Assert.AreEqual(0, _window.Intents, "req4 unsynchronized: pressing level-up sends no intent");
        }

        // req2/req8: a synchronized treasury of 0 (after the purchase) is shown as 0, unlike the unsynchronized window;
        // at the maximum level there is no next price to show.
        [Test]
        public void SynchronizedZeroTreasury_IsShownAsZero_AndTheMaximumLevelShowsNoNextPrice()
        {
            string[] unsynchronized = _window.ShownTextsWithDigits();
            _model.SetGuild(GuildInput.Purchased());
            long[] treasury = _window.Numbers("Treasury");
            long[] level = _window.Numbers("BaseLevel");
            string nextPrice = _window.Text("LevelUpCost");

            Assert.IsEmpty(unsynchronized, "req2 precondition: nothing numeric before synchronization");
            CollectionAssert.Contains(treasury, 0L, "req2 the synchronized treasury 0 is shown as 0");
            CollectionAssert.Contains(level, 2L, "req2 the base level 2 is shown");
            Assert.IsEmpty(GuildWindowDriver.NumbersIn(nextPrice), $"req2 the maximum level shows no next price: '{nextPrice}'");
        }

        // req2: the server's guild name, member and online counts, treasury, base level with its maximum and the next
        // price are shown as given, with the auto-join notice.
        [Test]
        public void ShowsTheGivenNameCountsTreasuryLevelAndNextPrice_WithTheAutoJoinNotice()
        {
            GuildInput entered = GuildInput.Entered();
            _model.SetGuild(entered);
            string name = _window.Text("GuildName");
            long[] members = _window.Numbers("MemberCount");
            long[] online = _window.Numbers("OnlineCount");
            long[] treasury = _window.Numbers("Treasury");
            long[] level = _window.Numbers("BaseLevel");
            long[] nextPrice = _window.Numbers("LevelUpCost");
            bool noticeShown = _window.IsLabelShown("AutoJoinNotice");
            string notice = _window.Text("AutoJoinNotice");

            StringAssert.Contains(entered.GuildName, name, "req2 the server's guild name");
            CollectionAssert.Contains(members, (long)entered.MemberCount, "req2 member count");
            CollectionAssert.Contains(online, (long)entered.OnlineCount, "req2 online count");
            CollectionAssert.Contains(treasury, entered.Treasury, "req2 treasury");
            CollectionAssert.IsSubsetOf(new[] { (long)entered.BaseLevel, entered.MaxBaseLevel }, level,
                "req2 base level and its maximum");
            CollectionAssert.Contains(nextPrice, entered.LevelUpCost, "req2 the next level-up price");
            Assert.IsTrue(noticeShown, "req2 the auto-join notice is shown");
            StringAssert.Contains("자동", notice, "req2 the notice explains automatic joining");
        }

        // req8: two members with an online count that drops from 2 to 1; only the new state supplied by the model
        // changes the shown count.
        [Test]
        public void OnlineCountChange_IsShownOnlyAfterTheModelSuppliesIt()
        {
            _model.SetGuild(GuildInput.Entered());
            long[] onlineBefore = _window.Numbers("OnlineCount");
            _window.Frame();
            long[] onlineAfterAFrame = _window.Numbers("OnlineCount");
            _model.SetGuild(GuildInput.Entered().With(guild => guild.OnlineCount = 1));
            long[] onlineAfter = _window.Numbers("OnlineCount");
            long[] membersAfter = _window.Numbers("MemberCount");

            CollectionAssert.Contains(onlineBefore, 2L, "req8 two online at first");
            CollectionAssert.AreEqual(onlineBefore, onlineAfterAFrame, "req4 nothing changes the count by itself");
            CollectionAssert.Contains(onlineAfter, 1L, "req8 the new online count 1 is shown");
            CollectionAssert.DoesNotContain(onlineAfter, 2L, "req8 the old online count is gone");
            CollectionAssert.Contains(membersAfter, 2L, "req8 the member count stays 2");
        }

        // req2: the locked and the unlocked blacksmith read differently with every number the same.
        [Test]
        public void LockedAndUnlockedBlacksmith_ReadDifferently()
        {
            _model.SetGuild(GuildInput.Entered());
            string locked = _window.StateText();
            _model.SetGuild(GuildInput.Entered().With(guild => guild.BlacksmithUnlocked = true));
            string unlocked = _window.StateText();

            Assert.AreNotEqual(locked, unlocked, "req2 the blacksmith's lock is shown");
        }

        // req2/C1: available, short, maximum level, in progress, refused and completed read differently. Each pair
        // differs only in its state (or in the numbers the state implies), and the comparison leaves out the named value
        // labels, so the difference is in the window's state text.
        [Test]
        public void LevelUpStates_ReadDistinctly()
        {
            var states = new Dictionary<string, GuildInput>
            {
                ["available"] = GuildInput.Funded(),
                ["short"] = GuildInput.Entered(),
                ["in progress"] = GuildInput.Funded().With(guild => guild.LevelUpPending = true),
                ["refused"] = GuildInput.Funded().With(guild => guild.LevelUpResult = "Refused"),
                ["maximum level"] = GuildInput.Purchased().With(guild => guild.LevelUpResult = "None"),
                ["completed"] = GuildInput.Purchased(),
            };
            var texts = new Dictionary<string, string>();
            foreach (KeyValuePair<string, GuildInput> state in states)
            {
                _model.SetGuild(state.Value);
                texts[state.Key] = _window.StateText();
            }
            string[] sameText = SameTexts(texts);

            Assert.IsEmpty(sameText, "req2/C1 each level-up state reads differently");
        }

        // req4: one press of an allowed level-up sends exactly one intent and changes no number; the treasury and level
        // change only when the model supplies the confirmed state.
        [Test]
        public void AllowedLevelUp_SendsOneIntent_AndOnlyTheSuppliedResultChangesTheNumbers()
        {
            _model.SetGuild(GuildInput.Funded());
            string treasuryBefore = _window.Text("Treasury");
            string levelBefore = _window.Text("BaseLevel");
            _window.PressAction();
            int intentsAfterPress = _window.Intents;
            string treasuryAfterPress = _window.Text("Treasury");
            string levelAfterPress = _window.Text("BaseLevel");
            _model.SetGuild(GuildInput.Purchased());
            long[] treasuryAfterResult = _window.Numbers("Treasury");
            string levelAfterResult = _window.Text("BaseLevel");

            Assert.AreEqual(1, intentsAfterPress, "req4 one press sends exactly one level-up intent");
            Assert.AreEqual(treasuryBefore, treasuryAfterPress, "req4 the press itself spends no treasury");
            Assert.AreEqual(levelBefore, levelAfterPress, "req4 the press itself raises no level");
            CollectionAssert.Contains(treasuryAfterResult, 0L, "req4 the supplied result shows the new treasury");
            Assert.AreNotEqual(levelBefore, levelAfterResult, "req4 the supplied result shows the new level");
            Assert.AreEqual(1, _window.Intents, "req4 the supplied result sends no further intent");
        }

        // req4: unsynchronized, short, maximum level, in progress, closed, input not allowed and a released source send
        // nothing even when the button's onClick runs directly. The allowed press above is the control.
        [TestCase("unsynchronized")]
        [TestCase("short")]
        [TestCase("maximum level")]
        [TestCase("in progress")]
        [TestCase("closed")]
        [TestCase("input not allowed")]
        [TestCase("released source")]
        public void LevelUp_SendsNothing_WhenNotAllowed(string state)
        {
            switch (state)
            {
                case "unsynchronized":
                    break;
                case "short":
                    _model.SetGuild(GuildInput.Entered());
                    break;
                case "maximum level":
                    _model.SetGuild(GuildInput.Purchased());
                    break;
                case "in progress":
                    _model.SetGuild(GuildInput.Funded().With(guild => guild.LevelUpPending = true));
                    break;
                case "closed":
                    _model.SetGuild(GuildInput.Funded());
                    _window.SetOpen(false);
                    break;
                case "input not allowed":
                    _model.SetGuild(GuildInput.Funded());
                    _window.SetInputAllowed(false);
                    break;
                case "released source":
                    _model.SetGuild(GuildInput.Funded());
                    _window.Bind(null);
                    break;
                default:
                    Assert.Fail("fixture: unknown state " + state);
                    break;
            }
            _window.PressAction();

            Assert.AreEqual(0, _window.Intents, $"req4 {state}: pressing level-up sends no intent");
        }

        // req2: automatic joining has no join button.
        [Test]
        public void HasNoJoinButton()
        {
            _model.SetGuild(GuildInput.Entered());
            string[] joinButtons = _window.ButtonCaptions().Where(caption => caption.Contains("가입")).ToArray();

            Assert.IsEmpty(joinButtons, "req2 automatic joining offers no join button");
        }

        // State names whose text equals an earlier state's text.
        internal static string[] SameTexts(Dictionary<string, string> texts) => texts
            .GroupBy(pair => pair.Value)
            .Where(group => group.Count() > 1)
            .Select(group => string.Join(" = ", group.Select(pair => pair.Key)) + $": '{group.Key}'")
            .ToArray();
    }
}
