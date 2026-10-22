#nullable enable
using Dawnholder.Client.Gameplay;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>Scene-owned composition. Session.Guild remains the sole request and pending owner.</summary>
    [DisallowMultipleComponent]
    public sealed class GuildUiController : MonoBehaviour
    {
        readonly GuildDisplayModel _model = new GuildDisplayModel();
        UnityClientSession? _session;
        GuildRequestController? _requests;
        InventoryState? _inventory;
        GuildWindow _guild = null!;
        BlacksmithWindow _smith = null!;
        Button _entrance = null!;
        TownBlacksmithPrompt? _prompt;
        bool _dirty = true;
        (long epoch, MapEntryState state, bool ready, bool input, bool alive, bool town) _renderedGate;

        internal bool CanOpenWindows => isActiveAndEnabled && IsCurrentScene() &&
            _session != null && _session.CanSendGameplay && Time.timeScale > 0f && !HasPriorityWindow() &&
            (SceneTransition.Instance == null || SceneTransition.Instance.ActiveRequest == null ||
                SceneTransition.Instance.ActiveRequest.IsFinished);

        internal static GuildUiController BuildRuntime(Transform parent)
        {
            var root = new GameObject("GuildUi", typeof(RectTransform));
            root.SetActive(false);
            root.transform.SetParent(parent, false);
            var controller = root.AddComponent<GuildUiController>();
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 11;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            controller._entrance = GuildWindowLayout.Entrance(root.transform, "GuildEntrance", "길드 · G", new Vector2(-16f, 16f));
            controller.EnsureWindows();
            if (root.scene.name == "Town")
                controller._prompt = TownBlacksmithPrompt.BuildRuntime(root.transform, controller);
            root.SetActive(true);
            return controller;
        }

        internal void OpenBlacksmith()
        {
            if (!isActiveAndEnabled) return;
            BindSources();
            if (!CanOpenWindows || _prompt == null || !_prompt.IsLocalPlayerNear()) return;
            _smith.SetOpen(true);
            QueryIfMissing();
            Render();
        }

        void OnEnable()
        {
            if (_entrance == null) return;
            _entrance.onClick.AddListener(ToggleGuild);
            AttachWindows();
            BindSources();
            Render();
        }

        void OnDisable()
        {
            if (_entrance != null) _entrance.onClick.RemoveListener(ToggleGuild);
            DetachWindows();
            UnbindSources();
        }

        void OnDestroy()
        {
            UnbindSources();
            if (_guild != null) Destroy(_guild.gameObject);
            if (_smith != null) Destroy(_smith.gameObject);
        }

        void Update()
        {
            EnsureWindows();
            BindSources();
            var gate = CurrentGate();
            // Ready, entry epoch, death and menu changes need no Guild.Changed notification.
            if (_dirty || gate != _renderedGate) Render();
            if (CanOpenWindows && Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
                ToggleGuild();
        }

        void EnsureWindows()
        {
            if (_guild == null)
            {
                // Each window keeps its own root CanvasScaler; nesting under the entrance canvas
                // would turn it into a nested canvas and lose its screen-space layout.
                _guild = GuildWindow.BuildRuntime(transform.parent);
                _guild.Bind(_model);
                _guild.LevelUpRequested += Upgrade;
                _guild.RefreshRequested += Refresh;
                _dirty = true;
            }
            if (_smith == null)
            {
                _smith = BlacksmithWindow.BuildRuntime(transform.parent);
                _smith.Bind(_model);
                _smith.EnhanceRequested += Enhance;
                _smith.RefreshRequested += Refresh;
                _dirty = true;
            }
        }

        void AttachWindows()
        {
            // Initial construction and re-enable share this path without duplicate callbacks.
            DetachWindows();
            _guild.Bind(_model);
            _guild.LevelUpRequested += Upgrade;
            _guild.RefreshRequested += Refresh;
            _smith.Bind(_model);
            _smith.EnhanceRequested += Enhance;
            _smith.RefreshRequested += Refresh;
        }

        void DetachWindows()
        {
            if (_guild != null)
            {
                _guild.LevelUpRequested -= Upgrade;
                _guild.RefreshRequested -= Refresh;
                _guild.SetInputAllowed(false);
                _guild.SetOpen(false);
                _guild.Bind(null);
            }
            if (_smith != null)
            {
                _smith.EnhanceRequested -= Enhance;
                _smith.RefreshRequested -= Refresh;
                _smith.SetInputAllowed(false);
                _smith.SetOpen(false);
                _smith.Bind(null);
            }
        }

        void BindSources()
        {
            UnityClientSession? session = UnityClientSession.Instance;
            if (session != null && !session.IsCurrentConnection) session = null;
            if (!ReferenceEquals(_session, session))
            {
                if (_requests != null) _requests.Changed -= SourceChanged;
                _session = session;
                _requests = session?.Guild;
                if (_requests != null) _requests.Changed += SourceChanged;
                _guild.SetOpen(false);
                _smith.SetOpen(false);
                _model.SetGuild(null);
                _model.SetBlacksmith(null);
                _dirty = true;
            }
            InventoryState? inventory = InventoryState.Instance;
            if (ReferenceEquals(_inventory, inventory)) return;
            if (!ReferenceEquals(_inventory, null)) _inventory!.OnInventoryChanged -= SourceChanged;
            _inventory = inventory;
            if (inventory != null) inventory.OnInventoryChanged += SourceChanged;
            _dirty = true;
        }

        void UnbindSources()
        {
            if (_requests != null) _requests.Changed -= SourceChanged;
            if (!ReferenceEquals(_inventory, null)) _inventory!.OnInventoryChanged -= SourceChanged;
            _requests = null;
            _session = null;
            _inventory = null;
            _model.SetGuild(null);
            _model.SetBlacksmith(null);
            _dirty = true;
        }

        void SourceChanged() => _dirty = true;

        bool IsCurrentScene() => gameObject.scene == SceneManager.GetActiveScene() &&
            (gameObject.scene.name == "Town" || gameObject.scene.name == "HuntingGround" ||
                gameObject.scene.name == "BossRoom" || gameObject.scene.name == "GameplayTest");

        bool HasPriorityWindow() => (_session != null && _session.IsInventoryPanelOpen) || NpcDialogPanel.IsShown ||
            (DungeonClearRewardState.Instance != null && DungeonClearRewardState.Instance.HasPendingFor(_session));

        bool CanPurchase() => CanOpenWindows && _session!.Entry.MapId == 0 && gameObject.scene.name == "Town" &&
            _session.Entry.HasHp && _session.Entry.CurrentHp > 0;

        (long epoch, MapEntryState state, bool ready, bool input, bool alive, bool town) CurrentGate() =>
            (_session?.Entry.Epoch ?? -1, _session?.Entry.State ?? MapEntryState.Closed,
                _session != null && _session.CanSendGameplay, CanOpenWindows,
                _session != null && _session.Entry.HasHp && _session.Entry.CurrentHp > 0,
                _session != null && _session.Entry.MapId == 0 && gameObject.scene.name == "Town");

        void ToggleGuild()
        {
            if (!isActiveAndEnabled) return;
            BindSources();
            if (!CanOpenWindows) return;
            _guild.SetOpen(!_guild.IsOpen);
            if (_guild.IsOpen) QueryIfMissing();
            Render();
        }

        void QueryIfMissing()
        {
            // An explicit first entry may query. Reopening a synchronized or pending view never resets it.
            if (_requests != null && !_requests.State.HasSnapshot && !_requests.IsPending) _requests.Refresh();
        }

        void Refresh()
        {
            if (!isActiveAndEnabled) return;
            BindSources();
            if (CanOpenWindows) _requests?.Refresh();
            Render();
        }

        void Upgrade()
        {
            if (!isActiveAndEnabled) return;
            BindSources();
            if (CanPurchase() && _guild.IsOpen && _guild.isActiveAndEnabled) _requests?.TryUpgradeStronghold();
            Render();
        }

        void Enhance()
        {
            if (!isActiveAndEnabled) return;
            BindSources();
            if (CanPurchase() && _smith.IsOpen && _smith.isActiveAndEnabled) _requests?.TryEnhanceWeapon();
            Render();
        }

        void Render()
        {
            _renderedGate = CurrentGate();
            _dirty = false;
            GuildDisplayProjection.Apply(_model, _requests, _session?.CharacterLoad.LoadedClass);
            bool purchase = CanPurchase();
            bool refresh = CanOpenWindows && _requests != null && !_requests.IsPending;
            string result = GuildDisplayProjection.Result(_requests);
            _guild.SetConnectionStatus(GuildDisplayProjection.Status(_requests, _renderedGate.ready,
                _renderedGate.town, _renderedGate.alive, false), result, refresh);
            _smith.SetConnectionStatus(GuildDisplayProjection.Status(_requests, _renderedGate.ready,
                _renderedGate.town, _renderedGate.alive, true), result, refresh,
                GuildDisplayProjection.Balance(_session != null ? _inventory : null, _requests?.State));
            _guild.SetInputAllowed(purchase);
            _smith.SetInputAllowed(purchase);
            _entrance.interactable = CanOpenWindows;
            // Preserve existing inventory, dialogue and reward controls without changing their owners or layout.
            if (!IsCurrentScene() || HasPriorityWindow())
            {
                _guild.SetOpen(false);
                _smith.SetOpen(false);
            }
        }
    }
}
