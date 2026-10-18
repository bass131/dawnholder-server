CREATE OR ALTER PROCEDURE dh.ReadOperationReceipt
    @LockTimeoutMs int,
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
    -- Global ID lock is shared by every mutation kind, including the create ledger.
    DECLARE @lockResult int;
    DECLARE @resource nvarchar(255) = N'Dawnholder.Persistence.Operation.' + CONVERT(nvarchar(36), @OperationId);
    EXEC @lockResult = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive',
        @LockOwner = 'Transaction', @LockTimeout = @LockTimeoutMs;
    IF @lockResult NOT IN (0, 1) THROW 51022, 'Operation lock unavailable.', 1;
    SELECT @outcome = Outcome,
        @resultCode = ResultCode,
        @resultSnapshot = ResultSnapshot,
        @recordedUtc = RecordedUtc,
        @isReplay = 1,
        @status = CASE WHEN Kind = @Kind AND PayloadVersion = 1
        AND DATALENGTH(Payload) = DATALENGTH(@payload) AND Payload = @payload
        THEN 'Terminal' ELSE 'OperationPayloadMismatch' END
    FROM dh.CharacterOperation WHERE OperationId = @OperationId;
    IF EXISTS(SELECT 1 FROM dh.CharacterCreateOperation WHERE OperationId = @OperationId)
        SET @status = 'OperationPayloadMismatch';
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
