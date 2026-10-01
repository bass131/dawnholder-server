# Q-1 — 가독성 기준 정착

## 요청·목표·현재 상태

사람이 중요한 문제를 조사할 때 읽을 수 있는 코드를 유지하도록, 서버 C#에 재현 가능한 정적 검사와 독립 코드 리뷰를 정착시킨다. 새 코드뿐 아니라 기존 코드도 기준선을 집계하고 모듈별로 정리해 선택한 규칙의 미해결 위반을 0으로 만든 뒤 전체 error로 승격한다. 줄 수·주석량·단일 AI 점수는 완료 기준으로 삼지 않는다.

출처는 메인 Claude의 `msg_0355f528b164`(2026-10-01 14:01:41 UTC)와 [요청 원문 사본](../../../.backups/verification/2026-10-01-readability-baseline/main-request.md)이다. 사용자 결정은 메인을 통해 전달됐으며 사용자 직접 입력으로 격상하지 않는다. 로컬 evidence는 Git 제외이고 다른 checkout/GitHub에서 자동 접근되지 않는다.

- 상태: **초안 `30147f3`의 Fable 검토 완료. 메인 원문 확인·goal 보완·승인 대기. 구현·독립 Opus 검증은 미실행.** 아래 설계와 PR 분할은 아직 승인되지 않은 검토 대상 초안이며 F-1~F-17을 반영하기 전이다.
- 작업 경로: `C:/Dev/DawnHolder_Project`; branch: `bass131/q1-readability-20261001`.
- base: fetch로 확인한 `origin/main` = `0239290d6f423dbfe91c42c3fffd0789f56de26f` ([PR158](https://github.com/bass131/dawnholder-server/pull/158) 병합). [이전 goal](../2026-10-01-operations-rules/goal.md)의 승인 대기 표시는 당시 기록이다.
- 기존 로컬 `bass131/operations-rules-20261001`은 base의 ancestor 확인 후 `git branch -d`로 삭제했다. 고정 보관 branch는 건드리지 않았다.
- evidence 루트: `.backups/verification/2026-10-01-readability-baseline/`. 원문·실행 명령·로그·판정·세션 receipt를 여기에 보존한다.
- 메인 `msg_84770287842d`로 `CLAUDE.md` O-5 보충 쓰기 종료와 goal 커밋 포함 허용을 받았다. R-7 Fable 쓰기 예외 한 줄(+1/-0)을 실제 diff/hash로 대조했으며 독립 검토는 아직 남았다.
- 최초 관측: 화면 `GPT-6-Astra xhigh`; backend 실제 모델 `unknown`. 새 Q-1 Run은 `run_a3a4a4d552d1`이다. 이전 목표의 실행 권한을 재사용하지 않는다.

## 범위와 보존할 동작

`Dawnholder.slnx`의 8개 프로젝트, `02_Server`, `04_ClientNet`, `98_Shared`, `99_Tools`의 C# Compile 입력을 대상으로 한다. `GameServer.Tests`, 도구·봇도 범위에 포함하며 과거 하위 `.editorconfig`의 완화 때문에 측정에서 빠지지 않게 한다. 솔루션 밖 C#과 Unity 에셋은 대상이 아니다.

관련 파일은 루트/하위 `.editorconfig`, `Directory.Build.props` 및 필요한 검사 도구, `.github/workflows/dotnet-tests.yml`, `.git-blame-ignore-revs`, 해당 C# 소스와 검증 테스트, `CODE_CONVENTION.md`, 필요한 실행 안내, goal-loop의 검증 지시다. 정확한 구현 파일 소유권은 각 작업 spec에 고정한다.

- 제외: Unity 분석기와 에셋 정리, 운영툴 TypeScript, 고유 오류 코드 체계, DB/영속화 기능, 프로토콜·PDL·패킷 ID/버전·직렬화 변경, 분석기/CLI 전역 설정·권한 변경.
- 생성 코드·외부 소스·`obj`/`bin`은 수기 코드와 별도 목록으로 구분한다. 생성물 경로와 판정 근거를 기준선에 남기며, 파일을 생성물로 바꿔 위반을 숨기지 않는다. 특히 `98_Shared/Protocol/Generated/GenPackets.cs`는 프로토콜 보존 때문에 재생성·수동 정리하지 않는 제안이며 메인 승인으로 제외 범위를 확정한다. 이를 포함한 전체 파일 수와 실제 검사 파일 수를 함께 보고한다.
- 현재 공개 API, 패킷 바이트, 세션·큐·맵 소유권, 게임 틱 순서, 실패/취소/종료·예외 동작을 보존한다. 메서드 추출과 멤버 이동은 필드 초기화·static 초기화 순서, 락 범위, await 경계, 할당·반복 비용을 바꾸지 않는지 검증한다.
- Shared/ClientNet 변경은 서버와 Unity 사용처의 계약을 함께 확인한다. Unity 실행을 생략한 경우 이를 별도로 남기며 서버 빌드를 Unity 검증으로 표현하지 않는다.

## 완료조건

1. SDK·포매터·분석기 버전, 규칙 기본값·활성 조건·라이선스와 검사 범위를 1차 출처 및 실제 설치물로 확인하고 고정한다. 규칙별 양성/음성 fixture로 진단 발생과 차단을 증명한다.
2. 서식을 범위 전체에 적용한 **공백 전용 커밋**이 별도로 있고, `git diff -w <parent> <commit> --exit-code`가 성공한다. 포매터 검사가 로컬·CI에서 같은 범위와 버전으로 재실행 시 변경 0을 반환한다. 이 커밋의 실제 SHA를 후속 커밋의 `.git-blame-ignore-revs`에 넣는다.
3. 선택한 복잡도·중첩·순서·주석 규칙은 새 파일과 변경 파일의 **파일 전체**에서 위반이 있으면 표준 로컬 검사와 CI 빌드 검사를 실패시킨다. 추가된 줄만 검사하지 않는다. 기존 8개 파일의 error와 IDE0011 보장을 약화하지 않는다.
4. 규칙×모듈 기준선에 commit·설정 hash·실제 SDK/분석기·Compile 입력 목록·생성물 제외 목록·원시 진단 경로가 있다. 경고 0과 분석기 미실행/파일 누락/실패를 구별한다. 중복 진단은 project/TFM/path/rule/location 기준으로 설명 가능하게 집계한다.
5. 승인된 모듈 배치가 독립 동작 보존 검증을 거쳐 미해결 위반 0에 도달한다. 해당 모듈을 전체 파일 error로 승격하고 기존 파일의 신규 위반도 차단되는지 증명한다. 모든 대상 모듈의 승격 전에는 Q-1 전체 완료로 보고하지 않는다.
6. 코드 기준에는 도구 판정 표·기본 임계값·국소 예외·리뷰 체크포인트와 앵커 예시가 있고, goal-loop 검증 지시에는 아래 코드 리뷰 절 계약이 반영된다.
7. 신규 외부 Opus가 실제 diff·구현 보고·실행 근거를 실사하고 독립 테스트와 코드 리뷰를 수행한다. 필수 결함은 새 Sol/새 Opus로 해결한다. 각 PR은 사용자 명시 병합 승인 이후에만 통합한다.

## 결정론 계층 설계

### 규칙과 도구

아래 수치는 메인 요청으로 채택한 기본값이다. 아직 설치 실측값이 아니다. 실제 버전의 기본값과 다르면 임의 조정하지 않고 근거와 영향으로 메인에 올린다.

| 구분 | 선택 규칙/정책 | 설치·검증 시 확인할 것 |
|---|---|---|
| 서식 | `.editorconfig` 레이아웃 + IDE0055, `dotnet format whitespace ... --verify-no-changes` | SDK 내장 포매터 실제 버전·slnx 지원·Compile 범위·줄바꿈/들여쓰기 결과 |
| 인지 복잡도 | Sonar S3776, 기본 임계값 15 | 설치 패키지 기본값·활성 상태·severity·동일 메서드의 15/초과 경계 |
| 제어 흐름 중첩 | Sonar S134, 기본 임계값 3 | 계산 대상 구조와 3/초과 경계 |
| 멤버 순서 | SA1201/SA1202/SA1203/SA1204/SA1214 | 각 규칙 실제 의미·필드 초기화 동작 보존·테스트/도구 상속 |
| 주석 배치 | SA1515/SA1512 | 연속 주석·파일 첫 주석 등 적용 예외를 설치 버전으로 검증 |
| 문서/시그니처 일치 | SA1612 | 실제 진단 의미와 XML 문서 분석 활성 전제 확인. 이름·순서 전체 검사를 이 ID 하나가 보장한다고 선결론 내리지 않음 |
| 기존 중괄호 | IDE0011, `when_multiline` | 기존 보존 기준을 유지하고 전체 승격 대상에 포함 |

CA1502/CA1506은 기존 관찰과 분리하며 인지 복잡도 대용으로 쓰지 않는다. CA1505, 함수/파일 줄 수·주석량은 차단 규칙으로 추가하지 않는다. 새 분석기에서 선택하지 않은 규칙을 우연히 전면 적용하지 않으며 선택 규칙을 끄는 우회도 금지한다.

`SonarAnalyzer.CSharp`는 현재 props에 없으므로 설치 전 정확한 패키지 버전·출처·라이선스·Roslyn/SDK 호환을 확인한다. StyleCop은 현재 `1.2.0-beta.556`이며 변경 필요성 없이 업데이트하지 않는다. Microsoft `AnalysisLevel`은 현재 SDK 계열의 명시 값으로 고정하는 제안이고, 서드파티 버전/설정 고정은 별도다. SDK `global.json`의 `rollForward: latestFeature`와 CI의 `10.0.x`가 동일 결과를 보장하는지도 실측해 로컬·CI 버전을 일치시킨다.

XML 문서 분석이 꺼져 SA1612가 무효가 되는 경우를 반드시 probe한다. 문서 전체 작성 강제 같은 새 정책으로 넓히지 말고 실제 활성화 조건과 새 경고 영향을 기록한다.

### 변경 파일 검사

제안은 **하나의 저장소 검사 진입점**에서 명시적인 base SHA와 검사 대상 HEAD/작업 트리, SDK 버전으로 파일 목록과 전체 분석 결과를 만들고 차단 결과를 반환하는 방식이다. 이 진입점을 CI 빌드 작업과 로컬에서 같이 사용한다. 제품 분석기 자체의 severity와 검사 진입점의 실패를 구별하며, 아직 승격되지 않은 모든 파일이 단독 `dotnet build`에서 error가 된다고 보고하지 않는다.

- PR: 검증 대상 head와 대상 branch의 merge-base SHA를 명시한다. checkout의 합성 merge commit/head 차이를 기록하며 shallow clone·누락 base를 조용히 건너뛰지 않는다. main push에도 명시적인 비교 base를 기록한다.
- 로컬: 같은 base/head 조합이면 CI와 같은 목록을 반환한다. 작업 트리 모드에서는 base 이후 committed 변경과 staged/unstaged 변경·새 untracked C#를 합친다. 삭제 파일은 제외하고 rename은 새 경로를 검사한다. 경로 구분자·공백·대소문자·링크 입력을 명세하고 Git의 NUL 구분 목록을 사용한다.
- 새/변경 파일의 선택 규칙 위반은 파일 전체에서 차단한다. 미변경 legacy 파일은 기준선에 집계하되 단계적 적용 기간에는 선택 규칙의 기존 경고로만 남긴다. 전체 error로 승격한 모듈은 변경 여부와 무관하게 차단한다.
- 실제 분석 결과는 구조화된 진단(SARIF 등)으로 받고 텍스트 로그 grep만으로 판정하지 않는다. 컴파일 실패, 분석기 로드 실패, 누락/오래된 결과, 누락 프로젝트는 실패로 처리한다. 캐시로 분석이 생략되지 않게 재분석 조건을 고정한다.
- Tests/Tools의 하위 설정이 선택 진단을 `none`으로 덮어쓰지 않도록 정리한다. 현재 8파일 error는 유지한다. 국소 suppression은 위치·규칙·이유·원시 위반 수를 기록하며 미해결 0과 허용 예외 수를 따로 보고한다.
- 기준선 JSON의 숫자를 낮추거나 base/제외 목록을 임의 변경해 통과시키지 않는다. 검사 설정·도구·baseline 갱신은 diff와 독립 검증 대상이다.

이 접근은 하나의 cross-platform CLI로 Windows/WSL/CI를 지원하는 것을 우선하며 구현 언어·파일 경로는 실측 뒤 spec에서 고정한다. 실패 fixture에는 위반 추가, 미변경 legacy, rename/삭제/새 파일, 설정 하위 override, 결과 누락, 승격 모듈의 위반을 포함한다.

### 전체 서식과 새 파일 검사 충돌 처리

포매터가 모든 파일을 변경하면 그 PR의 모든 파일이 변경 파일 검사 대상이 된다. 이를 피하려고 whitespace diff를 변경 파일에서 영구 제외하거나 임의 baseline commit을 넣지 않는다. **서식 PR을 먼저 통합하고 그 최신 main에서 변경 파일 차단을 도입하는 다음 PR을 만든다**는 순서를 제안한다. 첫 PR은 서식 규칙을 즉시 CI에 연결하고, 두 번째 PR에서 의미 규칙을 도입하는 시점부터 해당 차단을 적용한다. 이 분할과 적용 시점은 메인 승인을 받는다.

공백 전용 정리는 `dotnet format whitespace`로 제한한다. 공백-only diff도 문자열·raw string·전처리 의미를 바꿀 수 있으므로 `diff -w`만으로 동작 보존을 단정하지 않고 빌드·관련 테스트 및 token/문자열 값 대조를 독립 검증한다. 설정·검사 도구·ignore-revs·문서는 공백 전용 커밋과 분리한다.

## 기존 코드 배치·PR 제안

**제안: Q-1 한 goal 안에 후속 PR로 유지한다.** 기준선→정리→전면 승격이라는 한 완료조건의 추적을 유지하기 위해서다. 각 PR branch는 앞 PR 병합 뒤 최신 main에서 만들고, 승인되지 않은 병합이나 임의 stacked branch는 만들지 않는다. 메인이 별도 목표 분리를 선택하면 이 goal에 승인된 링크와 남은 범위를 남긴다.

| 순서 | PR 단위와 범위 | 독립 검증과 다음 조건 |
|---|---|---|
| A | 레이아웃 설정·서식 검사·공백 전용 전체 정리·ignore-revs·운영 설명 | 공백 전용 commit 증명, formatter 재실행, 전체 build/test, 문자열·생성물·DLL 보존. 사용자 병합 승인 뒤 B |
| B | Sonar/선택 StyleCop 활성화·기준선·변경 파일 차단·리뷰 기준 | 규칙별 양성/음성 fixture, 실제 프로젝트 진단 집계, 로컬/CI 동일 입력 대조, 새/변경 파일 차단. 사용자 병합 승인 뒤 C |
| C1 | Network 및 ClientNet의 동작 보존 정리·모듈 승격 | 프레이밍·길이 검증·send/close·재연결 수명 계약. Unity 사용처 확인과 미실행 범위 별도 |
| C2 | Shared 수기 소스 정리·승격 | packet round-trip/byte parity·공유 API·Unity 소비 계약. 생성 코드/PDL 변경 없음 |
| C3 | 99_Tools 프로젝트별 정리·승격 | PacketGenerator exit/생성 바이트·봇 시나리오·BgmComposer 출력 계약. 프로젝트별 별도 PR 가능 |
| C4 | GameServer의 도메인별 정리·승격 | 세션/핸들러, Maps/Actions, Systems/Transitions 등 책임 경계별 정상·실패·취소·종료 테스트 |
| C5 | 테스트 코드 잔여 정리 및 전체 최종 승격 | assertion·fixture·coverage 의미 보존, 선택 규칙 전범위 재집계, 전체 error와 음성 fixture |

크기는 공백 전용 A를 제외하고 **한 책임 경계/독립 검증 가능한 배치당 한 PR**로 한다. 실측 기준선의 파일 수·진단 수·변경 파급을 보고 너무 큰 모듈은 쪼갠다. 임의 줄 수 상한으로 코드 설계를 바꾸지 않는다. 표의 순서는 제안이며 기준선 후 구체적인 파일 목록·예상 크기·검증 계약을 메인에 제시해 확정한다. 관련 테스트 정리는 각 배치에서도 가능하되 Opus가 테스트 파일을 단독 소유한다.

모듈 승격 조건은 전체 Compile 입력이 분석됐다는 근거, 선택 진단 미해결 0, 국소 예외 목록의 독립 검토, 관련 동작 보존 테스트 통과, 신규 위반 차단 재현이다. 제외·임계값 확대·규칙 비활성화로 0을 만드는 것은 허용하지 않는다.

## 체크포인트 계층과 독립 검증 계약

결정론 검사 통과와 사람이 읽기 좋은지는 별도 판정이다. 독립 검증자는 구현자의 쓰기 종료 후 **보고↔실제 diff↔원시 명령/출력**을 먼저 실사하고 요구사항·보존 동작으로 독립 테스트를 작성·보완·실행한다. 제품 파일은 수정하지 않으며 결함에 번호를 붙여 반환한다.

검증 보고에 `코드 리뷰` 절을 두고 (1) 주석의 의미가 실제 조건·예외·수명과 일치하는지, (2) 멤버 순서가 실행 흐름/논리를 설명할 수 있는지, (3) 책임·의존성·초기화/실행 순서 보존을 검토한다. 결정론 규칙과 충돌하는 정렬은 조용히 강제하지 말고 국소 근거와 대안을 메인에 올린다.

- 지적 형식: `file:line | 문제와 읽기/변경 위험 | 대안 | 병합 차단/후속 처리/참고`.
- 앵커 예시(가상이며 현재 결함 아님): `ExampleHandler.cs:42 | 주석은 실패 시 해제를 말하지만 return 경로에 해제가 없음 | cleanup 소유권과 설명을 일치시킴 | 병합 차단`.
- 앵커 예시(가상): `ExampleSystem.cs:80 | 같은 흐름의 helper가 추가 날짜순으로 흩어져 호출 추적이 어려움 | 호출 책임 순서로 묶음 | 후속 처리`.
- `검토했고 지적 없음`과 `검토 안 함`을 명시적으로 구별하고 후자의 범위/이유를 적는다. 읽기 편함의 주관적 점수나 stylistic 취향만으로 blocking 판정하지 않는다.
- M-1 V3 코드 리뷰 결과는 아직 수신하지 않았다. 결과가 오면 원문·채택/비채택 근거를 goal에 기록하고 체크포인트를 보완한다. M-1 성공을 선결론으로 쓰지 않는다.

필수 결함은 새 Sol 수정→새 Opus 재검증으로 처리하며 같은 번호가 3번 재검증에 실패하면 메인에 보고한다. 파트 검증자는 동시에 하나만 유지한다. 최종 원문은 메인이 직접 읽고 [R-2](../../../00_Document/operations/ORCA.md#r2-source-check)의 원천 표본 대조를 한다.

## 실행 부작용과 보호 절차

현재 `98_Shared/Shared.csproj`와 `04_ClientNet/Dawnholder.Client.Net.csproj`의 `CopyToUnityPlugins`는 Build 후 Unity Plugins DLL을 복사한다. 소스가 embedded PDB에 포함되므로 공백 정리도 DLL hash를 바꿀 수 있다. `dotnet format`의 프로젝트 로드/restore/분석에도 부작용이 없는지 전후 확인한다.

1. 빌드/포매터 전 Git 상태, 대상 DLL의 존재/내용 hash와 로컬 변경 여부, Unity 보호 파일·stash·두 설정 파일 hash를 남긴다. 현재 보호 기준은 `preservation-before.json`이다.
2. 실행은 [DEVELOPMENT](../../../00_Document/operations/DEVELOPMENT.md)의 원본 경로별 WSL 복제 공간과 lock을 우선 사용한다. WSL 빌드는 Windows 원본 DLL을 갱신하지 않는다는 문서 계약을 실제 전후 hash로 대조한다. Windows 실행이 필요하면 검사 소유자 한 명이 정확한 출력 파일을 사전 보존한다.
3. DLL 복사로 생긴 변경은 요청된 소스 정리와 구별한다. 사용자 기존 DLL을 덮어써 버리지 않으며, 원래 깨끗했음을 증명하거나 사전 사본으로 복원할 수 있는 정확한 파일만 처리한다. 무관한 변경에 reset/checkout을 사용하지 않는다.
4. Unity 3파일의 skip-worktree와 내용, stash 2개, 양쪽 checkout의 `.claude/settings.local.json`, `.meta`·GUID·직렬화 값은 보존한다. 영구 실행 정책·권한 우회·전역 설정 변경을 하지 않는다.
5. DB와 포트 7777은 본 설계·정적 검사에서 사용하지 않는다. 동작 보존에 실행이 필요하면 해당 배치 spec에 자원 소유와 필요한 계약을 정하고 기존 프로세스를 임의 종료하지 않는다.

## 소유권·검토 순서

- 메인 Claude: 사용자 결정·범위·승인, `CLAUDE.md` O-5 보충 단독 쓰기, Fable 원문 확인과 보완 goal 승인, 최종 원문/표본 대조와 병합 승인 요청.
- GameDev Astra: goal/CURRENT·설계·분할·근거 통합·Git/PR 단독 담당. 보고서 자료·생성 스크립트가 필요하면 Astra 소유다.
- Fable 시범 2회차: 새 `claude-fable-5-1`, 쓰기는 이 폴더의 `goal-review.md`만. 제품·goal·테스트·설정 변경과 추가 위임/commit/push 금지. 한 작업 정산 후 종료.
- 구현: 승인 뒤 새 `gpt-6.1-sol` xhigh, 해당 배치의 제품/검사 도구/문서만 소유. 테스트 작성·검증 판정은 신규 Opus에게 맡긴다. 파일 소유권이 겹치는 동시 쓰기를 금지한다.
- 독립 검증: 새 `claude-opus-5-5`, 할당 테스트와 로컬 verdict/근거만 쓰기. 구현 파일 결함은 번호로 반환한다. 실사·독립 테스트·코드 리뷰 및 미실행 Unity/DB/플레이를 구별한다.

[R-7](../../../00_Document/operations/ORCA.md#r7-fable-pilot)에 따라 goal commit → 메인에 경로/SHA status → 신규 Fable 검토 → 메인 원문 확인 → goal 보완 → **메인 승인** → Sol 순서다. 작업자는 [R-5/R-6](../../../00_Document/operations/ORCA.md#r5-worker-launch)의 Astra 아래 split·첫 화면·준비 확인·최초 attach 절차를 따르며 모델/실행 명령/화면/backend unknown을 구분한다. 모든 작업자는 작업 하나 뒤 정산·종료하고 재사용하지 않는다.

## 메인 결정 요청과 다음 작업

1. A 서식/CI → B 분석기/변경 파일 차단 → C 모듈 정리·승격을 **동일 Q-1 goal의 후속 PR**로 둘지 승인받는다. 이는 대규모 서식 변경이 기존 위반 전부를 한꺼번에 차단하는 충돌을 피하기 위한 순서다.
2. 생성 코드 별도 집계/보존 범위와 Tests/Tools의 선택 규칙 단계 적용을 확정한다. 메인 요청의 slnx 전체를 임의 production-only로 축소하지 않는다.
3. Fable 검토 후 실제 도구 probe 결과로 세부 구현 계약을 확정한다. 규칙 의미/기본값/라이선스가 채택 전제와 다르면 코드 구현을 진행하기 전에 해당 사항을 메인에 올린다.

아직 formatter·빌드·테스트·분석기 설치·기준선 측정·코드 정리·PR 생성은 수행하지 않았다. Q-1 전체 위반 수, 완료 예상 PR 수, Unity 동작 보존은 미확인이다.

## Fable 검토 결과와 인계

- [검토 원문](goal-review.md)은 초안 `30147f3a2a5eb16d940a0dfd1f331cf74304303c`에 대한 Fable의 기록으로 보존한다. 필수 보완 12건(A 발행·병합 전 8건/B 발행 전 4건), 후속 5건, 참고 5건을 **검토자가 보고**했다. 번호별 채택/보완 판단과 메인 원문 확인은 아직 남았으며 원문 전체를 Astra가 읽었다.
- 새 `claude-fable-5-1` 세션의 Task `task_e636368193d1`, Dispatch `ctx_d64fa7a76a36`. 화면 `Fable 5.1 xhigh`, backend `unknown`. 최초 실행 명령 `fable-1-launch-command.txt`, 첫 화면 `fable-1-first-read.json`, ready/input_accepted/turn_started는 `fable-1-start.json`에 있다. 첫 화면 15행에서 선택창은 미관측이며 설정을 변경하지 않았다.
- 완료 `msg_e0745dde030b`와 실제 변경을 대조했다. 저장소 변경은 `goal-review.md` 한 파일이며 당시 goal/CLAUDE/CURRENT diff는 0이었다. 검토자는 scratchpad의 임시 Git 저장소에서 Git 동작을 재현했고 설치·포매터·빌드·제품 테스트는 실행하지 않았다고 보고했다. 원시 명령/결과가 별도 저장되지 않은 항목은 보고서의 실측 표와 직접 원천을 구분한다.
- 원문 사본: `.backups/verification/2026-10-01-readability-baseline/fable-1-goal-review-original.md`, SHA256 `0DF38451A4CA6B7023C089AB703D612C8E33178785B01EA31903799AC532A000`. 메인 전달 `msg_7297e42c4217`에 원문 경로와 아래 한계를 즉시 알렸다.
- **해석 주의**: F-2의 프로필 443개와 NuGet 기본 활성 연결은 원문 스스로 기억 기반·미실측으로 표시했지만 영향/요약에서 단정했다. 실제 설치 시 활성 규칙 수를 확인한 사실로 채택하지 않는다. F-1도 master 라이선스의 요약 도구 경유 관찰이며, 원문 전체 직접 읽기는 권한 거부로 못 했고 고정 패키지의 라이선스와 일치는 미확인이다. 해당 조항의 적용·수용을 Astra/Fable가 결정하지 않는다.
- 주요 보완 대상은 F-3 공백 증명, F-5 원본 Git과 WSL 분석/서식 반영 경계, F-6 SDK 일치, F-7 DLL 갱신, F-8 공백 커밋 보존, F-9 쓰기 소유권, F-10 probe 단계, F-11/12 트리·fixture·기준선 계약이다. 메인 원문 확인 후 설계와 소유권을 보완하며, routine 구현 선택과 사용자 결정 영역을 다시 구분한다.
- 완료 settlement 수신 뒤 runtime/incarnation/checkout 동일성을 확인해 release(`retained/external_terminal`, processAction none) → 정확한 Fable pane close(`ptyKilled=true`)를 수행했다. `fable-1-{before-close-show,final-identity,release,close,ack}.json`이 근거다. Delivery `delivery_9e4af9aaf16e` acknowledge 완료, reclaimable 0. 해당 세션을 재사용하지 않는다.
- 다음 입력은 메인의 **원문 확인과 보완 방향**이다. 회신 주소는 `run:run_a3a4a4d552d1`. Sol 발행·설치·제품 변경은 보완 goal의 메인 승인 이후다. 이번 상태 기록은 goal 설계의 승인이나 구현 완료를 뜻하지 않는다.

## 확인한 1차 자료와 남은 확인

- [Microsoft dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format): whitespace 하위 명령과 `--verify-no-changes`의 비정상 종료 계약, `.editorconfig` 사용 및 restore/compile 가능성을 확인했다. 실제 SDK 10 설치물의 옵션·slnx 동작은 별도 실측한다.
- [Microsoft MSBuild 속성](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#analysislevel): AnalysisLevel 고정 설계의 공식 진입점. 적용 값과 기존 활성 규칙 차이는 설치 SDK에서 확인한다.
- Sonar S3776/S134 공식 페이지 직접 조회는 502로 실패했다. 요청 원문의 15/3을 검증 완료로 기록하지 않았으며 패키지 공식 소스/메타데이터·probe로 재확인할 작업이 남았다.
