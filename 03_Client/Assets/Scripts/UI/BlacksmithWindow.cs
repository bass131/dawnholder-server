#nullable enable
using System;
using Shared.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>Scene-owned personal enhancement view. It reports intent without spending or confirming growth.</summary>
    [DisallowMultipleComponent]
    public sealed class BlacksmithWindow : MonoBehaviour
    {
        GuildDisplayModel? _source;
        bool _subscribed;
        bool _inputAllowed;
        bool _destroyed;
        GuildWindowLayout? _layout;
        TMP_Text _characterClass = null!;
        TMP_Text _level = null!;
        TMP_Text _bonus = null!;
        TMP_Text _gold = null!;
        TMP_Text _material = null!;
        TMP_Text _status = null!;
        TMP_Text _result = null!;
        Button _enhance = null!;

        public bool IsOpen { get; private set; }
        public event Action? EnhanceRequested;

        public static BlacksmithWindow BuildRuntime(Transform parent)
        {
            var root = new GameObject("BlacksmithWindow", typeof(RectTransform));
            root.SetActive(false);
            root.transform.SetParent(parent, worldPositionStays: false);
            var window = root.AddComponent<BlacksmithWindow>();
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
            if (_enhance != null) _enhance.onClick.RemoveListener(RequestEnhance);
            EnhanceRequested = null;
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
            BlacksmithDisplay? display = _source?.Blacksmith;
            return display != null && display.Enhance == GuildActionAvailability.Available && !display.EnhancePending;
        }

        void RequestEnhance()
        {
            // Eligibility belongs to the supplier; neither a cached button flag nor displayed costs can authorize it.
            if (CanRequest()) EnhanceRequested?.Invoke();
        }

        void Render()
        {
            if (_layout == null || _destroyed) return;
            BlacksmithDisplay? display = _source?.Blacksmith;
            _characterClass.text = display != null ? ClassText(display.CharacterClass) : "직업 확인 중";
            _level.text = display != null
                ? $"강화 단계 {display.EnhanceLevel} / {display.MaxEnhanceLevel}" : "강화 단계 —";
            _bonus.text = display != null ? "공격 보너스 " + display.AttackBonus.ToString("N0") : "공격 보너스 —";
            bool maximum = display?.Enhance == GuildActionAvailability.MaxLevel;
            _gold.text = display == null ? "다음 골드 비용 —" : maximum
                ? "다음 골드 비용 없음" : "다음 골드 비용 " + display.GoldCost.ToString("N0");
            _material.text = display == null ? "다음 재료 비용 —" : maximum
                ? "다음 재료 비용 없음" : "다음 재료 비용 " + display.MaterialCost.ToString("N0");
            _status.text = display == null ? "서버 동기화 대기 중" : display.EnhancePending
                ? "무기 강화 처리 중" : GuildWindowLayout.AvailabilityText(display.Enhance);
            _result.text = display != null ? GuildWindowLayout.ResultText(display.EnhanceResult) : string.Empty;
            _enhance.interactable = CanRequest();
            _layout.SetVisibility(isActiveAndEnabled && IsOpen, _inputAllowed);
        }

        static string ClassText(CharacterClass characterClass)
        {
            switch (characterClass)
            {
                case CharacterClass.Knight: return "직업 · 기사";
                case CharacterClass.Mage: return "직업 · 마법사";
                default: return "알 수 없는 직업";
            }
        }

        void BuildView()
        {
            _layout = new GuildWindowLayout(gameObject, "Blacksmith", "대장간", right: true);
            _characterClass = _layout.Label("CharacterClass", 58f, 48f);
            _level = _layout.Label("EnhanceLevel", 106f, 48f);
            _bonus = _layout.Label("AttackBonus", 154f, 48f);
            _gold = _layout.Label("GoldCost", 202f, 48f);
            _material = _layout.Label("MaterialCost", 250f, 48f);
            _status = _layout.Label("ActionStatus", 298f, 48f);
            _result = _layout.Label("ActionResult", 346f, 48f);
            _enhance = _layout.ActionButton("Enhance", "무기 강화");
            _enhance.onClick.AddListener(RequestEnhance);
            Render();
        }
    }
}
