using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Dawnholder.Client.Gameplay;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dawnholder.Client.Tests
{
    // 06 approved UI window application, written before the implementation
    // (01_Phases/goals/2026-10-13-demo-video-art/ui-window-application-scope.md 「만들 것」「보존 계약」「완료조건」 1·3).
    // A  The existing builders (InventoryPanelView ctor, DungeonClearRewardWindow / PartyInvitePopup / NpcDialogPanel
    //    BuildRuntime, PartyMemberHud runtime init on a fixture shaped like Scenes/99.UI/UI.unity Party_Info) must show
    //    the approved brown B1 pieces. Which piece a role uses comes from the approved kit previews (kit-spec.json
    //    previews[].placements, source SHA256 5986771c…); the per-image rules come from Art/UI/Components/README.md and
    //    import-spec.json.
    // B  The shared composition lives under Resources/UI and references the approved sprite GUIDs; no PNG copies.
    // C  What the art change must keep: canvas order and scaler, default texts, panel and input-area rects, one
    //    listener per button, hidden start, show/hide, subscriptions and the path names other code and tests use.
    //    Literals come from the baseline sources (4728e493) and the approved preview's "current" records.
    // D  Controls run the same observers on temporary objects, so a plain colour, a foreign sprite, a wrong image
    //    setting, a tinted or foreign-state button and an input-blocking decoration are shown to be detected.
    // Expected values never come from the code under test: the observers only read what Unity built.
    // Every test that builds DungeonClearRewardWindow is named RewardWindow_*, so a name filter can run or hold
    // them as a group; they read only BuildRuntime/BuildView output, which the Content C9 version keeps.
    public sealed class UiWindowAppearanceContractTests
    {
        const string KitFolder = "Assets/Art/UI/Components/";
        const string ResourcesUiFolder = "Assets/Resources/UI";
        const string FontPath = "Fonts & Materials/LiberationSans SDF";
        // import-spec units.pixelDensity: PNG px per UI unit (sprite PPU 200 on a Canvas reference PPU of 100).
        const float Density = 2f;
        // UI units. Layout literals here are whole units; this only absorbs float rounding through transforms.
        const float Tolerance = 0.5f;
        const string NpcLine = "대장장이: 오늘도 좋은 쇠가 들어왔다네. 강화가 필요하면 말하게.";

        enum Kind { Panel, Inset, Header, Button, ItemSlot, IconBase, CircleButton, NumberBadge, PortraitFrame, Divider, Corner, HeaderEnd }

        // One approved B1 piece: name, meta GUID, kind, mode and border L/R/T/B in PNG px, copied from
        // Art/UI/Components/import-spec.json sprites[] and the 33 .meta files at 4728e493.
        sealed class Piece
        {
            internal readonly string Name;
            internal readonly string Guid;
            internal readonly Kind Kind;
            internal readonly bool Sliced;
            internal readonly int Left, Right, Top, Bottom;

            internal Piece(string name, string guid, Kind kind, bool sliced, int left, int right, int top, int bottom)
            {
                Name = name;
                Guid = guid;
                Kind = kind;
                Sliced = sliced;
                Left = left;
                Right = right;
                Top = top;
                Bottom = bottom;
            }

            internal string AssetPath => KitFolder + Name + ".png";
        }

        static Piece Sliced(string name, string guid, Kind kind, int border) =>
            new Piece(name, guid, kind, true, border, border, border, border);

        static Piece Simple(string name, string guid, Kind kind) => new Piece(name, guid, kind, false, 0, 0, 0, 0);

        static readonly Piece[] Pieces =
        {
            Sliced("Panel_Default", "f5e7cab1fd4f4b9884c6d0f6454fccb6", Kind.Panel, 24),
            Sliced("Panel_Emphasis", "7dec580f6ab94568af987e047f5a2c36", Kind.Panel, 24),
            Sliced("Panel_Dialog", "40bd7251781d4d92a9fb572a4b350bc2", Kind.Panel, 24),
            Sliced("Inset_Default", "baf557470603430e83015dc40371610a", Kind.Inset, 12),
            Sliced("Inset_Selected", "e2c7e2ef02fc41d38527480b1023601b", Kind.Inset, 12),
            Sliced("Inset_Disabled", "dd85577f51e84c7188a5a8d8a3b03517", Kind.Inset, 12),
            Sliced("Header", "1371d63dc50f40ebae0726a4d60203b5", Kind.Header, 16),
            Sliced("Button_Default_Normal", "fb2ddb62027b4f0fa813440da5fe936b", Kind.Button, 16),
            Sliced("Button_Default_Highlighted", "27e1cc2d96474400b728f433782ec5d3", Kind.Button, 16),
            Sliced("Button_Default_Pressed", "8974b3847d14431b949a496b6cb1ae8f", Kind.Button, 16),
            Sliced("Button_Default_Disabled", "3283e29e3b0e48c48b364a8837a889bf", Kind.Button, 16),
            Sliced("Button_Accept_Normal", "304984e555104bf5b7d8648af717c165", Kind.Button, 16),
            Sliced("Button_Accept_Highlighted", "ce276490b85b4f7fa5f674b0f2546e87", Kind.Button, 16),
            Sliced("Button_Accept_Pressed", "0525c0bba70a41ad8bf71baebe25bc9f", Kind.Button, 16),
            Sliced("Button_Accept_Disabled", "296d9f9387434c4cbc07115da1352309", Kind.Button, 16),
            Sliced("Button_Decline_Normal", "2b776157d2a945229c5cb20bac70bcff", Kind.Button, 16),
            Sliced("Button_Decline_Highlighted", "1e8a01af6d0f46128d05c89866f20cd4", Kind.Button, 16),
            Sliced("Button_Decline_Pressed", "3e4ac54569094f8a9050a567ab5df3d8", Kind.Button, 16),
            Sliced("Button_Decline_Disabled", "ef2f8625204c492190b2d4c58a0121a1", Kind.Button, 16),
            Sliced("ItemSlot_Empty", "d5561f1d095f487a9fdcbd53febb2340", Kind.ItemSlot, 16),
            Sliced("ItemSlot_Filled", "e56f32f7404a4db188dd772d4fa46887", Kind.ItemSlot, 16),
            Sliced("ItemSlot_Selected", "7111abac3f5d47acb6bf5990fb04640c", Kind.ItemSlot, 16),
            Simple("IconBase_64", "22c20957275a444890958a62543db49f", Kind.IconBase),
            Simple("IconBase_96", "855b3dc208e844c992f6e37547fbf506", Kind.IconBase),
            Simple("CircleButton_Normal", "474e3ae378cc4b9991476f044b9c884b", Kind.CircleButton),
            Simple("CircleButton_Highlighted", "9f4a6e74cde643568a4e7b44ee97a246", Kind.CircleButton),
            Simple("CircleButton_Pressed", "52260cf68ffe4c6e9dda1c42a7cdbcb0", Kind.CircleButton),
            Simple("CircleButton_Disabled", "65e146fca12141b583582f207b9abca3", Kind.CircleButton),
            Simple("NumberBadge", "a87faf39aa5c4bc992758bfa8b60ea00", Kind.NumberBadge),
            Simple("PortraitFrame_128", "953890eb7a1a468bb096fdfeff81234d", Kind.PortraitFrame),
            new Piece("Divider_Horizontal", "66a1c6ad27d746138fffd6631bc59c56", Kind.Divider, true, 16, 16, 0, 0),
            Simple("Ornament_Corner", "db10b0962c964d5abacf20d000215a8c", Kind.Corner),
            Simple("Ornament_HeaderEnd", "1c5736df500a4b60bc27adc75eaec82d", Kind.HeaderEnd),
        };

        // Declared after Pieces: static initializers run in text order.
        static readonly Dictionary<string, Piece> PiecesByGuid = Pieces.ToDictionary(piece => piece.Guid);

        // Approved preview "current" rects kept by the composition (inventory-0 window, InventoryPanelView.cs:68-76):
        // top-left offset from the panel's top-left and size, UI units, y downward.
        static readonly Rect[] InventorySlotRects =
        {
            new Rect(12f, 134f, 257f, 35f), new Rect(281f, 134f, 257f, 35f),
            new Rect(12f, 177f, 257f, 35f), new Rect(281f, 177f, 257f, 35f),
            new Rect(12f, 220f, 257f, 35f), new Rect(281f, 220f, 257f, 35f),
            new Rect(12f, 263f, 257f, 35f), new Rect(281f, 263f, 257f, 35f),
        };

        static readonly Rect[] InventoryUseRects =
        {
            new Rect(215f, 134f, 50f, 35f), new Rect(484f, 134f, 50f, 35f),
            new Rect(215f, 177f, 50f, 35f), new Rect(484f, 177f, 50f, 35f),
            new Rect(215f, 220f, 50f, 35f), new Rect(484f, 220f, 50f, 35f),
            new Rect(215f, 263f, 50f, 35f), new Rect(484f, 263f, 50f, 35f),
        };

        readonly List<UnityEngine.Object> _owned = new List<UnityEngine.Object>();
        readonly List<Action> _cleanups = new List<Action>();

        [SetUp]
        public void Setup()
        {
            Assert.IsNull(NpcDialogPanel.Instance, "fixture: another test left the NPC dialog singleton");
            Assert.IsTrue(PartyState.Instance == null, "fixture: another test left a published PartyState");
            Assert.IsNull(DungeonClearRewardWindow.Instance, "fixture: another test left a reward window singleton");
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
        // A. Approved pieces on the real builders
        // ---------------------------------------------------------------------------------------------------------

        // Approved preview inventory-0: Panel_Default behind the whole 550×354 panel.
        [Test]
        public void InventoryPanel_Background_IsTheApprovedDefaultPanel()
        {
            InventoryWindow window = BuildInventory(_ => { }, () => { });
            Image background = Background(window.Panel);
            Log("inventory", window.Root.transform);

            Assert.AreEqual("Panel_Default", ApprovedPiece(background?.sprite)?.Name,
                $"A inventory background shows {Describe(background)}, approved Panel_Default");
        }

        // Approved preview: refresh and use buttons are Button_Default; scope 「보존 계약」: the normal, highlighted,
        // pressed and disabled states are the approved sprites.
        [Test]
        public void InventoryPanel_RefreshAndUseButtons_ShowTheApprovedDefaultButtonStates()
        {
            InventoryWindow window = BuildInventory(_ => { }, () => { });
            var problems = new List<string>();
            problems.AddRange(ButtonViolations(window.Refresh, "Default").Select(p => "Refresh: " + p));
            for (int i = 0; i < window.Uses.Length; i++)
                problems.AddRange(ButtonViolations(window.Uses[i], "Default").Select(p => $"Slot{i + 1}/Use: " + p));
            Log("inventory buttons", problems);

            Assert.IsEmpty(problems, "A inventory buttons use the approved Button_Default states");
        }

        // Scope 「제품 쓰기 경계」 inventory 「칸」; approved preview rows: Inset_* row and ItemSlot_* square per slot.
        [Test]
        public void InventoryPanel_EachOfTheEightSlots_StandsOnAnApprovedSlotPiece()
        {
            InventoryWindow window = BuildInventory(_ => { }, () => { });
            string[] bare = Enumerable.Range(0, window.Slots.Length)
                .Where(i => PiecesUnder(window.Panel, Centre(window.Panel, window.Slots[i]), Kind.Inset, Kind.ItemSlot).Length == 0)
                .Select(i => "Slot" + (i + 1)).ToArray();
            Log("inventory slots without a slot piece", bare);

            Assert.IsEmpty(bare, "A every inventory slot row stands on an approved Inset_* or ItemSlot_* piece");
        }

        // Goal 완료조건 1 「임시 그림·단색 대신 공통 조각의 조합」 with README/import-spec image rules.
        [Test]
        public void InventoryPanel_Chrome_IsOnlyApprovedPiecesUsedAsSpecified()
        {
            InventoryWindow window = BuildInventory(_ => { }, () => { });
            List<string> violations = ChromeViolations(window.Root.transform, Background(window.Panel), Array.Empty<Graphic>());
            Log("inventory chrome", violations);

            Assert.IsEmpty(violations, "A inventory shows no plain colour or foreign sprite and uses each piece per spec");
        }

        // Approved preview dungeon-reward-0: Panel_Emphasis behind the whole 520×364 panel.
        [Test]
        public void RewardWindow_Background_IsTheApprovedEmphasisPanel()
        {
            RewardWindow window = BuildReward();
            Image background = Background(window.Panel);
            Log("reward", window.Window.transform);

            Assert.AreEqual("Panel_Emphasis", ApprovedPiece(background?.sprite)?.Name,
                $"A reward background shows {Describe(background)}, approved Panel_Emphasis");
        }

        // Scope reward 「배경·제목·보상줄·확인 버튼」; approved preview: Header under the title, Inset_* under each row.
        [Test]
        public void RewardWindow_TitleAndRewardRows_StandOnApprovedHeaderAndInsetPieces()
        {
            RewardWindow window = BuildReward();
            Assert.IsNotNull(window.Title, "fixture: the 「던전 클리어」 title text (C checks it separately)");
            bool titleOnHeader = PiecesUnder(window.Panel, Centre(window.Panel, window.Title.rectTransform), Kind.Header).Length > 0;
            string[] bareRows = window.Rows.Where(row => PiecesUnder(window.Panel, Centre(window.Panel, row.rectTransform), Kind.Inset).Length == 0)
                .Select(row => row.name).ToArray();
            Log("reward rows without an inset", bareRows);

            Assert.IsTrue(titleOnHeader, "A the 「던전 클리어」 title stands on the approved Header");
            Assert.IsEmpty(bareRows, "A every reward row stands on an approved Inset_* piece");
        }

        // Approved preview dungeon-reward-0: 「확인」 is Button_Accept (green) with its four approved states.
        [Test]
        public void RewardWindow_Confirm_ShowsTheApprovedAcceptButtonStates()
        {
            RewardWindow window = BuildReward();
            List<string> problems = ButtonViolations(window.Confirm, "Accept");
            Log("reward confirm", problems);

            Assert.IsEmpty(problems, "A 「확인」 uses the approved Button_Accept states");
        }

        [Test]
        public void RewardWindow_Chrome_IsOnlyApprovedPiecesUsedAsSpecified()
        {
            RewardWindow window = BuildReward();
            List<string> violations = ChromeViolations(window.Window.transform, Background(window.Panel), Array.Empty<Graphic>());
            Log("reward chrome", violations);

            Assert.IsEmpty(violations, "A reward window shows no plain colour or foreign sprite and uses each piece per spec");
        }

        // Approved preview party-invite-0: Panel_Default behind the whole 400×180 panel.
        [Test]
        public void PartyInvite_Background_IsTheApprovedDefaultPanel()
        {
            InviteWindow window = BuildInvite();
            Image background = Background(window.Panel);
            Log("party invite", window.Popup.transform);

            Assert.AreEqual("Panel_Default", ApprovedPiece(background?.sprite)?.Name,
                $"A party invite background shows {Describe(background)}, approved Panel_Default");
        }

        // Scope 「보존 계약」 「수락 녹색·거절 적색을 보존」 through the approved Accept / Decline sprites, not a tint.
        [Test]
        public void PartyInvite_AcceptAndReject_ShowTheApprovedGreenAndRedButtonStates()
        {
            InviteWindow window = BuildInvite();
            var problems = new List<string>();
            problems.AddRange(ButtonViolations(window.Accept, "Accept").Select(p => "수락: " + p));
            problems.AddRange(ButtonViolations(window.Reject, "Decline").Select(p => "거절: " + p));
            Log("party invite buttons", problems);

            Assert.IsEmpty(problems, "A 수락 uses Button_Accept and 거절 uses Button_Decline states");
        }

        [Test]
        public void PartyInvite_Chrome_IsOnlyApprovedPiecesUsedAsSpecified()
        {
            InviteWindow window = BuildInvite();
            List<string> violations = ChromeViolations(window.Popup.transform, Background(window.Panel), Array.Empty<Graphic>());
            Log("party invite chrome", violations);

            Assert.IsEmpty(violations, "A party invite shows no plain colour or foreign sprite and uses each piece per spec");
        }

        // Scope PartyMemberHud 「기존 serialized root/슬롯의 runtime 표시 초기화」; approved preview party-members:
        // Panel_Default root, an Inset_* row under each member. Class icons are content and stay as they are.
        [Test]
        public void PartyMemberHud_RuntimeInit_ShowsTheApprovedPanelAndMemberRows()
        {
            PartyHud hud = BuildPartyHud(runLifecycle: true);
            Image background = Background(hud.Root);
            string[] bareMembers = hud.Texts.Where(text => PiecesUnder(hud.Root, Centre(hud.Root, text.rectTransform), Kind.Inset).Length == 0)
                .Select(text => text.text).ToArray();
            List<string> violations = ChromeViolations(hud.Root, background, hud.ClassIcons);
            Log("party members", hud.Root);
            Log("party members chrome", violations);

            Assert.AreEqual("Panel_Default", ApprovedPiece(background?.sprite)?.Name,
                $"A party member panel shows {Describe(background)}, approved Panel_Default");
            Assert.IsEmpty(bareMembers, "A each member row stands on an approved Inset_* piece");
            Assert.IsEmpty(violations, "A the party member panel shows no foreign sprite and uses each piece per spec");
        }

        // Approved preview npc-dialog: Panel_Dialog replaces the borrowed UI/Menu_Button behind the 700×160 panel.
        [Test]
        public void NpcDialog_Background_IsTheApprovedDialogPanel()
        {
            NpcWindow window = BuildNpcDialog(show: true);
            Image background = Background(window.Panel);
            Log("npc dialog", window.Dialog.transform);

            Assert.AreEqual("Panel_Dialog", ApprovedPiece(background?.sprite)?.Name,
                $"A NPC dialog background shows {Describe(background)}, approved Panel_Dialog");
        }

        // Scope NpcDialogPanel 「초상화틀」; approved preview: PortraitFrame_128 over the 128×128 portrait field.
        [Test]
        public void NpcDialog_ShownPortrait_SitsInTheApprovedPortraitFrame()
        {
            NpcWindow window = BuildNpcDialog(show: true);
            Piece[] frames = PiecesUnder(window.Panel, Centre(window.Panel, window.Portrait.rectTransform), Kind.PortraitFrame);

            Assert.AreEqual(new[] { "PortraitFrame_128" }, frames.Select(frame => frame.Name).ToArray(),
                "A the shown portrait sits in exactly one approved PortraitFrame_128");
            Assert.AreSame(window.PortraitSprite, window.Portrait.sprite, "A the portrait Image itself keeps showing the content sprite");
        }

        [Test]
        public void NpcDialog_Chrome_IsOnlyApprovedPiecesUsedAsSpecified()
        {
            NpcWindow window = BuildNpcDialog(show: true);
            List<string> violations = ChromeViolations(window.Dialog.transform, Background(window.Panel), new Graphic[] { window.Portrait });
            Log("npc dialog chrome", violations);

            Assert.IsEmpty(violations, "A NPC dialog shows no plain colour or foreign sprite and uses each piece per spec");
        }

        // ---------------------------------------------------------------------------------------------------------
        // B. Shared composition under Resources/UI
        // ---------------------------------------------------------------------------------------------------------

        // Scope 「설계와 대안」: Sprite references and composition prefabs as serialized assets read through the
        // existing Resources boundary, in a Resources/UI subfolder, pointing at the approved GUIDs.
        [Test]
        public void ResourcesUi_HoldsASharedCompositionThatReferencesTheApprovedSprites()
        {
            Dictionary<string, string[]> compositions = CompositionAssets();
            string[] inSubfolder = compositions.Keys
                .Where(path => path.Substring(ResourcesUiFolder.Length + 1).Contains("/")).ToArray();
            Log("Resources/UI compositions", compositions.Select(entry => entry.Key + " -> " + string.Join(", ", entry.Value)));

            Assert.IsNotEmpty(compositions, "B an asset under Resources/UI references approved Art/UI/Components sprites by GUID");
            Assert.IsNotEmpty(inSubfolder, "B the composition assets sit in a Resources/UI subfolder");
        }

        // The pieces section A requires must be reachable from the Resources/UI composition, the only runtime path
        // for code-built windows that may not use AssetDatabase. Inventory, party invite, party members and NPC here;
        // the reward window's own pieces are checked by RewardWindow_PiecesAreReachableFromTheResourcesUiComposition.
        [Test]
        public void ResourcesUi_CompositionReachesThePiecesOfInventoryPartyAndNpcViews()
        {
            string[] reachable = ReachableFromCompositions();
            string[] needed =
            {
                "Panel_Default", "Panel_Dialog", "PortraitFrame_128",
                "Button_Default_Normal", "Button_Default_Highlighted", "Button_Default_Pressed", "Button_Default_Disabled",
                "Button_Accept_Normal", "Button_Accept_Highlighted", "Button_Accept_Pressed", "Button_Accept_Disabled",
                "Button_Decline_Normal", "Button_Decline_Highlighted", "Button_Decline_Pressed", "Button_Decline_Disabled",
            };
            string[] missing = needed.Where(name => !reachable.Contains(KitFolder + name + ".png")).ToArray();
            bool insetReachable = reachable.Any(path => path.StartsWith(KitFolder + "Inset_", StringComparison.Ordinal));
            Log("pieces not reachable from Resources/UI", missing);

            Assert.IsEmpty(missing, "B every piece the inventory, party and NPC views need is referenced from Resources/UI");
            Assert.IsTrue(insetReachable, "B an Inset_* piece for rows is referenced from Resources/UI");
        }

        // Reward window only (approved preview dungeon-reward-0): Panel_Emphasis and the title Header.
        [Test]
        public void RewardWindow_PiecesAreReachableFromTheResourcesUiComposition()
        {
            string[] reachable = ReachableFromCompositions();
            string[] missing = new[] { "Panel_Emphasis", "Header" }
                .Where(name => !reachable.Contains(KitFolder + name + ".png")).ToArray();

            Assert.IsEmpty(missing, "B the reward window's Panel_Emphasis and Header are referenced from Resources/UI");
        }

        // Scope 「설계와 대안」 「원본 PNG는 Assets/Art/UI/Components에 그대로 두고 GUID로 참조한다 … PNG 복제는 하지 않는다」.
        [Test]
        public void ResourcesFolders_HoldNoCopyOfAnApprovedPng()
        {
            Dictionary<string, string> approvedByHash = Pieces.ToDictionary(piece => Sha256(piece.AssetPath), piece => piece.Name);
            string[] copies = Directory.GetDirectories("Assets", "Resources", SearchOption.AllDirectories)
                .SelectMany(folder => Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                .Where(file => !file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .Where(file => approvedByHash.ContainsKey(Sha256(file)))
                .Select(file => file.Replace('\\', '/') + " = " + approvedByHash[Sha256(file)]).ToArray();
            Log("approved PNG copies under Resources", copies);

            Assert.IsEmpty(copies, "B no Resources folder holds a byte copy of an approved PNG");
        }

        // Scope 「기존 리소스 이동·덮어쓰기 없음」: the existing Resources/UI assets keep their paths and GUIDs (.meta at 4728e493).
        [Test]
        public void ResourcesUi_ExistingAssetsKeepTheirPathsAndGuids()
        {
            var expected = new Dictionary<string, string>
            {
                ["Menu_Button.png"] = "4e3ed8b84d64e6c4f9d9e033443da675",
                ["Quest_Panel.png"] = "fc312c717e8716a409f8c1e1b9609476",
                ["QuestAvailable.png"] = "69920ac7e141e8e48a0edc73533c6649",
                ["QuestClear.png"] = "83e6686b782f1ef4da4d2e05814e450d",
                ["StageClearAnim.anim"] = "3a2b1f9904525d044ad55e9e63d2bd73",
                ["StageClearAnim.controller"] = "e3d54cce1cb22a6419d8413ca5b4c524",
                ["Status_Frame.png"] = "3266207a472f35d4bba05be5a719c18d",
            };
            string[] moved = expected.Where(entry => AssetDatabase.AssetPathToGUID(ResourcesUiFolder + "/" + entry.Key) != entry.Value)
                .Select(entry => entry.Key).ToArray();

            Assert.IsEmpty(moved, "B existing Resources/UI assets keep their path and GUID");
        }

        // Fixture check (not a requirement): the 33 approved GUIDs resolve to the imported kit sprites. If this
        // fails, every A/B result above is a fixture or environment failure, not a product RED.
        [Test]
        public void Fixture_ApprovedPieceGuids_LoadTheImportedKitSprites()
        {
            string[] broken = Pieces.Where(piece =>
            {
                string path = AssetDatabase.GUIDToAssetPath(piece.Guid);
                Sprite sprite = path == piece.AssetPath ? AssetDatabase.LoadAssetAtPath<Sprite>(path) : null;
                return sprite == null || ApprovedPiece(sprite) != piece;
            }).Select(piece => piece.Name).ToArray();

            Assert.AreEqual(33, Pieces.Length, "fixture: the approved table has 33 pieces");
            Assert.IsEmpty(broken, "fixture: each approved GUID loads its kit sprite");
        }

        // ---------------------------------------------------------------------------------------------------------
        // C. Behaviour and layout the art change must keep
        // ---------------------------------------------------------------------------------------------------------

        [Test]
        public void InventoryPanel_KeepsCanvasTextsPathsInputAreasCallbacksAndVisibility()
        {
            var uses = new List<int>();
            int refreshes = 0;
            InventoryWindow window = BuildInventory(uses.Add, () => refreshes++);
            Transform root = window.Root.transform;
            var canvas = window.Root.GetComponent<Canvas>();
            var scaler = window.Root.GetComponent<CanvasScaler>();
            var group = window.Root.GetComponent<CanvasGroup>();
            Rect refreshRect = FromTopLeft(window.Panel, (RectTransform)window.Refresh.transform);
            Rect[] slotRects = window.Slots.Select(slot => FromTopLeft(window.Panel, slot)).ToArray();
            Rect[] useRects = window.Uses.Select(use => FromTopLeft(window.Panel, (RectTransform)use.transform)).ToArray();
            int refreshListeners = Listeners(window.Refresh.onClick);
            int[] useListeners = window.Uses.Select(use => Listeners(use.onClick)).ToArray();
            string[] missingPaths = new[] { "Inventory", "Inventory/Currency", "Inventory/Synchronization", "Inventory/Refresh" }
                .Concat(Enumerable.Range(1, 8).SelectMany(n => new[] { $"Inventory/Slot{n}/Item", $"Inventory/Slot{n}/Use" }))
                .Where(path => root.Find(path) == null).ToArray();
            string[] raycastOutsideButtons = RaycastTargetsOutsideButtons(root);
            string[] foreignFonts = TextsWithoutProjectFont(root);
            bool titleShown = root.GetComponentsInChildren<TMP_Text>(true).Any(text => text.text == "인벤토리");

            window.Refresh.onClick.Invoke();
            foreach (Button use in window.Uses) use.onClick.Invoke();
            window.View.SetVisibility(false, false);
            float hiddenAlpha = group.alpha;
            bool hiddenInteractable = group.interactable, hiddenBlocks = group.blocksRaycasts;
            window.View.SetVisibility(true, true);

            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, "C overlay canvas (InventoryPanelView.cs:31)");
            Assert.AreEqual(5, canvas.sortingOrder, "C sorting order 5, under HUD 10 and menu 20 (InventoryPanelView.cs:32)");
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode, "C scaler mode (InventoryPanelView.cs:34)");
            AssertVector(new Vector2(1920f, 1080f), scaler.referenceResolution, "C reference resolution (InventoryPanelView.cs:35)");
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight, scaler.screenMatchMode, "C match mode (InventoryPanelView.cs:36)");
            Assert.AreEqual(0.5f, scaler.matchWidthOrHeight, 1e-4f, "C match 0.5 (InventoryPanelView.cs:37)");
            Assert.AreEqual(100f, scaler.referencePixelsPerUnit, 1e-4f, "C Canvas reference PPU 100 (scope 「Canvas reference PPU 100」)");
            Assert.IsNotNull(window.Root.GetComponent<GraphicRaycaster>(), "C graphic raycaster (InventoryPanelView.cs:38)");
            Assert.IsEmpty(missingPaths, "C names other code and tests find (InventoryPanelToggleTests, InventorySceneLifecycleTests, Content reward window)");
            AssertVector(new Vector2(550f, 354f), window.Panel.rect.size, "C panel size (InventoryPanelView.cs:15-16)");
            AssertVector(new Vector2(0.5f, 1f), window.Panel.anchorMin, "C panel anchored top centre (InventoryPanelView.cs:45)");
            AssertVector(new Vector2(0.5f, 1f), window.Panel.anchorMax, "C panel anchored top centre (InventoryPanelView.cs:45)");
            AssertVector(new Vector2(0.5f, 1f), window.Panel.pivot, "C panel pivot (InventoryPanelView.cs:46)");
            Assert.IsTrue(titleShown, "C the 「인벤토리」 title text (InventoryPanelView.cs:53)");
            AssertRect(new Rect(426f, 10f, 112f, 44f), refreshRect, "C refresh input area (InventoryPanelView.cs:58)");
            for (int i = 0; i < 8; i++)
            {
                AssertRect(InventorySlotRects[i], slotRects[i], $"C Slot{i + 1} row rect (InventoryPanelView.cs:68-70)");
                AssertRect(InventoryUseRects[i], useRects[i], $"C Slot{i + 1} use input area (InventoryPanelView.cs:76)");
            }
            Assert.AreEqual(1, refreshListeners, "C one refresh listener (InventoryPanelView.cs:60)");
            CollectionAssert.AreEqual(Enumerable.Repeat(1, 8).ToArray(), useListeners, "C one use listener per slot (InventoryPanelView.cs:77)");
            Assert.AreEqual(1, refreshes, "C refresh click reaches the refresh callback once");
            CollectionAssert.AreEqual(Enumerable.Range(0, 8).ToArray(), uses, "C each use button reports its own slot index once");
            Assert.IsEmpty(raycastOutsideButtons, "C only the buttons take raycasts; screen and slot backgrounds pass input (InventoryPanelView.cs:95)");
            Assert.IsEmpty(foreignFonts, "C texts keep the LiberationSans SDF font with the Korean fallback (InventoryPanelView.cs:43)");
            Assert.AreEqual(0f, hiddenAlpha, "C hidden alpha (InventoryPanelView.cs:92)");
            Assert.IsFalse(hiddenInteractable, "C hidden is not interactable (InventoryPanelView.cs:93)");
            Assert.IsFalse(hiddenBlocks, "C hidden blocks no raycast (InventoryPanelView.cs:94)");
            Assert.AreEqual(1f, group.alpha, "C shown alpha");
            Assert.IsTrue(group.interactable && group.blocksRaycasts, "C shown and interactive");
        }

        // Builder output only: the Content baseline moves placement and publication to LateUpdate (Content
        // art-window-baseline copy SHA256 01c5c3bb…); BuildRuntime/BuildView are the same in both versions.
        [Test]
        public void RewardWindow_KeepsCanvasTextsRectsListenerAndHiddenStart()
        {
            RewardWindow window = BuildReward();
            Transform root = window.Window.transform;
            var canvas = window.Window.GetComponent<Canvas>();
            var scaler = window.Window.GetComponent<CanvasScaler>();
            var group = window.Window.GetComponent<CanvasGroup>();
            Rect confirmRect = FromTopLeft(window.Panel, (RectTransform)window.Confirm.transform);
            Rect[] rowRects = window.Rows.Select(row => FromTopLeft(window.Panel, row.rectTransform)).ToArray();
            int confirmListeners = Listeners(window.Confirm.onClick);
            string[] raycastOutsideButtons = RaycastTargetsOutsideButtons(root);
            string[] foreignFonts = TextsWithoutProjectFont(root);

            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, "C overlay canvas (DungeonClearRewardWindow.cs:155)");
            Assert.AreEqual(15, canvas.sortingOrder, "C sorting order 15, above HUD 10 and below menu 20 (DungeonClearRewardWindow.cs:156)");
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode, "C scaler mode (DungeonClearRewardWindow.cs:158)");
            AssertVector(new Vector2(1920f, 1080f), scaler.referenceResolution, "C reference resolution (DungeonClearRewardWindow.cs:159)");
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight, scaler.screenMatchMode, "C default match mode");
            Assert.AreEqual(0.5f, scaler.matchWidthOrHeight, 1e-4f, "C match 0.5 (DungeonClearRewardWindow.cs:160)");
            Assert.AreEqual(100f, scaler.referencePixelsPerUnit, 1e-4f, "C Canvas reference PPU 100");
            Assert.IsNotNull(window.Window.GetComponent<GraphicRaycaster>(), "C graphic raycaster (DungeonClearRewardWindow.cs:161)");
            Assert.AreEqual(0f, group.alpha, "C the window is built hidden (DungeonClearRewardWindow.cs:164)");
            Assert.IsFalse(group.interactable || group.blocksRaycasts, "C the hidden window takes no input");
            AssertVector(new Vector2(520f, 364f), window.Panel.rect.size, "C panel size (DungeonClearRewardWindow.cs:17-18)");
            AssertVector(new Vector2(0.5f, 0.5f), window.Panel.anchorMin, "C panel centred (DungeonClearRewardWindow.cs:168)");
            AssertVector(new Vector2(0.5f, 0.5f), window.Panel.anchorMax, "C panel centred (DungeonClearRewardWindow.cs:168)");
            AssertVector(new Vector2(0.5f, 0.5f), window.Panel.pivot, "C panel pivot (DungeonClearRewardWindow.cs:169)");
            Assert.IsNotNull(window.Title, "C the 「던전 클리어」 title text (DungeonClearRewardWindow.cs:175)");
            AssertRect(new Rect(170f, 292f, 180f, 54f), confirmRect, "C 「확인」 input area (DungeonClearRewardWindow.cs:187)");
            AssertRect(new Rect(24f, 146f, 472f, 40f), rowRects[0], "C reward row 1 (DungeonClearRewardWindow.cs:183-184)");
            AssertRect(new Rect(24f, 188f, 472f, 40f), rowRects[1], "C reward row 2");
            AssertRect(new Rect(24f, 230f, 472f, 40f), rowRects[2], "C reward row 3");
            Assert.AreEqual(1, confirmListeners, "C one 「확인」 listener (DungeonClearRewardWindow.cs:194)");
            Assert.IsEmpty(raycastOutsideButtons, "C only 「확인」 takes raycasts; the panel and labels pass input (DungeonClearRewardWindow.cs:172, 218)");
            Assert.IsEmpty(foreignFonts, "C texts keep the LiberationSans SDF font (DungeonClearRewardWindow.cs:166)");
        }

        [Test]
        public void PartyInvite_KeepsCanvasTextsPathsHiddenStartListenersAndInput()
        {
            InviteWindow window = BuildInvite();
            Transform root = window.Popup.transform;
            var canvas = window.Popup.GetComponent<Canvas>();
            var scaler = window.Popup.GetComponent<CanvasScaler>();
            var group = window.Popup.GetComponent<CanvasGroup>();
            float startAlpha = group.alpha;
            bool startInteractable = group.interactable, startBlocks = group.blocksRaycasts;
            string[] missingPaths = new[] { "Panel", "Panel/InviteText", "Panel/AcceptButton", "Panel/RejectButton" }
                .Where(path => root.Find(path) == null).ToArray();
            int acceptListeners = Listeners(window.Accept.onClick), rejectListeners = Listeners(window.Reject.onClick);
            bool bodyCatchesClicks = CatchesRaycastAtCentre(window.Panel);
            string[] acceptBlockers = InputBlockers(root, window.Accept), rejectBlockers = InputBlockers(root, window.Reject);
            string[] foreignFonts = TextsWithoutProjectFont(root);

            SessionTestTools.Call(window.Popup, "ShowPopup");
            window.Accept.onClick.Invoke();
            float afterAccept = group.alpha;
            SessionTestTools.Call(window.Popup, "ShowPopup");
            window.Reject.onClick.Invoke();
            float afterReject = group.alpha;

            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, "C overlay canvas (PartyInvitePopup.cs:159)");
            Assert.AreEqual(1200, canvas.sortingOrder, "C sorting order 1200, above ToastUI 1100 (PartyInvitePopup.cs:160)");
            Assert.AreEqual(CanvasScaler.ScaleMode.ConstantPixelSize, scaler.uiScaleMode, "C default scaler (PartyInvitePopup.cs:162)");
            Assert.AreEqual(1f, scaler.scaleFactor, 1e-4f, "C default scale factor");
            Assert.AreEqual(100f, scaler.referencePixelsPerUnit, 1e-4f, "C Canvas reference PPU 100");
            Assert.IsNotNull(window.Popup.GetComponent<GraphicRaycaster>(), "C graphic raycaster (PartyInvitePopup.cs:163)");
            Assert.AreEqual(0f, startAlpha, "C built hidden (PartyInvitePopup.cs:166)");
            Assert.IsFalse(startInteractable || startBlocks, "C hidden popup takes no input (PartyInvitePopup.cs:167-168)");
            Assert.IsEmpty(missingPaths, "C names PartyInvitePopupBindingTests / PartyPopupFixture find");
            Assert.AreEqual("파티 초대", window.Text != null ? window.Text.text : null, "C default invite text (PartyInvitePopup.cs:194)");
            AssertVector(new Vector2(400f, 180f), window.Panel.rect.size, "C panel size (PartyInvitePopup.cs:178)");
            AssertVector(new Vector2(120f, 40f), ((RectTransform)window.Accept.transform).rect.size, "C 수락 input size (PartyInvitePopup.cs:248)");
            AssertVector(new Vector2(120f, 40f), ((RectTransform)window.Reject.transform).rect.size, "C 거절 input size (PartyInvitePopup.cs:248)");
            Assert.AreEqual(1, acceptListeners, "C one 수락 listener (PartyInvitePopup.cs:231)");
            Assert.AreEqual(1, rejectListeners, "C one 거절 listener (PartyInvitePopup.cs:232)");
            Assert.AreEqual(0f, afterAccept, "C 수락 click still runs OnAcceptClicked and hides the popup");
            Assert.AreEqual(0f, afterReject, "C 거절 click still runs OnRejectClicked and hides the popup");
            Assert.IsTrue(bodyCatchesClicks, "C the popup body still takes the raycast over its centre (PartyInvitePopup.cs:180)");
            Assert.IsEmpty(acceptBlockers, "C nothing drawn over 수락 takes its clicks");
            Assert.IsEmpty(rejectBlockers, "C nothing drawn over 거절 takes its clicks");
            Assert.IsEmpty(foreignFonts, "C texts keep the LiberationSans SDF font (PartyInvitePopup.cs:201-209)");
        }

        [Test]
        public void PartyMemberHud_KeepsActiveRootCanvasGroupToggleMemberTextsAndUnbind()
        {
            PartyHud hud = BuildPartyHud(runLifecycle: false);
            SessionTestTools.Call(hud.Hud, "Awake");
            var group = hud.Root.GetComponent<CanvasGroup>();
            float beforeBind = group != null ? group.alpha : -1f;
            SessionTestTools.Call(hud.Hud, "OnEnable");
            SessionTestTools.Call(hud.Hud, "Start");
            bool inParty = group.alpha == 1f && group.blocksRaycasts && group.interactable;
            bool[] membersActive = hud.Members.Select(member => member.activeSelf).ToArray();
            string[] memberTexts = hud.Texts.Select(text => text.text).ToArray();
            int bound = PartyUpdatedListeners(hud.State);
            bool bodyCatchesClicks = CatchesRaycastAtCentre(hud.Root);

            hud.State.ApplyUpdate(31, 71, 71, 0, 0, 0);
            bool secondMemberHidden = !hud.Members[1].activeSelf;
            hud.State.Clear();
            bool hiddenAfterLeave = group.alpha == 0f && !group.blocksRaycasts && !group.interactable;
            bool rootStillActive = hud.Root.gameObject.activeSelf;
            SessionTestTools.Call(hud.Hud, "OnDisable");
            int afterDisable = PartyUpdatedListeners(hud.State);

            Assert.AreEqual(0f, beforeBind, "C the root CanvasGroup starts at alpha 0 before binding (PartyMemberHud.cs:39-42)");
            Assert.IsTrue(inParty, "C in a party the panel is shown and takes input (PartyMemberHud.cs:85-87)");
            CollectionAssert.AreEqual(new[] { true, true }, membersActive, "C both member groups shown (PartyMemberHud.cs:93)");
            CollectionAssert.AreEqual(new[] { "기사 (71)", "마법사 (72)" }, memberTexts, "C member text format (PartyMemberHud.cs:95, 99)");
            Assert.AreEqual(1, bound, "C one OnPartyUpdated subscription (PartyMemberHud.cs:57)");
            Assert.IsTrue(bodyCatchesClicks, "C the shown panel still takes the raycast over its centre (UI.unity Party_Info m_RaycastTarget 1)");
            Assert.IsTrue(secondMemberHidden, "C an empty member slot is deactivated (PartyMemberHud.cs:92-93)");
            Assert.IsTrue(hiddenAfterLeave, "C leaving the party hides the panel through the CanvasGroup (PartyMemberHud.cs:85-87)");
            Assert.IsTrue(rootStillActive, "C the root GameObject stays active (PartyMemberHud.cs:28-32)");
            Assert.AreEqual(0, afterDisable, "C OnDisable releases the subscription (PartyMemberHud.cs:62-66)");
        }

        [Test]
        public void NpcDialog_KeepsCanvasHintShowHideCloseKeysPortraitRulesAndBodyInset()
        {
            NpcWindow window = BuildNpcDialog(show: false);
            Transform root = window.Dialog.transform;
            var canvas = window.Dialog.GetComponent<Canvas>();
            var scaler = window.Dialog.GetComponent<CanvasScaler>();
            const string secondLine = "강화 재료를 가져오면 바로 시작하지.";
            bool hiddenAfterBuilderHide = !window.Panel.gameObject.activeSelf && !NpcDialogPanel.IsShown;

            NpcDialogPanel.Show(secondLine, window.PortraitSprite);
            bool shown = window.Panel.gameObject.activeSelf && NpcDialogPanel.IsShown;
            string body = window.Body.text;
            bool portraitShown = window.Portrait.gameObject.activeSelf && window.Portrait.sprite == window.PortraitSprite;
            InputAction close = CloseAction(window.Dialog);
            bool closeEnabled = close.enabled;
            string[] closeKeys = close.bindings.Select(binding => binding.path).OrderBy(path => path, StringComparer.Ordinal).ToArray();
            NpcDialogPanel.Show(NpcLine, null);
            string bodyAfterSecondShow = window.Body.text;
            bool bodyCatchesClicks = CatchesRaycastAtCentre(window.Panel);
            Rect bodyRect = FromTopLeft(window.Panel, window.Body.rectTransform);
            Rect portraitRect = FromTopLeft(window.Panel, window.Portrait.rectTransform);
            string[] foreignFonts = TextsWithoutProjectFont(root);
            NpcDialogPanel.Hide();
            bool hiddenAfterHide = !window.Panel.gameObject.activeSelf && !NpcDialogPanel.IsShown && !close.enabled;
            NpcDialogPanel.Show(NpcLine, null);
            bool noPortrait = !window.Portrait.gameObject.activeSelf;
            NpcDialogPanel.Hide();

            // Panel_Dialog contentInsetLRTB 32 px each side (import-spec), divided by the density for UI layout.
            const float bodyInset = 32f / Density;
            Vector2 panelSize = window.Panel.rect.size;

            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, "C overlay canvas (NpcDialogPanel.cs:128)");
            Assert.AreEqual(920, canvas.sortingOrder, "C sorting order 920 (NpcDialogPanel.cs:129)");
            Assert.AreEqual(CanvasScaler.ScaleMode.ConstantPixelSize, scaler.uiScaleMode, "C default scaler (NpcDialogPanel.cs:131)");
            Assert.AreEqual(1f, scaler.scaleFactor, 1e-4f, "C default scale factor");
            Assert.AreEqual(100f, scaler.referencePixelsPerUnit, 1e-4f, "C Canvas reference PPU 100");
            Assert.IsNotNull(window.Dialog.GetComponent<GraphicRaycaster>(), "C graphic raycaster (NpcDialogPanel.cs:132)");
            Assert.IsFalse(window.AnythingShownAtStart, "C after Awake nothing of the dialog is shown (NpcDialogPanel.cs:46, 219)");
            CollectionAssert.Contains(window.TextsAtStart, "[E] 또는 [ESC] — 닫기", "C default close hint after Awake (NpcDialogPanel.cs:31, 47)");
            Assert.IsFalse(window.PortraitActiveAtStart, "C the portrait starts hidden (NpcDialogPanel.cs:48, 172)");
            Assert.IsTrue(hiddenAfterBuilderHide, "C Hide closes the dialog (NpcDialogPanel.cs:107-108)");
            Assert.IsTrue(shown, "C Show opens the dialog (NpcDialogPanel.cs:100)");
            Assert.AreEqual(secondLine, body, "C Show writes the new line (NpcDialogPanel.cs:91)");
            Assert.IsTrue(portraitShown, "C Show shows the given portrait (NpcDialogPanel.cs:93-98)");
            Assert.IsTrue(closeEnabled, "C the close action is enabled while shown (NpcDialogPanel.cs:101)");
            CollectionAssert.AreEqual(new[] { "<Keyboard>/e", "<Keyboard>/escape" }, closeKeys, "C E and ESC close the dialog (NpcDialogPanel.cs:51-52)");
            Assert.AreEqual(secondLine, bodyAfterSecondShow, "C a second Show while open is skipped (NpcDialogPanel.cs:86)");
            Assert.IsTrue(bodyCatchesClicks, "C the shown dialog body still takes the raycast over its centre (NpcDialogPanel.cs:145)");
            Assert.IsTrue(hiddenAfterHide, "C Hide closes the dialog and disables the keys (NpcDialogPanel.cs:107-109)");
            Assert.IsTrue(noPortrait, "C Show without a portrait hides the portrait (NpcDialogPanel.cs:95-96)");
            AssertVector(new Vector2(700f, 160f), panelSize, "C panel size (NpcDialogPanel.cs:143)");
            AssertVector(new Vector2(0.5f, 0f), window.Panel.anchorMin, "C panel bottom centre (NpcDialogPanel.cs:139)");
            AssertVector(new Vector2(0.5f, 0f), window.Panel.anchorMax, "C panel bottom centre (NpcDialogPanel.cs:140)");
            AssertVector(new Vector2(0.5f, 0f), window.Panel.pivot, "C panel pivot (NpcDialogPanel.cs:141)");
            AssertVector(new Vector2(0f, 40f), window.Panel.anchoredPosition, "C panel 40 UI above the bottom (NpcDialogPanel.cs:142)");
            AssertRect(new Rect(16f, 16f, 128f, 128f), portraitRect, "C portrait field 128×128 at 16 UI (NpcDialogPanel.cs:164-168)");
            Assert.IsTrue(window.Portrait.preserveAspect, "C the portrait keeps its aspect (NpcDialogPanel.cs:171)");
            Assert.GreaterOrEqual(bodyRect.xMin, bodyInset - 0.01f, "C body left of the approved Panel_Dialog content inset");
            Assert.GreaterOrEqual(bodyRect.yMin, bodyInset - 0.01f, "C body top of the approved content inset");
            Assert.LessOrEqual(bodyRect.xMax, panelSize.x - bodyInset + 0.01f, "C body right of the approved content inset");
            Assert.LessOrEqual(bodyRect.yMax, panelSize.y - bodyInset + 0.01f, "C body bottom of the approved content inset");
            Assert.IsEmpty(foreignFonts, "C texts keep the LiberationSans SDF font (NpcDialogPanel.cs:175-182)");
        }

        // ---------------------------------------------------------------------------------------------------------
        // D. Controls: the same observers catch wrong pieces on temporary objects (no product or scene change)
        // ---------------------------------------------------------------------------------------------------------

        [Test]
        public void Control_PlainColourForeignSpriteAndRuntimeCopy_AreReportedByTheChromeObserver()
        {
            RectTransform panel = ControlPanel();
            Image approved = ControlImage(panel, "approved", Load("Panel_Default"), new Vector2(400f, 300f));
            approved.raycastTarget = false;
            List<string> clean = ChromeViolations(panel, approved, Array.Empty<Graphic>());

            Image plain = ControlImage(panel, "plain", null, new Vector2(100f, 100f));
            plain.color = new Color(0.06f, 0.07f, 0.10f, 0.88f);
            Image foreign = ControlImage(panel, "foreign", Resources.Load<Sprite>("UI/Menu_Button"), new Vector2(100f, 100f));
            Sprite source = Load("Inset_Default");
            Image copy = ControlImage(panel, "runtime copy", Own(Sprite.Create(source.texture, source.rect, new Vector2(0.5f, 0.5f),
                source.pixelsPerUnit, 0, SpriteMeshType.FullRect, source.border)), new Vector2(100f, 100f));
            Image hitArea = ControlImage(panel, "invisible hit area", null, new Vector2(100f, 100f));
            hitArea.color = new Color(1f, 1f, 1f, 0f);
            List<string> dirty = ChromeViolations(panel, approved, Array.Empty<Graphic>());
            Log("control chrome", dirty);

            Assert.IsNotNull(foreign.sprite, "fixture: UI/Menu_Button exists as the borrowed foreign sprite");
            Assert.IsEmpty(clean, "control: an approved Panel_Default used per spec passes");
            Assert.AreEqual(3, dirty.Count, "control: exactly the plain, foreign and runtime-copy images are reported");
            Assert.IsTrue(dirty.Any(v => v.Contains(plain.name)), "control: plain colour reported");
            Assert.IsTrue(dirty.Any(v => v.Contains(foreign.name)), "control: foreign sprite reported");
            Assert.IsTrue(dirty.Any(v => v.Contains(copy.name)), "control: runtime copy without the approved GUID reported");
        }

        [Test]
        public void Control_WrongImageSettings_AreReportedByTheSpecObserver()
        {
            RectTransform panel = ControlPanel();
            Image good = ControlImage(panel, "good", Load("Panel_Default"), new Vector2(256f, 192f));
            Image simpleType = ControlImage(panel, "simple type", Load("Panel_Default"), new Vector2(256f, 192f), Image.Type.Simple);
            Image tinted = ControlImage(panel, "tinted", Load("Panel_Default"), new Vector2(256f, 192f));
            tinted.color = new Color(0.2f, 0.7f, 0.2f, 1f);
            Image dense = ControlImage(panel, "multiplier 3", Load("Panel_Dialog"), new Vector2(256f, 192f));
            dense.pixelsPerUnitMultiplier = 3f;
            Image tiny = ControlImage(panel, "below legal size", Load("Panel_Default"), new Vector2(20f, 20f));
            Image stretched = ControlImage(panel, "aspect off", Load("IconBase_64"), new Vector2(64f, 32f), Image.Type.Simple);
            Image goodSimple = ControlImage(panel, "good simple", Load("IconBase_64"), new Vector2(64f, 64f), Image.Type.Simple);
            goodSimple.preserveAspect = true;
            Image thick = ControlImage(panel, "divider 12 high", Load("Divider_Horizontal"), new Vector2(200f, 12f));
            Image goodDivider = ControlImage(panel, "divider 6 high", Load("Divider_Horizontal"), new Vector2(200f, 6f));

            Assert.IsEmpty(Spec(good), "control: Panel_Default Sliced at density 2 and legal size passes");
            Assert.IsEmpty(Spec(goodSimple), "control: IconBase_64 Simple with preserveAspect passes");
            Assert.IsEmpty(Spec(goodDivider), "control: the divider at 6 UI passes");
            Assert.AreEqual(1, Spec(simpleType).Count, "control: Simple type on a Sliced piece is reported");
            Assert.AreEqual(1, Spec(tinted).Count, "control: a tinted approved PNG is reported");
            Assert.AreEqual(1, Spec(dense).Count, "control: a pixelsPerUnitMultiplier that changes the border width is reported");
            Assert.AreEqual(1, Spec(tiny).Count, "control: a size below (L+R)/2 × (T+B)/2 UI is reported");
            Assert.AreEqual(1, Spec(stretched).Count, "control: a Simple piece without preserveAspect is reported");
            Assert.AreEqual(1, Spec(thick).Count, "control: a divider that is not 6 UI high is reported");
        }

        [Test]
        public void Control_TintedOrForeignStateButtons_AreReportedByTheButtonObserver()
        {
            RectTransform panel = ControlPanel();
            Button good = ControlButton(panel, "good", "Default");
            Button tint = ControlButton(panel, "colour tint", "Default");
            tint.transition = Selectable.Transition.ColorTint;
            Button mixed = ControlButton(panel, "mixed states", "Default");
            SpriteState states = mixed.spriteState;
            states.highlightedSprite = Load("Button_Accept_Highlighted");
            mixed.spriteState = states;

            Assert.IsEmpty(ButtonViolations(good, "Default"), "control: a Default button with its four approved states passes");
            Assert.IsNotEmpty(ButtonViolations(good, "Accept"), "control: the same button is not accepted as the green Accept family");
            Assert.AreEqual(1, ButtonViolations(tint, "Default").Count, "control: ColorTint instead of state sprites is reported");
            Assert.AreEqual(1, ButtonViolations(mixed, "Default").Count, "control: a highlighted sprite of another family is reported");
        }

        [Test]
        public void Control_DecorationOverAButton_IsReportedAsTakingItsClicks()
        {
            RectTransform panel = ControlPanel();
            Button button = ControlButton(panel, "button", "Default");
            List<string> withoutOrnament = ChromeViolations(panel, null, Array.Empty<Graphic>());
            Image ornament = ControlImage(panel, "ornament", Load("Ornament_Corner"), new Vector2(48f, 48f), Image.Type.Simple);
            ornament.preserveAspect = true;
            ornament.raycastTarget = true;
            string[] blockingOnTop = InputBlockers(panel, button);
            string[] outsideButtons = RaycastTargetsOutsideButtons(panel);
            List<string> chromeOnTop = ChromeViolations(panel, null, Array.Empty<Graphic>());
            ornament.transform.SetAsFirstSibling();
            string[] blockingUnderneath = InputBlockers(panel, button);
            ornament.raycastTarget = false;
            ornament.transform.SetAsLastSibling();
            string[] blockingPassThrough = InputBlockers(panel, button);
            List<string> chromePassThrough = ChromeViolations(panel, null, Array.Empty<Graphic>());

            Assert.IsEmpty(withoutOrnament, "control: the approved button alone passes the chrome observer");
            Assert.AreEqual(new[] { "Control panel/ornament" }, blockingOnTop, "control: a raycast decoration drawn over the button takes its clicks");
            Assert.AreEqual(new[] { "Control panel/ornament" }, outsideButtons, "control: it is a raycast target outside every button");
            Assert.AreEqual(1, chromeOnTop.Count, "control: the chrome observer reports the raycast decoration");
            Assert.IsEmpty(blockingUnderneath, "control: drawn under the button it does not take the clicks");
            Assert.IsEmpty(blockingPassThrough, "control: with raycastTarget off it does not take the clicks");
            Assert.IsEmpty(chromePassThrough, "control: a non-raycast decoration passes the chrome observer");
        }

        [Test]
        public void Control_BackingAndBackgroundObservers_FollowThePieceGeometry()
        {
            RectTransform panel = ControlPanel();
            Image header = ControlImage(panel, "header", Load("Header"), new Vector2(192f, 40f));
            header.rectTransform.anchoredPosition = new Vector2(0f, 100f);
            var title = new GameObject("title", typeof(RectTransform)).GetComponent<RectTransform>();
            title.SetParent(panel, false);
            title.sizeDelta = new Vector2(100f, 30f);
            title.anchoredPosition = new Vector2(0f, -100f);
            int awayFromTitle = PiecesUnder(panel, Centre(panel, title), Kind.Header).Length;
            title.anchoredPosition = new Vector2(0f, 100f);
            int underTitle = PiecesUnder(panel, Centre(panel, title), Kind.Header).Length;
            ControlImage(panel, "partial panel", Load("Panel_Emphasis"), new Vector2(200f, 150f));
            Image partialFound = Background(panel);
            Image full = ControlImage(panel, "full panel", Load("Panel_Emphasis"), new Vector2(400f, 300f));
            full.transform.SetAsFirstSibling();
            Image fullFound = Background(panel);

            Assert.AreEqual(0, awayFromTitle, "control: a Header elsewhere does not count as the title's backing");
            Assert.AreEqual(1, underTitle, "control: the Header under the title counts");
            Assert.IsNull(partialFound, "control: a piece smaller than the panel is not its background");
            Assert.AreSame(full, fullFound, "control: the first piece covering the whole panel is its background");
        }

        // The B observer reads serialized references through AssetDatabase.GetDependencies. Positive control on an
        // existing serialized asset: UI.unity's Party_Info Image references Resources/UI/Menu_Button.png. A negative
        // control would need a temporary asset in Assets, which this suite must not write.
        [Test]
        public void Control_DependencyObserver_SeesASpriteReferencedBySerializedAsset()
        {
            string[] dependencies = AssetDatabase.GetDependencies("Assets/Scenes/99.UI/UI.unity", false);

            Assert.Contains(ResourcesUiFolder + "/Menu_Button.png", dependencies,
                "control: a serialized Image reference shows up as a dependency of its asset");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Window builders: each calls the product's own entry point and only finds what it built.
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

        InventoryWindow BuildInventory(Action<int> useSlot, Action refresh)
        {
            GameObject root = Own(new GameObject("Inventory window under test", typeof(RectTransform)));
            var view = new InventoryPanelView(root, useSlot, refresh);
            _cleanups.Add(view.Dispose);
            Button refreshButton = One(ButtonsLabelled(root.transform, "새로고침"), "the 「새로고침」 button");
            var slots = new RectTransform[8];
            var uses = new Button[8];
            for (int i = 0; i < 8; i++)
            {
                slots[i] = (RectTransform)One(Descendants(root.transform, "Slot" + (i + 1)), "Slot" + (i + 1));
                uses[i] = One(ButtonsLabelled(slots[i], "사용"), $"the 「사용」 button of Slot{i + 1}");
            }
            return new InventoryWindow
            {
                Root = root,
                View = view,
                Panel = PanelOf(root.transform, refreshButton),
                Refresh = refreshButton,
                Uses = uses,
                Slots = slots,
            };
        }

        sealed class RewardWindow
        {
            internal DungeonClearRewardWindow Window;
            internal RectTransform Panel;
            internal Button Confirm;
            internal TMP_Text Title;
            internal TMP_Text[] Rows;
        }

        RewardWindow BuildReward()
        {
            GameObject parent = Own(new GameObject("Reward window parent"));
            DungeonClearRewardWindow window = DungeonClearRewardWindow.BuildRuntime(parent.transform);
            Own(window.gameObject);
            Button confirm = One(ButtonsLabelled(window.transform, "확인"), "the 「확인」 button");
            // Reward rows start empty, so they are found by their existing names (DungeonClearRewardWindow.cs:183).
            TMP_Text[] rows = Enumerable.Range(0, 3)
                .Select(i => One(Descendants(window.transform, "Reward" + i), "Reward" + i).GetComponent<TMP_Text>()).ToArray();
            return new RewardWindow
            {
                Window = window,
                Panel = PanelOf(window.transform, confirm),
                Confirm = confirm,
                Title = window.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text.text == "던전 클리어"),
                Rows = rows,
            };
        }

        sealed class InviteWindow
        {
            internal PartyInvitePopup Popup;
            internal RectTransform Panel;
            internal Button Accept;
            internal Button Reject;
            internal TMP_Text Text;
        }

        InviteWindow BuildInvite()
        {
            GameObject parent = Own(new GameObject("Party invite parent"));
            PartyInvitePopup popup = PartyInvitePopup.BuildRuntime(parent.transform);
            Own(popup.gameObject);
            Button accept = One(ButtonsLabelled(popup.transform, "수락"), "the 수락 button");
            return new InviteWindow
            {
                Popup = popup,
                Panel = PanelOf(popup.transform, accept),
                Accept = accept,
                Reject = One(ButtonsLabelled(popup.transform, "거절"), "the 거절 button"),
                // The path PartyInvitePopupBindingTests reads the invite text from.
                Text = popup.transform.Find("Panel/InviteText")?.GetComponent<TMP_Text>(),
            };
        }

        sealed class NpcWindow
        {
            internal NpcDialogPanel Dialog;
            internal RectTransform Panel;
            internal TMP_Text Body;
            internal Image Portrait;
            internal Sprite PortraitSprite;
            // State after Awake and before the first Show, read before the elements could be told apart.
            internal bool AnythingShownAtStart;
            internal string[] TextsAtStart;
            internal bool PortraitActiveAtStart;
        }

        NpcWindow BuildNpcDialog(bool show)
        {
            GameObject parent = Own(new GameObject("NPC dialog parent"));
            NpcDialogPanel dialog = NpcDialogPanel.BuildRuntime(parent.transform);
            Own(dialog.gameObject);
            // EditMode sends no lifecycle messages; the product's own Awake publishes the singleton and the keys.
            SessionTestTools.Call(dialog, "Awake");
            _cleanups.Add(() => SessionTestTools.Call(dialog, "OnDestroy"));
            Assert.AreSame(dialog, NpcDialogPanel.Instance, "fixture: Awake published the NPC dialog");
            bool anythingShown = NpcDialogPanel.IsShown || dialog.transform.Cast<Transform>().Any(child => child.gameObject.activeSelf);
            string[] texts = dialog.GetComponentsInChildren<TMP_Text>(true).Select(text => text.text).ToArray();
            Dictionary<Image, bool> activeAtStart = dialog.GetComponentsInChildren<Image>(true)
                .ToDictionary(image => image, image => image.gameObject.activeSelf);

            Sprite portrait = ContentSprite("NPC portrait (content)");
            // The portrait Image is the one Show fills with the given sprite; find it by doing exactly that.
            NpcDialogPanel.Show(NpcLine, portrait);
            Image portraitImage = One(dialog.GetComponentsInChildren<Image>(true).Where(image => image.sprite == portrait).ToArray(),
                "the portrait Image");
            TMP_Text body = TextShowing(dialog.transform, NpcLine);
            if (!show) NpcDialogPanel.Hide();
            return new NpcWindow
            {
                Dialog = dialog,
                Panel = PanelOf(dialog.transform, body),
                Body = body,
                Portrait = portraitImage,
                PortraitSprite = portrait,
                AnythingShownAtStart = anythingShown,
                TextsAtStart = texts,
                PortraitActiveAtStart = activeAtStart.TryGetValue(portraitImage, out bool active) && active,
            };
        }

        sealed class PartyHud
        {
            internal RectTransform Root;
            internal PartyMemberHud Hud;
            internal PartyState State;
            internal GameObject[] Members;
            internal TMP_Text[] Texts;
            internal Image[] ClassIcons;
        }

        // Party_Info as serialized in Scenes/99.UI/UI.unity (read-only YAML dump in the evidence folder): root
        // Image UI/Menu_Button Sliced with raycast, two MemberStatus groups each holding MemberInfo text and a
        // Class_Icon image, and the Info_Title text. Class icons are content: runtime sprites stand in for them.
        PartyHud BuildPartyHud(bool runLifecycle)
        {
            GameObject stateObject = Own(new GameObject("Party HUD state"));
            stateObject.SetActive(false);
            var state = stateObject.AddComponent<PartyState>();
            PartyPopupFixture.SetState(state);
            _cleanups.Add(() => PartyPopupFixture.SetState(null));
            state.ApplyUpdate(31, 71, 71, 72, 0, 1);

            GameObject canvasObject = Own(new GameObject("UI scene canvas (fixture)", typeof(RectTransform)));
            canvasObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            RectTransform root = Child(canvasObject.transform, "Party_Info", new Vector2(425.9252f, 348.2833f), new Vector2(-746.89526f, 87.98561f));
            var background = root.gameObject.AddComponent<Image>();
            background.sprite = Resources.Load<Sprite>("UI/Menu_Button");
            background.type = Image.Type.Sliced;
            Assert.IsNotNull(background.sprite, "fixture: the scene's current Party_Info sprite UI/Menu_Button");

            float[] memberY = { 33.73084f, -63.70976f };
            string[] sceneTexts = { "전사 (Player)", "마법사 (Other)" };
            var members = new GameObject[2];
            var texts = new TMP_Text[2];
            var icons = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                RectTransform member = Child(root, "MemberStatus", new Vector2(100f, 100f), new Vector2(-2.3580883f, memberY[i]));
                RectTransform info = Child(member, "MemberInfo", new Vector2(255.4583f, 68.866f), new Vector2(74.92f, 0f));
                texts[i] = info.gameObject.AddComponent<TextMeshProUGUI>();
                texts[i].text = sceneTexts[i];
                RectTransform icon = Child(member, "Class_Icon", new Vector2(80.0583f, 80.058f), new Vector2(-121.34256f, 0f));
                icons[i] = icon.gameObject.AddComponent<Image>();
                icons[i].sprite = ContentSprite("class icon (content)");
                members[i] = member.gameObject;
            }
            Child(root, "Info_Title", new Vector2(200f, 50f), new Vector2(2.9314148f, 131.0621f))
                .gameObject.AddComponent<TextMeshProUGUI>().text = " - 파티 정보 -";

            var hud = root.gameObject.AddComponent<PartyMemberHud>();
            SetSerialized(hud, "_root", root.gameObject);
            SetSerialized(hud, "_member0Group", members[0]);
            SetSerialized(hud, "_member0Text", texts[0]);
            SetSerialized(hud, "_member1Group", members[1]);
            SetSerialized(hud, "_member1Text", texts[1]);
            _cleanups.Add(() => SessionTestTools.Call(hud, "OnDisable"));
            if (runLifecycle)
            {
                SessionTestTools.Call(hud, "Awake");
                SessionTestTools.Call(hud, "OnEnable");
                SessionTestTools.Call(hud, "Start");
            }
            return new PartyHud { Root = root, Hud = hud, State = state, Members = members, Texts = texts, ClassIcons = icons };
        }

        // ---------------------------------------------------------------------------------------------------------
        // Observers
        // ---------------------------------------------------------------------------------------------------------

        // The approved piece an Image shows: its sprite must be the asset behind one of the 33 GUIDs, not a copy.
        static Piece ApprovedPiece(Sprite sprite)
        {
            if (sprite == null) return null;
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long _)) return null;
            return PiecesByGuid.TryGetValue(guid, out Piece piece) && sprite.name == piece.Name ? piece : null;
        }

        static string Describe(Image image)
        {
            if (image == null) return "no Image";
            if (image.sprite == null) return $"plain colour {image.color}";
            Piece piece = ApprovedPiece(image.sprite);
            if (piece != null) return piece.Name;
            string guid = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(image.sprite, out string found, out long _) ? found : "none";
            return $"non-approved sprite '{image.sprite.name}' (GUID {guid})";
        }

        static bool IsInvisibleHitArea(Image image) => image.sprite == null && image.color.a <= 0f;

        // The first visible Image drawn over the whole panel rect: the panel's own Image or a stretched child.
        static Image Background(RectTransform panel)
        {
            foreach (Image image in panel.GetComponentsInChildren<Image>(true))
                if (image.enabled && !IsInvisibleHitArea(image) && SameRect(InSpaceOf(panel, image.rectTransform), panel.rect))
                    return image;
            return null;
        }

        // Every Image/RawImage of the window except content must be an approved piece used as README and
        // import-spec say, and decorations (not the background, not a button graphic) must not take raycasts
        // (scope 「장식은 raycastTarget=false」). The background's raycast is a C preservation item per window.
        static List<string> ChromeViolations(Transform window, Image background, ICollection<Graphic> content)
        {
            var violations = new List<string>();
            foreach (Graphic graphic in window.GetComponentsInChildren<Graphic>(true))
            {
                if (!graphic.enabled || graphic is TMP_Text || graphic is TMP_SubMeshUI || content.Contains(graphic)) continue;
                string where = PathOf(graphic.transform, window);
                if (!(graphic is Image image))
                {
                    if (graphic.color.a > 0f) violations.Add($"{where}: {graphic.GetType().Name} is not an approved piece");
                    continue;
                }
                if (IsInvisibleHitArea(image)) continue;
                Piece piece = ApprovedPiece(image.sprite);
                if (piece == null)
                {
                    violations.Add($"{where}: {Describe(image)} instead of an approved piece");
                    continue;
                }
                violations.AddRange(SpecViolations(image, piece).Select(problem => $"{where} ({piece.Name}): {problem}"));
                bool buttonGraphic = IsButtonGraphic(image);
                if (buttonGraphic && !image.raycastTarget)
                    violations.Add($"{where} ({piece.Name}): the button graphic takes no raycast, so the button takes no input");
                if (!buttonGraphic && image != background && image.raycastTarget)
                    violations.Add($"{where} ({piece.Name}): decoration is a raycast target and can take clicks");
            }
            return violations;
        }

        static List<string> Spec(Image image) => SpecViolations(image, ApprovedPiece(image.sprite));

        static List<string> SpecViolations(Image image, Piece piece)
        {
            var problems = new List<string>();
            if (image.color != Color.white) problems.Add($"colour {image.color} tints the approved PNG");
            Rect rect = image.rectTransform.rect;
            if (piece.Sliced)
            {
                if (image.type != Image.Type.Sliced) problems.Add($"Image.type {image.type}, spec Sliced");
                float density = image.pixelsPerUnit * image.pixelsPerUnitMultiplier;
                if (Mathf.Abs(density - Density) > 0.001f)
                    problems.Add($"{density} PNG px per UI unit, spec {Density}: the border is not drawn at the approved width");
                float minWidth = (piece.Left + piece.Right) / Density, minHeight = (piece.Top + piece.Bottom) / Density;
                if (rect.width < minWidth - 0.01f || rect.height < minHeight - 0.01f)
                    problems.Add($"{rect.width}×{rect.height} UI is below the legal minimum {minWidth}×{minHeight} (README)");
                if (piece.Kind == Kind.Divider && Mathf.Abs(rect.height - 6f) > 0.01f)
                    problems.Add($"divider height {rect.height} UI, spec fixed 6 UI");
            }
            else
            {
                if (image.type != Image.Type.Simple) problems.Add($"Image.type {image.type}, spec Simple");
                if (!image.preserveAspect) problems.Add("preserveAspect off, spec keeps the piece's aspect");
            }
            return problems;
        }

        static bool IsButtonGraphic(Graphic graphic)
        {
            var owner = graphic.GetComponentInParent<Selectable>(true);
            return owner != null && owner.targetGraphic == graphic;
        }

        // Scope 「버튼의 보통/마우스 올림/누름/비활성은 승인 Sprite로 표시한다」: the normal sprite on the target graphic
        // and SpriteSwap states from the same approved family; the existing interactable decisions stay where they are.
        static List<string> ButtonViolations(Button button, string family)
        {
            var problems = new List<string>();
            var graphic = button.targetGraphic as Image;
            Piece normal = graphic == null ? null : ApprovedPiece(graphic.sprite);
            if (normal == null || normal.Name != $"Button_{family}_Normal")
                problems.Add($"shows {Describe(graphic)}, approved Button_{family}_Normal");
            if (button.transition != Selectable.Transition.SpriteSwap)
                problems.Add($"transition {button.transition}; the approved hover/press/disabled states are sprites");
            SpriteState states = button.spriteState;
            ExpectState(states.highlightedSprite, $"Button_{family}_Highlighted", "highlighted", problems);
            ExpectState(states.pressedSprite, $"Button_{family}_Pressed", "pressed", problems);
            ExpectState(states.disabledSprite, $"Button_{family}_Disabled", "disabled", problems);
            return problems;
        }

        static void ExpectState(Sprite sprite, string expected, string state, List<string> problems)
        {
            Piece piece = ApprovedPiece(sprite);
            if (piece != null && piece.Name == expected) return;
            string shown = sprite == null ? "nothing" : piece != null ? piece.Name : $"non-approved '{sprite.name}'";
            problems.Add($"{state} state shows {shown}, approved {expected}");
        }

        // Approved pieces of the given kinds whose rect lies under a point of the panel (a title, a row, a portrait).
        static Piece[] PiecesUnder(RectTransform panel, Vector2 point, params Kind[] kinds) => panel.GetComponentsInChildren<Image>(true)
            .Where(image => image.enabled)
            .Select(image => (image, piece: ApprovedPiece(image.sprite)))
            .Where(entry => entry.piece != null && kinds.Contains(entry.piece.Kind) &&
                ContainsPoint(InSpaceOf(panel, entry.image.rectTransform), point))
            .Select(entry => entry.piece).ToArray();

        // Graphics taking raycasts outside every Button: they swallow clicks inside the window.
        static string[] RaycastTargetsOutsideButtons(Transform window) => window.GetComponentsInChildren<Graphic>(true)
            .Where(graphic => graphic.enabled && graphic.raycastTarget && !(graphic is TMP_SubMeshUI) &&
                graphic.GetComponentInParent<Button>(true) == null)
            .Select(graphic => PathOf(graphic.transform, window)).ToArray();

        // Raycast targets outside the button and drawn after it (later in the hierarchy of one canvas) over its centre
        // take its clicks.
        static string[] InputBlockers(Transform window, Button button)
        {
            List<Transform> drawOrder = window.GetComponentsInChildren<Transform>(true).ToList();
            int afterButton = drawOrder.IndexOf(button.transform) + button.GetComponentsInChildren<Transform>(true).Length;
            var buttonRect = (RectTransform)button.transform;
            Vector3 centre = buttonRect.TransformPoint(buttonRect.rect.center);
            return window.GetComponentsInChildren<Graphic>(true)
                .Where(graphic => graphic.enabled && graphic.raycastTarget && !(graphic is TMP_SubMeshUI) &&
                    drawOrder.IndexOf(graphic.transform) >= afterButton &&
                    graphic.rectTransform.rect.Contains((Vector2)graphic.rectTransform.InverseTransformPoint(centre)))
                .Select(graphic => PathOf(graphic.transform, window)).ToArray();
        }

        static bool CatchesRaycastAtCentre(RectTransform panel)
        {
            Vector3 centre = panel.TransformPoint(panel.rect.center);
            return panel.GetComponentsInChildren<Graphic>(true).Any(graphic => graphic.enabled && graphic.raycastTarget &&
                !(graphic is TMP_SubMeshUI) &&
                graphic.rectTransform.rect.Contains((Vector2)graphic.rectTransform.InverseTransformPoint(centre)));
        }

        static string[] TextsWithoutProjectFont(Transform window)
        {
            var font = Resources.Load<TMP_FontAsset>(FontPath);
            Assert.IsNotNull(font, "fixture: Resources font " + FontPath);
            return window.GetComponentsInChildren<TMP_Text>(true).Where(text => text.font != font)
                .Select(text => $"{PathOf(text.transform, window)} uses {(text.font == null ? "no font" : text.font.name)}").ToArray();
        }

        // Runtime listeners sit in UnityEventBase.m_Calls.m_RuntimeCalls, which has no public count.
        static int Listeners(UnityEventBase unityEvent)
        {
            object calls = typeof(UnityEventBase).GetField("m_Calls", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(unityEvent);
            Assert.IsNotNull(calls, "fixture: UnityEventBase.m_Calls");
            var runtime = calls.GetType().GetField("m_RuntimeCalls", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(calls) as IList;
            Assert.IsNotNull(runtime, "fixture: InvokableCallList.m_RuntimeCalls");
            return runtime.Count + unityEvent.GetPersistentEventCount();
        }

        static int PartyUpdatedListeners(PartyState state) =>
            ((Delegate)typeof(PartyState).GetField("OnPartyUpdated", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(state))?.GetInvocationList().Length ?? 0;

        static InputAction CloseAction(NpcDialogPanel dialog)
        {
            FieldInfo field = typeof(NpcDialogPanel).GetField("_closeAction", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "fixture: NpcDialogPanel._closeAction");
            var action = field.GetValue(dialog) as InputAction;
            Assert.IsNotNull(action, "fixture: Awake created the close action");
            return action;
        }

        static string[] ReachableFromCompositions() => CompositionAssets().Keys
            .SelectMany(path => AssetDatabase.GetDependencies(path, true)).Distinct().ToArray();

        static Dictionary<string, string[]> CompositionAssets()
        {
            var result = new Dictionary<string, string[]>();
            foreach (string path in AssetDatabase.FindAssets("t:Object", new[] { ResourcesUiFolder })
                         .Select(AssetDatabase.GUIDToAssetPath).Distinct())
            {
                if (AssetDatabase.IsValidFolder(path) || AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D)) continue;
                string[] kit = AssetDatabase.GetDependencies(path, false)
                    .Where(dependency => dependency.StartsWith(KitFolder, StringComparison.Ordinal) && dependency.EndsWith(".png", StringComparison.Ordinal))
                    .ToArray();
                if (kit.Length > 0) result.Add(path, kit);
            }
            return result;
        }

        // ---------------------------------------------------------------------------------------------------------
        // Geometry, lookup and fixture helpers
        // ---------------------------------------------------------------------------------------------------------

        // A rectangle in the panel's local UI units, whatever the anchors, pivots, mirroring or scale in between.
        static Rect InSpaceOf(RectTransform space, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 a = space.InverseTransformPoint(corners[0]);
            Vector3 c = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
        }

        static Vector2 Centre(RectTransform panel, RectTransform target) => InSpaceOf(panel, target).center;

        // Offset of the target's top-left from the panel's top-left and its size, UI units with y downward (as in
        // the approved preview records).
        static Rect FromTopLeft(RectTransform panel, RectTransform target)
        {
            Rect r = InSpaceOf(panel, target), p = panel.rect;
            return new Rect(r.xMin - p.xMin, p.yMax - r.yMax, r.width, r.height);
        }

        static bool SameRect(Rect a, Rect b) =>
            Mathf.Abs(a.xMin - b.xMin) <= Tolerance && Mathf.Abs(a.xMax - b.xMax) <= Tolerance &&
            Mathf.Abs(a.yMin - b.yMin) <= Tolerance && Mathf.Abs(a.yMax - b.yMax) <= Tolerance;

        static bool ContainsPoint(Rect r, Vector2 p) =>
            p.x >= r.xMin - Tolerance && p.x <= r.xMax + Tolerance && p.y >= r.yMin - Tolerance && p.y <= r.yMax + Tolerance;

        static void AssertRect(Rect expected, Rect actual, string message) =>
            Assert.IsTrue(SameRect(expected, actual), $"{message}: expected {expected}, actual {actual}");

        static void AssertVector(Vector2 expected, Vector2 actual, string message) =>
            Assert.IsTrue(Mathf.Abs(expected.x - actual.x) <= 1e-3f && Mathf.Abs(expected.y - actual.y) <= 1e-3f,
                $"{message}: expected {expected}, actual {actual}");

        // The window's top-level child holding the given element (the panel the builder made under its canvas).
        static RectTransform PanelOf(Transform windowRoot, Component inside)
        {
            Transform t = inside.transform;
            while (t != null && t.parent != windowRoot) t = t.parent;
            Assert.IsNotNull(t, $"fixture: {inside.name} is not inside {windowRoot.name}");
            return (RectTransform)t;
        }

        static Button[] ButtonsLabelled(Transform root, string caption) => root.GetComponentsInChildren<Button>(true)
            .Where(button => button.GetComponentsInChildren<TMP_Text>(true).Any(text => text.text == caption)).ToArray();

        static TMP_Text TextShowing(Transform root, string shown) =>
            One(root.GetComponentsInChildren<TMP_Text>(true).Where(text => text.text == shown).ToArray(), $"the text 「{shown}」");

        static Transform[] Descendants(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).Where(t => t.name == name).ToArray();

        static T One<T>(T[] found, string what)
        {
            Assert.AreEqual(1, found.Length, $"fixture: exactly one {what}");
            return found[0];
        }

        static string PathOf(Transform t, Transform root)
        {
            var names = new List<string>();
            for (Transform x = t; x != null && x != root.parent; x = x.parent) names.Add(x.name);
            names.Reverse();
            return string.Join("/", names);
        }

        static Sprite Load(string name)
        {
            Piece piece = Pieces.Single(p => p.Name == name);
            Assert.AreEqual(piece.AssetPath, AssetDatabase.GUIDToAssetPath(piece.Guid), $"fixture: {name} GUID path");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(piece.AssetPath);
            Assert.IsNotNull(sprite, $"fixture: {piece.AssetPath} loads as a Sprite");
            return sprite;
        }

        static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }

        static void SetSerialized(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, $"fixture: serialized field {target.GetType().Name}.{field} (scene contract)");
            info.SetValue(target, value);
        }

        static RectTransform Child(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        T Own<T>(T owned) where T : UnityEngine.Object
        {
            _owned.Add(owned);
            return owned;
        }

        Sprite ContentSprite(string name)
        {
            var texture = Own(new Texture2D(4, 4));
            Sprite sprite = Own(Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f)));
            sprite.name = name;
            return sprite;
        }

        RectTransform ControlPanel()
        {
            GameObject root = Own(new GameObject("Control canvas", typeof(RectTransform)));
            root.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; // reference PPU 100, as in the windows
            return Child(root.transform, "Control panel", new Vector2(400f, 300f), Vector2.zero);
        }

        static Image ControlImage(Transform parent, string name, Sprite sprite, Vector2 size, Image.Type type = Image.Type.Sliced)
        {
            var image = Child(parent, name, size, Vector2.zero).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            return image;
        }

        static Button ControlButton(Transform parent, string name, string family)
        {
            Image image = ControlImage(parent, name, Load($"Button_{family}_Normal"), new Vector2(128f, 48f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = Load($"Button_{family}_Highlighted"),
                pressedSprite = Load($"Button_{family}_Pressed"),
                disabledSprite = Load($"Button_{family}_Disabled"),
            };
            return button;
        }

        static void Log(string label, Transform window)
        {
            foreach (Image image in window.GetComponentsInChildren<Image>(true))
                TestContext.WriteLine($"[{label}] {PathOf(image.transform, window)}: {Describe(image)} type={image.type} " +
                    $"aspect={image.preserveAspect} colour={image.color} raycast={image.raycastTarget} " +
                    $"size={image.rectTransform.rect.size} active={image.gameObject.activeInHierarchy}");
        }

        static void Log(string label, IEnumerable<string> lines)
        {
            foreach (string line in lines) TestContext.WriteLine($"[{label}] {line}");
        }
    }
}
