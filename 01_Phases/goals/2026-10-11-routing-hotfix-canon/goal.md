# 라우팅 핫픽스 정본 반영

- 만드는 것: 지금 쓰는 모델 라우팅 핫픽스 사용자 결정 넷을 운영 정본(ORCA)과 목표 루프 스킬에 넣는다. 리드 압축 기준, 같은 메시지로 두 번 깨지 않기, 로컬 DB 도구의 검증자, 10-31 평가 안건이다.
- 덧붙여 이전 Rules goal의 마지막 기록을 이월하고, 대기 목록(BACKLOG)에 새 후보 둘을 넣고 기존 행 둘을 갱신한다. 도구는 만들지 않는다.
- PR 하나, 문서만 바꾼다. Codex 영역은 사용자 결정 3이 승인을 겸한 목표 루프 스킬 한 파일뿐이다. 정본은 이 goal에서 한 번만 고치고, 각 worktree는 병합 뒤 main을 받아 정착시킨다.

## 재개 지점

기능 동결(10-28) 전의 작은 Rules goal이다. 메인이 범위를 승인했다(「적용 중인 사용자 결정」). 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch: `docs/routing-hotfix-canon-20261011`(base main `a570dbf3`). 정확한 head는 원격 branch와 승인 묶음에서 확인한다.
- 근거 폴더 E: `.backups/verification/2026-10-11-routing-hotfix-canon/`(Git 제외). 승인 범위는 [scope-draft-v1.md](../../../.backups/verification/2026-10-11-routing-hotfix-canon/scope-draft-v1.md)(SHA256 `32c0186e…b927`)와 메인 승인의 답 넷(「요구사항 원천과 적용 결정」)이다. 리드 맥락 메모는 E/lead-context.md이고 차례마다 절을 더한다.
- 리드: 신규 `claude-opus-5-5` xhigh(화면 「Opus 5.5 ⚡xhigh」, backend unknown), 압축 기준 `--autocompact 250000`은 메인 기동값이며 화면에서 확인하지 못했다. 태그 `[Rules 리드 Opus]`, handle `term_991652c5-0324-450d-9208-3d8c9663777f`, Run `run_bea2c841fa15`(회신 주소 `run:run_bea2c841fa15`). 메인 주소는 메인 term handle이다. 이전 Rules goal의 Run·Task·Dispatch·handle은 실행 권한이 아니다.
- 앞 goal의 끝: [규칙·운영 동결 전 문서 정리](../2026-10-11-pre-freeze-rule-docs/goal.md)는 PR229·PR231 병합으로 끝났다. 마지막 기록은 이 goal의 시작 커밋으로 넣었다(「현재 결과 / 착수」).
- **현재 위치**: 문서 실사. 정본 문장(Sol)과 BACKLOG(리드)를 마쳤고 리드 대조를 통과했다. bytes 초과는 메인이 허용 안으로 판단했다(「현재 결과 / 정본 문장과 리드 대조」). PR을 연 뒤 신규 Astra 문서 실사를 연다.
- **남은 순서**: PR 생성 → 문서 실사 → Gardener → 결과 기록 → 바뀐 부분 재실사 → 승인 묶음 → 사용자 병합 승인 → 종료 점검 → R-8. 다음 goal은 자동으로 시작하지 않는다.
- **사용자 차례**: 지금은 없다. bytes 초과(+174)는 메인이 사용자 허용 안으로 판단했다(메인 판단, 「현재 결과」).

## 진척 단계

- [x] 범위와 기준 확정
- [x] 정본 문장 작성
- [x] 대기 목록 정리
- [>] 문서 실사
- [ ] 종료 점검자 확인
- [ ] 결과 기록·재실사
- [ ] 병합
- [ ] 종료 점검과 R-8

## 범위

승인 범위는 초안 v1에 메인 답 넷을 더한 것이다. 초안과 달라진 곳은 결정 2의 문장(메인 정정판)과 ORCA 91행 보존(Q1 B)이다.

### 만들 것

1. 정본 문장 넷. 작성자는 원천 원문과 아래 의도를 받아 문장을 쓴다. 제목·anchor는 바꾸지 않는다. 특히 `검증자-모델-시범2026-10-31까지` anchor는 여러 파일이 링크한다.
   - 결정 1(ORCA 리드 압축 기준): R-1 리드 기동 명령(43행)을 `--command "claude --model claude-opus-5-5 --effort xhigh --autocompact 250000"`으로 바꾼다. 크래시·업데이트 복구(88행)와 R-8 목표 중간 교체(206행)의 `claude --resume`에도 같은 옵션을 붙인다. R-1 코드 블록 바로 뒤 문단(45행, 초안의 「44행 문단」)에 세 사실을 한 번만 적는다. 옵션은 그 실행에만 걸리고 전역 설정은 바꾸지 않는다. 기준을 넘은 대화를 이어 띄우면 첫 턴에 바로 압축되므로(2026-10-11 Core 27만→8.6만 관측) 재개에 필요한 것은 goal 재개 지점과 근거 폴더에 둔다. 압축 뒤 맥락 손실로 일을 다시 한 사례가 한 번이라도 나오면 리드가 바로 메인에 보고하고 메인이 모든 리드를 50만으로 되돌린다.
   - 결정 2(ORCA 「Orca 도구 관측과 우편함 대기」 90~92행): 메인 정정판 문장을 넣는다(「네」를 「리드」로). 「놓친 메시지는 메인 화면 감시가 5분 넘게 안 읽힌 완료·질문 메시지를 잡는다」를 같은 자리에 둔다. 91행 「메모리 회수로 대기가 꺼지면 스스로 다시 켜지 않는다」는 그대로 두고, 새 문장이 91행과 같은 내용을 되풀이하지 않게 쓴다(Q1 B).
   - 결정 3(목표 루프 스킬 「검증자 모델 시범」 54행 뒤 새 줄): 게임 영속 데이터를 바꾸지 않는 로컬 DB 설치·기동 도구와 실행 카드(SQL Server 인스턴스 설치·기동, 컨테이너 엔진 카드 등)를 54행의 DB 예외에서 빼고 신규 `gpt-6-astra` xhigh가 검증한다. 실제 경로 1회 실행(사용자 실행 카드 포함)은 그대로다. 사용자가 실행한 카드에서 독립 검증이 놓친 결함이 한 번이라도 나오면 Opus로 되돌린다. 카드 회차 수와 놓친 결함을 goal에 기록한다. 게임 영속 데이터의 스키마·저장 로직은 지금처럼 Opus가 검증한다. 53행(Astra 구현의 검증자는 Opus)과 40행 강 등급 표는 그대로다.
   - 결정 4(10-31 평가 안건): 「선행 시험 작성자의 Codex 이동 여부와 Gardener 배정을 10-31 검증자 시범 평가 때 함께 정한다」를 스킬 57행(Q3 A)과 ORCA Gardener 절 214행에 넣는다. 선행 시험 작성자는 지금처럼 신규 `claude-opus-5-5`다.
2. 이전 goal 이월(보충 1): 진척 단계 둘, 재개 지점 「종료」, 「현재 결과」 새 절 「PR231 병합과 종료 점검」, 「적용 중인 사용자 결정」의 핫픽스 원문. 완료조건 대조 6의 「진행 중」을 충족으로 고친 것은 이월 넷 밖의 정합이다.
3. BACKLOG
   - 새 절 「라우팅 핫픽스 정본 반영 goal에서 연결한 후보」에 새 행 둘: 리드 메모 감사 필드 검사 helper(보충 2), Unity 시험 harness의 사용자 미커밋 파일 표시(보충 셋째). 구현은 이 goal 밖이다.
   - 갱신 둘: `operating-trial-evaluation`(121행)에 결정 4와 출처 `msg_ca6641b7a9e2`, `lead-mailbox-receive-delay`(264행)의 열린 질문에 사용자 답 「유지」(Q1). helper 제안은 그대로 대기다.
4. 이 goal 파일과 CURRENT Rules 줄.

### 건드릴 곳

| 파일 | 쓰는 사람 | 내용 |
|---|---|---|
| `00_Document/operations/ORCA.md` | 신규 Sol | 결정 1(43·45·88·206행), 결정 2(90~92행), 결정 4(214행) |
| `.agents/skills/dawnholder-goal-loop/SKILL.md` | 신규 Sol | 결정 3(54행 뒤 새 줄), 결정 4(57행). Codex 영역이며 사용자 결정 3이 수정 승인을 겸한다 |
| `00_Document/operations/BACKLOG.md` | 리드 | 새 행 둘, 기존 행 갱신 둘 |
| `01_Phases/goals/2026-10-11-pre-freeze-rule-docs/goal.md` | 리드 | 이전 goal 이월. 시작 커밋 |
| 이 goal | 리드 | 범위·진척 단계·결정·결과. 시작 커밋 |
| `00_Document/operations/CURRENT.md` | 리드 | Rules 줄만. 다른 파트 줄은 보존 |

Sol과 리드는 파일이 겹치지 않는다. commit·push는 리드만 한다.

### 하지 않을 것

- AGENTS.md, CLAUDE.md, RESUME, `.claude/` 설정, 전역 Claude 설정(`autoCompactWindow` 포함), 코드·hook·helper·CI. CLAUDE.md의 5분 감시 줄 제안(초안 7절)은 메인이 받지 않았다. 5분 감시는 ORCA 정본에 들어간다.
- 넷 밖 새 규칙: 조사 원문 7절의 결정 2 되돌림 조건(메시지 10분 지연)·3일 뒤 깨어남 수 비교, 결정 1의 리드 한 명 시범 측정, 8절의 웹 디자인·E2E 배정. 사용자 결정 원문과 핫픽스 문장에 없기 때문이다.
- ORCA 91행 변경(Q1 B), 강 등급 표(스킬 40행), R-5 작업자·검증자 기동 명령, 선행 시험 배정 변경.
- 각 worktree에 따로 고치기. 정착은 병합 뒤 각 리드가 안전 지점에 main을 받아 한다(메인 판단).
- 다른 파트 goal, CURRENT의 다른 파트 줄, 새 BACKLOG 두 행의 구현, 규칙 문서 가지치기(10-31 Gardener 평가), 판(V1.0) 올림(Q4).

### 관찰 가능한 완료조건

1. ORCA의 R-1 기동 명령, 크래시 복구, R-8 중간 교체에 `--autocompact 250000`이 있다. 첫 턴 압축 사실과 50만 되돌림 조건이 한 번 적혀 있다. 전역 설정을 바꾸라는 문장은 없다.
2. ORCA 대기 절에 결정 2 정정판 문장과 5분 감시가 있고 `msg_9494d6bd3ac5`의 정정판 원문과 대조된다. 91행은 그대로이며 새 문장과 어긋나거나 같은 내용을 되풀이하지 않는다.
3. 스킬에서 로컬 DB 도구가 54행 예외에서 빠졌다. 실제 경로 1회 유지, 되돌림, 회차 기록, 스키마·저장 로직 Opus 유지가 모두 있다. 강 등급 표와 53행은 그대로이고, Astra가 구현한 DB 도구 카드는 53행대로 Opus가 검증한다는 읽기가 성립한다.
4. 10-31 안건 항목이 스킬 57행, ORCA 214행, BACKLOG `operating-trial-evaluation`에 있다.
5. 이전 goal 이월 넷이 들어 있다. BACKLOG 새 행 둘과 갱신 행이 항목 계약(7필드, 상태 세 값)을 지키고, 후보 도착 검사(`node 99_Tools/Backlog/check-candidates.mjs --backlog 00_Document/operations/BACKLOG.md --goal 01_Phases/goals/2026-10-11-routing-hotfix-canon/goal.md`)가 `allowed`·중복 0이다. 리드 실행 원시를 E에 두고 실사자가 같은 명령을 다시 돈다.
6. 변경 경계: `git diff --stat <base>...HEAD`에 「건드릴 곳」의 여섯 파일만 있다. Codex 영역은 목표 루프 스킬뿐이다.
7. 크기: ORCA 250줄 이하다. 여섯 파일 묶음 bytes는 「PR 경계와 검증」의 bytes 줄대로 잰다.
8. PR 설명에 바뀐 정본 파일 목록과 정착 확인 명령이 있다.
9. 신규 문서 실사 통과 → Gardener → 결과 기록과 재실사 → 사용자 병합 승인 → 종료 점검 → R-8 순서로 끝난다. 다음 goal은 자동으로 시작하지 않는다.

## PR 경계와 검증

| 항목 | 값 |
|---|---|
| PR | 하나. 최신 main에서 `docs/routing-hotfix-canon-20261011` |
| 검증 등급 | 문서 실사(문서·스킬 문장만). 줄 수 원시는 작성 뒤 `git diff --numstat`으로 E에 둔다 |
| TDD | 해당 없음(문서만, 목표 루프 「구현·검증·수정」) |
| R-7 | 비해당. 문서만이고 4범주 코드 작업이 아니다 |
| 정본 문장 작성 | 신규 `gpt-6.1-sol` max. 명세가 정해진 문서 문장이라 구현자 신호가 없다([배정](../../../.agents/skills/dawnholder-goal-loop/references/implementer-routing.md) 「신호가 없을 때 Sol max로 둘 일」의 「문서」) |
| 문서 실사 | 신규 `gpt-6-astra` xhigh(검증자 모델 시범 「문서 실사」). 현실 시나리오 여섯: 새 리드 R-1 기동, 목표 중간 `--resume` 뒤 첫 턴 압축, 대기가 살아 있을 때 자동 알림으로 깸, 메모리 회수로 대기가 꺼진 뒤 안내가 와서 check, Core SQL 설치 카드 검증 배정(Astra 구현 카드와 스키마 변경 포함), 10-31 평가 준비 |
| Gardener | 신규 `claude-opus-5-5` 읽기 전용, 보고서 한 파일(이름에 `context`를 넣어 세션 쓰기 가드와 맞춘다). 병합 전 고정 head(메인 판단) |
| 수정·재실사 | 결함이 나오면 새 Sol, 새 Astra. 같은 산출물 수정 3회 초과는 메인 체크포인트 |

- 차례: 시작 커밋(리드) → 정본 문장(Sol)과 BACKLOG(리드) 동시 → 리드 대조·bytes·후보 검사 → PR 생성 → 문서 실사 → Gardener → 결과 기록 → 바뀐 부분 재실사(결과는 goal을 고치지 않고 근거·승인 묶음으로) → 승인 묶음 → 사용자 병합 승인 → 종료 점검 → R-8.
- 규칙 문서 bytes: 사용자가 핫픽스 넷의 정본 문장만큼 느는 것을 허용했다(Q2, 「적용 중인 사용자 결정」). 기준 `a570dbf3`의 여섯 파일 묶음(AGENTS·ORCA·goal-loop SKILL·RESUME·session-handoff SKILL·orca-work)은 `git show <rev>:<file> | wc -c`로 합계 103,306 bytes, ORCA 41,084 bytes·245줄이다(E/bytes-base.txt). 초안의 예상 증가는 +1,774 bytes였고(E/scope-bytes.txt), 그 예상은 91행을 바꾸는 문장 기준이라 실제 값은 달라진다. 리드 해석: 묶음 증가가 +1,774 bytes 이하면 그대로 진행하고, 넘으면 실사 전에 원시 수치를 status로 올려 메인을 거쳐 다시 묻는다. CLAUDE.md는 메인 몫이라 따로, BACKLOG·CURRENT·goal은 묶음 밖에서 수치만 보고한다.
- CURRENT가 다른 파트 PR과 겹치면 승인 묶음 직전에 main을 받아 맞추고 다른 파트 줄을 보존한다.
- 정착 확인 명령(PR 설명에 넣는다). 바뀐 정본은 ORCA와 목표 루프 스킬 두 파일이다. 각 worktree에서 리드가 안전 지점에 main을 받은 뒤 메인이 확인한다. 둘째 줄이 `settled`이고 셋째 줄 출력이 비면 그 worktree는 정착했다.

```text
git fetch origin
git merge-base --is-ancestor <PR 병합 commit> HEAD && echo settled || echo not-settled
git diff --stat origin/main -- 00_Document/operations/ORCA.md .agents/skills/dawnholder-goal-loop/SKILL.md
```

### 위험

1. bytes: 상쇄할 구절이 남아 있지 않아 증가가 거의 그대로 남는다. 사용자 허용(Q2) 안에서 실제 값을 원시로 남긴다.
2. 결정 2 정정판과 91행이 한 절에 함께 있어 같은 내용을 두 번 말할 수 있다. 작성 계약에서 중복을 막고 문서 실사가 대조한다.
3. 「로컬 DB 도구·실행 카드」 경계가 애매한 작업이 나온다. 54행의 「해당 여부가 애매한 작업은 Opus」가 남아 있어 애매하면 Opus다.
4. `--autocompact`는 화면에 보이지 않는다. 리드 기록은 「메인 기동값」으로 적고 backend는 unknown이다.
5. 병합 전까지 각 worktree의 ORCA는 옛 기동 명령이다. 메인은 핫픽스대로 옵션을 붙여 띄우므로 운영 공백은 없다. 새 문장은 떠 있는 리드가 안전 지점에 main을 받을 때 적용된다([main 맞춤과 합류점](../../../.agents/skills/dawnholder-goal-loop/references/milestones.md#main-맞춤과-합류점)).

## 요구사항 원천과 적용 결정

메인이 전달한 사용자 결정은 사용자 직접 입력과 구분한다. 메인 판단은 사용자 결정이 아니다. 원시 메시지는 E/session/source-messages.json·check-entry.json·wait01.json·wait02.json이다.

- 진입 지시: 메인 `msg_300feadfab41`(2026-10-11T11:33:02Z). 승인된 넷과 정합 범위, 이전 리드 기록 결함 관찰을 담았다. 메인 판단: 정본은 이 goal에서 한 번만 고치고 정착은 병합 뒤 각 리드가 안전 지점에 main을 받아 한다.
- 핫픽스 원문 전달: 메인 `msg_ca6641b7a9e2`(11:03:17Z, 이전 Rules 리드에게 보낸 정본 반영 요청 1~4), 파트별 적용 `msg_154e43b875cb`(Content)·`msg_22f631ad7da7`(World)·`msg_0da44f41ac52`(Core). 다음 Rules goal 지정은 `msg_a361c0c617b0`(11:14:39Z).
- 조사 원문: 메인 개인 도구의 `plans/routing-balance-1011.md`(SHA256 `b9376190df1a5273eee02eb8ddbeb30af7a43c415f3ea777525912d22d09f827`, 읽기만). 넷 밖이라 넣지 않은 7·8절 항목은 「하지 않을 것」에 있다.
- 진입 보충: 메인 `msg_ded7bd2d6839`(11:33:33Z). 1은 이전 goal 이월이고 근거는 이전 goal 근거 폴더의 `closeout-handoff.md`다. 2는 메모 감사 필드 검사 helper 후보다. 목표 루프 「후보는 아이디어마다 승인을 묻지 않는다」에 따른 메인 판단이다.
- 진입 보충 셋째: 메인 `msg_e42c64db439a`(11:41:30Z). World 리드 `msg_5b7fc7beef82`가 원천인 Unity 시험 harness 후보다. World goal은 main 밖이라 msg ID로만 잇는다.
- 범위 초안 v1: 리드 `msg_162e62a9c2b2`(11:45:11Z, E/scope-draft-v1.md).
- 범위 승인: 메인 `msg_9494d6bd3ac5`(11:49:51Z, E/session/wait02.json). 답 「Q1 B, Q2 A, Q3 A, Q4 질문 안 함」.
  - Q1 B(사용자 결정 「A 유지」): ORCA 91행을 그대로 둔다. BACKLOG `lead-mailbox-receive-delay`의 열린 질문에 「유지」가 답해졌다고 적는다.
  - 결정 2 정정판(메인): 처음 핫픽스 문장의 「대기가 꺼졌거나 … check로 처리하고 대기를 다시 연다」와 「기본값은 처리한다 쪽이다」는 사용자 결정 2에 없던 메인의 덧붙임이었고 91행과 어긋났다(메인 실수, 리드가 범위 초안 Q1에서 찾음). 정정판 원문: 「네 우편함 대기 background 작업이 아직 돌고 있고(시작한 뒤 그 작업의 종료·kill 알림을 받지 않음) Orca 자동 알림(「You have N orchestration message」)으로 깼다면 도구 호출 없이 한 줄로 턴을 끝낸다. 메시지 처리는 대기가 끝나 깰 때 한다. 대기가 메모리 회수로 꺼졌거나 살아 있는지 확실하지 않으면 지금처럼 --wait 없는 check로 처리한다. 메모리 회수로 꺼진 대기는 스스로 다시 켜지 않는다.」 정본에는 「네」를 「리드」로 바꿔 넣고 「놓친 메시지는 메인 화면 감시가 5분 넘게 안 읽힌 완료·질문 메시지를 잡는다」를 같은 자리에 둔다.
  - Q2 A(사용자 결정 「A 허용」): 핫픽스 넷의 정본 문장만큼(예상 +1,774 bytes) 늘어도 된다. 실제 크기를 원시로 남기고 예상보다 크게 넘으면 다시 묻는다.
  - Q3 A(메인 판단): 결정 4를 스킬 57행에도 넣는다. 사용자가 정본 반영을 지시했고 같은 「검증자 모델 시범」 절이라 결정 3이 겸한 Codex 영역 수정 승인 범위로 본다.
  - Q4(메인): 판은 올리지 않고 사용자에게 묻지 않는다.
  - 기본값 확인(메인 판단): Gardener는 병합 전 고정 head에서 연다. Unity harness 행은 새 행으로 두고 관련 행과 잇는다. CLAUDE.md는 바꾸지 않는다. 메인은 감시 도구를 대시보드 폴더 `main-notes/tools/`로 옮겼다.
- 정합 탐색(초안 6절): 검색어 `autocompact`, `claude --model`, `claude --resume`, `--wait`, `깨`, `자동 알림`, `DB`와 검증자 모델, `10-31`·`10월 31일`. 범위는 AGENTS·ORCA·RESUME·`.agents/`·`00_Document/conventions/`·`99_Tools/README.md`·BACKLOG다. 고칠 곳은 「건드릴 곳」에 있고, 그대로 둔 곳과 이유는 아래와 같다.

| 위치 | 그대로 둔 이유 |
|---|---|
| RESUME 22행 진입 배치 | 기동 명령 문장이 없고 R-1로 이어진다 |
| ORCA 140행 R-5 작업자·검증자 기동 | 결정 1은 리드만 대상이다 |
| ORCA 223행 병합 관문 `--resume` 30분 기록 | 기동 옵션과 무관한 승인 기록 규칙이다 |
| ORCA 91행 메모리 회수 뒤 다시 켜지 않음 | 사용자 결정 「A 유지」(Q1 B) |
| ORCA 92행 heartbeat 깨움 관측 | 결정 2가 다루는 문제의 관측 근거다 |
| ORCA 128행 R-4 「메인 폴더 감시는 보조 그물」 | 5분 감시와 같은 방향이다 |
| 스킬 40행 강 등급 표 | 결정 3은 검증자 모델만 바꾸고 강 등급·실제 경로 1회는 유지한다 |
| 스킬 53행 Astra 구현의 검증자는 Opus | Astra가 구현한 DB 도구 카드는 계속 Opus다. 같은 모델 자기 검증을 막는 줄이라 새 줄보다 앞선다고 문서 실사에서 확인한다 |
| 스킬 55행 「선행 시험 작성자는 대상이 아니다」, 83행 TDD 새 Opus | 결정 4로 선행 시험은 지금처럼 Opus다 |
| AGENTS 24·35행 | 시범 링크와 「선행 시험 작성은 신규 `claude-opus-5-5`」라 결정 넷과 어긋나지 않는다 |
| `implementer-routing.md` 27행 | 구현자 시범의 평가라 결정 넷과 무관하다 |
| `orca-work.md` 28행 | 시범 링크만 있다 |
| `dawnholder-helper-session` 스킬 47행 | 보조는 상시 대기를 열지 않아 결정 2 대상이 아니다 |
| `99_Tools/README.md` 65행 | 세션 쓰기 가드의 `session_id` 설명이다 |
| CODE_CONVENTION 148행, `refs/agent-engineering/gardener.md` 15행 | 10-31 평가의 존재만 말한다 |
| BACKLOG 229·255·259·261행 | 결정 넷과 다른 후보다. 261행 `mailbox-wait-overlap-guard`의 도구 후보는 그대로 남는다 |

## 적용 중인 사용자 결정

모두 메인 창 Enter 제출, 메인 전달이다. 직접 입력으로 격상하지 않는다.

- **라우팅 핫픽스**(메인 전달 `msg_ca6641b7a9e2` 2026-10-11T11:03:17Z와 진입 지시 `msg_300feadfab41`, 사용자 제출 20:02 KST): 「대시보드 결정 응답: 1) 모델 라우팅 - 모든 리드를 문맥 25만에서 압축하도록 지금 적용할지 → A 전체 적용 · 2) 모델 라우팅 - 리드가 같은 메시지로 두 번 깨는 것을 지금 규칙으로 막을지 → A 지금 적용 · 3) 모델 라우팅 - 로컬 DB 설치·기동 도구의 검증을 지금부터 Astra로 옮길지 → A 지금부터 Astra · 4) 모델 라우팅 - 선행 시험 작성자를 Codex로 옮기는 시점 → A 10-31 평가 때」. 이 goal이 정본에 넣기 전까지 핫픽스로 적용 중이다. 결정 2는 위 메인 정정판으로 적용한다.
- **정본 반영과 정착**(진입 지시 `msg_300feadfab41`, 20:1x KST): 「뭐야, 룰에 반영 안된거면 나중에 다시 리드급 켤때 휘발되는 정보네, 정합하고 반영해야될 것 같은데 / 현재 룰 작업 끝나면 다음 작업으로 바로 진행해줘, 그리고 그 문서 기반으로 각 워크트레이 새로 각각 전담 Agent 반영해서 워크트리에 바로 반영 가능하게 하면 더 좋은데, 만약에 한번 규칙 정본으로 수정하고 이후에 각 워크트리에 뿌려서 정착시키는게 빠르면 그렇게 해도 좋고」. 메인은 정본을 한 번 고치고 병합 뒤 각 worktree에 정착시키는 쪽을 골랐다(메인 판단).
- **범위 승인의 사용자 답**(메인 전달 `msg_9494d6bd3ac5`, 2026-10-11T11:49:51Z): 「대시보드 결정 응답: 1) 라우팅 정본 반영 - 메모리 부족으로 꺼진 우편함 대기를 리드가 다시 켜지 않는 규칙을 유지할지 → A 유지 · 2) 라우팅 정본 반영 - 규칙 문서 크기가 약 1.8KB 느는 것을 허용할지 → A 허용」. 1)이 Q1 B, 2)가 Q2 A다.

## 현재 결과

### 착수

- 진입: Run `run_bea2c841fa15`(2026-10-11T11:36:00Z, E/session/run-create.json), READY status `msg_393cc84eb17f`(11:36:24Z). 진입 우편 소비 원시는 E/session/check-entry.json이다. 메모는 E/lead-context.md 「진입·범위 초안 차례」다.
- 범위 초안 v1 `msg_162e62a9c2b2`(11:45:11Z) → 메인 승인 `msg_9494d6bd3ac5`(11:49:51Z).
- 시작 기록: branch `docs/routing-hotfix-canon-20261011`을 origin/main `a570dbf3`(11:50:06Z fetch)에서 만들었다. 시작 커밋은 이전 goal 이월, 이 goal, CURRENT Rules 줄이다. 착수 메모는 E/lead-context.md 「착수 차례」(11:52:35Z)다. 이월 편집은 E/carryover-edit.mjs가 바꿀 원문이 정확히 한 번 있는지 단정하며 했다(E/carryover-edit-result.json).

### 정본 문장과 리드 대조

- 작성자: 신규 `gpt-6.1-sol` max(첫 화면 「GPT-6.1-Sol max」, rollout turn 모델 `gpt-6.1-sol`, backend unknown). Task `task_a1115b33e401`, Dispatch `ctx_b0e1f9945f0f`. 계약은 E/sentences-contract.md v1(SHA256 `b0495978…f682`), 고정 HEAD `b9a83152`다. 12:07:27Z에 시작해 worker_done succeeded `msg_d39708e41ade`(12:23:56Z)로 끝났다. 보고서 E/sentences/report.md, 기동 기록 E/sentences-start-note.md.
- 바뀐 자리: ORCA 43·88·207행 기동 명령에 `--autocompact 250000`, 45행 문단 앞에 실행 한정·첫 턴 압축·되돌림 조건(결정 1). ORCA 91행 새 줄에 자동 알림으로 깼을 때의 처리와 5분 감시를 넣었고 원래 91행은 92행으로 그대로다(결정 2). 목표 루프 스킬 55행 새 줄(결정 3). 스킬 58행과 ORCA 215행의 평가 안건(결정 4).
- 리드 R-2(E/r2-lead.mjs, E/r2-lead.json): 보존 줄 11개가 바이트 단위로 같고 각각 한 번만 있다. 두 파일의 제목·`<a id>`·링크 목록이 고정 HEAD와 같다. 바뀐 자리는 위 여덟 곳뿐이고 numstat은 ORCA 6/5, 스킬 2/1이다. 사용자 원문 `msg_ca6641b7a9e2`, 정정판 `msg_9494d6bd3ac5`, 진입 할 일 `msg_300feadfab41`과 문장 뜻을 대조했다.
- bytes: 여섯 파일 묶음 103,306 → 105,254(+1,948). ORCA +1,209(245→246줄), 스킬 +739(103→104줄), 나머지 넷은 0이다. 결정별로는 533·547·613·255다(Sol E/sentences/work/check-output.json). 예상 +1,774보다 174 크다. 리드가 status `msg_c95cbb6ba789`(12:27:09Z)로 물었고 메인이 `msg_f876e290be4c`(12:27:34Z)로 「A 그대로 진행」이라 답했다. 메인 판단이며 사용자 결정이 아니다. 늘어난 것은 결정 넷의 문장뿐이고 새 규칙이 없어 사용자 허용 「A 허용」 안이라는 판단이다.
- 생존 신호(E/sentences-liveness.json, `99_Tools/Orca/check-liveness.mjs`): `within`, 신호 7, 최대 간격 271초. 300초 초과 구간은 없다. Sol pane은 tui-idle 확인 뒤 12:25:56Z에 닫았다.
- BACKLOG(리드, E/backlog-edit.mjs): 갱신 둘(`operating-trial-evaluation`에 결정 4, `lead-mailbox-receive-delay`의 열린 질문에 답 「A 유지」)과 새 절 「라우팅 핫픽스 정본 반영 goal에서 연결한 후보」의 두 행. 141,329 → 145,559 bytes, 276 → 285줄(묶음 밖). 후보 도착 검사는 `allowed`(ID 116, 중복 0, 후보 2, unknown 0, E/backlog-check-after.json)이고 이전 goal도 `allowed`다(E/backlog-check-after-prev.json).

## 다음 계획 후보

이 goal 밖으로 둔 일이다. 후보마다 BACKLOG ID를 단다.

- 리드 메모 감사 필드 검사 helper(보충 2): 메모 차례마다 「완료 뒤 실제」 칸과 자기 쓰기 감사 줄이 채워졌는지 검사한다. 도구라 동결 뒤다. BACKLOG `lead-memo-audit-field-check`.
- Unity 시험 harness의 사용자 미커밋 파일 표시(보충 셋째): 전후 git 상태에서 알려진 사용자 미커밋 파일을 「리드 복원 대상」으로 따로 표시한다. BACKLOG `unity-harness-user-file-restore-mark`.
