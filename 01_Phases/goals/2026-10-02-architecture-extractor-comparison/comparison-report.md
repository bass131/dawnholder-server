# 정적 관계 추출기 비교

**상태 스냅샷: 2026-10-02 07:13 UTC — 구현 쓰기 종료, 독립 검증 전.** 닫힌 양성 표본 15개에서 CodeGraph는 14개, SDK Roslyn 경로는 15개를 찾았고 양쪽 오인은 0개였다. CodeGraph의 분석은 더 빨랐으며 양쪽 전체 해석은 partial이다. 이 문서는 현재 [goal](goal.md)의 비교 결과 설명이며, 최종 추출기 선택이나 PR 병합 승인이 아니다.

작성자는 Architecture Astra(지정 `gpt-6-astra`, 화면 `GPT-6-Astra xhigh`, 실제 backend `unknown`)다. 구현자 [완료 기록](../../../.backups/verification/2026-10-02-architecture-extractor-comparison/implementation/implementation.md)과 실제 측정 파일을 대조했다. 아래 수치는 신규 Opus의 독립 판정 전이며 이후 상태·판정 원문은 goal이 정본이다.

## 비교 이유와 판단 범위

시스템 개요에서 카드 사이의 관계를 눌렀을 때 실제 코드의 선언·직접 호출·타입 사용으로 내려가려면, 이름이 비슷한 다른 메서드를 구분하고 누락과 부분 해석을 드러내는 데이터가 필요하다. 이번 작업은 그 데이터를 얻는 두 방법과 공통 스냅샷 계약을 비교한다. 게임의 실제 실행 순서나 네트워크 전송을 계측하는 작업은 아니다.

CodeGraph는 설치된 `1.6.1`의 인덱스와 resolver 결과를 읽는다. Roslyn 경로는 고정 .NET SDK `10.0.301`에 동봉된 컴파일러·Workspace API로 프로젝트 의미 정보를 얻는 자체 추출기다. 이 비교는 두 라이브러리의 보편적 성능 순위가 아니라 **이번 고정 입력·설정·변환 구현**의 비교다. CodeGraph의 문법 보조 스크립트는 호출의 람다 문맥을 설명하며 별도 비용으로 기록한다.

분석 소스는 `881957cbb431d4af822d1d935ac117e1ede6c303`의 파일 225개, 그중 C# 210개다. 로컬 Unity managed DLL 157개를 별도 참조 입력으로 고정했다. 입력 전체와 사람이 의미를 판정한 작은 평가 표본을 구분한다. [입력 manifest](input-manifest.json), [평가 범위](evaluation-scope.json), [정답표](truth.json)가 근거다.

## 사전 동결과 채점

추출기 출력을 보기 전에 source/target의 닫힌 후보 36개를 정하고 양성 15개·음성 21개를 원천 코드에서 작성했다. 동결 commit `12327d8af48e06fed71058c0b9316df7bc4c2cef`의 UTC는 `2026-10-02T06:05:04Z`다. 첫 분석 전 기록은 CodeGraph `06:28:14.500700Z`, Roslyn `06:28:21.600996Z`이며, 로컬 evidence의 `freeze-record.json`과 `implementation/first-analysis-*.json`에 남아 있다. 기록 순서와 정답의 의미는 신규 Opus가 독립 확인한다.

정규화는 raw·manifest·도구 설정만 읽고, 채점 단계에서 평가 범위와 정답표를 읽는다. 같은 이름만으로 target을 맞추거나 정답으로 간선을 보충하지 않는다. 이름이 같은 `GameMap.ProcessAttack`과 `CombatSystem.ProcessAttack`, 패킷별 `Read`를 서로 다른 심볼로 다룬다. 정답 누락은 FN, 잘못된 후보 확정은 FP이며 unresolved/ambiguous는 확정 TP가 아니다. 범위 밖 관계는 별도 집계하고 분모 0은 N/A다.

정답 양성은 calls 8개, implements 4개, usesType 3개다. source층은 Server 9개, Shared 1개, ClientNet 1개, Client 4개다. 아래 수치는 구현자가 최종으로 지목한 `implementation/runs/20261002T065831395290Z`의 `score.json`에서 읽었다. 원문 위치의 공통 앞부분은 로컬 `.backups/verification/2026-10-02-architecture-extractor-comparison/`다. 이 작은 표본에서 얻은 정밀도·재현율을 저장소 전체 정확도로 해석하지 않는다.

| 도구 | TP / FP / FN | 정밀도 | 재현율 | 분석 상태 |
|---|---:|---:|---:|---|
| CodeGraph + 공통 변환 | 14 / 0 / 1 | 14/14 = 100% | 14/15 = 93.3% | partial |
| SDK Roslyn 자체 추출기 + 공통 변환 | 15 / 0 / 0 | 15/15 = 100% | 15/15 = 100% | partial |

| 관계 | 정답 양성 / 음성 후보 | CodeGraph TP / FP / FN | Roslyn TP / FP / FN |
|---|---:|---:|---:|
| calls | 8 / 7 | 7 / 0 / 1 | 8 / 0 / 0 |
| implements | 4 / 8 | 4 / 0 / 0 | 4 / 0 / 0 |
| usesType | 3 / 6 | 3 / 0 / 0 | 3 / 0 / 0 |

| source층 | 정답 양성 / 음성 후보 | CodeGraph TP / FP / FN | Roslyn TP / FP / FN |
|---|---:|---:|---:|
| Server | 9 / 13 | 9 / 0 / 0 | 9 / 0 / 0 |
| Shared | 1 / 2 | 1 / 0 / 0 | 1 / 0 / 0 |
| ClientNet | 1 / 0 | 0 / 0 / 1 | 1 / 0 / 0 |
| Client | 4 / 6 | 4 / 0 / 0 | 4 / 0 / 0 |

각 행의 정밀도는 TP/(TP+FP), 재현율은 TP/(TP+FN)이다. CodeGraph의 ClientNet은 확정 예측이 없어 정밀도 N/A, 재현율 0/1이다. 나머지 층은 양쪽 모두 표본 안에서 정밀도·재현율 100%다. 관계별로 CodeGraph calls 재현율은 7/8=87.5%이고 나머지 행은 양쪽 100%다. 이 표본에서는 세 관계 모두 지원 표시가 있지만 CodeGraph `usesType`의 일반 지원은 명시적인 method→type 생성 관계에 한정된 부분 지원이다.

유일한 FN은 `PacketSession.OnRecv → FrameValidator.TryValidateFrameHeader`다. source와 target은 [평가 범위](evaluation-scope.json)의 `netRecv`·`validateFrame`으로 식별된다. 누락을 정답표로 보충하지 않았다. 구체적인 raw 원인과 별도의 실패 사례는 독립 실사에서 확인한다.

전체 raw 집계는 CodeGraph 심볼 3,838개/관계 출현 7,322개, Roslyn 심볼 1,597개/관계 출현 8,574개다. 정규화 결과는 CodeGraph 3,097 nodes/5,208 edges, Roslyn 1,889 nodes/7,118 edges다. 그중 unresolved/ambiguous는 각각 1,626개/77개, 채점기가 분리한 확정 범위 밖 간선은 3,568개/7,026개다. 심볼 종류·외부 참조·타입 사용 지원 범위가 다르므로 이 개수 차이 자체를 정확도나 코드 품질 점수로 비교하지 않는다.

## 구현에서 읽을 경계

| 위치 | 책임과 의미 |
|---|---|
| `99_Tools/Architecture/run-architecture.ps1`, `run-wsl.sh` | Windows 입력 정보를 기록하고 소유 marker·lock이 있는 WSL 복사본에서 실행한다. 원본 Unity DLL·자산으로 역복사하지 않는다. |
| `Roslyn/` | 기존 project 해석과 Unity 참조의 부분 근사를 통해 심볼·관계·컴파일 진단을 raw로 출력한다. 정답표를 읽지 않는다. |
| `CodeGraph/` | 승인된 패키지·lock과 번들 문법의 문맥 보조 코드다. raw resolver target과 보조 문맥의 출처를 구분한다. |
| `Pipeline/inputs.py`, `execution.py`, `runner.py` | 고정 입력·도구·명령·비용·반복 실행을 기록한다. |
| `Pipeline/normalization.py`, `snapshot.py` | 공통 노드·간선·근거를 만들고 SHA·경로·endpoint·해석 상태를 검사한다. 같은 SHA의 codeReference 매핑 접점을 제공한다. |
| `Pipeline/evaluation.py`, `cli.py` | 정규화 완료 후 닫힌 정답 범위에서 채점하고 JSON/CSV를 남긴다. normalize·validate·score·join을 따로 실행할 수 있다. |

기존 게임 책임을 이 도구로 옮기지 않았다. 같은 목적의 기존 `Formatting` 도구처럼 `99_Tools` 아래에 실행·입력·계약 책임을 분리했다. 추출기가 시스템 카드 이름이나 화면 분류를 소유하지 않도록 카드 매핑은 별도 접점으로 둔다.

제품 도구 checkpoint는 `d0dffd1feb6082c3ddeb50fed359e2cc94886bea`로, 직전 `d7f3e82a82e496817887f9b8649683ea90ca0f39` 대비 새 파일 20개·1,542행이다. 측정 당시에는 도구가 미커밋이어서 config의 implementationHead는 직전 HEAD이며, 실제 실행 코드 SHA-256은 config와 `changed-files.json`으로 식별한다. Astra는 종료 후 현재 20개 파일과 기록 hash가 일치함을 확인하고 commit했다. 분석 대상 SHA와 도구 commit을 혼동하지 않는다. 전체 고정 diff는 다음 명령으로 볼 수 있다.

```text
git diff d7f3e82a82e496817887f9b8649683ea90ca0f39 d0dffd1feb6082c3ddeb50fed359e2cc94886bea -- 99_Tools/Architecture
```

## 시스템 개요에 연결하는 조건

[스냅샷 계약](snapshot-contract.md)은 node의 정확한 파일·namespace·signature와 edge의 출현 위치·문맥을 보존한다. `GameSession.SubmitAttack`의 큐 내부 람다에서 `GameMap.ProcessAttack`으로 이어지는 호출은 deferred 문맥이다. 이를 즉시 실행 순서로 표시하면 안 된다.

Management 하위 카드의 codeReference와 snapshot은 같은 전체 SHA에서만 조인한다. 같은 코드가 여러 카드에 들어가는 다대다 소속을 보존하고, 상위 시스템 관계는 그 소속에서 유도한 집계로 표시한다. 중복 edge를 한 번 세더라도 어떤 카드들이 같은 근거를 공유하는지는 남긴다. 현재 Management 디자인 샘플은 다른 SHA이며 실제 카드 corpus가 아니므로 이번 결과와 조인하지 않았다.

1~2홉·층별 배치·관계 필터·모듈 접기·두 지점 경로 질의·패킷 타입 중심 탐색은 이 정적 데이터의 소비 기능이다. 이번에는 화면을 만들지 않았다. 각 기능이 실제 데이터에서 지원되는 범위와 누락은 최종 산출물 대조 후 구분해 기록한다.

## 실행 비용·한계·남은 확인

설치·기존 의존성 restore·도구 build와 분석 시간을 분리한다. 각 추출기는 cold 1회와 warm 3회를 순차 실행한다. cold는 분석 DB/cache가 없는 상태이며 OS page cache를 비웠다는 뜻이 아니다. Roslyn의 반복은 새 process 실행이며 CodeGraph의 persistent DB와 동일한 캐시 구조가 아니다. peak memory는 GNU time의 명령·대기한 자식 최대 RSS이며 여러 process의 동시 메모리를 합산한 값이 아니다.

| 실행 | CodeGraph index 초 / peak bytes | 별도 문법 문맥 초 | CodeGraph 정규화 초 | Roslyn 추출 초 / peak bytes | Roslyn 정규화 초 |
|---|---:|---:|---:|---:|---:|
| cold | 1.072 / 472,920,064 | 0.367 | 0.177 | 7.694 / 446,783,488 | 0.086 |
| warm 1 | 0.821 / 475,070,464 | 0.368 | 0.180 | 7.393 / 459,411,456 | 0.092 |
| warm 2 | 0.819 / 471,588,864 | 0.368 | 0.193 | 7.792 / 441,241,600 | 0.092 |
| warm 3 | 0.872 / 475,209,728 | 0.368 | 0.191 | 8.043 / 452,370,432 | 0.102 |

같은 WSL2/Linux 환경에서 실행했으며 8회 분석의 exit는 모두 0이다. CodeGraph의 문법 문맥 보조 peak bytes는 cold/warm1/2/3 각각 96,927,744 / 97,468,416 / 98,447,360 / 97,411,072다. 원시 JSON export·입력 복사·검사·채점 등 일부 단계의 비용이 분석 열에 포함되지 않으므로 전체 사용자가 기다린 시간으로 표시하지 않는다. 각 실행 `analysis/command.json`, `syntax-context/command.json`과 공통 `measurements.json/CSV`가 원천이다.

승인된 npm 설치는 4.713초·exit 0, 실제 node_modules 296,018,321 bytes, npm cache 64,762,250 bytes로 기록됐다. 설치 과정의 네트워크 다운로드 바이트를 이 디스크 점유량과 동일시하지 않는다. 최종 batch의 tool restore 1.021초, tool build 2.224초, Server restore 2.223초, ClientNet restore 0.720초는 각각 exit 0이다. 이는 최종 batch의 기존 캐시 상태에서 측정한 준비 비용이며 최초 설치 전체 비용이나 제품 전체 build 성공으로 합치지 않는다.

Unity는 로컬 managed DLL만 사용하고 InputSystem/TMP의 Library·obj를 입력으로 읽지 않았다. 최종 Roslyn raw의 Unity 근사 compilation에는 Error 285개·Warning 55개·Hidden 250개가 있다. Server/Shared/ClientNet의 compilation에는 Error가 기록되지 않았지만 각각의 미해석 호출은 남아 있다. 측정 자료의 이 세 층 `complete`는 compiler error 없는 실제 project 해석 상태를 뜻하며 모든 관계가 해소됐다는 뜻으로 읽지 않는다. 최상위 snapshot은 partial이다.

CodeGraph는 조건부 지시문 양쪽을 문법적으로 인덱싱하는 설정이며 Roslyn은 기록된 Debug/Unity editor/windows parse options를 따른다. 입력 파일이 같다는 사실과 활성화된 컴파일 분기가 같다는 주장을 구분한다. CodeGraph 결과도 컴파일러가 완전하게 확인한 의미 그래프라고 표시하지 않는다. 설치 메모리나 정규화 개별 peak memory처럼 계측하지 않은 항목은 N/A다. telemetry 환경 설정·설치 소스 확인과 패킷 캡처를 구분한다.

## 선택 근거와 유지보수 대가

현재 C# 코드의 정확한 선언·signature·동명 구분을 우선하는 기본 후보는 **SDK Roslyn 경로**다. 이 표본에서 누락이 없고 compiler symbol 및 진단을 함께 제공하기 때문이다. 대가는 project restore·고정 SDK Workspace 의존·더 긴 분석 시간, Unity 참조와 조건부 컴파일 설정을 유지할 책임이다. Unity 부분 해석을 해결한 것으로 선택 근거를 과장하지 않는다.

CodeGraph는 이번 설정에서 빠른 정적 탐색 후보다. 별도 약 296 MB 설치와 고정 버전 번들, DB export·정규화·문법 보조의 유지보수가 필요하다. 이번 표본의 호출 1개 누락, 완전한 signature의 부족과 부분 타입 사용 지원, compiler defines 차이를 표시한 상태로 사용해야 한다. 두 결과를 합쳐 누락이 사라진 척하는 혼합 그래프는 이번에 구현하지 않았다. 최종 채택은 독립 검증 결과와 이 대가를 본 사용자에게 맡긴다.

구현자는 raw replay·반복 결과의 byte equality·계약 실패 경로 등의 자체 점검 33개와 C# whitespace 검사 exit 0을 보고했다. Astra는 완료 원문·명령 기록·파일 hash·집계 자료를 대조했으며, 이것을 독립 테스트 통과로 보고하지 않는다. 새 Opus가 source 정답·실제 diff·raw부터 실사한 뒤 독립 테스트를 작성한다. 구현 중 고친 JSON key order에 따른 ID 변화와 WSL의 이동 전 파일 잔존도 이전 batch를 남겨 재확인할 수 있게 했다.

Unity Editor·플레이·게임 서버·DB·제품 전체 빌드/테스트는 범위 밖으로 실행하지 않았다. CI는 GameDev의 독립 도구 목록 등록 PR이 main에 통합된 뒤 우리 프로젝트 항목을 추가해 확인한다. 그 전에는 이 작업의 PR을 내지 않는다. 독립 판정이 아직 없다는 점과 Unity 부분 해석·정적/실행 관계의 구분은 채택 판단에 중요한 제한이다. 최종 도구 선택과 각 PR 병합은 사용자의 결정으로 남긴다.

## 재현과 근거 찾기

Windows 저장소 루트의 PowerShell 7.6.6에서 이미 설치한 패키지를 사용해 `& ./99_Tools/Architecture/run-architecture.ps1 measure`를 실행하면 별도 시각의 batch를 만든다. `check`는 구현 도구의 계약·채점·C# 서식을 다시 확인하며 독립 검증을 대신하지 않는다. 소유 WSL 경로는 `/home/bass1/.cache/dawnholder/architecture/ff3952212f2c45d509f5`다. 구현 원문에 명령·설정·전용 SDK 상태와 실행 한계가 있다.

raw 재정규화는 `Pipeline/cli.py normalize`에 run의 `raw.json`·`extractor-config.json`·동결 manifest를 전달한다. `score`는 그 결과와 scope·truth를 별도로 받는다. 새 출력 경로를 지정해 기존 근거를 덮어쓰지 않는다. Windows 경로·개행과 WSL 경로·Git blob 바이트 차이도 기록된 hash의 기준에 맞춰 확인한다.

| 근거 | 로컬 evidence 안의 위치 |
|---|---|
| 승인 설치·lock·크기 | `implementation/install/measurement.json`, `dependency-check.json`, `install/package-lock.json` |
| 분석 전 동결 | `freeze-record.json`, `implementation/first-analysis-codegraph.json`, `first-analysis-roslyn.json` |
| 입력·실행·비용 | 최종 batch의 `input-copy.json`, `config.json`, `environment.json`, `measurements.json/csv`, 단계별 `command.json` |
| 독립 raw와 정규화·채점 | 최종 batch의 `{codegraph,roslyn}/{cold,warm1,warm2,warm3}/raw.json`, `normalized.json`, `score.json/csv` |
| 구현자 자체 검사·보존 | `implementation/inspection-results.json`, `format/whitespace/command.json`, `original-final-check.json`, `changed-files.json` |
| 최종 파일 대조·구현 diff | `astra-final-file-check.json`, `implementation.diff` |

이 evidence는 Git 제외 로컬 원문이다. 공유 가능한 범위의 수치·해석·고정 입력은 이 goal 폴더에 보존했고, PR 또는 원격에서 로컬 원문이 자동 제공된다고 가정하지 않는다.
