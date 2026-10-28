using Dawnholder.Server.GameServer.Accounts;
using Dawnholder.Server.GameServer.Loop;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Sessions;

// One connection's Stored menu. The World tick owns tickets and the retained selection;
// background work only publishes immutable Core results back to that same World.
internal sealed class CharacterMenuCatalogSession
{
    readonly GameWorld _world;
    readonly CharacterCatalogService? _catalog;
    readonly CharacterMenuRequestLifetime _requests = new();
    readonly Guid _sessionInstanceId = Guid.NewGuid();
    readonly CancellationTokenSource _closed = new();
    CharacterSelectionHandle? _selection;
    SafeCheckpoint? _admissionBaseline;

    internal CharacterMenuCatalogSession(GameWorld world, CharacterCatalogService? catalog)
    {
        _world = world;
        _catalog = catalog;
    }

    internal void HandleRequest(GameSession session, uint requestId, PacketID packetId,
        PersistenceDeadline deadline, bool wasSelecting, AccountIdentity? receivedAccount,
        byte slot, byte classId, string text)
    {
        if (session.IsClosing)
        {
            Close(null);
            return;
        }
        CharacterMenuRequestBeginResult begin = _requests.Begin(requestId, (ushort)packetId, deadline);
        if (begin.Status is CharacterMenuRequestBeginStatus.Ignored or CharacterMenuRequestBeginStatus.Closed)
            return;
        if (begin.Status == CharacterMenuRequestBeginStatus.InProgress)
        {
            SendFailure(session, requestId, packetId, CharacterMenuResult.RequestInProgress);
            return;
        }

        CharacterMenuRequestTicket ticket = begin.Ticket!;
        if (!wasSelecting || !session.CanUseCharacterMenu || receivedAccount is not AccountIdentity account ||
            session.Account != receivedAccount)
        {
            Reject(session, ticket, CharacterMenuResult.InvalidState);
            return;
        }
        if (_catalog is null)
        {
            Reject(session, ticket, CharacterMenuResult.Unavailable);
            return;
        }

        AccountContext owner = new(account.AccountId, account.Name);
        RequestContext request = new(_sessionInstanceId, ticket.Generation);
        switch (packetId)
        {
            case PacketID.C_CharacterListRequest:
                _ = ReceiveAsync(session, ticket, account, request, () =>
                    _catalog.ListCharactersAsync(owner, request, deadline, _closed.Token));
                break;
            case PacketID.C_CharacterCreateRequest:
                CreatePreparationResult prepared = _catalog.PrepareCreate(owner, slot, text, (CharacterClass)classId);
                if (prepared.Status != CatalogStatus.Prepared || prepared.Descriptor is null)
                {
                    Console.WriteLine($"[CharacterMenu] {packetId}: {prepared.Status}/{prepared.InvalidField}");
                    var failure = CharacterMenuCatalogResultMapper.MapFailure(prepared.Status, prepared.InvalidField);
                    Reject(session, ticket, failure.Result, failure.InvalidField);
                    return;
                }
                CreateDescriptor descriptor = prepared.Descriptor;
                Guid operationId = Guid.NewGuid();
                _ = ReceiveAsync(session, ticket, account, request,
                    () => CreateAsync(descriptor, operationId, request, deadline));
                break;
            case PacketID.C_CharacterSelectRequest:
                if (!Guid.TryParseExact(text, "D", out Guid characterId) || characterId == Guid.Empty)
                {
                    Reject(session, ticket, CharacterMenuResult.InvalidInput, CharacterMenuInvalidField.CharacterId);
                    return;
                }
                if (!session.TryBeginCatalogSelection())
                {
                    Reject(session, ticket, CharacterMenuResult.InvalidState);
                    return;
                }
                _ = ReceiveSelectionAsync(session, ticket, account, owner, request, characterId);
                break;
        }
    }

    internal void Close(SafeCheckpoint? projection)
    {
        _requests.Close();
        _ = CharacterCatalogService.ObserveAsync(_closed.CancelAsync());
        if (_selection is not null)
            ObserveClose(_selection, projection is not null && !projection.Equals(_admissionBaseline) ? projection : null);
    }

    internal void RecordAdmission(SafeCheckpoint baseline) => _admissionBaseline ??= baseline;

    async Task<CatalogResult> CreateAsync(CreateDescriptor descriptor, Guid operationId,
        RequestContext request, PersistenceDeadline deadline)
    {
        CatalogResult result = await _catalog!.CreateCharacterAsync(
            descriptor, operationId, request, deadline, _closed.Token).ConfigureAwait(false);
        if (result.Status == CatalogStatus.OutcomeUnknown && !deadline.IsExpired && !_closed.IsCancellationRequested)
        {
            // Reconciliation keeps the exact prepared intent, logical ID and original budget.
            result = await _catalog.ResolveCreateAsync(
                descriptor, operationId, request, deadline, _closed.Token).ConfigureAwait(false);
        }
        return result;
    }

    async Task ReceiveAsync(GameSession session, CharacterMenuRequestTicket ticket,
        AccountIdentity account, RequestContext request, Func<Task<CatalogResult>> operation)
    {
        CatalogResult result;
        try
        {
            // Even a repository's synchronous prefix must stay off the game tick.
            result = await Task.Run(operation).ConfigureAwait(false);
        }
        catch (Exception)
        {
            result = new(ticket.RequestPacketId == (ushort)PacketID.C_CharacterCreateRequest
                ? CatalogStatus.OutcomeUnknown : CatalogStatus.Unavailable, request);
        }
        _world.EnqueueAdmissionResult(() => Apply(session, ticket, account, request, result));
    }

    async Task ReceiveSelectionAsync(GameSession session, CharacterMenuRequestTicket ticket,
        AccountIdentity account, AccountContext owner, RequestContext request, Guid characterId)
    {
        CharacterSelectionHandle? handle = null;
        CatalogResult result;
        try
        {
            result = await Task.Run(async () =>
            {
                CharacterSelectionHandle acquired = _catalog!.BeginSelectCharacter(
                    owner, characterId, Guid.NewGuid(), request, ticket.Deadline, _closed.Token);
                handle = acquired;
                _world.EnqueueAdmissionResult(() =>
                {
                    if (session.IsClosing || !_requests.IsCurrent(ticket)) ObserveClose(acquired, null);
                    else _selection = acquired;
                });
                return await acquired.Completion.ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (Exception)
        {
            result = new(CatalogStatus.OutcomeUnknown, request);
        }
        _world.EnqueueAdmissionResult(() => Apply(session, ticket, account, request, result, handle, characterId));
    }

    void Apply(GameSession session, CharacterMenuRequestTicket ticket, AccountIdentity account,
        RequestContext request, CatalogResult result, CharacterSelectionHandle? handle = null,
        Guid selectedCharacterId = default)
    {
        if (session.IsClosing || !_requests.IsCurrent(ticket))
        {
            if (handle is not null) ObserveClose(handle, null);
            return;
        }
        PacketID packetId = (PacketID)ticket.RequestPacketId;
        bool selecting = packetId == PacketID.C_CharacterSelectRequest;
        bool admitted = false;
        try
        {
            if (session.Account != account || result.Request != request ||
                (selecting ? !session.CanApplyCatalogSelection : !session.CanUseCharacterMenu))
            {
                SendFailure(session, ticket.RequestId, packetId, CharacterMenuResult.IntegrityFailure);
                return;
            }

            IPacket reply;
            if (packetId == PacketID.C_CharacterListRequest && result.Status == CatalogStatus.Listed)
            {
                reply = CharacterMenuReplyFactory.ListSuccess(ticket.RequestId,
                    result.Characters.Select(character =>
                        (character.CharacterId, character.Slot, character.Name, character.Class)).ToArray());
            }
            else if (packetId == PacketID.C_CharacterCreateRequest &&
                result.Status is CatalogStatus.Created or CatalogStatus.Applied && result.Character is { } created)
            {
                reply = CharacterMenuReplyFactory.CreateSuccess(ticket.RequestId,
                    (created.CharacterId, created.Slot, created.Name, created.Class));
            }
            else if (selecting && result.Status == CatalogStatus.Selected &&
                handle is not null && ReferenceEquals(_selection, handle) && result.Lease is not null &&
                result.Character is { } selected && selected.CharacterId == selectedCharacterId &&
                selected.Class is CharacterClass.Knight or CharacterClass.Mage)
            {
                session.Send(CharacterMenuReplyFactory.SelectSuccess(ticket.RequestId, selected.CharacterId).Write());
                // 54 is correlation only. The existing 48/entry path consumes the stored class.
                session.CompleteCatalogSelection(selected.Class);
                admitted = true;
                return;
            }
            else
            {
                CatalogStatus failureStatus = packetId == PacketID.C_CharacterCreateRequest && result.Status == CatalogStatus.NotApplied
                    ? result.Rejection ?? CatalogStatus.IntegrityFailure : result.Status;
                var failure = CharacterMenuCatalogResultMapper.MapFailure(failureStatus, result.InvalidField);
                Console.WriteLine($"[CharacterMenu] {packetId}: {result.Status}/{result.InvalidField}, rejection:{result.Rejection}");
                reply = CharacterMenuReplyFactory.Failure(ticket.RequestId, packetId, failure.Result, failure.InvalidField);
            }
            if (!session.IsClosing) session.Send(reply.Write());
            if (result.Status is CatalogStatus.OutcomeUnknown or CatalogStatus.StillUnknown) session.Disconnect();
        }
        catch (Exception)
        {
            // Sending is not a storage rollback. Core retains dispatched work and its owner.
            session.Disconnect();
        }
        finally
        {
            _requests.Complete(ticket);
            if (selecting && !admitted)
            {
                if (handle is not null) ObserveClose(handle, null);
                if (ReferenceEquals(_selection, handle)) _selection = null;
                session.RejectCatalogSelection();
            }
        }
    }

    void Reject(GameSession session, CharacterMenuRequestTicket ticket, CharacterMenuResult result,
        CharacterMenuInvalidField invalidField = CharacterMenuInvalidField.None)
    {
        try { SendFailure(session, ticket.RequestId, (PacketID)ticket.RequestPacketId, result, invalidField); }
        finally { _requests.Complete(ticket); }
    }

    static void SendFailure(GameSession session, uint requestId, PacketID packetId, CharacterMenuResult result,
        CharacterMenuInvalidField invalidField = CharacterMenuInvalidField.None)
    {
        try
        {
            if (!session.IsClosing)
                session.Send(CharacterMenuReplyFactory.Failure(requestId, packetId, result, invalidField).Write());
        }
        catch (Exception)
        {
            session.Disconnect();
        }
    }

    static void ObserveClose(CharacterSelectionHandle handle, SafeCheckpoint? projection)
        => _ = CharacterCatalogService.ObserveAsync(handle.Close(projection));
}
