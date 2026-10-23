CREATE OR ALTER PROCEDURE dh.LockAndReadAuthority
    @SlotId int,
    @CharacterId uniqueidentifier,
    @LockTimeoutMs int,
    @lockResult int OUTPUT,
    @boundAccount uniqueidentifier OUTPUT,
    @boundCharacter uniqueidentifier OUTPUT,
    @currentFence bigint OUTPUT,
    @currentOwnerKind tinyint OUTPUT,
    @currentOwner uniqueidentifier OUTPUT,
    @currentSequence bigint OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    -- The caller owns the transaction and lock-failure rollback/51022; never read Authority after a failed lock.
    SET @lockResult = NULL;
    SET @boundAccount = NULL;
    SET @boundCharacter = NULL;
    SET @currentFence = NULL;
    SET @currentOwnerKind = NULL;
    SET @currentOwner = NULL;
    SET @currentSequence = NULL;
    -- Row/table waits have a fixed 2000 ms bound; application locks retain the requested timeout.
    -- SET LOCK_TIMEOUT requires a literal, and a dynamic SET would end with its inner batch scope.
    SET LOCK_TIMEOUT 2000;
    EXEC @lockResult = sys.sp_getapplock @Resource = N'Dawnholder.SchemaMigration',
        @DbPrincipal = 'public', @LockMode = 'Shared', @LockOwner = 'Transaction', @LockTimeout = @LockTimeoutMs;
    IF @lockResult NOT IN (0, 1) RETURN;
    DECLARE @resource nvarchar(255) = N'Dawnholder.Persistence.Character.' + CONVERT(nvarchar(36), @CharacterId);
    EXEC @lockResult = sys.sp_getapplock @Resource = @resource,
        @DbPrincipal = 'public',
        @LockMode = 'Exclusive',
        @LockOwner = 'Transaction',
        @LockTimeout = @LockTimeoutMs;
    IF @lockResult IN (0, 1)
    BEGIN
        SELECT @boundAccount = AccountId,
            @boundCharacter = CharacterId,
            @currentFence = Fence,
            @currentOwnerKind = OwnerKind,
            @currentOwner = OwnerId,
            @currentSequence = LastSequence
        FROM dh.CharacterAuthority WITH (UPDLOCK) WHERE CharacterId = @CharacterId AND SlotId = @SlotId;
    END;
END;
