using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Dawnholder.Client.Tests
{
    // The approved bright kit (goal 2026-10-29-inventory-character-select-look 완료조건 1·6·10) as the bright-look tests
    // expect it. The rows were printed by saved scripts from the approved specs outside the repository, never read from
    // the product folder: .backups/verification/2026-10-29-inventory-character-select-look/bright-kit/kit-spec.json
    // (sha256 d89cf84f8eee0f153dad38eaa0a4b4417188dc602b2a620ff7f0e61eacdd97f4) and bright-kit-guids-v1.json
    // (sha256 81c4029169dcf3ea21c20eca297e60ef330832769cdc92abda7b313cf8531304), script pr2-red-01/scripts/
    // emit-bright-table-v2.ps1, output receipts/emit-bright-table-02.stdout.txt, for the first 25 rows. The last 7 rows
    // are the pieces the default skin shares with the bright skin in PR3 (decision v3 A msg_79d2bf86419e, 완료조건 10):
    // bright-kit-v2/kit-spec.json (sha256 7853b3a5fd89afcf8156f18169a97a676e58281d772f9049de95e3bead4bfae2) and
    // pr3-bright-guids-v1.json (sha256 71f1c5cb6e8d6b971645d85f7fc5236b81205bb5dc293156943b24d1ef65b74e), script
    // pr3-red-01/scripts/emit-pr3-tables.mjs, output receipts/emit-pr3-tables-01.stdout.txt section A
    // (sha256 b5021ef6356f970301d7ad078d96acf502780f3a81271839450893f19ffb8384).
    internal static class BrightUiKitTestSupport
    {
        internal const string ArtFolder = "Assets/Art/UI/BrightComponents";
        internal const string ResourcesFolder = "Assets/Resources/UI/BrightComponents";
        // kit-spec units: sprite pixelsPerUnit 200, pixelDensity 2 PNG px per UI unit at Canvas reference PPU 100.
        internal const float PixelsPerUnit = 200f;
        internal const float Density = 2f;

        internal sealed class BrightPiece
        {
            internal readonly string Name;
            internal readonly string Guid;
            internal readonly string Kind;
            internal readonly bool Sliced;
            internal readonly int Width, Height;
            internal readonly int Left, Right, Top, Bottom;

            internal BrightPiece(string name, string guid, string kind, bool sliced, int width, int height,
                int left, int right, int top, int bottom)
            {
                Name = name;
                Guid = guid;
                Kind = kind;
                Sliced = sliced;
                Width = width;
                Height = height;
                Left = left;
                Right = right;
                Top = top;
                Bottom = bottom;
            }

            internal string AssetPath => ArtFolder + "/" + Name + ".png";

            // kit-spec units.borderOrder 「L R T B」 and borderUnit 「PNG pixels; Unity Vector4 is (L,B,R,T), without
            // division by density」.
            internal Vector4 UnityBorder => new Vector4(Left, Bottom, Right, Top);
        }

        // name, guid, kind, sliced, PNG width, PNG height, border L R T B in PNG px.
        internal static readonly BrightPiece[] Pieces =
        {
            new BrightPiece("Panel_Default", "387817131a7ee7abd35357ff32e6cd2a", "Panel", true, 512, 384, 24, 24, 24, 24),
            new BrightPiece("Header", "7e18e6245ab44c8b34442c1876be07d3", "Header", true, 384, 80, 16, 16, 16, 16),
            new BrightPiece("Inset_Default", "d86a90ca34d7ac84244096c673e46475", "Inset", true, 192, 128, 12, 12, 12, 12),
            new BrightPiece("Divider_Horizontal", "397c7e470f14e2989cc3622e25f75e24", "Divider", true, 256, 12, 16, 16, 0, 0),
            new BrightPiece("Button_Default_Normal", "218e99a5632015261381128b2909788b", "Button", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("Button_Default_Highlighted", "8c51a6fac51dd8f61ef4cede5690f59c", "Button", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("Button_Default_Pressed", "09e130da54a609f68834547b2363b2a6", "Button", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("Button_Default_Disabled", "0438a51907e9b5791382a7beecdf53ad", "Button", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("Button_Accept_Normal", "fdaa805e70f5533488dae81f7234282a", "Button", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("Button_Accept_Highlighted", "b19f5a6d0300e566d0fffea8534ac8ca", "Button", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("Button_Accept_Pressed", "819be5b406aaa976f19a17fb72ef6fae", "Button", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("Button_Accept_Disabled", "d14f5c1a8a0197176bd076a96fb0009e", "Button", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("ItemSlot_Empty", "e702897eaffb5b94a75480e1d24d43b4", "ItemSlot", true, 128, 128, 16, 16, 16, 16),
            new BrightPiece("ItemSlot_Filled", "74be0b4adcf9029de03d21c58f39d95d", "ItemSlot", true, 128, 128, 16, 16, 16, 16),
            new BrightPiece("ItemSlot_Selected", "d44be2e942c90002e105154dfa0f758a", "ItemSlot", true, 128, 128, 16, 16, 16, 16),
            new BrightPiece("IconBase_64", "87b8e15f8cfe6c51f3820283d7a2f67a", "IconBase", false, 128, 128, 0, 0, 0, 0),
            new BrightPiece("NumberBadge", "5efe8348bfdcd2327dfc5a5568c159d4", "NumberBadge", false, 96, 96, 0, 0, 0, 0),
            new BrightPiece("CharacterSlot_Default", "b0d2304d8eeaeac834d357ce350b062c", "CharacterSlot", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("CharacterSlot_Selected", "39f038f1abf5257969dd43c5458e0634", "CharacterSlot", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("Pedestal", "54faccc43070f3ab735dae9c756f651b", "Pedestal", false, 320, 160, 0, 0, 0, 0),
            new BrightPiece("InputField", "38f801c703515178c835f249a4db2983", "InputField", true, 256, 80, 16, 16, 16, 16),
            new BrightPiece("SelectionGlow", "ae12c245de4289f6e0ba6cabc07b8bf7", "SelectionGlow", false, 256, 384, 0, 0, 0, 0),
            new BrightPiece("Icon_Material", "9f5082fa3c5d6e75b68675a2d3f1a491", "Icon", false, 128, 128, 0, 0, 0, 0),
            new BrightPiece("Icon_CurrencyPouch", "3aadc85f7b3e7ca0c8a31d52cc7910a8", "Icon", false, 128, 128, 0, 0, 0, 0),
            new BrightPiece("Icon_Coin", "87e028dcd3360e891febb86e8d4c61d8", "Icon", false, 128, 128, 0, 0, 0, 0),
            // PR3 shared-skin pieces (v3 A msg_79d2bf86419e, 완료조건 10): kit v2 rows, emit-pr3-tables-01 section A.
            new BrightPiece("Panel_Dialog", "5685ac277531981b5554e3a74e56debe", "Panel", true, 512, 384, 24, 24, 24, 24),
            new BrightPiece("Panel_Emphasis", "3e26a93b5ba3982f399c126a95bf553d", "Panel", true, 512, 384, 24, 24, 24, 24),
            new BrightPiece("Button_Decline_Normal", "56edcbfaf68f16d0dff50a7bb0e3a466", "Button", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("Button_Decline_Highlighted", "11e73ce64b7278c04e4b6a6eff7a7717", "Button", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("Button_Decline_Pressed", "470f3c0476ddfd928b5c9a879903f117", "Button", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("Button_Decline_Disabled", "7d2ff4c8822f62b46b0e6d8fcf178640", "Button", true, 256, 96, 16, 16, 16, 16),
            new BrightPiece("PortraitFrame_128", "371e59829a40e0c260a9c8cdff7c4a23", "PortraitFrame", false, 256, 256, 0, 0, 0, 0),
        };

        // Declared after Pieces: static initializers run in text order.
        static readonly Dictionary<string, BrightPiece> s_byGuid = Pieces.ToDictionary(piece => piece.Guid);

        internal static BrightPiece Named(string name) => Pieces.Single(piece => piece.Name == name);

        // The GUID of the asset file a sprite was imported from; null for runtime sprites without one.
        internal static string SourceGuid(Sprite sprite) =>
            sprite != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long _) ? guid : null;

        // The bright piece a sprite is: the asset behind one of the 32 GUIDs under its own name, not a copy.
        internal static BrightPiece PieceOf(Sprite sprite)
        {
            string guid = SourceGuid(sprite);
            return guid != null && s_byGuid.TryGetValue(guid, out BrightPiece piece) && sprite.name == piece.Name ? piece : null;
        }

        internal static bool IsPiece(Sprite sprite, string name) => PieceOf(sprite)?.Name == name;
    }
}
