using System.Collections;
using System.Collections.Generic;
using Dawnholder.Client.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using static Dawnholder.Client.Tests.PlayMode.GuildSizeGeometry;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Preserved before the implementation: the guild window opened alone in Town leaves the HUD's character status (top
    // left) and minimap (top right) uncovered (lead contract pr-b-red-contract-v2 T3 「보존 단정」, answer msg_978f10e2d973 2).
    // Town is entered with the loopback scripted peer as HudLayoutSceneTests.cs:166-172 does, never the GameServer or
    // port 7777, and the window is Town's own: the one GuildUiController builds (CombatBootstrap.cs:155) and opens with G.
    // The HUD canvas and the guild canvas draw into one RenderTexture camera with their scalers as built (supplement
    // msg_107ba9cc8f94 (1)); rectangles and covered area as HudBottomWindowLayoutSceneTests.cs:696-715 measures them.
    public sealed class GuildWindowSizeHudTests
    {
        const int GameServerPort = 7777;
        static readonly string[] s_hudAreas = { "character_status", "MinimapPlaceholder" };

        MapEntryPlayFixture _fixture;
        GuildSizeSurface _surface;

        [SetUp]
        public void Setup()
        {
            _fixture = new MapEntryPlayFixture();
            _surface = null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            _surface?.Dispose();
            _surface = null;
            yield return _fixture.Cleanup();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Town_GuildWindowOpenedWithG_BothSizes_LeavesCharacterStatusAndMinimapUncovered()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(37, 100);
            yield return _fixture.WaitMap(0);
            yield return MapEntryPlayFixture.Wait(() => HudController.Instance != null && SceneManager.GetSceneByName("UI").isLoaded,
                "the additive HUD scene", 10);
            yield return GuildUiConnectionTestTools.Synchronize(_fixture);
            Record($"scripted peer on loopback port {_fixture.Peer.Port}");
            Assert.AreNotEqual(GameServerPort, _fixture.Peer.Port, "fixture: the scripted peer, not the GameServer port");
            GuildWindow[] windows = UnityEngine.Object.FindObjectsByType<GuildWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, windows.Length, "fixture: Town has its own single guild window");
            GuildWindow guild = windows[0];
            Assert.IsFalse(guild.IsOpen, "precondition: the guild window starts closed");
            yield return GuildGameplayWindowTestTools.KeyFrame(_fixture, Key.G);
            yield return MapEntryPlayFixture.Wait(() => guild.IsOpen, "the guild window opened with G", 5);
            Transform hud = HudController.Instance.transform;
            Canvas hudCanvas = hud.GetComponentInParent<Canvas>(true).rootCanvas;
            Canvas guildCanvas = guild.GetComponent<Canvas>();
            Assert.AreNotSame(hudCanvas, guildCanvas, "fixture: the HUD and the guild window have their own canvases");

            var covered = new List<string>();
            foreach (Vector2Int size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 720) })
            {
                _surface = GuildSizeSurface.Create(size.x, size.y);
                _surface.Attach(hudCanvas);
                _surface.Attach(guildCanvas);
                yield return GuildSizeSurface.Settle();
                Record($"surface {_surface.Size}: {_surface.Describe()}");
                Assert.IsEmpty(_surface.FixtureProblems(), $"fixture: the HUD and guild canvases draw into the {_surface.Size} texture as built");
                Assert.IsTrue(guild.IsOpen && ScreenUiProbe.IsShown(guild), "precondition: the guild window is still open and shown");
                Rect panel = PartyInviteLayout.ScreenRectOf(guild.transform.Find(GuildWindowsUnderTest.GuildPanelName));
                Assert.Greater(Area(panel), 0f, "precondition: the guild panel has an area");
                foreach (string name in s_hudAreas)
                {
                    Transform area = hud.Find(name);
                    Assert.IsNotNull(area, $"fixture: HUD {name}");
                    Assert.IsTrue(ScreenUiProbe.IsShown(area), $"precondition: HUD {name} is shown");
                    Rect rect = PartyInviteLayout.ScreenRectOf(area);
                    Assert.Greater(Area(rect), 0f, $"precondition: HUD {name} has an area");
                    float overlap = OverlapArea(panel, rect);
                    Record($"{_surface.Size}: guild panel {Format(panel)}, {name} {Format(rect)}, covered {overlap:F1} px²");
                    if (overlap > 0f) covered.Add($"{_surface.Size}: the guild panel covers {overlap:F1} px² of {name}");
                }
                _surface.Dispose();
                _surface = null;
                yield return null;
            }

            Assert.IsEmpty(covered, "T3 preserved: the guild window alone covers neither the character status nor the minimap");
        }

        static void Record(string line)
        {
            TestContext.Out.WriteLine("[guild size hud] " + line);
            Debug.Log("[guild size hud] " + line);
        }
    }
}
