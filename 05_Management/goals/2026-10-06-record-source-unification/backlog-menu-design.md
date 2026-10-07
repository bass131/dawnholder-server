# 백로그 메뉴 설계

[goal](goal.md)의 PR3(「만들 것」 6, 완료조건 5와 10의 백로그 부분)를 구현하기 전에 리드가 고정한 경계와 인터페이스다. 선행 시험 작성자, 구현 Sol, 독립 검증자가 같은 기준으로 이 문서를 읽는다. 이 문서와 시험이 다르면 시험을 고치기 전에 리드에게 묻는다.

요구 원천은 셋이다. goal 「만들 것」 6은 BACKLOG 표를 「이후 작업」으로 보이고 ID·goal 링크 어긋남은 warning, 표를 읽지 못하면 형식 오류로 표시하라고 한다. [시스템 카드 goal의 백로그 후속 절](../2026-10-02-system-cards/goal.md#후속-백로그--백로그-정본메뉴와-다음-일-정리)은 고치는 방법을 제시하고 검사 불능과 실제 위반을 구분하라고 한다. [색인 v2 설계](index-v2-design.md) 「색인 검사」는 BACKLOG 표 해석을 `electron/backlog-table.ts`의 순수 함수로 두고 메뉴가 같은 함수를 쓰라고 한다.

## 범위

- 만들 것: 개발 현황의 셋째 탭 「이후 작업」. `00_Document/operations/BACKLOG.md`의 후보를 읽기 전용으로 보이고, 문제를 「형식 오류」·「어긋남」·「확인 불가」로 나눠 고치는 방법과 함께 표시한다.
- 하지 않을 것: BACKLOG 편집·저장, Markdown 렌더링과 링크 이동, MCP의 백로그 도구, BACKLOG 데이터 수정(Rules 소유), 색인 검사의 CI 연결과 error 승격, `source-section-*`·`mcp/`·`mcp-dist` 변경, fence 판정 통합(아래 「fence 판정」).

## 공유 백로그 판정

지금 백로그 행 판정(ID 모양·중복·승격 링크·goal 링크 존재)은 `record-index-check.ts` 안에만 있다. 메뉴가 같은 판정을 다시 구현하면 PR2 V2처럼 정책이 두 곳으로 갈린다. 그래서 판정을 공유 모듈로 옮기고 색인 검사와 메뉴가 같은 결과를 쓴다.

| 파일 | 책임 |
|---|---|
| `electron/backlog-table.ts` | 표 해석(순수). 후보 행과 표 형식 문제 |
| `electron/backlog-contract.ts`(새) | 진단 코드·고정 문장·순수 행 판정, 문제 종류 판정, 결과 형식, bridge 타입 |
| `electron/backlog-store.ts`(새) | `createBacklogStore({ repositoryRoot })`의 `read()`. BACKLOG 읽기, 해석, 판정, goal 링크 존재 확인 |
| `electron/record-index-check.ts` | store 결과를 색인 검사 진단으로 옮긴다 |

이름은 `system-guide-*`·`catalog-*`·`source-section-*`·`checkout-*`의 contract/store 짝 관례를 따른다. 행 판정 함수의 이름과 나눔은 구현자가 정하되 순수 판정은 contract, 파일 I/O는 store에 둔다.

```ts
export type BacklogIssueCode =
  | 'BACKLOG_TABLE_FORMAT' | 'BACKLOG_ID_FORMAT' | 'BACKLOG_ID_DUPLICATE'
  | 'BACKLOG_PROMOTION_LINK_MISSING' | 'BACKLOG_GOAL_LINK_MISSING' | 'BACKLOG_GOAL_LINK_REJECTED';

export interface BacklogIssue {
  code: BacklogIssueCode;
  line: number | null;   // BACKLOG.md의 1부터 세는 행. 파일 전체의 문제는 null
  cause: string;
  fix: string;
}

export type BacklogResult =
  | { ok: true; rows: BacklogRow[]; issues: BacklogIssue[]; uncheckedLinks: { line: number; link: string }[] }
  | { ok: false; code: 'missing' | 'too-large' | 'invalid-encoding' | 'changed' | 'load' | 'denied'; message: string };

export interface BacklogBridge {
  readBacklog(): Promise<BacklogResult>;
}

export function backlogIssueKind(code: BacklogIssueCode): 'format' | 'mismatch';
```

- `backlogIssueKind`는 `BACKLOG_TABLE_FORMAT`만 `format`, 나머지는 `mismatch`다. 화면은 코드 문자열을 직접 비교하지 않고 이 함수를 쓴다.
- `issues` 순서: 표 형식 문제를 파일 순서로 먼저 둔다. 이어서 행마다 파일 순서로 ID 모양 → 중복 → 승격 링크 → 그 행의 goal 링크(셀 순서) 문제를 둔다. 지금 색인 검사의 출력 순서와 같다.
- 기존 다섯 코드의 `cause`·`fix` 문장은 지금 `record-index-check.ts`의 문장 그대로 옮긴다.
- `uncheckedLinks`는 예상 밖 I/O 오류로 존재를 확인하지 못한 goal 링크다. 위반이 아니라 검사 불능이다.

## 두 단계

구조 변경과 동작 변경을 다른 commit으로 남긴다(goal 「설계와 검증 경계」). 한 Sol 작업이 1단계를 끝내면 질문으로 멈추고, 리드가 1단계를 commit한 뒤 2단계를 진행한다.

| 항목 | 1단계(구조, 지금 동작 보존) | 2단계(동작) |
|---|---|---|
| 모듈 | 백로그 블록을 contract·store로 옮기고 색인 검사가 store를 부른다. 결과 형식은 위 최종 형식이다 | 같은 모듈에서 아래 동작을 바꾼다 |
| BACKLOG 읽기 | 지금처럼 `readFile` UTF-8. `ENOENT`는 `missing`, 그 밖은 `load` | 원문 읽기 경계 `createSourceSectionStore({ repositoryRoot }).read`에 store 안의 고정 git 출처(`00_Document/operations/BACKLOG.md`, `section: null`)를 넘긴다. 결과 대응은 아래 표 |
| goal 링크 판정 | 지금 그대로. BACKLOG 폴더 기준으로 풀고, 루트 밖·해독 실패·없음·파일 아님은 `BACKLOG_GOAL_LINK_MISSING` | 아래 「goal 링크 판정 순서」 |
| 예상 밖 링크 오류 | 그 링크를 `uncheckedLinks`에 넣고 그 자리에서 판정을 멈춘다(지금 예외가 나머지 행을 건너뛰는 동작과 같다) | 그 링크만 `uncheckedLinks`에 넣고 다음 링크와 행을 계속 판정한다 |
| ID 표 없음 | 진단 없음 | 첫 열이 `ID`인 후보 표가 하나도 없으면 `BACKLOG_TABLE_FORMAT`, `line: null` |
| fence 규칙 | 지금 그대로 | 원문 구간 판정과 같은 규칙. 백틱 fence의 info string에 백틱이 있으면 fence를 열지 않는다 |
| 행 묶음 | 없음 | `BacklogRow`에 `group: string \| null`을 더한다. 그 행 앞의 가장 가까운 fence 밖 `## ` 제목 문구다 |
| 색인 검사 대응 | 아래 「색인 검사」 | 같음. 새 코드 `BACKLOG_GOAL_LINK_REJECTED`와 `line: null`의 위치를 더한다 |
| 화면·IPC | 없음 | 아래 「Electron 경계」·「화면」 |

`backlogIssueKind`는 옮기는 코드가 아니라 새 함수라 2단계에 더한다. 1단계는 성공 경로의 진단 코드·문장·순서·위치와 종료 코드, 실패 경로의 묶음 실행 상태와 이미 낸 warning까지 지금과 같아야 한다. 기존 `record-index-check.test.ts`의 백로그 시험이 그대로 통과하는 것이 최소 기준이다.

2단계의 BACKLOG 읽기 결과 대응과 고정 문장은 다음과 같다. 원문 읽기 경계를 쓰므로 링크·junction·대소문자·크기 상한(파일 1 MiB, `section: null`이면 내용 전체가 구간 상한 256 KiB 안)·엄격 UTF-8·읽는 동안 바뀜 판정이 앱의 원문 읽기와 같아진다.

| 원문 읽기 결과 | `code` | `message` |
|---|---|---|
| `missing` | `missing` | BACKLOG.md를 찾지 못했습니다. |
| `too-large` | `too-large` | BACKLOG.md가 읽기 크기 상한을 넘습니다. |
| `invalid-encoding` | `invalid-encoding` | BACKLOG.md가 UTF-8 문서가 아닙니다. |
| `changed` | `changed` | 읽는 동안 BACKLOG.md가 바뀌었습니다. 다시 읽으세요. |
| `path-rejected`·`not-readable`·`load`, 그 밖 | `load` | BACKLOG.md를 읽지 못했습니다. 다시 읽으세요. |
| (IPC sender 거절) | `denied` | 이 창에는 기록 접근 권한이 없습니다. |

1단계의 `missing`·`load` 문장도 위 표와 같다. `denied`는 store가 아니라 `main.ts`의 기존 `denied` 상수가 낸다.

### goal 링크 판정 순서

2단계에서 goal 링크마다 아래 순서로 판정한다. 앞 단계에서 결론이 나면 멈춘다.

1. 대상: 셀의 상대 링크 중 `01_Phases/goals/` 또는 `05_Management/goals/`를 지나는 것(지금 `backlogGoalLinks` 그대로). `#` 뒤는 뺀다.
2. 퍼센트 해독. 실패하거나, 해독한 경로에 `\`가 있으면 `BACKLOG_GOAL_LINK_REJECTED`, reason `invalid`. `\`를 경로 구분자로 해석하는 운영체제와 아닌 운영체제의 판정이 갈리지 않게 풀기 전에 거절한다(선행 시험 보고의 질문 3).
3. `00_Document/operations` 기준으로 풀어 저장소 루트 상대 POSIX 경로로 바꾼다. 루트 밖이면 `BACKLOG_GOAL_LINK_REJECTED`, reason `parent`.
4. `inspectSourceFile({ repositoryRoot, locator })`의 결과로 판정한다.
   - 성공: 문제 없음.
   - `path-rejected`(reason `drive`·`absolute`·`parent`·`invalid`·`link`·`case`): `BACKLOG_GOAL_LINK_REJECTED`, cause에 reason.
   - `missing`, `not-readable`(`not-file`): `BACKLOG_GOAL_LINK_MISSING`.
   - `load`: `uncheckedLinks`.

대소문자가 다른 링크는 대소문자를 구분하지 않는 파일 시스템(Windows)에서 `REJECTED`(`case`), 구분하는 파일 시스템에서 `MISSING`이 된다. 원문 구간 읽기와 같은 성질이며 둘 다 warning이다.

새 고정 문장은 다음과 같다.

| 코드 | cause | fix |
|---|---|---|
| `BACKLOG_GOAL_LINK_REJECTED` | 상대 goal 링크가 저장소 경로 규칙에 맞지 않습니다 (<reason>). | BACKLOG.md 기준 상대경로로 저장소 안의 goal 파일을 대소문자까지 같게 가리키고 링크·junction을 거치지 마세요. |
| `BACKLOG_TABLE_FORMAT`(ID 표 없음) | 첫 열이 ID인 후보 표가 없습니다. | 머리글 첫 열이 ID인 후보 표와 구분 행을 두세요. |

### fence 판정

`backlog-table.ts`와 `source-section-contract.ts`가 fence를 따로 판정한다(PR2 검증 관찰 O2). 백틱 fence의 info string 규칙이 서로 달라, 같은 BACKLOG를 메뉴와 원문 읽기가 다르게 볼 수 있다. 하나로 합치려면 MCP 공유 모듈 `source-section-contract.ts`를 바꿔야 한다. 그러면 canonical `mcp-dist`(사용자 산출물) 재빌드와 MCP closure 시험 변경이 따른다. 이 PR은 2단계에서 `backlog-table.ts`의 규칙만 원문 판정과 같게 맞추고, 두 곳의 통합은 goal 「후속 후보」로 남긴다.

## 색인 검사

두 단계 모두 같은 대응이다.

- `read()`가 `ok: false`면 백로그 묶음을 실행 못 함으로 둔다(종료 코드 2). 진단으로 바꾸지 않는다.
- `ok: true`면 `issues`를 순서대로 severity `warning`, code 그대로, location `BACKLOG.md:<line>`(`line`이 null이면 `BACKLOG.md`)로 옮긴다.
- `uncheckedLinks`가 하나라도 있으면 `issues`를 옮긴 뒤 백로그 묶음을 실행 못 함으로 둔다.
- store의 예상 밖 예외는 지금처럼 백로그 묶음의 실행 못 함이다.

`RecordIndexDiagnosticCode`는 `BacklogIssueCode`를 포함한다. CLI 출력 형식은 바꾸지 않는다.

## Electron 경계

- `main.ts`: `createBacklogStore({ repositoryRoot })`를 기존 `repositoryRoot` 상수로 만든다. `ipcMain.handle('system-backlog:read', event => trustedSender(event) ? backlogStore.read() : denied)`를 더한다. 쓰기 채널은 없다.
- `preload.cts`: `contextBridge.exposeInMainWorld('systemBacklog', { readBacklog: () => ipcRenderer.invoke('system-backlog:read') })`. 인자를 넘기지 않는다.
- `src/recordsBridge.d.ts`: `Window`에 `systemBacklog?: BacklogBridge`를 더한다. `BacklogBridge`는 `GuideBridge`처럼 contract에 둔다.
- renderer는 경로나 입력을 넘기지 않는다. BACKLOG 경로는 store 안의 고정값이다.

## 화면

- `App.tsx`의 개발 현황 탭에 셋째 버튼 「이후 작업」을 더한다. `developmentView`는 `'cards' | 'records' | 'backlog'`이고 기존 탭처럼 `hidden`으로 전환한다.
- 본체는 `src/DevelopmentBacklog.tsx`다(`DevelopmentRecords.tsx`와 나란한 이름). 커지면 `src/developmentBacklog/`에 나눈다.
- 읽기: mount 때 한 번 읽고 「다시 읽기」 버튼으로 다시 읽는다. 진행 중이면 겹쳐 부르지 않는다. 늦게 도착한 이전 응답은 버린다. `DevelopmentRecords.tsx`의 양식을 따른다.
- 상태 문장
  - bridge 없음: 백로그 연결을 사용할 수 없습니다. 데스크톱 앱에서 실행하세요.
  - invoke 거부: 백로그 연결에서 응답을 받지 못했습니다.
  - `ok: false`: 결과의 `message`.
  - 이전에 읽은 결과가 있는데 다시 읽기가 실패하면 이전 후보를 그대로 보이고 「갱신 실패: 이전에 읽은 백로그를 표시합니다.」를 덧붙인다.
- 요약 한 줄: `후보 N개 · 형식 오류 a · 어긋남 b · 확인 불가 c`. 세 수는 `backlogIssueKind`와 `uncheckedLinks`로 센다.
- 후보: `group` 제목별로 파일 순서대로 묶는다. `group`이 null인 후보(첫 `## ` 제목 앞의 표)는 제목 없이 맨 앞에 보인다. 후보마다 ID, 제목, 상태, 담당 후보, 이유, 출처, 선행 조건, 위치 `BACKLOG.md:<행>`을 평문으로 보인다.
- 문제 표시: 그 행의 문제를 종류 이름(「형식 오류」 또는 「어긋남」)과 cause, 「고치는 방법: <fix>」로 보인다. 확인하지 못한 링크는 「확인 불가」와 링크, 「다시 읽기로 다시 확인하세요.」로 보인다. 행에 붙지 않는 문제(`line`이 null이거나 후보 행이 아닌 행)는 후보 목록 위 「표 형식 오류」 영역에 보인다.
- 읽기 전용: 조작 요소는 탭과 「다시 읽기」뿐이다. 입력·선택·contenteditable·링크(`a`) 요소를 두지 않는다. 셀의 Markdown은 렌더링하지 않고 React 텍스트로 그대로 보인다.
- 앱 zoom 125%의 보조 화면(1920×1080)에서 메뉴에 가로 스크롤이 생기지 않아야 한다. 스타일은 `styles.css`의 기존 관례를 따른다.

## 시험

- 위치·머리는 [색인 v2 설계](index-v2-design.md) 「시험과 기존 실패」 관례를 따른다.
- 1단계 보존: 기존 `record-index-check.test.ts`의 백로그 시험은 고치지 않고 통과해야 한다. store 직접 시험은 1단계에서 지금 동작(다섯 코드의 순서·위치·문장, `missing`·`load`)을 단정하고 1단계 뒤 통과한다.
- 2단계 요구: goal 링크 판정 순서의 각 갈래, 읽기 결과 대응 표의 각 행, ID 표 없음, fence의 백틱 info string, `group`, 확인 불가 뒤 계속 판정, 색인 검사 대응(새 코드, null 위치, 실패 코드의 묶음 상태), IPC 채널 등록과 sender 거절, preload 노출, 화면의 요약·묶음·종류 이름·고치는 방법·상태 문장·이전 결과 유지·읽기 전용, `App`의 셋째 탭.
- 결정적으로 재현할 수 없는 경우(링크 확인 중 예상 밖 I/O 오류, 읽는 동안 바뀜)는 제품 hook을 만들지 않는다. 쓰지 못한 시험은 이유와 함께 보고한다. 대소문자 시험은 대소문자를 구분하지 않는 파일 시스템에서만 `case`를 단정하고, 아닌 경우의 기대값을 따로 둔다.
- 실제 자료: 실제 BACKLOG.md를 store로 읽어 성공·형식 오류 0·모든 후보에 `group`이 있음을 단정한다. 행 수처럼 Rules가 바꾸는 값은 고정하지 않는다. 새 판정이 실제 BACKLOG에서 어긋남을 내면 BACKLOG를 고치지 않고 보고한다.
- 채널 수를 고정한 기존 `records-ipc-preload.test.ts` 시험처럼 이 PR이 바꾸는 계약을 단정하던 시험은 옛 구현 세부 단정으로 분류하고 이 문서 절을 근거로 고친다.
- 기대값은 고정 입력과 손으로 정한 값으로 둔다. 제품 해석·판정을 시험 안에서 다시 계산하지 않는다.

## 작업 순서와 소유

| 순서 | 작업자 | 쓰는 곳 | 끝 |
|---|---|---|---|
| 1 선행 시험 | 신규 `claude-opus-5-5` | `tests/`, `src/*.test.tsx` | 리드가 시험만 commit |
| 2 구현 1단계(구조) | 신규 `gpt-6.1-sol` max | `electron/backlog-contract.ts`·`backlog-store.ts`(새), `record-index-check.ts` | 질문으로 멈춤 → 리드 commit |
| 2 구현 2단계(동작) | 같은 Sol | 위 파일과 `backlog-table.ts`·`main.ts`·`preload.cts`, `src/DevelopmentBacklog.tsx`·`recordsBridge.d.ts`·`App.tsx`·`styles.css` | 리드 commit |
| 3 독립 검증(강) | 다른 신규 `claude-opus-5-5` | 판정에 필요한 시험 파일만 | 판정 원문 |

- 독립 검증은 실제 Electron에서 메뉴를 확인한다. 소유 TEMP 사본·자기 프로필·자기 프로세스만 쓰고, 사본의 BACKLOG를 바꿔 형식 오류·어긋남과 파일 없음 상태를 본다. 보조 화면 identity·bounds·scaleFactor와 앱 zoom 125%를 실행 때 관측한다. OS 합성 입력·전면화는 쓰지 않는다. 실행하지 못한 부분은 통과로 적지 않는다.
- MCP는 백로그 모듈에 닿지 않는다. 독립 검증은 MCP closure 시험과 canonical `mcp-dist` digest 시험이 이 PR 뒤에도 그대로 통과하는지 본다.
- 실제 줄 수는 단계마다 `git diff --numstat`로 남긴다.

## 정본에 미칠 영향

- `05_Management/README.md`의 화면 안내에 「이후 작업」 탭 한 줄을 리드가 구현 뒤 더한다.
- BACKLOG·AGENTS·CODE_CONVENTION은 바꾸지 않는다. fence 통합과 PR2 관찰 O1·O3·O4는 goal 종료 때 메인을 거쳐 Rules에 BACKLOG 후보로 보낸다.
