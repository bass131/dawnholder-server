# 규칙·운영 동결 전 문서 정리

- 만드는 것: 반복된 실수를 막는 규칙 문장 둘, 정본에 아직 없는 사용자 결정 셋의 문장, 낡은 링크 정정을 운영 문서 두 파일(ORCA·RESUME)과 CURRENT 하단 링크에 넣는다.
- 덧붙여 대기 목록(BACKLOG)에 빠진 후보 셋을 등록하고 기존 행을 갱신한다. 도구(hook·helper·검사 스크립트)는 만들지 않고 동결 뒤 후보로만 남긴다.
- PR 하나, 문서만 바꾼다. 코드·Codex 영역(AGENTS.md·`.agents/`)·다른 파트 goal은 건드리지 않는다.

## 재개 지점

기능 동결(10-28) 전에 닫는 작은 Rules goal이다. 사용자가 범위를 승인했다(「적용 중인 사용자 결정」). 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch: `docs/pre-freeze-rule-docs-20261011`(base main `6782cd67`). 정확한 head는 원격 branch와 승인 묶음에서 확인한다.
- 근거 폴더 E: `.backups/verification/2026-10-11-pre-freeze-rule-docs/`(Git 제외). 승인 범위는 [scope-draft-v1.md](../../../.backups/verification/2026-10-11-pre-freeze-rule-docs/scope-draft-v1.md)(SHA256 `b783ae27…`)와 메인 판단 넷(「요구사항 원천과 적용 결정」)이다. 리드 맥락 메모는 E/lead-context.md(진입·범위 초안)와 E/pr-lead-context.md(착수)다.
- 리드: 신규 `claude-opus-5-5` xhigh(화면 「Opus 5.5 ⚡xhigh」, backend unknown), 태그 `[Rules 리드 Opus]`, handle `term_b1fddc67-1e99-4471-af0b-8fe6c295d371`, Run `run_d447bca7c344`(회신 주소 `run:run_d447bca7c344`). 메인 주소는 메인 term handle이다. 이전 Rules goal의 Run·Task·Dispatch·handle은 실행 권한이 아니다.
- 앞 goal의 끝: [세션 쓰기 가드와 생존 신호 간격 도구](../2026-10-10-operating-tool-guards/goal.md)는 PR224·PR228 병합으로 끝났다. PR228 좁힌 재실사 통과(근거 rules-active [E/closeout-rereview/verdict.md](../../../.backups/verification/2026-10-10-operating-tool-guards/closeout-rereview/verdict.md)).
- **현재 위치**: 정본 문장 여섯과 BACKLOG 정리를 마쳤고 리드 R-2가 보고와 원시의 일치를 확인했다(「현재 결과 / 정본 문장 작성」·「대기 목록 정리」). bytes는 상쇄로 3 bytes 줄어 사용자 질문이 없다. PR229를 만들었고 문서 실사를 연다.
- **남은 순서**: 문서 실사 → Gardener → 결과 기록·재실사 → 승인 묶음 → 사용자 병합 승인 → 종료 점검 → R-8.
- **사용자 차례**: 병합 승인 줄이다.

## 진척 단계

- [x] 범위와 기준 확정
- [x] 정본 문장 작성
- [x] 대기 목록 정리
- [>] 문서 실사
- [ ] 종료 점검자 확인
- [ ] 결과 기록·재실사
- [ ] PR229 병합
- [ ] 종료 점검과 R-8

PR229(https://github.com/bass131/dawnholder-server/pull/229)를 2026-10-11에 만들었다.

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
3. BACKLOG: 새 절 「동결 전 문서 정리 goal에서 연결한 후보」에 새 행 셋(`rereview-head-diff-helper`, `terminal-workspace-alternative-review`, `merge-gate-deny-wording`). 기존 행 갱신: 이 goal로 승격 넷(`session-entry-memo-order`, `advisor-model-alternative-canon`, `claude-safeguard-verifier-switch`, `worker-pane-placement-wording`), 근거 더하기 둘(`mailbox-wait-overlap-guard` 셋째 발생, `goal-state-drift` 처리한 부분), `single-output-role-memo-first`에 S5가 메모 이름 조건을 정본에 두었다는 한 구절.
4. CURRENT Rules 줄(이 goal 링크·경로·branch)과 하단 링크(S6과 같은 문장).

### 건드릴 곳

| 파일 | 쓰는 사람 | 내용 |
|---|---|---|
| `00_Document/operations/ORCA.md` | 신규 Sol | S1~S4, bytes 상쇄로 옮길 지난 기록 구절 |
| `00_Document/operations/RESUME.md` | 신규 Sol | S5·S6 |
| 이 goal | 리드 | 범위·진척 단계·첫 기록·결정·결과 |
| `00_Document/operations/BACKLOG.md` | 리드 | 새 행 셋, 기존 행 갱신 |
| `00_Document/operations/CURRENT.md` | 리드 | Rules 줄과 하단 링크. 다른 파트 줄은 보존 |

Sol과 리드는 파일이 겹치지 않는다. commit·push는 리드만 한다.

### 하지 않을 것

- hook·helper·검사 스크립트·CI 변경. 필요한 도구는 BACKLOG 행만 둔다.
- 진입 지시가 동결 뒤로 넘긴 목록: helper·hook 구현, `mailbox-wait-overlap-guard` 도구, `official-draft-check-helper`, `lead-measured-fact-recompute`·`worker-harness-only-execution`의 도구화, 운영 셋업 마일스톤의 하네스·STE100·HTML 보고·자기 개선 루프, `credential-separation-followup`, `heartbeat-interval-review`, `session-guard-followup`(10-31 평가).
- `single-output-role-memo-first`의 정본 변경(R-7 「유일한 출력」 문구 포함).
- Codex 영역(AGENTS.md·`.agents/`)과 CLAUDE.md, `.claude/` 설정, 메인 개인 도구.
- 다른 파트 goal, 게임 코드, CURRENT의 다른 파트 줄, 끝난 Rules goal(2026-10-10-operating-tool-guards).
- 규칙 문서 가지치기(10-31 Gardener 평가).

### 관찰 가능한 완료조건

1. S1~S6이 ORCA·RESUME에 들어가고 각 문장이 원천(사용자 원문·메인 전달 msg ID·반복 근거 행)과 맞는다. 신규 독립 실사자가 원천 대조와 현실 시나리오 다섯(새 리드 진입과 가드, 미확인 메일이 있는 채 작업자 pane 닫기 직전, 확정 실패 3회 뒤 Advisor 고르기, Opus 검증자의 safeguard 선택창, 다른 worktree 작업자 띄우기)으로 확인한다.
2. BACKLOG 새 행 셋·기존 행 갱신이 항목 계약(7필드, 상태 세 값)을 지키고, 후보 도착 검사(`node 99_Tools/Backlog/check-candidates.mjs --backlog 00_Document/operations/BACKLOG.md --goal 01_Phases/goals/2026-10-11-pre-freeze-rule-docs/goal.md`)가 `allowed`·중복 0이다. 리드 실행 원시를 E에 두고 실사자가 같은 명령을 다시 돈다.
3. 이 goal에 첫 기록 셋과 이번 사용자 결정 원문이 있다.
4. 변경 경계: `git diff --stat <base>...HEAD`에 위 다섯 파일만 있다. 코드·Codex 영역·다른 파트 파일 변경 0이다.
5. 규칙 문서 크기: ORCA 250줄 이하. 여섯 파일 묶음 합계는 「PR 경계와 검증」의 bytes 줄대로 잰다.
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

## 적용 중인 사용자 결정

모두 메인 창 Enter 제출, 메인 전달이다. 직접 입력으로 격상하지 않는다.

- **다음 Rules 목표**(진입 지시 `msg_68b1c72e0160`, 2026-10-11 15:29 KST 무렵): 「대시보드 결정 응답: 1) Rules 다음 목표 - 기능 동결(10-28)까지 닫아 둘지 → A 작은 문서 목표 지금」. 선택지 효과 원문: 「새 Rules 리드를 지금 열어 반복 실수 규칙 문장과 정본에 빠진 사용자 결정 셋, 대기 목록 등록만 묶은 작은 범위 초안을 받는다. hook·helper 같은 도구는 동결 뒤로 미룬다.」 메인 추천은 B(동결 뒤)였고 사용자가 A를 골랐다. 계기는 15:19 KST 무렵 사용자 코멘트 「룰쪽 더 해야하는 부분 있나?」에 메인이 읽기 전용 조사로 답한 것이다.
- **범위 승인**(메인 전달 `msg_935fcc451ce8`, 2026-10-11 15:4x KST): 「대시보드 결정 응답: 1) 계획 검토 - World 다중 계정 로그인 goal 범위 초안 v1 → A 승인 (초안 msg_148c21badf30) · 2) 계획 검토 - Rules 동결 전 규칙 문서 정리 범위 초안 v1 → A 승인 (초안 msg_37a299c91578)」. 이 goal은 2)다. 초안 그대로이며 코멘트가 없어 S3은 초안대로 「Opus 검증자」 한정이다. 메인은 정리본에서 「사용자 원문은 검증자로 한정하지 않았다」는 관찰을 사용자에게 보였고 사용자는 코멘트 없이 승인했다.

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
- 생존 신호(helper, E/lead-check/liveness-sol.json): 신호 7, 최대 411초. 300초 초과는 셋(307초 07:02:08Z→07:07:15Z, 343초 07:07:15Z→07:12:58Z, 411초 07:12:58Z→07:19:49Z)이다. 작성자는 411초를 스스로 알렸다(status `msg_46bd296690cc`). 작성자 귀속 비차단 운영 기록이다.
- 정산: 빈 프롬프트 확인, `worker-release` retained(external_terminal), `terminal close`(07:23:52Z).

### 대기 목록 정리

- 리드가 E/backlog-edit.mjs로 고쳤다(결과 E/backlog-edit-result.json). 승격 넷, 근거 더하기 셋(`mailbox-wait-overlap-guard`·`goal-state-drift`·`single-output-role-memo-first`), 새 절의 행 셋이다. 스크립트는 고친 행마다 한 번만 있고 일곱 칸인지 단정했다. 단어 단위 diff로 덧붙인 글만 바뀐 것을 확인했다.
- 후보 도착 검사(E/lead-check/candidate-check-1.json): `allowed`, exit 0, BACKLOG ID 112, 중복 0, 후보 6, 인용 없는 후보 0, 모르는 ID 0.
- CURRENT 하단 링크는 S6 첫 문장으로 고쳤다(메인 판단 3).

### 첫 기록

교정 정본에 따라 기록한다. 이 goal 범위를 넓히지 않는다.

- 재검토 뒤 goal 수정(두 번째 이탈, 메인 판단 `msg_c3df82b7150e`): ORCA [병합 관문](../../../00_Document/operations/ORCA.md#merge-gate)은 「재실사 결과는 goal을 고치지 않고 근거 폴더·승인 묶음으로만 전한다」고 이미 정한다. 첫째는 PR214의 `ef7c943c`(「docs: record the closeout re-review pass」, 2026-10-09T10:51:38+09:00), 둘째는 PR228의 `a09ca3f1`(「docs: record the PR228 narrowed re-audit pass」, 2026-10-11T00:47:43+09:00)이고 `247829ad`(00:50:51+09:00)로 되돌렸다. 금지 문장이 있는데 반복됐으므로 다음 층은 승인 묶음 전 재실사 고정 head 대비 바뀐 파일을 보여 주는 helper다. 도구라 동결 뒤다(BACKLOG `rereview-head-diff-helper`).

## 정본에서 옮긴 적용 기록

규칙 문서 bytes를 상쇄하려고 ORCA에서 옮긴 출처 가리킴 구절 열셋이다. 원래 위치는 고정 HEAD `497324de150c70a39305e7575040fdc3fa09598b`의 `00_Document/operations/ORCA.md` 줄 번호다. 원문은 작성자 원시(E/sentences/work/offset-original-O*.txt)에서 생성했다. 상대 링크는 이 goal에서 같은 대상에 닿도록 고쳐 썼고 링크 밖 원문은 그대로다. 앞뒤 공백은 인용에서 뺐다.

- O1, ORCA:21(127 bytes): PR2 원천은 [goal 항목별 출처](../2026-10-06-merge-gate-canon-refresh/goal.md#pr2-canon-sources)다.
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

ORCA 안에 남은 `#pr2-source` 링크(R-8 「메인 운영 판단」 등)가 가리키던 PR2 원천은 위 O1이다.

## 다음 계획 후보

이 goal 밖으로 둔 일이다. 후보마다 BACKLOG ID를 단다.

- 재실사 뒤 goal 수정을 막는 helper(「첫 기록」): 승인 묶음 직전 재실사 고정 head 대비 바뀐 파일 목록을 내 goal 파일 변경을 드러낸다. BACKLOG `rereview-head-diff-helper`.
- 터미널 작업 공간 대안 재검토(진입 지시의 등록 누락 후보 1): Orca→Herdr 전면 이주는 보류됐고 다시 볼 조건만 남긴다. BACKLOG `terminal-workspace-alternative-review`.
- 병합 거부 문구(진입 지시의 등록 누락 후보 2): 병합 관문 hook의 거부 문구 앞머리에 정상 동작·사용자 영역·우회 금지·다음 행동을 넣는다. `99_Tools/MergeGate/merge-policy.mjs` 코드라 동결 뒤다. BACKLOG `merge-gate-deny-wording`.
- 한 파일만 쓰는 역할과 메모 순서(「위험」 4): 실제 발생 0회라 정본을 바꾸지 않았다. BACKLOG `single-output-role-memo-first`.
- 메인 우편함 대기 겹침의 셋째 발생: 도구라 동결 뒤다. BACKLOG `mailbox-wait-overlap-guard`.
- goal 기록 표류의 나머지(끝난 goal 줄 정리 주체·검사): 이 goal은 적용 결정 링크만 고쳤다. BACKLOG `goal-state-drift`.
