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

위 항목은 경고로 설정되어 있다. 빌드 성공이 경고 0을 뜻하지 않으며, 모든 이름·설계 규칙이 자동 검사되는 것도 아니다.

## 주석과 문서

이름과 코드만으로 드러나는 설명은 반복하지 않는다. 권한·프로토콜·스레드·수명주기의 비자명한 이유와 공개 계약을 짧게 남긴다. 클래스 책임이나 요청 흐름을 설명할 필요가 있으면 해당 코드 가까이에 적는다. 과거 Phase 수행 내역과 긴 대안 검토는 [보관 기록](../archive/INDEX.md)으로 연결한다.

코드 탐색은 [기능 지도](../FEATURE_MAP.md)와 [진입점](ENTRY_POINTS.md)을 사용한다. 이전 규칙·판단 수치·변경 이력은 [정비 전 원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/00_Document/conventions/CODE_CONVENTION.md)에 보존되어 있다.
