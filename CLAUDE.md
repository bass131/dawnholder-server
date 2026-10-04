@AGENTS.md

<!-- AGENTS.md가 프로젝트 정본이다. CLAUDE.md가 있으면 Claude Code는 AGENTS.md를 자동으로 읽지 않으므로 위 import를 지우지 않는다. 이 파일에는 Claude 세션에만 해당하는 경계만 둔다. -->

## Claude Code 세션의 경계

- Claude 세션의 역할은 둘이다. 메인 Claude는 방향 설정·파트 분할·사용자 조율·결과 통합·사용자 보고·병합 승인 요청을 맡고, 저장소 파일은 이 CLAUDE.md만 쓴다. Opus 검증자는 담당 Astra가 연 세션에서 실사·판정하고 판정에 필요한 테스트 파일만 쓴다. 제품 코드는 고치지 않고 결함을 번호로 보고한다.
- 예외로 [R-7](00_Document/operations/ORCA.md#r7-fable-pilot) 한정 시범의 Fable goal 검토자는 메인이 승인한 시범 목표에서만 열리며, 해당 목표의 `goal-review.md`만 쓴다.
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

- 사용자는 세션을 마칠 때 Astra를 모두 닫는다. 새 메인 세션은 [RESUME](00_Document/operations/RESUME.md)의 진입 절차대로 GameDev Astra를 메인 pane의 분할로, Management Astra를 Management worktree 탭에, Architecture Astra를 Architecture worktree 탭에 다시 연다([배치 정본](00_Document/operations/ORCA.md#r1-management-placement)).
- 세 Astra가 준비되면 메인 handle을 Orca 메시지로 알린다.
- 메인이 직접 하는 운영 의무는 세 가지다. 세부는 각 링크의 정본을 따른다.
  - 목표가 끝나면 그 Astra pane을 닫고 새로 연다([R-8](00_Document/operations/ORCA.md#r8-astra-lifecycle)).
  - 새 세션 첫 화면의 선택창은 메인이 처리하고 설정 불변을 확인한다([R-6](00_Document/operations/ORCA.md#r6-first-screen)).
  - 깨끗한 보고는 승인 전에 원천을 표본 대조한다. 검증자의 "전부/없음" 주장, 대상 0건으로 얻은 PASS, 고친 기존 테스트의 실패 분류 표도 대상이다([R-2](00_Document/operations/ORCA.md#r2-source-check)).

## Orca로 Codex 세션과 통신

- 명령 문법은 `orca skills get orchestration`의 버전 일치 가이드를 따른다. 터미널 handle은 매번 `orca terminal list --json`으로 확인하고 문서에 고정하지 않는다.
- 메시지(회신 subject 포함, [R-3](00_Document/operations/ORCA.md#r3-reply-tag))와 터미널 입력은 `[메인 Claude]`로 시작한다. 지시는 `orca orchestration send --to <상대 주소>`로 우편함에 넣고, 터미널 입력에는 "Orca 메시지를 확인하라"는 안내만 담는다. Run에 묶인 Astra의 주소는 `run:<run_id>`다([Run 회신 주소](00_Document/operations/ORCA.md#run-reply-address)). 보낸 사람 태그 규칙은 AGENTS.md를 따른다.
- 터미널 알림은 상대가 빈 프롬프트일 때만 `orca terminal send --enter`로 보낸다. 상대가 작업 중이거나 사용자가 프롬프트를 작성 중이면 보내지 않는다. 우편함에 지시를 넣은 뒤에는 상대 화면을 확인해, 빈 프롬프트로 대기 중이면 안내를 보낸다. 우편함만 채우고 대기 중인 세션을 방치하지 않는다. 답장은 `$ORCA_TERMINAL_HANDLE`로 받는다. 세부 절차는 `.agents/skills/dawnholder-session-handoff/SKILL.md`를 따른다.
- 우편함 대기 `orca orchestration check --wait`는 한 번에 하나만 실행한다. 백그라운드 `&`나 `/dev/null` 리다이렉트로 출력을 버리지 않는다. 받은 메시지를 출력에서 직접 읽고 처리한 뒤 다음 대기를 연다.

## 메인의 기록과 알림

- 프로젝트 메모리에는 도구·환경 사실과 정본 링크만 둔다. 작업 규칙은 AGENTS·ORCA·스킬 같은 정본에, 진행 상태는 goal에 두고 [CURRENT](00_Document/operations/CURRENT.md)는 링크만 유지한다.
- 새 작업 규칙이 생기면 메모리나 스크래치패드에 정본처럼 쌓지 않는다. 규칙 정본을 맡은 파트에 반영을 요청하고, 반영 전까지는 담당 goal에 사용자 원문과 msg ID로 적용 중인 결정을 기록한다.
- 사용자 결정이 필요한 응답(병합 승인·범위 판단·의존성 승인·디자인 확인)에는 PushNotification을 함께 보낸다. 알림 앞머리는 `결정 필요: 번호 - 작업내용`이다([표기 정본](00_Document/conventions/REPORTING.md#사용자-최종-보고-형식)). 일상 진행 보고에는 보내지 않는다.
