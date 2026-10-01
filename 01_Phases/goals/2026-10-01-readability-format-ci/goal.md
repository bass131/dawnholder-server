# Q-1A — P0-A 실측과 C# 서식·CI 정착

## 목표와 현재 단계

[Q-1 로드맵](../2026-10-01-readability-baseline/goal.md)의 첫 실행 목표다. 서버 솔루션의 C# 서식을 하나의 고정된 포매터 결과로 맞추고, 동작 보존 증명과 로컬·CI의 재현 가능한 검사로 유지한다. 먼저 P0-A에서 실행 환경·포매터·부작용을 임시 공간에서 실측하고, 그 결과로 이 goal을 보완한 뒤 실제 A 구현을 발행한다.

- 상태: **Q-1A goal 작성. P0-A Sol 발행은 메인 승인 대기.** 실측·서식 적용·검사 구현·독립 Opus 검증·PR 생성은 미실행이다.
- 근거: 메인 `msg_b7f867c99e0f`(2026-10-01 14:28:26 UTC), [결정 사본](../../../.backups/verification/2026-10-01-readability-format-ci/main-split-decision.json). 사용자 결정은 메인 경유이며 사용자 직접 입력으로 격상하지 않는다.
- 메인이 Q-1 [Fable 원문](../2026-10-01-readability-baseline/goal-review.md)을 전부 읽고 BOM·WSL 동기화·SDK·템플릿 공백 표본 일치를 보고했다. F-3~F-10/F-15와 관련 참고를 아래에 반영한다. 이후 `msg_69bc8300f5b7`의 Q-1B 조건부 자체 분석기/위반 수 ratchet 결정은 로드맵으로 인계하며 Q-1A 범위를 바꾸지 않는다.
- checkout: `C:/Dev/DawnHolder_Project`. 실행 branch는 기존 준비 branch `bass131/q1-readability-20261001`을 **Q-1A 전용으로 배정**한다. Q-1은 실행 없는 로드맵으로 바뀌며 B/C는 별도 branch를 사용한다.
- base: 이번 진입에서 다시 fetch한 `origin/main` = `0239290d6f423dbfe91c42c3fffd0789f56de26f` (PR158 병합). 기존 준비 커밋 `30147f3`(초안·CURRENT·메인 CLAUDE O-5), `d40ba0b`(검토 원문·인계)을 보존했다.
- evidence: `.backups/verification/2026-10-01-readability-format-ci/` (Git 제외). 이전 원문·보호 기준·Fable 정산은 인접 `2026-10-01-readability-baseline/`에 있다. 실제 결과는 이 goal 한 곳에 기록한다.
- coordinator Run `run_a3a4a4d552d1`; 현재 실행 중인 worker는 없다. 새 worker는 새 Task/Dispatch로 발행한다. 메인 회신 주소도 이 Run이다.

## 범위와 불변 조건

`Dawnholder.slnx`의 8개 프로젝트와 `02_Server`, `04_ClientNet`, `98_Shared`, `99_Tools`의 수기 C# Compile 입력을 대상으로 한다. `GameServer.Tests`와 도구·봇을 포함한다. 검토자의 정적 집계는 추적 C# 222파일이며 실제 포매터 입력·생성물·변경 예정 수는 P0-A가 확정한다. 정적 파일 수를 실제 분석 완료 수로 쓰지 않는다.

- A 관련 파일: 루트/하위 `.editorconfig`, `global.json`, `.github/workflows/dotnet-tests.yml`, `99_Tools/sync-wsl.sh`, 필요한 서식 검사·증명 도구, `.git-blame-ignore-revs`, 해당 C# 소스, `CODE_CONVENTION.md`의 서식 부분, `DEVELOPMENT.md`의 실행 안내, 이 goal/CURRENT. 세부 도구 경로는 P0-A 후 A spec에서 고정한다.
- `98_Shared/Protocol/Generated/GenPackets.cs`, SDK 생성물, `obj`/`bin`은 수기 소스와 별도 목록으로 집계하고 보존한다. 프로토콜 생성 코드를 재생성하거나 정리하지 않는다. `99_Tools/PacketGenerator/PacketFormat.cs`는 수기 템플릿 소스이며 생성물로 오인해 제외하지 않는다.
- 이번 범위 밖: Sonar 도입·복잡도·중첩·순서·주석 규칙의 확대 및 전면 error 승격, TypeScript, Unity 분석기/에셋, 게임 기능·정책·DB, 프로토콜 ID·PDL·버전·직렬화 변경. 기존 SA1201/SA1202/IDE0011과 8파일 error를 약화하지 않는다.
- `CLAUDE.md`는 메인 단독 소유다. 이미 종료된 O-5 한 줄은 준비 커밋에 보존됐으며 추가 수정하지 않는다.
- Unity 보호 3파일의 내용·skip-worktree, stash 2개, 두 checkout의 `.claude/settings.local.json`, `.meta`·GUID·직렬화 값을 보존한다. 기준 hash는 [preservation-before.json](../../../.backups/verification/2026-10-01-readability-baseline/preservation-before.json)이다.

## 확정한 서식 정책

메인 결정은 현재 코드 다수와 도구 기본값을 따르는 아래 값이다. 같은 범위의 원본/복제/CI에 적용한다. 포매터는 **`dotnet format whitespace`**로 한정하며, 스타일·분석기 수정까지 하는 전체 하위 명령 없는 실행으로 넓히지 않는다(N-1 수용).

| 속성 | 값 | 근거와 보존 조건 | P0-A 예상 변경 파일 수 |
|---|---|---|---|
| `end_of_line` | `lf` | `.gitattributes`의 C# 정책과 일치. 검토 정적 표본은 222파일 모두 LF | 미측정 |
| `charset` | `utf-8` (BOM 없음) | BOM 제거 3파일을 개별 기록: Network의 `JobQueue.cs`/`RecvBuffer.cs`, PacketGenerator의 `Program.cs` | 실측 대기(검토 사전 관측 3파일) |
| `indent_style` / `indent_size` | `space` / `4` | 기존 코드 다수·도구 기본값. 범위 밖 Unity에는 새 정책을 확장하지 않음 | 미측정 |
| `insert_final_newline` | `true` | 파일 끝 개행을 통일하고 전후 내용을 증명 | 미측정 |
| `trim_trailing_whitespace` | `true` | 아래 템플릿 파일의 국소 보호를 함께 적용 | 미측정 |

`PacketFormat.cs`의 문자열 내부 줄 끝 공백은 생성 바이트의 일부다. 이 파일에만 `trim_trailing_whitespace = false`를 두는 국소 보호를 우선안으로 삼고, P0-A에서 실제 포매터/편집기 속성 영향을 확인한다. 국소 설정으로 보존할 수 없으면 해당 속성을 이 파일에 도입하지 않는 방안을 메인에 올린다. 템플릿 내용을 바꾸거나 digest 기대값을 재산출해 통과시키지 않는다.

고정 판정 근거는 `02_Server/GameServer.Tests/Tools/PacketGeneratorExitTests.cs`의 `ValidInput_ExitsZeroAndPreservesBaselineOutput`이다. 정규화 후 기대 SHA256 `5725B8CCC663816C8EA816DBCEBB2AF475BE21528CC28F3F110AFB109E630F5B`를 A와 후속 C3에서 유지한다. 검사 성공은 실제 실행 뒤에만 기록한다.

포매터 CLI 결과를 서식의 주 판정으로 둔다. IDE0055는 보조이며, 두 결과가 다르면 CLI/SDK/설정 차이를 조사해 메인에 보고한다. 임의 severity·설정 완화로 맞추지 않는다. 추가 레이아웃 값은 설치 SDK의 기본값을 확인해 기록하고 다수와 다른 값이 필요하면 메인에 올린다.

## P0-A — 승인 뒤 수행할 좁은 실측 계약

담당은 **신규 Sol `gpt-6.1-sol` xhigh 한 세션**이다. 아직 발행하지 않았다. 쓰기는 이 goal의 evidence와 저장소 밖 전용 임시 Windows/WSL 공간으로 한정한다. 추적 파일은 바꾸지 않고, 원본에 formatter를 적용하거나 결과를 역복사하지 않는다. 필요한 원본은 읽기 전용으로 수집하며 secrets·로컬 Claude 설정·사용자 Unity 변경을 복제하지 않는다.

| 실측 | 입력·관찰할 것 | 산출물/통과 의미 |
|---|---|---|
| 실행 환경 | 현재 Windows/WSL `dotnet --version`, 설치 SDK 목록, CI 설정, formatter 버전·slnx 지원 | 실제 값·명령·출력. CI 설정값과 실제 runner 실행값을 구별하고 미실행 CI는 미확인 |
| 설정 효과 | 임시 snapshot에 위 속성만 적용해 verify/report와 실제 format을 수행 | 파일별 변경 목록·속성별/모듈별 수·BOM·개행 변화·생성물 포함/제외 목록. 여러 속성이 같은 파일을 바꾸면 합계를 중복계산하지 않음 |
| 원본/복제 경계 | 원본 Git에서 snapshot/파일 manifest를 만들고 전용 WSL 복제본에서 분석 | commit·파일 hash·설정 hash·경로 정규화 대조. `.git`이 없는 복제본에서 Git 목록을 재계산하지 않음 |
| Windows 실행 가능성 | 임시 snapshot에서 네이티브 `dotnet format`과 필요한 프로젝트 로드/restore | SAC 등으로 막히면 정확한 명령·오류·상태 보존. 정책·권한 우회 금지 |
| 적용 경로 | Windows 네이티브 적용 또는 WSL 결과의 통제된 원본 역반영 중 하나 제안 | 실제 가능 경로, 쓰기 소유자, 허용 파일 목록과 전후 hash 대조 방식. 이 probe에서는 원본 역반영하지 않음 |
| 부작용 | restore/분석의 캐시·생성물과 Shared/ClientNet 변경 예정 여부 | NuGet 전역 캐시 쓰기 여부·경로, 임시 DLL 출력, 원본 DLL·추적 파일 불변 근거 |
| 공백 증명 | 전후 토큰/리터럴/주석 동등성의 구현 가능한 수단과 파일 예외 | 증명 도구의 입력·실행 방식·한계 제안. 정식 검사 fixture·독립 테스트는 Opus 소유 |

P0-A는 기존 프로젝트 의존성의 필요한 restore와 임시 공간의 포매터·부수적인 프로젝트 로드/컴파일까지만 수행한다. Sonar 설치·라이선스 다운로드·전체 게임/서버/DB 실행·포트 7777·Unity 실행은 수행하지 않는다. NuGet 캐시 변경은 부작용으로 기록하고, 가능한 범위에서는 task 전용 캐시를 사용한다. 기존 캐시·다른 WSL 작업 공간을 삭제하지 않는다.

SDK를 새로 설치하거나 OS/전역 설정을 바꾸지 않는다. Windows 10.0.301·WSL 10.0.300은 Fable의 당시 관측일 뿐이며 시작 시 다시 확인한다. 공통으로 쓸 수 있는 기존 SDK가 없으면 **필요한 정확 버전·설치 위치·영향을 사용자 승인 항목으로 메인에 보고**한다. 설치나 승인 부재를 성공으로 처리하지 않는다.

P0-A 산출물은 실행 보고와 원시 근거 경로, 정확한 snapshot/SDK/옵션, 속성 표의 예상 변경 수, 선택할 실행 경로·고정 버전/rollForward/CI 제안, 원본 불변 결과다. `P0-A 보고 → Astra goal 보완 → 메인 승인 → 별도의 신규 A 구현 Sol` 순서로 진행한다. P0 세션을 A 구현에 재사용하지 않는다.

## A 구현 설계 — P0-A 후 확정할 계약

### 로컬·WSL·CI 입력과 SDK

로컬은 2단 구조다. **Windows 원본 Git에서 검사 파일과 snapshot manifest를 만들고 WSL 복제본에서 분석**한다. manifest에는 source commit/작업 트리 상태·상대 경로·파일 hash·설정 hash·SDK를 넣는다. 복제본 결과를 저장소 상대 경로로 정규화하고 입력 hash가 다르면 실패시킨다. A는 승인된 전체 서식 범위를 검사하며 B의 변경 파일/ratchet 판정은 구현하지 않는다.

`sync-wsl.sh`는 현재 네 소스 트리와 루트 파일 4개만 복사한다. A에 필요한 모든 루트 입력을 명시적으로 동기화하는 변경을 범위에 포함한다. `.git`을 무조건 복제하거나 루트 전체를 역동기화하지 않는다. 기존 원본 소유 marker·경로 검증·lock·4트리 한정 삭제 경계를 보존한다. 검사 전 원본과 복제본의 설정 hash가 같아야 한다.

검사 진입점은 실제 `dotnet --version`을 기록하고 선택한 고정값과 다르면 실패한다. `global.json` 정확 버전, `rollForward` 축소, CI `global-json-file` 사용을 P0-A 근거로 제안한다. SDK/CI의 새 버전을 지금 정한 것으로 쓰지 않는다. CI의 formatter 검사는 로컬과 동일한 SDK·옵션·파일 목록·설정을 사용하며 최신 main과의 통합 결과에서도 통과해야 한다.

P0-A가 선택한 formatter 적용 경로 하나를 A spec에 고정한다. WSL 역반영을 선택하면 소유자 한 명이 manifest의 수기 C#만 대상으로 원본의 사전 hash가 여전히 같은지 확인하고 정확한 파일별로 반영한다. 원본이 달라졌거나 범위 밖 파일이 나오면 덮어쓰지 않고 중단·보고한다.

### 공백 전용 커밋과 의미 보존

설정·도구·문서, C# 공백 전용 정리, `.git-blame-ignore-revs`, 필요한 DLL 갱신을 분리한다. 공백 전용 커밋에는 승인된 수기 C#의 서식·BOM·파일 끝 개행 변화만 포함하고 의미를 바꾸는 리팩터링/멤버 이동/주석 재작성/기대값 갱신은 넣지 않는다.

주 증명은 **공백·개행을 제외한 토큰열, 문자열·문자 리터럴 값, 주석 본문의 동등성**이다. 프로젝트의 실제 parse options/전처리 조건을 반영하고 지시문·비활성 영역·raw/verbatim 문자열처럼 단순 whitespace 제거로 손상될 수 있는 입력을 보존한다. 단순 문자열 정규식으로 공백을 모두 지우는 비교는 증명이 아니다. 도구 구현과 독립 fixture는 별도 소유권으로 검증한다.

`git diff -w <parent> <format-commit> --exit-code`는 보조 근거다. BOM 제거·줄 이동·빈 줄 변화의 거짓 실패 가능성을 명시하고 실제 실패 hunk를 분류한다. 그 밖의 실패나 토큰/리터럴/주석 차이가 나오면 **중단하고 메인에 보고**한다. 허용 실패를 줄이기 위해 공백 외 변경을 숨기지 않는다.

공백 커밋의 실제 SHA를 후속 `.git-blame-ignore-revs` 커밋에 넣는다. 로컬 사용 안내는 `git blame --ignore-revs-file .git-blame-ignore-revs`와 저장소 로컬 설정의 선택적 안내로 제한하며, 전역 Git 설정을 바꾸지 않는다(N-4).

### Shared/ClientNet DLL

P0-A에서 Shared·ClientNet 소스가 바뀌는지 먼저 확인한다. **바뀌면 소스와 추적 DLL을 함께 커밋해 온 관례에 따라 DLL을 별도 커밋으로 갱신**하는 것이 기본이다. 관련 소스가 안 바뀌면 불필요한 DLL 재생성으로 범위를 늘리지 않는다.

빌드 소유자 한 명, 정확한 SDK, 두 DLL의 사전/사후 hash·사전 로컬 변경·복사 경로를 고정한다. Windows 빌드가 막히면 메인에 보고하며 우회하거나 다른 방식의 바이너리를 몰래 대신하지 않는다. `CopyToUnityPlugins`와 embedded source/PDB 때문에 공백 변화도 DLL hash를 바꿀 수 있음을 기록한다. 원본 DLL의 기존 사용자 변경은 사전 사본으로 보존한다.

공유 DLL 변경 시 서버와 Unity 소비 계약을 모두 확인한다. Unity 컴파일/실행 여부와 미실행 영향은 별도 판정으로 남긴다. 서버 테스트만으로 Unity 통과를 주장하지 않는다.

## 소유권과 독립 검증

- 메인 Claude: 확정 정책·사용자 결정·P0-A 및 보완 goal 승인, `CLAUDE.md`, 최종 원문 확인/R-2 표본 대조와 PR별 사용자 병합 승인 요청.
- Astra: goal/CURRENT·로드맵·설계·위임·결과 통합·Git/PR 단독 담당. 추적 파일의 구현/테스트/검증 판정을 대신하지 않는다.
- 신규 P0-A Sol: 위의 evidence/임시 공간만. 신규 A Sol: 승인된 설정·검사 도구·제품 소스·문서. **A formatter의 기계적 출력만은 실행 소유자 한 명이 기존 테스트 파일까지 쓰는 한정 예외**를 메인이 승인했다(F-9). 수동 테스트 의미 변경이나 테스트 작성 권한으로 확대하지 않는다.
- 신규 Opus `claude-opus-5-5`: 구현자 쓰기 종료 후 검사 도구의 fixture·자체 테스트와 독립 동작 보존 테스트를 단독 소유한다. 제품 결함은 번호로 반환하며 제품 파일을 고치지 않는다. 테스트 코드 정리가 필요하면 정리 세션과 판정 세션을 서로 다른 신규 세션으로 나눈다.

작업자는 [R-5/R-6](../../../00_Document/operations/ORCA.md#r5-worker-launch)의 Astra 아래 pane·첫 화면·준비 확인·최초 attach를 따른다. 지정 모델/실행 명령/화면/backend unknown을 구분하고 한 작업 뒤 정산·종료한다. 수정/재검증은 새 세션이며 같은 번호의 3회 재검증 실패는 메인에 보고한다. 동시 쓰기를 금지한다.

독립 Opus는 보고와 실제 diff·원시 실행 근거를 먼저 대조한다. 공백 증명 도구에는 실제로 놓칠 수 있는 문자열 공백, raw/verbatim 문자열, 주석, 전처리·인코딩 경계를 검증하는 fixture를 작성한다. fixture는 제품 slnx의 C# Compile 입력에 섞지 않고 테스트 실행이 임시 입력으로 다루게 한다.

동작 보존 검사는 동일한 독립 테스트를 서식 전 parent와 서식 후 head 양쪽에서 실행해 결과를 비교한다(F-16). 별도 임시 checkout/복제본과 lock을 사용하며 공유 branch를 실행 중 전환하지 않는다. 기존 전체 build/test와 PacketGenerator digest를 포함하고, 실제 DLL 변경이면 공개 API·소비 계약을 비교한다. 이 목표에는 DB·실제 플레이 성공을 포함하지 않으며 미실행 범위를 분명히 적는다.

검증 원문에는 코드 리뷰 절을 포함한다. 주석 의미/코드 일치, 읽을 수 있는 실행 흐름, 책임·초기화 순서 보존을 `file:line | 문제 | 대안 | 병합 차단/후속/참고`로 쓰고, `검토했고 지적 없음`과 `검토 안 함`을 구별한다. 공백 정리에서 선언 순서를 바꾸지 않으며, 이후 C 배치의 초기화 순서·공개 API 검증 계약은 로드맵에 인계한다.

## 완료조건·병합·종료

1. P0-A의 정확한 환경·변경 예정 파일/수·실행 경로·부작용·실패/미실측이 기록되고, 보완 goal과 A spec을 메인이 승인한다. 필요한 SDK 설치는 별도 사용자 승인 이후에만 수행한다.
2. 승인된 전체 수기 C# 범위의 formatter 재실행이 변경 0을 반환한다. 로컬/CI 입력·설정 hash와 정확한 SDK가 일치하며 누락·실행 실패를 통과로 처리하지 않는다. 생성물·범위 밖 파일을 따로 집계한다.
3. 공백 전용 커밋에 주 증명과 보조 diff 분류가 있고, 문자열/문자/주석·생성 바이트·선언 순서가 보존된다. formatter 설정/도구/문서/DLL/ignore-revs는 별도 커밋이다.
4. 신규 Opus 실사·독립 테스트·코드 리뷰에서 필수 결함을 해소한다. 전체 build/test와 고정 PacketGenerator digest, 보호 파일·stash·설정 hash·필요한 소비 계약의 결과/미실행 범위를 기록한다.
5. A 기간의 **C# 병합 동결**은 메인 결정이다. 타 세션 병합을 Astra가 임의 통제하지 않고 메인이 조율한다. 병합 승인 전에 fetch한 최신 main 기준의 통합 결과에서 formatter 변경 0과 필요한 재검증을 확인한다. main 변동이나 동결 위반이 보이면 메인에 보고하고 기존 검증을 그대로 재사용하지 않는다.
6. PR 병합은 매번 사용자 명시 승인 이후 **merge commit 방식만** 사용하며 메인이 `--merge`로 실행한다. squash/rebase merge로 공백 커밋을 소멸시키지 않는다. 병합 후 공백 SHA가 `origin/main`의 ancestor인지와 `git blame --ignore-revs-file` 표본이 실제로 해당 커밋을 건너뛰는지 확인한다.
7. goal 결과 기록까지 완료하면 Q-1A가 종료된다. 메인이 [R-8](../../../00_Document/operations/ORCA.md#r8-astra-lifecycle)에 따라 Astra를 교체한다. Q-1B는 최신 main의 별도 branch/goal과 R-7 시범 3회차 검토로 시작한다. A 병합 승인을 B 착수·병합 승인으로 사용하지 않는다.

로컬 `bass131/menu-probe-lifetime-p1b`의 `b3cf78a` checkpoint는 보존한다. 해당 작업을 재개할 때 **서식 PR 병합 후의 main을 기준으로 rebase하고 충돌·서식·보존 동작을 다시 확인하는 절차가 필요**하다. 이번 goal에서 그 branch를 전환·rebase·삭제하지 않는다.

## 다음 행동과 미실행

이번 문서 커밋의 경로/SHA를 메인에 status로 보고한다. **메인이 P0-A 발행을 승인하면** 좁은 Sol 작업을 시작한다. P0-A 보고 전에는 실제 변경 수·고정 SDK·적용 경로를 채택 완료로 기재하지 않는다. P0-A 승인과 A 구현 승인을 구분한다.

현재 formatter 실행·SDK 설치·제품/테스트 수정·빌드·CI 실행·DLL 갱신·PR·병합은 모두 미실행이다. 보호 파일은 진입 시점의 기존 기준을 유지한다. Q-1B에는 라이선스 원문에서 조건 확인 후 자체 분석기를 구현하는 정책과 위반 수 ratchet을 인계했다. 해당 조건 확인·B 설계는 A 진행을 막지 않는다.
