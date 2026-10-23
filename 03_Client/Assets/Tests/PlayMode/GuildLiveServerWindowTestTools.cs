using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using NUnit.Framework;
using Shared.GameData;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Live-server lane for two GUI editors against one real GameServer. The window runner owns
    // the server and both editors; this file owns role selection, the file rendezvous between
    // the editors, read-only observations (product logs, public state, original captures) and
    // real virtual-device input. It never sends a packet, writes product state, or computes a
    // server value: expected numbers are the approved scene literals in the test file.
    internal static class GuildLiveServerWindowTestTools
    {
        internal const string LaneCategory = "LiveServerWindow";
        // An unfiltered run reports these cases as explicit not-run; the window runner selects each
        // one by its exact full name, which NUnit counts as an explicit selection.
        internal const string LaneExplicitReason =
            "needs a Main-bound real GameServer and a partner editor; run only by exact name in the live-server window";
        // Inside the runner's 2400 s limit for both roots, leaving room for the cold Library import.
        internal const int LiveTimeoutMs = 1500000;

        static readonly Regex HitLine = new Regex(
            @"^\[Unity\] Hit: attacker=(-?\d+) target=(-?\d+) dmg=(-?\d+) hp=(-?\d+)/(-?\d+) effect=(\w+)");

        // Product Debug.Log prefixes kept as raw client-side evidence of sent intents and received results.
        static readonly string[] ProductPrefixes =
        {
            "[Unity] Hit:", "[Attack]", "[Unity] Entity ", "[Party]", "[PartyInviteSender]", "[PartyInvitePopup]",
            "[Quest]", "[CheatSender]", "[Unity] OnConnected", "[Unity] OnDisconnected", "[NetworkService]",
            "[Unity] Map entry failed", "[Unity] Unknown PacketId",
        };

        internal sealed class LiveLane
        {
            LiveLane() { }

            internal string Role { get; private set; }
            internal string Partner => Role == "a" ? "b" : "a";
            internal string RunId { get; private set; }
            internal string SyncDirectory { get; private set; }
            internal string CaptureDirectory { get; private set; }
            internal string QuestGate { get; private set; }

            // Guard for an exact-name selection made without the runner's environment: there is no server
            // or partner, so the case fails with that reason instead of passing.
            internal static LiveLane Read(string expectedRole)
            {
                string role = Environment.GetEnvironmentVariable("DAWNHOLDER_LIVE_ROLE");
                if (string.IsNullOrEmpty(role))
                    Assert.Fail("live-server lane not assigned: DAWNHOLDER_LIVE_ROLE is absent, so no main-bound GameServer " +
                        "or partner editor exists for this explicitly selected case");
                Assert.AreEqual(expectedRole, role, "runner role must match the filtered case");
                var lane = new LiveLane
                {
                    Role = role,
                    RunId = Environment.GetEnvironmentVariable("DAWNHOLDER_LIVE_RUN_ID"),
                    SyncDirectory = Environment.GetEnvironmentVariable("DAWNHOLDER_LIVE_SYNC_DIR"),
                    CaptureDirectory = Environment.GetEnvironmentVariable("DAWNHOLDER_LIVE_CAPTURE_DIR"),
                    QuestGate = Environment.GetEnvironmentVariable("DAWNHOLDER_LIVE_QUEST_GATE"),
                };
                Assert.IsFalse(string.IsNullOrWhiteSpace(lane.RunId), "runner supplies the run identity");
                Assert.IsTrue(Path.IsPathRooted(lane.SyncDirectory ?? ""), "runner supplies an absolute rendezvous directory");
                Assert.IsTrue(Path.IsPathRooted(lane.CaptureDirectory ?? ""), "runner supplies an absolute capture directory");
                Assert.IsTrue(lane.QuestGate == "normal" || lane.QuestGate == "debug-f8",
                    "quest gate is the bound plan value normal or debug-f8: " + lane.QuestGate);
                return lane;
            }
        }

        // One flat record per rendezvous step. Fields that a step does not observe keep their defaults.
        [Serializable]
        internal sealed class SyncRecord
        {
            public string role, runId, step, utc, account, result, note, clickUtc;
            public int frame, entity, partyId, bossId, bossMaxHp, damage, treasury, level, members, online;
            public int gold, material, pouch, weaponLevel, attackBonus;
            public long guildSequence;
            public uint guildRevision, inventoryRevision, requestedRevision;
            public bool pendingAfterClick;
        }

        // Each editor writes only its own role directory; a step file appears complete through a rename.
        internal sealed class Rendezvous
        {
            readonly LiveLane _lane;
            readonly LiveLog _log;
            readonly string _own;
            readonly string _partner;

            internal Rendezvous(LiveLane lane, LiveLog log)
            {
                _lane = lane;
                _log = log;
                _own = Path.Combine(lane.SyncDirectory, lane.Role);
                _partner = Path.Combine(lane.SyncDirectory, lane.Partner);
                Assert.IsFalse(Directory.Exists(_own), "own rendezvous directory is new for this run: " + _own);
                Directory.CreateDirectory(_own);
            }

            internal void Post(string step, SyncRecord record)
            {
                record.role = _lane.Role;
                record.runId = _lane.RunId;
                record.step = step;
                record.utc = DateTime.UtcNow.ToString("O");
                record.frame = Time.frameCount;
                string path = Path.Combine(_own, step + ".json");
                Assert.IsFalse(File.Exists(path), "each rendezvous step is posted once: " + step);
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(record, true), new UTF8Encoding(false));
                File.Move(temporary, path);
                _log.Event("sync-post " + step, JsonUtility.ToJson(record));
            }

            internal IEnumerator Await(string step, float seconds, Action<SyncRecord> received)
            {
                string path = Path.Combine(_partner, step + ".json");
                string abort = Path.Combine(_partner, "abort.json");
                float deadline = Time.realtimeSinceStartup + seconds;
                while (!File.Exists(path))
                {
                    if (File.Exists(abort)) Assert.Fail("partner " + _lane.Partner + " aborted before " + step + ": " + File.ReadAllText(abort));
                    Assert.Less(Time.realtimeSinceStartup, deadline, "Timed out waiting for partner step " + step);
                    yield return null;
                }
                SyncRecord record = JsonUtility.FromJson<SyncRecord>(File.ReadAllText(path));
                Assert.AreEqual(_lane.Partner, record.role, "partner file role");
                Assert.AreEqual(_lane.RunId, record.runId, "partner file belongs to this run");
                _log.Event("sync-read " + step, JsonUtility.ToJson(record));
                received(record);
            }

            // Best effort from teardown, so the partner fails fast instead of waiting for its timeout.
            internal void Abort(string reason)
            {
                try
                {
                    string path = Path.Combine(_own, "abort.json");
                    if (!File.Exists(path)) File.WriteAllText(path, DateTime.UtcNow.ToString("O") + " " + reason, new UTF8Encoding(false));
                }
                catch (Exception error) { Debug.LogWarning("[guild live] abort marker not written: " + error.Message); }
            }
        }

        internal readonly struct HitObservation
        {
            internal HitObservation(int attacker, int target, int damage, int hp, int maxHp, string effect, string utc)
            {
                Attacker = attacker;
                Target = target;
                Damage = damage;
                Hp = hp;
                MaxHp = maxHp;
                Effect = effect;
                Utc = utc;
            }

            internal int Attacker { get; }
            internal int Target { get; }
            internal int Damage { get; }
            internal int Hp { get; }
            internal int MaxHp { get; }
            internal string Effect { get; }
            internal string Utc { get; }
        }

        internal readonly struct InventoryRow
        {
            internal InventoryRow(uint revision, int gold, int material, int pouch)
            {
                Revision = revision;
                Gold = gold;
                Material = material;
                Pouch = pouch;
            }

            internal uint Revision { get; }
            internal int Gold { get; }
            internal int Material { get; }
            internal int Pouch { get; }
        }

        // Observation log and original captures for one role. Checks collect non-blocking
        // requirement results; a blocking precondition uses Assert so the partner sees an abort.
        internal sealed class LiveLog : IDisposable
        {
            readonly LiveLane _lane;
            readonly string _directory;
            readonly List<string> _events = new List<string>();
            readonly List<string> _failures = new List<string>();
            readonly List<string> _product = new List<string>();
            readonly List<string> _errors = new List<string>();
            readonly List<HitObservation> _hits = new List<HitObservation>();
            readonly List<InventoryRow> _inventoryRows = new List<InventoryRow>();
            GuildRequestController _guild;
            InventoryState _inventory;
            int _growthSignals;
            int _pendingStarts;
            bool _wasPending;

            internal LiveLog(LiveLane lane)
            {
                _lane = lane;
                _directory = Path.Combine(lane.CaptureDirectory, lane.Role);
                Assert.IsFalse(Directory.Exists(_directory), "capture directory is new for this run: " + _directory);
                Directory.CreateDirectory(_directory);
                Application.logMessageReceived += OnLog;
                Event("start", "role=" + lane.Role + "; run=" + lane.RunId + "; questGate=" + lane.QuestGate +
                    "; real GameServer at 127.0.0.1:7777; two editors; virtual input");
            }

            internal int GrowthSignals => _growthSignals;
            // The controller raises Changed inside its own send attempt, so a not-pending to pending
            // transition is the client record of a request leaving, even if its result arrives in the same frame.
            internal int PendingStarts => _pendingStarts;
            internal IReadOnlyList<HitObservation> Hits => _hits;
            internal IReadOnlyList<InventoryRow> InventoryRows => _inventoryRows;

            internal int CountProduct(string prefix) => _product.Count(line => line.Contains("] " + prefix));

            internal bool ProductContains(string text) => _product.Any(line => line.Contains(text));

            internal void Event(string input, string observed)
            {
                string line = DateTime.UtcNow.ToString("O") + " frame=" + Time.frameCount + " [" + input + "] " + observed;
                _events.Add(line);
                Debug.Log("[guild live] " + _lane.Role + " " + line);
            }

            internal void Check(bool success, string message)
            {
                Event("check", success + ": " + message);
                if (!success) _failures.Add(message);
            }

            // Read-only subscription to the connection-owned controller's public notifications.
            internal void Observe(GuildRequestController guild)
            {
                _guild = guild;
                _wasPending = guild.IsPending;
                guild.Changed += OnGuildChanged;
                guild.GrowthConfirmed += OnGrowth;
                Event("guild-observe", Describe(guild));
            }

            // Every applied 36 in order, so a reward step can be told apart from ordinary kill gold.
            internal void ObserveInventory(InventoryState inventory)
            {
                _inventory = inventory;
                inventory.OnInventoryChanged += OnInventoryChanged;
                OnInventoryChanged();
            }

            internal static string Describe(GuildRequestController guild)
            {
                GuildClientState state = guild.State;
                return "seq=" + state.SnapshotSequence + "; kind=" + state.ChangeKind + "; guild=" + state.GuildId +
                    "; rev=" + state.GuildRevision + "; treasury=" + state.Treasury + "; level=" + state.StrongholdLevel +
                    "; unlocked=" + state.BlacksmithUnlocked + "; members=" + state.MemberCount + "; online=" +
                    state.OnlineMemberCount + "; personalRev=" + state.InventoryRevision + "/" + state.PersonalSnapshotRevision +
                    "; weapon=" + state.WeaponLevel + "; bonus=" + state.AttackBonus + "; pending=" + guild.IsPending +
                    "; last=" + guild.LastResult + "; sendError=" + (guild.LastSendError == null ? "none" : guild.LastSendError.GetType().Name);
            }

            internal IEnumerator Capture(MapEntryPlayFixture fixture, string name, string note)
            {
                yield return new WaitForEndOfFrame();
                Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
                Assert.IsNotNull(shot, "actual end-of-frame ScreenCapture");
                try
                {
                    UnityClientSession session = fixture.Session;
                    InventoryState inventory = InventoryState.Instance;
                    var capture = new CaptureData
                    {
                        role = _lane.Role, utc = DateTime.UtcNow.ToString("O"), frame = Time.frameCount, note = note,
                        account = AccountLoadout.SessionAccountName, scene = SceneManager.GetActiveScene().name,
                        screenWidth = Screen.width, screenHeight = Screen.height, pngWidth = shot.width, pngHeight = shot.height,
                        entity = session?.LocalEntityId ?? 0, map = session == null ? -1 : session.Entry.MapId,
                        entryState = session == null ? "none" : session.Entry.State.ToString(),
                        epoch = session == null ? 0 : session.Entry.Epoch,
                        hp = session == null ? 0 : session.Entry.CurrentHp, maxHp = session == null ? 0 : session.Entry.MaxHp,
                        guild = session == null ? "none" : Describe(session.Guild),
                        inventory = inventory == null || !inventory.HasSnapshot ? "none" : DescribeInventory(inventory),
                        party = PartyState.Instance == null ? "none" : "party=" + PartyState.Instance.PartyId + "; m0=" +
                            PartyState.Instance.Member0EntityId + "; m1=" + PartyState.Instance.Member1EntityId,
                        quest = QuestState.Instance == null ? "none" : QuestState.Instance.CurrentCount + "/" + QuestState.Instance.TargetCount,
                        growthSignals = _growthSignals,
                        texts = UnityEngine.Object.FindObjectsByType<TMP_Text>().Where(ScreenUiProbe.IsShown)
                            .Select(text => new TextData { path = Hierarchy(text.transform), text = text.text,
                                truncated = text.isTextTruncated, rect = ScreenUiProbe.ScreenRect(text.rectTransform) }).ToArray(),
                    };
                    Write(name + ".png", shot.EncodeToPNG());
                    Write(name + ".json", Encoding.UTF8.GetBytes(JsonUtility.ToJson(capture, true)));
                    Check(shot.width == Screen.width && shot.height == Screen.height, name + " original PNG equals Screen");
                    Check(Screen.width == 802 && Screen.height == 451, name + " actual Screen is the contracted 802x451");
                    Event("capture", name + ": " + shot.width + "x" + shot.height + " scene=" + capture.scene +
                        " texts=" + capture.texts.Length + "; original unresized ScreenCapture PNG");
                }
                finally { UnityEngine.Object.Destroy(shot); }
            }

            internal void Finish()
            {
                Save();
                Assert.IsEmpty(_failures, "pre-C9 live-server requirements: " + string.Join("; ", _failures));
            }

            public void Dispose()
            {
                Application.logMessageReceived -= OnLog;
                if (_guild != null)
                {
                    _guild.Changed -= OnGuildChanged;
                    _guild.GrowthConfirmed -= OnGrowth;
                }
                if (_inventory != null) _inventory.OnInventoryChanged -= OnInventoryChanged;
                Save();
            }

            void OnInventoryChanged()
            {
                if (_inventory == null || !_inventory.HasSnapshot) return;
                _inventoryRows.Add(new InventoryRow(_inventory.Revision, _inventory.Currency,
                    CountItem(_inventory, ItemId.Material), CountItem(_inventory, ItemId.CoinPouch)));
                Event("inventory-changed", DescribeInventory(_inventory));
            }

            static string DescribeInventory(InventoryState inventory) => "rev=" + inventory.Revision + "; gold=" +
                inventory.Currency + "; material=" + CountItem(inventory, ItemId.Material) + "; pouch=" + CountItem(inventory, ItemId.CoinPouch);

            void OnGuildChanged()
            {
                if (_guild.IsPending && !_wasPending) _pendingStarts++;
                _wasPending = _guild.IsPending;
                Event("guild-changed", Describe(_guild) + "; pendingStarts=" + _pendingStarts);
            }

            // C9 precursor observation only: the product signal Art would consume. Not a C9 result.
            void OnGrowth(GuildChangeKind kind)
            {
                _growthSignals++;
                Event("growth-signal", kind + "; count=" + _growthSignals + "; C9 art call not connected in this lane");
            }

            void OnLog(string message, string stackTrace, LogType type)
            {
                if (message == null || message.StartsWith("[guild live]")) return;
                string stamp = DateTime.UtcNow.ToString("O") + " frame=" + Time.frameCount + " " + type + "] ";
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _errors.Add(stamp + message);
                if (!ProductPrefixes.Any(prefix => message.StartsWith(prefix, StringComparison.Ordinal))) return;
                _product.Add(stamp + message);
                Match hit = HitLine.Match(message);
                if (hit.Success)
                {
                    _hits.Add(new HitObservation(int.Parse(hit.Groups[1].Value, CultureInfo.InvariantCulture),
                        int.Parse(hit.Groups[2].Value, CultureInfo.InvariantCulture), int.Parse(hit.Groups[3].Value, CultureInfo.InvariantCulture),
                        int.Parse(hit.Groups[4].Value, CultureInfo.InvariantCulture), int.Parse(hit.Groups[5].Value, CultureInfo.InvariantCulture),
                        hit.Groups[6].Value, DateTime.UtcNow.ToString("O")));
                }
            }

            void Save() => File.WriteAllText(Path.Combine(_directory, "observations.json"), JsonUtility.ToJson(new Summary
            {
                role = _lane.Role, runId = _lane.RunId, utc = DateTime.UtcNow.ToString("O"), events = _events.ToArray(),
                failures = _failures.ToArray(), productLog = _product.ToArray(), errorLog = _errors.ToArray(),
                growthSignals = _growthSignals,
            }, true), new UTF8Encoding(false));

            void Write(string name, byte[] bytes)
            {
                string path = Path.Combine(_directory, name);
                Assert.IsFalse(File.Exists(path), "each original capture is new: " + path);
                File.WriteAllBytes(path, bytes);
            }

            static string Hierarchy(Transform item)
            {
                var names = new List<string>();
                for (; item != null; item = item.parent) names.Insert(0, item.name);
                return string.Join("/", names);
            }

            [Serializable] sealed class Summary
            {
                public string role, runId, utc;
                public string[] events, failures, productLog, errorLog;
                public int growthSignals;
            }
            [Serializable] sealed class TextData { public string path, text; public bool truncated; public Rect rect; }
            [Serializable] sealed class CaptureData
            {
                public string role, utc, note, account, scene, entryState, guild, inventory, party, quest;
                public int frame, screenWidth, screenHeight, pngWidth, pngHeight, entity, map, hp, maxHp, growthSignals;
                public long epoch;
                public TextData[] texts;
            }
        }

        internal static int CountItem(InventoryState inventory, ItemId item)
        {
            int count = 0;
            for (int i = 0; i < InventoryLimits.MaxSlots; i++)
            {
                InventorySlot slot = inventory.GetSlot(i);
                if (slot.ItemId == item) count += slot.Count;
            }
            return count;
        }

        // Pointer input on a shown UI control. The product attack log is the client-side record of
        // every C_Attack intent, so its count before/after shows whether the UI click leaked into combat.
        internal static IEnumerator Click(MapEntryPlayFixture fixture, LiveLog log, Component target, string reason)
        {
            // The UI scene (and its EventSystem) loads additively after each map; a player's click waits for it too.
            yield return ScreenUiProbe.WaitForEventSystem();
            Vector2 point = ScreenUiProbe.ScreenPoint(target);
            GameObject[] hits = ScreenUiProbe.RaycastHits(point);
            int attacks = log.CountProduct("[Attack]");
            log.Event("pointer-before " + reason, "point=" + point + "; attacks=" + attacks + "; hits=" +
                string.Join("|", hits.Select(hit => hit.name)));
            log.Check(hits.Length > 0 && hits[0].transform.IsChildOf(target.transform), reason + " top pointer hit belongs to target");
            yield return InventoryPanelProbe.ClickAt(fixture.Mouse, point);
            yield return fixture.Frames(4);
            log.Check(log.CountProduct("[Attack]") == attacks, reason + " UI pointer emits no C_Attack");
        }

        internal static IEnumerator WalkToX(MapEntryPlayFixture fixture, float x, float tolerance, float seconds, string label)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (Mathf.Abs(fixture.Player.transform.position.x - x) > tolerance)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "virtual keyboard could not reach " + label);
                fixture.Keys(fixture.Player.transform.position.x < x ? Key.D : Key.A);
                yield return null;
            }
            fixture.Keys();
            yield return fixture.Frames(3);
        }

        // A world click on the local player's own screen position, the gameplay attack path. A point under a
        // window that just opened (for example the reward window after the partner's killing hit) is not clicked.
        internal static IEnumerator Swing(MapEntryPlayFixture fixture, LiveLog log)
        {
            fixture.Keys();
            yield return ScreenUiProbe.WaitForEventSystem();
            yield return MapEntryPlayFixture.Wait(() => fixture.Player != null && fixture.Player.CanAttack &&
                !fixture.Player.IsActionLocked, "attack cooldown ready", 6);
            Vector2 point = InventoryPanelProbe.PlayerScreenPoint(fixture.Player);
            if (ScreenUiProbe.RaycastHits(point).Length != 0) point = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            GameObject[] hits = ScreenUiProbe.RaycastHits(point);
            if (hits.Length != 0)
            {
                log.Event("swing-skipped", "point=" + point + " is under UI: " + string.Join("|", hits.Select(hit => hit.name)));
                yield break;
            }
            yield return InventoryPanelProbe.ClickAt(fixture.Mouse, point);
        }

        internal static RemoteEnemy Enemy(int entityId)
        {
            if (EnemyRegistry.Instance == null) return null;
            foreach ((int id, Transform transform) in EnemyRegistry.Instance.EnemyTransforms)
            {
                if (id == entityId && transform != null) return transform.GetComponent<RemoteEnemy>();
            }
            return null;
        }

        internal static RemoteEnemy NearestEnemy(MapEntryPlayFixture fixture, Func<RemoteEnemy, bool> accept)
        {
            if (EnemyRegistry.Instance == null || fixture.Player == null) return null;
            Vector3 origin = fixture.Player.transform.position;
            RemoteEnemy best = null;
            float bestScore = float.MaxValue;
            foreach ((int _, Transform transform) in EnemyRegistry.Instance.EnemyTransforms)
            {
                RemoteEnemy enemy = transform == null ? null : transform.GetComponent<RemoteEnemy>();
                if (enemy == null || enemy.CurrentHp <= 0 || !accept(enemy)) continue;
                // Prefer enemies on the player's walking line; a height difference may need a jump.
                float score = Mathf.Abs(transform.position.x - origin.x) + 4f * Mathf.Abs(transform.position.y - origin.y);
                if (score < bestScore)
                {
                    best = enemy;
                    bestScore = score;
                }
            }
            return best;
        }

        // Walk next to the target and swing until one own melee hit on it is logged by the product.
        internal static IEnumerator OwnHit(MapEntryPlayFixture fixture, LiveLog log, int targetId, float seconds,
            Action<HitObservation> observed)
        {
            int self = fixture.Session.LocalEntityId.Value;
            int before = log.Hits.Count;
            float deadline = Time.realtimeSinceStartup + seconds;
            while (true)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "no own melee hit on target " + targetId);
                HitObservation[] own = log.Hits.Skip(before).Where(hit => hit.Attacker == self && hit.Target == targetId).ToArray();
                if (own.Length > 0)
                {
                    observed(own[0]);
                    yield break;
                }
                RemoteEnemy enemy = Enemy(targetId);
                Assert.IsNotNull(enemy, "target " + targetId + " left the registry before an own hit");
                if (Mathf.Abs(enemy.transform.position.x - fixture.Player.transform.position.x) > 1f)
                    yield return WalkToX(fixture, enemy.transform.position.x, 0.8f, 10f, "target " + targetId);
                yield return Swing(fixture, log);
                yield return fixture.Frames(12);
            }
        }

        // Fire-time click on a pressed control: the press is queued early and the release at the
        // agreed instant, so both editors' purchase intents leave within one frame of that instant.
        internal static IEnumerator PressAndHold(MapEntryPlayFixture fixture, Vector2 point)
        {
            InputSystem.QueueStateEvent(fixture.Mouse, new MouseState { position = point });
            yield return fixture.Frames(2);
            InputSystem.QueueStateEvent(fixture.Mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return fixture.Frames(2);
        }

        internal static void Release(MapEntryPlayFixture fixture, Vector2 point) =>
            InputSystem.QueueStateEvent(fixture.Mouse, new MouseState { position = point });
    }
}
