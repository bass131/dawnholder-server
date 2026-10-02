-- Exact 001 and persistence-v1 catalog contract; runtime behavior requires independent SQL tests.
-- Applied inside the unchanged migration runner transaction. No dynamic SQL or data repair.
SET NOCOUNT ON;
DECLARE @columns TABLE (TableName sysname, ColumnName sysname, TypeName sysname, Length smallint, Scale tinyint, IsNullable bit);
INSERT @columns VALUES
('SchemaVersion','Version','int',4,0,0),
('SchemaVersion','Name','nvarchar',256,0,0),
('SchemaVersion','Checksum','char',64,0,0),
('SchemaVersion','AppliedUtc','datetime2',7,3,0),
('Account','AccountId','uniqueidentifier',16,0,0),
('Account','CreatedUtc','datetime2',7,3,0),
('Character','CharacterId','uniqueidentifier',16,0,0),
('Character','AccountId','uniqueidentifier',16,0,0),
('Character','Class','tinyint',1,0,0),
('Character','CreatedUtc','datetime2',7,3,0),
('Character','Version','timestamp',8,0,0),
('CharacterProgress','CharacterId','uniqueidentifier',16,0,0),
('CharacterProgress','MapId','tinyint',1,0,0),
('CharacterProgress','PositionX','real',4,0,0),
('CharacterProgress','PositionY','real',4,0,0),
('CharacterProgress','Hp','int',4,0,0),
('CharacterProgress','MaxHp','int',4,0,0),
('CharacterProgress','BossUnlocked','bit',1,0,0),
('CharacterProgress','SavedUtc','datetime2',7,3,0),
('CharacterProgress','Version','timestamp',8,0,0),
('CharacterAuthority','SlotId','tinyint',1,0,0),
('CharacterAuthority','AccountId','uniqueidentifier',16,0,0),
('CharacterAuthority','CharacterId','uniqueidentifier',16,0,0),
('CharacterAuthority','Fence','bigint',8,0,0),
('CharacterAuthority','OwnerKind','tinyint',1,0,0),
('CharacterAuthority','OwnerId','uniqueidentifier',16,0,1),
('CharacterAuthority','LastSequence','bigint',8,0,0),
('CharacterAuthority','ChangedUtc','datetime2',7,3,0),
('CharacterOperation','OperationId','uniqueidentifier',16,0,0),
('CharacterOperation','SlotId','tinyint',1,0,0),
('CharacterOperation','Kind','tinyint',1,0,0),
('CharacterOperation','PayloadVersion','tinyint',1,0,0),
('CharacterOperation','Payload','varbinary',512,0,0),
('CharacterOperation','Outcome','tinyint',1,0,0),
('CharacterOperation','ResultCode','smallint',2,0,0),
('CharacterOperation','ResultSnapshot','nvarchar',4096,0,1),
('CharacterOperation','RecordedUtc','datetime2',7,3,0);
IF EXISTS (
    SELECT TableName,ColumnName,TypeName,Length,Scale,IsNullable FROM @columns
    EXCEPT
    SELECT t.name,c.name,ty.name,c.max_length,c.scale,c.is_nullable FROM sys.tables t
    JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id
    WHERE t.schema_id=SCHEMA_ID('dh') AND c.is_identity=0 AND c.is_computed=0 AND c.is_sparse=0 AND c.generated_always_type=0
) OR (SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id WHERE t.schema_id=SCHEMA_ID('dh')) <> 37
    THROW 51001, 'Column/type/nullability drift detected.', 1;

DECLARE @indexes TABLE (TableName sysname, IndexName sysname, ColumnName sysname, IsUnique bit, IsPrimary bit, IndexType tinyint);
INSERT @indexes VALUES
('SchemaVersion','PK_SchemaVersion','Version',1,1,1),('SchemaVersion','UQ_SchemaVersion_Name','Name',1,0,2),
('Account','PK_Account','AccountId',1,1,1),('Character','PK_Character','CharacterId',1,1,1),
('Character','IX_Character_AccountId','AccountId',0,0,2),('CharacterProgress','PK_CharacterProgress','CharacterId',1,1,1),
('CharacterAuthority','PK_CharacterAuthority','SlotId',1,1,1),
('CharacterAuthority','UQ_CharacterAuthority_CharacterId','CharacterId',1,0,2),
('CharacterOperation','PK_CharacterOperation','OperationId',1,1,1);
IF EXISTS (
    SELECT * FROM @indexes
    EXCEPT
    SELECT t.name,i.name,c.name,i.is_unique,i.is_primary_key,i.type FROM sys.tables t
    JOIN sys.indexes i ON i.object_id=t.object_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
    JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
    WHERE t.schema_id=SCHEMA_ID('dh') AND i.is_disabled=0 AND i.has_filter=0 AND i.is_hypothetical=0
        AND i.ignore_dup_key=0 AND ic.key_ordinal=1 AND ic.is_descending_key=0
    AND (SELECT COUNT(*) FROM sys.index_columns x WHERE x.object_id=i.object_id AND x.index_id=i.index_id)=1
) OR (SELECT COUNT(*) FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id
    WHERE t.schema_id=SCHEMA_ID('dh') AND i.index_id>0)<>9
    THROW 51002, 'Primary/unique/FK lookup index drift detected.', 1;

DECLARE @foreignKeys TABLE (Name sysname, ParentTable sysname, ParentColumn sysname, RefTable sysname, RefColumn sysname);
INSERT @foreignKeys VALUES
('FK_Character_Account','Character','AccountId','Account','AccountId'),
('FK_CharacterProgress_Character','CharacterProgress','CharacterId','Character','CharacterId'),
('FK_CharacterOperation_Authority','CharacterOperation','SlotId','CharacterAuthority','SlotId');
IF EXISTS (
    SELECT * FROM @foreignKeys
    EXCEPT
    SELECT fk.name,OBJECT_NAME(fk.parent_object_id),COL_NAME(fc.parent_object_id,fc.parent_column_id),
           OBJECT_NAME(fk.referenced_object_id),COL_NAME(fc.referenced_object_id,fc.referenced_column_id)
    FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=fk.object_id
    WHERE fk.schema_id=SCHEMA_ID('dh') AND fk.is_disabled=0 AND fk.is_not_trusted=0
      AND fk.delete_referential_action=0 AND fk.update_referential_action=0
      AND OBJECT_SCHEMA_NAME(fk.referenced_object_id)='dh'
) OR (SELECT COUNT(*) FROM sys.foreign_key_columns fc JOIN sys.foreign_keys fk ON fk.object_id=fc.constraint_object_id
    WHERE fk.schema_id=SCHEMA_ID('dh'))<>3
    THROW 51003, 'Foreign key drift detected.', 1;

-- Compare SQL Server's stored expression text as well as names/ownership.
-- Deliberately fail closed even for a manually rewritten equivalent expression.
DECLARE @checks TABLE (Name sysname, TableName sysname, ColumnName sysname NULL, Definition nvarchar(4000));
INSERT @checks VALUES
('CK_SchemaVersion_Version','SchemaVersion','Version','([Version]>(0))'),
('CK_Character_Class','Character','Class','([Class]=(1) OR [Class]=(0))'),
('CK_CharacterProgress_Map','CharacterProgress','MapId','([MapId]=(3) OR [MapId]=(2) OR [MapId]=(1) OR [MapId]=(0))'),
('CK_CharacterProgress_Hp','CharacterProgress',NULL,'([MaxHp]>(0) AND [Hp]>=(0) AND [Hp]<=[MaxHp])');
IF EXISTS (
    SELECT * FROM @checks
    EXCEPT
    SELECT name,OBJECT_NAME(parent_object_id),COL_NAME(parent_object_id,parent_column_id),definition
    FROM sys.check_constraints WHERE schema_id=SCHEMA_ID('dh')
    AND parent_object_id IN (OBJECT_ID('dh.SchemaVersion'),OBJECT_ID('dh.Account'),OBJECT_ID('dh.Character'),OBJECT_ID('dh.CharacterProgress'))
    AND is_disabled=0 AND is_not_trusted=0 AND is_not_for_replication=0
) OR (SELECT COUNT(*) FROM sys.check_constraints WHERE schema_id=SCHEMA_ID('dh')
    AND parent_object_id IN (OBJECT_ID('dh.SchemaVersion'),OBJECT_ID('dh.Account'),OBJECT_ID('dh.Character'),OBJECT_ID('dh.CharacterProgress'))) <> 4
    THROW 51004, 'Check expression, ownership or trust drift detected.', 1;

DECLARE @defaults TABLE (Name sysname, TableName sysname, ColumnName sysname, Definition nvarchar(4000));
INSERT @defaults VALUES
('DF_SchemaVersion_AppliedUtc','SchemaVersion','AppliedUtc','(sysutcdatetime())'),
('DF_Account_CreatedUtc','Account','CreatedUtc','(sysutcdatetime())'),
('DF_Character_CreatedUtc','Character','CreatedUtc','(sysutcdatetime())'),
('DF_CharacterProgress_BossUnlocked','CharacterProgress','BossUnlocked','((0))'),
('DF_CharacterProgress_SavedUtc','CharacterProgress','SavedUtc','(sysutcdatetime())');
IF EXISTS (
    SELECT * FROM @defaults
    EXCEPT
    SELECT name,OBJECT_NAME(parent_object_id),COL_NAME(parent_object_id,parent_column_id),definition
    FROM sys.default_constraints WHERE schema_id=SCHEMA_ID('dh')
) OR (SELECT COUNT(*) FROM sys.default_constraints WHERE schema_id=SCHEMA_ID('dh')) <> 5
    THROW 51005, 'Default expression or ownership drift detected.', 1;

IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID('dh'))<>6
    OR EXISTS (SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID('dh')
        EXCEPT SELECT TableName FROM @columns)
    THROW 51006, 'Unexpected dh table catalog.', 1;
IF (SELECT principal_id FROM sys.schemas WHERE name='dh')<>DATABASE_PRINCIPAL_ID('dbo')
    OR EXISTS (SELECT 1 FROM sys.objects WHERE schema_id=SCHEMA_ID('dh')
        AND principal_id IS NOT NULL AND principal_id<>DATABASE_PRINCIPAL_ID('dbo'))
    THROW 51006, 'Common dbo ownership chain drift.', 1;
IF EXISTS (SELECT 1 FROM sys.triggers WHERE parent_id IN
    (SELECT object_id FROM sys.tables WHERE schema_id=SCHEMA_ID('dh')))
    THROW 51006, 'Unexpected persisted game/schema trigger.', 1;

-- Preserve grouping/operators/literals; ignore only whitespace and identifier case for new expressions.
DECLARE @persistenceChecks TABLE (Name sysname,TableName sysname,ColumnName sysname NULL,Definition nvarchar(4000));
INSERT @persistenceChecks VALUES
('CK_CharacterAuthority_Slot','CharacterAuthority','SlotId',N'([SlotId]=(1))'),
('CK_CharacterAuthority_Account','CharacterAuthority','AccountId',N'([AccountId]<>''00000000-0000-0000-0000-000000000000'')'),
('CK_CharacterAuthority_Character','CharacterAuthority','CharacterId',N'([CharacterId]<>''00000000-0000-0000-0000-000000000000'')'),
('CK_CharacterAuthority_Fence','CharacterAuthority','Fence',N'([Fence]>=(0))'),
('CK_CharacterAuthority_Sequence','CharacterAuthority','LastSequence',N'([LastSequence]>=(0))'),
('CK_CharacterAuthority_Owner','CharacterAuthority',NULL,N'(([OwnerKind]=(0)AND[OwnerId]ISNULLAND[LastSequence]=(0))OR(([OwnerKind]=(1)OR[OwnerKind]=(2))AND[OwnerId]ISNOTNULLAND[OwnerId]<>''00000000-0000-0000-0000-000000000000''AND[Fence]>(0)))'),
('CK_CharacterOperation_Id','CharacterOperation','OperationId',N'([OperationId]<>''00000000-0000-0000-0000-000000000000'')'),
('CK_CharacterOperation_Slot','CharacterOperation','SlotId',N'([SlotId]=(1))'),
('CK_CharacterOperation_Kind','CharacterOperation','Kind',N'([Kind]=(1)OR[Kind]=(2)OR[Kind]=(3)OR[Kind]=(4)OR[Kind]=(5))'),
('CK_CharacterOperation_PayloadVersion','CharacterOperation','PayloadVersion',N'([PayloadVersion]=(1))'),
('CK_CharacterOperation_Payload','CharacterOperation','Payload',N'(datalength([Payload])>=(1)ANDdatalength([Payload])<=(512))'),
('CK_CharacterOperation_Outcome','CharacterOperation',NULL,N'(([Outcome]=(1)AND[ResultSnapshot]ISNOTNULLAND(([Kind]=(1)AND[ResultCode]=(100))OR([Kind]=(2)AND([ResultCode]=(101)OR[ResultCode]=(102)))OR(([Kind]=(3)OR[Kind]=(5))AND[ResultCode]=(103))OR([Kind]=(4)AND([ResultCode]=(104)OR[ResultCode]=(105)))))OR([Outcome]=(2)AND[ResultSnapshot]ISNULLAND([ResultCode]=(200)OR[ResultCode]=(201)OR[ResultCode]=(203)OR[ResultCode]=(205)OR[ResultCode]=(206)OR[ResultCode]=(207)OR(([Kind]=(1)OR[Kind]=(2))AND[ResultCode]=(204))OR([Kind]=(2)AND[ResultCode]=(202)))))'),
('CK_CharacterOperation_Json','CharacterOperation','ResultSnapshot',N'([ResultSnapshot]ISNULLORisjson([ResultSnapshot])=(1))');
IF EXISTS (
    SELECT Name,TableName,ColumnName,UPPER(Definition) COLLATE Latin1_General_100_BIN2 FROM @persistenceChecks
    EXCEPT
    SELECT name,OBJECT_NAME(parent_object_id),COL_NAME(parent_object_id,parent_column_id),
        UPPER(REPLACE(REPLACE(REPLACE(REPLACE(definition,N' ',N''),NCHAR(9),N''),NCHAR(10),N''),NCHAR(13),N''))
            COLLATE Latin1_General_100_BIN2
    FROM sys.check_constraints WHERE parent_object_id IN
        (OBJECT_ID('dh.CharacterAuthority'),OBJECT_ID('dh.CharacterOperation'))
        AND is_disabled=0 AND is_not_trusted=0 AND is_not_for_replication=0
) OR (SELECT COUNT(*) FROM sys.check_constraints WHERE schema_id=SCHEMA_ID('dh'))<>17
    THROW 51007, 'Persistence CHECK expression, ownership or trust drift.', 1;

-- SHA-256 of the exact LF-normalized UTF-16 module source, including signature and defaults.
-- Recompute these catalog constants whenever an unapplied module migration changes.
DECLARE @modules TABLE (Name sysname,Type char(2),DefinitionBytes int,DefinitionHash varbinary(32));
INSERT @modules VALUES
('PersistencePayloadV1','FN',8220,0x563597C65657994D2B20F9F85EA7B5A72895A91D3CF7BB01B423751C96C4D484),
('ReadAdmission','P',9038,0x7416B345C42E3FCD55F10E2046B49EF02BE89AD7D702F8E7D9A45EC9EB03589B),
('AcquireAndLoad','P',22426,0x8BCF64664F2F46C28E117B4A72FA9A5397AF0B658EE0B9E7BBE9EB454F7BB6A2),
('WriteSafeCheckpoint','P',23380,0x5E73B90317909DD40F9E05B08EF2FCABCAACF571754BB9264291B54063A77813),
('ReleaseRuntime','P',17682,0xE16A3E078452B6685F708FD6C3FF00D3036D8196BD4F1018B210B696E360F333),
('ResolveRuntimeOperation','P',14560,0xC26817799EE35EF23B8E967BC7DF8CFD11DD4C7008E7E3E7DB87B80EDCC8F6D7),
('InspectRecovery','P',11376,0x433889357438C0E3CDED2E753BE242D574805EDAE21F3D08FD16D3219B03389E),
('RecoverAndLoad','P',19392,0x30561C14C8F1BC7AA8D29AD2A9EA0EDB5E71FF8AE942534248DDE78296695246),
('ReleaseRecovery','P',17684,0xE03E5282F651B40BBF75B393F281CDC0B33008E3D54873B024272E39E7B12D3C),
('ResolveRecoveryOperation','P',14558,0x056F5B0AAC536C7CB60ED1199231BFD7F36B771B84FFC141768B8E6545F3AD08);
IF EXISTS (
    SELECT Name,Type,DefinitionBytes,DefinitionHash FROM @modules
    EXCEPT
    SELECT o.name,o.type,DATALENGTH(REPLACE(sm.definition,NCHAR(13),N'')),
        HASHBYTES('SHA2_256',REPLACE(sm.definition,NCHAR(13),N''))
    FROM sys.objects o JOIN sys.sql_modules sm ON sm.object_id=o.object_id
    WHERE o.schema_id=SCHEMA_ID('dh') AND sm.execute_as_principal_id IS NULL
        AND sm.uses_ansi_nulls=1 AND sm.uses_quoted_identifier=1
        AND ((o.type='FN' AND sm.is_schema_bound=1) OR o.type='P')
) OR (SELECT COUNT(*) FROM sys.objects WHERE schema_id=SCHEMA_ID('dh') AND type IN ('P','FN','IF','TF','FS','FT','PC'))<>10
    THROW 51008, 'Persistence module/signature/body drift.', 1;

DECLARE @grants TABLE (RoleName sysname,ProcedureName sysname);
INSERT @grants VALUES
('dh_runtime','ReadAdmission'),('dh_runtime','AcquireAndLoad'),('dh_runtime','WriteSafeCheckpoint'),
('dh_runtime','ReleaseRuntime'),('dh_runtime','ResolveRuntimeOperation'),
('dh_recovery','InspectRecovery'),('dh_recovery','RecoverAndLoad'),
('dh_recovery','ReleaseRecovery'),('dh_recovery','ResolveRecoveryOperation');
IF (SELECT COUNT(*) FROM sys.database_principals WHERE name IN ('dh_runtime','dh_recovery') AND type='R'
    AND is_fixed_role=0 AND owning_principal_id=DATABASE_PRINCIPAL_ID('dbo'))<>2
    THROW 51009, 'Persistence role/owner drift.', 1;
IF EXISTS (
    SELECT RoleName,ProcedureName FROM @grants
    EXCEPT
    SELECT p.name,o.name FROM sys.database_permissions dp
    JOIN sys.database_principals p ON p.principal_id=dp.grantee_principal_id
    JOIN sys.objects o ON o.object_id=dp.major_id
    WHERE dp.class=1 AND dp.minor_id=0 AND dp.permission_name='EXECUTE' AND dp.state='G'
        AND o.schema_id=SCHEMA_ID('dh') AND o.type='P'
) OR (SELECT COUNT(*) FROM sys.database_permissions
    WHERE grantee_principal_id IN (DATABASE_PRINCIPAL_ID('dh_runtime'),DATABASE_PRINCIPAL_ID('dh_recovery')))<>9
    THROW 51009, 'Individual execute-only grant drift.', 1;
-- A role nested into any other role could inherit direct DML or cross-role execution.
IF EXISTS (SELECT 1 FROM sys.database_role_members
    WHERE member_principal_id IN (DATABASE_PRINCIPAL_ID('dh_runtime'),DATABASE_PRINCIPAL_ID('dh_recovery')))
    THROW 51009, 'Unexpected persistence role inheritance.', 1;
IF EXISTS (
    SELECT 1 FROM sys.database_role_members assigned
    JOIN sys.database_role_members other ON other.member_principal_id=assigned.member_principal_id
        AND other.role_principal_id<>assigned.role_principal_id
    WHERE assigned.role_principal_id IN (DATABASE_PRINCIPAL_ID('dh_runtime'),DATABASE_PRINCIPAL_ID('dh_recovery'))
) THROW 51009, 'Persistence principal has another explicit database role.', 1;
-- Reject legacy direct writers and broad database/schema permissions, including public grants.
IF EXISTS (
    SELECT 1 FROM sys.database_permissions dp
    WHERE dp.state IN ('G','W')
      AND dp.grantee_principal_id<>DATABASE_PRINCIPAL_ID('dbo')
      AND (
        (dp.class=0 AND dp.permission_name IN ('CONTROL','ALTER','ALTER ANY SCHEMA','ALTER ANY ROLE',
            'CREATE TABLE','CREATE PROCEDURE','CREATE FUNCTION','EXECUTE','SELECT','INSERT','UPDATE','DELETE'))
        OR (dp.class=3 AND dp.major_id=SCHEMA_ID('dh'))
        OR (dp.class=1 AND dp.major_id IN (SELECT object_id FROM sys.tables WHERE schema_id=SCHEMA_ID('dh'))
            AND (dp.permission_name IN ('INSERT','UPDATE','DELETE','CONTROL','ALTER','TAKE OWNERSHIP')
                OR (dp.permission_name='SELECT' AND dp.grantee_principal_id IN
                    (DATABASE_PRINCIPAL_ID('public'),DATABASE_PRINCIPAL_ID('dh_runtime'),DATABASE_PRINCIPAL_ID('dh_recovery')))))
        OR (dp.class=1 AND dp.major_id IN (SELECT object_id FROM sys.objects WHERE schema_id=SCHEMA_ID('dh') AND type IN ('P','FN'))
            AND NOT EXISTS (SELECT 1 FROM @grants g WHERE DATABASE_PRINCIPAL_ID(g.RoleName)=dp.grantee_principal_id
                AND OBJECT_ID(N'dh.'+g.ProcedureName)=dp.major_id AND dp.permission_name='EXECUTE' AND dp.state='G' AND dp.minor_id=0))
      )
) THROW 51009, 'Unnecessary broad grant or direct writer permission.', 1;

-- Runner still verifies every on-disk migration checksum before this catalog check.
DECLARE @migrations TABLE (Version int,Name nvarchar(128),Checksum char(64));
INSERT @migrations VALUES
(1,'001_initial.sql','F28502BB1A8D683A66F596F15BA9EEC779E5110BAD8ADDAEE03E393F26E54FCC'),
(2,'002_persistence_metadata.sql','DDCE76E3BC67362E7520C9C3946AAAE63DBEBB4645176D3A6821C9F4F7D46557'),
(3,'003_persistence_payload.sql','A81A7B7DF0C9F582CA8F83F526C84879BB131C91784FAF53F30B56D29470FD95'),
(4,'004_read_admission.sql','BDA7667DF3A09F077C6513FDFD80423D0A65E0031420E8B2544440B509D44DB8'),
(5,'005_acquire_and_load.sql','CBA17300287FC0E1806B3704935D3F38144AF89EF9457826ADF81357D972B635'),
(6,'006_write_safe_checkpoint.sql','7985F088D0CC6E54011275A7B3A4BBA8C2E36EC8792251940C9AE49B31F735F7'),
(7,'007_release_runtime.sql','BE018661DA4E84F147214393C4BF92E7BF243792FB6DE9FB80C1C3AB4746912F'),
(8,'008_resolve_runtime_operation.sql','AB3BF9499EEE834D7AD73528AA0D6966003205242127636DA6D2C45BDC063D91'),
(9,'009_inspect_recovery.sql','DEA31F812ED6885E8DD0BA64C471A1C8B7EEA70E05466C9D114D6C65AC5A0568'),
(10,'010_recover_and_load.sql','F975900075D0AB41588209FB93983EAF5CE996AD9398EEC1BB0D4CB58A9BB2EC'),
(11,'011_release_recovery.sql','CE24A36B9F6350F23F215BBBA2087A10C2612B01AB3406ED6463EF36B5C31681'),
(12,'012_resolve_recovery_operation.sql','BC672294D974FB76C162D71675C2A93051B8FF93B5C9E797D45FCBDCF36B9CF6'),
(13,'013_persistence_grants.sql','48D17D4B3F3AB176BA4873DED33789F052ED028B4767EF0E786C3D7C00181B22');
IF EXISTS (SELECT * FROM @migrations EXCEPT SELECT Version,Name,Checksum FROM dh.SchemaVersion)
    OR (SELECT COUNT(*) FROM dh.SchemaVersion)<>13
    THROW 51010, 'Migration name/checksum/version catalog drift.', 1;
