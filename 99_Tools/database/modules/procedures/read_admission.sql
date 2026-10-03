CREATE OR ALTER PROCEDURE dh.ReadAdmission
    @SlotId int,
    @AccountId uniqueidentifier,
    @CharacterId uniqueidentifier,
    @LockTimeoutMs int
AS
BEGIN
    -- Contract discriminators used by this RPC; numeric values remain the v1 wire/ledger contract.
    DECLARE @FreeOwnerKind tinyint = 0;
    -- Result codes are wire/ledger contracts; validation priority remains binding, fence, owner, sequence, game.
    DECLARE @IdentityMismatch smallint = 203;
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
    IF @SlotId IS NULL OR @SlotId <> 1 OR @AccountId IS NULL OR @CharacterId IS NULL
        OR @AccountId = '00000000-0000-0000-0000-000000000000'
        OR @CharacterId = '00000000-0000-0000-0000-000000000000'
        OR @LockTimeoutMs IS NULL OR @LockTimeoutMs < 1 OR @LockTimeoutMs > 2000
        THROW 51020, 'InvalidRequest.', 1;
    DECLARE @OperationId uniqueidentifier = NULL;
    DECLARE @Kind int = NULL;
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
    -- Admission/recovery preflight exposes ordered deployment identity through execute-only roles.
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
        SET @accountPresent = NULL;
        SET @characterPresent = NULL;
        SET @progressPresent = NULL;
        SET @migrationManifest = (SELECT Version AS version,
            Name AS name,
            Checksum AS checksum
            FROM dh.SchemaVersion ORDER BY Version FOR JSON PATH);
        IF @AccountId <> @boundAccount OR @CharacterId <> @boundCharacter
        BEGIN
            SET @status = 'IdentityMismatch';
            SET @resultCode = @IdentityMismatch;
        END
        ELSE
            SET @status = CASE WHEN @currentOwnerKind = @FreeOwnerKind THEN 'AdmissionFree' ELSE 'AdmissionHeld' END;
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
