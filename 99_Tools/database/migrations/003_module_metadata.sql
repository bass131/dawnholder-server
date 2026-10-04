-- Current code identity is separate from immutable migration history.
-- No SchemaVersion FK: the runner records that row after the migration body.
-- The final catalog checks release versions against the recorded migrations in the same transaction.
CREATE TABLE dh.ModuleRelease
(
    Version int NOT NULL CONSTRAINT PK_ModuleRelease PRIMARY KEY,
    ManifestChecksum char(64) NOT NULL
);

CREATE TABLE dh.ModuleDefinition
(
    ObjectName sysname NOT NULL CONSTRAINT PK_ModuleDefinition PRIMARY KEY,
    Kind char(2) NOT NULL,
    SourceChecksum char(64) NOT NULL,
    DefinitionBytes int NOT NULL,
    DefinitionChecksum char(64) NOT NULL
);
