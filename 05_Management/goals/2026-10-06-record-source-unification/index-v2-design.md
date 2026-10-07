# 기록 색인 v2 설계

[goal](goal.md)의 PR2(「만들 것」 1~5)를 구현하기 전에 리드가 고정한 경계와 인터페이스다. TDD 선행 시험 작성자, 구현 Sol, 독립 검증자가 같은 기준으로 이 문서를 읽는다. 이 문서와 시험이 다르면 시험을 고치기 전에 리드에게 묻는다.

설계 원칙은 goal 「설계와 검증 경계」 그대로다. 낡을 수 있는 서술·상태는 빼고, 원문을 찾는 데 필요한 이름·분류·연결·위치만 남긴다. 원문은 실행 중인 checkout의 파일을 읽는다(사용자 답 「질문 2 → A」). 앱과 MCP는 Git 명령을 실행하지 않고 파일을 쓰지 않는다.

## 색인 형식

`05_Management/records/catalog.json`은 `schemaVersion: 2`만 받는다. 객체마다 아래 키만 허용하고 다른 키가 있으면 색인 전체를 `invalid`로 거절한다. 서술·상태 필드가 다시 들어오지 못하게 하는 장치다.

| 객체 | 키 | 규칙 |
|---|---|---|
| 최상위 | `schemaVersion`·`sources`·`systems`·`records` | `asOf`·`sourceCommit`·`scopeNote`·수기 `revision`은 없다. 버전은 파일 내용 SHA-256이다(`catalog-hash.ts`) |
| 출처 | `id`·`title`·`kind`·`locator`·`section`·`availability` | 아래 「출처」 |
| 시스템 | `id`·`title`·`area`·`sourceIds`·`relatedSystemIds`·`recordIds` | 시스템 ID는 바꾸지 않는다. `system-guide.json`의 `relatedSystemIds`가 가리킨다 |
| 기록 | `id`·`type`·`title`·`systemIds`·`sourceIds`·`pullRequests` | `type`은 `변경`·`결정`·`검증`·`계획`. `pullRequests`는 `{ number, mergeCommit }` 배열이며 비어도 된다 |

- ID는 종류별로 유일하고 `^[a-z0-9]+(?:-[a-z0-9]+)*$`, 128자 이하다. 제목·분야는 앞뒤 공백 없는 비지 않은 문자열이다.
- `pullRequests[].number`는 양의 정수, `mergeCommit`은 소문자 40자리 hex다. 한 기록 안에서 PR 번호는 겹치지 않는다.
- 참조 ID(`sourceIds`·`relatedSystemIds`·`recordIds`·`systemIds`)가 없는 ID를 가리키면 색인 전체를 `invalid`로 거절한다. 지금의 `catalogReferenceErrors` 동작을 유지한다.
- 크기 상한 2 MiB(`MAX_CATALOG_BYTES`)는 그대로다.

**출처.** `kind`와 `availability`는 짝이 맞아야 한다. `git`은 `versioned`, `local`·`handoff`는 `local-only`다.

| kind | locator | section | 앱·MCP에서 |
|---|---|---|---|
| `git` | 저장소 루트 기준 상대경로(`/` 구분) | `.md`면 그 파일의 정확한 제목 문구 하나 또는 `null`(파일 처음부터), 그 밖의 확장자는 `null` | `.md`만 원문 구간을 읽는다. 그 밖의 확장자는 경로만 보인다 |
| `local` | Git 제외 로컬 파일의 저장소 루트 기준 상대경로(예: `.backups/…`) | `null` | 읽지 않는다. 「로컬 전용」으로 경로만 보인다 |
| `handoff` | Orca 메시지 ID(`^msg_[0-9a-f]{12}$`) | `null` | 읽지 않는다. 「전달 메시지」로 ID만 보인다 |

색인 계약(`readCatalog`)은 locator를 512자 이하·제어 문자 없는 비지 않은 문자열로만 검사한다. kind별 모양과 경로 안전성은 출처마다 원문 읽기 경계와 색인 검사가 판정한다. 잘못된 locator 하나가 색인 전체를 막지 않고 그 링크만 끊긴 링크로 보이게 하려는 선택이다.

**정확한 제목 문구.** Markdown ATX 제목 줄(앞 공백 0~3칸, `#` 1~6개)에서 앞의 `#`와 공백, 뒤의 닫는 `#`와 공백을 뺀 나머지 문자열 그대로다. 굵게·코드 표시도 원문 그대로 비교한다. 펜스 코드 블록(``` 또는 ~~~) 안의 줄은 제목으로 보지 않는다. Setext 제목은 지원하지 않는다. 파일 첫머리의 BOM은 제목 판정 전에 무시한다.

**구간.** 지정한 제목 줄부터 같은 단계 이하(`#` 개수가 같거나 적은) 다음 제목 직전까지다. 제목 줄을 포함하고 줄바꿈은 원문 그대로다. 같은 문구의 제목이 둘 이상이면 모호한 구간으로 거절한다.

## 원문 구간 읽기 경계

책임은 둘로 나눈다. `electron/source-section-contract.ts`는 순수 판정(경로 분류, 확장자, 제목·구간 추출, 결과 형식, 상한)을 맡는다. `electron/source-section-store.ts`는 파일 I/O(링크 확인, 정규 경로 대조, 제한 읽기, 엄격한 UTF-8 해석)를 맡는다. 저장소 루트는 호출자가 고정 경로로 넘긴다. 앱은 `desktop-dist/main.js` 기준 `../../../`, MCP는 `mcp-dist/mcp/main.js` 기준 `../../../../`, 색인 검사 CLI는 `scripts/` 기준 `../../../`다. 인자·환경 변수·cwd·renderer·MCP client가 루트를 고르지 못한다.

상한은 파일 1 MiB(1,048,576 바이트), 구간 256 KiB(262,144 UTF-8 바이트)다. 현재 저장소에서 가장 큰 추적 Markdown은 478,247 바이트다(3b90ab6f 작업 트리의 `git ls-files '*.md'` 크기 정렬).

판정 순서는 아래와 같다. 앞 단계에서 거절하면 뒤 단계는 하지 않는다.

1. 요청 검증: 출처 ID 문자열(위 ID 규칙). 아니면 `invalid-request`.
2. 색인 읽기: 실패하면 `index-unavailable`.
3. 출처 찾기: 없으면 `unknown-source`.
4. 읽을 수 있는 종류인가: `local`·`handoff`는 `not-readable`(reason `local-only`·`handoff`).
5. 경로 모양: 드라이브 문자 → 절대 경로 → 상위 경로 → 그 밖의 잘못된 모양 순으로 `path-rejected`(reason `drive`·`absolute`·`parent`·`invalid`). 역슬래시·빈 구간·`.` 구간·제어 문자·와일드카드·`:`는 `invalid`다.
6. 확장자: 정확히 소문자 `.md`가 아니면 `not-readable`(reason `extension`). `.MD`·`.md.txt`·`.cs`·`.html`이 여기 걸린다.
7. 파일 시스템: 루트부터 구간마다 `lstat`해 심볼릭 링크·정션이 있으면 `path-rejected`(reason `link`), 없으면 `missing`이다. 대상의 native realpath가 `realpath(루트)`에 구간을 이은 경로와 문자열로 다르면 `path-rejected`(reason `case`)다. 대소문자를 구분하지 않는 Windows 파일 시스템에서 대소문자만 다른 경로를 잡는다(구분하는 파일 시스템에서는 `missing`). 마지막 구간의 `lstat`이 일반 파일이 아니면(예: `.md` 이름의 디렉터리) `not-readable`(reason `not-file`)이다.
8. 읽기: 연 handle의 `fstat`이 일반 파일이 아니거나 앞 단계 `lstat`의 dev·ino와 다르면 `changed`다. 7단계와 8단계 사이에 파일이 바뀐 경우만 여기 온다. 1 MiB 넘으면 `too-large`(reason `file`)다. 읽기 전후 stat이 다르면 `changed`, 엄격한 UTF-8이 아니면 `invalid-encoding`이다. 제한 읽기 방식은 `system-guide-store.ts`를 따른다.
9. 구간: 제목이 없으면 `section-missing`, 둘 이상이면 `section-ambiguous`, 256 KiB 넘으면 `too-large`(reason `section`)다.

7·8단계에서 파일 없음이 아닌 예상 밖 I/O 오류(권한 거부 등)는 `load`(reason `null`)다. 색인 검사는 이를 그 출처의 실행 못 함으로 다룬다(「색인 검사」).

경계의 한계: 7단계의 `lstat`·realpath 판정은 hard link를 잡지 못한다. 루트 밖 파일을 가리키는 hard link가 루트 안에 있으면 읽힌다. git은 hard link를 저장하지 않아 로컬에서 직접 만들어야 생기며, 승인된 거절 기준(아래 8종) 밖이라 이번 범위에서 막지 않는다(메인 결정 `msg_883836e1208a` 2항, 후속 후보).

성공 결과는 `{ ok: true, sourceId, path, heading, text, bytes }`이다. `path`는 locator 그대로, `bytes`는 `text`의 UTF-8 바이트 수다. `section: null`이면 `heading`도 `null`이고 `text`는 BOM을 뺀 파일 전체다(256 KiB 상한은 그대로). 실패 결과는 `{ ok: false, code, reason, message }`다. `reason`은 위 표기 값 또는 `null`이고 `message`는 고정 한국어 문장이며 입력·경로·원시 오류를 담지 않는다. IPC 발신자가 신뢰되지 않으면 `denied`다.

진입 함수는 둘이고 모두 `electron/source-section-store.ts`에서 export한다(`source-section-contract.ts`는 순수 판정만). 시험과 구현이 같은 이름을 쓴다. 같은 방식으로 `createCheckoutStore`는 `electron/checkout-store.ts`, `checkRecordIndex`는 `electron/record-index-check.ts`에서 export한다.

- `createSourceSectionStore({ repositoryRoot })` → `{ read(source: RecordSource): Promise<SourceSectionResult> }`. 4~9단계다. MCP는 자기 스냅샷에서 출처를 찾은 뒤 이 함수를 부른다.
- `createSourceSectionReader({ readCatalog, store })` → `{ read(input: unknown): Promise<SourceSectionResult> }`. 1~3단계 뒤 `store.read`에 넘긴다. `readCatalog`는 `() => Promise<CatalogResult>`다. IPC 처리기는 `trustedSender` 확인 뒤 이 `read`에 그대로 위임한다.

### 거절 사례 8종

메인 결정(`msg_4c7e21aeead6` 2항)의 사례다. PR2 시험은 사례마다 앱 쪽 진입(`createSourceSectionReader().read`, 시험용 루트와 fixture 색인)과 MCP 도구 결과를 모두 확인한다. IPC 처리기 시험은 발신자 거절과 위임 연결을 확인한다. `main.ts`의 루트·색인 경로는 고정이라 fixture를 넣을 수 없기 때문이다.

| 사례 | 예시 | 앱 결과 | MCP 오류 |
|---|---|---|---|
| 상위 경로 | `../outside.md`, `docs/../../outside.md` | `path-rejected`·`parent` | `SOURCE_PATH_REJECTED`·`parent` |
| 절대 경로 | `/outside.md`, `//server/share/a.md` | `path-rejected`·`absolute` | `SOURCE_PATH_REJECTED`·`absolute` |
| 드라이브 문자 | `C:/outside.md`, `c:outside.md` | `path-rejected`·`drive` | `SOURCE_PATH_REJECTED`·`drive` |
| 대소문자만 다른 경로 | 실제 `docs/Guide.md`를 `docs/guide.md`로 등록 | `path-rejected`·`case` | `SOURCE_PATH_REJECTED`·`case` |
| 심볼릭 링크·정션 | 루트 안 정션 아래 `.md` | `path-rejected`·`link` | `SOURCE_PATH_REJECTED`·`link` |
| 허용 밖 확장자 | `a.cs`, `a.MD`, `a.md.txt` | `not-readable`·`extension` | `SOURCE_NOT_READABLE`·`extension` |
| 크기 상한 초과 | 1 MiB 넘는 파일, 256 KiB 넘는 구간 | `too-large`·`file`/`section` | `SOURCE_TOO_LARGE`·`file`/`section` |
| 등록되지 않은 출처 ID | `no-such-source` | `unknown-source` | `NOT_FOUND` |

정션은 관리자 권한 없이 `fs.symlink(target, path, 'junction')`으로 만든다. 파일 심볼릭 링크 생성이 권한으로 막히면 그 사실을 시험 결과에 남기고 정션 사례로 판정한다.

## checkout 정보

`electron/checkout-contract.ts`(순수 해석)와 `electron/checkout-store.ts`(I/O)가 읽은 checkout의 branch·HEAD를 만든다. Git 명령을 실행하지 않고 아래 고정 이름의 파일만 4 KiB 이하로 읽는다. `packed-refs`만 1 MiB 이하다.

1. `<루트>/.git`을 `lstat`한다. 링크면 unknown(`link`)이다. 디렉터리면 그것이 gitdir이자 commondir이다.
2. 파일이면 `gitdir: <절대 경로>` 한 줄이어야 한다. 그 gitdir의 `gitdir` 파일(역링크)이 `<루트>/.git`을 가리켜야 한다. 아니면 `pointer-invalid`·`backlink-mismatch`다. `commondir`이 있으면 gitdir 기준으로 풀어 commondir로 쓴다.
3. `<gitdir>/HEAD`가 `ref: refs/heads/<이름>`이면 branch, 40 또는 64자리 hex면 분리된 HEAD다. ref 이름은 `refs/heads/`로 시작하고 `..`·역슬래시·제어 문자·`.`로 시작하는 구간·`.lock` 끝이 없어야 한다(`ref-invalid`).
4. ref 값은 `<gitdir>/<ref>` → `<commondir>/<ref>` → `<commondir>/packed-refs` 순으로 찾는다(`#`·`^` 줄 무시). 없으면 `ref-missing`이다.

진입 함수는 `createCheckoutStore({ repositoryRoot })` → `{ read(): Promise<CheckoutInfo> }`다. 결과는 `{ state: 'known', branch: string | null, head: string }` 또는 `{ state: 'unknown', reason }`이다. reason은 `no-git`·`link`·`pointer-invalid`·`backlink-mismatch`·`head-invalid`·`ref-invalid`·`ref-missing`·`too-large`·`load`다. 참고 구현은 `99_Tools/ModuleBoundaries/inputs.py`의 `git_command`(역링크 확인, 4096 바이트 상한)다.

## Electron 경계

| IPC 채널 | 입력 | 결과 |
|---|---|---|
| `system-records:read` | 없음 | `{ ok: true, catalog, version }` 또는 `{ ok: false, code, message }`(`missing`·`invalid`·`load`·`denied`). 편집용 `text`는 없다 |
| `system-records:read-section` | 출처 ID 문자열(`unknown`으로 받아 검증) | 원문 구간 결과. 처리기는 매번 색인을 새로 읽어 출처를 찾는다. renderer는 경로를 넘기지 못한다 |
| `system-records:read-checkout` | 없음 | `{ ok: true, checkout }` 또는 `denied` |
| `system-guide:read` | 없음 | 지금 그대로 |

- `system-records:save`와 `catalog-store.ts`의 저장·백업·잠금·충돌 처리, `catalog-rename.ts`를 없앤다. `catalog-store.ts`는 읽기 전용 store로 남긴다. CODE_CONVENTION의 TS·Electron 절이 이 파일을 관례 출발점으로 링크하므로 파일을 지우지 않는다.
- 모든 처리기는 지금의 `trustedSender`를 먼저 거친다. preload는 `systemRecords.readCatalog()`·`readSection(sourceId)`·`readCheckout()`과 `systemGuide.readGuide()`만 노출한다.
- 의존 방향은 원문·checkout 계약 → 색인 계약이다. `catalog-contract.ts`는 `source-section-contract.ts`·`checkout-contract.ts`를 import하지 않는다. 세 결과를 묶는 renderer bridge 타입(`systemRecords`)은 `src/recordsBridge.d.ts`에 둔다. 색인 계약이 거꾸로 import하면 순환이 생기고, MCP 빌드 그래프에 원문 모듈이 4·5단계보다 먼저 들어간다.
- `catalog-store.ts`의 `.verification/system-records-last-good.json` 백업 경로 인자는 없앤다. 이미 있는 사용자 산출물 파일은 지우지 않는다.

## 화면

개발 현황의 보기 전환 버튼 「개발 기록 · 기록 편집」은 「개발 기록」으로 바꾼다. 개발 기록 탭은 읽기 전용이다. 기록 편집·JSON 불러오기·초안 검증·저장·편집 취소가 없다. 시험이 같은 이름으로 찾도록 아래 문구를 고정한다.

- 위 막대: 「기록 새로고침」 버튼, 상태 문장(`role="status"`), 「읽은 checkout: 」으로 시작하는 줄. 알면 `<branch> · HEAD <앞 12자리>`, 분리된 HEAD면 `분리된 HEAD · HEAD <앞 12자리>`, 모르면 `알 수 없음`이다.
- 소개의 스냅샷 정보는 「기록 색인 버전 <SHA-256 앞 12자리>」 하나다. 기준일·소스 commit·범위 메모는 없다.
- 보기 전환 버튼은 「시스템 N」·「변경·결정·검증·계획 N」·「출처 N」이다(이전 「근거」를 「출처」로). 목록: 시스템은 분야·제목, 기록은 종류·제목, 출처는 제목·종류·경로·구간이다. 상태 문구는 없다.
- 검색: 시스템은 ID·제목·분야·연결 출처 제목, 기록은 ID·제목·`#PR번호`·연결 출처 제목, 출처는 ID·제목·경로·구간이 대상이다.
- 시스템 상세: 분야, 관련 시스템, 연결 기록, 「원문」 목록. 기록 상세: 종류, `PR #<번호> · 병합 <앞 7자리>` 줄, 연결 시스템, 「원문」 목록. 「원문」 목록은 접지 않고 늘 보인다(이전 「근거 N개」 접기와 `evidenceOpen` 복원은 없앤다).
- 「원문」 목록의 읽을 수 있는 출처는 접근 이름 `<출처 제목> 원문 읽기` 버튼이다. 읽지 않는 출처는 `<출처 제목> · 앱에서 읽지 않음(로컬 전용 | 전달 메시지 | Markdown 아님)`과 locator를 글자로 보인다.
- 버튼을 누르면 출처 화면으로 이동한다(이동 기록에 쌓여 「← 뒤로」로 돌아온다). 제목은 출처 제목, 메타는 경로와 구간 제목(또는 「파일 처음부터」)이다. 본문은 `aria-label="원문 구간"`인 `<pre>`에 글자 그대로 넣는다. Markdown을 HTML로 바꾸지 않는다.
- 읽기 실패는 `role="alert"`로 「끊긴 링크: 」 뒤에 읽기 실패 결과의 `message`를 그대로 보인다. 이전에 읽은 다른 구간의 본문을 남겨 보이지 않는다. 늦게 도착한 이전 요청의 결과는 지금 출처 화면에 반영하지 않는다.
- 출처 목록 보기의 항목도 「원문」 목록과 같다. 읽을 수 있는 출처는 `<출처 제목> 원문 읽기` 버튼이고 누르면 같은 출처 화면으로 간다. 읽지 않는 출처는 글자로 보인다.

## MCP

DTO를 색인 v2로 바꾸고 도구 세 개를 더한다. 실제 개발 세션 연결은 하지 않는다. 응답 상한 16 KiB, 목록 기본 8 KiB 예산, 동시 4건·토큰 10개(초당 10개 보충), 고정 오류 문장, 알 수 없는 도구 이름의 사전 거절은 그대로다. 새 도구는 `inputSchemas`에 넣어야 transport가 받는다.

| 도구 | 입력 | 결과 데이터 |
|---|---|---|
| `list_systems` | 지금 그대로 | 미리보기 `{ id, area, title, lookupSupported, truncatedFields }` |
| `search_records` | 지금 그대로 | 미리보기 `{ id, type, title, systemIds, lookupSupported, truncatedFields }` |
| `get_system`·`get_record` | 지금 그대로 | v2 객체 전체 |
| `get_source` | 지금 그대로 | `{ source, evidenceRead: false, availabilityVerified: false }` |
| `read_source_section` | `{ id, offset?, expectedSectionHash?, expectedHash? }` | `{ source, path, heading, text, sectionHash, checkout, paging: { offset, returned, total, nextOffset } }` |
| `list_guide_cards` | `{ query?, limit?, offset?, expectedHash? }` | 미리보기 `{ id, parentId, title, relatedSystemIds, documentId, lookupSupported, truncatedFields }` |
| `get_guide_card` | `{ id, expectedHash? }` | `{ card, document }`(`document`는 없으면 `null`) |

- 스냅샷 메타데이터는 `{ hash }` 하나다. 기록 도구는 색인 파일 SHA-256, 카드 도구는 `system-guide.json` SHA-256이다. 앱의 `version`과 같은 계산(`catalog-hash.ts`의 UTF-8 해석 텍스트 해시)이므로 같은 파일이면 같은 값이다.
- `read_source_section`은 원문 구간 경계 모듈을 그대로 쓴다. 출처 ID로만 받고 경로 입력은 없다. `text`는 UTF-16 offset 기준으로 잘라 응답이 16 KiB를 넘지 않게 한다(대리 쌍을 가르지 않는다). `offset > 0`이면 `expectedSectionHash`가 필수이며(`VERSION_REQUIRED`) 다르면 `VERSION_CONFLICT`다. `sectionHash`는 구간 텍스트의 SHA-256이다.
- 새 오류 코드: `SOURCE_NOT_READABLE`·`SOURCE_PATH_REJECTED`·`SOURCE_MISSING`·`SOURCE_TOO_LARGE`·`SOURCE_CHANGED_DURING_READ`·`SOURCE_INVALID_ENCODING`·`SOURCE_UNREADABLE`·`SECTION_MISSING`·`SECTION_AMBIGUOUS`, 카드 파일용 `GUIDE_MISSING`·`GUIDE_UNREADABLE`·`GUIDE_TOO_LARGE`·`GUIDE_INVALID`·`GUIDE_CHANGED_DURING_READ`. reason은 `details.reason`의 고정 값이고 경로를 담지 않는다. 출처 ID가 없으면 지금처럼 `NOT_FOUND`다.
- 검증은 새로 빌드한 실행본(`npm run mcp:build` → `mcp-dist`)으로 한다. 빌드 digest 시험의 기준은 그 새 출력이다.
- 카드 도구는 `system-guide-store.ts`의 `createSystemGuideStore`를 요청마다 새로 만들어 읽는다. 앱과 같은 판정·hash를 쓰려는 선택이다. 요청마다 만들므로 앱 store의 `busy`(한 store의 동시 읽기 거절)는 MCP에서 생기지 않는다. 결과 코드는 `missing`→`GUIDE_MISSING`, `load`→`GUIDE_UNREADABLE`, `too-large`→`GUIDE_TOO_LARGE`, `invalid`→`GUIDE_INVALID`, `changed`→`GUIDE_CHANGED_DURING_READ`로 바꾼다. 대안인 MCP 전용 카드 리더는 같은 판정을 두 곳에 두게 된다.
- `read_source_section`의 `checkout`은 `createCheckoutStore({ repositoryRoot }).read()`의 결과(`CheckoutInfo`)다.
- MCP `main.ts`의 고정 경로는 셋이다. `mcp-dist/mcp/main.js` 기준 색인 `../../../records/catalog.json`, 카드 `../../../records/system-guide.json`, 저장소 루트 `../../../../`다.

**MCP 빌드 경계.** MCP가 앱 모듈을 재사용하므로 지금의 경계 시험(허용 공유 모듈 3개, 허용 bare import, 금지 호출, 파일 열기 위치)을 아래로 바꾼다. 시험 갱신은 4단계, 구현은 5단계다.

| 항목 | 지금 | 바뀐 뒤 |
|---|---|---|
| 빌드 그래프의 `electron/` 모듈 | `catalog-contract`·`catalog-hash`·`catalog-query` | 그 셋과 `source-section-contract`·`source-section-store`·`checkout-contract`·`checkout-store`·`system-guide-contract`·`system-guide-store`. `main`·`preload`·`catalog-store`·`src/`는 계속 금지 |
| 허용 bare import | MCP SDK 두 개, `node:crypto`, `node:fs/promises`, `node:url`, `zod` | 그 목록과 `node:fs`·`node:path`·`node:util` |
| 파일 열기 | MCP 리더만 | MCP 리더와 위 store 세 개(`source-section-store`·`checkout-store`·`system-guide-store`) |
| 프로세스 실행 금지 | `exec(` 문자열 검사 | 목적은 그대로다. `child_process` import와 프로세스 실행 호출을 막고, 정규식의 `.exec(` 호출은 허용한다 |

- 빌드 digest는 위 모듈이 그래프에 들어오므로 그 파일 변경에도 바뀌어야 한다. 색인·카드 데이터 변경에는 바뀌지 않는다.
- 소유 TEMP 사본으로 빌드하는 시험 helper(`tests/mcp-v3/temp-copy.ts`)는 지금 `catalog.json`만 복사한다. 카드 자료와 원문 구간 시험에 필요한 저장소 파일을 복사하도록 4단계에서 늘린다.
- `mcp-dist`는 Git 제외 빌드 출력이다. 이 설계를 쓸 때는 2026-10-02의 v1 빌드였고, 5단계가 `npm run mcp:build`로 새로 만들었다(goal 「만들 것」 4의 「새로 빌드한 실행본」, 결과는 goal 「PR2 MCP 구현」). 빌드 전에 옛 출력을 근거 폴더에 복사해 둔다. 기존 실패 B11·B12는 옛 출력과 비교하던 시험이므로 새 출력 기준으로 다시 판정한다.

**MCP 서버 주입 지점.** 시험과 구현이 같은 이름을 쓴다. `createCatalogServer(options)`의 `CatalogServerOptions`에 아래 셋을 더한다. 기존 `readSnapshot`·`version`·`now`·`onToolHandlerEntered`는 그대로다.

| 이름 | 형식 | `main.ts`의 연결 |
|---|---|---|
| `readGuide` | `(signal: AbortSignal) => Promise<GuideResult>` | 요청마다 `createSystemGuideStore(<카드 경로>).read()` |
| `readSourceSection` | `(source: RecordSource, signal: AbortSignal) => Promise<SourceSectionResult>` | `createSourceSectionStore({ repositoryRoot }).read(source)` |
| `readCheckout` | `(signal: AbortSignal) => Promise<CheckoutInfo>` | `createCheckoutStore({ repositoryRoot }).read()` |

대안은 서버가 `repositoryRoot`와 카드 경로를 받아 store를 직접 만드는 것이었다. 그러면 메모리 안 시험도 파일 시스템 fixture를 만들어야 하고, 앱과 MCP의 store 연결이 두 곳으로 갈린다. 주입 지점은 `main.ts` 한 곳에서만 실제 store에 잇는다. signal은 취소 확인에 쓰며 store 자체는 signal을 받지 않는다.

**도구 규칙.**

- 스냅샷: 기록 도구 다섯 개와 `read_source_section`의 `snapshot`은 `{ hash: <색인 hash> }`, 카드 도구 두 개의 `snapshot`은 `{ hash: <카드 자료 version> }`이다. 카드 자료 version은 `GuideResult.version`이다. `expectedHash`는 각 도구의 `snapshot.hash`와 비교하고 다르면 `VERSION_CONFLICT`(`details.expectedHash`)다.
- 판정 순서는 기존 도구와 같다. 취소 → 받아들임(동시·빈도) → 앞 페이지 버전 필수 검사 → 자료 읽기 → `expectedHash` 비교 → ID 찾기(`NOT_FOUND`) → 도구별 처리다. `VERSION_REQUIRED` 대상은 `list_systems`·`search_records`·`list_guide_cards`의 `offset > 0`과 `expectedHash` 없음, `read_source_section`의 `offset > 0`과 `expectedSectionHash` 없음이다.
- `read_source_section`: 입력 `id`는 1~128자, `offset`은 0~262,144 정수, `expectedSectionHash`·`expectedHash`는 소문자 64자리 hex다. 스냅샷에서 출처를 찾은 뒤 `readSourceSection(source)`를 부른다. 실패 결과는 아래 표의 코드로 바꾸고 `details.reason`에 결과의 `reason`을 그대로 넣는다(`null`이면 `details`를 생략). 성공하면 `sectionHash`(구간 `text`의 UTF-8 SHA-256)를 계산하고, `offset > 0`이면 `expectedSectionHash`와 비교해 다르면 `VERSION_CONFLICT`(`details.expectedSectionHash`)다. 그 뒤 `readCheckout()`으로 `checkout`을 채운다.
- 페이지: `offset`과 `returned`·`total`·`nextOffset`은 `text`의 UTF-16 code unit 기준이다. `total`은 `text.length`다. 서버는 응답 전체(기존 `responseBytes` 기준)가 16 KiB를 넘지 않는 가장 긴 조각을 `offset`부터 돌려준다. 조각 끝이 대리 쌍 가운데면 한 칸 줄인다. `offset`이 대리 쌍 가운데면 `INVALID_ARGUMENT`다. `offset ≥ total`이면 `text: ''`, `returned: 0`, `nextOffset: null`이다. 마지막 조각의 `nextOffset`은 `null`이다. 빈 조각으로도 16 KiB를 넘으면(긴 출처 필드 등) 기존처럼 `RESPONSE_TOO_LARGE`다. `offset < total`인데 문자 하나도 담지 못해도 `RESPONSE_TOO_LARGE`다.
- 결과의 `source`는 `get_source`와 같은 출처 객체, `path`는 locator, `heading`은 결과의 `heading`(구간이 `null`이면 `null`)이다.
- `list_guide_cards`: 입력은 `list_systems`와 같은 `query`(256자)·`limit`(1~50, 기본 10)·`offset`(0~100,000)·`expectedHash`다. `query`는 `matchesQuery`로 카드 `id`·`title`·`summary`를 찾는다. 순서는 카드 자료의 배열 순서다. 미리보기는 `{ id, parentId, title, relatedSystemIds, documentId, lookupSupported, truncatedFields }`이고 `title`은 80 UTF-16 code unit에서 자른다. `documentId`가 없으면 `null`, `lookupSupported`는 `id`가 128자 이하인지다. 목록 예산(기본 8 KiB, 그 밖 16 KiB)은 기존 목록 도구와 같다.
- `get_guide_card`: 입력 `id`는 1~128자다. 결과는 `{ card, document }`이고 `card`는 카드 객체 전체, `document`는 `card.documentId`와 같은 `id`의 구현 설명 문서 전체 또는 `null`이다. 카드가 없으면 `NOT_FOUND`다.
- 새 도구 세 개도 읽기 전용 annotation(`readOnlyHint: true`, `destructiveHint: false`, `idempotentHint: true`, `openWorldHint: false`)을 쓰고 `inputSchemas`·`outputSchemas`에 들어간다.
- 도구마다 읽는 자료가 다르다. 카드 도구 두 개는 `readGuide`만 부르고 `readSnapshot`을 부르지 않는다. 기록 도구 다섯 개와 `read_source_section`은 `readGuide`를 부르지 않는다. 색인이 깨져도 카드 조회가 막히지 않고, 카드 자료가 깨져도 기록 조회가 막히지 않게 하려는 선택이다.
- `readCheckout`은 `read_source_section`의 응답이 성공으로 끝날 때만 맨 마지막에 부른다. 원문 실패와 `expectedSectionHash` 불일치에서는 부르지 않는다.
- 오류 응답의 `snapshot`: `GUIDE_*`는 `null`이다(카드 자료를 읽지 못해 hash가 없다). `SOURCE_*`·`SECTION_*`와 `expectedSectionHash` 불일치는 색인 metadata `{ hash }`다. 기존 `NOT_FOUND`·`VERSION_CONFLICT`처럼 어느 색인 버전의 링크가 끊겼는지 알려 준다.
- 카드 미리보기 `title`의 80자 자르기는 기존 목록 미리보기와 같은 `previewText`(끝이 대리 쌍 가운데면 79)다.
- 원문 결과 `load`(예상 밖 I/O 실패)는 `SOURCE_UNREADABLE`(재시도 가능, `details` 생략)이다. 원인이 색인이 아니라 출처 파일이므로 `CATALOG_UNREADABLE`로 바꾸지 않는다.
- `read_source_section`의 처리 순서: `readSourceSection` → 실패 코드 변환 → `sectionHash` 계산과 `expectedSectionHash` 비교 → `offset` 대리 쌍 검사(`INVALID_ARGUMENT`) → `readCheckout` → 조각 크기 결정(`RESPONSE_TOO_LARGE`). 색인을 읽은 뒤 생기는 이 오류들의 `snapshot`은 색인 metadata `{ hash }`다.

| 원천 결과 | MCP 오류 코드 | 재시도 가능 |
|---|---|---|
| 원문 `not-readable` | `SOURCE_NOT_READABLE` | 아니오 |
| 원문 `path-rejected` | `SOURCE_PATH_REJECTED` | 아니오 |
| 원문 `missing` | `SOURCE_MISSING` | 아니오 |
| 원문 `too-large` | `SOURCE_TOO_LARGE` | 아니오 |
| 원문 `changed` | `SOURCE_CHANGED_DURING_READ` | 예 |
| 원문 `invalid-encoding` | `SOURCE_INVALID_ENCODING` | 아니오 |
| 원문 `load` | `SOURCE_UNREADABLE` | 예 |
| 원문 `section-missing` | `SECTION_MISSING` | 아니오 |
| 원문 `section-ambiguous` | `SECTION_AMBIGUOUS` | 아니오 |
| 카드 `missing` | `GUIDE_MISSING` | 예 |
| 카드 `load` | `GUIDE_UNREADABLE` | 예 |
| 카드 `too-large` | `GUIDE_TOO_LARGE` | 아니오 |
| 카드 `invalid` | `GUIDE_INVALID` | 아니오 |
| 카드 `changed` | `GUIDE_CHANGED_DURING_READ` | 예 |

원문 `missing`은 끊긴 링크라서 재시도로 풀리지 않는다. 카드 `missing`·`load`는 색인 파일의 `CATALOG_MISSING`·`CATALOG_UNREADABLE`과 같은 이유로 재시도 가능이다. 카드의 `busy`·`denied`와 원문의 `invalid-request`·`index-unavailable`·`unknown-source`·`denied`는 MCP 경로에서 생기지 않는다(입력 스키마·스냅샷·요청마다 새 store). 생기면 기존처럼 고정 문장으로 진단을 남기고 `CATALOG_UNREADABLE`이다. 새 오류 코드의 고정 문장은 한국어 한 문장이며 입력·locator·경로·원시 오류를 담지 않는다. 문구는 5단계가 정하고, 시험은 코드마다 문장이 고정이고 위 내용을 담지 않음을 단정한다.

## 색인 검사

`electron/record-index-check.ts`가 검사 본체(시험 대상)다. `scripts/check-record-index.mjs`가 CLI이고 `npm run records:check`로 실행한다. CLI는 Node 기본 TypeScript 실행으로 앱과 같은 원문 읽기 모듈을 그대로 불러온다. NodeNext 관례의 `.js` import를 `.ts` 원본으로 잇는 작은 resolve hook(`scripts/ts-source-loader.mjs`, `node:module`의 `register`)을 쓴다. 대안은 둘이었다. 별도 tsconfig로 컴파일하면 출력 폴더와 빌드 단계가 늘고, 검사 전용 JS 사본을 두면 앱과 판정이 갈릴 수 있다. 그래서 같은 코드를 쓰는 쪽을 골랐다. CLI가 불러오는 모듈은 지울 수 있는 TypeScript 문법만 쓴다(enum·namespace·생성자 매개변수 속성 없음).

검사 본체의 진입 함수는 `checkRecordIndex({ repositoryRoot }): Promise<RecordIndexCheckResult>`다. 결과는 `{ diagnostics, groups, exitCode }`이고 진단은 `{ severity: 'error' | 'warning', code, location, cause, fix }`, 묶음 상태는 `{ group: 'index' | 'goals' | 'backlog', ran: boolean }`이다. CLI는 진단마다 `<severity> <code> <location> — <cause> — 고치는 방법: <fix>` 한 줄을 내고, 마지막 줄에 `records:check index=<ran|failed> goals=<ran|failed> backlog=<ran|failed> errors=<수> warnings=<수>`를 낸다.

검사 묶음은 셋이고 묶음마다 실행 상태를 따로 낸다. 파일을 읽지 못한 경우(색인 파일 없음·읽기 실패, goals 폴더 목록 실패, BACKLOG 없음·읽기 실패, 출처 파일의 `load`·`changed`)는 그 묶음의 실행 못 함이고 정책 위반 진단으로 바꾸지 않는다. 진단 코드는 아래로 고정한다. `location`은 색인이면 `catalog.json`과 문제 객체의 ID·키(ID가 없으면 배열 위치), goal이면 goal 폴더 경로, 백로그면 `BACKLOG.md:<행>`을 담는다.

- 색인 error: `CATALOG_INVALID`(JSON·스키마·허용 밖 키·중복 ID), `REFERENCE_BROKEN`, `LOCATOR_INVALID`(local 경로 모양·handoff ID 모양), `SOURCE_PATH_REJECTED`(cause에 reason), `SOURCE_NOT_READABLE`(git 출처의 확장자에 `section` 지정, 일반 파일 아님), `SOURCE_MISSING`, `SOURCE_TOO_LARGE`, `SOURCE_INVALID_ENCODING`, `SECTION_MISSING`, `SECTION_AMBIGUOUS`.
- goal warning: `GOAL_NOT_INDEXED`.
- 백로그 warning: `BACKLOG_ID_FORMAT`, `BACKLOG_ID_DUPLICATE`, `BACKLOG_GOAL_LINK_MISSING`, `BACKLOG_PROMOTION_LINK_MISSING`, `BACKLOG_TABLE_FORMAT`, `BACKLOG_GOAL_LINK_REJECTED`(PR3, cause에 reason. 판정 순서는 [백로그 메뉴 설계](backlog-menu-design.md) 「goal 링크 판정 순서」).

| 묶음 | error(끊긴 링크) | warning(파일럿) |
|---|---|---|
| 색인 | 스키마·중복 ID·참조 ID 위반, git 출처의 경로 거절·파일 없음·확장자 밖 `section` 지정, Markdown 구간 없음·모호·크기 초과·인코딩, local locator 모양·handoff ID 모양 위반 | 없음 |
| goal | 없음 | `01_Phases/goals`·`05_Management/goals`에서 `goal.md`가 있는데 어떤 git 출처 locator도 `<goal 폴더>/`로 시작하지 않는 goal |
| 백로그 | 없음 | `00_Document/operations/BACKLOG.md`의 첫 열이 `ID`인 표에서 ID 모양·중복, 상대 링크 중 goal 링크의 대상 파일 없음, 상태가 「goal 승격」인데 goal 링크 없음, 열 수 불일치 |

종료 코드는 묶음 하나라도 실행 못 하면 2, 아니면 error가 있으면 1, 아니면 0이다. 실행 못 함과 정책 위반을 섞지 않는다([하네스 원칙](../../../00_Document/conventions/CODE_CONVENTION.md#하네스-원칙) 1·3·4). local 출처의 파일 존재는 다른 checkout에서 확인할 수 없어 보지 않는다. CI 연결과 error 승격은 이 PR 범위가 아니다(묶음 2, 사용자 판단). BACKLOG 표 해석은 `electron/backlog-table.ts`의 순수 함수로 두고 PR3 메뉴가 같은 함수를 쓴다. PR3가 백로그 판정을 공유 모듈로 옮기고 코드·판정을 넓히는 내용은 [백로그 메뉴 설계](backlog-menu-design.md)를 따른다.

## 데이터 전환

- 기존 출처 35개: `note`·`revision`을 뺀다. `local`의 locator는 `C:/Dev/DawnHolder_Project/` 접두를 뺀 저장소 루트 기준 경로로, `handoff`는 메시지 ID만 남긴다. git `.md`의 `section`은 정확한 제목 문구 하나다. 옛 값이 여러 제목을 이었다면 그 출처를 쓰는 기록·시스템의 목적에 가장 직접 답하는 제목 하나를 고른다. 둘 이상 꼭 필요하면 출처를 나누고 ID 뒤에 영문 slug를 붙인다. `.cs` 두 개와 local·handoff는 `section: null`이다. 옛 구간·기호·anchor가 위치를 알려 주던 정보는 제목에 이미 없을 때만 제목에 넣는다.
- 시스템 18개: 서술·상태 필드를 빼고 ID·제목·분야·연결은 유지한다. 시스템마다 그 시스템을 설명하는 git Markdown 출처를 하나 이상 둔다(FEATURE_MAP·ARCHITECTURE·영역별 계약의 해당 제목).
- 기록 18개: 서술·상태 필드를 빼고 `pullRequests`를 채운다. PR이 없는 결정·계획 기록은 빈 배열이다.
- 새 기록: a47a0276의 두 goals 폴더에서 이름이 `2026-09-30` 이상이고 `goal.md`가 있는 폴더 42개 중 색인에 없는 goal마다 기록 하나와 그 `goal.md` 출처 하나를 더한다. 진행 중인 goal도 넣는다. 서술·상태가 없으므로 낡지 않는다. 기록 ID는 날짜를 뺀 폴더 이름이고, 출처 ID는 `<기록 ID>-goal`이다. 출처 구간은 결과를 담은 제목(예: 「결과와 열린 사항」)이다. `type`은 산출물 성격으로 고른다(코드·문서 변경 `변경`, 결정·설계 `결정`, 검증만 `검증`, 계획만 `계획`). `systemIds`는 goal의 만들 것·건드릴 곳이 그 시스템을 직접 다룰 때만 넣는다.
- `pullRequests`는 `git log --first-parent --merges main -- <goal 폴더>`의 `Merge pull request #N` commit에서 읽는다. 병합 commit이 없는 PR은 지어내지 않고 완료 보고에 적는다.
- 전환표(옛 ID·구간 → 새 ID·구간과 고른 이유, 새 기록의 type·systemIds 근거, PR 출처 명령)를 완료 보고에 남긴다. 독립 검증자가 PR 번호·병합 commit을 Git 원천과 대조한다.

## 시험과 기존 실패

- 시험 파일은 `05_Management/frontend/tests/`와 화면 시험 위치(`src/*.test.tsx`)의 기존 관례를 따른다. Node 환경 시험은 `// @vitest-environment node` 머리를 쓴다.
- 이 PR이 없애거나 바꾸는 계약을 단정하던 기존 시험은 (a) 옛 구현 세부 단정으로 분류하고, 근거 요구(goal 「만들 것」 항목, 이 문서 절)와 함께 고치거나 지운다. 저장·rename 시험(`records-store`의 저장 부분, `mcp-v1-rename`, `mcp-v3-r1-rename`, `mcp-v3-r1/rename-contract.ts`)은 저장 제거에 따라 지운다.
- 이미 알려진 실패 B01·B09·B10·B11·B12와 가끔의 A01은 [기존 실패 분류](../2026-10-02-system-cards/existing-failures.md)에 있다. 이 PR이 같은 시험 파일을 다시 쓰면 그 실패를 포함해 전 실패를 다시 분류한다. 관계없는 실패는 고치지 않는다.
- 실제 색인의 시스템·기록 상세를 모두 여는 화면 시험(`tests/catalog-display-names.test.ts`의 rendered 시험)은 실행 시간이 색인 항목 수에 비례한다. 데이터 전환 뒤 상세가 36개에서 67개로 늘어, 전체 실행에서는 기본 5000ms를 넘었고 단독 실행에서는 3117ms에 통과했다(3단계 보고 E/`pr2-s2/report.md`). 그 시험의 시간 상한은 4단계 시험 작성자가 시험에 명시한다.

## 작업 순서와 소유

같은 파일은 한 사람씩 쓰고, 앞 작업자의 쓰기 종료와 정산 뒤 넘긴다.

| 순서 | 작업자 | 범위 |
|---|---|---|
| 1 | 신규 Opus 시험 작성자 | 기록 읽기 경계의 실패하는 요구 시험: 색인 v2 계약, 읽기 전용 store, 원문 구간 경계와 거절 사례(앱 경계), checkout, IPC·preload, 화면, 색인 검사, 실제 색인 성질. 같은 영역의 옛 시험 정리 |
| 2 | 신규 Sol | 1의 제품 코드(electron·preload·화면·검사 도구·`package.json` script). `catalog.json`은 건드리지 않는다 |
| 3 | 신규 Sol | `catalog.json` 데이터 전환과 새 goal 기록. 색인 검사 error 0 확인 |
| 4 | 신규 Opus 시험 작성자 | MCP v2의 실패하는 요구 시험: DTO, 새 도구 세 개, 거절 사례(MCP), 경계·빌드 시험, fixture v2 |
| 5 | 신규 Sol | MCP 제품 코드와 새 빌드 |
| 6 | 신규 Opus 독립 검증자 | PR 전체 강 등급 검증과 실제 Electron 확인 |

`package.json`은 scripts의 `"records:check"` 한 줄만 바꾼다. 의존성·lockfile 변경은 쓰기 전에 메인에 올린다(메인 `msg_27b8e5c2e6fb`).

2와 3 사이, 4와 5 사이에는 MCP 시험이 실패한 채 남는다. 각 완료 보고는 그 실패를 예정된 실패로 따로 적는다. 리드는 단계마다 README·MCP.md의 사용 흐름과 재빌드 조건을 쓴다.

## 정본에 미칠 영향

CODE_CONVENTION 「TypeScript·Electron 작성」은 `catalog-store.ts`를 관례 출발점으로 링크하고, 저장 경계 항목이 그 파일의 잠금·백업·rename을 예로 든다. PR2 뒤 이 파일에는 저장 경계가 없다. 링크는 살아 있지만 예시가 사라지므로, 병합 뒤 메인을 거쳐 Rules에 문구 갱신을 제안한다.
