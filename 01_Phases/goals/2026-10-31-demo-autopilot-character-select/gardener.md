# 종료 Gardener 점검 보고서

## 사전 맥락(최초 메모)

- 역할·태그: 신규 읽기 전용 목표 종료 점검자, `[Content Sol]`. Task `task_c8bf12a492b2`, Dispatch `ctx_e36caa1ebdbb`, 자기 handle `term_740f3807-b28b-48de-8d33-ae91abe167e4`, 리드 `term_3ca049fa-6b41-4f33-9caa-a1a22080940a`.
- 작성 시각: `2026-11-06T06:33:45.4334511Z` (`[DateTime]::UtcNow.ToString('o')` 실제 출력). 대상 파일 첫 쓰기이며 아래 결론은 아직 작성하지 않았다.
- cwd: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/content-active`. 관측 branch `docs/content-demo-autopilot-closeout-20261101`, HEAD `fff227fad36cb2704e756a10895a338cbd2363b5`. 착수 `git --no-optional-locks status --short` 출력 없음. 다른 작성자의 추적 파일 소유권을 침범하지 않는다.
- 계약: `../gardener-contract-v1.md`, SHA256 `3e36d39346f6df40e223f79f89b7118f8726a1dca11a5e5be3c70c78e01363b2` 일치. 이후 상대 경로 E는 `.backups/verification/2026-10-31-demo-autopilot-character-select/`, C는 `E/closeout-01/`, G는 `C/gardener-inputs/goal.md`, B는 같은 폴더 `BACKLOG.md`다. 줄 번호는 고정 입력 파일 기준이다.
- 지정 모델·명령: `codex --model gpt-6-astra -c model_reasoning_effort=xhigh --disable fast_mode`. 11-05 사용자 결정(G:167–173)과 계약의 배정을 따른다. 첫 화면: 이 세션의 허용 입력에 모델 표시 사본이 없어 미확인(원본 화면 JSON/터미널 조회 금지). 자기 인식: Codex/GPT-6 계열, 정확한 런타임 SKU·effort를 자기 설명으로 입증하지 않는다. backend 실제 모델: `unknown`.
- 고정 입력: `C/gardener-inputs-v1.txt` SHA256 `f27358916e9284cdc56eb3aeb4a4b393f7bcb4e78d09ad1f75fba92fbd8ac951`; 실제 재계산 `checked=224 mismatches=0`. 규칙 원문 `C/gardener-rules-v1.md` SHA256 `642af8b61252cba384aa92412e5b97c128f1324b10adc14af0c21206b17d2a23`도 목록 대조에 포함.
- 허용 쓰기: 이 보고서 한 파일. 부모 폴더 존재 확인. 도구 자동 TEMP는 `.backups/tmp/gardener-demo01/`만 허용. 수동 스크립트·중간 파일은 쓰지 않는다.
- 허용 실행: 잠금 없는 Git 읽기, 지정 파일 읽기·해시, 시계, 정본 후보 도착·생존 신호 helper의 읽기 전용 실행, live preamble의 heartbeat/ask/worker_done. 빌드·시험·게임·DB·웹·gh·Git 쓰기·추가 위임·다른 터미널 조회는 하지 않는다. 별도 실행 자원 관문은 해당 없음: 이 역할은 제품 실행을 하지 않는다.
- 문서 실사 등급. 제품 검증 통과/메인 처분을 재판정하지 않고 종료 기록과 근거의 대응을 확인한다. R-7 비해당(승인 시범 목표 아님). 목표 생성·범위 확대 없음.

### 읽기와 선택

| 읽은 파일·절 | 기준 | 적용 이유 |
|---|---|---|
| 제공된 AGENTS, HEAD CLAUDE의 결정 4A·Gardener 배정 | HEAD 위 값, 현재 사용자 제공 지침 | 권한·태그·독립 역할; 모델은 후행 11-05 결정 우선 |
| G 재개 지점·결정 제목·11-05 배정, 계약 전문 | 고정 224 입력 / 계약 hash | 종료점과 범위·입력·금지 원본 |
| task-context SKILL, templates 「맥락 메모」·「검증 판정」 | HEAD 및 입력 목록 hash | 사전 메모, 실제 준수, 수치와 실행불가 분리 |
| C/gardener-rules-v1.md 전문 | 위 hash | CODE_CONVENTION 파일 위치와 이름·주석과 문서·역할별 적용·변화 평가, ORCA 생존 신호·Gardener |
| goal-loop 「기준과 상태」, 99_Tools/README 「후보 도착 검사」「생존 신호 간격」 | HEAD/고정 입력 | 진척 표시와 정본 helper 계약 |
| FEATURE_MAP 「상태의 수명」, REPORTING 관련 절, DEVELOPMENT Node·실행 부작용 관련 검색 | HEAD | 보존 계약·원시 근거·읽기 전용 도구 확인 |

- 선택 원문은 계약이 고정한 규칙 파일 전체다. 제품의 상태·Unity·빌드·언어별 구현 규칙은 제품·시험·도구 변경이 없으므로 제외한다. 보고서 수치·설계 관찰·검사화 제안에는 변화 평가를 적용한다.
- 같은 책임 예시: `01_Phases/goals/2026-10-30-login-screen-guild-window/gardener.md`의 절 제목만 확인했다. 결론·후보는 기대답으로 읽거나 복사하지 않는다.
- 재사용: `99_Tools/Backlog/check-candidates.mjs`, `99_Tools/Orca/check-liveness.mjs`. 리드의 cjs를 실행하지 않는다. 새 helper·분석 파일은 만들지 않는다.
- 영향 파일은 이 보고서뿐. 제품 PR291·PR268, 녹화 판정 원문, goal·CURRENT·BACKLOG 및 시험·원시 입력을 보존한다.
- 위치·이름은 할당된 종료 근거 폴더의 context+gardener+report 계약을 그대로 따른다. 새 폴더 생성 없음. 보고서 첫 메모와 결과를 한 파일에 모아 쓰기 경계를 지킨다.
- 기본 예시 외 추가 읽기는 종료 기록 전수 대조와 반복 발생 탐색에 필요한 좁은 원천만 한다. 목록 밖 읽기는 「추가로 읽은 것」에 hash·bytes와 함께 남긴다.
- 열린 질문: 없음. 불일치 발견 시 GR 번호로 리드에 반환하며 변경 권한을 추정하지 않는다.

### 준수 연결(계획)

| 원문 절 | 준수 계획 | 실제 위치 |
|---|---|---|
| 역할별 적용·작업 맥락 | 최초 메모, 원시-기록 대조, 미검토·불가 분리 | 이 최초 메모; 결과 작성 뒤 「실제 준수」 갱신 |
| 교정 층과 반복 규칙·하네스 | 첫 발생/반복을 구분, 후보 최대 두 개와 검사화 대안 | 결과 작성 전 |
| 사람의 탐색과 가독성·주석과 문서 | 근거 파일:행을 붙이고 수기 집계를 측정으로 표시하지 않음 | 결과 작성 전 |
| 생존 신호와 초과 정산 | 독립 heartbeat 호출, 실제 UTC 간격 계산 | 결과 작성 뒤 「생존 신호」 |
| 파일 위치·Gardener 권한 | 보고서 외 쓰기 없음, 제품 실행·직접 수정 없음 | 결과 작성 뒤 「자기 쓰기 감사」 |


## 결론

**종료 기록 실사를 완료했다. 제품 PR/녹화 처분을 바꿀 차단 관찰 0건, 종료 문서 리드 반환 3건(GR-01~03), 정리 후보 2건이다.** GR-01은 BACKLOG 축약 SHA256 오기, GR-02는 생존 신호 반복 횟수의 집계 범위 누락, GR-03은 오래된 후보 출처 메시지 원시 미확인이다. 세 건을 정정·보충하거나 메인이 확인 수준을 정한 뒤 종료 기록 PR에 이 보고서를 반영한다. 별도로 이번 Gardener의 생존 신호 300초 초과 1구간(GD-01)을 자기 결함으로 공개한다.

PR291·PR268의 독립 검증 통과와 사용자 승인 병합, 녹화 빌드·리허설·녹화 v3·3회차의 메인 수용을 유지한다. 불합격 판정 원문을 통과로 고치지 않았고, 제품 재시험·영상 재생·DB 조회는 하지 않았다. 후보 도착 helper는 allowed/exit 0, 생존 정산 helper는 over/exit 1이다. 이 exit 1은 과거 초과 구간의 실제 정책 결과이며 도구 실패나 제품 실패가 아니다.


## 점검 범주별 결과

범주 사이에는 같은 사건이 중복될 수 있어 아래 행을 합쳐 총 결함 수로 쓰지 않는다. “발생 수”의 단위와 확인 범위를 함께 표시했다.

| 범주 | 발생 수·단위 | 원천(모두 E 기준, 별도 표시 제외) | 처분 상태 |
|---|---|---|---|
| 독립 결함 | 새 번호 25건: 당시 차단 18·비차단 7. 중단 C1은 뒤 D1과 중복하지 않고, 원천 없음 N1은 별도 1건 | 아래 판정별 표; 각 verdict의 기계 판독 표 | 이미 처분됨: 수정·재검증, 정오, 메인 수용을 구분 |
| CI 실패 | 최종 CI job 실패 0/8(두 PR 각 네 workflow). 조회 도구의 연결 실패 1회 | lead/pr291-view-02.json:1, pr268-view-ci-final.json:1; pr291-checks-watch-01.txt 마지막 두 줄 및 watch-02 마지막 줄 | 이미 처분됨: 재조회 exit 0. Unity 시험은 CI 범위 밖 |
| 새 경고 억제·설정 완화 | 두 제품 diff의 새 pragma/SuppressMessage/Ignore/Explicit/ignoreFailingMessages 0건, 저장소 검사 설정 변경 0. 녹화 준비 관문 재정의 C13 1건은 별도 공개 | Git 두 merge diff(A1·A13), G:527–528; recording/run-01/report.md:265,314,316 | 제품 억제 해당 없음. C13은 리드 승인·원시 보존된 관문 변경이며 이미 처분됨 |
| 임시 우회·예외 처분 | 아래 정의한 운영 선택 9건: base 생략 1, 중단 1, 도구 재실행 1, 시험 채택 보류 1, 녹화 단계 수용 4, C13 관문 변경 1 | G:45,395,411,16,448,500 전후·556–564·620–624·527; 해당 판정/메인 메시지 | 이미 처분됨, 시험 채택은 기존 후보 연결. 일반 예외로 확장하지 않음 |
| 드리프트·규칙 검사 | Unity 복원 묶음 8회, helper 초과 25구간/20 Dispatch·질문 대기 9, 자동 escalation 4, Content preview 가림 산출물 8파일/2handle. 후보 8개 도착 | lead/*-drift/*·drift-restore-01.txt; C/liveness-output-04.json·settlement:17–80; mask-server-preview-01.json:7–63·02:7–16 | 복원·가림·완료보고 정책은 이미 처분됨. 반복 두 갈래는 정리 후보. 기록 GR-01~03은 리드 반환 |

새 `-demo-no-bgm`은 효과음을 남기고 BGM만 끄는 녹화 입력이다(G:174 전후 사용자 123 A, DemoLaunchOptions.cs:17·52). 경고/테스트 실패를 숨기는 설정이 아니다. `DemoKeyboard.Install`의 IgnoreFocus·실키보드 비활성은 두 클라 자동 입력의 실행 동작이며 정적 검사 완화가 아니다. 해당 사용 경로를 읽었고 일반 사용자 입력의 새 시험을 실행한 것은 아니다.

C13은 “OS 프로세스 시작→첫 프레임”에서 “창 첫 관측→첫 프레임”으로 기준 사건을 바꿨다. 이전 실패를 지우지 않고 1·2차 원시를 남겼으며 실제 회차의 `firstFrameBeforeCharacterList`는 유지했다. 시작 전 구간의 지연까지 같은 보장으로 읽으면 안 된다. 변경 권한은 `msg_ca70a8a5372a`와 G:527이며 Gardener가 완화를 승인한 것이 아니다.


## 발생 수와 원천

### 독립 판정별 집계

기계 판독 표의 처음 발행된 D번호만 셌다(A22). 같은 D1이라는 이름이어도 판정/대상 Task가 다르면 별개다. 25 = 2+1+1+3+5+7+6, 당시 차단 18 = 0+1+1+2+4+7+3이다.

| 원문 | 새 결함·차단 | 처분 및 귀속 |
|---|---:|---|
| pr-a-verify-01/verdict.md:18–20,129 | 2·0 | 구현/RED 보고 수치 오기 D1/D2. G:377에 실제122+/65−, a20/c3 정정. 제품 통과 |
| pr268-verify-01/partial-report.md:43–48,108 | 확정 0 | 사용자 중단. C1 실행 전 후보는 다음 검증 D1로 이어져 중복 집계 안 함 |
| pr268-verify-02/verdict.md:17,84–92 | 1·1 | 열린 메뉴의 null을 Legacy로 오인. 제품/리드 계약 가정 귀속. v3 verdict:16–17,114에서 해소 |
| pr268-docs-recheck-01/verdict.md:19–20 | 1·1, N1별도 | D1 발신자 오기. N1 당시 프로세스 원시 없음. recheck-02:18–28에서 D1과 N1 해소; 제품 재검증과 구분 |
| recording-build-verify-01/verdict.md:19–22 | 3·2 | D1 작업자 계획 밖 실행·승인/환경 근거 부재, D2 PID 분류 비차단, D3 검증자 원시 보존. 메인 `msg_b50c04ca0c08` 수용 A, 재빌드 없음 |
| rehearsal-v2-verify-01/verdict.md:18–22 | 5·4 | D1/D2 실행 절차, D3 리드 수치, D4 이전 audio 알림 비차단, D5 검증자 heartbeat. 정정표 + `msg_d79eebc3d94c` 수용 |
| recording-v3-verify-01/verdict.md:20–26 | 7·7 | D1–D3 실행자, D4–D6 리드, D7 검증자. errata:10–13, `msg_45234d43130c` 결정3 A로 정오/기록 수용 |
| recording-r3-verify-01/verdict.md:29–34 | 6·3 | D1–D3 검증자 자신, D4/D5 리드·D6 계약은 비차단. G:609–611 정오, `msg_296cdc426451` 수용 A |

녹화 빌드 D1은 메인이 해당 계약의 확정 실패 1회로 셌다. 검증자 자기 D3와 다른 단계/다른 번호를 구현자 실패 횟수에 합치지 않는다. PR268 D1의 계약 귀속 처분(G:414)도 유지한다.

### 반복 네 갈래의 직접 재집계

| 갈래 | 이 goal에서 확인한 단위·횟수 | 다른 goal 원천·반복 여부 |
|---|---|---|
| 검증자 생존 신호 | 전체 helper: 검증자 7세션·11초과 구간. 자기 결함 번호를 붙인 녹화/리허설 검증 3세션. 근접 표식377/445, 정확322.222/489.939는 정밀도·중간 status 유무가 달라 helper377/322/490과 구분 | `01_Phases/goals/2026-10-04-persistence-integration/goal.md:985,993,1280`에도 세션 반복. `2026-10-11-pre-freeze-rule-docs/goal.md:191`에도 작성 세션 초과. 이번이 첫 발생 아님 |
| 리드 판정 수치 오류 | 리허설 v2 D3 1묶음 + v3 D4~D6 1묶음 + r3 D4/D5 1묶음 = 3묶음. 원문 정정표: rehearsal correction:8–14, v3 errata:10–13, G:609–610 | `2026-10-11-pre-freeze-rule-docs/goal.md:191,205`에 리드 수치/기록 정정이 이미 있음. 이번 범주의 “3”은 녹화 판정 묶음 수이며 개별 숫자 오류 수가 아님 |
| 터미널 preview | Content의 가림 기록은 01판7파일(term_7cc954de), 02판1파일(term_f672813a): 합8파일·2handle. 두 JSON의 records 수를 기준으로 한다. 정책 교정 뒤 재발 횟수라는 뜻이 아님 | B:515와 `2026-10-31-db-persistence-demo-build/gardener.md:118–122`의 Core 4파일+24칸1파일, 공식 경계 사건1회 기록. 동일 갈래의 기존 후보 `terminal-evidence-field-allowlist` |
| worker_done 없이 턴 종료 | 저장 메시지의 자동 escalation id 네 개, 11-05 17:33:50Z·20:12:54Z·21:02:48Z·21:22:55Z. 빌드1·리허설v2실행3 | 다른 goal 검색에서 보고 누락/미완 checkpoint 관련 기록은 있으나 같은 stopReason의 확정 사건을 추가 집계하지 않았다. 이 goal의4건 자체가 반복이며140 A가 이미 다룸. 관련 운영 기록 `2026-10-04-persistence-integration/goal.md:1609`는 유사 대책이지 같은 사건 수가 아님 |

### 생존 신호 25구간의 합·최대

`C/liveness-output-04.json`의 모든 overGaps를 Dispatch·from.type으로 분류했다(A13). 저장 정산의 각 구간과 일치하며 합은3+3+2+17=25다.

| 갈래 | 구간 | 최대 초 | 정산 원천 |
|---|---:|---:|---|
| 자기 판정 결함 세 검증자 | 3 | 490 | settlement:64–66 |
| 리허설v2 실행, escalation 뒤 대기 | 3 | 2179 | :61–63 |
| PR268v3 창8 대기 | 2 | 1888 | :52–53 |
| 나머지 작업·Unity 중 신호 공백 | 17 | 431 | :42–51,54–60 |

질문 대기9는 :72–80과 일치한다. 3회차 질문 `msg_172f56d2069c` 답 메시지 본체 미보존으로108초가 일반 간격에 남아도300초 판단은 바뀌지 않는다(:7). 리허설 약445초는 중간 status가 있어 helper에서는 초과가 아니며, 이 차이를 데이터 오류로 고치지 않는다(:64).

### Unity drift와 임시 선택

Unity 자동 drift는 PR-A U1/U2/U3 세 묶음(각 MinimapRT/ProjectSettings), PR268 창6·U3v2·창A·창B 네 묶음, 녹화 빌드 한 묶음으로 총8회다. `lead/u1-drift/status-after-restore.txt:12`, u2:3, u3:3, window6/status-after-restore:7, u3v2/restored-at:1, windowA/after-restore:1, windowB/after-restore:1, drift-restore-01.txt:17–26에 복원 결과가 있다. 제품/시험 변경은 남기고 생성 drift만 되돌린 기록이다. 이 세션은 복원을 실행하지 않았다.

임시 선택9건의 단위는 작업 경로/처분 한 건이다. (1) PR268 base 변경 차단 뒤 메인 생략, (2) 검증v1 사용자 중단 후 신규 검증, (3) IL Post Processor 시작 실패의 같은 명령 재시도, (4) Recheck 시험2개+.meta의 PR268 채택 보류, (5–8) 빌드·리허설v2·녹화v3·r3 절차 결함 수용, (9) C13 준비 관문 재정의다. C13은 위 범주에도 중복 노출했다. 독립 결함 자체를 우회 성공으로 바꾸지 않으며, 리드 수치 정오는 우회가 아닌 정확성 복구로 분리했다.


## 종료 기록 대조

지정 절의 수치·SHA256·경로·msg ID를 표본으로 고르지 않고 아래 범위로 나눠 대조했다. 본문 서술의 화면 관찰은 그림 금지에 따라 **저장된 판독/프레임 장부와의 일치**만 확인했다. 새 시각 판정을 했다고 쓰지 않는다.

| 고정 입력 위치 | 대조한 주장·수치·경로 | 원시·결과 |
|---|---|---|
| G:7 | 다음 순서, 3회차 A, v3 원본 유효, 재녹화 없음·편집 담당 | main-msg_296cdc426451:9–12, main-msg_45234d43130c:본문과 일치. 현재 Gardener→종료 기록 PR이며 완료 PR로 앞당기지 않음 |
| G:9 | 빌드 완료/Core 전달, D1·D3 불합격, 수용 A·재빌드 없음 | build verdict:3,19–21; 저장 msg_b50c04ca0c08 본문, send-build-handoff-core-20261106-02.receipt.json:1의 msg_c8dd2ae6e315 및 같은 이름 발행 cjs:26–45. receipt는 ID만 담아 실제 전송 body 전체의 별도 원시는 없음; Core 후속/발행 소스와 교차했다 |
| G:10 | 다섯 PR과 merge SHA·시각 | Git merge 객체 직접 확인(A12): PR291 58c8d4cb, PR268 3bb76772, PR294 a888cd35, PR295 fbf00de9…2631(11-04 12:13:47Z), PR297 cfe8bb45…9c1e(11-05 16:57:26Z) 일치 |
| G:11 | 고정 head, 서버 diff0, Current23, R1 build-03 exit0 | lead/fixed-head-check-01.txt:1–24, C/liveness-input-04.json의 msg_9203c45e18e9:4371부터 본문. Core가 확인한 서버 실행이라고 표시된 수준과 일치. 다른 worktree/DB를 직접 재검증하지 않음 |
| G:12 | v12 경로·전체 SHA e0c846ce…7083, A 수용, 109/110/123·제출 기한 | v12 실제224입력 hash 일치; msg_e8dd9cb571fd(입력:4392), G의 결정109/110/11-05절, lead/check-nowait-20261104-02.json의 msg_8d6a75a7daed 원문(11-09 14:00 KST)과 일치 |
| G:13–14 | PR-C 취소113 A, Art75/후속 미착수, 종료 branch·원격 push·미개설PR, 시험705 Gold_bar | msg_478035aa5063 본문, G:151–165의 재진입 지시. local origin/docs… = fff227fa, origin/main=f1bcae3b(A26). PR 미개설은 발행 시 리드 기록이며 gh/웹 금지로 현재 원격 PR 상태 재조회는 미실행. 코드705 및928–940 직접 확인(A12·A22) |
| G:16 | Recheck 두 시험+.meta 미채택, 사본·hash·유지 권고·다음 후보 | pr268-verify-03/verdict:265, untracked-tests-saved/copy-sha256.txt·source-sha256.txt의4줄 동일, 네 실제 파일 hash 전부 일치(A27), msg_e0b59e962663 본문 |
| G:17 | 열린 결정 없음,135 A·129/130/131·133/134, v2 메인 수용, Core 새이름 rec_ally_mage/새벽동료·카드v6 | main-msg_17a5453d6222:8–10, main-msg_dfc546b80260, G:186–218, msg_25e147f44c7f·b28231a86423·25dbd7e547fa·c946082e5b5b·3a756dd537ba의 저장 본문. 리드 송신5e1339c81084/c277074f6a51는 각각 receipt와 발행 cjs를 교차(실행된 전송 body 전문 원시는 없음) |
| G:18–19 | 다섯 빌드 입력·v23, E 경로·lead/resume-state.md | msg_d0012352013c/d783ac17eb63/d0250483d004 본문·Git merges·manifest 경로/실제 존재 일치 |
| G:24 | PR-A branch/base5bfda911/merge58c8d4cb, PR268 원격/로컬 이름·base main, PR-C 미생성, 종료branch main f1bcae3b 합침 | PR291 merge 두 부모/원문 계약·pr291-view-02, PR268 merge 두 부모·pr268-view-ci-final, msg_db6333b6b1c6. 종료 merge c1ca41e6의 둘째 부모가 f1bcae3b. 현재 branch와 일치. 역사 branch 이름을 현재 활성 branch로 오해하지 않음 |
| G:244–254 | 140 A 원문·sha·계약 마지막 문장·자동감시 두 차례·Rules 후속 | main-msg_391a8e696163:7–9와 글자 의미 일치; 실제 SHA e26e3938…3b9b 일치. gardener 계약:96, r3계약:165에 지정 문장 존재(A27). 자동 감시 구현 완료 주장은 없고 이 실사는 미검사 |
| G:342–355 및637 | 이전13→현재12, 표식·이름·PR표기 | 현재12·진행1·이름 최대12문자(공백/영문 포함 보수적 셈), 전부 허용표식. PR291/268 병합 형식. 이전 c1ca41e6의 진척 목록과 취소 줄 제거 대조. 최대12 허용과 일치 |
| G:600–604 | r3 판정 불합격3, 자기 D1–D3, worker_done9db…, verdict SHA, 489.939초·05:32Z수정·큰JSON | verdict:29–31·226–246, hb007/008의 sentUtc 실제 차489.939, helper-revision.json의 수정 기록; 실제 SHA41709a19…0f7d4 일치. 화면/로그 내용의 실행자 귀속 차단0은 이 독립 판정의 한정된5항목 범위 |
| G:605–611 | D4 R8 원시01:16:45.5985121Z·11행, D5 207–238→196/6.533초, D6 ClassId252–253 | main-msg_0496a60fc54e:11 정확 일치; visual-observations.json/frame-ledger와 verdict:178·196·288; Git CharacterMenuPresenter.cs:252–253 직접 확인. 장면은 재판독 미실행 |
| G:613 | 영상21765031 /96117a54…7640 /91.333초·2740프레임, 소리36517400 /a09d46c6…1049, 표식47337 /967919c2…3f04, 무음 편집·소리 미사용41행 | 매체 세 파일 Get-Item/Get-FileHash 재계산 전부 일치(A18). probe-video.stdout:35,72와 길이/프레임 일치. r3계약:41 직접 확인 |
| G:614–615 | 로그인90, 완전141–164/4.700–5.467, 부분139–140·165–179, 마을196, 소리 자름2.9214858–3.9522475, PNG 장부·ID행·hb시각 | visual-observations:5,14,26,35,47,56,104,133; audio-timing-recompute:lowerTrimSeconds/upperTrimSeconds. 선택13장 PNG hash와 ledger 일치(A20), 이미지 미열람. 로그는 rg LF 줄번호로 R1:81/R3:76의 같은ID 확인. PowerShell Get-Content 인덱스는 로그 CR 혼합에서 다른 줄을 보여 rg로 교정했으며 잘못된 첫 추출을 사실값으로 쓰지 않음 |
| G:616–617 | pane 닫힘·남은Content작업자0, e346… 보고와 결정요청1건 | close-recording-r3-verify-01.json:result.close.ptyKilled=true. redacted 목록5개 중 Content경로는 리드handle 하나뿐(A25). 발행 cjs/receipt+메인 답 msg296…가 결정요청·A/B 의미를 확인 |
| G:618·625–631 | 반복3·3·preview 메인집계 | 리드 수치 오류는3판정묶음 일치. preview는 Content8파일2handle이며 메인 전체집계는 주장하지 않음. heartbeat 분모는 **GR-02** |
| G:619–624 | A·sha2985fb70…ebc6, 실행자차단0·다섯 항목·신규검증없음·재녹화없음, 기사UUID와R1:81/R3:76 | main-msg_296cdc426451:1–12, 실제hash, 두로그 rg 원시와 일치. “메인 결정이며 사용자 결정 아님” 구분 보존 |
| G:635–636 | 맥락시각04:19:53Z, 쓰기 전, E/C, PR300/f1bcae3b/c1ca41e6, 충돌0·8파일·겹침0 | lead-context-memo-closeout-01:4–5와 Git 부모/merge-base 대조. 분기 공통기준 cfe8bb45, main8파일/topic1파일/겹침0(A13). 최초 작성 순서는 원문 메모의 자체 기록 수준이며 파일mtime을 과거 쓰기 증명으로 대체하지 않음 |
| G:638 | 후보6행·원본/쓰기cjs/json, before dd63edc7…fde206→after75ad4e99…120cdc, 취소·기존4행상태 | backlog-write-01.json:4–9의 전체hash, 입력BACKLOG 현재hash, 새절523–528의6행, B:470·441·481–483과 일치. 새절 Art축약hash만 **GR-01** |
| G:639 | 후보8·ID244·중복/unknown0, allowed/exit0 | 정본helper 고정사본1회 결과 일치. 8개 전부BACKLOG ID이므로 사본 상대경로 부작용 없음 |
| G:640–650 | 20Dispatch,25초과,9질문,3/3/2/17·최대490/2179/1888/431, hash9b533dc3…1e026e,489리드오기·108질문답누락 | 정본helper stdout이 저장liveness-output-04.json과 구조까지 동일. A13 전수재계수, settlement:61–80. 질문 원문msg172…와 answer receipt의 body없음도 일치. 전체 대화감사 미실행 |
| G:651–654 | 자동escalation4·시각4, 기존후보3연결, FEATURE_MAP41·길드프로세스수명, CURRENT | liveness-input에서 type escalation+worker_done만 필터해4건(A8), FEATURE_MAP:41 직접 확인, B:515·527–528 ID존재. CURRENT:7과 current-write-01.json의newMid·hash일치 |
| G:656–660 | 한계3, 예외launcher4의 사후명령, 수동heartbeat3 UTC없음, DB미조회·편집범위밖 | r3보고:78·148, 리드판정:95·120·123·128과 일치. mtime으로 작성도구를 확정하지 않은 점 유지. “매체진위 영향없음”은 수용된 리드판정이며 이세션의 실행 전수 감사 아님 |
| G:662–673 | 후보8개·제출후착수, Art1.3배,Gold_bar705,Recheck2,WaitFor,반복2,preview,4칸85A | 아래6행 전수대조 및 B:484·515. 모든ID존재. 마지막 msg45b…의 원시만 **GR-03**, 이전goal 기록은 일치 |
| CURRENT:7 | Content경로/branch·main·제품병합·녹화처분·종료기록 진행 | G:7,14,24,633–654와 일치. Gardener 완료·종료PR병합으로 미리 쓰지 않았음 |

### BACKLOG 새 여섯 행의 이유·출처·코드

| B행·ID | 코드/원천 대조 | 결과 |
|---|---|---|
| 523 character-create-screen-art-polish | Art원문:10–12의 안내문·선택강조·232폭·1.3배, PR-A verdict:52–54, msg7f… 시각16:25:08Z | 내용 일치, 축약SHA만GR-01 |
| 524 guild-consumer-test-gold-bar-reference | Git fff227fa 시험:705,930–940의 log-only/continue; PR297 diff에서 해당시험변경0; msgc421… 11-04 11:32:05Z, msg8d… 재진입 원문 | 일치. 옛HUD명 잔재이며 현재 시험 실패라고 쓰지 않음 |
| 525 demo-autopilot-recheck-tests-adoption | verdict:265 유지권고, 사본4개와원본hash목록 일치, msg e0b… 11-01 04:22:27Z | 일치. 최신main 재실행/채택은 미수행 |
| 526 demo-autopilot-step-timeout-result | Git fff227fa DemoAutopilot:482·486,21; DemoLaunchOptions:13; v2 verdict:43–44·v3:47–48,74 | 일치. timeout경고 뒤 다음단계라는 기존 설계관찰이며 새실행 실패가 아님 |
| 527 lead-judgement-raw-value-crosscheck | 리허설v2 D3·v3 D4~D6·r3 D4/D5, 정오표·G609–610; msg296… 05:55:02Z | 세판정묶음으로 일치. 더넓은 “리드글과원시” 범주(정규식·R7문구)는 별도 |
| 528 verifier-heartbeat-during-compaction | 자기판정3, 직접sentUtc322.222/489.939,리허설근접표식377/445, 정산표 전체7세션11구간 | 숫자의 출처는 일치, 집계분모누락GR-02 |

`guild-entry-gold-overlap` B:470 상태는113 A취소와일치한다. 이전 선행조건 칸의74 A는 역사착수기록이며 현재상태인폐기와 구분해 읽는다. 명령·판정은 아래 실행기록에 남겼다.


## 리드 반환(GR-xx)

### GR-01 — BACKLOG의 Art 의견 사본 SHA256 축약값 오기

- 위치: B:523 `character-create-screen-art-polish` 출처의 `2be227f1…6a826c11`.
- 실제 파일 `E/lead/a8-art-opinion-msg_7f9778ed1c3d.txt`를 SHA256으로 재계산한 값은 `2be227f1c5ecd477ffb7d8bf6685c22dfe3458f09047b2310c2d9b922a826c11`이다. 끝 여덟 자리는 `2a826c11`이며 `6a826c11`이 아니다. G:378의 전체 hash는 실제 값과 같다.
- 처리: 리드가 BACKLOG 해당 셀(필요하면 등록 원본 C/backlog-section-01.md의 사본 관계도 설명)의 축약값을 정정한다. 제품 판정·Art 의견 내용에는 영향 없음. 이 보고서는 파일을 고치지 않는다.
- 분류: 종료 문서 정확성의 리드 반환. 검사 전환은 고정 경로+전체 hash에서 축약 표시를 생성·대조하는 방법이 가능하다. 별도 정리 후보로 중복 제안하지 않는다.

### GR-02 — 검증자 생존 신호 반복 횟수의 집계 범위 누락

- 위치: G:618·629·671, B:528의 “이 goal에서 세 번” 표현. 메인 사본 `lead/main-msg_296cdc426451-round3-decision-A.txt:12`도 같은 세 번을 인용하지만, 원시 전체 발생 수와 구별해야 한다.
- `C/liveness-output-04.json`을 Dispatch/역할로 재집계하면 초과한 검증 세션은 **7개·11구간**이다. PR-A 검증 2, PR268 v2 1, PR268 v3 2, 녹화 빌드 검증 3, 리허설 v2 검증 1, 녹화 v3 검증 1, 3회차 검증 1구간. 원천은 `C/liveness-settlement-04.md:19,23,25,29,32,34,36` 및 해당 JSON.
- 세 번은 **녹화/리허설 검증자가 자기 판정에 결함 번호를 붙인 세 세션(D5·D7·D1)**이라는 범위에서는 맞다. G:644의 “검증자 자기 판정 결함” 세 구간도 맞다. 세 번을 그대로 유지하려면 이 한정을 반복 표·BACKLOG 설명에 적고 전체 정산 7세션·11구간과 함께 연결한다.
- 처리: 메인 수용 결정을 바꾸지 않고 사실 문장의 분모만 명확히 한다. 제품/녹화 원본의 진위를 뒤집는 차단 관찰이 아니다. 리드가 같은 범주를 전체 위반 수로 표현하려면 위 원시 집계로 고친다.

### GR-03 — 마지막 후보의 메시지 원시가 허용 입력에서 확인되지 않음

- 위치: G:673 `character-slot-count-four`의 `msg_45b7227b04ff`.
- 고정 입력의 메시지 집합에는 해당 ID가 없다. E/lead의 허용 JSON·txt(금지 터미널 원본 이름 제외), 목록 파일 및 허용 receipt 검색에도 해당 원시가 없었다(A8·A9·A24). 현재 B:483–484와 Git 고정 `01_Phases/goals/2026-10-30-login-screen-guild-window/goal.md:77`은 같은 ID·85 A·4칸 후속 결정을 기록한다(A28).
- 따라서 결정 내용이 틀렸다는 판정은 아니다. 기존 goal을 통한 기록 일치는 확인했으나 이번 계약이 요구한 **원시 메시지 대조**는 실행불가다. 리드가 해당 메시지 사본을 추가하거나, 이전 goal:77을 직접 출처로 연결해 “이전 기록에 따른 정보”라는 확인 수준을 명시한다. 입력 범위 밖 옛 근거 폴더를 임의로 열지 않았다.
- 분류: 근거 미확인 리드 반환, 제품 차단 아님. 독립 결함 25건이나 구현자 실패 횟수에 합산하지 않는다.


## 정리 후보 1·2

### 후보 1 — 리드 판정 수치를 원시에서 계산·대조

- 기존 BACKLOG ID: `lead-judgement-raw-value-crosscheck`(B:527). 새 후보를 중복 생성하지 않는다.
- 반복 단위는 녹화 독립 판정 **3묶음**이다. 리허설 v2 D3(시각·구간), v3 D4–D6(인증된 원시 수치·해석), r3 D4/D5(100ns UTC·장면 경계)가 각 정정표와 verdict에 남았다. 이전 `01_Phases/goals/2026-10-11-pre-freeze-rule-docs/goal.md:191,205`에도 리드 기록 정정이 있어 첫 발생이 아니다.
- 교정층: 문구 주의를 늘리는 대신 기존 `99_Tools/Orca/`의 읽기 전용 정본 검사에 원시 경로·필드·표시값 대조 기능을 두는 안이다. UTC는 정수 100ns/원문으로 보존하고, 파일 전체 hash에서 축약 표시를 생성하며, 프레임/PTS와 로그 사건의 구분을 입력 필드로 요구한다. 이번 GR-01의 hash 오기도 같은 책임으로 다룰 수 있다.
- 검사는 정상값만 재계산하지 말고 7자리 소수초 반올림, Z/+00:00 동치, 동일 D번호의 서로 다른 Task, 원시 누락, 한 글자 틀린 hash, 장면 관찰을 로그로 대신한 사례를 fixture로 넣어 각각 거부/미확인을 구분한다. 제품의 계산을 복제하는 성공 보장 시험으로 만들지 않는다.
- 대안: 원시 값을 표로 옮기는 수동 이중 점검은 도입 비용이 낮지만 이번 세 판정에서 반복된 전사·단위 오류를 계속 사람이 맡는다. 프레임 의미 판독은 자동 검사만으로 대체할 수 없으므로 사람이 확인한 관찰 출처와 검사 가능한 수치를 분리한다. 이 보고서는 구현·규칙 채택을 하지 않는다.

### 후보 2 — 문맥 정리 중 생존 신호 공백을 감독 상태와 연결

- 기존 BACKLOG ID: `verifier-heartbeat-during-compaction`(B:528). `heartbeat-interval-review`, `worker-heartbeat-target-gap-table`의 범위를 연결하며 새 중복 행은 만들지 않는다.
- 반복 단위는 전체 정산의 **검증자 7세션·11구간**, 그 중 자기 판정에 번호를 붙인 녹화/리허설 **3세션**이다(GR-02). 이전 `2026-10-04-persistence-integration/goal.md:985,993,1280`, `2026-10-11-pre-freeze-rule-docs/goal.md:191`에도 초과가 있다. **이번 Gardener도 300초를 넘겼다**는 자기 결함을 아래 생존 신호 절에 별도로 기록한다. 고정된 과거25구간 입력에 이를 몰래 더하지 않는다.
- 교정층: 기존 `99_Tools/Orca/check-liveness.mjs`·정책 및 승인된 Orca 감독 흐름에서 문맥 정리·공식 질문 대기·worker_done 없는 정지 상태를 구분하는 검사/fixture를 검토한다. 140 A의 완료보고 감시와 겹치는 부분은 같은 후속에서 책임을 정한다.
- fixture는 299/300/301초, 질문 응답 전후, status가 끼어든 heartbeat 간격, 자동 escalation 뒤 정지, 잘못된 Task/Dispatch/from_handle, 작업 완료 뒤 늦은 heartbeat를 포함한다. 프로세스가 살아 있다는 자동 신호와 에이전트의 실제 진행 신호를 다른 종류로 보존해야 공백을 덮는 구현이 되지 않는다.
- 대안: 계약의 180초 목표·긴 쓰기 전 송신을 강화하는 수동 운영은 즉시 가능하지만 문맥 정리 자체를 넘는 통제는 어렵다. 자동 송신만 붙이면 정지한 작업도 정상처럼 보일 수 있어 감독자의 종료/재개 판정과 함께 설계한다. 자동화 도입을 이 실사에서 승인하지 않는다.

## 첫 발생·기존 후보 연결(후보 아님)

다음은 추가 정리 후보 수에 포함하지 않는다. 첫 발생은 해당 goal 기록에 남기고, 이미 승인된 후속이나 BACKLOG가 있는 항목은 그곳으로 연결한다.

| 항목 | 원천·발생 수준 | 연결/처분 |
|---|---|---|
| PowerShell -File 배열 인자 전달 실패 | G:365, 최초 실행 기록 | 단일 사건으로 기록; 새 반복 규칙 제안 없음 |
| 승인 창 밖 offlineCompile·R7 신호 오기 | G:366–367,473,490 및 해당 계약/판정 | 이미 교정·귀속된 사건. 단순 “수치 오류 3” 분모에 무차별 추가하지 않음 |
| IL Post Processor 시작 실패·동일 명령 재실행 | G:411 | 실행 환경 사건1. 재시도 성공을 첫 실패 삭제로 표현하지 않음 |
| Windows/WSL listener 해석·미지원 release --run 옵션 | G:419,422 | 서로 다른 단발 관찰. 이번 보고서에서 통합 반복 정책을 만들 근거 없음 |
| 서버 창 preview 가림 | mask-server-preview 01/02, B:515, Core gardener:118–122 | 기존 `terminal-evidence-field-allowlist`. 가림 파일8개를 사건8회로 오해하지 않음 |
| worker_done 없는 턴 종료 | 자동 escalation4개, G:244–254 | 사용자140 A의 감독 조치로 연결. 별도 세 번째 정리 후보 없음 |
| PR268 Recheck 시험2개+.meta4파일 | G:16, pr268-verify-03/verdict:265 | 기존 `demo-autopilot-recheck-tests-adoption` 채택 판단 보류. 제품 시험 실행/커밋을 여기서 하지 않음 |
| 시연 대기·주석·HUD 검사 강도·문구·4칸 | G:666–673, B:523–526 및 해당 기존 행 | 후보 도착 helper의8개 연결 유지. 원문 없는 메시지는 GR-03으로 확인 수준 반환 |

반복 검색은 고정 HEAD의 goal/gardener와 B만 대상으로 했다. 검색에 없다는 이유로 전 저장소·모든 과거 세션에서 처음이라고 단정하지 않는다.

## 설계 관찰과 대안

모두 비차단 관찰이며 제품 PR의 독립 통과와 메인의 녹화 처분을 유지한다.

1. 대기/타임아웃 값과 데모 문구가 시연 진행기 안에서 함께 보인다. B:526의 `demo-autopilot-step-timeout-result`처럼 시간 초과 결과를 명시하는 대안은 있지만, 기존 시연 순서와 장면별 실패 진단을 함께 보존해야 한다. 이 점검에서 설정화나 무관한 리팩토링을 추가하지 않는다.
2. `GuildGrowthEffectConsumerLifecycleTests.cs:705`의 Gold_bar 검사는 구체적이나 :928–940의 HUD 탐색은 로그 관찰이 섞여 있다. 로그 출력 사실을 전부 자동 assertion으로 읽지 않도록 B:524의 `guild-consumer-test-gold-bar-reference`에서 실제 HUD 부품과 관찰 목적을 맞추는 판단이 필요하다.
3. C13은 첫 창 관측 이후의 준비 시간을 보장한다. OS 프로세스 기동부터의 전체 체감 대기는 다른 지표다. 두 기준을 모두 원시에 기록하는 대안이 있으며 기존 합격 판정의 의미를 소급 확대하지 않는다.
4. 녹화 수용은 절차 결함과 산출물 유효성을 분리한 메인 결정이다. 원시 verdict·정오·수용 메시지를 함께 보존한 점은 재판정 가능성을 지킨다. 경로/단위/Task를 기계가 대조하고 장면 의미·제품 품질은 사람이 판단하는 분리가 적합하다.

그림 직접 판독·영상/소리 재생·새 제품 실행은 미실행이다. 저장된 관찰문과 hash 일치로 그 이상의 품질 통과를 주장하지 않는다.

## 실제 준수

| 적용 원문 | 실제 준수 위치·산출물 | 한계/차이 |
|---|---|---|
| 파일 위치와 이름·Gardener 권한 | 이 파일의 최초 메모부터 자기 쓰기 감사까지; 보고서 하나에만 기록 | 제품·시험·helper 파일 작성 없음 |
| 주석과 문서·사람의 탐색과 가독성 | 다섯 범주/원천 표, 지정 종료 기록 전수 대응 표, GR 번호와 구체적 조치 | 원시 의미 판독과 저장 기록 대조를 명시적으로 구분 |
| 역할별 적용·작업 맥락 | 최초 메모의 모델/기준/권한, 독립 결함 귀속 표, 미실행 절 | 요청 모델과 backend unknown을 분리; 첫 화면 미확인 |
| 교정 층과 반복 규칙 | 후보 두 개는 반복 원천과 정본 검사/fixture 대안 제시; 첫 발생은 별도 표 | 규칙 채택/구현/다음 goal 생성 없음 |
| 하네스 원칙·변화 평가 | 정본 helper 두 개 재사용, 결과 exit/stdout 그대로; 수치 산출 명령 부록 | 새 harness 제작·제품 계산 복제 없음 |
| 생존 신호와 초과 정산 | 아래 모든 성공 송신 UTC와 간격·자기 결함 | **300초 초과 발생: 준수하지 못한 구간은 그대로 공개** |
| 보존·동시 쓰기 금지 | 고정224입력 hash 대조, 마지막 Git/폴더 확인 | 파일시스템 전체 감시를 실행한 것은 아님 |

최초 메모의 “실제 위치” 칸은 작성 당시 계획 상태 그대로 보존하고 이 표를 결과로 삼는다. 위반을 계획대로 준수했다고 바꾸지 않는다.

## 추가로 읽은 것

고정224개 입력 외에 내용·검색 결과·파일 hash를 확인한 파일이다. E/lead JSON·txt는 GR-03 ID 검색 범위 전체를 보수적으로 포함했다. 그 파일들을 모두 전문 해석했다는 뜻은 아니다. 금지 이름 원본은 제외했고 PNG·매체는 hash만 읽었으며 재생하지 않았다. Git 원천은 fff227fa 기준, 아래 물리 파일 hash는 같은 checkout의 감사 시점 값이다. 스킬 파일은 세션 진입 지침상 읽었다.

추가 목록: 1244파일. 누락 0개.

<details>
<summary>추가 원천 경로·bytes·SHA256</summary>

| 경로 | bytes | SHA256 |
|---|---:|---|
| C:/Users/bass1/.agents/skills/orchestration/SKILL.md | 4348 | 66d4c60a3ead54eb063a57341278cccaf28e016c2847a6f1808a0d4d2d163606 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/a8-art-opinion-msg_7f9778ed1c3d.txt | 2231 | 2be227f1c5ecd477ffb7d8bf6685c22dfe3458f09047b2310c2d9b922a826c11 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/a8-captures-sha-01.txt | 559 | ef8e83ebc44872733f14a099035630cd04eb6ab707d060e47ecd4a9b6887f500 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-38.json | 479 | c2aa714a667955477e0631ed64962d0af23671f6030e7c2fd77fec9ebf8c4369 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-39.json | 479 | bf3eff39685e245c343fc4a4a2a594ac7fbccec0b467934469480a45c69e365b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-40.json | 479 | 6aa16baabcefd7191b048a60d52c5c9e36fdd94920b604b9e52903ee9131ba04 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-41.json | 479 | 609d82ffe85092ede3ce754c8d7d0be3a0c6fc414ce1d703f862fdfed39de3bd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-42.json | 479 | 6cfba20f80436c63031d3165e71d64d1ad53e1ae9ea6b3686f5042c48cde0751 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-43.json | 479 | 39e53cb35e4b34ceec0271956fff5997dbb63dad44acc270abbe260b196b9cdf |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-44.json | 479 | 2005752d45e010098569246692d1bdd709dfa54ea22a7bfb29cbd261001f2779 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-45.json | 479 | ec3d905cb70cba0c1aad55da28e478401c6ea24acad2a84751e2f8239b953f3a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-46.json | 479 | 3cc098fd4e80af2cde653780a9f487a7568e8f3f7c3d658accef08a565475b7a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-47.json | 479 | ff818b630d1f563ddc626020183a9a2df5cd7086d0a0bbde8df714ed821f1a5b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-48b.json | 479 | 68d07582f19268c0567a9329b98d82837890fba4e8715ddba00083f5e91e5c1c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-49.json | 479 | 7f5a1d6e0b05cf423e7c0b11a665898268d8f951e8a108cac5af8c11f28f0d3e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-50.json | 479 | 630fc9529410556d562c52b2c70b5d92e4b2404e01bd1ba15c8fd2457fdc9132 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-51.json | 1510 | 5253362ffc35954dadc31b781689f607b9ce1be2451eabf5fea267c3f9e6a199 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-51b.json | 479 | 3d974ef39bf3ffe5a175c84f81621bf1852927ac89c99a9a189e1562e8cb241c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-52.json | 479 | 9377b1db1bc09c3f8a23893f6c9f46fffe7477af9e6de675323d84dfc9916a1c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-53.json | 479 | be5ad9081729d320a0e720e5aee842248d691496b8b69e44be66a3b0751c7714 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-54.json | 1846 | a273bd4bd8699487267acd378217377f32a0693eb3e99a371e739798e579dc48 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-54b.json | 479 | 1352823a5f18663871dff0700b11ff151c569da8529e0ed9434cf1356037f52a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-55.json | 479 | 377e5a4529ffe7229b767f4205f184c42bd94747560a9964972db93158e47624 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-56.json | 479 | ca189d8c0ac233299c6562c1b098c59521fc3db619734738ff35ffbe8330288b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-57.json | 479 | 0114e491f2f76a326386ee3f659958e7d1fb5bc6ff9adc7ebb13ab85b647b772 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-58.json | 479 | 0f027d81fad13bd1396ba47ad4d9d00ee5acd32bc3074183dc5ef76dc1a8a16d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-59.json | 479 | 93eb9a1bbe3f759a7a4e888065cfa7edbf3e485f6df2e38e289af3bb39389509 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-60.json | 479 | a6c13142fc0a2d7ff84be918d869cd17874945bfd51c98f1e0b0190432d31259 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-61.json | 479 | dd312319f0cd401f6f2c1dd9568441885b29a19c848350e170f9ed0364a064cc |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-62.json | 479 | 778f4a741f0c8b4190eafc6a3e7247830ed6ab7b8bcca6ad035e3f1c2109be71 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-63b.json | 1379 | 043bb494dc5e07897fce7be1048a1e483c4e69b5a1e604c8461dc8dcc215e6c0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-63c.json | 479 | b972df7705abb6a73b023a85a0dfa351673b831d6a44fbaf05408b88cca8fad9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-64.json | 479 | fe152f1006120f2fdc45f527e920c8fc7e4b0397a635801ea464e75c5195fc83 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-65.json | 479 | 23515f2d18bb5ecd43b56c88e80190c9561a4e5616b53e96c22f97c7638da5cb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-66.json | 479 | 18a6b7fc976f542e55987a19c9045ee842e68363d42f3c8b720cdcfa3bc3ef45 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-67.json | 479 | d28a3ccd4d1550a35170793618fccfad16277133ca76a9860b9a063a52c0fd30 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-68.json | 2027 | 0c2af6cd4475a2d8455ee96cd91cc707567a53f202e6e673b7194958b16ce323 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-69.json | 479 | 5911958ebce1108fdf26c8e601f7feea2b9aa5bdbb6f325515be3fd01fb4caef |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-70.json | 479 | 1ad6d6e9e655206a7bc8d4edfc7fb5bc90f61496752f4e669a4c753e8c0cbeaf |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-71.json | 479 | 86169045c8b2878c35b6c095050dc2c470cf61dd51042a7a08e3e59017fb8c9c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-72.json | 479 | 2bac2e2509da55bc9b735f86dada11f57afbee09ddc60f89014f2c918412a0d2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-73.json | 2521 | 404a0071c005fbd0bfd144bed9805cceccb034f80322e130cb12c59694cbde22 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-74.json | 479 | ccc141e5a55499687107c06ba4ebb4525ad22f9471f8050917b7a25b1ec44696 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-75.json | 479 | 2c17081204dcf473a1223051a074733bfe01a629bde777e7335bae3ce3f6321d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-76.json | 479 | 7ab732c469d51f3e9d116ba99108a9f5a9773857b5264f3fc140c56f3a1c1aba |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-77.json | 479 | 298910ed2eb07a55df64bd55413bb4609e596644737dcc94fd8f4685eea2026e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-78.json | 479 | 7e3408a193c5087ca971b502ed3511953eff1a651a426b5bee682e6550db17d9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-lead-79.json | 479 | 977ed2fc9a8c3b82854ea7431bada97c0de3926c83be286dd572621f7a77f4a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait05-01.json | 479 | 5bfb9731bbe1e34c2f9269011e39e79860a78e41ac1a3f2119fbce30349640d2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait06-01.json | 479 | f48a42b54030a6af78ad1d9395f5e010649b9c9f494b2dfd2af525d586c275d8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait07-01.json | 479 | 4bc1180fd0518ecd12c25ee89662f19a86f515e89b579f10793f9addf69592c7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait08-01.json | 479 | 3aabbed6565fd1365edbac107c913ba1c74705d39e1fab10eae9ba2e4d0bf714 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait09-01.json | 479 | 37f97e2afc37faaeb91af31cd9cb08800f255e10bbaec15d7e21c44bd9b9dcf1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait10-01.json | 479 | ded504fd78841717be4102ed1c9cd5c81ebf6ae31b2ba1f5adb5812d92afe1ee |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait11-01.json | 1122 | 1778b2949623604f1e181daaab6ec1838e37c4d2a430e18f452cb22d484f7af0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait12-01.json | 479 | 4a9b7887539d9d2a1a819074ed91455c644ba7ec27300d099d3fe5d4daa38e09 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait13-01.json | 479 | cef0c07919991b41fa42cdf02a1a09a359b987aa307d2757d0f8ba315832b5f1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait15-01.json | 479 | 516a8ebaa7a45c665e68f3575b200889ad5719626e88d82a44b2eb2bc30a8c4d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait16-01.json | 479 | d35af74e7e83168e89596ae40e0a0e5892956268e84d66244b92f979ec9431ea |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait17-01.json | 479 | 37ee10d75bde3cea025740217ae39df5b89e2b2323b2df5b5b38cfd79606c6a8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait18-01.json | 479 | 42834124505502d8b03298679f3e70a6fec026bda1eabc1e42043a01f9824be0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait19-01.json | 479 | ebdd56557a2d1bba00b847ae19c8a8339975b5d403d04fb63cdc8d0c9a90f387 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait20-01.json | 479 | 456b67860d98f53d33cc00540ac7efa5e2e8301b2acb15cd8a95fb4322f08152 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait21-01.json | 1122 | e4c1b5d0b37dd4a15342ccabda136d12f887f8dc63999ce32b3742eb93feb9c4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait22-01.json | 479 | d8a2a55453bbd4b4565a30253d3ab3d1ba70c59c08671928dbf7095501990380 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait23-01.json | 479 | 144393af74c475edc3cf77cb9995c85768c5ec8511baf0f16bf111958e37b9c3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait25-01.json | 479 | c3e0e5f142d6c4712a06cdbabc385ad80cfbf9d01b6e4927d95f53729d13afc7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait26-01.json | 479 | 3bfa51dde525acc9e20c4c994874bb9a4e77684fee9caf69f43c5108cd48e12d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait27-01.json | 479 | fe886f10cd4bd65c19014fffdf9ba518671a5ce396fdd2542cba56e971566e37 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait28-01.json | 479 | 517252781c78bf8424d7642478f4cc5817475c29273c09924a5223f4a19f90e7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait29-01.json | 479 | be8fd3e5694207769901b25ed0f8d8136533a04d8d6230559cea503ad3cf4915 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait30-01.json | 479 | ac33819407ffce43af8bc99e120efdfe34d081d0d3fb5cc3884a34ea02ad6a2b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait32-01.json | 1530 | d7d0ec5e337b8b6d1d434219947313864b46e68df862dfc3c7d29d42a35d85e2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait33-01.json | 479 | 4da2d451276edbd7aebc4ab5d63e0b0eef892aee7eaec9d9eb118651b6c97687 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait34-01.json | 479 | 0c8d30181151b9ad8f79cca5d898b2861813e7f68719d8c3e24c3a4937566ef4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait35-01.json | 479 | 4b592d610fa9d8183db69f6333f3eee35143cde75677f049e37cd729d450f146 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ack-wait36-01.json | 479 | 3d50a14a1e48833cade8ad8598390c059726904b91e6100dfb19db43b0e4de72 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-impl-v1-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-impl-v1-01.stdout.txt | 171 | 107b5e2969ce1f6c8f2ccd761d526fce082fdf8379824a9ba0ed2356e7b6e33c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-red-v1-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-red-v1-01.stdout.txt | 170 | de5874f3b1c074aa5dbfaf91e617bc5b06da1860b2644f3cbdf832c9c60456b7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-red-v1-02.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-red-v1-02.stdout.txt | 170 | a4b1285c86c9456a21ab4742230eec6d3d414421fa0d7d4b2faaffc27b31d25a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-red-v1-note.txt | 127 | 6d21a030c378b6c22967295229365515b0bb255917a2498fb0b1b488f91a460f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-red-v2-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-red-v2-01.stdout.txt | 170 | 78b70d841d8b3bd8c998c9ea760be63cf70159d6efed2a45b00c55f96fe13d3f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-red-v2-02.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-red-v2-02.stdout.txt | 170 | d526186bdf85de72c65f739bce5ef098c3c33472365d1d5b79aa81c8f1cb0b82 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-red-v2-03.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-red-v2-03.stdout.txt | 170 | b775e54ce85f54358bd492e1da25af24d16155910c12692ace19f80bc6aedbcb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-red-v2-note.txt | 300 | b157cec8334f243194c9766c4ba69b0eccb999f046298190b8532ca414127ec8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-verify-v1-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr-a-verify-v1-01.stdout.txt | 173 | 5ee431466f9712bfc2e155ef12f96f4666fcbd817b2749ebf6f9da7031974636 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr268-impl-v1-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr268-impl-v1-01.stdout.txt | 172 | 25254fd65ef1a8634d0d0e93ab3191bb609819648f5862a3db9fa1ea24676528 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr268-impl-v2-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr268-impl-v2-01.stdout.txt | 172 | 0d2bf459bf639dd291848c24a7452560b8d23cd26877ebb3cd404bd3f3e8610a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr268-red-v1-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr268-red-v1-01.stdout.txt | 171 | 012304c10972124d5fcf540587d80130e791a162e4444f10cba4d127220d428f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr268-verify-v1-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr268-verify-v1-01.stdout.txt | 174 | 434c5a083840dfad4dea57b6973c1af865560d1ea4e6ecd403f8288da3cb855b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr268-verify-v2-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-pr268-verify-v2-01.stdout.txt | 174 | c62c1c157f09c66f8a3521fc857a8fb990cb1f040eeef62ad4e29cd3f3a6b309 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-recording-build-verify-v1-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-recording-build-verify-v1-01.stdout.txt | 249 | 924b9848fffc6baf98b029272e51bcaa5e946092a83a8ff8e90e796ed8966831 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-recording-v3-verify-v1-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-recording-v3-verify-v1-01.stdout.txt | 181 | 8e05b4b9b5e0ecf772f5d13969315da391c966c63ebf0642daead65dc2d67484 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-recording-v3-verify-v2-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/assemble-recording-v3-verify-v2-01.stdout.txt | 181 | eec404791a12a2e529f4135df04bbfd109543dc47e98ab911ba5ddb0dc3bbbbc |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/build-sample-check-01.txt | 2150 | 5cb5106c52f5f21d8910290129060e7f7af4e21c1cb50665c8a9ee5a52d29879 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/build-sample-check-02.txt | 222 | 8206e73ac22e5f2263ab6920200c58c8d5dd191b60500814383f7cbd6dad7766 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/build-window-open-01.clock.txt | 21 | ac83aae06b85e8d6f923ae4146f958193d820b7e0f114eb6557cf23a4513731f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/build-window-open-01.exit.txt | 7 | 28d3b9e880a77975493dc7e359144c0295a4f694cfe0af4f928c22307bc5c320 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/build-window-open-01.stdout.txt | 548 | 7ea6826f2cd7327a1735e27a2c0539298e7e3e30ced6d13c85f9541fd6327cb3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-after-goal-commit-recording-v3-01.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-after-r3-verdict-01.json | 1296 | 2a7823486918a1f7bd9bba07929c046b1a277bf8cf6a664384764c641bd47391 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-after-recording-v3-verify-01.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-after-resume-01.json | 460 | 9945cb7626929458bb132ba2a1a1802e4da938c79d23512104d915da4c99073b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-after-resume-02.json | 460 | 8bf7ae2545258b009819dcfa1a6b5594a19f998d5a4492a9ed6fd98ad7b57e09 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-after-wrapup-01.json | 655 | 9f4fb5bb58035e89f5c59298d8e482199d2a9bbf687a0f4ea964905923b6ef82 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-after-wrapup-02.json | 1182 | a35c9db859dee8b6dc01ffe79a35119aec9d9948f57439ed00507c516784d6a8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-candidates-pr-a-01.json | 462 | 7e266cc166bb32c2a6794e4c5e93c1fef32d987313dbd0aaa16da7901e13790a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-candidates-pr-a-02.json | 224 | 580e17d56d0c6dc2a0db591c14bacb42504f3b0373ceef581d3236c70a6c7a2e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-closeout-start-01.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-closeout-start-02.json | 2017 | 328e87ceef18c441dd3c4b606cf26ff545be371fa23d13bcc2e7b7064f831388 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-01-ack.json | 479 | bf881a1de66ce50f621f641a254fc1bc266cb561a553e06fee34657c6568b031 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-01.json | 1127 | 4e694a1b6d047046fc0514f02eb11c4b8b06b288c3f3d24021a9c41fa514bf7f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-02-ack.json | 479 | dc30f94a9615b619cea3832862bf3e7f219bf6b3183712277b89fb6ea72a28f8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-02.json | 1127 | dfa77f3f973f4abd4b572f3b76620dd716f110b27d38788c6aedb78bda1580f2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-03-ack.json | 479 | ce8a1bcc270d30bf0d2acce918f2f09623b68933a3009fc380b05db392738743 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-03.json | 1126 | b3001518740ef7a81fa109e7d3933a1098c7c42a8314b28251594218b327e7a8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-04-ack.json | 479 | 49f6af57036d17d3e5e0db11327f9004ff06f89be8ec0e8f0a675ea5c33f0eb7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-04.json | 1126 | c5584f36cab34b0e25afd003e7a490344d499e853c5bc61ee88fb07e1e50ffb1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-05-ack.json | 479 | 67722717d279decad3fce665c7472eb0c70e0dc4a539392eb9c12b4294e8f788 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-05.json | 1126 | c7d5f6954594fcee5fb648a5b386681c5f9f9eefafbb14631f46956b8be42468 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-06-ack.json | 479 | f714b54357eb303db1286bc7898c749b81adfb57327da8b573d3861ba8207a6f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-06.json | 1126 | 0a4edeb1746812be941b1763818a6caca8bf2cd4a7bdacbce5b8fcdf783af8cc |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-07-ack.json | 479 | 6b3eec48a1398013959c7b274f8058d16cee101351847c52b1af5d2901829039 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-07.json | 1121 | f3f19a423c6ae547cd2df52844c0ff9812b3bb0519392e279434363e4c20bfa9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-08-ack.json | 1121 | 8cadb8b1d75a6e77f36e9d1411e2df12a03e137f1415b473690c77751624d4b5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-08.json | 1121 | 70dbe52a86f7b4ae19c25946323266ad6f8853ad80db976c314e3e0692488f17 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-09-ack.json | 479 | 7f9478d818198870fd8f18c4caaccff2d177c008587f6674a667428146933651 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-09.json | 1104 | 6b70a73c97aa33b2016e6119b37c2ddb9424269f78d6cc16d828da2833e5b258 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-10-ack.json | 479 | 7333a6d1b8ec7005ff5e3f1ee790e0d4d395d272679f5e92f3683e3fb9c68db4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-10.json | 1104 | dc364848af19a5865edee8cbd5379b5a4c4fbb8e29907bf710bfa53568834e59 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-11-ack.json | 479 | fe6db6b7d027707c788139da040e9bcb8ccc6affde7346f897fdb6e09cbc8b52 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-11.json | 1111 | 910002f7cfe146b0582d0fa13a8e922a344d7196eb7be606d67fcbb8f0515a1f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-12-ack.json | 479 | 00531873b3e9e43aaab87e991f27b43cb14a452ae91fa4dd2b4247e7fd9712dd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-12.json | 1111 | 1b2f454d0ff0f04c56a31a3c933d066ad45456b238e38dd2a3250c17d404835b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-13-ack.json | 479 | 041c7340f0b441bef8b809da6bb1a50f7ad3d4e023271de2ab2737268dc0e0a2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-13.json | 1101 | 7d8be09c350a7c56da89c38bb5e4d2892b69f3e6fb51b1dffe661d0544e72ee0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-136.json | 460 | e4cd20e4b77d91e966c908e8cec3e12189fe3c20d961b327a31927776e73955d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-14-ack.json | 479 | 3f7a39f8a2cf0c922668857098bdd160a316be5d49b101631202bdf03dd39cc3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-14.json | 1125 | 81e074f30d62f26a98195fa4960e869ca1d1515efe4ea19e2b24b37b06081887 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-15-ack.json | 1667 | 24b166eff4cafc899c1b2054c206059b5ad8fe71ed319e44d857f083eb35bc7f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-15.json | 1119 | 57be1eb0db699b2f6261618e3449c44e076efa2f16584246a0b8c6798081ff9a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-16-ack.json | 479 | 43f32eeb4b090c17f7eb585a137996b1ea2c0d94f1ab42353641f6d8b0ab05d9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-16.json | 1121 | 69cb0de9fa46a6d8c9701d61aab48273aa69d7cb21efef37d8e0a332f41d08b3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-17-ack.json | 1836 | 2d6669da7dd40ceacc76138076f7bbbb1e6278f4e6b02e84904eb2703c42f5f5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-17.json | 1121 | a2704fa1e4c2df42f300e81ba94bd4b08fa7e9eb5cdfa41d1fd5c640d1058c24 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-18-ack.json | 479 | 58076f49156a6dea9936c4d36c4256d6dee3094b464a75bf8db89ad69bbaff66 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-18.json | 1121 | e3432e4f7f8b8fd193d1e53c9bf2fbc052a9c4495843433d7129a49509023009 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-19-ack.json | 479 | 2bd13057563b5908a29ca2376d4234fc8493d181e7f901ade6f5292c640db94f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-19.json | 1119 | ed00469cbeea1232bdc533f8f8386bffcb3e08b0ebc713e10aa139d4732c0f42 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-20-ack.json | 479 | 228143c4dc8cb30d241aac90f09ace1eb002a8834ce772537f941c1b7f5c0e1e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-20.json | 1119 | bab3497d612abc96c8120488cbb050ec4ff15c18f01a7c8ee569cd92809b6beb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-21-ack.json | 479 | 090d23d26398ee36d5b3c85d7e49e1ad836df00158f27bd07ffb169a96882037 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-21.json | 1119 | e1943ff59812dd6b1e9b6708f39e6ebf9b43cc054f24d36d348856cd2190c973 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-22-ack.json | 479 | c3072fb3b261b2e70e2eef271e481f1bc9626169ba1c05b998f7a4751e19dc27 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-22.json | 1119 | af50b33c6a754cc364fa60e757455483d9a9b473ce82647577f59b8acad3941f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-23-ack.json | 479 | 2b7c935cbf16a8f291f0f5cd04db4511d23ffa5a912dab59174a4b3b27b258f0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-23.json | 1119 | 2ad74bba74fb6a9d7b2cbb860446ac3264a5c8e0de0b9f69e2ff64d1f5927cf8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-24-ack.json | 479 | 0ff62a737b203e869d87698cc01c22ad29bec482a54c8ce5eef391c31d7550ff |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-24.json | 1119 | 1d8132f72aa35678d8c39eeab0ec7d3d5fab09f4136289b338c63a0ecbee8c25 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-25-ack.json | 479 | 256c4032019a7717c4532acce1f94c0359d92fc006a7a03d120a84f0db093524 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-25.json | 1119 | d9848ad78c3b3e3c980ae65cbea7933529e53ec2f4bfe1f47536c61350c4e2c9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-26-ack.json | 479 | 948afb4779778ed13a67cfbf59c4c282a753b4bfd23cef74a6f8fc8b86da6bab |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-26.json | 1121 | a5d94c5e714fa4257ec53319908850421d0f3f214960eae655781e056210d346 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-27-ack.json | 479 | 421bed0d3cc61abd173f5a7c7764499222c5e6cce47235335069c3bf53834a8f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-27.json | 1121 | e0d5c6b5ce104e313346be31c937cfbd920e769a6cfdd53793f872aad909154f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-28-ack.json | 479 | 4d3e3cb3d587d4166fa300a6b487fb8e22805b6a4cbfdb4ade5ba29e94f92981 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-28.json | 1121 | 21edb8b79436aafa3eb0095dfa649547d070ccc9f63bb66a868d4a81eed90ab8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-29-ack.json | 479 | 1acb45df858f153dfb4c0311f4f0ac1cb51db2e8258b36fd6f3bfa8101a3eec6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-29.json | 1121 | 9124eac3096ce1d94d46ec3a04700e0c897d53635dc76c1b00245d041c1a3473 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-30.json | 2739 | e198693b2028ed0662dc304fc208db27c017aab835345b66a790991e1c1824ba |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-31-ack.json | 479 | 4f4451d4a827adedbbc74d96b449a76ade9a37b5d712caebcd5691a350b94e9c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-31.json | 1121 | 8cdaed749642c20eb0e18b5053894e24e9bfe385b13ebe83a88c512cd45f8ee3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-32-ack.json | 479 | 0c3f6f610564ae5f2048292f5a92ee150df7b63fec994707f5bfb10d94240c85 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-32.json | 1121 | dc03fcea90dee14fa43bfe180d61164df11fc8a4122881061081664f5ffa7d28 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-33-ack.json | 479 | 5e8dfd7c8296b39d22f21f7d9450dd9d3b7505407c8384160ca48b237d2fdaeb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-33.json | 1121 | 01323d9220f0164870c6db1c3c8877798f7dbdf205805bc939a359629157170a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-34-ack.json | 479 | 8330a746a6ced0d93f002da1a1e9a1d9cc72191bbe83b2dcbdf57a431b5bf363 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-34.json | 1121 | 670546f66248ee88236d5143350c33227cd37b53e4f64e0610129134540b9991 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-35-ack.json | 479 | a6d77044c77aeb0a26c82274157ddf78c6a0569a2a064caa9999917fd613d24f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-35.json | 1147 | 85f2a9384344d64a5be2e5d933891620986fbf99d365f6c7b1cd7af9a5a1f906 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-36-ack.json | 479 | abf856c6cd97119472ebec9de19eddde3ec269246939ff25521a6493112a7d1d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-36.json | 1154 | 4b6ae7207194d59418a6c1e876ee1572ad9f2f1e73026fff138a2e10228ac9b2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-37-ack.json | 479 | be635671da64c4e1f63b2ea67a6d7070be540cc1307d339fcb4e9e76237984cc |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-37.json | 1161 | 3aab9067741e3061f26c98638ded0f01e9bee60777018793b8772632ac932fc2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-38-ack.json | 479 | 266c9dbebcf68ab2210a328b57eea3afa7392ccb6fcdb0b983bab43a663f203b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-38.json | 1121 | 649b0ff3b9bbab0408ce62912ccf118978f3075ba19e817270b09956b9bff2dd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-39-ack.json | 479 | 255c708907986e7b05f7d506f56c9c8f82c73b1e08b349b18e166299fb73f485 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-39.json | 1121 | d028cfff8df18531850f0f1abf85f606d83c25077292d6175ecc37d48e36bfd0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-40-ack.json | 479 | eeeb33cfce78c99b89393e34731f22e96524964ea40f18d456dc38d499bc5868 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-40.json | 1121 | 98aad0c584e76c2bd22c0749583b585307cb11cffbfd86a9729963e5be786dc4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-41-ack.json | 479 | 6671c3b46a5f3c7a62219a4100b8465f99ba015bb639999c6aa8fbd89ac2aee9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-41.json | 1121 | 938bdc67c7888c6e044c6214f176d740c9c87df7496c81f8cbde39783a1b1038 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-42-ack.json | 479 | da9afe738bff5eaf3929530eb63cb56f4343ee381f3a7f3f425e5a7aa289a947 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-heartbeat-42.json | 1121 | eb2ecf1cc1918e250fcb72c4e5bf78bc7e91c09aa139215c7ad69e842e124d88 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-lead-20261106-rec-01.json | 151 | 296839f3bcff18ec2d242862bbf40187016b29268e4c2779842e63fb887ecf02 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-lead-20261106-rec-02.json | 377 | c9f9a4213a91b9ce12ff328bd09a965cfdb355fe565ab35af22bcfdffa945ff3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-lead-20261106-rec-04.json | 3559 | 500e5bbfe26b8ff3a640c26e671b75f8d038c11d14397d08616b197799e309de |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-lead-20261106-rec-05.json | 1830 | be5a6d20fbb9de6048a6a6f482f8453db0d58cf6a1c8fb2a072e4e34dfff5149 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-lead-20261106-v3-01.json | 2080 | 8a5d7cf644b5dd7bd5de62162fbdd078124866165b3e597838524f8a44da5a83 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-lead-69.json | 2007 | d035401c6f1c4748b1770e6e7d431ea9d14582473ac3a4ab1ce9f7013f1b69d0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-main-20261106-01.json | 3280 | 4eaa217e850d17483da54feee8d64a40cda9aeda0de66f47d77fe21cd97f885a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-main-decision-2-3-02.json | 1606 | d23ccc0d374e49df429d8671eb9908da3ad7787e5c7d8086f9182261d868a140 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-01.json | 740 | ba4f915dc2326113fba08df40ebb46f535c4111dd36974ac41d187943c688f1c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-02.json | 3763 | 2a644a018f17972d446017d65614ae59b4bd8e98867667cce4d66057fd98b264 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-03.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-04.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-05.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-06.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-07.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-08.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-09.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-10.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-11.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-12.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-13.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-14.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-15.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-16.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-17.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-18.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-19.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-20.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-21.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-22.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-23.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-24.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-25.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261105-01.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261105-02.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261105-03.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261105-04.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261105-05.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261105-06.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261105-07.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261105-08.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261105-09.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261106-01.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nudge-168.json | 460 | 8fdd13b0be84a4f667e9a07990c4110f102f0f7e4b6756fadcdd6be9907e021b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nudge-170.json | 460 | 23f8d76eec4fc8124b67406dda5fe897ac6de3133c17e772ae5be4c43f121169 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-run-20261106-01.json | 6185 | 79a4a0606b5721f9b72a1c75fad9f9147ae02dae6ce3bc66e85ff8903421adac |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-run-20261106-02.json | 1304 | 7f889895260e1137fa0bb3944d3ffe0c169429eee95db4f366af1bcbc86c1155 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-run-20261106-03.json | 1746 | 94399b6c1227f5779195c4ef7665ffcb85761e47a55ae31d0d89efef0c2f6ea4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-run-20261106-04.json | 2660 | d08f22e73b93cf452fc3352c6d7c373904b4a64c0d91bfcf8662930c7e5c8267 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-run-20261106-05.json | 1319 | 6b9cfbcbc05c035775f989998edd3e015ffda296bef948d8490d65ada8569778 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-run-20261106-06.json | 1150 | dbdcdf5a4daaa6403289c8616f67153e8fcd4032c136dd874d9a96cc17243d9f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-run-20261106-07.json | 1673 | f8776fcff8311f53065c852d21d2e5aabc48b1ba0337c0b415d4f9bf6d8c5cb9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-run-20261106-08.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-run-20261106-09.json | 1865 | ee9dbfb686122784d3b40ac5a6255d3ee65a9f591091512b699caa3b0341798d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-run-20261106-10.json | 2726 | 50c8e03504bb8a0491bf00a0983f67554e3f34cdfe2136f787c251025b6e44c2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/close-docsrecheck-01.json | 319 | 8098de5e87fb665fd3d54c6f440e1b6690752c907cab949975d09511053a6716 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/close-docsrecheck2-01.json | 319 | 5e49cfc8e4060fb53302442c3b884f8fc1ead4e64c6ff6122b96b2a10dff6fae |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/close-impl2-01.json | 319 | 0edb633f1684d11b227ba8a22939f3c9a5ee49448757c4492620422970602d82 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/close-verify3-01.json | 319 | 95ef6e6c5e086c3a93703b7879a144b2de99df3805795712ff145606038773f0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/closeout-process-owner-01.json | 2438 | 4f50cbf81249e101e0c354d6ff30e645bf4fb040fbe0c52df6287d91d190e19e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/closeout-state-01.json | 2057 | d9637b3261b9bebc31e0a28a55eb72b144cdf4d906b9168ca9cb58a59f7f03af |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-closeout-records-01.txt | 707 | 5636cd99b4901594df2260beac18a50a3f5d81c5839d2fd261dc2c001d6089fa |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-decision-133A-134A-01.txt | 188 | 01dda0dc171d5a2eee7f25d9e041f5162c6a639d253c5a6b3b6977fdcefc5d26 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-decision-137A-138A-01.txt | 177 | 5a26c85b25d5b81de8b92f328c35b341b2718e0976fbb618ad3254c5415c0e58 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-f672813a-140A-01.txt | 199 | 171281d2cbf087c429fc5d650e668c5b94ec22574d98e57e8bbded5b5c9c7cb4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-fixed-head-20261106.txt | 201 | 8f8808ddbc50d5bb487144438443798077259ad01185308a15a69e47fca108ee |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-hud-gold-01.txt | 156 | c20317277d6df3682b490c4585c00d2f8eb67a2efec9b55dd71ddcdfb94d2475 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-merge-main-f1bcae3b-01.txt | 242 | 3a5ef46f0d4a6ab2db470d816f169c90d34c9a06234a5158ad3823c0398e1363 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-pr-c-cancel-01.txt | 152 | 9985d0d76453ed21520d298ed909152f88ba7839a7dc87fbeccc0d33d55274a0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-recording-executor-ready-01.txt | 171 | 848e5a287fa2e5f94502dc1e26d75d3773f7135f54689cef8cd9d0db78a56524 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-recording-executor-start-01.txt | 169 | 397728967e5920d89ccdf27855083a1513579dec0703eaa2b50599981dfb29ba |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-recording-v3-disposition-01.txt | 177 | 2ace6d4c4a782ab553e1f8d07fae9b71647d18eb025f2d4f35725adcaf75fafa |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-recording-v3-verify-01.txt | 193 | f93dd6e6f7b6efe046a3579cfd5306b880c87d1809f7c6aaae4f9eda9fab0d6e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-reentry-20261104.txt | 176 | fe2a33fc0169a529b820ff01d8e87e03a2118f2390f8e8236ac9b1bc88fb9bd5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-rehearsal-v2-settle-01.txt | 174 | 79f8241403588d5696ebc4dec16c54eac80636f19df8cdc04e7ce2009c0f4033 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-rehearsal-v2-verify-settle-01.txt | 196 | 660b29b0aebfc445b2d7c6bf9cc32a060301a9ee1f1d1903273c7ce92fa95b1f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-round3-record-01.txt | 508 | 2365bee992474d10c8a76ce5a04b22350691d26070449626fd971537196ad0d2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-round3-verdict-01.txt | 435 | 4e2c8ae9bac43c06fda997efbcd5b5bc772f4c16009100456bf4ee6bed538868 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-take-main-20261104.txt | 133 | 55aa8e94b7e9e49504a2bc4e4281f9dac32cf051dea558aed5fac3095dd92497 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-user-check-scope-01.txt | 204 | 6dd574df15b4871de927fe0d1b3acad44cf1cfbe0b82296e10628c01b34dc88e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-verify-launch-135A-01.txt | 188 | e25e31abaf284e56c331d335da059f9559a38f9570681bf29862d9c41bf3ffd1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/commit-msg-wrapup-01.txt | 154 | b26b8d0cedba67f98a7fe173603c2b0c5d3ed378c8634d8b22f409bb28fa63d4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/compare-u2b-vs-u3v2.json | 1449 | 240ba1a0ccbd56be0558a530852d4928119f0a8f7b93f6c18e4ec492a44c58bf |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/compute-r3-judgement-values-01.stdout.txt | 259 | 081fe1b8107e44e0e39d18794afeb098ea44c2d323efa60ca45ae4be74cc4238 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-goal-recording-order-v1-2cb8910d.txt | 4728 | e2092d477a4024c5836bc8606f1da0a7f73dfbe03ee5baddf572d65217a307aa |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_0c71d92c392b-window8-server.txt | 2517 | 188774a90518a792a185097847aa376a6b20f8ed1262347bc581a60c8cfdc5ec |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_25dbd7e547fa-v3-decision-request.txt | 6632 | 1fd6690083d43ca5dcde8d8717c195bfc3c3b3a51ef585fbac16d01d940da3d2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_3a1b8966627b-v3-plan-restart-between.txt | 3137 | 263a7d12fe5bfb8985473f59187805546dd11268e5ec6fab9132a0583a25a735 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_3d0ad23c6476-recording-tool-agent.txt | 1984 | 9e444e04d4989d53f4b502b9da52d2f5ed8c8fc69821e2fc8294493ac6fd9b95 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_50bf844d60ce-inventory24.txt | 1536 | f328cf24b29add79ac4c9f41527225ed92d1e80b550cee79bfffc974f2fd88f2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_9203c45e18e9-fixed-head-taken.txt | 2648 | 27ff80b471d795ffcf586241fb173fcc5e258056780e359eed6c09e2e87d7846 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_98b6fd98f397-rehearsal-hands-time.txt | 4847 | 5ce5e9e8a57fd5c260320a2485015fefaffe5badf9c7d16bb13e548582ccec0d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_a6e92219cbe1-window8-two-rounds.txt | 1882 | 0ba24237b46163a28f7e50f56522295af5a62d0417bd969f0db09bbd078b99e5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_b28231a86423-v3-plan-revised.txt | 4647 | 6bafeeff48beec45625519dab645170f3d01bd6cd56fc7928d0b8194010ee2e7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_b752a2465246-contract-v2-diff-card-v5.txt | 4005 | 3a439d2ce7ff3b2bf0f5459e6ff47d5c4abd4fa6af6270257a32a3f4b0725f85 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_c946082e5b5b-card-v6-draft.txt | 2961 | 654886e791dbdb95e91d7976b37dfffed4683d9272e95193320fa6f40c7df103 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_e23465a75a03-goal-commit-server-contract-v1.2.txt | 1741 | 015dc625b9c64b0ef23fe4b3a65afa8863c90ac8698adefa71d2161ab4e4c227 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_e6d433cf9c2c-dungeon-clear-viewof.txt | 1944 | f2b2c1f67eba467bb19794acb60de0b814a84ae9d90536258b303da9789ec40b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/core-msg_ebb4e51fb548-rehearsal-division.txt | 2596 | 52af5aa1db997d14c0c2c3a2277b758d76917c1edafc439e43b1c06b365b17ba |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/detach-fixed-head-01.txt | 84 | b4068c79a88724f840599dbec7cda540b193ae8875c7b7a3ebe32e3ecad9a2c4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/dotnet-runs-recent-01.json | 1073 | ae59563dcfbed3b1eb38befe7658965d50731861868650208ddf507a0947e083 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-msg_34ce5fff8a95-r4-2-end.txt | 2381 | ab9b01ede070f450a2a1ab1147e55f1f56289fb65f2c13d62b1d4c7c24326d81 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-msg_466d82ce6842-notice-selftest.txt | 461 | ddc7af7c046c1da3ab1620d06febf3afcad736356b0df4910ce6979ae1b75253 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-msg_7030fe44e9cd-question-late-parse.txt | 1265 | 20c3efc31ebf9ed3f051b62f7ca007a37dac7290a049c3040eb7e4d18513bf67 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-msg_8dc1d362a630-question-c11.txt | 908 | b7076b42a28d345f47a687bd2f762e7231628c84c3e619c40fd9e891113bad2d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-msg_b3c26e551197-worker-done.txt | 916 | 11cb1f971ddd5f3911b55f4db920e42c5f33fd500ba15d62f65604ba2a12efb8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-msg_de32dbd79892-r4-1-end.txt | 2374 | 241f941ddf891d5781da762ccb4bedce45fdb5c2bab1898e26e9e99215f05ddb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-msg_ea656aee0735-wait-selftest-start.txt | 171 | 38887b916327ad70e4496c01f97d5911df5dfe83108cb85bfe5d9f64aa439be2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-msg_efc9e1f6aa33-question-c12.txt | 1188 | f86c4bd6d50385e396a9e8a79fd20e8bca23c8c844307d7f27620dbc4ce40289 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-msg_fd3e7497b01a-question-layout02.txt | 1103 | 2c9c0e745b44a9101b58703e162a1687d5fb57f9ce223792ce888da4651b0e62 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-r3-msg_07ae9826d9b6-ready.txt | 2794 | 544a6804550b9a27d0d9abe187de5874a19d968a29c84c4763b33a3fca13a8f4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-r3-msg_3b362aaec79d-notice-selftest.txt | 743 | feb102de6d6234f3908f7b49a4927629e981ea20d97e9cb58c1e539eaba88dad |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-r3-msg_871c7671385b-worker-done.txt | 1205 | 2acaa47afff4f5416e36cea6228326f6d7562850f4ee98db2b596f2c3158cade |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-r3-msg_9714bb2b5446-round3-end.txt | 2834 | 030ed0ecbbb07aed6ac2f2b2bb5a5d27e71e259ccd1dad7d5514c2844b346886 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-r3-msg_d6307173d107-wait-selftest-start.txt | 394 | 6b258222ea8bc5281859b2a210da4934d6b7718910a0432fbb612729287f337f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-v1-msg_0fcc8db42348-worker-done.txt | 702 | 0cb94a4d6355f987fccf54598429a487dc5682ae9d77d80404123ae16cdc417b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-v2-msg_11516e5c599b-ask-select-regex.txt | 1661 | 3c0981017e63b0d726edf6e45ee3376b49537e0151b1158155695c01846be8da |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-v2-msg_59f9ab1a020c-r4-end-clients-closed.txt | 2101 | 63ec439b34db3e325cc963b822d15c223dd9a6592b45a9968930cd02c533a682 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-v2-msg_627d29cd726d-worker-done.txt | 730 | 9909e1c782a0ecff7124f12cfd2a4496e779e7c2715616398d33adfeb5353c34 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-v2-msg_6a8a2b55ed50-ask-recovery.txt | 2065 | f4d3c601ca8b345890b641bc344699dadca5a357a664b0a710013e11f93969db |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-v2-msg_6eafe28e0042-r4-1-end-resent.txt | 1890 | 88789164163a33549a7530b97f79e94dab8ca5bdd663b56a12d4fd8bdfdaf40a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-v2-msg_78de84cbcd76-ready.txt | 1170 | 1646ce8488476906b270ea6af0528c27407de318ea5ce78557b532ab2c2407ae |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-v2-msg_9ac46ec73f78-r4-anomaly.txt | 1074 | 5b7e45e74572752d0fe395370c85546e721c1e0e9a901d3e27fc5646b4bb4102 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-v2-msg_b750def13ab4-r4-2-start.txt | 611 | 8a444b999c27d67fe7b1808ac3f4dae8a80e3e3cb5714c89e1ba1a13ad9449fa |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/executor-v2-msg_bd871d55397f-r7-end.txt | 1251 | 86717fcf0cb3593a1b6136b21fbd529f17a85ca25205dbc879f74e10b5b8a71f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/extract-recording-v3-av-sync-01.stdout.txt | 338 | ac78e2168522e81ad5f386829d9b67a61df6817041fb5ada56f201604776600e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/extract-recording-v3-av-sync-02.stdout.txt | 94 | bb0291df37c8656ced8bd81e1572873af1fe6e62b783385a2a1dc706ac84e1ff |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/extract-recording-v3-errata-values-01.stdout.txt | 755 | 37a57690976057b07fb65787454f5ae23b1e0dc8150838da499fbe577bc4543d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/extract-recording-v3-facts-01.stdout.txt | 771 | bd07ca8d2c1d9d8ebf2ba704b1d1b0157f303530ad3664b3392ea0e753d5a19a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/fetch-20261104-01.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ffmpeg-probe-02.txt | 2269 | 4fd883b6e43fb48df936456a12edffbd3845c5cf9afd27049ec8a63da5846b4e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/ffmpeg-probe-03.txt | 4153 | 928259c9f322004db0718ebc74ce4cf4d28106d6719962f8d1423d7813043228 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-100-ack.json | 479 | fc806dc8202546d2ccc604ab36610348fdc6461e92630784e26f2d2fee7e1b74 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-100.json | 1121 | 55869acc0d62f55c8680eb2a3143c10b60bccc3a4e938d528ff26794922003ad |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-101-ack.json | 479 | f1e0c9b4f651cee92ef00ee4dea2b3b0d3badcc99e1cdcc754302a176aa706e5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-101.json | 1121 | fa0e134920d08287aa4192e9a0d5df63d01b9c7c17a8060c39ec4210a0ea7cd6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-102-ack.json | 479 | be7ffb500a37a4c02a5d5ee049500f061d9f0e01984cfb8d113aa5288d1c549e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-102.json | 1121 | e021353696991e503da4ea6bd35a9d3beb04a218edf89777a14e22761a9e2b96 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-106-ack.json | 479 | fbeb3516093cc95ad31c3b6fe7fb1251484baa0668115e14b235d9ecd2d90c08 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-106.json | 1119 | 09c0a63575ed3e78902243e6f4c22761cbc918813dce7fce416230f5df7777d3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-107-ack.json | 479 | 146a9106863e86eb5584c0e2cd04eb7deaceed912c97511489b01f0c08449be0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-107.json | 1119 | a9de44191a642562091f35d103d66cc8eff89cebac62a95113d97baf4c9e60ff |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-110.json | 460 | c125cb6e89127f9fbb6bb27637c66ee6018010faeba901ec8d6dd24ed395030c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-81-ack.json | 479 | b0169cf92222973f0e3b5b7842f81f4a47b8a2437c3707a6bd072a70e2fc918c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-81.json | 1713 | 06171d65da5704d83e9bc8030745c5e5ec0fb70c9724b210e62fdd2e5193c42b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-84-ack.json | 479 | f044802bfc3d9e574554f20ddc07eec11e8b54164856324185ea2c6b13de5b3d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-84.json | 1113 | 1b927f276470ce99752da3e6cb99d824ee97c9d363e2feb94dbef7609b97745c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-85-ack.json | 479 | 50ca2abf22172dacdec74243ded9871469b6df0130d04406d48c66e165e59bd7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-85.json | 1113 | 1d376a7610c29665b65f27e1a896f8927878f05e49d9f991ae73e0531c09a215 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-89-ack.json | 479 | ebf99fa6d74188d1265ca939afaf2ca76982f368519e1c0183ab8797ddc9c1f7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-89.json | 1118 | 4b85efd23c5916d11226e48c2214fb168d15d9ea83c8cb24752585f9ef5380a6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-90-ack.json | 479 | 9942ce1ada2477dfd339b4c89f2fed7285f88f73b1a5d76a4cf4970b5ef481d8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-90.json | 1118 | 40a2fbb89037d17788236ca780cf47dc063c811d4648e930d35f55765607feef |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-91-ack.json | 479 | bb120ce046ac307ec171bf65c9101fa171ed4a0b4a76e287ece0ca65bd086551 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-91.json | 1118 | 6b9e2beea6b2f1ee578b2cbc371814db157f94ceb93fbacd7a14928824cab80b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-92-ack.json | 479 | 1a0584a478cf98dc0d203f5a87ce0ca2f75cc0741ca62069acf67d7480c587f6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-92.json | 1118 | d744f97c7053f5ea2f000a684ab11e532a5e8332b1738a061426a2b8b7bedd5f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-93-ack.json | 479 | a8d59f49d17f4d6607a217d235fb5934ab6d7245e6f69f2af3000008af25cf7b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-93.json | 1115 | 3c6606c961a25c216c7dc88d8b6ecf021d9638ab69f7e41f8179e3a6081dd2c5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-96-ack.json | 479 | 1f2d68cda5216c3fc84f4b0ed32bc024093296391c77d0ac938b3ae7baecf58a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-96.json | 1125 | 8d670ddaa34bc0de41f835b1fe535199861ced3aeb9a14575d3eede467bc055a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-97-ack.json | 479 | e1d8feef735fcfd2f393b5aba559da20a532f8cb437ae9d30559e9ad08cd80de |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-97.json | 1121 | 0e94fb04e4f9d882b3c6c4c532a993afcb23d1a448f03a9619bdc5044e8f2311 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-98-ack.json | 479 | 50db2910efd2c208455f941fee8a000376ec4635f801079d3d7125a32c477bb2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-98.json | 1121 | bab31198ad52a8633aa03f388785c8f166605fd94be8f8b325813f372e8704e3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-99-ack.json | 479 | d8870318bec842eb3c658eb793818d5ce823bfd986dcd81d7128b46341364237 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-99.json | 1121 | 438311efdbf0376fd30e089e230d24b4b92f50f9c13f94f902744c3f74c5de35 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-ack-37.json | 479 | 352df1a7840224ca1aded95e00e665aa091b134137edd58dc27053d23073f289 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-ack-38.json | 479 | a84b1a8276cc8c57051b889ee7e5f0eae16e3bc4422252677d591e48eb4b0bb5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-ack-41.json | 479 | 89358792acf0c2e0d9bdb35489e71160aa544a60df2dbaef2daed2a1974dd529 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-ack-43.json | 479 | 870791ca3d97ac3086792e61c4e1b1050503a20a286b7dd2c062b1bd43a192e2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-ack-44.json | 479 | 2d606a51fc5d7780fdf69cbd7e552e7fc84b7ee803c86e32783d6ac7365b1f7c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-ack-45.json | 479 | b6d4bcf9ce308cb8ea0cdaca9482198dc24f5f375965ecad9ab664b24ed95c4b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-ack-48.json | 2238 | 8921bd918654887ef4f19e0d6754589a84c8f7c99f97571b8cb0773cfe7d31b9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-ack-50.json | 479 | 78aa8c0f4167d86df8b31bc6dd0dff111d04b610cf19692f3f391a850981602a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-ack-58.json | 479 | 062116b19c0e73ed3edb00f13b914413e626eefe571a0eb0732bc4dffba6f7f4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-ack-60.json | 479 | 8e11c40e129614f5816671eae8983cd39792b7312ab4bf97d782d9a00c4cf273 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-ack-63.json | 3820 | e55d0bc5e28c8db8e4d7f5179dd2f843b2f38a7d4914be9822bafe8f74ffe9e0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-check-37.json | 1119 | fb28ba42551cc83e473cc4589ee12674d7c5093ed05d43f1cbbc629e274609e7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-check-38.json | 1118 | 0e8ed22abfe0cb45c53a7d5b248ddbbbd4f984eb5da9cac99a77ef87b14a6140 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-check-41.json | 1115 | ede2bcc26204432f83e40641ad6d4f26f428eca35e3f2f39d169c6c9c7f8e670 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-check-43.json | 1119 | 87aeb9075361d7fa56db752346b8c3e896fb63c501568ba3a4cca5c59e3dab17 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-check-44.json | 1118 | 12fc5345fa0c4cf98fbf6feea37434b3cc7b8a89a3379b397183367610687c52 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-check-45.json | 1115 | 858a5b4b8c18726bb2d88d1d0a7a3d2a663b3a17633c5fa16a6e61dd975568c0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-check-48.json | 1115 | 96770a18e587090998390eb1641d53e62b8d601e5dc78a024551eb4e50b5807e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-check-50.json | 1115 | 6991c4c911f0dd2c58bb4c7338a072b245b3a7ca7c57c07c4022e8fda33a4a10 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-check-58.json | 1115 | 730b4424da3543076a474ab31af6e01d592d10393066a89a8b63eae93ef259fb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-check-60.json | 1722 | ebaa0d59b0e61c9ee15c1fa1d8a3a402da20e0eb0758a3a6ea02d36953bad7bb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-check-63.json | 1119 | ade5a4971359ee5aaa1cf5894634cfa0f3abc943eafcfba3b8fa7b296c21726c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-66-01-ack.json | 479 | 0656cdba79987425848ee0f9c0efe3a010bc54cdb3f08fc3fec0a67e36d302c8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-66-01.json | 1101 | af6cd93c1994387b3f230e985deab7e5a3c21d13ebf5034126a52dc6e63163ec |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-66-02-ack.json | 479 | d670a7aff3bfe75def692ce78741b2b68035ca32af7ee8f1d58e7a8335ff8f06 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-66-02.json | 1101 | 7039b2cbcccf00d37967577e1e486c4b9493a8cd40ee8871e152f160ea0e07dc |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-68-01-ack.json | 479 | 2a22c5201703e39d5d3c2153db20746ee577e83af6f6c0e38d6baacda8d4fbde |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-68-01.json | 1099 | 40086af180fcecd27777905fe7a5a2f0764146b54d7e99a538cdb387217329fb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-68-02-ack.json | 479 | 6f807b0dc0a243a413877c5f634c2ee3e4ff8e23e0905985070533139c4760c9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-68-02.json | 1099 | 5eb76f01b0df5e80579abd2111c36d3cd41f18032f0042d43cbabd0487356268 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-70-01-ack.json | 479 | fa666a46efdd085c65f507c0c1146c7b8f6e305f07a9a2ac1d88fa0bb88aeae4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-70-01.json | 1099 | b601c9721dd6a10b2506d929da8c1690bc58201d95508b065dfb5b6aa1f46a84 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-70-02-ack.json | 479 | bc5dd152fd9d244c4f328ec159d0c03aa566c489ed59ae672a839da94492a37f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-70-02.json | 1121 | e89c619bb16d7d0043145ba7748c4454c8868f7be6a21aac4e800790068870b6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-71-01-ack.json | 479 | ea15c0f55b0ddb738a7c76d64cc5f9fcb03f01f90d9db63f87370a4022721c3f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-71-01.json | 1101 | 59cd6e6a93d7517783fc7c97b576cec40fb5fea0b20fce11d145642aa2a6f6aa |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-71-02-ack.json | 479 | 4921e98ad71af4622c9b38e0b617aa73ca7b286c3335376ad6c0fa6a83e5fc04 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-71-02.json | 1121 | 0167b48ee58e263c7f15ef5afe48c3609b464a6279d8b4a412828e07c2529d82 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-78-01-ack.json | 479 | 918bbaf73c055ee61d87c064795d0502721a6d6a9bf9180e995e509fcc573cd1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/hb-lead-78-01.json | 1101 | 8833a5b9f361673825b59d77fa2d9a567ec693a95564ce7146d207d5aff1a8d6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/impl-close-01.json | 319 | 1e29dcc3d618820452ab7eb4e697ee00c06e87214fcebdb736808ac7c41f88eb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/impl-release-01.json | 413 | b719cdc2a2921d25c94dcf800db53c77c9b2c2fd3e01715f48b06695a1bba8c1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/impl-worker-done-msg_7d2b8008e7d9.json | 2355 | 60859a8e230d0e18b2e52abe7ea631d210caa1c1a4d6191001aa8184bfd13e6a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-after-resume-01.json | 56952 | 1bb87469e4b4b5b19a76382d0061171b7070d5ce8f7c6316b422ca445315aa11 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-07.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-08.json | 1480 | 041578a44778aef5c21396c433c7f72eba6895697c15596f1b3a81f826c312ed |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-09.json | 1326 | 6a5ab967ceaa7f9f4e1f6e69db168e20c84d7d38246d37a5bd5044b1c3617083 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-10.json | 2061 | db6a5f93803d0eea0aed002cad7d806301ca0ad660a77bf78d2c122435266bfd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-11.json | 950 | e9c89b28ca98c2a17df38af798f38789dda6bac5d22a0e50ea3fff011afb4673 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-12.json | 1054 | 8c24ea6aadf1ada90640c0ceac2645546f0fbaf75bb98d6f66c73c6d4171b889 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-13.json | 1870 | 0074fda276e872e48be10585d58e3910e996289194a841ca903ba9cab63ea770 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-14.json | 4095 | a78e84f20738d81435d32da0ef0e5c985d765f64e274103e569e2896ae52395f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-15.json | 3219 | 90fcbb0b98f4920b215f533ed94ae42f6e5287e33a30b419098f1b6b0d01d1e9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-16.json | 4743 | b51c83139f926d60962613d01bc32087aad532b313762d17caa9fc7ed00233f8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-17.json | 1175 | 50987d8a4e0262276bd32157efc3073aeb0fda8ec5df1a41b5ec248a01fded33 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-18.json | 151 | 296839f3bcff18ec2d242862bbf40187016b29268e4c2779842e63fb887ecf02 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-19.json | 151 | 296839f3bcff18ec2d242862bbf40187016b29268e4c2779842e63fb887ecf02 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-20.json | 839 | 7f7263ae48d6eec11acbe71ec334f725f4ab4f6e6a0c1afb7f2a379f9b7ed3d6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-21.json | 151 | 296839f3bcff18ec2d242862bbf40187016b29268e4c2779842e63fb887ecf02 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-22.json | 1312 | 9319ced2df9202c6e5b54cd5dc33d0a0ac3792e48c027af6537fae1ffbd215be |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-23.json | 2161 | 92cad9ae8ec9f34d962108e0262690a38756bbf49fb3b9182359acf94c511501 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-24-term.json | 151 | 296839f3bcff18ec2d242862bbf40187016b29268e4c2779842e63fb887ecf02 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-24.json | 151 | 296839f3bcff18ec2d242862bbf40187016b29268e4c2779842e63fb887ecf02 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-25.json | 888 | 7d945df056af4315fb5c8392db47c633035ef02df9dedd577937998e1dbec622 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-26.json | 151 | 296839f3bcff18ec2d242862bbf40187016b29268e4c2779842e63fb887ecf02 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-27.json | 6704 | a2aa9978d037dde2e7e24a5c5a5ea73723329cb83b66718880dd009b62364246 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-28.json | 1355 | 3fa5b2fd8e6d6d0923c30009f8dec24c8b65d919ff1fc85a6ebdc5ab57d914f9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-29.json | 5228 | 072d0aa817ec5199e117510ecea0bdb803048e2bc7a5864e6e7135a1757ec4c8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-check-20261106-30.json | 37 | 5484c39bc45116656e60d2d5d3c34ff556f0cdb82ac0d61c2cd2b9f0047e74a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/inbox-full-executor-dispatch-20261106-01.json | 1141 | 959d39055c9f6d3a24946d10cf460991655d267e3baa9514db721cecc044c6b4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/liveness-docsrecheck-01.json | 414 | 73f3ce07863a72a021eaab71fdf7cfbb1b5b4f3c13787d52cc3add29f7cc630e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/liveness-docsrecheck2-01.json | 243 | 9421b5a72371e95f2f961681de2d0fd33b2eafdcb4463c27644350a98a5f3fcf |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/liveness-verify3-01.json | 5688 | 1c43d4266ed4b698699be48b3a16260b039f06c0404c437176f31f7e2e275c79 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-5bfda911-to-b8631ccb-files.txt | 837 | f88fa53f4ea0ed6dd635c2c748dcc679fbc5485e94313e122f2da786080abcb2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-inbox-view-20261106-01.json | 3365 | 0c52f62ca2470051f5d44519dbc44cb05cb07878a69ed13c74b97bdbda2f236f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_1c83cff0bb95-astra-xhigh-routing.txt | 1547 | 248b251c07da2090ccf862beace20ff06989de416af1b1fd6778e6ceb16a6f65 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_25e147f44c7f-contract-v2-A.txt | 1795 | 1bd773b05c100010d00c9eab7898119ca1f131e1041174c228ab044243ae05ee |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_297808ae9fe2-auto-recording.txt | 2143 | a755f1fe3f37cc098d737bd8d633b441a79fc2c3b362cf31056dec4bb5e06211 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_29d5cb57b68d-wrap-up.txt | 1886 | 88bdfd3d9b9fd32a714125c4c20c31472c3c82f1a1e2773b082127a08894c756 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_374115bbd7af-worker-routing.txt | 2419 | b3fd6d5fcd0f7867a69c761c2e8e0cef47f8ebae569412892e9a66d9a7f2e20b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_3f5de27030db-round3-server-stopped.txt | 1632 | ae2005f82e0f44551937a91f28730c8cf3b57299f9ef3dc6f3bd9fc57fdb80f8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_60747f335b97-decision-104.txt | 833 | 5496e13fbdc4229536d33f8912282913edb12d0b4e17bebf41a3056090f3d0b9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_762fb0fc39d4-r4-recovery-order.txt | 1289 | c3bd2e12caf0df198bbd725164458c23f02a95bcce2fe2c7b95d9bb4a0515955 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_865bbc326329-window-order-after-u3.txt | 613 | 5ac994acfd2ef1f5b60764121371207710651f7108f9e217144e0ea529d72ec6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_8ddb358897ec-u3-window.txt | 1021 | 6787020f37852c2a801c4ef62cffcdcf0e36909a92f1697b16a672ebfe7644a0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_a12e08143d11-decisions-100-102.txt | 1657 | 09354737b26f54e8be93ea09be9b8b4485ee85f6b9ed4e080d22ecf56af237db |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_a4ef37820229-restart-up.txt | 1120 | b510b94cfe9ea81791298e7be574137426ce105b57388179bc9a96a8e8dbb943 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_b50c04ca0c08-build-accept-rehearsal-executor.txt | 1743 | ccbc4f405d5ce51199b3aff02d8724f78480b33a3c6fbedddac611fa32faec78 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_bee95adac0e1-window-extend.txt | 1027 | 43e735d089d98ad097bfbb3738e7d9a8b0fe244fcd6882a9bff35beb9cc2c4b7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_c2f83365de06-window-order.txt | 780 | 985138f2a9ba657cd13e8b61d1b3aa260621bffb11591fe58cdaeb9f5a2c29cf |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_c83b0d4ff3ae-round3-server-up.txt | 1115 | 7b6b842f1b8dd5ef74089a543f070db3081c4cf472ba79edfd06ac720259f8f0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_cc7def042608-wrap-up-session-id.txt | 314 | 0854f5deccb5729a0f9b12c4fdb56dc5ee0f959badca591e2346287a62b99b2f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_cf050c154ac8.txt | 3353 | 69dba65f46afdc30c280eb1752d516181ab3a25dcb541e1ca7c3b24be09f802c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_cf71f4c479f7-decision-131C-split-recording.txt | 1914 | e5eeb3c4701a63f8e156c021a8d030589228bc314e32721fdbec41548e9f14ab |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_d0012352013c-build-input.txt | 832 | eb9d4604d5f96dffa9c392a1d69781daf28bc7f8b51928b5139db4b7565e0c95 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_d783ac17eb63-pr291-merged-97a.txt | 1695 | c974e128e84b6da222615c9035dbd12a15476acf9e2f7b5c9e9b0b137d00bd92 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_dfce8cb2c0d2-decision-129A-130A-launch.txt | 1204 | 45e2bbdde03278a819a9b25f56c6f465874ab6501937a762a8970b9608a6d708 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_e8dd9cb571fd-v12-approved-build-window.txt | 1744 | 9460d3683a7dc7e23d73732ce02e7e24209e9c8db6613bc166b85989bf155546 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_eb2a87d11597-r6-relay-r7-order.txt | 866 | bbd8ea6f2512c44903c42174689191365281b71a4b026be6b1e9e9bcd113bf7d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_ee36970aad0b-d1-test-adopt.txt | 1328 | 3656063159a136dc312e8b01fce1d710424d866c38925913733f460b40599bfc |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_f09146551027-r3-listening.txt | 778 | fd70e7863a4aab809ec67d090b67f687841c261a72227db2a4ddd8ed4b18a07e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_f4585a655ece-server-up.txt | 801 | 2de83fb9770a12d70ae8d1512237a07e62c66cf5b2eef9119ed6e14b01f6671b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_fc2a4af54c5b-u3-order.txt | 671 | 648bd15c53dd3e93c9a3f2da1eee1b81dc8217c96af2d637745177b64f1fe413 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/make-recording-r3-target-manifest-01.stdout.txt | 54 | d3da11a6679717fbe3ae7514d461e1ab1df8275c3e7072e7b6cd075ece7ea3a2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/make-recording-v3-create-dialog-frames-01.receipt.json | 1654 | 790597a329bdb36bfa32fcd7931d4efdb7f42685ee0efd95e9296ded8d618739 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/make-recording-v3-create-dialog-frames-01.stdout.txt | 154 | 110d984979282457b73ec65b72eb7f2a746a3498564f37a738fb232b3031f699 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/make-recording-v3-create-dialog-frames-02.receipt.json | 685 | a0aa81a86d55b833d6c6585b8d0c8d098569c6854ed363b64112360a291d11e0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/make-recording-v3-create-dialog-frames-02.stdout.txt | 8 | 176f7874f1f74fb1ff41312ec9f4440caba6b19547b1070bdaa55d7ee33ed1e9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/make-recording-v3-create-window-sheets-01.receipt.json | 1778 | 4a0c15c1964bd61b648f403b23e2d476033b347f1107b3f36c216db3f39e980f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/make-recording-v3-create-window-sheets-01.stdout.txt | 184 | 49cad533ba8984e4d78f51cffb0af15ec3aa41bfe8a3400ceb260b3778ace9fc |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/make-recording-v3-stills-sheets-01.receipt.json | 1433 | e89769b435a234a28931683a3a366e45b2cc26d5f4ffe86151499a4b5141a2e2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/make-recording-v3-stills-sheets-01.stdout.txt | 1025 | 794bd37ba1b852dcc95b4e97a4723c6f232f9e9985c65d3dc547174779fa13d9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/make-recording-v3-target-manifest-01.stdout.txt | 56 | 048082b8758ece78a75efa8ab0849aaa5f42903f303c344293ca4d95e9ffda50 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/make-recording-v3-verify-redacted-inputs-01.stdout.txt | 244 | 56c5fae63bfae722f1431e941544a99d1fe43da59df74e0b93e43bf7ee23a4e2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/mask-server-preview-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/mask-server-preview-01.stdout.txt | 1585 | d12c4864f8f07c59aca79e6ee2fb05c89cab9772a5d773c890876fff0c8e0743 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/mask-server-preview-02.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/mask-server-preview-02.stdout.txt | 366 | f6287775b8b2068ef3f4376374c7c724e26e0880852dda2ff12b43d6e604abf3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/merge-tree-6f4f721a-vs-main.txt | 41 | 5c3cee38a0bc5691953c506c0abda1254fb6ea3b22f6bb3a1b3b39cb7ab1db24 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/merge-tree-main-b8631ccb-252a59e1.txt | 41 | a1adb51d1c5a2b82df1da57c5a0b039ed64e8c2f2fb8b31fad9a8e43a0461b61 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/merge-tree-main-b8631ccb-fb3d65b1.txt | 41 | 97617924c64ff0ee10e66999f9fa1a0ecd78a7e9f00a29a3a0bfdef4806a6cf9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/msg-msg_dc6de0f78a62.txt | 3406 | 994dd81243e0006db05e79ac8e55da9b621961a36c2fb09c7e9cce5261871654 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/open-prs-20261104-01.json | 244 | 8fd05f4a8e6551ef1fd980b808fcfc4fc4afee1f086060c4a3256385a1a76d9b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/open-prs-20261105-01.json | 244 | 8fd05f4a8e6551ef1fd980b808fcfc4fc4afee1f086060c4a3256385a1a76d9b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/orchestration-guide-20261106-01.txt | 5744 | a583db1efe5513caf5aca62972b6eba953d010b1ad73b4014c82a69a48182cee |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/orchestration-guide-full-resume.txt | 47515 | c6fbec808275b3c0d0e96177166de77e734c6342b8b20375feafd19928645c94 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-100.json | 871 | 69180b5bfe376198caede85f2728cdc7707ac5746d7c7c857183b93f9de3e13d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-101.json | 871 | 764735b61fc7a5189e7dda7051d3008e35461e171f0a2b7d6eafbbfdc094e3eb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-102.json | 871 | 40284aa45bdbc97ec4f60472b410ed5465f191f69d1788ccecfaf24954386ed1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-106.json | 869 | 337709fcfc8d9d4e4fa2164084b95e0f7419824f9ca9cbc3a65ee3f684058ee5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-107.json | 869 | d9276c6087becbbf7756ee35f143e137efe930768f0ad92296aa78147d52fd41 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-81.json | 1463 | d107791b17d1bc5011b2b37bc6129c01a00bd5f6dc4ee4cda3243dddbf81ebd2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-84.json | 863 | 3a3a3aea51c5e4d22f3a8c96343d42a13cb9f15da76fc12941ca2d285866729d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-85.json | 863 | 724927bdf07a83b6e151f7066f925c9b25ad1e5bb45360998e4e955a9696c8b6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-89.json | 868 | db78faecb031d783a594b2a76f5825f52f94f81796595e3238ee9956f1d9ba00 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-90.json | 868 | 36cfd7d232462d53d767b4908149abf774b661947308cc4785bc5a3436e08f4e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-91.json | 868 | 293981964e68a28d557e6aaf38545314a81c7ee37a73b48f13c7ede7d902514f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-92.json | 868 | 94e033d170432e4cd4732e4e8adbe33ca5263b327884576dba0b66a9b4a993b9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-93.json | 865 | 407a2dc7f6a28ab6f2d3e6f13f68c1a58fd015bac01ca2abd1c94e4d13006ed0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-96.json | 875 | eeec943e39c276b553a4c0b46d6dbe725f0a18ef90edd108d498a5acdf6a8a98 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-97.json | 871 | 15261b3c5587e79099e2c73f030d815d399367bb64aef88dc009942d24b7f1dd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-98.json | 871 | 54592404fdb83cb888623ef3cc7ef51a86f462301306629c2ea7ca23c84d527b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-99.json | 871 | 27ca0414f0b4a672ac715158d0592ce331cd346fe78a99888ef583ae54e73f23 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-lead-66-01.json | 851 | c90d1164e89e3299f6f17a5bfaa451f4a2e60ceadbf268d1a83ea1e855aa02dd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-lead-66-02.json | 851 | 9a2324f3e7d260e59112571326395d7d7e1213590b3a2fe6c788869eb7a99883 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-lead-68-01.json | 849 | 266c49c095eb5abb50ff917b491b74633431bf404d7a830a5e10a0040b30a19a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-lead-68-02.json | 849 | 372777702b400935f58c66ab58565edcdad0195abb06919f1d0ba957d2d8b146 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-lead-69-01.json | 1758 | a697a8112efa7a66ca506644e67fe82cd3c8e8b83e4e69da3b109a37314b72ca |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-lead-70-01.json | 849 | a7e11359b780c1fcd061bdf995136e7d0a845831fe27e6532a0b57f0e19d4d43 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-lead-70-02.json | 871 | ae6f0c82c80ee17eb46747394ce26f156bab24a8d04dceea1f0e7085864ec34e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-lead-71-01.json | 851 | 9bc3434a8cf46b4fb3f97aaab9cd9a4dd77ce59fe8e667c6fc442a0cf522d9f2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-lead-71-02.json | 871 | 81910b7b36b6785384af18d684f15226971b380086c02f9ab986eb4d86c6617f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/peek-lead-78-01.json | 851 | 10a47fd4ddad6ea130ff834d894562d64a5402574e91949fa21c05207f08baf0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-create-01.txt | 54 | c6ce8ee3c26e40dc53872d13917d5b34fd1aa98f925a7b44a14400e0dddaeec8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-impl-contract-v1-sha-01.txt | 161 | 69e6fa1567ee10c1763cdead715115803bbbe3f37a7b4e815a9877a455db008f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-impl-inputs-v1.txt | 1990 | 0ad04f0ce9238fb2589d516ff4ccbee0fcfdd7ef403cfd0b47a7db1b1614cd2d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-impl-pane-close-01.json | 319 | cb6e038a31e24d8fc8c55ab3448105bc5aacecaa5069878e9b98c39d65bcf72a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-impl-split-01.json | 376 | 414c3bf160aae89c2d5d838bf277bd2b31a41470e13ff4a8ac58345c0300ed22 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-impl-wait-idle-01.json | 345 | 9b17299edd567b1f026d762d5241be7607f151cde9391aac63df46b139c96d43 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-impl-worker-release-01.json | 413 | 33aa11fca12b454640c9fd20bc7447162c629e5b0e7594d96a864d3db4452ee5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-impl-worker-start-01.json | 2251 | b486c2e4956f4a177d1eab43893fe444b8cfebd917b80a5db729cc04bca8e05e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-contract-v1-sha-01.txt | 328 | ba57a640f6ddc77e0de485f3b5a234ba00e805cb12eb5d479f0fab73d4141dcb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-contract-v2-sha-01.txt | 160 | 5f110dbff1ee2adb4d27f4a543ac3bcdf255104fbf94268b72c09479d891f3d1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-inputs-v1.txt | 1300 | f8340bf030bb7f43342128433cf7200b4d128d4eb918955d63052e0046bfd6b1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-inputs-v2.txt | 1520 | baadf8d41c674f8157c2eb9dffcad52be0669cf43a759f078e69014ab6bc32a0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-pane-close-01.json | 319 | a5d372c971806c7ea985ba3adfde78e60598ff2b33fa9fc4af153411afc283d7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-path-check-01.txt | 834 | d3d0f4ae29af14817e31da7aa6ad0ccf8c7cfc3626794f79a58ee84bbca09aa5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-split-01.clock.txt | 21 | 4f6eb9de659ac76bb2315db2c13e1d010d0c8d4a445c953c4addbd6077acbdae |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-split-01.json | 377 | 7f14d4b35598906e440e2cdcd6814b4001c2d3d93bf1a80b416b802430e82d38 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-split-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-wait-idle-01.json | 345 | ce5f73c5f4d7b1d5cad391a48754d837a95a8aaf74324bc28b4a0b0296ed8196 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-worker-release-01.json | 413 | 6522e9f89e6234e7f6df5f4a8bc5987b6a020063bc6bf2af940f4987e0510418 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-worker-show-01.json | 7376 | bbcc3d3cd7cff1cd65e5df31b2c0ade18394d01d10ff7b316c2a0239fe92489d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-worker-start-01.json | 2251 | b157987a8306cc1535e29b5398c553e40569cd3674512ecbfed56e0295bb816c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-red-worker-start-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-verify-contract-v1-sha-01.txt | 163 | 5852a05446c36eceb6edf28997b5c745ec466255aefe105f9755585ef4e4126f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-verify-inputs-v1.txt | 2518 | 58832b13afde28e48669af48d00b9cb327da2cf5328c61828734d9f15bf7050d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-verify-split-01.json | 376 | 304427c7f975a74c98f15b0d89934385fc5281a72b7ac7e2c315a233c2dfac7d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-verify-wait-idle-01.json | 345 | e7968e3de7cfd0be8e7d6ee256fb91dca52ac4fc236e40135786b4ba125df7d0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr-a-verify-worker-start-01.json | 2250 | 78396b63f9de0c1004c4dd5eadb22ba40d5c7d058ac7d030add8c34abcc3bfc1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-docsrecheck-split-01.clock.txt | 21 | 6b0530c346916d5a377461dc617dba9f9f2fed6789bc24aff8addd53dd6d123a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-docsrecheck-split-01.json | 376 | 8395c0b74cfc34a6aeaedc387f44b3376bac7f21b689b88748c15a525cc64169 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-docsrecheck-wait-idle-01.json | 345 | 7452b2a58889dd4b3a9fc73710350d53989feeda4bcf7e4c21df1313892ce84b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-docsrecheck-worker-start-01.json | 2250 | 1999fcb2fbf9591b704acdfba0e5a21a9b240726b40a2f7d09cc34ed14ba50eb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-docsrecheck2-split-01.clock.txt | 21 | 285a020a5aaf6a4172269b7e566887512cd612c384b575c08ccece7210d28dde |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-docsrecheck2-split-01.json | 376 | 60baef9478f6a1fe2dc7a107df5d5bc0dc664907d55dba871400db21c2bf4faa |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-docsrecheck2-wait-idle-01.json | 345 | c13279f568607bdb4d043888a87ac65967dd7bdd05b6c0d40e66af3b5e71f815 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-docsrecheck2-worker-start-01.json | 2250 | f852e40e9ffef60673ab78630b9cdbf7ca4a168cd037f12ccf5864d0105f7c9d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-impl-inputs-v1.txt | 1265 | ea448123572d3ac38a623c55f61cde761a377da6bc9fb2dd7265db6cd38a983a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-impl-inputs-v2.txt | 2096 | 88c239733798776bc1c85b4ec0acdc1f5b5ef5675443fcf83ca833b4568d2116 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-impl-split-01.json | 376 | bea42948d459d6dc6bade8a81fac1062ad09042ee6adca02736c4f6687ba8934 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-impl-wait-idle-01.json | 345 | cefc6175ebd336f902952e6e391d620a6adf967c7ce478ffa2b5dbfa11e4e3aa |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-impl-worker-start-01.json | 2250 | 65a05802129317932d7f9e7a4e25881bbbf4b966c59688e78b5307aee0c61da4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-impl-worker-start-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-impl2-split-01.clock.txt | 21 | f454a928de0bb9ad8d8f1e532233f54807db60a4f6eed386529e3ce53fea9f79 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-impl2-split-01.json | 376 | e3e15a42e84a379e71f199ef9cb01902f36377ee7b7445288ca165dc700e6e99 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-impl2-wait-idle-01.json | 345 | e1aef7e46aed5f52f3e1d46ef6eb232229f39cdcdc13a07de80bf4b9be22fe2e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-impl2-worker-start-01.json | 2250 | bfec2275e4a746137cf2c4790abc157be78f20c7e533c595236a4e88e2f40de4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-merge-main-b8631ccb.txt | 1836 | 74a18ffdd59a810e53afd1389fe3c3bec9273b6861efb585f6eb72542b18a4b9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-merge-tree-01.txt | 41 | 35bb795d5a8479c2980d494cf39ee4c9f7d95ce9c84e26bca595ceb3bfd43aba |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-red-close.json | 319 | 176881a4093b5648f7443db32cc5114c4302033719813a7f79a4005ba68431d6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-red-inputs-v1.txt | 1115 | ca2ba1c878a2a5ca76cf45ac37edd54eccd4750a2f00ebdc57dc016636e0ac64 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-red-release.json | 413 | c33229908f84d849b57745796399168b6d5e844e92a9dc7d610911eac2de28e4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-red-split-01.json | 376 | e957d5a6c4442d1ce837ba02f234a3a782d740f9a586962d7ca96833c1d66321 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-red-wait-idle-01.json | 345 | 0d49ba605545ac5f20256910bdc88ce06562671648660056b462cb89db46b8e3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-red-worker-start-01.json | 2250 | fcf5147422d40dbc7e9357065400e3e11b0a581ba41a4496ba8d087a3003f19a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-red-worker-start-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify-inputs-v1.txt | 3076 | f2e21f50651285dbf9cbb0e58913be9ee3297b2582c86636fc7150ea9cfdbf13 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify-inputs-v2.txt | 4781 | b722cf496d3c3924b03125d3a169390a3d290a1acc0a3b5a9a04d4834d96e39d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify-inputs-v3.txt | 7029 | bac63deb5562c827341fd8bcf643197c88dec3abc1fd488849dbd993a7fa85a0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify-split-01.clock.txt | 21 | 64b9bd57fc386adb4fcbb380e46d00a1a2aefbc5bc39d83431c42851bec481f2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify-split-01.json | 376 | b244f5bebac53529315869d70983ef0c77670ebe746ac8eab0b04141442b1f84 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify-wait-idle-01.json | 345 | 173cf3eaa5c088271bb4c6ed36244e84cab4971c717d8ec552d1d64066513a1f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify-worker-start-01.json | 2250 | fe4ea53dfe33422003ba20a838ed71b9beed7f17240228bae7773457d4d3eb23 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify2-close-01.json | 319 | 840fa54c6f505eae839f9b6ae3983560763520cb121f0ed1d2b797dd75ff8e2a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify2-release-01.json | 413 | e420e09ba19111d4cbdf38770818ef5c6793d50d738034ba64444829214328e3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify2-split-01.clock.txt | 21 | f337d4aab8c52853606d9777076433d1e5a36886dc2c984785d0cc7649b86cee |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify2-split-01.json | 377 | 33cdc25d5dec1a1a1860374c8fd10d7226802b9de845a63cfb42d8a183e5f764 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify2-wait-idle-01.json | 345 | 07d31fe96f889898c16f720128b226ebf52bf297493c0dedaf59c13602e176c5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify2-worker-start-01.json | 2249 | 17d4e4435011da11ef2c255f01ac8c2d9b504ce0fba4b1ab10b2956874b21228 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify3-split-01.clock.txt | 21 | b0b4d62770a3c6bbd4f2995fd126d6353833931012735a9a316d3d56157f80c7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify3-split-01.json | 376 | 65016e0ef4a46d0d43ba0c60285af20eff21bc35410120d95d985e8c3b794bea |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify3-wait-idle-01.json | 345 | de8bb747d9895b833556dabe7fc53c9f4ca7bffcee8b085821954a028a8869a6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr268-verify3-worker-start-01.json | 2250 | a646691fcef52aaff584f4c85c5b5252287e832f5063cad8fba751d2a9da509f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr295-client-settings-diff-01.txt | 1352 | 113e6e6e6db01384cbff54181ed1f843b3c14547e33edb4bf5771237c6542c30 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/pr297-view-01.json | 385 | 8bff0ceea0314a0f44d5caa8076b21e5af9d45790c2526a761f2ff62cff87833 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/push-closeout-branch-01.txt | 549 | 962806c200432de73668191e1ff177f19058d339899d57f5942076f08e6577d9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/push-pr-a-01.txt | 537 | 93c7157bfa25259349fb8240959f880d07cef18d8005300dc1d30a759e23b1f3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/push-pr-a-02.txt | 166 | 00b4a58e80a32bc784bdf02817fcee2ad1b8dee11171997317755c5319f231c9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/push-pr268-docsfix-01.txt | 137 | bddfe2cdf002e1cb92edd9019d976a35322cab2eaec2dbd0e16f0541c4471403 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/push-pr268-v3.txt | 137 | 2fb630af3520066759f31686cc5fc4305121c0e9bbeedbf89250bd90798924c5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/push-reentry-20261104-01.txt | 129 | f00b77aa3e4e9ccb40b943c11a3d23950b5bf45827cc59e3fd7f2f0143adfc0a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recent-prs.json | 468 | c2b630772bfd78fe1541ee423be311f9e4377709d8395e522786866c2f39c83b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-close-01.clock.txt | 21 | b87ecb637e40b5d5da03dcd5a39ddd3195989258c2b96adf8cedbee09cb8b3e6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-close-01.json | 319 | 6feb4c136a20e23de8ea6d414bdfa24bfb10bde98eb6b7f6985a03a321b4f7f1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-split-01.clock.txt | 21 | 0d9d6cc311e7d7317bbb1065b38523e8ffff746451d04d8d1ca0bcb1e5ae77a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-split-01.json | 377 | f89a22b07492d43529897805e45cc5965e8c2e0e771c007000ece62829aeb5e1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-verify-close-01.clock.txt | 21 | 5163be07484d660a83f6bf1da54aa8fc07f3ad598c2cd10b736f07a304925b9a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-verify-close-01.json | 319 | 570d5d2763213e58075e81412a1e73bff79cddcd0e3b5c099be1cac35686dcd6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-verify-inputs-v1.txt | 11785 | 986844779b33e62c85c1f573be8cd668d35e51dabba078d15761392cb170c7da |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-verify-split-01.clock.txt | 21 | 31512b3fb7bd81b8fbeb1f1409c60e4de8ad7ca2933cce48d1f43f2bf9960198 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-verify-split-01.json | 376 | 0585b9f84a4da0b83fc39af4490b5987fe11532d8e930197c23395f2ff6f7cfb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-verify-wait-idle-01.json | 345 | a6b2923fad0384380cb1fff7b4b438298aed067f4fdb9ee1bc109cb06de90de3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-verify-worker-start-01.json | 394 | f0f269a48950a5191b9415d9e864da91d906a9cdb8b8d1b39fbd900709a97279 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-wait-idle-01.json | 345 | 4028ea3126f2bc9fe73d41c0b79d93e753397fde6de634bbee46226d0fda04fa |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-worker-inputs-v1.txt | 2653 | 772be3d675dbe7ce63601c8618beb430b57798fb9797ed72546b173ed88b502b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-build-worker-start-01.json | 394 | 6b7bf42ba65ae38dd2a99e18139d8870c276a4c5791fa711fcc1a7f89e6c7410 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-executor-split-01.clock.txt | 21 | 3df04843bb2f82f61a39a4e43b203f063c5d3500473c1352a94290e330ab728c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-executor-split-01.json | 376 | 26bb7e0d7359d07c3f7fe6b9c456cd35434992ac7b3673d288a113674f1d3bd1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-executor-worker-start-01.json | 394 | a47fdb3e0aa4785440458402fce85a439736a38dd964b10346278a614926ab72 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-r3-executor-split-01.clock.txt | 21 | 9efa33df02d968342447390a542f8ee62becd1d902c9eaa29e3416a6835d79d5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-r3-executor-split-01.json | 376 | d1b5b19002f7ffd917e48a7ae821dea745c8da4a9d58fc159974ce61771d86af |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-r3-executor-worker-start-01.json | 394 | 6884e056c6883e7136de0e831a021fb79bcefb18119e66d8accbb9960b243f8f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-r3-verify-inputs-v1.txt | 5174 | c08ec273f494e9f8c744d435929a00fffa93c2e1d36ae271d7c34b3206ac0784 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-r3-verify-split-01.clock.txt | 21 | 8c099d749c6bab8de00e9a7e6e2077a3e53427b18aa1290302fbbf1ced875b76 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-r3-verify-split-01.json | 376 | 458f059b03505eda195447c5dda0da296cecbbac4d70e37d5fd92262e04fd28f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-r3-verify-worker-start-01.json | 394 | ad6dbfbef162ec9aa66bed050f03326dcc5db5f59b9d174aa766874aee88704f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-run-contract-r3-v1-inputs.txt | 6041 | b4b54ffe6e3c5d9dd191a5cad1d1a8e6fbfa688b99e0adc8d53ba4fe14480550 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-v3-av-sync-01.json | 1552 | f353f936fc97533edc2211914f82154cfa2b7bf64a4371191f284aa63ab6b474 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-v3-av-sync-02.json | 495 | e55e4c2388e46becbca5614d275e559c28fba5ecc9f11d953bec05e46e1a278b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-v3-errata-values-01.json | 3494 | ac7fc89bf25451047fda1da95e300d7083af7401687081293f041ed50af9546e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-v3-facts-01.json | 22479 | 1b244eb9e373cd4c5ec9d4170f9c96613900bb6e19ba7d2b53f6c22b6983e95d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-v3-lead-judgement.sha256.txt | 97 | 65e5324fcc8e07707324c6afd9a91400ce793fd239646cbea796efea180871f7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-v3-verify-inputs-v1.txt | 9658 | 00e1c21d33cab07ec5afdb04c70577ac752d1e02a0e803a8ee01a22bbaf264fa |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-v3-verify-inputs-v2.txt | 10025 | e46f999d66dcf7258cc1e6733917606f5405f5abe7d437fb9d2e6747deb0fb88 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-v3-verify-split-01.clock.txt | 21 | d9956c4d0cb79a7e1d2b6782da5831211111a67a275fe6a1a9575019d018eb03 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-v3-verify-split-01.json | 376 | da88c847ff336c0c10dc1cb7023c3a079fb72839540afcd2f3be12ac100e82ab |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-v3-verify-worker-start-01.json | 394 | 9445a979b3c7aac3dfe9cc620e755f241ce99d753446894bd5689d0fba3e63b3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-run-contract-v2-inputs.txt | 4636 | b0c968c72cf971f30e80812223b782f94e35a4fc8001c8b1ddf5bc12460bbed9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-run-split-01.clock.txt | 21 | afd086a67ca1d0aa9cdbf3b2f40ac75fd671308208b31e9b5972df767e1d2f0d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-run-split-01.json | 376 | 92398d61966c09877f7ad77f42c37a09730377f54028d91fc89692b14ddb9f64 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-run-v1-close-01.json | 319 | d966b7335343c1cd2197c30805f0af59cd61131b637841170ea21efedc7adbb4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-run-v2-split-01.clock.txt | 21 | 65d443e5071dfe13773ffbe623f66d191c5527590e78f99168e0c3acf7912a76 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-run-v2-split-01.json | 376 | 14ce9540c65735f5af51c7dfb4e3b1d07daf6e6612c5d01bc9fd19a263c4b393 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-run-v2-wait-01.json | 345 | eada77e32ca3eead36313584c5af96207c9747ba3431a3572c39db5375ef23b7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-run-v2-worker-start-01.json | 394 | 28e7a726a46bd585de5a9634f53042717576e8d293137269fab6e59b6d16527e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-run-wait-idle-01.json | 345 | 0dc89a42185e80384f4dad02039cb0ee39920209a5fd2fe4fef012e46fabf6b0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-run-worker-start-01.json | 394 | e2cbe3120674507f364193d7ea8befe25a7edd8f3a6eea41a592c06c9bc78bc4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-v2-lead-judgement.sha256.txt | 65 | 0a343ca4aa0fa6a9dec232f3e6086ecdc3053799fcf9f9f21eec633df6f5fbaf |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-v2-verify-split-01.clock.txt | 21 | c503fa1d7bce68206c3184ca29b2bf311c62edc5524c11a6f9b01edee575bfaa |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-v2-verify-split-01.json | 376 | d9eb385aa0216e1fd0d5d6debc6a276278e731573d03d822e3002e2c675defa0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/rehearsal-v2-verify-worker-start-01.json | 394 | 24dbda0d258a7e39b139a0a6fa1d938a406cb04215eb5c7cd7337e6fdaf3e0a3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/release-docsrecheck-01.json | 413 | d5c06d22800ade312e10a3a6c77daa2958f4dbfa32bedcbdd04068ab339e6f26 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/release-docsrecheck2-01.json | 413 | 9327dea7b3219182126897bf2d7bed5a97d1334ae69418909bccc18b1cd3a920 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/release-impl2-01.json | 413 | 62b377d98f38339faaddd5a027f8db137086940754e36f3fe4c06e585a95abff |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/release-verify3-01.json | 584 | 1c8a97c286782eff809e52f9703cc350fb6887f033f78c0dbc8ca25c5c593ac2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/reply-executor-stop-ask-20261106-01.receipt.json | 93 | e364cb691cc0c0127cf77ef0bca0ed8606bf09b14492b3b88e2a7f5b39dfa8ef |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/reply-r4-recovery-6a8a2b55ed50.receipt.json | 93 | 17bde42d7ad1a5f7be2b6a891d8dec80709b4dd98c332bb8922355e26646259e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/reply-recording-executor-q-7030fe44e9cd.receipt.json | 93 | 4edd46de20757f0ee82e2bebcebe1e70cb742d88d442894c77d71ed6743c8f5c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/reply-recording-executor-q-8dc1d362a630.receipt.json | 93 | d1e70bf03a0f37f3939e9713b3e93a0492d03124c70ceadb7876921673afd09f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/reply-recording-executor-q-efc9e1f6aa33.receipt.json | 93 | 97d576bf00e998d8d3641ddd9a4427e9f26a9797ad0b91449ec5643f734ad633 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/reply-recording-executor-q-fd3e7497b01a.receipt.json | 93 | d1d9b15918c03609152e5e12db5bbaaa8bf6cb4e560eeeb0eae63935498792d6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/reply-recording-r3-verifier-q-172f56d2069c.receipt.json | 93 | 1e163da6f836d01da565bc940cd9c1f0040f97d23a4c1e046068cba73950982e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/reply-recording-v3-verifier-q-4ab619294633.receipt.json | 93 | 026b8238d53d1660296b64f172da0f279754229c336dd5a1d9b03d08ae1e91bf |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/reply-rehearsal-v2-q-11516e5c599b.receipt.json | 93 | c142df9134ffab3aa4afcf3103d71423b415a822d6fbffa3e6e19b754fa12b9b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/reply-t7-answer-writer-01.receipt.json | 5272 | 16c12d5a5cf838d3ed433214fa58049af97d0d7fec74cb6aca2ce0759e76a150 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/reply-u3-wait-verifier-01.receipt.json | 2520 | da3a4348ebb5bc25132ac558f3e448a351d075ca0701f000b0611391b322c79a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/reply-verifier2-drift-01.receipt.json | 2698 | 8feb58dc55801de453ea9e79488e4fbf53a576d95936d9fed4e970932c1c1cb6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/reply-verifier3-q-c30d5975de99.receipt.json | 2368 | ecc0d334047cef149f2d6419ed73d3f575bb02043885fb5448bc892f62257cc7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/run-41222662857-01.json | 2038 | 2a9503150e238fc1e514fe87888948ccbcc6466215367a2cebdb8afdeb30041d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/run-baseline-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/run-baseline-01.stdout.txt | 185 | 977243dcb00bb411d38022afad418bd8056b32197ba790ff08a7e8299f8e8768 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/scan-server-preview-01.json | 216 | e1844e91a114967c18846b77d66cde24b8d895ea9d7adcc12e79fa27d9b17ff5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/scan-server-preview-01.stderr.txt | 0 | e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/scan-server-preview-01.stdout.txt | 146 | 60f376c5b13d57a5488491e2a0f11c063da175fa565eb52fa57edcabb1d6535a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-a8-art-01.receipt.json | 2864 | 8298d8b00399a492ed63faba6be676fa3c6ff6ac8fbaf9ef89d1c6e853ef0673 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-a8-opinion-verifier-01.receipt.json | 3734 | 438b6e86b736dc06f4a142c1c7197978cee8e2e88f2e012d178fb4ec875e7a1c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-ack-decisions-100-102-main-01.receipt.json | 2270 | 801849f50b8c453bd7d0062e1de7cb06eea9dc2c7db851bdc353f0b1eabb661a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-after-merge-main-01.receipt.json | 2298 | 76aaaee9a9f98db12a625d98c9842f2c8a377503409aa36dd675d3b001e933b9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-art-grid-eta-01.receipt.json | 1502 | 741bf3333a72605a99fa0c765f99fade9aa00a3a23c1525fba94e74a6155d088 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-auto-recording-plan-main-20261106-01.receipt.json | 230 | 6bfe4988286bb682db89a04ca697fbc85fa3e028c79b46a16e4f66a6f48f0d4e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-baseline-writer-01.receipt.json | 2674 | 755fd85d829931f5b8c0d5c6f733d62794fdae5961fcb05772cec67cf08a828d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-build-handoff-core-20261106-02.cjs | 5034 | 8c3d55b2e0e709718ace59c79a264271450c9406f4bb3f71abb4d79d642542e4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-build-handoff-core-20261106-02.receipt.json | 54 | 8eb5dcd58ca7c5aef63f48e636d8cc01eadf8a05d306e322c363f26f727d0823 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-build-result-verify-contract-main-20261106-01.receipt.json | 230 | 3a56aed6142f94694815384ef4af859036b48539aae8ecf6c63752466bd40eb3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-build-window-open-worker-20261106-01.receipt.json | 54 | fade371ae96ab4aebd8311e2a64924ba80bf089c7ac855d900e75dd10e7f9fb6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-build-worker-started-main-20261106-01.receipt.json | 230 | 835544cd61e1cebadf8c00bdb39d7439a5fec313ce9e65d33efa9a47caf58d33 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-c1-conditional-verifier-main-01.main.receipt.json | 2240 | 28f4ea57a126001cb4c8eb8a0489ae119901c3e8ef700fd5eb5f26d516f47b55 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-c1-conditional-verifier-main-01.verifier.receipt.json | 2188 | 21dbd430ca8da30723c4d42f5e3ba6d5a93a2a55ba3d993e8d96b5319688d31c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-c11-supplement-main-20261106-01.receipt.json | 230 | d73a41cca62a66ec6f22795cf3a07a5854ae3f9f1b9a8a86fc1878de38aae18c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-c12-c13-layout-main-20261106-01.receipt.json | 230 | deff6f7a42de611316ef8d182e144cfb84a31c65542e903a6331d992d3ff0aea |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-closeout-main-01.receipt.json | 3668 | cd92c8fbb308e1da63b4284797275c766832dc8262cb85f7f11680bca680e700 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-contract-draft-core-01.receipt.json | 8861 | 1062524dd9b60a1437381d803eefc52351d68dafddc1968f11dae45d659525ce |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-contract-draft-main-01.receipt.json | 1473 | f1087658ac3b7ef4638e190757ceaad6b1a6cf8ce7436f3a2d5a0b4417b1f7c6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-contract-draft-v2-core-01.receipt.json | 1777 | 935fa0168f3678bc06a1654556c1ab5261b52875cc671114eceb664b9de7f06d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-contract-draft-v2-main-01.receipt.json | 1671 | 1d3a9be3fbe0a2fe8bc1ce20503e705cb21f3538f0a91f0c0ee435c4edae43a1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-contract-draft-v3-01.core.receipt.json | 2099 | c35ebfac4429eee0bc6bcb283308f279163313f468d507bc757f7e2897dfa57d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-contract-draft-v3-01.main.receipt.json | 1525 | 825fe7d77239984ecb3df2b6700517b5e217c853f78606ac0e23697d1111bcf3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-contract-draft-v4-01.core.receipt.json | 2629 | 6a52a32d06ab07f38215a19cc7f4fe84bc277c6d1b16ccf9f871e00be72e0f24 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-contract-draft-v4-01.main.receipt.json | 1617 | 15e5a6aa5d954183ea4dc490844d524ccbde1713219125dc5cb65cc121770f33 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-contract-draft-v7-main-01.receipt.json | 1555 | cc454e7642d10acd2481055230bdc1cae50c96b2d5c394a3e9fd88c9ad45c070 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-contract-draft-v8-main-01.receipt.json | 1764 | 069ca1e59062f28333de1acf45e43771e993b6b78e86a9e3cd5301f6722c1edc |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-core-inventory24-reply-01.receipt.json | 5034 | e3cf35b30be007433569ea728d8d0dfda2c7461ebee71904bf80e6f12f8e79e0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-core-window8-server-q-01.receipt.json | 2411 | e6f799ba9300e180917ac0d1809f5027b6c93ef82994c505ddf96eb649607778 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-docs-recheck-started-main-01.receipt.json | 2060 | 9488d101bca6f18f922140cb8a0ebb33c35374ca86ea173a1b47d2ea06a0b864 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-docs-recheck2-started-main-01.receipt.json | 3043 | ed4cd29dfd5d688753fb742b7296a7a63fdb637828f90b4a423f958d75ceee13 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-fixed-head-core-20261106-01.receipt.json | 54 | d7c78fa9d2b9ec4bf9b9f11cda07c08fe3b80875342c34a2c6ddacbd0947c9ed |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-hud-gold-check-01.art.receipt.json | 2576 | ba0f158b183b936502b1f15862c8668dc56f8cb47b2454e9a92eff4f3b95db48 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-hud-gold-check-01.core.receipt.json | 1619 | 40f1555ba60533ca2bd5f09c479cec075afc43f2661d02d31f4eb83f995c43ab |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-hud-gold-check-01.main.receipt.json | 4361 | 4d6bbbb2b581774cc0c9876bd2df3094be17a467cb82f34a2e9b9f09de411bd5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-impl-start-main-01.receipt.json | 2261 | 22bd6949a4ba478f205efd131afaba61ea66c14cd6d8ecfa8745e84ec4651057 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-impl2-start-main-01.receipt.json | 2728 | f22a3bc8d3d56f56e6f3dce5098fc6abc202d37e8624be9f23060e827d7ca6be |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-inventory-edge-core-20261104-01.receipt.json | 54 | e7a1a67aa22f9e816b513da3fda4be7257e5b757bbe2b80f71e9d20a4a802365 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-player-start-main-01.receipt.json | 1459 | 641296a33c9ee3566add62ef71e3d64bbe8bab5afddc49b640baf45e56f82130 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-pr-c-cancel-main-01.receipt.json | 2061 | 6faa02ca674bc7e94b6df8385b5f2c1537660d6f1a4b10cae4f936dcff7b14a0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-pr268-ready-main-01.receipt.json | 5642 | f2066005112cfb3d199cbe74ffd3245c4ee9077583cae50b8b0137ae5f458d46 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-pr291-ready-main-01.receipt.json | 4261 | 24dfae49b5fe43cbfa6032b6687beb5ebafd8078114974063faedbe803a77eb2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-r7-closeout-correction-20261106-01.receipt.json | 54 | 320ebcf1c0cf1aed7e41a87d71d8daae5031d5026906e07ad0ebe2608d569c3e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-r7-window-risk-main-20261106-01.receipt.json | 230 | 5fb1042f26da3d1742cb7f9f2bdcb9bf74d113a556431fb4a8512521828dcaf3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-ready-main-01.receipt.json | 2576 | 1b431f6e11b3a8f6b2f25b73660901d2ea50cc6a7e14b360c22a7c83dcad0668 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-ready-main-20261104-01.receipt.json | 230 | df6a6ff717c9a373c9e4f7fa7b693f46fc06305eaf2f12926113ae94aa9c80e9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-ready-resume-main-01.receipt.json | 2746 | de8bf5d00715b8f279ffea5a48fe7535bd3f98e0a17b999bae6752f386a01cb8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-recording-build-core-01.core.receipt.json | 2440 | 5e66aa15637898e1ea0188cf69cfe13dd35381a43535d3c41f39310f32b2f686 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-recording-executor-ready-main-20261106-01.receipt.json | 230 | 20832300df9e87b0f860d1f49329cfa1c9af937dbf365564ba3c01d46655fc66 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-recording-executor-started-main-20261106-01.receipt.json | 230 | ac85a88bfca0f337fc504c2e2e3b424e34301aafef0cbad4bd507b5111f14858 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-recording-v3-disposition-done-main-20261106-01.receipt.json | 230 | dfb99cdd807266c5c87e538e6b03d463e396cebddeb8a3add9a8ee93c6ea4ca7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-recording-v3-verify-result-main-20261106-01.receipt.json | 230 | 2a1f9d6a937b3f14cef51f976776197a8b08a2bdc102fa83bd338c9e3874acdb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-recording-v3-verify-started-main-20261106-01.receipt.json | 230 | 806518bba6bd6e9c3bebbdfcd5a6e837addc59193705536d4eaa310fc87b2289 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-red-done-main-01.receipt.json | 3208 | c683a39e4f83b557f67ff56ddb0a70b278f3189ebf794a01458a4af5c486e960 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-rehearsal-contract-core-20261106-01.receipt.json | 54 | 843371d83ee8c03e1c5435c34114cc2babb6148abd1177177dc0f60ad3548e3a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-rehearsal-contract-main-20261106-01.receipt.json | 230 | 272d9c2b2ff95a93e27166c4ab96d0b66960a2610870b6931d1227893c253d9f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-rehearsal-contract-v2-core-20261106-01.receipt.json | 54 | d040cd0fbc62ba113c0d97f320f7bcad71da7b6e20c953113ffdcd8850c6a1e3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-rehearsal-contract-v2-main-20261106-01.receipt.json | 230 | ec53b169c54d4ee7841df76097273c899b5cc0da0d201f944761e0449e491355 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-rehearsal-executor-started-main-20261106-01.receipt.json | 230 | a0fadbc3c63fd75caee3f53b99d9b5f0521e3e68e3aa26941075dbd0fe4df297 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-rehearsal-prep-core-20261106-01.receipt.json | 54 | 442685b6d501f17a5e570dbada9b6151e0ecb2b03ba606cab7b68a609f9eb943 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-rehearsal-v2-ready-main-20261106-01.receipt.json | 230 | 5dd6bc5f78f58ad073fb15dc73327e757667c8aa579b82cffe89bf051120c76b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-rehearsal-v2-result-core-20261106-01.cjs | 3836 | 11a5cf04c0d143d5b46afe8fd5962046dc5dbe64a9a63e9afcd55a2523db525f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-rehearsal-v2-result-core-20261106-01.receipt.json | 54 | 05729da36053ca3f21884cc39e83fe2c7b06dd8e140eb6a7bd005fd69cb88fb1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-rehearsal-v2-result-main-20261106-01.receipt.json | 230 | f32829233e2e5bdc29e64189acfa4e2bf038693703a96f777538e4fa94824702 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-rehearsal-v2-started-main-20261106-01.receipt.json | 230 | b0ee2102b430bf63893ed6e9439ecab5e0b1aa514bed05463df73448885ed875 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-round3-ack-main-20261106-01.receipt.json | 230 | b58034566a66e783e921268b1e062194bd7c25536a77295ae24bfa6020437be3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-round3-correction-main-20261106-01.receipt.json | 230 | 725436d69a7864896cd62f9062bf3b5d8d5918d16de4e7927015967eb05bed2f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-round3-executor-started-main-20261106-01.receipt.json | 230 | 56fc2e165745ac7ba9b36ddd23d3b1b8dd499067cd84412361cab505e34b45a8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-round3-server-start-request-main-20261106-01.receipt.json | 230 | 686373db07424e1994259cb52e699f4ed9dc0dedfd0ccb09baa619ac458c2c35 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-round3-server-stop-request-main-20261106-01.receipt.json | 230 | 30ebe4187203564eca0c115deb402713831a91500c8d724b7c29b822f9772b15 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-round3-verify-result-main-20261106-01.cjs | 6730 | 6c67d11d69f1e23757cce8e433c2cd2936d7581fe7ab883e08a69f96ece85d30 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-round3-verify-result-main-20261106-01.receipt.json | 230 | ec8a6bc589195468315ad9497a3e0c222f830c5db0be4d3618dfe6bc20306bc8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-round3-verify-started-main-20261106-01.receipt.json | 230 | a0eda3949834d41d0369ac235e0f9473d24e5e541f7c3323f202a60410e1450c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-scope-draft-v1-main-01.receipt.json | 5185 | e7ca01967ab2171572524a8a4c8f0828a884b54b4e2627069a43adf3b62d6246 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-signal-r2-start-executor-20261106-01.receipt.json | 122 | e1766e5321f2b97623ba9f754dd7c99790cd5f9b7338246a08e0dd4bd51f52ad |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-signal-r2-start-v2-20261106-01.receipt.json | 54 | dc2d824f8c4fb72c28d6afb33053f45aee91a4b169b89e9d70f8ed90d737bdd7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-signal-r7-start-v2-20261106-01.receipt.json | 54 | 4e1cb43b00adb6ccfc1eed658eb3a8a62b916e6a2f6da428730f6060f5edd7e5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-signal-round2-start-executor-20261106-01.receipt.json | 122 | 51880aad4c6094756a8ca1c84e84e29ab60f75ffe0739e6172d7da45190a5284 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-signal-round3-start-executor-20261106-01.receipt.json | 122 | 3e234144b54629d0a97304e2d9050cf41e8106113db5cf69d0aa2ba180f44e48 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-signal-stop-executor-20261106-01.receipt.json | 54 | 620c68171e421fbde986dced0db18da389dcc45c8950330cb0fb0c5f09332185 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-signal-wait-selftest-executor-20261106-01.receipt.json | 122 | 511889ecd617433c4295075237ae2427395556a4227f9de454bcaa20725603ed |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-signal-wait-selftest-r3-executor-20261106-01.receipt.json | 122 | 85b462a55506cbb51293a6979ff92e3437f14f7fa9c94276bb20e3ab9691673e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-t7-table-main-01.receipt.json | 3122 | 75809ff28e3ad994ad3800f36cee391e05704b9ad9351e444841b05818762ea9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-u1-request-main-01.receipt.json | 2508 | 30b12cc3081e406cfc394ff3556ba58322a24d414629795caa12e678b91a0c71 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-u2-request-main-01.receipt.json | 2312 | 16162f021315650016cd98c51f459697f313d0e59d7b6f8ce62c572722c687a7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-u3-request-main-01.receipt.json | 2494 | 6dc05567ca210cfd91e700660a66d69f0be6452c0567761054d2cf8a975ccff6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-u3v2-request-main-01.receipt.json | 2252 | 52a26ef6837e0b22bc7380e6727f296b7681312c820a176128c9985f5e508396 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-u3v2-schedule-verifier-01.receipt.json | 1428 | 4c06561c251f0c6a178a605456794a428e77c2ce28c75928d8172fe9202de19e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-u3v2-window-done-main-01.receipt.json | 2445 | b10e28d09fdf3759fa2cfed9bd1874910c13e59d372b38f2f83aac22dc13658b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-u3v2-window-open-verifier-01.receipt.json | 1941 | c57158547ebc3ba4596c646ae53a9d01bc801eb96ad7d2138a65400d744e707e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-v10-main-20261104-01.receipt.json | 230 | 64638d1296db4918e28f5ff2228817aacb53c53e7b391b6250ba46718142c4be |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-v12-main-20261106-01.receipt.json | 230 | 7c53e65501ee525988cba423aa84f7782f1a6707df6abbd276d82d01054c889c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-v3-core-plan-answer-20261106-01.cjs | 3962 | d4e3323ebb9e55992e2590a135c3ed8beda65943c16b0c21b6e48db4978d4ad7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-v3-core-plan-answer-20261106-01.receipt.json | 54 | defc4dd6305ece823b9ecb7de0bd71e5540cb7e993b3ef0a782a34bac54707ad |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-v3-start-state-core-20261106-01.receipt.json | 54 | 53aff2865f273cfe75ce93d8eeb66743ad9bc00d078e20063a38b8dd7ea774b5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-verifier-launch-main-20261106-01.receipt.json | 230 | 3b564cf6c3322e27fe0574053e9357b1c5c80746234cd7d8c0847edbc7332f82 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-verifier-schedule-01.receipt.json | 1690 | 77d0c1acc97924e49268730e9e2f9b0e62ddc2e7096a8b71236bd48960c3aafc |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-verifier-started-main-20261106-01.receipt.json | 230 | 68fb8befa182dc590a1b8a0b6984c66a46a269a7bc24fd01c47b01b7bd342270 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-verifier-stop-01.receipt.json | 2160 | cfa2305ec4d0f10601766c1fe2317d8e427da5440c255790ed66d7d4991115bc |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-verify-start-main-01.receipt.json | 2894 | cd6888764616a8334abed6abfe028c6a32849b16763c8829e1f43d2d2794227f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-verify-start-main-268-01.receipt.json | 3815 | 462749834290c6ece7d930bce7d8549d82091adb5a3ff8e4d42b860171ba2cb9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-verify-verdict-main-20261106-01.receipt.json | 230 | 16fa6879442c1a7bd7661c1c93207a92ae520bc8c500368956e56391fc67031a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-verify-verdict-v3-main-20261106-01.receipt.json | 230 | 13a66777a86fb2a50d1a599674280cfe6d649d2f5ebd885921897f367f5b161a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-verify2-result-main-01.receipt.json | 4878 | c7ca668b7269098ced40122c4c6895fb8ca6580d0dc50f4dc2f87156bdae939d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-verify2-start-main-01.receipt.json | 2437 | ec656120bef406d797826da7f32621900b75f92e53a904a473eed59bd3b7a4ed |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-verify3-start-main-01.receipt.json | 2605 | dafb95bf78b65bb721804d503237316ea83e2a13b078feed766d1ad7175a8291 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-viewof-agree-core-01.core.receipt.json | 1578 | 9c22f06301b67d8ddf4c1a57792cbdddc4be4cf8b6dfe8dfdc4cbffe7b51ac9c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-viewof-agree-core-01.main.receipt.json | 1442 | 59a24d587333d0e4897f48d34faab019b703dbc84c039b7687965baabb805809 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window1-done-main-01.receipt.json | 2253 | 916538200ab07ae1524a1f5c183b2585c4e8970bac45684ad77c3fe10dbac426 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window1-request-main-01.receipt.json | 2747 | 959d3021fcb3ed03437875a83f9fdc95b1e286e01695426d3b7dae2d31364a3d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window2-done-main-01.receipt.json | 2507 | ca3b6938f0a146ea4b6803355f811a68cf0674f76ad51b202856118f1b5a6871 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window2-open-writer-01.receipt.json | 2137 | dd16f748f71fc096494b7e34506410ecbee15ccc542614f25da37eed61f92782 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window3-done-main-01.receipt.json | 2204 | 172b32a7b5258f1b41fe5b724de26ee7916c1926fe3bd6ea77a7d23dc2dcb75b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window3-open-impl-01.receipt.json | 1831 | d029c9e90fa48d4e6b8a401d43e86bdd8d2df4e997b5542ea7ba43a98acce539 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window4-done-main-01.receipt.json | 2147 | 32c80e72f62e03046e0117810aaa9ecb3fd9ccff688bdebed7d3b91611e4f5b5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window4-open-verifier-01.receipt.json | 1696 | 652a89159c94c78fa4ecdc45fb2f60e7d2b76a61169ead76572276142619f652 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window5-end-main-01.receipt.json | 2150 | c6396f32f8ad0080af8c4a621733fac04c6f4d1cc3b6a7b12a951fee443fff2b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window5-main-01.receipt.json | 3788 | 85d303e2cbfeb9b38e2da3472f9104cc21c7d4c98c80eef6afef8b97d3695ad0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window5-open-worker-01.receipt.json | 1822 | ed56991660d6e7e6db658154ccefd88f0bc8a7052af3916c7efc13f784edde21 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window6-end-main-01.receipt.json | 2586 | 6bbf68372a3726b66c05f60626edeadf5b9e8d332fc12aadd4c6d7a4a6ea7bfd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window6-main-01.receipt.json | 2586 | 0997a04c0bdb281136e4129db4e6f73d10210aa02e95a637031fa234fec4e76a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window6-open-worker-01.receipt.json | 2042 | 94a9015aa5b36e08ee1dd8bf4e4236fd77a63f11864ac9ec734bffaf77e738c5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window7-req-main-01.receipt.json | 2382 | cb162784d5b812ba11dffdfd93fc4e3fa857d470b0af392561e4bc90a165a724 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-a-verifier3-01.receipt.json | 1645 | 9ab46579089b522f2348bcca4812359d9033694e02bfaff3b6710b602cca391c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-c-verifier3-01.receipt.json | 1846 | 1d0b5cf9dc0d310d402a3625565dc1efc0f63745c2b9e2193ebd72599d63b67c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-core-scope-verifier3-01.receipt.json | 2140 | 130994f2bdc7f337630fa79646c86f98cd22668535561e624d9f5da41df84edd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-end-main-core-01.core.receipt.json | 2207 | 17e4b55ab80156591a70448aa4ace2de17222dde3bbb8fd51758a52b1e4f3703 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-end-main-core-01.main.receipt.json | 2562 | cf8647a93716af4436242059f5efca3d89675768f24b115079599aabefbadecb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-open-verifier3-01.core.receipt.json | 1385 | 937eae7617d47e4b258334e223ce837db4dbc9a57af16deaaf2200ef068f23d3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-open-verifier3-01.main.receipt.json | 1740 | 73a3ef76f7983d9a008d9336807b0ca967fc5e1e287303c7554db09e57819e4e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-open-verifier3-01.verifier.receipt.json | 2225 | f44eac8286811aa92c4e0d1ab407c88842c3e93a5e53ca07c3a8b5705bf4a09f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-ready-main-core-01.core.receipt.json | 2339 | 95f4131958a68a716f74bfe32341229634da7d3bb37dbd88e4bed5181642e006 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-ready-main-core-01.main.receipt.json | 2694 | 3c6a1d95eef524437442903795941eb2b3b3d98c87d2acca62c0a4e34a641ff5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-round1-end-main-01.core.receipt.json | 1813 | 5b44208e151384bead715dcdfc8d25e9813236377b4f705571099c59d096a2d7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-round1-end-main-01.main.receipt.json | 2168 | 43213a6c041839cf87a881f955b1c9b8c4749dff2c11bf7eb7cabeead79ecd4e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-round2-start-verifier3-01.receipt.json | 2184 | c45e040f925dfcd69a7f083993033324a66879fd0f1e7bfc37a097a2f26bac26 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-run-end-main-01.core.receipt.json | 2140 | b8056b4d409cdab1046d56a0f56b41d8bc36afd2e0d89343de151cf3179a2d8a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-run-end-main-01.main.receipt.json | 2495 | 5eada2a474f193317fd04904fa555574d2e5d5b052479f1e10b6d6f5641dd32e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-table-core-main-01.core.receipt.json | 4379 | 21f4bdce14e365986ac1b3d57a326f981a5a8b89b0eb1e134e9301c1f186921c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-table-core-main-01.main.receipt.json | 4734 | b47bebf570051669dd7445f6793ce2bcb22963f86ccda390786e0b0ef6b2a3a1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-user-guide-main-01.receipt.json | 3352 | a3cb032f0ed954257c5872692eb2c4d3c7ceaf05ce27b0b38fd1a79c01a90714 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-window8-wait-verifier3-01.receipt.json | 1492 | 3858f3ac51cbd83877ca01d467693bb196456eb7168e915ace3b8b33da244248 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-windowA-end-main-01.receipt.json | 2847 | 3de25bbded096a96f5a344c5668e669989118627d2d6956a18c74e6a11d24cba |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-windowA-open-impl-01.receipt.json | 1856 | e60f9349f8a2b3c38cd53268809111a39fd98c8397c8b51a272945b4137e3973 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-windowA-playmode-rerun-impl-01.receipt.json | 2239 | 493a7545c64dce1c424250f1283341869a799231de43b02fd83016e4ab3cfd3e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-windowA-request-main-01.receipt.json | 2080 | 9ed87f8aa56016471400601d96b41f56e9c116de0824c02733f4009202f3241a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-windowB-end-main-01.receipt.json | 2275 | 5c315d0bd96934600b4d17df87b6705b2089492ee82959ce98756adcd2c26715 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-windowB-open-verifier3-01.receipt.json | 2226 | 99c14313bc86517bc2a8d149a3a61503b7fea0f3826e35d715cc9dc6bc8607a9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-windowB-request-main-01.receipt.json | 2033 | 4dfc7b87a3ef00fe7b87d70a19cc17bdf37aefc36f310f2cf092ede764b00a3a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-worker-contract-main-20261106-01.receipt.json | 230 | 1768d1c83e369226b1d113f1ac99abe8bb8c559305f7f7f4f666ccf3a87697ed |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-wrapup-done-main-01.receipt.json | 3266 | c77fc638beed7bae08d3bd375d96ff93a79d05f08e77c544ab787193c3a2651b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/stills-list-r4-1-knight.txt | 2596 | 3d4a829b86d4352c2db96027f167b811a89d0742c33946521b888c2a9c67c86c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/stills-list-r4-2-mage.txt | 2558 | 59093d71f1f71c73a9599031f3a29ec6b6f55a65e50311d182e95ae4964b1e0e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/u1-drift/status-after-restore.txt | 784 | 841defd3c4c41f62ee396952330c87039ee2bb51869b04c0d4fe1850dc00dbd5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/u1-drift/status-before-restore.txt | 859 | 48f8fcdc631e3331a2eed7745da6a086b9276ce0ae40bd34b3a2fe9f1347d95c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/u2-drift/status-after-restore.txt | 115 | a4898b88b69c3c81e9dc98fc4e2d8a479b8000c15ef24b4b92fae60e5b3527b0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/u2-drift/status-before-restore.txt | 190 | 805cf044bf72aa50cbe3534cba4a4d2a8a9971efb1b35aac0d2fd4e6b0e98a8f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/u3-drift/status-after-restore.txt | 195 | 654e030c655fef5902a21c6fdd46dfb64d0a6d0e92096d6d8a20b7f68aeff671 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/u3-drift/status-before-restore.txt | 270 | 836c5ddbb6d03ab417fde655927c22ab7f4c90a5d4cf5e129f27afff8d063f8f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/u3v2-drift/drift-stat.txt | 341 | ccdf09b8d0bda645e443cbee6409275b92dc05d52066a1ec0875a611dc975a92 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/u3v2-drift/git-status-after.txt | 298 | 4fe1b65aee81bb1a1cbfa66080346da17ae2ae6329fbe95deff09db1772b8e0d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/u3v2-drift/git-status-before.txt | 566 | e0cb08bc94b84acbb0b19306cde47c63bd8c9b6b2fee57a7399f56ace5f565a2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/u3v2-drift/restored-at.txt | 21 | cf438e711c7d09404e76bc60f7c6ab0fe043744171e14e46c924894f8a693f2c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/u3v2-drift/tasklist-before-restore.csv | 12617 | b4fa1f1e9909231a6acf8f369a00421f3346ed299dbd0f8164288c0577edcd3f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verifier-close.json | 319 | 6bb13cde104ff6a4017ae46e8fd1d67501066ff71919b74164151359dba91c95 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verifier-msg_0acc9aa0534c-worker-done.txt | 1034 | ac75fc2d14468069e7d450eb5f8cd2a4f41c26a8c9a4e15566605735203d7177 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verifier-msg_e26075591d6e-mismatch-blocking.txt | 672 | 0dba52cabc89871bf374f7100bef95f0e303a467db73d9af964a5771c470f27c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verifier-pane-read-final.json | 2746 | d401f8424c5e6ec84014d09e30796f3ae1c182898fef054206ba7e48eff93738 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verifier-r3-msg_172f56d2069c-question-model.txt | 835 | 1758767e0acdefcd4d951b7801ae9dd9bd287e797ba04d6ebe3bec4276f70f0b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verifier-r3-msg_9db655702a7d-worker-done.txt | 1141 | e8448f9d7cec12345315f5012a6c907449d402676595049c5dd5a1b2d2248ffb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verifier-release-2.json | 413 | 96f4942749411d8c30a79bcf8e6d1de4b998789ce2d37792fe55bb4ffe1887a0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verifier-release.json | 584 | 1c8a97c286782eff809e52f9703cc350fb6887f033f78c0dbc8ca25c5c593ac2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verifier2-msg_32cde765a1f9-worker-done.json | 1611 | ce62b939bc48e6f6d15443405dae1b635081d5555abdbd7298c4d6792520c2f6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verifier2-msg_37b658e5a6a2-u3-ready.txt | 969 | 8daca72066ff06e3c64e715cd7b4aa19e21683ff4a04fbc531b18be1483f5a16 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verifier2-msg_6ec0ffe63f40-unity-done.txt | 640 | 2e8f0a5f5c07c33e0fa76cb713b8b22ab7a5b7e8cab55b3da787f0bede928c63 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verifier2-msg_aa1ec8df7079-d1.txt | 1168 | 5f22dfa98a9a572d7919f2a5c6154bfedcb7d275c1215ee0e5db41dc6984f663 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verifier2-msg_aa90a7212bc4-d1-impact.txt | 875 | 5f31ed13937962742b4bc147469c0b43df9fa8da9e05fcf91c8551e6735cb1c6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verify-close-01.json | 319 | a74af1bc85d9621e2533cc7da5a9df76d11eb81a9a4b94dae4dce3ef05d1d591 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verify-release-01.json | 413 | b6d10cc015bb36eb3db1be8d0485126bc8a1ac7a8c4921690233624d198f5706 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/verify-worker-done-msg_4f6a54239e8e.json | 8990 | 3b7e103c18aa8cd1d8152d235850b3b3174b2454c2af55f699b912e8f289d463 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-01.json | 7699 | 90968179debe3d2874fe9f6c23644770148f428141f2ff2b0cde28e37a04c55d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-02.json | 4045 | 85728340b89f70cff035f2b20dfafbed1cbdee9227a849fede762a84bbe9ccd2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-03.json | 2608 | ecd11693b1fed43f777ba03586aaee5db20928936627c31f5c4fa6c0cefd0162 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-04.json | 1955 | f1867e73742c786c151723ef99227f2010c2d4add6f61d73e37c5ed214b363fc |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-05.json | 4636 | 93d05c16d617a0b473bbc453d73cd01f5a916a9689ee323a728499f6d1542509 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-06.json | 2598 | e509ce3d6d939d899ed41808510749eed320b7631f441da1e96ba42e9fb656c2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-07.json | 4964 | bfa631a4edfae6531f842a23294953e52455a165118bf526b90acd9a4e7e2ff1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-08.json | 1488 | 32e97a455beb2f7f57697fa87c61d808f3b0e230443419e1b7427c8e57208a60 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-09.json | 2506 | 0e6e1327bab0a07fe27eb39213cb5b3a13ef9c433827632f4bdcc70196af8c7d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-10.json | 1496 | 02532f710921e0446ee5bfb165365c6efa40719a0d65dc03164f1dec2da70ff2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-100-ack.json | 479 | 833475624d0d40860a6a04e0ac61f7c4f174cf727b460ac87d230bad62bb60e8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-100.json | 2801 | 1446b0d795cfc7ca764361a4edd7ebba768a91e45701cf36d530a25896de1d24 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-101-ack.json | 479 | 3b853feb386ada886cd10006a7e0a416a3b093adfd7da53ecc9c0cf914206949 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-101.json | 1869 | df5b944f8b2b3afcfb21d666548e163665587672471e3c53e24b9cb8d73a1a69 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-102-ack.json | 479 | 99ceba6ba2f280af06675792053ce5c329c0eeb985273f3ac9effedeb023226b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-102.json | 1776 | 4dfa5b27201f3fcca19508e217a58ba0670456849365c677fcdefe2b88ecfaec |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-103-ack.json | 479 | 571a36cc514693ab42586be7f714a9073a757cf3b89b89f9033195e02a7c0036 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-103.json | 2190 | 65be06c7f3ad40c80eff401f9e43c5ec1844c9b88b08df5ef154c01281c245c9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-104-ack.json | 479 | 49d54dbc56184c54caff0af50c5efb2d1e777c1f3706afb8d78e61b6459f59a1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-104.json | 1561 | 7468414adf8d8ff1a9e1b9a6be1b226eaaf662dbaae5ebc73a8caf262686ec66 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-105-ack.json | 479 | 771b4fd962c03c2fb6474d9708d69cd500d8032bf309c31347039be96a1c05f4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-105.json | 2624 | b2542fe1a11d1f0582e256cdeaab8811aa837a8f2646e58cd0125487adb0bb5f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-106.json | 4996 | 8e7e47b29c33f311799eea4c935a95a8fefb7138fdb9e0122994be490776ebd5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-107-ack.json | 479 | e8283e298f523c48412055160d91019c2d65c07d28cf1d03174491731a86ce9a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-107.json | 2411 | 489af129d3fbc66434cefacd7697a9c19a1c67100122fea9151142e213d3d0b0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-108.json | 4996 | ecbb3d8aa7be8249f36b418e735ecdc681a69552667ee4428f2e26e5ff7f0513 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-109.json | 4996 | a312f132b5ff0fb060b1f1b15f7e3175a27dfe1fc6a09b18afb6b790757dbb62 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-11.json | 4863 | 9a9d45e0167e9e2728dc72aefc83eae8958ad26eeab0933b30660665398777af |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-110-ack.json | 479 | 14232d90610c0cf6b71a5e616efd64588f0ada929db13120e26996f37aab47b6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-110.json | 1776 | 476f1534605d597e3d70468f88da4673f4bf5fdad44330587c9635347f0f864a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-111-ack.json | 479 | d39f6e9446fe25644b2f9048aded76db40b2ac61cd61f00ce9324d0d3144322f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-111.json | 2155 | 2bd6241178ba7edf4cbf8c49223362fb5c76196f6fc86400615696f9fdd35140 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-112.json | 4996 | 72bd7bed780f0cd8773a859e29da3385c19dd05c1e6d5afbc3cb8ab9e5a3cc75 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-113-ack.json | 479 | 60795d2f68685945f9ed7ce430dee893bc5bedf77e63030312dcbc863be293c7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-113.json | 5054 | 0bc88b3eab5127eb7729e530f969e0f75015ab6a3d7ccb122cbbff1afc204714 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-114-ack.json | 479 | 07508c6509f6088792ef1d09013453cea1b62c5580a6b4c08808d15d5716d967 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-114.json | 3425 | 2542094746cd7626f8be849c2c700126cf3cb853e83755970b4fd86ab355a91f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-115-ack.json | 479 | 0ca4b3a306ba544e91dbb589dba8b54bd5cdd921936599f61c14f9f6c56b0066 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-115.json | 6276 | d72d98fefd1e5101b602c39545d86fff3d0de172ede021c546fd8378c02bd485 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-116-ack.json | 1122 | a506e318c7907dab23a38ac9cbdd83245a05a2fb37ba05539023d36f767be5d8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-116.json | 3050 | f5cd301d267963e625d3a973a64247c2f5d2d99714f3fe8eb5ed89d20ff1e760 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-117-ack.json | 479 | 5257ab4c3ca4569451a88f1fb24ed66a6bd1d9e4cb4619d07adbda5e3d690a38 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-117.json | 1102 | a49918820fb258f1cc5fd7d82e0d2b55d5bb440216568f9f9fb6344e2106852b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-118-ack.json | 479 | 5f2423e0c62d2dd7797c1705ebde681f4e9b4e0010becb89aee2d1af479f98eb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-118.json | 3152 | a4d2b37a20c34b14c59fdd4f102c055a0b327d7f4655976e6e74bec172a4b210 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-119-ack.json | 479 | a0c60dd0436a17f59b9a409dc485dfbdeeed1e22744caaa919a1b1ebc660d3d9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-119.json | 2829 | 2dfc39aab4e566f05808cc4d099906ec6f5b2d1aa09c264d0f6307ba9d86fc5a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-12.json | 1102 | e43ca96c72ec77b9db4cdfc9aa261c94ddddbfd7e8e9a8dd5097d225da0352ae |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-120-ack.json | 479 | da51d5f9ba5fc32fd7e515bff107ed21d0018901395e19971b04254e70ba3510 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-120.json | 1998 | f7afc4c7c4217285eeba8f7274f68cc0936fa7d4f32c103d7802238c73069dfe |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-121-ack.json | 479 | a4d655f70c8b9e060aae2ce3e0fe03f763304fa089cf8687940652e1a035d3a5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-121.json | 4755 | ecad565161bbadf3d4c3f3df582ee191aef1f7ea6756bff136f3cbeaf3c91990 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-122-ack.json | 479 | 6d5d7d723a8755e6c4e7467506ec0e88411cd3a901a79aae545eaeb337bedd3b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-122.json | 2370 | e00c0a00f3e6ca708ae44fe23c91701257f97b62ac43df8c6e91fd2405cc1313 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-123-ack.json | 1151 | 0e3337bbc6b27ba1cdb435b5c01998ee4880dfac22f1b1f039e8b96d839a7a64 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-123.json | 1943 | 68ce7dccb61add56d8cd172e8b21423022e0e70320fb415badfcac8bbca5b9f8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-124-ack.json | 479 | ecde3b67c1a196aa264d922ed22cd83999ac6c03eebd19ef7a59ee2690b1d28d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-124.json | 1131 | d244a42fcea4f36415bbba97c4e610c9ebd544b438cf46dab0d53777eb6a2b83 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-125-ack.json | 6927 | 7a74b3eee83eaba563c7699a99f5bca797244d38bb246bae9f7b4ab7c0647a9d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-125.json | 3511 | f39836eb6722d5a70bc9d8eaa5a70da337388aea5a8b2453c251c8e0f62e8e99 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-126-ack.json | 479 | 9e0697d313b179d3d21a26196028f89d8d50b0c3967a5ed89e0d99930445e45b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-126.json | 6907 | 24e7ddcb6cfc82998341afba03beb6aa9f473b8ca83dc5e505c9bf9d960ae751 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-127-ack.json | 479 | 239d05dc27f773b91d99ebaa93ec05983a14a1e81142de3c41b98555484c67ab |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-127.json | 13023 | 50aa4d9d503c959a3e20d6d6195282c0c87949af8908bcb88dee02f1a929d706 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-128-ack.json | 479 | 17511e6c918c71018180dff1e6c26acaed89c82a71af9c35c1ed32cfbf46d181 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-128.json | 4273 | deadfae807a926ee8fed57ebf7056d3c8bebaf6849415328f792e59aee95df84 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-129-ack.json | 479 | 2573098352a9212f3c7a555932872bec55211bd9dcf09c41714de534e3acb39e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-129.json | 2608 | 799e7ec2287a898a4bb4fc9b6d71d682c509586a9e583ab4f203a7c897ea9080 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-13.json | 5129 | 36abda1e65ec5e9f043c66e1dc2f4e21322ab4c7c7daeec32d77f1d84485be1f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-130-ack.json | 479 | 131a712703ddba0368993ddd16da17da1bfdeb8b860b4d1438efdd190d32a5e2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-130.json | 4565 | 0df3ada0db0486be12a55296e5915a807639eb4ffb6ab1059475ab7b8064b5d8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-131-ack.json | 479 | e84ce851556553070fdd759704fc6e45f8ad389ac1f7ff2443d283f9967ab301 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-131.json | 1770 | 8aa454a9d9a853e5c4c2107f048e8dc787f4254e31f2d1634ef4985e31447fe3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-132-ack.json | 4135 | d2737f970a791e51ced62498377b498e8962f935116417d8650a629cacb476d8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-132.json | 3377 | 30a9e3c01279888c1ebadf2f52f15885b1d92b3419e73fe66fa687e88cbc6fe4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-133-ack.json | 479 | bf4aff46f805cd1bf125ec11b099dcb1ce17658f8eae77a58faa784f582c28c5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-133.json | 4115 | 6a898e0e54bc4aaaff45831a1ca98687e26c43ff4439c5bb8933208dd3a7ac45 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-134-ack.json | 479 | 480ded66239e9f650f8fd9a7ba60568f3a356078a7e059969a57accd4bc1aeb4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-134.json | 3621 | d20ad4e4036876feac1afc8a35c78acf04d8bf8aff0c20550153e48004cfaac5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-135-ack.json | 479 | 71033450cb5c77a13d0f747a9b788624260f00548a96e8662072939f67f8cc74 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-135.json | 2965 | 67d0def2f1842714cf6bdff6a87609eb1946451024484ba71ed5359a10512506 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-136.json | 4996 | 935cbe6c0792955ded01e90d97faa7404e99d9689bc9c39cc77e4a80c1bf6e15 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-137.json | 4996 | 18c5e98608b8bff315ecf5c8822109f9af5bb912df97b6ce874be578fcfab673 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-138.json | 4996 | 143b72000765c43e43fa72136e709ea22704adc73d071eb950d91e397af3c5db |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-139.json | 4996 | 07d0aa746782cc4bac13573e3721a369f849e1deb05ab10760e63480e3249b35 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-14.json | 4996 | f5a536ce04a2454ab85a7373822d3d03e446e59ec84350500ad86919e33b8194 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-140.json | 4996 | fb7ae565850d4194ae95893ed1544f35ee7a7b830575f133d9655a75fee3795b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-141.json | 4996 | af6003201908c36c503609f0a1ed658a2590c663859946a041146f8632651f8a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-142.json | 4996 | 7eac10b551a903d7773adc40ffc3d3131e5a1f9fa1140efa10ec4577d8ee0f9c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-143.json | 4996 | 3c9db6c5026b656293a8a56a69e7fc3ba5e4c4d02c13eba93db1f153c7e5966f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-144.json | 4996 | 32542339079571ae44c4e56d2c4cbd4bf335744b22d8827f87478ea310b4408e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-145.json | 4996 | d310212631507c4c5e83477821610c9529c65d22690aabeb42fcd10fb635cef9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-146.json | 4996 | 1faebac1d34594f317751e5f076123ee3df822f273270a6e6386df4bf542b904 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-147.json | 4996 | 40538bf6a1928833919f1e570578e6ee8ddc9b719277e62f2985240c2b326215 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-148.json | 4996 | c7d9d64cfa7c47cdf4a31462a6aca892f96aa94e2873941fb3739a9bca0d83d3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-149.json | 4996 | ee8cc20f38ccc2d3acd2313233be47ccabd6eb2b058ca0604cca4fb0327c7354 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-15.json | 3081 | 34f87cee98e4ff14fe9bda96090d5170cb38780a8ecbe3301a10a93eb7684829 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-150-ack.json | 479 | 57a14bf5c0b36cf1ee5815e32f16bfcdb7756fb6e1bbba6aa33aa15bf735ceb5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-150.json | 3720 | 2d1d3e040f838f236187667d59352443ed361d50adf431dc3e7f8a7b3b67c45d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-151-ack.json | 479 | 7a05dca59fdd63b24158b6d83ec902063b0b1fc7ea2a76d5d4516a8783016277 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-151.json | 2624 | 8cfede084f2ad6248e30b227af6382836bfe7cbaa86e4f35a7ddfdf7bf317237 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-152-ack.json | 479 | 36cd6032a878cd14406d53fe7abc80878cff09e0f8989fb448e683b4cae2f7d2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-152.json | 2027 | 0a3e8361c399a150dfb6b2a84156b511c6a4658efaa39b4299c3f8aae0deb814 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-153-ack.json | 479 | f0d0396644855291cd508df956f6d09d116e9e41c9951bb66693d51c3bd72bb1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-153.json | 2069 | 732d06344395a56cc3390411d80a62c1f49c03d8df91f1db57b637eca4d8ddec |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-154-ack.json | 479 | b0843209db55ae038f96776ad971cb24d332221e78979809713be00fc732da87 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-154.json | 2581 | 3278017a7a7e873436bee952b2de723159fbbf82982a6dca8cfe42c30ccfb83f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-155.json | 4996 | 16d815f1301a5861e951dc868f23052b189d3d109e9972ccb362dd6be3f56cd8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-156.json | 4996 | 1ee308fa21c98e455ed3eea0efddafc0fc579f6d0e827a622d3177721f509fb6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-157.json | 4996 | b0e8abd8c215eae6bd29ea2326f85e5c03e6a68ee0642ad755d0b985ffc6cd64 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-158.json | 4996 | 6da87334d5f820f29e6d478f3f7038f681cce52de04af32419c40356e742b738 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-159.json | 4996 | af18639c0916f00d528cacfe4f1f1d12032477112fa7cf29944d758b63cd3657 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-16.json | 2226 | cd9b46613da1d2fdd0ee8e39d7fee6bf36f10f8493a4f2283276c161887a4989 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-160.json | 4996 | aeec46df4f5210329e2e6a1ebcced7e1f562663c176dafd5b340090b2a7f0302 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-161.json | 4996 | 593ffdc0a902971a7c499a747e78bffeed4179f1b03c88fc2d8a7bb801c73eeb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-162.json | 4996 | b222e1bd302421e7055c7a9134222b49af30254f2d840670a6d2e063d908658e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-163.json | 4996 | 88e549a3808a84d0384c1cd2ad627171d411d9e3192f493bc997e0a7c5545f36 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-164.json | 4996 | 6361d3ca3195ada9fa9a96c6692cb3ab7b748547879a66f18cc6b9e14bea02e1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-165.json | 4996 | 1cf5669af6577b647ca16efc6d4167d6f95bd47662af8cf9972d83b014c0e681 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-166.json | 4996 | 01ad25962f8b003078fd195443f7bd2b7a5ae7b3dfef75a2cd5971f3193a9862 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-167-ack.json | 479 | 0fe3c880317f31cdaea368a8efbd8a65f4e2c7e4b609e9de735ae913da0e4858 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-167.json | 3717 | 3e3abc6818efe77399e0f09cf884b4c14a386cb93b0a00186b91d00df9d5e05c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-168.json | 4996 | d622ad451569d90301be719432178d0516a2dfafe3e805b1eb5b35d92ee8be7b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-169-ack.json | 479 | ac952db2885c415dd1253f29466db0b78f8face0b2e7e9d11f0e7143bb0fbe44 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-169.json | 6373 | 86186aa6352984e4fa248f49a28173cbf7aba7532d43a84705338a13a21dac9e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-17.json | 2703 | e165a80e014c889cb00ccaca0a453aa69a6b811f5ef518d61f90fb99413c21e9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-170.json | 4590 | bd8f8406a071151e744c28efbee58ef7777ad83c9a4c8c624088c9918ed5fa86 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-171.json | 4840 | e5fac52015f8ae005ba12629200e3748d76265613ed6fe03459dfe2ea81bf451 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-172.json | 4590 | a0be17292bd57150e440222e9814620dcf76d3de78f50c9fcc77df5e4daaf6ed |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-173.json | 4590 | 544d3f10fa28de8d117e00e7eb796b80d7dc9ff7e593baed5927b91af51539d6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-174.json | 4228 | 9479c0dfe1575590908ec0f055e6c5f3d3370e4a2ef8f515ab27a3bfca752914 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-175.json | 4590 | 9d9b1e9babaa121976addceeb39f3867a233fe4f9e2804f89e30e8afeed25691 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-176.json | 4590 | 79ad7f4ce358fdea13cb7b13786778d9e4bd01f9dcec88bc7b4a32a69507902a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-177.json | 4590 | be4d89a4a7fb160a9bad58c43794fae65378c2e2452296782e185dd444bd467b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-178.json | 4590 | 72a6ee313031fd1e29d394a4fb30cb767b64ecf9d4ed46c21bbe7dec7b1adc5a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-179.json | 4590 | 07be897e7dc16e9d51c07b08ab57a607234d2d3e84314ecff40c0c011fd03076 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-18.json | 3356 | 44d8da3185b460af7838b03ba3ae93e97c5d0e69cfd8e1883bb8224d6067bdd9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-180.json | 4590 | a038dd5ca7bc5be82a75c995415f808c5c1bb8c825bd91ccdb1f34d770b3f506 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-181.json | 4590 | 40af8d9448921e82f0f4356f7efdf7280663c9561bcb96c36846b002d0f6f76e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-182.json | 4590 | b0ee255a35de4288b25ba58ec7085ba5ed3445e12653cf80960ce03e2fcac895 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-183.json | 4590 | f3f5d6861b949707d0233a285753fa0ca7d2eaa5020209a8b055f28fabb3007e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-184.json | 4590 | 6db9553e75b0503b0529d4dcf96a1ad94e49acc42666d860501514804a1f70bb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-185.json | 4590 | 7c792f5888fc60a5b046aa3ef0ff5e8d38a4bd9154b81d14c86fe622ac53e59e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-186.json | 3306 | fdb1078b7734078408b9d83856e7d0f7737f38f992d3c932d3b5f3372278a844 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-187.json | 4590 | 698656372a3e99862f65c1b8bb31c15e3e768aa21feda806721daf1b0a7218ec |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-188.json | 4590 | 9aa5afc998aac84ee608a5c7c19e65c0382a600fe756a8cc2ba71daaeabba0ac |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-189.json | 4590 | 63091f16823fbe345201f4563de5567132cfd5ba9241308cda2bf869cd200b6b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-19.json | 3488 | 55123691fc8cad7b63426a2a888165d7fca4e0dc8209458d0cbfb3a6d956e512 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-190.json | 4590 | c684bd213ce451184c6988db910ac216c90b887c2de1e8fee5b2062d66a2a37a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-191.json | 4590 | 42b57ca67700552ce764869c4cd00c4764ef6dffef443f46b52111653336b2d2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-192.json | 4590 | dc0d9e2167005aa1991d854d6ff62a7345d2bb93bc02da96e7e1daf66419072b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-193.json | 4590 | 9e4a1bc66020687635b9cadc624cb4105750eb19b303201374a161d280e9610e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-194.json | 4590 | e0fa91edf802db65d9b679b8495d37de9cd9dd6c1f77f9082f5bcf84fd4b800c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-195.json | 4590 | 0f733ff9149d372c2f59780acafc02c24881292433cbbf374e567d43e40e30a4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-196.json | 4590 | 78ee1516241e97f9e6391f1c82a1a28cd82511a52a419d3156314aeb17432111 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-197.json | 4590 | 9b9730fd34024b158c30091ba9c338df0e4ddc7593cfce9a690242e013987bbb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-198.json | 4590 | 016e350a07b15aae05e322e4a056c2333e7e70564cb1e31a448c30dc36e3efa1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-199.json | 4590 | c21163c3f3b41f9ed0f60c2df43b0b78560724d7d31510afd865a7c3fe1d0070 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20.json | 1852 | 48bec58a0da5cc94a1e3906d5e4e5b18273675380f98c6de997b13cd93256827 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-200.json | 4590 | 4f10b9b3a8723156e78d03fb8d7149a85fa9fc74200cef55fcdb9004fb8f1b1c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-201.json | 4590 | ac56600365bc4841f1fbeaca5c78b828a7f36787b85784c6e73de37434d2557b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-202.json | 4590 | 3bbbe1104278ce6acb7cb806041cecb289090133ab3afaef194e594b578633a6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-01.json | 687 | 6b9ee1a049768cc828332b56b3f48a9566a02f85534b75ec91d2d2b27353edfd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-02.json | 1006 | 971c3b01f2659b57c36f6d2a1da1bf2c8f1a27b8d37d8c9b0ee52870a6ca0b39 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-03.json | 1286 | 7ad8bd1355d693228c45ddbaff4a33433ffda4768b400145a14f3876fcec8c4c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-05.json | 1201 | 183b413adf77d2241f52072b66c275112d36d8b6a1c43e41bb9ead3140ae5aea |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-06.json | 3054 | b45b523b253e85022bc310152b7d0237f633fe616709b1da2706aba0b3f889e0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-07.json | 4704 | 7f101e15e9535a3745e44dc779ee6f194a717ec88fcc575756635053dc90228b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-08.json | 4704 | af7d2b733a4740290af7ca0d28d11bc9aa838c778a2388694e224f0b222dd2b7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-09.json | 4704 | 1a16d60d4553c8151be52895456450d16890f8c0cc312becc70ba45b03d18b17 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-10.json | 1409 | 5237dcfd95fc610199d7ffa14d743e37cd73803bc72b2ccab80148831e51a001 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-12.json | 3285 | cdaeb93dbb6149d0dd63efa0356ed8b15464415617d89c267a9296f27686d219 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-13.json | 1414 | 1e9ad4fb4ec7ab3ccf750dbcbaf6a8c31aa6a2b854444b59832a394d2f006006 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-14.json | 3369 | fa8a0c88c2e892b0f3521bbcc86b0341fb83a409e0a3eee55b5f09fcdaceb809 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-15.json | 2068 | 5323bafbd63026420a47a7eca2a06136a0dbe24772072cefc70e42d3f0839baf |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-16.json | 2908 | 372a5b2058792e44f3ce1eac27e713c834da15bb8b5338acd07a5281d1366504 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-20261106-rec-17.json | 1754 | cd827d3665708f0e2973af9a0615b1db5f3c55f5386f593e5e81f72dedc80c03 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-203.json | 2921 | c748246f725a8d63c51c67c0f7e39380f4d72aa030389b80c5093bd933c54944 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-204.json | 5145 | 5f41afd074b29f06578fc8b7540169b923f43c705f60fabb648cfdd8be6c6346 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-205.json | 800 | a31e79e2baeab00d755ea0efc58ac0ebffd3b2d1d6bb98a21f8b72e8b8747845 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-206.json | 1027 | ef6120b935d16c03b7452683f329ca9f8f502e2b67048f434d231aaf7f974c18 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-207.json | 5001 | c66302b455618282097bcb342c25ce22a7005365675c342d75cc7372cbc9396c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-208.json | 167 | 15ff93f480c0db454d43de5a6d9fc03ab68f9e9a9997cb952fc434143f6a0561 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-209.json | 167 | 15ff93f480c0db454d43de5a6d9fc03ab68f9e9a9997cb952fc434143f6a0561 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-21.json | 3324 | 3300b7091703eae9398d9a4b86b6faeb3be085d48089e3ddd1db0997ee13a4f2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-210.json | 1085 | 96120c3fb79f8857866cb3a5f047f4f04b30492fe3fc72ee0c594f0803e94f93 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-22.json | 1102 | 5bdcaa47efdadf02564e2e2541835c31c2071df0145fb6e0451e250efa7530c4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-23.json | 2324 | 3e3626c1059af598ff679553ac810d51fec70491ab3f2228fab8df20e298b2fd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-24.json | 1194 | 0f4d6fd5dcea9fb33ca157f9d6757516a26e0dbfa9c4254c9bfedaad202890bd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-25.json | 1647 | 1f9b921625984666ee60983e88d155d207e74a82734f8380a571c5b6fee6d03b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-26.json | 3420 | 07e20659915f38ad4b2ac4fe87631769cd8acd1ee96cfca2c3988ec1dd92bd14 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-27.json | 2297 | 6e8da8fe81ba2fc1ef94b7cad5f63ddae13ae637eb548ea6cfdd3310193a5e99 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-28.json | 3087 | 209393c689ee2c7501f47a40541949e9c7d69b83033b8236086d79f4f485f223 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-29.json | 4407 | ff69a9d874b7baca2cf12801aceb3add7e31ba265fad0f7a3ffdf3235a8b8541 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-30.json | 1366 | bdac94e91875a208f06d58a0eff7b80f0dc937abfa89c6820312b21a7808dc41 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-31.json | 1500 | e69cc184ec9f822b3bd3f5076c76f3816cd7bba82b8389b60be7e6fb92903b00 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-32.json | 1816 | 2f6cbff9803fc4f712af359278050a4c70f6c140dbfd38d3a6b11f91c1d510cd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-33.json | 1510 | bf03c1f382519c01ddebd47510e6aef07daecb5337f243d3b45adae6fda3e6ee |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-34.json | 2209 | 5ab2c72015dea17e7f6135eb6572dfca926d664fbce38fe97589fbfaa991edea |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-35.json | 1649 | 96d55e68692f938e05b2a91f14adc8189fb6cc07b6507c6c89dc59c97c9d0830 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-36.json | 2895 | c0fcea90c5b44b05de05a1a5c7028a8fde0fc7654dd1a0a6ddcbfc7e5079f568 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-37.json | 4996 | 5cbd549fce34b131aa4a7f97824165e191d44a3233f810042df4d1a897b72243 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-38.json | 1951 | ef718ce204acabb399baba3278a06a341ec26a6ee8e9a005bdaa2034ca16bf01 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-39.json | 2572 | a6147212acdaefd57c401450094448a0eef23351b79dc36b6841df1fc2a18f28 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-40.json | 1509 | 0e6092846c0ac026db6251ce3b3219a0a9815b0878b07acc6009d16241a11a5a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-41.json | 1552 | 78bc15f087bbe8b2b3b3a9976748b688fec4b65568753f52fee3f188ff71a8ac |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-42.json | 3475 | 7dc3e5055ca67da6b11534f35401493b8cb2c034ebb2743f95efecfcee364f7c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-43.json | 5139 | 261be5a97e339f2021c3908d0c8d4b09c5829236a8e1083fad6a18a902fbc831 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-44.json | 2636 | 02df8b90449ab0fccc125413d132413021a4a5e169683baf2618b2cfdfe22a46 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-45.json | 2558 | ceb374ab08aae3bd7274512f522f50f34de94d46d753dae0dbc8eac49ee72f65 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-46.json | 1982 | ae1b7b080f289b68a4a97816a292a7f549fbe9318cd5ae1c685426f5672e57e8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-47.json | 3605 | 26f52e54d082fbcecea4ea3cb2c64d57338fac7b93f8ebd0a651fcc4761b4c22 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-48.json | 1418 | 5e6210959c58821c0a39a7e49d26eb3f95757f6e2667d9fa54e3dc6306a0255d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-49.json | 2290 | d67cc8a9c3268bd6dda62eaac06d5f156654a02c7323ed51879e018bd6dcb38c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-50.json | 4151 | 8f9f0ed1b92e03682b42717a3e13ebf717112b1137501af19f6de622c6aa0c1d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-51.json | 2601 | 408ebe075052443d83a1ca3dc7c8778f550839e882bcc23ff18d81ec56261e06 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-52.json | 2083 | 8814d9d36b8196512f889a0b915cfd73bad151809c8c1c035267558f6a8b304f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-53.json | 1295 | d90023a11c25804b639241cf26dac7b8cc56e95d7400ab78484df1d6caa4c0ed |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-54.json | 1397 | eeb08bd68ade2ef544edf2e9b4d6270c0b5ea1a8854155497378fff0e1c1eb66 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-55.json | 1602 | b5593448e5d6c9820865f4eca73756118be530a1a501a2c84acaf574dd35398a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-56.json | 2287 | e166b02edba728a8e299634b3aad371edea4d705e6674179cf2d5373ddbd09b6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-57.json | 3728 | abd5f41467b18f6a79301b227581b33ea27c41b36d69f88765f9dcafa1a29f92 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-58.json | 2966 | 30e7187f000d582460e442fc7214977473f846e7f89fd6ddf41841982f9f9c2b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-59.json | 5714 | 4f113e3d84d4d17a49e35d1d0c5a8239188b422242704ff1d9fbb47d33c788c8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-60.json | 3590 | 11e5ce07856dec7df5f8263340bcb00ab3c6fee668343f6cfbd6194c6e3cdd7d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-61.json | 1693 | cfc9f684db56c95dbfab77ef46aa301c65fb883217b14227a5dc3e9c85f5be23 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-62.json | 1943 | e19eee23fbd83003e124921ea6e4bb8c4e8759494a803c80d8bbf41cd38baca5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-63.json | 1270 | 1aad186c665b3452cd60e3e10de1c735cd6cad76b1b58555b217c3b2054d0799 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-64.json | 10063 | 1220b744d8f72bde61cb45ac542d5f3a2cb7b795e6571e2d6c4d22c238637247 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-65.json | 3224 | 41d402b9209be3070ce01cbc2d9dd928397b3e084f6eb1d1db4f24ced04d19da |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-66.json | 3247 | c2085e87e87a6d526e185a3a8dca13b2ab23e00b50e8da623ef013d2bef686b2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-67.json | 1732 | a46779922d2f9c884094e7446b9585d6dd939ad52ecec291683d5697645bd9fa |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-68.json | 5628 | c361235e6960cf9087aad12238fc6c7edced5f37ded7c7dd969b1d2aa0347fa1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-70.json | 2724 | 91d8fd2f372877ecee65bb595b384c5e7923ae67586c5ee802a80777a41d1cb1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-71.json | 2800 | 763041951bd5ea86cd738db3e5b6546ff42c55ff07996b6583e3c53cd8fce053 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-72.json | 2220 | 90948db3f99774ae41aaf9ab60b4410edaeedce8ce593b6b2718316b56ca37f0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-73.json | 1485 | 645c9084d5bfeabbfdfc13e90b2eb2961c648e343f225951b611a032acd85f65 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-74.json | 2501 | 6754fd1ac005304d2f0df55613c567f2b4ce4f3f9bcc700abc16274d53e288b6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-75.json | 1917 | 40ae8ea656d405a49e8e57b5695056ead7c3307f6c2c748bf63d9e2c2d433aa1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-76.json | 1750 | a2a7e0cc02bcba56cf6f7144ca2d02f85c702f58d7f5c155c3d36cbeb858edc7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-77.json | 2132 | 80ade3ef1318a92460223ae0c335999293600546e71428baef1d1ed3bf6f3b72 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-78.json | 3679 | e4ed6f6220fdc9e2645a5e6b73ca1433360cea78d72949ce9233a345b1818082 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-79.json | 2543 | 640db721caaa5761c413c753a8d1e5411863a889e50a397bb17d8ff7bd6ae998 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-80-ack.json | 479 | d5b6679eb0947b7db2f008191548059ab132504963472a6441dcb1dd4f044ebf |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-80.json | 2580 | cd2c5f62ca381958f8dbf5fc8ecc67c235a4362fcb17c0c3ff1d77e121ac2eeb |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-81-ack.json | 479 | 7fb4cd9b9b1aedc4aeddaf0076fde49d1388d1ddb111355a0aa088bde442a82a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-81.json | 2003 | de8ff3750e9afd9400941267c34874ce221e90eed93b037c1e62685607c9bb3f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-82-ack.json | 479 | 45c0489d9a257d56b43f969526121443b06a9f9d0ea6d145555ea27dcaac6b52 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-82.json | 1615 | efdb6580fee7cdd3ae5c19b17703b09c4331638d4ec9a520f1fb252c36157a8a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-83-ack.json | 1114 | 0dada9720f934ce5fefd9ed03b57286f06a05748dc021c5cc177f8ab3370bd30 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-83.json | 4414 | 3e0292c3bee432a446ee41db2c090df5149a1766d878e612103ac2735671e18f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-86-ack.json | 1119 | bf11e20c04bce4384327ac939b68674b226ada42af366336a765433f8f3dd831 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-86.json | 1094 | 03b36fb7620e57ff259e26404d34ff9c5e85ce04af829054b354bf62e99885f5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-87-ack.json | 479 | e6fd1bec1ca76540a3241eddec63a1561e4428dd657c48a89ed618adc2e13273 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-87.json | 1099 | 7cf5f8e1887cf4ca8403722082f67932a18322f1428a92ebd3b5901fe0f2604d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-88-ack.json | 479 | e972cee1537b916e494ae635e18de7ad1f3098c429977d8c840453b252ba8497 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-88.json | 2635 | 95c224093813e07278627695dc0f0a920cc3e7b57bb942f779b57c181eeff404 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-89-ack.json | 479 | 41636f2360e2003b2c318df3efbf3ab7b6839476cdd6f5a61955597cea6053c1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-89.json | 1329 | e11541090930017668f1f3a992b12002fc8d9ce2c5ddb1246aaa774923e0e9fa |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-90-ack.json | 479 | ff2e59072e765e6b3f8d1664351679d6e28461ec37e76f6613c86175c649f912 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-90.json | 3098 | c6544a888c9570b70e6bdea1ffb1266b704893f183c9ad9fa36382f0f725c0b7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-91-ack.json | 479 | b8154064a21ab139f43d859c8aaa3357593ddbf1236b78eee4616185dd865f2c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-91.json | 2665 | a56e037021ec17717d2afba05f67a223cf2d9b22fbe26c09b326fbdb2458d8b1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-92-ack.json | 479 | 8600a36755ed2e54eac61b1fd4a5f6728de1d72b55f6ddbf751c86b223ca402b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-92.json | 3285 | 8a45ea23cf1eee39c4043c7ca80947517929bc8762f39af8faed2f15172718a2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-93-ack.json | 479 | f92c8550e2cce79aa034d5f1bb35ca42fa78ea5671d121f12fd1a7ded52c878e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-93.json | 2035 | d5815a24de9571f52bdfb72314a2ee0eb272019ffabef71b4792bc724065f3b1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-94-ack.json | 479 | 0ac1e49cb8f4c69738cdd151a5333d0bb205b4b862b8ecdbe05212d2b0fefc3b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-94.json | 1383 | 415b6b4f94df9acedb971d8811cd1a5685b65e47dd2a2bdee2b02fe3281301cd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-95-ack.json | 479 | 25fcc6ea6e9464c5f17d3f47e68c4963df3f4a358fd003ec5a3bdb6a73ffc638 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-95.json | 2972 | 945df2cc9dd88f53f4d6cbc337e186c51bebf54a9d32989ec5801421a48da14a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-96-ack.json | 479 | 370571ed13bdb4b65c99e116c2aba4be1259ba0838669b23ea1c96ffff69cb1e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-96.json | 2895 | 5d6a973f31142f535937c5b9fe61a6a881d1caf83e30812a32333fdbd1178b60 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-97-ack.json | 479 | bcb6c90ff8b62617ba00c1dfa29f462e76ab4ffd7d8a35de4dcc5317e6af1655 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-97.json | 4592 | 545bb83856d470462ba305ea5d47f40357448aa718c73a315759112c4d82a49e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-98-ack.json | 479 | 751b108b097259919883fe14bf82a618e08a3b83c6320a45a0f0666d65e85cab |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-98.json | 1494 | b3bf3460efd82cedf64a0f9ef4bd746669da04db2a8219d8b6539adb0ec5569d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-99-ack.json | 479 | 36fae42cb6af93ea993c4e044f0b58928060bd330d77842f2cd1f8e4ee53de02 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-99.json | 2271 | d6d2ddd77f18ecc576c9bcec64e09eba2096ec9b34b8e6d43f6cf73cb46031d1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-01.json | 2267 | e2cc3bac23bd1692ef5cd3f8ca5f4e88b88d7ad8c5f749d1675fd66cd782e60f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-02.json | 1989 | 2a87953c51081902ed24ecb4e91930a7f38453a5ee6d2ce7ad6d0e582e7e82aa |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-v2-01.json | 167 | 15ff93f480c0db454d43de5a6d9fc03ab68f9e9a9997cb952fc434143f6a0561 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-v2-02.json | 1725 | b812960b12a6c4dc18147ceff5fc8cb6f80dadff96734d5dd218d593b9b0023e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-v2-03.json | 1317 | 6c95a49621085ff3ef8f2a19fb30430d91101a8d26892967f6277bf30b677b24 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-v2-04.json | 1167 | e2da8aa5ef6083f7f426909ff760c4d66d79a12e6694bff3179c38ada70b6bcc |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-v2-05.json | 2045 | f8f0ba43ba94b941fb323c0a9b0d9a2e98fc4375eb51164094bde1406073daa3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-v2-06.json | 707 | 127fe78f1ae87db25a9cac9e7bf477e2dde4e2bcb06aa908540fbdd43b8ea159 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-v2-07.json | 2259 | a1ee17d5c5566cbda9e0f2d23fd21ed9206747d2101aa5b278c26606a7f15fda |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-v2-08.json | 1211 | 2a8abecc429f2482925df697f0be2cc8a6f650e65e438309bd0fd791ac7e27ac |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-v2-09.json | 167 | 15ff93f480c0db454d43de5a6d9fc03ab68f9e9a9997cb952fc434143f6a0561 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-v2-10.json | 1047 | 61a42814f53de75c6c0a0fe274295566c96c6d6c4acc7531fc89bcce1e7c7576 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-v2-11.json | 1367 | 261f9cf55e7a2ee139804d3385c283907233a14a9e4723d8a3afe060cc12c214 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-v2-12.json | 1159 | 85cc73c92106fb38b6c1245458871822caffb4d5b567c2f4dece0410e18e2cf9 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-rehearsal-v2-13.json | 1033 | 617165d88e640f8569f3937ea59f7c10965cc255a896dc91a1900b10001e15df |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-verify-v2-01.json | 1878 | 83287e09e93d5e8ada68a5219f3c8cee6539d4f1a2935a05d4c0e940621efb28 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-verify-v2-02.json | 2238 | f42bd002ff6887ea1cc03e551f2faa0fa61ad63205bf3a7d608b9367e3ec8248 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-verify-v2-03.json | 879 | 9d8abc24e79fbe04ee17485452535de0fe001e9e1a477040258fe8fbe9f97340 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-verify-v2-04.json | 2220 | 78d7bd3ee8c2ce74f53c0ec330f3da663d39b61827d4eb7e014f4e842d24b4a2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-lead-verify-v2-05.json | 1184 | 723c77a65c0054b2a52833f6dd48940c7839d291e2deb75de8540efbcaf1c877 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-main-decision-2-3-01.json | 53 | 379ffd74ff6a869e3a56a9190e3f7de7021f42000ddb2850d6e14ac8939ccdb6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-r3-verify-01.json | 1472 | e527ae113e81054225ddb5f781f715d1df93898b2bb38f21d1b2682e0b9757f3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-r3-verify-02.json | 4704 | 31cfdf50d64b4a97f8c12d709e02bf924b060ca0bfc44428d02503a4c5e05d2d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-r3-verify-03.json | 5040 | 19c432a3d11a0bd38c173d7c27cbf21c07e4b81ffb03083aa2cbae9bbf2417cc |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-recording-v3-verify-01.json | 1840 | 950c5be5b2c8bc9bf46676e869d922939ba87381955491b640a1c2120fd50af0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-recording-v3-verify-02.json | 4151 | dd84e5fe88d8a7625a78c55beef03e3058a8bb94b0ff2cd90d0a889a42658a80 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-recording-v3-verify-03.json | 3750 | a296a9b97bf864480a0cfc1aab9db4f047273fe2e35a30e43af0577d4ad2712d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-recording-v3-verify-04.json | 5142 | c258bdea42e043a7caf08fe21e48c4214670a7f489e078bd26732ce0df281d91 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-round3-done-01.json | 1709 | 97328b4dde4f742f06e284b3fe60c90d18b424d354883e9fdeed728113b3c399 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-round3-done-02.json | 2907 | 4390fbf3e88af796ef97bf5486a8549257a21bc5a400354c4660e314bc8737f0 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-round3-end-01.json | 3720 | c3965394c59cbd88d51c2e306fc2881c3afdf8ff4b4415e8818f22a2ed4afe18 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-round3-executor-01.json | 2690 | 424549fdd86261f3b31b289347a8b14382f23c1c6c977222fdc105662d6be3b5 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-round3-executor-02.json | 325 | 16c84d0a135eb0c45c175c6d61c80cde7a1a77864ee3472257b57531961507ef |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-round3-executor-03.json | 3188 | 9fcb1165a49c7d0bf856b7d3c77f23c4b2f3e7bd0c1e09a38a3584087f8e207c |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wait-round3-server-up-01.json | 1185 | 4a5ce08ca514aec2e16af471f4c122b9d2448882ad0c00f83850ebf6d1f0c777 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/window6-drift/drift-worktree-sha256.txt | 583 | cd51c875529b234ea7297bbf312f8690dd76079cd6764a8dfcc33b8c85a238b4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/window6-drift/drift.stderr.txt | 143 | e939e6e64c67063ab219e326d502ffef3d6e595684e57195c3211b627cd32d2e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/window6-drift/lead-return-check.txt | 68 | 49d954e0b2e4eb66096538125f685a6c2d638c47719455d9b7160899d2c1b9a6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/window6-drift/saved-utc.txt | 62 | e751b7185e0fd5509bb38479e1a4104096c3d304fe9fc5e32826290049f6d25d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/window6-drift/status-after-restore.txt | 343 | 248c10dd9feca0b88cda1eb419d744fdfabbe12e980dd44034e755d9603a16b8 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/windowA-drift/after-restore.txt | 85 | cf4ec4a12d559bb628e5a2ac03241476318f3d28331e5f4a2d903bcc1252b554 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/windowA-drift/projectsettings.diff.txt | 575 | 31e7aaf1de69c44f2f5f38af86cb9c4032b4caddeac71f08dddb2084e1c883e2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/windowB-drift/after-restore.txt | 317 | 3408cc17d192c4bce3236bc9e38247b1f35f315cc5a990e22b05a4abd74271fe |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/worker-list-round3-after-stop-01.json | 6799 | 52ca4722239ba6a1f989c9cc016f0628e7577205fb56126c2f281adcb23511c4 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/lead/wrapup-process-check.txt | 68 | 7324167b838c939bd3c9540a206904069a473223116d2114d9b338f3393a913f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/pr268-verify-03/untracked-tests-saved/EditMode/DemoAutopilotRecheckLifecycleTests.cs | 1448 | b5048c2186f62c8f0fff3abd6ab9fba1b6150a6077e5eb5319462067848cba06 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/pr268-verify-03/untracked-tests-saved/EditMode/DemoAutopilotRecheckLifecycleTests.cs.meta | 59 | cc30a2bda6e2dbdda6de75733c89728c0eb6cc49b71b5dab535cd6c1c64f9815 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/pr268-verify-03/untracked-tests-saved/PlayMode/DemoAutopilotRecheckLegacyTests.cs | 5576 | d049cdd4ad75ea52326cf746a82891c7d513476c9f3409cbf9e519c5e7db148a |
| .backups/verification/2026-10-31-demo-autopilot-character-select/pr268-verify-03/untracked-tests-saved/PlayMode/DemoAutopilotRecheckLegacyTests.cs.meta | 59 | 2744057af2daca4b7aed3c127fe0d4d66eb2a0dc816d8c9422d65cae84b089cd |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-build-01/recording-round3/knight-player-r4-3.log | 71877 | ef5f649da259c1e6248868a80d72c55f9a37a2ec53f3e3e178d217065b477aed |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-build-01/recording-round3/media/r4-3-audio.wav | 36517400 | a09d46c6f7eb9038cc6e7fb671d5385b55761fe2a220ec26568eea0f1c891049 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-build-01/recording-round3/media/r4-3-knight-video.mkv | 21765031 | 96117a54dd5632de400af6b4cc04834a1a5660c51f41d15109f0190d07307640 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-build-01/recording-round3/media/r4-3-markers.json | 47337 | 967919c2b9f18977b30bb56e29a111395f103ec7bb7f62b0886b63fd57813f04 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-build-01/recording/knight-player-r4-1.log | 68109 | d920d30a66788304f919520183f3b3a837c81929fad5b5fd45ba19fc317effb3 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/audio-timing-recompute.json | 1174 | 16f0229d3658aaddb622425db06bcd14efd565bfd86e9fcd0c1dbdb55d0bab7d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/editor-file-hashes.json | 891 | 229a12212cc0748a088215870e76eba3f92e49b312a342370630bdd7fcb07096 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frame-ledger.jsonl | 16636 | 20d5fa3f44d256b66d80562888ca697b0ce007a3c1dffc273ff510983772c988 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frames/bracket-001.png | 318783 | 46a687e6ef5eab4dc16fd5f834397c767a3f904f253503612c4a9403aafdd74f |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frames/bracket-008.png | 879317 | 7a53f520b26c536d9bee9765b295266e860be402896f586a6923530617de1247 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frames/coarse-004.png | 1287134 | a3574f8cc71fbb10541cbfe4a5174bfdf5fa58f4793ffca8562751069e743e8b |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frames/coarse-008.png | 365672 | 7665d0e0f91f44fa009ef87290822b61c1a49749befef172a7275291e123171d |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frames/coarse-009.png | 8724 | e800ce05da3389346497353ec5331028b846472f320d84a99c13ad65521464c1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frames/coarse-010.png | 8724 | e800ce05da3389346497353ec5331028b846472f320d84a99c13ad65521464c1 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frames/coarse-013.png | 919745 | c57bb666652d9015be7a4b3509ccd4a0889dcb97e39894c045f029a365959bc2 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frames/edges-001.png | 339145 | 8d7bacacef6267b871fe6e6f9be8bf1e518f19eb176b23b5a1868292b1dbc517 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frames/edges-002.png | 335780 | 60e1bf96af3baa8bf0391446545b2eb8b2249450b0049e85cbd724c56dbdc8e7 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frames/edges-003.png | 349258 | fda4775372f6ee089b6390c8ddb98766280367ced47b9c2a06a1741bd0595158 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frames/edges-008.png | 362177 | 586ed4b76ad3c6d33438635bd8abac09ab512e01c084919e0c3c37ec1c228111 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frames/edges-014.png | 159928 | 4696a572a1c1ace32c30044d37ca7413e940de9b210f36ccfa141543921d5c1e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frames/edges-015.png | 357942 | 8a11147cf6273647487335efcd872cb110b0b35a8f9dfa95f5e848060aac6c3e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/heartbeat-recompute.json | 3994 | d648ade750663bca679e86588535c35c21da52385c1116459c08923e3ebe7430 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/heartbeats/hb-007.json | 201 | fa0d1155ba3877058300f561cdfe65bbefadd08377efc54d6b680711ea150ff6 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/heartbeats/hb-008.json | 201 | 6ca49b42f4e0a0f3b71e16d675926bbf97376254fdcb3905a2842db8f7439530 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/helper-revision.json | 606 | 8072b609a26d0b29158479993cfb419179c21cba2ab590fbd5fd3929d02c8981 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/identity-recompute.json | 4695 | d5993e635a5943081fde61727c2c4df07e20608b5fbab6878d8882ec47b2c97e |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/player-log-hashes.json | 586 | 97d6f1d32654fdd9b4891eb323bedc830887ee5f8ee1dcaa17303c6034780990 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/probe-video.stdout.txt | 2779 | 37d42be7684a0bcfb59355ec3c917919bb0452742d632aa114519c2e4fdcce43 |
| .backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/visual-observations.json | 9716 | 4f46b1889917cf9178becc37a6d8d1398c351827a72a7f016e2266ffa84cda96 |
| 00_Document/operations/DEVELOPMENT.md | 26434 | ee8f19d07c342932abcfa84acc9be6ba2971ef9bad7ad8ad940ef963ef67e90e |
| 01_Phases/goals/2026-10-02-agent-rule-context/goal.md | 105519 | 917e2ed34ce617ad79b76341cd3e116ef3adbab77eb799a2ffba548f733826c9 |
| 01_Phases/goals/2026-10-03-harness-principles/goal.md | 141991 | 515e8fdde30ce84026479146a282d74c6746e5c93e5b70916c9c0e5eba3ae0ab |
| 01_Phases/goals/2026-10-04-persistence-integration/goal.md | 612339 | 665e04d25de7eb0c4b8f59baec9c184ab53d6d0cf9f4924de6e8aa4188952280 |
| 01_Phases/goals/2026-10-05-ci-warning-operating-followup/goal.md | 81212 | f86375783f9561e24cdc6abcc523ac01f99019fe34948def891cd15cbeaa5876 |
| 01_Phases/goals/2026-10-05-items-inventory-currency/goal.md | 170334 | ddba4ba1d4707b56d050829a29aca48a5d97ec7456a38280c0d6bc0c42a5873d |
| 01_Phases/goals/2026-10-05-operating-canon/goal.md | 102372 | 22735cb9a56cb6d1465f6dfb7df919b91959d2244fca419ede644b77b1030c1d |
| 01_Phases/goals/2026-10-06-merge-gate-canon-refresh/goal.md | 149726 | 671feab927d54ee4de469a8415929da0cd4f3cbceba909d4465afdce4996b663 |
| 01_Phases/goals/2026-10-07-hook-friction-helper-session/goal.md | 99447 | 57ed461a81a3feefe5d36b31789168200191ee326dc57da97ae9cad799952f18 |
| 01_Phases/goals/2026-10-07-unity-engine-upgrade/goal.md | 108035 | b32d954216cfe5fd96dd2bfd6ac0435b0b16397309e625bf1eebda02123ce19d |
| 01_Phases/goals/2026-10-08-tdd-canon-temp-write-boundary/goal.md | 72364 | d0da232551b9fba85c49b5c9a8372dc81df66bf0444a652cc4608981e7a2f79c |
| 01_Phases/goals/2026-10-09-plan-boundary-and-junction/goal.md | 103605 | 9cb393b1414a004a03cee2e99b8e014cf61a940a4dbe2cb245e02268df7c9e63 |
| 01_Phases/goals/2026-10-10-dungeon-clear-rewards/gardener.md | 32377 | 19f957d4b49fbc9807a030906a6f2a6b7fc551ce0363f3968547f8d7a0b85167 |
| 01_Phases/goals/2026-10-10-dungeon-clear-rewards/goal.md | 192521 | fde51a074a3705b5b98fdb8e7e8f2e5dbdd99d7a719379425ac9e9ee5a8720e6 |
| 01_Phases/goals/2026-10-10-operating-tool-guards/goal.md | 66062 | b2d8e88ae8cb0a41e356f8ea6b7f221108ca3352be3fdfff775932ad1e6d8145 |
| 01_Phases/goals/2026-10-11-multi-account-login/goal.md | 147506 | 14de41a0a9ebb26a055969c124ddb9a813af35f26d71506538e8a69f0afc7bdf |
| 01_Phases/goals/2026-10-11-pre-freeze-rule-docs/goal.md | 54771 | 9dad601f7f2c22d47403d022161fb639683f36e730ee170ed338985d39d005cf |
| 01_Phases/goals/2026-10-11-routing-hotfix-canon/goal.md | 60422 | 27a9716bdde5ff3a27e91d81c7a2691e3c88a0c3824a4fb9138a6e0486625858 |
| 01_Phases/goals/2026-10-13-astra-lead-mailbox-hotfix/goal.md | 36193 | 8c451452ac02800e58e7061925e49e05248e0269a59f91e5f5cdf287850afbe9 |
| 01_Phases/goals/2026-10-13-demo-video-art/goal.md | 735526 | ae41078ef28f9118d9faedfbc558d584c135f187ea3e76c7413ed9e34aca2d49 |
| 01_Phases/goals/2026-10-24-character-storage-restart-load/goal.md | 501695 | aab452e030dfb34e9e63a9cbdec77bffd2c9daf58ef7b3c06c6bd106cb18f4aa |
| 01_Phases/goals/2026-10-30-login-screen-guild-window/goal.md | 74398 | 75b3746ef1df10af5cc83b27137c966b79f20e393f6744302b14bb7fbfd4e840 |
| 01_Phases/goals/2026-10-31-db-persistence-demo-build/gardener.md | 34919 | 1eec659f0b218c433ca4a2469860d9037a6e304a79ae584c9ba83538ba968888 |
| 03_Client/Assets/Scripts/Demo/DemoKeyboard.cs | 2129 | 0ff94a4f29830b1fddc9920f83702235751842d712981b020c3cd31d366ad019 |
| 03_Client/Assets/Scripts/UI/CharacterMenuPresenter.cs | 13996 | 3b997a34158fb19e3ae9749dfb08c2a75a95da01f95acd02d31746907f58177c |
| CLAUDE.md | 12736 | 4e2c5636d406b6b5ad2597ebe036593a38ea3cfa9158a6724965071fdaf304ad |

</details>

## 실행 기록과 미실행·한계

모든 명령의 cwd는 최초 메모의 content-active다. 화면에서 읽은 원시 출력은 이 보고서에 옮기며 별도 원시 파일을 생성하지 않는다. 아래 helper는 새 게임·DB 검증이 아니라 저장 입력의 정적 검사다.


### 입력 대조 및 정본 helper 실행

```powershell
$manifestPath = '.backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/gardener-inputs-v1.txt'
$manifestHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $manifestPath).Hash.ToLowerInvariant()
$checked = 0
$mismatches = @(Get-Content -LiteralPath $manifestPath | Where-Object { $_ -and -not $_.StartsWith('#') } | ForEach-Object { if ($_ -notmatch '^([0-9a-f]{64})\s+(\d+)\s+(.+)$') { "FORMAT: $_" } else { $expectedHash = $Matches[1]; $expectedBytes = [long]$Matches[2]; $inputPath = $Matches[3]; $item = Get-Item -LiteralPath $inputPath -ErrorAction SilentlyContinue; $checked++; if ($null -eq $item) { "MISSING: $inputPath" } else { $actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $inputPath).Hash.ToLowerInvariant(); if ($item.Length -ne $expectedBytes -or $actualHash -ne $expectedHash) { "MISMATCH: $inputPath expected=$expectedHash/$expectedBytes actual=$actualHash/$($item.Length)" } } } })
"manifestSha256=$manifestHash"
"checked=$checked mismatches=$($mismatches.Count)"
$mismatches
```

exit 0, stdout:
```text
manifestSha256=f27358916e9284cdc56eb3aeb4a4b393f7bcb4e78d09ad1f75fba92fbd8ac951
checked=224 mismatches=0
```

```powershell
node 99_Tools/Backlog/check-candidates.mjs --backlog .backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/gardener-inputs/BACKLOG.md --goal .backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/gardener-inputs/goal.md
```

exit 0, stdout:
```json
{"status":"allowed","exitCode":0,"backlog":".backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/gardener-inputs/BACKLOG.md","goal":".backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/gardener-inputs/goal.md","counts":{"backlogIds":244,"duplicateIds":0,"candidates":8,"candidatesWithoutReference":0,"unknownIds":0},"diagnostics":[]}
```

```powershell
node 99_Tools/Orca/check-liveness.mjs .backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/liveness-input-04.json --threshold-seconds 300
```

exit 1, stdout(생략 없는 결과):
```json
{"status":"over","thresholdSeconds":300,"dispatches":[{"dispatchId":"ctx_7483fcc14f3b","signalCount":15,"firstAt":"2026-10-31T14:58:40Z","lastAt":"2026-10-31T15:43:11Z","maxGapSeconds":334,"overGaps":[{"from":{"id":"msg_c6e4e5acfcd4","at":"2026-10-31T15:12:27Z","type":"heartbeat"},"to":{"id":"msg_e300204a8722","at":"2026-10-31T15:18:01Z","type":"status"},"seconds":334},{"from":{"id":"msg_e300204a8722","at":"2026-10-31T15:18:01Z","type":"status"},"to":{"id":"msg_da5deee7c021","at":"2026-10-31T15:23:21Z","type":"status"},"seconds":320},{"from":{"id":"msg_65fae185285e","at":"2026-10-31T15:24:33Z","type":"heartbeat"},"to":{"id":"msg_fe55aa5a3257","at":"2026-10-31T15:29:44Z","type":"heartbeat"},"seconds":311}],"questionWaits":[{"from":{"id":"msg_bcc844a05140","at":"2026-10-31T15:03:52Z","type":"question"},"to":{"id":"msg_1db5b54dcd23","at":"2026-10-31T15:08:31Z","type":"status"},"seconds":279,"replyId":"msg_1db5b54dcd23"}]},{"dispatchId":"ctx_4e160b797a9a","signalCount":9,"firstAt":"2026-10-31T15:56:26Z","lastAt":"2026-10-31T16:18:49Z","maxGapSeconds":305,"overGaps":[{"from":{"id":"msg_bf57a59bae16","at":"2026-10-31T16:09:58Z","type":"heartbeat"},"to":{"id":"msg_2067aa014a55","at":"2026-10-31T16:15:03Z","type":"status"},"seconds":305}],"questionWaits":[]},{"dispatchId":"ctx_eaf67d259cea","signalCount":15,"firstAt":"2026-10-31T16:23:31Z","lastAt":"2026-10-31T16:56:40Z","maxGapSeconds":344,"overGaps":[{"from":{"id":"msg_a2280d557ffe","at":"2026-10-31T16:29:08Z","type":"heartbeat"},"to":{"id":"msg_fd4a73c618d6","at":"2026-10-31T16:34:44Z","type":"question"},"seconds":336},{"from":{"id":"msg_1b357c731502","at":"2026-10-31T16:37:46Z","type":"status"},"to":{"id":"msg_374233804a48","at":"2026-10-31T16:43:30Z","type":"heartbeat"},"seconds":344}],"questionWaits":[{"from":{"id":"msg_fd4a73c618d6","at":"2026-10-31T16:34:44Z","type":"question"},"to":{"id":"msg_1e27ae542707","at":"2026-10-31T16:35:09Z","type":"status"},"seconds":25,"replyId":"msg_1e27ae542707"}]},{"dispatchId":"ctx_d0c084e473d0","signalCount":7,"firstAt":"2026-10-31T17:13:38Z","lastAt":"2026-10-31T17:29:40Z","maxGapSeconds":309,"overGaps":[{"from":{"id":"msg_33b9703da986","at":"2026-10-31T17:24:31Z","type":"status"},"to":{"id":"msg_54f15ab248f6","at":"2026-10-31T17:29:40Z","type":"worker_done"},"seconds":309}],"questionWaits":[]},{"dispatchId":"ctx_7a13fa46a5dd","signalCount":27,"firstAt":"2026-10-31T17:36:30Z","lastAt":"2026-10-31T18:18:33Z","maxGapSeconds":301,"overGaps":[{"from":{"id":"msg_f57d3cb0f3a8","at":"2026-10-31T18:12:22Z","type":"status"},"to":{"id":"msg_8f817ffc7d17","at":"2026-10-31T18:17:23Z","type":"heartbeat"},"seconds":301}],"questionWaits":[]},{"dispatchId":"ctx_2f64b6a4037e","signalCount":8,"firstAt":"2026-10-31T18:29:10Z","lastAt":"2026-10-31T18:40:45Z","maxGapSeconds":290,"overGaps":[],"questionWaits":[]},{"dispatchId":"ctx_12f0c34ddea6","signalCount":20,"firstAt":"2026-10-31T23:54:46Z","lastAt":"2026-11-01T00:36:52Z","maxGapSeconds":318,"overGaps":[{"from":{"id":"msg_d384faa00084","at":"2026-11-01T00:07:28Z","type":"heartbeat"},"to":{"id":"msg_262f37806662","at":"2026-11-01T00:12:46Z","type":"heartbeat"},"seconds":318}],"questionWaits":[{"from":{"id":"msg_29962cc7a0e3","at":"2026-11-01T00:30:52Z","type":"question"},"to":{"id":"msg_b18d6acd297a","at":"2026-11-01T00:31:42Z","type":"status"},"seconds":50,"replyId":"msg_b18d6acd297a"}]},{"dispatchId":"ctx_40e49bd2fa84","signalCount":18,"firstAt":"2026-11-01T00:44:04Z","lastAt":"2026-11-01T01:21:01Z","maxGapSeconds":315,"overGaps":[{"from":{"id":"msg_d3ac52679a9d","at":"2026-11-01T01:13:56Z","type":"status"},"to":{"id":"msg_218a4bc9dcdd","at":"2026-11-01T01:19:11Z","type":"heartbeat"},"seconds":315}],"questionWaits":[]},{"dispatchId":"ctx_4064e2beda5b","signalCount":41,"firstAt":"2026-11-01T01:26:39Z","lastAt":"2026-11-01T03:41:59Z","maxGapSeconds":1888,"overGaps":[{"from":{"id":"msg_faf7e5c7b23c","at":"2026-11-01T01:54:39Z","type":"heartbeat"},"to":{"id":"msg_1566a8158fa6","at":"2026-11-01T02:10:27Z","type":"heartbeat"},"seconds":948},{"from":{"id":"msg_354a8c1c7c47","at":"2026-11-01T02:14:19Z","type":"status"},"to":{"id":"msg_c30d5975de99","at":"2026-11-01T02:45:47Z","type":"question"},"seconds":1888}],"questionWaits":[{"from":{"id":"msg_c30d5975de99","at":"2026-11-01T02:45:47Z","type":"question"},"to":{"id":"msg_eb0c6c5ea20f","at":"2026-11-01T02:46:11Z","type":"status"},"seconds":24,"replyId":"msg_eb0c6c5ea20f"}]},{"dispatchId":"ctx_36bd1abc1b36","signalCount":7,"firstAt":"2026-11-01T03:50:50Z","lastAt":"2026-11-01T03:59:33Z","maxGapSeconds":277,"overGaps":[],"questionWaits":[]},{"dispatchId":"ctx_f465132f38d8","signalCount":6,"firstAt":"2026-11-01T04:04:14Z","lastAt":"2026-11-01T04:12:51Z","maxGapSeconds":170,"overGaps":[],"questionWaits":[]},{"dispatchId":"ctx_eace4775d652","signalCount":11,"firstAt":"2026-11-05T17:20:17Z","lastAt":"2026-11-05T17:53:35Z","maxGapSeconds":431,"overGaps":[{"from":{"id":"msg_ce700886e8a1","at":"2026-11-05T17:20:17Z","type":"heartbeat"},"to":{"id":"msg_e6bf210b39e6","at":"2026-11-05T17:26:42Z","type":"heartbeat"},"seconds":385},{"from":{"id":"msg_e6bf210b39e6","at":"2026-11-05T17:26:42Z","type":"heartbeat"},"to":{"id":"msg_ae9e72018c47","at":"2026-11-05T17:33:38Z","type":"heartbeat"},"seconds":416},{"from":{"id":"msg_7d2c0fd65605","at":"2026-11-05T17:44:34Z","type":"heartbeat"},"to":{"id":"msg_133c4408fa0f","at":"2026-11-05T17:51:45Z","type":"heartbeat"},"seconds":431}],"questionWaits":[]},{"dispatchId":"ctx_c729c6bea4ae","signalCount":8,"firstAt":"2026-11-05T18:00:14Z","lastAt":"2026-11-05T18:34:29Z","maxGapSeconds":409,"overGaps":[{"from":{"id":"msg_98a73c751c9e","at":"2026-11-05T18:08:33Z","type":"heartbeat"},"to":{"id":"msg_9262a25be7b0","at":"2026-11-05T18:14:22Z","type":"heartbeat"},"seconds":349},{"from":{"id":"msg_9262a25be7b0","at":"2026-11-05T18:14:22Z","type":"heartbeat"},"to":{"id":"msg_eb09fd6163d7","at":"2026-11-05T18:19:41Z","type":"heartbeat"},"seconds":319},{"from":{"id":"msg_4311198b24f1","at":"2026-11-05T18:24:41Z","type":"heartbeat"},"to":{"id":"msg_763c60af7a4a","at":"2026-11-05T18:31:30Z","type":"heartbeat"},"seconds":409}],"questionWaits":[]},{"dispatchId":"ctx_ecfa4e8b93e2","signalCount":6,"firstAt":"2026-11-05T19:11:34Z","lastAt":"2026-11-05T19:25:22Z","maxGapSeconds":237,"overGaps":[],"questionWaits":[{"from":{"id":"msg_d1962c3c931c","at":"2026-11-05T19:21:06Z","type":"question"},"to":{"id":"msg_215189f0c4a5","at":"2026-11-05T19:21:35Z","type":"status"},"seconds":29,"replyId":"msg_215189f0c4a5"}]},{"dispatchId":"ctx_e5cf2061302e","signalCount":24,"firstAt":"2026-11-05T19:43:11Z","lastAt":"2026-11-05T21:34:10Z","maxGapSeconds":2179,"overGaps":[{"from":{"id":"msg_1941b7ceb476","at":"2026-11-05T19:52:49Z","type":"heartbeat"},"to":{"id":"msg_16cb126329c1","at":"2026-11-05T19:57:59Z","type":"heartbeat"},"seconds":310},{"from":{"id":"msg_stop_a8e574a5f5e21ffa9bb64339","at":"2026-11-05T20:12:54Z","type":"escalation"},"to":{"id":"msg_2b5a9fa8c292","at":"2026-11-05T20:49:13Z","type":"heartbeat"},"seconds":2179},{"from":{"id":"msg_stop_e6fea0f4259b86abd833ae1f","at":"2026-11-05T21:02:48Z","type":"escalation"},"to":{"id":"msg_b3c1ab34cc22","at":"2026-11-05T21:21:42Z","type":"heartbeat"},"seconds":1134},{"from":{"id":"msg_stop_5346099c3c5e17438992cb63","at":"2026-11-05T21:22:55Z","type":"escalation"},"to":{"id":"msg_56f87309493c","at":"2026-11-05T21:31:04Z","type":"heartbeat"},"seconds":489}],"questionWaits":[{"from":{"id":"msg_6a8a2b55ed50","at":"2026-11-05T20:54:34Z","type":"question"},"to":{"id":"msg_254d78fb8d2b","at":"2026-11-05T20:57:11Z","type":"status"},"seconds":157,"replyId":"msg_254d78fb8d2b"}]},{"dispatchId":"ctx_c52c5cb1100f","signalCount":8,"firstAt":"2026-11-05T21:55:27Z","lastAt":"2026-11-05T22:20:42Z","maxGapSeconds":377,"overGaps":[{"from":{"id":"msg_758732679d83","at":"2026-11-05T21:58:42Z","type":"heartbeat"},"to":{"id":"msg_3aa0ad6c26e2","at":"2026-11-05T22:04:59Z","type":"heartbeat"},"seconds":377}],"questionWaits":[]},{"dispatchId":"ctx_aca53cf6fdb7","signalCount":64,"firstAt":"2026-11-05T22:52:26Z","lastAt":"2026-11-06T01:26:16Z","maxGapSeconds":294,"overGaps":[],"questionWaits":[{"from":{"id":"msg_8dc1d362a630","at":"2026-11-05T23:09:55Z","type":"question"},"to":{"id":"msg_d3f9fea52646","at":"2026-11-05T23:12:20Z","type":"status"},"seconds":145,"replyId":"msg_d3f9fea52646"},{"from":{"id":"msg_efc9e1f6aa33","at":"2026-11-05T23:15:18Z","type":"question"},"to":{"id":"msg_480c864d8c3c","at":"2026-11-05T23:18:26Z","type":"status"},"seconds":188,"replyId":"msg_480c864d8c3c"},{"from":{"id":"msg_fd3e7497b01a","at":"2026-11-05T23:21:30Z","type":"question"},"to":{"id":"msg_ca70a8a5372a","at":"2026-11-05T23:27:19Z","type":"status"},"seconds":349,"replyId":"msg_ca70a8a5372a"}]},{"dispatchId":"ctx_e1d38dabbd61","signalCount":20,"firstAt":"2026-11-06T01:55:56Z","lastAt":"2026-11-06T02:35:44Z","maxGapSeconds":322,"overGaps":[{"from":{"id":"msg_11fcb04bbda2","at":"2026-11-06T02:24:13Z","type":"heartbeat"},"to":{"id":"msg_0f40518edb11","at":"2026-11-06T02:29:35Z","type":"heartbeat"},"seconds":322}],"questionWaits":[]},{"dispatchId":"ctx_fda4da83abb1","signalCount":12,"firstAt":"2026-11-06T04:45:18Z","lastAt":"2026-11-06T05:09:00Z","maxGapSeconds":251,"overGaps":[],"questionWaits":[]},{"dispatchId":"ctx_e3d3bf774549","signalCount":16,"firstAt":"2026-11-06T05:19:36Z","lastAt":"2026-11-06T05:51:51Z","maxGapSeconds":490,"overGaps":[{"from":{"id":"msg_6981f9608871","at":"2026-11-06T05:30:51Z","type":"heartbeat"},"to":{"id":"msg_314c821b56ef","at":"2026-11-06T05:39:01Z","type":"heartbeat"},"seconds":490}],"questionWaits":[]}],"diagnostics":[]}
```








### 수치·원천 산출 명령

아래 A번호는 본문의 근거다. 각 코드 블록은 실제 실행 명령이다. 여러 줄 셸의 exit는 마지막 네이티브 명령/셸의 결과이지 모든 중간 rg가 매치했다는 뜻은 아니다. 긴 검색 출력 A14와 A28 등은 잘렸으므로 필요한 원천은 좁은 조회로 확인했다. A18의 CR 혼합 로그 인덱스 추출은 A20의 rg 줄번호로 교정했다. A31은 감사 목록을 이 보고서에만 썼으며 새 helper가 아니다.

<details>
<summary>A1 — shell exit 0</summary>

```powershell
git --no-optional-locks diff --stat b8631ccb 58c8d4cb
git --no-optional-locks diff --stat ffe7f983 3bb76772
git --no-optional-locks diff --name-only b8631ccb 58c8d4cb
git --no-optional-locks diff --name-only ffe7f983 3bb76772
git --no-optional-locks diff b8631ccb 58c8d4cb -- .github '*.props' '*.targets' '*.json' '*.asset' '*.asmdef'
git --no-optional-locks diff ffe7f983 3bb76772 -- .github '*.props' '*.targets' '*.json' '*.asset' '*.asmdef'
```

</details>

<details>
<summary>A2 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select/lead'; foreach($f in @('pr291-checks-snap-01.txt','pr291-checks-watch-01.txt','pr291-checks-watch-02.txt','pr268-checks-watch-v3.txt')) { "FILE $f"; rg -n '(fail|error|cancel|timeout)' "$r/$f"; Get-Content -LiteralPath "$r/$f" | Select-Object -Last 5 }; foreach($f in @('pr291-view-02.json','pr268-view-ci-final.json')) { "FILE $f"; Get-Content -Raw -LiteralPath "$r/$f" }; rg -n 'CI|Unity|EditMode|PlayMode|경고|완화' "$r/pr-a-body-v1.md" "$r/pr268-body-v2.md"
```

</details>

<details>
<summary>A3 — shell exit 0</summary>

```powershell
git --no-optional-locks diff b8631ccb 58c8d4cb -- '*.cs' | Select-String -Pattern '^\+.*(pragma|Suppress|Ignore|Explicit|LogAssert|Assert|Warning|demo-no-bgm)' | Select-Object -First 55
git --no-optional-locks diff ffe7f983 3bb76772 -- '*.cs' | Select-String -Pattern '^\+.*(pragma|Suppress|Ignore|Explicit|LogAssert|Warning|demo-no-bgm)'
rg -n '^#|test-run|OpenMenuBeforeHandshake' '.backups/verification/2026-10-31-demo-autopilot-character-select/pr268-verify-02/u3/editmode/results.xml' '.backups/verification/2026-10-31-demo-autopilot-character-select/pr268-verify-02/u3/playmode/results.xml' | Select-Object -First 8
rg -n 'report.md|correction|errata|mask-server|drift-restore|main-msg|art.*feedback|c421|7f977' '.backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/gardener-inputs-v1.txt'
```

</details>

<details>
<summary>A4 — shell exit 0</summary>

```powershell
$p='.backups/verification/2026-10-31-demo-autopilot-character-select/recording-build-verify-01/verdict.md'; $a=Get-Content -LiteralPath $p; 18..23 | ForEach-Object {'{0}:{1}' -f ($_+1),$a[$_] }
$p='.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/verdict.md'; $a=Get-Content -LiteralPath $p; foreach($range in @(@(163,198),@(271,292))){ ($range[0]-1)..([Math]::Min($a.Count,$range[1])-1) | ForEach-Object { '{0}:{1}' -f ($_+1),$a[$_] } }
```

</details>

<details>
<summary>A5 — shell exit 0</summary>

```powershell
$p='.backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/gardener-inputs-v1.txt'; Get-Content -LiteralPath $p | Select-Object -Skip 120 -First 37; Get-Content -LiteralPath $p | Select-Object -Skip 181 -First 49
$p='.backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_391a8e696163-f672813a-140A.txt'; $a=Get-Content -LiteralPath $p; for($i=0;$i -lt $a.Count;$i++){'{0}:{1}' -f ($i+1),$a[$i]}
$p='.backups/verification/2026-10-31-demo-autopilot-character-select/lead/main-msg_296cdc426451-round3-decision-A.txt'; $a=Get-Content -LiteralPath $p; for($i=0;$i -lt $a.Count;$i++){'{0}:{1}' -f ($i+1),$a[$i]}
```

</details>

<details>
<summary>A6 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; foreach($rel in @('lead/mask-server-preview-01.json','lead/mask-server-preview-02.json','lead/rehearsal-v2-lead-judgement-correction-01.md','lead/recording-v3-lead-judgement-errata-01.md')) { "FILE $rel"; $a=Get-Content -LiteralPath "$r/$rel"; for($i=0;$i -lt $a.Count;$i++){'{0}:{1}' -f ($i+1),$a[$i]} }
```

</details>

<details>
<summary>A7 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; Get-ChildItem -LiteralPath "$r/recording-r3-verify-01" -File | Where-Object { $_.Name -notmatch 'terminal-list|terminal-read|first-screen|screen|close-read' } | Select-Object Name,Length
Get-Content -LiteralPath "$r/closeout-01/liveness-input-04.json" -TotalCount 18
Get-Content -LiteralPath "$r/lead/recording-r3-target-manifest-01.json" -TotalCount 16
$p="$r/lead/lead-context-memo-closeout-01.md"; $a=Get-Content -LiteralPath $p; 0..15 | ForEach-Object { '{0}:{1}' -f ($_+1),$a[$_] }
```

</details>

<details>
<summary>A8 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; $g=Get-Content -LiteralPath "$r/closeout-01/gardener-inputs/goal.md"; $b=Get-Content -LiteralPath "$r/closeout-01/gardener-inputs/BACKLOG.md"; $target=($g[6..18]+$g[23]+$g[243..254]+$g[599..672]+$b[522..527]) -join "`n"; $ids=@([regex]::Matches($target,'msg_[0-9a-f]+').Value | Sort-Object -Unique); $raw=Get-Content -Raw -LiteralPath "$r/closeout-01/liveness-input-04.json"; $msgs=$raw | ConvertFrom-Json; foreach($id in $ids) { $m=@($msgs | Where-Object id -eq $id); if($m.Count) { $at=[regex]::Match($raw,'"id":\s*"'+$id+'"(?s:.*?)"created_at":\s*"([^"]+)"').Groups[1].Value; "$id | $at | $($m[0].subject)" } else { "MISSING $id" } }
"targetMessageIds=$($ids.Count)"
$es=@($msgs | Where-Object { $_.type -eq 'escalation' -and ($_.body -match 'worker_done' -or $_.subject -match 'worker_done') }); foreach($m in $es) { $at=[regex]::Match($raw,'"id":\s*"'+$m.id+'"(?s:.*?)"created_at":\s*"([^"]+)"').Groups[1].Value; "$($m.id) | $at | $($m.subject) | $($m.dispatch_id)" }; "automaticEscalations=$($es.Count)"
```

</details>

<details>
<summary>A9 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; $listed=@(Get-Content -LiteralPath "$r/closeout-01/gardener-inputs-v1.txt" | Where-Object { $_ -match '^[0-9a-f]{64}\s+\d+\s+(.+)$' } | ForEach-Object { [regex]::Match($_,'^[0-9a-f]{64}\s+\d+\s+(.+)$').Groups[1].Value }); $receipts=@(Get-ChildItem -LiteralPath "$r/lead" -File -Filter '*.receipt.json' | Where-Object { $_.Name -notmatch 'terminal-list|terminal-read|first-screen|screen|close-read' } | ForEach-Object FullName); $texts=@(Get-ChildItem -LiteralPath "$r/lead" -File -Filter 'main-msg_*.txt' | ForEach-Object FullName); $files=@($listed+$receipts+$texts | Sort-Object -Unique); $hits=Select-String -LiteralPath $files -Pattern 'msg_45b7227b04ff|msg_5e1339c81084|msg_8d6a75a7daed|msg_c277074f6a51|msg_c8dd2ae6e315|msg_e346906e8f65|2be227f1' ; $hits | ForEach-Object { '{0}:{1}:{2}' -f $_.Path,$_.LineNumber,($_.Line.Substring(0,[Math]::Min(260,$_.Line.Length))) }
```

</details>

<details>
<summary>A10 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; $listed=@(Get-Content -LiteralPath "$r/closeout-01/gardener-inputs-v1.txt" | Where-Object { $_ -match '^[0-9a-f]{64}\s+\d+\s+(.+)$' } | ForEach-Object { [regex]::Match($_,'^[0-9a-f]{64}\s+\d+\s+(.+)$').Groups[1].Value } | Where-Object { $_ -notmatch 'contract|gardener-inputs/|backlog-section|resume-state|liveness-input|inbox-run-full' }); foreach($id in @('msg_45b7227b04ff','msg_8d6a75a7daed','msg_7f9778ed1c3d')) { "$id"; Select-String -LiteralPath $listed -SimpleMatch $id | ForEach-Object { '{0}:{1}' -f $_.Path,$_.LineNumber } }
$p="$r/lead/take-main-20261104-01.txt"; Get-Content -LiteralPath $p -TotalCount 40
Get-ChildItem -LiteralPath "$r/lead" -File | Where-Object { $_.Name -match 'fixed-head|merge-main|art.*opinion|art.*feedback|8d6a75|45b722|core-plan-answer|build-handoff-core|round3-verify-result|rehearsal-v2-result-core' } | Select-Object Name,Length
```

</details>

<details>
<summary>A11 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; Get-Content -LiteralPath "$r/closeout-01/liveness-sources-04.json" -TotalCount 75
Get-FileHash -Algorithm SHA256 -LiteralPath "$r/lead/a8-art-opinion-msg_7f9778ed1c3d.txt"
$p="$r/lead/a8-art-opinion-msg_7f9778ed1c3d.txt"; $a=Get-Content -LiteralPath $p; for($i=0;$i -lt $a.Count;$i++){ '{0}:{1}' -f ($i+1),$a[$i] }
$p="$r/lead/fixed-head-check-01.txt"; Get-Content -LiteralPath $p
```

</details>

<details>
<summary>A12 — shell exit 0</summary>

```powershell
git --no-optional-locks grep -n -E 'WaitFor|wait timeout|승인|결정|21A' fff227fa -- 03_Client/Assets/Scripts/Demo/DemoAutopilot.cs 03_Client/Assets/Scripts/Demo/DemoLaunchOptions.cs
git --no-optional-locks grep -n -E 'Gold_bar|HudReferences|LogHudAndReadability|found=0' fff227fa -- 03_Client/Assets/Tests/PlayMode/GuildGrowthEffectConsumerLifecycleTests.cs
git --no-optional-locks show fff227fa:03_Client/Assets/Scripts/UI/CharacterMenuPresenter.cs | Select-Object -Skip 247 -First 7
git --no-optional-locks diff 5dd67b09 cfe8bb45 -- 03_Client/Assets/Tests/PlayMode/GuildGrowthEffectConsumerLifecycleTests.cs
git --no-optional-locks show -s --format='%H %P %cI %s' 58c8d4cb 3bb76772 a888cd35 fbf00de9 cfe8bb45 f1bcae3b c1ca41e6
git --no-optional-locks rev-parse fff227fa:01_Phases/goals/2026-10-31-demo-autopilot-character-select/goal.md fff227fa:00_Document/operations/CURRENT.md fff227fa:00_Document/operations/BACKLOG.md
```

</details>

<details>
<summary>A13 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; $j=Get-Content -Raw -LiteralPath "$r/closeout-01/liveness-output-04.json" | ConvertFrom-Json; $groups=@{}; $over=0; $waits=0; $verifierIds=@('ctx_eaf67d259cea','ctx_2f64b6a4037e','ctx_12f0c34ddea6','ctx_4064e2beda5b','ctx_36bd1abc1b36','ctx_f465132f38d8','ctx_c729c6bea4ae','ctx_c52c5cb1100f','ctx_e1d38dabbd61','ctx_e3d3bf774549'); $vOver=0; $vSessions=0; foreach($d in $j.dispatches) { $waits+=$d.questionWaits.Count; if($verifierIds -contains $d.dispatchId -and $d.overGaps.Count -gt 0){$vSessions++;$vOver+=$d.overGaps.Count}; foreach($gap in $d.overGaps){$over++; $group=if(@('ctx_c52c5cb1100f','ctx_e1d38dabbd61','ctx_e3d3bf774549') -contains $d.dispatchId){'self-verdict'}elseif($d.dispatchId -eq 'ctx_e5cf2061302e' -and $gap.from.type -eq 'escalation'){'idle-after-turn'}elseif($d.dispatchId -eq 'ctx_4064e2beda5b'){'window8-wait'}else{'within-turn'}; if(-not $groups.ContainsKey($group)){$groups[$group]=@()}; $groups[$group]+=$gap.seconds } }; "dispatches=$($j.dispatches.Count) overGaps=$over questionWaits=$waits verifierSessionsWithOver=$vSessions verifierOverGaps=$vOver"; foreach($key in $groups.Keys | Sort-Object){"$key count=$($groups[$key].Count) max=$(($groups[$key] | Measure-Object -Maximum).Maximum)"}
$g=Get-Content -LiteralPath "$r/closeout-01/gardener-inputs/goal.md"; $stages=@($g[343..354]); $stages | ForEach-Object { $m=[regex]::Match($_,'^- \[([x> ])\] (.+)$'); "mark=$($m.Groups[1].Value) length=$($m.Groups[2].Value.Length) name=$($m.Groups[2].Value)" }; "stageCount=$($stages.Count) active=$(@($stages|Where-Object {$_ -match '^\- \[>\]'}).Count)"
$base=git --no-optional-locks merge-base c1ca41e6^1 c1ca41e6^2; $main=@(git --no-optional-locks diff --name-only $base c1ca41e6^2); $topic=@(git --no-optional-locks diff --name-only $base c1ca41e6^1); "mergeBase=$base mainChanged=$($main.Count) topicChanged=$($topic.Count) overlap=$(@($main|Where-Object {$topic -contains $_}).Count)"; $main
foreach($pair in @(@('b8631ccb','58c8d4cb'),@('ffe7f983','3bb76772'))){$added=@(git --no-optional-locks diff $pair[0] $pair[1] -- '*.cs' | Where-Object { $_ -match '^\+(?!\+)' -and $_ -match '#pragma\s+warning|SuppressMessage|Assert\.Ignore|\[Ignore|\[Explicit|ignoreFailingMessages' }); "suppressionChanges $($pair[0])..$($pair[1])=$($added.Count)"; $added}
```

</details>

<details>
<summary>A14 — shell exit 0</summary>

```powershell
git --no-optional-locks grep -n -E 'heartbeat.{0,50}(300|초과|문맥)|worker_done.{0,40}(없이|누락|턴)|preview.{0,50}(서버|가림)|리드.{0,20}(수치 오류|정오|정정)' fff227fa -- '01_Phases/goals/*/goal.md' '01_Phases/goals/*/gardener.md' | Where-Object { $_ -notmatch '2026-10-31-demo-autopilot-character-select' } | Select-Object -First 75
```

</details>

<details>
<summary>A15 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; $b="$r/closeout-01/gardener-inputs/BACKLOG.md"; rg -n '^\|.*(heartbeat-interval-review|worker-heartbeat-target-gap-table|terminal-evidence-field-allowlist|raw-value|원시.*보존|계약.*밖)' $b | ForEach-Object { $_.Substring(0,[Math]::Min(850,$_.Length)) }
$p="$r/closeout-01/gardener-inputs/goal.md"; rg -n 'drift|복원|첫 발생|두 번째|계획.*달|경고|skipped|self-test|수용' $p | ForEach-Object { $_.Substring(0,[Math]::Min(700,$_.Length)) }
```

</details>

<details>
<summary>A16 — shell exit 0</summary>

```powershell
git --no-optional-locks grep -n -E 'worker_done.{0,60}(없이|누락|턴)' fff227fa -- '01_Phases/goals/*/goal.md' '01_Phases/goals/*/gardener.md' | Where-Object { $_ -notmatch '2026-10-31-demo-autopilot-character-select' } | Select-Object -First 10 | ForEach-Object { $_.Substring(0,[Math]::Min(650,$_.Length)) }
git --no-optional-locks grep -n -E 'preview|서버 창|정리 후보 2' fff227fa -- '01_Phases/goals/2026-10-31-db-persistence-demo-build/gardener.md' | Select-Object -First 15 | ForEach-Object { $_.Substring(0,[Math]::Min(700,$_.Length)) }
git --no-optional-locks grep -n -E '수치 오류|기록 정정|heartbeat.*초과' fff227fa -- '01_Phases/goals/2026-10-11-pre-freeze-rule-docs/goal.md' | Select-Object -Last 8
```

</details>

<details>
<summary>A17 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; $j=Get-Content -Raw -LiteralPath "$r/closeout-01/liveness-sources-04.json" | ConvertFrom-Json; $j.sources | Where-Object incomplete -gt 0 | Select-Object source,incomplete
$p="$r/lead/resume-state.md"; rg -n '45b722|8d6a75|entry|진입' $p | Select-Object -First 10
$p="$r/recording-build-01/recording-round3/run-01/report.md"; rg -n '한계|launcher|heartbeat|사후|수동|계획' $p | Select-Object -Last 18
$p="$r/lead/recording-r3-lead-judgement.md"; rg -n '한계|넷|세 건|수동|launcher|D2|UTC' $p | Select-Object -Last 15
```

</details>

<details>
<summary>A18 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; foreach($rel in @('recording-r3-verify-01/audio-timing-recompute.json','recording-r3-verify-01/heartbeats/hb-007.json','recording-r3-verify-01/heartbeats/hb-008.json')){ "FILE $rel"; Get-Content -Raw -LiteralPath "$r/$rel" }; rg -n 'duration|nb_read_frames|width|height|r_frame_rate' "$r/recording-r3-verify-01/probe-video.stdout.txt"; foreach($rel in @('recording-build-01/recording-round3/media/r4-3-knight-video.mkv','recording-build-01/recording-round3/media/r4-3-audio.wav','recording-build-01/recording-round3/media/r4-3-markers.json')){ $p="$r/$rel"; "$rel bytes=$((Get-Item -LiteralPath $p).Length) sha256=$((Get-FileHash -Algorithm SHA256 -LiteralPath $p).Hash.ToLowerInvariant())" }; $p="$r/recording-build-01/recording/knight-player-r4-1.log"; $a=Get-Content -LiteralPath $p; "R1:81:$($a[80])"; $p="$r/recording-build-01/recording-round3/knight-player-r4-3.log"; $a=Get-Content -LiteralPath $p; "R3:76:$($a[75])"; Get-Content -LiteralPath "$r/recording-r3-verify-01/frame-ledger.jsonl" | Select-Object -Skip 12 -First 1
```

</details>

<details>
<summary>A19 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; foreach($rel in @('lead/check-nowait-20261104-02.json','lead/send-build-handoff-core-20261106-02.cjs','lead/send-rehearsal-v2-result-core-20261106-01.cjs','lead/send-round3-verify-result-main-20261106-01.cjs','lead/send-v3-core-plan-answer-20261106-01.cjs')){ "FILE $rel"; rg -n 'msg_8d6a75|14:00|Art가|body|확정|판정|새벽동료|새벽기사|A는|B는|created_at' "$r/$rel" | ForEach-Object { $_.Substring(0,[Math]::Min(1500,$_.Length)) } }
```

</details>

<details>
<summary>A20 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; rg -n 'c1b405cc-7a5b-41d0-bcfd-6a976147a7b1|CharacterMenu.*(50|52)' "$r/recording-build-01/recording/knight-player-r4-1.log" "$r/recording-build-01/recording-round3/knight-player-r4-3.log"
$p="$r/recording-r3-verify-01/player-log-hashes.json"; Get-Content -Raw -LiteralPath $p
Get-Content -LiteralPath "$r/recording-r3-verify-01/identity-recompute.json" -TotalCount 55
$p="$r/recording-r3-verify-01/frame-ledger.jsonl"; $entries=Get-Content -LiteralPath $p | ForEach-Object { $_ | ConvertFrom-Json }; $rows=@($entries | Where-Object { $_.inputFrame -in @(90,138,139,140,141,164,165,179,180,195,196,198,240) }); foreach($e in $rows){ $hash=(Get-FileHash -Algorithm SHA256 -LiteralPath $e.file).Hash.ToLowerInvariant(); "frame=$($e.inputFrame) pts=$($e.ptsSeconds) hashMatches=$($hash -eq $e.sha256)" }
$p="$r/lead/check-nowait-20261104-02.json"; $j=Get-Content -Raw -LiteralPath $p | ConvertFrom-Json; ($j.result.messages | Where-Object id -eq 'msg_8d6a75a7daed').body -split "`n" | Select-String -Pattern 'Art가|몫|후보|메인 세션|제출 기한' 
```

</details>

<details>
<summary>A21 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; foreach($rel in @('pr-a-red-01/report.md','pr-a-impl-01/report.md','pr268-red-01/report.md','pr268-impl-01/report.md','pr268-impl-02/report.md','recording-build-01/report.md','recording-build-01/rehearsal/run/report.md','recording-build-01/rehearsal/run-02/report.md','recording-build-01/recording/run-01/report.md','recording-build-01/recording-round3/run-01/report.md')){ "FILE $rel"; Select-String -LiteralPath "$r/$rel" -Pattern '계획과 달라|계획 변경|경고 억제|설정 완화|기대값 완화|no-bgm|C13' -Context 0,3 | ForEach-Object { "$($_.LineNumber):$($_.Line)"; $_.Context.PostContext } }
rg -n 'drift|드리프트' "$r/closeout-01/gardener-inputs/goal.md"
Get-Content -Raw -LiteralPath "$r/lead/drift-restore-01.txt"
Get-ChildItem -LiteralPath "$r/lead" -Directory | Where-Object Name -match 'drift' | Select-Object Name
```

</details>

<details>
<summary>A22 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; $files=@('pr-a-verify-01/verdict.md','pr268-verify-02/verdict.md','pr268-docs-recheck-01/verdict.md','recording-build-verify-01/verdict.md','rehearsal-v2-verify-01/verdict.md','recording-v3-verify-01/verdict.md','recording-r3-verify-01/verdict.md'); $total=0; $blocking=0; foreach($rel in $files){$s=Get-Content -Raw -LiteralPath "$r/$rel"; $part=[regex]::Match($s,'(?s)## 기계 판독 표(.*?)(?=\r?\n## |\z)').Groups[1].Value; $rows=@($part -split "`n" | Where-Object { $_ -match '^\|[^|]+\|\s*D\d' });$blocks=@($rows|Where-Object { ($_ -split '\|')[4].Trim() -match '^예' });"$rel defects=$($rows.Count) blocking=$($blocks.Count)"; $total+=$rows.Count;$blocking+=$blocks.Count};"totalDefects=$total blocking=$blocking nonblocking=$($total-$blocking)"
git --no-optional-locks diff b8631ccb 58c8d4cb -- 03_Client/Assets/Tests/EditMode/CharacterMenuBoundaryTests.cs 03_Client/Assets/Tests/EditMode/CharacterMenuPresenterTests.cs
git --no-optional-locks show fff227fa:03_Client/Assets/Tests/PlayMode/GuildGrowthEffectConsumerLifecycleTests.cs | Select-Object -Skip 927 -First 15
```

</details>

<details>
<summary>A23 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; $p="$r/closeout-01/liveness-input-04.json"; $j=Get-Content -Raw -LiteralPath $p | ConvertFrom-Json; foreach($id in @('msg_9203c45e18e9','msg_e8dd9cb571fd','msg_45234d43130c','msg_d79eebc3d94c','msg_478035aa5063','msg_b50c04ca0c08','msg_e0b59e962663')) { $m=$j|Where-Object id -eq $id; "$id"; $m.body }
Get-Content -Raw -LiteralPath "$r/lead/main-msg_17a5453d6222-decision-135A-name.txt"
```

</details>

<details>
<summary>A24 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; rg -l -F 'msg_45b7227b04ff' "$r/lead" -g '*.json' -g '*.txt' -g '!*.cjs' -g '!*terminal-list*' -g '!*terminal-read*' -g '!*first-screen*' -g '!*screen*' -g '!*close-read*'
Get-ChildItem -LiteralPath "$r/lead/window6-drift","$r/lead/u3v2-drift","$r/lead/windowA-drift","$r/lead/windowB-drift","$r/lead/u1-drift","$r/lead/u2-drift","$r/lead/u3-drift" -File | Select-Object FullName,Length
Get-Content -LiteralPath "$r/pr268-verify-03/untracked-tests-saved/copy-sha256.txt"
Get-Content -LiteralPath "$r/pr268-verify-03/untracked-tests-saved/source-sha256.txt"
Get-ChildItem -LiteralPath "$r/pr268-verify-03/untracked-tests-saved" -File | Select-Object Name,Length
```

</details>

<details>
<summary>A25 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; foreach($dir in @('u1-drift','u2-drift','u3-drift','window6-drift','u3v2-drift','windowA-drift','windowB-drift')) { "DIR $dir"; Get-ChildItem -LiteralPath "$r/lead/$dir" -File | ForEach-Object {"$($_.Name) $($_.Length)"}; Get-ChildItem -LiteralPath "$r/lead/$dir" -File | Where-Object { $_.Name -match 'restor|after|before|status|receipt|\.txt$' } | ForEach-Object { "FILE $($_.Name)"; Get-Content -LiteralPath $_.FullName | Select-Object -First 15 } }
$p="$r/pr268-verify-03/verdict.md"; $a=Get-Content -LiteralPath $p; 258..267 | ForEach-Object { '{0}:{1}' -f ($_+1),$a[$_] }
$p="$r/lead/recording-run-contract-r3-v1.md"; $a=Get-Content -LiteralPath $p; 38..42 | ForEach-Object {'{0}:{1}' -f ($_+1),$a[$_] }
Get-Content -Raw -LiteralPath "$r/lead/close-recording-r3-verify-01.json"
Get-Content -Raw -LiteralPath "$r/lead/terminal-list-after-recording-r3-verify-close-01.redacted.json"
```

</details>

<details>
<summary>A26 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; $g=Get-Content -LiteralPath "$r/closeout-01/gardener-inputs/goal.md"; 357..381 | ForEach-Object { '{0}:{1}' -f ($_+1),$g[$_] }
$p="$r/lead/main-msg_0496a60fc54e-r8-stop.txt"; $a=Get-Content -LiteralPath $p; 7..12 | ForEach-Object {'{0}:{1}' -f ($_+1),$a[$_] }
$p="$r/closeout-01/liveness-input-04.json"; rg -n 'msg_172f56d2069c|msg_c37abd59d1ed|msg_stop_|msg_296cdc426451|msg_7f9778ed1c3d|msg_c4212141fc7a|msg_9203c45e18e9|msg_e8dd9cb571fd' $p | Where-Object { $_.Length -lt 180 }
git --no-optional-locks rev-parse origin/docs/content-demo-autopilot-closeout-20261101 origin/main
git --no-optional-locks show fff227fa:03_Client/Assets/Scripts/Demo/DemoKeyboard.cs | Select-Object -First 68
$p='00_Document/conventions/REPORTING.md'; $a=Get-Content -LiteralPath $p; 32..43 | ForEach-Object {'{0}:{1}' -f ($_+1),$a[$_] }
```

</details>

<details>
<summary>A27 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'; $p="$r/pr268-verify-03/untracked-tests-saved/copy-sha256.txt"; Get-Content -LiteralPath $p | Where-Object { $_ -match '^([0-9a-f]{64}) \*(.+)$' } | ForEach-Object { $e=$Matches[1];$rel=$Matches[2];$f="$r/pr268-verify-03/untracked-tests-saved/$rel";"saved-test $rel matches=$(((Get-FileHash -Algorithm SHA256 -LiteralPath $f).Hash.ToLowerInvariant()) -eq $e)" }
$p="$r/closeout-01/gardener-inputs/BACKLOG.md"; rg -n '^\| `(demo-autoplay-character-menu|demo-recording-build-contract|character-select-three-wide-slots|character-menu-selecting-detail-text|character-slot-count-four)`' $p
$p="$r/recording-r3-verify-01/visual-observations.json"; rg -n 'first|last|141|164|139|140|165|179|196|90|138|198' $p | Select-Object -First 30
$p="$r/lead/recording-run-contract-r3-v1.md"; rg -n '마지막 행동은 worker_done' $p
$p="$r/closeout-01/gardener-contract-v1.md"; rg -n '마지막 행동은 worker_done' $p
$p="$r/closeout-01/current-write-01.json"; Get-Content -Raw -LiteralPath $p
```

</details>

<details>
<summary>A28 — shell exit 0</summary>

```powershell
git --no-optional-locks show fff227fa:01_Phases/goals/2026-10-31-db-persistence-demo-build/gardener.md | Select-Object -Skip 113 -First 12
git --no-optional-locks grep -n -F 'msg_45b7227b04ff' fff227fa -- 01_Phases/goals/2026-10-30-login-screen-guild-window/goal.md
git --no-optional-locks show c1ca41e6:01_Phases/goals/2026-10-31-demo-autopilot-character-select/goal.md | Select-String -Pattern '^\- \[[x> -]\]' 
$p='.backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/liveness-output-04.json'; rg -n 'ctx_eaf67d259cea|ctx_12f0c34ddea6|ctx_4064e2beda5b|ctx_c729c6bea4ae|ctx_c52c5cb1100f|ctx_e1d38dabbd61|ctx_e3d3bf774549' $p
```

</details>

<details>
<summary>A29 — shell exit 0</summary>

```powershell
Get-Content -LiteralPath '.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/helper-revision.json' -Raw
```

</details>

<details>
<summary>A30 — shell exit 0</summary>

```powershell
Get-Content -LiteralPath '.backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/gardener-inputs/BACKLOG.md' | Select-Object -Skip 522 -First 6
Get-Content -LiteralPath '.backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/gardener-inputs/goal.md' | Select-Object -Skip 665 -First 8
Get-Content -LiteralPath '.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frame-ledger.jsonl' -TotalCount 1
```

</details>

<details>
<summary>A31 — shell exit 0</summary>

```powershell
$r='.backups/verification/2026-10-31-demo-autopilot-character-select'
$root=(Get-Location).Path.Replace('\','/')
$manifestPaths=@(Get-Content -LiteralPath "$r/closeout-01/gardener-inputs-v1.txt" | Where-Object {$_ -match '^[0-9a-f]{64}\s+\d+\s+(.+)$'} | ForEach-Object {[IO.Path]::GetFullPath($Matches[1]).Replace('\','/')})
$paths=@('CLAUDE.md',
'00_Document/operations/DEVELOPMENT.md',
'.backups/verification/2026-10-31-demo-autopilot-character-select/lead/a8-art-opinion-msg_7f9778ed1c3d.txt',
'.backups/verification/2026-10-31-demo-autopilot-character-select/lead/fixed-head-check-01.txt',
'03_Client/Assets/Scripts/UI/CharacterMenuPresenter.cs',
'01_Phases/goals/2026-10-31-db-persistence-demo-build/gardener.md',
'01_Phases/goals/2026-10-11-pre-freeze-rule-docs/goal.md',
'.backups/verification/2026-10-31-demo-autopilot-character-select/lead/recording-r3-lead-judgement.md',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/audio-timing-recompute.json',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/heartbeat-recompute.json',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/editor-file-hashes.json',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/probe-video.stdout.txt',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/visual-observations.json',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/frame-ledger.jsonl',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/heartbeats/hb-007.json',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/heartbeats/hb-008.json',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-build-01/recording-round3/media/r4-3-knight-video.mkv',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-build-01/recording-round3/media/r4-3-audio.wav',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-build-01/recording-round3/media/r4-3-markers.json',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-build-01/recording/knight-player-r4-1.log',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-build-01/recording-round3/knight-player-r4-3.log',
'.backups/verification/2026-10-31-demo-autopilot-character-select/lead/check-nowait-20261104-02.json',
'.backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-build-handoff-core-20261106-02.cjs',
'.backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-rehearsal-v2-result-core-20261106-01.cjs',
'.backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-round3-verify-result-main-20261106-01.cjs',
'.backups/verification/2026-10-31-demo-autopilot-character-select/lead/send-v3-core-plan-answer-20261106-01.cjs',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/player-log-hashes.json',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/identity-recompute.json',
'03_Client/Assets/Scripts/Demo/DemoKeyboard.cs',
'01_Phases/goals/2026-10-30-login-screen-guild-window/goal.md',
'C:/Users/bass1/.agents/skills/orchestration/SKILL.md',
'00_Document/conventions/REPORTING.md',
'00_Document/FEATURE_MAP.md',
'.agents/skills/dawnholder-task-context/SKILL.md',
'.agents/skills/dawnholder-task-context/references/templates.md',
'.agents/skills/dawnholder-goal-loop/SKILL.md',
'99_Tools/README.md',
'01_Phases/goals/2026-10-30-login-screen-guild-window/gardener.md',
'.backups/verification/2026-10-31-demo-autopilot-character-select/recording-r3-verify-01/helper-revision.json')
$paths+=@(Get-ChildItem -LiteralPath "$r/lead" -File -Recurse | Where-Object { $_.Extension -in @('.json','.txt') -and $_.Name -notmatch 'terminal-list|terminal-read|first-screen|screen|close-read' } | ForEach-Object FullName)
foreach($dir in @('u1-drift','u2-drift','u3-drift','window6-drift','u3v2-drift','windowA-drift','windowB-drift')){$paths+=@(Get-ChildItem -LiteralPath "$r/lead/$dir" -File | Where-Object { $_.Name -match 'restor|after|before|status|receipt|\.txt$' } | ForEach-Object FullName)}
$paths+=@(Get-ChildItem -LiteralPath "$r/pr268-verify-03/untracked-tests-saved" -File -Recurse | ForEach-Object FullName)
$paths+=@(Get-Content -LiteralPath "$r/recording-r3-verify-01/frame-ledger.jsonl" | ForEach-Object { $_ | ConvertFrom-Json } | Where-Object {$_.inputFrame -in @(90,138,139,140,141,164,165,179,180,195,196,198,240)} | ForEach-Object file)
$historical=@(git --no-optional-locks grep -n -E 'heartbeat.{0,50}(300|초과|문맥)|worker_done.{0,40}(없이|누락|턴)|preview.{0,50}(서버|가림)|리드.{0,20}(수치 오류|정오|정정)' fff227fa -- '01_Phases/goals/*/goal.md' '01_Phases/goals/*/gardener.md' | Where-Object {$_ -notmatch '2026-10-31-demo-autopilot-character-select'} | Select-Object -First 75)
$historical+=@(git --no-optional-locks grep -n -E 'worker_done.{0,60}(없이|누락|턴)' fff227fa -- '01_Phases/goals/*/goal.md' '01_Phases/goals/*/gardener.md' | Where-Object {$_ -notmatch '2026-10-31-demo-autopilot-character-select'} | Select-Object -First 10)
$paths+=@($historical | ForEach-Object {if($_ -match '^fff227fa:(.+?):\d+:'){$Matches[1]}})
$paths=@($paths | ForEach-Object {[IO.Path]::GetFullPath($_).Replace('\','/')} | Where-Object {$_ -notin $manifestPaths} | Sort-Object -Unique)
$rows=@(); $missing=@()
foreach($p in $paths){$item=Get-Item -LiteralPath $p -ErrorAction SilentlyContinue; if($null -eq $item){$missing+=$p;continue};$hash=(Get-FileHash -Algorithm SHA256 -LiteralPath $p).Hash.ToLowerInvariant();$display=if($p.StartsWith($root+'/')){$p.Substring($root.Length+1)}else{$p};$rows+="| $display | $($item.Length) | $hash |"}
$table="고정224개 입력 외에 내용·검색 결과·파일 hash를 확인한 파일이다. E/lead JSON·txt는 GR-03 ID 검색 범위 전체를 보수적으로 포함했다. 그 파일들을 모두 전문 해석했다는 뜻은 아니다. 금지 이름 원본은 제외했고 PNG·매체는 hash만 읽었으며 재생하지 않았다. Git 원천은 fff227fa 기준, 아래 물리 파일 hash는 같은 checkout의 감사 시점 값이다. 스킬 파일은 세션 진입 지침상 읽었다."+"\n\n추가 목록: "+$rows.Count+"파일. 누락 "+$missing.Count+"개.\n\n<details>\n<summary>추가 원천 경로·bytes·SHA256</summary>\n\n| 경로 | bytes | SHA256 |\n|---|---:|---|\n"+($rows -join "\n")+"\n\n</details>"
$table=$table.Replace('\n',"`n")
$rp="$r/closeout-01/gardener/context-gardener-report.md"
$s=[IO.File]::ReadAllText($rp)
$old='목록 밖의 `CLAUDE.md`, `00_Document/operations/DEVELOPMENT.md`는 HEAD의 배정/도구 절만 Git 읽기로 확인했다. hash·bytes 및 반복 goal 원천은 최종 목록으로 보완한다.'
if(-not $s.Contains($old)){throw 'Inventory placeholder missing'}
$s=$s.Replace($old,$table)
[IO.File]::WriteAllText($rp,$s,[Text.UTF8Encoding]::new($false))
"extraFiles=$($rows.Count) missing=$($missing.Count)"
$missing
```

</details>

### 미실행과 판정의 한계

- Unity/Hub/클라이언트/서버/DB/WSL/dotnet/웹/gh 실행, 영상·소리·이미지 재생은 하지 않았다. 제품 동작의 새 통과 판정이 아니다.
- 현재 원격 PR 상태·실제 backend 모델은 재조회하지 않았다. 고정 로컬 Git 객체/저장 CI·판정·메인 메시지를 근거로 삼았다.
- 삭제·복원·패키지 설치·Git 쓰기·새 helper·서브에이전트·다른 터미널 조회는 하지 않았다. 리드 cjs는 전송 본문 출처를 읽었으며 실행하지 않았다.
- receipt만으로 전송 본문 전문 보존을 입증하지 않는다. 해당 행은 발행 소스·후속 응답과 교차한 수준이다. 원시 없는 오래된 메시지는 GR-03으로 반환한다.
- 독립 결함 수는 처음 나온 D번호 표의 문서 집계다. 수용/해소를 당시 통과로 바꾸지 않는다.
- 추가 목록은 문자열 검색 대상 전체를 보수적으로 포함한다. 전부 전문 해석하거나 검색 무매치로 역사상 부재를 입증한 것은 아니다.
- 초기 heartbeat 명령은 subject 누락 exit1 뒤 즉시 수정하여 성공했다. 권한 차단이 아니라 CLI 인자 오류였다. 성공 전송만 간격에 사용한다.
- 부록의 첫 일괄 쓰기는 Windows CreateProcess 오류206(명령 길이)로 프로세스 생성 전 실패했다. 출력 파일 쓰기는 발생하지 않았고 같은 보고서 추가를 짧은 호출로 분할했다. 자동 승인 거부가 아니며 우회 설정 변경도 없었다.

### 마감 감사 및 생존 신호 계산 명령

```powershell
git --no-optional-locks status --short
git --no-optional-locks rev-parse HEAD
Get-ChildItem -LiteralPath '.backups/tmp/gardener-demo01' -File -Recurse | Select-Object FullName,Length
Get-ChildItem -LiteralPath '.backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/gardener' -File | Select-Object Name,Length
rg -n '^## |demo-autoplay-wait-config|guild-growth-hud-assertion-strength|대조 중|보완한다|최종 위치는' '.backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/gardener/context-gardener-report.md'
```

exit 0. 추적 상태 출력은 비었고 HEAD는 `fff227fad36cb2704e756a10895a338cbd2363b5`다. TEMP 재귀 파일 목록은 비었고 허용 보고서 폴더에는 이 보고서 한 파일만 있었다. 이때 파일 크기 304679 bytes는 이 마감 절을 추가하기 전 값이다. 최종 크기와 hash는 worker_done에 실제 재계산값으로 보낸다.

```powershell
$ts=@('2026-11-06T06:32:27.1816733Z','2026-11-06T06:35:00.9431115Z','2026-11-06T06:36:15.9197455Z','2026-11-06T06:40:08.9913960Z','2026-11-06T06:43:52.2634211Z','2026-11-06T06:48:16.5669741Z','2026-11-06T06:57:54.6811104Z','2026-11-06T07:00:54.1199573Z','2026-11-06T07:04:07.7782267Z')
$gaps=@()
for($i=0;$i -lt $ts.Count;$i++){if($i -eq 0){"| $($i+1) | $($ts[$i]) | 첫 성공 송신 |"}else{$delta=([DateTimeOffset]::Parse($ts[$i]).Ticks-[DateTimeOffset]::Parse($ts[$i-1]).Ticks)/10000000.0;$gaps+=$delta;"| $($i+1) | $($ts[$i]) | $($delta.ToString('0.0000000',[Globalization.CultureInfo]::InvariantCulture)) |"}}
"maxSeconds=$(($gaps | Measure-Object -Maximum).Maximum) over300=$(@($gaps | Where-Object {$_ -gt 300}).Count) over180=$(@($gaps | Where-Object {$_ -gt 180}).Count)"
```

exit 0, 최종 요약 stdout:
```text
maxSeconds=578.1141363 over300=1 over180=5
```

## 생존 신호

첫 시도는 `2026-11-06T06:32:19.5578757Z`에 subject 누락으로 실패했고, 첫 성공은 아래 1번이다. 계약 SHA256 대조는 성공 신호 이후 수행했다. 성공 호출은 모두 즉전 UTC 시계와 heartbeat 전송만 담은 별도 셸 호출이었다. 공식 blocking ask는 없었다.

| 번호 | 실제 시계 UTC | 직전 성공부터 초 |
|---|---|---:|
| 1 | 2026-11-06T06:32:27.1816733Z | 첫 성공 송신 |
| 2 | 2026-11-06T06:35:00.9431115Z | 153.7614382 |
| 3 | 2026-11-06T06:36:15.9197455Z | 74.9766340 |
| 4 | 2026-11-06T06:40:08.9913960Z | 233.0716505 |
| 5 | 2026-11-06T06:43:52.2634211Z | 223.2720251 |
| 6 | 2026-11-06T06:48:16.5669741Z | 264.3035530 |
| 7 | 2026-11-06T06:57:54.6811104Z | 578.1141363 |
| 8 | 2026-11-06T07:00:54.1199573Z | 179.4388469 |
| 9 | 2026-11-06T07:04:07.7782267Z | 193.6582694 |

**자기 결함 GD-01:** 최대 **578.1141363초**, **300초 초과 1구간**, **180초 목표 초과 5구간**이다. 300초 초과는 06:48:16.5669741Z→06:57:54.6811104Z이며 긴 종료 기록 표 작성과 문맥 정리 구간을 포함한다. 문맥 정리가 끝난 뒤에는 첫 도구로 heartbeat를 보냈지만 이미 생긴 공백을 소급 준수로 바꾸지 않는다. 긴 입력 작성 전에 충분히 나누어 보내지 못한 이 세션의 절차 위반이다.

이 자기 결함은 고정 입력의 검증자 7세션·11구간이나 과거25구간에 포함하지 않는다. 과거 집계와 이번 Gardener 실행은 다른 Task/Dispatch다. 제품/녹화 처분을 뒤집는 차단 관찰은 아니며 리드가 자기 결함 처분을 결정할 수 있도록 worker_done 셋째 줄에도 1구간을 알린다. 위 계산 이후 최종 확인·완료 전송으로 종료하며, 완료 메시지 직전 마지막 구간은 이 표 이후의 경과이지 아직 관측하지 않은 송신 시각을 만들어 넣지 않는다.

대표 성공 호출(실제로 사용한 형식):
```powershell
[DateTime]::UtcNow.ToString('o')
orca orchestration send --from term_740f3807-b28b-48de-8d33-ae91abe167e4 --type heartbeat --subject '[Content Sol] Gardener 생존 신호' --body '[Content Sol] 승인된 읽기 전용 종료 점검 계속 중.' --task-id task_c8bf12a492b2 --dispatch-id ctx_e36caa1ebdbb
```

## 자기 쓰기 감사

- 수동 파일 쓰기는 `.backups/verification/2026-10-31-demo-autopilot-character-select/closeout-01/gardener/context-gardener-report.md` 한 파일뿐이다. 첫 쓰기는 최초 메모였고 결과는 절 단위로 추가/정정했다.
- 저장소 추적 파일은 착수·마감 모두 변경 출력 없음. HEAD/branch를 바꾸지 않았고 고정224입력은 최초 전체 SHA256/bytes 대조에서 모두 일치했다. 이후 고정 입력에 쓰는 명령은 없었다.
- 허용 보고서 폴더의 파일은 이 보고서 하나였고, `.backups/tmp/gardener-demo01/`에 남은 파일은 **0개**였다. 도구 자동 쓰기까지 전 디스크 추적한 결과는 아니며, 실행 명령과 지정 폴더/추적 상태 확인의 범위다.
- 금지된 이름의 터미널 JSON 원본, 자격·프로필 파일, 다른 서버 터미널, 다른 게임 그림을 열지 않았다. 허용 .redacted/.model-lines 사본만 보았고 PNG/매체는 hash만 확인했다.
- 파일·설정 삭제/복원, 홈·전역 설정 변경, 새 helper 파일, 제품·시험 코드 쓰기, Git 쓰기, 추가 위임은 없었다. 보고서 부록의 Windows 명령 길이 실패는 프로세스 생성 전 실패이며 분할 쓰기로 마쳤다.
- 자신을 독립 제품 검증자로 재분류하거나 구현자 실패를 늘리지 않았다. 결과 전달 후 같은 Task로 다른 일을 시작하지 않는다.
