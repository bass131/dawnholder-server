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

상태는 `대기`, `goal 승격 → 링크`, `폐기 + 이유`로 제한한다. 승격 시 실제 `goal.md` 링크를, 폐기 시 이유와 결정 출처를 기록한다. 승격한 작업의 진행·검증·완료 결과는 링크된 goal에서 관리한다. 현재 아래 12개는 모두 `대기`다.

## 후보

씨앗의 근거는 [하네스 원칙 목표](../../01_Phases/goals/2026-10-03-harness-principles/goal.md)의 첫 BACKLOG 표와 **메인이 Rules Astra에게 전달한 결정**이다. `msg_c9bc79f8ec43`는 2026-10-03T10:26:02Z, 최신 분리 결정 `msg_c1412c982ac5`는 2026-10-03T10:39:09Z의 메인 전달이다. 저장된 메시지와 계약을 근거로 하며 이 문서 작성자가 사용자에게 직접 수신한 지시로 표현하지 않는다. 과거 메시지 원문을 보지 않은 경우에는 재전달 근거만 적는다.

| ID | 제목 | 이유 | 출처(누가·언제·메시지 ID) | 선행 조건 | 담당 후보 | 상태 |
|---|---|---|---|---|---|---|
| `diagram-design` | 도식 디자인 개선 | 설명 그림의 개선 요청 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 구체 화면·불편 확인과 소유 조율 | Rules·Management 협의 | 대기 |
| `report-response-format` | ASD-STE100·HTML 답변 방식 | 보고 방식 탐색 요청 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 프로젝트 적합성 확인과 도입 여부 사용자 판단 | Rules | 대기 |
| `goal-loop-improvement` | 작업 루프 자기 개선·그래프 설계 | 반복 작업의 개선 구조 검토 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 현 루프의 실제 수행 근거 대조 | Rules | 대기 |
| `powershell-all-evidence` | 파트 PS 정리 후 All 비교·승격 제안과 문서 근거 인용/집계 비교 helper | 세 파트 정리 결과를 원시 근거와 대조하고 PS 정책을 판단 | 메인 전달·2026-10-03·`msg_8639aeaf7a12`(08:32:32Z, 인용 helper 채택)·`msg_d5453356cc75`(10:17:49Z, 집계 helper 통합)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달; `msg_c1412c982ac5`(10:39:09Z, 이번 목표에서 All/helper를 백로그로 분리) | **촉발 조건: 세 파트(GameDev·Architecture·Management) PS 정리 병합 완료.** 이후 별도 goal 승격 검토 | Rules | 대기 |
| `workflow-definition-lint` | workflow 정의 lint | workflow 정의 오류 조기 검출; 전달된 [이전 목표의 결함 #4](../../01_Phases/goals/2026-10-02-agent-rule-context/goal.md) | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | actionlint류 도구 도입 사용자 승인 | Rules | 대기 |
| `verification-depth-policy` | 검증 강도 차등·작은 작업 예외 | 검증 범위 차등의 미결 사용자 판단 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 메인→사용자 정책 결정; 현 독립 검증 유지 | 메인→사용자 | 대기 |
| `rule-document-pruning` | 규칙 문서 가지치기 | 규칙 유지 범위의 미결 사용자 판단 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 기존 명시 정비와 구분해 메인→사용자 정책 결정 | 메인→사용자 | 대기 |
| `human-code-walkthrough` | 사람용 코드 따라읽기 문서 | 사람용 탐색 문서 도입의 미결 사용자 판단 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | 목적·유지비와 도입 여부를 메인→사용자가 결정 | 메인→사용자 | 대기 |
| `post-db-static-analysis` | DB 뒤 정적 분석기 단계 | 유지보수성 후속 강화 | 메인 전달·2026-10-03·첫 재계획 `msg_8639aeaf7a12`(08:32:32Z)를 새 계약 `msg_c9bc79f8ec43`(10:26:02Z)가 재전달 | DB 연동 뒤, 대상·담당 소유 조율 | 관련 영역 소유자와 조율 필요 | 대기 |
| `npm-engine-warning` | CI npm engines 경고 노출 | 메인이 전달한 EBADENGINE 경고의 요약 누락 문제 | 메인 전달·2026-10-03·`msg_c9bc79f8ec43`가 재전달한 `msg_d5453356cc75` | warning 파일럿·npm 버전 증거·경고 포함/미포함 fixture·실측, Management engines 조율; 해소/승격은 사용자 판단 | Rules·Management | 대기 |
| `server-operations-view` | 운영툴 서버 운영 시각화 | 사용자 2026-10-03 요청을 메인이 전달 | 메인 전달·2026-10-03·`msg_c9bc79f8ec43`가 재전달한 `msg_acba01cbf81e` | DB 연동 뒤, 참고 출처는 구현 조사 때 확인 | Management | 대기 |
| `work-status-view` | 운영툴 작업 현황 화면 | 작업 중·예정·결정 대기를 상시 보는 화면 요청 | 메인 전달·2026-10-03·`msg_c9bc79f8ec43`, 개인 CLI 참고 허용 `msg_c1412c982ac5` | 표시 범위·소유 조율과 별도 goal 승격 검토 | Management | 대기 |

## 후보별 경계

`powershell-all-evidence`는 All 비교·승격 제안과 인용/집계 helper를 **한 후보**로 묶는다. 세 파트 PS 정리의 실제 병합 완료 뒤 별도 goal에서 최신 main을 측정한다. 이번 하네스 목표의 구현·종료 범위에는 포함하지 않으며, helper를 먼저 구현하지 않는다. 과거 PS289를 현재 잔여 수로 가정하거나 파트 회신만으로 All 재측정을 완료했다고 기록하지 않는다.

승격 검토의 근거에는 인용의 경로·줄·JSON Pointer·값 대조와 집계의 파일/규칙/파트 수·최소 줄·진단 identity 차이·소유 맵(존재 여부와 unknown)을 포함한다. 원시 인용 대조와 집계 생성의 책임을 나누고 의미 판정은 독립 Opus가 맡는다. 속성 순서만 다른 JSON·섞인 줄·소유 불명 경로 등 외부 관찰 가능한 fixture로 검증할 후보이며 아직 구현·실행한 결과가 아니다.

PS 정책 판단에서는 PSSA 진단 severity, checker 상태/exit, CI job 실패 여부를 따로 실측한다. 목표가 기록한 현행 선택 PS 진단의 exit1과 warning 파일럿의 충돌 지점을 확인하고, 이미 존재하는 차단 효과를 이름 변경만으로 새 성과로 기록하지 않는다. 기존 CI 완화나 새 error gate 적용은 메인→사용자 결정 뒤 별도 동작 변경으로 다룬다.

`npm-engine-warning`은 warning 파일럿으로 npm 버전 증거를 남기고 EBADENGINE annotation/요약 노출과 경고 포함·미포함 fixture를 확인할 후보다. 실측 뒤 경고 해소 방법 또는 승격 여부를 사용자가 판단하며 Management의 engines 계약과 조율한다. 이번 등록은 도구 구현·CI 변경·engines 수정이나 해소/승격 결정을 뜻하지 않는다.

`verification-depth-policy`, `rule-document-pruning`, `human-code-walkthrough`는 각각 별도의 미결 정책이다. 사용자 결정 전 현행 독립 검증과 CODE_CONVENTION의 주석 정책을 유지한다. 기존에 개별 승인된 문서 정비를 일반 가지치기 승인으로 확대하지 않는다.

`server-operations-view`의 [kciter 서버 모니터링 분석 가이드](https://kciter.so/posts/server-monitoring-analysis-guide/)는 사용자 전달 참고 URL이다. 본문은 읽지 않았으며 요약하거나 구현 근거로 확정하지 않는다.

`work-status-view`의 `C:/Dev/DawnHolder_Dashboard`는 **저장소 밖 개인 CLI 참고**다. 메인이 작성한 Orca·gh 읽기 전용 조회와 메인 계획판을 참고 대상으로 전달받았다. Dawnholder 저장소 산출물이나 본 작업의 실행·검증 실적으로 기록하지 않는다.

## 중복 제외

이미 예정된 CodeGraph 정돈, 모듈 경계 검사, Management 백로그 메뉴는 새 후보로 중복 등록하지 않는다. 그 작업의 기준·상태·결과는 각 승인된 목표에서 관리한다. 위 정적 분석기·작업 현황 후보를 그 예정 작업의 착수나 완료로 해석하지 않는다.
