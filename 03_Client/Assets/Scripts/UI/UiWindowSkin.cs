#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dawnholder.Client.UI
{
    /// <summary>Shared window artwork and composition references; no window or input state.</summary>
    public sealed class UiWindowSkin : ScriptableObject
    {
        [SerializeField] Image _panelPrefab = null!;
        [SerializeField] Image _headerPrefab = null!;
        [SerializeField] Button _buttonPrefab = null!;
        [SerializeField] Image _insetPrefab = null!;
        [SerializeField] Image _portraitPrefab = null!;
        [SerializeField] Sprite _dialogPanel = null!;
        [SerializeField] Sprite _emphasisPanel = null!;
        [SerializeField] Sprite _divider = null!;
        [SerializeField] Sprite _acceptNormal = null!;
        [SerializeField] SpriteState _acceptStates;
        [SerializeField] Sprite _declineNormal = null!;
        [SerializeField] SpriteState _declineStates;

        // Only the bright skin of the inventory and character select fills these; the default skin leaves them empty.
        [SerializeField] Sprite _itemSlotEmpty = null!;
        [SerializeField] Sprite _itemSlotFilled = null!;
        [SerializeField] Sprite _itemSlotSelected = null!;
        [SerializeField] Sprite _iconBase = null!;
        [SerializeField] Sprite _materialIcon = null!;
        [SerializeField] Sprite _currencyPouchIcon = null!;
        [SerializeField] Sprite _coinIcon = null!;
        [SerializeField] Sprite _characterSlot = null!;
        [SerializeField] Sprite _characterSlotSelected = null!;
        [SerializeField] Sprite _pedestal = null!;
        [SerializeField] Sprite _selectionGlow = null!;
        [SerializeField] Sprite _inputField = null!;
        [SerializeField] Sprite _knightArt = null!;
        [SerializeField] Sprite _mageArt = null!;

        internal Image PanelPrefab => Required(_panelPrefab, nameof(_panelPrefab));
        internal Image HeaderPrefab => Required(_headerPrefab, nameof(_headerPrefab));
        internal Button ButtonPrefab => Required(_buttonPrefab, nameof(_buttonPrefab));
        internal Image InsetPrefab => Required(_insetPrefab, nameof(_insetPrefab));
        internal Image PortraitPrefab => Required(_portraitPrefab, nameof(_portraitPrefab));
        internal Sprite DialogPanel => Required(_dialogPanel, nameof(_dialogPanel));
        internal Sprite EmphasisPanel => Required(_emphasisPanel, nameof(_emphasisPanel));
        internal Sprite Divider => Required(_divider, nameof(_divider));
        internal Sprite AcceptNormal => Required(_acceptNormal, nameof(_acceptNormal));
        internal SpriteState AcceptStates => _acceptStates;
        internal Sprite DeclineNormal => Required(_declineNormal, nameof(_declineNormal));
        internal SpriteState DeclineStates => _declineStates;
        internal Sprite ItemSlotEmpty => Required(_itemSlotEmpty, nameof(_itemSlotEmpty));
        internal Sprite ItemSlotFilled => Required(_itemSlotFilled, nameof(_itemSlotFilled));
        internal Sprite ItemSlotSelected => Required(_itemSlotSelected, nameof(_itemSlotSelected));
        internal Sprite IconBase => Required(_iconBase, nameof(_iconBase));
        internal Sprite MaterialIcon => Required(_materialIcon, nameof(_materialIcon));
        internal Sprite CurrencyPouchIcon => Required(_currencyPouchIcon, nameof(_currencyPouchIcon));
        internal Sprite CoinIcon => Required(_coinIcon, nameof(_coinIcon));
        internal Sprite CharacterSlot => Required(_characterSlot, nameof(_characterSlot));
        internal Sprite CharacterSlotSelected => Required(_characterSlotSelected, nameof(_characterSlotSelected));
        internal Sprite Pedestal => Required(_pedestal, nameof(_pedestal));
        internal Sprite SelectionGlow => Required(_selectionGlow, nameof(_selectionGlow));
        internal Sprite InputField => Required(_inputField, nameof(_inputField));
        internal Sprite KnightArt => Required(_knightArt, nameof(_knightArt));
        internal Sprite MageArt => Required(_mageArt, nameof(_mageArt));

        // A skin without a slot must fail where the slot is asked for, never fall back to another kit's piece.
        T Required<T>(T value, string field) where T : UnityEngine.Object =>
            value != null ? value : throw new InvalidOperationException($"UiWindowSkin '{name}' has no {field}.");
    }
}
