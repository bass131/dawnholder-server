#nullable enable
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>Scene-owned guild view. The supplier owns values and pending; this window emits only level-up intent.</summary>
    [DisallowMultipleComponent]
    public sealed class GuildWindow : MonoBehaviour
    {
        GuildDisplayModel? _source;
        bool _subscribed;
        bool _inputAllowed;
        bool _destroyed;
        GuildWindowLayout? _layout;
        GuildHousingLayout? _housing;
        TMP_Text _name = null!;
        TMP_Text _members = null!;
        TMP_Text _online = null!;
        TMP_Text _treasury = null!;
        TMP_Text _level = null!;
        TMP_Text _cost = null!;
        TMP_Text _blacksmith = null!;
        TMP_Text _status = null!;
        TMP_Text _result = null!;
        Button _levelUp = null!;
        Button _close = null!;
        Button _refresh = null!;
        string? _connectionStatus;
        string? _connectionResult;
        bool _refreshAllowed;

        public bool IsOpen { get; private set; }
        public event Action? LevelUpRequested;
        public event Action? RefreshRequested;

        public static GuildWindow BuildRuntime(Transform parent)
        {
            var root = new GameObject("GuildWindow", typeof(RectTransform));
            root.SetActive(false);
            root.transform.SetParent(parent, worldPositionStays: false);
            var window = root.AddComponent<GuildWindow>();
            window.BuildView();
            root.SetActive(true);
            return window;
        }

        /// <summary>Call on the Unity main thread. Rebinding replaces the actual subscribed source.</summary>
        public void Bind(GuildDisplayModel? model)
        {
            if (_destroyed) return;
            if (!ReferenceEquals(_source, model))
            {
                Unsubscribe();
                _source = model;
            }
            Subscribe();
            Render();
        }

        public void SetOpen(bool open)
        {
            if (_destroyed) return;
            IsOpen = open;
            Render();
        }

        public void SetInputAllowed(bool allowed)
        {
            if (_destroyed) return;
            _inputAllowed = allowed;
            Render();
        }

        internal void SetConnectionStatus(string? status, string? result, bool refreshAllowed)
        {
            _connectionStatus = status;
            _connectionResult = result;
            _refreshAllowed = refreshAllowed;
            Render();
        }

        void OnEnable()
        {
            Subscribe();
            Render();
        }

        void OnDisable()
        {
            Unsubscribe();
            Render();
        }

        void OnDestroy()
        {
            _destroyed = true;
            Unsubscribe();
            _source = null;
            if (_levelUp != null) _levelUp.onClick.RemoveListener(RequestLevelUp);
            if (_close != null) _close.onClick.RemoveListener(Close);
            if (_refresh != null) _refresh.onClick.RemoveListener(RequestRefresh);
            LevelUpRequested = null;
            RefreshRequested = null;
        }

        void LateUpdate() => _layout?.Resize();

        void Subscribe()
        {
            if (_destroyed || !isActiveAndEnabled || _subscribed || _source == null) return;
            _source.Changed += Render;
            _subscribed = true;
        }

        void Unsubscribe()
        {
            if (_subscribed && _source != null) _source.Changed -= Render;
            _subscribed = false;
        }

        bool CanRequest()
        {
            if (_destroyed || !isActiveAndEnabled || !IsOpen || !_inputAllowed || !_subscribed) return false;
            GuildStatusDisplay? display = _source?.Guild;
            return display != null && display.LevelUp == GuildActionAvailability.Available && !display.LevelUpPending;
        }

        void RequestLevelUp()
        {
            // Button.onClick can be invoked directly; inspect the current source instead of cached interactivity.
            if (CanRequest()) LevelUpRequested?.Invoke();
        }

        void Close() => SetOpen(false);

        void RequestRefresh()
        {
            if (!_destroyed && isActiveAndEnabled && IsOpen && _refreshAllowed)
                RefreshRequested?.Invoke();
        }

        void Render()
        {
            if (_layout == null || _destroyed) return;
            GuildStatusDisplay? display = _source?.Guild;
            _housing?.SetBaseLevel(display?.BaseLevel);
            _name.text = display?.GuildName ?? "길드 정보를 기다리는 중";
            _members.text = display != null ? "회원 " + display.MemberCount.ToString("N0") : "회원 —";
            _online.text = display != null ? "온라인 " + display.OnlineCount.ToString("N0") : "온라인 —";
            _treasury.text = display != null ? "공동 금고 " + display.Treasury.ToString("N0") : "공동 금고 —";
            _level.text = display != null ? $"거점 레벨 {display.BaseLevel} / {display.MaxBaseLevel}" : "거점 레벨 —";
            _cost.text = display == null ? "다음 비용 —" : display.LevelUp == GuildActionAvailability.MaxLevel
                ? "다음 성장 없음" : "다음 비용 " + display.LevelUpCost.ToString("N0");
            _blacksmith.text = display == null ? "대장간 상태 확인 중"
                : display.BlacksmithUnlocked ? "대장간 해금" : "대장간 잠김";
            _status.text = display == null ? "서버 동기화 대기 중" : display.LevelUpPending
                ? "거점 성장 처리 중" : GuildWindowLayout.AvailabilityText(display.LevelUp);
            _result.text = display != null ? GuildWindowLayout.ResultText(display.LevelUpResult) : string.Empty;
            if (_connectionStatus != null) _status.text = _connectionStatus;
            if (_connectionResult != null) _result.text = _connectionResult;
            _levelUp.interactable = CanRequest();
            _refresh.interactable = _refreshAllowed;
            _layout.SetVisibility(isActiveAndEnabled && IsOpen);
        }

        void BuildView()
        {
            _layout = new GuildWindowLayout(gameObject, "Guild", "길드 현황", right: false);
            _layout.Inset(354f, 150f);
            _close = _layout.CloseButton();
            _close.onClick.AddListener(Close);
            _housing = new GuildHousingLayout((RectTransform)_close.transform.parent);
            _refresh = _layout.RefreshButton();
            _refresh.onClick.AddListener(RequestRefresh);
            _name = _layout.Label("GuildName", 54f, 28f);
            TMP_Text notice = _layout.Label("AutoJoinNotice", 108f, 24f);
            notice.fontSizeMax = 17f;
            notice.text = "접속하면 기본 길드에 자동 가입됩니다.";
            _members = _layout.Label("MemberCount", 82f, 26f, width: 196f);
            _online = _layout.Label("OnlineCount", 82f, 26f, left: 228f, width: 196f);
            _treasury = _layout.Label("Treasury", 324f, 28f);
            _level = _layout.Label("BaseLevel", 356f, 28f);
            _cost = _layout.Label("LevelUpCost", 384f, 28f, width: 202f);
            _blacksmith = _layout.Label("BlacksmithStatus", 384f, 28f, left: 228f, width: 196f);
            _status = _layout.Label("ActionStatus", 412f, 28f);
            _result = _layout.Label("ActionResult", 440f, 42f);
            TMP_Text lifetime = _layout.Label("LifetimeNotice", 482f, 26f);
            lifetime.fontSizeMax = 17f;
            lifetime.text = "공동 성장은 서버 프로세스 동안 유지됩니다.";
            _levelUp = _layout.ActionButton("LevelUp", "거점 레벨업");
            _levelUp.onClick.AddListener(RequestLevelUp);
            GuildHousingLayout.PlaceActions(_levelUp, _refresh);
            Render();
        }
    }
}
