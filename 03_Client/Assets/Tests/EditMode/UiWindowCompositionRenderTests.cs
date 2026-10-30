using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Dawnholder.Client.Gameplay;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // Independent check of the 06 window art: what reaches the screen and the pointer. Requirement source:
    // 01_Phases/goals/2026-10-13-demo-video-art/ui-window-application-scope.md 「보존 계약」「완료조건」 1·2·4 and the
    // approved kit spec (Art/UI/Components/import-spec.json and the 33 .meta files). The structural reads already in
    // UiWindowAppearanceContractTests are not repeated.
    // R  The product canvases are drawn by a fixture camera into a render texture at 1920×1080 and at the project's
    //    1280×720 default window. Each approved piece a window draws is compared, in corner blocks no other graphic
    //    touches, with the same approved PNG drawn per spec (Sliced or Simple, multiplier 1, white) at the same screen
    //    rectangle. The reference never calls the product's art code; which piece is expected comes from the GUID table.
    // P  Pointer input goes through the window's own GraphicRaycaster and uGUI pointer events (enter, down, up, click),
    //    so a decoration that takes raycasts or covers a button is a lost click, not a geometry guess.
    // L  Hide, dispose, rebind and singleton boundaries are read through the same capture and raycast.
    // D  Controls run the same observers on fixture objects: plain colour, foreign sprite, halved border, tint,
    //    a decoration over a button and an opaque piece over text must each be reported. On the scene HUD canvas a
    //    raycastTarget probe must be hit while the reconstructed HUD's decoration stays noninteractive; an empty
    //    hit list counts only where the observer's explicit positive probe is shown to see that point.
    // Switching a product canvas from Overlay to ScreenSpaceCamera is part of the fixture: these captures are not the
    // game screen and not a before/after pair. PNGs are written only when DAWNHOLDER_UI_KIT_EVIDENCE_DIR is set.
    public sealed class UiWindowCompositionRenderTests
    {
        const string UiScenePath = "Assets/Scenes/99.UI/UI.unity";
        const string FontPath = "Fonts & Materials/LiberationSans SDF";
        const string EvidenceDirectoryVariable = "DAWNHOLDER_UI_KIT_EVIDENCE_DIR";
        // import-spec units.pixelDensity: PNG px per UI unit (sprite PPU 200 on a Canvas reference PPU of 100).
        const float Density = 2f;
        // Largest corner block in UI units; smaller pieces use their own border or half their size.
        const float CornerUnits = 8f;
        // Product and reference share the pipeline, sprite and rectangle; only sub-pixel placement may differ.
        const int MaxChannelTolerance = 24;
        const double MeanChannelTolerance = 4.0;
        // A channel difference above this counts as "something was drawn here".
        const int DrawnThreshold = 8;
        const string LongNpcLine =
            "대장장이: 이번에 들어온 운철은 쉽게 다룰 수 있는 물건이 아니라네. 망치질을 서두르면 결이 갈라지니, 재료를 넉넉히 챙겨서 다시 찾아오게나.";

        static readonly Vector2Int FullHd = new Vector2Int(1920, 1080);
        // ProjectSettings defaultScreenWidth/Height 1280×720 with resizableWindow 0: the player's fixed window.
        static readonly Vector2Int DefaultWindow = new Vector2Int(1280, 720);

        // One approved B1 piece: name, .meta GUID, spec image type and border L/T in PNG px. Copied from
        // Art/UI/Components/import-spec.json sprites[] and the .meta files at 14573c80, not from product code.
        sealed class Piece
        {
            internal readonly string Name;
            internal readonly string Guid;
            internal readonly bool Sliced;
            internal readonly bool PreserveAspect;
            internal readonly int BorderLeft;
            internal readonly int BorderTop;

            internal Piece(string name, string guid, bool sliced, bool preserveAspect, int borderLeft, int borderTop)
            {
                Name = name;
                Guid = guid;
                Sliced = sliced;
                PreserveAspect = preserveAspect;
                BorderLeft = borderLeft;
                BorderTop = borderTop;
            }
        }

        static readonly Piece[] Pieces =
        {
            new Piece("Panel_Default", "f5e7cab1fd4f4b9884c6d0f6454fccb6", true, false, 24, 24),
            new Piece("Panel_Emphasis", "7dec580f6ab94568af987e047f5a2c36", true, false, 24, 24),
            new Piece("Panel_Dialog", "40bd7251781d4d92a9fb572a4b350bc2", true, false, 24, 24),
            new Piece("Inset_Default", "baf557470603430e83015dc40371610a", true, false, 12, 12),
            new Piece("Inset_Selected", "e2c7e2ef02fc41d38527480b1023601b", true, false, 12, 12),
            new Piece("Inset_Disabled", "dd85577f51e84c7188a5a8d8a3b03517", true, false, 12, 12),
            new Piece("Header", "1371d63dc50f40ebae0726a4d60203b5", true, false, 16, 16),
            new Piece("Button_Default_Normal", "fb2ddb62027b4f0fa813440da5fe936b", true, false, 16, 16),
            new Piece("Button_Default_Highlighted", "27e1cc2d96474400b728f433782ec5d3", true, false, 16, 16),
            new Piece("Button_Default_Pressed", "8974b3847d14431b949a496b6cb1ae8f", true, false, 16, 16),
            new Piece("Button_Default_Disabled", "3283e29e3b0e48c48b364a8837a889bf", true, false, 16, 16),
            new Piece("Button_Accept_Normal", "304984e555104bf5b7d8648af717c165", true, false, 16, 16),
            new Piece("Button_Accept_Highlighted", "ce276490b85b4f7fa5f674b0f2546e87", true, false, 16, 16),
            new Piece("Button_Accept_Pressed", "0525c0bba70a41ad8bf71baebe25bc9f", true, false, 16, 16),
            new Piece("Button_Accept_Disabled", "296d9f9387434c4cbc07115da1352309", true, false, 16, 16),
            new Piece("Button_Decline_Normal", "2b776157d2a945229c5cb20bac70bcff", true, false, 16, 16),
            new Piece("Button_Decline_Highlighted", "1e8a01af6d0f46128d05c89866f20cd4", true, false, 16, 16),
            new Piece("Button_Decline_Pressed", "3e4ac54569094f8a9050a567ab5df3d8", true, false, 16, 16),
            new Piece("Button_Decline_Disabled", "ef2f8625204c492190b2d4c58a0121a1", true, false, 16, 16),
            new Piece("ItemSlot_Empty", "d5561f1d095f487a9fdcbd53febb2340", true, false, 16, 16),
            new Piece("ItemSlot_Filled", "e56f32f7404a4db188dd772d4fa46887", true, false, 16, 16),
            new Piece("ItemSlot_Selected", "7111abac3f5d47acb6bf5990fb04640c", true, false, 16, 16),
            new Piece("IconBase_64", "22c20957275a444890958a62543db49f", false, true, 0, 0),
            new Piece("IconBase_96", "855b3dc208e844c992f6e37547fbf506", false, true, 0, 0),
            new Piece("CircleButton_Normal", "474e3ae378cc4b9991476f044b9c884b", false, true, 0, 0),
            new Piece("CircleButton_Highlighted", "9f4a6e74cde643568a4e7b44ee97a246", false, true, 0, 0),
            new Piece("CircleButton_Pressed", "52260cf68ffe4c6e9dda1c42a7cdbcb0", false, true, 0, 0),
            new Piece("CircleButton_Disabled", "65e146fca12141b583582f207b9abca3", false, true, 0, 0),
            new Piece("NumberBadge", "a87faf39aa5c4bc992758bfa8b60ea00", false, true, 0, 0),
            new Piece("PortraitFrame_128", "953890eb7a1a468bb096fdfeff81234d", false, true, 0, 0),
            new Piece("Divider_Horizontal", "66a1c6ad27d746138fffd6631bc59c56", true, false, 16, 0),
            new Piece("Ornament_Corner", "db10b0962c964d5abacf20d000215a8c", false, true, 0, 0),
            new Piece("Ornament_HeaderEnd", "1c5736df500a4b60bc27adc75eaec82d", false, true, 0, 0),
        };

        // Declared after Pieces: static initializers run in text order.
        static readonly Dictionary<string, Piece> PiecesByGuid = Pieces.ToDictionary(piece => piece.Guid);

        // The approved bright kit the inventory moves to (decision 43 A, goal 2026-10-29-inventory-character-select-look
        // 완료조건 1). The inventory render tests filter by these 25 GUIDs instead of the brown 33, never both (Content
        // msg_b9b45522981b: no union; main msg_e873edba3d2d); other windows keep PiecesByGuid. Rows printed by
        // pr2-red-01/scripts/emit-bright-table-v2.ps1 from the approved spec; BrightUiKitTestSupport names the inputs.
        static readonly Piece[] BrightPieces =
        {
            new Piece("Panel_Default", "387817131a7ee7abd35357ff32e6cd2a", true, false, 24, 24),
            new Piece("Header", "7e18e6245ab44c8b34442c1876be07d3", true, false, 16, 16),
            new Piece("Inset_Default", "d86a90ca34d7ac84244096c673e46475", true, false, 12, 12),
            new Piece("Divider_Horizontal", "397c7e470f14e2989cc3622e25f75e24", true, false, 16, 0),
            new Piece("Button_Default_Normal", "218e99a5632015261381128b2909788b", true, false, 16, 16),
            new Piece("Button_Default_Highlighted", "8c51a6fac51dd8f61ef4cede5690f59c", true, false, 16, 16),
            new Piece("Button_Default_Pressed", "09e130da54a609f68834547b2363b2a6", true, false, 16, 16),
            new Piece("Button_Default_Disabled", "0438a51907e9b5791382a7beecdf53ad", true, false, 16, 16),
            new Piece("Button_Accept_Normal", "fdaa805e70f5533488dae81f7234282a", true, false, 16, 16),
            new Piece("Button_Accept_Highlighted", "b19f5a6d0300e566d0fffea8534ac8ca", true, false, 16, 16),
            new Piece("Button_Accept_Pressed", "819be5b406aaa976f19a17fb72ef6fae", true, false, 16, 16),
            new Piece("Button_Accept_Disabled", "d14f5c1a8a0197176bd076a96fb0009e", true, false, 16, 16),
            new Piece("ItemSlot_Empty", "e702897eaffb5b94a75480e1d24d43b4", true, false, 16, 16),
            new Piece("ItemSlot_Filled", "74be0b4adcf9029de03d21c58f39d95d", true, false, 16, 16),
            new Piece("ItemSlot_Selected", "d44be2e942c90002e105154dfa0f758a", true, false, 16, 16),
            new Piece("IconBase_64", "87b8e15f8cfe6c51f3820283d7a2f67a", false, true, 0, 0),
            new Piece("NumberBadge", "5efe8348bfdcd2327dfc5a5568c159d4", false, true, 0, 0),
            new Piece("CharacterSlot_Default", "b0d2304d8eeaeac834d357ce350b062c", true, false, 16, 16),
            new Piece("CharacterSlot_Selected", "39f038f1abf5257969dd43c5458e0634", true, false, 16, 16),
            new Piece("Pedestal", "54faccc43070f3ab735dae9c756f651b", false, true, 0, 0),
            new Piece("InputField", "38f801c703515178c835f249a4db2983", true, false, 16, 16),
            new Piece("SelectionGlow", "ae12c245de4289f6e0ba6cabc07b8bf7", false, true, 0, 0),
            new Piece("Icon_Material", "9f5082fa3c5d6e75b68675a2d3f1a491", false, true, 0, 0),
            new Piece("Icon_CurrencyPouch", "3aadc85f7b3e7ca0c8a31d52cc7910a8", false, true, 0, 0),
            new Piece("Icon_Coin", "87e028dcd3360e891febb86e8d4c61d8", false, true, 0, 0),
        };

        static readonly Dictionary<string, Piece> BrightPiecesByGuid = BrightPieces.ToDictionary(piece => piece.Guid);

        readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        readonly List<Action> _cleanups = new List<Action>();

        [SetUp]
        public void Setup()
        {
            Assert.IsNull(NpcDialogPanel.Instance, "fixture: another test left the NPC dialog singleton");
            Assert.IsTrue(PartyState.Instance == null, "fixture: another test left a published PartyState");
            Assert.IsNull(PartyInvitePopup.Instance, "fixture: another test left the party invite popup");
            string pipeline = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "built-in";
            TestContext.Out.WriteLine($"graphics device={SystemInfo.graphicsDeviceType} name={SystemInfo.graphicsDeviceName} " +
                $"colorSpace={PlayerSettings.colorSpace} pipeline={pipeline}");
            Assert.AreNotEqual(GraphicsDeviceType.Null, SystemInfo.graphicsDeviceType,
                "fixture: camera capture needs a graphics device; a -nographics run cannot pass this suite");
        }

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
            for (int i = _owned.Count - 1; i >= 0; i--)
                if (_owned[i] != null) UnityEngine.Object.DestroyImmediate(_owned[i]);
            _owned.Clear();
            if (first != null) throw first;
        }

        // ---------------------------------------------------------------------------------------------------------
        // D. Controls: the observers fail on known-wrong fixture objects
        // ---------------------------------------------------------------------------------------------------------

        // Approved Panel_Default drawn per spec passes; the pre-change inventory colour (InventoryPanelView at
        // 4728e493: 0.06, 0.07, 0.10, 0.88), the pre-change NPC sprite UI/Menu_Button at multiplier 3, a halved border
        // (multiplier 2, i.e. the meta border divided by the density) and a tint must each be reported.
        [Test]
        public void Control_CornerObserver_RejectsPlainColourForeignSpriteHalvedBorderAndTint()
        {
            CaptureRig rig = Rig(FullHd);
            Canvas canvas = FixtureCanvas(rig, "Corner control canvas");
            Image subject = FixtureImage(canvas.transform, "subject", new Vector2(400f, 300f), new Vector2(300f, 300f));
            Piece panel = PieceNamed("Panel_Default");
            Sprite approved = LoadPiece(panel);
            Sprite foreign = Resources.Load<Sprite>("UI/Menu_Button");
            Assert.IsNotNull(foreign, "fixture: the pre-change sprite Resources/UI/Menu_Button");

            var reported = new Dictionary<string, int>();
            void Variant(string name, Sprite sprite, float multiplier, Color colour)
            {
                subject.sprite = sprite;
                subject.type = Image.Type.Sliced;
                subject.pixelsPerUnitMultiplier = multiplier;
                subject.color = colour;
                Pixels drawn = rig.Capture();
                List<string> problems = DrawnPerSpec(rig, drawn, subject, panel, "control " + name, requireCompared: true);
                Log("control " + name, problems);
                reported[name] = problems.Count;
            }
            Variant("approved", approved, 1f, Color.white);
            Variant("plain colour", null, 1f, new Color(0.06f, 0.07f, 0.10f, 0.88f));
            Variant("foreign sprite", foreign, 3f, Color.white);
            Variant("halved border", approved, 2f, Color.white);
            Variant("tint", approved, 1f, new Color(0.7f, 0.85f, 0.7f, 1f));

            Assert.AreEqual(0, reported["approved"], "D the observer accepts the approved piece drawn per spec");
            Assert.Greater(reported["plain colour"], 0, "D a plain colour is reported");
            Assert.Greater(reported["foreign sprite"], 0, "D a foreign sprite is reported");
            Assert.Greater(reported["halved border"], 0, "D a halved border is reported");
            Assert.Greater(reported["tint"], 0, "D a tint is reported");
        }

        // A decoration that takes raycasts over a button takes the real click; with raycastTarget off the click
        // passes. Text under an opaque piece drawn after it is reported as hidden.
        [Test]
        public void Control_PointerAndTextObservers_ReportACoveredButtonAndCoveredText()
        {
            CaptureRig rig = Rig(FullHd);
            Canvas canvas = FixtureCanvas(rig, "Pointer control canvas");
            var raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            Image buttonImage = FixtureImage(canvas.transform, "button", new Vector2(120f, 40f), new Vector2(200f, 200f));
            buttonImage.sprite = LoadPiece(PieceNamed("Button_Default_Normal"));
            buttonImage.type = Image.Type.Sliced;
            buttonImage.raycastTarget = true;
            var button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            int clicks = 0;
            button.onClick.AddListener(() => clicks++);

            Vector2 centre = Centre(rig, buttonImage.rectTransform);
            GameObject open = Click(rig, raycaster, centre);
            int afterOpen = clicks;
            Image cover = FixtureImage(canvas.transform, "decoration", new Vector2(120f, 40f), new Vector2(200f, 200f));
            cover.color = new Color(1f, 1f, 1f, 0.3f);
            cover.raycastTarget = true;
            GameObject covered = Click(rig, raycaster, centre);
            int afterCovered = clicks;
            cover.raycastTarget = false;
            GameObject passed = Click(rig, raycaster, centre);
            int afterPassed = clicks;

            TMP_Text label = FixtureText(canvas.transform, "text", "가나다 확인", new Vector2(240f, 60f), new Vector2(600f, 600f));
            string[] hiddenWhenClear = HiddenTexts(rig, rig.Capture(), new[] { label });
            Image opaque = FixtureImage(canvas.transform, "opaque over text", new Vector2(240f, 60f), new Vector2(600f, 600f));
            opaque.color = Color.black;
            string[] hiddenWhenCovered = HiddenTexts(rig, rig.Capture(), new[] { label });

            Assert.AreSame(button.gameObject, open, "D an uncovered button takes the click");
            Assert.AreEqual(1, afterOpen, "D the uncovered click reaches the listener");
            Assert.AreNotSame(button.gameObject, covered, "D a raycast decoration over the button takes the click");
            Assert.AreEqual(1, afterCovered, "D the covered click does not reach the listener");
            Assert.AreSame(button.gameObject, passed, "D a decoration without raycastTarget passes the click");
            Assert.AreEqual(2, afterPassed, "D the passed click reaches the listener");
            Assert.IsEmpty(hiddenWhenClear, "D uncovered text is seen as drawn");
            Assert.IsNotEmpty(hiddenWhenCovered, "D text under an opaque piece is reported");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Inventory (InventoryPanelView ctor, the gameplay scene's builder)
        // ---------------------------------------------------------------------------------------------------------

        // Approved preview inventory-0 roles: Panel_Default behind the 550×354 panel, Button_Default states, an
        // Inset_*/ItemSlot_* piece under each slot row. Max legal state (Shared InventoryLimits and the snapshot rules
        // in client.md): currency 1,000,000,000, the two catalog items sorted with 99 each, six empty slots.
        // The inventory filters by the bright table (decision 43 A, goal 2026-10-29 완료조건 1, Content msg_b9b45522981b,
        // main msg_e873edba3d2d). T4: the roles must be found by that table, so the drawing checks are not run on an
        // empty set (lead msg_8d4e83bd3bb0).
        [TestCase(1920, 1080)]
        [TestCase(1280, 720)]
        public void Inventory_OnCamera_DrawsApprovedPiecesPerSpecAndKeepsTextReadableAtMaxLegalState(int width, int height)
        {
            CaptureRig rig = Rig(new Vector2Int(width, height));
            InventoryWindow window = BuildInventory(rig, _ => { }, () => { });

            Pixels built = rig.Capture();
            SaveScreen(rig, "inventory-built");
            var builtRoles = new List<string>();
            builtRoles.AddRange(RoleViolations(rig, window.Root.transform, window.Panel, "Panel_Default", BrightPiecesByGuid));
            builtRoles.AddRange(DrawnPerSpec(rig, built, Background(rig, window.Root.transform, window.Panel, BrightPiecesByGuid),
                PieceNamed("Panel_Default", BrightPieces), "inventory background", requireCompared: true));
            builtRoles.AddRange(ButtonStateViolations(window.Refresh, "Button_Default_Normal", BrightPiecesByGuid)
                .Select(p => "Refresh " + p));
            for (int i = 0; i < 8; i++)
                builtRoles.AddRange(ButtonStateViolations(window.Uses[i], "Button_Default_Normal", BrightPiecesByGuid)
                    .Select(p => $"Slot{i + 1}/Use " + p));
            builtRoles.AddRange(DrawnPerSpec(rig, built, TargetImage(window.Uses[0]), PieceNamed("Button_Default_Normal", BrightPieces),
                "Slot1/Use", requireCompared: true));
            string[] bareRows = window.Slots.Where(row => PiecesUnder(rig, window.Root.transform, Centre(rig, row), BrightPiecesByGuid,
                "Inset_Default", "Inset_Selected", "Inset_Disabled", "ItemSlot_Empty", "ItemSlot_Filled", "ItemSlot_Selected").Length == 0)
                .Select(row => row.name).ToArray();
            string[] builtPieces = ApprovedImages(window.Root.transform, BrightPiecesByGuid)
                .Select(image => ShownPiece(image, BrightPiecesByGuid).Name).ToArray();
            List<string> builtDrawing = AllPiecesDrawnPerSpec(rig, built, window.Root.transform, BrightPiecesByGuid);
            string[] builtHidden = HiddenTexts(rig, built, VisibleTexts(window.Root.transform));
            string[] builtMissing = MissingGlyphs(VisibleTexts(window.Root.transform));
            LogGeometry(rig, window);

            GameObject mirrorOwner = Own(new GameObject("Inventory mirror (fixture)"));
            mirrorOwner.SetActive(false);
            var mirror = mirrorOwner.AddComponent<InventoryState>();
            var slots = new InventorySlot[InventoryLimits.MaxSlots];
            slots[0] = new InventorySlot(ItemId.Material, InventoryLimits.MaxStack);
            slots[1] = new InventorySlot(ItemId.CoinPouch, InventoryLimits.MaxStack);
            Assert.AreEqual(InventorySnapshotApplyResult.Applied, mirror.ApplySnapshot(1, InventoryLimits.MaxCurrency, slots),
                "fixture: the max legal snapshot is accepted by the mirror");
            window.View.Render(mirror, null, true);
            Pixels max = rig.Capture();
            SaveScreen(rig, "inventory-max-legal");
            var maxRoles = new List<string>();
            maxRoles.AddRange(ButtonStateViolations(window.Refresh, "Button_Default_Disabled", BrightPiecesByGuid)
                .Select(p => "Refresh " + p));
            maxRoles.AddRange(ButtonStateViolations(window.Uses[1], "Button_Default_Disabled", BrightPiecesByGuid)
                .Select(p => "Slot2/Use " + p));
            maxRoles.AddRange(DrawnPerSpec(rig, max, TargetImage(window.Uses[1]), PieceNamed("Button_Default_Disabled", BrightPieces),
                "Slot2/Use disabled", requireCompared: true));
            List<string> maxDrawing = AllPiecesDrawnPerSpec(rig, max, window.Root.transform, BrightPiecesByGuid);
            TMP_Text[] maxTexts = VisibleTexts(window.Root.transform);
            string[] maxHidden = HiddenTexts(rig, max, maxTexts);
            string[] maxMissing = MissingGlyphs(maxTexts);
            Log("inventory max texts", maxTexts.Select(t => $"{t.name}: 「{t.text}」 truncated={t.isTextTruncated} overflowing={t.isTextOverflowing}"));

            Log("inventory built roles", builtRoles);
            Log("inventory built bright pieces", builtPieces);
            Log("inventory built drawing", builtDrawing);
            Log("inventory max roles", maxRoles);
            Log("inventory max drawing", maxDrawing);
            Assert.GreaterOrEqual(builtPieces.Count(name => name == "Panel_Default"), 1, "R T4 the bright table finds the window background");
            Assert.GreaterOrEqual(builtPieces.Count(name => name == "Header"), 1, "R T4 the bright table finds the title band");
            Assert.GreaterOrEqual(builtPieces.Count(name => name.StartsWith("ItemSlot_", StringComparison.Ordinal) ||
                    name.StartsWith("Inset_", StringComparison.Ordinal)), 8,
                "R T4 the bright table finds a slot piece for each of the eight slots");
            Assert.GreaterOrEqual(builtPieces.Count(name => name.StartsWith("Button_Default_", StringComparison.Ordinal)), 9,
                "R T4 the bright table finds the refresh and the eight use buttons");
            Assert.IsEmpty(builtRoles, "R panel and button pieces as approved and drawn per spec when built");
            Assert.IsEmpty(bareRows, "R every slot row is drawn on an approved Inset_*/ItemSlot_* piece");
            Assert.IsEmpty(builtDrawing, "R every approved piece the inventory draws matches the spec drawing");
            Assert.IsEmpty(builtHidden, "R built texts are drawn over the art");
            Assert.IsEmpty(builtMissing, "R built texts resolve every glyph through the font and its Korean fallback");
            Assert.IsEmpty(maxRoles, "R the disabled refresh and coin pouch use show Button_Default_Disabled");
            Assert.IsEmpty(maxDrawing, "R every approved piece matches the spec drawing at the max legal state");
            Assert.IsEmpty(maxHidden, "R max legal state texts are drawn over the art");
            Assert.IsEmpty(maxMissing, "R max legal state texts resolve every glyph");
        }

        // Scope 「보존 계약」: only buttons take input; their hover/press/disabled states are the approved sprites; a hidden
        // panel takes nothing; Dispose leaves no callbacks. Pointer input goes through the panel's own raycaster.
        // The inventory filters by the bright table (decision 43 A, goal 2026-10-29 완료조건 1, Content msg_b9b45522981b,
        // main msg_e873edba3d2d); T4: the decoration points must not be empty (lead msg_8d4e83bd3bb0).
        [Test]
        public void Inventory_RealPointer_ReachesOnlyEnabledButtonsWithApprovedStates_AndHiddenOrDisposedTakeNothing()
        {
            CaptureRig rig = Rig(FullHd);
            var uses = new List<int>();
            int refreshes = 0;
            InventoryWindow window = BuildInventory(rig, uses.Add, () => refreshes++);
            var raycaster = window.Root.GetComponent<GraphicRaycaster>();

            Press hover = PointerEnter(rig, raycaster, Centre(rig, (RectTransform)window.Uses[0].transform));
            string hoverShown = ShownPiece(TargetImage(window.Uses[0]), BrightPiecesByGuid)?.Name;
            List<string> hoverDrawing = DrawnPerSpec(rig, rig.Capture(), TargetImage(window.Uses[0]),
                PieceNamed("Button_Default_Highlighted", BrightPieces), "hover", requireCompared: true);
            PointerDown(hover);
            string pressShown = ShownPiece(TargetImage(window.Uses[0]), BrightPiecesByGuid)?.Name;
            List<string> pressDrawing = DrawnPerSpec(rig, rig.Capture(), TargetImage(window.Uses[0]),
                PieceNamed("Button_Default_Pressed", BrightPieces), "press", requireCompared: true);
            PointerUp(hover);
            PointerExit(hover);
            int usesAfterHoverClick = uses.Count;

            GameObject[] usePressed = window.Uses.Select(use => Click(rig, raycaster, Centre(rig, (RectTransform)use.transform))).ToArray();
            GameObject refreshPressed = Click(rig, raycaster, Centre(rig, (RectTransform)window.Refresh.transform));
            string[] rowHits = window.Slots.SelectMany(row =>
                HitNames(rig, raycaster, RowPointLeftOfUse(rig, row))).ToArray();
            Vector2[] decorationPoints = DecorationPoints(rig, window.Root.transform, BrightPiecesByGuid);
            string[] decorationHits = decorationPoints.SelectMany(point => HitNames(rig, raycaster, point)).ToArray();

            GameObject mirrorOwner = Own(new GameObject("Inventory mirror (fixture)"));
            mirrorOwner.SetActive(false);
            var mirror = mirrorOwner.AddComponent<InventoryState>();
            var slots = new InventorySlot[InventoryLimits.MaxSlots];
            slots[0] = new InventorySlot(ItemId.Material, 3);
            slots[1] = new InventorySlot(ItemId.CoinPouch, 2);
            mirror.ApplySnapshot(1, 120, slots);
            window.View.Render(mirror, null, true);
            int usesBeforeDisabled = uses.Count, refreshesBeforeDisabled = refreshes;
            Click(rig, raycaster, Centre(rig, (RectTransform)window.Uses[1].transform));
            Click(rig, raycaster, Centre(rig, (RectTransform)window.Refresh.transform));
            string disabledShown = ShownPiece(TargetImage(window.Uses[1]), BrightPiecesByGuid)?.Name;
            string[] hiddenUseHits = HitNames(rig, raycaster, Centre(rig, (RectTransform)window.Uses[0].transform));

            window.View.SetVisibility(false, false);
            Pixels hidden = rig.Capture();
            string[] hiddenPanelHits = HitNames(rig, raycaster, Centre(rig, (RectTransform)window.Uses[1].transform));
            Canvas canvas = window.Root.GetComponent<Canvas>();
            canvas.enabled = false;
            Pixels absent = rig.Capture();
            canvas.enabled = true;
            int hiddenDrawn = CountDifferent(hidden, absent, ToBlock(ScreenRect(rig, window.Panel), hidden));
            window.View.SetVisibility(true, true);

            // Dispose on a second, freshly built panel: its enabled buttons must no longer reach the callbacks.
            canvas.enabled = false;
            var disposedUses = new List<int>();
            int disposedRefreshes = 0;
            InventoryWindow second = BuildInventory(rig, disposedUses.Add, () => disposedRefreshes++);
            second.View.Dispose();
            var secondRaycaster = second.Root.GetComponent<GraphicRaycaster>();
            GameObject disposedUsePressed = Click(rig, secondRaycaster, Centre(rig, (RectTransform)second.Uses[0].transform));
            Click(rig, secondRaycaster, Centre(rig, (RectTransform)second.Refresh.transform));

            Log("inventory hover/press", hoverDrawing.Concat(pressDrawing));
            Log("inventory raycast hits on slot rows", rowHits);
            Log("inventory raycast hits on decorations", decorationHits);
            Assert.AreEqual("Button_Default_Highlighted", hoverShown, "P pointer enter shows the approved highlighted sprite");
            Assert.IsEmpty(hoverDrawing, "P the highlighted button is drawn per spec");
            Assert.AreEqual("Button_Default_Pressed", pressShown, "P pointer down shows the approved pressed sprite");
            Assert.IsEmpty(pressDrawing, "P the pressed button is drawn per spec");
            Assert.AreEqual(1, usesAfterHoverClick, "P the hover/press/release sequence is one click");
            CollectionAssert.AreEqual(window.Uses.Select(use => use.gameObject).ToArray(), usePressed,
                "P each use button takes the pointer press over its own centre");
            Assert.AreSame(window.Refresh.gameObject, refreshPressed, "P the refresh button takes its pointer press");
            CollectionAssert.AreEqual(new[] { 0, 0, 1, 2, 3, 4, 5, 6, 7 }, uses, "P each real click reports its own slot once");
            Assert.AreEqual(1, refreshes, "P one real refresh click reaches the refresh callback once");
            Assert.IsEmpty(rowHits, "P slot rows and their art pass the pointer (only buttons take raycasts)");
            Assert.IsNotEmpty(decorationPoints, "P T4 the bright table finds decoration pieces outside the buttons");
            Assert.IsEmpty(decorationHits, "P approved decoration pieces outside buttons take no raycast");
            Assert.AreEqual(usesBeforeDisabled, uses.Count, "P a disabled use button does not reach the callback");
            Assert.AreEqual(refreshesBeforeDisabled, refreshes, "P a disabled refresh button does not reach the callback");
            Assert.AreEqual("Button_Default_Disabled", disabledShown, "P a disabled button keeps the approved disabled sprite under the pointer");
            Assert.IsEmpty(hiddenUseHits, "P a use button hidden for a non-usable item takes no raycast");
            Assert.IsEmpty(hiddenPanelHits, "L the closed panel takes no raycast");
            Assert.AreEqual(0, hiddenDrawn, "L the closed panel draws nothing");
            Assert.AreSame(second.Uses[0].gameObject, disposedUsePressed, "fixture: the disposed panel's button is still pressable");
            Assert.IsEmpty(disposedUses, "L Dispose leaves no use callback");
            Assert.AreEqual(0, disposedRefreshes, "L Dispose leaves no refresh callback");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Party invite (PartyInvitePopup.BuildRuntime, CombatBootstrap's builder)
        // ---------------------------------------------------------------------------------------------------------

        // Approved preview party-invite-0: Panel_Default behind the 400×180 panel, 수락 Button_Accept (green) and
        // 거절 Button_Decline (red). Scope 「수락 녹색·거절 적색을 보존」.
        [TestCase(1920, 1080)]
        [TestCase(1280, 720)]
        public void PartyInvite_OnCamera_DrawsApprovedPanelAndGreenRedButtonsPerSpecWithReadableText(int width, int height)
        {
            CaptureRig rig = Rig(new Vector2Int(width, height));
            GameObject parent = Own(new GameObject("Party invite parent"));
            PartyInvitePopup popup = PartyInvitePopup.BuildRuntime(parent.transform);
            Own(popup.gameObject);
            SessionTestTools.Call(popup, "Awake");
            _cleanups.Add(() => SessionTestTools.Call(popup, "OnDestroy"));
            SessionTestTools.Call(popup, "ShowPopup");
            rig.Attach(popup.GetComponent<Canvas>());
            var panel = (RectTransform)popup.transform.Find("Panel");
            Button accept = popup.transform.Find("Panel/AcceptButton").GetComponent<Button>();
            Button reject = popup.transform.Find("Panel/RejectButton").GetComponent<Button>();

            Pixels shown = rig.Capture();
            SaveScreen(rig, "party-invite");
            var roles = new List<string>();
            roles.AddRange(RoleViolations(rig, popup.transform, panel, "Panel_Default"));
            roles.AddRange(DrawnPerSpec(rig, shown, Background(rig, popup.transform, panel), PieceNamed("Panel_Default"),
                "party invite background", requireCompared: true));
            roles.AddRange(ButtonStateViolations(accept, "Button_Accept_Normal").Select(p => "수락 " + p));
            roles.AddRange(ButtonStateViolations(reject, "Button_Decline_Normal").Select(p => "거절 " + p));
            roles.AddRange(DrawnPerSpec(rig, shown, TargetImage(accept), PieceNamed("Button_Accept_Normal"), "수락", requireCompared: true));
            roles.AddRange(DrawnPerSpec(rig, shown, TargetImage(reject), PieceNamed("Button_Decline_Normal"), "거절", requireCompared: true));
            List<string> drawing = AllPiecesDrawnPerSpec(rig, shown, popup.transform);
            TMP_Text[] texts = VisibleTexts(popup.transform);
            string[] hidden = HiddenTexts(rig, shown, texts);
            string[] missing = MissingGlyphs(texts);
            Rect panelRect = ScreenRect(rig, panel);
            Log("party invite geometry (VIS-03 record, not asserted)", new[]
            {
                $"panel {panelRect}",
                $"수락 {ScreenRect(rig, (RectTransform)accept.transform)} inside panel={Inside(panelRect, ScreenRect(rig, (RectTransform)accept.transform))}",
                $"거절 {ScreenRect(rig, (RectTransform)reject.transform)} inside panel={Inside(panelRect, ScreenRect(rig, (RectTransform)reject.transform))}",
            });
            Log("party invite roles", roles);
            Log("party invite drawing", drawing);

            Assert.IsEmpty(roles, "R Panel_Default background and the green/red approved buttons");
            Assert.IsEmpty(drawing, "R every approved piece the popup draws matches the spec drawing");
            Assert.IsEmpty(hidden, "R popup texts are drawn over the art");
            Assert.IsEmpty(missing, "R popup texts resolve every glyph");
        }

        // Real session and socket (PartyPopupFixture, read-only reuse): a real pointer click on 수락/거절 sends one
        // C_PartyRespond with the intent, consumes the pending invite and hides the popup; the hidden popup and the
        // header art take no clicks. Scope 「응답 command·pending·예외·구독/해제 보존」.
        [Test]
        public void PartyInvite_RealPointer_AcceptAndRejectSendOnceAndHide_DecorationAndHiddenPopupTakeNoClick()
        {
            CaptureRig rig = Rig(FullHd);
            using var h = new PartyPopupFixture();
            // EditMode sends lifecycle only to ExecuteAlways components, so activating the parent only lets uGUI draw.
            h.Root.SetActive(true);
            h.Ready();
            h.Invite();
            h.AssertVisible(true);
            rig.Attach(h.Popup.GetComponent<Canvas>());
            var raycaster = h.Popup.GetComponent<GraphicRaycaster>();
            Button accept = h.Popup.transform.Find("Panel/AcceptButton").GetComponent<Button>();
            Button reject = h.Popup.transform.Find("Panel/RejectButton").GetComponent<Button>();
            Vector2 acceptCentre = Centre(rig, (RectTransform)accept.transform);

            TMP_Text title = h.Popup.GetComponentsInChildren<TMP_Text>(true).Single(text => text.name == "Title");
            Assert.IsFalse(string.IsNullOrWhiteSpace(title.text), "the existing Title is popup content");
            Assert.IsNull(title.GetComponent<Image>(), "the content Title is not a decoration Image");
            GameObject panel = h.Popup.transform.Find("Panel").gameObject;
            GameObject inviteText = h.Popup.transform.Find("Panel/InviteText").gameObject;
            // Existing TMP content catches the pointer like the body. Header artwork still must not catch it.
            string[] decorationHits = DecorationPoints(rig, h.Popup.transform)
                .SelectMany(point => Hits(rig, raycaster, point)
                    .Where(hit => hit.gameObject != panel && hit.gameObject != inviteText && hit.gameObject != title.gameObject)
                    .Select(hit => hit.gameObject.name)).ToArray();
            bool bodyTakesRaycast = HitNames(rig, raycaster, Centre(rig, (RectTransform)h.Popup.transform.Find("Panel"))).Length > 0;
            Press press = PointerEnter(rig, raycaster, acceptCentre);
            string hoverShown = ShownPiece(TargetImage(accept))?.Name;
            List<string> hoverDrawing = DrawnPerSpec(rig, rig.Capture(), TargetImage(accept), PieceNamed("Button_Accept_Highlighted"), "수락 hover", requireCompared: true);
            PointerDown(press);
            string pressShown = ShownPiece(TargetImage(accept))?.Name;
            List<string> pressDrawing = DrawnPerSpec(rig, rig.Capture(), TargetImage(accept), PieceNamed("Button_Accept_Pressed"), "수락 press", requireCompared: true);
            PointerUp(press);
            PointerExit(press);
            GameObject acceptPressed = press.Pressed;
            h.AssertResponse(true);
            h.AssertVisible(false);
            h.AssertPending(false);
            string[] hiddenHits = HitNames(rig, raycaster, acceptCentre);
            Pixels hidden = rig.Capture();
            Canvas canvas = h.Popup.GetComponent<Canvas>();
            canvas.enabled = false;
            Pixels absent = rig.Capture();
            canvas.enabled = true;
            int hiddenDrawn = CountDifferent(hidden, absent, ToBlock(ScreenRect(rig, (RectTransform)h.Popup.transform.Find("Panel")), hidden));

            h.Invite();
            h.AssertVisible(true);
            Press rejectPress = PointerEnter(rig, raycaster, Centre(rig, (RectTransform)reject.transform));
            string rejectHoverShown = ShownPiece(TargetImage(reject))?.Name;
            PointerDown(rejectPress);
            string rejectPressShown = ShownPiece(TargetImage(reject))?.Name;
            PointerUp(rejectPress);
            PointerExit(rejectPress);
            h.AssertResponse(false);
            h.AssertVisible(false);

            Log("party invite decoration hits", decorationHits);
            Log("party invite hover/press", hoverDrawing.Concat(pressDrawing));
            Assert.IsEmpty(decorationHits, "P approved decoration pieces in the popup take no raycast");
            Assert.IsTrue(bodyTakesRaycast, "P the popup body still takes the raycast over its centre");
            Assert.AreEqual("Button_Accept_Highlighted", hoverShown, "P 수락 hover shows the approved green highlighted sprite");
            Assert.IsEmpty(hoverDrawing, "P 수락 hover is drawn per spec");
            Assert.AreEqual("Button_Accept_Pressed", pressShown, "P 수락 press shows the approved green pressed sprite");
            Assert.IsEmpty(pressDrawing, "P 수락 press is drawn per spec");
            Assert.AreSame(accept.gameObject, acceptPressed, "P the real pointer press lands on 수락");
            Assert.IsEmpty(hiddenHits, "L the hidden popup takes no raycast");
            Assert.AreEqual(0, hiddenDrawn, "L the hidden popup draws nothing");
            Assert.AreEqual("Button_Decline_Highlighted", rejectHoverShown, "P 거절 hover shows the approved red highlighted sprite");
            Assert.AreEqual("Button_Decline_Pressed", rejectPressShown, "P 거절 press shows the approved red pressed sprite");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Party members (the real Scenes/99.UI/UI.unity Party_Info, opened and closed without saving)
        // ---------------------------------------------------------------------------------------------------------

        // The checks of the Party_Info scenario. Each case runs the whole scenario and asserts one check, so a failed
        // check never stops the checks after it (the Unity NUnit 3.5 build has no Assert.Multiple).
        public enum PartyHudCheck
        {
            SerializedRefsKeepSceneFileIds,
            ApprovedPiecesDrawnAtBothSizes,
            SceneHudDecorationsTakeNoRaycast,
            SameCanvasProbeSeenByObserver,
            ShownPanelAllowsGameplayRaycast,
            MemberInsetsTakeNoRaycast,
            OneInsetPerMemberRow,
            RebindAddsNoSecondInsetSet,
            InsetsDrawnBehindMemberTexts,
            LeftPartyTakesNoRaycast,
            LeftPartyDrawsNothing,
            SceneFileNotWritten,
        }

        // Scope PartyMemberHud 「기존 serialized root/슬롯의 runtime 표시 초기화」 and 「씬은 쓰지 않는다」: the serialized
        // references keep the scene file's fileIDs; the runtime init draws Panel_Default and an Inset_* per member;
        // rebinding adds no second set; leaving the party draws nothing and takes no input; the scene file bytes are
        // unchanged. An empty hit list counts only where the observer is shown to see that point (else inconclusive).
        [Test]
        public void PartyMemberHud_OnTheRealUiScene_DrawsApprovedPiecesKeepsSerializedRefsAndRebindsWithoutDuplicates([Values] PartyHudCheck check)
        {
            PartyHudRun run = RunPartyHudScenario();
            Log("party members per size", run.PerSize);
            Log("party members raycast record", run.Raw);
            Log("party members probe controls failed", run.ProbeFailures);
            switch (check)
            {
                case PartyHudCheck.SerializedRefsKeepSceneFileIds:
                    Assert.IsEmpty(run.MovedRefs, "L serialized _root/_member*Group/_member*Text keep the scene's fileIDs");
                    break;
                case PartyHudCheck.ApprovedPiecesDrawnAtBothSizes:
                    Assert.IsEmpty(run.PerSize, "R Panel_Default and Inset_* rows drawn per spec with readable member texts at both sizes");
                    break;
                case PartyHudCheck.SceneHudDecorationsTakeNoRaycast:
                    Assert.IsFalse(run.RootRaycastBefore || run.RootRaycastAfter, "P Party_Info is noninteractive before and after init");
                    break;
                case PartyHudCheck.SameCanvasProbeSeenByObserver:
                    Assert.IsEmpty(run.ProbeFailures,
                        "D on the HUD canvas a raycastTarget probe is hit over the panel, shown and after leaving; raycastTarget-off and never-shown probes are not");
                    break;
                case PartyHudCheck.ShownPanelAllowsGameplayRaycast:
                    Assert.IsEmpty(run.ProbeFailures, "D the same-canvas positive and negative raycast controls work");
                    Assert.IsFalse(run.BodyTakesRaycast, "P the shown member panel does not block gameplay input");
                    break;
                case PartyHudCheck.MemberInsetsTakeNoRaycast:
                    Assert.IsEmpty(run.InsetHits, "P member row insets take no raycast");
                    // The insets are created at runtime under the HUD canvas, the same registration path as the probe.
                    RequireObservable(run.ProbeSeenAtTexts.Length > 0 && run.ProbeSeenAtTexts.All(seen => seen),
                        "P member row insets: the same-canvas probe is not hit at every member text centre");
                    break;
                case PartyHudCheck.OneInsetPerMemberRow:
                    Assert.AreEqual(2, run.InsetsBefore, "R one inset per member row after the runtime init");
                    break;
                case PartyHudCheck.RebindAddsNoSecondInsetSet:
                    Assert.AreEqual(run.InsetsBefore, run.InsetsAfterRebind, "L disable/enable rebinding adds no second set of insets");
                    break;
                case PartyHudCheck.InsetsDrawnBehindMemberTexts:
                    Assert.IsTrue(run.InsetsBehindTexts, "R each member inset is drawn behind its member text");
                    break;
                case PartyHudCheck.LeftPartyTakesNoRaycast:
                    Assert.IsEmpty(run.LeftHits, "L after leaving the party the panel takes no raycast");
                    // HUD artwork takes no raycast even while shown; the explicit probe proves the observer works.
                    RequireObservable(run.ProbeFailures.Count == 0 && run.ProbeSeenAfterLeaving,
                        "L after leaving: the same-canvas shown/left positive and negative probes were not observed");
                    break;
                case PartyHudCheck.LeftPartyDrawsNothing:
                    Assert.AreEqual(0, run.LeftDrawn, "L after leaving the party the panel draws nothing");
                    break;
                case PartyHudCheck.SceneFileNotWritten:
                    Assert.AreEqual(run.SceneBefore, run.SceneAfter, "L the UI scene file is not written");
                    break;
                default:
                    Assert.Fail($"fixture: no assertion for {check}");
                    break;
            }
        }

        // H1 attribution on the same scene HUD canvas: before any product code runs, after the product's own init, and
        // after Party_Info's image registers again (enabled off and on), it records what GraphicRaycaster reads for
        // Party_Info and for every raycast graphic of the canvas. Asserted are only the controls: a probe on this canvas
        // and a scene graphic the product does not touch are hit; the raycastTarget-off and never-shown probes are not.
        // The Party_Info rows are recorded for the failure classification, not turned into expectations.
        [Test]
        public void PartyMemberHud_SceneHudCanvas_ControlsAreHitAndPartyInfoRaycastStagesAreRecorded()
        {
            string sceneBefore = Sha256(UiScenePath);
            PartyHudScene scene = OpenUiScene();
            PartyMemberHud hud = scene.Hud;
            var root = (RectTransform)hud.transform;
            var rootImage = root.GetComponent<Image>();
            Canvas hudCanvas = root.GetComponentInParent<Canvas>().rootCanvas;
            var raw = new List<string>();
            var failures = new List<string>();
            try
            {
                raw.AddRange(RegistryRows("0 loaded", hudCanvas, root));

                CaptureRig rig = Rig(FullHd);
                rig.Attach(hudCanvas);
                rig.Draw();
                var raycaster = hudCanvas.GetComponent<GraphicRaycaster>();
                Vector2 centre = Centre(rig, root);
                raw.AddRange(RegistryRows("1 on the fixture camera, before product init", hudCanvas, root));
                raw.AddRange(RaycastStages("1 Party_Info centre, before product init", rig, raycaster, rootImage, centre));
                var sceneGraphics = Census("1 before product init", rig, raycaster, hudCanvas, raw);
                if (sceneGraphics.Any(row => row.OnScreen && row.Hit && !row.Graphic.transform.IsChildOf(root)
                    && row.Graphic.GetComponentInParent<Button>() == null))
                    failures.Add("1 before product init: a HUD decoration outside Party_Info catches the pointer");
                SameCanvasProbes probes = AddSameCanvasProbes(hudCanvas);
                ProbeControls("1 before product init", rig, raycaster, probes, centre, raw, failures);

                InitPartyHud(hud, root);
                raw.AddRange(RegistryRows("2 after product init", hudCanvas, root));
                raw.AddRange(RaycastStages("2 Party_Info centre, after product init", rig, raycaster, rootImage, centre));
                Census("2 after product init", rig, raycaster, hudCanvas, raw);
                ProbeControls("2 after product init", rig, raycaster, probes, centre, raw, failures);

                // Diagnostic only: Graphic.OnEnable caches the canvas and registers the graphic for it again.
                rootImage.enabled = false;
                rootImage.enabled = true;
                raw.AddRange(RegistryRows("3 after Party_Info image enabled off and on", hudCanvas, root));
                raw.AddRange(RaycastStages("3 Party_Info centre, after re-enable", rig, raycaster, rootImage, centre));

                SessionTestTools.Call(hud, "OnDisable");
                scene.Close();
                if (Sha256(UiScenePath) != sceneBefore) failures.Add("the UI scene file was written");
            }
            finally
            {
                Log("party hud raycast stages", raw);
                Log("party hud controls failed", failures);
            }
            Assert.IsEmpty(failures,
                "D the HUD canvas positive probe is hit, HUD decoration/off/never-shown probes are not; L the scene file is not written");
        }

        // ---------------------------------------------------------------------------------------------------------
        // NPC dialog (NpcDialogPanel.BuildRuntime, CombatBootstrap's builder)
        // ---------------------------------------------------------------------------------------------------------

        // Approved preview npc-dialog: Panel_Dialog behind the 700×160 panel and PortraitFrame_128 over a shown
        // portrait; with no portrait the frame is not drawn and the portrait field shows only the panel. A long Korean
        // line stays drawn over the art with every glyph resolved (overflow is recorded, the body rect is unchanged).
        [TestCase(1920, 1080)]
        [TestCase(1280, 720)]
        public void NpcDialog_OnCamera_DrawsApprovedDialogAndFrame_WithLongKoreanLine_AndNoPortraitDrawsNoFrame(int width, int height)
        {
            CaptureRig rig = Rig(new Vector2Int(width, height));
            NpcWindow window = BuildNpc(rig);
            NpcDialogPanel.Show(LongNpcLine, window.PortraitSprite);
            Pixels withPortrait = rig.Capture();
            SaveScreen(rig, "npc-dialog-portrait");
            var roles = new List<string>(RoleViolations(rig, window.Dialog.transform, window.Panel, "Panel_Dialog"));
            Image portrait = window.Dialog.GetComponentsInChildren<Image>(true).Single(image => image.sprite == window.PortraitSprite);
            Image[] frames = ApprovedImages(window.Dialog.transform).Where(image => ShownPiece(image)?.Name == "PortraitFrame_128" &&
                ScreenRect(rig, image.rectTransform).Contains(Centre(rig, portrait.rectTransform))).ToArray();
            List<string> drawing = AllPiecesDrawnPerSpec(rig, withPortrait, window.Dialog.transform);
            TMP_Text[] texts = VisibleTexts(window.Dialog.transform);
            string[] hidden = HiddenTexts(rig, withPortrait, texts);
            string[] missing = MissingGlyphs(texts);
            int portraitStillDrawn = PortraitPixels(rig, withPortrait, portrait);
            roles.AddRange(DrawnPerSpec(rig, withPortrait, Background(rig, window.Dialog.transform, window.Panel), PieceNamed("Panel_Dialog"),
                "npc background", requireCompared: true));
            roles.AddRange(frames.SelectMany(frame => DrawnPerSpec(rig, withPortrait, frame, PieceNamed("PortraitFrame_128"),
                "npc portrait frame", requireCompared: true)));
            TMP_Text body = texts.Single(text => text.text == LongNpcLine);
            Log("npc long line", new[] { $"body truncated={body.isTextTruncated} overflowing={body.isTextOverflowing} lines={body.textInfo.lineCount}" });

            NpcDialogPanel.Hide();
            NpcDialogPanel.Show(LongNpcLine, null);
            Pixels noPortrait = rig.Capture();
            SaveScreen(rig, "npc-dialog-no-portrait");
            bool frameShownWithoutPortrait = frames.Any(frame => frame.gameObject.activeInHierarchy);
            List<string> noPortraitDrawing = AllPiecesDrawnPerSpec(rig, noPortrait, window.Dialog.transform);
            List<string> portraitField = FieldShowsOnly(rig, noPortrait, portrait.rectTransform, window.Panel, "Panel_Dialog");

            Log("npc roles", roles);
            Log("npc drawing", drawing);
            Log("npc portrait field without portrait", portraitField);
            Assert.IsEmpty(roles, "R Panel_Dialog behind the dialog panel");
            Assert.AreEqual(1, frames.Length, "R exactly one approved PortraitFrame_128 over the shown portrait");
            Assert.IsEmpty(drawing, "R every approved piece the dialog draws matches the spec drawing");
            Assert.Greater(portraitStillDrawn, 0, "R the content portrait stays visible inside the frame");
            Assert.IsEmpty(hidden, "R dialog texts are drawn over the art");
            Assert.IsEmpty(missing, "R the long Korean line resolves every glyph");
            Assert.IsFalse(frameShownWithoutPortrait, "R without a portrait the frame is not shown");
            Assert.IsEmpty(noPortraitDrawing, "R without a portrait the remaining pieces match the spec drawing");
            Assert.IsEmpty(portraitField, "R without a portrait the portrait field shows only Panel_Dialog");
        }

        // The dialog body keeps taking the raycast, the portrait frame does not; Hide draws and takes nothing;
        // BuildRuntime while the singleton exists returns it without a second frame; after OnDestroy a rebuild has
        // exactly one frame.
        [Test]
        public void NpcDialog_RealPointerHideAndSingleton_FrameTakesNoClickAndIsBuiltOnce()
        {
            CaptureRig rig = Rig(FullHd);
            NpcWindow window = BuildNpc(rig);
            NpcDialogPanel.Show(LongNpcLine, window.PortraitSprite);
            var raycaster = window.Dialog.GetComponent<GraphicRaycaster>();
            Image portrait = window.Dialog.GetComponentsInChildren<Image>(true).Single(image => image.sprite == window.PortraitSprite);
            RaycastResult[] portraitHits = Hits(rig, raycaster, Centre(rig, portrait.rectTransform));
            string[] frameHits = portraitHits.Where(hit => ShownPieceOf(hit.gameObject)?.Name == "PortraitFrame_128")
                .Select(hit => hit.gameObject.name).ToArray();
            bool bodyTakesRaycast = HitNames(rig, raycaster, Centre(rig, window.Panel)).Contains(window.Panel.name);
            int framesAtStart = FrameCount(window.Dialog.transform);

            NpcDialogPanel.Hide();
            Pixels hidden = rig.Capture();
            string[] hiddenHits = HitNames(rig, raycaster, Centre(rig, window.Panel));
            Canvas canvas = window.Dialog.GetComponent<Canvas>();
            canvas.enabled = false;
            Pixels absent = rig.Capture();
            canvas.enabled = true;
            int hiddenDrawn = CountDifferent(hidden, absent, ToBlock(ScreenRect(rig, window.Panel), hidden));

            GameObject otherParent = Own(new GameObject("Second NPC parent"));
            NpcDialogPanel again = NpcDialogPanel.BuildRuntime(otherParent.transform);
            int framesAfterIdempotentBuild = FrameCount(window.Dialog.transform);

            SessionTestTools.Call(window.Dialog, "OnDestroy");
            bool clearedOnDestroy = NpcDialogPanel.Instance == null;
            NpcDialogPanel rebuilt = NpcDialogPanel.BuildRuntime(otherParent.transform);
            Own(rebuilt.gameObject);
            SessionTestTools.Call(rebuilt, "Awake");
            _cleanups.Add(() => SessionTestTools.Call(rebuilt, "OnDestroy"));
            int framesInRebuilt = FrameCount(rebuilt.transform);

            Assert.IsEmpty(frameHits, "P the portrait frame takes no raycast");
            Assert.IsTrue(bodyTakesRaycast, "P the dialog body still takes the raycast over its centre");
            Assert.AreEqual(1, framesAtStart, "R one portrait frame in the built dialog");
            Assert.IsEmpty(hiddenHits, "L the hidden dialog takes no raycast");
            Assert.AreEqual(0, hiddenDrawn, "L the hidden dialog draws nothing");
            Assert.AreSame(window.Dialog, again, "L BuildRuntime while the singleton exists returns it");
            Assert.AreEqual(1, framesAfterIdempotentBuild, "L the idempotent build adds no second frame");
            Assert.IsTrue(clearedOnDestroy, "L OnDestroy clears the singleton");
            Assert.AreNotSame(window.Dialog, rebuilt, "L a rebuild after OnDestroy is a new dialog");
            Assert.AreEqual(1, framesInRebuilt, "L the rebuilt dialog has exactly one frame");
        }

        // ---------------------------------------------------------------------------------------------------------
        // All four windows together
        // ---------------------------------------------------------------------------------------------------------

        // The four windows open at once on one camera, at both sizes: every panel and button lies on screen and each
        // button's centre is taken by that button through its own raycaster. Captures are saved for the morning check.
        [Test]
        public void AllFourWindows_TogetherOnCamera_StayOnScreenAndKeepTheirButtonsAtBothSizes()
        {
            PartyHudScene scene = OpenUiScene();
            GameObject stateObject = Own(new GameObject("Party HUD state (fixture)"));
            stateObject.SetActive(false);
            var state = stateObject.AddComponent<PartyState>();
            PartyPopupFixture.SetState(state);
            _cleanups.Add(() => PartyPopupFixture.SetState(null));
            state.ApplyUpdate(31, 71, 71, 72, 0, 1);
            SessionTestTools.Call(scene.Hud, "Awake");
            SessionTestTools.Call(scene.Hud, "OnEnable");
            SessionTestTools.Call(scene.Hud, "Start");
            _cleanups.Add(() => SessionTestTools.Call(scene.Hud, "OnDisable"));
            Canvas hudCanvas = scene.Hud.GetComponentInParent<Canvas>().rootCanvas;

            var problems = new List<string>();
            foreach (Vector2Int size in new[] { FullHd, DefaultWindow })
            {
                CaptureRig rig = Rig(size);
                rig.Attach(hudCanvas);
                InventoryWindow inventory = BuildInventory(rig, _ => { }, () => { });
                GameObject popupParent = Own(new GameObject("Party invite parent"));
                PartyInvitePopup popup = PartyInvitePopup.BuildRuntime(popupParent.transform);
                Own(popup.gameObject);
                SessionTestTools.Call(popup, "Awake");
                _cleanups.Add(() => { if (PartyInvitePopup.Instance == popup) SessionTestTools.Call(popup, "OnDestroy"); });
                SessionTestTools.Call(popup, "ShowPopup");
                rig.Attach(popup.GetComponent<Canvas>());
                NpcWindow npc = BuildNpc(rig);
                NpcDialogPanel.Show(LongNpcLine, npc.PortraitSprite);

                rig.Capture();
                SaveScreen(rig, "all-four-windows");
                var onScreen = new Dictionary<string, RectTransform>
                {
                    ["inventory panel"] = inventory.Panel,
                    ["inventory refresh"] = (RectTransform)inventory.Refresh.transform,
                    ["party invite panel"] = (RectTransform)popup.transform.Find("Panel"),
                    ["수락"] = (RectTransform)popup.transform.Find("Panel/AcceptButton"),
                    ["거절"] = (RectTransform)popup.transform.Find("Panel/RejectButton"),
                    ["npc dialog panel"] = npc.Panel,
                    ["party members"] = (RectTransform)scene.Hud.transform,
                };
                var screen = new Rect(0f, 0f, size.x, size.y);
                problems.AddRange(onScreen.Where(entry => !Inside(screen, ScreenRect(rig, entry.Value)))
                    .Select(entry => $"{size}: {entry.Key} {ScreenRect(rig, entry.Value)} leaves the screen"));
                var buttons = new List<(Button Button, GraphicRaycaster Raycaster)>
                {
                    (inventory.Refresh, inventory.Root.GetComponent<GraphicRaycaster>()),
                    (popup.transform.Find("Panel/AcceptButton").GetComponent<Button>(), popup.GetComponent<GraphicRaycaster>()),
                    (popup.transform.Find("Panel/RejectButton").GetComponent<Button>(), popup.GetComponent<GraphicRaycaster>()),
                };
                buttons.AddRange(inventory.Uses.Select(use => (use, inventory.Root.GetComponent<GraphicRaycaster>())));
                foreach ((Button button, GraphicRaycaster raycaster) in buttons)
                {
                    RaycastResult[] hits = Hits(rig, raycaster, Centre(rig, (RectTransform)button.transform));
                    GameObject top = hits.Length > 0 ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) : null;
                    if (top != button.gameObject) problems.Add($"{size}: {PathOf(button.transform)} centre is taken by {(top != null ? top.name : "nothing")}");
                }

                SessionTestTools.Call(popup, "OnDestroy");
                SessionTestTools.Call(npc.Dialog, "OnDestroy");
            }
            scene.Close();

            Log("all four windows", problems);
            Assert.IsEmpty(problems, "R/P all four windows stay on screen and keep their buttons at 1920×1080 and 1280×720");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Window fixtures (real product builders; only the canvas render mode is switched for the camera)
        // ---------------------------------------------------------------------------------------------------------

        sealed class InventoryWindow
        {
            internal GameObject Root;
            internal InventoryPanelView View;
            internal RectTransform Panel;
            internal Button Refresh;
            internal Button[] Uses;
            internal RectTransform[] Slots;
        }

        InventoryWindow BuildInventory(CaptureRig rig, Action<int> useSlot, Action refresh)
        {
            GameObject root = Own(new GameObject("Inventory window under test", typeof(RectTransform)));
            var view = new InventoryPanelView(root, useSlot, refresh);
            _cleanups.Add(view.Dispose);
            var canvas = root.GetComponent<Canvas>();
            rig.Attach(canvas);
            rig.Capture();
            RequireScreenSize(rig, canvas);
            // The panel sizes itself from the canvas rect; the camera canvas has its size only after the first frame.
            view.Resize();
            // Path names other code and tests use (InventoryPresentationTests, InventoryPanelToggleTests).
            var panel = (RectTransform)root.transform.Find("Inventory");
            Assert.IsNotNull(panel, "fixture: Inventory panel path");
            return new InventoryWindow
            {
                Root = root,
                View = view,
                Panel = panel,
                Refresh = panel.Find("Refresh").GetComponent<Button>(),
                Uses = Enumerable.Range(1, 8).Select(n => panel.Find($"Slot{n}/Use").GetComponent<Button>()).ToArray(),
                Slots = Enumerable.Range(1, 8).Select(n => (RectTransform)panel.Find($"Slot{n}")).ToArray(),
            };
        }

        sealed class NpcWindow
        {
            internal NpcDialogPanel Dialog;
            internal RectTransform Panel;
            internal Sprite PortraitSprite;
        }

        NpcWindow BuildNpc(CaptureRig rig)
        {
            GameObject parent = Own(new GameObject("NPC dialog parent"));
            NpcDialogPanel dialog = NpcDialogPanel.BuildRuntime(parent.transform);
            Own(dialog.gameObject);
            // EditMode sends no lifecycle messages; the product's own Awake publishes the singleton and the keys.
            SessionTestTools.Call(dialog, "Awake");
            _cleanups.Add(() => { if (NpcDialogPanel.Instance == dialog) SessionTestTools.Call(dialog, "OnDestroy"); });
            rig.Attach(dialog.GetComponent<Canvas>());
            var panel = (RectTransform)dialog.transform.Find("DialogPanel");
            Assert.IsNotNull(panel, "fixture: DialogPanel path");
            return new NpcWindow { Dialog = dialog, Panel = panel, PortraitSprite = ContentSprite("NPC portrait (content)", new Color(0.2f, 0.55f, 0.6f, 1f)) };
        }

        sealed class PartyHudScene
        {
            internal Scene Scene;
            internal Scene Previous;
            internal bool ReplacedRunnerScene;
            internal PartyMemberHud Hud;
            internal TMP_Text[] Texts;
            bool _closed;

            internal void Close()
            {
                if (_closed) return;
                _closed = true;
                if (ReplacedRunnerScene)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    return;
                }
                if (Previous.IsValid()) SceneManager.SetActiveScene(Previous);
                if (Scene.IsValid()) EditorSceneManager.CloseScene(Scene, true);
            }
        }

        // Same pattern as MainMenuSceneFixture: additive next to the runner's scene, single if Unity refuses, closed
        // without saving. The in-memory copy is changed by the test; the file is not written.
        PartyHudScene OpenUiScene()
        {
            var opened = new PartyHudScene { Previous = SceneManager.GetActiveScene() };
            try
            {
                opened.Scene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Additive);
            }
            catch (Exception error)
            {
                TestContext.Out.WriteLine($"fixture: additive open refused ({error.Message}); opening the UI scene alone");
                opened.Scene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Single);
                opened.ReplacedRunnerScene = true;
            }
            _cleanups.Add(opened.Close);
            PartyMemberHud[] huds = opened.Scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PartyMemberHud>(true)).ToArray();
            Assert.AreEqual(1, huds.Length, "fixture: one PartyMemberHud in UI.unity");
            opened.Hud = huds[0];
            var serialized = new SerializedObject(opened.Hud);
            opened.Texts = new[] { "_member0Text", "_member1Text" }
                .Select(field => serialized.FindProperty(field).objectReferenceValue as TMP_Text).ToArray();
            Assert.IsTrue(opened.Texts.All(text => text != null), "fixture: both member texts are wired in the scene");
            return opened;
        }

        // The product's own runtime init as the scene runs it, with the largest member texts the format can show
        // (class 1 「마법사」 with the largest entity ids). Only Party_Info is observed; its HUD siblings are switched off.
        PartyState InitPartyHud(PartyMemberHud hud, RectTransform root)
        {
            GameObject stateObject = Own(new GameObject("Party HUD state (fixture)"));
            stateObject.SetActive(false);
            var state = stateObject.AddComponent<PartyState>();
            PartyPopupFixture.SetState(state);
            _cleanups.Add(() => PartyPopupFixture.SetState(null));
            state.ApplyUpdate(31, int.MaxValue - 1, int.MaxValue - 1, int.MaxValue, 1, 1);
            foreach (Transform sibling in root.parent)
                if (sibling != root) sibling.gameObject.SetActive(false);
            SessionTestTools.Call(hud, "Awake");
            SessionTestTools.Call(hud, "OnEnable");
            SessionTestTools.Call(hud, "Start");
            _cleanups.Add(() => SessionTestTools.Call(hud, "OnDisable"));
            return state;
        }

        // What one run of the Party_Info scenario observed; Raw holds the recorded raycast stages and probe rows.
        sealed class PartyHudRun
        {
            internal string SceneBefore;
            internal string SceneAfter;
            internal string[] MovedRefs;
            internal readonly List<string> PerSize = new List<string>();
            internal bool RootRaycastBefore;
            internal bool RootRaycastAfter;
            internal bool BodyTakesRaycast;
            internal string[] InsetHits;
            internal bool[] ProbeSeenAtTexts = Array.Empty<bool>();
            internal int InsetsBefore;
            internal int InsetsAfterRebind;
            internal bool InsetsBehindTexts;
            internal bool PanelSeenWhileShown;
            internal string[] LeftHits;
            internal bool ProbeSeenAfterLeaving;
            internal int LeftDrawn;
            internal readonly List<string> ProbeFailures = new List<string>();
            internal readonly List<string> Raw = new List<string>();
        }

        // The scenario's own observations run in their original order. Raw records and probe raycasts come after the
        // observation they explain and only read state; the probes are inactive except during their own raycasts.
        PartyHudRun RunPartyHudScenario()
        {
            var run = new PartyHudRun { SceneBefore = Sha256(UiScenePath) };
            PartyHudScene scene = OpenUiScene();
            PartyMemberHud hud = scene.Hud;
            var root = (RectTransform)hud.transform;
            var rootImage = root.GetComponent<Image>();
            // fileIDs as written in UI.unity (Party_Info's PartyMemberHud block), read as text, not from product code.
            var serialized = new SerializedObject(hud);
            var expectedIds = new Dictionary<string, ulong>
            {
                ["_root"] = 271747914UL,
                ["_member0Group"] = 1303358837UL,
                ["_member0Text"] = 2125634492UL,
                ["_member1Group"] = 780923406UL,
                ["_member1Text"] = 1757069867UL,
            };
            run.MovedRefs = expectedIds.Where(entry =>
            {
                UnityEngine.Object target = serialized.FindProperty(entry.Key)?.objectReferenceValue;
                return target == null || GlobalObjectId.GetGlobalObjectIdSlow(target).targetObjectId != entry.Value;
            }).Select(entry => entry.Key).ToArray();
            run.RootRaycastBefore = rootImage.raycastTarget;
            Record(run.Raw, "loaded", () => RegistryRows("loaded, before product init", root.GetComponentInParent<Canvas>().rootCanvas, root));

            PartyState state = InitPartyHud(hud, root);
            Canvas hudCanvas = root.GetComponentInParent<Canvas>().rootCanvas;
            Record(run.Raw, "init", () => RegistryRows("after product init", hudCanvas, root));

            foreach (Vector2Int size in new[] { FullHd, DefaultWindow })
            {
                CaptureRig rig = Rig(size);
                rig.Attach(hudCanvas);
                Pixels shown = rig.Capture();
                RequireScreenSize(rig, hudCanvas);
                SaveScreen(rig, "party-members-ui-scene");
                run.PerSize.AddRange(RoleViolations(rig, root, root, "Panel_Default").Select(p => $"{size}: {p}"));
                run.PerSize.AddRange(DrawnPerSpec(rig, shown, Background(rig, root, root), PieceNamed("Panel_Default"),
                    "party members background", requireCompared: true).Select(p => $"{size}: {p}"));
                run.PerSize.AddRange(scene.Texts.Where(text => PiecesUnder(rig, root, Centre(rig, text.rectTransform),
                        "Inset_Default", "Inset_Selected", "Inset_Disabled").Length == 0)
                    .Select(text => $"{size}: member 「{text.text}」 is not drawn on an approved Inset_*"));
                run.PerSize.AddRange(AllPiecesDrawnPerSpec(rig, shown, root).Select(p => $"{size}: {p}"));
                TMP_Text[] texts = VisibleTexts(root);
                run.PerSize.AddRange(HiddenTexts(rig, shown, texts).Select(p => $"{size}: {p}"));
                run.PerSize.AddRange(MissingGlyphs(texts).Select(p => $"{size}: {p}"));
                Log($"party members texts {size}", texts.Select(t => $"{t.name}: 「{t.text}」 truncated={t.isTextTruncated} overflowing={t.isTextOverflowing}"));
            }
            CaptureRig pointerRig = Rig(FullHd);
            pointerRig.Attach(hudCanvas);
            var raycaster = hudCanvas.GetComponent<GraphicRaycaster>();
            Vector2 centre = Centre(pointerRig, root);
            run.BodyTakesRaycast = HitNames(pointerRig, raycaster, centre).Contains(root.name);
            run.InsetHits = scene.Texts.SelectMany(text => Hits(pointerRig, raycaster, Centre(pointerRig, text.rectTransform)))
                .Where(hit => ShownPieceOf(hit.gameObject)?.Name.StartsWith("Inset_", StringComparison.Ordinal) == true)
                .Select(hit => hit.gameObject.name).ToArray();
            run.InsetsBefore = InsetCount(root);
            Record(run.Raw, "shown", () => RaycastStages("shown, Party_Info centre", pointerRig, raycaster, rootImage, centre));
            Record(run.Raw, "shown census", () =>
            {
                var rows = new List<string>();
                Census("shown", pointerRig, raycaster, hudCanvas, rows);
                return rows;
            });
            SameCanvasProbes probes = null;
            try
            {
                probes = AddSameCanvasProbes(hudCanvas);
                RaycastResult[] shownHits = ProbeControls("shown", pointerRig, raycaster, probes, centre, run.Raw, run.ProbeFailures);
                run.PanelSeenWhileShown = shownHits.Any(hit => hit.gameObject.transform.IsChildOf(root));
                run.ProbeSeenAtTexts = scene.Texts.Select(text =>
                {
                    RaycastResult[] textHits = ProbeHits(pointerRig, raycaster, probes, Centre(pointerRig, text.rectTransform));
                    run.Raw.Add($"shown, {PathOf(text.transform)} centre with probes: {HitList(textHits)}");
                    return textHits.Any(hit => hit.gameObject == probes.On.gameObject);
                }).ToArray();
            }
            catch (Exception error)
            {
                run.ProbeFailures.Add($"shown: probe record failed {error.GetType().Name}: {error.Message}");
            }

            SessionTestTools.Call(hud, "OnDisable");
            SessionTestTools.Call(hud, "OnEnable");
            SessionTestTools.Call(hud, "Start");
            run.InsetsAfterRebind = InsetCount(root);
            run.InsetsBehindTexts = scene.Texts.All(text => root.GetComponentsInChildren<Image>(true)
                .Where(image => ShownPiece(image)?.Name.StartsWith("Inset_", StringComparison.Ordinal) == true &&
                                image.transform.parent == text.transform.parent)
                .Any(image => image.transform.GetSiblingIndex() < text.transform.GetSiblingIndex()));

            state.Clear();
            Pixels left = pointerRig.Capture();
            run.LeftHits = HitNames(pointerRig, raycaster, Centre(pointerRig, root));
            Record(run.Raw, "left", () => RaycastStages("after leaving, Party_Info centre", pointerRig, raycaster, rootImage, centre));
            try
            {
                if (probes == null) throw new InvalidOperationException("no probes were created");
                RaycastResult[] leftProbeHits = ProbeControls("after leaving", pointerRig, raycaster, probes, centre, run.Raw, run.ProbeFailures);
                run.ProbeSeenAfterLeaving = leftProbeHits.Any(hit => hit.gameObject == probes.On.gameObject);
            }
            catch (Exception error)
            {
                run.ProbeFailures.Add($"after leaving: probe record failed {error.GetType().Name}: {error.Message}");
            }
            root.gameObject.SetActive(false);
            Pixels absent = pointerRig.Capture();
            root.gameObject.SetActive(true);
            run.LeftDrawn = CountDifferent(left, absent, ToBlock(ScreenRect(pointerRig, root), left));
            run.RootRaycastAfter = rootImage.raycastTarget;

            SessionTestTools.Call(hud, "OnDisable");
            scene.Close();
            run.SceneAfter = Sha256(UiScenePath);
            return run;
        }

        // ---------------------------------------------------------------------------------------------------------
        // Capture rig
        // ---------------------------------------------------------------------------------------------------------

        sealed class Pixels
        {
            internal readonly int Width;
            internal readonly int Height;
            internal readonly Color32[] Data;

            internal Pixels(int width, int height, Color32[] data)
            {
                Width = width;
                Height = height;
                Data = data;
            }

            internal Color32 At(int x, int y) => Data[y * Width + x];
        }

        // A fixture camera drawing into a render texture. Attached product canvases become ScreenSpaceCamera on it;
        // their sorting order, scaler settings and children are left as built.
        sealed class CaptureRig : IDisposable
        {
            readonly GameObject _cameraObject;
            readonly Texture2D _readback;
            readonly List<Canvas> _canvases = new List<Canvas>();
            internal readonly Camera Camera;
            internal readonly RenderTexture Target;
            internal readonly int Width;
            internal readonly int Height;

            internal CaptureRig(Vector2Int size)
            {
                Width = size.x;
                Height = size.y;
                _cameraObject = new GameObject("Capture camera (fixture)");
                _cameraObject.hideFlags = HideFlags.DontSave;
                Camera = _cameraObject.AddComponent<Camera>();
                Camera.enabled = false;
                Camera.clearFlags = CameraClearFlags.SolidColor;
                // Opaque ground-like field, so a piece's transparent pixels show what is behind them.
                Camera.backgroundColor = new Color(0.16f, 0.2f, 0.16f, 1f);
                Camera.cullingMask = ~0;
                Camera.orthographic = true;
                Camera.allowHDR = false;
                Camera.allowMSAA = false;
                Camera.nearClipPlane = 0.3f;
                Camera.farClipPlane = 1000f;
                Target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Target.Create();
                Camera.targetTexture = Target;
                _readback = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
            }

            internal IEnumerable<Canvas> Canvases => _canvases.Where(canvas => canvas != null);

            internal void Attach(Canvas canvas)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera;
                canvas.planeDistance = 100f;
                if (!_canvases.Contains(canvas)) _canvases.Add(canvas);
            }

            // Scalers for the camera size, layout, text meshes and one camera frame (also assigns graphic depths,
            // which GraphicRaycaster needs).
            internal void Draw()
            {
                Canvas.ForceUpdateCanvases();
                foreach (Canvas canvas in Canvases)
                    foreach (CanvasScaler scaler in canvas.GetComponentsInChildren<CanvasScaler>(true))
                        SessionTestTools.Call(scaler, "Handle");
                Canvas.ForceUpdateCanvases();
                foreach (Canvas canvas in Canvases)
                    foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(false))
                        text.ForceMeshUpdate();
                Canvas.ForceUpdateCanvases();
                Camera.Render();
            }

            internal Pixels Capture()
            {
                RenderTexture previous = RenderTexture.active;
                try
                {
                    Draw();
                    RenderTexture.active = Target;
                    _readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                    _readback.Apply(false);
                    return new Pixels(Width, Height, _readback.GetPixels32());
                }
                finally
                {
                    RenderTexture.active = previous;
                }
            }

            internal byte[] LastPng() => _readback.EncodeToPNG();

            public void Dispose()
            {
                foreach (Canvas canvas in Canvases)
                    if (canvas.worldCamera == Camera) canvas.worldCamera = null;
                if (Camera != null) Camera.targetTexture = null;
                Target.Release();
                UnityEngine.Object.DestroyImmediate(Target);
                UnityEngine.Object.DestroyImmediate(_readback);
                UnityEngine.Object.DestroyImmediate(_cameraObject);
            }
        }

        CaptureRig Rig(Vector2Int size)
        {
            var rig = new CaptureRig(size);
            _cleanups.Add(rig.Dispose);
            return rig;
        }

        // A screen-size scaler must see the capture size as its screen, or the fixture does not reproduce that screen.
        static void RequireScreenSize(CaptureRig rig, Canvas canvas)
        {
            Vector2 seen = canvas.renderingDisplaySize;
            TestContext.Out.WriteLine($"{canvas.name}: renderingDisplaySize {seen} scaleFactor {canvas.scaleFactor:F4} " +
                $"rect {((RectTransform)canvas.transform).rect.size}");
            Assert.IsTrue(Mathf.Abs(seen.x - rig.Width) < 0.5f && Mathf.Abs(seen.y - rig.Height) < 0.5f,
                $"fixture: {canvas.name} sees a {seen} screen, the capture is {rig.Width}×{rig.Height}");
        }

        // ---------------------------------------------------------------------------------------------------------
        // R observers: approved pieces drawn per spec
        // ---------------------------------------------------------------------------------------------------------

        // One drawn layer of a reference: the screen rectangle and the image settings to draw there.
        sealed class Layer
        {
            internal Rect Screen;
            internal float PixelsPerUnit;
            internal Sprite Sprite;
            internal Image.Type Type;
            internal bool PreserveAspect;
            internal bool FillCenter = true;
            internal float Multiplier = 1f;
            internal Color Colour = Color.white;
        }

        // The approved piece an Image shows (its active sprite), by the .meta GUID of the sprite asset; null otherwise.
        // The table overloads below take the GUID table a window expects (brown PiecesByGuid or the inventory's
        // BrightPiecesByGuid); the judgement is the same (T3·T4, lead msg_8d4e83bd3bb0 1·2).
        static Piece ShownPiece(Image image) => ShownPiece(image, PiecesByGuid);

        static Piece ShownPiece(Image image, Dictionary<string, Piece> table)
        {
            Sprite sprite = image != null ? image.overrideSprite : null;
            if (sprite == null) return null;
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long _) &&
                   table.TryGetValue(guid, out Piece piece) ? piece : null;
        }

        static Piece ShownPieceOf(GameObject go) => go != null && go.TryGetComponent(out Image image) ? ShownPiece(image) : null;

        static Piece PieceNamed(string name) => PieceNamed(name, Pieces);

        static Piece PieceNamed(string name, Piece[] pieces) => pieces.Single(piece => piece.Name == name);

        static Sprite LoadPiece(Piece piece)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(piece.Guid));
            Assert.IsNotNull(sprite, $"fixture: approved {piece.Name} loads from its GUID");
            return sprite;
        }

        static Image[] ApprovedImages(Transform window) => ApprovedImages(window, PiecesByGuid);

        static Image[] ApprovedImages(Transform window, Dictionary<string, Piece> table) => window.GetComponentsInChildren<Image>(false)
            .Where(image => image.enabled && ShownPiece(image, table) != null).ToArray();

        static string[] PiecesUnder(CaptureRig rig, Transform window, Vector2 point, params string[] names) =>
            PiecesUnder(rig, window, point, PiecesByGuid, names);

        static string[] PiecesUnder(CaptureRig rig, Transform window, Vector2 point, Dictionary<string, Piece> table,
            params string[] names) =>
            ApprovedImages(window, table).Where(image => names.Contains(ShownPiece(image, table).Name) &&
                    ScreenRect(rig, image.rectTransform).Contains(point))
                .Select(image => ShownPiece(image, table).Name).ToArray();

        // The window background must be the approved role piece.
        static List<string> RoleViolations(CaptureRig rig, Transform window, RectTransform panel, string expected) =>
            RoleViolations(rig, window, panel, expected, PiecesByGuid);

        static List<string> RoleViolations(CaptureRig rig, Transform window, RectTransform panel, string expected,
            Dictionary<string, Piece> table)
        {
            Image background = Background(rig, window, panel, table);
            string shown = background != null ? ShownPiece(background, table).Name : "no approved piece";
            return shown == expected ? new List<string>() : new List<string> { $"{panel.name} background shows {shown}, approved {expected}" };
        }

        static List<string> ButtonStateViolations(Button button, string expected) => ButtonStateViolations(button, expected, PiecesByGuid);

        static List<string> ButtonStateViolations(Button button, string expected, Dictionary<string, Piece> table)
        {
            string shown = ShownPiece(TargetImage(button), table)?.Name ?? "no approved piece";
            return shown == expected ? new List<string>() : new List<string> { $"shows {shown}, approved {expected}" };
        }

        static Image TargetImage(Button button) => button.targetGraphic as Image;

        // Every approved piece the window draws is compared with the same piece drawn per spec.
        List<string> AllPiecesDrawnPerSpec(CaptureRig rig, Pixels drawn, Transform window) =>
            AllPiecesDrawnPerSpec(rig, drawn, window, PiecesByGuid);

        List<string> AllPiecesDrawnPerSpec(CaptureRig rig, Pixels drawn, Transform window, Dictionary<string, Piece> table) =>
            ApprovedImages(window, table)
            .Where(image => image.gameObject.activeInHierarchy && EffectiveAlpha(image) > 0f)
            .SelectMany(image => DrawnPerSpec(rig, drawn, image, ShownPiece(image, table), PathOf(image.transform), requireCompared: false))
            .ToList();

        // The window background: the approved piece covering exactly the panel's screen rectangle (null when none).
        static Image Background(CaptureRig rig, Transform window, RectTransform panel) => Background(rig, window, panel, PiecesByGuid);

        static Image Background(CaptureRig rig, Transform window, RectTransform panel, Dictionary<string, Piece> table)
        {
            Rect panelRect = ScreenRect(rig, panel);
            return ApprovedImages(window, table).FirstOrDefault(image => SameRect(ScreenRect(rig, image.rectTransform), panelRect));
        }

        // Compares the corner and edge blocks of the subject that no other drawn graphic touches. The reference draws,
        // under the subject, the images drawn before it as the product set them (context), and the subject as the spec
        // says (approved sprite, Sliced or Simple, preserveAspect, multiplier 1, white). With requireCompared false a
        // piece too small for a block at this size, or with every block covered by text drawn after it (the known
        // VIS-01 inset records), is logged as not compared instead of reported.
        List<string> DrawnPerSpec(CaptureRig rig, Pixels drawn, Image subject, Piece piece, string label, bool requireCompared)
        {
            var problems = new List<string>();
            if (subject == null || piece == null)
            {
                problems.Add($"{label}: no approved piece to compare");
                return problems;
            }
            List<(Graphic Graphic, Rect Screen, int Order)> visible = VisibleGraphics(rig);
            int subjectIndex = visible.FindIndex(entry => entry.Graphic == subject);
            if (subjectIndex < 0)
            {
                problems.Add($"{label}: not drawn");
                return problems;
            }
            int subjectOrder = visible[subjectIndex].Order;
            Rect subjectRect = visible[subjectIndex].Screen;
            float pixelsPerUnit = subjectRect.width / Mathf.Max(subject.rectTransform.rect.width, 0.0001f);
            float units = BlockUnits(piece, subject.rectTransform.rect.size);
            List<RectInt> blocks = CandidateBlocks(subjectRect, pixelsPerUnit, units, drawn);
            var under = new List<Image>();
            var usable = new List<RectInt>();
            foreach (RectInt block in blocks)
            {
                var overlapping = visible.Where(entry => entry.Graphic != subject && Overlaps(entry.Screen, block)).ToList();
                if (overlapping.Any(entry => entry.Order > subjectOrder || !(entry.Graphic is Image))) continue;
                usable.Add(block);
                foreach (var entry in overlapping)
                    if (!under.Contains((Image)entry.Graphic)) under.Add((Image)entry.Graphic);
            }
            if (usable.Count == 0)
            {
                string reason = blocks.Count == 0 ? "too small for a block at this size" : "every block is covered by a graphic drawn after it";
                TestContext.Out.WriteLine($"{label} {piece.Name}: not compared ({reason})");
                if (requireCompared) problems.Add($"{label}: not compared ({reason})");
                return problems;
            }
            var layers = under.OrderBy(image => visible.First(entry => entry.Graphic == image).Order)
                .Select(image => AsDrawn(rig, image)).ToList();
            layers.Add(new Layer
            {
                Screen = subjectRect,
                PixelsPerUnit = pixelsPerUnit,
                Sprite = LoadPiece(piece),
                Type = piece.Sliced ? Image.Type.Sliced : Image.Type.Simple,
                PreserveAspect = piece.PreserveAspect,
            });
            Pixels reference = RenderReference(rig, layers);
            foreach (RectInt block in usable)
            {
                (int max, double mean) = Difference(drawn, reference, block);
                TestContext.Out.WriteLine($"{label} {piece.Name} block {block}: max {max} mean {mean:F2}");
                if (max > MaxChannelTolerance || mean > MeanChannelTolerance)
                    problems.Add($"{label}: block {block} differs from {piece.Name} drawn per spec (max {max}, mean {mean:F2})");
            }
            return problems;
        }

        // A context layer as the product set it.
        static Layer AsDrawn(CaptureRig rig, Image image)
        {
            Rect screen = ScreenRect(rig, image.rectTransform);
            return new Layer
            {
                Screen = screen,
                PixelsPerUnit = screen.width / Mathf.Max(image.rectTransform.rect.width, 0.0001f),
                Sprite = image.overrideSprite,
                Type = image.type,
                PreserveAspect = image.preserveAspect,
                FillCenter = image.fillCenter,
                Multiplier = image.pixelsPerUnitMultiplier,
                Colour = image.color * new Color(1f, 1f, 1f, EffectiveAlpha(image) / Mathf.Max(image.color.a, 0.0001f)),
            };
        }

        // Draws the layers on a fixture canvas of the same camera while the attached canvases are switched off.
        static Pixels RenderReference(CaptureRig rig, IEnumerable<Layer> layers)
        {
            List<Canvas> switchedOff = rig.Canvases.Where(canvas => canvas.enabled).ToList();
            var root = new GameObject("Reference canvas (fixture)", typeof(RectTransform));
            root.hideFlags = HideFlags.DontSave;
            try
            {
                foreach (Canvas canvas in switchedOff) canvas.enabled = false;
                var referenceCanvas = root.AddComponent<Canvas>();
                referenceCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                referenceCanvas.worldCamera = rig.Camera;
                referenceCanvas.planeDistance = 100f;
                referenceCanvas.scaleFactor = 1f;
                int index = 0;
                foreach (Layer layer in layers)
                {
                    var go = new GameObject("reference layer " + index++, typeof(RectTransform));
                    go.transform.SetParent(root.transform, false);
                    var rect = (RectTransform)go.transform;
                    rect.anchorMin = rect.anchorMax = Vector2.zero;
                    rect.pivot = Vector2.zero;
                    rect.anchoredPosition = layer.Screen.position;
                    rect.sizeDelta = layer.Screen.size / layer.PixelsPerUnit;
                    rect.localScale = new Vector3(layer.PixelsPerUnit, layer.PixelsPerUnit, 1f);
                    var image = go.AddComponent<Image>();
                    image.sprite = layer.Sprite;
                    image.type = layer.Type;
                    image.preserveAspect = layer.PreserveAspect;
                    image.fillCenter = layer.FillCenter;
                    image.pixelsPerUnitMultiplier = layer.Multiplier;
                    image.color = layer.Colour;
                    image.raycastTarget = false;
                }
                return rig.Capture();
            }
            finally
            {
                foreach (Canvas canvas in switchedOff) canvas.enabled = true;
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        // Only the panel piece may be drawn over the given field; the field's pixels must equal the panel piece
        // drawn per spec (used for the empty portrait field).
        List<string> FieldShowsOnly(CaptureRig rig, Pixels drawn, RectTransform field, RectTransform panel, string panelPiece)
        {
            var problems = new List<string>();
            Rect fieldRect = ScreenRect(rig, field);
            RectInt block = ToBlock(fieldRect, drawn, 2);
            var others = VisibleGraphics(rig).Where(entry => Overlaps(entry.Screen, block) &&
                !(entry.Graphic is Image image && SameRect(entry.Screen, ScreenRect(rig, panel)) && ShownPiece(image)?.Name == panelPiece)).ToList();
            problems.AddRange(others.Select(entry => $"{PathOf(entry.Graphic.transform)} is drawn over the empty portrait field"));
            Rect panelRect = ScreenRect(rig, panel);
            Piece piece = PieceNamed(panelPiece);
            Pixels reference = RenderReference(rig, new[]
            {
                new Layer
                {
                    Screen = panelRect,
                    PixelsPerUnit = panelRect.width / panel.rect.width,
                    Sprite = LoadPiece(piece),
                    Type = Image.Type.Sliced,
                },
            });
            (int max, double mean) = Difference(drawn, reference, block);
            if (max > MaxChannelTolerance || mean > MeanChannelTolerance)
                problems.Add($"empty portrait field differs from {panelPiece} alone (max {max}, mean {mean:F2})");
            return problems;
        }

        // Pixels of the content portrait seen through the frame: with the frame drawn, switching only the portrait
        // off must change the field's centre (an opaque frame centre would hide the portrait).
        static int PortraitPixels(CaptureRig rig, Pixels drawn, Image portrait)
        {
            portrait.enabled = false;
            Pixels without = rig.Capture();
            portrait.enabled = true;
            Rect inner = ScreenRect(rig, portrait.rectTransform);
            // The centre third of the field, inside the frame's content inset (34 PNG px of 256).
            var centre = new Rect(inner.x + inner.width / 3f, inner.y + inner.height / 3f, inner.width / 3f, inner.height / 3f);
            return CountDifferent(drawn, without, ToBlock(centre, drawn));
        }

        // Texts whose glyph area does not change when the text is switched off: art drawn over them hides them.
        static string[] HiddenTexts(CaptureRig rig, Pixels drawn, IEnumerable<TMP_Text> texts)
        {
            var hidden = new List<string>();
            foreach (TMP_Text text in texts)
            {
                Rect glyphs = GlyphRect(rig, text);
                if (glyphs.width <= 0f || glyphs.height <= 0f) continue;
                text.enabled = false;
                Pixels without = rig.Capture();
                text.enabled = true;
                int changed = CountDifferent(drawn, without, ToBlock(glyphs, drawn));
                if (changed == 0) hidden.Add($"{PathOf(text.transform)} 「{text.text}」 is not visible over the art");
            }
            return hidden.ToArray();
        }

        // Characters TMP replaced because neither the font nor its fallback has them. The ellipsis TMP inserts for a
        // truncated text is a truncation (logged with the text), not a missing glyph.
        static string[] MissingGlyphs(IEnumerable<TMP_Text> texts)
        {
            var missing = new List<string>();
            foreach (TMP_Text text in texts)
            {
                text.ForceMeshUpdate();
                TMP_TextInfo info = text.textInfo;
                string source = text.text ?? string.Empty;
                for (int i = 0; i < info.characterCount; i++)
                {
                    TMP_CharacterInfo character = info.characterInfo[i];
                    if (character.index < 0 || character.index >= source.Length) continue;
                    char expected = source[character.index];
                    if (char.IsWhiteSpace(expected) || character.character == expected) continue;
                    if (character.character == '…' && text.isTextTruncated) continue;
                    missing.Add($"{PathOf(text.transform)}: '{expected}' drawn as '{character.character}'");
                }
            }
            return missing.ToArray();
        }

        static TMP_Text[] VisibleTexts(Transform window) => window.GetComponentsInChildren<TMP_Text>(false)
            .Where(text => text.enabled && !string.IsNullOrEmpty(text.text) && EffectiveAlpha(text) > 0f).ToArray();

        // ---------------------------------------------------------------------------------------------------------
        // P observers: the window's own GraphicRaycaster and uGUI pointer events
        // ---------------------------------------------------------------------------------------------------------

        sealed class Press
        {
            internal PointerEventData Data;
            internal GameObject Hit;
            internal GameObject Pressed;
            internal readonly List<GameObject> Entered = new List<GameObject>();
        }

        // GraphicRaycaster results for a screen point after one frame (depths are assigned when the canvas draws).
        static RaycastResult[] Hits(CaptureRig rig, GraphicRaycaster raycaster, Vector2 point)
        {
            rig.Draw();
            var data = new PointerEventData(null) { position = point, button = PointerEventData.InputButton.Left };
            var results = new List<RaycastResult>();
            raycaster.Raycast(data, results);
            return results.ToArray();
        }

        static string[] HitNames(CaptureRig rig, GraphicRaycaster raycaster, Vector2 point) =>
            Hits(rig, raycaster, point).Select(hit => hit.gameObject.name).ToArray();

        // Pointer enter on the top hit and its parents up to the canvas, as the input module sends it.
        static Press PointerEnter(CaptureRig rig, GraphicRaycaster raycaster, Vector2 point)
        {
            RaycastResult[] hits = Hits(rig, raycaster, point);
            var press = new Press { Data = new PointerEventData(null) { position = point, button = PointerEventData.InputButton.Left } };
            if (hits.Length == 0) return press;
            press.Hit = hits[0].gameObject;
            press.Data.pointerCurrentRaycast = hits[0];
            press.Data.pointerEnter = press.Hit;
            for (Transform t = press.Hit.transform; t != null; t = t.parent)
            {
                ExecuteEvents.Execute(t.gameObject, press.Data, ExecuteEvents.pointerEnterHandler);
                press.Entered.Add(t.gameObject);
                if (t == raycaster.transform) break;
            }
            return press;
        }

        static void PointerDown(Press press)
        {
            if (press.Hit == null) return;
            press.Data.pointerPressRaycast = press.Data.pointerCurrentRaycast;
            press.Data.pressPosition = press.Data.position;
            press.Data.eligibleForClick = true;
            GameObject pressed = ExecuteEvents.ExecuteHierarchy(press.Hit, press.Data, ExecuteEvents.pointerDownHandler);
            if (pressed == null) pressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(press.Hit);
            press.Pressed = pressed;
            press.Data.pointerPress = pressed;
            press.Data.rawPointerPress = press.Hit;
        }

        // Pointer up on the pressed object and a click when the click handler under the pointer is the same object.
        static void PointerUp(Press press)
        {
            if (press.Hit == null) return;
            if (press.Pressed != null) ExecuteEvents.Execute(press.Pressed, press.Data, ExecuteEvents.pointerUpHandler);
            GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(press.Hit);
            if (press.Pressed != null && press.Pressed == clickHandler && press.Data.eligibleForClick)
                ExecuteEvents.Execute(press.Pressed, press.Data, ExecuteEvents.pointerClickHandler);
            press.Data.eligibleForClick = false;
            press.Data.pointerPress = null;
            press.Data.rawPointerPress = null;
        }

        static void PointerExit(Press press)
        {
            foreach (GameObject entered in press.Entered)
                if (entered != null) ExecuteEvents.Execute(entered, press.Data, ExecuteEvents.pointerExitHandler);
            press.Entered.Clear();
        }

        // A whole click (enter, down, up, exit); returns the object that took the press, or null.
        static GameObject Click(CaptureRig rig, GraphicRaycaster raycaster, Vector2 point)
        {
            Press press = PointerEnter(rig, raycaster, point);
            PointerDown(press);
            PointerUp(press);
            PointerExit(press);
            return press.Pressed;
        }

        // ---------------------------------------------------------------------------------------------------------
        // H1 records: what GraphicRaycaster (uGUI 2.x) reads on the scene HUD canvas, and probes on that same canvas
        // ---------------------------------------------------------------------------------------------------------

        static readonly System.Reflection.FieldInfo CachedCanvasField =
            typeof(Graphic).GetField("m_Canvas", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        // Raw records must not stop the scenario's own checks; a record that throws is logged as failed.
        static void Record(List<string> raw, string label, Func<IEnumerable<string>> rows)
        {
            try { raw.AddRange(rows()); }
            catch (Exception error) { raw.Add($"{label}: record failed {error.GetType().Name}: {error.Message}"); }
        }

        // Listed means enabled in the canvas's registry set; GraphicRaycaster iterates only those.
        static bool Listed(IList<Graphic> set, Graphic graphic)
        {
            int index = set.IndexOf(graphic);
            return index >= 0 && index < set.Count;
        }

        // The canvas a graphic registered under, read from the field so that reading it does not cache it.
        static string CachedCanvas(Graphic graphic, Canvas expected)
        {
            if (CachedCanvasField == null) return "field not found";
            var cached = CachedCanvasField.GetValue(graphic) as Canvas;
            return cached == null ? "none" : cached == expected ? "this canvas" : cached.name;
        }

        static string HitList(IEnumerable<RaycastResult> hits) =>
            string.Join(", ", hits.Select(hit => $"{PathOf(hit.gameObject.transform)}(depth {hit.depth})"));

        // Registry state of the panel's graphics for the canvas.
        static IEnumerable<string> RegistryRows(string label, Canvas canvas, Transform panel)
        {
            IList<Graphic> all = GraphicRegistry.GetGraphicsForCanvas(canvas);
            IList<Graphic> raycastable = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
            var rows = new List<string> { $"{label}: canvas {canvas.name} listed graphics {all.Count}, listed raycast graphics {raycastable.Count}" };
            foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(true))
                rows.Add($"{label}:   {PathOf(graphic.transform)} {graphic.GetType().Name} active={graphic.gameObject.activeInHierarchy} " +
                    $"enabled={graphic.enabled} raycastTarget={graphic.raycastTarget} listed={Listed(all, graphic)} " +
                    $"raycastListed={Listed(raycastable, graphic)} cachedCanvas={CachedCanvas(graphic, canvas)}");
            return rows;
        }

        // Each filter GraphicRaycaster.Raycast applies, for one graphic at one point after one frame (so depths are
        // current): registry listing, raycastTarget, cull, depth (-1 = not drawn), the rect test, the graphic's own
        // filter walk (its Image and any CanvasGroup above it), then every hit the raycaster reports there.
        static IEnumerable<string> RaycastStages(string label, CaptureRig rig, GraphicRaycaster raycaster, Graphic graphic, Vector2 point)
        {
            RaycastResult[] hits = Hits(rig, raycaster, point);
            Canvas canvas = raycaster.GetComponent<Canvas>();
            Camera eventCamera = raycaster.eventCamera;
            IList<Graphic> raycastable = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
            string path = PathOf(graphic.transform);
            Vector3 viewport = eventCamera != null ? eventCamera.ScreenToViewportPoint(point) : Vector3.zero;
            float screenZ = eventCamera != null ? eventCamera.WorldToScreenPoint(graphic.rectTransform.position).z : 0f;
            float farClip = eventCamera != null ? eventCamera.farClipPlane : 0f;
            bool rectContains = RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, point, eventCamera, graphic.raycastPadding);
            var rows = new List<string>
            {
                $"{label}: point {point} viewport {viewport} raycaster enabled={raycaster.enabled} " +
                    $"eventCamera is fixture camera={eventCamera == rig.Camera} blocking={raycaster.blockingObjects} ignoreReversed={raycaster.ignoreReversedGraphics}",
                $"{label}: canvas {canvas.name} renderMode={canvas.renderMode} worldCamera is fixture camera={canvas.worldCamera == rig.Camera} " +
                    $"targetDisplay={canvas.targetDisplay} camera targetDisplay={rig.Camera.targetDisplay} sortingOrder={canvas.sortingOrder} " +
                    $"listed raycast graphics={raycastable.Count}",
                $"{label}: {path} raycastListed={Listed(raycastable, graphic)} cachedCanvas={CachedCanvas(graphic, canvas)} " +
                    $"raycastTarget={graphic.raycastTarget} activeAndEnabled={graphic.isActiveAndEnabled} cull={graphic.canvasRenderer.cull} " +
                    $"depth={graphic.depth} inheritedAlpha={graphic.canvasRenderer.GetInheritedAlpha():F3} padding={graphic.raycastPadding}",
                $"{label}: {path} rectContainsPoint={rectContains} screenZ={screenZ:F2} farClip={farClip:F2} " +
                    $"graphicRaycast={graphic.Raycast(point, eventCamera)} position={graphic.transform.position} forward={graphic.transform.forward} " +
                    $"camera {rig.Camera.transform.position} forward {rig.Camera.transform.forward}",
            };
            foreach (CanvasGroup group in graphic.GetComponentsInParent<CanvasGroup>(true))
                rows.Add($"{label}: CanvasGroup on {group.name} enabled={group.enabled} alpha={group.alpha:F2} " +
                    $"blocksRaycasts={group.blocksRaycasts} interactable={group.interactable} ignoreParentGroups={group.ignoreParentGroups}");
            rows.Add($"{label}: hits {hits.Length}: {HitList(hits)}");
            return rows;
        }

        // Every active raycastTarget graphic of the canvas (TMP sub-meshes aside) and whether the raycaster reports it
        // at its own centre, with its registry listing, cached canvas, depth and cull.
        static List<(Graphic Graphic, bool OnScreen, bool Hit)> Census(string label, CaptureRig rig, GraphicRaycaster raycaster,
            Canvas canvas, List<string> raw)
        {
            var rows = new List<(Graphic Graphic, bool OnScreen, bool Hit)>();
            foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(false))
            {
                if (!graphic.raycastTarget || graphic is TMP_SubMeshUI) continue;
                Rect screen = ScreenRect(rig, graphic.rectTransform);
                Vector2 point = screen.center;
                bool onScreen = screen.width > 0f && screen.height > 0f &&
                                point.x > 0f && point.y > 0f && point.x < rig.Width && point.y < rig.Height;
                bool hit = Hits(rig, raycaster, point).Any(result => result.gameObject == graphic.gameObject);
                IList<Graphic> raycastable = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);
                rows.Add((graphic, onScreen, hit));
                raw.Add($"{label}:   {PathOf(graphic.transform)} {graphic.GetType().Name} onScreen={onScreen} hitAtCentre={hit} " +
                    $"raycastListed={Listed(raycastable, graphic)} cachedCanvas={CachedCanvas(graphic, canvas)} " +
                    $"depth={graphic.depth} cull={graphic.canvasRenderer.cull}");
            }
            raw.Add($"{label}: raycast graphics {rows.Count}, hit at their centre {rows.Count(row => row.Hit)}");
            return rows;
        }

        // Probes on the HUD canvas itself, stretched over the whole canvas and created inactive.
        sealed class SameCanvasProbes
        {
            internal Image On;
            internal Image Off;
            internal Image Hidden;
        }

        SameCanvasProbes AddSameCanvasProbes(Canvas canvas)
        {
            Image Probe(string name, bool raycastTarget)
            {
                GameObject go = Own(new GameObject(name, typeof(RectTransform)));
                go.SetActive(false);
                go.transform.SetParent(canvas.transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var image = go.AddComponent<Image>();
                image.color = new Color(1f, 0f, 1f, 0.25f);
                image.raycastTarget = raycastTarget;
                return image;
            }
            return new SameCanvasProbes
            {
                On = Probe("Same-canvas probe (fixture, raycastTarget on)", true),
                Off = Probe("Same-canvas probe (fixture, raycastTarget off)", false),
                Hidden = Probe("Same-canvas probe (fixture, never shown)", true),
            };
        }

        // Raycasts with the on and off probes shown; the never-shown probe stays inactive. Both are hidden again after,
        // so no capture draws them.
        static RaycastResult[] ProbeHits(CaptureRig rig, GraphicRaycaster raycaster, SameCanvasProbes probes, Vector2 point)
        {
            probes.On.gameObject.SetActive(true);
            probes.Off.gameObject.SetActive(true);
            try { return Hits(rig, raycaster, point); }
            finally
            {
                probes.On.gameObject.SetActive(false);
                probes.Off.gameObject.SetActive(false);
            }
        }

        // D on the same canvas: the raycastTarget probe is hit at the point, the off and never-shown probes are not.
        static RaycastResult[] ProbeControls(string label, CaptureRig rig, GraphicRaycaster raycaster, SameCanvasProbes probes,
            Vector2 point, List<string> raw, List<string> failures)
        {
            RaycastResult[] hits = ProbeHits(rig, raycaster, probes, point);
            raw.Add($"{label}: hits with probes at {point}: {HitList(hits)}");
            if (!hits.Any(hit => hit.gameObject == probes.On.gameObject)) failures.Add($"{label}: the raycastTarget probe is not hit");
            if (hits.Any(hit => hit.gameObject == probes.Off.gameObject)) failures.Add($"{label}: the raycastTarget-off probe is hit");
            if (hits.Any(hit => hit.gameObject == probes.Hidden.gameObject)) failures.Add($"{label}: the never-shown probe is hit");
            return hits;
        }

        // An empty hit list is evidence only where the observer sees that point; otherwise the case is inconclusive,
        // never passed.
        static void RequireObservable(bool observable, string what)
        {
            if (!observable) Assert.Inconclusive("not observable: " + what);
        }

        // Centres of approved pieces that are not part of a button (panel, header, rows, frame, divider).
        static Vector2[] DecorationPoints(CaptureRig rig, Transform window) => DecorationPoints(rig, window, PiecesByGuid);

        static Vector2[] DecorationPoints(CaptureRig rig, Transform window, Dictionary<string, Piece> table) => ApprovedImages(window, table)
            .Where(image => image.GetComponentInParent<Button>(true) == null && image.GetComponentsInChildren<Button>(true).Length == 0)
            .Select(image => Centre(rig, image.rectTransform)).ToArray();

        // A point on the slot row between its left edge and the use button: the row art, the item text area.
        static Vector2 RowPointLeftOfUse(CaptureRig rig, RectTransform row)
        {
            Rect r = ScreenRect(rig, row);
            return new Vector2(r.xMin + r.width * 0.3f, r.center.y);
        }

        // ---------------------------------------------------------------------------------------------------------
        // Geometry, pixels and fixture helpers
        // ---------------------------------------------------------------------------------------------------------

        static Rect ScreenRect(CaptureRig rig, RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(rig.Camera, corners[0]);
            Vector2 c = RectTransformUtility.WorldToScreenPoint(rig.Camera, corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        static Vector2 Centre(CaptureRig rig, RectTransform rect) => ScreenRect(rig, rect).center;

        // Screen rectangle of the glyphs a text actually draws (empty when it draws none).
        static Rect GlyphRect(CaptureRig rig, TMP_Text text)
        {
            text.ForceMeshUpdate();
            if (text.textInfo.characterCount == 0) return Rect.zero;
            Bounds bounds = text.textBounds;
            if (bounds.size.x <= 0f || bounds.size.y <= 0f) return Rect.zero;
            Vector2 a = RectTransformUtility.WorldToScreenPoint(rig.Camera, text.transform.TransformPoint(bounds.min));
            Vector2 c = RectTransformUtility.WorldToScreenPoint(rig.Camera, text.transform.TransformPoint(bounds.max));
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        // Every graphic the attached canvases draw, in draw order, with the screen area it can paint.
        static List<(Graphic Graphic, Rect Screen, int Order)> VisibleGraphics(CaptureRig rig)
        {
            var visible = new List<(Graphic, Rect, int)>();
            int order = 0;
            foreach (Canvas canvas in rig.Canvases.Where(canvas => canvas.enabled && canvas.gameObject.activeInHierarchy)
                         .OrderBy(canvas => canvas.sortingOrder))
            {
                foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(false))
                {
                    order++;
                    if (!graphic.enabled || graphic is TMP_SubMeshUI || EffectiveAlpha(graphic) <= 0f) continue;
                    if (graphic is Image image && image.overrideSprite == null && image.color.a <= 0f) continue;
                    Rect screen = graphic is TMP_Text text ? GlyphRect(rig, text) : ScreenRect(rig, graphic.rectTransform);
                    if (screen.width <= 0f || screen.height <= 0f) continue;
                    visible.Add((graphic, screen, order));
                }
            }
            return visible;
        }

        static float EffectiveAlpha(Graphic graphic)
        {
            float alpha = graphic.color.a;
            foreach (CanvasGroup group in graphic.GetComponentsInParent<CanvasGroup>())
            {
                alpha *= group.alpha;
                if (group.ignoreParentGroups) break;
            }
            return alpha;
        }

        // Block size in UI units: within the piece's corner where it has one, never more than half the rect.
        static float BlockUnits(Piece piece, Vector2 size)
        {
            float corner = piece.Sliced
                ? Mathf.Min(piece.BorderLeft > 0 ? piece.BorderLeft / Density : CornerUnits, piece.BorderTop > 0 ? piece.BorderTop / Density : CornerUnits)
                : CornerUnits;
            return Mathf.Min(corner, CornerUnits, size.x / 2f, size.y / 2f);
        }

        // Squares inside the rectangle's border band, 1 px away from its edges: the four corners, then the middle of
        // each edge (used when text drawn later covers a corner).
        static List<RectInt> CandidateBlocks(Rect screen, float pixelsPerUnit, float units, Pixels bounds)
        {
            var blocks = new List<RectInt>();
            int size = Mathf.FloorToInt(units * pixelsPerUnit) - 2;
            if (size < 1) return blocks;
            int left = Mathf.CeilToInt(screen.xMin) + 1;
            int bottom = Mathf.CeilToInt(screen.yMin) + 1;
            int right = Mathf.FloorToInt(screen.xMax) - 1 - size;
            int top = Mathf.FloorToInt(screen.yMax) - 1 - size;
            int middleX = Mathf.RoundToInt(screen.center.x - size / 2f);
            int middleY = Mathf.RoundToInt(screen.center.y - size / 2f);
            Vector2Int[] origins =
            {
                new Vector2Int(left, bottom), new Vector2Int(right, bottom), new Vector2Int(left, top), new Vector2Int(right, top),
                new Vector2Int(left, middleY), new Vector2Int(right, middleY), new Vector2Int(middleX, bottom), new Vector2Int(middleX, top),
            };
            foreach (Vector2Int origin in origins)
            {
                var block = new RectInt(origin.x, origin.y, size, size);
                bool inside = block.xMin >= 0 && block.yMin >= 0 && block.xMax <= bounds.Width && block.yMax <= bounds.Height;
                if (inside && !blocks.Contains(block)) blocks.Add(block);
            }
            return blocks;
        }

        static RectInt ToBlock(Rect screen, Pixels bounds, int inset = 0)
        {
            int xMin = Mathf.Clamp(Mathf.CeilToInt(screen.xMin) + inset, 0, bounds.Width);
            int yMin = Mathf.Clamp(Mathf.CeilToInt(screen.yMin) + inset, 0, bounds.Height);
            int xMax = Mathf.Clamp(Mathf.FloorToInt(screen.xMax) - inset, 0, bounds.Width);
            int yMax = Mathf.Clamp(Mathf.FloorToInt(screen.yMax) - inset, 0, bounds.Height);
            return new RectInt(xMin, yMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin));
        }

        static bool Overlaps(Rect screen, RectInt block) =>
            screen.xMin < block.xMax && screen.xMax > block.xMin && screen.yMin < block.yMax && screen.yMax > block.yMin;

        static bool SameRect(Rect a, Rect b) =>
            Mathf.Abs(a.xMin - b.xMin) <= 1f && Mathf.Abs(a.xMax - b.xMax) <= 1f &&
            Mathf.Abs(a.yMin - b.yMin) <= 1f && Mathf.Abs(a.yMax - b.yMax) <= 1f;

        static bool Inside(Rect outer, Rect inner) =>
            inner.xMin >= outer.xMin - 0.5f && inner.yMin >= outer.yMin - 0.5f &&
            inner.xMax <= outer.xMax + 0.5f && inner.yMax <= outer.yMax + 0.5f;

        static (int Max, double Mean) Difference(Pixels a, Pixels b, RectInt block)
        {
            int worst = 0;
            long sum = 0;
            int count = 0;
            for (int y = block.yMin; y < block.yMax; y++)
            {
                for (int x = block.xMin; x < block.xMax; x++)
                {
                    int d = ChannelDifference(a.At(x, y), b.At(x, y));
                    worst = Math.Max(worst, d);
                    sum += d;
                    count++;
                }
            }
            return (worst, count == 0 ? 0.0 : (double)sum / count);
        }

        static int CountDifferent(Pixels a, Pixels b, RectInt block)
        {
            int changed = 0;
            for (int y = block.yMin; y < block.yMax; y++)
                for (int x = block.xMin; x < block.xMax; x++)
                    if (ChannelDifference(a.At(x, y), b.At(x, y)) > DrawnThreshold) changed++;
            return changed;
        }

        static int ChannelDifference(Color32 a, Color32 b) =>
            Math.Max(Math.Max(Math.Abs(a.r - b.r), Math.Abs(a.g - b.g)), Math.Max(Math.Abs(a.b - b.b), Math.Abs(a.a - b.a)));

        static int InsetCount(Transform root) => root.GetComponentsInChildren<Image>(true)
            .Count(image => ShownPiece(image)?.Name.StartsWith("Inset_", StringComparison.Ordinal) == true);

        static int FrameCount(Transform root) => root.GetComponentsInChildren<Image>(true)
            .Count(image => ShownPiece(image)?.Name == "PortraitFrame_128");

        Canvas FixtureCanvas(CaptureRig rig, string name)
        {
            GameObject go = Own(new GameObject(name, typeof(RectTransform)));
            var canvas = go.AddComponent<Canvas>();
            rig.Attach(canvas);
            canvas.scaleFactor = 1f;
            return canvas;
        }

        Image FixtureImage(Transform parent, string name, Vector2 size, Vector2 bottomLeft)
        {
            GameObject go = Own(new GameObject(name, typeof(RectTransform)));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = bottomLeft;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        TMP_Text FixtureText(Transform parent, string name, string content, Vector2 size, Vector2 bottomLeft)
        {
            GameObject go = Own(new GameObject(name, typeof(RectTransform)));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = bottomLeft;
            rect.sizeDelta = size;
            var text = go.AddComponent<TextMeshProUGUI>();
            var font = Resources.Load<TMP_FontAsset>(FontPath);
            Assert.IsNotNull(font, "fixture: Resources font " + FontPath);
            text.font = font;
            text.fontSize = 24f;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }

        Sprite ContentSprite(string name, Color colour)
        {
            var texture = Own(new Texture2D(64, 64, TextureFormat.RGBA32, false));
            texture.SetPixels32(Enumerable.Repeat((Color32)colour, 64 * 64).ToArray());
            texture.Apply();
            Sprite sprite = Own(Sprite.Create(texture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f));
            sprite.name = name;
            return sprite;
        }

        T Own<T>(T owned) where T : UnityEngine.Object
        {
            _owned.Add(owned);
            return owned;
        }

        static string PathOf(Transform t)
        {
            var names = new List<string>();
            for (; t != null && t.GetComponent<Canvas>() == null; t = t.parent) names.Insert(0, t.name);
            return string.Join("/", names);
        }

        static string Sha256(string assetPath)
        {
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(assetPath))).Replace("-", string.Empty);
        }

        // Records the approved inset against the existing text rectangles (VIS-01/02 input for the morning check).
        static void LogGeometry(CaptureRig rig, InventoryWindow window)
        {
            float pixelsPerUnit = ScreenRect(rig, window.Panel).width / window.Panel.rect.width;
            var lines = new List<string>();
            foreach (RectTransform row in window.Slots.Take(1))
            {
                RectTransform item = (RectTransform)row.Find("Item");
                if (item == null) continue;
                float left = (ScreenRect(rig, item).xMin - ScreenRect(rig, row).xMin) / pixelsPerUnit;
                lines.Add($"Slot1 Item text starts {left:F1} UI into the row (Inset_Default content inset 16 px / {Density} = {16f / Density} UI)");
            }
            Log("inventory geometry (VIS-01/02 record, not asserted)", lines);
        }

        static void SaveScreen(CaptureRig rig, string name)
        {
            string directory = Environment.GetEnvironmentVariable(EvidenceDirectoryVariable);
            if (string.IsNullOrEmpty(directory)) return;
            string full = Path.GetFullPath(directory);
            string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Assert.IsFalse(full.StartsWith(project, StringComparison.OrdinalIgnoreCase),
                $"fixture: {EvidenceDirectoryVariable} must be outside the Unity project ({full})");
            Directory.CreateDirectory(full);
            File.WriteAllBytes(Path.Combine(full, $"fixture-{name}-{rig.Width}x{rig.Height}.png"), rig.LastPng());
        }

        static void Log(string label, IEnumerable<string> lines)
        {
            string[] all = lines.ToArray();
            TestContext.Out.WriteLine($"[{label}] {all.Length}");
            foreach (string line in all) TestContext.Out.WriteLine("  " + line);
        }
    }
}
