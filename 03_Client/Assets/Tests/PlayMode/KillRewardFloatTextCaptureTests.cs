using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Dawnholder.Client.Combat;
using NUnit.Framework;
using Shared.GameData;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Real-screen capture of the kill reward text (01_Phases/goals/2026-10-30-kill-reward-float-text/goal.md 완료조건 4) for
    // the user's look check and the independent verifier. HuntingGround is entered through the scripted loopback peer of
    // MapEntryPlayFixture, not GameServer: Town with a gold 25, material 2 snapshot, then the move to map 1 with the
    // client's requery answered by the same values. One monster near the player dies and a +10 gold, +1 material
    // snapshot follows; at 1920x1080 and then 1280x720, each with its own kill, the frames 0.5 s (main), 1.0 s and 1.5 s
    // after the text is first visible are saved as PNG. Each frame's record holds what was drawn: every line's text,
    // place, screen bounds, colour, alpha, font and outline, and the dead monster. The text is found the black-box way of
    // KillRewardFloatTextProbe, never by a product type name. Only the capture's usefulness is asserted (both sizes, the
    // two lines, main frames inside the screen); colour, outline and legibility are judged from the frames. Runs only
    // when the capture runner names its output folder and writes the GameView permit, and needs the Editor's Game view
    // (no -batchmode).
    public sealed class KillRewardFloatTextCaptureTests
    {
        const string OutputVariable = "DAWNHOLDER_UI_CAPTURE_DIR";
        const string PermitVariable = "DAWNHOLDER_GAMEVIEW_PERMIT";
        // The runner writes the permit after it has identified the Unity process it started.
        const float PermitWaitSeconds = 120f;
        const float SizeWaitSeconds = 3f;
        const int SettleFrames = 10;
        const int MaxHp = 100;
        const byte Town = 0;
        const byte HuntingGround = 1;
        const float PlayerSpawnX = 2f;
        // Three units right of the player's spawn, the pretest's NearX, so the monster stands inside the camera view.
        const float EnemyX = 5f;
        const int BaseGold = 25;
        const int BaseMaterial = 2;
        const int KillGold = 10;
        const int KillMaterial = 1;
        const float QuerySeconds = 3f;
        const float AppearSeconds = 3f;
        // The pretest's GoneSeconds plus its watch margin.
        const float GoneSeconds = 5.5f;
        // The pretest's WindowExpirySeconds: the 1.0 s pairing window plus a margin, counted once the death is applied.
        const float WindowExpirySeconds = 1.2f;
        const string Synchronized = "서버 확인 완료";
        const string GoldTen = "+금화 10";
        const string MaterialOne = "+재료 1";

        static readonly (int Width, int Height)[] Sizes = { (1920, 1080), (1280, 720) };
        // Seconds after the text is first visible, and the frame's name; "main" is the user's look-check frame.
        static readonly (float At, string Name)[] Shots = { (0.5f, "main"), (1.0f, "1.0s"), (1.5f, "1.5s") };

        readonly List<string> _record = new List<string>();
        readonly List<string> _failures = new List<string>();
        readonly List<string> _missingCharacters = new List<string>();
        MapEntryPlayFixture _fixture;
        string _output;
        int _hpMarker;
        uint _revision;
        int _gold;
        int _material;
        int _enemyId;
        int _serverTick;
        int _otherMissingCharacters;
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
            _hpMarker = 40;
            _revision = 0;
            _gold = 0;
            _material = 0;
            _enemyId = 800;
            _serverTick = 0;
            _otherMissingCharacters = 0;
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
            // Written before the fixture cleanup, so a failing cleanup cannot lose the record of the frames already saved.
            if (!string.IsNullOrEmpty(_output) && Directory.Exists(_output)) WriteNew("capture-record.txt", string.Join("\n", _record) + "\n");
            yield return _fixture.Cleanup();
            if (restoreError != null) throw new InvalidOperationException("Game view restore failed", restoreError);
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator KillRewardText_AboveTheDeathSpot_AtBothSizes_CapturedFromTheGameView()
        {
            _output = Environment.GetEnvironmentVariable(OutputVariable);
            if (string.IsNullOrEmpty(_output)) Assert.Ignore($"live capture runs only when the capture runner sets {OutputVariable}");
            Assert.IsTrue(Directory.Exists(_output), "capture output folder exists: " + _output);
            Note("utc", DateTime.UtcNow.ToString("o"));
            Note("isPlaying / isBatchMode / unityVersion", $"{Application.isPlaying} / {Application.isBatchMode} / {Application.unityVersion}");
            Note("graphics device", $"{SystemInfo.graphicsDeviceType} / {SystemInfo.graphicsDeviceName}");

            yield return OpenGameView();
            yield return EnterCaptureMap();
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
                yield return KillAndCapture(size);
            }
#if UNITY_EDITOR
            Note("game view after captures", _gameView.Describe());
#endif
            Note("missing characters in kill texts", _missingCharacters.Count == 0 ? "0" : _missingCharacters.Count + ": " + string.Join("; ", _missingCharacters));
            Note("missing characters in other texts", _otherMissingCharacters);
            foreach (string failure in _failures) Note("failure", failure);
            CollectionAssert.IsEmpty(_failures, "every capture check holds; the frames and the record are written either way");
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

        // Town ready with the client's own inventory query answered, then HuntingGround as InventorySceneLifecycleTests
        // 455~472: the move, fresh HP, Ready, and the client's requery answered with the same values, so the panel is not
        // left 「확인 중」 and no kill text can come from it. If HuntingGround cannot be synchronized, the reason is recorded
        // and the captures are taken in Town instead (capture contract requirement 2.3).
        IEnumerator EnterCaptureMap()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(++_hpMarker, MaxHp);
            yield return _fixture.WaitMap(Town);
            string townBlocked = null;
            yield return AnswerTheInventoryQuery("Town", 0, reason => townBlocked = reason);
            if (townBlocked != null) Assert.Fail("Town baseline: " + townBlocked);
            Note("scripted peer", $"loopback 127.0.0.1:{_fixture.Peer.Port} (fixture stand-in, not GameServer)");

            string blocked = null;
            yield return MoveTo(HuntingGround, "HuntingGround", reason => blocked = reason);
            if (blocked != null)
            {
                Note("HuntingGround blocked, capturing in Town", blocked);
                string again = null;
                yield return MoveTo(Town, "Town after HuntingGround", reason => again = reason);
                if (again != null) Assert.Fail("Town fallback: " + again);
            }
            yield return ScreenUiProbe.WaitForEventSystem();
            Note("capture scene", $"{SceneManager.GetActiveScene().name}, map {_fixture.Session.Entry.MapId}, player {PlayerSpot()}");
        }

        IEnumerator MoveTo(byte map, string label, Action<string> blocked)
        {
            int queriesBefore = _fixture.Peer.Count(PacketID.C_InventoryRequest);
            _fixture.Peer.Send(new S_MapTransition { destMapId = map, spawnX = PlayerSpawnX, spawnY = 0 }.Write());
            _fixture.Hp(++_hpMarker, MaxHp);
            yield return _fixture.WaitMap(map);
            yield return AnswerTheInventoryQuery(label, queriesBefore, blocked);
        }

        // The client asks for its inventory once a map is Ready; the answer is the baseline again, never an increase.
        IEnumerator AnswerTheInventoryQuery(string label, int queriesBefore, Action<string> blocked)
        {
            bool queried = false;
            yield return ScreenUiProbe.WaitUntil(() => _fixture.Peer.Count(PacketID.C_InventoryRequest) > queriesBefore, QuerySeconds,
                met => queried = met);
            Note(label + " C_InventoryRequest", $"{queriesBefore} before, {_fixture.Peer.Count(PacketID.C_InventoryRequest)} now");
            if (!queried)
            {
                blocked($"no C_InventoryRequest within {QuerySeconds} s of Ready");
                yield break;
            }
            SendInventory(BaseGold, BaseMaterial);
            bool synchronized = false;
            yield return ScreenUiProbe.WaitUntil(() => InventoryPanelProbe.Status == Synchronized, QuerySeconds, met => synchronized = met);
            Note(label + " inventory status", InventoryPanelProbe.Status);
            if (!synchronized)
            {
                blocked($"inventory status is 「{InventoryPanelProbe.Status}」 {QuerySeconds} s after the answer");
                yield break;
            }
            yield return AfterEarlierPacketsApply();
            Note(label + " kill texts after the answer", KillRewardFloatTextProbe.Describe(KillRewardFloatTextProbe.Find()));
        }

        // One new kill at this size, then the three frames by real time since the text was first visible. Before the
        // next size the text must be gone and this death out of the pairing window, so the next text is its own increase.
        IEnumerator KillAndCapture(string size)
        {
            int id = ++_enemyId;
            yield return SpawnSettled(id, EnemyX);
            Assert.IsTrue(EnemyRegistry.Instance.TryGetTransform(id, out Transform enemy), $"fixture: enemy {id} is alive before its death");
            GameObject body = enemy.gameObject;
            Vector3 spot = enemy.position;
            Note($"{size} enemy {id}", $"settled at {Format(spot)}, player {PlayerSpot()}");
            _fixture.Peer.Send(new S_EntityDeath { entityId = id }.Write());
            SendInventory(_gold + KillGold, _material + KillMaterial);
            float sent = Time.realtimeSinceStartup;

            TextMeshPro[] texts = KillRewardFloatTextProbe.Find();
            while ((texts.Length == 0 || KillRewardFloatTextProbe.Opacity(texts) <= 0f) && Time.realtimeSinceStartup - sent < AppearSeconds)
            {
                yield return null;
                texts = KillRewardFloatTextProbe.Find();
            }
            if (texts.Length == 0 || KillRewardFloatTextProbe.Opacity(texts) <= 0f)
            {
                _failures.Add($"{size}: no visible kill text within {AppearSeconds} s of the snapshot");
                Note(size + " world texts", KillRewardFloatTextProbe.DescribeWorldTexts());
                yield break;
            }
            float firstSeen = Time.realtimeSinceStartup;
            Note(size + " text first visible", string.Format(CultureInfo.InvariantCulture, "{0:F3} s after the snapshot was sent, frame {1}: {2}",
                firstSeen - sent, Time.frameCount, KillRewardFloatTextProbe.Describe(texts)));
            foreach (var (at, name) in Shots)
            {
                while (Time.realtimeSinceStartup - firstSeen < at) yield return null;
                yield return new WaitForEndOfFrame();
                CaptureFrame(size, name, firstSeen, spot, body);
            }

            bool gone = false;
            yield return ScreenUiProbe.WaitUntil(() => KillRewardFloatTextProbe.Find().Length == 0, GoneSeconds, met => gone = met);
            Note(size + " text gone", gone
                ? string.Format(CultureInfo.InvariantCulture, "{0:F3} s after first visible", Time.realtimeSinceStartup - firstSeen)
                : $"still shown {GoneSeconds} s later: {KillRewardFloatTextProbe.Describe(KillRewardFloatTextProbe.Find())}");
            yield return MapEntryPlayFixture.Wait(() => EnemyRegistry.Instance == null || !EnemyRegistry.Instance.TryGetTransform(id, out _),
                $"death {id} applied", 3);
            float until = Time.realtimeSinceStartup + WindowExpirySeconds;
            yield return MapEntryPlayFixture.Wait(() => Time.realtimeSinceStartup >= until, "pairing window expiry", WindowExpirySeconds + 1);
        }

        // Called right after WaitForEndOfFrame: the Game view's final frame saved as captured, then what that frame drew.
        void CaptureFrame(string size, string name, float firstSeen, Vector3 spot, GameObject body)
        {
            string label = $"{size} {name}";
            float after = Time.realtimeSinceStartup - firstSeen;
            int screenWidth = Screen.width, screenHeight = Screen.height;
            Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                string written = WriteNew($"{size}-{name}.png", shot.EncodeToPNG());
                Note(label + " frame", string.Format(CultureInfo.InvariantCulture,
                    "utc {0} | {1:F3} s after first visible, frame {2} | Screen {3}x{4} | texture {5}x{6} {7} | method " +
                    "ScreenCapture.CaptureScreenshotAsTexture after WaitForEndOfFrame | {8}", DateTime.UtcNow.ToString("o"), after,
                    Time.frameCount, screenWidth, screenHeight, shot.width, shot.height, shot.format, written));
                if (shot.width != screenWidth || shot.height != screenHeight) _failures.Add($"{label}: frame size differs from the screen");
            }
            finally { UnityEngine.Object.Destroy(shot); }

            TextMeshPro[] texts = KillRewardFloatTextProbe.Find();
            string[] lines = KillRewardFloatTextProbe.Lines(texts);
            Note(label + " lines", $"[{string.Join(", ", lines)}] in {texts.Length} TextMeshPro");
            Camera camera = Camera.main;
            Note(label + " camera", camera == null ? "none" : string.Format(CultureInfo.InvariantCulture,
                "{0} in {1}, orthographic {2} size {3:F3}, position {4}, pixelRect {5}, death spot on screen {6}", camera.name,
                camera.gameObject.scene.name, camera.orthographic, camera.orthographicSize, Format(camera.transform.position), camera.pixelRect,
                Format(camera.WorldToScreenPoint(spot))));
            bool allInside = texts.Length > 0;
            foreach (TextMeshPro text in texts.OrderByDescending(item => item.transform.position.y).ThenBy(item => item.transform.position.x))
            {
                bool inside = false;
                string bounds = "no camera";
                if (camera != null)
                {
                    Rect rect = ScreenBounds(camera, text, out bool inFront);
                    Vector3 extent = text.textBounds.size;
                    inside = inFront && extent.x > 0f && extent.y > 0f && rect.xMin >= 0f && rect.yMin >= 0f &&
                        rect.xMax <= screenWidth && rect.yMax <= screenHeight;
                    bounds = string.Format(CultureInfo.InvariantCulture, "x {0:F1}..{1:F1} y {2:F1}..{3:F1} (local size {4}, in front {5})",
                        rect.xMin, rect.xMax, rect.yMin, rect.yMax, Format(extent), inFront);
                }
                allInside &= inside;
                Note($"{label} line '{string.Join("\\n", KillRewardFloatTextProbe.Lines(new[] { text }))}'", string.Format(CultureInfo.InvariantCulture,
                    "world {0} | screen bounds {1} | inside {2} | color {3} #{4} | alpha {5:F3} | font {6} size {7:F2} | sorting order {8} | {9}",
                    Format(text.transform.position), bounds, inside, text.color, ColorUtility.ToHtmlStringRGBA(text.color), text.alpha,
                    text.font != null ? text.font.name : "null", text.fontSize, SortingOrder(text), Outline(text.fontSharedMaterial)));
            }
            Note(label + " dead monster", body != null
                ? $"present at {Format(body.transform.position)}, active {body.activeInHierarchy}"
                : "destroyed");
            Note(label + " death spot", $"{Format(spot)}, player {PlayerSpot()}");

            if (name != "main") return;
            if (!lines.SequenceEqual(new[] { GoldTen, MaterialOne })) _failures.Add($"{label}: kill text lines are [{string.Join(", ", lines)}]");
            if (!allInside) _failures.Add($"{label}: a kill text line is not inside the screen");
        }

        // The laid-out text's local bounds, all eight corners through the camera, as a screen rectangle in pixels
        // (bottom-left origin, as Screen and ScreenCapture use).
        static Rect ScreenBounds(Camera camera, TMP_Text text, out bool inFront)
        {
            Bounds local = text.textBounds;
            float left = float.PositiveInfinity, bottom = float.PositiveInfinity;
            float right = float.NegativeInfinity, top = float.NegativeInfinity;
            inFront = true;
            for (int corner = 0; corner < 8; corner++)
            {
                var point = new Vector3((corner & 1) == 0 ? local.min.x : local.max.x, (corner & 2) == 0 ? local.min.y : local.max.y,
                    (corner & 4) == 0 ? local.min.z : local.max.z);
                Vector3 onScreen = camera.WorldToScreenPoint(text.transform.TransformPoint(point));
                inFront &= onScreen.z > 0f;
                left = Mathf.Min(left, onScreen.x);
                bottom = Mathf.Min(bottom, onScreen.y);
                right = Mathf.Max(right, onScreen.x);
                top = Mathf.Max(top, onScreen.y);
            }
            return Rect.MinMaxRect(left, bottom, right, top);
        }

        static string Outline(Material material)
        {
            if (material == null) return "material null";
            string width = material.HasProperty(ShaderUtilities.ID_OutlineWidth)
                ? material.GetFloat(ShaderUtilities.ID_OutlineWidth).ToString("F3", CultureInfo.InvariantCulture)
                : "absent";
            string colour = material.HasProperty(ShaderUtilities.ID_OutlineColor)
                ? "#" + ColorUtility.ToHtmlStringRGBA(material.GetColor(ShaderUtilities.ID_OutlineColor))
                : "absent";
            return $"material {material.name} shader {(material.shader != null ? material.shader.name : "null")} | " +
                $"{ShaderUtilities.Keyword_Outline} {material.IsKeywordEnabled(ShaderUtilities.Keyword_Outline)} | outline width {width} colour {colour}";
        }

        static string SortingOrder(Component text)
        {
            Renderer renderer = text.GetComponent<Renderer>();
            return renderer == null ? "no renderer" : renderer.sortingOrder.ToString(CultureInfo.InvariantCulture);
        }

        // MapEntrySceneLifecycleTests 93~98 and 111~114, as the pretest: spawn, one authoritative state at the same x, then
        // wait for the interpolated transform to settle there.
        IEnumerator SpawnSettled(int id, float x)
        {
            _fixture.Peer.Send(new S_EntitySpawn { entityId = id, entityKind = 0, x = x, y = 0, currentHp = MaxHp, maxHp = MaxHp }.Write());
            _fixture.Peer.Send(new S_EntityState { entityId = id, serverTick = ++_serverTick, x = x, y = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => EnemyRegistry.Instance != null && EnemyRegistry.Instance.TryGetTransform(id, out Transform enemy) &&
                Mathf.Abs(enemy.position.x - x) < 0.01f, $"enemy {id} settled at x {x}", 5);
        }

        // As the pretest and DungeonClearRewardWindowSceneTests: the peer writes in order on one TCP stream and the client
        // applies packets in order on the main thread, so once this HP value shows, every packet sent before it is handled.
        IEnumerator AfterEarlierPacketsApply()
        {
            int marker = ++_hpMarker;
            _fixture.Hp(marker, MaxHp);
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.CurrentHp == marker, "HP marker " + marker, 3);
            yield return _fixture.Frames(5);
        }

        void SendInventory(int gold, int material)
        {
            _gold = gold;
            _material = material;
            var snapshot = new S_InventorySnapshot { revision = ++_revision, currency = gold, slot0ItemId = (int)ItemId.Material, slot0Count = material };
            _fixture.Peer.Send(snapshot.Write());
            Note("snapshot", $"revision {_revision}: gold {gold}, material {material}");
        }

        string PlayerSpot() => _fixture.Player == null ? "none" : Format(_fixture.Player.transform.position);

        static string Format(Vector3 value) =>
            string.Format(CultureInfo.InvariantCulture, "({0:F3}, {1:F3}, {2:F3})", value.x, value.y, value.z);

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
            Debug.Log($"[KillRewardFloatText capture] {key}: {value}");
        }

        void RecordMissingCharacter(int unicode, int stringIndex, string text, TMP_FontAsset fontAsset, TMP_Text textComponent)
        {
            // The event is global; the kill reward lines are world TextMeshPro texts with a line starting "+".
            bool killText = textComponent is TextMeshPro && (text ?? string.Empty).Split('\n')
                .Any(line => line.Trim().StartsWith("+", StringComparison.Ordinal));
            if (!killText)
            {
                _otherMissingCharacters++;
                return;
            }
            _missingCharacters.Add(string.Format(CultureInfo.InvariantCulture, "U+{0:X4} at {1} in '{2}' ({3})", unicode, stringIndex,
                text, fontAsset != null ? fontAsset.name : "no font"));
        }
    }
}
