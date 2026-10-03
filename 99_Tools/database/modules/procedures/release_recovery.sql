CREATE OR ALTER PROCEDURE dh.ReleaseRecovery
    @SlotId int,
    @AccountId uniqueidentifier,
    @CharacterId uniqueidentifier,
    @OperationId uniqueidentifier,
    @OwnerId uniqueidentifier,
    @ExpectedFence bigint,
    @Sequence bigint,
    @LockTimeoutMs int
AS
BEGIN
    -- Contract discriminators used by this RPC; numeric values remain the v1 wire/ledger contract.
    DECLARE @RecoveryOwnerKind tinyint = 2;
    DECLARE @FreeOwnerKind tinyint = 0;
    DECLARE @KnightClass tinyint = 0;
    DECLARE @MageClass tinyint = 1;
    DECLARE @ReleaseRecoveryKind int = 5;
    DECLARE @MaxCounterValue bigint = 9223372036854775807;
    -- Result codes are wire/ledger contracts; validation priority remains binding, fence, owner, sequence, game.
    DECLARE @Released smallint = 103;
    DECLARE @Busy smallint = 200;
    DECLARE @StaleFence smallint = 201;
    DECLARE @IdentityMismatch smallint = 203;
    DECLARE @SequenceMismatch smallint = 206;
    DECLARE @IntegrityFailure smallint = 207;
    DECLARE @Applied tinyint = 1;
    DECLARE @NotApplied tinyint = 2;
    -- OwnerKind: 0 Free, 1 Runtime, 2 Recovery.
    -- Kind: 1 Acquire, 2 Checkpoint, 3 ReleaseRuntime, 4 Recover, 5 ReleaseRecovery.
    -- Errors: 51020 InvalidRequest, 51021 ambient transaction, 51022 applock,
    -- 51023 schema/binding, 51024 mutation/snapshot, 51025 transaction count.
    IF @@TRANCOUNT <> 0
        THROW 51021, 'Ambient transaction is not accepted.', 1;
    SET IMPLICIT_TRANSACTIONS OFF;
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    DECLARE @Kind int = @ReleaseRecoveryKind;
    IF @SlotId IS NULL OR @SlotId <> 1 OR @AccountId IS NULL OR @CharacterId IS NULL
        OR @AccountId = '00000000-0000-0000-0000-000000000000'
        OR @CharacterId = '00000000-0000-0000-0000-000000000000'
        OR @LockTimeoutMs IS NULL OR @LockTimeoutMs < 1 OR @LockTimeoutMs > 2000
        THROW 51020, 'InvalidRequest.', 1;
    IF @OperationId IS NULL OR @OperationId = '00000000-0000-0000-0000-000000000000'
        THROW 51020, 'InvalidRequest.', 1;
    -- Named arguments keep absent kind-specific fields explicit; no codec grant is added.
    DECLARE @payload varbinary(512);
    EXEC @payload = dh.PersistencePayloadV1
        @Kind = @Kind,
        @SlotId = @SlotId,
        @AccountId = @AccountId,
        @CharacterId = @CharacterId,
        @OwnerId = @OwnerId,
        @ExpectedFence = @ExpectedFence,
        @Sequence = @Sequence,
        @Class = NULL,
        @TownX = NULL,
        @TownY = NULL,
        @KnightMaxHp = NULL,
        @MageMaxHp = NULL,
        @ExpectedCharacterVersion = NULL,
        @ExpectedProgressVersion = NULL,
        @MaxHp = NULL,
        @ExpectedOwnerKind = NULL,
        @ExpectedOwnerId = NULL,
        @Reason = NULL;
    IF @payload IS NULL
        THROW 51020, 'InvalidRequest.', 1;
    -- Lock and current authority (observed inside the slot transaction).
    DECLARE @lockResult int;
    DECLARE @boundAccount uniqueidentifier;
    DECLARE @boundCharacter uniqueidentifier;
    DECLARE @currentFence bigint;
    DECLARE @currentOwnerKind tinyint;
    DECLARE @currentOwner uniqueidentifier;
    DECLARE @currentSequence bigint;
    -- Game-row observations and opaque versions.
    DECLARE @accountPresent bit = 0;
    DECLARE @characterPresent bit = 0;
    DECLARE @progressPresent bit = 0;
    DECLARE @storedAccount uniqueidentifier;
    DECLARE @storedClass tinyint;
    DECLARE @characterVersion binary(8);
    DECLARE @progressVersion binary(8);
    DECLARE @mapId tinyint;
    DECLARE @positionX real;
    DECLARE @positionY real;
    DECLARE @hp int;
    DECLARE @storedMaxHp int;
    DECLARE @bossUnlocked bit;
    -- Current diagnostic JSON; the snapshot helper writes the terminal proof below.
    DECLARE @storedProgress nvarchar(max);
    DECLARE @safe nvarchar(max);
    -- Terminal proof (replay remains the original ledger snapshot).
    DECLARE @status varchar(32) = 'Terminal';
    DECLARE @outcome tinyint;
    DECLARE @resultCode smallint;
    DECLARE @resultSnapshot nvarchar(2048);
    DECLARE @isReplay bit = 0;
    DECLARE @recordedUtc datetime2(3);
    DECLARE @safeMaxHp int;
    -- Deployment identity is returned by ReadAdmission/InspectRecovery preflight.
    -- Mutation/resolution rows carry operation proof and current state; this metadata field stays NULL.
    -- Preflight boundary: 01_Phases/goals/2026-10-01-persistence-technical-design/technical-spec.md, section 5.
    -- Current transport contract: 01_Phases/goals/2026-10-02-persistence-repository/goal.md.
    DECLARE @migrationManifest nvarchar(max);
    DECLARE @productVersion nvarchar(128) = CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion'));
    DECLARE @databaseName nvarchar(128) = DB_NAME();
    DECLARE @schemaVersion int;
    -- One transaction owns the slot lock, current observations, mutation and proof.
    BEGIN TRY
        BEGIN TRANSACTION;
        EXEC dh.LockAndReadAuthority
            @LockTimeoutMs = @LockTimeoutMs,
            @lockResult = @lockResult OUTPUT,
            @boundAccount = @boundAccount OUTPUT,
            @boundCharacter = @boundCharacter OUTPUT,
            @currentFence = @currentFence OUTPUT,
            @currentOwnerKind = @currentOwnerKind OUTPUT,
            @currentOwner = @currentOwner OUTPUT,
            @currentSequence = @currentSequence OUTPUT;
        IF @lockResult NOT IN (0, 1)
        BEGIN
            IF XACT_STATE() <> 0
                ROLLBACK TRANSACTION;
            THROW 51022, 'Slot application lock was not acquired.', 1;
        END;
        IF @boundAccount IS NULL
            THROW 51023, 'Required slot binding is absent.', 1;
        -- Preserve Authority → schema → Operation validation under the same transaction/applock.
        EXEC dh.AssertPersistenceContract
            @SchemaVersion = @schemaVersion OUTPUT;
        EXEC dh.ReadOperationReceipt
            @OperationId = @OperationId,
            @Kind = @Kind,
            @payload = @payload,
            @status = @status OUTPUT,
            @outcome = @outcome OUTPUT,
            @resultCode = @resultCode OUTPUT,
            @resultSnapshot = @resultSnapshot OUTPUT,
            @isReplay = @isReplay OUTPUT,
            @recordedUtc = @recordedUtc OUTPUT;
        EXEC dh.ReadCharacterState
            @boundAccount = @boundAccount,
            @boundCharacter = @boundCharacter,
            @accountPresent = @accountPresent OUTPUT,
            @characterPresent = @characterPresent OUTPUT,
            @progressPresent = @progressPresent OUTPUT,
            @storedAccount = @storedAccount OUTPUT,
            @storedClass = @storedClass OUTPUT,
            @characterVersion = @characterVersion OUTPUT,
            @progressVersion = @progressVersion OUTPUT,
            @mapId = @mapId OUTPUT,
            @positionX = @positionX OUTPUT,
            @positionY = @positionY OUTPUT,
            @hp = @hp OUTPUT,
            @storedMaxHp = @storedMaxHp OUTPUT,
            @bossUnlocked = @bossUnlocked OUTPUT;
        IF @isReplay = 0 AND @status = 'Terminal'
        BEGIN
            IF @AccountId <> @boundAccount OR @CharacterId <> @boundCharacter
                SET @resultCode = @IdentityMismatch;
            ELSE IF @ExpectedFence <> @currentFence
                SET @resultCode = @StaleFence;
            ELSE IF @currentOwnerKind <> @RecoveryOwnerKind OR @currentOwner <> @OwnerId OR @currentOwner IS NULL
                SET @resultCode = @Busy;
            -- Reject the maximum before +1: fences/sequences must never wrap or reuse an old value.
            ELSE IF @currentSequence = @MaxCounterValue
                SET @resultCode = @IntegrityFailure;
            ELSE IF @Sequence <> @currentSequence + 1
                SET @resultCode = @SequenceMismatch;
            -- First failure wins: later game checks run only while resultCode is unset.
            -- Binding/fence/owner/sequence rejection therefore keeps its original priority.
            IF @resultCode IS NULL AND @characterPresent = 1 AND @storedAccount <> @boundAccount
                SET @resultCode = @IdentityMismatch;
            IF @resultCode IS NULL
                AND ((@characterPresent = 1 AND (@accountPresent = 0 OR @storedClass NOT IN (@KnightClass, @MageClass)))
                OR (@progressPresent = 1 AND @characterPresent = 0))
                SET @resultCode = @IntegrityFailure;
            -- Reject the maximum before +1: a fence must never wrap or reuse an old value.
            IF @resultCode IS NULL AND @currentFence = @MaxCounterValue
                SET @resultCode = @IntegrityFailure;
            IF @resultCode IS NULL
            BEGIN
                SET @currentFence = @currentFence + 1;
                SET @currentOwnerKind = @FreeOwnerKind;
                SET @currentOwner = NULL;
                SET @currentSequence = 0;
                UPDATE dh.CharacterAuthority SET Fence = @currentFence,
                    OwnerKind = @currentOwnerKind,
                    OwnerId = @currentOwner,
                    LastSequence = @currentSequence,
                    ChangedUtc = SYSUTCDATETIME() WHERE SlotId = 1;
                IF @@ROWCOUNT <> 1
                    THROW 51024, 'Authority update failed.', 1;
                SET @resultCode = @Released;
                SET @outcome = @Applied;
                -- Release proves authority relinquishment only; game observations stay in current diagnostics.
                EXEC dh.SerializePersistenceSnapshot
                    @Kind = @Kind,
                    @resultCode = @resultCode,
                    @boundAccount = @boundAccount,
                    @boundCharacter = @boundCharacter,
                    @currentOwnerKind = @currentOwnerKind,
                    @currentOwner = @currentOwner,
                    @currentFence = @currentFence,
                    @currentSequence = @currentSequence,
                    @characterPresent = NULL,
                    @storedClass = NULL,
                    @characterVersion = NULL,
                    @progressPresent = NULL,
                    @progressVersion = NULL,
                    @safe = NULL,
                    @storedProgress = NULL,
                    @resultSnapshot = @resultSnapshot OUTPUT;
            END
            ELSE
                SET @outcome = @NotApplied;
            EXEC dh.RecordOperationReceipt
                @OperationId = @OperationId,
                @Kind = @Kind,
                @payload = @payload,
                @outcome = @outcome,
                @resultCode = @resultCode,
                @resultSnapshot = @resultSnapshot,
                @recordedUtc = @recordedUtc OUTPUT;
        END;
        SET @storedProgress = NULL;
        IF @progressPresent = 1
            EXEC dh.SerializeProgress
                @mapId = @mapId,
                @positionX = @positionX,
                @positionY = @positionY,
                @hp = @hp,
                @storedMaxHp = @storedMaxHp,
                @bossUnlocked = @bossUnlocked,
                @Json = @storedProgress OUTPUT;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
    IF @@TRANCOUNT <> 0
        THROW 51025, 'Transaction count did not return to zero.', 1;
    -- Emit one terminal row only after COMMIT; Current* does not replace historical ResultSnapshot.
    EXEC dh.EmitPersistenceResult
        @status = @status,
        @OperationId = @OperationId,
        @outcome = @outcome,
        @Kind = @Kind,
        @resultCode = @resultCode,
        @resultSnapshot = @resultSnapshot,
        @isReplay = @isReplay,
        @recordedUtc = @recordedUtc,
        @databaseName = @databaseName,
        @productVersion = @productVersion,
        @schemaVersion = @schemaVersion,
        @migrationManifest = @migrationManifest,
        @boundAccount = @boundAccount,
        @boundCharacter = @boundCharacter,
        @currentOwnerKind = @currentOwnerKind,
        @currentOwner = @currentOwner,
        @currentFence = @currentFence,
        @currentSequence = @currentSequence,
        @accountPresent = @accountPresent,
        @characterPresent = @characterPresent,
        @storedClass = @storedClass,
        @characterVersion = @characterVersion,
        @progressPresent = @progressPresent,
        @progressVersion = @progressVersion,
        @storedProgress = @storedProgress;
END;
