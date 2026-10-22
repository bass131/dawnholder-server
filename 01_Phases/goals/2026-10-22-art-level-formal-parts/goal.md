# Art·Level 정식 파트 운영 규칙

## 재개 지점

다음 첫 일은 메인이 [PR257 - Art·Level 정식 파트 반영](https://github.com/bass131/dawnholder-server/pull/257)의 판정 원문·R-2 표본을 대조하고 사용자 정확 head 병합 승인을 받는 것이다. 작성자와 검증자는 정산·종료했고, 원래 보류 branch/HEAD의 clean 복귀도 확인했다. 이 PR 상태 기록을 커밋·푸시한 뒤 다시 그 branch로 돌아가 최종 head와 복귀 근거를 메인에 보낸다. 리드는 병합·자동 병합하지 않는다.

- 범위 v1은 메인 `msg_05287e94df36`의 사용자 지시 대조로 착수했다. 뒤이은 사용자 결정 A(메인 전달 `msg_7c3b9abcbabf`)로 Art·Level만 정식 승격하고 World의 지위·경로·권한은 유지하는 범위가 확정됐다.
- 공간 `rules-active`, branch `docs/art-level-formal-parts-20261022`, 기준은 최신 원격 main `65cdd20054b6ef296e814ad34a9a3363eb1a0877`이다. 보류 branch `feat/shared-execution-lock-20261015`와 기존 NOT PASS는 보존한다. PR 보고 뒤 시작 branch로 clean 복귀한다.
- 근거 E는 `.backups/verification/2026-10-22-art-level-formal-parts/`다. 현재 Run 주소·모델·runtime/handle/incarnation 등 상세는 E/`lead-context.md`·`run-create.json`·`run-address-send.json`을 따른다.
- 소유권: 리드는 이 goal/CURRENT Rules 줄, 작성자는 정본 네 파일, CLAUDE는 메인만 쓴다. 작성자/검증자는 Git·추가 위임을 하지 않는다.

## 진척 단계

- [x] 범위 검토
- [x] 새 목표 기록
- [x] 문서 작성
- [x] 메인 문장 반영
- [x] 독립 문서 실사
- [x] 문서 PR 생성
- [x] 원래 브랜치 복귀
- [>] PR257 병합
- [ ] 목표 종료 정리

## 요구사항 원천과 범위 결정

- 메인 진입 `msg_497900f2cb8e`(2026-10-22T11:48:54Z)가 전달한 사용자 원문: 「보조세션 열어서 아트랑 레벨쪽도 정식 파트로 승격하고, 파트 숨김처리 기능은 작동 안하는 리드는 숨기고 일하는 파트만 보이게 개선해줘」. 이 pane의 직접 사용자 입력으로 격상하지 않는다.
- 메인 해석은 앞부분의 저장소 운영 규칙 반영이다. 현황판 숨김 기능은 메인 보조 세션이 따로 한다. 원문은 E/`reception-inbox.json`, 범위 초안은 E/`scope-draft.md`, 원문 검색은 E/`current-parts-scan.txt`다.
- 메인 `msg_05287e94df36`은 경로·원문·모델 배정을 대조하고 v1 착수를 지시했다. 수신은 E/`check-scope-review-result.json`, ack는 E/`ack-scope-review-result.json`이다.
- World 답이 작성자 계약 발행 전에 오면 새 범위를 고정해 발행한다. 발행 뒤면 기존 작업에 주입하지 않고 같은 PR의 추가 범위를 새 작성자가 수행한다. 답이 늦어도 Art·Level을 진행한다. CLAUDE는 작성자 종료 뒤 메인이 사용자 답을 반영해 수정하고 종료를 알린다.

## 범위

### 만들 것

1. Art와 Level을 기존 Core·Content·Rules·CodeMap·Management에 더한 정식 파트로 반영한다. R-1을 상세 배치 정본으로 유지한다.
2. Art의 경로 `art-active`, 태그 `[Art 리드 Sol]`·`[Art Sol]`·`[Art 구현 Astra]`·`[Art 검증자]`를 반영한다. Level의 경로 `level-active`, 태그 `[Level 리드 Sol]`·`[Level Sol]`·`[Level 검증자]`를 반영한다. Astra 일반 태그 규칙은 기존대로 유지하며 Level의 모델 배정을 새로 만들지 않는다.
3. 활성 지침의 기존 다섯 리드 표현과 추가 파트 설명을 정합한다. 정식 파트 지위와 개별 목표·쓰기·실행·Git·병합 승인을 구분한다.
4. 새 goal에 범위·완료조건·PR 경계·적용 중 사용자 결정 원문/출처·실제 단계 결과를 두고 CURRENT의 Rules 줄을 연결한다.

### 건드릴 곳

| 파일·현재 위치(origin/main 기준) | 수정할 내용 | 작성 소유자 |
|---|---|---|
| `AGENTS.md:28` 역할과 범위 | 다섯 리드에 Art·Level을 추가한 정식 파트 명단. CodeMap/Architecture 경로·태그 유지 | 외부 작성자 1명 |
| `AGENTS.md:35` 모델 라우팅 | 기본 다섯 지칭을 정식 파트 지칭으로 정합. 모델·effort·시범·예외는 이번 PR에서 바꾸지 않음 | 같은 작성자 |
| `AGENTS.md:43` 메시지와 보고 | Art·Level의 지정 발신 태그 추가. 일반 Astra 태그 규칙과 출처 대조 유지 | 같은 작성자 |
| `00_Document/operations/ORCA.md:27-58` R-1 | 제목·정식 명단·배치 표 두 행·기본 다섯/종료 문장 정합. “게임 두 파트”는 Core·Content의 기존 담당을 보존하면서 새 정식 파트와 충돌하지 않게 표현. “Rules를 포함한 기존 목표 한정 추가 파트”는 정식 파트 외 기존 추가 파트의 승인 경계를 보존하는 문장으로 정합 | 같은 작성자 |
| `00_Document/operations/RESUME.md:21` 세션 진입 배치 | “현재 리드”를 “정식 리드”로 정합하고 상세는 R-1 링크로 유지. 별도 명단·경로표 복제 안 함 | 같은 작성자 |
| `.agents/skills/dawnholder-session-handoff/SKILL.md:24` 세션 진입과 배치 | “마감 구간 다섯 리드(Core·Content·Rules·CodeMap·Management)”를 “마감 구간 정식 리드”로 정합. 추가 파트 생성·상시 배치 권한이 스킬에서 생기지 않는다는 경계 유지 | 같은 작성자 |
| `CLAUDE.md:26` 메인 세션 진입 | 아래 원문 대비안. 메인이 직접 이 branch에서 수정하고 쓰기 종료를 알려 줌 | 메인 전용 |
| `01_Phases/goals/2026-10-22-art-level-formal-parts/goal.md` 신규 | 승인된 범위·단계·현재 결정·결과·재개 지점 | Rules 리드 |
| `00_Document/operations/CURRENT.md` Rules 줄 | 새 goal·branch 연결. 이전 보류 goal은 링크로 보존, 다른 파트 줄의 상태·branch는 변경 안 함 | Rules 리드 |
| E 아래 메모·원문·계약·실사·Git receipt | 근거만 보존하며 Git 제외 유지 | 역할별 할당 |

범위 탐색 원시: `current-parts-scan.txt`. 같은 책임의 활성 지침만 검색했다. goal-loop의 확정 실패 “다섯 번째”와 역사/BACKLOG의 당시 “다섯”은 파트 수가 아니므로 수정하지 않는다. `AGENTS.md:51`의 일반 추가 파트 태그 규칙은 Art·Level 승격과 충돌하지 않아 보존한다.

### 하지 않을 것

- World의 지위·경로·권한은 현재대로 유지한다. 별도 사용자 결정 없이 정식 승격하지 않는다.
- 대시보드 코드·숨김 기능·개인 도구·게임 구현·아트/레벨 에셋·다른 파트 goal을 수정하지 않는다.
- 보류 중 공용 실행 잠금 목표·제품·시험·기존 NOT PASS·실패 집계를 변경하지 않는다.
- 모델 라우팅 개선 결정의 정본 반영, 전역/프로젝트 설정 변경, 판 번호 변경, 새 검사·도구·하네스 구현을 포함하지 않는다. 계약과 goal에는 현재 적용 결정의 원문·출처를 남긴다.
- 리드가 운영 규칙의 구현을 대신하거나 CLAUDE.md를 쓰지 않는다. 역사 goal·보관 기록·고정 링크·기존 태그 기록을 일괄 교체하지 않는다.
- Unity·WSL·Windows dotnet·게임·DB 실행은 하지 않는다. 실행 창이 필요해지면 메인에게 창 요청 세 줄을 보낸 뒤 배정을 기다린다.
- Codex/리드/작성자/검증자가 병합·자동 병합하지 않는다.

### 관찰 가능한 완료조건

1. AGENTS의 정식 파트·태그와 R-1의 명단·경로 표가 Art·Level을 일치되게 포함한다. CodeMap/Architecture와 World 추가 파트 경계, 기존 승인·소유권·모델 계약은 보존된다.
2. CLAUDE·RESUME·session-handoff가 현재 R-1과 충돌하지 않는다. 활성 “다섯 리드/기본 다섯” 잔여를 경로별 대조하고, 수정하지 않은 일치 항목은 역사/다른 의미임을 분류한다.
3. 작성자와 분리된 신규 외부 Sol xhigh가 최종 실제 diff와 메인 CLAUDE 쓰기 종료 후 내용을 문서 실사한다. 내용·참조·권한·사전 메모·관련 규칙 원문·실제 준수 위치를 판정하고 원문을 E에 보존한다.
4. 문서 시나리오에서 새 Art/Level 리드의 작업 경로·태그·기동 정본을 찾고, World의 기존 승인 한계 및 목표 종료/R-8·병합 경계를 찾을 수 있다. 문서 반영을 실제 세션 기동/실행/숨김 기능 성공으로 보고하지 않는다.
5. 최신 main 기반 문서 PR 1개와 정확한 40자 head·독립 판정 원문·수행/미실행을 메인에 보고한다. 이 checkout은 원래 보류 branch로 clean 복귀한다. 병합은 PR별 사용자 명시 승인 뒤 메인이 수행한다.

## CLAUDE.md 메인 전용 수정안

원문(26행의 해당 문장):

> 마감 구간의 리드는 Core·Content·Rules·CodeMap·Management 다섯이다.

제안:

> 마감 구간의 정식 리드는 Core·Content·Rules·CodeMap·Management·Art·Level 일곱이다.

같은 항목의 메인 경로·Core 경로·나머지 승인 worktree·R-1 링크·Core 전환 문장은 유지한다. Rules/작성자는 이 파일을 수정하지 않는다.

## PR 경계와 검증

- 하나의 문서 PR: 위 현행 문서 5개(메인 CLAUDE 포함) + goal + CURRENT. 실행 도구/코드/테스트 변경 0.
- 경량 경로: 승인 후 외부 작성자 한 명이 문서 범위를 끝까지 작성한다. 지정은 `gpt-6.1-sol max`이며 최초 화면에서 max를 확인하지 못하면 대체 없이 메인에 보고한다. 작성자 추가 위임·Git 작업 금지, 작업 하나 뒤 정산·종료.
- 메인이 CLAUDE를 직접 수정한 뒤, PR 직전 신규 `gpt-6.1-sol xhigh` 독립 문서 실사 한 번. 파트당 검증자 동시 하나. 결함이 있으면 같은 범위 안에서 새 작성자/검증자로 수정·재검증한다.
- 판정 등급은 문서 실사. TDD·게임/DB/빌드/실행 창은 해당 없음. 스킬 한 문장 변경은 현실적인 리드 배치·목표 종료 시나리오를 함께 대조한다.
- 자체 확인은 기존 git diff --check, 기계 검색·링크/anchor 실사로 제한한다. 새 테스트·검사 하네스를 만들지 않는다. 후보를 등록하거나 종료 기록이 포함되면 기존 후보 도착 검사만 적용한다.
- 관문: 범위 대조 → latest main 및 clean 확인 → goal/CURRENT → 외부 작성 → 메인 CLAUDE 쓰기 종료 → 독립 실사 → 리드 commit/push/PR → 정확 head 보고·clean 복귀. 병합 승인과 R-8/종료 점검은 별도 경계다.


## 결정과 단계 결과

- `git fetch origin main` 종료 0, 기준은 `65cdd200` 그대로였고 새 문서 branch를 만들었다(E/`branch-create.txt`). 로컬 main은 뒤처져 있어 기준으로 쓰지 않았다. 보류 branch에는 통합/쓰기를 하지 않았다.
- 외부 Sol max 첫 화면·readiness·cwd/branch를 확인하고 최초 작업 계약을 발행했다. worker-start는 입력 접수 뒤 시작 미관측이었으나, 단일 draft의 크기·현재 runtime/incarnation·다른 입력 0을 대조한 뒤 R-5의 bare Enter 한 번으로 실제 시작을 확인했다. 기동 원시는 E/`doc-write-worker-start.json`, 복구 전제는 E/`doc-write-draft-recovery-gate.json`, 실제 시작은 E/`doc-write-post-submit-screen.json`이다.
- **계약 전달 차이에 대한 메인 판단:** `exactEqual=false`이며 끝 CR/LF 네 문자만 다르다. 자동 지침과 분리한 실제 Task user 입력의 계약 본문은 끝 줄바꿈 제거 비교에서 완전 일치했다(E/`doc-write-post-submit-contract-comparison.json`, 원문/제출 계약과 user 입력 txt 보존). 메인 `msg_94b8c3bae068`(2026-10-22T12:12:42Z)은 의미·규칙·경로 누락이 없고 R-5 복구 조건도 충족됐으므로 같은 Task·Dispatch 진행과 보류 해제를 지시했다. 수신 원문은 E/`ack-writer-hold-handoff-status.json`이다. 이 차이와 근거는 독립 실사 계약에도 넣어 검증자가 다시 대조한다.
- 위 메인 지시에 따른 goal 상태·판단 기록 추가는 행정 보충이며 범위·계약 본문을 바꾸지 않는다. 최초 고정 입력 manifest는 보존하고, goal의 새 hash만 반영한 별도 보충 manifest를 작성자에게 명시 전달한다. 독립 실사·제품 실행·실행 창 사용·PR 생성은 아직 없다.
- **문서 작성 완료:** 작성자 `worker_done succeeded`를 현재 handle·Task·Dispatch와 대조하고 실제 네 파일 diff 및 보고를 읽었다. 네 파일의 14삽입·12삭제는 실제 numstat와 일치했다. 보고는 E/`doc-write/report.md`, 완료 원문은 E/`check-writer-resume-followup.json`이다. 신규 독립 판정은 아직 아니다.
- 정상 정산 뒤 worker-release는 외부 터미널로 retained를 반환했다. 정산·현재 runtime/incarnation·빈 prompt를 확인하고 정확한 작성자 pane만 닫았으며 부모 Rules pane 하나가 남음을 확인했다(E/`doc-write-release.json`, `doc-write-close.json`, `doc-write-after-close-terminals.json`). 작성자는 재사용하지 않는다.
- **생존 신호 운영편차:** 기존 helper가 저장된 세 신호에서 12:12:31Z 보류 status→12:23:18Z 완료까지 647초 초과 1구간을 계산했다. 보류 해제 뒤 작성·자체점검 중 추가 신호가 없었으며 공식 heartbeat도 관측되지 않았다. 첫 신호 이전은 helper가 측정하지 않으므로 전 구간 준수로 보고하지 않는다. 근거는 E/`doc-write-liveness-command.json`·`doc-write-liveness-result.json`이며 메인에게 보고한다. 초과만으로 추가 대화·도구 전수 감사를 수행하지 않는다.
- 최초 goal/CURRENT 쓰기는 최신 main의 CURRENT Rules 줄이 보류 branch와 달라 patch 검증에서 중지됐다. 파일 변경 없이 해당 줄을 다시 읽고 최신 원문만 대조해 수정했다. 무관한 과거 상태를 현재값으로 복사하지 않는다.

- **메인 쓰기 종료와 편차 처리:** 메인 `msg_5f517075aa00`(2026-10-22T12:44:12Z)은 CLAUDE.md 26행의 쓰기 종료와 1삽입·1삭제를 알리고 독립 실사 발행을 지시했다. 실제 SHA256은 전달값과 일치했고 나머지 문장은 보존된 diff를 확인했다(E/`main-claude-write-hash-check.json`). 생존 신호 647초 편차는 기록으로 충분하며 추가 감사·재작업 없이 독립 실사 입력으로 보존하라는 메인 판단이다. 원문은 E/`check-main-claude-write-finished.json`이다.

- **독립 문서 실사 PASS:** 신규 Sol xhigh가 승인 원문·실제 일곱 파일 diff·내용/참조/권한·메모/원문·실제 준수 및 문서 시나리오를 실사하고 차단 문서 결함 없음으로 판정했다. 판정 원문 E/`doc-review/verdict.md`, 최종 입력 보존 E/`doc-review/final-audit.json`, 완료 원문 E/`check-doc-review-final.json`을 리드가 직접 읽고 현재 Task/Dispatch·명시 outcome과 실제 상태를 대조했다. 당시 실사 범위는 7파일 190삽입·14삭제다. 게임·DB·CI·실제 세션/숨김 기능은 미실행이며 문서 PASS로 확대하지 않는다.
- 검증자 자신의 신호 12:55:48Z→13:01:37Z 349초 편차는 원시/helper로 확인해 구간·이유를 기록했다. 공식 질문 대기 13:02:18Z→13:07:22Z와 구분하고 전 구간 준수를 주장하지 않는다. 다른 의심 근거가 없어 기존 초과 정산 기준에 따라 추가 감사·재작업 없이 처리했다(E/`doc-review-heartbeat-question-reply.json`, 판정 OP-2). 작성자 647초는 메인 처리 OP-1로 별도 보존한다.
- 검증자의 전체 쓰기 종료·worker_done succeeded 뒤 release는 retained/external_terminal을 반환했다. exact 현재 identity/빈 prompt를 확인해 해당 pane만 닫았고 부모 Rules pane 하나를 확인했다(E/`doc-review-release.json`·`doc-review-close.json`·`doc-review-after-close-terminals.json`). 완료 delivery도 전체 처리 뒤 ack했다. 검증자는 재사용하지 않는다.
- 위 고정 실사 입력은 보존하며, 이 단락과 다음 PR/복귀 상태 등 리드의 사후 결과 기록만 goal에 추가한다. 실사 뒤 정본 다섯 파일과 CURRENT의 내용을 변경하지 않으며, 최종 PR 보고에서 실사 입력과 후속 행정 기록을 구분한다.

- **PR 생성과 복귀 확인:** [PR257 - Art·Level 정식 파트 반영](https://github.com/bass131/dawnholder-server/pull/257)을 main 대상으로 생성했다. 원문/receipt는 E/`pr-body.md`·`pr-create-receipt.json`, commit/push 원시는 E/`git-commit-documents.txt`·`git-push-documents.txt`다. 원래 `feat/shared-execution-lock-20261015`/`2419c013`로 clean 복귀해 보류 상태 보존을 확인했고, 이 PR 번호·단계 기록을 위해 문서 branch로 잠깐 돌아왔다(E/`return-original-branch-first-state.json`). 마지막 상태 기록 push 뒤 다시 원래 branch로 clean 복귀하며 최종 head/원격 대조는 E와 메인 보고에 둔다.
- 현재 PR은 열림·미병합이다. 전체 목표 종료/Gardener/결과 기록과 모든 PR 병합이 끝나기 전에는 최신 main detach 정리를 완료로 표시하지 않는다. 다음 goal을 자동 시작하지 않는다.

## 적용 중인 사용자 결정

- **전체 목표 종료 때 최신 main 정리:** 메인 전달 `msg_5f517075aa00`의 사용자 원문은 「각 파트들 Goal 작업 다 끝나면 main 브랜치를 체크아웃 받아서 한번 최신화해줘」다. 메인 해석은 이 목표의 PR이 모두 병합되고 결과 기록이 끝난 뒤 리드 pane을 닫기 전에 실행하는 것이다. `git fetch` → clean 확인 → `git checkout --detach origin/main` → 종료 보고에 HEAD 40자 기록 순서다. main branch는 메인 checkout이 소유하므로 다른 worktree는 detach로 이동한다. 미커밋 변경이 있으면 이동하지 않고 경로를 메인에 보고하며, 로컬/원격 branch를 삭제하지 않는다. 이 문서 PR에서 R-8 정본 문장을 새로 바꾸지 않는다. 현재 PR 생성 전 이동하는 지시가 아니며, 기존 보류 branch는 보존한다.

- **World 제외 확정:** 메인 전달 `msg_7c3b9abcbabf`(2026-10-22T12:00:26Z)의 사용자 원문은 「대시보드 결정 응답: 1) Art·Level 정식 파트 문서 PR - World도 함께 정식 파트로 올릴지 → A Art·Level만」이다. 작성자 계약 발행 전에 수신했으며 v1 범위 그대로 적용한다. 원문은 E/`check-before-writer-launch.json`이다. CLAUDE는 작성자 종료 뒤 메인이 초안 문장대로 직접 수정한다.

아래는 메인 진입 메시지 `msg_497900f2cb8e`의 원문 전달이다. 이 세션의 직접 사용자 입력이나 본 PR의 라우팅 정본 반영으로 격상하지 않는다. 사용량·시각은 당시 근거이며 현재 재측정값이 아니다. Rules 10-28 뒤 보류라는 일반 우선순위와 이번 승인된 문서 작업을 구분하며 착수는 위 별도 범위 대조에 근거한다.

적용 중인 사용자 결정 원문:
1. 병렬 지시: 「병렬로 가능한 파트는 작업 촘촘하게 지시해줘」
2. 실행 창 대기 종료: 「그리고 만약에 한 파트에서 이미 서버를 쓰고 있으면 다른 대기 파트에서 계속 리드나 작업자들이 우편확인 하는게 아니라 재개 지점 작성하고 세션을 종료하는 식으로 간다음 다시 열어서 재개하는 식으로 가자

토큰 낭비가 심해」
3. 경량 경로: 「대시보드 결정 응답: 1) 10-28까지 로컬 개발 작업에 경량 경로를 쓸지 → A 10-28까지 경량 경로」 → 작업자 한 명이 PR 범위 끝까지, 독립 검증은 PR 직전 한 번, 창 요청 세 줄. 병합 승인·비밀 열람 0·설정 불변·우회 금지·PR별 독립 검증은 그대로.
4. 보이는 기반 우선: 「빠르게 기반 먼저 만드는거를 목표로 해야돼, 지금 뭐 보여줄게 없어 우리」
5. 낡은 Git 잠금: 「대시보드 결정 응답: 1) 낡은 Git 잠금 파일이 또 생겼을 때 처리 방식 → A 조건이 맞으면 메인이 지우고 알림」 → 리드·작업자는 index.lock을 지우거나 우회하지 않는다.
6. 비밀 쓰기 명령: 「이럴거면 내가 직접 실행하는게 낫지 않나」 → 비밀 값을 쓰는 명령은 사용자가 직접 실행한다. AI는 비밀 원문을 읽지 않는다.

--- 적용 중인 사용자 결정 블록(원문, main-notes/2026-10-19/routing-decision-entry-block.md) ---
# 리드 진입 메시지에 넣을 적용 중 사용자 결정 — 모델 라우팅 개선 구성과 사용량 페이싱

이 파일은 메인이 리드 진입 메시지에 그대로 붙이는 전달 초안이다. 정본이 아니다. 정본 반영은 Rules 리드가 맡고, 반영 전까지는 각 리드가 자기 goal의 「적용 중인 사용자 결정」에 이 블록의 원문과 출처를 기록한다.

## 출처

- 사용자 원문(2026-10-19 메인 대화, Enter로 제출된 입력): 「OK A로 진행하되, 지금 워크트리로 규칙이 사본 형식 느낌으로 분산되어있어서 전부 적용하려면 시간이 좀 걸릴꺼 같은데」
- A의 내용: 현황판 결정 「모델 라우팅 - 개선 구성 적용과 10-25 리셋까지 Codex 크레딧 사용량」의 A 「개선 구성 + 크레딧 절반」. 근거 스크립트 `C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-19/scripts/update-decision-routing-bundle-1400.cjs`.
- 선례: Sol xhigh 전환처럼 사용자 결정에 따른 런타임 전환은 정본 병합을 기다리지 않는다. 자기 checkout의 AGENTS·ORCA·스킬 사본이 아래와 다르면 이 블록이 우선한다.

## 라우팅

| 역할 | 모델 · 실행 |
|---|---|
| 파트 리드 (Core 포함) | `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh` |
| 구현자 일반 | `codex --model gpt-6.1-sol -c model_reasoning_effort=max` |
| 구현자 위험 신호 (셸·보안·입력 경계, 시간·순서·동시성·수명, 런타임·언어 경계) | `codex --model gpt-6.1-sol -c model_reasoning_effort=max` |
| 구현자 (같은 계약 두 번째 독립 불합격) | `codex --model gpt-6-astra -c model_reasoning_effort=xhigh` |
| 위험 신호 구현의 독립 검증 | `claude --model claude-opus-5-5 --autocompact 250000` |
| DB·영속 데이터·프로토콜·공유 DLL·보안 경계 독립 검증 | `claude --model claude-opus-5-5 --autocompact 250000` |
| 그 밖의 독립 검증 (문서 실사, 약·강 등급, 로컬 DB 설치·기동 도구 카드) | `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh` |
| 선행 시험 작성, Gardener | `claude --model claude-opus-5-5` (변화 없음) |
| 확정 실패 3회 뒤 자문, 설계 검토 시범 | `claude-fable-5-1` (변화 없음) |

- 「설계 판단이 남은 새 다파일 기능」 신호와 「애매하면 Astra」 규칙은 쓰지 않는다.
- Sol 구현을 다른 새 Sol 세션이 검증하는 것을 허용한다. 구현자·검증자 분리, 작업 하나 뒤 종료, 독립 검증 생략 금지, 리드 직접 구현 금지는 그대로다.
- 로컬 DB 도구 카드: 사용자 실행 카드에서 독립 검증이 놓친 결함이 한 번이라도 나오면 Opus 검증으로 되돌린다.
- `max`가 첫 기동 화면에 없으면 대체하지 말고 메인에 보고한다. 계약에는 지정 모델·effort와 화면 관측값을 적는다.
- capacity 예외(Sol 30분 실패 뒤 Astra xhigh 새 세션)와 Claude 첫 화면 처리는 그대로다.

## 사용량 페이싱

- 사용자 원문(2026-10-19 14:1x KST, 위 A의 크레딧 부분을 바꿈): 「크레딧은 최대한 아끼고 싶긴한데, 사용량이 부족하면 어쩔 수 없긴 하네 / Codex 사용량 절감은 최대한 플랜 주간 소모량 기준으로 잡고, 크레딧은 비상 상황에서만 쓰는거로 하ㅏ」
- Codex는 플랜 주간 한도만 기준으로 나눠 쓴다. 주간 한도를 하루 약 14%p로 나눈다. 크레딧은 비상 상황에서만 쓴다.
- Codex 주간 한도는 10-19 14:11 기준 95%이고 리셋은 10-25 05:40 KST다. 그 전까지 남은 약 5%(약 1,600 크레딧)를 넘어 쓰면 크레딧이 줄어든다.
- 사용자 원문(2026-10-19 14:2x KST, 비상 사용 결정): 「음 어찌보면 지금이 그 비상상황같은데 일단 크레딧을 쓰느거로 할까, 대신에 라우팅 측정할때는 주간 사용량 기준으로만 체크하는 식으로 가고」
- 그래서 10-25 05:40까지는 크레딧을 비상으로 쓴다. 쓰는 속도는 주간 한도 기준과 같게 맞춘다. 하루 약 14%p, 크레딧으로 약 4,600이다(주간 100% ≈ 3만 2천 크레딧, 10-18 측정 환산). 라우팅의 효과는 이 주간 환산 속도로만 판단한다.
- 상한은 앞서 승인한 18,000 크레딧이다. 쓴 크레딧이 약 16,000에 닿으면 메인이 사용자에게 다시 묻는다.
- 메인이 사용률을 목표선과 비교해 앞서면 우선순위가 낮은 파트부터 새 작업자 기동 보류를 지시하고, 뒤처지면 재개를 지시한다. 리드는 그 지시를 따른다.
- 작업자 세션 하나의 크레딧이 크게 늘면(예: Sol max 구현이 세션당 200을 넘음) 리드가 메인에 알린다.

## 리드가 할 일

- 이 블록의 원문과 출처를 자기 goal.md 「적용 중인 사용자 결정」에 기록한다. 자동 압축 뒤에는 goal.md를 다시 읽고 이 결정을 따른다.
- 작업자·검증자 위임 계약마다 위 표의 지정 모델·effort·실행 명령을 적고, 기동 뒤 화면 관측값을 함께 남긴다. 자기 checkout의 AGENTS·ORCA·스킬 사본과 다르면 이 블록을 따른다. worktree의 규칙 파일은 고치지 않는다.
- 파트 배분: 동시에 두 트랙까지만 연다. 순서는 Core → World 한 트랙, 그다음 Content, 그다음 Art(PR253 병합 뒤)다. Rules·Management는 10-28 뒤다. 메인이 목표선에 따라 보류·재개를 지시한다.
- 2026-10-20 20:00 KST 전에는 새 Opus 세션을 열지 않는다. Opus 검증이 필요한 단계(DB·영속 데이터·프로토콜·공유 DLL·보안 경계, 위험 신호 구현)는 그 뒤로 미루고, 그 전에는 Sol로 끝나는 단계만 진행한다.
