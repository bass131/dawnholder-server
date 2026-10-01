@AGENTS.md

<!-- AGENTS.md가 프로젝트 정본이다. CLAUDE.md가 있으면 Claude Code는 AGENTS.md를 자동으로 읽지 않으므로 위 import를 지우지 않는다. 이 파일에는 Claude 세션에만 해당하는 경계만 둔다. -->

## Claude Code 세션의 경계

- AGENTS.md의 모델 라우팅은 Codex 작업자 기준이다. Claude 세션은 사용자가 호출했을 때 감사·조율을 맡는다: 작업 문서와 결과 점검, Astra 메인과의 질의, 사용자 결정 정리.
- 생산 코드와 테스트는 AGENTS.md 라우팅대로 Codex 작업자에게 맡긴다(구현 `gpt-6.1-sol`, 테스트·검증 `gpt-6-astra`). 사용자가 Claude에게 직접 작성을 지시했을 때만 Claude가 쓰고, 보고에 실제 작성 모델을 적는다.
- Claude 서브에이전트(Agent 도구)는 읽기 전용 조사·요약에만 쓴다. AGENTS.md가 요구하는 독립 검증이나 Orca 실행 증거를 대신하지 않는다.
- 사용자 요청 없이 Codex 영역을 수정하지 않는다: `AGENTS.md`, `.agents/`, Codex CLI 설정.
- Codex 세션과 같은 checkout을 쓰면, 쓰기 전에 Orca 메시지로 파일 소유를 나누고 상대의 쓰기 종료를 확인한다.

## Orca로 Codex 세션과 통신

- 명령 문법은 `orca skills get orchestration`의 버전 일치 가이드를 따른다. 터미널 handle은 매번 `orca terminal list --json`으로 확인하고 문서에 고정하지 않는다.
- 질문은 `orca orchestration send --to <상대 handle>`로 우편함에 넣고, 상대가 빈 프롬프트일 때만 `orca terminal send --enter`로 짧게 알린다. 답장은 `$ORCA_TERMINAL_HANDLE`로 받는다.
- 상대가 작업 중이거나 사용자가 프롬프트를 작성 중이면 보내지 않는다. 세부 절차는 `.agents/skills/dawnholder-session-handoff/SKILL.md`를 따른다.
