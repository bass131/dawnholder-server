CREATE OR ALTER PROCEDURE dh.ReadOperationReceipt
    @OperationId uniqueidentifier,
    @Kind int,
    @payload varbinary(512),
    @status varchar(32) OUTPUT,
    @outcome tinyint OUTPUT,
    @resultCode smallint OUTPUT,
    @resultSnapshot nvarchar(2048) OUTPUT,
    @isReplay bit OUTPUT,
    @recordedUtc datetime2(3) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    -- Only inspect and compare the historical receipt; the caller owns fresh/replay/seal decisions.
    SET @status = 'Terminal';
    SET @outcome = NULL;
    SET @resultCode = NULL;
    SET @resultSnapshot = NULL;
    SET @isReplay = 0;
    SET @recordedUtc = NULL;
    SELECT @outcome = Outcome,
        @resultCode = ResultCode,
        @resultSnapshot = ResultSnapshot,
        @recordedUtc = RecordedUtc,
        @isReplay = 1,
        @status = CASE WHEN Kind = @Kind AND PayloadVersion = 1
        AND DATALENGTH(Payload) = DATALENGTH(@payload) AND Payload = @payload
        THEN 'Terminal' ELSE 'OperationPayloadMismatch' END
    FROM dh.CharacterOperation WITH (UPDLOCK, HOLDLOCK) WHERE OperationId = @OperationId;
    IF @status = 'OperationPayloadMismatch'
    BEGIN
        -- Never expose a different request's historical receipt as this request's result.
        SET @outcome = NULL;
        SET @resultCode = NULL;
        SET @resultSnapshot = NULL;
        SET @recordedUtc = NULL;
        SET @isReplay = 0;
    END;
END;
