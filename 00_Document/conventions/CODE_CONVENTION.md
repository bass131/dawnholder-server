# 코드 규칙

프로젝트 공학 조건은 [AGENTS](../../AGENTS.md)에 있다. 이 문서는 수정한 코드의 구조·표기 기준이다. 관련 규칙만 적용하고 무관한 파일을 일괄 변경하지 않는다.

## 상태와 책임

- 맵 상태는 해당 틱 실행 흐름에서 변경한다. 외부 요청은 작업 큐로 전달하고 틱에서 I/O 완료를 기다리지 않는다.
- 전송·프레이밍은 게임 규칙에 의존하지 않는다. 공유 데이터·공식과 서버의 적용·판정을 구분한다.
- 클래스의 변경 이유가 여러 도메인에 걸치면 책임 분리를 검토한다. 상태·큐는 소유 컨테이너에, 개별 판정은 시스템에 둔다. 줄 수만으로 파일을 나누지 않는다.
- 시스템 실행 순서를 명시한다. 시스템끼리의 호출로 상태 소유권과 actor 경계를 우회하지 않는다.
- 같은 이유로 함께 바뀌는 로직이 세 곳 이상이면 추출한다. 모양만 같고 변경 이유가 다르면 분리해 유지한다.
- 사용처 없는 확장 hook·추상화를 추가하지 않는다. 풀링·공간 분할 등 성능 변경은 측정 결과로 판단한다.
- 조합을 우선하고 일반 상속 깊이는 1 이하로 제한한다. 기존 `Session → PacketSession → Game/Unity/BotSession`은 프레이밍과 도메인 콜백을 분리하는 의도된 깊이 2 예외다. 깊이 3 이상은 사용하지 않는다.

- 상태마다 단일 소유자와 수명을 드러낸다. 누가 생성·변경·정리하는지, 실패·취소·종료 시 자원과 대기 작업을 어떻게 처리하는지 계약에 포함한다.
- 책임과 의존성 방향을 명시한다. 함께 변경되는 이유와 소유권을 기준으로 경계를 정하고, 호출 편의를 위해 상태 소유자나 실행 경계를 우회하지 않는다.
- 외부 I/O·시간·Unity 실행 경계를 게임 규칙과 구분해 독립 검증할 수 있게 한다. 분리한 경계 자체와 실제 통합·실패 경로의 검증 범위를 함께 정한다.
- 외부 입력은 경계에서 길이·범위·현재 상태·소유권을 검증한다. 잘못된 입력이나 늦게 도착한 결과가 상태에 미치는 영향과 오류 전달 책임을 드러낸다.
- 실제 변경·소유·검증에 필요한 경계만 추상화한다. 추출한 책임이 무엇이고 어떤 변경 이유를 분리하는지 설명할 수 있어야 한다. 추상화나 패턴 적용 자체를 목표로 삼지 않는다.
- 게임 틱·프레임 동작, 프로토콜·공유 DLL, Unity 자산 계약은 [AGENTS 공학 조건](../../AGENTS.md#공학-조건)과 해당 [영역별 계약](../domains/INDEX.md)을 보존한다. 실행 전 생성물·DLL 복사 등 부작용은 [DEVELOPMENT](../operations/DEVELOPMENT.md)에서 확인한다.

## 파일 위치와 이름

새 파일을 만들기 전에 같은 책임의 기존 파일 1~2개를 찾는다. 위치·이름·재사용할 helper와 선택 이유를 작업 전 맥락 메모에 적는다. [기능 지도](../FEATURE_MAP.md)와 [구조의 책임 경계](../ARCHITECTURE.md)를 출발점으로 쓰며 저장소 전체를 훑지 않는다.

| 위치 | 모을 책임 |
|---|---|
| `02_Server/` | 서버 권위·판정·상태와 서버 전송·저장소 등 독립 책임의 프로젝트, 해당 테스트 |
| `03_Client/` | Unity 실행·표현·입력과 Unity 자산 |
| `04_ClientNet/` | Unity API에 의존하지 않는 클라이언트 전송 라이브러리 |
| `98_Shared/` | 서버와 클라이언트가 함께 소비하는 프로토콜·데이터·공식 |
| `99_Tools/` | 생성·검사·DB 관리·개발 실행 도구와 도구별 테스트 |
| `05_Management/` | 운영 애플리케이션의 화면·기능과 해당 테스트 |

같은 책임은 모으고 성격이 다른 도구를 한 폴더에 평평하게 섞지 않는다. 기존 하위 구조로 담을 수 없을 때만 목적이 분명한 폴더를 만든다. 이름은 무엇을 하는지 드러내고 주변의 대소문자·동사-명사·접두사 관례를 따른다. 제품·도구의 파일·폴더·식별자에 마일스톤 코드, 날짜, 작업자 이름을 붙이지 않는다. 승인·환경·실행 이력은 코드 이름이나 상수에 넣지 않고 설정·manifest·목표 기록으로 분리한다.

이동·이름 변경은 호출·dot-source·빌드/검증·문서 링크를 함께 갱신하고 이전→새 경로 대응표를 남긴다. 독립 검증자는 동작과 별도로 책임별 배치, 이름의 의미, 처음 읽는 사람이 찾을 수 있는지, 맥락 메모의 기존 관례를 실제로 따랐는지를 판정한다.

제품·도구 이름의 금지를 역사 goal의 날짜 접두 폴더 관례나 외부 계약 이름의 일괄 변경에 적용하지 않는다. 역할별 메모와 판정은 [작업 맥락 스킬](../../.agents/skills/dawnholder-task-context/SKILL.md)을 따른다.

## Unity와 이름

MonoBehaviour는 Unity 생명주기 연결을 맡고, 예측·상태 계산처럼 독립 검증 가능한 로직은 일반 C# 타입에 둔다. 네트워크 결과는 메인 스레드에서 Unity 객체에 적용한다.

| 대상 | 표기 |
|---|---|
| 인스턴스 필드·SerializeField | `_camelCase` |
| 매개변수·지역 변수 | `camelCase` |
| 공개 타입·메서드·상수 | `PascalCase` |
| 정적 필드 | 기존 영역의 `s_` 표기를 따름 |

직렬화 필드 이름을 바꾸면 `FormerlySerializedAs`와 기존 prefab·scene 값을 확인한다. `.meta`·GUID·문자열 에셋 경로도 함께 확인한다.

## 빌드가 검사하는 규칙

근거는 루트 [.editorconfig](../../.editorconfig)와 [Directory.Build.props](../../Directory.Build.props)다.

- SA1201: 필드 → 생성자 → 이벤트 → 프로퍼티 → 메서드 → 중첩 타입 순서.
- SA1202: 같은 종류에서 public → internal → protected internal → protected → private 순서.
- IDE0011: `when_multiline`; 여러 줄 본문에는 중괄호를 사용하고 한 줄 가드절은 허용한다.
- Tests·99_Tools는 하위 설정으로 일부 규칙을 완화한다. Unity는 [별도 props](../../03_Client/Directory.Build.props)로 NuGet 분석기 상속을 차단한다.

기본 production 범위는 warning이며, 첫 파일럿인 `02_Server/GameServer/Maps/GameMap.cs`, `Maps/Actions/MeleeAction.cs`, `Maps/Actions/DashAction.cs`에서는 위 세 진단을 error로 지정한다. [dotnet-tests CI](../../.github/workflows/dotnet-tests.yml)의 `dotnet build Dawnholder.slnx --no-incremental`이 이 세 파일의 위반을 실패시킨다. 서버 패킷 표현 통합에서는 `Sessions/GameSession.cs`, `Maps/Transitions/MapMigration.cs`, `Maps/MapPacketPublisher.cs`, `Maps/Systems/EnemyAISystem.cs`, `Maps/Systems/BossBehaviorSystem.cs`에도 같은 세 error 진단을 적용한다. 기존 StyleCop.Analyzers `1.2.0-beta.556`과 `EnforceCodeStyleInBuild`를 사용하며 전역 warnings-as-errors는 적용하지 않는다.

따라서 SA1201·SA1202·IDE0011의 error 적용은 S1의 3파일 + S2의 추가 5파일 = **총 8개 production 파일**이다. CI 빌드가 이 8파일의 세 진단 위반을 실패시키며, 그 밖 기본 production 범위는 warning이다. 이 적용 범위를 전체 소스의 error 검사나 전체 설계 기준 자동 검증으로 해석하지 않는다.

S1 파일럿의 세 파일에서만 .NET SDK 분석기의 CA1502·CA1506을 warning으로 관찰한다. 기본 임계값 초과 진단이므로 진단이 없는 상태를 연속 복잡도·결합 점수나 전체 설계 검증으로 해석하지 않는다.

Tests·99_Tools의 세 진단 완화와 Unity 분석기 격리를 유지한다. 범위 밖 경고는 남을 수 있으므로 빌드 성공이 경고 0을 뜻하지 않으며, 모든 이름·설계 규칙이 자동 검사되는 것도 아니다.

## C# 공백 서식

`02_Server`·`04_ClientNet`·`98_Shared`·`99_Tools`의 수기 C#에 LF, BOM 없는 UTF-8, 공백 4칸, 파일 끝 개행, 행 끝 공백 제거를 적용한다. `99_Tools/PacketGenerator/PacketFormat.cs`는 행 끝 공백 제거를 끈다. `98_Shared/Protocol/Generated/GenPackets.cs`는 formatter에서 명시적으로 제외하고 전후 hash가 같아야 한다. Unity 소스와 `.meta`·직렬화 자산은 이 서식 적용 범위에 포함하지 않는다.

SDK `10.0.301`의 `dotnet format whitespace`만 사용하며 추가 layout 규칙이나 선언 순서 변경을 포함하지 않는다. 실행은 [DEVELOPMENT의 서식 검사](../operations/DEVELOPMENT.md#c-서식-검사)를 따른다. `.editorconfig`의 기존 severity와 8 production 파일의 error, Tests·Tools 정책을 유지한다. 검사 도구와 `Formatting.Tests`는 제품 slnx에 넣지 않고 별도로 build/format/test한다.

제품 프로젝트 기대 집합의 정본은 `Dawnholder.slnx`로 두고 프로젝트 수를 고정 숫자로 제한하지 않는다. 독립 도구는 코드와 분리된 명시 목록으로 등록하며 제품 집합과 구분한다. 두 집합과 입력 manifest·실제 Workspace를 대조하고 restore·Compile·서식·보존 단계에 같은 대상을 연결한다. 누락·중복·예상 밖 프로젝트와 미등록 C#·Compile 누락은 계속 거부한다. 각 파트는 자기 PR에서 자기 도구 항목만 추가하며 검사 코드의 변경은 GameDev가 맡는다.

서식 전후 보존은 같은 SDK Workspace의 실제 프로젝트별 Debug/Release parse options로 token 종류·원문 순서·리터럴 값·주석 본문·directive를 전수 비교한다. 비활성 영역의 차이는 실제 활성 대응 조건에서 증명해야 하고 증명할 조건이 없으면 원문이 같아야 한다. 정규식으로 모든 공백을 제거한 문자열 비교는 의미 증명이 아니다. BOM·EOF와 생성 소스는 별도로 기록한다. `git diff -w`는 보조 근거이며 BOM·개행 등 잔여 hunk를 분류한다.

공백 커밋은 설정·도구·문서와 분리하고 실제 SHA를 `.git-blame-ignore-revs`에 기록한다. 로컬에서 `git blame --ignore-revs-file .git-blame-ignore-revs <파일>`로 사용한다. 원하면 저장소 한정 `git config blame.ignoreRevsFile .git-blame-ignore-revs`를 설정할 수 있으며 전역 Git 설정은 변경하지 않는다.

## SQL·PowerShell 작성

[001_initial.sql](../../99_Tools/database/migrations/001_initial.sql)의 DDL 배치와 [Database.Common.ps1](../../99_Tools/database/Database.Common.ps1)의 helper·자원 수명, [Test-Database.ps1](../../99_Tools/database/Test-Database.ps1)의 단계·검증 범위 주석을 관례의 출발점으로 삼는다. 다음 기준은 이번에 바꾸는 책임에 적용하며 무관한 파일의 일괄 정리로 넓히지 않는다.

- 들여쓰기는 공백 4칸이다. 중첩 `BEGIN/END`와 중괄호의 소속을 맞추고 연산자·쉼표 뒤 공백을 둔다. 한 줄에는 한 판단·대입·부작용을 두며 120자를 넘는 식·인자·열 목록은 의미 단위로 줄을 나눈다. 긴 경로·리터럴은 값 보존이 우선이다.
- 상태·결과·오류 숫자는 이름 있는 값, 조회 정의 또는 가까운 주석 표로 의미를 밝힌다. 외부 코드값을 가독성 수정 과정에서 바꾸지 않는다. 변수는 한꺼번에 나열하지 않고 입력·현재 상태·판정·결과 등 책임별로 묶는다.
- 호출은 이름 있는 인자를 우선하고 긴 인자 목록은 한 줄에 하나씩 둔다. PowerShell은 명명 인자나 splatting을 쓴다. SQL의 `EXEC`도 명명 인자를 사용하며 위치 인자만 가능한 표현에서는 각 인자의 의미가 드러나게 쓴다. 설명 없는 `NULL` 나열을 남기지 않는다.
- 같은 이유로 반복되는 검사는 함수·프로시저 한 곳에서 소유한다. 입력 검증 → 잠금·조회 → 판정 → 쓰기 → 결과 생성의 경계를 읽을 수 있게 나누되, 분리 때문에 transaction·잠금·오류 전파 순서를 바꾸지 않는다. 외부에 불필요한 실행 권한을 추가하지 않는다.
- JSON·결과 조립과 공통 환경 검사를 중복 작성하지 않는다. 큰 함수는 책임을 나누고 각 helper의 입력·출력·부작용을 이름과 계약으로 드러낸다. 줄 수를 줄이려고 서로 다른 단계를 한 줄에 압축하지 않는다.
- 주석은 구문을 번역하는 대신 검사 순서·잠금·의도된 NULL·정리 중단 조건 등의 이유를 설명한다. 세션 메시지 ID·실행자 SID·머신 경로 같은 승인/환경 값은 검증된 manifest·설정·명시 인자로 받는다. 설정으로 옮겨도 정확 대상·identity 대조와 실패 시 중단 경계를 유지한다.
- 동작 보존 정리는 SQL 객체 signature, 결과 열·순서·코드값, 오류 번호, 권한 대상과 PowerShell 진입 인자·부작용의 전후 대조를 남긴다. 서식·정적 대조, 오프라인 시험, 실제 SQL·계정 실행은 구분하며 가독성·배치·책임 분리를 독립 판정 항목에 넣는다.

## TypeScript·Electron 작성

현재 관례의 출발점은 Management의 [preload.cts](../../05_Management/frontend/electron/preload.cts), [catalog-store.ts](../../05_Management/frontend/electron/catalog-store.ts)와 해당 [package.json](../../05_Management/frontend/package.json)·[화면 tsconfig](../../05_Management/frontend/tsconfig.json)·[Electron tsconfig](../../05_Management/frontend/tsconfig.electron.json)·[MCP tsconfig](../../05_Management/frontend/tsconfig.mcp.json)다. 전달 예시의 `preload.ts` 대신 실제 `.cts` 경로와 CommonJS 경계를 확인한다. 기존 압축 한줄 표현은 가독성 모범으로 강제하지 않으며 다음 기준을 바꾸는 책임에 적용한다.

- 공백 2칸·세미콜론·작은따옴표를 따른다. 함수·변수는 camelCase, 타입·클래스·React 컴포넌트는 PascalCase를 쓴다. 한 줄에 서로 다른 판단·부작용을 압축하지 않고 긴 조건·인자·결과 조립을 의미 단위로 나눈다. 이름은 기존 하위 폴더의 책임과 관례를 따른다.
- 타입 전용 import를 드러내고 `strict`·`noUncheckedIndexedAccess`·`exactOptionalPropertyTypes`·`verbatimModuleSyntax` 계약을 보존한다. Electron/MCP의 NodeNext import는 기존 `.js` 경로 관례를 따르며 화면의 Bundler 해석이나 `.cts` 경계와 혼동하지 않는다. 외부 입력은 `unknown`에서 검증하고 타입 단언이나 `any`로 검증을 생략하지 않는다. 누락·`null`·오류 결과의 기존 의미를 보존한다.
- 화면 표현·순수 조회/변환·입력 계약·파일 I/O의 책임을 구분한다. [catalog-contract.ts](../../05_Management/frontend/electron/catalog-contract.ts)·[catalog-query.ts](../../05_Management/frontend/electron/catalog-query.ts)의 기존 계약/helper를 먼저 찾고 같은 검증·결과 조립을 중복 소유하지 않는다. 분리할 때는 변경 이유와 테스트할 경계를 설명하며 사용처 없는 추상화를 추가하지 않는다.
- preload는 좁은 API를 노출하고 renderer에 Node·파일 시스템·임의 IPC 접근을 넘기지 않는다. [main.ts](../../05_Management/frontend/electron/main.ts)의 sender·mainFrame·URL 검증과 `nodeIntegration: false`·`contextIsolation: true`·`sandbox: true` 경계를 보존한다. TypeScript 타입만으로 IPC 입력이나 발신자가 검증됐다고 보지 않는다.
- 저장 경계의 파일 handle·잠금·임시 파일·백업·버전 충돌·rename 순서와 실패 시 정리 책임을 드러낸다. Windows 공유 위반 재시도는 기존 대상/잠금의 소유와 제한된 수명을 보존하고 다른 실행자의 잠금을 임의 삭제하지 않는다. 주석은 이 순서·수명의 비자명한 이유를 관련 코드 가까이에 둔다.

기존 `typecheck`·`desktop:typecheck`·`mcp:typecheck`는 각 tsconfig의 타입 검사이고 Vitest는 실행한 테스트 범위의 근거다. 타입 검사 성공을 서식 lint·설계·IPC 통합 실행 통과로 쓰지 않는다. 이 절은 새 ESLint/Prettier 설치나 CI 연결 완료를 뜻하지 않는다.

## Python 도구 작성

관례 참고는 **미병합 Architecture `d0dffd1`**의 `99_Tools/Architecture/Pipeline/inputs.py` 경로/hash 경계와 `execution.py` argv·로그·소유 process group 수명이다. 해당 코드는 신규 Opus 검증 중이며 현재 main이나 확정 모범으로 삼지 않는다. 파트가 전달한 WSL Python **3.14.4**는 관측 환경이고 저장소 지원 최소버전/pin의 합의가 아니다.

- 모듈·함수·변수는 snake_case, 클래스는 PascalCase, 상수는 UPPER_SNAKE_CASE, 들여쓰기는 공백 4칸을 따른다. 긴 조건·인자·자료 조립은 의미 단위로 나누고 기존 압축 표현을 모범으로 강제하지 않는다. 모듈은 책임별로 모으며 도구 경계와 이름을 작업 전 메모에 설명한다.
- 관측 Pipeline은 표준 라이브러리만 사용한다. 기존 helper·입력/출력 계약을 먼저 확인하고 새 의존성·지원버전 변경이 필요하면 승인된 작업 범위를 대조한다. 설치나 실행 환경 관측을 저장소 전체의 의존성/버전 규칙으로 확대하지 않는다.
- 파일/manifest 입력은 경계에서 형식·상대 경로·대소문자·root 이탈·symlink·hash/크기와 소유를 확인한다. 복사·쓰기 전 대상 identity를 대조하고 입력 불일치를 성공 결과로 바꾸지 않는다. 경로 검증과 JSON 조립을 같은 책임의 helper에서 소유하며 Windows 원본과 WSL 복제 경계를 구분한다.
- 외부 명령은 argv·cwd·환경·timeout·exit·원시 stdout/stderr를 구분한다. 문자열 shell 조립으로 인자 의미를 바꾸지 않는다. 파일과 자식 프로세스는 소유 수명에서 정리하며 timeout/취소 시 자기 process group만 대상으로 한다. 코드 배치나 가독성 정리 때문에 종료·오류 전파 순서를 바꾸지 않는다.
- JSON·텍스트의 encoding·hash 대상과 실패 전달 책임을 명시한다. docstring·가까운 주석은 입력 계약·수명·제약의 비자명한 이유를 설명한다. 실행 성공·측정 값·관측 불가를 구분하고 미확인 값을 고정 성공값으로 채우지 않는다.

구문/compile 검사는 실제 입력의 처리·소유 process 종료·통합 동작 검증을 대신하지 않는다. 별도 lint/formatter는 관측하지 못했으며 이 절의 문서 보강을 Python 검사 구현이나 CI 통과로 표현하지 않는다.

## 주석과 문서

이름과 코드만으로 드러나는 설명은 반복하지 않는다. 권한·프로토콜·스레드·수명주기의 비자명한 이유와 공개 계약을 짧게 남긴다. 클래스 책임이나 요청 흐름을 설명할 필요가 있으면 해당 코드 가까이에 적는다. 과거 Phase 수행 내역과 긴 대안 검토는 [보관 기록](../archive/INDEX.md)으로 연결한다.

코드 탐색은 [기능 지도](../FEATURE_MAP.md)와 [진입점](ENTRY_POINTS.md)을 사용한다. 이전 규칙·판단 수치·변경 이력은 [정비 전 원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/CODE_CONVENTION.md)에 보존되어 있다.

- 이름·공개 API·테스트에 의도와 계약을 표현한다. 주석은 코드의 반복 설명보다 비자명한 이유와 제약을 남긴다. 사람과 AI가 변경에 필요한 맥락을 코드 가까이에서 찾을 수 있게 한다.
- 계약이나 탐색 경로가 바뀌면 관련 현재 문서를 함께 갱신한다. 작업 이력과 원문 로그를 코드 설명에 복제하지 않는다.

## 역할별 적용

설계·분할·위임·검증·통합 흐름과 전달 정보는 [목표 루프](../../.agents/skills/dawnholder-goal-loop/SKILL.md)를 따른다.

각 작성자와 검증자는 [작업 맥락 스킬](../../.agents/skills/dawnholder-task-context/SKILL.md)의 사전 메모·관련 절 원문 계약·실제 준수 위치·판정 양식을 적용한다. 맥락 메모 부재·관련 원문 누락·적용 규칙 위반·메모와 결과 불일치는 수정 또는 메인 결정 전 통과 차단이다. 검토했고 지적 없음·미검토·해당 없음과 이유를 구분한다.

측정값 위장(상수·수기 값을 실제 계측처럼 기록)과 제품의 계산을 기대값으로 복제해 항상 통과하는 테스트도 [추가 차단 정본](../../.agents/skills/dawnholder-task-context/SKILL.md#독립-판정과-통과-차단)을 따른다. 기존 테스트 실패를 바꿀 때는 그 정본과 [판정 양식](../../.agents/skills/dawnholder-task-context/references/templates.md#검증-판정)의 전수 분류·요구사항 출처·같은 명령 전후 실적을 남긴다.

구현자는 변경 경계와 설계 이유를 코드·계약에 반영하고 적절한 자체 점검을 수행한다. 변경 파일, 실제 점검 결과와 근거, 남은 위험·미실행 범위를 독립 검증자와 메인에게 전달한다. 자체 점검은 독립 검증을 대신하지 않는다.

독립 검증자는 구현자의 설명을 결론으로 삼지 않고 요구사항·보존 동작·원시 근거를 기준으로 검증한다. 코드 변경은 승인된 [검증 강도 4주 시범](../../.agents/skills/dawnholder-goal-loop/SKILL.md#검증-강도-4주-시범)의 강/약 필수 검증과 별도 테스트 소유권을 따르며 독립 세션은 생략하지 않는다. 정상 경로와 관련 실패·취소·종료 경로를 확인하고 실제 진입 실행·미실행 판정은 [작업 맥락 정본](../../.agents/skills/dawnholder-task-context/SKILL.md#독립-판정과-통과-차단)을 따른다. 구현 세부를 복제하는 테스트를 피하고, 결함은 구현자에게 돌려 수정 후 영향 범위를 재검증한다. 독립 리뷰에서는 책임·의존성·계약 보존과 변경 파급을 확인한다.

문서만 바뀐 경우에는 내용·참조·권한 일관성의 정적 검토가 적절하다. 코드 테스트, 통합 실행, 실제 플레이, DB 확인은 각각 수행한 범위와 미실행 범위를 구분해 보고한다. 역할별 모델은 [AGENTS 모델 라우팅](../../AGENTS.md#모델-라우팅)을 따른다.

## 변화 평가

### 교정 층과 반복 규칙

사용자 교정마다 **구조·타입·린트·테스트·문서 중 해당 실수를 막을 수 있는 가장 높은 층**을 선택한다. 문서로 끝내면 더 높은 층으로 막기 어려운 이유를 해당 goal에 남긴다. 첫 발생은 goal에 기록하고, 새 반복 규칙은 두 번째 발생부터 만든다. 기존 승인 규칙을 첫 발생이라는 이유로 무효화하거나 적용하지 않는 근거로 쓰지 않는다.

이 원칙은 교정 위치의 선택 기준이다. 검사·린트·CI의 실제 구현이나 현행 차단 변경은 각 goal의 승인 범위를 따른다. [운영 정본 반영](../../01_Phases/goals/2026-10-05-operating-canon/goal.md)의 PR1은 문서 원칙만 반영하며 후속 검사/helper 구현을 완료로 주장하지 않는다. 규칙 문서 가지치기는 2026-10-31 Gardener 평가 때 판단한다.

### 하네스 원칙

메인이 전달한 사용자 채택의 배경·적용 범위는 [하네스 목표](../../01_Phases/goals/2026-10-03-harness-principles/goal.md)와 [ADR-034](../ADR/harness/ADR-034-harness-principles.md)에 있다. 진행 중인 단계는 마치고 **다음 작업 계약부터 승인된 범위**에 다음 원칙을 적용한다.

1. 반복 규칙 중 기계로 판단할 수 있는 것은 검사로 옮긴다. 실패 진단에는 원인·대상 위치와 고치는 방법을 함께 제공한다. 실행하지 못한 검사를 위반이나 통과로 바꾸지 않는다.
2. 정본 helper·생성기·구조를 쓰는 경로를 가장 쉽고 짧게 제공한다. 진입 문서에서 실행 명령과 입력 계약을 바로 찾을 수 있게 하고, 같은 책임의 임시 helper를 계속 복제하지 않는다. 검증 없이 기존 helper를 정본으로 새로 지정하지 않는다.
3. 수기로 관리하는 경로 목록은 실제 존재와 드리프트를 검사할 대상으로 삼는다. 상대 기준·제외·파일/디렉터리·외부 URL 경계를 계약에서 정하고 출처 위치·수리 안내를 낸다. 후보만 등록한 검사를 선행 구현하지 않는다.
4. 새 검사 정책은 warning 파일럿 → 같은 조건의 실측 → **사용자 판단으로 error 승격 또는 유지/강등** 순서로 다룬다. 가치보다 유지 비용·잡음이 커지면 강등을 제안한다. 진단 severity, 검사 상태/exit, CI job 실패 여부를 따로 관측한다. 현행 선택 PS 진단의 exit1 같은 승인된 동작은 그대로 보고하며, warning이라는 이름으로 기존 차단을 조용히 완화하지 않는다. 도구·환경 실패와 실제 정책 위반은 다른 상태로 낸다.
5. 구조 변경과 동작 변경은 별도 커밋으로 나누고 각 변경의 보존 계약·실행 근거를 드러낸다. 이름/배치/서식 정리가 정책·결과·exit 변경을 숨기지 않게 한다.

이 원칙은 현행 주석 정책을 유지한다. 이 하네스 원칙만으로 주석 삭제 정책·작은 작업 예외·검증 강도 차등·규칙 가지치기를 새로 도입하지 않는다. 별도로 승인된 [검증 강도 시범](../../.agents/skills/dawnholder-goal-loop/SKILL.md#검증-강도-4주-시범)은 해당 정본의 한정 범위로 적용한다. 원칙을 적용하는 검사 구현·승격의 실제 권한과 범위는 각 goal 계약에 둔다.

### 사람의 탐색과 가독성

독립 검토에는 **30초 가독성 확인**을 포함한다. 처음 읽는 사람이 변경 대상 X의 정의/원천과 누가 X를 변경하는지를 코드 가까운 계약·이름·진입점에서 찾는지 확인하고, 실제 찾은 경로와 찾지 못한 경계를 남긴다. 30초는 사람 검토 기준이며 새 시간 측정 도구나 모델 점수로 대체하지 않는다. 린터 통과도 이를 대신하지 않는다. 가독성 지적은 [판정 양식](../../.agents/skills/dawnholder-task-context/references/templates.md#검증-판정)의 `file:line · 문제 · 대안 · 현재 차단/후속`으로 기록한다.

자동 판정 가능한 정적 기준은 구체적인 기준과 적용 범위를 확정해 첫 코드 적용부터 CI 검사에 연결한다. 책임 경계·테스트 품질·AI 탐색 부담은 독립 검토와 별도 측정으로 평가한다. 이 문서 정비는 CI 설정이나 검사 구현의 완료를 뜻하지 않는다.

자동 진단은 실제 설정·대상·도구/버전·실행 근거가 확인된 범위만 주장한다. 위 C# 분석기/서식, TS 타입 검사와 이후 선정할 PowerShell·SQL·Python 정적 진단의 검출 범위를 구분한다. 반복 RPC 뼈대·불필요한 변환·중복 책임·주석 위치·파일 배치/이름·탐색 부담은 사람 검토로 남기며 린터 성공으로 통과시키지 않는다. 승인된 검사 도입 계획의 실제 설정·명령·CI 상태는 해당 goal과 DEVELOPMENT에서 확인한다.

복잡도·결합·변경 파급·실패 검증·탐색 부담을 구분한다. 변경 목적에 관련된 지표와 독립 검토를 선택하고, 같은 범위의 기준선과 변경 후 결과를 비교한다. 숫자 개선이나 단일 AI 점수, 코드 줄 수·주석량·테스트 건수 자체를 목표로 삼지 않는다.

실제로 계측한 값과 미측정 판단을 구분한다. 비교 근거에는 대상·생성물 제외 범위·기준 및 변경 후 commit·측정 방법·실행 환경을 남기고, 모델 평가를 사용했다면 지정/확인 모델과 제공한 맥락 조건도 기록한다. 미커밋 결과는 해당 상태로 표시한다. 서로 다른 조건의 수치를 같은 추세로 해석하지 않으며, 필요한 결과와 근거 경로만 목표 기록에 남긴다.
