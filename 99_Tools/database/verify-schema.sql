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
    ('CharacterOperation', 'RecordedUtc', 'datetime2', 7, 3, 0);
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
) OR (SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id WHERE t.schema_id = SCHEMA_ID('dh')) <> 37
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
    ('CharacterOperation', 'PK_CharacterOperation', 'OperationId', 1, 1, 1);
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
    WHERE t.schema_id = SCHEMA_ID('dh') AND i.index_id > 0) <> 9
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
IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('dh')) <> 6
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
-- Recompute these catalog constants whenever an unapplied module migration changes.
DECLARE @modules TABLE
(
    Name sysname,
    Type char(2),
    DefinitionBytes int,
    DefinitionHash varbinary(32)
);
INSERT @modules VALUES
    ('PersistencePayloadV1', 'FN', 9216, 0x63868CCDC03EC1BA15658A29A0A959CD7D11AB5033D1C7D0EF73131ACB41C83E),
    ('AssertPersistenceContract', 'P', 1508, 0x930E9ADA0380D4D76AFF92C55A21C9A9DCC3E725B233C6B40449A4CA1C105F10),
    ('ReadAdmission', 'P', 13458, 0xBB66AD9AA6C67F5C2AB4A1C2CDB5C9227B98FD4483FD18FD432A46187C23B1A6),
    ('AcquireAndLoad', 'P', 33982, 0xA49AB75DA7D11740C646BA187D0792D5222253C16CC1EFC7290A757EA33B69A3),
    ('WriteSafeCheckpoint', 'P', 35690, 0xF43A97C533C26F3B92FDABCE7CF4DC31D857CB5A105497412A011B43817774AB),
    ('ReleaseRuntime', 'P', 27622, 0xD8A74E6F06E98B7C7B42EC4F3B23128D3CB32B1EBA77F99A8E5AFF19FA17DACF),
    ('ResolveRuntimeOperation', 'P', 22052, 0x3701BD10DF4723CD15B295482CD8FD13E3BFC96C1B0ACCBF818C350C44383677),
    ('InspectRecovery', 'P', 16534, 0x1CBA47DF4BE4825B5C520E8BC822FC9756F0F68A6B0FAF34BEEED3392E9285BA),
    ('RecoverAndLoad', 'P', 28796, 0x86A478C91C54C6B54CBF2868683172041866BA0D2DF5A51C0CEDF490B029D346),
    ('ReleaseRecovery', 'P', 27632, 0xAEA787CB282DA192820BA249FB57F3B946F11871168802D2064FCCDB02719907),
    ('ResolveRecoveryOperation', 'P', 21950, 0x44B8A150C3B213C003BFDA1C6A0F4B907430A6ED2374516B8B01D33F0635169B);
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
) OR (SELECT COUNT(*) FROM sys.objects WHERE schema_id = SCHEMA_ID('dh') AND type IN ('P', 'FN', 'IF', 'TF', 'FS', 'FT', 'PC')) <> 11
    THROW 51008, 'Persistence module/signature/body drift.', 1;
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
    (3, '003_persistence_payload.sql', 'B37E8DB8A894F51AFDC9EF62C06E525F5E4C257391E41B796F1B1711B9C7BA55'),
    (4, '004_assert_persistence_contract.sql', '2E8F18DBC16CB40FD502D73E7649A591470839777F95726C4D494E4BAE9B8B59'),
    (5, '005_read_admission.sql', 'AC8972298184143414842E8396ED3DFF4570CF75268E18B57C080E80D8597D3D'),
    (6, '006_acquire_and_load.sql', 'A895630D040569BC1BC5A668EFF74FE87D1E67ADA11144FFF17F39E7A224F3B0'),
    (7, '007_write_safe_checkpoint.sql', 'DE5168C67EA955E94BABE7A8B6096121F61E6B08333B14D604D375FB25DB0336'),
    (8, '008_release_runtime.sql', '641FC66FCFADD4EE8E998B78F6B2266AF153B59E4A29749423A9CECEFD5C552E'),
    (9, '009_resolve_runtime_operation.sql', '176A6426475401B0AA4BCF7C3BA16FDE16E0A02D15BD2BE766B89F4E3779F0C6'),
    (10, '010_inspect_recovery.sql', '16C4A12D51DA7AB60D935188D779B7ABC265FF5E7B9A87E940DFD0DB9416D6AF'),
    (11, '011_recover_and_load.sql', 'ECEAFA2B42E25FE4242A46613DF89049612EF8D8B8F2C4AFB8CE388588E23B98'),
    (12, '012_release_recovery.sql', 'D08D0D847250CDB0D8350120B5526792A296D6B2033CDDA489C5ACC0A7F6EFAF'),
    (13, '013_resolve_recovery_operation.sql', '233CB83ECCEE609A02AE5323302255EB5EA3A94D12FDB105E1160E8F9C5F43A5'),
    (14, '014_persistence_grants.sql', '48D17D4B3F3AB176BA4873DED33789F052ED028B4767EF0E786C3D7C00181B22');
IF EXISTS (SELECT * FROM @migrations EXCEPT SELECT Version, Name, Checksum FROM dh.SchemaVersion)
    OR (SELECT COUNT(*) FROM dh.SchemaVersion) <> 14
    THROW 51010, 'Migration name/checksum/version catalog drift.', 1;
