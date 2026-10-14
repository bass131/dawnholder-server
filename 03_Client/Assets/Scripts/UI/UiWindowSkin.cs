#nullable enable
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
        [SerializeField] Sprite _divider = null!;
        [SerializeField] Sprite _acceptNormal = null!;
        [SerializeField] SpriteState _acceptStates;
        [SerializeField] Sprite _declineNormal = null!;
        [SerializeField] SpriteState _declineStates;

        internal Image PanelPrefab => _panelPrefab;
        internal Image HeaderPrefab => _headerPrefab;
        internal Button ButtonPrefab => _buttonPrefab;
        internal Image InsetPrefab => _insetPrefab;
        internal Image PortraitPrefab => _portraitPrefab;
        internal Sprite DialogPanel => _dialogPanel;
        internal Sprite Divider => _divider;
        internal Sprite AcceptNormal => _acceptNormal;
        internal SpriteState AcceptStates => _acceptStates;
        internal Sprite DeclineNormal => _declineNormal;
        internal SpriteState DeclineStates => _declineStates;
    }
}
