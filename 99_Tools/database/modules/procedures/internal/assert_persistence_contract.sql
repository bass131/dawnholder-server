CREATE OR ALTER PROCEDURE dh.AssertPersistenceContract
    @SchemaVersion int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    -- Caller has already validated binding and holds the slot transaction/applock.
    -- Count + endpoints are strict because SchemaVersion has an integer primary key.
    -- 51023 is the existing incompatible-schema error; no result set or role grant is added.
    DECLARE @requiredSchemaVersion int = 4;
    IF (SELECT COUNT(*) FROM dh.SchemaVersion) <> @requiredSchemaVersion
        OR (SELECT MIN(Version) FROM dh.SchemaVersion) <> 1
        OR (SELECT MAX(Version) FROM dh.SchemaVersion) <> @requiredSchemaVersion
        THROW 51023, 'Persistence schema version is incompatible.', 1;

    SET @SchemaVersion = @requiredSchemaVersion;
END;
