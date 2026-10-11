#nullable enable
using Dawnholder.Client.Network;
using Dawnholder.Client.State;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Dawnholder.Client.UI
{
    /// <summary>Scene-owned binding and click entry; the connection controller owns requests and recovery.</summary>
    [DisallowMultipleComponent]
    public sealed class InventoryPanel : MonoBehaviour
    {
        InventoryPanelView? _view;
        InventoryState? _source;
        UnityClientSession? _session;
        InventoryRequestController? _requests;
        long _renderedEpoch = -1;
        MapEntryState _renderedEntryState;
        bool _renderedCanSend;

        public static InventoryPanel? Instance { get; private set; }

        public static InventoryPanel BuildRuntime(Transform parent)
        {
            if (Instance != null) return Instance;
            var root = new GameObject("InventoryPanel", typeof(RectTransform));
            root.SetActive(false);
            root.transform.SetParent(parent, worldPositionStays: false);
            var panel = root.AddComponent<InventoryPanel>();
            panel._view = new InventoryPanelView(root, panel.UseSlot, panel.Refresh);
            root.SetActive(true);
            return panel;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnEnable()
        {
            BindSources();
            Render();
            UpdateVisibility();
        }

        void OnDisable()
        {
            UnbindSources();
            _view?.SetVisibility(false, false);
        }

        void OnDestroy()
        {
            UnbindSources();
            _view?.Dispose();
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            // Sources can appear late or be replaced while this scene remains enabled.
            bool rebound = BindSources();
            long epoch = _session != null ? _session.Entry.Epoch : -1;
            MapEntryState state = _session != null ? _session.Entry.State : MapEntryState.Closed;
            bool canSend = _session != null && _session.CanSendGameplay;
            if (rebound || epoch != _renderedEpoch || state != _renderedEntryState || canSend != _renderedCanSend)
                Render();
            if (CanAcceptInput() && Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            {
                _session!.IsInventoryPanelOpen = !_session.IsInventoryPanelOpen;
                Render();
            }
            UpdateVisibility();
            _view?.Resize();
        }

        bool IsInGameplayScene() => IsGameplayScene(SceneManager.GetActiveScene().name) &&
            IsGameplayScene(gameObject.scene.name);

        bool CanAcceptInput() => IsInGameplayScene() && _session != null && _session.CanSendGameplay &&
            Time.timeScale > 0f &&
            (SceneTransition.Instance == null || SceneTransition.Instance.ActiveRequest == null ||
                SceneTransition.Instance.ActiveRequest.IsFinished);

        void UpdateVisibility()
        {
            bool visible = IsInGameplayScene() && _session != null && _session.IsInventoryPanelOpen;
            // Hide only the view: the scene binding and connection's Ready query stay alive while closed.
            _view?.SetVisibility(visible, visible && CanAcceptInput());
        }

        bool BindSources()
        {
            var source = InventoryState.Instance;
            var session = UnityClientSession.Instance;
            var requests = session?.Inventory;
            bool changed = false;
            if (!ReferenceEquals(_source, source))
            {
                // CLR reference identity is intentional: destroyed Unity objects can still own managed events.
                if (!ReferenceEquals(_source, null)) _source!.OnInventoryChanged -= Render;
                _source = source;
                if (source != null) source.OnInventoryChanged += Render;
                changed = true;
            }
            if (!ReferenceEquals(_requests, requests))
            {
                if (_requests != null) _requests.Changed -= Render;
                _requests = requests;
                if (_requests != null) _requests.Changed += Render;
                changed = true;
            }
            _session = session;
            return changed;
        }

        void UnbindSources()
        {
            if (!ReferenceEquals(_source, null)) _source!.OnInventoryChanged -= Render;
            if (_requests != null) _requests.Changed -= Render;
            _source = null;
            _requests = null;
            _session = null;
        }

        void UseSlot(int index)
        {
            if (!isActiveAndEnabled) return;
            BindSources();
            if (_source == null || _requests == null) return;
            _requests.TryUse(_source.GetSlot(index).ItemId);
            Render();
        }

        void Refresh()
        {
            if (!isActiveAndEnabled) return;
            BindSources();
            _requests?.Refresh();
            Render();
        }

        void Render()
        {
            _renderedEpoch = _session != null ? _session.Entry.Epoch : -1;
            _renderedEntryState = _session != null ? _session.Entry.State : MapEntryState.Closed;
            _renderedCanSend = _session != null && _session.CanSendGameplay;
            _view?.Render(_source, _requests, _renderedCanSend);
        }

        static bool IsGameplayScene(string name) =>
            name == "Town" || name == "HuntingGround" || name == "BossRoom" || name == "GameplayTest";
    }
}
