# 실행 선택과 실패 전달 계약

승인된 goal의 Roslyn 기본·CodeGraph 명시 선택을 위한 동작 단계 설계다. 구현 완료 문서가 아니다. 구조 단계의 기존 실행 보존을 먼저 검증한 뒤 신규 Sol에 발행한다. snapshot schema·동결된 비교 입력·설치본/cache 보존은 goal의 계약을 유지한다.

## 실행 진입과 저장 위치

- PowerShell 진입점의 기존 Action(path/prepare/measure/check)은 유지하고 `-Extractor Roslyn|CodeGraph|Compare`를 추가한다. 기본값은 Roslyn이다. WSL/Python 내부 진입점은 같은 선택을 `--extractor roslyn|codegraph|compare`로 받으며 직접 호출의 기본값도 roslyn이다. Compare는 이전 양쪽 비교를 명시적으로 재실행하는 선택이다.
- PowerShell은 선택적 `-EvidencePath`를 저장소 상대 경로로 받고 WSL에 전달한다. 허용 범위는 Git 제외 `.backups/` 아래이며 생략 시 `.backups/architecture/<선택>/`를 새 실행 근거 root로 사용한다. 기존 goalPath·freezeRecordPath와 과거 latest-run/batch는 보존한다. comparison-settings.json의 evidencePath는 아래 O1 절의 독립 검증 후 한 필드 이동만 허용한다. 이 예외는 메인 msg_b955907203c5 결정이 이전 설정 불변 설계를 대체한 것이다. 기존 상대 경로 경계 검증을 재사용하며 저장소 밖·symlink·다른 소유자의 runtime 접근을 허용하지 않는다.
- 실행의 source SHA·manifest/동결 입력 확인과 실제 compiler 설정은 유지한다. 현재 frozen source/evaluation 자료와 로컬 freeze-record·외부 참조가 필요한 도구이며, 이 목표에서 최신 HEAD용 입력 수집기로 일반화하지 않는다. 깨끗한 clone 검증에는 필요한 동결 자료를 명시적으로 제공한다.
- mode별 실행 공간 또는 동등한 명시 분리로 Roslyn 실행이 남아 있는 CodeGraph bundle/input/cache를 사용하지 않게 한다. 기존 owner marker·lock 경계와 과거 workspace를 보존한다. 입력 복사·verifiedCopies와 extractor config에는 실제 선택/복사/실행한 대상만 기록한다.
- check는 선택한 모드/입력에 해당하는 저장 결과만 검사한다. 결과 부재·다른 mode/manifest/SHA·일부 extractor 결과 누락은 명확히 보고하고 이전 성공을 현재 실행 성공으로 대신하지 않는다. 과거 증거의 읽기/재생은 허용하지만 덮어쓰지 않는다.

## 도구 선택 경계

| 선택 | 필요한 도구와 실행 |
|---|---|
| Roslyn | 고정 SDK와 실제 입력/참조, Python/소유 workspace 도구. CodeGraph 설치본 복사·Node/npm 실행·bundle 조회/hash·syntax-context를 요구하지 않는다. |
| CodeGraph | 승인된 1.6.1 bundle와 bundled Node·C# grammar·syntax-context를 명시 확인한다. 이 경로에 필요 없는 Roslyn 추출/restore를 실행하지 않는다. |
| Compare | 두 도구의 선행조건을 먼저 확인하고 기존 CodeGraph→Roslyn, cold/warm1/warm2/warm3 순서와 채점 의미를 보존한다. 선택한 한 도구가 없으면 비교 미완료를 명시하고 다른 도구로 대체하지 않는다. |

필요한 도구가 없을 때 자동 설치하지 않는다. CodeGraph의 수리 안내는 저장소의 고정 package/lock와 `install-codegraph.ps1`를 가리키고 Linux x64 bundle과 bundled Node 조건을 설명한다. 기존 설치/cache를 제거하거나 전역 npm/PATH를 고치는 명령을 제안하지 않는다. 기존 설치 스크립트의 의미 변경은 필요가 입증되지 않으면 하지 않는다.

## 결과와 실패

snapshot의 complete/partial/failed/notRun 상태는 그대로 둔다. 도구 실행 상태와 미래의 모듈 규칙 위반은 다른 축이다. 이 목표는 모듈 규칙을 추가하거나 위반 0건을 산출하지 않는다.

- 이번 실행의 기계 결과는 새 evidence root의 `execution-result.json`에 남긴다. 최소 항목은 action, selection, selectedExtractors, executionStatus, reasonCode, message, repair, 근거 위치다. 생성한 분석 결과에는 analysisStatus를 연결한다. 선택하지 않은 extractor는 실행 성공 목록에 넣지 않는다.
- `executionStatus=completed`는 필요한 실행/검사 명령을 완료했다는 뜻이며 분석의 complete를 뜻하지 않는다. 분석 partial은 그 상태와 diagnostics를 유지한다. 성공 종료는 0이다.
- 명시 선택한 CodeGraph bundle/Node/필수 입력 부재 또는 필요한 실행 도구를 시작하지 못한 경우 `executionStatus=unavailable`, 원인별 reasonCode, 고치는 방법과 재실행 명령을 출력하고 nonzero로 끝낸다. 정상적인 빈 그래프/위반으로 만들지 않는다.
- 도구가 시작한 뒤 실패·timeout·정규화/검증 실패는 `executionStatus=failed`와 실제 command/exit·stdout/stderr 근거를 연결하고 nonzero로 끝낸다. unavailable과 failed는 둘 다 실패한 실행이지만 기계 상태와 원인을 구분한다. shell/인자/안전 경로 단계에서 파일을 안전하게 만들 수 없으면 stderr에 진단과 수리 방법을 남긴다.
- 기존 snapshot schema를 늘려 검증기를 무력화하지 않는다. 이미 수행한 결과가 있더라도 이번 시도 실패는 별도 기록하며 latest-run과 전체 성공을 혼동하지 않는다. shared helper는 필요한 결과/선행조건만 맡고 범용 상태 프레임워크를 만들지 않는다.

## 독립 검증의 관찰 조건

신규 Opus가 요구사항·실제 diff·구현자 원시 근거부터 실사한 뒤 별도 테스트를 작성한다. Roslyn 기본과 명시 Roslyn, 명시 CodeGraph/Compare, 잘못된 선택, CodeGraph 미설치·Node/grammar 누락·시작 실패, 실제 도구 실패·partial, check 결과 부재/선택 불일치, 두 실행의 evidence 충돌을 확인한다. 원본 설치본을 지우는 대신 설치본을 제공하지 않은 전용 clone으로 음성 사례를 만든다.

실제 Roslyn 실행과 실제 CodeGraph/Compare 재실행은 서로 다른 실행 범위로 기록한다. unit fixture/mocked argv만으로 실제 추출 완료를 보고하지 않는다. Python 기존 계약 테스트의 설치 비의존성, 기존 expected failure, 과거 replay의 skip/근거 한계를 유지한다. 메인 msg_41c49cc997fa에 따라 새 기능 테스트 CI 파일럿은 이번 PR에서 제외하고 보존하여 다음 목표의 첫 계획 후보로 넘긴다. 이번 계약의 독립 검증을 새 CI 작업으로 넓히지 않는다.
## O1: 기본 비교 결과 선택과 과거 재생

기본 테스트는 comparison-settings.json의 evidencePath 아래 latest-run.json이 가리키는 batch를 읽는다. 메인 msg_b955907203c5(main-o1-decision.json) 세부1·4에 따라 이번 코드로 새 Compare batch를 실제 실행하고 신규 Opus가 command.json·exit·시간 및 현재 코드 hash와 기록의 일치를 먼저 검증한다. 그 뒤 별도 Sol이 evidencePath 한 필드를 검증된 새 Compare 근거 root로 변경하고 신규 Opus가 pointer 이동 diff 1건과 기본 suite를 확인한다. 같은 PR 안의 명시적 변경이며 과거 root의 latest-run/batch와 frozen goal/freeze 경로는 불변이다. 이 순서는 기본 suite에 알려진 실패를 영구히 남기지 않기 위한 계약이다.

과거 batch는 ARCHITECTURE_EVIDENCE_BATCH로 명시해 별도 재생한다. 테스트 소유자인 신규 Opus가 당시 구현 bytes/hash 비교 대상을 명시 선택하는 경계를 보완하되, 기본 검사는 현재 코드와의 일치를 유지한다. 기록 hash를 자기 자신과만 비교하는 검사는 허용하지 않는다. 새 batch 검증/포인터 이동 전 O1 실패는 skip이나 expected failure로 바꾸지 않는다. 원본 제안은 msg_b4762841aa4e(structure-o1-question.json)이며, README로 실패를 설명하기만 하던 부분은 위 메인 결정이 대체했다.
