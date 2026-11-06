using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Layout belongs to UI.unity. Read it before runtime icon fitting and close without saving.
    // Literal requirements: hud-portrait-login-rerecord goal G1/G2, approved HUD size table.
    public sealed class HudPanelSizingSceneTests
    {
        const string ScenePath = "Assets/Scenes/99.UI/UI.unity";
        const float Tolerance = 0.01f;
        Scene _previous;
        Scene _scene;
        bool _replaced;
        string _beforeHash;
        Transform _hud;

        [SetUp]
        public void Setup()
        {
            _beforeHash = SceneHash();
            _previous = SceneManager.GetActiveScene();
            _replaced = false;
            try
            {
                _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }
            catch (Exception error)
            {
                TestContext.Out.WriteLine("fixture: additive open refused: " + error.Message);
                _scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                _replaced = true;
            }
            HudLayout[] layouts = _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<HudLayout>(true)).ToArray();
            Assert.AreEqual(1, layouts.Length, "fixture: one HUD layout in the actual UI scene");
            _hud = layouts[0].transform;
        }

        [TearDown]
        public void Teardown()
        {
            if (_replaced)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else
            {
                if (_previous.IsValid() && _previous.isLoaded) SceneManager.SetActiveScene(_previous);
                if (_scene.IsValid() && _scene.isLoaded) EditorSceneManager.CloseScene(_scene, true);
            }
            Assert.AreEqual(_beforeHash, SceneHash(), "preservation: UI.unity was not saved");
        }

        [Test]
        public void G1_PartyPanelAndTitle_KeepTheirPlaceWithTheApprovedSize()
        {
            RectTransform party = Rect("Party_Info");
            RectTransform title = Rect("Party_Info/Info_Title");
            var errors = new List<string>();
            Vector(errors, new Vector2(520, 184), party.sizeDelta, "Party_Info size");
            Vector(errors, new Vector2(24, -196), party.anchoredPosition, "Party_Info position");
            Vector(errors, new Vector2(480, 40), title.sizeDelta, "Info_Title size");
            Vector(errors, new Vector2(20, -12), title.anchoredPosition, "Info_Title position");
            TMP_Text text = title.GetComponent<TMP_Text>();
            Assert.IsNotNull(text, "fixture: Info_Title has TMP text");
            Number(errors, 30, text.fontSize, "Info_Title font size");
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void G1_MemberRows_Are480By52AtTheTwoApprovedPositions()
        {
            RectTransform[] rows = Rows();
            var errors = new List<string>();
            Vector2[] positions = { new Vector2(20, -58), new Vector2(20, -118) };
            for (int index = 0; index < rows.Length; index++)
            {
                Vector(errors, new Vector2(480, 52), rows[index].sizeDelta, "row " + index + " size");
                Vector(errors, positions[index], rows[index].anchoredPosition, "row " + index + " position");
            }
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void G1_MemberText_Is32PointAndMidline()
        {
            var errors = new List<string>();
            foreach (RectTransform row in Rows())
            {
                TMP_Text text = row.Find("MemberInfo")?.GetComponent<TMP_Text>();
                Assert.IsNotNull(text, "fixture: each row has MemberInfo");
                Number(errors, 32, text.fontSize, text.name + " font size");
                Number(errors, 4096, (int)text.verticalAlignment, text.name + " Midline serialized enum");
            }
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void G2_AuthoredSkillIconsAndKeyLabels_FitTheSmallerSlots()
        {
            var errors = new List<string>();
            string[] keys = { "QKey", "EKey" };
            // The two assigned slots have authored icons and labels; slots 3/4 are intentionally empty.
            for (int index = 0; index < keys.Length; index++)
            {
                string slot = "skill_panel/skill_slot_" + (index + 1) + "/";
                RectTransform icon = Rect(slot + "icon");
                RectTransform key = Rect(slot + keys[index]);
                Vector(errors, new Vector2(66, 66), icon.sizeDelta, slot + "icon size");
                Vector(errors, new Vector2(0, 8), icon.anchoredPosition, slot + "icon position");
                Vector(errors, new Vector2(62, 30), key.sizeDelta, slot + "key size");
                Vector(errors, new Vector2(0, -32), key.anchoredPosition, slot + "key position");
                TMP_Text text = key.GetComponent<TMP_Text>();
                Assert.IsNotNull(text, "fixture: authored key has TMP text");
                Number(errors, 26, text.fontSize, slot + "key font size");
            }
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [TestCase("status_icon1", 132f, -15f, 20f)]
        [TestCase("status_icon2", 116f, 0f, 13f)]
        public void PortraitCell_KeepsItsFrameBackdropAndArtGeometry(string portraitName, float artSize,
            float artX, float artY)
        {
            RectTransform portrait = Rect("character_status/" + portraitName);
            Sprite original = portrait.GetComponent<Image>().sprite;
            Assert.IsNotNull(original, "fixture: the authored portrait head sprite");
            SessionTestTools.Call(_hud.GetComponent<HudLayout>(), "Awake");
            RectTransform[] portraits = { Rect("character_status/status_icon1"), Rect("character_status/status_icon2") };
            foreach (RectTransform other in portraits) other.gameObject.SetActive(other == portrait);
            for (Transform node = portrait; node != null; node = node.parent) node.gameObject.SetActive(true);
            Assert.IsTrue(portrait.gameObject.activeInHierarchy, "fixture: the selected class portrait is active");
            var errors = new List<string>();
            Vector(errors, new Vector2(112, 112), portrait.rect.size, "portrait size");
            Vector(errors, new Vector2(22, -22), portrait.anchoredPosition, "portrait position");
            var frame = portrait.Find("PortraitFrame") as RectTransform;
            var clip = portrait.Find("PortraitClip") as RectTransform;
            Assert.IsNotNull(frame, "Portrait: direct PortraitFrame is required");
            Assert.IsNotNull(clip, "Portrait: direct PortraitClip is required");
            Number(errors, 0, frame.GetSiblingIndex(), "frame first sibling");
            Number(errors, 1, clip.GetSiblingIndex(), "clip next sibling");
            Vector(errors, Vector2.zero, frame.anchorMin, "frame stretch anchorMin");
            Vector(errors, Vector2.one, frame.anchorMax, "frame stretch anchorMax");
            Vector(errors, Vector2.zero, frame.offsetMin, "frame stretch offsetMin");
            Vector(errors, Vector2.zero, frame.offsetMax, "frame stretch offsetMax");
            Vector(errors, new Vector2(98, 98), clip.rect.size, "clip size");
            Vector(errors, Vector2.zero, clip.anchoredPosition, "clip centre");
            var backdrop = clip.Find("PortraitBackdrop") as RectTransform;
            var art = clip.Find("PortraitArt") as RectTransform;
            Assert.IsNotNull(backdrop, "Portrait: backdrop is required");
            Assert.IsNotNull(art, "Portrait: art is required");
            Number(errors, 0, backdrop.GetSiblingIndex(), "backdrop first child");
            Number(errors, 1, art.GetSiblingIndex(), "art next child");
            Vector(errors, new Vector2(98, 98), backdrop.rect.size, "backdrop fills clip");
            Vector(errors, Vector2.zero, backdrop.anchoredPosition, "backdrop centre");
            Image backdropImage = backdrop.GetComponent<Image>();
            Assert.IsNotNull(backdropImage, "Portrait: backdrop Image is required");
            Assert.IsTrue(backdropImage.isActiveAndEnabled, "Portrait: backdrop is drawn");
            Assert.IsNull(backdropImage.sprite, "Portrait: backdrop is plain colour");
            Assert.AreEqual(new Color32(61, 44, 32, 255), (Color32)backdropImage.color, "Portrait: backdrop colour");
            Image artImage = art.GetComponent<Image>();
            Assert.IsNotNull(artImage, "Portrait: art Image is required");
            Assert.AreSame(original, artImage.sprite, "Portrait: keep the scene's head sprite");
            Vector(errors, new Vector2(artSize, artSize), art.rect.size, "art size");
            Vector(errors, new Vector2(artX, artY), art.anchoredPosition, "art offset from clip centre");
            Vector(errors, new Vector2(0.5f, 0.5f), art.anchorMin, "art centre anchorMin");
            Vector(errors, new Vector2(0.5f, 0.5f), art.anchorMax, "art centre anchorMax");
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        RectTransform Rect(string path)
        {
            var rect = _hud.Find(path) as RectTransform;
            Assert.IsNotNull(rect, "fixture: actual scene path " + path);
            return rect;
        }

        RectTransform[] Rows()
        {
            RectTransform[] rows = Rect("Party_Info").Cast<Transform>()
                .Where(child => child.name == "MemberStatus").Cast<RectTransform>()
                .OrderByDescending(row => row.anchoredPosition.y).ToArray();
            Assert.AreEqual(2, rows.Length, "fixture: two authored member rows");
            return rows;
        }

        static void Vector(List<string> errors, Vector2 expected, Vector2 actual, string label)
        {
            TestContext.Out.WriteLine($"{label}: expected={expected} observed={actual}");
            if (Mathf.Abs(expected.x - actual.x) > Tolerance || Mathf.Abs(expected.y - actual.y) > Tolerance)
                errors.Add($"{label}: expected={expected} observed={actual}");
        }

        static void Number(List<string> errors, float expected, float actual, string label)
        {
            TestContext.Out.WriteLine($"{label}: expected={expected} observed={actual}");
            if (Mathf.Abs(expected - actual) > Tolerance)
                errors.Add($"{label}: expected={expected} observed={actual}");
        }

        static string SceneHash()
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(ScenePath)));
        }
    }
}
