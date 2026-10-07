# hook 차단 줄이기와 보조 세션 스킬

## 재개 지점

Rules의 목표다. 사용자가 범위 초안 v1을 승인했고(아래 「요구사항 원천과 적용 결정」) 재개를 지시했다. 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch: PR2는 `docs/hook-helper-session-canon-20261008`(base 최신 main `7086d45b`)이고 이 goal 문서의 현행판이 여기에 있다. PR1 `fix/hook-net-false-positives-20261007`(`13fe344d`, 원격 보존)은 사용자 결정으로 멈췄고 분리 시험 후속 goal로 넘긴다.
- 근거 폴더 E: `.backups/verification/2026-10-07-hook-friction-helper-session/`(Git 제외). 리드 맥락 메모는 [astra-context.md](../../../.backups/verification/2026-10-07-hook-friction-helper-session/astra-context.md), 승인 확정본은 [scope-approved.md](../../../.backups/verification/2026-10-07-hook-friction-helper-session/scope-approved.md)(SHA256 `0e536cac…`)다. 받은 메시지 원시와 수신 대조는 E/session/에 있다.
- 리드: 신규 `claude-opus-5-5` xhigh, 태그 `[Rules Astra]`, handle `term_ad29f105-dff5-4f1b-9411-372b93adf7ea`. Run은 `run_573214a00f1b`, 회신 주소는 `run:run_573214a00f1b`다. 이전 Rules goal의 Run·Task·Dispatch는 실행 권한이 아니다.
- **현재 위치**(2026-10-07T19:0xZ): PR1은 멈추고 보존했다. 자격 증명 분리 시험의 밤사이 범위를 마쳤다(아래 「자격 증명 분리 시험」). 사용자 결정으로 이 goal은 PR2로 마치며 PR2를 진행한다(「적용 중인 사용자 결정」의 「이 goal은 PR2로 마침」).
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

사용자 결정(`msg_1108186019ec`)으로 아래 1~4와 7·8의 PR1 몫은 분리 시험 후속 goal로 넘긴다. 이 goal은 5·6·8과 PR2 병합으로 마친다.

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
| 1 - 그물 오탐 줄이기(멈춤, 분리 시험 후속 goal로 넘김) | 판정 코드·시험·도구 README·ORCA 그물 문장·goal·CURRENT | 강: 보안 경계와 실행 도구. 선행 시험은 신규 Opus(시범 대상 아님), 구현은 Sol max, 독립 검증은 신규 Opus(시범의 보안 경계 배정) |
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
- **safeguard 차단 시 검증자 대체**(정본 반영 전 적용 중): 메인 `msg_1aa6aa9fa263`(16:40:00Z, E/session/check31.raw.txt)이 전달한 원문은 **「만약 또 SafeGuard로 막히면 Astra로 대체해서 진행해줘」**다. 메인 해석은 다음과 같다.
  - 조건은 Opus 검증자에게 「Opus 5.5's safeguards flagged this session」 선택창이 다시 뜨는 것이다. 창에서는 고르지 않는다. 「Switch automatically」(Opus 4.8 전환)는 계속 금지이고, 이 대체는 실행 중 모델 변경이 아니라 세션 교체다.
  - Opus 검증자는 산출물을 근거 폴더에 둔 채 정산·종료하고 막힌 시각과 화면 문구를 남긴다.
  - 같은 계약으로 신규 `gpt-6-astra` xhigh 검증자를 새 Task·Dispatch로 열고 R-5·R-6을 따른다. 판정은 새 검증자가 독립으로 다시 한다. Opus 부분 산출물을 참고 입력으로 줄지는 리드가 정해 계약에 적는다.
  - 대체하면 메인에 status 한 통(막힌 시각, 새 handle, 모델 표시)을 보낸다.
- **실행 시점 검사 안 먼저 검토**: 메인 `msg_882e45455690`(17:19:05Z, E/session/check47.raw.txt)이 전달한 사용자 원문 셋이다(메인 창 Enter 제출, 메인 전달).
  - 계기 질문: **「그러면 검증 조건에 Read랑 Write 툴은 제외하고, 나머지 실행관련된 Tool을 Call할때 직전에 검증하는 방식으로 그물망 구현하는건 안되나?」**
  - 판단 기준: **「흠 확실히 좀 까다로운 문제네, 검증기를 통해서 솔직히 명령어 텍스트를 검사하는건 문제가 안되는데, 얼마나 속도적 병목이 없는지가 핵심인거 같아」**
  - 결정: **「대시보드 결정 응답: 1) Rules PR1 - 다음 수정 회차 전에 「실제 실행 시점 검사」 안을 리드가 먼저 검토할지 → A 검토 먼저」**
  - 메인 지시: 다음 수정 회차는 이 검토 뒤 사용자 결정으로 시작한다. 비교 대상은 A안 실행 시점 검사(git pre-push hook, gh 래퍼, 글자 그물 축소)와 B안 지금 방향의 가리기 축소다. 리드 v3.3 계획(`msg_ef093e2120a8`)은 B안으로 쓴다(`msg_278a7130befd`). 검토는 읽기 전용이며 산출은 근거 폴더 비교 문서와 메인 결정 요청 한 통이다.
  - 리드 결정 요청 `msg_eac0fa1f3d89`(「B 먼저」 추천, E/runtime-check-comparison.md) 뒤 메인 `msg_72a64f4bed5a`(18:04:16Z, E/session/check49.raw.txt)가 전달한 원문은 **「대시보드 결정 응답: 1) Rules PR1 - 가리기 수정 한 회차 더(B)냐, PR1 멈추고 자격 증명 분리 시험(C)이냐 → C 코멘트 「일단 이건 더 논의 해보자」」**다. 「C 코멘트」는 현황판 의견 칸이며 메인 지시대로 C 선택이 아니라 미결·논의 중으로 읽었다. 아래 결정으로 닫혔다.
- **PR1 멈춤과 자격 증명 분리 시험**: 메인 `msg_0e7cb4fe6f52`(18:19:23Z, E/session/check49.raw.txt)이 전달한 원문은 **「대시보드 결정 응답: 1) Rules PR1 - 병합 통제를 「대화형 유지 + 자격 증명 분리」로 바꾸는 시험을 할지 → A 대화형 유지 분리 시험」**이다(메인 창 Enter 제출, 메인 전달). 위 「미결·논의 중」은 이 결정으로 닫는다.
  - 사용자에게 보인 A: PR1 수정은 멈추고 branch는 보존한다. 에이전트가 병합하지 못하는 좁은 토큰과 push용 deploy key로 바꾸는 시험을 먼저 한다. 통과하면 글자 그물·가리기 코드를 걷는 새 Rules goal로 가고, 병합은 지금처럼 승인 한 줄을 치면 메인이 한다. 고르지 않은 B는 가리기 수정 한 회차로 PR1을 병합하는 안이었다.
  - 메인 지시: v3.3과 선행 시험은 열지 않는다. branch `fix/hook-net-false-positives-20261007`(head `ed320419`, GitHub PR 없음)는 삭제·force push·rebase 없이 보존한다. 지금 hook과 그물은 시험 동안 그대로 둔다. 리드는 자격 증명 분리 시험 계획 초안을 메인에 status로 보내고, GitHub 설정·자격 증명·원격을 건드리는 실행은 초안 승인 뒤에만 한다. main을 직접 대상으로 하는 push·병합 시도는 넣지 않고, 토큰 값·개인 키는 메일·goal·로그·화면·저장소에 남기지 않는다.
- **자격 증명 분리 시험 밤사이 시작과 범위 추가**: 메인 `msg_fbbb07fa38a9`(18:39:35Z, E/session/check52.raw.txt)이 전달한 원문은 **「A로 진행하자」**다(메인 창 Enter 제출, 메인 전달). 질문은 현황판 rules-overnight-test-start 「Rules 분리 시험 - 계획 초안을 메인 대조로 승인해 밤사이 시작할지」, A 원문은 「초안을 메인 수정대로 승인하고 지금 Rules goal에 범위를 더해 밤사이 시작한다. 밤사이엔 deploy key push·PR 생성·세션 싣기·넘어감 확인만, 시험 ruleset·병합 403 시도·정리는 아침에 네 손으로.」다. 앞 결정은 위 `msg_0e7cb4fe6f52`의 A다.
  - 계획: v1 E/credential-trial-plan-draft.md(`561a6664…`) + v2 차이 E/credential-trial-plan-v2-diff.md(`4f83f6cc…`). 메인 대조 `msg_af15c0a64df7`. 사용자 손 단계(deploy key·좁은 토큰·토큰 파일)는 메인 확인 `msg_f1bbd87c7176`.
  - **범위 추가**: 이 결정으로 아래 「하지 않을 것」 중 「Codex 세션 병합 차단, 에이전트 계정·ruleset, 전역 Claude·Codex·Git·gh 설정 변경」을 이 시험에 한해 덮는다. 더한 범위는 계획 v1의 범위 표와 v2 차이다. 만들 것은 시험 기록·사용자 손 안내·리드 시험 스크립트·결과 보고와 후속 goal 입력이고, 건드릴 곳은 GitHub의 좁은 토큰·deploy key·시험 ruleset(아침, 사용자 승인)·`trial/cred-*` branch와 시험 PR, 로컬의 `%USERPROFILE%\.dawnholder-agent\`·rules-active `config.worktree`·일회 pane이다. main 대상 push·병합, main ruleset 변경, 그물·가리기 코드 변경, 정본 변경은 계속 하지 않는다.
  - 근거 폴더는 E/credential-trial/로 나눈다. 비밀 값은 쓰지 않는다.
- **이 goal은 PR2로 마침**: 메인 `msg_1108186019ec`(18:44:30Z, E/session/check53.raw.txt)이 전달한 원문은 **「대시보드 결정 응답: 1) Rules hook 정리 - 분리 시험 뒤 PR2(문서)까지 밤사이 진행하고 이 goal을 PR2로 마칠지 → A 시험 뒤 PR2까지」**다(메인 창 Enter 제출, 메인 전달). 사용자에게 보인 A는 「분리 시험이 끝나면 리드가 PR2(문서)를 최신 main에서 진행해 PR까지 올린다. 이 goal은 PR2 병합으로 마치고, PR1 몫은 분리 시험 후속 goal이 맡는다. PR2 병합은 아침 네 승인.」이고, 현황판 상세에 「이 goal 완료조건에서 PR1을 빼고 분리 시험 후속 goal로 넘긴다」, 「병합 관문의 그물 설명처럼 분리 방향과 겹치는 문장은 PR2에서 빼고 후속 goal로 넘긴다」를 함께 적었다.
  - 적용: 아래 「관찰 가능한 완료조건」과 「PR 경계와 검증」의 PR1 몫은 분리 시험 후속 goal로 넘긴다. PR1 branch `fix/hook-net-false-positives-20261007`(`13fe344d`)는 보존한다. PR2는 최신 main `7086d45b`에서 만든 `docs/hook-helper-session-canon-20261008`이고, 이 goal 문서는 PR1 branch `13fe344d`의 내용을 그대로 옮겨 이어 쓴다(커밋 `97ac25ba`). 메인 지시대로 PR2 범위는 승인 v1이다. 분리 방향과 겹쳐 뺀 문장은 아래 「다음 계획 후보」의 후속 goal 입력에 적는다.
  - 오늘 밤 멈출 지점은 PR2를 올리고 독립 검증을 마친 뒤 병합 승인 요청을 보낸 지점이다. 사용자 원문은 「OK 전 파트 완료되면, 작업 결과 및 재개시점 맥락 최신화 해달라고하고, 리드급 세션들 다 닫고, 메인 세션만 남기고 메일함 자동갱신은 꺼줘」다(메인 `msg_a82d8297c361`).

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

### PR1 수정 회차

- 동작 계약 v3.2(E/merge-gate-behavior-spec-v3.2.md, SHA256 `69d3b1c9…`). 막는 쪽으로만 바뀐다. V1은 「bash와 다르게 읽을 수 있는 꼴」(따옴표 밖 `$'`·`$"`, 역따옴표 치환 안의 따옴표·역슬래시, CR, 따옴표·heredoc 본문 밖 특수 공백류)을 읽기 실패로 둔다. V2는 「묶음 뒤 파이프」를 묶음 안 모든 명령의 파이프로 본다. O1은 「실행기 찾기」 4(명령 낱말에 `$`·역따옴표)·5(gh·api 낱말)다. 문서 후속은 V5·V6·O4·O2다.
- 선행 시험: 신규 `claude-opus-5-5`(화면 「Opus 5.5 with xhigh effort」, backend unknown), Task `task_15f6e834cd37`, Dispatch `ctx_3ef4fbe18b53`, 계약 E/tdd2-contract.md(`642d50c4…`), 보고서 E/tdd2/report.md, worker_done `msg_da5ab53b69e2`. `text-masking.test.mjs`에 45개(막힘 유지 35, 대조 10)를 더하고 V4 본문을 고쳤다. 수정 전 306개 중 271 통과·35 실패이며 실패는 새 막힘 유지 35개뿐이다. 원문 harness는 v3.2 기대와 두 cwd 10/10이며 「풀림 7건」은 유지된다(메인 조건 충족).
- 리드 R-2(E/lead-check/tdd2-r2-check.md): 같은 명령 재실행 수와 실패 이름 집합, harness 결과가 작업자 원시와 같다. 시험 파일에 개인 경로 0건이다. 비차단 관찰: 시험 파일 473·474·525행의 U+00A0·U+3000 실제 글자(작업자가 주석 표시), 작업자의 TEMP scratchpad receipt 쓰기(아래 「다음 계획 후보」).
- 커밋(push 전): `b9fb4bef` 시험. 선행 시험 pane은 15:41:48Z에 닫았다.
- 수정: 신규 `gpt-6.1-sol` max(화면 「GPT-6.1-Sol max · Full Access」, Codex v0.160.1, backend unknown), Task `task_8ad06eae8c39`, Dispatch `ctx_d3e40f51ae3f`, 계약 E/impl2-contract.md(`04b38e5d…`, 위험 2 문장 포함). worker-start가 `turn_start_unobserved`라 「공식 계약 draft 복구」대로 Enter 한 번을 보냈고 Codex 세션 기록에서 계약 전문 제출을 대조했다(E/impl2-draft-recovery.md).
- 수정 결과(worker_done `msg_1d6bcee78d13`, 보고서 E/impl2/report.md): `inert-text.mjs`(V1·V2·O1, O4 주석), `merge-policy.mjs` 245행 주석(V6), README 「병합 관문」(V5, v3.2 설명), ORCA 233행(O2). MergeGate 306/306, Orca 22/22, 원문 harness 두 cwd 10/10(「풀림 7건」 유지), 리드 재현 V행 after = before. 여섯 문서 합 102,648. v3.2 최소 목록 밖에서 `$((` 산술 확장과 여는 괄호 앞 명령·대입 낱말도 읽기 실패로 뒀다(막는 쪽).
- 리드 R-2(E/lead-check/impl2-r2-check.md): 네 파일 hash, 시험 수, harness, 재현 결과가 Sol 보고와 같다. 보호 파일 diff 없음. 비차단 관찰: Sol이 작업 36분 동안 heartbeat를 보내지 않았다.
- 커밋(push 전): `d2fb23f0` O-T3 주석만(구조), `be093211` 동작과 문서. inert-text의 주석 변경은 바뀐 읽기를 설명하고 동작 hunk와 섞여 있어 동작 커밋에 함께 뒀다(커밋 메시지에 적음). 커밋 파일 hash가 Sol 최종과 같다. Sol pane은 16:22:18Z에 닫았고 리드 pane은 그대로다.

### PR1 재검증(수정 회차, FAIL)

- Opus 재검증: 신규 `claude-opus-5-5`, Task `task_4e391a7fdd7c`, Dispatch `ctx_79d41b550e17`, 계약 E/verify2-contract.md(`c9603d29…`).
  - 16:3xZ에 탐침 표를 쓰던 중 「Model switch」 선택창(「Opus 5.5's safeguards flagged this session … Switch to Opus 4.8 …?」)에서 멈췄다. 메인이 고르지 않고 Esc로 취소했고(`msg_56cf0ff441de`) 리드가 재개 dispatch `msg_06a6e4baa29e`를 보냈다.
  - 16:50:37Z에 선택창이 다시 떠 위 「safeguard 차단 시 검증자 대체」대로 바꿨다(메인 지시 `msg_47708ee158ad`). Dispatch는 failed/operator_close(16:50:56Z)다. 판정이 없어 확정 실패 집계에서 뺀다.
  - 산출물 34개는 E/verify2/에 두고 새 검증자에게 주지 않았다(정산 E/verify2-opus-settlement.md).
- 대체 재검증: 신규 `gpt-6-astra` xhigh(화면 「GPT-6-Astra xhigh · Full Access」, Codex v0.160.1, backend unknown), Task `task_a5f6d858bd2d`, Dispatch `ctx_28f276dccc0c`, spec E/verify2a-spec.md(`ef7f4f4a…`, 보충 + 같은 계약). worker-start가 `turn_start_unobserved`라 draft 복구대로 Enter 한 번을 보냈다(E/verify2a-draft-recovery.md). 판정 E/verify2a/verdict.md, worker_done `msg_8810c52a48c7`(17:16:57Z).
- 기대대로인 것: MergeGate 306/306, Orca 22/22, v3.1 제품 사본 271/35(실패 이름이 선행 시험과 같음), 원문 harness 두 cwd 10/10(「풀림 7건」 유지), 첫 검증 지정 D·G 68행이 v2.3과 같은 코드, 일상 T 72행 변화 없음, 문서 bytes 102,648·ORCA 250줄·링크 8/8. V1·V2·V4·V5·V6·O1·O2·O4는 닫혔다. 6b(실제 Claude 세션 확인)는 Codex 세션이라 미실행이며 리드 몫이다.
- 차단 W1: 조건문·반복문(`if … fi`, `for`·`while`·`until … done`) 안의 따옴표 heredoc은 출력이 `fi`·`done` 뒤 파이프로 넘어가도 가려져 결정 없음이 된다(v2.3은 막음). reader가 `()`·`{}` 묶음만 기억해 파이프 표시가 마지막 낱말에만 붙는다. v3.1부터 있던 결함이며 귀속은 impl2 계약(Task `task_8ad06eae8c39`)이다.
- W2(메인 수용·종결, 비차단): 검증자가 escalation `msg_155705daf8e4`로 올렸다.
  - 사실: 수정 회차 선행 시험 작성자(Task `task_15f6e834cd37`)의 맥락 메모 E/tdd2/context.md는 「이 작업의 첫 쓰기」라고 적었다. 보고서 §11.4는 15:12~15:21Z에 orca receipt 다섯 개를 Claude Code scratchpad(TEMP 아래)에 썼다가 15:21:29Z에 근거 폴더로 옮겼다고 자진 보고했다. 메모는 15:20:20Z, 시험 파일 수정은 15:26:34Z다.
  - **정정: 메모의 「첫 쓰기」 주장은 TEMP receipt를 빼고 맞다.**
  - 리드 결정 요청 `msg_8a8efab2d8df`에 대한 메인 결정 `msg_f1b350feddc3`(17:10:20Z, 사용자에게 보고)은 **「W2 수용·종결」**이다. 근거는 메모 규칙의 목적(대상 파일을 쓰기 전에 규칙·근거를 남김)이 지켜졌고 시험 내용·결과에 닿지 않았다는 것이다.
  - 조건: 교정은 계약 양식 후보(아래 「다음 계획 후보」)로 남긴다. 이번 목표의 다음 계약에는 「Claude Code scratchpad도 TEMP다, receipt는 처음부터 근거 폴더 아래」를 유지한다. 메인은 「같은 이탈이 다시 나오면 두 번째 발생이라 반복 규칙 대상」이라고 했다.
- 비차단 관찰: Q1은 부분 문법 reader의 유지비다(지원 밖 문법은 원문을 돌려주는 경계를 명시하는 방향). Q2는 시험 473·474·525행의 보이지 않는 공백을 다음 수정 때 ` `·`　`으로 쓰자는 것이다.
- 리드 R-2(E/lead-check/verify2a-r2-check.md): 판정 본문에서 따로 만든 행으로 W1을 실제 hook 진입과 Git Bash 무해 표식으로 재현했다(w1-repro.mjs). 독립 W1 시험 4/4 실패, suite 306/306·22/22, 풀린 378행 분류의 표본 대조가 판정과 같다.
- 확정 실패 집계: impl2 계약의 W1 1회. 같은 산출물(inert-text.mjs) 수정은 다음이 두 번째다.
- 검증자 pane은 17:19:01Z에 닫았다. 리드 pane은 그대로다.
- 리드 수정 계획(메인 보고 `msg_ef093e2120a8`): 동작 계약 v3.3은 「bash와 다르게 읽을 수 있는 꼴」에 명령 자리의 복합 명령 낱말을 더해 읽기 실패로 둔다(막는 쪽). 리드 사전 점검(E/lead-check/w1-fix-option-check.txt, 제품 아님)에서 census 풀림 7건과 일상 T 18행은 그대로였다. 탐침 681개 중에서는 N1~N4가 막히고 if 안 echo 메모 하나(A27)가 v2.3처럼 다시 막혔다.
- 다음: 메인 지시 `msg_278a7130befd`로 v3.3을 보류하고 두 안을 비교해 결정을 요청했다(`msg_eac0fa1f3d89`). 사용자 결정(`msg_0e7cb4fe6f52`)으로 v3.3·두 번째 수정 회차는 열지 않고 PR1을 멈춘다. 확정 실패 집계는 impl2 계약 W1 1회에서 멈춘다.

### 자격 증명 분리 시험

- 계획: v1 E/credential-trial-plan-draft.md + v2 차이 E/credential-trial-plan-v2-diff.md. 근거는 E/credential-trial/이고 결과 정리는 E/credential-trial/results-overnight.md다. 비밀 값은 쓰지 않았다(근거 폴더 토큰 패턴 검색 0건).
- 밤사이(2026-10-07T18:40~18:48Z, main sha 다섯 시점 모두 `7086d45b`):
  - 에이전트 gh 설정: 토큰 파일에서 `config.yml`(version "1")·`hosts.yml`(호스트 단위 토큰)을 만들었다. 출처가 에이전트 `hosts.yml`이고 keyring 항목은 그대로다.
  - ① rules-active `config.worktree`에 SSH push 주소·deploy key 전용 `core.sshCommand`·빈 `credential.helper`를 넣었다. deploy key로 `trial/cred-base`·`trial/cred-work-1` push 성공(SSH 신원은 저장소 이름).
  - ② 좁은 토큰으로 PR #205 생성·조회·코멘트·`gh pr checks` 성공(Checks·Issues 권한 없이).
  - ③ 기동 명령 앞 `GH_CONFIG_DIR`만으로 맨 PowerShell·일회 Codex(`gpt-6-astra` low)·일회 Claude(`claude-haiku-4-5-20251001`)가 모두 에이전트 자격 증명을 받았다.
  - ⑥ 넘어감: 환경을 빠뜨리면 gh는 관리자, 에이전트 설정에서도 사용자 지정 토큰 조회는 keyring 관리자, 에이전트 worktree의 https 직접 push는 막힘, 메인 checkout을 빌린 push는 관리자로 인증(dry-run).
- 아침에 남은 것: 보호 branch 거부(U3 뒤), 병합 세 형태 403(사용자), ⑤ 브라우저, 선택 ④(b)·(라) 시험, ⑦ 정리. 순서는 메인 보고 `msg_27ff509d7f16` 「아침 순서」 1~8이다. 1번(`trial/cred-protected` 만들기)도 사용자 아침 답과 묶어 그때 한다(메인 `msg_1dfa956a845b`).
- 메인 대조(`msg_1dfa956a845b`): 원격 ref·PR #205·worktree 설정·main sha 기록이 일치했다. **불일치 1건, 확인 보류**: 리드 보고는 「토큰 접두어 패턴 검색 0건」인데 메인이 근거 폴더 전체에서 리터럴 `github_pat_`을 세니 파일 1개가 걸렸다. 메인이 열어 보려 했으나 auto mode 분류기가 막았고, 리드도 그 파일을 열거나 찾지 않는다(메인 지시). 리드 스캔은 18:48Z의 `grep -rl -E "github_pat_|gho_[A-Za-z0-9]{20}"`이며 대상은 E/credential-trial/와 E/session/뿐이었다. 그 뒤 리드가 쓴 결과 문서 머리말은 검색한 패턴 이름을 글자로 적었다(리드 기억, 미확인). 근거 폴더는 Git 밖이라 원격 노출 경로는 없다. 사용자가 아침에 직접 보거나 허용 여부를 정한다.

## 다음 계획 후보

- `merge-gate-code-followup`의 O2(비ASCII 공백)·O6(이상한 session_id): 이 goal 밖이다. 종료 때 미해결이면 BACKLOG 새 행으로 등록한다.
- Claude 작업자의 TEMP 쓰기(두 번째 발생): 첫 선행 시험 작성자의 `/tmp` 쓰기(첫 발생)에 이어, 수정 회차 선행 시험 작성자가 orca receipt를 Claude Code 세션 scratchpad(TEMP 아래)에 썼다가 근거 폴더로 옮겼다. 원인은 Claude Code가 scratchpad를 안내하고 계약은 「그 밖의 TEMP 쓰기 금지」만 적은 데 있다. 교정 후보는 위임 계약 양식(작업 맥락 스킬 templates)에 「Claude Code가 안내하는 scratchpad도 TEMP다. receipt·임시 파일은 근거 폴더 아래에 쓴다」를 넣는 것이다. 이 goal의 PR2 범위 밖이라 종료 때 BACKLOG 새 행으로 등록하고, 그 전까지 이 goal의 Claude 계약에 그 문장을 넣는다.
- 실행 시점 검사·역할 분리(다음 Rules goal 설계 입력): 리드 비교 문서 E/runtime-check-comparison.md, 결정 요청 `msg_eac0fa1f3d89`. 그 뒤 도착한 메인 보충 `msg_9ff8eb25d846`(구조화된 도구 경로, 리드급 전면 거부와 메인 요청 인터페이스, 막는 층 셋)과 `msg_a7ce1ae26151`(세션 권한 판별 근거)은 사용자 원문과 함께 E/next-goal-inputs-runtime-role-split.md에 보관했다(메인 지시 `msg_4b7fc0b428af`). 메인이 설명한 「병합 자체를 사람 행동으로 빼는 안」은 사용자 판단 전이라 후보로만 둔다. PR1 결정 뒤 종료 때 BACKLOG 새 행 후보다.
- Claude safeguard와 병합 관문 검증(첫 발생): 병합·push 문구를 많이 다루는 검증은 Opus 5.5 safeguard 선택창에 걸릴 수 있다(위 「PR1 재검증(수정 회차, FAIL)」). 반복 규칙은 두 번째 발생부터다. 생기면 교정 층(계약 양식 또는 검증자 배정)을 정하고, 종료 때 BACKLOG 새 행 후보로 본다.
- 자격 증명 분리 후속 goal(이 goal의 PR1 몫을 받음, `msg_1108186019ec`): 아침 시험 3·4가 통과하면 R-5 기동에 `GH_CONFIG_DIR`, R-1 배치에 에이전트 worktree 설정 세 줄을 넣고 글자 그물·가리기 코드를 걷는다. 사용자 결정 후보는 (라)·(가), 프로젝트 범위 보강 후보는 (나)다(위 「자격 증명 분리 시험」·E/credential-trial/results-overnight.md). PR1 branch `13fe344d`의 시험·판정 원시는 출발 자료다. PR2에서 분리 방향과 겹쳐 뺀 문장은 아래 둘이며 이 goal에서 다시 쓴다.
  - 보조 세션 스킬 초안(`C:/Dev/DawnHolder_Dashboard/plans/helper-session-skill-draft.md`) 38행 「gh·merge 또는 push·main 낱말이 함께 든 명령은 막힌다. 그런 문자열 검색은 Grep·Read 도구로 하고, 메시지 본문은 파일로 넘긴다.」 PR2 스킬에는 병합 관문 링크만 둔다.
  - 같은 초안 44행의 이유 「본문 낱말이 명령 문자열에 섞여 hook에 막히는 일을 피한다.」
- 작업자 pane 배치 문장 정리(ORCA R-1·R-5 2단계): 같은 worktree는 리드 pane split, 다른 worktree는 `terminal create --worktree`로 바로 열고 빈 받침 pane을 만들지 않는다(사용자 결정 `msg_e62c4989b3dc`). split close 때 부모 pane이 꺼지는 Orca 1.4.222 관측과 함께 다룬다. 종료 때 BACKLOG 새 행으로 등록한다.
