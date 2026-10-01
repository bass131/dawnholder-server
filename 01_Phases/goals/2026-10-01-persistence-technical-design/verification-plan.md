# D1a 독립 실사와 D1b SQL 반증 계획

[goal](goal.md)과 [technical-spec](technical-spec.md)을 기준으로 한다. **현재 수행 범위는 문서 실사뿐이며 아래 SQL/driver/게임 시험은 미실행**이다. 코드 구현 후 새로운 Opus가 요구사항과 실제 diff부터 검토하고 독립 테스트를 작성·보완·실행한다. 구현자의 smoke 결과만으로 PASS하지 않는다.

## 1. D1a 문서 완료조건

신규 외부 `claude-opus-5-5` 검증자가 작성 종료 후 다음을 실사한다. 제품/SQL/test 파일 쓰기·DB 접속·build·비밀 읽기·추가 위임·commit/push를 하지 않는다. 판정은 `.backups/verification/2026-10-01-persistence-technical-design/verification-N/verdict.md`에 원문으로 남긴다.

| 검토 | 반증 기준 |
|---|---|
| D0/P0 추적 | 저장 class·Town풀HP·session quest·중복 Ready 보존·0-owner close·한 deadline을 바꾼 문장이 있는가 |
| 실제 코드/001/설정과 대조 | 없는 .NET repository를 완료로 서술하거나 ODBC probe/문서 검토를 runtime 성공으로 쓰는가 |
| driver/출처 | 6.1.7의 공식 존재·지원표·확인일과 net10/OS 실측 구분이 정확한가 |
| 테이블/필드 | 각 필드 이유·키·CHECK·absence·보존·기존 001와 다중행 보존을 구현자가 추측해야 하는가 |
| fence/atomicity | 미존재 생성부터 모든 writer/resolve/recovery가 같은 경계를 쓰는가. 검사와 commit 사이 빈틈이 있는가 |
| ledger/unknown | ID/payload 의미, 늦은 DB 도착, seal 경쟁, historical success와 현재 owner를 혼동하는가 |
| API/소유/권한 | runtime direct DML·legacy login·관리 recovery 우회, mutable token alias, sequence/배열/부분 receipt 처리가 빠졌는가 |
| 시험 계획 | 양성·음성·두 연결 순서·각 fault 지점·독립 관측·격리/cleanup이 구체적인가 |

판정은 PASS 또는 결함 번호(D1A-01...)·심각도·문서 위치·실패 순서·필요 수정으로 남긴다. 조건부 결함을 남긴 PASS는 사용하지 않는다. 수정은 Astra가 문서만 수행하고 **새 검증자**로 재검증한다. 같은 결함 번호가 3번 재검증에 실패하면 메인에 보고한다. PASS는 기술 계약의 정적 타당성 판정이며 SQL/권한/durability PASS가 아니다.

## 2. D1b 실행 gate — 지금은 미선택·미승인

메인이 D1b 시작 시 아래 표의 정확한 값을 확정한다. `Dawnholder_Dev_<suffix>`의 새 격리 DB를 추천하지만 이름을 추천했다는 이유로 만들거나 접속하지 않는다. Northwind·BaseballGame은 연습 DB로 전 과정에서 제외한다. 기존 `Dawnholder_Dev`는 게임용이며 자동 초기화/cleanup 대상이 아니다.

메인이 전달한 추가 사용자 결정 `msg_dac79ea2b268`에서 GameDB도 연습용임을 확인했다. 제외 목록은 **GameDB·BaseballData·Northwind**이며, 이전 전달의 `BaseballGame`은 BaseballData를 가리킬 가능성이 있으나 D1a에서 실제 이름을 조회하지 않았다. 이름 추정으로 대상을 넓히지 않으며 **D1b에서 정확히 선택한 Dawnholder DB 외에는 모두 제외**한다. 실제 catalog 이름은 D1b 실행 gate에서 확인해 기록한다.

| 확정할 값 | gate에서 남길 기록 |
|---|---|
| SQL endpoint·DB·DB 소유 marker | 서버/instance/포트·DB_NAME·버전·collation·RCSI·migration hash, 승인 message와 조회 시각 |
| 실행자와 시간 창 | 한 SQL executor, 사용할 Windows/WSL process·코드 commit, 동시 writer 부재 |
| runtime/recovery/test observer principal | runtime 최소권한, 실제 recovery용 전용 비-sysadmin Windows principal의 dh_recovery 단독 매핑과 group 상속 권한, 별도 observer를 구분. 기존 관리자 sysadmin으로 recovery 시험을 대체하지 않음. 전용 Windows 계정·SQL login/user/role·실행 launcher/자격증명 접근 제어의 생성·설정은 별도 정확 범위 승인. legacy direct-DML/의도하지 않은 사용자 매핑이 없는지 |
| TLS·secret 전달 경로 | secret 값 없는 설정, 인증서 검증 여부와 예외 승인. 기존 DPAPI 파일을 읽을 주체/launcher |
| singleton 바인딩 | 시험 전용 AccountId/CharacterId·slot1·초기 Fence0/Free, 재시도에도 동일 GUID. 기존 행 채택 시 소유/class 사전 검사 |
| 허용 side effect | 002+ DDL·실제 commit·시험 행·권한 음성 시도·task-owned 연결 차단·정리 범위 |
| 격리 fixture 수명 | 재생성/삭제할 정확한 시험 행 또는 시험 DB allowlist. 기존 게임 행이면 파괴성 fixture 시험을 다른 승인 격리 DB로 이동 |
| 증거·정리 책임 | 로컬 결과 경로·operation/owner/GUID manifest·정리 executor·보존할 미확정 목록 |

정확 DB/권한/격리가 미확정이면 SQL 실행을 멈추고 메인에 값을 요청한다. fake 성공이나 기존 ODBC 시험으로 빈칸을 채우지 않는다. D1b 제품/테스트/실행 소유 범위는 별도 goal에서 확정한다. 이 문서는 서버 service 재시작·backup restore·타 process 종료·전역 SQL/방화벽/인증 정책 변경을 허용하지 않는다.

## 3. 공통 시험 장치와 판정 증거

- 실행 전 `dotnet --info`, resolved package/version, OS/architecture·native dependency, schema catalog와 git SHA를 기록한다. Windows build의 DLL 자동 복사 부작용은 DEVELOPMENT를 따라 차단한다. driver library와 실제 package hash를 남기고 connection secret은 제외한다.
- 이 머신의 Windows .NET 실행은 ADR-029의 Smart App Control 제약이 있을 수 있다. 막히면 전역 정책을 바꾸지 않고 정확 명령/오류·Windows 미실행을 기록한다. WSL 성공으로 Windows PASS를 대신하지 않으며 D1b 완료의 허용 실행 환경은 메인이 gate에서 정한다.
- 실제 경쟁 주체는 **독립 SqlConnection A/B**다. 필요한 barrier 제어 연결과 읽기 전용 관측 연결은 별도로 둔다. 한 connection/transaction을 공유해 동시성을 흉내 내지 않는다. 각 connection의 session ID·client connection ID·operation ID·owner·fence·sequence를 기록한다.
- 테스트는 TaskCompletionSource/외부 barrier 또는 실제 row lock 대기로 순서를 제어한다. 임의 sleep 후 “먼저 실행됐을 것”이라고 판정하지 않는다. event timestamp는 설명용이며 lock/fence/proof가 판정 근거다.
- DB lock 획득 뒤 변경 전 정지는 제어 연결이 대상 행 lock을 보유하게 해 실제 procedure가 application lock을 가진 채 row lock에서 기다리도록 구성한다. 관측 권한은 시험 관리자에게만 준다. runtime에 DMV/ALTER 권한을 추가하지 않는다. 단계 위치를 관측하지 못했으면 그 시나리오는 미실행이다.
- commit 후 응답 유실은 실제 procedure/transaction을 그대로 둔 채 test-owned wrapper가 정상 완료 receipt를 받은 뒤 caller 전달을 버리는 경우와, task-owned network/proxy 경계에서 연결을 끊는 경우를 구분한다. 전자는 결정적인 repository 조정 시험이며 후자의 driver/transport 손실 검증을 대신하지 않는다. 실제 commit 여부는 독립 connection의 ledger로 확인한다.
- production procedure에 테스트 WAITFOR/우회권한을 배포하지 않는다. 특별 계측 procedure를 썼다면 실제 procedure와 동일 구현이라는 근거·차이를 남기고 production SQL에 대한 확인 범위를 별도로 표시한다.
- 각 시나리오에서 before/after의 세 게임 행·Authority·Operation·두 token을 읽는다. 결과 row 수/값·fence·sequence·ledger ID/payload/outcome과 잠금 순서가 기대와 모두 일치해야 PASS다. 예외 타입이나 process 종료만으로 rollback/durable를 단정하지 않는다.

## 4. schema·권한·driver 시험 행렬

| ID | 자극 | 필요한 관측 |
|---|---|---|
| S01 | 001만 설치된 새 승인 fixture에 002+ 적용 | 001 파일/checksum·게임 행 불변, 단일 batch 파일별 함수/procedure 설치, 기존+신규 catalog 검증, 테이블/각 필드/PK/FK/CHECK/role/grant 존재, 바인딩 자동 생성 없음 |
| S02 | migration 재실행·중간 오류 주입 | 두 번째 실행 무변경, 부분 schema/SchemaVersion 기록 없음. rollback으로 rowversion 전역 counter가 안 변한다고 단정하지 않음 |
| S03 | slot!=1·empty GUID·잘못된 owner/null 조합·음수 fence/sequence·잘못된 outcome/code·invalid JSON | 제약 또는 procedure가 거부. app principal로 직접 INSERT를 허용해 음성 fixture를 만들지 않음 |
| S04 | 승인 초기화로 fixed binding 등록, 다른 GUID config 시작 | binding 정확히1행, mismatch는 시작/입장 실패. 임의 Character 첫 행 채택·바인딩 UPDATE 없음 |
| S05 | runtime 정상 RPC 및 직접 SELECT/INSERT/UPDATE/DELETE/DDL·관리 RPC 시도 | 개별 정상 RPC 양성, 테이블/관리 경로 음성. legacy grants/role 상속·의도하지 않은 DB 사용자도 조사. public applock 점유 방해는 가용성 위험이며 takeover 없이 실패하는지 확인. 권한 오류 로그에 secret 없음 |
| S06 | 실제 recovery용 전용 비-sysadmin Windows principal로 통합 인증 후 runtime principal과 비교 | 실제 login·group/role 상속 확인, 관리 RPC 양성 및 직접 DML/DDL/runtime RPC 음성을 DB에서 확인. runtime resolver kind4/5 위장 거부. 도구의 허용 RPC·DB/slot/GUID 외 입력 거부도 확인. 기존 sysadmin 계정 결과로 대체하지 않음 |
| S07 | Windows·WSL 각각 6.1.7/net10 실제 연결·RPC | package/native dependency·TLS·인증·대상 DB 확인. 인증서 오류는 실패하며 자동 trust/암호화 downgrade 없음 |
| S08 | 잘못된 DB/schema/binding/endpoint·secret 누락 | 입장 gate 닫힘, 다른 DB/메모리 fallback 없음. 비밀·원문 connection string 출력 없음 |
| S09 | SQL codec golden vectors 및 모든 payload field를 하나씩 변경 | Guid byte order·real·null/8-byte token·유효 class·긴 reason·길이 동일/다름 비교 정확. NaN/±Inf 거부·-0 정규화, JSON style3 좌표 fresh/replay roundtrip 확인. 같은 ID의 의미 변경 전부 거부. 엔진/patch 변경 전 정산·변경 후 vector gate도 확인 |
| S10 | ambient/nested transaction, implicit-transactions 설정을 가진 연결로 호출 | @@TRANCOUNT>0 접수 거부. count0 진입에서는 implicit off로 자체 transaction 하나만 열고 정상 반환 시 count0. 외부 commit 전 Durable을 반환하는 경로 없음 |
| S11 | 동일 endpoint/credential로 서로 다른 짧은 Open deadline 사용 | 연결 문자열 Connect Timeout=5 유지, deadline별 pool 생성 없음. 취소 요청 뒤에도 진행 Open task를 유실하지 않고 종료 추적 |

## 5. 생성·load·token·operation 시험 행렬

| ID | 자극 | 필요한 관측 |
|---|---|---|
| C01 | 게임 행 없음, Knight/Mage 각각 첫 acquire | 고정 GUID Account/Character/Progress 각1행·저장 class·safe값·Fence1/Runtime·sequence0·Applied ledger 원자 commit. class별 fixture reset은 승인된 시험 행에서만 |
| C02 | 같은 Free fence의 서로 다른 두 acquire 동시 발행 | 하나만 Acquired, 나머지 Busy/StaleFence. Character가2개 되지 않음. 같은 ID/동일 payload 동시 재요청은 하나의 증빙을 공유 |
| C03 | 다른 CharacterId/AccountId, 또는 Checkpoint class≠저장 class | 전자는 IdentityMismatch(203), 후자의 유효 class는 Conflict(202), 범위 밖 class는 InvalidClass(204). 기존 무관한 Character 보존. Acquire의 반대 intendedClass 수용은 C05 |
| C04 | Account INSERT 후·Character INSERT 후·Progress INSERT 후 실패 | 승인된 격리 fixture에만 각 테이블의 시험 AFTER INSERT trigger가 THROW하도록 하나씩 설치해 실제 production procedure의 중간 실패를 주입하고 즉시 정리한다. row-lock barrier만으로 이 지점에 닿았다고 하지 않음. 세 게임 행·authority·Applied 증빙 부분 commit 없음. 동일 ID absent는 unknown이며 seal로 최종 NotApplied |
| C05 | 기존 Character와 반대 intendedClass, missing Progress | 저장 class 유지, 해당 class의 safe Progress만 INSERT, Character token 불변·Progress token 존재 |
| C06 | 기존 combat Progress/낮은HP/unlock=true로 acquire→close | 반환 safe는 Town풀HP/unlock=false, 기존 게임 행·두 token은 그대로. diagnostic과 runtime projection 구분, logout 게임 UPDATE0 |
| C07 | stale CharacterVersion만, stale ProgressVersion만, missing/present 불일치 | 각각 Conflict, 게임 DML·owner sequence 변화 없음, NotApplied 증빙. stale DTO에 token 교체 재전송 안 함 |
| C08 | Progress-only 요청 도중 관리 writer가 Character 의존 값을 변경하려 함 | 같은 slot 경계에서 앞/뒤로 직렬화, 검증과 commit 사이 변경 불가. 앞선 변경이면 Character token conflict. class 변경 fixture는 시험 전용 관리자에서만 |
| C09 | 유효 fence/두 token으로 명시 safe mutation, 이어 같은 값 요청 | 첫 요청 Progress token만 변경, 두 번째는 CheckpointNoChange·게임 DML0. 각각 sequence/ledger 확정. 이 API를 평소 close가 호출하지 않음 |
| C10 | 접수 직후 원본/반환 byte[] 변경 시도 | queued request·SQL expected token·저장된 snapshot 불변. accessor alias가 없고 원본과 token 비교가 뒤틀리지 않음 |
| C11 | 같은 ID로 class/token/owner/fence/sequence/default/reason/kind 중 하나 변경 | OperationPayloadMismatch 또는 preflight 거부, 기존 ledger/게임/authority 보존 |
| C12 | 유효 owner에서 seq 누락/중복/역순, fence overflow fixture | SequenceMismatch 또는 IntegrityFailure, overflow 재사용 없음. 동일 ID replay는 seq 재증가 없음 |
| C13 | current owner·Character token은 유효하고 Progress만 없는 승인 fixture에 Checkpoint | ExpectedProgressVersion missing일 때만 safe INSERT·sequence/proof 원자 commit, present token이면 Conflict. fixture 생성/정리는 관리자가 동일 경계·시험 GUID에서만 수행 |

## 6. old/new 두 연결 순서와 unknown 반증

`A`는 old operation, `B`는 new acquire 또는 명시 관리자 recovery다. active old owner를 일반 B acquire가 빼앗는 시험으로 바꾸지 않는다. 정상 new acquire 양성은 old release commit 뒤, 강제 교체 양성은 관리 recovery로만 만든다.

| ID | barrier와 교차 순서 | 필요한 관측 |
|---|---|---|
| O01 | A가 slot lock 후 UPDATE 전 대기 → B RecoverAndLoad 요청 → A commit 허용 | B는 기다렸다가 A의 확정 safe값/token과 함께 새 fence를 load. old-first 증명 |
| O02 | O01에서 A rollback | B가 부분값을 읽지 않음, A Applied 없음. B의 fence/load만 확정 |
| O03 | A mutation을 DB 진입 직전에 정지 → B recovery commit → A 실행 | A StaleFence, 게임 행 보존. old process/task가 살아 있어도 쓰기 차단 |
| O04 | A stale release를 정지 → B recovery·관리 release·new runtime acquire → A 실행 | A가 새 owner를 Free로 만들지 못함. B owner/값/token 유지 |
| O05 | A acquire가 Free fence F를 probe한 뒤 DB 진입 전 정지 → B recovery+관리 release → A 실행 | A의 F는 거부, Account/Character 생성 없음. 새 연결의 현재 fence acquire만 가능 |
| O06 | active old Ready 상태에서 B 일반 acquire | B 거부, old owner/Ready/게임 동작 유지. 시간 경과·process 부재 표식으로 takeover 없음 |
| O07 | A 정상 release commit → B 새 probe/acquire | 새 fence와 저장 class/load, 과거 Released replay가 B owner를 해제하지 않음 |
| U01 | acquire/create commit 뒤 caller receipt를 버림 | AcquireUnknown·Ready0, 동일ID resolver로 Applied 증빙 확보. same GUID1행. 늦은 UI 취소/세션 폐기로 row DELETE 없음 |
| U02 | checkpoint commit 뒤 응답 유실 | CommitUnknown·후속 작업0, ledger와 commit 후 token으로만 Durable 확정. 같은 값/SavedUtc/rowversion 증가로 추정하지 않음 |
| U03 | release commit 뒤 응답 유실, 그 사이 B acquire | Released history 확정, 현재 B owner는 별도 관측. A가 B를 해제하거나 Ready로 재진입하지 않음 |
| U04 | A를 DB 진입 전에 멈춤 → resolver ReadOnly absent → A 실행 | absent를 성공/NotApplied로 확정하지 않았음. A 이후 상태를 다시 증빙으로 판정 |
| U05 | U04의 resolver가 SealIfAbsent commit → A 실행 | A는 CancelledBeforeApply ledger replay, 게임/authority 변경0. 늦은 요청 차단 증명 |
| U06 | A가 lock 보유 중 → SealIfAbsent 요청 → A commit 또는 rollback 각각 | commit이면 Applied를 반환, rollback이면 seal NotApplied. 둘 다 같은 ID에 결과1개이며 교차/부분결과 없음 |
| U07 | seal commit의 응답 유실 → 동일 resolver 반복 | unknown 유지 후 NotApplied 확정, 원요청이 재도착해도 변경0 |
| U08 | 과거 Acquired ledger replay 시 owner/fence/sequence/token이 각각 바뀐 경우 | historical success만 반환, 현재 Acquired/Ready로 사용 불가. 순서0·동일owner·동일token인 정상 replay만 복구 가능 |
| U09 | command timeout·cancel·network fault를 commit 전/후 각각 주입 | 전체 receipt 없으면 unknown, sql exception/cancel을 rollback 증거로 사용하지 않음. 독립 SQL 관측으로 실제 결과 대조 |
| U10 | result 첫 row 뒤 transport 실패/불완전 JSON·token 수신 | 부분 result로 Durable 반환0, operation 추적 유지·resolver 또는 Unresolved |
| U11 | deadlock/lock timeout/connection open 실패/남은 예산0 | lock 음수 rollback 처리, 열린 transaction/재사용오염 없음. command 전 NotDispatched와 command 뒤 Unknown 구분 |

## 7. recovery·D2/D3 소비 시험 인계

| ID | 자극 | 필요한 관측·소유 단계 |
|---|---|---|
| R01 | 관리 Inspect 이후 다른 owner가 먼저 변경 | expected owner/fence mismatch, 새 정상 owner를 회수하지 않음(D1b) |
| R02 | 관리 recovery의 fence 교체+load와 old mutation 교차 | 동일 경계·일관 snapshot·ledger commit. recovery result가 client Ready로 바로 쓰이지 않음(D1b/D3) |
| R03 | 아직 Character 없음 또는 Progress 없음 | RecoveredAbsent/진단 absence, 게임 행 자동 수선 없음. 관리 release 뒤 정상 acquire가 최초/누락 Progress를 안전 생성(D1b) |
| R04 | recovery/recovery release 응답 유실, runtime이 대신 해제/resolve 시도 | runtime 거부. 관리 same-ID resolver로만 확정, timeout/TTL 자동 해제 없음(D1b) |
| R05 | Recovery held 중 runtime Acquire, Runtime held에 ReleaseRecovery | 각각 Busy(200), 기존 owner/fence/게임 데이터 보존. stale fence를 함께 주면 우선 StaleFence(201)(D1b) |
| L01 | EOF/명시 close/Host/0-owner/destination close skip | safe immutable 인계→entity/party cleanup 유한 종결, 이후 owner가 operation/release 추적. 게임 틱 DB 대기0(D2) |
| L02 | durable 변경/진행 작업 없는 close | local NoWriteNeeded, checkpoint RPC0·게임 UPDATE0, 조건부 release 성공 또는 명시 unknown(D2) |
| L03 | full queue·final slot·DB 지연·늦은 ack | Accepted와 Durable 구분, 유한 접수/cleanup, 같은 전체 deadline을 사용, ShutdownIncomplete 목록 유지(D2) |
| L04 | create commit 뒤 session generation 교체/취소 | 결과가 새 actor에 적용되지 않아도 owner가 old operation/release 정산. 행 삭제/새GUID없음(D2/D3) |
| E01 | load barrier 중 gameplay 입력과 중복 class 선택 | move/attack/skill/portal/party/cheat 거부, DB작업/entry1회. 이후 정상 양성(D3) |
| E02 | 저장 Knight/Mage와 반대 메뉴 선택·scene/class/HP 순서 교차 | class 권위 전 local spawn0, 결과 이후 stats/입력/HUD 일치. LoadResult→EnterMap→HP→roster·Ready 보존(D3) |
| E03 | 잘못된 class·성공 없는 EnterMap·old generation 결과 | 현재 오류는 종료, old 결과는 새 연결을 닫지 않음. Knight fallback으로 결함 숨기지 않음(D3) |
| E04 | 실제 commit 뒤 reconnect/process restart | 같은 Account/Character GUID, 저장 class·Town풀HP·quest/unlock 미복원. runtime EntityId 숫자 재사용과 영속 identity를 구분(D4) |

L/E 항목은 D1b 전체 통과라고 보고하지 않는다. D1b는 API/fake로 소비 계약 일부를 확인할 수 있고 실제 Host/Unity/DB 통합은 각 단계에서 별도 실행한다. 두 게임 행 UPDATE의 확장 기능도 이번 범위에 넣지 않으며 양쪽 생성 원자성·양쪽 token 반환은 C01/C04로 확인한다.

## 8. 종료·정리·결과 보고

1. 새 요청을 멈추고 모든 task-owned 연결/operation ID를 목록화한다. 진행 중 task·transaction·Unresolved를 숨기지 않는다. 프로세스를 죽였다는 이유로 SQL rollback을 완료 처리하지 않는다.
2. confirmed current owner는 같은 규약의 release로 정산한다. 미확정 owner는 승인된 local recovery 경로로 다루거나 증빙과 상태를 보존해 메인에 남긴다. cleanup 전에 old 연결의 소멸/차단과 slot 경계를 확인한다.
3. 반복 fixture가 필요하면 **승인된 시험 전용 GUID·DB에 한해** 관리자가 동일 slot lock·Free 상태를 확인한 뒤 Progress→Character→Account 순서로 시험 행을 정리한다. binding/Fence/Operation은 유지하고 다음 case는 새로운 OperationId/OwnerId를 쓴다. 게임 최초 class 불변의 예외인 파괴적 fixture reset임을 명시하며 기존 게임 데이터에는 적용하지 않는다.
4. ledger·binding 전체 제거는 모든 시험 process/연결이 정산된 뒤 **정확한 시험 전용 DB 폐기 승인이 있을 때만** 한다. DB명 prefix만으로 선택하지 않는다. 미확정 요청을 재실행할 수 있는 동안 증빙을 삭제하지 않는다. 실제 게임 DB의 증빙 자동 정리는 범위 밖이다.
5. 임시 권한/시험 장치/로그·process/포트 잔여와 정리 결과를 기록한다. 대상 밖 DB·SQL service·다른 process·001·유저 Unity 설정·stash는 보존한다.

결과 원문에는 ID별 PASS/FAIL/미실행, 정확 명령·시각·환경·commit 여부·관측 행/token/proof·fault 주입 위치·남은 자원을 남긴다. fixture 실행 실패와 제품 결함을 구분한다. 문서 PASS / fake PASS / 실제 SQL PASS / 실제 Unity·재접속 PASS / CI / PR 병합은 각각 별개다. D1a 원문을 메인이 직접 읽고, PR 병합은 해당 PR에 대한 사용자 명시 승인 후에만 수행한다.
