using System;
using Shared.Protocol;

namespace Dawnholder.Client.Network.CharacterMenu
{
    /// <summary>Main-thread owner of one connection's menu pending request. canSend owns session/generation validity.</summary>
    public sealed class CharacterMenuClient : IDisposable
    {
        readonly Func<bool> _canSend;
        readonly Action<ArraySegment<byte>> _send;
        readonly Action _disconnect;
        readonly Action<string> _selectionAccepted;
        uint _lastRequestId;
        ushort _pendingReplyId;
        byte _pendingSlot;
        Guid _pendingSelection;
        bool _ended;
        bool _disconnectIssued;
        bool _disposed;

        public CharacterMenuClient(Func<bool> canSend, Action<ArraySegment<byte>> send,
            Action disconnect, Action<string> selectionAccepted)
        {
            _canSend = canSend ?? throw new ArgumentNullException(nameof(canSend));
            _send = send ?? throw new ArgumentNullException(nameof(send));
            _disconnect = disconnect ?? throw new ArgumentNullException(nameof(disconnect));
            _selectionAccepted = selectionAccepted ?? throw new ArgumentNullException(nameof(selectionAccepted));
        }

        public event Action Changed;

        public bool HasPending => PendingRequestId != 0;
        public uint PendingRequestId { get; private set; }
        public CharacterMenuReply LastReply { get; private set; }

        public bool TryList() => TrySend(id => new C_CharacterListRequest
        {
            requestId = id,
            reserved = 0,
        }.Write(), PacketID.S_CharacterListResult);

        public bool TryCreate(byte slot, byte classId, string rawName)
        {
            // Preserve raw input; Core alone owns normalization, display-name policy and uniqueness.
            if (slot < 1 || slot > 3 || classId > 1 || rawName == null || rawName.Length > 36 ||
                !CharacterMenuReplyReader.IsWellFormedUtf16(rawName.AsSpan())) return false;
            return TrySend(id => new C_CharacterCreateRequest
            {
                requestId = id,
                slot = slot,
                classId = classId,
                reserved = 0,
                rawName = rawName,
            }.Write(), PacketID.S_CharacterCreateResult, slot);
        }

        public bool TrySelect(string characterIdD)
        {
            if (characterIdD == null || characterIdD.Length != 36 ||
                !Guid.TryParseExact(characterIdD, "D", out Guid selected) || selected == Guid.Empty) return false;
            return TrySend(id => new C_CharacterSelectRequest
            {
                requestId = id,
                reserved = 0,
                characterIdD = characterIdD,
            }.Write(), PacketID.S_CharacterSelectResult, selected: selected);
        }

        public bool TryReceive(CharacterMenuReply reply)
        {
            if (_disposed || _ended || !_canSend() || reply == null || !HasPending ||
                reply.RequestId != PendingRequestId || reply.PacketId != _pendingReplyId) return false;
            bool success = reply.Result == (byte)CharacterMenuResult.Success;
            bool selected = success && reply.PacketId == (ushort)PacketID.S_CharacterSelectResult;
            if (success && reply.PacketId == (ushort)PacketID.S_CharacterCreateResult &&
                (reply.Character == null || reply.Character.Slot != _pendingSlot)) return false;
            if (selected && (!Guid.TryParseExact(reply.SelectedCharacterIdD, "D", out Guid selectedId) ||
                selectedId != _pendingSelection)) return false;

            bool unknown = reply.Result == (byte)CharacterMenuResult.OutcomeUnknown;
            ClearPending();
            LastReply = reply;
            // Close menu requests before external callbacks can reenter. 54 still grants no gameplay authority.
            _ended = unknown || selected;
            try { NotifyChanged(); }
            finally
            {
                if (unknown) DisconnectOnce();
                else if (selected && !_disposed && _canSend()) _selectionAccepted(reply.SelectedCharacterIdD);
            }
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ClearPending();
            Changed = null;
            // Session cleanup owns transport/global state; disposing this helper must not disconnect it again.
        }

        bool TrySend(Func<uint, ArraySegment<byte>> write, PacketID replyId, byte slot = 0, Guid selected = default)
        {
            if (_disposed || _ended || HasPending || !_canSend()) return false;
            if (_lastRequestId == uint.MaxValue)
            {
                _ended = true;
                DisconnectOnce();
                return false;
            }

            uint requestId = ++_lastRequestId;
            PendingRequestId = requestId;
            _pendingReplyId = (ushort)replyId;
            _pendingSlot = slot;
            _pendingSelection = selected;
            try
            {
                // ID, kind and captured immutable payload are fixed before send can synchronously deliver a reply.
                ArraySegment<byte> frame = write(requestId);
                _send(frame);
            }
            catch (Exception)
            {
                _ended = true;
                ClearPending();
                DisconnectOnce();
                return false;
            }
            if (PendingRequestId == requestId) NotifyChanged();
            return true;
        }

        void ClearPending()
        {
            PendingRequestId = 0;
            _pendingReplyId = 0;
            _pendingSlot = 0;
            _pendingSelection = Guid.Empty;
        }

        void DisconnectOnce()
        {
            if (_disconnectIssued || _disposed || !_canSend()) return;
            _disconnectIssued = true;
            _disconnect();
        }

        void NotifyChanged()
        {
            if (_disposed || !_canSend() || Changed == null) return;
            foreach (Action notify in Changed.GetInvocationList())
            {
                if (_disposed || !_canSend()) return;
                notify();
            }
        }
    }
}
