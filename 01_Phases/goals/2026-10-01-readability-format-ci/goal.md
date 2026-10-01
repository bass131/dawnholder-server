# Q-1A — P0-A 실측과 C# 서식·CI 정착

## 목표와 현재 단계

[Q-1 로드맵](../2026-10-01-readability-baseline/goal.md)의 첫 실행 목표다. 서버 솔루션의 C# 서식을 하나의 고정된 포매터 결과로 맞추고, 동작 보존 증명과 로컬·CI의 재현 가능한 검사로 유지한다. 먼저 P0-A에서 실행 환경·포매터·부작용을 임시 공간에서 실측하고, 그 결과로 이 goal을 보완한 뒤 실제 A 구현을 발행한다.

- 상태: **P0-A와 A Sol의 CP1 자체 확인·쓰기 중지 보고를 받았다. 설정·도구·문서 커밋 후 같은 작업자가 원본 서식 적용 단계로 진행한다.** 원본 서식 적용·독립 Opus 검증은 아직 완료 보고를 받지 않았다.
- 근거: 메인 `msg_b7f867c99e0f`(2026-10-01 14:28:26 UTC), [결정 사본](../../../.backups/verification/2026-10-01-readability-format-ci/main-split-decision.json). 사용자 결정은 메인 경유이며 사용자 직접 입력으로 격상하지 않는다.
- A 조건부 승인: 메인 `msg_ba75a9bdad3a`(2026-10-01 15:28:29 UTC), [승인 원문](../../../.backups/verification/2026-10-01-readability-format-ci/a-conditional-approval-c1.json). `2fda71b`의 A안에 아래 C-1을 추가하고 나머지 제안은 승인했다. 이 시점에는 SDK 설치의 사용자 승인이 전달되기 전이었다.
- SDK 사용자 승인 전달: 메인 `msg_7f23f9a1e303`(2026-10-01 15:56:51 UTC), [전달 원문](../../../.backups/verification/2026-10-01-readability-format-ci/sdk-user-approval.json). 사용자의 “SDK 설치 승인할게”를 메인이 전달했으며 사용자 직접 입력으로 격상하지 않는다. `26f878d`의 경로·버전 1건만 승인했고 A Sol 발행을 허용했다. 앞의 SDK 답변 대기는 이 전달로 해소됐다.
- 메인이 Q-1 [Fable 원문](../2026-10-01-readability-baseline/goal-review.md)을 전부 읽고 BOM·WSL 동기화·SDK·템플릿 공백 표본 일치를 보고했다. F-3~F-10/F-15와 관련 참고를 아래에 반영한다. 이후 `msg_69bc8300f5b7`의 Q-1B 조건부 자체 분석기/위반 수 ratchet 결정은 로드맵으로 인계하며 Q-1A 범위를 바꾸지 않는다.
- checkout: `C:/Dev/DawnHolder_Project`. 실행 branch는 기존 준비 branch `bass131/q1-readability-20261001`을 **Q-1A 전용으로 배정**한다. Q-1은 실행 없는 로드맵으로 바뀌며 B/C는 별도 branch를 사용한다.
- base: 이번 진입에서 다시 fetch한 `origin/main` = `0239290d6f423dbfe91c42c3fffd0789f56de26f` (PR158 병합). 기존 준비 커밋 `30147f3`(초안·CURRENT·메인 CLAUDE O-5), `d40ba0b`(검토 원문·인계)을 보존했다.
- evidence: `.backups/verification/2026-10-01-readability-format-ci/` (Git 제외). 이전 원문·보호 기준·Fable 정산은 인접 `2026-10-01-readability-baseline/`에 있다. 실제 결과는 이 goal 한 곳에 기록한다.
- coordinator Run `run_a3a4a4d552d1`; P0-A worker는 정산·종료했다. 현재 A Task `task_389cb0776c39` / Dispatch `ctx_05b030e24036`, terminal `term_dff33cb7-c289-4447-af55-2980cd371d40`, incarnation `c92e0d71-1d4a-4f04-b7cb-c2f6e9a8746d`. 신규 pane의 최초 명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh`와 첫 화면 GPT-6.1-Sol xhigh/빈 prompt를 확인했다. backend는 unknown. [시작 receipt](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-1-start.json)의 input_accepted/turn_started를 확인했고 [작업 계약](../../../.backups/verification/2026-10-01-readability-format-ci/a-sol-1-spec.txt)에 CP1/CP2 쓰기중지·Astra Git 순서를 명시했다. 메인 회신 주소도 이 Run이다.

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

C-1과 SDK 승인 기록을 `1a78510`에 commit한 뒤 R-5/R-6에 따라 신규 A Sol을 발행했고 CP1까지 HEAD를 유지했다. **공식 배포물 checksum을 확인한 전용 SDK 설치 → 같은 입력·설정의 Windows/WSL 동일 SDK 임시 재대조 → 원본 적용** 순서를 감독한다. CP1은 설정·도구·문서 15파일과 Astra goal 기록만 커밋하고 실제 HEAD를 blocking reply로 회신한다. 다음 CP2에서 기존 수기 C#만 별도 공백 커밋으로 만든다. Astra Git checkpoint와 신규 Opus 독립 검증·PR별 사용자 병합 승인 경계는 그대로다.

P0-A 임시 formatter 실행과 관측 helper build, A의 승인 SDK 설치 완료 보고를 받았다. A의 제품 변경/자체 검사 결과는 진행 보고·CP1에서 확인하며, 원본 서식 적용·고정 digest·전수 증명·Opus 독립 판정·CI 실행·DLL 갱신·PR·병합의 완료를 아직 주장하지 않는다. Astra의 tracked 쓰기는 goal 기록뿐이고 제품 파일은 A Sol 소유다. Q-1B의 조건부 자체 분석기/위반 수 ratchet 결정은 로드맵에 유지하며 A 승인으로 B를 시작하지 않는다.
