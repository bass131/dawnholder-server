# 검증 경량화 정본과 공용 실행 잠금

## 재개 지점

**다음 첫 일: PR2의 고정 실행 계약으로 신규 Opus가 선행 실패 시험을 작성한다.** PR243은 사용자 승인 뒤 메인이 병합했다. PR2는 최신 main의 별도 브랜치에서 시작하며 선행 시험 → Astra 구현 → 신규 Opus 강 독립 검증 순서로 진행한다.

작업 공간은 `rules-active`, 현재 도구 PR 브랜치는 `feat/shared-execution-lock-20261015`다. 문서 PR 브랜치는 `docs/verification-lightening-20261015`였다. 최신 main에서 시작한 기준·현재 Run·모델 관측·receipt는 근거 폴더 E의 `lead-context.md`와 해당 진입 원시에 둔다. E는 `.backups/verification/2026-10-15-verification-lightening-execution-lock/`다.

## 진척 단계

- [x] 이전 변경 보존
- [x] 범위 초안 작성
- [x] 사용자 범위 승인
- [x] 경량화 정본 반영
- [x] PR243 병합
- [x] 잠금 설계 확정
- [>] 선행 실패 시험
- [ ] 잠금 도구 구현
- [ ] 독립 강 검증
- [ ] 도구 병합
- [ ] 종료 점검과 기록

PR 생성 뒤 병합 단계 이름에 실제 PR 번호를 넣는다.

## 범위

### 만들 것

1. **실행 단계 일괄 승인 정본:** 새 계약 발행 때 빌드·시험·실제 진입의 목록·순서·입력과 자동 관문을 리드가 한 번 승인한다. 이미 발행한 계약은 남은 단계의 버전 고정 보충으로 묶을 수 있다. 관문 실패·계획 밖 실행은 공식 질문으로 올리고, 승인된 다음 단계마다 같은 허가를 다시 받지 않는다. 입력 hash·잔여 프로세스·Unity 수·직전 물리/commit 여유 등 관련 관문과 실행 근거는 보존한다.
2. **heartbeat와 goal 기록 경량화 정본:** 첫 신호는 기동 직후 보낸다. 300초 초과는 구간·이유 한 줄로 기록하고, 기존 정산 helper 집계와 메인 R-2 표본 대조를 유지한다. 다른 의심 근거가 있을 때만 해당 구간의 대화·도구 호출 원시를 추가 감사한다. goal에는 결정·단계 결과·결함과 근거 경로를 두며, 수치·handle·PID·hash 원시는 근거 폴더에 둔다. 재개 블록은 다음 첫 일을 먼저 보이고 기록 커밋은 단계가 끝날 때 묶는다. 기존 역사 기록은 고치지 않는다.
3. **공용 실행 잠금 helper:** 여러 worktree가 같은 기계의 공용 상태를 사용하는 명시적 실행 진입과 상태 조회를 제공한다. Unity는 한 번에 하나이며 대기 요청은 Content 우선, WSL 대기 요청은 Core 우선이다. 실행 중인 남의 프로세스를 선점·종료하지 않는다. Core WSL과 Unity 병행은 직전 물리 여유 6GiB와 commit 여유 4GiB를 모두 충족해야 한다. 메모리 관측 불가·잠금 충돌·소유 불명확은 실행 승인으로 바꾸지 않는다.
4. **수명과 검증:** 잠금 소유를 실제 실행 프로세스와 연결하고 정상 종료·실패·취소·소유자 소실을 구분한다. Unity import 자식은 `parentPid`로 같은 실행 묶음에 포함하고, 다른 소유자의 Unity를 같은 묶음으로 오인하지 않는다. 살아 있는 자식이 남았는데 부모의 종료만 보고 잠금을 풀지 않는다. PID 재사용·잔여 상태·동시 획득·대기/취소·잘못된 입력을 시험하고 독립 강 검증과 CI를 연결한다.

### 건드릴 곳

| PR | 허용 대상과 책임 |
|---|---|
| 1 - 검증 경량화 정본 | `.agents/skills/dawnholder-goal-loop/SKILL.md`의 계약·기록·정산, 관련 `references/orca-work.md`, `.agents/skills/dawnholder-task-context/SKILL.md`와 `references/templates.md`의 실행 승인·원시 연결, `00_Document/operations/ORCA.md`의 우편함/정산·R-2 관련 절. 중복된 상세 규칙은 정본을 참조한다. |
| 2 - 공용 실행 잠금 도구 | `99_Tools/ExecutionLock/`와 `99_Tools/ExecutionLock.Tests/`를 책임별 배치 후보로 삼는다. `99_Tools/README.md`, `00_Document/operations/DEVELOPMENT.md`, `00_Document/domains/tooling.md`, ORCA의 해당 실행 조율 절, `.github/workflows/code-rules.yml`의 도구 시험·근거 보존 단계만 연결한다. 실제 파일명/API/공용 상태 경로는 승인 후 설계 계약에서 고정한다. |
| 목표 관리 | 이 `goal.md`, `00_Document/operations/CURRENT.md`의 Rules 한 줄, 승인된 후보의 `BACKLOG.md` 상태/goal 링크, 할당된 E 근거. |

도구는 저장소 공통 식별자와 명시적 프로젝트 범위 설정으로 공용 상태를 찾도록 설계한다. worktree별 잠금을 공용 잠금이라고 보고하거나 머신 절대 경로를 제품 코드에 고정하지 않는다. 원시·잠금 상태는 Git 제외 실행 자료이며, 전역 설정을 바꾸지 않는다.

### 하지 않을 것

- 기존 PR2 병합 가드 재개·검증, 기존 병합 관문 hook/settings 변경, 다른 파트 제품 코드·Unity 자산 수정.
- 모든 셸·IDE·외부 실행을 자동 차단하는 시스템이나 기존 실행 wrapper의 일괄 개조. 도구의 명시적 진입과 관측 범위를 문서에 밝힌다.
- 게임 상태·DB·프로토콜·공유 DLL 변경, 실제 게임/DB 검증을 이 helper의 시험 성과로 보고하기.
- heartbeat 간격 변경·정산 helper/R-2 생략, 독립 세션·역할 분리·강약 등급·병합 승인 완화.
- 남의 실행·포트·컨테이너·보존 자원 종료/삭제, 전역 설정·설치·페이지 파일 변경.
- 미채택 Gardener 제안인 첫 맥락 메모 전 초기 원시 보존 위치 정비, 별도 후보 helper의 추가 구현.

### 관찰 가능한 완료조건

1. 문서 실사자가 실제 계약 작성·자동 관문 실패·heartbeat 초과 정산·재개 시나리오에서 새 규칙을 찾고, 기존 필수 대조·승인·역할 경계와 모순이 없음을 판정한다. 이 goal에서도 결정 1~3을 실제로 적용한다.
2. 서로 다른 worktree 경로에서 시작한 두 도구 진입이 하나의 공용 상태를 사용한다. Unity의 중복 admission을 막고 대기 중 Content Unity/Core WSL 우선순위를 확인한다. 실행 중 작업을 강제로 중단하지 않는다.
3. 메모리 관문 직전의 실제 관측·단위·시각·판정 근거를 저장한다. 6GiB/4GiB 병행 경계의 바로 아래·동일·바로 위와 관측 실패를 시험한다. 별도 단독 실행 임계값을 근거 없이 만들지 않는다.
4. 도구의 실제 진입점으로 무해한 자식 프로세스를 실행해 입력→잠금/관문→자식→exit/정리를 관측한다. 정상·nonzero·취소·부모/자식 수명·경합·잔여 잠금·소유 불명확을 검증하고, 다른 소유자의 자원을 건드리지 않는다. 실제 Unity import/WSL 제품 실행을 하지 않은 부분은 명시한다.
5. 신규 선행 시험의 구현 전 실패와 구현 후 같은 명령의 통과, 신규 독립 검증자의 자체 시험·실제 진입 실행·판정 원문, CI 결과가 연결된다. Linux CI의 정책/fixture 결과를 Windows 실제 자원 관측 성공으로 확대하지 않는다. 실제 환경/권한 때문에 필수 실행을 못 하면 미실행·판정 보류다.
6. 각 PR의 독립 판정과 정확 head·CI·남은 위험을 메인에 보고하고, 각 병합은 별도 사용자 명시 승인 뒤 메인이 실행한다. 도구가 main에 들어가기 전 기존 수동 조율 기준을 유지한다.

## PR 경계와 검증

- **1 - 검증 경량화 정본:** 문서 전용 PR이다. Sol xhigh 작성 → 신규 Astra xhigh 문서 실사로 내용·참조·권한 및 현실적 계약/정산 시나리오를 확인한다. 기존 helper나 hook의 코드 변경은 넣지 않는다. 파일/줄 수는 실제 diff가 생긴 뒤 원시로 저장한다.
- **2 - 공용 실행 잠금 도구:** PR1 병합 후 당시 최신 main에서 별도 브랜치를 만든다. 실행/I/O·동시성·수명·비용 관문이므로 **강**이다. 신규 Opus 선행 시험 → 신규 구현자 → 신규 Opus 강 독립 검증이며, 제품/시험 파일 소유를 분리한다. 구현 전 실패·실제 진입·CI를 생략하지 않는다.
- PR2 구현자는 **신규 Astra xhigh**다. 현행 `implementer-routing.md`의 신호2(동시성·프로세스 수명), 신호3(Windows/WSL 경계), 신호4(새 다파일 도구)에 해당한다. 메인 `msg_7fcc3dcf1084`이 첫 Sol 제안을 철회하고 이 배정을 확정했다(메인 판단).
- **R-7 비해당: 메인 판단, 이유는 시연 전 속도 우선과 강 등급 독립 검증 유지다**(`msg_7fcc3dcf1084`). 네 범주에 닿을 수 있으나 이번 goal은 한정 Fable 설계 검토 시범에 포함하지 않는다.
- 단계별 실행 목록·순서·입력·자동 관문·실행 자원·근거 경로를 계약 때 일괄 고정한다. 공용 상태 위치, 우선순위의 대기 처리, 관문 관측/실패/취소·자식 수명은 설계에서 확정한다. 다른 파트의 실제 Unity/WSL 중량 실행 권한은 별도 자원 소유 경계를 따른다.
- 도구 PR 병합·결과 기록 뒤 신규 Gardener → 독립 확인된 종료 기록 PR 하나 → 사용자/메인 종료 점검 → R-8 순서다. 첫 PR마다 Gardener를 추가하지 않는다.

## 결정과 단계 결과

- **기존 사용자 결정:** 메인 전달 `msg_b49fffe1ccaa`의 1~4 모두 A다. 계약 때 실행 단계 일괄 승인, heartbeat 초과 한 줄+기존 집계/R-2, goal 핵심 기록, 공용 잠금 도구다. 전체 원문과 상세 경계는 E/`verification-operating-simplification-original.json`에 보존했다. 메인 전달을 직접 사용자 입력으로 격상하지 않는다.
- **진입 지시:** 메인 `msg_27c6fd9cb341`이 rules-active 재사용·PR2 기계 보존·최신 main 새 브랜치·범위 초안을 지시했다. 별도 worktree 대신 재사용한 것은 두 PR·TDD·독립 강 검증의 작업량을 고려한 메인 판단이다. 기존 BACKLOG 후보는 `verification-operating-simplification-canon`이다.
- **이전 변경 보존 완료:** 기존 동결 16경로의 파일 목록·SHA256·원본 사본을 남겨 그대로 한 커밋/push에 보존하고 기존 goal에 중단 기록 한 줄을 추가했다. 원격 head와 원본 보존 대조는 E/`freeze-result.json`, 커밋/push 원시는 E/`freeze-commit.txt`·`freeze-push.txt`다. 제품 시험·독립 판정은 수행하지 않았고 PR2를 재개하지 않았다.
- **범위 승인:** 메인 `msg_7fcc3dcf1084`의 사용자 원문은 「대시보드 결정 응답: 1) 계획 검토 - Rules Rules 새 goal 범위 - 검증 경량화 규칙 문서 PR과 공용 실행 잠금 도구 PR → A 승인 (초안 msg_ddb68c0b4007)」이다. 메인이 전문·초안 hash를 대조하고 전달했다. 승인본은 E/`scope-approved-original.md`, 수신 원문은 E/`check-next-instruction.json`이다. PR2 Astra 배정과 R-7 비포함은 사용자가 함께 본 정리본에 적힌 메인 판단이며 사용자 결정으로 격상하지 않는다. PR1 문서 작업·독립 실사를 이어가고, 실사 중 PR2 선행 시험 계약의 정적 준비만 허용한다.
- **PR1 작성 단계:** 신규 Sol이 허용 5문서에 실행 단계 일괄 승인·heartbeat 정산·goal 핵심 기록과 정본 연결을 반영했다. 작성 보고·사전 메모·자체 정적 harness 원시는 E/`pr1-author/`이며 자체점검과 독립 판정을 구분한다. 쓰기 종료·정산/종료는 E/`check-author-update-02.json`·`pr1-author-release.json`·`pr1-author-close.json`, 생존 신호 정산은 E/`pr1-author-liveness-complete.json`으로 초과 구간이 없다. 신규 독립 문서 실사와 메인 R-2·별도 병합 승인은 남았다.
- **PR243 - 검증 실행 승인·정산·목표 기록 경량화:** [draft PR](https://github.com/bass131/dawnholder-server/pull/243)을 만들었다. 독립 문서 실사 전 상태이며 최종 판정·정확 head·CI는 E의 검증/승인 묶음으로 전달한다. 실사 고정 뒤 이 goal을 판정 결과 기록으로 바꾸지 않는다.
- **PR243 병합:** 신규 Astra 문서 실사 PASS·메인 R-2·정확 head CI 성공 뒤 사용자가 별도 병합 승인했고 메인이 병합했다. 승인 제출·관문 실행은 메인 `msg_4ae0c4249ce8` 전달이며, PR의 MERGED·병합 commit·시각은 E/`pr1-merge-observation.json`으로 읽기 확인했다. 판정/정산/최종 승인 묶음은 E/`pr1-review/`·`pr1-final-handoff.json`이다.
- **PR2 실행 계약:** 같은 저장소 공통 상태를 쓰는 JSON runner, Unity/WSL 대기 우선순위와 비선점, 직전 관측·병행 관문, PID/시작 identity·자식 수명과 실패/취소를 E/`pr2-execution-contract-v1.md`에 고정했다. 기본 상태는 Git common directory의 전용 Git 제외 경로이며 실제 관측과 시험용 주입을 구분한다. 대역의 무해한 Node 자식과 Windows 실제 진입의 확인 범위를 나누고, 새로운 단독 메모리 수치·전역 설정·직접 실행 전체 차단은 추가하지 않는다. 신규 Opus의 선행 RED 원시 뒤 Astra 구현을 발행한다.

## 결함과 다음 후보

현재 산출물의 독립 판정·결함 번호는 없다. 현재 범위 밖 후보는 기존 BACKLOG 정본에 두며, 이 goal에 상세 후보 장부를 복제하지 않는다.
