using System;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Window lifetime of goal 2026-10-15-guild-client-ui PR1 (contract-v1 req 5 and 8; goal C8 for the window's own
    // part), for both new windows. Real path: the product's BuildRuntime, Bind, SetOpen and SetInputAllowed with Unity's
    // message order (ViewLifecycle) including OnDisable/OnDestroy. "Shown", "takes raycasts" and "takes clicks" read the
    // live CanvasGroup, Canvas, GraphicRaycaster and button state as InventoryPanelToggleTests does. The real pointer
    // path and EventSystem raycast are GuildWindowSceneTests (PlayMode).
    public sealed class GuildWindowLifecycleTests
    {
        // req5: a built window starts closed: not drawn, no raycast, no click, and its button sends nothing.
        [TestCase(GuildWindowDriver.Guild)]
        [TestCase(GuildWindowDriver.Blacksmith)]
        public void BuiltWindow_StartsClosed_AndTakesNoRaycastsOrClicks(string kind)
        {
            using var window = new GuildWindowDriver(kind);
            var model = new GuildModelDriver();
            SupplyAllowed(model, kind);
            window.Bind(model);
            bool open = window.IsOpen;
            GuildWindowDriver.WindowView view = window.View();
            window.PressAction();

            Assert.IsFalse(open, "req5 a built window starts closed");
            Assert.IsFalse(view.Shown, "req5 the closed window is not drawn");
            Assert.IsFalse(view.TakesRaycasts, "req5 the closed window takes no raycast, so pointer attacks pass");
            Assert.IsFalse(view.TakesClicks, "req5 the closed window takes no click");
            Assert.AreEqual(0, window.Intents, "req4 the closed window's button sends nothing");
        }

        // req5: opening draws the window, but its button acts only once the owner allows input; closing stops both.
        [TestCase(GuildWindowDriver.Guild)]
        [TestCase(GuildWindowDriver.Blacksmith)]
        public void Opening_DrawsTheWindow_InputAllowedTakesClicks_AndClosingStopsBoth(string kind)
        {
            using var window = new GuildWindowDriver(kind);
            var model = new GuildModelDriver();
            SupplyAllowed(model, kind);
            window.Bind(model);
            window.SetOpen(true);
            GuildWindowDriver.WindowView openedWithoutInput = window.View();
            window.PressAction();
            int intentsWithoutInput = window.Intents;
            window.SetInputAllowed(true);
            bool open = window.IsOpen;
            GuildWindowDriver.WindowView opened = window.View();
            window.SetOpen(false);
            bool openAfterClose = window.IsOpen;
            GuildWindowDriver.WindowView closed = window.View();

            Assert.IsTrue(openedWithoutInput.Shown, "req5 opening draws the window");
            Assert.AreEqual(0, intentsWithoutInput, "req4 a built window allows no input until its owner does");
            Assert.IsTrue(open, "req5 the window reports it is open");
            Assert.IsTrue(opened.Shown, "req5 the open window is drawn: " + opened.Describe());
            Assert.IsTrue(opened.TakesRaycasts, "req5 the open window's button takes raycasts: " + opened.Describe());
            Assert.IsTrue(opened.TakesClicks, "req5 the open window's button takes clicks: " + opened.Describe());
            Assert.IsFalse(openAfterClose, "req5 the window reports it is closed");
            Assert.IsFalse(closed.Shown, "req5 closing hides the window: " + closed.Describe());
            Assert.IsFalse(closed.TakesRaycasts, "req5 closing stops raycasts: " + closed.Describe());
            Assert.IsFalse(closed.TakesClicks, "req5 closing stops clicks: " + closed.Describe());
        }

        // req5: the open window takes raycasts only on its controls, so its background is no attack-blocking board; the
        // button is a uGUI raycast target under the window's own GraphicRaycaster, which GameplayPointerInput can see.
        [TestCase(GuildWindowDriver.Guild)]
        [TestCase(GuildWindowDriver.Blacksmith)]
        public void OpenWindow_TakesRaycastsOnlyOnItsControls(string kind)
        {
            using var window = new GuildWindowDriver(kind);
            var model = new GuildModelDriver();
            SupplyAllowed(model, kind);
            window.Bind(model);
            window.SetOpen(true);
            window.SetInputAllowed(true);
            string[] outsideControls = window.RaycastTargetsOutsideControls();
            Graphic buttonGraphic = window.ActionButton.targetGraphic;
            bool buttonIsTarget = buttonGraphic != null && buttonGraphic.raycastTarget;
            GraphicRaycaster raycaster = buttonGraphic != null && buttonGraphic.canvas != null
                ? buttonGraphic.canvas.rootCanvas.GetComponent<GraphicRaycaster>()
                : null;

            Assert.IsEmpty(outsideControls, "req5 only controls take raycasts; background and texts let input through");
            Assert.IsTrue(buttonIsTarget, "req5 the action button is a uGUI raycast target");
            Assert.IsNotNull(raycaster, "req5 the window's canvas has a GraphicRaycaster for the pointer gate");
        }

        // req5/req8: Bind subscribes once and draws the source at once; binding the same source again adds nothing;
        // another source replaces the first and releases it; Bind(null) releases the last one and the window goes back to
        // unsynchronized, where its button sends nothing although the replaced source allowed the action.
        [TestCase(GuildWindowDriver.Guild)]
        [TestCase(GuildWindowDriver.Blacksmith)]
        public void Bind_SubscribesOnce_ReplacesAndReleasesTheSource(string kind)
        {
            using var window = new GuildWindowDriver(kind);
            var first = new GuildModelDriver();
            var second = new GuildModelDriver();
            SupplyAllowed(first, kind);
            SupplyAllowedVariant(second, kind);
            window.SetOpen(true);
            window.SetInputAllowed(true);
            window.Bind(first);
            int firstAfterBind = first.Subscribers;
            window.Bind(first);
            int firstAfterRebind = first.Subscribers;
            string fromFirst = window.Text(VariantLabel(kind));
            window.Bind(second);
            int firstAfterReplace = first.Subscribers;
            int secondAfterReplace = second.Subscribers;
            string fromSecond = window.Text(VariantLabel(kind));
            window.PressAction();
            int intentsWhileBound = window.Intents;
            window.Bind(null);
            int secondAfterRelease = second.Subscribers;
            string[] numbersAfterRelease = window.ShownTextsWithDigits();
            window.PressAction();
            int intentsAfterRelease = window.Intents;

            Assert.AreEqual(1, firstAfterBind, "req5 Bind subscribes once");
            Assert.AreEqual(1, firstAfterRebind, "req5 binding the same source again adds no subscription");
            Assert.AreEqual(0, firstAfterReplace, "req8 another source releases the first");
            Assert.AreEqual(1, secondAfterReplace, "req8 the new source is subscribed once");
            Assert.AreNotEqual(fromFirst, fromSecond, "req8 the window shows the new source at once");
            Assert.AreEqual(1, intentsWhileBound, "control: the bound allowed state sends one intent");
            Assert.AreEqual(0, secondAfterRelease, "req8 Bind(null) releases the source");
            Assert.IsEmpty(numbersAfterRelease, "req8 the released window is unsynchronized again: no number");
            Assert.AreEqual(1, intentsAfterRelease, "req4 the released window's button sends nothing");
        }

        // req5: destroying the window (a scene unload) releases its source, and a button callback captured before the
        // destroy neither throws nor sends; later source changes reach no dead window.
        [TestCase(GuildWindowDriver.Guild)]
        [TestCase(GuildWindowDriver.Blacksmith)]
        public void Destroy_ReleasesTheSource_AndLeavesNoLiveButtonCallback(string kind)
        {
            using var window = new GuildWindowDriver(kind);
            var model = new GuildModelDriver();
            SupplyAllowed(model, kind);
            window.Bind(model);
            window.SetOpen(true);
            window.SetInputAllowed(true);
            int subscribedBefore = model.Subscribers;
            Button.ButtonClickedEvent onClick = window.ActionButton.onClick;
            window.DestroyWindow();
            int subscribedAfter = model.Subscribers;
            Exception staleCallbackError = Capture(() => onClick.Invoke());
            Exception laterChangeError = Capture(() => SupplyAllowedVariant(model, kind));

            Assert.AreEqual(1, subscribedBefore, "req5 precondition: the window subscribed its source");
            Assert.AreEqual(0, subscribedAfter, "req5 the destroyed window released its source");
            Assert.IsNull(staleCallbackError, "req5 a captured button callback of the destroyed window does not throw");
            Assert.AreEqual(0, window.Intents, "req5 a captured button callback of the destroyed window sends nothing");
            Assert.IsNull(laterChangeError, "req5 a later source change reaches no destroyed window");
        }

        // req5: a window built again for the same source (a map reload) shows the source's current value at once and
        // subscribes once.
        [TestCase(GuildWindowDriver.Guild)]
        [TestCase(GuildWindowDriver.Blacksmith)]
        public void RecreatedWindow_ShowsTheCurrentSourceAtOnce(string kind)
        {
            var model = new GuildModelDriver();
            SupplyAllowed(model, kind);
            using (var first = new GuildWindowDriver(kind))
            {
                first.Bind(model);
                first.DestroyWindow();
            }
            using var second = new GuildWindowDriver(kind);
            second.Bind(model);
            second.SetOpen(true);
            long[] shown = second.Numbers(CurrentValueLabel(kind));
            int subscribers = model.Subscribers;

            CollectionAssert.Contains(shown, CurrentValue(kind), "req5 the new window shows the source's current value");
            Assert.AreEqual(1, subscribers, "req5 only the new window is subscribed");
        }

        // The allowed state of each window: scene 6 funds the level-up, scene 8 readies the enhancement.
        static void SupplyAllowed(GuildModelDriver model, string kind)
        {
            if (kind == GuildWindowDriver.Guild) model.SetGuild(GuildInput.Funded());
            else model.SetBlacksmith(BlacksmithInput.Ready());
        }

        // Still allowed, with one shown value changed: one of the two members went offline, or the other class.
        static void SupplyAllowedVariant(GuildModelDriver model, string kind)
        {
            if (kind == GuildWindowDriver.Guild) model.SetGuild(GuildInput.Funded().With(guild => guild.OnlineCount = 1));
            else model.SetBlacksmith(BlacksmithInput.Ready().With(smith => smith.CharacterClass = CharacterClass.Mage));
        }

        static string VariantLabel(string kind) => kind == GuildWindowDriver.Guild ? "OnlineCount" : "CharacterClass";

        static string CurrentValueLabel(string kind) => kind == GuildWindowDriver.Guild ? "Treasury" : "GoldCost";

        static long CurrentValue(string kind) =>
            kind == GuildWindowDriver.Guild ? GuildInput.Funded().Treasury : BlacksmithInput.Ready().GoldCost;

        static Exception Capture(Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (Exception error)
            {
                return error;
            }
        }
    }
}
