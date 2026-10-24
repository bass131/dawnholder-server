# 운영 사례의 계약·정산 경계 정본화

## 재개 지점

다음 첫 일은 [PR263 - 운영 계약·원시 보존·실행 정산 규칙 정본화](https://github.com/bass131/dawnholder-server/pull/263)의 최종 head·독립 판정 원문을 메인이 R-2로 대조하고 사용자에게 정확 head 병합 승인을 받는 것이다. 일곱 문서의 신규 Astra 재실사는 통과했다. 이 단계 결과 기록의 독립 대조와 최종 head·CI 관측은 E/`admin-review/` 및 최종 인계 근거에 연결한다. 현재 PR은 미병합이며 Codex는 병합하지 않는다. 실제 병합 뒤 Gardener·종료 기록 PR·종료 점검·최신 main 정리로 이어간다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`
- branch: `docs/ops-candidate-canon-20261025`, base `cf207c1f355ced664e62119152e8b0686b0ec051`. fetch/clean/분기 원시는 E/`git-fetch-start.txt`·`branch-start-state.json`이다.
- 근거 E: 이 checkout의 Git 제외 `.backups/verification/2026-10-25-ops-candidate-canon/`. `lead-context.md`는 초안 전, `lead-approved-context-v1.md`는 승인 뒤 대상 쓰기 전 맥락이다.
- 현재 Rules Run `run_543efc26689e`, 회신 `run:run_543efc26689e`. handle/Task/Dispatch는 기동 때 현재 receipt로 확인하며 이 기록만으로 실행 권한을 인정하지 않는다.
- 승인 전 초안 `scope-draft-v1.md` 18250B/SHA256 `EF0871EFE2389465ACE8B98B06C7E34F74833042BC1E5834F9699CBFFD0061EC`는 보존한다. 초안 작성 당시에는 독립 PASS·commit/push·PR·병합이 없었다.

## 진척 단계

- [x] 범위·원문 대조
- [x] 목표·브랜치 등록
- [x] 정본 문서 작성
- [x] 메인 문구 반영
- [x] 독립 문서 실사
- [>] PR263 병합
- [ ] 종료 점검
- [ ] 종료 PR 병합
- [ ] 최신 기준 복귀

## 요구사항 원천과 적용 결정

1. 메인 진입 `msg_e26051b54fb1`(2026-10-24T21:26:49Z)은 10-24 운영 후보 여덟 건을 묶은 범위 초안만 요청했다. 초안 제출 `msg_f7339b83d80c` 전후 정본·스킬 쓰기는 없었다.
2. 메인 `msg_e482f53c0545`(22:25:29Z)가 전달한 사용자 원문은 「대시보드 결정 응답: 1) 계획 검토 - Rules 운영 사례 여덟 건 정본화 범위 초안 v1 → A 승인 (초안 msg_f7339b83d80c)」다. A의 효과는 「메인이 이 답을 그대로 파트에 전달하고, 파트는 이 초안대로 goal을 시작한다.」다. 이 pane 직접 사용자 입력으로 격상하지 않는다.
3. 승인 권고는 1의 현행 계약 범위 명시, 2·4·5·6 포함, 3·7 최소 보완, 8 정책 변경 제외다. 작은 읽기 일반 면제와 선택창 Esc 권한 허용은 선택되지 않았다.
4. 후보4의 최초 원문 대조로 대상은 **메인과 각 worktree의 `index.lock`**으로 맞춘다. 삭제 주체는 메인만이다. 초안의 메인 checkout 한정은 최초 원문보다 좁았으며 이 대조가 승인된 착수 관문이다. mtime 문구는 아래의 보수적 메인 적용으로 구분한다.
5. 같은 승인 전달은 새 사용자 잡일 보조세션 규칙을 goal에 기록하고 메인이 `CLAUDE.md` 한 줄을 쓰도록 지정했다. PR1 대상에 이 파일 하나를 추가하고 별도 commit·독립 실사에 포함한다. 나머지 정본/코드/설정 경계는 그대로다. 이 착수 대조 결과를 메인에게 보고하며 별도 사용자 선택을 추정하지 않는다.

## 범위

### 만들 것

| 번호 - 후보 | 이번 결과와 보존 경계 |
|---|---|
| 1 - 읽기 명령 실행 관문 | 단순 파일 읽기 기본 허용과 계약별 런타임/자식/하네스 실행을 구분한다. 계약이 모든 Node/PS·CLI 대역을 대상으로 정했으면 JSON 읽기 `node -e`도 해당한다. 작은 읽기 전면 면제·전 파트 공통 수치 신설은 없다. |
| 2 - 원시와 쓰기 범위 | 성공 parse/AST 출력·실패 시도·명령/exit도 보존한다. 자기 근거 폴더는 임의 삭제 허가가 아니다. 새 시도는 새 경로로 연결하며 삭제된 원호출을 재실행으로 소급 복구했다고 하지 않는다. 분석 스크립트/원시/receipt/보고 및 provider 자동 TEMP와 수동 TEMP의 허용 범위를 계약에서 구분한다. |
| 3 - 생존 신호 초과 | 현행 첫 신호·5분·helper·300초 초과 한 줄·R-2를 유지한다. 선택창/권한 차단은 관측한 사유로 남기고 실제 초과 간격을 지우거나 추정시간을 빼지 않는다. Return 지연 처분과 신호 처분은 별개다. 공식 질문 대기는 현행 helper 기준으로 분리한다. |
| 4 - 낡은 Git 잠금 | 메인이 정확한 `index.lock` 한 파일씩, 크기0·mtime 기준10분 **초과**·그 순간 git.exe0을 직전 확인한 경우만 삭제/기록한다. 메인·각 worktree 대상, 리드/작업자/검증자는 삭제하지 않고 메인에 보낸다. 미충족/미확인 조건은 묻는다. 원인은 미확정이다. |
| 5 - 시험 사본 Library | 사용자 A 전문의 시점·대상·보존·직전확인·기록을 정본에 연결한다. 반환/정산과 실패분류, 현재/다른 goal 참조, 다른 파트 실행 창, 재시도 금지·매번 재허가 없음·종료 전 잔여 확인을 함께 유지한다. |
| 6 - 표본 cap 한계 | 표본 기반 cap은 검출/정지 기준이며 연속 순간 메모리의 하드 상한 보장이 아님을 설명한다. 계약별 표본 주기·대상process·측정종류·검출/정지/질문을 원시에 연결한다. 표본최대·종료peak·미측정을 구분하고 H1 한건 처분을 일반 비차단 예외로 확대하지 않는다. |
| 7 - 선택창 거절 | R-6 메인 처리 주체 유지. 메인 입력도 권한 분류기에 거절되면 우회/대리입력/설정변경 없이 상태·소유·미완료를 보존해 사용자 처리 필요를 보고한다. 선택창 부재와 해제 주체/설정불변 확인을 구분한다. |
| 8 - 일반 reply 자동 Re | 정책 변경 제외. 실제 Art 일반 lead question/status thread 회신에는 공식 blocking ask/worker question 예외 근거가 없었다. 일반 회신은 기존 자기태그 send+thread를 유지하며 이 원천 판단만 결과로 기록한다. |

### 건드릴 곳

| 파일 | 변경/소유 |
|---|---|
| `00_Document/operations/ORCA.md` | 신규 외부 Sol: 공유 자원·잠금/Library·생존 신호·R-6의 해당 절 최소 보완. R-3 예외 확대 없음 |
| `.agents/skills/dawnholder-task-context/SKILL.md` | 같은 Sol: 원시/쓰기 보존·계약 실행 범위·표본 cap 설명, 기존 권한/통과 차단 유지 |
| `.agents/skills/dawnholder-task-context/references/templates.md` | 같은 Sol: 분석/raw/receipt/provider TEMP와 관문 대상·측정종류 기록 칸 최소 보완 |
| `CLAUDE.md` | 메인만 잡일 규칙 한줄 작성. 종료 뒤 리드는 별도 commit으로 수록하고 독립 실사 대상에 포함 |
| 이 `goal.md`, `CURRENT.md` Rules 자기 행, `BACKLOG.md` 관련 원천/보류 연결 | Rules 리드. 후보는 기존 ID와 대조하고 채택/구현으로 표시하지 않음 |
| E의 역할별 폴더 | 자기 맥락·계약·pin·원시/receipt·보고/판정/문서사용사례. 기존 원시 삭제/덮어쓰기 없음 |

같은 파일 동시쓰기를 금지한다. AGENTS·goal-loop·orca-work·session-handoff는 기존 진입 링크로 연결해 변경하지 않는다. 메인/다른 파트 checkout 파일과 기존 봉인 판정은 읽기만 한다.

### 하지 않을 것

- 제품·도구·hook·runner·helper·CI·권한/전역/프로젝트 설정 구현/변경, 자동 heartbeat/삭제·하드 메모리 제한.
- 실제 lock/Library/원시 삭제, Unity/WSL/DB/빌드·실제 게임·메인 checkout Git. 새 권한을 이유로 감시 PID/pattern/허용 목록을 넓히지 않는다.
- 작은 읽기의 전면 면제, 모든 shell 호출의 공통 관문/수치 신설, cap초과 일괄 면제, 과거 위반 소급 면제, R-3 일반 reply 예외 확대.
- .agents 전반 하위분할·ORCA 전면 재구성·규칙 가지치기·ops-v1.0 판 올림·모델/검증강도/독립세션/병합 권한 변경.
- 작업자 추가 위임·commit/push, 리드의 정본 세파일/CLAUDE 직접 작성, Codex 병합/자동병합 예약, 다음 goal 자동 착수.

### 관찰 가능한 완료조건

1. 위 승인 처분과 실제 diff가 일치한다. 사용자 원문·메인 판단·관측·제안을 구분하고 잠금 대상/mtime·Library 보존/시점 조건이 누락되지 않는다.
2. 신규 Sol이 정본 세파일만 쓰고 메인 CLAUDE 쓰기 종료를 확인한다. 신규 Astra xhigh가 보고·실제 diff·규칙 원문·사전메모·참조/권한·30초 탐색을 독립 실사한다. 결함은 번호로 반환하고 수정/재검증은 새 세션이다.
3. 검증자가 자기 메모→원문계약→판정 양식을 실제 사용하여 문서 사례를 남긴다: 파일읽기/계약Node 실행 구분, 성공AST 삭제/재실행, 수동/provider TEMP, 선택창 중 초과/Return, 조건미확인 잠금과 참조중사본 정리보류, 표본cap/종료peak·H1처분, 거절된 Esc와 일반 Re 회신. 사례 판정과 실제 런타임 실행을 구분한다.
4. 메인이 판정 원문/실제 준수·실패분류/측정 출처 표본을 R-2로 확인할 수 있다. 문서 PASS를 자동 준수·실제 삭제/메모리 강제·게임/DB/CI 통과로 확대하지 않는다.
5. 최신 main 문서 PR의 정확 head40자·독립 원문·리스크·수행/미실행을 보고한다. 각 PR은 사용자 명시 승인 뒤 메인만 병합한다. 전체 goal은 기존 Gardener·종료기록 PR·종료 점검·clean 최신main/R-8까지다.

## PR 경계와 검증

- **PR1 - 운영 사례의 계약·정산 경계:** 정본 세파일 + 메인 CLAUDE 별도 commit + goal/CURRENT/BACKLOG. 범위/원문대조→최신main/clean→goal→외부작성→두작성자종료→신규독립문서실사→리드commit/push/PR→정확head 보고 순서다. 메인 CLAUDE 추가를 착수 체크포인트로 보고한다.
- **PR2 - 종료 기록:** PR1 병합/결과 뒤 기존 Opus Gardener 1회, 결과/승인된 후보 연결만. 새 정책/도구는 넣지 않는다. 종료 고정 문서 실사·PR별 승인·R-8을 따른다.
- 문서 전용이므로 코드 TDD·강/약 제품 실행은 비해당이다. 구현자 배정은 고정 명세의 문서라 라우팅 신호1~5 비해당인 신규 Sol xhigh, 독립은 현행 시범의 신규 Astra xhigh다. R-7 시범 목표로 지정되지 않았고 실행 도구 설계 변경도 없어 비해당이다.
- 위임 전에 실제 worker cwd에서 입력 존재/hash·신규출력 부모/구분을 기계 확인하고 CODE_CONVENTION 관련 절 제목·본문 원문 전체와 제외 이유를 계약에 붙인다. 기존 live 계약/봉인 입력은 바꾸지 않고 새 계약부터 적용한다.
- 문서 수준으로 끝내는 이유: 이번 범위는 처리 주체·출처·허용 범위를 고정하는 계약 작업이다. 자동강제/도구/권한 허용은 설계·실증·별도 범위가 필요한 다음 층이다. 운영 지침이 기술적 강제를 보장한다고 적지 않는다.

## 적용 중인 사용자 결정

### 잠금 조건부 복구

메인 msg_e482f53c0545가 대조한 원문(2026-10-21 22:3x KST)은 「대시보드 결정 응답: 1) 낡은 Git 잠금 파일이 또 생겼을 때 처리 방식 → A 조건이 맞으면 메인이 지우고 알림」이다. A의 효과는 「지금 두 개를 지운다. 앞으로도 크기 0이고 10분 넘게 남았고 Git 프로세스가 0이면 메인이 바로 지우고 보고에 적는다. 그 밖의 경우는 묻는다.」다. 방법 원문은 「메인이 지우는 조건은 셋이다: 크기 0, 생긴 지 10분 넘음, 그 순간 git.exe 0. 하나라도 아니면 지우지 않고 묻는다.」 및 「지울 때는 그 파일 하나만 지운다. 지운 시각·경로·mtime을 메인 기록과 다음 보고에 적는다.」다.

리드가 읽기 대조한 원천은 저장소 밖 `C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-21/scripts/board-stale-locks-2235.cjs`의 `stale-index-locks`와 `main-resume-20261021-1636.md` 160~164행이다. 첫 적용 13:35:27Z는 메인/Art의 두 잠금이다. 원인 추정은 확정하지 않고 원천 스크립트를 실행하지 않았다.

**보수적 메인 적용:** 사용자 인용을 바꾸지 않고 정본 운영 문장은 마지막 쓰기 시각(mtime)부터 10분 초과로 고정한다. 메인은 이전부터 mtime으로 쟀으며 10-24 22:16Z 감시 보조에서 이름 재사용 시 이전 생성시각 승계를 관측했다고 보고했다(msg_e482f53c0545). 이는 메인 관측 보고이고 Rules의 NTFS 독립 실증이 아니다. 생성시각만으로 나이를 부풀리지 않는 적용이며 실측/조건 불명확은 삭제 허가가 아니다.

### 실행 정산별 시험 사본 Library

Main msg_e7752a20e30b(10-24 18:25:03Z)가 전달한 사용자 원문은 「대시보드 결정 응답: 1) 시험용 Unity 복사본 정리 규칙 - 실행 정산 때마다 지울지, goal 종료 때 한 번에 지울지 → A 실행 정산 때마다」다. 전문은 Art 현재 goal `2026-10-28-level-redesign-art`의 「사용자 A — 실행 정산별 Unity 복사본 Library 정리」와 E/`source-pins.json` 연결 원문에서 확인했다. 정본 반영 전에도 적용되는 결정이며 이번에 새 삭제 권한을 만들지 않는다.

- 반환·해당 작업자/검증자 정산 뒤 그 실행이 새로 만든 시험 사본 Library만, 실패/unknown은 실패분류 뒤 정리한다. goal 종료 전 잔여도 확인한다.
- 허용된 `.backups/tmp`/짧은 경로 사본을 포함하되 Assets·Packages·ProjectSettings·보고/원시/PNG/JSON·verification·현재/다른 실행·goal 참조 사본은 보존한다.
- 직전 사용 프로세스/다른 파트 실행 창을 확인한다. 실패는 재시도하지 않고 잔여 보고, 기록은 경로·크기·전후 C여유, 매번 메인 재허가 없음/R-2 대조다.

### 잡일의 태스크별 보조세션

메인이 같은 msg_e482f53c0545로 전달한 사용자 원문(10-24 21:43:40Z 무렵 작업 중 대기열 제출)은 「그리고 추가 룰인데, 목업 생성 같은 잡일은 보조세션을 몇개든 상관없으니까 각 태스크 별로 추가 고용해서 하는거로 진행하는거로 앞으로 하자」다. 메인 첫 적용은 잠금 감시 보조 진입계약 msg_7088002b48d3다. 메인 운영 규칙의 정본 위치는 메인 소유 CLAUDE.md다. 이번 정본 세파일로 복제하지 않으며 일반 작업자의 추가 위임/새 파트 권한으로 넓히지 않는다.

## 결정과 단계 결과

- **착수 대조:** 승인 초안의 잠금 원문 관문을 통과했고 대상/mtime 판단과 메인 CLAUDE 한파일 추가를 위에 고정했다. E/`check-followup-20261025-01.json`의 두 메시지를 모두 읽었다. 두 번째 msg_baa017f04b28의 감시 PID22928은 메인 소유 정보 공지이며 새 권한이 아니다. 허용pattern/자원관문을 바꾸지 않는다.
- **branch 준비:** rules-active에서 fetch·clean 확인 뒤 최신 origin/main cf207c1f 기반 신규 branch를 만들었다. 메인 checkout Git은 부르지 않았다. 새 실행/정리0이며 외부 작성/검증은 아직이다.
- **메인 쓰기 종료:** msg_1d559a53b81e(22:28:49Z)의 CLAUDE 한 줄 추가/종료를 현재 Main from으로 대조했다. 실제 9080B/SHA256 `EDF0BE7C1364C19FC207C307B843F8B8F67C71E031C0CAAA14BB723981686854`, 1삽입 diff가 일치했다(E/`main-claude-one-line.patch`). 파일을 고치지 않았으며 별도 commit/독립 실사는 아직이다.
- **외부 작성 시작:** 신규 Sol xhigh의 최초 command/화면·같은 cwd/branch·신규 pane을 대조했다. Task `task_72f61be3f906`, Dispatch `ctx_eda550eb1f78`, handle `term_c9ac6b4c-3c6e-45ad-b459-96850a22bc8e`, incarnation `6b965d8c-acf7-4c6c-927f-40e7c7eef290`, backend unknown이다. 계약 E/`writer/contract-v1.md`, 입력15건/출력부모 존재는 `input-pins-v1.json`·`path-preflight-v1.json`이다. 명세 고정 문서 작업이라 라우팅 신호 비해당이다.
- **주입 복구와 한계:** 공식 receipt는 input_accepted/turn_start_unobserved였다. 동일 runtime/target/incarnation·새 pane의 다른 입력 없음·draft 추정14503자=계약9714+관측머리말4787+2를 R-5와 대조해 텍스트 없는 Enter를 한 번만 보냈다. 뒤 draft 소멸/Working/실제 도구 호출을 관측했다. 첫 precheck는 줄바꿈된 placeholder를 놓쳐 v2로 교정하고 v1을 보존했다. 사후 sourceExact transcript의 user 입력은 공식 Dispatch 머리말로 식별됐지만 `transcript_payload`로1214자만 남아 **계약 전문 일치는 미확인**이다. 메인에 msg_c2c18082e1cb로 즉시 공개했으며 이를 전문 일치/독립 PASS로 쓰지 않는다. 원시 E/`writer/{worker-start-v1.json,draft-precheck-v1.json,draft-precheck-v2.json,enter-only-once-v1.json,post-enter-screen-v1.json,contract-submission-transcript-v1.json}`.
- **작성 완료 인수:** 현재 from/Task/Dispatch·명시 succeeded인 worker_done `msg_e98ae397c9bb`(22:38:32Z)와 worker-show completed/succeeded/exactWorker를 대조했다. report/context 전문·실제 세파일 +44/-2와 기록 명단18파일 bytes/SHA 차이0을 확인했다(E/`writer/{report.md,raw/final-write-audit-01.json,lead-actual-audit-v1.json}`). manifest 자기파일을 더한 전체 자기쓰기19개를 구분한다. 이는 리드 인수/자체점검 실사이며 독립 PASS가 아니다.
- **작성자 신호/종료:** 기존 helper는 서버 신호5개에서 최대176초·300초초과0·공식질문대기0, within/exit0을 반환했다. 첫신호전/완료뒤는 미측정이다(E/`writer/{liveness-mailbox-all-v1.json,liveness-result-v1.json,liveness-command-v1.json}`). release retained/external_terminal 뒤 동일 incarnation·idle/빈draft를 대조해 정확 pane만 close, ptyKilled=true·부모리드1개를 확인했다(`release-v1.json`·`close-v1.json`·`after-close-terminals-v1.json`). 완료 delivery도 전체처리/ack했고 작성자는 재사용하지 않는다.
- **독립 입력 고정:** PR1 대상 일곱 문서와 요구/원시/자기쓰기·메인 CLAUDE를 신규 Astra의 실제 diff/문서사용사례 실사에 고정한다. 이 문서 고정 뒤 독립 판정/PR번호/정확head/CI/병합 관측은 E와 메인 보고에 남기며 판정 입력을 조용히 바꾸지 않는다. 결함 수정이 필요하면 새 작성/재검증과 새입력을 명시한다.
- **첫 독립 판정 차단:** E/`review/verdict.md` 21853B/SHA256 `61D60D94965F53C7EE2B22B0F8ED1A53735B8203B52A50CC8AB0E181C20F5A9B` 전문·자기 맥락·사용사례7개를 직접 읽었다. 실제 done `msg_6b8407c43fa5`(22:52:19Z)/Task `task_0a20d7bb476c`/Dispatch `ctx_dc8b7d94812f`·from·명시failed와 worker-show failed/exactWorker를 대조했다. #1 중간/차단은 리드 계약의 CODE 사람 탐색 절 원문 누락, #2 낮음/차단은 리드 goal의 후보 인용 오류2건이다. 정본 세파일·Main CLAUDE에는 지적이 없었다. O1(H1 원천 탐색)은 비차단 관찰로 남기며 의존 수정 범위를 늘리지 않는다.
- **첫 검증자 정산:** 판정 원시 명단14파일의 bytes/SHA 차이0(자기manifest 포함15)이다. 기존helper의 검증자 신호10개 최대99초/초과0/질문대기0·exit0이며 첫신호전/완료뒤는 미측정이다. release retainedexternal→동일incarnation/idle/빈draft→정확pane close ptyKilled=true·부모리드1개를 확인하고 완료묶음을 ack했다(E/`review/{lead-actual-audit-v1.json,liveness-result-v1.json,release-v1.json,close-v1.json,after-close-terminals-v1.json}`). 재사용하지 않는다.
- **리드 소유 보완 1회:** 두 결함은 리드 계약/행정 귀속이므로 Sol 제품 확정 실패·라우팅 신호5에 합산하지 않는다. 원계약/첫 고정판/판정을 보존한 E/`correction/contract-rule-supplement-v1.1.md`에 누락절 원문 전체·출처SHA·관련성·향후 적용단계를 명시했다. 완료된 Sol에게 보충을 전달했다거나 당시 원문 준수를 소급 수리했다고 하지 않는다. 신규 재실사는 이 명시 버전 보충과 현재 산출물로 독립 판단한다.
- **후보 인용 보완:** 리드 goal의 ID 세개에 정본 BACKLOG 인용을 붙이고 후보가 아닌 비채택 선언은 일반 문단으로 바꿨다. 같은 기존 helper는 전 policy-violation/exit1·참조없음2/후보3 → 후 allowed/exit0·참조없음0/후보2·중복ID0/unknown0이다(E/`review/raw/candidates-01.*`·`correction/candidates-after-v1.*`). 검사기/정책·안정ID를 바꾸지 않았다. 보완 후 새 일곱 문서 입력을 `review-02/`에 고정하며 독립 해소/PASS는 아직이다.
- **신규 독립 재실사 통과:** 이후 새 Astra의 E/`review-02/verdict.md` 전문을 직접 읽고 현재 #1·#2 해소와 문서 실사 통과를 인수했다. 실제 Task/Dispatch·succeeded done, 자기 명단16파일(자기manifest 포함17)의 bytes/SHA 차이0·고정46입력 차이0을 대조했다. 일곱 문서·일곱 양식 사용사례/30초 탐색·변경 로컬 링크22개 미해결0·동일 후보helper exit0/진단0이다. O1(H1 원문 탐색)·과거 원문 누락·최초 작성자 주입 전문/backend 모델 미확인은 보존하며 제품/삭제/게임/DB/CI 실행 통과로 확대하지 않는다. 판정·원시·인수는 E/`review-02/`에 있다.
- **재실사 신호 형식 처분:** 초기 HB 두 건의 태그 붙은 alive/빈본문은 실제 body-tag 위반이다. 검증자가 발견했고 리드도 최초 수신에서 이를 놓쳤다. 두 원문/기존helper 위반 결과를 보존하고 이후 정확 alive로 교정했다. 메인 `msg_9c7fc4dc26eb`(23:08:44Z)은 현재 identity 일치·문서/측정/원시를 바꾸는 내용 없음·자기발견/교정을 이유로 **이번 두 건만 문서 판정과 분리해 비차단** 처리했다. 원문보존·교정·리드누락공개·판정의 출처/영향없음/한계 기록 조건을 최종 판정에서 확인했다. 사용자 결정·소급무위반·새일반예외가 아니며 원문은 E/`hb-disposition/main-disposition-original-v1.json`이다. 다른 세션 재발은 메인 지시대로 규칙 후보로 올리되 이번에 새 반복규칙을 만들지 않았다.
- **재실사 정산과 PR 발행:** 기존helper는 재실사 신호8개 최대224초·300초초과0/exit0을 반환했다(형식위반은 별도 보존, 첫신호전/완료뒤 미측정). release·동일 incarnation/idle·정확 pane 종료/부모리드1개와 완료 ack를 확인했다. 검수한 일곱 파일과46입력은 그대로인 상태에서 메인 CLAUDE만 별도 commit, 나머지 문서를 다음 commit으로 담아 PR263을 생성했다. 최초 발행 head는 `a091ae8b003a35e9ae85a02976beb1e08efc068c`이며 2commit·7파일 +177/-4, OPEN/미병합이다. 생성 뒤 관측한 CI4종은 당시 실행 중이었으며 최종 head·CI는 E의 새 관측으로 확인한다.
- **단계 결과 기록:** 최초 고정판·계약·판정은 E snapshot으로 보존한다. 독립 실사 완료와 PR 발행으로 단계가 바뀌어 goal/CURRENT의 리드 소유 두 문서에 결과·다음 행동을 기록하고 해당 행정 diff를 신규 Astra가 별도로 실사한다. 정본 세파일/CLAUDE/BACKLOG는 이 기록에서 바꾸지 않는다. 추가 기록 commit의 자기 head를 미리 적지 않고 최종 정확 head/CI와 판정 원문은 E 및 메인 보고로 연결한다. 메인 R-2·각 PR 승인/병합과 전체 종료는 아직 남았다.

## 다음 계획 후보

- 작은 읽기 일반 면제는 BACKLOG `small-read-resource-gate-boundary`에 두며 이번 현행 계약 명시와 구분한다.
- heartbeat 간격/helper 변경은 BACKLOG `heartbeat-interval-review`, 잠금 재발 원인은 BACKLOG `index-lock-recurrence-cause`, 선택창 권한 정책은 BACKLOG `claude-modal-permission-policy`에 원천/보류 조건을 연결했다. 상세 후보는 [BACKLOG](../../../00_Document/operations/BACKLOG.md) 한 곳이며 새 구현 채택은 없다.

Art H1의 하드 제한 구현·R-3 일반reply 예외·규칙 전면분할은 이번에 자동 채택/착수하지 않는다.
