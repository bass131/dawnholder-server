using System;
using System.Reflection;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // The visible ID/PW submit sources must represent one start request in a frame, without adding a second
    // path to the scene button. A recording callback observes the view boundary before the controller's
    // probe lock could hide duplicate requests. The real scene is opened and closed without saving.
    public sealed class MainMenuLoginIndependentSubmitTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void BothFieldsSubmittingInOneFrame_RequestStartOnlyOnce(bool accountFirst)
        {
            using var menu = new MainMenuSceneFixture();
            int requests = 0;
            BuildView(menu.Controller, () => requests++);
            TMP_InputField account = Reference<TMP_InputField>(menu.Controller, "_accountNameInputField");
            TMP_InputField password = account.transform.parent.Find("PasswordInput")?.GetComponent<TMP_InputField>();
            Assert.IsNotNull(password, "B4: the view builds the visible PasswordInput submit source");

            TMP_InputField first = accountFirst ? account : password;
            TMP_InputField second = accountFirst ? password : account;
            first.onSubmit.Invoke(first.text);
            second.onSubmit.Invoke(second.text);
            first.onSubmit.Invoke(first.text);

            Assert.AreEqual(1, requests, "one frame's field submit notifications must request start once");
            TestContext.Out.WriteLine($"Same-frame field notifications=3, observed start requests={requests}");
        }

        static void BuildView(MainMenuController controller, Action requested)
        {
            Type view = typeof(MainMenuController).Assembly.GetType("Dawnholder.Client.UI.MainMenuLoginView", true);
            object instance = Activator.CreateInstance(view, BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[]
                {
                    Reference<TMP_InputField>(controller, "_accountNameInputField"),
                    Reference<TMP_InputField>(controller, "_serverHostInputField"),
                    Reference<Button>(controller, "_startButton"),
                    Reference<TMP_Text>(controller, "_errorMessageText"), requested,
                }, null);
            Assert.IsNotNull(instance, "fixture: construct the product view with the scene's original controls");
        }

        static T Reference<T>(MainMenuController controller, string name) where T : UnityEngine.Object
        {
            SerializedProperty property = new SerializedObject(controller).FindProperty(name);
            Assert.IsNotNull(property, $"fixture: serialized reference {name}");
            var value = property.objectReferenceValue as T;
            Assert.IsNotNull(value, $"fixture: assigned reference {name}");
            return value;
        }
    }
}
