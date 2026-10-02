-- OwnerKind: 0 Free, 1 Runtime, 2 Recovery.
-- Kind: 1 Acquire, 2 Checkpoint, 3 ReleaseRuntime, 4 Recover, 5 ReleaseRecovery.
-- Outcome: 1 Applied (100 Acquired, 101 CheckpointApplied, 102 CheckpointNoChange,
-- 103 Released, 104 Recovered, 105 RecoveredAbsent); 2 NotApplied (200 Busy, 201 StaleFence,
-- 202 Conflict, 203 IdentityMismatch, 204 InvalidClass, 205 CancelledBeforeApply,
-- 206 SequenceMismatch, 207 IntegrityFailure). CHECK expressions retain exact grouping.
-- Binding is provisioned separately; migrations never adopt or create game identities.
CREATE TABLE dh.CharacterAuthority
(
    SlotId tinyint NOT NULL CONSTRAINT PK_CharacterAuthority PRIMARY KEY
        CONSTRAINT CK_CharacterAuthority_Slot CHECK ([SlotId] = (1)),
    AccountId uniqueidentifier NOT NULL
        CONSTRAINT CK_CharacterAuthority_Account CHECK ([AccountId] <> '00000000-0000-0000-0000-000000000000'),
    CharacterId uniqueidentifier NOT NULL CONSTRAINT UQ_CharacterAuthority_CharacterId UNIQUE
        CONSTRAINT CK_CharacterAuthority_Character CHECK ([CharacterId] <> '00000000-0000-0000-0000-000000000000'),
    Fence bigint NOT NULL CONSTRAINT CK_CharacterAuthority_Fence CHECK ([Fence] >= (0)),
    OwnerKind tinyint NOT NULL,
    OwnerId uniqueidentifier NULL,
    LastSequence bigint NOT NULL CONSTRAINT CK_CharacterAuthority_Sequence CHECK ([LastSequence] >= (0)),
    ChangedUtc datetime2(3) NOT NULL,
    CONSTRAINT CK_CharacterAuthority_Owner CHECK
    (
        ([OwnerKind] = (0) AND [OwnerId] IS NULL AND [LastSequence] = (0))
        OR
        (
            ([OwnerKind] = (1) OR [OwnerKind] = (2))
            AND [OwnerId] IS NOT NULL
            AND [OwnerId] <> '00000000-0000-0000-0000-000000000000'
            AND [Fence] > (0)
        )
    )
);

CREATE TABLE dh.CharacterOperation
(
    OperationId uniqueidentifier NOT NULL CONSTRAINT PK_CharacterOperation PRIMARY KEY
        CONSTRAINT CK_CharacterOperation_Id CHECK ([OperationId] <> '00000000-0000-0000-0000-000000000000'),
    SlotId tinyint NOT NULL CONSTRAINT CK_CharacterOperation_Slot CHECK ([SlotId] = (1)),
    Kind tinyint NOT NULL CONSTRAINT CK_CharacterOperation_Kind CHECK
        ([Kind] = (1) OR [Kind] = (2) OR [Kind] = (3) OR [Kind] = (4) OR [Kind] = (5)),
    PayloadVersion tinyint NOT NULL CONSTRAINT CK_CharacterOperation_PayloadVersion CHECK ([PayloadVersion] = (1)),
    Payload varbinary(512) NOT NULL CONSTRAINT CK_CharacterOperation_Payload
        CHECK (datalength([Payload]) >= (1) AND datalength([Payload]) <= (512)),
    Outcome tinyint NOT NULL,
    ResultCode smallint NOT NULL,
    ResultSnapshot nvarchar(2048) NULL CONSTRAINT CK_CharacterOperation_Json
        CHECK ([ResultSnapshot] IS NULL OR isjson([ResultSnapshot]) = (1)),
    RecordedUtc datetime2(3) NOT NULL,
    CONSTRAINT FK_CharacterOperation_Authority FOREIGN KEY (SlotId) REFERENCES dh.CharacterAuthority(SlotId),
    CONSTRAINT CK_CharacterOperation_Outcome CHECK
    (
        (
            [Outcome] = (1) AND [ResultSnapshot] IS NOT NULL
            AND
            (
                ([Kind] = (1) AND [ResultCode] = (100))
                OR ([Kind] = (2) AND ([ResultCode] = (101) OR [ResultCode] = (102)))
                OR (([Kind] = (3) OR [Kind] = (5)) AND [ResultCode] = (103))
                OR ([Kind] = (4) AND ([ResultCode] = (104) OR [ResultCode] = (105)))
            )
        )
        OR
        (
            [Outcome] = (2) AND [ResultSnapshot] IS NULL
            AND
            (
                [ResultCode] = (200) OR [ResultCode] = (201) OR [ResultCode] = (203)
                OR [ResultCode] = (205) OR [ResultCode] = (206) OR [ResultCode] = (207)
                OR (([Kind] = (1) OR [Kind] = (2)) AND [ResultCode] = (204))
                OR ([Kind] = (2) AND [ResultCode] = (202))
            )
        )
    )
);

CREATE ROLE dh_runtime AUTHORIZATION dbo;
CREATE ROLE dh_recovery AUTHORIZATION dbo;
