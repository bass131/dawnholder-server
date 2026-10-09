# 규칙·운영 V1.0 — 후보 도착 검사와 합류점 절차

## 재개 지점

Rules의 목표이며 운영 셋업 3단계 「계획 경계」와 사용자 지시 「규칙·운영 V1.0 셋업 뒤 main 통합·재분화」를 묶는다. 사용자가 범위를 승인했다(아래 「적용 중인 사용자 결정」). 기준·상태·결과는 이 파일에 모으고 [CURRENT](../../../00_Document/operations/CURRENT.md)는 이 목표를 가리킨다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch: 정본 PR은 `docs/ops-v1-canon-20261009`(base main `89a2c022`)다.
- 근거 폴더 E: `.backups/verification/2026-10-09-plan-boundary-and-junction/`(Git 제외). 리드 맥락 메모는 [lead-context.md](../../../.backups/verification/2026-10-09-plan-boundary-and-junction/lead-context.md), 승인 범위는 [scope-draft-v1.md](../../../.backups/verification/2026-10-09-plan-boundary-and-junction/scope-draft-v1.md)(SHA256 `22cdc5b6…`)와 [scope-revision-v2.md](../../../.backups/verification/2026-10-09-plan-boundary-and-junction/scope-revision-v2.md)(SHA256 `23619ccf…`)다. 받은 메시지 원시는 E/session/에 있다.
- 리드: 신규 `claude-opus-5-5` xhigh(화면 「Opus 5.5 ⚡xhigh」, backend unknown), 태그 `[Rules 리드 Opus]`, handle `term_5f6a014b-4ec5-45f9-9103-b84cc11c18d2`, Run `run_ee8c71e0897d`. 메인 주소는 메인 term handle이다. 이전 Rules goal의 Run·Task·Dispatch·handle은 실행 권한이 아니다.
- **현재 위치**(2026-10-09T12:47:43Z): 정본 PR 문서는 재실사 PASS, 규칙 점검 보고서는 재검토 PASS다(「정본 PR 재실사」·「규칙 점검 보고서」).
- **다음 할 일**: 정본 PR 생성과 승인 묶음 → 사용자 병합 승인 뒤 후보 도착 검사 PR.
- 이 goal의 마지막 PR(종료 기록 PR) 병합 커밋이 합류점 J다.

## 진척 단계

- [x] 범위와 기준 확정
- [x] 정본 PR 작성·실사
- [x] 규칙 점검 보고서
- [>] 정본 PR 병합
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
4. Sol max 작업자 Fast 모드 정본화: AGENTS 모델 라우팅의 Sol 줄과 ORCA R-5 Sol 명령(「적용 중인 사용자 결정」, 메인 `msg_7a08526d6820`). 여섯 파일 묶음 bytes는 같은 PR에서 상쇄한다.
5. BACKLOG `report-response-format` 근거 덧붙임: ASD-STE100 사양 PDF의 경로·hash(메인 `msg_09633f3c479e`)와 보고서 검토의 후속 입력(#9·O-R1·O-R2·첫 검토 O5).

### 건드릴 곳

- 정본 PR: AGENTS(판 한 줄·모델 라우팅·태그 형식·전환 문단), ORCA(R-4·R-5·R-8·capacity·확정 실패·75행), RESUME 14행, goal-loop SKILL(짧은 절), `.agents/skills/dawnholder-goal-loop/references/`(milestones 새 절, 새 기준표 파일), templates(머리말·위임 계약), CURRENT Rules 줄, BACKLOG, 이 goal, `01_Phases/reports/2026-10-09-operating-rules-v1-review/`.
- 후보 도착 검사 PR: `99_Tools/Backlog/`, `99_Tools/Backlog.Tests/`, `.github/workflows/code-rules.yml`(단계 하나), `99_Tools/README.md`, goal-loop SKILL(한 문장), BACKLOG(승격 한 칸), 이 goal.
- 종료 기록 PR: 이 goal, BACKLOG(새 후보 행), CURRENT의 Rules 줄, AGENTS 모델 라우팅의 Sol 줄, ORCA R-5 Sol 명령.

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
- **Sol max 작업자 Fast 모드**(메인 전달 `msg_7a08526d6820`, 2026-10-09T11:54:27Z, 20:5x KST): 「6.1 Sol Max 한정으로 Fast 모드로 작업하는거로 가닥잡자」.
  - 대상은 `gpt-6.1-sol` max 작업자뿐이다. Astra 구현자·검증자·리드·Fable은 그대로다.
  - 기동 후보는 R-5 Sol 명령에 `-c service_tier="priority"`를 더한 것이다. 값 철자는 메인 추정이며 첫 기동에서 확정한다.
  - 첫 Fast 기동에서 요청 명령·화면 Fast 표시·rollout `service_tier` 값을 이 goal에 적고 그 Sol rollout의 token_count를 남긴다. Fast가 확인되지 않으면 기록하고 메인에 status를 보낸다. 작업은 기본 tier로 계속할 수 있다. 실행 중인 세션의 tier는 바꾸지 않는다.
  - 정본(AGENTS 모델 라우팅의 Sol 줄, ORCA R-5 Sol 명령)은 종료 기록 PR에서 반영하고 그 실사 범위에 넣는다. 정본 PR과 보고서는 고치지 않는다. 리드는 보고서 검토자의 쓰기 종료(`msg_102c3c3810ed`) 뒤에 이 기록을 썼다.
- **ASD-STE100 Issue 9 사양 PDF**(메인 전달 `msg_09633f3c479e`, 2026-10-09T12:31:08Z, 21:3x KST): 「C:\Users\bass1\Downloads\ASD-STE100_ISSUE9.pdf 이거 문서 찾았어, 너가 보기 좋은 위치에 문서 이동시켜도 돼」.
  - 메인이 `C:/Dev/DawnHolder_Project/.backups/research/asd-ste100/ASD-STE100_ISSUE9.pdf`(Git 제외)로 옮겼다. SHA256 `d1f4ea9e…`, 434쪽, Issue 9 January 2025다.
  - ASD 저작권 문서다. 저장소에 넣지 않고 보고서·정본에 규칙 원문을 옮기지 않는다. 규칙 번호와 한국어 요약만 쓰고, 꼭 필요하면 15단어 미만 인용 하나까지 둔다. 쪽을 보며 읽고 본문 텍스트를 파일로 뽑지 않는다.
  - 메인 대조: 쓰기 규칙 9절 53개와 GR-1~GR-8이 있다. 보고서의 「Issue 9, 2025년 1월, 9개 절, 53개」는 사양과 맞다.
  - 보고서 재검토가 PASS라 보고서는 다시 열지 않는다. 종료 기록 PR에서 PDF 경로와 hash를 BACKLOG `report-response-format` 근거에 더해 V1.x STE 검사 작업의 입력으로 둔다.
- **이전 결정의 대체**: 이번 원문의 구현자 난이도 라우팅은 이전 사용자 결정 「Astra 구현 격상은 사용하지 않는다」(메인 `msg_c78692a42979`, management-active 시스템 카드 goal 882행, 확정 실패 네 번째 시도 맥락)를 바꾼다. 시범 운영은 메인이 원문을 좁히지 않는 운영 방식으로 수용했다(`msg_bd7e0409e4c5`).

## 이번 합류점 순서표

실측: 2026-10-09T09:19:27Z, origin/main `89a2c022`, `git rev-list --left-right --count origin/main...HEAD`(뒤/앞). 원시는 E/session/branch-states.txt다. PR 상태(DIRTY·CLEAN·보류·V3 차단)는 진입 지시의 메인 확인(18:1x KST)을 옮겼다. 종료 기록 PR에서 최종판으로 다시 잰다.

| 순서 | 파트 | 지금 상태 | J 뒤 처리 | 주체·시점 |
|---|---|---|---|---|
| 0 | Rules | 이 goal | 종료 기록 PR 병합이 J. 다음 Rules goal은 J 이후 main에서 새 branch | 메인이 J 기록, 다음 Rules 리드 |
| 1 | Core | `feat/persistence-engine-judgment-20261006` 10/43, 앞선 diff는 영속화 goal·BACKLOG 두 파일(BACKLOG는 Core 담당 새 행 하나), push됨, PR 없음, 리드 살아 있음 | 기록만 앞섬: 다음 안전 지점에 J를 받는다. 영속화 goal을 잇는다. 인스턴스 맵 goal은 J 이후 main에서 새 branch(Core goal의 기존 결정) | Core 리드, 메인의 재개 신호 뒤 첫 안전 지점 |
| 2 | Content | `feat/items-inventory-ui-20261005` 222/11, PR191 DIRTY, 실화면 검증 보류, 리드 닫힘 | 검증이 남은 열린 PR: 재진입 첫 일로 J를 통합(6.6 위, ProjectSettings 예외는 이 통합 한 번) → 실화면 독립 검증 → 새 head 승인 | 새 Content 리드, 메인의 게임 재개 신호 뒤 |
| 3 | Management | `feat/intro-site-20261008` 24/37, PR207 CLEAN, 사용자 지시로 보류, 독립 검증 V3 차단 2건 | 보류된 PR: J 때 처리 없음. 재개 결정 뒤 J를 받고 차단 2건 처리 → 승인 묶음 | 사용자 재개 결정 뒤 Management 리드 |
| 4 | CodeMap | `docs/architecture-tests-ci-closeout-20261005` 273/0, 끝남, upstream 없음 | 받을 것 없음. 다음 goal을 J 이후 main에서 새 branch | CodeMap 리드, 재진입 첫 일 |

- Core와 Content는 같은 시기에 열 수 있다. 두 PR이 같은 시기에 열리면 뒤에 병합하는 쪽이 최신 main을 받는다.
- 정정(2026-10-09T11:05:12Z, 리드): Core 행의 앞선 diff를 원시(E/session/branch-states.txt 14~15행)대로 「영속화 goal·BACKLOG」로 고쳤다. 이전 표기 「영속화 goal·CURRENT」는 범위 초안 v1부터 리드가 잘못 옮긴 것이다(정본 PR 문서 실사 #1). BACKLOG 변경은 Core 담당 새 행 `sql-environment-human-access` 하나다(E/lead-check/core-backlog-diff.txt, 11:03:27Z). 메인 대조로는 core-active 로컬에 push 안 된 goal만 바꾼 commit `e0658900`이 더 있으나 판정에 영향이 없다(`msg_16c35ae8003d`).
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
- 커밋: 리드가 작성 결과와 이 기록을 `c78ff977`로 커밋했다(push 전).

### 정본 PR 문서 실사

- 실사: `[Rules 검증자]` 요청 모델 `gpt-6-astra` xhigh(화면 「GPT-6-Astra xhigh」, backend unknown), pane `term_e61aa523-daf9-4279-8215-ae940a094850`, Task `task_c68b0458da26`, Dispatch `ctx_2c7268432b49`. 계약 E/pr1-review-contract.md(SHA256 `cbaa6f9a…`), 고정 HEAD `c78ff977`. 계약 전달은 draft 복구 Enter 1회이고 Codex 기록에서 계약 전문 입력 하나를 확인했다(E/pr1-review-draft-recovery.md).
- 실사 중 status `msg_49ae64900b02`(10:52:29Z)로 #1을 먼저 알렸다. 리드가 원시를 확인하고 메인에 전달했다(`msg_667bf29411d6`).
- 판정: worker_done `msg_5f67bdb7061a`(11:02:24Z), 수신 helper allowed. **NOT PASS, 차단 2.** 판정 원문은 E/pr1-review/verdict.md다. S1~S5·C1~C9를 수행했고 bytes·링크·이관 원문 일곱·전환 문안은 대조됐다. 정산(retained/external_terminal) 뒤 대기를 확인하고 pane을 닫았다.
  - #1 Core 합류점 입력의 원천 불일치와 분류 공백. 귀속은 리드 기록·리드 계약이다. 실제 Core 입력(goal·BACKLOG 새 행)이 milestones 네 상태 어디에도 들지 않는다.
  - #2 작성 Sol heartbeat 343초(상한 300초). 귀속은 작성자 절차다. 산출물 손상은 찾지 못했다.
  - 설계 관찰 O1(보류 판정 우선순위), O2(「Opus」 검증자 호칭), O3(「Sol」 역할·모델 읽힘)은 비차단이다.
- 메인 판단 `msg_16c35ae8003d`(11:04:36Z, 리드 질문 `msg_fa359cb2baed`):
  - 1 수용: 「기록만 앞섬」 판정을 「앞선 diff가 자기 goal·CURRENT 자기 줄·담당이 자기 파트인 BACKLOG 새 행뿐이다. goal은 진행 중이다.」로 고친다. 다른 BACKLOG 변경 일반은 넣지 않는다. O1을 같은 표에 한 마디로 더한다. 순서는 goal 정정 → 새 Sol max로 milestones 수정 → 신규 `gpt-6-astra` xhigh 재실사다.
  - 2 비차단 운영 기록: heartbeat 343초는 이번 판정을 막지 않는다. 메인이 선례 `msg_a8e661983ce5`와 같은 처분을 이번에 다시 정했다. 신호 5·확정 실패 집계에서 뺀다. BACKLOG `worker-liveness-tool-check` 출처에 사례로 더했다.
  - 리드 조사에서 본 goal-loop 52·53행 우선순위와 신호 5의 「Sol」 읽힘은 보고서 후보로만 둔다.
- 같은 산출물 수정 횟수: 정본 PR 문서 1회차(milestones 정의 수정).

### 실사 결함 수정 1회차

- 리드 기록 커밋 `aabdf54c`(goal 정정·실사 기록·BACKLOG 사례) 뒤 새 `[Rules Sol]`이 milestones 두 칸을 고쳤다. 요청 모델 `gpt-6.1-sol` max(화면 「GPT-6.1-Sol max」, backend unknown), 배정 신호 해당 없음(문안이 고정된 한 파일 두 칸). pane `term_c229bdbc-2706-48f6-8296-17ca94f75414`, Task `task_a93e15a3567a`, Dispatch `ctx_6ab39f266014`. 계약 E/fix1-contract.md(SHA256 `a916256f…`). worker-start 11:07:31Z에 turnStart observed라 draft 복구는 없었다.
- 완료: worker_done `msg_7fee9ab255e5`(11:17:14Z), 수신 helper allowed. 보고 E/fix1/report.md, 맥락 메모 E/fix1/context.md(생성 11:10:45Z, milestones 수정 11:13:10Z보다 앞섬). 정산(retained/external_terminal) 뒤 대기를 확인하고 pane을 닫았다.
- 리드 R-2: diff는 milestones 한 파일의 38·40행이며 판정 칸만 메인 수용 문안과 글자 그대로 같다. 여섯 파일 묶음 102,600 bytes, ORCA 249줄로 변화가 없다.
- 관찰: 수정 Sol의 heartbeat 간격 302초 한 구간(11:08:50Z `msg_6fe71be9e2d7` → 11:13:52Z `msg_57440506ffb1`)이 있었다. 상한 300초를 2초 넘었다(E/lead-observations.md). 리드 질문 `msg_163d1240d1de`에 메인이 `msg_f4a418ddd476`(11:18:39Z)로 비차단 기록을 정했다. 신호 5·확정 실패 집계에서 빼고 BACKLOG `worker-liveness-tool-check` 출처에 사례로 더했다.
- 반복 관찰(메인 `msg_f4a418ddd476`): heartbeat 상한 초과의 비차단 처분이 이 goal에서 두 번(343초, 302초), 선례 `msg_a8e661983ce5`까지 세 번째다. 교정 정본상 반복 규칙 후보이지만 정본 PR에는 새 규칙을 넣지 않는다. 보고서 V1.x 후보 「상한 초과 허용 폭 또는 원인 도구 점검」으로 둔다.
- 커밋: 수정과 이 기록을 `5a282fb2`로 커밋했고, 처분 기록을 다음 커밋에 더했다(push 전).

### 정본 PR 재실사

- 재실사: 새 `[Rules 검증자]` 요청 모델 `gpt-6-astra` xhigh(화면 「GPT-6-Astra xhigh」, backend unknown). pane `term_bb090ada-c850-4e08-bb04-e38806389f97`, Task `task_47fbeb983c82`, Dispatch `ctx_4379c58ed8d1`. 계약 E/pr1-rereview-contract.md(SHA256 `cb2eacfe…`)는 메인의 heartbeat 처분 두 건을 원문으로 담았다. 고정 HEAD는 `36b76d9b`다. 계약 전달은 draft 복구 Enter 1회이고 Codex 기록에서 계약 전문 입력 하나를 확인했다(E/pr1-rereview-draft-recovery.md).
- 판정: worker_done `msg_573b5cf0366d`(11:32:34Z), 수신 helper allowed. **PASS.** 판정 원문은 E/pr1-rereview/verdict.md다. #1은 해소됐고 #2(343초)와 302초는 메인 처분대로 비차단이다. 정산 뒤 pane을 닫았다.
- 재실사자 heartbeat 간격은 75~283초로 상한 안이다(E/session/inbox-rereview-hb.json).
- 관찰 O-R1(비차단): 이 goal 재개 지점이 실사 전 상태였다. 이번 기록에서 고쳤다.

### 규칙 점검 보고서

- 위치: [보고서](../../reports/2026-10-09-operating-rules-v1-review/report.html). 리드가 작성했다. 생성 스크립트 E/report/build.mjs가 템플릿 E/report/template.html에 base `89a2c022`→head `36b76d9b`의 Git 값을 채운다. 채우는 값은 여섯 파일 묶음 bytes, ORCA 줄 수, 파일 크기, 바뀐 파일 수, diff 11개, 문장 길이 측정이다.
- 사용자 결정 요청: STE 문장 규칙과 HTML 보고 방식을 V1.x에서 정본화(추천)할지 V1.0에 넣을지다. 메인에게 「STE A」·「STE B」로 받는다.
- 정보 공백 처리: Karpathy 원문은 메인이 사용자 Chrome으로 확인했다(`msg_77b54a58aa27`). STE 사양 PDF는 받지 못해 모든 적용 판단에 「사양 전문 미대조」를 붙였다. answer-me-with-html은 설치하지 않았고 README 수치는 재현하지 않았다고 적었다.
- STE식 측정(생성 스크립트): 절차 문장 상한 20어절 초과 0, 설명 문장 상한 25어절 초과 0, 여섯 문장 넘는 문단 0, 괄호 두 쌍 이상 문장 0이다.
- 리드 렌더 점검(E/report/preview/render-check.md): 1440·390 폭 모두 페이지 가로 넘침 0, 끊긴 앵커 0, 상대 링크 29개 모두 존재다. 점검 중 수기 값이던 「바뀐 문서 9」를 Git 값 10(신규 2)으로 바꿨다. diff 줄 사이 빈 줄과 좁은 폭의 결정 표 잘림도 고쳤다. 인쇄에 어두운 테마 글자색이 남던 것도 고쳤다. 라이트 테마와 인쇄 PDF를 확인했다. 이 점검은 리드 확인이며 독립 검토가 아니다.
- 독립 검토: 새 `[Rules 검증자]` 요청 모델 `claude-opus-5-5`(화면 「Opus 5.5 with xhigh effort」, backend unknown). 기동은 R-5 Claude 명령에 TEMP·TMP를 `.backups/tmp/rr1/`로 정했다. pane `term_dc850591-7668-465a-a47b-c4cd3b14984e`, Task `task_565edc4076da`, Dispatch `ctx_f2d7a917eb3b`. 계약 E/report-review-contract.md(SHA256 `b43933d3…`), 고정 HEAD `e4212086`. worker-start 11:47:35Z에 turnStart observed라 draft 복구는 없었다. heartbeat 간격은 300초 안이다.
- 판정: worker_done `msg_102c3c3810ed`(12:10:49Z), 수신 helper allowed. **NOT PASS, 차단 7.** 판정 원문은 E/report-review/verdict.md다. 생성 재현은 바이트 일치였다. diff 11개, 상대 링크 29개, 앵커 9개도 모두 맞았다. 정산(retained/external_terminal) 뒤 pane을 닫았다. 결함은 모두 리드 몫이다.
  - #1 3절 카드 3의 diff가 사용자 차례 칸이 아니라 구현자 배정 근거 칸을 보인다.
  - #2 7절 「7개 파일 12곳」은 원천(조사 요약 한 줄)에 명령·정의·commit이 없고 재현되지 않는다(측정값 위장 사유). 「상대 링크 475개」의 원 목록 위치 표기도 틀렸다. 「6벌·1,718 bytes」·「goal 넷」은 재현되지만 정의·commit이 없다.
  - #3 STE 측정의 제외 범위 일부가 본문에 없다. #4 근거 표에 재실사 판정과 수정 보고가 없다. #5 작성 모델 표기가 없다.
  - #6 `msg_59451b2e9096`이 요구한 README 확인 commit·날짜와 직접 HTML 작성과의 비교(또는 미측정)가 없다. #7 배경 그래픽을 끈 기본 인쇄에서 diff 색이 사라진다.
  - #8(비차단)은 9절 10-31 표 셋과 6절 다섯 건의 불일치다. 설계 관찰 O1은 결정 질문 형식(두 검토 질문을 A의 결과로 넣은 것)이며 메인 확인 대상이다.
- 리드 R-2(E/lead-check/report-review-r2.md): #1 diff 표식, #2 수치 원천, #7 기본 인쇄 PDF를 직접 봤고 판정과 같다.
- 관찰(리드 계약 귀속, 첫 관찰): 검토자가 자기 감사에서 읽기 금지 위반 1건을 보고했다. Claude Code 하네스가 큰 출력을 `~/.claude` 아래 tool-results 파일로 자동 저장했고 검토자가 그 파일을 읽었다. 계약의 「`~/.claude` 아래 세션 기록 읽기 금지」가 하네스 동작과 겹쳤다. 다른 세션 기록은 열지 않았다. 다음 Claude 계약에는 자기 tool-results 파일 읽기를 허용 범위로 적는다.
- 같은 산출물 수정 횟수: 보고서 1회차(리드 수정).
- 메인 판단 `msg_9123cdb49c02`(12:14:45Z): O1은 「STE A/B」 한 질문을 유지하고 질문 아래에 「(가)와 (나)를 다르게 정하려면 따로 말해 달라」 한 줄을 더한다. 차단 7은 리드 수정 → 새 Opus 재검토로 한다. #2 수치는 재현할 원시와 기준 commit이 없으면 지운다. 재검토 계약은 자기 세션의 하네스 자동 저장 출력 읽기를 허용한다.
- 리드 수정 1회차(E/report/preview/fix1/render-check-fix1.md):
  - #1 카드 3은 사용자 차례 칸 hunk를 보이고, 구현자 배정 근거 hunk는 카드 5로 옮겼다. diff는 12개다.
  - #2 7절 개수는 생성 스크립트가 head에서 `git grep -F`로 세고 원시를 data.json `greps`에 남긴다. 「7개 파일 12곳」은 재측정 값으로 바꿨다. 「상대 링크 475개」는 지우고 「확인하지 않은 것」에 전수 점검 미실행을 적었다.
  - #3 측정 제외 표시를 모두 없앴다. 뺀 구조의 개수는 생성 스크립트가 측정표 아래에 적는다.
  - #4·#5 근거 표에 재실사 판정·수정 보고·보고서 작성 모델을 더했다. #6 README 확인 날짜와 commit 미기록, 직접 HTML 작성과의 비교(토큰·시간 미측정)를 적었다. #7 diff에 왼쪽 테두리를 주고 인쇄 색 유지를 지정했다. 기본 인쇄 PDF로 확인했다.
  - #8과 관찰 O2~O4도 고쳤다. O1은 메인 판단대로 한 줄을 더했다. O5는 보고서 템플릿 후속 후보로 둔다.
- 독립 재검토: 새 `[Rules 검증자]` 요청 모델 `claude-opus-5-5`(화면 「Opus 5.5 with xhigh effort」, backend unknown). TEMP·TMP는 `.backups/tmp/rr2/`다. pane `term_7ba87abd-f4aa-4b51-bd18-4e9151a4ab92`, Task `task_8ea4b9e7dd7e`, Dispatch `ctx_b9bed7a0d29f`. 계약 E/report-rereview-contract.md(SHA256 `5380215d…`)는 자기 세션의 하네스 자동 저장 출력 읽기를 허용했다. 고정 HEAD는 `22e97f73`이다.
  - 첫 worker-start는 계약 32,517 bytes가 명령줄 한도에 걸려 bash 「Argument list too long」으로 실패했다. 세션에 전달된 것은 없다. CODE_CONVENTION 「파일 위치와 이름」 원문을 제외 이유와 함께 빼고 30,604 bytes로 다시 보냈다. turnStart observed였다(E/report-rereview-start-note.md).
- 재검토 판정: worker_done `msg_09bcc15f3ee4`(12:45:50Z), 수신 helper allowed. **PASS.** 판정 원문은 E/report-rereview/verdict.md다. 첫 검토 #1~#8은 모두 해소됐다. 생성 재현은 바이트 일치이고 diff 12개, 상대 링크 31개, md 앵커 11개가 맞았다. 기본 인쇄와 STE 재계산도 맞았다. 허용 밖 쓰기는 0이다. 정산(retained/external_terminal) 뒤 pane을 닫았다.
  - 비차단 #9: 7절 「병합 승인」 13줄은 용어 언급 수다. 승인 줄 자체는 3줄이라 「복제」 제목 아래에서 복제 수처럼 읽힌다.
  - 관찰 O-R1(2절 「네 묶음」과 표 머리 「층」·「여섯 파일 묶음」의 이름 겹침), O-R2(측정 상자 「그대로」 문구), O-R3(나눠 답할 때의 답 형식, 메인 몫).
- 리드 R-2(E/lead-check/rr-rebuild/): 생성 스크립트로 다시 만든 보고서가 HEAD blob과 바이트 일치했다. #9의 승인 줄 위치(AGENTS:60, CLAUDE:47, ORCA:227)와 `.backups/tmp/rr2/` 쓰기(하네스·node 캐시뿐)를 확인했다.
- 처리: 보고서는 재검토 PASS 상태로 닫고 더 고치지 않는다. 고치면 마지막 검토 근거가 바뀌기 때문이다. #9·O-R1·O-R2와 첫 검토 O5는 STE·보고 방식 V1.x 작업의 입력으로 두고, 종료 기록 PR에서 BACKLOG `report-response-format` 근거에 더한다. O-R3은 승인 묶음에서 메인에 넘긴다.

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
