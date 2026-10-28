#nullable enable
using System;
using Dawnholder.Client.Network.CharacterMenu;
using Shared.Protocol;
using Phase = Dawnholder.Client.UI.CharacterMenuViewState.Phase;

namespace Dawnholder.Client.UI
{
    /// <summary>
    /// Joins the display-only <see cref="CharacterMenuView"/> to one connection's <see cref="CharacterMenuClient"/>.
    /// Only this type holds the listed character IDs; the view gets names and classes. Reply bytes become a view
    /// phase in <see cref="Apply"/> and player text in <see cref="CharacterMenuResultText"/>. It subscribes to one
    /// client at a time, and <see cref="Observe"/> drops that subscription when the connection changes or ends.
    /// Each applied result, IDs included, also goes to the Editor log as one line (<see cref="LogResult"/>).
    /// </summary>
    internal sealed class CharacterMenuPresenter : IDisposable
    {
        // Local outcomes that no server result names, so they are not in CharacterMenuResultText's refusal table.
        const string CreatedText = "캐릭터를 만들었습니다. 입장할 캐릭터를 선택하세요.";
        const string NotSentText = "요청을 보내지 못했습니다. 입력을 확인하고 다시 시도해 주세요.";
        const string ListNotSentText = "목록 요청을 보내지 못했습니다. 다시 시도해 주세요.";

        // Indexed by slot - 1. The IDs stay here; a select turns the view's slot into its ID.
        readonly CharacterMenuCharacter?[] _characters = new CharacterMenuCharacter?[3];
        Action<CharacterMenuViewState>? _render;
        Action? _disconnect;
        Action<Exit>? _exit;
        CharacterMenuClient? _client;
        Action? _clientChanged;
        CharacterMenuReply? _handledReply;
        Phase _phase = Phase.Connecting;
        string _message = string.Empty;
        // The slot a pending create or select asked for; its reply text names this original slot.
        byte _requestSlot;
        bool _sawConnection;
        // No more client work: the connection ended, was replaced, or the menu left.
        bool _ended;
        // Back, the legacy switch or disposal happened; nothing renders or leaves again.
        bool _left;

        internal CharacterMenuPresenter(Action<CharacterMenuViewState> render, Action disconnect, Action<Exit> exit)
        {
            _render = render ?? throw new ArgumentNullException(nameof(render));
            _disconnect = disconnect ?? throw new ArgumentNullException(nameof(disconnect));
            _exit = exit ?? throw new ArgumentNullException(nameof(exit));
        }

        /// <summary>
        /// Main thread, every frame, with the current connection's client. The first client seen while connected
        /// is adopted. Afterwards a different client, none, or a closed connection ends the menu: the view keeps
        /// Disconnected as final, and a new connection gets a new scene-owned menu.
        /// </summary>
        internal void Observe(CharacterMenuClient? current, bool connected)
        {
            if (_ended) return;
            if (_client != null)
            {
                if (!connected || !ReferenceEquals(current, _client)) EndConnection();
                return;
            }
            if (connected) _sawConnection = true;
            if (current != null && connected) Attach(current);
            // Closed before a menu client existed, e.g. a refused login or a lost transport.
            else if (_sawConnection && !connected) EndConnection();
        }

        internal void Select(byte slot)
        {
            CharacterMenuCharacter? character = CanRequest() && _phase == Phase.Ready && slot >= 1 && slot <= 3
                ? _characters[slot - 1] : null;
            if (character == null)
            {
                Render();
                return;
            }
            string characterId = character.CharacterIdD;
            Request(Phase.Selecting, Phase.Ready, slot, string.Empty, client => client.TrySelect(characterId));
        }

        internal void Create(byte slot, byte classId, string rawName)
        {
            if (!CanRequest() || _phase != Phase.Ready)
            {
                Render();
                return;
            }
            // The draft goes out as typed; Core alone judges and normalizes the name.
            Request(Phase.Creating, Phase.Ready, slot, string.Empty, client => client.TryCreate(slot, classId, rawName));
        }

        internal void Refresh()
        {
            if (!CanRequest() || (_phase != Phase.Ready && _phase != Phase.ListFailed))
            {
                Render();
                return;
            }
            Request(Phase.LoadingList, _phase, 0, string.Empty, client => client.TryList());
        }

        internal void Back()
        {
            if (!_left) Leave(Exit.MainMenu);
        }

        /// <summary>Scene teardown. Drops the subscription only; the connection stays for the entry that follows.</summary>
        public void Dispose()
        {
            _left = true;
            End();
            _render = null;
            _disconnect = null;
            _exit = null;
        }

        bool CanRequest() => !_ended && _client != null && !_client.HasPending;

        void Attach(CharacterMenuClient client)
        {
            _client = client;
            // One handler per client, so a notification still running for a replaced client fails the check below.
            _clientChanged = () => OnClientChanged(client);
            client.Changed += _clientChanged;
            // The facade sends the first list in the call that creates the client; its reply may already be here.
            if (client.LastReply != null) Apply(client.LastReply);
            else if (client.HasPending) Show(Phase.LoadingList, string.Empty);
            else Show(Phase.ListFailed, ListNotSentText);
        }

        void OnClientChanged(CharacterMenuClient source)
        {
            if (_ended || !ReferenceEquals(source, _client)) return;
            CharacterMenuReply? reply = source.LastReply;
            // A send notification carries no new reply; Request has already shown the pending phase.
            if (reply != null && !ReferenceEquals(reply, _handledReply)) Apply(reply);
        }

        // What each result does to the menu. The client has already matched the reply to the pending request.
        void Apply(CharacterMenuReply reply)
        {
            _handledReply = reply;
            byte slot = _requestSlot;
            _requestSlot = 0;
            LogResult(reply, slot);
            if (reply.Result == (byte)CharacterMenuResult.LegacyFlowOnly)
            {
                // A legacy host never moves this connection to the class flow: it closes, and the scene's class
                // buttons open the legacy Town connection. No text and no main menu.
                Leave(Exit.LegacyClasses);
                return;
            }
            if (reply.Result == (byte)CharacterMenuResult.OutcomeUnknown)
            {
                // The create may have happened. The client closes the connection right after this notification;
                // nothing is sent again on it, and the text stays until the player goes back.
                End();
                Show(Phase.Disconnected, Text(reply, slot));
                return;
            }
            bool success = reply.Result == (byte)CharacterMenuResult.Success;
            switch ((PacketID)reply.PacketId)
            {
                case PacketID.S_CharacterListResult when success:
                    for (int index = 0; index < _characters.Length; index++) _characters[index] = reply.Slots[index];
                    // An automatic refresh keeps the refusal that asked for it.
                    Show(Phase.Ready, _message);
                    break;
                case PacketID.S_CharacterListResult:
                    // A refused list is not an empty account.
                    Array.Clear(_characters, 0, _characters.Length);
                    Show(Phase.ListFailed, Text(reply, 0));
                    break;
                case PacketID.S_CharacterCreateResult when success:
                    _characters[reply.Character!.Slot - 1] = reply.Character;
                    // A new character is listed, not entered; entry needs an explicit select.
                    Show(Phase.Ready, $"슬롯 {slot}: {CreatedText}");
                    break;
                case PacketID.S_CharacterSelectResult when success:
                    // 54 confirms the ID only. 48 brings class authority and the map entry opens the scene;
                    // this menu stays locked until then.
                    Show(Phase.Entering, string.Empty);
                    break;
                default:
                    Refuse(reply, slot);
                    break;
            }
        }

        // A refused create or select. Refused lists stay in Apply, so a refresh never asks for another one.
        void Refuse(CharacterMenuReply reply, byte slot)
        {
            string message = Text(reply, slot);
            var field = (CharacterMenuInvalidField)reply.InvalidField;
            // CharacterMenuResultText tells the player 「목록을 새로고침합니다」 for these; this is that one list.
            if (reply.Result == (byte)CharacterMenuResult.InvalidInput && (field == CharacterMenuInvalidField.Slot ||
                field == CharacterMenuInvalidField.Class || field == CharacterMenuInvalidField.CharacterId))
                Request(Phase.LoadingList, Phase.ListFailed, 0, message, client => client.TryList());
            else
                Show(Phase.Ready, message);
        }

        void Request(Phase pending, Phase fallback, byte slot, string message, Func<CharacterMenuClient, bool> send)
        {
            _phase = pending;
            _requestSlot = slot;
            _message = message;
            // A refused send leaves nothing pending, so the view must not stay locked waiting for a reply.
            if (!send(_client!))
            {
                _phase = fallback;
                _requestSlot = 0;
                _message = slot == 0 ? ListNotSentText : $"슬롯 {slot}: {NotSentText}";
            }
            // Also after a reply delivered during send: the view releases its click latch on every render.
            Render();
        }

        static string Text(CharacterMenuReply reply, byte slot)
        {
            CharacterMenuResultText.TryGetFailureText(reply.Result, reply.InvalidField, out string text);
            return slot == 0 ? text : $"슬롯 {slot}: {text}";
        }

        // The only place a run shows character IDs: the view gets names and classes, and neither the client's
        // network code nor the server logs IDs. The joint-window check (goal completion row 4) reads these lines to
        // match IDs across a reconnect, so the wording is that check's contract. Apply calls this once per reply
        // before anything else, so 12 and 16 get their line too. Account names, addresses and credentials stay out.
        static void LogResult(CharacterMenuReply reply, byte slot) =>
            UnityEngine.Debug.Log("[CharacterMenu] " + ResultLine(reply, slot));

        static string ResultLine(CharacterMenuReply reply, byte slot)
        {
            if (reply.Result == (byte)CharacterMenuResult.Success)
            {
                switch ((PacketID)reply.PacketId)
                {
                    case PacketID.S_CharacterListResult:
                        return $"50 목록 성공: 1={Row(reply.Slots[0])} 2={Row(reply.Slots[1])} 3={Row(reply.Slots[2])}";
                    case PacketID.S_CharacterCreateResult:
                        // The server's slot, where Apply keeps the new ID; the client accepts it only if it is the
                        // requested one.
                        return $"52 생성 성공: 슬롯 {reply.Character!.Slot} {Row(reply.Character)}";
                    case PacketID.S_CharacterSelectResult:
                        return $"54 선택 성공: 슬롯 {slot} {reply.SelectedCharacterIdD}";
                }
            }
            // Every other result is a refusal. A list names no slot; a create or select names the one it asked for.
            byte named = reply.PacketId == (ushort)PacketID.S_CharacterListResult ? (byte)0 : slot;
            return $"{reply.PacketId} 거절: 슬롯 {named} 결과 {reply.Result}/{reply.InvalidField}";
        }

        static string Row(CharacterMenuCharacter? character) =>
            character == null ? "빈칸" : $"{character.CharacterIdD}/{character.Name}/{character.ClassId}";

        void EndConnection()
        {
            End();
            Show(Phase.Disconnected, string.Empty);
        }

        void End()
        {
            _ended = true;
            if (_client != null && _clientChanged != null) _client.Changed -= _clientChanged;
            _client = null;
            _clientChanged = null;
        }

        void Leave(Exit exit)
        {
            _left = true;
            End();
            Action? disconnect = _disconnect;
            Action<Exit>? leave = _exit;
            // The menu connection closes before the next screen takes over.
            disconnect?.Invoke();
            leave?.Invoke(exit);
        }

        void Show(Phase phase, string message)
        {
            _phase = phase;
            _message = message;
            Render();
        }

        void Render()
        {
            if (!_left) _render?.Invoke(new CharacterMenuViewState(_phase, Display(0), Display(1), Display(2), _message));
        }

        CharacterMenuViewState.Slot? Display(int index) =>
            _characters[index] is { } character ? new CharacterMenuViewState.Slot(character.Name, character.ClassId) : null;

        internal enum Exit
        {
            MainMenu,
            LegacyClasses,
        }
    }
}
