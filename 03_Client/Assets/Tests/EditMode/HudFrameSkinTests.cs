using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Dawnholder.Client.Tests.BrightUiKitTestSupport;

namespace Dawnholder.Client.Tests
{
    // PR3 T7 (decision v3 A msg_79d2bf86419e, goal 2026-10-29-inventory-character-select-look 완료조건 10): the combat HUD
    // frames HudLayout puts on the real Scenes/99.UI/UI.unity come from the default skin, which now holds the bright kit.
    // The scene is opened next to the runner's scene and closed without saving, and only HudLayout.Awake runs, the way
    // UiWindowCompositionRenderTests runs PartyMemberHud, so no server, class or input binding is involved. Expected
    // pieces are the BrightUiKitTestSupport rows (approved spec and GUID tables), never read from the product's skin.
    public sealed class HudFrameSkinTests
    {
        const string UiScenePath = "Assets/Scenes/99.UI/UI.unity";
        // The brown B1 folder, read only by the control below.
        const string BrownFolder = "Assets/Art/UI/Components";

        readonly List<Action> _cleanups = new List<Action>();

        [TearDown]
        public void Teardown()
        {
            Exception first = null;
            for (int i = _cleanups.Count - 1; i >= 0; i--)
            {
                try { _cleanups[i](); }
                catch (Exception error) { first ??= error; }
            }
            _cleanups.Clear();
            if (first != null) throw first;
        }

        // Every listed HUD panel takes the skin's panel piece. The portrait cell and the skill slots both use the
        // bright Inset_Default; the portrait cell is behind the clipped backdrop and art.
        [Test]
        public void HudLayoutAwake_DrawsTheBrightPanelPortraitFrameAndSkillSlotPiecesOnTheRealUiScene()
        {
            string sceneBefore = Sha256(UiScenePath);
            HudLayout hud = OpenUiScene();
            var serialized = new SerializedObject(hud);
            Image[] panels = References<Image>(serialized, "_panels");
            RectTransform[] portraits = References<RectTransform>(serialized, "_portraits");
            Image[] slots = new[] { "_qSlot", "_eSlot" }
                .Select(field => (serialized.FindProperty(field).objectReferenceValue as RectTransform)?.GetComponent<Image>())
                .Where(slot => slot != null).ToArray();
            SessionTestTools.Call(hud, "Awake");
            // A2: new mask/art Images are content, not frame artwork. Judge the direct named frame independently.
            Dictionary<RectTransform, Image[]> frames = portraits.ToDictionary(portrait => portrait,
                portrait => portrait.Cast<Transform>().Where(child => child.name == "PortraitFrame")
                    .Select(child => child.GetComponent<Image>()).Where(image => image != null).ToArray());
            string[] wrongPanels = panels.Where(panel => !IsPiece(panel.sprite, "Panel_Default")).Select(Describe).ToArray();
            string[] unframed = portraits.Where(portrait => frames[portrait].Length != 1 ||
                frames[portrait][0].transform.GetSiblingIndex() != 0).Select(portrait => portrait.name).ToArray();
            string[] wrongFrames = frames.Values.SelectMany(added => added)
                .Where(frame => !IsPiece(frame.sprite, "Inset_Default")).Select(Describe).ToArray();
            string[] wrongSlots = slots.Where(slot => !IsPiece(slot.sprite, "Inset_Default")).Select(Describe).ToArray();
            TestContext.Out.WriteLine($"hud panels={panels.Length} [{string.Join(", ", panels.Select(Describe))}]");
            TestContext.Out.WriteLine($"hud frames={frames.Values.Sum(added => added.Length)} " +
                $"[{string.Join(", ", frames.Values.SelectMany(added => added).Select(Describe))}]");
            TestContext.Out.WriteLine($"hud skill slots={slots.Length} [{string.Join(", ", slots.Select(Describe))}]");
            CloseScenes();

            Assert.Greater(panels.Length, 0, "precondition: HudLayout lists HUD panels in the scene");
            Assert.Greater(portraits.Length, 0, "precondition: HudLayout lists portraits in the scene");
            Assert.AreEqual(2, slots.Length, "precondition: the Q and E skill slots carry an Image");
            Assert.IsEmpty(unframed, "A2: every HUD portrait has one direct PortraitFrame as its first sibling (behind the clipped art)");
            Assert.IsEmpty(wrongPanels, "every HUD panel shows the bright Panel_Default");
            Assert.IsEmpty(wrongFrames, "every HUD portrait frame shows the bright Inset_Default, the skill-slot piece");
            Assert.IsEmpty(wrongSlots, "the Q and E skill slots show the bright Inset_Default");
            Assert.AreEqual(sceneBefore, Sha256(UiScenePath), "the UI scene file is not written");
        }

        // Control (contract 「기대값을 바꾼 시험마다 일부러 틀린 조각」): the judge above rejects the brown piece of each
        // name and accepts the bright PR2 pieces. The bright PortraitFrame_128 arrives with PR3, so only its brown piece is
        // checked here; this control passes before and after the implementation.
        [Test]
        public void Control_BrightJudge_RejectsTheBrownPanelFrameAndInset_AndAcceptsTheBrightPanelAndInset()
        {
            string[] names = { "Panel_Default", "PortraitFrame_128", "Inset_Default" };
            Sprite[] brown = names.Select(name => AssetDatabase.LoadAssetAtPath<Sprite>(BrownFolder + "/" + name + ".png")).ToArray();
            Sprite brightPanel = AssetDatabase.LoadAssetAtPath<Sprite>(Named("Panel_Default").AssetPath);
            Sprite brightInset = AssetDatabase.LoadAssetAtPath<Sprite>(Named("Inset_Default").AssetPath);

            Assert.AreEqual(names.Length, brown.Count(sprite => sprite != null), "fixture: the three brown pieces load");
            Assert.IsTrue(names.Select((name, i) => IsPiece(brown[i], name)).All(accepted => !accepted),
                "control: no brown piece passes as the bright piece of its name");
            Assert.IsTrue(IsPiece(brightPanel, "Panel_Default"), "control: the bright Panel_Default passes");
            Assert.IsTrue(IsPiece(brightInset, "Inset_Default"), "control: the bright Inset_Default passes");
        }

        // Additive next to the runner's scene, single if Unity refuses; closed without saving in CloseScenes.
        HudLayout OpenUiScene()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene;
            bool replaced = false;
            try
            {
                scene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Additive);
            }
            catch (Exception error)
            {
                TestContext.Out.WriteLine($"fixture: additive open refused ({error.Message}); opening the UI scene alone");
                scene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Single);
                replaced = true;
            }
            _cleanups.Add(() =>
            {
                if (replaced)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    return;
                }
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            });
            HudLayout[] huds = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HudLayout>(true)).ToArray();
            Assert.AreEqual(1, huds.Length, "fixture: one HudLayout in UI.unity");
            return huds[0];
        }

        void CloseScenes()
        {
            for (int i = _cleanups.Count - 1; i >= 0; i--) _cleanups[i]();
            _cleanups.Clear();
        }

        static T[] References<T>(SerializedObject serialized, string field) where T : UnityEngine.Object
        {
            SerializedProperty list = serialized.FindProperty(field);
            return Enumerable.Range(0, list.arraySize).Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue as T)
                .Where(reference => reference != null).ToArray();
        }

        static string Describe(Image image) => image.sprite == null
            ? $"{image.name}: no sprite"
            : $"{image.name}: '{image.sprite.name}' {SourceGuid(image.sprite) ?? "no GUID"}";

        static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }
    }
}
