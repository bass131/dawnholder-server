# 하네스 원칙 채택과 문서 정비

2026-10-03, 작성 `[Rules Astra]`. **사용자 휴식으로 일시 대기 — [재개 지점](#재개-지점)부터 읽는다.** 메인 `msg_ac1ed6a996ab`가 전달한 휴식 결정에 따라 새 작업자를 발행하지 않는다. 첫 BACKLOG PR의 결함 #2 수리는 끝났지만 독립 재검증·PR·CI·병합은 남았다. All 비교/승격/helper와 legacy 통합·삭제·보관은 이번 범위 밖이다. ADR-035는 GameDev 첫 SQL 구조 PR 병합 뒤 작성한다. [적용 결정](#정본-반영-전-적용-중인-사용자-결정)은 지금 지킬 운영 결정이며, 후속 정본 문서·도구의 구현·검증·병합 실적과 구분한다.

## 문제와 목표

반복 규칙이 문서에만 있고 경로 목록·검사 결과·종료 기록이 실제 상태와 어긋나면 다음 작업의 유지보수성과 검증 신뢰도가 낮아진다. 먼저 goal 전 후보의 정본 BACKLOG를 만들어 Management가 소비할 계약을 제공하고, 승인된 하네스 원칙을 기존 규칙·도구·문서 탐색에 연결한다. DB 저장소·연동 완성의 선행 경로를 막지 않으며 속도보다 독립 검증·실제 근거·사람 가독성을 우선한다.

목표 기준·상태·결과는 이 `goal.md` 한 곳에 둔다. CURRENT는 링크만 유지한다. BACKLOG는 goal로 승격되기 전 후보만 담는다. 분할 PR은 이 목표의 작업 단위이며, 목표 분할이나 추가 Astra 수명은 메인이 결정한다.

## 결정 출처와 적용 경계

모든 사용자 결정은 **메인이 전달한 것**이며 사용자 직접 입력으로 격상하지 않는다. 아래 메시지는 요구사항 근거이며 과거 세션의 실행 권한을 재사용하는 근거가 아니다. 최신 착수 결정 `msg_c1412c982ac5`(2026-10-03T10:39:09Z)는 세 보정과 개선 입력 네 건의 추천 방향을 승인했다. 원문은 `.backups/verification/2026-10-03-harness-principles/main-goal-approval.json`이다.

| 출처 | 이 목표에 반영한 결정 |
|---|---|
| `msg_c9bc79f8ec43` (메인, 2026-10-03T10:26:02Z) | 새 Rules 세션의 현재 계약, 초안 후 검토, BACKLOG 씨앗 보강, 종료 기록 이관, 개선 입력 네 건 |
| `msg_61d4c35695da` (메인, 2026-10-03, 이전 근거 `main-decisions.json`) | 참고 자료·ADR·운영/기술 문서·legacy 정비·경로 드리프트·고정 보관 tag의 앞선 승인 범위 |
| `msg_8639aeaf7a12` (메인, 2026-10-03T08:32:32Z) | DB 우선, PS 정리의 파트별 소유, BACKLOG 첫 별도 PR, heartbeat 예외, 인용 검사 helper 후보 |
| `msg_d5453356cc75` (메인, 2026-10-03T10:17:49Z) | npm warning 후보는 백로그, PowerShell `[ordered]`는 이번 범위, 집계·비교 helper는 인용 검사 후보에 통합 |
| `msg_22a9b4109ee7` (메인 최종 결정, 새 계약과 이전 인계가 인용) | 확정 실패 3회 뒤 네 번째는 새 Sol + 새 Fable Advisor, 재승인 대기 없음, Astra 구현 격상 제외 |
| `msg_ae8e98ad1d0d` (메인, 이전 근거 `main-draft-input-guidance.json`) | 미제출 draft·추천 프롬프트는 사용자 지시가 아니고 그 자체로 pane 종료를 막지 않음 |
| `msg_acba01cbf81e` (메인→Management, 새 계약이 전달) | DB 연동 뒤 운영툴 서버 운영 시각화 후보 |
| `msg_238982aa5d26` (메인, 2026-10-03T11:00:22Z) | 사용자 범위 원칙, 점검 동결 대체, legacy 통합·삭제·보관을 다음 계획으로 이관, 기존 운영 규칙 단위에 이 원칙의 정식 문서화 |
| `msg_168d9aa35710` (메인, 2026-10-03T11:14:25Z) | 메모/임시 자료의 공용 규칙·환경 사실을 정본으로 이관, 점검 지점·다음 계획 규칙, E후보는 BACKLOG 다음 PR에 추가, CLAUDE는 메인 작성 |
| `msg_bc5d8b721551` (메인, 2026-10-03T11:15:30Z) | capacity 재시도 및 30분 지속 실패 시 신규 Sol→신규 Astra 작업자 한정 예외; 리드 직접 구현 금지·독립 Opus 대체 금지 |
| `msg_39d7be6b2eb9` (메인, 2026-10-03T11:16:48Z) | 두 개 이상 goal로 이어지는 안건의 마일스톤 승격·점검 연계·상태 정본 규칙을 같은 운영 단위에 반영; 실제 파트별 로드맵은 goal 종료 점검 때 |
| `msg_b10d232dce1b` (메인, 2026-10-03T11:29:24Z) | draft 세 번째 관측 추가; 새 pane·공식 계약 크기의 placeholder 확인 시 담당 Astra의 Enter 단독 복구, 조건 불충족 시 메인 보고를 운영 단위에서 정식화 |

원문 위치: `.backups/verification/2026-10-02-agent-rule-context/resume-2026-10-03/`. 초기 계약은 `.backups/planning/harness-principles/main-request.json`에 보존한다. 원문을 직접 재조회하지 않은 메시지는 위와 같이 전달 출처를 적고 직접 수신으로 표현하지 않는다.

채택 원칙은 다음 다섯 가지다.

1. 반복 규칙은 검사로 바꾸고 진단에 고치는 방법을 포함한다.
2. 정본 helper·생성기·구조를 사용하는 길을 가장 쉽게 만든다.
3. 수기 경로 목록에는 실제 존재를 확인하는 드리프트 검사를 붙인다.
4. 새 검사는 warning 파일럿 → 실측 → error 승격 순서로 도입하고, 유지비가 가치보다 크면 강등한다. 실행 불가와 규칙 위반은 다른 상태다.
5. 구조 변경과 동작 변경은 다른 커밋으로 나눈다.

진행 중 단계는 마친 뒤 다음 계약부터 적용한다. 현행 주석 정책은 유지한다. 검증 강도 차등·작은 작업 예외, 규칙 문서 가지치기, 사람용 코드 따라읽기 문서는 미결정으로 남기며 이번에 정책으로 도입하지 않는다. 앞서 개별 승인된 legacy 정비도 최신 `msg_238982aa5d26`에 따라 다음 계획 후보로 옮겼다.

## 범위 운영 원칙

사용자가 정한 원칙을 메인이 `msg_238982aa5d26`으로 전달했다. 범위는 계획 때 완료조건·PR 경계·범위 밖 목록으로 확정한다. 완료조건을 채우는 데 필요한 독립 검증 결함, 이번 변경이 만든 테스트·빌드·CI 실패, 계약·규칙 위반과 그 결함에 한정한 재발 방지는 현재 루프에서 해결한다.

완료조건에 없는 새 기능·화면·도구·검사, 디자인 개선·다른 영역 정리는 다음 계획 후보로 한 줄만 기록하고 별도 승인 요청으로 올리지 않는다. 사용자의 새 요청도 기본은 다음 계획이며, 사용자가 이번 goal 포함을 명시하면 메인이 범위 변경으로 다시 계획해 전달한다. 이번 변경과 무관한 기존 실패는 범위 밖으로 두되 PR 보고에 공개한다. 애매하면 기본값은 범위 밖으로 두고 메인에 「범위 판정」을 묻는다. 결함 수정이 완료조건 변경을 요구하면 그 변경을 진행하기 전에 메인 판단을 받는다. 다음 goal 범위는 PR 병합 승인 때 메인과 사용자가 정한다.

## 진입 상태와 보존

- cwd `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`, 진입 branch `fix/code-rules-import-candidates`; 착수 branch `docs/harness-backlog`.
- HEAD·origin/main·원격 refs/heads/main 조회 결과 `5616573c32a2b2e0b677bc21b75e22a08d21f285`; 좌우 차이 0/0. 실제 브랜치 생성 직전에 main을 다시 확인한다.
- 진입 시 tracked 변경은 이전 `01_Phases/goals/2026-10-02-agent-rule-context/goal.md` 하나(62+/3−)였다. hash `99CD14239F0ADD91DD8B334A8B4CCC4C171CC7CA98A4BBD6C3EA9583831FDA08`가 r8-final-state·스냅샷과 일치하고, 당시 diff는 r8-final-goal.patch와 개행 정규화 후 일치했다.
- 메인 지시에 따라 이 종료 기록은 최신 main의 새 브랜치에 보존 이관했다. **첫 별도 문서 커밋 `110dd8f`**으로 BACKLOG PR에 포함한다. 기존 내용의 임의 정정·폐기·옛 브랜치 추가 push는 하지 않는다. 커밋 직전 사전 맥락과 diff/hash를 다시 확인한다.
- 위 이관 커밋은 사전 메모·diff/hash 확인 후 이미 완료했다. 비교 기준은 로컬 main ref가 아니라 확인한 `origin/main`의 `5616573`이다.
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
| 검증 강도 차등·작은 작업 예외 | 미결 사용자 판단; 메인 첫 재계획 `msg_8639aeaf7a12`(2026-10-03T08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 현 독립 검증 유지, 메인→사용자 |
| 규칙 문서 가지치기 | 미결 사용자 판단; 메인 첫 재계획 `msg_8639aeaf7a12`(2026-10-03T08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 기존 명시 정비와 구분, 메인→사용자 |
| 사람용 코드 따라읽기 문서 | 미결 사용자 판단; 메인 첫 재계획 `msg_8639aeaf7a12`(2026-10-03T08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 목적·유지비 결정, 메인→사용자 |
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
| 계획 시 범위 확정과 루프 안/밖 판정 | Sol: goal-loop 및 관련 운영 정본 | 위 「범위 운영 원칙」의 완료조건·PR 경계·보류 목록·새 요청·기존 실패·애매한 경우·완료조건 변경·다음 계획 결정 시점이 계약과 일치하는지 실사 |
| 미제출 draft와 공식 계약 draft 구분 | Sol: ORCA R-6, session-handoff | 사용자 제출 여부·추천 문구·계약 주입 상태를 분리하는 현실적인 절차 실사 |
| 3회 확정 실패 뒤 새 Sol+Fable Advisor | Sol: AGENTS·orca-work·ORCA R-5/R-7 | 같은 계약/번호 집계, 사전 보고/재승인 불필요, 조언 선행·채택/기각 기록, 네 번째 실패 뒤 메인 판단. 기존 goal 검토 시범과 구분 |
| 내용 없는 heartbeat 태그 예외 | Sol: AGENTS·ORCA와 수신 판정 helper | 빈 생존 신호만 예외, from_handle/taskId/dispatchId 일치. 내용 있는 메시지는 태그 필수. 신규 Opus가 정상/누락/불일치 fixture 작성·실행 |
| 이전 N1 추가 차단사유 두 개 동기화 | Sol: AGENTS·CODE_CONVENTION·task-context/양식의 필요한 정본 연결 | 상수/수기 값을 측정값처럼 기록, 구현을 복제해 항상 통과하는 테스트. `verification/verdict.md` N1이 원천이고 Gardener N1과 별개 |
| 이전 N2 DEVELOPMENT 시점 표현 | Sol: DEVELOPMENT 검사 실행 설명 | PR166/167의 과거 실행 근거로 갱신, 새 목표 실행 실적으로 표현 금지 |

CLAUDE.md는 메인 소유다. 그 문서의 이미 존재하는 두 차단 사유를 나머지 계약·양식에 연결하며, 메인 쓰기가 추가로 필요하면 별도 소유권 조율한다. 스킬 수정 작업 발행 시 해당 제작 지침도 확인한다. 외부 원문은 다음 작성 단계에서 확인하고, 이번 초안에서는 전달된 채택 결정을 외부 저자의 발언으로 인용하지 않는다.

ADR-034에는 이미 전달된 기각 대안과 이유도 보존한다: 검사를 별도 목표로 미루면 그동안 새 코드가 검사 없이 쌓이는 점, 뷰어보다 경계 검사가 유지보수성 우려에 직접 대응하고 같은 스냅샷을 재사용하는 점, 주간 Gardener보다 목표 종료가 세션 운영의 자연 지점이라는 점, 메인 직접 작성 대신 역할·검증 분리를 유지한 점, legacy 전체 archive 이동보다 지정 통합을 선택해 manifest/link-map 변경 비용을 줄인 점이다. 새로운 대안 검토 결과처럼 쓰지 않는다.

### 문서의 사실·탐색 정정과 경로 검사

앞선 승인 `msg_61d4c35695da` 중 최신 범위 결정에서 유지한 사실·탐색 정정만 수행한다. 각 파일의 실제 상태와 소유자를 작업 직전에 확인한다. legacy 통합·삭제·보관은 아래 범위 밖 목록으로 분리했다.

- FEATURE_MAP의 `Maps/States/Actions/`를 실제 `Maps/Actions/`에 맞춘다. ADR-035는 **GameDev 첫 SQL 구조 PR이 실제 병합된 뒤 GameDev Astra에게 사실 대조를 요청하고** 최신 영속성 계약을 대조해 SqlClient 직접 ADO.NET·typed RPC·ORM 미도입을 설명하고 ADR-005에 대체 관계를 표시한다. runtime SQL principal과 recovery Windows 통합 인증을 혼동하지 않는다.
- ADR-010 DLL 정책은 `.gitignore` 허용 목록과 `Shared.csproj`의 현재 설정을 대조한다. ADR-029 부록의 실행 안내는 DEVELOPMENT로 연결한다. ADR-021·030·033·017의 지정된 옛 경로·표기를 정리한다. 코드 주석이 참조하는 ADR 본문/번호는 보존한다.
- CURRENT의 완료 목표 링크·RESUME의 과거 상태 재서술을 현재 goal 정본과 조율한다. INDEX에 MSSQL 진입 링크와 완료 로드맵 표기를 정리한다. MSSQL 본문은 GameDev 소유다.
- ORCA의 깨진 로컬 링크를 고친다. `Directory.Build.props`의 없는 절 번호 주석은 GameDev 소유 확인 뒤 Sol이 한 줄만 수정한다. 과거 관찰의 archive 이동은 다음 계획으로 넘긴다.
- REVIEW_CHECKLIST는 goal-loop 검증 단계에서 기존 문서를 링크한다. 내용 통합·삭제는 이번에 수행하지 않는다.
- `archive/**`의 역사 자료, 코드 주석이 참조하는 ADR, catalog locator의 FEATURE_MAP·ARCHITECTURE·domains 위치는 보존한다. 이번에는 legacy 이동·삭제나 archive 색인/link-map 변경을 수행하지 않는다.

Sol은 FEATURE_MAP·ENTRY_POINTS·domains의 저장소 경로 존재 검사를 `99_Tools/CodeRules/`에 추가한다. 기존 수집/실행/결과 계약을 재사용하고 warning 파일럿으로 연결한다. 진단은 출처 파일·대상 경로·수정 방법을 제공한다. 외부 URL·심볼·상대 기준 경로·파일/디렉터리 경로의 계약을 먼저 정해 오탐을 통제한다. 도구/환경 실패는 위반 warning과 별도 상태로 남기며 조용히 성공시키지 않는다. 신규 Opus가 유효 경로·없는 경로·기준 디렉터리·제외 항목·실행 불가의 회귀를 작성·실행한다.

보관 tag 생성/push는 legacy 보관 단위와 함께 다음 계획으로 넘긴다. 앞선 승인 대상과 보존 경계는 아래 목록에 출처와 함께 남기며 이번에는 실행하지 않는다.

### PowerShell 출력 순서 고정

새 Sol이 `99_Tools/CodeRules/check-powershell.ps1`의 결과·파일·진단 객체를 `[ordered]`로 고정한다. Python·SQL adapter 순서 안정성은 이 작업의 사전 조사에 포함하고 결과만 기록한다. 다른 adapter에 수정이 필요하면 실제 결함과 영향 파일을 메인에 보고한다.

신규 Opus는 같은 고정 fixture를 **별도 프로세스에서 두 번** 실행해 stdout 바이트가 같은지 확인하고, 구조·진단 의미·exit·stderr 계약 보존도 대조한다. 실행환경·입력 hash·명령·원시 출력을 보존한다. 순서만 안정화하는 구조 커밋과 정책/exit 같은 동작 변경을 섞지 않는다. 두 번 일치했다는 근거를 모든 입력·환경의 영구 결정성으로 확대하지 않는다.

### 백로그로 분리 — 파트별 정리 뒤 실제 All 비교와 승격 제안

메인 `msg_c1412c982ac5`에 따라 아래 단위는 **이 목표의 구현·종료 범위에서 제외해 BACKLOG 후보로 등록**한다. 촉발 조건은 GameDev·Architecture·Management 세 파트 PS 정리의 실제 병합 완료다. 조건 충족 뒤 별도 goal에서 최신 main을 기준으로 측정한다. Rules가 다른 파트 파일의 서식을 수정하지 않는다. 기존 PS289를 현재 잔여 수로 가정하지 않는다.

이때 백로그의 문서 근거 인용 검사와 집계·비교 helper를 실제 사용 목표로 승격하고 goal 링크를 남긴다. 파일·규칙·소유 파트별 수, 최소 줄, 진단 identity 차이, 소유 맵 존재/unknown을 제공한다. 경로·줄·JSON Pointer·값의 원시 대조와 집계 생성은 책임을 나누고 의미 판정은 Opus가 수행한다. 속성 순서만 다른 JSON·섞인 줄·소유 불명 경로 등 외부 관찰 가능한 fixture로 검증한다.

기존 CodeRules는 선택 PS 진단 한 건에도 exit1이다. 따라서 **PSSA 진단 severity**, **checker의 상태/exit**, **CI job 실패 여부**를 따로 측정하고 warning 파일럿과 충돌하는 실제 지점을 보고한다. 승격 효과가 이미 존재하면 그대로 알리고 이름만 error로 바꾸는 작업을 성과로 삼지 않는다. 기존 CI를 비차단으로 낮추거나 새 error gate를 적용하는 정책은 메인→사용자 결정 뒤 별도 동작 커밋으로 한다.

## 소유권·PR 경계·검증

PR 경계는 **BACKLOG → 운영 규칙/참고 근거 → 사실 정정/경로 검사 → 출력 순서 고정**이다. 착수 때 사실 정정과 legacy 정비는 검토 성격이 달라 분리하기로 했고, 최신 `msg_238982aa5d26`으로 legacy PR 자체를 이번 goal에서 제외했다. BACKLOG 이후 독립이고 파일이 겹치지 않는 작업은 준비 상태에 따라 순서를 조정한다. ADR-035의 선행 PR 병합 전에는 그 파일을 쓰지 않는다. PR마다 정확한 사용자 병합 승인을 받는다. 정책 제안 자체와 사용자 결정 뒤 실제 정책 변경은 구분한다.

- Astra: goal·계약·출처 조사·설계 설명·ADR/보고 자료·결과 통합·담당 브랜치 commit/push/PR. 구현 전체를 직접 대신하지 않는다.
- 새 Sol `gpt-6.1-sol xhigh`: 할당 제품·기계 문서 정비 파일만 쓴다. 테스트 파일·Git commit/push·추가 위임 권한은 없다.
- 신규 Opus `claude-opus-5-5`: 작성 종료 뒤 실제 diff/원시 실행부터 독립 실사, 테스트 파일과 판정 근거만 쓴다. 문서만 변경한 단위는 정적 실사, 코드 단위는 독립 테스트까지 수행한다. 파트당 동시 검증자는 하나다.
- Gardener: 목표 전체 PR 병합·결과 기록 뒤, R-8 직전 신규 Opus. 보고서 하나만 쓰고 최대 두 후보만 제안한다. goal 상태 문장의 시점 경과가 재발했는지도 관측한다.
- GameDev의 DB·SQL·서버 영속성·MSSQL과 Management catalog/frontend는 다른 파트 소유다. 쓰기 조율이 필요한 공유 문서는 자연 지점과 정확한 파일 소유권을 합의한다. SQLFluff 보류를 임의 해제하지 않는다.
- 각 작업자 계약에는 최신 규칙 관련 절 원문·기준 SHA/hash·사전 메모 위치·허용 파일·금지·수용 조건을 붙인다. 새 도구의 진단은 수리 안내와 실행 불가/위반 구분을 요구한다. 구현자와 검증자의 파일 소유를 분리하고 이전 쓰기 종료 뒤 넘긴다.
- 작업 하나 후 정산·종료하고 새 세션으로 수정/재검증한다. 지정 모델 부재·권한 모달·모호한 launch는 공식 절차로 메인에 보고한다. 내부 sub-agent로 구현/독립 검증을 대체하지 않는다.

검증 근거는 단위별 `.backups/verification/2026-10-03-harness-principles/` 아래에 두고 goal은 경로·환경·결과·미실행만 요약한다. 실제 출력·diff·원문 판정과 사전 메모의 준수 위치를 대조한다. 원문 계약/메모 누락, 규칙 위반, 메모-결과 불일치, 측정값 위장, 항상 통과하는 동어반복 테스트는 수정 또는 메인 결정 전 통과하지 않는다. 도구가 통과해도 사람 가독성·책임 분리·주석·배치와 이름·탐색·중복 이유는 별도 판정한다.

## 메인이 방향을 승인한 개선 입력

| 입력 | 승인된 추천 | 적용 경계 |
|---|---|---|
| 검증·병합 기록의 기계적 이관 맥락 메모 | 이번 종료 기록은 짧더라도 현행 메모를 작성한다. 일반 간이 형식은 기존 양식 내 필수 필드 축약으로 제안한다 | 작은 작업 예외나 독립 검증 면제를 만들지 않는다. 이 승인 방향 안에서 형식을 구체화하고 예외 확대는 메인 판단 |
| PS warning 파일럿과 현행 exit1 | 기존 상태를 정확히 문서화하고 정리 후 실측으로 severity/exit/job를 분리 비교한다 | 과거 도입을 소급해 warning 성공으로 바꾸거나 현재 CI 정책을 무단 완화하지 않는다 |
| 큰 공식 계약의 미제출 draft | receipt가 turn_start_unobserved이면 대상/runtime/incarnation을 확인하고 **terminal read JSON의 draft → 공식 payload 일치 → 텍스트 없는 Enter1회 → 수 초 뒤 draft 소멸·Working·worker-show** 순서로 확인한다. 메인이 첫 회에 tail만 보고 오판한 사례도 관찰 사실로 보존한다 | `msg_c1412c982ac5`가 승인한 절차 정비 방향. 공식 CLI 복구 계약과 결합하며 임의 사용자 draft·중복 텍스트·침묵만으로 abandon 금지. 모호하면 메인 보고 |
| Run 바인딩 후 term 우편 누락 | Astra가 새 Run을 만든 즉시 메인에게 run 주소를 알리고 바인딩 전 메시지는 처리/보존 후 전환한다. R-3/R-4 근처에 회신 주소 원칙을 둔다 | 이번도 Run 생성 시 이 방식 적용. handle 태그/출처 대조는 유지 |

미결 세 정책, workflow lint 도구 도입, npm engines 해소, PS 정책 승격은 이미 분리된 사용자 결정 영역이다. 모든 결정을 먼저 받느라 BACKLOG를 막지 않는다. 메인 검토는 `msg_c1412c982ac5`로 완료됐고 승인 방향 안의 작업은 계속한다.

## 범위 밖과 다음 계획 후보

- legacy 문서 통합·삭제·보관: 원래 `msg_61d4c35695da`, 이관 `msg_238982aa5d26`. ADR.md/ADR_History→ADR/INDEX, refs 책 요약 통합, 옛 workflow 제안 삭제, ai-readiness·setup 보고서 이동, ORCA 과거 관찰 archive 이동, REVIEW_CHECKLIST 내용 통합, archive 색인/link-map 정비를 다음 계획에서 함께 범위화한다.
- 보관 ref 작업: 같은 두 메시지. 앞선 단일 tag `archive/document-sources-2026-09-29`, SHA `59c7f087dc630df79650cedc3ede29765397bd8d`의 기존 승인 사실을 보존하되 이번 실행은 제외한다. `archive/claude-setup-2026-09-29`는 고정 보관이다.
- 파트 PS 정리 후 All 비교·승격·인용/집계 helper: `msg_c1412c982ac5`, 세 파트 PS 정리 병합 완료 뒤 별도 goal 후보이며 BACKLOG에 묶는다.
- 미결 정책·새 도구·화면 등 첫 BACKLOG의 나머지 후보는 기록만 한다. 씨앗 등록이 그 후보의 착수 권한을 뜻하지 않는다.
- 이번 변경과 무관한 기존 실패는 PR 보고에 구분해 적는다. 현 루프에서 새로 발견한 추가 작업 제안은 아직 없음.

## 후속 운영 단위의 정본 이관과 점검 계획

메인 `msg_168d9aa35710`의 대조표는 `rules-migration-inventory.md`로 보존했다(SHA256 `31ACAB31B5198DF37ACE2DB3EDAE96040C2B8848AF44DCF5D70463E468983A94`). 원천 임시 경로·원본/사본 hash는 `rules-migration-inventory-provenance.json`, 범위 원문은 `main-rules-migration-request.json`과 `backlog-review-and-new-decisions.json`이다. Sol은 대조표의 각 원천 메모리 파일을 직접 읽고 최신 정본을 다시 대조한다. 표의 줄번호는 탐색 시작점이며 이 표 자체를 원문 검증으로 대신하지 않는다.

- 총 네 PR 단위를 유지한다: 현재 BACKLOG → 공용 운영규칙/참고근거 → 도구환경·사실정정/경로검사 → PowerShell 출력순서 고정. PR 줄수 상한은 두지 않는다. 단계 간 수정은 완료조건에 필요한 결함 범위에서 한다.
- 두 번째 PR: 대조표 A 공용 규칙, F의 상충 정리, B의 Orca 운용 사실, 점검 지점·마일스톤 규칙, E의 후보 기록, 기존 운영/하네스 규칙·heartbeat 판정·참고/ADR-034. E는 현재 BACKLOG PR 뒤에 추가하며 지금의 12후보를 확장하지 않는다. 새 검사를 후보 기록만으로 구현하지 않는다.
- draft 복구의 추가 근거는 `main-draft-recovery-third-observation.json`이다. 메인이 전달한 세 번째 사례(Management Sol, `msg_19ca70e05ed8`·`msg_7276a1181c81`)와 당시 Astra의 현행 규칙 준수를 보존한다. 후속 운영 PR에서 새 pane에 다른 입력이 없고 draft가 공식 계약 크기와 맞는 붙여넣기 placeholder임을 확인한 경우 담당 Astra가 Enter만 보내는 절차를 정식화한다. 조건이 맞지 않으면 메인에 보고하며, accepted 뒤 침묵만으로 새 텍스트를 보내지 않는 기존 경계와 구분한다. 이 기록은 Rules가 그 복구를 직접 재현했다는 뜻이 아니다.
- 세 번째 PR: B의 개발·Unity 환경 사실을 기존 사실정정/경로검사와 함께 대조한다. 도구/버전/모델·실행환경의 당시 관측과 현재 재현을 구분하고 근거 없는 일반 보장으로 옮기지 않는다. GameDev 소유 문서·경로는 조율한다.
- capacity: C절 보류는 `msg_bc5d8b721551`로 해소됐다. 작업자 화면의 capacity 오류에는 같은 세션/task에서 1→2→5→10분 간격으로 재시도한다. 최초 관측 뒤 누적30분에도 Sol이 계속 실패하면 기존 세션 정산·종료 뒤 새 gpt-6-astra xhigh **작업자**를 허용한다. 요청/관측/확인불가 실제 모델·전환 사유·결정 근거를 기록한다. 리드 직접 구현은 금지이며 독립 Opus는 재시도만 하고 대체하지 않는다. 다른 모델/권한 대체를 허용하지 않는다.
- D의 메인 전용 행동은 메인이 CLAUDE.md에 쓴다. 운영단위 착수 때 Astra가 브랜치·cwd·Sol 허용파일을 알리고, Sol 쓰기 종료 뒤 메인 CLAUDE 작성·종료를 확인한 다음 신규 Opus가 작성자별로 함께 실사한다. 메모리 정리는 병합 뒤 메인 소유다.
- 점검 지점은 중간에 **현재 BACKLOG PR 병합 뒤**, 기본 종료에 **마지막 PR·goal 결과·Gardener 뒤**로 둔다. 메인이 사용자와 결과·남은 위험·BACKLOG·다음 계획을 확인한 뒤 재개한다. goal 종료 후 다음 goal 자동 시작은 금지한다. 점검 사이 새 아이디어는 BACKLOG에만 두며, 기존 goal 보류 목록과의 연결 방식은 이번 운영규칙 단위에서 명시한다.
- 계획 때 두 개 이상 goal로 이어져야 끝나는 안건은 마일스톤 로드맵을 만들거나 갱신하고 각 goal이 링크한다. roadmap은 순서·선행조건·goal 링크만 두고 진행률·검증결과·결정은 각 goal에 둔다. 종료 점검에서 다음 단계/선행조건/바뀐 순서를 사용자와 확인해 갱신한 뒤 roadmap 또는 BACKLOG에서 다음 goal을 고른다. 목적 중심 표기를 사용하고 기존 역사 기록의 명칭을 무관하게 일괄 변경하지 않는다. Management 경로는 해당 Astra와 조율한다. **이번에는 각 파트의 실제 로드맵을 작성하지 않으며 종료 점검에서 담당 Astra가 초안을 낸다.**

운영규칙 단위가 추가 메시지의 반영 자리이며 새 PR 단위는 만들지 않는다. 최초 Opus가 판정 중이던 고정 입력은 새 요청 때문에 변경하지 않았고, 종료 뒤 이 계획 기록을 추가했다.

## 목표 종료 기준

1. BACKLOG 첫 PR의 필드·씨앗·출처·중복 제외와 이전 종료 기록 보존이 독립 실사되고 병합된다.
2. 승인된 하네스·범위 운영 원칙·운영 결정·N1/N2가 정본/양식에 일관되게 연결되고 현실적인 작업 경로에서 찾을 수 있는지 검토된다.
3. 이번 참고·ADR 설명과 사실·탐색 정정이 출처·대체 관계·기존 참조·catalog locator를 보존한다. ADR-035는 GameDev 첫 SQL 구조 PR 병합 뒤 사실 대조한다. legacy 통합·삭제·보관/tag는 이번 완료조건이 아니다.
4. 새 경로 검사와 heartbeat 판정, PowerShell 출력 고정은 신규 Opus의 독립 테스트와 실제 실행 근거를 갖춘다. 코드/문서·과거/이번·실행/보류를 구분한다.
5. 파트 정리 뒤 실제 All 비교·승격 제안·인용/집계 helper가 BACKLOG의 한 후보에 묶이고 세 파트 PS 정리 병합 완료라는 촉발 조건·출처·담당을 갖춘다. 그 단위의 실제 구현·측정은 이 목표 종료조건이 아니다.
6. 각 PR 사용자 승인·병합, 파트별 정본/다음 계약 적용 시점 전달과 수신 기록, goal 결과, Gardener 판정·정산 뒤 메인에게 R-8 종료를 요청한다.

## 진행 기록과 현재 미실행

- 2026-10-03: `msg_c1412c982ac5`를 live 메인 발신자와 대조하고 승인 보정 세 건을 반영했다. `git fetch origin main` 뒤 기준 `5616573`이 같음을 확인하고 `docs/harness-backlog`를 만들었다. 이전 종료 기록은 첫 별도 커밋 `110dd8f`로 보존했다.
- 착수 사전 맥락: `.backups/verification/2026-10-03-harness-principles/astra-context.md`. 초안 자체 확인은 `.backups/planning/harness-principles/entry-evidence.json`이다. 자체 확인은 독립 판정이 아니다.
- 새 Run `run_a2cfcad665e6`을 만들고 메인에 회신 주소를 통지했다(`msg_7c88aa892d56`). BACKLOG 구현은 `task_8e2401deae8a` / `ctx_ff14d925ab15`, 신규 Sol `term_cb796a21-fbfb-489e-997f-f8c72b38f849`, incarnation `47d0bcb3-6ad5-44f6-b016-280d7e162ddd`다. 최초 명령과 화면은 gpt-6.1-sol xhigh, backend unknown. readiness satisfied와 빈 첫 화면 확인 뒤 `input_accepted` 및 `turn_started`를 관측했다. 근거 `backlog-sol-{readiness,show,first-read,start}.json`.
- **점검 중 범위 동결:** 메인 `msg_4971e82ce074`(2026-10-03T10:46:47Z)에 따라 현재 BACKLOG 단계와 그 결함 수정·독립 실사·PR 준비는 계속한다. 점검 종료 전 다음 단계·새 goal·백로그 항목에는 착수하지 않는다. 이 규칙을 최초 Sol 계약에도 전달했다. 이번 목표의 독립 판정·PR·CI는 아직 미실행이며 외부 원문 재조사·태그·DB·Unity·게임도 미실행이다.
- 점검 보고는 `.backups/verification/2026-10-03-harness-principles/scope-audit-report.md`에 있고 별도 상태 정본을 만들지 않는다. 현재 새로 발견해 보류한 제안은 없음. 기존 미결 정책과 후보는 첫 BACKLOG의 기록 대상으로만 유지한다.
- Sol `msg_6f88e11a8ae4`(2026-10-03T10:58:30Z)의 worker_done succeeded와 쓰기 종료를 확인했다. BACKLOG 신규56줄·12후보와 INDEX 링크1행의 실제 diff/보고를 읽었다. 자체점검은 독립 PASS가 아니다. 보고는 `backlog-implementation/report.md`, 원시는 `self-check.json`이다. `git diff --no-index --check`의 차이 exit1을 래퍼 오류로 처리한 이력은 보고에 공개돼 있고 후속 공백 대조와 구분한다.
- 구현자 completed/succeeded → worker-release(retained/external_terminal) → 동일 incarnation 확인 → terminal close(ptyKilled=true)로 정산했다. 근거 `backlog-sol-{done,settled,release,before-close,close}.json`. 최초 명령은 Astra split의 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`; 구현 보고의 최초 명령 unknown은 구현자가 직접 확인하지 못한 범위다. 신규 독립 Opus의 내용·출처·보존 실사는 아직 판정 전이다.
- 메인 `msg_238982aa5d26`(11:00:22Z)으로 점검 동결이 위 범위 원칙으로 대체됐다. legacy 통합·삭제·보관/tag를 다음 계획 목록으로 옮기고 운영규칙 단위에 범위원칙 문서화를 반영했다. 출처 `main-scope-principle.json`, 사전 맥락 `scope-change-context.md`. 독립 실사는 중단하지 않았고 기존 고정 goal을 `goal-before-scope-decision.md`로 보존한 뒤 검증자에 변경을 통지했다. 새 입력 manifest는 `verification-inputs-scope-update.json`이며 다른 고정 제품/계약은 불변이다.
- 첫 독립 Opus task_d17b411e8f9c/ctx_bed20675a036의 판정 원문 `backlog-verification/verdict.md`(SHA256 `7F5085B8EB5366B794CEB85334D783F0E9D6E6AE5848250EED8F68EADB0E5F60`)은 **출처 결함 #1로 차단**이다. 일부 BACKLOG 행의 원 결정 ID·재전달 관계·이전 결함 #4의 goal 경로를 보완해야 한다. 독립1회/NOT PASS1회/재검증0회이며 worker_done succeeded는 실사 작업 수행 완료이지 제품 PASS가 아니다. Astra가 전문과 실제 출처 행을 읽었고 신규 Sol→신규 Opus로 수정한다.
- 첫 Opus는 completed→release(external retained)→동일 incarnation `84649e2b-b25f-4f53-8ddc-efa6452c8f94`→close ptyKilled=true로 종료했다. N1~N6은 비차단 참고로 원문 보존하며 이번 출처 결함 수정의 필수 범위로 확대하지 않는다. 후보 기록 위치의 관계(N4)는 이미 승인된 후속 운영규칙에서 다룬다. 모델 최초명령의 별도 원시파일 부재(N5)와 로컬 main ref 지연(N6)은 공개된 한계다.
- 신규 수정 Sol `task_b80846a2326c` / `ctx_2c7e5a2ac137`은 `msg_73b2640e767b`(2026-10-03T11:41:00Z)로 쓰기 종료를 보고했다. BACKLOG의 지정6행 출처6셀·이유1셀만 보완했고 12후보·7필드·ID·상태·조건은 보존했다. 후 hash는 `6902ACFBFAD729D5CC8D2433F4CAE69F10895EB78A915C25FDA27C0FBDE1E9CF`이며 보고·diff·원시 명령은 `backlog-repair/`에 있다. 최초 자체확인의 JS 구성 오류와 DateTime 처리 오류는 원문 보존 뒤 보정해 exit0을 확인했다. 이는 자체확인 완료이며 아직 독립 재검증 판정이 아니다.
- 수정 Sol 최초명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`의 실제 호출 기록은 `backlog-repair-sol-launch.json`, 화면은 `backlog-repair-sol-first-read.json`에 있다. Sol 자신이 미확인한 최초명령과 코디네이터의 관측을 구분하며 backend는 unknown이다. 완료→release external retained→동일 incarnation `41fc85eb-110b-4e54-b3dd-053091218b32` 확인→close ptyKilled=true를 `backlog-repair-sol-*`에 보존했다. 다음 신규 Opus에는 현재 입력을 별도 manifest로 고정해 전달한다.
- 신규 Opus `task_2a8271b7a11f` / `ctx_a9477fdbbee9`의 재검증1은 `msg_eee7f524d74f`(2026-10-03T11:54:39Z), 원문 `backlog-reverification/verdict.md`(SHA256 `AF109EA7A95F24EA2468983698FC5AFAF9F3F838F514D350D30BAA00489193FB`)이다. **결함 #1 해소, 새 결함 #2로 NOT PASS**이며 27개 입력 hash 일치·보고/실제 수행 일치를 확인했다. 첫 실사가 놓친 미결3정책의 원결정 ID·재전달 누락을 첫 검출한 것이어서 #1의 재실패로 합산하지 않는다. 수정 Sol의 한정 계약 위반도 아니다. Astra는 원문을 다시 대조해 위 씨앗표 세 행의 출처를 보완하고, 새 Sol이 BACKLOG 출처3셀만 수정하도록 한다. 첫 실사1회+재검증1회, NOT PASS2회, 열린 결함은 #2 한 건이다.
- 재검증1 Opus는 완료→release external retained→동일 incarnation `e64fae32-e1e6-486d-8f3a-c2f03d8353f4`→close ptyKilled=true로 종료했다(`backlog-reverifier-*`). 비차단 R1~R4와 기존 N1~N6은 원문에 보존한다. 후속 검증에서는 수정된 세 셀뿐 아니라 전체12후보의 원문 출처 누락 여부를 함께 대조한다. 아직 PR·CI·병합은 수행하지 않았다.
- 결함 #2 수정 Sol `task_00610ce2e8a4` / `ctx_bd4996c470a4`은 `msg_0db5bd89c49a`(2026-10-03T12:09:28Z)로 쓰기 종료를 보고했다. 지정 출처3셀만 수정했고 메인 전수대조 지시 `msg_002afaca32e2`를 받은 추가계약 `msg_1b5bb5878c10`에 따라 12행을 모두 원문과 대조했다. 추가 출처 누락 없음·비대상 바이트 및 읽기 전용20입력 보존·자체 확인 exit0을 보고했으며 Astra가 보고와 실제3셀 diff를 읽었다. 이는 자체확인 완료이며 독립 PASS가 아니다. 근거는 `backlog-repair2/`, 후 BACKLOG SHA256은 `C39FB604DBD1BC8743332814D15EF1DF3117ACD2504D6E5B2D9D4D06AA95E952`다.
- 수정2 Sol은 완료→release external retained→동일 incarnation `41e3207b-d1cb-4049-9fe5-0bc575aa4200` 확인→close ptyKilled=true로 종료했다(`backlog-repair2-sol-*`). 최초 명령과 화면은 gpt-6.1-sol xhigh, backend unknown이다. 사용자 휴식 결정 `msg_ac1ed6a996ab`에 따라 신규 Opus를 열지 않았고 PR·CI·병합도 미실행이다. 현재 수정 산출물과 독립 판정 미완료를 구분해 아래 재개 지점에 남긴다.

## 정본 반영 전 적용 중인 사용자 결정

2026-10-03의 적용 결정을 이 절 한 곳에서 찾는다. 사용자 발언은 모두 메인이 전달한 것이며 사용자 직접 입력으로 격상하지 않는다. 아래 인용은 표시한 Orca 메시지 원문의 일부이고 외부 저자의 직접 인용이 아니다. 사용자 결정과 메인 결정을 구분한다. 이미 적용할 운영 결정과 후속 정본·도구의 구현 완료는 다르며, 운영 규칙 PR 병합 뒤 이 절은 해당 정본 링크로 바꾼다. 원문 보존 파일은 Rules worktree의 로컬 근거다.

### 하네스 원칙과 적용 시점

메인이 전달한 사용자 결정 · msg_c9bc79f8ec43 · 2026-10-03T10:26:02Z

> 반복되는 규칙은 글이 아니라 검사로 바꾼다.

채택한 다섯 원칙은 반복 규칙의 검사화와 수리 안내, 정본 helper를 쓰는 가장 쉬운 경로, 수기 경로의 존재 드리프트 검사, warning 파일럿→실측→error 승격과 실행 불가/위반 구분, 구조 변경과 동작 변경의 별도 커밋이다. 진행 중인 단계는 마치고 다음 작업 계약부터 승인 범위 안에서 적용한다. 현행 주석 정책을 유지하며 검증 강도 차등·작은 작업 예외·규칙 가지치기·사람용 따라읽기는 미결이다.

원문: `.backups/planning/harness-principles/main-request.json`.

### DB 우선과 완료 근거

메인이 전달한 사용자 결정 · msg_c9bc79f8ec43 · 2026-10-03T10:26:02Z

> 속도보다 완성도다.

DB 저장소·연동이 게임 콘텐츠의 선행 관문이다. 다른 파트는 GameDev DB 경로의 파일 소유와 순서를 조율한다. 독립 검증·실제 실행 근거·사람 가독성 검토를 줄여 일정을 당기지 않으며, 이전 실행을 이번 실적으로 복사하지 않는다.

원문: `.backups/planning/harness-principles/main-request.json`.

### 목표 종료 Gardener 4주 파일럿

메인이 전달한 사용자 결정 · msg_c9bc79f8ec43 · 2026-10-03T10:26:02Z

> 후보 채택은 메인을 거쳐 사용자가 정한다.

각 goal의 PR 병합·결과 기록 후 R-8 직전에 신규 claude-opus-5-5를 읽기 전용으로 연다. 보고서 하나만 쓰고 결함·CI 실패·억제·우회·드리프트의 담당 범위에서 최대 두 정리 후보와 검사화 방법을 제안한다. 직접 수정하지 않는다. 2026-10-31 무렵 비용·잡음으로 지속 여부를 평가한다.

원문: `.backups/planning/harness-principles/main-request.json`.

### 미제출 입력과 공식 계약 draft

메인이 전달한 사용자 결정 · msg_ae8e98ad1d0d · 2026-10-03T06:38:46Z

> 사용자 직접 지시는 Enter로 제출돼 대화 기록에 들어간 표식 없는 입력뿐이다

입력창 draft·추천 프롬프트·ghost text를 지시로 실행하거나 그것만으로 pane 종료를 보류하지 않는다. 공식 계약의 미제출 주입은 사용자 입력과 구분해 복구한다. 아래 draft 복구 보강과 함께 적용하며 일반 사용자 draft를 대신 제출하지 않는다.

원문: `.backups/verification/2026-10-02-agent-rule-context/resume-2026-10-03/main-draft-input-guidance.json`.

### 확정 실패 세 번 뒤 새 Sol과 Fable

메인이 전달한 사용자 결정 · msg_22a9b4109ee7 · 2026-10-03T07:08:40Z

> 같은 산출물의 FAILED와 NOT PASS를 이중으로 세지 않는다.

같은 계약·결함 번호의 Sol FAILED 또는 독립 NOT PASS 3회 확정 시 적용한다. 조사 전용과 개발 중 자체 smoke 수리는 제외한다. 실패 세션 정산·종료 후 새 gpt-6.1-sol xhigh와 새 읽기 전용 claude-fable-5-1 Advisor를 연다. Advisor는 조언 파일 하나만 쓰고 Sol이 구현 전 직접 질문해 채택·기각 이유를 남긴다. Astra 직접 구현은 금지다. 기동 전에 실패 원문 세 건과 이유를 메인 status로 보고하되 재승인을 기다리지 않는다. 네 번째도 실패하면 다섯 번째 전에 메인 question이 필요하다. 선택창·모델 부재는 R-5/R-6대로 보고한다.

원문: `.backups/verification/2026-10-02-agent-rule-context/resume-2026-10-03/main-three-failure-final-decision.json`.

### 내용 없는 heartbeat 예외와 파트별 PS 정리

메인이 전달한 사용자 결정 · msg_8639aeaf7a12 · 2026-10-03T08:32:32Z

> 내용 없는 생존 신호 heartbeat에는 subject·body 태그를 요구하지 않는다.

빈 생존 신호만 from_handle/taskId/dispatchId 일치로 수신하고 교정 메시지를 보내지 않는다. 내용 있는 지시·보고·질문·완료에는 태그가 필수다. 같은 결정의 PS 정리는 각 파트가 다음 목표 첫 구조 커밋에서 담당 파일을 기계적으로 정리하며 git diff -w 무차이와 해당 All PS 진단으로 검증한다. 289건은 과거 실행 관측이지 현재 고정 기대값이 아니다. BACKLOG를 첫 별도 PR로 만들고 예정 목표를 중복 등록하지 않는다.

원문: `.backups/verification/2026-10-02-agent-rule-context/resume-2026-10-03/main-first-replan-decision.json`.

### Gardener 채택 후보의 경계

메인이 전달한 사용자 결정 · msg_d5453356cc75 · 2026-10-03T10:17:49Z

> 사용처 없는 선행 제작을 하지 않는다.

npm engines 경고 노출은 warning 파일럿 후보로 기록한다. npm 버전·EBADENGINE annotation/요약·경고 포함/미포함 fixture를 검증하고 해소·error 승격은 실측 뒤 사용자 판단, engines는 Management 조율이다. PowerShell 결과 객체 [ordered]와 같은 fixture 두 번의 stdout 바이트 대조는 이번 goal의 후속 구현 범위다. 집계·비교 helper는 인용 helper 후보에 합친다. 이는 채택·범위 결정이며 구현 완료를 뜻하지 않는다.

원문: `.backups/verification/2026-10-02-agent-rule-context/resume-2026-10-03/main-gardener-candidate-decision.json`.

### goal 범위와 루프 안팎

메인이 전달한 사용자 결정 · msg_238982aa5d26 · 2026-10-03T11:00:22Z

> goal 범위는 계획 때 확정한다. goal에는 완료조건, PR 경계, 범위 밖 목록을 둔다.

완료조건에 필요한 결함, 이번 변경이 만든 테스트·빌드·CI 실패, 계약·규칙 위반, 그 결함에 한정한 재발 방지는 루프 안에서 해결한다. 새 기능·화면·도구·검사·디자인·다른 영역 정리와 새 요청의 기본값은 다음 계획이다. goal 보류 목록에 한 줄로 남기며 승인 요청을 남발하지 않는다. 사용자가 이번 goal 포함을 명시하면 메인이 다시 계획한다. 무관한 기존 실패는 분리해 보고한다. 애매하면 메인에 범위 판정을 묻고 기본은 범위 밖이다. 완료조건 변경이 필요하면 해당 진행을 멈춰 메인에 올린다. Rules legacy 통합·삭제·보관은 다음 계획으로 이관됐다.

원문: `.backups/verification/2026-10-03-harness-principles/main-scope-principle.json`.

### 규칙 정본 이관과 점검 지점

메인이 전달한 사용자 결정 · msg_168d9aa35710 · 2026-10-03T11:14:25Z

> goal이 끝나면 그 파트는 다음 goal을 자동으로 시작하지 않는다.

메인과 사용자가 결과·남은 위험·BACKLOG·다음 계획을 점검한 뒤 재개한다. 계획에 기본 종료 점검과 필요 중간 점검을 적고 PR 줄 수 상한은 두지 않는다. 점검 사이 새 아이디어는 BACKLOG에 둔다. Rules 중간 점검은 첫 BACKLOG PR 병합 뒤다. 메모리/임시 자료는 사본·hash를 보존하고 Sol이 각 원문을 직접 대조해 공용 정본에 이관한다. E 후보는 다음 PR, D 메인 행동은 Sol 종료 뒤 메인이 CLAUDE.md에 쓰고 신규 Opus가 함께 실사한다. 메모리 정리는 병합 뒤 메인 소유다.

원문: `.backups/verification/2026-10-03-harness-principles/main-rules-migration-request.json`.

### capacity 재시도와 모델 전환 한정 예외

메인이 전달한 사용자 결정 — 지금부터 적용 · msg_bc5d8b721551 · 2026-10-03T11:15:30Z

> 예외는 Sol에서 Astra 모델로 바꾸는 경우 하나뿐이다.

Selected model is at capacity를 관측하면 같은 세션/task에서 1→2→5→10분 간격으로 재시도한다. gpt-6.1-sol이 최초 관측 후 누적30분에도 계속 실패하면 기존 작업자를 정산·종료하고 새 gpt-6-astra xhigh 작업자를 열 수 있다. 실행 중 모델 변경과 리드 Astra의 직접 구현은 금지다. Opus는 재시도만 하며 대체하지 않는다. 요청/관측/실제 또는 unknown 모델, 사유와 이 결정 근거를 기록한다. 정본 PR 병합 전에도 적용한다.

원문: `.backups/verification/2026-10-03-harness-principles/backlog-review-and-new-decisions.json`.

### 연결 goal의 마일스톤 승격

메인이 전달한 사용자 결정 · msg_39d7be6b2eb9 · 2026-10-03T11:16:48Z

> 계획할 때 두 개 이상의 goal로 이어져야 끝나는 안건이면 마일스톤 로드맵을 만들거나 갱신한다.

각 goal은 roadmap을 링크하고 roadmap은 순서·선행조건·goal 링크만 둔다. 진행률·검증·결정은 goal에만 둔다. goal 종료 점검 때 담당 Astra가 다음 단계·의존성·달라진 순서의 초안을 내고 사용자와 확인한 뒤 갱신하고 다음 goal을 고른다. 새 이름은 목적 중심이며 역사 코드를 무관하게 일괄 변경하지 않는다. Management 위치는 해당 Astra와 조율한다. 현재 goal에 실제 로드맵 작성을 끼워 넣지 않고, 정본 병합 전 점검이 와도 메인이 이 결정으로 진행한다.

원문: `.backups/verification/2026-10-03-harness-principles/backlog-review-and-new-decisions.json`.

### 휴식과 재개 기록

메인이 전달한 사용자 결정 · msg_ac1ed6a996ab · 2026-10-03T12:06:53Z

> 지금부터 새 작업자를 발행하지 않는다.

현재 작업자는 맡은 단위와 보고를 마치거나 20분 이상 추가 소요가 예상되면 쓰기 한 단위가 끝난 안전 지점에서 부분 결과를 남긴다. 미완료를 통과로 세지 않고 정상 정산·종료한다. 변경을 되돌리지 않는다. goal의 재개 기록만 별도 커밋하고 원격 branch가 이미 있을 때만 백업 push한다. CURRENT/RESUME/Rules 적용결정 절은 이번 명시 위임에 따라 Astra가 직접 작성하고 BACKLOG PR에 포함한다. 지금 새 실사는 열지 않고 사용자 재개 뒤 신규 Opus가 함께 실사한다.

원문: `.backups/verification/2026-10-03-harness-principles/main-pause-request.json`.

### Rules 착수 보정과 운영 개선 입력

메인 범위·운영 결정 · msg_c1412c982ac5 · 2026-10-03T10:39:09Z

> 개선 입력 4건의 추천 방향은 모두 동의한다.

All 비교·승격·인용/집계 helper는 세 파트 PS 정리 병합 뒤의 한 BACKLOG 후보로 분리한다. ADR-035는 GameDev 첫 SQL 구조 PR 병합 뒤 사실 대조 후 작성한다. PR 경계는 사람이 읽는 변경 책임으로 판단한다. 기계적 이관도 사전 맥락을 남기고, PS warning 표현과 checker/CI exit를 실측으로 구분하며 과거 실패를 소급해 성공으로 쓰지 않는다. 공식 draft 복구는 JSON draft와 payload를 대조하고 Enter만 한 번 보낸 뒤 관측한다. Run 바인딩 직후 메인에 run 주소를 알려 term 우편 누락을 피한다. 외부 개인 CLI는 참고이고 저장소 실적이 아니다.

원문: `.backups/verification/2026-10-03-harness-principles/main-goal-approval.json`.

### 공식 draft 복구의 수행 주체 보강

메인 운영 결정 · msg_b10d232dce1b · 2026-10-03T11:29:24Z

> 복구를 담당 Astra가 직접 할 수 있게 정하라.

새 pane에 다른 입력이 없고 draft가 공식 계약 크기와 맞는 붙여넣기 placeholder일 때 담당 Astra의 Enter 단독 제출 절차를 운영 PR에 정식화한다. 조건이 맞지 않으면 메인에 올린다. accepted 뒤 침묵만으로 새 텍스트를 보내는 것과 구분한다. 세 번째 Management 관측 msg_19ca70e05ed8/msg_7276a1181c81은 메인 전달 사실이며 Rules의 재현 실적이 아니다.

원문: `.backups/verification/2026-10-03-harness-principles/main-draft-recovery-third-observation.json`.

### 기존 테스트 대량 실패의 분류

메인 결정 — 사용자 결정 아님 · msg_71e41e231d55 · 2026-10-03T12:07:57Z

> 실패한 기존 테스트는 하나도 빠짐없이 분류한다.

GameDev msg_bd6ae56c412e와 Management msg_6e86b93bc63e에 보낸 원문을 재전달받았다. 전 실패를 (a) 옛 구현 세부 단정→요구사항 근거로 수정, (b) fixture/환경 누락→빠진 입력만 보완, (c) 실제 회귀→제품 결함 번호, (d) 원인 미확정→통과로 바꾸지 않음으로 분류한다. 판정 표에는 테스트 이름/이전 단정/새 단정/요구사항 출처 경로·줄 또는 msg id를 둔다. 제품과 같은 계산으로 기대값을 만들거나 기대값 일괄 재생성·완화는 금지다. 전후 같은 명령의 통과·실패 수와 원시를 남긴다. 메인은 승인 요청 전 표본을 원문과 대조한다. 해당 Opus 계약에 원문 그대로 넣는다.

원문: `.backups/verification/2026-10-03-harness-principles/main-test-classification-decision.json`.

### BACKLOG 출처 전수 대조와 같은 부류 재발

메인 결정 — 이번 BACKLOG에 적용 · msg_002afaca32e2 · 2026-10-03T11:55:41Z

> 표본 대조는 안 된다.

수정 Sol과 신규 Opus 모두 12행 전체 출처 열을 원문과 대조하고 실제 대조행수를 남긴다. Sol의 실제 수정은 계약 셀로 제한하며 다른 불일치는 모두 보고한다. 같은 부류의 원결정 ID/재전달 누락이 또 나오면 행이 달라도 같은 부류 재실패로 메인에 보고한다. 이를 일반 운영규칙으로 승격할지는 후속 운영 PR에서 검토하며, 현재 모든 결함 집계 규칙을 임의 교체하지 않는다.

원문: `.backups/verification/2026-10-03-harness-principles/main-source-full-audit.json`.



## 재개 지점

**사용자 휴식으로 대기 중이며 goal 완료가 아니다.** 메인 `msg_ac1ed6a996ab`의 휴식·재개기록 지시를 적용했다. 사용자 재개와 메인의 현재 권한 확인 전에는 새 작업자를 열지 않는다. 모든 과거 handle/Run/Task/Dispatch는 아래 수행 근거이며 새 세션 실행 권한이 아니다.

### 위치와 보존 상태

- 기록 시각: 2026-10-03T12:15:10Z (UTC). 작성자 `[Rules Astra]`.
- worktree: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`, branch `docs/harness-backlog`.
- 재개기록 작성 전 HEAD: `35aaa155d4a0852f0da6798635e7e93458094dc9`. base: `5616573c32a2b2e0b677bc21b75e22a08d21f285`(`origin/main`, goal 착수 때 최신 확인). 로컬 `main` ref는 오래돼 비교 기준으로 쓰지 않는다. 재개 때 remote main·HEAD·미커밋 상태를 다시 관측한다.
- 보존 커밋은 이 goal 한 파일만 담는 `docs: preserve rules pause checkpoint`다. 자기 commit SHA는 파일 안에 순환 기록하지 않고 `.backups/verification/2026-10-03-harness-principles/resume-final-state.json`과 메인 회신에 기록한다. `git log -1 --format=%H --grep='docs: preserve rules pause checkpoint'`로 찾을 수 있다. 이전 보존 커밋 `110dd8f`와 계획 커밋 `35aaa15`도 그대로다.
- 원격 `docs/harness-backlog`는 보존 직전 `git ls-remote`에서 없음을 확인했다. 따라서 이번에는 로컬 커밋만 하며 push하지 않는다. PR은 아직 없고 CI·병합도 수행하지 않았다.

제품/진입 문서는 커밋하지 않고 아래 상태로 보존한다. SHA256은 작업 파일 바이트 기준이며 이 goal은 별도 보존 커밋 대상이다.

| 미커밋 파일 | 변경·소유 | SHA256 |
|---|---|---|
| `00_Document/operations/BACKLOG.md` | untracked, Sol 작성·수리, 12후보와 출처 | `C39FB604DBD1BC8743332814D15EF1DF3117ACD2504D6E5B2D9D4D06AA95E952` |
| `00_Document/INDEX.md` | tracked 수정, 최초 Sol의 BACKLOG 링크1행 | `CB4F24C5ADB6913466623A472FA41FB29C6642300238FB5CE5CF43553E794F46` |
| `00_Document/operations/CURRENT.md` | tracked 수정, 휴식 지시로 Astra가 네파트 goal·worktree·branch 진입 갱신 | `70379CD0EE5A60B3D1EC6702B70B87E70EBC1390F617DE30748C9CA7977E48EB` |
| `00_Document/operations/RESUME.md` | tracked 수정, Astra가 다음 조각 진입을 goal 재개절·점검·적용결정 링크로 교체 | `13AA2067FFA46C855D72DDDDC5F9F23F224FAA59339E8B17CF0AF6D5617D5CBA` |

CURRENT의 네 goal은 각 소유 worktree에서 실재함을 확인했다(`resume-placement.json`). GameDev·Architecture·Management goal은 아직 Rules checkout에 없다. 상대 링크를 따라 파일이 없으면 CURRENT의 해당 worktree에서 읽으며, 다른 파트의 미병합 goal을 이 branch로 임의 복제하지 않는다. Architecture 실제 branch는 `feat/codegraph-adapter-cleanup-20261003`이며 메인 최초 안내의 예전 branch와 다르다.

### 종료한 작업자와 판정

로컬 근거 루트는 `.backups/verification/2026-10-03-harness-principles/`다. 아래의 경로는 이 근거 루트 기준이며 산출물은 모두 보존됐다.

| 작업 | Task / Dispatch | 최종 상태·근거 |
|---|---|---|
| 최초 구현 | `task_8e2401deae8a` / `ctx_ff14d925ab15` | 완료·정산·close. `backlog-implementation/report.md`, `backlog-sol-*` |
| 첫 독립 실사 | `task_d17b411e8f9c` / `ctx_bed20675a036` | 실사 완료·NOT PASS #1·정산·close. `backlog-verification/verdict.md` |
| #1 수정 | `task_b80846a2326c` / `ctx_2c7e5a2ac137` | 수정 완료·정산·close. `backlog-repair/report.md`, `backlog-repair-sol-*` |
| 재검증1 | `task_2a8271b7a11f` / `ctx_a9477fdbbee9` | 실사 완료·#1해소/#2 NOT PASS·정산·close. `backlog-reverification/verdict.md` SHA256 `AF109EA7A95F24EA2468983698FC5AFAF9F3F838F514D350D30BAA00489193FB` |
| #2 수정 | `task_00610ce2e8a4` / `ctx_bd4996c470a4` | 완료·정산·close. `msg_0db5bd89c49a`(12:09:28Z), `backlog-repair2/report.md`와 `self-check.json`. 출처3셀만 수정, 12행 전수 대조에서 추가 누락 없음, 비대상 바이트/읽기 전용20입력 보존을 자체 확인. 독립 PASS 아님 |
| 다음 독립 재검증 | 미발행 — Task/Dispatch 없음 | 휴식 지시로 미착수. 새 Opus에 발행할 계약 초안 `backlog-reverification2-contract.md` |

#2 Sol 최초명령은 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`, 화면은 GPT-6.1-Sol xhigh, backend unknown. 마지막 incarnation `41e3207b-d1cb-4049-9fe5-0bc575aa4200`을 재확인한 뒤 close했고 `ptyKilled=true`다. 보고와 실제3셀 diff를 Astra가 읽었지만 독립 재검증을 대신하지 않는다. 지금 실행 중인 Rules 작업자는 없다.

- **열린 결함: #2 한 건.** 수정 완료·독립 확인 대기이며 #2의 수정 후 재검증은 **0회**다. #1은 **재검증1회로 해소**됐다. 전체 실사는 최초1회+재검증1회, NOT PASS2회다.
- 원결정 ID·재전달 누락이라는 같은 부류는 두 차례 검출됐다. 메인 `msg_002afaca32e2`에 따라 다음에 행이 달라도 같은 부류가 나오면 재실패로 메인에 보고한다. 행별 새 번호만으로 별개 문제로 처리하지 않는다.
- 비차단 N1~N6 및 R1~R4는 두 판정 원문에 남아 있다. 이번 수리 범위로 확대하지 않는다. R4의 지금 적용/문서화 계획 구분은 위 적용 결정 절에서 명시했다.

### 검증 입력과 재개 첫 단계

- 첫 실사의 `verification-inputs.json`, scope 보정 `verification-inputs-scope-update.json`, 재검증1 `reverification-inputs.json`은 역사 고정본이다. 현재 입력이라고 덮어쓰지 않는다.
- 재개용 고정본은 **`reverification2-inputs.json`**이다. #2 수리 뒤 BACKLOG·INDEX·CURRENT·RESUME·두 goal·관련 계약/원문·작성자 근거를 포함하며 실제 HEAD/파일 SHA는 `resume-final-state.json`에도 있다. 재개 때 현재 hash와 먼저 대조하고 변경이 있으면 원인·소유를 확인한 새 manifest를 만든다.
- **재개 첫 단계:** 사용자 재개 및 메인의 현재 세션/권한 확인 → 실제 Git·hash 대조 → 신규 `claude-opus-5-5` 세션에 **`backlog-reverification2-contract.md`**와 고정본을 전달해 독립 재검증을 발행한다. 옛 검증자를 재사용하지 않는다. 새 Astra라면 R-5/R-6 준비 확인과 새 Run 회신 주소 통지부터 한다.
- 판정 대상은 #1 해소 보존, #2 수리, **12행 전체 출처 열 전수 대조와 실제 대조행수**, INDEX 진입, 이전 goal 보존, 이번 CURRENT/RESUME/goal 재개기록이다. 외부 문헌 내용·제품 실행·게임·DB·Unity·CI는 이번 문서 실사로 통과 처리하지 않는다. `msg_acba01cbf81e`는 새 계약이 전달한 간접 출처이고 독립 원문은 미열람이다.
- 독립 PASS 후 담당 Astra가 제품 문서를 커밋·push하고 PR을 만든다. 실제 PR CI를 확인한 뒤 메인에 변경·리스크·판정 원문·정확 SHA를 보고한다. 사용자에게 **해당 PR의 병합 승인**을 받은 뒤에만 병합한다. 승인 후 중간 점검을 거쳐 기존 네 PR 계획의 후속 운영규칙 단위로 간다. 현재 휴식이 그 점검을 자동 대체하지 않는다.

### 적용 결정·메인 확인·남은 결정

오늘의 정본 반영 전 운영 기준은 위 **[정본 반영 전 적용 중인 사용자 결정](#정본-반영-전-적용-중인-사용자-결정)**에 원문 인용·msg id·원문 보존 위치를 모았다. scope `msg_238982aa5d26`, 점검/자동착수금지 `msg_168d9aa35710`, capacity `msg_bc5d8b721551`, 마일스톤 `msg_39d7be6b2eb9`는 사용자 결정의 메인 전달이다. 테스트 분류 `msg_71e41e231d55`와 BACKLOG 전수 대조 `msg_002afaca32e2`는 메인 결정이다. 정본 문서와 규칙 파일의 정식 반영은 후속 운영규칙 PR에서 하며 지금 CLAUDE.md는 쓰지 않았다.

메인은 재개 뒤 신규 판정 전문을 읽고, 병합 승인 요청 전 R-2 형식으로 원문 메시지↔12행 출처표·실제 diff/원시 근거의 표본을 독립 대조한다. 이 메인 표본 대조는 Opus의 12행 전수 대조를 대신하지 않는다. GameDev/Management는 테스트 분류 표의 이전/새 단정·요구사항 출처·전후 명령 수치를 별도로 표본 대조한다. CURRENT 네파트 실경로와 미병합 goal 경계, 적용 결정의 사용자/메인 구분도 확인한다.

남은 사용자 결정은 이 BACKLOG PR의 병합 승인, 계획된 중간 점검 뒤 후속 진행, 그리고 별도 후보인 검증 강도 차등·작은 작업 예외/규칙 가지치기/사람용 따라읽기 정책이다. workflow lint 도구 도입과 npm 해소·PS error 승격은 해당 미래 후보의 조건이며 현재 BACKLOG 문서 완료를 막는 선행 승인이 아니다. Management의 125% 확인은 그 파트 goal의 경계다. 다음 goal을 자동 시작하지 않는다.
