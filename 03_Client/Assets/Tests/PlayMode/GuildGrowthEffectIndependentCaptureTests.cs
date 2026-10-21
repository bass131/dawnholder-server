using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Independent real-screen capture of the guild growth effect over the real Town and HUD at 1920x1080 and 1280x720
    // (growth-effect-pr independent-preparation-01, verifying implementation Task task_826d0b47be93; goal 「검증 계획」 실화면).
    // Town is entered the way UiWindowLiveCaptureTests does: the scripted loopback peer of MapEntryPlayFixture, not GameServer
    // or a database. The effect is built under a plain object of the Town scene, because the Content wiring that will own it
    // is a later PR. For each size it writes, before any verdict: an idle frame-time and memory baseline, a frame-time and
    // memory trace of each kind and of both, the pointer raycast through the effect, and for each kind and both a short
    // recording (JPEG sequence with its timing CSV) and the before, start, emphasis, afterglow and end frames as PNG. Start,
    // afterglow and end are the first drawn, last drawn and first clear frames; emphasis is the frame showing the most
    // light (drawn area times alpha). All are chosen by what was drawn, not by the product's stage timing. Frame and memory
    // cost have no approved limit, so they are raw only. Runs only when the capture runner names its output folder and
    // writes the GameView permit, and needs the Editor's Game view (no -batchmode).
    public sealed class GuildGrowthEffectIndependentCaptureTests
    {
        const string OutputVariable = "DAWNHOLDER_UI_CAPTURE_DIR";
        const string PermitVariable = "DAWNHOLDER_GAMEVIEW_PERMIT";
        // The runner writes the permit after it has identified the Unity process it started.
        const float PermitWaitSeconds = 120f;
        const float SizeWaitSeconds = 3f;
        const int SettleFrames = 10;
        const int BaselineFrames = 120;
        // A short recording: at most this many frames per scenario, JPEG to keep the folder small.
        const int RecordingCap = 40;
        const int JpegQuality = 85;
        const float AfterClear = 0.5f;
        const float WatchCap = 4.5f;
        // The pointer check reads the raycast this far into a two-kind effect, when both are drawn.
        const float PointerAt = 0.5f;
        const int MaxPointerPoints = 24;

        static readonly (int Width, int Height)[] Sizes = { (1920, 1080), (1280, 720) };
        static readonly (string Name, (GuildChangeKind Kind, int Level)[] Requests)[] Scenarios =
        {
            ("shared", new[] { (GuildChangeKind.StrongholdUpgrade, 4) }),
            ("personal", new[] { (GuildChangeKind.WeaponEnhance, 7) }),
            ("both", new[] { (GuildChangeKind.StrongholdUpgrade, 5), (GuildChangeKind.WeaponEnhance, 8) }),
        };

        readonly List<string> _record = new List<string>();
        readonly List<string> _failures = new List<string>();
        readonly List<string> _missingCharacters = new List<string>();
        MapEntryPlayFixture _fixture;
        string _output;
        GameObject _holder;
#if UNITY_EDITOR
        GameViewTargetSize _gameView;
#endif

        [SetUp]
        public void Setup()
        {
            _fixture = new MapEntryPlayFixture();
            _record.Clear();
            _failures.Clear();
            _missingCharacters.Clear();
            _output = null;
            _holder = null;
#if UNITY_EDITOR
            _gameView = null;
#endif
            TMP_Text.OnMissingCharacter += RecordMissingCharacter;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Exception restoreError = null;
#if UNITY_EDITOR
            try { if (_gameView != null) Note("game view restore", _gameView.Restore()); }
            catch (Exception error) { restoreError = error; }
#endif
            TMP_Text.OnMissingCharacter -= RecordMissingCharacter;
            if (_holder != null) UnityEngine.Object.Destroy(_holder);
            yield return _fixture.Cleanup();
            if (!string.IsNullOrEmpty(_output) && Directory.Exists(_output)) WriteNew("capture-record.txt", string.Join("\n", _record) + "\n");
            if (restoreError != null) throw new InvalidOperationException("Game view restore failed", restoreError);
        }

        [UnityTest]
        public IEnumerator BothKinds_OverTheRealTownHud_AtBothSizes_RecordKeyFramesAndCost()
        {
            _output = Environment.GetEnvironmentVariable(OutputVariable);
            if (string.IsNullOrEmpty(_output)) Assert.Ignore($"live capture runs only when the capture runner sets {OutputVariable}");
            Assert.IsTrue(Directory.Exists(_output), "capture output folder exists: " + _output);
            Note("utc", DateTime.UtcNow.ToString("o"));
            Note("isPlaying / isBatchMode / unityVersion", $"{Application.isPlaying} / {Application.isBatchMode} / {Application.unityVersion}");
            Note("graphics device", $"{SystemInfo.graphicsDeviceType} / {SystemInfo.graphicsDeviceName}");

            yield return ReadySyncedTown();
            _holder = new GameObject("GuildGrowthEffectIndependentHolder");
            Note("holder scene", _holder.scene.name);
            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_holder.transform);
            yield return OpenGameView();

            foreach (var (width, height) in Sizes)
            {
                string size = width + "x" + height;
#if UNITY_EDITOR
                Note(size + " select", _gameView.TrySelect(width, height));
#endif
                bool reached = false;
                yield return ScreenUiProbe.WaitUntil(() => Screen.width == width && Screen.height == height, SizeWaitSeconds,
                    done => reached = done);
                Note(size + " Screen after selection", $"{Screen.width}x{Screen.height} reached {reached}");
                if (!reached)
                {
                    _failures.Add($"{size}: the Game view did not reach the size (Screen {Screen.width}x{Screen.height})");
                    continue;
                }
                yield return _fixture.Frames(SettleFrames);
                NoteCanvases(size);
                yield return Baseline(size);
                var footprints = new Dictionary<string, Rect>();
                foreach (var scenario in Scenarios) yield return Measure(size, view, scenario.Name, scenario.Requests, footprints);
                CheckFootprints(size, footprints);
                yield return Pointer(size, view);
                foreach (var scenario in Scenarios) yield return Record(size, view, scenario.Name, scenario.Requests);
            }
#if UNITY_EDITOR
            Note("game view after captures", _gameView.Describe());
#endif
            if (_missingCharacters.Count != 0) _failures.Add("missing glyphs: " + string.Join("; ", _missingCharacters));
            foreach (string failure in _failures) Note("failure", failure);
            CollectionAssert.IsEmpty(_failures, "every capture check holds; the raw files are written either way");
        }

        // Dungeon goal fixture order, as UiWindowLiveCaptureTests: Town ready, the client's inventory query, the snapshot.
        IEnumerator ReadySyncedTown()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) == 1, "C_InventoryRequest #1", 3);
            _fixture.Peer.Send(Snapshot(1, 1250, (ItemId.Material, 3), (ItemId.CoinPouch, 1)));
            yield return MapEntryPlayFixture.Wait(() => InventoryPanelProbe.Status == "서버 확인 완료", "synchronized Town panel", 3);
            yield return ScreenUiProbe.WaitForEventSystem();
            Note("scripted peer", $"loopback 127.0.0.1:{_fixture.Peer.Port} (fixture stand-in, not GameServer)");
            Note("active scene", SceneManager.GetActiveScene().name);
        }

        IEnumerator OpenGameView()
        {
#if UNITY_EDITOR
            string permit = Environment.GetEnvironmentVariable(PermitVariable);
            Assert.IsFalse(string.IsNullOrEmpty(permit), "the capture runner names the GameView permit in " + PermitVariable);
            bool written = false;
            yield return ScreenUiProbe.WaitUntil(() => File.Exists(permit), PermitWaitSeconds, done => written = done);
            Assert.IsTrue(written, "the capture runner wrote the GameView permit: " + permit);
            _gameView = GameViewTargetSize.Find(out string missing);
            Assert.IsNotNull(_gameView, "a Game view to size: " + missing);
            _gameView.EnableApprovedSizes();
            Note("game view before", _gameView.Describe());
            Note("game view built-in sizes", _gameView.ListBuiltIn());
#else
            Assert.Ignore("the two-size capture needs the Editor's Game view");
            yield break;
#endif
        }

        // Idle frames before any effect: the reference for the scenarios' frame time and memory.
        IEnumerator Baseline(string size)
        {
            var rows = new List<string> { CostHeader };
            float start = Time.realtimeSinceStartup;
            for (int frame = 0; frame < BaselineFrames; frame++)
            {
                yield return null;
                rows.Add(CostRow(start, 0));
            }
            Note(size + " baseline", WriteNew($"{size}-baseline-frames.csv", string.Join("\n", rows) + "\n"));
        }

        // Frame time and memory every frame of one scenario without capturing, so capture cost does not enter the numbers.
        // The scenario's drawn footprint is kept for the size and overlap checks.
        IEnumerator Measure(string size, GuildGrowthEffectView view, string scenario, (GuildChangeKind Kind, int Level)[] requests,
            Dictionary<string, Rect> footprints)
        {
            var watch = new GuildGrowthEffectIndependentWatch($"{size}/{scenario}/measure", () => view != null ? view.transform : null);
            var rows = new List<string> { CostHeader };
            float requested = Request(view, size, scenario, requests, watch);
            float start = Time.realtimeSinceStartup;
            bool seen = false;
            float clearSince = float.NaN;
            while (Time.realtimeSinceStartup - start < WatchCap)
            {
                yield return null;
                GuildGrowthEffectIndependentFrame frame = watch.Sample();
                rows.Add(CostRow(start, frame.Parts.Length));
                if (frame.Any) { seen = true; clearSince = float.NaN; }
                else if (seen && float.IsNaN(clearSince)) clearSince = frame.At;
                if (seen && !float.IsNaN(clearSince) && frame.At - clearSince >= AfterClear) break;
            }
            Note($"{size} {scenario} measure", WriteNew($"{size}-{scenario}-measure-frames.csv", string.Join("\n", rows) + "\n"));
            footprints[scenario] = watch.Footprint();
            NoteAreas($"{size} {scenario}", watch);
            CheckShown($"{size} {scenario}", watch, requested, requests);
        }

        // The pointer passes through: at points where the two-kind effect is drawn, the raycast that decides whether a
        // click belongs to the UI hits nothing of the effect, and its topmost hit is the same as once the effect has ended.
        IEnumerator Pointer(string size, GuildGrowthEffectView view)
        {
            var watch = new GuildGrowthEffectIndependentWatch($"{size}/pointer", () => view != null ? view.transform : null);
            Request(view, size, "pointer", Scenarios.Single(item => item.Name == "both").Requests, watch);
            yield return watch.For(PointerAt);
            Vector2[] points = watch.Frames.Last().Parts.Select(part => GuildGrowthEffectIndependentProbe.Clip(part.Rect).center)
                .Distinct().Take(MaxPointerPoints).ToArray();
            if (points.Length == 0) _failures.Add($"{size}: no drawn part to probe the pointer at");
            GameObject[][] during = points.Select(point => ScreenUiProbe.RaycastHits(point)).ToArray();
            yield return watch.UntilClear(AfterClear, WatchCap);
            for (int index = 0; index < points.Length; index++)
            {
                GameObject[] after = ScreenUiProbe.RaycastHits(points[index]);
                string[] onEffect = during[index].Where(hit => view != null && hit.transform.IsChildOf(view.transform))
                    .Select(hit => ScreenUiProbe.PathOf(hit.transform, view.transform, true)).ToArray();
                string topDuring = during[index].Length == 0 ? "<none>" : during[index][0].name;
                string topAfter = after.Length == 0 ? "<none>" : after[0].name;
                Note($"{size} pointer {points[index]}", $"during [{string.Join(", ", during[index].Select(hit => hit.name))}] " +
                    $"after [{string.Join(", ", after.Select(hit => hit.name))}]");
                if (onEffect.Length != 0) _failures.Add($"{size}: the raycast at {points[index]} hits the effect: {string.Join(", ", onEffect)}");
                if (topDuring != topAfter) _failures.Add($"{size}: the topmost hit at {points[index]} is {topDuring} during, {topAfter} after");
            }
        }

        // A short recording of one scenario and its five key frames. The idle frame before the request is "before".
        IEnumerator Record(string size, GuildGrowthEffectView view, string scenario, (GuildChangeKind Kind, int Level)[] requests)
        {
            string prefix = $"{size}-{scenario}";
            var watch = new GuildGrowthEffectIndependentWatch($"{size}/{scenario}/record", () => view != null ? view.transform : null);
            yield return new WaitForEndOfFrame();
            SavePng(prefix + "-before", ScreenCapture.CaptureScreenshotAsTexture(), true);
            float requested = Request(view, size, scenario, requests, watch);
            var rows = new List<string> { "row,frame,real_from_request,dt,drawn,weight,numbers,file" };
            Texture2D emphasis = null, afterglow = null;
            float bestWeight = 0f;
            bool started = false, ended = false;
            // endedAt is the first clear frame; clearSince restarts if anything is drawn again after it.
            float endedAt = float.NaN, clearSince = float.NaN;
            int recorded = 0;
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < WatchCap)
            {
                yield return new WaitForEndOfFrame();
                GuildGrowthEffectIndependentFrame frame = watch.Sample();
                Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
                string file = "";
                if (recorded < RecordingCap && (frame.Any || started))
                {
                    file = $"{prefix}-rec-{recorded:000}.jpg";
                    WriteNew(file, shot.EncodeToJPG(JpegQuality));
                    recorded++;
                }
                rows.Add(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:F4},{3:F4},{4},{5:F1},{6},{7}", rows.Count, frame.Index,
                    frame.At - requested, frame.Delta, frame.Parts.Length, frame.Weight, string.Join(" ", frame.Numbers), file));
                bool keep = false;
                if (frame.Any)
                {
                    if (!started) { started = true; SavePng(prefix + "-start", shot, false); }
                    // The kept frames are the latest drawn one and the brightest so far; a frame that is neither
                    // any more is destroyed once.
                    Texture2D previousAfterglow = afterglow;
                    afterglow = shot;
                    if (frame.Weight > bestWeight)
                    {
                        bestWeight = frame.Weight;
                        if (emphasis != null && emphasis != previousAfterglow) UnityEngine.Object.Destroy(emphasis);
                        emphasis = shot;
                    }
                    if (previousAfterglow != null && previousAfterglow != emphasis) UnityEngine.Object.Destroy(previousAfterglow);
                    keep = true;
                    clearSince = float.NaN;
                }
                else if (started)
                {
                    if (!ended)
                    {
                        ended = true;
                        endedAt = frame.At;
                        SavePng(prefix + "-end", shot, false);
                    }
                    if (float.IsNaN(clearSince)) clearSince = frame.At;
                }
                if (!keep) UnityEngine.Object.Destroy(shot);
                if (ended && !float.IsNaN(clearSince) && frame.At - clearSince >= AfterClear) break;
            }
            Note($"{size} {scenario} recording", WriteNew(prefix + "-recording.csv", string.Join("\n", rows) + "\n") + $" frames {recorded}");
            if (emphasis != null) SavePng(prefix + "-emphasis", emphasis, false);
            if (afterglow != null) SavePng(prefix + "-afterglow", afterglow, false);
            if (afterglow != emphasis && afterglow != null) UnityEngine.Object.Destroy(afterglow);
            if (emphasis != null) UnityEngine.Object.Destroy(emphasis);
            if (!started) _failures.Add($"{prefix}: nothing was drawn while recording");
            if (started && !ended) _failures.Add($"{prefix}: the screen did not clear inside {WatchCap} s");
            GuildGrowthEffectIndependentFrame[] late = ended ? watch.After(endedAt).Where(frame => frame.Any).ToArray()
                : Array.Empty<GuildGrowthEffectIndependentFrame>();
            if (ended && late.Length != 0) _failures.Add($"{prefix}: drawn again after the end at frames {string.Join(",", late.Select(f => f.Index))}");
        }

        float Request(GuildGrowthEffectView view, string size, string scenario, (GuildChangeKind Kind, int Level)[] requests,
            GuildGrowthEffectIndependentWatch watch)
        {
            foreach (var (kind, level) in requests)
            {
                if (!view.TryPlayConfirmed(kind, level)) _failures.Add($"{size} {scenario}: {kind} {level} was refused");
            }
            return watch.Mark(scenario + " " + string.Join(" + ", requests.Select(item => $"{item.Kind} {item.Level}")));
        }

        // Each request's level, and nothing else, is shown as laid-out text inside the screen that overlaps no other shown
        // HUD text, and every requested level is on screen in one frame.
        void CheckShown(string label, GuildGrowthEffectIndependentWatch watch, float requested,
            (GuildChangeKind Kind, int Level)[] requests)
        {
            GuildGrowthEffectIndependentFrame[] frames = watch.After(requested);
            long[] shown = frames.SelectMany(frame => frame.Numbers).Distinct().OrderBy(number => number).ToArray();
            long[] asked = requests.Select(item => (long)item.Level).OrderBy(number => number).ToArray();
            if (!shown.SequenceEqual(asked)) _failures.Add($"{label}: shown levels [{string.Join(",", shown)}], requested [{string.Join(",", asked)}]");
            if (!frames.Any(frame => asked.All(level => frame.Numbers.Contains(level))))
                _failures.Add($"{label}: the requested levels are never on screen together");
            GuildGrowthEffectIndependentPart[] texts = frames.SelectMany(frame => frame.Parts).Where(part => part.LaidOut).ToArray();
            if (texts.Length == 0) _failures.Add($"{label}: no laid-out level text");
            foreach (GuildGrowthEffectIndependentPart text in texts.Where(part =>
                !GuildGrowthEffectIndependentProbe.Inside(part.Rect, GuildGrowthEffectIndependentProbe.ScreenBox())).Take(3))
            {
                _failures.Add($"{label}: level text leaves the screen: {text}");
            }
            var hud = HudTexts().ToArray();
            foreach (var (path, rect) in hud)
            {
                GuildGrowthEffectIndependentPart over = texts.FirstOrDefault(part => GuildGrowthEffectIndependentProbe.OverlapArea(part.Rect, rect) > 0f);
                if (over != null) _failures.Add($"{label}: level text {over} overlaps HUD text {path} {rect}");
            }
            Note(label + " HUD texts on screen", hud.Length);
        }

        // Personal smaller than shared, and the two kinds' footprints apart (approved plan: 작은 영역, 비중첩).
        void CheckFootprints(string size, Dictionary<string, Rect> footprints)
        {
            Rect shared = footprints["shared"], personal = footprints["personal"];
            float overlap = GuildGrowthEffectIndependentProbe.OverlapArea(shared, personal);
            Note(size + " footprints", string.Format(CultureInfo.InvariantCulture, "shared {0} personal {1} overlap {2:F0}", shared, personal, overlap));
            if (personal.width * personal.height >= shared.width * shared.height) _failures.Add($"{size}: the personal footprint is not smaller");
            if (overlap > 0f) _failures.Add($"{size}: the two kinds' footprints overlap by {overlap:F0} px²");
        }

        // Shown texts on screen-space canvases outside the effect, by their glyph bounds on screen.
        static IEnumerable<(string Path, Rect Rect)> HudTexts()
        {
            foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>())
            {
                if (!ScreenUiProbe.IsShown(text) || string.IsNullOrWhiteSpace(text.text) || text.textInfo == null ||
                    text.textInfo.characterCount == 0 || text.GetComponentInParent<GuildGrowthEffectView>() != null) continue;
                // Graphic.canvas is uGUI, which this assembly does not reference; the Canvas component is engine API.
                Canvas canvas = text.GetComponentInParent<Canvas>();
                if (canvas == null || canvas.rootCanvas.renderMode == RenderMode.WorldSpace) continue;
                Camera camera = canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
                Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, text.transform.TransformPoint(text.textBounds.min));
                Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, text.transform.TransformPoint(text.textBounds.max));
                Rect rect = Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
                if (GuildGrowthEffectIndependentProbe.OnScreen(rect) > 0f) yield return (PathOf(text.transform), rect);
            }
        }

        void NoteAreas(string label, GuildGrowthEffectIndependentWatch watch)
        {
            foreach (var area in watch.Frames.SelectMany(frame => frame.Parts).GroupBy(part => part.Area))
            {
                Rect union = GuildGrowthEffectIndependentProbe.Union(area.Select(part => GuildGrowthEffectIndependentProbe.Clip(part.Rect)));
                Note($"{label} area {area.Key}", string.Format(CultureInfo.InvariantCulture, "{0} of screen {1}x{2}", union, Screen.width, Screen.height));
            }
        }

        void NoteCanvases(string size)
        {
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>().Where(item => item.isRootCanvas && item.isActiveAndEnabled)
                .OrderBy(item => item.sortingOrder))
            {
                Note($"{size} root canvas {PathOf(canvas.transform)}", $"scene {canvas.gameObject.scene.name}, {canvas.renderMode}, order " +
                    $"{canvas.sortingOrder}, scaleFactor {canvas.scaleFactor.ToString("F3", CultureInfo.InvariantCulture)}");
            }
        }

        const string CostHeader = "frame,real_from_start,unscaled_dt,drawn,allocated,reserved,mono_used,gc_total,gc0_count";

        static string CostRow(float start, int drawn) => string.Format(CultureInfo.InvariantCulture, "{0},{1:F4},{2:F5},{3},{4},{5},{6},{7},{8}",
            Time.frameCount, Time.realtimeSinceStartup - start, Time.unscaledDeltaTime, drawn, Profiler.GetTotalAllocatedMemoryLong(),
            Profiler.GetTotalReservedMemoryLong(), Profiler.GetMonoUsedSizeLong(), GC.GetTotalMemory(false), GC.CollectionCount(0));

        // A captured frame as PNG; destroy says whether the texture is no longer needed after it.
        void SavePng(string name, Texture2D shot, bool destroy)
        {
            try
            {
                string path = WriteNew(name + ".png", shot.EncodeToPNG());
                Note("key frame " + name, $"{path} {shot.width}x{shot.height} Screen {Screen.width}x{Screen.height}");
                if (shot.width != Screen.width || shot.height != Screen.height) _failures.Add($"{name}: frame size differs from the screen");
            }
            finally
            {
                if (destroy) UnityEngine.Object.Destroy(shot);
            }
        }

        string WriteNew(string name, string text) => WriteNew(name, new System.Text.UTF8Encoding(false).GetBytes(text));

        // Every output is new: an existing file of the same name fails instead of being overwritten.
        string WriteNew(string name, byte[] bytes)
        {
            string path = Path.Combine(_output, name);
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) stream.Write(bytes, 0, bytes.Length);
            string hash;
            using (SHA256 sha = SHA256.Create()) hash = string.Concat(sha.ComputeHash(bytes).Select(item => item.ToString("x2")));
            return $"{path} {bytes.Length} bytes sha256 {hash}";
        }

        void Note(string key, object value)
        {
            _record.Add($"{key}: {value}");
            Debug.Log($"[GuildGrowthEffectIndependent capture] {key}: {value}");
        }

        static string PathOf(Transform transform) => transform.parent == null ? transform.name : PathOf(transform.parent) + "/" + transform.name;

        void RecordMissingCharacter(int unicode, int stringIndex, string text, TMP_FontAsset fontAsset, TMP_Text textComponent)
        {
            // The event is global; only the effect's texts count.
            if (textComponent == null || textComponent.GetComponentInParent<GuildGrowthEffectView>(true) == null) return;
            _missingCharacters.Add(string.Format(CultureInfo.InvariantCulture, "U+{0:X4} at {1} in '{2}' ({3})", unicode, stringIndex,
                text, fontAsset != null ? fontAsset.name : "no font"));
        }

        // The same wire snapshot UiWindowLiveCaptureTests builds; its builder is private to that class.
        static ArraySegment<byte> Snapshot(uint revision, int currency, params (ItemId item, int count)[] stacks)
        {
            var packet = new S_InventorySnapshot { revision = revision, currency = currency };
            for (int i = 0; i < stacks.Length; i++)
            {
                typeof(S_InventorySnapshot).GetField($"slot{i}ItemId").SetValue(packet, (int)stacks[i].item);
                typeof(S_InventorySnapshot).GetField($"slot{i}Count").SetValue(packet, stacks[i].count);
            }
            return packet.Write();
        }
    }
}
