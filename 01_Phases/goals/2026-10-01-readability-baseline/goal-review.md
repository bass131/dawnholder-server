# Q-1 goal 검토 — R-7 Fable 시범 2회차

- 검토자: `[GameDev 검증자]` (goal 검토자). 지정 모델 `claude-fable-5-1`, 시작 화면 `Fable 5.1 xhigh`(Astra 관측 전달), backend 실제 모델 `unknown`.
- 대상: [goal.md](goal.md) @ `30147f3a2a5eb16d940a0dfd1f331cf74304303c`, base `0239290d6f423dbfe91c42c3fffd0789f56de26f`, branch `bass131/q1-readability-20261001`. 작성 종료 2026-10-01 14:24 UTC.
- Task `task_e636368193d1`, Dispatch `ctx_d64fa7a76a36`.
- 쓰기: 이 파일 하나. goal·CLAUDE.md·제품·테스트·설정은 건드리지 않았다. 빌드·포매터·분석기·테스트·패키지 설치는 실행하지 않았다.
- 이 검토는 **계획의 검토**다. 정식 구현이나 독립 Opus 검증을 대신하지 않으며, 계획이 실행에서 성공한다는 주장도 아니다.

표기: **관측** = 이 세션이 저장소·명령·1차 출처에서 직접 확인한 것. **제안** = 검토자의 보완안이며 승인된 결정이 아니다. **미확정** = 실측이나 메인·사용자 결정이 남은 것. 임계값 완화나 범위 확대는 어떤 항목에서도 제안하지 않는다.

## 요약

goal의 방향(서식 → 분석기·변경 파일 차단 → 모듈 정리·승격, 수치는 도구 기본값, 계획과 실측의 구분)은 메인 요청과 맞는다. 다만 실제 저장소와 도구를 대조하니 **구현 전 필수 보완 12건**(A 발행·병합 전 8건, B 발행 전 4건)이 goal에 빠져 있다. 그 밖에 후속 5건, 참고 5건이 있다. 가장 무거운 것은 다음 셋이다.

1. **F-1** SonarAnalyzer.CSharp의 현재 라이선스에 외부 AI 사용을 제한하는 조항이 있다. 이 프로젝트는 AI 작업자가 진단을 읽고 고치는 구조라 사용자 결정이 필요하다.
2. **F-5** 로컬 표준 실행 공간인 WSL 복제본에는 `.git`이 없고 동기화가 단방향이다. 포매터 적용과 변경 파일 검사 둘 다 지금 설계대로는 로컬에서 성립하지 않는다.
3. **F-3** 완료조건 2의 `git diff -w --exit-code`는 정상 서식 변경(BOM, 줄 이동)에서 실패하고, 문자열 내부 공백 변경은 통과시킨다. 증명 수단을 바꿔야 한다.

| 번호 | 중요도 | 해소 시점 | 제목 |
|---|---|---|---|
| F-1 | 구현 전 필수 보완 | B 발행 전(probe에서 확인) | Sonar 라이선스의 AI 조항 |
| F-2 | 구현 전 필수 보완 | B 발행 전 | 미선택 Sonar 규칙 443개 기본 활성, S134 기본 비활성, 프로퍼티 임계값 3 |
| F-3 | 구현 전 필수 보완 | A 발행 전 | `git diff -w` 증명의 거짓 실패와 거짓 통과 |
| F-4 | 구현 전 필수 보완 | A 발행 전 | A의 레이아웃 속성 값이 미정이고 승인 항목에 없음 |
| F-5 | 구현 전 필수 보완 | A 발행 전 | WSL 복제본과 포매터·변경 파일 검사의 불일치 |
| F-6 | 구현 전 필수 보완 | A 발행 전 | SDK가 세 환경에서 서로 다름(실측) |
| F-7 | 구현 전 필수 보완 | A 발행 전 | 추적 중인 Unity DLL의 갱신 여부 미결정 |
| F-8 | 구현 전 필수 보완 | A 병합 승인 전 | 병합 방식에 따라 ignore-revs가 조용히 무효 |
| F-9 | 구현 전 필수 보완 | A 발행 전 | 테스트 파일 쓰기 소유권의 충돌 |
| F-10 | 구현 전 필수 보완 | 즉시(다음 단계) | probe 단계에 담당·쓰기 경계·승인 지점이 없음 |
| F-11 | 구현 전 필수 보완 | B 발행 전 | CI와 로컬이 분석하는 트리·파일 목록 정의 미결정 |
| F-12 | 구현 전 필수 보완 | B 발행 전 | 양성 fixture의 위치와 기준선 파일의 역할 |
| F-13 | 후속 | B spec | SARIF 경로 정규화, 분석기 실패 진단, 생성물 표식 감시 |
| F-14 | 후속 | B 병합 전 | 변경 파일 전체 차단이 병행 목표에 주는 부담 |
| F-15 | 후속 | A 병합 전 | A 진행 중 main이 움직일 때의 규칙 |
| F-16 | 후속 | C 발행 전 | 동작 보존을 정리 전·후 커밋 양쪽에서 확인하는 절차 |
| F-17 | 후속 | 메인 결정 1과 함께 | 한 goal·다수 PR과 R-8 교체 시점, 후속 설계의 재검토 |
| N-1~N-5 | 참고 | — | 아래 「참고」 절 |

## 구현 전 필수 보완

### F-1. Sonar 라이선스의 AI 조항

- **근거(관측)**:
  - goal.md:56은 "정확한 패키지 버전·출처·라이선스"를 확인한다고만 쓴다. goal.md:135는 라이선스가 "채택 전제와 다르면" 메인에 올린다고 쓰지만 어떤 라이선스가 전제인지는 없다.
  - NuGet의 최신 안정판은 `10.35.0.4138`(2026-09-28)이다. nuspec의 license는 `type="file"`, 값 `licenses\LICENSE.txt`이고 `developmentDependency=true`다.
  - 저장소 master의 `LICENSE.txt`는 "SONAR Source-Available License v1.0"이다. "Non-competitive Purpose" 정의가 제외하는 목적에 (c) "employing, using, or engaging artificial intelligence technology that is not part of the Program to ingest, interpret, analyze, train on, or interact with the data provided by the Program"이 들어 있다.
- **영향**: Q-1의 작업 방식은 Sol·Opus가 분석기 진단을 읽고 코드를 고치는 것이다. 이 사용이 조항 (c)에 해당하는지는 법적 해석이며 검토자가 판단할 수 없다. 해당한다면 B의 S3776·S134 채택 전제가 바뀐다. A는 Sonar와 무관하므로 영향이 없다.
- **보완안(제안)**:
  1. probe에서 고정할 버전의 nupkg 안 `licenses/LICENSE.txt` 원문을 evidence에 보존하고 해당 조항을 그대로 메인에 전달한다.
  2. goal의 메인 결정 요청에 "Sonar 라이선스 수용 여부"를 사용자 결정으로 추가한다. B 발행의 선행 조건으로 둔다.
  3. 수용하지 않을 때의 선택지를 probe 범위에 넣는다. 예: 라이선스 변경 이전의 마지막 판을 고정(해당 판의 SDK 10·C# 문법 호환을 실측), 또는 인지 복잡도·중첩을 다른 수단으로 판정. CA1502로 대체하지 않는다는 goal.md:54의 결정은 유지한다.
- **확인 한계**: 라이선스 본문은 WebFetch 요약 도구를 거쳐 읽었다. 원문 전체를 직접 내려받는 시도는 권한 거부로 실패했다. 10.35 패키지 안의 파일과 master가 같은지는 미확인이다. 조항이 들어간 버전 범위도 미확인이다.

### F-2. 미선택 Sonar 규칙의 기본 활성, S134의 기본 비활성, 프로퍼티 임계값

- **근거(관측)**:
  - goal.md:54는 "선택하지 않은 규칙을 우연히 전면 적용하지 않"는다고 쓰지만 수단이 없다.
  - sonar-dotnet master의 `Sonar_way_profile.json`에는 규칙 키가 443개 있다. `S3776`은 들어 있고 `S134`는 없다. NuGet 패키지는 이 프로필의 규칙을 기본 활성으로 둔다(이 연결은 검토자 기억 기반, 미실측).
  - `CognitiveComplexityBase.cs`의 기본값은 `DefaultThreshold = 15`, `DefaultPropertyThreshold = 3`이다. goal.md:47의 표와 완료조건은 15만 언급한다.
  - 현재 루트 `.editorconfig:10-17`은 StyleCop 카테고리 8개만 끈다. Sonar에 대한 설정은 없다.
- **영향**:
  - 패키지를 `Directory.Build.props`에 넣는 순간 8개 프로젝트 전체에 선택하지 않은 규칙 약 440개가 warning으로 켜진다. 기준선이 선택 규칙과 섞이고 "새 분석기의 우연한 전면 적용 금지"를 어긴다.
  - S134는 명시적으로 켜지 않으면 진단이 0건이다. 이 0은 위반 없음이 아니라 미실행이다.
  - 프로퍼티 접근자는 15가 아니라 3에서 걸린다. "도구 기본값 그대로" 원칙대로면 3이 적용되지만 goal 어디에도 없어 위반 수 예측과 fixture 경계가 달라진다.
- **보완안(제안)**:
  1. 미선택 규칙을 끄는 수단을 probe에서 정하고 goal에 고정한다. 후보는 고정 버전의 규칙 목록에서 생성한 개별 `dotnet_diagnostic.S####.severity = none` 목록이다. 전체 일괄 옵션(`dotnet_analyzer_diagnostic.severity = none`)은 지금 기본으로 켜진 CA·IDE 진단까지 끌 수 있으므로(미실측) 기존 보장이 약해지는지 실측하기 전에는 쓰지 않는다.
  2. 검사 진입점에 **허용 목록 검사**를 넣는다. 구조화 진단에 승인된 규칙 ID와 도입 전부터 나오던 ID 밖의 것이 있으면 실패로 본다. 분석기 버전을 올릴 때 새 규칙이 끼어드는 것도 이 검사로 잡힌다.
  3. S134를 명시 활성으로 적고, 완료조건 1의 fixture에 "프로퍼티 3/4 경계"와 "S134 미활성 시 0건" 음성 확인을 추가한다.
  4. 프로퍼티 임계값 3의 적용을 메인에 사실로 전달한다. 바꾸자는 제안이 아니다.

### F-3. `git diff -w` 증명의 거짓 실패와 거짓 통과

- **근거(관측)**: goal.md:31은 `git diff -w <parent> <commit> --exit-code` 성공을 완료조건으로 둔다. 임시 저장소에서 git 2.54.0.windows.1로 실측한 결과는 다음과 같다(「실측 기록」 M-3).

  | 변경 종류 | `git diff -w --exit-code` |
  |---|---|
  | 탭→공백, 줄 끝 공백 제거, 파일 끝 개행 추가 | 0 (통과) |
  | BOM 제거 | 1 (실패) |
  | 여는 중괄호를 다음 줄로 이동 | 1 (실패) |
  | 빈 줄 제거 | 1 (실패) |
  | 문자열 리터럴 내부 공백 변경 | 0 (통과) |

  현재 범위 222개 파일 중 BOM 보유는 3개다: `02_Server/Network/JobQueue.cs`, `02_Server/Network/RecvBuffer.cs`, `99_Tools/PacketGenerator/Program.cs`. 파일 끝 개행 없음은 2개다: `02_Server/Network/Connector.cs`, `02_Server/Network/SendBuffer.cs`. 줄 끝 `{`로 끝나는 제어문은 정규식 표본에서 0건이었다.
- **영향**: `charset`을 설정해 BOM을 통일하면 완료조건 2가 실패한다. 통일하지 않으면 BOM 혼재가 남는다. 반대로 `-w`가 0이어도 문자열 값이 바뀌었을 수 있다. goal.md:77은 뒤쪽 위험을 이미 인정하지만 완료조건 2는 `-w` 성공을 그대로 요구해 서로 어긋난다.
- **보완안(제안)**:
  1. 완료조건 2의 주 증명을 **토큰 동등성**으로 바꾼다. 커밋 전·후 각 파일에서 공백·개행을 뺀 토큰열이 같고, 문자열·문자 리터럴 값과 주석 본문이 같음을 보인다. 구현 수단은 spec에서 정한다.
  2. `git diff -w`는 보조 증거로 내린다. BOM·줄 이동·빈 줄 변화처럼 실패가 예상되는 종류를 goal에 적고, 그 밖의 실패는 중단 후 보고로 둔다.
  3. BOM 처리(F-4의 `charset`)를 메인 결정으로 올린다.

### F-4. A의 레이아웃 속성 값이 미정이고 승인 항목에 없음

- **근거(관측)**:
  - 루트 `.editorconfig:1-49`에는 들여쓰기·줄바꿈·문자셋·파일 끝 개행 속성이 하나도 없다. goal.md:46과 goal.md:85는 "레이아웃 설정"이라고만 쓰고, 메인 결정 요청(goal.md:133-135)에도 값이 없다.
  - `.gitattributes:61`이 `*.cs text eol=lf`를 강제한다. 범위 222개 파일은 index·작업 트리 모두 LF다.
  - `99_Tools/PacketGenerator/PacketFormat.cs:32,46,236`에는 템플릿 문자열 **안**에 줄 끝 공백이 있다. 이 생성기의 출력은 `02_Server/GameServer.Tests/Tools/PacketGeneratorExitTests.cs:26-28`에서 SHA-256으로 고정돼 있다.
- **영향**: 속성 값이 곧 일괄 변경의 크기와 내용이다. 승인 없이 Sol이 정하면 사용자 결정 영역을 구현자가 채운다. `end_of_line`을 `crlf`로 두면 모든 환경에서 검사가 실패한다. `trim_trailing_whitespace = true`를 넣으면 포매터는 문자열을 건드리지 않더라도(미실측) 편집기가 저장할 때 템플릿 문자열의 공백을 지워 생성 바이트가 달라질 수 있다.
- **보완안(제안)**:
  1. goal에 A의 속성 표를 넣고 메인 승인을 받는다. 열은 속성, 값, 근거(도구 기본값 또는 현재 코드 실측), 예상 변경 파일 수다. 예상 변경 수는 probe의 `--verify-no-changes --report` 결과로 채운다.
  2. `end_of_line`은 `.gitattributes`와 같은 `lf`만 허용한다고 적는다.
  3. `PacketFormat.cs`에는 줄 끝 공백 제거를 적용하지 않는 국소 설정을 두거나 해당 속성을 도입하지 않는다. `PacketGeneratorExitTests.cs:28`의 digest 테스트를 A와 C3의 고정 판정 근거로 goal에 명시한다.

### F-5. WSL 복제본과 포매터·변경 파일 검사의 불일치

- **근거(관측)**:
  - goal.md:116은 실행에 "WSL 복제 공간과 lock을 우선 사용"한다고 쓴다. `DEVELOPMENT.md:10`은 이 머신에서 서버·테스트 실행이 Windows 정책으로 막혀 WSL을 쓴다고 쓴다.
  - `99_Tools/sync-wsl.sh:48-56`은 `02_Server`, `04_ClientNet`, `98_Shared`, `99_Tools` 네 트리만 `rsync -a --delete`로 **단방향** 복사한다. `sync-wsl.sh:57-60`은 루트에서 `Dawnholder.slnx`, `global.json`, `Directory.Build.props`, `.editorconfig` 네 파일만 복사한다. `.git`, `.github/`, 그 밖의 루트 파일은 복제본에 없다.
  - goal.md:21의 관련 파일 목록에 `sync-wsl.sh`가 없다.
- **영향**:
  1. 포매터를 복제본에서 실행하면 결과가 Windows 원본에 반영되지 않고 다음 동기화에서 사라진다. A의 핵심 작업을 어디서 실행하고 어떻게 원본에 쓰는지가 비어 있다.
  2. 변경 파일 목록은 Git 이력이 필요한데 복제본에는 `.git`이 없다. goal.md:62의 "하나의 저장소 검사 진입점"이 로컬에서 그대로는 동작하지 않는다.
  3. B가 루트에 추가할 파일(검사 도구, 분석기 매개변수 파일, 패키지 버전 파일 등)은 동기화 목록에 없어 복제본 결과가 CI와 **조용히** 달라진다. 완료조건 3의 "로컬·CI 같은 결과"를 깨는 직접 원인이다.
- **보완안(제안)**:
  1. 로컬 검사의 2단 구조를 goal에 고정한다. 원본 쪽 Git으로 base·head·파일 목록을 만들고, 복제본에서 분석하며, 결과 경로를 저장소 상대 경로로 정규화해 대조한다.
  2. 포매터 적용 경로를 하나로 정한다. Windows 네이티브 `dotnet format`(SDK 자체 실행이 정책에 막히는지 probe) 또는 복제본 실행 뒤 명시적 역반영 절차 중 하나다. 역반영이면 복제본→원본 복사의 소유자와 전후 대조를 spec에 쓴다.
  3. `sync-wsl.sh`의 루트 입력 목록을 A·B의 수정 대상과 관련 파일 목록에 넣는다. 검사 전 조건으로 "원본과 복제본의 설정 파일 hash 일치"를 둔다.

### F-6. SDK가 세 환경에서 서로 다름

- **근거(관측)**: `global.json:3-4`는 `10.0.203`, `rollForward: latestFeature`다. 이 머신의 Windows는 `dotnet --version`이 `10.0.301`이다(설치: 8.0.425, 10.0.204, 10.0.301). WSL의 `~/.dotnet`에는 `10.0.300`만 있다. CI는 `dotnet-tests.yml:19`의 `10.0.x`다. CI가 실제로 고르는 버전은 미실측이다.
- **영향**: 포매터와 IDE 분석기는 SDK에 묶여 있다. 완료조건 2의 "같은 범위와 버전"은 지금 상태로는 세 환경 어디에서도 성립하지 않는다. goal.md:56은 "실측해 일치시킨다"고만 쓰고 수단과 승인 대상(`global.json` 변경)을 적지 않았다. `98_Shared/Shared.csproj:14-22`의 주석은 SDK 버전이 달라지면 결정적 빌드의 hash가 깨진다고 적고 있어 F-7과 엮인다.
- **보완안(제안)**:
  1. 완료조건 1에 "검사 진입점은 실제 `dotnet --version`을 기록하고 고정 값과 다르면 실패한다"를 추가한다.
  2. 고정 방식(`global.json`의 정확한 버전과 `rollForward` 축소, CI의 `global-json-file` 사용)을 메인 결정으로 올리고 `global.json`을 관련 파일 목록에 넣는다. WSL·Windows에 SDK를 새로 설치해야 하면 그것은 머신 변경이므로 별도 승인 항목이다.

### F-7. 추적 중인 Unity DLL의 갱신 여부 미결정

- **근거(관측)**:
  - `03_Client/Assets/Plugins/Shared/Shared.dll`과 `03_Client/Assets/Plugins/ClientNet/Dawnholder.Client.Net.dll`은 Git 추적 파일이다.
  - `98_Shared/Shared.csproj:11-12`와 `04_ClientNet/Dawnholder.Client.Net.csproj:17-18`은 소스를 DLL에 통째로 넣는다. goal.md:113도 공백 정리가 DLL hash를 바꿀 수 있다고 인정한다.
  - `Shared.dll`의 마지막 커밋 `0b3b722`는 `98_Shared` 소스의 마지막 변경 커밋과 같다. 즉 지금까지는 소스와 DLL을 함께 커밋해 왔다.
  - goal.md:113-117은 DLL을 **보호**하는 절차만 있고, 소스가 바뀐 뒤 추적 DLL을 갱신할지는 없다.
- **영향**: A·C1·C2에서 Shared·ClientNet 소스가 바뀌면 추적 DLL이 소스와 어긋난다. 갱신하지 않으면 다음 Windows 빌드에서 사용자의 작업 트리가 dirty가 된다. 갱신하면 바이너리가 PR에 들어가고, AGENTS의 "공유 DLL 변경은 서버와 Unity 사용처 양쪽 검증" 의무가 생긴다. DLL은 Windows 빌드에서만 갱신되고(`DEVELOPMENT.md:36`) hash는 SDK 버전에 따라 달라진다(F-6).
- **보완안(제안)**:
  1. probe에서 포매터가 `98_Shared`·`04_ClientNet`의 파일을 실제로 바꾸는지 먼저 확인한다. 검토자의 표본에서는 두 디렉터리의 수기 소스에 탭·BOM·파일 끝 개행 이상치가 없었다. 다만 이 표본은 포매터가 고치는 전체 항목을 대표하지 않는다.
  2. PR마다 "DLL 갱신을 별도 커밋으로 포함" 또는 "갱신하지 않고 불일치를 기록" 중 하나를 메인 결정으로 올린다.
  3. 갱신한다면 빌드 소유자, 사용 SDK, 전후 hash, Unity 미실행 범위를 spec에 고정하고 공백 전용 커밋과 분리한다.

### F-8. 병합 방식에 따라 ignore-revs가 조용히 무효

- **근거(관측)**: `origin/main`의 최근 이력은 #154~#158이 merge commit이고 #135~#139는 squash다(`git log`의 `(#139)` 접미). 두 방식이 모두 쓰였다. 존재하지 않는 SHA를 넣은 ignore-revs 파일로 `git blame`을 돌리면 오류 없이 exit 0이다(「실측 기록」 M-3). GitHub 저장소의 허용 병합 방식 설정은 미확인이다.
- **영향**: A가 squash나 rebase로 병합되면 공백 전용 커밋의 SHA가 main에 없다. `.git-blame-ignore-revs`는 오류 없이 아무 효과도 내지 않는다. 공백 커밋이 설정·문서 커밋과 합쳐져 완료조건 2의 "별도 커밋"도 main에서 사라진다. 병합은 되돌리기 어렵다.
- **보완안(제안)**:
  1. A의 병합 승인 요청에 "merge commit 방식으로 병합"을 전제 조건으로 적는다.
  2. 완료조건 2에 병합 후 확인을 추가한다. `git merge-base --is-ancestor <공백 커밋 SHA> origin/main` 성공과, 표본 파일의 `git blame --ignore-revs-file` 결과가 공백 커밋을 건너뛰는지다.

### F-9. 테스트 파일 쓰기 소유권의 충돌

- **근거(관측)**: AGENTS는 테스트 작성을 신규 Opus 검증자에게 맡기고 구현자와 검증자를 분리한다. goal.md:93은 "Opus가 테스트 파일을 단독 소유"한다고 쓴다. 그런데 goal.md:19는 `GameServer.Tests`를 범위에 넣는다. 이 프로젝트의 추적 C# 파일은 87개다. A의 포매터 일괄 적용과 C5의 "테스트 코드 잔여 정리"(goal.md:91)는 테스트 파일을 고친다. 검사 진입점의 fixture와 자체 테스트(goal.md:71)의 소유도 적혀 있지 않다.
- **영향**:
  - A에서 포매터를 돌리는 작업자가 테스트 87개를 쓰면 "Opus 단독 소유"와 어긋난다.
  - C5에서 Opus가 테스트를 리팩터링하면 그 Opus는 구현자가 된다. 같은 세션이 판정까지 하면 구현·검증 분리가 깨진다.
  - fixture 소유가 비어 있으면 Sol과 Opus의 쓰기 경계가 spec마다 흔들린다.
- **보완안(제안)**: 소유권 절에 다음 셋을 명시한다.
  1. A의 포매터 일괄 적용은 기계 출력이므로 실행 소유자 한 명이 테스트 파일까지 쓴다는 **한정 예외**. 메인 승인 대상이다.
  2. 테스트 코드 정리는 정리 담당 세션과 검증 세션을 서로 다른 신규 세션으로 나눈다. 정리 전·후의 테스트 목록, 통과 결과, assertion 의미를 대조한다.
  3. 검사 진입점의 fixture·자체 테스트를 제품(Sol)과 독립 테스트(Opus) 중 어디에 둘지 지정한다.

### F-10. probe 단계에 담당·쓰기 경계·승인 지점이 없음

- **근거(관측)**: 완료조건 1(goal.md:30)은 "실제 설치물로 확인"을 요구한다. goal.md:135는 "Fable 검토 후 실제 도구 probe 결과로 세부 구현 계약을 확정"한다고 쓴다. 그러나 PR 표(goal.md:83-91)와 소유권 절(goal.md:123-127)에 probe를 누가, 어디서, 무엇을 쓰며 하는지가 없다.
- **영향**: F-1~F-7이 모두 probe 결과에 달려 있다. 담당이 없으면 Astra가 직접 실행하거나(역할 위반 소지) Sol이 A 구현 중에 섞어서 한다. 뒤의 경우 승인 전에 설정 값이 정해진다. 패키지 복원은 사용자 전역 NuGet 캐시에 쓰기를 남기므로 부작용 기록 대상이기도 하다.
- **보완안(제안)**: PR 표 앞에 **P0 probe** 행을 추가한다.
  - 담당: 신규 Sol 한 세션. A용(포매터·SDK)과 B용(Sonar·StyleCop·SARIF)으로 나눌 수 있다.
  - 쓰기: evidence 폴더와 저장소 밖 임시 프로젝트만. 추적 파일은 바꾸지 않는다.
  - 산출물: 포매터의 slnx 동작과 변경 예정 파일 목록, 생성물 판정, SDK 세 환경 버전, Sonar 라이선스 원문·기본 활성 규칙 목록·임계값 경계, SA1612의 활성 조건, SARIF 경로 표기.
  - 승인: probe 보고 → goal 보완 → 메인 승인 → A spec 발행.

### F-11. CI와 로컬이 분석하는 트리·파일 목록 정의 미결정

- **근거(관측)**: 메인 요청 1번은 "바뀐 파일만 error"를 로컬과 CI에서 같은 결과로 재현하라고 요구한다(main-request.md:65). goal.md:64는 합성 merge commit과 head의 차이를 "기록"한다고만 쓴다. `dotnet-tests.yml:15`는 `actions/checkout@v4` 기본값이다. 기본값은 얕은 clone이고 PR에서는 합성 merge commit을 받는다(기억 기반, 미실측). `dotnet-tests.yml:6-9`의 push 트리거는 `main`과 `feature/**`인데 현재 브랜치 이름은 `bass131/...`이다.
- **영향**: main이 앞서 있으면 CI는 merge 결과 트리를, 로컬은 head 트리를 분석한다. 같은 base·head를 넣어도 진단이 달라질 수 있다. rename 감지는 유사도 휴리스틱과 Git 설정에 의존하므로 목록 자체도 환경에 따라 달라질 수 있다.
- **보완안(제안)**:
  1. 검사 job은 PR head SHA를 명시적으로 checkout하고 base를 명시적으로 fetch한다고 정한다. merge 결과 트리의 build·test는 별도 job으로 유지할 수 있다.
  2. 파일 목록을 한 줄 명령으로 정의한다. 예: `git diff --no-renames --diff-filter=d -z --name-only <merge-base> <head> -- '*.cs'`. rename은 새 경로의 추가로 나타나므로 goal.md:65의 "rename은 새 경로 검사, 삭제 제외"와 결과가 같고 휴리스틱 의존이 없어진다.
  3. push 이벤트의 base(`before` SHA와 0으로만 된 SHA의 처리)와 `feature/**` 트리거 정리를 B의 범위에 적는다.

### F-12. 양성 fixture의 위치와 기준선 파일의 역할

- **근거(관측)**: 완료조건 1과 goal.md:71은 규칙별 양성 fixture를 요구한다. 양성 fixture는 규칙을 위반하는 코드다. 완료조건 3은 새 파일의 위반을 차단하고, 완료조건 5는 승격 모듈을 전체 error로 만든다. goal.md:69는 "기준선 JSON의 숫자를 낮추"는 우회를 금지하지만 기준선이 차단 판정의 입력인지 보고용인지 적지 않았다.
- **영향**: 양성 fixture가 `Dawnholder.slnx`의 Compile 입력에 들어가면 자기 자신이 변경 파일 차단에 걸리고, 승격 뒤에는 빌드를 깬다. 이를 피하려고 제외나 suppression을 넣으면 goal.md:95의 "제외로 0을 만들지 않는다"와 충돌한다. 기준선이 판정 입력이면 줄 위치가 든 JSON은 수정 때마다 낡고 병행 PR끼리 충돌한다.
- **보완안(제안)**:
  1. fixture는 slnx 밖의 별도 입력으로 두고 검사 진입점의 자체 검증만 이를 분석한다고 정한다. "전체 파일 수/검사 파일 수" 보고에서 별도 행으로 둔다.
  2. 기준선의 역할을 한 문장으로 고정한다. 제안은 "기준선은 보고용 스냅샷이고, 차단은 변경 파일 검사와 승격 모듈의 severity로만 한다"다. 규칙×모듈 수치는 저장소에, 원시 진단은 evidence에 둔다.

## 후속

### F-13. SARIF 경로 정규화, 분석기 실패 진단, 생성물 표식 감시 (B spec)

- **근거(관측)**: `98_Shared/Shared.csproj:24`와 `04_ClientNet/Dawnholder.Client.Net.csproj:24`는 각자의 프로젝트 폴더를 같은 `/_/`로 매핑한다. `98_Shared/Protocol/Generated/GenPackets.cs:1`은 `// <auto-generated />`로 시작한다. `99_Tools/PacketGenerator/PacketFormat.cs:98`에도 같은 문구가 있지만 문자열 안이고 파일은 `using`으로 시작한다. 범위 안의 탭 들여쓰기 792줄과 줄 끝 공백 191줄은 전부 `GenPackets.cs`에 있다.
- **영향(미확정)**: PathMap이 구조화 진단의 경로에도 적용되는지는 미실측이다. 적용된다면 두 프로젝트의 파일이 같은 접두 경로로 나와 저장소 경로 복원이 프로젝트 정보 없이는 불가능하다. 생성물 판정은 파일 첫머리 표식으로 도구가 자동으로 한다(기억 기반). 따라서 다른 파일에 같은 표식을 붙이면 포매터와 분석기에서 조용히 빠진다. goal.md:24의 "생성물로 바꿔 위반을 숨기지 않는다"를 감시할 수단이 없다. StyleCop `1.2.0-beta.556`이 최신 문법에서 예외를 내면 기본 warning인 AD0001로만 나온다(기억 기반).
- **보완안(제안)**: 진단을 프로젝트별 파일로 받고 (프로젝트, 경로) 쌍으로 저장소 상대 경로를 복원한다. 실패 조건에 AD0001과 분석기 생성 실패 진단을 명시한다. 승인된 생성물 목록 밖의 파일에 생성물 표식이나 `generated_code = true`가 생기면 검사가 실패하게 한다. 생성물 판정 근거로 `GenPackets.cs:1`을 기준선에 적는다.

### F-14. 변경 파일 전체 차단이 병행 목표에 주는 부담 (B 병합 전)

- **근거(관측)**: 완료조건 3은 변경 파일의 **파일 전체**를 차단한다. 이는 사용자 결정에서 나온 것이다(main-request.md:11). `CODE_CONVENTION.md:3`은 "무관한 파일을 일괄 변경하지 않는다"고 쓰고 AGENTS는 무관한 정리로 범위를 넓히지 말라고 쓴다.
- **영향**: B가 병합된 뒤 C가 끝나기 전에는, 다른 목표가 legacy 파일을 한 줄 고쳐도 그 파일의 복잡도·순서·주석 위반을 전부 같은 PR에서 해소해야 한다. 기능 변경과 동작 보존 리팩터링이 한 PR에 섞인다. 급하면 국소 suppression을 "나중에 정리" 용도로 쓰려는 압력이 생긴다.
- **보완안(제안)**: B의 기준선에서 위반 보유 파일 목록을 뽑아 예정된 다른 목표가 건드릴 파일과 교차하고 C의 순서를 그에 맞춘다. 무관한 PR이 legacy 파일을 건드릴 때의 규칙(같은 PR의 별도 커밋으로 정리 또는 선행 정리 PR)을 goal과 코드 기준에 적는다. suppression의 미루기 용도 사용을 허용할지는 메인 결정으로 올린다.

### F-15. A 진행 중 main이 움직일 때의 규칙 (A 병합 전)

- **근거(관측)**: 공백 커밋은 한 시점의 트리에서 만든다. 현재 진행 중인 다른 작업 공간은 `management-active`의 `feat/management-shared-read-mcp`이며 `origin/main` 대비 C# 변경은 없다(goal.md 1개 파일).
- **영향**: A가 열려 있는 동안 main에 C# 변경이 들어오면 병합 결과에 서식 안 된 코드가 생긴다. 공백 커밋을 다시 만들면 SHA가 바뀌어 ignore-revs도 고쳐야 한다.
- **보완안(제안)**: "A 기간 중 C# 병합 동결" 또는 "main 변동 시 공백 커밋 재생성, ignore-revs SHA 갱신, 재검증" 중 하나를 goal에 적는다. 병합 승인 요청의 전제로 최신 main 기준 포매터 재검사 0건을 둔다.

### F-16. 동작 보존을 정리 전·후 양쪽에서 확인하는 절차 (C 발행 전)

- **근거(관측)**: goal.md:99는 검증자가 구현자의 쓰기 종료 **뒤에** 독립 테스트를 쓴다고 정한다. 정리 전 코드에서 같은 테스트가 통과하는지 확인하는 절차는 없다. goal.md:25는 필드·static 초기화 순서 보존을 요구한다. 범위 안의 `static readonly` 선언은 60개 파일에 117줄이다.
- **영향**: 정리 뒤에만 쓰고 돌린 테스트는 리팩터링된 동작을 기준으로 삼는다. 정리 전과 달라진 동작을 "통과"로 고정할 수 있다.
- **보완안(제안)**: C 배치의 검증 계약에 "검증자 테스트를 배치의 parent 커밋과 head 양쪽에서 실행하고 결과가 같다"를 넣는다. 구현 보고에 필드 선언 순서가 바뀐 위치 목록과 초기화식이 다른 필드를 참조하는 경우의 표시를 요구한다. Shared·ClientNet 배치에는 공개 API 목록의 전후 비교를 추가한다.

### F-17. 한 goal·다수 PR과 R-8 교체 시점, 후속 설계의 재검토 (메인 결정 1과 함께)

- **근거(관측)**: goal.md:81은 A~C5를 한 goal의 후속 PR로 두자고 제안한다. `ORCA.md` R-8은 "PR 병합과 goal 결과 기록이 모두 끝나면" Astra를 교체하고 "목표 중간에는 수동으로 비우지 않는다"고 정한다. R-7의 검토는 goal commit 직후 한 번이다. 검사 진입점의 실제 설계와 배치별 파일 목록은 probe와 기준선 뒤에 정해진다(goal.md:93,135).
- **영향**: 한 goal로 두면 PR 7개 이상을 한 Astra 세션이 이어 간다. R-8을 만든 배경(사용률)과 맞지 않는다. 가장 영향이 큰 B의 설계는 이번 검토 뒤에 확정되므로 goal 검토를 거치지 않는다.
- **보완안(제안, 메인 전달)**: 메인 결정 1의 선택지에 운영 영향을 붙인다. 한 goal을 유지한다면 "각 PR 병합을 Astra 교체 시점으로 본다"와 goal.md의 PR별 상태 절 구조를 함께 정한다. B spec을 메인이 승인할 때 추가 검토가 필요한지도 메인이 정한다. R-7은 2~3개 한정 시범이므로 회차를 늘릴지는 검토자가 제안하지 않는다.

## 참고

- **N-1. `dotnet format` 범위의 해석 변경.** main-request.md:64는 `dotnet format --verify-no-changes`를 요구하고 goal.md:46은 `dotnet format whitespace`로 좁힌다. 하위 명령 없이 쓰면 코드 스타일·분석기 수정까지 검사하므로 좁힌 것은 타당해 보인다. 다만 요청 문구와 다르므로 메인에 명시적으로 알릴 것을 제안한다.
- **N-2. SA1612의 전제.** `.editorconfig:20`은 SA0001을 끈다. 어떤 csproj·props에도 `GenerateDocumentationFile`이 없다. StyleCop 문서는 이 속성이 없으면 문서 주석 분석이 꺼진다고 쓴다. 범위 안의 `<param>` 태그는 10개 파일 23줄뿐이다. 이 속성을 켜면 컴파일러의 문서 경고가 새로 뜬다(기억 기반). goal.md:58의 probe 항목에 "새로 뜨는 컴파일러 경고의 ID와 건수, 끌 범위"를 추가할 것을 제안한다.
- **N-3. IDE0055와 포매터의 이중 판정.** 빌드의 IDE0055와 CLI 포매터가 다르게 판정할 때 어느 쪽이 기준인지 goal에 없다. 포매터 CLI를 기준으로 두고 IDE0055는 보조로 적을 것을 제안한다.
- **N-4. 로컬 blame 설정.** 이 저장소에 `blame.ignoreRevsFile`은 설정돼 있지 않다. 운영 설명에는 저장소 로컬 설정 안내만 넣고 전역 설정은 바꾸지 않는다.
- **N-5. 메인 요청의 행 번호.** main-request.md:51·56은 줄 수·주석량 문장을 `CODE_CONVENTION.md` 73행이라고 쓰지만 실제는 75행이다. goal에는 영향이 없다.

## 요구사항 대응

| 메인 요청 「goal에 담을 요구」 | goal 반영 | 판정 |
|---|---|---|
| 1. 결정론 계층, 로컬·CI 동일 재현 | goal.md:38-71 | 반영. 재현 수단은 F-5·F-6·F-11 보완 필요 |
| 2. 서식 일괄 정리, 공백 전용 증명, ignore-revs, DLL 확인 | goal.md:31,73-77,113-119 | 반영. F-3·F-4·F-7·F-8 보완 필요 |
| 3. 기준선·배치·승격 조건·PR 분할 제안 | goal.md:33-34,79-95 | 반영. F-12·F-14·F-16 보완 필요 |
| 4. 체크포인트 계층, V3 미수신 명시 | goal.md:35,97-109 | 반영, 지적 없음 |
| 5. 범위 밖 | goal.md:23 | 반영, 지적 없음 |
| 6. CLAUDE.md는 메인 소유 | goal.md:14,123 | 반영, 지적 없음 |
| 7. 진행 순서 | goal.md:129 | 반영. probe 단계는 F-10 |
| 8. 이전 목표 기록과 보존 대상 | goal.md:11-12,118 | 반영, 지적 없음 |

## 검토했고 지적 없음

- **범위 수치**: `Dawnholder.slnx:3-16`의 프로젝트 8개는 추적 중인 csproj 8개와 일치한다. 범위 안 추적 C#은 222개다(GameServer.Tests 87, GameServer 67, headless-bot 22, 98_Shared/GameData 14, BgmComposer 13, Network 7, 04_ClientNet 7, 98_Shared/Protocol 3, PacketGenerator 2). 링크된 Compile 항목이나 다중 TFM은 csproj에 없다.
- **현황 서술**: error 적용 8개 파일(`.editorconfig:35,46`), CA1502·CA1506의 3개 파일 한정(`.editorconfig:35,42-43`), Tests·Tools 하위 완화(`02_Server/GameServer.Tests/.editorconfig:5-7`, `99_Tools/.editorconfig:5-7`), StyleCop 버전(`Directory.Build.props:7`), DLL 복사 대상 2개가 goal의 서술과 맞다.
- **보존 대상**: skip-worktree 3개 파일과 stash 2개가 goal.md:118 및 `preservation-before.json`과 맞다.
- **멤버 재정렬의 바이너리·직렬화 위험**: 범위 안 222개 파일에서 `StructLayout`, `MemoryMarshal.`, `Unsafe.`, `Marshal.`, `fixed (`, `unsafe`, `JsonSerializer.`를 정규식으로 찾았고 0건이다. `sizeof`는 `GenPackets.cs`(326줄)와 그 템플릿인 `PacketFormat.cs`(20줄)에만 있고 대상은 기본형이다. `stackalloc`은 `99_Tools/BgmComposer/Synth/MidiWriter.cs:88`의 바이트 버퍼 1건이다. 리플렉션의 멤버 열거는 테스트의 순서 무관 단언뿐이다(`PlayerStatsWiringTests.cs:44,49`). 이 정규식 범위에서는 선언 순서에 의존하는 메모리 배치·직렬화 근거를 찾지 못했다. 초기화 순서는 F-16에서 다뤘다.
- **계획과 실측의 구분**: goal.md:9,42,137,143은 미실행·미확인을 명시한다. 계획을 성공으로 쓴 문장은 찾지 못했다.
- **승인·Git 경계**: PR별 사용자 병합 승인, stacked branch 금지, Astra 단독 commit·push, 작업자 1작업 후 종료가 AGENTS와 맞다.
- **체크포인트 계층**: 지적 형식, 가상 예시 표기, "검토했고 지적 없음"과 "검토 안 함"의 구분, 취향만으로 차단하지 않는다는 문장이 메인 요청의 운영 원칙과 맞다.

## 미검토·미실측

- 포매터·빌드·분석기·테스트를 실행하지 않았다. 포매터의 slnx 지원, 실제 변경 파일 수, `charset`·파일 끝 개행 처리, 복원 부작용은 모두 미실측이다.
- Sonar·StyleCop의 실제 진단, `.editorconfig` 일괄 옵션의 동작, 구조화 진단의 경로 표기, 실제 위반 수는 미실측이다.
- CI runner가 고르는 SDK, GitHub 저장소의 허용 병합 방식과 ruleset은 확인하지 않았다.
- 범위 안 222개 소스의 본문은 읽지 않았다. 위의 수치는 Git 객체에 대한 정규식·바이트 표본이다. "K&R 중괄호 0건"은 정규식 한정이다.
- Sonar 라이선스 원문 전체, 10.35 패키지 안의 라이선스 파일, 조항의 적용 버전 범위는 직접 열람하지 못했다.
- Unity 쪽 소비 계약, `.claude/settings.local.json` 두 파일의 hash, M-1 V3 결과는 검토하지 않았다.
- `03_Client`의 C# 238개는 범위 밖이라 보지 않았다.

## 실측 기록

모두 읽기 전용이다. M-3만 세션 scratchpad의 임시 Git 저장소에서 실행했다.

- **M-1 저장소 상태**: `git status --short` 출력 없음, `HEAD` = `30147f3a…`. `git show --stat 30147f3`은 `CURRENT.md`, `goal.md`, `CLAUDE.md` 3개 파일이다.
- **M-2 서식 현황**(`git ls-files --eol`, `git grep -P`, `git cat-file blob`의 앞·끝 바이트): 222개 전부 `i/lf w/lf attr/text`. BOM 3개, 파일 끝 개행 없음 2개(F-3). 탭 들여쓰기 792줄은 `GenPackets.cs` 한 파일. 줄 끝 공백은 `GenPackets.cs` 191줄과 `PacketFormat.cs` 3줄.
- **M-3 Git 동작**(git 2.54.0.windows.1, `core.autocrlf=false` 임시 저장소): F-3의 표. 존재하지 않는 SHA가 든 파일로 `git blame --ignore-revs-file` 실행 시 정상 출력과 exit 0.
- **M-4 SDK**: Windows `dotnet --version` = 10.0.301, `--list-sdks` = 8.0.425 / 10.0.204 / 10.0.301. WSL Ubuntu `~/.dotnet/dotnet --list-sdks` = 10.0.300. WSL의 PATH에는 `dotnet`이 없고 `sync-wsl.sh:63-64`가 `~/.dotnet/dotnet`으로 대체한다.
- **M-5 이력**: `git log origin/main`의 merge·squash 혼재(F-8). `Shared.dll`과 `98_Shared` 소스의 마지막 커밋이 모두 `0b3b722`(F-7).
- **M-6 작업 공간**: worktree 3개(이 checkout, archive 보관, `management-active`). `git diff --name-only origin/main...feat/management-shared-read-mcp -- '*.cs'` 출력 없음.

## 외부 출처와 확인 한계

| 출처 | 확인한 것 | 한계 |
|---|---|---|
| [NuGet SonarAnalyzer.CSharp](https://www.nuget.org/packages/SonarAnalyzer.CSharp), [10.35.0.4138 nuspec](https://api.nuget.org/v3-flatcontainer/sonaranalyzer.csharp/10.35.0.4138/sonaranalyzer.csharp.nuspec) | 최신 안정판과 날짜, license `type=file`, `developmentDependency=true`, 의존성 없음 | 패키지 안 라이선스 파일은 미열람 |
| [sonar-dotnet LICENSE.txt](https://github.com/SonarSource/sonar-dotnet/blob/master/LICENSE.txt) | SSALv1, "Non-competitive Purpose"의 (c) 조항 | 요약 도구 경유. master 기준이며 고정할 버전과의 일치는 미확인. 법적 해석은 하지 않음 |
| [Sonar_way_profile.json](https://github.com/SonarSource/sonar-dotnet/blob/master/analyzers/rspec/cs/Sonar_way_profile.json) | 규칙 키 443개, S3776 포함, S134 없음 | master 기준. 프로필과 NuGet 기본 활성의 연결은 기억 기반 |
| [CognitiveComplexityBase.cs](https://github.com/SonarSource/sonar-dotnet/blob/master/analyzers/src/SonarAnalyzer.Core/Rules/CognitiveComplexityBase.cs) | `DefaultThreshold = 15`, `DefaultPropertyThreshold = 3` | master 기준 |
| [S134 설명](https://github.com/SonarSource/sonar-dotnet/blob/master/analyzers/rspec/cs/S134.html) | 대상 구문(if·switch·for·foreach·while·do·try), 기본 3 | 요약 도구 경유. 소스의 상수는 미열람 |
| [StyleCop SA1612](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA1612.md), [SA0001](https://github.com/DotNetAnalyzers/StyleCopAnalyzers/blob/master/documentation/SA0001.md) | SA1612는 문서의 매개변수가 실제와 다르거나 순서가 다를 때. SA0001은 `GenerateDocumentationFile` 없이 문서 분석이 꺼진 상태 | master 문서. `1.2.0-beta.556`의 실제 동작은 미실측 |
| [Microsoft dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format) | `whitespace` 하위 명령, `--verify-no-changes`의 비정상 종료, `--include-generated`, `--no-restore`, 복원·컴파일 가능성 경고 | 인자 설명에 slnx 언급 없음. 실제 지원은 미실측 |
| [git-blame 문서](https://git-scm.com/docs/git-blame) | `--ignore-revs-file`, `blame.ignoreRevsFile` | 없는 SHA의 처리는 문서에 없어 M-3으로 실측 |

## 메인 전달 사항

검토자는 아래를 결정하지 않는다. 메인 원문 확인 → Astra 보완 → 메인 승인 순서에서 판단할 항목이다.

1. **사용자 결정이 필요해 보이는 것**: F-1(Sonar 라이선스 수용 여부), F-7(추적 DLL 갱신 방침), F-8(A의 병합 방식), F-4의 BOM·레이아웃 값.
2. **메인 결정이 필요해 보이는 것**: F-6(SDK 고정 방식과 `global.json` 변경), F-9(포매터의 테스트 파일 쓰기 예외), F-10(probe 단계 신설), F-14(병행 목표 규칙), F-17(한 goal 유지 시 운영).
3. **Astra가 goal에 반영하면 되는 것**: F-2, F-3, F-5, F-11, F-12, F-13, F-15, F-16과 참고 N-1~N-4.
4. 검토자가 찾지 못한 문제가 없다는 뜻은 아니다. 「미검토·미실측」 범위는 probe와 독립 검증에서 다시 확인해야 한다.
