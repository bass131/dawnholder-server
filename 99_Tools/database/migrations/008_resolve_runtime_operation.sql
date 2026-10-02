CREATE PROCEDURE dh.ResolveRuntimeOperation
    @SlotId int, @AccountId uniqueidentifier, @CharacterId uniqueidentifier,
    @OperationId uniqueidentifier, @OwnerId uniqueidentifier, @ExpectedFence bigint, @Sequence bigint,
    @Kind int, @SealIfAbsent int, @LockTimeoutMs int,
    @Class int=NULL, @TownX real=NULL, @TownY real=NULL, @KnightMaxHp int=NULL, @MageMaxHp int=NULL,
    @ExpectedCharacterVersion varbinary(max)=NULL, @ExpectedProgressVersion varbinary(max)=NULL,
    @MaxHp int=NULL, @ExpectedOwnerKind int=NULL, @ExpectedOwnerId uniqueidentifier=NULL, @Reason nvarchar(max)=NULL
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
    IF @Kind IS NULL OR @Kind NOT IN (1,2,3) OR @SealIfAbsent IS NULL OR @SealIfAbsent NOT IN (0,1)
       OR @OperationId IS NULL OR @OperationId='00000000-0000-0000-0000-000000000000'
        THROW 51020, 'InvalidRequest.', 1;
    DECLARE @payload varbinary(512)=dh.PersistencePayloadV1(
        @Kind,@SlotId,@AccountId,@CharacterId,@OwnerId,@ExpectedFence,@Sequence,
        @Class,@TownX,@TownY,@KnightMaxHp,@MageMaxHp,@ExpectedCharacterVersion,@ExpectedProgressVersion,
        @MaxHp,@ExpectedOwnerKind,@ExpectedOwnerId,@Reason);
    IF @payload IS NULL THROW 51020, 'InvalidRequest.', 1;
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
        SELECT @outcome=Outcome,@resultCode=ResultCode,@resultSnapshot=ResultSnapshot,
            @recordedUtc=RecordedUtc,@isReplay=1,
            @status=CASE WHEN Kind=@Kind AND PayloadVersion=1
                AND DATALENGTH(Payload)=DATALENGTH(@payload) AND Payload=@payload
                THEN 'Terminal' ELSE 'OperationPayloadMismatch' END
        FROM dh.CharacterOperation WITH (UPDLOCK,HOLDLOCK) WHERE OperationId=@OperationId;
        IF @status='OperationPayloadMismatch'
        BEGIN
            -- Never expose a different request's historical receipt as this request's result.
            SET @outcome=NULL; SET @resultCode=NULL; SET @resultSnapshot=NULL; SET @recordedUtc=NULL;
            SET @isReplay=0;
        END;
        SELECT @accountPresent=1 FROM dh.Account WITH (UPDLOCK,HOLDLOCK) WHERE AccountId=@boundAccount;
        SELECT @characterPresent=1,@storedAccount=AccountId,@storedClass=Class,@characterVersion=Version
        FROM dh.Character WITH (UPDLOCK,HOLDLOCK) WHERE CharacterId=@boundCharacter;
        SELECT @progressPresent=1,@mapId=MapId,@positionX=PositionX,@positionY=PositionY,
            @hp=Hp,@storedMaxHp=MaxHp,@bossUnlocked=BossUnlocked,@progressVersion=Version
        FROM dh.CharacterProgress WITH (UPDLOCK,HOLDLOCK) WHERE CharacterId=@boundCharacter;
        IF @isReplay=0 AND @status='Terminal'
        BEGIN
            IF @SealIfAbsent=0 SET @status='StillUnknown';
            ELSE
            BEGIN
                -- Seal this ID only; current authority, sequence and game data remain untouched.
                SET @outcome=2; SET @resultCode=205;
            SET @recordedUtc=SYSUTCDATETIME();
            INSERT dh.CharacterOperation(OperationId,SlotId,Kind,PayloadVersion,Payload,Outcome,ResultCode,ResultSnapshot,RecordedUtc)
            VALUES(@OperationId,1,@Kind,1,@payload,@outcome,@resultCode,@resultSnapshot,@recordedUtc);
            END;
        END;
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
