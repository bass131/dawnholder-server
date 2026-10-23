CREATE OR ALTER PROCEDURE dh.ReadCharacterState
    @boundAccount uniqueidentifier,
    @boundCharacter uniqueidentifier,
    @accountPresent bit OUTPUT,
    @characterPresent bit OUTPUT,
    @progressPresent bit OUTPUT,
    @storedAccount uniqueidentifier OUTPUT,
    @storedClass tinyint OUTPUT,
    @characterVersion binary(8) OUTPUT,
    @progressVersion binary(8) OUTPUT,
    @mapId tinyint OUTPUT,
    @positionX real OUTPUT,
    @positionY real OUTPUT,
    @hp int OUTPUT,
    @storedMaxHp int OUTPUT,
    @bossUnlocked bit OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    -- Initialize every output on each read; preserve Account -> Character -> Progress locked observations.
    SET @accountPresent = 0;
    SET @characterPresent = 0;
    SET @progressPresent = 0;
    SET @storedAccount = NULL;
    SET @storedClass = NULL;
    SET @characterVersion = NULL;
    SET @progressVersion = NULL;
    SET @mapId = NULL;
    SET @positionX = NULL;
    SET @positionY = NULL;
    SET @hp = NULL;
    SET @storedMaxHp = NULL;
    SET @bossUnlocked = NULL;
    -- Account identity is immutable; an update lock here would serialize different characters of one account.
    SELECT @accountPresent = 1 FROM dh.Account WHERE AccountId = @boundAccount;
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
END;
