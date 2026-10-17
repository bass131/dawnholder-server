# PR249 - 길드 금고·거점 성장·무기 강화 서버 종료 Gardener

## 사전 맥락(최초 메모)

- 작성 시각: 시계 출력 `2026-10-17T18:27:08Z` / `2026-10-18 03:27:08 KST`(직전 `date -u`·`TZ=KST-9 date`). 이 절이 이 세션의 첫 성공 파일 쓰기다. 그 전 scratchpad 리디렉션 1회는 SessionGuard memo-first가 차단해 파일이 생기지 않았다(아래 자기 쓰기 감사에 기록).
- 역할: `[World Gardener]`, Task `task_4b4d245146b5` / Dispatch `ctx_4eca52978d4f`, 리드 Run `run_cda82a34d041`, 자기 handle `term_360806a9-c034-4e47-b800-2f545ef65d5e`. 요청 모델 `claude-opus-5-5`, 세션 자기 인식도 같은 ID다. 최초 실행 명령·화면은 리드 원시(같은 폴더 launch-command.txt·screen-before-dispatch.json)이며 이 세션은 아직 읽지 않았다. backend 실제 모델은 `unknown`이다.
- 기준 SHA: HEAD = origin/main(로컬 ref) = `f6bf03fcf8067ec837ec8ccec944990f91c225a4`, branch `docs/guild-server-closeout-20261018`. 이 commit의 부모는 `48aabbd5d23c8ec7021ff22282c90f295db64c67`(첫 부모, 이전 main)와 `256372f32968b45c558cec46cc89eb45c9ffc6b8`(PR249 head)다. 제품 diff는 첫 부모→merge로 계산한다. 이번 리드 문서 diff는 base→working tree의 `goal.md`·`CURRENT.md` 2파일(`git status --short` 실측)이다.
- 고정 입력: 같은 폴더 inputs.json 28파일의 SHA256·bytes를 착수 직후 node로 재계산해 28/28 일치했다. 계약 SHA256 `60F13A35…2B1D8`은 sha256sum 실측과 path-check.json 값이 같다. path-check의 `OutputExists=false`와 일치하게 이 파일을 새로 만든다.
- 읽은 절(메모 시점): AGENTS·CLAUDE 전체(세션 주입, hash는 고정 입력과 일치), 계약 v1 전문, inputs.json·path-check.json, goal.md 3~58행(최신 재개 세 절)·238~336행(진척·요구 원천·범위·G1~G8·확정 기본값)·447~610행(사용자 결정 1~4·동결 뒤 정리·운영 결정·PR 경계·남은 위험), 예시 gardener.md 1~50행.
- 선택 원문: 계약에 실린 CODE_CONVENTION 「파일 위치와 이름」·「주석과 문서」·「역할별 적용」·「변화 평가」(교정 층·하네스 원칙·가독성), REPORTING 「수치와 원시 근거」, ORCA 「목표 종료 Gardener 4주 파일럿」. 상태와 책임·Unity와 이름·언어별 코딩 절은 제품·시험·도구 코드를 쓰지 않아 제외한다(계약 20행의 제외 이유와 같다).
- 기존 예시: `01_Phases/goals/2026-10-14-sol-xhigh-routing-hotfix/gardener.md`의 「사전 맥락 → 결론 → 범주별 표 → 기록 대조 → 후보 → 첫 발생 → 실행/미실행 → 자기 쓰기 감사」 구조만 참고한다. 결론은 이 goal 원천으로 독립 작성한다.
- 재사용: 범주 판정 표 형식과 「첫 발생 기록(후보 아님)」 구분을 예시에서 가져온다. 새 helper·스크립트는 만들지 않고 git·node 한 줄 조회를 stdout으로만 읽는다.
- 영향 파일: 이 파일 한 개. goal.md·CURRENT.md·다른 근거·BACKLOG·규칙은 읽기만 한다. 리드가 완료본을 goal/gardener.md로 bytes 그대로 복사한다.
- 배치와 이름: 계약이 지정한 `closeout-01/gardener-01/context-gardener-report.md`. SessionGuard memo-first의 `.backups/verification/<goal>/…context….md` 조건을 만족한다. 제품 이름 금지 규칙은 비해당이다.
- 점검 계획: 다섯 범주(독립 결함·CI 실패·새 경고 억제/설정 완화·임시 우회·드리프트/규칙 검사)를 원천 report/verdict/정산 → 필요한 raw → 제품 diff 키워드·파일 유형 조회 순으로 본다. 큰 JSON은 필드·count만 읽는다.
- 관찰(메모 시점, 확인 전): 현재 goal 238~251행의 「진척 단계」가 PR249 병합 뒤에도 `[>] 독립·봇 검증`·`[ ] 제품 병합`으로 남아 있다. 253행 규칙(「PR 번호가 생기면 해당 병합 단계에 실제 PR번호 병합을 적는다」)과 대조해 반환 여부를 판단한다.
- 운영 제약 관찰: SessionGuard mailbox-output-loss가 `| cat` 파이프를 막았다. 계약은 receipt 파일 쓰기를 금지하므로 우편함 명령은 파이프·리디렉션 없이 단독 호출로 출력을 직접 읽는다. 첫 heartbeat `msg_a0f49c010a78`이 그렇게 성공했다.
- 질문: 현재 없음.

## 결론

- **새 독립 제품 결함은 없다.** 원래 판정은 strong / deferred / ENV-D01(환경·계약 경계)이다. 메인이 전달한 사용자 선택 A에 따라 리드가 「ENV-D01 한 줄 차이를 명시한 조건부 통과」로 정산했다. 이 Gardener는 그 판정을 PASS로 다시 쓰지 않는다.
- **CI 실패는 없다.** 저장된 GitHub 관측에서 최종 head의 4개 검사가 모두 SUCCESS다.
- **새 경고 억제·설정 완화는 없다.** 제품 diff에서 설정 파일 변경과 억제 키워드 추가 행이 0이다. PR249가 만든 새 경고 4건은 억제하지 않고 goal 「동결 뒤 정리」로 미뤄져 있다.
- **제품 코드의 임시 우회는 없다.** 운영 쪽 임시 예외 1건(하루 한정 600초 생존 간격)은 이미 만료·대체됐다.
- **규칙 파일 드리프트는 없다**(로컬 origin/main 기준). 대신 PR249가 고친 봇 회귀 목록의 수기 드리프트를 git 이력으로 확인했고, 시나리오 3개가 아직 목록 밖이다.
- 정리 후보는 **2개**다. 후보 1은 봇 시나리오 등록과 회귀 목록의 드리프트 검사, 후보 2는 TRX 시험 결과 집계의 정본 helper다.
- 리드 반환은 **비차단 2건**(GR-01 진척 단계 미갱신, GR-02 동결 뒤 정리 1의 줄 번호 이동)이다. 제품 차단 반환은 없다.
- 이 보고는 goal 종료·사용자 승인·새 정책 채택을 선언하지 않는다.

## 점검 범주별 결과

제품 diff는 병합 commit의 첫 부모 `48aabbd5…`→`f6bf03fc…`로 계산했다. 결과는 64파일 +8337/-116이다(`git diff --shortstat`). PR249의 GitHub 기준인 merge-base `63aab6ba…`→최초 head `b1b2de1d…`는 64파일 +8336/-116이고, 최종 head `256372f3…`는 goal.md +4/-3이 더해진 값이다. goal 22행의 「64파일/8336추가·116삭제」는 최초 PR 기준 값과 일치한다.

| 범주 | 확인 범위·명령 | 결과 |
|---|---|---|
| 독립 결함 | strong-review-01 `verdict.md` 전문(SHA256 `AB8596CC…` 일치)·`report.md`·`unity-drift-analysis.md` 관련 행·`lead-settlement-note.md`, lead-pr-01 `lead-conditional-settlement.md`, goal 680~697행 | 검토했고 새 지적 없음. 최종 판정의 제품 확정 결함은 0이다(verdict 21행). 보류 사유 ENV-D01은 환경·계약 경계, OPS-V01은 운영, OBS-UI01은 승인된 한계, STYLE-01/02는 비차단 후속이다. 기초 단계 독립 결함 D-01·D-02는 재검증에서 해소됐다는 goal 기록만 읽었고 원시는 열지 않았다. |
| CI 실패 | closeout-01 `pr249-merge-observation.json`의 `statusCheckRollup`, `entry-facts.json`의 `Checks` | architecture-tests/test, code-rules/check, dotnet-tests/test, module-boundaries/check 4개 모두 COMPLETED·SUCCESS(완료 17:54:21Z~18:13:13Z). 저장 관측이며 gh 조회는 하지 않았다. 최초 head `b1b2de1d…`의 CI 결과는 저장 관측이 없어 미관측이다. |
| 새 경고 억제·설정 완화 | 변경 64파일에 설정류 정규식(`.editorconfig`·`.props`·`.targets`·`.csproj`·`.sln`·`.github/`·`.json`·`.asmdef`·`.yml`·ruleset·`.ps1`·`.sh`·settings) 적용. `.md` 제외 추가 행에 `NoWarn\|pragma warning\|SuppressMessage\|dotnet_diagnostic\|severity *= *none\|WarningsAsErrors\|TreatWarnings\|Skip *=\|\[Ignore\|\[Explicit\|continue-on-error\|--no-verify\|bypass\|TODO\|HACK\|FIXME\|XXX\|workaround\|temporar\|임시\|우회\|억제\|완화` grep | 설정류 1건은 `99_Tools/run_bot_regression.sh`이며 시나리오 5개를 더한 변경이라 완화가 아니다. 키워드 추가 행은 0행(exit 1)이다. 새 경고는 아래 「경고 귀속」에 따로 적는다. |
| 임시 우회 | 위 키워드 grep의 `.md` 추가 행 10행을 직접 읽음 | 제품 코드 0. 우회의 실체는 goal 122행의 운영 임시 기준 1건뿐이다(메인 `msg_0998eeebe75b`, 하루 한정 600초 생존 간격). goal 216행은 적용 0, 482행은 현행 판정에서 다시 쓰지 않는다고 기록했다. 나머지 9행은 금지 문장(「우회 재개하지 않는다」·「완화하지 않는다」 등)이나 승인된 임시 시연 수치를 한 출처로 모은다는 설계 문장이다. ENV-D01 조건부 수락은 우회가 아니라 사용자 결정의 한정 예외이며 goal 11·20·21행에 정확한 한 줄 범위와 원문 deferred 유지가 연결돼 있다. |
| 드리프트·규칙 검사 | `git rev-parse HEAD origin/main`; `git diff --stat origin/main -- AGENTS.md CLAUDE.md .claude/settings.json .agents/`; 고정 입력 28개 node 재해시; PR249가 추가한 문서 링크의 경로 존재; 봇 시나리오 대 회귀 목록 비교 | 규칙 파일 diff는 빈 출력(exit 0)이다. 01_Phases를 뺀 변경 문서 4개의 추가 링크 고유 21쌍은 경로가 21/21 존재한다(앵커 미검사). 봇 회귀 목록은 base에서 7개, 현재 3개가 목록 밖이다(후보 1). fetch를 하지 않아 원격의 더 새 상태는 미관측이다. |

### 경고 귀속(실측)

원천은 strong-review-01 `execution-raw/Build/stdout.txt`(43,354 bytes)다. `file.cs(행,열): warning 코드` 형식 행을 `sort -u`한 고유 위치는 46개이고, MSBuild 요약은 `50 Warning(s)`다. 차이 4는 같은 위치가 프로젝트별로 다시 출력된 것으로 추정하며 확인하지 않았다.

| 묶음 | 고유 위치 | 근거 | 상태 |
|---|---|---|---|
| PR249 신규 파일 `GuildRegistry.cs` | 4: IDE0011 138·225·328행, SA1201 375행 | `git diff --name-status` 상태 A | 실측 신규. goal 「동결 뒤 정리」 1·5·6에 등록돼 있다. |
| PR249 수정 파일 `GameWorld.cs` | 2: SA1201 141행, SA1202 169행 | `git blame` 결과 두 행 모두 base 조상 commit `bcafb6bc`(Core 영속화) 소유 | 귀속 unknown. 순서 규칙이라 주변 삽입의 영향 여부를 판단하지 않았다. |
| PR249 미변경 파일 | 40 | diff에 해당 파일 없음, 분석기 설정 파일 변경 0 | 기존 경고로 추론 |

goal 12행의 「경고50은 기존/신규 귀속 unknown」은 동일 조건 기준선이 없다는 뜻이라 틀리지 않다. 위 실측으로 4건은 신규 귀속이 확정되므로, 리드가 원하면 부분 귀속을 덧붙일 수 있다(반환 아님).

## 발생 수와 원천

같은 사례를 두 번 세지 않았고, 제품 확정 실패와 운영·환경 실패를 합치지 않았다.

| 사례 | 수 | 원천 | 상태 |
|---|---|---|---|
| 제품 확정 결함(최종 판정) | 0 | verdict 16~21행 | 판정 원문 |
| 기초 단계 독립 결함 D-01·D-02 | 2, 해소 | goal 682~697행 | goal 기록 인용, 원시 미대조 |
| whole 실패 INT-FAIL-01~03과 TEST-F01 | 4, 분류 a3·b1 | verdict 112~121행 | 판정 원문. 서로 다른 시험이라 확정 실패 3회로 세지 않는다(goal 48행과 같음). |
| CI 실패 | 0/4 | `pr249-merge-observation.json` | 저장 관측 |
| 새 경고 | 신규 4, unknown 2, 기존 추론 40 | Build `stdout.txt` | 위 표 |
| 경고 억제·설정 완화·제품 임시 우회 | 0 | 제품 diff grep | 실측 |
| 운영 임시 예외 | 1, 만료 | goal 122·216·482행 | goal 기록 인용 |
| 환경 차이 ENV-D01 | World D 실행 1 | verdict 3·104행, `unity-drift-analysis.md` 22~24행 | 판정 원문. 다른 파트 반복은 기존 BACKLOG 행이 다룬다. |
| 운영 OPS-V01 | 신호 44, heartbeat 28, helper 초과 11구간, heartbeat 쌍 초과 16구간, 형식 위반 5 | strong-review-01 `lead-settlement-liveness-reasons.json` 필드 `WorkerSignals`·`HeartbeatCount`·`HelperOverGapCount`·`HeartbeatOnlyOverGapCount`·`FormatViolationCount` | 리드 원시. 한 검증 세션의 값이다. |
| 봇 회귀 목록 밖 시나리오 | base 7 → 현재 3 | `git show <commit>:99_Tools/headless-bot/Program.cs`와 `…/run_bot_regression.sh` 비교 | 실측(후보 1) |
| TRX Counters와 행의 NotExecuted 차이 | 직접 실측 TRX 2개 + 리드 노트 1개 | 두 `whole.trx`의 `<Counters>`와 `<UnitTestResult>` 행 outcome 집계 | 실측(후보 2) |

## 종료 기록 대조

대상은 base→working tree의 리드 문서 diff다. `git diff --numstat` 실측은 goal.md +10/-0, CURRENT.md +1/-1이다.

| 항목 | 기록 위치 | 원천 | 결과 |
|---|---|---|---|
| 병합 승인 출처 | goal.md 9행 | `main-decision-originals.json` Messages[0]: `msg_783227d00ff6`, from `term_aa109e71…`, to `run:run_cda82a34d041`, created 2026-10-17T18:20:31Z, 본문의 승인 줄 「병합 승인: PR249 head 256372f3…」 | 일치. 메인 전달로 표기했고 직접 사용자 입력으로 격상하지 않았다. |
| head·병합 commit·시각 | goal.md 9행 | `pr249-merge-observation.json` headRefOid·mergeCommit·mergedAt `18:19:04Z`, `entry-facts.json` MergeParents, 로컬 `git log` 부모 | 일치. 03:19:04 KST는 UTC+9 계산과 맞다. |
| CI | goal.md 9행 | 위 4 SUCCESS | 일치 |
| B·D 수치 | goal.md 11행 | B: strong-review `whole.trx` 행 1642 = Passed 1637 + NotExecuted 5(Counters total1642/executed1637/notExecuted0). D: `editmode.xml`의 `<test-run>` total493·passed493·failed0·skipped0, test-case 493행 모두 Passed. 두 파일 SHA256 앞자리 `5278062893c8f779`·`432f051ab65a42dc`가 report 기재값과 같다. | 일치(실측) |
| C 수치 | goal.md 11행 | verdict·report 인용만 읽음 | 원시 미대조 |
| ENV-D01 범위와 원문 보존 | goal.md 11·20·21행 | `msg_e951138f3f3a` 본문, `lead-conditional-settlement.md`, verdict 3행 | 일치. 사용자 수락이 원문 deferred를 고치지 않고 리드 정산에 연결돼 있다. 「원본과 완전히 같은 조건」 주장은 없다. |
| 미실행 | goal.md 12행 | verdict 38행, `lead-conditional-settlement.md` 미실행 줄 | 일치. CI는 PR 뒤 실제로 돌아 목록에서 빠진 것이 원천과 맞다. |
| 진척 단계 | goal.md 249~250행 | 253행 규칙 | 불일치 → GR-01 |
| 동결 뒤 정리 1의 위치 | goal.md 544행 | 현재 빌드 원시의 IDE0011 위치 | 이동 → GR-02 |
| CURRENT World 줄 | CURRENT.md diff | branch만 `docs/guild-server-closeout-20261018`로 교체 | 지적 없음 |

## 리드 반환

- **GR-01** · `goal.md:249~250` · 진척 단계가 `[>] 독립·봇 검증`, `[ ] 제품 병합`으로 남아 있다. PR249는 2026-10-17T18:19:04Z에 병합됐고, 253행은 「PR 번호가 생기면 해당 병합 단계에 실제 `PR번호 병합`을 적는다」고 정한다. · 대안: 독립·봇 검증을 조건부 통과로 완료 표시하고 제품 병합에 `PR249 병합`을 적는다. · 제품 비차단. 종료 기록 PR 전 반영을 권한다.
- **GR-02** · `goal.md:544` · 「동결 뒤 정리 1」의 `GuildRegistry.cs:124/211`은 이전 빌드 기준이다. 이번 strong-review 빌드 원시에서 같은 IDE0011은 138·225행이다. · 대안: 현재 행 번호나 감싼 메서드 이름으로 바꿔 동결 뒤 작업자가 바로 찾게 한다. · 비차단. 동결 뒤 정리 때 처리해도 된다.

## 정리 후보

### 후보 1 - 봇 시나리오 등록과 회귀 목록의 드리프트 검사

- **반복(실측):** base `48aabbd5…`에서 `Program.cs`가 분기하는 시나리오 24개 중 7개가 `run_bot_regression.sh` 목록(17개) 밖이었다. 개수는 `string.Equals(scenarioName, "…")` 23개와 `scenarioName == "M2BasicMovement"` 1개를 합친 값이다. `git log -S`로 본 첫 추가 commit은 PartyQuest `fc05c79f`(06-15), InventorySmoke `0f956d08`(10-05), InstanceMapIsolationSmoke `2bbd710d`(10-10), DungeonClearSmoke `2e2c5c92`(10-11), PersistenceReconnect·PersistenceRestart `bcafb6bc`(10-15)다. 모두 목록 등록 없이 들어왔다. BossGate는 `e5db1854`(06-19)에 목록에 들어갔다가 `bf93c66c`(09-29) 스크립트 재작성에서 빠졌다. 서로 다른 commit 6개의 사건이다.
- **이번 goal:** PR249의 `e2744dc7`이 4개를 복원하고 GuildGrowthSmoke를 더해 목록이 22개가 됐다. tooling.md 27행이 이 추가를 적었다. 현재 `Program.cs` 25개 중 InstanceMapIsolationSmoke·PersistenceReconnect·PersistenceRestart 3개가 목록 밖이고, tooling.md에는 제외 이유가 없다(`git grep` 0행). Persistence 둘은 별도 계정·phase 인자를 받으므로 DB 의존이라 뺐을 가능성이 있다(추론).
- **영향:** 목록 밖 시나리오는 「원본22봇 회귀」 같은 전체 회귀 주장에서 조용히 빠진다. 시나리오를 추가한 사람이 목록을 잊으면 회귀 범위가 줄어도 아무 검사가 알리지 않는다.
- **검사화(높은 층부터):**
  - 구조: headless-bot에 시나리오 등록표 하나를 둔다(이름 → 실행 함수, 회귀 포함 여부, 제외 이유). `run_bot_regression.sh`는 그 등록표의 출력으로 목록을 받는다. 수기 목록 자체가 없어진다. `Program.cs`에 비교 형식 두 가지가 섞여 있어 정적 파싱 검사도 깨지기 쉬우므로 이 층이 가장 확실하다.
  - 시험(대안): 기존 시험 프로젝트에 `Program.cs` 시나리오 이름과 `.sh` 목록을 비교하는 정적 시험을 둔다. 빠진 이름마다 「목록에 넣거나 제외 manifest에 이유와 함께 적어라」를 진단으로 낸다. 하네스 원칙 4대로 warning 파일럿부터 시작한다.
- **현재 차단:** 아니다. **후속:** 시연 동결 뒤 후보이며 채택은 메인을 거쳐 사용자가 정한다. 99_Tools 담당 파트는 CodeMap 분석과 코드 주인 파트가 정한다. 이 Gardener는 구현·BACKLOG 등록을 하지 않았다.

### 후보 2 - TRX 시험 결과 집계의 정본 helper

- **반복(실측):** 이 goal의 세 세션(integration-complete-01, integration-repair-01, strong-review-01)이 각자 TRX 결과를 읽는 스크립트를 새로 썼다. `UnitTestResult\|NotExecuted` 문자열을 담은 스크립트는 6개다(`grep -l` 실측, 파일별 파싱 범위는 미검토). 저장소의 `99_Tools`·`.github`에는 같은 책임의 helper가 없다(`git grep` 0건).
- **함정(실측):** 두 `whole.trx` 모두 `<Counters … notExecuted="0">`인데 행 기준 NotExecuted가 5개다. strong-review는 total1642/executed1637이고 행은 Passed1637·NotExecuted5다. integration-complete는 total1634/executed1629이고 행은 Passed1626·Failed3·NotExecuted5다. integration-repair는 리드 노트 5행이 같은 차이를 기록했다(직접 미대조). integration-complete `report.md` 4절은 이전 요약의 Counter 기반 NotExecuted0을 행 기준 5로 정정했다고 적었다. 실제 오보고가 1건 있었다는 뜻이다.
- **영향:** Counters만 읽는 새 helper는 미실행 5개를 0으로 보고한다. 「미실행을 통과로 쓰지 않는다」는 보고 기준을 깨는 경로다. 지금은 세션마다 계약에 이 함정을 다시 적어야 한다(strong-review 계약 v1~v3·contract-core에 언급, `grep -l` 실측).
- **검사화:** 99_Tools에 TRX 집계 helper 하나를 정본으로 두고 행별 outcome 집계·Counters 원값·둘의 차이 진단을 함께 낸다. fixture 시험은 위 두 TRX를 줄인 사본(Counters notExecuted=0과 NotExecuted 행)으로 「차이를 경고로 낸다」를 고정한다. 대안인 DEVELOPMENT 문서 안내는 세션마다 helper를 새로 쓰는 흐름을 막지 못한다. REPORTING 「수치와 원시 근거」(37행 문단)가 「실행 근거 helper의 정본화는 후속 범위」라고 이미 적은 범위와 맞닿는다.
- **현재 차단:** 아니다. **후속:** 시연 동결 뒤 후보다. 기존 BACKLOG `powershell-all-evidence`(문서 근거 인용·집계 비교 helper)와 책임이 다르며, 겹침 판단은 메인이 한다.

## 첫 발생·기존 후보 연결(후보 아님)

- **ENV-D01:** BACKLOG `unity-sentis-define-drift`(202행)가 같은 SENTIS 한 줄을 이미 다룬다. 이번 World D 실행은 사례 하나를 더한다. 사용자 선택 A의 「정확히 같은 한 줄만 같은 기준」을 그 행에 연결할지는 리드·메인이 판단한다.
- **OPS-V01(생존 신호):** BACKLOG `worker-liveness-tool-check`(217행)·`liveness-helper-followup`(275행)·`heartbeat-interval-review`(277행)가 이미 있다. 새 후보로 만들지 않는다.
- **PowerShell JSON 시각 자동 변환:** integration-repair `report.md` 24행의 첫 plan 승인 관문 exit1이다. World 범위에서는 1회이고, 이후 계약에 `-DateKind String`을 넣어 strong-review에서는 재발하지 않았다(verdict 25행). BACKLOG에 같은 행은 없다(`grep` 0건). 다른 파트의 반복은 이 세션이 원시로 확인하지 않았다.
- **우편함 check와 실행을 한 batch로 묶은 오류:** 메인 `msg_793fa17c688d`가 두 번째 발생으로 보고 운영 지시를 이미 적용했다(goal 55행). 새 후보가 아니다.
- **옛 세부를 단정한 시험:** TEST-F01(전체 수신 순서)과 INT-FAIL-03(내부 로그)은 verdict 분류 a다. INT-FAIL-01은 의도된 프로토콜 prefix 단정이 48 추가로 바뀐 경우라 같은 유형으로 세지 않았다. 같은 유형 2건이라 관찰로만 남긴다.
- **일회성 helper 오류:** 리드의 PSCustomObject 인덱싱(lead-pr-01 `final-lead-report.md`), 검증자의 semantic size 접두사·apply_patch context 2건·Where-Object 줄바꿈·JS store·e-audit import(strong-review `report.md` 5절), 구현자의 HOME 가정 관문 실패(integration-complete `report.md` 7절), 이전 C 준비의 빈 배열 `.Count` 관문 오류(goal 89행)다. 유형이 서로 달라 한 사례로 합치지 않았고, 후보 2의 「세션마다 helper를 새로 씀」 배경으로만 둔다.

## 실제 준수

| 규칙 | 실제 위치 |
|---|---|
| task-context 첫 메모 | 「사전 맥락(최초 메모)」이 이 세션의 첫 성공 쓰기다. SessionGuard memo-first 경계를 만족했다. |
| 파일 위치와 이름 | 계약 지정 경로 한 파일. 제품 이름 금지 규칙은 비해당이다. |
| 주석과 문서 | 긴 원시를 복제하지 않고 근거 경로·필드·행으로 연결했다(「점검 범주별 결과」~「종료 기록 대조」). |
| 역할별 적용 | 문서 실사 등급이다. 검토·미검토·미대조·미관측을 표마다 구분했다. |
| 교정 층과 반복 규칙 | 후보마다 구조→시험→문서 순으로 가장 높은 층을 먼저 적었다. 1회 사례는 후보로 올리지 않았다(「첫 발생·기존 후보 연결」). |
| 하네스 원칙 | 후보 1은 원칙 3(수기 목록 드리프트), 후보 2는 원칙 1·2(진단·정본 helper)다. 새 검사는 원칙 4의 warning 파일럿부터 적었다. |
| 수치와 원시 근거 | 모든 수치에 파일·필드·명령을 붙였고 실측·추론·미대조를 표시했다. 합산하지 않은 항목을 명시했다. |
| ORCA 목표 종료 Gardener | 후보 최대 2개와 검사화 방법을 적었고, 실행불가와 위반을 구분했다. 채택·구현·BACKLOG 작성은 하지 않았다. |

## 실행 기록과 미실행·한계

- **실행:** `git --no-optional-locks` 조회(rev-parse, status, diff, log, show, blame, grep, merge-base, cat-file), `sha256sum`·node 해시 대조, grep·awk·sed·node 정적 읽기, `date`, 이 파일 확정 전까지 Orca heartbeat 8회(`msg_a0f49c010a78`·`msg_61ce6d61e397`·`msg_1769ea373d8f`·`msg_a8ef8c97d7c4`·`msg_913c1fc00b63`·`msg_f249d1f67faf`·`msg_28ec4e909428`·`msg_8ec9c318ea39`)와 check 2회(둘 다 메시지 0)를 했다. worker_done 직전의 마지막 check는 이 파일 확정 뒤라 여기에 결과를 적지 않는다. 모든 출력은 stdout으로만 읽었다.
- **미실행:** 빌드·시험·Unity·WSL·DB·7777·봇·gh/GitHub 조회·fetch. CI는 저장 관측만 썼다.
- **한계:** 경고 고유 46은 경로·행·열·코드 기준 `sort -u`이며 요약 50과의 차이 4는 확인하지 않았다. 링크 검사는 경로만 봤다. 봇 시나리오 수는 `Program.cs`의 두 비교 형식 기준이다. C 22봇 원시, D-01·D-02 원시, 다른 파트의 SENTIS 원시는 열지 않았다. 원격 main은 관측하지 않았다.
- **이 세션의 명령 오류:** screen JSON을 읽는 node 한 줄이 정규식 따옴표 오류로 실패했다(파일 쓰기 없음). grep으로 다시 읽어 화면 표시 「Opus 5.5 ⚡xhigh」를 확인했다. 최초 실행 명령은 `launch-command.txt`의 `claude --model claude-opus-5-5`이고 backend 실제 모델은 `unknown`이다.

## 자기 쓰기 감사

- **직접 쓰기:** 이 파일 한 개다. Write 1회(사전 맥락), Edit 1회(본문 추가), 문구·실행 기록 보정 Edit 3회다. 이 Edit들이 이 세션의 마지막 쓰기다.
- **차단된 시도:** 첫 heartbeat 출력을 scratchpad `hb1.txt`로 리디렉션하려던 명령 1회가 SessionGuard memo-first에 막혔다. 파일은 생기지 않았다(`ls` 확인). 이어 `| cat` 파이프를 붙인 heartbeat는 mailbox-output-loss 가드에 막혔다. 계약이 receipt 파일을 금지하므로 이후 우편함 명령은 파이프·리디렉션 없이 단독 호출로 출력을 읽었다. 가드를 우회하지 않았다.
- **런타임 임시:** 지정 TEMP `.backups/tmp/gsg1` 아래의 Claude 런타임 scratchpad 폴더는 런타임이 만들었고 비어 있다.
- **Git:** `git status --short`는 시작과 같이 goal·CURRENT 2파일 M뿐이다. `.backups`는 ignored다. index·commit·push는 0이다.
- **허용 밖 쓰기:** 확인된 것은 0이다. 전체 OS 쓰기 추적은 미측정이다.
