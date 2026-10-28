# PR270 - 캐릭터 서버 종료 Gardener

## 사전 맥락(최초 메모)

- 작성 시각: 시계 출력 `2026-10-28T23:15:23Z` / `2026-10-29 08:15:23 KST`(직전 `date -u`·`TZ=KST-9 date`). 이 절이 이 세션의 첫 파일 쓰기다. 그 전에는 읽기·hash·git 조회와 heartbeat 1회(`msg_ef75252475cd`)만 했다.
- 역할과 live 신원: `[World Gardener]`, Task `task_141e27c7730f` / Dispatch `ctx_c51255552aa8`, 자기 handle `term_6f220585-f503-4a98-8eee-6efd89cbe4ef`(이름 지정 `ORCA_TERMINAL_HANDLE` 값과 같다). coordinator는 `[World 리드 Opus]` `term_a7ec0ab8-3b3b-4824-bd59-24f5366b309b`, Run `run_90d18490c825`(계약 7행). 요청 모델 `claude-opus-5-5`, 세션 자기 인식도 같은 ID다. backend 실제 모델은 `unknown`이다.
- 기준 SHA: HEAD `ae1ba9c05f05b13ffe01e66efc770120603eb08e`, branch `docs/character-server-closeout-20261029`. HEAD~1 = 로컬 origin/main = PR270 merge `8b6010d543c95837f504a225cdec5b9da9f854b5`이고 부모는 `540172a4…`(첫 부모)와 `2d9df386…`(PR head)다. HEAD~1→HEAD는 goal.md +23/-6·CURRENT.md +1/-1 2파일이다(`git diff --numstat`, 최초 메모 때 `--stat` 막대를 잘못 읽은 값 +22/-7을 본문 작성 때 정정). 작업 트리 미커밋은 계약 15행대로 읽지 않는 `99_Tools/Start-PersistenceServer.ps1` 하나다.
- 계약: `closeout-01/gardener-contract-v1.md` sha256 `537708a56e2751ebe0507b09dac5d6669cef113eb7144d8bf05bff9e69e36fdb`, 12520자(`sha256sum`·`wc -m` 실측, brief 값과 일치). 전문을 읽었다.
- 고정 입력: `closeout-01/gardener-inputs.json`(utc `2026-10-28T23:12:35.801Z`, head `ae1ba9c0…`) 13파일의 bytes·sha256을 node로 재계산해 13/13 일치했다.
- 읽은 절(메모 시점): AGENTS·CLAUDE 전체(세션 주입), 계약 v1 전문, inputs.json, ORCA.md 231~256행(「R-8」·「목표 종료 Gardener 4주 파일럿」), 형식 예시 `01_Phases/goals/2026-10-14-guild-server/gardener.md` 전문(SHA256 `38CC9403…` 고정 입력 일치).
- 선택 원문: 계약 61~159행에 실린 CODE_CONVENTION 「파일 위치와 이름」·「빌드가 검사하는 규칙」·「주석과 문서」·「역할별 적용」·「변화 평가」. 제외 절은 계약 66행의 이유와 같다. 범주 실사에 필요하면 해당 절만 더 읽고 이유를 남긴다.
- 형식 예시와 재사용: 예시의 「사전 맥락 → 결론 → 범주별 표 → 발생 수 → 종료 기록 대조 → 리드 반환 → 후보 → 첫 발생 → 실제 준수 → 실행/미실행 → 자기 쓰기 감사」 구조와 범주 판정 표 형식만 가져온다. 예시의 결론(봇 회귀 목록·TRX helper)은 기대답으로 복제하지 않고, 계약 32행대로 새 근거가 없으면 다시 제안하지 않는다. 새 helper·스크립트는 만들지 않고 git·node 한 줄 조회를 stdout으로만 읽는다.
- 권한: 쓰기는 이 파일 한 개다. 제품·시험·규칙·BACKLOG·goal 쓰기, 빌드·시험·SQL·DB·WSL·dotnet·Unity·서버·harness 실행, commit·push·병합, 추가 위임은 0이다. 허용은 파일 읽기, git·gh 읽기, hash 계산이다. 우편함 명령은 파이프·리디렉션 없이 단독 호출한다.
- 보존: World PASS 범위와 미실행 한계(Unity PlayMode·화면 메뉴→Town·Player build 미실행, WSL DB·장시간 28건 건너뜀, Content·Core 판정 제외)를 바꾸지 않는다. 보고서 수치를 새 재실행으로 세지 않는다.
- 점검 계획: 다섯 범주(독립 결함·CI 실패·새 경고 억제/설정 완화·임시 우회·드리프트/규칙 검사)를 판정 원문 `verify-pr270-01/worker/report.md` 전문 → 구현자 보고·분류표의 필요한 절 → 저장 CI → 제품 diff(`627c9915…`→`8b6010d5…`) 키워드·파일 유형 조회 순으로 본다. goal.md는 계약 29행의 절만 읽는다. BACKLOG는 검색으로만 겹침을 본다.
- 계약 밖 리드 관측 두 건(계약 36·37행): `gh pr ready 270` 권한 분류기 거부, closeout branch의 upstream이 `origin/main`으로 잡힌 일. 후보 여부를 원천과 반복 근거로 판단한다.
- 관찰(메모 시점, 확인 전): 계약 14행의 범위 기준 `627c9915…`는 merge 첫 부모 `540172a4…`와 다르다. merge-base인지 확인하고 차이를 보고서에 적는다.
- 질문: 현재 없음.
- 메모 시점 관찰의 확인 결과: `627c9915…`는 PR269 merge이고 PR270의 원래 기준이다. `540172a4…`(PR271, CLAUDE.md)는 branch에 merge commit `93c4b41b`로 들어갔다(goal 961행). 그래서 `627c9915…`→merge는 91파일 +9878/-154, merge 첫 부모→merge는 90파일 +9867/-154다(`git diff --shortstat`). 차이 1파일은 CLAUDE.md다. 이 보고서는 계약 14행의 91파일 범위를 쓴다.

## 범주별 근거와 검토 상태

### 요약

- **새 독립 제품 결함은 없다.** World 판정은 PASS이고 제품 차단 결함은 0이다(verify `report.md` 3·142·185행). 이 Gardener는 판정을 다시 쓰지 않는다.
- **CI 실패는 2건 있었고 최종 head에서는 0이다.** PR270 branch의 24 run 가운데 dotnet-tests 2 run이 실패했다. 하나는 서식 56건, 하나는 이미 알려진 회귀 12건이다. goal에는 두 실패 run이 이름으로 남아 있지 않다(GR-01).
- **새 경고 억제·설정 완화는 없다.** 설정류 파일 변경은 0이다. 키워드에 걸린 2행은 조건부 DB 입력 skip과 단정 메시지라 억제가 아니다.
- **제품 코드의 임시 우회는 없다.** 운영 쪽 임시 상태는 Core launcher 미커밋 사본 하나가 아직 열려 있다(되돌리기 대기, goal 15행).
- **규칙 파일 드리프트는 없다.** 대신 현재 문서 드리프트를 찾았다. `domains/protocol.md`의 최신 절이 v20인데 코드는 v22다(후보 1, GR-02).
- 정리 후보는 **2개**다. 후보 1은 프로토콜 버전과 현재 문서의 드리프트 검사, 후보 2는 JSON 시각 처리의 정본 helper와 계약 승계다.
- 리드 반환은 **2건**이다. GR-01은 CI 실패 기록 보강(비차단), GR-02는 현재 문서 미갱신(규칙 위반 관찰, 제품 판정 변경 아님)이다.
- 이 보고는 제품 전체 검증, 사용자 승인, goal 최종 종료를 선언하지 않는다.

### 범주 표

| 범주 | 확인 범위·명령 | 결과 |
|---|---|---|
| 독립 결함 | verify `report.md` 전문(SHA256 `38327457…` 고정 입력 일치), `unity-failure-classification.md` 1~20행, `existing-test-classification.md` 절 제목, 구현자 보고 두 개의 실행·보충·미실행 절, goal 936~982행 | 검토했고 새 지적 없음. 제품 차단 결함 0이다(report 20~31행). 검증자 쪽 VTEST-01(새 시험 기대 오류)·VENV-01/02(실행 환경)·VEXEC-01/02(실행 보조 코드)는 판정 원문에서 제품 결함으로 세지 않았고 원시와 함께 보존됐다(report 24~32행). 구현 단계의 18 실패는 이 branch의 회귀 9(버전·패킷 기대 6, selector 거절 3)와 기준 branch의 기존 불안정 9로 분류돼 수정 구현에서 0이 됐다(impl-fix `report.md` 77~85·98행). 설계 관찰 O1·O2는 비차단, O3는 직접 대조로 충족이다(report 61~63·206~213행). |
| CI 실패 | `gh run list --branch feat/character-server-20261018`(24 run, 6 head), 실패 run의 `gh run view --json jobs`와 `--log-failed`, 저장 `pr-checks-4755f807-final.txt`·`pr-checks-2d9df386-final.txt` | 실패 2/24. ① run `40708363252`(head `4c4d54da`, 14:41Z) dotnet-tests의 「Whitespace, input manifest, preservation and formatting tool tests」 단계 실패. `error WHITESPACE` 56행이 5파일에서 나왔다(CharacterMenuReplyFactory.cs 30, CharacterMenuCatalogSessionTests.cs 15, CharacterMenuPacketContractTests.cs 5, ServerHostLifecycleTests.cs 4, PersistenceCharacterProbe.cs 2). 구현자 보충 1이 손으로 고쳤다(impl-stored-create `report.md` 48~61행). ② run `40718932629`(head `77591936`, 15:44Z) dotnet-tests의 「Test」 단계 실패. Total 2563 = Passed 2523 + Failed 12 + Skipped 28이다. 같은 head의 서식 단계는 success다. 12는 버전·패킷 기대 6, selector 거절 3, GuildDebugTreasuryCheatTests 3으로 읽힌다(실패 이름 목록). 같은 시점 WSL E2의 18(Integration 9 포함)과 수가 다른 것은 단일 GameWorld 경합의 승자가 실행마다 바뀐 것과 맞는다(impl-fix 77~83행의 양방향 재현, 추론). 최종 `4755f807`·`2d9df386`은 각 4/4 success이고 저장 파일과 gh 조회가 같다. |
| 새 경고 억제·설정 완화 | 91파일 확장자 집계(cs 70, md 9, meta 10, dll 1, xml 1), 설정류 정규식(`.editorconfig`·`.props`·`.targets`·`.csproj`·`.sln(x)`·`.json`·`.asmdef`·`.yml`·`.ps1`·`.sh`·`.github/`·settings) 적용, `.md` 제외 추가 행의 억제 키워드 grep(`NoWarn`·`pragma warning`·`SuppressMessage`·`dotnet_diagnostic`·`Skip *=`·`[Ignore`·`continue-on-error`·`bypass`·`TODO`·`HACK`·`workaround`·`임시`·`우회`·`억제`·`완화` 등) | 설정류 0파일. meta 10개는 모두 신규(A)이고 기존 meta 수정은 0이다. 키워드 2행: `CharacterMenuDatabaseInput.cs:14`의 `Skip =`은 `DAWNHOLDER_CATALOG_DB_INPUT`이 없을 때만 이유를 붙여 건너뛰는 조건부 attribute다. 같은 모양이 기존 Core `CatalogDatabase.cs:17·23`에 있다. 사용처는 `ServerHostLifecycleTests.cs:60` 한 곳이다. 다른 1행은 단정 메시지의 「bypass」 낱말이다. 경고는 아래 「경고 귀속」에 적는다. |
| 임시 우회 | 위 키워드 grep(제품 0), goal 15·415·857행, verify report 114~116행 | 제품 코드 0. 운영 쪽: ① Core launcher `99_Tools/Start-PersistenceServer.ps1` 미커밋 사본은 sha256 핀(`F27EEC2D…`) 아래 World commit에서 빠져 있고, 메인이 서버 창을 닫은 뒤 HEAD 판으로 되돌리기로 했다(goal 15·857행, Core `msg_df8970186d11`). 이 세션 기준 `git status`에 아직 M으로 남아 있다(파일은 읽지 않음). ② 밤 HB 300초 초과 한정 처분은 10-27 09:00 KST에 만료됐다(goal 45·415행). ③ V3 새 snapshot 경로와 `-nographics` 제외는 메인 보충 승인의 실행 조건 변경이며 기대값 변경 0이다(verify report 79·114~116행). 우회가 아니다. |
| 드리프트·규칙 검사 | `git diff --stat origin/main HEAD -- AGENTS.md CLAUDE.md .claude/settings.json .agents/`, 고정 입력 13개 재해시, PR270의 md 변경 목록, `ProtocolVersion.cs`·`domains/protocol.md`·`FEATURE_MAP.md`, 계약 원문의 메인 지시 승계 grep | 규칙 파일 diff는 빈 출력(exit 0). 고정 입력 13/13 일치. PR270이 바꾼 md 9개는 goal 폴더 7개와 CURRENT.md·CLAUDE.md(PR271)다. `00_Document/domains/`·`FEATURE_MAP.md` 변경은 0이다. `ProtocolVersion.cs:89`는 `Current = 22`인데 `domains/protocol.md`의 최신 절은 11행 `## v20 길드·거점·개인 강화`다(후보 1). 10-17 메인 지시의 JSON 시각 문장은 정본에 없고 Opus 시기 계약에서 빠졌다(후보 2). fetch를 하지 않아 원격의 더 새 상태는 미관측이다. |

### 경고 귀속(실측)

원천은 verify `worker/raw/wsl-test-2.txt`(75,139 bytes, SHA256 앞자리 `14f8636d67e9b9a3`)다. MSBuild 요약은 `80 Warning(s)`·`0 Error(s)`다. `file.cs(행,열): warning 코드` 형식의 고유 위치는 66개다. 그중 PR270 변경 `.cs`에 있는 것은 `Persistence/Admission/CharacterAdmissionServices.cs(74,19)` SA1202 하나다. `git blame`으로 74행은 base 조상 `bcafb6bc`(10-15 Core 영속화) 소유이고, PR270은 같은 파일에 `internal TimeProvider TimeProvider => _timeProvider;` 한 줄을 더했다. 같은 조건의 base 빌드 원시가 없어 이 경고의 신규 여부는 unknown이다. 수정 구현자는 전후 모두 80이라고 적었다(impl-fix `report.md` 107행). 고유 66과 요약 80의 차이는 확인하지 않았다.

### 종료 기록 대조

대상은 goal의 완료·미완료 표현과 현재 main·판정이다. HEAD~1→HEAD는 goal.md +23/-6, CURRENT.md +1/-1이다(`git diff --numstat`).

| 항목 | 기록 위치 | 원천 | 결과 |
|---|---|---|---|
| 병합 상태·시각 | goal 9·15행 | `gh pr view 270`: state MERGED, mergedAt `2026-10-28T23:06:56Z`, merge `8b6010d5…`, head `2d9df386…`, isDraft false. 로컬 origin/main = `8b6010d5…` | 일치. 승인 줄 원문과 `msg_4892c80fd856`은 원문 미대조다. |
| 진척 단계 | goal 925~934행 | 898행 「생기면 진척 단계에 실제 `PR번호 병합`을 적는다」 | 일치. 제품 병합에 PR270이 적혔고 종료 기록 `[>]`, 종료 점검 `[ ]`가 현재와 맞다. |
| 판정 수치 | goal 86·963행 | verify report 3·94·98·101·185행 | 일치(2550/0/28, Unity 1337 통과, 4755f807 CI 4 성공, 제품 차단 0). |
| 병합 head CI | goal 969행 | `gh run list`의 `2d9df386` 4 run success, 저장 `pr-checks-2d9df386-final.txt` 4줄 pass | 일치. 저장 파일에는 head·시각·workflow 이름이 없고 파일 이름으로만 head를 알 수 있다(설계 관찰). |
| 구현 단계 CI | goal 948·951행 | 위 「CI 실패」 행 | 부분 일치. 951행의 「head 77591936의 PR CI 서식 check 2건은 통과」는 서식 단계 기준으로 맞다. 그러나 같은 head의 dotnet-tests는 Test 단계에서 실패했고, `4c4d54da`의 서식 실패 run도 goal에 이름이 없다 → GR-01. |
| 미실행 한계 | goal 973~976행 | verify report 106·231~234행 | 일치. PlayMode·화면 메뉴→Town·Player build 미실행, 28 skip 비통과, Content·Core 판정 제외가 그대로다. |
| 현재 문서 갱신 | goal 826·863행 | `domains/protocol.md` 절 제목, `FEATURE_MAP.md:9`, PR270 md 목록 | 불일치 → GR-02. 863행은 「이번 제품 변화 시 현재 설명을 함께 맞춘다」고 적었다. |
| CURRENT World 줄 | CURRENT.md diff | branch를 `docs/character-server-closeout-20261029`로, 상태를 「제품 PR270 병합·종료 기록 진행」으로 바꿈 | 지적 없음. |

### 리드 반환

- **GR-01** · `goal.md:948·951` · PR270 branch의 CI 실패 2 run이 goal에 run·head로 남아 있지 않다. `40708363252`(`4c4d54da`, 서식 56건)와 `40718932629`(`77591936`, Test 12 실패)다. 951행의 「서식 check 2건은 통과」만 읽으면 그 head의 CI가 통과한 것처럼 보일 수 있다. · 대안: 「결정과 단계 결과」의 구현자 항목에 두 run ID와 실패 단계·수를 한 줄씩 더한다. · 제품 비차단. 종료 기록 PR 전 반영을 권한다.
- **GR-02** · `goal.md:826·863`, `00_Document/domains/protocol.md:11·37`, `00_Document/FEATURE_MAP.md:9` · PR270은 protocol 22와 패킷 49~54, 새 handler 3개(`Handlers/Session/CharacterList·Create·SelectRequestHandler.cs`)를 넣었지만 현재 문서를 고치지 않았다. protocol.md의 최신 절은 v20이고 37행 접속 순서는 `C_Login → Accepted → C_CharacterSelect`로 끝난다. FEATURE_MAP 9행은 「로그인 뒤 `CharacterSelectHandler`」만 가리킨다. CODE_CONVENTION 「주석과 문서」의 「계약이나 탐색 경로가 바뀌면 관련 현재 문서를 함께 갱신한다」에 대한 위반 관찰이다. · 대안: 종료 기록 PR이나 별도 문서 작업에서 protocol.md에 v22 절(필요하면 v21도)을, FEATURE_MAP 9행에 메뉴 요청 handler→`GameSession`→`CharacterMenuCatalogSession` 경로를 더한다. · 현재 차단 여부: 제품 판정을 바꾸지 않는다(PR270 병합 완료). 종료 기록 PR에 넣을지, Content 화면 착수 전 별도로 할지는 리드·메인 판단이다. 후보 1과 연결된다.

## 정리 후보

### 후보 1 - 프로토콜 버전과 현재 문서의 드리프트 검사

- **원천·관측 횟수(실측):** `git log -S "Current = 21"`은 `bcafb6bc`(2026-10-15, Core 「persistence runtime entry」), `-S "Current = 22"`는 `dcf906a0`(2026-10-18, 이 goal, PR270)를 가리킨다. `domains/protocol.md`의 마지막 commit은 `e2744dc7`(10-15 길드)이고, 버전 절은 `## v20`(11행)부터 `v19`·`v18`·`v17`까지뿐이다. 버전을 두 번 연속 올리는 동안 서로 다른 goal·파트 2곳에서 현재 문서가 갱신되지 않았다. 이번 goal은 863행에서 갱신을 스스로 예정했다. BACKLOG에 같은 행은 없다(`protocol.md`·`protocol-doc` 검색 0행).
- **실제 위반과 미실행:** 문서 상태는 실제 관측이다. 이를 잡는 검사는 저장소에 없다. 그래서 「검사 미실행」이 아니라 「검사 부재」다.
- **현재 범위 밖인 이유:** 제품 PR은 병합됐다. 공용 검사 구현은 이 goal의 「하지 않을 것」(835행, 공용 운영 규칙 정비)에 걸리고 Gardener는 쓰기 권한이 없다.
- **영향:** 사용자 결정 27 A에 따라 Content가 이 서버 계약 위에서 선택·생성 창을 만든다(goal 475행). 처음 읽는 사람이 현재 문서 입구(`domains/protocol.md`·FEATURE_MAP)에서 출발하면 v20과 옛 접속 순서를 만난다. 정확한 계약은 goal 폴더의 `packet-contract-v1.md`에만 있다.
- **한 줄 대안:** 버전 올림 PR마다 protocol.md에 `## v<N>` 절이 생겼는지 기계로 비교한다.
- **검사화(높은 층부터):**
  - 시험: 기존 정적 검사 묶음(architecture-tests 또는 code-rules)에 `ProtocolVersion.Current` 값과 `domains/protocol.md`의 `## v<N>` 최댓값을 비교하는 시험을 둔다. 실패 진단은 「Current=22인데 protocol.md 최신 절은 v20이다. v21·v22 절을 추가하라」처럼 원인·위치·고칠 방법을 낸다(하네스 원칙 1). PDL 최대 패킷 ID가 문서에 언급되는지도 같은 시험에 넣을 수 있다. 하네스 원칙 4대로 warning 파일럿부터 시작한다.
  - 문서(낮은 층): 규칙 원문은 이미 CODE_CONVENTION에 있는데 두 번 빠졌으므로 문서만으로는 막기 어렵다.
- **담당 후보 파트:** 검사는 CodeMap(Architecture) 또는 Rules, 문서 내용은 v22 World·v21 Core다. 채택은 메인을 거쳐 사용자가 정한다. 이 Gardener는 구현·BACKLOG 등록을 하지 않았다.

### 후보 2 - JSON 시각 처리의 정본 helper와 계약 승계

- **이번 goal 원시(실측):** VEXEC-02다. `verify-pr270-01/worker/unity/timeout-calculation-error-2.json`(260 bytes, SHA256 `0578f964…`)은 `loadedStart 17:19:38.4777682Z`를 `erroneousReparse 08:19:38.0000000Z`로 바꾼 값을 남겼다. 9시간 이동과 소수초 손실이다. 그 결과 검증자가 소유 Unity PID 21628을 약 69초에 조기 종료했다(`worker/context-memo.md` 62행, report 28·97행). 실제 deadline은 17:44:38Z였다. V3 시도 2는 결과 XML 없이 무효가 됐고 새 snapshot07 복사와 시도 3이 필요했다(report 114~116행).
- **규칙은 있었지만 정본이 아니다:** 메인 10-17 지시 문장은 「JSON의 시각 필드는 ConvertFrom-Json -DateKind String으로 읽거나 원문 문자열로 비교하고, 시각을 쓰는 실행 관문 스크립트는 실제 실행 창 전에 오프라인으로 한 번 통과시켜 본다.」다. 기록은 `2026-10-17-character-storage-foundation/goal.md:363`(`msg_37121b644c71`, 「정본 반영은 10-28 뒤 Rules 후보」), `2026-10-15-guild-client-ui/goal.md:463`(`msg_f4d55486e803`), 이 goal의 `approval-01/compile-red-02/main-date-guidance-ack.md`(`msg_c616d0d5aa9a`)다. `git grep DateKind HEAD -- 00_Document/operations 00_Document/conventions .agents`는 0행이고 BACKLOG 검색도 0행이다.
- **계약 승계 누락(실측):** `approval-01/*/task-spec-v*.md` 가운데 12개가 이 문장을 1줄씩 담았다(Sol 리드 시기, 발행 시각은 대조하지 않음). 10-28 Opus 리드 전환 뒤 계약 4개는 0줄이다: `impl-stored-create-01/contract-v1.md`·`contract-supplement-01.md`, `impl-fix-01/contract-v1.md`, `verify-pr270-01/contract-v1.md`(`grep -c`). 오류 뒤 리드 보충 `msg_578eee12a3eb`가 같은 취지를 다시 넣었다(`worker/raw/v3-supplement-msg_578eee12a3eb.txt` 12행).
- **다른 goal의 같은 유형(기록 인용, 원시 미대조):** 10-05 architecture-tests-ci goal 160행(UTC 문자열이 KST로 바뀐 값을 원시 시각으로 부른 문서 오기, 첫 사례로 기록), 길드 서버 integration-repair 첫 plan 관문 exit1(앞 Gardener `2026-10-14-guild-server/gardener.md:119`), `2026-10-15-guild-client-ui`·`2026-10-17-character-storage-foundation` 두 goal에 메인 지시가 내려간 일이다. 이번 VEXEC-02까지 같은 함정이 네 goal 이상에서 기록됐다. 이 Gardener가 원시로 본 것은 VEXEC-02 하나다.
- **실제 위반과 미실행:** VEXEC-02는 실제 실행 오류다(검증자 실행 보조 코드, 제품 결함 아님). 지시 미승계는 계약 원문으로 확인한 실제 누락이다. 이 함정을 막는 검사는 저장소에 없다.
- **현재 범위 밖인 이유:** 운영 정본·helper 정비는 이 goal의 「하지 않을 것」(835행)이고, Gardener는 규칙 채택 권한이 없다.
- **영향:** 지시가 goal 기록에만 있으면 리드가 바뀔 때 계약에서 빠지고, 세션마다 새로 쓰는 관문 스크립트가 같은 함정에 다시 빠진다. 이번 비용은 Unity 시도 1회 무효와 snapshot 재복사 1회다.
- **한 줄 대안:** 경과·deadline 계산과 JSON 시각 읽기를 정본 helper 하나로 모으고, 계약 템플릿이 그 helper를 가리키게 한다.
- **검사화(높은 층부터):**
  - 정본 helper: `99_Tools`에 시각 읽기(`-DateKind String` 또는 원문 문자열)와 경과 측정(Stopwatch 또는 같은 시계 비교)을 한 곳에 둔다. fixture 시험은 `…Z` 문자열을 KST 시계에서 읽어도 이동 0·소수초 보존을 고정한다(하네스 원칙 2).
  - lint(보조): 저장소 `.ps1`의 `ConvertFrom-Json` 호출 중 `-DateKind`가 없는 것을 warning으로 낸다(원칙 4). 다만 이번 사고 스크립트는 저장소 밖 근거 폴더에 있어 lint만으로는 닿지 않는다. 그래서 helper가 주 층이다.
  - 문서(낮은 층): task-context 위임 계약 템플릿에 문장을 넣는 것만으로는 이번처럼 리드 교체 때 빠질 수 있다.
- **담당 후보 파트:** 정본 문장·템플릿은 Rules, helper 배치는 CodeMap이 코드 주인 파트를 정한다. 기존 BACKLOG `powershell-dotnet-boundary-checklist`(70행, PowerShell과 .NET 경계, 대기)와 책임이 닿는다. 합칠지는 메인이 판단한다.

## 첫 발생과 미실행의 구분

후보로 올리지 않은 사례다. 같은 사례를 두 번 세지 않았고 제품 확정 실패와 운영·환경 오류를 합치지 않았다.

| 사례 | 수·원천 | 처리 |
|---|---|---|
| Unity 전체 EditMode 실행 명령(VENV-01 `-nographics` 81건, VENV-02 268자 경로 1건, VEXEC-01 launcher 반환 0을 종료로 오인) | 이 goal 3건. VENV 둘은 리드 계약 `verify-pr270-01/contract-v1.md:103`의 `-batchmode -nographics`와 긴 snapshot 경로에서 나왔다. 앞 기록: `2026-09-29-refactor-client-entry/goal.md:75`(PlayMode `-nographics` native crash), `2026-10-13-demo-video-art/goal.md:361`(긴 snapshot 질문)·`:450`(「runner/wrapper exit0은 시험 통과가 아니다」), BACKLOG `persistence-runtime-review-observations`(375행)의 「긴 snapshot」. `2026-10-01-hierarchical-routing-pilot/goal.md:95`는 M0 선례의 `-nographics`를 따르라고 적었다. 99_Tools에 Unity 실행 helper가 없고(`ls`) DEVELOPMENT에 Unity 시험 명령이 없다(grep 0). | 반복 근거는 있지만 다른 goal 사례는 PlayMode·질문·관찰로 종류가 섞이고 원시는 이번 goal만 봤다. 후보 상한 2개라 올리지 않았다. 기존 BACKLOG `verification-operating-simplification-canon`(285행, Unity 단일 실행 공용 도구)·`unity-harness-user-file-restore-mark`(312행)에 플래그·경로 길이·실제 종료 대기를 함께 묶을지는 메인 판단이다. VEXEC-02는 후보 2에서만 셌다. |
| 로컬 서식 검사 미실행 → CI 서식 실패 | CI 1회(56건). E3 `format-check.ps1` exit1(global.json 10.0.301·rollForward disable, Windows SDK 8.0.427·10.0.103, impl-stored-create `report.md` 72·76행). 같은 goal 세 세션이 로컬 서식을 돌리지 않았다(impl-stored-create 61행, impl-fix 113행, verify report 101행). WSL 진입 `99_Tools/format-check.sh`는 있다(`2026-10-01-readability-format-ci/goal.md:120`). | BACKLOG `prepush-ci-format-checks`(381행)가 같은 책임이다. 사례 추가로 연결한다. goal 951행이 「계약에서 명령을 잘못 고른 리드 책임」으로 기록했다. |
| WSL `--` 인자 재해석 | 수정 구현자 시도 1 exit 127(필터의 `\|`가 파이프로 해석, impl-fix `report.md` 95·100행). 검증자는 `--exec`를 공식 회신으로 승인받아 피했다(verify report 108행). DEVELOPMENT 26~30행은 아직 `wsl -d Ubuntu -- bash` 형태다. | BACKLOG `wsl-entry-argument-split`(270행)이 다룬다. 사례 추가로 연결한다. |
| `gh pr ready 270` 권한 분류기 거부 | 1회, 22:37Z, 사유 「Unrequested Commit in a Connected App」(`lead-opus-entry-20261028/main-ci-ready-blocked-v1.txt`, 1,332 bytes, SHA256 `31ee21da…`). 사용자가 메인 pane에서 해제했다(goal 969행). | BACKLOG `auto-mode-workflow-blockers`(149행)와 같은 종류다. 우회 없음이 원천에 적혀 있다. 새 후보 아님. |
| closeout branch의 upstream이 `origin/main`으로 잡힘 | 계약 37행(리드 관측). 앞 사례 `2026-10-05-module-boundary-warning/goal.md:203`. 두 번째 발생이다. 현재 `branch.docs/character-server-closeout-20261029.merge`는 같은 이름 branch이고 `origin/docs/character-server-closeout-20261029` = HEAD `ae1ba9c0`다. `push.default`는 저장소에 설정이 없다(`git config --get` exit 1). | Git 기본 `simple`이면 이름이 다른 upstream으로의 인자 없는 push는 거절된다(추론, 실행하지 않음). 피해 0이고 후보 상한 때문에 올리지 않았다. 두 번째 발생이라 반복 규칙 요건은 채운다. 낮은 비용 대안은 R-8 또는 milestones 「main 맞춤과 합류점」에 `git switch --no-track -c <branch> origin/main` 한 줄이다. 판단은 메인이 한다. |
| 검증자 초기 heartbeat 5분 초과 | 1구간 공개(verify report 124행, `msg_39186d2b5a61`) | BACKLOG `worker-liveness-tool-check`(223행)·`liveness-helper-followup`(281행)·`heartbeat-interval-review`(283행)가 있다. 새 후보 아님. |
| VTEST-01 | 1회, 검증자 새 시험의 packet 9 기대 오류(verify report 24·83행) | 판정 원문에서 수정·재실행 완료. 첫 발생. |
| CONTRACT-PS01 | 1회(goal 946행, Sol 리드 시기) | goal이 첫 발생으로 기록했고 계약 층에서 처리했다. |
| 기준 branch의 collection 누락 | 1파일, `GuildDebugTreasuryCheatTests`의 `[Collection]` 누락(impl-fix `report.md` 77~85행). 병렬 단계에서 GameWorld를 만드는 다른 파일은 0이다(같은 보고 82행의 정적 확인). | 이번 PR이 한 줄로 고쳤다. 첫 발생. 같은 유형이 다시 나오면 「GameWorld를 만드는 시험은 비병렬 collection」 정적 시험을 검토할 수 있다. |
| 구현자 worker_done의 파일 수 셈 오류 | 1회(「12개」 vs 실제 11, impl-stored-create `report.md` 119행) | 보고서에서 스스로 정정. 일회성. |

미실행 구분: 이 Gardener는 빌드·시험·Unity·WSL·DB·서버를 실행하지 않았다. 위 수치는 모두 기존 원시나 gh 저장 관측에서 읽었다. 앞 목표 Gardener의 미채택 후보(TRX helper, 봇 등록·회귀 목록 드리프트)는 새 근거가 없어 다시 제안하지 않았다(goal 982행).

## 설계 관찰과 대안

- **O1·O2(판정 원문):** `CharacterMenuRequestReader.cs:53` 주석이 Catalog 경로를 설명하지 못한다. selector 호환 세션(Mode=Stored, Catalog=null)이 16을 보내는 점과 packet-contract 99행 설명이 다르다. goal 974행에 후속 후보로 남아 있다. 이 Gardener는 새 판단을 더하지 않는다.
- **조건부 DB skip attribute 두 벌:** `Network/CharacterMenuDatabaseInput.cs:9~16`과 Core `Persistence/CharacterCatalog/CatalogDatabase.cs:17·23`이 「환경 입력이 없으면 이유를 붙여 skip」을 각자 구현한다. 지금은 두 곳이라 공유를 요구하지 않는다. 세 번째가 생기면 시험 공용 helper 하나로 모으는 것이 탐색에 낫다.
- **CI 저장 관측 형식:** `pr-checks-4755f807-final.txt`는 머리에 조회 UTC·head와 run 표가 있지만 `pr-checks-2d9df386-final.txt`는 `gh pr checks` 4줄뿐이라 head를 파일 이름으로만 알 수 있다. BACKLOG `pr-observation-evidence`(34행)와 같은 책임이다.
- **30초 가독성 확인(현재 문서 기준):** 「캐릭터 메뉴 요청을 누가 처리하나」를 FEATURE_MAP에서 찾으면 9행이 `CharacterSelectHandler`만 가리켜 새 handler 3개와 `CharacterMenuCatalogSession`에 닿지 못했다. 실제 경로는 `Handlers/Session/` 목록(`git ls-tree`)과 verify report 54행에서 찾았다. GR-02와 같은 원인이다.

## 실제 준수와 추가 읽기

| 규칙 | 실제 위치 |
|---|---|
| 첫 쓰기 = 사전 맥락 | 「사전 맥락(최초 메모)」을 Write 1회로 먼저 만들었다(23:15:23Z 시계 출력 직후). |
| 파일 위치와 이름 | 계약 19행의 지정 경로 한 파일이다. 제품 이름 금지 규칙은 비해당이다. |
| 주석과 문서 | 긴 원시를 복제하지 않고 path·행·hash·run ID로 연결했다. 현재 문서 갱신 규칙을 GR-02와 후보 1의 판정 근거로 썼다. |
| 역할별 적용 | 문서 실사 등급이다. 표마다 검토·미대조·미관측·추론을 나눴다. |
| 교정 층과 반복 규칙 | 두 후보 모두 시험·helper를 문서보다 먼저 적었다. 1회 사례는 후보로 올리지 않았다(「첫 발생과 미실행의 구분」). |
| 하네스 원칙 | 후보 1은 원칙 1·3·4, 후보 2는 원칙 2·4다. 실행하지 못한 검사를 통과나 위반으로 바꾸지 않았다. |
| 사람의 탐색과 가독성 | 「설계 관찰」의 30초 확인에 찾은 경로와 못 찾은 경계를 적었다. |
| ORCA 목표 종료 Gardener | 후보 2개에 검사화 방법을 적고 실제 위반과 검사 부재·미실행을 나눴다. 채택·구현·BACKLOG 쓰기는 하지 않았다. |

추가 읽기와 이유(계약 29·30행 밖):

- goal.md 1~112행 전체: 「재개 지점」의 다섯 「다음 첫 일」과 「합동 실DB 창 결과」가 그 구간 안에 이어져 있어 함께 읽었다. 88~112행 재개 카드는 완료 표현 대조용으로 훑었다.
- 다른 goal 특정 행(`git grep`으로 찾은 뒤 해당 행만): 2026-10-05-architecture-tests-ci 160, 2026-10-05-module-boundary-warning 200~206, 2026-10-15-guild-client-ui 244·463, 2026-10-17-character-storage-foundation 363, 2026-09-29-refactor-client-entry 75, 2026-09-29-refactor-baseline 74, 2026-10-01-hierarchical-routing-pilot 95, 2026-10-13-demo-video-art 361·450, 2026-10-14-persistence-runtime-entry 223, 2026-10-10-dungeon-clear-rewards 564, 2026-10-01-readability-format-ci 120. 반복 빈도를 원천 행으로 세기 위해서다.
- BACKLOG: 검색 뒤 겹치는 행 34·70·149·223·270·281·283·285·312·375·381만 읽었다.
- 현재 문서: `DEVELOPMENT.md` 26~30행(WSL 명령 형태), `domains/protocol.md` 절 제목과 37행, `FEATURE_MAP.md` 9행, `98_Shared/Protocol/ProtocolVersion.cs` 89행. GR-02·후보 1과 WSL 사례 확인용이다.
- 근거 폴더 원시: verify `contract-v1.md` 103행(Unity 명령), `worker/context-memo.md` 62~63행, `unity/timeout-calculation-error-2.json`, `unity/completion-2-correction.json`, `raw/v3-supplement-msg_578eee12a3eb.txt` 12행, `raw/wsl-test-2.txt` 경고 행, `approval-01/compile-red-02/main-date-guidance-ack.md`, 계약 4개와 task-spec들의 `DateKind` 수, `lead-opus-entry-20261028/main-ci-ready-blocked-v1.txt`.
- gh 읽기: `gh run list`(branch), 실패 2 run의 `gh run view --json jobs`·`--log-failed`, `77591936` code-rules run의 단계 목록, `gh pr view 270`.
- 읽지 않은 것: `99_Tools/Start-PersistenceServer.ps1`(계약 15행), closeout-01의 launch·first-screen JSON(최초 실행 명령은 계약 5행 값만 인용), 구현자·검증자의 다른 raw, 다른 goal의 원시.

## 자기 쓰기 감사

- **직접 쓰기:** 이 파일 한 개다. Write 1회(사전 맥락), Edit 1회(본문 추가), 수치·문구 보정 Edit 6회(이 줄을 고친 Edit 포함)다. 마지막 쓰기 시각은 worker_done 본문에 적는다.
- **우편함:** heartbeat 6회(`msg_ef75252475cd`·`msg_01ead3e5ecd5`·`msg_ecccacac5cd3`·`msg_ba7904310de1`·`msg_efb8b3afea1c`·`msg_e8760c17519c`)와 check 1회(메시지 0)를 파이프·리디렉션 없이 단독 호출했다. 이 파일 확정 뒤의 check·worker_done은 여기에 적지 않는다.
- **셸 출력 버림:** 우편함 명령이 아닌 조회에서 `> /dev/null`을 세 번 썼다(`gh run view --log-failed` 1회, `git diff --name-only` 1회, `sort -u` 1회). 파일 쓰기가 아니며 같은 명령을 변수로 다시 읽었다.
- **cwd 이동:** 읽기 조회 중 `cd`로 근거 폴더(closeout-01, verify `worker`, goal 근거 루트)에 세 번 들어갔다가 world-active로 돌아왔다. 쓰기는 없었다.
- **런타임 임시:** 지정 TEMP `.backups/tmp/wgard1` 아래 `claude` 폴더는 런타임이 만들었다. 이 세션이 직접 넣은 파일은 없다.
- **Git:** `git status --short`는 시작과 같이 `M 99_Tools/Start-PersistenceServer.ps1` 한 줄이다. `.backups`는 Git 밖이다. index·commit·push·fetch는 0이다.
- **실행:** 빌드·시험·SQL·DB·WSL·dotnet·Unity·서버·harness 실행은 0이다. 허용 범위의 파일 읽기·git·gh 읽기·hash 계산(`sha256sum`·node)·`date`만 했다.
- **허용 밖 쓰기:** 확인된 것은 0이다. 전체 OS 쓰기 추적은 미측정이다.
