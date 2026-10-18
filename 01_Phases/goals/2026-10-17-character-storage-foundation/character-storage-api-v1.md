# Core 캐릭터 저장 API 계약 v1

- 버전: **1.0.0 / Core 제안 v1 고정본**. 승인 범위는 [goal](goal.md), 메인 승인 전달 `msg_8a66c440ec13`, World 요구 `msg_560337c050b9`.
- 목적: World·Content가 저장 구현을 기다리지 않고 동일한 입력/결과/취소 계약으로 대역을 만들 수 있게 한다. 두 파트가 이미 합의했거나 구현/독립 검증됐다는 뜻이 아니다.
- 기준 소스: `48aabbd5d23c8ec7021ff22282c90f295db64c67`. 문서 method/DTO 이름은 아래의 **목표 서버 내부 API** 식별자이며 아직 코드에 존재하지 않는다. 네트워크 ID/필드 배치/숫자 오류 코드는 World 계약에서 별도 고정한다.
- 고정 뒤 이 파일을 조용히 고치지 않는다. 변경은 v2 파일·차이·적용 시점·새 hash를 전달한다. 이 파일 bytes의 SHA256은 별도 근거 manifest에 둔다.

## 책임과 호출 경계

Core는 목록/생성/저장 소유권 획득·불러오기·receipt 조정·종료 저장을 소유한다. World는 세션 계정·현재 상태·generation·응답 전송·맵 입장을 소유한다. Content는 사용자 입력과 화면 표시를 소유한다. 비동기 완료 함수는 엔티티·인벤토리·길드를 직접 변경하지 않고 World 큐로 결과를 넘긴다. 틱에서 Task.Result/Wait·SQL/파일 I/O를 기다리지 않는다. 동기 prefix가 막힐 수 있는 저장 진입은 기존 admission처럼 틱 밖에서 시작한다.

Core 호출의 AccountContext는 World가 로그인 성공 후 서버 세션에서 만든 값이다. 클라이언트 패킷의 AccountId를 그대로 전달하지 않는다. 현재 로그인은 이름 기반 식별이며 비밀번호 인증이 아니다. Core도 정규화 계정 이름과 UUID v5 AccountId 일치·Character 소유를 다시 확인한다.

저장 활성 profile은 모든 정상 계정에 같은 저장 규칙을 적용한다. 실패 시 조용히 비영속 캐릭터로 입장시키지 않는다. 개발 비영속 실행 모드의 존속/차단은 호스트 명시 설정으로 구분하며 저장 활성 경로의 실패 fallback으로 쓰지 않는다.

## 값과 한도

| 값 | 타입·검사 | 소유 |
|---|---|---|
| AccountContext | AccountId non-empty Guid + NormalizedAccountName. 기존 AccountNameRule: 3~16 ASCII 소문자/숫자/underscore, 입력 대문자 소문자화. UUID v5 일치 검사. 캐릭터 이름 규칙과 다름. | World 세션→Core 재검사 |
| CharacterId | non-empty Guid, 생성 때 서버 발급·영구 유지. EntityId/슬롯/세션ID와 다름. 문서/텍스트 도구에서는 D형 Guid, 패킷 byte order는 World 고정 대상. | Core |
| Slot | byte 의미의 정수1..3, 계정 내 유일. 사용자가 고른 빈 슬롯 명시, 자동 슬롯 교체/덮어쓰기 없음. | 요청 의도→Core 원자 검증 |
| Name | 원시 UTF-16 최대36 code units(정상 한글12음절의 분해형 수용), 잘못된 surrogate 거부. NFC 후2..12개, U+AC00..D7A3/A..Z/a..z/0..9만. 공백 trim 없음. | Core 정본, 화면은 사전 안내 |
| NameKey | NFC 이름의 ASCII 소문자만 고정 대문자화. 정확 동일성 비교, DB binary 비교 기반 unique 제약과 동일 키 생성. 표시 Name은 대소문자 보존. | Core 저장 |
| Class | Knight=0, Mage=1만. 생성 때 고정, 선택은 class 입력을 받지 않음. | Core 저장→World 적용 |
| OperationId | non-empty Guid, 서버가 발급한 논리적 변경 요청 ID. 재시도/조정 시 동일 ID+동일 typed payload 유지. 클라이언트 correlation ID와 구분. | Core/World 서버 adapter |
| SessionInstanceId | 서버 발급 non-empty Guid, 연결마다 새 값. 재접속 때 재사용하지 않음. | World |
| Generation | 서버의 non-negative Int64, 연결 내 요청 교체/종료 시 증가. 최대치면 wrap하지 않고 연결을 종료. | World |
| Deadline | 기존 PersistenceDeadline, 단조 시간 기반 예산. 한 작업의 재시도/조정은 원래 예산 내; 기본 공개 작업5초, SQL 잠금 최대2초·연결/명령 최대5초를 넘기지 않음. | Core 호스트 |

Name의36자 원시 상한·명시 슬롯·아래 API 형태는 승인 기본값을 구체화한 Core 설계 선택이다. 허용한 NFC 결과2~12자 정책을 축소하지 않는다. 잘못된 입력에 예외 상세/SQL/비밀을 외부로 보내지 않는다. 결과 목록은 최대3개, 슬롯 오름차순, 페이지네이션 없음. DB에서3개 초과/중복 슬롯/잘못된 class를 읽으면 잘라 성공시키지 않고 IntegrityFailure.

## 공개 작업과 결과

각 작업은 cancellationToken과 Deadline을 받는 비동기 호출이다. `RequestContext = (SessionInstanceId, Generation)`는 완료에 그대로 반환되는 상관값이며 DB의 영속 식별자나 canonical payload에 넣지 않는다. 반환 데이터는 불변 snapshot이다.

`CharacterSummary = (CharacterId, Slot, Name, Class)`; AccountId·NameKey·fence·owner·SQL 정보는 클라이언트 목록에 포함하지 않는다.

| 작업 | 입력 | 성공 | 업무 거부/불확실 |
|---|---|---|---|
| ListCharactersAsync | AccountContext, RequestContext | Listed: summaries 0..3. DB 계정 행이 없으면 빈 목록, 조회만으로 게임 행 생성하지 않음. | InvalidInput, CancelledBeforeDispatch, Unavailable, SchemaMismatch, IntegrityFailure |
| CreateCharacterAsync | AccountContext, Slot, raw Name, Class, OperationId, RequestContext | Created: CharacterSummary. 계정 최초 등록·Character·Progress·authority·생성 receipt가 한 transaction. | InvalidInput, OperationConflict, SlotOccupied, SlotsFull, NameTaken, CancelledBeforeDispatch, Unavailable, SchemaMismatch, IntegrityFailure, OutcomeUnknown |
| BeginSelectCharacter | AccountContext, CharacterId, OperationId, RequestContext | 즉시 Core 소유 AdmissionHandle 반환. Completion이 Selected 또는 거부 결과로 끝남. Selected=(CharacterSummary, opaque AdmissionLease). | 시작 거부 InvalidInput/RequestInProgress/InvalidState. Completion: CharacterNotFound, NotOwned, Busy, RecoveryRequired, OperationConflict, Unavailable, SchemaMismatch, IntegrityFailure, OutcomeUnknown, CancelledBeforeDispatch |
| ResolveCreateAsync | AccountContext, 최초 OperationId와 정규화된 최초 생성 payload, 현재 RequestContext | Applied: 원래 Created summary, 또는 NotApplied: 확정 거부 코드. | OperationConflict, StillUnknown, InvalidInput, Unavailable, SchemaMismatch, IntegrityFailure |

BeginSelectCharacter는 DB 시작 전에 수명 handle을 반환해 World가 연결 종료 즉시 Close를 전달할 수 있게 한다. 반환 직전 발생한 연결 종료는 World가 handle 수령 즉시 다시 닫는다. Completion은 기존 lifetime과 같이 저장 작업을 기다리는 Task이며 틱을 막지 않는다.

선택은 기존 Character만 대상으로 한다. 없는 캐릭터를 acquire 중 자동 생성하지 않는다. 권한 획득은 Core가 읽은 현재 fence와 서버 발급 owner를 사용한다. Selected의 lease는 World가 보관하는 불투명 토큰이며 fence/sequence를 클라이언트나 패킷에서 받지 않는다. 목록 조회/생성만으로 runtime 소유권이나 player를 등록하지 않는다.

**선택 후 게임 안에서 다른 캐릭터로 교체하는 API는 없다.** World는 연결당 목록/생성/선택의 사용자 요청을 하나만 진행하고, 선택 중 추가 요청은 RequestInProgress, Active/Entering 뒤 요청은 InvalidState로 거부한다. 목록 새로고침은 완료 뒤 가능하다. 다른 캐릭터 선택은 새 연결의 정상 로그인 흐름으로 한다. Core도 같은 RequestContext에 중복 admission 소유자를 만들지 않도록 방어한다.

## 오류 의미와 우선순위

이름은 내부 의미 코드다. World가 wire enum을 고정하며 현재 패킷 오류 번호를 재사용/추정하지 않는다.

| 결과 | 의미와 호출자 행동 |
|---|---|
| InvalidInput | 이름/ID/slot/class/계정 문맥 오류. InvalidField(Account, CharacterId, Slot, Name, Class, OperationId)만 제공 가능. 다시 보낼 입력을 고친 새 논리 요청은 새 OperationId. |
| SlotOccupied / SlotsFull / NameTaken | 유효한 생성 요청의 확정 업무 거부. 자동 삭제/교체/다른 슬롯 생성 없음. 최신 목록 또는 이름 변경 안내. |
| CharacterNotFound / NotOwned | 없는 ID와 다른 계정 소유를 Core 내부에서 구분. World 외부 표현은 존재 여부 노출을 막기 위해 같은 선택 불가로 합칠 수 있으나 내부 시험/진단의 구분 유지. 다른 계정 정보 반환 없음. |
| RequestInProgress / InvalidState | 세션/수명 상태 거부, DB 요청을 새로 시작하지 않음. |
| Busy / RecoveryRequired | 다른 runtime/recovery owner가 보유. 강제 탈취/자동 fence 초기화 없음. 재접속/운영 복구 판단. |
| OperationConflict | 같은 OperationId에 다른 account/character/종류/payload. 요청 재작성·ID만 바꾼 자동 재시도 금지. 로그와 명시 실패. |
| CancelledBeforeDispatch | DB 변경 명령을 보내지 않았음을 확실히 아는 경우만. DB 효과 없음. |
| Unavailable | 읽기 실패, 또는 변경 dispatch 전 실패가 확인됨. 변경이 시작됐을 가능성이 있으면 이것으로 확정하지 않고 OutcomeUnknown. |
| OutcomeUnknown / StillUnknown | 변경 적용 여부 불명. 성공/실패를 임의 확정하지 않음. 원래 ID/payload로 receipt 조정. 새로운 생성/선택 ID로 자동 재시작 금지. |
| SchemaMismatch / IntegrityFailure | 호환 schema/profile 불일치 또는 데이터 불변식 훼손. 입장 차단, 운영 진단. 비영속 fallback 없음. |

검사 순서: World 세션 로그인/상태/generation → Core 구조·값 검증 → DB schema 호환·계정 identity → 동일 OperationId receipt/payload → 대상 소유·업무 제약 → mutation/receipt. 기존 저장 RPC의 fence→owner→sequence와 receipt 규칙은 기존 타입·SQL 계약을 보존하며 임의 순서 교체로 결과를 바꾸지 않는다.

생성 업무 제약 우선순위는 전체3칸 사용이면 SlotsFull, 아니면 요청 슬롯 점유 SlotOccupied, 아니면 이름 충돌 NameTaken이다. 동시성 unique 충돌도 최종 원자 관측에 따라 이 코드로 해석하며 raw SQL 예외를 그대로 전달하지 않는다. 성공 receipt 재조회는 이미 차 있는 슬롯 검사보다 먼저 처리되어 동일 요청이 SlotOccupied로 바뀌지 않는다.

## 생성 transaction과 재시도

새 계정 등록은 기존 이름/UUID 일치 입력만 받고 임의 이름 역산/기존 ID 채택을 하지 않는다. 슬롯 검사는 계정 단위, 이름은 전역 unique로 직렬화하고 일관된 잠금 순서를 갖는다. CharacterId는 최초 생성에서 서버가 정하고 성공 receipt에 저장한다. 같은 요청 재시도는 새 GUID를 발급해 다른 행을 만들지 않는다.

생성의 canonical payload는 (버전, 종류 Create, AccountId, NormalizedAccountName, Slot, 정규화 표시 Name, NameKey, Class)다. 이름 대소문자는 유일성에서는 같지만 표시 이름 변경을 포함한 재전송은 다른 payload다. 원시 Unicode 표현만 다르고 NFC 표시값이 같으면 같은 payload다. 시간·retry 횟수·새 connection generation은 포함하지 않는다.

생성 receipt는 Character가 아직 없거나 실패한 경우도 표현해야 하므로 기존 character FK만으로 생성 ledger를 제한하지 않는다. **생성 receipt의 구체 SQL 배치/내부 타입은 구현 설계 책임**, 의미는 이 문서대로다. commit은 Character·Progress·authority와 receipt에 원자적이다. 동일 ID의 확정 업무 거부도 같은 결과를 재현한다. 잘못된 형식처럼 dispatch 전에 거부된 요청은 receipt를 만들지 않아도 된다.

ResolveCreate에서 receipt가 없다는 조회 하나만으로 NotApplied를 선언하지 않는다. 같은 논리 요청과 경합하는 실행이 없고 적용되지 않았음을 보장하는 조정 경계가 필요하며, 확인 불가면 StillUnknown이다. payload 충돌이면 OperationConflict다. 기존 CharacterOperation의 runtime/recovery 영수증 의미와 전역 ID 충돌 검사를 함께 보존한다. 종류 간 ID 충돌도 성공 재사용하지 않는다.

응답 유실 뒤 연결이 끊겨도 생성 commit은 유지된다. 다음 로그인 목록에 해당 캐릭터가 나타나며 자동 재생성하지 않는다. 서버가 보유한 미결 요청은 원래 ID/payload로 계속 조정한다. 클라이언트가 OperationId를 생성하거나 서버 receipt를 조작하도록 노출하는 것은 이 계약에 포함하지 않는다. 프로세스 재시작 뒤의 복구는 durable receipt·목록으로 확인하며 메모리 요청 ID 보존을 전제하지 않는다.

## 선택·취소·종료 상태 계약

| 시점 | Core | World/Content |
|---|---|---|
| 요청 수락 | immutable AccountContext/CharacterId와 새 owner 수명·operation을 고정. Character별 authority 획득. | 현재 session/generation과 handle 보관, Loading 표시. |
| 선택 성공 | 저장된 직업·summary와 lease 반환. 과거 acquire 성공 receipt만으로 현재 lease 소유를 보장하지 않고 현재 fence/owner 재대조. | World 큐에서 session 동일·열림·generation 일치·Loading을 모두 확인. 성공48→맵입장→길드가입. |
| 완료 전 취소/연결 종료 | handle.Close(null) idempotent. 새 작업 제출 차단, 이미 dispatch된 요청은 조정/필요 release까지 Core가 소유. | generation 무효화, 나중 completion을 입장/등록에 사용하지 않음. |
| 성공이 늦게 도착 | authority 획득이 있었다면 동일 수명 owner가 release/조정을 수행. stale lease를 다른 세션에 넘기지 않음. | 화면/플레이어·inventory·guild에 적용하지 않음. 결과 폐기와 저장 수명 정리를 구분. |
| 입장 뒤 정상 종료 | handle.Close(최종 safe projection)를 한 번 캡처하고 checkpoint/release. 결과가 미확정이면 host가 수명을 추적. | 실제 소유 맵/세션의 안전 snapshot을 넘기고 이중 최종 캡처 금지. |
| 예산 만료·DB 끊김 | 기다리는 요청은 bounded 결과로 끝내되 미정 DB 소유권을 지우지 않음. bounded 조정 실패는 recovery 필요로 보존, 무한 hot-loop 없음. | 미확정을 성공/확정 실패로 표시하지 않고 새 입장 차단·오류 안내. |

Close는 동일 handle에 여러 번 호출되어도 동일 최종화 작업을 돌려준다. 선택 전 닫힘에는 임의 gameplay projection을 만들지 않는다. 별도 취소 토큰은 기다리는 호출의 관심 종료이며 이미 commit된 DB 결과의 rollback 약속이 아니다. 선택 성공이 곧 player 등록을 뜻하지 않는다.

기존 CharacterPersistenceOwner의 checkpoint·release·ResolveOperationAsync·recovery 의미는 유지한다. 같은 캐릭터의 fence가 바뀌면 이전 owner는 쓰기/입장 불가. FenceLost는 과거 성공 receipt가 있어도 새 소유권이 아니다. Sequence/카운터 overflow는 거부하며0으로 wrap하지 않는다. 서로 다른 캐릭터는 독립 잠금, 동일 캐릭터는 독점이다.

## World·Content 대역 및 패킷 연결

- World 전달 현 기준 protocol21·ID1..48, guild43..47·CharacterLoadResult48 보존. 새 패킷 ID는 뒤에 추가하며 PDL/생성물/공유 DLL·양쪽 직렬화 시험은 World 소유. Core는 wire 값/UUID byte order를 이 문서로 지정하지 않는다.
- 대역 입력/출력은 위 DTO 의미와 동일하다. 목록0/1/3, 생성 성공/동시 중복/slot full, 선택 성공/타 계정/없는 ID, delay/Unknown/닫힘 뒤 완료를 선택할 수 있어야 한다. 임의 시드가 실제 DB 영속성 시험을 대신하지 않는다.
- 목록/생성 후 선택은 CharacterId를 제출하고 서버가 불러온 class로 입장한다. 로컬 PlayerPrefs class는 기존 캐릭터의 권위 입력이 아니다. UI의 연결 시작 시점 변경은 Content와 World 후속 계약에서 담당한다.
- 서버가 같은 계정의 두 캐릭터를 다른 길드/새 seed 대상으로 취급하지 않는다. 기존 AccountId 가입·개인 연결/공동 프로세스 수명 그대로. 재화/길드/강화 저장 전환은 다음 Core 목표.
- World 외부 패킷 매핑이 확정되면 메인이 해당 계약 version/hash를 양쪽에 전달한다. 이 Core 내부 계약만으로 네트워크 호환이나 사용자 전체 흐름 완료를 선언하지 않는다.

## 시험 수용 기준과 남은 구현 결정

독립 시험은 형식·정규화 경계(1/2/12/13자·분해 한글·surrogate·36 초과·공백/전각), 대소문자 유일성, 슬롯/이름 경합, 두 계정 격리, 재시도 payload/종류 충돌, Unknown 조정, 늦은 선택/종료, fence/sequence와 recovery 경합을 요구 기반으로 작성한다. 제품 계산을 복제해 기대값을 만들지 않는다.

실제 DB의 빈 생성·빈 기존 schema 업그레이드·행 있는 migration 거부/무변경, 실제 Core API의 저장/재접속·실패 경로를 강 독립 검증한다. 대역·단위·실제 SQL·Unity 실행은 구분한다. 현재 이 계약은 문서만 있으며 전부 미실행이다.

고정한 것은 위 **관찰 가능한 API 의미**다. SQL 함수/프로시저 배치·생성 receipt 테이블명·DTO 구현 파일·module version 값·호스트 profile의 정확 필드는 Core 구현 계약에서 정한다. API 의미·오류 순서·취소 수명이 달라지면 내부 선택으로 숨기지 않고 v2로 변경한다. GuildMember는 AccountId 키로 고정하되 무기 키/성장 상한 등 이번 저장 API 밖 schema 세부는 실제 World/Content 현재 계약과 대조한다.
