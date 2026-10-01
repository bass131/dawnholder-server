@AGENTS.md

<!-- AGENTS.md가 프로젝트 정본이다. CLAUDE.md가 있으면 Claude Code는 AGENTS.md를 자동으로 읽지 않으므로 위 import를 지우지 않는다. 이 파일에는 Claude 세션에만 해당하는 경계만 둔다. -->

## Claude Code 세션의 경계

- Claude 세션의 역할은 둘이다. 메인 Claude는 방향 설정·파트 분할·사용자 조율·결과 통합·사용자 보고·병합 승인 요청을 맡고, 저장소 파일은 이 CLAUDE.md만 쓴다. Opus 검증자는 담당 Astra가 연 세션에서 실사·판정하고 판정에 필요한 테스트 파일만 쓴다. 제품 코드는 고치지 않고 결함을 번호로 보고한다.
- 생산 코드는 AGENTS.md 라우팅대로 Codex 작업자가 쓴다. 사용자가 Claude에게 직접 작성을 지시했을 때만 메인 Claude가 쓰고, 보고에 실제 작성 모델을 적는다.
- Claude 서브에이전트(Agent 도구)는 읽기 전용 조사·요약에만 쓴다. AGENTS.md가 요구하는 독립 검증이나 Orca 실행 증거를 대신하지 않는다.
- 사용자 요청 없이 Codex 영역을 수정하지 않는다: `AGENTS.md`, `.agents/`, Codex CLI 설정.
- Codex 세션과 같은 checkout을 쓰면, 쓰기 전에 Orca 메시지로 파일 소유를 나누고 상대의 쓰기 종료를 확인한다.

## 메인 세션 진입

- 사용자는 세션을 마칠 때 Astra를 모두 닫는다. 새 메인 세션은 [RESUME](00_Document/operations/RESUME.md)의 진입 절차대로 두 Astra를 지금 보이는 pane의 분할로 다시 연다. 새 탭은 탭 그룹 뒤에 가려지므로 쓰지 않는다.
- 두 Astra가 준비되면 메인 handle을 Orca 메시지로 알린다.

## Orca로 Codex 세션과 통신

- 명령 문법은 `orca skills get orchestration`의 버전 일치 가이드를 따른다. 터미널 handle은 매번 `orca terminal list --json`으로 확인하고 문서에 고정하지 않는다.
- 메시지와 터미널 입력은 `[메인 Claude]`로 시작한다. 지시는 `orca orchestration send --to <상대 handle>`로 우편함에 넣고, 터미널 입력에는 "Orca 메시지를 확인하라"는 안내만 담는다. 보낸 사람 태그 규칙은 AGENTS.md를 따른다.
- 터미널 알림은 상대가 빈 프롬프트일 때만 `orca terminal send --enter`로 보낸다. 상대가 작업 중이거나 사용자가 프롬프트를 작성 중이면 보내지 않는다. 답장은 `$ORCA_TERMINAL_HANDLE`로 받는다. 세부 절차는 `.agents/skills/dawnholder-session-handoff/SKILL.md`를 따른다.
