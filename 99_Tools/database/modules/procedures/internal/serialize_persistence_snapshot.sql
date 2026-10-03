CREATE PROCEDURE dh.SerializePersistenceSnapshot
    @Kind int,
    @resultCode smallint,
    @boundAccount uniqueidentifier,
    @boundCharacter uniqueidentifier,
    @currentOwnerKind tinyint,
    @currentOwner uniqueidentifier,
    @currentFence bigint,
    @currentSequence bigint,
    @characterPresent bit,
    @storedClass tinyint,
    @characterVersion binary(8),
    @progressPresent bit,
    @progressVersion binary(8),
    @safe nvarchar(max),
    @storedProgress nvarchar(max),
    @resultSnapshot nvarchar(2048) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    -- Serialize an already-decided fresh Applied proof; nullable inputs preserve release/recovery shapes.
    DECLARE @snapshot nvarchar(max);
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
