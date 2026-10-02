CREATE PROCEDURE dh.RecoverAndLoad
    @SlotId int,
    @AccountId uniqueidentifier,
    @CharacterId uniqueidentifier,
    @OperationId uniqueidentifier,
    @OwnerId uniqueidentifier,
    @ExpectedFence bigint,
    @Sequence bigint,
    @ExpectedOwnerKind int,
    @ExpectedOwnerId uniqueidentifier,
    @Reason nvarchar(max),
    @LockTimeoutMs int
AS
BEGIN
    -- Contract discriminators used by this RPC; numeric values remain the v1 wire/ledger contract.
    DECLARE @RecoveryOwnerKind tinyint = 2;
    DECLARE @KnightClass tinyint = 0;
    DECLARE @MageClass tinyint = 1;
    DECLARE @RecoverKind int = 4;
    DECLARE @MaxCounterValue bigint = 9223372036854775807;
    -- Result codes are wire/ledger contracts; validation priority remains binding, fence, owner, sequence, game.
    DECLARE @Recovered smallint = 104;
    DECLARE @RecoveredAbsent smallint = 105;
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
    DECLARE @Kind int = @RecoverKind;
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
        @ExpectedOwnerKind = @ExpectedOwnerKind,
        @ExpectedOwnerId = @ExpectedOwnerId,
        @Reason = @Reason;
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
    -- Current diagnostic JSON and fresh historical snapshot.
    DECLARE @storedProgress nvarchar(max);
    DECLARE @safe nvarchar(max);
    DECLARE @snapshot nvarchar(max);
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
        EXEC @lockResult = sys.sp_getapplock @Resource = N'Dawnholder.Persistence.Slot.1',
            @DbPrincipal = 'public',
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = @LockTimeoutMs;
        IF @lockResult NOT IN (0, 1)
        BEGIN
            IF XACT_STATE() <> 0
                ROLLBACK TRANSACTION;
            THROW 51022, 'Slot application lock was not acquired.', 1;
        END;
        SELECT @boundAccount = AccountId,
            @boundCharacter = CharacterId,
            @currentFence = Fence,
            @currentOwnerKind = OwnerKind,
            @currentOwner = OwnerId,
            @currentSequence = LastSequence
        FROM dh.CharacterAuthority WITH (UPDLOCK, HOLDLOCK) WHERE SlotId = 1;
        IF @boundAccount IS NULL
            THROW 51023, 'Required slot binding is absent.', 1;
        -- Preserve Authority → schema → Operation validation under the same transaction/applock.
        EXEC dh.AssertPersistenceContract
            @SchemaVersion = @schemaVersion OUTPUT;
        SELECT @outcome = Outcome,
            @resultCode = ResultCode,
            @resultSnapshot = ResultSnapshot,
            @recordedUtc = RecordedUtc,
            @isReplay = 1,
            @status = CASE WHEN Kind = @Kind AND PayloadVersion = 1
            AND DATALENGTH(Payload) = DATALENGTH(@payload) AND Payload = @payload
            THEN 'Terminal' ELSE 'OperationPayloadMismatch' END
        FROM dh.CharacterOperation WITH (UPDLOCK, HOLDLOCK) WHERE OperationId = @OperationId;
        IF @status = 'OperationPayloadMismatch'
        BEGIN
            -- Never expose a different request's historical receipt as this request's result.
            SET @outcome = NULL;
            SET @resultCode = NULL;
            SET @resultSnapshot = NULL;
            SET @recordedUtc = NULL;
            SET @isReplay = 0;
        END;
        SELECT @accountPresent = 1 FROM dh.Account WITH (UPDLOCK, HOLDLOCK) WHERE AccountId = @boundAccount;
        SELECT @characterPresent = 1,
            @storedAccount = AccountId,
            @storedClass = Class,
            @characterVersion = Version
        FROM dh.Character WITH (UPDLOCK, HOLDLOCK) WHERE CharacterId = @boundCharacter;
        SELECT @progressPresent = 1,
            @mapId = MapId,
            @positionX = PositionX,
            @positionY = PositionY,
            @hp = Hp,
            @storedMaxHp = MaxHp,
            @bossUnlocked = BossUnlocked,
            @progressVersion = Version
        FROM dh.CharacterProgress WITH (UPDLOCK, HOLDLOCK) WHERE CharacterId = @boundCharacter;
        IF @isReplay = 0 AND @status = 'Terminal'
        BEGIN
            IF @AccountId <> @boundAccount OR @CharacterId <> @boundCharacter
                SET @resultCode = @IdentityMismatch;
            ELSE IF @ExpectedFence <> @currentFence
                SET @resultCode = @StaleFence;
            ELSE IF @ExpectedOwnerKind <> @currentOwnerKind
                OR (@ExpectedOwnerId IS NULL AND @currentOwner IS NOT NULL)
                OR (@ExpectedOwnerId IS NOT NULL AND @currentOwner IS NULL)
                OR @ExpectedOwnerId <> @currentOwner
                SET @resultCode = @Busy;
            ELSE IF @Sequence <> 0
                SET @resultCode = @SequenceMismatch;
            -- First failure wins: later game checks run only while resultCode is unset.
            -- Binding/fence/owner/sequence rejection therefore keeps its original priority.
            IF @resultCode IS NULL AND @characterPresent = 1 AND @storedAccount <> @boundAccount
                SET @resultCode = @IdentityMismatch;
            IF @resultCode IS NULL AND ((@characterPresent = 1 AND (@accountPresent = 0 OR @storedClass NOT IN (@KnightClass, @MageClass)))
                OR (@progressPresent = 1 AND @characterPresent = 0))
                SET @resultCode = @IntegrityFailure;
            -- Reject the maximum before +1: a fence must never wrap or reuse an old value.
            IF @resultCode IS NULL AND @currentFence = @MaxCounterValue
                SET @resultCode = @IntegrityFailure;
            IF @resultCode IS NULL
            BEGIN
                -- No Account/Character/Progress writes and no runtime safe projection.
                SET @currentFence = @currentFence + 1;
                SET @currentOwnerKind = @RecoveryOwnerKind;
                SET @currentOwner = @OwnerId;
                SET @currentSequence = 0;
                UPDATE dh.CharacterAuthority SET Fence = @currentFence,
                    OwnerKind = @currentOwnerKind,
                    OwnerId = @currentOwner,
                    LastSequence = @currentSequence,
                    ChangedUtc = SYSUTCDATETIME() WHERE SlotId = 1;
                IF @@ROWCOUNT <> 1
                    THROW 51024, 'Authority update failed.', 1;
                SET @resultCode = CASE WHEN @characterPresent = 1 THEN @Recovered ELSE @RecoveredAbsent END;
                SET @outcome = @Applied;
            END
            ELSE
                SET @outcome = @NotApplied;
        END;
        SET @storedProgress = NULL;
        IF @progressPresent = 1
            SET @storedProgress = (SELECT @mapId AS mapId,
                CONVERT(varchar(32), @positionX, 3) AS x,
                CONVERT(varchar(32), @positionY, 3) AS y,
                @hp AS hp,
                @storedMaxHp AS maxHp,
                @bossUnlocked AS bossUnlocked
                FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES);
        -- Current JSON reflects final game observations; only fresh Applied proof snapshots reuse it.
        IF @isReplay = 0 AND @status = 'Terminal'
        BEGIN
            IF @outcome = @Applied
            BEGIN
                SET @snapshot = (SELECT 1 AS version,
                    @Kind AS kind,
                    @resultCode AS resultCode,
                    1 AS slotId,
                    LOWER(CONVERT(char(36), @boundAccount)) AS accountId,
                    LOWER(CONVERT(char(36), @boundCharacter)) AS characterId,
                    @currentOwnerKind AS ownerKind,
                    LOWER(CONVERT(char(36), @currentOwner)) AS ownerId,
                    CONVERT(varchar(20), @currentFence) AS fence,
                    CONVERT(varchar(20), @currentSequence) AS sequence,
                    @characterPresent AS characterPresent,
                    @storedClass AS class,
                    CONVERT(char(16), @characterVersion, 2) AS characterVersionHex,
                    @progressPresent AS progressPresent,
                    CONVERT(char(16), @progressVersion, 2) AS progressVersionHex,
                    JSON_QUERY(@safe) AS safe,
                    JSON_QUERY(@storedProgress) AS storedProgress
                    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES);
                IF DATALENGTH(@snapshot) > 4096 OR ISJSON(@snapshot) <> 1
                    THROW 51024, 'Snapshot serialization failed.', 1;
                SET @resultSnapshot = CONVERT(nvarchar(2048), @snapshot);
            END;
            SET @recordedUtc = SYSUTCDATETIME();
            INSERT dh.CharacterOperation
            (
                OperationId,
                SlotId,
                Kind,
                PayloadVersion,
                Payload,
                Outcome,
                ResultCode,
                ResultSnapshot,
                RecordedUtc
            )
            VALUES
            (
                @OperationId,
                1,
                @Kind,
                1,
                @payload,
                @outcome,
                @resultCode,
                @resultSnapshot,
                @recordedUtc
            );
        END;
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
    SELECT CONVERT(int, 1) AS TransportVersion,
        @status AS Status,
        @OperationId AS OperationId,
        CONVERT(int, @Kind) AS Kind,
        @outcome AS Outcome,
        @resultCode AS ResultCode,
        @resultSnapshot AS ResultSnapshot,
        @isReplay AS IsReplay,
        @recordedUtc AS RecordedUtc,
        @databaseName AS DatabaseName,
        @productVersion AS ProductVersion,
        @schemaVersion AS SchemaVersion,
        CONVERT(int, 1) AS PayloadVersion,
        CONVERT(int, 1) AS SnapshotVersion,
        @migrationManifest AS MigrationManifest,
        CONVERT(int, 1) AS CurrentSlotId,
        @boundAccount AS CurrentAccountId,
        @boundCharacter AS CurrentCharacterId,
        @currentOwnerKind AS CurrentOwnerKind,
        @currentOwner AS CurrentOwnerId,
        @currentFence AS CurrentFence,
        @currentSequence AS CurrentLastSequence,
        @accountPresent AS CurrentAccountPresent,
        @characterPresent AS CurrentCharacterPresent,
        @storedClass AS CurrentClass,
        @characterVersion AS CurrentCharacterVersion,
        @progressPresent AS CurrentProgressPresent,
        @progressVersion AS CurrentProgressVersion,
        @storedProgress AS CurrentStoredProgress;
END;
