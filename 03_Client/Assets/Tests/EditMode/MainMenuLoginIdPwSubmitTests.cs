using System;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // T4/B4: observe the view's start callback before the controller's probe lock can conceal duplicate submits.
    public sealed class MainMenuLoginIdPwSubmitTests
    {
        [TestCase("AccountNameInput", false)]
        [TestCase("PasswordInput", false)]
        [TestCase("AccountNameInput", true)]
        [TestCase("PasswordInput", true)]
        public void T4_FieldSubmit_RequestsStartExactlyOnce(string firstName, bool repeatSameFrame)
        {
            using var menu = new MainMenuSceneFixture();
            int requests = 0;
            Type view = typeof(MainMenuController).Assembly.GetType("Dawnholder.Client.UI.MainMenuLoginView", true);
            Activator.CreateInstance(view, BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { Reference<TMP_InputField>(menu.Controller, "_accountNameInputField"),
                    Reference<TMP_InputField>(menu.Controller, "_serverHostInputField"),
                    Reference<Button>(menu.Controller, "_startButton"),
                    Reference<TMP_Text>(menu.Controller, "_errorMessageText"), (Action)(() => requests++) }, null);
            TMP_InputField[] fields = menu.Scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<TMP_InputField>(true)).ToArray();
            TMP_InputField first = fields.SingleOrDefault(field => field.name == firstName);
            Assert.IsNotNull(first, $"B4: {firstName} missing");
            first.onSubmit.Invoke(first.text);
            if (repeatSameFrame)
            {
                first.onSubmit.Invoke(first.text);
                TMP_InputField other = fields.SingleOrDefault(field => field.name ==
                    (firstName == "AccountNameInput" ? "PasswordInput" : "AccountNameInput"));
                Assert.IsNotNull(other, "B4: second visible submit field missing");
                other.onSubmit.Invoke(other.text);
            }
            TestContext.Out.WriteLine($"first={firstName} repeatSameFrame={repeatSameFrame} requests={requests}");
            Assert.AreEqual(1, requests, "B4: one frame requests one start, observed before probe lock");
        }

        static T Reference<T>(MainMenuController controller, string field) where T : UnityEngine.Object
        {
            var property = new SerializedObject(controller).FindProperty(field);
            Assert.IsNotNull(property, $"fixture: {field}");
            var reference = property.objectReferenceValue as T;
            Assert.IsNotNull(reference, $"fixture: assigned {field}");
            return reference;
        }
    }
}
