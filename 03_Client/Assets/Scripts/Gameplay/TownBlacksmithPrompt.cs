#nullable enable
using Dawnholder.Client.Network;
using Dawnholder.Client.Prediction;
using Dawnholder.Client.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dawnholder.Client.Gameplay
{
    /// <summary>Runtime Town entrance using the live NPC trigger and the connection's current local player.</summary>
    [DisallowMultipleComponent]
    public sealed class TownBlacksmithPrompt : MonoBehaviour
    {
        GuildUiController _owner = null!;
        Button _button = null!;
        CanvasGroup _visibility = null!;
        NpcInteractable? _npc;
        Collider2D? _trigger;

        internal static TownBlacksmithPrompt BuildRuntime(Transform parent, GuildUiController owner)
        {
            Button button = GuildWindowLayout.Entrance(parent, "BlacksmithEntrance", "대장간", new Vector2(-16f, 68f));
            button.gameObject.SetActive(false);
            var prompt = button.gameObject.AddComponent<TownBlacksmithPrompt>();
            prompt._owner = owner;
            prompt._button = button;
            prompt._visibility = button.gameObject.AddComponent<CanvasGroup>();
            prompt.SetVisible(false);
            button.gameObject.SetActive(true);
            return prompt;
        }

        internal bool IsLocalPlayerNear()
        {
            UnityClientSession? session = UnityClientSession.Instance;
            LocalPlayerMovement? player = LocalPlayerMovement.Instance;
            if (!isActiveAndEnabled || _owner == null || !_owner.CanOpenWindows || session == null ||
                player == null || !session.CanControlPlayer(player) || session.Entry.MapId != 0 ||
                gameObject.scene.name != "Town" || gameObject.scene != SceneManager.GetActiveScene() ||
                player.gameObject.scene != gameObject.scene || !player.isActiveAndEnabled ||
                !session.Entry.HasHp || session.Entry.CurrentHp <= 0) return false;
            if (_npc == null || _trigger == null) FindNpc();
            if (_npc == null || !_npc.isActiveAndEnabled || _npc.gameObject.scene != gameObject.scene ||
                _trigger == null || !_trigger.enabled || !_trigger.isTrigger) return false;
            // Use the actual scene colliders, including their offsets/transforms. Remote players and
            // prefab asset coordinates cannot satisfy this check; the existing NPC E handler is untouched.
            foreach (Collider2D collider in player.GetComponentsInChildren<Collider2D>())
            {
                if (collider.enabled && collider.gameObject.activeInHierarchy &&
                    _trigger.Distance(collider).isOverlapped) return true;
            }
            return false;
        }

        void OnEnable()
        {
            if (_button != null) _button.onClick.AddListener(Open);
        }

        void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(Open);
            SetVisible(false);
            _npc = null;
            _trigger = null;
        }

        void Update() => SetVisible(IsLocalPlayerNear());

        void Open()
        {
            if (IsLocalPlayerNear()) _owner.OpenBlacksmith();
        }

        void SetVisible(bool visible)
        {
            if (_visibility == null) return;
            _visibility.alpha = visible ? 1f : 0f;
            _visibility.interactable = visible;
            _visibility.blocksRaycasts = visible;
        }

        void FindNpc()
        {
            _npc = null;
            _trigger = null;
            foreach (NpcInteractable npc in FindObjectsByType<NpcInteractable>(FindObjectsSortMode.None))
            {
                if (npc.gameObject.scene != gameObject.scene || npc.name != "Npc_BlackSmith") continue;
                Collider2D trigger = npc.GetComponent<Collider2D>();
                if (trigger == null || !trigger.isTrigger) continue;
                _npc = npc;
                _trigger = trigger;
                return;
            }
        }
    }
}
