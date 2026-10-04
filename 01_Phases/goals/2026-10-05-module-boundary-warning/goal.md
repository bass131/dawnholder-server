# 서버 모듈 경계 warning 시범

상태: **두 차례 PC 크래시의 부분 결과를 보존한 뒤 신규 Sol이 구현과 자체 점검을 완료했다. 현재 main·작업본 실측과 쓰기 종료를 확인했고 신규 Opus 독립 검증을 준비한다. 독립 판정·실제 PR checkout/CI는 아직 미실행이다.** CodeMap 역할은 분석·검사이고 태그는 정본 반영 전까지 `[Architecture Astra]`·`[Architecture Sol]`·`[Architecture 검증자]`를 유지한다.

## 원천과 기준

- 메인 재개 지시 `msg_33bba3450aad`(2026-10-04T14:56:48Z), 실제 발신 `term_145bd5f0-00a7-4887-a187-16bf935db560`와 현행 메인 terminal을 대조했다. 전달된 사용자 결정을 직접 사용자 입력으로 격상하지 않는다.
- 승인 순서 원문: 「4) 범위 - CodeMap: 종료 기록 PR → 모듈 경계 검사 warning 시범 → 기능 테스트 CI 시범 → A 승인」.
- 읽기 전용 원천: `C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-04/`의 `HANDOFF.md` 오늘 확정 결정 1~15, `plan-scopes-draft.md` CodeMap 절, `routing-draft.md`, `deadline-roadmap-draft.md`.
- 작업 공간: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/architecture-active`. 브랜치 `feat/module-boundary-warning-20261005`, 시작 base/HEAD `955002a932925ff2c4ac81f4a5a99f2024a4b9b2`(fetch한 origin/main). 실행 기준 main은 측정 직전에 다시 resolve하여 SHA를 원시에 남긴다.
- 선행 발행: [176 - CodeGraph 종료 기록](https://github.com/bass131/dawnholder-server/pull/176), head `d34b39d32acfa017912354aee40c52be730a1e8f`. 사용자 개별 승인 전달 `msg_d3f9773b7353` 뒤 2026-10-04T15:32:05Z에 merge `11aa4b83131bc6349f186a141cfea9c58d2230e3`로 병합했다. main push .NET run `37213367199`의 전체 job/step success를 원시 `2026-10-04-codemap-resume/pr176-main-ci-final.json`에서 확인했다. 이 기록은 이 goal의 구현 검증이 아니다.
- PR176 정정 포인터(메인 `msg_00f588e41468`/`msg_6a86e2ba3c2c`): 이전 goal은 커밋·발행·병합됐고 `msg_33bba3450aad`로 동결이 해제됐다. 이전 CURRENT의 O6는 O-6을 가리킨다. 과거 원문의 당시 상태를 현재 상태로 읽지 않는다.
- 사전 맥락과 전달 원문: `.backups/verification/2026-10-04-codemap-resume/astra-context.md`, `main-request.json`. 이후 이 goal의 원시·계약·독립 판정은 `.backups/verification/2026-10-05-module-boundary-warning/`에 둔다.

## 범위

### 만들 것

서버 계층 간 허용 의존 방향과 위반 판정을 명시한 규칙 파일, 현재 checkout을 검사하는 진입점, PR마다 실행되어 위반을 warning으로 표시하는 CI job을 만든다. 파일/심볼·위치·의존 방향·규칙 ID·수리 안내를 출력하고 규칙 위반과 검사 실행 불가를 구별한다.

첫 규칙의 경계는 `02_Server/GameServer/Handlers`, `Sessions`, `Maps`를 중심으로 정한다. `Handlers → Sessions → Maps`는 메인 지시의 예시이며 모든 실제 의존을 일괄 금지하는 확정 규칙으로 쓰지 않는다. `FEATURE_MAP`과 `ARCHITECTURE`의 요청·상태 소유 계약을 근거로 허용/금지 방향·관측 대상 참조 종류·제외와 한계를 명시한 뒤 테스트로 고정한다. 구조 의존을 검사한 결과를 틱/actor 소유권이나 실제 게임 동작의 검증으로 확대하지 않는다.

현재 `99_Tools/Architecture/README.md`의 도구는 동결 manifest 비교용이다. 이번 완료조건에 필요한 현재 PR/main 소스 입력 경로를 별도로 마련하고 기존 Roslyn 자산의 재사용 경계를 설계한다. 비교용 frozen manifest·정답·과거 근거를 현재 checkout에 맞춰 바꾸지 않는다. 구체적인 추출 방법과 정확 파일 목록은 아래 설계 점검 뒤 외부 작업자 계약에 고정한다.

### 건드릴 곳

- CodeMap 제품 도구와 규칙: `99_Tools/Architecture/` 안의 모듈 경계 검사 책임, 필요한 현재 입력/Roslyn 연결과 코드 가까운 `README.md`. 기존 도구를 CodeMap 이름으로 일괄 이동하지 않는다.
- 요구사항 테스트·fixture와 독립 테스트: `99_Tools/Architecture.Tests/`의 새 검사 관련 파일. 구현자 TDD 파일과 독립 검증자 파일의 쓰기 소유를 계약에서 구분한다.
- CI: 신규 `.github/workflows/module-boundaries.yml` 제안. 기존 `.github/workflows/code-rules.yml`·`dotnet-tests.yml`은 수정하지 않는 경계로 Rules와 조율 중이다(`msg_19e875227fe4`). 합의 전 workflow 쓰기를 시작하지 않는다.
- 기존 CI의 명시 등록 보존: `99_Tools/Formatting/independent-projects.json`에 새 Architecture 검사 프로젝트 항목 하나만 추가한다. CODE_CONVENTION 「C# 공백 서식」의 파트별 자기 도구 등록 권한을 따르며 Formatting 검사 코드는 변경하지 않는다.
- 목표/탐색: 이 goal과 승인 후 `CURRENT.md`의 해당 goal 링크. 관측된 제품 위반은 코드 주인에게 근거와 함께 전달하여 그 파트의 BACKLOG에 연결한다. 다른 파트의 문서를 임의로 수정하지 않는다.

### 하지 않을 것

정책 위반의 error 승격, required check·branch protection 변경, 제품 코드 리팩토링, 서버/Unity/DB/프로토콜/공유 DLL 변경과 실행, 패키지/SDK 임의 업그레이드, CodeGraph 재설치·npm cache 정리, 기존 동결 분석 결과/채점 기준 변경을 하지 않는다. 새 전역 설정이나 상시 서비스도 만들지 않는다.

운영툴 vitest·기존 Architecture suite·DB 오프라인 PowerShell을 포괄하는 **기능 테스트 CI 시범은 다음 goal**이다. 이번 checker 자체의 요구사항/독립 테스트는 이번 완료조건에 포함하지만 기존 기능 테스트 전체를 CI에 옮기지 않는다. Unity 라이선스 문제를 이번에 해결하지 않는다.

### 관찰 가능한 완료조건

1. 규칙마다 소유 경계·허용/금지 방향·판정하는 참조 종류·제외 사유와 원천을 찾을 수 있고, 허용/금지 fixture가 요구사항에 근거한다. 코드 위치를 바꿔 검사를 피하는 등 경로 분류 한계도 공개한다.
2. 실행 직전 고정한 실제 main SHA 전체 대상에 진입점을 실행하고 대상 파일 수·분석 커버리지·실행 상태·규칙별 위반 목록을 원시 결과에 남긴다. 0대상/미해석/도구 실패를 0위반 PASS로 바꾸지 않는다. 숫자는 원시 결과 파일에서 읽는다.
3. 독립 검증자는 규칙별 위반·허용 사례를 소스와 표본 대조하여 오탐/미탐·미판정과 표본 선정 범위를 기록한다. 해당 범위 밖의 전체 오탐 0을 주장하지 않는다. 발견된 제품 위반은 고치지 않고 코드 주인에 인계한다.
4. 실제 GitHub PR에서 해당 head/검사 checkout SHA를 구분해 job 기동·완료·warning 표시와 결과 artifact를 확인한다. 실제 위반이 0이면 warning 렌더링은 명시적인 테스트 fixture로 별도 확인하고, 실제 소스 실측 수와 합산하지 않는다.
5. 위반은 warning 시범으로 유지한다. 정상 검사 0위반/위반 있음과 입력·도구·분석 실패를 결과/exit/CI 상태에서 구별한다. 세부 실패 정책은 설계 점검에서 정하며 정상 실행처럼 숨기지 않는다. branch protection 설정은 바꾸지 않는다.
6. 같은 조건에서 main과 PR 검사에 사용한 명령·도구 버전·입력 SHA·종료값·실행 시간을 원시에 남긴다. 비용 상한과 중단 시 결과는 구현 전 결정하며, 측정 전 추정치를 실행 시간으로 쓰지 않는다.
7. TDD의 구현 전 실패 근거와 구현 뒤 통과 근거, 신규 Opus의 실사·독립 테스트·실제 진입 실행을 보존한다. 기존 동결 비교 경로에 영향이 있으면 해당 보존 회귀도 검증하고 미실행을 구분한다.

## PR 경계와 점검

새 goal은 **도구·규칙·전용 workflow·해당 테스트·필수 안내를 하나의 기능 PR**로 묶는다. 구조 정리가 필요한 경우만 구조 보존과 정책 변경을 별도 커밋으로 나누며 무관한 정리를 넣지 않는다. 종료 기록 PR176은 별도이며 내용 변경이나 그 승인을 이 PR로 넘기지 않는다.

점검은 메인 범위 확인 → 오류 분류/비용 상한의 설계 불변식 → 외부 Sol의 요구사항 TDD·구현 → 쓰기 종료 → 신규 외부 Opus 실사·독립 테스트 → 실제 PR CI 원시 대조 → 메인 R-2와 PR별 사용자 병합 승인 순서다. 범위 안 완료조건을 막는 결함만 수정 루프에 넣고 같은 산출물 수정 3회 초과 때 메인에게 체크포인트를 알린다. goal 종료 뒤 다음 기능 테스트 CI goal을 자동 시작하지 않는다.

검증 등급은 **강**이다. CI/검사 진입점은 설치·실행·I/O 도구이고 오류 분류·비용 상한에도 닿는다. 구현은 `gpt-6.1-sol` max, 독립 검증은 신규 `claude-opus-5-5` 외부 Orca 세션이다. 내부 subagent를 구현/독립 판정 대신 쓰지 않는다. 메인 `msg_c123b85e69af`에 따라 이번 goal은 Fable 시범 대상이 아니며 오류 분류·비용 상한은 아래 불변식과 신규 Opus의 설계 관찰로 다룬다. 지정 모델이 없으면 대체하지 않는다.

## 범위 확인과 소유권 조율

메인 `msg_c123b85e69af`는 승인 초안과 네 범위·단일 PR·강 등급·TDD를 대조한 뒤 확인했다. 예시 방향을 곧바로 금지 규칙으로 삼지 않는 판단과 현재 입력 경로를 동결 자산에서 분리하는 설계도 확인했다. Fable 시범은 Content 첫 goal·GameDev 인스턴스 맵 수명·GameDev 게임 저장 고리 세 개에 한정되므로 이 goal을 제외한다. 사용자 시범 전체를 취소한 결정으로 확대하지 않는다.

Rules `msg_9396505985f8`는 전용 module-boundaries.yml과 현재 Rules 문서 작업 사이 충돌 없음을 확인했다. `msg_eb05b7c56111`은 independent-projects.json에도 현재 쓰기 소유가 없으며 각 파트의 자기 도구 등록 경계를 재확인했다. 이는 소유권 대조이며 검사 정책/required check 승인이 아니다. 신규 등록 필요와 파일 경계는 메인 `msg_522fb25ae2f0`에도 알렸다. 원문은 `.backups/verification/2026-10-04-codemap-resume/`의 main-scope-approved.json, rules-workflow-ownership.json, review-done-delivery.json에 있다.

## 첫 구현 설계와 불변식

별도 SDK Roslyn 검사 프로젝트 `99_Tools/Architecture/Boundaries/Architecture.Boundaries.csproj`를 둔다. 현재 서버 프로젝트와 그 참조를 design-time compilation으로 읽으며 제품 실행/emit/build·DLL 복사를 하지 않는다. 기존 동결 Roslyn Program은 Unity/ClientNet을 가정하고 기존 RelationExtractor의 usesType은 메서드 본문 중심이므로, 필드·시그니처까지 포함할 새 경계 검사 진입에 그대로 재사용하지 않는다. SDK 참조·Workspace 선택 관례는 재사용하되 기존 파일의 의미와 frozen 결과는 보존한다.

초기 규칙은 다음 세 개의 warning 시범이다. 정책 원천은 FEATURE_MAP의 Handler→GameSession.Submit→Maps 흐름과 ARCHITECTURE의 요청/상태 변경, 실제 GameSession.OnRecvPacket의 dispatcher 계약이다. 규칙 파일에 이 근거와 허용/제외 이유를 함께 남긴다.

| 규칙 | 경고 대상 | 보존 동작 |
|---|---|---|
| MB001 | Handlers의 Maps 타입/멤버 직접 의존 | Handlers→Sessions 제출 |
| MB002 | Maps의 Handlers 타입/멤버 의존 | Maps→Sessions의 기존 송신·연결 참조 |
| MB003 | Sessions의 구체 Handlers 타입/멤버 의존 | 정확한 HandlerRegistry/IPacketHandler dispatcher 타입은 허용 |

대상은 이 세 폴더의 컴파일된 수기 소스에서 Roslyn이 해석한 정적 타입·멤버 참조다. using 문자열만 검사하지 않고 alias/완전 수식/필드/시그니처/호출의 정적 참조를 같은 규칙으로 다룬다. 외부 타입·다른 게임 영역·자기 영역·주석·문자열·reflection/dynamic·런타임 actor 판정의 범위와 제외를 명시한다. 미해석 참조/컴파일 오류·대상 누락은 완료로 숨기지 않는다.

불변식:

- 정상 0위반과 정상 warning은 exit 0으로 구분 기록한다. 입력 오류·도구 부재·해석 실패·timeout은 별도 reason/status와 nonzero다. 검사 실패를 정책 위반으로 표시하거나 오래된 성공 결과를 재사용하지 않는다.
- 원천 root·규칙 경로·출력 root를 경계에서 검증하고 소스/기존 근거를 덮어쓰지 않는다. 새 실행은 새 빈 산출물 위치에 기록하고 삭제·자동 재측정·cache 정리는 하지 않는다.
- SDK는 global.json의 10.0.301이며 전역 PATH/설정을 바꾸지 않는다. 로컬 WSL과 CI 모두 같은 공개 진입·결과 계약을 사용한다. 서버/DB/Unity 실행, 제품 DLL emit/copy 없이 실제 진입을 한 번 이상 확인한다.
- 입력 수/크기·실행 시간에 유한 상한을 두고 그 값과 관측치를 원시에 남긴다. 기본 상한은 소스 5,000파일/64MiB, 개별 외부 단계 600초, CI 전체 job 20분이다. 상한 초과를 성공으로 반환하지 않으며 취소/timeout은 자기 작업 process만 정산한다. 이 상한은 성능 실측값이 아니다.
- source SHA·입력 hash·규칙 버전·실행 도구·명령·exit·시간은 원시에서 추적한다. PR head와 GitHub merge checkout을 구분한다. fixture warning 시연은 실제 main/PR 위반 수와 합산하지 않는다.
- 구현 전 요구사항 테스트 실패를 파일·원시로 남기고 구현 뒤 동일 검사를 통과시킨다. 독립 Opus는 자기 소유 테스트에서 허용/금지/누락/해석 실패/실행 실패와 실제 진입을 추가 검증한다. 상수 결과·제품 계산 복제로 통과시키지 않는다.

## 정본 반영 전 적용 중인 사용자 결정

아래는 메인 전달 원문과 지정 읽기 전용 자료의 사용자 결정이다. Rules 정본 반영 뒤에는 해당 정본 링크로 연결하며 이 goal을 전역 규칙의 새 정본으로 만들지 않는다.

| 원문/결정 | 출처 | 이 goal 적용 |
|---|---|---|
| 「4) CodeMap: 종료 기록 PR → 모듈 경계 검사 warning 시범 → 기능 테스트 CI 시범 → A 승인」 | plan-scopes-draft.md 머리·CodeMap 절, msg_33bba3450aad | 순서와 네 범위, 개별 PR 경계 |
| 「2) 약점 지도 - 기능 테스트 CI 시범을 CodeMap의 두 번째 일로 → A 예」 | 메인 msg_33bba3450aad, HANDOFF.md 결정12 | 다음 goal로 분리 |
| 범위 4항목·착수 전 확인, 「OK 그렇게 가자」 | HANDOFF.md 결정1, msg_33bba3450aad | 메인 확인 전 구현 보류, 수정 3회 초과 체크포인트 |
| TDD 새 goal부터, 「A」 | HANDOFF.md TDD 항목, msg_33bba3450aad | 구현 전 요구사항 테스트 실패부터 기록 |
| 「2) 설계 4범주 작업은 구현 전에 Fable이 불변식 목록 작성(시범) → A 시범 도입」 | routing-draft.md 머리·결정2 | 오류 분류/비용 상한 검토, 메인 시범 배정 확인 |
| 「3) Sol effort 시험은 보류하고 max 유지 → A 보류」·「5) 검증 강도 2등급 4주 시범 → A 시범 도입」 | routing-draft.md 머리·결정3/5, msg_33bba3450aad | Sol max·강 등급, 10-31 재평가 |
| 계약 경로 확인·예문 대신 요구 항목·해당 없는 절 이유 명시·harness 파일화·설계 결정 시 한 줄 대안·수치는 원시에서 | routing-draft.md 권고6개, HANDOFF.md 결정13, msg_33bba3450aad | 발행 전 실제 경로 확인, 해당 규칙 원문만 포함, 판정별 Assert 변수 |
| 「4) 작업별 자동 기록 범위 → A 둘 다」 | routing-draft.md 머리·결정4 | 판정 표 verifies/task·결함 번호/심각도/차단/귀속과 설계 관찰 절. 자동 토큰 helper 구현은 범위 밖 |

보고 수치·실행 도구의 실제 진입 요구, 사용자 개별 PR 병합 승인과 완료 뒤 자동 착수 금지는 메인 msg_33bba3450aad를 그대로 적용한다. 졸업작품 마감의 10-28 기능 동결 목표와 도구 파트 승인 순서 유지 결정은 deadline-roadmap-draft.md 머리 및 HANDOFF.md 결정14를 따른다.

별도 **메인 운영 결정** `msg_90ffd91a86bb`(2026-10-04T16:49:33Z)은 Orca1.4.217에서 공식 blocking ask의 고정 subject `Question`과 subject 옵션이 없는 공식 reply의 R-3 예외를 한시 적용한다. body 자기 태그와 현재 from_handle·Task·Dispatch 대조는 유지하며 일반 send의 subject 태그는 면제하지 않는다. 수신 helper를1.4.218로 가장하지 않고 해당 버전에서는 원시를 수동 대조한다.1.4.218 이상 업그레이드 또는 subject 옵션 지원 시 이 예외는 끝난다. 근거는 `crash-recovery-2/r3-1217-main-decision.json`, `orca-ask-help.txt`, `orca-reply-help.txt`다. 구현 중 고정 입력은 바꾸지 않고 쓰기 종료 뒤 이 단락에 반영했다. 이를 사용자 직접 입력이나 전역 정본 변경으로 표현하지 않는다.

## 재개 지점

메인 확인과 Rules 소유 회신 뒤 Task `task_b266e9eb59bb`를 Sol에 발행했다. 구현 전 TDD 원시 `implementation/work/red/command.json`은 22 tests, failures 21/errors 1, exit 1이며 실제 공개 진입 부재 등으로 실패했다. 이는 구현 전 실패 근거이며 green 또는 독립 통과가 아니다. 아래 두 복구는 당시 이력이며 최종 완료 근거는 마지막 「구현 종료와 검증 인계」에 둔다. 앞선 implementation/·implementation-recovery-1/의 기록을 새 작업자의 실행으로 소급하지 않는다.

### 2026-10-05 크래시 복구

메인 `msg_41295b025418`(2026-10-04T16:00:09Z)은 2026-10-05 00:45 KST 블루스크린(0x3B)으로 모든 프로세스가 종료됐고 사용자 전달 결정 「지금 재개하고 재발을 지켜봄」에 따라 재개하라고 지시했다. 이전 Sol `term_857ddf96-8b91-4bbe-839a-6c7df3138689` / Dispatch `ctx_33011d2c9783`은 TDD red 후 구현 중이었으며 쓰기 종료·worker_done이 없다. 디스크의 Boundaries/·공개 .py/.sh·TDD 및 support 14파일은 **크래시로 중단된 부분 결과**이고 `crash-recovery/partial-files/`와 `partial-manifest.json`에 원본과 hash를 보존했다. 자동 복구의 failed/abandoned 표시는 같은 계약·결함의 확정 실패 집계에 넣지 않는다. Astra는 현재 handle `term_c4cec985-1da6-448b-bbf8-975b3e4b0b69`에서 기존 Run `run_04e869ec070e`를 generation 2로 재연결했고 메인에게 `msg_fed3186fd0d3`으로 확인 회신했다. 죽은 세션을 재사용하지 않고 신규 gpt-6.1-sol max에 같은 Task의 retry를 발행한다. 기존 계약 v1은 `crash-recovery/implementation-contract.v1.md`(SHA256 `C162C702EE6609A826D1B05E22D532DE71C277C9F48E52F194BD7948F0CECAF6`)에 보존하고 현재 implementation-contract.md의 복구 머리말과 새 recovery-input-manifest.json을 적용한다. 독립 검증은 최종 쓰기 종료 뒤 신규 Opus가 처음부터 수행한다. 쓰기 종료된 결과의 로컬 checkpoint commit만 추가 허용됐으며 push·PR·병합 권한은 기존대로다.

위 근거의 상대 경로는 `.backups/verification/2026-10-05-module-boundary-warning/` 기준이다. 다음 단계는 신규 Sol 준비/모델/최초 attach 관측 → 부분 결과의 남은 구현·실측 → 쓰기 종료 → 신규 Opus의 실사·독립 테스트 → 실제 PR CI와 메인 보고다. 다음 기능 테스트 CI goal은 자동 시작하지 않는다.

### 두 번째 중단과 복구

메인 `msg_db6123bf7067`(2026-10-04T16:40:31Z)은 2026-10-05 01:15 KST 두 번째 블루스크린(0x44)과 기존 작업자 종료를 확인하고, 사용자 전달 결정 「둘 다 하자, 안랩 세이프 트랜잭션도 지우고, Orca도 이전버전으로 다운그레이드하자」에 따른 환경 조치 뒤 작업을 재개하라고 지시했다. 신규 Sol이었던 `term_7f575b9d-e177-4ac5-a5b0-940c52996be1` / `ctx_d2ab7915063a`도 완료 보고 없이 중단됐으며 `implementation-recovery-1/`의 메모·중간 실행은 보존하고 완료 판정으로 쓰지 않는다. 14개 부분 제품 파일은 `crash-recovery-2/partial-files/`와 manifest에 다시 고정했다. Orca1.4.217/runtime `c37fa9b2-410f-4791-ac59-9ad67570b6ef`의 가이드와 실제 명령을 확인하고 Astra `term_a9aa8dd7-e5ba-43fb-b6f5-5efa57fad718`에서 같은 Run을 generation3으로 재연결했다. 메인에게 `msg_3f410a202e0e`로 착수 전 상태를 회신했다. 두 크래시 모두 확정 결함 실패 집계에서 제외하고, 같은 Task의 신규 Sol max가 현재 계약의 복구 r2와 `recovery-2-input-manifest.json`을 적용한다. 이번 첫 메모·보고·실행 원시는 `implementation-recovery-2/`이며 앞선 두 구현 시도의 근거는 읽기 전용이다. 이전 r1 계약 원본은 `crash-recovery-2/implementation-contract.r1.md`(SHA256 `96192ADE5538EDF31DBC3429A34FF49F07ADC92D623593FFD104C3D4F6A73C41`)에 보존했다. 범위·완료조건·독립 검증·PR별 병합 승인 규칙은 그대로다.

### 구현 종료와 검증 인계

최종 신규 Sol은 `term_29b12d78-055e-4ecd-bc9c-6bf29e5cc717` / Dispatch `ctx_6a3d879452a9`이며 Task는 위와 같다. 최초 명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=max`, 화면 GPT-6.1-Sol max, backend unknown으로 구분한다. 2026-10-04T17:38:08Z `msg_5aeb72e7b029`의 succeeded·쓰기 종료와 `implementation-recovery-2/report.md`를 대조했다. release 뒤 동일 incarnation·idle·화면을 확인하고 해당 pane을 닫았다(`implementation-close.json`, ptyKilled true). 이전 두 크래시 dispatch의 비정상 중단을 이 정상 정산으로 대체하지 않는다.

동일 산출물의 기능 수정 세 차례 뒤 가독성 쓰기를 네 번째로 진행하는 checkpoint는 `msg_13966bbd2f5e`·메인 보고 `msg_459430abbbe8`·진행 회신 `msg_f4fa153cd24f`와 `crash-recovery-2/fourth-write-checkpoint.json`에 남겼다. 요구사항·기대값·범위 변경 없이 진행했으며 개발 중 red/smoke와 인프라 중단은 확정 실패 집계에 넣지 않는다. 현재 확정 실패는 0회다.

최종 자체 테스트는 동일 공개 unittest 명령으로 28건/실패0/오류0/exit0이고 원시는 `implementation-recovery-2/work/green-final-2/`다. 외부 명령 경과시간은57.795초다. 최초 red, 복구 baseline22건 중10실패, red-2의4실패 및 각 수정 분류를 보고서와 원시에 보존했으며 기대값 완화로 성공시키지 않았는지는 신규 Opus가 독립 판정한다. Astra는 최종17파일의 hash와 보고 manifest 일치를 기계 대조했다.

| 자체 실측 입력 | source SHA / 입력 모드 | 실제 범위와 결과 | 검사 내부 / 외부 명령 시간 |
|---|---|---|---|
| main 원본 | `11aa4b83131bc6349f186a141cfea9c58d2230e3` / git_blobs | C#91파일477,096bytes, Compile91/91, boundary46/46, MB001/2/3 각0, clean/exit0 | 54.273초 / 54.532초 |
| 현재 작업본 | `270406d64e62cefc542cbdd8f9a3b9f90ad49f3f` + 미커밋 도구 / workspace | 같은 C#·coverage·규칙 결과, 실제 PR checkout은 아님 | 42.954초 / 43.208초 |

최종 도구 input hash는 두 실행 모두 `263ad09bf0bec0640467684f2c4bce2371a7884910d48493e774187fdf86877d`다. 원시는 `work/main-2/`, `work/current-2/` 및 각각 `main-command-2/`, `current-command-2/`이고 main resolve는 `main-resolution-2.json`이다. 두 입력 수집 방식이 다르며 프로젝트/props 줄바꿈으로 입력 hash도 달라 속도 개선으로 해석하지 않는다. 별도 fixture는 MB0018·MB0023·MB0031의 총12 warning/exit0을 냈다(`fixture-annotation-observations-2.json`); 실제 source 위반0과 합산하지 않는다.

WSL linked worktree의 Git metadata 경로로 최초 실제 진입이 실패한 원시도 보존했다. 최종 도구는 drive mount/backlink를 대조하고 Git pointer·index·설정을 쓰지 않는 read-only 경로를 사용한다. 제품 build/emit·DLL copy·서버/Unity/DB 실행은 수행하지 않았다. workflow 정적 검사와 로컬 annotation 문자열은 실제 GitHub PR job/표시를 대신하지 않는다. 다음은 고정된 구현 입력에 대한 신규 Opus 실사·독립 테스트, 이후 PR의 정확한 head/checkout·실제 CI 확인과 메인 보고다.
