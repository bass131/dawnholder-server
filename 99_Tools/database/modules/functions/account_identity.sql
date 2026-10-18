CREATE OR ALTER FUNCTION dh.AccountIdentity(@Name varchar(16))
RETURNS uniqueidentifier
WITH SCHEMABINDING
AS
BEGIN
    -- UUID v5 namespace and ASCII normalized name, in RFC network byte order (same as AccountIdResolver).
    IF @Name IS NULL OR DATALENGTH(@Name) NOT BETWEEN 3 AND 16
        OR @Name COLLATE Latin1_General_100_BIN2 LIKE '%[^a-z0-9_]%'
        RETURN NULL;
    DECLARE @hash binary(20) = HASHBYTES('SHA1',
        0xd05083ec60674d219ce9271da508bbfd + CONVERT(varbinary(16), @Name));
    DECLARE @hex varchar(40) = LOWER(CONVERT(varchar(40), @hash, 2));
    SET @hex = STUFF(@hex, 13, 1, '5');
    DECLARE @variant int = CONVERT(int, SUBSTRING(@hash, 9, 1)) % 64 + 128;
    SET @hex = STUFF(@hex, 17, 2, LOWER(CONVERT(char(2), CONVERT(binary(1), @variant), 2)));
    RETURN CONVERT(uniqueidentifier, SUBSTRING(@hex, 1, 8) + '-' + SUBSTRING(@hex, 9, 4) + '-' +
        SUBSTRING(@hex, 13, 4) + '-' + SUBSTRING(@hex, 17, 4) + '-' + SUBSTRING(@hex, 21, 12));
END;
