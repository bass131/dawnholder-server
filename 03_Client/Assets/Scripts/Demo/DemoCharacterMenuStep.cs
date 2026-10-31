#nullable enable
using Dawnholder.Client.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Phase = Dawnholder.Client.UI.CharacterMenuViewState.Phase;

namespace Dawnholder.Client.Demo
{
    internal enum DemoCharacterMenuOutcome
    {
        Waiting,
        Creating,
        Selecting,
        Legacy,
        Stopped,
    }

    // One menu visit: observe the presenter's display state and use only this panel's real controls.
    // Character identity, name validation and reply interpretation remain with the existing menu/server.
    internal sealed class DemoCharacterMenuStep
    {
        readonly DemoRole _role;
        readonly string? _characterName;
        DemoCharacterMenuOutcome _outcome;
        bool _creationRequested;

        internal DemoCharacterMenuStep(DemoRole role, string? characterName)
        {
            _role = role;
            _characterName = characterName;
        }

        internal string StopReason { get; private set; } = string.Empty;
        internal byte SelectedSlot { get; private set; }

        internal DemoCharacterMenuOutcome Advance(CharacterMenuViewState? state, Transform? panel)
        {
            // These outcomes are terminal even after a later frame loses the menu state.
            if (_outcome == DemoCharacterMenuOutcome.Selecting || _outcome == DemoCharacterMenuOutcome.Stopped)
                return _outcome;
            if (state == null) return DemoCharacterMenuOutcome.Legacy;

            switch (state.CurrentPhase)
            {
                case Phase.Connecting:
                case Phase.LoadingList:
                case Phase.Creating:
                case Phase.Selecting:
                case Phase.Entering:
                    return DemoCharacterMenuOutcome.Waiting;
                case Phase.ListFailed:
                case Phase.Disconnected:
                    return Stop($"Character menu {state.CurrentPhase}: {state.Message}");
            }

            if (panel == null) return Stop("Character menu panel missing: CharacterMenu/Panel");
            byte classId = _role == DemoRole.Knight ? (byte)0 : (byte)1;
            for (byte slot = 1; slot <= 3; slot++)
            {
                if (state.GetSlot(slot)?.ClassId != classId) continue;
                if (!Click(panel, "Slot" + slot) || !Click(panel, "Submit")) return _outcome;
                SelectedSlot = slot;
                _outcome = DemoCharacterMenuOutcome.Selecting;
                return _outcome;
            }

            if (_creationRequested) return Stop($"Character creation refused: {state.Message}");
            if (string.IsNullOrEmpty(_characterName)) return Stop("Character creation needs -demo-character.");

            byte emptySlot = 0;
            for (byte slot = 1; slot <= 3; slot++)
            {
                if (state.GetSlot(slot) != null) continue;
                emptySlot = slot;
                break;
            }
            if (emptySlot == 0) return Stop("No empty character slot for the requested class.");
            if (!Click(panel, "Slot" + emptySlot)) return _outcome;

            const string namePath = "CreateForm/Name";
            TMP_InputField? name = panel.Find(namePath)?.GetComponent<TMP_InputField>();
            if (name == null || !name.gameObject.activeInHierarchy || !name.interactable)
                return Stop($"Character menu input unavailable: {namePath}");
            name.text = _characterName;
            string classPath = _role == DemoRole.Knight ? "CreateForm/Knight" : "CreateForm/Mage";
            if (!Click(panel, classPath)) return _outcome;

            // Latch before the request callback can synchronously render its next state.
            _creationRequested = true;
            if (!Click(panel, "CreateForm/Create")) return _outcome;
            return DemoCharacterMenuOutcome.Creating;
        }

        bool Click(Transform panel, string path)
        {
            Button? button = panel.Find(path)?.GetComponent<Button>();
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                Stop($"Character menu button unavailable: {path}");
                return false;
            }
            button.onClick.Invoke();
            return true;
        }

        DemoCharacterMenuOutcome Stop(string reason)
        {
            StopReason = reason;
            _outcome = DemoCharacterMenuOutcome.Stopped;
            return _outcome;
        }
    }
}
