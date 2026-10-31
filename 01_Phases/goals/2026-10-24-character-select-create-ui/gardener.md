# PR273 - 캐릭터 선택·생성 화면 종료 Gardener

## 사전 맥락(최초 메모)

- 작성 시각: 시계 출력 `2026-10-29T00:05:59Z` / `2026-10-29 09:06:00 KST`(직전 `date -u`·`TZ=KST-9 date`). 이 절이 이 세션의 첫 파일 쓰기다. 그 전 쓰기 시도는 없다.
- 역할: `[Content Gardener]`다. live preamble의 Task는 `task_0da9134a10e9`, Dispatch는 `ctx_c4a5803589c1`, coordinator는 `term_21bb487f-a7ee-4d74-9892-2536b1d0350d`다. 첫 check 응답의 Run은 `run_5ff5a7b7de60`이다.
- 자기 handle `term_81b6e7ce-ef9f-49b4-a571-adf53a7d2b1a`는 `$ORCA_TERMINAL_HANDLE` 이름 지정 조회 값과 preamble 값이 같다.
- 모델: 요청 모델은 `claude-opus-5-5` xhigh이고 세션 자기 인식도 같은 ID다. backend 실제 모델은 `unknown`이다. 최초 실행 명령은 계약 6행 기재값만 읽었고 launch 원시(`gardener-worker-start.json` 등)는 열지 않았다.
- 기준 SHA: HEAD = 로컬 origin/main = `571d261c4d279de9a5955ee909f7205af637018c`이고 branch는 `docs/content-character-select-closeout-20261029`다. 첫 부모는 `8b6010d543c95837f504a225cdec5b9da9f854b5`(이전 main), 둘째 부모는 `63e9152a59e7a7acfa3b7b54a1ca6c4e47c8e406`(PR273 head)다. `git rev-parse HEAD^1 HEAD^2`로 확인했다. 제품 diff는 첫 부모→merge로 계산한다.
- 리드 로컬 변경: `git status --short` 실측은 goal.md 한 파일 M이다. 읽기 전용이다.
- 고정 입력
  - 계약 `closeout-20261029/gardener-contract-v1.md`의 SHA256은 `40c90ee11e8aac053aa6a236a813595180f752430a03ae93ab1bd23880b620ab`다(`sha256sum` 실측). 붙여 넣어 받은 계약 본문과 이 파일 1~20행을 대조했다.
  - `gardener-inputs.txt` 17파일의 SHA256·bytes를 착수 직후 node 한 줄로 다시 계산했다. 17/17 일치, 불일치 0이다.
  - 계약이 인용한 `00_Document/conventions/CODE_CONVENTION.md` 전체 SHA256 `2e233bda…20d0`도 실측 일치했다.
- 읽은 절(메모 시점)
  - AGENTS·CLAUDE 전체(세션 주입).
  - 계약 v1 전문(붙여 넣은 본문).
  - ORCA 113~122행(생존 신호와 초과 정산)과 231~256행(R-8·목표 종료 Gardener 4주 파일럿).
  - 예시 `01_Phases/goals/2026-10-17-character-storage-foundation/gardener.md` 전문(186행).
  - goal.md 1~494행 전문. 재개 카드, 적용 중인 사용자 결정, 만들 것, 건드릴 곳, 하지 않을 것, 완료조건, PR 경계, 진척, 단계 결과, 종료 기록을 포함한다.
- 선택 원문: 계약에 실린 CODE_CONVENTION 「파일 위치와 이름」·「주석과 문서」·「역할별 적용」·「변화 평가」(교정 층·하네스 원칙·가독성)와 ORCA 「목표 종료 Gardener 4주 파일럿」이다. 상태와 책임·Unity·언어별·빌드 절은 제품·시험·도구를 쓰거나 실행하지 않아 제외한다. 계약의 제외 이유와 같다.
- 기존 예시: storage-foundation `gardener.md`의 절 구성만 형식·배치 참고로 쓴다. 결론과 후보는 이 goal 원천으로 독립 작성한다. 예시의 후보(PowerShell 서식 검사, 서식 동등 판정 helper)를 기대답으로 가져오지 않는다.
- 재사용: 범주 표 형식과 「첫 발생·기존 후보 연결(후보 아님)」 구분을 예시에서 가져온다. 새 helper·스크립트는 만들지 않는다. git·gh·node·grep 단독 조회를 stdout으로만 읽는다.
- 영향 파일: 이 파일 한 개다. goal·CURRENT·BACKLOG·판정 원문·제품·시험·규칙은 읽기만 한다. 리드가 완료본을 goal의 `gardener.md`로 bytes 그대로 복사한다.
- 배치와 이름: 계약이 지정한 `closeout-20261029/gardener/context-gardener-report.md`다. 제품 이름 금지 규칙은 해당 없다.
- 권한·보존
  - 제품·시험·규칙·goal·CURRENT·BACKLOG 쓰기는 0이다.
  - Unity·dotnet·서버·DB·WSL·기존 harness 실행은 0이다. 파일 읽기, git·gh 읽기, hash만 한다.
  - commit·push·branch 전환·PR 조작·추가 위임은 0이다.
  - 우편함 명령은 파이프·리디렉션 없이 단독 호출하고 출력은 파일로 저장하지 않는다.
  - `MinimapRT.renderTexture`는 읽기만 한다. 사용자 자격 파일은 보지 않는다.
  - 최종 판정 보류, 확인된 제품 결함 0, U1·U2의 결정 44 A 남은 위험 상태를 그대로 인용만 한다. report와 CI의 숫자를 새 재실행으로 세지 않는다.
- 점검 계획
  - 다섯 범주(독립 결함·CI 실패·새 경고 억제/설정 완화·임시 우회·드리프트/규칙 검사)를 본다.
  - 순서는 독립 판정 1차·최종 → 구현 단계 report·S6a report·리드 검토 → heartbeat 정산 기록 → PR273 CI(`gh pr checks 273`, `gh run list --branch feat/content-character-select-create-20261024`) → 제품 diff 파일 유형·키워드 조회 → BACKLOG 기존 행 대조다.
  - 반복 빈도는 goal·원시에 적힌 사례를 직접 세고 같은 사례를 두 번 세지 않는다.
- 질문: 현재 없음.

## 결론

- 다섯 범주에서 새 차단 관찰은 0건이다.
- 리드 반환은 비차단 다섯 건(GR-01~GR-05)이다. 그중 GR-03은 리드·메인의 처분 판단이 필요하다.
- 정리 후보는 2개다. 둘 다 기존 BACKLOG 행에 근거를 더하는 형태다.
  - 후보 1은 생존 신호 정산을 정본 helper 없이 손으로 기록해 초과 구간이 빠진 반복이다. 기존 행 `heartbeat-interval-review`에 연결한다.
  - 후보 2는 Unity 실행 관문·사용자 프로필 관측을 세션마다 임시 helper로 다시 만든 반복이다. 기존 행 `unity-baseline-harness-reuse`에 연결한다.
- 현재 판정은 그대로다. 독립 검증 최종 판정은 보류이고, 확인된 제품 결함은 0이다. U1(장면 ② 이름 중복 거절 실제 화면 미실행)과 U2(S7 뒤 Content 캐릭터 저장 미관측)는 사용자 결정 44 A의 남은 위험이다.
- 이 보고는 제품 전체 검증, 사용자 승인, goal 최종 종료를 선언하지 않는다.

## 점검 범주별 결과

| 범주 | 관측 | 원천 | 검토 상태 |
|---|---|---|---|
| 독립 결함 | 1차 판정은 통과다. 제품 결함 번호는 0이고 차단 사유도 0이다. 최종 판정은 보류이며 U1·U2는 「제품 결함 확정이 아닌 검증 공백 번호」다. U1·U2를 같은 제품 결함의 확정 실패로 셀 근거는 없다고 검증자가 적었다 | `verify-astra-v1/verdict-stage1.md` 3·111행, `verdict-final.md` 3·26~29행 | 판정 원문 두 개를 전문 읽었다. 확정 실패 집계 대상은 0이다. 병합 전 1차 실행의 실패 2건은 구현자가 같은 창에서 새로 쓴 캡처 시험의 판정기 결함이고 제품 결함이 아니다(`impl-pre-merge-v1/report.md` 48~52행, 리드 검토 13~18행, 검증자 stage1 81~82행). 계약 줄 수 오기(+2,182 대 +2,312)는 O4로 정정됐다 |
| CI 실패 | PR273 branch의 CI 실행은 두 차례 8건이다. head `ed196555`(23:10:35Z)와 head `63e9152a`(23:11:26Z)에서 architecture-tests·module-boundaries·code-rules·dotnet-tests가 모두 success다. 실패는 0이다 | `gh pr checks 273`, `gh run list --branch feat/content-character-select-create-20261024 --json …`(00:1xZ 단독 조회) | CI 재실행은 하지 않았다. CI 범위의 한계는 「설계 관찰」에 적는다. `99_Tools/CodeRules/config.json` 17행이 `03_Client/`를 제외하고, workflow에는 Unity 실행이 없다. 따라서 이 PR의 03_Client 19파일은 CI 4종의 검사 대상이 아니다 |
| 새 경고 억제·설정 완화 | 제품 diff `8b6010d5..571d261c`는 21파일 +3115/−12다. 내역은 03_Client 19파일(.cs 10, .meta 9), goal.md, CURRENT.md다. 설정·빌드·검사 경로(`.editorconfig`, csproj, asmdef, ProjectSettings, workflow, CodeRules 설정) 변경은 0이다. 변경 .cs 10파일의 억제 키워드 조회 hit은 2줄이고 둘 다 억제가 아니다 | `git diff --stat 8b6010d5 571d261c`, `git grep -n -i -E 'pragma|SuppressMessage|\[Ignore|Assert\.Ignore|Skip|#if|NoWarn|TODO|HACK|FIXME|temporary|임시|workaround|우회|Obsolete|catch …|Bypass|\[Explicit' 571d261c -- <10파일>` | 완화 0으로 판단한다. hit 둘은 `CharacterMenuView.cs:11` summary의 「temporary input」(입력 중 상태 설명)과 `CharacterMenuPresenterTests.cs:270`의 LINQ `.Skip(states)`다. Unity 컴파일 경고는 구현 1단 「바꾼 파일 0줄」(`impl-post-merge-v2/report.md` 122행), 검증자 「CharacterMenu 변경 파일 C# warning 0」(stage1 92행)이다. 캡처 판정기의 기준 변경(줄 상자 → 글리프 상자)은 리드와 검증자가 「약화 아님」으로 판단했다. 이 보고는 그 판단을 다시 하지 않는다 |
| 임시 우회 | 제품의 임시 우회는 0이다. presenter의 `Debug.Log` 직접 호출(O1)과 「연결하는 중」 고착(O2, 새 timeout 금지)은 계약이 허용한 설계 선택·알려진 한계이며 goal 482·484행의 BACKLOG 계획에 있다. 운영 쪽 임시물은 `.backups/tmp/`의 helper(`cmi1/2/3/gate.cjs` 동일 SHA 3벌, `s6a/gate-s6a.cjs`, `s6a/hb-loop.cjs`, `cmi3/manifest*.cjs`)와 복사본 네 폴더다 | 구현 report 셋, `joint-s6a/report.md` 80행, `sha256sum`(아래 「발생 수」), `ls` | 복사본 `cmi1u`·`cmi2u`·`cmi3u`·`cv1u`는 `03_Client/Library`가 없고 나머지 파일이 남아 있다(`ls` 확인). goal 492행 기록과 맞는다. 임시 helper 반복은 후보 2로 연결한다 |
| 드리프트·규칙 검사 | HEAD = origin/main이라 규칙 파일(AGENTS·CLAUDE·`.claude/settings.json`·`.agents/`·conventions·ORCA)의 main 대비 차이는 0이다. 작업본 goal.md는 495행·CRLF 0이며 상대 파일 링크 1개·자기 anchor 6개가 모두 해석된다. 규칙 준수 쪽에서는 생존 신호 정산의 helper 미사용(ORCA 118행)과 10-24 Main 보호 결정의 10-28/29 계약 미반영이 관측된다 | `git diff --stat origin/main..HEAD` 빈 출력, node 링크 조회, ORCA 113~122행, goal 301~335행, lead 계약 다섯의 grep | 링크 드리프트 0이다. 생존 신호는 후보 1·GR-05, 보호 결정은 후보 2·GR-03으로 연결한다. CURRENT·BACKLOG·재개 카드의 낡은 문구는 GR-01·GR-02·GR-04다 |

## 발생 수와 원천

| 항목 | 수 | 원천 | 실제 위반 vs 미실행 |
|---|---|---|---|
| 제품 결함(독립 판정) | 0 | stage1 111행, final 124행 | 해당 없음 |
| 검증 공백 번호 | 2(U1·U2) | final 26~27행 | 미실행·미관측. 결정 44 A로 남은 위험 |
| CI 실패 | 0 / 8실행 | gh 조회 | 해당 없음 |
| 억제·완화 | 0 | git grep·diff stat | 해당 없음 |
| 생존 신호 300초 초과(정본 helper 의미로 손 집계) | 14구간 / Dispatch 5개. 병합 전 구현 5(396·332·697·335·374초), 1단 구현 4(464·396·338·343초), 2a 구현 1(308초), S6a 관측 1(338초), 검증 3(376·628·609초) | 리드 저장 우편함 JSON 301파일(`lead-opus-20261029/*.json`)을 node로 읽어 Dispatch별 `created_at` 간격을 셌다. 모든 메시지 유형을 신호로 치고, `to_handle`이 `dispatch:`인 후속은 빼고, 첫 worker_done까지 쟀다. 검증자의 공식 ask 대기 19:42:31Z→22:10:24Z는 질문 대기로 뺐다 | 관측된 운영 편차다. 손 집계라 helper 출력과 다를 수 있다(추론). check `--wait` 대기 구간도 섞여 있으며, ORCA 120행은 이를 자동 면제하지 않는다 |
| 같은 초과 중 리드 손 기록이 초과나 간격으로 표시한 것 | 8 / 14. 병합 전 1/5, 1단 3/4, 2a 0/1, S6a 1/1, 검증 3/3 | `lead-opus-20261029/impl-heartbeat-log.txt`·`impl2-…`·`impl2a-…`·`impl2b-…`·`verify-heartbeat-log.txt` | 손 기록은 heartbeat만 적고 status를 빠뜨린 Dispatch가 있다(병합 전 15:29:46Z status 등) |
| 같은 초과 중 goal 단계 결과에 적힌 것 | Dispatch 2개(S6a 338초, 검증 512초→status 뒤 376초) | goal 434·454행 | 병합 전·1단·2a 단계 결과에는 신호 정산 줄이 없다. 검증 628·609초는 손 기록과 verdict-final 117행에만 있다 |
| 정본 생존 신호 helper 실행 근거 | 0 | `check-liveness`·`liveness-policy` grep 0건(근거 폴더 전체) | 미실행. ORCA 118행은 리드가 이 helper로 집계하라고 적는다 |
| 자동 heartbeat 반복을 쓴 세션 | 1(S6a 관측자 `hb-loop.cjs`, 초과 1구간·나머지 300초) | `joint-s6a/report.md` 80~81행, `impl2b-heartbeat-log.txt` | 관측. 반복 시작 전 1구간만 넘었다 |
| 첫 heartbeat가 hook에 막히거나 출력 저장을 못 한 세션 | 2(1단 「heartbeat 01·02는 메모 전이라 출력을 저장하지 못했다」, 2a 「기동 직후 heartbeat 두 번은 hook이 막아 나가지 않았다」) | `impl-post-merge-v2/report.md` 161행, `impl-2a/report.md` 121행 | 2a의 첫 신호는 기동 18:28:33Z 뒤 18:32:54Z(261초)다. ORCA 116행 「기동 직후」와 어긋난다. 1단은 신호가 나갔고 receipt만 없다 |
| Unity 실행 직전 관문 helper 구현 | 10-28/29에 3개. `gate.cjs` 1벌을 3번 복사(`cmi1`·`cmi2`·`cmi3`, SHA256 `6cb413a9…7d82` 동일), `s6a/gate-s6a.cjs`(`0a532e49…d3e2`), 검증자 `run-editmode.ps1`(`aa4ee401…1d25d`) | `sha256sum` 실측, 구현 report(post 103행, 2a 72행), stage1 101행 | 위반 아님. 같은 책임의 임시 helper 반복이다(하네스 원칙 2) |
| Unity가 LocalLow `TestResults.xml`에 저장한 batchmode 실행 | 10-28/29에 5회(병합 전 1·2차, 1단, 2a, 검증). 10-24 v2E 1회(A-PROTECT-01)를 더하면 이 goal 6회 | 각 `editmode*.log`의 `Saving results to: C:/Users/bass1/AppData/LocalLow/DefaultCompany/03_Client\TestResults.xml`(pre `editmode.log` 250018행, `editmode-2.log` 224474행, post 274706행, 2a 278703행, verify 375778행), goal 294행 | 10-28/29의 before/after 관측은 미실행이다. 계약 다섯에 해당 관문이 없다(grep 0). 구현·검증 보고 넷이 「사용자 프로필 쓰기 미측정」을 공개했다(pre 72행, post 157행, 2a 143행, stage1 107행) |
| Unity의 GiCache 디렉터리 생성 로그 | 10-28/29에 6회(batchmode 5 + S6a Editor 1) | 같은 로그들의 `Created GICache directory at …/LocalLow/Unity/Caches/GiCache`, `joint-s6a/editor.log` 2999행 | 10-24 Main 정책의 4메타 관측은 10-28/29에 미실행이다 |
| Unity가 복사본 ProjectSettings define을 바꾼 실행 | 5(병합 전·1단·2a·검증은 바뀜 보고, 2a·검증은 `SENTIS_ANALYTICS_ENABLED` 제거 명시, S6a는 define 순서 변경)와 S6a의 MinimapRT 줄 끝 1 | pre 72행, post 156행, 2a 104~105행, stage1 104행, s6a 72행 | 원본 checkout은 바뀌지 않았다. 복사본 방식이 막았다. 기존 행 `unity-sentis-define-drift`(BACKLOG 208행)의 추가 관측이다 |

## 종료 기록 대조

- goal 7행 재개 카드와 471행 종료 기록의 병합 사실은 `gh pr view 273` 조회와 맞는다. state MERGED, head `63e9152a59e7a7acfa3b7b54a1ca6c4e47c8e406`, merge `571d261c4d279de9a5955ee909f7205af637018c`, mergedAt `2026-10-28T23:54:21Z`, draft false다. 「10-29 08:54 KST」도 같은 시각이다.
- goal 472행의 「merge tree = head tree」는 맞다. 두 tree는 모두 `de0c8988580816b137d57229ce157b9a5cf16228`이다.
- goal 472행의 「코드 경로에서 `803a1b6c`와 다른 것은 검증자 시험과 meta 둘뿐」도 맞다. `git diff --stat 803a1b6c 63e9152a -- 03_Client 02_Server 98_Shared 04_ClientNet 05_Management 99_Tools .github .claude .agents`는 그 두 파일만 낸다. 전체 diff의 나머지는 goal 둘(World goal 포함)과 CURRENT다.
- goal 258행 「CI 4개 success」는 head `63e9152a`의 4 workflow success와 맞다. 다만 그 4종은 03_Client를 검사하지 않는다(「설계 관찰」).
- goal 263행 「Content 21파일(제품 5·시험 5와 각 meta, goal·CURRENT)」은 diff stat 21파일과 맞다. CharacterSelectController는 기존 파일이라 meta 변경이 없어 meta는 9개다.
- goal 490행의 「1122 → 1421 → 1431」과 「독립 25/25, 1456/1456」은 각 report·판정 원문의 수치와 같다. 서로 다른 실행이라 합치지 않는다는 문장도 원문과 맞다.
- goal 251·252행 진척의 `[x]`는 단계 처리 완료를 뜻하고, 같은 줄이 「최종 판정 보류」「U1·U2 미관측은 남은 위험」을 함께 적는다. 판정 원문과 어긋나지 않는다.
- 작업본 goal.md는 LF만 쓴다(CRLF 0). 이 보고서도 LF만 쓴다. 그래서 bytes 그대로 복사한 `gardener.md`의 blob 해시는 이 파일의 SHA256과 같아야 한다(추론, commit 전 미확인).
- 낡은 문구는 GR-01·GR-02·GR-04로 아래에 둔다.

## 리드 반환

- 새 차단 관찰: 없음.
- GR-01, 비차단: CURRENT.md 7행의 Content 줄이 main에서 아직 「PR273 올림, 병합 승인 대기」와 branch `feat/content-character-select-create-20261024`를 적는다. 리드 로컬 변경은 goal.md 하나라 종료 기록 작업본에 CURRENT 갱신이 아직 없다. 현재 차단 여부: 차단 아님.
- GR-02, 비차단: goal 481~484행이 연결하겠다는 BACKLOG ID 넷(`character-menu-unobserved-real-screens`, `character-menu-connect-failure-state`, `character-menu-selecting-detail-text`, `character-menu-presenter-log-seam`)은 현재 BACKLOG에 없다(grep 0, BACKLOG 로컬 변경 없음). 같은 종류의 기존 행도 찾지 못했다(「연결하는 중」·`Debug.Log`·「실제 화면 미」 grep 0). 종료 기록 PR에서 행을 만들지 않으면 goal이 없는 ID를 가리킨다. 현재 차단 여부: 차단 아님.
- GR-03, 비차단·처분 판단 필요: 10-24 Main 결정 셋이 「B 보충부터」의 보호를 정했다. LocalLow `TestResults.xml` before 캡처·반환 대조(`msg_6d2d607a721a`, goal 307행), GiCache 4메타 관측(`msg_b62ee0d53624`, goal 321행), before를 B 진입 직전에 둠(`msg_e05dd4749c68`, goal 334행)이다. 10-28/29의 병합 전·1단·2a·S6a·검증 계약 다섯에는 이 셋이 없다(`TestResults|GiCache|LocalLow|v2G|v2H|A-PROTECT` grep 0). S6a 계약에는 PlayerPrefs before/after 읽기만 있다(`impl-2b-contract-v1.md` 26·40행). goal에 「대체됐다」는 기록도 없다. 같은 기간 batchmode 5회가 LocalLow `TestResults.xml`에 저장했다고 로그가 적는다. 「대체됨」으로 기록할지 「남은 위험」으로 기록할지는 리드·메인 판단이다. 현재 차단 여부: 제품 판정과 종료 기록 문장의 차단은 아니다. 10-24 Main 처분(after 보존, 삭제·복원 0, GiCache는 공유 파생 캐시로 변경 수용)과 결과가 같을 가능성이 높다(추론).
- GR-04, 비차단: 재개 카드 표 13행 「독립 판정은 아직이다」와 14행 「사용자 직접 플레이와 병합 승인은 아침에 묶어 요청한다」가 현재와 다르다. 지금은 독립 1차 통과·최종 보류이고, PR273은 병합됐고, 직접 플레이는 U1·U2 확인과 함께 다음으로 미뤄졌다. 현재 차단 여부: 차단 아님.
- GR-05, 비차단: goal 단계 결과의 신호 정산 줄은 S6a 관측자(338초)와 검증자(512초)뿐이다. 병합 전·1단·2a 단계에는 신호 정산 줄이 없다. 검증자 628·609초 구간도 goal에는 없다. 정본 helper 정산 원시도 없다(후보 1). 현재 차단 여부: 차단 아님.

## 정리 후보

### 후보 1 — 생존 신호 정산의 정본 helper 미사용과 손 기록 누락(기존 행 `heartbeat-interval-review` 갱신)

- 원천
  - 이 goal: 리드 저장 우편함 JSON 301파일, 손 기록 다섯(`lead-opus-20261029/*-heartbeat-log.txt`), goal 434·454행, `impl-post-merge-v2/report.md` 159~163행, `impl-2a/report.md` 110~122행, `verdict-final.md` 117행, `joint-s6a/report.md` 80~81행.
  - 규칙: ORCA 118행 「리드는 정산 때 기존 생존 신호 간격 helper로 저장한 원시를 집계하고 300초 초과는 구간·이유 한 줄로 기록한다」. helper는 `99_Tools/Orca/check-liveness.mjs`(README 69~82행)다.
  - 이전 관측: BACKLOG 223행 `worker-liveness-tool-check`는 helper가 생기기 전 「원시 9구간 중 리드 기록 6구간」을 적었다. 이 행은 helper를 만든 V1.x 1단계로 승격됐다. 열린 행은 283행 `heartbeat-interval-review`(대기, 「간격/자동화/helper 변경은 이 후보에 남는다」)다.
- 관측 횟수
  - 이 goal: helper 의미의 손 집계로 14구간·Dispatch 5개다. 손 기록이 표시한 것은 8, goal에 적힌 것은 Dispatch 2개다(관측).
  - helper 실행 근거는 0이다(관측).
  - 「리드 기록이 원시보다 초과를 적게 잡음」은 223행 이후 두 번째 goal 관측이다(관측). 다른 goal에서 helper가 실제로 쓰였는지는 열어 보지 않았다.
- 실제 위반 vs 미실행
  - helper 미실행은 ORCA 118행 절차와 어긋난다(관측). 고의 여부는 판단하지 않는다.
  - 14구간 자체는 운영 편차다. 그중 check `--wait` 대기와 겹친 구간이 있다(검증 628·609초, 병합 전 697초 「창 대기 중」 추정). ORCA 120행은 이를 자동 면제하지 않는다.
  - 보고 숨김은 없다. 구현자·검증자가 자기 report에 초과를 적은 Dispatch도 있다(1단 셋, 검증 512초).
- 현재 범위 밖인 이유: 이 goal은 캐릭터 화면 제품 goal이다. 「하지 않을 것」에 새 도구·harness 수리 루프가 있다(goal 183행). 생존 신호 간격·정산 방식은 Rules 소유 행이다. Gardener는 채택 권한이 없다.
- 한 줄 대안·영향
  - 리드가 종료 정산 때 저장 우편함 전체를 `node 99_Tools/Orca/check-liveness.mjs <파일들>`에 넣고, 출력 경로와 Dispatch별 초과 수를 goal 단계 결과에 한 줄씩 남긴다.
  - 영향: 손 기록 누락(이 goal 6구간 이상)이 사라지고 메인 R-2 표본 대조의 입력이 고정된다. 비용은 명령 한 번이다. 입력 JSON의 모양 오류로 helper가 전체를 막는 약점(BACKLOG 281행 O9)은 남는다.
  - 보조 대안: 작업자 heartbeat를 자동 반복으로 보내는 정본 helper다. S6a의 임시 `hb-loop.cjs` 세션만 초과가 1구간이었다(관측). 이는 283행의 「긴 명령 중 신호 방법」과 같은 질문이다.
- 검사화 방법
  - 정본 helper: 기존 `check-liveness.mjs`를 그대로 쓴다. 종료 점검 항목에 「helper 출력 경로」를 필수 칸으로 둔다.
  - fixture: 이번 goal 저장 우편함에서 Dispatch 다섯을 뽑아 helper 회귀 입력으로 삼는다. 기대값은 helper 코드가 아니라 독립 손 계산(이 보고의 14구간 표)에서 온다. 단, 이 손 계산은 helper 출력과 아직 대조하지 않았다.
  - 승격: 하네스 원칙 4에 따라 warning 파일럿 → 실측 → 사용자 판단이다.
- 담당 후보: 기존 행대로 Rules. 이번 근거의 행 갱신과 우선순위 판단은 메인 경유다.

### 후보 2 — Unity 실행 관문·사용자 프로필 관측의 세션별 임시 helper(기존 행 `unity-baseline-harness-reuse` 갱신)

- 원천
  - 이 goal 10-28/29
    - 관문: `gate.cjs` 1벌을 `cmi1`→`cmi2`→`cmi3`로 복사해 썼다(SHA256 셋 동일 `6cb413a9…7d82`, post report 103행 「cmi1 사본」, 2a report 72행 「cmi2 사본」). S6a는 `gate-s6a.cjs`, 검증자는 `run-editmode.ps1` 안의 관문을 따로 썼다.
    - 사용자 프로필: batchmode 5회 모두 LocalLow `TestResults.xml` 저장 줄이 있고, Editor 포함 6회 모두 GiCache 생성 줄이 있다(「발생 수」 표의 행 번호). 계약 다섯에는 그 관측 관문이 없다. 보고 넷이 미측정을 공개했다.
    - 복사본 목록: 2a의 `cmi3/manifest.cjs`·`manifest-diff.cjs`를 S6a가 그대로 다시 썼다(s6a report 80행).
  - 이 goal 10-24: A-PROTECT-01(goal 294행)과 Main 결정 셋(goal 307·321·334행)이 B의 보호를 정했다.
  - 이전 관측: BACKLOG 214행 `unity-baseline-harness-reuse`는 「같은 역할의 harness를 세션마다 새로 쓰거나 고쳤다(batch 실행 6개, 상태 기록 9개, 서버 lane 4개)」를 적는다(사용자 채택, 대기). 관련 행은 208행 `unity-sentis-define-drift`, 312행 `unity-harness-user-file-restore-mark`, 285행에서 승격된 공용 실행 잠금 goal(현재 보류, CURRENT 15행)이다.
- 관측 횟수
  - 관문 구현 3개(복사 3벌 포함)가 10-28/29에 있다. 10-24 단계의 별도 guard(goal 122·287행 「기존 자원 guard」)까지 치면 이 goal 4개 이상이다(관측과 추론).
  - LocalLow 저장은 이 goal 6회, 그중 관측된 것은 10-24의 1회뿐이다(관측).
- 실제 위반 vs 미실행
  - 관문 반복은 위반이 아니다. 같은 책임의 임시 helper 복제다.
  - 10-28/29의 TestResults·GiCache before/after는 미실행이다. 10-24 Main 결정이 아직 적용되는지는 goal에 기록이 없다(GR-03). 그래서 위반으로 확정하지 않는다.
  - 원본 checkout 보호는 복사본 방식으로 지켜졌다(원본 2,341파일 hash 변화 0, final 63행).
- 현재 범위 밖인 이유: goal 183행이 새 도구·harness 수리 루프를 금지한다. 공용 관문은 보류 중인 Rules 공용 실행 잠금 goal과 겹친다. 소유 조율이 먼저다.
- 한 줄 대안·영향
  - 추적되는 정본 Unity 실행 wrapper 하나를 둔다. 실행 전후 관문(Unity·dotnet 수, 물리·commit 여유), 복사본 목록 diff와 알려진 drift 분류(208행의 SENTIS define·MinimapRT 줄 끝), LocalLow `TestResults.xml`과 GiCache의 존재·파일 수·총 bytes·최신 mtime before/after를 한 출력 JSON에 낸다.
  - 영향: 계약마다 보호 문장을 다시 쓰지 않아도 10-24 Main 정책이 기본값이 된다. 임시 helper 복사가 사라진다. 단점은 Windows·Unity 버전 변화에 따른 유지 비용과 공용 잠금 goal과의 경계 조율이다.
- 검사화 방법
  - 정본 helper와 `node --test` fixture 셋이다.
  - 관문 판정: 수치 입력 fixture로 통과·차단 경계(예: 기존 2GiB·4GiB·6GiB 기준)를 단정한다.
  - 메타 관측: 임시 폴더 fixture에서 파일 추가·크기 변경이 before/after 차이로 나오는지 단정한다.
  - drift 분류: `SENTIS_ANALYTICS_ENABLED` 한 줄 제거 diff는 「알려진 drift」, 다른 ProjectSettings 줄 변경은 「미분류」로 나오는지 단정한다.
  - 하네스 원칙 2(정본 helper를 가장 쉬운 경로로)와 3(수기 경로 목록의 드리프트 검사)에 해당한다.
- 담당 후보: 214행대로 Unity harness를 다시 쓰는 goal의 담당(Content)이며, 공용 관문 부분은 Rules 공용 실행 잠금 goal과 조율한다. 최종 배정은 메인이다.

## 첫 발생·기존 후보 연결(후보 아님)

- U1의 직접 원인: 사용자가 장면 ②를 건너뛰었고, 리드 안내(22:01:06Z)가 Editor 종료(22:01:08Z) 직전에 도착했다. 다시 열면 로딩 약 6분이라 창 한도에 걸렸다(`joint-s6a/report.md` 15행). 필수 로그 줄(`52 거절 … 6/0`)을 Editor 닫기 전 미관측으로 알리는 감시는 없었다. 이 goal 첫 관측이다.
- 완료조건 4와 순서표 v3 S7 행의 판정 기준 어긋남: 결정 44 A 기록(goal 66행)에 남았다. 이 goal 첫 관측이며 두 문서는 고치지 않기로 했다.
- 리드 계약의 낡은 줄 수(+2,182, 2a 이전 값): 검증자가 잡아 정정했다(O4). 첫 관측이다.
- S6a의 Editor PlayerPrefs `AccountName`이 `user2`에서 `c_s6a_test1029`로 바뀐 채 남았다(goal 452·492행). 2b 계약이 before/after를 읽게 해 관측됐다. 첫 관측이며 리드가 되돌리지 않기로 기록했다.
- 첫 heartbeat의 memo-first 차단·receipt 미저장(1단·2a 2건): 기존 행 278행 `session-entry-memo-order`(승격 완료)와 290행 `single-output-role-memo-first`의 같은 경계다. 이 Gardener 계약은 「우편함 출력은 파일로 저장하지 않는다」로 이미 막았다. 새 후보로 세지 않는다.
- 복사본 ProjectSettings define·MinimapRT 줄 끝 변화: 기존 행 208행 `unity-sentis-define-drift`의 추가 관측이다.
- 검증자 자체 감사의 줄 번호 한 칸 밀림(`Get-Content`가 단독 CR을 줄로 나눔): 자기 발견·정정이다(final 64행). 거짓 판정은 없다.
- 10-24 단계의 A-RESOURCE-01(commit 여유 2GiB 미만 3연속 중단)·A-COMPILE-01·A-TEST-01과 P0 계약-명령 불일치: goal 269·280·286행에 분류와 Main 처분이 있다. 제품 확정 실패·독립 불합격 집계는 0이다.

## 설계 관찰과 대안

- CI 4종은 이 PR의 03_Client 19파일을 검사하지 않는다. `code-rules`는 `03_Client/`를 제외하고(`99_Tools/CodeRules/config.json` 17행), Unity 실행 workflow는 없다. 클라이언트 검증은 전부 메인이 배정하는 로컬 Unity 창에 기대며, 10-29 하루에 그런 창이 다섯이었다(goal 15행). goal 258행 「CI 4개 success」를 이 PR 제품의 검사 통과로 읽으면 안 된다. 대안으로 03_Client C# 정적 분석의 CI 연결이나 Unity batch CI가 있지만, 라이선스·runner·비용 판단이 필요한 별도 사용자 결정이라 후보로 올리지 않는다.
- 후보 1과 후보 2는 둘 다 「정본 helper가 있거나 정해졌는데 세션이 손이나 임시 사본으로 대신한」 모양이다. 후보 1은 helper가 이미 있어 진입 비용이 낮다. 후보 2는 소유 조율이 먼저다.
- 대안으로 「리드가 정산을 더 꼼꼼히 손으로 한다」도 있다. 하지만 이 goal에서 손 기록은 status를 빠뜨려 구간을 놓쳤다. 같은 실수를 문서 층으로 막기는 어렵다(교정 정본의 가장 높은 층 원칙).

## 실제 준수

| 계획(사전 맥락) | 실제 위치 |
|---|---|
| 첫 쓰기가 이 파일의 사전 맥락 | 00:05:59Z 직후 Write 1회로 「사전 맥락」 절 작성, 이후 Edit로 같은 파일만 확장 |
| 고정 입력 17파일 전수 대조 | 「사전 맥락」의 고정 입력 항목(17/17 일치) |
| 범주 5개 실사 | 「점검 범주별 결과」 표 5행 |
| 반복 빈도 기준 후보 최대 2개, 기존 행 우선 연결 | 「정리 후보」 2개, 각 기존 행 ID·원천·횟수·위반/미실행·범위 밖 이유·대안·검사화·담당 |
| 관측과 추론 분리 | 각 칸의 「관측」「추론」「미확인」 표기 |
| 현재 판정 보존 | 「결론」 마지막 두 항목, 「종료 기록 대조」 |
| 예시는 형식만 참고 | storage-foundation 절 구성만 사용. 결론과 후보는 이 goal 원천으로 작성 |

## 추가로 읽은 것(17 입력 밖)과 이유

- `99_Tools/README.md` 33·69~82행: SessionGuard와 생존 신호 helper의 정본 입력·의미를 확인했다.
- `.claude/settings.json` 전문: memo-first hook 등록을 확인했다.
- `99_Tools/CodeRules/config.json`(grep 17행)과 `.github/workflows/`(grep): CI의 03_Client 범위를 확인했다.
- `00_Document/operations/DEVELOPMENT.md` 98행: CI 검사와 Unity 검증의 경계 문장을 확인했다.
- `00_Document/operations/CURRENT.md` 전문: GR-01을 확인했다.
- BACKLOG 114·179·208·214·223·228·256·278·281~283·285·289~291·312·316행(node 부분 출력): 기존 행 대조다.
- `lead-opus-20261029/` 파일 목록, `*-contract-v*.md` 다섯의 grep, `impl-post-merge-contract-v2.md` 30행, `scope-draft-decision31-v1.md`의 grep(0건): GR-03의 계약 반영 여부를 확인했다.
- `lead-opus-20261029/*.json` 301파일의 메시지 필드(node 읽기): 생존 신호 구간을 셌다.
- 각 단계 `editmode*.log`·`joint-s6a/editor.log`의 `LocalLow` grep: 후보 2의 사용자 프로필 쓰기 횟수를 셌다.
- `.backups/tmp/` 아래 관문 helper 다섯 파일의 `sha256sum`, 복사본 네 폴더의 `ls`: 후보 2와 goal 492행을 확인했다.
- `01_Phases/goals/2026-10-14-guild-server/gardener.md`·`2026-10-10-dungeon-clear-rewards/gardener.md`의 grep 몇 줄: memo-first 선례를 확인했다.

## 실행 기록과 미실행·한계

- 실행한 것: 파일 읽기, `git`(`--no-optional-locks` 포함 rev-parse·log·diff·grep·status), `gh pr checks 273`·`gh run list`·`gh pr view 273`, `sha256sum`, node 한 줄 해시·링크·JSON 필드 조회다. 결과는 stdout으로만 읽었다.
- 우편함
  - heartbeat: `msg_9d961b6710bc`, `msg_19482a75b37c`, `msg_f98052d36340`, `msg_009e8a8479a5`, `msg_43c08623eb5b`, `msg_309d210be13b`, `msg_44065fae1609`를 단독 호출로 보냈다. 이후 송신분은 worker_done 본문에 적는다.
  - 단독 check: 착수 직후 1회와 본문 작성 직전 1회는 메시지 0이었다. 완료 직전 check는 worker_done 직전에 한 번 더 한다.
  - 공식 ask: 0.
- 하지 않은 것
  - Unity·dotnet·서버·DB·WSL·기존 harness 실행
  - 생존 신호 helper 실행. 14구간은 helper 규칙을 손으로 옮긴 node 읽기라 helper 출력과 다를 수 있다
  - CI 재실행
  - 10-24 단계 Dispatch들의 생존 신호 원시 집계
  - 다른 worktree·다른 goal의 helper 사용 여부 조사
  - S6a 원시 85파일과 검증 원시의 전수 열람
- 환경 사건
  - Bash 한 번에서 `cd`로 `lead-opus-20261029/` 폴더에 들어가 세션 작업 디렉터리가 그 폴더로 바뀌었다. 그 폴더에 쓰기는 없었다.
  - 다른 Bash 한 번에서 `cd`로 저장소 루트(content-active)로 돌아왔다. 이후 절대 경로와 `git -C`를 주로 썼다.

## 자기 쓰기 감사

- 이 세션이 수동으로 쓴 파일은 이 보고서 하나다.
  - 최초 Write 1회는 00:05:59Z 직후다. 이후 같은 파일의 Edit로만 확장했다.
- 별도 context·raw·receipt·스크립트·TEMP 수동 파일·scratchpad·제품·시험·규칙·goal·CURRENT·BACKLOG 쓰기는 0이다.
- commit·push·branch 전환·PR 조작·추가 위임·도구 채택은 0이다.
- provider 자동 출력(Claude Code 대화 기록, 큰 도구 결과의 자동 저장)은 하네스 쓰기이며 내 수동 쓰기가 아니다.
- 최종 SHA256과 쓰기 종료 시각은 이 파일에 넣을 수 없어 worker_done 본문에 적는다.
