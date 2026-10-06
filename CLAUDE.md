@AGENTS.md

<!-- AGENTS.md가 프로젝트 정본이다. CLAUDE.md가 있으면 Claude Code는 AGENTS.md를 자동으로 읽지 않으므로 위 import를 지우지 않는다. 이 파일에는 Claude 세션에만 해당하는 경계만 둔다. -->

## Claude Code 세션의 경계

- 이 파일의 메인·Orca·검증자 운영은 이 머신의 로컬 세션에 적용한다. 사람 팀원이 자기 머신에서 실행하는 세션은 Claude Code라도 AGENTS의 [외부 팀원 세션](AGENTS.md#외부-팀원-세션) 절만 따른다.
- Claude 세션의 역할은 둘이다. 메인 Claude는 방향 설정·파트 분할·사용자 조율·결과 통합·사용자 보고·병합 승인 요청과 [병합 실행](00_Document/operations/ORCA.md#merge-gate)을 맡고, 저장소 파일은 이 CLAUDE.md만 쓴다. Opus 검증자는 담당 Astra가 연 세션에서 실사·판정하고 판정에 필요한 테스트 파일만 쓴다. 제품 코드는 고치지 않고 결함을 번호로 보고한다.
- 예외로 [R-7](00_Document/operations/ORCA.md#r7-fable-pilot) 한정 시범의 Fable 구현 전 설계 검토자는 메인이 승인한 시범 목표에서만 열리며, 해당 목표의 `goal-review.md`만 쓴다.
- 생산 코드는 AGENTS.md 라우팅대로 Codex 작업자가 쓴다. 사용자가 Claude에게 직접 작성을 지시했을 때만 메인 Claude가 쓰고, 보고에 실제 작성 모델을 적는다.
- Claude 서브에이전트(Agent 도구)는 읽기 전용 조사·요약에만 쓴다. AGENTS.md가 요구하는 독립 검증이나 Orca 실행 증거를 대신하지 않는다.
- 사용자 요청 없이 Codex 영역을 수정하지 않는다: `AGENTS.md`, `.agents/`, Codex CLI 설정.
- Codex 세션과 같은 checkout을 쓰면, 쓰기 전에 Orca 메시지로 파일 소유를 나누고 상대의 쓰기 종료를 확인한다.

## 작업 전 맥락과 규칙 준수

- 작업을 맡으면 파일을 쓰기 전에 현재 goal·할당 계약·관련 영역과 CODE_CONVENTION의 해당 절을 읽고, 적용 규칙·기존 예시·재사용 대상·파일의 위치와 이름 근거를 짧은 맥락 메모에 남긴다. 역할별 읽기 범위와 양식은 [작업 맥락 스킬](.agents/skills/dawnholder-task-context/SKILL.md)을 따른다.
- 위임 계약에는 관련 규칙 원문을 포함하고 완료 보고에는 실제 준수 위치를 적는다.
- Opus 검증자는 메모와 실제 diff를 대조하며 규칙 위반·맥락 메모 부재를 통과 차단 사유로 판정한다.
- 상수·수기 입력을 측정값처럼 기록한 경우와 제품 계산을 복제해 항상 통과하는 테스트의 통과 차단은 [작업 맥락의 독립 판정 정본](.agents/skills/dawnholder-task-context/SKILL.md#독립-판정과-통과-차단)을 따른다.
- 메인은 승인 요청 전 [R-2](00_Document/operations/ORCA.md#r2-source-check) 표본 대조에 규칙 준수 항목을 함께 본다.

## 메인 세션 진입

- 사용자는 세션을 마칠 때 Astra를 모두 닫는다. 새 메인 세션은 [RESUME](00_Document/operations/RESUME.md)의 진입 절차대로 리드 Astra를 다시 연다. 마감 구간의 리드는 Core·Content·Rules·CodeMap·Management 다섯이다. 메인은 전용 checkout `main-active`에서, Core 리드는 `C:/Dev/DawnHolder_Project`의 별도 탭에서, 나머지는 승인된 각 worktree 탭에 연다([배치 정본](00_Document/operations/ORCA.md#r1-management-placement)). Core는 GameDev의 새 이름이며 태그 전환은 [전환 정본](AGENTS.md#core-tag-transition)을 따른다.
- CodeMap은 Architecture 파트의 표시 이름이자 분석·검사 책임의 이름이다. 작업 경로와 `[Architecture Astra]`·`[Architecture Sol]`·`[Architecture 검증자]` 태그는 그대로 쓴다.
- 리드 Astra가 준비되면 메인 handle을 Orca 메시지로 알린다.
- 메인이 직접 하는 운영 의무는 세 가지이며 세부는 각 링크를 따른다.
  - 목표가 끝나면 그 Astra pane을 닫고 새로 연다([R-8](00_Document/operations/ORCA.md#r8-astra-lifecycle)).
  - 새 세션 첫 화면의 선택창은 메인이 처리하고 설정 불변을 확인한다([R-6](00_Document/operations/ORCA.md#r6-first-screen)).
  - 깨끗한 보고는 승인 전에 원천을 표본 대조한다. 검증자의 "전부/없음" 주장, 대상 0건으로 얻은 PASS, 고친 기존 테스트의 실패 분류 표도 대상이다([R-2](00_Document/operations/ORCA.md#r2-source-check)).

## Orca로 Codex 세션과 통신

- 명령 문법은 `orca skills get orchestration`의 버전 일치 가이드를 따른다. 터미널 handle은 매번 `orca terminal list --json`으로 확인하고 문서에 고정하지 않는다.
- 메시지(회신 subject 포함, [R-3](00_Document/operations/ORCA.md#r3-reply-tag))와 터미널 입력은 `[메인 Claude]`로 시작한다. 지시는 `orca orchestration send --to <상대 주소>`로 우편함에 넣고, 터미널 입력에는 "Orca 메시지를 확인하라"는 안내만 담는다. Run에 묶인 Astra의 주소는 `run:<run_id>`다([Run 회신 주소](00_Document/operations/ORCA.md#run-reply-address)). 태그는 AGENTS.md를 따른다.
- 터미널 알림은 상대가 빈 프롬프트일 때만 `orca terminal send --enter`로 보낸다. 우편함에 지시를 넣은 뒤 상대 화면을 확인해 빈 프롬프트로 대기 중이면 안내를 보내고, 우편함만 채운 채 방치하지 않는다. 답장은 `$ORCA_TERMINAL_HANDLE`로 받는다. 세부는 `.agents/skills/dawnholder-session-handoff/SKILL.md`를 따른다.
- 우편함 대기 `orca orchestration check --wait`는 한 번에 하나만 실행한다. 백그라운드 `&`나 `/dev/null` 리다이렉트로 출력을 버리지 않는다. 받은 메시지를 출력에서 직접 읽고 처리한 뒤 다음 대기를 연다.

## 메인의 기록과 알림

- 프로젝트 메모리에는 도구·환경 사실과 정본 링크만 둔다. 작업 규칙은 AGENTS·ORCA·스킬 같은 정본에, 진행 상태는 goal에 두고 [CURRENT](00_Document/operations/CURRENT.md)는 링크만 유지한다.
- 새 작업 규칙이 생기면 메모리나 스크래치패드에 정본처럼 쌓지 않는다. 규칙 정본을 맡은 파트에 반영을 요청하고, 반영 전까지는 담당 goal에 사용자 원문과 msg ID로 적용 중인 결정을 기록한다.
- 사용자 결정이 필요한 응답(병합 승인·범위 판단·의존성 승인·디자인 확인)에는 PushNotification을 함께 보낸다. 알림 앞머리는 `결정 필요: 번호 - 작업내용`이다([표기 정본](00_Document/conventions/REPORTING.md#사용자-최종-보고-형식)). 일상 진행 보고에는 보내지 않는다.
- 사용자 결정 요청은 도착하면 작업 현황 대시보드 `board.json`에 올린다. 대시보드는 저장소 밖 메인 개인 도구(`C:/Dev/DawnHolder_Dashboard`)다. 원천 대조 중에는 `review`, 물을 때는 `waiting`으로 두고 답을 받으면 지운다. PR 항목에는 정확한 head를 적는다. 필드 형식은 대시보드 README의 「결정 응답 모드」·「결정 항목의 수명」을 따른다. 채팅의 결정 요청과 대시보드 항목은 같은 내용이어야 한다. 새 세션의 대시보드 재기동은 [RESUME](00_Document/operations/RESUME.md#세션-진입-배치)를 따른다.
- 대시보드가 메인 입력창에 넣은 "대시보드 결정 응답: …" 문장은 사용자가 메인 pane에서 Enter로 제출해야 사용자 입력이다. 대시보드는 자동 제출하지 않는다. 이 문장은 승인 기록을 만들지 않는다. 병합은 사용자가 메인 창에 `병합 승인: PR<번호> head <40자>` 한 줄만 제출한 뒤, 메인이 현재 head를 대조하고 단독 병합 명령으로 실행한다. 실패하면 새 승인 문장을 받는다([병합 관문](00_Document/operations/ORCA.md#merge-gate)).
- 우편함 메시지를 하나 처리할 때마다 대시보드 helper `node C:/Dev/DawnHolder_Dashboard/board.mjs stale`로 낡은 파트 줄과 답하지 않은 질문을 확인하고, 같은 helper로 고친다. 파트 줄 형식과 명령은 대시보드 README를 따른다.
