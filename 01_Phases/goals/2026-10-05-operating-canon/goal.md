# 운영 정본 반영

## 재개 지점

2026-10-05 KST, Rules 운영 정본 반영 목표다. **PR1 전체가 신규 Opus의 독립 문서 실사를 통과했다**(차단0·필수 미검토0, ORCA 실측249줄). 작성자와 검증자는 쓰기 종료·정산·pane 종료했다. 비차단 #8의 회차 표지는 Astra가 결과 기록과 함께 보완했고, 검증 후 변경분을 메인 R-2 대조에 전달한다. 이전 BLOCKED와 자체검사 실패 원시는 보존한다. 다음은 PR 생성·CI와 정확 head의 사용자 병합 승인이다. 아직 PR·CI·병합은 미실행이다. 이 파일이 기준·상태·결과의 정본이다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/rules-active`.
- branch: `docs/operating-canon-20261005`. 기준: fetch한 `origin/main`의 `11aa4b83131bc6349f186a141cfea9c58d2230e3`.
- 기존 `docs/teammate-onboarding`의 `a550eda`·`f35aee2`·`e0d4fe7`·`21ae047`은 보존한다. PR177 결과 기록만 PR1에 별도 커밋으로 이관한다. 이전 Run·Task·Dispatch는 실행 권한으로 재사용하지 않는다.
- 새 Run `run_2e3ef4cc0150`, 회신 주소 `run:run_2e3ef4cc0150`. 식별자는 이번 진입의 관측이며 다음 세션의 실행 권한이 아니다.
- 로컬 근거: `.backups/verification/2026-10-05-operating-canon/`. 사전 메모 `astra-context.md`, 최초 메인 원문 `main-request.txt`.

## 범위

### 만들 것

2026-10-04~05에 승인된 운영 결정을 현재 정본에 옮기고, 적용 시점·역할·권한과 후속 계획의 경계를 맞춘다. 계약과 판정 양식은 PR2에서 따로 정리한다. 메인 자료의 조사·평가 결과를 재검증 없이 이번 실적으로 주장하지 않는다.

- PR1: HANDOFF 결정 1~16의 정본 위치 또는 비반영 이유 대조표, 범위 4항목·PR 경계와 수정 3회 초과 체크포인트, 교정 층 선택·두 번째 발생부터 규칙화, 팀원 역할·외부 세션 적용 경계, CodeMap 이름·책임과 Content 배치·태그, Unity 숨김 이유, SAC Off 관측, 결정 대시보드 수명, 추가 입력 목록 처리.
- PR1: 보관 브랜치의 현재 안내 3곳(AGENTS·README·.gitignore) 정리. 과거 goal 언급은 유지하며 원격 `archive/claude-setup-2026-09-29` 삭제는 이 PR 병합 뒤 메인이 수행한다.
- PR1: PR177의 로컬 결과 기록 4커밋을 별도 기록 커밋으로 옮겨 원격 재개 상태 드리프트를 해소한다. 원시와 숫자·명령 label의 연결을 대조한다.
- PR2: 계약 작성 권고 6개, I/O·설치·실행 도구의 진입 경로 실행 기준, 작업 맥락 스킬의 관련 없는 절 제외와 누락 차단의 관계, 판정의 기계 판독 표·설계 관찰 절, Fable 설계 검토 한정 시범, 검증 강도 2등급 시범을 관련 정본·양식에 반영한다.

### 건드릴 곳

- `AGENTS.md`(사용자 요청으로 변경), `00_Document/operations/ORCA.md`, `.agents/skills/`의 관련 프로젝트 스킬·양식.
- `00_Document/conventions/CODE_CONVENTION.md`와 `REPORTING.md`의 해당 절, `00_Document/operations/BACKLOG.md`·`CURRENT.md`.
- `00_Document/operations/RESUME.md`는 「세션 진입 배치」의 세 Astra 나열을 ORCA R-1 링크로 좁힌다. 처음 메인 범위 판정 `msg_1eb78ae1b0cb`(2026-10-04T17:26:43Z)는 2·4항만 허용했고, 독립 #1 뒤 `msg_c9d3dd5e196d`(18:42:18Z)가 같은 완료조건 2의 근거로 1항 두 줄도 포함했다. 그 밖의 문장·구조 정비는 제외한다. 원계약v1·보충v1a·첫 판정은 보존하고 새 수정 계약에서 적용한다.
- `README.md`·`.gitignore`는 보관 브랜치 언급만. 기존 `01_Phases/goals/2026-10-04-teammate-onboarding/goal.md`는 승인된 결과 기록 이관만.
- 이 goal과 로컬 계약·근거 기록. 상태·결정·결과는 이 goal, CURRENT는 링크, BACKLOG는 goal 전 후보로 유지한다.
- `CLAUDE.md`는 **메인 소유**다. Rules가 변경 위치·문장 초안을 보내고 메인이 이 branch에 쓴 뒤 쓰기 종료를 알린다. Sol·검증자는 이 파일을 쓰지 않는다.

### 하지 않을 것

- 규칙 문서 가지치기(2026-10-31 Gardener 평가 때), 하네스 마무리·새 helper·경로 검사 구현·토큰 기록·TDD 규칙 문구(후속 goal).
- 제품 코드·테스트 기능·새 검사·workflow 구현, Unity 설정·skip-worktree 상태·SAC·방화벽·전역 설정 변경, 원격 보관 브랜치 삭제.
- 다른 파트의 제품 구현·현재 goal 재계획·후속 goal 착수. 조사 보고·초안의 미채택 추천을 새 정책으로 채택하지 않는다.
- Gardener2는 메인이 전달한 사용자 결정 `msg_7bfc0ca278f5`에 따라 기존 두 BACKLOG 행의 근거만 연결한다. 새 후보·검사 구현·기존 촉발 조건 변경·goal-state-drift 행 갱신은 하지 않는다.

### 관찰 가능한 완료조건

1. HANDOFF 결정 1~16과 아래 추가 입력 각각에 현재 정본 위치 또는 비반영 이유·출처가 있다. PR2 소관 항목은 PR1에서 완료로 표시하지 않는다.
2. 변경 문서의 링크·경로와 권한·적용 시점의 일관성을 검사하고, 숫자와 주장의 원시 위치를 대조한다. 실행불가·대상 없음·미실행은 통과로 합치지 않는다.
3. PR마다 작성자의 쓰기 종료 뒤 **신규 `claude-opus-5-5`**가 독립 문서 실사하고 판정 원문·수행 근거를 남긴다. 관련 필수 미검토·차단 결함이 없어야 한다.
4. PR마다 정확 head·CI·미실행을 메인에 보고하고 사용자 명시 병합 승인을 받은 뒤 통합한다. 메인의 R-2 원천 대조와 병합 권한은 기존 지침을 유지한다.
5. 두 PR의 병합·결과 기록 뒤 신규 Gardener와 종료 점검을 수행한다. R-8 교체는 메인이 담당하고 다음 goal은 자동 착수하지 않는다.

## PR 경계와 검증

| 순서 | 산출물 | 점검 지점 |
|---|---|---|
| PR1 — 10-04~05 운영 결정 정본 반영 | 현재 운영 결정·추가 입력·숫자/원천 대조·PR177 결과 이관 | Sol 쓰기 종료 → 신규 Opus 문서 실사 → PR/CI → 메인 R-2 → 해당 PR 사용자 승인 |
| PR2 — 계약·판정 양식 정비 | 권고 6개·진입 경로 실행·관련 절 선정·기계 판독·설계 관찰·Fable·검증 2등급 | PR1 통합 뒤 최신 main에서 후속 branch → 신규 Sol → 신규 Opus → PR/CI → 사용자 승인 |
| 전체 종료 | 두 PR 결과·남은 위험·BACKLOG 연결 | 신규 Gardener → 메인/사용자 종료 점검 → R-8 |

검증 등급은 **문서 실사**다. 제품 코드가 없으므로 이 goal의 TDD는 해당 없음이며, 제품 빌드·플레이·DB 실행을 문서 실사로 대체하거나 수행했다고 보고하지 않는다. 검증 세션을 생략하는 예외는 없다. 스킬·양식 변경은 실제 문서 작업 시나리오에 적용해 결과를 살피며 자동 선택·행동 강제를 보장하지 않는다.

파일 쓰기 소유는 goal·계약·통합이 Rules Astra, 정본 문서 작성이 외부 Rules Sol, 독립 판정이 신규 Rules 검증자, CLAUDE가 메인이다. 각 작업자는 한 작업 뒤 정산·종료하고 재사용하지 않는다. 같은 파일 동시 쓰기는 금지한다.

## 요구사항 원천과 적용 결정

직접 사용자 입력과 메인이 전달한 사용자 결정은 구분한다. 이번 착수 권한은 메인 `msg_05ba75ccd7f9`가 전달한 대시보드 응답 **「3) Rules 다음 goal: 운영 정본 반영 → A 승인」**과 같은 메시지의 구체 범위다. 아래 파일은 저장소 밖 메인의 읽기 전용 자료이며, 계약에는 발행 시점 사본·hash를 고정한다.

- `C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-04/HANDOFF.md`의 「오늘 확정한 결정」 1~16.
- 같은 폴더 `routing-draft.md`의 사용자 7건 승인, `plan-scopes-draft.md`의 Rules 범위, `deadline-roadmap-draft.md`의 마감 1A·2A·3A·4B. 초안의 미채택 추천과 역사 상태는 현재 결정과 구분한다.
- 범위 원문 **「OK 그렇게 가자」**: 만들 것·건드릴 곳·하지 않을 것·관찰 가능한 완료조건 및 PR 경계를 착수 전에 확인한다. 범위 안 완료조건을 막는 결함만 수정하며 같은 산출물 수정 3회 초과는 메인 체크포인트다.
- 라우팅 응답 **「7건 모두 A」**: 모델 배정은 유지하고 권고 6개를 바로 적용한다. 검증 등급은 강/약 4주 시범이며 애매하면 강, 문서는 실사다. 판정 기계 표는 PR2, 토큰 기록은 helper 정본화 이후로 분리한다. 규칙 가지치기는 10-31 평가 때다.
- 이 goal 진행 중에도 계약 경로 실재 확인, 요구 항목·판정 기준 중심 계약, 해당 없는 규칙 절과 이유, 검증 harness 파일화·Assert 판단 변수 분리, 설계 결정에 한 줄 대안, 원시 파일의 수치 읽기를 적용한다.
- 새 goal부터 TDD라는 결정은 인정하되 문구 정본화는 후속 goal이다. 이번 문서 작업에는 제품 코드가 없어 해당 없음이다.

## 결정 대조표 — 승인된 계획

착수 때 고정한 계획을 보존한다. 작성 뒤 실제 반영 위치·비반영 이유는 아래 「PR1 작성 결과 대조」에 따로 기록하며, 독립 판정 전에는 작성자 보고다.

| HANDOFF 번호 | 결정 | 처리 계획·경계 |
|---|---|---|
| 1 | 범위 4항목·루프·수정 체크포인트 | PR1 goal-loop/운영 정본 |
| 2 | Lauren Tan 원문 조사 | 조사 자체는 메인 완료 자료. 채택 결정 11의 적용만 반영 |
| 3 | 모델 평가·약점 지도 분리와 라우팅 재구성 | 조사·보고 재작성은 제외, 결정 12·13과 연결 |
| 4 | Public 방화벽 비활성·Relay 관측 | PR1 환경 사실로 한정, 설정 변경·접속 재검증 없음 |
| 5 | Unity 3파일 숨김 유지·이유·게임 설정 부분 커밋 | PR1 이유·향후 담당 범위만 기록, 실제 에셋/인덱스 변경 없음 |
| 6 | 영속화 기존 순서와 체크포인트 | 후속 결정 15의 첫 PR 종료로 갱신된 경계 명시, 제품 goal 재계획 제외 |
| 7 | CodeMap 이름·분석/검사 책임 | PR1 운영 정본. 기존 경로·Architecture 태그와 표시 이름 구분 |
| 8 | 팀원 PR 코멘트 비동기 | PR1 기존 팀원 정본 유지·외부 적용 경계 보완 |
| 9 | 게임 정책 열린 질문·명세는 입력 | PR1 기존 안내 근거 확인, 새 게임 정책 결정 없음 |
| 10 | SAC Off·배포 정책은 배포 goal | PR1 이 PC 관측과 미실행 범위, 전역 설정/배포 실행 없음 |
| 11 | 격차표 채택 8·기존 6·차이 3·보류 5 | PR1 원시 표 숫자 대조·교정 층/두 번째 발생 원칙, 후속 구현 제외 |
| 12 | 약점 지도·기능 CI·실패 12건 분류 | PR1 소유·시점 연결, CI 구현·운영툴 수리 제외 |
| 13 | 라우팅 7건 | PR1 결정 연결, PR2 권고 6개·Fable·검증 등급·판정 표. 토큰 기록 제외 |
| 14 | 마감 고리·Content·5리드·Docker 후속 | PR1 운영 배치/태그·범위, 게임 기능 구현 제외 |
| 15 | 파트별 플랜 5건 | PR1 담당 goal/후속 계획 연결, 다른 파트 goal 수정 제외 |
| 16 | Claude 셋업 보관 브랜치 폐기 | PR1 현재 언급 3곳 정리. 백업/역사 기록 보존, 원격 삭제는 메인 |

## 추가 입력 대조표 — 승인된 계획

| 입력 | 출처 | 처리 계획·경계 |
|---|---|---|
| persistence-recovery-post-deadline | GameDev msg_e1afafb84002 / 최초 메인 지시 | PR1 BACKLOG. 제한 복구·principal 실증·시험 행렬·crash 복구를 마감 뒤 후보로 |
| enemy-hit-dead-guard | GameDev msg_0a650b7e1c8b / 최초 메인 지시 | PR1 BACKLOG. ApplyImmediateEnemyHit IsDead 가드 후보, 구현 없음 |
| CodeMap workflow 소유 | msg_19e875227fe4 | 원문·현재 소유를 확인해 PR1 기록, workflow를 이 goal에서 쓰지 않음 |
| R-3 Orca 1.4.217 한정 적용 | msg_2efcbd0d05d1 / 최초 메인 지시 | PR1. 공식 ask/reply만 기존 조건 유지. 1.4.218 이상 또는 subject 지원 시 한정 적용 종료. helper 버전 위장 금지 |
| 크래시 확정 실패 제외·새 세션·checkpoint | msg_bcd8d484a61d 및 최초 메인 지시 | PR1 ORCA. 부분 결과 보존과 판정 근거를 분리, push/PR/병합 권한 유지 |
| 1.4.220/1.4.217 복구 관측 | 메인 최초 지시·HANDOFF | PR1 ORCA에 시점 있는 운영 관측. 자동 복구 보장·원인 확정 아님 |
| PR177 비차단 #2 label / #3 외부 경계 | msg_4eb82c2222fc / 기존 goal | #2 원시 경로를 대조하고 원래 보고 보존, #3 PR1 적용 경계 보완 |
| Gardener2 후보 2개 | msg_7bfc0ca278f5 / gardener2-report.md 정리 후보 절 | PR1 powershell-all-evidence·contract-context-check 기존 행에 관측 근거만 연결. 새 행·촉발 조건 변경·goal-state-drift 행 갱신 없음 |

Gardener2의 사용자 원문은 **「1) Gardener 후보 2개 - PR177 점검 근거를 기존 BACKLOG 후보에 연결 → A 두 근거를 기존 행에 연결」**(메인 전달, 2026-10-05 02시대 KST)이다. 원문 보고서 SHA256 `d566c5285cc1e74bd8b8e6aaf8ff4a9bf5f5270985a5d16b6d90c0b0e19f8c2d`, 전달은 로컬 `main-gardener-decision.json`에 보존했다. 후보1의 문서 인용 결함 2건과 사람 검토 검출, 후보2의 helper 중복 2회·CRLF 거짓 불일치·메모 선행 이탈 1건·birthtime 재설정 거짓 위반 관측만 해당 행에 덧붙인다. helper 구현은 후속 범위이며 기존 촉발 조건은 유지한다.

## PR1 작성 결과 대조

근거 기준 E는 `.backups/verification/2026-10-05-operating-canon/`다. 아래 위치는 원 작성 `E/sol-pr1-report.md` 및 수정 r1 `sol-pr1-repair-report.md`·r2 `sol-pr1-r2-report.md`·r3 `sol-pr1-r3-report.md`와 파일별 diff에 연결한 작성 결과다. 독립 판정은 아래 재실사 결과 절과 원문에 따로 기록한다. 첫 독립 차단 판정은 원 입력과 함께 보존한다. PR2의 정책 문구와 다른 파트 실행을 PR1 완료로 세지 않는다.

| HANDOFF 번호 | 실제 반영 위치 또는 비반영 이유 |
|---|---|
| 1 | AGENTS 역할과 범위·goal-loop 기준과 상태: 4항목·PR 경계·승인 초안 대조·범위 안 결함·수정 3회 초과 체크포인트. 확정 실패 3회와 구분 |
| 2·3 | 완료 조사·모델 보고 재작성과 전체 수치 재분석 제외. 채택된 교정/소유·시점만 결정 11~13에 연결 |
| 4·10 | 이 goal의 「2026-10-04~05 환경·복구 기록」에 메인 당시 Public 방화벽/Relay·이 PC SAC Off 관측을 보존하고 ORCA 「관찰 기록: 2026-10-04~05」에는 링크만 둠. 새 접속·배포 검증과 설정 변경 없음 |
| 5 | ORCA 공유 자원: Unity 3파일 숨김 이유와 게임 설정 부분 커밋의 후속 담당. Rules checkout의 실제 표시는 H→H이며 사용자 유지 결정과 구분 |
| 6 | 후속 결정15에서 GameDev 첫 PR 종료로 순서가 갱신됨을 이 대조표에 기록. 저장 고리/맵 수명은 다른 파트, 제한 복구는 BACKLOG 후보. 다른 goal 재계획 제외 |
| 7 | AGENTS 역할·태그, ORCA R-1, CURRENT: CodeMap 표시 이름·분석/검사 책임과 코드 주인의 리팩토링 책임, 기존 Architecture 경로·태그 유지 |
| 8·9 | AGENTS 외부 세션 및 ORCA/스킬 진입: PR 코멘트 비동기·로컬 기동 절차 제외. 기존 game-design README의 명세는 입력/열린 질문은 유지, 게임 정책 결정·플레이 확인 없음 |
| 11 | CODE_CONVENTION 「교정 층과 반복 규칙」, goal-loop, AGENTS 설계 우선순위. 채택8·기존6·차이3·보류5=22의 원문 분류를 자체점검에서 대조. 경로검사/helper/기록 구현은 후속 |
| 12 | 기능 CI는 CodeMap의 두 번째 일, 실패12건 분류는 Management 재개 범위라는 소유·시점 연결만. workflow/실패 수리와 높음4·실패12의 재분석 없음 |
| 13 | 7건 모두 A 원문과 PR 경계 연결. 권고6·진입 실행·기계 표/설계 관찰·Fable/강도 정본은 PR2, 토큰/helper/TDD 문구는 후속. 라우팅 결정6 「증가만 멈춤」의 누락은 독립 #2로 확인했다. 사용자 원문 「1) ORCA 규칙 문서 270줄 - 「증가만 멈춤」 결정과 어긋남, 어떻게 맞출지 → B 새로 넣은 시점 기록을 밖으로」(메인 전달 `msg_6c248ebe130e`)에 따라 새 관측표와 R-3 출처를 이 goal로 연결했다. 기존 문장·현재 규칙은 남겼고 이동 뒤 263줄/초과13줄과 애매한 문장 목록을 `msg_5891c5a06bdd`로 보고해 추가 판단을 요청했다. 후속 msg_bbd37070c7b7의 사용자 B에 따라 출처·시각 14건의 추가 이동 r2를 마쳤으나 256줄/초과6줄이다. 아래 결정 대조표에 원문·실제 결과를 연결했다. 후속 msg_cb365b462610의 승인된 역사8줄 이관을 r3에서 수행해 실제249줄을 확인했다. 신규 Opus 재실사에서 문서 실사 통과를 확인했다. 기존 가지치기는 10-31 평가 |
| 14 | ORCA R-1·AGENTS·session-handoff: 다섯 리드·Content 별도 탭/태그·GameDev horizontal/작업자 vertical. 원문1A·2A·3A·4B, 축소안 미채택. 게임/Docker 실행 없음 |
| 15 | 승인5파트 연결: GameDev 첫 PR 종료 뒤 맵 수명/저장 고리, Content 아이템/인벤토리/재화, Rules PR1→PR2, CodeMap 종료기록→경계warning→기능CI, Management 실패분류→125%→첫PR→종료. CURRENT는 실재 확인한 진입만 갱신 |
| 16 | AGENTS·README·.gitignore 현재 안내를 역사 고정 원문/색인으로 변경. 과거 goal·ignore 패턴 보존. 원격 보관 브랜치 삭제는 PR1 병합 뒤 메인 |

| 추가 입력 | 실제 처리·출처·한계 |
|---|---|
| 복구/dead guard 후보 | BACKLOG 마감 뒤 후보에 각1행. `msg_e1afafb84002.json`·`msg_0a650b7e1c8b.json`, 구현·도달성 재검증 없음 |
| CodeMap workflow 소유 | 최초 제안과 현행 회신 `architecture-workflow-confirmation.json`(`msg_914436f39e18`) 대조. 소유 충돌 없음과 기존 workflow/CodeRules/protection 보존 회신, 당시 구현 종료·독립 실사·PR/CI 미완료. 이 goal의 workflow diff 없음 |
| R-3 1.4.217 | ORCA R-3/수신 보조에 공식 ask/reply만 한정 확장·종료 조건·body/identity/receipt·helper 버전 위장 금지. 기존 E0 `main-r3-1217-decision.json` |
| crash·checkpoint·복구 관측 | ORCA 확정 실패/crash-recovery·orca-work에 현재 규칙을 두고, 시점별 관측은 이 goal로 연결. 크래시 제외·새 세션·부분 결과 통과 재사용 금지·쓰기 종료 뒤 로컬 checkpoint, 기존 Git 권한 유지. E0 복구 원문2건, 원인/일반 복구 보장 없음 |
| PR177 #2·#3 | #2 원 label `onboardingFinalLocators`는 실제 원시0건, `documentChecks`5개 중 `/documentChecks/4/result/exit_code=0` 연결. 원보고 보존. #3 외부 경계는 AGENTS/ORCA/스킬에 반영 |
| Gardener2 | BACKLOG 기존 `powershell-all-evidence`·`contract-context-check`에 승인된 관측 근거만 추가. 조건·goal-state-drift 행 불변. 별도 검사 구현 없음 |
| 메인 CLAUDE·대시보드 | 메인 `msg_a8de68ac47de` 쓰기 종료: 외부/로컬 경계·다섯 리드/CodeMap과 메시지 처리 뒤 `board.mjs stale` 확인. 사용자 원문 「LLM이 가끔 내용 업데이트를 누락하는 부분도 있어서, 그 부분도 보강해줘」는 메인 전달(`main-claude-write-end.json`). Rules의 대시보드 구현/실행 실적이 아님 |
| RESUME 보충v1a | 메인 `msg_1eb78ae1b0cb`에 따라 진입 2·4항만 R-1 링크로 교체. 원계약/manifest 보존·별도 보충 hash·baseline·수신 `msg_518c6c3ea4e1`. 다른 문장 정비 없음 |

## 현재 결과와 다음 작업

- 새 runtime·handle·incarnation·Orca 1.4.217 확인, 새 Run 생성과 READY 회신 완료(`msg_73dc772120f0`). 모델 화면 GPT-6-Astra xhigh, backend unknown.
- 기존 브랜치 clean과 PR177 기록 4커밋의 대상 파일을 확인하고 최신 main에서 새 branch를 생성했다.
- PR177 결과는 별도 커밋 `0cc8ff9`에 이관했다. 이전 `21ae047`과 이관 파일의 Git blob은 둘 다 `b8f57d9c510d58a0d70a2691e9e592585dc1355f`다(`astra-integration-source-check.json`). 재검증 실적을 새로 만든 것이 아니다.
- Sol Task `task_4c45e31ec39e`/Dispatch `ctx_39c42a70913d`, v1 계약과 RESUME 보충v1a 수행. Astra가 사용한 최초 명령은 `codex --model gpt-6.1-sol -c model_reasoning_effort=max`지만 split JSON 원시는 별도로 보존하지 않았다(`astra-execution-notes.md`, 명령은 Astra의 도구 실행 기록). 보존한 전후 화면 원시는 GPT-6.1-Sol max, backend unknown이다. 최초 attach는 `turn_start_unobserved`; 공식 draft 조건 확인 뒤 Enter 한 번으로 회복한 원시를 보존했다. 원래 receipt를 정상 기동으로 바꾸지 않는다. 독립 비차단 #7에 따라 이 증거 한계를 명시했다.
- Sol `msg_27721ccd9c5f`(2026-10-04T18:03:53Z)에서 succeeded·쓰기 종료 보고. 정산 release는 retained/external_terminal/processAction none, 동일 pane/incarnation 확인 뒤 close의 ptyKilled=true. `sol-pr1-completion.json`·`sol-pr1-release.json`·`sol-pr1-before-close.json`·`sol-pr1-close.json`. 재사용하지 않는다.
- 작성자 결과: 11문서 +111/-26, 자체점검 exit0·변경 줄 로컬 링크/anchor54회·스킬 frontmatter2개·보호파일/ignore 패턴/workflow diff/Unity flags 확인. 첫 파서·최종 근거 검사 실패와 exact-context 편집 실패도 원시에 보존했다. 독립 실사 또는 제품 검증으로 합산하지 않는다. 원문 `sol-pr1-report.md`, 명령·실제 값 `sol-pr1-raw/`.
- Astra는 Sol 완료 보고 원문과 주요 자체점검 값·11파일 최종 hash, AGENTS/goal-loop/.gitignore/RESUME 실제 diff를 직접 확인했다. 메인 CLAUDE 실제 diff SHA256은 전달값 `b2935a577835718c0a92f13422b6f7ebba3c03c25a6a343c4546bfc6145b2a0a`와 일치한다. 이 부분 대조를 전체 독립 실사로 보고하지 않는다.
- 이 goal·CLAUDE·PR177 이관·Sol 산출물 14파일을 `review-pr1-input.diff`/`review-pr1-input-manifest.json`에 고정해 신규 Opus Task `task_54f89c1d05a6`/Dispatch `ctx_737a76d67db9`로 실사했다. 최초 명령 `claude --model claude-opus-5-5`·화면 Opus5.5 xhigh, backend unknown. readiness satisfied·선택창 없음·attach ready/turn_started는 `review-pr1-launch-notes.md`와 원시로 구분한다.
- 독립 원문 `review-pr1-verdict.md`: **차단 #1·#2, 비차단 #3~#7, 설계 관찰9건**, 필수 미검토 없음. `msg_ec349480644f`의 lifecycle outcome succeeded는 검증 작업의 완료이며 제품 통과가 아니다. 고정40파일/hash/diff·Sol +111/-26·로컬 링크54·결정/숫자·PR177 역사 CI 값을 독립 대조했다. 새 빌드/Unity/게임/DB/CI·외부 URL 실행은 없다.
- 검증자 검색 범위 이탈3건은 관측 뒤 축소 안내 `msg_5c66762ede8c`, 수신·한계 기록 `msg_a95633d21be3`와 `review-pr1-raw/00-search-scope-note.txt`에 보존했다. 과거 준수로 소급하지 않는다. 보고-실제 수행 불일치/미실행 통과 사례는 독립 판정에서 찾지 못했다고 기록했다.
- 검증자 release retained/external_terminal/processAction none 뒤 같은 incarnation 확인·close ptyKilled=true(`review-pr1-release.json`·`review-pr1-before-close.json`·`review-pr1-close.json`). 이전 판정/입력을 보존하며 재사용하지 않는다. 다음 수정·재실사는 새 세션이다.

## PR1 독립 결함과 결정 대기

| 번호 | 상태·귀속 | 처리 경계 |
|---|---|---|
| 1 - RESUME 1항 잔여 세 파트 | 독립 재실사 해소 확인 / Astra 계약·goal·메인·Sol | 메인 `msg_c9d3dd5e196d`에 따라 1항 두 줄을 R-1 현재 리드로 좁혔고 다른 문장은 보존 |
| 2 - 규칙 문서 증가 결정 연결 | 독립 재실사 해소 확인 / Astra 계약·goal·Sol | r1 당시에는 ORCA 270→263줄·상한 exit1/초과13이었다. r2·r3의 추가 승인 이관 뒤 실측249줄·39,800 bytes와 현재 규칙 보존을 신규 Opus가 확인했다. 과거 실패 원시는 보존 |
| 3 - Content 형제 worktree 링크 | 독립 재실사 해소 확인 / Sol | CURRENT 같은 파일의 Content worktree 안내 anchor로 연결하고 미통합 로컬 goal의 내부 경로를 코드 문자열로 명시. 없는 저장소/원격 링크로 가장하지 않음 |
| 4 - 템플릿 안내 대상 부재 | 독립 재실사 해소 확인 / Sol·기존 문서 | .gitignore의 부정확한 템플릿 주석 한 줄만 삭제, 나머지 bytes와 ignore 패턴 보존 |
| 5 - PR177 이관 기록 시제 | 독립 재실사 해소 확인 / Astra | 원래 종료 시점의 상태임을 별도 문단으로 표시. 원이관0cc8ff9 내용/동일성 근거 보존, 현재 main 반영 완료로 쓰지 않음. 신규 Opus가 blob과 시점 안내를 대조 |
| 6 - 복구 메시지 시각 | 독립 재실사 해소 확인 / Sol | ORCA checkpoint 출처와 goal에서 created_at 01:00:10/01:40:32 KST와 제목 표기01:10/01:40을 구분 |
| 7 - Sol split 원시 한계 | 독립 재실사 해소 확인 / Astra | 위 실행 기록에 split JSON 미보존·도구 기록/화면 대조의 범위 명시. 신규 Opus가 한계 기록을 확인 |

결정 요청 원문 `review-pr1-main-decision-request.txt`, status 재전달 `msg_17b7266dc37f`(2026-10-04T18:41:03Z). 최초 type=decision_gate CLI는 sender_not_assignee였으나 메인이 `msg_061f93aebd43` 원문을 참조해 회신했다. 따라서 최초 비전달을 단정했던 기록을 정정하고 실패 receipt/메인 수신 관측을 구분한다. #1 메인 판단 `msg_c9d3dd5e196d`, #2 사용자 B 전달 `msg_6c248ebe130e` 원문은 `main-review-pr1-decisions.json`·`main-pr1-growth-decision.json`에 있다. 보관 브랜치 삭제 뒤 고정 f0f23f7 링크가 PR125 ref에 의존한다는 독립 관측은 삭제 전 메인 확인 대상으로 남았다.

메인 정리 확인 지시 `msg_cfcda024031c` 뒤 `review-pr1-post-close-terminal-list.json`과 worker-list를 대조했다. **close 뒤 terminal list 재확인: 남은 작업자 pane 0**이며 Rules Astra만 남았다. worker-list의 두 완료 Task handle은 실제 목록에 없다. 현재 PR·push·CI·병합·PR2·전체 Gardener는 미실행이다.

### 수정 r1 실제 결과

- 새 Sol Task `task_bfd4962bbdab`/Dispatch `ctx_7b527e75c398`, `msg_1e2c8d297fb4`(2026-10-04T19:18:58Z)로 허용 수정·보고와 쓰기 종료. `sol-pr1-repair-report.md` 전체를 직접 읽었다. 지정/최초 명령/화면은 gpt-6.1-sol max, backend unknown. 최초 attach 미확인과 공식 draft Enter 1회 복구·실제 시작은 `sol-pr1-repair-launch-notes.md` 및 원시로 분리한다.
- 네 문서 증분 +8/-16. 자체 점검45항목/실패0, 네 파일 밖 추적2815파일과 별도 입력15파일 hash 보존, ignore87개와 Rules Unity H3파일 유지. 최초 검사 SyntaxError는 보존했다. 별도 `size-limit-check.json`은 263>250/초과13/exit1, 최종 guard는 `overallPR1Passed=false`다. 작성 회차의 succeeded를 PR1 PASS로 바꾸지 않는다.
- Astra는 ORCA/CURRENT 증분 diff, 실제4파일 hash, 보고/context hash와 원시 점검 값을 표본 대조했다(`astra-repair-source-check.json`). 제품 빌드·Unity·게임·DB·새 CI·외부 URL은 미실행이다.
- release retained/external_terminal/processAction none → 같은 incarnation 확인 → close ptyKilled=true → terminal list에서 Astra만 남음. **close 뒤 terminal list 재확인: 남은 작업자 pane 0**(`sol-pr1-repair-post-close-list.json`). 이 수정자도 재사용하지 않는다.
- 추가 판단 요청은 `msg_5891c5a06bdd`(19:21:38Z), 원문 `pr1-263-lines-decision-request.txt`다. 남긴 현재 규칙은 R-1 배치, R-3 조건·종료와 helper 경계, crash/checkpoint/Git 권한, 실패 집계, 대시보드·Unity 경계다. 애매한 결합 문장은 ORCA31의 승인 출처, 38의 현재 리드와 당시 승인 응답, 174의 checkpoint 권한과 원문 시각이다. 기존 문장은 이동하지 않았고 새 관찰 절 heading/anchor도 진입 보존을 위해 유지했다. 사용자 판단 원문을 받은 뒤 새 Opus 입력을 고정한다.

<a id="pr1-environment-observations"></a>
## 2026-10-04~05 환경·복구 기록

사용자 B에 따라 ORCA에 이번 PR1에서 추가했던 시점 기록의 목적지를 이 goal로 정했다. 아래는 **메인이 전달한 이 PC의 당시 관측**이며 Rules가 설정을 바꾸거나 접속·보안·복구를 새로 검증한 결과가 아니다. ORCA의 기존 문장과 현재 적용 규칙은 이동하지 않는다. 첫 복구 메시지의 시각은 독립 #6에 따라 created_at과 제목 표기를 구분했다.

| 시점·환경 | 메인 관측과 결정 | 적용 한계·출처 |
|---|---|---|
| 2026-10-04, Orca 접속 | 사용자가 Public 방화벽 규칙을 비활성화했고 Relay 접속을 확인했다. | 이 PC의 관측이며 다른 머신·배포 환경은 별도 확인한다. 새 방화벽/전역 변경 허가가 아니다. [HANDOFF 결정4](../../../.backups/verification/2026-10-05-operating-canon/sources/handoff-decisions.md) |
| 2026-10-04 22:39 KST, 이 PC | 사용자가 SAC를 껐고 메인이 확인했다. | SAC Off는 사용자 유지 결정의 환경 사실이다. SAC On VM/클라이언트 배포 정책은 배포 goal에서 별도 실측·판단하며 여기서 실행하지 않았다. [HANDOFF 결정10](../../../.backups/verification/2026-10-05-operating-canon/sources/handoff-decisions.md) |
| 2026-10-05 01:00:10 KST 생성(제목 표기01:10), Orca1.4.220 복구 보고 | Astra 탭의 Codex 대화를 유지한 채 새 handle로 복구했다. 작업자 split pane은 복구되지 않았다. | [첫 복구 지시](../../../.backups/verification/2026-10-04-teammate-onboarding/main-crash-recovery.json), `msg_0025fdc1a3c1`. 모든 세션의 자동복구 보장으로 일반화하지 않는다. |
| 2026-10-05 01:40:32 KST 생성(제목 표기01:40), Orca1.4.217 복구 보고 | 일부 탭만 복구했고 메인이 `codex resume <session-id> -m gpt-6-astra -c model_reasoning_effort=xhigh`로 리드를 다시 열었다. 작업자 split pane은 이 버전에서도 복구되지 않았다. | [두 번째 복구 지시](../../../.backups/verification/2026-10-04-teammate-onboarding/main-crash-recovery2.json)·[최초 운영 지시](../../../.backups/verification/2026-10-05-operating-canon/msg_05ba75ccd7f9.json). 당시 명령 기록이며 현재 handle/세션 재사용 권한이 아니다. |

<a id="orca-14217-source"></a>
### 1.4.217 임시 적용의 출처

메인 결정 `msg_2efcbd0d05d1`(2026-10-04T16:49:34Z)은 사용자의1.4.217 복귀 동안 기존 R-3 공식 ask/reply 예외를 같은 조건으로 적용하라는 운영 결정이다. [메인 원문](../../../.backups/verification/2026-10-04-teammate-onboarding/main-r3-1217-decision.json)·[ask help](../../../.backups/verification/2026-10-04-teammate-onboarding/crash2-orca-ask-help.txt)·[reply help](../../../.backups/verification/2026-10-04-teammate-onboarding/crash2-orca-reply-help.txt)를 보존한다. 아직 적용 중인 조건·종료 조건·helper 버전 위장 금지는 [ORCA R-3](../../../00_Document/operations/ORCA.md#r3-reply-tag)의 현재 규칙이며, 시점 기록인지 애매한 경우 남기라는 사용자 결정에 따라 유지할 대상으로 분류한다. 이는 r1 당시 Sol에게 준 보고 지시였으며, 실제 결과는 위 「수정 r1 실제 결과」와 이후 r2·r3 및 재실사 결과에 기록했다.


<a id="orca-source-relocation-r2"></a>
## ORCA 출처 이관 — 수정 r2 결정 대조표

메인 전달 사용자 원문 「1) ORCA 규칙 문서 263줄 - 시점 기록을 뺀 뒤에도 250줄 초과, 지금 더 줄일지 → B 지금 250줄로 맞춤」, 메시지 `msg_bbd37070c7b7`(2026-10-04T19:23:27Z). [전달 원문](../../../.backups/verification/2026-10-05-operating-canon/main-pr1-250-decision.json)을 보존한다. 이는 사용자 직접 입력으로 격상하지 않는다.

| 결정·원래 규칙 | 승인 범위와 적용 위치 | r2 당시 결과 |
|---|---|---|
| 라우팅 결정6 및 독립 결함 #2 · 사용자 원문 「1) ORCA 규칙 문서 263줄 - 시점 기록을 뺀 뒤에도 250줄 초과, 지금 더 줄일지 → B 지금 250줄로 맞춤」 · msg_bbd37070c7b7 | ORCA의 승인 출처·시각 및 출처 표 행을 이 절로 옮기고 해당 행 링크만 남김. 현재 규칙 자체 재작성·합치기·기존 문장 삭제·줄바꿈 축약 금지. R-1~R-8 등 진입 anchor 유지 | 새 Sol r2가 출처 14건을 실제 이관했다. ORCA 263→256줄/초과6, 43,486→41,464 bytes. 규칙·anchor 보존, 상한 exit1. 당시 더 줄이지 않고 후속 메인 판단을 요청했다. 상세는 아래 r2 실제 결과이며 이후 승인·해소는 r3와 재실사 결과에 기록 |

이관 전 기준은 ORCA r1 SHA256 `4b3a98fa7279e5d749eba722f04496503d24541935fc27dc5eae2c78d17fa0ee`이다. 아래 출처는 당시 승인 기록이며 실행 권한이나 현재 규칙 자체가 아니다. 현재 적용 규칙은 [ORCA](../../../00_Document/operations/ORCA.md)에 남는다. r1 수정분과 독립 #5·#7 기록은 유지한다.

| 원래 규칙·위치 | 전달 원문 | 식별자·시각 | 보존 위치 |
|---|---|---|---|
| <a id="orca-source-table-1"></a>R-1~R-7 / 이관 전 ORCA 26행 | R-1~R-7 | `msg_0f0b14870182`, 2026-10-01 13:14:47 UTC | [main-request.json](../../../.backups/verification/2026-10-01-operations-rules/main-request.json) |
| <a id="orca-source-table-2"></a>R-8 / 이관 전 ORCA 27행 | R-8 추가 | `msg_339a1cb74839`, 2026-10-01 13:15:57 UTC | [main-request-r8.json](../../../.backups/verification/2026-10-01-operations-rules/main-request-r8.json) |
| <a id="orca-source-table-3"></a>R-3 회신 예외 / 이관 전 ORCA 28행 | R-3 CLI 제약의 회신 예외 | `msg_fc7e6335130c`, 2026-10-01 13:29:31 UTC | [main-r3-decision.json](../../../.backups/verification/2026-10-01-operations-rules/main-r3-decision.json) |
| <a id="orca-source-table-4"></a>R-1 Architecture 배치 / 이관 전 ORCA 29행 | R-1 Architecture 파트 추가 | `msg_39f5b5bcb525`, 2026-10-02 03:14:21 UTC | [main-source-recovered.json](../../../.backups/verification/2026-10-02-architecture-part-rules/main-source-recovered.json) |
| <a id="orca-source-table-5"></a>R-1 목표 한정 파트 / 이관 전 ORCA 30행 | R-1 목표 한정 추가 파트 | `msg_9d5215e34c70`, 2026-10-02 메인 전달 | [현재 목표의 승인 결정](../../../01_Phases/goals/2026-10-02-agent-rule-context/goal.md#현재-상태와-승인된-결정) |
| <a id="orca-source-table-6"></a>R-1 마감 다섯 리드 / 이관 전 ORCA 31행 | 마감 다섯 리드·운영 결정 반영 | `msg_05ba75ccd7f9`, 2026-10-04T17:08:39Z 메인 전달; 범위 일치 `msg_cf9c705dcf36` | [운영 정본 반영 goal](../../../01_Phases/goals/2026-10-05-operating-canon/goal.md#요구사항-원천과-적용-결정) · [HANDOFF 결정 발췌](../../../.backups/verification/2026-10-05-operating-canon/sources/handoff-decisions.md) |

| 원래 규칙·위치 | 이관할 승인 출처·시각 원문 |
|---|---|
| <a id="orca-source-leads"></a>R-1 다섯 리드 / 이관 전 ORCA 38행 | 사용자는 도구·운영 축소안을 채택하지 않고 승인된 순서를 유지했다(2026-10-04 23시대 KST, 로드맵 응답 1A·2A·3A·4B; 후속 파트별 범위 5건 A). 출처는 [마감 결정 사본](../../../.backups/verification/2026-10-05-operating-canon/sources/deadline-roadmap-draft.md)과 위 메인 전달이다. |
| <a id="orca-source-checkpoint"></a>crash checkpoint 권한 / 이관 전 ORCA 174행 | 근거는 메인 복구 지시 `msg_0025fdc1a3c1`(2026-10-05 01:00:10 KST 생성, 제목 표기01:10)·`msg_bcd8d484a61d`(01:40:32 KST 생성, 제목 표기01:40)의 [첫 원문](../../../.backups/verification/2026-10-04-teammate-onboarding/main-crash-recovery.json)·[두 번째 원문](../../../.backups/verification/2026-10-04-teammate-onboarding/main-crash-recovery2.json)이다. |
| <a id="orca-source-r3-reply"></a>R-3 reply 버전 한정 예외 / 이관 전 ORCA 88행 | 메인 결정 `msg_fc7e6335130c` |
| <a id="orca-source-r3-ask"></a>R-3 공식 ask 예외 / 이관 전 ORCA 90행 | 근거는 메인 범위 판정 `msg_09a19a463a74`(2026-10-03T18:06:16Z)의 [원문](../../../.backups/verification/2026-10-03-harness-principles/operating-rules/main-official-ask-decision.json), [ask help](../../../.backups/verification/2026-10-03-harness-principles/operating-rules/orca-ask-help.txt), [실제 질문](../../../.backups/verification/2026-10-03-harness-principles/operating-rules/sol-heartbeat-boundary-question.json)이다. |
| <a id="orca-source-capacity"></a>capacity 예외 / 이관 전 ORCA 158행 | 이는 메인이 전달한 `msg_bc5d8b721551`(2026-10-03T11:15:30Z)의 승인 예외이며 [현재 하네스 goal의 원문 결정](../../../01_Phases/goals/2026-10-03-harness-principles/goal.md#정본-반영-전-적용-중인-사용자-결정)에서 찾는다. |
| <a id="orca-source-confirmed"></a>확정 실패 3회 뒤 Advisor / 이관 전 ORCA 167행 | 이 규칙의 출처는 메인이 전달한 사용자 최종 결정 `msg_22a9b4109ee7`(2026-10-03T07:08:40Z)이며 [하네스 goal의 적용 결정](../../../01_Phases/goals/2026-10-03-harness-principles/goal.md#정본-반영-전-적용-중인-사용자-결정)으로 연결한다. |
| <a id="orca-source-draft"></a>공식 계약 draft 복구 / 이관 전 ORCA 185행 | 근거는 메인 운영 결정 `msg_c1412c982ac5` 및 보강 `msg_b10d232dce1b`(2026-10-03T11:29:24Z), [하네스 goal의 재개/설계](../../../01_Phases/goals/2026-10-03-harness-principles/goal.md#운영-규칙-pr-재개와-설계)다. |
| <a id="orca-source-gardener"></a>종료 Gardener 파일럿 / 이관 전 ORCA 232행 | 출처는 `msg_c9bc79f8ec43`의 사용자 채택을 보존한 [하네스 goal](../../../01_Phases/goals/2026-10-03-harness-principles/goal.md#정본-반영-전-적용-중인-사용자-결정)이다. |

위 목적지로의 실제 이관은 r2 원문 보고와 source-relocation.json에 연결한다. r2 당시에는 250줄을 충족하지 못했으며, r3와 재실사에서249줄을 확인했다. 이관 전 문맥의 관측·한계·조건은 현재 문서에 보존했으며 최초 실패 원시는 별도로 남긴다.


### 수정 r2 실제 결과와 추가 판단

- 새 Sol Task `task_073a0540576f` / Dispatch `ctx_7dbed91f0ba3`, 완료 `msg_fc8cdf33199c`(2026-10-04T19:56:34Z). 실제 split은 gpt-6.1-sol effort max, 화면 일치/backend unknown. 이번 최초 attach는 ready/input_accepted/turn_started이며 Enter 복구를 하지 않았다. 원 receipt와 `sol-pr1-r2-launch-notes.md`를 보존한다.
- 쓰기 종료 2026-10-04T19:55:15.236Z; ORCA 한 파일 증분 +12/-19, 263→256줄, 43,486→41,464 bytes. 표 8행을 링크 1행으로 바꾼 감소 7줄이며 본문 출처 교체는 줄 감소 0이다. 승인 출처 14건과 참조만 이동, 나머지 244줄·빈 줄 96개·명시 anchor 15개·heading 21개를 보존했다는 작성자 근거는 raw/line-mapping.json·source-relocation.json에 있다.
- 최초 문서 자체검사 50항목 중 48통과/2실패, 상한 검사 exit1/초과6/overallPR1Passed=false. 실패는 Astra가 쓴 goal 표 4열/데이터 3열 및 로컬 근거 5개 부재다. ORCA 밖 고정 입력22개·추적2818파일 hash 보존. lifecycle succeeded는 전체 PR1 통과가 아니다.
- Astra는 완료 뒤 보고 원문·증분 diff 전체와 ORCA/report/context hash를 직접 대조했다(`astra-r2-source-check.json`). goal의 누락 구분자 6개를 보완하고, 메인 checkout의 4개·architecture-active의 1개 원본을 식별자·hash 대조 후 해당 로컬 경로에 보존했다(`astra-r2-source-restoration.json`). 원본과 복사본 bytes가 같으며 새 실행 근거가 아니다. Sol의 최초 실패를 성공으로 소급하지 않는다.
- release retained/external_terminal/processAction none 뒤 동일 incarnation ebcc836b-917c-4d0c-be8a-6259de56ed55를 확인해 close ptyKilled=true. `sol-pr1-r2-post-close-list.json`에는 Astra만 있어 남은 작업자 pane은 0개다. 이전 완료 세션을 재사용하지 않았다.
- 250줄 이후 물리행 251·253·255는 기존 빈 줄, 252는 09-29 터미널 재사용 관측, 254는 10-04~05 진입 heading, 256은 r1의 환경 기록 링크다. 이 여섯 줄 자체를 잘라야 한다는 뜻이 아니다. 승인 출처 이동만으로 추가 감소가 없어 사용자 판단이 필요하다. 새 Opus·PR·CI·병합 및 제품 빌드/Unity/게임/DB는 미실행이다.

후속 판단 요청은 `msg_4b7ebca13688`(2026-10-04T20:00:57Z), 원문 `E/pr1-256-lines-decision-request.txt`다. 제안 B는 현재 ORCA245~252의 09-29 관측 표 2건·두 식별자 문단을 goal에 온전히 보존하고 해당 위치를 링크1행으로 바꾸는 것으로 예상249줄이다. 241 heading·243 구분 문장과 모든 현재 규칙/anchor는 유지한다. 기존 관측 전체 이관은 앞선 승인 밖이므로 실행하지 않았다. A는 이번 PR1 한정256줄 허용으로 별도 사용자 결정이 필요하다. 이 선택을 대신 판단하지 않으며 답변 전 추가 Sol/Opus·PR 발행은 보류한다.

## 수정 r3 결정 대조표

메인 전달 msg_cb365b462610 (2026-10-04T20:08:41Z)의 [원문](../../../.backups/verification/2026-10-05-operating-canon/main-pr1-history-decision.json)을 보존한다. 메인이 전달한 사용자 결정이며 직접 사용자 입력으로 격상하지 않는다. 사용자 원문 전체는 다음과 같고, 안건1·2는 GameDev 소유이므로 이 goal의 권한으로 가져오지 않는다.

> 「대시보드 결정 응답: 1) TESTDB-01 - 실제 DB 검사 도구 결함을 DB 단계 전에 고칠지 → A DB 단계 전에 좁게 고침 · 2) INSTALL-05·06·07 최종 검증 - 절차 이탈을 기록한 채 결과를 받아들일지 → B 새 검증자로 다시 · 3) ORCA 규칙 문서 256줄 - 09-29 옛 관찰 기록까지 밖으로 옮길지 → B 09-29 관찰 기록도 밖으로」

| 결정·원래 규칙 | 승인된 변경·보존 범위 | 적용 상태 |
|---|---|---|
| 라우팅 결정6·독립 #2 · 안건3 사용자 B 「09-29 관찰 기록도 밖으로」 · msg_cb365b462610 | ORCA245~252의 표2건·초기 시도/기존 터미널 식별자 문단8줄을 아래 역사 기록으로 그대로 보존하고 원위치에는 링크1행. 241 heading·243 두 시도 구분 문장·모든 현재 규칙·R-1~R-8·10-04~05 진입 보존 | Astra가 목적지를 준비한 뒤 새 Sol r3가8줄→링크1줄 이관 완료. 실제256→249줄, 41,464→39,800 bytes. 원문·앞뒤 bytes·heading/anchor 보존 자체점검 exit0. 이후 신규 Opus가 독립 대조해 해소 확인 |

<a id="orca-20260929-history"></a>
### 2026-09-29 Orca 시도 당시 관찰 원문

아래는 ORCA r2 SHA256 `695d637fd5718cae95bcbfb24e48b0f67848852736171bbfcd4ab761fcb15181`의245~252행을 내용 변경 없이 보존한 역사 기록이다. 당시 수치·모델·검사·권한을 이 goal의 새 실행 결과로 삼지 않는다. 코드 문자열의 TEMP/로컬 경로는 당시 위치이고 공개 PR 링크만 외부 URL이다. 현재 규칙·두 시도 구분과 진입 heading은 [ORCA](../../../00_Document/operations/ORCA.md#관찰-기록-2026-09-29)에 남긴다.

| 시도 | 실제 관찰 | 남은 확인 |
|---|---|---|
| 초기 새 worker 자동 시작 | Orca 1.4.216 연결, 요청·실제 모델 `gpt-6-astra` 일치. Codex TUI는 열렸으나 `agent_readiness`에서 60초 timeout. Task 주입과 `worker_done` 없음. 공식 release 후 `reclaimable=0`, 기존 메인 터미널 유지. | 새 worker 자동 시작·지침 선택·완료 수신은 미검증. 정확한 readiness 실패 원인은 미확정. |
| 사용자 기존 터미널 재사용, MSSQL 목표 | 실제 작업 주입·heartbeat·질문 응답 후 기본 테이블 구현과 독립 Windows 테스트 40개 통과. 사용자 승인 하 관리자 설정 적용 후 네이티브 WSL 로그인·저장·rowversion·rollback·최소 권한 검사 통과. | 기록 시점 최종 push·CI와 `worker_done` 수신 마감 중. 서버 런타임 저장 통합과 관리자 Restore 실행은 미검증. 새 worker 자동 시작 성공으로 해석하지 않는다. |

초기 시도: Run `run_85f26e01d393`, Task `task_d783e29c2337`, Dispatch `ctx_7e635e1b1d19`. 로컬 원본은 `%TEMP%/dawnholder-orca-smoke-20260929/`에 있다.

기존 터미널 재사용: Run `run_af1e4581e074`, Task `task_8973cd06c2c8`, Dispatch `ctx_cbd3c2c78d02`, Terminal `term_103c9e14-2792-432e-8a0e-b00215dbe0fd`. 로컬 상태 증거는 `.backups/mssql-orca-coordination-20260929/worker-show.json`이다. 작업 공간은 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/feat-mssql-game-schema`, 브랜치는 `bass131/feat-mssql-game-schema`로 문서 정비 공간과 분리했다. 결과와 당시 인계는 별도 [PR127](https://github.com/bass131/dawnholder-server/pull/127)에 기록했다. SQL 테스트의 트랜잭션 rollback 통과와 관리자 설정 Restore 실행 검증은 구분한다. 당시 관찰 시점에는 PR 병합 승인이 없었다.


### 수정 r3 실제 결과와 재실사 입력

- Task `task_f3b1d921b2cd` / Dispatch `ctx_8ae4489a2441`, 완료 `msg_b16fe99f5af8`(2026-10-04T20:27:51Z). 최초 split gpt-6.1-sol effort max, 화면 일치/backend unknown. 최초 attach ready/input_accepted/turn_started이며 복구 Enter 없음. `sol-pr1-r3-launch-notes.md`와 원 receipt에 연결한다.
- 승인된 원문8줄은 위 `#orca-20260929-history`에 내용 변경 없이 보존했다. 원문/approved block/목적지 SHA256 `c923cf4e8506f72013da7eae5f77efba5932ed1e6066dea85754a3727a0cc42e`, 현재 ORCA245는 링크1줄이다. 앞244줄·뒤4줄 bytes 및 명시anchor15개/heading21개, 다른고정입력21개 hash 보존은 `sol-pr1-r3-raw/verify.json`의 작성자 근거다.
- 실측256→249줄, 41,464→39,800 bytes, 현재 ORCA SHA256 `e526d559cb8005e9f81339a7b57d125a26aec5cbc0367087c74ae0169b3511ec`. before/apply/verify 명령 exit0, diff --check exit0, no-index diff exit1은 의도한 차이 존재다. 검사는 한 번 수행했고 실패·범위 확대·기대값 완화 없음. 이는 r3 자체점검 결과이며, 이후 PR1 전체 독립 판정은 아래 절에 기록한다.
- 제품 쓰기 종료20:22:14.264Z, 자기 근거 종료20:27:23.0708815Z. 보고 전체와 실제diff/원시를 Astra가 직접 읽고 파일 수치/hash를 대조했다(`astra-r3-source-check.json`). 원 r1/r2/첫 독립 결과·실패를 소급하거나 덮어쓰지 않았다.
- release retained/external_terminal/processAction none 뒤 같은 incarnation 확인·close ptyKilled=true. `sol-pr1-r3-post-close-list.json`에 Astra만 남아 작업자pane0이다. r3 종료 당시 다음 단계로 최종 PR1 diff/goal과 각 원문을 새 manifest에 고정해 신규 Opus 재실사를 발행했다. 제품 빌드/Unity/게임/DB·새CI/PR/병합은 미실행이다.

## PR1 독립 재실사 결과와 통합 기록

- 새 Opus Task `task_530effe5e849` / Dispatch `ctx_a93695080d22`, 완료 `msg_a7a66a68ea79`(2026-10-04T20:52:03Z). 지정 `claude-opus-5-5`, 최초 명령 `claude --model claude-opus-5-5`, 화면 Opus5.5 xhigh, backend unknown. 최초 attach ready/input_accepted/turn_started, Enter 복구 없음.
- [판정 원문](../../../.backups/verification/2026-10-05-operating-canon/review-pr1-recheck-verdict.md)을 Astra가 전부 읽었다. **문서 실사 통과, 차단0·관련 필수 미검토0, 이전 #1~#7 해소.** 비차단 #8은 위 과거 회차의 상태 문장 표지4곳이며, 이 결과 갱신에서 r1/r2 당시임을 명시했다. 검증 후 goal 기록 변경이므로 기존73파일 manifest/diff와 동일 입력이라고 주장하지 않는다. 변경 diff·보존 hash는 `astra-pr1-post-review-integration.json`에 남기고 메인 R-2로 확인한다.
- 독립 실행 원시 `review-pr1-recheck-raw/00-commands.md`·`01`~`07`: 고정73파일/hash·diff 일치, ORCA249줄/39,800 bytes, 승인 단계별 원문 보존, 변경 줄 링크104개(로컬98정상·외부6미실행), 들어오는 anchor179개 정상. 검증자 도구 작성 오류2건은 원시에 보존했으며 성공으로 합산하지 않았다. Astra 표본은 `astra-pr1-recheck-source-check.json`이다.
- 설계 관찰8건은 판정 원문에 보존한다. PR2 관련 외부 팀원 맥락은 기존 승인 범위에서 다루며, Rules 지위/Unity 관측 checkout/링크 글자/규칙 증가 기준/CodeMap 순서 등은 새 정책으로 채택하지 않는다. 복구 기록의 「제목 표기」는 실제 메시지 본문 머리의 시각 표기이며 subject 필드가 아니다. 이관 원문 자체는 바꾸지 않았다. 보관 브랜치 삭제와 삭제 뒤 일회성 안내 정리는 메인 담당이다.
- release retained/external_terminal/processAction none → 같은 incarnation `533b714f-17e8-434d-9012-b50da5e212dd`와 완료 대화·빈 prompt 확인 → close ptyKilled=true. **2026-10-04T20:52:49Z(10-05 05:52:49 KST) terminal list에 Astra만 남아 작업자pane0**(`review-pr1-recheck-post-close-list.json`). 완료 보고 직전 목록을 다시 확인해 시각을 전달한다.
- 제품 빌드·Unity·게임·DB·외부 URL·원격 ref는 이 재실사에서 미실행. PR·CI·병합은 아직 미실행이며 전체 goal은 PR2·Gardener·종료 점검까지 남아 있다.

### 다음 후보와 pane 재등장 관측

메인 `msg_03395836b2df`(2026-10-04T20:29:42Z)는 close 직후와 완료 보고·R-8 직전의 terminal list/시각 확인을 임시 적용하고, 정본화는 이번 PR1 밖의 다음 후보로만 기록하라고 했다. 후보: **완료 작업자 pane의 재등장 여부를 보고 직전 다시 확인하는 정산 정본 보완**. 새 BACKLOG 행·현재 정본·PR2 범위를 추가하지 않으며 다음 goal 자동 착수 없음.

실제20:34:13Z에 종료한 r3 Sol의 같은 leaf가 새 handle `term_b8450582-e7f0-409f-ac82-37b642231be9`로 보였다. 완료 대화와 빈 prompt를 제한 read/show로 확인하고 입력 없이 정확 새 handle을 close했다.20:34:52Z 재확인에는 Astra와 당시 활성 Opus만 남았다(`reappeared-pane-first-read.json`·`first-show.json`·`close.json`·`post-close-audit.json`). 원인과 메인이 말한 전체6회는 Rules의 재검증 결과가 아니다.
