# 정적 관계 추출기 비교와 스냅샷 계약

내부 목표 ID: A-1. **현재 상태: 정식 goal 작성, 메인 검토 대기. 설치·시범 구현·정답표 동결·독립 검증은 미실행이다.**

## 목표와 결정 출처

CodeGraph와 SDK 동봉 Roslyn을 동일한 실제 코드 표본으로 비교해 정적 관계의 정확도, 누락과 오인, 실행 비용을 확인한다. 운영툴에서 사용할 스냅샷 계약 초안을 만들고 추출기 선택 근거를 제공한다. 최종 추출기 선택은 결과를 본 사용자에게 맡긴다.

메인 요청은 `msg_b07ae67f4d45`와 발신 표기 정정 `msg_6c699283e2d2`다. 현재 메인 handle과 두 메시지의 `from_handle`을 대조했다. 원문은 로컬 `.backups/verification/2026-10-02-architecture-extractor-comparison/main-entry-messages.json`에 있다. 메인이 전달한 사용자 결정과 이 세션의 직접 관측을 구분한다.

- 사용자 원문 **“CodeGraph 승인”**은 메인 `msg_0eca5c1e009c`가 전달했다. 승인된 패키지·명령·실행 설정·시점은 아래와 같으며 재승인을 요청하지 않는다. 정식 goal의 메인 검토 뒤 신규 Sol이 실행한다.
- 전체 시스템 도식표 방향은 메인 `msg_870cb0736cfa`가 전달했다. 사용자는 운영툴 4번 항목에서 옛 Architecture visualizer의 동적 도식처럼 전체 시스템 흐름을 한눈에 보기를 원했다. 네 번째 메뉴를 뷰어 진입점으로 삼는 것과 시스템 카드로 개요를 구성하는 것은 메인의 구체화이며, 실제 실행 흐름 계측 요청으로 해석하지 않는다.
- 사용자 작성 규칙의 전달 원문: “운영툴에 작성하는 작업관련 게시글에 D1,D2같은 프로젝트에서만 쓰이는 마일스톤 명칭으로 특정 작업의 작업명을 대체하면 시간이 좀 지나고 봤을때 이해하기 어려우니까 라벨링을 자제해야해”. 운영툴 작업 글, 사용자에게 보이는 보고서 제목·본문의 작업명과 스냅샷 표시명은 구체적인 작업·시스템 이름을 쓴다. D1·M-2·A-1로 작업명을 대신하지 않는다. 내부 goal ID와 기술 추적 식별자는 유지할 수 있다.

승인·기존 합의 원문은 `.backups/verification/2026-10-02-architecture-part-rules/`의 `resume-followups.json`, `a1-dependency-approval.md`, `m2-path-proposal.json`, `m2-path-agreement.json`, `main-system-overview-direction.json`에 보존되어 있다. 같은 폴더의 `a1-goal-draft.md`는 과거 승인 전 초안이다. 이 goal의 현재 승인 조건이 기준이며, 로컬 근거는 Git 제외 자료다.

## 범위와 소유권

비교 도구는 `99_Tools/Architecture/`에 두고 제품 코드와 분리한다. 포함 범위는 읽기 입력·manifest, 두 추출기의 실행과 공통 결과 변환, 닫힌 표본의 평가 자료, 스냅샷 계약·검사, 비교 보고다. 1~2홉 그래프, `Client → ClientNet → Shared ← Server` 배치, 관계 종류별 표시, 모듈 접기, 두 지점 경로 질의, 패킷 중심 탐색과 시스템 개요를 뒷받침할 데이터가 있는지 평가한다.

| 담당 | 쓰기 범위와 책임 |
|---|---|
| Architecture Astra | 이 goal 폴더의 계획·원천 기반 정답표·계약 설명·비교 보고와 CURRENT 링크, 로컬 근거·세션 기록, 이 브랜치의 commit/push/PR. 보고서 HTML이나 전용 생성 스크립트가 필요하면 직접 작성한다. |
| 신규 Sol | `99_Tools/Architecture/`의 추출·정규화·실행·평가 도구와 로컬 ignore, 승인된 CodeGraph package/lock, 해당 작업의 원시 출력·자체 점검 기록. 수기 정답표와 판정·보고서 본문은 수정하지 않는다. |
| 신규 Opus | `99_Tools/Architecture.Tests/`의 독립 계약·실패 경로 검사와 로컬 판정 원문. 구현자의 쓰기 종료 뒤 실제 diff·보고·원시 실행 근거를 실사하고 테스트를 작성·보완·실행한다. 제품 도구를 수정하지 않고 결함 번호로 반환한다. |
| Management | 시스템 카드의 ID·계층·codeReference, 운영툴 화면. 기술 접점만 Astra끼리 조율한다. |

`02_Server`, `03_Client`, `04_ClientNet`, `98_Shared`와 기존 설정·동기화 도구는 읽기 입력이다. 원본 제품 소스·PDL·생성 패킷·DLL·Unity 자산과 `.meta`·GUID는 보존한다. `05_Management/frontend`, `CLAUDE.md`, 전역 설정은 수정하지 않는다. 기존 `99_Tools/sync-wsl.sh`의 기본 입력에는 `03_Client`가 없으므로 목표 도구 내부에서 명시적 manifest 복사를 준비한다. 기존 helper 변경이 필요하면 범위를 메인에 올린다.

화면 구현·운영툴 게시·runtime trace·상주 서비스·Unity Editor/플레이·게임 서버/7777·DB 실행은 범위 밖이다. 뷰어는 시스템 카드 작업 병합 뒤 frontend 소유권을 조율하는 후속 목표다. 내부 ID M-2는 이 의존성과 코드 접점 추적에만 사용한다.

## 설치와 실행 조건

0단계 운영 규칙은 PR163으로 병합됐다. 이 목표는 main `881957cbb431d4af822d1d935ac117e1ede6c303`에서 만든 `feat/architecture-extractor-a1-20261002`를 이어받는다. 설치의 남은 선행조건은 **이 goal에 대한 메인 검토·승인**이다. 그 뒤 [R-5](../../../00_Document/operations/ORCA.md#r5-worker-launch)대로 기동한 신규 Sol이 아래 승인 명령을 Windows 저장소 루트에서 그대로 실행한다.

```powershell
npm install --prefix 99_Tools/Architecture/CodeGraph --save-exact --ignore-scripts --no-audit --no-fund --os=linux --cpu=x64 --bin-links=false --cache 99_Tools/Architecture/CodeGraph/.npm-cache @colbymchenry/codegraph@1.6.1
```

- private `package.json`과 `package-lock.json`은 해당 폴더에 둔다. 설치 산출물 중 Git에 넣는 것은 이 두 파일이며 `node_modules`, cache, 분석 DB·원시 결과는 Git에서 제외한다. 패키지는 `@colbymchenry/codegraph@1.6.1`과 같은 버전 Linux x64 번들이다. 명령·exit·lock의 버전/integrity·실제 크기와 다운로드/설치 비용을 보존한다.
- 기존 승인안은 Linux 번들 unpacked 약 281 MiB와 wrapper 약 1.1 MiB를 관측했다. 이는 과거 메타데이터 값이며 현재 실측으로 보고하지 않는다. 추가 script/의존성, 다른 버전 또는 예상보다 훨씬 큰 크기 등 설치 결과가 승인안과 다르면 중단하고 메인에 보고한다.
- CodeGraph 실행마다 프로세스 한정 `DO_NOT_TRACK=1`, `CODEGRAPH_TELEMETRY=0`, `CODEGRAPH_NO_UPDATE_CHECK=1`, `CODEGRAPH_NO_DAEMON=1`을 전달한다. 소유 표시와 lock이 있는 전용 WSL 복제본에서 번들 `bin/codegraph`와 번들 node를 사용하고 필요한 실행 권한도 그 두 파일에 한정한다. init의 쓰기 위치는 실행 전 help·소스로 확인한다.
- 전역 설치·PATH/전역 설정 변경·`codegraph install`·인자 없는 CodeGraph 실행·agent MCP 연결·상주 serve/watch·다른 버전·추가 의존성은 승인 범위 밖이다. 패키지 다운로드 외 분석 단계의 외부 전송이나 추가 다운로드를 필요 조건으로 만들지 않는다. telemetry 설정 전달·출력 위치·잔여 프로세스는 검증 근거를 남긴다.
- Roslyn은 **SDK `10.0.301` 동봉 DLL만** 참조한다. 기존 `99_Tools/Formatting/Formatting.csproj` 패턴을 참고하되 수정하지 않는다. 새 NuGet 패키지는 없다. 필요한 Workspace API나 참조를 확보할 수 없으면 한계와 대안을 메인에 보고하고 조용히 의존성을 추가하지 않는다.
- 모든 .NET 호출은 [DEVELOPMENT의 WSL 절차](../../../00_Document/operations/DEVELOPMENT.md#wsl-빌드테스트실행)를 따른다. 첫 호출 전부터 격리된 CLI home·NuGet 경로, 인증서·전역 도구 PATH 추가 억제, 고정 SDK 확인, 원본 경로 소유 marker·lock을 적용한다. Shared/ClientNet 빌드의 Unity DLL 복사 부작용도 해당 복제본에 한정하고 원본으로 역복사하지 않는다.

## 비교 설계와 정답표 동결

### 입력과 평가 단위

분석 대상은 위 기준 main의 고정 SHA로 시작한다. 추출기 구현 HEAD와 분석 대상 `commitSha`를 분리하고, 선택한 실제 파일·해시와 해석에 필요한 project/props/참조·버전을 manifest로 고정한다. 두 도구가 같은 입력을 읽었는지 복사 전후 hash를 대조한다. 분석 입력을 바꾸면 새 manifest와 별도 결과로 기록하며 미커밋 입력을 clean commit의 결과로 표시하지 않는다.

정답표는 Astra가 **추출기 출력 없이 원천 코드만 보고 작성**한다. 두 도구의 첫 분석 실행 전에 `evaluation-scope.json`, `truth.json`, `input-manifest.json`을 이 goal 폴더에 기록하고 commit SHA·파일 SHA-256·동결 시각을 남긴다. 버전/help 조회·설치 자체와 분석 실행은 구분한다. Sol은 동결 근거가 확보되기 전 실제 입력이나 별도 표본에 추출을 실행하지 않는다. 신규 Opus는 이후 원천과 동결 이력을 독립 대조한다.

- 표본은 수기 판정 가능한 10~15개 양성 관계를 목표로 한다. source symbol·관계 종류·target domain을 열거해 **닫힌 평가 범위**를 먼저 정하고 그 범위의 양성을 빠짐없이 기록한다. 실제 양성이 목표 개수를 넘으면 임의로 빼지 않고 범위·분모를 조정해 동결한다.
- 각 정답 행은 source/target의 namespace·type·method signature·repo-relative file, 관계 종류, 코드 증거 위치를 담는다. 직접 호출, 인터페이스 구현, 패킷 타입 사용, 클라이언트 핸들러를 포함한다. 타입 사용과 런타임 패킷 전달은 다른 의미다.
- 후보는 `AttackHandler.Handle → GameSession.SubmitAttack → GameMap.ProcessAttack → CombatSystem.ProcessAttack`, `MoveIntentHandler`의 `IPacketHandler` 구현·`C_MoveIntent` 사용, `PongHandler`의 `IClientPacketHandler` 구현·`S_Pong` 사용·`EnqueueApply` 호출이다. 아직 동결 정답이 아니며 실제 선언·signature·호출 위치를 확인해야 한다.
- 서로 다른 타입의 동명 메서드를 양성·혼동 가능한 음성 후보로 포함한다. 메서드 이름만 같아도 일치로 판정하지 않는다. 오버로드/인터페이스 호출은 정적으로 해석한 선언과 잠재적 runtime 대상을 구분하고, 람다 내 호출은 소유 메서드와 deferred 문맥을 표시한다.
- 도구 출력에서 정답을 역으로 만들지 않는다. 동결 뒤 원천 판정 오류를 발견하면 원본과 사유를 보존하고 양쪽 점수를 같은 수정본으로 재계산해 수정 전후를 구분한다.

### 집계와 비용

범위 안의 추출 간선은 전부 수기 정답과 대조한다. 범위 밖 추출은 별도 개수로 보고하고 FP로 넣지 않는다. 중복 제거 기준은 정규화된 source·target·관계 종류이며 원시 출현 위치는 보존한다. 같은 이름의 다른 target은 FP이고 놓친 정답 target은 FN이다. unresolved/ambiguous 후보는 확정 TP로 계산하지 않는다.

관계 종류별·층별 TP/FP/FN과 분모, `precision=TP/(TP+FP)`, `recall=TP/(TP+FN)`을 공개한다. 분모 0은 N/A이며, 미지원 관계도 동일 정답 분모에서 FN으로 남기고 지원 여부를 병기한다. 실패·부분 출력과 분석 자체 미실행은 유효한 빈 출력과 구분한다. 이 표본 결과를 저장소 전체 정확도로 일반화하지 않는다.

서버·Shared·ClientNet과 Unity 클라이언트를 별도 표로 측정한다. compiler diagnostics, unresolved/ambiguous 수, 참조 확보 내역과 조건부 컴파일 설정을 남긴다. 로컬 Unity `6000.4.7f1` 참조의 사용 가능성을 읽기 전용으로 확인하고 필요한 파일·hash를 manifest에 넣는다. 참조를 얻지 못한 Unity semantic 결과는 **부분 해석 또는 미실행**으로 표시하고 임의 stub으로 성공을 만들지 않는다. 부분 해석의 정답 분모·누락도 숨기지 않는다.

동일 WSL 환경에서 설치·복원 비용과 분석 비용을 나눈다. 분석 cold 1회, 같은 입력의 warm 3회를 기본으로 각 시간·peak memory·파일/심볼/간선 수·cache/설정 상태를 기록한다. cold는 도구 분석 DB/cache가 없는 상태로 정의하고 OS page cache를 제거한 것으로 주장하지 않는다. 두 도구를 동시에 실행하지 않으며 실행 실패·측정 불가 지표는 N/A와 사유로 남긴다. raw 명령·stdout/stderr·exit·입력 hash로 집계를 재계산할 수 있어야 한다.

## 스냅샷 계약 초안

아래는 비교를 위한 공통 최소 계약이다. 실제 도구가 제공하는 정보와 변환 과정의 추론을 구분해 `snapshot-contract.md`와 검사 가능한 형식으로 구체화한다. 확정되지 않은 관계나 숫자 confidence를 사실처럼 만들지 않는다.

| 대상 | 최소 정보와 보존 조건 |
|---|---|
| 최상위 | `schemaVersion`, `repository`, 전체 `commitSha`, `extractor`(name/version/configHash), `analysisScope`(manifestHash, 포함·제외 root), 완전/부분/실패 상태와 `diagnostics`, `nodes`, `edges`. |
| node | 안정적인 `id`, `kind`(module/file/type/method/packet), 설명적인 표시명, namespace/signature, `layer`, `source`(repo-relative path 및 선택 line/column). packet의 protocol ID/version은 원천 근거가 있을 때만 제공한다. |
| edge | `id`, `kind`(contains/calls/implements/usesType), `sourceId`, `targetId`, 증거 위치, resolution 상태. unresolved/ambiguous 후보는 확정 간선과 구분한다. |
| 정규화·검사 | 동일 SHA/설정에서 결정적인 정렬·중복 제거·ID, endpoint 존재, schema version, 중복 ID·잘못된 SHA/path·누락 입력·incomplete 결과 처리. 모르는 schema와 잘못된 입력을 정상 complete로 받아들이지 않는다. |

### 시스템 카드와 codeReference 접점

Management와 합의한 형태는 `codeReference:{commitSha,mappings:[{path,kind,namespace?,role?}]}`다. 카드 `id/parentId`와 이 필드는 Management 소유이며 추출기는 `snapshot.commitSha`와 `node.source.path`를 제공한다. 이 합의는 양쪽 구현 완료를 뜻하지 않는다.

- 전체 Git SHA가 같은 경우에만 현재 매핑으로 조인한다. 다르면 불일치 상태를 표시한다. 구현 설명의 `sourceCommit`도 연결 카드의 `commitSha`와 맞춘다.
- `path`는 Git tree의 정확한 대소문자를 보존한 repo root 기준 `/` 상대경로다. `kind`는 file/directory이며 저장값에는 trailing slash를 두지 않는다. 절대경로·빈 경로·점/상위 세그먼트·역슬래시·URL·glob을 거부하고 OS별 소문자화를 하지 않는다.
- directory는 path 자체 또는 `path + /` 세그먼트 경계로 매칭한다. `namespace`는 설명용 메타데이터이며 필터가 아니다. 미구현/코드 없는 카드는 `mappings:[]`와 상태·사유로 표현하고 가짜 경로를 만들지 않는다.

운영툴 네 번째 메뉴의 **전체 시스템 도식표**를 받치도록 같은 SHA의 카드 계층과 코드 노드를 연결하는 예시를 계약에 포함한다. 시스템 간 관계는 원천 코드 edge ID와 관계 종류를 추적할 수 있어야 하며, 부분 분석·미매핑·중복 매핑을 구분하고 모호한 소유권을 임의로 하나에 몰지 않는다. 카드 ID·계층·다중 매핑의 해석은 Management와 확인한다. 카드 계층→시스템 개요→원천 클래스/메서드로 내려갈 수 있는 데이터 접점을 검토하되 화면과 집계 UI는 구현하지 않는다. 메인이 전달한 1단 7개·하위 39개 카드는 계획상의 구조이며 이 목표가 실사한 구현 수치가 아니다.

## 순서와 관찰 가능한 완료조건

1. **계획 검토:** Astra가 이 goal과 CURRENT를 commit해 메인 검토를 요청한다. 승인 전 CodeGraph 설치·작업자 발행은 하지 않는다. Fable 시범은 이 요청에 포함되지 않았으므로 자동으로 시작하지 않는다.
2. **평가 기준 동결:** 승인 뒤 원천만으로 닫힌 평가 범위·정답표·동일 입력 manifest를 작성·commit한다. 분석 전 동결 SHA/hash가 있어야 하며 아직 동결하지 않은 후보를 정답으로 보고하지 않는다.
3. **구현·측정:** 신규 `gpt-6.1-sol`이 승인 명령으로 설치하고 두 실행 경로·공통 변환·재계산 가능한 평가 자료를 구현한다. 자체 점검·원시 근거와 쓰기 종료를 보고하고 정산·종료한다. CodeGraph가 필요한 관계를 못 내거나 Unity가 부분 해석이어도 관측 결과를 보존한다. 정확도 우열 자체를 통과조건으로 정하지 않는다.
4. **계약·보고 작성:** Astra가 실행 근거로 스냅샷 계약·시스템 개요 접점·사용자용 비교 보고를 완성한다. 정확도/지원 범위/비용/유지보수 제약과 추천 이유, 미실행을 구분하고 milestone ID로 표시명을 대신하지 않는다.
5. **독립 검증:** 모든 작성자의 쓰기 종료 뒤 신규 `claude-opus-5-5`가 실제 diff·정답표 원천/동결·설치/실행 근거를 먼저 실사한다. 독립 테스트는 동명 오인·추출 누락·불완전 입력·schema/SHA/path/endpoint·결정적 정규화·카드 경로 경계 등 요구 계약을 검증한다. 실제 .NET 검사는 WSL에서 실행하며 새 NuGet을 추가하지 않는다. 테스트만 작성하고 도구 결함은 번호로 반환한다.
6. **보완·통합:** 수정은 새 Sol, 재검증은 새 Opus로 수행한다. 같은 번호가 3번 재검증 실패하면 메인에 보고한다. Astra는 판정 원문과 실행 근거를 대조하고 합의 범위 안에서 commit/push/PR을 수행한다. 메인은 [R-2](../../../00_Document/operations/ORCA.md#r2-source-check)의 원천 표본 대조 후 최종 추출기 선택과 해당 PR의 사용자 병합 승인을 요청한다. 자동 병합은 하지 않는다.

최종 완료 근거는 동결 정답·manifest, 두 도구의 원시 출력/진단/비용, TP/FP/FN 재계산 자료, Unity 별도 한계, 스냅샷 계약과 시스템 개요 예시, 신규 Opus 판정 원문이다. 구현·검증·사용자 선택·PR 병합 상태를 각각 기록한다. 보고와 실제 수행이 다르거나 미실행을 통과로 보고하면 즉시 메인에 알린다.

외부 작업자는 **동시에 하나만** 둔다. Architecture Astra 아래 vertical split으로 새 세션을 열고 첫 화면·모델·준비·runtime/handle/incarnation을 확인한 뒤 새 Run/Task/Dispatch로 연결한다. 일반 작업자는 추가 위임·Git commit/push/병합을 하지 않는다. 각 작업 뒤 정산·종료하며 수정·재검증에 재사용하지 않는다. 구체 절차는 [Orca 위임 지침](../../../.agents/skills/dawnholder-goal-loop/references/orca-work.md)을 따른다.

## 진입 상태와 다음 경계

- 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/architecture-active`.
- 브랜치: `feat/architecture-extractor-a1-20261002`. 진입 HEAD `c9d07ceca15d059b07286536f0e2abd09885e1c6`는 이전 목표의 결과·R-8 인계 기록이다. 기준 main `881957cbb431d4af822d1d935ac117e1ede6c303`와 현재 원격 main이 같음을 2026-10-02 `git ls-remote`로 확인했다. 진입 tracked 변경은 없었다.
- 현재 Astra: handle `term_cf097010-8b72-4381-800d-3fdc2882c826`, runtime `8a673084-6819-45b9-a551-347226cdce9b`, incarnation `bca09aee-2864-4bd1-b988-f6f9ab8fe06e`. 화면 `GPT-6-Astra xhigh`, backend 실제 모델 `unknown`. 메인에 READY `msg_87eae6e32eba`를 보냈다. 근거는 이번 로컬 evidence 폴더의 `astra-identity.json`, `astra-screen.json`이다.
- 이 목표의 새 Run/Task/Dispatch와 외부 작업자는 아직 없다. 과거 목표의 식별자를 실행 권한으로 재사용하지 않는다. 설치·정답 동결·분석·제품 build/test·Unity·DB·독립 판정은 아직 수행하지 않았다.
- CURRENT는 이 goal 링크만 추가하고 기존 링크를 모두 보존한다. GameDev 저장 연동 브랜치도 CURRENT를 수정하므로 뒤에 통합하는 쪽이 최신 main과 대조해 양쪽 링크를 보존한다. 현재 원격 main과의 관계를 미병합 다른 브랜치와의 비충돌로 표현하지 않는다.
- 남은 설계 확인은 Unity 참조의 실제 읽기 입력, 도구의 관계별 지원 수준, 시스템 카드의 다중 매핑 해석이다. 이들은 시범의 조사 대상이며 측정 전에 성공으로 가정하지 않는다. 설치 조건을 넓혀야 하거나 파일 소유 범위를 벗어나야 하면 메인에 올린다.
- 다음 행동은 메인의 goal 검토 결과 반영이다. 승인 뒤 평가 기준을 동결하고 신규 Sol을 발행한다. PR 병합과 결과 기록이 모두 끝난 뒤 Astra 교체는 메인이 [R-8](../../../00_Document/operations/ORCA.md#r8-astra-lifecycle)에 따라 수행한다.
