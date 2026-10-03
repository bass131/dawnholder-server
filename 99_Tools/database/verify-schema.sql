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
    WHERE t.schema_id = SCHEMA_ID('dh')
    AND c.is_identity = 0 AND c.is_computed = 0
    AND c.is_sparse = 0 AND c.generated_always_type = 0
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
    JOIN sys.indexes i ON i.object_id = t.object_id
    JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
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
    AND parent_object_id IN (
        OBJECT_ID('dh.SchemaVersion'), OBJECT_ID('dh.Account'),
        OBJECT_ID('dh.Character'), OBJECT_ID('dh.CharacterProgress'))
    AND is_disabled = 0 AND is_not_trusted = 0 AND is_not_for_replication = 0
) OR (SELECT COUNT(*) FROM sys.check_constraints WHERE schema_id = SCHEMA_ID('dh')
    AND parent_object_id IN (
        OBJECT_ID('dh.SchemaVersion'), OBJECT_ID('dh.Account'),
        OBJECT_ID('dh.Character'), OBJECT_ID('dh.CharacterProgress'))) <> 4
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
    ('ReadAdmission', 'P', 12072, 0xB2F72D5D3785C92F66D90F59FED7D868CD2447AD498403DF34B7D3BC76D025A8,
        '75A5EEBF685669B4F94FEE4654039F6F9918B0C34F58D5848088CA82F2893439'),
    ('AcquireAndLoad', 'P', 30852, 0x62A12FB3F4B7891DDEE834968BA9BB6BD7823316DF757AABEFF8806DDF10D40C,
        '432C596B4870625564B0462791985A4C1330102CBC4F8A6A69B4607DE7A768F6'),
    ('WriteSafeCheckpoint', 'P', 32552, 0x0B3E65F06D84A9278FEDA3184F52BB234261D435F87D204B851874A8A0B8ED85,
        'AE1EAEFEC64AA790D4B324C902D051F9ED6BFB0726D46A43DA4BD9217DE85812'),
    ('ReleaseRuntime', 'P', 24864, 0xC9048FDBC6DF65658A2BF0BBC752A67C605258BDFEC9B6506935AB10A6039A37,
        '4ADC8C4840ABFA894A984411522E8F3B23F66A579941EF8AA213F4BE21036DB1'),
    ('ResolveRuntimeOperation', 'P', 19542, 0x06D3E53E8BBD94F73706450288B5E5989514CB9E3E03D61D16839294FD32385C,
        '55DDBDB6D3C617A98400E79B327EFD75AE21F0F4ED440C8499C974789ADA8B20'),
    ('InspectRecovery', 'P', 15568, 0x65CC7DE08BF3AB5D1628A760A75E97687943236EBF67B7A222FCA58BAB6D73BD,
        'C12617844F4D913174A694518A8CB7439677E1D50A9029DE5A76F5BCA03D1937'),
    ('RecoverAndLoad', 'P', 25744, 0xF5286664BE0D445161F4DFE58BD7FE43A8D8A83DAF5CA584752BEC5E232600AF,
        'EAC2DBBF54464B25FBF6EC9E95A5463D6C61D1FB778FFD3DC2A6A0A74DE1DD46'),
    ('ReleaseRecovery', 'P', 24874, 0x9A721D41741327EB201C156D78958EF00F7847ABFA0F23E2E8353E56385F3262,
        '06CC80FB31A554C3F9DE4B08C8EFD39AB136F420DB0BC4E2E9C9AF6EEBC01A66'),
    ('ResolveRecoveryOperation', 'P', 19440, 0x9AF8060ADD23E33BFD207175ED58C7EB8DDC8DD8E98786BDD229BE7E0966F25D,
        '1F97F00BF04157CFEBA9E46467883586DE27CBFE4526E11077BDDA416E02DFFF');
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
    (4, '004_module_release.sql', '8A9BA4EDAE097AAD802ABC65BE5C873230BC243F6325845288F9E2B69953620A');
IF EXISTS (SELECT * FROM @migrations EXCEPT SELECT Version, Name, Checksum FROM dh.SchemaVersion)
    OR (SELECT COUNT(*) FROM dh.SchemaVersion) <> 4
    THROW 51010, 'Migration name/checksum/version catalog drift.', 1;
-- The release declaration is checked after the runner has recorded its schema migration row.
IF (SELECT COUNT(*) FROM dh.ModuleRelease) <> 1
    OR NOT EXISTS (SELECT 1 FROM dh.ModuleRelease r JOIN dh.SchemaVersion s ON s.Version = r.Version
        WHERE r.Version = 4 AND r.ManifestChecksum COLLATE Latin1_General_100_BIN2 =
            '589B5B102A906D16B67FC49F555E3EF5CC56152450196C7A6E8781CB8F759671')
    THROW 51010, 'Module release declaration drift.', 1;
