using System;
using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Dawnholder.Client.Tests.BrightUiKitTestSupport;
using Phase = Dawnholder.Client.UI.CharacterMenuViewState.Phase;
using Slot = Dawnholder.Client.UI.CharacterMenuViewState.Slot;

namespace Dawnholder.Client.Tests
{
    // The bright character select, written before the implementation (goal 2026-10-29-inventory-character-select-look
    // 완료조건 1·3·5; user decision 51 A; decision 45; contract pr2-red-contract-v4 T6·T8). CharacterMenuViewTests keeps
    // the Content behaviour and wording; this file adds what the new look must show. Images are found by the GUID of
    // their sprite's source asset, never by object name. Expected pieces come from the approved spec rows and the
    // existing Knight/Mage idle art GUIDs, not from product code.
    public sealed class CharacterMenuViewAppearanceTests
    {
        const string KnightIdlePath = "Assets/Art/Characters/Playable/Knight/Knight_Idle.png";
        const string KnightIdleGuid = "3746a00054fb99846ba1375428e1d37a";
        const string MageIdlePath = "Assets/Art/Characters/Playable/Mage/Mage_Idle.png";
        const string MageIdleGuid = "3bc44f695dc715e45bf56d950d3a0052";
        // UI units; only absorbs float rounding through transforms.
        const float Tolerance = 0.5f;

        // Content boundary msg_3a2dfa53e814 (goal 「건드릴 곳과 소유」): names other code and tests find under Panel.
        // Goal 2026-10-31-demo-autopilot-character-select A1 removed the details box; its 「설계 고정」 adds Create and Cancel.
        static readonly string[] s_contentPaths =
        {
            "Header", "Slot1", "Slot2", "Slot3", "Refresh", "Submit", "Status", "Result",
            "CreateForm/Name", "CreateForm/NameRule", "CreateForm/Create", "CreateForm/Cancel",
        };

        GameObject _root;
        CharacterMenuView _view;

        Transform Panel => _root.transform.Find("CharacterMenu/Panel");

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Character menu appearance under test", typeof(RectTransform));
            _view = new CharacterMenuView(_root, _ => { }, (_, _, _) => { }, () => { }, () => { });
        }

        [TearDown]
        public void TearDown()
        {
            _view?.Dispose();
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
        }

        // T6 조각: Panel and Header show the bright Panel_Default and Header. The details box (once Inset_Default) is gone
        // (goal 2026-10-31-demo-autopilot-character-select A1).
        [Test]
        public void PanelAndHeader_ShowTheBrightPieces_AndNoDetailsBox()
        {
            string panel = PieceOf(Panel.GetComponent<Image>()?.sprite)?.Name;
            string header = PieceOf(Panel.Find("Header").GetComponent<Image>()?.sprite)?.Name;

            Assert.AreEqual("Panel_Default", panel, "T6 the panel background is the bright Panel_Default");
            Assert.AreEqual("Header", header, "T6 the header is the bright Header");
            Assert.IsNull(Panel.Find("DetailsBackground"), "A1 the details box is gone");
        }

        // T6 조각: Back and Refresh show the bright Button_Default states, Submit the bright Button_Accept states
        // (CharacterMenuView builds Submit with ButtonStyle.Accept).
        [Test]
        public void BackRefreshAndSubmit_ShowTheBrightButtonStates()
        {
            var problems = new List<string>();
            problems.AddRange(ButtonViolations(Control("Back"), "Default").Select(p => "Back: " + p));
            problems.AddRange(ButtonViolations(Control("Refresh"), "Default").Select(p => "Refresh: " + p));
            problems.AddRange(ButtonViolations(Control("Submit"), "Accept").Select(p => "Submit: " + p));
            TestContext.Out.WriteLine(string.Join("\n", problems));

            Assert.IsEmpty(problems, "T6 Back/Refresh use the bright Default states and Submit the bright Accept states");
        }

        // T6 조각: the name input stands on the bright InputField.
        [Test]
        public void NameInput_StandsOnTheBrightInputField()
        {
            var input = Panel.Find("CreateForm/Name").GetComponent<TMP_InputField>();
            var background = input.targetGraphic as Image;

            Assert.IsNotNull(background, "fixture: the name input's target graphic is an Image");
            Assert.AreEqual("InputField", PieceOf(background.sprite)?.Name, "T6 the name input background is the bright InputField");
        }

        // T6 자리: the three slots stand side by side (same y, three x values in slot order).
        [Test]
        public void Slots_StandSideBySide()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Rect[] slots = SlotRects();
            TestContext.Out.WriteLine("slots " + string.Join("; ", slots));

            Assert.AreEqual(slots[0].center.y, slots[1].center.y, Tolerance, "T6 Slot1 and Slot2 are on one row");
            Assert.AreEqual(slots[0].center.y, slots[2].center.y, Tolerance, "T6 Slot1 and Slot3 are on one row");
            Assert.Greater(slots[1].center.x, slots[0].center.x + Tolerance, "T6 Slot2 is right of Slot1");
            Assert.Greater(slots[2].center.x, slots[1].center.x + Tolerance, "T6 Slot3 is right of Slot2");
        }

        // T6 자리 and 51 A 빈 발판: every slot, empty or not, has a bright Pedestal below its centre and stands on the
        // bright CharacterSlot_Default when it is not chosen.
        [Test]
        public void EverySlot_HasABrightPedestalBelowAndTheDefaultSlotPiece()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("가람", 0), new Slot("누리", 1)));
            Rect[] slots = SlotRects();
            Image[] pedestals = Visible().Where(image => IsPiece(image.sprite, "Pedestal")).ToArray();

            var problems = new List<string>();
            var used = new HashSet<Image>();
            for (int i = 0; i < 3; i++)
            {
                Rect slot = slots[i];
                Image pedestal = pedestals.FirstOrDefault(image => !used.Contains(image) && BelowWithin(RectOf(image), slot));
                if (pedestal == null) problems.Add($"Slot{i + 1} has no bright Pedestal below its centre");
                else used.Add(pedestal);
                if (!PiecesUnder(slot.center).Contains("CharacterSlot_Default"))
                    problems.Add($"Slot{i + 1} does not stand on the bright CharacterSlot_Default");
            }
            TestContext.Out.WriteLine($"pedestals={pedestals.Length} " + string.Join("; ", problems));

            Assert.IsEmpty(problems, "T6 each slot has its own bright Pedestal and the bright CharacterSlot_Default");
        }

        // T6 자리: the chosen slot shows the bright CharacterSlot_Selected and SelectionGlow; the others show neither.
        [Test]
        public void ChosenSlot_ShowsTheSelectedPieceAndGlow_OthersDoNot()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("가람", 0), new Slot("누리", 1)));
            Click("Slot2");
            Rect[] slots = SlotRects();
            Rect[] glows = Visible().Where(image => IsPiece(image.sprite, "SelectionGlow")).Select(RectOf).ToArray();

            var problems = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                bool chosen = i == 1;
                bool selectedPiece = PiecesUnder(slots[i].center).Contains("CharacterSlot_Selected");
                bool glow = glows.Any(rect => ContainsPoint(rect, slots[i].center));
                if (selectedPiece != chosen) problems.Add($"Slot{i + 1} CharacterSlot_Selected shown={selectedPiece}, chosen={chosen}");
                if (glow != chosen) problems.Add($"Slot{i + 1} SelectionGlow shown={glow}, chosen={chosen}");
            }
            TestContext.Out.WriteLine($"glows={glows.Length} " + string.Join("; ", problems));

            Assert.IsEmpty(problems, "T6 only the chosen slot shows CharacterSlot_Selected and SelectionGlow");
        }

        // T6 직업 그림: class 0 shows a sprite of Knight_Idle.png, class 1 of Mage_Idle.png (by file GUID, any sub-sprite);
        // an empty slot shows no class art.
        [Test]
        public void ClassArt_FollowsTheServerClass_AndAnEmptySlotShowsNone()
        {
            Assert.AreEqual(KnightIdlePath, AssetDatabase.GUIDToAssetPath(KnightIdleGuid), "fixture: Knight idle art GUID");
            Assert.AreEqual(MageIdlePath, AssetDatabase.GUIDToAssetPath(MageIdleGuid), "fixture: Mage idle art GUID");
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("가람", 0), new Slot("누리", 1)));
            Rect[] slots = SlotRects();

            string[] slot1 = ClassArtOver(slots[0]);
            string[] slot2 = ClassArtOver(slots[1]);
            string[] slot3 = ClassArtOver(slots[2]);
            TestContext.Out.WriteLine($"slot1=[{string.Join(",", slot1)}] slot2=[{string.Join(",", slot2)}] slot3=[{string.Join(",", slot3)}]");

            CollectionAssert.AreEqual(new[] { "Knight" }, slot1.Distinct().ToArray(), "T6 the class 0 slot shows Knight idle art only");
            CollectionAssert.AreEqual(new[] { "Mage" }, slot2.Distinct().ToArray(), "T6 the class 1 slot shows Mage idle art only");
            Assert.IsEmpty(slot3, "T6 / 51 A an empty slot shows no class art (empty pedestal)");
        }

        // T6 직업 그림: an unknown class shows no class art. Passes before the implementation (no art at all) and must
        // keep passing after it.
        [Test]
        public void UnknownClass_ShowsNoClassArt()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("확인대상", 9)));

            Assert.IsEmpty(ClassArtOver(SlotRects()[0]), "T6 an unknown class shows neither Knight nor Mage art");
        }

        // Goal 2026-10-31-demo-autopilot-character-select A1 and A3 replace 51 A's 「새 캐릭터」 title and fixed count of 18
        // texts: choosing an empty slot opens the create form, and the removed details wording does not come back.
        [Test]
        public void ChoosingAnEmptySlot_OpensTheCreateForm_WithoutTheDetailsWording()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready));
            Click("Slot1");
            bool formOpen = Panel.Find("CreateForm").gameObject.activeInHierarchy;
            TMP_Text[] texts = _root.GetComponentsInChildren<TMP_Text>(true);
            string[] names = texts.Select(text => text.name).ToArray();
            TestContext.Out.WriteLine($"form open={formOpen} texts={texts.Length} " +
                $"[{string.Join(", ", texts.Select(text => PathUnder(text.transform)))}]");

            Assert.IsTrue(formOpen, "A3 an empty slot opens the create form");
            CollectionAssert.DoesNotContain(names, "SelectedTitle", "A1 the selected title text is gone");
            CollectionAssert.DoesNotContain(names, "Details", "A1 the details text is gone");
        }

        // Decision 45 kept the details area free of face and icon images. Goal 2026-10-31-demo-autopilot-character-select
        // A1 removes that area; the name stays as decision 45's trace (main msg_e6ef913df2cd).
        [Test]
        public void Details_ShowNoFaceOrIconImages()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("가람", 0)));
            Click("Slot1");

            Assert.IsNull(Panel.Find("DetailsBackground"), "A1 the details box is gone");
        }

        // T6: raycasts are taken only by the buttons, the name input and the panel background (which already blocks
        // clicks behind the menu, CreatePanel raycastTarget true at the baseline).
        [Test]
        public void RaycastTargets_AreOnlyButtonsTheNameInputAndThePanel()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("가람", 0)));
            Click("Slot2");
            var panelImage = Panel.GetComponent<Image>();
            var nameInput = Panel.Find("CreateForm/Name").GetComponent<TMP_InputField>();
            string[] others = _root.GetComponentsInChildren<Graphic>(true)
                .Where(graphic => graphic.enabled && graphic.raycastTarget && !(graphic is TMP_SubMeshUI))
                .Where(graphic => graphic != panelImage && graphic != nameInput.targetGraphic && !IsButtonGraphic(graphic))
                .Select(graphic => PathUnder(graphic.transform)).ToArray();

            Assert.IsEmpty(others, "T6 only buttons, the name input and the panel background take raycasts");
        }

        // T6: the new art images (pedestals, slot pieces, glow, class art) take no raycast. They must be found first.
        [Test]
        public void NewArtImages_TakeNoRaycast()
        {
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("가람", 0), new Slot("누리", 1)));
            Click("Slot1");
            Image[] art = Visible().Where(image => IsNewArt(image) && !IsButtonGraphic(image)).ToArray();
            string[] kinds = art.Select(ArtName).Distinct().OrderBy(name => name, StringComparer.Ordinal).ToArray();
            string[] raycasting = art.Where(image => image.raycastTarget).Select(image => PathUnder(image.transform)).ToArray();
            TestContext.Out.WriteLine($"art=[{string.Join(",", kinds)}]");

            CollectionAssert.IsSupersetOf(kinds, new[] { "Knight", "Mage", "Pedestal", "SelectionGlow" },
                "precondition: pedestals, the glow and both class arts are shown");
            Assert.IsEmpty(raycasting, "T6 new art images are not raycast targets");
        }

        // Content boundary: the names and hierarchy other code and tests use stay as they are.
        [Test]
        public void ContentPaths_AreKept()
        {
            string[] missing = s_contentPaths.Where(path => Panel.Find(path) == null).ToArray();

            Assert.IsNotNull(Panel, "the menu keeps CharacterMenu/Panel");
            Assert.IsEmpty(missing, "the Content boundary names under the panel are kept");
        }

        // T8: with a slot chosen (selection effect active), Dispose still leaves no CharacterMenu root and no other
        // object behind in the scene.
        [Test]
        public void Dispose_WithAChosenSlot_LeavesNoMenuAndNoStrayObject()
        {
            _view.Dispose();
            UnityEngine.Object.DestroyImmediate(_root);
            var before = new HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
            _root = new GameObject("Character menu dispose under test", typeof(RectTransform));
            _view = new CharacterMenuView(_root, _ => { }, (_, _, _) => { }, () => { }, () => { });
            _view.Render(new CharacterMenuViewState(Phase.Ready, new Slot("가람", 0)));
            Click("Slot1");

            _view.Dispose();
            string[] stray = SceneManager.GetActiveScene().GetRootGameObjects()
                .Where(go => go != _root && !before.Contains(go)).Select(go => go.name).ToArray();

            Assert.IsNull(_root.transform.Find("CharacterMenu"), "T8 Dispose removes the CharacterMenu root");
            Assert.IsEmpty(stray, "T8 the selection effect leaves no other root object after Dispose");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Observers
        // ---------------------------------------------------------------------------------------------------------

        Button Control(string path) => Panel.Find(path).GetComponent<Button>();

        void Click(string path) => Control(path).onClick.Invoke();

        RectTransform PanelRect => (RectTransform)Panel;

        Rect[] SlotRects() => Enumerable.Range(1, 3).Select(n => InPanel((RectTransform)Panel.Find("Slot" + n))).ToArray();

        Rect RectOf(Image image) => InPanel(image.rectTransform);

        Image[] Visible() => Panel.GetComponentsInChildren<Image>(false)
            .Where(image => image.isActiveAndEnabled && image.sprite != null && image.color.a > 0f).ToArray();

        string[] PiecesUnder(Vector2 point) => Visible()
            .Where(image => PieceOf(image.sprite) != null && ContainsPoint(RectOf(image), point))
            .Select(image => PieceOf(image.sprite).Name).ToArray();

        // Class art over a slot: Knight/Mage idle sprites whose centre is within the slot's horizontal span.
        string[] ClassArtOver(Rect slot) => Visible()
            .Where(image => ClassOf(image.sprite) != null)
            .Where(image => RectOf(image).center.x >= slot.xMin - Tolerance && RectOf(image).center.x <= slot.xMax + Tolerance)
            .Select(image => ClassOf(image.sprite)).ToArray();

        static string ClassOf(Sprite sprite)
        {
            string guid = SourceGuid(sprite);
            return guid == KnightIdleGuid ? "Knight" : guid == MageIdleGuid ? "Mage" : null;
        }

        static bool IsNewArt(Image image)
        {
            string piece = PieceOf(image.sprite)?.Name;
            return ClassOf(image.sprite) != null || piece == "Pedestal" || piece == "SelectionGlow" ||
                piece == "CharacterSlot_Default" || piece == "CharacterSlot_Selected";
        }

        static string ArtName(Image image) => ClassOf(image.sprite) ?? PieceOf(image.sprite)?.Name;

        // A pedestal belongs to a slot when its centre is within the slot's horizontal span and below the slot centre.
        static bool BelowWithin(Rect pedestal, Rect slot) =>
            pedestal.center.x >= slot.xMin - Tolerance && pedestal.center.x <= slot.xMax + Tolerance &&
            pedestal.center.y < slot.center.y;

        static bool IsButtonGraphic(Graphic graphic)
        {
            var owner = graphic.GetComponentInParent<Selectable>(true);
            return owner != null && owner.targetGraphic == graphic;
        }

        // Normal sprite on the target graphic and SpriteSwap states from the same bright family.
        static List<string> ButtonViolations(Button button, string family)
        {
            var problems = new List<string>();
            var graphic = button.targetGraphic as Image;
            string normal = graphic != null ? PieceOf(graphic.sprite)?.Name : null;
            if (normal != $"Button_{family}_Normal") problems.Add($"shows {normal ?? "no bright piece"}, approved Button_{family}_Normal");
            if (button.transition != Selectable.Transition.SpriteSwap) problems.Add($"transition {button.transition}, approved SpriteSwap");
            SpriteState states = button.spriteState;
            ExpectState(states.highlightedSprite, $"Button_{family}_Highlighted", "highlighted", problems);
            ExpectState(states.pressedSprite, $"Button_{family}_Pressed", "pressed", problems);
            ExpectState(states.disabledSprite, $"Button_{family}_Disabled", "disabled", problems);
            return problems;
        }

        static void ExpectState(Sprite sprite, string expected, string state, List<string> problems)
        {
            string shown = PieceOf(sprite)?.Name;
            if (shown != expected) problems.Add($"{state} state shows {shown ?? "no bright piece"}, approved {expected}");
        }

        // A rectangle in the panel's local UI units, whatever the anchors, pivots or scale in between.
        Rect InPanel(RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 a = PanelRect.InverseTransformPoint(corners[0]);
            Vector3 c = PanelRect.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        static bool ContainsPoint(Rect r, Vector2 p) =>
            p.x >= r.xMin - Tolerance && p.x <= r.xMax + Tolerance && p.y >= r.yMin - Tolerance && p.y <= r.yMax + Tolerance;

        string PathUnder(Transform t)
        {
            var parts = new List<string>();
            for (Transform at = t; at != null && at != _root.transform; at = at.parent) parts.Insert(0, at.name);
            return string.Join("/", parts);
        }
    }
}
