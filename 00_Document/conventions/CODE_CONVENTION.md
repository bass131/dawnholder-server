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

## 주석과 문서

이름과 코드만으로 드러나는 설명은 반복하지 않는다. 권한·프로토콜·스레드·수명주기의 비자명한 이유와 공개 계약을 짧게 남긴다. 클래스 책임이나 요청 흐름을 설명할 필요가 있으면 해당 코드 가까이에 적는다. 과거 Phase 수행 내역과 긴 대안 검토는 [보관 기록](../archive/INDEX.md)으로 연결한다.

코드 탐색은 [기능 지도](../FEATURE_MAP.md)와 [진입점](ENTRY_POINTS.md)을 사용한다. 이전 규칙·판단 수치·변경 이력은 [정비 전 원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/CODE_CONVENTION.md)에 보존되어 있다.

- 이름·공개 API·테스트에 의도와 계약을 표현한다. 주석은 코드의 반복 설명보다 비자명한 이유와 제약을 남긴다. 사람과 AI가 변경에 필요한 맥락을 코드 가까이에서 찾을 수 있게 한다.
- 계약이나 탐색 경로가 바뀌면 관련 현재 문서를 함께 갱신한다. 작업 이력과 원문 로그를 코드 설명에 복제하지 않는다.

## 역할별 적용

설계·분할·위임·검증·통합 흐름과 전달 정보는 [목표 루프](../../.agents/skills/dawnholder-goal-loop/SKILL.md)를 따른다.

구현자는 변경 경계와 설계 이유를 코드·계약에 반영하고 적절한 자체 점검을 수행한다. 변경 파일, 실제 점검 결과와 근거, 남은 위험·미실행 범위를 독립 검증자와 메인에게 전달한다. 자체 점검은 독립 검증을 대신하지 않는다.

독립 검증자는 구현자의 설명을 결론으로 삼지 않고 요구사항·보존 동작·원시 근거를 기준으로 검증한다. 실제 코드 변경에는 별도 소유권을 받은 테스트 코드를 작성·보완하고 실행하며, 정상 경로와 관련 실패·취소·종료 경로를 확인한다. 구현 세부를 복제하는 테스트를 피하고, 결함은 구현자에게 돌려 수정 후 영향 범위를 재검증한다. 독립 리뷰에서는 책임·의존성·계약 보존과 변경 파급을 확인한다.

문서만 바뀐 경우에는 내용·참조·권한 일관성의 정적 검토가 적절하다. 코드 테스트, 통합 실행, 실제 플레이, DB 확인은 각각 수행한 범위와 미실행 범위를 구분해 보고한다. 역할별 모델은 [AGENTS 모델 라우팅](../../AGENTS.md#모델-라우팅)을 따른다.

## 변화 평가

자동 판정 가능한 정적 기준은 구체적인 기준과 적용 범위를 확정해 첫 코드 적용부터 CI 검사에 연결한다. 책임 경계·테스트 품질·AI 탐색 부담은 독립 검토와 별도 측정으로 평가한다. 이 문서 정비는 CI 설정이나 검사 구현의 완료를 뜻하지 않는다.

복잡도·결합·변경 파급·실패 검증·탐색 부담을 구분한다. 변경 목적에 관련된 지표와 독립 검토를 선택하고, 같은 범위의 기준선과 변경 후 결과를 비교한다. 숫자 개선이나 단일 AI 점수, 코드 줄 수·주석량·테스트 건수 자체를 목표로 삼지 않는다.

실제로 계측한 값과 미측정 판단을 구분한다. 비교 근거에는 대상·생성물 제외 범위·기준 및 변경 후 commit·측정 방법·실행 환경을 남기고, 모델 평가를 사용했다면 지정/확인 모델과 제공한 맥락 조건도 기록한다. 미커밋 결과는 해당 상태로 표시한다. 서로 다른 조건의 수치를 같은 추세로 해석하지 않으며, 필요한 결과와 근거 경로만 목표 기록에 남긴다.
