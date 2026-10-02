CREATE FUNCTION dh.PersistencePayloadV1
(
    @Kind int, @SlotId int, @AccountId uniqueidentifier, @CharacterId uniqueidentifier,
    @OwnerId uniqueidentifier, @ExpectedFence bigint, @Sequence bigint,
    @Class int, @TownX real, @TownY real, @KnightMaxHp int, @MageMaxHp int,
    @ExpectedCharacterVersion varbinary(max), @ExpectedProgressVersion varbinary(max),
    @MaxHp int, @ExpectedOwnerKind int, @ExpectedOwnerId uniqueidentifier, @Reason nvarchar(max)
)
RETURNS varbinary(512)
WITH SCHEMABINDING
AS
BEGIN
    -- Wide transport arguments prevent truncation/conversion before validation.
    IF @Kind IS NULL OR @Kind NOT IN (1,2,3,4,5) OR @SlotId IS NULL OR @SlotId<>1
       OR @AccountId IS NULL OR @CharacterId IS NULL OR @OwnerId IS NULL
       OR @AccountId='00000000-0000-0000-0000-000000000000'
       OR @CharacterId='00000000-0000-0000-0000-000000000000'
       OR @OwnerId='00000000-0000-0000-0000-000000000000'
       OR @ExpectedFence IS NULL OR @ExpectedFence<0 OR @Sequence IS NULL OR @Sequence<0
        RETURN NULL;

    IF @Kind IN (1,2)
    BEGIN
        IF @Class IS NULL OR @Class<0 OR @Class>255 OR @TownX IS NULL OR @TownY IS NULL
           OR ABS(CONVERT(float,@TownX))>3.4028234663852886E38
           OR ABS(CONVERT(float,@TownY))>3.4028234663852886E38
           OR @ExpectedOwnerKind IS NOT NULL OR @ExpectedOwnerId IS NOT NULL OR @Reason IS NOT NULL
            RETURN NULL;
        -- SQL real input conversion rejects nonfinite values; content bounds belong to the caller.
        IF @TownX=0 SET @TownX=CONVERT(real,0);
        IF @TownY=0 SET @TownY=CONVERT(real,0);
        IF @Kind=1 AND (@KnightMaxHp IS NULL OR @KnightMaxHp<=0 OR @MageMaxHp IS NULL OR @MageMaxHp<=0
            OR @ExpectedCharacterVersion IS NOT NULL OR @ExpectedProgressVersion IS NOT NULL OR @MaxHp IS NOT NULL)
            RETURN NULL;
        IF @Kind=2 AND (@ExpectedCharacterVersion IS NULL OR DATALENGTH(@ExpectedCharacterVersion)<>8
            OR (@ExpectedProgressVersion IS NOT NULL AND DATALENGTH(@ExpectedProgressVersion)<>8)
            OR @MaxHp IS NULL OR @MaxHp<=0 OR @KnightMaxHp IS NOT NULL OR @MageMaxHp IS NOT NULL)
            RETURN NULL;
    END
    ELSE
    BEGIN
        IF @Class IS NOT NULL OR @TownX IS NOT NULL OR @TownY IS NOT NULL
           OR @KnightMaxHp IS NOT NULL OR @MageMaxHp IS NOT NULL OR @MaxHp IS NOT NULL
           OR @ExpectedCharacterVersion IS NOT NULL OR @ExpectedProgressVersion IS NOT NULL
            RETURN NULL;
        IF @Kind IN (3,5) AND (@ExpectedOwnerKind IS NOT NULL OR @ExpectedOwnerId IS NOT NULL OR @Reason IS NOT NULL)
            RETURN NULL;
        IF @Kind=4 AND (@ExpectedOwnerKind IS NULL OR @ExpectedOwnerKind NOT IN (0,1,2)
            OR (@ExpectedOwnerKind=0 AND @ExpectedOwnerId IS NOT NULL)
            OR (@ExpectedOwnerKind IN (1,2) AND (@ExpectedOwnerId IS NULL
                OR @ExpectedOwnerId='00000000-0000-0000-0000-000000000000'))
            OR @Reason IS NULL OR LEN(@Reason)=0 OR DATALENGTH(@Reason)>256)
            RETURN NULL;
    END;

    DECLARE @payload varbinary(512)=0x01+CONVERT(binary(1),@Kind)+CONVERT(binary(1),@SlotId)
        +CONVERT(binary(16),@AccountId)+CONVERT(binary(16),@CharacterId)+CONVERT(binary(16),@OwnerId)
        +CONVERT(binary(8),@ExpectedFence)+CONVERT(binary(8),@Sequence);
    IF @Kind=1
        SET @payload=@payload+CONVERT(binary(1),@Class)+CONVERT(binary(4),@TownX)+CONVERT(binary(4),@TownY)
            +CONVERT(binary(4),@KnightMaxHp)+CONVERT(binary(4),@MageMaxHp);
    IF @Kind=2
        SET @payload=@payload+@ExpectedCharacterVersion
            +CASE WHEN @ExpectedProgressVersion IS NULL THEN 0x00 ELSE 0x01+@ExpectedProgressVersion END
            +CONVERT(binary(1),@Class)+CONVERT(binary(4),@TownX)+CONVERT(binary(4),@TownY)+CONVERT(binary(4),@MaxHp);
    IF @Kind=4
        SET @payload=@payload+CONVERT(binary(1),@ExpectedOwnerKind)
            +CASE WHEN @ExpectedOwnerId IS NULL THEN 0x00 ELSE 0x01+CONVERT(binary(16),@ExpectedOwnerId) END
            +CONVERT(binary(2),DATALENGTH(@Reason))+CONVERT(varbinary(256),@Reason);
    RETURN @payload;
END;
