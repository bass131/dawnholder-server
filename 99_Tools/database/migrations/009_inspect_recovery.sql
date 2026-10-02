CREATE PROCEDURE dh.InspectRecovery
    @SlotId int, @AccountId uniqueidentifier, @CharacterId uniqueidentifier, @LockTimeoutMs int
AS
BEGIN
    IF @@TRANCOUNT<>0 THROW 51021, 'Ambient transaction is not accepted.', 1;
    SET IMPLICIT_TRANSACTIONS OFF;
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    IF @SlotId IS NULL OR @SlotId<>1 OR @AccountId IS NULL OR @CharacterId IS NULL
       OR @AccountId='00000000-0000-0000-0000-000000000000'
       OR @CharacterId='00000000-0000-0000-0000-000000000000'
       OR @LockTimeoutMs IS NULL OR @LockTimeoutMs<1 OR @LockTimeoutMs>2000
        THROW 51020, 'InvalidRequest.', 1;
    DECLARE @OperationId uniqueidentifier=NULL,@Kind int=NULL;
    DECLARE @lockResult int, @boundAccount uniqueidentifier, @boundCharacter uniqueidentifier,
        @currentFence bigint, @currentOwnerKind tinyint, @currentOwner uniqueidentifier, @currentSequence bigint,
        @accountPresent bit=0, @characterPresent bit=0, @progressPresent bit=0,
        @storedAccount uniqueidentifier, @storedClass tinyint, @characterVersion binary(8), @progressVersion binary(8),
        @mapId tinyint, @positionX real, @positionY real, @hp int, @storedMaxHp int, @bossUnlocked bit,
        @storedProgress nvarchar(max), @safe nvarchar(max), @snapshot nvarchar(max),
        @status varchar(32)='Terminal', @outcome tinyint, @resultCode smallint, @resultSnapshot nvarchar(2048),
        @isReplay bit=0, @recordedUtc datetime2(3), @safeMaxHp int,
        @migrationManifest nvarchar(max), @productVersion nvarchar(128)=CONVERT(nvarchar(128),SERVERPROPERTY('ProductVersion')),
        @databaseName nvarchar(128)=DB_NAME();
    BEGIN TRY
        BEGIN TRANSACTION;
        EXEC @lockResult=sys.sp_getapplock @Resource=N'Dawnholder.Persistence.Slot.1',
            @DbPrincipal='public', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=@LockTimeoutMs;
        IF @lockResult NOT IN (0,1)
        BEGIN
            IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
            THROW 51022, 'Slot application lock was not acquired.', 1;
        END;
        SELECT @boundAccount=AccountId,@boundCharacter=CharacterId,@currentFence=Fence,
            @currentOwnerKind=OwnerKind,@currentOwner=OwnerId,@currentSequence=LastSequence
        FROM dh.CharacterAuthority WITH (UPDLOCK,HOLDLOCK) WHERE SlotId=1;
        IF @boundAccount IS NULL THROW 51023, 'Required slot binding is absent.', 1;
        IF (SELECT COUNT(*) FROM dh.SchemaVersion)<>13
           OR (SELECT MIN(Version) FROM dh.SchemaVersion)<>1
           OR (SELECT MAX(Version) FROM dh.SchemaVersion)<>13
            THROW 51023, 'Persistence schema version is incompatible.', 1;
        SELECT @accountPresent=1 FROM dh.Account WITH (UPDLOCK,HOLDLOCK) WHERE AccountId=@boundAccount;
        SELECT @characterPresent=1,@storedAccount=AccountId,@storedClass=Class,@characterVersion=Version
        FROM dh.Character WITH (UPDLOCK,HOLDLOCK) WHERE CharacterId=@boundCharacter;
        SELECT @progressPresent=1,@mapId=MapId,@positionX=PositionX,@positionY=PositionY,
            @hp=Hp,@storedMaxHp=MaxHp,@bossUnlocked=BossUnlocked,@progressVersion=Version
        FROM dh.CharacterProgress WITH (UPDLOCK,HOLDLOCK) WHERE CharacterId=@boundCharacter;
        SET @migrationManifest=(SELECT Version AS version,Name AS name,Checksum AS checksum
            FROM dh.SchemaVersion ORDER BY Version FOR JSON PATH);
        IF @AccountId<>@boundAccount OR @CharacterId<>@boundCharacter
        BEGIN
            SET @status='IdentityMismatch'; SET @resultCode=203;
        END
        ELSE IF @characterPresent=1 AND @storedAccount<>@boundAccount
        BEGIN
            SET @status='IdentityMismatch'; SET @resultCode=203;
        END
        ELSE IF (@characterPresent=1 AND (@accountPresent=0 OR @storedClass NOT IN (0,1)))
            OR (@progressPresent=1 AND @characterPresent=0)
        BEGIN
            SET @status='IntegrityFailure'; SET @resultCode=207;
        END
        ELSE SET @status='Inspected';
        SET @storedProgress=NULL;
        IF @progressPresent=1
            SET @storedProgress=(SELECT @mapId AS mapId,CONVERT(varchar(32),@positionX,3) AS x,
                CONVERT(varchar(32),@positionY,3) AS y,@hp AS hp,@storedMaxHp AS maxHp,@bossUnlocked AS bossUnlocked
                FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES);
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
    IF @@TRANCOUNT<>0 THROW 51025, 'Transaction count did not return to zero.', 1;
    SELECT CONVERT(int,1) AS TransportVersion,@status AS Status,
        @OperationId AS OperationId,CONVERT(int,@Kind) AS Kind,@outcome AS Outcome,
        @resultCode AS ResultCode,@resultSnapshot AS ResultSnapshot,@isReplay AS IsReplay,@recordedUtc AS RecordedUtc,
        @databaseName AS DatabaseName,@productVersion AS ProductVersion,CONVERT(int,13) AS SchemaVersion,
        CONVERT(int,1) AS PayloadVersion,CONVERT(int,1) AS SnapshotVersion,@migrationManifest AS MigrationManifest,
        CONVERT(int,1) AS CurrentSlotId,@boundAccount AS CurrentAccountId,@boundCharacter AS CurrentCharacterId,
        @currentOwnerKind AS CurrentOwnerKind,@currentOwner AS CurrentOwnerId,@currentFence AS CurrentFence,
        @currentSequence AS CurrentLastSequence,@accountPresent AS CurrentAccountPresent,
        @characterPresent AS CurrentCharacterPresent,@storedClass AS CurrentClass,
        @characterVersion AS CurrentCharacterVersion,@progressPresent AS CurrentProgressPresent,
        @progressVersion AS CurrentProgressVersion,@storedProgress AS CurrentStoredProgress;
END;
