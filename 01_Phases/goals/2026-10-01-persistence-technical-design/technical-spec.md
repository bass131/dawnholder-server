# D1a 캐릭터 영속성 기술 명세

[goal](goal.md)이 범위·승인·결과를 소유한다. 이 문서는 D1b 구현 계약이며 **SQL·저장소·게임에서 실행 검증한 결과가 아니다**. D0의 고정 계정/캐릭터 1개, 최초 클래스 유지, 재접속 Town 안전 spawn·기본 풀HP, quest/해금 비영속 결정을 유지한다. 반증과 실행 gate는 [verification-plan](verification-plan.md)에 있다.

## 1. 선택과 적용 경계

기존 `Account`, `Character`, `CharacterProgress`는 게임 데이터를 담는다. 신규 `CharacterAuthority`는 고정 캐릭터를 지금 누가 사용할 수 있는지, `CharacterOperation`은 특정 요청이 확정됐는지를 담는다. 두 메타데이터 테이블을 둬서 게임 데이터의 rowversion이나 마지막 저장 시각에 이 역할을 맡기지 않는다. 001은 고정하고 신규 **002 이후** migration으로 테이블·procedure·DB role을 추가한다. 이 문서에는 실행할 SQL을 넣지 않는다.

| 결정 | 이유·제한 |
|---|---|
| `Microsoft.Data.SqlClient` **6.1.7** 고정 | 확인일 2026-10-01 기준 6.1은 LTS, 해당 계열 최신 patch가 6.1.7이며 지원 종료는 2028-08-14다. 최신 major 자동 추종보다 지원되는 계열의 명시적 갱신을 택한다. [Microsoft 지원표](https://learn.microsoft.com/en-us/sql/connect/ado-net/sqlclient-driver-support-lifecycle?view=sql-server-ver17) |
| GameServer `net10.0`, 직접 ADO.NET·typed RPC | [공식 NuGet 6.1.7](https://www.nuget.org/packages/Microsoft.Data.SqlClient/6.1.7)은 .NET 8+를 지원하고 net10 호환을 계산해 표시한다. ORM/다른 SQL driver를 함께 도입하지 않는다. Windows native SNI 산출물과 WSL 실제 runtime 호환은 D1b 실측 대상이다 |
| DB procedure가 transaction·직렬화 경계를 소유 | 한 SQL 호출에서 판정·변경·증빙을 원자 처리한다. C#에서 검사한 뒤 별도 UPDATE하지 않는다 |
| DB에 사전 바인딩한 **slot 1** | 001이 허용하는 여러 Character 행 중 임의 첫 행을 고르지 않는다. 신규 GUID를 매 재시도 생성해서 두 캐릭터를 만드는 길도 막는다 |
| DB의 증가하는 `bigint Fence` | 수명 교체마다 증가하며 감소·초기화·재사용하지 않는다. 메모리 lease/시간/프로세스 존재 여부에 의존하지 않는다 |
| 요청별 terminal ledger + 취소 확정 기록 | 응답 유실을 값 비교로 추측하지 않는다. 늦게 도착하는 동일 요청도 이미 확정된 결과에 묶는다 |

드라이버 설치·restore·패키지 취약점 검사·Windows/WSL .NET 접속은 아직 하지 않았다. 문서의 지원 여부와 실제 SQLExpress·OS·인증 조합의 성공을 구분한다. 후속 착수일에는 지원표/patch를 다시 확인하고 버전 변경은 diff와 재검증으로 남긴다.

## 2. 연결·설정·비밀·권한

### 2.1 저장소 설정과 수명

D1b `PersistenceOptions`는 immutable 값으로 `Database`, 명시적인 `ServerEndpoint`, `SlotId=1`, `AccountId`, `CharacterId`, timeout 설정을 받는다. 두 GUID는 비어 있으면 오류다. DB에 등록된 바인딩과 일치해야 한다. 새 GUID 생성, 다른 DB/계정으로 fallback, 실패 시 메모리 저장소 전환을 금지한다. 설정과 실제 `DB_NAME()`·schema/version·바인딩을 시작 gate에서 대조한다. gate 실패 시 영속 입장을 열지 않는다.

저장소는 주입된 connection factory만 사용한다. 비밀 읽기·환경 변수 해석·전역 singleton·GameSession 접근을 저장소 안에 넣지 않는다. D1b 범위는 options/factory의 계약과 검증이며 실제 Host·launcher 연결은 D2의 단일 writer에게 인계한다. 공개 설정과 자격증명을 분리하고 연결 문자열 전체를 로그하지 않는다.

- runtime: Windows와 WSL 모두 **최소 권한의 전용 SQL principal**을 기본 경로로 한다. 기존 Windows 관리자 통합 인증을 runtime에 쓰지 않는다. local admin tool만 Windows 로컬 통합 인증을 쓴다.
- endpoint: 기존 기록의 Windows `YYH_Desktop\SQLEXPRESS`, WSL loopback TCP 14330은 후보 근거일 뿐이다. 실행 직전 실제 endpoint/DB를 지정하고 확인한다. SQL Browser 탐색·다른 포트 자동 fallback은 없다.
- `SqlConnectionStringBuilder`: `Encrypt=Mandatory`, `TrustServerCertificate=false`, `PersistSecurityInfo=false`, `MultipleActiveResultSets=false`, `Enlist=false`, `ConnectRetryCount=0`, `Pooling=true`, `MinPoolSize=0`, `MaxPoolSize=4`, 고정 `Application Name=Dawnholder.Persistence`를 명시한다. `RetryLogicProvider`는 설정하지 않는다. pool은 실행 동시성 제한의 대체물이 아니다.
- 기존 self-signed 환경에서 인증서 검증이 실패하면 설정 오류로 멈춘다. D1b 격리 개발 시험에 한해서 endpoint를 확인한 뒤 `TrustServerCertificate=true`를 명시 승인·기록할 수 있으나 자동 fallback은 금지한다. TLS는 켜져 있어도 이 설정은 서버 신원을 검증하지 않는다. [Microsoft 인증서 설명](https://learn.microsoft.com/en-us/sql/connect/ado-net/encryption-and-certificate-validation?view=sql-server-ver17)
- 기존 DPAPI 자격증명은 승인된 로컬 launcher만 읽고 제한된 자식 process의 stdin/익명 pipe로 전달한다. WSL은 이미 사용하는 LF 인코딩 경계를 유지한다. CLI 인수·저장소·Unity·report·명령 출력·지속 환경 변수에 password/connection string을 넣지 않는다. D1a에서는 비밀 파일을 읽지 않는다.
- 한 repository 호출에 connection 하나를 열고 typed `SqlParameter`를 쓴다(`AddWithValue`/문자열 SQL 조립 금지). connection/reader/command를 actor 사이에서 공유하지 않는다. 전체 결과와 마지막 완료를 읽은 뒤 반환하고 정상 종결 후 dispose한다. 진행 중 task를 버리고 connection을 재사용하지 않는다.

### 2.2 최소 권한과 관리자 경계

002+에는 login/password를 넣지 않는다. `dh_runtime`·`dh_recovery` DB role과 개별 procedure의 EXECUTE 계약만 둔다. runtime은 `ReadAdmission`, `AcquireAndLoad`, `WriteSafeCheckpoint`, `ReleaseRuntime`, `ResolveRuntimeOperation`만 실행한다. recovery role은 별도 `InspectRecovery`, `RecoverAndLoad`, `ReleaseRecovery`, `ResolveRecoveryOperation`을 실행한다. role에 schema 전체 EXECUTE, `db_owner`, 직접 테이블 DML/DDL/DELETE, 다른 DB 권한을 주지 않는다. procedure와 테이블은 같은 소유자 아래 정적 SQL로 구현하며 동적 SQL·cross-database ownership chain·`EXECUTE AS OWNER` 범위 확대를 쓰지 않는다. [procedure 권한 부여 근거](https://learn.microsoft.com/en-us/sql/relational-databases/stored-procedures/grant-permissions-on-a-stored-procedure?view=sql-server-ver17)

`Configure-WslAccess.ps1`의 기존 runtime login은 3개 게임 테이블의 SELECT/INSERT/UPDATE를 받는다. **그 login을 새 저장소의 principal로 그대로 사용하면 이 설계의 fencing을 우회할 수 있다.** D1b 실행 gate에서 새 principal/role과 기존 writer 접근을 확인한다. 격리 DB에서는 legacy principal을 매핑하지 않으며, 기존 게임 DB를 선택하면 해당 DB의 기존 쓰기 권한 철회·사용 중 process 차단 계획을 별도 승인받아야 한다. D1a에서 기존 login/권한/script를 변경하지 않는다.

DB 소유자/sysadmin 자체를 기술적으로 막는 설계는 아니다. 관리 변경도 같은 slot 경계를 따르고 runtime을 quiesce한다. 로컬 Windows 관리자가 명시 실행하는 제한된 DB 관리 도구가 recovery 입구다. 일반 게임 서버·클라이언트 패킷·Management command에는 이 권한을 노출하지 않는다. Management의 복구 command/운영 권한 설계는 별도 목표다.

## 3. 002+ schema 계약과 각 필드의 이유

### 3.1 `dh.CharacterAuthority` — 고정 논리 주체와 현재 owner

MVP singleton metadata다. D1b의 승인된 초기화 도구가 **정확히 한 행**을 사전 등록한다. migration은 환경별 GUID를 하드코딩하거나 기존 게임 행을 자동 채택하지 않는다. 바인딩 등록은 Account/Character 생성을 뜻하지 않는다. 두 게임 행이 아직 없어도 이 metadata로 같은 경계를 잡을 수 있다.

| 필드 | SQL 타입·제약 | 필요한 이유 |
|---|---|---|
| `SlotId` | `tinyint NOT NULL PRIMARY KEY`, CHECK `=1` | 존재하지 않는 캐릭터도 하나의 개발용 논리 주체로 직렬화한다 |
| `AccountId` | `uniqueidentifier NOT NULL`, empty GUID 금지 | 어느 서버 고정 계정에 연결됐는지 고정한다 |
| `CharacterId` | `uniqueidentifier NOT NULL UNIQUE`, empty GUID 금지 | 최초 생성·재접속·재시도가 동일 캐릭터를 가리키게 한다 |
| `Fence` | `bigint NOT NULL`, CHECK `>=0`, 초기 0 | 이전 수명 요청을 DB에서 거부하는 영구 세대 번호다 |
| `OwnerKind` | `tinyint NOT NULL`, 0 Free / 1 Runtime / 2 Recovery | runtime과 관리자 수명을 분리하고 자동 takeover를 막는다 |
| `OwnerId` | `uniqueidentifier NULL`, 있으면 empty GUID 금지 | 같은 runtime 수명의 재전송과 새 세션의 중복 접속을 구분한다 |
| `LastSequence` | `bigint NOT NULL`, CHECK `>=0`, 초기 0 | 현재 owner에서 작업의 확정 순서를 검사한다 |
| `ChangedUtc` | `datetime2(3) NOT NULL`, DB UTC | 운영 진단 시 변경 시점을 설명한다. TTL/권한 판정에는 쓰지 않는다 |

행 CHECK: Free이면 `OwnerId IS NULL AND LastSequence=0`; Held이면 `OwnerId IS NOT NULL AND Fence>0`. 계정/캐릭터 바인딩은 일반 procedure로 변경/삭제하지 않는다. 아직 없는 게임 행을 가리키므로 이 두 GUID에 게임 테이블 FK를 걸지 않는다. 대신 모든 procedure가 게임 행 존재 시 Character.AccountId 일치를 검사하고 불일치면 실패한다. 임의 다중 캐릭터 선택·기존 행 삭제·바인딩 교체는 별도 승인 대상이다.

정상 acquire, release, 관리 recovery 각각 Fence를 **현재값+1**로 변경한다. release도 증가시켜 해제 전 admission probe를 무효화한다. `bigint` 최대값이면 overflow 전에 `IntegrityFailure`로 닫으며 wrap/reset하지 않는다. LastSequence는 acquire/recovery 때 0, runtime 작업마다 +1, release 후 Free에서는 0이다. DB backup restore로 counter/ledger를 과거로 돌리는 상황은 이 계약의 자동 복구 범위 밖이며 구 process·연결 격리와 별도 복구 gate 없이는 재개하지 않는다.

### 3.2 `dh.CharacterOperation` — 요청의 확정 증빙

진행 중 기록은 넣지 않는다. 게임/authority 변경과 **같은 transaction에서 terminal 결과만 INSERT**한다. 행을 다시 UPDATE해 상태를 바꾸지 않는다. 같은 ID가 반복되면 그 결과를 읽는다.

| 필드 | SQL 타입·제약 | 필요한 이유 |
|---|---|---|
| `OperationId` | `uniqueidentifier NOT NULL PRIMARY KEY`, empty GUID 금지 | 재전송과 응답 유실 조정의 고정 식별자다 |
| `SlotId` | `tinyint NOT NULL`, Authority FK, CHECK `=1` | 증빙을 삭제되지 않는 고정 주체에 연결한다 |
| `Kind` | `tinyint NOT NULL`, 1 Acquire / 2 Checkpoint / 3 ReleaseRuntime / 4 Recover / 5 ReleaseRecovery | 동일 ID로 다른 종류의 요청을 실행하지 못하게 한다 |
| `PayloadVersion` | `tinyint NOT NULL`, CHECK `=1` | 요청 동등성의 byte 형식을 migration/codec 변경에서 구분한다 |
| `Payload` | `varbinary(512) NOT NULL`, 길이 1..512 | hash 충돌에 의존하지 않고 실제 요청을 정확히 비교한다 |
| `Outcome` | `tinyint NOT NULL`, 1 Applied / 2 NotApplied | 변경 확정과 거부/취소 확정을 구분한다 |
| `ResultCode` | `smallint NOT NULL`, 아래 허용 집합만 | 클라이언트 예외 문자열 없이 확정 결과를 재현한다 |
| `ResultSnapshot` | `nvarchar(2048) NULL`, 존재하면 ISJSON=1 | 당시 반환할 class·token·fence·projection을 보존한다 |
| `RecordedUtc` | `datetime2(3) NOT NULL`, DB UTC | 증빙의 생성 시각을 진단한다. 성공 판정 근거는 ID/payload/outcome이다 |

허용 code: Applied=`Acquired(100)`, `CheckpointApplied(101)`, `CheckpointNoChange(102)`, `Released(103)`, `Recovered(104)`, `RecoveredAbsent(105)`; NotApplied=`Busy(200)`, `StaleFence(201)`, `Conflict(202)`, `IdentityMismatch(203)`, `InvalidClass(204)`, `CancelledBeforeApply(205)`, `SequenceMismatch(206)`, `IntegrityFailure(207)`. CHECK로 Outcome/ResultCode 조합을 제한한다. Applied는 ResultSnapshot 필수, NotApplied는 snapshot을 NULL로 둔다. 동일 ID/다른 payload는 기존 증빙을 바꾸지 않고 `OperationPayloadMismatch`를 반환한다.

Applied snapshot의 JSON v1 키는 `version`, `kind`, `resultCode`, `slotId`, `accountId`, `characterId`, `ownerKind`, `ownerId`, `fence`, `sequence`, `characterPresent`, `class`, `characterVersionHex`, `progressPresent`, `progressVersionHex`, `safe`(mapId/x/y/hp/maxHp/bossUnlocked), `storedProgress`(동일 필드 또는 null)다. BIGINT는 문자열, GUID는 정규 D 문자열, token은 정확히 16개 hex 문자다. Character absence의 class/token/safe/storedProgress는 null이고 Progress absence이면 progress token/storedProgress만 null이다. release는 게임 load 결과를 소비하지 않으므로 characterPresent부터 storedProgress까지 전부 null인 별도 shape와 해제 후 Free/fence/sequence를 담는다. SQL이 typed 값으로 생성하며 앱 JSON을 그대로 증빙으로 받지 않는다. 길이 초과·kind별 필수 키 누락은 transaction 실패다.

MVP에는 ledger 자동 TTL·purge·archive를 넣지 않는다. 바인딩/fence도 삭제하지 않는다. 증빙 크기 증가를 계측하고 후속 보존 정책을 별도 결정한다. 미해결 ID 또는 다시 도착 가능한 ID의 증빙 삭제는 idempotency를 깨므로 금지한다. 승인된 시험 전용 DB 폐기 외 cleanup은 아래 시험 계획을 따른다.

### 3.3 payload v1과 immutable 값

외부 입력은 typed 매개변수로 한정한다. DB 내부 공통 codec이 실제 판정에 쓰는 모든 매개변수에서 canonical byte열을 만든다. **호출자가 보낸 opaque hash/byte열을 믿지 않는다.** C#은 같은 typed 요청을 보관하고 Resolve에도 그대로 전달한다.

공통 순서: version(1 byte), kind(1), SlotId(1), AccountId(16), CharacterId(16), OwnerId(16), ExpectedFence(8), Sequence(8). 뒤에는 종류별 값이 붙는다. GUID는 SQL `CONVERT(binary(16), value)`, 정수는 지정 크기 binary 변환, token은 원래 8 bytes다. 가변 값은 길이 prefix를 붙이며 NULL은 별도 1-byte presence 뒤 값으로 인코딩한다. SQL binary 변환 규칙은 v1 codec에 고정하고 D1b에 golden vector로 검증한다. 길이와 bytes를 모두 비교해 padding 차이도 다른 payload로 판정한다.

| Kind | 공통부 뒤의 필드·순서 |
|---|---|
| Acquire | intendedClass(1), SafeDefaults: Town X/Y 각각 real의 binary(4), KnightMaxHp(4), MageMaxHp(4) |
| Checkpoint | CharacterVersion(8), ProgressVersion presence(1)+있으면(8), class(1), Town X/Y(각4), MaxHp(4). Hp=MaxHp·Map=0·unlock=false는 procedure가 구성 |
| ReleaseRuntime / ReleaseRecovery | 추가 없음. sequence와 expected fence가 이전 작업의 순서를 고정 |
| Recover | 관찰한 ExpectedOwnerKind(1), ExpectedOwnerId presence+값, 관리 사유 UTF-16LE(2-byte byte길이+최대 128자), SafeDefaults(위와 같음). 공통 OwnerId는 신규 관리 수명 ID |

Acquire/Recover는 Sequence=0이다. deadline/trace ID/retry count는 게임 의미가 없어 payload에서 제외한다. deadline만 새로 부여해 같은 ID의 의미를 바꾸지는 않는다. real 값은 SQL에 도달하기 전 finite·허용 content 범위를 검사하고 음의 0을 양의 0으로 정규화한다. DB도 범위·MaxHp 양수·class0/1·GUID·token 길이·kind별 필수 매개변수를 검사한다. 원래 값 범위를 벗어나는 HP나 class를 fallback으로 고치지 않는다.

GUID/enum/정수/float는 immutable value다. rowversion은 정확히 8-byte opaque token으로 취급하고 숫자 순서·시간으로 해석하지 않는다. request 생성, DB reader 수신, DTO 전달, 반환 accessor에서 mutable byte[] alias를 남기지 않는다. 내부 고정 8-byte 값 또는 방어 복사한 전용 value type을 사용한다. payload와 token을 포함한 DTO를 접수 후 수정할 수 없게 한다. [rowversion 근거](https://learn.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql?view=sql-server-ver17): 값이 같아도 UPDATE하면 token이 바뀌므로 불필요 UPDATE를 하지 않는다.

## 4. 모든 procedure가 공유하는 DB 경계

slot 1은 하나의 논리 캐릭터다. 모든 ReadAdmission/acquire/checkpoint/release/resolve/recovery는 동일 DB에서 고정 리소스 **`Dawnholder.Persistence.Slot.1`**, `@DbPrincipal='public'`, `Exclusive`, `LockOwner='Transaction'`인 `sp_getapplock`을 쓴다. GUID·프로세스·procedure 이름에 따라 다른 lock을 잡지 않는다. runtime/관리 principal도 동일한 resource/principal 조합을 쓴다. 이 제한을 장래 다중 계정 설계로 일반화하지 않는다.

공통 순서는 다음과 같다.

1. `@@TRANCOUNT=0` 확인(ambient/nested transaction 거부), `NOCOUNT ON`, `XACT_ABORT ON`, `READ COMMITTED`, 명시 transaction 시작.
2. 남은 예산에서 제한한 application lock 획득. 반환값 0/1만 성공. 음수이면 명시 rollback 후 해당 실패를 반환한다. lock deadlock 반환 자체가 자동 rollback이라는 가정을 두지 않는다.
3. Authority `UPDLOCK,HOLDLOCK` 읽기, 바인딩 검증. 고정 순서 Authority → Operation → Account → Character → Progress. 게임 행은 필요한 row/key range를 `UPDLOCK,HOLDLOCK`으로 읽고 transaction 끝까지 유지한다. RCSI가 켜져 있어도 token 검사와 변경 사이 의존 행이 바뀌지 않게 한다.
4. ledger에 동일 OperationId가 있으면 kind/version/길이/bytes 비교. 같으면 historical terminal 결과와 **현재** authority/token 관측을 돌려줄 준비만 한다. 다른 payload면 거부. 없으면 종류별 소유·fence·sequence·token 검증 후 변경하고 terminal 증빙을 INSERT한다.
5. 결과를 transaction 내부 변수에 담아 **COMMIT 후에만** result set을 내보낸다. `OUTPUT`으로 얻은 token도 commit 전 client에 내보내지 않는다. CATCH는 열린 transaction을 rollback하고 예외를 반환한다. commit 응답 유실은 별도 unknown 처리다.

트랜잭션 lock은 commit/rollback까지 유지되며 DB ID·principal·resource 이름이 lock identity를 이룬다. 음수 반환 처리 등은 [sp_getapplock 공식 계약](https://learn.microsoft.com/en-us/sql/relational-databases/system-stored-procedures/sp-getapplock-transact-sql?view=sql-server-ver17)을 따른다. `XACT_ABORT`와 `THROW`를 사용하지만 클라이언트 cancel을 rollback 증거로 읽지는 않는다. [XACT_ABORT 근거](https://learn.microsoft.com/en-us/sql/t-sql/statements/set-xact-abort-transact-sql?view=sql-server-ver17)

두 연결에서 old가 lock을 먼저 얻으면 new는 old의 commit/rollback 뒤 결과를 읽는다. new의 acquire/recovery가 먼저 끝나면 old는 fence 불일치로 쓰기·해제를 못 한다. 직접 DML이 가능한 writer를 허용하면 이 증명이 깨지므로 권한 gate가 필수다. migration의 별도 `Dawnholder.SchemaMigration` lock은 runtime slot lock의 대체물이 아니다. schema 배포 시 runtime을 quiesce한다.

## 5. procedure별 transaction 계약

### 5.1 ReadAdmission → AcquireAndLoad

`ReadAdmission`은 동일 경계에서 schema 호환 정보·binding·Free 여부·OwnerId·Fence만 읽는다. runtime에 SchemaVersion 직접 SELECT를 주지 않고 이 procedure 안에서 요구 migration/contract version을 검사한다. 게임 권위를 부여하거나 ledger에 쓰지 않는다. Held이면 신규 접속을 거부한다. 같은 process에서 활성 owner임을 알고 있으면 duplicate, 다른/미확정 owner이면 RecoveryRequired로 표시하며 어느 쪽도 takeover하지 않는다. Free이면 서버 owner가 이 fence와 **새 고정 OperationId·OwnerId**로 Acquire 요청을 한 번 만든다. probe 이후 상태가 바뀌면 acquire에서 거부한다. 응답 유실 재시도에는 GUID·payload를 바꾸지 않는다.

Acquire 공통 검증 뒤 `OwnerKind=Free AND Fence=ExpectedFence`를 요구한다. 기존 held runtime을 새 요청으로 빼앗지 않는다. Account 없으면 고정 AccountId로 INSERT, Character 없으면 **바인딩된 CharacterId**와 유효 intendedClass로 INSERT한다. Character가 있으면 Account 일치·class0/1을 검사하고 **저장 class를 사용**한다. 임의 첫 캐릭터 선택·기존 class UPDATE를 하지 않는다.

Progress가 없으면 stored class에 맞는 SafeDefaults로 INSERT한다. SafeDefaults는 서버 content Town spawn과 `PlayerStats`의 class별 MaxHp에서 캡처한 두 클래스 후보이며 DB가 저장 class로 고른다. SQL은 content를 소유하지 않지만 caller의 범위·양수 제약을 검사한다. 기존 Progress가 있으면 전투 위치/HP/해금 값을 UPDATE하지 않는다. 기존 값은 진단 snapshot에만 남기고 반환 `safe`는 현재 content의 Town·풀HP·unlock=false로 계산한다. 이 차이가 자동 dirty flag를 만들지 않는다.

Authority Fence+1, Runtime/OwnerId/LastSequence=0으로 변경하고 Account/Character/Progress 일관 snapshot·두 token·성공 ledger를 같은 transaction에 넣어 commit한다. 중간 오류면 신규 세 게임 행·authority·증빙 모두 rollback한다. 기존 잘못된 ownership/class나 필수 identity 결함은 입장 실패다. 부분 생성·값 교정·Knight fallback은 없다.

### 5.2 WriteSafeCheckpoint — 제한된 mutation primitive

D1b는 fence/token/operation 계약을 반증할 수 있는 repository primitive를 정의한다. 이 API는 **주기 저장·매 logout UPDATE를 도입하는 허가가 아니다**. 현재 D0 흐름은 최초 생성/누락 Progress 처리를 acquire 안에서 마치므로 정상 close의 호출 수는 0이다. D2는 명시적으로 승인된 durable 필드 변경이 없는 DTO를 이 API로 보내지 않는다. 기존 legacy Progress를 정상화하는 caller도 추가하지 않는다.

허용 payload는 저장 class를 기준으로 한 Town·현재 content 안전 위치·기본 풀HP·해금false뿐이다. class 변경·다른 위치/rawHP/quest 데이터는 받지 않는다. current Runtime owner/fence, `Sequence=LastSequence+1`, Account/Character 소유, **ExpectedCharacterVersion**을 검사한다. Progress가 있으면 ExpectedProgressVersion 필수 일치, 없으면 missing token만 허용한다. 읽기 의존 Character row의 잠금을 commit까지 보유한다.

명시 요청의 safe 값과 현재 Progress가 다르면 Progress만 조건부 INSERT/UPDATE한다. 같으면 게임 DML 없이 `CheckpointNoChange`를 기록한다. 두 경우 모두 owner LastSequence와 terminal ledger는 원자 commit한다. UPDATE0을 성공으로 세지 않고 conflict로 처리한다. Character는 읽기 의존 token만 확인하고 불필요 UPDATE하지 않는다. 양쪽 게임 행 생성은 acquire의 원자 transaction이며, 장래 양쪽 UPDATE가 필요하면 양쪽 expected token 검사와 commit 후 양쪽 새 token 반환을 그대로 요구한다. 이번 API에 클래스 변경을 추가하지 않는다.

### 5.3 ReleaseRuntime / ReleaseRecovery

해당 종류의 current OwnerKind/OwnerId/Fence와 `Sequence=LastSequence+1`을 요구한다. owner는 진행 요청의 결과를 확정한 뒤에만 release를 만든다. 게임 데이터 저장이 없어도 release transaction은 필요하다. Free로 바꾸면서 Fence+1·OwnerId=NULL·LastSequence=0으로 만들고 Released ledger를 함께 commit한다. 게임 데이터에는 UPDATE하지 않는다.

기존 Released 증빙 replay는 당시 해제가 확정됐음을 뜻한다. 이미 새 owner가 있다면 그 owner를 해제하지 않으며 결과에 현재 수명 불일치를 표시한다. 같은 ID가 아닌 stale release는 StaleFence/Busy로 거부한다. runtime procedure로 Recovery owner를 해제할 수 없고 그 반대도 허용하지 않는다.

### 5.4 ResolveOperation — 조회와 취소 확정

결과 미확정 후 같은 ID·동일 typed payload로만 호출한다. runtime resolver는 kind1..3만, recovery resolver는 kind4..5만 받는다. query/reconcile도 동일 slot lock과 transaction을 쓴다.

- ledger가 있으면 payload를 비교하고 historical terminal 결과 + 현재 authority/token snapshot을 반환한다. ledger bytes만 보고 현재 입장/쓰기 권위를 새로 부여하지 않는다.
- ledger가 없고 단순 조회 모드이면 `StillUnknown`이다. “없음”은 이전 RPC가 DB에 아직 도착하지 않았다는 경우도 포함한다.
- owner가 그 요청을 더 실행하지 않기로 정한 **SealIfAbsent** 모드이면 같은 ID/payload의 `NotApplied/CancelledBeforeApply` 증빙을 INSERT하고 commit한다. 이것은 데이터 mutation 없이 그 요청을 최종 취소하는 기록이다. 늦은 원요청은 공통 ledger 검사에서 이 결과를 읽고 게임/authority를 변경하지 못한다.
- 원요청이 먼저 lock을 얻고 commit하면 resolver는 Applied를 본다. 원요청 rollback 뒤에는 seal을 기록할 수 있다. resolver가 먼저 seal하면 원요청은 실행되지 않는다. seal 응답까지 유실되면 다시 같은 resolver를 호출하며 unknown을 유지한다.

seal은 그 **한 OperationId**만 취소한다. owner의 다른 작업을 취소하거나 권위를 회수하지 않는다. 현재 owner가 바뀌었다면 old 작업의 historical 결과를 정산하고 `FenceLost`로 끝낸다. mutation이 seal로 NotApplied가 됐어도 그 payload를 새 ID/token으로 자동 재시도하지 않는다. 같은 수명의 순차 작업을 재개할지 닫을지는 D2 owner가 최신 권위와 확정 sequence로 판단한다. 실패 입장의 새 시도는 새 연결이다.

### 5.5 로컬 관리자 RecoverAndLoad

관리 도구는 target DB/slot/두 GUID, InspectRecovery에서 확인한 ExpectedFence·ExpectedOwnerKind·ExpectedOwnerId, 관리 사유, 신규 관리 OwnerId/OperationId를 명시한다. 운영자가 이전 runtime을 quiesce하고 대상과 이유를 확인해야 한다. process 부재·시간 경과 자체는 SQL 완료 증거가 아니다.

procedure는 관리 role에서만 실행된다. 같은 slot lock 안에서 **관찰한 owner/fence가 아직 같은지** 확인하고 Fence+1, Recovery owner/LastSequence=0으로 교체한 뒤 Character/Progress를 일관 load한다. 동시 정상 새 owner가 먼저 바뀌었다면 관찰값 불일치로 거부한다. 게임 값은 수정하지 않는다. 유효 Character가 없으면 존재하지 않는 상태를 일관 load한 `RecoveredAbsent`를 기록한다. 잘못된 ownership/class는 실패하며 자동 데이터 수선하지 않는다. missing Progress도 여기서는 diagnostic absence로 반환한다.

복구 결과는 관리자에게만 반환한다. Runtime Ready를 이 결과로 직접 열지 않는다. 관리 owner의 확인된 `ReleaseRecovery` 후 일반 새 연결이 새 fence로 AcquireAndLoad하며 필요한 missing Progress를 안전 초기화한다. recovery 또는 release가 unknown이면 관리 owner 상태를 유지하고 동일 operation resolver로 확정한다. 자동 시간 만료·앱 recovery key·TTL takeover는 없다.

회복 전에 발행되어 DB 진입을 기다리던 old acquire도 ExpectedFence가 과거이므로 거부된다. old mutation/release 역시 거부된다. 이 보호는 관찰한 old process의 종료 확인보다 DB fence/lock에 의존한다. 운영 tool의 화면/명령명·배포·Management 연동 구현은 D1b 이후 정확 범위가 배정될 때 작성한다.

## 6. repository 결과와 D2/D3 인계

`ICharacterRepository`의 비동기 연산은 `ReadAdmissionAsync`, `AcquireAndLoadAsync`, `WriteSafeCheckpointAsync`, `ReleaseAsync`, `ResolveOperationAsync`다. 관리자 API는 별도 `ICharacterRecoveryRepository`/connection factory로 분리해 GameSession 의존성에 넣지 않는다. 모든 호출은 immutable request와 monotonic absolute deadline, cancellation 요청을 받는다. 이름은 구현 시 코드 기준에 맞출 수 있으나 아래 의미는 유지한다.

| 결과/값 | 소비 계약 |
|---|---|
| `AdmissionProbe` | binding·Free/Held·Fence 관측. 권위/입장 성공 아님 |
| `Acquired` | commit 증빙 ID, current Runtime owner/fence, sequence0, stored class, safe projection, 복사한 두 token |
| `Durable` / `CheckpointNoChange` | 요청 증빙·확정 sequence·commit 후 token. 게임 UPDATE 여부를 따로 표시 |
| `Released` | 그 operation의 조건부 해제 commit 확정. 현재 다른 owner에 대한 권위 없음 |
| `Rejected` / `Conflict` | 상세 내부 reason·operation 확정 여부. stale DTO에 token만 바꿔 재시도 금지 |
| `NotDispatched` | command 호출 전 로컬 검증/예산/접수 실패. 이 호출은 DB에 보낸 적이 없음 |
| `AcquireUnknown` / `CommitUnknown` | command 진입 뒤 전체 terminal receipt를 받지 못함. lane freeze·동일 ID resolver 대상 |
| `OperationResolution` | historical outcome/snapshot와 현재 owner/fence/sequence/tokens를 분리. `StillUnknown`, `NotApplied`, `Applied`, `FenceLost`를 명시 |
| `Unresolved` / `RecoveryRequired` | 예산 내 결과 확정 불가 또는 잔존 owner. 성공·Released로 표시하지 않음 |

Acquired replay를 admission에 사용하려면 current Runtime owner/OwnerId/Fence가 같고 LastSequence=0 및 현재 두 token이 ledger snapshot과 같아야 한다. 하나라도 다르면 **historical Acquired일 뿐** 새 Ready를 만들지 않는다. Checkpoint 결과도 현재 owner/fence가 다르면 Durable 이력만 정산하고 후속 mutation을 막는다. late release 성공은 이력으로 받아도 새 owner의 상태를 덮어쓰지 않는다.

D2는 한 캐릭터당 순차 lane, 진행 작업 1개, 유한 대기 공간과 별도 final handoff 공간을 소유한다. `Accepted`는 이 owner가 책임을 인수했다는 뜻이고 Durable이 아니다. unknown 동안 후속 mutation/release/admission을 보내지 않는다. EOF·Host·map migration 0-owner에서도 immutable 요청/identity/class/safe context를 owner에게 인계하고 entity/party cleanup은 I/O를 기다리지 않는다. close의 `NoWriteNeeded`는 로컬 판정이며 SQL CheckpointNoChange와 다르다. 전자는 checkpoint RPC 자체가 0이고 조건부 release만 남는다.

D3의 `HandshakePending → Selecting → Loading → Resolved → Entering → Active`에서 repository 결과는 현재 session generation/closing/authority를 재확인한 후 적용한다. class를 `_stats`에 넣었다는 이유만으로 입장을 열지 않는다. 최초 intendedClass와 저장 class가 다르면 저장 class로 통일한다. `LoadResult(success,class) → EnterMap → full HP → roster` 및 기존 Ready barrier를 보존한다. 정확 packet ID·공개 status는 D3에서 확정하며 GUID/token/DB reason을 client에 보내지 않는다. 닫힌 세션의 늦은 create 성공을 폐기해도 owner는 생성 증빙과 release를 종결한다. 행 삭제·새 GUID 생성으로 되돌리지 않는다.

## 7. timeout·cancel·재시도

D1b 기본 상한은 Open 5초, command network timeout 5초, application lock 2초로 설정하되 각 호출의 **남은 절대 예산**으로 줄인다. 무한 timeout 0/-1은 사용하지 않는다. command 초 단위는 남은 시간 올림값과 상한 중 작은 값(최소1초), lock은 남은 ms와 2000 중 작은 값이다. 더 짧은 실제 deadline은 cancellation timer로 요청하되 cancel 완료를 SQL rollback으로 간주하지 않는다. D2 Host 전체 종료 deadline의 최종 수치는 별도 측정 대상이며 단계마다 5초를 새로 주지 않는다.

`CommandTimeout`은 전체 업무 deadline이 아니며 첫 row 이후에도 읽기 timeout이 날 수 있다. [SqlCommand 공식 문서](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlcommand.commandtimeout?view=sqlclient-dotnet-core-6.1) 따라서 result set 일부만 받았거나 DB exception/IO failure/cancel/process 종료가 command 진입 후 발생하면 보수적으로 unknown이다. 서버가 보낸 완전한 terminal rejection receipt 또는 seal commit 증빙이 있어야 NotApplied로 확정한다. SQL 오류 번호 하나만으로 “미실행”이라고 단정하지 않는다.

자동 command 재시도·driver retry provider를 쓰지 않는다. [SqlClient retry 기능](https://learn.microsoft.com/en-us/sql/connect/ado-net/configurable-retry-logic-sqlclient-introduction?view=sql-server-ver17)이 있어도 요청 의미를 보장하는 것은 이 ledger다. 로컬 pre-dispatch 실패는 예산 내 새 연결을 시도할 수 있으나 이미 보낸 command는 먼저 동일 ID로 조정한다. resolver의 동일 ID 반복은 허용하며 예산/시도 수를 기록한다. 운영 기본은 한 번 조회 후 필요 시 SealIfAbsent 한 번, 그 응답까지 미확정이면 Unresolved로 남긴다. 남은 예산 없이 해제를 억지 실행하지 않는다.

진행 task가 취소 요청에 즉시 응답하지 않으면 actor/Host는 ShutdownIncomplete와 operation 목록을 남기고 owner가 task/connection을 추적한다. “닫았다”는 이유로 pending 목록을 지우지 않는다. 다음 runtime은 잔존 owner가 있으면 RecoveryRequired다. 이 설계는 무손실 crash 복구나 강제 SQL 중단의 완료를 주장하지 않는다.

## 8. 구현 전에 실제 SQL로 확인할 가정

아래는 공식 자료/설계에서 도출한 **아직 미검증인 가정**이다. D1b 결과에는 각각 PASS/FAIL/미실행을 붙인다.

| 가정 | 필요한 실제 관측 |
|---|---|
| 6.1.7 + net10 + 현 Windows/WSL + SQLExpress 조합 | 두 OS의 runtime/package/native dependency·인증/TLS·typed RPC 성공 |
| 두 principal의 같은 application lock과 row lock이 모든 경로를 직렬화 | old-first/new-first 두 연결 barrier와 독립 관측 연결 결과 |
| execute-only role이 정상 RPC를 허용하고 직접 DML/recovery 우회를 거부 | 실 principal별 양성/음성 시험, legacy grant 조사 |
| atomic commit·ledger·seal이 응답 유실과 지연 도착을 견딤 | commit 전/후, DB 진입 전, release 응답 후 차단을 구분한 장애 주입 |
| codec/token/result snapshot이 원본 의미를 보존 | SQL golden vector·real/Guid/null/token alias·partial result 반증 |
| schema 추가가 001/기존 데이터/이력을 보존 | migration/catalog/재실행/실패 rollback·전후 row 비교 |

문서 실사는 이 계약의 일관성을 확인한다. 실제 권한·locking·rollback·durability·게임 복구를 대신하지 않는다.
