-- Catalog errors: 51001 columns, 51002 indexes, 51003 foreign keys,
-- 51004 baseline CHECKs, 51005 defaults, 51006 schema/tables/triggers, 51007 persistence constraints,
-- 51008 modules, 51009 permissions, 51010 migrations. Exact expectations remain fail-closed.
-- Exact 001 and persistence-v1 catalog contract; runtime behavior requires independent SQL tests.
-- Applied inside the existing migration runner transaction. No dynamic SQL or data repair.
SET NOCOUNT ON;
DECLARE @columns TABLE
(
    TableName sysname,
    ColumnName sysname,
    TypeName sysname,
    Length smallint,
    Scale tinyint,
    IsNullable bit
);
INSERT @columns VALUES
    ('Account', 'NormalizedName', 'varchar', 16, 0, 0),
    ('Character', 'Slot', 'tinyint', 1, 0, 0),
    ('Character', 'Name', 'nvarchar', 24, 0, 0),
    ('Character', 'NameKey', 'nvarchar', 24, 0, 0),
    ('CharacterOperation', 'CharacterId', 'uniqueidentifier', 16, 0, 0),
    ('CharacterCreateOperation', 'OperationId', 'uniqueidentifier', 16, 0, 0),
    ('CharacterCreateOperation', 'AccountId', 'uniqueidentifier', 16, 0, 0),
    ('CharacterCreateOperation', 'Payload', 'varbinary', 512, 0, 0),
    ('CharacterCreateOperation', 'ResultCode', 'varchar', 32, 0, 0),
    ('CharacterCreateOperation', 'CharacterId', 'uniqueidentifier', 16, 0, 1),
    ('CharacterCreateOperation', 'RecordedUtc', 'datetime2', 7, 3, 0),
    ('CharacterWallet', 'CharacterId', 'uniqueidentifier', 16, 0, 0),
    ('CharacterWallet', 'Gold', 'int', 4, 0, 0),
    ('CharacterWallet', 'Revision', 'bigint', 8, 0, 0),
    ('CharacterWallet', 'Version', 'timestamp', 8, 0, 0),
    ('CharacterWeapon', 'CharacterId', 'uniqueidentifier', 16, 0, 0),
    ('CharacterWeapon', 'Level', 'tinyint', 1, 0, 0),
    ('CharacterWeapon', 'Revision', 'bigint', 8, 0, 0),
    ('CharacterWeapon', 'Version', 'timestamp', 8, 0, 0),
    ('Guild', 'GuildId', 'bigint', 8, 0, 0),
    ('Guild', 'Name', 'nvarchar', 64, 0, 0),
    ('Guild', 'StrongholdLevel', 'tinyint', 1, 0, 0),
    ('Guild', 'Treasury', 'int', 4, 0, 0),
    ('Guild', 'Revision', 'bigint', 8, 0, 0),
    ('Guild', 'Version', 'timestamp', 8, 0, 0),
    ('GuildMember', 'AccountId', 'uniqueidentifier', 16, 0, 0),
    ('GuildMember', 'GuildId', 'bigint', 8, 0, 0),
    ('GuildMember', 'JoinedUtc', 'datetime2', 7, 3, 0),
    ('GuildMember', 'JoinSeedGranted', 'bit', 1, 0, 0),
    ('SchemaVersion', 'Version', 'int', 4, 0, 0),
    ('SchemaVersion', 'Name', 'nvarchar', 256, 0, 0),
    ('SchemaVersion', 'Checksum', 'char', 64, 0, 0),
    ('SchemaVersion', 'AppliedUtc', 'datetime2', 7, 3, 0),
    ('Account', 'AccountId', 'uniqueidentifier', 16, 0, 0),
    ('Account', 'CreatedUtc', 'datetime2', 7, 3, 0),
    ('Character', 'CharacterId', 'uniqueidentifier', 16, 0, 0),
    ('Character', 'AccountId', 'uniqueidentifier', 16, 0, 0),
    ('Character', 'Class', 'tinyint', 1, 0, 0),
    ('Character', 'CreatedUtc', 'datetime2', 7, 3, 0),
    ('Character', 'Version', 'timestamp', 8, 0, 0),
    ('CharacterProgress', 'CharacterId', 'uniqueidentifier', 16, 0, 0),
    ('CharacterProgress', 'MapId', 'tinyint', 1, 0, 0),
    ('CharacterProgress', 'PositionX', 'real', 4, 0, 0),
    ('CharacterProgress', 'PositionY', 'real', 4, 0, 0),
    ('CharacterProgress', 'Hp', 'int', 4, 0, 0),
    ('CharacterProgress', 'MaxHp', 'int', 4, 0, 0),
    ('CharacterProgress', 'BossUnlocked', 'bit', 1, 0, 0),
    ('CharacterProgress', 'SavedUtc', 'datetime2', 7, 3, 0),
    ('CharacterProgress', 'Version', 'timestamp', 8, 0, 0),
    ('CharacterAuthority', 'SlotId', 'tinyint', 1, 0, 0),
    ('CharacterAuthority', 'AccountId', 'uniqueidentifier', 16, 0, 0),
    ('CharacterAuthority', 'CharacterId', 'uniqueidentifier', 16, 0, 0),
    ('CharacterAuthority', 'Fence', 'bigint', 8, 0, 0),
    ('CharacterAuthority', 'OwnerKind', 'tinyint', 1, 0, 0),
    ('CharacterAuthority', 'OwnerId', 'uniqueidentifier', 16, 0, 1),
    ('CharacterAuthority', 'LastSequence', 'bigint', 8, 0, 0),
    ('CharacterAuthority', 'ChangedUtc', 'datetime2', 7, 3, 0),
    ('CharacterOperation', 'OperationId', 'uniqueidentifier', 16, 0, 0),
    ('CharacterOperation', 'SlotId', 'tinyint', 1, 0, 0),
    ('CharacterOperation', 'Kind', 'tinyint', 1, 0, 0),
    ('CharacterOperation', 'PayloadVersion', 'tinyint', 1, 0, 0),
    ('CharacterOperation', 'Payload', 'varbinary', 512, 0, 0),
    ('CharacterOperation', 'Outcome', 'tinyint', 1, 0, 0),
    ('CharacterOperation', 'ResultCode', 'smallint', 2, 0, 0),
    ('CharacterOperation', 'ResultSnapshot', 'nvarchar', 4096, 0, 1),
    ('CharacterOperation', 'RecordedUtc', 'datetime2', 7, 3, 0),
    ('ModuleRelease', 'Version', 'int', 4, 0, 0),
    ('ModuleRelease', 'ManifestChecksum', 'char', 64, 0, 0),
    ('ModuleDefinition', 'ObjectName', 'sysname', 256, 0, 0),
    ('ModuleDefinition', 'Kind', 'char', 2, 0, 0),
    ('ModuleDefinition', 'SourceChecksum', 'char', 64, 0, 0),
    ('ModuleDefinition', 'DefinitionBytes', 'int', 4, 0, 0),
    ('ModuleDefinition', 'DefinitionChecksum', 'char', 64, 0, 0);
IF EXISTS (
    SELECT TableName,
        ColumnName,
        TypeName,
        Length,
        Scale,
        IsNullable FROM @columns
    EXCEPT
    SELECT t.name,
        c.name,
        ty.name,
        c.max_length,
        c.scale,
        c.is_nullable FROM sys.tables t
    JOIN sys.columns c ON c.object_id = t.object_id JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    WHERE t.schema_id = SCHEMA_ID('dh')
    AND c.is_identity = 0 AND c.is_computed = 0
    AND c.is_sparse = 0 AND c.generated_always_type = 0
) OR (SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.schema_id = SCHEMA_ID('dh')) <> (SELECT COUNT(*) FROM @columns)
    THROW 51001, 'Column/type/nullability drift detected.', 1;
DECLARE @indexes TABLE
(
    TableName sysname,
    IndexName sysname,
    ColumnName sysname,
    IsUnique bit,
    IsPrimary bit,
    IndexType tinyint
);
INSERT @indexes VALUES
    ('Account', 'UQ_Account_NormalizedName', 'NormalizedName', 1, 0, 2),
    ('Character', 'UQ_Character_NameKey', 'NameKey', 1, 0, 2),
    ('CharacterCreateOperation', 'PK_CharacterCreateOperation', 'OperationId', 1, 1, 1),
    ('CharacterWallet', 'PK_CharacterWallet', 'CharacterId', 1, 1, 1),
    ('CharacterWeapon', 'PK_CharacterWeapon', 'CharacterId', 1, 1, 1),
    ('Guild', 'PK_Guild', 'GuildId', 1, 1, 1),
    ('GuildMember', 'PK_GuildMember', 'AccountId', 1, 1, 1),
    ('SchemaVersion', 'PK_SchemaVersion', 'Version', 1, 1, 1),
    ('SchemaVersion', 'UQ_SchemaVersion_Name', 'Name', 1, 0, 2),
    ('Account', 'PK_Account', 'AccountId', 1, 1, 1),
    ('Character', 'PK_Character', 'CharacterId', 1, 1, 1),
    ('Character', 'IX_Character_AccountId', 'AccountId', 0, 0, 2),
    ('CharacterProgress', 'PK_CharacterProgress', 'CharacterId', 1, 1, 1),
    ('CharacterAuthority', 'PK_CharacterAuthority', 'CharacterId', 1, 1, 1),
    ('CharacterOperation', 'PK_CharacterOperation', 'OperationId', 1, 1, 1),
    ('ModuleRelease', 'PK_ModuleRelease', 'Version', 1, 1, 1),
    ('ModuleDefinition', 'PK_ModuleDefinition', 'ObjectName', 1, 1, 1);
IF EXISTS (
    SELECT * FROM @indexes
    EXCEPT
    SELECT t.name,
        i.name,
        c.name,
        i.is_unique,
        i.is_primary_key,
        i.type FROM sys.tables t
    JOIN sys.indexes i ON i.object_id = t.object_id
    JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE t.schema_id = SCHEMA_ID('dh') AND i.is_disabled = 0 AND i.has_filter = 0 AND i.is_hypothetical = 0
    AND i.ignore_dup_key = 0 AND ic.key_ordinal = 1 AND ic.is_descending_key = 0
    AND (SELECT COUNT(*) FROM sys.index_columns x WHERE x.object_id = i.object_id AND x.index_id = i.index_id) = 1
) OR (SELECT COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
    WHERE t.schema_id = SCHEMA_ID('dh') AND i.index_id > 0) <> (SELECT COUNT(*) + 1 FROM @indexes)
    THROW 51002, 'Primary/unique/FK lookup index drift detected.', 1;
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes i WHERE i.object_id = OBJECT_ID('dh.Character')
    AND i.name = 'UQ_Character_AccountSlot' AND i.is_unique = 1 AND i.type = 2
    AND i.is_disabled = 0 AND i.has_filter = 0 AND i.ignore_dup_key = 0 AND i.is_hypothetical = 0
    AND (SELECT COUNT(*) FROM sys.index_columns c WHERE c.object_id = i.object_id AND c.index_id = i.index_id) = 2
    AND EXISTS(SELECT 1 FROM sys.index_columns c WHERE c.object_id = i.object_id AND c.index_id = i.index_id
        AND c.key_ordinal = 1 AND c.is_descending_key = 0 AND COL_NAME(c.object_id, c.column_id) = 'AccountId')
    AND EXISTS(SELECT 1 FROM sys.index_columns c WHERE c.object_id = i.object_id AND c.index_id = i.index_id
        AND c.key_ordinal = 2 AND c.is_descending_key = 0 AND COL_NAME(c.object_id, c.column_id) = 'Slot'))
    THROW 51002, 'Account slot uniqueness drift detected.', 1;
IF EXISTS(SELECT 1 FROM sys.columns c WHERE
    ((c.object_id = OBJECT_ID('dh.Account') AND c.name = 'NormalizedName') OR
    (c.object_id = OBJECT_ID('dh.Character') AND c.name IN ('Name', 'NameKey')))
    AND c.collation_name <> 'Latin1_General_100_BIN2')
    THROW 51001, 'Catalog name collation drift detected.', 1;
DECLARE @foreignKeys TABLE
(
    Name sysname,
    ParentTable sysname,
    ParentColumn sysname,
    RefTable sysname,
    RefColumn sysname
);
INSERT @foreignKeys VALUES
    ('FK_CharacterAuthority_Character', 'CharacterAuthority', 'CharacterId', 'Character', 'CharacterId'),
    ('FK_CharacterWallet_Character', 'CharacterWallet', 'CharacterId', 'Character', 'CharacterId'),
    ('FK_CharacterWeapon_Character', 'CharacterWeapon', 'CharacterId', 'Character', 'CharacterId'),
    ('FK_GuildMember_Account', 'GuildMember', 'AccountId', 'Account', 'AccountId'),
    ('FK_GuildMember_Guild', 'GuildMember', 'GuildId', 'Guild', 'GuildId'),
    ('FK_Character_Account', 'Character', 'AccountId', 'Account', 'AccountId'),
    ('FK_CharacterProgress_Character', 'CharacterProgress', 'CharacterId', 'Character', 'CharacterId'),
    ('FK_CharacterOperation_Authority', 'CharacterOperation', 'CharacterId', 'CharacterAuthority', 'CharacterId');
IF EXISTS (
    SELECT * FROM @foreignKeys
    EXCEPT
    SELECT fk.name,
        OBJECT_NAME(fk.parent_object_id),
        COL_NAME(fc.parent_object_id, fc.parent_column_id),
        OBJECT_NAME(fk.referenced_object_id), COL_NAME(fc.referenced_object_id, fc.referenced_column_id)
    FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = fk.object_id
    WHERE fk.schema_id = SCHEMA_ID('dh') AND fk.is_disabled = 0 AND fk.is_not_trusted = 0
    AND fk.delete_referential_action = 0 AND fk.update_referential_action = 0
    AND OBJECT_SCHEMA_NAME(fk.referenced_object_id) = 'dh'
) OR (SELECT COUNT(*) FROM sys.foreign_key_columns fc JOIN sys.foreign_keys fk ON fk.object_id = fc.constraint_object_id
    WHERE fk.schema_id = SCHEMA_ID('dh')) <> (SELECT COUNT(*) FROM @foreignKeys)
    THROW 51003, 'Foreign key drift detected.', 1;
-- Compare SQL Server's stored expression text as well as names/ownership.
-- Deliberately fail closed even for a manually rewritten equivalent expression.
DECLARE @checks TABLE
(
    Name sysname,
    TableName sysname,
    ColumnName sysname NULL,
    Definition nvarchar(4000)
);
INSERT @checks VALUES
    ('CK_SchemaVersion_Version', 'SchemaVersion', 'Version', '([Version]>(0))'),
    ('CK_Character_Class', 'Character', 'Class', '([Class]=(1) OR [Class]=(0))'),
    ('CK_CharacterProgress_Map', 'CharacterProgress', 'MapId', '([MapId]=(3) OR [MapId]=(2) OR [MapId]=(1) OR [MapId]=(0))'),
    ('CK_CharacterProgress_Hp', 'CharacterProgress', NULL, '([MaxHp]>(0) AND [Hp]>=(0) AND [Hp]<=[MaxHp])');
IF EXISTS (
    SELECT * FROM @checks
    EXCEPT
    SELECT name,
        OBJECT_NAME(parent_object_id),
        COL_NAME(parent_object_id, parent_column_id),
        definition
    FROM sys.check_constraints WHERE schema_id = SCHEMA_ID('dh')
    AND parent_object_id IN (
        OBJECT_ID('dh.SchemaVersion'), OBJECT_ID('dh.Account'),
        OBJECT_ID('dh.Character'), OBJECT_ID('dh.CharacterProgress'))
    AND is_disabled = 0 AND is_not_trusted = 0 AND is_not_for_replication = 0
) OR (SELECT COUNT(*) FROM sys.check_constraints WHERE schema_id = SCHEMA_ID('dh')
    AND parent_object_id IN (
        OBJECT_ID('dh.SchemaVersion'), OBJECT_ID('dh.Account'),
        OBJECT_ID('dh.Character'), OBJECT_ID('dh.CharacterProgress'))) <> 7
    THROW 51004, 'Check expression, ownership or trust drift detected.', 1;
DECLARE @defaults TABLE
(
    Name sysname,
    TableName sysname,
    ColumnName sysname,
    Definition nvarchar(4000)
);
INSERT @defaults VALUES
    ('DF_SchemaVersion_AppliedUtc', 'SchemaVersion', 'AppliedUtc', '(sysutcdatetime())'),
    ('DF_Account_CreatedUtc', 'Account', 'CreatedUtc', '(sysutcdatetime())'),
    ('DF_Character_CreatedUtc', 'Character', 'CreatedUtc', '(sysutcdatetime())'),
    ('DF_CharacterProgress_BossUnlocked', 'CharacterProgress', 'BossUnlocked', '((0))'),
    ('DF_CharacterProgress_SavedUtc', 'CharacterProgress', 'SavedUtc', '(sysutcdatetime())');
IF EXISTS (
    SELECT * FROM @defaults
    EXCEPT
    SELECT name,
        OBJECT_NAME(parent_object_id),
        COL_NAME(parent_object_id, parent_column_id),
        definition
    FROM sys.default_constraints WHERE schema_id = SCHEMA_ID('dh')
) OR (SELECT COUNT(*) FROM sys.default_constraints WHERE schema_id = SCHEMA_ID('dh')) <> 5
    THROW 51005, 'Default expression or ownership drift detected.', 1;
IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('dh')) <> 13
    OR EXISTS (SELECT name FROM sys.tables WHERE schema_id = SCHEMA_ID('dh')
    EXCEPT SELECT TableName FROM @columns)
    THROW 51006, 'Unexpected dh table catalog.', 1;
IF (SELECT principal_id FROM sys.schemas WHERE name = 'dh') <> DATABASE_PRINCIPAL_ID('dbo')
    OR EXISTS (SELECT 1 FROM sys.objects WHERE schema_id = SCHEMA_ID('dh')
    AND principal_id IS NOT NULL AND principal_id <> DATABASE_PRINCIPAL_ID('dbo'))
    THROW 51006, 'Common dbo ownership chain drift.', 1;
IF EXISTS (SELECT 1 FROM sys.triggers WHERE parent_id IN
    (SELECT object_id FROM sys.tables WHERE schema_id = SCHEMA_ID('dh')))
    THROW 51006, 'Unexpected persisted game/schema trigger.', 1;
-- Preserve grouping/operators/literals; ignore only whitespace and identifier case for new expressions.
DECLARE @persistenceChecks TABLE
(
    Name sysname,
    TableName sysname,
    ColumnName sysname NULL,
    Definition nvarchar(4000)
);
-- Fixed Owner/Outcome expectations use reduced parentheses; remaining grouping and AND-before-OR keep the CHECK logic.
INSERT @persistenceChecks VALUES
    ('CK_Account_Name', 'Account', 'NormalizedName', N'(datalength([NormalizedName])>=(3)ANDdatalength([NormalizedName])<=(16)ANDNOT[NormalizedName]like''%[^a-z0-9_]%'' )'),
    ('CK_Character_Slot', 'Character', 'Slot', N'([Slot]>=(1)AND[Slot]<=(3))'),
    ('CK_Character_Name', 'Character', NULL, N'(datalength([Name])>=(4)ANDdatalength([Name])<=(24)ANDNOT[Name]likeN''%[^가-힣A-Za-z0-9]%''ANDdatalength([NameKey])=datalength([Name])AND[NameKey]=(upper([Name]))COLLATELatin1_General_100_BIN2)'),
    ('CK_CharacterCreateOperation_Id', 'CharacterCreateOperation', 'OperationId', N'([OperationId]<>''00000000-0000-0000-0000-000000000000'')'),
    ('CK_CharacterCreateOperation_Payload', 'CharacterCreateOperation', 'Payload', N'(datalength([Payload])>=(1)ANDdatalength([Payload])<=(512))'),
    ('CK_CharacterCreateOperation_Result', 'CharacterCreateOperation', NULL, N'([ResultCode]=''Created''AND[CharacterId]ISNOTNULLOR([ResultCode]=''CancelledBeforeDispatch''OR[ResultCode]=''NameTaken''OR[ResultCode]=''SlotOccupied''OR[ResultCode]=''SlotsFull'')AND[CharacterId]ISNULL)'),
    ('CK_CharacterWallet_Gold', 'CharacterWallet', 'Gold', N'([Gold]>=(0)AND[Gold]<=(1000000000))'),
    ('CK_CharacterWallet_Revision', 'CharacterWallet', 'Revision', N'([Revision]>=(0)AND[Revision]<=(4294967295.))'),
    ('CK_CharacterWeapon_Level', 'CharacterWeapon', 'Level', N'([Level]>=(0)AND[Level]<=(1))'),
    ('CK_CharacterWeapon_Revision', 'CharacterWeapon', 'Revision', N'([Revision]>=(0)AND[Revision]<=(4294967295.))'),
    ('CK_Guild_Id', 'Guild', 'GuildId', N'([GuildId]>=(1)AND[GuildId]<=(4294967295.))'),
    ('CK_Guild_Name', 'Guild', 'Name', N'(datalength([Name])>=(2)ANDdatalength([Name])<=(64))'),
    ('CK_Guild_Stronghold', 'Guild', 'StrongholdLevel', N'([StrongholdLevel]>=(1)AND[StrongholdLevel]<=(2))'),
    ('CK_Guild_Treasury', 'Guild', 'Treasury', N'([Treasury]>=(0)AND[Treasury]<=(1000000000))'),
    ('CK_Guild_Revision', 'Guild', 'Revision', N'([Revision]>=(0)AND[Revision]<=(4294967295.))'),
    ('CK_CharacterAuthority_Slot', 'CharacterAuthority', 'SlotId', N'([SlotId]>=(1)AND[SlotId]<=(3))'),
    ('CK_CharacterAuthority_Account', 'CharacterAuthority', 'AccountId', N'([AccountId]<>''00000000-0000-0000-0000-000000000000'')'),
    ('CK_CharacterAuthority_Character', 'CharacterAuthority', 'CharacterId', N'([CharacterId]<>''00000000-0000-0000-0000-000000000000'')'),
    ('CK_CharacterAuthority_Fence', 'CharacterAuthority', 'Fence', N'([Fence]>=(0))'),
    ('CK_CharacterAuthority_Sequence', 'CharacterAuthority', 'LastSequence', N'([LastSequence]>=(0))'),
    ('CK_CharacterAuthority_Owner', 'CharacterAuthority', NULL, N'([OWNERKIND]=(0)AND[OWNERID]ISNULLAND[LASTSEQUENCE]=(0)OR([OWNERKIND]=(1)OR[OWNERKIND]=(2))AND[OWNERID]ISNOTNULLAND[OWNERID]<>''00000000-0000-0000-0000-000000000000''AND[FENCE]>(0))'),
    ('CK_CharacterOperation_Id', 'CharacterOperation', 'OperationId', N'([OperationId]<>''00000000-0000-0000-0000-000000000000'')'),
    ('CK_CharacterOperation_Slot', 'CharacterOperation', 'SlotId', N'([SlotId]>=(1)AND[SlotId]<=(3))'),
    ('CK_CharacterOperation_Kind', 'CharacterOperation', 'Kind', N'([Kind]=(1)OR[Kind]=(2)OR[Kind]=(3)OR[Kind]=(4)OR[Kind]=(5))'),
    ('CK_CharacterOperation_PayloadVersion', 'CharacterOperation', 'PayloadVersion', N'([PayloadVersion]=(1))'),
    ('CK_CharacterOperation_Payload', 'CharacterOperation', 'Payload', N'(datalength([Payload])>=(1)ANDdatalength([Payload])<=(512))'),
    ('CK_CharacterOperation_Outcome', 'CharacterOperation', NULL, N'([OUTCOME]=(1)AND[RESULTSNAPSHOT]ISNOTNULLAND([KIND]=(1)AND[RESULTCODE]=(100)OR[KIND]=(2)AND([RESULTCODE]=(101)OR[RESULTCODE]=(102))OR([KIND]=(3)OR[KIND]=(5))AND[RESULTCODE]=(103)OR[KIND]=(4)AND([RESULTCODE]=(104)OR[RESULTCODE]=(105)))OR[OUTCOME]=(2)AND[RESULTSNAPSHOT]ISNULLAND([RESULTCODE]=(200)OR[RESULTCODE]=(201)OR[RESULTCODE]=(203)OR[RESULTCODE]=(205)OR[RESULTCODE]=(206)OR[RESULTCODE]=(207)OR([KIND]=(1)OR[KIND]=(2))AND[RESULTCODE]=(204)OR[KIND]=(2)AND[RESULTCODE]=(202)))'),
    ('CK_CharacterOperation_Json', 'CharacterOperation', 'ResultSnapshot', N'([ResultSnapshot]ISNULLORisjson([ResultSnapshot])=(1))');
IF EXISTS (
    SELECT Name,
        TableName,
        ColumnName,
        UPPER(REPLACE(Definition, N' ', N'')) COLLATE Latin1_General_100_BIN2 FROM @persistenceChecks
    EXCEPT
    SELECT name,
        OBJECT_NAME(parent_object_id),
        COL_NAME(parent_object_id, parent_column_id),
        UPPER(REPLACE(REPLACE(REPLACE(REPLACE(definition, N' ', N''), NCHAR(9), N''), NCHAR(10), N''), NCHAR(13), N''))
    COLLATE Latin1_General_100_BIN2
    FROM sys.check_constraints WHERE schema_id = SCHEMA_ID('dh')
    AND is_disabled = 0 AND is_not_trusted = 0 AND is_not_for_replication = 0
) OR (SELECT COUNT(*) FROM sys.check_constraints WHERE schema_id = SCHEMA_ID('dh')) <> (SELECT COUNT(*) + 4 FROM @persistenceChecks)
    THROW 51007, 'Persistence CHECK expression, ownership or trust drift.', 1;
-- SHA-256 of the exact LF-normalized UTF-16 module source, including signature and defaults.
-- Reviewed current source expectations; database observations never become the expected values.
DECLARE @modules TABLE
(
    Name sysname,
    Type char(2),
    DefinitionBytes int,
    DefinitionHash varbinary(32),
    SourceChecksum char(64)
);
INSERT @modules VALUES
    ('AccountIdentity', 'FN', 1944, 0xBA3135B3D5232F8BA1A4B746207C7AB06B01ABB2A40A38E2EF015F841E256799,
        '3E1887366CF940F6C4EDAD6AAE638C2B8CABE87C710F7A2EB3FB757AD3AB9ABE'),
    ('PersistencePayloadV1', 'FN', 9340, 0xA41AFD7B1BA8CD22899EF55C1E18809A2530111D87734E94C519996E1041ED6D,
        'CDDAC82BCF0E0CEDA6671D892E8E021294FCE27A658A5A7C7D739052A899F75C'),
    ('LockAndReadAuthority', 'P', 3860, 0xD24D9E52CB970E40FCC658AA25314F3C4B169E32FE68A926936734CFA6FD78DB,
        'CB9E73472EE83CAEFB7E8C04D4A2AA57FC97B67954FA7E1EFCC30B637BA655E4'),
    ('AssertPersistenceContract', 'P', 1696, 0x7D624C534B5565E0AF268525DE5715DB509019FCF48FA91D84C4675C1F20A4EB,
        '0212DDADC10AD6F1E18711D447ACF61870B8EA9A72F7591D82F90D5FA755F0D7'),
    ('ReadOperationReceipt', 'P', 3992, 0x7300B60508A1B0632D76AE8BF24423E5F667FA26DE304BA62DA3A43161DA6617,
        'B22F4AA84C2F90C93A7EAC47E0027B73F18E05AF9037E75C47F03C369574926D'),
    ('ReadCharacterState', 'P', 3582, 0x7989DF09A8974AC5064B2747DAE60E579E3E7EA42FCAAAC11FEE7FAD2EC6C3FF,
        'FF5B33284B5D70F28FDC5181604AED9379483B612E40D4320D031DACC275FECD'),
    ('SerializeProgress', 'P', 1252, 0x676FBD88E4D5C31B5FB43756500F29A9AA919DBD1C48DEEFDC76A52D44B5104E,
        'DF7C1012C9F34299E52387527013BFA37D7435B6F975CE0D4F9F364710723FB7'),
    ('SerializePersistenceSnapshot', 'P', 3604, 0x89E7B6403D6E374588ED34862D3ACCAB21A7E051800A6481C99281EBB5A334C8,
        '01B80E130153068ED5A94520030D4738664AA65A23998504E0D17A20E57EA1EE'),
    ('RecordOperationReceipt', 'P', 1870, 0x5650406F63E517FD30363F31D9C79906B4E666FE1D8F380C6610F97F6A241F7A,
        '785F4D5667114DAD07AD7F05116EC9981D47252BE85EDDE303F3F2111A33DEB3'),
    ('EmitPersistenceResult', 'P', 4382, 0x64610AD460257942BF1BB04977F1D1B2FBE5360BB54508032FE0116339D3F6CE,
        'A363EF895950A116914D68CA01459B5668546183AC0A1241F7951671B17F9FFB'),
    ('ReadAdmission', 'P', 12600, 0x661076CE69A2E046DB08D36F6683354DEC49A513E1A636B2CECBF5333D27C913,
        'D9A8DB1FE2CD7E18BD1369410CBBFBBEE975C827FA7B3698FF3B64EE717FA026'),
    ('AcquireAndLoad', 'P', 29200, 0x025B85942466177E02BE216C3123DDE4ADAEB3B0D2F2E173EFF44A2148E3B99F,
        'F0175D4581C503714F6D2BF3C4AD0FAD4444E6288D76AB79F1ED6541C58A95F4'),
    ('WriteSafeCheckpoint', 'P', 33452, 0x9D2A0F166D5695CEE143790D437897989E47937BAA8345B4F45CF42DA5C41C20,
        'A4100492A28935952DDAC7CECA35BCE53E6D75721613782FC1E0D9E6224A6444'),
    ('ReleaseRuntime', 'P', 25764, 0xD79E38EFB235C29248F34450343966D9C6B3C5A680CAB0FED47FE9BBE595A71E,
        '31289D821B9ED346314F70DE4453121D0106D025C55FEF4A4AF9EC1827819A2D'),
    ('ResolveRuntimeOperation', 'P', 20326, 0xBF6F599266026EA1352E62504044D2CD8B57CF8588A5BF463C7346D76F5754A5,
        '595773C5A12A9430A597B3821325239B849BFEC2AA9049706EB75CB0FDBE6C5F'),
    ('InspectRecovery', 'P', 16096, 0x7CD7C090CB137D149BC29365B3AEE0F07BF9D164BFA0CC1B8E6703FB57C9B2F8,
        '5F4065E641C85E0D4B20CEA37F8553754C9A2DE8946DA7E53D224E8B569A89C2'),
    ('RecoverAndLoad', 'P', 26644, 0x01E2E6F7C841C6EFBC0C050F0B1DAB691834A79737290F3FF61C3C25B2103C77,
        '3C23CA48C1D8E7F3F7FC876C22A9AB7498B10E8E689B1AF4EE63AFDC07C9E966'),
    ('ReleaseRecovery', 'P', 25774, 0xD5F162FF9B8120BE9B061CE977947459186246B486A7490CD5856BB6A587692E,
        'AA09B609E65F997BA8721A7911BB6FE9E2054F0ABC5501A6FA9145F5059364B5'),
    ('ResolveRecoveryOperation', 'P', 20224, 0xCCC26CCFAA3B9FBAEB8F27592BA3630B74EB7BC93B561E0D875152421B9039A7,
        '506B682B008A2E4BA6332FF091FD5BBB78AF0A77B54A8662FA1F0AAA829D85F7'),
    ('CharacterCatalog', 'P', 21438, 0x9738B24C904B437EDB97D569599143E0A0F78B283612962CEC564491B4256EC6,
        '4F1CD5BFFDB6F90BD84B8369B789A26F65C1D31DD98D19B80BE95BE2C1B3D198');
-- Binary equality keeps object type codes exact when the catalog and database type collations differ.
IF EXISTS (
    SELECT Name,
        Type COLLATE Latin1_General_100_BIN2,
        DefinitionBytes,
        DefinitionHash FROM @modules
    EXCEPT
    SELECT o.name,
        o.type COLLATE Latin1_General_100_BIN2,
        DATALENGTH(REPLACE(sm.definition, NCHAR(13), N'')),
        HASHBYTES('SHA2_256', REPLACE(sm.definition, NCHAR(13), N''))
    FROM sys.objects o JOIN sys.sql_modules sm ON sm.object_id = o.object_id
    WHERE o.schema_id = SCHEMA_ID('dh') AND sm.execute_as_principal_id IS NULL
    AND sm.uses_ansi_nulls = 1 AND sm.uses_quoted_identifier = 1
    AND ((o.type = 'FN' AND sm.is_schema_bound = 1) OR o.type = 'P')
) OR (SELECT COUNT(*) FROM sys.objects WHERE schema_id = SCHEMA_ID('dh')
    AND type IN ('P', 'FN', 'IF', 'TF', 'FS', 'FT', 'PC')) <> 20
    THROW 51008, 'Persistence module/signature/body drift.', 1;
-- Current registration must contain reviewed expectations, not hashes adopted from sys.sql_modules.
IF EXISTS (
    SELECT N'dh.' + Name, Type, SourceChecksum COLLATE Latin1_General_100_BIN2,
        DefinitionBytes, CONVERT(char(64), DefinitionHash, 2) COLLATE Latin1_General_100_BIN2 FROM @modules
    EXCEPT
    SELECT ObjectName, Kind, SourceChecksum COLLATE Latin1_General_100_BIN2,
        DefinitionBytes, DefinitionChecksum COLLATE Latin1_General_100_BIN2 FROM dh.ModuleDefinition
) OR (SELECT COUNT(*) FROM dh.ModuleDefinition) <> 20
    THROW 51008, 'Reviewed module registration drift.', 1;
DECLARE @grants TABLE
(
    RoleName sysname,
    ProcedureName sysname
);
INSERT @grants VALUES
    ('dh_runtime', 'CharacterCatalog'),
    ('dh_runtime', 'ReadAdmission'),
    ('dh_runtime', 'AcquireAndLoad'),
    ('dh_runtime', 'WriteSafeCheckpoint'),
    ('dh_runtime', 'ReleaseRuntime'),
    ('dh_runtime', 'ResolveRuntimeOperation'),
    ('dh_recovery', 'InspectRecovery'),
    ('dh_recovery', 'RecoverAndLoad'),
    ('dh_recovery', 'ReleaseRecovery'),
    ('dh_recovery', 'ResolveRecoveryOperation');
IF (SELECT COUNT(*) FROM sys.database_principals WHERE name IN ('dh_runtime', 'dh_recovery') AND type = 'R'
    AND is_fixed_role = 0 AND owning_principal_id = DATABASE_PRINCIPAL_ID('dbo')) <> 2
    THROW 51009, 'Persistence role/owner drift.', 1;
IF EXISTS (
    SELECT RoleName,
        ProcedureName FROM @grants
    EXCEPT
    SELECT p.name,
        o.name FROM sys.database_permissions dp
    JOIN sys.database_principals p ON p.principal_id = dp.grantee_principal_id
    JOIN sys.objects o ON o.object_id = dp.major_id
    WHERE dp.class = 1 AND dp.minor_id = 0 AND dp.permission_name = 'EXECUTE' AND dp.state = 'G'
    AND o.schema_id = SCHEMA_ID('dh') AND o.type = 'P'
) OR (SELECT COUNT(*) FROM sys.database_permissions
    WHERE grantee_principal_id IN (DATABASE_PRINCIPAL_ID('dh_runtime'), DATABASE_PRINCIPAL_ID('dh_recovery'))) <> 10
    THROW 51009, 'Individual execute-only grant drift.', 1;
-- A role nested into any other role could inherit direct DML or cross-role execution.
IF EXISTS (SELECT 1 FROM sys.database_role_members
    WHERE member_principal_id IN (DATABASE_PRINCIPAL_ID('dh_runtime'), DATABASE_PRINCIPAL_ID('dh_recovery')))
    THROW 51009, 'Unexpected persistence role inheritance.', 1;
IF EXISTS (
    SELECT 1 FROM sys.database_role_members assigned
    JOIN sys.database_role_members other ON other.member_principal_id = assigned.member_principal_id
    AND other.role_principal_id <> assigned.role_principal_id
    WHERE assigned.role_principal_id IN (DATABASE_PRINCIPAL_ID('dh_runtime'), DATABASE_PRINCIPAL_ID('dh_recovery'))
)
    THROW 51009, 'Persistence principal has another explicit database role.', 1;
-- Reject legacy direct writers and broad database/schema permissions, including public grants.
IF EXISTS (
    SELECT 1 FROM sys.database_permissions dp
    WHERE dp.state IN ('G', 'W')
    AND dp.grantee_principal_id <> DATABASE_PRINCIPAL_ID('dbo')
    AND (
        (dp.class = 0 AND dp.permission_name IN ('CONTROL', 'ALTER', 'ALTER ANY SCHEMA', 'ALTER ANY ROLE',
                'CREATE TABLE', 'CREATE PROCEDURE', 'CREATE FUNCTION',
                'EXECUTE', 'SELECT', 'INSERT', 'UPDATE', 'DELETE'))
        OR (dp.class = 3 AND dp.major_id = SCHEMA_ID('dh'))
        OR (dp.class = 1 AND dp.major_id IN (SELECT object_id FROM sys.tables WHERE schema_id = SCHEMA_ID('dh'))
            AND (dp.permission_name IN ('INSERT', 'UPDATE', 'DELETE', 'CONTROL', 'ALTER', 'TAKE OWNERSHIP')
                OR (dp.permission_name = 'SELECT' AND dp.grantee_principal_id IN
                    (DATABASE_PRINCIPAL_ID('public'), DATABASE_PRINCIPAL_ID('dh_runtime'),
                        DATABASE_PRINCIPAL_ID('dh_recovery')))))
        OR (dp.class = 1 AND dp.major_id IN (
            SELECT object_id FROM sys.objects WHERE schema_id = SCHEMA_ID('dh') AND type IN ('P', 'FN'))
            AND NOT EXISTS (SELECT 1 FROM @grants g WHERE DATABASE_PRINCIPAL_ID(g.RoleName) = dp.grantee_principal_id
                AND OBJECT_ID(N'dh.' + g.ProcedureName) = dp.major_id
                AND dp.permission_name = 'EXECUTE' AND dp.state = 'G' AND dp.minor_id = 0))
    )
)
    THROW 51009, 'Unnecessary broad grant or direct writer permission.', 1;
-- Runner still verifies every on-disk migration checksum before this catalog check.
DECLARE @migrations TABLE
(
    Version int,
    Name nvarchar(128),
    Checksum char(64)
);
INSERT @migrations VALUES
    (1, '001_initial.sql', 'F28502BB1A8D683A66F596F15BA9EEC779E5110BAD8ADDAEE03E393F26E54FCC'),
    (2, '002_persistence_metadata.sql', '867F8CC52350029EE15E753EC9134DDB7F7FAD91D5A9383D0F2209EAAD2C3599'),
    (3, '003_module_metadata.sql', 'A4F271547EC0E2E700413F51D8281A361D8654D383996E73F9476E4540A40F9B'),
    (4, '004_module_release.sql', '2B9480C378AFBFFD7FA09E45BC2C416977D70B3BFD5436D5DDD3A1C24CB207B0'),
    (5, '005_character_catalog.sql', '49C22F5E8DFA77C72835B0E810334455EB15B38C5434FB0E039B086FEFD60114');
IF EXISTS (SELECT * FROM @migrations EXCEPT SELECT Version, Name, Checksum FROM dh.SchemaVersion)
    OR (SELECT COUNT(*) FROM dh.SchemaVersion) <> 5
    THROW 51010, 'Migration name/checksum/version catalog drift.', 1;
-- The release declaration is checked after the runner has recorded its schema migration row.
IF (SELECT COUNT(*) FROM dh.ModuleRelease) <> 2
    OR NOT EXISTS (SELECT 1 FROM dh.ModuleRelease r JOIN dh.SchemaVersion s ON s.Version = r.Version
        WHERE r.Version = 5 AND r.ManifestChecksum COLLATE Latin1_General_100_BIN2 =
            'AC6D31EA3C28C21FD01D150F2828BD9AA9FCAF8384FEB9B9B2A171F4E718A571')
    THROW 51010, 'Module release declaration drift.', 1;
