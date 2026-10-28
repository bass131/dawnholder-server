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
    readonly string? _session;
    readonly IReadOnlyList<ListedCharacter> _expectedListing = [];
    readonly NewCharacter? _create;
    bool _enteredWithoutAuthority;
    StoredStage _storedStage;
    CharacterClass? _storedClass;
    string? _storedFailure;
    uint _requestId;
    byte _createSlot;
    string[] _listedIds = [];
    string? _createdCharacterIdD;

    internal PersistenceCharacterProbe(string accountName, CharacterClass intent)
    {
        _name = accountName;
        _intent = intent;
    }

    internal PersistenceCharacterProbe(string accountName, string characterIdD, string phase)
        : this(accountName, characterIdD, phase, session: null, expectedListing: [], create: null)
    {
    }

    /// <param name="expectedListing">Other characters the list must show with exactly these values before
    /// selection.</param>
    /// <param name="create">Requested in the lowest empty listed slot after listing and before selection.</param>
    internal PersistenceCharacterProbe(string accountName, string characterIdD, string phase, string? session,
        IReadOnlyList<ListedCharacter> expectedListing, NewCharacter? create)
    {
        _name = accountName;
        _characterIdD = characterIdD;
        _phase = phase;
        _session = session;
        _expectedListing = expectedListing;
        _create = create;
    }

    /// <summary>The validated ID from a successful create result; null until then.</summary>
    internal string? CreatedCharacterIdD
    {
        get { lock (Gate) return _createdCharacterIdD; }
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

        failure = SendStored(StoredStage.ListRequested, "list request", "list-request", "",
            id => new C_CharacterListRequest { requestId = id, reserved = 0 }, cancellationToken);
        if (failure is not null) return failure;
        failure = await WaitStoredAsync(() => _storedStage >= StoredStage.ListObserved, "list result", cancellationToken);
        if (failure is not null) return failure;
        if (_create is { } create)
        {
            byte slot;
            lock (Gate) slot = _createSlot;
            failure = SendStored(StoredStage.CreateRequested, "create request", "create-request",
                $" slot={slot} class={create.Class} name={create.Name}",
                id => new C_CharacterCreateRequest
                {
                    requestId = id,
                    slot = slot,
                    classId = (byte)create.Class,
                    reserved = 0,
                    rawName = create.Name,
                }, cancellationToken);
            if (failure is not null) return failure;
            failure = await WaitStoredAsync(() => _storedStage >= StoredStage.CreateObserved, "create result",
                cancellationToken);
            if (failure is not null) return failure;
        }
        failure = SendStored(StoredStage.SelectRequested, "ID selection", "select-request", "",
            id => new C_CharacterSelectRequest { requestId = id, reserved = 0, characterIdD = _characterIdD! },
            cancellationToken);
        if (failure is not null) return failure;
        failure = await WaitStoredAsync(() => _storedStage >= StoredStage.SelectAccepted, "selection result", cancellationToken);
        if (failure is not null) return failure;
        failure = await WaitStoredAsync(() => _storedStage >= StoredStage.LoadAccepted, "stored class", cancellationToken);
        if (failure is not null) return failure;
        return await WaitStoredAsync(() => _storedStage == StoredStage.Entered, "map entry", cancellationToken);
    }

    /// <summary>Requests the normal close and records it in the same Stored line format.</summary>
    internal void CloseStored()
    {
        Disconnect();
        LogStored("close-requested", "");
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
                    !BotSession.TryReadCharacterList(buffer, out S_CharacterListResult list) ||
                    list.requestId != _requestId)
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
                ObserveListing(list);
                break;
            case PacketID.S_CharacterCreateResult:
                if (_storedStage != StoredStage.CreateRequested ||
                    !BotSession.TryReadCharacterCreate(buffer, out S_CharacterCreateResult created) ||
                    created.requestId != _requestId)
                {
                    FailStored("Malformed, uncorrelated or out-of-order create result.");
                    return;
                }
                ObserveCreated(created);
                break;
            case PacketID.S_CharacterSelectResult:
                if (_storedStage != StoredStage.SelectRequested ||
                    !BotSession.TryReadCharacterSelection(buffer, out S_CharacterSelectResult selection) ||
                    selection.requestId != _requestId)
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

    // Called under Gate. The selected ID's listed class becomes the authority later checked against 48.
    void ObserveListing(S_CharacterListResult list)
    {
        (string Id, string Name, byte ClassId)[] slots =
        [
            (list.slot1CharacterIdD, list.slot1Name, list.slot1ClassId),
            (list.slot2CharacterIdD, list.slot2Name, list.slot2ClassId),
            (list.slot3CharacterIdD, list.slot3Name, list.slot3ClassId),
        ];
        int target = Array.FindIndex(slots, slot => slot.Id == _characterIdD);
        if (target < 0)
        {
            FailStored("The explicit character ID is absent from this account's list.");
            return;
        }
        _storedClass = (CharacterClass)slots[target].ClassId;
        foreach (ListedCharacter expected in _expectedListing)
        {
            int index = Array.FindIndex(slots, slot => slot.Id == expected.CharacterIdD);
            if (index < 0)
            {
                FailStored($"Expected character {expected.CharacterIdD} is absent from this account's list.");
                return;
            }
            LogStored("listed-expected", $"listedCharacterId={expected.CharacterIdD} slot={index + 1} " +
                $"name={slots[index].Name} class={(CharacterClass)slots[index].ClassId}");
            if (slots[index].Name != expected.Name || slots[index].ClassId != (byte)expected.Class)
            {
                FailStored($"Listed character {expected.CharacterIdD} differs from the expected name or class.");
                return;
            }
        }
        if (_create is not null)
        {
            // occupiedMask bit0..2 is slot1..3; a valid Knight is also class 0, so only the mask marks an empty slot.
            int empty = Enumerable.Range(0, slots.Length)
                .FirstOrDefault(slot => (list.occupiedMask & (1 << slot)) == 0, -1);
            if (empty < 0)
            {
                FailStored("No empty slot is listed for the new character.");
                return;
            }
            _createSlot = (byte)(empty + 1);
            _listedIds = slots.Where(slot => slot.Id.Length != 0).Select(slot => slot.Id).ToArray();
        }
        _storedStage = StoredStage.ListObserved;
        LogStored("listed-character", $"storedClass={_storedClass}");
    }

    // Called under Gate. Only a fully checked success prints createdCharacterId, which the after phase takes as input.
    void ObserveCreated(S_CharacterCreateResult created)
    {
        LogStored("create-result", $"packet=52 requestId={created.requestId} " +
            $"result={(CharacterMenuResult)created.result} " +
            $"invalidField={(CharacterMenuInvalidField)created.invalidField} slot={created.slot} " +
            $"characterIdD={created.characterIdD} name={created.name} class={(CharacterClass)created.classId}");
        if (created.result != (byte)CharacterMenuResult.Success)
        {
            FailStored($"Create request refused: {(CharacterMenuResult)created.result}.");
            return;
        }
        if (created.slot != _createSlot || created.name != _create!.Name || created.classId != (byte)_create.Class ||
            _listedIds.Contains(created.characterIdD))
        {
            FailStored("Create succeeded with a different slot, name or class, or with an already listed ID.");
            return;
        }
        _createdCharacterIdD = created.characterIdD;
        _storedStage = StoredStage.CreateObserved;
        LogStored("created-character", $"createdCharacterId={created.characterIdD} slot={created.slot} " +
            $"name={created.name} class={(CharacterClass)created.classId}");
    }

    // Requests are sequential, so the handler of the next result accepts only the latest connection-local ID.
    string? SendStored(StoredStage stage, string purpose, string observation, string details,
        Func<uint, IPacket> request, CancellationToken cancellationToken)
    {
        lock (Gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_storedFailure is not null || IsDisconnected)
                return _storedFailure ?? $"Disconnected before {purpose}.";
            IPacket packet = request(++_requestId);
            _storedStage = stage;
            LogStored(observation, $"packet={packet.Protocol} requestId={_requestId}{details}");
            Session!.Send(packet.Write());
            return null;
        }
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
        Console.WriteLine($"[Persistence Stored] utc={DateTime.UtcNow:O} phase={_phase}" +
            (_session is null ? "" : $" session={_session}") + $" account={_name} " +
            $"characterId={_characterIdD} event={observation}" + (details.Length == 0 ? "" : $" {details}"));

    internal sealed record ListedCharacter(string CharacterIdD, string Name, CharacterClass Class);

    internal sealed record NewCharacter(string Name, CharacterClass Class);

    enum StoredStage
    {
        Connecting, HandshakeReceived, LoginReceived, ListRequested, ListObserved, CreateRequested, CreateObserved,
        SelectRequested, SelectAccepted, LoadAccepted, Entered,
    }
}
