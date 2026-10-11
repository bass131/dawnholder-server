using System.Collections;
using System.Linq;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Independent verification of dungeon goal P8 (01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md
    // 「던전 패킷·창 PR의 요구」) for the boss room: 「사냥터·보스방에는 붙이지 않는다」. The PR's label test enters
    // HuntingGround only. The label is found by its text, shown or not, so a hidden copy in BossRoom also fails.
    public sealed class DungeonEntranceLabelBossRoomSceneTests
    {
        const string EntranceLabel = "던전 입구";

        MapEntryPlayFixture _fixture;

        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();
        [UnityTearDown] public IEnumerator Teardown() => _fixture.Cleanup();

        [UnityTest]
        public IEnumerator BossRoom_HasNoEntranceLabel_WhileTownHasOne()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(40, 100);
            yield return _fixture.WaitMap(0);
            yield return _fixture.Frames(5);
            int townLabels = Labels();

            _fixture.Peer.Send(new S_MapTransition { destMapId = 2, spawnX = 2, spawnY = 0 }.Write());
            yield return MapEntryPlayFixture.Wait(() => _fixture.Session.Entry.MapId == 2 && _fixture.Session.Entry.SceneReady,
                "BossRoom scene", 10);
            _fixture.Hp(41, 100);
            yield return _fixture.WaitMap(2);
            yield return _fixture.Frames(10);
            int bossRoomLabels = Labels();
            Debug.Log($"[dungeon P8 verify] Town labels {townLabels}, BossRoom labels {bossRoomLabels}");

            Assert.AreEqual(1, townLabels, "control: the same count finds Town's label");
            Assert.AreEqual(0, bossRoomLabels, "P8 BossRoom has no 「던전 입구」 label");
        }

        // Every TMP text in a loaded scene with the label's words, active or not (as InventoryPanelProbe.LivePanels counts).
        static int Labels() => Resources.FindObjectsOfTypeAll<TMP_Text>()
            .Count(text => text.gameObject.scene.IsValid() && text.text != null && text.text.Contains(EntranceLabel));
    }
}
