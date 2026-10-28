# World의 Core 소비 접점 v1

- 버전 **1.0.0 / 서버 선행 준비 입력**. [승인 목표](goal.md)와 [전송 계약](packet-contract-v1.md)을 함께 따른다.
- 의미 정본은 Core API 1.0.0+2.0.0이며 v2 명시 변경이 우선한다. v2 hash는 `B05B18BA3B636BB3D94FA012ACA0E6E46A1793B05A91C3F7F47E1121755AC2A1`다.
- 현재 선언의 출처는 Core `msg_66dc4898461f`(2026-10-18T05:40:09Z)와 core-active의 지목한 파일이다. **미커밋 구현의 공개 선언 관측**이며 실제DB·독립강·main 통합 인계는 아직 없다. World branch로 제품 파일을 복사하거나 대체 Core 타입을 만들지 않는다.
- 아래 표는 선행 시험의 reflection/대역 준비 입력이다. 최종 통합 전 Core가 넘기는 commit·파일/hash와 다시 대조한다. 공개 signature/의미가 바뀌면 새 버전으로 인계한다.

## 타입과 결과

기본 namespace는 `Dawnholder.Server.GameServer.Persistence.Catalog`, 기존 Deadline/Checkpoint/Submission은 `Dawnholder.Server.GameServer.Persistence`, 직업은 `Shared.Protocol.CharacterClass`다.

| 타입 | 공개 의미/형식 |
|---|---|
| AccountContext | record(Guid AccountId, string NormalizedAccountName) |
| RequestContext | record(Guid SessionInstanceId, long Generation) |
| CharacterSummary | record(Guid CharacterId, byte Slot, string Name, CharacterClass Class) |
| CharacterStorageMode | Legacy, Stored. wire ordinal로 사용하지 않음 |
| InvalidField | None, Account, CharacterId, Slot, Name, Class, OperationId, Request. 의미별 외부 매핑 |
| CreatePreparationResult | CatalogStatus Status, CreateDescriptor? Descriptor, InvalidField InvalidField |
| CreateDescriptor | public constructor 없음. get-only Account/Slot/Name/NameKey/Class/PayloadVersion(1). PrepareCreate 결과만 보관/전달 |
| CatalogResult | get-only Status, Request, Character?, IReadOnlyList Characters, InvalidField, Rejection?, Lease?. 생성자는 아래 입력 의미를 가지며 목록 저장소를 복사함 |
| CharacterSelectionHandle | public 생성 없음. Task<CatalogResult> Completion, Task Close(SafeCheckpoint? finalProjection) |
| AdmissionLease | public 생성 없음. Checkpoint(SafeCheckpoint, PersistenceDeadline) → PersistenceSubmission |

CatalogResult의 공개 생성 입력은 `(CatalogStatus status, RequestContext request, CharacterSummary? character=null, IEnumerable<CharacterSummary>? characters=null, InvalidField invalidField=None, CatalogStatus? rejection=null, AdmissionLease? lease=null)`다. 시험용 결과는 이 공개 입력을 소비할 수 있지만 내부 constructor를 가진 handle/lease를 제품 외부에서 재구성하지 않는다.

CatalogStatus는 Prepared/Listed/Created/Selected/Applied/NotApplied/StillUnknown, InvalidInput/OperationConflict/SlotOccupied/SlotsFull/NameTaken, CharacterNotFound/NotOwned/RequestInProgress/InvalidState/Busy/RecoveryRequired, CancelledBeforeDispatch/Unavailable/SchemaMismatch/IntegrityFailure/OutcomeUnknown의 **의미**를 가진다. 숫자 cast는 금지다. NotApplied는 Rejection, Applied는 원 Created summary를 사용하며 상태별 필수값 부재/상관 불일치는 성공으로 추정하지 않는다.

## facade

`CharacterCatalogService`의 공개 생성 입력은 `(ICharacterCatalogRepository catalog, ICharacterRepository characters, SafeDefaults defaults, TimeProvider timeProvider, int capacity=128)`다. repository 구현과 SQL은 Core 소유다.

| 메서드 | 입력 | 반환 |
|---|---|---|
| PrepareCreate | AccountContext, byte slot, string rawName, CharacterClass | CreatePreparationResult |
| ListCharactersAsync | AccountContext, RequestContext, PersistenceDeadline, CancellationToken | Task<CatalogResult> |
| CreateCharacterAsync | CreateDescriptor, Guid operationId, RequestContext, PersistenceDeadline, CancellationToken | Task<CatalogResult> |
| ResolveCreateAsync | 최초 CreateDescriptor, 최초 Guid operationId, RequestContext, 원 PersistenceDeadline, CancellationToken | Task<CatalogResult> |
| BeginSelectCharacter | AccountContext, Guid characterId, Guid operationId, RequestContext, PersistenceDeadline, CancellationToken | CharacterSelectionHandle |
| FlushAsync | PersistenceDeadline | Task |

PrepareCreate는 DB 없이 유효성을 검증한다. Catalog는 `_pending.Add`로 host 소유 등록 뒤 dispatch하며 capacity 초과/등록 실패 시 미소유 변경을 시작하지 않는다. Core 보고의 host 복구는 원ID/descriptor로1회·최대5초다. 이것은 사용자 요청 예산의 갱신 허가가 아니다. 실제 SQL 실패 수명 안전성은 Core의 실제 실행/강 판정 뒤 확인한다.

## 호스트·입장 연결

- `Dawnholder.Server.GameServer.Persistence.Admission.CharacterAdmissionServices(CharacterCatalogService, TimeProvider)`의 Mode는 Stored이며 Catalog getter를 제공한다. 이전 단일 identity 생성자는 새 소비 경로에서 쓰지 않는다.
- `Dawnholder.Server.GameServer.Hosting.ServerHost(GameWorld, IPEndPoint)`는 get-only StorageMode/CharacterCatalog를 갖는다. Core host가 같은 world.Persistence를 고정하며 Stored+null catalog/참조 변경을 시작 실패로 다룬다.
- internal PersistenceRuntimeStartup.CreateAsync(args, token, SafeDefaults? defaults=null)는 profile2 preflight 뒤 Services를 만든다. Program이 host 생성 전 GameWorld.AttachPersistence(Services)를 호출한다.
- 정상 종료는 handle.Close(실제 소유자의 SafeCheckpoint?)로 연결하고 host FlushAsync가 생성 settlement·선택 closure를 추적한다. 입장 전 null, 실제 입장 뒤 안전 projection 한 번 캡처를 보존한다.
- lease.Checkpoint가 돌려주는 PersistenceSubmission의 Status Accepted는 **제출 책임 인수**다. Task<PersistenceWorkResult>? Completion을 관측해야 저장 결과를 안다. Accepted 자체를 DB 성공으로 표시하지 않는다. 제출 거부 QueueFull/Closing/Frozen/Invalid/Expired도 구분한다.
- 실제 선택/입장은 World가 session/generation/상태를 대조해 [전송 계약](packet-contract-v1.md)의54→48→입장→guild 순서를 수행한다. Core가 player/inventory/guild 등록을 직접 호출하지 않는다.

## 소유권과 후속 관문

Core 소유는 Program.cs, Hosting/ServerHost.cs, Hosting/PersistenceRuntime/PersistenceRuntimeStartup.cs, Persistence/Admission/CharacterAdmissionServices.cs·CharacterAdmissionLifetime.cs와 Catalog/저장 구현이다. World는 Sessions/GameSession.cs·Loop/GameWorld.cs·새 패킷 handler와 필요한 adapter를 소유한다. Core 지목 파일을 World가 임의 수정하지 않으며 통합 뒤 최소 소비 변경도 먼저 조율한다.

Core는 현재 추가 변경을 확정하지 않았지만 DB 결함 결과에 따라 바뀔 수 있다고 밝혔다. 따라서 이 표를 구현·실DB 통과 보증으로 쓰지 않는다. Core 실제 인계·schema5/profile2·저장재접속·독립강·최신 통합 SHA·실행 창이 최종 통합의 선행 조건이다.

Content 공유 Unity 파일의 전체 쓰기 종료는 미정이다. `Network/UnityClientSession.cs`·`Network/NetworkService.cs`의 길드 PR2 소유와 직렬 조율하고, `Network/CharacterLoad/CharacterLoadAuthority.cs`·`Network/Handlers/Session/CharacterLoadResultHandler.cs`는 Core의48/입장 계약과 메인 배정까지 대조한다. Content 회신만으로 World가 이 기존 입장 파일 전체를 쓰지 않는다. 새 비화면 CharacterMenu 파일은 후속 한정 계약 전까지 후보이며 현재 선행 준비에서 Unity 쓰기0이다.
