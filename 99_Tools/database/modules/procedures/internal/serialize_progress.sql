CREATE PROCEDURE dh.SerializeProgress
    @mapId tinyint,
    @positionX real,
    @positionY real,
    @hp int,
    @storedMaxHp int,
    @bossUnlocked bit,
    @Json nvarchar(max) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    -- Pure projection of captured values; caller controls absence and safe/current observation timing.
    SET @Json = (SELECT @mapId AS mapId,
        CONVERT(varchar(32), @positionX, 3) AS x,
        CONVERT(varchar(32), @positionY, 3) AS y,
        @hp AS hp,
        @storedMaxHp AS maxHp,
        @bossUnlocked AS bossUnlocked
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES);
END;
