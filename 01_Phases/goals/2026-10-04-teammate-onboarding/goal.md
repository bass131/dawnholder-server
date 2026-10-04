# 팀원 합류 최소 정본

## 목표와 승인

2026-10-05 합류할 사람 팀원이 자기 머신의 Codex 세션으로 게임 기획 문서를 작성할 수 있도록 역할 경계와 첫 작업 안내를 마련한다. 팀원 세션이 이 머신의 파트 리드나 Orca 작업자로 자신을 해석하지 않도록 한다.

메인 전달 `msg_2b93ecffcce9`(2026-10-04T14:07:27Z)가 이번 범위의 원천이다. 메인은 사용자 발언 「이미 콜라보레이터로 등록 되어 있어, A로 가는게 좋겠어」와 범위 초안에 대한 「OK 그렇게 가자」를 전달했다. **AGENTS.md는 Codex 영역이며 이 사용자 승인을 수정 요청 근거로 기록한다.** 메인의 전달을 사용자 직접 입력으로 격상하지 않는다. 원문은 로컬 `.backups/verification/2026-10-04-teammate-onboarding/main-request.json`(SHA256 `2aaa23d63c730f73f6f88bc12b8727c02b651c11b84f5fc8497dd974c7894097`)에 보존한다.

- 첫 작업은 코드 변경 없는 「현행 콘텐츠 명세」다. 실제 구현과 추측·미확인을 구분한다.
- GitHub PR 코멘트로 비동기 소통하며 팀원 AI 태그는 `[GameDesign Astra]`다. 팀원 AI·사람 팀원의 코멘트는 논의 자료이며 정책 결정은 사용자에게 있다.
- 초대 수락 요청 drop 뒤 팝업/pending 처리와 맵 이동 시 스킬 쿨다운은 명세의 열린 질문으로 넘긴다. 명세는 결정 입력이며 다른 구현의 필수 관문으로 새로 지정하지 않는다.
- 다른 파트의 중간 마감 동결은 유지하며 이 goal만 승인된 예외다.

## 범위와 완료조건

| 변경 대상 | 책임과 완료조건 |
|---|---|
| `AGENTS.md` | 외부 팀원 세션의 대상과 로컬 운영 절차 비적용을 밝히고 쓰기 경로를 `00_Document/game-design/` 하나로 한정한다. 작업 브랜치·PR·병합 금지·사용자 승인·PR 코멘트 태그·입력 신뢰 경계를 명시한다. |
| `00_Document/game-design/README.md` | 첫 작업, 읽기 진입점 세 곳, 근거/확인 상태를 포함한 산출물 형식, 열린 질문 두 개와 출처, PR 절차를 제공한다. |
| `00_Document/INDEX.md` | 새 안내 링크 한 행을 추가한다. |
| 이 `goal.md`, `00_Document/operations/CURRENT.md` | 목표·결정·실제 상태/근거와 현재 Rules 링크를 관리한다. 이전 하네스 goal은 보류 링크로 보존한다. |

신규 Sol이 안내 세 파일을 구현하고 쓰기를 종료한 뒤 신규 Opus가 요구사항·보고·실제 diff·원문 근거·맥락 메모·내용/참조/권한·가독성을 독립 실사한다. 문서 변경이므로 제품 테스트·빌드·게임 플레이·DB 실행은 이번 로컬 검증에 포함하지 않는다. Astra가 commit/push/PR 생성과 해당 head의 CI 결과까지 보고하며 사용자 명시 승인 전 병합하지 않는다.

범위 밖: 팀원 역할 전체 정본과 계정/승인 상세, 2026-10-04 메인 정리 결과 반영, 다른 운영 규칙, 코드·CI·스킬, 기존 하네스 미완 PR3/PR4/경로 검사, 콘텐츠 명세 자체 작성, 게임 정책 확정, 실시간 중계 서버. 후속 운영 정본 반영은 별도 계획이며 자동 착수하지 않는다.

## 설계와 소유권

- AGENTS의 새 절은 대상·권한의 정본이고 README는 작업 순서·산출물 안내다. README가 로컬 운영 절차를 팀원에게 다시 적용하거나 쓰기 범위를 늘리지 않게 연결한다. INDEX는 탐색 링크만 둔다.
- 기존 `PRD.md`, `FEATURE_MAP.md`, `domains/INDEX.md`와 [open-items 정책 행](../2026-10-01-refactor-record-corrections/open-items.md#후속-후보의-구분)을 재사용한다. 현재 구현으로 확인되지 않은 보상 등을 이미 구현됐다고 단정하지 않는다.
- Rules Astra: goal/CURRENT·계약/근거·Git/PR. Rules Sol: AGENTS/README/INDEX 세 파일과 자기 맥락/보고. Rules 검증자: 자기 맥락/판정/원시 근거만. 동일 파일 동시 쓰기와 작업자 commit/push·추가 위임 금지.
- 적용 CODE_CONVENTION 절은 파일 위치와 이름, 주석과 문서, 역할별 적용, 변화 평가다. 원문 전체와 기준 SHA를 위임 계약에 고정한다. Markdown 전용 언어 절은 없으며 기존 문서 표·상대 링크 관례를 따른다.

## 작업 공간과 진행

- 경로 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`, branch `docs/teammate-onboarding`, base `955002a932925ff2c4ac81f4a5a99f2024a4b9b2`(fetch한 origin/main).
- 기존 `docs/management-e2e-backlog`의 로컬 체크포인트 `000c6e2`, `5c23c0103512978dc6d55b8927eff13985c0ebca`는 원래 브랜치에 보존했다. 이 PR에 포함하지 않으며 처리는 하네스 재개 때 정한다.
- Run `run_de39b9ad84a8`, coordinator `term_608e0cad-db6f-4d7b-a3d7-9437fb139317`, incarnation `c8c926b0-c741-45b8-bed7-eb73281c4acb`, runtime `c4c45d49-1185-4356-8565-59e73ab1080b`, Orca 1.4.220. 메인에게 현재 Run 회신 주소를 전달했다. 이 값은 이번 관측이며 재진입 때 새로 확인한다.
- 모델: 리드 화면 GPT-6-Astra xhigh. 구현 요청 `gpt-6.1-sol max`, 검증 요청 `claude-opus-5-5`; 각 기동 명령/화면을 별도 근거로 남기며 backend는 확인할 수 없으면 unknown이다.
- 모든 로컬 근거의 루트는 `.backups/verification/2026-10-04-teammate-onboarding/`다. Astra 사전 메모는 `astra-context.md`.

## 재개 지점

현재 상태: 첫 독립 실사는 기록 근거 인용 결함 #1로 차단됐다. 해당 goal 문장을 정정했으며 신규 Opus 재검증을 준비한다. 다음 단계는 독립 재판정 → PR/CI 보고 → 사용자 병합 승인 대기다. 병합과 결과 기록이 끝나면 전체 goal Gardener와 종료 점검을 거쳐 R-8을 적용하며 다음 goal을 자동 시작하지 않는다.

보류된 [하네스 goal](../2026-10-03-harness-principles/goal.md)은 별개로 유지한다. 과거 판정·테스트·PR175 성공을 이번 변경의 검증으로 사용하지 않는다.

## 실행 근거

- 구현 계약 `sol-contract.md` v1은 메인 요구 원문과 CODE_CONVENTION 관련 네 절 전체를 포함한다. SHA256 `ad96584f11e233971819e56200cde2600822f2cc132e0b42c8ecf4942df41aeb`.
- 담당 Astra 아래 vertical split으로 `codex --model gpt-6.1-sol -c model_reasoning_effort=max`를 실행했다. `sol-before-attach-show.json`과 `sol-before-attach-read.json`은 신규 pane 경로·branch·화면 GPT-6.1-Sol max와 선택창 없는 첫 화면을 보존한다. 별도로 실행했던 `terminal wait` 응답 원시는 파일로 보존하지 않았으므로 이 근거 묶음에서 당시 readiness 값을 증명할 수 없다. backend unknown.
- 최초 연결 `sol-start.json`: Task `task_9527acad8df6`, Dispatch `ctx_2a6dde3693eb`, terminal `term_37929324-9902-4399-8f4a-352d48c3fd1d`, incarnation `d7a9192e-928c-4df5-8d19-076051382d79`. 접수 `input_accepted`와 실제 시작 `turn_started` 모두 관측했다. attach의 null launch 모델 필드는 모델 미지정의 증거로 쓰지 않는다.
- Sol 완료 `msg_290b8cf122ae`(2026-10-04T14:30:09Z)는 정확 Task/Dispatch·태그·명시 succeeded를 가진다. `sol-completion.json`/`sol-completion-policy.json`과 `sol-report.md`, 실제 diff를 대조했고 파일 쓰기 종료를 확인했다. 자체 점검은 기존 AGENTS 본문 보존, INDEX 한 행 추가, 새 링크 9곳, 범위·공백이며 원시는 `sol-raw/document-selfchecks.json`이다. 아직 독립 판정은 아니다.
- `sol-release.json`은 external_terminal로 retained/processAction none을 반환했다. 동일 incarnation·종료 화면을 `sol-before-close.json`에서 대조한 뒤 `sol-close.json`으로 이 작업 pane만 닫았다. 완료 세션은 재사용하지 않는다.

## 첫 실사와 기록 정정

- 신규 `claude --model claude-opus-5-5` 세션은 화면 Opus 5.5 xhigh, backend unknown이었다(`review-before-attach-read.json`). Task `task_72956dc35a36`/Dispatch `ctx_0c138d5d3eed`의 `review-start.json`은 접수와 실제 시작을 보존한다. 완료 `msg_6c726565dbe9`(2026-10-04T14:44:12Z, `review-completion.json`)의 outcome은 failed이며 원문은 `verdict.md`다.
- 판정은 안내 3문서의 요구 충족을 확인했지만, 이 goal이 show/read JSON에 없는 readiness 값을 해당 파일에서 확인했다고 연결한 문장을 결함 #1로 차단했다. Astra는 `msg_4d58b587a3ac`로 메인에 즉시 보고했다. 위 실행 근거를 실제 보존 내용으로 좁히고 wait 원시 미보존을 명시했다. 이전 실행을 새 실행·전사로 소급 증명하지 않으며 첫 판정·입력은 그대로 보존한다.
- 비차단 #2: Sol 보고의 `onboardingFinalLocators` label은 원시에 없고 해당 명령/결과는 label 없는 `documentChecks[4]`에 있다. 원래 보고는 보존한다. 비차단 #3: 외부 세션의 절별 규칙·스킬 적용과 README 변경 경계 상세는 승인 범위 밖인 다음 Rules 「운영 정본 반영」 goal의 입력이다. 이 goal에서 새 정책·검사를 구현하지 않는다.
- 첫 검증자는 입력 hash 원시를 자기 사전 메모보다 먼저 쓴 절차 편차도 공개했다. 첫 결과를 최종 통과로 사용하지 않으며, 신규 검증자는 원시 파일을 포함한 첫 쓰기 전에 자기 메모를 작성하고 현재 고정 입력을 독립 실사한다.
- 첫 검증 세션은 완료 identity·판정 원문을 대조한 뒤 정산(retained/external_terminal, `review-release.json`)하고 동일 incarnation `58d2ea3d-47d3-47bc-ab46-017752a47d88`의 pane만 닫았다(`review-before-close.json`, `review-close.json`). 재사용하지 않는다. 같은 결함 #1의 확정 실패는 이번 한 회이며 Sol의 안내 구현 실패로 이중 집계하지 않는다.
- 메인 `msg_4eb82c2222fc`(2026-10-04T14:46:04Z, `main-correction-decision.json`)는 판정 원문과 보고의 일치를 확인하고 위 정정·신규 Opus 재검증·통과 뒤 PR/CI 보고를 지시했다. #2는 비차단 기록, #3은 이번 완료조건을 막지 않으면 후속 입력으로 넘기며 범위 확대 승인은 따로 필요 없다고 정했다.
