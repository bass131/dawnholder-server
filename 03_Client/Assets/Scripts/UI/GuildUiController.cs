#nullable enable
using System;
using Dawnholder.Client.Gameplay;
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using Shared.GameData;
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
        GuildGrowthEffectView? _growthEffect;
        Action<GuildChangeKind>? _growthConfirmed;
        object? _growthBinding;
        bool _areaFailureReported;
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
            {
                controller._prompt = TownBlacksmithPrompt.BuildRuntime(root.transform, controller);
                // This independent canvas shares the bootstrap lifetime, never the entrance canvas's scaler.
                try { controller._growthEffect = GuildGrowthEffectView.BuildRuntime(parent); }
                catch (Exception error)
                {
                    Debug.LogError($"Guild growth effect creation failed: {error}", controller);
                }
            }
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
            if (_growthEffect != null) Destroy(_growthEffect.gameObject);
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

        void LateUpdate()
        {
            // Use the existing scene owner for gate/resize observation; no queued confirmation survives a closed gate.
            if (!CanPurchase()) _growthEffect?.CancelAll();
            UpdateGrowthArea();
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
            GuildRequestController? requests = session?.Guild;
            if (!ReferenceEquals(_session, session) || !ReferenceEquals(_requests, requests))
            {
                DetachRequests();
                _session = session;
                _requests = requests;
                if (requests != null && session != null)
                {
                    requests.Changed += SourceChanged;
                    var binding = new object();
                    _growthBinding = binding;
                    // Capture this binding, including its identity across disable/re-enable of the same session.
                    _growthConfirmed = kind => OnGrowthConfirmed(session, requests, binding, kind);
                    requests.GrowthConfirmed += _growthConfirmed;
                }
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
            DetachRequests();
            if (!ReferenceEquals(_inventory, null)) _inventory!.OnInventoryChanged -= SourceChanged;
            _requests = null;
            _session = null;
            _inventory = null;
            _model.SetGuild(null);
            _model.SetBlacksmith(null);
            _dirty = true;
        }

        void DetachRequests()
        {
            // Invalidate first: an already captured invoke-list can still call the old delegate after removal.
            _growthBinding = null;
            if (_requests != null)
            {
                _requests.Changed -= SourceChanged;
                if (_growthConfirmed != null) _requests.GrowthConfirmed -= _growthConfirmed;
            }
            _growthConfirmed = null;
            if (_growthEffect != null) _growthEffect.CancelAll();
        }

        void OnGrowthConfirmed(UnityClientSession session, GuildRequestController requests, object binding,
            GuildChangeKind kind)
        {
            if (!ReferenceEquals(_growthBinding, binding) || !ReferenceEquals(_session, session) ||
                !ReferenceEquals(_requests, requests) || !ReferenceEquals(UnityClientSession.Instance, session) ||
                !session.IsCurrentConnection) return;
            if (!CanPurchase())
            {
                if (_growthEffect != null) _growthEffect.CancelAll();
                return;
            }

            // Correlation and deduplication belong to GuildRequestController; read the confirmed value at arrival.
            int level;
            switch (kind)
            {
                case GuildChangeKind.StrongholdUpgrade: level = requests.State.StrongholdLevel; break;
                case GuildChangeKind.WeaponEnhance: level = requests.State.WeaponLevel; break;
                default: return;
            }
            if (_growthEffect == null)
            {
                Debug.LogError($"Guild growth effect is unavailable for {kind}, level {level}.", this);
                return;
            }
            // The canvas may not have completed its first layout when a server event arrives.
            Canvas.ForceUpdateCanvases();
            if (!UpdateGrowthArea()) return;
            try
            {
                if (_growthEffect.TryPlayConfirmed(kind, level)) return;
                _growthEffect.CancelAll();
                Debug.LogWarning($"Guild growth effect refused {kind}, level {level}.", this);
            }
            catch (Exception error)
            {
                _growthEffect.CancelAll();
                Debug.LogError($"Guild growth effect playback failed for {kind}, level {level}: {error}", this);
            }
        }

        bool UpdateGrowthArea()
        {
            if (_growthEffect == null) return false;
            try
            {
                if (GuildWindowLayout.TryGetGrowthArea((RectTransform)_growthEffect.transform, out Rect area) &&
                    _growthEffect.TrySetAvailableArea(area))
                {
                    _areaFailureReported = false;
                    return true;
                }
                RecordAreaFailure("No valid available rectangle was accepted.");
            }
            catch (Exception error) { RecordAreaFailure(error.ToString()); }
            return false;
        }

        void RecordAreaFailure(string reason)
        {
            if (_growthEffect != null) _growthEffect.CancelAll();
            if (!_areaFailureReported) Debug.LogError($"Guild growth effect layout failed: {reason}", this);
            _areaFailureReported = true;
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
            if (!purchase && _growthEffect != null) _growthEffect.CancelAll();
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
