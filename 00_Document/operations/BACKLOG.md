# 목표 전 후보

[CURRENT](CURRENT.md)는 진행 중 goal 링크, BACKLOG는 goal로 승격되기 전 후보, 각 `goal.md`는 세부 기준·상태·결과의 정본이다. 여기에는 후보의 처분과 승격 링크만 남기며 진행 상태 장부를 따로 만들지 않는다. 후보 등록이나 선행 조건 충족만으로 채택·착수·담당 확정 권한이 생기지 않는다.

## 항목 계약

| 필드 | 기록 기준 |
|---|---|
| ID | 목적을 나타내는 안정적인 소문자 문자열. 날짜·마일스톤 코드·작업자 이름을 붙이지 않으며 제목·담당 변경만으로 바꾸지 않는다. |
| 제목 | 검토할 작업이나 정책의 이름 |
| 이유 | 후보로 남기는 문제·필요 |
| 출처 | 누가·언제 전달했는지와 메시지 ID. 재전달은 그 관계를 표시한다. |
| 선행 조건 | 승격 검토 전 충족할 의존성·측정·사용자 판단 |
| 담당 후보 | 소유 조율 대상이며 확정 배정이 아님 |
| 상태 | 아래 세 값 중 하나 |

상태는 `대기`, `goal 승격 → 링크`, `폐기 + 이유`로 제한한다. 승격 시 실제 `goal.md` 링크를, 폐기 시 이유와 결정 출처를 기록한다. 승격한 작업의 진행·검증·완료 결과는 링크된 goal에서 관리한다. 현재 처분은 각 행의 상태·결정 출처에서 확인한다. 최초 등록 후보의 ID·출처·조건·7필드는 보존한다.

## 후보

씨앗의 근거는 [하네스 원칙 목표](../../01_Phases/goals/2026-10-03-harness-principles/goal.md)의 첫 BACKLOG 표와 **메인이 Rules Astra에게 전달한 결정**이다. `msg_c9bc79f8ec43`는 2026-10-03T10:26:02Z, 최신 분리 결정 `msg_c1412c982ac5`는 2026-10-03T10:39:09Z의 메인 전달이다. 저장된 메시지와 계약을 근거로 하며 이 문서 작성자가 사용자에게 직접 수신한 지시로 표현하지 않는다. 과거 메시지 원문을 보지 않은 경우에는 재전달 근거만 적는다.

| ID | 제목 | 이유 | 출처(누가·언제·메시지 ID) | 선행 조건 | 담당 후보 | 상태 |
|---|---|---|---|---|---|---|
| `diagram-design` | 도식 디자인 개선 | 설명 그림의 개선 요청 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 구체 화면·불편 확인과 소유 조율 | Rules·Management 협의 | 대기 |
| `report-response-format` | ASD-STE100·HTML 답변 방식 | 보고 방식 탐색 요청 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 프로젝트 적합성 확인과 도입 여부 사용자 판단 | Rules | 대기 |
| `goal-loop-improvement` | 작업 루프 자기 개선·그래프 설계 | 반복 작업의 개선 구조 검토 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 현 루프의 실제 수행 근거 대조 | Rules | 대기 |
| `powershell-all-evidence` | 파트 PS 정리 후 All 비교·승격 제안과 문서 근거 인용/집계 비교 helper | 세 파트 정리 결과를 원시 근거와 대조하고 PS 정책을 판단 | 메인 전달·2026-10-03·`msg_8639aeaf7a12`(08:32:32Z, 인용 helper 채택)·`msg_d5453356cc75`(10:17:49Z, 집계 helper 통합)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달; `msg_c1412c982ac5`(10:39:09Z, 이번 목표에서 All/helper를 백로그로 분리); 메인 전달 `msg_7bfc0ca278f5`(2026-10-04T17:11:41Z), Gardener2 후보1의 문서 인용 결함 2건·사람 검출 관측을 기존 인용검사 근거에 연결 | **촉발 조건: 세 파트(GameDev·Architecture·Management) PS 정리 병합 완료.** 이후 별도 goal 승격 검토 | Rules | 대기 |
| `workflow-definition-lint` | workflow 정의 lint | workflow 정의 오류 조기 검출; 전달된 [이전 목표의 결함 #4](../../01_Phases/goals/2026-10-02-agent-rule-context/goal.md) | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | actionlint류 도구 도입 사용자 승인 | Rules | 대기 |
| `verification-depth-policy` | 검증 강도 차등·작은 작업 예외 | 원래 등록 이유: 검증 범위 차등의 미결 사용자 판단. 현재 처분: 승인된 검증 강도 시범의 goal 연결 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 등록 시 조건: 메인→사용자 정책 결정; 현 독립 검증 유지. 현재 결정: [승인 근거](../../01_Phases/goals/2026-10-05-operating-canon/goal.md#요구사항-원천과-적용-결정)·[검증 강도 시범 정본](../../.agents/skills/dawnholder-goal-loop/SKILL.md#검증-강도-4주-시범) | 메인→사용자 | goal 승격 → [운영 정본 반영](../../01_Phases/goals/2026-10-05-operating-canon/goal.md) |
| `rule-document-pruning` | 규칙 문서 가지치기 | 규칙 유지 범위의 미결 사용자 판단; 계획 26의 ORCA 줄·스킬 bytes 증가와 가지치기 미반영 관측을 연결(이번 정본의 현재 수치가 아님) | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달; 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 26(76행), [PR2 원천](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#pr2-canon-sources) | 기존 명시 정비와 구분해 메인→사용자 정책 결정 | 메인→사용자 | 대기 |
| `human-code-walkthrough` | 사람용 코드 따라읽기 문서 | 사람용 탐색 문서 도입의 미결 사용자 판단 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 목적·유지비와 도입 여부를 메인→사용자가 결정 | 메인→사용자 | 대기 |
| `post-db-static-analysis` | DB 뒤 정적 분석기 단계 | 유지보수성 후속 강화; 계획 30의 SQL·TS·Python 정적 검사 공백 후보를 연결 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달; 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 30(80행), [PR2 원천](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#pr2-canon-sources) | DB 연동 뒤, 대상·담당 소유 조율 | 관련 영역 소유자와 조율 필요 | 대기 |
| `npm-engine-warning` | CI npm engines 경고 노출 | 메인이 전달한 EBADENGINE 경고의 요약 누락 문제 | 메인 전달·2026-10-03·`msg_c9bc79f8ec43`가 재전달한 `msg_d5453356cc75` | warning 파일럿·npm 버전 증거·경고 포함/미포함 fixture·실측, Management engines 조율; 해소/승격은 사용자 판단 | Rules·Management | goal 승격 → [CI npm 경고와 운영 후속 정본화](../../01_Phases/goals/2026-10-05-ci-warning-operating-followup/goal.md) |
| `server-operations-view` | 운영툴 서버 운영 시각화 | 사용자 2026-10-03 요청을 메인이 전달 | 메인 전달·2026-10-03·`msg_c9bc79f8ec43`가 재전달한 `msg_acba01cbf81e`; 별도 목표 유지 승인 `msg_1da9a73ad0d2`와 [좁은 실시간 후보·Core 계약 근거](../../05_Management/goals/2026-10-05-development-record-navigation/goal.md#후속-후보와-서버-운영-시각화-근거) | DB 연동 뒤, 참고 출처는 구현 조사 때 확인 | Management | 대기 |
| `work-status-view` | 운영툴 작업 현황 화면 | 작업 중·예정·결정 대기를 상시 보는 화면 요청 | 메인 전달·2026-10-03·`msg_c9bc79f8ec43`, 개인 CLI 참고 허용 `msg_c1412c982ac5` | 표시 범위·소유 조율과 별도 goal 승격 검토 | Management | 폐기 + 사용자 결정 「운영툴 「작업 현황」 화면 후보를 폐기할지 → A 폐기」; 메인 전달 `msg_a1fe33623cbe`·[다음 goal 사전 결정](../../01_Phases/goals/2026-10-05-operating-canon/goal.md#다음-goal-사전-결정) |
| `management-e2e-input` | 운영툴 E2E 입력 — 앱 내부 입력 시범(Electron debugger CDP, 포커스 흉내, showInactive) + Hyper-V 가상 머신 준비 계획 | OS 입력 없는 보조 모니터 E2E 조작의 다음 계획 보존; Management 브랜치에 BACKLOG가 없다는 메인 설명에 따라 Rules가 main 기준 운영 문서 한 곳에 반영해 checkout 간 동시 쓰기 방지 | 메인 Claude 재전달·2026-10-04T09:36:50Z·`msg_4ac97f9a40b4`; 이 메시지가 메인 전달 `msg_d265a8b08d4b`·Management goal 기록 `msg_7b16a80549b1`을 재전달(두 원메시지 직접 실사 안 함); 메인이 전달한 사용자 대시보드 결정 응답(Enter 제출) 원문 「2) 보조 모니터 - OS 입력 없이 E2E 조작하는 방법 → B A + 가상 머신 준비」 | Management 현재 목표 종료 뒤 별도 goal 승격 검토; 후보 등록은 E2E/VM 구현 착수·설치 허가 아님 | Management | 대기 |

## 영속 통합 goal에서 연결한 후보

[실제 SQL 설치·엔진 판정 goal](../../01_Phases/goals/2026-10-04-persistence-integration/goal.md)에서 메인 전달에 따라 남긴 후속 후보다. 등록은 이번 G2 범위의 구현이나 새 goal 착수를 뜻하지 않는다.

| ID | 제목 | 이유 | 출처(누가·언제·메시지 ID) | 선행 조건 | 담당 후보 | 상태 |
|---|---|---|---|---|---|---|
| `powershell-dotnet-boundary-checklist` | PowerShell과 .NET 경계 체크리스트 | builder indexer·`[NullString]`·예외 체인 관련 실패에서 경계 확인 항목을 검토 | 메인 전달·2026-10-05T05:55:03Z·`msg_d57b6b7d08d2`, Fable 읽기 전용 조사 요약을 바탕으로 후보만 기록 지시 | 실제 원천 재확인·DB 단계 뒤 별도 범위와 사용자 채택 판단 | GameDev | 대기 |
| `database-allowlist-drift-test` | DB allowlist 드리프트 probe의 정식 테스트 승격 | 검증자가 사용한 AST probe의 유지·회귀 검사 가치 검토 | 메인 전달·2026-10-05T05:55:03Z·`msg_d57b6b7d08d2`, 현재 TESTDB·INSTALL goal 범위에는 넣지 않음 | probe 원문·범위·오탐 및 기존 테스트 중복 대조 뒤 별도 goal 판단 | GameDev | 대기 |
| `combat-smoke-timeout-investigation` | CombatSmoke_ZeroLag_Succeeds 간헐 timeout 조사 | 서버 제품 diff가 없다고 전달된 두 CI에서 15초 timeout. 원인은 미확정이며 flake·회귀로 단정하지 않음; 계획 34의 같은 조사 후보를 연결 | 메인 전달·2026-10-05T06:53:29Z·`msg_65740b4635b6`; 아래 두 사례와 Content 원시 경로는 재전달 근거; 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 34(84행), [PR2 원천](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#pr2-canon-sources) | G2·첫 PR 뒤 사용자와 조사 착수·범위를 결정. 후보 등록만으로 테스트 변경·서버 조사에 착수하지 않음 | GameDev | 대기 |

`combat-smoke-timeout-investigation`의 메인 전달 사례(원시 직접 미열람): PR179 Management는 서버 diff 0인 attempt1에서 실패하고 같은 head 재실행은 통과했으며 직전 head는 12초 통과했다. run ID는 이번 전달에서 미제공이다. PR180 Content는 새 head `3ee38bf`(최근 merge 제품 diff 0이라고 전달됨)의 dotnet run `37272468313` attempt1에서 테스트 :59의 target1이 15초 안에 공격거리2로 수렴하지 못했다. 같은 제품 코드 `ce3267a`의 CI `37235164389`는 12초 통과했고, 전달 시점에는 같은 head 1회 재실행 중이었다.

메인은 해당 테스트와 EmergencyCombatSmoke 소스가 `11aa4b8` 이후 불변이라고 전달했다. 조사로 확인한 결론이 아니다. 원시 위치는 Content worktree `content-active/.backups/verification/2026-10-05-items-inventory-currency/pr1-main-sync-ci-*`다. G2 실사 중 수신했으며 입력 동결 해제 뒤 후보만 기록했다.

## 후보별 경계

`powershell-all-evidence`는 All 비교·승격 제안과 인용/집계 helper를 **한 후보**로 묶는다. 세 파트 PS 정리의 실제 병합 완료 뒤 별도 goal에서 최신 main을 측정한다. 이번 하네스 목표의 구현·종료 범위에는 포함하지 않으며, helper를 먼저 구현하지 않는다. 과거 PS289를 현재 잔여 수로 가정하거나 파트 회신만으로 All 재측정을 완료했다고 기록하지 않는다.

승격 검토의 근거에는 인용의 경로·줄·JSON Pointer·값 대조와 집계의 파일/규칙/파트 수·최소 줄·진단 identity 차이·소유 맵(존재 여부와 unknown)을 포함한다. 원시 인용 대조와 집계 생성의 책임을 나누고 의미 판정은 독립 Opus가 맡는다. 속성 순서만 다른 JSON·섞인 줄·소유 불명 경로 등 외부 관찰 가능한 fixture로 검증할 후보이며 아직 구현·실행한 결과가 아니다.

PR177 Gardener2의 [정리 후보1](../../.backups/verification/2026-10-04-teammate-onboarding/gardener2-report.md#정리-후보)은 `goal.md:51` 원본의 없는 관측 인용과 `sol-report.md:44`의 없는 label 인용 **2건**을 기록했고, 둘 다 사람 검토에서 검출했다. 메인이 전달한 사용자 A 결정은 이 원시 관측을 기존 인용검사 근거에 연결하는 것뿐이다. [전달 원문](../../.backups/verification/2026-10-05-operating-canon/main-gardener-decision.json)의 범위대로 기존 촉발 조건을 유지하고 새 helper·행·검사를 구현하지 않는다.

PS 정책 판단에서는 PSSA 진단 severity, checker 상태/exit, CI job 실패 여부를 따로 실측한다. 목표가 기록한 현행 선택 PS 진단의 exit1과 warning 파일럿의 충돌 지점을 확인하고, 이미 존재하는 차단 효과를 이름 변경만으로 새 성과로 기록하지 않는다. 기존 CI 완화나 새 error gate 적용은 메인→사용자 결정 뒤 별도 동작 변경으로 다룬다.

`npm-engine-warning`의 warning 파일럿 범위·실측·검증·PR 결과는 승격한 [CI npm 경고와 운영 후속 정본화 goal](../../01_Phases/goals/2026-10-05-ci-warning-operating-followup/goal.md)에서 확인한다. 이 처분은 engines 수정이나 경고 해소·error 승격 결정을 뜻하지 않는다.

`goal-loop-improvement`와 겹치는 진척 형식·Astra 갱신 책임은 [goal-loop 기준과 상태](../../.agents/skills/dawnholder-goal-loop/SKILL.md#기준과-상태)와 [이번 정본화의 요구 원천](../../01_Phases/goals/2026-10-05-ci-warning-operating-followup/goal.md#요구사항-원천과-적용-결정)으로 연결한다. 자기 개선·그래프 설계의 나머지 범위는 기존 후보의 `대기`를 유지하며 새 도구 채택·착수로 확대하지 않는다.

`verification-depth-policy`는 [운영 정본 반영](../../01_Phases/goals/2026-10-05-operating-canon/goal.md) goal로 승격했다. 승인된 [검증 강도 4주 시범](../../.agents/skills/dawnholder-goal-loop/SKILL.md#검증-강도-4주-시범)의 문서 실사·강/약 구분은 독립 세션을 생략하는 작은 작업 예외를 허용하지 않는다.

`rule-document-pruning`, `human-code-walkthrough`는 각각 별도의 미결 정책이다. 사용자 결정 전 현행 독립 검증과 CODE_CONVENTION의 주석 정책을 유지한다. 기존에 개별 승인된 문서 정비를 일반 가지치기 승인으로 확대하지 않는다.

`server-operations-view`의 [kciter 서버 모니터링 분석 가이드](https://kciter.so/posts/server-monitoring-analysis-guide/)는 사용자 전달 참고 URL이다. 본문은 읽지 않았으며 요약하거나 구현 근거로 확정하지 않는다.

폐기한 `work-status-view`의 등록 당시 참고인 `C:/Dev/DawnHolder_Dashboard`는 **저장소 밖 개인 CLI 참고**다. 메인이 작성한 Orca·gh 읽기 전용 조회와 메인 계획판을 참고 대상으로 전달받았다. Dawnholder 저장소 산출물이나 본 작업의 실행·검증 실적으로 기록하지 않는다.

## 중복 제외

[CodeGraph 정돈](../../01_Phases/goals/2026-10-03-codegraph-adapter-cleanup/goal.md#pr-174-병합과-goal-종료-점검)과 [모듈 경계 검사](../../01_Phases/goals/2026-10-05-module-boundary-warning/goal.md)는 각 goal의 기준·결과로 연결하고 새 후보로 중복 등록하지 않는다. Management 백로그 메뉴의 별도 목표 계획은 아래 [백로그 후속 연결](#백로그-후속-연결)에서 확인한다. 이 문서에 진행·완료 상태를 복제하거나 위 정적 분석기·작업 현황 후보의 처분을 다른 예정 작업의 착수·완료로 해석하지 않는다.

## 규칙 이관에서 연결한 후보

메인이 전달한 이관 inventory의 E절을 원천과 [현재 하네스 goal](../../01_Phases/goals/2026-10-03-harness-principles/goal.md#후속-운영-단위의-정본-이관과-점검-계획)에 대조해 기록한다. inventory 보존 원문은 `.backups/verification/2026-10-03-harness-principles/operating-rules/source-snapshots/rules-migration-inventory.md`, SHA256 `31ACAB31B5198DF37ACE2DB3EDAE96040C2B8848AF44DCF5D70463E468983A94`이고 메인 전달은 `msg_168d9aa35710`(2026-10-03T11:14:25Z)이다. 연결 임시 원문 replan-agenda와 msg-scope-principle도 직접 대조했다. 개별 사건 원시/검증 판정을 직접 확인하지 않은 후보는 그 한계와 재확인 조건을 적으며, 기록이 채택·착수 권한은 아니다.

| ID | 제목 | 이유 | 출처(누가·언제·메시지 ID) | 선행 조건 | 담당 후보 | 상태 |
|---|---|---|---|---|---|---|
| `registration-error-repair` | 도구 등록 오류의 수리 안내 | Architecture O2로 전달된 등록 누락 진단에 수정 방법이 없음 | 메인 전달 inventory E·`msg_168d9aa35710`(2026-10-03); replan-agenda의 Architecture O2 안건 직접 대조, 개별 판정 원시 미확인 | 해당 오류 원문/현재 동작 재확인·Formatting 코드 GameDev 소유 조율 | GameDev·Rules 협의 | 대기 |
| `contract-context-check` | 위임 계약과 사전 맥락 누락의 검사화 | 규칙 원문 누락·읽기 순서 이탈의 반복 여부 검토 | 메인 전달 inventory E·`msg_168d9aa35710`(2026-10-03); replan-agenda의 Management P-01 사례, 사건 원시 미확인; 메인 전달 `msg_7bfc0ca278f5`(2026-10-04T17:11:41Z), Gardener2 후보2의 helper 중복 2회·CRLF 거짓 불일치·메모 선행 이탈 1건·birthtime 재설정 관측 연결; 사용자 결정 2A·메인 전달 `msg_02077b4a4df3`(2026-10-07T11:58:55Z)·Management 넘김 `msg_299e90686a33`: [운영툴 기록 원본 일원화 Gardener 결과](../../05_Management/goals/2026-10-06-record-source-unification/goal.md#gardener-결과) 후보 1(쓰기 시각 추출 스크립트 6벌·판정 근거 정정 3건, 정본 읽기 전용 CLI·fixture 시험 제안), 같은 goal [PR202 좁힌 문서 실사](../../05_Management/goals/2026-10-06-record-source-unification/goal.md#pr202-좁힌-문서-실사) Z1(리드 메모 사후 작성, 근거 포함은 메인 `msg_53e10a4d10c3` 결정) | 현행 메모/원문 계약과 실제 이탈 근거 대조·검사 범위/오탐 검토 | Rules·해당 파트 | 대기 |
| `intermediate-commit-validation` | 중간 커밋의 보존·검증 확인 | GameDev 중간 tree가 단계별 검증 원칙과 달랐다는 안건 | 메인 전달 inventory E·`msg_168d9aa35710`(2026-10-03); replan-agenda 안건, 개별 중간 tree/실행 미확인 | 해당 SHA·보존 동작·실행 근거와 GameDev 계획 대조 | GameDev·Rules 협의 | 대기 |
| `goal-state-drift` | goal 상태 기록 형식과 드리프트 검사 | goal 산문 상태/시점 정정 중 다른 오류가 재발한 안건 | 메인 전달 inventory E·`msg_168d9aa35710`(2026-10-03); replan-agenda의 Architecture 문서 #1→#4 및 Rules Gardener 후속 관찰, 개별 실패 원시 미확인 | 다음 Gardener/현재 goal의 실제 상태 근거·형식/검사 비용 대조 | Rules·각 파트 | 대기 |
| `representative-platform-fixtures` | 대표 입력과 플랫폼 실행 근거 | PR166 Changed의 TS0건 통과 뒤 Linux JSON import 후보 오류 | 메인 전달 inventory E·`msg_168d9aa35710`(2026-10-03); replan-agenda 및 이전 맥락 goal의 PR167 실제 Linux/독립28회귀 근거·[PR167 실제 All 원천](../../01_Phases/goals/2026-10-02-agent-rule-context/goal.md#pr-167의-실제-저장소-linux-all-추가-검증)(과거 실행 근거이며 이번 실측 아님) | PR167에서 이미 고친 범위 제외·향후 대표 fixture/플랫폼 필요를 실제 결함에 한정 검토 | Rules·도구 소유자 | 대기 |
| `post-db-load-profiling` | DB 연동 뒤 부하 프로파일링 | 헤드리스 봇으로 실제 병목을 확인한 뒤 최적화할 후보 | 메인 전달 inventory E·`msg_168d9aa35710`(2026-10-03)만 보존; 별도 사용자/사건 원문 없음 | 영속화 완료·대표 부하와 원시 측정 방법·GameDev 범위 확인 | GameDev | 대기 |
| `legacy-document-consolidation` | legacy 문서 통합·보관 계획 | 현 하네스 goal에서 별도 계획으로 뺀 통합/삭제/보관 | 메인 전달 `msg_238982aa5d26`(2026-10-03T11:00:22Z), 최초 `msg_61d4c35695da`는 하네스 goal에 보존된 이관 관계로 연결 | 다음 계획에서 archive/link-map/tag·보존 경계와 정확한 파일 범위 재확정 | Rules | 대기 |

<a id="백로그-후속-연결"></a>

E절의 「다음 일」27개 정리·ID/goal 링크 드리프트는 [Management 시스템 카드 goal의 백로그 후속 계약](../../05_Management/goals/2026-10-02-system-cards/goal.md#후속-백로그--백로그-정본메뉴와-다음-일-정리)으로 연결한다. management-active 원문의 「첫 재계획 사용자 결정」·백로그 후속 계약(`msg_6e281afe2167`)·「범위 밖 목록 — 다음 계획으로 이관」을 대조했다. 백로그 메뉴는 시스템 카드와 분리된 별도 목표 계획이며, 이 checkout에 후속 goal이 존재한다고 가정하거나 현재 상태를 복제하지 않는다. Management 전체 corpus·기록 소개/상세·창/배율 복원·코드 보기·매핑 드리프트·I-03~05도 그 goal의 출처 있는 범위 밖 표를 정본으로 연결한다([해당 worktree 진입](CURRENT.md)).

메인 `msg_f43ee6f5d226`의 정리 결정에 따라 Architecture 기능 테스트 CI는 [Architecture 테스트 전체 PR CI goal](../../01_Phases/goals/2026-10-05-architecture-tests-ci/goal.md)로 연결하며 새 후보로 중복 등록하지 않는다. 다른 checkout의 최신 목표·상태는 [CURRENT](CURRENT.md)에서 해당 worktree를 찾아 확인하며 이 문서에 상태를 복사하지 않는다. 외부 URL 내용 확인이나 운영 화면 구현은 이 이관 작업에서 수행하지 않는다.

PR177 Gardener2의 [정리 후보2](../../.backups/verification/2026-10-04-teammate-onboarding/gardener2-report.md#정리-후보)는 원문 대조 helper의 독립 구현 **2회**, CRLF 꼬리로 인한 거짓 불일치, 메모 선행의 실제 이탈 **1건**, 다시 쓰기에서 생성 시각이 재설정되어 birthtime 사후 검사가 거짓 위반을 낸 관측을 남겼다. [사용자 A 전달](../../.backups/verification/2026-10-05-operating-canon/main-gardener-decision.json)은 `contract-context-check`의 근거 연결만 허용했다. 기존 선행 조건을 유지하고 구현은 후속에 두며, `goal-state-drift` 행은 이번 결정으로 갱신하지 않는다. 이 단락의 수치는 Gardener 원문 관측이며 이번 작성자가 새로 실행한 helper 결과가 아니다.

## 마감 뒤 후보

등록 전 기존 ID의 중복 부재를 확인했다. 아래 등록은 메인 `msg_05ba75ccd7f9`(2026-10-05 02시대 KST)이 전달한 후속 경계이며 새 goal 착수·제품 구현·실제 도달성 재검증의 완료를 뜻하지 않는다.

| ID | 제목 | 이유 | 출처(누가·언제·메시지 ID) | 선행 조건 | 담당 후보 | 상태 |
|---|---|---|---|---|---|---|
| `persistence-recovery-post-deadline` | 영속화 제한 복구·principal 실증·전체 시험 행렬·crash 복구 | 기존 두 번째 PR의 PersistenceRecovery·Windows principal 실증·D1a 전체 행렬·crash 복구를 마감 뒤로 분리 | GameDev `msg_e1afafb84002`(2026-10-04T15:05:07Z), [전달 원문](../../.backups/verification/2026-10-05-operating-canon/msg_e1afafb84002.json); 메인 `msg_05ba75ccd7f9` 재전달, 승인된 [파트별 범위 사본](../../.backups/verification/2026-10-05-operating-canon/sources/plan-scopes-draft.md) | 11월 전시회 마감 뒤 별도 goal 승격 검토·현재 GameDev goal의 종료/보존 자원·실제 환경 경계 재확인; 저장소 자체의 마감용 구현은 게임 저장 고리 goal과 구분 | GameDev | 대기 |
| `enemy-hit-dead-guard` | 즉시 적 타격의 IsDead 사전 가드 | `ApplyImmediateEnemyHit`의 죽은 대상 사전 거부 검토 후보; 실제 도달성은 이번 문서 작업에서 재검증하지 않음 | GameDev `msg_0a650b7e1c8b`(2026-10-04T16:11:42Z), [전달 원문](../../.backups/verification/2026-10-05-operating-canon/msg_0a650b7e1c8b.json)의 Content 전달 근거 `resume-content-dead-guard-candidate.json`·담당 goal; 메인 `msg_05ba75ccd7f9` 재전달 | 11월 전시회 마감 뒤 실제 경로·보존 전투 동작 재확인 및 코드 주인과 범위 조율 | GameDev·Content 협의 | 대기 |

## 운영 현행화에서 연결한 후보

계획 번호는 저장소 밖 `C:/Dev/DawnHolder_Dashboard/plans/ops-backlog-20261006.md` 75~88행이다. 원문은 E/pr2/sources.md 「BACKLOG 등록 원천」의 고정 발췌만 읽었다. 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`가 전달한 처리 계획 승인 「OK A로 가자…」의 「뒤로」 묶음이며 [PR2 원천](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#pr2-canon-sources)으로 연결한다. 계획 26·34는 기존 행, 30의 정적 분석은 `post-db-static-analysis`에 근거를 덧붙였다. 계획 38은 이미 반영돼 등록하지 않는다.

| ID | 제목 | 이유 | 출처(누가·언제·메시지 ID) | 선행 조건 | 담당 후보 | 상태 |
|---|---|---|---|---|---|---|
| `harness-contract-completion` | 하네스 goal 미완 범위 연결 | 경로·사실 검사, PS 출력 고정, 홈 경로 하드코딩(format-check.ps1:83, runner.py:67), FEATURE_MAP 오경로 `Maps/States/Actions`, 없는 CODE_CONVENTION 절 참조, ADR-035가 승인 범위에서 멈췄다는 전달 | 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 25(75행) | 기존 goal의 미완 범위·현재 소유권 확인; 새 착수 승인으로 삼지 않음 | Rules(+Core) | goal 승격 → [하네스 원칙](../../01_Phases/goals/2026-10-03-harness-principles/goal.md) |
| `operating-trial-evaluation` | 10-31 운영 시범 평가 준비 | Gardener 지속·검증 강도 시범·규칙 가지치기 세 평가의 담당·자료 수집이 필요 | 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 27(77행) | 담당 조율·각 시범 원시 확보·사용자 평가; 검증자 모델 시범 자료는 해당 goal에서 확인 | 메인 | 대기 |
| `task-token-verdict-recording` | 작업별 토큰·판정 자동 기록 | 10-04 사용자 승인으로 전달된 기록 과제와 Sol rollout 공백으로 측정 원천이 비는 문제 | 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 28(78행) | 승인 원문·측정 원천·빈 rollout 처리·수집 범위를 재확인 | Rules | 대기 |
| `codemap-runner-followup` | CodeMap runner·경로 검사 후속 | runner 회귀 fixture 저장소 편입·폴더 범위·경로 예산 검사·Node20 경고 후보 | 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 29(79행) | 현재 runner·CI와 기존 CodeMap goal의 완료 범위를 대조 | CodeMap | 대기 |
| `operating-reference-maintenance` | 운영 검사 전제·참조 보존 점검 | CodeRules 로컬 실행 전제·Orca helper 1.4.218 고정·정본의 Git 밖 링크·보관 커밋 GC 위험 후보 | 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 30(80행); Gardener 제안(채택은 사용자): 공식 ask 수신 helper의 Orca 1.4.218 고정을 상태 구분·help 확인 helper·fixture로 검사 가능하게. 근거는 병합 관문 goal(`2026-10-06-merge-gate-canon-refresh`)의 공식 ask 15건(사람 대조 기록 13건, 미확인 2건)과 직전 Rules goal 2건이며 이번 작성자의 실측이 아님. Gardener `msg_925c5596342c`(2026-10-07T10:14:55Z), E/gardener/report.md 「후보 2」(229~256행) | 각 원문·현재 버전/참조·보존 수단 확인; SQL·TS·Python 정적 분석은 `post-db-static-analysis`로 연결 | Rules·CodeMap | 대기 |
| `auto-mode-workflow-blockers` | auto mode의 리드 작업 차단 검토 | 리드 push·우편함 읽기가 pane별 사용자 허용으로만 풀렸다는 관측 | 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 31(81행) | 원시 차단 근거·프로젝트 범위 해법 확인; 전역 변경·우회는 사용자 명시 선택 범위만 | 메인→사용자 | 대기 |
| `unity-mcp-seat-visibility` | Unity MCP 연결·시트 표시 후속 | AI Assistant 업그레이드로 연결 끊김 제거와 현황판 시트 보유 표시 후보 | 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 32(82행) | 현재 연결·시트 원시와 업그레이드 필요성·현황판 소유 조율 | Content·메인(현황판) | 대기 |
| `windows-execution-prerequisites` | VHDX·PS5.1 실행 안내 점검 | 전달된 실행 전제를 DEVELOPMENT에 반영할 후보 | 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 33(83행) | 실제 환경·현재 실행 안내와 부작용을 대조 | Core | 대기 |
| `management-operations-followup` | 운영 백엔드·시스템 카드 후속 | R-01~R-09·서버 등록/로그 계약·시스템 카드 전체 자료·R-15~R-17 후보 | 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 35(85행) | 기존 Management goal·`server-operations-view`와 경계 대조. VM 계획은 `management-e2e-input`, 실행 배치 확인은 `management-launcher-real-run`으로 연결 | Management | 대기 |
| `script-formatting-followup` | PS·SQL 서식 보강 | OBS-1 이어지는 줄 199곳 등 서식 보강 후보이며 현재 잔여 실측값은 아님 | 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 36(86행) | 현재 잔여·코드 주인·이미 병합된 서식 범위 대조; 정리 후 All 측정은 `powershell-all-evidence` | Core+Rules | 대기 |
| `rule-wording-ownership-followup` | 명칭·판정 표현·담당 잔여 점검 | GameDev 표기, O2, N1, N2, 판정 양식 표현, 외부 팀원 PR 응대 담당, 도우미 세션 역할, Unity 설정 부분 커밋 담당 후보 | 메인 전달·2026-10-06·목표 요청서 `msg_3902e180080c`, 외부 계획 37(87행); O2는 이 goal의 관문 O2와 다른 과거 관찰 번호 | 각 번호의 원래 goal·현재 명칭·담당 경계 대조; 일괄 명칭 정리는 승인 밖 | Rules 등 | 대기 |

## Management에서 이관한 후보

받은 7필드 행을 그대로 보존한다. [다음 일 분류표](../../05_Management/goals/2026-10-06-record-source-unification/next-steps-classification.md)는 commit `1f8a9dce`(반영 `3fc61360`) 기준으로 메인이 전달했다. remote-play-check·system-map-3d의 A(백로그로 옮긴다)는 2026-10-06 사용자 결정이며 Management `msg_2f00062564f8`가 전달했다.

| ID | 제목 | 이유 | 출처(누가·언제·메시지 ID) | 선행 조건 | 담당 후보 | 상태 |
|---|---|---|---|---|---|---|
| management-launcher-real-run | 운영툴 실행 배치의 실제 더블클릭 확인 | README 34행이 사용자 진입점을 「미검증」으로 남겼다. 배치 분기 시험은 stub START였다 | Management 리드 제안·2026-10-06·msg_33365ac784b2(10:37:32Z, 같은 본문의 msg_54535201beaa)를 메인이 msg_b1bf869e87fd(10:38:19Z)로 받아 다음 Rules goal 입력에 넣음. 계획 35번과의 분리는 메인 결정 msg_da1c987f7ff0(10:54:00Z). 원문은 catalog nextSteps #25(change-launcher), 분류표 | 사람이 직접 더블클릭해야 한다. OS 합성 입력은 금지다 | Management | 대기 |
| remote-play-check | 실제 원격 플레이 확인 | 원격 보간 goal이 실서버 원격 플레이·시각 jitter·성능을 미실행으로 남겼고 이후 실행 기록이 없다 | 사용자 결정·2026-10-06·메인 전달 msg_dfd3843e9464, catalog nextSteps #11(remote-rendering)·#23(change-interpolation), 분류표 | 로드맵 P7 「게임 회귀」 단계와 겹치는지 확인 | Core | 대기 |
| system-map-3d | 운영툴 시스템 지도 3D 표현 | 9-30 이주 협의안의 Three.js 시스템 지도 후보가 처분 없이 남아 있다 | 사용자 결정·2026-10-06·메인 전달 msg_dfd3843e9464, catalog nextSteps #21(management-records), 분류표 | 색인 전환 뒤 표시할 관계 자료와 필요성 확인 | Management | 대기 |
| `records-check-invalid-catalog-noise` | 색인이 깨질 때 records:check 진단 정리 | 색인이 `CATALOG_INVALID`(형식·JSON·스키마 위반)이면 `catalog`는 null인데 `indexAvailable`은 true로 남는다. 그러면 goal 묶음이 빈 locator 목록으로 돌아 모든 goal 폴더(약 70개)에 `GOAL_NOT_INDEXED` warning을 내고 진짜 오류 한 줄이 묻힌다. 위치 `05_Management/frontend/electron/record-index-check.ts` 63~89·151~175행. 코드 읽기 추론이며 실제 출력은 미측정 | Management 리드·2026-10-07·msg_60ff11c7efdc; 원천 PR2 독립 검증 관찰 O1(`msg_1f2a08d5bf2a`, 판정 원문 211행); Rules 조율 지시 메인 `msg_7ba36ac633a9` | 깨진 색인으로 `npm run records:check` 실제 출력 측정 | Management | 대기 |
| `source-read-hard-link` | 원문 읽기 경계의 hard link 거절 | `lstat`·native realpath 판정은 저장소 안 hard link가 루트 밖 파일을 가리키는 경우를 잡지 못한다. 메인이 정한 거절 8종 밖이고, 만들려면 저장소 쓰기 권한이 필요하다. 설계 「원문 구간 읽기 경계」에 한계로 적혀 있다. 위치 `05_Management/frontend/electron/source-section-store.ts` 36~44행. 검증자 제안은 `stat.nlink > 1`을 `link`로 거절 | Management 리드·2026-10-07·msg_60ff11c7efdc; 원천 PR2 독립 검증 관찰 O3(`msg_1f2a08d5bf2a`, 판정 원문 213행); 메인 결정 `msg_883836e1208a` 2항(2026-10-06T19:40:31Z, PR 범위 밖·Rules에 넘길 BACKLOG 후보); 메인 `msg_7ba36ac633a9` | 보안 경계 확장 여부의 메인 판단, `nlink` 거절이 git 작업 트리 정상 파일에 오탐을 내지 않는지 확인 | Management | 대기 |
| `management-tests-typecheck` | 운영툴 시험 파일 타입 검사 script | vitest는 타입을 지우는데 `tests/*.test.ts`를 타입 검사하는 npm script가 없다. `05_Management/frontend/tests/tsconfig.json`은 있지만 `package.json` scripts가 쓰지 않고, 앱 `tsconfig.json`의 include는 `src`·`vite.config.ts`뿐이다 | Management 리드·2026-10-07·msg_60ff11c7efdc; 원천 PR2 독립 검증 관찰 O4(`msg_1f2a08d5bf2a`, 판정 원문 214행); 메인 `msg_7ba36ac633a9` | 운영툴 시험 CI 목표(사용자 결정으로 Management 담당, 메인 전달 `msg_85435d570141`)에 묶을지 확인 | Management | 대기 |
| `markdown-fence-tracker-unification` | Markdown fence 판정 통합 | `electron/backlog-table.ts`(65~78행)와 `electron/source-section-contract.ts`(71~80행)가 fence를 따로 판정한다. PR3(PR200)는 규칙만 맞췄다. 합치면 MCP 공유 모듈 변경과 `mcp-dist` 재빌드가 따른다(백로그 메뉴 설계 「fence 판정」) | Management 리드·2026-10-07·msg_60ff11c7efdc; 원천 PR2 독립 검증 관찰 O2(`msg_1f2a08d5bf2a`, 판정 원문 212행)와 PR3 리드 설계 결정; 메인 `msg_7ba36ac633a9` | 두 판정이 다르게 읽는 실제 문서가 있는지 확인, `mcp-dist` 재빌드와 MCP 검증 범위 확인 | Management | 대기 |
| `management-text-contrast-guard` | 운영툴 전 화면 글자 대비 장치 | 밝은 테마 덮어쓰기에 없는 class가 예전 어두운 테마의 고정 색을 써서 글자가 흐려진 사용자 관측이 PR179(`bc38a4c8`)에 이어 PR200 「이후 작업」 탭(`5911f13f`)에서 두 번째로 나왔다. PR200은 그 탭만 실제 renderer 대비 회귀 시험(`951fae1e`)으로 막았다. 후보는 전 화면 대비 시험이나 `styles.css` 고정 색 금지 검사다. Gardener 관측상 `styles.css`의 `color: #hex` 줄이 63개라 기준선·허용 목록이 필요하고, 대비 측정 코드가 그 goal 안에 세 벌 생겼다. PR200 재검증 관찰 O4(고정 색 금지 이유 주석 없음)도 함께 본다 | 사용자 결정·2026-10-07·메인 전달 msg_02077b4a4df3(1A). 원천은 메인 결정 msg_6bae84d02fa1(07:33:45Z, 두 번째 발생, 기록만 하고 착수는 사용자 범위 결정 뒤). Management 리드 전달은 msg_299e90686a33(2026-10-07T11:59:58Z) | 운영툴 시험 CI 목표에서 Electron 대비 시험의 Linux 실행 확인 | Management | 대기 |

## 병합 관문 goal에서 연결한 후보

[병합 관문 goal의 후보](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#다음-계획-후보)와 판정·관측을 연결한다. 메인 `msg_bf63c20c8abe`가 전달한 사용자 「후속 계획의 일괄 검토」 결정대로 개별 승인 없이 대기로 등록하며 새 검사·hook·계정 변경은 시작하지 않는다. 판정 원시는 E(`.backups/verification/2026-10-06-merge-gate-canon-refresh/`)에 있다.

| ID | 제목 | 이유 | 출처(누가·언제·메시지 ID) | 선행 조건 | 담당 후보 | 상태 |
|---|---|---|---|---|---|---|
| `codex-merge-blocking` | Codex 세션 병합 차단 | 현재 Claude hook은 Codex에 적용되지 않음 | Rules goal 2026-10-06-merge-gate-canon-refresh 「다음 계획 후보」·초안 세부 근거 4; 메인 전달·2026-10-06·`msg_3902e180080c` | 프로젝트 hook 지원·신뢰 및 변경마다 사용자 검토 조건·현재 관문과 동일 판정 확인 | Rules | 대기 |
| `agent-account-ruleset` | 에이전트 계정 분리·ruleset 보강 | 모든 세션을 서버 쪽에서 막는 대안과 관리자 우회 「PR로만」 중간안 후보 | Rules goal 2026-10-06-merge-gate-canon-refresh 「다음 계획 후보」·초안 세부 근거 3; 메인 전달·2026-10-06·`msg_3902e180080c` | 공식 ruleset 확인·계정/classic 토큰·이 PC gh/git 로그인 비용 검토·사용자 범위 승인 | 메인→사용자 | 대기 |
| `mailbox-output-loss-hook` | 대기 출력 유실 차단 hook | 우편함 대기의 `&`·`/dev/null` 사용을 PreToolUse 층에서 막을 후보 | Rules goal 2026-10-06-merge-gate-canon-refresh 「다음 계획 후보」; 메인 확인·2026-10-06·`msg_bc4cea4b161f` 2항; Management 리드 두 번째 발생·2026-10-07T06:38:36Z·`msg_9f655bb20a2c`(`check --wait`를 `&`·`> /dev/null 2>&1`로 실행), 메인 판단 `msg_cec953dd3fdc`(06:40:23Z); Rules 리드·2026-10-07T06:57Z·`--wait` 없는 `check --ack` 출력 `/dev/null` 폐기([goal 후보 기록](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#다음-계획-후보)); Management 넘김 `msg_f0bc3d74e103`(2026-10-07T12:01:13Z, 메인 `msg_a75d6232584a` 지시): 1024바이트 배수에서 끊긴 송신 결과 파일 4개(Management 근거 폴더·Git 밖). 그중 2개는 `tee <파일>` 뒤 `grep -m`·`head -c` 파이프가 먼저 끝나 끊긴 것을 당시 명령으로 확인했다. 나머지 2개는 크기만 1024의 배수이고 JSON으로 읽히지 않으며 당시 명령은 확인하지 못했다. [운영툴 기록 원본 일원화 PR202 좁힌 문서 실사](../../05_Management/goals/2026-10-06-record-source-unification/goal.md#pr202-좁힌-문서-실사) Z2에서 발견; 대기 출력 버림과 같은 「출력 유실」 갈래이며 명령 모양은 다름 | 묶음 2 계획 12와 범위·오탐·현행 Bash 백그라운드 절차 대조 | Rules | 대기 |
| `merge-gate-code-followup` | 병합 관문 코드·경계 후속 | T1 `+main`·한국어 주석 언어·O-T3 단독 병합 예외 이유, 비ASCII 공백 O2·이상한 session_id O6 후보 | Rules goal 2026-10-06-merge-gate-canon-refresh 「다음 계획 후보」, E/reverify3/verdict.md 및 E/verify/verdict.md:138·142(2026-10-06). O2는 gh 실패의 관측·추론, O6는 UUID 사용 시 영향 없음 관측; Gardener 제안(채택은 사용자): 병합 관문 판정 형태 표를 저장소 시험 자료로 두고 막힘 유지를 CI에서 단정. 근거는 위 병합 관문 goal 안 연속 차단 3회, 그중 회귀 1회(S1·S2), 미리 적은 오탐 관측 4개 판정, 실제 오탐 사건 3건(Rules 1, 메인 전달 2)이며 Gardener 당시 집계다. Gardener `msg_925c5596342c`(2026-10-07T10:14:55Z), E/gardener/report.md 「후보 1」(197~227행). 이후 Rules 리드 차단 1건은 별도 관측(E/backlog-intake-lead-block-excerpt.json, 명령 2026-10-07T10:58:18.814Z·hook 응답 2026-10-07T10:58:18.906Z)이다. 보존 원문은 fetch·병합 결과 조회·로컬 근거와 수신 메모 기록을 하려던 명령이며 병합·push 호출이 없어 `merge-gate:suspect-words` 차단을 오탐으로 판단했다. [다음 goal 씨앗 ①](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#다음-계획-후보)과 겹침 | 각 판정·현행 동작·반례 재확인; 다른 goal의 계획 37 O2와 구분·현재 승인 관문 보존 | Rules | goal 승격 → [hook 차단 줄이기와 보조 세션 스킬](../../01_Phases/goals/2026-10-07-hook-friction-helper-session/goal.md) |
| `sendmessage-delivery-observation` | SendMessage 전달 모양 실측 | 분류기 거부로 전달 모양을 미측정했으며 hook은 입력을 이미 막음 | Rules goal 2026-10-06-merge-gate-canon-refresh 「관문 적용 확인」(2026-10-07) | 허용된 안전 입력과 분류기 경계 확인·실제 전달/차단 근거 확보 | Rules·메인 | 대기 |
| `report-folder-unification` | 작업 결과 보고서 폴더 한 곳 | Management 제안의 관측: `00_Document/reports` 2건·`01_Phases/reports` 1건으로 나뉘고 위치 규칙이 없음. `01_Phases/reports/<날짜>-<주제>/` 한 곳과 기존 2건의 이동 또는 안내를 검토. 계획 24의 Management goal 위치는 PR2의 [INDEX](../INDEX.md) 진입 줄에 반영됨 | Management `msg_026b701e3728` 1항(2026-10-06T09:48:57Z)을 메인 요청서 `msg_3902e180080c`가 전달; 외부 계획 24(69행), 승인 초안 100행·[적용 결정](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#요구사항-원천과-적용-결정)의 `msg_e02ee97c2a7e`·`msg_bc4cea4b161f` 2항 | 이동 또는 안내 선택·링크 보존 방식 결정·[REPORTING](../conventions/REPORTING.md) 53행 참고 링크와 기존 링크의 영향 확인 | Rules·Management 협의 | 대기 |
| `management-feature-map-entry` | Management 기능 지도 진입 행 | [INDEX](../INDEX.md) 운영툴 진입은 PR2 반영, [FEATURE_MAP](../FEATURE_MAP.md)의 Management 행은 승인 초안에서 BACKLOG로 분리. `harness-contract-completion`의 FEATURE_MAP 오경로와 다른 일 | Management `msg_026b701e3728` 3항 뒷부분(2026-10-06T09:48:57Z)을 메인 요청서 `msg_3902e180080c`가 전달; 승인 초안 100행·[적용 결정](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#요구사항-원천과-적용-결정)의 `msg_e02ee97c2a7e`·`msg_bc4cea4b161f` 2항 | FEATURE_MAP의 기능별 입력·판정·클라이언트 경로 행 형식에 운영툴이 맞는지 확인·문서 소유 조율 | Rules·Management 협의 | 대기 |
| `new-goal-tdd-canon-link` | 새 goal부터 TDD 정본화의 후속 연결 | 10-04 「A」 결정은 Content goal에만 적용되고 정본에는 없음. PR2가 goal-loop 68행의 후속 문장을 [goal 이관 기록](../../01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md#goal-loop-당시-후속-범위)으로 옮겨 정본 쪽 연결이 사라짐. 외부 계획 13(묶음 2)의 정본화로 연결만 하며 이 행 등록은 정본화·착수 아님 | 사용자 결정 2026-10-04 「A」·[Content 적용 결정](../../01_Phases/goals/2026-10-05-items-inventory-currency/goal.md#정본-반영-전-적용-중인-사용자-결정) 137행·59행 4항; 외부 `ops-backlog-20261006.md` 계획 13(48행); 메인 관찰 `msg_9f5e4fce46f7` 2항(2026-10-07T07:51:24Z), 리드 판단 `msg_0a179bcca658`(07:53:04Z) | 계획 13의 소유·적용 대상과 기존 Content TDD 절차·정본 반영 파일 확인 | Rules·각 파트 협의 | 대기 |
| `dispatch-first-screen-display` | 위임 계약의 첫 화면 표시와 원시 경로 | `orca terminal` 금지로 작업자·검증자가 자기 화면 모델을 읽지 못해 공식 ask 세 건. 리드가 기동 직후 읽은 첫 화면 표시와 원시 경로를 [위임 계약](../../.agents/skills/dawnholder-task-context/references/templates.md#위임-계약)의 「요청·화면·backend 구분」 필드에 넣을 후보이며 backend 미확인은 `unknown` 유지 | Rules 작성자·실사자·수정자(2026-10-07)의 [작성 ask](../../.backups/verification/2026-10-06-merge-gate-canon-refresh/pr2-question1-manual-check.md) `msg_2f1221cc05d7`(06:18:43Z), [실사 ask](../../.backups/verification/2026-10-06-merge-gate-canon-refresh/pr2-audit-question1-manual-check.md) `msg_fb4341b4059e`(07:11:55Z), [수정 ask](../../.backups/verification/2026-10-06-merge-gate-canon-refresh/pr2-fix-question1-manual-check.md) `msg_0febd4ad4675`(07:31:29Z); v1 계약의 메인 관찰 `msg_9f5e4fce46f7`·리드 판단 `msg_0a179bcca658` 연결 | 작업 맥락 스킬의 위임 계약 양식 소유와 변경 범위 확인·요청값/리드 화면 관찰/backend 구분 보존 | Rules | 대기 |
