# 2026-10-01 운영 규칙 문서 정리

## 목표와 범위

2026-10-01 운영에서 확정한 결정과 관측 사실 R-1~R-8을 현재 운영 문서에 반영한다. 메인 Claude가 전달한 사용자 승인 범위이며 전달 메시지를 사용자 직접 입력으로 격상하지 않는다. 승인 원문은 로컬 `.backups/verification/2026-10-01-operations-rules/main-request.json`과 `main-request-r8.json`에 보존한다.

- 새 규칙 정본: [ORCA 운영 문서](../../../00_Document/operations/ORCA.md). 다른 현재 문서와 프로젝트 스킬은 정본 링크를 사용하고 상충하는 기존 설명을 제거한다.
- 대상: `AGENTS.md`, `CLAUDE.md`, `00_Document/operations/{ORCA,RESUME,CURRENT}.md`, `.agents/skills/dawnholder-goal-loop/`의 본문·Orca 위임 지침, `.agents/skills/dawnholder-session-handoff/SKILL.md`, 이 goal.
- 제외: `05_Management/RESUME.md`, 제품·테스트 코드, SQL·Unity·게임 실행, Claude/Codex 설정 변경, 새 Fable 시범 목표 실행, PR 병합.

## 완료조건과 보존 계약

1. R-1~R-8이 ORCA 문서의 각각 고정 anchor에 있고 사용자 결정·관측·한정 시범·미확인 범위가 구분된다.
2. AGENTS·CLAUDE·RESUME·관련 스킬의 진입 링크가 유효하고 이전 배치/미검증 설명과 모순되지 않는다. 새 규칙 상세를 여러 문서에 복제하지 않는다.
3. 메인 CLAUDE 쓰기 종료와 Sol 쓰기 종료 후 신규 외부 Opus가 실제 diff·근거·권한 경계를 실사한다. 필수 결함은 새 Sol/Opus 세션으로 수정·재검증한다.
4. goal에 검증 원문·실제 수행/미수행·정산을 기록하고 PR을 만든다. 각 PR 병합은 사용자 명시 승인 대기다.

Unity 사용자 3파일의 skip-worktree·내용과 stash 2개를 보존한다. 두 checkout의 `.claude/settings.local.json`은 읽기 전용이며 기초 해시는 evidence의 `settings-before.json`에 있다. 고정 보관 브랜치는 변경하지 않는다.

## 소유권과 실행 계획

- 메인 Claude: `CLAUDE.md` 단독 작성, 사용자 결정·최종 원문 확인·승인 전 원천 표본 대조.
- GameDev Astra: goal/CURRENT, 설계·범위·외부 위임, Git·PR·결과 통합. 같은 파일의 동시 쓰기와 작업자 commit/push를 금지한다.
- 신규 GameDev Sol (`gpt-6.1-sol`, xhigh): CLAUDE·goal·CURRENT를 제외한 허용 운영 문서와 스킬 수정. 작업 하나 후 정산·종료.
- 신규 GameDev 검증자 (`claude-opus-5-5`): 문서 정적 실사와 로컬 verdict만 작성. 제품 문서 수정 금지. 구현자와 분리하며 작업 하나 후 정산·종료.

최초 실행 명령·화면 모델과 백엔드 확인값을 구분한다. 백엔드 실제 모델을 확인할 수 없으면 `unknown`이다. 문서 실사는 코드·DB·게임 실행 검증을 대신하지 않는다.

## 현재 상태

- 기준: `origin/main` = `e06a74b90fd570807fd34c95193e8368ab109623` (PR157 병합).
- checkout: `C:/Dev/DawnHolder_Project`, branch: `bass131/operations-rules-20261001`.
- Orca Run: `run_b807dbc5e6af`. 이전 목표의 Run/Task/Dispatch를 새 작업 권한으로 재사용하지 않는다.
- 2026-10-01: 메인 R-1~R-8 요청 수신, 정본·파일 소유권 확정. 메인 `msg_3cbdcc8c99c2`로 CLAUDE 쓰기 종료. 자동 로딩되는 CLAUDE에는 메인 의무 한 줄과 정본 anchor를 남기고 상세 규칙은 ORCA에만 둔다는 메인 보완 설명을 수신했다.
- Sol: Task `task_3c9cb392e40c`, Dispatch `ctx_ee89e4f2875d`; 새 pane split 후 최초 연결. `sol-1-start.json`에 ready·input_accepted·turn_started를 보존했다. 화면 `GPT-6.1-Sol xhigh`, backend `unknown`. `msg_bed347ea543e`로 쓰기 종료·worker_done succeeded를 받았다. 원문은 `sol-1-report.md`다. 요청 모델은 `sol-1-spec.txt`, 확인 모델은 화면 표시가 근거다. 최초 split 명령 원문은 별도 로컬 파일로 보존하지 않았고 split receipt에는 command 필드가 없으며 attach launch값은 null이다.
- Sol 보고와 실제 허용 6파일 diff를 대조했다. 자체 점검의 109개 상대 링크·57개 anchor 참조·고정 anchor 8개 확인은 독립 실사를 대신하지 않는다. 정확한 runtime·incarnation 대조 후 release(retained/external_terminal)·close(ptyKilled=true)를 완료했다. 근거: `sol-1-before-close-show.json`, `sol-1-release.json`, `sol-1-close.json`, `sol-1-done.json`.
- Sol이 R-3의 CLI 제약을 발견했고 Astra·메인이 `reply --help`로 대조했다. 메인 `msg_fc7e6335130c`의 보완 결정은 `main-r3-decision.json`에 보존하며 실행 가능한 회신 절차의 정본은 [R-3](../../../00_Document/operations/ORCA.md#r3-reply-tag)에 둔다. CLAUDE 요약은 정본 링크와 함께 유지한다는 메인 지시다.
- 근거 루트: `.backups/verification/2026-10-01-operations-rules/` (Git 제외).
- 메인과 Sol 모두 쓰기 종료. 규칙 본문을 `08665adf971b1a9e553439288c2c6ba5fb35caf3`에 고정해 독립 실사를 마쳤고 [PR158](https://github.com/bass131/dawnholder-server/pull/158)을 생성했다. **문서 수정·독립 실사는 완료, PR 병합은 사용자 명시 승인 대기**다. CI는 PR checks의 해당 HEAD 결과를 확인한다.

## 결과와 인계

- 신규 외부 Opus `claude-opus-5-5`의 문서 정적 실사: **PASS, 필수 결함 0건, 비차단 관찰 O-1~O-8**. 대상 `08665ad`, base `e06a74b`, 9파일 +173/−28. 원문: `.backups/verification/2026-10-01-operations-rules/verification-1/verdict.md`. Astra는 원문 전체를 읽고 메인에게 경로와 주요 관찰을 전달했다. 메인의 승인 전 원천 표본 대조는 별도 메인 기록을 따른다.
- 독립 검사: 9문서 상대 링크 114개·anchor 참조 59개·고정 규칙 anchor 8개, 문제 0. 실제 diff·요구 원문 4건·CLI 1.4.218 help·관련 과거 goal/receipt·보존 상태를 대조했다. 규칙 상세는 ORCA, 다른 문서는 정본 링크로 연결하며 CLAUDE의 최소 의무 한 줄은 메인 결정대로 유지했다.
- 검증 Task `task_0cca592e2180`, Dispatch `ctx_8afd5711433a`; `opus-1-start.json`에 ready·input_accepted·turn_started를 보존했다. 요청은 spec의 `claude-opus-5-5`, 화면 `Opus 5.5 xhigh`, backend `unknown`. 최초 split 명령 원문 별도 파일은 없고 attach launch값은 null이다. 첫 화면 16행과 종료 화면 53행(`limited=false`)에서 선택창은 관측되지 않았다. 이 화면 관찰을 모든 세션의 설정 효과 보장으로 확대하지 않는다.
- `msg_1ef2ed68c617` worker_done succeeded와 원문을 대조한 뒤 runtime·incarnation을 확인해 release(retained/external_terminal)·close(ptyKilled=true)를 완료했다. 근거는 `opus-1-{before-close-ready,before-close-show,before-close-read,release,close,done}.json`이다. Sol·Opus 모두 작업 하나 후 종료했고 재사용하지 않았다.
- Unity 3파일 내용·skip-worktree와 stash 2개, 두 checkout의 로컬 Claude 설정 해시가 시작 시점과 같음을 실사에서 확인했다. 제품·테스트·설정·Management 파일 변경은 없다. 코드·DB·SQL·Unity·게임·빌드·Markdown 렌더러 및 실제 Management 배치/선택창/교체/blocking 해제 절차는 미실행이다.
- 실사 뒤 변경은 이 goal의 결과·근거 표기·PR 링크 기록뿐이다. 규칙 본문 8파일은 실사 대상과 동일하게 유지한다.

| 비차단 관찰 | 처리·인계 |
|---|---|
| O-1 최초 실행 명령의 근거 위치 | 위 Sol·Opus 기록을 요청 spec/화면 확인·명령 원문 별도 미보존으로 정정했다. 향후 split 직전 명령 원문을 함께 보존한다. |
| O-2 운영 규칙 R-2 안의 과거 목표명 R-2 표현 | 과거 내장 컴포넌트 null 감사 목표를 뜻하며 바로 뒤 링크가 있다. 의미 혼동 가능성은 메인에 전달했고 본문은 실사본을 유지한다. |
| O-3 Git 제외 근거 링크 | `.backups/` 링크는 GameDev checkout 로컬 전용이며 GitHub·다른 checkout으로 자동 배포되지 않는다. 규칙 본문은 링크 없이도 읽을 수 있다. |
| O-4 help 파일의 버전 줄 부재 | 검증자가 버전과 help를 함께 `verification-1/cli-help-observed.txt`에 보존했다. |
| O-5 CLAUDE의 두 역할 정의와 Fable 한정 시범 | 메인 소유 문서의 선택적 보완 사항으로 전달했다. 시범 세션 spec의 모델·쓰기 범위 명시와 기존 정식 라우팅 구분은 유지한다. |
| O-6 `worker-start --agent --model` 대안 제거 | 실패 시 메인 대리 기동으로 보내는 실제 절차 변화다. 메인 R-5 요청에 부합하며 메인에게 명시했다. |
| O-7 `05_Management/RESUME.md`의 이전 배치 잔여 | 이번 범위 밖이고 M-1 뒤 Management 반영 대상이다. 검증자는 Management의 작업 브랜치에는 이미 새 배치가 있음을 읽기 전용으로 확인했다. 병합 완료를 뜻하지 않는다. |
| O-8 CLAUDE 회신 subject 문구 | R-3 정본 링크로 충분하므로 유지한다는 메인 최신 결정대로 둔다. |

Sol 보고의 Astra 후속 `msg_6158a17909cb`는 별도 원문 파일이 없어 검증자에게 미확인이었다. 반영 결과는 `main-r3-decision.json`과 직접 대조해 일치했다. 보고와 실제 수행의 불일치나 미실행 통과 주장은 실사에서 발견되지 않았다.

이번 목표 종료 뒤 Astra 세션 인계는 [R-8](../../../00_Document/operations/ORCA.md#r8-astra-lifecycle)을 따른다. D1b와 M-1 후속 작업은 이 문서 목표의 범위가 아니다.
