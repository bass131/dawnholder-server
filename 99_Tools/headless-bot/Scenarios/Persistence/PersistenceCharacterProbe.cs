using Shared.Protocol;
using Shared.GameData;
using System.Buffers.Binary;

namespace Dawnholder.Tools.HeadlessBot.Scenarios.Persistence;

/// <summary>Observes public admission and packet order only; never logs store authority or credentials.</summary>
sealed class PersistenceCharacterProbe : ProbeBase
{
    readonly string _name;
    readonly CharacterClass _intent;
    readonly string? _characterIdD;
    readonly string _phase = "";
    bool _enteredWithoutAuthority;
    StoredStage _storedStage;
    CharacterClass? _storedClass;
    string? _storedFailure;

    internal PersistenceCharacterProbe(string accountName, CharacterClass intent)
    {
        _name = accountName;
        _intent = intent;
    }

    internal PersistenceCharacterProbe(string accountName, string characterIdD, string phase)
    {
        _name = accountName;
        _characterIdD = characterIdD;
        _phase = phase;
    }

    protected override string AccountName => _name;
    protected override CharacterClass SelectedClass => _intent;
    protected override bool SelectClassAfterLogin => _characterIdD is null;

    internal async Task<string?> ObserveMageAsync(string host, int port, CancellationToken cancellationToken)
    {
        TimeSpan timeout = TimeSpan.FromSeconds(5);
        Connect(host, port);
        if (!WaitConnected(timeout) || !WaitHandshake(timeout) || !HandshakeOk)
            return "Connection or handshake did not complete.";
        if (!WaitLogin(timeout) || LoginResult != Shared.GameData.LoginResult.Accepted)
            return "Account login was not accepted.";
        if (!await WaitUntil(() => Session?.CharacterLoad.HasValue == true, timeout, cancellationToken))
            return "No valid character load result was observed.";
        var load = Session!.CharacterLoad!.Value;
        if (load.Status != CharacterLoadStatus.Success)
            return $"Admission refused: {load.Status}.";
        if (load.LoadedClass != CharacterClass.Mage)
            return "The server did not return the stored Mage class.";
        if (!WaitEnterMap(timeout) || _enteredWithoutAuthority)
            return "EnterMap did not follow a valid authority result.";
        return null;
    }

    internal async Task<string?> ObserveStoredAsync(string host, int port, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!await ConnectWithCancellationAsync(host, port, cancellationToken))
            return "Connection was refused or exceeded 5s.";
        string? failure = await WaitStoredAsync(() => WaitHandshake(TimeSpan.Zero), "handshake", cancellationToken);
        if (failure is not null) return failure;
        if (!HandshakeOk) return "Connection handshake was refused.";
        failure = await WaitStoredAsync(() => WaitLogin(TimeSpan.Zero), "login", cancellationToken);
        if (failure is not null) return failure;
        if (LoginResult != Shared.GameData.LoginResult.Accepted) return $"Account login was refused: {LoginResult}.";

        lock (Gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_storedFailure is not null || IsDisconnected) return _storedFailure ?? "Disconnected before list request.";
            _storedStage = StoredStage.ListRequested;
            LogStored("list-request", "packet=49 requestId=1");
            Session!.Send(new C_CharacterListRequest { requestId = 1, reserved = 0 }.Write());
        }
        failure = await WaitStoredAsync(() => _storedStage >= StoredStage.ListObserved, "list result", cancellationToken);
        if (failure is not null) return failure;
        lock (Gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_storedFailure is not null || IsDisconnected) return _storedFailure ?? "Disconnected before ID selection.";
            _storedStage = StoredStage.SelectRequested;
            LogStored("select-request", "packet=53 requestId=2");
            Session!.Send(new C_CharacterSelectRequest
            {
                requestId = 2, reserved = 0, characterIdD = _characterIdD!,
            }.Write());
        }
        failure = await WaitStoredAsync(() => _storedStage >= StoredStage.SelectAccepted, "selection result", cancellationToken);
        if (failure is not null) return failure;
        failure = await WaitStoredAsync(() => _storedStage >= StoredStage.LoadAccepted, "stored class", cancellationToken);
        if (failure is not null) return failure;
        return await WaitStoredAsync(() => _storedStage == StoredStage.Entered, "map entry", cancellationToken);
    }

    protected override bool ValidatePacket(ArraySegment<byte> buffer)
    {
        if (_characterIdD is null) return true;
        if (_storedFailure is not null) return false;
        if (buffer.Array is null || buffer.Count < 4 ||
            BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan()) != buffer.Count)
        {
            FailStored("Malformed packet header.");
            return false;
        }
        ReadOnlySpan<byte> bytes = buffer.AsSpan();
        PacketID id = (PacketID)BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(2));
        switch (id)
        {
            case PacketID.S_HandshakeResult:
                if (_storedStage != StoredStage.Connecting || bytes.Length < 9 || bytes[4] > 1 ||
                    BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(7)) % 2 != 0 ||
                    BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(7)) != bytes.Length - 9 ||
                    (bytes[4] == 1 && BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(5)) != ProtocolVersion.Current))
                {
                    FailStored("Malformed or out-of-order handshake result.");
                    return false;
                }
                _storedStage = StoredStage.HandshakeReceived;
                break;
            case PacketID.S_LoginResult:
                if (_storedStage != StoredStage.HandshakeReceived || !HandshakeOk || bytes.Length != 5 ||
                    !Enum.IsDefined((Shared.GameData.LoginResult)bytes[4]))
                {
                    FailStored("Malformed or out-of-order login result.");
                    return false;
                }
                _storedStage = StoredStage.LoginReceived;
                break;
            case PacketID.S_EnterMap:
                if (_storedStage != StoredStage.LoadAccepted || bytes.Length != 16)
                {
                    FailStored("EnterMap did not follow selection and stored class authority.");
                    return false;
                }
                break;
        }
        return true;
    }

    protected override void HandleExtraPacket(PacketID id, ArraySegment<byte> buffer)
    {
        if (_characterIdD is null) return;
        // ProbeBase serializes callbacks with Gate; the scenario uses that same gate for requests/waits.
        switch (id)
        {
            case PacketID.S_CharacterListResult:
                if (_storedStage != StoredStage.ListRequested ||
                    !BotSession.TryReadCharacterList(buffer, out S_CharacterListResult list) || list.requestId != 1)
                {
                    FailStored("Malformed, uncorrelated or out-of-order list result.");
                    return;
                }
                LogStored("list-result", $"packet=50 requestId={list.requestId} result={(CharacterMenuResult)list.result} " +
                    $"invalidField={(CharacterMenuInvalidField)list.invalidField} occupiedMask={list.occupiedMask}");
                if (list.result != (byte)CharacterMenuResult.Success)
                {
                    FailStored($"List request refused: {(CharacterMenuResult)list.result}.");
                    return;
                }
                _storedClass = list.slot1CharacterIdD == _characterIdD ? (CharacterClass)list.slot1ClassId :
                    list.slot2CharacterIdD == _characterIdD ? (CharacterClass)list.slot2ClassId :
                    list.slot3CharacterIdD == _characterIdD ? (CharacterClass)list.slot3ClassId : null;
                if (_storedClass is null)
                {
                    FailStored("The explicit character ID is absent from this account's list.");
                    return;
                }
                _storedStage = StoredStage.ListObserved;
                LogStored("listed-character", $"storedClass={_storedClass}");
                break;
            case PacketID.S_CharacterSelectResult:
                if (_storedStage != StoredStage.SelectRequested ||
                    !BotSession.TryReadCharacterSelection(buffer, out S_CharacterSelectResult selection) ||
                    selection.requestId != 2)
                {
                    FailStored("Malformed, uncorrelated or out-of-order selection result.");
                    return;
                }
                LogStored("select-result", $"packet=54 requestId={selection.requestId} " +
                    $"result={(CharacterMenuResult)selection.result} invalidField={(CharacterMenuInvalidField)selection.invalidField} " +
                    $"selectedCharacterId={selection.selectedCharacterIdD}");
                if (selection.result != (byte)CharacterMenuResult.Success || selection.selectedCharacterIdD != _characterIdD)
                {
                    FailStored("ID selection failed or returned a different character ID.");
                    return;
                }
                _storedStage = StoredStage.SelectAccepted;
                break;
            case PacketID.S_CharacterLoadResult:
                if (_storedStage != StoredStage.SelectAccepted || !BotSession.TryReadCharacterLoad(buffer, out var load))
                {
                    FailStored("Malformed or out-of-order character load result.");
                    return;
                }
                LogStored("character-load", $"packet=48 result={load.Status} storedClass={load.LoadedClass}");
                if (load.Status != CharacterLoadStatus.Success || load.LoadedClass != _storedClass)
                {
                    FailStored("Character load failed or its class differs from the account list.");
                    return;
                }
                _storedStage = StoredStage.LoadAccepted;
                break;
        }
    }

    protected override void OnEnterMap(S_EnterMap packet)
    {
        if (_characterIdD is not null)
        {
            if (packet.entityId <= 0 || !float.IsFinite(packet.spawnX) || !float.IsFinite(packet.spawnY))
            {
                FailStored("Malformed map entry values.");
                return;
            }
            _storedStage = StoredStage.Entered;
            LogStored("enter-map", $"packet=3 entityId={packet.entityId} storedClass={_storedClass}");
            return;
        }
        var load = Session?.CharacterLoad;
        _enteredWithoutAuthority = load is null || load.Value.Status != CharacterLoadStatus.Success;
    }

    async Task<string?> WaitStoredAsync(Func<bool> observed, string description, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        bool completed = await WaitUntil(() =>
        {
            lock (Gate) return _storedFailure is not null || IsDisconnected || observed();
        }, TimeSpan.FromSeconds(5), cancellationToken);
        lock (Gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_storedFailure is not null) return _storedFailure;
            if (IsDisconnected) FailStored($"Disconnected while observing {description}.");
            else if (!completed) FailStored($"Timed out observing {description} (5s).");
            return _storedFailure;
        }
    }

    void FailStored(string reason)
    {
        if (_storedFailure is not null) return;
        _storedFailure = reason;
        LogStored("failure", reason);
    }

    void LogStored(string observation, string details) =>
        Console.WriteLine($"[Persistence Stored] utc={DateTime.UtcNow:O} phase={_phase} account={_name} " +
            $"characterId={_characterIdD} event={observation} {details}");

    enum StoredStage
    {
        Connecting, HandshakeReceived, LoginReceived, ListRequested, ListObserved,
        SelectRequested, SelectAccepted, LoadAccepted, Entered,
    }
}
