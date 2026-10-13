using Dawnholder.Client.UI;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;

namespace Dawnholder.Client.Tests
{
    // Goal T10 (completion condition 8) on the real MainMenu scene asset. Scene wiring is Unity serialized data, so
    // it is read the way Unity stores it (SerializedObject by field name, as HudGoldMirrorTests does).
    public sealed class MainMenuAccountSceneTests
    {
        // T10: MainMenuController in MainMenu.unity has its own account name TMP_InputField, active in the menu.
        [Test]
        public void T10_MainMenuScene_WiresAnAccountNameInputField()
        {
            using var menu = new MainMenuSceneFixture();
            var serialized = new SerializedObject(menu.Controller);
            SerializedProperty host = serialized.FindProperty("_serverHostInputField");
            SerializedProperty account = serialized.FindProperty("_accountNameInputField");
            Assert.IsNotNull(host, "fixture: MainMenuController keeps _serverHostInputField");

            Assert.IsNotNull(account, "MainMenuController has no serialized _accountNameInputField");
            var accountField = account.objectReferenceValue as TMP_InputField;
            Assert.IsNotNull(accountField, "MainMenu.unity leaves _accountNameInputField unassigned");
            Assert.AreNotSame(host.objectReferenceValue, accountField, "the account field reuses the server address field");
            Assert.AreEqual(menu.Scene, accountField.gameObject.scene);
            Assert.IsTrue(accountField.gameObject.activeInHierarchy, "the account field is inactive in the menu");
        }

        // T10: a login rejection notice left for the menu shows in its error text when the menu starts, and is
        // consumed. The menu's own Awake/Start run through ViewLifecycle; the scene is never saved.
        [Test]
        public void T10_MenuStart_ShowsAndConsumesThePendingLoginNotice()
        {
            const string notice = "이미 접속 중인 계정입니다";
            using var menu = new MainMenuSceneFixture();
            SerializedProperty error = new SerializedObject(menu.Controller).FindProperty("_errorMessageText");
            var errorText = error?.objectReferenceValue as TMP_Text;
            Assert.IsNotNull(errorText, "fixture: MainMenu.unity wires _errorMessageText");
            MainMenuNotice.Set(notice);
            try
            {
                ViewLifecycle.Begin(menu.Controller.gameObject);

                Assert.AreEqual(notice, errorText.text);
                Assert.IsFalse(MainMenuNotice.TryTake(out _), "the notice was shown but left pending");
            }
            finally
            {
                MainMenuNotice.TryTake(out _);
            }
        }

        // Screen check S-1: every MainMenuNotice sentence shows in the menu's error text. Its font has a static atlas, so a
        // character missing from it and its fallbacks renders blank. Both sentences the goal names must be found.
        [Test]
        public void S1_EveryMenuNotice_HasAllItsCharactersInTheErrorTextFont()
        {
            using var menu = new MainMenuSceneFixture();
            SerializedProperty error = new SerializedObject(menu.Controller).FindProperty("_errorMessageText");
            var errorText = error?.objectReferenceValue as TMP_Text;
            Assert.IsNotNull(errorText, "fixture: MainMenu.unity wires _errorMessageText");

            TMP_FontAsset font = errorText.font;
            Assert.IsNotNull(font, "fixture: the menu error text has a font");

            var notices = typeof(MainMenuNotice).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .ToDictionary(field => field.Name, field => (string)field.GetRawConstantValue());
            var problems = new List<string>();
            foreach (string name in new[] { "InvalidAccountName", "AlreadyOnlineAccount" })
            {
                if (!notices.ContainsKey(name)) problems.Add($"missing notice constant {name}");
            }

            foreach (var notice in notices)
            {
                string name = notice.Key;
                foreach (char c in notice.Value)
                {
                    if (!font.HasCharacter(c, searchFallbacks: true, tryAddCharacter: false))
                        problems.Add($"{name}: U+{(int)c:X4} '{c}' is not in {font.name} or its fallbacks");
                }
            }

            Assert.That(problems, Is.Empty);
        }
    }
}
