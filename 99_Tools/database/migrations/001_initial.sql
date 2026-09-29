-- Applied inside the runner's transaction + exclusive migration application lock.
-- No runtime EntityId, credentials, party state, or inferred economy tables.
CREATE TABLE dh.Account
(
    AccountId uniqueidentifier NOT NULL CONSTRAINT PK_Account PRIMARY KEY,
    CreatedUtc datetime2(3) NOT NULL CONSTRAINT DF_Account_CreatedUtc DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dh.Character
(
    CharacterId uniqueidentifier NOT NULL CONSTRAINT PK_Character PRIMARY KEY,
    AccountId uniqueidentifier NOT NULL,
    Class tinyint NOT NULL,
    CreatedUtc datetime2(3) NOT NULL CONSTRAINT DF_Character_CreatedUtc DEFAULT SYSUTCDATETIME(),
    Version rowversion NOT NULL,
    CONSTRAINT FK_Character_Account FOREIGN KEY (AccountId) REFERENCES dh.Account(AccountId),
    CONSTRAINT CK_Character_Class CHECK (Class IN (0, 1))
);
CREATE INDEX IX_Character_AccountId ON dh.Character(AccountId);

CREATE TABLE dh.CharacterProgress
(
    CharacterId uniqueidentifier NOT NULL CONSTRAINT PK_CharacterProgress PRIMARY KEY,
    MapId tinyint NOT NULL,
    PositionX real NOT NULL,
    PositionY real NOT NULL,
    Hp int NOT NULL,
    MaxHp int NOT NULL,
    BossUnlocked bit NOT NULL CONSTRAINT DF_CharacterProgress_BossUnlocked DEFAULT 0,
    SavedUtc datetime2(3) NOT NULL CONSTRAINT DF_CharacterProgress_SavedUtc DEFAULT SYSUTCDATETIME(),
    Version rowversion NOT NULL,
    CONSTRAINT FK_CharacterProgress_Character FOREIGN KEY (CharacterId) REFERENCES dh.Character(CharacterId),
    CONSTRAINT CK_CharacterProgress_Map CHECK (MapId IN (0, 1, 2, 3)),
    CONSTRAINT CK_CharacterProgress_Hp CHECK (MaxHp > 0 AND Hp >= 0 AND Hp <= MaxHp)
);
