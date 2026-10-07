# 개발·검증 도구

패킷 생성, 헤드리스 통신 시나리오와 WSL 실행을 제공한다.

- [sync-wsl.sh](sync-wsl.sh): 원본별 동기화·빌드·테스트·서버·봇 실행.
- [PacketGenerator/Program](PacketGenerator/Program.cs): PDL에서 패킷 코드 생성.
- [PDL](PacketGenerator/PDL.xml): 패킷 정의 원본.
- [headless-bot/Program](headless-bot/Program.cs): 시나리오 선택·실행.
- [Scenarios](headless-bot/Scenarios/): 기능별 통신 검사.

[도구·검증 계약](../00_Document/domains/tooling.md)과 [정확한 명령](../00_Document/operations/DEVELOPMENT.md)을 따른다. 생성기는 Shared 소스를 변경하고 서버·봇은 7777 포트를 사용한다. 봇은 ClientNet·Shared에 의존하므로 계약 변경 후 함께 확인한다.

## Orca 메시지 수신 판정

[Orca/message-policy.mjs](Orca/message-policy.mjs)의 `evaluateMessage(input)`은 활성 Dispatch 작업자 메시지의 순수 판정이고 [Orca/check-message.mjs](Orca/check-message.mjs)는 로컬 UTF-8 JSON 파일을 읽어 JSON stdout을 내는 CLI다. Node 표준 라이브러리만 쓰며 외부 의존성 설치가 없다.

```powershell
node 99_Tools/Orca/check-message.mjs .backups/수신입력.json
```

입력은 `message`(원시 Orca message 객체)와 `expected`(coordinator가 현재 receipt/worker-show로 별도 확인한 `fromHandle`, `taskId`, `dispatchId`, `tag`)다. payload는 JSON 문자열이나 객체를 그대로 제공한다. identity를 메시지에서 유도하지 않는다. subject/body의 누락·null·공백은 빈 텍스트이고 비문자 값은 입력 오류다. 빈 heartbeat의 `alive` 표식, 내용 있는 메시지의 태그와 지원 문맥은 [ORCA 정본](../00_Document/operations/ORCA.md#dispatch-message-policy)을 따른다.

Orca1.4.218 공식 blocking ask의 `Question` 예외를 쓸 때만 coordinator가 질문 receipt에서 확인한 `expected.officialAsk = { messageId, cliVersion: '1.4.218' }`를 추가한다. 실제 id/thread/dispatch 발신/payload.question도 대조하지만 **일반 send가 모양을 흉내낼 수 있으므로** 이 근거를 메시지에서 만들지 않는다. 근거 부재·지원 버전 변경이면 예외를 열지 않고 현재 CLI와 사람 대조로 돌아간다. body 태그와 세 identity 일치는 계속 필수다.

결과는 `{ status, exitCode, exception, diagnostics }`이며 진단마다 `{ code, path, message, repair }`가 있다. exit0 `allowed`, exit1 `policy-violation`, exit2 `input-error`(형식/필수 identity/지원 문맥·파일/JSON/도구 실패)로 구분한다. stdout JSON은 기계 소비용이고 수정·보고 안내를 담는다. 파일/네트워크 쓰기·메시지 전송·ack·Orca 수명주기 변경·프로세스 강제 종료는 없다.

일반 Main terminal-only 메시지, blocking reply의 기존 subject 예외, 다른 message type과 원격 진위 검증은 이 helper의 범위 밖이다. 허용 결과만으로 런타임 진위나 worker_done 정산을 증명하지 않는다. 독립 회귀는 신규 Opus 소유 `99_Tools/Orca.Tests/message-policy.test.mjs`이며 기존 [code-rules workflow](../.github/workflows/code-rules.yml)의 별도 단계가 부재/load 실패/nonzero도 실패 처리하고 stdout/stderr/exit를 기존 artifact 폴더에 보존한다. 자체 smoke와 신규 Opus 판정·원격 CI는 별도 근거다.

## 병합 관문

[MergeGate/claude-hook.mjs](MergeGate/claude-hook.mjs)는 인자 없이 stdin hook JSON을 읽는다. `.claude/settings.json`이 `node "$CLAUDE_PROJECT_DIR/99_Tools/MergeGate/claude-hook.mjs"`를 세 사건에 등록한다. PreToolUse matcher는 `Bash|Monitor|Write|Edit|MultiEdit|NotebookEdit|CronCreate|ScheduleWakeup|SendMessage|RemoteTrigger`, PermissionRequest matcher는 `Bash`다. 상태는 `$CLAUDE_PROJECT_DIR/.claude/state/merge-gate/`의 `main-checkout`과 `approvals/<session_id>.json`이며 Git 제외다. Node 표준 라이브러리만 사용하고 모든 결과의 exit는 0이다. 순수 정책은 [merge-policy.mjs](MergeGate/merge-policy.mjs), bash 글 가리기는 [inert-text.mjs](MergeGate/inert-text.mjs), 기록 I/O는 [approval-store.mjs](MergeGate/approval-store.mjs)로 나눈다.

| 결과·코드 | 의미 |
|---|---|
| `approved PR<번호>` / PermissionRequest allow / 결정 없음 | 사용 기록 저장 뒤 통과 JSON / 같은 명령의 확인 허용 JSON / 빈 stdout |
| `not-main-checkout` · `subagent` · `no-approval` | 메인 표식 없음 · 하위 에이전트 · 해당 세션/PR 승인 없음 |
| `head-mismatch` · `expired` · `already-used` | 승인 head 불일치 · 30분 만료 · 이미 소비한 기록 |
| `compound-command` · `forbidden-flag` · `missing-match-head-commit` · `bad-form` | 복합·감싼 명령 또는 다른 명령·환경 변수 할당 뒤 병합 · 금지 플래그 · 별도 head 인자 없음(`=` 형태 불가) · 맨 앞 gh 명령 낱말의 단독 형태 불일치(낱말 사이 `-R`·`--repo` 옵션·대문자·경로·`.exe` 등; 그 밖의 값 옵션은 마지막 그물로 차단) |
| `non-bash-merge` | Monitor의 병합 시도는 승인 기록이 있어도 차단; Bash 단독 명령만 통과 경로가 있음 |
| `api-merge` · `push-main` · `branch-lookup-failed` | API 병합 · main 목적지 push · 입력 cwd의 branch 조회 실패 |
| `protected-path` · `approval-injection` | 상태 경로 접근 · 다른 터미널이나 예약·전달 prompt 도구의 입력(중첩 문자열 포함)에 승인 문장 주입 |
| `suspect-words` | 단독 병합 형태가 아닌 셸 명령의 가린 문자열(실행기가 있으면 원문)에 병합·main push 의심 낱말 동반; 병합은 단독 명령으로, 다른 명령은 나눠 실행하거나 문구를 파일로 넘김 |
| `invalid-input` · `state-write-failed` | 입력/도구 해석 실패 · 사용 기록 저장 실패; 모두 deny JSON과 원인·수리 안내 |

순차 호출 계약이다. 같은 세션의 기록 쓰기가 겹치면(병합 시도끼리, 승인 문장 제출과 병합 시도) 두 번 통과하거나 소비된 기록을 되살릴 수 있어, 실패한 병합을 30분 안에 새 승인 없이 재시도할 여지가 있다. 되살아나는 기록은 원래 승인된 PR·head에 한정된다.

셸 판정은 두 자리의 글만 가린다. 따옴표 구분자 heredoc 본문은 소문자 `cat`·`tee`나 표준 입력을 메시지로 받는 `git commit -F -`·`--file -`·`--file=-`에 보내고 출력 파이프·명령 치환 밖일 때 가린다. 따옴표 인자는 `echo`·`printf`, `git commit`의 `-m`·`--message`, `gh pr create/edit`의 `-t`·`--title`·`-b`·`--body`, `orca orchestration send/reply`의 `--subject`·`--body`만 가린다. `=` 값은 git의 `--message`와 gh의 네 옵션만 받으며 orca는 별도 값만 가린다. 큰따옴표 안 치환은 중첩까지 코드로 남기고 변수 대입이 붙은 값은 가리지 않는다. `echo`·`printf`는 명령 전체의 파이프가 `tee cat head tail wc cut grep sort uniq` 밖 명령으로 흐르면 가리지 않는다(`||`·`&&`는 파이프가 아니다). 읽기 실패·프로세스 치환은 전체를 원문으로 두고 here-string은 가리지 않는다.

가린 문자열에 실행기 낱말이 남으면 전체를 원문으로 되돌린다. 마지막 `/` 뒤 이름의 `.exe`를 뗀 뒤 비교하는 목록은 `bash sh zsh dash ksh fish pwsh powershell cmd wsl`, `node nodejs deno bun bunx npx npm pnpm yarn tsx python python3 py perl ruby php lua awk gawk java dotnet cscript wscript mshta make`, `source eval exec xargs env sudo nohup timeout command builtin start call`이다. `./`·`../`·`~/`로 시작하는 낱말과 명령 낱말 `.`·경로도 실행기로 본다. 가린 글 안의 낱말은 셈하지 않는다.

병합 시도·API 병합·push와 마지막 그물은 가린 문자열을, 승인 문장 주입·상태 경로·병합 통과 조건은 원문을 읽는다. 정밀 판정은 줄 이음 접기와 경계/치환 묶음 읽기를 유지한다. 정밀 차단이 먼저며 정확한 단독 병합은 그물을 건너뛰고 표식·하위 에이전트·승인 기록을 판정한다. 나머지는 gh·merge, gh·api·병합 낱말, push·main 대상 낱말 동반을 branch 조회 전에 막는다(main 대상 비교는 맨 앞 `+`를 뗀다). 따옴표 없는 제목(`gh pr create --title merge --body text`), 같은 줄의 main과 작업 branch push, 실행기가 있는 명령의 문구도 막히므로 나눠 실행하거나 문구를 파일로 넘긴다. 한계는 입력 출처 미구분·Codex 미적용·문자열 판정의 의도적 우회(별칭·변수/치환 값·스크립트 파일, 실행기 목록 밖의 git hook·인터프리터)다. 등록 밖 도구·hook/settings 자체 수정·하위 폴더·settings 부재·hook 미실행은 보호하지 않는다. 셸 상태 경로의 구분자·`/./`는 접지만 `..`는 풀지 않는다.

독립 회귀는 `node --test 99_Tools/MergeGate.Tests/*.test.mjs`다. [code-rules workflow](../.github/workflows/code-rules.yml)의 `Run independent MergeGate regressions` 단계는 시험 부재/load 실패/nonzero도 실패 처리하고 `$RULES_OUTPUT/merge-gate-independent-tests/`에 command/stdout/stderr/exit를 보존한다. 운영 절차·지원 한계는 [병합 관문 정본](../00_Document/operations/ORCA.md#merge-gate)을 따른다.
