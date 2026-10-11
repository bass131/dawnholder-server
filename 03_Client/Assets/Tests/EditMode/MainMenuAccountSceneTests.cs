using Dawnholder.Client.UI;
using NUnit.Framework;
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
    }
}
