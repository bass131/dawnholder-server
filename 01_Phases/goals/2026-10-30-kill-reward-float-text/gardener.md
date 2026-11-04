# [Effect Gardener] Effect 세 goal 종료 점검 보고 — 공격 키 왼쪽 Ctrl·처치 보상 글·PlayMode 전제 건너뜀

**결정 76 A 예외(정본과 다른, 이번 한 번의 사용자 결정 예외).** ORCA 「목표 종료 Gardener 4주 파일럿」 정본은 goal마다 종료 Gardener를 하나씩 두지만, 이번에는 사용자 결정으로 끝난 세 goal(기본 공격 키 왼쪽 Ctrl·처치 보상 떠오르는 글·PlayMode 전제 건너뜀)에 Gardener 하나와 보고서 한 파일을 둔다. 사용자 원문은 메인 전달 `msg_84d148dc5236`의 「대시보드 결정 응답: 2) 76 - Effect 종료 점검(Gardener)을 끝난 goal 셋에 하나로 묶을지 → A 하나로 묶음 · …」다. 묶었다고 범주나 대조를 줄이지 않고, 각 goal의 입력은 그대로 본다. 보고서가 하나라서 정리 후보는 세 goal을 합쳐 최대 두 개다.

## 사전 맥락(최초 메모)

- 역할·자기 태그 / 할당 작업 / 지정 모델·관찰 모델·backend unknown 여부: 신규 읽기 전용 점검자 `[Effect Gardener]` / Effect 세 goal 종료 Gardener 하나(계약 v1, 결정 76 A) / 지정 `claude --model claude-opus-5-5 --effort xhigh`(리드 기동 기록 `gardener-launch-record-01.md`의 split `--command` 원문). 첫 화면 표시는 기동 기록이 옮긴 「Opus 5.5 with xhigh effort · Claude Max」다. 자기 인식 모델은 시스템 안내의 `claude-opus-5-5`(Opus 5.5)다. backend 실제 모델은 unknown이다.
- Task/Dispatch·handle: task `task_804c48ba36e8`, dispatch `ctx_2ada0118bc03`, run `run_5f3209972e68`(첫 check 응답), 자기 handle `term_feb9fa3b-7e28-4de4-9676-d564a39c34d2`(`ORCA_TERMINAL_HANDLE` 이름 지정 조회와 일치), 리드 handle `term_a9a632ac-e7eb-4ebf-92bc-55b8405f3a86`.
- 현재 goal·할당 계약의 경로 / 작업 경로·branch·base·HEAD: 계약 `.backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-contract-v1.md`(sha256 `be0e5da37be0c9ee097264937401318b341a161c953d912a29c9a46ceec8ec1d`, 78행, 포인터의 기대값과 일치) / 작업 경로 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/effect-active`, branch `docs/effect-closeout-20261031`, HEAD = origin/main = merge-base = `f5adcf31e605af3b30581746c20a64642bb144fc`. 대상 goal 셋은 `01_Phases/goals/2026-10-30-attack-key-left-ctrl/`, `…/2026-10-30-kill-reward-float-text/`, `…/2026-10-30-playmode-precondition-ignores/`이며 판정 근거는 고정 복사본이다.
- 메모 작성 시점 / 대상 파일 첫 쓰기 전 여부: `date -u` 출력 `Sat, Oct 31, 2026 10:23:13 AM`, `TZ=KST-9 date` 출력 `Sat, Oct 31, 2026  7:23:13 PM`. 착수 시각은 `Sat, Oct 31, 2026 10:20:45 AM`(KST `7:20:45 PM`)이다. 이 메모가 보고서의 첫 쓰기다.
- 기존 사용자 변경·다른 작성자의 소유권: `git status --short`에 리드의 미커밋 변경 다섯(`00_Document/operations/BACKLOG.md`, `00_Document/operations/CURRENT.md`, 세 goal의 `goal.md`)이 있다. 리드 소유이며 읽지도 판정 근거로 쓰지도 않는다. 고정 복사본만 쓴다.
- 허용 쓰기·실행·금지 범위: 쓰기는 이 보고서 한 파일이다. 실행은 git 읽기, `sha256sum`, `wc`, `date -u`, `TZ=KST-9 date`, 정본 helper 두 개(`check-candidates.mjs`, `check-liveness.mjs`)의 읽기 전용 실행, Read·Grep·Glob이다. 금지는 저장소 추적 파일·다른 근거 파일·고정 복사본 쓰기, `node -e`·PowerShell 식 계산 같은 저장하지 않은 인라인 분석, Unity·dotnet·gh·git 쓰기·Python, 설정 변경, Agent 도구의 쓰기·실행 위임, `ORCA_*` 전체 조회다. 우편함 명령은 파이프·리디렉션 없이 단독으로 부른다.
- 관문 적용 실행 대상 / 분석·raw·receipt·보고 경로 / TEMP 구분: 관문 실행 없음(제품·시험·도구를 쓰거나 돌리지 않는다). 보고 경로는 이 파일이다. TEMP·TMP는 `C:\Users\bass1\orca\workspaces\DawnHolder_Project\effect-active\.backups\tmp\effect-gardener-20261031`(이름 지정 조회로 확인)이며 Claude Code가 남기는 임시 파일은 도구 동작이다. 내가 직접 만드는 TEMP 파일은 없다.
- 원문 요구/후속 보충의 계약 버전·시각 / 고정 입력 경로·hash / 적용 단계: 계약 v1(기준 HEAD `f5adcf31…`, 입력 판 v2). 후속 보충은 첫 check 시점까지 0건이다(count 0). 고정 입력 목록 `gardener-inputs-v2.txt`(sha256 `d0fd2cc0506ba8b3d6f1205e1568113e421d49260fd0b7ad64875e536777115b`, 115행 = 머리 7행 + 파일 108행)를 다시 쟀다. 108파일 전부 `sha256sum` 값이 목록과 같다. 바이트 수는 sha256이 같으므로 같다고 본다. 규칙 원문 `gardener-rules-v2.md` sha256 `ed6afd5c02fff72a5a7e0f4d16338f3cdba3cf652c9253892f5a1945dc13e325`도 일치한다. 적용 단계는 종료 Gardener 하나다.
- 검증 등급 / R-7: 해당 없음. 이 점검은 독립 검증이 아니라 종료 기록 점검이며 판정(세 goal PASS, 결정 78 C)을 바꾸지 않는다. R-7 Fable 설계 검토 시범도 해당 없다(시범 목표가 아니다).

### 읽기와 선택

| 읽은 파일·절 | commit 또는 hash | 이 작업에서 필요한 규칙·경계 |
|---|---|---|
| 계약 `gardener-contract-v1.md` 전체 | `be0e5da3…ec1d` | 범주 다섯·요구항목 8·쓰기/실행 경계·보고 형식 |
| 포인터 `gardener-contract-v1-pointer.md` | `d5469e36…a2` | 첫 쓰기 범위(제목·76 A 문단·사전 맥락) |
| 고정 입력 목록 `gardener-inputs-v2.txt` | `d0fd2cc0…115b` | 108파일 고정 판, 복사본 다섯의 원본 대응 |
| 규칙 원문 `gardener-rules-v2.md`(CODE_CONVENTION 「파일 위치와 이름」·「주석과 문서」·「역할별 적용」·「변화 평가」, ORCA 「생존 신호와 초과 정산」·「목표 종료 Gardener 4주 파일럿」) | `ed6afd5c…e325`(기준 commit `f5adcf31`) | 교정 층과 반복 규칙(첫 발생·두 번째), 정리 후보 최대 둘·검사화 방법, 검사 실행불가와 실제 위반 구분, 300초 초과 정산 |
| `.agents/skills/dawnholder-task-context/references/templates.md` 「맥락 메모」 | 작업 트리(추적 파일, 미변경) | 이 메모의 항목 |
| 리드 기동 기록 `gardener-launch-record-01.md` | `f95a8701…66f3` | 지정 명령·첫 화면·dispatch 값 |

- 같은 책임의 예시 / 따를 관례: 형식 참고는 `01_Phases/goals/2026-10-24-character-select-create-ui/gardener.md`(sha256 `e82bc4b8…8e31`)의 절 구성뿐이다. 그 결론과 후보는 기대답으로 가져오지 않는다. 절 순서는 계약 요구항목 8을 따른다.
- 재사용 helper·계약: 정본 helper `99_Tools/Backlog/check-candidates.mjs`(goal마다 한 번, 모두 세 번)와 `99_Tools/Orca/check-liveness.mjs`(리드 정산 값 재현, 허용 범위 안에서만)를 쓴다. 새 helper·스크립트는 만들지 않는다. 대조는 Read·Grep 도구와 허용 명령 출력으로만 한다.
- 영향 파일·사용처·보존 동작: 쓰는 파일은 이 보고서뿐이다. 고정 복사본·근거 폴더·추적 파일은 보존한다.
- 신규 파일 위치·이름 / 근거: `lead/closeout-01/gardener/context-gardener-report.md`는 계약이 지정했고 폴더는 리드가 미리 만들었다. 새 폴더는 만들지 않는다.
- 읽기 상한을 넘긴 추가 경로: 세 goal 근거 폴더의 다른 파일은 계약이 허용한다. 읽은 것은 「추가로 읽은 것」에 경로와 sha256을 적는다.
- 언어 규칙: 해당 없음. 코드를 쓰지 않는다.
- 관련 규칙 절 선택 / 제외 이유: 규칙 원문 v2의 여섯 절 전부를 쓴다. 상태와 책임·Unity와 이름·빌드·언어별 절은 리드가 제외했다. 이 점검이 제품·시험·도구를 쓰거나 실행하지 않아서이며 나도 그 판단을 따른다.
- 열린 질문·차단 / 담당자: 현재 없음. 고정 입력 불일치 0이라 ask가 필요 없었다. 막히면 리드에게 공식 ask로 묻는다.
- 권한: 직접 수정·규칙 채택·다음 goal 발행·추가 위임·commit·push 권한 없음. 정리 후보 채택은 메인을 거쳐 사용자가 정한다.
- 메모 뒤 갱신(본문 작성 시점 `date -u` 출력 `Sat, Oct 31, 2026 10:39:31 AM`, `TZ=KST-9 date` 출력 `Sat, Oct 31, 2026  7:39:31 PM`): 계획대로 진행했다. 바뀐 점은 셸 명령이다. 계약 62행 허용 실행 목록 밖의 읽기 전용 셸 명령(`ls`·`cat`·`sed -n`·`cut`·`head`·`echo`·`cd`, 셸 `for` 1회, stdin 파이프의 `git grep --no-index` 1회, 옵션 오류로 끝난 `git grep -x` 1회)을 썼다. 상태를 바꾸는 명령은 없다. 전부 「실행 기록과 미실행·한계」에 적었고 「자기 쓰기 감사」에서 이탈로 센다. 앞 goal Gardener와 같은 목록이라 반복 빈도에도 넣었다(정리 후보 2).

## 결론

- **차단 관찰 0, 리드 반환 6(GR-01~GR-06), 정리 후보 2.** 세 goal의 현재 판정(독립 검증 PASS 셋, 사용자 플레이 「나머지는 괜찮음」 결정 78 C)을 바꿀 근거는 찾지 못했다.
- 고정 입력 108파일의 sha256은 목록과 모두 같다. 정본 helper 두 개를 읽기 전용으로 돌렸다. 후보 도착 검사는 공격 키·처치 보상 goal이 exit 0, PlayMode 전제 goal이 **exit 1**(259행 후보에 BACKLOG ID·goal 링크 없음, GR-01)이다. 생존 신호 helper를 리드와 같은 입력으로 다시 돌린 출력은 리드 저장본과 sha256이 같다.
- 종료 기록의 병합 시각·head·병합 commit·CI·main 통합 수치·플레이 창 측정·사용자 결정 원문은 원시와 맞는다. 원시와 다르거나 원시가 없는 사실 문장은 넷이다(GR-02~GR-04, GR-06). 300초 초과 8구간의 「구간·이유 한 줄」이 대부분 비어 있다(GR-05).
- 정리 후보 1은 사용자 플레이 창의 Editor 기동 기록 사고(두 번째 발생)다. 근거 폴더 스크립트 복사를 없애는 저장소 helper와 node 시험이 가장 높은 층이라는 리드 판단에 동의한다. 다만 자기 시간 상한은 보조가 아니라 필수 요건으로 둬야 한다.
- 정리 후보 2는 계약 허용 실행 목록 밖 명령의 반복이다. 세 goal에서 10건(작업자·검증자 7세션, 리드 2 goal, 빈 `node -e` 7회 이상)이고, Gardener 목록 밖 읽기는 이번이 두 번째다. 리드마다 따로 짠 정산 스크립트의 금지 명령 집계를 정본 helper로 옮기는 안이다.
- 리드 직접 쓰기 전 맥락 메모 누락은 기존 행 `lead-direct-write-context-memo`로 연결한다(후보 아님). 반복 규칙 요건이 이미 찼고 Rules 몫이다. 이번 종료의 두 번째 관측(플레이 창 복원 쓰기)이 goal·BACKLOG에 빠져 있어 GR-06으로 돌려보낸다.

## goal 1 — 기본 공격 키 왼쪽 Ctrl(결정 65 A, PR282)

### 점검 범주별 결과

| 범주 | 발생 수 | 원천(파일:행) | 처분 상태 |
|---|---|---|---|
| 독립 결함 | 제품 결함 0. 보고 결함 1(V1). 이 goal 귀속이 아닌 기존 실패 1((d) `MenuTransitionAndOutsideTown_NoPurchases`) | `verify-01/verdict.md:3`·`:17`, `attack-key-goal.md:220`(V1), `attack-key-goal.md:191-194`·`:247`((d)) | 이미 처분됨. V1은 메인 `msg_898736ad5eb6` A 기록만이다. (d)는 메인 `msg_812478372cdf` 기록만이고 원인 분류는 Content다 |
| CI 실패 | 0 | `kill-reward…/lead/closeout-01/pr282-checks-final-01.json:1`(4개 모두 SUCCESS, completedAt 2026-10-30T08:59:26Z~09:24:51Z), `lead/pr-body-01.md`에 CI 실패 기록 없음 | 해당 없음 |
| 새 경고 억제·설정 완화 | 0 | `git diff --name-only -G'<억제 낱말>' d4ee301c dd9e755b` 결과는 `goal.md` 하나(산문)다. 설정 파일(`*.asmdef`·`*.csproj`·`.editorconfig`·`.github`·`ProjectSettings`·`*.json`·`*.yml`) 변경 0. `implement-01/report.md:89`는 바뀐 파일의 `warning CS` 0줄을 적었다 | 해당 없음. Unity 로그 원문은 이 점검의 입력 밖이라 보지 않았다(미검토) |
| 임시 우회 | 3 | `attack-key-goal.md:187`(PR280 head 위에서 시험 작성, 리드 선택), `:189-190`(계약이 비우게 한 캡처 변수를 3회째 실행에서 작성자 폴더로 지정, 리드 결정), `:13`(앞 goal `Library` 재사용, 메인 처분) | 이미 처분됨. 셋 다 기록과 이유가 있다 |
| 드리프트·규칙 검사 | 0 | 후보 도착 검사 exit 0(「실행 기록」), BACKLOG 복사본 `:459` `touch-tap-attack-ui-block-test` | 해당 없음 |

### 발생 수와 원천

- 과정 이탈(범주 밖, 반복 빈도에 씀): 선행 작성자의 인라인 node 3회(`attack-key-goal.md:198`, `lead/settle-pretest-01.json:13-31`), 구현자의 목록 밖 git 읽기(`:211`), 검증자의 `git branch --show-current` 1회(`:223`). 셋 다 기록 후 진행으로 처분됐다.
- 300초 초과 4구간: 작성자 473·312초, 구현자 305초, 검증자 305초(`:197`·`:211`·`:222`·`:245`). 이유는 적혀 있지 않다(GR-05).
- 앞 goal에서 넘어온 기록: PR280 실사의 맥락 메모 부재(`:58-65`). 프로젝트로는 `lead-direct-write-context-memo`의 세 번째다(메인 `msg_68ef27e7bc61` 3 A, 원문은 `liveness-input-01.json:5384`).

### 종료 기록 대조

| 문장(복사본 행) | 원시 | 결과 |
|---|---|---|
| PR282 병합 2026-10-31T07:55:44Z, head `70ab4f19f4ca…6ec2b`, 병합 commit `dd9e755b78ba…9cd`(`:11`·`:232`) | `pr-merged-01.json:1`(number 282), `git log -1 dd9e755b`(부모 `d4ee301c`·`70ab4f19`) | 일치 |
| base가 main으로 바뀌고 head 그대로(`:232`) | `…precondition…/lead/pr-state-16.json:1`(282 baseRefName main, headRefOid `70ab4f19`) | 일치 |
| 메인이 사용자 승인 줄을 받아 병합(`msg_20097f42c3e3` 1, `:232`) | `liveness-input-01.json:18212` 1항 「PR282 → dd9e755b…」 | 일치 |
| 72 B 원문·16:5x KST(`:37`), 적용 3문장(`:38`) | `liveness-input-01.json:18212`(사용자 원문, 3항) | 일치 |
| 「별도 시연 목록이 없다」(`msg_ea34b7ba5963` 4, `:39`) | `liveness-input-01.json:18227` 4항 | 일치 |
| 79 C 「시연은 영상으로 보여줄 예정임」(`msg_3412074211cc`, `:40`) | `liveness-input-01.json:18272` | 일치 |
| 76 A·77 A 링크(`:42-44`) | 개선안 3 goal 복사본 `:28-32` | 일치(앵커 존재는 검사하지 않음) |
| 승인 `msg_6d9e5309e129`(`:9`) | `liveness-input-01.json:5841`(id 존재) | id 존재 확인, 본문 대조는 하지 않음 |
| Library 3,088,006,164 B(`:13`) | 같은 값이 `…knight-dash…/lead/preimport-01/after.json` 등 36파일에 있음(Grep) | 원시 있음 |
| 완료조건 6 「플레이에는 문제가 없음」·「나머지는 괜찮음」(`msg_a2b3903508fb` 1, `:242`) | `liveness-input-01.json:18287` | 일치 |
| 생존 신호 473·312·305·305초(`:245`) | `liveness-select-01.json`의 Dispatch 셋(`:8` 305, `:28` 473·312, `:60` 305), `liveness-output-01.json`(helper 원 출력) | 일치 |
| BACKLOG 세 번째·네 번째 관측 덧붙임(`:248`) | BACKLOG 복사본 `:31`, `closeout-context-01.md:6`(08:06:10Z) | 일치. 다만 이번 종료의 다른 관측 하나가 빠졌다(GR-06) |
| 다음 계획 후보 `touch-tap-attack-ui-block-test`(`:228`) | BACKLOG 복사본 `:459`, HEAD에는 없음(`git grep` exit 1) | 이번에 더한 행, 일치 |
| 진척 단계(`:17-23`) | 위 병합 기록 | 일치(「결과 기록과 종료」만 진행 중) |

## goal 2 — 처치 보상 떠오르는 글(개선안 3, PR283)

### 점검 범주별 결과

| 범주 | 발생 수 | 원천(파일:행) | 처분 상태 |
|---|---|---|---|
| 독립 결함 | 제품 결함 0. 기존 시험 (a) 1(구독자 집합 「정확히 셋」) | `kill-reward-goal.md:189`·`:214`·`:219`, `settle-verifier-01.json:6`(판정 첫 줄 「PASS. 제품 결함 V번호 없음」) | 이미 처분됨. 메인 `msg_b50d7db4c2ab` 조건 셋대로 검증자가 「정확히 넷」으로 고쳤고 65/1 → 66/0이다 |
| CI 실패 | 0 | `lead/ci-final-01.json:1`과 `lead/closeout-01/pr283-checks-final-01.json:1`(sha256 같음 `636ec28f…3839`, 4개 SUCCESS, main 통합 head 기준) | 해당 없음 |
| 새 경고 억제·설정 완화 | 0 | `git diff --name-only -G…` 결과는 새 파일 `KillRewardFloatTextCaptureTests.cs` 하나다. 그 `Assert.Ignore` 2곳(`f5adcf31:…/KillRewardFloatTextCaptureTests.cs:118`·`:167`)은 새 opt-in 캡처 시험의 lane 문이고 기존 검사 완화가 아니다(`:199`·`:218`). 설정 파일 변경 0 | 해당 없음. 문서 연결은 설계 관찰 O1 |
| 임시 우회 | 4 | (1) main 통합 뒤 Unity 미재실행·판정 유지(`:256`, 병합 관문 정본 적용). (2) 플레이 창 Editor 스크립트를 앞 goal에서 복사해 메모리 사고, 사용자가 `taskkill`(`:268-273`). (3) 결정 81 A의 서버 교체(`:274`). (4) 전 상태 스크립트의 `trim` 결함으로 거짓 false가 난 검사를 수기로 판단(`:265`, `play-01-preserve.json:47`·`:55`) | (1)·(3) 이미 처분됨. (2)는 정리 후보 1. (4)는 같은 부류(PowerShell·근거 폴더 스크립트)로 정리 후보 1에 묶는다 |
| 드리프트·규칙 검사 | 실제 위반 0. 검사 도구 결함 1((4)) | `play-01-preserve.json:45-55`(`onlyDocsUncommitted` false), `play-01-restore.json:82-91`(전부 true), 후보 도착 검사 exit 0, BACKLOG 복사본 `:461`, 교정 제안 `correction-proposal-editor-record-01.md` | (4)는 위 행. 교정 제안 판단은 정리 후보 1 |

### 발생 수와 원천

- 과정 이탈: 리드 계약 누락 첫 발생(`:192`), 리드 R-3 이탈 첫 발생(`:193`), 구현자 자기 신고 10건(`:194`, `git branch --show-current`·빈 `node -e ""` 1회·stderr 버린 읽기 포함), 캡처 작업자의 계약 밖 `Stop-Process`와 빈 `node -e ""` 1회(`:209-210`), 검증자의 메모 전 읽기(`:223`), 리드의 빈 `node -e ""` 1회(메인 금지 뒤, `:285`·`closeout-context-01-addendum-01.md:31`), 리드의 충돌 해소 선행 쓰기(`:257`).
- 300초 초과 3구간: 구현자 302초, 캡처 307초, 검증자 333초(`:194`·`:210`·`:224`·`:246`). 이유는 캡처 1구간(「창 배정 대기 중」)에만 있다(GR-05).

### 종료 기록 대조

| 문장(복사본 행) | 원시 | 결과 |
|---|---|---|
| PR283 병합 08:34:08Z, head `2fd74253d894…0945`, 병합 commit `f5adcf31e605…144fc`(`:5`·`:233`) | `pr-merged-01.json:1`(number 283), `git log -1 f5adcf31`(부모 `dd9e755b`·`2fd74253`) | 일치 |
| 범위 초안 sha256 `e8a69526…`(`:9`) | `sha256sum lead/scope-draft-v1.md` = `e8a69526cdf8…0712` | 일치 |
| 메인 R-2 대조 뒤 병합(`msg_ea34b7ba5963`·`msg_84d148dc5236` 1, `:233`) | `liveness-input-01.json:18227` 1항, `:18242` 1항 | 일치 |
| 76 A·77 A 원문·17:3x KST(`:30`), 76 A 적용(`:31`), 77 A 배정(`:32`·`:261`) | `liveness-input-01.json:18242` 사용자 원문·2항·3항 | 일치 |
| DIRTY·CONFLICTING, head `fef2f850`(`:250`) | `liveness-input-01.json:18212` 2항, `pr-state-16.json:1`(283 OPEN·DIRTY·`fef2f850…`) | 일치 |
| 통합 commit 부모 `fef2f850`·`dd9e755b`(`:251`) | `git log -1 2fd74253` | 일치 |
| 122경로·20·100·양쪽 2(`:253`) | `check-merge-blobs-01.json:2-20`(total 122, prSideCount 22, mainSideCount 102, bothSides 2) | 일치(22−2=20, 102−2=100) |
| 22파일 +2132/−2(`:254`) | `git diff --stat dd9e755b f5adcf31`(22 files, 2132 insertions, 2 deletions) | 일치 |
| 21파일 patch-id 같음, client.md 더한 줄 sha256 같음(`:254`) | `patch-id-per-file-01.txt:1-23`(same 21, client.md own=new `5598818ab0359db9`) | 일치 |
| CI 4개 SUCCESS(`:255`) | `ci-final-01.json:1` | 일치 |
| 「길드 창 배치와 밝은 스킨」 절 뒤 「처치 보상 글」 절(`:251`) | `git grep -n '^#+ ' f5adcf31 -- client.md`(58행·66행) | 일치 |
| 절차 이탈 자기 신고(`msg_ea34b7ba5963` 5, `:257`) | `liveness-input-01.json:18227` 5항, `closeout-context-01.md:6` | 일치 |
| 플레이 전 상태 08:38:26Z, HEAD `f5adcf31`, 미커밋 문서 4, drift 2 HEAD와 같음(`:262-263`) | `play-01-preserve.json:2-33` | 일치 |
| 「사용자 설정 두 곳의 sha256을 남겼다」(`:263`) | `play-01-preserve.json:34-44`(GameViewSizes `exists:false`, Shortcuts `files:[]`) | **불일치, GR-02** |
| 관문 Unity 0·dotnet 0·7777 없음·물리 17.12 GiB·commit 26.56 GiB(`:264`) | `play-01-preserve.json:10-17`(18378346496 B·28514369536 B) | 일치(수기 환산 17.116·26.556) |
| 서버 handle·46.2초·PID 1156(`:266`) | `play-01-server.json:23`·`:27`·`:30` | 일치 |
| Editor PID 7404, 6000.6.4f1(12bfff696524), 인자 `-projectPath`만(`:267`) | `play-01-launch-state-01.json:13-18`·`:83` | 일치 |
| 08:48:40Z 8.71 GiB·물리 1.92 GiB(`:270`) | `play-01-launch-state-01.json:2`·`:9`·`:75`(9352732672 B, 2056957952 B) | 일치(수기 환산 8.710·1.916) |
| 메인 측정 10.03 GiB·0.78 GiB(`msg_476db413d3d1` 1, `:270`) | `liveness-input-01.json:18257`(두 값 있음) | 일치 |
| 「08:52:43Z 전에 사라졌고」·물리 12.70 GiB(`:271`) | 인용한 `play-01-launch-state-02.json:2`의 utc는 08:52:52.811Z, `:65` `editorScriptPowerShell` 빈 배열, `:66` 13635952640 B | 12.70 GiB 일치. **시각 08:52:43Z의 저장 원시 없음, GR-03** |
| 두 스크립트 차이는 머리 주석과 출력 이름뿐(`:272`, 교정 제안 12행) | `git diff --no-index -U0 <앞 goal 2판> <이 goal 1판>`(1행 주석·출력 경로만) | 일치 |
| 81 A·80 A(`msg_3412074211cc`, `:274`) | `liveness-input-01.json:18272` | 일치 |
| 「화면의 시험 캐릭터는 「시험새벽1029」다」(`:274`) | `liveness-input-01.json:18287`(`msg_a2b3903508fb`, 「10-29 시험 캐릭터」). `msg_3412074211cc`에는 없음 | **원천 어긋남, GR-04** |
| 78 C 원문(`:275`), 범위 밖 의견(`:277`) | `liveness-input-01.json:18287` | 일치 |
| 84 A 원문, PID 7404 없음·Unity 0(`:278`) | `liveness-input-01.json:18302` | 일치 |
| 복원 10:12:04Z, MinimapRT 하나, 알 수 없는 변경 0, 설정 두 곳 같음(`:279-281`) | `play-01-restore.json:4`·`:45-52`·`:60-91` | 일치 |
| 복원 뒤 status 문서 5, 물리 17.38 GiB(`:282`) | `git-status-after-restore-01.txt:1-7`, `play-01-restore.json:7`(18657300480 B) | 일치(수기 환산 17.376) |
| Legacy 탭 닫음(`:283`) | `server-terminal-close-01.json:5-8`(ptyKilled true) | 일치 |
| 메인 알림 `msg_c3a81be9ec02`(`:284`·`:285`) | `play-01/send-restore-done-main-01.json`(id 있음, Grep) | 일치 |
| 생존 신호 293·302·307·333초(`:246`) | `liveness-select-01.json`의 Dispatch 넷(`:91` 307, `:117` 302, `:148` 293, `:161` 333) | 일치 |
| 다음 계획 후보 `dungeon-clear-test-helpers-shared`(`:229`) | BACKLOG 복사본 `:461`, HEAD에 없음 | 이번에 더한 행, 일치 |

## goal 3 — PlayMode 전제 건너뜀(실패 23건 정리, PR286)

### 점검 범주별 결과

| 범주 | 발생 수 | 원천(파일:행) | 처분 상태 |
|---|---|---|---|
| 독립 결함 | 차단 결함 0. 원인 미확정 기존 실패 13행(L1 잉크 11, L2 2) | `precondition-goal.md:211`·`:224-225`·`:237-238` | 이미 처분됨(결정 73 A, 실패로 두고 후속 후보). 후속 후보 기록은 GR-01 |
| CI 실패 | 0 | `kill-reward…/lead/closeout-01/pr286-checks-final-01.json:1`(4개 SUCCESS) | 해당 없음. Unity PlayMode는 CI 대상이 아니다(네 workflow 이름) |
| 새 경고 억제·설정 완화 | 검사 완화 7곳(`Assert.Ignore` 5, `Explicit` 2) | `git diff 3ac8d148 9cc80a9d`: `GuildGameplayWindowTestTools.cs`(hunk @111), `GuildWindowIndependentSceneTests.cs`(hunk @651·@710·@760·@2670, 판정 helper @1355), `InventoryServerIntegrationTests.cs:43`, `MapEntryServerIntegrationTests.cs`(hunk @18). goal `:100-105`·`:116-121`·`:141` | 이미 처분됨. 사용자 지시(「실패 23건도 이참에 병렬로 좀 정리좀 해줘」, 메인 초안 `msg_71f0ed6d4994`가 인용, `liveness-input-01.json:16832`)의 목표 자체이고, 메인 처분 `msg_e302e19ae9fa`·Content 조건 `msg_196d2c0ec1d4`(빈 값만, 값이 틀리면 실패)를 따른다. 남는 구멍(runner가 변수를 빠뜨림)은 근거 폴더 harness에만 막혀 있다 → 기존 행 `playmode-lane-runner-in-repo` |
| 임시 우회 | 2 | runner·판정기가 Git 밖(`:104`·`:107`·`:235`), 메인 초안과의 차이 9곳(`:244-254`) | 이미 처분됨(메인 `msg_e302e19ae9fa`). runner는 기존 후보 |
| 드리프트·규칙 검사 | 실제 위반 1 | 후보 도착 검사 exit 1, 진단 `missing-reference` 259행(「실행 기록」) | 리드 반환 GR-01 |

### 발생 수와 원천

- 과정 이탈: 선행 작성자의 빈 `node -e "" 2>/dev/null;` 1회, 리드의 빈 `node -e` 2회(`:66`·`:227`), 검증자의 기록 wrapper 밖 읽기 1회(`:68`). 메인 처분 `msg_202151c481c8`·`msg_b0795f230724`(원문 id는 `liveness-input-01.json`에 있음).
- 리드 보고 오기 1: 「35건 모두 같은 lane」(`msg_a62bd59fda05`)을 검증자가 바로잡았다(`:224`).
- 생존 신호 정산을 단계마다 정본 helper로 하지 않았다(`:277`). 종료 때 한 번에 쟀다.
- 300초 초과 1구간: 검증자 437초(17:06:43Z→17:14:00Z, `:279`). 구간은 있고 이유는 「스스로 신고한 공백과 같은 구간」뿐이다(GR-05).

### 종료 기록 대조

| 문장(복사본 행) | 원시 | 결과 |
|---|---|---|
| PR #286 병합 07:54:37Z, head `bf044c7235d0…3980`, 병합 commit `9cc80a9d2b8c…7d33`(`:12`·`:263`) | `pr-merged-01.json:1`(number 286), `pr-state-16.json:1`, `git log -1 9cc80a9d`(부모 `3ac8d148`·`bf044c72`) | 일치 |
| PR281·PR285와 30초 안에 차례로 병합(07:54:07Z~07:54:37Z, `:263`) | `pr-merged-01.json:1`(281 07:54:07Z, 285 07:54:24Z, 286 07:54:37Z) | 일치 |
| `dd9e755b`에 `Explicit` 43행과 왼쪽 Ctrl 109~116행이 함께 있음(`:264`) | `git grep -n … dd9e755b -- InventoryServerIntegrationTests.cs`(43행 `Explicit`, 112행 `Key.LeftCtrl`) | 일치 |
| 73 A 원문·16:5x KST(`:35`), 적용(`:36`) | `liveness-input-01.json:18212` 사용자 원문·4항 | 일치 |
| 13행 = L1 잉크 11 + L2 2(`:37`) | 같은 goal `:224-225`(검증 판정 요약) | 일치(goal 안 대조) |
| rebase 13:44:28Z·15:22:13Z, `429481c2`·`c41952da`·`ae839e0c`(`:11`) | `lead/rebase-01.txt:1`·`:8`, `lead/rebase-02.txt:1-5` | 일치 |
| 승인 초안 `msg_71f0ed6d4994`, 대조 `msg_e302e19ae9fa`(`:9`) | `liveness-input-01.json:16827`·`:16832`(「실패 23건도 이참에」)·`:16857` | 일치 |
| 생존 신호 32회 240초, 21회 298초, 26회 437초(`:278-279`) | `liveness-select-01.json`의 Dispatch 셋(`:197` 298, `:210` 240, `:223` 437) | 일치 |
| 다음 계획 후보 둘(`:258-259`) | 첫째는 BACKLOG 복사본 `:460`(HEAD에 없음). 둘째는 인용 없음 | **둘째 후보 GR-01** |

## 공통 — CURRENT·BACKLOG 대조, 생존 신호

- CURRENT Effect 줄(복사본 `:17`): 세 goal 링크, 경로, branch `docs/effect-closeout-20261031`, `PR282(dd9e755b)·PR283(f5adcf31)·PR286(9cc80a9d) 병합`, 결정 78 C, 결정 76 A가 세 goal과 맞다. 줄은 리드 스크립트 `write-current-effect-01.cjs:6-19`가 `- Effect: `로 시작하는 줄 하나만 바꾼다. HEAD판과 복사본은 둘 다 21행이다. 다른 줄이 그대로인지는 줄 단위 비교 도구가 허용 목록에 없어 직접 대조하지 못했다(한계).
- BACKLOG: 복사본은 HEAD보다 10행 많다(461 대 451). 새 절 「공격 키·처치 보상 글·전제 건너뜀 종료에서 연결한 후보」(`:453-458`)와 세 행(`:459-461`)이다. 세 ID는 HEAD에 없다(`git grep` exit 1). 세 행의 이유·출처가 각 goal 「다음 계획 후보」와 맞다. `lead-direct-write-context-memo`(`:31`)의 세 번째·네 번째 문장도 HEAD에 없던 덧붙임이고 65 A goal `:248`과 맞다.
- 생존 신호: `node 99_Tools/Orca/check-liveness.mjs …/liveness-input-01.json --threshold-seconds 300`을 다시 돌린 stdout sha256이 `0605467224c5…08dc`(25299 B)로 리드 저장본 `liveness-output-01.json`과 같다. exit 1(over)도 같다. 원 출력의 10 Dispatch 값(signalCount·firstAt·lastAt·maxGapSeconds)은 리드 선별본 `liveness-select-01.json`과 모두 같다.

## 리드 반환

| 번호 | 대상(복사본 행) | 사실 | 원시 | 고치는 법 |
|---|---|---|---|---|
| GR-01 | `precondition-goal.md:259` 「다음 계획 후보」 둘째 | 후보에 BACKLOG ID도 기존 goal 링크도 없다. 정본 helper가 exit 1 `missing-reference`를 낸다 | 「실행 기록」 세 번째 실행 | Content가 쓸 BACKLOG ID가 아직 없으면, 기존 goal 링크 `[로그인 화면 재구성·길드 거점 창 goal](../2026-10-30-login-screen-guild-window/goal.md)`(HEAD에 있음)를 넣는다. 또는 Content 행이 생기면 그 ID를 넣는다. 이 문장을 후보 목록 밖 문단으로 옮겨도 검사는 통과하지만, 후보의 도착을 남기는 쪽이 낫다 |
| GR-02 | `kill-reward-goal.md:263` | 「사용자 설정 두 곳(GameViewSizes·Shortcuts)의 sha256을 남겼다」 | `play-01-preserve.json:34-44`: GameViewSizes는 `exists:false`, Shortcuts는 `files:[]`라 sha256이 없다 | 「GameViewSizes.asset 없음, Shortcuts 폴더 파일 0을 남겼다」로 고친다. 복원 줄(`:281`)과 같은 표현이다 |
| GR-03 | `kill-reward-goal.md:271` | 「08:52:43Z 전에 사라졌고」 | 같은 문장이 인용한 `play-01-launch-state-02.json:2`의 utc는 08:52:52.811Z다. 08:52:43Z는 리드 메시지 문안(`play-01/send-play-ready-main-01.cjs:21`, 「내 확인 루프 기록」)에만 있고 루프 원시는 저장돼 있지 않다 | 저장 원시 기준 「08:52:52Z 기록에서 없었다」로 고치거나, 08:52:43Z를 쓴다면 그 확인 루프 원시를 근거 폴더에서 찾아 경로를 단다 |
| GR-04 | `kill-reward-goal.md:274` | 「화면의 시험 캐릭터는 「시험새벽1029」다」가 결정 80 A 문장 바로 뒤, `msg_3412074211cc` 인용 문단 안에 있다 | 이 이름의 원천은 `msg_a2b3903508fb`(「10-29 시험 캐릭터 「시험새벽1029」」, `liveness-input-01.json:18287`)다. `msg_3412074211cc` 본문(`:18272`)에는 없다 | 「화면의 시험 캐릭터는 10-29에 만든 「시험새벽1029」다(메인 `msg_a2b3903508fb`)」처럼 원천과 날짜를 단다. 80 A로 새로 만든 행이라는 오해를 막는다 |
| GR-05 | `attack-key-goal.md:197`·`:211`·`:222`·`:245`, `kill-reward-goal.md:194`·`:224`·`:246`, `precondition-goal.md:279` | 300초 초과 8구간의 기록에 구간 시각은 PlayMode 전제 검증자 1구간에만, 이유는 처치 보상 캡처 1구간에만 있다. 나머지는 초 값과 근거 경로뿐이다 | 규칙 원문 「생존 신호와 초과 정산」 「300초 초과는 **구간·이유 한 줄**로 기록한다」, 구간 원시는 `liveness-select-01.json` | 각 goal 「결과」의 생존 신호 줄에 구간마다 `from→to`와 이유 한 줄을 더한다. 이유를 모르면 「이유 미확인」으로 적는다. 추정 대기시간을 빼거나 면제하지 않는다(같은 절) |
| GR-06 | `kill-reward-goal.md:259-285`, BACKLOG 복사본 `:31` | 플레이 창(결정 77 A)의 전 상태 저장·서버·Editor·복원을 맥락 메모 없이 했고, 그중 추적 파일 쓰기(`MinimapRT.renderTexture` 복원)가 있었다는 사실이 리드 보충 메모에만 있다. goal 「사용자 직접 플레이 창」의 절차 이탈 줄(`:285`)과 BACKLOG 행에는 없다 | `closeout-context-01-addendum-01.md:7-9`(10:14:32Z), `play-01-restore.json:45-52` | goal 플레이 창 절의 절차 이탈에 한 줄로 더하고, 메인이 같은 부류로 보면 BACKLOG `lead-direct-write-context-memo`에 관측으로 덧붙인다. 같은 부류로 셀지는 메인 판단이다 |

## 정리 후보 1 — 사용자 플레이 창 Editor 기동 기록 사고(두 번째 발생)

- 반복 빈도: 두 번째 발생이다. 첫 발생은 기사 Dash goal(`01_Phases/goals/2026-10-29-knight-dash-effect-sheet/goal.md:358-360`, HEAD)이고 BACKLOG `verification-harness-powershell-helper` 덧붙임(복사본 `:30`)과 앞 Gardener 후보 2(`…/2026-10-29-knight-dash-effect-sheet/gardener.md:264`, HEAD)로 이미 올라가 있다. 두 번째는 이 goal(`kill-reward-goal.md:268-273`)이다. 같은 goal 안에서 같은 부류(근거 폴더 PowerShell 기록 스크립트의 결함)가 하나 더 있다. 전 상태 스크립트의 `trim` 결함(`:265`, `play-01-preserve.json:47`)이다.
- 원시 근거: 앞 goal 2판과 이 goal 1판의 차이는 머리 주석과 출력 이름뿐이다(`git diff --no-index`, 「실행 기록」). 1판 `play-01-editor.ps1:80`이 `Get-Content` 줄을 `$versionLine`에 넣고 `:105`가 `ConvertTo-Json -Depth 8`로 쓴다. 측정은 `play-01-launch-state-01.json:9`(9352732672 B)와 메인 `msg_476db413d3d1` 1(10.03 GiB)이다. 리드의 `taskkill`은 자동 모드 분류기가 거부해 사용자가 직접 껐다(`:271`). 원인(PSObject 속성 펼침)은 두 번 모두 재현하지 않은 리드 추론이다.
- 리드 교정 제안의 층 판단 검토(`correction-proposal-editor-record-01.md:17-27`): 「문서·계약 층은 이미 지났고, 복사할 스크립트를 없애는 저장소 helper가 가장 높은 층」이라는 판단에 동의한다. 근거는 하나 더 있다. 근거 폴더 스크립트는 Git 밖이라 lint·CI·시험 어느 층도 닿지 않는다. 저장소로 옮겨야 비로소 시험 층을 쓸 수 있다. 그래서 구조(helper)와 시험은 따로가 아니라 한 묶음이다.
- 더 높은 층이 있는 곳: 제안 4 「자기 실행 시간 상한」은 보조가 아니라 필수 요건으로 올려야 한다. 이번 사고에서 실제 피해를 키운 것은 원인 결함보다 「리드가 스스로 끌 수 없음」(분류기 거부)이었다. JSON 생성을 node로 옮겨도 다른 결함으로 멈추는 경우가 남는다. 시간 상한은 원인과 무관하게 그 피해를 막는다. 리드 제안의 나머지(기록 JSON은 node, PowerShell은 값만)는 같은 고비에 사고가 없던 `play-01-launch-state.cjs` 방식이라 근거가 있다(제안 15행).
- 검사화 방법: 정본 helper + node 시험이다. 위치는 `99_Tools/`의 기존 관례(`Backlog`·`Orca`·`SessionGuard`와 각 `.Tests`)를 따라 책임 이름의 새 폴더 하나와 그 `.Tests`를 둔다. 이름은 Rules·주인 파트가 정한다. 시험은 가짜 Editor 로그와 가짜 프로세스 목록으로 기록 단계를 돌리고 (1) 정해진 시간 안에 끝나는지, (2) 출력 JSON 값이 문자열·수·불리언뿐인지, (3) 시간 상한을 넘으면 스스로 끝나고 상태를 남기는지 본다. code-rules workflow가 이미 `99_Tools/Orca.Tests`를 돌리므로 같은 단계에 더할 수 있다. 하네스 원칙 4대로 새 검사는 warning 파일럿부터다.
- 기존 BACKLOG 행: `verification-harness-powershell-helper`(복사본 `:30`, 첫 발생 기록)와 `unity-baseline-harness-reuse`(`:229`, 실행 관문 중복). 새 행으로 나눌지는 메인 판단이다. 내 의견은 덧붙임이다. 다만 범위가 「PowerShell helper 정본」보다 좁고 성격이 다르다(PowerShell로 JSON을 만들지 않게 하는 쪽). 그래서 덧붙임 문장에 「플레이 창 helper는 PowerShell helper 정본과 별도로 먼저 할 수 있다」를 남기기를 권한다.

## 정리 후보 2 — 계약 허용 실행 목록 밖 명령의 반복

- 반복 빈도(세 goal, 모두 「기록만·비차단」 처분): 작업자·검증자 7세션과 리드 2 goal, 모두 10건이다.
  - 공격 키 goal: 선행 작성자 인라인 node 3회(`attack-key-goal.md:198`), 구현자 목록 밖 git 읽기(`:211`), 검증자 `git branch --show-current`(`:223`).
  - 처치 보상 goal: 구현자 `git branch --show-current`·빈 `node -e ""`·stderr 버린 읽기(`kill-reward-goal.md:194`), 캡처 작업자 빈 `node -e ""`·계약 밖 `Stop-Process`(`:209-210`), 검증자 메모 전 저장하지 않은 읽기(`:223`), 리드 빈 `node -e ""`(메인 금지 뒤, `:285`).
  - PlayMode 전제 goal: 선행 작성자 빈 `node -e ""`, 리드 빈 `node -e` 2회(`precondition-goal.md:66`·`:227`), 검증자 기록 wrapper 밖 읽기(`:68`).
  - 그중 빈 `node -e`(또는 `node -p "1"`)는 5세션 7회 이상이다. 메인이 리드에게 「붙이지 마라」고 한 뒤에도 같은 리드가 한 번 더 했다(`closeout-context-01-addendum-01.md:31`). 규칙 문장만으로 막지 못했다는 관측이다.
  - 앞 goal: Gardener 목록 밖 읽기 셸 명령(`…/2026-10-29-knight-dash-effect-sheet/goal.md:401`, 메인 `msg_68ef27e7bc61` 1 A), 구현자 둘의 git 목록 밖 읽기(같은 goal `:85`·`:87`, 앞 Gardener는 「BACKLOG 행 없음」으로 적음, `gardener.md:287`). 이번 Gardener도 같은 목록의 명령을 썼다(이 보고서 「실행 기록」). Gardener 계약의 목록 밖 읽기는 이번이 두 번째다.
- 원시 근거: 리드가 단계 정산마다 따로 짠 스크립트가 대화 기록·rollout에서 `inlineNode`·`gitWriteLike`·`orcaListingLike`를 센다(`2026-10-30-attack-key-left-ctrl/lead/settle-pretest-01.json:10-31`, `2026-10-30-kill-reward-float-text/lead/settle-verifier-01.json:20-22`). 같은 책임의 임시 helper가 단계·goal마다 복제되고 있다. 규칙 원문 하네스 원칙 2 「같은 책임의 임시 helper를 계속 복제하지 않는다」에 걸린다.
- 왜 정리 후보인가: 하나하나는 읽기 전용이라 피해가 없다. 그러나 건마다 처분 메시지가 든다. 이번 세 goal에서는 메인 `msg_812478372cdf`·`msg_2ad9c188b1d8`·`msg_202151c481c8`·`msg_b0795f230724`와 리드 `msg_b9a065338255`가 있다(`attack-key-goal.md:196`·`:222`·`:223`, `kill-reward-goal.md:222`, `precondition-goal.md:66`·`:68`). 진짜 범위 밖 쓰기가 이 잡음 속에 묻힐 위험도 커진다.
- 검사화 방법: 정본 helper + fixture 시험이다. 입력은 계약의 허용 실행 목록(기계 판독 표 하나)과 세션 대화 기록 jsonl 또는 Codex rollout이다. 출력은 목록 밖 명령, 쓰기형 git, `ORCA_*` 전체 조회, 인라인 node를 원문 줄과 시각으로 낸다. 위치는 `99_Tools/Orca/`(생존 신호 helper 옆) 또는 `99_Tools/SessionGuard/`다. 시험은 `99_Tools/Orca.Tests/`처럼 저장 fixture로 한다. 그다음 층은 `SessionGuard`(Claude Code hook)의 warning이다. 세션 시작 때 받은 허용 목록 밖 Bash 호출에 경고만 낸다. 하네스 원칙 4대로 warning 파일럿 → 같은 조건 실측 → 사용자 판단 순서다. 계약 쪽 보조는 「시작 확인 읽기(git `rev-parse`·`branch --show-current`·`status`, Claude의 Read·Glob·Grep)」를 위임 계약 양식의 기본 허용에 넣는 것이다. 앞 Gardener도 같은 마찰의 앞선 흔적으로 `2026-10-04-persistence-integration/goal.md:919`를 들었다(`…/2026-10-29-knight-dash-effect-sheet/gardener.md:287`, 그 연결은 앞 Gardener의 추론).
- 기존 BACKLOG 행: `saved-analysis-run-rule`(복사본 `:71`, 저장하지 않은 인라인 분석), `worker-harness-only-execution`(`:297`, harness 밖 부작용 명령). 둘 다 이번 세 goal의 관측을 아직 담지 않았다. 빈 `node -e`는 값을 내지 않아 `saved-analysis-run-rule`의 「분석」과 정확히 같은 부류는 아니다. 새 행으로 둘지 덧붙임으로 둘지는 메인 판단이다.

## 첫 발생·기존 후보 연결(후보 아님)

| 관측 | 횟수와 원천 | 연결 | 이유 |
|---|---|---|---|
| 리드 직접 쓰기 전 맥락 메모 누락 | 프로젝트 5번째 관측. BACKLOG 복사본 `:31`에 넷(10-29 둘, 10-30 PR280, 10-31 `client.md` 충돌 해소). 이번 종료의 두 번째 관측(플레이 창 복원 쓰기)은 `closeout-context-01-addendum-01.md:7-9`에만 있다 | 기존 `lead-direct-write-context-memo`(`:31`), 관련 `lead-memo-audit-field-check`(`:326`), `single-output-role-memo-first`(`:305`) | 반복 규칙 요건이 이미 찼고 정본 반영은 Rules·사용자 몫이다. 층 판단: 지금 행의 선행 조건은 문서 층(문구와 위치)뿐이다. `SessionGuard` 규칙 3은 세션의 첫 쓰기만 막고, 이번 넷째·다섯째는 세션 중간 쓰기와 node 스크립트 쓰기라 hook이 보지 못한다(`:297`가 같은 한계를 적음). 그래서 다음 층은 검사다. 리드 branch의 commit 시각(`git log`)과 리드 메모 갱신 절의 `date -u` 시각을 대조하는 helper를 `lead-memo-audit-field-check`와 한 helper로 두기를 권한다. 넷째(통합 commit 08:01:01Z, 메모 08:06:10Z)는 이 방식으로 잡힌다. 다섯째(commit 없는 복원)는 못 잡는다. 기록 누락은 GR-06 |
| 단계 정산 때 생존 신호 helper 미실행 | PlayMode 전제 goal `precondition-goal.md:277`. 앞 goal Gardener 후보 1이 같은 내용이다(`…/2026-10-29-knight-dash-effect-sheet/gardener.md:247`) | 기존 `heartbeat-interval-review`(`:298`) | 두 번째 발생이고 기존 행이 있다. 이번에는 종료 때 helper로 모두 쟀고 값이 맞았다. 이 행에 관측을 덧붙이는 일은 리드·메인 판단이다 |
| 보고의 「미실행 포함 통과」 표현(V1) | 공격 키 goal `:220`, `verify-01/verdict.md:17`·`:74` | 기존 `verification-execution-claim-check`(`:299`) | 같은 부류(실행하지 않은 단언을 통과로 기록)의 기존 행이다. 판정서가 검사 전환을 「일부 가능」으로 적었다 |
| 리드 R-3 이탈(일반 회신에 `reply --id`) | 처치 보상 goal `:193`, 리드 첫 발생 | 기존 `orca-general-reply-tag-guard`(`:73`, 외관 goal Art 리드 10-29 두 번) | 프로젝트로는 세 번째 관측이다. goal은 「첫 발생이라 규칙을 새로 만들지 않는다」고 적었는데, 이미 기존 행이 있는 부류다. 행에 관측을 덧붙일지는 리드·메인 판단이다 |
| 리드가 메인에 옮긴 수치의 오기 | PlayMode 전제 goal `:224`(「35건 모두 같은 lane」, `msg_a62bd59fda05`) | 기존 `lead-measured-fact-recompute`(`:301`) | 같은 부류의 기존 행이다. 검증자가 바로잡았다 |
| 리드 계약 누락(새 구독자 수 단정 grep) | 처치 보상 goal `:192` | 없음 | 첫 발생. goal에 다음 계약 문장이 있다 |
| 캡처 작업자의 계약 밖 `Stop-Process`(자기 고아 `tail`·`grep`) | 처치 보상 goal `:209` | 없음(정리 후보 2와 같은 집계에 들어간다) | 첫 발생. goal에 다음 계약 허용 문장이 있다 |
| 빈 값 판단식 차이·실서버 lane `Recorder`·원인 미확정 13행 | PlayMode 전제 goal `:234`·`:239`·`:237` | Content가 쓸 후보(아직 행 없음) | 결정 73 A로 Content 몫이다. 도착 표시는 GR-01 |

## 설계 관찰과 대안(비차단)

- O1 `DEVELOPMENT.md` 「Unity PlayMode 실행 전제」 표(`9cc80a9d`, 116~119행)는 이번 23건만 담는다. 뒤에 병합된 `KillRewardFloatTextCaptureTests`의 `DAWNHOLDER_UI_CAPTURE_DIR`·Game 화면 전제(`f5adcf31:…:32`·`:118`·`:167`)와 원래 건너뛰던 19건은 없다. 표 머리가 범위를 말하지 않아 처음 읽는 사람은 전부로 읽을 수 있다. 대안은 표 위에 「이 표는 23건 정리 대상만 담는다」 한 문장을 두거나 캡처 시험 행을 더하는 것이다. 주인은 문서를 넣은 Effect와 시험 주인이다.
- O2 PR282는 07:55:44Z에 병합됐는데, 그 전에 PR284(07:54:57Z)·PR287(07:55:10Z)이 main에 들어갔다. PR282의 마지막 CI는 10-30의 자기 head 기준이다(`pr282-checks-final-01.json:1`). 합친 상태는 뒤의 PR283 통합 head `2fd74253`의 CI(`ci-final-01.json`)가 `dd9e755b`를 포함해 사후에 덮었다. PR286도 base가 바뀐 뒤 병합 전 확인을 못 했다고 goal이 적었다(`precondition-goal.md:263`). 연속 병합에서 「base 바뀐 뒤 확인」이 빠지는 구조다. 병합 관문은 메인 몫이라 관찰로만 둔다.
- O3 후보 도착 검사의 경로 해석: 세 goal의 후보 넷 중 goal 링크를 쓴 후보가 없다. 그래서 고정 복사본 위치 때문에 결과가 달라질 후보는 없다. GR-01을 goal 링크로 고치면 그때는 복사본 위치가 해석에 영향을 준다. 그 검사는 원본 위치에서 돌려야 한다.
- O4 공격 키 goal 「결과」(`:232`)가 PlayMode 전제 goal 근거 폴더의 `pr-state-16.json`을 근거로 든다. 세 goal이 근거를 공유한 결과이고 틀리지 않았다. 처음 읽는 사람의 탐색 부담은 늘어난다. 종료 근거를 `kill-reward…/lead/closeout-01/`에 모은 리드 결정(`closeout-context-01.md:24`)과도 어긋나므로, 다음 종료에서는 같은 폴더로 모으는 편이 낫다.
- O5 Gardener 계약의 허용 실행 목록에 `ls`·`cat`·`head`가 없고 같은 일을 하는 Read·Glob이 있다. 두 Gardener가 같은 습관으로 이탈했다. 계약 문장에 「파일 보기는 Read, 목록은 Glob으로」를 직접 적으면 문서 층에서도 줄일 수 있다. 정리 후보 2의 보조로 둔다.

## 실제 준수

| 규칙 원문 절(`gardener-rules-v2.md`) | 보고서에서 지킨 곳 |
|---|---|
| CODE_CONVENTION 「파일 위치와 이름」 | 새 파일은 계약이 정한 보고서 하나다(사전 맥락 「신규 파일 위치·이름」). 정리 후보의 helper 위치는 `99_Tools/`의 기존 `<책임>`·`<책임>.Tests` 관례를 예로 들었고 이름에 날짜·작업자를 넣지 않았다(정리 후보 1·2) |
| 「주석과 문서」 | 문서 관찰 O1(실행 전제 표의 범위 문장)과 GR-02~GR-04(원시와 다른 문장의 수리 안내) |
| 「역할별 적용」 | 판정을 바꾸지 않고 검토함·지적 없음·미검토를 나눴다(범주 표의 「미검토」, 「실행 기록과 미실행·한계」). 문서 점검이라 정적 대조만 했다 |
| 「변화 평가」 「교정 층과 반복 규칙」 | 첫 발생과 두 번째 이상을 나눴다(「첫 발생·기존 후보 연결」 표). 교정 제안의 층을 따로 판단했다(정리 후보 1). 문서 층에서 멈춘 리드 메모 건에 다음 층을 적었다 |
| 「하네스 원칙」 1·2·4 | 검사화 방법(정리 후보 1·2), 임시 helper 복제 지적(정리 후보 2), warning 파일럿 순서(두 후보) |
| 「사람의 탐색과 가독성」 | O1·O4 |
| 수치 계측과 판단 구분 | GiB 환산은 수기라고 표시했다(goal 2 대조 표). 108파일 대조가 눈 대조임을 「한계」에 적었다 |
| ORCA 「생존 신호와 초과 정산」 | 정본 helper 재실행과 선별본 대조(공통 절), 「구간·이유 한 줄」 누락(GR-05). 추정 대기시간을 빼거나 면제하지 않았다 |
| ORCA 「목표 종료 Gardener 4주 파일럿」 | 쓰기는 보고서 한 파일, 정리 후보 최대 둘과 검사화 방법, 검사 실행불가와 실제 위반 구분(범주 표), 직접 수정·채택 없음 |

## 추가로 읽은 것

고정 입력 목록 밖에서 읽은 파일이다. 근거 폴더 파일은 sha256을 적고, 추적 파일은 읽은 판을 적는다.

| 경로 | sha256 또는 판 | 읽은 방법·이유 |
|---|---|---|
| `…/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-launch-record-01.md` | `f95a870180c4fc36e30e5857488498d5633f255350245397363d1124d38f66f3` | 전체. 기동 명령·첫 화면 |
| `…/lead/closeout-01/gardener-contract-v1-pointer.md` | `d5469e3654e2e2214d2f29921486fa8defbf8ee753c14a37c5a73a704c818ea2` | 전체. 첫 쓰기 범위 |
| `…/lead/closeout-01/liveness-input-01.json` | `a94d6a4b44dfa4c9e1fba1e7e99924ce0071a59fa69928b801e7ccb179ad8936` | Grep(msg id·원문 낱말), Read `:5379-5390`·`:18207-18302`. 사용자 결정 원문·msg ID 대조 |
| `…/lead/closeout-01/inbox-run-01.json` | `105562d6cd99cff2e135f88482b04bbc508fefc31726164ba8b4aed8cf129cd1` | Grep(파일 이름만). 08:52:43Z 출처 찾기 |
| `…/lead/scope-draft-v1.md` | `e8a69526cdf81150b654d426cbaae80341d4bdf519497b433ff99b695a910712` | sha256만. goal `:9` 앞자리 대조 |
| `…/lead/play-01/send-play-ready-main-01.cjs` | `a2203d7841d9fe561240c9deea8657b9b26f5f2859b236abec119d14b71b9830` | Grep `:21`. GR-03 |
| `…/lead/play-01/send-restore-done-main-01.json` | `4b52a84a5a9e9e09aee30d05a5599cfa8bcbe385d04ec8b66f8c1dad84107cba` | Grep(파일 이름만). `msg_c3a81be9ec02` 존재 |
| `…/2026-10-30-playmode-precondition-ignores/lead/rebase-01.txt` | `1162584b25bd78004974f05e7bcf7cd7dd2f601a52494f17345589ca3288f158` | Grep `:1`·`:8` |
| `…/2026-10-30-playmode-precondition-ignores/lead/rebase-02.txt` | `0dfa926dd566c52a7e31b45a34e4066b3d9d26009a5d970acbad98cf8f9c5aed` | Grep `:1-5`·`:12`·`:15` |
| `…/2026-10-30-attack-key-left-ctrl/verify-01/defect-v1.md` | `176f8b065fbd0ec27bd500333bb2bd903678bc72c354478699619feef84c087d` | 존재와 sha256만(본문은 판정서 `:17`이 인용) |
| `…/2026-10-30-attack-key-left-ctrl/implement-01/report.md` 등 각 단계 report | 고정 입력 | Grep(경고 억제 낱말). 공격 키 `implement-01/report.md:89` 한 줄 |
| `…/lead/closeout-01/gardener-screen-02.json` | `9f04443cb448e2a0a2c9034ba751bc8be55e180426e1fedce401d7ac4b57aa9f` | sha256만. 읽지 않았다 |
| `.agents/skills/dawnholder-task-context/references/templates.md` | 작업 트리(추적, 미변경) | 「맥락 메모」 양식 1~44행 |
| `01_Phases/goals/2026-10-29-knight-dash-effect-sheet/goal.md`·`gardener.md` | HEAD `f5adcf31`(`git grep`)과 작업 트리(Grep, 미변경 파일) | 반복 빈도. 후보 제목·처분 행만 |
| `01_Phases/goals/*/goal.md` 전체 | 작업 트리 | Grep 개수만(`node -e`·목록 밖 읽기). 세 Effect goal은 리드 미커밋 판이 섞였고 판정 근거로 쓰지 않았다 |
| 제품 파일 | HEAD 계열 commit의 `git diff`·`git grep` | 범주 3·대조 표 |

## 실행 기록과 미실행·한계

- 착수: `date -u` → `Sat, Oct 31, 2026 10:20:45 AM`, `TZ=KST-9 date` → `Sat, Oct 31, 2026  7:20:45 PM`. `git rev-parse HEAD origin/main` → 둘 다 `f5adcf31e605af3b30581746c20a64642bb144fc`. `git branch --show-current` → `docs/effect-closeout-20261031`. `git status --short` → 리드 미커밋 5파일.
- 계약 대조: `sha256sum …/gardener-contract-v1.md` → `be0e5da37be0c9ee097264937401318b341a161c953d912a29c9a46ceec8ec1d`. `sha256sum gardener-inputs-v2.txt gardener-rules-v2.md` → `d0fd2cc0…115b`, `ed6afd5c…e325`. 108파일 `sha256sum`(세 번에 나눠 28·35·45파일) → 목록 8~115행과 모두 같다.
- 후보 도착 검사 세 번(저장소 루트, 출력 그대로):

```text
$ node 99_Tools/Backlog/check-candidates.mjs --backlog .backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-inputs-v2/BACKLOG.md --goal .backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-inputs-v2/attack-key-goal.md
{"status":"allowed","exitCode":0,"backlog":".backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-inputs-v2/BACKLOG.md","goal":".backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-inputs-v2/attack-key-goal.md","counts":{"backlogIds":205,"duplicateIds":0,"candidates":1,"candidatesWithoutReference":0,"unknownIds":0},"diagnostics":[]}
exit=0

$ node 99_Tools/Backlog/check-candidates.mjs --backlog .backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-inputs-v2/BACKLOG.md --goal .backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-inputs-v2/kill-reward-goal.md
{"status":"allowed","exitCode":0,"backlog":".backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-inputs-v2/BACKLOG.md","goal":".backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-inputs-v2/kill-reward-goal.md","counts":{"backlogIds":205,"duplicateIds":0,"candidates":1,"candidatesWithoutReference":0,"unknownIds":0},"diagnostics":[]}
exit=0

$ node 99_Tools/Backlog/check-candidates.mjs --backlog .backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-inputs-v2/BACKLOG.md --goal .backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-inputs-v2/precondition-goal.md
{"status":"policy-violation","exitCode":1,"backlog":".backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-inputs-v2/BACKLOG.md","goal":".backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-inputs-v2/precondition-goal.md","counts":{"backlogIds":205,"duplicateIds":0,"candidates":2,"candidatesWithoutReference":1,"unknownIds":0},"diagnostics":[{"code":"missing-reference","path":".backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener-inputs-v2/precondition-goal.md","line":259,"message":"The candidate has neither a BACKLOG ID citation nor an existing goal link.","repair":"Add BACKLOG `<id>` or a link to an existing other goal.md within this candidate."}]}
exit=1
```

- 생존 신호 helper: `node 99_Tools/Orca/check-liveness.mjs .backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/liveness-input-01.json --threshold-seconds 300 | sha256sum` → `0605467224c531a09da76d60b635f3da076c65b941bce4aa82761d9d910308dc *-`, `helper-exit=1`. 같은 명령 `| wc -c` → `25299`. `sha256sum`·`wc -c liveness-output-01.json` → 같은 값.
- git 읽기: `git log -1 --format='%H parents=%P committer=%cI' <dd9e755b|f5adcf31|9cc80a9d|2fd74253>`(셸 `for` 안), `git diff --stat d4ee301c dd9e755b`(15 files, +442/−32), `git diff --stat dd9e755b f5adcf31`(22 files, +2132/−2), `git diff --stat 3ac8d148 9cc80a9d`(6 files, +293/−4), `git diff -U1 3ac8d148 9cc80a9d -- 03_Client 00_Document/operations/DEVELOPMENT.md`, `git diff --name-only -G'Assert\.Ignore|Explicit|\[Ignore|#pragma|SuppressMessage|NoWarn|Inconclusive|Assume\.' <세 범위>`, 설정 경로 `git diff --name-only <세 범위> -- '*.asmdef' '*.csproj' '*.editorconfig' '.github' 'ProjectSettings' '*.json' '*.yml'`(출력 없음), `git grep -n -E 'Assert\.Ignore|Explicit\(|#pragma|SuppressMessage|NoWarn|Assert\.Inconclusive|Assume\.That' f5adcf31 -- <새 파일 8개>`, `git grep -n -E 'OutputVariable *=|…' f5adcf31 -- …CaptureTests.cs`, `git grep -n -E 'Explicit\(|leftCtrl|LeftCtrl|Key\.LeftCtrl' dd9e755b -- …InventoryServerIntegrationTests.cs`, `git grep -n -E '^#+ ' f5adcf31 -- 00_Document/domains/client.md`, `git grep -n -c -e <세 ID> HEAD -- BACKLOG.md`(exit 1), `git grep -n -e '세 번째는' -e '네 번째는' HEAD -- BACKLOG.md`(exit 1), `git show HEAD:…/BACKLOG.md | wc -l`(451), `git show HEAD:…/CURRENT.md | wc -l`(21), `git grep -n -E '^- Effect' HEAD -- CURRENT.md`, `git ls-tree HEAD …CURRENT.md`, `git ls-tree --name-only HEAD 01_Phases/goals/2026-10-30-login-screen-guild-window/ …knight-dash…/`, `git grep -n -E 'ConvertTo-Json|작업 집합|10,849,169,408' HEAD -- '01_Phases/goals/*/goal.md' '01_Phases/goals/*/gardener.md'`, `git ls-tree --name-only HEAD 99_Tools/`, `git diff --no-index --stat`·`-U0 <앞 goal play-01-editor-02.ps1> <이 goal play-01-editor.ps1>`, `git diff --no-index -U0 <이 goal 1판> <2판>`.
- Read·Grep·Glob: 위 대조 표의 각 원천 행 번호가 그 도구 호출의 결과다.
- 우편함: check 2회(count 0 두 번), heartbeat 착수 직후 1회와 그 뒤 약 5분 간격. 모두 파이프·리디렉션 없이 단독으로 불렀다.
- **허용 목록 밖 실행(자기 신고, 모두 읽기 전용)**:
  1. `ls -la`(closeout-01 폴더와 `gardener/`) 1회.
  2. `cat`(기동 기록·포인터) 1회.
  3. `echo`(이름을 지정한 `ORCA_TERMINAL_HANDLE`·`TEMP`·`TMP` 값, `exit=$?`·`PIPESTATUS` 표시) 여러 호출.
  4. `sed -n '1,29p' <BACKLOG 복사본> | cut -c1-400` 1회.
  5. `git grep … HEAD -- BACKLOG.md | cut -c1-120` 1회.
  6. `git diff -U0 … | git grep --no-index … -- - 2>/dev/null` 1회. stdin 파이프였고 stderr를 버렸다. 결과(exit 1)는 쓰지 않고 `-G` 명령으로 다시 쟀다.
  7. 셸 `for` 반복(`git log -1` 네 번) 1회.
  8. `git grep -v -F -x -f …` 시도 1회. `-x`가 없는 옵션이라 오류로 끝났고 결과는 쓰지 않았다(파이프의 `wc -l`·`git grep --no-index -c` 포함).
  9. `git ls-tree … 2>&1 | head -20` 1회.
  10. 대부분 호출의 `cd`(작업 경로 안).
- 미실행·한계:
  - 108파일 sha256 대조는 허용 목록에 비교 도구가 없어 출력과 목록을 눈으로 줄마다 대조했다. 사람 대조의 오류 가능성이 남는다.
  - CURRENT의 Effect 줄 밖 20줄이 HEAD와 같은지는 직접 대조하지 못했다(줄 수 21 대 21과 리드 스크립트 원문만 봤다).
  - Unity 로그 원문, PR 화면, CI 로그 본문은 보지 않았다. CI는 저장된 최종 check 원시만 봤다.
  - 사용자 결정 원문은 저장 우편함 원시와 대조했다. Orca 서버의 실제 메시지는 조회하지 않았다.
  - 승인 `msg_6d9e5309e129` 등 10-30 메시지는 id 존재만 확인했다.
  - Editor 메모리 사고의 원인(PSObject 속성 펼침)은 확인하지 않았다. 스크립트를 실행하지 않는 점검이다.
- 본문 작성 뒤 인용 재대조(10:39Z~10:49Z):
  - 보고서가 인용한 행 번호와 msg ID를 원천과 다시 맞췄다. 도구는 Read·Grep·Glob이고, Bash는 `sha256sum`(`ci-final-01.json`·`pr283-checks-final-01.json`), `git log -1 --format='%H %cI %aI' 2fd74253`(`2026-10-31T17:01:01+09:00`), `git grep`(캡처 변수·`DEVELOPMENT.md` 표·`code-rules.yml`의 node 시험·앞 Gardener의 `persistence-integration`)만 썼다.
  - 다시 읽은 원천: `play-01/` JSON 다섯(preserve·launch-state 둘·server·restore), `git-status-after-restore-01.txt`, `server-terminal-close-01.json`, `settle-verifier-01.json`, 공격 키 `lead/settle-pretest-01.json`, `check-merge-blobs-01.json`, `patch-id-per-file-01.txt`, `closeout-context-01.md`과 보충 메모, 교정 제안, `write-current-effect-01.cjs`, `verify-01/verdict.md`(Grep), 세 goal 복사본의 해당 절, BACKLOG 복사본 `:30`·`:31`·`:297`(Grep).
  - 고친 인용은 넷이다. 처분 메시지 목록을 실제 처분 메시지로 바꿨다. PlayMode 전제 goal의 「사용자 지시」에 원문과 msg 위치를 달았다. 생존 신호 선별본의 행 범위를 Dispatch 행으로 바꿨다. 앞 Gardener 인용에 원천 행을 달았다.
  - Glob 도구는 `gardener/*`처럼 폴더를 앞에 붙인 패턴에서 결과 0을 냈다. `path`를 그 폴더로 주고 `**/*`로 다시 보아 파일을 확인했다.

## 자기 쓰기 감사

- 이 세션이 쓴 경로: `.backups/verification/2026-10-30-kill-reward-float-text/lead/closeout-01/gardener/context-gardener-report.md` 하나다(Write 1회, Edit로 본문 추가).
- 허용 밖 쓰기: 0. 직접 만든 TEMP 파일은 없다. Claude Code가 TEMP(`.backups/tmp/effect-gardener-20261031`)에 남기는 파일은 도구 동작이다.
- 허용 밖 실행: 위 「허용 목록 밖 실행」 10항목. 모두 읽기 전용이고 저장소·근거 폴더·설정 상태를 바꾸지 않았다. 판정 원문에 사실로 남긴다.
- 확인 방법: 끝내기 직전 `git status --short`가 착수 때와 같은 리드 미커밋 5파일인지, `gardener/` 폴더에 이 보고서 하나만 있는지(Glob) 본다.
- 결과(`date -u` 출력 `Sat, Oct 31, 2026 10:49:54 AM`): `git status --short`는 ` M 00_Document/operations/BACKLOG.md`, ` M 00_Document/operations/CURRENT.md`, ` M 01_Phases/goals/2026-10-30-attack-key-left-ctrl/goal.md`, ` M 01_Phases/goals/2026-10-30-kill-reward-float-text/goal.md`, ` M 01_Phases/goals/2026-10-30-playmode-precondition-ignores/goal.md` 다섯 줄이다. 착수 때와 같다. Glob(`path` = `gardener/`, `**/*`)은 `context-gardener-report.md` 하나다. 허용 밖 쓰기 위반은 0이다.
- 이 보고서의 sha256과 bytes는 이 줄을 쓴 뒤 `sha256sum`·`wc -c`로 재서 `worker_done` 첫 줄에 적는다. 보고서 안에는 자기 해시를 적을 수 없다.
