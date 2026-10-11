# 라우팅 핫픽스 정본 반영

- 만드는 것: 지금 쓰는 모델 라우팅 핫픽스 사용자 결정 넷을 운영 정본(ORCA)과 목표 루프 스킬에 넣는다. 리드 압축 기준, 같은 메시지로 두 번 깨지 않기, 로컬 DB 도구의 검증자, 10-31 평가 안건이다.
- 덧붙여 이전 Rules goal의 마지막 기록을 이월하고, 대기 목록(BACKLOG)에 새 후보 둘을 넣고 기존 행 둘을 갱신한다. 사용자 직접 지시로 새 후보 셋과 기존 행 갱신 둘을 더했다(메인 전달 `msg_30cd8b7b30d8`). 도구는 만들지 않는다.
- PR 하나, 문서만 바꾼다. Codex 영역은 사용자 결정 3이 승인을 겸한 목표 루프 스킬 한 파일뿐이다. 정본은 이 goal에서 한 번만 고치고, 각 worktree는 병합 뒤 main을 받아 정착시킨다.

## 재개 지점

기능 동결(10-28) 전의 작은 Rules goal이다. 메인이 범위를 승인했다(「적용 중인 사용자 결정」). 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch: `docs/routing-hotfix-canon-20261011`(base main `a570dbf3`). 정확한 head는 원격 branch와 승인 묶음에서 확인한다.
- 근거 폴더 E: `.backups/verification/2026-10-11-routing-hotfix-canon/`(Git 제외). 승인 범위는 [scope-draft-v1.md](../../../.backups/verification/2026-10-11-routing-hotfix-canon/scope-draft-v1.md)(SHA256 `32c0186e…b927`)와 메인 승인의 답 넷(「요구사항 원천과 적용 결정」)이다. 리드 맥락 메모는 E/lead-context.md이고 차례마다 절을 더한다.
- 리드: 신규 `claude-opus-5-5` xhigh(화면 「Opus 5.5 ⚡xhigh」, backend unknown), 압축 기준 `--autocompact 250000`은 메인 기동값이며 화면에서 확인하지 못했다. 태그 `[Rules 리드 Opus]`, handle `term_991652c5-0324-450d-9208-3d8c9663777f`, Run `run_bea2c841fa15`(회신 주소 `run:run_bea2c841fa15`). 메인 주소는 메인 term handle이다. 이전 Rules goal의 Run·Task·Dispatch·handle은 실행 권한이 아니다.
- 앞 goal의 끝: [규칙·운영 동결 전 문서 정리](../2026-10-11-pre-freeze-rule-docs/goal.md)는 PR229·PR231 병합으로 끝났다. 마지막 기록은 이 goal의 시작 커밋으로 넣었다(「현재 결과 / 착수」).
- **현재 위치**: 결과 기록까지 커밋하고 마감했다. 2026-10-11 사용자 마감 지시(메인 전달 `msg_9cabdebd16c1`)에 따라 새 작업자·검증자 세션은 열지 않았다. [PR232](https://github.com/bass131/dawnholder-server/pull/232)의 Gardener가 끝났고, 그 결과와 사용자가 채택한 후보 둘의 BACKLOG 반영, 기록 누락·낡음 정정을 이 결과 기록에 넣었다(「현재 결과 / 종료 점검(Gardener)」).
- **다음 한 걸음**: 바뀐 부분 재실사를 신규 `gpt-6-astra` xhigh로 연다. 범위는 S1 정정(ORCA 91행, Gardener의 대기 재개 갈래 관찰을 입력으로), R3·R4 기록과 처분 기록, BACKLOG의 새 행 여섯과 갱신 행, 후보 도착 검사 재실행, 이 결과 기록이다. 결과는 goal을 고치지 않고 근거·승인 묶음으로 넘긴다.
- **남은 순서**: 바뀐 부분 재실사 → 승인 묶음(CI 재조회, CURRENT가 다른 PR과 겹치면 main 받기) → 사용자 병합 승인 → 종료 점검 → R-8. 다음 goal은 자동으로 시작하지 않는다.
- **사용자 차례**: 지금은 없다. Gardener 결정 요청(`msg_0f80e7631586`)의 안건 1은 사용자 결정 A, 안건 2는 메인 운영 결정 A로 답을 받았다(「요구사항 원천과 적용 결정」).
- **다시 열 때**: 이 리드의 Claude 세션 ID는 `d721bb8b-fb36-4f01-bc3e-773baaeda336`, Run은 `run_bea2c841fa15`다. 마감 때 이 작업 경로에 사용자 미커밋 파일은 없었고, 띄운 작업자·검증자·Gardener pane은 모두 정산해 닫았다. 재개는 [R-8](../../../00_Document/operations/ORCA.md#r8-astra-lifecycle)의 목표 중간 교체·복구 문장(같은 대화 `--resume`과 `run-use`)을 따른다.

## 진척 단계

- [x] 범위와 기준 확정
- [x] 정본 문장 작성
- [x] 대기 목록 정리
- [x] 문서 실사
- [x] 종료 점검자 확인
- [>] 결과 기록·재실사(결과 기록 끝, 바뀐 부분 재실사 전)
- [ ] 병합
- [ ] 종료 점검과 R-8

## 범위

승인 범위는 초안 v1에 메인 답 넷을 더한 것이다. 초안과 달라진 곳은 결정 2의 문장(메인 정정판)과 ORCA 91행 보존(Q1 B)이다. 이 절의 ORCA·스킬 줄 번호는 base `a570dbf3` 기준이다.

### 만들 것

1. 정본 문장 넷. 작성자는 원천 원문과 아래 의도를 받아 문장을 쓴다. 제목·anchor는 바꾸지 않는다. 특히 `검증자-모델-시범2026-10-31까지` anchor는 여러 파일이 링크한다.
   - 결정 1(ORCA 리드 압축 기준): R-1 리드 기동 명령(43행)을 `--command "claude --model claude-opus-5-5 --effort xhigh --autocompact 250000"`으로 바꾼다. 크래시·업데이트 복구(88행)와 R-8 목표 중간 교체(206행)의 `claude --resume`에도 같은 옵션을 붙인다. R-1 코드 블록 바로 뒤 문단(45행, 초안의 「44행 문단」)에 세 사실을 한 번만 적는다. 옵션은 그 실행에만 걸리고 전역 설정은 바꾸지 않는다. 기준을 넘은 대화를 이어 띄우면 첫 턴에 바로 압축되므로(2026-10-11 Core 27만→8.6만 관측) 재개에 필요한 것은 goal 재개 지점과 근거 폴더에 둔다. 압축 뒤 맥락 손실로 일을 다시 한 사례가 한 번이라도 나오면 리드가 바로 메인에 보고하고 메인이 모든 리드를 50만으로 되돌린다.
   - 결정 2(ORCA 「Orca 도구 관측과 우편함 대기」 90~92행): 메인 정정판 문장을 넣는다(「네」를 「리드」로). 「놓친 메시지는 메인 화면 감시가 5분 넘게 안 읽힌 완료·질문 메시지를 잡는다」를 같은 자리에 둔다. 91행 「메모리 회수로 대기가 꺼지면 스스로 다시 켜지 않는다」는 그대로 두고, 새 문장이 91행과 같은 내용을 되풀이하지 않게 쓴다(Q1 B).
   - 결정 3(목표 루프 스킬 「검증자 모델 시범」 54행 뒤 새 줄): 게임 영속 데이터를 바꾸지 않는 로컬 DB 설치·기동 도구와 실행 카드(SQL Server 인스턴스 설치·기동, 컨테이너 엔진 카드 등)를 54행의 DB 예외에서 빼고 신규 `gpt-6-astra` xhigh가 검증한다. 실제 경로 1회 실행(사용자 실행 카드 포함)은 그대로다. 사용자가 실행한 카드에서 독립 검증이 놓친 결함이 한 번이라도 나오면 Opus로 되돌린다. 카드 회차 수와 놓친 결함을 goal에 기록한다. 게임 영속 데이터의 스키마·저장 로직은 지금처럼 Opus가 검증한다. 53행(Astra 구현의 검증자는 Opus)과 40행 강 등급 표는 그대로다.
   - 결정 4(10-31 평가 안건): 「선행 시험 작성자의 Codex 이동 여부와 Gardener 배정을 10-31 검증자 시범 평가 때 함께 정한다」를 스킬 57행(Q3 A)과 ORCA Gardener 절 214행에 넣는다. 선행 시험 작성자는 지금처럼 신규 `claude-opus-5-5`다.
2. 이전 goal 이월(보충 1): 진척 단계 둘, 재개 지점 「종료」, 「현재 결과」 새 절 「PR231 병합과 종료 점검」, 「적용 중인 사용자 결정」의 핫픽스 원문. 이월 넷 밖의 정합은 두 곳이다. 이전 goal 32행 PR231 설명의 「같은 날 만들었다」에 「같은 날 병합했다」를 더했고, 완료조건 대조 6(222행)의 「진행 중」을 충족으로 고쳤다. 둘 다 PR231 병합이라는 같은 종료 사실을 맞춘 것이다. 32행은 처음 이 줄에서 빠졌고 문서 실사 R4로 더했다(「현재 결과 / 문서 실사」).
3. BACKLOG
   - 새 절 「라우팅 핫픽스 정본 반영 goal에서 연결한 후보」에 새 행 둘: 리드 메모 감사 필드 검사 helper(보충 2), Unity 시험 harness의 사용자 미커밋 파일 표시(보충 셋째). 구현은 이 goal 밖이다.
   - 갱신 둘: `operating-trial-evaluation`(121행)에 결정 4와 출처 `msg_ca6641b7a9e2`, `lead-mailbox-receive-delay`(264행)의 열린 질문에 사용자 답 「유지」(Q1). helper 제안은 그대로 대기다.
   - 문서 실사 처분(메인 `msg_3bc350db7c54`): `contract-context-check` 출처 칸에 R1 근거를 더한다.
   - 사용자 직접 지시(메인 전달 `msg_30cd8b7b30d8`): 같은 새 절에 새 행 셋 `loop-graph-concepts`·`agent-flow-program`·`wake-token-filter`를 둔다. 갱신 둘은 `goal-loop-improvement`(진단 근거, 남은 범위는 `loop-graph-concepts` 먼저)와 `rule-document-subdivision`(순서 1A, 전이 표 입력 3A, 문서 실사 O1·O2)이다. 행의 구현과 개념 조사는 하지 않는다.
4. 이 goal 파일과 CURRENT Rules 줄.

### 건드릴 곳

| 파일 | 쓰는 사람 | 내용 |
|---|---|---|
| `00_Document/operations/ORCA.md` | 신규 Sol | 결정 1(43·45·88·206행), 결정 2(90~92행), 결정 4(214행) |
| `.agents/skills/dawnholder-goal-loop/SKILL.md` | 신규 Sol | 결정 3(54행 뒤 새 줄), 결정 4(57행). Codex 영역이며 사용자 결정 3이 수정 승인을 겸한다 |
| `00_Document/operations/BACKLOG.md` | 리드 | 새 행 다섯, 기존 행 갱신 다섯(사용자 직접 지시의 새 행 셋·갱신 둘과 실사 처분의 근거 추가 하나 포함) |
| `01_Phases/goals/2026-10-11-pre-freeze-rule-docs/goal.md` | 리드 | 이전 goal 이월. 시작 커밋 |
| 이 goal | 리드 | 범위·진척 단계·결정·결과. 시작 커밋 |
| `00_Document/operations/CURRENT.md` | 리드 | Rules 줄만. 다른 파트 줄은 보존 |

Sol과 리드는 파일이 겹치지 않는다. commit·push는 리드만 한다.

### 하지 않을 것

- AGENTS.md, CLAUDE.md, RESUME, `.claude/` 설정, 전역 Claude 설정(`autoCompactWindow` 포함), 코드·hook·helper·CI. CLAUDE.md의 5분 감시 줄 제안(초안 7절)은 메인이 받지 않았다. 5분 감시는 ORCA 정본에 들어간다.
- 넷 밖 새 규칙: 조사 원문 7절의 결정 2 되돌림 조건(메시지 10분 지연)·3일 뒤 깨어남 수 비교, 결정 1의 리드 한 명 시범 측정, 8절의 웹 디자인·E2E 배정. 사용자 결정 원문과 핫픽스 문장에 없기 때문이다.
- ORCA 91행 변경(Q1 B), 강 등급 표(스킬 40행), R-5 작업자·검증자 기동 명령, 선행 시험 배정 변경.
- 각 worktree에 따로 고치기. 정착은 병합 뒤 각 리드가 안전 지점에 main을 받아 한다(메인 판단).
- 다른 파트 goal, CURRENT의 다른 파트 줄, 새 BACKLOG 행의 구현과 개념 조사, 규칙 문서 가지치기(10-31 Gardener 평가), 판(V1.0) 올림(Q4).

### 관찰 가능한 완료조건

1. ORCA의 R-1 기동 명령, 크래시 복구, R-8 중간 교체에 `--autocompact 250000`이 있다. 첫 턴 압축 사실과 50만 되돌림 조건이 한 번 적혀 있다. 전역 설정을 바꾸라는 문장은 없다.
2. ORCA 대기 절에 결정 2 정정판 문장과 5분 감시가 있고 `msg_9494d6bd3ac5`의 정정판 원문과 대조된다. 91행은 그대로이며 새 문장과 어긋나거나 같은 내용을 되풀이하지 않는다.
3. 스킬에서 로컬 DB 도구가 54행 예외에서 빠졌다. 실제 경로 1회 유지, 되돌림, 회차 기록, 스키마·저장 로직 Opus 유지가 모두 있다. 강 등급 표와 53행은 그대로이고, Astra가 구현한 DB 도구 카드는 53행대로 Opus가 검증한다는 읽기가 성립한다.
4. 10-31 안건 항목이 스킬 57행, ORCA 214행, BACKLOG `operating-trial-evaluation`에 있다.
5. 이전 goal 이월 넷이 들어 있다. BACKLOG 새 행 다섯과 갱신 행 다섯이 항목 계약(7필드, 상태 세 값, 최초 필드 보존)을 지키고, 후보 도착 검사(`node 99_Tools/Backlog/check-candidates.mjs --backlog 00_Document/operations/BACKLOG.md --goal 01_Phases/goals/2026-10-11-routing-hotfix-canon/goal.md`)가 `allowed`·중복 0이다. 리드 실행 원시를 E에 두고 실사자가 같은 명령을 다시 돈다.
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
- 문서 실사 처분: 메인 `msg_3bc350db7c54`(2026-10-11T13:09:28Z, E/session/wait13.json). 메인 운영 결정이며 사용자 결정이 아니다. R1·R2·V1은 비차단 운영 기록, S1은 새 Sol, 바뀐 부분 재실사는 신규 Astra, O1·O2는 다음 goal 입력이다. 세부는 「현재 결과 / 문서 실사」다.
- 다음 Rules goal 순서: 메인 `msg_42d892af6e89`(13:13:52Z, E/session/wait14.json)가 사용자 결정 1A·2A를 전달했다(「적용 중인 사용자 결정」). 이 goal은 결과 기록의 다음 goal 통지만 고친다. 앞 처분의 임시 문구 「규칙 문서 분화(앞에 순서 변경 안건 대기)」는 이 통지로 바뀐다. 새 goal 초안·branch는 만들지 않고, PR232 병합 뒤 R-8로 여는 새 리드가 연다.
- BACKLOG 범위 변경: 메인 `msg_30cd8b7b30d8`(13:22:13Z, E/session/wait15.json). 사용자가 BACKLOG 갱신을 직접 지시했고 BACKLOG를 고치는 열린 PR이 PR232뿐이라 메인이 여기로 보냈다. 새 행 셋과 갱신 둘이며 행의 구현과 개념 조사는 하지 않는다. 바뀐 부분 재실사에 새 행·갱신 행을 넣고 후보 도착 검사를 다시 확인한다. bytes 허용(Q2)은 정본 문장 묶음 기준이라 BACKLOG 증가에는 적용되지 않고, BACKLOG 줄 수·bytes 변화는 결과 기록에 적는다.
  - 메인 해석(사용자에게도 알림): 「개념을 먼저 정립하고 진행」은 1A·2A로 정한 다음 goal을 멈추라는 뜻이 아니라 그 밖의 자기 개선 루프·그래프 작업에 붙는 선행 조건이다.
  - 리드 확정: ID 셋은 메인 제안 그대로다. `rule-document-subdivision`의 출처 칸에는 메인 처분 `msg_3bc350db7c54`가 분화 goal 입력으로 둔 문서 실사 O1·O2도 더했다.
- Gardener 결정 요청: 리드 status `msg_0f80e7631586`(2026-10-11T14:01:44Z, 본문 E/session/gardener-decision-body.md). 안건 1은 정리 후보 둘의 BACKLOG 반영, 안건 2는 Gardener 계약 이탈 셋의 처분이다.
  - 안건 2: 메인 `msg_9cabdebd16c1`(14:03:06Z, E/session/wait19.json)의 메인 운영 결정 A, 비차단 운영 기록이다. 사용자 결정이 아니다. 선례는 `msg_9b35d66297d6`·`msg_3bc350db7c54`다.
  - 안건 1: 사용자 결정 A(「적용 중인 사용자 결정」), 메인 전달 `msg_491f67bea26c`(14:05:24Z, E/session/wait20.json).
- 마감: 같은 `msg_9cabdebd16c1`이 사용자 지시 「일단 슬슬 마감 준비도 하자, 각 파트 마지막 작업 끝나면 재개 포인트 잡고 마무리하는 방향으로 지시해줘」(메인 pane Enter 제출)를 전달했다. 메인은 Rules의 마지막 작업을 결과 기록 커밋·push로 정했다. 바뀐 부분 재실사와 승인 묶음은 다음 세션이다.
- 정합 탐색(초안 6절): 검색어 `autocompact`, `claude --model`, `claude --resume`, `--wait`, `깨`, `자동 알림`, `DB`와 검증자 모델, `10-31`·`10월 31일`. 범위는 AGENTS·ORCA·RESUME·`.agents/`·`00_Document/conventions/`·`99_Tools/README.md`·BACKLOG다. 고칠 곳은 「건드릴 곳」에 있고, 그대로 둔 곳과 이유는 아래와 같다. 표의 줄 번호는 base `a570dbf3` 기준이다. HEAD에서 ORCA 140·223·91·92·128행은 141·224·92·93·129행이다.

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
- **다음 Rules goal 순서**(메인 전달 `msg_42d892af6e89`, 2026-10-11T13:13:52Z, 현황판 결정 응답): 「1) 루프 진단 1 - 후보 출구 goal을 규칙 문서 분화 앞에 넣을지 → A 분화 앞에 넣는다 · 2) 루프 진단 2 - 반복 실패 기준을 「같은 계약의 독립 판정 3회 막힘」으로 넓힐지 → A 넓힌다」. PR232 다음 Rules goal은 「후보 출구와 10-31 시범 평가 준비」이고 규칙 문서 분화는 그 뒤다. 메인 전달은 2)를 그 다음 goal 범위에 넣으라고 했다. 이 goal의 S1은 같은 계약의 독립 판정 1회 막힘이라 어느 기준으로도 체크포인트 전이다.
- **BACKLOG 갱신과 개념 선행**(메인 전달 `msg_30cd8b7b30d8`, 2026-10-11T13:22:13Z): 메인 pane Enter 제출 원문은 「OK 그럼 일단 백로그 갱신해줘, 자기개선 루프랑 그래프는 실제 최신 자료 기준으로 개념을 먼저 정립하고 진행해야될 것 같다」다. 같은 흐름의 현황판 결정 응답은 아래와 같다.
  - 3C 코멘트: 「이 그래프라는 정의가 아직도 난 잘 모르겠는데, 실제로 각각의 작업을 결정론적 노드화를 해서 Flow를 정의하겠다는건 이해를 하겠거든? 근데 현재 Terminal에 Claude Code와 Codex Harness 기준으로는 이걸 강제하기가 어려울 것 같다는 말이지, 결국 Agent SDK로 내가 뼈대를 직접 만들면서 해야한다는건데 "AgentDeck" 개발하는거처럼?」
  - 4C 코멘트: 「Jev도 모델 라우팅 기준이나 어떤 일을 해야하는지 기준이 명확한 부분에 LLM이 직접 라우팅 하는거 보다, 절약된다는 증거가 있으면 도입 할 만 한데, 그리고 우리가 지금 Orca 오케스트레이팅 스킬에서 대기하거나 응답하는데에 토큰을 많이 쓰니까, 그 부분을 만약에 Jev로 토큰 절감을 할 수 있는지가 문제야」
  - 재질문 응답: 「1) 루프 진단 3(재질문) - 흐름을 쥐는 바깥 프로그램을 지금 만들지 → A 지금은 안 만든다 · 2) 루프 진단 4(재질문) - 대기·응답 토큰부터 재고 Jev를 판단할지 → A 측정 먼저」. 메인은 이를 3A·4A로 부른다.
  - 이 goal은 BACKLOG 행으로만 반영한다(「만들 것」 3).
- **Gardener 정리 후보 둘**(메인 전달 `msg_491f67bea26c`, 2026-10-11T14:05:24Z, 현황판 결정 응답): 「대시보드 결정 응답: 1) PR232 - 라우팅 핫픽스 정본 Gardener 정리 후보 둘을 백로그에 넣을지 → A 넣는다」. 리드 결정 요청 `msg_0f80e7631586`의 안건 1이다. 착수는 동결 뒤이고 지금은 기록만 한다. 반영은 「현재 결과 / 종료 점검(Gardener)」에 있다.
- **대기 55분 연장 후보**(메인 전달 `msg_413d0b280e14`, 2026-10-11T14:08:24Z, 현황판 결정 응답): 「대시보드 결정 응답: 1) 깨우기 토큰(재질문) - 대기 시간 55분 연장만 다음 Rules 작업 후보로 넣을지 → A 55분 연장만 후보로」. ORCA 우편함 대기의 `--timeout-ms 900000`(15분)을 55분으로 늘리는 문서 수정을 다음 Rules goal의 후보로 둔다. 코드 필터 셋(대기 감싸기, Orca 안내 거르기 hook, 우편함 처리 묶기)은 동결 뒤 그대로다. 이 goal은 ORCA와 BACKLOG를 고치지 않고 「다음 계획 후보」에 한 줄로만 적는다.

## 현재 결과

### 착수

- 진입: Run `run_bea2c841fa15`(2026-10-11T11:36:00Z, E/session/run-create.json), READY status `msg_393cc84eb17f`(11:36:24Z). 진입 우편 소비 원시는 E/session/check-entry.json이다. 메모는 E/lead-context.md 「진입·범위 초안 차례」다.
- 범위 초안 v1 `msg_162e62a9c2b2`(11:45:11Z) → 메인 승인 `msg_9494d6bd3ac5`(11:49:51Z).
- 시작 기록: branch `docs/routing-hotfix-canon-20261011`을 origin/main `a570dbf3`(11:50:06Z fetch)에서 만들었다. 시작 커밋은 이전 goal 이월, 이 goal, CURRENT Rules 줄이다. 착수 메모는 E/lead-context.md 「착수 차례」(11:52:35Z)다. 이월 편집은 E/carryover-edit.mjs가 바꿀 원문이 정확히 한 번 있는지 단정하며 했다(E/carryover-edit-result.json).

### 정본 문장과 리드 대조

- 작성자: 신규 `gpt-6.1-sol` max(첫 화면 「GPT-6.1-Sol max」, rollout turn 모델 `gpt-6.1-sol`, backend unknown). Task `task_a1115b33e401`, Dispatch `ctx_b0e1f9945f0f`. 계약은 E/sentences-contract.md v1(SHA256 `b0495978…f682`), 고정 HEAD `b9a83152`다. 12:07:14Z에 시작해(worker-start 시각, Enter 12:07:27Z) worker_done succeeded `msg_d39708e41ade`(12:23:56Z)로 끝났다. 보고서 E/sentences/report.md, 기동 기록 E/sentences-start-note.md.
- 바뀐 자리(HEAD 줄 번호): ORCA 43·88·207행 기동 명령에 `--autocompact 250000`, 45행 문단 앞에 실행 한정·첫 턴 압축·되돌림 조건(결정 1). ORCA 91행 새 줄에 자동 알림으로 깼을 때의 처리와 5분 감시를 넣었고 원래 91행은 92행으로 그대로다(결정 2). 목표 루프 스킬 55행 새 줄(결정 3). 스킬 58행과 ORCA 215행의 평가 안건(결정 4).
- 리드 R-2(E/r2-lead.mjs, E/r2-lead.json): 보존 줄 11개가 바이트 단위로 같고 각각 한 번만 있다. 두 파일의 제목·`<a id>`·링크 목록이 고정 HEAD와 같다. 바뀐 자리는 위 여덟 곳뿐이고 numstat은 ORCA 6/5, 스킬 2/1이다. 사용자 원문 `msg_ca6641b7a9e2`, 정정판 `msg_9494d6bd3ac5`, 진입 할 일 `msg_300feadfab41`과 문장 뜻을 대조했다.
- bytes: 여섯 파일 묶음 103,306 → 105,254(+1,948). ORCA +1,209(245→246줄), 스킬 +739(103→104줄), 나머지 넷은 0이다. 결정별로는 533·547·613·255다(Sol E/sentences/work/check-output.json). 예상 +1,774보다 174 크다. 결정별 예상은 562·473·582·157이고 차이는 −29·+74·+31·+98(합 +174)이다. 이 값은 산식(ORCA=P1+(1+P2)+P3+P4+(P5+1−P5OLD)+(1+P8), 스킬=(P6+1)+P7)의 이음 바이트 넷을 각 결정에 넣은 것이다. 실사 계약과 아래 질문 본문은 이음 바이트를 빼 결정별 예상 합이 1,770, 차이 합이 +178로 4 어긋났다(문서 실사 R3). 리드가 status `msg_c95cbb6ba789`(12:27:09Z)로 물었고 메인이 `msg_f876e290be4c`(12:27:34Z)로 「A 그대로 진행」이라 답했다. 메인 판단이며 사용자 결정이 아니다. 늘어난 것은 결정 넷의 문장뿐이고 새 규칙이 없어 사용자 허용 「A 허용」 안이라는 판단이다.
- 생존 신호(E/sentences-liveness.json, `99_Tools/Orca/check-liveness.mjs`): `within`, 신호 7, 최대 간격 271초. 300초 초과 구간은 없다. Sol pane은 tui-idle 확인 뒤 12:25:56Z에 닫았다.
- 정산 순서: [Orca 위임 지침](../../../.agents/skills/dawnholder-goal-loop/references/orca-work.md)은 `worker-release` 뒤 `terminal close`인데, 리드가 pane을 먼저 닫고(12:25:56Z) release를 뒤에 했다(12:30:01Z, E/sentences-release.json). 결과는 `retained`(`external_terminal`, `processAction` none)다. 영향이 없다는 것은 retained 결과가 순서대로 한 이전 release와 같다는 제한된 관측과 리드 판단이다. release receipt에는 서버 시각 필드가 없어 12:30:01Z는 receipt 파일 mtime이다. 문서 실사 R2로 차단됐고 메인이 비차단 운영 기록으로 처분했다(`msg_3bc350db7c54`, 메인 운영 결정). 첫 발생이라 이 goal에 기록한다. 두 번째 발생이면 lifecycle wrapper(release·close 순서를 강제하는 감싸기 명령) 검사화 후보로 BACKLOG에 올린다.
- PR232: 커밋 `95033862`(정본 문장·BACKLOG·이 goal 기록)를 push하고 12:29:30Z에 열었다(E/session/pr-create.txt, E/session/pr-body.md). 실사 고정 head는 실사 계약에 적는다.
- BACKLOG(리드, E/backlog-edit.mjs): 갱신 둘(`operating-trial-evaluation`에 결정 4, `lead-mailbox-receive-delay`의 열린 질문에 답 「A 유지」)과 새 절 「라우팅 핫픽스 정본 반영 goal에서 연결한 후보」의 두 행. 141,329 → 145,559 bytes, 276 → 285줄(묶음 밖). 후보 도착 검사는 `allowed`(ID 116, 중복 0, 후보 2, unknown 0, E/backlog-check-after.json)이고 이전 goal도 `allowed`다(E/backlog-check-after-prev.json).

### 문서 실사

- 실사자: 신규 `gpt-6-astra` xhigh(첫 화면 「GPT-6-Astra xhigh」, rollout turn 모델 `gpt-6-astra`, backend unknown). Task `task_1fb1fc8f939c`, Dispatch `ctx_d4d002768b81`. 계약은 E/review-contract.md v1(SHA256 `09887d9e…783f`), 고정 head `c2c3e2a3`이다. 12:38:59Z에 시작해 worker_done succeeded `msg_8b86e9290459`(13:01:00Z)로 끝났다. 판정 E/review/verdict.md(SHA256 `13fa28da…2582`), 기동 기록 E/review-start-note.md.
- 판정: 차단 다섯. 변경 경계, 실제 bytes·줄 수, 사용자 원문 인용, 후보 도착 검사는 일치했다.
  - S1(정본 문장): ORCA 91행 둘째 문장의 「이때」가 정정판 결정 2의 「대기가 메모리 회수로 꺼졌거나 살아 있는지 확실하지 않으면 check」를 자동 알림으로 깬 경우로 좁힌다. 정정판에는 그 한정이 없다. 리드 작성 계약의 보존 설명이 이 축소를 유도했다.
  - R1(리드 메모 늦음): E/lead-context.md 「리드 R-2·goal 기록·커밋·PR 차례」의 시계가 12:27:29Z다. 그 차례의 첫 근거 E/r2-lead.json은 12:24:45Z, 작성자 pane close는 12:25:56Z, bytes 질문 본문은 12:27:04Z에 썼다. 그 메모는 사후 메모이며 사전 메모로 소급하지 않는다.
  - R2(정산 순서): 위 「정본 문장과 리드 대조」의 정산 순서 줄.
  - R3(bytes 예상): 위 bytes 줄.
  - R4(이월 기록): 「만들 것」 2.
- 실사자 자기 위반 V1: 생존 신호 helper(E/review-liveness.json) 결과는 `over`, 신호 10, 최대 307초다. 300초 초과는 한 구간(12:53:24Z → 12:58:31Z)이고 실사자가 status `msg_5493aa7c72e0`로 스스로 알렸다.
- 비차단 관찰: O1은 ORCA 45행 문단을 압축 규칙과 기존 pane 안내로 나누자는 것이다. O2는 ORCA 215행에 평가 이름과 날짜를 밝히자는 것이다. 이번에는 고치지 않고 결과 기록에서 다음 goal 입력으로 넘긴다.
- 리드 R-2(E/r2-review.md): 다섯 모두 원천에서 재현됐다. 후보 도착 검사를 다시 돌려 `allowed`(ID 116, 중복 0, 후보 2, unknown 0, E/r2-review-backlog.json)를 얻었다. 여섯 파일 bytes 103,306 → 105,254도 실사와 같다.
- 정산: peek 0건 → worker-release 13:04:36Z(`retained`, `external_terminal`, E/review-release.json) → terminal list·tui-idle·화면 확인 → close 13:04:53Z(E/review-close.json). release 뒤 close 순서를 지켰다.
- 메인 처분 `msg_3bc350db7c54`(13:09:28Z): A. R1·R2·V1을 비차단 운영 기록으로 둔다. 메인 운영 결정이며 사용자 결정이 아니다. 결정 요청은 리드 status `msg_589385d78bf9`(13:07:33Z)다.
  - R1: 사후 메모와 시점 차이를 위에 적었다. BACKLOG `contract-context-check` 출처 칸에 근거를 더했고 새 행은 만들지 않았다. 메인 문장은 `session-entry-memo-order`도 들었지만 그 행은 세션 진입 순서 후보이고 이미 goal 승격으로 처분됐다. 그래서 리드 판단으로 붙이지 않았고 이유는 수정 완료 보고에 적는다.
  - R2: 위 정산 순서 줄을 제한된 관측과 리드 판단으로 낮춰 썼다.
  - V1: 선례 `msg_9b35d66297d6`대로 비차단 운영 기록이다. BACKLOG `heartbeat-interval-review`에서 같은 부류와 묶어 본다. 그 행의 선행 조건이 goal별 초과 기록이라 BACKLOG는 고치지 않았다.
- 수정: S1은 신규 `gpt-6.1-sol` max가 고친다. 같은 계약의 확정 실패 1회째이고 같은 산출물 수정은 첫 번째다. 계약 E/fix-contract.md v1(SHA256 `c950eb86…63c5`)에 정정판 결정 2 (다)가 조건 없이 적용된다는 문장을 넣었다. Task `task_315eb423b6a1`, Dispatch `ctx_a45d625e504a`, 13:15:21Z 시작(E/fix-start-note.md). R3·R4와 처분 기록은 리드가 이 파일과 BACKLOG에 적었다. 바뀐 부분 재실사는 신규 `gpt-6-astra` xhigh다.
- S1 수정 결과: worker_done succeeded `msg_2bfb54e4c4f5`(13:25:05Z), 보고서 E/sentences-fix/report.md. ORCA 91행 둘째 문장의 「이때 」만 지웠다. 둘째 문장은 「대기가 메모리 회수로 꺼졌거나 살아 있는지 확실하지 않으면 `--wait` 없는 check로 처리한다.」가 됐다.
- 리드 R-2(E/fix-r2.mjs, E/fix-r2.json): 바뀐 줄은 91행 하나다. 91행 첫·셋째 문장과 92행은 바이트 단위로 같다. 제목 21·`<a id>` 21·링크 60이 고정 head와 같다. ORCA는 246줄 그대로이고 42,293 → 42,286 bytes(−7)다. 여섯 파일 묶음은 105,254 → 105,247 bytes가 된다.
- 정산: peek 0건 → worker-release 13:25:33Z(`retained`, `external_terminal`, E/fix-release.json) → terminal list·tui-idle·화면 확인 → close 13:25:43Z(E/fix-close.json). 생존 신호(E/fix-liveness.json)는 `within`, 신호 3, 최대 간격 211초다.

### BACKLOG 추가(사용자 직접 지시)

- 근거: 메인 `msg_30cd8b7b30d8`(「요구사항 원천과 적용 결정」, 「적용 중인 사용자 결정」). 메모는 E/lead-context.md 「BACKLOG 추가 차례」(13:22:50Z, 첫 쓰기 전)다.
- 편집(E/add-backlog-edit.mjs, E/add-backlog-edit-result.json): 각 앵커가 정확히 한 번 있는지 단정하고 기존 칸 끝에만 덧붙였다. 새 행 셋은 「라우팅 핫픽스 정본 반영 goal에서 연결한 후보」 절 끝에 두었다. 절 머리에는 셋째~다섯째 행의 출처를 한 문장으로 더했다.
- BACKLOG 크기(묶음 밖, `wc -lc`): 기준 `a570dbf3` 141,329 bytes·276줄 → 실사 고정 head `c2c3e2a3` 145,559 bytes·285줄 → 지금 151,402 bytes·288줄이다. 실사 처분의 R1 근거가 +528 bytes, 사용자 직접 지시가 +5,315 bytes·+3줄이다. 사용자 직접 지시분 가운데 다섯 행은 +5,219이고 나머지 96은 절 머리 문장이다(Gardener 표본 14).
- 추가 뒤 후보 도착 검사(13:27:04Z, E/add-backlog-check.json): `allowed`, ID 119, 중복 0, 후보 5, 인용 없음 0, 모르는 ID 0이다. 이전 goal도 `allowed`(후보 10, E/add-backlog-check-prev.json)다. 이 줄은 처음 결과 기록에서 빠졌고 Gardener 표본 15가 찾았다.

### 종료 점검(Gardener)

- 점검자: 신규 `claude-opus-5-5`(화면 「Opus 5.5 with xhigh effort」, backend unknown), 태그 `[Rules 검증자]`, Task `task_dc58718e29f9`, Dispatch `ctx_fbac1f3bc722`, 2026-10-11T13:36:22Z~13:56:09Z, worker_done `msg_167f3909585a`(succeeded). 병합 전 고정 head `02fa6423`(메인 판단). 계약 E/gardener-contract.md v1(SHA256 `dbc9f30f…0182`), 보고 E/gardener/context-report.md, 기동 기록 E/gardener-start-note.md다. 고정 입력 78개(E/gardener-inputs.json)의 hash는 시작과 끝이 같았다.
- 집계: 같은 계약·결함 번호의 확정 실패 3회 도달은 0이다. 현행 기준과 사용자 결정 2A 기준(같은 계약의 독립 판정 3회 막힘)의 값이 같다.
  - 번호 결함은 다섯(S1·R1~R4)이고 실사자 자기 위반 V1이 하나다. R1~R4는 리드 귀속이고, S1은 작성 Sol 주·리드 계약 유도다.
  - S1·R3·R4는 정정이 커밋됐고 독립 확인은 바뀐 부분 재실사가 맡는다. R1·R2·V1은 메인 처분이다.
  - 같은 산출물 수정 3회 초과 체크포인트 도달도 0이다. CI 실패·재실행, 승인 없는 경고 억제·설정 완화, 검사를 끈 우회는 모두 0이다.
- 드리프트 발견:
  - 리드 압축 기준 25만이 ORCA 43·45·88·207행 네 곳에 반복되고, 리드 기동 명령(43행)에는 TEMP·TMP가 없다. 50만 되돌림 조건이 발동하면 세 명령이 함께 낡는다(추론).
  - ORCA 91행 둘째 문장 「살아 있는지 확실하지 않으면 `--wait` 없는 check」 뒤에 대기를 다시 여는지가 정해져 있지 않다(추론). 바뀐 부분 재실사 계약의 입력으로 둔다. 재실사가 결함으로 판정하면 그때 메인에 올린다.
  - 메인 5분 화면 감시가 메인 진입 문서에 없고, 10-31 평가 안건이 흩어져 있다. 둘 다 다음 Rules goal 입력이다(「다음 계획 후보」 표).
  - 줄 상한·분화 문구가 정본에 없고 BACKLOG·goal에만 있다. 앞 goal Gardener와 같은 발견의 반복이라 `rule-document-measure-helper` 근거로 더했다.
- 반복:
  - 공식 계약 draft 미제출은 Codex 기동 3/3이고, draft 크기는 셋 모두 계약 글자 수 + 4,787 + 1이다. 계약 인자 길이 실패는 리드 스크립트의 `spawnSync`로 미리 피했다(다섯째 goal).
  - 작업자 heartbeat 5분 초과는 Dispatch 셋 중 하나(실사 307초)다.
  - 앞 goal들과 이어지는 것: 리드 기록 수치·근거 오류(R3·R4), 리드 계측 산식 오류(R3), 리드 계약이 유도한 결함(S1), bytes 재질문, 목표 중간 BACKLOG 추가, 고정 head의 재개 지점 낡음, 리드 진입의 가드 차단 둘이다.
  - 리드 차례 메모 늦음(R1)은 가드 규칙 3 뒤 첫 사례이고, 정산 순서(R2)는 첫 발생이다.
- 리드 기록 대조: 표본 15 중 14 일치, 1 기록 누락이다. 표본 밖에서 낡음 하나와 기준 혼용 둘(틀림이 아니라 표기)이 나왔다. 이 결과 기록에서 모두 고쳤다.
  - 기록 누락: BACKLOG 추가 뒤 후보 도착 검사(「BACKLOG 추가」 절 마지막 줄).
  - 낡음: 고정 head의 재개 지점·진척 단계.
  - 기준 혼용: 작업자 「시작」은 worker-start 시각으로 맞췄다(「정본 문장과 리드 대조」 첫 줄). 「범위」와 정합 표의 ORCA 줄 번호는 base 기준임을 밝혔다.
  - 「BACKLOG 추가」의 +5,315 bytes가 절 머리 문장 96을 포함한 값임을 밝혔다.
- 정리 후보 둘(동결 뒤 착수 전제): 리드가 status `msg_0f80e7631586`로 물었고 사용자가 「A 넣는다」로 채택했다(「적용 중인 사용자 결정」).
  - 후보 1은 새 대기 행 `lead-launch-command-source`다.
  - 후보 2는 `lead-memo-audit-field-check`의 범위·근거를 넓혔고, `contract-context-check`에서 그 행을 가리킨다.
  - 반복 근거는 `official-draft-check-helper`(draft 3/3, 인자 길이)와 `rule-document-measure-helper`(R3 산식, bytes 재질문, 크기 규칙 부재)에 더했다.
  - 편집은 E/closeout-backlog-edit.mjs가 앵커가 행 안과 파일에서 정확히 한 번인지 단정하며 칸 끝에만 덧붙였다(E/closeout-backlog-edit-result.json). BACKLOG는 151,402 → 157,536 bytes(+6,134), 288 → 289줄이다(묶음 밖).
  - 후보 도착 검사(14:10:14Z, E/closeout-backlog-check.json)는 `allowed`, ID 120, 중복 0, 후보 7, 인용 없음 0, 모르는 ID 0이다. 이전 goal도 `allowed`(후보 10, E/closeout-backlog-check-prev.json)다.
- Gardener 계약 이탈 셋(쓰기 아님): `git cat-file -s` 1회, 일반 파이프 둘, `cd`다.
  - `cd`는 Bash 호출 44개에 있고 E로 옮긴 명령은 넷이라 보고서의 「두 번」과 수치가 다르다(E/gardener-r2.md, 세션 기록 102호출 대조). 쓰기는 보고서 한 파일뿐이었다.
  - 처분은 메인 운영 결정 A(`msg_9cabdebd16c1`)로 비차단 운영 기록이다. 사용자 결정이 아니다.
- 리드 R-2(E/gardener-r2.md): 「전부/없음」 주장 여섯은 원천과 일치했고 새 발견 일곱은 재현됐다. CI는 14:00:09Z에 다시 조회했다(E/pr232-ci-2/). 고정 head `02fa6423`의 code-rules·module-boundaries·architecture-tests·dotnet-tests 넷 모두 success, attempt 1이다.
- Gardener 생존 신호(E/gardener-liveness.json): 신호 22, 최대 90초, 300초 초과 0이다. 정산: peek 1건(같은 worker_done) → `worker-release` 13:57:43Z(retained) → terminal list·tui-idle·화면 확인 → `terminal close` 13:58:02Z.

## 다음 계획 후보

이 goal 밖으로 둔 일이다. 후보마다 BACKLOG ID를 단다.

- 리드 메모 감사 필드 검사 helper(보충 2): 메모 차례마다 「완료 뒤 실제」 칸과 자기 쓰기 감사 줄이 채워졌는지 검사한다. 종료 점검 후보 2로 차례별 메모 시계와 그 차례 첫 쓰기의 순서까지 넓혔다(사용자 채택). 도구라 동결 뒤다. BACKLOG `lead-memo-audit-field-check`.
- 리드 기동 명령의 단일 출처와 checkout 안 TEMP(종료 점검 후보 1, 사용자 채택): 리드 기동·재개 명령을 helper 한 곳에서 만들고 TEMP·TMP를 작업 경로 안에 둔다. 도구라 동결 뒤다. BACKLOG `lead-launch-command-source`.
- 우편함 대기 55분 연장(사용자 결정 A, `msg_413d0b280e14`): ORCA 우편함 대기의 15분 제한을 55분으로 늘리는 문서 수정이다. 다음 Rules goal의 후보로 두고 그 goal에서 Orca `check --wait`와 Claude Code background 작업의 최대 시간, 길어진 대기 동안 작업자 정지 감지 지연을 메인 화면 감시가 메우는지를 먼저 확인한다. 측정 근거는 다음 goal에서 관련 BACKLOG `wake-token-filter`에 붙인다.
- Unity 시험 harness의 사용자 미커밋 파일 표시(보충 셋째): 전후 git 상태에서 알려진 사용자 미커밋 파일을 「리드 복원 대상」으로 따로 표시한다. BACKLOG `unity-harness-user-file-restore-mark`.
- 자기 개선 루프·에이전트 그래프 개념 정립(사용자 직접 지시): 최신 원천으로 개념·용어를 정하고 이 프로젝트의 무엇에 대응하는지 정리한다. 남은 자기 개선·그래프 작업의 선행 조건이다. BACKLOG `loop-graph-concepts`.
- 흐름을 쥐는 바깥 프로그램(사용자 3C·3A): Agent SDK 뼈대이며 지금은 만들지 않는다. BACKLOG `agent-flow-program`.
- 대기·응답 토큰 측정과 깨우기 필터·Jev 판단(사용자 4C·4A): 측정을 먼저 한다. BACKLOG `wake-token-filter`.

다음 Rules goal은 「후보 출구와 10-31 시범 평가 준비」이고 그 뒤가 규칙 문서 분화(BACKLOG `rule-document-subdivision`)다(사용자 결정 1A·2A, 메인 전달 `msg_42d892af6e89`). 새 goal 초안·branch는 PR232 병합 뒤 R-8로 여는 새 리드가 열며 자동으로 시작하지 않는다. 다음 goal로 넘기는 입력은 아래와 같다.

| 입력 | 받을 goal |
|---|---|
| 문서 실사 O1(ORCA 45행 문단 나누기)·O2(ORCA 215행 평가 이름·날짜) | 규칙 문서 분화 |
| Gardener 관찰: 10-31 평가 이름이 넷으로 흩어졌고 준비 행 `operating-trial-evaluation`에 구현자 모델 시범 평가·세션 쓰기 가드 자료가 없다 | 후보 출구와 10-31 시범 평가 준비 |
| Gardener 관찰: 메인 5분 화면 감시가 ORCA 91행에만 있고 메인 진입 문서(CLAUDE.md·RESUME)에 없다 | 후보 출구와 10-31 시범 평가 준비 |
| 우편함 대기 55분 연장 후보 | 후보 출구와 10-31 시범 평가 준비 |
