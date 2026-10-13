# 규칙·운영 동결 전 문서 정리

- 만드는 것: 반복된 실수를 막는 규칙 문장 둘, 정본에 아직 없는 사용자 결정 셋의 문장, 낡은 링크 정정을 운영 문서 두 파일(ORCA·RESUME)과 CURRENT 하단 링크에 넣는다.
- 덧붙여 대기 목록(BACKLOG)에 빠진 후보 셋을 등록하고 기존 행을 갱신한다. 도구(hook·helper·검사 스크립트)는 만들지 않고 동결 뒤 후보로만 남긴다.
- PR 하나, 문서만 바꾼다. 코드·Codex 영역(AGENTS.md·`.agents/`)·다른 파트 goal은 건드리지 않는다. 예외는 실사 뒤 사용자가 승인한 세션 이어받기 스킬의 배치 문장 하나다(「적용 중인 사용자 결정」).

## 재개 지점

기능 동결(10-28) 전에 닫는 작은 Rules goal이었고 끝났다. 사용자가 범위를 승인했다(「적용 중인 사용자 결정」). 기준·상태·결과는 이 파일에 모은다. [CURRENT](../../../00_Document/operations/CURRENT.md)의 Rules 줄은 다음 Rules goal [라우팅 핫픽스 정본 반영](../2026-10-11-routing-hotfix-canon/goal.md)을 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch: `docs/pre-freeze-rule-docs-20261011`(base main `6782cd67`)은 PR229로 병합됐다(merge commit `704baa5e`). 종료 기록 branch는 `docs/pre-freeze-rule-docs-closeout-20261011`(base main `704baa5e`)이다. 정확한 head는 원격 branch와 승인 묶음에서 확인한다.
- 근거 폴더 E: `.backups/verification/2026-10-11-pre-freeze-rule-docs/`(Git 제외). 승인 범위는 [scope-draft-v1.md](../../../.backups/verification/2026-10-11-pre-freeze-rule-docs/scope-draft-v1.md)(SHA256 `b783ae27…`)와 메인 판단 넷(「요구사항 원천과 적용 결정」)이다. 리드 맥락 메모는 E/lead-context.md(진입·범위 초안), E/pr-lead-context.md(착수), E/closeout-lead-context.md(종료 기록)다.
- 리드: 신규 `claude-opus-5-5` xhigh(화면 「Opus 5.5 ⚡xhigh」, backend unknown), 태그 `[Rules 리드 Opus]`, handle `term_b1fddc67-1e99-4471-af0b-8fe6c295d371`, Run `run_d447bca7c344`(회신 주소 `run:run_d447bca7c344`). 메인 주소는 메인 term handle이다. 이전 Rules goal의 Run·Task·Dispatch·handle은 실행 권한이 아니다.
- 앞 goal의 끝: [세션 쓰기 가드와 생존 신호 간격 도구](../2026-10-10-operating-tool-guards/goal.md)는 PR224·PR228 병합으로 끝났다. PR228 좁힌 재실사 통과(근거 rules-active [E/closeout-rereview/verdict.md](../../../.backups/verification/2026-10-10-operating-tool-guards/closeout-rereview/verdict.md)).
- **현재 위치**: 종료. PR229와 종료 기록 PR231이 병합됐고 종료 점검과 R-8이 끝났다(「현재 결과 / PR231 병합과 종료 점검」).
- **남은 순서**: 없다. PR231 병합 뒤의 기록은 다음 Rules goal의 첫 커밋으로 넣었다.
- **사용자 차례**: 없다.

## 진척 단계

- [x] 범위와 기준 확정
- [x] 정본 문장 작성
- [x] 대기 목록 정리
- [x] 문서 실사
- [x] 종료 점검자 확인
- [x] 결과 기록·재실사
- [x] PR229 병합
- [x] PR231 병합
- [x] 종료 점검과 R-8

PR229(https://github.com/bass131/dawnholder-server/pull/229)를 2026-10-11에 만들었고 같은 날 병합했다. 종료 기록 PR231(https://github.com/bass131/dawnholder-server/pull/231)을 같은 날 만들었고 같은 날 병합했다.

## 범위

승인 범위는 초안 v1 그대로다. 넣는 기준은 메인 진입 지시의 후보 표이고, 후보마다 넣음·기록만·넣지 않음의 판정과 이유는 초안 「1. 후보별 판정」에 있다.

### 만들 것

1. 정본 문장 여섯. 작성자는 원천 원문과 아래 의도를 받아 문장을 쓴다. 문장은 기존 문단 안에 붙여 ORCA 줄 수를 늘리지 않는다.
   - S1 ORCA 「Orca 도구 관측과 우편함 대기」: 리드는 작업자 기동·종료·지시 직전, 작업자는 컨테이너·볼륨 삭제나 중지처럼 되돌리기 어려운 명령 직전에 `--wait` 없는 check를 한 번 한다. 같은 부류 3회의 반복 규칙이다(「요구사항 원천과 적용 결정」).
   - S2 ORCA 「확정 실패 집계와 Fable Advisor」: Advisor 기본은 신규 읽기 전용 `claude-fable-5-1`이고, 리드는 이유를 기록하고 신규 읽기 전용 `gpt-6-astra` xhigh를 대신 고를 수 있다. 기동 조건·기동 전 메인 보고·조언 파일 하나·새 Sol의 채택 기록·R-5·R-6은 그대로다.
   - S3 ORCA 「capacity 재시도와 신규 작업자 예외」: Opus 검증자에게 safeguard 선택창이 뜨면 창에서 고르지 않는다. 산출물을 근거 폴더에 둔 채 정산·종료하고, 같은 계약의 신규 `gpt-6-astra` xhigh 검증자를 새 Task·Dispatch로 연다. 메인에 status 한 통(막힌 시각·새 handle·모델 표시)을 보낸다. 실행 중 모델 변경이 아니라 세션 교체다. 적용 대상은 Opus 검증자로 한정한다(사용자 승인, 「적용 중인 사용자 결정」).
   - S4 ORCA R-1 끝 문단과 R-5 2단계: 같은 worktree의 작업자는 리드 pane split, 다른 worktree는 `orca terminal create --worktree`로 바로 열고 빈 받침 pane을 만들지 않는다. split을 닫은 뒤 부모 pane이 꺼질 수 있으니(1.4.222 관측) 다음 split 전에 부모가 살아 있는지 본다.
   - S5 RESUME 「세션 진입 배치」: Claude 세션은 첫 파일 쓰기 전에 맥락 메모를 쓴다. 진입 순서는 읽기 → 메모 → Run·receipt·READY다. 메모는 `.backups/verification/<goal>/` 아래 이름에 `context`가 든 `.md`이고, 범위 승인 전이면 예정 goal 이름의 폴더를 쓴다. 리드 세션의 임시 파일은 Claude Code scratchpad(checkout 밖이라 가드가 막는다)가 아니라 `.backups/tmp/` 아래에 둔다.
   - S6 RESUME 「다음 조각을 시작하는 순서」 끝 줄: 적용 중인 결정은 각 goal의 「적용 중인 사용자 결정」 절에 있고, 정본 반영을 기다리는 결정은 BACKLOG 행으로 찾는다. CURRENT 하단 「정본 반영 전 적용 결정」 링크도 같은 문장으로 고친다(메인 판단 3, 리드가 S6 확정 뒤 씀).
2. 이 goal 파일. 첫 기록 셋(메인 판단 `msg_c3df82b7150e`): 재개 지점의 PR228 재실사 통과 한 줄, 「현재 결과 / 첫 기록」의 재검토 뒤 goal 수정 두 번째 이탈과 helper 후보, 「위험」 4의 남은 위험. bytes 상쇄로 옮긴 문장이 있으면 「정본에서 옮긴 적용 기록」 절에 둔다.
3. BACKLOG: 새 절 「동결 전 문서 정리 goal에서 연결한 후보」에 새 행 셋(`rereview-head-diff-helper`, `terminal-workspace-alternative-review`, `merge-gate-deny-wording`)과 실사 뒤 사용자 조건으로 더한 넷째 `rule-document-subdivision`, Gardener 후보를 사용자가 채택한 다섯째 `rule-document-measure-helper`(「적용 중인 사용자 결정」). Gardener 결과로 `official-draft-check-helper`·`lead-mailbox-receive-delay`에 근거를 더한다. 기존 행 갱신: 이 goal로 승격 넷(`session-entry-memo-order`, `advisor-model-alternative-canon`, `claude-safeguard-verifier-switch`, `worker-pane-placement-wording`), 근거 더하기 둘(`mailbox-wait-overlap-guard` 셋째 발생, `goal-state-drift` 처리한 부분), `single-output-role-memo-first`에 S5가 메모 이름 조건을 정본에 두었다는 한 구절.
4. CURRENT Rules 줄(이 goal 링크·경로·branch)과 하단 링크(S6과 같은 문장).

### 건드릴 곳

| 파일 | 쓰는 사람 | 내용 |
|---|---|---|
| `00_Document/operations/ORCA.md` | 신규 Sol | S1~S4, bytes 상쇄로 옮길 지난 기록 구절. 실사 D2 수정은 새 Sol(지운 출처 구절 O1 복원·연속 빈 줄 정리) |
| `00_Document/operations/RESUME.md` | 신규 Sol | S5·S6 |
| `.agents/skills/dawnholder-session-handoff/SKILL.md` | 새 Sol | 실사 D1 수정: 25행의 배치 문장 하나만 R-1 참조로 바꾼다(사용자 승인, 이 한 문장 한정) |
| 이 goal | 리드 | 범위·진척 단계·첫 기록·결정·결과 |
| `00_Document/operations/BACKLOG.md` | 리드 | 새 행 다섯(넷째는 실사 뒤 사용자 조건의 `rule-document-subdivision`, 다섯째는 사용자가 채택한 Gardener 후보 `rule-document-measure-helper`), 기존 행 갱신 |
| `00_Document/operations/CURRENT.md` | 리드 | Rules 줄과 하단 링크. 다른 파트 줄은 보존 |

Sol과 리드는 파일이 겹치지 않는다. commit·push는 리드만 한다.

### 하지 않을 것

- hook·helper·검사 스크립트·CI 변경. 필요한 도구는 BACKLOG 행만 둔다.
- 진입 지시가 동결 뒤로 넘긴 목록: helper·hook 구현, `mailbox-wait-overlap-guard` 도구, `official-draft-check-helper`, `lead-measured-fact-recompute`·`worker-harness-only-execution`의 도구화, 운영 셋업 마일스톤의 하네스·STE100·HTML 보고·자기 개선 루프, `credential-separation-followup`, `heartbeat-interval-review`, `session-guard-followup`(10-31 평가).
- `single-output-role-memo-first`의 정본 변경(R-7 「유일한 출력」 문구 포함).
- Codex 영역(AGENTS.md·`.agents/`)과 CLAUDE.md, `.claude/` 설정, 메인 개인 도구. 세션 이어받기 스킬의 승인된 한 문장만 예외다.
- 다른 파트 goal, 게임 코드, CURRENT의 다른 파트 줄, 끝난 Rules goal(2026-10-10-operating-tool-guards).
- 규칙 문서 가지치기(10-31 Gardener 평가).

### 관찰 가능한 완료조건

1. S1~S6이 ORCA·RESUME에 들어가고 각 문장이 원천(사용자 원문·메인 전달 msg ID·반복 근거 행)과 맞는다. 신규 독립 실사자가 원천 대조와 현실 시나리오 다섯(새 리드 진입과 가드, 미확인 메일이 있는 채 작업자 pane 닫기 직전, 확정 실패 3회 뒤 Advisor 고르기, Opus 검증자의 safeguard 선택창, 다른 worktree 작업자 띄우기)으로 확인한다.
2. BACKLOG 새 행 다섯·기존 행 갱신이 항목 계약(7필드, 상태 세 값)을 지키고, 후보 도착 검사(`node 99_Tools/Backlog/check-candidates.mjs --backlog 00_Document/operations/BACKLOG.md --goal 01_Phases/goals/2026-10-11-pre-freeze-rule-docs/goal.md`)가 `allowed`·중복 0이다. 리드 실행 원시를 E에 두고 실사자가 같은 명령을 다시 돈다.
3. 이 goal에 첫 기록 셋과 이번 사용자 결정 원문이 있다.
4. 변경 경계: `git diff --stat <base>...HEAD`에 「건드릴 곳」의 여섯 파일만 있다. 코드·다른 파트 파일 변경 0이고, Codex 영역은 승인된 스킬 한 문장뿐이다.
5. 규칙 문서 크기: ORCA 250줄 이하. 여섯 파일 묶음 합계는 「PR 경계와 검증」의 bytes 줄대로 잰다. 출처 복원에 따른 증가는 사용자가 허용했다(「적용 중인 사용자 결정」).
6. 신규 실사 통과 → Gardener(goal 종료 점검자) → 결과 기록과 바뀐 부분 재실사 → 사용자 병합 승인 → 종료 점검 → R-8. 다음 goal은 자동으로 시작하지 않는다.

## PR 경계와 검증

| 항목 | 값 |
|---|---|
| PR | 하나. 최신 main에서 `docs/pre-freeze-rule-docs-20261011` |
| 검증 등급 | 문서 실사(문서만 변경). 줄 수 원시는 작성 뒤 `git diff --numstat`으로 E에 둔다 |
| TDD | 해당 없음(문서만 변경, goal-loop 「구현·검증·수정」) |
| R-7 | 비해당(메인 판단 4: 문서만 바꾸는 작업이고 시범 자리도 다 썼다) |
| 정본 문장 작성 | 신규 `gpt-6.1-sol` max. 구현자 신호 해당 없음(명세가 정해진 문서 문장, [배정](../../../.agents/skills/dawnholder-goal-loop/references/implementer-routing.md) 「신호가 없을 때 Sol max로 둘 일」의 「문서」) |
| 문서 실사 | 신규 `gpt-6-astra` xhigh(검증자 모델 시범 「문서 실사」) |
| Gardener | 신규 `claude-opus-5-5` 읽기 전용, 보고서 한 파일(이름에 `context`를 넣어 세션 쓰기 가드와 맞춘다). 병합 전 고정 head(메인 판단 2) |
| 수정·재실사 | 결함이 나오면 새 Sol, 새 Astra. 같은 산출물 수정 3회 초과는 메인 체크포인트 |

- 차례: 시작 커밋(리드) → 정본 문장(Sol)과 BACKLOG(리드) 동시 → 리드 대조·bytes·후보 검사 → PR 생성 → 문서 실사 → Gardener → 결과 기록 → 바뀐 부분 재실사(결과는 goal을 고치지 않고 근거·승인 묶음으로) → 승인 묶음 → 사용자 병합 승인 → 종료 점검 → R-8.
- 규칙 문서 bytes: 사용자 결정 「규칙 가지치기 → A 지금은 늘지 않게만」(2026-10-04 「7건 모두 A」, 가지치기는 10-31 Gardener 평가)이 유효하다. 지난 Rules goal의 「상쇄 의무 없음」(메인 `msg_39e69295de62`)은 그 goal 한정이었다. 기준 `6782cd67`의 여섯 파일 묶음(AGENTS·ORCA·goal-loop SKILL·RESUME·session-handoff SKILL·orca-work)은 `git show <rev>:<file> | wc -c`로 합계 103,221 bytes, ORCA 41,343 bytes·249줄이다(E/bytes-base.txt). CLAUDE.md는 메인 몫이라 따로, BACKLOG·CURRENT는 묶음 밖에서 수치만 보고한다. 늘어난 만큼 같은 PR에서 ORCA의 지난 적용 기록 구절을 「정본에서 옮긴 적용 기록」으로 옮긴다. 현재 규칙 문장은 지우지 않는다. 최대한 상쇄해도 남으면 실사 전에 원시 수치(기준·head 여섯 파일 합계, ORCA 줄 수·bytes, 옮긴 구절 목록)를 status로 올리고 메인이 사용자에게 묻는다(메인 판단 1).
- CURRENT가 다른 파트 PR과 겹치면 승인 묶음 직전에 main을 받아 맞추고 다른 파트 줄을 보존한다(메인 판단 3).

### 위험

1. bytes 상쇄가 모자랄 수 있다. 옮길 수 있는 구절이 S1~S6 증가분보다 적을 가능성이 크다. 실사 전 사용자 판단으로 정한다.
2. S3과 AGENTS 「모델 라우팅」의 「capacity 장기 실패 시 신규 `gpt-6-astra` 작업자 전환만」 구절이 충돌해 보일 수 있다. 리드 판단은 그 구절이 capacity 경우만 한정해 충돌이 아니라는 것이다. 실사자가 대조하고, 충돌로 판정되면 메인을 거쳐 사용자 승인으로만 AGENTS를 고친다(Codex 영역).
3. 새 진입 순서 문장은 병합 뒤 새로 여는 세션부터 읽힌다. 떠 있는 리드는 다음 안전 지점에 main을 받는다([main 맞춤과 합류점](../../../.agents/skills/dawnholder-goal-loop/references/milestones.md#main-맞춤과-합류점)).
4. 남은 위험(메인 판단 `msg_c3df82b7150e`): R-7 검토자·Fable Advisor·Gardener처럼 한 파일만 쓰는 역할은 세션 쓰기 가드 규칙 3(첫 쓰기는 메모)과 충돌한다. R-7을 쓰면 메인이 계약 예외를 정한다. 이 goal은 정본을 바꾸지 않는다(BACKLOG `single-output-role-memo-first`).

## 요구사항 원천과 적용 결정

메인이 전달한 사용자 결정은 사용자 직접 입력과 구분한다. 메인 판단은 사용자 결정이 아니다.

- 진입 지시: 메인 `msg_68b1c72e0160`(2026-10-11T06:31:18Z, E/session/inbox-peek-entry.json). 후보 표, 동결 뒤로 넘길 목록, 첫 기록 셋(메인 판단 `msg_c3df82b7150e` 인용), 범위 원칙(문서만, PR 하나, 도구는 BACKLOG 행만, CURRENT는 Rules 줄만, bytes 조건 확인).
- 범위 초안 v1: 리드 `msg_37a299c91578`(06:4x Z, E/scope-draft-v1.md).
- 범위 승인과 메인 판단 넷: 메인 `msg_935fcc451ce8`(2026-10-11T06:45:15Z, E/session/wait01.json).
  1. bytes 상쇄가 모자랄 때는 메인이 정하지 않는다. 사용자 결정 「지금은 늘지 않게만」의 예외이기 때문이다. 같은 PR에서 최대한 상쇄한 뒤 남으면 실사 전에 원시 수치와 status로 올린다. 메인이 그때 사용자에게 묻는다.
  2. Gardener는 PR 하나로 끝내도록 병합 전 고정 head에서 돌리는 것을 허용한다(ORCA R-8).
  3. CURRENT 하단 링크는 S6과 같은 문장으로 이 PR에서 고친다. 다른 파트 줄은 보존한다.
  4. R-7 비해당.
- S1 반복 근거(같은 부류 3회, Core branch `feat/persistence-container-engine-20261010`의 커밋 `dab31b60`에 있는 [영속 통합 goal](https://github.com/bass131/dawnholder-server/blob/dab31b6087311d80d0da1f05f9f9e89a29a73689/01_Phases/goals/2026-10-04-persistence-integration/goal.md) 617·618·642행, 아직 main 밖): Core 셋째 작업자가 리드 지시를 읽기 전에 컨테이너·볼륨을 지웠고 「복구 직전 우편함 확인을 빠뜨렸다」고 알렸다. Core 리드 실수 11은 마감 지시를 읽기 전 작업자 기동, 실수 12는 메인 지시를 읽기 전 pane 닫기다. 교정 층: 셋째 발생은 Codex 작업자라 Claude hook이 닿지 않는다. 리드·작업자 둘 다에 닿는 층은 정본 문장과 계약 문구다. 기능 동결 전 도구 금지(사용자 결정)도 있다.
- S2·S3·S4 원천: [hook 정리 goal 「적용 중인 사용자 결정」](../2026-10-07-hook-friction-helper-session/goal.md#적용-중인-사용자-결정) 154~160행.
  - S2 사용자 원문 「음 만약에 Advisor가 필요하면 Astra로 체크해보는거도 방법이야」·「OK, Advisor로 Fable 5.1도 있지만 GPT 6 Astra도 가능한점을 고려해줘」(메인 전달 `msg_87c0fed69b04`·`msg_6439bbd9ef9b`, 2026-10-07T13:30:23Z·13:32:07Z).
  - S3 사용자 원문 「만약 또 SafeGuard로 막히면 Astra로 대체해서 진행해줘」(메인 전달 `msg_1aa6aa9fa263`, 2026-10-07T16:40:00Z). 실제 교체는 메인 지시 `msg_47708ee158ad`(16:50:37Z, 선택창 두 번째 발생 뒤).
  - S4 사용자 원문 「음 아마 우리 규칙에서 문장상 모호한게 있었나보네, 창 하나를 띄울때는 굳이 Pane을 하나 더 만들어서 Agent를 할당할 필요는 없어」(메인 전달 `msg_e62c4989b3dc`, 2026-10-07T14:23:02Z). split close 뒤 부모 pane 꺼짐은 같은 goal 206행의 1.4.222 관측이다.
- S5 반복 근거: BACKLOG `session-entry-memo-order`, 메인 집계 리드 첫 쓰기 차단 3회. 이번 진입에서도 둘이 막혔다(「현재 결과 / 착수」).
- S6 근거: BACKLOG `goal-state-drift`. RESUME·CURRENT의 적용 결정 링크가 [병합 관문 goal](../2026-10-06-merge-gate-canon-refresh/goal.md#적용-중인-사용자-결정) 하나만 가리키지만 적용 중인 결정은 여러 goal에 흩어져 있다.
- 문서 실사 판정: 실사자 worker_done `msg_3bf3bbccbb2d`(2026-10-11T07:51:47Z), 판정 원문 E/review/verdict.md, 차단 D1~D4(「현재 결과 / 문서 실사」).
- 메인 결정 D4 `msg_9b35d66297d6`(2026-10-11T07:57:52Z, E/session/wait09.json, 사용자 결정 아님): 작성 Sol의 생존 신호 300초 초과 셋은 비차단 운영 기록이다. 산출물은 별도 독립 실사를 거쳤고 위반은 산출물 정확성이 아니라 신호 간격이라는 이유다. 같은 부류는 BACKLOG `heartbeat-interval-review`에서 묶어 본다. 다음 Sol·Astra 계약에 5분 상한과 「길어지면 스스로 알림」을 다시 강조한다.
- 출처 복원 답의 메인 해석 `msg_0c222663ca85`(2026-10-11T08:02:27Z, E/session/wait10.json): 이번 PR은 지운 출처 구절을 원문 그대로 되살리고 빈 줄을 정리한 뒤 bytes·ORCA 줄 수를 다시 잰다. 「하위 문서로 분화」 조건은 에이전트가 한 번에 읽는 길이 원칙(200줄 목표·250줄 상한)으로 읽는다. 수정 뒤 ORCA가 250줄을 넘거나 분화가 AI 읽기에 필요하면 리드는 혼자 나누지 않고 분화안을 status로 올린다. 250줄 이하면 이 PR은 복원만 하고 분화는 다음 계획 후보와 BACKLOG 행으로 남긴다.
- 스킬 문장 답의 메인 전달 `msg_3253008689b9`(2026-10-11T08:05:22Z, E/session/wait11.json): Codex 영역 쓰기 승인은 세션 이어받기 스킬 25행 그 한 문장에 한정된다. 같은 파일의 다른 줄과 `.agents/`의 다른 파일은 그대로다.

## 적용 중인 사용자 결정

모두 메인 창 Enter 제출, 메인 전달이다. 직접 입력으로 격상하지 않는다.

- **다음 Rules 목표**(진입 지시 `msg_68b1c72e0160`, 2026-10-11 15:29 KST 무렵): 「대시보드 결정 응답: 1) Rules 다음 목표 - 기능 동결(10-28)까지 닫아 둘지 → A 작은 문서 목표 지금」. 선택지 효과 원문: 「새 Rules 리드를 지금 열어 반복 실수 규칙 문장과 정본에 빠진 사용자 결정 셋, 대기 목록 등록만 묶은 작은 범위 초안을 받는다. hook·helper 같은 도구는 동결 뒤로 미룬다.」 메인 추천은 B(동결 뒤)였고 사용자가 A를 골랐다. 계기는 15:19 KST 무렵 사용자 코멘트 「룰쪽 더 해야하는 부분 있나?」에 메인이 읽기 전용 조사로 답한 것이다.
- **범위 승인**(메인 전달 `msg_935fcc451ce8`, 2026-10-11 15:4x KST): 「대시보드 결정 응답: 1) 계획 검토 - World 다중 계정 로그인 goal 범위 초안 v1 → A 승인 (초안 msg_148c21badf30) · 2) 계획 검토 - Rules 동결 전 규칙 문서 정리 범위 초안 v1 → A 승인 (초안 msg_37a299c91578)」. 이 goal은 2)다. 초안 그대로이며 코멘트가 없어 S3은 초안대로 「Opus 검증자」 한정이다. 메인은 정리본에서 「사용자 원문은 검증자로 한정하지 않았다」는 관찰을 사용자에게 보였고 사용자는 코멘트 없이 승인했다.
- **실사 결함 처리 — 출처 복원과 bytes**(메인 전달 `msg_0c222663ca85`, 2026-10-11 17:0x KST): 「대시보드 결정 응답: 1) Core 엔진 검사 약점 - 실행 절차에 확인 단계를 넣을지, 도구 코드를 고칠지 → A 절차에 확인 단계 · 2) PR229 - 세션 이어받기 스킬 한 문장을 정본 참조로 바꿀지(Codex 영역) → C 코멘트 「세션 이어받기 스킬이 어떤거 말하는거지?」 · 3) PR229 - 끊긴 출처 링크를 되살려 규칙 문서가 조금(약 73 bytes) 느는 것을 허용할지 → C 코멘트 「대부분 A로가되, 너무 길어지면 하위 문서로 분화시킬 것, AI Readiness를 최대한 원활하게 해줘」」. Rules 해당은 2)·3)이다. 1)은 Core 안건이고, 2)는 아래 줄에서 다시 답했다. 메인 해석은 「요구사항 원천과 적용 결정」에 있다.
- **실사 결함 처리 — 스킬 문장 교체**(메인 전달 `msg_3253008689b9`, 2026-10-11T08:05:22Z): 「대시보드 결정 응답: 1) PR229 - 세션 준비·인계 스킬의 작업자 배치 문장 하나를 정본 참조로 바꿀지 → A 바꿈 승인」. 앞 응답의 2)에서 사용자가 「세션 이어받기 스킬이 어떤거 말하는거지?」라고 물어 메인이 설명한 뒤 다시 받은 답이다.
- **Gardener 정리 후보 반영**(메인 전달 `msg_404b9a6bdb15`, 2026-10-11T09:17:21Z): 「대시보드 결정 응답: 1) PR229 - 작업자 기동 도우미 스크립트 후보를 BACKLOG에 넣을지 → A 넣음 · 2) PR229 - 규칙 문서 크기 계측 스크립트 후보를 BACKLOG에 넣을지 → A 넣음」. 1)은 Gardener 후보 1(공식 계약 기동 helper), 2)는 후보 2(규칙 문서 크기·출처 연결 계측 helper)다. 메인이 사용자에게 보인 설명: 둘 다 착수 권한이 아니고 기능 동결(10-28) 뒤 착수 전제다. 2)의 줄 수 상한(250줄을 늘 지키는 상한으로 올릴지, 200줄 목표, 묶음 범위) 같은 정책은 이 답으로 정해지지 않았고 착수 때 다시 묻는다.
- **라우팅 핫픽스**(메인 전달 `msg_ca6641b7a9e2`, 2026-10-11T11:03:17Z. 사용자 제출 시각 20:02 KST는 메인 진입 지시 `msg_300feadfab41`): 「대시보드 결정 응답: 1) 모델 라우팅 - 모든 리드를 문맥 25만에서 압축하도록 지금 적용할지 → A 전체 적용 · 2) 모델 라우팅 - 리드가 같은 메시지로 두 번 깨는 것을 지금 규칙으로 막을지 → A 지금 적용 · 3) 모델 라우팅 - 로컬 DB 설치·기동 도구의 검증을 지금부터 Astra로 옮길지 → A 지금부터 Astra · 4) 모델 라우팅 - 선행 시험 작성자를 Codex로 옮기는 시점 → A 10-31 평가 때」. 메인 지시는 각 리드가 자기 goal에 이 원문과 메시지 ID를 적는 것이다. 이 goal은 PR231 검토 중이라 넣지 못했고 PR231 병합 뒤 다음 Rules goal의 첫 커밋에서 넣었다. 정본 반영은 다음 Rules goal [라우팅 핫픽스 정본 반영](../2026-10-11-routing-hotfix-canon/goal.md)이 한다.

## 현재 결과

### 착수

- 진입: Run `run_d447bca7c344`(2026-10-11T06:36:56Z, E/session/run-create.json), READY status `msg_caf09cfaa914`. 진입 지시는 Run 바인딩 전 터미널 우편함으로 왔다. 바인딩 뒤에는 소비할 수 없어 원문 보존으로 처리했다(바인딩 뒤 Run peek 0건, E/session/check-peek-after-bind.json).
- 진입 중 세션 쓰기 가드 차단 둘, 실제 쓰기 0(S5 근거): (1) orchestration 가이드 출력을 Claude Code scratchpad로 저장하려다 `write-outside-checkout`, (2) 메모 전에 우편함 출력을 근거 폴더에 저장하려다 `memo-first`. 리드는 `check --peek`(가드 허용)으로 지시를 읽고 메모(E/lead-context.md, 06:36:13Z)부터 썼다.
- 시작 기록: branch `docs/pre-freeze-rule-docs-20261011`을 origin/main `6782cd67`(06:45:45Z fetch)에서 만들었다. 커밋은 `27845993`(이 goal과 CURRENT Rules 줄)과 `497324de`(근거 줄 범위 154~160 정정)다. 착수 메모는 E/pr-lead-context.md(06:45:45Z)다.

### 정본 문장 작성

- 작성자: 신규 `gpt-6.1-sol` max(화면 「GPT-6.1-Sol max」, backend unknown), Task `task_f5fbd4403fbb`, Dispatch `ctx_0b84ff5dd49f`, 2026-10-11T06:54:41Z~07:21:45Z, worker_done `msg_555fe02cf582`(succeeded). 계약 E/sentences-contract.md v1(SHA256 `c87e0b01…`, 고정 HEAD `497324de`), 보고 E/sentences/report.md, 기동 기록 E/sentences-start-note.md다. 작성자 맥락 메모 E/sentences/context.md는 대상 첫 쓰기 전(06:56:17Z)이다.
- 기동: 첫 worker-start는 Git Bash가 35KB 계약 인자를 받지 못해(「Argument list too long」) orca가 실행되지 않았다. 입력은 없었다. 지난 goal의 node 감싸기(E/worker-start.mjs)로 다시 했다. `turn_start_unobserved`였고 draft 25,042자(계약 20,254 + 4,787 + 1)를 확인해 Enter만 한 번 보냈다. rollout 사후 대조에서 Enter로 제출된 입력 하나가 계약 전문을 담았다(E/sentences-rollout-match.json).
- 결과: S1 ORCA:93, S2 ORCA:158, S3 ORCA:153(151행에 「capacity로 막힌」 한정 구절), S4 ORCA:47·51·142, S5 RESUME:22, S6 RESUME:38. 고정 HEAD 대비 numstat은 ORCA 20/20, RESUME 2/1이다.
- bytes: 여섯 파일 묶음 103,221 → 103,218(−3), ORCA 41,343 → 40,961 bytes, 249줄. 문장 증가 1,675 bytes를 출처 가리킴 구절 열셋 1,678 bytes 이관으로 상쇄했다(「정본에서 옮긴 적용 기록」). 메인 판단 1의 사용자 질문 조건은 생기지 않았다.
- 리드 R-2(E/lead-check/r2-sentences.json, 스크립트 r2-sentences.mjs): 합계·ORCA 줄 수를 다시 재어 보고와 같다. 열세 구절은 모두 고정 HEAD에 원문 그대로 있었고 작업 파일에서 빠졌다. 두 파일의 제목·anchor 목록이 같다(ORCA 제목 21·anchor 21, RESUME 제목 7). 보고서의 S6 문장이 RESUME 줄과 같다. 표본 대조이며 독립 판정이 아니다.
- Codex 영역 충돌 후보(작성자 보고, 파일은 쓰지 않음): (1) AGENTS 「모델 라우팅」의 「리드 직접 구현·실행 중 모델 변경·Opus 대체는 허용하지 않는다」를 따로 읽으면 S3과 어긋날 수 있다. 리드 판단은 구현자 실패 때 Opus로 대신 맡기는 것을 막는 문장이라 충돌이 아니라는 것이다. (2) session-handoff 스킬 「세션 진입과 배치」의 「작업자·검증자는 각 리드 아래 vertical split이다」는 S4의 다른 worktree 작업자와 어긋날 수 있다. 문서 실사가 판정하고, 충돌이면 메인을 거쳐 사용자 승인으로만 고친다(「위험」 2와 같은 처리).
- 생존 신호(helper, E/lead-check/liveness-sol.json): 신호 7, 최대 411초. 300초 초과는 셋(307초 07:02:08Z→07:07:15Z, 343초 07:07:15Z→07:12:58Z, 411초 07:12:58Z→07:19:49Z)이다. 작성자는 411초를 스스로 알렸다(status `msg_46bd296690cc`). 처분은 메인 결정 `msg_9b35d66297d6`(07:57:52Z)에 따라 비차단 운영 기록이다(실사 D4, 「요구사항 원천과 적용 결정」).
- 정산: 빈 프롬프트 확인, `worker-release` retained(external_terminal), `terminal close`(07:23:52Z).

### 대기 목록 정리

- 리드가 E/backlog-edit.mjs로 고쳤다(결과 E/backlog-edit-result.json). 승격 넷, 근거 더하기 셋(`mailbox-wait-overlap-guard`·`goal-state-drift`·`single-output-role-memo-first`), 새 절의 행 셋이다. 스크립트는 고친 행마다 한 번만 있고 일곱 칸인지 단정했다. 단어 단위 diff로 덧붙인 글만 바뀐 것을 확인했다.
- 후보 도착 검사(E/lead-check/candidate-check-1.json): `allowed`, exit 0, BACKLOG ID 112, 중복 0, 후보 6, 인용 없는 후보 0, 모르는 ID 0.
- CURRENT 하단 링크는 S6 첫 문장으로 고쳤다(메인 판단 3).
- 실사 뒤 넷째 새 행 `rule-document-subdivision`을 같은 절에 더했다(메인 해석 `msg_0c222663ca85`). 후보 도착 검사(E/lead-check/candidate-check-3.json): `allowed`, exit 0, BACKLOG ID 113, 중복 0, 후보 7, 인용 없는 후보 0, 모르는 ID 0.
- 결과 기록 때 사용자가 채택한 Gardener 후보로 다섯째 새 행 `rule-document-measure-helper`를 같은 절에 더했고, `official-draft-check-helper`(이유·출처)와 `lead-mailbox-receive-delay`(출처)에는 원래 글 뒤에 덧붙이기만 했다. 후보 도착 검사(E/lead-check/candidate-check-5.json): `allowed`, exit 0, BACKLOG ID 114, 중복 0, 후보 10, 인용 없는 후보 0, 모르는 ID 0.

### 문서 실사

- 실사자: 신규 `gpt-6-astra` xhigh(화면 「GPT-6-Astra xhigh」, backend unknown), Task `task_cce8d5843f53`, Dispatch `ctx_eedaf0063445`, 2026-10-11T07:30:14Z~07:51:47Z, worker_done `msg_3bf3bbccbb2d`(succeeded, 판정은 차단). 계약 E/review-contract.md v1(SHA256 `c287560d…`, 고정 head `70c80014`), 판정 E/review/verdict.md, 기동 기록 E/review-start-note.md다.
- 지적 없음: 여섯 문장의 원천, 현실 시나리오 다섯 중 넷, 옮긴 열세 구절 중 열둘, bytes 계측, BACKLOG 새 행·승격·근거 더하기, 후보 도착 검사(`allowed`, ID 112), CURRENT 다른 파트 줄 불변, 변경 경계, 측정값 위장 없음.
- 차단 넷과 처리:
  - D1(P2, 리드 계약 귀속): 세션 이어받기 스킬 25행 「작업자·검증자는 각 리드 아래 vertical split이다」가 S4의 다른 worktree 작업자 직접 생성과 어긋난다. 사용자가 그 한 문장 교체를 승인했다. 새 Sol이 고친다.
  - D2(P2, 작성 Sol 귀속): 상쇄로 ORCA 21행에서 지운 O1 때문에 R-8 「메인 운영 판단」·「메인 결정」과 우편함 절 「원천·측정 한계」의 `#pr2-source` 링크가 실제 원천으로 이어지지 않는다. 사용자가 bytes 증가를 허용해 새 Sol이 O1을 원문 그대로 되살린다. 같은 계약·결함 번호의 확정 실패 1회째다.
  - D3(P3, 리드 귀속): 리드 맥락 메모 둘의 「준수 연결」이 미작성이었다. 리드가 2026-10-11T07:57:25Z에 완료 칸과 자기 쓰기 감사를 채웠다. 처음 쓴 계획 칸과 시각은 고치지 않았다.
  - D4(P3, 처분 근거): 작성 Sol 생존 신호 위반의 「비차단」 단정에 근거가 없었다. 메인 결정 `msg_9b35d66297d6`을 처분 출처로 적었다(「정본 문장 작성」).
- 실사자 생존 신호(E/lead-check/liveness-review.json): 신호 9, 최대 264초, 300초 초과 0. 정산: 빈 프롬프트 확인, `worker-release` retained, `terminal close`(07:53:24Z).
- 수정 1회째(같은 산출물 ORCA 1회째): 새 `gpt-6.1-sol` max(화면 「GPT-6.1-Sol max」, backend unknown), Task `task_5ecf769dd096`, Dispatch `ctx_dfc3d135f594`, 2026-10-11T08:08:26Z~08:19:53Z, worker_done `msg_63040e8836b6`(succeeded). 계약 E/fix1-contract.md v1(SHA256 `cc2b7624…`, 고정 head `70c80014`), 보고 E/fix1/report.md, 기동 기록 E/fix1-start-note.md다. `turn_start_unobserved`였고 draft 23,672자(계약 18,884 + 4,787 + 1)를 확인해 Enter만 한 번 보냈다. rollout 사후 대조에서 Enter로 제출된 입력 하나가 계약 전문을 담았다(E/fix1-rollout-match.json).
- 수정 결과: ORCA 21행이 base 21행과 같아졌다. 연속 빈 줄 묶음 둘(앞 head 22~24행, 179~181행, 각 빈 줄 셋)에서 초과 빈 줄 4개를 지워 묶음이 2에서 0이 됐다. 리드 원시 r2-fix1.json의 `orcaBlankRunsOutsideFences` 값 4는 묶음 수가 아니라 인접한 빈 줄 쌍의 수다. ORCA는 249줄에서 245줄이 됐고 제목·anchor 수는 그대로다. 스킬은 25행 한 문장만 「Core 리드 탭과 작업자·검증자 pane 배치는 R-1 정본에서 확인한다.」로 바뀌었다(120 → 85 bytes).
- bytes: 여섯 파일 묶음이 base 103,221, 앞 고정 head 103,218, 수정 뒤 103,306이다. 이번 수정은 앞 고정 head 대비 +88(출처 복원 127 bytes − 빈 줄 4 bytes − 스킬 문장 35 bytes)이고, 앞 head의 base 대비 −3과 합쳐 base 대비 +85다. 이 증가는 사용자가 허용했다. 리드 R-2(E/lead-check/r2-fix1.json, 스크립트 r2-fix1.mjs)가 같은 값을 다시 쟀다. 표본 대조이며 독립 판정이 아니다.
- 수정 작성자 생존 신호(E/lead-check/liveness-fix1.json): 신호 5, 최대 241초, 300초 초과 0. 정산: 빈 프롬프트 확인, `worker-release` retained, `terminal close`(08:20:47Z).
- 좁힌 재실사(수정 루프): 신규 `gpt-6-astra` xhigh(화면 「GPT-6-Astra xhigh」, backend unknown), Task `task_097db7d38c8f`, Dispatch `ctx_5bc3ee6aac42`, 2026-10-11T08:23:27Z~08:38:10Z, worker_done `msg_f85d0c0711fa`(succeeded). 계약 E/rereview-contract.md v1(SHA256 `9f110eb5…`, 고정 head `200354d2`), 판정 E/rereview/verdict.md, 기동 기록 E/rereview-start-note.md다. D1~D4는 해소됐다. 새 차단 R1(P3, 리드 기록)은 수정 결과 줄의 빈 줄 계수 단위 오류였고, 리드가 커밋 `407ff8dc`에서 고쳤다(같은 산출물 goal 결과 줄 수정 1회째). 설계 관찰 O-R1(bytes 증가분의 기준)도 같은 커밋에 반영했다. R1 정정의 독립 확인은 결과 기록 뒤의 바뀐 부분 재실사가 맡는다.
- 좁힌 재실사자 생존 신호(E/lead-check/liveness-rereview.json): 신호 7, 최대 270초, 300초 초과 0. 정산: 빈 프롬프트 확인, `worker-release` retained, `terminal close`(08:38:44Z).
- 우편함 대기: 리드의 background 대기가 08:29Z 무렵 메모리 회수로 꺼졌다. ORCA 「우편함 대기」대로 다시 켜지 않았고 메인에 알렸다(`msg_9156859f0d3b`). 그 뒤 Orca 알림을 받을 때마다 `--wait` 없는 check로 처리했다(E/session/bg-wait-events.md).
- 설계 관찰(비차단) 둘은 하위 분화 후보에 묶었다(「다음 계획 후보」): AGENTS 「모델 라우팅」의 「Opus 대체」 목적어가 따로 읽으면 모호하다. ORCA capacity 제목 아래의 safeguard 안내는 제목만 훑으면 찾기 어렵다.

### 종료 점검(Gardener)

- 점검자: 신규 `claude-opus-5-5`(화면 「Opus 5.5 with xhigh effort」, backend unknown), Task `task_fedf973ae3d6`, Dispatch `ctx_09c108f86171`, 2026-10-11T08:43:03Z~09:03:00Z, worker_done `msg_502689654297`(succeeded). 병합 전 고정 head `407ff8dc`(메인 판단 2). 계약 E/gardener-contract.md v1(SHA256 `fb5699cb…`), 보고 E/gardener/context-report.md, 기동 기록 E/gardener-start-note.md다. 고정 입력 34개(E/gardener-inputs.json)의 hash는 시작과 끝이 같았다.
- 집계: 같은 계약·결함 번호의 확정 실패 3회 도달 0(번호별 최대 1). 번호 결함 다섯(D1~D4·R1) 중 리드 귀속 넷(D1 리드 계약, D3·D4·R1 리드 기록). CI 실패·재실행 0이고, 고정 head의 CI 넷은 조회 때 진행 중이었다(E/pr229-ci/). 승인 없는 경고 억제·설정 완화·검사를 끈 우회 0.
- 드리프트 발견: 계약 인자 길이로 worker-start가 실행되지 않은 일이 네 goal에서 4회였고 점검 때는 BACKLOG에 근거가 없었다(결과 기록에서 `official-draft-check-helper`에 더했다). 「250줄 상한·200줄 목표」와 bytes 상쇄 조건이 정본에 없고 goal·계약에만 있다. draft 크기 차이는 넷 모두 계약 글자 수 + 4,788이며 정본은 단위를 적지 않는다. 메모리 회수로 꺼진 대기에 실린 ack와 Orca 자동 안내 문구가 정본에 없다. S1 check는 저장 출력의 모양으로 보아 `--peek`로 돌렸다고 Gardener가 추론했고(명령 원문은 저장 원시에 없음), 정본은 check의 형태를 정하지 않는다. R-1 리드 기동 명령에는 TEMP·TMP 설정이 없다. ORCA 「크래시 중단과 checkpoint」의 「복구 관측은 아래 기록 링크로 확인하며」(ORCA:167)는 이 PR 전부터 가리킬 링크가 없다.
- 반복: 공식 계약 draft 미제출은 Codex 기동 4/4로 앞 goal들과 이어진다. 인자 길이 실패는 네 goal 4회다. 작업자 heartbeat 초과는 작성 Sol 한 Dispatch였고, 메인 결정 뒤 계약에 상한을 다시 강조한 세 Dispatch는 0이었다(원인은 단정하지 않음). 리드 기록 수치 오류 둘(근거 줄 범위 자체 정정, R1)과 리드 계약이 만든 결함 둘(D1, D2를 통과시킨 상쇄 조건)이 있다. 가드의 리드 진입 차단 둘, 메모리 회수 하나가 앞 goal들과 이어진다. 재실사 뒤 goal 수정은 0이다.
- 리드 기록 대조: 표본 13 중 12 일치, 1 낡음(고정 head의 재개 지점·진척 단계). 이 결과 기록에서 고쳤다.
- 정리 후보 둘(동결 뒤 착수 전제): 공식 계약 기동 helper, 규칙 문서 크기·출처 연결 계측 helper와 크기 규칙의 정본 자리. 리드가 메인에 물었고(`msg_f2292fcea8ae`) 사용자가 둘 다 BACKLOG에 넣기로 했다(「적용 중인 사용자 결정」). 후보 1은 `official-draft-check-helper`에 근거를 더하고 범위를 계약 파일 전달까지 넓혔고, 후보 2는 새 대기 행 `rule-document-measure-helper`다. 메모리 회수로 꺼진 대기의 관찰은 `lead-mailbox-receive-delay`에 근거로 더했다.
- Gardener 생존 신호(E/lead-check/liveness-gardener.json): 신호 26, 최대 124초, 300초 초과 0. 정산: 대기 화면 확인, `worker-release` retained, `terminal close`(09:03:27Z).

### PR229 병합 전 재실사와 병합

병합 전 판정은 ORCA 병합 관문대로 근거 폴더와 승인 묶음으로만 전했고, PR229 병합 뒤 이 종료 기록에 옮긴다.

- 결과 기록: 커밋 `d352d719`(Gardener 결과와 사용자가 채택한 후보의 BACKLOG 반영).
- 바뀐 부분 재실사: 신규 `gpt-6-astra` xhigh(화면 「GPT-6-Astra xhigh」, backend unknown), Task `task_07e8fa5579f0`, Dispatch `ctx_c088dd0bf827`, 고정 head `d352d719`(범위 `200354d2..d352d719`), worker_done `msg_8e0638a664b8`(2026-10-11T09:34:23Z, succeeded). 판정 E/finalreview/verdict.md, 기동 기록 E/finalreview-start-note.md다. R1은 해소됐다. 리드 기록 결함 셋(P3)이 나왔다.
  - F1: 「종료 점검(Gardener)」 드리프트 줄이 S1 check의 형태를 Gardener의 추론에서 확인된 사실로 올려 적었다.
  - F2: 같은 줄이 ORCA 낡은 링크 구절의 절 이름을 잘못 적었다.
  - F3: 리드 메모가 `rule-document-subdivision` 이유에 Gardener 관찰 셋을 더하겠다는 계획을 정산하지 않았다.
- 정정: 리드가 커밋 `42294d10`에서 goal 한 줄(F1·F2)을 고치고 메모(F3)를 정산했다. 재실사가 goal 결함을 냈으므로 정정은 했고, 판정 문장은 goal에 넣지 않았다. goal 리드 기록 정정은 `200354d2`(D4)·`407ff8dc`(R1)·`42294d10`(F1·F2)의 3회이며 3회 초과 체크포인트에는 닿지 않았다.
- 좁힌 재확인: 신규 `gpt-6-astra` xhigh, Task `task_f2d345cacd9f`, Dispatch `ctx_0a03dc3c3219`, 고정 head `42294d10`, worker_done `msg_29cea433ec34`(10:07:05Z, succeeded). 판정 E/finalcheck2/verdict.md, 기동 기록 E/finalcheck2-start-note.md다. F1·F2는 해소, F3은 일부 해소였다. 새 G1(P3, 리드 메모)은 정산 줄이 Gardener 관찰 1·4·5를 모두 보고서 6절 「뽑지 않은 것」으로 설명한 원천 귀속 오류다. 5는 보고서 4절이 기존 `rule-document-subdivision` 정리 대상으로 적은 것이다. 리드가 근거 폴더 메모만 고쳤고 추적 파일과 head는 그대로다(E/pr-lead-context.md 「수정 차례 4」).
- G1 재확인: 신규 `gpt-6-astra` xhigh, Task `task_4c96719eaa65`, Dispatch `ctx_54430399b6a2`, 고정 head `42294d10`과 메모 SHA256 `af42bfa8…`, worker_done `msg_2ef259452ae1`(10:29:57Z, succeeded). 판정 E/finalcheck3/verdict.md, 기동 기록 E/finalcheck3-start-note.md다. 통과했다. G1·F3이 해소됐고 새 결함은 없다.
- 확인 불가 하나: 같은 메모 파일 안에서 절 추가와 줄 정정의 실제 순서다. Git 제외 파일이라 커밋 시각이 없고 리드 세션 기록은 읽기 금지다. 재확인자는 이 사실을 통과 근거에서 뺐다. 리드는 계약 경계 질문 `msg_d556c5d63183`에 그 한 사실만 확인 한계로 남긴다고 답했다(`msg_2c6cddfd2c7f`).
- 생존 신호(helper): 재실사 최대 241초, 재확인 최대 164초, G1 재확인 최대 188초이며 300초 초과는 0이다(E/lead-check/liveness-finalreview.json·liveness-finalcheck2.json·liveness-finalcheck3.json).
- 승인 묶음: fresh 조회에서 origin/main은 base `6782cd67`(10:30:27Z), PR229는 OPEN·CLEAN·MERGEABLE(10:30:33Z), head `42294d10`의 CI 넷(code-rules·module-boundaries·architecture-tests·dotnet-tests)은 모두 성공이었다(E/session/fetch-before-merge-ready.txt, E/pr229-ci/pr-state-final.json·runs-final.json). 리드 status `msg_c723d607fb20`(10:32:01Z, 본문 E/session/approval-bundle-body.md)로 올렸다.
- 병합(메인 전달 `msg_a01a200eb1e3`, 10:36:32Z): 사용자가 메인 pane에 「병합 승인: PR229 head 42294d10929c11f795c5ab884f9c061473c8c851」를 제출했다. 메인이 head와 CLEAN·MERGEABLE을 다시 대조하고 단독 병합 명령으로 병합했다. merge commit `704baa5e52666da97eabaf20249a994b33700db8`(2026-10-11T10:35:55Z)의 둘째 부모가 승인 head이고, 트리는 승인 head와 같다(E/lead-check/closeout-measure.txt).

### 완료조건 대조

병합 commit `704baa5e` 기준이다. 원시는 E/lead-check/closeout-measure.txt와 E/lead-check/candidate-check-closeout.json이다.

1. 충족. S1~S6이 ORCA·RESUME에 들어갔다. 문서 실사(E/review/verdict.md)가 원천과 현실 시나리오 다섯을 보았고, 차단 D1~D4는 좁힌 재실사(E/rereview/verdict.md)에서 해소됐다.
2. 충족. BACKLOG 새 행 다섯과 기존 행 갱신이 들어갔다. 종료 기록 때 리드가 다시 돌린 후보 도착 검사는 `allowed`, exit 0, BACKLOG ID 114, 중복 0, 후보 10, 인용 없는 후보 0, 모르는 ID 0이다. 실사자 재실행은 문서 실사와 바뀐 부분 재실사가 했다.
3. 충족. 첫 기록 셋(재개 지점의 PR228 줄, 「첫 기록」, 「위험」 4)과 사용자 결정 원문(「적용 중인 사용자 결정」)이 있다.
4. 충족. base `6782cd67`...`704baa5e`의 바뀐 파일은 「건드릴 곳」의 여섯뿐이다(+276 −34). 코드·다른 파트 파일 변경은 0이고, Codex 영역은 승인된 스킬 한 문장뿐이다.
5. 충족. ORCA는 245줄이다. 여섯 파일 묶음은 103,306 bytes로 base 대비 +85이며 사용자가 허용했다.
6. 충족. 실사 통과 → Gardener → 결과 기록과 재실사 → PR229 사용자 병합 승인 → 종료 기록 PR231 실사·승인·병합 → 종료 점검 → R-8까지 끝났다(「PR231 병합과 종료 점검」). 이 줄은 PR231 병합 뒤 다음 Rules goal의 첫 커밋에서 고쳤다.

### PR231 병합과 종료 점검

PR231 병합 뒤 다음 Rules goal의 첫 커밋으로 넣은 절이다. 원천은 이전 리드의 종료 인계 E/closeout-handoff.md와 종료 보고 `msg_302ba5bdcae3`(2026-10-11T11:32:27Z)다.

- 종료 기록 PR231 문서 실사(E/closeout-review/verdict.md, Task `task_4ea10ad4efa9`, worker_done `msg_d3e3b64b873a` 10:55:56Z): goal·CURRENT 사실 대조, 직접 계측, 후보 검사 재실행, 진척 단계 형식은 지적이 없었다. C1(P3, 리드 메모 자기 쓰기 감사 누락)이 차단이었다.
- C1 재확인(E/closeout-recheck/verdict.md, Task `task_f62b4dabc5d2`, `msg_2b65799834b0` 11:13:11Z): C1은 일부 해소였다. H1(Git 제외 수 66 → 64)과 H2(감사 JSON 생성 시각 설명 역순)가 차단이었다.
- H1·H2 재확인(E/closeout-recheck2/verdict.md, Task `task_ed932ded6b2d`, `msg_8361a1a234fb` 11:26:36Z): 통과했다. C1·H1·H2가 해소됐고 새 결함은 없다. 같은 메모 안의 실제 쓰기 순서는 확인 불가라 통과 근거에서 뺐다.
- 정정은 모두 Git 제외 리드 메모 E/closeout-lead-context.md에서만 했고 head는 실사 때와 같다. 승인 묶음은 status `msg_b158e02c9d22`였다(fresh 조회 CLEAN·MERGEABLE, CI 넷 성공, E/pr231-ci/).
- 병합(메인 전달 `msg_6033d8d4a1eb`, 11:30:07Z): 사용자가 메인 pane에 「병합 승인: PR231 head 0d24340475fdbb2fbfce5c7d06adc8e3db107dda」를 제출했다. 메인이 head와 OPEN·MERGEABLE·CLEAN을 다시 대조하고 단독 병합 명령으로 병합했다. merge commit `a570dbf333ce98b1ca8318338e85295056041844`(mergedAt 2026-10-11T11:29:47Z)의 첫째 부모는 `999b1b98`(PR230 병합), 둘째 부모는 승인 head `0d243404`다. merge commit의 이 goal 파일은 승인 head와 같다. CURRENT는 Content 줄(PR230)만 다르고 Rules 줄은 보존됐다(E/lead-check/pr231-merge-check.txt).
- 종료 점검(이전 리드, 종료 보고 `msg_302ba5bdcae3`):
  - 완료조건: 1~5는 「완료조건 대조」대로 충족이다. 6은 그 절의 줄을 충족으로 고쳤다.
  - 「위험」 대조: 1(bytes)은 사용자가 +85 bytes를 허용해 닫혔다. 2(S3와 AGENTS capacity 구절)는 문서 실사가 직접 모순 없음으로 판정했다. 「Opus 대체」 목적어 모호는 설계 관찰 O-1로 `rule-document-subdivision`에 묶였다(E/review/verdict.md:77·205). 3(새 진입 문장의 적용 시점)은 떠 있는 리드가 안전 지점에 main을 받을 때 적용된다. 4(한 파일 역할과 메모 순서)는 BACKLOG `single-output-role-memo-first`로 남았다.
  - BACKLOG: 이 goal이 새 행 다섯을 넣고 기존 행을 갱신했다. 후보 도착 검사는 allowed, ID 114, 후보 10이다. 종료 점검에서 새 행은 없었다.
  - CI: PR229 head `42294d10`와 PR231 head `0d243404` 모두 넷 성공이다. 시험·빌드·게임·DB는 문서만이라 해당 없다.
  - 생존 신호(helper, Dispatch 열하나): 작성 Sol 한 Dispatch가 최대 411초로 300초 초과 셋이었다(메인 결정 `msg_9b35d66297d6`으로 비차단 운영 기록). 나머지 열은 초과 0이며 최대는 264·241·270·124·241·164·188·216·229·197초다(E/lead-check/liveness-*.json).
  - 우편함 대기: 08:29Z 메모리 회수로 꺼진 뒤 다시 켜지 않았고 Orca 안내마다 check로 처리했다.
- R-8(메인): 메인은 종료 보고를 받으면 이 goal의 리드 pane을 닫고 새 Rules 리드를 연다고 알렸다(`msg_6033d8d4a1eb`). 새 리드가 2026-10-11T11:45Z 무렵 저장한 terminal 목록(다음 goal 근거 폴더 `.backups/verification/2026-10-11-routing-hotfix-canon/session/terminal-list-1145.json`)에서 rules-active 터미널은 새 리드 handle `term_991652c5-…` 하나다. 이 goal의 리드 handle `term_b1fddc67-…`는 그 목록에 없다. 새 리드는 새 Run `run_bea2c841fa15`로 진입했다(메인 진입 지시 `msg_300feadfab41`).
- 리드 기록 품질(메인 지시로 새 리드의 첫 계약 점검 항목): 이 goal의 리드 기록 결함은 열 개다(D3·D4·R1·F1·F2·F3·G1·C1·H1·H2, 모두 P3 이하, 리드 기록 귀속). 유형은 셋이다. 수치·시각·원천을 원시에서 옮기지 않고 요약하다 틀림(R1·F2·G1·H1·H2), 메모 필수 정산 누락(D3·F3·C1), 확인 수준 격상(F1·D4)이다. 판정 밖 발견으로 E/pr-lead-context.md의 「수정 차례 3」·「수정 차례 4」 절에도 자기 쓰기 감사 줄이 없다.
- 메모 감사 누락은 D3에 이어 C1이 두 번째 발생이다. 이전 리드는 BACKLOG 등록 여부를 사용자 몫으로 남겼다. 메인이 목표 루프 「후보는 아이디어마다 승인을 묻지 않는다」에 따라 등록을 판단했다(`msg_ded7bd2d6839`, 사용자 결정 아님). 다음 Rules goal이 BACKLOG `lead-memo-audit-field-check`로 넣었다.

### 첫 기록

교정 정본에 따라 기록한다. 이 goal 범위를 넓히지 않는다.

- 재검토 뒤 goal 수정(두 번째 이탈, 메인 판단 `msg_c3df82b7150e`): ORCA [병합 관문](../../../00_Document/operations/ORCA.md#merge-gate)은 「재실사 결과는 goal을 고치지 않고 근거 폴더·승인 묶음으로만 전한다」고 이미 정한다. 첫째는 PR214의 `ef7c943c`(「docs: record the closeout re-review pass」, 2026-10-09T10:51:38+09:00), 둘째는 PR228의 `a09ca3f1`(「docs: record the PR228 narrowed re-audit pass」, 2026-10-11T00:47:43+09:00)이고 `247829ad`(00:50:51+09:00)로 되돌렸다. 금지 문장이 있는데 반복됐으므로 다음 층은 승인 묶음 전 재실사 고정 head 대비 바뀐 파일을 보여 주는 helper다. 도구라 동결 뒤다(BACKLOG `rereview-head-diff-helper`).

## 정본에서 옮긴 적용 기록

규칙 문서 bytes를 상쇄하려고 ORCA에서 옮긴 출처 가리킴 구절 열셋이다. O1은 실사 D2로 ORCA에 되돌려, 지금 옮겨 둔 것은 열둘(1,551 bytes)이다. 원래 위치는 고정 HEAD `497324de150c70a39305e7575040fdc3fa09598b`의 `00_Document/operations/ORCA.md` 줄 번호다. 원문은 작성자 원시(E/sentences/work/offset-original-O*.txt)에서 생성했다. 상대 링크는 이 goal에서 같은 대상에 닿도록 고쳐 썼고 링크 밖 원문은 그대로다. 앞뒤 공백은 인용에서 뺐다.

- O1, ORCA:21(127 bytes, 실사 D2로 ORCA:21에 되돌림): PR2 원천은 [goal 항목별 출처](../2026-10-06-merge-gate-canon-refresh/goal.md#pr2-canon-sources)다.
- O2, ORCA:23(171 bytes): 승인 출처·시각과 전달 원문은 [출처 이관 대조표](../2026-10-05-operating-canon/goal.md#orca-source-relocation-r2)에서 확인한다.
- O3, ORCA:30(142 bytes): 승인 응답·시각은 [다섯 리드 승인 출처](../2026-10-05-operating-canon/goal.md#orca-source-leads)에 있다.
- O4, ORCA:32(115 bytes): ([배치 결정](../2026-10-06-merge-gate-canon-refresh/goal.md#적용-중인-사용자-결정))
- O11, ORCA:73(101 bytes): [승인 출처](../2026-10-05-operating-canon/goal.md#orca-source-r3-reply)대로
- O5, ORCA:123(130 bytes): 당시 관측은 [이관 기록](../2026-10-06-merge-gate-canon-refresh/goal.md#orca-moved-history)에 있다.
- O6, ORCA:139(185 bytes): 사용자 결정과 설정 경로의 출처는 [Unity opt-in 적용 기록](../2026-10-05-operating-canon/goal.md#pr183-제출-뒤-적용한-사용자-결정)이다.
- O7, ORCA:169(129 bytes): 근거는 [checkpoint 승인 출처](../2026-10-05-operating-canon/goal.md#orca-source-checkpoint)에 있다.
- O8, ORCA:180(182 bytes): 근거는 [계약 draft 복구 승인 출처](../2026-10-05-operating-canon/goal.md#orca-source-draft)와 [크기 추정·사후 대조 결정](../../../00_Document/operations/ORCA.md#pr2-source)이다.
- O10, ORCA:192(167 bytes): 기존 goal 검토 시범을 이 형태로 넓힌 [승인 결정](../2026-10-05-operating-canon/goal.md#요구사항-원천과-적용-결정)이며
- O12, ORCA:210(35 bytes): ([원천](../../../00_Document/operations/ORCA.md#orca-tool-observations))
- O13, ORCA:230(37 bytes): ([운영 판단 원천](../../../00_Document/operations/ORCA.md#pr2-source))
- O9, ORCA:247(157 bytes): 근거는 [HANDOFF 결정 5](../../../.backups/verification/2026-10-05-operating-canon/sources/handoff-decisions.md)와 운영 정본 goal의 승인 범위다.

ORCA 안에 남은 `#pr2-source` 링크(R-8 「메인 운영 판단」 등)는 O1이 있는 ORCA:21에서 PR2 원천으로 이어진다. 상쇄 때 이 연결을 놓친 것이 실사 D2다.

## 다음 계획 후보

이 goal 밖으로 둔 일이다. 후보마다 BACKLOG ID를 단다.

- 재실사 뒤 goal 수정을 막는 helper(「첫 기록」): 승인 묶음 직전 재실사 고정 head 대비 바뀐 파일 목록을 내 goal 파일 변경을 드러낸다. BACKLOG `rereview-head-diff-helper`.
- 터미널 작업 공간 대안 재검토(진입 지시의 등록 누락 후보 1): Orca→Herdr 전면 이주는 보류됐고 다시 볼 조건만 남긴다. BACKLOG `terminal-workspace-alternative-review`.
- 병합 거부 문구(진입 지시의 등록 누락 후보 2): 병합 관문 hook의 거부 문구 앞머리에 정상 동작·사용자 영역·우회 금지·다음 행동을 넣는다. `99_Tools/MergeGate/merge-policy.mjs` 코드라 동결 뒤다. BACKLOG `merge-gate-deny-wording`.
- 한 파일만 쓰는 역할과 메모 순서(「위험」 4): 실제 발생 0회라 정본을 바꾸지 않았다. BACKLOG `single-output-role-memo-first`.
- 메인 우편함 대기 겹침의 셋째 발생: 도구라 동결 뒤다. BACKLOG `mailbox-wait-overlap-guard`.
- goal 기록 표류의 나머지(끝난 goal 줄 정리 주체·검사): 이 goal은 적용 결정 링크만 고쳤다. BACKLOG `goal-state-drift`.
- 규칙 문서 하위 분화(실사 뒤 사용자 조건 「너무 길어지면 하위 문서로 분화」): 수정 뒤 ORCA는 250줄 상한 안이라 이 PR은 출처 복원만 했다. 문서 재구성 때 실사의 설계 관찰 둘(AGENTS 「Opus 대체」 목적어, safeguard 안내의 제목 탐색)도 함께 본다. BACKLOG `rule-document-subdivision`.
- 공식 계약 기동 helper(Gardener 후보 1, 사용자 채택): 계약 파일을 셸 인자 없이 worker-start에 넘기고 `turn_start_unobserved`일 때 draft 크기를 글자 수로 대조한다. 동결 뒤다. BACKLOG `official-draft-check-helper`.
- 규칙 문서 크기·출처 연결 계측 helper와 크기 규칙의 정본 자리(Gardener 후보 2, 사용자 채택): 정책 값은 착수 때 사용자에게 묻는다. 동결 뒤다. BACKLOG `rule-document-measure-helper`.
- 메모리 회수로 꺼진 대기에 실린 ack와 Orca 자동 안내 문구(Gardener G2-1): 근거를 더했다. BACKLOG `lead-mailbox-receive-delay`.
