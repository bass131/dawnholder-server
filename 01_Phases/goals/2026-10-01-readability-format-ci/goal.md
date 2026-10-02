# Q-1A — P0-A 실측과 C# 서식·CI 정착

## 목표와 현재 단계

[Q-1 로드맵](../2026-10-01-readability-baseline/goal.md)의 첫 실행 목표다. 서버 솔루션의 C# 서식을 하나의 고정된 포매터 결과로 맞추고, 동작 보존 증명과 로컬·CI의 재현 가능한 검사로 유지한다. 먼저 P0-A에서 실행 환경·포매터·부작용을 임시 공간에서 실측하고, 그 결과로 이 goal을 보완한 뒤 실제 A 구현을 발행한다.

- 상태: **Q-1A 구현·독립 검증·사용자 승인·PR160 병합과 종료 대조 완료.** 메인이 전달한 사용자 결정에 따라 2026-10-02 02:05:13Z merge commit `10bcafd7f25a861318dff6412fa3dc542addd4ef`로 통합했다. 병합 head `070d1fb`의 실제 CI는 전체 success(제품834통과·5skip), 독립 판정은 CI-1·D-2 해소/신규 결함0이다. 병합 후 `755bdcc` ancestor·ignore-revs와 blame4표본을 확인했다. Windows 실제 검증은 사용자 선택3A에 따라 [Q-1B 진입 조건](#q1b-entry-windows)으로 이관했으며 이번 목표의 미실행 범위를 그대로 남긴다. 이 결과 기록의 Git 반영 상태와 R-8 교체는 아래 종료 인계를 따른다.
- 환경 보존 정정: **초기 Windows User PATH 불변 보고는 철회했고, 사용자 직접 실행으로 이번 작업의8항목만 제거 완료했다.** 메인 `msg_f17b37f089a8`과 실행 기록은 02:10:31Z 37→29항목·ExpandString 유지·after SHA256 `6FB87D14DB862FE871939211056E3623955F0513A4062D3D7B383D0BE0364DDA`를 남긴다. Astra는 현재 원문이 정확 제거안 proposedRaw와 같고 대상 task 경로0임을 읽기 전용 대조했다. 작업 전 PATH 정본은 없어 전체 원복이라고 부르지 않는다. 기존29항목과 순서는 유지했고 Astra의 registry 쓰기는0회다.
- 근거: 메인 `msg_b7f867c99e0f`(2026-10-01 14:28:26 UTC), [결정 사본](../../../.backups/verification/2026-10-01-readability-format-ci/main-split-decision.json). 사용자 결정은 메인 경유이며 사용자 직접 입력으로 격상하지 않는다.
- A 조건부 승인: 메인 `msg_ba75a9bdad3a`(2026-10-01 15:28:29 UTC), [승인 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-conditional-approval-c1.json). `2fda71b`의 A안에 아래 C-1을 추가하고 나머지 제안은 승인했다. 이 시점에는 SDK 설치의 사용자 승인이 전달되기 전이었다.
- SDK 사용자 승인 전달: 메인 `msg_7f23f9a1e303`(2026-10-01 15:56:51 UTC), [전달 원문](../../../.backups/verification/2026-10-01-readability-format-ci/sdk-user-approval.json). 사용자의 “SDK 설치 승인할게”를 메인이 전달했으며 사용자 직접 입력으로 격상하지 않는다. `26f878d`의 경로·버전 1건만 승인했고 A Sol 발행을 허용했다. 앞의 SDK 답변 대기는 이 전달로 해소됐다.
- 메인이 Q-1 [Fable 원문](../2026-10-01-readability-baseline/goal-review.md)을 전부 읽고 BOM·WSL 동기화·SDK·템플릿 공백 표본 일치를 보고했다. F-3~F-10/F-15와 관련 참고를 아래에 반영한다. 이후 `msg_69bc8300f5b7`의 Q-1B 조건부 자체 분석기/위반 수 ratchet 결정은 로드맵으로 인계하며 Q-1A 범위를 바꾸지 않는다.
- checkout: `C:/Dev/DawnHolder_Project`. 실행 branch는 기존 준비 branch `bass131/q1-readability-20261001`을 **Q-1A 전용으로 배정**한다. Q-1은 실행 없는 로드맵으로 바뀌며 B/C는 별도 branch를 사용한다.
- base: 이번 진입에서 다시 fetch한 `origin/main` = `0239290d6f423dbfe91c42c3fffd0789f56de26f` (PR158 병합). 기존 준비 커밋 `30147f3`(초안·CURRENT·메인 CLAUDE O-5), `d40ba0b`(검토 원문·인계)을 보존했다.
- evidence: `.backups/verification/2026-10-01-readability-format-ci/` (Git 제외). 이전 원문·보호 기준·Fable 정산은 인접 `2026-10-01-readability-baseline/`에 있다. 실제 결과는 이 goal 한 곳에 기록한다.
- coordinator Run `run_a3a4a4d552d1`; 제품 구현자·검증자는 각각 정산·종료했고 재사용하지 않는다. 제품 최종 Task `task_6ea6f03ba929` / Dispatch `ctx_a9315073dfd4`도 완료했다. 아래 별도 종료 기록 PR의 신규 Opus 정적 실사·사용자 승인·병합을 마친 뒤 메인이 R-8로 이 Astra를 교체한다. 신규 Astra는 현재 runtime·handle·incarnation·최신 main과 다음 goal을 새로 확인하고 이 Run/Dispatch를 실행 권한으로 재사용하지 않는다. Q-1B는 아직 착수하지 않았다.

## 범위와 불변 조건

`Dawnholder.slnx`의 8개 프로젝트와 `02_Server`, `04_ClientNet`, `98_Shared`, `99_Tools`의 수기 C# Compile 입력을 대상으로 한다. `GameServer.Tests`와 도구·봇을 포함한다. P0-A에서 원본 Git C# 목록과 8개 프로젝트의 초기 Compile 합집합 **222파일 = 수기 221 + GenPackets 1**의 일치를 확인했다. 원본 입력 manifest는 설정 등을 포함해 240개다. 추가 검사 도구/독립 테스트는 아래의 별도 프로젝트 목록으로 집계하며 원래 8개 프로젝트의 수치를 바꾸어 보고하지 않는다.

- A 관련 파일: 루트/하위 `.editorconfig`, `global.json`, `.github/workflows/dotnet-tests.yml`, `99_Tools/sync-wsl.sh`, 필요한 서식 검사·증명 도구, `.git-blame-ignore-revs`, 해당 C# 소스, `CODE_CONVENTION.md`의 서식 부분, `DEVELOPMENT.md`의 실행 안내, 이 goal/CURRENT. 세부 도구 경로는 P0-A 후 A spec에서 고정한다.
- `98_Shared/Protocol/Generated/GenPackets.cs`, SDK 생성물, `obj`/`bin`은 수기 소스와 별도 목록으로 집계하고 보존한다. 프로토콜 생성 코드를 재생성하거나 정리하지 않는다. `99_Tools/PacketGenerator/PacketFormat.cs`는 수기 템플릿 소스이며 생성물로 오인해 제외하지 않는다.
- 이번 범위 밖: Sonar 도입·복잡도·중첩·순서·주석 규칙의 확대 및 전면 error 승격, TypeScript, Unity 분석기/에셋, 게임 기능·정책·DB, 프로토콜 ID·PDL·버전·직렬화 변경. 기존 SA1201/SA1202/IDE0011과 8파일 error를 약화하지 않는다.
- `CLAUDE.md`는 메인 단독 소유다. 이미 종료된 O-5 한 줄은 준비 커밋에 보존됐으며 추가 수정하지 않는다.
- Unity 보호 3파일의 내용·skip-worktree, stash 2개, 두 checkout의 `.claude/settings.local.json`, `.meta`·GUID·직렬화 값을 보존한다. 기준 hash는 [preservation-before.json](../../../.backups/verification/2026-10-01-readability-baseline/preservation-before.json)이다.

## 확정한 서식 정책

메인 결정은 현재 코드 다수와 도구 기본값을 따르는 아래 값이다. 같은 범위의 원본/복제/CI에 적용한다. 포매터는 **`dotnet format whitespace`**로 한정하며, 스타일·분석기 수정까지 하는 전체 하위 명령 없는 실행으로 넓히지 않는다(N-1 수용).

| 속성 | 값 | 근거와 보존 조건 | P0-A 예상 변경 파일 수 |
|---|---|---|---|
| `end_of_line` | `lf` | `.gitattributes`의 C# 정책과 일치. P0-A 시작/최종 222파일 모두 LF | 0 |
| `charset` | `utf-8` (BOM 없음) | Network의 `JobQueue.cs`/`RecvBuffer.cs`, PacketGenerator의 `Program.cs` | 3 |
| `indent_style` / `indent_size` | `space` / `4` | 기존 대응 줄의 선행 공백 변화. 속성만 분리한 인과 실험은 아님. Unity에는 새 정책을 확장하지 않음 | 3 (layout 변화와 중복) |
| `insert_final_newline` | `true` | Network의 `Connector.cs`/`SendBuffer.cs` | 2 |
| `trim_trailing_whitespace` | `true` | 아래 템플릿 파일의 국소 보호를 함께 적용 | 0 |

SDK 기본 syntax whitespace/layout의 `WHITESPACE` 진단은 80파일/698건, 전체 중복 제거 변경은 **81파일**이었다. 속성별 숫자를 합산하지 않는다. 설치 SDK 기본 옵션의 reflection은 assembly version conflict로 실패해 개별 기본값 수치를 전부 확인하지 못했다. A는 추가 layout 값을 임의 지정하지 않고 승인될 정확 SDK의 CLI 출력을 계약으로 삼는 안이다.

`PacketFormat.cs`의 문자열 내부 줄 끝 공백은 생성 바이트의 일부다. 이 파일에만 `trim_trailing_whitespace = false`를 두는 국소 보호를 우선안으로 삼았다. P0-A에서 CLI 보존을 관측했으며 편집기 저장 동작은 미측정이다. A에서 보존할 수 없으면 해당 속성을 이 파일에 도입하지 않는 방안을 메인에 올린다. 템플릿 내용을 바꾸거나 digest 기대값을 재산출해 통과시키지 않는다.

고정 판정 근거는 `02_Server/GameServer.Tests/Tools/PacketGeneratorExitTests.cs`의 `ValidInput_ExitsZeroAndPreservesBaselineOutput`이다. 정규화 후 기대 SHA256 `5725B8CCC663816C8EA816DBCEBB2AF475BE21528CC28F3F110AFB109E630F5B`를 A와 후속 C3에서 유지한다. 검사 성공은 실제 실행 뒤에만 기록한다.

포매터 CLI 결과를 서식의 주 판정으로 둔다. IDE0055는 보조이며, 두 결과가 다르면 CLI/SDK/설정 차이를 조사해 메인에 보고한다. 임의 severity·설정 완화로 맞추지 않는다. 추가 레이아웃 값은 설치 SDK의 기본값을 확인해 기록하고 다수와 다른 값이 필요하면 메인에 올린다.

## P0-A — 수행한 좁은 실측 계약

메인 `msg_8789c1a1fb96`가 승인한 **신규 Sol `gpt-6.1-sol` xhigh 한 세션**이 아래 계약으로 수행했다. 쓰기는 이 goal의 evidence와 저장소 밖 전용 임시 Windows/WSL 공간으로 한정했다. 추적 파일·원본 formatter 적용·역복사·SDK 설치는 수행하지 않았다. 필요한 원본만 읽어 수집했고 secrets·로컬 Claude 설정·사용자 Unity 변경은 복제하지 않았다.

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

SDK를 새로 설치하거나 OS/전역 설정을 바꾸지 않는 계약을 유지했다. P0-A가 다시 확인한 실제 선택값도 Windows 10.0.301·WSL 10.0.300이며 공통 설치 SDK가 없었다. **정확 버전·설치 위치·영향을 아래 사용자 승인 항목으로 메인에 보고**한다. 설치나 승인 부재를 성공으로 처리하지 않는다.

P0-A 산출물은 실행 보고와 원시 근거 경로, 정확한 snapshot/SDK/옵션, 속성 표의 예상 변경 수, 선택할 실행 경로·고정 버전/rollForward/CI 제안, 원본 불변 결과다. `P0-A 보고 → Astra goal 보완 → 메인 승인 → 별도의 신규 A 구현 Sol` 순서로 진행한다. P0 세션을 A 구현에 재사용하지 않는다.

## P0-A 결과와 정산

- 원문: [P0-A report.md](../../../.backups/verification/2026-10-01-readability-format-ci/p0a-sol-1/report.md), [명령 인덱스](../../../.backups/verification/2026-10-01-readability-format-ci/p0a-sol-1/commands.md), [근거 파일 manifest](../../../.backups/verification/2026-10-01-readability-format-ci/p0a-sol-1/artifact-manifest.json). 최종 원문 SHA256 `735627B45D31C46AC70FC1149AF799B0F534119B606FD48957303486E4BFE7AA`를 Astra가 전부 읽었다. 독립 Opus 판정 원문은 아직 없다.
- 고정 입력 HEAD `95fe8f769b701d79bf672abdd1c36e1665bff2df`. Windows temp `C:/Users/bass1/AppData/Local/Temp/dawnholder-p0a-task_df82d0ec21ff/`, WSL temp `/home/bass1/.cache/dawnholder/p0a/task_df82d0ec21ff/`. 원본 Git manifest 240개와 WSL 복제 hash가 모두 일치했다. 자료는 보존하며 다른 공간이나 캐시를 삭제하지 않았다.
- Windows 설치 SDK `8.0.425/10.0.204/10.0.301`, WSL `10.0.300`. `10.0.301 + rollForward:disable` probe는 Windows exit 0, WSL exit 155(SDK 부재). CI는 설정만 확인했고 실제 runner는 미실행이다.
- 양쪽 모두 slnx whitespace verify **exit 2/81파일**, 임시 적용 **exit 0/81파일**, 재검사 **exit 0/report []**. 최종 C# 222개 바이트가 환경 간 동일했다. 다른 SDK의 같은 snapshot 결과이며 동일 SDK 재현을 증명하지 않는다. Windows native formatter의 SAC 차단은 없었지만 제품 build/실행의 정책 상태는 미확인이다.

| 모듈 | 수기 C# | 변경 예상 |
|---|---:|---:|
| GameServer.Tests | 87 | 32 |
| GameServer | 67 | 18 |
| Network | 7 | 5 |
| ClientNet | 7 | 0 |
| Shared | 16 | 8 |
| BgmComposer | 13 | 8 |
| headless-bot | 22 | 9 |
| PacketGenerator | 2 | 1 |
| 합계 | 221 | 81 |

`PacketFormat.cs`는 국소 trim=false와 임시 true 대조 모두 원본 hash가 같고 문자열 내부 줄 끝 공백 3줄을 보존했다. 편집기 저장 동작은 미측정이므로 **국소 false 유지**를 A안으로 제안한다. `GenPackets.cs`는 명시 exclude와 hash로 보존했다. 각 formatter snapshot의 design-time obj 39개 중 SDK 생성 C# 23개는 별도 집계했다. CLI의 245파일을 수기 입력 수로 사용하지 않는다. 참조 metadata 누락 메시지가 있어 제품 restore/build·완전한 참조 바인딩 성공으로 확대하지 않는다.

실제 21파일/Debug·Release 42관측의 token 원문·리터럴 값·주석·directive 표본은 일치했다. 전체 실제 Workspace ParseOptions를 취득한 전수 증명은 아니다. `CheatBuildGateTests.cs`의 Debug 분기 쉼표 뒤 공백 2→1이 Release DisabledTextTrivia 차이로 관측됐다. A는 해당 구간이 다른 실제 조건에서 활성화될 때 보존됨을 대응시켜 확인해야 한다. 단순 whitespace 제거로 비활성 텍스트를 통과시키지 않는다. 보조 diff -w의 16개 nonzero는 BOM 3개와 initializer 줄 분리 13개로 분류됐고 모두 표본에 포함됐다. PacketGenerator digest 테스트는 미실행이다.

부작용은 다음과 같이 남긴다. task NuGet package cache는 양쪽 0이며 WSL HTTP cache 2개와 scratch lock 24개가 생겼다. 초기 기본 scratch(Windows system temp/WSL `/tmp/NuGetScratchbass1`)의 실제 쓰기는 사전 inventory가 없어 미확인이다. 이후 task scratch를 명시했다. 첫 formatter 버전 실행은 개발용 인증서 설치 안내와 task CLI home의 0바이트 sentinel을 만들었다. 새 인증서 파일은 발견되지 않았고 사전 전수 목록 증명은 없다. **메인 독립 대조: 일치(기본 저장소 신규 없음), 방법: X509Store 읽기·WSL 디렉터리 timestamp**([전달 원문](../../../.backups/verification/2026-10-01-readability-format-ci/main-certificate-crosscheck.json)). 이후 process 한정 `DOTNET_GENERATE_ASPNET_CERTIFICATE=false`를 사용했고 인증서 삭제·신뢰 변경·개인키 export는 하지 않았다. 원본/임시 제품 DLL 생성·갱신은 없었고 관측 helper의 자체 DLL/restore는 별도다.

Astra의 [원본·보호 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-p0a-source-preservation.json)에서 입력 240개, 보호 5파일, skip-worktree 3개, stash 2개가 기준과 일치했고 tracked status 0이었다. [두 추적 DLL hash](../../../.backups/verification/2026-10-01-readability-format-ci/astra-p0a-dll-preservation.json)도 일치했다. 이는 실행 자료와 보존 여부의 대조이며 독립 제품 검증을 대신하지 않는다.

Task `task_df82d0ec21ff`, Dispatch `ctx_1025421355cc`, terminal `term_75d525b0-0f90-46d6-83be-f80c520d735e`, incarnation `b1f98cfa-c65d-4cdf-97e2-b68824058936`. 최초 명령은 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`, 화면은 GPT-6.1-Sol xhigh, backend는 unknown이었다. [worker_done](../../../.backups/verification/2026-10-01-readability-format-ci/p0a-worker-done.json) `msg_4761fff2b89c`(2026-10-01 15:20:57 UTC)의 succeeded는 **P0-A 계약 완료**다. [release](../../../.backups/verification/2026-10-01-readability-format-ci/p0a-sol-1-release.json)는 external_terminal/retained/processAction none이었고, runtime·incarnation·경로를 다시 대조한 뒤 [해당 pane close](../../../.backups/verification/2026-10-01-readability-format-ci/p0a-sol-1-close.json)의 ptyKilled=true를 확인했다. Delivery 전체를 ack했고 이 세션은 재사용하지 않는다.

## A 구현 계약 — C-1과 SDK 설치 승인 전달 완료

### 실행 경로·SDK·파일 소유권

메인이 승인한 A안은 **원본 적용 Windows native 10.0.301**, **검사 원본 Git manifest → 전용 WSL 복제 → 동일 SDK**다. 원본 역복사 도구는 포함하지 않는다. native가 실제 적용 때 막히면 상태를 메인에 올리고 별도 경로를 승인받는다. `.editorconfig`의 새 서식 속성은 네 소스 트리에만 적용해 P0의 임시 root `[*.cs]` 절을 그대로 Unity까지 확장하지 않는다. PacketFormat 국소 false와 생성 소스 제외/hash 검사를 유지한다.

**SDK 설치 승인 범위:** Linux x64 .NET SDK **10.0.301**과 동봉 runtime/host를 `/home/bass1/.local/share/dawnholder/dotnet-10.0.301/`에 추가한다. 실행파일은 그 경로의 `dotnet`, SDK 위치는 `sdk/10.0.301/`이다. 다운로드·전용 디스크 사용이 생긴다. 출처는 **Microsoft 공식 배포물**로 한정하고 버전·checksum·용량·설치 파일 근거를 남긴다. 기존 `/home/bass1/.dotnet`, 전역 PATH, 셸 profile, 시스템 설정은 바꾸지 않고 명시 실행파일과 아래 C-1 탐색 순서를 사용한다. Windows 설치·다른 버전·다른 위치는 승인 범위 밖이다.

설치 진행 근거: Sol `msg_3fbd80c02e2f`가 승인 경로 설치를 완료했다고 보고했다([수신 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-sdk-install-status.json)). `a-sol-1/official-sdk.json`과 `download-checksum.json`의 Microsoft 공식 metadata/archive SHA512는 `cfbeec3a3a1d3ad3e168e37a77c4cc26c23125acd84a86d014047da3ecffce4c368a9acac4d7c950a047fa3d98989ce8aea69f8e5842cb6d330e8911e1c335a7`로 같았고 다운로드 235,086,718 bytes/설치 628,124,550 bytes, SDK 표시 10.0.301을 기록했다. Astra는 이 원시 기록을 읽었으며 독립 설치 검증은 미실행이다. Windows python alias 실패로 240-input durable capture는 설치 **후** 복구해 P0 hash와 비교했다. 설치 전 WSL profile/기존 dotnet hash와 구분하고 사전 전수 증명으로 주장하지 않는다. 메인에도 한계를 전달했다. 실제 명령·파일 hash는 `a-sol-1/installed-sdk-*`, `wsl-preservation-before.json`에 보존한다.

동일 SDK 임시 재대조: Sol `msg_39cdb8acf1e1`의 [보고](../../../.backups/verification/2026-10-01-readability-format-ci/a-same-sdk-status.json)는 Windows/WSL 10.0.301에서 restore 0, verify 2, apply 0, rerun 0이다. Astra가 비교 JSON 240행을 읽어 변경 81파일·환경 간 최종 바이트 불일치 0·P0 변경 경로 차이 0을 확인했다([자료 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-a-cross-report-comparison.json)). 전수 Workspace 의미 증명 통과를 뜻하지 않는다. `msg_1017f83aff8e`는 임시 전후 Debug/Release build가 0이어도 Shared/ClientNet metadata 관련 Workspace 진단으로 manifest가 실패한다고 보고했다([진행 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-capacity-resumed-status.json)). 후속 `msg_72faa4f847bc`는 도구를 포함한 227소스·실제 Debug/Release 454조건·81변경·비활성 1영역 대응 증명에서 Windows 임시 proof 0을 보고했다([후속 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-capacity-2-resumed-status.json)). CP1의 최종 도구/입력으로 양쪽 전수 결과를 확인하기 전 원본 기존 C#을 적용하지 않는다.

전체 테스트 초기 실패: Sol `msg_1165e4a13f28`는 첫 baseline 임시 테스트 839개 중 829통과·5실패·5skip을 보고했다([원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-snapshot-content-omission-status.json)). 소스 위주 P0 snapshot에 실제 Content인 맵 terrain/content 6개 bin을 넣지 않은 누락을 원인으로 보고했고, 원본 6파일을 변경하지 않고 manifest·복제·hash 보호에 포함해 전후 Windows/WSL 테스트를 다시 실행 중이다. P0 240입력과 새 도구/Content가 추가된 A 집계를 구분한다. 최초 실패와 보완 후 결과를 모두 보존하며 최종 CP1 보고를 대조하기 전 자체 검사 전체 완료로 기록하지 않는다.

2026-10-01 16:36 UTC 모델 capacity 오류 후 동일 모델·세션·Task의 첫 재시도에서 실제 도구 실행이 재개됐다. 메인 `msg_71674b338ea8`가 전달한 사용자 지침은 지정 모델 유지, 간격을 늘린 재시도, 시각·결과 기록, 약 30분 이상 지속 실패나 상태 손상 때 보고다. [지침 원문](../../../.backups/verification/2026-10-01-readability-format-ci/capacity-retry-main-instruction.json)과 [관측 기록](../../../.backups/verification/2026-10-01-readability-format-ci/astra-capacity-retries.md)을 보존하며 사용자 직접 입력으로 격상하지 않는다.

메인 `msg_0b8d28f601a7`(16:52:50 UTC)의 [추가 사용자 지침 전달](../../../.backups/verification/2026-10-01-readability-format-ci/capacity-fallback-main-instruction.json)은 **Sol의 첫 capacity 관측 후 누적 30분이 지나도 재시도(1→2→5→10분)로 계속 실패할 때만**, 상태·근거 보존과 기존 작업자 정산·종료 뒤 같은 미완료 범위를 **새 외부 `gpt-6-astra` xhigh 작업자**에게 맡기는 것을 허용한다. 실행 중인 세션의 모델은 바꾸지 않고 역할 태그는 `[GameDev Sol]`을 유지한다. 최초 명령/화면의 실행 모델·요청 모델 Sol·backend unknown·전환 근거를 기록한다. 파트 리드의 직접 구현과 Opus 검증자 모델 대체는 허용하지 않는다. 16:52:09 UTC에는 기존 Sol의 재개 보고를 받았으므로 전환하지 않았다. 지침은 메인 경유이며 사용자 직접 입력으로 격상하지 않는다.

`global.json`은 정확히 `10.0.301`, `rollForward: disable`, CI는 `global-json-file: global.json`으로 바꾸는 계약이다. 진입점에서 실제 SDK가 다르면 실패하며 CI 실제 값도 로그에 남긴다. 설치 후 **원본 적용 전에** 동일 입력·설정으로 Windows/WSL format을 임시 공간에서 비교한다. 두 환경 일치·누락 없음·의미 차이 없음 확인이 선행조건이다. 알려진 81파일과 다른 변경은 원인을 보고하고 임의 포함하지 않는다. process 한정 task CLI home/package/http/plugins/**scratch** 경로와 `DOTNET_GENERATE_ASPNET_CERTIFICATE=false`를 첫 dotnet 호출부터 지정한다.

| 소유자 | 경로·책임 |
|---|---|
| 새 A Sol | `99_Tools/format-check.ps1`: Windows 원본 Git의 commit/상태/파일·설정 hash manifest 생성 및 WSL 검사 호출. source 쓰기와 검사를 분리 |
| 새 A Sol | `99_Tools/format-check.sh`: WSL·CI 검사 진입점. manifest/경로/SDK 검증, formatter exit/report, 정식 보존 도구 실행과 근거 출력 |
| 새 A Sol | `99_Tools/Formatting/Formatting.csproj`와 해당 디렉터리: Roslyn 보존 검사 CLI. manifest 해석·경로 검증·구문 비교 책임을 분리하며 실제 필요한 경계만 추상화 |
| 새 A Sol | 기존 설정/CI/sync-wsl, 범위 안 수기 C#의 기계적 서식, 필요한 Shared DLL, ignore-revs, CODE_CONVENTION/DEVELOPMENT 실행 안내 |
| 새 독립 Opus | `99_Tools/Formatting.Tests/`의 독립 테스트 프로젝트·fixture와 필요한 동작 보존 테스트. 제품 결함은 번호로 반환하고 검사 구현은 쓰지 않음 |
| Astra | 이 goal과 설계·위임·Git/PR. `CLAUDE.md`는 메인 소유 유지 |

검사 도구와 독립 테스트 프로젝트는 원래 8개 제품 프로젝트와 별도로 build/format/test하며 CI에 명시적으로 포함한다. fixture는 제품 Compile에 섞이지 않는 입력 파일로 다룬다. 새 도구 자체의 C#도 formatter/기존 적용 규칙/독립 리뷰 대상이다. 의존성은 구현 시 정확 버전·근거를 기록하며 Sonar나 Q-1B 규칙 구현을 추가하지 않는다.

CI는 실제 checkout SHA와 그 작업 트리에서 Git 입력 manifest를 새로 만든다. Windows와 CI는 같은 manifest schema·파일 열거/경로 정규화 구현을 공유하고, Windows가 만든 과거 manifest를 CI에 그대로 재사용하지 않는다. Git이 없는 WSL 복제본은 전달받은 manifest만 검증한다. PR의 실제 checkout이 합성 merge인지와 SHA를 기록하며 전체 범위를 검사하므로 B의 변경 파일/merge-base 판정을 미리 구현하지 않는다.

정식 보존 CLI는 같은 고정 SDK의 Workspace에서 **프로젝트별 실제 ParseOptions/전처리 기호**를 얻는다. source commit·작업 상태·파일/설정 hash·Compile 집합의 누락, 허용 목록 밖 경로, 경로 탈출, SDK/입력 불일치, load/parse 실패·결과 누락은 실패로 처리한다. Debug/Release의 token 종류/원문 순서·string/char/interpolated 값·주석본문·directive를 비교한다. 변경된 비활성 구간은 실제 조건에서 활성인 대응 구간의 보존 증명이 있어야 하며, 어느 대상 조건에서도 증명할 수 없으면 동일 원문을 요구하고 차이는 중단·보고한다. raw/verbatim 문자열·문서 주석·BOM/EOF 경계는 독립 fixture로 검증한다.

한 신규 A Sol의 작업 안에서도 쓰기와 Astra의 Git checkpoint를 순차 진행한다. 설정·도구·문서를 먼저 분리하고, 원본 적용 후 **기존 수기 C#만** 공백 커밋으로 만든다. Sol은 해당 checkpoint에서 쓰기를 멈추고 Astra가 실제 공백 SHA를 회신한 뒤 `.git-blame-ignore-revs`를 작성한다. 필요한 DLL은 별도 커밋이다. Sol은 commit/push하지 않는다. 최종 쓰기 종료·worker_done·정산 뒤에만 신규 Opus에게 테스트 쓰기를 넘긴다.

### 로컬·WSL·CI 입력과 SDK

로컬은 2단 구조다. **Windows 원본 Git에서 검사 파일과 snapshot manifest를 만들고 WSL 복제본에서 분석**한다. manifest에는 source commit/작업 트리 상태·상대 경로·파일 hash·설정 hash·SDK를 넣는다. 복제본 결과를 저장소 상대 경로로 정규화하고 입력 hash가 다르면 실패시킨다. A는 승인된 전체 서식 범위를 검사하며 B의 변경 파일/ratchet 판정은 구현하지 않는다.

`sync-wsl.sh`는 현재 네 소스 트리와 루트 파일 4개만 복사한다. A에 필요한 모든 루트 입력을 명시적으로 동기화하는 변경을 범위에 포함한다. `.git`을 무조건 복제하거나 루트 전체를 역동기화하지 않는다. 기존 원본 소유 marker·경로 검증·lock·4트리 한정 삭제 경계를 보존한다. 검사 전 원본과 복제본의 설정 hash가 같아야 한다.

현재 루트 4개에 `.gitattributes`, `.github/workflows/dotnet-tests.yml`, task별 manifest를 명시적으로 더한다. 앞의 두 파일은 MSBuild 필수 입력이 아니라 정책/CI 계약의 대조 자료다. 새 도구에 필요한 props/targets/NuGet.config/lock이 추가되면 그 파일도 명시 목록·hash에 넣는다. 하위 editorconfig와 도구 소스는 네 트리 범위에 포함하되 secrets·bin/obj를 원본에서 무차별 복사하지 않는다.

검사 진입점은 실제 `dotnet --version`을 기록하고 승인된 고정값과 다르면 실패한다. 위 10.0.301 pin/CI 계약은 승인됐고 전용 SDK 설치 보고를 받았으며, 정식 진입점 구현·전수 증명은 CP1 이전 진행 중이다. CI의 formatter 검사는 로컬과 동일한 SDK·옵션·파일 목록·설정을 사용하며 최신 main과의 통합 결과에서도 통과해야 한다. P0의 metadata 누락을 넘겨받지 않도록 정식 증명/빌드 전에 필요한 명시적 restore를 수행하고 결과를 기록한다.

### C-1 — 기존 WSL 표준 실행의 SDK 해석 보존

현재 `sync-wsl.sh`는 `DAWNHOLDER_DOTNET`, PATH의 `dotnet`, 마지막으로 `~/.dotnet/dotnet`을 고른다. 현재 fallback SDK는 10.0.300이므로 global.json만 10.0.301로 고정하면 기존 build/test/run 경로가 실패한다. 메인은 A에서 이 호환성 문제를 함께 해소하도록 조건을 붙였다. 문서에 기록한 현재 문제이며 스크립트 수정 완료가 아니다.

1. SDK 선택 순서는 **명시한 `DAWNHOLDER_DOTNET` → 승인된 전용 `/home/bass1/.local/share/dawnholder/dotnet-10.0.301/dotnet` → 기존 PATH 탐색/`~/.dotnet/dotnet` fallback**으로 바꾼다. 명시 override가 잘못됐거나 선택 SDK가 요구 버전과 다르면 다른 SDK로 조용히 우회하지 않고 명확히 실패한다.
2. 복제본의 `global.json`이 적용되는 작업 경로에서 선택 실행파일과 실제 SDK를 확인한다. 요구 버전·선택 경로·관측 버전 또는 선택 실패 원인을 오류에 남긴다. 버전 확인 실패를 build/test 성공으로 처리하지 않는다. build/test/run/bot의 공통 SDK 해석에 적용하고, 기존 sync/소유 marker/lock/경로 안전 경계를 유지한다.
3. `DEVELOPMENT.md`의 현재 실행 안내에 탐색 순서·설치 위치·override·버전 불일치 실패를 반영한다. **과거 ADR의 `~/.dotnet` 명령은 역사 기록으로 보존**하며 수정하지 않는다. 전역 PATH나 기존 SDK 설치를 바꾸지 않는다.
4. 신규 독립 Opus는 `DAWNHOLDER_DOTNET`과 PATH의 dotnet에 기대지 않는 표준 경로에서 `sync-wsl.sh build`와 `test`가 실제 10.0.301로 통과하는지 확인한다. override 우선순위·잘못된 버전/부재의 실패 경계도 독립 테스트로 검증한다. Windows의 설치된 10.0.301과 CI의 global-json-file에서도 동일 pin의 실제 build/test 결과·명령·SDK를 기록한다. 미실행이나 정책 차단은 별도로 보고하며 서버/게임/DB 실행으로 범위를 확대하지 않는다.

Windows native 적용 소유자 한 명이 manifest의 수기 C#만 대상으로 원본의 사전 hash가 여전히 같은지 확인한다. 원본이 달라졌거나 범위 밖 파일이 나오면 덮어쓰지 않고 중단·보고한다. WSL 역반영은 현재 제안에 포함하지 않는다.

### 공백 전용 커밋과 의미 보존

설정·도구·문서, C# 공백 전용 정리, `.git-blame-ignore-revs`, 필요한 DLL 갱신을 분리한다. 공백 전용 커밋에는 승인된 수기 C#의 서식·BOM·파일 끝 개행 변화만 포함하고 의미를 바꾸는 리팩터링/멤버 이동/주석 재작성/기대값 갱신은 넣지 않는다.

주 증명은 **공백·개행을 제외한 토큰열, 문자열·문자 리터럴 값, 주석 본문의 동등성**이다. 프로젝트의 실제 parse options/전처리 조건을 반영하고 지시문·비활성 영역·raw/verbatim 문자열처럼 단순 whitespace 제거로 손상될 수 있는 입력을 보존한다. 단순 문자열 정규식으로 공백을 모두 지우는 비교는 증명이 아니다. 도구 구현과 독립 fixture는 별도 소유권으로 검증한다.

`git diff -w <parent> <format-commit> --exit-code`는 보조 근거다. BOM 제거·줄 이동·빈 줄 변화의 거짓 실패 가능성을 명시하고 실제 실패 hunk를 분류한다. 그 밖의 실패나 토큰/리터럴/주석 차이가 나오면 **중단하고 메인에 보고**한다. 허용 실패를 줄이기 위해 공백 외 변경을 숨기지 않는다.

공백 커밋의 실제 SHA를 후속 `.git-blame-ignore-revs` 커밋에 넣는다. 로컬 사용 안내는 `git blame --ignore-revs-file .git-blame-ignore-revs`와 저장소 로컬 설정의 선택적 안내로 제한하며, 전역 Git 설정을 바꾸지 않는다(N-4).

### Shared/ClientNet DLL

P0-A에서 Shared 8파일 변경, ClientNet 0파일을 확인했다. **Shared의 추적 DLL을 별도 커밋으로 갱신**하는 안이다. ClientNet은 소비 계약 확인 대상이지만 소스가 그대로인 만큼 불필요한 DLL 재생성으로 범위를 늘리지 않는다. 필수 소비 빌드/SDK 변경이 다른 추적 DLL을 실제로 바꾸면 사전 사본·hash와 원인을 보존해 메인에 포함 여부를 보고한다.

CP3의 필수 Windows Debug build에서 두 DLL 모두 변경됐다. 메인 `msg_9c2df2c77d9e`(2026-10-01 18:03:23 UTC)는 **Shared와 ClientNet을 DLL 전용 커밋 하나로 포함**하도록 결정했다([원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp3-clientnet-main-decision.json)). 승인된 Q-1A 안의 메인 판단이며 사용자 직접 결정이 아니다. 병합 승인 요청 때 함께 보고하고 사용자 요청 시 이 커밋을 병합 전에 제외할 수 있도록 분리한다. `.meta`는 변경하지 않는다. 실제 빌드 출처는 SDK `10.0.301`, `Debug`, source HEAD `755bdccf74db6f7e83165d3455f3fd7b20639060`이며 ClientNet 기존 소스7개 변경은 0이다. DLL 자체를 담는 후속 커밋 SHA와 빌드 출처 SHA를 혼동하지 않는다.

| DLL | 빌드 전 SHA256 | 빌드 후 SHA256 |
|---|---|---|
| Shared | `b82f6a3285fd8196f8e5e0cd7476caae68c852458a40f68569ece7f096d4ee11` | `0f2a6c0a956fea13694a0c50d27d6da397c662e2d8cc8a62f7e82f5ab19029fc` |
| ClientNet | `ca54059fbe3db4b029583372213dd5908d627e69efc5923a7e391bbd5d38e8e8` | `db0f750fc69802aace2e2a0d25c0a4b0d36712e9de597fb975da4c6578ccb9bb` |

[Astra 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-cp3-dll-correspondence.json)는 사전 사본·실제 Debug 산출물·추적 DLL의 hash를 확인했다. 신규 Opus는 두 DLL의 공개 API metadata·서버/Unity 소비 계약·동일 SDK 재빌드 hash와 dirty 여부를 병합 전 독립 확인한다. 실제 `AssemblyInformationalVersion`에 build source SHA가 들어가므로 동일 source revision 재현과 최종 구현 HEAD 기본 빌드의 dirty 여부를 구분하며, 후자가 dirty면 무변경 조건 충족으로 보고하지 않는다. Unity 실행은 별도 미실행이다. Astra는 [실제 blocking reply](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp3-clientnet-reply.json)로 구현을 재개시켰고 제품 version 정책 변경은 허용하지 않았다.

후속 메인 `msg_f82fbe084582`(18:05:16 UTC)는 [커밋 간 재현성 요구와 좁은 설정 변경](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp3-dll-reproducibility-main-decision.json)을 추가 승인했다. 전용 Git 복제본에서 동일 소스에 빈 커밋만 더해 기본 빌드 DLL 바이트 변화를 먼저 측정한다. 위반이면 **Shared/ClientNet 두 프로젝트에만 commit 종속 정보를 넣지 않는 최소 빌드 속성**을 실측으로 선택해 허용한다. 이 한정 예외는 앞선 제품 csproj/version 정책 변경 금지보다 우선한다. 설정은 DLL 앞의 별도 커밋에 두고 그 뒤 DLL을 다시 빌드한다. 따라서 위 hash/HEAD는 CP3 최초 빌드 관측이며 최종 산출물로 확정한 값이 아니다. Astra만 Git 작업을 수행하고 Sol은 전용 복제본 준비와 두 프로젝트 구현·빌드만 한다. 원본 설정 커밋 때에도 tracked 쓰기를 멈춘 명시 checkpoint를 둔다. 신규 Opus는 빈 커밋 전후 재현성·최종 HEAD 기본 빌드 dirty 여부·공개 API 불변·서버/Unity 소비 계약을 확인한다. 이 추가 판단 역시 메인 결정이며 사용자 직접 결정이 아니고 병합 승인 요청에 포함한다.

실측과 설정 checkpoint: 전용 독립 clone에서 Astra가 만든 빈 커밋 `2b9a3d3f7a34db4f37f98f362fdc1a282e5e917d`와 parent `755bdcc`의 tree는 같다([Git 근거](../../../.backups/verification/2026-10-01-readability-format-ci/astra-cp3-repro-empty-commit.json)). Sol의 [재빌드 실측](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-1/cp3-repro-after-empty.json)은 실제 입력이 같은데도 두 DLL hash와 ProductVersion의 SHA가 달라짐을 기록했다. Sol은 두 csproj 각각에 `IncludeSourceRevisionInInformationalVersion=false`, `EnableSourceLink=false`와 주석 한 줄만 추가했고 기존 embedded source/PDB·deterministic·PathMap·API 소스·복사 target는 유지했다. Astra는 원본/clone 설정 hash와 전체 diff를 대조한 뒤 쓰기 중지 질문 `msg_3404436c38c7`에 따라 **`2dc546d6d405e0ca468ab3acf6e77118ce58b3f6`**(2파일6행)으로 설정만 커밋했다. clone에는 두 번째 빈 커밋 `b4c8e4dad59194e4eb82fef3ca461b2358ef9c01`을 만들고 [두 Git 결과](../../../.backups/verification/2026-10-01-readability-format-ci/astra-cp3-settings-commit.json)와 [reply](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp3-settings-reply.json)를 보존했다. 수정 후 재현성·원본 최종 DLL은 아직 측정 중이다.

메인 `msg_1a2f53824606`(18:24:03 UTC)은 [checkout 간 재현성 측정](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp3-cross-checkout-main-decision.json)을 추가했다. 수정 후 같은 tree를 원본과 독립 clone, 가능하면 다른 경로에서 빌드해 두 DLL을 비교한다. 같은 SDK라도 경로·remote·기타 차이가 남으면 원인을 보고하고 범위를 임의 확대하지 않는다. 커밋 간 동일 hash만으로 이 조건을 대체하지 않으며 신규 Opus도 독립 확인한다.

같은 커밋의 새 clone에서도 차이가 남았고, Sol의 `a-sol-1/cp3-cross-generated-source-hashes.json`과 `cp3-cross-cause-swap.json`은 두 프로젝트의 SDK 생성 `.NETStandard,Version=v2.1.AssemblyAttributes.cs` 입력 차이(원본204B/clone208B)를 맞춘 실험에서 두 DLL hash가 원본과 일치함을 기록했다. 생성 입력을 수동 교체한 실험을 기본 빌드 재현 통과로 쓰지 않는다. 메인 `msg_ecf6ba885d49`(18:52:19 UTC)는 원본 생성물이2026-06-07mtime/LF임을 직접 대조하고 [fresh 빌드 조치](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp3-fresh-dll-main-decision.json)를 승인했다. Sol은 원본 Shared/ClientNet의 obj/bin 네 디렉터리만 사전 목록·hash와 추적파일 없음·정확 절대경로·링크 없음 확인 뒤 정리하고, Windows10.0.301 Debug에서 새로 빌드해 fresh clone 및 원본 다음 기본 빌드와 비교한다. clone DLL 복사로 대신하지 않는다. DEVELOPMENT에는 기존 SDK obj가 남은 checkout의1회 clean 빌드 안내만 더한다. **정본 DLL은 Windows fresh 산출물**이며 Windows와 WSL/Linux fresh 산출의 차이는 신규 Opus가 측정·기록만 한다. OS 간 차이를 없애려는 범위 확대는 허용하지 않는다. stale obj 추론과 실제 후속 재현 결과를 구분하고, 다른 원인이 나오면 실측을 우선해 다시 메인에 보고한다.

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

CP1: Sol의 blocking ask `msg_35ed03eaeaa7`(2026-10-01 17:31:48 UTC)에 따라 전체 tracked 쓰기를 정지했다. Astra는 [CP1 원문 사본](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp1-report.md)(SHA256 `86283D487BC1F63B162E847FEA05E3B81C7494C26016B5AA714C3761D5939F76`)을 전부 읽고 원시 명령·test/digest 로그와 대조했다. [자료/현재 파일 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-cp1-correspondence-resolved.json)에서 255입력의 현재 원본 및 Windows/WSL 전후 hash 차이 0, 양쪽 227소스·실제 조건 454·변경 81·비활성 대응 증명 1을 확인했다. 원래 제품 수기221·생성1에 도구5소스를 더한 집계이며 제품 slnx8개는 유지한다. 임시 전후 Debug/Release build와 전후 Debug test(각 834통과·5skip·0실패), 고정 digest 통과는 구현자 자체 실행이다. 원본 적용과 신규 Opus 독립 검증을 대체하지 않는다.

최종 임시 제품/도구 formatter는 양쪽 0이다. 실제 Windows→WSL 진입점은 입력/hash/SDK/Compile 검증 후 아직 없는 `Formatting.Tests`를 명확히 실패로 처리했으며 전체 진입점·CI 통과로 기록하지 않는다. 초기 Workspace 실패 일부는 helper 로그명 재사용으로 원시 로그가 남지 않아 `a-sol-1/observed-failures.md`의 관측 요약만 존재한다. 이후 attempt별 원시 결과를 보존했고 설치 전 Windows 전수 hash 부재도 원문에 명시했다.

CP1 대조 때 `.git/config`의 후속 차이를 발견해 메인에 즉시 알렸다. 메인 `msg_8a98e72b00e1`은 같은 저장소 worktree의 Management PR159 `push -u`에 따른 정상 변경을 확인하고 진행을 허용했다([전달 원문](../../../.backups/verification/2026-10-01-readability-format-ci/cp1-shared-config-main-resolution.json)). Astra의 [읽기 전용 재구성](../../../.backups/verification/2026-10-01-readability-format-ci/astra-cp1-git-config-resolution-final.json)은 기존 Management branch의 `merge` 한 줄이 `refs/heads/main`에서 해당 branch로 바뀐 차이만 제거하면 사전 hash `8f67fa3d...`와 정확히 일치함을 확인했다. 설정을 되돌리지 않았다. 공유 Git config는 승인된 upstream 변경을 구분하며, 나머지 보호 파일 불일치는 없고 goal 차이는 Astra의 기록이다. Q-1A push 때 명시한 해당 branch upstream을 확인한다.

C-1과 SDK 승인 기록을 `1a78510`에 commit한 뒤 R-5/R-6에 따라 신규 A Sol을 발행했고 CP1까지 HEAD를 유지했다. **공식 배포물 checksum을 확인한 전용 SDK 설치 → 같은 입력·설정의 Windows/WSL 동일 SDK 임시 재대조 → 원본 적용** 순서를 지켰다. CP1은 설정·도구·문서 15파일과 Astra goal 기록을 `ff3154f7a9567b5e6069958908de41743482cbdf`에 커밋하고 실제 HEAD를 [blocking reply](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp1-reply.json)로 회신했다. [커밋 범위/clean 상태](../../../.backups/verification/2026-10-01-readability-format-ci/astra-cp1-commit.json)를 기록했다.

CP2: Sol `msg_f2a3e6688da6`(17:44:08 UTC)의 [원문 사본](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp2-report.md)을 전부 읽었다. 원본 Windows native10.0.301에서 수기221 include·GenPackets exclude로 verify2/apply0/rerun0, 전수 proof0을 실행했다. Astra의 [대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-cp2-correspondence.json)는 255개 현재 출력이 CP1 동일SDK 임시 출력과 모두 일치하며, proof227소스·454실제조건·변경81·비활성 대응1, 보호 기록의 현재hash가 일치함을 확인했다. 실제 `diff -w` 잔여16파일의 모든 hunk를 읽어 BOM제거3·initializer 줄분리12·switch arm 줄분리1 분류와 대조했다. 주 증명은 작업자의 token/literal/comment/directive 비교이며 이 대조는 독립 제품 검증이 아니다.

기존 수기 C#81파일만 **`755bdccf74db6f7e83165d3455f3fd7b20639060`**에 공백·승인 encoding 전용으로 커밋했다(parent **`ff3154f7a9567b5e6069958908de41743482cbdf`**). [커밋 후 clean 상태](../../../.backups/verification/2026-10-01-readability-format-ci/astra-cp2-commit.json)와 [실제 SHA reply](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp2-reply.json)를 보존했다. CP2까지 기존 DLL2개·GenPackets·Content6은 불변이며 원본 제품build를 하지 않았다. ignore-revs·필요 DLL·후속 도구 보완은 공백 커밋에 섞지 않는다.

CP3의 추가 확인: 메인 `msg_c972f219ae3b`가 `format-check.sh`의 원본 Shared/ClientNet Debug·Release 빌드에 따른 추적 Unity DLL 복사 가능성을 관찰했다([원문](../../../.backups/verification/2026-10-01-readability-format-ci/cp1-main-dll-sideeffect-observation.json)). 전용 Git/Linux 및 Windows 마운트 복제본에서 DLL hash/configuration·Git 상태·최종 검사/후속단계 영향을 재현하고, 필요하면 이미 허용된 도구·진입점 범위에서 보완해 별도 커밋한다. 제품 csproj·독립 테스트·Unity 자산 쓰기로 넓히지 않는다. 같은 항목은 신규 Opus의 독립 재현에도 포함한다. 원본 필수build가 ClientNet 추적 DLL을 실제로 바꾸면 사전사본·hash·원인과 포함 여부를 별도 질문한다.

원본 서식 적용·재검사와 구현자의 전수 비교, 임시 전후 build/test·고정 digest 실행까지 확인했다. 원본 최종build/test·C-1 표준 실행 경로·DLL 갱신·도구 부작용 보완·Opus 독립 판정·CI 실행·PR·병합은 아직 완료되지 않았다. Astra는 위의 명시 checkpoint 외에는 최종 구현 쓰기 종료 전 HEAD를 다시 바꾸지 않는다. Q-1B의 조건부 자체 분석기/위반 수 ratchet 결정은 로드맵에 유지하며 A 승인으로 B를 시작하지 않는다.

CP3 원본 Windows 테스트 관측: [cp3-original-test-Debug.log](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-1/cp3-original-test-Debug.log)는 총839개 중825통과·9실패·5skip이다. Astra는 9실패 모두 원본 `GameServer.Tests/bin/Debug/net10.0/Dawnholder.Tools.HeadlessBot.dll` 로딩의 앱 제어 정책 오류 `0x800711C7`임을 읽고 메인 `msg_6a1f85670009`로 보고했다. PacketGenerator 고정 digest는 같은 실행에서 통과했다. 정책 변경·우회를 하지 않으며 CP1 임시 Windows/WSL 전후 통과를 원본 전체 테스트 통과로 대체하지 않는다. DLL 재현성을 위한 새 명시 checkpoint와 WSL 표준 경로 등 나머지 승인 작업은 계속한다.

후속 CP3 자체 실행: [Sol 보고](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp3-fixed-native-status.json)와 `a-sol-1/cp3-current-original-*` 원시는 설정 커밋 `2dc546d`의 Windows10.0.301 explicit restore/Debug 전체 build/formatter가0, 전체 테스트가834통과·5skip·0실패임을 기록한다. Astra도 실제 테스트 요약을 읽었다. 최초 SAC 실패와 별도 결과이며 보안 정책이 변경되거나 영구 해결됐다는 증거는 아니다. 수정 후 같은 clone의 빈 커밋 전후 두 DLL hash는 일치했으나, 원본과 clone의 산출 hash 차이는 남아 메인 요구대로 같은 커밋의 새 독립 clone과 실제 입력/생성 metadata를 대조 중이다([진행 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp3-cross-checkout-progress.json)). WSL 표준 test 최초 시도는 본문 실행 전 작업 공간 소유 검사에서 중단됐고 [관측 원문 사본](../../../.backups/verification/2026-10-01-readability-format-ci/astra-cp3-standard-test-observation-1838.log)을 보존했다. 후속 시도·원인 확인·독립 판정은 아직 완료되지 않았다.

Windows fresh 후속: [Sol 보고](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp3-final-fresh-status.json)는 원본 네 obj/bin의54파일 사본/hash·추적0·링크0 확인 뒤 정리했고, 원본 fresh·다음 기본 빌드·같은 HEAD fresh clone의 두 DLL이 일치함을 기록한다. 최종 후보 hash는 Shared `0a6362c6a0572843fc6b686d0d66ca60fa5199247f86c87b6e99fc3f8b207618`, ClientNet `dc708e2f4c0443b87b397c371f6f040b2d32eacc83a77493650efc9e7a88e175`이며 Windows10.0.301/Debug/source `2dc546d` 산출물이다. [Astra 파일 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-cp3-fresh-dll-correspondence.json)는 추적 DLL·실제 bin 출력·fresh 사본·보고 hash가 모두 일치하고 `.meta` 변경0임을 확인했다. 제품별 실제 compiler 참조121개/생성 Compile 입력의 clone 대조와 fresh 전체 테스트834통과·5skip·고정digest/제품·도구 formatter0은 구현자 자체 실행이며 신규 Opus 판정을 대신하지 않는다.

C-1 잠금 보완: [Sol 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp3-lock-fd-status.json)과 `a-sol-1/cp3-lock-after-retry-build.json`/`cp3-lock-after-retry-test.json`에서 build0 직후 test1이 `sync-wsl.sh:34`의 flock 재획득 실패이며 VBCSCompiler가 task 환경의 workspace FD8과 helper FD9를 상속한 것을 확인했다. Astra는 기존 C-1/sync-wsl 범위에서 부모의 실행 중 잠금은 유지하고 장기 자식의 종료 뒤 잔류를 막는 최소 보완을 지시했다(`msg_8a9821f06cd3`, 메인 보고 `msg_e4f9c7fa4a0d`). lock 삭제·전역 compiler 종료·다른 작업 프로세스 종료는 허용하지 않았다. 새 runtime에서 즉시 build→test, 실제 동시 요청 거부, 종료 뒤 잠금 해제와 독립 검증을 남은 확인으로 둔다.

사전 파일 수 정정: Sol은 msg_ff82770d16e1의52개를 msg_3a9fe863866a에서54파일·16디렉터리로 정정했다([원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp3-precleanup-count-correction.json)). Astra가 실제 목록과54백업파일 hash를 전부 대조해 누락·차이0을 확인했다([대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-cp3-precleanup-count-and-backups.json)). 메인 msg_5d7d62d23aa8로 보고-원시 수치 불일치를 즉시 알렸으며, 이전52개 요약을 증명으로 사용하지 않는다. DLL hash 및 실행 결과는 별도 근거로 유지한다.

C-1 후속 자체 실행: [Sol 보고](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp3-standard-wsl-status.json)는 새 runtime에서 override 없이 전용10.0.301을 선택해 build0→즉시 test0(834통과·5skip), 동시 sync1 거부와 build/test 종료 뒤 flock 재획득0을 기록했다. Astra는 `cp3-standard-fresh-*.command.txt`, SDK 실제값, test 요약과 lock 결과를 읽었다. 최종 Windows→WSL 진입점은 원본 final-validate0 뒤 독립 Formatting.Tests 부재를 명확히 실패로 반환했으므로 전체 검사·CI·독립 검증 통과가 아니다. `cp3-fresh-windows-linux-dlls.json`은 같은10.0.301/Debug라도 Linux fresh DLL hash와 생성 TFM 소스의 줄끝 hash가 Windows와 다름을 기록하며 OS 차이를 없애는 추가 설정은 하지 않았다. 최종 구현 보고·쓰기 종료·정산과 신규 Opus 검증을 기다린다.

DLL embedded source 실사: Sol의 `a-sol-1/cp3-final-embedded-source-impact.json`은 Shared19/ClientNet10문서의 최종 embedded hash가 현재원본과 모두 일치함을 기록했다. 기존 ClientNet DLL의 Connector.cs가 task시작원본과 달라 메인에 즉시 보고했고(`msg_bde5037a74dd`), 후속 [분류 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-cp3-connector-classification-status.json)은 마지막CRLF→LF1byte뿐이며 본문2047byte와 task시작/최종원본이 같다고 밝혔다. Astra는 실제diff와 성공한native PEReader SHA에 binding한 blob추출·바이트assert를 읽고 메인 `msg_098f0b96672e`로 보충했다. 추가본문추출용native감사DLL은 AppControl0x800711C7로 차단됐고 원시로그를 남긴 뒤 DLL/PDB를 데이터로만 읽었다. 이 분류는 전체DLL차이가 metadata만이라는 뜻이 아니며 신규Opus의 공개API/소비계약/독립실사를 대체하지 않는다.

## A 구현 최종 정산 — 2026-10-01 19:41 UTC

- 완료 신호 `msg_eed7dae23a1c`의 Task `task_389cb0776c39` / Dispatch `ctx_05b030e24036`와 발신 handle이 구현 계약과 일치했다. 화면의 파일 쓰기 종료를 확인하고 [완료 receipt](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-1-worker-done.json)를 보존했다.
- Astra는 [최종 보고 원문 사본](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-1-final-report.md)을 전부 읽었다. SHA256 `BA34E7985E38AAD54DDA04C9E7E44F8A27BD66B193C9C1D79AED135FD27E1818`. 구현자 자체확인 보고이며 독립 판정이 아니다. 기존 실패·로그 부재·설치 전 기록 한계와 실제 CI/Unity/game/DB 미실행을 유지한다.
- [정산 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-a-sol-1-final-correspondence-resolved.json): 변경101개 목록과 완료 payload 일치, 남은9개 파일 해시 일치, 기존수기221+Gen1은 CP2 해시 그대로, 보호/승인 변경10개 해시 일치, stash2/skip-worktree3 유지. Astra의 첫 목록 대조 helper가 문자열 배열을 path 객체로 읽은 오류는 수정했고 실제 파일 해시 불일치는 없었다.
- `worker-release`는 외부 terminal이므로 retained/processAction none을 반환했다. runtime·incarnation·checkout을 다시 대조한 뒤 해당 pane만 close했고 `ptyKilled=true`였다. [release](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-1-release.json), [close](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-1-close.json). 구현 세션은 재사용하지 않는다.
- [남은 Git 정산](../../../.backups/verification/2026-10-01-readability-format-ci/astra-cp3-final-commits.json): 도구/문서6개 `0e0313bf6565ffdd653a167453e9949cd5edde53`, ignore-revs1개 `ae07a487d1b0956fc74b661499acfa538860147c`, Windows fresh DLL2개만 `80ac2dbd4693d924a26040a45d2e3a6abba844ba`. DLL 빌드 원본 HEAD는 `2dc546d6d405e0ca468ab3acf6e77118ce58b3f6`이며 이후 commit을 빌드 source로 오기하지 않는다. 서식 commit `755bdccf74db6f7e83165d3455f3fd7b20639060`은 보존했다.
- 신규 Opus는 보고·diff·원시 근거를 먼저 실사하고 독립 Formatting.Tests와 필요한 제품 동작 테스트만 작성한다. 제품 수정/Git 권한은 없으며 결함은 번호로 반환한다. 최종 구현 HEAD 기본 빌드·빈 commit·다른 checkout의 DLL 재현, C-1 즉시 build→test·잠금, 검사 진입점 부작용과 전체 통과를 독립 확인한다. 전체 검사 진입점은 현재 독립 테스트 부재로 명시 실패하며 아직 통과로 보고하지 않는다.
## A 신규 독립 검증 발행

- 구현 정산 후 clean HEAD `ee4df1a0683fcd87ad8d00d28ed15bad0efd0694`에서 신규 `claude --model claude-opus-5-5`를 담당 Astra 아래 vertical split으로 기동했다. 첫 화면 Claude Code2.1.287/Opus5.5 xhigh·빈 prompt, 모달/auto-mode-setup 안내 미관측, 일반 auto mode on 상태줄이었다. backend 실제 모델은 unknown이다.
- 신규 terminal `term_9872271b-637e-4788-9c4e-bac3ac50e973`, incarnation `39336662-2bfc-49b3-bdb0-a706e09a01c1`, runtime `8a673084-6819-45b9-a551-347226cdce9b`, 작업 경로 `C:/Dev/DawnHolder_Project`. [launch](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-1-launch.json), [ready](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-1-ready.json), [첫 화면](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-1-first-screen.json).
- 최초 작업을 한 번 연결했다. Task `task_606a0e3f7472` / Dispatch `ctx_0c23fe95f779`, [start receipt](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-1-start.json)의 input_accepted/turn_started 확인. attach launch model null을 실제 모델로 해석하지 않는다.
- [발행 계약](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-1-spec.txt), 판정 예정 원문 `.backups/verification/2026-10-01-readability-format-ci/a-opus-1/verdict.md`. 검증자는 독립 테스트만 쓰고 제품 결함을 번호로 반환한다. 원본 HEAD는 명시적인 Git checkpoint 질문 전까지 유지한다. Astra의 이 goal 상태 기록은 제품 변경과 구분한다.

## 독립 검증 중 PATH 보존 불일치 조사

- Astra는 Opus의 WSL command에 포함된 Windows task 도구 경로를 보고 Windows User/Process/Machine PATH를 읽기 전용 대조했다. [관측](../../../.backups/verification/2026-10-01-readability-format-ci/astra-windows-path-observation.json): User PATH에 P0 task1개+A Sol task7개, Process/Machine에는 해당 항목0개였다. 구현 보고의 PATH 불변 주장은 실제 관측과 불일치하며 통과 근거로 사용하지 않는다. 구현 원문은 그대로 보존한다. 메인도8개 항목을 독립 확인했다.
- [정확8항목 복구안](../../../.backups/verification/2026-10-01-readability-format-ci/astra-windows-path-remediation-proposal.json)은 현재 HKCU Environment/Path 원문·ExpandString 종류·SHA256 `351E10DAD0EE5AC658603A884D925545FFE01BFA4C974D8C55D7D6CE9F2BB2CE`와 제거 후 문자열을 보존한다. 나머지 항목/순서/빈구간/종류를 유지하고 실제 실행 직전 값 일치를 전제한다. 실행하지 않았다. task 시작 전 User PATH 정본이 없어 전체 원복이라고 부르지 않는다. 이전 목표의29행은 범위 밖으로 보존한다.
- 메인의 최종 결정은 `msg_621954fe6fe4`(2026-10-01 20:30:32Z), [원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-path-main-final-hold.json)이다. 사용자 승인 전 **새 CLI_HOME으로 Windows dotnet 첫 실행 금지**, User PATH 수정/복원 금지. 중간 조건부 허용은 철회됐다. Windows 진입점은 이번 Opus 판정에서 미실행으로 남기고 WSL·읽기 전용 검증은 계속한다. 정확8개 제거는 메인이 아침 사용자 결정으로 올린다. 제품 wrapper 결함은 Opus 번호 판정 뒤 신규 Sol 수정·신규 Opus 재검증으로 처리한다.
- [원천 감사](../../../.backups/verification/2026-10-01-readability-format-ci/astra-path-source-audit.md): 설치 SDK10.0.301의 .version VMR `96856fd726ffd058fb3dfef0851dbafc7ef3b011` → 공식 source-manifest → SDK `8ece2d6b49edf91f2eed52867ab8a8f48ac8a2e5`를 연결했다. 기본true인 DOTNET_ADD_GLOBAL_TOOLS_TO_PATH, first-use sentinel 조건, CLI_HOME/.dotnet/tools 계산, HKCU Environment/Path ExpandString 추가를 실제 소스에서 확인했다. 공식 문서는0/false/no의 추가 방지를 설명한다. Windows 실행시험이나 registry 쓰기는 하지 않았다.
- [WSL 현재 프로필 조사](../../../.backups/verification/2026-10-01-readability-format-ci/astra-wsl-profile-path-audit.json): ~/.profile·~/.bashrc와/etc/profile.d11파일에 dotnet/DOTNET 줄·이번 task 경로 없음, ~/.bash_profile 없음. 현재 내용의 읽기 전용 관측이며 모든 파일의 사전 불변을 새로 증명한 것으로 확대하지 않는다.
- 독립 테스트 dev2/dev3는 각각195개 중100통과95실패/exit1이며 원시 로그를 보존한다. 최종 중간 분류는 Roslyn MEF 의존성 누락88개·fixture PATH에서 python3 누락6개·restore 없는 package-free workspace에 대한 잘못된 실패 기대1개다. 첫89+6 보고를 원시 로그 대조 뒤88+6+1로 정정했다. fixture를 보완하고 마지막 기대를 제거한 dev4는194/194통과다. mutation M4가 드러낸 category 테스트의 약점은 별도로 보강했으며 dev5의 해당13/13통과를 확인했다. 전체 최종 실행·제품 판정은 아직 대기다.
- 어셈블리 비교 중간 JSON의 PacketGenerator/BgmComposer own_row null은 실제 AssemblyName 연결 오류였다. 수정된 `a-opus-1/item7-il-equivalence-v2.json`에서8프로젝트×Debug/Release의 실제 own assembly/hash와 일치를 대조했다. 이는 PDB/embedded source/SourceLink 등의 변수를 통제한 byte 비교이며 제품 기본 빌드 재현성 통과를 대신하지 않는다. 서식 전후 실제 기본 테스트는 Debug 각각834통과·5skip, Release 각각831통과·5skip이며 원시 command exit0와 로그 수치가 일치했다.

## A 독립 검증 진행 — 2026-10-01 21:13 UTC

- Linux 전체 검사 진입점 `entry-linux-2`는exit0이며, Windows mount의 Git 입력 진입점 `entry-mount-1-a-mount-git`도exit0를 확인했다. 이후 테스트 보강이 있어 최종 테스트 파일을 포함한 `entry-linux-3`와 mutation 재실행이 진행 중이다. C-1 표준 build 직후test는834통과·5skip/exit0, 동시sync 거부와 종료 후lock 회수 근거를 읽었다. 실제 GitHub CI와 Windows SDK 진입점 실행은 미실행이다.
- 독립 DLL metadata/API 비교 `a-opus-1/item9/item9-compare.json`은 양쪽 공개API·참조·TFM·version 동일, 최종 embedded 수기 소스가 build source `2dc546d`와 일치함을 기록했다. 기존 ClientNet의 Connector.cs embedded 차이는 별도 원시 분류와 대조하며 전체DLL 차이를 단일 원인으로 환원하지 않는다. Unity 실제 compile/play는 미실행이다.
- blocking ask `msg_852d166707fd`에 따라 Opus가 쓰기를 멈춘 소유 WSL clone의 owner·독립Git-dir·remote0·alternates없음·staged없음·HEAD/tree와 두 DLL hash를 확인했다. Astra가 이 clone에만 add 없이 빈 commit `ccd88f1f4ab0e0debde443862f478b8c69d60f62`을 만들었다. parent `ee4df1a0683fcd87ad8d00d28ed15bad0efd0694`, tree `f4c62969409410ea784636e4fdd0e741f93febcb`, 두 DLL의M상태/hash 모두 불변이다. [사전](../../../.backups/verification/2026-10-01-readability-format-ci/astra-opus-item10-empty-before.json)·[사후](../../../.backups/verification/2026-10-01-readability-format-ci/astra-opus-item10-empty-after.json). 첫 읽기 helper의 clone user.name 미설정 오류에는 쓰기가 없었고, commit identity는 process `git -c`만 사용했다. 증분·fresh·metadata 대조군은 Opus가 이어서 검증한다. 원본 HEAD는 고정 유지한다.

## A 첫 독립 검증 정산 — 2026-10-01 21:24 UTC

- 완료 `msg_f1906381b2ba`의 Task `task_606a0e3f7472`/Dispatch `ctx_0c23fe95f779`/발신자와 파일13개가 계약에 일치했다. outcome succeeded는 검증 작업 완료이며 제품 통과가 아니다. 판정은 **D-1 병합차단1건 반환**이다. [불변 최종 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-1-final-verdict.md), SHA256 `C6DF224600924F30942F950738CBA40A676E68BFB8EBFD8A29744676CC06AADB`를 Astra가 전부 읽었다. `worker-release`의 external_terminal retained 후 runtime/incarnation/path를 대조하고 해당 pane만 close해 ptyKilled=true를 확인했다. 검증자는 재사용하지 않는다.
- 최종 `entry-linux-3`는 195/195 통과·exit0, 268입력 final-validate·formatter report0이며 원본 테스트 13파일과 실제 실행 사본의 hash가 일치한다. Windows mount(a)의 Git 진입점과 (b)의 manifest/runtime 진입점은 보강 전 194/194·exit0다. mutation 8종은 보강 후 모두 검출했다. 동일 tree 빈 commit 전후 증분·fresh Linux 기본 빌드의 두 DLL hash가 같고 metadata 재활성 대조군만 달랐다. Windows native/PS1/fresh DLL 검증, 실제 CI·Unity compile/play·game/DB는 미실행이다.
- [Astra 정산 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-a-opus-1-final-correspondence.json): 테스트 13개 hash·payload·실제 untracked 목록 일치, 현재 보호 14개 hash와 사전/사후 HEAD·branch·stash·skip·Unity meta·기존 출력·WSL 상태 일치, User PATH는 앞의 오염 관측값에서 추가 변경 없음. Windows SDK는 사전 host 목록과 사후 폴더 조회로 방법이 달라 동등 실행으로 간주하지 않는다. Windows host 조회 2회, SDK/new CLI_HOME 실행 0회를 구분한다. 독립 테스트 13파일은 `9b2d649a2306c1836c0999bcac6e5b6da9d5b2f4`로 별도 커밋했다.
- 원문 수치 정정: §3 `entry-linux-1 exit2`는 내부 `tests-format`의 종료 코드다. 실제 외부 entrypoint command는 exit1이다. 둘 다 실패이므로 성공 판정 오류는 아니지만 보고 불일치로 메인 `msg_3574e393e6b9`에 즉시 알렸다. 원문은 수정하지 않고 위 정산 JSON의 정정을 함께 읽는다. 구현자 PATH 불변 불일치는 최종 원문 R-1과 제품 결함 D-1로 별도 유지한다.
- D-1은 `format-check.ps1`의 새 CLI_HOME에서 SDK PATH 추가를 끄지 않는 문제다. 기존 메인 승인 범위에서 신규 Sol이 ps1/sh/sync-wsl/CI의 첫 dotnet 호출 전부터 process 한정 방지 변수를 적용하고 신규 Opus가 독립 검증한다. 제품 wrapper가 호출자 환경의 방지 변수에 의존하는 것으로 수정 완료를 대신하지 않는다. 기존 PATH 8항목 정리와 Windows 실제 신규 CLI_HOME 시험은 사용자 승인 대기다.
- 코드 리뷰 후속은 원문 §5에 남긴다: checkout 동시 검사의 bootstrap build 잠금, 누적 state/cache 보존 정책, 긴 조건식 진단 메시지. 나머지 bootstrap bin/obj 부작용 문서·고정 home/SDK 문자열·인자 표현·미restore package-free workspace·blame 새 줄 한계는 참고다. D-1 수정 범위를 이들로 자동 확대하지 않는다.

## D-1 신규 Sol 수정 정산 — 2026-10-01 21:56 UTC

- 신규 Sol의 최초 명령은 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`, 첫 화면은 Codex0.159.3/GPT-6.1-Sol xhigh·빈 prompt였다. backend는 unknown이다. Task `task_b20983c3e7d8`/Dispatch `ctx_d12a3a31f889`, terminal `term_6b3d9e15-d5fb-44c0-bbce-9ad54ebdca9c`, incarnation `25c2abd4-6401-4d35-a577-6b613c9d46a2`에 input_accepted/turn_started를 확인했다. 작업 계약은 로컬 `a-sol-2-d1-spec.txt`다.
- 완료 `msg_e7ed6a3080ef`의 식별자·파일5개를 확인했고 [불변 보고 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-2-d1-final-report.md)을 전부 읽었다. SHA256 `342A80E8F256D4E3939A0C9F664EEA434CFAE8F979A9EE45E1F57F720C2FCD9D`. release 후 동일 runtime/incarnation/path를 확인해 해당 pane만 close했고 ptyKilled=true다. 재사용하지 않는다.
- PS1·두 Bash 진입점·CI가 첫 dotnet 호출 전부터 process 한정 `DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0`을 적용하며 DEVELOPMENT를 보완했다. 기존 PS1 저장/복원 구조·SDK 선택·FD8·snapshot 격리는 그대로다. 5파일 7삽입/3삭제를 `9fe9d0413ff23584dc257557e433396416b260ee`로 커밋했다. 테스트·제품 C#·SDK 설정·DLL은 바꾸지 않았다.
- Sol 자체 실행: WSL checker 외부0/내부35명령0, 독립 테스트195/195, 서식 report0/0/0, 보존238소스·입력268, C-1 build0→test0(834통과·5skip), 동시sync1 거부와 종료 후lock 재획득0. 외부 호출자의 방지 변수를 제거한 실행에서 제품 export0→SDK 조회 순서와 실제 자식 환경0을 표본 관측했다. 전체 프로세스의 완전 추적은 아니다. Windows SDK 호출·PS1 전체/환경복원 실행·실제 CI는 미실행이며 신규 Opus 판정을 대신하지 않는다.
- [Astra 정산 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-sol-2-d1-final-correspondence-resolved.json): 현재 수정5파일·원본/실행clone hash·완료목록·실제diff 일치, 기존 독립 테스트13개와 현재 보호14개 표본 hash 일치, 보호2648·기존출력1016·Unity meta1131의 사전/사후 기록 및 PATH·WSL 상태 일치. 첫 helper는 JSON 속성 순서 차이를 PATH 차이로 오인했다. 원문값·종류·hash·항목은 동일하며 의미 기반 대조로 정정했고 초기 결과도 보존했다. User PATH는 오염 후 기준값 그대로이며 복구하지 않았다.
- 실행 clone만 Astra가 `27f8cad8206c039ee152a9d64b6783cdac985297`로 checkpoint했다. 원본의5파일과 hash가 같고 parent2190269·tree9be57189·종료clean이다. 첫 reply의 불필요한 placeholder 문구는 실제 SHA로 즉시 정정했다. C-1 lock 격리용 HOME 변경 안내는 상위 지침 확인 뒤 실행 전에 철회했고 새 mount/unshare도 하지 않았다. 대신 기존 승인된 C-1 실행에 필요한 정확한 task lock `workspace-1fce5793b425c4ff5a82.lock` 하나만 미존재·경로 확인 후 생성·보존했다. 다른 lock과 전역 설정은 변경하지 않았다.
- 첫 Opus 원문 §6의 “유일한 commit”은 제품/checkpoint 직접 Git에 한정한다. 기존 `MiniRepository`와 manifest의 새commit 테스트는 일회용 합성 fixture에서 init/add/commit을 수행하며 기존 SDK 선택 테스트는 namespace fixture를 실행한다. 이 승인된 테스트 내부 동작을 직접 Git/새 lock 격리와 구분한다. Astra는 [정정 근거](../../../.backups/verification/2026-10-01-readability-format-ci/astra-a-opus-1-final-correspondence.json)의 fixtureGitClarification과 메인 `msg_ca2705022287`로 즉시 알렸다. 원문은 고치지 않았다.

## D-1 첫 독립 재검증 정산 — 2026-10-01 22:32 UTC

- 신규 `claude --model claude-opus-5-5`를 Astra 아래 vertical split에 기동했다. Task `task_c8cf48d2f2a5`/Dispatch `ctx_65707668145a`, terminal `term_003f31b6-d7d9-48ec-87d3-02f4ceaf2249`, incarnation `88483e77-b38e-4525-a8d0-3074e3339f85`다. Astra의 첫 화면은 ClaudeCode2.1.287/Opus5.5 xhigh·빈 prompt·모달 미관측이었고 backend는 unknown이다. [계약](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-2-d1-spec.txt)·[최초 연결](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-2-d1-start.json)의 input_accepted/turn_started를 확인했다. 실행 중 원본 HEAD `e8f2d32`는 유지했다.
- 완료 `msg_ca977c27fefd`의 식별자와 쓰기 범위를 대조했다. Astra는 [불변 최종 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-2-d1-final-verdict.md)을 전부 읽었다(SHA256 `1611E33675024572677942FC5D9F4A42B78837F1CC1D4862DFA1CD622DBD46D2`). 판정은 **D-1 해소 / 신규 D-2 비차단·후속1건**이며 Windows 실제 SDK 재현을 한 판정으로 확대하지 않는다. 원문의 작업자 화면 unknown과 별개로 Astra의 첫 화면 기록은 위와 같다.
- [Astra 원천 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-opus-2-d1-final-correspondence.json): 현재 보호16개 표본과 사전/사후 추적2650·기존출력1016·Unity meta1131파일에 차이 없음, 현재 User PATH 원문 hash/종류는 오염 후 기준과 같고 복구하지 않았다. 추가 테스트 한 파일의 원본/실제 checker 사본 hash `8941f74c83a22899337f1de98e7ef1d3bddfc7b3c4617238415cdb7ed7f1e4c4` 일치. 기존13파일195개는 그대로이고 신규5개를 `a88cb9248e549c410a497173a3d7ca6e2c22c5d9`에 별도 커밋했다.
- WSL check1 외부0·내부35명령0, 독립 테스트200/200, formatter report0/0/0, 보존239소스·입력269다. C-1은 호출자 true에서 build0→test0(834통과·5skip), 동시sync1 거부, 종료 후 자기lock 해제와 프로세스 종료, 잘못된 override3종1을 확인했다. 실제 자식 환경의0 전파는10ms 표본 관측이며 전수 추적이 아니다. format3종·sync2종·PS1 guard제거1종 변형은 모두 검출하고 원복했다.
- Windows는 실제 SDK에 위임하지 않는 task 소유 `dotnet.cmd`와 제한된 process PATH를 사용한 부분 시험만 수행했다(`msg_a73fae3af079` 질문에 `msg_904b37508edc`로 승인, 메인 `msg_663bc3185e37` 보고). 첫 가짜 SDK 조회와 tool-restore의 guard0을 관측했고 예상exit7에서 중단해 WSL에 도달하지 않았다. 실제 dotnet/SDK 호출0·native PS1전체 미실행·HKCU PATH 추가 방지의 실제 SDK 재현 미실행이다. 실제 CI와 Windows DLL 재현도 미실행이다.
- **D-2/R-1**: pwsh7.6.6/.NET10.0.12에서 PS1의 기존 finally가 미설정 변수를 빈 값으로 남긴다. P1/P4는 하네스의 null 처리 때문에 실제로 빈 값 호출자였으므로 미설정 시험으로 쓰지 않고 보존했다. 정확한 미설정으로 만든 P5/P6와 별도 더미변수 probe로 제품 복원 방식의 원인을 분리했다. DEVELOPMENT의 복원 문장과 Sol 보고의 “미설정 포함” 정적 추론은 이 경우 사실과 다르며 메인 `msg_8a19cd20b291`에 즉시 보고했다. 영구 PATH 결함과 구분되고 독립 판정은 비차단·후속이다. 메인 `msg_788606d2506d`에 이번 목표의 좁은 후속 수정 포함 여부를 요청했다. `a-sol-3-d2-spec-draft.txt`는 검토용 초안이며 발행하지 않았다.
- `worker-release`는 external_terminal retained/processAction none을 반환했다. 종료 전 확인한 동일 pane에 `terminal read`의 전송 전 draft가 있어 close를 보류하고 메인 `msg_3f0b7b800e99`에 보고했다. [관측](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-2-d1-draft-observed.json)의 draft는 출처가 확인되지 않았으며 사용자 제출·추가 작업 승인으로 취급하지 않는다. 완료 세션을 재사용하지 않는다.
- 최신 main을 실제 fetch한 [통합 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-final-main-integration-before-pr.json)는 `origin/main=0239290d6f423dbfe91c42c3fffd0789f56de26f`로 기존 base와 같고 현재 HEAD의 ancestor다. 제품 통합 충돌이나 새로운 C# 입력 변경은 없다. D-1 독립 결과를 유지하며 PR·실제 CI를 준비한다. 병합 전 사용자 명시 승인과 merge commit 방식은 그대로 필요하다.

## PR160·CI-1 수정 정산과 D-2 포함 — 2026-10-01 22:50 UTC

- [초안 PR160](https://github.com/bass131/dawnholder-server/pull/160)을 HEAD7649e3a로 생성했다. push는 이 목표 branch의 명시 ref만 사용했고 shared gitconfig hash는 `edc4f005…e93051` 그대로다. 첫 실제 Actions [run36935859919](https://github.com/bass131/dawnholder-server/actions/runs/36935859919)는 failure·jobs0·check-runs0이며 실행 로그가 없다. [실제 annotation](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-3-ci/github-run-annotation.txt)은 YAML18~22행의 `runner.temp`5개를 `Unrecognized named-value: runner`로 거부했다. 과거 CI YAML 검토와 로컬200/200을 실제CI 통과로 확대하지 않는다.
- 신규 CI Sol Task `task_0e10daa1cda4`/Dispatch `ctx_297c6a571133`, terminal `term_3128cbdc-d067-4a4c-9f6c-74d585e8a9d0`, incarnation `04eab5c4-87ed-4497-95c6-8ef6d28397b9`에 최초 연결했고 input_accepted/turn_started를 확인했다. 최초 명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`, 첫 화면0.159.3/GPT-6.1-Sol xhigh·빈 prompt, backend unknown이다. [계약](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-3-ci-spec.txt)은 workflow 한 파일만 허용했다. 계약/진행 보고의 경로6개 표기는 Astra 집계 오류였고 실제 DOTNET_CLI_HOME1+NuGet4=5로 작업자·메인에게 즉시 정정했다(`msg_f47e73e900c1`, `msg_b86cbe316cb7`).
- 완료 `msg_a1224d80d0ff`를 확인하고 Astra는 [불변 구현 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-3-ci-final-report.md)을 전부 읽었다(SHA256 `D9B0F435716B77422B6BBB4300FF65F02BE9109A01F1D00392E32E5EEDD552A3`). [원천 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-sol-3-ci-final-correspondence.json)는 실제 workflow diff/hash·완료 범위·현재 보호16개 표본을 대조했다. 전후 추적2651개 중 workflow 하나만 변경, 기존출력1016·Unity meta1131·PATH/stash/skip/독립테스트는 그대로다. release 뒤 정확 runtime/incarnation/path를 대조해 해당 pane만 close했고 ptyKilled=true다.
- CI-1 수정은 `223457763b7094637838d6a0153fd1e84865d4f7`, workflow1파일12삽입/5삭제다. job의 guard0/certificatefalse/telemetry1은 유지하고 경로5개는 checkout 다음·SDKsetup 이전 Bash step에서 RUNNER_TEMP 기반으로 GITHUB_ENV에 쓴다. 기존 action/trigger/pin/검사·restore/build/test는 유지했다. 기존 PyYAML/Bash와 task 소유 가짜 host의 자체36항목·6시나리오를 확인했으며 실제SDK 호출0·수정 후 실제CI는 아직 미실행이다. setup 대표 자식과 fixture scheduler 모사는 실제 GitHub action/runner를 대체하지 않는다.
- 메인 `msg_84135249c075`의 [결정](../../../.backups/verification/2026-10-01-readability-format-ci/a-d2-main-inclusion.json)은 D-2를 이번 목표에 포함하고 CI Sol 정산 뒤 신규 Sol의 PS1 한 파일 수정, 이후 신규 Opus 한 세션의 CI-1/D-2 합동 검증을 허용했다. 동일 checkout에서 두 작업자를 동시에 쓰지 않는다. D-2는 가짜 host 방식만 사용하고 실제 Windows SDK·PATH 정리는 계속 제외한다. 메인의 Q-1A 범위 결정이며 사용자 직접 결정이나 병합 승인이 아니다.
- 앞의 pane close 보류는 해결됐다. 실제 Orca 화면의 회색 문구와 [Claude Code 공식 prompt suggestions 설명](https://code.claude.com/docs/en/interactive-mode#prompt-suggestions)을 대조해 자동 추천으로 판단했다([기록](../../../.backups/verification/2026-10-01-readability-format-ci/astra-draft-resolution.json)). 추천을 제출하거나 승인으로 취급하지 않았다. D-1 Opus의 동일 runtime/incarnation/path를 재확인하고 [close](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-2-d1-close.json) ptyKilled=true로 종료했다. 메인도 두 회색 문구가 자동 추천임을 확인했다. 설정 변경은 없으며 메인 pane은 유지했다.

## D-2 수정 정산·첫 실제 CI 통과 — 2026-10-01 23:14 UTC

- CI Sol 종료 뒤 신규 D-2 Sol을 순차 발행했다. Task `task_0c72bdabd502`/Dispatch `ctx_81d6d7a2678c`, terminal `term_3b93de4f-159c-4887-9bfc-7e4744e1c59a`, incarnation `5433a01a-2cf0-43a3-8ce7-1f4caea306fd`. 최초 명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`, 첫 화면0.159.3/GPT-6.1-Sol xhigh·빈 prompt, backend unknown, input_accepted/turn_started 확인. [확정 계약](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-4-d2-spec.txt)은 PS1 한 파일과 task 가짜 host 부분 실행만 허용했다.
- 완료 `msg_611d81e10225` 후 Astra는 [불변 최종 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-4-d2-final-report.md)을 전부 읽었다(SHA256 `721615067DEEDFA7C2272D36E5158CA4FE091511C5C1B18C50333B8337EBC67B`). [Astra 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-sol-4-d2-final-correspondence.json)는 현재 보호18개 표본, 제품/실행 복사본 hash, 완료 목록과 실제 diff를 확인했다. 사전/사후 추적2651개 중 PS1만 변경, 기존출력1016·Unity meta1131·테스트14파일·PATH/stash/skip 불변이다. release 후 정확 runtime/incarnation/path를 대조해 해당 pane을 close했고 ptyKilled=true다.
- 수정 `1cee83b53ed848118e3ec84288f500a155057e6f`는 PS1 finally만8삽입/1삭제다. 원래 null이면 `[NullString]::Value`로 변수를 삭제하고 빈 값/일반 값은 그대로 복원한다. 최종 PS1 SHA256 `bccc74877d9c17689b1c3b1090ca8fcf67008e1b15bac1134e53f05a14111873`. 그 block 외 원문은 같고 D-1 guard·SDK 선택·잠금·snapshot은 유지했다.
- 구현자 자체 확인은 수정 전2사례에서 복원 실패(외부1)를 재현한 뒤 수정본8조합에서9변수의 미설정/빈 값/일반 값과 반환 후 자식 관측 복원을 확인했다. 첫 두 가짜 dotnet 호출의 guard0/certificatefalse, 내부 restore7·원래 오류 전달·lock 해제와 WSL 미도달을 확인했고 외부 하네스는8개 모두0이다. 실제 WindowsSDK0회이며 실패 경로 부분 시험이다. native PS1전체·다른PowerShell/runtime·독립 판정은 미실행이다.
- 초기 preflight는 pwsh가 자기 설치 폴더를 PATH에 추가해 정확한 경로 점검에서 멈췄고 제품/stub을 실행하지 않았다. 자식 내부에서 승인된 제한 PATH를 다시 적용한 별도 round로 재현했다. 첫 보호 비교는 단일 경로 배열 축소와 ordered dictionary 표시 오류였고 원문을 보존했다. 당시 비교의 child exit는 따로 기록되지 않아 후행 diff-check의 outer0을 비교 통과 근거로 쓰지 않는다. 정정된 `a-sol-4-d2/state-comparison-round2.json`의 직접 실행0과 실제 전후 값을 대조했다.
- CI-1 수정 후 실제 [run36937589813](https://github.com/bass131/dawnholder-server/actions/runs/36937589813)은23:12:54Z success로 종료했다. workflow head는 f93ae2c이며 checkout은 그 head를 기존 main0239290에 합친 임시 merge `beab27b`다. [원시 metadata](../../../.backups/verification/2026-10-01-readability-format-ci/a-ci-2-run.json)·[전체 로그](../../../.backups/verification/2026-10-01-readability-format-ci/a-ci-2-run.log): 경로 초기화·SDK setup/probe·checker·제품build/test 모두 success, checker는 Formatting checks passed, 제품은839개 중834통과·5skip이다. checker 내부 테스트 수는 성공 로그에 노출되지 않아 이전 로컬200개 수치를 실제CI 실측 수로 복사하지 않는다. 이 run에는 D-2 수정이 아직 없으므로 D-2 포함 최종 커밋의 CI와 신규 Opus 합동 검증을 이어간다.

## CI-1·D-2 합동 독립 검증 최종 정산 — 2026-10-01 23:49 UTC

- CI/D-2 구현자들을 순차 정산·종료한 뒤 clean HEAD `439997b1562b68c34726b0f97fd96e3f3bbe1919`를 고정하고 신규 `claude --model claude-opus-5-5`를 Astra 아래 vertical split에 기동했다. 첫 화면 ClaudeCode2.1.287/Opus5.5 xhigh·빈 prompt·선택창 미관측, backend unknown이다. terminal `term_df51c66f-20d2-42ce-b232-4b0b7a5ac093`, incarnation `43b29f35-7a85-40b0-b0e5-6c3e87865bfb`, Task `task_6ea6f03ba929`/Dispatch `ctx_a9315073dfd4`. [확정 계약](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-3-ci-d2-spec.txt)과 [연결 receipt](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-3-ci-d2-start.json)의 input_accepted/turn_started를 확인했다.
- 완료 `msg_9cf35a3788b7`의 발신자·Task/Dispatch를 확인했다. Astra는 [불변 판정 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-3-ci-d2-final-verdict.md)을 전부 읽었다. SHA256 `941B8AB72D00DCA6A21401E3649171B9700DAABFB27C9209A7EF38C50CCC7FD4`. 판정은 **CI-1 해소 / D-2 첫 재검증 해소 / 신규 결함0**이다. [Astra 원천 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-opus-3-ci-d2-final-correspondence.json)는 현재 보호20개 표본·전후 추적2651·기존출력1016·Unity meta1131·기존테스트14·입력원문14 기록과 실제 CI 로그, fixture 명령/결과를 대조했다. 제품·추적 테스트 변경0, 원본 clean/HEAD 유지다.
- 실제 [run36939767252](https://github.com/bass131/dawnholder-server/actions/runs/36939767252)는 event pull_request, head439997b, 23:15:04Z 생성→23:37:56Z completed/success다. checkout `a824a0cf7b718c33840fc784be9bce97605360b2`의 부모는 main0239290·head439997b이며 tree `299e18d89fc70ebdbdddc6dbdea76282598947a4`는 원본 head tree와 같다. [원시 metadata](../../../.backups/verification/2026-10-01-readability-format-ci/a-ci-3-run.json)·[로그](../../../.backups/verification/2026-10-01-readability-format-ci/a-ci-3-run.log)에서 SDK setup 전 경로5개·guard0/certificatefalse 전달, 실제 SDK10.0.301, Formatting checks passed, build0 error, 제품839=834통과+5skip을 확인했다. checker 내부 테스트 수는 성공 로그에 없어 이전 로컬200/200으로 대체하지 않는다. Linux CI는 PS1을 실행하지 않으며 D-2 통과 근거는 아래 별도 시험이다.
- 독립 CI fixture는 기존 WSL Python/PyYAML과 실제 YAML 초기화 block으로17검사를 통과했다. 공백 경로·후속 대표 자식의 환경값, RUNNER_TEMP 미설정·GITHUB_ENV 미설정/디렉터리/읽기전용 실패를 확인했다. 실제 Actions 실패 단계의 후속 skip은 실행하지 않았다. SDK/setup action을 fixture에서 실행했다고 보고하지 않는다.
- 독립 D-2는 task 소유 Windows PS1 byte 복사본·가짜 dotnet.cmd·제한 process PATH를 사용했다. pwsh7.6.6/.NET10.0.12에서 최종본6사례47검사 모두 통과했고,9변수 각각 미설정·빈 값·일반 문자열을 혼합해 반환 후 in-process·cmd·새 pwsh 자식으로 확인했다. tool-restore7·evidence 중복의 두 실패 지점, 같은 세션 두 번 호출, 원래 예외 전달과 lock 해제를 포함한다. 수정 전 복원·빈 값 과삭제·복원 제거·lock 해제 제거 변형4종을 검출하고 복사본 hash를 복구했다. 실제 Windows/WSL dotnet 호출0, native PS1 전체·성공 경로·WSL 단계는 미실행이다. 영구 xUnit 테스트는 추가하지 않았고 이번 task fixture가 근거다.
- 검증자 도구 실패: 초기 D-2 driver 배열 펼침 때문에 M3/M4의8호출이 인자 검사에서 멈췄고 제품을 실행하지 않았다. 수정된 round2는 해당 변형2종만 기대대로 검출했다. CI1차는 경로 문자열 dawnholder-dotnet의 dotnet 부분을 명령으로 오인한 검사식 때문에 실패했으며 초기화 block과 나머지16검사는 실행됐다. 수정된 토큰 검사는 음성 대조 후17검사를 통과했다. 이 실패들을 제품 통과 근거로 쓰지 않는다.
- 보고 정정과 보존 한계는 위 정산 JSON의 corrections에 남겼다. 완료 payload의 evidence 설명이 쉼표로 쪼개진 실경로 아닌 조각을 포함하므로 [실제 근거66파일 목록](../../../.backups/verification/2026-10-01-readability-format-ci/astra-opus-3-ci-d2-evidence-files.json)과 tracked 변경0을 정본으로 삼는다. failures.md의 모든 원문 보존 문구에는 예외가 있다: 초기 M3-no-restore-r 첫 개별 command JSON이 동명 두 번째 호출로 덮였고 회차 요약만 남았다. 종료 화면의 도구 실패2건 모두 제품 미실행 문구도 CI1차 초기화 block 실행과 구분해야 한다. 원문은 수정하지 않고 메인 `msg_57b494585f5a`·`msg_d7c76b9d6610`·`msg_6dfbb8ade04d`로 즉시 알렸다. 정상 최종본·정정 재실행·실제 CI의 근거는 별도로 존재한다.
- release는 external_terminal retained/processAction none을 반환했다. 동일 runtime/incarnation/path와 종료 화면을 확인한 뒤 [정확 pane close](../../../.backups/verification/2026-10-01-readability-format-ci/a-opus-3-ci-d2-close.json) ptyKilled=true, 완료 delivery ACK를 확인했다. 검증자를 재사용하지 않는다.
- 비차단 참고3건은 원문 §6에 남긴다: CI 초기화 위치의 설명 주석 부재, 정의된 빈 RUNNER_TEMP 미차단, 기존 action Node20/ubuntu-latest 변경 경고. 이번 제품 수정 범위를 확대하지 않는다. 실제 Windows SDK/native PS1 전체/Windows fresh DLL 독립 실행, User PATH8항목 복구, Unity compile/play·실제 게임/DB는 미실행이다. User PATH는 오염 후 기준값 보존이며 원상 복구가 아니다. 메인의 최종 원문/R-2 대조 및 사용자 PR160 병합 승인 뒤 merge commit 방식으로만 통합한다. 승인 전 자동 병합 예약·병합은 하지 않는다.

## 병합 결과와 종료 인계 — 2026-10-02

- 사용자 결정은 메인 `msg_48452f9fcb78`(02:08:35Z)의 [전달 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-main-merge-and-user-decisions.json)으로 받았다. 메인은 사용자의 직접 입력 “셋 다 승인”이 1A 두 PR 병합, 2A 이번 작업의 User PATH8항목만 제거, 3A Windows 실제 검증을 Q-1B 진입 때 방지 변수와 HKCU 전후 대조로1회 수행하는 선택이라고 전달했다. 이 세션의 사용자 직접 입력으로 격상하지 않는다.
- 메인이 PR159를 `96cc89a83d9abd305a46d025333382e86488ca2f`에 먼저 병합한 뒤 [PR160](https://github.com/bass131/dawnholder-server/pull/160)을 02:05:13Z `10bcafd7f25a861318dff6412fa3dc542addd4ef`에 merge commit으로 병합했다. [실제 PR 조회](../../../.backups/verification/2026-10-01-readability-format-ci/a-postmerge-pr160.json)는 MERGED/head070d1fb/merge10bcafd를 확인한다. Astra는 병합을 실행하지 않았다.
- 메인의 최종 원문/R-2 대조 완료는 `msg_47c521aa926b`의 [기록](../../../.backups/verification/2026-10-01-readability-format-ci/a-main-final-r2-and-ci-watch.json)에 있다. 병합 head `070d1fb7069757b07b1d8bd9719f0dc1ce6ea6ec`의 [CI run36942931587](https://github.com/bass131/dawnholder-server/actions/runs/36942931587)은 00:10:18Z 전체 success였다. [원시 metadata](../../../.backups/verification/2026-10-01-readability-format-ci/a-ci-4-run.json)·[로그](../../../.backups/verification/2026-10-01-readability-format-ci/a-ci-4-run.log)에서 SDK10.0.301, Formatting checks passed, build 성공, 제품839=834통과+5skip을 확인했다. checker 내부 테스트 수는 이 성공 로그로 확인하지 못했고 이전 독립 로컬200/200과 구분한다.
- 병합 후 실제 fetch한 `origin/main=10bcafd`의 부모는 main96cc89a와 PR head070d1fb다. head070d1fb 대비 차이는 `05_Management/`56파일뿐이며 Q-1A 입력과 겹치지 않는다. [Astra 종료 대조](../../../.backups/verification/2026-10-01-readability-format-ci/astra-postmerge-final-audit.json)는 서식 commit `755bdccf74db6f7e83165d3455f3fd7b20639060`의 main ancestor와 ignore-revs blob 일치를 확인했다. main에서 PlayerEntity.cs:261, CheatBuildGateTests.cs:25, SkillCatalog.cs:35, PacketGenerator/Program.cs:1의 기본 blame은755bdcc이고 `--ignore-revs-file`을 적용하면 각각 기존 작성 commit으로 돌아간다. 이4개 표본을 전수 blame 보존으로 확대하지 않는다.
- User PATH는 메인의 최초 실행 시도가 Claude Code 권한 분류기에 거부돼 미실행이었고, 이후 사용자가 직접 실행했다. 메인 `msg_f17b37f089a8`의 [완료 전달](../../../.backups/verification/2026-10-01-readability-format-ci/a-main-path-user-executed.json), [백업](../../../.backups/verification/2026-10-01-readability-format-ci/main-user-path-backup-20261002T021031Z.json), [실행 기록](../../../.backups/verification/2026-10-01-readability-format-ci/main-user-path-remediation-20261002T021031Z.json): 02:10:31Z 정확8항목 제거,37→29,ExpandString 유지, before351E10DA…→after6FB87D14…。 Astra가 registry를 읽어 현재 원문과 정확 제거안 proposedRaw의 일치·대상 task 경로0을 확인했다. 기존 Codex 경로와 나머지 항목/순서를 보존했으며 작업 전 정본 복구를 증명한 것은 아니다. Astra의 registry 쓰기는0회다.
- 모든 구현자·검증자는 정산·종료했고 재사용하지 않는다. 실제 Windows SDK/native PS1 전체/Windows fresh DLL의 신규 독립 실행, Unity compile/play·실제 게임/DB는 이번 목표에서 미실행이다. Windows1회 검증은 아래 사용자 선택3A로 이관하며 기존 비차단 코드리뷰·CI 참고도 원문에 남긴다. Q-1 전체/B/C 완료로 보고하지 않는다.
- 실행 branch `bass131/q1-readability-20261001`의 병합 head는070d1fb, 기존 base는0239290이었다. 병합 결과의 정본은 main10bcafd다. 완료 보고 뒤 메인이 R-8로 현재 GameDev Astra를 교체한다. 로컬 `bass131/menu-probe-lifetime-p1b`의 b3cf78a, archive branch, 보호 파일·stash·기존 task 근거/임시 공간은 보존하며 정리하지 않는다. CURRENT는 이 완료 goal과 로드맵 링크를 유지한다.

### 종료 기록의 Git 통합

메인 `msg_79e2f9371013`(02:19:52Z)의 [결정 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-main-closeout-docs-pr.json)에 따라 병합 main `10bcafd`에서 `docs/q1a-closeout-20261002`를 만들고 이 goal과 Q-1 로드맵의 인계 링크만 별도 PR로 통합한다. 새 목표가 아닌 Q-1A의 종료 기록이다. 신규 Opus 한 세션이 두 파일의 링크·사실·미실행 표기를 정적으로 실사한다. 제품 변경·추가 SDK 실행은 없으며 이 기록 PR에도 별도 사용자 병합 승인이 필요하다. 현재 판정·PR 생성·승인은 대기 중이다.

<a id="q1b-entry-windows"></a>

### Q-1B 진입 조건 — 사용자 선택3A

Q-1B를 시작하는 새 Astra는 최신 main에서 별도 branch/goal을 만들고 R-7 Fable 시범3회차 절차를 따른다. 그 goal과 외부 작업 계약에 **Windows 실제 검증1회**를 먼저 반영한다. SDK 첫 호출부터 task 전용 CLI_HOME/NuGet, `DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0`, `DOTNET_GENERATE_ASPNET_CERTIFICATE=false`를 적용하고 HKCU `Environment/Path`의 전후 원문·종류·hash·항목을 비교한다. 실행 명령·실제 SDK·종료 코드와 실제 확인 범위를 기록한다.

사용자 PATH 정리 결과를 당시의 새로운 기준으로 삼되, 실행 직전 현재 값을 다시 읽는다. 방지 변수 설정만으로 영구 PATH 불변을 성공 처리하지 않는다. 실제 변경·권한 거부·실패가 있으면 근거와 상태를 보존해 메인에 보고한다. 1회 검증은 아직 수행하지 않았으며 Q-1A의 가짜 host/WSL/CI 결과로 대신하지 않는다. 이 인계는 Q-1B의 목표·제품 구현을 이번 Astra가 시작했다는 뜻이 아니다.
