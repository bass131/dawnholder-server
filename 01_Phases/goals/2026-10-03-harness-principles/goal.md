# 하네스 원칙 채택과 문서 정비

2026-10-03, 작성 `[Rules Astra]`. **진행 중 — 정식 목표 승인, BACKLOG 첫 작업 준비.** 메인 `msg_c1412c982ac5`가 초안·이 경로·`docs/harness-backlog` 착수를 승인했다. All 비교/승격/helper는 외부 선행 조건을 가진 백로그 후보로 분리하고 ADR-035는 GameDev 첫 SQL 구조 PR 병합 뒤 작성한다. 아래 계획을 구현·검증·병합 실적으로 취급하지 않는다.

## 문제와 목표

반복 규칙이 문서에만 있고 경로 목록·검사 결과·종료 기록이 실제 상태와 어긋나면 다음 작업의 유지보수성과 검증 신뢰도가 낮아진다. 먼저 goal 전 후보의 정본 BACKLOG를 만들어 Management가 소비할 계약을 제공하고, 승인된 하네스 원칙을 기존 규칙·도구·문서 탐색에 연결한다. DB 저장소·연동 완성의 선행 경로를 막지 않으며 속도보다 독립 검증·실제 근거·사람 가독성을 우선한다.

목표 기준·상태·결과는 이 `goal.md` 한 곳에 둔다. CURRENT는 링크만 유지한다. BACKLOG는 goal로 승격되기 전 후보만 담는다. 분할 PR은 이 목표의 작업 단위이며, 목표 분할이나 추가 Astra 수명은 메인이 결정한다.

## 결정 출처와 적용 경계

모든 사용자 결정은 **메인이 전달한 것**이며 사용자 직접 입력으로 격상하지 않는다. 아래 메시지는 요구사항 근거이며 과거 세션의 실행 권한을 재사용하는 근거가 아니다. 최신 착수 결정 `msg_c1412c982ac5`(2026-10-03T10:39:09Z)는 세 보정과 개선 입력 네 건의 추천 방향을 승인했다. 원문은 `.backups/verification/2026-10-03-harness-principles/main-goal-approval.json`이다.

| 출처 | 이 초안에 반영한 결정 |
|---|---|
| `msg_c9bc79f8ec43` (메인, 2026-10-03T10:26:02Z) | 새 Rules 세션의 현재 계약, 초안 후 검토, BACKLOG 씨앗 보강, 종료 기록 이관, 개선 입력 네 건 |
| `msg_61d4c35695da` (메인, 2026-10-03, 이전 근거 `main-decisions.json`) | 참고 자료·ADR·운영/기술 문서·legacy 정비·경로 드리프트·고정 보관 tag의 앞선 승인 범위 |
| `msg_8639aeaf7a12` (메인, 2026-10-03T08:32:32Z) | DB 우선, PS 정리의 파트별 소유, BACKLOG 첫 별도 PR, heartbeat 예외, 인용 검사 helper 후보 |
| `msg_d5453356cc75` (메인, 2026-10-03T10:17:49Z) | npm warning 후보는 백로그, PowerShell `[ordered]`는 이번 범위, 집계·비교 helper는 인용 검사 후보에 통합 |
| `msg_22a9b4109ee7` (메인 최종 결정, 새 계약과 이전 인계가 인용) | 확정 실패 3회 뒤 네 번째는 새 Sol + 새 Fable Advisor, 재승인 대기 없음, Astra 구현 격상 제외 |
| `msg_ae8e98ad1d0d` (메인, 이전 근거 `main-draft-input-guidance.json`) | 미제출 draft·추천 프롬프트는 사용자 지시가 아니고 그 자체로 pane 종료를 막지 않음 |
| `msg_acba01cbf81e` (메인→Management, 새 계약이 전달) | DB 연동 뒤 운영툴 서버 운영 시각화 후보 |

원문 위치: `.backups/verification/2026-10-02-agent-rule-context/resume-2026-10-03/`. 초기 계약은 `.backups/planning/harness-principles/main-request.json`에 보존한다. 원문을 직접 재조회하지 않은 메시지는 위와 같이 전달 출처를 적고 직접 수신으로 표현하지 않는다.

채택 원칙은 다음 다섯 가지다.

1. 반복 규칙은 검사로 바꾸고 진단에 고치는 방법을 포함한다.
2. 정본 helper·생성기·구조를 사용하는 길을 가장 쉽게 만든다.
3. 수기 경로 목록에는 실제 존재를 확인하는 드리프트 검사를 붙인다.
4. 새 검사는 warning 파일럿 → 실측 → error 승격 순서로 도입하고, 유지비가 가치보다 크면 강등한다. 실행 불가와 규칙 위반은 다른 상태다.
5. 구조 변경과 동작 변경은 다른 커밋으로 나눈다.

진행 중 단계는 마친 뒤 다음 계약부터 적용한다. 현행 주석 정책은 유지한다. 검증 강도 차등·작은 작업 예외, 규칙 문서 가지치기, 사람용 코드 따라읽기 문서는 미결정으로 남기며 이번에 정책으로 도입하지 않는다. 아래 명시된 legacy 정비는 기존 개별 승인 범위이고, 일반적인 규칙 문서 가지치기 승인을 뜻하지 않는다.

## 진입 상태와 보존

- cwd `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`, 진입 branch `fix/code-rules-import-candidates`; 착수 branch `docs/harness-backlog`.
- HEAD·origin/main·원격 refs/heads/main 조회 결과 `5616573c32a2b2e0b677bc21b75e22a08d21f285`; 좌우 차이 0/0. 실제 브랜치 생성 직전에 main을 다시 확인한다.
- tracked 변경은 이전 `01_Phases/goals/2026-10-02-agent-rule-context/goal.md` 하나(62+/3−). hash `99CD14239F0ADD91DD8B334A8B4CCC4C171CC7CA98A4BBD6C3EA9583831FDA08`가 r8-final-state·스냅샷과 일치하고, 현재 diff는 r8-final-goal.patch와 개행 정규화 후 일치했다.
- 메인 지시에 따라 이 종료 기록은 최신 main의 새 브랜치에 보존 이관했다. **첫 별도 문서 커밋 `110dd8f`**으로 BACKLOG PR에 포함한다. 기존 내용의 임의 정정·폐기·옛 브랜치 추가 push는 하지 않는다. 커밋 직전 사전 맥락과 diff/hash를 다시 확인한다.
- 기존 Gardener 보류 원문 `A1207CFD…CB56`, 보정 판정 `48D6B0DB…C7A2C` hash를 이번에 대조했다. 보정 PASS는 이전 독립 실사 결과이며 이번 목표의 독립 판정이 아니다.
- 과거 All의 PS289/9파일·SQL2 보류는 기준선이다. 각 파트의 정리 회신을 최신 main All 재측정으로 바꾸지 않는다. DB·Unity·게임·새 CI는 이번 초안 단계에서 실행하지 않았다.
- 새 Run 바인딩 뒤 메인에게 `run:<id>`를 회신 주소로 알려 준다. 과거 Run/Task/Dispatch는 재사용하지 않는다.

## 작업 순서와 관찰 가능한 완료조건

### 첫 별도 PR — BACKLOG 정본과 이전 종료 기록

승인된 첫 브랜치는 `docs/harness-backlog`다. 첫 문서 커밋은 위 종료 기록 이관, 그 다음 커밋에 새 goal·필요한 CURRENT 링크와 BACKLOG를 둔다. 정식 goal 작성은 Astra, BACKLOG의 계약에 따른 문서 구현은 새 Sol, 독립 정적 실사는 새 Opus가 맡는다.

`00_Document/operations/BACKLOG.md`에 ID·제목·이유·출처(누가/언제/메시지 ID)·선행 조건·담당 후보·상태를 둔다. 상태는 `대기`, `goal 승격 → 링크`, `폐기 + 이유`로 제한한다. ID는 의미 기반으로 정하고 마일스톤 코드를 붙이지 않는다. 출처가 메인 전달뿐이면 그 메시지 ID와 전달 사실을 적는다. 필드·ID·상태 계약은 Management에 전달하되 운영툴 구현은 Management 소유다.

| 첫 후보 | 이유와 출처 | 선행 조건·담당 후보 |
|---|---|---|
| 도식 디자인 개선 | 설명 그림의 개선 요청; 새 계약·첫 재계획 전달 | 구체 화면/불편 확인, Rules/Management 협의 |
| ASD-STE100·HTML 답변 방식 | 보고 방식 탐색 요청; 새 계약·첫 재계획 전달 | 프로젝트 적합성·도입 여부 사용자 판단, Rules |
| 작업 루프 자기 개선·그래프 설계 | 반복 작업의 개선 구조 검토; 새 계약·첫 재계획 전달 | 현 루프 실적 대조, Rules |
| 파트 PS 정리 후 All 비교·승격 제안과 근거 인용/집계 helper | 세 파트 정리 결과를 원시와 대조하고 정책을 판단; 인용·최소 줄·진단 identity·소유 맵 검증 포함; `msg_8639aeaf7a12`, `msg_d5453356cc75`, `msg_c1412c982ac5` | **촉발 조건: 세 파트(GameDev·Architecture·Management) PS 정리 병합 완료.** 이후 별도 goal로 승격, Rules; 이번 목표 종료를 막지 않음 |
| workflow 정의 lint | workflow 정의 오류 조기 검출; 첫 재계획·이전 결함 #4 | actionlint류 도구 도입 사용자 승인, Rules |
| 검증 강도 차등·작은 작업 예외 | 미결 사용자 판단; 새 계약 | 현 독립 검증 유지, 메인→사용자 |
| 규칙 문서 가지치기 | 미결 사용자 판단; 새 계약 | 기존 명시 정비와 구분, 메인→사용자 |
| 사람용 코드 따라읽기 문서 | 미결 사용자 판단; 새 계약 | 목적·유지비 결정, 메인→사용자 |
| 정적 분석기 단계 | 유지보수성 후속 강화; 첫 재계획 | DB 연동 뒤, 담당 소유 조율 |
| CI npm engines 경고 노출 | 실제 EBADENGINE이 요약에서 숨겨짐; `msg_d5453356cc75` | warning fixture 포함/미포함, npm 버전 증거, Rules+Management engines 조율; 해소/승격은 실측 뒤 사용자 결정 |
| 운영툴 서버 운영 시각화 | 사용자 2026-10-03 요청, 새 계약과 `msg_acba01cbf81e` 전달 | DB 연동 뒤, Management; kciter 글은 구현 조사 때 확인할 참고 출처 |
| 운영툴 작업 현황 화면 | 작업 중·예정·결정 대기 상시 표시 요청; 새 계약·`msg_c1412c982ac5` | Management. `C:/Dev/DawnHolder_Dashboard`는 메인이 작성한 Orca·gh 읽기 전용 + 메인 계획판의 개인 CLI 참고이며 저장소 산출물이 아님 |

예정된 CodeGraph 정돈·모듈 경계 검사·Management 백로그 메뉴는 중복 후보로 등록하지 않는다. 독립 검증은 필드 누락·ID 중복·출처와 상태 일치·goal 승격 링크·예정 목표 중복·탐색 링크를 실사한다. 이 PR의 완료는 실제 판정과 CI 결과, **해당 PR 사용자 병합 승인·병합**까지다. BACKLOG 첫 PR을 다른 제품 변경으로 지연시키지 않는다.

### 운영 규칙·참고 근거 정비

승인된 다음 내용은 하나의 규칙 정본으로 연결하고 여러 파일에 절차를 반복 복제하지 않는다.

| 작업 | 작성 소유·주요 파일 | 검증 |
|---|---|---|
| 원칙의 설명 자료·반대 의견·Gardener 근거 | Astra: `00_Document/conventions/refs/agent-engineering/_index.md`와 연결 자료 | 원문을 직접 재확인하고 인용과 프로젝트 채택 구분, 신규 Opus 내용·출처 실사 |
| ADR-034 하네스 원칙 채택 | Astra: `00_Document/ADR/`의 현행 명명 관례에 맞춤 | 원칙5개·적용시점·기각 대안·남은 결정과 원문 대조 |
| 원칙1·3·4·5와 30초 가독성 시험 | Sol: CODE_CONVENTION | 새 독자가 X의 출처와 변경 주체를 찾는 사람 검토. 린트 성공으로 대체하지 않음 |
| 결함을 검사로 바꿀 수 있는지와 Gardener 4주 파일럿 | Sol: goal-loop·판정 양식·ORCA R-8의 정본 연결 | 종료 시점·읽기전용·보고서1개·최대2후보·사용자 채택·2026-10-31 평가 경계 |
| 미제출 draft와 공식 계약 draft 구분 | Sol: ORCA R-6, session-handoff | 사용자 제출 여부·추천 문구·계약 주입 상태를 분리하는 현실적인 절차 실사 |
| 3회 확정 실패 뒤 새 Sol+Fable Advisor | Sol: AGENTS·orca-work·ORCA R-5/R-7 | 같은 계약/번호 집계, 사전 보고/재승인 불필요, 조언 선행·채택/기각 기록, 네 번째 실패 뒤 메인 판단. 기존 goal 검토 시범과 구분 |
| 내용 없는 heartbeat 태그 예외 | Sol: AGENTS·ORCA와 수신 판정 helper | 빈 생존 신호만 예외, from_handle/taskId/dispatchId 일치. 내용 있는 메시지는 태그 필수. 신규 Opus가 정상/누락/불일치 fixture 작성·실행 |
| 이전 N1 추가 차단사유 두 개 동기화 | Sol: AGENTS·CODE_CONVENTION·task-context/양식의 필요한 정본 연결 | 상수/수기 값을 측정값처럼 기록, 구현을 복제해 항상 통과하는 테스트. `verification/verdict.md` N1이 원천이고 Gardener N1과 별개 |
| 이전 N2 DEVELOPMENT 시점 표현 | Sol: DEVELOPMENT 검사 실행 설명 | PR166/167의 과거 실행 근거로 갱신, 새 목표 실행 실적으로 표현 금지 |

CLAUDE.md는 메인 소유다. 그 문서의 이미 존재하는 두 차단 사유를 나머지 계약·양식에 연결하며, 메인 쓰기가 추가로 필요하면 별도 소유권 조율한다. 스킬 수정 작업 발행 시 해당 제작 지침도 확인한다. 외부 원문은 다음 작성 단계에서 확인하고, 이번 초안에서는 전달된 채택 결정을 외부 저자의 발언으로 인용하지 않는다.

ADR-034에는 이미 전달된 기각 대안과 이유도 보존한다: 검사를 별도 목표로 미루면 그동안 새 코드가 검사 없이 쌓이는 점, 뷰어보다 경계 검사가 유지보수성 우려에 직접 대응하고 같은 스냅샷을 재사용하는 점, 주간 Gardener보다 목표 종료가 세션 운영의 자연 지점이라는 점, 메인 직접 작성 대신 역할·검증 분리를 유지한 점, legacy 전체 archive 이동보다 지정 통합을 선택해 manifest/link-map 변경 비용을 줄인 점이다. 새로운 대안 검토 결과처럼 쓰지 않는다.

### 문서의 사실·탐색·legacy 정비와 경로 검사

앞선 승인 `msg_61d4c35695da`의 구체 목록을 유지하되, 각 파일의 실제 상태와 소유자를 작업 직전에 확인한다.

- FEATURE_MAP의 `Maps/States/Actions/`를 실제 `Maps/Actions/`에 맞춘다. ADR-035는 **GameDev 첫 SQL 구조 PR이 실제 병합된 뒤 GameDev Astra에게 사실 대조를 요청하고** 최신 영속성 계약을 대조해 SqlClient 직접 ADO.NET·typed RPC·ORM 미도입을 설명하고 ADR-005에 대체 관계를 표시한다. runtime SQL principal과 recovery Windows 통합 인증을 혼동하지 않는다.
- ADR-010 DLL 정책은 `.gitignore` 허용 목록과 `Shared.csproj`의 현재 설정을 대조한다. ADR-029 부록의 실행 안내는 DEVELOPMENT로 연결한다. ADR-021·030·033·017의 지정된 옛 경로·표기를 정리한다. 코드 주석이 참조하는 ADR 본문/번호는 보존한다.
- CURRENT의 완료 목표 링크·RESUME의 과거 상태 재서술을 현재 goal 정본과 조율한다. INDEX에 MSSQL 진입 링크와 완료 로드맵 표기를 정리한다. MSSQL 본문은 GameDev 소유다.
- ORCA의 과거 관찰은 archive/workflow에 보관하고 깨진 로컬 링크를 고친다. `Directory.Build.props`의 없는 절 번호 주석은 GameDev 소유 확인 뒤 Sol이 한 줄만 수정한다.
- `00_Document/ADR.md`, `ADR_History.md`는 살아 있는 ADR/INDEX에 작성 기준·날짜를 합친 뒤 원본을 삭제한다. 승인된 refs 책 요약 29항목은 두 `_index.md` 표에 핵심 한 줄·고정 URL로 합치고 conventions/INDEX를 갱신한다.
- `00_Document/reports/2026-09-29-codex-workflow-proposal.html` 삭제와 archive/link-map의 해당 target 고정 URL 변경, ai-readiness 평가·codex setup 보고서의 완료 goal 근거 폴더 이동은 승인된 대상만 수행한다. 이전→새 경로표, href 수정, 삭제 대상 역참조 0과 archive link-map 무결성을 확인한다.
- REVIEW_CHECKLIST는 goal-loop 검증 단계에서 링크하는 안을 우선한다. 내용 통합은 실제 중복/누락을 비교한 뒤 선택 이유를 기록한다.
- `archive/**`의 역사 자료, 코드 주석이 참조하는 ADR, catalog locator의 FEATURE_MAP·ARCHITECTURE·domains 위치는 보존한다. 위에서 명시 승인된 archive 색인/link-map 정비만 예외다.

Sol은 FEATURE_MAP·ENTRY_POINTS·domains의 저장소 경로 존재 검사를 `99_Tools/CodeRules/`에 추가한다. 기존 수집/실행/결과 계약을 재사용하고 warning 파일럿으로 연결한다. 진단은 출처 파일·대상 경로·수정 방법을 제공한다. 외부 URL·심볼·상대 기준 경로·파일/디렉터리 경로의 계약을 먼저 정해 오탐을 통제한다. 도구/환경 실패는 위반 warning과 별도 상태로 남기며 조용히 성공시키지 않는다. 신규 Opus가 유효 경로·없는 경로·기준 디렉터리·제외 항목·실행 불가의 회귀를 작성·실행한다.

보관 tag는 사용자 승인된 `archive/document-sources-2026-09-29` 하나, 대상 SHA `59c7f087dc630df79650cedc3ede29765397bd8d`에 한정한다. 문서 이동/삭제의 보관 근거 단계에서 존재와 이름 미사용을 확인해 annotated tag를 만들고 그 ref 하나만 push하며 `git ls-remote --tags`로 대조한다. 이미 있으면 대상 일치를 확인하고 덮어쓰지 않는다. `archive/claude-setup-2026-09-29`는 고정 보관을 유지한다. 현재 초안 단계에서는 실행하지 않았다.

### PowerShell 출력 순서 고정

새 Sol이 `99_Tools/CodeRules/check-powershell.ps1`의 결과·파일·진단 객체를 `[ordered]`로 고정한다. Python·SQL adapter 순서 안정성은 이 작업의 사전 조사에 포함하고 결과만 기록한다. 다른 adapter에 수정이 필요하면 실제 결함과 영향 파일을 메인에 보고한다.

신규 Opus는 같은 고정 fixture를 **별도 프로세스에서 두 번** 실행해 stdout 바이트가 같은지 확인하고, 구조·진단 의미·exit·stderr 계약 보존도 대조한다. 실행환경·입력 hash·명령·원시 출력을 보존한다. 순서만 안정화하는 구조 커밋과 정책/exit 같은 동작 변경을 섞지 않는다. 두 번 일치했다는 근거를 모든 입력·환경의 영구 결정성으로 확대하지 않는다.

### 백로그로 분리 — 파트별 정리 뒤 실제 All 비교와 승격 제안

메인 `msg_c1412c982ac5`에 따라 아래 단위는 **이 목표의 구현·종료 범위에서 제외해 BACKLOG 후보로 등록**한다. 촉발 조건은 GameDev·Architecture·Management 세 파트 PS 정리의 실제 병합 완료다. 조건 충족 뒤 별도 goal에서 최신 main을 기준으로 측정한다. Rules가 다른 파트 파일의 서식을 수정하지 않는다. 기존 PS289를 현재 잔여 수로 가정하지 않는다.

이때 백로그의 문서 근거 인용 검사와 집계·비교 helper를 실제 사용 목표로 승격하고 goal 링크를 남긴다. 파일·규칙·소유 파트별 수, 최소 줄, 진단 identity 차이, 소유 맵 존재/unknown을 제공한다. 경로·줄·JSON Pointer·값의 원시 대조와 집계 생성은 책임을 나누고 의미 판정은 Opus가 수행한다. 속성 순서만 다른 JSON·섞인 줄·소유 불명 경로 등 외부 관찰 가능한 fixture로 검증한다.

기존 CodeRules는 선택 PS 진단 한 건에도 exit1이다. 따라서 **PSSA 진단 severity**, **checker의 상태/exit**, **CI job 실패 여부**를 따로 측정하고 warning 파일럿과 충돌하는 실제 지점을 보고한다. 승격 효과가 이미 존재하면 그대로 알리고 이름만 error로 바꾸는 작업을 성과로 삼지 않는다. 기존 CI를 비차단으로 낮추거나 새 error gate를 적용하는 정책은 메인→사용자 결정 뒤 별도 동작 커밋으로 한다.

## 소유권·PR 경계·검증

PR 경계는 BACKLOG → 운영 규칙/참고 근거 → 사실 정정/경로 검사 → legacy 통합·삭제/보관 → 출력 순서 고정으로 나눈다. **착수 판단:** 사실 정정(FEATURE_MAP·ADR 경로·CURRENT/RESUME/INDEX)과 legacy 통합·삭제·archive link-map·tag는 검토 성격과 보존 검증이 달라 별도 PR로 나눈다. legacy의 정확한 이동·삭제 목록은 해당 단위 시작 때 고정한다. BACKLOG 이후 독립이고 파일이 겹치지 않는 작업은 준비 상태에 따라 순서를 조정한다. ADR-035의 선행 PR 병합 전에는 그 파일을 쓰지 않는다. PR마다 정확한 사용자 병합 승인을 받는다. 정책 제안 자체와 사용자 결정 뒤 실제 정책 변경은 구분한다.

- Astra: goal·계약·출처 조사·설계 설명·ADR/보고 자료·결과 통합·담당 브랜치 commit/push/PR. 구현 전체를 직접 대신하지 않는다.
- 새 Sol `gpt-6.1-sol xhigh`: 할당 제품·기계 문서 정비 파일만 쓴다. 테스트 파일·Git commit/push·추가 위임 권한은 없다.
- 신규 Opus `claude-opus-5-5`: 작성 종료 뒤 실제 diff/원시 실행부터 독립 실사, 테스트 파일과 판정 근거만 쓴다. 문서만 변경한 단위는 정적 실사, 코드 단위는 독립 테스트까지 수행한다. 파트당 동시 검증자는 하나다.
- Gardener: 목표 전체 PR 병합·결과 기록 뒤, R-8 직전 신규 Opus. 보고서 하나만 쓰고 최대 두 후보만 제안한다. goal 상태 문장의 시점 경과가 재발했는지도 관측한다.
- GameDev의 DB·SQL·서버 영속성·MSSQL과 Management catalog/frontend는 다른 파트 소유다. 쓰기 조율이 필요한 공유 문서는 자연 지점과 정확한 파일 소유권을 합의한다. SQLFluff 보류를 임의 해제하지 않는다.
- 각 작업자 계약에는 최신 규칙 관련 절 원문·기준 SHA/hash·사전 메모 위치·허용 파일·금지·수용 조건을 붙인다. 새 도구의 진단은 수리 안내와 실행 불가/위반 구분을 요구한다. 구현자와 검증자의 파일 소유를 분리하고 이전 쓰기 종료 뒤 넘긴다.
- 작업 하나 후 정산·종료하고 새 세션으로 수정/재검증한다. 지정 모델 부재·권한 모달·모호한 launch는 공식 절차로 메인에 보고한다. 내부 sub-agent로 구현/독립 검증을 대체하지 않는다.

검증 근거는 단위별 `.backups/verification/2026-10-03-harness-principles/` 아래에 두고 goal은 경로·환경·결과·미실행만 요약한다. 실제 출력·diff·원문 판정과 사전 메모의 준수 위치를 대조한다. 원문 계약/메모 누락, 규칙 위반, 메모-결과 불일치, 측정값 위장, 항상 통과하는 동어반복 테스트는 수정 또는 메인 결정 전 통과하지 않는다. 도구가 통과해도 사람 가독성·책임 분리·주석·배치와 이름·탐색·중복 이유는 별도 판정한다.

## 메인 검토가 필요한 개선 입력

| 입력 | 승인된 추천 | 적용 경계 |
|---|---|---|
| 검증·병합 기록의 기계적 이관 맥락 메모 | 이번 종료 기록은 짧더라도 현행 메모를 작성한다. 일반 간이 형식은 기존 양식 내 필수 필드 축약으로 제안한다 | 작은 작업 예외나 독립 검증 면제를 만들지 않는다. 이 승인 방향 안에서 형식을 구체화하고 예외 확대는 메인 판단 |
| PS warning 파일럿과 현행 exit1 | 기존 상태를 정확히 문서화하고 정리 후 실측으로 severity/exit/job를 분리 비교한다 | 과거 도입을 소급해 warning 성공으로 바꾸거나 현재 CI 정책을 무단 완화하지 않는다 |
| 큰 공식 계약의 미제출 draft | receipt가 turn_start_unobserved이면 대상/runtime/incarnation을 확인하고 **terminal read JSON의 draft → 공식 payload 일치 → 텍스트 없는 Enter1회 → 수 초 뒤 draft 소멸·Working·worker-show** 순서로 확인한다. 메인이 첫 회에 tail만 보고 오판한 사례도 관찰 사실로 보존한다 | `msg_c1412c982ac5`가 승인한 절차 정비 방향. 공식 CLI 복구 계약과 결합하며 임의 사용자 draft·중복 텍스트·침묵만으로 abandon 금지. 모호하면 메인 보고 |
| Run 바인딩 후 term 우편 누락 | Astra가 새 Run을 만든 즉시 메인에게 run 주소를 알리고 바인딩 전 메시지는 처리/보존 후 전환한다. R-3/R-4 근처에 회신 주소 원칙을 둔다 | 이번도 Run 생성 시 이 방식 적용. handle 태그/출처 대조는 유지 |

미결 세 정책, workflow lint 도구 도입, npm engines 해소, PS 정책 승격은 이미 분리된 사용자 결정 영역이다. 모든 결정을 먼저 받느라 BACKLOG를 막지 않는다. 메인 검토는 `msg_c1412c982ac5`로 완료됐고 승인 방향 안의 작업은 계속한다.

## 목표 종료 기준

1. BACKLOG 첫 PR의 필드·씨앗·출처·중복 제외와 이전 종료 기록 보존이 독립 실사되고 병합된다.
2. 승인된 원칙·운영 결정·N1/N2가 정본/양식에 일관되게 연결되고 현실적인 작업 경로에서 찾을 수 있는지 검토된다.
3. 참고·ADR·legacy 정비는 출처·대체 관계·역참조·보관 근거·catalog locator를 보존하며, tag 실행 결과는 단일 ref의 실제 증거로 남는다.
4. 새 경로 검사와 heartbeat 판정, PowerShell 출력 고정은 신규 Opus의 독립 테스트와 실제 실행 근거를 갖춘다. 코드/문서·과거/이번·실행/보류를 구분한다.
5. 파트 정리 뒤 실제 All 비교·승격 제안·인용/집계 helper가 BACKLOG의 한 후보에 묶이고 세 파트 PS 정리 병합 완료라는 촉발 조건·출처·담당을 갖춘다. 그 단위의 실제 구현·측정은 이 목표 종료조건이 아니다.
6. 각 PR 사용자 승인·병합, 파트별 정본/다음 계약 적용 시점 전달과 수신 기록, goal 결과, Gardener 판정·정산 뒤 메인에게 R-8 종료를 요청한다.

## 진행 기록과 현재 미실행

- 2026-10-03: `msg_c1412c982ac5`를 live 메인 발신자와 대조하고 승인 보정 세 건을 반영했다. `git fetch origin main` 뒤 기준 `5616573`이 같음을 확인하고 `docs/harness-backlog`를 만들었다. 이전 종료 기록은 첫 별도 커밋 `110dd8f`로 보존했다.
- 착수 사전 맥락: `.backups/verification/2026-10-03-harness-principles/astra-context.md`. 초안 자체 확인은 `.backups/planning/harness-principles/entry-evidence.json`이다. 자체 확인은 독립 판정이 아니다.
- 현재 제품/문서 구현 작업자·독립 판정·PR·CI는 미실행이다. 외부 원문 재조사·태그·DB·Unity·게임 실행도 이번 목표에서 아직 수행하지 않았다. 새 Run과 BACKLOG 최초 작업 발행을 이어간다.

