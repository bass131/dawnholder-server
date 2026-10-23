# Core 캐릭터 저장 API 계약 v2

- 버전: **2.0.0 / 구현 계약 고정본**. 기준 main `f6bf03fcf8067ec837ec8ccec944990f91c225a4`(PR249). 구현·검증 완료를 뜻하지 않는다.
- 승인 범위·이름/슬롯/직업·영속성 요구는 [goal](goal.md), 변경하지 않은 의미는 [API v1](character-storage-api-v1.md)을 따른다. 두 문서를 함께 읽으며 아래 명시 변경이 우선한다. v1 bytes와 기존 RED 원시는 보존한다.
- 원천: 메인 `msg_c3583e7e4111`, `msg_d5a04b2bf268`; World 질의 `msg_aa083c106e80` 및 Core 답변 `world-api-reply-v0.1.md`(goal 근거 폴더). World 패킷 v0.2 hash `59DBADCB27EB32551E3D435AB0F7B8587E65D95CAEC1EB447339D6F00FA7AED5`의 Q1/Q2/Q4 접점을 구체화한다.
- 변경 후 이 파일을 조용히 고치지 않는다. 새 버전·차이·적용 단계·hash를 전달한다. hash는 별도 manifest에 기록한다.

## v1 대비 변경

1. `PrepareCreate`가 DB 없이 이름·계정·슬롯·직업을 검증하고 불변 `CreateDescriptor`를 만든다. World가 NFC/비교 키를 중복 구현하지 않는다.
2. `CreateCharacterAsync`와 `ResolveCreateAsync`의 생성 입력은 이 descriptor다. v1의 raw 입력을 직접 받는 signature는 대체한다. 목록/선택의 의미와 메서드 이름은 유지한다.
3. Core host가 변경 dispatch 전에 미결 생성 요청을 소유한다. 연결 종료 이후 별도 인계 호출의 성공을 안전성 전제로 삼지 않는다.
4. host 시작 때 `CharacterStorageMode.Legacy` 또는 `Stored`를 불변으로 주입한다. Stored 구성 실패는 시작 실패이며 실행 중 fallback하지 않는다.

## 서버 내부 공개 접점

네임스페이스는 `Dawnholder.Server.GameServer.Persistence.Catalog`, 공개 facade는 `CharacterCatalogService`다. 파일은 같은 책임별로 `Persistence/Catalog/`에 둔다. 생성자와 repository 내부 배치는 구현자가 결정해 보고한다. 아래 값의 이름·입력 의미는 고정하며 네트워크 구조체가 아니다.

| 접점 | 입력 | 결과 |
|---|---|---|
| `PrepareCreate` | `AccountContext`, byte Slot, string rawName, `CharacterClass` | `CreatePreparationResult`: Prepared + `CreateDescriptor`, 또는 InvalidInput + InvalidField |
| `ListCharactersAsync` | `AccountContext`, `RequestContext`, `PersistenceDeadline`, `CancellationToken` | Task의 결과는 v1 Listed/거부와 같은 의미 |
| `CreateCharacterAsync` | `CreateDescriptor`, Guid OperationId, `RequestContext`, `PersistenceDeadline`, `CancellationToken` | Task의 결과는 v1 Created/거부/OutcomeUnknown과 같은 의미 |
| `ResolveCreateAsync` | `CreateDescriptor`, 최초 Guid OperationId, `RequestContext`, `PersistenceDeadline`, `CancellationToken` | Task의 결과는 v1 Applied/NotApplied/StillUnknown 등과 같은 의미 |
| `BeginSelectCharacter` | `AccountContext`, Guid CharacterId, Guid OperationId, `RequestContext`, `PersistenceDeadline`, `CancellationToken` | 즉시 수명 handle 또는 시작 거부. Completion/Close는 v1 의미 유지 |

`AccountContext(AccountId, NormalizedAccountName)`, `RequestContext(SessionInstanceId, Generation)`, `CharacterSummary(CharacterId, Slot, Name, Class)`의 값 의미는 v1과 같다. 정상/거부 완료 모두 원래 RequestContext를 보존한다. prepare는 아직 RequestContext나 OperationId를 만들거나 DB에 쓰지 않는다.

`CreateDescriptor`는 Core가 검증한 계정·슬롯·NFC 표시 이름·NameKey·직업·payload version의 불변 묶음이다. 외부 caller가 임의로 구성/수정할 공개 setter나 생성 경로를 두지 않는다. World는 보관·전달만 하며 payload/NameKey를 패킷에 노출하지 않는다. facade는 null·다른 계정/operation 충돌·상관값의 유효성을 계속 방어한다. RequestContext·timestamp·retry 횟수·deadline은 descriptor와 canonical payload에서 제외한다.

서버가 발급한 OperationId를 descriptor와 함께 최초 호출 전에 고정한다. 다른 generation에서 원래 요청을 조정해도 같은 ID/descriptor를 사용한다. 변경한 이름/직업/슬롯은 새 논리 요청이며 Unknown의 자동 재생성 수단이 아니다.

대안인 World 정규화 중복 또는 raw payload 재구성은 의미 드리프트와 재시도 충돌을 만들므로 채택하지 않는다. v1 시험의 reflection adapter는 독립 시험 소유자가 이 signature에 재연결한다. 구현자가 시험을 완화하거나 임시 생성자 가정에 맞춰 제품 책임을 왜곡하지 않는다.

## 생성 미결 작업의 소유권

Core host 수명의 catalog가 검증된 OperationId/descriptor/dispatch 상태/최종 결과를 **DB 변경 dispatch 전에** 등록한다. 등록 실패·용량 초과·host 종료로 소유권을 보장할 수 없으면 dispatch하지 않는다. 사전 거부는 확정된 무변경 결과로 반환하며 dispatch 가능성이 있으면 OutcomeUnknown을 보존한다.

클라이언트 연결·RequestContext의 수명은 이 소유권과 별개다. caller 취소·deadline·응답 실패·World 제거로 미결 항목을 버리지 않는다. 사용자 요청의 모든 재시도/조정은 원래 deadline 안에서만 하며 새 5초로 갱신하지 않는다. host 복구는 별도 bounded 작업으로 원래 ID/descriptor만 조정할 수 있다. 새로운 사용자 성공 송신이나 입장 권한을 부여하지 않는다.

복구 횟수·예산·큐 용량은 구현 코드/설정에서 명시하고 무한 재시도·무제한 항목 누적을 금지한다. 운영 중인 사용자 설정값을 임의 변경하지 않는다. 복구 예산 소진·종료 제한시간 경과도 성공으로 치환하지 않고 durable receipt/authority와 진단으로 남긴다. 재시작 뒤 메모리 요청 목록 보존을 가정하지 않는다. durable 상태의 안전한 재조회 또는 다음 로그인 목록으로 관측하며 receipt 부재 한 번으로 NotApplied를 선언하지 않는다.

기존 host 종료 정리와 같은 소유 경계에서 미결 생성과 선택 closure를 기다리고, 시간 제한 초과는 미완료로 보고한다. 복구 task 예외를 관측하고 늦은 선택 owner 정리도 계속 추적한다. 선택의 Close/lease/sequence/fence·새 소유자 불침해는 v1 그대로다.

## 시작 모드와 World 연결

`CharacterStorageMode`는 Legacy/Stored 두 값이며 host 시작 구성에서 한 번 결정한다. 기존 args 없음은 Legacy, 명시 `--persistence-profile`은 Stored다. 새 profile은 단일 계정/캐릭터 지정 대신 전체 정상 계정 대상이며 구 profile을 자동 해석하거나 일부 계정만 저장하지 않는다. schema/module/profile 호환성을 게임 행 생성 없이 preflight한다.

World에는 같은 host 수명의 mode/catalog/admission 연결을 제공한다. Stored에서 catalog가 없거나 초기화가 실패하면 시작을 실패시킨다. 요청별 설정 재조회·Stored 실패 뒤 Legacy 입장·기존 C_CharacterSelect를 통한 저장 우회는 금지한다. World 세션/패킷 adapter 구현은 World 소유이며 Core는 준비된 소비 접점과 수명 계약을 전달한다.

PR249의 입장 commit·inventory/guild 등록·close cleanup을 보존한다. 길드 멤버 키는 AccountId다. 같은 계정의 다른 CharacterId가 새 가입 seed나 별도 길드를 만들지 않는다. Core가 등록을 직접 호출하지 않는다. 기존 CharacterLoadResult48의 권위 직업 전달과 실제 입장 성공을 구분한다. 늦은 완료/닫힌 generation에서는 player/inventory/guild 등록이 모두 없어야 한다.

외부 오류 매핑은 World 소유: CharacterNotFound/NotOwned는 동일 선택 불가로 통합, 서버가 만든 Account/OperationId의 InvalidInput은 IntegrityFailure/field0으로 표현하되 내부 원래 코드·필드는 진단/시험에 남긴다. Slot/Name/Class/CharacterId의 사용자 입력 오류 의미는 유지한다.

## 기반 schema와 기준선

main protocol21, guild43..47·S_CharacterLoadResult48을 기준으로 한다. 49..54/v22는 World 제안이며 이 문서가 예약·생성·배포하지 않는다. Core 구현 계약은 PDL/공유 DLL/World 세션을 수정하지 않는다.

전체 표는 goal대로 만들되 wallet/weapon/guild의 게임 저장 연결은 제외한다. 현재 `GuildRules`의 GuildId=1, treasury cap=1,000,000,000, stronghold max=2, weapon max=1과 계정 멤버 수명을 schema 설계 근거로 읽는다. 이를 저장표 초기 seed 지급 또는 기존 게임 데이터 이관 권한으로 해석하지 않는다. 세부 키/열·미래 확장 한계는 구현 보고에서 현재 타입과 대조한다.

v1 선행 RED는 이전 main48aabbd5의 C# 실패77/DBskip21, SQL13PASS/3FAIL이다. PR249 이후 같은 결과를 실행했다고 주장하지 않는다. 새 signature/host 소유권/모드 접점의 시험 재연결·추가, 새main 회귀, 실제 DB/API·재접속과 독립 강 검증이 남아 있다.
