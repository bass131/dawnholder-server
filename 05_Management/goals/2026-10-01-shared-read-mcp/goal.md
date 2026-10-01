# M-1 — 개발 기록 공동 조회 MCP

## 목표와 재개 근거

사람의 Management UI와 Game Dev/Management 에이전트가 같은 `05_Management/records/catalog.json`을 필요한 만큼 조회한다. 로컬 stdio MCP와 공통 조회 모듈을 만들고 앱이 꺼진 상태에서 독립 client 두 개로 조회·버전 일치·종료를 검증한다. 조회는 목록/검색 → 시스템 요약 → 관련 변경·결정·검증·계획 → 출처 메타데이터 순서다.

2026-10-01 메인 Claude의 Orca 메시지 `msg_a873e160a5bc`가 M-1 재개 결정을 전달했다. 발신 `term_6505bda3-c071-476a-a50a-755c10fa02eb`를 살아 있는 메인 Claude terminal과 대조했다. **메인이 전달한 사용자 결정이며 사용자 직접 입력으로 격상하지 않는다.** [R-10](../../requirements.md#r-10)의 MCP 구현 보류는 이 목표 범위에서 해제한다. 서버 등록·로그 조회와 쓰기·실행 제어는 계속 후속 범위다.

기준은 [공동 조회 완료조건 1~5 및 다음 결정](../2026-09-30-system-records/shared-read-agreements.md)이다. 구현 순서·화면 없는 조회·동일 읽기 권한·공통 원본은 확정 사항으로 다시 묻지 않는다. 해당 합의의 과거 역할·모델·CLI 지정 대신 현재 [AGENTS](../../../AGENTS.md)를 적용한다. 이번 목표의 기준·상태·결과는 이 파일에 모으며 root CURRENT는 Game Dev 소유이므로 변경하지 않는다.

## 범위와 보존 계약

- 허용: 05 안의 읽기 전용 MCP, UI와 공유하는 catalog 계약·검증·읽기·검색 모듈, 필요한 05 package/TypeScript 설정, 독립 테스트와 실행·연결 안내. requirements/decisions/README/RESUME는 이 목표로 연결하는 데 필요한 부분만 후속 갱신한다. 첫 체크포인트는 이 goal 작성·커밋뿐이다.
- MCP에는 catalog 조회 도구만 등록한다. 쓰기·서버 조작·shell·외부 URL 요청·임의 파일 경로 읽기·원문 파일 열기를 노출하지 않는다. 도구의 path/URL 인자, client roots에 따른 읽기 범위 확장, 자동 source locator 해석을 두지 않는다. 읽기 전용 표식은 설명이며 실제 보장은 서버의 등록 도구·의존성·파일 접근 경계와 독립 테스트로 확인한다.
- catalog 본문·기존 ID·출처·`asOf`·`sourceCommit`을 이번 구현으로 최신화하지 않는다. catalog에 적힌 게임 상태와 실제 현재 게임 상태를 구분한다. 기존 UI JSON 불러오기·수정·저장·백업·잠금·낙관적 충돌 처리와 미저장 초안을 보존한다.
- Electron의 빌드 진입점 `frontend/desktop-dist/main.js`와 그 위치에서 `../../records/catalog.json`을 해석한 절대경로를 보존한다. 공통 모듈 이동이 `rootDir: electron`·`outDir: desktop-dist`·package의 main 경로를 조용히 바꾸지 않아야 한다. 빌드된 MCP entry도 동일한 05 원본을 가리킨다.
- Game Dev 소유 `02_Server`, root CURRENT/goal, AGENTS/.agents, 공유 DLL·PDL·Unity·실행 wrapper는 변경하지 않는다. HTTP/관리백엔드·서버 등록/로그·초안 승인 자동화·새 기록 저장 체계는 포함하지 않는다.
- Codex/Claude 설정(`.codex/config.toml`, `.mcp.json`, `.claude/*`, 사용자 설정 포함)은 변경하지 않는다. 실제 개발 세션 연결에 설정 적용이 필요하면 아래 D4의 사용자 결정으로 올린다. 전역 설정·업데이트·권한 우회는 수행하지 않는다.

## 설계 경계와 현재 근거

현재 `frontend/electron/catalog-contract.ts`는 스키마·중복 ID·참조 검증과 2 MiB 제한을, `catalog-store.ts`는 SHA-256 읽기 버전 및 저장/백업/잠금/충돌을 담당한다. `frontend/src/recordCatalog.ts`에는 UI의 시스템/기록 검색 규칙이 있다. catalog 크기는 75,931 bytes, SHA-256은 `4d81faaaf3dd375d2eebf0acb95983ca0aa2311038782dba6ee7d35ec9554488`이다. 이는 착수 시점의 정적 관측이며 새 MCP 검증 결과가 아니다.

Sol은 05 안에서 다음 책임을 분리한다. 정확한 파일 배치는 이 경계를 유지하는 범위에서 정하고 위임 spec에 소유권을 열거한다.

1. **공통 계약·순수 조회:** Electron/React/MCP 의존성 없이 타입, 스키마·참조 검증, 기존 검색 의미, ID 연결과 DTO를 소유한다. UI와 MCP가 이를 사용하며 기존 import는 필요한 경우 재수출로 보존한다.
2. **고정 원본 읽기:** Node 파일 I/O와 snapshot 생성만 담당한다. production 진입점은 자체 module 위치를 기준으로 고정 catalog를 결정하며 argv·env·caller cwd·요청 인자·client roots로 원본을 고르지 않는다. UI 저장은 기존 write 경계에 남기고 MCP는 write 모듈을 가져오지 않는다.
3. **MCP adapter:** 입력 검증, 공통 조회 호출, 응답 크기·오류 변환, stdio 수명주기를 소유한다. Electron 실행과 분리된 build/start 명령을 제공하고 실행 중 catalog 수정에는 재빌드가 필요 없다. stdout은 프로토콜 전용, 진단은 stderr다.

한 요청은 한 번 확보·검증한 불변 snapshot만 사용한다. 같은 열린 파일의 크기/상태를 읽기 전후 대조하고 변경이 감지되면 부분 결과 없이 오류를 반환한다. UI의 atomic rename으로 교체된 경우 기존 또는 새 완결 snapshot 하나만 반환한다. 외부의 비원자적 편집이 감지되면 읽기 실패로 다루며 정상본 cache로 조용히 대체하지 않는다. 재요청 때 파일을 다시 읽는다. handle은 읽기/상태 확인 직후 finally에서 닫고, 검색·직렬화·응답 전송 중에는 보유하지 않는다. 읽기와 UI 저장의 양방향 간섭은 Windows의 실제 파일 I/O 회귀로 확인한다.

**시험 경계는 Sol의 필수 구현 범위다.** catalog 경로·파일 연산(open/stat/길이 제한 read/close)·AbortSignal을 받는 내부 reader factory와, `readSnapshot(signal)`을 주입받는 서버 factory를 export한다. production entry는 이 factory를 고정 경로·실제 I/O로만 조립하고 시험용 import/선택 옵션을 노출하지 않는다. 검증자 소유의 별도 시험 entry가 factory를 import해 fixture child process를 띄운다. 주입한 I/O와 readSnapshot의 Promise를 시험에서 멈췄다 재개할 수 있게 해 stat/read 사이의 변경, read 중 증가, 취소를 결정적으로 재현한다. 지연은 시험 구현이 제공하고 production 기본에는 sleep이나 환경변수 hook을 두지 않는다. signal을 I/O 전후와 응답 생성 전에 확인하며 취소된 요청의 성공 data를 내보내지 않고 handle/동시 처리 슬롯을 해제한다. 이 경계는 실제 실패·취소 검증을 위한 것이며 일반 플러그인·임의 실행 기능으로 넓히지 않는다.

## 남은 결정에 대한 추천안

**메인 후속 지시 `msg_d72a957a219c`에 따른 보완안이다.** D1의 SDK 2.2.0은 첫 smoke 확인을 조건으로 메인이 채택했고 D2 응답 한도 축소도 메인 기술 결정이다. 두 항목을 새 사용자 승인으로 올리지 않는다. D4 정본 범위는 후속 `msg_f37d2837f0b1`로 전달된 사용자 결정에 따라 **management-active 작업 트리로 확정**했다. 메인이 전달한 결정이며 사용자 직접 입력으로 격상하지 않는다. 나머지 기술 계약은 아래와 같이 구체화해 메인의 goal diff 확인을 받는다. 공식 근거 확인일은 모두 **2026-10-01**이며, 문서 근거와 실제 실행 결과를 구분한다. 메인이 보완 goal을 승인하기 전에는 Sol을 발행하지 않는다.

### D1. 프로토콜과 SDK

**조건부 채택:** TypeScript 공식 `@modelcontextprotocol/server` **2.2.0**과 schema 작성용 `zod` **4.6.5**를 직접 dependency로, 독립 시험 client용 `@modelcontextprotocol/client` **2.2.0**을 devDependency로 Sol이 exact 고정하고 lockfile에 반영한다. 전이 의존성을 직접 import하지 않는다. 검증자는 이 lockfile로 `npm ci`를 실행하고 package/lockfile을 수정하지 않는다. `serveStdio(factory)`로 **2026-07-28** 규격과 **2025-11-25** 초기화 호환 경로를 모두 검증한다. 다른 SDK 기본 지원 revision은 시험한 범위와 구분한다. HTTP·인증·추가 운영 기능은 도입하지 않는다.

공식 [2.2.0 release](https://github.com/modelcontextprotocol/typescript-sdk/releases/tag/v2.2.0)와 [SDK 저장소](https://github.com/modelcontextprotocol/typescript-sdk)는 v2 안정 계열을 제공한다. [규격 전환 안내](https://ts.sdk.modelcontextprotocol.io/v2/migration/support-2026-07-28)는 기존 `initialize` 경로와 새 요청별 규격을 구분하며, [stdio 안내](https://ts.sdk.modelcontextprotocol.io/v2/serving/stdio.html)는 하나의 factory로 구형 client도 수용하는 진입점을 설명한다. 따라서 합의의 초기화 시험을 최신 규격의 필수 wire 메시지라고 오해하지 않는다. legacy에서는 initialize/initialized를, modern에서는 discovery·요청 metadata를 시험한다.

로컬 조회 결과 Node `v24.15.0`, npm `11.13.0`이며 두 2.2.0 package의 registry metadata는 Node `>=20`이다. `npm view`로 zod 4.6.5와 SDK 2.1.0의 존재도 확인했지만 설치·실행 성공은 아니다. GitHub 공식 release API의 2.2.0 게시일은 **2026-09-28T19:24:07Z**로 검토일 기준 3일 전이다. 대안인 직전 2.x(2.1.0)는 신규 release 노출 기간이 더 길지만 2.2.0의 수정분을 놓치며 이번 조사의 설치/차이 실증은 없다. v1은 legacy 한정과 후속 전환 비용이 있어 추천하지 않는다. 메인은 exact pin·lockfile과 첫 smoke를 조건으로 2.2.0을 수용했다.

**Sol 첫 단계는 smoke 보고다.** 지정 package 설치, serveStdio 기동과 legacy/modern 연결, strict schema의 알 수 없는 키·과대 query·잘못된 enum 거부 채널을 확인해 원시 요청/응답·실제 dependency version·stdout 오염 여부를 Astra에게 먼저 보고한다. 이는 구현자 자체 점검이며 독립 시험이 아니다. API 불일치·입력 무시·과대/민감 입력 반사 등 문제가 있으면 상태를 보존하고 메인에 보고하며 임의로 다른 버전으로 바꾸지 않는다. 시험용 client 패키지 추가와 내부 factory를 이 단계부터 Sol spec에 명시한다.

### D2. 조회 도구와 응답 제한

도구는 `list_systems`, `search_records`, `get_system`, `get_record`, `get_source` 다섯 개다. 아래에서 `?`는 선택 인자이며 나머지는 필수다. 모든 도구는 선택적 `expectedHash`를 추가로 받는다. 미지정 query/area/type/systemId는 필터 없음이다. query의 공백만 있는 값은 목록 의미, area의 빈 값은 필터 없음이며 type/systemId를 지정했다면 빈 값은 거부한다.

| 도구 | 인자(expectedHash 공통) | 성공 envelope의 data |
|---|---|---|
| `list_systems` | `query?`, `area?`, `limit?`, `offset?` | `{ items: SystemSummary[], paging: { total, offset, limit, returned, nextOffset } }` |
| `search_records` | `query?`, `area?`, `type?`, `systemId?`, `limit?`, `offset?` | `{ items: RecordSummary[], paging: { total, offset, limit, returned, nextOffset } }` |
| `get_system` | `id` | `{ system: SystemRecord }` |
| `get_record` | `id` | `{ record: DevelopmentRecord }` |
| `get_source` | `id` | `{ source: RecordSource, evidenceRead: false, availabilityVerified: false }` |

`SystemSummary` 필드는 `id,title,area,summary,implementationStatus,integrationStatus,verificationStatus,lookupSupported,truncatedFields`다. `RecordSummary`는 `id,type,title,summary,status,systemIds,lookupSupported,truncatedFields`다. summary는 160, title과 상태 문자열은 80 UTF-16 code units까지의 preview이며 잘랐을 때 해당 필드명을 item의 `truncatedFields`에 넣는다. surrogate pair 중간을 자르지 않는다. ID·area·type·연결 ID 배열은 자르지 않는다. `lookupSupported`는 ID 길이가 128 이하인지 표시하며 더 긴 기존 ID는 목록에 그대로 보이지만 상세 입력 한도로 조회할 수 없다는 알려진 제한을 문서화한다. 현 catalog ID 최대 길이는 Fable 관측 27이다. UI 스키마/저장을 이 제한에 맞춰 임의 변경하지 않는다.

상세 DTO는 현 catalog 계약의 필드명을 보존한다. `SystemRecord`: `id,title,area,summary,responsibility,behavior,implementationStatus,integrationStatus,verificationStatus,limitations,nextSteps,sourceIds,relatedSystemIds,recordIds`. `DevelopmentRecord`: `id,type,title,summary,reason,status,systemIds,sourceIds,details,limitations,nextSteps`. `RecordSource`: `id,title,kind,locator,revision,section,availability,note`. 상세 필드를 생략·잘라 정상 결과로 반환하지 않는다. `locator`는 데이터이며 실행하거나 열지 않는다. 두 false 필드는 catalog에 적힌 출처와 실제 원문 읽기/현존 확인을 구분한다.

필터는 AND로 결합한다. query 대상과 공백 분리 AND 검색은 기존 UI 함수를 공유하고 `systemId`는 records.systemIds의 정확 일치다. 존재하지 않는 systemId는 `NOT_FOUND`, area가 일치하지 않거나 검색 결과가 없으면 정상 빈 목록이다. 정렬은 ID의 **UTF-16 code unit 순서**로 고정하며 localeCompare를 쓰지 않는다. 기존 `toLocaleLowerCase()` 기반 검색은 공통 함수 하나로 유지하고, 기본 locale이 다른 실행 환경 간 결과 차이는 알려진 제한으로 기록한다. 동일 환경·동일 hash·동일 조건의 결정성을 시험한다.

| 항목 | 추천 계약 |
|---|---|
| query / ID / area | query 최대 256, ID·area 최대 128 UTF-16 code units. ID는 빈 값 거부. query의 빈 값은 목록 의미이며 검색 정규화/대상 필드는 기존 UI 의미를 공유한다. |
| record type | 기존 `변경`, `결정`, `검증`, `계획`만 허용한다. 알 수 없는 인자·잘못된 enum·범위 밖 수치는 거부한다. |
| 페이지 | `limit` 기본 **10**, 1~50 정수. `offset` 기본 0, 0~100,000 정수. total은 필터 결과 건수, limit은 요청/기본 건수, returned는 실제 건수다. nextOffset은 남았을 때 `offset + returned`, 끝이면 null. offset이 total 이상이면 빈 목록·nextOffset null. 뒤 페이지는 앞 응답의 expectedHash 필수. 필터를 바꾸면 offset 0부터 다시 시작한다. |
| 응답 | 서버가 작성한 `{ content, structuredContent, isError }`를 JSON.stringify한 UTF-8 크기 **16,384 bytes(16 KiB)** 이하. content의 text 한 블록은 structuredContent envelope의 JSON 문자열로 고정해 양쪽을 모두 계산한다. JSON-RPC envelope/id, SDK가 붙이는 resultType·metadata 및 SDK 자체 거부 결과는 측정 대상에서 구분한다. |
| 목록 예산 | 기본 목록은 위 측정 기준 **8,192 bytes(8 KiB)**를 목표로 limit보다 적게 반환할 수 있다. 단일 요약이 8 KiB를 넘으면 16 KiB 안에서 한 건을 반환한다. 16 KiB에도 안 들어가면 오류다. 최대 50개 요청도 16 KiB 상한을 우선한다. |
| 초과 | 실제 반환 건수에 맞게 nextOffset을 계산해 누락/무한 반복을 막는다. preview 생략은 item의 truncatedFields로 표시한다. 단일 상세가 들어가지 않으면 내용 없는 RESPONSE_TOO_LARGE 오류를 반환한다. 잘린 상세를 정상 전체 결과라고 표시하지 않는다. |
| 원본 | 기존 최대 **2,097,152 bytes(2 MiB)** 유지. MCP read는 제한 + 1 bytes 이내에서 초과를 판정해 증가 중인 파일을 무제한 읽지 않는다. |
| 호출량 | 프로세스별 동시 tools/call 처리 최대 2, 대기열 0. 별도로 단조 시계의 token bucket을 적용해 capacity 10, 초당 10회 충전으로 제한한다. 초과는 RATE_LIMITED와 retryAfterMs(상한 1,000)를 반환한다. 취소/실패/정상 종료 모두 슬롯을 반환한다. 클라이언트 두 개는 별도 프로세스이므로 전역 quota·공유 상태를 추가하지 않는다. |

한도 축소는 메인 기술 결정이며 **16 KiB 상한 / 기본 8 KiB 목표**는 Astra의 구체 수치 선택이다. [Claude Code의 공식 출력 제한](https://code.claude.com/docs/en/mcp#mcp-output-limits-and-warnings)은 경고 10,000 tokens·기본 최대 25,000 tokens다. 64 KiB는 다국어·escaping·중복 표현을 고려하면 여유를 설명하기 어렵고, 8 KiB를 모든 상세의 강제 상한으로 삼으면 불필요한 거부가 늘 수 있어 16/8 KiB를 선택했다. bytes는 tokens와 동치가 아니므로 특정 tokenizer·host rendering에서 경고가 없다고 미리 보장하지 않는다. 정본 기준 각 도구의 기본/최대 페이지·최대 상세 응답 bytes와 실제 wire overhead를 독립 검증에 기록하고, 실제 client 연결 시 토큰/파일 대체 여부를 별도로 관측한다. 출력 제한을 높이는 설정이나 vendor별 우회 annotation은 추가하지 않는다.

[2025-11-25 도구 규격](https://modelcontextprotocol.io/specification/2025-11-25/server/tools)과 [2026-07-28 도구 규격](https://modelcontextprotocol.io/specification/2026-07-28/server/tools)의 입력 검증·구조화 결과·호출 빈도 제한·오류 원칙을 따른다. 위 byte/건수/빈도 수치는 프로젝트 선택이지 규격의 지정값이 아니다. 현대 규격의 resultType 등 SDK 부가 필드와 application result 크기를 혼동하지 않는다. 기능 설명/schema 자체에 catalog 원문을 넣지 않는다.

### D3. 읽기 오류와 버전 충돌

성공 envelope는 `{ ok: true, snapshot: { hash, revision, asOf, sourceCommit }, data }`, domain 오류는 `{ ok: false, snapshot, error: { code, message, retryable, details? } }`이며 오류에는 data가 없다. 각 도구에 이 성공/오류 구조의 outputSchema를 선언하고 structuredContent와 동일 JSON의 text 한 블록을 항상 병행한다. 성공은 isError=false, domain 오류는 true다. SDK가 오류 outputSchema 검증을 생략하더라도 독립 테스트가 오류 계약을 검증한다.

hash는 **UI와 MCP가 같은 함수 하나**로 계산한다. 기존 UI 의미를 보존해 UTF-8로 decode한 문자열을 다시 UTF-8로 인코딩해 SHA-256을 계산한다. JSON 정렬/정규화는 하지 않으며 유효하지 않은 UTF-8에서 raw byte hash와 다를 수 있음을 기록한다. revision/asOf/sourceCommit은 catalog 값을 보존하며 현재 시각·서버 build commit으로 바꾸지 않는다. 검증된 snapshot이 없는 오류는 snapshot:null이다. 긴 metadata로 상한을 넘으면 snapshot:null과 details.metadataOmitted=true를 반환한다.

**strict 입력 schema를 채택한다.** 알 수 없는 속성을 제거하는 기본 object 대신 추가 속성을 거부하는 schema를 사용한다. SDK가 handler 이전에 거부한 입력은 SDK의 isError/프로토콜 거부 형태를 별도 채널로 기록하며 위 domain envelope를 보장하지 않는다. 공통 관찰 조건은 **오류 반환이며 정상 data 없음**이다. SDK 자체 거부가 과대 입력 원문·절대 경로 등을 반사하거나 비정상 크기로 증가하는지는 첫 smoke에서 확인해 문제 시 메인에 보고한다. 정상 handler에 도달한 의미 검증은 INVALID_ARGUMENT로 통일한다.

판정 순서는 **프로토콜/strict schema → 동시 처리·빈도 제한 → 의미 인자(뒤 페이지의 VERSION_REQUIRED 포함) → 원본 I/O·크기·읽기 변경 → JSON/스키마·참조 검증 → expectedHash 일치 → ID/systemId 존재 → 검색·응답 크기**다. 예를 들어 잘못된 입력은 catalog를 읽지 않고, 손상된 catalog는 VERSION_CONFLICT나 NOT_FOUND로 가리지 않는다. rate 제한을 받지 않는 같은 조건의 정상 요청에서 결과 결정성을 확인한다. 취소는 각 await 경계에서 우선 중단하며 성공 data를 반환하지 않는다.

각 도구는 선택적 `expectedHash`(64자리 소문자 hex)를 받는다. 현재 정상 snapshot의 hash와 다르면 `VERSION_CONFLICT`와 요청 hash/현재 snapshot metadata만 반환하고 조회 data는 반환하지 않는다. 과거 snapshot 저장·자동 재시도·최신 결과 혼합은 없다. 호출자가 새 목록부터 다시 읽어 버전을 선택한다. 앞 페이지에서 받은 hash로 이후 상세도 고정할 수 있다. hash가 같고 조건이 같으면 두 client의 조회 결과도 같아야 한다.

| 오류 code | 의미와 후속 행동 |
|---|---|
| `INVALID_ARGUMENT` | query/ID/페이지/hash 등 입력을 수정해야 한다. SDK schema 거부도 오류이며 정상 빈 결과로 바꾸지 않는다. |
| `CATALOG_MISSING` / `CATALOG_UNREADABLE` | 원본 부재 또는 접근/I/O 실패. rename 구간의 일시 접근 실패도 UNREADABLE, retryable=true이며 서버 내부 자동 재시도는 없다. 빈 catalog·오류 없음으로 표시하지 않는다. |
| `CATALOG_TOO_LARGE` / `CATALOG_INVALID` | 크기·JSON·스키마·중복 ID 오류. 자동 복구/초기화하지 않는다. |
| `CATALOG_REFERENCE_BROKEN` | 끊긴 연결을 명시한다. 식별 가능한 오류 예시 최대 5개와 전체 개수만 반환한다. |
| `CATALOG_CHANGED_DURING_READ` | 읽는 동안 변경 감지. 결과를 버리고 호출자가 재요청한다. |
| `NOT_FOUND` | 정상 snapshot에 요청 ID가 없다. 연결된 원문 부재와 혼동하지 않는다. |
| `VERSION_CONFLICT` / `VERSION_REQUIRED` | 버전 불일치 또는 뒤 페이지에 hash 누락. 자동으로 최신 페이지를 섞지 않는다. |
| `RESPONSE_TOO_LARGE` | 최소 한 항목/상세/metadata가 한도에 들어가지 않는다. 본문을 제외하고 생략 사실을 알린다. metadata도 넘치면 snapshot null과 metadata 생략 사유를 준다. |
| `RATE_LIMITED` | 동시 처리 또는 호출 빈도 한도 초과. retryable=true, details.retryAfterMs 제공. |
| `REQUEST_CANCELLED` | handler가 취소를 관측했다. 응답 채널이 열려 있다면 이 오류를 반환하고, SDK가 응답을 억제/연결을 닫았다면 성공 data 없음과 자원 반환을 시험한다. |

domain 오류 message는 최대 256 UTF-16 code units의 고정 안내를 사용하고 입력 문자열·raw JSON·절대 경로·stack·OS 오류 원문을 반사하지 않는다. missing/unreadable/changed/rate-limit의 retryable은 true, 나머지는 false(입력/원본 수정 또는 새 버전 선택 필요)다. reference 오류 details는 예시 최대 5개와 전체 개수이며 예시 ID도 길이를 제한해 응답 예산을 지킨다. VERSION_CONFLICT의 details는 expectedHash, VERSION_REQUIRED는 hash 필요 안내다. 프로토콜 오류는 SDK 계약으로 구분한다. [공식 오류 안내](https://ts.sdk.modelcontextprotocol.io/v2/servers/errors.html)를 기준으로 outputSchema와 SDK 자체 입력 거부도 독립 검증한다.

### D4. 첫 client 연결

**먼저 설정을 변경하지 않는 SDK stdio 시험 client 두 개**가 동일한 빌드된 entry를 각자 child process로 실행한다. production 경로 점검은 정본의 읽기만 수행하고 변경·손상·경합 시험은 공통 fixture를 주입하는 별도 시험 entry를 사용한다. UI·Orca 화면·HTTP·API key는 이 시험의 전제가 아니다. modern은 `versionNegotiation: { mode: { pin: '2026-07-28' } }`로 fallback 없이, legacy는 기본 legacy mode와 supportedProtocolVersions의 2025-11-25 제한으로 각각 시험한다. 협상 revision·client/서버 version·원본 hash·요청/응답·정상/취소/EOF 종료 관측을 기록한다. 아직 시험하지 않은 다른 과거 revision을 지원 검증 완료로 표시하지 않는다.

프로젝트 안내에는 `node <management-active>/05_Management/<빌드된 MCP entry>`의 command/args를 명시한다. 패키지 설치·빌드는 사전 단계로 분리해 stdio 시작 중 npm 출력·자동 다운로드를 섞지 않는다. 두 개발 worktree의 각 catalog를 따로 읽도록 예시를 만들지 않는다. 실제 entry 경로는 구현 후 안내와 독립 실행에서 확정한다.

**정본 범위 — 확정:** 메인 메시지 `msg_f37d2837f0b1`로 전달된 사용자 결정은 **management-active 작업 트리의 `05_Management/records/catalog.json`**이다. Management가 checkout한 branch의 미병합 편집도 다른 세션에 그대로 보이며, 응답 hash/revision으로 읽은 버전을 구분한다. main 병합본만 보이는 별도 고정본을 추가하지 않는다. 이 결정은 메인이 전달한 사용자 결정이며 사용자 직접 입력으로 취급하지 않는다.

**branch 전환 또는 MCP/공통 코드 변경 뒤에는 재빌드하고 client의 MCP child process를 다시 시작**한다. catalog 데이터만 수정하면 다음 조회에서 즉시 반영한다. serverInfo.version은 package의 상수 0.0.0만 쓰지 않고 빌드 시 MCP/공통 입력 소스·build 설정·lockfile의 digest를 포함해 빌드 실체를 식별한다. 실제 build 명령/시점/digest를 시험 근거에 남기며 catalog의 sourceCommit과 혼동하지 않는다. Git 제외 옛 산출물이 존재한다는 이유로 최신 source의 구현이라고 보고하지 않고, branch 전환 뒤 옛 프로세스·옛 산출물 사용은 안내상 지원하지 않는다.

**사용자 결정 항목:** 시험 통과 후 실제 Game Dev/Management 세션을 어느 방식으로 연결할지 메인에게 올린다. 추천은 한시적 세션 연결 예시(Codex `-c` override / Claude `--mcp-config`)를 검토한 뒤 적용하는 것이다. 영구 프로젝트 설정을 택할 경우 적용 checkout·파일·추가 entry의 실제 diff를 먼저 제시한다. 이 체크포인트에서 설정 파일을 만들거나 설정 명령을 실행하지 않는다. CLI override 역시 지금 적용된 것으로 보고하지 않는다.

[OpenAI MCP 안내의 최종 URL](https://learn.chatgpt.com/docs/extend/mcp?surface=cli)은 사용자/프로젝트 config.toml의 command·args 기반 stdio 설정을 설명한다. 원래 developers.openai.com/codex/mcp 링크는 이 페이지로 redirect됐다. 로컬 codex --help의 `-c`는 확인했지만 **이 MCP 안내에 `-c`로 mcp_servers를 넘기는 예시는 없으며 실제 수용/연결도 미확인**이다. [Claude Code MCP 안내](https://code.claude.com/docs/en/mcp)는 `.mcp.json`과 `--mcp-config`를 설명한다. 한시적 Claude 연결 시 다른 MCP를 함께 쓸지, `--strict-mcp-config`로 명시한 서버만 쓸지는 연결 예시 검토 때 정한다. 기존 MCP를 임의로 비활성화하거나 승인 절차를 우회하지 않는다. **SDK 시험 성공은 실제 두 개발 세션에 도구가 연결됐다는 뜻이 아니다.**

## 완료조건과 독립 검증

| 합의 번호 | 관찰 가능한 완료조건과 근거 |
|---|---|
| 1 | 두 client가 D2의 도구/DTO 계약으로 목록/검색 → 시스템 → 관련 기록(systemId 필터 포함) → 출처를 탐색한다. text JSON과 structuredContent의 동등성, outputSchema, 고정 정렬/필터/다음 페이지의 무누락을 구현과 독립적으로 시험한다. |
| 2 | 응답 hash/revision/asOf/sourceCommit이 해당 fixture/정본과 일치한다. 과거 catalog 시각·출처가 현재 시각/HEAD로 바뀌지 않는다. 원문 미조회 표시를 확인한다. |
| 3 | 단일 snapshot, 동일 hash/조건의 두 client 결과 일치, 재빌드 없는 catalog 갱신, 이전 hash 상세/페이지의 충돌, D3 오류 우선순위를 시험한다. 주입 I/O로 변경·증가·손상을 결정적으로 재현하고 실제 Windows I/O에서 MCP 반복 조회 중 createCatalogStore.save가 성공하는지 시험한다. 실패 code/시도 수/빈도를 남기며 저장 간섭이 확인되면 보존 회귀 실패로 돌린다. 읽기 구간의 UNREADABLE은 명시적 오류와 다음 요청 회복을 확인한다. |
| 4 | 경계값·과대 입력·긴 ID·누락/손상/중복/참조·권한 오류·다국어/escaping·16 KiB 상한·기본 8 KiB 목표·페이지/oversized item·rate/동시 처리 한도를 시험한다. production entry에 원본 선택용 argv/env를 줘도 응답 hash는 module 기준 원본이며 fixture factory가 production 원본 선택 통로가 되지 않음을 확인한다. 도구의 임의 path/URL/알 수 없는 키는 오류를 반환하고 정상 data가 없어야 한다. 쓰기/네트워크 경계와 fixture 전후 보존도 확인한다. |
| 5 | 앱 없이 실제 stdio child process 두 개로 legacy 2025-11-25와 pin한 modern 2026-07-28의 연결·목록·읽기·검색을 수행한다. stdout의 모든 비어 있지 않은 줄은 JSON-RPC 메시지다. 정상 EOF/close는 5초 안 exit code 0을 기대하며 timeout에 의한 강제 종료는 실패 정리로 구분한다. 주입형 지연에서 중간 취소 후 성공 data 없음·handle/슬롯 반환·다음 조회 정상, 한 client 종료 시 다른 client 보존을 확인한다. 실제 개발 세션 연결은 별도 관측이다. |

검증은 **신규 Opus 세션 3개를 순차**로 배정한다. 한 세션이 끝나 정산·종료한 다음 세션을 열며 기존 세션을 재사용하지 않는다. 세션별 테스트 파일 쓰기 소유권을 spec에 명시하고 이전 세션의 테스트는 후속이 읽고 실행할 수 있으나 허가 없이 수정하지 않는다.

| 순서 | 검증 한 작업의 범위 | 허용 쓰기/산출물 |
|---|---|---|
| V1 계약·실패 경계 | 실제 diff/구현자 보고 실사, D2/D3의 단위·fixture·결정적 I/O/취소·hash·rate 계약 시험 | `tests` 아래 계약/reader 시험 및 독립 fixture builder. 제품 factory 부족은 결함으로 반환. 원문 v1-review.md와 실행 근거. |
| V2 stdio 통합 | 두 실제 child process·두 revision·SDK 거부·stdout/EOF/취소·Windows rename/save 경합·production 경로 비확장·응답 bytes | 별도 stdio 시험 entry/통합 테스트. V1 factory/fixture를 읽어 사용. 원문 v2-review.md와 wire/종료/최대 bytes 근거. |
| V3 UI·build 회귀 | npm test, typecheck/build, desktop typecheck/build, 저장/백업/잠금/충돌, 아래 임시 사본 Electron 실사와 build 경로 대조 | 기존 UI 회귀 테스트의 필요한 보완과 build 경로 시험. 원문 v3-review.md, GUI 수행/미실행·원본 보존 근거. |

V3는 05 트리를 **저장소 밖 고유 TEMP 경로의 사본**으로 복사해 원래 상대 배치를 유지하고 그 사본의 catalog·백업·profile만 쓰는 Electron을 실행한다. 정본의 userData/profile·catalog를 재사용하지 않는다. Git·에이전트 설정·기존 검증 로그를 사본으로 옮기지 않으며 잠금으로 실행자 소유를 표시한다. 검증자가 보이는 시험 창에서 조회·새로고침·저장·미저장 초안 보존을 확인하고 자기 시험 프로세스만 종료한다. GUI 조작 수단·실행 환경이 불가능하면 이를 **Electron GUI 미실행**으로 보고하고 vitest 회귀와 산출물 경로 점검으로 한정한다. GUI를 mock 시험으로 통과했다고 표시하지 않는다. 임시 디렉터리 정리는 절대경로가 이 시험 소유 사본 안인지 확인한 후 수행한다.

V3는 **빌드된 Electron main의 실제 위치·package main과 빌드된 MCP entry가 해석하는 catalog 절대경로**가 동일한 05 원본을 가리키는지 사본에서도 확인한다. Electron 산출물을 실행/로드해 부작용을 만들기 전에 정적으로 경로를 대조한다. `electron/catalog-contract.ts`를 UI가 재수출하는 기존 방식처럼 rootDir를 유지하는 배치가 가능하며 Sol은 변경 사유가 없으면 이 경로를 보존한다. 검증자는 제품/설정/package를 수정하지 않고 부족한 경계는 새 Sol 수정과 신규 Opus 재검증으로 돌린다.

루트 .NET 빌드는 이 목표에 필요하지 않으며 Shared DLL 복사 등 부작용을 만들지 않는다. Unity 플레이·게임 서버·DB·실제 개발 세션 연결을 실행하지 않았으면 미실행으로 남긴다. 기존 테스트 통과나 문서 조사만으로 위 완료조건을 충족했다고 판정하지 않는다.

## 작업 순서와 소유권

1. Management Astra가 이 goal과 공식 근거 기반 제안을 커밋한다. 이어 아래 Fable의 계획 검토 시범을 수행하고 판정 원문을 메인에게 전달해 goal 확인을 받는다. 확인 전 Sol은 발행하지 않는다.
2. 확인 후 외부 `gpt-6.1-sol` 구현자에게 05 제품 코드·내부 시험 factory·필요한 package/build 설정을 위임한다. client 2.2.0 exact devDependency, server 2.2.0/zod 4.6.5 직접 dependency·lockfile과 **첫 smoke 보고**를 spec에 명시한다. 기존 테스트는 읽기 전용, 신규 독립 하니스/테스트는 Opus 소유이며 goal/Git·검증 판정은 Astra 소유다. smoke 자체 점검은 TEMP 명령/짧은 실행으로 기록하고 독립 테스트를 대신 작성하지 않는다.
3. Sol의 쓰기 종료·보고·정산 후 신규 외부 `claude-opus-5-5`의 V1 → V2 → V3를 순차 수행한다. 매번 실제 diff/원시 근거를 먼저 실사하며 각자 할당된 테스트 파일만 작성·보완한다. 제품 결함은 번호로 반환하고 결함이 있으면 의존 검증을 진행하기 전에 새 Sol에서 고친다. 수정 Sol·재검증 Opus는 매번 신규이며 같은 결함의 3회 재검증 실패는 메인에 올린다.
4. Astra가 원문 판정·근거·미실행 범위를 이 goal에 기록하고 05 탐색 문서를 필요한 만큼 갱신한다. 문서 검토까지 통합해 commit/push/PR을 담당한다. PR 생성은 허용되지만 **해당 PR 병합 직전 사용자 명시 승인**이 필요하다. 자동 병합은 금지한다.

이번 세션의 메인 지시로 Management Astra는 독립 Management 탭에 있다. 작업자 pane은 이 탭의 Astra 아래 split에 열고 `worker-start --terminal`로 연결한다. 이전 Game Dev 탭의 mismatch 관찰을 현재 성공으로 재사용하지 않는다. 실제 split/readiness/attach receipt를 저장하고 거부되면 residual 자원·미발행 상태를 확인한 뒤 승인된 새 탭 대안을 사용한다. 상세 절차는 [Orca 위임 지침](../../../.agents/skills/dawnholder-goal-loop/references/orca-work.md)을 발행 전에 읽는다.

모든 작업자·검증자는 작업 하나 후 정산·종료하고 재사용·추가 위임하지 않는다. 파트당 검증자는 동시에 하나만 열며 같은 파일 동시 쓰기를 금지한다. 세션 모델은 요청/launch·최초 실행 명령/화면/백엔드를 구분한다. 현재 Astra 화면은 `GPT-6-Astra xhigh`, 실제 백엔드 모델은 `unknown`이다. Sol/Opus는 아직 발행하지 않아 launch/화면 근거가 없다.

### Fable goal 검토 시범

메인 후속 메시지 `msg_f224c8b5b682`(2026-10-01)가 사용자 승인에 따른 이번 계획 검토 한정 예외를 전달했다. 사용자 직접 입력으로 격상하지 않으며 제품 구현 Sol·독립 테스트/검증 Opus 배정은 유지한다.

- goal 커밋 후 Management 탭의 Astra 아래 `--direction vertical` split에서 최초 명령 `claude --model claude-fable-5-1`로 새 세션을 연다. 요청 모델·최초 명령·화면 표시·준비 상태를 저장한다. backend는 `unknown`이며 모델이 안 뜨거나 다른 모델로 표시되면 대체하지 않고 메인에 보고한다.
- `tui-idle` 확인 뒤 `worker-start --terminal`로 최초 작업을 연결하고 성공/거부 receipt를 보존한다. 발신 태그는 `[Management 검증자]`, subject에는 `goal 검토`를 넣는다.
- 검토는 합의 완료조건 1~5 및 다음 결정의 누락/초과, 관찰·시험 가능성, D1~D4 근거와 대안, 임의 경로/응답 폭주/버전 혼합 위험, 소유권과 시험 계획의 실행 가능성을 비판적으로 평가한다. 구현 성공이나 독립 제품 테스트를 판정하는 작업은 아니다.
- 검토자에게 허용한 쓰기는 로컬 `.backups/verification/2026-10-01-shared-read-mcp/goal-review.md` 하나다. goal·저장소 파일 수정, Git 작업, 추가 위임은 금지한다. 첫 줄은 **승인 가능** 또는 **수정 필요**, 지적마다 번호·심각도·근거 위치·수정 방향을 기록하며 사용자 결정 질문은 따로 모은다.
- 완료 후 정산·종료하고 재사용하지 않는다. Astra는 판정 원문 경로를 메인에게 보낸다. **메인이 원문을 읽고 goal을 확인하기 전에는 Sol을 발행하지 않는다.**

## 현재 상태와 근거

**Fable 계획 검토 지적 17건과 메인 후속 결정을 반영한 goal 보완 완료, 메인 diff 확인·goal 승인 대기. 구현·독립 제품 검증 미착수.** 최초 Fable 판정은 수정 필요이며 이 보완안을 승인했다고 해석하지 않는다.

- 실제 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`.
- 시작: clean `main`, HEAD `18c8ca6a5aa3032873029cbd36658f0c4f9095c5` (PR156). `git fetch origin main` 후 origin/main도 같은 SHA임을 확인했다.
- 작업 브랜치: `feat/management-shared-read-mcp`, 위 origin/main에서 새로 생성. 첫 커밋 대상은 이 goal 한 파일이다. 원격 push·PR·병합은 아직 수행하지 않았다.
- 현재 runtime `8a673084-6819-45b9-a551-347226cdce9b`, Astra terminal `term_6df8363a-d0bf-454f-aa67-7c7c7323008a`, incarnation `3068c493-4095-4e8c-b907-eed3f3b741e6`. Orca worktree 소속과 실제 cwd 모두 management-active로 확인했다. 이 값은 관찰 기록이며 향후 실행 권한이 아니다.
- READY 회신 `msg_46884ca17d25`를 메인에게 enqueue했다. enqueue 성공을 메인이 읽거나 승인했다는 근거로 쓰지 않는다.
- 현행 코드·문서의 좁은 정적 조사, 버전/registry metadata 조회, 공식 문서 확인과 Fable 계획 검토를 수행했다. package 설치·MCP/앱 실행·테스트·빌드·독립 제품 검증·설정 변경은 수행하지 않았다.
- 원시 receipt/로그와 판정 원문은 Git 제외 `.backups/verification/2026-10-01-shared-read-mcp/`에 보존한다. 계획 판정은 아래와 같고 구현/제품 검증 판정은 아직 없다. 원문은 로컬 근거이며 원격 가용성을 보장하지 않는다.

남은 선행 단계는 메인의 보완 diff 확인·goal 승인이다. 정본 범위는 확정됐고 응답 축소와 SDK 조건부 채택은 메인 기술 결정으로 처리했다. D4 실제 개발 세션 연결/설정 적용은 시험 후의 결정 항목으로 남는다. 구현·SDK 설치/호환성·회귀는 아직 검증되지 않았다.

### Fable 계획 검토 결과 — 검토 후 기록

검토 대상은 첫 goal 커밋 `8875c6b`다. Fable은 해당 작업 트리 파일을 읽고 **수정 필요, 높음 1·중간 8·낮음 8건**을 반환했다. 합의 완료조건 누락과 치명적 설계 결함은 찾지 못했다고 보고했다. 높음 1건은 실제 stdio fixture 주입·경합 재현·취소 지연에 필요한 제품 쪽 시험 경계를 발행 전에 정하라는 지적이다. 중간 지적은 도구별 입출력/오류 순서, UI 저장과 읽기의 간섭, 응답 예산, SDK 입력 거부, Electron fixture 실행, 정본/빌드 식별, 산출물 경로, client 의존성 소유를 구체화하는 내용이다.

판정 원문은 `.backups/verification/2026-10-01-shared-read-mcp/goal-review.md`이며 Astra가 전체를 읽었다. 완료 원문은 `fable-completion.json`이다. Fable 당시 질문 3건은 정본의 작업 트리 범위, 64 KiB 수치, SDK 2.2.0 고정 수용이며 D4의 연결 방식 질문도 유지했다. 당시 질문은 검토자 제안으로 기록했고 이후 메인의 처리와 보완은 아래에 구분한다.

Fable은 일부 외부 출처를 WebFetch 요약으로 확인했으며 Windows rename·SDK 실행·출력 token 수 등 미확인 범위를 원문 7절에 명시했다. 검토자의 Git 금지 범위 때문에 commit 바이트 대조는 하지 않았고, Astra가 최초 기록 수정 직전에 `git diff --exit-code 8875c6b -- <goal 경로>` exit 0으로 검토 기간 중 goal의 미변경을 확인했다. `ed465b0`의 상태·정산 기록은 기록 전용이며 Fable 판정 밖이었다. **그 뒤 메인 지시로 수행한 아래 기술 계약 보완도 Fable의 기존 판정에 포함되지 않는다.** 코드·설정·catalog 변경이나 제품 시험은 없었다.

Run `run_178353cf7ce2`, Task `task_caa031611206`, Dispatch `ctx_dec63c9a6dd9`다. Management 탭 아래 vertical split의 최초 `worker-start --terminal` 연결이 실제 성공했고 `turn_started`를 확인했다(`fable-worker-start.json`). 최초 실행은 `claude --model claude-fable-5-1`, 화면은 Fable 5.1 xhigh, backend는 `unknown`이다. `--terminal` 연결의 launch model=null을 모델 근거로 사용하지 않았다. 처음의 effort 선택창은 메인이 Keep xhigh로 처리했다고 통지했으며 동일 runtime/handle/incarnation·빈 prompt·tui-idle을 다시 확인한 뒤 attach했다(`main-effort-resolution.json`, `fable-ready-*.json`, `fable-launch-observations.md`).

계획 판정과 별개로 검토 작업은 `worker_done outcome=succeeded`로 정상 정산됐다. release는 `retained / external_terminal / processAction none`이어서 같은 incarnation의 완료·빈 prompt를 확인하고 이 작업 pane만 닫았다. `fable-pane-close.json`은 `ptyKilled=true`다. 완료 Delivery `delivery_a49d1ce1d4d4`를 acknowledge했고 reclaimable 목록은 0개다. 세션은 재사용하지 않는다. 메인의 원문 확인과 goal 확인 전 Sol은 발행하지 않는다.

### 메인 원문 확인과 goal 보완

`msg_d72a957a219c`에서 메인은 Fable 원문 247줄을 직접 읽고 지적 17건에 동의했으며 #8의 build 경로도 실제 코드로 대조했다고 통지했다. Astra는 이를 근거로 아래 계약을 보완했다. 메인의 확인 통지를 Astra 자신의 독립 제품 검증으로 바꾸지 않는다. `msg_f37d2837f0b1`의 사용자 결정으로 정본 대기를 해제했으며 실제 세션 연결 설정 권한은 추가되지 않았다.

| 지적 번호 | 이번 보완과 확인할 결과 |
|---|---|
| #1 | 경로/I/O/signal을 받는 내부 reader·서버 factory와 검증자 소유 fixture entry, 주입형 지연 확정. production argv/env 비확장 완료조건 추가. |
| #2·5 | 도구별 입력/DTO/outputSchema·text 병행·오류 우선순위와 strict schema 채택. SDK 선행 거부는 별도 채널, Sol 첫 smoke 보고. |
| #3 | handle 보유 최소화, rename 읽기 실패는 명시 오류/내부 재시도 없음, 실제 Windows 반복 조회 중 UI save 회귀. |
| #4·13 | 64 KiB를 16 KiB로 축소, 기본 8 KiB·10건. SDK 부가 필드와 측정 대상 구분, 정본 최대 bytes 관측을 V2에 배정. |
| #6·8 | 저장소 밖 05 사본에서 V3 GUI 실사, 불가 시 미실행 구분. Electron 진입점과 두 entry의 catalog 절대경로 보존/점검. |
| #7 | 정본 작업 트리 확정·미병합 편집 가시성, branch 전환 뒤 재빌드/재시작, serverInfo.version의 build digest. |
| #9·10·11 | Sol이 server/zod 직접 dependency와 client exact devDependency를 추가. 2.2.0 게시일·2.1.0 대안·조건부 smoke 명시. |
| #12·15·16 | 프로세스별 rate/동시 처리 제한, UI/MCP 단일 hash 함수·code unit 정렬·locale 한계·긴 ID 표시, modern pin/legacy revision 확인. |
| #14·17 | Codex 최종 URL과 CLI MCP override 미확인, Claude strict 옵션은 후속 선택. V1/V2/V3 신규 세션 순차 배정·stdout/EOF/취소 종료 관측. |

이 보완은 문서 작업이며 제품 코드·설정·catalog는 변경하지 않았다. SDK·GUI 실행은 전부 계획이다. 메인이 보완 commit diff를 확인해 goal을 승인할 때까지 Sol을 발행하지 않는다.
