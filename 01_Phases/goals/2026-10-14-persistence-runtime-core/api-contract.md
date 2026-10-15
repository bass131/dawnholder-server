# 저장소 결과와 저장 작업 공통 API 계약 v1

기준은 [goal](goal.md)의 사용자 범위와 [D1a 6·7절](../2026-10-01-persistence-technical-design/technical-spec.md#6-repository-결과와-d2d3-인계)이다. 계약 문서 writer는 Core 리드 한 명이다. 아래 이름·의미·책임·시험 경계는 먼저 고정한다. 구체 constructor/파일 분할은 구현 전 작성자 메모에서 아래 불변식을 보존해 선택하며 의미 변경은 리드에 돌려준다. SQL wire codec/driver/실제 게임 접속은 이 계약의 구현 대상이 아니다.

## 공통 값과 소유

Namespace는 Dawnholder.Server.GameServer.Persistence다. Shared.Protocol.CharacterClass(Knight0/Mage1)를 재사용하고 새 클래스 enum을 만들지 않는다. PlayerSnapshot 전체를 persistence DTO로 받지 않는다.

| 계약 이름 | 필수 의미/검증 |
|---|---|
| PersistenceIdentity | SlotId=1, 비어 있지 않은 AccountId·CharacterId. 런타임 EntityId/SessionId와 구별하며 constructor에서 불변 값을 확정한다. 자동 GUID/slot 생성·다른 주체 fallback 없음 |
| PersistenceAuthority | Identity, OwnerKind(Free/Runtime/Recovery), OwnerId, Fence, LastSequence. Free는 owner 없음/sequence0, Held는 유효 owner/Fence>0. Fence/sequence 음수·overflow 거부. owner/fence/sequence를 한 묶음으로 비교 |
| PersistenceToken | 정확8bytes의 불변 값. 입력 및 외부 반환 배열을 복사하고 외부 alias mutation으로 내부가 바뀌지 않음. null progress token과 영(0) token을 구별. SQL byte order 변환은 이번 범위 밖 |
| SafeCheckpoint | 저장 CharacterClass와 Town(map0)·유한 Single x/y·양수 MaxHp, HP=MaxHp·bossUnlocked=false. raw 전투HP/맵/quest/class 변경을 받지 않음. 안전 좌표/기본HP의 실제 content 연동은 caller의 후속 책임 |
| PersistenceDeadline | 주입한 TimeProvider의 monotonic timestamp 도메인에서 한 절대 마감. 같은 owner/작업의 단계가 남은 예산을 공유하며 UTC/wall clock 경과로 계산하지 않음. 테스트는 제어 가능한 시간을 사용 |
| OperationIdentity | 비어 있지 않은 OperationId, Kind, 고정 identity/owner/expected fence/sequence 및 typed payload. resolver는 동일 ID와 동일 payload를 유지. token만 바꾸거나 새 ID로 자동 mutation 재시도 금지 |

필드의 자료형은 GUID=Guid, Fence/Sequence=long, SlotId=byte, 좌표=float, HP=int, class=기존 enum이다. 요청·결과 및 중첩 배열/목록은 외부 변경과 분리한다. constructor의 명백한 불변식 위반은 ArgumentException 계열로 즉시 거부한다. 실행 중 예산/큐 접수 실패는 아래 NotDispatched/접수 결과로 다룬다. 설정·비밀·connection·GameSession 객체를 DTO에 보관하지 않는다.

## 저장소·복구 API — 작성자 A

ICharacterRepository의 비동기 연산명은 ReadAdmissionAsync, AcquireAndLoadAsync, WriteSafeCheckpointAsync, ReleaseAsync, ResolveOperationAsync다. 각 연산은 해당 immutable request, PersistenceDeadline, CancellationToken을 받아 Task 기반 결과를 반환한다. 구체 request/result 타입은 연산명과 연결해 이름 짓고 아래 표의 의미를 빠뜨리지 않는다. 하나의 boolean 성공값이나 null로 서로 다른 결과를 합치지 않는다.

| 연산 | 요청의 핵심 | 결과의 핵심 |
|---|---|---|
| ReadAdmissionAsync | identity | AdmissionProbe 또는 AdmissionReadFailed/NotDispatched. probe에는 관측 authority와 schema/binding 적합 여부; acquire 발행이나 현재 입장 권위가 아님 |
| AcquireAndLoadAsync | operation, 관측 expected fence, intended class, 안전 기본값 | Acquired 또는 Rejected/Conflict/NotDispatched/AcquireUnknown. acquired는 historical receipt와 current authority·stored class·safe·character/progress token을 구별 |
| WriteSafeCheckpointAsync | 현재 Runtime authority, 다음 sequence, operation, expected 두 token, safe payload | Durable/CheckpointNoChange 또는 Rejected/Conflict/NotDispatched/CommitUnknown. 게임 UPDATE 여부·확정 sequence/commit 후 token을 드러냄 |
| ReleaseAsync | 현재 Runtime authority, 다음 sequence, operation | Released 또는 거부/미발행/unknown. historical Released는 새 owner를 해제할 권리가 아님 |
| ResolveOperationAsync | 원 operation ID/typed payload, QueryOnly 또는 SealIfAbsent | StillUnknown/NotApplied/Applied/FenceLost. historical outcome와 current authority/tokens를 분리. ledger없음 QueryOnly는 NotApplied가 아님 |

관리자 계약 ICharacterRecoveryRepository는 InspectRecoveryAsync, RecoverAndLoadAsync, ReleaseRecoveryAsync, ResolveRecoveryOperationAsync를 별도로 제공한다. Recover는 관측 owner kind/ID/fence, 신규 관리 owner/operation,1~128자 사유를 받고 현재 owner가 바뀌면 거부한다. recovery 결과에는 diagnostic stored state/absence만 있으며 Runtime Ready나 SafeDefaults를 만들지 않는다. 이번 구현은 interface/immutable 값/입력·결과 불변식뿐이다. DB 주체·connection factory·관리 도구/권한 실행은 만들지 않는다.

Acquired 이력의 재사용은 current Runtime owner/ID/fence 일치, current sequence0, snapshot의 두 token과 current token 일치 때만 가능하다. 현재 권위가 바뀌면 이력만 정산한다. 필드 간 모순·잘못된 class/token·부분 receipt는 성공으로 만들지 않는다. 외부 adapter가 command 진입 뒤 완전한 receipt를 못 받으면 unknown이어야 한다. A는 실제 SQL adapter 없이 이 결과 표현/검증 경계만 구현한다.

## 저장 작업 핵심 — 작성자 B

CharacterPersistenceOwner는 한 고정 캐릭터와 Runtime 수명, 주입 ICharacterRepository·TimeProvider, 양수 유한 queue capacity를 소유한다. 실제 GameSession/World/Host는 이번에 연결하지 않는다. PersistenceWorker가 별도 책임으로 필요하면 해당 owner의 task 실행/종결만 맡고 다른 authority를 소유하지 않는다. 불필요한 범용 scheduler/확장 hook은 만들지 않는다.

- 접수 API는 TrySubmit 계열로 즉시 반환한다. Accepted는 immutable 작업/종결 책임 인수이며 Durable이 아니다. full/closing/frozen/invalid/expired는 구별해 거부하고 repository 호출0을 보장한다. caller는 선택적으로 완료 ticket을 기다릴 수 있으나 접수 경로가 I/O를 동기 대기하지 않는다.
- 한 lane에 repository 진행 작업은 최대1개다. 유한 대기 공간과 별도의 final close 인계 공간을 둔다. 같은 sequence의 중복 발행·큐가 찼다는 이유의 final 데이터 유실·진행 task 추적 유실을 금지한다.
- CloseCaptureTicket은 identity/수명·class/safe context·진행 operation·변경 필요 유무를 불변으로 인계한다. 같은 수명 close는 idempotent하고 다른 수명 티켓은 거부한다. 게임 entity/party 상태 참조나 I/O 대기를 필요로 하지 않는다. 실제 EOF/Host/map0-owner 콜백 연결은 후속이다.
- 기본 정상 close는 승인된 durable 필드 변경이 없어 NoWriteNeeded다. checkpoint RPC0이며 기존 authority를 종결할 조건부 release는 필요하다. 이 API의 존재로 주기 저장·매 logout UPDATE·legacy Progress 정상화를 도입하지 않는다.
- 명시적인 허용 checkpoint 작업이 있을 때만 WriteSafeCheckpoint를 발행한다. sequence 증가·token 교체는 확정 결과 뒤에만 한다. 오래된 payload에 새 token만 넣어 재시도하지 않는다.
- AcquireUnknown/CommitUnknown 또는 발행 뒤 task fault/cancellation에는 lane을 freeze한다. 후속 mutation/release/admission을 발행하지 않고 동일ID resolver로 조정한다. 기본은 QueryOnly1회, 필요하면 SealIfAbsent1회; 여전히 미확정이면 Unresolved다. 무한 루프/자동 새operation/시간만으로 성공 간주는 금지한다.
- historical Applied/Durable/Released와 current authority가 다르면 이력 정산 후 FenceLost/RecoveryRequired로 끝내고 새 owner 상태를 덮어쓰지 않는다. late 성공은 폐기하더라도 추적·정산을 유지한다.
- CloseAsync 계열은 Task를 반환하고 주어진 한 absolute deadline의 잔여만 사용한다. 취소 요청은 rollback·release 증거가 아니다. 마감에 미완료 operation 목록/ShutdownIncomplete 또는 RecoveryRequired를 남기며 진행 task 아래 자원을 먼저 dispose하거나 목록을 비우지 않는다.

정책 수치(대기 capacity, 전체 close 시간)는 유효한 명시 인자로 받으며 이번 goal에서 전역 운영 기본값을 임의 확정하지 않는다. 메모리 대역은 Tests 전용이고 운영 fallback이 아니다.

## 선행·독립 시험 경계

1. 값 불변식/token alias, invalid identity/class/safe/owner/fence/sequence·overflow, runtime/recovery API 분리.
2. 역사 receipt/current authority 분리와 현재 sequence/token 불일치. DB 밖에서 확인 불가능한 commit/locking은 통과로 주장하지 않는다.
3. 접수 nonblocking, 진행1/queue상한/final공간, unknown freeze·동일ID·query/seal 순서·예산 초과.
4. NoWriteNeeded checkpoint0/release1, closing 반복, 늦은 응답/취소/멈춘 task에서 미확정 정산과 추적 보존.
5. 정상 성공·거부 양성을 함께 실행하고 실제 새 API 진입을 호출한다. fake 자신의 로직만 시험하거나 제품 계산을 기대값으로 복제하지 않는다.

A/B는 이 문서의 의미를 독자 변경하지 않는다. 신규 Opus 선행 시험 작성자는 정확 API 표면을 시험 계약으로 구체화하고 그 표면을 리드가 A/B 계약에 동일하게 붙인다. 제품 파일은 쓰지 않으며 구현 전 실패를 기록한다. PR/독립 판정 묶음은 사용자 A(msg_be066f877900)로 한 PR·두 구현자 뒤 독립 강 판정 한 번으로 승인됐다. 이 계약은 실제 SQL/Host/게임/재접속 복원 완료의 증거가 아니다.
