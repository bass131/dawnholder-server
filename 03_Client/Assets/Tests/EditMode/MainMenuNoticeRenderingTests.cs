using System.Collections.Generic;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;

namespace Dawnholder.Client.Tests
{
    public sealed class MainMenuNoticeRenderingTests
    {
        // E1: the real menu rejects "ab" before probing; observe its resulting mesh, not only TMP_Text.text.
        [Test]
        public void InvalidAccountClick_DrawsEveryNonSpaceCharacterOfTheNameRule()
        {
            const string expected = "계정 이름은 영문 소문자, 숫자, 밑줄 3~16자로 입력해주세요";
            using var menu = new MainMenuSceneFixture();
            try
            {
                ViewLifecycle.Begin(menu.Controller.gameObject);
                TMP_InputField account = AccountField(menu);
                TMP_Text errorText = ErrorText(menu);
                account.text = "ab";

                menu.Controller.OnStartClicked();

                Assert.AreEqual(expected, errorText.text, "MainMenuController must show the specified name rule for ab");
                Assert.AreEqual(MainMenuNotice.InvalidAccountName, errorText.text);
                List<int> changedOrHidden = ObserveChangedOrHiddenCharacters(errorText, expected);
                Assert.That(changedOrHidden, Is.Empty,
                    "MainMenu error glyphs must preserve the notice; use characters present in its font and fallbacks");
            }
            finally
            {
                MainMenuNotice.TryTake(out _);
            }
        }

        // E2: the old sentence is a negative control for the same observer. Both U+00B7 positions must be caught.
        [Test]
        public void UnsupportedMiddleDots_AreDetectedAsChangedOrHiddenByTheSameObserver()
        {
            const string unsupported = "계정 이름은 영문 소문자·숫자·밑줄 3~16자로 입력해주세요";
            using var menu = new MainMenuSceneFixture();
            try
            {
                ViewLifecycle.Begin(menu.Controller.gameObject);
                TMP_Text errorText = ErrorText(menu);
                errorText.text = unsupported;

                List<int> changedOrHidden = ObserveChangedOrHiddenCharacters(errorText, unsupported);

                CollectionAssert.AreEqual(new[] { 13, 16 }, changedOrHidden,
                    "The control must detect exactly the two unsupported middle dots; inspect the logged source/rendered glyphs");
            }
            finally
            {
                MainMenuNotice.TryTake(out _);
            }
        }

        // E3: the real Start consumes the already-online notice; its mesh must preserve every non-space character.
        [Test]
        public void MenuStart_DrawsAndConsumesTheAlreadyOnlineNotice()
        {
            const string expected = "이미 접속 중인 계정입니다";
            using var menu = new MainMenuSceneFixture();
            try
            {
                TMP_Text errorText = ErrorText(menu);
                MainMenuNotice.Set(MainMenuNotice.AlreadyOnlineAccount);

                ViewLifecycle.Begin(menu.Controller.gameObject);

                Assert.AreEqual(expected, errorText.text, "MainMenu.Start must show the specified already-online sentence");
                bool noticeStillPending = MainMenuNotice.TryTake(out _);
                Assert.IsFalse(noticeStillPending, "MainMenu.Start must consume the pending notice");
                List<int> changedOrHidden = ObserveChangedOrHiddenCharacters(errorText, expected);
                Assert.That(changedOrHidden, Is.Empty,
                    "MainMenu error glyphs must preserve the notice; use characters present in its font and fallbacks");
            }
            finally
            {
                MainMenuNotice.TryTake(out _);
            }
        }

        static TMP_InputField AccountField(MainMenuSceneFixture menu)
        {
            SerializedProperty property = new SerializedObject(menu.Controller).FindProperty("_accountNameInputField");
            var field = property?.objectReferenceValue as TMP_InputField;
            Assert.IsNotNull(field, "fixture: MainMenu.unity must wire _accountNameInputField");
            return field;
        }

        static TMP_Text ErrorText(MainMenuSceneFixture menu)
        {
            SerializedProperty property = new SerializedObject(menu.Controller).FindProperty("_errorMessageText");
            var text = property?.objectReferenceValue as TMP_Text;
            Assert.IsNotNull(text, "fixture: MainMenu.unity must wire _errorMessageText");
            Assert.IsTrue(text.isActiveAndEnabled, "fixture: the real menu error label must be active");
            Assert.IsNotNull(text.font, "fixture: the real menu error label must have its font");
            return text;
        }

        // These plain BMP sentences have no rich-text tags. Keep the source index, visible flag and rendered
        // character separate so a replacement space cannot masquerade as the original character being drawn.
        static List<int> ObserveChangedOrHiddenCharacters(TMP_Text text, string source)
        {
            text.ForceMeshUpdate();
            TMP_TextInfo info = text.textInfo;
            Assert.AreEqual(source.Length, info.characterCount,
                "The real menu mesh must contain an entry for each source character; inspect scene text activation and parsing");
            var changedOrHidden = new List<int>();
            int observedNonSpaces = 0;
            TestContext.Out.WriteLine($"label={text.name} font={text.font.name} source='{source}' characters={info.characterCount}");
            for (int index = 0; index < source.Length; index++)
            {
                TMP_CharacterInfo character = info.characterInfo[index];
                Assert.AreEqual(index, character.index, "fixture: this plain sentence must retain its source indices");
                if (char.IsWhiteSpace(source[index])) continue;
                observedNonSpaces++;
                bool sameCharacter = character.character == source[index];
                bool visible = character.isVisible;
                bool sameGlyph = character.textElement != null && character.textElement.unicode == source[index];
                TestContext.Out.WriteLine($"index={index} source=U+{(int)source[index]:X4} " +
                    $"rendered=U+{(int)character.character:X4} visible={visible} sameGlyph={sameGlyph}");
                if (!sameCharacter || !visible || !sameGlyph) changedOrHidden.Add(index);
            }
            Assert.Greater(observedNonSpaces, 0, "The observer must inspect a non-empty notice, not pass with zero glyphs");
            TestContext.Out.WriteLine($"observedNonSpaces={observedNonSpaces} changedOrHidden=[{string.Join(",", changedOrHidden)}]");
            return changedOrHidden;
        }
    }
}
