-- Fail closed on catalog drift. Behavioral tests are in Test-Database.ps1.
SET NOCOUNT ON;
DECLARE @columns TABLE (TableName sysname, ColumnName sysname, TypeName sysname, Length smallint, Scale tinyint);
INSERT @columns VALUES
('SchemaVersion','Version','int',4,0),('SchemaVersion','Name','nvarchar',256,0),
('SchemaVersion','Checksum','char',64,0),('SchemaVersion','AppliedUtc','datetime2',7,3),
('Account','AccountId','uniqueidentifier',16,0),('Account','CreatedUtc','datetime2',7,3),
('Character','CharacterId','uniqueidentifier',16,0),('Character','AccountId','uniqueidentifier',16,0),
('Character','Class','tinyint',1,0),('Character','CreatedUtc','datetime2',7,3),('Character','Version','timestamp',8,0),
('CharacterProgress','CharacterId','uniqueidentifier',16,0),('CharacterProgress','MapId','tinyint',1,0),
('CharacterProgress','PositionX','real',4,0),('CharacterProgress','PositionY','real',4,0),
('CharacterProgress','Hp','int',4,0),('CharacterProgress','MaxHp','int',4,0),
('CharacterProgress','BossUnlocked','bit',1,0),('CharacterProgress','SavedUtc','datetime2',7,3),
('CharacterProgress','Version','timestamp',8,0);
IF EXISTS (
    SELECT TableName,ColumnName,TypeName,Length,Scale FROM @columns
    EXCEPT
    SELECT t.name,c.name,ty.name,c.max_length,c.scale FROM sys.tables t
    JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id
    WHERE t.schema_id=SCHEMA_ID('dh') AND c.is_nullable=0 AND c.is_identity=0 AND c.is_computed=0
) OR (SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id WHERE t.schema_id=SCHEMA_ID('dh')) <> 20
    THROW 51001, 'Column/type/nullability drift detected.', 1;

DECLARE @indexes TABLE (TableName sysname, IndexName sysname, ColumnName sysname, IsUnique bit, IsPrimary bit);
INSERT @indexes VALUES
('SchemaVersion','PK_SchemaVersion','Version',1,1),('SchemaVersion','UQ_SchemaVersion_Name','Name',1,0),
('Account','PK_Account','AccountId',1,1),('Character','PK_Character','CharacterId',1,1),
('Character','IX_Character_AccountId','AccountId',0,0),('CharacterProgress','PK_CharacterProgress','CharacterId',1,1);
IF EXISTS (
    SELECT * FROM @indexes
    EXCEPT
    SELECT t.name,i.name,c.name,i.is_unique,i.is_primary_key FROM sys.tables t
    JOIN sys.indexes i ON i.object_id=t.object_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
    JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
    WHERE t.schema_id=SCHEMA_ID('dh') AND i.is_disabled=0 AND i.has_filter=0 AND ic.key_ordinal=1
    AND (SELECT COUNT(*) FROM sys.index_columns x WHERE x.object_id=i.object_id AND x.index_id=i.index_id)=1
) THROW 51002, 'Primary/unique/FK lookup index drift detected.', 1;

DECLARE @foreignKeys TABLE (Name sysname, ParentTable sysname, ParentColumn sysname, RefTable sysname, RefColumn sysname);
INSERT @foreignKeys VALUES
('FK_Character_Account','Character','AccountId','Account','AccountId'),
('FK_CharacterProgress_Character','CharacterProgress','CharacterId','Character','CharacterId');
IF EXISTS (
    SELECT * FROM @foreignKeys
    EXCEPT
    SELECT fk.name,OBJECT_NAME(fk.parent_object_id),COL_NAME(fc.parent_object_id,fc.parent_column_id),
           OBJECT_NAME(fk.referenced_object_id),COL_NAME(fc.referenced_object_id,fc.referenced_column_id)
    FROM sys.foreign_keys fk JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=fk.object_id
    WHERE fk.schema_id=SCHEMA_ID('dh') AND fk.is_disabled=0 AND fk.is_not_trusted=0
      AND fk.delete_referential_action=0 AND fk.update_referential_action=0
      AND OBJECT_SCHEMA_NAME(fk.referenced_object_id)='dh'
) THROW 51003, 'Foreign key drift detected.', 1;

IF (SELECT COUNT(*) FROM sys.check_constraints WHERE schema_id=SCHEMA_ID('dh') AND is_disabled=0 AND is_not_trusted=0
    AND name IN ('CK_SchemaVersion_Version','CK_Character_Class','CK_CharacterProgress_Map','CK_CharacterProgress_Hp')) <> 4
    THROW 51004, 'Missing, disabled or untrusted check constraint.', 1;
IF (SELECT COUNT(*) FROM sys.default_constraints WHERE schema_id=SCHEMA_ID('dh')
    AND name IN ('DF_SchemaVersion_AppliedUtc','DF_Account_CreatedUtc','DF_Character_CreatedUtc',
                 'DF_CharacterProgress_BossUnlocked','DF_CharacterProgress_SavedUtc')) <> 5
    THROW 51005, 'Missing default constraint.', 1;
