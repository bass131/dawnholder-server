using System;
using System.Collections;
using System.Linq;
using Dawnholder.Client.Bootstrap;
using Dawnholder.Client.Network;
using Dawnholder.Client.Scenes;
using NUnit.Framework;
using Shared.Protocol;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Dawnholder.Client.Tests.PlayMode
{
    // The shipped CharacterSelect scene through Start/Update with the three-slot create form, written before the
    // implementation (goal 2026-10-31-demo-autopilot-character-select, PR-A completion row A7). As in
    // CharacterMenuMotionSceneTests, EntryWirePeer is the loopback wire stand-in: it answers the handshake and login,
    // this test answers the list and the create. Empty list → slot 1 → CreateForm → name and Mage → Create → the peer
    // reads C_CharacterCreateRequest with that slot, class and raw name → create success → the slot row returns → slot 1
    // → Submit → the peer reads C_CharacterSelectRequest for the created character.
    // Stand-in only: no GameServer or database is run or observed here (that is PR268's DB run).
    public sealed class CharacterSelectWideSceneTests
    {
        const string TypedName = "새벽지기";
        // The Mage button, CharacterMenuView class 1; Knight (0) is the draft default, so Mage proves the input travels.
        const byte MageClass = 1;
        // Any valid character ID in GUID D form, as CharacterMenuMotionSceneTests' list rows use.
        const string CreatedId = "55555555-5555-4555-8555-555555555551";

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
        public IEnumerator RealCharacterSelect_EmptySlotCreatesThroughTheForm_ThenTheNewCharacterIsSelected()
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
            AccountLoadout.SessionAccountName = "wide_" + Guid.NewGuid().ToString("N").Substring(0, 11);
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

            yield return MapEntryPlayFixture.Wait(() => _peer.Count(PacketID.C_CharacterListRequest) > 0, "real menu list request on wire");
            var listRequest = _peer.Last<C_CharacterListRequest>(PacketID.C_CharacterListRequest);
            _peer.Send(new S_CharacterListResult
            {
                requestId = listRequest.requestId, result = 0, occupiedMask = 0,
                slot1CharacterIdD = "", slot1Name = "",
                slot2CharacterIdD = "", slot2Name = "",
                slot3CharacterIdD = "", slot3Name = "",
            }.Write());
            yield return MapEntryPlayFixture.Wait(() => Interactable(panel.Find("Slot1")), "empty wire list unlocks the slots");

            Click(panel, "Slot1");
            yield return null;
            Transform form = panel.Find("CreateForm");
            bool formExists = form != null;
            Assert.IsTrue(formExists, "exists: Panel/CreateForm");
            bool formOpen = form.gameObject.activeInHierarchy;
            bool slotRowHidden = !panel.Find("Slot1").gameObject.activeInHierarchy;
            Assert.IsTrue(formOpen, "A7 the empty slot opens the create form in the real scene");
            Assert.IsTrue(slotRowHidden, "A3 the slot row is hidden while the create form is open");
            SetInputText(panel, "CreateForm/Name", TypedName);
            Click(panel, "CreateForm/Mage");
            Click(panel, "CreateForm/Create");

            yield return MapEntryPlayFixture.Wait(() => _peer.Count(PacketID.C_CharacterCreateRequest) > 0, "create request on wire");
            var createRequest = _peer.Last<C_CharacterCreateRequest>(PacketID.C_CharacterCreateRequest);
            TestContext.Out.WriteLine($"real scene={_scene.name}; create request={createRequest.requestId} slot={createRequest.slot} " +
                $"class={createRequest.classId} name=「{createRequest.rawName}」");
            Assert.AreEqual(1, _peer.Count(PacketID.C_CharacterCreateRequest), "A4 one create request on the wire");
            Assert.AreEqual((byte)1, createRequest.slot, "A7 the wire create names slot 1");
            Assert.AreEqual(MageClass, createRequest.classId, "A7 the wire create names the chosen Mage class");
            Assert.AreEqual(TypedName, createRequest.rawName, "A7 the wire create carries the typed name unchanged");

            // Fields as CharacterMenuPresenterTests.Created. The create success itself fills slot 1
            // (CharacterMenuPresenter applies reply.Character); the client sends no list request after it, so none is answered.
            _peer.Send(new S_CharacterCreateResult
            {
                requestId = createRequest.requestId, slot = 1, characterIdD = CreatedId, name = TypedName, classId = MageClass,
            }.Write());
            yield return MapEntryPlayFixture.Wait(() => !form.gameObject.activeInHierarchy, "the created slot closes the create form");
            bool slotRowBack = panel.Find("Slot1").gameObject.activeInHierarchy;
            bool submitLockedAfterCreate = !Interactable(panel.Find("Submit"));
            Assert.IsTrue(slotRowBack, "A4 the slot row returns after the create");
            Assert.IsTrue(submitLockedAfterCreate, "A4 no slot is chosen after the create, so Submit stays locked");

            Click(panel, "Slot1");
            yield return null;
            Click(panel, "Submit");
            yield return MapEntryPlayFixture.Wait(() => _peer.Count(PacketID.C_CharacterSelectRequest) > 0, "select request on wire");
            var selectRequest = _peer.Last<C_CharacterSelectRequest>(PacketID.C_CharacterSelectRequest);
            TestContext.Out.WriteLine($"select request={selectRequest.requestId} character={selectRequest.characterIdD}");

            Assert.AreEqual(1, _peer.Count(PacketID.C_CharacterSelectRequest), "A5 one select request on the wire");
            Assert.AreEqual(CreatedId, selectRequest.characterIdD, "A7 the wire select names the created character");
            Assert.IsEmpty(_peer.Errors, "loopback fixture delivered valid frames");
        }

        // PlayMode intentionally has no uGUI assembly reference; keep the established fixture's reflection boundary
        // (CharacterMenuMotionSceneTests.UiComponent). TMP_InputField derives from uGUI's Selectable, so it is reached the
        // same way.
        static Component ComponentNamed(Transform target, string fullName) => target.GetComponents<Component>()
            .FirstOrDefault(component => component != null && component.GetType().FullName == fullName);

        static bool Interactable(Transform target)
        {
            Component button = target == null ? null : ComponentNamed(target, "UnityEngine.UI.Button");
            return button != null && (bool)button.GetType().GetProperty("interactable").GetValue(button);
        }

        static void Click(Transform panel, string path)
        {
            Transform target = panel.Find(path);
            Assert.IsTrue(target != null, $"exists: Panel/{path}");
            Component button = ComponentNamed(target, "UnityEngine.UI.Button");
            Assert.IsTrue(button != null, $"exists: Button on Panel/{path}");
            Assert.IsTrue((bool)button.GetType().GetProperty("interactable").GetValue(button), $"real menu button Panel/{path} is unlocked");
            ((UnityEvent)button.GetType().GetProperty("onClick").GetValue(button)).Invoke();
        }

        static void SetInputText(Transform panel, string path, string value)
        {
            Transform target = panel.Find(path);
            Assert.IsTrue(target != null, $"exists: Panel/{path}");
            Component input = ComponentNamed(target, "TMPro.TMP_InputField");
            Assert.IsTrue(input != null, $"exists: TMP_InputField on Panel/{path}");
            input.GetType().GetProperty("text").SetValue(input, value);
        }
    }
}
