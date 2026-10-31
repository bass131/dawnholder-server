# 캐릭터 메뉴 전송 계약 v1

- 버전: **1.0.0 / 서버·전송 선행 준비 고정본**. 승인 goal은 [캐릭터 서버 목표](goal.md)다. 사용자 A 승인 전달은 `msg_fb6c227d3b39`, 적용 기준은 승인 기록 commit `2bf2206621d31ee3675af1ee74b67348abe46d90`다.
- 패킷 **49..54 / protocol22를 이 목표에 배정**한다. 현재 제품은 여전히 protocol21/48개이며 이 문서로 실행·배포·생성을 하지 않는다. 2026-10-18의 최신 origin/main 확인, 메인 `msg_239b72df7331`의 6개 checkout 관측, Core `msg_66dc4898461f`와 Content `msg_85481145274c`의 사용/예약0을 대조했다. 계약 발행·통합 때 새 충돌이 생기면 이 번호를 재사용하지 않고 새 버전으로 조정한다.
- 원천: 이전 `packet-contract-v0.2.md`의 wire/오류/수명 조율, Core API v1 의미+v2 우선 변경, Content 두 회신. 원시·고정 입력 hash는 `.backups/verification/2026-10-18-character-server/approval-01/`에 보존한다. 이 문서는 **Core 구현·실DB 강 판정 또는 Unity 코드 소유 이전 증거가 아니다.**
- 변경은 새 버전·차이·적용 단계·hash로 전달한다. 진행 중 시험/검증자의 고정 입력을 조용히 바꾸지 않는다.

## 프레임과 값

기존 4바이트 header는 `size:ushort, packetId:ushort`다. 숫자는 explicit LittleEndian이고 string은 `UInt16 LE 바이트 길이 + UTF-16LE payload`다. 길이는 UTF-16 문자 수가 아닌 바이트 수다. 새 list/binary Guid codec과 생성기 기능 확장은 하지 않는다.

- CharacterId 입력은 정확36자 D형 Guid이며 Guid.Empty를 거부한다. 출력은 소문자 canonical D형이다. 영문 대소문자가 달라도 같은 ID다. 최대72bytes이며 Guid.ToByteArray의 필드 순서를 wire로 삼지 않는다.
- 생성 rawName은 최대36 UTF-16 code units/72bytes다. 잘못된 surrogate는 거부한다. 정규화·비교 키·유일성은 Core PrepareCreate가 소유한다. 응답 이름은 Core가 보존한 NFC2..12자(허용 BMP)의 최대24bytes다. trim하지 않는다.
- 목록 occupiedMask는 bit0/1/2가 slot1/2/3이며 나머지 bit0이다. 각 슬롯 필드는 characterIdD/name/classId이고 빈 슬롯은 empty/empty/0이다. 유효 Knight도 class0이므로 **mask로 빈 상태를 구분**한다. 목록3개 초과·중복 슬롯·잘못된 class를 잘라 성공시키지 않는다.
- 요청 requestId는 nonzero uint다. reserved는0이다. 클라이언트는 연결마다 증가시키고 wrap 대신 새 연결을 사용한다. AccountId/이름·SessionInstanceId·Generation·OperationId·owner/fence/sequence·DB정보는 wire로 받거나 노출하지 않는다. 선택 입력에는 class가 없다.

## 필드 순서와 길이

응답 공통 필드 순서는 `requestId:uint, result:byte, invalidField:byte`다. 다음 길이는 header를 포함한 **설계 산식**이며 측정값이 아니다.

모든 패킷의 전체 타입 이름은 `Shared.Protocol.<아래 이름>`이다. 고정 슬롯의 실제 PDL/생성 필드명은 순서대로 `slot1CharacterIdD, slot1Name, slot1ClassId, slot2CharacterIdD, slot2Name, slot2ClassId, slot3CharacterIdD, slot3Name, slot3ClassId`다. 나머지 필드명은 아래 표의 대소문자를 그대로 사용한다.

| ID | 이름 | header 뒤 필드 순서 | 최대bytes |
|---|---|---|---:|
| 49 | C_CharacterListRequest | requestId, reserved:byte | 9 |
| 50 | S_CharacterListResult | 공통, occupiedMask:byte, slot1/2/3 각각 characterIdD:string/name:string/classId:byte | 314 |
| 51 | C_CharacterCreateRequest | requestId, slot:byte, classId:byte, reserved:byte, rawName:string | 85 |
| 52 | S_CharacterCreateResult | 공통, slot:byte, characterIdD:string, name:string, classId:byte | 112 |
| 53 | C_CharacterSelectRequest | requestId, reserved:byte, characterIdD:string | 83 |
| 54 | S_CharacterSelectResult | 공통, selectedCharacterIdD:string | 84 |

50의 성공 빈 목록은 mask0/세 빈 슬롯으로26bytes다. 50 실패도 mask0/빈 슬롯이며 소비자는 실패의 빈 필드를 정상 목록으로 반영하지 않는다. 52 실패는 slot0/두 empty string/class0으로16bytes, 54 실패는 empty string으로12bytes다. 성공52는 유효 slot/ID/NFC이름/class를, 성공54는 요청한 같은 선택ID를 반환한다. invalidField는 InvalidInput일 때만 아래 공개 값을 쓰고 나머지는0이다.

header/실제 길이·정확 소비 바이트·reserved0·string byte 상한/짝수 길이·정상 UTF-16은 **큰 문자열 할당과 업무/Core 호출 전**에 검사한다. 구조 위반은 기존 frame/handler 경계에서 거부하며 Core 호출0이다. 구조가 정상인 slot/class/name/ID의 값 오류는 InvalidInput으로 표현한다. packet requestId0은 상관 불가능한 요청으로 응답0/Core호출0으로 폐기한다.

## 공개 결과

새 `Shared.Protocol.CharacterMenuResult:byte`와 `CharacterMenuInvalidField:byte`를 사용한다. Core 내부 enum ordinal을 cast하지 않고 의미별로 매핑한다. 기존48·Guild enum은 바꾸지 않는다.

| result | 이름 | 의미 |
|---:|---|---|
| 0 | Success | 해당 작업의 Listed/Created/Selected 또는 조정 Applied |
| 1 | InvalidInput | 공개 사용자 입력 오류 |
| 2 | RequestInProgress | 연결의 원래 요청은 유지하고 새 Core 작업0 |
| 3 | InvalidState | 로그인 전 또는 Entering/Active 등 비허용 상태 |
| 4 | SlotOccupied | 요청 슬롯 점유 |
| 5 | SlotsFull | 전체3칸 사용 |
| 6 | NameTaken | 이름 유일성 거부 |
| 7 | CharacterUnavailable | CharacterNotFound/NotOwned의 같은 외부 표현 |
| 8 | Busy | 다른 runtime owner, 강제 탈취0 |
| 9 | RecoveryRequired | 복구/미확정 소유로 입장 차단 |
| 10 | CancelledBeforeDispatch | mutation dispatch 전 취소가 확실할 때만 |
| 11 | Unavailable | 읽기 실패 또는 mutation 전 실패가 확실할 때만 |
| 12 | OutcomeUnknown | OutcomeUnknown/StillUnknown, 적용 여부 미확정 |
| 13 | SchemaMismatch | schema/profile 호환 불일치 |
| 14 | IntegrityFailure | 데이터/서버 문맥 불변식 훼손 |
| 15 | OperationConflict | 같은 서버 논리ID의 종류/payload 충돌 |
| 16 | LegacyFlowOnly | host 시작 때 명시한 Legacy 모드에서만 |

invalidField는 None0/Slot1/Name2/Class3/CharacterId4다. Core Account/OperationId/Request 등 **서버가 만든 문맥**의 InvalidInput은 IntegrityFailure/None으로 매핑한다. None 등 의미 불명의 InvalidInput을 사용자 이름 오류로 추정하지 않는다. 내부 원래 코드·필드와 NotFound/NotOwned 구분을 진단/시험에 보존하며 SQL/비밀/타계정 데이터는 보내지 않는다.

World 로그인·상태/generation → Core 값/구조 → schema/계정 identity → 같은 OperationId receipt/payload → 소유/업무 순서를 보존한다. 생성의 확정 업무 우선순위는 SlotsFull → SlotOccupied → NameTaken이며 성공 receipt 재조회는 슬롯 점유 검사보다 앞선다. Core NotApplied는 고정 원래 Rejection의 의미로, Applied는 원래 Created summary로 매핑한다. 호출 종류와 불일치한 성공/summary/상관값은 정상 성공으로 추정하지 않는다.

## 요청·세대·예산

1. 계정은 로그인 성공의 서버 AccountContext만 사용한다. 현재 이름 기반 로그인이며 새 인증 체계는 범위 밖이다.
2. 연결당 목록/생성/선택의 사용자 요청은 하나다. 별도 새 requestId의 추가 요청은 RequestInProgress이고 원래 pending/generation/handle/deadline을 바꾸지 않는다. Entering/Active의 메뉴 요청은 InvalidState다.
3. 서버는 연결 수명의 requestId 상한값을 보유한다. 이미 관측한 값 이하의 ID는 응답0·새 Core 호출0으로 폐기하며 원래 pending은 그대로다. 추가 요청의 거부가 원래 ID를 완료시키지 않는다. 중복 ID의 다른 payload/종류도 새 요청으로 실행하지 않는다. 이 제한은 Core operation receipt의 영속 재시도와 별개다.
4. SessionInstanceId/Int64 generation/OperationId는 서버가 발급한다. 세대 한도에서 wrap하지 않고 종료한다. UI는 현재 연결/요청종류/requestId가 맞는 결과만 소비한다. 이전 연결·세대 완료는 화면·player/inventory/guild에 적용하지 않는다.
5. 생성은 DB 없는 Core PrepareCreate의 불변 descriptor를 최초 OperationId와 고정해 Create/Resolve에 그대로 전달한다. World가 NameKey/payload를 재구성하지 않는다. 모든 사용자 조정은 **원래 PersistenceDeadline** 안에서만 한다. 새5초·새ID로 Unknown을 재생성하지 않는다.
6. Core host는 dispatch 전 미결 생성을 소유한다. caller 취소·송신 실패·World 제거 뒤도 Core가 bounded 복구/진단을 소유한다. 사용자 예산 이후 host 복구는 새 사용자 성공·옛 입장 권한을 만들지 않는다. receipt 부재 한 번으로 NotApplied를 선언하지 않는다.
7. 저장 진입의 동기 prefix도 틱 밖에서 시작한다. 완료는 불변 결과를 World 큐에 게시하고 동일 session·열림·generation·현재 상태를 다시 확인한다. 틱의 Task.Result/Wait·DB/file I/O 대기는 금지다.

## 모드·선택·입장·종료

- host 시작 구성에서 Legacy/Stored를 한 번 고정한다. args 없음 Legacy, 명시 profile Stored의 Core 접점을 소비한다. Stored 초기화 실패는 시작 실패, 실행 오류는 오류로 남긴다. 요청마다 모드 재조회·Legacy fallback·기존 class 선택을 통한 저장 우회0이다. 명시 Legacy에서만16으로 기존 흐름을 안내한다.
- Stored 목록0은 성공/빈 목록이며 조회만으로 게임 행을 만들지 않는다. 생성 성공도 player/runtime owner/길드를 등록하지 않으며 선택은 사용자가 별도53으로 요청한다.
- BeginSelectCharacter는 DB 시작 전에 Core 소유 handle을 반환한다. 수령 직전 닫힘은 수령 즉시 Close(null), pending 닫힘은 generation 무효화와 Close(null)다. handle/lease를 World가 임의 생성하지 않는다.
- 유효 Selected는 **54 Success(선택ID) → 기존48 Success(저장 class) → 실제 맵 입장 패킷 → 길드 가입/44**다. 54는 플레이 가능 신호가 아니다. UI 로컬class/목록 class를 입장 권위로 사용하지 않는다.
- 선택 거부는54 거부로 끝내며 성공48/맵/player/inventory/guild 등록0이다. Selected 뒤 입장 실패/닫힘은 기존 정리와 Core handle 종료를 수행한다. 늦은 Selected는 World에 적용하지 않고 Core가 같은 owner 수명으로 release/조정한다.
- 입장 전에는 임의 gameplay projection을 만들지 않는다. 실제 입장 뒤 정상 종료는 소유자의 safe projection을 한 번만 캡처해 Close한다. checkpoint의 PersistenceSubmission.Accepted는 제출 책임 인수이며 저장 성공은 Completion으로 판단한다.
- 최종 Unknown은12를 알리고 연결을 닫는다. 이미 commit된 생성은 유지돼 다음 로그인 목록에서 확인한다. 응답 실패/닫힘을 rollback·재생성·보상 재지급으로 바꾸지 않는다.
- 길드 키/가입 seed는 AccountId 수명이다. 같은 계정의 다른 캐릭터를 새 길드/추가seed로 만들지 않는다. 개인 wallet/weapon 연결 수명과 공동 프로세스 수명·현재 수치는 보존한다.

## 제품·시험 경계

PDL 끝에만 추가하고 기존1..48의 번호·필드 순서를 보존한다. GenPackets는 생성기로 만들고 protocol22 equality handshake를 서버·봇·Shared/ClientNet·Unity 소비자와 함께 검증한다. PDL/버전만 먼저 배포하지 않는다. 기존21은 새 wire 배포판에서 임의 수락하지 않는다.

선행 요구 시험에는 byte 고정 프레임/한도/잘린 UTF-16·길이·reserved, 목록0/1/3·mask/class0, 생성 거부/중복·상관값, 계정 소유 격리, pending/닫힘/늦은 완료, Unknown/원래 예산·Core host 소유,54→48→입장→guild, Legacy와 Stored 실패, AccountId seed 보존을 넣는다. 기대 byte/결과는 이 계약의 독립 고정값에서 만들며 제품 계산 복제로 항상 통과하게 하지 않는다.

**Unity 제품 파일 쓰기와 실제 실행은 아직 허용되지 않았다.** Content가 공유 `UnityClientSession.cs`/`NetworkService.cs`를 길드 PR2에서 사용할 예정이므로 쓰기 종료·한정 소유 이전 전 World는 이 파일과 입장 관련 파일을 쓰지 않는다. 새 Network/CharacterMenu 책임 경로도 후보이며 Unity 후속 계약에서 정확 파일을 고정한다. 현재 서버/wire 선행 준비의 성공은 Unity 소비자·UI·실DB·Player build 완료가 아니다.

## Content 선택·생성 창 안내 — 2026-10-28 추가

사용자 결정 27 A·범위 v2 승인(goal 「적용 중인 사용자 결정」의 `msg_5642ff7de0f4`)에 따라 Content 창이 쓸 서버 규칙을 적는다. 위 wire 필드·번호·결과는 바꾸지 않는다. 화면 동작은 Unity 소비 계약 `unity-consumer-contract-v1.md`의 `CharacterMenuClient` API(목록·생성·선택 요청과 결과 통지) 위에 만든다.

- **이름 규칙:** 서버가 판정한다. 원천은 `02_Server/GameServer/Persistence/Catalog/CreateDescriptor.cs`의 `Name`이다. 원문은 2~36 UTF-16 단위까지 받고, NFC 정규화 뒤 2~12자여야 한다. 허용 문자는 한글 음절(가~힣)·영문 대소문자·숫자뿐이다. 중복은 영문만 대문자로 바꾼 키로 본다(`abc`와 `ABC`는 같은 이름). 창은 이 규칙을 안내 문구로만 쓰고, 거절은 51 응답의 `InvalidInput`·`invalidField`와 `NameTaken`으로 표시한다. Shared 공개 상수는 만들지 않는다(Core 회신 `msg_a4bad38aab8c` 6).
- **새 로그인 이름:** 로그인 이름은 서버에서 계정 ID로 바로 바뀐다(`AccountIdResolver`). 처음 보는 이름은 캐릭터 0명인 빈 계정으로 시작하며 별도 계정 생성 요청은 없다.
- **Legacy 호스트:** DB 없이 Legacy로 시작한 서버는 로그인 뒤 캐릭터를 고르는 단계의 49·51·53 요청에 `LegacyFlowOnly`(16)로 답한다. 그 단계가 아니면 `InvalidState`(3)다(`Sessions/CharacterMenuSession.cs`의 `HandleLegacyRequest`). 창은 `LegacyFlowOnly`를 받으면 기존 직업 선택 흐름으로 넘어간다. Stored 서버는 이 결과를 보내지 않는다.
- **선택 뒤 순서:** 54 Success는 플레이 준비 완료가 아니다. 그 뒤 48의 저장 직업과 맵 입장 패킷이 와야 화면을 넘긴다(위 「모드·선택·입장·종료」).
