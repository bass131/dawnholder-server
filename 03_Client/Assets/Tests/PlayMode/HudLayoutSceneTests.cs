using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Dawnholder.Client.UI;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Scene integration and lifetime checks. The separate live captures measure both actual Game view sizes.
    public sealed class HudLayoutSceneTests
    {
        MapEntryPlayFixture _fixture;
        readonly List<GameObject> _owned = new();

        [SetUp] public void Setup() => _fixture = new MapEntryPlayFixture();

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            yield return _fixture.Cleanup();
            foreach (GameObject owned in _owned)
                if (owned != null) UnityEngine.Object.Destroy(owned);
            _owned.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActualHud_UsesOnlyRealSlotsAndTheOriginalMapAspect_WithoutDecorativeBlockers()
        {
            yield return ReadyHud();
            Transform hud = HudController.Instance.transform;
            Assert.AreEqual(1, hud.GetComponents<HudLayout>().Length);
            Assert.AreEqual(10, hud.GetComponent<Canvas>().sortingOrder);
            Assert.IsTrue(hud.Find("skill_panel/skill_slot_1").gameObject.activeInHierarchy);
            Assert.IsFalse(hud.Find("skill_panel/skill_slot_2").gameObject.activeInHierarchy, "Knight has no E slot");
            foreach (string hidden in new[] { "character_status/MpSlider", "character_status/ExpSlider",
                "skill_panel/skill_slot_3", "skill_panel/skill_slot_4", "skill_panel/skill_slot_5" })
                Assert.IsFalse(hud.Find(hidden).gameObject.activeInHierarchy, hidden + " must not invent a resource or skill");

            TMP_Text hp = hud.Find("character_status/HpText").GetComponent<TMP_Text>();
            Assert.IsTrue(hp.isActiveAndEnabled);
            Assert.AreEqual("HP 37 / 100", hp.text, "the fixture sent exactly 37/100");
            Component map = hud.Find("MinimapPlaceholder/MinimapView").GetComponent(PartyInviteLayout.UiType("RawImage"));
            Texture mapTexture = (Texture)PartyInviteLayout.Get(map, "texture");
            Rect mapRect = map.GetComponent<RectTransform>().rect;
            Assert.AreEqual(320, mapTexture.width, "the original RT width");
            Assert.AreEqual(256, mapTexture.height, "the original RT height");
            Assert.AreEqual(5f / 4f, mapRect.width / mapRect.height, 0.0001f);
            Assert.AreEqual(new Rect(0, 0, 1, 1), (Rect)PartyInviteLayout.Get(map, "uvRect"), "the original map is not cropped");

            foreach (string area in new[] { "character_status", "skill_panel", "Gold_bar", "MinimapPlaceholder", "Quest_Hud", "Party_Info" })
            {
                foreach (Component graphic in hud.Find(area).GetComponentsInChildren(PartyInviteLayout.UiType("Graphic")))
                    if (ScreenUiProbe.IsShown(graphic))
                        Assert.IsFalse((bool)PartyInviteLayout.Get(graphic, "raycastTarget"), area + "/" + graphic.name + " is HUD decoration, not a button");
            }
        }

        [UnityTest]
        public IEnumerator DisableEnableAndUiRecreation_DoNotDuplicateSceneOwnedArtworkOrLeaveAPersistentLayout()
        {
            yield return ReadyHud();
            GameObject hud = HudController.Instance.gameObject;
            int before = PortraitFrames(hud.transform);
            Assert.AreEqual(2, before, "one scene-owned frame for each actual portrait asset");
            for (int cycle = 0; cycle < 3; cycle++)
            {
                hud.SetActive(false);
                yield return _fixture.Frames(1);
                hud.SetActive(true);
                yield return _fixture.Frames(2);
                Assert.AreEqual(before, PortraitFrames(hud.transform));
            }
            for (int cycle = 0; cycle < 2; cycle++)
            {
                yield return SceneManager.UnloadSceneAsync("UI");
                yield return _fixture.Frames(2);
                Assert.IsEmpty(UnityEngine.Object.FindObjectsByType<HudLayout>(), "unloaded UI owns no persistent layout");
                yield return SceneManager.LoadSceneAsync("UI", LoadSceneMode.Additive);
                yield return WaitHud();
                yield return _fixture.Frames(2);
                HudLayout[] layouts = UnityEngine.Object.FindObjectsByType<HudLayout>();
                Assert.AreEqual(1, layouts.Length);
                Assert.AreEqual("UI", layouts[0].gameObject.scene.name);
                Assert.AreEqual(before, PortraitFrames(layouts[0].transform));
            }
        }

        [UnityTest]
        public IEnumerator JoinedParty_TransportClose_HidesTheOldRosterBeforeTheOwnersQueuedCleanup()
        {
            yield return ReadyHud();
            _fixture.Peer.Send(new S_PartyUpdate { partyId = 11, leaderEntityId = 700,
                member0EntityId = 700, member1EntityId = 701, member0Class = 0, member1Class = 1 }.Write());
            CanvasGroup group = HudController.Instance.transform.Find("Party_Info").GetComponent<CanvasGroup>();
            yield return MapEntryPlayFixture.Wait(() => PartyState.Instance.InParty && group.alpha == 1f, "joined party shown", 3);
            UnityClientSession closing = _fixture.Session;
            MainThreadDispatcher[] paused = UnityEngine.Object.FindObjectsByType<MainThreadDispatcher>()
                .Where(dispatcher => dispatcher.enabled).ToArray();
            Assert.IsNotEmpty(paused, "fixture: the connection's dispatcher exists");
            try
            {
                foreach (MainThreadDispatcher dispatcher in paused) dispatcher.enabled = false;
                _fixture.Peer.Dispose();
                yield return MapEntryPlayFixture.Wait(() => closing.IsClosed, "closed party transport", 5);
                yield return _fixture.Frames(3);
                Assert.IsTrue(ReferenceEquals(UnityClientSession.Instance, closing), "owner cleanup is still queued");
                Assert.IsTrue(PartyState.Instance.InParty, "the old mirror has not been reset yet");
                Assert.AreEqual(0f, group.alpha);
                Assert.IsFalse(group.blocksRaycasts);
                Assert.IsEmpty(ScreenUiProbe.ShownTextsUnder(group.transform));
            }
            finally
            {
                foreach (MainThreadDispatcher dispatcher in paused)
                    if (dispatcher != null) dispatcher.enabled = true;
            }
            yield return MapEntryPlayFixture.Wait(() => UnityClientSession.Instance == null, "party connection cleanup", 5);
        }

        [UnityTest]
        public IEnumerator ReplacedPartyMirror_DetachesFromTheActualOldSource_AndDisableDestroyDetachFromTheNewOne()
        {
            yield return ReadyHud();
            PartyState previous = PartyState.Instance;
            Assert.IsNotNull(previous, "fixture: the connection created the party mirror");
            PartyMemberHud hud = UnityEngine.Object.FindFirstObjectByType<PartyMemberHud>();
            Assert.IsNotNull(hud);
            yield return _fixture.Frames(2);
            Assert.AreEqual(1, PartyHudSubscribers(previous));

            // A destroyed Unity object retains its managed event; this exposes detaching from Instance by mistake.
            UnityEngine.Object.Destroy(previous.gameObject);
            yield return _fixture.Frames(1);
            var replacementRoot = new GameObject("ReplacementPartyMirror");
            _owned.Add(replacementRoot);
            PartyState current = replacementRoot.AddComponent<PartyState>();
            current.ApplyUpdate(11, 700, 700, 701, 0, 1);
            yield return _fixture.Frames(2);
            Assert.AreEqual(0, PartyHudSubscribers(previous), "old managed event no longer holds this HUD");
            Assert.AreEqual(1, PartyHudSubscribers(current));
            string[] shown = hud.GetComponentsInChildren<TMP_Text>().Select(text => text.text).ToArray();
            Assert.Contains("기사 (700)", shown);
            Assert.Contains("마법사 (701)", shown);

            hud.gameObject.SetActive(false);
            yield return _fixture.Frames(1);
            Assert.AreEqual(0, PartyHudSubscribers(current));
            hud.gameObject.SetActive(true);
            yield return _fixture.Frames(2);
            Assert.AreEqual(1, PartyHudSubscribers(current));
            UnityEngine.Object.Destroy(hud.gameObject);
            yield return _fixture.Frames(2);
            Assert.AreEqual(0, PartyHudSubscribers(current));
        }

        IEnumerator ReadyHud()
        {
            yield return _fixture.EnterScriptedTown();
            _fixture.Hp(37, 100);
            yield return _fixture.WaitMap(0);
            yield return WaitHud();
            yield return _fixture.Frames(2);
        }

        static IEnumerator WaitHud() => MapEntryPlayFixture.Wait(() => HudController.Instance != null &&
            SceneManager.GetSceneByName("UI").isLoaded, "the additive HUD scene", 10);

        static int PortraitFrames(Transform hud) => hud.Find("character_status").GetComponentsInChildren<Transform>(true)
            .Count(node => node.name == "PortraitFrame");

        static int PartyHudSubscribers(PartyState source)
        {
            FieldInfo storage = typeof(PartyState).GetField(nameof(PartyState.OnPartyUpdated), BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(storage);
            var subscribers = storage.GetValue(source) as Delegate;
            return subscribers?.GetInvocationList().Count(callback => callback.Target is PartyMemberHud) ?? 0;
        }
    }
}
