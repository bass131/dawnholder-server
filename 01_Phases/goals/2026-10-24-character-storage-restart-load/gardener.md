# PR272 - Core 캐릭터 저장의 서버 재기동·불러오기 종료 Gardener

## 사전 맥락(최초 메모)

- 작성 시각: 시계 출력 `2026-10-29T00:44:47Z` / `2026-10-29 09:44:47 KST`(직전 `date -u`·`TZ=KST-9 date`). 이 절이 이 세션의 첫 파일 쓰기다. 그 전 쓰기 시도는 없다.
- 역할: `[Core Gardener]`. live preamble의 Task `task_233a38afd1e4` / Dispatch `ctx_20f46a76ac5a`, coordinator `term_2ed01331-371a-428d-b13e-e84664ea33e1`, check 응답의 Run `run_58bc61a38687`. 자기 handle `term_56cec101-0b46-408c-bce9-c0f564fb57cb`은 `$ORCA_TERMINAL_HANDLE` 이름 지정 조회와 preamble 값이 같다. 요청 모델 `claude-opus-5-5`이고 세션 자기 인식도 같은 ID다. backend 실제 모델은 `unknown`이다. 최초 실행 명령은 계약 기재값(`claude --model claude-opus-5-5 --effort xhigh`)만 읽었고 launch 원시(`gardener-worker-start.json`·`gardener-first-screen.json`)는 열지 않았다.
- 기준 SHA: HEAD `0a7caad1a9a5f9d489acb30ec02aeee2a6352921`, 부모 = 로컬 origin/main `34756e01151db5d9f58254f7691f2fef2478415a`(PR272 merge commit), branch `docs/character-storage-restart-closeout-20261029`, `git status --short` 출력 없음. HEAD 커밋은 리드의 로컬 결과 기록이고 diff는 BACKLOG.md +1, CURRENT.md 1줄, goal.md +22/−4다(`git diff --stat 34756e01 HEAD`). 제품 고유 diff는 계약대로 `571d261c..4bee7ab9`로 계산한다.
- 고정 입력: `closeout-01/gardener-inputs.json` SHA256 `81205BA9…C5E9`를 `sha256sum`으로 실측해 일치했다. 25파일의 SHA256·bytes를 착수 직후 node로 재계산해 25/25 일치했다. 계약 본문 `closeout-01/gardener-contract-v1.md`는 18,344B, SHA256 `D4C12ED2…8A88D`를 기록만 한다(대조할 고정 hash는 계약에 없다).
- 읽은 절(메모 시점): AGENTS·CLAUDE 전체(세션 주입, hash는 고정 입력과 일치), 계약 전문(preamble), inputs.json, ORCA 231~256행(R-8·목표 종료 Gardener), 예시 `2026-10-17-character-storage-foundation/gardener.md` 전문, goal.md 1~125행(상태·적용 사용자 결정·메인 결정·재개 지점 v127)·1020~1323행(요구 원천·범위·완료조건·DB 경계·PR 경계·진척·HAR 기록·현재 보고와 남은 위험·다음 계획 후보·종료 점검 준비).
- 선택 원문: 계약에 실린 CODE_CONVENTION 「파일 위치와 이름」·「주석과 문서」·「역할별 적용」·「변화 평가」(교정 층·하네스 원칙·가독성), ORCA 「목표 종료 Gardener 4주 파일럿」. 상태와 책임·Unity·언어별·빌드 절은 제품·시험·도구를 쓰거나 실행하지 않아 제외한다(계약의 제외 이유와 같다).
- 기존 예시: 앞 저장 goal `gardener.md`의 「사전 맥락 → 결론 → 범주별 표 → 발생 수 → 종료 기록 대조 → 리드 반환 → 후보 → 첫 발생 → 설계 관찰 → 실제 준수 → 추가 읽기 → 실행/미실행 → 자기 쓰기 감사」 구조만 형식·배치 참고로 쓴다. 결론은 이 goal 원천으로 독립 작성하며 예시의 판단(PSSA 서식 후보 등)을 기대값으로 복제하지 않는다. 같은 BACKLOG 행에 닿으면 그 행의 추가 관측으로 적을지는 원시를 보고 정한다.
- 재사용: 범주 표 형식과 「첫 발생·기존 후보 연결(후보 아님)」 구분을 예시에서 가져온다. 새 helper·스크립트 파일은 만들지 않고 git·node·grep 한 줄 조회를 stdout으로만 읽는다.
- 영향 파일: 이 파일 한 개. goal·CURRENT·BACKLOG·판정 원문·제품·시험·규칙은 읽기만 한다. 리드가 완료본을 goal의 `gardener.md`로 bytes 그대로 복사한다.
- 배치와 이름: 계약이 지정한 `closeout-01/gardener/context-gardener-report.md`. 제품 이름 금지 규칙은 비해당이다.
- 권한·보존: 제품·시험·규칙·goal·CURRENT·BACKLOG 쓰기 0, 새 실행(SQL/DB/WSL/dotnet/Unity/서버/harness) 0, commit/push/추가 위임 0. 파일 읽기·git 읽기·hash·gh 읽기 조회만 한다. 우편함 명령은 파이프·리디렉션 없이 단독 호출한다. Core 강검증 PASS(`core-review/worker/verdict-v1.md`)와 CI 수정의 좁은 재검증 PASS(`indent-fix-01/verifier/verdict-v1.md`)의 범위를 구분하고, report·CI 숫자를 새 재실행으로 세지 않는다.
- 점검 계획: 다섯 범주(독립 결함·CI 실패·새 경고 억제/설정 완화·임시 우회·드리프트/규칙 검사)를 판정 원문(Core 강·indent 좁은·launcher-output 독립01~04·noReturn-review-02) → CI 원시(code-rules 로컬·CI results.json, dotnet 두 실패 로그)·main 받기 기록 → 제품 diff 파일 유형·키워드 조회 → goal 문구와 현재 main·판정 대조 순으로 본다. 큰 JSON·로그는 필드·count·grep만 읽는다. 반복 빈도는 goal·원시에 적힌 사례를 직접 세고 같은 사례를 두 번 세지 않는다.
- 질문: 현재 없음.

## 결론

- 다섯 범주에서 새 차단 관찰은 0건이다. 읽은 판정 7개에서 확정된 제품 결함도 0건이다.
- 비차단 리드 반환은 GR-01~GR-03 세 개다(아래 「리드 반환」).
- 정리 후보는 2개다.
  - 후보 1은 검증·실행용 자작 PowerShell harness가 같은 종류의 함정에 반복해 빠진 일이다. 새 행 후보다.
  - 후보 2는 판정의 비차단 후속이 「다음 계획 후보」·BACKLOG로 옮겨지지 않은 일이다. 기존 행 `candidate-check-excluded-section`의 추가 관측이다. 이번에는 옮겨지지 않은 위험(IV02-05)이 4일 뒤 계획 밖 실제 `wsl.exe` 9회로 나타났다.
- PR272 CI code-rules 실패는 기존 행 `prepush-ci-format-checks`의 추가 관측으로 적는다. 후보 수에는 넣지 않는다(「첫 발생·기존 후보 연결」).
- 두 PASS의 범위는 다르다.
  - Core 독립 강검증 PASS(`core-review/worker/verdict-v1.md` 3행)는 수정 전 bytes(launcher `F27EEC2D…04C6`)의 Core 할당 범위다. 직접 SQL·전체 DB 행 불변, 실제 WSL 강제 Retain, Content 화면은 미검증이다(같은 판정 178행).
  - CI 수정의 좁은 재검증 PASS(`indent-fix-01/verifier/verdict-v1.md` 3행)는 `f3e94759`의 7줄 서식 범위다. 이전 PASS를 그 범위·한계와 함께 새 bytes에 적용할 수 있다는 판정이며, 옛 실제 서버·DB 실행을 새 hash에서 다시 한 것은 아니다(같은 판정 74행).
- 이 보고는 제품 전체 검증, 사용자 승인, goal 최종 종료를 선언하지 않는다.

## 점검 범주별 결과

| 범주 | 관측 | 원천 | 검토 상태 |
|---|---|---|---|
| 독립 결함 | Core 강 PASS: CORE-V1-E1-01 fixture/환경으로 해소, H01 비차단 후속, OBS01 검증자 decoder 원문 유실, OBS02 SHA 표기 정정. 좁은 재검증 PASS: INDENT-OBS01 보고 단정 수준, INDENT-H01 시험 환경 후속, INDENT-OBS02 검증자 파생표 정정, INDENT-D01 설계 관찰. O3 독립01 INCONCLUSIVE(V-HARNESS-01), 독립02 INCONCLUSIVE·NOT_RUN(ENV-01 index.lock, IV02-01~06 경미·후보, VP-01 원시 삭제, VP-02 선보고 미이행), 독립03 IV03-H02·H03 harness 결함으로 E10·E11 판정 불가, 독립04 요구 단언 범위 PASS(새 제품 결함 0), noReturn 정적 사전검토 STATIC_PREFLIGHT_PASS(결함 0, 설계 관찰 4) | 각 판정의 머리와 기계 판독 표: Core 32~35행, 좁은 재검증 101~103행, 독립01 17행, 독립02 17~27행, 독립03 15~22행, 독립04 15~24행, noReturn 26·55~58행 | 판정 원문의 결론·결함 표를 읽었다. 결함 번호마다 원시를 전수 대조하지는 않았다. 확정 실패 3회 도달 기록은 읽은 범위에 없다. goal 379·684·772·777·794·905·938행은 모두 「집계 아님」 또는 「구분」으로 적는다. 후보 1·2로 연결 |
| CI 실패 | code-rules: head `6a2552ca`, Changed base `540172a4`, targets 4, violations 7(모두 `PSUseConsistentIndentation`), status FAIL. CI는 linux·node v22.23.4, startedAt 22:15:31.049Z, inputs.head `4411291f`. 로컬 재현은 win32·node v24.14.1, 22:19:43.444Z, head `6a2552ca`, 같은 7건이다. dotnet-tests: head `48f2fcc1`(goal 77행, 옛 base `540172a4`)에서 두 attempt 모두 Total 1952 / Passed 1922 / Failed 3 / Skipped 27이다. 실패 3건은 `GuildDebugTreasuryCheatTests`이고 오류는 `GameWorld는 단일 인스턴스만 허용`이다 | CI·로컬 `results.json`(입력 목록 SHA256 `186BC5BA…AF77E`·`A557E575…1C56A`), `log-failed.txt` 1238·1249·1260·17006~17009행, `log-failed-attempt2.txt` 1245·1254·1263·16976~16979행 | 저장 원시만 읽었다. CI 재실행은 0이다. CI inputs.head `4411291f`는 PR 병합 ref 체크아웃으로 보인다(추론, 미확인). dotnet 실패는 PR272 고유 diff에 C# 파일이 0이므로 PR272 제품 원인이 아니라고 본다. 세부는 「첫 발생·기존 후보 연결」 |
| 새 경고 억제·설정 완화 | 제품 diff `571d261c..4bee7ab9`는 7파일 +2912/−10이다. .md 밖 추가 행에서 `SuppressMessage`·`SilentlyContinue`·`pragma`·`NoWarn`·`Skip` hit은 0이다. `-Force`는 2줄이다(Output 시험 29행 reparse point 확인용 `Get-Item`, 425행 synthetic dummy `SecureString`). `bypass`는 Output 시험 668행의 guard 예외 문구 1줄이다. 규칙·설정 경로(CodeRules·.github·settings·psd1·.claude·.agents·AGENTS·CLAUDE) 변경은 0이다. `pssa-settings.psd1`은 입력 목록 hash와 같고 IncludeRules는 서식 두 규칙이다. 판정 조건 조정은 2건이다. S5 「같은 boot」를 통과 조건에서 기록 항목으로 내렸고(쓰이지 않음), 사용자 손 3분 기준의 시작점을 launched 첫 관측으로 정했다(S2·S4에 적용) | git 조회(아래 「실행 기록」), goal 102·103행, `pssa-settings.psd1` | 완화 0으로 판단한다. 판정 조건 조정 2건은 창 안 리드 계약 판단으로 기록·통지돼 있다(msg_57d88bae1378, msg_6c61adb44115). 그 타당성은 재판정하지 않았다 |
| 임시 우회 | 제품 코드 안 임시 우회는 0이다(키워드 조회 기준). 운영 우회는 셋이다. (1) Claude 세션의 Output suite는 `env -u NoDefaultCurrentDirectoryInExePath`로 돌렸고, 이후 검증자 계약에 「변수가 있으면 Output 미실행」 관문을 넣었다. (2) WorkRoot와 code-rules `--output`은 core-active `.backups` junction 대신 짧은 `C:/Dev/...` 표기를 쓴다. (3) 합성 XML 1파일 954B가 K10 unknown→skip 처분으로 남아 있다 | (1) 구현자 report 50행, 검증자 계약 59~61행, goal 88·1293행. (2) 구현자 계약 34행, 검증자 계약 39행, goal 110행, `code-rules-local-01.txt`. (3) goal 1161행 | (1)은 BACKLOG 26행이 해소 경로다. (2)는 BACKLOG 27·379행에 연결된다. (3)은 처분 기록대로 보존 상태이며 존재·hash는 확인하지 않았다 |
| 드리프트·규칙 검사 | core-active의 AGENTS.md·CLAUDE.md·`.claude/settings.json`·`.agents/`·`00_Document/conventions/`·ORCA.md·`99_Tools/CodeRules`는 origin/main과 차이 0이다. HEAD 커밋 추가 행의 상대 링크 3개는 모두 대상이 있다. 리드의 후보 도착 검사는 allowed·exit 0·candidates 5·without reference 0이다. 종료 기록의 로컬 code-rules는 PASS다(00:40:26Z 실행 폴더). main `34756e01`의 제품 4파일 blob SHA256은 `merge-main-03.txt`와 같다(launcher `D4957D6A…`, Sdk `2C679B03…`, Output `E1393E08…`, Framing `3072655A…`). gh 조회로 PR272는 MERGED, merge `34756e01`, head `4bee7ab9`, mergedAt 00:36:10Z다 | git 조회, node 링크 조회, `candidate-arrival-stdout.txt`(SHA256 `D03684FA…54B1`), `closeout-01/code-rules-stdout.txt`, `gh pr view 272` | 드리프트 0. 종료 기록 문구는 GR-01~GR-03 |

## 발생 수와 원천

| 항목 | 수 | 원천 | 실제 위반 vs 미실행 |
|---|---|---|---|
| 자작 PowerShell harness 함정 | 5부류 11건 | 후보 1 원천 | 위반 아님. 모두 exit≠0이나 INCONCLUSIVE로 드러났고 거짓 PASS는 관측하지 못했다 |
| O3 강 독립 판정 불성립 | 4회 중 3회(01 harness, 02 index.lock 환경, 03 harness) | 독립01 3행, 독립02 3·19행, 독립03 21~22행, 독립04 15행 | 실행 미성립. 04만 요구 단언 범위 PASS |
| code-rules CI 실패 | 1회, 진단 7건 | CI `results.json` | 실제 서식 위반 |
| PR272 push 전 로컬 code-rules 기록 | 0회 | 제품 커밋 `9992b1c9` 22:13:58Z·`6a2552ca` 22:14:13Z(git log), CI startedAt 22:15:31Z, 첫 로컬 시도 파일 mtime 22:17:53Z(KST 07:17:53) | 기록 기준 미실행. 「실행했지만 기록 안 함」을 배제할 원시는 없다 |
| dotnet-tests CI 실패 | head `48f2fcc1`에서 2회, 각 3건 | 위 두 로그 | PR272 원인 아님(추론) |
| 계획 밖 실제 `wsl.exe` | 1사건 9 child, 22:30:12Z~22:30:37Z | 구현자 report 73~80행 | 실제 발생. 정확한 경로·인과는 미확정(INDENT-OBS01) |
| 판정 비차단 후속의 후보 절 미도착 | 1처분 3항목 중 2항목 미도착, 1항목은 사건 뒤 등록 | 후보 2 원천 | 기계 기준 없음. 위반 판정하지 않는다 |
| 같은 산출물 수정 3회 초과 체크포인트 | 읽은 범위 4건 | goal 772·905·1274·1278행 | 규칙대로 알린 기록이다. 위반 아님 |
| 리드 실수 첫 발생 | 4건 | goal 104~107·112행 | 첫 발생 기록. 반복 규칙 없음 |

## 종료 기록 대조

- goal 5행 상태, 76행 병합 기록, 1212~1220행 진척(PR272 병합 [x], 종료 기록 병합 [>], 종료 점검 [ ])은 gh 조회(MERGED, merge `34756e01`, head `4bee7ab9`, mergedAt 00:36:10Z)와 맞는다.
- 완료조건 대조
  - 2·3·4는 Core 판정 10~12행의 실제 경로 PASS와 맞는다.
  - 5는 Core 판정 13행대로 「계약의 Core 관측 범위」다. goal 1292·1320행도 지정 캐릭터 밖 DB 행 불변을 미대조로 적는다.
  - 6의 원문(goal 1178행)은 「PR 직전 신규 Opus 한 명」이다. 실제는 결정 4A에 따른 신규 Astra였고 goal 1319행이 이를 적는다.
  - Core 강 판정(22:08:11Z) 뒤에 CI 수정 커밋 `f3e94759`와 main 받기 `4bee7ab9`가 왔다. 그래서 강 판정이 최종 PR head bytes를 직접 실행한 것은 아니다. 좁은 재검증(72~74행)과 `merge-main-03.txt`의 제품 4파일 hash 불변이 그 사이를 잇는다. goal 1293행이 이 연결을 적는다.
- 범위 v2 절(1119행)의 「순서표 v2, 목표 45분, 한도 60분」은 결정 37 A(21행, 착석 약 50분·창 약 75분·한도 약 90분)와 순서표 v3(113행)로 대체됐다. 1118행이 「이 절이 다음만 바꾼다」고 적고 결정 절이 뒤 결정이라 따라 읽을 수는 있다. GR-02에 함께 적는다.
- 「현재 보고와 남은 위험」 1295~1306행은 execution-01·O3 당시 문장이다. 「(역사)」 표시는 1295행 하나뿐이다. 1303행 「이번 시도는 … failed다」와 1306행 「제품 결함 확정·제품 수정·PR/commit/push/merge는0이며 전체 goal을 닫지 않는다」가 현재형으로 남아 76·1292행의 병합 기록과 충돌해 읽힌다. GR-02다.
- 「종료 점검 준비」 1316~1322행에는 사용자 A(goal 1063행)의 「goal 종료 점검 전에 남은 복사본 Library를 한 번 더 확인한다」 항목이 없다. Core 합동 창은 Core가 Unity를 실행하지 않았으므로 대상 0일 수 있다(Core 판정 106행, 추론). 확인 기록은 없다. GR-03이다.
- 「다음 계획 후보」 1308~1314행 다섯 항목은 BACKLOG 25·26·27·405행과 Content goal 링크로 모두 도착했다(후보 도착 검사 without reference 0). 다만 Main ② 처분의 IV02-04·IV02-06은 이 절에도 BACKLOG에도 없다. GR-01과 후보 2다.
- `gardener.md` 경로의 속성은 `eol: lf`, `text: set`이다. 이 보고서는 LF만 쓴다. 따라서 bytes 그대로 복사한 `gardener.md`의 blob 해시는 이 파일의 SHA256과 같아야 한다(추론, commit 전 미확인).

## 리드 반환

- GR-01, 비차단: 「다음 계획 후보」에 IV02-04와 IV02-06이 없다.
  - IV02-04(P9)는 launcher의 `$startInfo.FileName = 'wsl.exe'` 맨 이름 해석이다. 독립02는 「보안 결함 후보, 운영 영향 미확정」으로 적었다(23행). 이 줄은 main `34756e01`의 532행에 그대로 있다.
  - IV02-06은 launcher 허용 문장과 `Program.cs` 문장의 drift 시험 공백이다(25행).
  - Main msg_0c635b90e265 ②가 둘을 IV02-05와 함께 「현재 O3 비차단 후속」으로 두었다(goal 828행).
  - BACKLOG 26행의 출처 칸에 10-24 IV02-05 선행 처분을 연결할지도 같은 판단이다.
  - 추가할지, 제외 이유를 적을지는 리드 판단이다. 현재 차단 여부: 차단 아님.
- GR-02, 비차단: 「현재 보고와 남은 위험」 1296~1306행의 옛 시도 문장이 현재형이다. 「(역사)」 표시나 이동은 리드 판단이다. 범위 v2 절 1119행의 대체된 창 시간도 같은 성격이다. 현재 차단 여부: 차단 아님.
- GR-03, 비차단: 「종료 점검 준비」에 시험용 Unity 복사본 Library 재확인 줄이 없다. 대상이 0이면 0이라고 적으면 된다. 현재 차단 여부: 차단 아님.
- 새 차단 관찰: 없음.

## 정리 후보

### 후보 1 — 자작 PowerShell harness의 같은 종류 함정 반복(새 행 후보)

- 원천(모두 이 goal)
  - StrictMode에서 빈·0건 집합의 속성 접근 실패, 3건
    - JR-HAR-01: S00 근거 묶음 `Run-Stage.ps1`의 빈 unknownProcesses 배열(goal 1258~1262행).
    - P00-HAR-01: 정적 검사기의 빈 staticIssues 요약(goal 1254~1256행). goal이 스스로 「앞 JR-HAR-01과 빈 집계 처리의 같은 부류」라고 적었다.
    - 들여쓰기 구현자 attempt-1: 토큰 비교 harness `Compare-Tokens.ps1:75`에서 'Count' 속성 없음(report 85행, `implementer/attempt-1/token-compare/stderr.txt` SHA256 `5A28229F…F543AF`).
  - 자동·예약 변수 충돌, 2건
    - GP-HAR-01: 관측 helper의 `ShellId` 예약 변수(goal 1250~1252행).
    - INDENT-OBS02: 검증자 파생표가 pipeline에서 자동 변수 `$input`을 써서 세 파일을 잘못 분류(좁은 재검증 76행).
  - .NET 메서드 반환값의 pipeline 누출, 1건
    - V-HARNESS-01: `$state.AllText.Clear()`의 반환값이 결과에 섞여 사례 결과가 배열이 됐고 runner exit 2(독립01 17·49행, 독립02 18행).
  - native 출력의 한글 decode 훼손, 3건
    - S00-HAR-01·S00-HAR-02: observer·Capture의 JSON title 한글 decode 파손(goal 1222~1234행).
    - CORE-V1-OBS01: 검증자 D1 기본 decoder가 한글 진단을 훼손했고 원래 bytes는 유실(Core 판정 34행).
    - 위 attempt-1 stderr도 한글 오류 문구가 깨진 채 저장돼 있다(관측). 수에는 넣지 않았다.
  - 날짜형 자동 변환·UTC 비교, 2건
    - S00-HAR-03: `[datetime]` 캐스트의 로컬화 비교(goal 1236~1240행).
    - O3-B0-SF-01: JSON 날짜 타입·정확 UTC 비교(goal 1282~1284행).
  - 같은 계열이지만 수에서 뺀 것: IV03-H02·H03(독립03 21~22행)은 종료 시각 FILETIME 1601·handle 실패로 E10·E11을 무효로 만든 측정 harness 결함이다. 부류가 달라 위 11건에 넣지 않았다.
- 관측 횟수: 5부류 11건이다(관측). 실행자·준비 작업자·독립 검증자·구현자·리드 정적 실사의 다섯 역할에서 났다. 같은 세션의 중복 여부는 Task ID를 전수 대조하지 않아 미확인이다. BACKLOG에서 같은 행은 찾지 못했다. 인접 행은 `verification-readonly-bootstrap-bundle`(29행, Art 허용 cmdlet의 인코딩 누락)과 `worker-harness-only-execution`(285행)이다.
- 반복 규칙 문턱: goal 1256행은 P00-HAR-01을 JR-HAR-01과 같은 부류의 두 번째로 적고 「정본 반복 규칙·제품 변경으로 넓히지 않았다」고 처분했다. 그 뒤 10-28에 세 번째가 났다. CODE_CONVENTION 「교정 층과 반복 규칙」은 새 반복 규칙을 두 번째 발생부터 만든다고 한다. 이 처분이 그 문장에 어긋나는지는 Gardener 권한 밖이라 판정하지 않는다. 두 번째 발생 때 BACKLOG 후보 등록이 없었다는 사실만 적는다.
- 실제 위반 vs 미실행: 제품 규칙 위반이 아니다. 모두 자기 발견·원시 보존으로 처리됐다. 비용은 새 label 재관문, 새 세션, 강 판정 불성립이다. O3 강 독립 검증은 4회 중 04만 성립했다. 불성립 3회 중 두 번(01·03)이 검증자 harness 결함 때문이었다.
- 현재 범위 밖인 이유: 이 goal은 저장 재기동 실증이다. 공용 도구 추가는 「하지 않을 것」 1168행의 「새 검증 도구 체계」에 해당한다. Gardener는 채택 권한이 없다.
- 한 줄 대안·영향: 검증 계약이 쓰는 공용 PowerShell 근거 helper 하나를 정본으로 두고 계약이 그것을 지정한다. 원시 명령 기록, stdout/stderr bytes 보존과 명시 decode, `@()` 집계, `DateTimeOffset` UTC 파싱을 담는다. 세션마다 harness를 새로 짜며 같은 함정에 빠지는 비용을 줄인다. 단점은 helper 유지 비용과 PS 5.1·7 차이다.
- 검사화 방법
  - 현재 상태(관측): 근거 폴더 harness는 code-rules 대상 밖이다(DEVELOPMENT 90행의 `.backups` 제외). `pssa-settings.psd1`의 IncludeRules도 서식 두 규칙뿐이다.
  - lint: 검증 계약이 자기 harness에 PSSA를 따로 돌리는 선택 단계를 두는 방안이다. PSSA 기본 규칙 `PSAvoidAssignmentToAutomaticVariable`이 이번 `ShellId`·`$input` 사례를 잡는지는 미확인이다.
  - fixture: helper 자기 시험으로 0개·1개 집합의 Count, 한글 stderr bytes 왕복, `Z` ISO 문자열의 UTC 비교, 반환값 있는 .NET 메서드 호출 뒤 함수 반환 개수 1을 확인한다.
  - 정본 helper: 하네스 원칙 2에 해당한다. 같은 원칙의 「검증 없이 기존 helper를 정본으로 새로 지정하지 않는다」에 따라, 기존 세션 helper(`Invoke-Raw.ps1`, `Summarize-ReviewEvidence.ps1` 등) 중 하나를 고른다면 독립 검증 뒤 지정한다.
  - 승격: 하네스 원칙 4의 warning 파일럿 → 실측 → 사용자 판단이다.
- 담당 후보: Rules(검증 계약 양식·templates·정본 helper 위치)와 Core(발생처). 배정은 메인이다.

### 후보 2 — 판정의 비차단 후속이 후보 절·BACKLOG로 도착하지 않음(기존 행 `candidate-check-excluded-section` 추가 관측)

- 원천
  - 2026-10-24 독립02 판정 23~25행이 IV02-04(launcher 맨 이름 `wsl.exe`), IV02-05(Output 시험이 `NoDefaultCurrentDirectoryInExePath`를 지우지도 검사하지도 않음), IV02-06(허용 문장 drift 시험 공백)을 적었다.
  - Main msg_0c635b90e265(2026-10-24T21:41:27Z) ②가 셋을 「현재 O3 비차단 후속」으로 두었다(goal 828행).
  - 당시 BACKLOG에 세 항목의 행이 생기지 않았다. 현재 BACKLOG에서 `System32`·`P9`·`IV02`·`Program.cs` drift를 찾으면 관련 없는 50·246행만 걸린다.
  - 2026-10-28T22:30Z 구현자 attempt-2에서 child 9개가 가짜가 아닌 `wsl.exe`로 떴다(report 73~80행, goal 84~88행). 실패한 곳은 IV02-05가 짚은 전제와 같다. 단 IV02-04가 적은 해석 방향(현재 폴더 우선)과 이번 사건의 방향(변수로 현재 폴더를 건너뜀)은 반대다(관측).
  - 사건 뒤 BACKLOG 26행이 새로 생겼다. 출처 칸은 INDENT-H01·msg_d7f0a13b2aea만 적는다. IV02-04·IV02-06은 지금도 「다음 계획 후보」와 BACKLOG에 없다(GR-01).
  - 읽은 범위의 다른 비차단 후속(IV03-P01·P02·F01·D01, noReturn 설계 관찰 1~4)도 후보 절 밖이다. 이것들을 모두 등록해야 하는지는 기준이 없어 수에 넣지 않았다.
- 관측 횟수: 이 goal에서 1처분 3항목이다. 기존 행 본문에는 이전 관측 세 곳이 있다(병합 관문 goal의 누락 두 행, 후보 도착 검사 goal의 두 항목, 던전 종료 Gardener 후보2의 판정 후속 3 등). 이번이 goal 단위 네 번째 관측이다(행 본문 기준 계수).
- 실제 위반 vs 미실행: 판정 후속을 후보 절로 옮기라는 기계 기준은 아직 없다. 기존 행이 바로 그 공백이다. 그래서 규칙 위반으로 판정하지 않는다. 실제 영향은 계획 밖 실행 1사건이다. 넘어간 인자는 가짜 경로와 시험이 만든 가짜 자격 envelope였고, 실제 DB 자격 접근은 없었다고 본다(goal 85행). 「미리 등록했으면 막았다」는 추론이다. 등록은 수정 착수가 아니므로 예방 효과는 미확정이다.
- 현재 범위 밖인 이유: 후보 도착 검사기 변경은 규칙·도구 변경이라 이 goal 밖이다.
- 한 줄 대안·영향: 판정 양식 기계 판독 표의 비차단 후속 행에 「후속 위치(BACKLOG ID / 다음 계획 후보 / 처분 없음과 이유)」 칸을 둔다. 종료 기록 때 후보 도착 검사가 goal 안 「비차단 후속」 표기와 후보 절을 대조해 warning을 낸다. 리드의 손 대조를 줄인다. 단점은 표기 낱말에 기대는 검사의 거짓 경보다.
- 검사화 방법
  - 정본 helper: 기존 `99_Tools/Backlog/check-candidates.mjs`를 넓힌다(새 helper 아님).
  - fixture: 후속이 재개 지점·판정 처분에만 있는 goal은 warning, 후보 절에 있는 goal은 통과하는 두 사례다.
  - 양식: `.agents/skills/dawnholder-task-context/references/templates.md` 「검증 판정」의 칸 추가다. 기존 행이 이미 「템플릿 연결 칸/후보절 등록과 경고 fixture 방식」을 채택 전 제안으로 적고 있다.
  - 승격: 하네스 원칙 4의 warning 파일럿부터다.
- 담당 후보: 기존 행대로 Rules다. 이번 관측의 행 보충은 메인 경유다.

## 첫 발생·기존 후보 연결과 미실행 구분

### 기존 행 연결·첫 발생(후보 아님)

- PR272 code-rules CI 실패: 기존 행 `prepush-ci-format-checks`(BACKLOG 384행)의 추가 관측이다. PR226·PR242·PR259에 이은 네 번째 PR이다.
  - 기록상 push 전 로컬 code-rules는 0회다. 제품 커밋은 22:13:58Z, CI는 22:15:31Z에 시작했고, 첫 로컬 시도는 22:17:53Z였다(위 「발생 수」).
  - 실제 서식 위반 7건이다. 수정에 구현자·검증자 세션이 하나씩 더 열렸고, 그 수정 안에서 계획 밖 `wsl.exe` 사건이 났다(관측). 비용 전부가 서식 때문이라고 단정하지 않는다.
  - 종료 기록 커밋은 push 전 로컬 code-rules PASS(00:40:26Z)를 남겼다(관측).
- code-rules의 junction 출력 거부와 긴 junction 경로의 CS1619: 기존 행 `windows-local-checker-paths`(379행, SessionGuard junction 2건 포함)의 추가 관측이다.
  - 첫 로컬 시도는 `Links are not accepted at execution/output boundaries: …core-active\.backups`로 끝났다(`claude-lead-entry-01/code-rules-local-01.txt`).
  - CORE-V1-E1-01은 core-active `.backups` junction 표기의 253자 WorkRoot에서 csc CS1619였다(goal 110행). 이 건은 BACKLOG 27행에도 연결돼 있다.
  - 지금은 계약이 짧은 `C:/Dev/...` 표기로 우회한다.
- dotnet-tests 실패: World 소유 시험의 격리 문제로 보인다. 이 goal의 첫 관측이고 처리됐으므로 goal 77행 기록으로 충분하다.
  - 옛 base `540172a4`의 `GuildDebugTreasuryCheatTests.cs`에는 `[Collection("GameWorldRegistryTests")]`가 없다. `571d261c`의 11행에는 있다. 그 속성을 넣은 `00441ff4`는 `540172a4`의 조상이 아니다(git 조회).
  - gh 조회로 `540172a4`의 check-run `test`는 success(2026-10-28T16:51:01Z)였다. 같은 C#인 PR head는 두 번 같은 3건으로 실패했다. 그래서 시험 순서·병렬 실행에 기대는 실패로 추정한다(추론, 미확인).
  - BACKLOG에서 관련 행은 찾지 못했다.
- 이미 후보 절·BACKLOG에 있는 것: CORE-V1-H01(27행), INDENT-H01(26행), reader 취소 fixture(25행), 앞 goal 미실행 경로(405행), S6a 장면 ②(Content goal).
- 리드 실수 첫 발생 4건(goal 104~107·112행): 사용자 서버 카드의 자격 경로 안내 누락(S2 약 18분 지연), subject 태그 오기, 시간 어림 혼동, D1 문구다. 첫 발생이며 교정 정본에 따라 goal 기록으로 충분하다.
- 검증자 절차 이탈: VP-01 원시 삭제·VP-02 선보고 미이행(독립02 26~27행)은 Main ①에서 처분됐다(goal 828행). IV04-V01~V05(독립04 22행)는 판정 영향 없음으로 기록됐다. OBS02·INDENT-OBS02는 정정 완료다.
- 생존 신호: goal 772행이 300초 초과 1구간과 순수 heartbeat 4구간을 적는다. 기존 생존 신호 행(BACKLOG 226행 `worker-liveness-tool-check`)의 근거로만 연결할 수 있다. 새 후보로 세지 않는다.

### 미실행·한계

- 판정이 밝힌 미실행(재판정하지 않음): 직접 SQL·전체 DB 행 불변, 실제 WSL 강제 reader 취소·Retain, crash/kill·동시 writer, Resume Verify·Cleanup, Content의 Unity·HP·Town·사용자 화면, S6a 장면 ②, Core의 CI·새 빌드(Core 판정 178행). 좁은 재검증의 실제 WSL·서버·DB·Unity·빌드·원격 CI 미실행(같은 판정 118행).
- 이 점검이 하지 않은 것
  - SQL·DB·WSL·dotnet·Unity·서버·제품 시험·기존 harness 실행, CI 재실행.
  - 판정 raw 전수 열람과 결함 번호별 원시 대조. 합동 창 원시 `e2-s1`~`e2-s8`와 World 원시는 열지 않았다.
  - goal 전체(495,114B) 통독. 계약이 지정한 절과 grep 결과 행만 읽었다.
  - `4bee7ab9` 뒤 CI 4개 success의 저장 관측. goal 77행의 기록만 인용했다.
  - 종료 기록 code-rules 결과 JSON. stdout 한 줄만 읽었다.
  - 합성 XML 954B의 존재·hash 확인.
- 시각의 한계: push 전 로컬 실행 부재는 커밋 시각·CI 시작 시각·로컬 결과 파일 mtime의 비교다. push 시각 자체는 관측하지 않았다.

## 설계 관찰과 대안

- 근거 폴더의 harness는 규칙 검사 대상 밖이다. 그래서 같은 PowerShell 함정이 실행 창이나 판정 단계에서야 드러난다. 이 goal에서는 그 비용이 새 label 재관문, 새 세션, 강 판정 불성립으로 나타났다(관측). 후보 1의 정본 helper와 fixture가 가장 높은 층의 예방이다. 계약에 「함정 목록」 문장을 넣는 방안은 문서 층이라 그보다 낮다.
- 판정이 위험을 정적으로 짚어도 비차단 처분 뒤 추적 위치가 없으면 사라진다. 후보 2는 이 경로를 검사로 막는다.
- launcher 532행의 맨 이름 `wsl.exe`는 시험 쪽 사건과 같은 뿌리다. 시험 보호(BACKLOG 26행)만으로는 제품 실행 경로의 해석 의존(IV02-04)이 남는다(추론). 절대 경로 지정은 제품 변경이라 별도 goal·승인 대상이다. 이것은 GR-01의 판단 재료이며 새 후보로 세지 않았다.

## 실제 준수

| 계획(사전 맥락) | 실제 위치 |
|---|---|
| 첫 쓰기가 이 파일의 사전 맥락 | 00:44:47Z Write 1회로 「사전 맥락」 절 작성, 이후 같은 파일의 Edit로만 확장 |
| 입력 25파일 착수 직후 전수 대조 | 사전 맥락 「고정 입력」 항목(25/25 일치) |
| 범주 5개 실사 | 「점검 범주별 결과」 표 5행 |
| 반복 빈도 기준 후보 최대 2개 | 「정리 후보」 2개. 각 원천·횟수·위반/미실행·범위 밖 이유·대안·검사화·담당 |
| 기존 BACKLOG 행이면 추가 관측 | 후보 2와 「기존 행 연결」의 `prepush-ci-format-checks`·`windows-local-checker-paths` |
| 관측과 추론 분리 | 각 항목의 「추론」·「미확인」 표기 |
| Core 강 PASS와 좁은 재검증 PASS 범위 구분, report·CI 숫자 재실행으로 세지 않음 | 「결론」 다섯째 항목, 「종료 기록 대조」 완료조건 6 항목. 63/0/1과 CI 수치는 원시 인용만 했다 |
| goal 완료·미완료 표현과 main·판정 대조 | 「종료 기록 대조」, GR-01~GR-03 |
| 예시는 형식만 참고 | 절 구성만 사용. 후보·결론은 이 goal 원천으로 작성했다. 예시 후보 1과 같은 기존 행은 원시로 다시 확인한 뒤 후보 수 밖 추가 관측으로만 적었다 |

## 추가 읽기(25 입력 밖)와 이유

- `closeout-01/gardener-contract-v1.md`: hash와 크기만 쟀다.
- `closeout-01/candidate-arrival-command.txt`·`candidate-arrival-exit.txt`·`code-rules-stdout.txt`·`commit-msg-local-result.txt`: 입력 목록의 `candidate-arrival-stdout.txt`와 같은 종료 점검의 명령·exit·code-rules 결과를 확인했다.
- `claude-lead-entry-01/` 목록, `code-rules-local-01.txt`·`code-rules-local-02.txt`: goal 78행이 연결한 로컬 재현의 앞 시도와 push 전 실행 여부를 확인했다.
- `indent-fix-01/implementer/attempt-1/token-compare/stderr.txt`: 구현자 report 85행이 적은 harness 실패의 원문을 확인했다(후보 1의 세 번째 사례).
- goal 682~686·772·826~829행: 체크포인트 수와 Main ② 처분 원문을 확인했다.
- `00_Document/operations/BACKLOG.md` 1~24·25~29·254·379·384행과 grep 결과, `00_Document/operations/DEVELOPMENT.md` 80·90·96~99행: 기존 행과 code-rules 제외 범위를 확인했다.
- `.agents/skills/dawnholder-task-context/references/templates.md`·`99_Tools/Backlog/check-candidates.mjs`: 후보 2의 검사화 위치가 실제로 있는지만 확인했다.
- git 조회: `GuildDebugTreasuryCheatTests.cs`의 두 base 속성 줄, `00441ff4`·`540172a4` 이력, main `34756e01`의 제품 4파일 blob과 launcher 532·154행.
- gh 조회: `gh pr view 272`, `540172a4`의 check-run.

## 실행 기록

- 실행한 것: `date -u`·`TZ=KST-9 date`, `sha256sum`, node 한 줄 조회(입력 hash 대조, JSON 필드, 링크), `git --no-optional-locks`(rev-parse·log·diff·show·ls-tree·merge-base·check-attr), grep·sed·head·ls·stat·wc, `gh pr view 272`, `gh api …/commits/540172a4…/check-runs`. 결과는 stdout으로만 읽었다.
- 우편함
  - heartbeat: `msg_2b882933a7f2`, `msg_749ffa4a86d6`, `msg_f8e75558f818`, `msg_807847f29112`, `msg_c4716bc9f073`, `msg_49f452d8f058`, `msg_c7c22dd36213`, `msg_bb5a1230ec9b`, `msg_bdced68c65e1`를 단독 호출로 보냈다. 이 줄 작성 뒤의 heartbeat ID는 worker_done 본문에 적는다.
  - 단독 check: 착수 직후 1회(Run `run_58bc61a38687`, Dispatch `ctx_20f46a76ac5a`, count 0). 완료 직전 check 결과는 이 파일에 넣을 수 없어 worker_done 본문에 적는다.
  - 공식 ask: 0.
- 환경 사건: 한 git 조회에서 `> /dev/stdout |` 형태로 표준 출력을 다시 파이프에 넘겼다. 파일 쓰기는 생기지 않았다. 우편함 명령에는 리디렉션·파이프를 쓰지 않았다.

## 자기 쓰기 감사

- 이 세션이 수동으로 쓴 파일은 이 보고서 하나다.
  - 최초 Write 1회는 00:44:47Z 직후다. 이후 같은 파일의 Edit로만 확장·정정했다. 본문 세 조각을 붙였고, head 표기·행 번호·문장 정정을 같은 파일에 했다.
- 별도 context·raw·receipt·스크립트·TEMP 수동 파일·scratchpad·제품·시험·규칙·goal·CURRENT·BACKLOG 쓰기는 0이다.
- commit·push·추가 위임·도구 채택은 0이다.
- provider 자동 출력(Claude Code 대화 기록·도구 결과 저장)은 하네스 쓰기이며 내 수동 쓰기가 아니다.
- 최종 SHA256과 쓰기 종료 시각은 이 파일에 넣을 수 없어 worker_done 본문에 적는다.
