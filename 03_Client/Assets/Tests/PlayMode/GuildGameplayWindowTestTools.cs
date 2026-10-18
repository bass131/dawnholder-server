using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Dawnholder.Client.Gameplay;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Test-only observations of the live bootstrap. The existing finite TCP fixture owns
    // transport/input cleanup; this file owns capture data and the temporary GameView item.
    internal static class GuildGameplayWindowTestTools
    {
        // PlayMode has no product IVT. Observe the published CanvasGroup as the existing
        // inventory/reward screen probes do; never substitute hidden state for a shown UI.
        internal static bool InventoryShown() => InventoryPanel.Instance != null &&
            ScreenUiProbe.IsShown(InventoryPanel.Instance) && InventoryPanelProbe.Group.alpha > 0f;

        internal static bool RewardShown()
        {
            DungeonClearRewardWindow window = DungeonClearRewardWindow.Instance;
            CanvasGroup group = window == null ? null : window.GetComponent<CanvasGroup>();
            return group != null && group.alpha > 0f && ScreenUiProbe.IsShown(window);
        }

        internal static IEnumerator KeyFrame(MapEntryPlayFixture fixture, Key key)
        {
            fixture.Keys(key);
            yield return fixture.Frames(2);
            fixture.Keys();
            yield return fixture.Frames(4);
        }

        internal static Component Entrance(string name)
        {
            Scene scene = SceneManager.GetActiveScene();
            Component[] found = Resources.FindObjectsOfTypeAll<Component>().Where(component => component != null &&
                component.gameObject.scene == scene && component.name == name &&
                component.GetType().FullName == "UnityEngine.UI.Button").ToArray();
            Assert.AreEqual(1, found.Length, "bootstrap entrance " + name);
            return found[0];
        }

        internal static NpcInteractable SmithNpc() => UnityEngine.Object.FindObjectsByType<NpcInteractable>()
            .Single(npc => npc.gameObject.scene == SceneManager.GetActiveScene() && npc.name == "Npc_BlackSmith");

        internal static IEnumerator PlaceLocal(MapEntryPlayFixture fixture, bool near, Recorder record)
        {
            Collider2D trigger = SmithNpc().GetComponent<Collider2D>();
            Collider2D body = fixture.Player.GetComponentsInChildren<Collider2D>().First(collider => collider.enabled);
            Vector3 oldPosition = fixture.Player.transform.position;
            Vector3 bodyOffset = body.bounds.center - oldPosition;
            Vector3 position = trigger.bounds.center - bodyOffset;
            if (!near) position.x -= trigger.bounds.extents.x + body.bounds.extents.x + 8f;
            fixture.Player.SetServerPosition(position);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return fixture.Frames(5);
            bool overlapped = fixture.Player.GetComponentsInChildren<Collider2D>().Any(collider =>
                collider.enabled && trigger.Distance(collider).isOverlapped);
            record.Event("fixture-runtime-reposition", "near=" + near + "; old=" + oldPosition + "; requested=" + position +
                "; actual=" + fixture.Player.transform.position + "; trigger=" + trigger.bounds + "; overlaps=" + overlapped);
            record.Check(overlapped == near, "actual local collider overlap must match fixture position");
        }

        // The upper input is a legal v20 boundary, not a computed product expectation:
        // 16 UTF-16 Korean code units, treasury 1e9, member/online Int32.MaxValue.
        internal static byte[] UpperSnapshot(long sequence)
        {
            const string name = "가나다라마바사아자차카타파하길드";
            Assert.AreEqual(16, name.Length);
            byte[] normal = GuildUiConnectionTestTools.Snapshot(sequence: sequence, guildRevision: 8,
                inventoryRevision: 4, treasury: 1000000000, unlocked: true, enhanced: true);
            byte[] nameBytes = Encoding.Unicode.GetBytes(name);
            byte[] wire = new byte[66 + nameBytes.Length];
            Buffer.BlockCopy(normal, 0, wire, 0, 66);
            BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(0), (ushort)wire.Length);
            BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(28), int.MaxValue);
            BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(32), int.MaxValue);
            BinaryPrimitives.WriteUInt16LittleEndian(wire.AsSpan(64), (ushort)nameBytes.Length);
            Buffer.BlockCopy(nameBytes, 0, wire, 66, nameBytes.Length);
            return wire;
        }

        internal sealed class Recorder : IDisposable
        {
            readonly string _directory;
            readonly List<string> _failures = new List<string>();
            readonly List<string> _events = new List<string>();
            readonly List<string> _missing = new List<string>();
            readonly string _case;
            readonly int _width;
            readonly int _height;

            internal Recorder(string name, int width, int height)
            {
                _case = name;
                _width = width;
                _height = height;
                string root = Environment.GetEnvironmentVariable("DAWNHOLDER_GUILD_VERIFY_CAPTURE_DIR");
                Assert.IsFalse(string.IsNullOrWhiteSpace(root), "runner supplies the owned capture directory");
                _directory = Path.Combine(root, name);
                Directory.CreateDirectory(_directory);
                TMP_Text.OnMissingCharacter += Missing;
                Event("start", "finite loopback; virtual input; actual bootstrap; no production server");
            }

            internal void Check(bool success, string message)
            {
                Event("check", success + ": " + message);
                if (!success) _failures.Add(message);
            }

            internal void Event(string input, string observed)
            {
                string line = DateTime.UtcNow.ToString("O") + " frame=" + Time.frameCount + " [" + input + "] " + observed;
                _events.Add(line);
                Debug.Log("[guild gameplay] " + _case + " " + line);
            }

            internal IEnumerator Click(MapEntryPlayFixture fixture, Component component, string reason, bool expectHit = true)
            {
                Vector2 point = ScreenUiProbe.ScreenPoint(component);
                yield return ClickAt(fixture, point, reason, component, expectHit);
            }

            internal IEnumerator ClickAt(MapEntryPlayFixture fixture, Vector2 point, string reason,
                Component target = null, bool expectHit = true)
            {
                int attacks = fixture.Peer.Count(PacketID.C_Attack);
                int stronghold = fixture.Peer.Count(PacketID.C_StrongholdUpgrade);
                int enhance = fixture.Peer.Count(PacketID.C_WeaponEnhance);
                GameObject[] hits = ScreenUiProbe.RaycastHits(point);
                Event("pointer-before " + reason, "point=" + point + "; attacks=" + attacks + ";45=" + stronghold +
                    ";46=" + enhance + "; hits=" + string.Join("|", hits.Select(hit => hit.name)));
                if (expectHit && target != null)
                    Check(hits.Length > 0 && hits[0].transform.IsChildOf(target.transform), reason + " top pointer hit belongs to target");
                yield return InventoryPanelProbe.ClickAt(fixture.Mouse, point);
                yield return fixture.Frames(4);
                int after = fixture.Peer.Count(PacketID.C_Attack);
                Event("pointer-after " + reason, "attacks=" + after + ";45=" + fixture.Peer.Count(PacketID.C_StrongholdUpgrade) +
                    ";46=" + fixture.Peer.Count(PacketID.C_WeaponEnhance));
                Check(attacks == after, reason + " UI pointer emits no C_Attack");
            }

            internal IEnumerator Capture(MapEntryPlayFixture fixture, string name)
            {
                yield return fixture.Frames(5);
                Canvas.ForceUpdateCanvases();
                yield return new WaitForEndOfFrame();
                Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
                Assert.IsNotNull(shot, "actual end-of-frame ScreenCapture");
                try
                {
                    var capture = new CaptureData
                    {
                        utc = DateTime.UtcNow.ToString("O"), frame = Time.frameCount,
                        scene = SceneManager.GetActiveScene().name, screenWidth = Screen.width, screenHeight = Screen.height,
                        requestedWidth = _width, requestedHeight = _height,
                        pngWidth = shot.width, pngHeight = shot.height,
                        camera = Camera.main == null ? null : Camera.main.name,
                        hud = HudController.Instance == null ? null : HudController.Instance.name,
                        canSend = fixture.Session != null && fixture.Session.CanSendGameplay,
                        inventoryOpen = InventoryShown(),
                        npcOpen = NpcDialogPanel.IsShown,
                        rewardShown = RewardShown(),
                        attacks = fixture.Peer.Count(PacketID.C_Attack), stronghold = fixture.Peer.Count(PacketID.C_StrongholdUpgrade),
                        enhance = fixture.Peer.Count(PacketID.C_WeaponEnhance), missingGlyphEvents = _missing.ToArray()
                    };
#if UNITY_EDITOR
                    UnityEditor.PlayModeWindow.GetRenderingResolution(out uint width, out uint height);
                    capture.renderingWidth = width;
                    capture.renderingHeight = height;
                    capture.mainView = GameViewScope.MainDescription();
#endif
                    capture.texts = UnityEngine.Object.FindObjectsByType<TMP_Text>().Where(ScreenUiProbe.IsShown)
                        .Select(ReadText).ToArray();
                    capture.art = Resources.FindObjectsOfTypeAll<Component>().Where(component => component != null &&
                        component.gameObject.scene == SceneManager.GetActiveScene() && ScreenUiProbe.IsShown(component) &&
                        component.GetType().FullName == "UnityEngine.UI.Image")
                        .Select(component => new ArtData { path = Hierarchy(component.transform),
                            sprite = (component.GetType().GetProperty("sprite").GetValue(component) as Sprite)?.name,
                            rect = ScreenUiProbe.ScreenRect((RectTransform)component.transform) }).ToArray();
                    Write(name + ".png", shot.EncodeToPNG());
                    Write(name + ".json", Encoding.UTF8.GetBytes(JsonUtility.ToJson(capture, true)));
                    Check(capture.pngWidth == capture.screenWidth && capture.pngHeight == capture.screenHeight,
                        name + " original PNG dimensions equal Screen");
                    Check(capture.screenWidth == _width && capture.screenHeight == _height,
                        name + " actual Screen equals requested contracted resolution " + _width + "x" + _height);
                    Check(capture.renderingWidth == capture.screenWidth && capture.renderingHeight == capture.screenHeight,
                        name + " main rendering resolution equals Screen");
                    Check(capture.camera != null && capture.hud != null, name + " actual scene camera and HUD exist");
                    foreach (TextData row in capture.texts.Where(row => row.path.Contains("GuildWindow/") || row.path.Contains("BlacksmithWindow/")))
                    {
                        Check(!row.truncated, name + " not truncated: " + row.path);
                        GlyphData[] outside = row.glyphs.Where(glyph => glyph.visible && (glyph.rect.xMin < -1f ||
                            glyph.rect.yMin < -1f || glyph.rect.xMax > Screen.width + 1f || glyph.rect.yMax > Screen.height + 1f)).ToArray();
                        Check(outside.Length == 0, name + " on-screen glyphs " + row.path + "; outside=" +
                            string.Join(",", outside.Select(glyph => "U+" + glyph.codepoint.ToString("X4"))));
                    }
                    Event("capture", name + ": " + shot.width + "x" + shot.height + " scene=" + capture.scene +
                        " texts=" + capture.texts.Length + "; art=" + capture.art.Length + "; original unresized ScreenCapture PNG");
                }
                finally { UnityEngine.Object.Destroy(shot); }
            }

            internal void Finish()
            {
                Save();
                Assert.IsEmpty(_failures, "live screen/gameplay requirements: " + string.Join("; ", _failures));
                Debug.Log("[guild gameplay completed] " + _case);
            }

            public void Dispose()
            {
                TMP_Text.OnMissingCharacter -= Missing;
                Save();
            }

            void Save() => File.WriteAllText(Path.Combine(_directory, "observations.json"), JsonUtility.ToJson(new Summary
            {
                caseName = _case, utc = DateTime.UtcNow.ToString("O"), events = _events.ToArray(),
                failures = _failures.ToArray(), missingGlyphEvents = _missing.ToArray()
            }, true), new UTF8Encoding(false));

            void Missing(int unicode, int index, string text, TMP_FontAsset font, TMP_Text component)
            {
                string message = "U+" + unicode.ToString("X4") + " at " + index + "; font=" + font?.name +
                    "; path=" + (component == null ? "null" : Hierarchy(component.transform)) + "; text=" + text;
                _missing.Add(message);
                if (component != null && (component.GetComponentInParent<GuildWindow>() != null ||
                    component.GetComponentInParent<BlacksmithWindow>() != null)) _failures.Add("missing window glyph: " + message);
            }

            void Write(string name, byte[] bytes)
            {
                string path = Path.Combine(_directory, name);
                Assert.IsFalse(File.Exists(path), "each original capture is new: " + path);
                File.WriteAllBytes(path, bytes);
            }

            static TextData ReadText(TMP_Text text)
            {
                text.ForceMeshUpdate();
                var glyphs = new List<GlyphData>();
                for (int i = 0; i < text.textInfo.characterCount; i++)
                {
                    TMP_CharacterInfo glyph = text.textInfo.characterInfo[i];
                    Vector3 min = ScreenPoint(text, glyph.bottomLeft);
                    Vector3 max = ScreenPoint(text, glyph.topRight);
                    glyphs.Add(new GlyphData { codepoint = glyph.character, visible = glyph.isVisible,
                        font = glyph.fontAsset == null ? null : glyph.fontAsset.name,
                        rect = Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y),
                            Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y)) });
                }
                return new TextData { path = Hierarchy(text.transform), text = text.text,
                    font = text.font == null ? null : text.font.name, fontSize = text.fontSize,
                    overflow = text.isTextOverflowing, truncated = text.isTextTruncated,
                    rect = ScreenUiProbe.ScreenRect(text.rectTransform), glyphs = glyphs.ToArray() };
            }

            static Vector3 ScreenPoint(TMP_Text text, Vector3 local)
            {
                Vector3 world = text.transform.TransformPoint(local);
                Canvas canvas = text.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) return world;
                return Camera.main != null ? Camera.main.WorldToScreenPoint(world) : world;
            }

            static string Hierarchy(Transform item)
            {
                var names = new List<string>();
                for (; item != null; item = item.parent) names.Insert(0, item.name);
                return string.Join("/", names);
            }

            [Serializable] sealed class Summary { public string caseName, utc; public string[] events, failures, missingGlyphEvents; }
            [Serializable] sealed class GlyphData { public int codepoint; public bool visible; public string font; public Rect rect; }
            [Serializable] sealed class TextData
            {
                public string path, text, font;
                public float fontSize;
                public bool overflow, truncated;
                public Rect rect;
                public GlyphData[] glyphs;
            }
            [Serializable] sealed class ArtData { public string path, sprite; public Rect rect; }
            [Serializable] sealed class CaptureData
            {
                public string utc, scene, camera, hud, mainView;
                public int frame, screenWidth, screenHeight, requestedWidth, requestedHeight, pngWidth, pngHeight, attacks, stronghold, enhance;
                public uint renderingWidth, renderingHeight;
                public bool canSend, inventoryOpen, npcOpen, rewardShown;
                public string[] missingGlyphEvents;
                public TextData[] texts;
                public ArtData[] art;
            }
        }

#if UNITY_EDITOR
        // Same editor calls as the existing independent scene fixture, with this task's
        // narrower two-size scope. No SaveToHDD, SetCustomRenderingResolution or window resizing.
        internal sealed class GameViewScope : IDisposable
        {
            const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            readonly UnityEditor.EditorWindow _view;
            readonly object _group;
            readonly object[] _before;
            readonly PropertyInfo _index;
            readonly MethodInfo _select;
            readonly int _original;
            readonly Recorder _record;
            readonly bool _created;
            object _added;

            internal GameViewScope(Recorder record)
            {
                _record = record;
                Assert.AreEqual("6000.6.4f1", Application.unityVersion);
                Assembly editor = typeof(UnityEditor.EditorWindow).Assembly;
                Type gameView = editor.GetType("UnityEditor.GameView", true);
                _view = MainView();
                _created = _view == null;
                if (_created)
                {
                    MethodInfo create = typeof(UnityEditor.EditorWindow).GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .Single(method => method.Name == "CreateWindow" && method.IsGenericMethodDefinition && method.GetParameters().Length == 1);
                    _view = (UnityEditor.EditorWindow)create.MakeGenericMethod(gameView).Invoke(null, new object[] { Type.EmptyTypes });
                }
                Assert.IsInstanceOf(gameView, _view);
                _index = gameView.GetProperty("selectedSizeIndex", Members);
                _select = gameView.GetMethod("SizeSelectionCallback", Members, null, new[] { typeof(int), typeof(object) }, null);
                Assert.IsNotNull(_index);
                Assert.IsNotNull(_select);
                _original = (int)_index.GetValue(_view);
                Type sizesType = editor.GetType("UnityEditor.GameViewSizes", true);
                object sizes = sizesType.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                Assert.AreEqual("Standalone", sizesType.GetProperty("currentGroupType", Members).GetValue(sizes).ToString());
                _group = sizesType.GetProperty("currentGroup", Members).GetValue(sizes);
                _before = Entries();
                Assert.IsTrue(_before.Any(size => Match(size, 1920, 1080) &&
                    (string)size.GetType().GetProperty("baseText", Members).GetValue(size) == "Full HD"), "existing Full HD is required");
                _record.Event("gameview-before", "index=" + _original + "; created=" + _created + "; list=" + DescribeEntries());
            }

            internal IEnumerator Select(int width, int height)
            {
                Assert.IsTrue((width == 709 && height == 399) || (width == 802 && height == 451) ||
                    (width == 1920 && height == 1080), "only the three contracted resolutions");
                object[] entries = Entries();
                int index = Array.FindIndex(entries, size => Match(size, width, height));
                if (index < 0)
                {
                    Assert.AreNotEqual(1920, width, "never add Full HD");
                    Assert.IsNull(_added, "one owned item per test");
                    Assembly editor = typeof(UnityEditor.EditorWindow).Assembly;
                    Type kind = editor.GetType("UnityEditor.GameViewSizeType", true);
                    Type size = editor.GetType("UnityEditor.GameViewSize", true);
                    _added = Activator.CreateInstance(size, Enum.Parse(kind, "FixedResolution"), width, height, "Temporary " + Guid.NewGuid().ToString("N"));
                    _group.GetType().GetMethod("AddCustomSize", Members).Invoke(_group, new[] { _added });
                    index = Entries().Length - 1;
                    Assert.AreSame(_added, Entries()[index]);
                }
                _select.Invoke(_view, new object[] { index, null });
                _view.Repaint();
                yield return MapEntryPlayFixture.Wait(() => Screen.width == width && Screen.height == height,
                    "actual main GameView " + width + "x" + height, 10);
                UnityEditor.PlayModeWindow.GetRenderingResolution(out uint renderingWidth, out uint renderingHeight);
                Assert.AreEqual(width, renderingWidth);
                Assert.AreEqual(height, renderingHeight);
                _record.Event("gameview-selected", MainDescription() + "; index=" + _index.GetValue(_view) + "; list=" + DescribeEntries());
            }

            public void Dispose()
            {
                object[] current = Entries();
                Assert.AreEqual(_before.Length + (_added == null ? 0 : 1), current.Length, "only our temporary size changed");
                for (int i = 0; i < _before.Length; i++) Assert.AreSame(_before[i], current[i], "preserved size object " + i);
                _select.Invoke(_view, new object[] { _original, null });
                Assert.AreEqual(_original, _index.GetValue(_view), "original size selection restored");
                if (_added != null)
                {
                    Assert.AreSame(_added, current[current.Length - 1]);
                    _group.GetType().GetMethod("RemoveCustomSize", Members).Invoke(_group, new object[] { current.Length - 1 });
                }
                CollectionAssert.AreEqual(_before, Entries(), "own temporary size removed, original list retained");
                _record.Event("gameview-returned", "original index=" + _original + "; list=" + DescribeEntries());
                if (_created) _view.Close();
            }

            internal static string MainDescription()
            {
                UnityEditor.EditorWindow view = MainView();
                return view == null ? "missing" : view.GetType().FullName + "; target=" +
                    typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.PlayModeView", true)
                        .GetProperty("targetSize", Members).GetValue(view) + "; window=" + view.position;
            }

            static UnityEditor.EditorWindow MainView() => (UnityEditor.EditorWindow)typeof(UnityEditor.EditorWindow).Assembly
                .GetType("UnityEditor.PlayModeView", true).GetMethod("GetMainPlayModeView", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, null);

            object[] Entries()
            {
                int count = (int)_group.GetType().GetMethod("GetTotalCount", Members).Invoke(_group, null);
                MethodInfo get = _group.GetType().GetMethod("GetGameViewSize", Members);
                return Enumerable.Range(0, count).Select(index => get.Invoke(_group, new object[] { index })).ToArray();
            }

            static bool Match(object size, int width, int height) =>
                size.GetType().GetProperty("sizeType", Members).GetValue(size).ToString() == "FixedResolution" &&
                (int)size.GetType().GetProperty("width", Members).GetValue(size) == width &&
                (int)size.GetType().GetProperty("height", Members).GetValue(size) == height;

            string DescribeEntries() => string.Join("|", Entries().Select(size =>
                size.GetType().GetProperty("sizeType", Members).GetValue(size) + ":" +
                size.GetType().GetProperty("width", Members).GetValue(size) + "x" +
                size.GetType().GetProperty("height", Members).GetValue(size) + ":" +
                size.GetType().GetProperty("baseText", Members).GetValue(size)));
        }
#endif
    }
}
