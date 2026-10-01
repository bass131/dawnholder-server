# M-1 — 개발 기록 공동 조회 MCP

## 목표와 재개 근거

사람의 Management UI와 Game Dev/Management 에이전트가 같은 `05_Management/records/catalog.json`을 필요한 만큼 조회한다. 로컬 stdio MCP와 공통 조회 모듈을 만들고 앱이 꺼진 상태에서 독립 client 두 개로 조회·버전 일치·종료를 검증한다. 조회는 목록/검색 → 시스템 요약 → 관련 변경·결정·검증·계획 → 출처 메타데이터 순서다.

2026-10-01 메인 Claude의 Orca 메시지 `msg_a873e160a5bc`가 M-1 재개 결정을 전달했다. 발신 `term_6505bda3-c071-476a-a50a-755c10fa02eb`를 살아 있는 메인 Claude terminal과 대조했다. **메인이 전달한 사용자 결정이며 사용자 직접 입력으로 격상하지 않는다.** [R-10](../../requirements.md#r-10)의 MCP 구현 보류는 이 목표 범위에서 해제한다. 서버 등록·로그 조회와 쓰기·실행 제어는 계속 후속 범위다.

기준은 [공동 조회 완료조건 1~5 및 다음 결정](../2026-09-30-system-records/shared-read-agreements.md)이다. 구현 순서·화면 없는 조회·동일 읽기 권한·공통 원본은 확정 사항으로 다시 묻지 않는다. 해당 합의의 과거 역할·모델·CLI 지정 대신 현재 [AGENTS](../../../AGENTS.md)를 적용한다. 이번 목표의 기준·상태·결과는 이 파일에 모으며 root CURRENT는 Game Dev 소유이므로 변경하지 않는다.

## 범위와 보존 계약

- 허용: 05 안의 읽기 전용 MCP, UI와 공유하는 catalog 계약·검증·읽기·검색 모듈, 필요한 05 package/TypeScript 설정, 독립 테스트와 실행·연결 안내. requirements/decisions/README/RESUME는 이 목표로 연결하는 데 필요한 부분만 후속 갱신한다. 첫 체크포인트는 이 goal 작성·커밋뿐이다.
- MCP에는 catalog 조회 도구만 등록한다. 쓰기·서버 조작·shell·외부 URL 요청·임의 파일 경로 읽기·원문 파일 열기를 노출하지 않는다. 도구의 path/URL 인자, client roots에 따른 읽기 범위 확장, 자동 source locator 해석을 두지 않는다. 읽기 전용 표식은 설명이며 실제 보장은 서버의 등록 도구·의존성·파일 접근 경계와 독립 테스트로 확인한다.
- catalog 본문·기존 ID·출처·`asOf`·`sourceCommit`을 이번 구현으로 최신화하지 않는다. catalog에 적힌 게임 상태와 실제 현재 게임 상태를 구분한다. 기존 UI JSON 불러오기·수정·저장·백업·잠금·낙관적 충돌 처리와 미저장 초안을 보존한다.
- Game Dev 소유 `02_Server`, root CURRENT/goal, AGENTS/.agents, 공유 DLL·PDL·Unity·실행 wrapper는 변경하지 않는다. HTTP/관리백엔드·서버 등록/로그·초안 승인 자동화·새 기록 저장 체계는 포함하지 않는다.
- Codex/Claude 설정(`.codex/config.toml`, `.mcp.json`, `.claude/*`, 사용자 설정 포함)은 변경하지 않는다. 실제 개발 세션 연결에 설정 적용이 필요하면 아래 D4의 사용자 결정으로 올린다. 전역 설정·업데이트·권한 우회는 수행하지 않는다.

## 설계 경계와 현재 근거

현재 `frontend/electron/catalog-contract.ts`는 스키마·중복 ID·참조 검증과 2 MiB 제한을, `catalog-store.ts`는 SHA-256 읽기 버전 및 저장/백업/잠금/충돌을 담당한다. `frontend/src/recordCatalog.ts`에는 UI의 시스템/기록 검색 규칙이 있다. catalog 크기는 75,931 bytes, SHA-256은 `4d81faaaf3dd375d2eebf0acb95983ca0aa2311038782dba6ee7d35ec9554488`이다. 이는 착수 시점의 정적 관측이며 새 MCP 검증 결과가 아니다.

Sol은 05 안에서 다음 책임을 분리한다. 정확한 파일 배치는 이 경계를 유지하는 범위에서 정하고 위임 spec에 소유권을 열거한다.

1. **공통 계약·순수 조회:** Electron/React/MCP 의존성 없이 타입, 스키마·참조 검증, 기존 검색 의미, ID 연결과 DTO를 소유한다. UI와 MCP가 이를 사용하며 기존 import는 필요한 경우 재수출로 보존한다.
2. **고정 원본 읽기:** Node 파일 I/O와 snapshot 생성만 담당한다. production 진입점은 배포된 05 기준 고정 catalog를 module 위치에서 결정하며 caller cwd나 요청 인자로 원본을 고르지 않는다. UI 저장은 기존 write 경계에 남기고 MCP는 write 모듈을 가져오지 않는다. 시험 fixture 주입은 내부 API/시험용 진입점에서만 허용한다.
3. **MCP adapter:** 입력 검증, 공통 조회 호출, 응답 크기·오류 변환, stdio 수명주기를 소유한다. Electron 실행과 분리된 build/start 명령을 제공하고 실행 중 catalog 수정에는 재빌드가 필요 없다. stdout은 프로토콜 전용, 진단은 stderr다.

한 요청은 한 번 확보·검증한 불변 snapshot만 사용한다. 같은 열린 파일의 크기/상태를 읽기 전후 대조하고 변경이 감지되면 부분 결과 없이 오류를 반환한다. UI의 atomic rename으로 교체된 경우 기존 또는 새 완결 snapshot 하나만 반환한다. 외부의 비원자적 편집이 감지되면 읽기 실패로 다루며 정상본 cache로 조용히 대체하지 않는다. 재요청 때 파일을 다시 읽는다. 이 경계의 동시 교체·손상·성장 중 읽기를 검증자에게 인계한다.

## 남은 결정에 대한 추천안

**D1~D4는 제안이며 채택 완료가 아니다.** 공식 근거 확인일은 모두 **2026-10-01**이다. 기술 문서는 아래 선택을 검토하는 근거이며 프로젝트의 수치·권한 결정을 대신하지 않는다. 메인이 goal을 확인하기 전에는 Sol을 발행하지 않는다.

### D1. 프로토콜과 SDK

추천: TypeScript 공식 `@modelcontextprotocol/server` **2.2.0**, 독립 시험 client용 `@modelcontextprotocol/client` **2.2.0**을 exact version과 lockfile로 고정한다. `serveStdio(factory)`로 **2026-07-28** 규격과 **2025-11-25** 초기화 호환 경로를 모두 검증한다. 다른 SDK 기본 지원 revision은 시험한 범위와 구분한다. HTTP·인증·추가 운영 기능은 도입하지 않는다.

공식 [2.2.0 release](https://github.com/modelcontextprotocol/typescript-sdk/releases/tag/v2.2.0)와 [SDK 저장소](https://github.com/modelcontextprotocol/typescript-sdk)는 v2 안정 계열을 제공한다. [규격 전환 안내](https://ts.sdk.modelcontextprotocol.io/v2/migration/support-2026-07-28)는 기존 `initialize` 경로와 새 요청별 규격을 구분하며, [stdio 안내](https://ts.sdk.modelcontextprotocol.io/v2/serving/stdio.html)는 하나의 factory로 구형 client도 수용하는 진입점을 설명한다. 따라서 합의의 초기화 시험을 최신 규격의 필수 wire 메시지라고 오해하지 않는다. legacy에서는 initialize/initialized를, modern에서는 discovery·요청 metadata를 시험한다.

로컬 조회 결과 Node `v24.15.0`, npm `11.13.0`이며 두 2.2.0 package의 `npm view ... version engines peerDependencies --json`은 Node `>=20`을 반환했다. 이는 설치·실행 성공이 아니다. 대안은 v1 `@modelcontextprotocol/sdk`의 legacy 한정 구현이지만 신규 구현의 유지보수 계열과 향후 전환 비용을 고려해 추천하지 않는다. 선택한 정확한 package 설치/API가 공식 문서와 다르면 임의 버전 대체 전에 메인에 보고한다.

### D2. 조회 도구와 응답 제한

추천 도구는 `list_systems`, `search_records`, `get_system`, `get_record`, `get_source` 다섯 개다. 목록은 ID·제목·영역·짧은 요약과 상태를 반환하고 상세는 관련 ID를 제공해 필요한 항목만 이어 읽는다. `get_source`는 catalog에 있는 출처 메타데이터만 반환한다. `locator`는 데이터이며 실행하거나 열지 않고 `evidenceRead: false`, `availabilityVerified: false`로 실제 원문 조회·현존 확인과 구분한다.

| 항목 | 추천 계약 |
|---|---|
| query / ID / area | query 최대 256, ID·area 최대 128 UTF-16 code units. ID는 빈 값 거부. query의 빈 값은 목록 의미이며 검색 정규화/대상 필드는 기존 UI 의미를 공유한다. |
| record type | 기존 `변경`, `결정`, `검증`, `계획`만 허용한다. 알 수 없는 인자·잘못된 enum·범위 밖 수치는 거부한다. |
| 페이지 | `limit` 기본 20, 1~50 정수. `offset` 기본 0, 0~100,000 정수. ID 기준의 고정 정렬, `total`·`nextOffset` 제공. 뒤 페이지는 앞 응답의 `expectedHash` 필수. 필터를 바꾸면 offset 0부터 다시 시작한다. |
| 응답 | `CallToolResult` 전체 JSON의 UTF-8 크기 **65,536 bytes(64 KiB)** 이하. structuredContent와 text를 함께 쓰면 양쪽을 모두 계산한다. JSON-RPC envelope/id와 SDK 자체 프로토콜 오류는 이 application result 한도와 구분한다. |
| 초과 | 목록은 실제 반환 건수까지 줄이고 정확한 nextOffset을 준다. preview 생략은 `truncatedFields`로 표시한다. 단일 상세가 들어가지 않으면 내용 없는 `RESPONSE_TOO_LARGE` 오류를 반환한다. 잘린 상세를 정상 전체 결과라고 표시하지 않는다. |
| 원본 | 기존 최대 **2,097,152 bytes(2 MiB)** 유지. MCP read는 제한 + 1 bytes 이내에서 초과를 판정해 증가 중인 파일을 무제한 읽지 않는다. |

위 수치는 **프로젝트 추천값**이다. [MCP 도구 규격](https://modelcontextprotocol.io/specification/2025-11-25/server/tools)의 입력 검증·구조화 결과·오류 계약을 따르되 이 규격이 64 KiB나 50개를 요구한다고 주장하지 않는다. 기능 설명/schema 자체가 catalog 원문을 포함하지 않게 한다.

### D3. 읽기 오류와 버전 충돌

추천: 성공 응답 공통 envelope는 `ok`, `snapshot: { hash, revision, asOf, sourceCommit }`, `data`, 필요한 paging/생략 표시다. hash는 실제 읽은 catalog의 SHA-256으로 UI `version`과 같은 의미다. revision/asOf/sourceCommit은 catalog의 값을 보존하며 현재 시각·서버 build commit으로 바꾸지 않는다. 검증된 snapshot이 없는 오류는 `snapshot: null`이며 관측 hash가 필요하면 미검증 값으로 별도 구분한다.

각 도구는 선택적 `expectedHash`(64자리 소문자 hex)를 받는다. 현재 정상 snapshot의 hash와 다르면 `VERSION_CONFLICT`와 요청 hash/현재 snapshot metadata만 반환하고 조회 data는 반환하지 않는다. 과거 snapshot 저장·자동 재시도·최신 결과 혼합은 없다. 호출자가 새 목록부터 다시 읽어 버전을 선택한다. 앞 페이지에서 받은 hash로 이후 상세도 고정할 수 있다. hash가 같고 조건이 같으면 두 client의 조회 결과도 같아야 한다.

| 오류 code | 의미와 후속 행동 |
|---|---|
| `INVALID_ARGUMENT` | query/ID/페이지/hash 등 입력을 수정해야 한다. SDK schema 거부도 오류이며 정상 빈 결과로 바꾸지 않는다. |
| `CATALOG_MISSING` / `CATALOG_UNREADABLE` | 원본 부재 또는 접근/I/O 실패. 빈 catalog·오류 없음으로 표시하지 않는다. |
| `CATALOG_TOO_LARGE` / `CATALOG_INVALID` | 크기·JSON·스키마·중복 ID 오류. 자동 복구/초기화하지 않는다. |
| `CATALOG_REFERENCE_BROKEN` | 끊긴 연결을 명시한다. 식별 가능한 오류 예시 최대 5개와 전체 개수만 반환한다. |
| `CATALOG_CHANGED_DURING_READ` | 읽는 동안 변경 감지. 결과를 버리고 호출자가 재요청한다. |
| `NOT_FOUND` | 정상 snapshot에 요청 ID가 없다. 연결된 원문 부재와 혼동하지 않는다. |
| `VERSION_CONFLICT` / `VERSION_REQUIRED` | 버전 불일치 또는 뒤 페이지에 hash 누락. 자동으로 최신 페이지를 섞지 않는다. |
| `RESPONSE_TOO_LARGE` | 최소 한 항목/상세/metadata가 한도에 들어가지 않는다. 본문을 제외하고 생략 사실을 알린다. metadata도 넘치면 snapshot null과 metadata 생략 사유를 준다. |

domain 오류는 `isError: true`와 짧은 code/message/복구 안내를 반환한다. raw JSON·절대 경로·stack·OS 오류 원문을 되돌려 주지 않는다. 잘못된 JSON-RPC 형식/알 수 없는 도구 등 프로토콜 오류는 SDK 계약으로 구분한다. [공식 오류 안내](https://ts.sdk.modelcontextprotocol.io/v2/servers/errors.html)가 두 오류 채널을 구분한다. 출력 schema와 SDK가 자체 생성한 입력 오류까지 검증자 실사에 포함한다.

### D4. 첫 client 연결

추천: **먼저 설정을 변경하지 않는 SDK stdio 시험 client 두 개**가 동일한 빌드된 entry를 각자 child process로 실행하게 한다. 두 프로세스는 같은 Management 정본 catalog를 읽는다. UI·Orca 화면·HTTP·API key는 이 시험의 전제가 아니다. legacy 초기화와 modern 경로를 별도로 시험하며 기록할 값은 client/서버 version, 협상 revision, 원본 hash, 요청/응답과 종료 code다. 시험용 복사본에서 변경·손상·쓰기 보존을 확인하고 production catalog는 변경하지 않는다.

프로젝트 안내에는 `node <management-active>/05_Management/<빌드된 MCP entry>`의 command/args를 명시한다. 패키지 설치·빌드는 사전 단계로 분리해 stdio 시작 중 npm 출력·자동 다운로드를 섞지 않는다. 두 개발 worktree의 각 catalog를 따로 읽도록 예시를 만들지 않는다. 실제 entry 경로는 구현 후 안내와 독립 실행에서 확정한다.

**사용자 결정 항목:** 시험 통과 후 실제 Game Dev/Management 세션을 어느 방식으로 연결할지 메인에게 올린다. 추천은 한시적 세션 연결 예시(Codex `-c` override / Claude `--mcp-config`)를 검토한 뒤 적용하는 것이다. 영구 프로젝트 설정을 택할 경우 적용 checkout·파일·추가 entry의 실제 diff를 먼저 제시한다. 이 체크포인트에서 설정 파일을 만들거나 설정 명령을 실행하지 않는다. CLI override 역시 지금 적용된 것으로 보고하지 않는다.

[OpenAI MCP 안내](https://developers.openai.com/codex/mcp)는 사용자/프로젝트 `config.toml`의 command·args 기반 stdio 설정을 설명한다. [Claude Code MCP 안내](https://code.claude.com/docs/en/mcp)는 `.mcp.json`의 project scope와 `--mcp-config` 연결을 설명한다. 로컬 `codex --help`에서 `-c` override, `claude --help`에서 `--mcp-config`를 확인했지만 실제 연결은 미실행이다. **SDK 시험 성공은 실제 두 개발 세션에 도구가 연결됐다는 뜻이 아니다.**

## 완료조건과 독립 검증

| 합의 번호 | 관찰 가능한 완료조건과 근거 |
|---|---|
| 1 | 두 client가 목록/검색 → 시스템 → 기록 → 출처를 ID로 탐색하고 UI와 같은 원본·검색 의미를 사용한다. unrelated 원문 전체를 선반환하지 않는다. |
| 2 | 응답 hash/revision/asOf/sourceCommit이 해당 fixture/정본과 일치한다. 과거 catalog 시각·출처가 현재 시각/HEAD로 바뀌지 않는다. 원문 미조회 표시를 확인한다. |
| 3 | 한 요청의 단일 snapshot, 동일 hash의 두 client 결과 일치, 재빌드 없는 다음 조회 갱신, 이전 hash 상세·페이지의 명시적 충돌을 시험한다. concurrent rename·손상·읽기 중 증가/변경·오류 뒤 복구도 다룬다. |
| 4 | 경계값/과대 입력, 누락·손상·중복 ID·끊긴 참조·권한 오류, 응답 UTF-8 bytes 한도와 다국어/escaping, pagination·oversized item을 시험한다. 도구 목록·임의 경로/URL 인자 거부·네트워크/쓰기 경계와 fixture 전후 보존을 확인한다. |
| 5 | 앱을 켜지 않고 실제 SDK client/stdio child process로 초기화 또는 modern 협상·도구 목록·읽기·검색·정상 종료/EOF·중간 취소를 실행한다. 두 client 동시 조회와 한 client 종료 시 다른 client 보존을 확인한다. 실제 개발 세션 연결은 별도 관측으로 남긴다. |

기존 Management의 `npm test`, typecheck/build, desktop typecheck/build와 저장/백업/잠금/충돌 회귀를 영향에 맞게 독립 실행한다. 필요한 fixture/하니스/테스트 코드는 신규 Opus 소유이며 Sol의 제품 구현을 복제하는 assertion보다 계약·실패 경로를 검증한다. UI가 공통 경계로 옮겨져 영향을 받으면 실제 Electron의 조회·새로고침·저장과 미저장 초안 보존도 시험용 catalog에서 확인한다. 기존 정본을 검증 fixture로 편집하지 않는다. 별도 fixture 실행 전용 경계가 필요하면 검증자와 Sol 간 파일 소유권을 명시해 순차 반영한다.

루트 .NET 빌드는 이 목표에 필요하지 않으며 Shared DLL 복사 등 부작용을 만들지 않는다. Unity 플레이·게임 서버·DB·실제 개발 세션 연결을 실행하지 않았으면 미실행으로 남긴다. 기존 테스트 통과나 문서 조사만으로 위 완료조건을 충족했다고 판정하지 않는다.

## 작업 순서와 소유권

1. Management Astra가 이 goal과 공식 근거 기반 제안을 커밋한다. 이어 아래 Fable의 계획 검토 시범을 수행하고 판정 원문을 메인에게 전달해 goal 확인을 받는다. 확인 전 Sol은 발행하지 않는다.
2. 확인 후 외부 `gpt-6.1-sol` 구현자 한 명에게 05 제품 코드·필요한 package/build 설정만 위임한다. 기존 테스트는 읽기 전용이며 goal/Git·검증 판정은 Astra 소유다. 최소 관련 파일과 위 계약을 self-contained spec으로 전달한다.
3. Sol의 쓰기 종료·보고·정산을 확인한 뒤 신규 외부 `claude-opus-5-5` 검증자가 실제 diff·실행 근거를 먼저 실사하고 테스트 파일만 작성/보완·실행한다. 제품 결함은 번호로 반환한다. 수정 Sol·재검증 Opus는 매번 신규 세션이며 같은 결함의 3회 재검증 실패는 메인에 올린다.
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

**goal 제안 작성 완료. 커밋 후 Fable 계획 검토 예정. 메인 확인·D1~D4 결정 전이며 구현·독립 제품 검증 미착수.**

- 실제 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`.
- 시작: clean `main`, HEAD `18c8ca6a5aa3032873029cbd36658f0c4f9095c5` (PR156). `git fetch origin main` 후 origin/main도 같은 SHA임을 확인했다.
- 작업 브랜치: `feat/management-shared-read-mcp`, 위 origin/main에서 새로 생성. 첫 커밋 대상은 이 goal 한 파일이다. 원격 push·PR·병합은 아직 수행하지 않았다.
- 현재 runtime `8a673084-6819-45b9-a551-347226cdce9b`, Astra terminal `term_6df8363a-d0bf-454f-aa67-7c7c7323008a`, incarnation `3068c493-4095-4e8c-b907-eed3f3b741e6`. Orca worktree 소속과 실제 cwd 모두 management-active로 확인했다. 이 값은 관찰 기록이며 향후 실행 권한이 아니다.
- READY 회신 `msg_46884ca17d25`를 메인에게 enqueue했다. enqueue 성공을 메인이 읽거나 승인했다는 근거로 쓰지 않는다.
- 현행 코드·문서의 좁은 정적 조사, 버전/registry metadata 조회, 공식 문서 확인만 수행했다. package 설치·MCP/앱 실행·테스트·빌드·독립 판정·설정 변경은 수행하지 않았다.
- 원시 receipt/로그와 판정 원문은 Git 제외 `.backups/verification/2026-10-01-shared-read-mcp/`에 보존한다. 아직 계획/구현/검증 판정 원문은 없다. 원문은 로컬 근거이며 원격 가용성을 보장하지 않는다.

남은 판단은 D1~D3의 기술 계약 채택, D4의 시험 client 우선 방식과 실제 개발 세션 연결 방식이다. 치명 결함이 발견됐다는 뜻은 아니며 구현·호환성·회귀가 아직 검증되지 않은 상태다.
