# 정적 관계 추출기 비교

**2026-10-02 독립 실사 후 정정본 — 새 문서 재실사 전.** 닫힌 후보 36개 안에서는 CodeGraph TP/FP/FN이 14/0/1, SDK Roslyn 경로가 15/0/0이었다. 그러나 **범위 밖에서는 CodeGraph가 프로젝트 참조상 불가능한 층 방향 간선 16개를 확정했다. 같은 방향 위반은 Roslyn에서 0개였다.** CodeGraph의 유일한 표본 FN도 서버의 동명 메서드로 잘못 연결한 결과다. 이 차이를 추출기 선택의 핵심 근거로 본다.

CodeGraph 인덱싱은 더 빨랐으며 양쪽 전체 해석은 partial이다. Roslyn 기본 후보 판단에도 Unity 부분 해석과 타입 사용 분류의 한계가 남는다. 이 문서는 현재 [goal](goal.md)의 비교 결과 설명이며, 최종 추출기 선택이나 PR 병합 승인이 아니다.

작성자는 Architecture Astra(지정 `gpt-6-astra`, 화면 `GPT-6-Astra xhigh`, 실제 backend `unknown`)다. 첫 [Opus 판정](../../../.backups/verification/2026-10-02-architecture-extractor-comparison/verification/verdict.md)은 **FAIL: 보고 필수 수정 D1–D3, 제품 필수 결함 0**이었다. 이전 보고 `4b064ec`의 범위 밖 오확정 축소·캐시 조건 오기·뷰어 평가 누락을 이 정정본에서 고쳤다. 최초 판정과 원시 자료를 보존하며, 새 Opus의 문서 재실사와 이후 상태는 goal에 연결한다.

## 비교 이유와 판단 범위

시스템 개요에서 카드 사이의 관계를 눌렀을 때 실제 코드의 선언·직접 호출·타입 사용으로 내려가려면, 이름이 비슷한 다른 메서드를 구분하고 누락과 부분 해석을 드러내는 데이터가 필요하다. 이번 작업은 그 데이터를 얻는 두 방법과 공통 스냅샷 계약을 비교한다. 게임의 실제 실행 순서나 네트워크 전송을 계측하는 작업은 아니다.

CodeGraph는 설치된 `1.6.1`의 인덱스와 resolver 결과를 읽는다. Roslyn 경로는 고정 .NET SDK `10.0.301`에 동봉된 컴파일러·Workspace API로 프로젝트 의미 정보를 얻는 자체 추출기다. 이 비교는 두 라이브러리의 보편적 성능 순위가 아니라 **이번 고정 입력·설정·변환 구현**의 비교다. CodeGraph의 문법 보조 스크립트는 호출의 람다 문맥을 설명하며 별도 비용으로 기록한다.

분석 소스는 `881957cbb431d4af822d1d935ac117e1ede6c303`의 파일 225개, 그중 C# 210개다. 로컬 Unity managed DLL 157개를 별도 참조 입력으로 고정했다. 입력 전체와 사람이 의미를 판정한 작은 평가 표본을 구분한다. [입력 manifest](input-manifest.json), [평가 범위](evaluation-scope.json), [정답표](truth.json)가 근거다.

## 사전 동결과 채점

추출기 출력을 보기 전에 source/target의 닫힌 후보 36개를 정하고 양성 15개·음성 21개를 원천 코드에서 작성했다. 동결 commit `12327d8af48e06fed71058c0b9316df7bc4c2cef`의 UTC는 `2026-10-02T06:05:04Z`다. 첫 분석 전 기록은 CodeGraph `06:28:14.500700Z`, Roslyn `06:28:21.600996Z`이며, 로컬 evidence의 `freeze-record.json`과 `implementation/first-analysis-*.json`에 남아 있다. 첫 신규 Opus가 기록 순서와 정답의 의미를 독립 확인했다.

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

유일한 FN은 `PacketSession.OnRecv → FrameValidator.TryValidateFrameHeader`다. CodeGraph는 ClientNet 호출(`04_ClientNet/ClientSession.cs:53`)의 target을 **Server의 동명 메서드**(`02_Server/Network/FrameValidator.cs:32`)로 확정했다. raw의 `resolvedBy=qualified-name`, `confidence=0.85`는 도구가 남긴 값이며 올바른 의미 해석의 보증이 아니다. 네 번의 실행 모두 같았다. Roslyn은 실제 ClientNet 선언(`04_ClientNet/FrameValidator.cs:56`)으로 연결했다.

동결 target은 ClientNet 메서드이므로 CodeGraph 결과는 FN이다. 잘못 고른 Server target은 사전에 정한 후보 밖에 있어 채점 규칙상 FP 대신 범위 밖 집계에 들어간다. **이 때문에 닫힌 후보 안 FP 0이 추출 전체의 오인 0을 뜻하지 않는다.** 점수와 분모를 사후 변경하지 않았고 정답으로 간선을 보충하지 않았다. 해당 원시·정규화 간선 ID와 네 실행의 대조는 `verification/logs/audit/frame.txt`에 있다.

전체 raw 집계는 CodeGraph 심볼 3,838개/관계 출현 7,322개, Roslyn 심볼 1,597개/관계 출현 8,574개다. 정규화 결과는 CodeGraph 3,097 nodes/5,208 edges, Roslyn 1,889 nodes/7,118 edges다. 그중 unresolved/ambiguous는 각각 1,626개/77개, 채점기가 분리한 확정 범위 밖 간선은 3,568개/7,026개다. 심볼 종류·외부 참조·타입 사용 지원 범위가 다르므로 이 개수 차이 자체를 정확도나 코드 품질 점수로 비교하지 않는다.

### 범위 밖 관찰: 잘못된 층 연결

현재 프로젝트 참조를 기준으로 다음 세 방향의 코드 참조는 성립할 수 없다. ClientNet csproj에는 ProjectReference가 없고, Unity는 Shared·ClientNet DLL을 참조하며, 서버 프로젝트는 Shared·Network를 참조한다. 독립 검증자는 이 원천과 확정 간선을 대조했다. Astra도 원시 대조와 별도의 읽기 집계(`report-correction/summary.json`)로 같은 16개를 확인했다.

| 불가능한 방향 | CodeGraph 확정 간선 | Roslyn | CodeGraph 예시 |
|---|---:|---:|---|
| Client → Server | 8 (calls 7, usesType 1) | 0 | `Client.Network.NetworkService.Update → Server.Network.Session.Send` |
| ClientNet → Server | 6 (calls) | 0 | `Client.Net.PacketSession.OnRecv → Server.Network.FrameValidator.TryValidateFrameHeader` |
| Server → Client | 2 (calls) | 0 | `Server.Network.Listener.Init → Client.UI.QuestIntroSequencer.Run` |
| 합계 | **16** | **0** | 확정 간선의 고유 ID 기준 |

이 화살표를 그대로 시스템 카드에 집계하면 존재하지 않는 시스템 연결이나 경로를 보여 줄 수 있다. 빠르다는 이유로 CodeGraph 결과를 자동 집계의 신뢰 가능한 원천으로 곧바로 채택할 수 없다. Roslyn의 0도 **이 세 층 방향 위반 관찰에서의 0**이며 전체 그래프가 무오류라는 판정은 아니다. 16개는 확인한 오류 집합이고 범위 밖 전체에 대한 완전한 정답표나 전역 정밀도 계산은 아니다.

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

다음은 최종 cold snapshot의 실제 node/edge를 읽어 수행한 **데이터 지원 평가**다. 화면·조회 API를 구현하거나 플레이를 실행했다는 뜻이 아니다. 양쪽 정규화 결과가 네 번 동일한지는 독립 replay 검사가 확인했다.

| 뷰어 기능 | 확인한 데이터와 판정 | 부족한 부분·소비 조건 |
|---|---|---|
| 1~2홉 탐색 | 부분 지원. 양쪽 `AttackHandler.Handle → GameSession.SubmitAttack → GameMap.ProcessAttack`의 두 간선과 중간 endpoint가 실제로 연결된다. | 두 번째 호출은 deferredLambda다. 확정 상태·관계 종류를 필터해야 하며 CodeGraph 오확정은 거짓 이웃을 만든다. |
| Client/ClientNet/Shared/Server 배치 | 층 필드 지원. 모든 node에 layer가 있다. | External은 CodeGraph 1,626/Roslyn 405 nodes, CodeGraph에는 PDL.xml의 Tools node도 1개 있다. 네 층에 억지로 넣지 않는다. 배치만으로 16개 잘못된 연결이 바로잡히지 않는다. |
| 관계 종류별 표시 | 네 종류가 모두 있다. CodeGraph contains/calls/implements/usesType은 2,438/2,547/79/144, Roslyn은 2,453/2,089/83/2,493개다. | 위 개수는 미해석 상태를 포함한다. CodeGraph usesType은 생성 관계 중심의 부분 지원, Roslyn에도 N4의 분류 한계가 있다. 상태와 종류를 함께 구분해야 한다. |
| 모듈 접기 | 거친 단위의 부분 지원. 양쪽 module은 입력 root 5개이고 module→file, file→type/method, type→method 포함 관계가 있다. | namespace·중간 폴더의 별도 모듈 계층은 없다. 파일·타입의 포함은 다중 부모 관계가 있어 tree로 가정하면 중복될 수 있다. 소비자의 집계·접기 구현이 필요하다. |
| 두 지점 경로 질의 | 부분 지원. sourceId/targetId 연결로 정적 경로를 질의할 자료가 있다. 위 2홉 예시는 실제 간선 연결로 확인했다. | 질의 API·UI는 미구현이며 runtime 순서를 보장하지 않는다. CodeGraph의 16개 오확정은 거짓 경로도 만들 수 있다. |
| 패킷 중심 탐색 | 일반 type 사용·인터페이스 관계를 통한 부분 탐색은 가능하다. | 양쪽 `packet` kind node는 0개다. protocol ID/version도 없으므로 완성된 패킷 식별·전송 경로 뷰는 미지원이다. 실제 패킷 분류와 PDL 연결은 후속이다. |
| 전체 시스템 개요 | 계약·합성 매핑 검사 단계. 같은 SHA의 하위 codeReference로 membership을 얻을 수 있다. | 실제 카드 corpus 조인, 상위 집계, 운영툴 UI는 미구현·미검증이다. CodeGraph 오확정과 Roslyn 부분 해석을 집계에 그대로 신뢰해서는 안 된다. |

module root 5개는 `02_Server/GameServer`, `02_Server/Network`, `03_Client/Assets/Scripts`, `04_ClientNet`, `98_Shared`다. file node는 CodeGraph 211개(C# 210개와 PDL.xml), Roslyn 210개다. 자세한 종류·포함 관계 집계와 2홉의 ID/파일/문맥은 `report-correction/summary.json`에 있다. 실제 카드 조인이 없는 이유는 아직 같은 SHA의 확정 corpus를 받지 않았기 때문이며 화면은 이 goal의 범위 밖이다.

## 실행 비용·한계·남은 확인

설치·기존 의존성 restore·도구 build와 분석 시간을 분리한다. 각 추출기는 cold 1회와 warm 3회를 순차 실행한다. cold는 분석 DB/cache가 없는 상태이며 OS page cache를 비웠다는 뜻이 아니다. Roslyn의 반복은 새 process 실행이며 CodeGraph의 persistent DB와 동일한 캐시 구조가 아니다. peak memory는 GNU time의 명령·대기한 자식 최대 RSS이며 여러 process의 동시 메모리를 합산한 값이 아니다.

| 실행 | CodeGraph index 초 / peak bytes | 별도 문법 문맥 초 | CodeGraph 정규화 초 | Roslyn 추출 초 / peak bytes | Roslyn 정규화 초 |
|---|---:|---:|---:|---:|---:|
| cold | 1.072 / 472,920,064 | 0.367 | 0.177 | 7.694 / 446,783,488 | 0.086 |
| warm 1 | 0.821 / 475,070,464 | 0.368 | 0.180 | 7.393 / 459,411,456 | 0.092 |
| warm 2 | 0.819 / 471,588,864 | 0.368 | 0.193 | 7.792 / 441,241,600 | 0.092 |
| warm 3 | 0.872 / 475,209,728 | 0.368 | 0.191 | 8.043 / 452,370,432 | 0.102 |

같은 WSL2/Linux 환경에서 실행했으며 8회 분석의 exit는 모두 0이다. CodeGraph의 문법 문맥 보조 peak bytes는 cold/warm1/2/3 각각 96,927,744 / 97,468,416 / 98,447,360 / 97,411,072다. 원시 JSON export·입력 복사·검사·채점 등 일부 단계의 비용이 분석 열에 포함되지 않으므로 전체 사용자가 기다린 시간으로 표시하지 않는다. 각 실행 `analysis/command.json`, `syntax-context/command.json`과 공통 `measurements.json/CSV`가 원천이다.

승인된 npm 설치는 4.713초·exit 0, 실제 node_modules 296,018,321 bytes, npm cache 64,762,250 bytes로 기록됐다. 설치 과정의 네트워크 다운로드 바이트를 이 디스크 점유량과 동일시하지 않는다.

`measure` 진입 명령은 매번 새 `.dotnet-state-*`를 만들고 전용 NuGet package·HTTP·plugins·scratch 경로를 사용한다. **최종 준비 단계에는 api.nuget.org에서 기존 제품 의존성을 내려받은 시간이 들어 있다.** 최종 state는 소유 WSL root 아래 `.dotnet-state-82UwSiLX`다. 하나의 `measure` 안의 8회 분석은 이 준비 이후 실행되며 각 cold/warm 분석마다 restore를 새로 수행한 것은 아니다.

| 최종 준비 단계 | 다시 집계한 초 | cache·수행 범위 |
|---|---:|---|
| 도구 restore | 1.021 | 새 격리 상태. 도구에는 새 NuGet PackageReference가 없다. |
| Server restore | 2.223 | 새 package/HTTP cache에 기존 제품 패키지를 다운로드했다. |
| ClientNet restore | 0.720 | 같은 진입 실행에서 앞선 restore가 채운 격리 cache를 사용한다. |
| restore 합계 | **3.964** | 세 restore 명령의 elapsedSeconds 합, 설치·분석 제외 |
| 도구 build (`--no-restore`) | 2.224 | 제품 전체 build가 아니다. |
| restore + 도구 build | **6.188** | 원시 반올림 전 수치의 합, 모두 exit 0 |

최종 cache의 공통 경로는 `/home/bass1/.cache/dawnholder/architecture/ff3952212f2c45d509f5/.dotnet-state-82UwSiLX/nuget`다. 그 아래 `packages`에는 `NETStandard.Library.Ref 2.1.0` 23,193,422 bytes, `StyleCop.Analyzers 1.2.0-beta.556` 34,522 bytes, `StyleCop.Analyzers.Unstable 1.2.0.556` 1,957,899 bytes가 있다. 합계 157파일·25,185,843 bytes다. 같은 위치의 `http`는 11파일·4,126,428 bytes이며 api.nuget.org의 패키지/목록 cache 파일 시각은 Server restore 구간과 일치한다. 이 수치는 cache의 파일 크기이며 실제 네트워크 전송량은 N/A다.

모든 기록된 .NET 명령의 NuGet 환경은 이 격리 경로를 가리킨다. **기존 전역 NuGet cache의 전후 해시를 수집하지 않았으므로 그 내용의 불변은 미검증이다.** 격리 경로 지정과 전역 cache 불변의 실증을 혼동하지 않는다. 경로·환경·개별 크기·비용 재집계는 `report-correction/summary.json`, 원천 다운로드 시각은 `verification/logs/audit/preserve.txt`에 있다. 재실행 시 새 cache에 기존 의존성을 복원하는 네트워크 의존이 유지 비용으로 남는다.

Unity는 로컬 managed DLL만 사용하고 InputSystem/TMP의 Library·obj를 입력으로 읽지 않았다. 최종 Roslyn raw의 Unity 근사 compilation에는 Error 285개·Warning 55개·Hidden 250개가 있다. Server/Shared/ClientNet의 compilation에는 Error가 기록되지 않았지만 각각의 미해석 호출은 남아 있다. 측정 자료의 이 세 층 `complete`는 compiler error 없는 실제 project 해석 상태를 뜻하며 모든 관계가 해소됐다는 뜻으로 읽지 않는다. 최상위 snapshot은 partial이다.

CodeGraph는 조건부 지시문 양쪽을 문법적으로 인덱싱하는 설정이며 Roslyn은 기록된 Debug/Unity editor/windows parse options를 따른다. 입력 파일이 같다는 사실과 활성화된 컴파일 분기가 같다는 주장을 구분한다. CodeGraph 결과도 컴파일러가 완전하게 확인한 의미 그래프라고 표시하지 않는다. 설치 메모리나 정규화 개별 peak memory처럼 계측하지 않은 항목은 N/A다. telemetry 환경 설정·설치 소스 확인과 패킷 캡처를 구분한다.

## 선택 근거와 유지보수 대가

현재 C# 코드의 정확한 선언·signature·동명 구분을 우선하는 기본 후보는 **SDK Roslyn 경로**다. 이 표본에서 누락이 없고 FrameValidator의 정확한 소유 선언을 구분했으며, 확인한 불가능 층 방향 간선이 0개이고 compiler symbol·진단을 제공하기 때문이다. 대가는 고정 SDK Workspace 의존·더 긴 분석 시간, 현재 runner의 실행별 새 NuGet cache와 기존 패키지 다운로드, Unity 참조·조건부 컴파일 설정을 유지할 책임이다. N4의 usesType 분류와 Unity 부분 해석이 남아 있어 전체 그래프가 정확하다고 보장하지 않는다.

CodeGraph는 이번 설정에서 인덱싱이 빨랐지만, **동명 target 오확정과 불가능 층 방향 16개 때문에 자동 시스템 개요의 기본 원천으로 바로 채택하기 어렵다.** 별도 약 296 MB 설치와 고정 버전 번들, DB export·정규화·문법 보조의 유지보수도 필요하다. 완전한 signature 부족, 부분 타입 사용 지원, compiler defines 차이를 함께 표시해야 한다. 두 결과를 합쳐 오류나 누락이 사라진 척하는 혼합 그래프는 구현하지 않았다. 최종 채택은 사용자가 결정한다.

구현자의 자체 점검 33개와 C# whitespace exit 0 이후, 신규 Opus가 원천·실제 diff·raw를 실사하고 별도 CLI 테스트 46개를 작성했다. WSL Python 3.14.4에서 **43개 정상 성공, 3개 expected failure**로 실행 종료 코드는 0이었다. expected failure는 아래 N1–N3의 알려진 미충족을 재현한 것이며 통과가 아니다. 8회 raw의 정규화·채점 재현, 정답표 36개 원천 대조, 입력 파일·Unity DLL hash 보존을 확인했다. 제품 필수 결함은 0이었지만 보고 D1–D3 때문에 첫 전체 판정은 FAIL이다. 테스트 commit은 `1daa45153b12074f12bd39b98a9a3abcd8fd3638`이며 원문은 `verification/logs/final-run.txt`다.

### 비차단 발견과 적용 한계

첫 검증자가 현재 동결 결과에는 비차단으로 분류한 사항도 후속 사용에서는 고려해야 한다. 이번 문서 정정에 제품 수정을 섞지 않았으며, 아래 항목을 해결했다고 주장하지 않는다.

| 항목 | 남은 사실과 영향 |
|---|---|
| N1–N3 | CodeGraph raw의 complete+입력 누락 상태 보정, 정답표 중복/ID/counts 검증, manifest/scope/truth의 알 수 없는 schemaVersion 거부가 부족하다. 독립 테스트의 expected failure 3개다. 현재 동결 입력·raw는 정상으로 독립 재계산에 일치했다. |
| N4 | Roslyn usesType에는 `var`의 추론 타입 66개가 확정되고, 메서드 후보인 이름 18개가 미해석/모호한 usesType으로 분류됐다. 닫힌 점수에는 영향이 없지만 전체 타입 사용 수와 개요 해석에는 한계가 있다. |
| N5 | layer의 complete는 compiler error 기준이고 snapshot의 complete는 미해석 간선도 없는 상태를 요구한다. 이름이 같은 상태의 의미 차이를 소비자가 구분해야 한다. |
| N6 | 동결 manifest의 작업본 CRLF hash는 `9f440a1a…`, Git LF blob hash는 `890aee6b…`다. 현재 snapshot은 작업 바이트 기준이므로 새로운 LF checkout에 그대로 이식되지 않는다. 원래 동결을 바꾸지 않았고 표준화 hash는 후속 보강 사항이다. |
| N7–N8 | 07:03의 check가 06:58 batch의 score/채점 명령 기록을 갱신했다(재계산은 동일). 실행 config의 코드 hash에는 `.ps1`과 comparison-settings.json이 빠져 있다. 완료 파일 hash/commit과 실행 시점 출처를 구분해야 한다. |
| N9 | Roslyn의 WorkspaceFailed API 사용에 CS0618 obsolete 경고가 있다. 도구 build exit 0은 경고 0이 아니다. |
| 가독성 | 책임별 배치는 적합하나 긴 Python/C# 식, 큰 measure 함수, PowerShell 명명 인자 부분 미준수가 비차단 의견으로 남았다. 구현/검증 당시 기준과 다음 신규 계약부터 강화되는 차단 기준의 적용 시점을 구분한다. |

Unity Editor·플레이·게임 서버·DB·제품 전체 빌드/테스트는 실행하지 않았다. 검증자도 .NET/CodeGraph를 새로 추출 실행하지 않고 기존 raw와 공개 변환·검사·채점 CLI를 독립 시험했다. CI는 GameDev의 독립 도구 목록 등록 PR이 main에 통합된 뒤 우리 프로젝트 항목을 추가해 확인한다. 그 전에는 이 작업의 PR을 내지 않는다. 정정 보고의 새 문서 판정이 아직 없고, Unity 부분 해석과 위 비차단 발견이 남아 있다. 최종 도구 선택과 각 PR 병합은 사용자의 결정으로 남긴다.

## 재현과 근거 찾기

현재 동결 바이트·설치 자료를 보존한 Windows checkout의 PowerShell 7.6.6에서 `& ./99_Tools/Architecture/run-architecture.ps1 measure`를 실행하면 별도 시각의 batch를 만든다. 기존 CodeGraph 설치는 사용하지만 NuGet 격리 상태는 새로 만든다. `check`는 계약·채점·C# 서식을 다시 확인하고 기존 batch의 채점 파일을 갱신한다. 근거 보존이 필요한 독립 검사는 이 명령을 그대로 재실행하지 않는다. 소유 WSL 경로는 `/home/bass1/.cache/dawnholder/architecture/ff3952212f2c45d509f5`다. 새 checkout의 manifest 개행 차이(N6)와 Git 제외 freeze/evidence 자료 필요를 함께 고려해야 한다.

raw 재정규화는 `Pipeline/cli.py normalize`에 run의 `raw.json`·`extractor-config.json`·동결 manifest를 전달한다. `score`는 그 결과와 scope·truth를 별도로 받는다. 새 출력 경로를 지정해 기존 근거를 덮어쓰지 않는다. Windows 경로·개행과 WSL 경로·Git blob 바이트 차이도 기록된 hash의 기준에 맞춰 확인한다.

| 근거 | 로컬 evidence 안의 위치 |
|---|---|
| 승인 설치·lock·크기 | `implementation/install/measurement.json`, `dependency-check.json`, `install/package-lock.json` |
| 분석 전 동결 | `freeze-record.json`, `implementation/first-analysis-codegraph.json`, `first-analysis-roslyn.json` |
| 입력·실행·비용 | 최종 batch의 `input-copy.json`, `config.json`, `environment.json`, `measurements.json/csv`, 단계별 `command.json` |
| 독립 raw와 정규화·채점 | 최종 batch의 `{codegraph,roslyn}/{cold,warm1,warm2,warm3}/raw.json`, `normalized.json`, `score.json/csv` |
| 구현자 자체 검사·보존 | `implementation/inspection-results.json`, `format/whitespace/command.json`, `original-final-check.json`, `changed-files.json` |
| 최종 파일 대조·구현 diff | `astra-final-file-check.json`, `implementation.diff` |
| 독립 실사·검사·보고 정정 근거 | `verification/verdict.md`, `verification/logs/final-run.txt`, `verification/logs/audit/{frame,viewer,preserve}.txt`, `report-correction/summary.json` |

이 evidence는 Git 제외 로컬 원문이다. 공유 가능한 범위의 수치·해석·고정 입력은 이 goal 폴더에 보존했고, PR 또는 원격에서 로컬 원문이 자동 제공된다고 가정하지 않는다.
