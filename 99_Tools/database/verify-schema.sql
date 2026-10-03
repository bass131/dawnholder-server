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
    WHERE t.schema_id = SCHEMA_ID('dh') AND c.is_identity = 0 AND c.is_computed = 0 AND c.is_sparse = 0 AND c.generated_always_type = 0
) OR (SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.schema_id = SCHEMA_ID('dh')) <> 44
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
    ('SchemaVersion', 'PK_SchemaVersion', 'Version', 1, 1, 1),
    ('SchemaVersion', 'UQ_SchemaVersion_Name', 'Name', 1, 0, 2),
    ('Account', 'PK_Account', 'AccountId', 1, 1, 1),
    ('Character', 'PK_Character', 'CharacterId', 1, 1, 1),
    ('Character', 'IX_Character_AccountId', 'AccountId', 0, 0, 2),
    ('CharacterProgress', 'PK_CharacterProgress', 'CharacterId', 1, 1, 1),
    ('CharacterAuthority', 'PK_CharacterAuthority', 'SlotId', 1, 1, 1),
    ('CharacterAuthority', 'UQ_CharacterAuthority_CharacterId', 'CharacterId', 1, 0, 2),
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
    JOIN sys.indexes i ON i.object_id = t.object_id JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE t.schema_id = SCHEMA_ID('dh') AND i.is_disabled = 0 AND i.has_filter = 0 AND i.is_hypothetical = 0
    AND i.ignore_dup_key = 0 AND ic.key_ordinal = 1 AND ic.is_descending_key = 0
    AND (SELECT COUNT(*) FROM sys.index_columns x WHERE x.object_id = i.object_id AND x.index_id = i.index_id) = 1
) OR (SELECT COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
    WHERE t.schema_id = SCHEMA_ID('dh') AND i.index_id > 0) <> 11
    THROW 51002, 'Primary/unique/FK lookup index drift detected.', 1;
DECLARE @foreignKeys TABLE
(
    Name sysname,
    ParentTable sysname,
    ParentColumn sysname,
    RefTable sysname,
    RefColumn sysname
);
INSERT @foreignKeys VALUES
    ('FK_Character_Account', 'Character', 'AccountId', 'Account', 'AccountId'),
    ('FK_CharacterProgress_Character', 'CharacterProgress', 'CharacterId', 'Character', 'CharacterId'),
    ('FK_CharacterOperation_Authority', 'CharacterOperation', 'SlotId', 'CharacterAuthority', 'SlotId');
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
    WHERE fk.schema_id = SCHEMA_ID('dh')) <> 3
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
    AND parent_object_id IN (OBJECT_ID('dh.SchemaVersion'), OBJECT_ID('dh.Account'), OBJECT_ID('dh.Character'), OBJECT_ID('dh.CharacterProgress'))
    AND is_disabled = 0 AND is_not_trusted = 0 AND is_not_for_replication = 0
) OR (SELECT COUNT(*) FROM sys.check_constraints WHERE schema_id = SCHEMA_ID('dh')
    AND parent_object_id IN (OBJECT_ID('dh.SchemaVersion'), OBJECT_ID('dh.Account'), OBJECT_ID('dh.Character'), OBJECT_ID('dh.CharacterProgress'))) <> 4
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
IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('dh')) <> 8
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
INSERT @persistenceChecks VALUES
    ('CK_CharacterAuthority_Slot', 'CharacterAuthority', 'SlotId', N'([SlotId]=(1))'),
    ('CK_CharacterAuthority_Account', 'CharacterAuthority', 'AccountId', N'([AccountId]<>''00000000-0000-0000-0000-000000000000'')'),
    ('CK_CharacterAuthority_Character', 'CharacterAuthority', 'CharacterId', N'([CharacterId]<>''00000000-0000-0000-0000-000000000000'')'),
    ('CK_CharacterAuthority_Fence', 'CharacterAuthority', 'Fence', N'([Fence]>=(0))'),
    ('CK_CharacterAuthority_Sequence', 'CharacterAuthority', 'LastSequence', N'([LastSequence]>=(0))'),
    ('CK_CharacterAuthority_Owner', 'CharacterAuthority', NULL, N'(([OwnerKind]=(0)AND[OwnerId]ISNULLAND[LastSequence]=(0))OR(([OwnerKind]=(1)OR[OwnerKind]=(2))AND[OwnerId]ISNOTNULLAND[OwnerId]<>''00000000-0000-0000-0000-000000000000''AND[Fence]>(0)))'),
    ('CK_CharacterOperation_Id', 'CharacterOperation', 'OperationId', N'([OperationId]<>''00000000-0000-0000-0000-000000000000'')'),
    ('CK_CharacterOperation_Slot', 'CharacterOperation', 'SlotId', N'([SlotId]=(1))'),
    ('CK_CharacterOperation_Kind', 'CharacterOperation', 'Kind', N'([Kind]=(1)OR[Kind]=(2)OR[Kind]=(3)OR[Kind]=(4)OR[Kind]=(5))'),
    ('CK_CharacterOperation_PayloadVersion', 'CharacterOperation', 'PayloadVersion', N'([PayloadVersion]=(1))'),
    ('CK_CharacterOperation_Payload', 'CharacterOperation', 'Payload', N'(datalength([Payload])>=(1)ANDdatalength([Payload])<=(512))'),
    ('CK_CharacterOperation_Outcome', 'CharacterOperation', NULL, N'(([Outcome]=(1)AND[ResultSnapshot]ISNOTNULLAND(([Kind]=(1)AND[ResultCode]=(100))OR([Kind]=(2)AND([ResultCode]=(101)OR[ResultCode]=(102)))OR(([Kind]=(3)OR[Kind]=(5))AND[ResultCode]=(103))OR([Kind]=(4)AND([ResultCode]=(104)OR[ResultCode]=(105)))))OR([Outcome]=(2)AND[ResultSnapshot]ISNULLAND([ResultCode]=(200)OR[ResultCode]=(201)OR[ResultCode]=(203)OR[ResultCode]=(205)OR[ResultCode]=(206)OR[ResultCode]=(207)OR(([Kind]=(1)OR[Kind]=(2))AND[ResultCode]=(204))OR([Kind]=(2)AND[ResultCode]=(202)))))'),
    ('CK_CharacterOperation_Json', 'CharacterOperation', 'ResultSnapshot', N'([ResultSnapshot]ISNULLORisjson([ResultSnapshot])=(1))');
IF EXISTS (
    SELECT Name,
        TableName,
        ColumnName,
        UPPER(Definition) COLLATE Latin1_General_100_BIN2 FROM @persistenceChecks
    EXCEPT
    SELECT name,
        OBJECT_NAME(parent_object_id),
        COL_NAME(parent_object_id, parent_column_id),
        UPPER(REPLACE(REPLACE(REPLACE(REPLACE(definition, N' ', N''), NCHAR(9), N''), NCHAR(10), N''), NCHAR(13), N''))
    COLLATE Latin1_General_100_BIN2
    FROM sys.check_constraints WHERE parent_object_id IN
    (OBJECT_ID('dh.CharacterAuthority'), OBJECT_ID('dh.CharacterOperation'))
    AND is_disabled = 0 AND is_not_trusted = 0 AND is_not_for_replication = 0
) OR (SELECT COUNT(*) FROM sys.check_constraints WHERE schema_id = SCHEMA_ID('dh')) <> 17
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
    ('PersistencePayloadV1', 'FN', 9258, 0xB7D79454BFEFB461791E987BB713B55C7766BF5CBBA066E2673E189DD4C2C9E4,
        'D074787D4B18C9E1AF95A0F8EE1E61B4FFFF97CE21DBAD3A8F6A48695A107926'),
    ('LockAndReadAuthority', 'P', 2604, 0xE0C3E452CBCCB84AB19C2FA8CAAAAE3C824C4806486FAF89D253723B471F0427,
        'FCC77C1B391B06346E5B0C1B617188EDB87B89172CF565AB9ABB30FBF60C6F94'),
    ('AssertPersistenceContract', 'P', 1524, 0xE8504643281A1DE27D4A2081B7A64E5920C809CFBC27F411556E313E4620B748,
        'E25B3C75A2D96398AA0DDDCE2C80A5D8B0D6828F7C5499A3ECB0A151036E4B00'),
    ('ReadOperationReceipt', 'P', 2800, 0x48DBC12D0E943469C3B94FDA86D49740C0DD28D288E4F7A87FF69769FD454972,
        '56DC8B6C845A991162E4EF60B48E14C33924432388B6579B166A0FDC81696193'),
    ('ReadCharacterState', 'P', 3424, 0x1EEA5821ADF69BB8D368693E6E9A80155EA05554CE45555C4C602FF00E994258,
        '8A095DC941685F616EF01D1B3948EB6C516A18F121A815CC5A3F21C6D4AD31DE'),
    ('SerializeProgress', 'P', 1266, 0xCC8971A7F867F51AD5F064C7CC9BC0DF6A74962099AC3E0245D92BD18802CCB2,
        'DF7C1012C9F34299E52387527013BFA37D7435B6F975CE0D4F9F364710723FB7'),
    ('SerializePersistenceSnapshot', 'P', 3572, 0xDB35102EFA7B5AA097B5A0C33810E004514BE6578C3860778ED754E765CFD130,
        '134F5AA1ADC199FCBBFFE9AAE85B399EC23CC33259DCBC31030061B5C4366B0A'),
    ('RecordOperationReceipt', 'P', 1682, 0x3280839F42578F3C28E2A7E2C0FB3F1AC9DE25BA2A898C4DAF070D0272EC10F6,
        'AAFB8C190266AAED7F55750A7BB4CDE179013DDB03DFAE6F59C036AC45648B25'),
    ('EmitPersistenceResult', 'P', 4350, 0x0E5D8E6941FFBD9675655BEBB7E6737470D7882177224B76908B31B269002C1D,
        'ABB087091CD097FA59241362AA97A4B5D6A7427E92E98253FD9AF52F9450F691'),
    ('ReadAdmission', 'P', 12800, 0x1659E51318BD2B4035A97BF8713D0718F11665A9D720559E23CEDBD779C0200E,
        '58093893445077FFBFE41EA9AB27E632A9772AD409D50337CD216E25F0074E53'),
    ('AcquireAndLoad', 'P', 30880, 0xCE4519FB18B213ADD31F10B37B31EAE395A5EA9C8A4666462F1D6DB6C00F86FE,
        '3BB8D8A354D1B1D75828E07B11F65DCDE86F2363873212913D04EAEADE49A87B'),
    ('WriteSafeCheckpoint', 'P', 32580, 0x42BBE81F91B9F965F5D1C16583146F04A1FB0E5929B413DAE892F8388B843558,
        'C8EFC4AA3DCE79E6E3FEFB20B70E97FA643DAF6D127DDE817FF5AA54C361C5E4'),
    ('ReleaseRuntime', 'P', 24790, 0x68F7D16D6F1F0007F4EF0AD077956476A990A223E3462182350A1D01332AB70A,
        '04FA0C10B81D7493C641E6FEAA10366AD3BFF054FF1A7E54ED5FBFFB68B01643'),
    ('ResolveRuntimeOperation', 'P', 19770, 0x35154902610E8BFC0D438F8446AAAEAF2F28301334D99B083FAF6040B2DF0993,
        '2B95D1BB56BF98FF11C78336D87CC5E680952A6F487BB3FE7DE3887380D6D064'),
    ('InspectRecovery', 'P', 15870, 0x5C42A1292158B97608F5DE5FA37AE71C02485C9B59AD24AAB0F4684763FFB5F7,
        '88D1EA2A9B3F8B6362858972EAA2E7F9C385D7C5C5CA3E37C77B7A94AFF05CB8'),
    ('RecoverAndLoad', 'P', 25828, 0x64180C75DF88AD6D9DFF67EC6E3D2A920EAE099863866977FB024494CF05F31F,
        'A597DCE2C0B2886A3445152F10E7DA79352F9BE8EC4A9BB1A8E3B7AA3910561B'),
    ('ReleaseRecovery', 'P', 24800, 0x2D69E5AB560F33F7B55BDC7C1DC325839364E03C9A6EE158E21BFEFCE0C3FC8B,
        '3136D78B8EF3F271119768255F2CC5683BEEE0504CC85B40156FEDFCED41DD2C'),
    ('ResolveRecoveryOperation', 'P', 19668, 0x3B19F6178598CA47F720687BB37ABAEF01E2166AA76CEF45CF848DFFC3132AC8,
        '9C4CB515AF1020EB8C37DB23CEE936D9A37F9100E3B6EFBDB1A8EBA9C7CBC2FC');
IF EXISTS (
    SELECT Name,
        Type,
        DefinitionBytes,
        DefinitionHash FROM @modules
    EXCEPT
    SELECT o.name,
        o.type,
        DATALENGTH(REPLACE(sm.definition, NCHAR(13), N'')),
        HASHBYTES('SHA2_256', REPLACE(sm.definition, NCHAR(13), N''))
    FROM sys.objects o JOIN sys.sql_modules sm ON sm.object_id = o.object_id
    WHERE o.schema_id = SCHEMA_ID('dh') AND sm.execute_as_principal_id IS NULL
    AND sm.uses_ansi_nulls = 1 AND sm.uses_quoted_identifier = 1
    AND ((o.type = 'FN' AND sm.is_schema_bound = 1) OR o.type = 'P')
) OR (SELECT COUNT(*) FROM sys.objects WHERE schema_id = SCHEMA_ID('dh')
    AND type IN ('P', 'FN', 'IF', 'TF', 'FS', 'FT', 'PC')) <> 18
    THROW 51008, 'Persistence module/signature/body drift.', 1;
-- Current registration must contain reviewed expectations, not hashes adopted from sys.sql_modules.
IF EXISTS (
    SELECT N'dh.' + Name, Type, SourceChecksum COLLATE Latin1_General_100_BIN2,
        DefinitionBytes, CONVERT(char(64), DefinitionHash, 2) COLLATE Latin1_General_100_BIN2 FROM @modules
    EXCEPT
    SELECT ObjectName, Kind, SourceChecksum COLLATE Latin1_General_100_BIN2,
        DefinitionBytes, DefinitionChecksum COLLATE Latin1_General_100_BIN2 FROM dh.ModuleDefinition
) OR (SELECT COUNT(*) FROM dh.ModuleDefinition) <> 18
    THROW 51008, 'Reviewed module registration drift.', 1;
DECLARE @grants TABLE
(
    RoleName sysname,
    ProcedureName sysname
);
INSERT @grants VALUES
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
    WHERE grantee_principal_id IN (DATABASE_PRINCIPAL_ID('dh_runtime'), DATABASE_PRINCIPAL_ID('dh_recovery'))) <> 9
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
                'CREATE TABLE', 'CREATE PROCEDURE', 'CREATE FUNCTION', 'EXECUTE', 'SELECT', 'INSERT', 'UPDATE', 'DELETE'))
        OR (dp.class = 3 AND dp.major_id = SCHEMA_ID('dh'))
        OR (dp.class = 1 AND dp.major_id IN (SELECT object_id FROM sys.tables WHERE schema_id = SCHEMA_ID('dh'))
            AND (dp.permission_name IN ('INSERT', 'UPDATE', 'DELETE', 'CONTROL', 'ALTER', 'TAKE OWNERSHIP')
                OR (dp.permission_name = 'SELECT' AND dp.grantee_principal_id IN
                    (DATABASE_PRINCIPAL_ID('public'), DATABASE_PRINCIPAL_ID('dh_runtime'), DATABASE_PRINCIPAL_ID('dh_recovery')))))
        OR (dp.class = 1 AND dp.major_id IN (SELECT object_id FROM sys.objects WHERE schema_id = SCHEMA_ID('dh') AND type IN ('P', 'FN'))
            AND NOT EXISTS (SELECT 1 FROM @grants g WHERE DATABASE_PRINCIPAL_ID(g.RoleName) = dp.grantee_principal_id
                AND OBJECT_ID(N'dh.' + g.ProcedureName) = dp.major_id AND dp.permission_name = 'EXECUTE' AND dp.state = 'G' AND dp.minor_id = 0))
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
    (4, '004_module_release.sql', '13908494AC2FE43DF6D8A2CAE2AADECE97F4818BABBB4A9F3066F0C5EFB13AA0');
IF EXISTS (SELECT * FROM @migrations EXCEPT SELECT Version, Name, Checksum FROM dh.SchemaVersion)
    OR (SELECT COUNT(*) FROM dh.SchemaVersion) <> 4
    THROW 51010, 'Migration name/checksum/version catalog drift.', 1;
-- The release declaration is checked after the runner has recorded its schema migration row.
IF (SELECT COUNT(*) FROM dh.ModuleRelease) <> 1
    OR NOT EXISTS (SELECT 1 FROM dh.ModuleRelease r JOIN dh.SchemaVersion s ON s.Version = r.Version
        WHERE r.Version = 4 AND r.ManifestChecksum COLLATE Latin1_General_100_BIN2 =
            'A8D8DC923CD472F1113DD1A1B1FC92A9A02E4034C5AF20489DEA87867ADE1070')
    THROW 51010, 'Module release declaration drift.', 1;
