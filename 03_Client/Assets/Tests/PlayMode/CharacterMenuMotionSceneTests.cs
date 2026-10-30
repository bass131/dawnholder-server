using System;
using System.Collections;
using System.Linq;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Network;
using Dawnholder.Client.Scenes;
using NUnit.Framework;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // Loads the shipped CharacterSelect scene and crosses its Start/Update/OnDestroy path. EntryWirePeer supplies
    // framed handshake/login/list responses only; this test does not run or claim to verify GameServer or a database.
    public sealed class CharacterMenuMotionSceneTests
    {
        NetworkService _network;
        EntryWirePeer _peer;
        Scene _scene;
        int _oldPort;
        string _oldAccount;
        float _oldScale, _oldVolume;
        bool _captured, _ownsConnection;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            try
            {
                _peer?.Dispose();
                if (_ownsConnection && _network != null) _network.Disconnect();
                if (_peer != null) yield return MapEntryPlayFixture.Wait(() => _peer.Completion.IsCompleted, "menu peer shutdown", 3);
                if (_scene.IsValid() && _scene.isLoaded)
                {
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(_scene);
                    if (unload != null) yield return MapEntryPlayFixture.Wait(() => unload.isDone, "owned CharacterSelect unload");
                }
            }
            finally
            {
                if (_captured)
                {
                    if (_network != null) MapEntryPlayFixture.Set(_network, "_serverPort", _oldPort);
                    AccountLoadout.SessionAccountName = _oldAccount;
                    Time.timeScale = _oldScale;
                    AudioListener.volume = _oldVolume;
                }
            }
        }

        [UnityTest]
        public IEnumerator RealCharacterSelect_SelectedArtMovesWhilePaused_ControlsStayStill_AndUnloadDisposesIt()
        {
            Assert.IsTrue(Application.isPlaying);
            Assert.IsFalse(SceneManager.GetSceneByName("CharacterSelect").isLoaded, "fixture owns only the scene it loads");
            yield return MapEntryPlayFixture.Wait(() => NetworkService.Instance != null, "persistent NetworkService");
            _network = NetworkService.Instance;
            Assert.IsFalse(_network.IsConnected, "do not adopt another connection");
            _oldPort = MapEntryPlayFixture.Field<int>(_network, "_serverPort");
            _oldAccount = AccountLoadout.SessionAccountName;
            _oldScale = Time.timeScale;
            _oldVolume = AudioListener.volume;
            _captured = true;
            AudioListener.volume = 0f;
            Time.timeScale = 1f;
            AccountLoadout.SessionAccountName = "menu_" + Guid.NewGuid().ToString("N").Substring(0, 11);
            _peer = EntryWirePeer.Listen();
            MapEntryPlayFixture.Set(_network, "_serverPort", _peer.Port);
            // Explicit loopback override affects this connection only; no PlayerPrefs write or real server contact.
            _ownsConnection = true;
            _network.ConnectCharacterMenu("127.0.0.1");
            AsyncOperation load = SceneManager.LoadSceneAsync("CharacterSelect", LoadSceneMode.Additive);
            yield return MapEntryPlayFixture.Wait(() => load.isDone, "CharacterSelect load");
            _scene = SceneManager.GetSceneByName("CharacterSelect");
            CharacterSelectController controller = _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<CharacterSelectController>(true)).Single();
            yield return MapEntryPlayFixture.Wait(() => controller.transform.Find("CharacterMenu/Panel") != null, "actual scene controller creates menu");
            Transform panel = controller.transform.Find("CharacterMenu/Panel");
            yield return MapEntryPlayFixture.Wait(() => _peer.Count(PacketID.C_CharacterListRequest) > 0, "real menu request on wire");
            var request = _peer.Last<C_CharacterListRequest>(PacketID.C_CharacterListRequest);
            _peer.Send(new S_CharacterListResult
            {
                requestId = request.requestId, result = 0, occupiedMask = 3,
                slot1CharacterIdD = "44444444-4444-4444-8444-444444444441", slot1Name = "가람", slot1ClassId = 0,
                slot2CharacterIdD = "44444444-4444-4444-8444-444444444442", slot2Name = "누리", slot2ClassId = 1,
                slot3CharacterIdD = "", slot3Name = "",
            }.Write());
            yield return MapEntryPlayFixture.Wait(() => panel.Find("Slot1/Label").GetComponent<TMP_Text>().text.Contains("가람"), "wire list reaches rendered menu");
            var firstArt = (RectTransform)panel.Find("Slot1/ClassArt");
            var secondArt = (RectTransform)panel.Find("Slot2/ClassArt");
            Vector2 firstRest = firstArt.anchoredPosition;
            Vector2 secondRest = secondArt.anchoredPosition;
            Click(panel.Find("Slot1"));
            yield return null;
            // Only art may move. Sample every text rectangle and every button rectangle in the real menu.
            RectTransform[] fixedRects = panel.GetComponentsInChildren<RectTransform>(true)
                .Where(rect => rect.GetComponent<TMP_Text>() != null || UiComponent(rect, "Button") != null).ToArray();
            Vector2[] positions = fixedRects.Select(rect => rect.anchoredPosition).ToArray();
            Assert.Greater(fixedRects.Length, 10, "precondition: observer includes labels and controls");
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.6f);
            Vector2 moved = firstArt.anchoredPosition;
            TestContext.Out.WriteLine($"real scene={_scene.name}; request={request.requestId}; timeScale={Time.timeScale}; art rest={firstRest} now={moved}; controls={fixedRects.Length}");
            NegativeControl(() => RequireArtMotion(firstRest, firstRest));
            RequireArtMotion(firstRest, moved);
            var wrongPositions = (Vector2[])positions.Clone();
            wrongPositions[0] += Vector2.up;
            NegativeControl(() => RequireStationary(positions, wrongPositions));
            RequireStationary(positions, fixedRects.Select(rect => rect.anchoredPosition).ToArray());
            Assert.AreEqual(secondRest, secondArt.anchoredPosition, "unselected art remains still");
            Click(panel.Find("Slot2"));
            Assert.AreEqual(firstRest, firstArt.anchoredPosition, "changing selection returns the old art to rest");
            Click(panel.Find("Slot3"));
            Assert.AreEqual(secondRest, secondArt.anchoredPosition, "empty-slot selection stops the other art");
            Assert.IsTrue(panel.Find("CreateForm").gameObject.activeInHierarchy, "actual empty-slot branch opens the creation form");
            Assert.IsEmpty(_peer.Errors, "loopback fixture delivered valid frames");
            var ownedEffects = panel.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(component => component != null && component.GetType().Name == "CharacterArtBob").ToArray();
            Assert.AreEqual(3, ownedEffects.Length, "precondition: all three art effects were created by the view");
            Time.timeScale = 1f;
            AsyncOperation unloadScene = SceneManager.UnloadSceneAsync(_scene);
            yield return MapEntryPlayFixture.Wait(() => unloadScene.isDone, "actual controller OnDestroy");
            yield return null;
            NegativeControl(() => RequireDestroyed(new[] { false }));
            RequireDestroyed(ownedEffects.Select(component => component == null).ToArray());
            Assert.IsTrue(panel == null, "controller destruction disposes the menu canvas");
        }

        // PlayMode intentionally has no uGUI assembly reference; keep the established fixture's reflection boundary.
        static Component UiComponent(Transform target, string type) => target.GetComponents<Component>()
            .FirstOrDefault(component => component != null && component.GetType().FullName == "UnityEngine.UI." + type);
        static void Click(Transform target)
        {
            Component button = UiComponent(target, "Button");
            Assert.IsNotNull(button);
            Assert.IsTrue((bool)button.GetType().GetProperty("interactable").GetValue(button), "real menu button is unlocked");
            ((UnityEvent)button.GetType().GetProperty("onClick").GetValue(button)).Invoke();
        }
        static void RequireArtMotion(Vector2 rest, Vector2 actual)
        {
            Assert.AreEqual(rest.x, actual.x, "only a small vertical drawing motion is allowed");
            Assert.That(actual.y - rest.y, Is.GreaterThan(0.05f).And.LessThanOrEqualTo(3f), "selected art moves slowly while timeScale is zero");
        }
        static void RequireStationary(Vector2[] expected, Vector2[] actual) => CollectionAssert.AreEqual(expected, actual, "buttons and labels do not bob with art");
        static void RequireDestroyed(bool[] destroyed) => Assert.IsTrue(destroyed.All(value => value), "no owned motion component survives scene unload");
        static void NegativeControl(TestDelegate wrongObservation)
        {
            var error = Assert.Throws<AssertionException>(wrongObservation);
            TestContext.Out.WriteLine("NEGATIVE CONTROL rejected: " + error.Message);
        }
    }
}
