# PR278 - 기사 Dash 이펙트 시트 새로 만들기 종료 Gardener

## 사전 맥락(최초 메모)

- 작성 시각: 시계 출력 `Fri, Oct 30, 2026  5:43:55 AM`(`date -u`) / `Fri, Oct 30, 2026  2:43:55 PM`(`TZ=KST-9 date`). 이 절이 이 세션의 첫 파일 쓰기다. 그 전 쓰기 시도는 없다.
- 역할: 신규 읽기 전용 점검자 `[Effect Gardener]`다. live preamble의 Task는 `task_1a209eb67ce8`, Dispatch는 `ctx_d7397c2a39b9`, coordinator는 `term_26de49e7-5ee3-4df7-a809-4e0ac25e8ce9`다. 첫 check 응답의 Run은 `run_fcbc2441a227`이다. 첫 heartbeat는 `msg_28d00185dcf3`이다.
- 자기 handle `term_6c08e099-b68b-46de-9f1d-d5ddf06a7992`는 `$ORCA_TERMINAL_HANDLE` 이름 지정 조회 값과 preamble의 `--from` 값이 같다.
- 모델: 요청 모델은 계약 6행의 `claude --model claude-opus-5-5 --effort xhigh`다. 세션 자기 인식 모델 ID는 `claude-opus-5-5`다. 첫 화면 표시는 이 세션 안에서 볼 수 없어 `unknown`이다. backend 실제 모델도 `unknown`이다. launch 원시(`start-gardener-worker-01.json` 등)는 메모 시점에 열지 않았다.
- 기준 SHA: HEAD = 로컬 origin/main = `7bc758987e3816e0690f4df78f8160fbc3da2135`, branch `docs/knight-dash-effect-closeout-20261030`이다(`git rev-parse HEAD origin/main`, `git branch --show-current`). HEAD의 둘째 부모 `64c16a10…`는 Art PR279 쪽이다.
- 제품 PR278: merge `a9910064860ee81385074bb0319d7e8267f2e6ef`, 첫 부모 `dc66b77e6de836f243fde633a8ba81c3f5e93aa1`, 둘째 부모 `325777b70af718e40fbd09b4ff63f3df75ba0191`(계약의 head와 같음)다. commit 시각은 `2026-10-30T14:10:56+09:00`으로 계약의 `05:10:56Z`와 같다. `git merge-base --is-ancestor a9910064 HEAD`는 성공이다.
- 제품 diff(`git diff --stat dc66b77e a9910064`): CURRENT.md +2, goal.md +270, `Knight_Dash_Skill_Effect.png` 466085→867066 bytes, 3파일이다.
- 리드 로컬 변경: `git status --short` 실측은 BACKLOG.md·CURRENT.md·goal.md 세 파일 M이다. 계약 13행은 goal·CURRENT 둘로 적었다. BACKLOG는 리드가 계약 뒤 고치기 시작한 것으로 보이며 판정 근거로 쓰지 않는다. 고정 복사본만 읽는다.
- 고정 입력
  - 계약 `lead/closeout-01/gardener-contract-v1.md`의 SHA256은 `f70d68d14e9404b6f0815917cfd8ba1726fd23a6a7743a7cc220ef6d19f008c9`, 10472 bytes다(`sha256sum`·`wc -c` 실측). 포인터의 기대값과 같다.
  - `gardener-inputs-v1.txt` SHA256 `cf6d85d0…c0f8`, `gardener-rules-v1.md` SHA256 `20550544…64ee`는 계약 기재값과 같다.
  - 57파일을 `sha256sum`·`wc -c`로 다시 쟀다. 측정한 57개 「sha256 두 칸 bytes」 쌍을 alternation 정규식으로 묶어 Grep 도구 count로 입력 목록에 대조했고 57줄이 일치했다. 불일치 0이다.
- 읽은 절(메모 시점)
  - AGENTS·CLAUDE 전체(세션 주입).
  - 계약 v1 전문 1~60행.
  - 규칙 원문 v1 전문 1~104행(CODE_CONVENTION 4절, ORCA 「생존 신호와 초과 정산」·「목표 종료 Gardener 4주 파일럿」).
  - templates.md 1~44행(「맥락 메모」 양식).
  - 형식 참고 `01_Phases/goals/2026-10-24-character-select-create-ui/gardener.md`의 절 제목 목록과 1~38행(사전 맥락 형식).
  - ORCA.md 절 제목 목록(생존 신호 114행, Gardener 251행 위치 확인).
- 선택 원문: 계약에 실린 CODE_CONVENTION 「파일 위치와 이름」·「주석과 문서」·「역할별 적용」·「변화 평가」(교정 층·하네스 원칙·가독성)와 ORCA 「생존 신호와 초과 정산」·「목표 종료 Gardener 4주 파일럿」이다. 상태와 책임·Unity와 이름·빌드·언어별 절은 제외한다. 이 점검은 제품·시험·도구를 쓰거나 실행하지 않아서이며 계약 15행의 제외 이유와 같다.
- 기존 예시: character-select-create-ui `gardener.md`의 절 구성만 형식 참고로 쓴다. 그 결론과 후보(생존 신호 helper 미사용, Unity 실행 관문 helper)를 기대답으로 가져오지 않는다. 같은 주제가 이번 원천에서 나오면 반복 빈도 근거로만 센다.
- 재사용: 범주 표 형식과 「첫 발생·기존 후보 연결(후보 아님)」 구분을 예시에서 가져온다. 새 helper·스크립트는 만들지 않는다. 정본 helper 두 개(`99_Tools/Backlog/check-candidates.mjs`, `99_Tools/Orca/check-liveness.mjs`)만 읽기 전용으로 돌린다.
- 영향 파일: 이 보고서 한 파일이다. goal·CURRENT·BACKLOG·판정 원문·제품·시험·규칙·고정 복사본은 읽기만 한다.
- 배치와 이름: 계약 31·42행이 지정한 `lead/closeout-01/gardener/context-gardener-report.md`다. 폴더는 리드가 미리 만들었다. 제품 이름 금지 규칙은 해당 없다.
- 권한·보존
  - 저장소 추적 파일·다른 근거 파일·고정 복사본 쓰기는 0이다.
  - Unity·Hub·MCP·dotnet·서버·DB·WSL·웹·gh·Python 실행은 0이다. git 읽기·`sha256sum`·`wc`·`date`와 정본 helper 두 개, Read·Grep·Glob만 쓴다.
  - commit·push·branch 전환·stash·PR 조작·추가 위임은 0이다.
  - 인라인 분석(`node -e`, PowerShell 식)은 쓰지 않는다. 우편함 명령은 파이프·리디렉션 없이 단독 호출하고 출력은 파일로 남기지 않는다.
  - 현재 판정(독립 검증 PASS, 사용자 플레이 괜찮음)은 인용만 하고 바꾸지 않는다.
- 점검 계획
  - 고정 복사본 goal 전문 → 독립 판정·리드 수용 기록 → 단계 report의 「계획과 달라진 점」 → PR 본문·checks → 제품 diff·Unity 로그의 경고 → play-01 원시 → liveness 원시와 helper 재실행 → 후보 도착 검사 → 다른 goal·BACKLOG의 반복 조회 순서다.
  - 다섯 범주(독립 결함·CI 실패·새 경고 억제/설정 완화·임시 우회·드리프트/규칙 검사)마다 발생 수·원천 행·처분을 센다. 같은 사례를 두 범주에 겹쳐 세지 않는다.
- 질문: 현재 없음.
- 메모 뒤 갱신(본문 작성 시점 `Fri, Oct 30, 2026  5:55:13 AM` UTC / `2:55:13 PM` KST): 계획대로 진행했다. 바뀐 점은 셸 읽기 명령 사용이다. 계약 44행의 허용 실행 목록 밖의 읽기 전용 셸 명령(`ls`·`cat`·`sed -n`·`grep`·`cut`·`head`·`echo`·`cd`)을 썼다. 상태를 바꾸는 명령은 없다. 전부 「실행 기록」에 적었고 「자기 쓰기 감사」에서 이탈로 센다.

## 결론

- 차단 관찰 0, 리드 반환 3(GR-01~GR-03, 모두 비차단), 정리 후보 2다. 현재 판정(독립 검증 PASS, 사용자 플레이 괜찮음)을 바꿀 근거는 찾지 못했다.
- 종료 기록 다섯 절과 CURRENT Effect 줄의 사실 63개를 원시와 대조했다. 값이 다른 것은 0이다. 저장 원시가 없는 사실은 1개다(GR-02).
- 후보 도착 검사는 exit 1이다. 「다음 계획 후보」의 한 줄에 BACKLOG ID가 없다(GR-01).
- 이번 goal의 이탈 두 가지가 기존 BACKLOG 행에 이미 있는데도 goal에는 「첫 발생」으로 적혔다(GR-03).
- 후보 1은 단계 정산 때 생존 신호 helper를 돌리지 않은 일이다. 다른 goal에 이어 두 번째 발생이다. 기존 행은 `heartbeat-interval-review`다.
- 후보 2는 Unity·Editor·서버 실행 관문 PowerShell 스크립트를 세션마다 새로 쓴 일이다. 같은 부류 함정이 세 번 났고, 그중 하나는 메모리를 10 GiB 넘게 잡았다. 기존 행은 `verification-harness-powershell-helper`·`unity-baseline-harness-reuse`다.
- 이 보고는 제안이다. 채택과 실제 구현 완료는 아니다(ORCA 「목표 종료 Gardener 4주 파일럿」 100행).

## 점검 범주별 결과

| 범주 | 발생 수 | 처분 상태 | 요약 |
|---|---|---|---|
| 독립 결함 | 제품 0, 절차 8 | 이미 처분됨 | 검증자 번호 #P1·#P2·#P3·#P4a·#P4b·#P5·#P6 일곱과 리드 대조로 나온 안건 7 하나다. 모두 메인 결정으로 비차단 기록이다. |
| CI 실패 | 0 | 해당 없음 | head `325777b7`에서 4/4 통과다. CI는 이 PR의 제품 PNG·Unity 가져오기·`.meta` 보존을 검사하지 않는다. |
| 새 경고 억제·설정 완화 | 0 | 해당 없음 | 제품 diff에 설정·분석기·억제 변경이 없다. 이 시트 가져오기 줄에 경고가 없다. |
| 임시 우회 | 7 | 이미 처분됨 5, 리드 반환 1, 정리 후보 연결 1(겹침 포함) | 아래 「발생 수와 원천」 임시 우회 표다. |
| 드리프트·규칙 검사 | 5 | 이미 처분됨 2, 리드 반환 2, 정리 후보 1 | Codex 자동 업데이트, MinimapRT 줄 끝, 생존 신호 정산, 후보 도착 검사, 첫 발생 분류다. |

- 검사 실행불가: GitHub의 현재 PR 본문과 CI 실행 상세는 `gh` 금지로 보지 못했다. 저장 원시만 읽었다. Unity·서버 재실행도 하지 않았다. 이것들은 위반으로도 통과로도 세지 않는다.

## 발생 수와 원천

### 독립 결함

| 항목 | 원천(파일:행) | 처분 |
|---|---|---|
| 제품 결함 0 | `verify-01/verdict.md:3`, `:26`, `:163` | 해당 없음 |
| #P1 저장하지 않은 인라인 node 3회 | `verify-01/verdict.md:22`·`:106`, `unity-01/report.md:170` | 메인 `msg_9359ede6590b` 1 A(goal 76행) |
| #P2 홈 Unity 공용 캐시·로그 쓰기 계약 누락 | `verify-01/verdict.md:23`·`:107`, `unity-01/report.md:163-167` | 메인 2 A(goal 77행) |
| #P3 계약 git 읽기 목록 밖 읽기(clear) | `verify-01/verdict.md:21`·`:108`, `clear-01/report.md:188` | 메인 `msg_99c0be48fa4c` 3 A(goal 78행) |
| #P4a session-guard 상태 파일 | `verify-01/verdict.md:20`·`:109`, `align-01/report.md:214` | 메인 4 A(goal 79행) |
| #P4b 첫 통신 출력 미저장·메모 전 읽기 | `verify-01/verdict.md:19`·`:110`, `apply-01/report.md:141-143` | 메인 4 A(goal 79행) |
| #P5 계약 git 읽기 목록 밖 읽기(align) | `verify-01/verdict.md:20`·`:111` | 메인 `msg_eb0ee60c5440` 5 A(goal 80행) |
| #P6 검증자 harness 밖 `Get-Content` 1회 | `verify-01/verdict.md:24`·`:112` | 메인 6 A(goal 81행) |
| 안건 7 검증자 harness 밖 `Get-Content` 1회(17:13:34Z) | `lead/verify-01-lead-acceptance-01.md:30`·`:39`, goal 82행·305행 | 메인 `msg_40f9b3da4ec4` 7 A |

### CI 실패

| 항목 | 원천 | 처분 |
|---|---|---|
| check 2, test 2 모두 SUCCESS, 마지막 끝 18:40:23Z | `lead/pr-checks-02.json:1`, goal 319행 | 해당 없음 |
| head 연결 | `git log` 상 `325777b7` commit 18:13:21Z(03:13:21+09:00), job 시작 18:13:38Z·18:13:39Z(`pr-checks-02.json:1`), `lead/pr-view-04.json`·`pr-view-05.json`의 `headRefOid` `325777b7…` | 추론(아래 설계 관찰 O2) |
| CI가 검사하는 것 | `.github/workflows/code-rules.yml:20`·`:24`·`:138-145`(Changed 범위 규칙 검사), `:169-259`(도구 회귀 node 시험), `module-boundaries.yml:11`, `architecture-tests.yml:11`, `dotnet-tests.yml:12`·`:60`(dotnet test) | — |
| CI가 검사하지 않는 것 | 네 workflow에 Unity·PNG·`.meta` 검사가 없다(`git grep -i unity` 0건). Backlog 회귀(`code-rules.yml:247`)는 도구 자체 시험이고 goal 후보를 검사하지 않는다 | 해당 없음. PR 본문 `lead/pr-body-01.md:29`도 CI를 근거로 쓰지 않았다 |

### 새 경고 억제·설정 완화

| 항목 | 원천 | 처분 |
|---|---|---|
| 제품 diff 3파일: CURRENT M, goal A, PNG M | `git diff --name-status dc66b77e a9910064` | 해당 없음 |
| `.meta` blob 그대로 `fa69cf53…`(9516 B) | `git ls-tree -l dc66b77e`·`a9910064` Dash 폴더 | 해당 없음 |
| 이 시트 가져오기 줄에 경고 없음 | `unity-01/run-01.log:13627-13628`, `run-03.log:313-314`, `verify-01/editmode.log:13625-13626`, `verify-01/verdict.md:45`·`:153` | 해당 없음 |
| ProjectSettings 바뀌지 않음(SENTIS 줄 그대로) | `lead/play-01/play-01-restore-classify.json:11-13` | 해당 없음 |
| 기존 경고(이번 diff 밖): 클라이언트 C# CS8632·CS0618·CS8618, 서버 빌드 xUnit 경고 80개 | `lead/preimport-01/unity.log:1618-1640`, `lead/play-01/play-01-server-orca.json:46` | 해당 없음. 이번 PR이 만든 경고가 아니다 |

### 임시 우회

| 항목 | 원천 | 처분 |
|---|---|---|
| verify-01 한정 묶음 기준(상태 무변화 읽기의 비차단 기록) | goal 84-88행, 96행 | 이미 처분됨(메인 결정, verify-01 한정). 일반 기준 여부는 Rules 후보 |
| 홈 `TestResults.xml` 덮어쓰기 3회를 질문에 continue로 승인, 다음 계약은 허용 부작용으로 고정 | `unity-01/report.md:155-161`, goal 291행, `verify-01-contract-v1.md:65` | 이미 처분됨(리드 승인). 기존 보호 행과 연결 안 됨 → GR-03 |
| 서버 첫 기동 `cmd.exe /c` 실패 뒤 Orca 터미널로 재기동 | `lead/play-01/play-01-launch-server.json:2-16`, `play-01-server-orca.json:24-30`, goal 336-338행 | 이미 처분됨(메인 `msg_bc7db1abbc38`) |
| Editor 스크립트 1판 관문 오판 뒤 2판 | `lead/play-01/play-01-editor.json:9-13`, `play-01-editor-02.ps1:50-51`, goal 340-341행 | 이미 처분됨. 정리 후보 2의 근거 |
| Editor 스크립트 2판 PowerShell 메모리 폭주 뒤 사용자 taskkill | `lead/play-01/after-effect-play-state-01.json:9-19`, goal 348-351행 | 이미 처분됨(사용자 종료). 정리 후보 2의 근거 |
| 서버 닫기 1차 시도 중단 뒤 스크립트 제자리 수정·재실행 | `lead/play-01/server-stop-01.cjs:26-27`·`:59`·`:64`, `server-stop-01-interrupt.json:14-17`·`:53-55`, goal 366행 | 리드 반환 GR-02 |
| 생존 신호 입력 조립 임시 스크립트 | `lead/closeout-01/liveness-01.cjs`(4,672 B), `liveness-sources-01.json:3` | 정리 후보 1의 근거 |

- 프로세스 한정 `-ExecutionPolicy Bypass`(`play-01-editor-02.ps1:3`, `after-effect-play-state-01.json:19`)는 AGENTS 「공학 조건」의 허용 예외이고 실행 근거가 남아 있어 세지 않았다.

### 드리프트·규칙 검사

| 항목 | 원천 | 처분 |
|---|---|---|
| Codex 0.164.0 → 0.165.0 자동 업데이트 | goal 137-141행, 251행, `lead/codex-version-after-update-01.txt` | 이미 처분됨(사용자 결정 57 A) |
| MinimapRT 39줄 CRLF→LF drift, 보존본으로 복원 | `play-01-restore-classify.json:16-19`, `play-01-restore.cjs:52`, `play-01-restore-apply.json:125-147` | 이미 처분됨. 기존 행 `unity-sentis-define-drift`(BACKLOG 219행)·`unity-harness-user-file-restore-mark`(323행) |
| 생존 신호 단계 정산 helper 미실행, 손 기록은 초과 3구간 중 1구간 | goal 254행·371-376행, goal 283-310행(Unity·검증 절에 정산 줄 없음), `liveness-output-01.json:1` | 정리 후보 1 |
| 후보 도착 검사 실패 1건 | goal 220행, 아래 「실행 기록」 | 리드 반환 GR-01 |
| 첫 발생 분류가 BACKLOG와 어긋남 | goal 76-77행·94행·291-292행, BACKLOG 71행·439행 | 리드 반환 GR-03 |

- goal이 인용한 BACKLOG ID는 `saved-analysis-run-rule` 하나다(goal 95행). 고정 복사본 BACKLOG 71행에 있다. 해당 없음(통과)이며 위 다섯에 세지 않았다.

## 종료 기록 대조

다섯 절의 수치·sha256·경로·시각·msg ID를 전부 봤다. 표의 「일치」는 원시 값과 같다는 뜻이다.

### 재개 카드(goal 5-14행) — 12개, 불일치 0

| 사실 | 원시 | 결과 |
|---|---|---|
| merge `a9910064`, 05:10:56Z | `git log -1` 14:10:56+09:00, `lead/play-01/closeout-facts-play-01.json:507-516` | 일치 |
| Dash 네 항목 괜찮음 | `msg_eebc2c05914d`·`msg_5f9631da2f6c` 본문(`closeout-facts-play-01.json:32`·`:56`) | 일치 |
| 종료 순서, `msg_5f9631da2f6c` | 같은 본문 「3. 순서」 | 일치 |
| R-8 시점과 남은 PR의 인계, `msg_55345fc2fc09` | `lead/play-01/wait-play-06.json:14` 「2.」 | 일치 |
| `lead/scope-draft-v1.md` sha256 `79c9f0ea…ec7c` | `sha256sum` 실측 | 일치 |
| Run `run_fcbc2441a227`, 회신 주소 | 이 세션 첫 check의 runId, `wait-play-06.json:5`·`:12` | 일치 |
| 제품 branch head `325777b7`, 종료 branch, base `7bc75898` | `git rev-parse`, `git branch --show-current` | 일치 |
| 이미지 생성 3회 중 1회 | goal 253행·263행, `lead/pr-body-01.md:3` | 일치 |
| Unity Editor와 7777 서버 닫힘 | `play-01-restore-classify.json:4`(05:14:13Z Unity 0), `server-stop-01-close.json:7-12`·`:25-33` | 일치 |
| 서버 05:20Z, 잠금 풀림 | `server-stop-01-interrupt.json:2`·`:49-52` | 일치 |
| Library 3,088,006,164 B | `play-01-gate.json:19-23` | 일치 |
| Library 유지 메인 처분 | `msg_5f9631da2f6c` 「5.」 | 일치 |

### 이어받을 목표(goal 16-24행) — 7개, 불일치 0

| 사실 | 원시 | 결과 |
|---|---|---|
| 범위 초안 승인 방식, `msg_55345fc2fc09` | `wait-play-06.json:14` 「메인 해석과 운영 방식」 | 일치 |
| 65 A 초안 대조 통과, `msg_6d9e5309e129` | `closeout-facts-play-01.json:45-48` | 일치 |
| 초안 `goal-65a-draft/goal.md` sha256 `cafa4ea6…6adc` | `sha256sum` 실측 | 일치 |
| 사실 `facts-65a-01.json` sha256 `a3f92ab0…5156`, 저장 스크립트, 기준 HEAD `325777b7` | `sha256sum` 실측, `facts-65a-01.cjs` 있음, `facts-65a-01.json:3` | 일치 |
| 새 goal `2026-10-30-attack-key-left-ctrl`, branch, 등급 강 | `goal-65a-draft/goal.md:6`·`:102`·`:104` | 일치 |
| 개선안 3 절 링크 | goal 28행 제목에서 앵커를 수기로 만들어 대조 | 일치(렌더 확인은 안 함) |
| `lead/improve3-precheck/precheck.md` | 파일 있음(sha256 `a802df3c…8ae6`) | 일치 |

### 사용자 플레이 확인(goal 328-352행) — 21개, 불일치 0

| 사실 | 원시 | 결과 |
|---|---|---|
| 13:09 KST 재부팅, 같은 대화 재개, `msg_f8eef940387e` | `closeout-facts-play-01.json:5-8` | 일치 |
| READY `msg_9406b8882ec5` | `closeout-facts-play-01.json:68-72` | 일치 |
| 관문 04:20:03Z, Unity 0, dotnet 0, 22,447,353,856 B, 37,373,415,424 B | `lead/play-01/play-01-gate.json:3-7` | 일치 |
| HEAD=원격 `325777b7`, status 빈, Library 49,929개·3,088,006,164 B | `play-01-gate.json:12-23` | 일치 |
| 보존 sha256이 기준과 같음 | `play-01-preserve.json:7`·`:13`과 미리 가져오기 기준 `closeout-facts-play-01.json:438-444` | 일치(기준은 인용 파일 밖, O4) |
| 첫 서버 시도 5.1초, exit 1, 7777 없음 | `play-01-launch-server.json:2-16` | 일치 |
| 원인 미확인 | `server-fail-diag-02.json:9-13`(실행 공간 폴더 없음만 관측) | 일치 |
| `msg_cb566d881eef`, `msg_bc7db1abbc38` | `closeout-facts-play-01.json:21-24`·`:74-79` | 일치 |
| Orca 터미널 명령·제목 「7777 서버」 | `play-01-server-orca.json:16`·`:24` | 일치 |
| 44.3초, PID 1158 준비 줄 | `play-01-server-orca.json:27`·`:30` | 일치 |
| 1판 「7777 not listening」, Unity 안 띄움 | `play-01-editor.json:9-13` | 일치 |
| 1판 원인(한 원소 배열이 문자열로 풀림) | `play-01-editor.json:9`(배열이 아니라 문자열로 저장), `play-01-editor-02.ps1:50-51` | 일치 |
| 2판 04:33:25Z 시작, `-projectPath`만 | `after-effect-play-state-01.json:14-17`(PID 7988 생성 04:33:25.25Z), `play-01-editor-02.ps1:69` | 일치 |
| 2판 출력 파일 없음 | `after-effect-play-state-01.json:104`, `play-01-editor-02-run.txt` 0 B | 일치 |
| 사용자 원문 네 묶음 | `closeout-facts-play-01.json:32`·`:56` | 일치 |
| D1~D4의 네 항목 | 메인 본문 「D1~D4 괜찮음」, `lead/pr-body-01.md:33-36` 네 항목 | 내용 일치. D 번호 원천인 메인 통합 카드는 읽지 않았다 |
| PID 7988, 04:43:48Z, 10,849,169,408 B, 3,653,636,096 B | `after-effect-play-state-01.json:2`·`:9`·`:14-18` | 일치 |
| `msg_c73ff11a4bbc` | `closeout-facts-play-01.json:81-86` | 일치 |
| taskkill, 10.19 GiB, 1.14 GiB | `msg_5f9631da2f6c` 본문(메인 관측) | 일치(메인 관측으로 표기됨) |
| `ConvertTo-Json` 원인, 재현 안 함 | goal 351행이 리드 분석으로 표기 | 일치(추론 표기) |
| art-active Editor 04:50:42Z 종료 | `msg_5f9631da2f6c` 본문(메인 관측) | 일치 |

### 병합과 플레이 뒤 정리(goal 354-367행) — 15개, 불일치 0, 원시 없음 1

| 사실 | 원시 | 결과 |
|---|---|---|
| 승인 줄 원문 | `msg_5f9631da2f6c` 본문 | 일치 |
| merge 40자·시각·부모 `dc66b77e`·`325777b7` | `git rev-parse a9910064^1 a9910064^2`, `closeout-facts-play-01.json:507-516` | 일치 |
| 그 위 PR279 `7bc75898` | `git rev-parse HEAD`, `7bc75898^2` = `64c16a10…` | 일치 |
| 복원 05:14Z, Unity 0·dotnet 0 | `play-01-restore-classify.json:2-5` | 일치 |
| ProjectSettings 그대로 | `play-01-restore-classify.json:11-13` | 일치 |
| MinimapRT 39줄 모두 CRLF→LF | `play-01-restore-classify.json:18-19`, 판정 논리 `play-01-restore.cjs:52`(줄 끝 정규화 비교) | 일치. 원시 목록은 20줄까지만 담는다(`play-01-restore.cjs:60`) |
| 다른 추적 파일 변화 0 | `play-01-restore-classify.json:124` | 일치 |
| `286d3cc8…09d1`, HEAD, status 빈, PNG 일치 | `play-01-restore-apply.json:125-147` | 일치 |
| 원시·스크립트 경로, `msg_11c6b7df64fe` | 파일 있음, `closeout-facts-play-01.json:96-100` | 일치 |
| 서버 종료 메인 결정 `msg_55345fc2fc09` | `wait-play-06.json:14` 「1.」 | 일치 |
| Ctrl+C 1회, 3.4초, 「exit code 130」 줄 | `server-stop-01-interrupt.json:30-45`·`:71` | 일치 |
| 7777 수신 0, 잠금 free, GameServer.dll 0, 터미널 닫음 | `server-stop-01-close.json:7-12`·`:25-33` | 일치 |
| `msg_d120418a5b5e` | `lead/play-01/send-server-stop-01.json:6`·`:16` | 일치 |
| 첫 닫기 시도가 「서버 남음」으로 멈춤 | 저장 원시 없음 | GR-02 |
| Library 유지 | `play-01-gate.json:21-22`, `msg_5f9631da2f6c` 「5.」 | 일치 |

### 생존 신호 정산(goal 369-376행) — 8개, 불일치 0

| 사실 | 원시 | 결과 |
|---|---|---|
| 정본 helper·300초 | `liveness-run-01.json:4` | 일치 |
| 단계 정산은 330초만 손으로 적음 | goal 254행에만 있음. goal 283-294행·296-310행에 정산 줄 없음 | 일치 |
| 입력 69건 | `liveness-sources-01.json:3` | 일치 |
| 원시 경로, 출력 sha256 `3f3c61be…8a77`(goal 372행은 64자 전체) | `sha256sum` 실측. 같은 입력으로 helper를 다시 돌린 stdout의 sha256도 같다 | 일치 |
| Dispatch 일곱, 넷 300초 안, 초과 셋, 질문 대기 0 | `liveness-output-01.json:1` | 일치 |
| 시안 `ctx_79810e299e4a` 15:10:20Z→15:15:50Z 330초 | 같은 출력, Dispatch 대응 `lead/start-mockup-worker-01.json:7` | 일치 |
| Unity `ctx_ebad0e8f0741` 16:47:45Z→16:56:23Z 518초, batchmode 16:50:23Z~16:59:45Z, 반복 17:02Z | 같은 출력, `start-unity-worker-01.json:7`, `unity-01/report.md:62`·`:195` | 일치(문구는 O6) |
| 검증 `ctx_406ae4b0aa2e` 17:22:07Z→17:27:48Z 341초, 판정 157행 310초·396초 | 같은 출력, `start-verify-worker-01.json:7`, `verify-01/verdict.md:157` | 일치 |

### CURRENT Effect 줄

- 고정 복사본 `gardener-inputs/CURRENT.md:17`의 링크·경로·branch·상태 문구가 goal 재개 카드와 같다. 불일치 0이다.

## 리드 반환

모두 비차단이다. 현재 판정을 바꾸지 않는다.

### GR-01 — 「다음 계획 후보」 한 줄에 BACKLOG ID가 없다

- 원천: 고정 복사본 goal 220행(`DashSkill.prefab` 기본 sprite fileID `3027720894737066909`가 `.meta` 목록에 없음).
- 검사: `check-candidates.mjs`가 exit 1, `policy-violation`, 진단 `missing-reference` line 220을 냈다(「실행 기록」 원문).
- 고칠 것: BACKLOG 행을 만들고 그 줄에 ``BACKLOG `<id>` ``를 단다. 또는 다룰 기존 goal 링크를 단다.
- 경로 해석: 이 후보에는 goal 링크가 없다. 그래서 고정 복사본 위치 때문에 결과가 달라지는 경우가 아니다.
- 보충: goal 92-96행의 「규칙 후보」 넷은 「다음 계획 후보」 절 밖이라 도착 검사가 보지 않는다(O1). 그중 `saved-analysis-run-rule`만 BACKLOG ID가 있다.

### GR-02 — 서버 첫 닫기 시도의 원시가 없다

- 원천: goal 366행 「첫 닫기 시도는 `pgrep -af`가 자기 조회 줄을 잡아 「서버 남음」으로 멈췄다」.
- 관측: `server-stop-01.cjs`의 close 단계는 중단 때 예외를 던지고(59행) 성공 때만 파일을 쓴다(64행). 그래서 첫 시도의 출력은 저장되지 않았다.
- 관측: `server-stop-01-interrupt.json:14-17`·`:53-55`의 프로세스 목록에는 `pgrep` 자기 줄이 들어 있다. 지금 스크립트 27행은 그 줄을 뺀다. 저장된 스크립트(sha256 `c9915383…26f`)는 interrupt 원시를 만든 판이 아니라 그 뒤 제자리에서 고친 판이다(추론, 고치기 전 판은 없다).
- 고칠 것: goal에 「첫 닫기 시도 출력은 저장하지 않았다(대화 기록만), 간접 근거는 interrupt 원시의 `pgrep` 줄」과 「스크립트를 제자리에서 고쳤다」를 적는다.

### GR-03 — 「첫 발생」 분류가 기존 BACKLOG 행과 어긋난다

- (a) 인라인 `node -e`: goal 292행은 「계약 위반 첫 발생」이라 적었다. `lead/unity-01-lead-acceptance-01.md:30`도 「첫 발생으로 goal에 기록하고 새 규칙은 만들지 않는다」다. BACKLOG 71행 `saved-analysis-run-rule`은 같은 부류를 이미 다섯 번 기록했다. goal 95행은 이 행을 잇는다. 그래서 고칠 것은 292행의 분류 문구다(「이 goal 안의 첫 발생, BACKLOG 기준 여섯 번째」).
- (b) 홈 `TestResults.xml`: Unity 확인 계약 `unity-01-contract-v1.md`에 TestResults·LocalLow 문구가 없다(Grep 0건). 1회째 실행 전 값이 없다(`unity-01/report.md:155-161`의 표는 실행 뒤 값만 있다). BACKLOG 439행 `unity-testresults-protection-rule`은 10-24 첫 발생과 10-28/29 두 번째를 기록한 보호 규칙 행이다. 이번이 세 번째다. goal 94행의 규칙 후보는 「허용 부작용으로 적는다」이고 이 행을 잇지 않는다. 방향(보호 vs 허용)도 다르다.
- 고칠 것: goal 94행·291행에 `unity-testresults-protection-rule`을 잇고 방향 차이를 Rules·메인 판단 사항으로 적는다. 방향을 정하는 것은 이 점검의 권한 밖이다.

## 정리 후보

### 후보 1 — 단계 정산 때 생존 신호 helper 미실행과 손 기록 누락(기존 행 `heartbeat-interval-review` 갱신)

- 반복 빈도: helper가 있는데 단계 정산에서 돌리지 않은 일은 두 번째 발생이다.
  - 첫째: 캐릭터 선택·생성 goal `01_Phases/goals/2026-10-24-character-select-create-ui/goal.md:495`·`:532`(손 기록 8/14). BACKLOG 294행에 이미 관측으로 들어 있다.
  - 둘째: 이 goal 371행(손 기록 1/3).
  - helper가 생기기 전 손 기록 누락도 있었다. BACKLOG 234행 `worker-liveness-tool-check`(6/9, 10-08)다.
- 원시 근거
  - `liveness-output-01.json:1`의 초과 셋(330·518·341초) 가운데 단계 결과에 적힌 것은 goal 254행의 330초 하나다.
  - 종료 때도 리드는 helper 입력을 직접 만들지 못하고 임시 조립 스크립트 `liveness-01.cjs`(4,672 B)를 새로 썼다. 이 스크립트는 lead 폴더 JSON 가운데 `result.messages`가 있는 것만 골라 합치고 run 우편함을 더 읽는다.
  - helper는 이미 여러 파일을 id로 합친다(`99_Tools/README.md:78`). 다만 우편함이 아닌 JSON이 섞인 폴더를 그대로 받지 못한다는 것은 추론이다. 리드가 단계 정산 때 helper를 돌리지 않은 이유의 원시는 없다(미관측).
- 검사화 방법: 정본 helper 확장과 fixture 시험.
  - `99_Tools/Orca/check-liveness.mjs`에 저장 폴더 입력을 더한다. 메시지가 없는 JSON은 건너뛰고 건너뛴 수를 진단에 낸다.
  - 같은 CLI에 `--dispatch <ctx>` 걸러내기를 더한다. 그러면 단계 수용 때 그 Dispatch 하나를 명령 한 줄로 잰다.
  - `99_Tools/Orca.Tests/check-liveness.test.mjs`에 우편함 원시와 관문 JSON이 섞인 폴더 fixture를 둔다.
  - 문서 층 보완으로 리드 단계 수용 기록에 helper 출력 경로 칸을 둔다. 이것만으로는 앞 goal에서 막지 못했다.
- 기존 BACKLOG: `heartbeat-interval-review`(294행). 관련 `worker-liveness-tool-check`(234행), `liveness-helper-followup`(292행).

### 후보 2 — Unity·Editor·서버 실행 관문 PowerShell 스크립트의 세션별 재작성(기존 행 `verification-harness-powershell-helper`·`unity-baseline-harness-reuse` 갱신)

- 반복 빈도
  - 이 goal 안: 자원 관문(물리·commit 여유, Unity·dotnet 수)을 10파일에 따로 구현했다. 리드 8(ps1 7, cjs 1), 구현자 1(`unity-01/tools/common.ps1`), 검증자 1(`verify-01/tools/run-editmode.ps1`)이다(Grep `FreePhysicalMemory`).
  - 이 goal의 같은 부류 함정 셋
    1. `Start-Process -PassThru`의 handle을 잡지 않아 exit가 null로 남았다(`unity-01/report.md:154`).
    2. 한 줄 출력이 함수 반환에서 문자열로 풀려 관문이 「7777 not listening」으로 잘못 멈췄다(`play-01-editor.json:9-13`, `play-01-editor-02.ps1:50`).
    3. Editor 기동 뒤 기록 단계가 끝나지 않고 PowerShell이 작업 집합 10,849,169,408 B를 잡았다(`after-effect-play-state-01.json:14-19`, goal 349-351행). 원인이 `ConvertTo-Json`의 확장 속성 펼침이라는 것은 리드 추론이고 재현하지 않았다.
  - 앞선 goal: 저장·재기동 goal의 세션별 harness 함정 5부류 11건(BACKLOG 30행, 「`@()` 집계」 포함), 길드 클라이언트 UI goal의 배열 파이프 함정(`01_Phases/goals/2026-10-15-guild-client-ui/goal.md:282`), 캐릭터 선택·생성 goal의 관문 helper 세 벌(BACKLOG 225행 추가분). 배열 펼침 부류는 이번이 세 번째 goal이다.
- 원시 근거: 위 파일·행. 함정 3은 물리 여유를 메인 관측 1.14 GiB까지 낮췄고 사용자가 직접 프로세스를 껐다(goal 350행).
- 검사화 방법: 정본 helper와 시험.
  - `99_Tools/database/*.Common.ps1`과 `99_Tools/database/tests/*.Tests.ps1` 짝 관례를 따라 Unity 실행 관문 공용 모듈과 시험을 둔다. 위치와 이름은 코드 주인 파트와 CodeMap이 정한다(예: `99_Tools/Unity/`).
  - 시험 fixture 넷: 한 줄 `ss` 출력이 Count 1로 남는다, `Get-Content` 줄을 JSON으로 저장하면 평문 문자열이고 크기 상한 안이다, `Start-Process` 결과 exit가 null이 아니다, LocalLow `TestResults.xml`의 전후 sha256을 남긴다.
  - 세션은 이 모듈을 dot-source만 한다. 마지막 fixture가 GR-03 (b)의 보호 규칙을 도구 층으로 옮긴다.
- 기존 BACKLOG: `verification-harness-powershell-helper`(30행), `unity-baseline-harness-reuse`(225행). 관련 `unity-testresults-protection-rule`(439행), `unity-harness-user-file-restore-mark`(323행).

## 첫 발생·기존 후보 연결(후보 아님)

| 항목 | 원천 | 분류 |
|---|---|---|
| 인라인 `node -e` 3회(#P1) | `unity-01/report.md:170` | 기존 행 `saved-analysis-run-rule`(BACKLOG 71행) 여섯 번째. 분류 문구는 GR-03 (a) |
| 홈 `TestResults.xml` 전 값 누락 | `unity-01/report.md:155-161` | 기존 행 `unity-testresults-protection-rule`(439행) 세 번째. GR-03 (b), 후보 2에 묶음 |
| 홈 Unity 공용 캐시·로그 계약 누락(#P2) | `unity-01/report.md:163-167` | BACKLOG에서 `Caches`·`공용 캐시` 0건. 첫 발생. goal 94행 규칙 후보 |
| 계약 git 읽기 목록 밖 읽기(#P3·#P5) | `clear-01/report.md:188`, `verify-01/verdict.md:111` | BACKLOG에서 `rev-parse`·`branch --show-current` 0건. 이 goal 안에서 두 구현자. 앞선 흔적으로 `01_Phases/goals/2026-10-04-persistence-integration/goal.md:919`(「시작 확인 Git 명령을 담은 허용 목록」을 계약에 넣기로 함)가 있다(같은 마찰이라는 것은 추론). goal 93행 규칙 후보이며 BACKLOG 행은 없다 |
| 메모 전 첫 heartbeat와 메모 전 쓰기 금지 hook의 순서 | `compose-01/report.md:138`, `apply-01/report.md:141-144` | 이 goal 안 두 구현자. 정확히 같은 BACKLOG 행은 찾지 못했다. 관련 `mailbox-output-loss-hook`(190행) |
| session-guard 상태 파일(#P4a) | `align-01/report.md:214` | Git 제외 hook 동작. 앞 작업자 파일도 같은 폴더에 있다. 후보 아님 |
| 리드 자체 「위반 아님」 판단 | goal 91행, `lead/align-01-lead-acceptance-01.md:20`, `lead/clear-01-lead-acceptance-01.md:20` | 메인 교정으로 처분. 첫 발생 기록 |
| Codex 첫 화면 자동 업데이트 | goal 137-141행·251행 | BACKLOG에서 `0.165`·`Update now` 0건. 첫 발생. 사용자 결정 57 A |
| 서버 첫 기동 `cmd.exe /c` 무출력 실패 | `play-01-launch-server.json`, goal 336행 | 첫 발생. 메인 결정으로 Orca 터미널 사용 |
| `DashSkill.prefab` 기본 sprite fileID | goal 220행 | GR-01 |

## 설계 관찰과 대안

모두 비차단이다.

- O1 — goal 92-96행의 「규칙 후보」는 「다음 계획 후보」 절 밖이다. `check-candidates.mjs`는 그 절만 읽는다(`99_Tools/README.md:107`). 대안은 규칙 후보도 「다음 계획 후보」에 BACKLOG ID와 함께 두는 것이다.
- O2 — `lead/pr-checks-02.json`에는 head SHA가 없다. head 연결은 commit 시각과 job 시작 시각, 다른 파일의 `headRefOid`로 추론했다. 대안은 checks와 head를 한 호출로 저장하는 것이다.
- O3 — CI 네 job은 PNG·Unity·`.meta`를 보지 않는다. 약 등급 그림 PR의 근거는 로컬 Unity EditMode와 독립 검증뿐이다. 이번에는 둘 다 있었다. Unity CI 여부는 BACKLOG `unity-16x9-ui-regression`(251행)처럼 비용 판단이 걸린 별도 일이다.
- O4 — `play-01-preserve.json`에는 비교 기준 값이 없다. 기준은 미리 가져오기 원시에 있다. 대안은 보존 원시에 기준 sha256과 일치 여부를 함께 적는 것이다.
- O5 — `lead/verify-01-lead-acceptance-01.md:3`은 「절차 이탈 1~6 처분」이라 적었다. 지금 goal 제목은 1~7이다. 근거 폴더 파일이라 링크 검사 밖이다.
- O6 — goal 375행의 518초 구간은 16:47:45Z에 시작한다. batchmode 시작(16:50:23Z)보다 158초 앞선다. 「첫 batchmode 실행 중」은 구간 대부분에만 맞는다.
- O7 — `lead/pr-body-01.md:3`·`:15`는 head `90e248e8`과 커밋 셋을 적었다. 병합 head는 `325777b7`이고 커밋은 넷이다(`git log dc66b77e..325777b7`). 넷째 커밋은 goal·CURRENT 문서만 바꿨다(`git diff --stat 90e248e8 325777b7`). GitHub 본문이 갱신됐는지는 검사 실행불가다.
- O8 — 생존 신호 helper는 기동부터 첫 신호까지를 재지 않는다(`01_Phases/goals/2026-10-04-persistence-integration/goal.md:1280`에 같은 한계 기록). 메모 뒤로 첫 heartbeat를 미룬 구현자(`compose-01/report.md:138`)의 앞 구간은 이번 정산에 없다.

## 실제 준수

| 규칙 원문의 절 | 보고서에서 지킨 곳 |
|---|---|
| CODE_CONVENTION 「파일 위치와 이름」 | 「사전 맥락」 배치와 이름(계약 지정 경로, 새 파일은 이 보고서 하나). 후보 2의 위치는 기존 `99_Tools/database` 짝 관례를 예로 들고 결정은 주인 파트에 남김 |
| CODE_CONVENTION 「주석과 문서」 | 결과와 근거 경로만 적고 원문 로그를 옮기지 않음(종료 기록 대조 표) |
| CODE_CONVENTION 「역할별 적용」 | 검토했고 지적 없음(「일치」)·미검토(D 번호 원천, GitHub 본문)·해당 없음을 나눔. 문서·원시 정적 검토만 하고 Unity·서버·CI는 재실행하지 않았다고 「실행 기록과 미실행·한계」에 적음 |
| 「교정 층과 반복 규칙」 | 「첫 발생·기존 후보 연결」에서 첫 발생과 두 번째 이상을 나눔. 후보 둘은 두 번째 이상이고 문서보다 높은 층(helper·시험)을 고름. GR-03은 기존 승인 행을 첫 발생이라는 이유로 건너뛰지 않게 함 |
| 「하네스 원칙」 1·2·3 | 후보 1·2가 반복 규칙을 helper·시험으로 옮기는 안. 검사 실행불가를 위반·통과로 바꾸지 않음(「점검 범주별 결과」 끝) |
| 「사람의 탐색과 가독성」 | 실측 값과 추론을 나눠 적음(GR-02, 후보 1·2의 「추론」 표기). 서로 다른 조건의 수치를 같은 추세로 묶지 않음(손 기록 비율은 goal별로 따로) |
| ORCA 「생존 신호와 초과 정산」 | 리드 원시로 helper를 다시 돌려 같은 출력을 확인했고, 그것만으로 종료 기록 대조를 대신하지 않음. 초과 구간에서 추정 대기를 빼지 않음 |
| ORCA 「목표 종료 Gardener 4주 파일럿」 | 쓰기는 이 보고서 한 파일. 후보 최대 둘과 검사화 방법. 검사 실행불가와 실제 위반을 나눔. 직접 수정·규칙 채택·goal 발행 없음 |

## 추가로 읽은 것

계약의 고정 입력 57개 밖에서 읽은 것과 이유다.

- `.agents/skills/dawnholder-task-context/references/templates.md` 1-44행: 계약 31행이 맥락 메모 양식으로 지정.
- `lead/closeout-01/liveness-run-01.json`, `liveness-input-01.json`(helper 입력으로만): 생존 신호 helper 재실행.
- `lead/play-01/` 안 고정 목록 밖 원시: `play-01-gate.json`, `play-01-preserve.json`, `play-01-server-orca.json`, `play-01-editor-02-run.txt`, `play-01-restore-apply.json`, `play-01-restore.cjs`(52·60행 판정 논리), `closeout-facts-play-01.json`, `server-stop-01-interrupt.json`, `server-stop-01-close-console.txt`, `send-server-stop-01.json`, `wait-play-06.json`. 다섯 절의 사실을 대조하려고.
- `lead/start-*-worker-01.json` 일곱(Dispatch·Task ID만): 생존 신호 Dispatch와 단계를 이으려고.
- `lead/goal-65a-draft/goal.md`(6·102·104행)·`facts-65a-01.json`(3행), `lead/scope-draft-v1.md`·`improve3-precheck/precheck.md`·`resume-card-1030.md`·`user-play-card-01.md`(sha256과 D 번호 검색만): 이어받을 목표와 재개 카드 대조.
- `lead/pr-view-01~05.json`(`headRefOid`·`mergeStateStatus`만): CI head 연결.
- `lead/preimport-01/unity.log`(경고 검색), `unity-01/run-01.log`·`run-03.log`·`verify-01/editmode.log`(이 시트 줄 앞뒤): 경고 분류.
- `.github/workflows/*.yml`(기준 commit `7bc75898`, job 이름·실행 단계): CI가 검사하는 범위.
- `99_Tools/README.md` 69-80행·94-107행: helper 사용법.
- 반복 빈도 조회
  - 고정 복사본 BACKLOG 30·71·125·190·219·225·234·251·292·294·323·439행.
  - `01_Phases/goals/2026-10-24-character-select-create-ui/goal.md` 495·532행, 같은 폴더 `gardener.md` 44·68-70·101-121행(반복 근거로만).
  - `01_Phases/goals/2026-10-15-guild-client-ui/goal.md` 282행.
  - `01_Phases/goals/2026-10-04-persistence-integration/goal.md` 914-920행, 1280행(Grep 결과 줄).
  - `01_Phases/goals/*/goal.md` 전체 Grep(`check-liveness`·`손 기록`, `@()`·`ConvertTo-Json`·`PassThru`·`ExitCode`).
- 읽지 않은 것: 메인 통합 플레이 카드(`C:/Dev/DawnHolder_Dashboard/...`, 작업 경로 밖), 작업 트리의 goal·CURRENT·BACKLOG 원본(계약 11행), 그림 파일 전부(계약 48행).

## 실행 기록과 미실행·한계

작업 경로는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/effect-active`다. `V=.backups/verification/2026-10-29-knight-dash-effect-sheet`로 줄여 적는다.

1. `sha256sum $V/lead/closeout-01/gardener-contract-v1.md`, `wc -c` 같은 파일, `ls -la $V/lead/closeout-01/` → `f70d68d1…08c9`, 10472.
2. `sha256sum $V/lead/closeout-01/gardener-inputs-v1.txt $V/lead/closeout-01/gardener-rules-v1.md && cat $V/lead/closeout-01/gardener-inputs-v1.txt`.
3. `sha256sum <57개 경로>`, `wc -c <57개 경로>`(경로는 `gardener-inputs-v1.txt` 6-62행 순서). 측정한 57개 「sha256 두 칸 bytes」를 alternation으로 묶은 Grep 도구 count → 57.
4. `git rev-parse HEAD origin/main && git branch --show-current && git status --short && date -u && TZ=KST-9 date && echo "TERM=$ORCA_TERMINAL_HANDLE"`.
5. `git rev-parse a9910064 a9910064^1 a9910064^2 && git log -1 --format='%H %cI %s' a9910064 && git diff --stat dc66b77e a9910064 && git merge-base --is-ancestor a9910064 HEAD && git rev-parse 7bc75898^1 7bc75898^2`.
6. `ls lead/ lead/play-01/ lead/goal-65a-draft/ lead/improve3-precheck/ lead/preimport-01/ unity-01/ verify-01/`($V 안).
7. `cat lead/closeout-01/liveness-output-01.json lead/closeout-01/liveness-run-01.json lead/closeout-01/liveness-01.cjs`($V 안).
8. 생존 신호 helper 재실행: `node 99_Tools/Orca/check-liveness.mjs $V/lead/closeout-01/liveness-input-01.json --threshold-seconds 300` → exit 1, stdout은 `liveness-output-01.json`과 같은 내용. 같은 명령 `| sha256sum` → `3f3c61be213844a7c0c60f202b1ad4d59cb0146b16eee74b2b0e875969ef8a77`.
9. `sed -n '60,64p;193,197p' unity-01/report.md; sed -n '155,159p' verify-01/verdict.md`($V 안).
10. `for f in start-*-worker-01.json; do grep -o '"dispatchId"…' $f; done`(출력 없음) 뒤 Grep 도구 `ctx_[0-9a-f]{12}|task_[0-9a-f]{12}` on `lead/start-*-worker-01.json`.
11. `sha256sum $V/lead/scope-draft-v1.md $V/lead/goal-65a-draft/goal.md $V/lead/goal-65a-draft/facts-65a-01.json $V/lead/goal-65a-draft/facts-65a-01.cjs $V/lead/improve3-precheck/precheck.md $V/lead/resume-card-1030.md $V/lead/user-play-card-01.md`.
12. `git log --format='%H %cI %s' dc66b77e..325777b7 && git diff --stat 90e248e8 325777b7`.
13. `git ls-files .github/workflows && git grep -n -E '^name:|^  [a-z_-]+:$|paths(-ignore)?:|^\s+- .(0[0-9]_|\*\*)|run: ' 7bc75898 -- .github/workflows`.
14. `git show 7bc75898:.github/workflows/code-rules.yml | sed -n '1,60p'`, `git grep -n -i -E 'unity|\.png|03_Client|Assets|markdown|\.md|link' 7bc75898 -- .github/workflows | sed -n '1,60p'`, `git show 7bc75898:.github/workflows/code-rules.yml | grep -n -E 'name: |run-rules|\.ps1|\.mjs|\.ts|Changed|scope' | sed -n '1,40p'`, `git show 7bc75898:.github/workflows/dotnet-tests.yml | sed -n '1,22p'`.
15. `sed -n '13627,13631p' unity-01/run-01.log; sed -n '313,317p' unity-01/run-03.log; sed -n '13625,13629p' verify-01/editmode.log; wc -l unity-01/run-0*.log verify-01/editmode.log lead/preimport-01/unity.log`($V 안).
16. `git diff --name-status dc66b77e a9910064 && git diff dc66b77e a9910064 -- 00_Document/operations/CURRENT.md && git ls-tree -l dc66b77e <Dash 폴더>/ && git ls-tree -l a9910064 <Dash 폴더>/`.
17. 후보 도착 검사: `node 99_Tools/Backlog/check-candidates.mjs --backlog $V/lead/closeout-01/gardener-inputs/BACKLOG.md --goal $V/lead/closeout-01/gardener-inputs/goal.md` → exit 1. stdout 원문:

    ```text
    {"status":"policy-violation","exitCode":1,"backlog":".backups/verification/2026-10-29-knight-dash-effect-sheet/lead/closeout-01/gardener-inputs/BACKLOG.md","goal":".backups/verification/2026-10-29-knight-dash-effect-sheet/lead/closeout-01/gardener-inputs/goal.md","counts":{"backlogIds":197,"duplicateIds":0,"candidates":1,"candidatesWithoutReference":1,"unknownIds":0},"diagnostics":[{"code":"missing-reference","path":".backups/verification/2026-10-29-knight-dash-effect-sheet/lead/closeout-01/gardener-inputs/goal.md","line":220,"message":"The candidate has neither a BACKLOG ID citation nor an existing goal link.","repair":"Add BACKLOG `<id>` or a link to an existing other goal.md within this candidate."}]}
    ```

18. BACKLOG 행 읽기: `sed -n "${n}p" $B | cut -c1-1600`(n = 71·225·294·323·439), `cut -c1-900`(n = 30·219·234·292), `cut -c1-700`(n = 125·190). `$B`는 고정 복사본 BACKLOG다. 294행 끝을 보려던 `rev`는 「command not found」로 실행되지 않았다.
19. `git ls-files 99_Tools | grep -i -E 'unity|liveness|Orca/' | head -30`, `git ls-files '99_Tools/*.ps1' '99_Tools/**/*.ps1' '99_Tools/*.psm1' '99_Tools/**/*.psm1' | sed -n '1,40p'`.
20. Grep 도구: `FreePhysicalMemory`(근거 폴더 `*.ps1`·`*.cjs` count), `TestResults|LocalLow|AppData|공용 캐시`(`*-contract-v1.md`), goal 전체 반복 조회, BACKLOG ID·문구 조회. Read 도구: 위 「추가로 읽은 것」.
21. 시각: `date -u`·`TZ=KST-9 date`를 05:43:31Z, 05:43:55Z, 05:55:13Z 등에 돌렸다. 메모와 본문의 시각은 그 출력이다.
22. 우편함: heartbeat `msg_28d00185dcf3`·`msg_94aa8a34e0f4`·`msg_c72d4233d9d1`·`msg_b30b4f890785`·`msg_d599b6117745`·`msg_b419a0f7adaf`(06:02Z), check(count 0). 파이프·리디렉션 없이 단독 호출했다. 05:55Z와 06:02Z 사이 간격은 약 7분으로 5분 주기를 넘었다(본문 작성 중).
23. 본문 작성 뒤 재확인(06:02Z): Grep 도구 `3f3c61be` on 고정 goal(372행), Glob `lead/codex-version*`, `git grep -n -E 'Changed|run-rules|node --test|Backlog|dotnet test' 7bc75898 -- .github/workflows/code-rules.yml .github/workflows/dotnet-tests.yml && git grep -n -E '^name:' 7bc75898 -- .github/workflows`, `git show 7bc75898:<workflow> | sed -n '<행>'` 네 번. 이 재확인으로 표의 sha256 축약 오기 1개와 workflow 행 번호 1개를 고쳤다.

- 미실행: Unity·Hub·MCP, dotnet, 서버·DB·WSL, gh, Python, git 쓰기, 이미지 열기는 0이다. CI·Unity·서버 결과는 저장 원시만 읽었다.
- 한계: GitHub의 현재 PR 본문과 CI 상세, 메인 통합 플레이 카드, 작업 트리 원본은 읽지 않았다. 리드가 단계 정산 때 helper를 돌리지 않은 이유와 PowerShell 메모리 폭주의 원인은 원시가 없어 추론으로만 적었다.

## 자기 쓰기 감사

- 이 세션이 쓴 경로: 이 보고서 `.backups/verification/2026-10-29-knight-dash-effect-sheet/lead/closeout-01/gardener/context-gardener-report.md` 한 파일이다. Write 1회(사전 맥락), Edit 6회(본문 덧붙임 1, 재확인 뒤 고침 5)다.
- 도구 동작으로 남는 것: Claude Code가 TEMP `.backups/tmp/gardener01/` 아래에 남기는 임시 기록이다. 이 세션이 손으로 쓴 파일은 없다.
- 허용 밖 쓰기: 0이다. 확인 방법은 이 세션의 도구 호출 목록이다. Write·Edit 대상은 위 한 경로뿐이고, 셸 명령에는 리디렉션(`>`)·`tee`·`cp`·`mv`·`rm`·git 쓰기가 없다.
- 허용 밖 실행(쓰기 아님, 이탈로 셈): 계약 44행 허용 목록에 없는 읽기 전용 셸 명령 `ls`·`cat`·`sed -n`·`grep`·`cut`·`head`·`echo`·`cd`와 셸 `for` 반복 1회를 썼다(「실행 기록」 1·2·6·7·9·10·14·15·18·19). `rev`는 실행되지 않았다. 상태 변경은 없다. 이 출력에서 보고서로 옮긴 값은 같은 명령으로 다시 낼 수 있다.
