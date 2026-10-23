CREATE OR ALTER PROCEDURE dh.RecordOperationReceipt
    @CharacterId uniqueidentifier,
    @SlotId int,
    @OperationId uniqueidentifier,
    @Kind int,
    @payload varbinary(512),
    @outcome tinyint,
    @resultCode smallint,
    @resultSnapshot nvarchar(2048),
    @recordedUtc datetime2(3) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    -- Record only the caller-decided fresh result at its original timestamp/INSERT site; no replay decision.
    SET @recordedUtc = SYSUTCDATETIME();
    INSERT dh.CharacterOperation
    (
        OperationId,
        SlotId,
        CharacterId,
        Kind,
        PayloadVersion,
        Payload,
        Outcome,
        ResultCode,
        ResultSnapshot,
        RecordedUtc
    )
    VALUES
    (
        @OperationId,
        @SlotId,
        @CharacterId,
        @Kind,
        1,
        @payload,
        @outcome,
        @resultCode,
        @resultSnapshot,
        @recordedUtc
    );
END;
