# 정적 관계 추출기 선택 근거

**정확한 선언 연결을 우선하는 시스템 개요의 기본 후보로 SDK Roslyn 경로를 추천한다.** 이번 표본에서 누락이 없고, 확인한 불가능 층 방향 간선도 없었다. CodeGraph는 분석이 빠르지만 동명 메서드 오확정 때문에 자동 집계 원천으로 바로 채택하기 어렵다. 아래 수치에 근거한 설계 판단이며 최종 선택은 사용자에게 남긴다. [근거 S01](comparison-claims.md#s01)

비교 입력은 소스 `881957cbb431d4af822d1d935ac117e1ede6c303`의 225파일(C# 210개)과 별도 Unity managed DLL 157개다. CodeGraph 1.6.1과 .NET SDK 10.0.301 동봉 Roslyn을 사용한 이 입력·설정·변환 구현의 비교이며 일반적인 도구 순위로 확대하지 않는다. [근거 S02](comparison-claims.md#s02)

최신 검증·통합 상태는 [goal](goal.md), 검증 이력과 이전 보고는 [로컬 이력](../../../.backups/verification/2026-10-02-architecture-extractor-comparison/report-reduction/history.md)에서 확인한다. 아래 로컬 원시 근거는 Git 제외 자료다.

## 정답표와 범위 밖 오류

닫힌 후보는 36개(양성 15·음성 21)다. TP는 맞게 찾은 양성, FP는 음성 후보를 잘못 확정한 것, FN은 놓친 양성이다. 후보 밖 간선은 점수 분모와 FP에 넣지 않으므로 **FP 0은 추출 전체의 오류 0을 뜻하지 않는다.** [근거 S03](comparison-claims.md#s03)

| 도구 | TP / FP / FN | 정밀도 / 재현율 | 전체 해석 상태 | 근거 |
|---|---:|---|---|---|
| CodeGraph | 14 / 0 / 1 | 100% / 93.3% | partial | [S04](comparison-claims.md#s04) |
| SDK Roslyn | 15 / 0 / 0 | 100% / 100% | partial | [S05](comparison-claims.md#s05) |

CodeGraph는 `04_ClientNet/ClientSession.cs:53`의 FrameValidator 호출을 `02_Server/Network/FrameValidator.cs:32`의 서버 선언으로 잘못 확정했다. Roslyn은 실제 `04_ClientNet/FrameValidator.cs:56`으로 연결했다. 동결 정답 target은 ClientNet 선언이어서 CodeGraph에는 FN이 되고, 잘못 고른 서버 target은 후보 밖이다. [근거 S06](comparison-claims.md#s06)

프로젝트 참조상 성립하지 않는 Client→Server, ClientNet→Server, Server→Client 방향의 확정 간선은 CodeGraph가 각각 8·6·2개(**합계 16**), Roslyn은 각각 0개였다. 이 간선을 시스템 카드에 집계하면 없는 연결을 만들 수 있다. Roslyn의 0은 이 세 방향에서의 관찰이며 전체 정확성 보증은 아니다. [근거 S07](comparison-claims.md#s07)

## 설치·준비·분석 비용

아래는 같은 WSL2 환경의 cold 1회·warm 3회다. cold는 분석 cache가 없는 상태이며 OS page cache를 비운 실행이 아니다. Roslyn은 매회 새 process다. peak는 GNU time의 명령과 대기한 자식의 최대 RSS이며 동시 process 합산이 아니다. 분석 열은 설치·restore·build·복사·export·채점을 포함한 총 대기 시간이 아니다. [근거 S08](comparison-claims.md#s08)

| 실행 | CodeGraph index 초 / peak bytes | 문법 보조 초 | CG 정규화 초 | Roslyn 추출 초 / peak bytes | Roslyn 정규화 초 | 근거 |
|---|---:|---:|---:|---:|---:|---|
| cold | 1.072 / 472,920,064 | 0.367 | 0.177 | 7.694 / 446,783,488 | 0.086 | [S09](comparison-claims.md#s09) |
| warm 1 | 0.821 / 475,070,464 | 0.368 | 0.180 | 7.393 / 459,411,456 | 0.092 | [S10](comparison-claims.md#s10) |
| warm 2 | 0.819 / 471,588,864 | 0.368 | 0.193 | 7.792 / 441,241,600 | 0.092 | [S11](comparison-claims.md#s11) |
| warm 3 | 0.872 / 475,209,728 | 0.368 | 0.191 | 8.043 / 452,370,432 | 0.102 | [S12](comparison-claims.md#s12) |

CodeGraph npm 설치는 4.713초였다. 설치 후 node_modules는 296,018,321 bytes, npm cache는 64,762,250 bytes였다. 이는 디스크 점유량이며 네트워크 전송량은 N/A다. 설치 peak memory도 계측하지 않았다. [근거 S13](comparison-claims.md#s13)

최종 준비의 restore 합계는 3.964초, 도구 build를 더하면 6.188초였다. 현재 runner는 measure 진입마다 새 NuGet cache를 만들며 기존 제품 의존성 다운로드가 준비 비용에 들어간다. 한 measure 안의 8회 분석은 준비된 상태를 공유한다. 반복 실행의 네트워크 의존과 cache 누적을 유지 비용으로 고려해야 한다. [근거 S14](comparison-claims.md#s14)

## 뷰어 기능에 쓸 수 있는 데이터

| 필요한 기능 | 현재 데이터와 제한 | 근거 |
|---|---|---|
| 1~2홉 탐색 | 양쪽에 AttackHandler.Handle→GameSession.SubmitAttack→GameMap.ProcessAttack이 이어진다. 두 번째 간선은 deferredLambda여서 즉시 실행 순서로 표시하면 안 된다. | [S15](comparison-claims.md#s15) |
| Client·ClientNet·Shared·Server 배치 | 양쪽 node에 layer가 있다. External과 CodeGraph의 Tools 노드도 있어 네 층 외 항목을 별도 처리해야 한다. | [S16](comparison-claims.md#s16) |
| 관계 종류별 표시 | 양쪽 edge에 calls·implements·usesType·contains가 있다. 종류별 필터의 재료가 되지만 usesType의 지원 한계는 아래와 같다. | [S17](comparison-claims.md#s17) |
| 모듈 접기 | 양쪽에 입력 root 5개의 module과 file/type/method 포함 관계가 있다. 다중 부모가 가능하고 중간 폴더·namespace 노드 트리는 없다. | [S18](comparison-claims.md#s18) |
| 두 지점 경로 질의 | sourceId·targetId로 인접 그래프를 만들 수 있다. 현재 CLI에는 경로 질의 명령이 없어 탐색 알고리즘과 UI가 더 필요하다. | [S19](comparison-claims.md#s19) |
| 패킷 중심 탐색 | packet kind 노드와 protocol ID/version 필드는 없다. 패킷은 일반 type으로 있어 선언 탐색은 가능하지만 송수신 의미 연결은 추가 작업이다. | [S20](comparison-claims.md#s20) |
| 시스템 카드 개요 | 같은 commitSha와 파일/디렉터리 경로의 codeReference를 node membership에 연결하는 접점이 있다. 실제 카드 자료 조인·상위 집계·운영툴 UI는 미구현·미검증이다. | [S21](comparison-claims.md#s21) |

## 선택 후에도 남는 해석 한계

Unity는 supplied managed DLL 기반의 부분 해석이며 InputSystem/TMP 패키지 assembly가 빠져 있다. Roslyn Unity compilation에는 Error 285·Warning 55·Hidden 250개가 기록됐다. Unity editor/windows 조건을 사용한 근사이며 Editor 컴파일·플레이 결과가 아니다. 양쪽 최상위 snapshot은 partial이다. [근거 S22](comparison-claims.md#s22)

CodeGraph는 compiler configuration·공급 DLL의 의미 해석 없이 문법/resolver로 연결하며, usesType은 명시적인 method→type 생성 중심의 부분 지원이다. Roslyn에도 메서드 후보를 미해석 usesType으로 분류한 사례가 남는다. 그러므로 작은 정답표의 좋은 점수만으로 전체 타입 사용 그래프를 신뢰하지 않는다. [근거 S23](comparison-claims.md#s23)

공통 계약의 알려진 미충족과 재현 제약은 [스냅샷 계약의 현재 한계](snapshot-contract.md#현재-구현-접점과-독립-확인의-한계)에 남겨 둔다. 최종 선택과 통합 진행은 [goal](goal.md)을 따른다.
