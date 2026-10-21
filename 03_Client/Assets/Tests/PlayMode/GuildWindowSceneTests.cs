using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
    // Real Unity frames for the guild windows of goal 2026-10-15-guild-client-ui PR1 (contract-v1 req 4, 5, 6 and 9;
    // goal C8 and C9 for the window itself), written before the product. Real path: the product's BuildRuntime in Play
    // Mode, its own Canvas and TMP layout, the current EventSystem's raycast and a virtual mouse click through the Input
    // System UI module. Stand-ins: an unsaved empty scene with its own EventSystem, values supplied by this test through
    // the product's GuildDisplayModel, and no server, socket, Town or map bootstrap. The product's own
    // RuntimeInitializeOnLoadMethod services start with Play Mode as in every PlayMode test; this test does not use them.
    // Real combat input and the overlap with the existing UI are PR2's live-screen checks.
    public sealed class GuildWindowSceneTests
    {
        const float WaitSeconds = 3f;
        static readonly string[] GuildLabels =
            { "GuildName", "MemberCount", "OnlineCount", "Treasury", "BaseLevel", "LevelUpCost", "AutoJoinNotice" };
        static readonly string[] BlacksmithLabels =
            { "CharacterClass", "EnhanceLevel", "AttackBonus", "GoldCost", "MaterialCost" };

        Scene _previousActive;
        Scene _scene;
        GameObject _eventSystem;
        GameObject _root;
        Mouse _mouse;
        InputSettings _originalInputSettings;
        InputSettings _originalInputSettingsCopy;
        InputSettings _runtimeInputSettings;
        bool _inputSettingsReplaced;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            Assert.IsTrue(Application.isPlaying, "this lane must execute real Unity Play Mode");
            _previousActive = SceneManager.GetActiveScene();
            // Not a CombatBootstrap map name, so no gameplay bootstrap installs itself into this scene.
            _scene = SceneManager.CreateScene("GuildWindowFixture_" + Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(_scene);
            // Input System 1.20.0 destroys replaced settings flagged HideAndDontSave (client.md); restore from an
            // untouched copy if the original does not survive, as MapEntryPlayFixture does.
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
            _mouse = InputSystem.AddDevice<Mouse>("GuildWindowTestMouse");
            // This assembly does not reference uGUI, so the EventSystem and the Input System UI module go by type name.
            _eventSystem = new GameObject("GuildWindowTestEventSystem");
            _eventSystem.AddComponent(TypeByName("UnityEngine.EventSystems.EventSystem, UnityEngine.UI"));
            _eventSystem.AddComponent(TypeByName("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem"));
            _root = new GameObject("GuildWindowTestRoot");
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
                if (unload != null) yield return MapEntryPlayFixture.Wait(() => unload.isDone, "guild fixture scene unload", 5);
            }
            if (errors.Count != 0) throw new AggregateException("Guild window fixture restoration failed", errors);
        }

        // req6/req9/req4: the open guild window is readable without art (every named label laid out on screen in full,
        // Korean included), its button takes the EventSystem raycast while screen corners do not hit the window, and one
        // real pointer click sends exactly one level-up intent.
        [UnityTest]
        public IEnumerator OpenGuildWindow_IsReadableWithoutArt_AndOnePointerClickSendsOneIntent()
        {
            yield return OpenWindowIsReadableAndClickable(GuildSceneWindow.Guild, GuildLabels);
        }

        // The same for the blacksmith window and its enhance intent.
        [UnityTest]
        public IEnumerator OpenBlacksmithWindow_IsReadableWithoutArt_AndOnePointerClickSendsOneIntent()
        {
            yield return OpenWindowIsReadableAndClickable(GuildSceneWindow.Blacksmith, BlacksmithLabels);
        }

        // req5/req9: once closed, the guild window draws nothing, the raycast at its button's place hits nothing of it,
        // and a pointer click there sends nothing. The same raycast while open is the control.
        [UnityTest]
        public IEnumerator ClosedGuildWindow_TakesNoRaycast_AndAPointerClickThereSendsNothing()
        {
            GuildSceneWindow window = GuildSceneWindow.Build(GuildSceneWindow.Guild, _root.transform);
            window.SupplyAllowed(pending: false);
            window.Bind();
            window.SetOpen(true);
            window.SetInputAllowed(true);
            yield return Frames(3);
            yield return ScreenUiProbe.WaitForEventSystem();
            Vector2 point = ScreenUiProbe.ScreenPoint(window.ActionButton);
            HashSet<GameObject> windowObjects = window.Objects();
            bool openTakesRaycast = ScreenUiProbe.RaycastHits(point).Any(windowObjects.Contains);

            window.SetOpen(false);
            yield return Frames(3);
            string[] shownWhileClosed = ScreenUiProbe.ShownTextsUnder(window.Window.transform)
                .Select(text => $"{text.name}='{text.text}'")
                .ToArray();
            string[] hitsWhileClosed = ScreenUiProbe.RaycastHits(point)
                .Where(windowObjects.Contains)
                .Select(hit => hit.name)
                .ToArray();
            yield return InventoryPanelProbe.ClickAt(_mouse, point);
            yield return Frames(3);
            Debug.Log($"[guild PR1 closed] open hit {openTakesRaycast}, shown [{string.Join(", ", shownWhileClosed)}], " +
                $"hits [{string.Join(", ", hitsWhileClosed)}], intents {window.Intents}");

            Assert.IsTrue(openTakesRaycast, "control: the open window takes the raycast at its button");
            Assert.IsEmpty(shownWhileClosed, "req5 the closed window draws nothing");
            Assert.IsEmpty(hitsWhileClosed, "req5 the closed window takes no raycast, so pointer attacks pass");
            Assert.AreEqual(0, window.Intents, "req4 a click on the closed window sends nothing");
        }

        // req4/req9: while the enhancement is in progress a real pointer click on the button sends nothing; after the
        // model supplies the settled state the same click sends one intent (the control that the click lands).
        [UnityTest]
        public IEnumerator InProgressBlacksmith_APointerClickSendsNothing_UntilTheModelSettles()
        {
            GuildSceneWindow window = GuildSceneWindow.Build(GuildSceneWindow.Blacksmith, _root.transform);
            window.SupplyAllowed(pending: true);
            window.Bind();
            window.SetOpen(true);
            window.SetInputAllowed(true);
            yield return Frames(3);
            yield return ScreenUiProbe.WaitForEventSystem();
            Vector2 point = ScreenUiProbe.ScreenPoint(window.ActionButton);
            yield return InventoryPanelProbe.ClickAt(_mouse, point);
            yield return Frames(3);
            int intentsWhilePending = window.Intents;

            window.SupplyAllowed(pending: false);
            yield return Frames(3);
            point = ScreenUiProbe.ScreenPoint(window.ActionButton);
            yield return InventoryPanelProbe.ClickAt(_mouse, point);
            yield return ScreenUiProbe.WaitUntil(() => window.Intents > 0, WaitSeconds, _ => { });
            yield return Frames(3);
            int intentsAfterSettling = window.Intents;

            Assert.AreEqual(0, intentsWhilePending, "req4 a click while in progress sends nothing");
            Assert.AreEqual(1, intentsAfterSettling, "control: once settled, one click sends one intent");
        }

        // req5: in real Play Mode, destroying the window releases its source, and a later source change reaches nothing.
        [UnityTest]
        public IEnumerator DestroyedGuildWindow_ReleasesItsSource()
        {
            GuildSceneWindow window = GuildSceneWindow.Build(GuildSceneWindow.Guild, _root.transform);
            window.SupplyAllowed(pending: false);
            window.Bind();
            yield return Frames(2);
            int subscribedBefore = window.ModelSubscribers;
            UnityEngine.Object.Destroy(_root);
            _root = null;
            yield return Frames(2);
            int subscribedAfter = window.ModelSubscribers;
            Exception laterChange = null;
            try { window.SupplyAllowed(pending: true); }
            catch (Exception error) { laterChange = error; }

            Assert.AreEqual(1, subscribedBefore, "req5 precondition: the window subscribed its source");
            Assert.AreEqual(0, subscribedAfter, "req5 the destroyed window released its source");
            Assert.IsNull(laterChange, "req5 a later source change reaches no destroyed window");
        }

        IEnumerator OpenWindowIsReadableAndClickable(string kind, string[] labels)
        {
            GuildSceneWindow window = GuildSceneWindow.Build(kind, _root.transform);
            window.SupplyAllowed(pending: false);
            window.Bind();
            window.SetOpen(true);
            window.SetInputAllowed(true);
            yield return Frames(3);
            yield return ScreenUiProbe.WaitForEventSystem();
            bool ownEventSystem = IsCurrentEventSystem(_eventSystem);
            string[] unreadable = labels.SelectMany(name => Unreadable(window.Label(name))).ToArray();
            Component button = window.ActionButton;
            bool buttonShown = ScreenUiProbe.IsShown(button);
            string caption = string.Join(" ", button.GetComponentsInChildren<TMP_Text>(true)
                .Where(ScreenUiProbe.IsShown)
                .Select(text => text.text));
            Vector2 point = ScreenUiProbe.ScreenPoint(button);
            bool buttonOnScreen = ScreenUiProbe.OnScreen(point);
            var buttonObjects = new HashSet<GameObject>(button.GetComponentsInChildren<Transform>(true).Select(item => item.gameObject));
            bool buttonTakesRaycast = ScreenUiProbe.RaycastHits(point).Any(buttonObjects.Contains);
            HashSet<GameObject> windowObjects = window.Objects();
            string[] cornerHits = ScreenCorners()
                .SelectMany(corner => ScreenUiProbe.RaycastHits(corner).Where(windowObjects.Contains))
                .Select(hit => hit.name)
                .ToArray();
            yield return InventoryPanelProbe.ClickAt(_mouse, point);
            yield return ScreenUiProbe.WaitUntil(() => window.Intents > 0, WaitSeconds, _ => { });
            yield return Frames(3);
            int intents = window.Intents;
            Debug.Log($"[guild PR1 open {kind}] screen {Screen.width}x{Screen.height}, button at {point}, unreadable " +
                $"[{string.Join("; ", unreadable)}], caption '{caption}', button hit {buttonTakesRaycast}, corner hits " +
                $"[{string.Join(", ", cornerHits)}], intents {intents}");

            Assert.IsTrue(ownEventSystem, "fixture: the fixture scene's EventSystem is the current one");
            Assert.IsEmpty(unreadable, "req6 every named label is readable on screen without art");
            Assert.IsTrue(buttonShown, "req6 the action button is drawn");
            Assert.IsNotEmpty(caption.Trim(), "req6 the action button has a readable caption");
            Assert.IsTrue(buttonOnScreen, $"req6 the action button is on screen at {point}");
            Assert.IsTrue(buttonTakesRaycast, "req5/req9 the EventSystem raycast hits the open window's button");
            Assert.IsEmpty(cornerHits, "req5 the open window is no full-screen raycast board");
            Assert.AreEqual(1, intents, "req4/req9 one real pointer click sends exactly one intent");
        }

        // Why a named label is not readable, or nothing: not drawn, empty, cut off, a character not laid out as written
        // (a missing glyph is replaced) or outside the screen.
        static IEnumerable<string> Unreadable(TMP_Text label)
        {
            string name = label.name;
            if (!ScreenUiProbe.IsShown(label)) { yield return name + " is not drawn"; yield break; }
            label.ForceMeshUpdate();
            string written = label.text;
            if (string.IsNullOrWhiteSpace(written)) { yield return name + " is empty"; yield break; }
            if (label.isTextOverflowing) yield return $"{name} '{written}' is cut off";
            string laidOut = new string(label.textInfo.characterInfo
                .Take(label.textInfo.characterCount)
                .Select(character => character.character)
                .ToArray());
            if (laidOut != written) yield return $"{name} lays out '{laidOut}' for '{written}'";
            Rect rect = ScreenUiProbe.ScreenRect((RectTransform)label.transform);
            bool inside = rect.xMin >= -1f && rect.yMin >= -1f && rect.xMax <= Screen.width + 1f && rect.yMax <= Screen.height + 1f;
            if (!inside) yield return $"{name} at {rect} is outside the {Screen.width}x{Screen.height} screen";
        }

        static IEnumerable<Vector2> ScreenCorners()
        {
            const float inset = 2f;
            yield return new Vector2(inset, inset);
            yield return new Vector2(Screen.width - inset, inset);
            yield return new Vector2(inset, Screen.height - inset);
            yield return new Vector2(Screen.width - inset, Screen.height - inset);
        }

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        static bool IsCurrentEventSystem(GameObject owner)
        {
            Type type = TypeByName("UnityEngine.EventSystems.EventSystem, UnityEngine.UI");
            object current = type.GetProperty("current", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            return current is Component component && component.gameObject == owner;
        }

        static Type TypeByName(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName);
            Assert.IsNotNull(type, "fixture: type " + assemblyQualifiedName);
            return type;
        }
    }

    // One product window in Play Mode, reached by the names .backups/verification/2026-10-15-guild-client-ui/pr1-tdd/
    // api-contract.md fixes. EditMode's GuildWindowTestAdapter does the same there; the two test assemblies cannot share
    // a file, so this keeps only what the real-frame lane needs. A missing type or member fails with "RED unimplemented".
    internal sealed class GuildSceneWindow
    {
        internal const string Guild = "GuildWindow";
        internal const string Blacksmith = "BlacksmithWindow";
        const string Namespace = "Dawnholder.Client.UI.";
        static readonly Assembly Client = typeof(InventoryPanel).Assembly;

        readonly Type _type;
        readonly string _kind;
        int _intents;

        GuildSceneWindow(string kind, Transform parent)
        {
            _kind = kind;
            _type = Require(kind);
            Type modelType = Require("GuildDisplayModel");
            if (modelType.GetConstructor(Type.EmptyTypes) == null) Assert.Fail("RED unimplemented: public GuildDisplayModel()");
            Model = Activator.CreateInstance(modelType);
            MethodInfo build = Method(_type, "BuildRuntime", typeof(Transform));
            string intentName = kind == Guild ? "LevelUpRequested" : "EnhanceRequested";
            EventInfo intent = _type.GetEvent(intentName, BindingFlags.Public | BindingFlags.Instance);
            if (intent == null || intent.EventHandlerType != typeof(Action))
                Assert.Fail($"RED unimplemented: public event Action {_type.Name}.{intentName}");
            Window = (Component)Invoke(() => build.Invoke(null, new object[] { parent }));
            Assert.IsNotNull(Window, $"RED unimplemented: {_type.Name}.BuildRuntime returns the window");
            intent.AddEventHandler(Window, (Action)(() => _intents++));
        }

        internal Component Window { get; }

        internal object Model { get; }

        internal int Intents => _intents;

        internal int ModelSubscribers
        {
            get
            {
                FieldInfo field = Model.GetType().GetField("Changed", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(field, "RED unimplemented: field-like event GuildDisplayModel.Changed");
                return (field.GetValue(Model) as Delegate)?.GetInvocationList().Length ?? 0;
            }
        }

        internal Component ActionButton
        {
            get
            {
                string name = _kind == Guild ? "LevelUp" : "Enhance";
                Component[] found = Window.GetComponentsInChildren<Transform>(true)
                    .Where(item => item.name == name)
                    .SelectMany(item => item.GetComponents<Component>())
                    .Where(component => component != null && component.GetType().FullName == "UnityEngine.UI.Button")
                    .ToArray();
                if (found.Length != 1) Assert.Fail($"RED unimplemented: exactly one Button named '{name}' (found {found.Length})");
                return found[0];
            }
        }

        internal static GuildSceneWindow Build(string kind, Transform parent) => new GuildSceneWindow(kind, parent);

        internal TMP_Text Label(string name)
        {
            TMP_Text[] found = Window.GetComponentsInChildren<TMP_Text>(true).Where(text => text.name == name).ToArray();
            if (found.Length != 1) Assert.Fail($"RED unimplemented: exactly one TMP_Text named '{name}' (found {found.Length})");
            return found[0];
        }

        internal HashSet<GameObject> Objects() =>
            new HashSet<GameObject>(Window.GetComponentsInChildren<Transform>(true).Select(item => item.gameObject));

        internal void Bind() => Call("Bind", Require("GuildDisplayModel"), Model);

        internal void SetOpen(bool open) => Call("SetOpen", typeof(bool), open);

        internal void SetInputAllowed(bool allowed) => Call("SetInputAllowed", typeof(bool), allowed);

        // The allowed state from the approved World values (World goal 2026-10-14-guild-server 「승인 A」): treasury 30
        // funds the 30 level-up at level 1 of 2; the unlocked blacksmith takes 20 gold and 1 Material for step 0 -> 1.
        internal void SupplyAllowed(bool pending)
        {
            if (_kind == Guild)
            {
                object display = Construct("GuildStatusDisplay", new Dictionary<string, object>
                {
                    ["guildName"] = "새벽 길드",
                    ["memberCount"] = 2,
                    ["onlineCount"] = 2,
                    ["treasury"] = 30L,
                    ["baseLevel"] = 1,
                    ["maxBaseLevel"] = 2,
                    ["blacksmithUnlocked"] = false,
                    ["levelUpCost"] = 30L,
                    ["levelUp"] = "Available",
                    ["levelUpPending"] = pending,
                    ["levelUpResult"] = "None",
                });
                SetModel("SetGuild", "GuildStatusDisplay", display);
                return;
            }
            object smith = Construct("BlacksmithDisplay", new Dictionary<string, object>
            {
                ["characterClass"] = CharacterClass.Knight,
                ["enhanceLevel"] = 0,
                ["maxEnhanceLevel"] = 1,
                ["attackBonus"] = 0,
                ["goldCost"] = 20L,
                ["materialCost"] = 1,
                ["enhance"] = "Available",
                ["enhancePending"] = pending,
                ["enhanceResult"] = "None",
            });
            SetModel("SetBlacksmith", "BlacksmithDisplay", smith);
        }

        void SetModel(string method, string displayType, object display)
        {
            MethodInfo set = Method(Model.GetType(), method, Require(displayType));
            Invoke(() => set.Invoke(Model, new[] { display }));
        }

        void Call(string method, Type parameter, object argument)
        {
            MethodInfo call = Method(_type, method, parameter);
            Invoke(() => call.Invoke(Window, new[] { argument }));
        }

        static Type Require(string name)
        {
            Type type = Client.GetType(Namespace + name);
            if (type == null) Assert.Fail($"RED unimplemented: product type {Namespace}{name} (api-contract.md)");
            return type;
        }

        static MethodInfo Method(Type type, string name, Type parameter)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
                null, new[] { parameter }, null);
            if (method == null) Assert.Fail($"RED unimplemented: public {type.Name}.{name}({parameter.Name})");
            return method;
        }

        // By constructor parameter names; numbers convert to the parameter's type and enum members go by name.
        static object Construct(string typeName, Dictionary<string, object> arguments)
        {
            Type type = Require(typeName);
            ConstructorInfo constructor = type.GetConstructors().FirstOrDefault(candidate => candidate.GetParameters()
                .Select(parameter => parameter.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(arguments.Keys.OrderBy(name => name, StringComparer.Ordinal)));
            if (constructor == null)
                Assert.Fail($"RED unimplemented: public {type.Name}({string.Join(", ", arguments.Keys)}) by parameter name");
            object[] values = constructor.GetParameters()
                .Select(parameter => Convert(arguments[parameter.Name], parameter.ParameterType, type.Name + "." + parameter.Name))
                .ToArray();
            return Invoke(() => constructor.Invoke(values));
        }

        static object Convert(object value, Type target, string where)
        {
            Type underlying = Nullable.GetUnderlyingType(target) ?? target;
            if (value == null || underlying.IsInstanceOfType(value)) return value;
            if (underlying.IsEnum && value is string member)
            {
                if (!Enum.IsDefined(underlying, member)) Assert.Fail($"RED unimplemented: {underlying.Name}.{member}");
                return Enum.Parse(underlying, member);
            }
            if (value is Enum || underlying.IsEnum)
                Assert.Fail($"RED unimplemented: {where} must take {value.GetType().Name} (found {target.Name})");
            return System.Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
        }

        static object Invoke(Func<object> call)
        {
            try
            {
                return call();
            }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
    }
}
