# M-1 — 개발 기록 공동 조회 MCP

## 목표와 재개 근거

사람의 Management UI와 Game Dev/Management 에이전트가 같은 `05_Management/records/catalog.json`을 필요한 만큼 조회한다. 로컬 stdio MCP와 공통 조회 모듈을 만들고 앱이 꺼진 상태에서 독립 client 두 개로 조회·버전 일치·종료를 검증한다. 조회는 목록/검색 → 시스템 요약 → 관련 변경·결정·검증·계획 → 출처 메타데이터 순서다.

2026-10-01 메인 Claude의 Orca 메시지 `msg_a873e160a5bc`가 M-1 재개 결정을 전달했다. 발신 `term_6505bda3-c071-476a-a50a-755c10fa02eb`를 살아 있는 메인 Claude terminal과 대조했다. **메인이 전달한 사용자 결정이며 사용자 직접 입력으로 격상하지 않는다.** [R-10](../../requirements.md#r-10)의 MCP 구현 보류는 이 목표 범위에서 해제한다. 서버 등록·로그 조회와 쓰기·실행 제어는 계속 후속 범위다.

기준은 [공동 조회 완료조건 1~5 및 다음 결정](../2026-09-30-system-records/shared-read-agreements.md)이다. 구현 순서·화면 없는 조회·동일 읽기 권한·공통 원본은 확정 사항으로 다시 묻지 않는다. 해당 합의의 과거 역할·모델·CLI 지정 대신 현재 [AGENTS](../../../AGENTS.md)를 적용한다. 이번 목표의 기준·상태·결과는 이 파일에 모으며 root CURRENT는 Game Dev 소유이므로 변경하지 않는다.

## 범위와 보존 계약

- 허용: 05 안의 읽기 전용 MCP, UI와 공유하는 catalog 계약·검증·읽기·검색 모듈, 필요한 05 package/TypeScript 설정, 독립 테스트와 실행·연결 안내. requirements/decisions/README/RESUME는 이 목표로 연결하는 데 필요한 부분만 후속 갱신한다. 첫 체크포인트는 이 goal 작성·커밋뿐이다.
- MCP에는 catalog 조회 도구만 등록한다. 쓰기·서버 조작·shell·외부 URL 요청·임의 파일 경로 읽기·원문 파일 열기를 노출하지 않는다. 도구의 path/URL 인자, client roots에 따른 읽기 범위 확장, 자동 source locator 해석을 두지 않는다. 읽기 전용 표식은 설명이며 실제 보장은 서버의 등록 도구·의존성·파일 접근 경계와 독립 테스트로 확인한다.
- catalog 본문·기존 ID·출처·`asOf`·`sourceCommit`을 이번 구현으로 최신화하지 않는다. catalog에 적힌 게임 상태와 실제 현재 게임 상태를 구분한다. 기존 UI JSON 불러오기·수정·저장·백업·잠금·낙관적 충돌 처리와 미저장 초안을 보존한다.
- Windows 읽기/저장 간섭 보완은 메인 `msg_f1bd05219af4`의 명시 범위로 한정한다. UI 저장 rename의 일시 오류에만 총 1초 이내 제한된 재시도를 적용하고, 소진 시 기존 저장 실패를 반환한다. 아래 D3와 실제 재현 근거를 따른다.
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
| 호출량 | 프로세스별 동시 tools/call 처리 최대 **4**, 대기열 0. 별도로 단조 시계의 token bucket을 적용해 capacity 10, 초당 10회 충전으로 제한한다. 초과는 RATE_LIMITED와 retryAfterMs(상한 1,000)를 반환한다. 취소/실패/정상 종료 모두 슬롯을 반환한다. 클라이언트 두 개는 별도 프로세스이므로 전역 quota·공유 상태를 추가하지 않는다. |

한도 축소는 메인 기술 결정이며 **16 KiB 상한 / 기본 8 KiB 목표**는 Astra의 구체 수치 선택이다. [Claude Code의 공식 출력 제한](https://code.claude.com/docs/en/mcp#mcp-output-limits-and-warnings)은 경고 10,000 tokens·기본 최대 25,000 tokens다. 64 KiB는 다국어·escaping·중복 표현을 고려하면 여유를 설명하기 어렵고, 8 KiB를 모든 상세의 강제 상한으로 삼으면 불필요한 거부가 늘 수 있어 16/8 KiB를 선택했다. bytes는 tokens와 동치가 아니므로 특정 tokenizer·host rendering에서 경고가 없다고 미리 보장하지 않는다. 정본 기준 각 도구의 기본/최대 페이지·최대 상세 응답 bytes와 실제 wire overhead를 독립 검증에 기록하고, 실제 client 연결 시 토큰/파일 대체 여부를 별도로 관측한다. 출력 제한을 높이는 설정이나 vendor별 우회 annotation은 추가하지 않는다.

[2025-11-25 도구 규격](https://modelcontextprotocol.io/specification/2025-11-25/server/tools)과 [2026-07-28 도구 규격](https://modelcontextprotocol.io/specification/2026-07-28/server/tools)의 입력 검증·구조화 결과·호출 빈도 제한·오류 원칙을 따른다. 위 byte/건수/빈도 수치는 프로젝트 선택이지 규격의 지정값이 아니다. 현대 규격의 resultType 등 SDK 부가 필드와 application result 크기를 혼동하지 않는다. 기능 설명/schema 자체에 catalog 원문을 넣지 않는다.

### D3. 읽기 오류와 버전 충돌

**Windows UI 저장 간섭 보완:** TEMP 제어군 저장 100/100 성공 대비, production reader를 100ms 간격으로 병행한 사본에서 저장 99/100·write 실패 1건이 보고돼 메인이 허용한 bounded retry를 적용한다. 대상은 UI 저장 rename의 `EPERM`·`EACCES`·`EBUSY`뿐이며 총 1초 이내 backoff 후 소진 시 기존 저장 실패다. 1초는 새 재시도의 시작 기한이며 이미 대기 중인 native I/O의 완료 시간을 강제로 제한하지 않는다. 잠금·낙관적 충돌·백업·미저장 초안의 의미를 유지한다. MCP reader의 `CATALOG_UNREADABLE`은 retryable인 명시 오류로 두고 내부 자동 재시도를 추가하지 않는다. 이 재현은 구현자의 동일 reader 직접 시험이며 실제 stdio 조회 중 독립 저장 회귀와 시도 횟수 분포는 V2/V3에서 확인한다. 구체 backoff·최종 실행 근거는 아래 결과 기록을 따른다.

성공 envelope는 `{ ok: true, snapshot: { hash, revision, asOf, sourceCommit }, data }`, domain 오류는 `{ ok: false, snapshot, error: { code, message, retryable, details? } }`이며 오류에는 data가 없다. 각 도구에 이 성공/오류 구조의 outputSchema를 선언하고 structuredContent와 동일 JSON의 text 한 블록을 항상 병행한다. 성공은 isError=false, domain 오류는 true다. SDK가 오류 outputSchema 검증을 생략하더라도 독립 테스트가 오류 계약을 검증한다.

hash는 **UI와 MCP가 같은 함수 하나**로 계산한다. 기존 UI 의미를 보존해 UTF-8로 decode한 문자열을 다시 UTF-8로 인코딩해 SHA-256을 계산한다. JSON 정렬/정규화는 하지 않으며 유효하지 않은 UTF-8에서 raw byte hash와 다를 수 있음을 기록한다. revision/asOf/sourceCommit은 catalog 값을 보존하며 현재 시각·서버 build commit으로 바꾸지 않는다. 검증된 snapshot이 없는 오류는 snapshot:null이다. 긴 metadata로 상한을 넘으면 snapshot:null과 details.metadataOmitted=true를 반환한다.

**strict 입력 거부를 유지하며 오류 문구는 입력 원문을 반사하지 않는다.** 알 수 없는 속성을 제거하는 기본 object 대신 추가 속성을 거부한다. 메인 후속 지시 `msg_f9186b55efca`의 1안을 채택해 schema별 공식 오류 옵션으로 고정 문구를 지정한다. Zod의 strictObject와 필드 string/max/enum 옵션을 적용한 legacy/modern 보완 smoke가 통과했다. SDK가 먼저 strict 검증하며 handler에서만 검증하는 2안은 채택하지 않았다. SDK pin은 유지하고 node_modules 패치나 전역 오류 설정은 하지 않는다. SDK가 handler 이전에 거부한 입력은 SDK의 isError/프로토콜 거부 형태를 별도 채널로 기록하며 위 domain envelope를 보장하지 않는다. 공통 관찰 조건은 **오류 반환·정상 data 없음·입력 key/value 원문 미반사**다. 입력 거부 결과의 JSON UTF-8 크기는 SDK/handler 어느 경로든 입력 길이와 무관하게 **1,024 bytes 이하**인지 두 규격에서 확인한다. SDK 부가 필드를 포함한 client 관측 result를 측정하고 JSON-RPC wire overhead는 별도 기록한다. 정상 handler에 도달한 의미 검증은 INVALID_ARGUMENT로 통일한다.

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

모든 작업자·검증자는 작업 하나 후 정산·종료하고 재사용·추가 위임하지 않는다. 파트당 검증자는 동시에 하나만 열며 같은 파일 동시 쓰기를 금지한다. 세션 모델은 요청/launch·최초 실행 명령/화면/백엔드를 구분한다. 현재 Astra 화면은 `GPT-6-Astra xhigh`, 실제 백엔드 모델은 `unknown`이다. Sol과 신규 Opus V1의 발행 근거는 아래에 기록한다.

### Fable goal 검토 시범

메인 후속 메시지 `msg_f224c8b5b682`(2026-10-01)가 사용자 승인에 따른 이번 계획 검토 한정 예외를 전달했다. 사용자 직접 입력으로 격상하지 않으며 제품 구현 Sol·독립 테스트/검증 Opus 배정은 유지한다.

- goal 커밋 후 Management 탭의 Astra 아래 `--direction vertical` split에서 최초 명령 `claude --model claude-fable-5-1`로 새 세션을 연다. 요청 모델·최초 명령·화면 표시·준비 상태를 저장한다. backend는 `unknown`이며 모델이 안 뜨거나 다른 모델로 표시되면 대체하지 않고 메인에 보고한다.
- `tui-idle` 확인 뒤 `worker-start --terminal`로 최초 작업을 연결하고 성공/거부 receipt를 보존한다. 발신 태그는 `[Management 검증자]`, subject에는 `goal 검토`를 넣는다.
- 검토는 합의 완료조건 1~5 및 다음 결정의 누락/초과, 관찰·시험 가능성, D1~D4 근거와 대안, 임의 경로/응답 폭주/버전 혼합 위험, 소유권과 시험 계획의 실행 가능성을 비판적으로 평가한다. 구현 성공이나 독립 제품 테스트를 판정하는 작업은 아니다.
- 검토자에게 허용한 쓰기는 로컬 `.backups/verification/2026-10-01-shared-read-mcp/goal-review.md` 하나다. goal·저장소 파일 수정, Git 작업, 추가 위임은 금지한다. 첫 줄은 **승인 가능** 또는 **수정 필요**, 지적마다 번호·심각도·근거 위치·수정 방향을 기록하며 사용자 결정 질문은 따로 모은다.
- 완료 후 정산·종료하고 재사용하지 않는다. Astra는 판정 원문 경로를 메인에게 보낸다. **메인이 원문을 읽고 goal을 확인하기 전에는 Sol을 발행하지 않는다.**

## 현재 상태와 근거

**종료 확인(2026-10-06, 이후 goal 기록):** 종료 기록 [PR #161](https://github.com/bass131/dawnholder-server/pull/161)은 2026-10-02T02:51:36Z에 head `f39042e`로 병합됐다(merge `333fe20`). 이 goal은 종료됐다. 아래 「PR161의 병합은 사용자 별도 명시 승인 전」은 병합 전 기록이다. 실제 개발 세션 연결은 2026-10-06 사용자 결정으로 다음 계획에 남았다. 확인 원시는 [운영툴 기록 원본 일원화 goal](../2026-10-06-record-source-unification/goal.md)의 근거 폴더 `ended-goal-pr-states.txt`다.

**M-1의 구현·독립 검증·[PR #159](https://github.com/bass131/dawnholder-server/pull/159) 병합을 마쳤다. 병합 커밋은 `96cc89a83d9abd305a46d025333382e86488ca2f`, 시각은 2026-10-02 02:03:27 UTC(11:03:27 KST)다.** 최종 V3-R1은 249/249, 제품 차단 0건·새 제품 결함 0건·참고 9건이며 V3-01/02와 R01~R09는 승인 범위에서 해소됐다. 실제 개발 세션 연결(D4)은 별도 결정 전이며 시작하지 않았다. 병합 결과와 PR158 O-7의 RESUME 안내를 담은 두 문서는 독립 정적 실사 PASS·필수 결함0 후 [후속 PR #161](https://github.com/bass131/dawnholder-server/pull/161)로 올렸고, 작업자 정산을 마쳤다. PR161은 별도 사용자 병합 승인 전이다. V2 보고 과장, V3 GUI 미확인 합성 클릭, Sol 환경 복구 표현, V3-R1 수치 오기와 정리 절차 이탈은 아래 원문·감사 기록을 유지하며 성공으로 소급하지 않는다.

- 실제 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`.
- 시작: clean `main`, HEAD `18c8ca6a5aa3032873029cbd36658f0c4f9095c5` (PR156). `git fetch origin main` 후 origin/main도 같은 SHA임을 확인했다.
- 최초 구현 브랜치: `feat/management-shared-read-mcp`, 위 origin/main에서 새로 생성해 PR159로 병합했다. 첫 커밋 대상은 이 goal 한 파일이었다. 현재 종료 문서 브랜치는 `docs/management-m1-closeout`이며 최신 상태는 이 절 첫 문단과 아래 후속 기록을 따른다.
- 현재 runtime `8a673084-6819-45b9-a551-347226cdce9b`, Astra terminal `term_6df8363a-d0bf-454f-aa67-7c7c7323008a`, incarnation `3068c493-4095-4e8c-b907-eed3f3b741e6`. Orca worktree 소속과 실제 cwd 모두 management-active로 확인했다. 이 값은 관찰 기록이며 향후 실행 권한이 아니다.
- READY 회신 `msg_46884ca17d25`를 메인에게 enqueue했다. enqueue 성공을 메인이 읽거나 승인했다는 근거로 쓰지 않는다.
- 구현 발행 전에는 좁은 정적 조사, 버전/registry metadata 조회, 공식 문서 확인과 Fable 계획 검토를 수행했다. 이후 Sol 구현·자체 실행과 독립 검증의 근거는 아래 기록을 따른다. 실제 개발 세션 연결 설정은 변경하지 않았다.
- 원시 receipt/로그와 판정 원문은 Git 제외 `.backups/verification/2026-10-01-shared-read-mcp/`에 보존한다. 계획·구현·최초 수정 필요·후속 독립 통과 판정은 아래에 구분한다. 원문은 로컬 근거이며 원격 가용성을 보장하지 않는다.

Sol 구현과 V1-01 수정·재검증, V2, V3 수정·재검증 및 PR159 병합을 마쳤다. 메인이 해당 PR의 사용자 승인을 받아 병합했음을 전달했고 Astra는 GitHub 병합 사실을 별도로 대조했다. D4 실제 개발 세션 연결/설정 적용은 계속 별도 결정 항목이다. 정본 범위는 확정됐고 SDK·응답 제한·Windows 저장 보완의 기술 결정과 실행 근거는 아래에 구분한다.

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

이 보완은 문서 작업이며 제품 코드·설정·catalog는 변경하지 않았다. SDK·GUI 실행은 이 보완 시점에 모두 계획이었다. 이후 승인과 실행은 아래에 구분한다.

### goal 승인과 구현 착수

메인 메시지 `msg_8c4a3b4c61ff`에서 `ed465b0..8001372` diff를 직접 확인하고 goal 및 Sol 발행·V1→V2→V3·PR까지 승인했다. 병합은 해당 PR의 사용자 명시 승인 대상이다. 메인이 허용한 호출량 조정 중 **동시 처리 4, 대기열 0**을 선택했다. 작은 병렬 도구 호출을 수용하면서 대기열·대기 timeout의 추가 수명주기를 만들지 않기 위해서이며, 나머지 16/8 KiB·10건·초당 10회 수치는 유지한다.

Sol 발행은 Run `run_178353cf7ce2`, Task `task_da962a6212eb`, Dispatch `ctx_7eb42a498622`다. Management 탭 아래 새 vertical split의 최초 명령은 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`, 화면은 `GPT-6.1-Sol xhigh`, backend는 `unknown`이다. 빈 prompt와 `tui-idle satisfied=true`를 확인한 뒤 `worker-start --terminal` 연결에서 `input_accepted`와 `turn_started`, 잔여 자원 없음이 확인됐다. `sol-split.json`, `sol-ready-*.json`, `sol-worker-start.json`, `sol-implementation-spec.md`에 원시 근거를 보존했다. 이 기록은 작업 발행 성공이며 SDK/제품 검증 성공을 뜻하지 않는다.

### 첫 SDK smoke와 오류 반사 보완

Sol의 최초 자체 점검은 **차단 발견**이다. exact package 설치, legacy/modern 연결, 일반 strict 거부·stdout 순수성·EOF exit 0은 관측됐으나, Zod 기본 strictObject가 알 수 없는 property 이름을 그대로 오류에 넣었다. 인공 민감 경로 sentinel과 27,648 UTF-16 code unit key가 반사됐고 SDK client result JSON은 legacy 27,782 bytes / modern 27,882 bytes였다. 이는 SDK 오류 결과 크기이며 application envelope 한도 측정과 구분한다. 원문 `sol-smoke.md`와 `sol-evidence/sdk-smoke-*.json`을 Astra가 확인해 메인에 보고했다. 제품 변경은 당시 package/lockfile뿐이며 Sol은 쓰기를 중단했다.

메인 `msg_f9186b55efca`는 pin·strict 거부 유지, 입력 원문 미반사와 입력 거부 결과 1 KiB 이하를 두 규격에서 확인하도록 지시했다. 먼저 schema-local 고정 오류 문구의 공식 API를 보완 probe에서 확인하고, 통과하면 재승인 없이 같은 미완료 구현 작업을 이어간다. 실패 시에만 handler strict 대안을 검토한다. 보완 smoke는 아래와 같이 통과했다. 이 체크포인트 당시 독립 제품 검증은 미착수였다. 최초 실패를 성공으로 바꾸어 기록하지 않는다.

Sol의 보완 smoke는 두 규격 각각 잘못된 입력 9건(민감한 모양의 key/value, 27,648-unit key, 300 keys, nested object/array, 과대 query, enum 포함)을 고정 문구로 거부했다. SDK 필드 포함 client result JSON 최대치는 legacy **142 bytes**, modern **242 bytes**였고 원문 반사와 성공 data가 없었다. 잘못된 입력의 handler 미실행은 거부 envelope와 stderr 실행 marker로 추정한 것이며, 요약 log의 handlerRuns: 1은 코드 상수로 전용 호출 counter 측정값이 아니다. stdout JSON-RPC와 EOF exit 0도 관측됐다. Astra는 원문 보완 절·probe·요약 log를 직접 읽고 schema-local 고정 오류 방식 채택 및 구현 재개를 통지했다. 근거는 sol-smoke.md의 Supplemental checkpoint, sol-evidence/sdk-sanitized-*.json과 sdk-sanitized-run.log다. 이는 구현자 자체 점검이며 V2가 실제 제품으로 다시 독립 검증한다.

메인의 표기 정정 요청 msg_3507e1989469에 따라 위의 관측/추정을 구분했다. Astra가 client probe를 직접 대조했으며 :42-43의 stderr 한 줄·고정 marker assert도 보조 근거로 기록한다. V2는 내부 server factory의 시험용 callback(예: onToolHandlerEntered)을 tool handler 진입 첫 지점에 주입해 실제 호출 counter를 측정한다. production entry는 이 callback을 주입하거나 외부 옵션으로 노출하지 않는다. 기존 readSnapshot 호출수만으로 전체 handler 진입 횟수를 대신하지 않는다.

### Windows rename 간섭 확인 — 진행 중

Sol은 TEMP 사본에서 열린 읽기 handle을 강제로 유지한 atomic rename probe의 EPERM(-4048)을 보고했다. reader-probe-atomic-run.log는 probe 실행 완료(exit 0)와 rename 실패(CATALOG_UNREADABLE)를 함께 기록하므로 저장 성공 근거로 사용하지 않는다. 실제 createCatalogStore.save와 MCP 반복 조회의 간섭은 별도로 확인 중이다.

메인 msg_f1bd05219af4는 실제 저장 실패가 재현될 때에만 UI 저장의 rename 단계에서 EPERM/EACCES/EBUSY에 대한 총 1초 이내 bounded backoff 재시도를 허용했다. 잠금·낙관적 충돌·백업·초안 보존은 유지하고 소진 시 기존 저장 실패를 반환한다. MCP reader는 finally close와 명시적 UNREADABLE(retryable)을 유지하며 내부 자동 재시도는 추가하지 않는다. 재현되지 않으면 수정하지 않고 강제 handle probe를 위험 근거로만 남긴다. 수정 시 V2/V3는 반복 조회 중 저장 성공률과 실제 rename 시도 횟수 분포를 측정한다. 후속 quota 범위 직접 reader 시험에서 저장 실패가 재현돼 아래 보완을 진행했다.

Sol은 조회 없는 제어군 save 100/100 성공과 100ms 간격 직접 reader 병행의 save 99/100(write 실패 1건)을 보고했고, 마지막 catalog rename에만 10→20→40ms 상한 backoff와 단조 1초 deadline을 적용했다. backup rename과 reader에는 재시도가 없다. 같은 사본 조건의 보완 후 저장은 100/100 성공, 실제 onAttempt 관측의 시도 분포는 첫 시도 98·두 번째 2(EPERM 2건)였다. 이 짧은 시험에서 reader 성공은 7회이며 실제 stdio client 반복 검증과 구분한다. 가상 clock의 EPERM/EACCES/EBUSY 소진은 각 27회 시도·1000ms 뒤 기존 write 실패, 비대상 EIO는 1회·대기 0이었다. 원본/백업/입력 초안 보존과 소유 lock·임시파일 정리를 자체 관측했다. Astra는 windows-save-probe-limited-after.log와 rename-retry-probe-run.log 원문을 읽었다. createCatalogStore의 세 번째 CatalogRenameOptions(rename/wait/now/onAttempt)는 독립 검증의 시도 분포·소진 관측에 사용하고 production IPC에 노출하지 않는다. 이 자체 점검 보고 당시에는 최종 쓰기 종료와 독립 판정을 기다렸다. 후속 상태는 아래를 따른다.

### Sol 완료·정산과 V1 시작

Sol 최종 원문 `sol-implementation.md`는 제품 쓰기 종료 `2026-10-01T22:24:13+09:00`과 제품 18개 파일 변경을 기록한다. 기존 27개 테스트, UI/Electron/MCP 빌드와 실제 entry의 두 규격 자체 smoke는 성공했다. 입력 거부 결과의 최대 크기는 legacy 387 / modern 554 bytes, 표본 application 응답의 최대 크기는 16,220 bytes였다. modern SDK 부가 필드와 wire 크기는 별도다.

두 client는 순차 실행했으며 동시 격리·실제 stdio 취소·독립 V1/V2/V3·GUI는 당시 미실행으로 구분했다. Astra는 원문 전체와 실제 변경 목록을 대조했다. 실제 개발 세션 연결·전역 설정·정본 편집·게임/DB 검증은 수행하지 않았다.

`worker_done msg_9b8bc95a93ef`의 정확한 Task/Dispatch와 원문을 대조하고 release했다. 반환은 `retained / external_terminal / processAction none`이어서 같은 incarnation의 완료·빈 prompt를 확인한 뒤 Sol pane만 닫았다(`ptyKilled=true`). Delivery `delivery_330194967fe5`를 acknowledge했고 reclaimable 목록 0개를 확인했다. 근거는 `sol-completion.json`, `sol-release.json`, `sol-close-identity.json`, `sol-pane-close.json`이다. 세션은 재사용하지 않는다.

V1은 같은 Management 탭 아래 새 split에서 `claude --model claude-opus-5-5`로 시작했다. 화면은 `Opus 5.5 with xhigh effort`, backend는 `unknown`이며 빈 prompt와 `tui-idle`을 확인했다. Run `run_178353cf7ce2` / Task `task_769048c370e9` / Dispatch `ctx_6517dc2b7475` / terminal `term_5c8b52c7-86ad-4906-95f9-ccec697d2671`이다.

최초 연결은 `input_accepted`·`turn_started`와 잔여 자원 없음으로 확인했다. `v1-spec.md`와 `v1-*.json`에 발행 근거가 있다. V1에게 제품 파일 쓰기 권한은 부여하지 않았다.

### V1 판정·정산과 V1-01 수정

Astra는 `v1-review.md` 전체를 읽었다. 독립 7개 파일 123건 중 121건이 통과했고, 기존 27건도 통과했다. 유일한 제품 결함 V1-01은 알 수 없는 도구 이름을 SDK가 `Tool <이름> not found` 오류에 그대로 반사하는 문제다. 5,024-unit 이름은 두 규격 모두 client 관측 5,069 bytes였으며, 기존 1 KiB·입력 미반사 기준을 위반한다. handler 진입·catalog 읽기·정상 data는 없었다. arguments 거부 31유형×두 규격은 실제 handler counter로 미진입·미반사를 확인했다. 제품·정본·package/lock·기존 시험 26개 파일 해시는 검증 전후 같았다.

메인에게 `msg_284f2834465f`로 원문 경로·결함·잔여 범위와 수정 진행을 보고했다. 기준을 약화하지 않고 신규 Sol이 공개 SDK API를 이용한 최소 제품 수정을 수행한 뒤 신규 Opus가 재검증한다. V1-01은 최초 발견이며 재검증 실패 횟수는 현재 0이다. 공개 API로 해결할 수 없거나 범위 확대가 필요하면 메인에 보고한다.

R-1은 Sol 자체 probe의 요약 필드 `strictInvalidDidNotEnterHandler`가 뒤 정상 호출을 포함하는 식이라 오해할 수 있다는 표기 지적이다. 앞의 실제 `assert.equal(entered, beforeInvalid)` 및 V1의 독립 counter 관측은 유효하므로 미실행을 통과로 보고한 불일치는 아니다. 기존 원시 로그는 보존하며 V2는 실제 counter와 wire 관측을 유지한다. V2 stdio·실제 Windows 경합, V3 UI·코드 리뷰는 아직 미실행이다.

V1 `worker_done msg_7f12f52e0932`는 검증 작업 완료이며 제품 통과를 뜻하지 않는다. 원문과 Task/Dispatch를 대조하고 `worker-release`의 `retained / external_terminal / processAction none`을 확인했다. 같은 incarnation의 완료·빈 prompt를 확인하고 해당 pane만 닫아 `ptyKilled=true`를 받았다. Delivery `delivery_9a928cb2b132` acknowledge 및 reclaimable 0을 확인했다. 근거는 `v1-completion.json`, `v1-release.json`, `v1-close-screen.json`, `v1-pane-close.json`이며 세션은 재사용하지 않는다.

수정 Sol은 새로운 Astra 아래 split에서 최초 명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`로 실행했다. 화면 `GPT-6.1-Sol xhigh`, backend `unknown`, 빈 prompt·tui-idle을 확인했다. Task `task_e0cb9ebdaee9` / Dispatch `ctx_4addcdf164d6` / terminal `term_c2e9d861-3094-44db-890d-c913d3d9e613` / incarnation `9e32c144-b447-43f8-887e-1b0a6783e0e3`다. 최초 연결 `input_accepted`·`turn_started`, 잔여 자원 없음이며 `sol-fix-v1-01-spec.md` 및 같은 접두사의 launch receipt에 보존했다. 쓰기는 MCP 결함 관련 최소 제품 모듈과 자기 보고에 한정했고 V1 시험은 읽기 전용이다.

수정 Sol은 `catalog-server.ts`와 새 `catalog-tool-name-transport.ts` 두 제품 파일을 변경했다. public `McpServer.connect`와 `Transport`를 조합해 등록되지 않은 문자열 이름을 SDK 조회 전에 고정 `-32602 / Unknown tool name.`으로 거부한다. SDK 내부 교체나 node_modules 패치는 없다고 보고했으며 Astra는 두 제품 파일·기존 server 대비 diff와 `sol-fix-v1-01.md` 전체를 읽었다. 최종 build digest는 `0.0.0+sha256.08509f24db77418ae3cfca9ca9d2951612e14c1f24ae09ec118d36046a2cf18c`다.

구현자 자체 재실행은 기존 V1 123/123, 전체150/150, MCP typecheck/build exit0이다. 양 규격 in-memory·계측 stdio·production stdio의 54개 미등록 이름은 client 오류46bytes, wire79bytes로 반사 없이 거부됐다. 계측된36호출의 handler/reader는0이며 production counter는 미측정이다. 실제 stdio child4개는 EOF 후 exit0, stdout JSON-RPC만 관측했다. 이 결과는 V2 전체나 독립 재검증 통과를 뜻하지 않는다. 기존81파일 중 server 외80파일은 해시불변이고 catalog도 이전해시를 유지했다.

제품 쓰기 종료는 `2026-10-01T23:21:03+09:00`이며 `worker_done msg_606fe515a530`의 정확한 Task/Dispatch와 보고를 대조했다. release는 retained/external_terminal이므로 같은 incarnation의 완료·빈 prompt를 확인하고 해당 pane만 닫아 ptyKilled=true를 받았다. Delivery `delivery_6dbb4f9d47f9` acknowledge 및 reclaimable0을 확인했다. `sol-fix-v1-01-completion.json`·release/close receipt에 보존했고 이 세션은 재사용하지 않는다. 신규 Opus의 첫 재검증을 진행한다.

첫 재검증 V1-R1은 새 split의 `claude --model claude-opus-5-5`로 실행했고 화면 Opus5.5 xhigh, backend unknown, 빈 prompt·tui-idle을 확인했다. Task `task_c6260f5ed8ad` / Dispatch `ctx_499a690e574f` / terminal `term_92dfa7f5-d558-4637-aaf8-e150a90fd1b2` / incarnation `ffde5d31-814c-4343-a87e-82458d69aeae`다. 연결은 input_accepted·turn_started, 잔여 자원 없음으로 확인했으며 `v1-r1-spec.md` 및 launch receipts에 보존했다. 제품은 읽기 전용이고 원래 기준과 실패 사례를 유지해 시험을 보완·실행한다. 판정은 `v1-r1-review.md` 대기 중이다.

### V1-R1 독립 재검증 통과·정산

`v1-r1-review.md`의 최종 판정은 **V1 재검증 통과, V1-01 해결**이며 Astra가 전체 원문을 읽었다. 공개 API만 사용하는 수정임을 실사했고 SDK/server/core/client 및 zod가 registry tarball과 바이트 동일함을 확인했다. 미등록 이름39종(1,000,012-unit 이름·경로/URL·Unicode·prototype key 포함)은 두 규격 모두 client46bytes, wire79~80bytes로 고정 거부됐다. 실제 tool handler 첫줄 counter·readSnapshot·open은0이며 이어 정상 다섯 도구의 counter가5로 늘어 계측의 동작도 확인했다.

새 독립시험24건, 원래V1 123건, 전체174건과 시험/MCP typecheck·MCP build가 모두 exit0이다. 수정 전 server를 이용한 저장소 밖 변이 시험에서는 새 시험 중14건이 실패해 원래 결함 판별력도 확인했다. 제품 원본은 변이하지 않았으며 검증 전후55파일 해시와 정본은 불변이다. production stdio 보조 점검은 두 규격을 순차 실행해 정상 다섯 도구·미반사·stdout JSON·EOF exit0을 확인했지만 V2의 동시client/실제취소/Windows 경합을 대신하지 않는다. 보고/실행 불일치는 없었다.

남은 V1 제품 결함은 없다. V1-01 재검증 실패 횟수는0이다. 비차단 관찰은 schema key와 등록 도구 집합의 수동 결합(V3 코드리뷰 인계), wrapper의 SDK envelope 검사 이전 거부, SDK/transport 갱신 때 전달 멤버 재점검, 비문자열 이름의 고정 크기 SDK문구다. Transport `extra` 전달은 코드상 확인했으나 시험에서는 효과를 판별하지 못했다. V2/V3와 실제 설정 연결은 별도이며 미실행 범위를 통과로 바꾸지 않는다.

시험 쓰기 종료 `2026-10-01T23:45:56+09:00`, 보고 종료 `23:50:07+09:00`, 완료 `msg_0e73d5464f53`의 Task/Dispatch를 대조했다. release retained/external_terminal 후 같은 incarnation의 완료·빈 prompt를 확인하고 해당 pane만 닫아 ptyKilled=true를 받았다. Delivery `delivery_9f36037d2498` acknowledge 및 reclaimable0을 확인했으며 `v1-r1-completion.json`·release/close receipts에 보존했다. 다음 V2는 새 세션으로 수행한다.

### V2 기준 커밋과 신규 검증자 발행

모든 작업자 쓰기가 끝난 상태에서 제품·시험·05 안내/상태 35파일을 `41010557917adcfff8c2d11258cff371ff777569`로 커밋했다. `git diff --check`·staged check는 exit0, 커밋 후 clean을 확인했다. 기존 main 기준 `18c8ca6`에서 추가된 제품 파일도 이제 Git diff에 포함된다. 원격 push·PR·병합은 아직 수행하지 않았다. 메인에게 `msg_5d42b5d47260`으로 V1-R1 원문 경로·통과 근거·잔여 범위와 이 커밋을 보고했다.

V2는 새 Astra 아래 split의 `claude --model claude-opus-5-5`로 실행했고 화면 Opus5.5 xhigh, backend unknown, 빈 prompt·tui-idle을 확인했다. Task `task_ea27a8466c95` / Dispatch `ctx_494133981704` / terminal `term_08bf6163-fe4d-4787-865e-1da59e2a190f` / incarnation `bbe3496e-00ff-46e3-bcd2-116cb234d527`다. 연결은 input_accepted·turn_started, 잔여 자원 없음이며 `v2-spec.md`, `v2-*-*.json`에 근거를 보존했다. 제품·기존/V1/V1-R1 시험은 읽기 전용이고 V2 시험만 작성한다. 실제 stdio 동시두client·취소/종료·quota·응답크기·고정정본경로와 Windows 반복 저장 경합을 검증한다. `v2-review.md` 판정 대기 중이다.

### V2 통과·실사 보완·정산

Astra는 `v2-review.md` 전체를 읽었다. V2 신규32건, 전체206건, 시험/MCP typecheck·build가 exit0이며 제품·정본49파일과 mcp-dist12파일 해시는 불변이다. 두 규격의 실제 동시child에서 탐색·hash고정·충돌·재빌드 없는 갱신·취소시 응답억제/handle/slot반환·프로세스별quota·argv/env/cwd/roots로 정본을 바꾸지 못함을 확인했다. 최종실행50프로세스의 EOF 종료는 최대13ms, exit0, 강제kill0이었다. 입력거부50유형의 fixture handler/read/open 실측0, 정상대조군계측동작, 최대client거부609bytes이며 production에는 counter가 없어 직접계측으로 주장하지 않는다.

정본 최대application은16,220bytes, modern client16,387/wire16,445bytes다. SDK부가필드는 application한도와 별도다. Windows 두프로세스가 각각tokenbucket 상한으로 읽는7회 실행에서 저장1,400/1,400, rename1회1,395·2회5(EPERM5)였다. 제어군350/350, 공유없는handle의 실제EBUSY→UNREADABLE/해제후회복, 강제읽기handle의EPERM23회·1,008~1,017ms후기존write실패, EIO1회실패·backup/lock/conflict 보존을 확인했다. 마지막새시도시작은956~964ms로1초창안이며 OS I/O 자체지연과 구분한다.

초기 시험실패 두건은 시험측 원인이었다. IPC의pause등록확인보다stdin요청이앞서는 순서문제와, 비원자적관측 직후회복조회가소진된quota의retryAfterMs를지키지않던 문제를 보완했다. 실패로그는 보존됐다. 다만 **보고 원문12절의 초기실패실행도 "부분 성공0건"이라는 문장은 미입증**이다. 7절은 그 실행의측정/최종검사가남지않았다고 정확히구분한다. Astra가 `v2-windows-attempt-before-fix.log`와 시험흐름을 직접 대조하니 회복assert실패후의unknownHashes검사는 미실행이었다. 이 차이는 `msg_e9bf2c4563f1`로 즉시메인보고했고 `v2-astra-audit.md`에 보완했다. 원문은 보존한다. 최종및나머지6회부분성공검사/전체pass, 별개인원자적저장1,400측정의근거는유효하다.

비차단 관찰은 modern SDK부가크기, 비원자적편집의일시적INVALID(retryable=false), schema확장시거부크기재측정, SDK10MiB초과stdin연결종료, 환경별경합빈도차이다. MCP안내에는 외부편집완료확인후수동재조회와지속오류확인을추가했고V3문서실사대상이다. V3 UI/ElectronGUI/build/코드리뷰와실제개발세션연결은아직미실행이다.

시험쓰기종료 `2026-10-02T00:20:22+09:00`, 보고종료00:26:24, 완료 `msg_15de0df8beec`의Task/Dispatch를대조했다. release retained/external_terminal후같은incarnation의완료·빈prompt를확인하고해당pane만닫아ptyKilled=true를받았다. Delivery `delivery_c13e5c812626` acknowledge및reclaimable0을확인했고 `v2-completion.json`·release/close receipts에보존했다. 완료세션은재사용하지않는다.

### V3 발행

V2 시험 8파일과 goal/MCP 안내 2파일을 `637145171bc8ef721b32aa2bd948a60482b4e30a`로 커밋하고 clean tree를 확인했다. 제품은 `4101055`와 같다. V3는 이 커밋을 기준으로 실행한다.

새 Astra 아래 split의 최초 명령은 `claude --model claude-opus-5-5`, 화면은 Opus 5.5 xhigh, backend는 unknown이다. 빈 prompt·tui-idle 확인 후 Task `task_d7619a50ed94` / Dispatch `ctx_da4608163a86` / terminal `term_958ff834-d02e-4cdd-96d1-c6459237e711` / incarnation `40fd9fb6-4acd-4a8e-9523-d73f8942352c`를 연결했다. input_accepted·turn_started와 잔여 자원 없음을 확인했고 `v3-spec.md` 및 launch receipts에 보존했다.

제품·문서·V1/V1-R1/V2 시험은 읽기 전용이며 V3는 UI/store 회귀 시험과 자기 근거만 쓴다. 메인 `msg_fe19acfb19c9`의 코드 리뷰 시범을 V3에만 적용한다. 책임 분리·의존 방향·중복·사람 가독성·요구사항 기반 시험을 검토하고, 별도 코드 리뷰 절에 차단/후속/참고를 구분한다. 전역 스킬이나 검증 규칙은 변경하지 않았다. V2 원문과 `v2-astra-audit.md`를 함께 인계했다. Electron GUI는 고유 TEMP 05 사본에서만 확인하며 실행 불가 시 미실행을 명시한다. 현재 `v3-review.md` 판정 대기다.

### V3 판정·운영 관찰·정산

Astra는 `v3-review.md` 전체와 최종 시험/해시 로그, 실제 시험 diff를 읽었다. 최종 판정은 **V3 수정 필요**다. 전체 213/213(신규 V3 7건), launcher 12/12, UI·desktop·MCP·시험 typecheck와 새 TEMP 사본의 desktop/MCP build는 통과했다. MCP digest는 `08509f24…`로 유지됐다. 실제 Electron 사본에서 조회·새로고침·초안 보존·충돌·rename 소진 뒤 write 실패 및 회복, 두 MCP 프로세스 상한 조회 중 저장 10/10을 확인했다. 정본 catalog·산출물·profile은 불변이다. GUI의 파일 불러오기 OS 대화상자와 트레이 메뉴는 미실행이며 기존 자동 시험 근거와 구분한다.

V3-01(낮음)은 편집 화면의 “MCP 연동은 제공하지 않습니다”라는 과거 문구다. V3-02(중간)는 현재 고정 Electron package가 install script를 자동 실행하지 않아 `npm ci` 후 실행 파일이 없어지는데 MCP/README에 준비 단계가 빠진 문제다. 정본 `node_modules/electron/dist`도 부재이며 정확히 어느 실행에서 사라졌는지는 미확인이다. 새로운 Sol이 제품 문구와 기존 exact package의 로컬 바이너리 복구를 맡고, Astra는 MCP/README 안내를 보완한다. 코드·설정·전역 환경으로 범위를 확대하지 않는다. 두 결함의 재검증 실패 횟수는 0이며 신규 Opus가 재검증한다.

코드 리뷰 시범은 병합 차단 0·후속 처리 8·참고 1이다. 도구/enum/기본값/build 입력의 중복, 호출량 정책 분리, 예외·rename 기한 설명, backoff 일정을 고정한 시험이 후속 관찰이다. 지금 제품 결함으로 판정하지 않았으므로 이번 수정에 리팩토링을 추가하지 않는다. 메인이 원문 7절을 읽어 시범을 평가한다. V2 초기 실패 실행의 “부분 성공 0건”이 미입증이라는 Astra 보완도 V3가 원시 로그로 확인했다.

**운영 사고:** V3 원문 9절 O-4는 00:43:50 KST에 시험 창 전면 확인 없이 합성 클릭을 한 번 보냈고, 다른 창 영향은 미확인이라고 기록한다. O-3의 첫 캡처는 다른 앱이 찍혀 evidence 사본을 즉시 삭제했으나 `%TEMP%/orca-computer-use`의 Orca 원본은 남았다. 이를 정상 수행이나 영향 없음으로 바꾸지 않으며 `msg_90f09f4cc8ce`로 메인에 즉시 보고했다. 후속 검증은 이번 문구/설치 보완에 필요한 범위로 한정하고 불필요한 GUI 조작을 반복하지 않는다.

시험 쓰기 종료 `2026-10-02T00:52:47+09:00`, 보고 종료 01:01:48, `worker_done msg_51af58025f52`의 Task/Dispatch를 대조했다. release retained/external_terminal 뒤 같은 incarnation의 완료·빈 prompt를 확인하고 해당 pane만 닫아 ptyKilled=true를 받았다. Delivery `delivery_0b7395a6a96a`를 acknowledge하고 reclaimable 0을 확인했다. `v3-completion.json`·release/close receipts에 보존했으며 세션은 재사용하지 않는다. V3 TEMP 정리 helper의 프로세스 한정 PowerShell `-ExecutionPolicy Bypass` 사용은 `tests/mcp-v3/temp-copy.ts:77-80`과 최종 시험 실행 로그로 확인했고, 영구 정책은 변경하지 않았다.

수정 Sol은 새 split에서 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`로 실행했다. 화면 GPT-6.1-Sol xhigh, backend unknown, 빈 prompt·tui-idle을 확인했다. Task `task_ea01bc527389` / Dispatch `ctx_6cf98c98d455` / terminal `term_6fc65d99-e227-439c-91b0-4c3b6a4d5ccf` / incarnation `f2c715f5-56a7-4175-aa51-e0802ff91eeb`다. 연결 input_accepted·turn_started와 잔여 자원 없음을 확인했다. 제품 문구 한 곳과 로컬 Electron 실행 파일 복구만 맡겼고 문서는 Astra가 소유한다. `sol-fix-v3-spec.md` 및 launch receipt에 근거가 있다.

### 메인 결정에 따른 V3 코드 리뷰 보완 포함

메인 `msg_cad7eaf821c8`은 V3 원문 7·9절을 직접 읽고 사람 가독성을 우선하라는 사용자 이유를 들어, 코드가 새로 작성된 이번 수정에 후속 사항도 포함하기로 결정했다. 앞의 “후속 리팩토링을 추가하지 않는다”는 당시 계획을 대체한다. 미완료 수정 Sol의 범위에 R01 도구 정의 결합, R02 admission 분리/수치 명명, R03 기본 limit 공유, R04 enum/타입 공유, R05 import closure 기반 digest와 산출물 정리, R06 고정 진단/주석, R07 기한/observer 정리, R09 schema 가독성을 추가했다. R08 backoff 시험의 불변식 보완은 신규 재검증 Opus에게만 허용했다. 오류 code와 기존 기능 계약은 바꾸지 않는다.

메인은 Electron 바이너리 복구를 우선하고 exact package·검증된 로컬 cache에 한정했다. Orca 원본 캡처는 메인이 사용자 판단을 받을 때까지 삭제하지 않는다. 앞으로 GUI 합성 입력은 시험 창 전면 확인과 restore-window 후에만 하며 캡처는 시험 창 영역으로 한정하도록 재검증 spec에 명시했다. 이번 재검증은 GUI를 다시 조작하지 않고 기능 suite·TEMP 빌드/설치·코드 리뷰로 수행한다. 추가 지시의 본문 태그 누락 한 건은 `msg_c7d95297f1d4`에서 `[Management Astra]`와 같은 from_handle로 즉시 정정했다.

메인 `msg_a7adb1516811`은 사용자 결정(메인 경유)에 따라 2026-10-02 01:12 KST에 V3 Orca 원본 캡처 7개를 내용 열람 없이 삭제했다고 통지했다. Astra는 evidence에 남은 이미지 두 개를 직접 확인했고 모두 시험 Management 창 영역만 담겼으며 최초 잘못 찍힌 사본은 없었다. 이 목표 evidence 범위에서 다른 앱 화면 사본은 확인되지 않았다. `v3-review.md` 14절에 Astra 후속 보완을 추가하고 최초 판정은 `v3-review-original.md`에 바이트 그대로 보존했다. 합성 클릭의 다른 창 영향은 계속 미확인이다.

Electron 복구 status `msg_fdec13953be9`의 설치 exit 0·cache hit와 version44.5.0/path.txt/electron.exe SHA256 `3e76fb616aa60a850b7f64800dd81b21e06ddee3f1919650183b047f03e7cb9f`를 Astra가 직접 대조했다. 앱 실행은 하지 않았다. 최초 부재 시점은 당시 dist 존재 관측이 없어 확정할 수 없고 V1/V2 npm ci 원시 로그와 추론을 `v3-electron-absence-timeline.md`에 구분했다.

이 status의 “프로세스 환경 기존값 복구”는 **실제 수행과 다른 표현**이었다. `electron-install.log` 끝의 restored=False를 Astra가 발견해 질의했고, Sol은 원래 부재(null)였던 electron_config_cache/DEBUG가 SetEnvironmentVariable(null,Process) 후 빈 문자열로 남았다고 정정했다(`msg_2c07e489b004`). 설치 subprocess는 종료됐고 전역/타프로세스 쓰기는 없다고 보고했다. 별도 `environment-restore-probe.json`은 기존식 false와 부재 시 Remove-Item Env:name을 쓰는 보정식 true를 기록한다. 최초 로그는 보존하며 후속 probe를 최초 실행 성공으로 소급하지 않는다. `msg_dd4e7b95d958`로 메인에 즉시 보고하고 새 Opus의 실사 대상에 추가했다. 파일 복구 성공과 이 환경 복구 실패를 구분한다.

메인 `msg_d1b1339e63cf`에 따라 V3 O-4는 **“다른 창 영향 미확인, 사용자 직접 관측 없음, 추가 조치 없음으로 종결”**했다. 사용자는 당시 화면을 보지 않았다고 메인을 통해 답했다. 해당 synthetic_input은 검증자 명령이며 사용자 클릭이 아니다. `v3-review.md` 15절에 Astra 후속 보완을 추가했다. 영향 없음으로 판정을 바꾸지 않으며 전면 확인/restore-window/시험 창 캡처 조건은 재검증 spec에 유지한다. 전역 규칙 문서는 변경하지 않는다.

### V3 수정 Sol 완료·정산

Astra는 `sol-fix-v3.md` 전체와 제품 9파일 실제 diff(신규 admission 포함), 시험 실패절·설치/보존/별도 probe 원시 근거를 읽었다. 제품 마지막 수정은 `2026-10-02T01:20:44.9103256+09:00`, 쓰기 종료 선언은01:28:00.827이다. 메인 승인 R01은 단일 표 대신 허용된 최소안(outputSchema 타입 결합·transport 주석)을 적용했고 명시 등록의 수동 관리는 남았다. 나머지 admission 분리·공통 limit/enum·compiler 의존 그래프 기반 digest·안전한 산출물 정리·고정 stderr 진단·observer 예외 격리·schema 가독성과 V3-01 문구를 보완했다. MCP/README 설치 안내는 Astra가 작성했다.

자체 UI/desktop/MCP/시험 typecheck와 MCP build는 exit0, 새 digest는 `0.0.0+sha256.fd60b03a6b866f127446178b3e431b00cd7b457a7a257000d5182c10bc8a7ae9`다. 전체 시험은 **210/213, 3건 실패**이며 과거 buildscript 문자열(V1 boundary), 12개 산출물 목록(V2 path), 이전 digest(V2 stdio)를 고정한 기대와 충돌했다. 해당 assertion 이후 미실행 경로는 통과로 주장하지 않는다. 새 Opus에게 이 세 파일과 R08의 V1 rename 시험 보완 권한을 명시하고 요구사항의 판별력을 유지하도록 했다. 원시 실패/정정은 보존한다. 신규 독립 판정은 아직 없다.

Electron 파일 복구는 exact package44.5.0·검증된 로컬 cache·process network guard로 exit0/cache hit다. 전역 설정·package/lock/catalog·기존 시험·정본 UI/desktop 산출물은 내용 불변이다. 환경 변수의 최초 부재 복구 실패는 앞의 기록을 유지하며, 별도 no-op 재실행의 정확한 복구식에서 전후 부재·동등성 true를 확인했다. 이를 최초 실행 성공으로 소급하지 않는다. 앱/GUI/배치는 실행하지 않았다. Start-Management.bat가 다음 실행 때 소스를 다시 빌드한다는 것은 정적 확인이다.

자동 승인 검토가 자기 probe 사본의 계산 경로/좁은 literal 경로 삭제 **두 차례를 실행 전 `blocked by policy`로 거절**했다. 더 구체적인 이유는 반환되지 않았다. 앞서 Astra의 “재시도하지 않았다”는 진행 설명은 최종 원문에서 이 두 시도가 있었던 것으로 정정한다. 이후 추가 삭제는 하지 않았다. 정확한 보존 경로는 `.backups/verification/2026-10-01-shared-read-mcp/sol-fix-v3-evidence/빌드 경로 probe 4h8hIb`, 44파일·177500bytes·링크0이다. node_modules junction은 제거됐고 catalog/profile은 없다. `probe-cleanup-rejected.log`·`probe-retained-manifest.json`에 근거가 있다. 신규 검증자는 이 사본을 삭제하거나 재사용하지 않는다.

완료 `msg_a288db114bfb`의 Task/Dispatch와 보고를 대조했다. release retained/external_terminal 후 같은 incarnation의 완료·빈 prompt를 확인하고 해당 pane만 닫아 ptyKilled=true를 받았다. Delivery `delivery_5d071f50c680` acknowledge 및 reclaimable0을 확인했다. `sol-fix-v3-completion.json`·release/close receipts에 보존했으며 세션은 재사용하지 않는다.

### V3 첫 재검증 발행

제품 보완·기존 V3 시험·Astra 안내/상태 16파일을 `5a7ad0a3c6ff07e30d7cd76a7e951c894ea830cf`로 커밋했다. diff/staged whitespace check exit0, 커밋 후 clean을 확인했다. 이 커밋은 재검증 기준이며 통과나 PR/병합 완료를 뜻하지 않는다.

새 Astra 아래 split의 최초 명령은 `claude --model claude-opus-5-5`, 화면 Opus5.5 xhigh, backend unknown이다. 빈 prompt·tui-idle 확인 후 Task `task_73b31886940c` / Dispatch `ctx_0c267aaa3bb0` / terminal `term_85966825-0034-4b85-b281-e988943bc1c5` / incarnation `f3326fd3-5d4c-4e45-b66d-84abd6d2c316`를 연결했다. input_accepted·turn_started와 잔여 자원 없음을 확인했다. `v3-r1-spec.md` 및 launch receipts에 근거를 보존했다.

제품/문서는 읽기 전용이며 R08 rename 시험과 자체 실패 3시험의 요구사항 기반 보완, 필요한 신규 독립 시험만 허용했다. 전체 기능·코드 리뷰·TEMP 설치/desktop build를 재검증하고 GUI는 다시 조작하지 않는다. 보고 표현 정정·미실행 범위·캡처 사고 후속·삭제 거절 사본 보존을 함께 인계했다. `v3-r1-review.md` 판정 대기다. 메인 `msg_930793254a5b`의 capacity 발생 시 동일 미완료 task/모델을 유지하며 간격을 늘려 재시도하는 사용자 지침도 인계했다. 현재까지 이 발행에서 capacity는 관측되지 않았다.

### capacity 대응의 조건부 모델 예외

메인 `msg_38f843354063`은 사용자 부재 중 적용할 사용자 지침(메인 경유)을 전달했다. 구현 gpt-6.1-sol에서 capacity가 반복되면 먼저 같은 미완료 task/세션에서 1→2→5→10분 간격으로 재시도하고, 최초 관측 후 누적 30분 이상 계속 실패할 때에만 기존 세션을 상태/근거 보존·정산·종료한 뒤 별도 신규 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh` 작업자로 같은 범위의 미완료 작업을 이어갈 수 있다. 역할 태그는 Sol을 유지하고 요청/최초 명령·화면/백엔드 unknown과 시각별 사유를 기록한다. 파트 리드가 구현을 대신하는 허가는 아니다. Opus에는 전환 예외가 없으며 장시간 실패 시 대기 상태로 남긴다.

현재 Management 수정 Sol은 capacity 없이 이미 종료됐고 V3-R1 Opus도 capacity가 관측되지 않았다. 따라서 현재 세션 모델을 바꾸지 않는다. 이 조건부 지침은 향후 필요 시에만 적용하며 프로젝트 전역 규칙을 수정하지 않는다.

### V3-R1 독립 재검증 통과·감사·정산

신규 Opus의 `v3-r1-review.md` 첫줄은 **V3 재검증 통과**다. Astra가 원문 전체, 실제 시험 diff, 원시 최종 runner/typecheck/build 차등·보존 근거를 직접 읽었다. V3-01/02와 R01~R09는 승인 범위에서 해소됐고 제품 차단 0건·새 제품 결함 0건·참고 9건이다. Sol의 제품 쓰기 종료 후 검증했고 검증자 시험 쓰기 종료는 `2026-10-02T02:02:01+09:00`, 완료 메시지는02:22:40이다. 모델 capacity는 발생하지 않았다.

최종 **21파일 249/249**, UI/desktop/MCP/시험 typecheck 4종, 05 launcher **12/12**, 정본 MCP build, TEMP MCP·desktop build가 exit0이다. 249는 기존213에서 R08의 고정 상수 시험1개를 요구사항 시나리오로 통합하고 신규37개를 더한 값이다. rename의 합법적 backoff 변형3개는 통과하고 위반9개는 거부했으며 observer 예외 격리, 새 전이/type-only 소스의 digest 포함, 안전한 산출물 정리, 두 규격의 고정 오류 진단과 공개 응답 보존을 확인했다. `637145..5a7ad0a`의 old/new 별도 build는 tools/list와16개 호출이 두 규격에서 동일했고 build version만 달랐다. 정본 MCP digest는 `fd60b03a6b866f127446178b3e431b00cd7b457a7a257000d5182c10bc8a7ae9`로 불변이다.

이번 Windows 재실행은 제어50/50·읽기 부하200/200 저장(첫 시도199·EPERM 후 두 번째1), held-handle 소진1008ms/23시도, EIO1회, 취소·quota·크기 제한·50개 stdio EOF 정상 종료를 확인했다. 과거 V2의1400/1400 저장과 최초 V3 실제 GUI 결과는 별도 실행 근거이며 이번 GUI 재실행으로 부르지 않는다. 이번 TEMP 설치 실증은 오프라인 npm cache와 검증 ZIP으로 `npm ci`의 Electron dist 소실 및 install.js·lazy require 복구를 재현했다. 정본 Electron44.5.0 exe SHA와 TEMP 복구 SHA가 일치한다. UI/desktop 기존 정본 산출물은 보존했고 정상 launcher가 다음 실행에 새 소스를 build하는 것은 정적 확인이다.

**Astra 감사 정정:** 원문35행의 DevelopmentRecords 시험6건은 실제 `r1-final-npm-test.log`499·515·516·537·546·554·555행의 **7건**이다. Sol 원시 로그도7건이며 전체249/249에는 영향이 없다. 또한 spec의 자기 TEMP native PowerShell 정리 지시와 달리 신규 rename 시험은 Node `rmSync`를 사용했다. OS TEMP 직계 자식·고유 prefix·하위 링크 없음 검사를 했지만 지시한 정리 방식과 다르다. 대상은 mutant .ts뿐 아니라 같은 helper의 R07 store fixture catalog/backup도 포함한다. 원문 §10과 Astra 최초 알림의 “작은 .ts만” 표현도 축소였음을 정정한다. 정본 catalog 삭제나 잔여 TEMP는 관측되지 않았다. 메인에 `msg_13234b64aa12`, 이어 정확한 대상 범위를 `msg_4a73ce51cb73`으로 즉시 보고했다. `v3-r1-astra-audit.md`에 원문 hash·근거를 별도 보존하며 원문 판정은 수정하지 않는다.

남은 참고는 정본 UI 산출물의 다음 정상 build 필요, MCP 안내의 npm ci/lazy install 주어 명확화, 수동 도구 등록 잔여, Zod 타입 결합의 단방향 한계, 보수적인 tsconfig digest 포함, node_modules라는 상위 디렉터리 이름의 특수 경로 한계(미실행 추론), 고정 stderr 진단의 단계 구분 제한, retry 상수 설명 위치, 기존 긴 schema/DTO 행이다. 독립 판정은 모두 참고로 분류했다. 일부 실제 build/digest 시험은 Windows에서만 실행된다. 실제 Codex/Claude 개발 세션 연결, 이번 GUI, .NET·DB·Unity는 미실행이다. O-4는 “다른 창 영향 미확인, 사용자 직접 관측 없음, 추가 조치 없음으로 종결”을 유지한다. 이전 자동 승인 검토가 삭제를 거절한 Sol probe 사본44파일177500bytes도 그대로 보존한다.

완료 `msg_d8ca6b24801d`를 Task/Dispatch와 대조하고 worker-release의 retained/external_terminal 뒤 동일 incarnation의 완료·빈 prompt를 확인했다. 해당 pane만 종료해 ptyKilled=true를 받았고 `delivery_a593cdf4b3e4`를 acknowledge했다. reclaimable0이며 검증자 세션은 재사용하지 않는다. 완료·종료 receipt는 `v3-r1-completion.json`, `v3-r1-close.json`이다. 제품 소스·package/lock/catalog·정본 Electron/UI 산출물·AppData·환경·ExecutionPolicy와 삭제 거절 사본은 검증 전후 차이0이다. 시험10파일과 Astra goal 상태만 Git 변경으로 통합한다.

### PR 생성 — 사용자 병합 승인 전

독립 검증 시험10파일과 goal 상태를 `7cdcecb04f025a024866cb30629589c754a87fae`로 커밋했다. whitespace/staged check exit0·clean 확인 후 정확한 원격 `bass131/dawnholder-server`로 작업 브랜치를 push하고 `main` 대상 [PR #159](https://github.com/bass131/dawnholder-server/pull/159)를 생성했다. 같은 head의 기존 PR은 없었다. 생성 직후 API에서 OPEN·MERGEABLE·autoMergeRequest=null을 확인했고 .NET CI는 진행 중이었다. 이 CI는 Management의 독립249개 시험과 다르다. 전체 변경56파일은 모두05_Management 아래이며 catalog SHA는 착수값과 같다.

메인에게 최종 원문 `v3-r1-review.md`와 `v3-r1-astra-audit.md`를 전달해 직접 확인하도록 한다. 원문 SHA-256은 `966c2cdd0d667365abd953549e99a41dc2ffd6424a0b9da0582a91295f4fb5e1`이다. 병합·자동 병합 예약·실제 MCP client 설정 적용은 하지 않았다. PR 생성 허용과 병합 허용은 별개이며, **PR #159 병합 직전 사용자 명시 승인**을 받아야 한다. 현재 승인된 구현·검증·PR 작업은 마쳤고 병합과 D4 연결 결정을 기다린다.

### PR159 병합 결과와 문서 후속 — 2026-10-02

메인 `msg_43e5a928b372`(2026-10-02 02:08:35 UTC)는 사용자의 직접 승인 “셋 다 승인” 중 1A로 PR159를 병합했다고 전달했다. **Astra가 받은 것은 메인 경유 보고이며 사용자 직접 입력으로 격상하지 않는다.** 메인은 head `15d5a47`, `--match-head-commit` 사용, CI pass·CLEAN 확인을 보고했다. Astra의 별도 GitHub 조회값은 PR159 MERGED, 전체 head `15d5a4712fafa78f611a67b950b01cff374e8201`, merge `96cc89a83d9abd305a46d025333382e86488ca2f`, mergedAt `2026-10-02T02:03:27Z`다. 메인의 실행 명령과 사용자 승인 원문을 GitHub API가 검증한 것으로 확대하지 않는다. 원시 전달과 조회는 `.backups/verification/2026-10-02-management-m1-closeout/{main-request,pr159-merged}.json`에 보존한다.

최종 head의 원격 `dotnet-tests`는 run `36900307930`에서 2분38초 후 성공했다. 이는 Windows Management 독립 249/249와 별개이며 원격 .NET 결과다. API 근거는 이전 evidence의 `pr159-final-state.json`이다. 기존 서버 analyzer와 Actions 런타임 경고는 05 변경 범위 밖이라 수정하지 않았다. 경고 원문은 이번 독립 조회의 `verification-1/ci-annotations-110497519560.json`이며 최종 상태 JSON에는 경고 본문이 없다. 본 절은 제품·검증의 재실행이나 기존 참고9건의 추가 해소를 뜻하지 않는다.

메인이 함께 허용한 후속은 PR158 O-7의 `05_Management/RESUME.md` 배치 문장을 현행 [R-1](../../../00_Document/operations/ORCA.md#r1-management-placement)과 [R-8](../../../00_Document/operations/ORCA.md#r8-astra-lifecycle) 등 정본 참조로 정리하는 일이다. 새 목표나 D4 연결을 시작하지 않고 M-1 종료 기록에 한정한다. `git fetch origin main`으로 최신 `10bcafd7f25a861318dff6412fa3dc542addd4ef`(PR160 병합)를 확인한 뒤 clean 상태에서 `docs/management-m1-closeout`을 만들었다. 병합된 기능 브랜치에는 후속 commit을 추가하지 않는다.

후속 쓰기 소유는 Astra의 이 goal과 신규 Sol의 RESUME이며 같은 파일을 동시에 쓰지 않는다. 신규 Opus가 두 문서 diff·근거·상대 링크를 독립 정적으로 실사한다. 제품 코드·시험·catalog·설정·빌드·DB·게임은 이번 문서 범위에서 실행하거나 바꾸지 않는다. 후속 PR의 병합은 다시 사용자 명시 승인을 받아야 하며 자동 병합하지 않는다. 결과 보고 뒤 Astra 교체는 메인이 R-8로 수행한다. 아직 새 Astra 기동·교체·READY를 확인한 것은 아니며 현재 Astra가 자기 pane을 닫거나 새 목표를 시작하지 않는다.

신규 Sol은 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`로 Astra 아래에 기동했다. 최초 화면 GPT-6.1-Sol xhigh·빈 prompt와 tui-idle을 확인했고 backend는 unknown이다. Task `task_ecb984c52716` / Dispatch `ctx_8f90b40e48f6`의 최초 연결에서 input_accepted·turn_started를 확인했다. Sol은 RESUME3행 추가/1행 삭제, 변경 문단의 상대 링크·anchor5개 및 whitespace 정적 검사 성공을 보고했으며 Astra가 전체 보고와 실제 diff를 대조했다. RESUME 최종 쓰기는11:14:37 KST, 정산 보고서 최종 저장 직전은11:17:14다. split receipt에는 모델 필드가 없고 attach launch 모델은 null이다. 최초 실행 명령은 Astra 발행 기록 `sol-launch-command.txt`·회신 `msg_ff846d5b46f5`로 보완했으며 화면 관측과 구분했다.

완료 `msg_f376079bca31` 뒤 release retained/external_terminal, 같은 incarnation `f1e67918-4b7b-415d-99e2-fa33d0084002`의 완료·빈 prompt 대조, 정확한 pane close ptyKilled=true, delivery acknowledge와 reclaimable0을 확인했다. 재사용하지 않는다. 원문 `sol-report.md`와 `sol-{launch,first-screen,start,done,release,close}.json`은 이번 closeout evidence에 있다. 이 자체 정적 검사와 후속 독립 실사를 구분한다.

### 종료 문서 독립 실사와 인계

신규 Opus는 `claude --model claude-opus-5-5`로 기동했고 첫 화면 Opus5.5 xhigh·빈 prompt와 tui-idle을 확인했다. backend는 unknown이다. Task `task_1c02b2d66cc5` / Dispatch `ctx_996e413e3e62`의 input_accepted·turn_started를 확인했다. 판정 원문 `.backups/verification/2026-10-02-management-m1-closeout/verification-1/verdict.md`는 **PASS·필수 결함0·비차단 관찰7**이다. 검토 대상은 `10bcafd..05a427cbd5b5e15355f64c287a00e7c14fa5c236`의 두 문서다. Astra가 원문 전체와 변경 diff, 별도 API 조회를 읽었다. 원문 SHA-256은 `c19f43362013ba7b86b58d9de766916fd517121330ac03db198b5f64133b2894`다.

검증자는 실제 PR159 병합/CI·Sol 보고/정산을 대조하고, 변경 행의 상대 링크7개와 RESUME 전체23개를 commit blob에서 정적 검사해 실패0을 확인했다. diff --check도 exit0이다. raw4개와 판정 외 문서/제품 쓰기는 없었고 완료 메시지의 쓰기 종료는11:25:53 KST다. 이번 판정은 문서 실사이며 제품·GUI·DB·Unity·MCP·빌드는 재실행하지 않았다.

비차단 관찰 처리는 다음과 같다. O-1/O-4는 메인 보고의 짧은 head·명령과 Astra API의 전체 SHA, split receipt의 모델 필드 부재를 위 결과 기록에서 명확히 했다. O-2는 검증자가 보존한 경고 원문 경로를 추가했다. O-3의 Sol ACK는 Astra가 CLI 응답에서 확인했지만 별도 raw 파일은 남기지 않아 검증자가 직접 확인하지 못했다. 이후 현재 reclaimable0은 검증자 raw와 `reclaimable-final.json`으로 대조했고 이를 과거 ACK receipt로 소급하지 않는다. O-5는 첫 문단의 감사 색인을 복원했고 기존 원문/감사 본문은 그대로다. O-6의 RESUME 범위 밖 진행형·포인터 반복은 후속 후보로 남겨 검토된 RESUME를 더 수정하지 않았다. O-7에 따라 문서 PR 병합 전 Astra를 교체하면 **메인이 새 Astra에게 이 branch/PR의 리뷰·병합 후 결과 기록 소유를 명시 인계**해야 한다. 새 목표/D4를 시작하는 권한으로 해석하지 않는다.

완료 `msg_5592a0ef4846` 뒤 worker-release retained/external_terminal, 동일 incarnation `5ed35327-5098-41ae-94b1-8fd0df782f0e`의 완료·빈 prompt를 확인해 정확한 pane만 닫았다(ptyKilled=true). `opus-ack.json`은 `delivery_ed2d982c9055` acknowledge를, `reclaimable-final.json`은0건을 보존한다. 작업자 재사용은 없다. heartbeat3건의 body가 빈 값이었고 Astra가 `msg_22bdc24b4850`으로 태그 준수를 요청했다는 형식 관찰도 남긴다. 최종 worker_done의 subject/body와 출처는 일치했다.

실사 후 수정은 이 goal의 결과·정산·근거 귀속/인용 명확화이며 RESUME 본문은 `05a427c`와 같다. 실사 결과 기록은 `033b213069b9412f5f8f6cb4623e4917b05a5d36`으로 커밋해 `docs/management-m1-closeout`을 push하고 `main` 대상 [PR #161](https://github.com/bass131/dawnholder-server/pull/161)을 만들었다. 동일 head의 선행 PR은 없었다. 제품 코드·시험·catalog·설정 변경은 없고 D4 연결과 R-8 실제 교체도 여전히 미실행이다.

이 종료 기록과 O-7 수정의 구현·독립 실사·PR 발행은 마쳤다. **PR161의 병합은 사용자 별도 명시 승인 전**이며 자동 병합하지 않는다. 메인이 R-8로 교체할 때의 최소 인계는 정본 `management-active`, branch `docs/management-m1-closeout`, PR161, 이 goal과 위 최종 판정 원문이다. 메인은 새 Astra에 PR 후속 담당을 명시하고 실제 새 handle/runtime/incarnation/READY를 다시 확인한다. 이전 Run·Task·Dispatch를 새 목표의 실행 권한으로 쓰지 않는다. 현재 Astra는 이 결과를 보고한 뒤 새 작업을 시작하지 않는다.
