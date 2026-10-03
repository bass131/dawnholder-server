# CodeGraph 연결 코드 정돈

상태: **메인의 첫 재계획으로 목표 발행 승인. 최신 main `48e722b`에서 새 브랜치를 만들고 이전 결과 기록을 `8ebc031`로 이관했다. 현재 구조 단계 계약을 준비하며 구현·독립 검증은 아직 미실행이다.** 작업 브랜치는 `feat/codegraph-adapter-cleanup-20261003`. 승인된 초안은 로컬 `.backups/planning/2026-10-03-codegraph-adapter-cleanup/goal.md`, 이번 근거는 `.backups/verification/2026-10-03-codegraph-adapter-cleanup/`에 둔다.

## 첫 재계획의 승인과 적용

메인 전달 `msg_00bfde61139b`의 사용자 원문은 “오케이 추천 사항으로 진행하잡”이다. 사용자 직접 입력으로 격상하지 않으며 발신 handle을 현재 메인과 대조했다. 원문은 이번 근거 폴더 `main-approval.json`이다. 아래 결정이 초안의 발행 대기·미결 부분보다 우선한다.

- CodeGraph 연결 정돈 목표를 승인 초안 기준으로 발행한다. R-7 Fable goal 검토는 이번에 적용하지 않는다. 이후 모듈 경계 검사는 별도 목표로 이어간다.
- DB 연동을 최우선으로 하고 다른 파트의 서버 영속성·`99_Tools/database`·MSSQL 문서를 건드리지 않는다.
- 첫 구조 커밋에 `run-architecture.ps1`·`install-codegraph.ps1`의 서식을 한 번에 정리한다. 메인이 전달한 All run 37109061247, main 48e722b의 진단은 각각 3건·1건이다. 새 실측으로 재확인하며 두 파일의 `git diff -w`가 비고 All PS 진단 0인지 독립 검증한다. error 승격은 Rules 실측 뒤 별도 결정이다.
- 내용 없는 heartbeat에는 subject/body 태그를 요구하지 않고 from_handle·taskId·dispatchId로 출처를 판단한다. 내용 있는 지시·보고·질문·worker_done·ask·escalation은 태그를 유지한다. heartbeat 태그 누락 교정 메시지를 보내지 않는다.
- Gardener의 문서 인용 검사 helper/fixture 후보는 Rules 공용 도구 백로그로 채택됐다. BACKLOG.md 신설은 Rules 첫 별도 PR이며 Management의 메뉴는 그 뒤다. 이 파트는 필요할 때 기존 gardener.md와 verification-3~5 판정의 근거 위치만 제공한다.
- 최신 task-context의 사전 메모·관련 규칙 원문 계약·독립 판정 양식을 적용한다. 단, `8ebc031` 기계 이관은 새 규칙 읽기/메모보다 먼저 수행됐다. `astra-context.md`에 사실을 기록하고 `msg_85d6ac69e5b4`로 메인 처리 결정을 요청했다. 사전 작성으로 소급하지 않으며 해당 문서 실사의 판정 전 결정이 필요하다.

## 근거와 현재 경계

- 메인 전달 메시지 `msg_1e28e7ca4583`(2026-10-03T07:02:59Z), 원문 `entry-delivery.json`. from_handle을 현재 메인 terminal과 대조했다. 이 문서의 사용자 결정은 메인이 전달한 결정이며 사용자 직접 입력으로 격상하지 않는다.
- 보존 결정 정본: `01_Phases/goals/2026-10-02-architecture-extractor-comparison/goal.md`의 「PR 생성 뒤 CodeGraph 보존 결정과 후속 재개」. SDK Roslyn 기본 선택, CodeGraph 패키지·node_modules·npm cache 보존, 연결 코드 삭제 없이 재사용 가능한 정돈을 유지한다.
- 진입 경로 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/architecture-active`, 브랜치 `feat/architecture-extractor-a1-20261002`, HEAD `69c622431107b35c2f9a6e5647ebe3386f73ebff`. origin/main과 원격 main은 조회 시 모두 `963335414cbf52886fe24aee41aebfaf8cf16fbe`. HEAD...origin/main은 2/1, 작업 트리는 clean이었다. 관측 상세는 `research-notes.md`.
- 로컬 결과 기록 `d5fb467`·`69c6224`는 원 diff가 이전 goal.md 하나인 것을 확인하고 순서대로 no-commit cherry-pick하여 `8ebc031` 첫 문서 커밋으로 이관했다. 옛 브랜치는 보존하고 push하지 않는다.

## 목표와 범위

Roslyn 실행이 CodeGraph 설치에 의존하지 않게 하고, CodeGraph는 명시적으로 선택한 adapter로 계속 재실행·비교할 수 있게 한다. 현재 정규화 스냅샷과 동결 비교 자료의 의미·근거를 보존한다. 새로운 모듈 경계 규칙이나 현재 HEAD용 전체 소스 수집기는 이 목표에 넣지 않는다.

허용 후보는 `99_Tools/Architecture/`의 실행 진입점, `Architecture.Common.ps1`, `Pipeline/`의 실행·입력 준비·정규화 연결, `CodeGraph/syntax-context.cjs`, `install-codegraph.ps1`, package.json/lock와 코드 가까운 실행 안내다. 정확한 쓰기 파일은 목표 확정 시 좁힌다. Roslyn 추출 알고리즘, 공통 snapshot schema, frozen manifest/scope/truth, 기존 비교 결과와 원시 로그는 의미 변경하지 않는다. package 버전과 lock은 원칙적으로 보존하며 설치 경로를 정돈할 필요가 있어도 임의 업그레이드는 하지 않는다.

검증자는 `99_Tools/Architecture.Tests/`의 관련 독립 테스트와 자기 판정·근거만 쓴다. Astra는 목표·설계·보고·Git 통합을 맡는다. 제품 서버/DB/Unity·패킷·공유 DLL, Formatting 구현/등록 정책, CodeRules와 중앙 CI, 사용자·Claude 설정은 이번 쓰기 범위 밖이다. 실행 자원도 GameDev DB 연동과 분리한다.

## 설계 제안과 보존 동작

1. 공통 실행 책임은 입력 SHA·manifest/설정 hash 확인, 소유 workspace/lock, 프로세스 실행 근거, 정규화·검증에 둔다. CodeGraph의 bundle 확인·DB dump·syntax-context·cache 기록은 adapter가 맡는다. 장래 도구를 위한 범용 plugin 체계는 만들지 않는다.
2. 기본 Roslyn, 명시 CodeGraph, 명시 비교라는 선택을 한 정본에서 해석해 PowerShell/WSL/Python에 전달한다. 구체적인 옵션 이름은 구현 계약에서 고정한다. 알려지지 않은 선택은 수정 가능한 안내와 함께 거부한다. CodeGraph가 없다고 다른 추출기로 조용히 대체하지 않는다.
3. Roslyn 경로는 CodeGraph bundle 복사·버전 조회·Node/npm 실행·syntax-context 처리·bundle hash에 접근하지 않는다. 선택하지 않은 CodeGraph는 미실행으로 표시하고 오류나 빈 성공 그래프로 만들지 않는다. 공유 Python fixture가 CodeGraph raw 형식을 다루는 것은 설치 의존성과 구분한다.
4. 명시 비교는 기존 frozen 입력과 cold/warm 라벨, 원시 결과·정규화·채점·cache 조건을 유지한다. 도구 선택별 결과와 실행 여부를 기록하고 일부 실행만으로 양쪽 비교 완료를 보고하지 않는다. 구조 이동으로 달라지는 실행 파일 목록·config hash는 새 구현의 근거로 기록한다. 과거 증거를 새 hash에 맞춰 고치지 않는다.
5. 새 실행 근거는 목표 전용 폴더와 새 batch에 둔다. 기존 `comparison-settings.json`의 과거 goal/evidence/freeze 지시를 무조건 새 목표로 덮지 않는다. 과거 결과 재생과 새 실행 경로를 구분하고 공유 latest-run을 덮어 과거 비교를 잃지 않도록 한다.
6. 설치 안내는 고정 버전·lock·설치 스크립트를 정본으로 삼는다. 기존 설치본과 npm cache는 삭제·이름 변경하지 않는다. lock 기반 설치 방식을 바꾸면 새 격리 디렉터리에서만 검증하며, 기존 node_modules를 지우는 명령으로 수리하지 않는다. 기본 Roslyn 실행에서 자동 설치·다운로드하지 않는다.

## 하네스 원칙과 결과 계약

- 반복되는 선택·선행조건 규칙은 독립 fixture/실행 검사로 고정한다. 실패 출력에는 대상·원인·근거 위치·바로 다음 수정/재실행 방법을 넣고, 테스트는 이 필드가 실제 원인과 일치하는지 확인한다.
- 정상 경로는 기본 Roslyn 한 경로로 둔다. adapter를 켠 경우에만 해당 선행조건을 검사한다. 새 안내의 수기 파일 경로는 존재를 검사하되 저장소 전체 문서 드리프트 검사로 범위를 넓히지 않는다.
- 실행 불가(미설치·도구 시작 실패), 실행 실패, 분석 partial, 검출된 규칙 위반을 서로 구분한다. 기존 snapshot의 notRun/failed/partial 의미를 유지하고 실행 진단을 별도로 담는다. 이 목표에는 새 모듈 규칙이 없으므로 위반 수를 만들지 않는다. 환경 실패를 위반이나 0건 통과로 표현하지 않는다.
- 신규 정책 검사는 warning 파일럿→실측→별도 error 승격으로 도입한다. warning은 정상 분석의 필수 조건 실패를 exit 0 성공으로 바꾸는 수단이 아니다. 실행 불가의 프로세스 종료값·기계 결과 조합은 발행 전 계약에 적고 CodeRules 접점이 필요할 때 Rules와 합의한다.
- 구조 이동과 기본 선택·선행조건/진단 등 동작 변경은 다른 커밋으로 분리한다. 현행 「주석과 문서」 정책을 유지한다. 작은 작업 검증 예외·규칙 문서 가지치기·사람용 코드 따라읽기 문서는 도입하지 않는다.

## 관찰 가능한 완료조건

| 조건 | 필요한 실제 근거 |
|---|---|
| CodeGraph 미설치에서도 기본 Roslyn 준비·실행·검사가 성립 | node_modules와 npm cache를 복사하지 않은 전용 clone/WSL 공간에서 실행. 선택 기록, argv, 종료값, snapshot과 입력 hash. 원본 설치본을 지워서 시험하지 않는다. |
| Python 계약 검사는 설치 없는 환경에서 실행 | 기존 fixture와 신규 독립 테스트의 원시 결과. 과거 batch 부재 skip은 사유와 개수를 적고 성공으로 세지 않는다. 기존 expected failure를 숨기지 않는다. |
| CodeGraph를 명시하고 설치가 없으면 실행 불가로 분류 | bundle/Node/필수 파일 누락 음성 사례. 기계 상태·사용자 메시지·수리 명령이 원인과 일치하고 Roslyn fallback이나 빈 성공이 없음. |
| CodeGraph와 비교 경로가 실제로 재사용 가능 | 보존된 설치본을 격리 공간에 복사해 양쪽 실제 추출·정규화·검증·비교 실행. raw 종류·cold/warm·설정/입력 provenance와 필요한 결과가 존재함. |
| 보존 자료와 원본이 유지 | package/lock, 원본 node_modules/cache의 사전·사후 inventory/hash, frozen 입력, 과거 원시 근거, 제품/Unity 생성물과 Git diff 확인. 검증한 범위를 적고 전역 불변으로 확대하지 않음. |
| 안내와 구조가 읽기 쉬움 | 기본 실행·adapter 실행·설치 수리·비교 재현 명령, 알려진 partial/미실행 한계를 코드 가까이 제공. 독립 Opus가 책임 경계·중복·오류 회복 안내와 경로를 실사. |
| 통합 근거 완성 | 이전 goal 기록 이관까지 포함한 최종 diff 실사, 관련 서식/기존 CI 결과, 독립 판정 전문과 실패/미실행 범위. 모든 PR의 병합 직전 사용자 명시 승인. |

## 단계·커밋·독립 검증 계획

1. PR166 병합 후 메인이 목표 발행을 확정하면 최신 main SHA·작업 트리와 동시 쓰기 소유권을 다시 확인한다. 승인된 checkout에서 최신 main 기준 설명형 새 브랜치를 만들고, 이전 두 커밋의 실제 변경 경로가 이전 goal.md 하나뿐인지 재확인한다. 현재 이관은 미실행이다.
2. `d5fb467` 다음 `69c6224`를 순서대로 문서 이관한다. 제안은 두 패치를 `cherry-pick --no-commit`으로 적용하고 첫 문서 커밋 하나로 정리하는 것이다. 충돌 시 현재 main과 이전 goal 기록만 대조하고, 다른 파일 변경을 함께 넣거나 유실하지 않는다. 동일 내용이 먼저 main에 들어갔는지도 확인한다. 이관된 기록은 다음 PR의 독립 문서 실사에 포함한다.
3. 새 goal을 정본으로 확정하고 CURRENT에는 자기 goal 링크만 둔다. R-7 Fable goal 검토는 메인이 해당 시범 적용을 지정할 때만 수행한다. 그 뒤 신규 Sol(`gpt-6.1-sol`)에 먼저 동작 보존 구조 분리 계약을 발행한다. 쓰기 종료·정산 후 신규 Opus(`claude-opus-5-5`)가 실제 diff/자체 근거를 실사하고 독립 테스트를 작성·실행한다. 구조 커밋을 먼저 확정한다.
4. 다음 신규 Sol은 확정 계약에 따라 기본 Roslyn 선택, adapter opt-in, 선행조건과 수리 안내의 동작 변경을 수행한다. 새 Opus 한 명이 해당 계약을 독립 검증한다. 구현/검증 파일 소유권을 순차 이전하며 각 세션은 한 작업 후 정산·종료한다. 제품 결함은 번호로 반환하고 검증자는 제품 코드를 고치지 않는다.
5. 검증은 helper mock만으로 끝내지 않는다. 선택하지 않은 adapter의 환경 의존성을 끊는 음성 사례와 실제 Roslyn/CodeGraph 실행을 모두 포함한다. SDK는 global.json의 고정값, WSL은 goal 전용 clone과 marker/lock·격리 CLI/NuGet state를 사용한다. 원본 Unity DLL 복사 부작용을 피하고 실행 전 DEVELOPMENT의 조건을 다시 읽는다.
6. 기본 계약 테스트의 현재 명령 후보는 `python3 -B -m unittest discover -s 99_Tools/Architecture.Tests`다. 실제 추출 명령은 확정 CLI와 격리 경로로 기록한다. 새 기능 테스트를 CI에 연결할 필요가 생기면 Rules/현재 workflow 소유자와 합의해 별도 소유권을 명시한다. 기존 Roslyn 서식 등록은 기능 테스트 자동 연결을 뜻하지 않는다.
7. 후속 메인 전달 결정 `msg_da3f8f595533`(원문 `failure-retry-decision.json`)에 따라 같은 계약·같은 결함 번호의 Sol FAILED/독립 NOT PASS 3회가 확정되면 이유와 실패 원문 3개 경로를 메인에 status로 알리고, 회신을 기다리지 않고 승인된 재시도 절차를 진행한다. 같은 산출물의 FAILED/NOT PASS는 이중 계산하지 않으며 조사 전용과 개발 중 자체 smoke 수정은 제외한다. 실패한 세션은 정산·종료하고, 네 번째 시도는 신규 `gpt-6.1-sol` xhigh가 맡는다. Astra는 자기 pane 아래 신규 `claude-fable-5-1` Advisor를 R-5·R-6으로 열며 읽기 전용과 조언 파일 하나만 허용한다. 새 Sol은 구현 전에 Orca 메시지로 Advisor에게 직접 질문하고 조언의 채택/기각 이유를 보고한다. Advisor는 제품 코드·테스트를 쓰지 않고 Astra 구현 격상도 하지 않는다. 지정 모델 불가·첫 화면 effort 선택창은 대체/입력하지 않고 메인에 보고한다. 네 번째 시도도 실패하면 다섯 번째 전에 question으로 메인에 올린다. 이는 메인이 전달한 사용자 사전 승인 규칙이며 지금 세션을 새로 발행하는 권한은 아니다.
8. 최종 판정·필요 원시 근거·미실행/위험을 메인에 status/question으로 전달한다. 메인의 R-2와 PR별 사용자 병합 승인 후 통합한다. PR 병합과 결과 기록 뒤 신규 Opus Gardener(보고서 한 파일만 쓰기)가 파트 소유 결함·CI 실패·새 억제/우회·드리프트 결과에서 최대 두 후보와 검사 전환 방법을 제안한다. 없으면 없음, 실행 불가와 실제 문제를 구분한다. 채택은 메인을 거친 사용자 결정이며 2026-10-31 무렵 4주 파일럿을 평가한다. 이후 메인이 R-8 세션 교체한다.

## 다음 목표 후보: 모듈 경계 검사

이번 정돈을 별도 작은 목표로 끝낸 뒤 최신 main에서 설계한다. 기존 전달의 순서·후보를 유지하며 아직 규칙이나 구현을 확정하지 않는다.

- 전송/프레이밍→게임 규칙 의존 금지, 시스템 간 직접 호출, Client→ClientNet→Shared←Server 방향을 후보로 실측한다. 허용 예외·검사 단위(assembly/type/member)·생성물·미해석 관계 처리와 source→target 근거를 메인 검토로 확정한다.
- warning 파일럿을 기본으로 기존 위반의 수·파일·관계를 공개 기준선에 남긴다. 자동 baseline/억제로 기존 위반을 숨기지 않는다. 생성 실패·partial·미해석과 확정 위반을 구분하며 수정 방향을 제공한다. error 승격은 실측 뒤 별도 결정이다.
- Architecture는 모듈 의존 방향·snapshot/adapter를 맡고, GameDev는 코드 내부 틱 블로킹 패턴·ratchet을 맡는다. Rules의 CodeRules 결과 형식·CI 연결·문서 경로 드리프트와 접점을 조율한다. 지금 예상 API나 다른 파트 파일의 쓰기 권한을 고정하지 않는다.
- 시스템 도식 뷰어와 Management codeReference 조인은 그 뒤 같은 snapshot·계층 정의를 재사용하는 후보다. DB 연동 선행 경로와 파일/자원을 침범하지 않는다.

## 역사 기록 보충과 현재 한계

PR165 병합 주체는 미확정이다. 메인은 Codex·Claude 세션 기록, computer-use, PowerShell 기록 전수 검색에서 병합 명령 기록 0건이며 사용자 승인 16초 뒤 사용자 계정으로 병합됐다고 전달했다. 이전 Astra의 goal에는 병합 명령을 실행하지 않았고 조회 때 이미 MERGED였다고 기록돼 있다. 새 Astra는 이 검색을 독립 수행하지 않았다. 승인 HEAD `8023152`와 merge `9633354` 및 성공 CI에 관한 기존 기록을 보존하며 주체를 추정하지 않는다. 이 보충은 이 초안에 한 번 남겼고 정식 goal 이관 때 출처를 유지한다.

이전 goal의 Astra 구현 격상 선택지는 당시 이력이다. 현재는 위 재시도 단계에 기록한 후속 결정이 임시 규칙의 미결 부분을 대체하며, 정식 AGENTS·orca-work·ORCA 반영은 Rules 문서 정비 목표가 맡는다. 미제출 draft·추천 문구는 사용자 직접 입력이 아니며 작업 지시나 pane 종료 보류 근거로 삼지 않는다. 미제출 공식 작업 계약은 recovery 절차로 구분한다. heartbeat 태그 누락은 재계획 결정 전 from_handle·Task·Dispatch로 출처를 판단하며 매번 교정하지 않는다.

초안 준비와 현재 착수 단계에서는 소스·Git·전달 근거 확인 및 문서 이관만 수행했다. 설치·추출·테스트·build·Unity·게임·DB·새 CI는 미실행이며 독립 판정은 아직 없다. 기존 제품 비차단 관찰/expected failure와 Unity partial을 해소로 보고하지 않는다. 아래 실행 기록과 각 단계 계약에서 실제 상태·정확한 파일 소유권·CLI/결과 계약을 갱신한다. 위 단계 계획의 미래형은 이력과 구분한다.

## 실행 기록

- 최초 구조 단계: PowerShell 두 파일은 공백만 수정하고, Python CodeGraph adapter의 책임을 기존 Pipeline 안으로 옮긴다. 기본 실행·설치·옵션·snapshot·공개 import·프로세스 수명은 보존한다. 동작 변경은 다음 단계다. 정확한 소유권·원문 규칙은 근거 폴더 `structure-contract.md`에 있다.
- 사전 메모는 `astra-context.md`, 이후 구현/검증자의 메모·원문 보고는 각 작업 근거 폴더에 둔다. 판정 전문과 원시 명령/exit를 요약과 구분한다.
