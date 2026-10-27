#nullable enable
using System;
using System.Collections;
using System.Linq;
using Dawnholder.Client.Combat;
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.Scenes;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using Shared.GameData;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dawnholder.Client.Demo
{
    // Scripted demo run (user decision 21A, 10-28): a knight and a mage player, each launched with
    // -demo-role, play town → party → hunting ground → boss → guild growth with no human input so the
    // run can be recorded. Each step waits on state this client receives from the server (scene, party,
    // enemies, guild snapshot), never on the other client. Steps log "[Demo] step ..." with UTC time so the
    // edit can cut the recording at those moments.
    // The demo server is the DEBUG build: F8 completes the 20-kill quest that gates the boss portal and F9
    // funds the stronghold, so one minute of footage can show the whole loop.
    public sealed class DemoAutopilot : MonoBehaviour
    {
        const float TownPortalX = 25f;
        const float HuntingGroundPortalX = 56f;
        const float KnightReach = 1.1f;
        const float MageReach = 4.5f;
        const float AttackInterval = 0.6f;
        const float MageSkillInterval = 3.5f;

        DemoLaunchOptions _options = null!;
        DemoKeyboard _keys = null!;
        int _step;
        LocalPlayerMovement? _me;
        float _progressX = float.NaN;
        float _progressCheckedAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            DemoLaunchOptions? options = DemoLaunchOptions.Parse(Environment.GetCommandLineArgs());
            if (options == null) return;

            var host = new GameObject("DemoAutopilot");
            DontDestroyOnLoad(host);
            host.AddComponent<DemoAutopilot>()._options = options;
        }

        // The virtual keyboard goes in once Input System has reported the real devices, before Town's
        // PlayerInput pairs a keyboard.
        void Start()
        {
            _keys = DemoKeyboard.Install();
            StartCoroutine(Run());
        }

        void OnDestroy() => _keys?.ReleaseAll();

        IEnumerator Run()
        {
            Log("start", $"role={_options.Role} account={_options.Account}");
            yield return EnterGame();
            yield return FormParty();
            yield return TravelThroughPortal(TownPortalX, "HuntingGround");
            yield return HuntThenUnlockBoss();
            yield return TravelThroughPortal(HuntingGroundPortalX, "BossRoom", waitForPartner: true);
            yield return FightBossAndReturn();
            yield return GuildGrowth();
            _keys.ReleaseAll();
            Log("done", string.Empty);
        }

        // ── Steps ────────────────────────────────────────────────────────────────

        IEnumerator EnterGame()
        {
            yield return WaitFor(() => SceneIs("MainMenu") && FindFirstObjectByType<MainMenuController>() != null, 30f, "main menu");
            yield return new WaitForSeconds(1f);
            foreach (TMP_InputField field in FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None))
            {
                if (field.name == "AccountNameInput") field.text = _options.Account;
                if (field.name == "ServerHostInput") field.text = "127.0.0.1";
            }
            FindFirstObjectByType<MainMenuController>()?.OnStartClicked();
            Log("login", _options.Account);

            yield return WaitFor(() => SceneIs("CharacterSelect") && FindFirstObjectByType<CharacterSelectController>() != null, 20f, "character select");
            yield return new WaitForSeconds(1f);
            CharacterSelectController? select = FindFirstObjectByType<CharacterSelectController>();
            if (_options.Role == DemoRole.Knight) select?.OnKnightClicked();
            else select?.OnMageClicked();
            Log("class", _options.Role.ToString());

            yield return WaitUntilPlayable("Town");
            Log("town", string.Empty);
        }

        IEnumerator FormParty()
        {
            if (_options.Role == DemoRole.Knight)
            {
                yield return WaitFor(() => RemoteEntityRegistry.Instance != null && RemoteEntityRegistry.Instance.Entities.Any(), 90f, "mage in town");
                yield return new WaitForSeconds(1.5f);
                for (int attempt = 0; attempt < 8 && !InParty; attempt++)
                {
                    yield return _keys.Tap(Key.P);
                    yield return WaitFor(() => InParty, 4f, null);
                }
            }
            else
            {
                yield return WaitFor(() => PartyState.Instance != null && PartyState.Instance.HasPendingInvite, 90f, "party invite");
                yield return new WaitForSeconds(1.2f);
                ClickButton(PartyInvitePopup.Instance, "AcceptButton");
                yield return WaitFor(() => InParty, 8f, "party joined");
            }
            Log("party", InParty ? "joined" : "not joined");
            yield return new WaitForSeconds(1.5f);
        }

        IEnumerator TravelThroughPortal(float portalX, string nextScene, bool waitForPartner = false)
        {
            yield return MoveToX(portalX, 0.35f, 45f);
            // The two players hunt at different speeds; meeting at the boss portal keeps the boss fight a party fight.
            // The first to see the other lingers a moment, and the later one goes straight on once the first is gone.
            if (waitForPartner)
            {
                yield return WaitFor(() => PartnerNear(portalX, 4f) || !PartnerOnMap, 25f, "partner at portal");
                if (PartnerOnMap) yield return new WaitForSeconds(1f);
            }
            for (int attempt = 0; attempt < 6 && !SceneIs(nextScene); attempt++)
            {
                yield return _keys.Tap(Key.W);
                yield return WaitFor(() => SceneIs(nextScene), 3f, null);
                if (!SceneIs(nextScene)) yield return MoveToX(portalX, 0.25f, 5f);
            }
            yield return WaitUntilPlayable(nextScene);
            Log("arrived", nextScene);
        }

        IEnumerator HuntThenUnlockBoss()
        {
            float end = Time.time + 30f;
            while (Time.time < end && MyX < HuntingGroundPortalX - 6f)
            {
                if (TryNearestEnemy(10f, out Transform? enemy) && enemy != null) yield return Engage(enemy, 8f);
                else yield return MoveToX(MyX + 4f, 0.3f, 3f);
            }
            yield return _keys.Tap(Key.F8); // DEBUG cheat: complete the 20-kill quest that opens the boss portal.
            Log("quest", "F8");
        }

        IEnumerator FightBossAndReturn()
        {
            float end = Time.time + 180f;
            while (Time.time < end && !ClearWindowReady())
            {
                if (TryNearestEnemy(40f, out Transform? boss) && boss != null) yield return Engage(boss, 4f);
                else yield return MoveToX(MyX - 3f, 0.3f, 2f);
            }
            _keys.ReleaseAll();
            Log("boss", ClearWindowReady() ? "cleared" : "timeout");
            yield return new WaitForSeconds(3f); // hold on the reward window and treasury toast
            ClickButton(DungeonClearRewardWindow.Instance, "Confirm");
            yield return WaitUntilPlayable("Town");
            Log("returned", "Town");
        }

        IEnumerator GuildGrowth()
        {
            UnityClientSession? session = UnityClientSession.Instance;
            if (session == null) yield break;
            GuildRequestController guild = session.Guild;
            yield return WaitFor(() => guild.State.HasSnapshot, 10f, "guild snapshot");
            yield return RefreshGuild(guild);

            if (_options.Role == DemoRole.Knight)
            {
                yield return new WaitForSeconds(1f);
                yield return _keys.Tap(Key.G);
                yield return new WaitForSeconds(1.5f);
                yield return UpgradeStronghold(guild);
                yield return new WaitForSeconds(5f); // the centred growth effect plays here
                yield return _keys.Tap(Key.G);
                yield break;
            }

            // The mage waits for the knight's upgrade to unlock the shared guild's blacksmith; a separate
            // guild per player falls back to its own upgrade.
            yield return WaitFor(() => guild.State.BlacksmithUnlocked, 25f, null);
            if (!guild.State.BlacksmithUnlocked) yield return UpgradeStronghold(guild);
            yield return EnhanceAtBlacksmith(session, guild);
        }

        // The last snapshot came while the player was in the dungeon, so its availability still reads
        // "not in town"; the purchase gate needs one taken after arrival.
        IEnumerator RefreshGuild(GuildRequestController guild)
        {
            yield return WaitFor(() => !guild.IsPending, 6f, "guild idle");
            long before = guild.State.SnapshotSequence;
            if (!guild.Refresh()) Debug.LogWarning("[Demo] guild refresh not sent");
            yield return WaitFor(() => guild.State.SnapshotSequence > before && !guild.IsPending, 6f, "guild refresh");
        }

        IEnumerator UpgradeStronghold(GuildRequestController guild)
        {
            yield return RefreshGuild(guild);
            if (guild.State.StrongholdLevel >= guild.State.MaxStrongholdLevel)
            {
                Log("stronghold", "already max");
                yield break;
            }
            if (guild.State.Treasury < guild.State.StrongholdCost)
            {
                yield return _keys.Tap(Key.F9); // DEBUG cheat: credit the stronghold cost to the guild treasury.
                yield return WaitFor(() => guild.State.Treasury >= guild.State.StrongholdCost, 6f, "treasury");
            }
            int before = guild.State.StrongholdLevel;
            if (!guild.TryUpgradeStronghold())
                Debug.LogWarning($"[Demo] stronghold upgrade not sent: {guild.State.StrongholdAvailability}");
            yield return WaitFor(() => guild.State.StrongholdLevel > before, 8f, "stronghold level-up");
            Log("stronghold", $"level={guild.State.StrongholdLevel}");
        }

        IEnumerator EnhanceAtBlacksmith(UnityClientSession session, GuildRequestController guild)
        {
            GameObject? npc = GameObject.Find("Npc_BlackSmith");
            if (npc == null)
            {
                Log("blacksmith", "npc missing");
                yield break;
            }
            yield return MoveToX(npc.transform.position.x, 0.6f, 25f);
            yield return new WaitForSeconds(0.8f);
            FindFirstObjectByType<GuildUiController>()?.OpenBlacksmith();
            yield return new WaitForSeconds(1.5f);
            yield return RefreshGuild(guild);
            if (guild.State.WeaponAvailability != GuildResult.Success)
            {
                Log("enhance", $"skipped {guild.State.WeaponAvailability}");
                yield break;
            }
            int before = guild.State.WeaponLevel;
            guild.TryEnhanceWeapon();
            yield return WaitFor(() => guild.State.WeaponLevel > before, 8f, "weapon enhance");
            Log("enhance", $"level={guild.State.WeaponLevel}");
            yield return new WaitForSeconds(4f);
        }

        // ── Movement and combat ──────────────────────────────────────────────────

        IEnumerator MoveToX(float targetX, float tolerance, float timeout)
        {
            float end = Time.time + timeout;
            while (Time.time < end)
            {
                LocalPlayerMovement? me = Me;
                if (me == null)
                {
                    yield return null;
                    continue;
                }
                float dx = targetX - me.transform.position.x;
                if (Mathf.Abs(dx) <= tolerance) break;
                Steer(dx > 0f ? 1 : -1);
                if (BlockedWhileSteering(me)) yield return _keys.Tap(Key.Space);
                yield return null;
            }
            Steer(0);
        }

        // A step or ledge stops horizontal progress; the caller jumps over it while still holding the direction.
        bool BlockedWhileSteering(LocalPlayerMovement me)
        {
            if (Time.time - _progressCheckedAt < 0.35f) return false;
            float x = me.transform.position.x;
            bool blocked = !float.IsNaN(_progressX) && Mathf.Abs(x - _progressX) < 0.05f && me.OnGround;
            _progressX = x;
            _progressCheckedAt = Time.time;
            return blocked;
        }

        IEnumerator Engage(Transform enemy, float maxSeconds)
        {
            float reach = _options.Role == DemoRole.Knight ? KnightReach : MageReach;
            float end = Time.time + maxSeconds;
            float nextAttack = 0f;
            float nextSkill = Time.time + 1f;
            while (Time.time < end && enemy != null && enemy.gameObject.activeInHierarchy)
            {
                LocalPlayerMovement? me = Me;
                if (me == null || ClearWindowReady()) break;
                float dx = enemy.position.x - me.transform.position.x;
                if (Mathf.Abs(dx) > reach)
                {
                    Steer(dx > 0f ? 1 : -1);
                    if (BlockedWhileSteering(me)) yield return _keys.Tap(Key.Space);
                    yield return null;
                    continue;
                }
                Steer(0);
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + AttackInterval;
                    bool skill = _options.Role == DemoRole.Mage && Time.time >= nextSkill;
                    if (skill) nextSkill = Time.time + MageSkillInterval;
                    yield return _keys.Tap(skill ? Key.Q : Key.Enter);
                }
                yield return null;
            }
            Steer(0);
        }

        void Steer(int direction)
        {
            if (direction > 0)
            {
                _keys.Release(Key.A);
                _keys.Hold(Key.D);
            }
            else if (direction < 0)
            {
                _keys.Release(Key.D);
                _keys.Hold(Key.A);
            }
            else
            {
                _keys.Release(Key.A);
                _keys.Release(Key.D);
                _progressX = float.NaN;
            }
        }

        bool TryNearestEnemy(float maxDistance, out Transform? nearest)
        {
            nearest = null;
            LocalPlayerMovement? me = Me;
            EnemyRegistry? registry = EnemyRegistry.Instance;
            if (me == null || registry == null) return false;
            Vector2 at = me.transform.position;
            float best = maxDistance;
            foreach ((int _, Transform enemy) in registry.EnemyTransforms)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                Vector2 delta = (Vector2)enemy.position - at;
                if (Mathf.Abs(delta.y) > 2.5f) continue; // another floor of the stage
                float distance = Mathf.Abs(delta.x);
                if (distance >= best) continue;
                best = distance;
                nearest = enemy;
            }
            return nearest != null;
        }

        // ── State reads ──────────────────────────────────────────────────────────

        // The local player is rebuilt with each scene, so a destroyed cache entry means "look again".
        LocalPlayerMovement? Me
        {
            get
            {
                if (_me == null) _me = FindFirstObjectByType<LocalPlayerMovement>();
                return _me;
            }
        }

        float MyX => Me != null ? Me.transform.position.x : 0f;

        static bool InParty => PartyState.Instance != null && PartyState.Instance.InParty;

        // Remote entities on a field map are the other players; enemies live in EnemyRegistry.
        static bool PartnerNear(float x, float within) =>
            RemoteEntityRegistry.Instance != null && RemoteEntityRegistry.Instance.Entities.Any(entity =>
                entity != null && Mathf.Abs(entity.transform.position.x - x) <= within);

        static bool PartnerOnMap =>
            RemoteEntityRegistry.Instance != null && RemoteEntityRegistry.Instance.Entities.Any(entity => entity != null);

        static bool SceneIs(string name) => SceneManager.GetActiveScene().name == name;

        static bool ClearWindowReady()
        {
            DungeonClearRewardWindow? window = DungeonClearRewardWindow.Instance;
            return window != null && window.isActiveAndEnabled && FindButton(window, "Confirm") is Button { interactable: true };
        }

        IEnumerator WaitUntilPlayable(string scene)
        {
            yield return WaitFor(() => SceneIs(scene) && Me != null &&
                UnityClientSession.Instance != null && UnityClientSession.Instance.CanSendGameplay, 30f, $"{scene} playable");
            yield return new WaitForSeconds(1f);
        }

        IEnumerator WaitFor(Func<bool> condition, float timeout, string? what)
        {
            float end = Time.time + timeout;
            while (!condition() && Time.time < end) yield return null;
            if (!condition() && what != null) Debug.LogWarning($"[Demo] wait timeout: {what} ({timeout:F0}s)");
        }

        static Button? FindButton(Component? root, string name) =>
            root == null ? null : root.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);

        void ClickButton(Component? root, string name)
        {
            Button? button = FindButton(root, name);
            if (button != null && button.isActiveAndEnabled && button.interactable) button.onClick.Invoke();
            else Debug.LogWarning($"[Demo] button not clickable: {name}");
        }

        void Log(string step, string detail)
        {
            _step++;
            Debug.Log($"[Demo] step {_step} {step} t={Time.realtimeSinceStartup:F1} utc={DateTime.UtcNow:HH:mm:ss.f} {detail}");
        }
    }
}
