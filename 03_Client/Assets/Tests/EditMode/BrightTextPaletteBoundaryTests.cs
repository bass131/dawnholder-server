using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dawnholder.Client.Gameplay;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Tests
{
    // The existing contrast tests permit any sufficiently dark colour. The palette contract separately requires
    // the inventory's existing ink or quiet ink, and matching Color/Color32 serialization for the scene labels.
    public sealed class BrightTextPaletteBoundaryTests
    {
        const string UiScenePath = "Assets/Scenes/99.UI/UI.unity";
        static readonly Color s_ink = new Color(0.26f, 0.17f, 0.09f, 1f);
        static readonly Color s_quietInk = new Color(0.45f, 0.33f, 0.2f, 1f);
        static readonly (string Path, int Count)[] s_sceneTargets =
        {
            ("HUD/character_status/ClassText", 1),
            ("HUD/character_status/HpText", 1),
            ("HUD/Gold_bar/GoldText", 1),
            ("HUD/Party_Info/Info_Title", 1),
            ("HUD/Party_Info/MemberStatus/MemberInfo", 2),
            ("HUD/Quest_Hud/Title", 1),
            ("HUD/Quest_Hud/Explain", 1),
            ("HUD/Quest_Hud/Prograss", 1),
            ("HUD/skill_panel/skill_slot_1/QKey", 1),
            ("HUD/skill_panel/skill_slot_2/EKey", 1),
        };

        [Test]
        public void SceneLabels_KeepApprovedInkThroughLayoutInitialization_AndBothSerializedColoursAgree()
        {
            byte[] before = File.ReadAllBytes(UiScenePath);
            string yaml = File.ReadAllText(UiScenePath);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                TMP_Text[] all = roots.SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true)).ToArray();
                TMP_Text[] targets = s_sceneTargets.SelectMany(target =>
                {
                    TMP_Text[] matches = all.Where(text => PathOf(text.transform) == target.Path).ToArray();
                    Assert.AreEqual(target.Count, matches.Length, "required scene labels: " + target.Path);
                    return matches;
                }).ToArray();
                Assert.AreEqual(11, targets.Length, "all required scene labels are observed");
                foreach (TMP_Text text in targets)
                {
                    RequireInk(text.color, s_ink, PathOf(text.transform));
                    ulong fileId = GlobalObjectId.GetGlobalObjectIdSlow(text).targetObjectId;
                    Match block = Regex.Match(yaml, @"(?ms)^--- !u!114 &" + fileId + @"\r?\n(.*?)(?=^--- !u!|\z)");
                    Assert.IsTrue(block.Success, "serialized TMP component exists for " + fileId);
                    Match packed = Regex.Match(block.Groups[1].Value,
                        @"m_fontColor32:\s*\r?\n\s+serializedVersion: 2\s*\r?\n\s+rgba: (\d+)");
                    Assert.IsTrue(packed.Success, "serialized Color32 exists for " + fileId);
                    RequirePackedColour(text.color, uint.Parse(packed.Groups[1].Value), PathOf(text.transform));
                }

                HudLayout[] layouts = roots.SelectMany(root => root.GetComponentsInChildren<HudLayout>(true)).ToArray();
                PartyMemberHud[] parties = roots.SelectMany(root => root.GetComponentsInChildren<PartyMemberHud>(true)).ToArray();
                Assert.AreEqual(1, layouts.Length);
                Assert.AreEqual(1, parties.Length);
                SessionTestTools.Call(layouts[0], "Awake");
                SessionTestTools.Call(parties[0], "Awake");
                foreach (TMP_Text text in targets) RequireInk(text.color, s_ink, PathOf(text.transform));
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
            CollectionAssert.AreEqual(before, File.ReadAllBytes(UiScenePath), "opening and observing never saves the scene");
        }

        [TestCase("NpcDialog")]
        [TestCase("PartyInvite")]
        [TestCase("DungeonReward")]
        public void BuiltWindowLabels_UseOnlyTheRequiredExistingInk(string windowKind)
        {
            Assert.IsNull(NpcDialogPanel.Instance, "no NPC singleton left by another test");
            Assert.IsNull(PartyInvitePopup.Instance, "no party singleton left by another test");
            Assert.IsNull(DungeonClearRewardWindow.Instance, "no reward singleton left by another test");
            var parent = new GameObject("Palette boundary fixture");
            try
            {
                Transform window;
                (string Name, Color Colour)[] expected;
                switch (windowKind)
                {
                    case "NpcDialog":
                        window = NpcDialogPanel.BuildRuntime(parent.transform).transform;
                        expected = new[] { ("DialogText", s_ink), ("CloseHintText", s_quietInk) };
                        break;
                    case "PartyInvite":
                        window = PartyInvitePopup.BuildRuntime(parent.transform).transform;
                        expected = new[] { ("InviteText", s_ink) };
                        break;
                    case "DungeonReward":
                        window = DungeonClearRewardWindow.BuildRuntime(parent.transform).transform;
                        expected = new[] { ("Summary", s_ink) }.Concat(
                            Enumerable.Range(0, DungeonClearRewards.MaxItemKinds).Select(row => ("Reward" + row, s_ink))).ToArray();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(windowKind));
                }
                TMP_Text[] labels = window.GetComponentsInChildren<TMP_Text>(true);
                foreach (var target in expected)
                {
                    TMP_Text[] found = labels.Where(label => label.name == target.Name).ToArray();
                    Assert.AreEqual(1, found.Length, "required label exists once: " + target.Name);
                    RequireInk(found[0].color, target.Colour, target.Name);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void Control_PaletteObserverRejectsANewDarkColour_White_AndTransparentInk()
        {
            Reject(() => RequireInk(new Color(0.1f, 0.1f, 0.1f, 1f), s_ink, "unapproved dark colour"));
            Reject(() => RequireInk(Color.white, s_ink, "old white"));
            Reject(() => RequireInk(new Color(s_ink.r, s_ink.g, s_ink.b, 0f), s_ink, "transparent ink"));
            RequireInk(s_ink, s_ink, "approved ink control");
            RequireInk(s_quietInk, s_quietInk, "approved quiet ink control");
        }

        [Test]
        public void Control_SerializedPairObserverRejectsWhiteAndReversedBytes()
        {
            Reject(() => RequirePackedColour(s_ink, uint.MaxValue, "stale white Color32"));
            Color32 bytes = s_ink;
            uint reversed = ((uint)bytes.r << 24) | ((uint)bytes.g << 16) | ((uint)bytes.b << 8) | bytes.a;
            Reject(() => RequirePackedColour(s_ink, reversed, "reversed Color32 bytes"));
        }

        static void RequireInk(Color actual, Color expected, string label)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.000001f), label + " red");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.000001f), label + " green");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.000001f), label + " blue");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.000001f), label + " alpha");
        }

        static void RequirePackedColour(Color colour, uint packed, string label)
        {
            // Use Unity's conversion itself; the scene's packed integer is the independent stored observation.
            Color32 channels = colour;
            uint expected = channels.r | ((uint)channels.g << 8) | ((uint)channels.b << 16) | ((uint)channels.a << 24);
            Assert.AreEqual(expected, packed, label + " serialized colour pair");
        }

        static void Reject(TestDelegate observation)
        {
            AssertionException error = Assert.Throws<AssertionException>(observation, "the observer must reject the wrong input");
            TestContext.Out.WriteLine("NEGATIVE CONTROL rejected: " + error.Message);
        }

        static string PathOf(Transform transform)
        {
            var names = new List<string>();
            for (Transform at = transform; at != null; at = at.parent) names.Add(at.name);
            names.Reverse();
            return string.Join("/", names);
        }
    }
}
