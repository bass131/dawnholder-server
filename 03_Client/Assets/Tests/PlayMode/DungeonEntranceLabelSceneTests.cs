using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dawnholder.Client.Gameplay;
using Dawnholder.Client.Network;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Dungeon goal P8 (01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md 「던전 패킷·창 PR의 요구」) on the real
    // Town and HuntingGround scenes: Town's only portal carries a 「던전 입구」 label above it that takes no UI raycast,
    // and the hunting ground shows none. The label is whatever the product attaches at runtime; it is found by its text.
    public sealed class DungeonEntranceLabelSceneTests
    {
        const string EntranceLabel = "던전 입구";

        // Town's portal stands at x = 25 (Town.unity, Portal_Town) and the fixture's usual spawn x = 2 leaves it off
        // screen, so this entry spawns beside it: the label must be on screen for its raycast to be observed.
        const float SpawnBesideTownPortal = 22f;

        MapEntryPlayFixture _fixture;

        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();
        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        [UnityTest]
        public IEnumerator TownPortal_HasOneEntranceLabelAboveIt_ThatTakesNoRaycast_AndHuntingGroundHasNone()
        {
            yield return EnterTownBesideThePortal();
            // The camera eases toward the spawned player each frame (CameraFollow), so the view settles first.
            yield return MapEntryPlayFixture.Wait(
                () => Mathf.Abs(Camera.main.transform.position.x - _fixture.Player.transform.position.x) < 0.1f, "camera on the player", 5);
            bool probeSeesAKnownButton = false;
            yield return ProbeSeesTheOpenPanelRefresh(seen => probeSeesAKnownButton = seen);
            PortalTrigger[] townPortals = Object.FindObjectsByType<PortalTrigger>();
            TMP_Text[] townLabels = ScreenUiProbe.ShownTexts(EntranceLabel);
            Vector2 portalPoint = townPortals.Length == 1 ? ScreenUiProbe.ScreenPoint(townPortals[0]) : new Vector2(-1f, -1f);
            bool portalOnScreen = ScreenUiProbe.OnScreen(portalPoint);
            bool above = false;
            bool onScreen = false;
            string comparedBy = "not compared";
            string[] labelHits = { };
            if (townPortals.Length == 1 && townLabels.Length == 1)
            {
                Transform portal = townPortals[0].transform;
                TMP_Text label = townLabels[0];
                // World y for a label in the world (goal wording); screen y when an overlay or camera canvas draws it,
                // because there its transform position is not a world position.
                Canvas canvas = label.GetComponentInParent<Canvas>();
                if (canvas == null || canvas.rootCanvas.renderMode == RenderMode.WorldSpace)
                {
                    comparedBy = $"world y label {label.transform.position.y:F2} portal {portal.position.y:F2}";
                    above = label.transform.position.y > portal.position.y;
                }
                else
                {
                    float portalScreenY = Camera.main.WorldToScreenPoint(portal.position).y;
                    float labelScreenY = ScreenUiProbe.ScreenPoint(label).y;
                    comparedBy = $"screen y label {labelScreenY:F1} portal {portalScreenY:F1}";
                    above = labelScreenY > portalScreenY;
                }
                Vector2 point = ScreenUiProbe.ScreenPoint(label);
                onScreen = ScreenUiProbe.OnScreen(point);
                HashSet<GameObject> labelGroup = ScreenUiProbe.GroupBelowCanvas(label);
                labelHits = ScreenUiProbe.RaycastHits(point)
                    .Where(labelGroup.Contains)
                    .Select(hit => hit.name)
                    .ToArray();
            }

            _fixture.Peer.Send(new S_MapTransition { destMapId = 1, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == 1 && _fixture.Session.Entry.SceneReady,
                "HuntingGround scene", 10);
            _fixture.Hp(31, 100);
            yield return _fixture.WaitMap(1);
            yield return _fixture.Frames(10);
            TMP_Text[] huntingLabels = ScreenUiProbe.ShownTexts(EntranceLabel);
            Debug.Log($"[dungeon P8] screen {Screen.width}x{Screen.height}, Town portals {townPortals.Length} at {portalPoint} " +
                $"(on screen {portalOnScreen}), probe sees the panel's Refresh {probeSeesAKnownButton}, Town labels {townLabels.Length}, " +
                $"{comparedBy}, label on screen {onScreen}, label raycast hits [{string.Join(", ", labelHits)}], " +
                $"HuntingGround labels {huntingLabels.Length}");

            Assert.AreEqual(1, townPortals.Length, "P8 Town has exactly one portal; a second portal must fail here first");
            Assert.IsTrue(portalOnScreen, "fixture: the spawn beside the portal puts it on screen");
            Assert.IsTrue(probeSeesAKnownButton, "fixture: the raycast probe sees a known raycast target (the open panel's Refresh)");
            Assert.AreEqual(1, townLabels.Length, "P8 Town shows one 「던전 입구」 label");
            Assert.IsTrue(above, "P8 the label is above the portal (" + comparedBy + ")");
            Assert.IsTrue(onScreen, "fixture: the label is on screen beside the portal, so its raycast can be observed");
            Assert.IsEmpty(labelHits, "P8 the label takes no UI raycast");
            Assert.AreEqual(0, huntingLabels.Length, "P8 HuntingGround shows no 「던전 입구」 label");
        }

        // Control for "the label takes no raycast": the same probe must see a button the product already has. I opens
        // the inventory panel (its buttons take raycasts while open, client.md), and I closes it again before the label
        // is checked, since a closed panel takes none.
        IEnumerator ProbeSeesTheOpenPanelRefresh(System.Action<bool> done)
        {
            yield return InventoryPanelProbe.OpenWithI(_fixture);
            Component refresh = InventoryPanelProbe.Refresh;
            done(ScreenUiProbe.RaycastHits(ScreenUiProbe.ScreenPoint(refresh)).Any(hit => hit.transform.IsChildOf(refresh.transform)));
            _fixture.Keys(Key.I);
            yield return _fixture.Frames(2);
            _fixture.Keys();
            yield return _fixture.Frames(2);
            Assert.AreEqual(0f, InventoryPanelProbe.Group.alpha, "fixture: I closes the panel again before the label check");
        }

        // MapEntryPlayFixture.EnterScriptedTown with the spawn moved beside the portal; the rest is the same entry.
        IEnumerator EnterTownBesideThePortal()
        {
            yield return _fixture.Prepare(false);
            _fixture.Peer.Send(new S_EnterMap { entityId = 700, spawnX = SpawnBesideTownPortal, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.State == MapEntryState.Entering &&
                _fixture.Session.Entry.SceneReady && _fixture.Player != null, "Town binding before HP");
            _fixture.PairInput();
            _fixture.Hp();
            yield return _fixture.WaitMap(0);
        }
    }
}
