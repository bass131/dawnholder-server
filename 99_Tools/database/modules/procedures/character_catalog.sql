CREATE OR ALTER PROCEDURE dh.CharacterCatalog
    @Action varchar(16),
    @AccountId uniqueidentifier = NULL,
    @AccountName varchar(17) = NULL,
    @CharacterId uniqueidentifier = NULL,
    @OperationId uniqueidentifier = NULL,
    @Slot tinyint = NULL,
    @Name nvarchar(37) = NULL,
    @NameKey nvarchar(37) = NULL,
    @Class tinyint = NULL,
    @TownX real = NULL,
    @TownY real = NULL,
    @KnightMaxHp int = NULL,
    @MageMaxHp int = NULL,
    @LockTimeoutMs int
AS
BEGIN
    -- Actions are an internal execute-only RPC, not client wire values.
    IF @@TRANCOUNT <> 0 THROW 51021, 'Ambient transaction is not accepted.', 1;
    SET IMPLICIT_TRANSACTIONS OFF;
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    IF @Action IS NULL OR @Action NOT IN ('Preflight', 'List', 'Select', 'Create', 'Resolve')
        OR @LockTimeoutMs IS NULL OR @LockTimeoutMs NOT BETWEEN 1 AND 2000
        THROW 51020, 'Invalid catalog request.', 1;
    -- Row/table waits have a fixed 2000 ms bound; application locks retain the requested timeout.
    -- SET LOCK_TIMEOUT requires a literal, and a dynamic SET would end with its inner batch scope.
    SET LOCK_TIMEOUT 2000;
    IF @Action <> 'Preflight' AND
        (@AccountId IS NULL OR DATALENGTH(@AccountName) NOT BETWEEN 3 AND 16
        OR dh.AccountIdentity(@AccountName) IS NULL OR @AccountId <> dh.AccountIdentity(@AccountName))
        THROW 51020, 'Invalid account identity.', 1;
    IF @Action = 'Select' AND (@CharacterId IS NULL OR @CharacterId = '00000000-0000-0000-0000-000000000000')
        THROW 51020, 'Invalid character identity.', 1;
    IF @Action IN ('Create', 'Resolve') AND
        (@OperationId IS NULL OR @OperationId = '00000000-0000-0000-0000-000000000000'
        OR @Slot IS NULL OR @Slot NOT BETWEEN 1 AND 3 OR @Class IS NULL OR @Class NOT IN (0, 1)
        OR @Name IS NULL OR DATALENGTH(@Name) NOT BETWEEN 4 AND 24
        OR @Name COLLATE Latin1_General_100_BIN2 LIKE N'%[^가-힣A-Za-z0-9]%'
        OR @NameKey IS NULL OR DATALENGTH(@NameKey) <> DATALENGTH(@Name)
        OR @NameKey COLLATE Latin1_General_100_BIN2 <> UPPER(@Name COLLATE Latin1_General_100_BIN2))
        THROW 51020, 'Invalid create intent.', 1;
    IF @Action = 'Create' AND (@TownX IS NULL OR @TownY IS NULL OR @KnightMaxHp IS NULL
        OR @MageMaxHp IS NULL OR @KnightMaxHp <= 0 OR @MageMaxHp <= 0)
        THROW 51020, 'Invalid safe defaults.', 1;

    DECLARE @status varchar(32);
    DECLARE @rejection varchar(32);
    DECLARE @characters nvarchar(max);
    DECLARE @manifest nvarchar(max);
    DECLARE @schema int;
    DECLARE @lockResult int;
    DECLARE @resource nvarchar(255);
    DECLARE @payload varbinary(512);
    DECLARE @storedPayload varbinary(512);
    DECLARE @storedAccount uniqueidentifier;
    DECLARE @storedResult varchar(32);
    DECLARE @resultCharacter uniqueidentifier;
    DECLARE @resultSlot tinyint;
    DECLARE @resultName nvarchar(12);
    DECLARE @resultClass tinyint;

    BEGIN TRY
        BEGIN TRANSACTION;
        EXEC @lockResult = sys.sp_getapplock @Resource = N'Dawnholder.SchemaMigration',
            @LockMode = 'Shared', @LockOwner = 'Transaction', @LockTimeout = @LockTimeoutMs;
        IF @lockResult NOT IN (0, 1) THROW 51022, 'Schema lock unavailable.', 1;
        EXEC dh.AssertPersistenceContract @SchemaVersion = @schema OUTPUT;
        IF @Action = 'Preflight'
        BEGIN
            SET @status = 'Listed';
            SET @manifest = (SELECT Version AS version, Name AS name, Checksum AS checksum
                FROM dh.SchemaVersion ORDER BY Version FOR JSON PATH);
        END
        ELSE IF EXISTS(SELECT 1 FROM dh.Account WHERE
            (AccountId = @AccountId AND NormalizedName <> @AccountName COLLATE Latin1_General_100_BIN2)
            OR (NormalizedName = @AccountName COLLATE Latin1_General_100_BIN2 AND AccountId <> @AccountId))
            SET @status = 'IntegrityFailure';
        ELSE IF @Action = 'List'
        BEGIN
            SET @characters = (SELECT CharacterId AS characterId, Slot AS slot, Name AS name, Class AS class
                FROM dh.Character WHERE AccountId = @AccountId ORDER BY Slot FOR JSON PATH);
            SET @status = 'Listed';
        END
        ELSE IF @Action = 'Select'
        BEGIN
            SELECT @storedAccount = AccountId, @resultCharacter = CharacterId,
                @resultSlot = Slot, @resultName = Name, @resultClass = Class
            FROM dh.Character WHERE CharacterId = @CharacterId;
            SET @status = CASE WHEN @storedAccount IS NULL THEN 'CharacterNotFound'
                WHEN @storedAccount <> @AccountId THEN 'NotOwned' ELSE 'Selected' END;
            IF @status <> 'Selected'
            BEGIN
                SET @resultCharacter = NULL;
                SET @resultSlot = NULL;
                SET @resultName = NULL;
                SET @resultClass = NULL;
            END;
        END
        ELSE
        BEGIN
            -- Length-prefix the variable UTF-16 fields; correlation, defaults and retry time are not the intent.
            SET @payload = 0x0106 + CONVERT(binary(16), @AccountId) +
                CONVERT(binary(1), DATALENGTH(@AccountName)) + CONVERT(varbinary(16), @AccountName) +
                CONVERT(binary(1), @Slot) + CONVERT(binary(1), DATALENGTH(@Name)) + CONVERT(varbinary(24), @Name) +
                CONVERT(binary(1), DATALENGTH(@NameKey)) + CONVERT(varbinary(24), @NameKey) + CONVERT(binary(1), @Class);
            SET @resource = N'Dawnholder.Persistence.Operation.' + CONVERT(nvarchar(36), @OperationId);
            EXEC @lockResult = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = @LockTimeoutMs;
            IF @lockResult NOT IN (0, 1) THROW 51022, 'Operation lock unavailable.', 1;
            SELECT @storedPayload = Payload, @storedResult = ResultCode, @resultCharacter = CharacterId
            FROM dh.CharacterCreateOperation WHERE OperationId = @OperationId;
            IF EXISTS(SELECT 1 FROM dh.CharacterOperation WHERE OperationId = @OperationId)
                OR (@storedPayload IS NOT NULL AND
                    (DATALENGTH(@storedPayload) <> DATALENGTH(@payload) OR @storedPayload <> @payload))
                SET @status = 'OperationConflict';
            ELSE IF @storedPayload IS NULL
            BEGIN
                IF @Action = 'Resolve'
                    -- A tombstone under the same operation lock also excludes any late dispatch of this ID.
                    SET @storedResult = 'CancelledBeforeDispatch';
                ELSE
                BEGIN
                    SET @resource = N'Dawnholder.Catalog.Account.' + CONVERT(nvarchar(36), @AccountId);
                    EXEC @lockResult = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive',
                        @LockOwner = 'Transaction', @LockTimeout = @LockTimeoutMs;
                    IF @lockResult NOT IN (0, 1) THROW 51022, 'Account lock unavailable.', 1;
                    SET @resource = N'Dawnholder.Catalog.Name.' + @NameKey;
                    EXEC @lockResult = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive',
                        @LockOwner = 'Transaction', @LockTimeout = @LockTimeoutMs;
                    IF @lockResult NOT IN (0, 1) THROW 51022, 'Name lock unavailable.', 1;
                    IF (SELECT COUNT(*) FROM dh.Character WHERE AccountId = @AccountId) >= 3
                        SET @storedResult = 'SlotsFull';
                    ELSE IF EXISTS(SELECT 1 FROM dh.Character WHERE AccountId = @AccountId AND Slot = @Slot)
                        SET @storedResult = 'SlotOccupied';
                    ELSE IF EXISTS(SELECT 1 FROM dh.Character WHERE NameKey = @NameKey COLLATE Latin1_General_100_BIN2)
                        SET @storedResult = 'NameTaken';
                    ELSE
                    BEGIN
                        IF NOT EXISTS(SELECT 1 FROM dh.Account WHERE AccountId = @AccountId)
                            INSERT dh.Account(AccountId, NormalizedName) VALUES (@AccountId, @AccountName);
                        SET @resultCharacter = NEWID();
                        INSERT dh.Character(CharacterId, AccountId, Slot, Name, NameKey, Class)
                            VALUES (@resultCharacter, @AccountId, @Slot, @Name, @NameKey, @Class);
                        DECLARE @hp int = CASE WHEN @Class = 0 THEN @KnightMaxHp ELSE @MageMaxHp END;
                        INSERT dh.CharacterProgress(CharacterId, MapId, PositionX, PositionY, Hp, MaxHp, BossUnlocked)
                            VALUES (@resultCharacter, 0, @TownX, @TownY, @hp, @hp, 0);
                        INSERT dh.CharacterAuthority(SlotId, AccountId, CharacterId, Fence, OwnerKind, OwnerId,
                            LastSequence, ChangedUtc)
                            VALUES (@Slot, @AccountId, @resultCharacter, 0, 0, NULL, 0, SYSUTCDATETIME());
                        SET @storedResult = 'Created';
                    END;
                END;
                INSERT dh.CharacterCreateOperation(OperationId, AccountId, Payload, ResultCode, CharacterId, RecordedUtc)
                    VALUES (@OperationId, @AccountId, @payload, @storedResult, @resultCharacter, SYSUTCDATETIME());
            END;
            IF @status IS NULL
            BEGIN
                IF @storedResult = 'Created'
                BEGIN
                    SELECT @resultSlot = Slot, @resultName = Name, @resultClass = Class
                    FROM dh.Character WHERE CharacterId = @resultCharacter AND AccountId = @AccountId;
                    IF @resultSlot IS NULL THROW 51024, 'Create receipt character is missing.', 1;
                    SET @status = CASE WHEN @Action = 'Resolve' THEN 'Applied' ELSE 'Created' END;
                END
                ELSE
                BEGIN
                    SET @status = CASE WHEN @Action = 'Resolve' THEN 'NotApplied' ELSE @storedResult END;
                    IF @Action = 'Resolve' SET @rejection = @storedResult;
                END;
            END
            ELSE IF @status = 'OperationConflict'
                SET @resultCharacter = NULL;
        END;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
    -- The provider must consume the final DONE token as well as this one row.
    SELECT @status AS Status, @resultCharacter AS CharacterId, @resultSlot AS Slot,
        @resultName AS Name, @resultClass AS Class, @characters AS Characters,
        @rejection AS Rejection, @manifest AS MigrationManifest, @schema AS SchemaVersion, DB_NAME() AS DatabaseName;
END;
