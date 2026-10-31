using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // T3 (goal 2026-10-30-login-screen-guild-window completion row 2, 「건드릴 곳과 소유」 PR-A rows): rebuilding the menu
    // into the login window keeps the MainMenuController wiring of the shipped scene. The expected file IDs and values
    // are the ones MainMenu.unity holds at base 7bc758987e3816e0690f4df78f8160fbc3da2135 (line numbers of that file),
    // read the way Unity stores the wiring (SerializedObject, as MainMenuAccountSceneTests). The scene is never saved.
    public sealed class MainMenuLoginSceneWiringTests
    {
        // MainMenu.unity at 7bc75898: line 1111 「--- !u!114 &1335115214」, the MainMenuController on UIController.
        const ulong ControllerId = 1335115214;
        // Lines 1123-1126: the four references, each the component's own file ID.
        const ulong ServerHostInputId = 1209814488; // line 1123, TMP_InputField on ServerHostInput (line 770)
        const ulong AccountNameInputId = 2200000006; // line 1124, TMP_InputField on AccountNameInput (line 1880)
        const ulong ErrorMessageTextId = 989223559; // line 1125, TMP text on ErrorMessageText (line 580)
        const ulong StartButtonId = 2065114786; // line 1126, Button on StartButton (line 1696)
        // Lines 1127-1129: the three values.
        const int ServerPort = 7777;
        const int ConnectTimeoutMs = 3000;
        const string DefaultHost = "127.0.0.1";
        // Line 376 「--- !u!114 &324360351」, the Button on QuitButton (line 351); lines 420-422 its OnQuitClicked call.
        const ulong QuitButtonId = 324360351;

        // T3: the controller is the base scene's component and its four references point at the base scene's objects.
        [Test]
        public void T3_ControllerReferences_PointAtTheBaseSceneObjects()
        {
            using var menu = new MainMenuSceneFixture();
            var serialized = new SerializedObject(menu.Controller);
            Assert.AreEqual(ControllerId, FileId(menu.Controller), "MainMenuController is not the base scene's component");

            var problems = new List<string>();
            ExpectReference(serialized, "_serverHostInputField", ServerHostInputId, problems);
            ExpectReference(serialized, "_accountNameInputField", AccountNameInputId, problems);
            ExpectReference(serialized, "_errorMessageText", ErrorMessageTextId, problems);
            ExpectReference(serialized, "_startButton", StartButtonId, problems);

            Assert.IsEmpty(problems, "T3 the controller keeps the base scene's four references");
        }

        // T3: the three serialized values keep the base scene's values.
        [Test]
        public void T3_ControllerValues_KeepTheBaseSceneValues()
        {
            using var menu = new MainMenuSceneFixture();
            var serialized = new SerializedObject(menu.Controller);
            SerializedProperty port = serialized.FindProperty("_serverPort");
            SerializedProperty timeout = serialized.FindProperty("_connectTimeoutMs");
            SerializedProperty host = serialized.FindProperty("_defaultHost");
            Assert.IsNotNull(port, "MainMenuController lost its serialized _serverPort");
            Assert.IsNotNull(timeout, "MainMenuController lost its serialized _connectTimeoutMs");
            Assert.IsNotNull(host, "MainMenuController lost its serialized _defaultHost");

            Assert.AreEqual(ServerPort, port.intValue, "_serverPort");
            Assert.AreEqual(ConnectTimeoutMs, timeout.intValue, "_connectTimeoutMs");
            Assert.AreEqual(DefaultHost, host.stringValue, "_defaultHost");
        }

        // T3: the start button calls only OnStartClicked and the base quit button only OnQuitClicked, both on the controller.
        [Test]
        public void T3_StartAndQuitButtons_KeepTheirPersistentControllerCalls()
        {
            using var menu = new MainMenuSceneFixture();
            var start = new SerializedObject(menu.Controller).FindProperty("_startButton")?.objectReferenceValue as Button;
            Button[] quits = menu.Scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Button>(true))
                .Where(button => FileId(button) == QuitButtonId).ToArray();
            Assert.IsNotNull(start, "MainMenu.unity leaves _startButton unassigned");
            Assert.AreEqual(1, quits.Length, $"MainMenu.unity has {quits.Length} Buttons with the base quit button's file ID {QuitButtonId}");

            CollectionAssert.AreEqual(new (Object, string)[] { (menu.Controller, nameof(MainMenuController.OnStartClicked)) },
                PersistentCalls(start), "start button persistent onClick calls");
            CollectionAssert.AreEqual(new (Object, string)[] { (menu.Controller, nameof(MainMenuController.OnQuitClicked)) },
                PersistentCalls(quits[0]), "quit button persistent onClick calls");
        }

        static void ExpectReference(SerializedObject controller, string field, ulong expected, List<string> problems)
        {
            SerializedProperty property = controller.FindProperty(field);
            if (property == null)
            {
                problems.Add($"MainMenuController lost its serialized {field}");
                return;
            }
            Object target = property.objectReferenceValue;
            if (target == null) problems.Add($"{field} is unassigned, the base scene points it at file ID {expected}");
            else if (FileId(target) != expected) problems.Add($"{field} points at {target.name} file ID {FileId(target)}, the base scene's {expected}");
        }

        // The object's local file ID inside the saved scene, the number after 「&」 in the scene file.
        static ulong FileId(Object sceneObject) => GlobalObjectId.GetGlobalObjectIdSlow(sceneObject).targetObjectId;

        static (Object, string)[] PersistentCalls(Button button) => Enumerable.Range(0, button.onClick.GetPersistentEventCount())
            .Select(index => (button.onClick.GetPersistentTarget(index), button.onClick.GetPersistentMethodName(index)))
            .ToArray();
    }
}
