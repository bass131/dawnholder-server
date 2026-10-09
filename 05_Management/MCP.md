# 개발 기록 공동 조회

Management 앱과 에이전트는 `management-active/05_Management/records/`의 기록 색인 `catalog.json`과 시스템 카드 자료 `system-guide.json`을 같은 원본으로 읽는다. 색인에 등록된 git Markdown 출처는 출처 ID로 원문 구간을 읽을 수 있다. MCP는 로컬 stdio child process로 동작하며 Management 창이 필요 없다. 처음 구현과 검증은 [공동 조회 goal](goals/2026-10-01-shared-read-mcp/goal.md)(PR159·PR161 종료), 색인 v2와 새 도구 세 개는 [기록 원본 일원화 goal](goals/2026-10-06-record-source-unification/goal.md)과 [색인 v2 설계](goals/2026-10-06-record-source-unification/index-v2-design.md)의 「MCP」에서 확인한다. 이 안내나 SDK 시험은 실제 Codex/Claude 개발 세션의 연결 완료를 뜻하지 않는다.

## 준비와 실행

Node/npm 지원 범위는 [package.json](frontend/package.json)의 engines를 따른다. 관리 UI와 같은 frontend 의존성을 lockfile로 설치하고 MCP를 별도로 빌드한다.

```powershell
Set-Location 'C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active/05_Management/frontend'
npm ci
npm run mcp:typecheck
npm run mcp:build
```

`npm ci`는 기존 `node_modules`를 다시 설치하므로 수동으로 받은 Electron 실행 파일도 지운다. 현재 고정 버전은 이 파일을 자동으로 다시 받지 않는다. MCP만 실행할 때는 Electron이 필요 없지만, Management 앱도 사용하려면 같은 frontend에서 이어서 아래 명령을 실행한다. 공식 바이너리를 캐시에서 풀거나 다운로드한다.

```powershell
node node_modules/electron/install.js
```

MCP client가 실행할 명령은 `node`, 인자는 다음 절대경로 하나다.

```text
C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active/05_Management/frontend/mcp-dist/mcp/main.js
```

client는 빌드된 entry를 직접 실행한다. `npm run mcp:build`나 npm의 실행 안내 출력을 stdio 프로토콜에 섞지 않는다. frontend에서 `npm run --silent mcp:start`로 수동 기동할 수도 있지만 연결 설정에는 위 `node` 명령을 사용한다. entry를 직접 실행하면 터미널 메뉴 대신 MCP 요청을 기다린다. stdout은 JSON-RPC, 진단은 stderr다.

원본은 실행 module의 위치로 고정된다. 다른 cwd에서 실행하거나 argv/env/client roots를 전달해도 다른 파일이나 저장소를 선택하지 않는다. 따라서 entry 파일만 다른 폴더로 옮기지 말고 저장소 안의 상대 배치를 유지한다. `mcp-dist/mcp/main.js` 기준 고정 경로는 셋이다.

```text
색인:        ../../../records/catalog.json      → 05_Management/records/catalog.json
카드 자료:   ../../../records/system-guide.json → 05_Management/records/system-guide.json
저장소 루트: ../../../../                        → management-active/
```

원문 구간은 저장소 루트 아래의 색인 locator만 읽고, 그 결과에는 읽은 checkout의 branch·HEAD가 함께 온다.

## 조회 순서

| 순서 | 도구 | 용도 |
|---|---|---|
| 1 | `list_systems` | query/area로 시스템의 분야·제목과 ID 찾기 |
| 2 | `get_system` | 시스템의 분야·출처·관련 시스템·연결 기록 ID 읽기 |
| 3 | `search_records` | query/area/type/systemId로 관련 변경·결정·검증·계획 찾기 |
| 4 | `get_record` | 기록의 종류·연결 시스템·출처·병합 PR 읽기 |
| 5 | `get_source` | 출처의 종류·locator·구간·가용성 메타데이터 읽기 |
| 6 | `read_source_section` | 등록된 출처 ID로 git Markdown 원문 구간 읽기 |
| 7 | `list_guide_cards` | query로 시스템 카드의 제목·연결 시스템·구현 설명 ID 찾기 |
| 8 | `get_guide_card` | 카드 전체와 연결된 구현 설명 문서 읽기 |

색인에는 ID·제목·연결과 병합 PR만 있다. 책임·동작·상태·이유 같은 서술은 출처가 가리키는 프로젝트 문서에 있으므로 `read_source_section`으로 읽는다.

각 응답의 `snapshot.hash`로 읽은 자료를 식별한다. 기록 도구와 `read_source_section`은 색인 파일의 hash, 카드 도구는 카드 자료 파일의 hash다. 앱 화면의 「기록 색인 버전」과 같은 계산이다. `get_source`의 `evidenceRead:false`, `availabilityVerified:false`는 그 도구가 원문을 열거나 현재 가용성을 확인하지 않았다는 뜻이다. 원문은 `read_source_section`으로 따로 읽는다.

`read_source_section`은 출처 ID만 받고 경로를 받지 않는다. 앱과 같은 원문 읽기 모듈을 쓰므로 거절 규칙과 상한(파일 1 MiB, 구간 256 KiB)이 같다. 로컬 전용·전달 메시지·Markdown이 아닌 출처는 `SOURCE_NOT_READABLE`이고, 상위·절대·드라이브 경로, 대소문자만 다른 경로, 링크는 `SOURCE_PATH_REJECTED`다. 결과 `text`는 UTF-16 code unit 기준 `paging.offset`부터 응답 16 KiB 안에서 잘린 조각이다. 다음 조각은 `paging.nextOffset`과 첫 응답의 `sectionHash`를 `expectedSectionHash`로 보내 읽는다. offset이 0보다 크면 `expectedSectionHash`가 필수이고, 없으면 `VERSION_REQUIRED`다. 구간이 바뀌었으면 `VERSION_CONFLICT`다. `nextOffset`이 null이면 끝이다.

처음에는 offset 0 또는 생략으로 조회한다. 이어지는 상세나 관련 조회에도 첫 응답의 `snapshot.hash`를 `expectedHash`로 전달하면 같은 버전을 유지할 수 있다. 뒤 페이지에는 `expectedHash`가 필수다. 응답의 `paging.nextOffset`이 null이면 끝이며, 다음 페이지를 계산할 때 요청한 limit 대신 반환된 nextOffset을 사용한다. 필터를 바꾸면 offset 0에서 시작한다.

`VERSION_CONFLICT`가 나면 새 목록부터 다시 읽고 어떤 버전을 사용할지 판단한다. 서버가 오래된 snapshot으로 조용히 되돌리거나 자동 재시도하지 않는다. 파일 오류에는 정상 data가 없으며 `retryable`과 오류 code로 후속 처리를 구분한다. `RATE_LIMITED`는 `retryAfterMs` 뒤에 다시 요청한다. 원문 구간의 `SOURCE_*`·`SECTION_*` 오류는 대부분 그 링크가 끊겼다는 뜻이며 `details.reason`에 이유가 온다. 그중 재시도로 풀릴 수 있는 것은 `SOURCE_CHANGED_DURING_READ`와 `SOURCE_UNREADABLE`이다. 카드 자료 오류는 `GUIDE_*`다. 색인이 깨져도 카드 도구는 동작하고, 카드 자료가 깨져도 기록 도구는 동작한다. 기존 도구의 오류 코드·입출력 계약은 [goal D2/D3](goals/2026-10-01-shared-read-mcp/goal.md#d2-조회-도구와-응답-제한), 새 도구와 v2 변경은 [색인 v2 설계](goals/2026-10-06-record-source-unification/index-v2-design.md)의 「MCP」를 따른다.

외부 편집 도구가 파일을 제자리에서 나눠 쓰는 동안에는 잠깐 `CATALOG_INVALID`가 나올 수 있다. 편집 도구의 저장이 끝났는지 확인한 뒤 다시 조회하고, 오류가 계속되면 catalog 내용을 확인한다. 서버는 쓰기 중인 파일과 손상된 파일을 구분할 수 없어 이 오류를 자동 재시도하지 않는다.

## 버전과 제한

색인·카드 자료·출처 문서 내용만 수정하면 다음 요청에서 다시 읽으므로 MCP 재빌드는 필요 없다. 같은 작업 트리의 미병합 편집도 공동 조회에 보인다. branch 전환이나 MCP 코드, MCP가 함께 쓰는 앱 모듈(`electron/`의 색인·검색·원문 읽기·checkout·카드 모듈), 의존성 변경 뒤에는 `npm ci`가 필요한지 확인하고 MCP를 재빌드한 다음 client의 MCP child process를 다시 시작한다. 오래된 실행본을 계속 사용하는 방식은 지원하지 않는다.

서버 `serverInfo.version`의 `sha256` digest는 MCP/공통 소스·빌드 설정·lockfile을 식별한다. 이는 색인·카드 자료의 hash와 다르다. SDK 연결 검증은 legacy `2025-11-25`와 modern `2026-07-28`을 각각 대상으로 한다. 실제 client의 협상 결과와 적용 여부는 연결 시 별도로 확인한다.

목록은 기본 10건, 최대 50건이며 출력 크기에 따라 더 적게 반환할 수 있다. 서버가 작성한 text와 structuredContent를 합산한 응답은 최대 16 KiB, 기본 목록 목표는 8 KiB다. 요약의 잘린 필드는 `truncatedFields`에 표시되고 상세를 조용히 잘라서 반환하지 않는다. 한 상세가 한도를 넘으면 `RESPONSE_TOO_LARGE`다. 이 byte 수치는 client가 세는 token 수나 SDK·JSON-RPC의 전체 wire 크기와 같지 않다.

query는 256, ID/area는 128 UTF-16 code units까지 받는다. 기존 catalog의 더 긴 ID는 목록에 보이지만 `lookupSupported:false`이며 상세 조회 한계를 갖는다. 검색은 UI와 동일한 locale 기반 소문자 변환을 사용하므로 기본 locale이 다른 환경 사이의 결과 차이는 가능하다. hash는 기존 UI와 같이 UTF-8 decode 후 재인코딩한 문자열의 SHA-256이며 잘못된 UTF-8의 raw byte hash와 다를 수 있다.

프로세스마다 동시 처리 4개, 대기열 없음, token bucket capacity 10과 초당 10회 충전이다. 별도 client 프로세스는 quota를 공유하지 않는다. 임의 path/URL 조회, 색인에 없는 파일 읽기, 쓰기·서버 제어 도구는 제공하지 않는다. 같은 PC의 다른 파일 편집 도구까지 이 MCP가 통제하는 것은 아니다.

## 실제 개발 세션 연결 제안 — 적용 전

이번 목표에서는 설정을 변경하지 않은 SDK client로 먼저 시험한다. 아래는 실제 세션 연결 방식을 결정할 때 검토할 설정 예시이며 자동으로 적용하지 않는다. MCP entry 경로를 동일한 management-active 정본으로 맞춘다.

Codex가 사용하는 서버 정의는 다음 형태다. 기존 config에 쓰거나 한시적 `-c` override로 전달하는 방법 중 적용 범위를 먼저 정한다. 로컬 `codex --help`에서 dotted key의 TOML override를 확인했지만 이 프로젝트 MCP의 실제 CLI 연결은 별도 검증 대상이다. [공식 Codex MCP 안내](https://learn.chatgpt.com/docs/extend/mcp?surface=cli)

```toml
[mcp_servers.dawnholder_records]
command = "node"
args = ["C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active/05_Management/frontend/mcp-dist/mcp/main.js"]
```

Claude Code의 한시적 `--mcp-config`에 전달할 JSON 예시는 다음과 같다. 파일 경로 또는 JSON 문자열을 받는 옵션은 로컬 CLI 도움말에서 확인했다. 현재 세션에 적용하거나 프로젝트 `.mcp.json`을 생성한 상태는 아니다. `--strict-mcp-config`는 기존 다른 MCP 설정의 로드에도 영향을 주므로 기본 예시에 포함하지 않는다. [공식 Claude Code MCP 안내](https://code.claude.com/docs/en/mcp)

```json
{
  "mcpServers": {
    "dawnholder_records": {
      "type": "stdio",
      "command": "node",
      "args": ["C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active/05_Management/frontend/mcp-dist/mcp/main.js"]
    }
  }
}
```

연결을 적용할 때는 client가 도구 여덟 개를 발견하는지, 실제 조회 hash가 정본과 일치하는지, 출력의 token 경고·파일 대체 여부, 종료 후 child process 정리를 관측한다. SDK 시험 결과와 실제 개발 세션 관측은 goal에서 구분해 기록한다.
