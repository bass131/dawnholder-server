-- Empty-development-database upgrade only. The installer holds SchemaMigration and table locks
-- before any migration/module mutation; these guards also protect direct execution in a transaction.
IF EXISTS(SELECT 1 FROM dh.Account WITH (TABLOCKX, HOLDLOCK))
    OR EXISTS(SELECT 1 FROM dh.Character WITH (TABLOCKX, HOLDLOCK))
    OR EXISTS(SELECT 1 FROM dh.CharacterProgress WITH (TABLOCKX, HOLDLOCK))
    OR EXISTS(SELECT 1 FROM dh.CharacterAuthority WITH (TABLOCKX, HOLDLOCK))
    OR EXISTS(SELECT 1 FROM dh.CharacterOperation WITH (TABLOCKX, HOLDLOCK))
    THROW 51030, 'Catalog upgrade refuses existing game rows.', 1;

ALTER TABLE dh.Account ADD NormalizedName varchar(16) COLLATE Latin1_General_100_BIN2 NOT NULL;
-- Compile new-column constraints after ADD, within the caller's same migration transaction.
EXEC sys.sp_executesql @stmt = N'
ALTER TABLE dh.Account ADD CONSTRAINT UQ_Account_NormalizedName UNIQUE (NormalizedName);
ALTER TABLE dh.Account ADD CONSTRAINT CK_Account_Name CHECK
    (DATALENGTH(NormalizedName) BETWEEN 3 AND 16 AND NormalizedName NOT LIKE ''%[^a-z0-9_]%'');';
ALTER TABLE dh.Character ADD Slot tinyint NOT NULL,
    Name nvarchar(12) COLLATE Latin1_General_100_BIN2 NOT NULL,
    NameKey nvarchar(12) COLLATE Latin1_General_100_BIN2 NOT NULL;
EXEC sys.sp_executesql @stmt = N'
ALTER TABLE dh.Character ADD CONSTRAINT CK_Character_Slot CHECK (Slot BETWEEN 1 AND 3);
ALTER TABLE dh.Character ADD CONSTRAINT CK_Character_Name CHECK
    (DATALENGTH(Name) BETWEEN 4 AND 24 AND Name NOT LIKE N''%[^가-힣A-Za-z0-9]%'' AND
    DATALENGTH(NameKey) = DATALENGTH(Name) AND NameKey = UPPER(Name) COLLATE Latin1_General_100_BIN2);
ALTER TABLE dh.Character ADD CONSTRAINT UQ_Character_AccountSlot UNIQUE (AccountId, Slot);
ALTER TABLE dh.Character ADD CONSTRAINT UQ_Character_NameKey UNIQUE (NameKey);';

ALTER TABLE dh.CharacterOperation DROP CONSTRAINT FK_CharacterOperation_Authority;
ALTER TABLE dh.CharacterAuthority DROP CONSTRAINT PK_CharacterAuthority, CK_CharacterAuthority_Slot,
    UQ_CharacterAuthority_CharacterId;
ALTER TABLE dh.CharacterAuthority ADD CONSTRAINT PK_CharacterAuthority PRIMARY KEY (CharacterId),
    CONSTRAINT CK_CharacterAuthority_Slot CHECK (SlotId BETWEEN 1 AND 3),
    CONSTRAINT FK_CharacterAuthority_Character FOREIGN KEY (CharacterId) REFERENCES dh.Character(CharacterId);
ALTER TABLE dh.CharacterOperation DROP CONSTRAINT CK_CharacterOperation_Slot;
ALTER TABLE dh.CharacterOperation ADD CharacterId uniqueidentifier NOT NULL;
EXEC sys.sp_executesql @stmt = N'
ALTER TABLE dh.CharacterOperation ADD CONSTRAINT CK_CharacterOperation_Slot CHECK (SlotId BETWEEN 1 AND 3),
    CONSTRAINT FK_CharacterOperation_Authority FOREIGN KEY (CharacterId) REFERENCES dh.CharacterAuthority(CharacterId);';

-- Create receipts also represent refusals before an Account or Character exists. No character FK can own this ledger.
CREATE TABLE dh.CharacterCreateOperation
(
    OperationId uniqueidentifier NOT NULL CONSTRAINT PK_CharacterCreateOperation PRIMARY KEY,
    AccountId uniqueidentifier NOT NULL,
    Payload varbinary(512) NOT NULL,
    ResultCode varchar(32) NOT NULL,
    CharacterId uniqueidentifier NULL,
    RecordedUtc datetime2(3) NOT NULL,
    CONSTRAINT CK_CharacterCreateOperation_Id CHECK (OperationId <> '00000000-0000-0000-0000-000000000000'),
    CONSTRAINT CK_CharacterCreateOperation_Payload CHECK (DATALENGTH(Payload) BETWEEN 1 AND 512),
    CONSTRAINT CK_CharacterCreateOperation_Result CHECK
    (
        (ResultCode = 'Created' AND CharacterId IS NOT NULL)
        OR (ResultCode IN ('SlotsFull', 'SlotOccupied', 'NameTaken', 'CancelledBeforeDispatch') AND CharacterId IS NULL)
    )
);

-- Future economy persistence only: no rows, seed grants or live Inventory/Guild writes are introduced here.
CREATE TABLE dh.CharacterWallet
(
    CharacterId uniqueidentifier NOT NULL CONSTRAINT PK_CharacterWallet PRIMARY KEY,
    Gold int NOT NULL,
    Revision bigint NOT NULL,
    Version rowversion NOT NULL,
    CONSTRAINT FK_CharacterWallet_Character FOREIGN KEY (CharacterId) REFERENCES dh.Character(CharacterId),
    CONSTRAINT CK_CharacterWallet_Gold CHECK (Gold BETWEEN 0 AND 1000000000),
    CONSTRAINT CK_CharacterWallet_Revision CHECK (Revision BETWEEN 0 AND 4294967295)
);
CREATE TABLE dh.CharacterWeapon
(
    CharacterId uniqueidentifier NOT NULL CONSTRAINT PK_CharacterWeapon PRIMARY KEY,
    Level tinyint NOT NULL,
    Revision bigint NOT NULL,
    Version rowversion NOT NULL,
    CONSTRAINT FK_CharacterWeapon_Character FOREIGN KEY (CharacterId) REFERENCES dh.Character(CharacterId),
    CONSTRAINT CK_CharacterWeapon_Level CHECK (Level BETWEEN 0 AND 1),
    CONSTRAINT CK_CharacterWeapon_Revision CHECK (Revision BETWEEN 0 AND 4294967295)
);
CREATE TABLE dh.Guild
(
    GuildId bigint NOT NULL CONSTRAINT PK_Guild PRIMARY KEY,
    Name nvarchar(32) NOT NULL,
    StrongholdLevel tinyint NOT NULL,
    Treasury int NOT NULL,
    Revision bigint NOT NULL,
    Version rowversion NOT NULL,
    CONSTRAINT CK_Guild_Id CHECK (GuildId BETWEEN 1 AND 4294967295),
    CONSTRAINT CK_Guild_Name CHECK (DATALENGTH(Name) BETWEEN 2 AND 64),
    CONSTRAINT CK_Guild_Stronghold CHECK (StrongholdLevel BETWEEN 1 AND 2),
    CONSTRAINT CK_Guild_Treasury CHECK (Treasury BETWEEN 0 AND 1000000000),
    CONSTRAINT CK_Guild_Revision CHECK (Revision BETWEEN 0 AND 4294967295)
);
CREATE TABLE dh.GuildMember
(
    AccountId uniqueidentifier NOT NULL CONSTRAINT PK_GuildMember PRIMARY KEY,
    GuildId bigint NOT NULL,
    JoinedUtc datetime2(3) NOT NULL,
    JoinSeedGranted bit NOT NULL,
    CONSTRAINT FK_GuildMember_Account FOREIGN KEY (AccountId) REFERENCES dh.Account(AccountId),
    CONSTRAINT FK_GuildMember_Guild FOREIGN KEY (GuildId) REFERENCES dh.Guild(GuildId)
);

-- Current catalog module release is reviewed source identity, never a database observation.
INSERT dh.ModuleRelease(Version, ManifestChecksum)
VALUES(5, 'AC6D31EA3C28C21FD01D150F2828BD9AA9FCAF8384FEB9B9B2A171F4E718A571');
