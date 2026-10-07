# hook 차단 줄이기와 보조 세션 스킬

## 재개 지점

Rules의 목표다. 사용자가 범위 초안 v1을 승인했고(아래 「요구사항 원천과 적용 결정」) 재개를 지시했다. 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch: PR1은 `fix/hook-net-false-positives-20261007`(base `8e498440`)다. PR2는 PR1 병합 뒤 최신 main에서 만든다.
- 근거 폴더 E: `.backups/verification/2026-10-07-hook-friction-helper-session/`(Git 제외). 리드 맥락 메모는 [astra-context.md](../../../.backups/verification/2026-10-07-hook-friction-helper-session/astra-context.md), 승인 확정본은 [scope-approved.md](../../../.backups/verification/2026-10-07-hook-friction-helper-session/scope-approved.md)(SHA256 `0e536cac…`)다. 받은 메시지 원시와 수신 대조는 E/session/에 있다.
- 리드: 신규 `claude-opus-5-5` xhigh, 태그 `[Rules Astra]`, handle `term_ad29f105-dff5-4f1b-9411-372b93adf7ea`. Run은 `run_573214a00f1b`, 회신 주소는 `run:run_573214a00f1b`다. 이전 Rules goal의 Run·Task·Dispatch는 실행 권한이 아니다.
- **현재 위치**(2026-10-07T15:1xZ): 첫 독립 검증이 FAIL(차단 V1·V2)이다(아래 「PR1 독립 검증」). 메인이 O1을 받았다. 다음은 동작 계약 v3.2 → 신규 Opus 선행 시험 → 신규 Sol max 수정 → 신규 Opus 재검증이다. push 전이다.
- 주의: rules-active의 리드 세션에도 병합 관문 hook과 마지막 그물이 실린다. PR1 구현 중에는 이 checkout의 hook이 고치는 중인 판정 코드를 바로 쓴다(아래 「위험」).

## 진척 단계

- [x] 범위와 기준 확정
- [x] 그물 설계와 선행 시험
- [>] 그물 구현·검증
- [ ] 그물 PR 병합
- [ ] 정본·스킬 작성·실사
- [ ] 정본 PR 병합
- [ ] 결과 기록·Gardener
- [ ] 종료 점검과 R-8 인계

PR 단계 이름은 PR이 생기면 「PR### 병합」으로 바꾼다.

## 범위

승인 범위는 초안 v1(`msg_15f282e3eec6`)이다. 사용자 질문 1·2의 답은 A, 메인 판단을 청한 질문 3도 A다. 아래는 그 답을 반영한 범위다.

### 만들 것

PR1 — 그물 오탐 줄이기(코드)

1. 리드가 쓰는 동작 계약 v3. 현행 v2.3(E0/merge-gate-behavior-spec-v2.3.md, E0는 이전 goal 근거 폴더)의 §4 「마지막 그물」과 정밀 판정 입력 준비의 차이만 적는다.
   - 「실행되지 않는 글자」: 따옴표 구분자 heredoc 중 셸·인터프리터가 아닌 명령이 받는 본문(`cat >`·`cat >>`·`tee`·`git commit -F -` 등), 글만 다루는 명령에 넘기는 따옴표 문자열의 글자 부분. 큰따옴표 안 `$(...)`·백틱은 코드로 남긴다.
   - 글만 다루는 명령의 기본안: `echo`·`printf`·`cat`·`tee`, `git commit -m`, `gh pr create`·`gh pr edit`의 제목·본문 값, `orca orchestration send`의 subject·body 값, 스크립트 파일 경로를 받은 `node`의 뒤 인자. 마지막 항목은 「스크립트 파일 속 명령」이 이미 정본의 우회 한계라 새 구멍을 만들지 않는다는 판단이다. 목록은 v3에서 확정한다.
   - 그대로 코드로 보는 곳: 별칭·함수 정의, 변수 대입 값, `eval`·`source`, `bash -c`·`sh -c`·`node -e`·`python -c` 류 인자, 셸·인터프리터로 가는 heredoc과 파이프, `git push`의 인자와 `gh`의 제목·본문 밖 인자.
   - 정밀 판정(`gh pr merge` 찾기)도 같은 글자를 병합 시도로 보지 않는다. 관측된 오탐은 0건이지만 그물만 풀고 정밀 판정이 같은 글을 막으면 규칙이 어긋난다.
   - 경계 판정이 애매하면 지금처럼 막는다(실패 시 차단).
2. 선행 시험(판정 형태 표): 「막힘 유지」와 「통과」 두 묶음.
   - 통과 묶음: 10-07 실제 오탐 10건의 원문(부작용 없는 hook 입력으로만 실행), 현행 「일부러 받아들이는 오탐」 중 새 정의로 풀리는 것.
   - 막힘 유지 묶음: 현행 정밀·그물 시험 형태 전부와 새 우회 변형(따옴표로 감싼 refspec·별칭·변수·`eval`·셸로 가는 heredoc·셸로 파이프되는 문자열·인터프리터 `-c`/`-e`).
   - 이 표가 이전 goal 종료 Gardener 정리 후보 ①(판정 형태 표의 시험 자료화)을 채운다.
3. 판정 코드 변경: `99_Tools/MergeGate/merge-policy.mjs`의 그물·정밀 판정 입력 준비.
4. 병합 관문 후속 세 건(BACKLOG `merge-gate-code-followup`): T1 그물 조건 3의 `+main`, 같은 파일 한국어 이유 주석 세 줄의 영어화, O-T3 정식 단독 병합이 그물을 건너뛰는 이유 주석.
5. 그물 설명 맞추기: ORCA 「병합 관문」의 그물 두 문장, `99_Tools/README.md`의 `suspect-words` 행.
6. goal 시작 기록: 이 문서, CURRENT의 Rules 줄.

PR2 — 정본·스킬·BACKLOG(문서)

1. 보조 세션 진입 스킬 `.agents/skills/dawnholder-helper-session/SKILL.md`. 메인 개인 도구의 초안(`C:/Dev/DawnHolder_Dashboard/plans/helper-session-skill-draft.md`, 8,508 bytes)을 옮긴다. 그물 동작은 다시 적지 않고 ORCA 병합 관문 링크만 둔다. Git 밖 문서 의존 문장은 「메인 개인 도구」로 표시하거나 뺀다.
2. AGENTS 「메시지와 보고」 태그 목록에 보조 태그 형식 `[<용도> <모델>]` 한 구절.
3. ORCA R-1에 보조 세션 배치 한 문장(메인 checkout 탭의 split, 읽기 전용, 진입은 보조 세션 스킬). 기존 문단에 붙여 250줄을 지킨다.
4. 「PR 생성은 리드가 확인 창 없이 하고, 사람의 PR 검수는 병합 관문(메인 R-2 대조 → 사용자 승인 한 줄)에서 한다」를 goal-loop 「통합과 보고」와 ORCA 병합 관문에 맞춘다. 전역 설정 상태는 정본에 적지 않는다.
5. BACKLOG 첫 변경(Management 넘김 원문 그대로).
   - 「Management에서 이관한 후보」에 `management-text-contrast-guard` 새 행(1A).
   - `contract-context-check` 출처 칸에 Management goal 종료 Gardener 후보 1 근거(2A)와 Z1(PR202 리드 메모 사후 작성) 연결. 행의 ID·최초 7필드는 보존한다.
   - `mailbox-output-loss-hook` 출처 칸에 송신 결과 파이프 끊김 네 파일 근거 연결.
   - `merge-gate-code-followup` 상태를 이 goal 승격 링크로 바꾼다. 남는 O2·O6는 「다음 계획 후보」로 두고 종료 때 미해결이면 새 행으로 등록한다.
6. CLAUDE.md 서브에이전트 문장에 「메인 개인 도구 폴더 한정, 사용자 결정마다」 예외 한 줄(질문 2 A, 메인 작성, 리드가 커밋).

### 건드릴 곳

- PR1: `99_Tools/MergeGate/merge-policy.mjs`, `99_Tools/MergeGate.Tests/`(새 표 시험 파일, 바뀌는 기존 단정), `99_Tools/README.md`, ORCA 「병합 관문」, 이 goal, CURRENT Rules 줄. CI workflow는 glob(`99_Tools/MergeGate.Tests/*.test.mjs`)이 새 시험을 잡아서 바꾸지 않는다.
- PR2: `.agents/skills/dawnholder-helper-session/SKILL.md`(신규), AGENTS 「메시지와 보고」, ORCA R-1·「병합 관문」(감축 포함), goal-loop 「통합과 보고」, BACKLOG, CLAUDE.md(메인), 이 goal.

### 하지 않을 것

- 상태 폴더 보호(`protected-path`)와 승인 문장 주입 차단(`approval-injection`)의 범위 변경.
- 승인 기록·통과 조건·PermissionRequest 대신 승인의 변경.
- `merge-gate-code-followup`의 O2(비ASCII 공백)·O6(이상한 session_id).
- 진입 스크립트의 import 실패 처리 변경(아래 「위험」 2). 필요하면 다음 계획 후보로 둔다.
- 최소 운영 셋업 2단계(검증자 임시 쓰기 경계·TDD 정본화), 3단계(BACKLOG 도착 확인 장치), 4단계(우편함 대기 출력 유실 hook).
- Codex 세션 병합 차단, 에이전트 계정·ruleset, 전역 Claude·Codex·Git·gh 설정 변경.
- 현황판 코드·README 변경(메인 개인 도구), 보조 세션의 실제 기동, Orca→Herdr 이주 검토.
- BACKLOG `merge-gate-code-followup` 행 출처 칸 축약(이전 PR 실사 관찰 O1). 최초 등록 필드 보존 계약과 부딪힌다.
- 과거 goal 원문 수정.

### 관찰 가능한 완료조건

1. PR1 선행 시험이 구현 전에 통과 묶음에서 실패하고 구현 뒤 통과한다. 막힘 유지 묶음은 구현 전후 모두 막힌다.
2. 기존 MergeGate·Orca 시험이 통과한다. 바뀐 기존 단정은 전수 분류 표에 이름·이전 단정·새 단정·요구사항 출처를 남긴다.
3. 후속 세 건이 반영되고 독립 검증자가 확인한다.
4. PR1 독립 검증자가 진입 스크립트에 10건 원문 입력을 넣어 한 번 실행한다. 실제 Claude 세션에서도 무해한 형태로 한 번 확인한다(파일로 가는 heredoc 통과, 셸로 가는 heredoc의 우회 형태 차단). 실제 병합과 main push는 하지 않는다.
5. ORCA는 250줄 이하다. 여섯 파일 묶음(AGENTS·ORCA·goal-loop·RESUME·세션 인계 스킬·orca-work)은 PR마다 bytes가 늘지 않거나 같은 PR에서 줄인 원시 수치를 남긴다. 기준은 main `8e498440`의 102,658 bytes다(E/bytes-baseline.txt). 새 스킬은 묶음 밖에서 따로 재며 8,508 bytes 이하가 목표다. CLAUDE.md는 메인 몫으로 따로 잰다.
6. PR2는 신규 검증자의 문서 실사를 통과한다. 현실 시나리오 셋을 대조한다: 메인이 보조를 여는 진입, 보조 pane의 사용자 직접 지시와 메인 전달, 리드 PR 생성 → 승인 묶음 → 메인 R-2 → 사용자 승인 줄.
7. 두 PR 모두 새 관문(메인 창 승인 줄)으로 병합한다.
8. 결과 기록 → Gardener → 종료 기록 PR → 종료 점검 → R-8 순서로 끝낸다. 다음 goal(최소 운영 셋업 2단계)은 자동으로 시작하지 않는다.

## PR 경계와 검증

| PR | 변경 경계 | 등급과 세션 |
|---|---|---|
| 1 - 그물 오탐 줄이기 | 판정 코드·시험·도구 README·ORCA 그물 문장·goal·CURRENT | 강: 보안 경계와 실행 도구. 선행 시험은 신규 Opus(시범 대상 아님), 구현은 Sol max, 독립 검증은 신규 Opus(시범의 보안 경계 배정) |
| 2 - 정본·스킬·BACKLOG | 문서만 | 문서 실사. 작성은 Sol max, 실사는 신규 `gpt-6-astra` xhigh(시범). CLAUDE.md 문장은 저장소 밖 메인 개인 도구에 관한 것이라 보안 경계로 보지 않는다(메인 `msg_53e10a4d10c3`) |

- 순서: PR1 → PR2. 둘 다 ORCA와 이 goal을 고치므로 PR2 branch는 PR1 병합 뒤 최신 main에서 만든다. PR1 검증 동안 PR2 계약 준비(읽기)는 한다.
- R-7: 그물 변경은 「보호 집합」 범주에 닿지만 메인 결정(`msg_53e10a4d10c3`)으로 비적용이다. 이전 goal의 판단(`msg_bc4cea4b161f` 1항)과 같다.
- 규칙 문서 bytes: PR1은 ORCA 그물 문장을 고쳐 써 증가 0이 목표다. PR2의 증가분(AGENTS 태그 구절·R-1 문장·PR 검수 문장)은 ORCA 「병합 관문」 첫 문단의 지난 적용 기록 두 문장(118 + 201 bytes)을 이 goal로 옮겨 상쇄한다. 부족하면 실사 전에 메인과 정한다.

### 위험

1. 구현 중 hook이 이 checkout에 바로 실린다. Sol이 rules-active에서 판정 파일을 고치는 동안 리드·검증자 pane의 hook이 고친 코드를 쓴다(이전 goal에서 세 번, 재시작 없이 반영 관측).
2. 판정 파일에 문법 오류가 생기면 진입 스크립트가 정적 import에서 죽는다. Claude Code는 exit 2가 아닌 hook 오류를 차단으로 보지 않으므로 그동안 hook이 열린다[추론, 미측정]. 대비: Sol 계약에 「고칠 때마다 시험을 바로 돌려 깨진 상태를 남기지 않는다」와 「그동안 병합·push 형태를 실행하지 않는다」를 넣는다(메인 지시 `msg_53e10a4d10c3`). 메인 checkout은 병합 전까지 영향이 없다.
3. 그물은 문자열 판정이라 의도적 우회를 다 막지 못한다. 이 한계는 ORCA 「병합 관문」에 있고 이번 변경으로 없어지지 않는다. 막힘 유지 표는 「지금 막히는 것은 계속 막힌다」만 보장한다.

### 오탐 집계(범위 근거)

리드가 로컬 대화 기록에서 hook 자신의 차단 결과만 셌다(2026-10-06T15:00Z 이후, 하위 에이전트 기록 포함). 스크립트와 원시는 E/census/다.

- 실제 차단 34건 중 그물(`suspect-words`) 12건이다. 2건은 10-06 검증자의 의도적 탐침이고 10건이 10-07 일상 명령 오탐이다(Rules 리드 5, 메인 3, Management 리드 2).
- 10건 중 6건은 따옴표 구분자 heredoc 본문(받는 명령 `cat >`·`cat >>`·`git commit -F -`), 4건은 `echo`·`printf`·`node` 스크립트 인자의 따옴표 문자열이었다. 셸로 간 heredoc은 없었다.
- 근사 분석(heredoc 본문 제외, 따옴표 문자열은 `$(...)` 부분만 코드로 남김)으로 10건이 모두 풀리고 탐침 2건은 계속 막힌다. 근사라서 v3과 선행 시험에서 다시 확인한다.
- **v3.1 정정**: 같은 명령에 실행기가 있으면 가리지 않는다(아래 메인 결정). 그래서 **7건이 풀리고 3건은 계속 막힌다**. 막히는 3건은 05:44:15(heredoc 뒤 `node -e`), 08:38:19(`node board.mjs` 인자와 같은 명령의 `gh pr view`), 12:38:52(heredoc 뒤 `node --check`)다. 리드 근사는 E/census/interp-trigger-check.txt다.
- 「7건 풀림」은 원문이 막힌 실제 cwd(feature branch 저장소) 기준이다. 저장소가 아닌 cwd에서는 05:52:41이 refspec 없는 push라 `branch-lookup-failed`가 된다. 원문 harness는 두 cwd를 모두 기록한다.
- 집계 뒤 추가 오탐 1건: 13:3xZ 리드가 수신 대조 기록을 따옴표 구분자 heredoc으로 쓰고 같은 명령에서 우편함 송신을 하다 그물에 막혔다. 실행기가 없어 v3.1에서 풀리는 형태다.

## 요구사항 원천과 적용 결정

메인이 전달한 사용자 결정은 사용자 직접 입력과 구분한다.

- 진입 지시: 메인 `msg_a944d3b84d3b`(2026-10-07T12:40:09Z, E/session/entry-check-msg_a944d3b84d3b.json). 씨앗 파일은 메인 개인 도구의 `plans/rules-hook-followup-seed.md`다.
- 범위 초안 v1: 리드 `msg_15f282e3eec6`(12:55:32Z, 사본 E/scope-draft-v1.md).
- 메인 결정 `msg_53e10a4d10c3`(12:57:04Z, E/session/wait1.raw.txt, 사용자 결정 아님): 질문 3 A(새 스킬은 bytes 묶음 밖), R-7 비적용, Z1을 `contract-context-check` 근거에 넣음, CLAUDE.md는 질문 2 A일 때 예외 한 줄만, PR2 실사는 시범 배정, 위험 2의 계약 문장 두 개는 PR1 계약에 필수.
- 리드 설계 결정 둘(리드 `msg_4d4ba3803397`, 13:21:25Z): 1은 같은 명령 안 쓰기·실행을 받아들이는 한계로 두자는 안, 2는 저장소 시험에 원문 대신 구조 보존 축소형을 넣고 원문 10건은 근거 폴더 harness로 진입 스크립트에 넣는다는 안이다. 저장소가 공개라 원문의 개인 경로·내부 메모를 넣지 않는다.
- 메인 결정 `msg_4e602fb57806`(13:22:13Z, E/session/wait4.raw.txt, 사용자 결정 아님): 1은 대안 「같은 명령에 셸·인터프리터 실행이 있으면 가리지 않음」으로 바꾼다(쓴 경로 추적 없이 넓은 쪽). 2는 동의. 리드가 동작 계약 v3.1(SHA256 `3e6a358f…`)과 선행 시험 계약 보충 v1.1(`msg_ec2d7bef05d9`)로 옮겼다. `node`가 실행기가 되면서 가리는 자리 「스크립트 파일 경로를 받은 node의 뒤 인자」는 쓸모가 없어 뺐다. 승인 범위 기본안에 「목록은 v3에서 확정한다」가 있다.
- 메인 동의 `msg_589e4883d721`(13:33:12Z, E/session/wait6.raw.txt): v3.1과 「7건 풀림, 3건 유지」 정정을 승인 범위 안의 확정으로 본다. 08:38:19의 대가를 받아들이고, 쓰기와 실행이 함께 있을 때만 끄는 좁은 규칙은 만들지 않는다.
- 선행 시험 질문 `msg_ad14f6e585f4`(13:39:07Z)과 리드 답 `msg_acb60f8728de`: 원문 harness는 저장소 아닌 임시 폴더와 feature branch 임시 저장소 두 cwd를 모두 기록한다.
- 메인 결정 `msg_93a8d89ec8b1`(15:05:53Z, E/session/wait15.raw.txt, 사용자 결정 아님, 메인이 사용자에게 보고함): 리드 질문 `msg_bacd2d2a6a07`의 **O1 받음**. 치환 결과가 명령 이름이 되는 꼴과 가린 파일을 같은 명령의 gh api가 읽는 꼴은 실행되는 글이라 원문으로 판정한다(동작 계약 v3.2). 근거는 승인 범위의 두 원칙과 메인 결정 `msg_4e602fb57806`의 연장이며 막는 쪽으로만 바뀐다. 조건은 둘이다. 원문 harness에서 「풀림 7건」 중 다시 막히는 것이 생기면 구현 전에 메인에 알린다. 확정 실패는 구현 계약 기준 V1·V2 1회로 센다.

## 적용 중인 사용자 결정

정본 반영 전까지 적용하는 결정이다. 모두 메인 전달이며 직접 입력으로 격상하지 않는다. 씨앗 원문은 메인 개인 도구의 씨앗 파일에 있다.

- **범위 승인**: 메인 `msg_9dc312f58c0f`(2026-10-07T13:00:01Z, E/session/wait2.raw.txt)가 전달한 사용자 원문은 **「대시보드 결정 응답: 1) 계획 검토 - Rules hook 차단 줄이기·보조 세션 스킬 goal → A 승인 (초안 msg_15f282e3eec6), 코멘트 「A 방향으로 가는 거 찬성, 근데 일단 계획만 일단 확정하고, 작업 잠시 대기」」**다. 한 제출에 코멘트 없는 같은 문장이 앞에 붙어 있었고 메인은 코멘트가 붙은 뒤 문장을 최종 답으로 읽었다. 질문 1·2는 추천대로 A다. 이 승인이 이전 goal Gardener 정리 후보 ①(판정 형태 표) 채택의 출처다.
- **작업 재개**: 메인 `msg_fac5ebe5cd1c`(13:10:53Z, E/session/wait3.raw.txt)가 전달한 원문은 **「대시보드 결정 응답: 1) Rules 재개 - Herdr 이주를 보류했으니 hook 정리 goal 작업을 지금 시작할지 → A 지금 재개」**다. 대기 사유였던 Orca→Herdr 이주는 「참고만, 전면 이주 보류」로 정해졌고 이 goal 범위 밖이다.
- **범위의 출발**(씨앗): 「음 계속 hook 때문에 가로막히는 일이 생길 거 같은데 한번 수정해보자」.
- **PR 생성과 검수**(씨앗): 「아니면 PR 생성은 어차피 리드급이 작성하고 메인 세션에 보낼테니까 괜찮은데, Merge만 우리가 PR 내용 검수하고 같이 하는거로 해도 괜찮아」. 사용자가 전역 ask에서 PR 생성 줄을 직접 지웠다(「OK 전역설정 hook 고쳤어」). PR2 반영 예정.
- **보조 세션 진입 스킬**(씨앗): 「대시보드 결정 응답: 1) 보조 세션 진입 스킬 - 다음 Rules goal에 넣을지, 몇 번 더 써 본 뒤 넣을지 → A 다음 Rules goal에 넣음」. PR2 반영 예정.
- **Core·Content 재개 시점**(씨앗): 「대시보드 결정 응답: 1) Core·Content 재개 시점 - 최소 운영 셋업 몇 단계 뒤에 게임 goal을 다시 열지 → A 3단계 뒤 재개」. 이 goal과 최소 운영 셋업 2·3단계가 게임 goal 재개의 선행 조건이다.
- **현황판 서브에이전트 쓰기 예외의 반복**(씨앗): 이전 goal의 첫 예외(보조 세션 표시) 뒤 10-07에 세 번 더 있었다. 두 번째는 표·코드 블록 터미널 렌더(18:0x KST), 세 번째는 mod 확인 실험(21:21), 네 번째는 「2) 병합 승인 방식 - 타이핑 대신 대시보드가 승인 줄을 채울지, 모달로 바꿀지 → A 대시보드가 승인 줄 채움」(21:26)이다. 네 번 모두 「메인 개인 도구 폴더 한정 + 사용자 결정마다」 모양이었다. 반복 규칙으로 올리는 것은 사용자 질문 2 A이며 PR2에서 CLAUDE.md(메인)에 반영한다.
- **현황판의 승인 줄 채우기**: ORCA에는 적지 않는다(리드 판단, 범위 승인에 포함). ORCA 「병합 관문」이 이미 「사용자가 메인 창에 Enter로 제출」을 기준으로 삼고 입력 출처를 구분하지 못한다는 한계를 적어 두었다. 채우기는 메인 개인 도구의 동작이다.
- **Management 종료 점검 1A·2A**: Management `backlog-management-closeout-handoff.md`가 전달한 원문(메인 `msg_02077b4a4df3`)은 「대시보드 결정 응답: 1) Management 종료 점검 1 - 운영툴 전 화면 글자 대비 장치를 어디에 둘지 → A BACKLOG 대기 행 · 2) Management 종료 점검 2 - Gardener 후보(맥락 메모 선행 판정 helper) → A 기존 BACKLOG 행에 근거 연결 · …」이다. 원문 사본은 이전 goal 근거 폴더에 있다. PR2의 BACKLOG 첫 변경 근거다.
- **보조 세션의 상시 「보조 메인」 승격**: 메인 `msg_b279e2d2759e`(14:00:40Z, E/session/wait9.raw.txt)가 전달한 원문은 **「보조 메인 세션을 몇번 돌려보니까 엄청 편하고 좋은데, 현황판에 정식으로 승격해줘, 지금은 규칙점검 Opus로 되어있는데 / 이름 바꿔줘」**다. 메인은 현황판 표시를 「보조 메인 Opus」로 고쳤고 보조 pane에 태그 `[보조 메인 Opus]`를 쓰라고 보냈다. PR2 반영: 보조 세션 진입 스킬과 AGENTS 태그 구절의 예시를 `[보조 메인 Opus]`로 바꾸고, 메인 옆에 상시로 두는 보조가 있을 수 있다는 한 줄을 넣는다. 형식 `[<용도> <모델>]`과 「태그는 권한이 아님」은 그대로다. 결정 범위는 이름과 상시 자리뿐이다. 상시 보조에 새 권한·절차가 필요해 보이면 넣지 않고 메인에 묻는다.
- **작업자 하나에 받침 pane을 만들지 않음**(정본 반영 전 적용 중): 메인 `msg_e62c4989b3dc`(14:23:02Z, E/session/wait11.raw.txt)가 전달한 원문은 **「음 아마 우리 규칙에서 문장상 모호한게 있었나보네, 창 하나를 띄울때는 굳이 Pane을 하나 더 만들어서 Agent를 할당할 필요는 없어」**다. 계기는 unity-upgrade-active 탭의 빈 「Terminal 1」 받침 pane이다. 메인 해석: 같은 worktree면 리드 pane split, 다른 worktree면 `orca terminal create --worktree <선택자> --command <최초 실행 명령>`으로 바로 연다. 정본(ORCA R-1·R-5 2단계) 문장 정리는 승인된 PR2 범위 밖이라 다음 Rules goal 후보로 둔다(아래 「다음 계획 후보」). 이 goal은 빈 pane을 만들지 않았다. Sol은 정산된 선행 시험 pane에서 split했다.
- **Advisor 모델 대안**(정본 반영 전 적용 중): 메인 `msg_87c0fed69b04`(13:30:23Z)·`msg_6439bbd9ef9b`(13:32:07Z, E/session/wait5.raw.txt)가 전달한 원문은 **「음 만약에 Advisor가 필요하면 Astra로 체크해보는거도 방법이야」**와 확인 **「OK, Advisor로 Fable 5.1도 있지만 GPT 6 Astra도 가능한점을 고려해줘」**다. 해석(사용자 확인으로 확정): 확정 실패 3회 뒤 Advisor는 신규 읽기 전용 `claude-fable-5-1`이 기본이고, 리드가 신규 읽기 전용 `gpt-6-astra` xhigh를 대안으로 고를 수 있다. 고르면 이유를 수행 기록에 남긴다. 기동 조건·기동 전 메인 보고·조언 파일 하나·새 Sol의 채택 기록·R-5·R-6은 그대로다. ORCA·AGENTS 반영은 이 goal 범위 밖이며 메인 씨앗 파일에 정본 반영 후보로 있다.

## 현재 결과

### PR1 선행 시험

- 작성: 신규 `claude-opus-5-5`(화면 「Opus 5.5 with xhigh effort」, backend unknown), Task `task_91bc8b29a7ca`, Dispatch `ctx_38c14a6a0db5`, 계약 E/tdd-contract.md(`3ec3a43b…`)와 보충 v1.1(`382d8fa9…`). 보고서 E/tdd/report.md.
- 새 시험 `99_Tools/MergeGate.Tests/text-masking.test.mjs`(135개)와 fixture `monitorInput` 추가, 기존 단정 세 곳 변경(`blocked-commands` 1, `suspect-words` 2, 전수 분류 표는 보고서 6절).
- 구현 전 실행: 261개 중 215 통과, 46 실패. 실패는 통과 묶음 42(실행기 없는 축소형 7 포함), T1 2, 기대를 바꾼 기존 시험 2뿐이다. 막힘 유지 91(실행기 있는 축소형 3 포함)과 나머지 기존 시험 124는 통과다.
- 원문 harness(E/tdd/census-harness.mjs): 두 cwd에서 10행 모두 `suspect-words`다. 구현 뒤 기대는 임시 폴더가 결정 없음 6·`branch-lookup-failed` 1·`suspect-words` 3, feature branch 저장소가 결정 없음 7·`suspect-words` 3이다.
- 리드 R-2 대조(E/lead-check/tdd-r2-check.md): 같은 명령 재실행 결과와 실패 시험 이름 집합, harness 결과가 작업자 원시와 같다. 축소형 하나를 원문과 대조했다.
- 작업자 자진 보고 경계 이탈 셋(비차단): 사용자 TEMP(`/tmp`) 쓰기 1건을 같은 명령에서 지움, Claude Code가 큰 셸 출력을 홈의 tool-results에 자동 저장(원문 포함, 저장소 밖), 맥락 메모 전 분석 명령 하나가 그물에 막힘(우회 없음). 첫 발생이라 반복 규칙은 만들지 않는다.
- 작업자 pane은 정산 뒤 열어 둔다. 리드 pane을 직접 split한 pane이라, Orca 1.4.222에서 split을 닫을 때 부모가 꺼진 사례 때문에 Sol 완료 직후 Sol pane과 함께 닫는다(사용자 직접 지시와 확인, E/session/user-cleanup-request.md). Sol은 이 pane을 부모로 split했고 빈 받침 pane은 만들지 않았다.

### PR1 구현

- 작성: `gpt-6.1-sol` max(화면 「GPT-6.1-Sol max · Full Access」, Codex v0.160.1, backend unknown), Task `task_9074722499fc`, Dispatch `ctx_ec72292449d1`, 계약 E/impl-contract.md(`44cafa82…`), 보고서 E/impl/report.md. worker-start가 `turn_start_unobserved`라 ORCA 「공식 계약 draft 복구」대로 Enter 한 번을 보냈고 Codex 세션 기록에서 계약 전문 제출을 대조했다(E/impl-draft-recovery.md).
- 새 순수 모듈 `99_Tools/MergeGate/inert-text.mjs`(345행), `merge-policy.mjs` 연결·T1·주석 후속, README 「병합 관문」, ORCA 233·234행.
- 질문 `msg_9a71b59d4f3c`와 리드 답 `msg_3e7decaf50bb`: `=` 형태는 v3.1 문장대로 가린다. git은 `--message=`만, gh는 네 옵션의 `=` 형태, orca는 별도 값만이다.
- 결과: MergeGate 261/261, Orca 22/22. 원문 harness는 저장소 아닌 폴더가 결정 없음 6·`branch-lookup-failed` 1·`suspect-words` 3, feature branch 저장소가 결정 없음 7·`suspect-words` 3으로 기대와 10/10 같다. 위험 2 이력은 편집마다 바로 시험했고 실패 상태로 넘어간 구간이 없다.
- 규칙 문서 bytes: 여섯 파일 102,658 → 102,645(ORCA 41,958 → 41,945, 250줄).
- 리드 R-2(E/lead-check/impl-r2-check.md): 재실행 수와 harness, 파일 hash가 Sol 보고와 같다. 문서·주석 표본 관찰 넷은 독립 검증 입력으로 넘긴다.
- 커밋(push 전): `1e061c70` 주석만(구조), `26d1ba4b` 동작. 커밋된 파일 hash가 Sol 최종과 같다.
- Sol pane은 14:2xZ에 닫았다. 부모인 선행 시험 pane은 꺼지지 않았다(1.4.222 부모 꺼짐 현상이 이번에는 재현되지 않음).

### PR1 독립 검증(첫 회, FAIL)

- 검증: 신규 `claude-opus-5-5`(화면 「Opus 5.5 with xhigh effort」, backend unknown), Task `task_695c7f0024ae`, Dispatch `ctx_b6990132173c`, 계약 E/verify-contract.md(`e2d4b02d…`), 판정 E/verify/verdict.md, worker_done `msg_74d6ce1f851d`(14:57:23Z).
- 기대대로인 것: MergeGate 261/261, Orca 22/22, 구현 전 사본 215/46, 원문 harness 두 cwd 10/10, 실제 세션 확인 두 번, 문서 bytes 102,645·ORCA 250줄·링크.
- 차단 V1: 가리기 읽기가 몇몇 따옴표·공백 꼴을 bash와 다르게 읽어, 구현 전에 막히던 병합·push 꼴이 결정 없음이 된다. 차단 V2: 묶음 `( … )` 뒤의 파이프를 보지 못해 같은 일이 생긴다. 귀속은 둘 다 이번 Sol이다.
- 비차단: V4(선행 시험 10:55:27 축소형 본문에 원문의 `node`·`gh` 낱말 누락), V5(README에서 정밀 판정의 따옴표 묶음 설명 누락), V6(O-T3 주석의 이유 누락). 설계 관찰 O1은 위 메인 결정으로 받았다. O2는 이번 수정에서 「main에서」로 되돌린다(+3 bytes). O3·O5는 bytes 여유(13) 때문에 하지 않는다. O6은 아래 「다음 계획 후보」로 둔다.
- 리드 R-2(E/lead-check/verify-r2-check.md): 검증자 스크립트를 쓰지 않고 따로 쓴 스크립트(verify-r2-repro.mjs)로 V1 5행·V2 3행을 구현 전후 hook과 Git Bash 무해 표식으로 재현했다. 판정과 같다. V4·V5·V6도 원천에서 확인했다.
- 확정 실패 집계: 구현 계약 기준 V1·V2 1회. 같은 산출물(inert-text.mjs) 수정은 이번이 첫 회다.
- 검증자 pane은 정산 뒤 15:01:41Z에 닫았다(사용자 직접 지시 범위). 부모인 리드 pane은 살아 있다.

## 다음 계획 후보

- `merge-gate-code-followup`의 O2(비ASCII 공백)·O6(이상한 session_id): 이 goal 밖이다. 종료 때 미해결이면 BACKLOG 새 행으로 등록한다.
- 작업자 pane 배치 문장 정리(ORCA R-1·R-5 2단계): 같은 worktree는 리드 pane split, 다른 worktree는 `terminal create --worktree`로 바로 열고 빈 받침 pane을 만들지 않는다(사용자 결정 `msg_e62c4989b3dc`). split close 때 부모 pane이 꺼지는 Orca 1.4.222 관측과 함께 다룬다. 종료 때 BACKLOG 새 행으로 등록한다.
