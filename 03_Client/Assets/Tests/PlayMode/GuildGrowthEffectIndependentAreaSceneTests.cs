using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Independent real-screen check of GuildGrowthEffectView.TrySetAvailableArea over the real Town at 1920x1080 and 1280x720
    // (effect-layout-independent-01, verifying implementation Task task_1d758d73cee1). Town is entered the way
    // GuildGrowthEffectIndependentCaptureTests does, and the effect is built under a plain Town object because the Content
    // caller is a later PR. The test plays that caller: it measures the live Guild window root that the Town's
    // GuildUiController built through the public GuildWindow.BuildRuntime, takes its screen box as a fraction of the effect
    // canvas and passes it in. That root is the verdict area (Main msg_04e16c8af059 B). The window's private "Guild" panel is
    // read by name only, as an extra observation; the window is never opened and no Content state changes.
    // Every frame each active graphic is boxed by all four world corners (the halo and sparks rotate, which the two-corner
    // ScreenUiProbe.ScreenRect understates) and each level text by its padded glyph quads. Inside and apart are judged against
    // those boxes and the given area only, never against the product's layout formula. Readability has no approved number:
    // TMP's chosen size and the glyph heights in pixels under the default layout and under each area are written side by side,
    // with one PNG per scenario for visual review. A tiny area checks geometry, separation and state only (Main
    // msg_fbd9d6179581 A). Runs only when the capture runner names its output folder and writes the GameView permit, and needs
    // the Editor's Game view (no -batchmode).
    public sealed class GuildGrowthEffectIndependentAreaSceneTests
    {
        const string OutputVariable = "DAWNHOLDER_UI_CAPTURE_DIR";
        const string PermitVariable = "DAWNHOLDER_GAMEVIEW_PERMIT";
        // The runner writes the permit after it has identified the Unity process it started.
        const float PermitWaitSeconds = 120f;
        const float SizeWaitSeconds = 3f;
        const int SettleFrames = 10;
        const float AfterClear = 0.5f;
        const float WatchCap = 4.5f;
        // Mid-playback changes run this long after the request, while both kinds are still drawn.
        const float ChangeAt = 0.4f;
        // Below one 8-bit colour step nothing shows on screen.
        const float MinimumAlpha = 1f / 255f;
        const int SharedLevel = 5;
        const int PersonalLevel = 8;
        const int ExamplesPerCheck = 3;
        const string RecordHeader = "key,value";
        const string PartsHeader = "frame,real_from_request,screen,area,kind,path,alpha,x_min,y_min,x_max,y_max,inside,phase";
        const string LabelsHeader = "frame,real_from_request,kind,text,alpha,font_size,font_min,font_max,lossy_scale_y,font_px," +
            "visible_chars,non_space_chars,glyph_min_px,glyph_max_px,digit_max_px,label_box_inside,screen,area,phase";
        const string CapturesHeader = "file,frame,real_from_request,screen,area,phase,capture_frame,capture_real_from_request," +
            "capture_utc,capture_screen,texture,change_frame,change_real_from_request,target_screen,target_area," +
            "after_change_frame,target_screen_reached,target_area_applied,both_labels_drawn,parts_csv,labels_csv";

        static readonly (int Width, int Height)[] Sizes = { (1920, 1080), (1280, 720) };
        // Valid, but far too small for text (Main A).
        static readonly Rect Tiny = new Rect(0.62f, 0.12f, 0.02f, 0.02f);
        // Each must be refused: not finite, not positive, or not entirely inside 0..1.
        static readonly (string Name, Rect Area)[] Invalid =
        {
            ("NaN x", new Rect(float.NaN, 0.1f, 0.5f, 0.5f)),
            ("+Infinity width", new Rect(0.1f, 0.1f, float.PositiveInfinity, 0.5f)),
            ("-Infinity y", new Rect(0.1f, float.NegativeInfinity, 0.5f, 0.5f)),
            ("zero width", new Rect(0.1f, 0.1f, 0f, 0.5f)),
            ("negative height", new Rect(0.1f, 0.1f, 0.5f, -0.2f)),
            ("x below 0", new Rect(-0.1f, 0.1f, 0.5f, 0.5f)),
            ("y at 1", new Rect(0.1f, 1f, 0.5f, 0.1f)),
            ("right edge past 1", new Rect(0.6f, 0.1f, 0.5f, 0.5f)),
            ("top edge past 1", new Rect(0.1f, 0.6f, 0.5f, 0.5f)),
        };
        static readonly Regex Digits = new Regex("\\d+");

        readonly List<string> _record = new List<string>();
        readonly List<string> _failures = new List<string>();
        readonly List<string> _observations = new List<string>();
        readonly List<string> _missingCharacters = new List<string>();
        // Per screen size, each kind's root box under the default layout: what null has to restore.
        readonly Dictionary<string, Dictionary<string, Rect>> _defaultRoots = new Dictionary<string, Dictionary<string, Rect>>();
        // Per "size scenario kind", the level text reading of the frame kept as PNG.
        readonly Dictionary<string, LabelReading> _keptLabels = new Dictionary<string, LabelReading>();
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
            _observations.Clear();
            _missingCharacters.Clear();
            _defaultRoots.Clear();
            _keptLabels.Clear();
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
            if (!string.IsNullOrEmpty(_output) && Directory.Exists(_output))
            {
                WriteNew("area-record.csv", RecordHeader + "\n" + string.Join("\n", _record) + "\n");
            }
            if (restoreError != null) throw new InvalidOperationException("Game view restore failed", restoreError);
        }

        [UnityTest]
        public IEnumerator AvailableArea_MeasuredGuildWindowRoot_AtBothSizes_KeepsBothKindsInsideAndApart()
        {
            _output = Environment.GetEnvironmentVariable(OutputVariable);
            if (string.IsNullOrEmpty(_output)) Assert.Ignore($"live area check runs only when the capture runner sets {OutputVariable}");
            Assert.IsTrue(Directory.Exists(_output), "capture output folder exists: " + _output);
            Note("utc", DateTime.UtcNow.ToString("o"));
            Note("isPlaying / isBatchMode / unityVersion", $"{Application.isPlaying} / {Application.isBatchMode} / {Application.unityVersion}");
            Note("graphics device", $"{SystemInfo.graphicsDeviceType} / {SystemInfo.graphicsDeviceName}");

            yield return ReadySyncedTown();
            _holder = new GameObject("GuildGrowthEffectIndependentAreaHolder");
            Note("holder scene", _holder.scene.name);
            GuildGrowthEffectView view = GuildGrowthEffectView.BuildRuntime(_holder.transform);
            Note("effect canvas sorting order", view.GetComponent<Canvas>().sortingOrder);
            yield return OpenGameView();

            foreach (var (width, height) in Sizes)
            {
                string size = width + "x" + height;
                bool reached = false;
                yield return Select(width, height, done => reached = done);
                if (!reached) continue;
                yield return _fixture.Frames(SettleFrames);
                if (!TryMeasureContent(size, view, out Rect root, out Rect? panel)) continue;
                yield return Scenario(size, "default", view, null, null);
                // The invalid calls and the pointer probe run while both kinds play inside the measured root.
                yield return Scenario(size, "root", view, root, current =>
                {
                    Refuse(size + " root", view);
                    Pointer(size + " root", view);
                    return current;
                });
                if (panel.HasValue)
                {
                    yield return Scenario(size, "panel", view, panel, null);
                    yield return Scenario(size, "update", view, panel, current => Change(size + " update", view, current, root),
                        afterArea: root);
                }
                else
                {
                    _failures.Add($"{size} update: changed-state PNG is unproved because the starting panel was unavailable");
                }
                yield return Scenario(size, "restore", view, root, current => Change(size + " restore", view, current, null));
                yield return Scenario(size, "tiny", view, Tiny, current =>
                {
                    Refuse(size + " tiny", view);
                    return current;
                });
                CompareReadability(size);
            }
#if UNITY_EDITOR
            yield return Resize(view);
            Note("game view after checks", _gameView.Describe());
#endif
            if (_missingCharacters.Count != 0) _failures.Add("missing glyphs: " + string.Join("; ", _missingCharacters));
            foreach (string observation in _observations) Note("observation", observation);
            foreach (string failure in _failures) Note("failure", failure);
            CollectionAssert.IsEmpty(_failures, "every area check holds; the raw files are written either way");
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
            Assert.Ignore("the two-size area check needs the Editor's Game view");
            yield break;
#endif
        }

        IEnumerator Select(int width, int height, Action<bool> done)
        {
            string size = width + "x" + height;
#if UNITY_EDITOR
            Note(size + " select", _gameView.TrySelect(width, height));
#endif
            bool reached = false;
            yield return ScreenUiProbe.WaitUntil(() => Screen.width == width && Screen.height == height, SizeWaitSeconds,
                result => reached = result);
            Note(size + " Screen after selection", $"{Screen.width}x{Screen.height} reached {reached}");
            if (!reached) _failures.Add($"{size}: the Game view did not reach the size (Screen {Screen.width}x{Screen.height})");
            done(reached);
        }

#if UNITY_EDITOR
        // A size change while both kinds play: the measured root stays the area and has to hold on every frame through it.
        IEnumerator Resize(GuildGrowthEffectView view)
        {
            bool reached = false;
            yield return Select(1920, 1080, done => reached = done);
            if (!reached) yield break;
            yield return _fixture.Frames(SettleFrames);
            if (!TryMeasureContent("resize 1920x1080", view, out Rect root, out _)) yield break;
            yield return Scenario("1920x1080-to-1280x720", "resize", view, root, current =>
            {
                Note("resize select 1280x720", _gameView.TrySelect(1280, 720));
                return current;
            }, afterArea: root, afterSize: (1280, 720));
            Note("resize Screen after", $"{Screen.width}x{Screen.height}");
            if (Screen.width != 1280 || Screen.height != 720)
            {
                _failures.Add("resize: the Game view did not reach 1280x720 while both kinds played " +
                    $"(Screen {Screen.width}x{Screen.height})");
            }
        }
#endif

        // The caller's side of the API. The Town's GuildUiController builds the Guild window through the public
        // GuildWindow.BuildRuntime; its root's measured screen box, as a fraction of the effect canvas, is the verdict area.
        // The private panel "Guild" is read by name only, as an extra observation. Nothing is opened or changed.
        bool TryMeasureContent(string size, GuildGrowthEffectView view, out Rect root, out Rect? panel)
        {
            root = default;
            panel = null;
            GuildWindow[] windows = UnityEngine.Object.FindObjectsByType<GuildWindow>();
            Note(size + " guild windows", string.Join("; ", windows.Select(window =>
                $"{PathOf(window.transform)} scene {window.gameObject.scene.name} open {window.IsOpen}")));
            if (windows.Length != 1)
            {
                _failures.Add($"{size}: the Town has {windows.Length} Guild windows, not one");
                return false;
            }
            Rect canvas = CornerBox((RectTransform)view.transform);
            Rect rootBox = CornerBox((RectTransform)windows[0].transform);
            root = Normalize(rootBox, canvas);
            Note(size + " effect canvas box", Describe(canvas));
            Note(size + " guild root box / normalized", $"{Describe(rootBox)} / {Describe(root)} (measured, the verdict area)");
            var panelRect = windows[0].transform.Find("Guild") as RectTransform;
            if (panelRect == null)
            {
                _observations.Add($"{size}: the Guild window has no child named Guild to observe");
                return true;
            }
            Rect panelBox = CornerBox(panelRect);
            panel = Normalize(panelBox, canvas);
            Note(size + " guild panel box / normalized", $"{Describe(panelBox)} / {Describe(panel)} (measured, observation only)");
            return true;
        }

        // Passes start, requests both kinds and reads every frame at its end until the screen has been clear for AfterClear.
        // midway runs once, ChangeAt after the request, and returns the area in force from the next frame.
        IEnumerator Scenario(string size, string name, GuildGrowthEffectView view, Rect? start, Func<Rect?, Rect?> midway,
            Rect? afterArea = null, (int Width, int Height)? afterSize = null)
        {
            string label = size + " " + name;
            bool accepted = view.TrySetAvailableArea(start);
            Note(label + " start area", $"{Describe(start)} accepted {accepted}");
            if (!accepted)
            {
                _failures.Add($"{label}: the start area {Describe(start)} was refused");
                yield break;
            }
            (int Width, int Height) target = afterSize ?? (Screen.width, Screen.height);
            var log = new ScenarioLog { AfterArea = afterArea, AfterWidth = target.Width, AfterHeight = target.Height };
            Rect? area = start;
            bool changed = false, seen = false;
            bool needsAfterFrame = name == "update" || name == "restore" || name == "resize";
            float clearSince = float.NaN, keptScore = 0f, afterScore = 0f;
            ScenarioFrame kept = null, keptAfter = null;
            float requested = Request(view, label);
            try
            {
                while (Time.realtimeSinceStartup - requested < WatchCap)
                {
                    yield return new WaitForEndOfFrame();
                    var frame = new ScenarioFrame
                    {
                        Index = Time.frameCount,
                        At = Time.realtimeSinceStartup - requested,
                        ScreenWidth = Screen.width,
                        ScreenHeight = Screen.height,
                        Area = area,
                        Phase = midway == null ? "unchanged" : changed ? "after-change" : "before-change",
                    };
                    List<Part> reading = Read(view);
                    frame.Labels = Judge(name, view, frame, reading, log);
                    // Keep the original strict maximum over the whole playback. A separate maximum supplies the
                    // changed-state evidence, after the request frame and only once the actual target is drawn.
                    float score = frame.Labels.Length == 2 ? frame.Labels.Min(item => item.Alpha) : 0f;
                    bool keepOverall = score > keptScore;
                    bool afterRequestFrame = changed && frame.Index > log.ChangeFrame;
                    bool targetScreenReached = frame.ScreenWidth == log.AfterWidth && frame.ScreenHeight == log.AfterHeight;
                    bool targetAreaApplied = Nullable.Equals(frame.Area, log.AfterArea);
                    bool keepAfter = needsAfterFrame && afterRequestFrame && targetScreenReached && targetAreaApplied &&
                        frame.BothLabelsDrawn && score > afterScore;
                    if (keepOverall || keepAfter)
                    {
                        frame.Capture(requested);
                        ScenarioFrame previousKept = kept, previousAfter = keptAfter;
                        if (keepOverall)
                        {
                            keptScore = score;
                            kept = frame;
                        }
                        if (keepAfter)
                        {
                            afterScore = score;
                            keptAfter = frame;
                        }
                        if (previousKept != kept && previousKept != keptAfter) previousKept?.Dispose();
                        if (previousAfter != previousKept && previousAfter != kept && previousAfter != keptAfter) previousAfter?.Dispose();
                    }
                    if (reading.Any(part => part.Visible))
                    {
                        seen = true;
                        clearSince = float.NaN;
                    }
                    else if (seen && float.IsNaN(clearSince))
                    {
                        clearSince = frame.At;
                    }
                    if (midway != null && !changed && frame.At >= ChangeAt)
                    {
                        changed = true;
                        area = midway(area);
                        log.ChangeFrame = frame.Index;
                        log.ChangedAt = frame.At;
                        Note(label + " midway", string.Format(CultureInfo.InvariantCulture, "at {0:F4} s frame {1}, area now {2}", frame.At,
                            frame.Index, Describe(area)));
                    }
                    if (seen && !float.IsNaN(clearSince) && frame.At - clearSince >= AfterClear) break;
                }

                if (!seen) _failures.Add($"{label}: nothing was drawn");
                else if (float.IsNaN(clearSince)) _failures.Add($"{label}: the screen did not clear inside {WatchCap} s");
                if (!log.Shown.SetEquals(new long[] { SharedLevel, PersonalLevel }))
                {
                    _failures.Add($"{label}: shown levels [{string.Join(",", log.Shown)}], requested [{SharedLevel},{PersonalLevel}]");
                }
                if (!log.BothTogether) _failures.Add($"{label}: the two levels are never drawn together");
                if (midway != null && !changed) _failures.Add($"{label}: the midway change never ran");
                if (changed && !log.BothAfterChange) _failures.Add($"{label}: the two levels are not drawn together after the midway change");
                if (needsAfterFrame && keptAfter == null)
                {
                    _failures.Add($"{label}: no frame after change frame {log.ChangeFrame} drew both complete level texts " +
                        $"at target Screen {log.AfterWidth}x{log.AfterHeight} and area {Describe(log.AfterArea)} to keep as PNG");
                }
                _failures.AddRange(log.Failed.Lines(label));
                _observations.AddRange(log.Observed.Lines(label));
                string prefix = size + "-" + name;
                Note(label + " parts", WriteNew(prefix + "-parts.csv", string.Join("\n", log.Parts) + "\n"));
                Note(label + " labels", WriteNew(prefix + "-labels.csv", string.Join("\n", log.Labels) + "\n"));
                Note(label + " frames / last drawn after the request", $"{log.Frames} / " + string.Join(", ", log.LastDrawn.Select(item =>
                    string.Format(CultureInfo.InvariantCulture, "{0} {1:F4} s", item.Key, item.Value))));
                if (kept == null)
                {
                    _failures.Add($"{label}: no frame drew both level texts to keep as PNG");
                    yield break;
                }
                SavePng(prefix + "-labels", kept, prefix, log);
                if (keptAfter != null) SavePng(prefix + "-after-labels", keptAfter, prefix, log);
                Note(label + " captures", WriteNew(prefix + "-captures.csv", string.Join("\n", log.Captures) + "\n"));
                foreach (LabelReading reading in kept.Labels)
                {
                    _keptLabels[$"{size} {name} {reading.Kind}"] = reading;
                    Note($"{label} kept frame {reading.Kind}", reading);
                }
            }
            finally
            {
                kept?.Dispose();
                if (keptAfter != kept) keptAfter?.Dispose();
            }
        }

        // Records one frame and judges it. Under an area every drawn part lies inside it and the two kinds' drawn boxes are
        // apart; a drawn level text draws all its characters; after a midway null each kind's root is back at the default.
        LabelReading[] Judge(string name, GuildGrowthEffectView view, ScenarioFrame sample, List<Part> reading, ScenarioLog log)
        {
            int frame = sample.Index;
            float at = sample.At;
            Rect? area = sample.Area;
            bool changed = sample.Phase == "after-change";
            string screen = sample.ScreenWidth + "x" + sample.ScreenHeight;
            string where = string.Format(CultureInfo.InvariantCulture, "frame {0} at {1:F4} s", frame, at);
            Rect? box = area.HasValue ? OnCanvas(area.Value, view) : (Rect?)null;
            log.Frames++;
            foreach (Part part in reading)
            {
                bool? inside = box.HasValue ? GuildGrowthEffectIndependentProbe.Inside(part.Box, box.Value) : (bool?)null;
                log.Parts.Add(string.Format(CultureInfo.InvariantCulture, "{0},{1:F4},{2},{3},{4},{5},{6:F4},{7:F3},{8:F3},{9:F3},{10:F3},{11},{12}",
                    frame, at, screen, Describe(area), part.Kind, part.Path, part.Alpha, part.Box.xMin, part.Box.yMin, part.Box.xMax,
                    part.Box.yMax, inside.HasValue ? (inside.Value ? "1" : "0") : "", sample.Phase));
                if (inside != false) continue;
                if (part.Visible) log.Failed.Add("a drawn part leaves the area", $"{where} {part} area {Describe(box.Value)}");
                else log.Observed.Add("a transparent part lies outside the area", $"{where} {part} area {Describe(box.Value)}");
            }

            Part[] drawn = reading.Where(part => part.Visible).ToArray();
            Rect[] kinds = drawn.GroupBy(part => part.Kind)
                .Select(group => GuildGrowthEffectIndependentProbe.Union(group.Select(part => part.Box))).ToArray();
            if (box.HasValue && kinds.Length == 2)
            {
                float overlap = GuildGrowthEffectIndependentProbe.OverlapArea(kinds[0], kinds[1]);
                if (overlap > 0f)
                {
                    log.Failed.Add("the two kinds' drawn boxes overlap", string.Format(CultureInfo.InvariantCulture,
                        "{0} {1} and {2} by {3:F1} px²", where, Describe(kinds[0]), Describe(kinds[1]), overlap));
                }
            }

            var labels = new List<LabelReading>();
            foreach (Part part in reading.Where(item => item.Label != null))
            {
                LabelReading label = LabelReading.Of(part, box);
                labels.Add(label);
                log.Labels.Add(label.Row(frame, at) + $",{screen},{Describe(area)},{sample.Phase}");
                if (part.Visible && label.Visible != label.NonSpace)
                {
                    log.Failed.Add("a drawn level text does not draw all its characters", $"{where} {label}");
                }
            }
            long[] numbers = drawn.Where(part => part.Label != null).SelectMany(part => Numbers(part.Label.text)).Distinct().ToArray();
            foreach (long number in numbers) log.Shown.Add(number);
            if (numbers.Contains(SharedLevel) && numbers.Contains(PersonalLevel))
            {
                log.BothTogether = true;
                if (changed) log.BothAfterChange = true;
            }
            foreach (Part part in drawn) log.LastDrawn[part.Kind] = at;

            Dictionary<string, Rect> roots = Roots(view);
            if (name == "default" && roots.Count == 2)
            {
                if (!_defaultRoots.TryGetValue(screen, out Dictionary<string, Rect> first)) _defaultRoots[screen] = roots;
                else if (!Same(roots, first)) log.Observed.Add("the default roots move between frames", $"{where} {DescribeRoots(roots)}");
            }
            else if (name != "default" && changed && !area.HasValue)
            {
                if (!_defaultRoots.TryGetValue(screen, out Dictionary<string, Rect> expected))
                {
                    log.Failed.Add("no default layout was recorded at this size", $"{where} {screen}");
                }
                else
                {
                    foreach (KeyValuePair<string, Rect> root in roots.Where(item =>
                        !expected.TryGetValue(item.Key, out Rect was) || !Same(item.Value, was)))
                    {
                        log.Failed.Add("null does not restore the default layout",
                            $"{where} {root.Key} {Describe(root.Value)}, default {DescribeRoots(expected)}");
                    }
                }
            }
            return labels.ToArray();
        }

        float Request(GuildGrowthEffectView view, string label)
        {
            if (!view.TryPlayConfirmed(GuildChangeKind.StrongholdUpgrade, SharedLevel)) _failures.Add($"{label}: StrongholdUpgrade {SharedLevel} was refused");
            if (!view.TryPlayConfirmed(GuildChangeKind.WeaponEnhance, PersonalLevel)) _failures.Add($"{label}: WeaponEnhance {PersonalLevel} was refused");
            return Time.realtimeSinceStartup;
        }

        Rect? Change(string label, GuildGrowthEffectView view, Rect? current, Rect? next)
        {
            bool accepted = view.TrySetAvailableArea(next);
            Note(label + " change", $"{Describe(next)} accepted {accepted}");
            if (accepted) return next;
            _failures.Add($"{label}: the midway area {Describe(next)} was refused");
            return current;
        }

        // Every invalid area is refused, and in the same frame no part moves, fades or changes text.
        void Refuse(string label, GuildGrowthEffectView view)
        {
            string[] before = Read(view).Select(part => part.Exact).ToArray();
            foreach (var (name, area) in Invalid)
            {
                bool accepted = view.TrySetAvailableArea(area);
                Note($"{label} invalid {name}", $"{Describe(area)} accepted {accepted}");
                if (accepted) _failures.Add($"{label}: the invalid area {name} {Describe(area)} was accepted");
            }
            string[] after = Read(view).Select(part => part.Exact).ToArray();
            bool same = before.SequenceEqual(after);
            Note(label + " parts before / after the invalid calls", $"{before.Length} / {after.Length}, unchanged {same}");
            if (!same)
            {
                _failures.Add($"{label}: the invalid calls changed what is drawn: before [{string.Join("; ", before)}] " +
                    $"after [{string.Join("; ", after)}]");
            }
        }

        // The effect never takes the pointer: at the centre of each drawn part the UI raycast hits nothing of the effect.
        void Pointer(string label, GuildGrowthEffectView view)
        {
            Vector2[] points = Read(view).Where(part => part.Visible)
                .Select(part => GuildGrowthEffectIndependentProbe.Clip(part.Box).center).Distinct().ToArray();
            if (points.Length == 0) _failures.Add($"{label}: no drawn part to probe the pointer at");
            int hit = 0;
            foreach (Vector2 point in points)
            {
                string[] onEffect = ScreenUiProbe.RaycastHits(point).Where(item => item.transform.IsChildOf(view.transform))
                    .Select(item => ScreenUiProbe.PathOf(item.transform, view.transform, true)).ToArray();
                if (onEffect.Length == 0) continue;
                hit++;
                _failures.Add($"{label}: the raycast at {point} hits the effect: {string.Join(", ", onEffect)}");
            }
            Note(label + " pointer", $"{points.Length} points probed, {hit} hit the effect");
        }

        // No approved readability number exists: the kept frames' on-screen font size and digit height under each area beside
        // the default layout's, as raw ratios for the visual review of the PNGs.
        void CompareReadability(string size)
        {
            string prefix = size + " default ";
            foreach (string kind in _keptLabels.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
                .Select(key => key.Substring(prefix.Length)).ToArray())
            {
                LabelReading baseline = _keptLabels[prefix + kind];
                foreach (string name in new[] { "root", "panel", "tiny" })
                {
                    if (!_keptLabels.TryGetValue($"{size} {name} {kind}", out LabelReading other)) continue;
                    Note($"{size} readability {name} vs default {kind}", string.Format(CultureInfo.InvariantCulture,
                        "font px {0:F2} / {1:F2} = {2:F3}, digit px {3:F2} / {4:F2} = {5:F3}, font size {6:F2} / {7:F2}", other.FontPixels,
                        baseline.FontPixels, other.FontPixels / baseline.FontPixels, other.DigitMax, baseline.DigitMax,
                        other.DigitMax / baseline.DigitMax, other.FontSize, baseline.FontSize));
                }
            }
        }

        // Every active graphic under the effect. Unlike GuildGrowthEffectIndependentProbe.Drawn this keeps transparent and
        // off-screen parts and boxes each by all four corners, because containment has to see the whole quad of a rotated
        // part; level texts are boxed by their padded glyph quads. The alpha rule repeats the probe's private one.
        static List<Part> Read(GuildGrowthEffectView view)
        {
            var parts = new List<Part>();
            if (view == null) return parts;
            Transform scope = view.transform;
            foreach (CanvasRenderer renderer in scope.GetComponentsInChildren<CanvasRenderer>())
            {
                var graphic = renderer.GetComponents<Component>().FirstOrDefault(IsGraphic) as Behaviour;
                if (graphic == null || !graphic.isActiveAndEnabled || !(renderer.transform is RectTransform rect)) continue;
                var label = graphic as TMP_Text;
                // TMP's sub-mesh graphics draw glyphs of their text, which the text's own quads already cover.
                if (label == null && graphic.GetComponentInParent<TMP_Text>() != null) continue;
                if (label != null && string.IsNullOrEmpty(label.text)) continue;
                string path = ScreenUiProbe.PathOf(rect, scope);
                parts.Add(new Part
                {
                    Kind = path.Split('/')[0],
                    Path = path,
                    Box = label != null ? GlyphBox(label) : CornerBox(rect),
                    Alpha = AlphaOf(graphic) * renderer.GetAlpha() * GroupAlpha(rect),
                    Label = label,
                });
            }
            return parts;
        }

        // Each kind's root box while it plays: the effect's active direct children.
        static Dictionary<string, Rect> Roots(GuildGrowthEffectView view)
        {
            var roots = new Dictionary<string, Rect>();
            foreach (Transform child in view.transform)
            {
                if (child.gameObject.activeSelf && child is RectTransform rect) roots[child.name] = CornerBox(rect);
            }
            return roots;
        }

        // A normalized area of the effect canvas in screen pixels (anchor space, bottom-left origin).
        static Rect OnCanvas(Rect area, GuildGrowthEffectView view)
        {
            Rect canvas = CornerBox((RectTransform)view.transform);
            return new Rect(canvas.xMin + area.x * canvas.width, canvas.yMin + area.y * canvas.height, area.width * canvas.width,
                area.height * canvas.height);
        }

        // The caller's computation: a screen box as a fraction of the effect canvas.
        static Rect Normalize(Rect box, Rect canvas) => new Rect((float)((box.xMin - (double)canvas.xMin) / canvas.width),
            (float)((box.yMin - (double)canvas.yMin) / canvas.height), (float)(box.width / (double)canvas.width),
            (float)(box.height / (double)canvas.height));

        // On Overlay canvases world corners are screen pixels.
        static Rect CornerBox(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return BoxOf(corners.Select(corner => (Vector2)corner));
        }

        // The padded quads TMP draws for the visible characters; the text's rectangle while none is laid out.
        static Rect GlyphBox(TMP_Text text)
        {
            TMP_TextInfo info = text.textInfo;
            var points = new List<Vector2>();
            for (int i = 0; info != null && i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible) continue;
                foreach (Vector3 local in new[] { character.vertex_BL.position, character.vertex_TL.position, character.vertex_TR.position,
                    character.vertex_BR.position })
                {
                    points.Add(text.transform.TransformPoint(local));
                }
            }
            return points.Count == 0 ? CornerBox(text.rectTransform) : BoxOf(points);
        }

        static Rect BoxOf(IEnumerable<Vector2> points)
        {
            Vector2[] all = points.ToArray();
            return Rect.MinMaxRect(all.Min(point => point.x), all.Min(point => point.y), all.Max(point => point.x), all.Max(point => point.y));
        }

        static bool Same(Rect a, Rect b) => Mathf.Approximately(a.xMin, b.xMin) && Mathf.Approximately(a.yMin, b.yMin) &&
            Mathf.Approximately(a.xMax, b.xMax) && Mathf.Approximately(a.yMax, b.yMax);

        static bool Same(Dictionary<string, Rect> a, Dictionary<string, Rect> b) =>
            a.Count == b.Count && a.All(item => b.TryGetValue(item.Key, out Rect other) && Same(item.Value, other));

        static IEnumerable<long> Numbers(string text) =>
            Digits.Matches(text ?? string.Empty).Cast<Match>().Select(match => long.Parse(match.Value, CultureInfo.InvariantCulture));

        static bool IsGraphic(Component component)
        {
            for (Type type = component != null ? component.GetType() : null; type != null; type = type.BaseType)
            {
                if (type.FullName == "UnityEngine.UI.Graphic") return true;
            }
            return false;
        }

        // uGUI is reached by name because this assembly does not reference it.
        static float AlphaOf(Component graphic) =>
            graphic is TMP_Text text ? text.color.a : ((Color)graphic.GetType().GetProperty("color").GetValue(graphic)).a;

        // A CanvasGroup's alpha multiplies everything below it until a group that ignores its parents (uGUI's own rule).
        static float GroupAlpha(Transform transform)
        {
            float alpha = 1f;
            for (Transform current = transform; current != null; current = current.parent)
            {
                foreach (CanvasGroup group in current.GetComponents<CanvasGroup>())
                {
                    if (!group.enabled) continue;
                    alpha *= group.alpha;
                    if (group.ignoreParentGroups) return alpha;
                }
            }
            return alpha;
        }

        // Exact round-trip numbers without commas, so they also fit a CSV field.
        static string Describe(Rect? rect) => rect.HasValue
            ? string.Format(CultureInfo.InvariantCulture, "(x {0:R} y {1:R} w {2:R} h {3:R})", rect.Value.x, rect.Value.y, rect.Value.width,
                rect.Value.height)
            : "null";

        static string DescribeRoots(Dictionary<string, Rect> roots) => string.Join(" ", roots.Select(item => item.Key + " " + Describe(item.Value)));

        // The scenario owns the texture until both selections have been saved. Compare against its capture Screen,
        // while the observation frame joins the PNG to the parts/labels CSVs; the later save Screen is diagnostic only.
        void SavePng(string name, ScenarioFrame sample, string prefix, ScenarioLog log)
        {
            string path = WriteNew(name + ".png", sample.Texture.EncodeToPNG());
            Note("kept frame " + name, string.Format(CultureInfo.InvariantCulture,
                "{0} texture {1}x{2} capture Screen {3}x{4} frame {5} at {6:R} s UTC {7}, observed frame {8} at {9:F4} s " +
                "area {10} phase {11}; save Screen {12}x{13} frame {14}", path, sample.TextureWidth, sample.TextureHeight,
                sample.CaptureScreenWidth, sample.CaptureScreenHeight, sample.CaptureIndex, sample.CaptureAt, sample.CaptureUtc,
                sample.Index, sample.At, Describe(sample.Area), sample.Phase, Screen.width, Screen.height, Time.frameCount));
            bool textureMatchesCapture = sample.TextureWidth == sample.CaptureScreenWidth && sample.TextureHeight == sample.CaptureScreenHeight;
            bool observationMatchesCapture = sample.Index == sample.CaptureIndex && sample.ScreenWidth == sample.CaptureScreenWidth &&
                sample.ScreenHeight == sample.CaptureScreenHeight;
            if (!textureMatchesCapture) _failures.Add($"{name}: frame size differs from the capture screen");
            if (!observationMatchesCapture) _failures.Add($"{name}: capture frame or Screen differs from the observed CSV frame");
            log.Captures.Add(string.Format(CultureInfo.InvariantCulture,
                "{0},{1},{2:F4},{3}x{4},{5},{6},{7},{8:R},{9},{10}x{11},{12}x{13},{14},{15:R},{16}x{17},{18},{19},{20},{21},{22},{23},{24}",
                name + ".png", sample.Index, sample.At, sample.ScreenWidth, sample.ScreenHeight, Describe(sample.Area), sample.Phase,
                sample.CaptureIndex, sample.CaptureAt, sample.CaptureUtc, sample.CaptureScreenWidth, sample.CaptureScreenHeight,
                sample.TextureWidth, sample.TextureHeight, log.ChangeFrame, log.ChangedAt, log.AfterWidth, log.AfterHeight,
                Describe(log.AfterArea), sample.Phase == "after-change" && sample.Index > log.ChangeFrame ? "1" : "0",
                sample.ScreenWidth == log.AfterWidth && sample.ScreenHeight == log.AfterHeight ? "1" : "0",
                Nullable.Equals(sample.Area, log.AfterArea) ? "1" : "0", sample.BothLabelsDrawn ? "1" : "0",
                prefix + "-parts.csv", prefix + "-labels.csv"));
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

        // The record is CSV because the capture folder keeps only png, jpg, json and csv files.
        void Note(string key, object value)
        {
            _record.Add(Field(key) + "," + Field(Convert.ToString(value, CultureInfo.InvariantCulture)));
            Debug.Log($"[GuildGrowthEffectIndependent area] {key}: {value}");
        }

        static string Field(string text) => "\"" + (text ?? string.Empty).Replace("\"", "\"\"") + "\"";

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

        // One end-of-frame observation and, only if selected, its actual screen texture. Capturing never yields;
        // the saved metadata checks that Screen/frame still match the observation used by both raw CSVs.
        sealed class ScenarioFrame : IDisposable
        {
            internal int Index;
            internal float At;
            internal int ScreenWidth;
            internal int ScreenHeight;
            internal Rect? Area;
            internal string Phase;
            internal LabelReading[] Labels;
            internal Texture2D Texture;
            internal int CaptureIndex;
            internal float CaptureAt;
            internal string CaptureUtc;
            internal int CaptureScreenWidth;
            internal int CaptureScreenHeight;
            internal int TextureWidth;
            internal int TextureHeight;

            internal bool BothLabelsDrawn
            {
                get
                {
                    bool complete = Labels.Length == 2 && Labels.Select(item => item.Kind).Distinct().Count() == 2 &&
                        Labels.All(item => item.Alpha >= MinimumAlpha && item.NonSpace > 0 && item.Visible == item.NonSpace);
                    long[] levels = Labels.SelectMany(item => Numbers(item.Text)).Distinct().ToArray();
                    return complete && levels.Contains(SharedLevel) && levels.Contains(PersonalLevel);
                }
            }

            public void Dispose()
            {
                if (Texture != null) UnityEngine.Object.Destroy(Texture);
                Texture = null;
            }

            internal void Capture(float requested)
            {
                CaptureIndex = Time.frameCount;
                CaptureAt = Time.realtimeSinceStartup - requested;
                CaptureUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                CaptureScreenWidth = Screen.width;
                CaptureScreenHeight = Screen.height;
                Texture = ScreenCapture.CaptureScreenshotAsTexture();
                TextureWidth = Texture.width;
                TextureHeight = Texture.height;
            }
        }

        // One active graphic of the effect this frame: the kind it belongs to (the effect's child it sits under), its screen
        // box and its alpha after CanvasRenderer and CanvasGroups; Label is set for a level text.
        sealed class Part
        {
            internal string Kind;
            internal string Path;
            internal Rect Box;
            internal float Alpha;
            internal TMP_Text Label;

            internal bool Visible => Alpha >= MinimumAlpha;

            // Round-trip numbers, for the same-frame comparison around the invalid calls.
            internal string Exact => string.Format(CultureInfo.InvariantCulture, "{0} {1:R} {2:R} {3:R} {4:R} a={5:R} '{6}'", Path, Box.xMin,
                Box.yMin, Box.xMax, Box.yMax, Alpha, Label != null ? Label.text : string.Empty);

            public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0} a={1:F3} ({2:F1},{3:F1})-({4:F1},{5:F1})",
                Path, Alpha, Box.xMin, Box.yMin, Box.xMax, Box.yMax);
        }

        // Raw readability of one level text this frame: TMP's chosen size, that size in screen pixels, and the visible glyphs'
        // ink heights in pixels, digits apart because the level is the number.
        sealed class LabelReading
        {
            internal string Kind;
            internal string Text;
            internal float Alpha;
            internal float FontSize;
            internal float FontMin;
            internal float FontMax;
            internal float Scale;
            internal int Visible;
            internal int NonSpace;
            internal float GlyphMin = float.NaN;
            internal float GlyphMax = float.NaN;
            internal float DigitMax = float.NaN;
            internal bool? BoxInside;

            internal float FontPixels => FontSize * Scale;

            internal static LabelReading Of(Part part, Rect? area)
            {
                TMP_Text text = part.Label;
                var reading = new LabelReading
                {
                    Kind = part.Kind,
                    Text = text.text,
                    Alpha = part.Alpha,
                    FontSize = text.fontSize,
                    FontMin = text.fontSizeMin,
                    FontMax = text.fontSizeMax,
                    Scale = text.transform.lossyScale.y,
                    NonSpace = text.text.Count(character => !char.IsWhiteSpace(character)),
                };
                TMP_TextInfo info = text.textInfo;
                for (int i = 0; info != null && i < info.characterCount; i++)
                {
                    TMP_CharacterInfo character = info.characterInfo[i];
                    if (!character.isVisible) continue;
                    float height = ((Vector2)text.transform.TransformPoint(character.topLeft) -
                        (Vector2)text.transform.TransformPoint(character.bottomLeft)).magnitude;
                    reading.Visible++;
                    reading.GlyphMin = float.IsNaN(reading.GlyphMin) ? height : Mathf.Min(reading.GlyphMin, height);
                    reading.GlyphMax = float.IsNaN(reading.GlyphMax) ? height : Mathf.Max(reading.GlyphMax, height);
                    if (char.IsDigit(character.character))
                    {
                        reading.DigitMax = float.IsNaN(reading.DigitMax) ? height : Mathf.Max(reading.DigitMax, height);
                    }
                }
                if (area.HasValue) reading.BoxInside = GuildGrowthEffectIndependentProbe.Inside(CornerBox(text.rectTransform), area.Value);
                return reading;
            }

            internal string Row(int frame, float at) => string.Format(CultureInfo.InvariantCulture,
                "{0},{1:F4},{2},{3},{4:F4},{5:F2},{6:F2},{7:F2},{8:F5},{9:F2},{10},{11},{12:F2},{13:F2},{14:F2},{15}", frame, at, Kind, Text,
                Alpha, FontSize, FontMin, FontMax, Scale, FontPixels, Visible, NonSpace, GlyphMin, GlyphMax, DigitMax,
                BoxInside.HasValue ? (BoxInside.Value ? "1" : "0") : "");

            public override string ToString() => string.Format(CultureInfo.InvariantCulture,
                "'{0}' a={1:F3} font {2:F2} ({3:F2}-{4:F2}) x scale {5:F4} = {6:F2} px, glyphs {7}/{8} ink {9:F2}-{10:F2} px, digit {11:F2} px, " +
                "label box inside {12}", Text, Alpha, FontSize, FontMin, FontMax, Scale, FontPixels, Visible, NonSpace, GlyphMin, GlyphMax,
                DigitMax, BoxInside.HasValue ? BoxInside.Value.ToString() : "n/a");
        }

        // One scenario's raw rows and its failed or only observed checks.
        sealed class ScenarioLog
        {
            internal readonly List<string> Parts = new List<string> { PartsHeader };
            internal readonly List<string> Labels = new List<string> { LabelsHeader };
            internal readonly List<string> Captures = new List<string> { CapturesHeader };
            internal readonly Tally Failed = new Tally();
            internal readonly Tally Observed = new Tally();
            internal readonly Dictionary<string, float> LastDrawn = new Dictionary<string, float>();
            internal readonly SortedSet<long> Shown = new SortedSet<long>();
            internal bool BothTogether;
            internal bool BothAfterChange;
            internal int Frames;
            internal Rect? AfterArea;
            internal int AfterWidth;
            internal int AfterHeight;
            internal int ChangeFrame = -1;
            internal float ChangedAt = float.NaN;
        }

        // A count and the first examples per check, so a fault that lasts many frames reads as one line with its raw count.
        sealed class Tally
        {
            readonly Dictionary<string, (int Count, List<string> Examples)> _checks = new Dictionary<string, (int Count, List<string> Examples)>();

            internal void Add(string check, string example)
            {
                if (!_checks.TryGetValue(check, out (int Count, List<string> Examples) entry)) entry = (0, new List<string>());
                if (entry.Examples.Count < ExamplesPerCheck) entry.Examples.Add(example);
                _checks[check] = (entry.Count + 1, entry.Examples);
            }

            internal IEnumerable<string> Lines(string label) => _checks.Select(item =>
                $"{label}: {item.Key} x{item.Value.Count}, e.g. {string.Join(" | ", item.Value.Examples)}");
        }
    }
}
