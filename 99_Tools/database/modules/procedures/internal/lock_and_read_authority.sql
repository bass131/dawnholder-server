CREATE OR ALTER PROCEDURE dh.LockAndReadAuthority
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
    EXEC @lockResult = sys.sp_getapplock @Resource = N'Dawnholder.Persistence.Slot.1',
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
        FROM dh.CharacterAuthority WITH (UPDLOCK, HOLDLOCK) WHERE SlotId = 1;
    END;
END;
