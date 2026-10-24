using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Independent check of the files GuildGrowthEffectIndependentAreaSceneTests writes (effect-layout-independent-02,
    // verifying the capture-time fix Task task_d36a46b69604). It runs in a later batch run and only reads the folder the
    // runner names in DAWNHOLDER_AREA_EVIDENCE_DIR. Each PNG is judged by its own bytes (IHDR size, SHA-256) against the
    // captures row that lists it, the parts and labels rows of that frame and the area-record notes. A changed-state PNG has
    // to be a frame after the change frame, at the target Screen and area, drawing both whole level texts, and the flags its
    // row claims have to follow from the raw columns. The area box comes from the recorded canvas box and the caller's
    // normalized area, never from the product's layout. Without the variable every case is ignored, which the execution
    // plan counts as unproved, never as a pass; a named folder with a missing file or note fails.
    public sealed class GuildGrowthEffectAreaEvidenceTests
    {
        const string FolderVariable = "DAWNHOLDER_AREA_EVIDENCE_DIR";
        const string Tag = "[GuildGrowthEffectAreaEvidence] ";
        const string RecordFile = "area-record.csv";
        // Part box edges are written with 3 decimals; the canvas box and the areas round-trip.
        const double BoxTolerance = 0.001;
        // One 8-bit colour step (1/255) as the 4-decimal alpha column writes it. A value written as exactly this may sit a
        // hair under the step; it counts as drawn, the stricter side for containment.
        const double DrawnAlpha = 0.0039;
        const long SharedLevel = 5;
        const long PersonalLevel = 8;
        const string ResizeSize = "1920x1080-to-1280x720";

        static readonly string[] Sizes = { "1920x1080", "1280x720" };
        // Each size's scenarios in the capture's order. Only default and panel run without a midway call.
        static readonly string[] Scenarios = { "default", "root", "panel", "update", "restore", "tiny" };
        static readonly string[] Unchanged = { "default", "panel" };
        // The scenarios that also keep a changed-state PNG.
        static readonly string[] ChangedState = { "update", "restore", "resize" };
        static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        static readonly Regex Hashed = new Regex("^(?<path>.+?) (?<bytes>\\d+) bytes sha256 (?<sha>[0-9a-f]{64})(?: |$)");
        static readonly Regex Box = new Regex("^\\(x (?<x>\\S+) y (?<y>\\S+) w (?<w>\\S+) h (?<h>\\S+)\\)$");
        static readonly Regex Measured = new Regex("^\\(x [^)]*\\) / (?<area>\\(x [^)]*\\)) \\(measured, the verdict area\\)$");
        static readonly Regex Midway = new Regex("^at \\S+ s frame (?<frame>\\d+), area now (?<area>.+)$");
        static readonly Regex Kept = new Regex(" texture (?<texture>\\d+x\\d+) capture Screen (?<screen>\\d+x\\d+) frame (?<frame>\\d+) " +
            "at .+, observed frame (?<observed>\\d+) at \\S+ s area (?<area>.+) phase (?<phase>\\S+); save Screen ");
        static readonly Regex Digits = new Regex("\\d+");

        [Test]
        public void UpdateAfter1920x1080_PngRowsShowTheChangedRootArea() =>
            AfterChange("1920x1080", "update", "1920x1080", "1920x1080 guild root box / normalized");

        [Test]
        public void UpdateAfter1280x720_PngRowsShowTheChangedRootArea() =>
            AfterChange("1280x720", "update", "1280x720", "1280x720 guild root box / normalized");

        [Test]
        public void RestoreAfter1920x1080_PngRowsShowTheRestoredNullArea() => AfterChange("1920x1080", "restore", "1920x1080", null);

        [Test]
        public void RestoreAfter1280x720_PngRowsShowTheRestoredNullArea() => AfterChange("1280x720", "restore", "1280x720", null);

        // The resize keeps the root measured at 1920x1080 as its area while the Game view goes to 1280x720, so its after
        // frame is boxed with the canvas the same run recorded at 1280x720.
        [Test]
        public void ResizeAfter1920x1080To1280x720_PngRowsShowTheTargetScreen() =>
            AfterChange(ResizeSize, "resize", "1280x720", "resize 1920x1080 guild root box / normalized");

        // Every scenario's original PNG is still its brightest frame (the capture's strict maximum of the lower level text
        // alpha over frames with two level texts), joined to the captures, parts and labels rows of that frame and Screen.
        [Test]
        public void OriginalPngs_AreTheBrightestFramesJoinedToTheirRows()
        {
            Evidence evidence = Evidence.Open();
            var failures = new List<string>();
            foreach (var (size, name) in AllScenarios())
            {
                string prefix = size + "-" + name;
                string file = prefix + "-labels.png";
                Table captures = evidence.Csv(prefix + "-captures.csv");
                int expectedRows = ChangedState.Contains(name) ? 2 : 1;
                Check(failures, captures.Rows.Length == expectedRows,
                    $"{prefix}-captures.csv has {captures.Rows.Length} rows, not {expectedRows}");
                string[] capture = captures.Single("file", file);
                int frame = captures.Int(capture, "frame");
                string screen = captures.Get(capture, "screen");
                string area = captures.Get(capture, "area");
                string phase = captures.Get(capture, "phase");
                string png = evidence.PngSize(file);
                Report($"{file}: frame {frame} screen {screen} PNG {png} area {area} phase {phase}");

                Check(failures, captures.Int(capture, "capture_frame") == frame,
                    $"{file}: capture frame {captures.Get(capture, "capture_frame")} differs from frame {frame}");
                foreach (string column in new[] { "capture_screen", "texture" })
                {
                    Check(failures, captures.Get(capture, column) == screen,
                        $"{file}: {column} is {captures.Get(capture, column)}, the row's Screen is {screen}");
                }
                Check(failures, png == screen, $"{file}: the PNG's own IHDR size is {png}, the row's Screen is {screen}");
                bool unchanged = Unchanged.Contains(name);
                Check(failures, unchanged ? phase == "unchanged" : phase == "before-change" || phase == "after-change",
                    $"{file}: phase {phase} for a scenario {(unchanged ? "without" : "with")} a midway call");
                failures.AddRange(evidence.KeptNote(file, png, captures, capture).Select(item => file + ": " + item));

                Edges? box = area == "null" ? (Edges?)null : evidence.AreaBox(screen, area);
                failures.AddRange(FrameParts(evidence.Csv(prefix + "-parts.csv"), frame, screen, area, phase, box)
                    .Select(item => file + ": " + item));
                Table labels = evidence.Csv(prefix + "-labels.csv");
                string[][] kept = labels.Where("frame", Text(frame));
                failures.AddRange(FrameLabels(labels, kept, frame, screen, area, phase).Select(item => file + ": " + item));
                if (kept.Length != 2) continue;
                double keptAlpha = MinAlpha(labels, kept);
                string[] brighter = labels.Rows.GroupBy(row => labels.Get(row, "frame")).Select(group => group.ToArray())
                    .Where(rows => rows.Length == 2 && MinAlpha(labels, rows) > keptAlpha).Select(rows => labels.Get(rows[0], "frame"))
                    .ToArray();
                Check(failures, brighter.Length == 0,
                    $"{file}: frames [{string.Join(", ", brighter)}] draw both level texts brighter than the kept frame {frame}");
            }
            CollectionAssert.IsEmpty(failures, "every original PNG is its scenario's brightest frame and matches its rows");
        }

        // The folder holds exactly the expected PNG and CSV files, and every file the record lists with a size and SHA-256
        // lies in this folder with those bytes.
        [Test]
        public void EvidenceFolder_HoldsExactlyTheRecordedFiles()
        {
            Evidence evidence = Evidence.Open();
            var failures = new List<string>();
            string[] prefixes = AllScenarios().Select(item => item.Size + "-" + item.Name).ToArray();
            string[] pngs = prefixes.Select(prefix => prefix + "-labels.png").Concat(AllScenarios()
                .Where(item => ChangedState.Contains(item.Name)).Select(item => item.Size + "-" + item.Name + "-after-labels.png")).ToArray();
            string[] csvs = prefixes.SelectMany(prefix => new[] { "-parts.csv", "-labels.csv", "-captures.csv" }
                .Select(suffix => prefix + suffix)).ToArray();
            string[] present = Directory.GetFiles(evidence.Folder).Select(item => Path.GetFileName(item)).ToArray();
            Report("folder files: " + string.Join(", ", present.OrderBy(item => item, StringComparer.Ordinal)));
            foreach (var (extension, expected) in new[] { (".png", pngs), (".csv", csvs.Concat(new[] { RecordFile }).ToArray()) })
            {
                string[] found = present.Where(item => string.Equals(Path.GetExtension(item), extension, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                string[] missing = expected.Except(found, StringComparer.Ordinal).ToArray();
                string[] extra = found.Except(expected, StringComparer.Ordinal).ToArray();
                Check(failures, missing.Length == 0 && extra.Length == 0,
                    $"{extension} files: missing [{string.Join(", ", missing)}], unexpected [{string.Join(", ", extra)}]");
            }

            var recorded = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string value in evidence.Values)
            {
                Match hashed = Hashed.Match(value);
                if (!hashed.Success) continue;
                string path = hashed.Groups["path"].Value;
                string name = Path.GetFileName(path);
                Check(failures, SameFolder(Path.GetDirectoryName(path), evidence.Folder),
                    $"{name} was recorded under {Path.GetDirectoryName(path)}, not the named folder {evidence.Folder}");
                recorded[name] = recorded.TryGetValue(name, out int count) ? count + 1 : 1;
                if (File.Exists(Path.Combine(evidence.Folder, name))) failures.AddRange(evidence.Matches(name, hashed));
                else failures.Add($"{name} is recorded but not in the folder");
            }
            string[] written = pngs.Concat(csvs).ToArray();
            string[] unrecorded = written.Where(name => !recorded.ContainsKey(name)).ToArray();
            string[] repeated = recorded.Where(item => item.Value != 1).Select(item => $"{item.Key} x{item.Value}").ToArray();
            string[] foreign = recorded.Keys.Except(written, StringComparer.Ordinal).ToArray();
            Report($"{recorded.Count} files recorded with size and SHA-256, {written.Length} expected");
            Check(failures, unrecorded.Length == 0 && repeated.Length == 0 && foreign.Length == 0,
                $"record: unrecorded [{string.Join(", ", unrecorded)}], repeated [{string.Join(", ", repeated)}], " +
                $"unexpected [{string.Join(", ", foreign)}]");
            foreach (string failure in evidence.Notes("failure")) Report("the capture recorded failure: " + failure);
            CollectionAssert.IsEmpty(failures, "the evidence folder holds exactly the recorded files with their recorded bytes");
        }

        // A changed-state PNG and the rows it is joined to: after the change frame, at the target Screen and area, both
        // whole level texts drawn, every drawn part inside the area and the two kinds apart, and the brightest such frame.
        static void AfterChange(string size, string name, string targetScreen, string rootNote)
        {
            Evidence evidence = Evidence.Open();
            string label = size + " " + name;
            string prefix = size + "-" + name;
            string file = prefix + "-after-labels.png";
            string targetArea = rootNote == null ? "null" : evidence.MeasuredArea(rootNote);
            var failures = new List<string>();

            Table captures = evidence.Csv(prefix + "-captures.csv");
            string[] capture = captures.Single("file", file);
            int frame = captures.Int(capture, "frame");
            int changeFrame = captures.Int(capture, "change_frame");
            string screen = captures.Get(capture, "screen");
            string area = captures.Get(capture, "area");
            string phase = captures.Get(capture, "phase");
            string png = evidence.PngSize(file);
            Report($"{file}: frame {frame} change frame {changeFrame} screen {screen} PNG {png} area {area} phase {phase}; " +
                $"target {targetScreen} {targetArea}");

            Check(failures, phase == "after-change", $"phase is {phase}, not after-change");
            Check(failures, changeFrame >= 0 && frame > changeFrame, $"frame {frame} is not after change frame {changeFrame}");
            Check(failures, captures.Double(capture, "capture_real_from_request") > captures.Double(capture, "change_real_from_request"),
                $"capture time {captures.Get(capture, "capture_real_from_request")} s is not after change time " +
                $"{captures.Get(capture, "change_real_from_request")} s");
            Check(failures, captures.Int(capture, "capture_frame") == frame,
                $"capture frame {captures.Get(capture, "capture_frame")} differs from frame {frame}");
            foreach (string column in new[] { "screen", "capture_screen", "texture", "target_screen" })
            {
                Check(failures, captures.Get(capture, column) == targetScreen,
                    $"{column} is {captures.Get(capture, column)}, the target is {targetScreen}");
            }
            Check(failures, png == targetScreen, $"the PNG's own IHDR size is {png}, the target is {targetScreen}");
            foreach (string column in new[] { "area", "target_area" })
            {
                Check(failures, captures.Get(capture, column) == targetArea,
                    $"{column} is {captures.Get(capture, column)}, the target is {targetArea}");
            }

            Match midway = evidence.Note(label + " midway", Midway);
            Check(failures, midway.Groups["frame"].Value == Text(changeFrame) && midway.Groups["area"].Value == targetArea,
                $"the midway note '{midway.Value}' differs from change frame {changeFrame} and area {targetArea}");
            if (name != "resize")
            {
                string change = evidence.Note(label + " change");
                Check(failures, change == targetArea + " accepted True", $"the change note is '{change}'");
            }
            failures.AddRange(evidence.KeptNote(file, png, captures, capture));

            Edges? box = targetArea == "null" ? (Edges?)null : evidence.AreaBox(targetScreen, targetArea);
            failures.AddRange(FrameParts(evidence.Csv(prefix + "-parts.csv"), frame, screen, area, phase, box));
            Table labels = evidence.Csv(prefix + "-labels.csv");
            string[][] kept = labels.Where("frame", Text(frame));
            failures.AddRange(FrameLabels(labels, kept, frame, screen, area, phase));
            bool both = BothDrawn(labels, kept);
            Check(failures, both, $"frame {frame} does not draw both whole level texts");

            Flag(failures, captures, capture, "after_change_frame", phase == "after-change" && frame > changeFrame);
            Flag(failures, captures, capture, "target_screen_reached", screen == captures.Get(capture, "target_screen"));
            Flag(failures, captures, capture, "target_area_applied", area == captures.Get(capture, "target_area"));
            Flag(failures, captures, capture, "both_labels_drawn", both);

            if (both)
            {
                double keptAlpha = MinAlpha(labels, kept);
                foreach (IGrouping<string, string[]> other in labels.Rows.GroupBy(row => labels.Get(row, "frame")))
                {
                    string[][] rows = other.ToArray();
                    bool qualifies = int.Parse(other.Key, CultureInfo.InvariantCulture) > changeFrame && rows.All(row =>
                        labels.Get(row, "phase") == "after-change" && labels.Get(row, "screen") == targetScreen &&
                        labels.Get(row, "area") == targetArea) && BothDrawn(labels, rows);
                    if (qualifies && MinAlpha(labels, rows) > keptAlpha)
                    {
                        failures.Add(string.Format(CultureInfo.InvariantCulture, "frame {0} also shows the target state and draws " +
                            "brighter ({1:F4} > {2:F4})", other.Key, MinAlpha(labels, rows), keptAlpha));
                    }
                }
            }
            CollectionAssert.IsEmpty(failures, $"{file} shows the {label} target state");
        }

        // The parts rows of one frame: present, on the claimed Screen, area and phase, with both kinds drawn. Under an area
        // every drawn part lies inside its box and the two kinds' drawn boxes do not overlap; without one no row claims inside.
        static List<string> FrameParts(Table parts, int frame, string screen, string area, string phase, Edges? box)
        {
            var failures = new List<string>();
            string[][] rows = parts.Where("frame", Text(frame));
            Check(failures, rows.Length > 0, $"no parts rows at frame {frame}");
            Check(failures, rows.All(row => parts.Get(row, "screen") == screen && parts.Get(row, "area") == area &&
                parts.Get(row, "phase") == phase), $"a parts row at frame {frame} differs from Screen {screen}, area {area} or phase {phase}");
            string[][] drawn = rows.Where(row => parts.Double(row, "alpha") >= DrawnAlpha).ToArray();
            string[] kinds = drawn.Select(row => parts.Get(row, "kind")).Distinct().ToArray();
            Check(failures, kinds.Length == 2, $"frame {frame} draws parts of [{string.Join(", ", kinds)}], not of both kinds");
            if (!box.HasValue)
            {
                Check(failures, rows.All(row => parts.Get(row, "inside") == ""), $"frame {frame} has no area but a row claims inside");
                return failures;
            }
            foreach (string[] row in drawn)
            {
                Edges part = PartEdges(parts, row);
                bool inside = part.Inside(box.Value, BoxTolerance);
                if (inside && parts.Get(row, "inside") == "1") continue;
                failures.Add($"frame {frame} {parts.Get(row, "path")} {part} against the area box {box.Value}: inside {inside}, " +
                    $"claimed '{parts.Get(row, "inside")}'");
            }
            Edges[] unions = drawn.Select(row => (Kind: parts.Get(row, "kind"), Bounds: PartEdges(parts, row)))
                .Where(item => item.Bounds.HasSize).GroupBy(item => item.Kind).Select(group => Edges.Union(group.Select(item => item.Bounds)))
                .ToArray();
            if (unions.Length == 2)
            {
                double width = Math.Min(unions[0].XMax, unions[1].XMax) - Math.Max(unions[0].XMin, unions[1].XMin);
                double height = Math.Min(unions[0].YMax, unions[1].YMax) - Math.Max(unions[0].YMin, unions[1].YMin);
                Check(failures, width <= BoxTolerance || height <= BoxTolerance,
                    $"frame {frame}: the two kinds' drawn boxes {unions[0]} and {unions[1]} overlap");
            }
            return failures;
        }

        // The labels rows of one frame: one per kind, on the claimed Screen, area and phase.
        static List<string> FrameLabels(Table labels, string[][] rows, int frame, string screen, string area, string phase)
        {
            var failures = new List<string>();
            Check(failures, rows.Length == 2, $"frame {frame} has {rows.Length} labels rows, not 2");
            Check(failures, rows.All(row => labels.Get(row, "screen") == screen && labels.Get(row, "area") == area &&
                labels.Get(row, "phase") == phase), $"a labels row at frame {frame} differs from Screen {screen}, area {area} or phase {phase}");
            return failures;
        }

        // Two level texts of different kinds, each drawn with every non-space character, showing both requested levels.
        static bool BothDrawn(Table labels, string[][] rows) =>
            rows.Length == 2 && rows.Select(row => labels.Get(row, "kind")).Distinct().Count() == 2 &&
            rows.All(row => labels.Double(row, "alpha") >= DrawnAlpha && labels.Int(row, "non_space_chars") > 0 &&
                labels.Int(row, "visible_chars") == labels.Int(row, "non_space_chars")) &&
            new[] { SharedLevel, PersonalLevel }.All(level => rows.SelectMany(row => Numbers(labels.Get(row, "text"))).Contains(level));

        static double MinAlpha(Table labels, string[][] rows) => rows.Min(row => labels.Double(row, "alpha"));

        // A claimed 0/1 flag of a captures row has to follow from the raw columns of the same row and frame.
        static void Flag(List<string> failures, Table captures, string[] capture, string column, bool raw)
        {
            string claimed = captures.Get(capture, column);
            string expected = raw ? "1" : "0";
            Check(failures, claimed == expected, $"{column} claims {claimed}, the raw columns give {expected}");
        }

        static void Check(List<string> failures, bool holds, string failure)
        {
            if (!holds) failures.Add(failure);
        }

        static IEnumerable<(string Size, string Name)> AllScenarios() =>
            Sizes.SelectMany(size => Scenarios.Select(name => (size, name))).Concat(new[] { (ResizeSize, "resize") });

        static Edges PartEdges(Table parts, string[] row) =>
            new Edges(parts.Double(row, "x_min"), parts.Double(row, "y_min"), parts.Double(row, "x_max"), parts.Double(row, "y_max"));

        // "(x .. y .. w .. h ..)" as the capture describes a Rect.
        static (double X, double Y, double Width, double Height) Described(string text)
        {
            Match match = Box.Match(text);
            Assert.IsTrue(match.Success, "a described box: " + text);
            return (Number(match.Groups["x"].Value), Number(match.Groups["y"].Value), Number(match.Groups["w"].Value),
                Number(match.Groups["h"].Value));
        }

        static bool SameFolder(string a, string b) => string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'),
            Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

        static double Number(string text) => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

        static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

        static IEnumerable<long> Numbers(string text) =>
            Digits.Matches(text ?? string.Empty).Cast<Match>().Select(match => long.Parse(match.Value, CultureInfo.InvariantCulture));

        static int BigEndian(byte[] bytes, int at) => (bytes[at] << 24) | (bytes[at + 1] << 16) | (bytes[at + 2] << 8) | bytes[at + 3];

        static string Sha256(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create()) return string.Concat(sha.ComputeHash(bytes).Select(item => item.ToString("x2")));
        }

        // RFC 4180 rows: a quoted field may hold commas, doubled quotes and line breaks.
        static List<string[]> ReadCsv(string text)
        {
            var rows = new List<string[]>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (quoted)
                {
                    if (character != '"') field.Append(character);
                    else if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else quoted = false;
                }
                else if (character == '"') quoted = true;
                else if (character == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (character == '\n')
                {
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row.ToArray());
                    row.Clear();
                }
                else if (character != '\r') field.Append(character);
            }
            if (field.Length != 0 || row.Count != 0)
            {
                row.Add(field.ToString());
                rows.Add(row.ToArray());
            }
            return rows;
        }

        // Each line goes to the test result output and the Editor log, so the files behind every verdict are on record.
        static void Report(string line)
        {
            TestContext.Out.WriteLine(Tag + line);
            Debug.Log(Tag + line);
        }

        // The named folder's files, each read once and reported with its size and SHA-256, and the area-record notes.
        sealed class Evidence
        {
            readonly Dictionary<string, byte[]> _bytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            readonly Dictionary<string, Table> _tables = new Dictionary<string, Table>(StringComparer.Ordinal);
            readonly Table _record;

            Evidence(string folder)
            {
                Folder = folder;
                _record = Csv(RecordFile);
            }

            internal string Folder { get; }

            internal IEnumerable<string> Values => _record.Rows.Select(row => _record.Get(row, "value"));

            internal static Evidence Open()
            {
                string folder = Environment.GetEnvironmentVariable(FolderVariable);
                if (string.IsNullOrEmpty(folder))
                {
                    Assert.Ignore($"reads the capture folder only when the runner names it in {FolderVariable}; ignored is unproved, not a pass");
                }
                Report($"{FolderVariable} = {folder} (full path {Path.GetFullPath(folder)})");
                Assert.IsTrue(Directory.Exists(folder), "the named evidence folder exists: " + folder);
                return new Evidence(folder);
            }

            internal byte[] Bytes(string name)
            {
                if (_bytes.TryGetValue(name, out byte[] bytes)) return bytes;
                string path = Path.Combine(Folder, name);
                Assert.IsTrue(File.Exists(path), "the evidence file exists: " + path);
                bytes = File.ReadAllBytes(path);
                Report($"read {name} {bytes.Length} bytes sha256 {Sha256(bytes)}");
                _bytes[name] = bytes;
                return bytes;
            }

            internal Table Csv(string name)
            {
                if (_tables.TryGetValue(name, out Table table)) return table;
                table = new Table(name, ReadCsv(new UTF8Encoding(false).GetString(Bytes(name))));
                _tables[name] = table;
                return table;
            }

            internal string[] Notes(string key) =>
                _record.Rows.Where(row => _record.Get(row, "key") == key).Select(row => _record.Get(row, "value")).ToArray();

            internal string Note(string key)
            {
                string[] values = Notes(key);
                Assert.AreEqual(1, values.Length, $"{RecordFile} has one note '{key}'");
                return values[0];
            }

            internal Match Note(string key, Regex format)
            {
                string value = Note(key);
                Match match = format.Match(value);
                Assert.IsTrue(match.Success, $"the note '{key}' reads as expected: {value}");
                return match;
            }

            // The normalized Guild window root the capture measured and passed as the area.
            internal string MeasuredArea(string key) => Note(key, Measured).Groups["area"].Value;

            // A normalized area on the effect canvas this run recorded at that Screen size, in screen pixels.
            internal Edges AreaBox(string screen, string area)
            {
                var canvas = Described(Note(screen + " effect canvas box"));
                var fraction = Described(area);
                double xMin = canvas.X + fraction.X * canvas.Width;
                double yMin = canvas.Y + fraction.Y * canvas.Height;
                return new Edges(xMin, yMin, xMin + fraction.Width * canvas.Width, yMin + fraction.Height * canvas.Height);
            }

            // The width and height a PNG's own IHDR chunk gives.
            internal string PngSize(string name)
            {
                byte[] bytes = Bytes(name);
                Assert.IsTrue(bytes.Length >= 24 && bytes.Take(8).SequenceEqual(PngSignature) && Encoding.ASCII.GetString(bytes, 12, 4) == "IHDR",
                    name + " starts with the PNG signature and its IHDR chunk");
                return BigEndian(bytes, 16) + "x" + BigEndian(bytes, 20);
            }

            // The file's actual size and SHA-256 against the ones recorded for it.
            internal List<string> Matches(string name, Match hashed)
            {
                byte[] bytes = Bytes(name);
                string sha = Sha256(bytes);
                var failures = new List<string>();
                Check(failures, hashed.Groups["bytes"].Value == Text(bytes.Length) && hashed.Groups["sha"].Value == sha,
                    $"{name} is {bytes.Length} bytes sha256 {sha}, recorded {hashed.Groups["bytes"].Value} bytes sha256 {hashed.Groups["sha"].Value}");
                return failures;
            }

            // A PNG's "kept frame" note: its recorded file, size and SHA-256 are the PNG's own, and its texture, capture Screen,
            // capture frame, observed frame, area and phase are those of the captures row.
            internal List<string> KeptNote(string file, string png, Table captures, string[] capture)
            {
                string value = Note("kept frame " + Path.GetFileNameWithoutExtension(file));
                Match hashed = Hashed.Match(value);
                Assert.IsTrue(hashed.Success, $"the kept note of {file} records its size and SHA-256: {value}");
                Match kept = Kept.Match(value);
                Assert.IsTrue(kept.Success, $"the kept note of {file} records its capture: {value}");
                var failures = new List<string>();
                Check(failures, Path.GetFileName(hashed.Groups["path"].Value) == file, $"the kept note names {hashed.Groups["path"].Value}");
                failures.AddRange(Matches(file, hashed));
                Check(failures, kept.Groups["texture"].Value == png && captures.Get(capture, "texture") == png,
                    $"texture: note {kept.Groups["texture"].Value}, row {captures.Get(capture, "texture")}, PNG {png}");
                foreach (var (group, column) in new[] { ("screen", "capture_screen"), ("frame", "capture_frame"), ("observed", "frame"),
                    ("area", "area"), ("phase", "phase") })
                {
                    Check(failures, kept.Groups[group].Value == captures.Get(capture, column),
                        $"the kept note's {group} {kept.Groups[group].Value} differs from the row's {column} {captures.Get(capture, column)}");
                }
                return failures;
            }
        }

        // One CSV with a header row; every row has the header's columns.
        sealed class Table
        {
            readonly string _name;
            readonly Dictionary<string, int> _columns = new Dictionary<string, int>(StringComparer.Ordinal);

            internal Table(string name, List<string[]> rows)
            {
                _name = name;
                Assert.IsTrue(rows.Count > 0, name + " has a header row");
                string[] header = rows[0];
                for (int i = 0; i < header.Length; i++) _columns[header[i]] = i;
                Rows = rows.Skip(1).ToArray();
                Assert.IsTrue(Rows.All(row => row.Length == header.Length), $"{name}: every row has the header's {header.Length} columns");
            }

            internal string[][] Rows { get; }

            internal string Get(string[] row, string column)
            {
                if (!_columns.TryGetValue(column, out int index)) Assert.Fail($"{_name} has no column {column}");
                return row[index];
            }

            internal int Int(string[] row, string column) => int.Parse(Get(row, column), NumberStyles.Integer, CultureInfo.InvariantCulture);

            internal double Double(string[] row, string column) => Number(Get(row, column));

            internal string[][] Where(string column, string value) => Rows.Where(row => Get(row, column) == value).ToArray();

            internal string[] Single(string column, string value)
            {
                string[][] rows = Where(column, value);
                Assert.AreEqual(1, rows.Length, $"{_name} has one row with {column} {value}");
                return rows[0];
            }
        }

        // A box by its edges in screen pixels, kept in double so the recorded values are not rounded again.
        readonly struct Edges
        {
            internal readonly double XMin;
            internal readonly double YMin;
            internal readonly double XMax;
            internal readonly double YMax;

            internal Edges(double xMin, double yMin, double xMax, double yMax)
            {
                XMin = xMin;
                YMin = yMin;
                XMax = xMax;
                YMax = yMax;
            }

            internal bool HasSize => XMax > XMin && YMax > YMin;

            internal bool Inside(Edges outer, double tolerance) => XMin >= outer.XMin - tolerance && YMin >= outer.YMin - tolerance &&
                XMax <= outer.XMax + tolerance && YMax <= outer.YMax + tolerance;

            internal static Edges Union(IEnumerable<Edges> all)
            {
                Edges[] boxes = all.ToArray();
                return new Edges(boxes.Min(box => box.XMin), boxes.Min(box => box.YMin), boxes.Max(box => box.XMax), boxes.Max(box => box.YMax));
            }

            public override string ToString() =>
                string.Format(CultureInfo.InvariantCulture, "({0:F3},{1:F3})-({2:F3},{3:F3})", XMin, YMin, XMax, YMax);
        }
    }
}
