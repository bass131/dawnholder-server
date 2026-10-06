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

상태는 `대기`, `goal 승격 → 링크`, `폐기 + 이유`로 제한한다. 승격 시 실제 `goal.md` 링크를, 폐기 시 이유와 결정 출처를 기록한다. 승격한 작업의 진행·검증·완료 결과는 링크된 goal에서 관리한다. 최초 12개와 아래 이관 후보 7개는 등록 당시 모두 `대기`였다. 현재 승격·폐기 처분은 해당 행의 상태와 결정 출처에서 확인한다. 최초 12개 ID·출처·조건·7필드는 보존한다.

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
| `rule-document-pruning` | 규칙 문서 가지치기 | 규칙 유지 범위의 미결 사용자 판단 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 기존 명시 정비와 구분해 메인→사용자 정책 결정 | 메인→사용자 | 대기 |
| `human-code-walkthrough` | 사람용 코드 따라읽기 문서 | 사람용 탐색 문서 도입의 미결 사용자 판단 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 목적·유지비와 도입 여부를 메인→사용자가 결정 | 메인→사용자 | 대기 |
| `post-db-static-analysis` | DB 뒤 정적 분석기 단계 | 유지보수성 후속 강화 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | DB 연동 뒤, 대상·담당 소유 조율 | 관련 영역 소유자와 조율 필요 | 대기 |
| `npm-engine-warning` | CI npm engines 경고 노출 | 메인이 전달한 EBADENGINE 경고의 요약 누락 문제 | 메인 전달·2026-10-03·`msg_c9bc79f8ec43`가 재전달한 `msg_d5453356cc75` | warning 파일럿·npm 버전 증거·경고 포함/미포함 fixture·실측, Management engines 조율; 해소/승격은 사용자 판단 | Rules·Management | goal 승격 → [CI npm 경고와 운영 후속 정본화](../../01_Phases/goals/2026-10-05-ci-warning-operating-followup/goal.md) |
| `server-operations-view` | 운영툴 서버 운영 시각화 | 사용자 2026-10-03 요청을 메인이 전달 | 메인 전달·2026-10-03·`msg_c9bc79f8ec43`가 재전달한 `msg_acba01cbf81e`; 별도 목표 유지 승인 `msg_1da9a73ad0d2`와 [좁은 실시간 후보·Core 계약 근거](../../05_Management/goals/2026-10-05-development-record-navigation/goal.md#후속-후보와-서버-운영-시각화-근거) | DB 연동 뒤, 참고 출처는 구현 조사 때 확인 | Management | 대기 |
| `work-status-view` | 운영툴 작업 현황 화면 | 작업 중·예정·결정 대기를 상시 보는 화면 요청 | 메인 전달·2026-10-03·`msg_c9bc79f8ec43`, 개인 CLI 참고 허용 `msg_c1412c982ac5` | 표시 범위·소유 조율과 별도 goal 승격 검토 | Management | 폐기 + 사용자 결정 「운영툴 「작업 현황」 화면 후보를 폐기할지 → A 폐기」; 메인 전달 `msg_a1fe33623cbe`·[다음 goal 사전 결정](../../01_Phases/goals/2026-10-05-operating-canon/goal.md#다음-goal-사전-결정) |
| `management-e2e-input` | 운영툴 E2E 입력 — 앱 내부 입력 시범(Electron debugger CDP, 포커스 흉내, showInactive) + Hyper-V 가상 머신 준비 계획 | OS 입력 없는 보조 모니터 E2E 조작의 다음 계획 보존; Management 브랜치에 BACKLOG가 없다는 메인 설명에 따라 Rules가 main 기준 운영 문서 한 곳에 반영해 checkout 간 동시 쓰기 방지 | 메인 Claude 재전달·2026-10-04T09:36:50Z·`msg_4ac97f9a40b4`; 이 메시지가 메인 전달 `msg_d265a8b08d4b`·Management goal 기록 `msg_7b16a80549b1`을 재전달(두 원메시지 직접 실사 안 함); 메인이 전달한 사용자 대시보드 결정 응답(Enter 제출) 원문 「2) 보조 모니터 - OS 입력 없이 E2E 조작하는 방법 → B A + 가상 머신 준비」 | Management 현재 목표 종료 뒤 별도 goal 승격 검토; 후보 등록은 E2E/VM 구현 착수·설치 허가 아님 | Management | 대기 |
| `combat-smoke-zero-lag-flaky` | `CombatSmoke_ZeroLag_Succeeds` 원인 미확정 flaky | 공격 거리 수렴 15초 timeout. 같은 head 재실행은 통과했으나 원인 규명 아님; PR2 제품 범위 제외 | 메인 Claude 전달·2026-10-05T07:17:31Z·`msg_a61c279bc351`([전달 원문](../../.backups/verification/2026-10-05-items-inventory-currency/pr1-new-head-approval.json)); 두 번째 흔들림은 메인의 판단이며 이전 별도 발생 직접 실사 안 함 | [CI 기록](../../.backups/verification/2026-10-05-items-inventory-currency/pr1-main-sync-ci-audit.json)의 실패·재실행 및 이전 별도 발생 원시 로그 대조, 별도 범위·소유 조율 뒤 승격 검토; 후보 등록은 조사·완화·재시도 정책 착수 권한 아님 | Content·GameDev 협의 | 대기 |
| `unity-sentis-define-drift` | 반복 Unity 실행의 SENTIS define 자동 제거 | PR1·PR2 선행 실행에서 `Standalone`의 `SENTIS_ANALYTICS_ENABLED;` 제거 한 줄과 동일 SHA 쌍(C1412CD2…49D480 → 934110CB…F94162)을 관측; 패키지 차이는 원인 추정이며 확정하지 않음 | 메인 Claude·2026-10-04T18:32:29Z·`msg_7691eaa0154c`의 반복 후보 지시([원문](../../.backups/verification/2026-10-05-items-inventory-currency/unity-settings-restore-decision.json)); [PR1 사본·승인 복원](../../.backups/verification/2026-10-05-items-inventory-currency/ProjectSettings-approved-restore.json), [PR2 사본·반복 복원](../../.backups/verification/2026-10-05-items-inventory-currency/ProjectSettings-pr2-tdd-approved-restore.json) | 각 실행 전후 원시 diff·hash·사본/복원과 실제 패키지·define 변경 주체를 대조한 뒤 별도 범위·소유 조율; 현재 후보 등록은 설정/패키지 수리 착수 권한 아님 | Content·GameDev 협의 | 대기 |

## 영속 통합 goal에서 연결한 후보

[실제 SQL 설치·엔진 판정 goal](../../01_Phases/goals/2026-10-04-persistence-integration/goal.md)에서 메인 전달에 따라 남긴 후속 후보다. 등록은 이번 G2 범위의 구현이나 새 goal 착수를 뜻하지 않는다.

| ID | 제목 | 이유 | 출처(누가·언제·메시지 ID) | 선행 조건 | 담당 후보 | 상태 |
|---|---|---|---|---|---|---|
| `powershell-dotnet-boundary-checklist` | PowerShell과 .NET 경계 체크리스트 | builder indexer·`[NullString]`·예외 체인 관련 실패에서 경계 확인 항목을 검토 | 메인 전달·2026-10-05T05:55:03Z·`msg_d57b6b7d08d2`, Fable 읽기 전용 조사 요약을 바탕으로 후보만 기록 지시 | 실제 원천 재확인·DB 단계 뒤 별도 범위와 사용자 채택 판단 | GameDev | 대기 |
| `database-allowlist-drift-test` | DB allowlist 드리프트 probe의 정식 테스트 승격 | 검증자가 사용한 AST probe의 유지·회귀 검사 가치 검토 | 메인 전달·2026-10-05T05:55:03Z·`msg_d57b6b7d08d2`, 현재 TESTDB·INSTALL goal 범위에는 넣지 않음 | probe 원문·범위·오탐 및 기존 테스트 중복 대조 뒤 별도 goal 판단 | GameDev | 대기 |
| `combat-smoke-timeout-investigation` | CombatSmoke_ZeroLag_Succeeds 간헐 timeout 조사 | 서버 제품 diff가 없다고 전달된 두 CI에서 15초 timeout. 원인은 미확정이며 flake·회귀로 단정하지 않음 | 메인 전달·2026-10-05T06:53:29Z·`msg_65740b4635b6`; 아래 두 사례와 Content 원시 경로는 재전달 근거 | G2·첫 PR 뒤 사용자와 조사 착수·범위를 결정. 후보 등록만으로 테스트 변경·서버 조사에 착수하지 않음 | GameDev | 대기 |

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
| `contract-context-check` | 위임 계약과 사전 맥락 누락의 검사화 | 규칙 원문 누락·읽기 순서 이탈의 반복 여부 검토 | 메인 전달 inventory E·`msg_168d9aa35710`(2026-10-03); replan-agenda의 Management P-01 사례, 사건 원시 미확인; 메인 전달 `msg_7bfc0ca278f5`(2026-10-04T17:11:41Z), Gardener2 후보2의 helper 중복 2회·CRLF 거짓 불일치·메모 선행 이탈 1건·birthtime 재설정 관측 연결 | 현행 메모/원문 계약과 실제 이탈 근거 대조·검사 범위/오탐 검토 | Rules·해당 파트 | 대기 |
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
