using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Independent verification of goal 2026-10-15-guild-client-ui PR1 in real Unity frames (goal C1, C8 and C9 for the
    // windows themselves; api-contract 5, 6, 8 and 9), written by the verifier after the product. Real path: the
    // product's BuildRuntime, its own Canvas/TMP/GraphicRaycaster/CanvasGroup, Unity's own OnEnable/OnDisable, the
    // current EventSystem's raycast and a virtual mouse through the Input System UI module. Stand-ins: an unsaved empty
    // scene with its own EventSystem, values supplied by this test through the product's GuildDisplayModel, and no
    // server, socket, Town or map bootstrap. From input v4 visibility is judged per glyph quad from captured pixels
    // (Judge; rules and thresholds in Visibility, fixed before any v4 run; contract supplement v9 section 3). The real
    // Overlay is judged at the default screen where it draws and, in the editor-only OverlayAtTarget cases that run in
    // their own last root, at each goal size the main play mode view really reaches. The RenderTexture cases and the
    // negative controls switch the product canvases to ScreenSpaceCamera so a camera draws them at a chosen pixel size:
    // they are auxiliary and never stand for the Overlay screen or its pointers. Combat input and overlap with the
    // existing UI are PR2's live-screen checks.
    public sealed class GuildWindowIndependentSceneTests
    {
        const float WaitSeconds = 3f;
        // The harness points this at the verification evidence folder; without it nothing is written.
        const string CaptureDirectoryVariable = "DAWNHOLDER_GUILD_VERIFY_CAPTURE_DIR";
        const string ButtonTypeName = "UnityEngine.UI.Button";
        static readonly Regex Number = new Regex("\\d(?:[\\d,.   ]*\\d)?");

        Scene _previousActive;
        Scene _scene;
        GameObject _eventSystem;
        GameObject _root;
        Mouse _mouse;
        InputSettings _originalInputSettings;
        InputSettings _originalInputSettingsCopy;
        InputSettings _runtimeInputSettings;
        bool _inputSettingsReplaced;
        // TMP raises this when neither the font nor any fallback has a character and lays out the missing-glyph square
        // instead (TextMeshProUGUI's MISSING CHARACTER HANDLING), so it is the direct signal for unreadable Korean.
        readonly List<string> _missingCharacters = new List<string>();

        // The same fixture as GuildWindowSceneTests (that class is sealed and owns its copy): an unsaved scene that is
        // no CombatBootstrap map, a replaced and restored InputSettings (client.md), a virtual mouse and an EventSystem
        // with the Input System UI module reached by type name because this assembly does not reference uGUI.
        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.IsTrue(Application.isPlaying, "this lane must execute real Unity Play Mode");
            _previousActive = SceneManager.GetActiveScene();
            _scene = SceneManager.CreateScene("GuildIndependentFixture_" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(_scene);
            _originalInputSettings = InputSystem.settings;
            _originalInputSettingsCopy = UnityEngine.Object.Instantiate(_originalInputSettings);
            _originalInputSettingsCopy.hideFlags = HideFlags.HideAndDontSave;
            _runtimeInputSettings = UnityEngine.Object.Instantiate(_originalInputSettings);
            _runtimeInputSettings.hideFlags = HideFlags.HideAndDontSave;
            _runtimeInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _runtimeInputSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = _runtimeInputSettings;
            _inputSettingsReplaced = true;
            _mouse = InputSystem.AddDevice<Mouse>("GuildIndependentTestMouse");
            _eventSystem = new GameObject("GuildIndependentTestEventSystem");
            _eventSystem.AddComponent(TypeByName("UnityEngine.EventSystems.EventSystem, UnityEngine.UI"));
            _eventSystem.AddComponent(TypeByName("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"));
            _root = new GameObject("GuildIndependentTestRoot");
            _missingCharacters.Clear();
            TMP_Text.OnMissingCharacter += RecordMissingCharacter;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            var errors = new List<Exception>();
            void Restore(Action action)
            {
                try { action(); }
                catch (Exception error) { errors.Add(error); }
            }
            Restore(() => TMP_Text.OnMissingCharacter -= RecordMissingCharacter);
            Restore(() => { if (_root != null) UnityEngine.Object.Destroy(_root); });
            Restore(() => { if (_eventSystem != null) UnityEngine.Object.Destroy(_eventSystem); });
            Restore(() => { if (_mouse != null && _mouse.added) InputSystem.RemoveDevice(_mouse); });
            if (_inputSettingsReplaced)
            {
                // Unity null check, not ??: a destroyed original reads as null, and its copy takes its place.
                Restore(() =>
                {
                    InputSettings restored = _originalInputSettings != null ? _originalInputSettings : _originalInputSettingsCopy;
                    if (restored != null) InputSystem.settings = restored;
                });
            }
            foreach (InputSettings copy in new[] { _runtimeInputSettings, _originalInputSettingsCopy })
                Restore(() => { if (copy != null && copy != InputSystem.settings) UnityEngine.Object.Destroy(copy); });
            yield return null;
            Restore(() => { if (_previousActive.IsValid() && _previousActive.isLoaded) SceneManager.SetActiveScene(_previousActive); });
            if (_scene.IsValid() && _scene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(_scene);
                if (unload != null) yield return MapEntryPlayFixture.Wait(() => unload.isDone, "guild independent fixture unload", 5);
            }
            if (errors.Count != 0) throw new AggregateException("Guild independent fixture restoration failed", errors);
        }

        // api-contract 8 with Unity's own messages: a disabled guild window holds no subscription, so a change made
        // meanwhile reaches nothing; enabling it again subscribes once and shows the current value at once, and one
        // real pointer click then sends one intent.
        [UnityTest]
        public IEnumerator DisabledGuildWindow_TakesNoSourceChange_AndShowsTheCurrentValueOnceEnabled()
        {
            var model = new GuildDisplayModel();
            GuildWindow window = GuildWindow.BuildRuntime(_root.transform);
            int intents = 0;
            window.LevelUpRequested += () => intents++;
            model.SetGuild(ApprovedGuild(treasury: 30));
            yield return DisableEnableCycle(window, model, () => model.SetGuild(ApprovedGuild(treasury: 45)), "Treasury", 45L,
                () => intents);
        }

        // The same for the blacksmith window, with the next gold cost changed while it is disabled.
        [UnityTest]
        public IEnumerator DisabledBlacksmithWindow_TakesNoSourceChange_AndShowsTheCurrentValueOnceEnabled()
        {
            var model = new GuildDisplayModel();
            BlacksmithWindow window = BlacksmithWindow.BuildRuntime(_root.transform);
            int intents = 0;
            window.EnhanceRequested += () => intents++;
            model.SetBlacksmith(ApprovedSmith(gold: 20));
            yield return DisableEnableCycle(window, model, () => model.SetBlacksmith(ApprovedSmith(gold: 35)), "GoldCost", 35L,
                () => intents);
        }

        // C8 「미준비 상태에서 조작을 막는다」: an open window whose owner does not allow input sends nothing for a real
        // pointer click on its button; once allowed, the same click sends one intent (the control that the click lands).
        [UnityTest]
        public IEnumerator OpenWindowWithInputNotAllowed_APointerClickSendsNothing_UntilAllowed()
        {
            var model = new GuildDisplayModel();
            GuildWindow window = GuildWindow.BuildRuntime(_root.transform);
            int intents = 0;
            window.LevelUpRequested += () => intents++;
            model.SetGuild(ApprovedGuild(treasury: 30));
            window.Bind(model);
            window.SetOpen(true);
            window.SetInputAllowed(false);
            yield return Frames(3);
            yield return ScreenUiProbe.WaitForEventSystem();
            Component button = ActionButton(window);
            Vector2 point = ScreenUiProbe.ScreenPoint(button);
            bool drawnWhileNotAllowed = ScreenUiProbe.IsShown(Label(window, "Treasury"));
            HashSet<GameObject> windowObjects = Objects(window);
            string[] hitsWhileNotAllowed = ScreenUiProbe.RaycastHits(point).Where(windowObjects.Contains).Select(hit => hit.name).ToArray();
            yield return InventoryPanelProbe.ClickAt(_mouse, point);
            yield return Frames(3);
            int intentsWhileNotAllowed = intents;

            window.SetInputAllowed(true);
            yield return Frames(3);
            bool buttonHitWhenAllowed = ScreenUiProbe.RaycastHits(point).Any(Objects(button).Contains);
            yield return InventoryPanelProbe.ClickAt(_mouse, point);
            yield return ScreenUiProbe.WaitUntil(() => intents > 0, WaitSeconds, _ => { });
            yield return Frames(3);
            Debug.Log($"[guild verify input-not-allowed] screen {Screen.width}x{Screen.height}, button at {Format(point)}, drawn " +
                $"{drawnWhileNotAllowed}, hits while not allowed [{string.Join(", ", hitsWhileNotAllowed)}], intents while not " +
                $"allowed {intentsWhileNotAllowed}, button hit when allowed {buttonHitWhenAllowed}, intents {intents}");

            Assert.IsTrue(drawnWhileNotAllowed, "precondition: the open window is drawn while input is not allowed");
            Assert.AreEqual(0, intentsWhileNotAllowed, "C8 a click while input is not allowed sends nothing");
            Assert.IsTrue(buttonHitWhenAllowed, "control: the allowed window's button takes the raycast");
            Assert.AreEqual(1, intents, "control: once allowed, one click sends one intent");
        }

        // api-contract 1/9: both windows open at once on one model; a real click on each button sends only its own intent.
        [UnityTest]
        public IEnumerator BothWindowsOpen_EachPointerClickSendsOnlyItsOwnIntent()
        {
            var model = new GuildDisplayModel();
            GuildWindow guild = GuildWindow.BuildRuntime(_root.transform);
            BlacksmithWindow smith = BlacksmithWindow.BuildRuntime(_root.transform);
            int levelUps = 0;
            int enhances = 0;
            guild.LevelUpRequested += () => levelUps++;
            smith.EnhanceRequested += () => enhances++;
            model.SetGuild(ApprovedGuild(treasury: 30));
            model.SetBlacksmith(ApprovedSmith(gold: 20));
            OpenBoth(guild, smith, model);
            yield return Frames(3);
            yield return ScreenUiProbe.WaitForEventSystem();
            Vector2 guildPoint = ScreenUiProbe.ScreenPoint(ActionButton(guild));
            Vector2 smithPoint = ScreenUiProbe.ScreenPoint(ActionButton(smith));

            yield return InventoryPanelProbe.ClickAt(_mouse, guildPoint);
            yield return ScreenUiProbe.WaitUntil(() => levelUps > 0, WaitSeconds, _ => { });
            yield return Frames(3);
            int levelUpsAfterGuildClick = levelUps;
            int enhancesAfterGuildClick = enhances;
            yield return InventoryPanelProbe.ClickAt(_mouse, smithPoint);
            yield return ScreenUiProbe.WaitUntil(() => enhances > 0, WaitSeconds, _ => { });
            yield return Frames(3);
            Debug.Log($"[guild verify both-open clicks] screen {Screen.width}x{Screen.height}, guild button {Format(guildPoint)}, " +
                $"blacksmith button {Format(smithPoint)}, after guild click {levelUpsAfterGuildClick}/{enhancesAfterGuildClick}, " +
                $"final {levelUps}/{enhances}");

            Assert.AreEqual(1, levelUpsAfterGuildClick, "a click on the guild button sends one level-up");
            Assert.AreEqual(0, enhancesAfterGuildClick, "a click on the guild button sends no enhance");
            Assert.AreEqual(1, enhances, "a click on the blacksmith button sends one enhance");
            Assert.AreEqual(1, levelUps, "a click on the blacksmith button sends no level-up");
        }

        // api-contract 8: after the open window is bound to a replacement source, a real click follows the
        // replacement's availability, not the old source's.
        [UnityTest]
        public IEnumerator ReplacedSourceWhileOpen_ThePointerClickFollowsTheReplacement()
        {
            var old = new GuildDisplayModel();
            var replacement = new GuildDisplayModel();
            GuildWindow window = GuildWindow.BuildRuntime(_root.transform);
            int intents = 0;
            window.LevelUpRequested += () => intents++;
            old.SetGuild(ApprovedGuild(treasury: 30));
            replacement.SetGuild(ApprovedGuild(treasury: 12, levelUp: GuildActionAvailability.Insufficient));
            window.Bind(old);
            window.SetOpen(true);
            window.SetInputAllowed(true);
            yield return Frames(2);
            window.Bind(replacement);
            yield return Frames(3);
            yield return ScreenUiProbe.WaitForEventSystem();
            Vector2 point = ScreenUiProbe.ScreenPoint(ActionButton(window));
            int oldSubscribers = Subscribers(old);
            yield return InventoryPanelProbe.ClickAt(_mouse, point);
            yield return Frames(3);
            int intentsUnderShortReplacement = intents;

            replacement.SetGuild(ApprovedGuild(treasury: 30));
            yield return Frames(3);
            yield return InventoryPanelProbe.ClickAt(_mouse, point);
            yield return ScreenUiProbe.WaitUntil(() => intents > 0, WaitSeconds, _ => { });
            yield return Frames(3);
            Debug.Log($"[guild verify replaced source] old subscribers {oldSubscribers}, intents under short replacement " +
                $"{intentsUnderShortReplacement}, final {intents}");

            Assert.AreEqual(0, oldSubscribers, "api-contract 8 the old source keeps no subscription");
            Assert.AreEqual(0, intentsUnderShortReplacement, "api-contract 8 the replacement's Insufficient decides the click");
            Assert.AreEqual(1, intents, "api-contract 8 the replacement's Available allows one click");
        }

        // C9/api-contract 2: the server's name is literal text. TMP markup in it is laid out character by character,
        // not applied as rich text.
        [UnityTest]
        public IEnumerator SuppliedMarkupName_IsLaidOutAsLiteralCharacters()
        {
            const string markup = "<b>등불</b>";
            var model = new GuildDisplayModel();
            GuildWindow window = GuildWindow.BuildRuntime(_root.transform);
            model.SetGuild(ApprovedGuild(treasury: 30, name: markup));
            window.Bind(model);
            window.SetOpen(true);
            window.SetInputAllowed(true);
            yield return Frames(3);
            TMP_Text label = Label(window, "GuildName");
            label.ForceMeshUpdate();
            string laidOut = LaidOut(label);
            Debug.Log($"[guild verify markup name] text '{label.text}', laid out '{laidOut}', overflowing " +
                $"{label.isTextOverflowing}, missing [{string.Join("; ", _missingCharacters)}]");

            Assert.AreEqual(markup, laidOut, "every character of the supplied name is laid out as written, tags included");
            Assert.IsEmpty(_missingCharacters, "no character of the literal name is missing from the font and its fallbacks");
            Assert.IsFalse(label.isTextOverflowing, "the literal name fits its label");
        }

        // Control for C9's "laid out as written" check (contract supplement v6 section 1). U+E000 is in no font of the
        // label's chain (LiberationSans SDF, its dynamic fallback's source cmap and Pretendard SDF Proper:
        // static/font-coverage-probe-1.json), so TMP lays out its replacement. The comparison against the source string
        // and GuildWindowSceneTests.Unreadable must both report it; GetParsedText cannot, since it reads the replaced
        // characters too. The source comparison holds only for literal text without escapes or surrogate pairs: the
        // product sets richText off (GuildWindowLayout.cs:116) and this name has no backslash and only BMP characters.
        // Until the prior helper compares with the source (input v5), the last assertion is the planned RED.
        [UnityTest]
        public IEnumerator MissingGlyphControl_AnAbsentCharacterIsReportedByBothReadabilityChecks()
        {
            const string name = "등불";
            var model = new GuildDisplayModel();
            GuildWindow window = GuildWindow.BuildRuntime(_root.transform);
            model.SetGuild(ApprovedGuild(treasury: 30, name: name));
            window.Bind(model);
            window.SetOpen(true);
            window.SetInputAllowed(true);
            yield return Frames(3);
            TMP_Text label = Label(window, "GuildName");
            label.ForceMeshUpdate();
            string laidOut = LaidOut(label);
            string parsed = label.GetParsedText();
            string[] reported = PriorUnreadable(label);
            Debug.Log($"[guild verify missing glyph control] text '{label.text}', laid out '{laidOut}', parsed '{parsed}', " +
                $"richText {label.richText}, missing [{string.Join("; ", _missingCharacters)}], prior helper [{string.Join("; ", reported)}]");

            Assert.IsFalse(label.richText, "precondition: the product lays the name out as literal text (richText off)");
            Assert.That(_missingCharacters, Has.Some.StartsWith("U+E000"), "precondition: the font chain lacks U+E000");
            Assert.AreNotEqual(name, laidOut, "the comparison against the source string sees the replacement");
            Assert.AreEqual(laidOut, parsed, "evidence: GetParsedText returns the replaced characters, so it cannot see them");
            Assert.That(reported, Has.Some.Contains("lays out"), "GuildWindowSceneTests.Unreadable reports the replaced character");
        }

        // C8/C9 at this run's default screen, whose size is read, not chosen (contract supplement v11 section 4: the
        // PlayMode roots run the GUI editor, whose own main GameView draws the Overlay; no size is forced and this case
        // sizes nothing). The product's own Overlay is captured and judged by Judge. The case fails unless the main play
        // mode view is that existing GameView, Screen is positive and equals PlayModeWindow.GetRenderingResolution and
        // every capture's size, each capture of the judgement arrives at a later frame than the one before, and the frame
        // is not empty (a capture with no panel, no ink and no reference text drawn fails; it is no longer a "not
        // reviewed" batch limit). The same windows are then judged in a RenderTexture of the measured Screen size
        // (auxiliary renderMode), which never stands for the Overlay. Renamed in input v4 from
        // BothWindowsOpen_AtTheActualScreen_ApprovedValuesAreWholeAndApart (static/case-name-mapping-4.json).
        [UnityTest]
        public IEnumerator DefaultScreen_ApprovedValues_OverlayIsCapturedAndJudgedWhereDrawn_RenderTextureAuxiliaryIsWholeAndApart()
        {
            yield return MeasureAtDefaultScreen("default-screen-approved", ApprovedGuild(treasury: 30), ApprovedSmith(gold: 20),
                checkRaycasts: true);
        }

        // The same with the largest values the World contract allows (World goal 2026-10-14-guild-server lines 345 and
        // 350: a 16 UTF-16 code unit name, int counts/treasury/costs/bonus and byte levels). Renamed in input v4 from
        // BothWindowsOpen_AtTheActualScreen_UpperBoundValuesAreWholeAndApart.
        [UnityTest]
        public IEnumerator DefaultScreen_UpperBoundValues_OverlayIsCapturedAndJudgedWhereDrawn_RenderTextureAuxiliaryIsWholeAndApart()
        {
            yield return MeasureAtDefaultScreen("default-screen-upper-bound", UpperBoundGuild(), UpperBoundSmith(),
                checkRaycasts: false);
        }

        // Auxiliary (see the class comment): the goal's 709x399 drawn into a RenderTexture of that size.
        [UnityTest]
        public IEnumerator RenderTextureAuxiliary_709x399_BothWindowsAreWholeAndApart()
        {
            yield return MeasureInRenderTexture(709, 399);
        }

        // Auxiliary: the goal's 802x451.
        [UnityTest]
        public IEnumerator RenderTextureAuxiliary_802x451_BothWindowsAreWholeAndApart()
        {
            yield return MeasureInRenderTexture(802, 451);
        }

        // Auxiliary: the goal's 1920x1080.
        [UnityTest]
        public IEnumerator RenderTextureAuxiliary_1920x1080_BothWindowsAreWholeAndApart()
        {
            yield return MeasureInRenderTexture(1920, 1080);
        }

        // Negative controls for Judge (contract supplement v9 section 3, v10 section 2). Each damages real product
        // objects in a RenderTexture of the measured Screen size (auxiliary renderMode), checks from the raw counts that
        // the damage really changed the drawn pixels, and requires the same Judge that the size cases use to report it;
        // undoing the damage must leave no finding. They prove the judgement, not the Overlay pipeline.

        // A RectMask2D that keeps only the left half of Treasury's glyph band: the hidden characters are reported as cut,
        // a kept character is not, and the clipped quads leave the mask.
        [UnityTest]
        public IEnumerator NegativeControl_PartlyClippedText_OnlyItsHiddenCharactersAreReported()
        {
            yield return NegativeControl("negative-partly-clipped",
                (pair, expected) => new[] { Find(expected, pair.Guild, "Treasury").Name },
                pair => ClipTo(Label(pair.Guild, "Treasury"),
                    band => Rect.MinMaxRect(band.xMin - 4f, band.yMin - 4f, band.center.x, band.yMax + 4f)),
                "Treasury's S ink drops, one character loses more than half of it and another keeps it (1 px)",
                (before, after, target) => after.Part(target[0]).inkS < before.Part(target[0]).inkS &&
                    Pairs(before, after, target[0]).Any(pair => pair.After.s * 2 < pair.Before.s) &&
                    Pairs(before, after, target[0]).Any(pair => pair.Before.s > 0 && pair.After.s + 1 >= pair.Before.s),
                "some Treasury character is cut, another has no finding, and Treasury's quads leave the RectMask2D",
                (damaged, target) => Characters(damaged, target[0]).Any(index => damaged.Reports(target[0], index, Kinds.Cut)) &&
                    Characters(damaged, target[0]).Any(index => damaged.Clean(target[0], index)) &&
                    damaged.Findings.Any(finding => finding.Name == target[0] && finding.Kind == Kinds.Structure &&
                        finding.Detail.Contains("RectMask2D")));
        }

        // A RectMask2D that keeps only the lower 35% of BaseLevel's glyph band: every character is cut at once.
        [UnityTest]
        public IEnumerator NegativeControl_EveryCharacterClipped_EachCharacterIsReported()
        {
            yield return NegativeControl("negative-every-character-clipped",
                (pair, expected) => new[] { Find(expected, pair.Guild, "BaseLevel").Name },
                pair => ClipTo(Label(pair.Guild, "BaseLevel"),
                    band => Rect.MinMaxRect(band.xMin - 4f, band.yMin - 4f, band.xMax + 4f, band.yMin + band.height * 0.35f)),
                "every BaseLevel character that drew S ink draws less",
                (before, after, target) => Pairs(before, after, target[0]).Any() &&
                    Pairs(before, after, target[0]).All(pair => pair.Before.s > 0 && pair.After.s < pair.Before.s),
                "every BaseLevel character is cut",
                (damaged, target) => Characters(damaged, target[0]).Any() &&
                    Characters(damaged, target[0]).All(index => damaged.Reports(target[0], index, Kinds.Cut)));
        }

        // The guild window's button moved over Treasury (it is drawn after the texts): Treasury is covered.
        [UnityTest]
        public IEnumerator NegativeControl_ButtonOverText_TheCoveredTextIsReported()
        {
            yield return NegativeControl("negative-button-over-text",
                (pair, expected) => new[] { Find(expected, pair.Guild, "Treasury").Name },
                pair => Move((RectTransform)ActionButton(pair.Guild).transform,
                    ((RectTransform)Label(pair.Guild, "Treasury").transform).anchoredPosition + new Vector2(0f, 7f)),
                "Treasury's F∩S ink drops",
                (before, after, target) => after.Part(target[0]).inkFS < before.Part(target[0]).inkFS,
                "Treasury is reported covered",
                (damaged, target) => damaged.Reports(target[0], Kinds.Covered));
        }

        // BaseLevel moved onto Treasury: each text's quads carry the other's ink.
        [UnityTest]
        public IEnumerator NegativeControl_NeighbourTextOverText_BothMixedTextsAreReported()
        {
            yield return NegativeControl("negative-neighbour-over-text",
                (pair, expected) => new[] { Find(expected, pair.Guild, "Treasury").Name, Find(expected, pair.Guild, "BaseLevel").Name },
                pair => Move((RectTransform)Label(pair.Guild, "BaseLevel").transform,
                    ((RectTransform)Label(pair.Guild, "Treasury").transform).anchoredPosition),
                "Treasury's quads hold more F ink",
                (before, after, target) => after.Part(target[0]).inkF > before.Part(target[0]).inkF,
                "Treasury and BaseLevel are both reported with foreign ink",
                (damaged, target) => target.All(name => damaged.Reports(name, Kinds.Foreign)));
        }

        // The blacksmith panel moved onto the guild panel with its canvas sorted above: every guild text is covered.
        [UnityTest]
        public IEnumerator NegativeControl_OtherWindowOverText_EveryCoveredTextIsReported()
        {
            yield return NegativeControl("negative-other-window-over-text",
                (pair, expected) => expected.Where(item => item.Window == pair.Guild).Select(item => item.Name).ToArray(),
                pair =>
                {
                    RectTransform guildPanel = Panel(pair.Guild);
                    RectTransform smithPanel = Panel(pair.Smith);
                    Canvas guildCanvas = pair.Guild.GetComponent<Canvas>();
                    Canvas smithCanvas = pair.Smith.GetComponent<Canvas>();
                    Vector2 anchorMin = smithPanel.anchorMin;
                    Vector2 anchorMax = smithPanel.anchorMax;
                    Vector2 anchored = smithPanel.anchoredPosition;
                    int order = smithCanvas.sortingOrder;
                    smithPanel.anchorMin = guildPanel.anchorMin;
                    smithPanel.anchorMax = guildPanel.anchorMax;
                    // Offset shared header captions so the covering window cannot redraw the same glyphs in place.
                    smithPanel.anchoredPosition = guildPanel.anchoredPosition + new Vector2(8f, -8f);
                    smithCanvas.sortingOrder = guildCanvas.sortingOrder + 1;
                    return () =>
                    {
                        smithPanel.anchorMin = anchorMin;
                        smithPanel.anchorMax = anchorMax;
                        smithPanel.anchoredPosition = anchored;
                        smithCanvas.sortingOrder = order;
                    };
                },
                "every guild text's F∩S ink drops",
                (before, after, target) => target.Length > 0 && target.All(name => after.Part(name).inkFS < before.Part(name).inkFS),
                "every guild text is reported covered",
                (damaged, target) => target.All(name => damaged.Reports(name, Kinds.Covered)));
        }

        // Both canvases switched off: the frame is empty and every text is reported cut.
        [UnityTest]
        public IEnumerator NegativeControl_EmptyScreen_EveryTextIsReported()
        {
            yield return NegativeControl("negative-empty-screen",
                (pair, expected) => expected.Select(item => item.Name).ToArray(),
                pair =>
                {
                    Canvas[] canvases = pair.Both.Select(window => window.GetComponent<Canvas>()).ToArray();
                    bool[] enabled = canvases.Select(canvas => canvas.enabled).ToArray();
                    foreach (Canvas canvas in canvases) canvas.enabled = false;
                    return () =>
                    {
                        for (int i = 0; i < canvases.Length; i++) canvases[i].enabled = enabled[i];
                    };
                },
                "the frame had ink before and has none after",
                (before, after, target) => before.Record.frameInk > 0 && after.Record.frameInk == 0,
                "every text is reported cut",
                (damaged, target) => target.Length > 0 && target.All(name => damaged.Reports(name, Kinds.Cut)));
        }

        // The guild button's caption emptied: the expected caption stays the one shown before the damage, so the empty
        // label is reported as another text and every expected character as cut.
        [UnityTest]
        public IEnumerator NegativeControl_EmptiedButtonCaption_TheMissingCaptionIsReported()
        {
            TMP_Text caption = null;
            yield return NegativeControl("negative-emptied-button-caption",
                (pair, expected) =>
                {
                    caption = Label(ActionButton(pair.Guild), "Label");
                    LabelExpectation[] found = expected.Where(item => ReferenceEquals(item.Text, caption)).ToArray();
                    Assert.AreEqual(1, found.Length, "fixture: exactly one expected text for the action button caption");
                    return new[] { found[0].Name };
                },
                pair =>
                {
                    string before = caption.text;
                    caption.text = string.Empty;
                    return () => caption.text = before;
                },
                "the caption drew S ink before and none after",
                (before, after, target) => before.Part(target[0]).inkS > 0 && after.Part(target[0]).inkS == 0,
                "the caption is reported as another text and every expected character as cut",
                (damaged, target) => damaged.Reports(target[0], Kinds.Text) && Characters(damaged, target[0]).Any() &&
                    Characters(damaged, target[0]).All(index => damaged.Reports(target[0], index, Kinds.Cut)));
        }

#if UNITY_EDITOR
        // C8/C9 at the real Overlay screen of the goal's 709x399: select an existing FixedResolution or add just
        // that missing size in memory under the runner permit; no disk save or window resize. Judged only
        // when Screen, PlayModeWindow.GetRenderingResolution and the Overlay capture are all 709x399; otherwise the size is
        // not reviewable here, which fails the case without saying anything of the product. As at the default screen
        // (contract supplement v12 section 3), every capture of the judgement must be of that size and arrive at a later
        // frame than the one before. Runs in its own last root.
        [UnityTest]
        public IEnumerator OverlayAtTarget_709x399_BothWindowsAreWholeAndApart()
        {
            yield return MeasureOverlayAtTarget(709, 399);
        }

        // The same at the goal's 802x451.
        [UnityTest]
        public IEnumerator OverlayAtTarget_802x451_BothWindowsAreWholeAndApart()
        {
            yield return MeasureOverlayAtTarget(802, 451);
        }

        // The same at the goal's 1920x1080, by selecting the current size group's existing FixedResolution 1920x1080 item
        // with GameView.SizeSelectionCallback, as the view's own size menu does; nothing is added to the list.
        [UnityTest]
        public IEnumerator OverlayAtTarget_1920x1080_BothWindowsAreWholeAndApart()
        {
            yield return MeasureOverlayAtTarget(1920, 1080);
        }
#endif

        IEnumerator DisableEnableCycle(Component window, GuildDisplayModel model, Action changeWhileDisabled,
            string valueLabel, long changedValue, Func<int> intents)
        {
            OpenOne(window, model);
            yield return Frames(3);
            yield return ScreenUiProbe.WaitForEventSystem();
            int subscribedWhileEnabled = Subscribers(model);

            window.gameObject.SetActive(false);
            yield return Frames(2);
            int subscribedWhileDisabled = Subscribers(model);
            Exception changeError = null;
            try { changeWhileDisabled(); }
            catch (Exception error) { changeError = error; }
            yield return Frames(2);

            window.gameObject.SetActive(true);
            yield return Frames(3);
            int subscribedAfterEnable = Subscribers(model);
            long[] shownAfterEnable = NumbersIn(Label(window, valueLabel).text);
            bool drawnAfterEnable = ScreenUiProbe.IsShown(Label(window, valueLabel));
            Vector2 point = ScreenUiProbe.ScreenPoint(ActionButton(window));
            yield return InventoryPanelProbe.ClickAt(_mouse, point);
            yield return ScreenUiProbe.WaitUntil(() => intents() > 0, WaitSeconds, _ => { });
            yield return Frames(3);
            Debug.Log($"[guild verify disable-enable {window.GetType().Name}] subscribed {subscribedWhileEnabled}/" +
                $"{subscribedWhileDisabled}/{subscribedAfterEnable}, {valueLabel} after enable " +
                $"[{string.Join(", ", shownAfterEnable)}], drawn {drawnAfterEnable}, intents {intents()}");

            Assert.AreEqual(1, subscribedWhileEnabled, "precondition: the enabled window subscribed its source once");
            Assert.AreEqual(0, subscribedWhileDisabled, "api-contract 8 the disabled window holds no subscription");
            Assert.IsNull(changeError, "a change while the window is disabled reaches nothing that throws");
            Assert.AreEqual(1, subscribedAfterEnable, "api-contract 8 enabling subscribes once again");
            CollectionAssert.Contains(shownAfterEnable, changedValue, "api-contract 8 the enabled window shows the value changed while disabled");
            Assert.IsTrue(drawnAfterEnable, "the re-enabled open window is drawn");
            Assert.AreEqual(1, intents(), "one real click on the re-enabled window sends one intent");
        }

        IEnumerator MeasureAtDefaultScreen(string label, GuildStatusDisplay guildValue, BlacksmithDisplay smithValue, bool checkRaycasts)
        {
            int width = Screen.width;
            int height = Screen.height;
            WindowPair pair = Build(label, guildValue, smithValue);
            Surface overlay = OverlaySurface(pair.Holder);
            yield return Settle();
            LabelExpectation[] expected = Expect(pair.Both);
            var real = new Judgement(label + "-overlay", "ScreenSpaceOverlay (product as built) at the default screen");
            VisibilityRecord seen = real.Record;
            seen.mainPlayModeView = MainPlayModeViewState();
#if UNITY_EDITOR
            // As in MeasureOverlayAtTarget: the main view's own targetSize, recorded beside what Screen and the capture show.
            UnityEditor.PlayModeWindow.GetRenderingResolution(out uint renderingWidth, out uint renderingHeight);
            seen.renderingWidth = (int)renderingWidth;
            seen.renderingHeight = (int)renderingHeight;
#else
            seen.renderingWidth = -1;
            seen.renderingHeight = -1;
#endif
            CheckModel(real, expected, pair);
            yield return Judge(overlay, pair.Both, expected, real);
            var raycasts = new List<string>();
            if (checkRaycasts)
            {
                yield return ScreenUiProbe.WaitForEventSystem();
                raycasts.AddRange(RaycastProblems(pair.Both, seen));
            }
            bool sizeAgrees = width > 0 && height > 0 && seen.screenWidth == width && seen.screenHeight == height &&
                seen.renderingWidth == width && seen.renderingHeight == height && seen.frameWidth == width && seen.frameHeight == height;
            string[] capturesOff = CapturesOff(seen, width, height);
            bool nothingDrawn = seen.frameInk == 0 && seen.referenceInk == 0 && seen.panelLit.All(lit => lit < Visibility.PanelNotDrawn);
            string[] overlayProblems = real.Lines;
            Finish(real);

            var auxiliary = new Judgement(label + "-rendertexture", "ScreenSpaceCamera into a RenderTexture of the measured " +
                "Screen size (auxiliary renderMode; the product builds ScreenSpaceOverlay)");
            Surface auxiliarySurface = RenderTextureSurface(pair.Holder, width, height, pair.Both);
            yield return Settle();
            CheckModel(auxiliary, expected, pair);
            yield return Judge(auxiliarySurface, pair.Both, expected, auxiliary);
            string[] auxiliaryProblems = auxiliary.Lines;
            int judged = auxiliary.Record.labels.Length;
            Finish(auxiliary);
            Release(auxiliarySurface);
            UnityEngine.Object.Destroy(pair.Holder);
            yield return Frames(2);
            Debug.Log($"[guild verify {label}] Screen {width}x{height}, rendering {seen.renderingWidth}x{seen.renderingHeight}, " +
                $"main play mode view {seen.mainPlayModeView}, capture {seen.frameWidth}x{seen.frameHeight}, captures off " +
                $"[{string.Join("; ", capturesOff)}], nothing drawn {nothingDrawn}, raycasts [{string.Join("; ", seen.raycasts ?? new string[0])}]");

            Assert.AreEqual("present (UnityEditor.GameView)", seen.mainPlayModeView, "GUI: the main play mode view is the editor's " +
                "own GameView, which this case does not create or size");
            Assert.IsTrue(sizeAgrees, $"GUI: Screen {width}x{height} is positive and equals the rendering resolution " +
                $"{seen.renderingWidth}x{seen.renderingHeight} and the Overlay capture ({seen.captureMethod}: {seen.frameWidth}x{seen.frameHeight})");
            Assert.IsEmpty(capturesOff, "GUI: every capture of the Overlay judgement is of the Screen's size and arrives at a later frame");
            Assert.IsFalse(nothingDrawn, $"GUI: the default screen draws the windows; {seen.frameLit} lit and {seen.frameInk} ink pixels, " +
                $"reference ink {seen.referenceInk}");
            Assert.IsEmpty(overlayProblems, $"C8/C9 at the real {width}x{height} Overlay as drawn every expected text is whole and apart");
            Assert.IsEmpty(raycasts, "each window's button takes the raycast at the default screen");
            Assert.Greater(judged, 0, "precondition: expected texts were judged");
            Assert.IsEmpty(auxiliaryProblems, $"C8/C9 auxiliary RenderTexture of the measured {width}x{height} screen: every " +
                "expected text is whole and apart");
        }

        IEnumerator MeasureInRenderTexture(int requestedWidth, int requestedHeight)
        {
            var fixtureProblems = new List<string>();
            var allProblems = new List<string>();
            foreach (bool upperBound in new[] { false, true })
            {
                string label = $"rendertexture-{requestedWidth}x{requestedHeight}-{(upperBound ? "upper-bound" : "approved")}";
                WindowPair pair = Build(label, upperBound ? UpperBoundGuild() : ApprovedGuild(treasury: 30),
                    upperBound ? UpperBoundSmith() : ApprovedSmith(gold: 20));
                Surface surface = RenderTextureSurface(pair.Holder, requestedWidth, requestedHeight, pair.Both);
                yield return Settle();
                LabelExpectation[] expected = Expect(pair.Both);
                var judgement = new Judgement(label, "ScreenSpaceCamera into a RenderTexture (auxiliary; the product builds ScreenSpaceOverlay)");
                judgement.Record.requestedWidth = requestedWidth;
                judgement.Record.requestedHeight = requestedHeight;
                CheckModel(judgement, expected, pair);
                yield return Judge(surface, pair.Both, expected, judgement);

                VisibilityRecord record = judgement.Record;
                Vector2 scalerSize = pair.Guild.GetComponent<Canvas>().renderingDisplaySize;
                if (record.renderTextureWidth != requestedWidth || record.renderTextureHeight != requestedHeight ||
                    record.frameWidth != requestedWidth || record.frameHeight != requestedHeight ||
                    record.cameraPixelWidth != requestedWidth || record.cameraPixelHeight != requestedHeight)
                    fixtureProblems.Add($"{label}: texture {record.renderTextureWidth}x{record.renderTextureHeight}, readback " +
                        $"{record.frameWidth}x{record.frameHeight}, camera {record.cameraPixelWidth}x{record.cameraPixelHeight}");
                if (!Mathf.Approximately(scalerSize.x, requestedWidth) || !Mathf.Approximately(scalerSize.y, requestedHeight))
                    fixtureProblems.Add($"{label}: the canvas scaler sees {Format(scalerSize)}");
                if (record.frameInk == 0) fixtureProblems.Add($"{label}: the camera drew no ink ({record.captureMethod})");
                allProblems.AddRange(judgement.Lines.Select(problem => label + ": " + problem));
                Finish(judgement);
                Release(surface);
                UnityEngine.Object.Destroy(pair.Holder);
                yield return Frames(2);
            }

            Assert.IsEmpty(fixtureProblems, $"fixture: texture, readback, camera and canvas scaler are {requestedWidth}x{requestedHeight} " +
                "and the camera drew the windows");
            Assert.IsEmpty(allProblems, $"C8/C9 auxiliary {requestedWidth}x{requestedHeight}: every expected text is whole and apart");
        }

        // One negative control: the undamaged windows, the damaged ones and the undone damage are judged by the same
        // Judge on the same surface against the expectations taken before any damage. The damage and its undo are real
        // product objects; the raw-count rule shows the damage reached the pixels before the report rule is asked.
        IEnumerator NegativeControl(string label, Func<WindowPair, LabelExpectation[], string[]> targets,
            Func<WindowPair, Action> damage, string damageRule, Func<Judgement, Judgement, string[], bool> damageIsReal,
            string reportRule, Func<Judgement, string[], bool> reported)
        {
            WindowPair pair = Build(label, ApprovedGuild(treasury: 30), ApprovedSmith(gold: 20));
            Surface surface = RenderTextureSurface(pair.Holder, Screen.width, Screen.height, pair.Both);
            yield return Settle();
            LabelExpectation[] expected = Expect(pair.Both);
            string[] names = targets(pair, expected);
            const string mode = "ScreenSpaceCamera into a RenderTexture of the measured Screen size (auxiliary renderMode; negative control)";
            var baseline = new Judgement(label + "-baseline", mode);
            CheckModel(baseline, expected, pair);
            yield return Judge(surface, pair.Both, expected, baseline);
            Action undo = damage(pair);
            yield return Settle();
            var damaged = new Judgement(label + "-damaged", mode);
            yield return Judge(surface, pair.Both, expected, damaged);
            undo();
            yield return Settle();
            var restored = new Judgement(label + "-restored", mode);
            yield return Judge(surface, pair.Both, expected, restored);
            bool real = damageIsReal(baseline, damaged, names);
            bool detected = reported(damaged, names);
            string[] baselineProblems = baseline.Lines;
            string[] restoredProblems = restored.Lines;
            int judged = baseline.Record.labels.Length;
            Finish(baseline);
            Finish(damaged);
            Finish(restored);
            Release(surface);
            UnityEngine.Object.Destroy(pair.Holder);
            yield return Frames(2);
            Debug.Log($"[guild verify {label}] targets [{string.Join(", ", names)}], damage real {real} ({damageRule}), " +
                $"reported {detected} ({reportRule})");

            Assert.Greater(judged, 0, "precondition: expected texts were judged");
            Assert.IsEmpty(baselineProblems, "precondition: the undamaged windows are judged whole and apart");
            Assert.IsTrue(real, "precondition: the damage changed the drawn pixels: " + damageRule);
            Assert.IsTrue(detected, "the visibility judgement reports the damage: " + reportRule);
            Assert.IsEmpty(restoredProblems, "control: once the damage is undone the same judgement reports nothing");
        }

#if UNITY_EDITOR
        IEnumerator MeasureOverlayAtTarget(int width, int height)
        {
            var notReviewable = new List<string>();
            var allProblems = new List<string>();
            var sizing = new List<string>();
            MainPlayModeView view = MainPlayModeView.Open(sizing);
            try
            {
                view.SelectFixedResolution(width, height, sizing);
                yield return Frames(3);
                foreach (bool upperBound in new[] { false, true })
                {
                    string label = $"overlay-{width}x{height}-{(upperBound ? "upper-bound" : "approved")}";
                    WindowPair pair = Build(label, upperBound ? UpperBoundGuild() : ApprovedGuild(treasury: 30),
                        upperBound ? UpperBoundSmith() : ApprovedSmith(gold: 20));
                    Surface surface = OverlaySurface(pair.Holder);
                    yield return Settle();
                    yield return ScreenUiProbe.WaitForEventSystem();
                    // Not an independent reading: GetRenderingResolution returns the main view's targetSize (installed IL),
                    // the value the view computed. Screen and the capture are what the editor actually drew.
                    UnityEditor.PlayModeWindow.GetRenderingResolution(out uint renderingWidth, out uint renderingHeight);
                    LabelExpectation[] expected = Expect(pair.Both);
                    var judgement = new Judgement(label, "ScreenSpaceOverlay (product as built), size asked of the main play mode view");
                    VisibilityRecord record = judgement.Record;
                    record.requestedWidth = width;
                    record.requestedHeight = height;
                    record.renderingWidth = (int)renderingWidth;
                    record.renderingHeight = (int)renderingHeight;
                    record.mainPlayModeView = MainPlayModeViewState();
                    record.playModeView = view.Describe();
                    record.sizing = sizing.ToArray();
                    CheckModel(judgement, expected, pair);
                    yield return Judge(surface, pair.Both, expected, judgement);
                    List<string> raycasts = upperBound ? new List<string>() : RaycastProblems(pair.Both, record);
                    bool sizeReached = record.screenWidth == width && record.screenHeight == height &&
                        record.renderingWidth == width && record.renderingHeight == height &&
                        record.frameWidth == width && record.frameHeight == height;
                    string[] capturesOff = CapturesOff(record, width, height);
                    if (!sizeReached)
                    {
                        record.notReviewed = $"Screen {record.screenWidth}x{record.screenHeight}, rendering {record.renderingWidth}x" +
                            $"{record.renderingHeight}, capture {record.frameWidth}x{record.frameHeight}, captures off " +
                            $"[{string.Join("; ", capturesOff)}]";
                        notReviewable.Add(label + ": " + record.notReviewed);
                    }
                    else
                    {
                        allProblems.AddRange(judgement.Lines.Concat(raycasts).Concat(capturesOff.Select(off => "capture off: " + off))
                            .Select(problem => label + ": " + problem));
                    }
                    Finish(judgement);
                    UnityEngine.Object.Destroy(pair.Holder);
                    yield return Frames(2);
                }
            }
            finally
            {
                view.Release(sizing);
            }
            yield return Frames(2);
            sizing.Add(string.Format(CultureInfo.InvariantCulture, "after release: Screen {0}x{1}", Screen.width, Screen.height));
            WriteNotes($"overlay-{width}x{height}-sizing", sizing);

            Assert.IsEmpty(notReviewable, $"N: Screen, rendering resolution and Overlay capture reach {width}x{height}; " +
                "otherwise this size is not reviewable here (no product defect)");
            Assert.IsEmpty(allProblems, $"C8/C9 at the real {width}x{height} Overlay screen every expected text is whole and " +
                "apart, each button takes its raycast and every capture is of that size at a later frame");
        }
#endif

        // Judges every expected text on one drawn surface (contract supplement v9 section 3). F is the frame as drawn.
        // For each text, R is a separate unmasked TextMeshPro on its own root canvas (same render mode, camera, sorting
        // order and scale factor as the product's) with the same font, material, style, colour, size, rectangle and
        // pixel scale at the same place, carrying the expected string, drawn alone while the product canvases are off.
        // S is the product text itself while every other Graphic of the windows is switched off for that one capture;
        // its canvases, masks and its own TMP sub-meshes stay as they are and every switched Graphic is put back in a
        // finally block. Per visible character, over R's glyph quad: R must have ink; S may lose at most
        // MinimumSOverR of R's ink, F may hide at most MinimumFOverS of S's ink (each with one pixel of slack); and inside
        // the quad shrunk by one pixel no F ink may stand where S is dark (another drawing's ink). Text correspondence,
        // the label against R's quads, the surface, any RectMask2D, overflow and the visibility limits are checked too,
        // and a last capture must show the same ink as F. Nothing here repeats the product's placement formula. Every
        // capture (F, each R and S, after) is noted and its whole frame kept as a new PNG before it is released
        // (Judgement.NoteCapture, contract supplement v12 section 3); the R/S/F crops are only derived from them.
        IEnumerator Judge(Surface surface, Component[] windows, LabelExpectation[] expected, Judgement judgement)
        {
            VisibilityRecord record = judgement.Record;
            Canvas[] canvases = windows.Select(window => window.GetComponent<Canvas>()).ToArray();
            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (LabelExpectation item in expected) item.Text.ForceMeshUpdate();
            record.surface = surface.Mode;
            record.screenWidth = Screen.width;
            record.screenHeight = Screen.height;
            record.surfaceWidth = surface.Width;
            record.surfaceHeight = surface.Height;
            record.canvasScaleFactor = canvases[0].scaleFactor;
            record.canvasRenderingDisplaySize = Format(canvases[0].renderingDisplaySize);
            record.canvasPixelRect = Format(canvases[0].pixelRect);
            if (surface.Texture != null)
            {
                record.renderTextureWidth = surface.Texture.width;
                record.renderTextureHeight = surface.Texture.height;
                record.cameraPixelWidth = surface.Camera.pixelWidth;
                record.cameraPixelHeight = surface.Camera.pixelHeight;
            }

            Frame full = null;
            yield return Capture(surface, frame => full = frame);
            int fullOriginal = judgement.NoteCapture("F", full, surface);
            record.captureMethod = full != null ? full.Method : "no capture: " + (surface.LastProblem ?? "no reason given");
            record.captureFrame = full != null ? full.FrameCount : -1;
            record.frameWidth = full != null ? full.Width : -1;
            record.frameHeight = full != null ? full.Height : -1;
            judgement.FrameTexture = full?.Texture;
            if (full == null || full.Width != surface.Width || full.Height != surface.Height)
            {
                judgement.Add("frame", -1, Kinds.Unjudgeable, $"capture {record.frameWidth}x{record.frameHeight} is not the " +
                    $"{surface.Width}x{surface.Height} surface ({record.captureMethod})");
                yield break;
            }
            record.frameLit = CountAtLeast(full, Visibility.LitLevel);
            record.frameInk = CountAtLeast(full, Visibility.InkLevel);
            record.panelLit = windows.Select(window => LitFraction(full, Panel(window), surface.Camera)).ToArray();
            LabelState[] states = expected.Select(item => new LabelState(item, surface.Camera)).ToArray();

            Canvas referenceCanvas = ReferenceCanvas(windows[0].transform.parent, canvases[0]);
            bool[] canvasesEnabled = canvases.Select(canvas => canvas.enabled).ToArray();
            try
            {
                // The new root canvas takes its place on the surface first, so R is laid out at its final pixel scale.
                yield return null;
                Canvas.ForceUpdateCanvases();
                record.referenceScaleFactor = referenceCanvas.scaleFactor;
                record.referenceRoot = DescribeRoot(referenceCanvas.transform);
                record.productRoot = DescribeRoot(canvases[0].transform);
                if (!SamePlace(referenceCanvas.transform, canvases[0].transform))
                    judgement.Add("frame", -1, Kinds.Unjudgeable, $"R's canvas root {record.referenceRoot} is not the product " +
                        $"canvas root {record.productRoot}");
                foreach (LabelState state in states) state.Reference = CreateReference(referenceCanvas, state.Item);
                foreach (Canvas canvas in canvases) canvas.enabled = false;
                foreach (LabelState state in states)
                {
                    state.Reference.enabled = true;
                    state.Reference.ForceMeshUpdate();
                    Frame alone = null;
                    yield return Capture(surface, frame => alone = frame);
                    state.OriginalR = judgement.NoteCapture("R " + state.Item.Name, alone, surface);
                    state.ReferenceQuads = Quads(state.Reference, surface.Camera);
                    state.Reference.enabled = false;
                    if (SameSize(alone, full)) state.MeasureReference(alone);
                    else judgement.Add(state.Item.Name, -1, Kinds.Unjudgeable, "R capture is missing or of another size");
                    Release(alone);
                }
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++) canvases[i].enabled = canvasesEnabled[i];
                referenceCanvas.enabled = false;
                UnityEngine.Object.Destroy(referenceCanvas.gameObject);
            }

            Behaviour[] graphics = windows.SelectMany(window => window.GetComponentsInChildren<Behaviour>(true)).Where(IsGraphic).ToArray();
            foreach (LabelState state in states)
            {
                HashSet<Behaviour> kept = Kept(state.Item.Text, graphics);
                bool[] enabledBefore = graphics.Select(graphic => graphic.enabled).ToArray();
                state.Record.graphicsSwitchedOff = graphics.Where((graphic, i) => enabledBefore[i] && !kept.Contains(graphic)).Count();
                state.Record.graphicsKept = kept.Count;
                Frame alone = null;
                try
                {
                    foreach (Behaviour graphic in graphics)
                    {
                        if (!kept.Contains(graphic)) graphic.enabled = false;
                    }
                    yield return Capture(surface, frame => alone = frame);
                }
                finally
                {
                    for (int i = 0; i < graphics.Length; i++) graphics[i].enabled = enabledBefore[i];
                }
                state.OriginalS = judgement.NoteCapture("S " + state.Item.Name, alone, surface);
                if (SameSize(alone, full)) state.MeasureAlone(full, alone);
                else judgement.Add(state.Item.Name, -1, Kinds.Unjudgeable, "S capture is missing or of another size");
                Release(alone);
            }

            yield return null;
            Canvas.ForceUpdateCanvases();
            foreach (LabelState state in states) state.CheckRestored(surface.Camera);
            Frame after = null;
            yield return Capture(surface, frame => after = frame);
            judgement.NoteCapture("after", after, surface);
            record.restoredInkDifferences = SameSize(after, full) ? InkDifferences(full, after) : -1;
            Release(after);
            if (record.restoredInkDifferences != 0)
                judgement.Add("frame", -1, Kinds.Restore, $"after the R and S captures {record.restoredInkDifferences} pixels changed " +
                    "ink against F (-1: no capture of that size)");

            foreach (LabelState state in states) state.Judge(judgement, full, surface.Camera, graphics);
            record.labels = states.Select(state => state.Record).ToArray();
            record.referenceInk = states.Sum(state => state.Record.inkR);
            foreach (LabelState state in states) judgement.AddCrop(state, full, fullOriginal);
            record.missingCharacters = _missingCharacters.ToArray();
            foreach (string missing in record.missingCharacters) judgement.Add("glyph", -1, Kinds.Glyph, missing);
        }

        // One drawn frame of the surface. The Overlay screen is read at the end of a frame (CaptureOverlay); a
        // RenderTexture is drawn on demand once the canvases have rebuilt. Rows run bottom-up, as screen pixels do.
        IEnumerator Capture(Surface surface, Action<Frame> done)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (surface.Texture == null)
            {
                Texture2D shot = null;
                int frame = -1;
                string error = null;
                yield return CaptureOverlay((texture, at, problem) =>
                {
                    shot = texture;
                    frame = at;
                    error = problem;
                });
                surface.LastProblem = shot != null ? error : error ?? "ScreenCapture.CaptureScreenshotAsTexture returned null";
                done(shot != null ? new Frame(shot, frame, $"ScreenCapture.CaptureScreenshotAsTexture at the end of frame {frame}") : null);
                if (surface.LastProblem != null) Debug.Log($"[guild verify capture] {surface.LastProblem}");
                yield break;
            }
            surface.LastProblem = null;
            string method = Render(surface.Camera, surface.Texture);
            var pixels = new Texture2D(surface.Texture.width, surface.Texture.height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = surface.Texture;
            pixels.ReadPixels(new Rect(0, 0, surface.Texture.width, surface.Texture.height), 0, 0);
            pixels.Apply();
            RenderTexture.active = previous;
            done(new Frame(pixels, Time.frameCount, method));
        }

        // ScreenCapture reads the Game view once every camera and Overlay canvas has drawn, so it runs at the end of a
        // frame. The fixture's EventSystem behaviour hosts that one wait; an editor that never reaches the end of a
        // frame leaves the capture missing after WaitSeconds instead of stalling the run, and an exception from the
        // capture itself is kept as its reason.
        IEnumerator CaptureOverlay(Action<Texture2D, int, string> done)
        {
            var host = (MonoBehaviour)_eventSystem.GetComponent(TypeByName("UnityEngine.EventSystems.EventSystem, UnityEngine.UI"));
            bool finished = false;
            Texture2D shot = null;
            int frame = -1;
            string error = null;
            Coroutine running = host.StartCoroutine(EndOfFrameCapture((texture, at, problem) =>
            {
                shot = texture;
                frame = at;
                error = problem;
                finished = true;
            }));
            float deadline = Time.realtimeSinceStartup + WaitSeconds;
            while (!finished && Time.realtimeSinceStartup < deadline) yield return null;
            if (!finished)
            {
                host.StopCoroutine(running);
                error = $"no end of frame within {WaitSeconds}s";
            }
            done(shot, frame, error);
        }

        static IEnumerator EndOfFrameCapture(Action<Texture2D, int, string> done)
        {
            yield return new WaitForEndOfFrame();
            Texture2D shot = null;
            string error = null;
            try { shot = ScreenCapture.CaptureScreenshotAsTexture(); }
            catch (Exception exception) { error = exception.GetType().Name + ": " + exception.Message; }
            done(shot, Time.frameCount, error);
        }

        // Draws the camera once into its texture: the pipeline's render request where it supports one, else
        // Camera.Render. The texture is read back afterwards either way; an empty readback fails the fixture assert.
        static string Render(Camera camera, RenderTexture texture)
        {
            Type pipeline = Type.GetType("UnityEngine.Rendering.RenderPipeline, UnityEngine.CoreModule");
            Type requestType = Type.GetType("UnityEngine.Rendering.RenderPipeline+StandardRequest, UnityEngine.CoreModule");
            MethodInfo supports = pipeline?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "SupportsRenderRequest" && method.IsGenericMethodDefinition);
            MethodInfo submit = pipeline?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "SubmitRenderRequest" && method.IsGenericMethodDefinition);
            string requestOutcome = "no render request API";
            if (requestType != null && supports != null && submit != null)
            {
                try
                {
                    object request = Activator.CreateInstance(requestType);
                    requestType.GetField("destination")?.SetValue(request, texture);
                    bool supported = (bool)supports.MakeGenericMethod(requestType).Invoke(null, new[] { camera, request });
                    if (supported)
                    {
                        submit.MakeGenericMethod(requestType).Invoke(null, new[] { camera, request });
                        return "RenderPipeline.SubmitRenderRequest(StandardRequest)";
                    }
                    requestOutcome = "render request not supported";
                }
                catch (Exception error)
                {
                    requestOutcome = "render request failed: " + (error.InnerException ?? error).GetType().Name;
                }
            }
            camera.Render();
            return "Camera.Render (" + requestOutcome + ")";
        }

        WindowPair Build(string label, GuildStatusDisplay guildValue, BlacksmithDisplay smithValue)
        {
            _missingCharacters.Clear();
            var holder = new GameObject("GuildVerifyRoot_" + label);
            holder.transform.SetParent(_root.transform, worldPositionStays: false);
            var pair = new WindowPair(holder, GuildWindow.BuildRuntime(holder.transform), BlacksmithWindow.BuildRuntime(holder.transform),
                guildValue, smithValue);
            OpenBoth(pair.Guild, pair.Smith, pair.Model);
            return pair;
        }

        // Layout, the canvas scaler and the product's LateUpdate resize settle within frames; uGUI's button colour tint
        // cross-fades over 0.1 s of real time (ColorBlock default), so a capture waits that out as well.
        static IEnumerator Settle()
        {
            yield return Frames(3);
            yield return new WaitForSecondsRealtime(0.25f);
            yield return Frames(2);
        }

        // The product's Overlay canvases on the real screen over a camera that only clears to black, so every capture has
        // the same background (without a camera the editor paints its "No cameras rendering" notice there).
        static Surface OverlaySurface(GameObject holder)
        {
            var cameraObject = new GameObject("GuildVerifyBackgroundCamera");
            cameraObject.transform.SetParent(holder.transform, worldPositionStays: false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            return new Surface(null, null, "ScreenSpaceOverlay on the screen, captured at the end of a frame");
        }

        // The product canvases switched to ScreenSpaceCamera so a camera draws them into a RenderTexture of a chosen pixel
        // size (auxiliary renderMode: the product builds ScreenSpaceOverlay). The holder's destruction ends the switch.
        static Surface RenderTextureSurface(GameObject holder, int width, int height, Component[] windows)
        {
            var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            texture.Create();
            var cameraObject = new GameObject("GuildVerifyCaptureCamera");
            cameraObject.transform.SetParent(holder.transform, worldPositionStays: false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.targetTexture = texture;
            foreach (Canvas canvas in windows.Select(window => window.GetComponent<Canvas>()))
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }
            return new Surface(camera, texture, "ScreenSpaceCamera into a RenderTexture, drawn on demand");
        }

        static void Release(Surface surface)
        {
            if (surface.Camera != null) surface.Camera.targetTexture = null;
            if (surface.Texture == null) return;
            surface.Texture.Release();
            UnityEngine.Object.Destroy(surface.Texture);
        }

        static void Release(Frame frame)
        {
            if (frame != null) UnityEngine.Object.Destroy(frame.Texture);
        }

        // A root canvas beside the product's with the same render mode, camera, plane distance, sorting order and scale
        // factor, so its root transform is the product canvas root's and R can stand where the product text stands.
        static Canvas ReferenceCanvas(Transform parent, Canvas product)
        {
            var canvasObject = new GameObject("GuildVerifyReferenceCanvas", typeof(RectTransform));
            canvasObject.transform.SetParent(parent, worldPositionStays: false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = product.renderMode;
            canvas.worldCamera = product.worldCamera;
            canvas.planeDistance = product.planeDistance;
            canvas.sortingOrder = product.sortingOrder;
            canvas.scaleFactor = product.scaleFactor;
            return canvas;
        }

        // R for one expected text: created on an active object so TMP applies its own settings first, as the product's
        // texts get them, then given the product text's font, material, style, colour, final (auto-sized) font size,
        // alignment, wrapping, overflow, spacing and rectangle, and placed relative to its canvas root as the product
        // text is relative to the product canvas root. It starts switched off.
        static TMP_Text CreateReference(Canvas canvas, LabelExpectation item)
        {
            TMP_Text label = item.Text;
            var referenceObject = new GameObject("GuildVerifyReference_" + label.name, typeof(RectTransform));
            var rect = (RectTransform)referenceObject.transform;
            rect.SetParent(canvas.transform, worldPositionStays: false);
            TMP_Text reference = referenceObject.AddComponent<TextMeshProUGUI>();
            reference.font = label.font;
            reference.fontSharedMaterial = label.fontSharedMaterial;
            reference.fontStyle = label.fontStyle;
            reference.enableAutoSizing = false;
            reference.fontSize = label.fontSize;
            reference.alignment = label.alignment;
            reference.textWrappingMode = label.textWrappingMode;
            reference.overflowMode = label.overflowMode;
            reference.richText = label.richText;
            reference.color = label.color;
            reference.margin = label.margin;
            reference.characterSpacing = label.characterSpacing;
            reference.wordSpacing = label.wordSpacing;
            reference.lineSpacing = label.lineSpacing;
            reference.paragraphSpacing = label.paragraphSpacing;
            reference.extraPadding = label.extraPadding;
            reference.text = item.Expected;
            var source = (RectTransform)label.transform;
            Transform productRoot = label.GetComponentInParent<Canvas>().rootCanvas.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = source.pivot;
            rect.sizeDelta = source.rect.size;
            rect.localPosition = productRoot.InverseTransformPoint(source.position);
            rect.localRotation = Quaternion.Inverse(productRoot.rotation) * source.rotation;
            Vector3 rootScale = productRoot.lossyScale;
            Vector3 labelScale = source.lossyScale;
            rect.localScale = new Vector3(labelScale.x / rootScale.x, labelScale.y / rootScale.y,
                Mathf.Approximately(rootScale.z, 0f) ? 1f : labelScale.z / rootScale.z);
            reference.enabled = false;
            return reference;
        }

        // The texts a player is meant to read in the intended state: every shown, non-empty text of the windows, with
        // the string it shows now. Taken before any damage; CheckModel ties them to the supplied values.
        static LabelExpectation[] Expect(Component[] windows) => windows
            .SelectMany(window => window.GetComponentsInChildren<TMP_Text>(true)
                .Where(text => ScreenUiProbe.IsShown(text) && !string.IsNullOrEmpty(text.text))
                .Select(text => new LabelExpectation(window, text)))
            .ToArray();

        static LabelExpectation Find(LabelExpectation[] expected, Component window, string name)
        {
            LabelExpectation[] found = expected.Where(item => item.Window == window && item.Text.name == name).ToArray();
            Assert.AreEqual(1, found.Length, $"fixture: exactly one expected text named '{name}' in {window.GetType().Name}");
            return found[0];
        }

        // Independent of the product's wording: each supplied value reaches the text that carries it, the required texts
        // are shown, and a result text is shown exactly when the model holds a result.
        static void CheckModel(Judgement judgement, LabelExpectation[] expected, WindowPair pair)
        {
            string Shown(Component window, string name) =>
                expected.FirstOrDefault(item => item.Window == window && item.Text.name == name)?.Expected;
            void Exact(Component window, string name, string value)
            {
                string shown = Shown(window, name);
                if (shown != value)
                    judgement.Add(window.GetType().Name + "/" + name, -1, Kinds.Model, $"shows '{shown ?? "nothing"}', not the supplied '{value}'");
            }
            void Carries(Component window, string name, params long[] values)
            {
                string shown = Shown(window, name);
                long[] numbers = NumbersIn(shown);
                if (shown == null || values.Any(value => !numbers.Contains(value)))
                    judgement.Add(window.GetType().Name + "/" + name, -1, Kinds.Model,
                        $"shows '{shown ?? "nothing"}' without the supplied {string.Join(", ", values)}");
            }
            void Shows(Component window, string name, bool required)
            {
                bool shown = Shown(window, name) != null;
                if (shown != required)
                    judgement.Add(window.GetType().Name + "/" + name, -1, Kinds.Model,
                        required ? "shows nothing although the window needs it" : "shows a text the model has no value for");
            }

            GuildStatusDisplay guild = pair.GuildValue;
            BlacksmithDisplay smith = pair.SmithValue;
            Exact(pair.Guild, "GuildName", guild.GuildName);
            Carries(pair.Guild, "MemberCount", guild.MemberCount);
            Carries(pair.Guild, "OnlineCount", guild.OnlineCount);
            Carries(pair.Guild, "Treasury", guild.Treasury);
            Carries(pair.Guild, "BaseLevel", guild.BaseLevel, guild.MaxBaseLevel);
            if (guild.LevelUp == GuildActionAvailability.MaxLevel) Shows(pair.Guild, "LevelUpCost", true);
            else Carries(pair.Guild, "LevelUpCost", guild.LevelUpCost);
            foreach (string name in new[] { "Title", "AutoJoinNotice", "BlacksmithStatus", "ActionStatus", "Label" })
                Shows(pair.Guild, name, true);
            Shows(pair.Guild, "ActionResult", guild.LevelUpResult != GuildActionResult.None);
            Carries(pair.Smith, "EnhanceLevel", smith.EnhanceLevel, smith.MaxEnhanceLevel);
            Carries(pair.Smith, "AttackBonus", smith.AttackBonus);
            if (smith.Enhance == GuildActionAvailability.MaxLevel)
            {
                Shows(pair.Smith, "GoldCost", true);
                Shows(pair.Smith, "MaterialCost", true);
            }
            else
            {
                Carries(pair.Smith, "GoldCost", smith.GoldCost);
                Carries(pair.Smith, "MaterialCost", smith.MaterialCost);
            }
            foreach (string name in new[] { "Title", "CharacterClass", "ActionStatus", "Label" })
                Shows(pair.Smith, name, true);
            Shows(pair.Smith, "ActionResult", smith.EnhanceResult != GuildActionResult.None);
        }

        static List<string> RaycastProblems(Component[] windows, VisibilityRecord record)
        {
            var problems = new List<string>();
            var seen = new List<string>();
            foreach (Component window in windows)
            {
                Component button = ActionButton(window);
                Vector2 point = ScreenUiProbe.ScreenPoint(button);
                bool hit = ScreenUiProbe.RaycastHits(point).Any(Objects(button).Contains);
                seen.Add($"{window.GetType().Name}/{button.name} at {Format(point)} hit {hit}");
                if (!hit) problems.Add($"{window.GetType().Name}/{button.name} at {Format(point)} takes no raycast");
            }
            record.raycasts = seen.ToArray();
            return problems;
        }

        static string MainPlayModeViewState()
        {
#if UNITY_EDITOR
            MethodInfo main = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.PlayModeView")?
                .GetMethod("GetMainPlayModeView", BindingFlags.NonPublic | BindingFlags.Static);
            if (main == null) return "PlayModeView.GetMainPlayModeView not found";
            object view = main.Invoke(null, null);
            return view != null ? "present (" + view.GetType().FullName + ")" : "none";
#else
            return "not an editor";
#endif
        }

        // A RectMask2D placed between the text and its panel that keeps only part of the text's glyph band (in the
        // text's local units); the text keeps its place on screen. The returned undo puts the text back under its panel
        // at its sibling index and anchored position and removes the mask.
        static Action ClipTo(TMP_Text text, Func<Rect, Rect> keep)
        {
            text.ForceMeshUpdate();
            Rect kept = keep(GlyphBand(text));
            var label = (RectTransform)text.transform;
            Transform parent = label.parent;
            int sibling = label.GetSiblingIndex();
            Vector2 anchored = label.anchoredPosition;
            var clip = new GameObject("GuildVerifyClip", typeof(RectTransform));
            var rect = (RectTransform)clip.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.SetSiblingIndex(sibling);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = Vector2.zero;
            rect.localScale = label.localScale;
            rect.localRotation = label.localRotation;
            rect.sizeDelta = kept.size;
            rect.position = label.TransformPoint(new Vector3(kept.xMin, kept.yMin, 0f));
            clip.AddComponent(TypeByName(Visibility.RectMask2DTypeName + ", UnityEngine.UI"));
            label.SetParent(rect, worldPositionStays: true);
            return () =>
            {
                label.SetParent(parent, worldPositionStays: false);
                label.SetSiblingIndex(sibling);
                label.anchoredPosition = anchored;
                UnityEngine.Object.Destroy(clip);
            };
        }

        static Action Move(RectTransform rect, Vector2 anchoredPosition)
        {
            Vector2 before = rect.anchoredPosition;
            rect.anchoredPosition = anchoredPosition;
            return () => rect.anchoredPosition = before;
        }

        // The union of the text's visible glyph quads in its own local units.
        static Rect GlyphBand(TMP_Text text)
        {
            Vector3[] corners = VisibleCharacters(text)
                .SelectMany(character => new[] { character.vertex_BL.position, character.vertex_TL.position,
                    character.vertex_TR.position, character.vertex_BR.position })
                .ToArray();
            Assert.Greater(corners.Length, 0, $"fixture: {text.name} lays out visible characters");
            return Rect.MinMaxRect(corners.Min(corner => corner.x), corners.Min(corner => corner.y),
                corners.Max(corner => corner.x), corners.Max(corner => corner.y));
        }

        static IEnumerable<TMP_CharacterInfo> VisibleCharacters(TMP_Text text) => text.textInfo.characterInfo
            .Take(text.textInfo.characterCount)
            .Where(character => character.isVisible);

        // Each visible character's glyph quad (TMP's own vertices, padding included) in the camera's pixels; with no
        // camera the Overlay world is the screen.
        static Quad[] Quads(TMP_Text text, Camera camera)
        {
            var quads = new List<Quad>();
            TMP_TextInfo info = text.textInfo;
            for (int index = 0; index < info.characterCount; index++)
            {
                TMP_CharacterInfo character = info.characterInfo[index];
                if (!character.isVisible) continue;
                Vector2[] corners = new[] { character.vertex_BL.position, character.vertex_TL.position, character.vertex_TR.position,
                        character.vertex_BR.position }
                    .Select(local => RectTransformUtility.WorldToScreenPoint(camera, text.transform.TransformPoint(local)))
                    .ToArray();
                quads.Add(new Quad(index, character.character, corners));
            }
            return quads.ToArray();
        }

        // The largest corner distance (per axis) between two equally long quad lists.
        static float Deviation(Quad[] first, Quad[] second)
        {
            float largest = 0f;
            for (int i = 0; i < first.Length; i++)
            {
                for (int corner = 0; corner < 4; corner++)
                {
                    Vector2 difference = first[i].Corners[corner] - second[i].Corners[corner];
                    largest = Mathf.Max(largest, Mathf.Abs(difference.x), Mathf.Abs(difference.y));
                }
            }
            return largest;
        }

        // The whole pixels a quad touches (rounded outward), moved inward by inset and clamped to the frame.
        static RectInt PixelArea(Vector2 min, Vector2 max, int width, int height, int inset)
        {
            int xMin = Mathf.Clamp(Mathf.FloorToInt(min.x) + inset, 0, width);
            int yMin = Mathf.Clamp(Mathf.FloorToInt(min.y) + inset, 0, height);
            int xMax = Mathf.Clamp(Mathf.CeilToInt(max.x) - inset, 0, width);
            int yMax = Mathf.Clamp(Mathf.CeilToInt(max.y) - inset, 0, height);
            return new RectInt(xMin, yMin, Math.Max(0, xMax - xMin), Math.Max(0, yMax - yMin));
        }

        static int CountAtLeast(Frame frame, int level)
        {
            int count = 0;
            for (int y = 0; y < frame.Height; y++)
            {
                for (int x = 0; x < frame.Width; x++)
                {
                    if (frame.Brightest(x, y) >= level) count++;
                }
            }
            return count;
        }

        static int InkIn(Frame frame, RectInt area)
        {
            int ink = 0;
            for (int y = area.yMin; y < area.yMax; y++)
            {
                for (int x = area.xMin; x < area.xMax; x++)
                {
                    if (frame.Brightest(x, y) >= Visibility.InkLevel) ink++;
                }
            }
            return ink;
        }

        static int InkDifferences(Frame first, Frame second)
        {
            int differences = 0;
            for (int y = 0; y < first.Height; y++)
            {
                for (int x = 0; x < first.Width; x++)
                {
                    if ((first.Brightest(x, y) >= Visibility.InkLevel) != (second.Brightest(x, y) >= Visibility.InkLevel)) differences++;
                }
            }
            return differences;
        }

        // How much of a panel's area (two pixels inside its edge) is not black: the panel colour 0.06/0.07/0.10 lights it.
        static float LitFraction(Frame frame, RectTransform panel, Camera camera)
        {
            Rect rect = PixelRect(panel, camera);
            RectInt area = PixelArea(rect.min, rect.max, frame.Width, frame.Height, 2);
            int lit = 0;
            for (int y = area.yMin; y < area.yMax; y++)
            {
                for (int x = area.xMin; x < area.xMax; x++)
                {
                    if (frame.Brightest(x, y) >= Visibility.LitLevel) lit++;
                }
            }
            int total = area.width * area.height;
            return total > 0 ? (float)lit / total : 0f;
        }

        static bool SameSize(Frame frame, Frame full) => frame != null && frame.Width == full.Width && frame.Height == full.Height;

        // Contract supplement v11 section 4, widened to every Overlay judgement (N included) by supplement v12 section 3:
        // the captures of one Overlay judgement that are missing, of another size than the screen's (or N's goal), or not
        // at a later frame than every capture before them. Empty only when each capture came at that size and at a later
        // frame. A RenderTexture is drawn on demand and is not held to this rule.
        static string[] CapturesOff(VisibilityRecord record, int width, int height)
        {
            var off = new List<string>();
            if (record.captureFrames.Length == 0) off.Add("no capture");
            int latest = -1;
            for (int i = 0; i < record.captureFrames.Length; i++)
            {
                if (record.captureWidths[i] != width || record.captureHeights[i] != height || record.captureFrames[i] <= latest)
                    off.Add(record.captures[i]);
                latest = Math.Max(latest, record.captureFrames[i]);
            }
            return off.ToArray();
        }

        static bool IsGraphic(Component component)
        {
            for (Type type = component != null ? component.GetType() : null; type != null; type = type.BaseType)
            {
                if (type.FullName == Visibility.GraphicTypeName) return true;
            }
            return false;
        }

        // What S leaves on: the text, its own TMP sub-meshes (the Korean fallback glyphs) and any Graphic that carries a
        // Mask, whose stencil the text could depend on.
        static HashSet<Behaviour> Kept(TMP_Text text, Behaviour[] graphics)
        {
            var kept = new HashSet<Behaviour>(text.GetComponentsInChildren<TMP_SubMeshUI>(true)) { text };
            foreach (Behaviour graphic in graphics)
            {
                if (graphic.GetComponents<Component>().Any(component => component != null &&
                    component.GetType().FullName == Visibility.MaskTypeName)) kept.Add(graphic);
            }
            return kept;
        }

        // The window's panel: the parent of its title text.
        static RectTransform Panel(Component window) => (RectTransform)Label(window, "Title").transform.parent;

        static IEnumerable<int> Characters(Judgement judgement, string name) =>
            Enumerable.Range(0, judgement.Part(name)?.chars.Length ?? 0);

        static IEnumerable<(CharacterRecord Before, CharacterRecord After)> Pairs(Judgement before, Judgement after, string name)
        {
            CharacterRecord[] first = before.Part(name)?.chars ?? new CharacterRecord[0];
            CharacterRecord[] second = after.Part(name)?.chars ?? new CharacterRecord[0];
            return first.Zip(second, (one, other) => (Before: one, After: other));
        }

        // Writes the judgement's record and each text's R/S/F crop (derived from the originals, top to bottom) next to the
        // run's evidence when the harness names a folder, logs one summary line and frees the frame. The F frame is not
        // written again: pngPath names its original, kept when it was captured. No evidence file is ever overwritten.
        static void Finish(Judgement judgement)
        {
            VisibilityRecord record = judgement.Record;
            record.findings = judgement.Lines;
            record.nearThreshold = judgement.Near.ToArray();
            record.evidencePath = string.Empty;
            string directory = Environment.GetEnvironmentVariable(CaptureDirectoryVariable);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                string basePath = Path.Combine(directory, record.label);
                OriginalRecord frameOriginal = record.originals.FirstOrDefault(original => original.role == "F" &&
                    string.IsNullOrEmpty(original.problem));
                record.pngPath = frameOriginal != null ? Path.Combine(directory, frameOriginal.file) : string.Empty;
                foreach (Crop crop in judgement.Crops)
                {
                    var texture = new Texture2D(crop.Width, crop.Height, TextureFormat.RGBA32, false);
                    texture.SetPixels32(crop.Pixels);
                    texture.Apply();
                    string path = basePath + "." + crop.Name.Replace('/', '-') + ".rsf.png";
                    WriteNew(path, texture.EncodeToPNG());
                    UnityEngine.Object.Destroy(texture);
                    LabelRecord part = judgement.Part(crop.Name);
                    if (part != null) part.cropPath = path;
                }
                record.evidencePath = basePath + ".json";
                WriteNew(record.evidencePath, System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(record, true)));
            }
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "[guild verify {0}] {1}; surface {2}x{3}, Screen {4}x{5}, " +
                "frame {6}x{7}, lit {8}, ink {9}, reference ink {10}, texts {11}, findings {12}, near {13}{14}; evidence '{15}'; " +
                "first findings [{16}]", record.label, record.mode, record.surfaceWidth, record.surfaceHeight, record.screenWidth,
                record.screenHeight, record.frameWidth, record.frameHeight, record.frameLit, record.frameInk, record.referenceInk,
                record.labels.Length, record.findings.Length, record.nearThreshold.Length,
                string.IsNullOrEmpty(record.notReviewed) ? string.Empty : "; " + record.notReviewed, record.evidencePath,
                string.Join(" | ", record.findings.Take(20))));
            if (judgement.FrameTexture != null) UnityEngine.Object.Destroy(judgement.FrameTexture);
            judgement.FrameTexture = null;
        }

        static void WriteNotes(string label, List<string> notes)
        {
            var record = new NotesRecord { label = label, notes = notes.ToArray() };
            Debug.Log($"[guild verify {label}] {string.Join(" | ", record.notes)}");
            string directory = Environment.GetEnvironmentVariable(CaptureDirectoryVariable);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return;
            WriteNew(Path.Combine(directory, label + ".json"), System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(record, true)));
        }

        // A new evidence file: an existing one at that path throws instead of being replaced (contract supplement v12
        // section 3), so a repeated label or a second run into the same folder can never hide earlier evidence.
        static void WriteNew(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        // Upper-case hex, the spelling the harness hashes files with, so a kept original compares with its file directly.
        static string Sha256(byte[] bytes)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty);
            }
        }

        static void OpenBoth(GuildWindow guild, BlacksmithWindow smith, GuildDisplayModel model)
        {
            guild.Bind(model);
            smith.Bind(model);
            guild.SetOpen(true);
            smith.SetOpen(true);
            guild.SetInputAllowed(true);
            smith.SetInputAllowed(true);
        }

        // The approved World scenario (World goal 2026-10-14-guild-server 「승인 A」, as GuildWindowSceneTests uses it):
        // one guild of two online members, level 1 of 2, a 30 level-up; the knight's step 0 -> 1 for 20 gold and 1 Material.
        static GuildStatusDisplay ApprovedGuild(long treasury, string name = "새벽 길드",
            GuildActionAvailability levelUp = GuildActionAvailability.Available) =>
            new GuildStatusDisplay(guildName: name, memberCount: 2, onlineCount: 2, treasury: treasury, baseLevel: 1,
                maxBaseLevel: 2, blacksmithUnlocked: false, levelUpCost: 30, levelUp: levelUp, levelUpPending: false,
                levelUpResult: GuildActionResult.None);

        static BlacksmithDisplay ApprovedSmith(long gold) =>
            new BlacksmithDisplay(characterClass: CharacterClass.Knight, enhanceLevel: 0, maxEnhanceLevel: 1, attackBonus: 0,
                goldCost: gold, materialCost: 1, enhance: GuildActionAvailability.Available, enhancePending: false,
                enhanceResult: GuildActionResult.None);

        static GuildStatusDisplay UpperBoundGuild() =>
            new GuildStatusDisplay(guildName: "가나다라마바사아자차카타파하거너", memberCount: int.MaxValue,
                onlineCount: int.MaxValue - 1, treasury: int.MaxValue, baseLevel: byte.MaxValue - 1, maxBaseLevel: byte.MaxValue,
                blacksmithUnlocked: true, levelUpCost: int.MaxValue - 2, levelUp: GuildActionAvailability.Available,
                levelUpPending: true, levelUpResult: GuildActionResult.Refused);

        static BlacksmithDisplay UpperBoundSmith() =>
            new BlacksmithDisplay(characterClass: CharacterClass.Mage, enhanceLevel: byte.MaxValue - 1,
                maxEnhanceLevel: byte.MaxValue, attackBonus: int.MaxValue, goldCost: int.MaxValue - 3,
                materialCost: int.MaxValue - 4, enhance: GuildActionAvailability.Available, enhancePending: true,
                enhanceResult: GuildActionResult.Refused);

        static int Subscribers(GuildDisplayModel model)
        {
            // A field-like event keeps its delegate in a private field of the same name (as GuildSceneWindow reads it).
            FieldInfo field = typeof(GuildDisplayModel).GetField("Changed", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "fixture: GuildDisplayModel.Changed is a field-like event");
            return (field.GetValue(model) as Delegate)?.GetInvocationList().Length ?? 0;
        }

        // GuildWindowSceneTests' private readability helper, called as written so the control tests that code, not a copy.
        static string[] PriorUnreadable(TMP_Text label)
        {
            MethodInfo helper = typeof(GuildWindowSceneTests).GetMethod("Unreadable", BindingFlags.NonPublic | BindingFlags.Static,
                null, new[] { typeof(TMP_Text) }, null);
            Assert.IsNotNull(helper, "fixture: GuildWindowSceneTests.Unreadable(TMP_Text) exists");
            return ((IEnumerable<string>)helper.Invoke(null, new object[] { label })).ToArray();
        }

        static void OpenOne(Component window, GuildDisplayModel model)
        {
            if (window is GuildWindow guild)
            {
                guild.Bind(model);
                guild.SetOpen(true);
                guild.SetInputAllowed(true);
                return;
            }
            var smith = (BlacksmithWindow)window;
            smith.Bind(model);
            smith.SetOpen(true);
            smith.SetInputAllowed(true);
        }

        void RecordMissingCharacter(int unicode, int stringIndex, string text, TMP_FontAsset fontAsset, TMP_Text textComponent)
        {
            // The event is global; only texts of this test's windows count.
            if (_root == null || textComponent == null || !textComponent.transform.IsChildOf(_root.transform)) return;
            _missingCharacters.Add(string.Format(CultureInfo.InvariantCulture, "U+{0:X4} at {1} in {2} '{3}' ({4})", unicode,
                stringIndex, textComponent.name, text, fontAsset != null ? fontAsset.name : "no font"));
        }

        static Component ActionButton(Component window)
        {
            string name = window is GuildWindow ? "LevelUp" : "Enhance";
            Component[] found = window.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name)
                .SelectMany(item => item.GetComponents<Component>())
                .Where(component => component != null && component.GetType().FullName == ButtonTypeName)
                .ToArray();
            Assert.AreEqual(1, found.Length, $"fixture: exactly one Button named '{name}' (api-contract 3.1)");
            return found[0];
        }

        static TMP_Text Label(Component window, string name)
        {
            TMP_Text[] found = window.GetComponentsInChildren<TMP_Text>(true).Where(text => text.name == name).ToArray();
            Assert.AreEqual(1, found.Length, $"fixture: exactly one TMP_Text named '{name}' (api-contract 3.1)");
            return found[0];
        }

        static HashSet<GameObject> Objects(Component owner) =>
            new HashSet<GameObject>(owner.GetComponentsInChildren<Transform>(true).Select(item => item.gameObject));

        static string LaidOut(TMP_Text text) => new string(text.textInfo.characterInfo
            .Take(text.textInfo.characterCount)
            .Select(character => character.character)
            .ToArray());

        static long[] NumbersIn(string text) => Number.Matches(text ?? string.Empty)
            .Cast<Match>()
            .Select(match => long.Parse(new string(match.Value.Where(char.IsDigit).ToArray()), CultureInfo.InvariantCulture))
            .ToArray();

        // World corners in the camera's pixels; with no camera the Overlay world is the screen.
        static Rect PixelRect(RectTransform rect, Camera camera)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return PixelRectOf(corners, camera);
        }

        static Rect PixelRectOf(Vector3[] world, Camera camera)
        {
            Vector2[] screen = world.Select(point => RectTransformUtility.WorldToScreenPoint(camera, point)).ToArray();
            return Rect.MinMaxRect(screen.Min(point => point.x), screen.Min(point => point.y),
                screen.Max(point => point.x), screen.Max(point => point.y));
        }

        static bool Inside(Rect inner, Rect outer, float tolerance) =>
            inner.xMin >= outer.xMin - tolerance && inner.yMin >= outer.yMin - tolerance &&
            inner.xMax <= outer.xMax + tolerance && inner.yMax <= outer.yMax + tolerance;

        static string Format(Vector2 point) => string.Format(CultureInfo.InvariantCulture, "({0:F1}, {1:F1})", point.x, point.y);

        static string Format(Rect rect) => string.Format(CultureInfo.InvariantCulture, "[{0:F1},{1:F1} - {2:F1},{3:F1}]",
            rect.xMin, rect.yMin, rect.xMax, rect.yMax);

        static string Position(Rect rect) => string.Format(CultureInfo.InvariantCulture, "({0:F1}, {1:F1}, {2:F1} x {3:F1})",
            rect.x, rect.y, rect.width, rect.height);

        static string Number3(float value) => value.ToString("F3", CultureInfo.InvariantCulture);

        static string DescribeRoot(Transform root) => string.Format(CultureInfo.InvariantCulture,
            "position ({0:F3}, {1:F3}, {2:F3}) scale ({3:F5}, {4:F5}, {5:F5}) rotation {6}", root.position.x, root.position.y,
            root.position.z, root.lossyScale.x, root.lossyScale.y, root.lossyScale.z, root.rotation.eulerAngles);

        // Two canvas roots stand at the same place with the same scale and turn (a hundredth of a unit, of a degree).
        static bool SamePlace(Transform first, Transform second) =>
            (first.position - second.position).magnitude <= 0.01f &&
            (first.lossyScale - second.lossyScale).magnitude <= 0.0001f &&
            Quaternion.Angle(first.rotation, second.rotation) <= 0.01f;

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        static Type TypeByName(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName);
            Assert.IsNotNull(type, "fixture: type " + assemblyQualifiedName);
            return type;
        }

        // Fixed before any v4 run (readiness-v4) and never tuned to a result. Ink is a pixel whose brightest channel
        // reaches InkLevel: the white and gold texts on the dark panel do, while the panel (0.06/0.07/0.10) and button
        // (0.20/0.35/0.48) colours stay below it. Below DarkLevel the text alone drew no ink there (its own anti-aliased
        // edge over the panel cannot reach InkLevel from there). R and S are the same glyph mesh at the same place, so in
        // an undamaged frame S equals R and F∩S equals S; the ratios leave room for single-pixel rasterisation noise only,
        // with one pixel of slack so a 4-pixel full stop or middle dot (static/glyph-ink-probe-1.json) is not judged by
        // noise. A ratio below the Near values or S above R by more than that is listed for review before the next root.
        static class Visibility
        {
            internal const int InkLevel = 150;
            internal const int DarkLevel = 40;
            internal const int LitLevel = 8;
            internal const float MinimumSOverR = 0.90f;
            internal const float MaximumSOverR = 1.10f;
            internal const float MinimumFOverS = 0.95f;
            internal const float NearSOverR = 0.95f;
            internal const float NearFOverS = 0.98f;
            internal const float QuadTolerance = 0.5f;
            internal const float StructureTolerance = 1f;
            internal const float PanelNotDrawn = 0.05f;
            internal const string GraphicTypeName = "UnityEngine.UI.Graphic";
            internal const string RectMask2DTypeName = "UnityEngine.UI.RectMask2D";
            internal const string MaskTypeName = "UnityEngine.UI.Mask";
        }

        // The kinds of finding Judge and CheckModel report.
        static class Kinds
        {
            internal const string Cut = "cut";                 // S has less than MinimumSOverR of R's ink
            internal const string Covered = "covered";         // F shows less than MinimumFOverS of S's ink
            internal const string Foreign = "foreign";         // F ink where the text alone is dark
            internal const string Unjudgeable = "unjudgeable"; // a capture, R or R's alignment is missing
            internal const string Text = "text";               // the shown or laid-out text is not the expected one
            internal const string Structure = "structure";     // off the surface, outside a RectMask2D, overflow, visibility limits
            internal const string Glyph = "glyph";             // TMP's missing-character event
            internal const string Model = "model";             // the expected text does not carry the supplied value
            internal const string Restore = "restore";         // the judgement's own switching left the frame or layout changed
        }

        // The two product windows of one case under one holder, open on one model with the supplied values.
        sealed class WindowPair
        {
            internal readonly GameObject Holder;
            internal readonly GuildWindow Guild;
            internal readonly BlacksmithWindow Smith;
            internal readonly GuildDisplayModel Model = new GuildDisplayModel();
            internal readonly GuildStatusDisplay GuildValue;
            internal readonly BlacksmithDisplay SmithValue;

            internal WindowPair(GameObject holder, GuildWindow guild, BlacksmithWindow smith, GuildStatusDisplay guildValue,
                BlacksmithDisplay smithValue)
            {
                Holder = holder;
                Guild = guild;
                Smith = smith;
                GuildValue = guildValue;
                SmithValue = smithValue;
                Model.SetGuild(guildValue);
                Model.SetBlacksmith(smithValue);
            }

            internal Component[] Both => new Component[] { Guild, Smith };
        }

        // Where a judgement draws: the real screen (no camera and no texture) or a RenderTexture a camera draws into.
        sealed class Surface
        {
            internal readonly Camera Camera;
            internal readonly RenderTexture Texture;
            internal readonly string Mode;
            internal string LastProblem;

            internal Surface(Camera camera, RenderTexture texture, string mode)
            {
                Camera = camera;
                Texture = texture;
                Mode = mode;
            }

            internal int Width => Texture != null ? Texture.width : Screen.width;
            internal int Height => Texture != null ? Texture.height : Screen.height;
        }

        // One captured frame and its pixels.
        sealed class Frame
        {
            internal readonly Texture2D Texture;
            internal readonly Color32[] Pixels;
            internal readonly int Width;
            internal readonly int Height;
            internal readonly int FrameCount;
            internal readonly string Method;

            internal Frame(Texture2D texture, int frameCount, string method)
            {
                Texture = texture;
                Pixels = texture.GetPixels32();
                Width = texture.width;
                Height = texture.height;
                FrameCount = frameCount;
                Method = method;
            }

            internal int Brightest(int x, int y)
            {
                Color32 color = Pixels[y * Width + x];
                return Math.Max(color.r, Math.Max(color.g, color.b));
            }
        }

        // A text the player is meant to read, named by its window and its path under the window, with the string it
        // showed in the intended state.
        sealed class LabelExpectation
        {
            internal readonly Component Window;
            internal readonly TMP_Text Text;
            internal readonly string Name;
            internal readonly string Expected;

            internal LabelExpectation(Component window, TMP_Text text)
            {
                Window = window;
                Text = text;
                Name = window.GetType().Name + "/" + ScreenUiProbe.PathOf(text.transform, window.transform);
                Expected = text.text;
            }
        }

        sealed class Quad
        {
            internal readonly int Index;
            internal readonly char Character;
            internal readonly Vector2[] Corners;
            internal readonly Vector2 Min;
            internal readonly Vector2 Max;

            internal Quad(int index, char character, Vector2[] corners)
            {
                Index = index;
                Character = character;
                Corners = corners;
                Min = new Vector2(corners.Min(corner => corner.x), corners.Min(corner => corner.y));
                Max = new Vector2(corners.Max(corner => corner.x), corners.Max(corner => corner.y));
            }

            internal string Describe() => Format(Rect.MinMaxRect(Min.x, Min.y, Max.x, Max.y));
        }

        sealed class Finding
        {
            internal readonly string Name;
            internal readonly int Character;
            internal readonly string Kind;
            internal readonly string Detail;

            internal Finding(string name, int character, string kind, string detail)
            {
                Name = name;
                Character = character;
                Kind = kind;
                Detail = detail;
            }

            public override string ToString() => Character < 0 ? $"{Kind} {Name}: {Detail}" : $"{Kind} {Name} #{Character}: {Detail}";
        }

        sealed class Crop
        {
            internal readonly string Name;
            internal readonly int Width;
            internal readonly int Height;
            internal readonly Color32[] Pixels;

            internal Crop(string name, int width, int height, Color32[] pixels)
            {
                Name = name;
                Width = width;
                Height = height;
                Pixels = pixels;
            }
        }

        // The findings, review notes and evidence of one Judge call.
        sealed class Judgement
        {
            internal readonly VisibilityRecord Record;
            internal readonly List<Finding> Findings = new List<Finding>();
            internal readonly List<string> Near = new List<string>();
            internal readonly List<Crop> Crops = new List<Crop>();
            internal Texture2D FrameTexture;

            internal Judgement(string label, string mode)
            {
                Record = new VisibilityRecord { label = label, mode = mode, labels = new LabelRecord[0], panelLit = new float[0],
                    captures = new string[0], captureFrames = new int[0], captureWidths = new int[0], captureHeights = new int[0],
                    originals = new OriginalRecord[0] };
            }

            internal string[] Lines => Findings.Select(finding => finding.ToString()).ToArray();

            // One entry per capture, in the order taken (its role, frame and size; -1 when none came), and the capture's
            // whole frame kept as it was read (contract supplement v12 section 3, D-CAPTURE-01): a new flat PNG
            // "<label>.<overlay|rendertexture>-<sequence>-<F|R|S|after>.png" in the harness's capture folder, written before
            // the frame is released, read back and hashed. Returns the capture's 1-based sequence. A capture that never came
            // keeps its reason (the caller already fails the judgement for it); a frame that could not be written, read
            // back or hashed, or a run without a capture folder, is a finding here, so the judgement cannot pass without
            // its originals.
            internal int NoteCapture(string role, Frame frame, Surface surface)
            {
                Record.captures = Record.captures.Append(frame != null
                    ? $"{role}: frame {frame.FrameCount}, {frame.Width}x{frame.Height}" : $"{role}: none").ToArray();
                Record.captureFrames = Record.captureFrames.Append(frame?.FrameCount ?? -1).ToArray();
                Record.captureWidths = Record.captureWidths.Append(frame?.Width ?? -1).ToArray();
                Record.captureHeights = Record.captureHeights.Append(frame?.Height ?? -1).ToArray();
                int sequence = Record.captures.Length;
                var original = new OriginalRecord
                {
                    sequence = sequence,
                    role = role,
                    source = surface.Texture == null ? "overlay" : "rendertexture",
                    frame = frame?.FrameCount ?? -1,
                    width = frame?.Width ?? -1,
                    height = frame?.Height ?? -1,
                    file = string.Empty,
                    sha256 = string.Empty,
                    problem = string.Empty,
                };
                if (frame == null) original.problem = "no capture: " + (surface.LastProblem ?? "no reason given");
                else
                {
                    try { KeepOriginal(original, frame); }
                    catch (Exception error) { original.problem = error.GetType().Name + ": " + error.Message; }
                    if (!string.IsNullOrEmpty(original.problem))
                        Add("evidence", -1, Kinds.Unjudgeable, $"original {sequence} ({role}) not kept: {original.problem}");
                }
                Record.originals = Record.originals.Append(original).ToArray();
                return sequence;
            }

            internal void Add(string name, int character, string kind, string detail) =>
                Findings.Add(new Finding(name, character, kind, detail));

            internal bool Reports(string name, string kind) => Findings.Any(finding => finding.Name == name && finding.Kind == kind);

            internal bool Reports(string name, int character, string kind) =>
                Findings.Any(finding => finding.Name == name && finding.Character == character && finding.Kind == kind);

            internal bool Clean(string name, int character) =>
                !Findings.Any(finding => finding.Name == name && finding.Character == character);

            internal LabelRecord Part(string name) => Record.labels.FirstOrDefault(part => part.name == name);

            // R above S above F, each cropped to the text's quads and four pixels around them, split by magenta rows. A
            // derived picture for reading, never an original: the record names the originals and the F pixel area it
            // was cut from.
            internal void AddCrop(LabelState state, Frame full, int fullOriginal)
            {
                if (state.CropR == null || state.CropS == null || state.CropArea.width == 0 || state.CropArea.height == 0) return;
                RectInt area = state.CropArea;
                state.Record.cropDerivedFrom = string.Format(CultureInfo.InvariantCulture, "derived, not an original: rows of " +
                    "originals F {0}, S {1} and R {2} over F pixels x {3}, y {4} (from the bottom), {5}x{6}", fullOriginal,
                    state.OriginalS, state.OriginalR, area.x, area.y, area.width, area.height);
                Color32[] cropF = CropPixels(full, area);
                int height = area.height * 3 + 4;
                var pixels = new Color32[area.width * height];
                var separator = new Color32(255, 0, 255, 255);
                for (int i = 0; i < pixels.Length; i++) pixels[i] = separator;
                Color32[][] bands = { cropF, state.CropS, state.CropR };
                for (int band = 0; band < bands.Length; band++)
                {
                    int top = band * (area.height + 2);
                    Array.Copy(bands[band], 0, pixels, top * area.width, bands[band].Length);
                }
                Crops.Add(new Crop(state.Item.Name, area.width, height, pixels));
            }

            void KeepOriginal(OriginalRecord original, Frame frame)
            {
                string directory = Environment.GetEnvironmentVariable(CaptureDirectoryVariable);
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                    throw new DirectoryNotFoundException($"no capture folder ({CaptureDirectoryVariable})");
                string file = string.Format(CultureInfo.InvariantCulture, "{0}.{1}-{2:D3}-{3}.png", Record.label, original.source,
                    original.sequence, original.role.Split(' ')[0]);
                string path = Path.Combine(directory, file);
                byte[] png = frame.Texture.EncodeToPNG();
                WriteNew(path, png);
                byte[] read = File.ReadAllBytes(path);
                string written = Sha256(png);
                if (read.Length != png.Length || Sha256(read) != written) throw new IOException($"{file} read back differs from the bytes written");
                original.file = file;
                original.bytes = read.Length;
                original.sha256 = written;
            }
        }

        // One expected text through a Judge call: its glyph quads at F, R's quads, the per-character counts and crops.
        sealed class LabelState
        {
            internal readonly LabelExpectation Item;
            internal readonly LabelRecord Record;
            internal readonly Quad[] LabelQuads;
            internal Quad[] ReferenceQuads = new Quad[0];
            internal TMP_Text Reference;
            internal RectInt CropArea;
            internal Color32[] CropR;
            internal Color32[] CropS;
            internal int OriginalR = -1;
            internal int OriginalS = -1;

            internal LabelState(LabelExpectation item, Camera camera)
            {
                Item = item;
                TMP_Text text = item.Text;
                LabelQuads = Quads(text, camera);
                Record = new LabelRecord
                {
                    window = item.Window.GetType().Name,
                    name = item.Name,
                    expected = item.Expected,
                    text = text.text,
                    laidOut = LaidOut(text),
                    fontSize = text.fontSize,
                    characters = LabelQuads.Length,
                    overflowing = text.isTextOverflowing,
                    box = Format(PixelRect((RectTransform)text.transform, camera)),
                    maxQuadDeviation = -1f,
                    restoredQuadDeviation = -1f,
                    rectMasks = new string[0],
                    causes = new string[0],
                    chars = new CharacterRecord[0],
                };
            }

            internal void MeasureReference(Frame alone)
            {
                Record.referenceFontSize = Reference.fontSize;
                Record.referenceCharacters = ReferenceQuads.Length;
                bool comparable = Item.Text.text == Item.Expected && ReferenceQuads.Length == LabelQuads.Length;
                if (comparable) Record.maxQuadDeviation = Deviation(LabelQuads, ReferenceQuads);
                Record.chars = ReferenceQuads.Select((quad, i) => new CharacterRecord
                {
                    index = quad.Index,
                    character = quad.Character.ToString(),
                    referenceQuad = quad.Describe(),
                    quad = comparable ? LabelQuads[i].Describe() : string.Empty,
                    r = InkIn(alone, PixelArea(quad.Min, quad.Max, alone.Width, alone.Height, 0)),
                }).ToArray();
                Record.inkR = Record.chars.Sum(character => character.r);
                if (ReferenceQuads.Length == 0) return;
                CropArea = PixelArea(new Vector2(ReferenceQuads.Min(quad => quad.Min.x) - 4f, ReferenceQuads.Min(quad => quad.Min.y) - 4f),
                    new Vector2(ReferenceQuads.Max(quad => quad.Max.x) + 4f, ReferenceQuads.Max(quad => quad.Max.y) + 4f),
                    alone.Width, alone.Height, 0);
                CropR = CropPixels(alone, CropArea);
            }

            internal void MeasureAlone(Frame full, Frame alone)
            {
                for (int i = 0; i < Record.chars.Length; i++)
                {
                    CharacterRecord character = Record.chars[i];
                    Quad quad = ReferenceQuads[i];
                    Count(full, alone, PixelArea(quad.Min, quad.Max, full.Width, full.Height, 0),
                        PixelArea(quad.Min, quad.Max, full.Width, full.Height, 1), character);
                    character.sOverR = character.r > 0 ? (float)character.s / character.r : 0f;
                    character.fsOverS = character.s > 0 ? (float)character.fs / character.s : 0f;
                }
                Record.inkS = Record.chars.Sum(character => character.s);
                Record.inkF = Record.chars.Sum(character => character.f);
                Record.inkFS = Record.chars.Sum(character => character.fs);
                Record.foreign = Record.chars.Sum(character => character.foreign);
                if (CropR != null) CropS = CropPixels(alone, CropArea);
            }

            internal void CheckRestored(Camera camera)
            {
                Item.Text.ForceMeshUpdate();
                Record.restoredFontSize = Item.Text.fontSize;
                Quad[] now = Quads(Item.Text, camera);
                Record.restoredQuadDeviation = now.Length == LabelQuads.Length ? Deviation(LabelQuads, now) : -2f;
            }

            internal void Judge(Judgement judgement, Frame full, Camera camera, Behaviour[] graphics)
            {
                string name = Item.Name;
                TMP_Text text = Item.Text;
                if (text.text != Item.Expected)
                    judgement.Add(name, -1, Kinds.Text, $"shows '{text.text}', not the expected '{Item.Expected}'");
                else if (Record.laidOut != Item.Expected)
                    judgement.Add(name, -1, Kinds.Text, $"lays out '{Record.laidOut}' for '{Item.Expected}'");
                else if (Record.referenceCharacters != Record.characters)
                    judgement.Add(name, -1, Kinds.Unjudgeable, $"R lays out {Record.referenceCharacters} visible characters, the text " +
                        $"{Record.characters}");
                else if (Record.maxQuadDeviation < 0f || Record.maxQuadDeviation > Visibility.QuadTolerance)
                    judgement.Add(name, -1, Kinds.Unjudgeable, $"R's glyph quads are {Number3(Record.maxQuadDeviation)}px off the " +
                        "text's (-1: not compared)");
                if (Record.chars.Length == 0) judgement.Add(name, -1, Kinds.Unjudgeable, "R gives no visible character to judge");
                if (Record.overflowing) judgement.Add(name, -1, Kinds.Structure, "overflows its box");
                TMP_TextInfo info = text.textInfo;
                if (text.firstVisibleCharacter != 0 || text.maxVisibleCharacters < info.characterCount || text.maxVisibleLines < info.lineCount)
                    judgement.Add(name, -1, Kinds.Structure, $"shows from character {text.firstVisibleCharacter}, at most " +
                        $"{text.maxVisibleCharacters} characters and {text.maxVisibleLines} lines of {info.characterCount}/{info.lineCount}");
                if (!Mathf.Approximately(Record.restoredFontSize, Record.fontSize) || Record.restoredQuadDeviation < 0f ||
                    Record.restoredQuadDeviation > Visibility.QuadTolerance)
                    judgement.Add(name, -1, Kinds.Restore, $"after the captures its font size is {Number3(Record.restoredFontSize)} " +
                        $"(was {Number3(Record.fontSize)}) and its quads moved {Number3(Record.restoredQuadDeviation)}px (-2: count changed)");

                var surface = new Rect(0f, 0f, full.Width, full.Height);
                (string Name, Rect Rect)[] masks = text.GetComponentsInParent<Behaviour>(true)
                    .Where(component => component.enabled && component.GetType().FullName == Visibility.RectMask2DTypeName)
                    .Select(mask => (Name: mask.name, Rect: PixelRect((RectTransform)mask.transform, camera)))
                    .ToArray();
                Record.rectMasks = masks.Select(mask => mask.Name + " " + Format(mask.Rect)).ToArray();
                for (int i = 0; i < Record.chars.Length; i++)
                {
                    CharacterRecord character = Record.chars[i];
                    Quad quad = ReferenceQuads[i];
                    Rect rect = Rect.MinMaxRect(quad.Min.x, quad.Min.y, quad.Max.x, quad.Max.y);
                    string at = $"'{character.character}' {character.referenceQuad}";
                    if (!Inside(rect, surface, Visibility.StructureTolerance))
                        judgement.Add(name, i, Kinds.Structure, $"{at} leaves the {full.Width}x{full.Height} surface");
                    foreach ((string maskName, Rect maskRect) in masks)
                    {
                        if (!Inside(rect, maskRect, Visibility.StructureTolerance))
                            judgement.Add(name, i, Kinds.Structure, $"{at} leaves RectMask2D {maskName} {Format(maskRect)}");
                    }
                    string counts = $"{at} R {character.r}, S {character.s}, F {character.f}, F∩S {character.fs}, foreign {character.foreign}";
                    if (character.r == 0)
                    {
                        judgement.Add(name, i, Kinds.Unjudgeable, counts + ": R draws no ink in the quad");
                        continue;
                    }
                    if (character.r < 4)
                        judgement.Near.Add($"{name} #{i} R has only {character.r} ink pixels, too few for S/R to decide: {counts}");
                    if (character.s < character.r * Visibility.MinimumSOverR - 1f) judgement.Add(name, i, Kinds.Cut, counts);
                    else if (character.s > character.r * Visibility.MaximumSOverR + 1f)
                        judgement.Add(name, i, Kinds.Unjudgeable, counts + ": S has more ink than R");
                    else if (character.sOverR < Visibility.NearSOverR || character.sOverR > 2f - Visibility.NearSOverR)
                        judgement.Near.Add($"{name} #{i} S/R {Number3(character.sOverR)}: {counts}");
                    if (character.s > 0 && character.fs < character.s * Visibility.MinimumFOverS - 1f)
                        judgement.Add(name, i, Kinds.Covered, counts);
                    else if (character.s > 0 && character.fsOverS < Visibility.NearFOverS)
                        judgement.Near.Add($"{name} #{i} F∩S/S {Number3(character.fsOverS)}: {counts}");
                    if (character.foreign > 0) judgement.Add(name, i, Kinds.Foreign, counts);
                }
                if (judgement.Findings.Any(finding => finding.Name == name && (finding.Kind == Kinds.Covered || finding.Kind == Kinds.Foreign)))
                    Record.causes = Causes(graphics, camera);
            }

            // The other enabled Graphics whose rectangles meet the text's quads: listed as possible causes only.
            string[] Causes(Behaviour[] graphics, Camera camera)
            {
                if (ReferenceQuads.Length == 0) return new string[0];
                Rect union = Rect.MinMaxRect(ReferenceQuads.Min(quad => quad.Min.x), ReferenceQuads.Min(quad => quad.Min.y),
                    ReferenceQuads.Max(quad => quad.Max.x), ReferenceQuads.Max(quad => quad.Max.y));
                var own = new HashSet<Behaviour>(Item.Text.GetComponentsInChildren<TMP_SubMeshUI>(true)) { Item.Text };
                return graphics
                    .Where(graphic => graphic.enabled && graphic.gameObject.activeInHierarchy && !own.Contains(graphic) &&
                        PixelRect((RectTransform)graphic.transform, camera).Overlaps(union))
                    .Select(graphic => $"{graphic.GetType().Name} {graphic.name} (canvas order " +
                        $"{graphic.GetComponentInParent<Canvas>()?.sortingOrder}, sibling {graphic.transform.GetSiblingIndex()})")
                    .ToArray();
            }

            static void Count(Frame full, Frame alone, RectInt area, RectInt inner, CharacterRecord character)
            {
                for (int y = area.yMin; y < area.yMax; y++)
                {
                    for (int x = area.xMin; x < area.xMax; x++)
                    {
                        int frameInk = full.Brightest(x, y);
                        int textInk = alone.Brightest(x, y);
                        bool text = textInk >= Visibility.InkLevel;
                        bool frame = frameInk >= Visibility.InkLevel;
                        if (text) character.s++;
                        if (frame) character.f++;
                        if (text && frame) character.fs++;
                        if (frame && textInk < Visibility.DarkLevel && x >= inner.xMin && x < inner.xMax && y >= inner.yMin && y < inner.yMax)
                            character.foreign++;
                    }
                }
            }
        }

        static Color32[] CropPixels(Frame frame, RectInt area)
        {
            var pixels = new Color32[area.width * area.height];
            for (int y = 0; y < area.height; y++)
                Array.Copy(frame.Pixels, (area.yMin + y) * frame.Width + area.xMin, pixels, y * area.width, area.width);
            return pixels;
        }

#if UNITY_EDITOR
        // The editor's main play mode view, reached through internal members of the installed 6000.6.4f1
        // UnityEditor.CoreModule.dll (SHA 03391DC36F8781867267ECC3EC625FE6371041E8324D3F9318C6440593B1BFF0; IL in
        // static/gameview-metadata-probe-5.json, -11.json and -12.json). Sizes are read from the current GameViewSizes
        // group through getters that only read fields (GetTotalCount, GetGameViewSize, width, height, sizeType, baseText).
        // An existing item is selected with GameView.SizeSelectionCallback, whose IL sets selectedSizeIndex, updates the
        // zoom area, sets PlayModeView.targetSize from targetRenderSize and calls no SaveToHDD; the public
        // PlayModeWindow.SetCustomRenderingResolution, which reaches GameViewSizes.SaveToHDD, is not used. Free Aspect
        // sizing is no longer used. What native code saves is not shown by those bytes, so the harness
        // compares the user Preferences files and the EditorPrefs registry values around the N root (contract supplement
        // v9 sections 4 and 5). Only the N cases open this (contract supplement v12 section 3): when the editor, GUI or
        // batch, has no main play mode view, a GameView is created (its constructor registers it as the main view). The
        // default-screen cases never come here; they read the existing view and neither create nor size one. Release
        // returns the original selection, checks unchanged position, removes its own added size and closes a created view.
        sealed class MainPlayModeView
        {
            const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            readonly UnityEditor.EditorWindow _view;
            readonly bool _created;
            readonly PropertyInfo _targetSize;
            readonly PropertyInfo _selectedSizeIndex;
            readonly MethodInfo _sizeSelection;
            readonly MethodInfo _main;
            readonly Vector2 _originalSize;
            readonly Rect _originalPosition;
            readonly int _originalIndex;
            readonly MemorySizeList _memory;
            int _ownedIndex;
            bool _indexChanged;
            bool _released;

            MainPlayModeView(UnityEditor.EditorWindow view, bool created, PropertyInfo targetSize, PropertyInfo selectedSizeIndex,
                MethodInfo sizeSelection, MethodInfo main)
            {
                _view = view;
                _created = created;
                _targetSize = targetSize;
                _selectedSizeIndex = selectedSizeIndex;
                _sizeSelection = sizeSelection;
                _main = main;
                _originalSize = TargetSize;
                _originalPosition = view.position;
                _originalIndex = SelectedIndex;
                _ownedIndex = _originalIndex;
                _memory = MemorySizeList.Open(_originalIndex);
            }

            Vector2 TargetSize => (Vector2)_targetSize.GetValue(_view);

            int SelectedIndex => (int)_selectedSizeIndex.GetValue(_view);

            internal static MainPlayModeView Open(List<string> notes)
            {
                MemorySizeList.CheckPermit();
                Assembly editor = typeof(UnityEditor.EditorWindow).Assembly;
                Type playModeView = editor.GetType("UnityEditor.PlayModeView");
                Type gameView = editor.GetType("UnityEditor.GameView");
                Assert.IsNotNull(playModeView, "fixture: UnityEditor.PlayModeView exists in this editor");
                Assert.IsNotNull(gameView, "fixture: UnityEditor.GameView exists in this editor");
                MethodInfo main = playModeView.GetMethod("GetMainPlayModeView", BindingFlags.NonPublic | BindingFlags.Static);
                PropertyInfo targetSize = playModeView.GetProperty("targetSize", BindingFlags.NonPublic | BindingFlags.Instance);
                PropertyInfo selectedSizeIndex = gameView.GetProperty("selectedSizeIndex", AnyInstance);
                MethodInfo sizeSelection = gameView.GetMethod("SizeSelectionCallback", AnyInstance, null,
                    new[] { typeof(int), typeof(object) }, null);
                Assert.IsNotNull(main, "fixture: PlayModeView.GetMainPlayModeView exists in this editor");
                Assert.IsNotNull(targetSize, "fixture: PlayModeView.targetSize exists in this editor");
                Assert.IsNotNull(selectedSizeIndex, "fixture: GameView.selectedSizeIndex exists in this editor");
                Assert.IsNotNull(sizeSelection, "fixture: GameView.SizeSelectionCallback(int, object) exists in this editor");
                Assert.AreEqual(typeof(int), selectedSizeIndex.PropertyType, "selected index signature");
                Assert.AreEqual(typeof(Vector2), targetSize.PropertyType, "target size signature");
                Assert.AreEqual(typeof(void), sizeSelection.ReturnType, "selection method signature");
                Assert.AreEqual(playModeView, main.ReturnType, "main view method signature");
                var view = main.Invoke(null, null) as UnityEditor.EditorWindow;
                bool created = view == null;
                if (created)
                {
                    MethodInfo create = typeof(UnityEditor.EditorWindow).GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .Single(method => method.Name == "CreateWindow" && method.IsGenericMethodDefinition && method.GetParameters().Length == 1);
                    view = (UnityEditor.EditorWindow)create.MakeGenericMethod(gameView).Invoke(null, new object[] { Type.EmptyTypes });
                }
                Assert.IsInstanceOf(gameView, view, "fixture: the main play mode view is a GameView");
                try
                {
                    var opened = new MainPlayModeView(view, created, targetSize, selectedSizeIndex, sizeSelection, main);
                    notes.Add((created ? "no main play mode view, created one: " : "found the main play mode view: ") + opened.Describe());
                    return opened;
                }
                catch
                {
                    if (created) view.Close();
                    throw;
                }
            }

            // Only an absent approved size is added, in memory. Full HD must already exist.
            internal void SelectFixedResolution(int width, int height, List<string> notes)
            {
                _memory.Ensure(width, height);
                SizeEntry[] sizes = Sizes();
                notes.Add("sizes of the current group: " + string.Join("; ", sizes.Select(size => size.ToString())));
                SizeEntry chosen = sizes.FirstOrDefault(size => size.Type == "FixedResolution" && size.Width == width &&
                    size.Height == height && (width != 1920 || size.Text == "Full HD"));
                Assert.IsNotNull(chosen, "approved FixedResolution is available");
                Select(chosen, notes);
                _memory.Record("selected", SelectedIndex, _originalIndex, false, null);
            }

            internal string Describe()
            {
                if (_view == null) return "view closed";
                object main = _main.Invoke(null, null);
                return string.Format(CultureInfo.InvariantCulture, "{0} created {1}, main {2}, target {3} (original {4}), position {5} " +
                    "(original {6}), selected size {7} (original {8}), pixelsPerPoint {9}", _view.GetType().FullName, _created,
                    ReferenceEquals(main, _view), Format(TargetSize), Format(_originalSize), Position(_view.position),
                    Position(_originalPosition), SelectedIndex, _originalIndex, UnityEditor.EditorGUIUtility.pixelsPerPoint);
            }

            // Asks the original size item and window position back (the result is noted as it is, not assumed) and
            // closes a created view.
            internal void Release(List<string> notes)
            {
                if (_released) return;
                _released = true;
                try
                {
                    Assert.IsNotNull(_view, "memory return: view still owned");
                    _memory.AssertOwned();
                    Assert.AreEqual(_ownedIndex, SelectedIndex, "memory return: selected item is still owned");
                    if (_indexChanged) _sizeSelection.Invoke(_view, new object[] { _originalIndex, null });
                    Assert.AreEqual(_originalIndex, SelectedIndex, "memory return: original selection returned");
                    Assert.AreEqual(_originalPosition, _view.position, "memory return: original position returned");
                    _memory.RemoveOwned();
                    _memory.Record("closed", SelectedIndex, _originalIndex, true, null);
                    notes.Add("release: " + Describe());
                    if (_created) _view.Close();
                }
                catch (Exception error)
                {
                    _memory.Record("failed", _view == null ? -1 : SelectedIndex, _originalIndex, false, error.ToString());
                    throw;
                }
            }

            void Select(SizeEntry chosen, List<string> notes)
            {
                int before = SelectedIndex;
                if (before == chosen.Index)
                {
                    notes.Add($"size item {chosen} is already selected");
                    return;
                }
                _sizeSelection.Invoke(_view, new object[] { chosen.Index, null });
                _indexChanged = true;
                _ownedIndex = chosen.Index;
                Assert.AreEqual(chosen.Index, SelectedIndex, "memory selection applied");
                notes.Add($"SizeSelectionCallback({chosen.Index}): selected {before} -> {SelectedIndex} ({chosen}); {Describe()}");
            }

            static SizeEntry[] Sizes()
            {
                Assembly editor = typeof(UnityEditor.EditorWindow).Assembly;
                Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
                Assert.IsNotNull(sizesType, "fixture: UnityEditor.GameViewSizes exists in this editor");
                PropertyInfo instance = null;
                for (Type type = sizesType; type != null && instance == null; type = type.BaseType)
                    instance = type.GetProperty("instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
                Assert.IsNotNull(instance, "fixture: GameViewSizes has its ScriptableSingleton instance");
                object group = sizesType.GetProperty("currentGroup", AnyInstance)?.GetValue(instance.GetValue(null));
                Assert.IsNotNull(group, "fixture: GameViewSizes.currentGroup");
                Type groupType = group.GetType();
                MethodInfo total = groupType.GetMethod("GetTotalCount", AnyInstance, null, Type.EmptyTypes, null);
                MethodInfo get = groupType.GetMethod("GetGameViewSize", AnyInstance, null, new[] { typeof(int) }, null);
                Assert.IsNotNull(total, "fixture: GameViewSizeGroup.GetTotalCount()");
                Assert.IsNotNull(get, "fixture: GameViewSizeGroup.GetGameViewSize(int)");
                int count = (int)total.Invoke(group, null);
                var sizes = new List<SizeEntry>();
                for (int index = 0; index < count; index++)
                {
                    object size = get.Invoke(group, new object[] { index });
                    Type sizeType = size.GetType();
                    sizes.Add(new SizeEntry(index, (int)sizeType.GetProperty("width", AnyInstance).GetValue(size),
                        (int)sizeType.GetProperty("height", AnyInstance).GetValue(size),
                        sizeType.GetProperty("sizeType", AnyInstance).GetValue(size).ToString(),
                        (string)sizeType.GetProperty("baseText", AnyInstance).GetValue(size)));
                }
                return sizes.ToArray();
            }

            sealed class SizeEntry
            {
                internal readonly int Index;
                internal readonly int Width;
                internal readonly int Height;
                internal readonly string Type;
                internal readonly string Text;

                internal SizeEntry(int index, int width, int height, string type, string text)
                {
                    Index = index;
                    Width = width;
                    Height = height;
                    Type = type;
                    Text = text;
                }

                public override string ToString() => $"#{Index} {Type} {Width}x{Height} '{Text}'";
            }
        }

        // The memory list owns only its new object, while MainPlayModeView owns selection and position.
        // Signature checks are performed on 6000.6.4f1 at runtime before any addition or selection.
        // External file bytes are watched by the runner; successful list return is not a disk return claim.
        sealed class MemorySizeList
        {
            const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            readonly object _sizes;
            readonly object _group;
            readonly Type _sizeType;
            readonly Type _kindType;
            readonly MethodInfo _add;
            readonly MethodInfo _remove;
            readonly GroupState[] _groups;
            readonly string _label;
            readonly string _directory;
            object _added;
            SizeState _addedState;
            int _width;
            int _height;

            MemorySizeList(object sizes, object group, Type sizeType, Type kindType, GroupState[] groups,
                MethodInfo add, MethodInfo remove, int originalIndex)
            {
                _sizes = sizes;
                _group = group;
                _sizeType = sizeType;
                _kindType = kindType;
                _groups = groups;
                _add = add;
                _remove = remove;
                _label = Guid.NewGuid().ToString("N");
                _directory = MemoryDirectory();
                Record("before", originalIndex, originalIndex, false, null);
            }

            internal static void CheckPermit()
            {
                Assert.AreEqual("6000.6.4f1", Application.unityVersion, "exact GameView API version");
                string path = Environment.GetEnvironmentVariable("DAWNHOLDER_GUILD_MEMORY_PERMIT");
                Assert.IsFalse(string.IsNullOrEmpty(path), "memory use requires a runner permit");
                string directory = MemoryDirectory();
                Assert.AreEqual(Path.Combine(directory, "memory-permit.json"), Path.GetFullPath(path), "permit location");
                var permit = JsonUtility.FromJson<MemoryPermit>(File.ReadAllText(path));
                Assert.AreEqual("guild-memory-permit-v1", permit.schema, "permit schema");
                Assert.AreEqual(Environment.GetEnvironmentVariable("DAWNHOLDER_GUILD_MEMORY_TOKEN"), permit.token, "permit token");
                Assert.IsTrue(!string.IsNullOrEmpty(permit.token) && permit.token.Length == 32, "nonempty token");
                Assert.AreEqual(Environment.GetEnvironmentVariable("DAWNHOLDER_GUILD_EXECUTION_TASK"), permit.task, "permit Task");
                Assert.AreEqual(Environment.GetEnvironmentVariable("DAWNHOLDER_GUILD_EXECUTION_DISPATCH"), permit.dispatch, "permit Dispatch");
                Assert.IsTrue(Regex.IsMatch(permit.task ?? "", "^task_[a-z0-9]+$"), "Task syntax");
                Assert.IsTrue(Regex.IsMatch(permit.dispatch ?? "", "^ctx_[a-z0-9]+$"), "Dispatch syntax");
                Assert.AreEqual(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), Path.GetFullPath(permit.project), "project");
                Assert.AreEqual("C:/Users/bass1/AppData/Roaming/Unity/Editor-5.x/Preferences/GameViewSizes.asset",
                    permit.target.Replace('\\', '/'), "exact protected file");
                Assert.Less(DateTimeOffset.UtcNow, DateTimeOffset.Parse(permit.deadlineUtc, CultureInfo.InvariantCulture), "permit deadline");
                using (var process = System.Diagnostics.Process.GetCurrentProcess())
                {
                    Assert.AreEqual(process.Id, permit.unityPid, "Unity PID");
                    Assert.AreEqual(process.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture), permit.unityBirth, "Unity birth");
                }
                using (var owner = System.Diagnostics.Process.GetProcessById(permit.ownerPid))
                {
                    Assert.IsFalse(owner.HasExited, "supervisor alive");
                    Assert.AreEqual(owner.StartTime.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture), permit.ownerBirth, "supervisor birth");
                }
            }

            internal static MemorySizeList Open(int originalIndex)
            {
                CheckPermit();
                Assembly editor = typeof(UnityEditor.EditorWindow).Assembly;
                Type sizesType = editor.GetType("UnityEditor.GameViewSizes", true);
                Type groupType = editor.GetType("UnityEditor.GameViewSizeGroup", true);
                Type sizeType = editor.GetType("UnityEditor.GameViewSize", true);
                Type kindType = editor.GetType("UnityEditor.GameViewSizeType", true);
                Type groupKind = editor.GetType("UnityEditor.GameViewSizeGroupType", true);
                PropertyInfo instance = sizesType.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                Assert.IsNotNull(instance, "ScriptableSingleton.instance signature");
                object sizes = instance.GetValue(null);
                MethodInfo pathMethod = sizesType.BaseType.GetMethod("GetFilePath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(pathMethod, "serialized path API");
                Assert.AreEqual(typeof(string), pathMethod.ReturnType, "serialized path result");
                Assert.AreEqual("C:/Users/bass1/AppData/Roaming/Unity/Editor-5.x/Preferences/GameViewSizes.asset",
                    ((string)pathMethod.Invoke(null, null)).Replace('\\', '/'), "serialized path is protected target");
                PropertyInfo current = RequireProperty(sizesType, "currentGroup", groupType);
                PropertyInfo currentKind = RequireProperty(sizesType, "currentGroupType", groupKind);
                Assert.AreEqual("Standalone", currentKind.GetValue(sizes).ToString(), "Standalone group only");
                MethodInfo getGroup = RequireMethod(sizesType, "GetGroup", groupType, groupKind);
                MethodInfo add = RequireMethod(groupType, "AddCustomSize", typeof(void), sizeType);
                MethodInfo remove = RequireMethod(groupType, "RemoveCustomSize", typeof(void), typeof(int));
                RequireMethod(groupType, "GetBuiltinCount", typeof(int));
                RequireMethod(groupType, "GetTotalCount", typeof(int));
                RequireMethod(groupType, "GetGameViewSize", sizeType, typeof(int));
                RequireProperty(sizeType, "width", typeof(int));
                RequireProperty(sizeType, "height", typeof(int));
                RequireProperty(sizeType, "baseText", typeof(string));
                RequireProperty(sizeType, "sizeType", kindType);
                Assert.IsNotNull(sizeType.GetConstructor(new[] { kindType, typeof(int), typeof(int), typeof(string) }), "size constructor");
                GroupState[] groups = new[] { "Standalone", "iOS", "Android", "HMD" }.Select(name =>
                    new GroupState(name, getGroup.Invoke(sizes, new[] { Enum.Parse(groupKind, name) }))).ToArray();
                object group = current.GetValue(sizes);
                Assert.IsTrue(ReferenceEquals(groups[0].Group, group), "current group identity");
                Assert.IsTrue(groups[0].Entries.Any(entry => entry.Kind == "FixedResolution" && entry.Width == 1920 &&
                    entry.Height == 1080 && entry.Text == "Full HD"), "existing Full HD required; never add it");
                foreach (GroupState state in groups)
                {
                    FieldInfo field = sizesType.GetField("m_" + state.Name, InstanceMembers);
                    Assert.IsNotNull(field, "runtime group field " + state.Name);
                    Assert.AreEqual(groupType, field.FieldType, "runtime group field type " + state.Name);
                    Assert.IsTrue(ReferenceEquals(state.Group, field.GetValue(sizes)), "runtime group field identity");
                }
                return new MemorySizeList(sizes, group, sizeType, kindType, groups, add, remove, originalIndex);
            }

            static string MemoryDirectory()
            {
                string capture = Path.GetFullPath(Environment.GetEnvironmentVariable(CaptureDirectoryVariable));
                string expected = Path.GetFullPath(Path.Combine(capture, "..", "..", "runs", Path.GetFileName(capture), "memory"));
                string actual = Path.GetFullPath(Environment.GetEnvironmentVariable("DAWNHOLDER_GUILD_MEMORY_DIR"));
                Assert.AreEqual(expected, actual, "memory evidence belongs to this capture run");
                return actual;
            }

            internal void Ensure(int width, int height)
            {
                CheckPermit();
                AssertOwned();
                Assert.IsTrue((width == 709 && height == 399) || (width == 802 && height == 451) ||
                    (width == 1280 && height == 720) || (width == 1920 && height == 1080), "allowed size");
                _width = width;
                _height = height;
                if (GroupState.Read(_group).Any(entry => entry.Kind == "FixedResolution" && entry.Width == width && entry.Height == height)) return;
                Assert.AreNotEqual(1920, width, "Full HD is never added");
                Assert.IsNull(_added, "one requested size per scope");
                _added = Activator.CreateInstance(_sizeType, Enum.Parse(_kindType, "FixedResolution"), width, height, "Temporary " + _label);
                _addedState = new SizeState(_added);
                _add.Invoke(_group, new[] { _added });
                AssertOwned();
                Record("added", -1, -1, false, null);
            }

            internal void AssertOwned()
            {
                Assert.IsTrue(ReferenceEquals(_group, _sizes.GetType().GetProperty("currentGroup", InstanceMembers).GetValue(_sizes)),
                    "memory group did not change");
                foreach (GroupState group in _groups)
                {
                    Assert.IsTrue(ReferenceEquals(group.Group,
                        _sizes.GetType().GetField("m_" + group.Name, InstanceMembers).GetValue(_sizes)),
                        "memory group object ownership " + group.Name);
                    Assert.AreEqual(group.BuiltinCount,
                        (int)group.Group.GetType().GetMethod("GetBuiltinCount", InstanceMembers).Invoke(group.Group, null),
                        "builtin/custom boundary " + group.Name);
                    SizeState[] expected = ReferenceEquals(group.Group, _group) && _addedState != null ?
                        group.Entries.Concat(new[] { _addedState }).ToArray() : group.Entries;
                    SizeState[] current = GroupState.Read(group.Group);
                    Assert.AreEqual(expected.Length, current.Length, "memory list count " + group.Name);
                    for (int index = 0; index < current.Length; index++)
                        Assert.IsTrue(expected[index].Same(current[index]), "memory object/value ownership " + group.Name + " #" + index);
                }
            }

            internal void RemoveOwned()
            {
                AssertOwned();
                if (_added != null)
                {
                    int index = _groups[0].Entries.Length;
                    int builtin = (int)_group.GetType().GetMethod("GetBuiltinCount", InstanceMembers).Invoke(_group, null);
                    Assert.GreaterOrEqual(index, builtin, "only a custom item may be removed");
                    _remove.Invoke(_group, new object[] { index });
                    _added = null;
                    _addedState = null;
                }
                AssertOwned();
            }

            internal void Record(string phase, int selected, int original, bool returned, string error)
            {
                var record = new MemoryRecord { phase = phase, width = _width, height = _height, selected = selected,
                    original = original, memoryReturned = returned, error = error, ownedObject = _added == null ? null : _label,
                    lists = _groups.SelectMany(group => GroupState.Read(group.Group).Select((entry, index) =>
                        group.Name + " #" + index + " " + entry.Describe())).ToArray() };
                WriteNew(Path.Combine(_directory, "memory-" + _label + "-" + phase + ".json"),
                    System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(record, true)));
            }

            static MethodInfo RequireMethod(Type type, string name, Type result, params Type[] parameters)
            {
                MethodInfo method = type.GetMethod(name, InstanceMembers, null, parameters, null);
                Assert.IsNotNull(method, "runtime method " + name);
                Assert.AreEqual(result, method.ReturnType, "runtime method return " + name);
                return method;
            }

            static PropertyInfo RequireProperty(Type type, string name, Type result)
            {
                PropertyInfo property = type.GetProperty(name, InstanceMembers);
                Assert.IsNotNull(property, "runtime property " + name);
                Assert.AreEqual(result, property.PropertyType, "runtime property type " + name);
                return property;
            }

            sealed class GroupState
            {
                internal readonly string Name;
                internal readonly object Group;
                internal readonly SizeState[] Entries;
                internal readonly int BuiltinCount;
                internal GroupState(string name, object group)
                {
                    Name = name;
                    Group = group;
                    Entries = Read(group);
                    BuiltinCount = (int)group.GetType().GetMethod("GetBuiltinCount", InstanceMembers).Invoke(group, null);
                }
                internal static SizeState[] Read(object group)
                {
                    Type type = group.GetType();
                    int count = (int)type.GetMethod("GetTotalCount", InstanceMembers).Invoke(group, null);
                    MethodInfo get = type.GetMethod("GetGameViewSize", InstanceMembers);
                    return Enumerable.Range(0, count).Select(index => new SizeState(get.Invoke(group, new object[] { index }))).ToArray();
                }
            }

            sealed class SizeState
            {
                internal readonly object Identity;
                internal readonly int Width;
                internal readonly int Height;
                internal readonly string Kind;
                internal readonly string Text;
                internal SizeState(object value)
                {
                    Identity = value;
                    Type type = value.GetType();
                    Width = (int)type.GetProperty("width", InstanceMembers).GetValue(value);
                    Height = (int)type.GetProperty("height", InstanceMembers).GetValue(value);
                    Kind = type.GetProperty("sizeType", InstanceMembers).GetValue(value).ToString();
                    Text = (string)type.GetProperty("baseText", InstanceMembers).GetValue(value);
                }
                internal bool Same(SizeState other) => ReferenceEquals(Identity, other.Identity) && Width == other.Width &&
                    Height == other.Height && Kind == other.Kind && Text == other.Text;
                internal string Describe() => $"{Kind} {Width}x{Height} '{Text}'";
            }

            [Serializable]
            sealed class MemoryPermit
            {
                public string schema;
                public string token;
                public string task;
                public string dispatch;
                public string project;
                public string target;
                public string deadlineUtc;
                public string unityBirth;
                public string ownerBirth;
                public int unityPid;
                public int ownerPid;
            }

            [Serializable]
            sealed class MemoryRecord
            {
                public string phase;
                public string ownedObject;
                public string error;
                public int width;
                public int height;
                public int selected;
                public int original;
                public bool memoryReturned;
                public string[] lists;
            }
        }
#endif

        // Raw visibility of one judged surface, written next to its capture and summarised in one log line.
        [Serializable]
        internal sealed class VisibilityRecord
        {
            public string label;
            public string mode;
            public string surface;
            public int screenWidth;
            public int screenHeight;
            public int surfaceWidth;
            public int surfaceHeight;
            public int requestedWidth;
            public int requestedHeight;
            public int frameWidth;
            public int frameHeight;
            public int renderTextureWidth;
            public int renderTextureHeight;
            public int cameraPixelWidth;
            public int cameraPixelHeight;
            public int renderingWidth;
            public int renderingHeight;
            public string mainPlayModeView;
            public string playModeView;
            public string[] sizing;
            public float canvasScaleFactor;
            public float referenceScaleFactor;
            public string referenceRoot;
            public string productRoot;
            public string canvasRenderingDisplaySize;
            public string canvasPixelRect;
            public string captureMethod;
            public int captureFrame;
            public string[] captures;
            public int[] captureFrames;
            public int[] captureWidths;
            public int[] captureHeights;
            public OriginalRecord[] originals;
            public int frameLit;
            public int frameInk;
            public int referenceInk;
            public float[] panelLit;
            public int restoredInkDifferences;
            public string notReviewed;
            public string pngPath;
            public string evidencePath;
            public string[] missingCharacters;
            public string[] raycasts;
            public string[] findings;
            public string[] nearThreshold;
            public LabelRecord[] labels;
        }

        [Serializable]
        internal sealed class LabelRecord
        {
            public string window;
            public string name;
            public string expected;
            public string text;
            public string laidOut;
            public float fontSize;
            public float referenceFontSize;
            public int characters;
            public int referenceCharacters;
            public float maxQuadDeviation;
            public bool overflowing;
            public string box;
            public string[] rectMasks;
            public int graphicsSwitchedOff;
            public int graphicsKept;
            public float restoredFontSize;
            public float restoredQuadDeviation;
            public int inkR;
            public int inkS;
            public int inkF;
            public int inkFS;
            public int foreign;
            public string cropPath;
            public string cropDerivedFrom;
            public string[] causes;
            public CharacterRecord[] chars;
        }

        [Serializable]
        internal sealed class CharacterRecord
        {
            public int index;
            public string character;
            public string quad;
            public string referenceQuad;
            public int r;
            public int s;
            public int f;
            public int fs;
            public int foreign;
            public float sOverR;
            public float fsOverS;
        }

        // One capture's whole frame as Judgement.NoteCapture kept it: its place in the capture order, role, kind of
        // surface (the real Overlay screen or an auxiliary RenderTexture, never mixed), frame and size, and the flat file
        // with its bytes and SHA-256 (the harness compares both with the file). problem names why there is no file.
        [Serializable]
        internal sealed class OriginalRecord
        {
            public int sequence;
            public string role;
            public string source;
            public int frame;
            public int width;
            public int height;
            public string file;
            public long bytes;
            public string sha256;
            public string problem;
        }

        [Serializable]
        internal sealed class NotesRecord
        {
            public string label;
            public string[] notes;
        }
    }
}
