using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Catalog;
using Shared.Protocol;

namespace Dawnholder.Server.GameServer.Tests.Integration;

// One store behind both repository boundaries that outlives a server host, so a restarted host reads the
// rows, fences and tokens the previous host left. The fixed-answer test repositories cannot remember a
// created row. Every call settles synchronously and no progress token ever exists here.
internal sealed class InMemoryCharacterStore : ICharacterCatalogRepository, ICharacterRepository
{
    readonly object _gate = new();
    readonly List<Row> _rows = new();
    readonly Dictionary<Guid, CharacterSummary> _created = new();
    long _lastToken;
    int _createCalls;
    int _acquireCalls;

    internal int CreateCalls { get { lock (_gate) return _createCalls; } }
    internal int AcquireCalls { get { lock (_gate) return _acquireCalls; } }
    internal int HeldCount
    {
        get { lock (_gate) return _rows.Count(row => row.OwnerKind != PersistenceOwnerKind.Free); }
    }

    public Task<CatalogStorageResult> ListAsync(AccountContext account,
        PersistenceDeadline deadline, CancellationToken token)
        => Task.FromResult(new CatalogStorageResult(CatalogStatus.Listed, characters: Characters(account.AccountId)));

    // Refusals keep the SQL catalog's order: a full account, then an occupied slot, then a taken name.
    public Task<CatalogStorageResult> CreateAsync(CreateDescriptor descriptor, Guid operationId,
        SafeDefaults defaults, PersistenceDeadline deadline, CancellationToken token)
    {
        lock (_gate)
        {
            _createCalls++;
            Row[] owned = _rows.Where(row => row.AccountId == descriptor.Account.AccountId).ToArray();
            CatalogStatus? refusal = owned.Length >= 3 ? CatalogStatus.SlotsFull
                : owned.Any(row => row.Slot == descriptor.Slot) ? CatalogStatus.SlotOccupied
                : _rows.Any(row => row.NameKey == descriptor.NameKey) ? CatalogStatus.NameTaken
                : null;
            if (refusal is { } status) return Task.FromResult(new CatalogStorageResult(status));

            int maxHp = descriptor.Class == CharacterClass.Knight ? defaults.KnightMaxHp : defaults.MageMaxHp;
            SafeCheckpoint safe = new(descriptor.Class, defaults.TownX, defaults.TownY, maxHp);
            Row row = new(descriptor.Account.AccountId, Guid.NewGuid(), descriptor.Slot, descriptor.Name,
                descriptor.NameKey, safe, NextToken());
            _rows.Add(row);
            _created.Add(operationId, row.Summary);
            return Task.FromResult(new CatalogStorageResult(CatalogStatus.Created, row.Summary));
        }
    }

    public Task<CatalogStorageResult> ResolveCreateAsync(CreateDescriptor descriptor, Guid operationId,
        PersistenceDeadline deadline, CancellationToken token)
    {
        lock (_gate)
        {
            return Task.FromResult(_created.TryGetValue(operationId, out CharacterSummary? created)
                ? new CatalogStorageResult(CatalogStatus.Applied, created)
                : new CatalogStorageResult(CatalogStatus.NotApplied));
        }
    }

    public Task<CatalogStorageResult> ReadSelectionAsync(AccountContext account, Guid characterId,
        PersistenceDeadline deadline, CancellationToken token)
    {
        lock (_gate)
        {
            Row? row = _rows.FirstOrDefault(candidate => candidate.CharacterId == characterId);
            return Task.FromResult(row is null ? new CatalogStorageResult(CatalogStatus.CharacterNotFound)
                : row.AccountId != account.AccountId ? new CatalogStorageResult(CatalogStatus.NotOwned)
                : new CatalogStorageResult(CatalogStatus.Selected, row.Summary));
        }
    }

    public Task<ReadAdmissionResult> ReadAdmissionAsync(ReadAdmissionRequest request,
        PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Row? row = Bound(request.Identity);
            PersistenceAuthority observed = row?.Authority
                ?? new PersistenceAuthority(request.Identity, PersistenceOwnerKind.Free, null, 0, 0);
            return Task.FromResult(ReadAdmissionResult.Probe(
                new AdmissionProbe(observed, schemaCompatible: true, bindingMatches: row is not null)));
        }
    }

    // The scenarios never race one character, so any other observed authority is one stale refusal.
    public Task<AcquireAndLoadResult> AcquireAndLoadAsync(AcquireAndLoadRequest request,
        PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _acquireCalls++;
            OperationIdentity operation = request.Operation;
            Row? row = Bound(operation.Identity);
            if (row is null || row.OwnerKind != PersistenceOwnerKind.Free || row.Fence != operation.ExpectedFence)
                return Task.FromResult(AcquireAndLoadResult.Rejected(PersistenceResultCode.StaleFence));

            row.Hold(operation.OwnerId);
            OperationReceipt receipt = new(operation.OperationId, PersistenceOperationKind.Acquire,
                PersistenceResultCode.Acquired, row.Authority, row.Class, row.Safe, row.CharacterToken, null);
            return Task.FromResult(AcquireAndLoadResult.Acquired(receipt, row.Observation));
        }
    }

    public Task<WriteSafeCheckpointResult> WriteSafeCheckpointAsync(WriteSafeCheckpointRequest request,
        PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            OperationIdentity operation = request.Operation;
            Row? row = Held(request.Authority);
            if (row is null || !request.ExpectedCharacterToken.Equals(row.CharacterToken) ||
                request.Safe.CharacterClass != row.Class)
            {
                return Task.FromResult(WriteSafeCheckpointResult.Rejected(PersistenceResultCode.StaleFence));
            }

            row.Checkpoint(operation.Sequence, request.Safe, NextToken());
            OperationReceipt receipt = new(operation.OperationId, PersistenceOperationKind.Checkpoint,
                PersistenceResultCode.CheckpointApplied, row.Authority, row.Class, row.Safe, row.CharacterToken, null);
            return Task.FromResult(WriteSafeCheckpointResult.Durable(receipt, row.Observation));
        }
    }

    public Task<ReleaseResult> ReleaseAsync(ReleaseRequest request,
        PersistenceDeadline deadline, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Row? row = Held(request.Authority);
            if (row is null) return Task.FromResult(ReleaseResult.Rejected(PersistenceResultCode.StaleFence));

            row.Release();
            OperationReceipt receipt = new(request.Operation.OperationId, PersistenceOperationKind.ReleaseRuntime,
                PersistenceResultCode.Released, row.Authority, null, null, null, null);
            return Task.FromResult(ReleaseResult.Released(receipt, row.Observation));
        }
    }

    public Task<ResolveOperationResult> ResolveOperationAsync(ResolveOperationRequest request,
        PersistenceDeadline deadline, CancellationToken cancellationToken)
        => Task.FromResult(ResolveOperationResult.StillUnknown());

    // Entry checkpoints project the host's safe defaults, so a seeded checkpoint's values are never read back.
    internal void Seed(Guid accountId, Guid characterId, byte slot, string name, string nameKey,
        CharacterClass characterClass)
    {
        SafeCheckpoint safe = new(characterClass, 0f, 0f, 1);
        lock (_gate)
            _rows.Add(new(accountId, characterId, slot, name, nameKey, safe, NextToken()));
    }

    internal IReadOnlyList<CharacterSummary> Characters(Guid accountId)
    {
        lock (_gate)
        {
            return _rows.Where(row => row.AccountId == accountId)
                .Select(row => row.Summary)
                .OrderBy(summary => summary.Slot)
                .ToArray();
        }
    }

    Row? Bound(PersistenceIdentity identity) => _rows.FirstOrDefault(row => row.Identity.Equals(identity));

    Row? Held(PersistenceAuthority authority) => _rows.FirstOrDefault(row => row.Authority.Equals(authority));

    PersistenceToken NextToken() => new(BitConverter.GetBytes(++_lastToken));

    sealed class Row(Guid accountId, Guid characterId, byte slot, string name, string nameKey,
        SafeCheckpoint safe, PersistenceToken characterToken)
    {
        Guid? _ownerId;
        long _lastSequence;

        internal Guid AccountId { get; } = accountId;
        internal Guid CharacterId { get; } = characterId;
        internal byte Slot { get; } = slot;
        internal string Name { get; } = name;
        internal string NameKey { get; } = nameKey;
        internal SafeCheckpoint Safe { get; private set; } = safe;
        internal PersistenceToken CharacterToken { get; private set; } = characterToken;
        internal PersistenceOwnerKind OwnerKind { get; private set; } = PersistenceOwnerKind.Free;
        internal long Fence { get; private set; }
        internal CharacterClass Class => Safe.CharacterClass;
        internal CharacterSummary Summary => new(CharacterId, Slot, Name, Class);
        internal PersistenceIdentity Identity => new(Slot, AccountId, CharacterId);
        internal PersistenceAuthority Authority => new(Identity, OwnerKind, _ownerId, Fence, _lastSequence);
        internal PersistenceObservation Observation => new(Authority, CharacterToken, null);

        internal void Hold(Guid ownerId)
        {
            OwnerKind = PersistenceOwnerKind.Runtime;
            _ownerId = ownerId;
            Fence++;
            _lastSequence = 0;
        }

        internal void Checkpoint(long sequence, SafeCheckpoint checkpoint, PersistenceToken token)
        {
            _lastSequence = sequence;
            Safe = checkpoint;
            CharacterToken = token;
        }

        internal void Release()
        {
            OwnerKind = PersistenceOwnerKind.Free;
            _ownerId = null;
            Fence++;
            _lastSequence = 0;
        }
    }
}
