# 규칙·운영 V1.0 — 후보 도착 검사와 합류점 절차

## 재개 지점

Rules의 목표이며 운영 셋업 3단계 「계획 경계」와 사용자 지시 「규칙·운영 V1.0 셋업 뒤 main 통합·재분화」를 묶는다. 사용자가 범위를 승인했다(아래 「적용 중인 사용자 결정」). 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch: 정본 PR은 `docs/ops-v1-canon-20261009`(base main `89a2c022`)다.
- 근거 폴더 E: `.backups/verification/2026-10-09-plan-boundary-and-junction/`(Git 제외). 리드 맥락 메모는 [lead-context.md](../../../.backups/verification/2026-10-09-plan-boundary-and-junction/lead-context.md), 승인 범위는 [scope-draft-v1.md](../../../.backups/verification/2026-10-09-plan-boundary-and-junction/scope-draft-v1.md)(SHA256 `22cdc5b6…`)와 [scope-revision-v2.md](../../../.backups/verification/2026-10-09-plan-boundary-and-junction/scope-revision-v2.md)(SHA256 `23619ccf…`)다. 받은 메시지 원시는 E/session/에 있다.
- 리드: 신규 `claude-opus-5-5` xhigh(화면 「Opus 5.5 ⚡xhigh」, backend unknown), 태그 `[Rules 리드 Opus]`, handle `term_5f6a014b-4ec5-45f9-9103-b84cc11c18d2`, Run `run_ee8c71e0897d`. 메인 주소는 메인 term handle이다. 이전 Rules goal의 Run·Task·Dispatch·handle은 실행 권한이 아니다.
- **현재 위치**(2026-10-09T10:37:18Z): 정본 PR의 Sol 작성과 리드 R-2를 마쳤다. 결과는 「정본 PR 작성」이다.
- **다음 할 일**: 문서 실사 → 규칙 점검 보고서 → 보고서 Opus 검토 → 정본 PR 승인 묶음.
- 이 goal의 마지막 PR(종료 기록 PR) 병합 커밋이 합류점 J다.

## 진척 단계

- [x] 범위와 기준 확정
- [>] 정본 PR 작성·실사
- [ ] 규칙 점검 보고서
- [ ] 정본 PR 병합
- [ ] 후보 검사 선행 시험
- [ ] 후보 검사 구현·검증
- [ ] 후보 검사 PR 병합
- [ ] 결과 기록·Gardener
- [ ] 종료 기록 PR 병합
- [ ] 종료 점검과 R-8

PR 단계 이름은 PR이 생기면 「PR### 병합」으로 바꾼다.

## 범위

승인 범위는 초안 v1과 개정 v2다. 넣는 기준은 (가) 진입 지시의 범위 씨앗 1~6, (나) 같은 범위에서 보라고 한 BACKLOG `backlog-id-extraction-check`, (다) 사용자 수정 둘(구현자 난이도 라우팅, 규칙 전체 점검 HTML 보고서), (라) 규칙 문서 bytes를 늘리지 않기 위한 상쇄다.

### 만들 것

정본 PR(문서와 보고서)

1. 기록 양식 한 줄(씨앗 2): 작업 맥락 양식 머리말에 「원천을 옮겨 적을 때는 원천의 확인 수준을 유지하고 관측과 추론을 나눠 적는다」.
2. worktree 규칙 맞춤(씨앗 3): 규칙 문서 PR 병합 뒤 살아 있는 리드는 다음 안전 지점(새 계약·세션을 열기 전)에 최신 main을 받고, 닫힌 파트는 재진입 첫 일로 받는다. 떠 있는 세션은 새 세션부터 반영된다. 문장은 milestones 새 절에 두고 ORCA R-8은 그 절을 가리킨다.
3. 사용자 차례 알림(씨앗 4): 작업자가 사용자에게 명령·입력 차례를 넘기거나 보고를 마치면 리드에 status, 리드는 판단 없이 메인에 한 줄 status. 메인의 폴더 감시는 보조 그물. ORCA R-4와 위임 계약 양식 한 칸.
4. 합류점 통합과 재분화 절차(씨앗 5): milestones 새 절 「main 맞춤과 합류점」. branch 상태 네 가지(끝남·기록만 앞섬·검증이 남은 열린 PR·보류된 PR)의 처리·주체·시점. 이번 합류점 순서표는 이 goal 「이번 합류점 순서표」에 둔다(메인 M3).
5. V1.0 표시(씨앗 6, Q2 A): AGENTS 머리에 판 한 줄과 합류점 태그 이름 `ops-v1.0`. 태그는 J 병합 뒤 사용자 승인으로 단다.
6. 구현자 난이도 라우팅(수정 1, 시범): 구현자를 `gpt-6.1-sol` max와 `gpt-6-astra` xhigh 둘로 두고 리드가 위임 계약에 난이도 신호와 원시 근거로 고른다. 신호 다섯(열린·적대적 입력 해석, 시간·동시성·수명, 런타임 간 상호 운용, 설계 판단이 남은 새 다파일 기능, 같은 계약에서 Sol 구현의 독립 NOT PASS 두 번째) 중 하나라도 해당하면 Astra, 애매하면 Astra다. 상세 기준표는 goal-loop references의 새 파일 한 개, goal-loop에는 짧은 절. 맞출 곳: AGENTS 모델 라우팅과 태그 형식 한 줄(`[<파트> 구현 Astra]`)·「Sol」 역할 정의 한 문장, ORCA R-5 구현자 명령·capacity(Astra 배정은 재시도만)·확정 실패 네 번째 시도, 위임 계약 양식 모델 칸. Astra 구현 작업의 독립 검증자는 신규 `claude-opus-5-5`다. 10-31 Gardener 평가에서 사용자가 유지·조정·되돌리기를 정한다.
7. 규칙 전체 점검 HTML 보고서(수정 2): `01_Phases/reports/2026-10-09-operating-rules-v1-review/report.html`. 리드가 조사·본문·HTML을 쓴다. 정본 PR 문서 실사를 통과한 head의 정본 묶음이 대상이다. 규칙 지도, V1.0에서 바뀐 것, 역할·모델 라우팅 표, 사용자 손이 필요한 지점, 중복·충돌·낡은 문장 후보, 남은 V1.x, ASD-STE100 적용표를 담는다. 보고서에 STE 정본화 질문(A V1.x 추천 / B V1.0)을 싣는다.
8. bytes 상쇄(라): AGENTS 「Core 명칭과 리드 태그 전환」 문단의 Core 명칭 전환 부분 축소(메인 확인 `msg_f705412e769c`), RESUME 14행과 ORCA 75행의 지난 기록 가리킴 문장 이관. 모자라면 지난 기록 문장을 더 찾는다.
9. BACKLOG: `report-response-format` 승격, `lead-tag-transition-wording` 출처 덧붙임(전환 문단 축소). 이 goal 시작 기록과 CURRENT의 Rules 줄.

후보 도착 검사 PR(코드, 씨앗 1과 (나))

1. `99_Tools/Backlog/`의 읽기 전용 helper: BACKLOG 표 ID를 백틱 유무와 무관하게 뽑고(머리 칸이 `ID`인 표만) 중복을 낸다. goal의 `## 다음 계획 후보` 절에서 들여쓰기 없는 목록 줄마다 BACKLOG ID나 기존 goal 링크가 있는지, 인용한 ID가 BACKLOG 표에 있는지 판정한다. JSON 한 개(`allowed` 0·`policy-violation` 1·`input-error` 2, 개수, `code`·`path`·`line`·`message`·`repair` 진단). 파일 쓰기·네트워크·Git 변경 없음.
2. `99_Tools/Backlog.Tests/` 회귀 시험과 실제 사례 fixture 둘(병합 관문 goal의 BACKLOG 누락 두 행, 백틱 없는 ID 오집계).
3. CI `code-rules`에 시험 단계 하나, `99_Tools/README.md` 진입, goal-loop 「기준과 상태」 한 문장(종료 기록 PR 전 리드 실행, 실사자 재실행).
4. BACKLOG `backlog-id-extraction-check` 승격.

종료 기록 PR(문서) = 합류점 J

1. 결과 기록, Gardener 결과, 다음 계획 후보의 BACKLOG 등록.
2. 이 goal의 「다음 계획 후보」에 후보 도착 검사를 처음 실제 실행한다. 위반 0이어야 올린다.
3. 「이번 합류점 순서표」 최종판.

### 건드릴 곳

- 정본 PR: AGENTS(판 한 줄·모델 라우팅·태그 형식·전환 문단), ORCA(R-4·R-5·R-8·capacity·확정 실패·75행), RESUME 14행, goal-loop SKILL(짧은 절), `.agents/skills/dawnholder-goal-loop/references/`(milestones 새 절, 새 기준표 파일), templates(머리말·위임 계약), CURRENT Rules 줄, BACKLOG, 이 goal, `01_Phases/reports/2026-10-09-operating-rules-v1-review/`.
- 후보 도착 검사 PR: `99_Tools/Backlog/`, `99_Tools/Backlog.Tests/`, `.github/workflows/code-rules.yml`(단계 하나), `99_Tools/README.md`, goal-loop SKILL(한 문장), BACKLOG(승격 한 칸), 이 goal.
- 종료 기록 PR: 이 goal, BACKLOG(새 후보 행), CURRENT의 Rules 줄.

### 하지 않을 것

- 합류점의 실제 통합 실행. 각 파트 리드가 자기 branch와 PR로 한다.
- 남은 로드맵 넷(우편함 출력 유실 hook·하네스 마무리·보고 방식·자기 개선 루프). V1.x다. 단 보고 방식 중 `report-response-format`은 수정 2로 이 goal에서 첫 적용까지만 한다.
- ASD-STE100 정본화(REPORTING 개정). 보고서를 본 뒤 사용자가 정한다.
- Gardener 후보 둘(`worker-liveness-tool-check`·`official-draft-check-helper`). 「우편함 출력 유실 hook」 단계 몫이다.
- 후보 도착 검사의 CI 강제(PR의 goal.md에 거는 검사). 하네스 원칙 4의 warning 파일럿부터라 V1.x 후보다.
- Astra 배정의 capacity 때 Sol로 전환하는 예외(메인 M7 불수용). V1.x 후보다.
- CURRENT 경로 줄 구조 변경, 다른 파트의 goal·CURRENT 줄, 끝난 goal 문서, CLAUDE.md(메인 몫), 기존 세션의 모델 변경, 진행 중인 다른 파트 계약의 모델 소급 변경.
- `credential-separation-followup`, `receive-helper-review-followup`, `designated-temp-wording`, `powershell-clock-command`.
- 전역 Claude·Codex·Git·gh 설정, 현황판 코드. 태그·원격 변경은 사용자 승인 전에 하지 않는다.

### 관찰 가능한 완료조건

1. 정본 PR이 신규 `gpt-6-astra` xhigh 문서 실사를 통과한다. 현실 시나리오 다섯을 대조한다.
   - 합류점 J 직후 다섯 파트 branch(「이번 합류점 순서표」의 상태)를 정본만 보고 처리·주체·시점을 정한다.
   - 작업자가 사용자에게 차례를 넘길 때 계약 → 리드 status → 메인 한 줄 status로 이어진다.
   - 규칙 PR 병합 뒤 살아 있는 리드와 닫힌 파트가 언제 main을 받는지 정해진다.
   - 기록 양식 한 줄을 양식 머리말에서 바로 찾는다.
   - 새 위임 계약에서 정본만 보고 난이도 신호로 구현 모델을 고르고, capacity·두 번째 NOT PASS·확정 실패 때 다음 모델과 검증자 모델을 정한다.
2. 규칙 점검 보고서를 신규 `claude-opus-5-5`가 내용·근거·표시로 검토하고 실제 렌더를 데스크톱과 좁은 화면에서 본다. ASD-STE100 적용표의 각 규칙에 근거 또는 「사양 전문 미대조」 표시가 있다. 보고서는 정본 PR 승인 묶음과 함께 간다.
3. 후보 도착 검사의 선행 시험이 구현 전에 실패하고 구현 뒤 같은 명령으로 통과한다. 기존 시험은 그대로 통과한다.
4. 후보 도착 검사의 독립 검증자가 실제 진입점을 한 번 실행한다. 대상은 이전 Rules goal 「다음 계획 후보」와 현재 BACKLOG다. 실제 사례 fixture 둘에서 위반을 잡는 것도 확인한다.
5. 후보 도착 검사 PR head의 CI `code-rules`가 새 시험 단계를 실제로 실행해 통과한다(시험 개수로 대상 0건이 아님을 확인).
6. 종료 기록 PR 전 리드가 이 goal에 후보 도착 검사를 실행해 위반 0이고, 종료 기록 실사자가 같은 명령을 다시 실행한다.
7. ORCA는 250줄 이하다. 여섯 파일 묶음(AGENTS·ORCA·goal-loop·RESUME·세션 인계 스킬·orca-work)은 PR마다 bytes가 늘지 않거나 같은 PR에서 줄인 원시 수치를 남긴다. 기준은 main `89a2c022`의 102,601 bytes다. templates·milestones·새 기준표·README·BACKLOG·보고서는 묶음 밖에서 따로 잰다.
8. 각 PR은 메인 창의 승인 줄로 병합한다. 결과 기록 → Gardener → 종료 기록 PR(=J) → 종료 점검 → R-8 순서로 끝낸다. 태그 `ops-v1.0`은 J 병합 뒤 사용자 승인으로 단다. V1.x 단계는 자동으로 시작하지 않는다.

## PR 경계와 검증

| PR | 변경 경계 | 등급과 세션 |
|---|---|---|
| 정본 | 문서와 보고서 | 문서 실사. 작성 Sol `gpt-6.1-sol` max(난이도 신호 해당 없음: 명세가 정해진 문서 작성), 실사 신규 `gpt-6-astra` xhigh(시범). 보고서는 리드 작성, 신규 `claude-opus-5-5` 검토(REPORTING, 메인 M5). R-7 비해당 |
| 후보 도착 검사 | 도구 코드·시험·CI 한 단계·문서 두 곳 | 강. 선행 시험 신규 `claude-opus-5-5` → 구현자(정본 PR 병합 뒤 새 기준표로 배정) → 독립 검증(구현자 모델에 따라 시범 배정 또는 신규 Opus). R-7 비해당(메인 M1 `msg_51bd025d0b29`) |
| 종료 기록 | 문서만 | 문서 실사. 신규 `gpt-6-astra` xhigh |

- 순서: 정본 → 후보 도착 검사 → 종료 기록. 셋 다 이 goal을 고치므로 각 branch는 앞 PR 병합 뒤 최신 main에서 만든다. 정본 PR 실사·승인 대기 중에 후보 도착 검사의 선행 시험 작성자를 먼저 열 수 있다(시험 파일은 E에 두고 branch를 만든 뒤 리드가 옮겨 커밋).
- 적용 시점: 이 goal은 TDD 정본 병합 뒤 범위 승인을 받은 새 goal이라 후보 도착 검사 PR에 선행 시험을 적용한다. 문서 PR 둘은 대상이 아니다. 구현자 난이도 라우팅은 정본 PR 병합 뒤 내는 계약부터 쓴다.
- 규칙 문서 bytes: 늘면 같은 PR에서 지난 기록 문장을 이 goal 「정본에서 옮긴 적용 기록」으로 옮겨 상쇄한다. 모자라면 실사 전에 원시 수치와 함께 메인에 올린다(이전 goal 메인 판단 M3와 같은 방식).

### 위험

1. 합류점이 PR 세 번의 승인을 기다린다. 정본 PR이 보고서까지 담아 길어진다(추정 +3~4시간).
2. 구현자 라우팅은 Astra 구현 표본이 0인 상태의 시범이다. Astra가 낫다는 근거는 아직 없다(E/routing-evidence.md).
3. 후보 줄 형식이 goal마다 달라 다른 파트 goal에 검사를 돌리면 위반이 많이 나온다. 이 goal은 지정한 goal에만 돌린다.
4. 태그 push를 GitHub ruleset이 막는지 모른다.
5. 합류점 통합 때 여러 파트가 CURRENT 줄을 같이 고치면 DIRTY가 다시 나온다. 줄 띄우기 뒤 첫 실측이다.
6. 병합 관문 그물: gh·merge 또는 push·main 낱말이 함께 든 명령은 막힌다. auto mode가 push를 「Out-of-Place Publication」으로 막은 Core 사례(메인 `msg_c7bf46bc0419`)가 있다. 막히면 우회하지 않고 메인에 알린다.

## 요구사항 원천과 적용 결정

메인이 전달한 사용자 결정은 사용자 직접 입력과 구분한다. 메인 판단은 사용자 결정이 아니다. 원시와 정리는 E/main-decisions-log.md에 있다.

- 진입 지시: 메인 `msg_416f23a3207b`(2026-10-09T09:10:17Z, E/session/entry-msg_416f23a3207b.raw.json). 범위 씨앗 1~6과 메인 해석(이 goal이 운영 셋업 3단계이자 규칙·운영 V1.0, 마지막 PR 병합 커밋이 합류점).
- 범위 초안 v1: 리드 `msg_9ceb06a017d4`(09:20:02Z). 메인 판단 `msg_51bd025d0b29`(09:21:18Z): M1 R-7 비해당, M2 처음 불가, M3 순서표는 이 goal·일반 절차는 milestones.
- bytes 계획 v2: 리드 `msg_7ed952247304`(09:23:32Z). 메인 판단 `msg_f705412e769c`(09:24:02Z): Core 명칭 전환 부분 축소 수용(떠 있는 세션 중 `[GameDev …]` 사용 없음 확인), bytes 계획 v2 수용.
- 범위 개정 v2: 리드 `msg_6dee83b79520`(09:49:54Z). 메인 판단 `msg_bd7e0409e4c5`(09:50:29Z): 착수, M4 `[<파트> 구현 Astra]` 형식 한 줄 조건 수용, M5 보고서 검증자 Opus, M6 Astra 구현 작업 검증자 Opus, M7 Astra→Sol capacity 전환 지금은 불수용.
- 라우팅 근거: E/routing-evidence.md. Sol max 시기 14개 사례 중 3회 이상 걸린 영역은 셸 해석 보안 그물, PS5.1↔.NET DB 수명 도구, 서버 같은 틱 경합, Unity 실화면, 새 다파일 검사기였다. 같은 결함 번호의 재실패는 0건이라 「같은 번호 3회」 규칙은 발동하지 않았다. Astra 구현 표본은 0이다. 조사는 Claude 내부 읽기 전용 에이전트이고 리드가 원문 셋을 표본 대조했다.

## 적용 중인 사용자 결정

모두 메인 창 Enter 제출, 메인 전달이다. 직접 입력으로 격상하지 않는다.

- **V1.0 셋업과 main 통합**(진입 지시 `msg_416f23a3207b`, 18:0x KST): 「OK 일단 남은 워크트리 파트들 추가 진행하기전에, 한번정도 Main Branch에 통합하고, 그 통합된 Main Branch에서 또 워크트리 분화하는식으로 가는게 좋겠어 / 그전에 Rule쪽 더 논의해야하는 부분이 있다하면, 그 부분 먼저 최종적으로 V1.0 으로 셋업하고 main에 merge하고 최종적인 프로젝트 규칙 및 운영방식으로 셋업하고 넘어가자」.
- **이어지는 기존 결정**(진입 지시가 전달): 10-08 「main 합류점 → A 게임 재개 관문을 합류점으로」, 10-07 「Core·Content 재개 시점 → A 3단계 뒤 재개」, 10-09 11:42 「TDD 정본화 goal 종료 점검 → A 제안대로」와 「worktree 규칙 맞춤 → A 열 때 맞춤 + 규칙화」(원문은 메인 개인 도구 main-notes/2026-10-09/next-rules-goal-entry.md, 이전 Rules goal `msg_b92b1bafa87f`).
- **범위 승인과 수정 둘**(`msg_97fab6206b91`, 18:3x KST): 현황판 계획 검토의 A(「Q1 A, Q2 A」)와 원문 「그리고 아까 Rule쪽 계획을 읽어봤는데, A로 가되, 구현자를 6.1 Sol Max로만 한정지으니까 어려운 작업에서는 힘들어하던데 난이도별로 Astra xHigh랑 Sol Max를 구분지어서 라우팅하는 방향으로 마지막 수정만 하고 진행하자 / 그리고 최종 규칙을 한번 전체적으로 점검해보게 HTML로 보고서 작성해줘 / 이때 보고서 양식을 처음으로 우리 백로그에있는 ASD-STM100 with HTML을 같이 해결하면서 진행해보자」. 「ASD-STM100」은 BACKLOG `report-response-format`을 가리킨다는 것은 메인 해석이다. Q1 A는 V1.0을 씨앗 1~6으로 닫는 것, Q2 A는 AGENTS 판 한 줄과 합류점 태그 `ops-v1.0`(태그 push는 별도 승인)이다.
- **이전 결정의 대체**: 이번 원문의 구현자 난이도 라우팅은 이전 사용자 결정 「Astra 구현 격상은 사용하지 않는다」(메인 `msg_c78692a42979`, management-active 시스템 카드 goal 882행, 확정 실패 네 번째 시도 맥락)를 바꾼다. 시범 운영은 메인이 원문을 좁히지 않는 운영 방식으로 수용했다(`msg_bd7e0409e4c5`).

## 이번 합류점 순서표

실측: 2026-10-09T09:19:27Z, origin/main `89a2c022`, `git rev-list --left-right --count origin/main...HEAD`(뒤/앞). 원시는 E/session/branch-states.txt다. PR 상태(DIRTY·CLEAN·보류·V3 차단)는 진입 지시의 메인 확인(18:1x KST)을 옮겼다. 종료 기록 PR에서 최종판으로 다시 잰다.

| 순서 | 파트 | 지금 상태 | J 뒤 처리 | 주체·시점 |
|---|---|---|---|---|
| 0 | Rules | 이 goal | 종료 기록 PR 병합이 J. 다음 Rules goal은 J 이후 main에서 새 branch | 메인이 J 기록, 다음 Rules 리드 |
| 1 | Core | `feat/persistence-engine-judgment-20261006` 10/43, 앞선 diff는 영속화 goal·CURRENT 두 파일, push됨, PR 없음, 리드 살아 있음 | 기록만 앞섬: 다음 안전 지점에 J를 받는다. 영속화 goal을 잇는다. 인스턴스 맵 goal은 J 이후 main에서 새 branch(Core goal의 기존 결정) | Core 리드, 메인의 재개 신호 뒤 첫 안전 지점 |
| 2 | Content | `feat/items-inventory-ui-20261005` 222/11, PR191 DIRTY, 실화면 검증 보류, 리드 닫힘 | 검증이 남은 열린 PR: 재진입 첫 일로 J를 통합(6.6 위, ProjectSettings 예외는 이 통합 한 번) → 실화면 독립 검증 → 새 head 승인 | 새 Content 리드, 메인의 게임 재개 신호 뒤 |
| 3 | Management | `feat/intro-site-20261008` 24/37, PR207 CLEAN, 사용자 지시로 보류, 독립 검증 V3 차단 2건 | 보류된 PR: J 때 처리 없음. 재개 결정 뒤 J를 받고 차단 2건 처리 → 승인 묶음 | 사용자 재개 결정 뒤 Management 리드 |
| 4 | CodeMap | `docs/architecture-tests-ci-closeout-20261005` 273/0, 끝남, upstream 없음 | 받을 것 없음. 다음 goal을 J 이후 main에서 새 branch | CodeMap 리드, 재진입 첫 일 |

- Core와 Content는 같은 시기에 열 수 있다. 두 PR이 같은 시기에 열리면 뒤에 병합하는 쪽이 최신 main을 받는다.
- 메인이 받은 관찰(다른 파트 몫, 이 goal은 고치지 않음): CURRENT 「Content(Unity 업그레이드)」 줄의 `unity-upgrade-active` worktree가 지금 `git worktree list`에 없다. content-active 미커밋 두 파일, core-active 미추적 보고서 폴더 하나.

## 현재 결과

### 착수

- 범위 승인(`msg_97fab6206b91`)과 개정 확인(`msg_bd7e0409e4c5`) 뒤 2026-10-09T09:50:53Z에 최신 main `89a2c022`에서 `docs/ops-v1-canon-20261009`를 만들고 upstream을 해제했다. 리드 맥락 메모는 E/lead-context.md(09:51:03Z)다.
- 리드 이탈(리드 귀속, 첫 관찰, 메인 `msg_f705412e769c` 지시로 기록):
  - 범위 초안 단계에서 근거 폴더 밖 쓰기 넷이 있었다. 측정 명령 하나가 Git Bash `/tmp/x`(36 bytes)를 만들어 내용 확인 뒤 지웠다. 진입 때 Windows TEMP에 쓴 받은 메시지·터미널 목록·orchestration 가이드 파일 셋은 E/session으로 옮기고 지웠다.
  - 메인에 보낼 status를 `run:run_ee8c71e0897d`(리드 자신의 Run)로 보내 리드 자신이 받았다(`msg_dcc3fd8da3da`). 메인 term handle로 다시 보냈다(`msg_7ed952247304`). 리드→메인 주소는 메인 term handle이다.
  - 교정 층: 둘 다 첫 관찰이라 이 기록으로 둔다. 같은 일이 다시 나면 반복 규칙 대상이다.

### 정본 PR 작성

- 작성: `[Rules Sol]` 요청 모델 `gpt-6.1-sol` max(backend unknown), 배정 신호 해당 없음(명세가 정해진 문서 작성). pane `term_c13433ad-d3ec-4eb5-b9e0-349c331042b4`, Task `task_2871c36d68f6`, Dispatch `ctx_b603f1bde17b`. 계약은 E/pr1-contract.md다. split 09:56:35Z, worker-start 09:56:51Z였다.
- 계약 전달: worker-start가 turnStart unobserved였다. 리드가 draft를 확인하고 Enter 한 번으로 제출했다. Codex 기록에서 계약 전문을 담은 제출 입력 하나를 확인했다(E/pr1-draft-recovery.md).
- 질문 1회: bytes 상쇄 후보(`msg_5f6887c37359`, 10:13:57Z). 리드가 공식 reply `msg_3e464481d320`으로 O2~O5 이관과 새 문장 축소를 답했다. 원문은 E/pr1-ask1-answer.md다. 이관 원문은 「정본에서 옮긴 적용 기록」에 있다.
- 완료: worker_done `msg_b76e13e7165e`(10:33:55Z). 수신 helper 판정 allowed다. 정산(retained/external_terminal) 뒤 대기를 확인하고 pane을 닫았다. 보고는 E/pr1/report.md, 맥락 메모는 E/pr1/context.md다.
- 관찰: heartbeat 간격 343초 한 구간(09:58:45Z `msg_813b8fe6d3e1` → 10:04:28Z `msg_092e582ae3a5`)이 있었다. BACKLOG `worker-liveness-tool-check`와 같은 갈래다. 산출물 영향은 실사 전 미확인이다(E/lead-observations.md).
- 리드 R-2(10:37:18Z): 변경 파일이 허용 9개뿐임을 확인했다. 여섯 파일 묶음은 102,600 bytes(base 102,601)이고 ORCA는 249줄이다. AGENTS 전환 문단이 승인 문안(E/agents-transition-proposal.txt)과 같다. 라우팅 기준의 「애매하면 Astra」와 규칙 맞춤 문장은 개정 v2와 이전 goal의 정본화 후보 원문에 맞다. 새 anchor `#구현자-모델-시범`·`#main-맞춤과-합류점`이 있다. 이 대조는 리드 확인이며 독립 실사가 아니다.

## 정본에서 옮긴 적용 기록

규칙 문서 bytes를 상쇄하려고 옮긴 원문을 둔다. 원래 위치는 base `89a2c0225ea38a846f5b6c163620a9e828a610e3` 기준이다. 상대 링크는 이 goal에서 같은 대상에 닿도록 고쳐 썼다. 링크 밖 원문은 그대로 보존했다.

### AGENTS 전환 문단

원래 위치는 `AGENTS.md:45`다. Core 명칭 전환 부분 축소의 메인 확인은 `msg_f705412e769c`다. 이 메시지는 메인 판단이다.

> - **Core 명칭과 리드 태그 전환:** GameDev는 Core의 이전 이름이다. [운영 후속 정본화의 PR2](../2026-10-05-ci-warning-operating-followup/goal.md#요구사항-원천과-적용-결정) 병합 뒤 새로 여는 세션과 새 계약부터 Core 태그를 쓴다. PR2 병합 전에 연 GameDev 세션과 진행 중인 계약은 종료까지 `[GameDev …]`를 유지한다. 진행 중인 영속화 통합 goal에는 중간 변경을 요구하지 않는다. 그 goal의 R-8로 새 리드를 열 때부터 Core 태그를 쓴다. [리드 태그 PR](../2026-10-08-tdd-canon-temp-write-boundary/goal.md#요구사항-원천과-적용-결정) 병합 뒤 새로 여는 리드 세션과 그 세션의 새 계약부터 `[<파트> 리드 Opus]`를 쓴다. 병합 전에 연 리드 세션은 종료까지 `[<파트> Astra]`를 쓴다. 전환기 수신 측은 각 전환의 두 태그를 같은 파트로 인정하되 현재 `from_handle`·Task·Dispatch 대조를 계속한다. 과거 기록·BACKLOG의 GameDev·Astra는 당시 이름으로 해석한다.

### RESUME 이전 준비 branch

원래 위치는 `00_Document/operations/RESUME.md:14`다. 뺀 절이 들어 있던 문장 전체를 보존한다.

> 현재 branch·기준 commit은 CURRENT의 goal, 이전 준비 branch는 P1a 종료 절에 있다.

이전 준비 branch 절을 뺐다. CURRENT의 goal을 가리키는 앞절은 문장으로 다듬어 유지했다.

### ORCA 지난 ask 적용

원래 위치는 `00_Document/operations/ORCA.md:75`의 끝 문장이다.

> 지난 적용은 [1.4.218 승인](../2026-10-05-operating-canon/goal.md#orca-source-r3-ask)·[1.4.220 복귀](../2026-10-05-operating-canon/goal.md#orca-14220-return)에 보존한다.

### ORCA 추가 출처 기록 이관

이관 근거는 리드 답(질문 `msg_5f6887c37359`에 대한 공식 reply `msg_3e464481d320`)이다. O2·O3·O4·O5를 옮겼다. O1인 ORCA 23행의 출처 이관 대조표 문장과 21행은 그대로 뒀다. 이 대조표에 아래 네 출처 행이 모두 있으므로 정본에서 출처까지 한 단계로 이어진다는 것은 리드 확인이다. 답 원시는 `E/pr1/work/offset-ask-receipt.txt`다. 아래 원문도 상대 링크만 이 goal 위치에 맞췄다.

O2의 원래 위치는 `00_Document/operations/ORCA.md:73`의 마지막 문장이다.

> 근거는 [전달 원문](../2026-10-05-operating-canon/goal.md#orca-source-table-3)과 [로컬 reply help](../../../.backups/verification/2026-10-01-operations-rules/reply-help.txt)다.

O3의 원래 위치는 `00_Document/operations/ORCA.md:162`의 첫 문장이다.

> 이 규칙의 출처는 [확정 실패 승인 기록](../2026-10-05-operating-canon/goal.md#orca-source-confirmed)이다.

O4의 원래 위치는 `00_Document/operations/ORCA.md:153`의 마지막 문장이다.

> 승인 출처는 [capacity 예외 기록](../2026-10-05-operating-canon/goal.md#orca-source-capacity)에 있다.

O5의 원래 위치는 `00_Document/operations/ORCA.md:220`의 첫 문장이다.

> 출처는 [Gardener 사용자 채택 기록](../2026-10-05-operating-canon/goal.md#orca-source-gardener)이다.

## 다음 계획 후보

이 goal 밖으로 둔 일이다. 후보마다 BACKLOG ID나 기존 goal 링크를 단다. 종료 기록 PR 전에 후보 도착 검사로 확인한다.
