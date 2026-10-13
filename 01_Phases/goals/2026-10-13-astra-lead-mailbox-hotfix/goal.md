# 리드 Astra 전환과 우편함 수신·Codex 병합 금지

리드 모델과 대기 방식을 바꾸고, Codex의 지원 도구 진입에서 병합·원격 main 갱신을 막는 프로젝트 가드를 만든다. 문서 전환과 가드 구현은 별도 PR이다. 규칙·운영 판은 V1.0을 유지한다.

## 재개 지점

- 메인이 범위 초안 v1을 승인했다(`msg_f6fcab954e76`, 2026-10-13T10:12:35Z). 승인 원문은 아래 「적용 중인 사용자 결정」과 구분된 메인 판단이다.
- 작업 공간: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`. 현재 PR2 branch `feat/codex-merge-guard-20261013`, 생성 당시 최신 main/base `44bd4319f78078f6fcc3f4941590f6371d58e2b7`. PR1 branch는 `docs/astra-lead-mailbox-20261013`, 당시 base는 `9c731f7cc2182724b757d71ab7d28d0dbd6c0b23`였다.
- 근거 E: `.backups/verification/2026-10-13-astra-lead-mailbox-hotfix/`(Git 제외). 맥락 메모 `E/lead-context.md`, 승인 초안 `E/scope-draft-v1.md` SHA256 `49b705fb39c087396010b7c4637f39a840f5f44f25de738221985c023d638aa0`, 조사 `E/research/findings.md`.
- 현재 차례: PR234는 메인이 사용자 승인 뒤 병합했다(`msg_56377f9a74a8`, merge `44bd4319…`). A 후속 관측은 실제 압축 미관측으로 부분 종료했다. PR2 첫 수정 `83b6f3d4`의 신규 Opus 재검증도 **NOT PASS**다. #1~#9 수정은 확인됐지만 새 차단 결함 #10~#18이 확정됐고, 검증자는 정산·종료했다. 메인의 호스트 재불일치 조건에 따라 다음 제품 수정은 판단 전 보류한다. 실제 Codex 진입과 Linux CI는 미실행이다. Art 기본 파트 추가 결정은 즉시 적용하되 별도 문서 PR은 PR2 병합 뒤이며 재계획 초안/담당 문구 두 판을 메인에 보낸다. 기존 후속 기록·Archify/Unity 정합도 유지한다(`E/pending-next-doc-records.md`, `pending-backlog-archify.md`, `pending-next-doc-unity-permission.md`).
- Run `run_54319375ed08`은 이번 goal의 현재 namespace다. 회신 `run:run_54319375ed08`. handle·runtime·incarnation은 사용할 때 다시 확인하며 이전 Rules goal의 ID는 권한으로 쓰지 않는다.

## 진척 단계

- [x] 범위와 기준 승인
- [x] 리드 규칙 작성
- [x] 문서 재실사와 부분 관측 정리
- [x] 문서 PR 병합
- [>] 가드 신뢰 경계 — 소스 조사 완료, 실제 UI/신규 세션 유지 미실행
- [>] 가드 구현과 검증 — 첫 재검증 NOT PASS 새9건, 후속 수정은 메인 판단 대기
- [ ] 가드 PR 병합
- [ ] 종료 점검과 기록
- [ ] 종료 확인과 교체

## 범위

Art 기본 파트 문서 PR 추가 결정은 아래 사용자 결정6에 기록했다. 다섯 범위 요소의 재계획 초안은 `E/art-formal-part-replan-v1.md`이며 메인 대조·담당 문구 두 판의 사용자 판단 전이다. 현재 PR2 뒤 최신 main의 별도 문서 PR이고 아래 기존 PR2 기준은 유지한다.

### 만들 것

1. 모든 기본·승인된 추가 파트 리드를 신규 `gpt-6-astra` xhigh(Codex CLI), `[<파트> 리드 Astra]`로 전환한다. CodeMap의 Architecture 경로/태그 이름과 `[<파트> 구현 Astra]`를 구분한다. R-1의 최초 기동·R-8의 같은 대화 `codex resume <session-id>`·rollout cwd/id/model 대조를 맞춘다. 현재 anchor·역사 기록은 보존한다. Claude 리드 전용 autocompact 명령을 Codex 세션 옵션으로 교체하고 Claude 분류기/권한 문장은 Claude 세션에만 좁힌다.
2. Codex 리드는 우편함용 `check --wait`·sleep·반복 조회·terminal wait를 열지 않는다. 알림/안내로 깨면 대기 없는 현재 Run check로 전체 묶음을 받고 처리·ack한다. 할 일이 남으면 계속하고 답을 기다리면 턴을 끝낸다. heartbeat-only이면 다른 도구 없이 한 줄로 끝내고 ack를 다음 자연스러운 check에 합친다. 기동·종료·지시 전 확인, worker_done 대조·정산·해제, heartbeat 5분과 정산 측정은 유지한다. 출력은 E에 저장해 읽는다. 기존 Claude background 대기·중복 깨움 방지·메모리 회수 후 재기동 금지는 Claude 세션에만 남긴다.
3. Codex에 메일을 보낸 쪽은 화면을 한 번 확인하고, 자동 알림으로 턴이 시작되지 않은 빈 prompt에만 자기 태그와 Orca 메시지 확인 안내를 보낸다. busy·모달·작성 중 prompt와 accepted 뒤 침묵의 재전송 금지, 공식 계약 receipt/draft 절차는 유지한다.
4. 기존 MergeGate·SessionGuard가 Claude Code 전용임을 구분한다. PR1에서는 Codex가 두 hook 밖이라는 사실을 유지하고, PR2 검증 뒤 새 가드의 지원 범위만 기술 보호로 바꾼다. Codex의 출력/쓰기 가드 이식은 BACKLOG 후보 한 행에만 둔다.
5. 10-31 검증자 시범 평가와 BACKLOG에 전환 뒤 리드 토큰을 10-05 기준선과 rollout token_count로 비교하고 비용이 다시 커지면 사용자에게 올리는 안건을 넣는다. 조건이 다른 수치를 같은 추세로 단정하거나 절감률·새 측정 도구를 만들지 않는다.
6. 프로젝트 `.codex/hooks.json`의 PreToolUse 등록과 Codex용 명령 판정·시험을 만든다. `gh pr merge`(auto 포함), gh api의 PR merge/auto-merge 변경 호출, 원격 main/기본 branch를 갱신하는 git push를 막는다. 옵션·경로·refspec·기본 push 목적지·all/mirror·삭제 등 같은 동작을 대조한다. 로컬 git merge·작업 branch push·gh pr create/view/checks·일반 조회는 보존하고, 문구에 단어가 있다는 이유의 오탐을 만들지 않는다. 해석 가능한 명시 명령·셸 compound/wrapper가 대상이며 동적 실행·별칭·외부 스크립트·재사용 셸 stdin·다른 클라이언트 우회까지 완전 차단하는 권한 시스템은 아니다. 최소 요구의 누락을 한계로 면제하지 않는다. 기존 Claude hook/settings/승인 저장소는 건드리지 않는다.
7. 최신 메인 계획 `msg_58a7c1654b0c`에 따라 리드 신규 기동은 `codex --model gpt-6-astra -c model_reasoning_effort=xhigh`, 재개는 같은 대화 `codex resume <session-id>`에 같은 모델·effort를 사용한다. 문맥 창·압축 기준 override는 명령에서 빼고 두 키는 사용자 결정으로 조정하는 수단으로만 설명한다. 조정 검토 근거는 압축 뒤 맥락 손실로 일을 다시 한 사례다. 10-11의 25만/50만 결정은 당시 Claude 세션으로 좁힌다. 최초 실사 세션의 1000000/250000 요청과 usable828400 관측은 역사 근거로만 남긴다.
8. 설치 버전 공식 소스의 기본 압축 한도와 실제 발동 조건을 구분하고, 신규 Astra 관측자 한 명이 저장 harness로 작은 A/B 실험을 한다. A는 창만64000으로 줄이고 압축 기준은 기본값, B는 기본 창에 압축 기준40000이다. 실제 읽기 등으로 채운 뒤 자동 압축 직전·직후 token_count/이벤트를 기록한다. 두 실험의 합계 입력은 대략30만 이하로 제한하고 강제 압축·큰 창 채우기·전역 변경은 하지 않는다. 소스와 큰 차이가 나면 메인에 즉시 보고한다. PR1 수정 차례 안에 끝나면 정본/PR에 한 줄 결과, 늦으면 진행 중으로 적고 PR2/종료 기록에 반영한다.
   메인 후속 `msg_d681c449a2ce`에 따라 B의 다음 턴 실제 발동·한 턴 내 일시 기준 초과를 PR1 정본 한 줄에 반영한다. A 미관측은 보존하고 PR1 생성 뒤 병합 승인 대기 중 신규 Astra 한 명으로 더 작은 창24000/소스 기본 기준21600·입력8만 이하의 A만 재시도한다. 결과는 PR2 또는 종료 기록에 넣으며 PR1을 막지 않는다.

### 건드릴 곳

| 경로 | 쓰는 사람 | 경계 |
|---|---|---|
| `AGENTS.md` | PR1 문서 Sol | 리드 라우팅·태그·전환. Git 권한 유지 |
| `00_Document/operations/ORCA.md` | PR1 문서 Sol, PR2 구현 Astra 순차 | R-1·우편함·R-8·hook 적용/한계 정합. anchor 유지 |
| `.agents/skills/dawnholder-goal-loop/SKILL.md` | 문서 Sol | 10-31 리드 토큰 평가 안건 |
| `.agents/skills/dawnholder-session-handoff/SKILL.md` | 문서 Sol | 발신 측 화면 확인과 중복 안내 방지 |
| `00_Document/operations/RESUME.md` | 문서 Sol | 로컬 리드 메모 순서·Claude 기술 가드 구분 |
| `05_Management/RESUME.md` | 문서 Sol | Management 리드 태그 한 곳 |
| `CLAUDE.md` | 메인 단독 | Architecture 리드 태그·Codex 수신자 조건부 안내. Sol 종료 뒤 쓰기 |
| `README.md` | 소개 한 문장은 리드(msg_2aff25585c91), 추가 두 표현은 신규 Sol(msg_cd0af5d1a4a5) | AI 협업 역할과 「프로젝트 전용 스킬」·「현행 운영 명령」 정합. 새 재실사에 포함 |
| `00_Document/operations/BACKLOG.md` | 리드 | 지정 다섯 행·평가 행과 후속 후보, 독립 지적 #1의 주장 정정 |
| `E/doc-fix/`, `E/compact-experiment/` | 신규 Sol / 신규 Astra 각각 | 저장 검사 harness·재실행 / 소규모 자동 압축 관측. 이전 근거 불변 |
| `00_Document/operations/CURRENT.md` Rules 줄·이 goal | 리드 | 기준·진척·결정·결과. 타 파트 보존 |
| `.codex/hooks.json` | PR2 구현 Astra | 신규 프로젝트 hook 등록. config.toml은 범위 아님 |
| `99_Tools/CodexGuard/codex-hook.mjs`, `command-policy.mjs` | PR2 구현 Astra | 신규 어댑터/판정. 추가 파일은 같은 책임 내 계약에 고정 |
| `99_Tools/CodexGuard.Tests/*.test.mjs` | 선행 시험 Opus·독립 Opus 순차 | 요구 시험/검증. 실제 세션 원시는 E |
| `99_Tools/README.md`, `.github/workflows/code-rules.yml` | PR2 구현 Astra | 신규 진입/지원 경계와 회귀 단계 하나. 기존 정책 불변 |

CodexGuard는 MergeGate·SessionGuard의 책임별 폴더 관례를 따른다. 기존 MergeGate는 승인 소비와 메인 허용 경로를 소유하므로 그대로 호출하거나 리팩토링하지 않는다. helper 재사용은 계약·오탐을 비교해 이유를 남기되 기존 공유 모듈 쓰기를 열지 않는다. commit/push는 Rules 리드 한 명만 한다.

### 하지 않을 것

- 원격 PR 병합·자동 병합 예약·main push와 실제 외부 쓰기 실험. 전역 Codex/Claude/Git 설정·업데이트·신뢰 우회 플래그·현재 리드 자기 재기동.
- 기존 `.claude/settings.json`·MergeGate·SessionGuard 코드/승인 상태 변경, Codex의 메모/출력/파일 쓰기 가드 구현.
- 서버·Unity·DB·프로토콜·공유 DLL·대시보드·메인 개인 도구 쓰기, 다른 파트 goal과 과거 기록·Orca.Tests 역사 태그 변경.
- heartbeat 간격 변경·우편함 필터·지속 운영 실행 helper·새 비용 측정기. 할당 근거 폴더의 저장 검증/관측 harness는 필요 범위에 포함한다. 기존 구현자/검증자·Gardener/Advisor/선행 시험 배정 변경. 판 올림·다음 goal 자동 착수.

orca-work·implementer-routing은 AGENTS/R-1을 참조하므로 그대로 둔다. helper-session의 Claude 메인 수신 안내, R-5 작업자 명령/Claude TEMP, CODE_CONVENTION·task-context의 과거 역할 표현도 이번 리드 전환을 이유로 일괄 정리하지 않는다.

### 관찰 가능한 완료조건

1. 허용 diff 안에서 현재 리드 모델·태그·기동·같은 대화 재개·추가 파트 규칙이 맞고 역사 기록·기존 Claude 경계가 보존된다.
2. 신규 문서 실사자가 진입·worker_done·heartbeat-only·무알림 시나리오에서 무대기 수신/전체 처리/ack/조건부 안내를 실제 문서로 찾아 대조한다. 문서가 미래 자동 준수를 보장한다고 쓰지 않는다.
3. 메인의 CLAUDE 쓰기 종료·diff가 최종 실사에 포함되고, 리드 기본 문맥 명령·사용자 조정 경계와 최초 실사 관측의 한계가 남는다. 작은 압축 관측은 저장 harness/원시와 소스의 한도·실제 발동을 구분해 보고하며, 늦으면 PR1에 미완료 상태를 숨기지 않는다.
4. 가드 최소 차단/허용 표본을 자동 회귀와 실제 신규 Codex 세션 모두에서 확인한다. 가짜 gh/git 또는 안전한 로컬 fixture로 deny 전 실행 표식 없음·허용 표식/출력을 남기고 실제 병합/push는 없다. stdin 직접 시험만으로 실제 진입 PASS를 내지 않는다. 신뢰·오류/timeout·잘못된 JSON·재사용 셸·동적 명령 한계를 공개하며 정상 명령 오탐은 통과하지 않는다.
5. 신규 가드 CI 회귀와 실행 원시가 있고 기존 Claude 파일/승인 경로 보존을 diff·관련 회귀로 확인한다. 실제 메인 병합을 시험하지 않는다.
6. 리드와 실사자가 후보 도착 검사(`node 99_Tools/Backlog/check-candidates.mjs --backlog 00_Document/operations/BACKLOG.md --goal 01_Phases/goals/2026-10-13-astra-lead-mailbox-hotfix/goal.md`)를 각각 실행한다. 최초 출처·7필드·안정 ID·미완료 후보를 보존한다.
7. PR에 변경 정본 목록·정착 확인 명령·판정 원문·미실행/위험을 넣고 최종 head 재실사·메인 R-2·PR별 사용자 승인·전체 종료 Gardener/기록·R-8을 거친다.

## PR 경계와 검증

| PR | 산출물 | 배정·검증 |
|---|---|---|
| 1 - 리드 모델·수신·문맥 정본 | 항목 1~5·7의 문서/관리 기록, 항목8은 같은 차례 완료 시 결과 한 줄. `docs/astra-lead-mailbox-20261013` | 문서 실사. 신규 Sol max 문서·저장 자체검사 수정과 신규 Astra xhigh 관측 병행 → 신규 Astra xhigh 재실사. 제품/지속 도구 코드는 없으며 TDD 비해당. 임시 근거 harness는 실행 파일로 보존 |
| 2 - Codex 병합 금지 가드 | 항목 6 코드·등록·시험·CI/설명. PR1 병합 뒤 최신 main에서 `feat/codex-merge-guard-20261013` | 강(보안 입력/실행 도구). 신규 Opus 선행 요구 시험/실패 → 신규 Astra xhigh 구현(신호 1·3) → 신규 Opus 독립 시험/실제 진입 |
| 종료 기록 | 전체 결과·Gardener/후보·종료 점검 하나 | 신규 Opus Gardener와 바뀐 기록 독립 실사 |

R-7은 메인이 지정하지 않았다(아래 판단 4). 중간 점검은 PR1 실측·실사 뒤와 PR2 신뢰/설계 경계 확인 뒤다. PR1 승인 대기 중 PR2 선행 시험 계약을 준비할 수 있지만 PR2 branch는 PR1 병합 뒤 만든다. 작업자·검증자는 한 작업 후 정산/종료하고 수정·재검증은 새 세션이다.

정착은 병합 뒤 안전 지점에서 main을 받은 각 worktree에서 `git diff --stat origin/main -- <해당 PR 변경 정본 파일들>`로 확인한다. PR 본문에는 실제 파일 목록을 넣는다. 문서 bytes/변경 줄 수는 base 대비 원시로 공개하고 앞 goal의 일회성 증가 허용을 재사용하지 않는다.

## 적용 중인 사용자 결정

아래는 **메인이 자기 pane Enter 제출로 확인해 전달한 사용자 원문**이다. Rules pane의 직접 사용자 입력으로 격상하지 않는다.

1. 메인 `msg_f8bd6a9085b0`(2026-10-13T09:54:10Z), 사용자 18:3x KST: 「나 그리고 돌아왔어 이어서 진행하자」, 이어 「잠깐, 그전에 리드급을 아스트라로 변경하고, 당시에 아스트라가 Orca 메일대기 때문에 낭비되던 토큰 사용방식을 개선하는 방향으로 룰 핫픽스 진행해줘」.
2. 메인 `msg_3345bd69d295`(2026-10-13T09:58:30Z), 사용자 19:0x KST: 「그러면 병합 금지 훅을 한번 셋업해주고, 잦은 압축같은 경우에는 Codex 모델들도 컨텍스트 메모리 크기를 커스텀 할 수 있어」. 앞 지시의 「Codex 기술 가드는 BACKLOG만」을 가드 구현으로 바꾼다.
3. 메인 `msg_fbf9b79c0a77`(2026-10-13T10:18:18Z): 「대시보드 결정 응답: 1) 리드 재개 시점 - 문서 PR 병합 뒤 바로 열지, 병합 금지 가드 PR까지 기다릴지 → A 문서 PR 뒤 바로」. 메인은 PR1 병합 뒤 Core·Content·Art·World 리드를 Astra로 연다. PR2 전까지 병합 금지는 규칙 경계이며 PR1 우선과 실제 관측으로 확정한 R-1 명령을 유지한다.
4. 메인 `msg_58a7c1654b0c`(2026-10-13T11:10:23Z): 「아마 273K로 GPT 모델들의 컨텐스트를 제약한게, Tibo라는 OpenAI 테크리드가 이야기하기를 제일 효율적인 토큰사용량을 가져서 그렇게 설정한거라는데, 성능적인거도 그렇고, 그부분도 감안해서 최대 Context 메모리를 세팅하는게 좋을꺼야」, 「그리고 Compaction이 생각보다 정해진 퍼센트에 정확하게 진행하는지는 확인된게 없어서 확인이 필요해」. 효율성·성능 설명은 사용자 전언이며 원출처를 검증한 사실이 아니다. 기본값 사용과 작은 실험의 구체 방식은 아래 메인 적용 판단이다.

5. 메인 `msg_3a48680901db`(2026-10-13T14:06:36Z), 사용자 23:0x KST: 「그리고 아무래도 매번 넣기 불편하니까 필요한 워크트리에는 계속 세팅해주는게 좋겠어」. **메인 해석**은 content-active·world-active·art-active 세 곳의 Git 제외 `.claude/settings.local.json`에서 `permissions.allow`에 `mcp__unity-mcp__Unity_RunCommand` 한 항목만 상시 허용하는 것이다. Core·Rules·CodeMap·Management, 다른 MCP/Bash 허용은 제외한다. 메인 `msg_6302a1f52a46`은 사용자가 자기 pane에서 명령을 실행했고 메인이 14:20:57Z에 세 파일과 Git 제외를 확인했다고 보고했다. content SHA256 `791ab2fd62cff891e8e2ac95bfd3b0fc0c8859c3b704c9c666191f674c201ec0`, world/art 각각 `cceb092d846438ec1f97037fa65cfcceaf1076f18a451b816fcb5126c138c9d9`다. Rules의 설정 쓰기나 독립 확인 실적이 아니다. 원시·기존 내용 보존·다음 문서 PR의 좁은 정합 범위는 `E/pending-next-doc-unity-permission.md`에 있다.

6. 메인 `msg_13ca12fccdc4`(2026-10-13T17:49:49Z)가 자기 pane Enter 제출로 확인해 전달한 사용자 원문은 다음 두 줄이다.
   - 요청: 「아트쪽또 정신 파트로 편입시켜도 좋을거 같은데 갱신좀 해줄래?」. 메인은 「정식 파트 편입」으로 이해했고 결정 화면에도 그렇게 표시했다.
   - 결정: 「대시보드 결정 응답: 1) Art 정식 파트 편입 - 규칙 문서에 지금 반영할지, 다음 Rules 목표로 미룰지 → A 이번 Rules 목표에 문서 PR 추가」.
   Art는 지금부터 기본 여섯 번째 파트이며 목표 종료 뒤 R-8로 새 목표를 열고 새 메인 진입에도 포함한다. World는 목표 한정 추가 파트다. 현재 PR2 가드 완료·병합 뒤 최신 main에서 별도 문서 PR을 만든다. 담당 문구의 추가 후보는 아래 메인 보충과 재계획 초안에 두며 사용자 확정 전이다. Rules pane의 직접 사용자 입력으로 격상하지 않는다.

### 메인 적용 판단과 승인

- Art 추가 문서 PR 재계획은 `msg_13ca12fccdc4` 승인 초안과 대조한다. 메인 보충 `msg_dec1c5141657`(17:52:10Z)은 Art 담당 문구를 승인 초안과 수정 후보 두 판으로 제시하도록 했다. 수정 후보는 표시 전용 코드까지 명시하는 차이가 있어 메인 대조 뒤 사용자가 판단한다. Art 기본 파트 결정은 적용하되 문구 선택을 승인된 것으로 소급하지 않는다.

- 진입 메시지: 리드급은 기본 다섯·승인 추가 파트 모두이며 사용자 모델 전환 지시가 AGENTS 변경 승인을 겸한다고 해석했다. 25만 압축·재작업 1회 뒤 50만 복귀를 Codex 세션 옵션으로 맞추는 것은 메인 판단이다.
- `msg_0a7edf480a02`(09:59:51Z): READY 뒤 종료는 진입 한 번만이었다. 남은 일이 있으면 계속하고 답을 기다릴 때만 턴을 끝낸다.
- 범위 승인 `msg_f6fcab954e76`(10:12:35Z, E/check-20261013-04.json): 메인이 초안 150줄 전체를 대조하고 누락/범위 밖 없음으로 승인했다. PR 분리/branch·CLAUDE 제안도 승인했다.
- 판단 1: PR1 문서, PR2 가드, 종료 기록 PR 경계 유지.
- 판단 2: 「지원 도구 진입의 금지 명령 가드」 경계 승인. **PR2 구현 계약 전** hook 신뢰 저장 위치/유지 기간/프로젝트·세션별 범위, worker-start Sol·검증자·리드의 각각 확인 여부, 화면 없는 작업자의 중단 여부를 조사해 메인에게 올린다. 전역 파일에 정상 UI 신뢰 기록이 생기는 것과 수동 편집을 구분한다. 세션마다 사람 확인이 기동을 막으면 구현 전에 보고한다.
- 판단 3: 문맥 옵션 1000000/250000을 PR1 실사에서 관측한다. 상한 절삭이면 실제 표시 최대값/250000으로 리드가 확정한다. 272000 불변·압축 미적용·거부만 다시 올린다.
- 판단 4: R-7 미지정. 메인 전달에 따르면 마지막 시범 자리는 Core 컨테이너 PR에 배정됐다(`persistence-integration/goal.md` 21행의 `msg_446a9239bf3e`). 이 goal은 별도 Fable 없이 강 검증한다.
- CLAUDE는 문서 Sol 쓰기 종료 뒤 메인이 같은 branch의 해당 두 문장만 쓰고 종료를 알린다. 다른 파트 리드 재개 시점은 메인이 사용자와 정하며 이 goal 범위를 바꾸지 않는다.
- `msg_ffae972fdb0b`(11:06:44Z)는 창872000/압축250000·실제 압축 발동 미관측을 허용하고 리드 ack 주장 오류 제거/새 실사를 승인했다. **문맥·압축 부분은 뒤의 msg_58a7c1654b0c가 대체한다.** 오류 제거 승인과 이전 판정 보존은 유지한다.
- 최신 `msg_58a7c1654b0c`(E/check-after-context-decision.json)는 기본 문맥/압축 설정 사용, 두 키의 사용자 조정 경계, Claude 역사 결정 구분, 신규 Astra 한 명의 작은 A/B 실험·두 실험 합계 입력 대략30만 이하·저장 harness/원시·큰 차이 즉시 보고를 정했다. 기존 1000000/250000 실사자는 그대로 마치며 새 런타임 전환으로 보고하지 않는다. PR1을 오래 막지 않으며 지연 시 실험 결과만 PR2/종료 기록으로 넘긴다.
- `msg_132ccdcee6f7`(11:06:16Z)의 「리드 Astra 안착 뒤 검증자 Opus 복귀」는 조건부 다음 후보다. 사용자 원문은 「만약에 아스트라 리드를 성공적으로 안착시키면 예전처럼 검증자를 Opus 5.5를 써도 됄 것 같은데」이며 현재 시범 변경 승인이 아니다. BACKLOG의 해당 후보에 메인 제안 기준을 둔다.
- `msg_2aff25585c91`(11:42:07Z, E/check-notification-20261013-03.json)은 README 소개의 AI 협업 한 문장을 현재 메인 Claude/파트 리드 Codex 역할로 정합하도록 PR1에 추가했다. 메인 판단이며 세부 모델명·숫자·성과 추가와 그 밖의 README/CURRENT World 변경은 제외한다. 발행 계약에 새 범위를 넣지 않고 메인의 명시 허용에 따라 리드가 한 문장을 수정해 새 재실사 입력으로 넘긴다.
- `msg_d681c449a2ce`(11:56:54Z, E/check-before-recheck-launch.json)는 B의 명시40000 기준 초과 뒤 다음 턴 실제 압축 관측을 정본 한 줄에 넣고 A는 PR1 생성 뒤 작은 창/입력8만 한도로 재시도하도록 정했다. 메인 판단이며 사용자 직접 결정으로 격상하지 않는다. 관측의 설치 버전·조건·미관측 범위를 구분한다.
- `msg_cd0af5d1a4a5`(11:59:58Z, 같은 원시)는 README65·91의 「프로젝트 전용 Codex 스킬」→「프로젝트 전용 스킬」, 「현행 Codex 명령」→「현행 운영 명령」 두 표현만 추가했다. 원천은 보조 pane의 사용자 원문 「지금은 Claude를 메인 세션으로 쓰고 있어」·선택 A를 보조 msg_3cfc9943c11c가 메인에 전달한 사실이다. 메인 판단으로 수신했으며 재실사가 아직 미발행이어서 PR1에 포함한다. 기존14행은 보존한다.

## 현재 결과

### 진입과 승인

- Codex CLI 0.162.1, Orca 1.4.224, 화면 GPT-6-Astra xhigh, backend unknown. 최초 HEAD da717247·미커밋 0, 원격 main 9c731f7c를 대조했다. 조사 요약 hash는 메인 지정값과 같았다.
- 새 Run 생성 09:59:03Z(E/run-create.json), READY `msg_19e3f9ab3252`(09:59:31Z), 범위 초안 `msg_f09ff5c217eb`(10:11:34Z), 승인 위 메시지.
- Run 미바인딩 최초 메일의 40초 무알림은 메인 관측이다. Run 바인딩 후 `msg_0a7edf480a02`의 자동 알림으로 Rules 턴이 시작된 것은 직접 관측이다. created 09:59:51Z/delivered 09:59:52Z(E/check-20261013-02.json)를 사람의 읽기 지연으로 해석하지 않는다.
- 공식 문서에서 프로젝트 PreToolUse·문맥 키를 확인했다. 설치 번들/캐시 Astra는 context_window 272000, max_context_window 872000이다. `--strict-config`와 debug 조합은 지원되지 않아 exit1이며 성공 근거가 아니다. 상세/원시는 E/research/findings.md와 source-manifest.json이다.
- 승인 뒤 최신 origin/main에서 PR1 branch 생성(E/pr1-fetch.txt, pr1-branch.txt). 아직 가드·새 문맥 세션 적용·독립 실사·CI는 미실행이다.

### PR1 문서 작성과 리드 기록

- 첫 기록 commit `497d72420f3dcd64ac0e3f22614e5b5d2eee222a`는 goal과 CURRENT의 Rules 줄만 담았다. 문서 구현 Task `task_6875328cef32`, Dispatch `ctx_b3ad135d8bdd`, 계약 E/doc-impl-contract-v1.md(SHA256 `77e0a3d4c1538484695f670db9e4f0931691e88af4ead745d486cf3782ff10f9`)로 Sol에 여섯 문서를 맡겼다. 최초 명령 `codex --model gpt-6.1-sol -c model_reasoning_effort=max`, 화면 GPT-6.1-Sol max, backend unknown.
- `worker-start`는 입력 접수 뒤 `turn_start_unobserved`였다. R-5의 현재 동일성·다른 입력 없음·draft 크기 조건을 대조해 텍스트 없는 Enter 한 번을 보냈다. draft 소멸과 Working, 실제 제출 user 기록을 확인했다. 계약의 CRLF 하나가 LF 둘로 변환되어 빈줄 한 개가 늘었으며, CR→LF 전송 변환 후 전문은 일치한다. 바이트 일치 실패 원시와 차이를 E/doc-impl-submitted-contract-check.json·transport-check.json에 보존하고 메인 `msg_2b8e88fd6808`로 알렸다. 실제 실행은 관측했지만 초기 receipt의 unknown 상태를 성공 receipt로 고쳐 쓰지 않았다.
- BACKLOG의 여섯 지정 행은 최초 ID·출처·조건을 보존하면서 이번 부분 정합을 연결했고, 출력/쓰기 기술 가드 후보 `codex-session-write-guard`를 등록했다. Jev·helper·heartbeat 간격 변경은 여전히 대기다. 내용 실사와 새 문맥 기동 관측은 아직 독립 검증 전이다.
- 리드의 기존 후보 검사는 첫 실행 exit1(두 번째 후보의 `BACKLOG <id>` 표기 누락), 해당 goal 문장 교정 뒤 동일 명령 exit0이었다(E/backlog-candidate-check.txt·after.txt). diff 공백 검사는 exit0이며 독립 실사를 대신하지 않는다. 10:34:02Z 작업자 관측은 Working 8m13s/lastHeartbeatAt=null이어서 기존 5분 신호 계약을 `msg_d7397f2bc3c1`로 안내했다. 작업자 `msg_7d72a5b5496c`의 반증을 실제 우편 원시와 대조하니 10:27:04Z·10:32:30Z·10:35:22Z에 같은 from/Task/Dispatch의 heartbeat가 있었다. projection null을 신호 부재로 해석하지 않는다. 관측 구간의 메시지 간격은 326초·172초이며 전자는 300초 초과다(E/doc-impl-heartbeat-source-audit.json·gaps.json). 이 부분 관측을 최종 전체 정산으로 대신하지 않는다.
- Sol은 6문서 쓰기 종료 status `msg_e7967b5a835b`(10:45:38Z)를 보냈다. E/doc-impl/report.md·context.md와 실제 diff를 읽었고 summary-data.json의 종료6파일 hash를 현재 파일과 대조해 일치를 확인했다(E/doc-impl-end-hash-check.json). 6파일 추가50/삭제23줄·bytes +6410·참조 자체점검은 구현자 보고이며 독립 PASS가 아니다. 추가 원시 heartbeat-03→04는344초로300초를44초 초과했다. 메인 CLAUDE 인계는 `msg_418351426364`(10:46:50Z), 실사 계약은 E/doc-review-contract-draft.md로 준비했으며 아직 미발행이다.
- 최종 Sol 보충 `msg_322f3056e4fe`와 worker_done `msg_5461dd979639`(10:47:31Z)의 from/Task/Dispatch·6개파일·보고를 대조했다. worker-show는 completed/succeeded였고 release는 external_terminal/retained여서 R-5의 같은 작업 pane 확인 뒤 해당 terminal만 닫았다(E/doc-impl-done-show.json·release.json·close.json). 부모 리드 생존도 확인했다. 수정에 이 세션을 재사용하지 않는다.
- 메인 `msg_4ec72d6ae5e7`(10:47:42Z)은 CLAUDE.md의 태그·발신 안내 두 문장 쓰기 종료를 알렸다. 실제 SHA256 `db7eda36a24e7db8aee219d06bacbad3d1f6c171497e68721927044be0ba270b`, 8690bytes와 메인 보고가 일치하고 Claude background 대기 문장은 보존됐다(E/claude-write-end-check.json). 해당 입력은 독립 실사에 포함한다. PR push/생성·문맥 실측·독립 판정은 아직 미완료다.

### PR1 첫 실사와 첫 수정 차례

- 독립 실사 Task `task_0b8683fe42c1` / Dispatch `ctx_a5a02bdb21c8`, 고정 HEAD `96df2ea2c41428180c398465dda21960fb05a007`, 계약 E/doc-review-contract-v1.md. 판정 E/doc-review/verdict.md(SHA256 `2f1c621a637706626244c052179e24c426da44365650fbf270d65617be4aff72`)을 리드가 직접 읽었다. 참조50·보호절7·원문계약10·후보/diff 검사에 지적은 없지만 **NOT PASS #1·#2**다. 결과를 전체 통과로 쓰지 않는다.
- #1은 리드가 승인 원천 없이 검증 계약과 BACKLOG에 중단 ack 복구가 정본화됐다고 쓴 오류다. Sol 구현 계약 요구가 아니므로 Sol 단독 실패로 세지 않는다. #2는 실제 자체검사를 inline 문자열로 실행하고 실행 파일로 보존하지 않은 차이이며, 리드 계약의 하네스 비해당 분류와 Sol 실행에 함께 귀속된다. 두 번호는 현재 산출물 첫 확정1회이며 같은 결과를 worker_done과 중복 집계하지 않는다. 문맥 V1은 미확인/후속 결정 항목이지 Sol 미적용 결함이 아니다.
- worker_done `msg_ac7c10fab878`(11:11:21Z) outcome failed를 현재 identity/Task/Dispatch와 대조했다. worker-show failed/settled, release retained(external), 같은 pane close ptyKilled=true, reclaimable0(E/doc-review-done-show.json·release.json·close.json·settled-list.json). 검증자는 재사용하지 않는다. 마지막 판정 뒤 새 메인 문맥 결정은 다음 계약에만 반영한다.
- 이번 첫 수정은 리드의 BACKLOG 주장/goal 원천 정정과 신규 Sol의 ORCA 기본 명령/저장 harness 재실행이다. 승인 근거와 완료 주장·계약을 바로잡는 층을 택하고 첫 실수로 기록한다. 새 반복 규칙/일괄 lint를 만들지 않는다. 이전 inline 실행을 저장 파일로 수행했다고 소급하지 않는다.
- 최초 문맥 관측은 요청1000000/250000, usable828400이며 설정상 상한872000×95%와 일치한다(E/doc-review/session-observations.json). 실제 자동 압축/큰 입력 성공은 미관측이다. 공식 설치 버전 소스의 기본 압축 한도90%와 usable95%는 별개다(E/research/context-config-findings.md·source manifest). 실제 발동은 작은 실험에서 구분할 예정이며 아직 결과가 없다.
- 기존 Sol 최종 생존 helper는 신호6개 구간에서 최대344초, 300초 초과326·344초 두 구간, exit1이었다(E/run-doc-impl-liveness.ps1·doc-impl-final-liveness.json·execution.json). 첫 신호 전·마지막 신호 후·ID 없는 status까지 측정했다고 확대하지 않는다.
- 첫 수정 계약은 기준 commit `828d8d2ac81e9e5538c86a1ad5f15a9b24e485af`에서 발행했다. Sol Task `task_3d6b9611d483` / Dispatch `ctx_c6a734efa764`는 E/doc-fix-contract-v1.md에 따라 ORCA R-1/R-8과 E/doc-fix를 소유한다. 별도 관측 Astra Task `task_9d659be9b266` / Dispatch `ctx_24993f08a170`는 E/compact-experiment-contract-v1.md에 따라 E/compact-experiment만 소유한다. 입력15/17개와 각각 출력부모를 저장 E/check-fix-contract-paths.ps1로 확인했다. 독립 재실사는 아직 발행하지 않았다.
- 두 최초 명령/화면은 각각 gpt-6.1-sol max·gpt-6-astra xhigh, backend unknown이다. 둘 다 turn_start_unobserved receipt 뒤 같은 R-5 조건(새 pane·동일 incarnation·공식 입력만·draft=계약+4788자)을 확인하고 bare Enter 한 번씩 보냈다. 실제 시작과 계약 대조 원시는 E/doc-fix-*·compact-experiment-*에 보존하며 작업 완료/통과로 해석하지 않는다.
- README 역할 정합은 PR1에 포함한다. 리드가 메인 msg_2aff25585c91의 지정 한 문장만 바꿨고 다음 독립 실사에서 원천·소유·실제 diff를 대조한다. 메인 목표·결정·병합 승인 요청과 파트 리드의 구현/독립 검증 조정을 구분했으며 다른 소개 문장·모델 세부명·수치·성과는 추가하지 않았다.

### PR1 수정 결과와 압축 부분 관측

- 수정 Sol의 쓰기 종료 `msg_b447c52cff6e`, worker_done `msg_2a4a96b41e31`(11:44:43Z)은 succeeded다. E/doc-fix/report.md를 직접 읽고 ORCA SHA256 `53f2b0ee8bdb753729a741ca3a9024fe6a583c0a2a7e5aa7ba253b26340fa8b7`과 저장 inspect-documents.mjs hash를 대조했다. 실제 저장 파일 실행 exit0, 자체 참조51/보호절7/규칙9·예상 밖 경로0, ORCA 1파일 +7/-5줄은 E/doc-fix/document-observations-01.json·execution-01.json의 결과이며 독립 PASS가 아니다. 이전 inline 실행 원시는 그대로 보존했다.
- 관측 Astra의 쓰기 종료 `msg_f0fbf3e2add0`, worker_done `msg_fb86c1f6c9b3`(11:44:05Z)은 **failed/partial**이다. E/compact-experiment/report.md(SHA256 `5428cb0827c72e67514c377d1a89d93029b7371510ff962b6cb84b5d2163cdc2`)와 result.json을 읽었다. 연구 요구 일부 미관측에 따른 부분 종료이며 제품 결함 확정 실패 횟수에 합치지 않는다.
- B는 기본 창/명시 압축40000에서 직전 응답 last total40376, 다음 턴의 실제 compacted/ContextCompaction 이벤트를 관측했다. 압축 요청 input40396과 compaction_response_id가 원시에서 연결된다. 압축 직후39826은 재추정이며 실제 후속 요청 input39644와 구분한다. 기준 초과376(0.94%)는 이 한 사례이고 정확한 연속 임계점/보편적 오차나 성능을 뜻하지 않는다. 공식 설치 대응 소스의 pre-turn 검사 위치와 부합하는 관측이다.
- A는 창64000/기본 압축의 소스 기준57600, usable60800에서 한 요청 뒤 last total55645였다. **발동은 미관측**이며 기본90% 발동 실증으로 보고하지 않는다. 이 세션의 추가 모델 실행은 중단했고 A 관측은 미완료로 남는다. 후속 메인 msg_d681c449a2ce에 따라 PR1 생성 뒤 별도 신규 Astra의 작은 창/입력8만 실험을 예정하며 그 결과는 PR2/종료 기록에 남긴다.
- 실제 A/B 합계 입력210292(B154661+A55631), cached114176은 입력의 부분집합, output459다. 압축 usage40396을 포함하며 일반 token_count 누적169896만으로 마감하지 않았다. 남은89708로 A의 추가 요청·압축·후속 비용을 제한할 수 없어 중단했다. B의 사전 예약78000 대비 실제80040은2040 초과했으나 전체30만은 넘지 않았다. 예약은 엄격한 런타임 상한이 아니며 관측자 자체 세션 비용·가격·서버 숨은 usage는 이 합계로 검증하지 않았다(E/compact-experiment/result.json·commands.json·원시/receipt).
- 두 작업자 모두 현재 from/Task/Dispatch와 worker-show 완료 상태를 대조하고 release의 external retained 뒤 해당 pane만 close했다(각 E/doc-fix-*·compact-experiment-* done-show/release/close). reclaimable0이며 재사용하지 않는다. 고정 입력을 바꾼 뒤 기존 판정을 재활용하지 않고 새 재실사를 수행한다.
- 정산 생존 검사 E/run-fix-wave-liveness.ps1·fix-wave-final-liveness.json·execution.json은 exit1이다. 첫 검증자 신호10개/최대268초, 관측 Astra9개/최대365초, 수정 Sol5개/최대382초(303·310·382초 초과 구간)다. 5분 전체 준수를 주장하지 않으며 첫 신호 전/마지막 신호 후·ID 없는 신호는 포함하지 않는다.
- 추가 세 문구는 신규 Sol Task `task_e231912e836c` / Dispatch `ctx_df250d2b5dc2`, 계약 E/doc-addendum-contract-v1.md(SHA256 `413885260d086170d090598cd34d1db646c55af51aa22109a49f3d0cc99f209f`)에 맡겼다. 기준 HEAD `aae5cd8e28d62ec78177dc415581c70ce845df56`, 입력17/출력부모1 모두 존재, 최초 명령/화면 gpt-6.1-sol max·backend unknown이다. R-5 조건부 bare Enter1회 뒤 실제 제출 계약의 CR→LF 전송 대조와 Working을 확인했다(E/doc-addendum-*). 쓰기 종료 후 재실사 입력 v2를 고정하며 v1은 미발행 이력이다.
- 추가 Sol writes-end `msg_03f53479c35a`(12:27:44Z), worker_done `msg_54433de2e5da`(12:28:43Z) outcome succeeded를 현재 identity/Task/Dispatch와 대조했다. E/doc-addendum/report.md(SHA256 `82f335cd0d94431c90f198ea7c96a1030e4959772eddbc10b784ba0216200d6b`)·최종 receipt·실제 diff를 직접 읽고 ORCA/README/저장 harness hash를 확인했다. ORCA +1/-1줄·+368bytes, README +2/-2줄·-5bytes이며 지정 구간 밖/README14를 보존한 자체검사 exit0이다. 독립 PASS는 아직 아니다.
- 추가 Sol의 prepare-01 exit1은 리드가 발행 뒤 goal에 Task/Dispatch 기록 한 줄을 쓴 것을 감지한 입력/소유 가정 차이다. 요구 변경 없음과 리드 귀속을 `msg_8c1c9b3d8fa2`로 확인했고 이전/현재 hash·실패 원시를 보존한 prepare-02와 최종 저장 inspection-02는 exit0이다. 다른 예상 밖 변경을 허용하거나 과거 실패를 없애지 않았다. 제품 확정 실패 횟수에 합치지 않는다.
- 추가 Sol worker-show completed/succeeded→release external retained→같은 pane close ptyKilled=true를 확인했다(E/doc-addendum-done-show.json·release.json·close.json). E/run-addendum-liveness.ps1·addendum-final-liveness.json은 이 Dispatch 신호7개/최대353초·초과1구간을 확인했고 exit1이다. 첫/마지막 신호 바깥을 포함한 전 구간 준수를 주장하지 않는다. 최종 재실사는 이 절차 이탈과 제품/문서 판정을 구분한다.

### PR2 신뢰 사전 조사

- 메인 요청의 신뢰 저장·유지·checkout/세션 범위 조사는 E/research/hook-trust-findings.md와 공식 `rust-v0.162.1` 대응 소스 원시에 보존하고 `msg_fd2c6560a871`로 보고했다. 정상 TUI 신뢰는 사용자 설정의 hook 경로/위치 키별 해시 상태이며 세션 ID·만료 시간은 없다. 동일 설정·cwd·정의의 새 세션마다 확인하는 구조는 아니라는 소스상 추론이다. 다른 checkout·정의 변경의 재확인, R-6 메인 소유의 최초 picker 처리는 구분한다.
- 실제 hook 등록/UI 승인·새 세션의 신뢰 유지·Windows headless 동작은 미실행이다. 사용자 설정에 정상 UI 신뢰 상태가 쓰이는 것과 에이전트의 전역 수동 편집을 구분하며 후자는 하지 않았다. PR2 가드 구현·실제 진입 통과 근거로 이 조사를 대신하지 않는다.

### PR2 선행 시험과 구현 경계

- 신규 Opus 선행 시험 Task `task_f6bae5117df2` / Dispatch `ctx_8e527d71d1dd`는 계약 `E/guard-pretest-contract-v1.md`와 설계 보충1·2에 따라 시험 8파일을 작성했다. 최초 명령 `claude --model claude-opus-5-5`, 화면 Opus5.5 xhigh, backend unknown이다. `E/guard-pretest/report.md` SHA256 `d1c03e4822f61d43e97b72da8d68efce81d5e04802fb2437f42df49fb5d76c22`와 요구 표를 리드가 직접 읽었다.
- 저장 harness `E/guard-pretest/run-pretest.ps1 -RunLabel run-01`은 기록/합계 대조 exit0, 기준 `node --test "99_Tools/CodexGuard.Tests/*.test.mjs"`는 exit1이다. leaf371은 진입 부재367·등록 부재1·fixture self-check PASS3이며 fixture 오류0·그 밖의 단정 실패0다. 의미 있는 잘못된 판정 검출과 실제 hook 진입은 미실행이다. 의도한 선행 RED를 제품 결함·독립 PASS로 세지 않는다. 리드의 `E/settle-guard-pretest.ps1`은 시험8/8 및 보고 hash 일치를 확인했다.
- Q1은 메인 `msg_1915fbc7b085`의 B: 원격 기본 branch를 확인할 로컬 근거가 없으면 오류 deny하며 대상 원격·사람의 로컬 복구 방법을 안내하고 자동 네트워크 조회/설정 수리는 하지 않는다. Q2~Q5는 리드 보충1(`0829fb5f…`)과 메인의 수용, Q1 원문은 보충2(`d91f3b21…`)에 있다. 보호 범위는 모든 원격의 main/각 기본 branch, 판정 가능한 입력 오류·잡힌 예외도 오류 deny, REST merge 변경/GraphQL merge·enable auto·enqueue는 deny, 조회/disable auto는 보존한다. gh pr merge 명령 자체는 disable-auto도 deny다. 명시 cd·명령 치환·리터럴 eval/iex는 포함하고 EncodedCommand 본문은 제외한다. legacy decision:block도 공식 동등 출력으로 인정하되 정상 allow/명령 rewrite는 하지 않는다.
- writes-end `msg_aa340b5094b6`, worker_done `msg_8ad6a447e6fb` succeeded와 현재 identity를 대조하고 release external retained 뒤 동일 incarnation의 idle pane만 닫았다(`E/guard-pretest-close.json`, ptyKilled=true; reclaimable0). 생존 helper는 공식 reply 원문 포함, 현재 Dispatch 신호11·최대407초·초과1구간·questionWaits0였다. background ask 중에도 신호가 이어졌으므로 질문 전체를 면제하지 않았다. 절차 간격 이탈은 제품 결함 집계와 구분하며 메인에 보고한다.
- 신규 Astra xhigh 구현은 첫 회차, 독립 제품 결함0이다. 신호1(열린 셸/명령줄 입력)·3(Codex hook/Windows·POSIX/Node 경계)에 따라 배정하며 선행 시험을 읽기 전용으로 고정한다. 실제 신뢰 UI·동일 경로 새 세션 유지·실제 exec/code-mode 발동과 Linux CI는 후속 독립 Opus의 강 검증에 남는다.

### PR2 구현 종료와 독립 검증 인계

- 구현 Task `task_2ebfebb5935a` / Dispatch `ctx_960d59bd3bad` / 계약 `E/guard-impl-contract-v1.md` SHA256 `2d0983861ae5eb5e9c9d4866c49f8c2d2a2c5ffb96d405169940371b6cd303ac`. 최초 명령/화면 gpt-6-astra xhigh, backend unknown이다. R-5 동일성·draft18105+4788 확인 뒤 bare Enter1회, 실제 제출 전문 대조와 Working을 확인했다. 최초 receipt의 start_unknown을 ready로 고쳐 쓰지 않았다(`E/guard-impl-submitted-check.json`).
- writes-end `msg_ee636b433f40`와 worker_done `msg_de275a1fd501` succeeded를 대조했다. 보고 `E/guard-impl/report.md` SHA256 `da2e0b73ed025e458a22cdb6cf37db9195490342ddff765750db33b86e8147a4` 전체와 맥락 메모·실제 변경/원시를 읽었다. worker-show completed/succeeded→release external retained→동일 idle pane close ptyKilled=true, reclaimable0이다. 이 구현자를 재사용하지 않는다.
- 구현자 저장 최종 회귀 `raw/impl-03`은 전체434/434·leaf371/371, 기존 Claude 회귀293/293, 추가 정책39/39·등록 wrapper10/10·CI fixture3/3이라고 기록한다. 자체 결과이며 독립 PASS가 아니다. 리드의 저장 `E/settle-guard-impl.mjs`는 선행시험8·제품9·보호입력32·최종회귀입력17의 현재 hash와 근거를 직접 대조해 불일치0·허용 밖 Git 경로0을 확인했다(`guard-impl-lead-settlement.json`). 구현 commit의 diff는9파일 +790/-2다.
- 보충1의 root 조사 판단(`msg_08543dfe1175`, 질문 `msg_3e46aaa42f43`)에 따라 공식 git-root 조회와 Windows wrapper를 사용한다. 보고/context에서 질문ID를 회신ID로 표기한 부분은 원시 질문/실제 회신으로 구분해 후속 검증에 연결한다. 존재하지 않는 프로젝트 환경변수·전역 신뢰 설정을 추가하지 않았다.
- 자체 보완 3회 초과 체크포인트 `msg_41cd9155e902`에 메인 `msg_4a3fc66dd26a`는 같은 범위·모델 계속을 판단했다. 이후 POSIX 분기 cwd 소실/no-space PowerShell else 보완이 이어졌으므로 「명시 정책 구분자·제어흐름/cwd」를 신규 Opus의 필수 확인 항목으로 지정한다(`E/guard-review-required-checkpoint.md`). Windows wrapper 실패 전달도 검증한다. 현재 독립 확정 제품 결함0이며 개발 중 자체 수정과 확정 실패3회를 합치지 않는다.
- 생존 helper는 공식 reply 원문 포함 신호12개·최대403초·초과341/351/403초 3구간·질문 대기174초를 계산했다. full-run exit1이며 현재 Dispatch도 초과다(`E/guard-impl-final-liveness.json`, `liveness-execution.json`). 이를 5분 준수로 보고하지 않으며 제품/독립 판정과 구분해 메인에 전달한다.
- 남은 강 검증은 diff/원시 실사·신규 독립 시험/실행·실제 Codex 정상 trust/같은 경로 신규 세션/exec·code-mode 진입이다. 안전한 가짜 CLI/로컬 fixture로만 명령 실행 전 차단·허용 표식을 관측한다. 필요 TUI 대상은 리드가 승인된 같은 goal의 검증 대상으로 기동하고 최초 선택창은 메인이 R-6으로 처리한다. 검증자 추가 위임·전역 설정 수동 주입·실제 원격 병합/push는 허용하지 않는다. Linux 원격 CI도 별도 실행 결과로 남긴다.

### PR2 첫 독립 검증과 수정 경계

- 검증 Task `task_eab85b90c8bd` / Dispatch `ctx_c54a4a5886e1`, 신규 Opus5.5 xhigh(backend unknown). writes-end `msg_ac126b63223b`·worker_done `msg_5aa8e8d25b84`, 원문 `E/guard-review/verdict.md` SHA256 `b58228644265eee27c4153686f537bf4f4871827edee2e352baff90654d4d0b4`를 전체 읽었다. 대상 구현은 `task_2ebfebb5935a`이고 판정은 **NOT PASS**다. official completed/succeeded는 검증 수행의 정산 상태이며 제품 PASS로 해석하지 않는다. 원래 계약은 outcome 두 값을 나열했으나 판정과의 대응은 명시하지 않았다. 다음 계약에서 실패/보류→failed를 명확히 한다.
- 결함 #1 제어흐름의 cd 위치 소실, #2 공백 없는 catch 본문, #3 PowerShell -Command 옵션 오인, #4 iex -Command 리터럴, #5 heads/main 목적지, #6 --all --prune 삭제, #7 별도 pushurl 원격 작업 branch 오탐, #8 heredoc/here-string·프로세스 치환 오탐, #9 push 없는 계산 cd 오탐이다. 각 번호의 독립 확정 실패는 **1회**이며 개발 중 자체 수리/절차 이탈과 합치지 않는다. #8/#9도 완료조건4 안에서 수정하며 한계 수용으로 완화하지 않는다. #1/#2는 이전 체크포인트의 원인 묶음과 연결하고 가독성 지적도 함께 수정한다.
- 저장 회귀 `guard-review/raw/review-03`은 전체564/통과512/실패52, leaf472/통과436/실패36(기존371/371, 독립101 중36실패), Claude293/293이다. 실패36 leaf는 결함9건으로 분류했고 미분류0이다. 등록 wrapper 공식 runner 형태·Windows URL·if/else/no-space else 기존 수정은 통과했다. 실제 Codex 진입은 차단 결함 해결 전 미실행·판정 보류이며 Linux CI도 미실행이다.
- `E/settle-guard-review.mjs`는 실행입력24·고정입력71·시험6개·verdict hash와 Git 소유를 대조해 불일치0/허용 밖 Git 경로0을 확인했다. 이는 내용 통과가 아니다. worker-show completed/succeeded→release external retained→동일 idle pane close ptyKilled=true로 정산했고 재사용하지 않는다. 생존 helper는 현재 Dispatch 신호17·최대392초·초과1구간·질문대기0, exit1이었다(`E/guard-review-final-liveness.json`·`guard-review-liveness-execution.json`). 조회 원시는 최신100건이며 현재 Dispatch 전체는 포함하지만 Run의 처음부터 끝까지라는 뜻은 아니다.
- 검증자 `probe/git-semantics.mjs`는 지정 TEMP 아래 로컬 bare에 실제 Git push(main 포함)를 실행했다. 하네스·원시의 대상은 해당 fixture이고 외부 원격 변경 근거는 없지만 완료조건4의 「실제 병합/push는 없다」와 충돌하는 절차 이탈로 남긴다. 이후 직접 Bash 실행은 Claude 관문에 막혔고 우회하지 않았다는 보고다. 리드가 소급 승인하거나 제품 실패에 중복 합산하지 않는다. 후속은 변경 명령을 전달하지 않는 가짜 CLI 표식을 쓰며 이 하네스는 재실행하지 않는다.
- 메인 보고 `msg_cdc2b3fc39a8`에 결함·원문·미실행·상태 표기·로컬 fixture 실행을 전달했다. 새 Astra 수정의 쓰기는 기존 제품/등록/좁은 문서·CI 및 `E/guard-fix-01`뿐이며 시험14개와 과거 원시는 고정한다. O1~O7의 범위 밖 명령·shim/셸 환경 한계는 문서에 드러내고 새 지원 보장으로 넓히지 않는다. 새 Opus가 수정 결과와 실제 진입을 독립 판정한다.

### PR2 첫 수정 종료와 강 재검증 인계

- 수정 Task `task_3fe8f2e6ca86` / Dispatch `ctx_b21594bb9e8a`, 신규 gpt-6-astra xhigh/화면·rollout model 일치·backend unknown. 계약 `E/guard-fix-01-contract-v1.md` SHA256 `a465fd620a19afc0abfc3db71496000b8e5d220e99988c923c665a1994b69555`, 입력61/부모3/누락0이었다. R-5 draft24126=계약19338+4788·동일 identity 확인 후 bare Enter1회, 실제 제출 전문 대조 exit0과 Working을 확인했다. 최초 start_unknown receipt는 보존했다.
- writes-end `msg_d43fc2f3523e`·worker_done `msg_5e4fdd29a0fd` succeeded를 대조했다. 보고 `E/guard-fix-01/report.md` SHA256 `d780e365df0167c954f7a5c9b7601b3e3be7cf896bbbde8eaa77a10fd07559d5` 전체·context·실제 diff를 읽었다. 제품3/문서2 +302/-88, 기존14시험·등록/CI 무변경이다. worker-show completed/succeeded→release external retained→동일 idle pane close ptyKilled=true로 종료했고 재사용하지 않는다.
- 자체 회귀 before 전체564/512/52·leaf472/436/36에서 after-04 전체564/564·leaf472/472, Claude293/293으로 보고했다. 추가 표본은25중1실패→29중5실패→29/29→31중2실패→31/31→32중1실패→32/32다. #1 위치 합류·#3 호스트 약어의 자체 보완 원시를 보존했고 초기25개 harness는 사후 재구성/hash 대조라고 밝혔으며 당시 파일로 소급하지 않았다. 리드 저장 `E/settle-guard-fix-01.mjs`는 실행입력24/수정5/보호56의 불일치0·허용 밖 Git경로0을 확인했다. 자체검사와 해시 정산은 독립 PASS가 아니다.
- 같은 산출물 수정3회 초과를 `msg_32ccdd1dae62`로 메인에 알렸다. 메인 `msg_59c9ef934fa2`는 현재 범위 계속을 판단하고 #3 두 호스트 실측과 다시 어긋나 같은 지점을 수정해야 하면 먼저 멈추고 원인(파서 설계/표본 누락)을 보고하도록 했다. 보충 `E/guard-fix-01-design-supplement-01.md`·전달 `msg_c9b994e9011e`를 작업자가 수신·대조했으며 마지막 보완 뒤 조건 미발생이라고 보고했다. 독립 확정 실패 각1회와 별도이며 재검증은 #1 그룹 파이프/OR/finally·#3 두 호스트 옵션 소비를 필수 확인한다.
- 생존 helper는 현재 Dispatch 신호10·최대390초·초과1구간·질문대기0, exit1이었다(`E/guard-fix-01-final-liveness.json`·`guard-fix-01-liveness-execution.json`). 최신100건에 현재 Dispatch 신호는 모두 있으나 과거 Run 전체 범위가 아니다. 5분 준수로 보고하지 않는다. 제품 판정/확정 실패와 별도 절차 이탈이다.
- 신규 Opus의 verifies는 이번 수정 Task `task_3fe8f2e6ca86`다. 이전 #1~#9와 새 귀속을 연결하고 전체 PR2 보존·내용·사람 가독성을 실사한다. 실제 Codex 진입은 변경 명령을 전달하지 않는 가짜 CLI 계획을 새로 만든 뒤 리드 기동/R-6 메인 신뢰 처리와 조율한다. 이전 live 계획의 실제 로컬 bare push는 재사용하지 않는다. 실제 trust·같은 경로 새 세션·exec/code-mode/headless와 Linux CI는 계속 미실행이다.

### PR2 첫 수정 재검증 종료

- 신규 Opus Task `task_95d61c26df09` / Dispatch `ctx_d78fde8cee77`의 writes-end `msg_760a1cd9f850`·worker_done `msg_18288b5312f0` failed를 대조했다. 판정 `E/guard-recheck-01/verdict.md` SHA256 `9547898b46df731872231dc804d25d8d207514d1474ce28280b1c033fac69f98`와 context 전체·신규 시험4파일을 읽었다. verifies는 수정 Task `task_3fe8f2e6ca86`이며 **NOT PASS**다.
- #1~#9의 원래/추가 표본 수정은 확인됐다. 새 차단은 #10 백그라운드 cd 위치, #11 실패 조건 본문 위치, #12 exit로 끝나는 실패 경계 오탐, #13 Windows PowerShell 위치 인자/옵션, #14 Command·cmd 꼬리 결합, #15 pwsh CommandWithArgs, #16 iex 약어·콜론·리터럴 파이프, #17 catch 타입 목록, #18 PowerShell 백틱 인용 오탐이다. #12 전부와 #13 네 표본·#15 두 표본은 첫 수정이 만든 회귀다. 새 번호별 확정은 각1회이며 옛 #1~#9 각1회·개발 중 자체 보완과 합치지 않는다.
- 저장 원시 `guard-recheck-01/raw/recheck-01/summary.json`은 전체718/통과669/실패49, leaf603/통과564/실패39다. 기존14시험472/472, 신규4시험131중39실패이며 모두 제품 결함으로 귀속했고 미분류0이다. Claude293/293이다. 제품 수정은 없고 실제 Codex 진입/신뢰·새 세션·code-mode·headless와 Linux CI는 미실행·보류다. stdin 시험 통과로 대신하지 않는다.
- 구현 수치/hash·사후 재구성 주장은 독립 재집계와 일치한다. 다만 지원 동작 주장은 설치 호스트 실측과 다르고 정상 exit 관용구 회귀가 있다. 호스트별 CLI 의미와 위치/종료 경로를 충분히 나누지 않은 파서 설계가 잠정 원인이다. 메인 `msg_59c9ef934fa2`의 조건에 따라 중간 `msg_0373f32408b6`로 즉시 알렸고 후속 제품 수정은 판단 전 시작하지 않는다. N1 일반 순차 cd 성공 가정·N4 catch 예외 경로, #16 파이프 범위와 N2/N3 후속 후보 해석도 최종 판정에 남아 있다.
- `E/settle-guard-recheck-01.mjs`는 실행입력28·고정입력83·신규시험4·판정/메모 hash를 대조해 불일치0·허용 밖 Git 경로0을 확인했다. 이 정산 뒤에만 Art 결정/goal 기록을 썼다. worker-show failed/settled→release external retained→동일 idle pane close ptyKilled=true, reclaimable0이며 재사용하지 않는다. 제품 PASS와 다른 소유·입력 정산이다.
- 생존 helper 원시는 최신100건에 현재 Dispatch 전체를 포함하며 신호16·최대306초·초과305/306초 두 구간·질문0, exit1이다(`E/guard-recheck-01-final-liveness.json`). 검증자가 공개한 두 번의 5분 초과와 일치한다. 최초 메모 전 heartbeat 시도2개는 hook에 막혀 전송되지 않았고 우회하지 않았다는 판정 기록을 유지한다. 절차 이탈을 제품 확정 실패에 더하지 않는다.

## 다음 계획 후보

- Codex 출력/쓰기 경계 기술화: BACKLOG `codex-session-write-guard`. 이번 가드 PR은 PR 병합/원격 main 갱신만 다룬다.
- 리드 Astra 안착 뒤 검증자 Opus 복귀: BACKLOG `reviewer-opus-after-astra-settles`. 현재 시범을 변경하지 않고 안착 근거 뒤 사용자 판단을 받는 조건부 후속이다.
- 기존 BACKLOG `wake-token-filter`의 Jev/필터 구현, BACKLOG `heartbeat-interval-review`, BACKLOG `mailbox-wait-overlap-guard`, BACKLOG `lead-mailbox-receive-delay`, BACKLOG `lead-launch-command-source` 도구는 대기다. 이번 문서 정합·평가 안건 반영을 해당 도구의 구현 완료로 보고하지 않는다.
