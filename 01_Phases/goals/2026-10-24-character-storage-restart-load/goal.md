# 캐릭터 저장의 서버 재기동·불러오기 합동 확인

장면 9에서 서버를 끊었다 다시 켜도 같은 캐릭터의 직업이 유지되는지 실제 저장 경로로 확인한다. World의 캐릭터 서버·Stored 봇과 연결하되 Core 저장 검증과 Content 화면 완료를 구분한다.

**상태: 사용자 지시로 세션 마감, 정적 READY·실제 합동 미실행인 채 다음 세션으로 인계.** 메인 `msg_b88e88c29142`가 범위 v1 초안 SHA256 `2C87D4B0B9CCB4F3D759B01DA8B966BF24F1537F82C717C166B4E13CCA04AF3A`를 대조 승인했고, `msg_85c64266bdb2`의 사용자 A로 Town·HP 관측은 Content에 이관했다. 준비 작업자는 정산·종료했다. 제품/시험 코드 변경·DB 접근·빌드/서버/봇/WSL/Unity 실행은 없으며, 목표 완료나 독립 검증 통과가 아니다.

## 재개 지점

기록 시각은 **2026-10-24T06:45:15Z (15:45:15 KST)**다. `C:/Program Files/Git/usr/bin/date.exe -u +%Y-%m-%dT%H:%M:%SZ`의 실제 출력·exit0을 `joint-runtime-01/session-close-time.json`에 보존했다. Main `msg_e0a50e1633c0`가 메인 pane Enter 제출 사용자 원문 「일단 새로운 세션에서 이어서 진행하자」를 전달했다. 원문 제출시각은 메인의 「2026-10-24 15:4x KST」 표기 그대로이며 이 Core pane 직접 입력으로 격상하지 않는다. **오늘 Core 합동60분은 시작하지 않고 새 작업자·검증자·단계·실행을 열지 않는다.**

**다음 첫 일:** 새 메인 세션에서 사용자 준비와 현재 자원을 확인해 **첫 사용자 창을 Core 합동60분으로 잡고 정확 T를 다시 통지**한다. 「아침 첫 창은 Core 합동」 사용자 결정은 유효하다. 이후 현재 Core/World 입력·소유권을 다시 대조하고 신규 외부 실행자·최종 계약을 발행한다. 과거 Level A75 반환예정05:43·이전 창은 실행 권한으로 재사용하지 않는다. 사용자가 직접 두 서버를 기동하고 메인 신호 뒤 각각 Enter를 누른다.

**열린 Task·Dispatch 없음. 작업자·검증자 pane 0.** 현재 terminal 목록은 이 리드 하나뿐이다. 역사 준비 Task `task_75cf8d9e51df`/Dispatch `ctx_1448a0a12d84`는 succeeded/completed/settled다. external retained 회계1은 남지만 정확 child pane은 이미 종료됐다. 현재 관측은 `joint-runtime-01/session-close-open-state.json`과 원본 terminal/worker/task JSON에 있다.

정적 계약 초안 `joint-runtime-01/contract-draft-v1.md` SHA256 `8C2002CDF95D03C4EBAB4079D62B0D99C07FEAB761001ADE3006970C556983E2`, 권한 보충 `authority-note-v1.md`, 사용자 카드·계획·입력 검토 `preparation-01/lead-ready-review.md`를 이어 읽는다. 이들은 준비 당시 HEAD의 snapshot이며 마감 checkpoint 뒤 최종 계약 발행 시 새 HEAD/현재 goal·source/public/fresh DLL hash·경로·자원을 다시 고정한다. 이전 준비자를 재사용하지 않는다.

실제 남은 범위는 같은ID·저장Mage의 before/after 목록/select/load와 별도 Linux 서버 두 프로세스·정상 flush/종료·7777해제다. Town·현재/최대HP는 Content 조건3·4 접수만 확인했고 실제 Unity 관측은 아직 없다. Core 합동 후 같은ID/서버교체 원시를 Content에 추가 인계한다. 신규 build/test/DB/WSL/서버/봇/Unity와 강 독립 판정은 모두 미실행이다. 사용자 능동2~4분·대기포함25~30분은 미측정 추정이다.

- 작업 경로: `C:/Users/bass1/orca/workspaces/DawnHolder_Project/core-active`
- branch: `feat/character-storage-restart-load-20261024`
- fetch 뒤 기준 main/HEAD: `eda68f4315ba3039d602691a0ac2f309dc9d8b7c`
- 리드 대화 ID(`CODEX_SESSION_ID`): `01a16904-ecb2-77a2-b9e5-acee793111ae`. named 환경값과 정확 rollout의 cwd/id·최초 설정 `gpt-6.1-sol`/xhigh/priority를 대조했다(`joint-runtime-01/session-close-identity.json`), backend unknown이다.
- Run ID: `run_58bc61a38687`. 재개 시 현재 runtime/handle/incarnation과 대화를 다시 확인하고 같은 Run 인수 여부를 메인과 연결한다. 과거 handle/Task/Dispatch를 새 실행 권한으로 쓰지 않는다.
- 남은 사용자 차례: Core60분의 준비 신호·직접 두 기동/각 Enter와 향후 각 PR별 정확 head의 병합 승인이다. 즉시 미해결 A/B 판단은 없다. 밤사이 메인 판단 위임 원문은 아래에 보존하며 새 세션에서 현재 적용 여부를 대조한다.
- 근거 폴더: `.backups/verification/2026-10-24-character-storage-restart-load/`
- 진입 메모: 위 폴더 `core-entry-context.md`. 새 Run 회신 주소는 같은 폴더 진입 기록에 보존하고 메인에 통지한다. 과거 goal의 Run·Task·Dispatch·실행 창은 재사용하지 않는다.

## 요구사항 원천과 적용 결정

- 메인 `msg_bf4c327079b3`(2026-10-23T18:05:01Z)가 전달한 사용자 원문이다. 메인 pane Enter 제출이며 **이 Core pane의 직접 사용자 입력으로 격상하지 않는다.**
  > 대시보드 결정 응답: 1) Core 다음 목표 - 저장·재접속 장면을 영상에 넣고 캐릭터 서버를 이어 갈지, 동결 뒤로 미룰지 → A 장면 9 넣고 캐릭터 서버 재개
  > A로 가서 마무리 해야겠는데? 빨리 작업 진행해줘, 밤사이에 하면 되겠다
- 메인의 할당: 10-28 시연 영상에 장면 9 저장·재접속을 넣는다. Core는 서버 재기동·불러오기 합동 확인, World는 캐릭터 서버, Content는 캐릭터 선택·생성 화면을 각각 맡는다. 지금 권한은 최신 main 기반 새 branch·Run과 범위 초안 작성이며, 초안은 메인이 사용자 A와 대조 승인한다.
- 원천 후보는 [BACKLOG `character-storage-unexecuted-paths`](../../../00_Document/operations/BACKLOG.md)와 [앞 저장 goal의 완료조건5](../2026-10-17-character-storage-foundation/goal.md#범위) 중 **별도 서버 프로세스 재기동·불러오기 전체 경로**다. 이전 사용자 A로 미실행한 사실은 보존하고 이번의 새 실행 근거와 합산하지 않는다.
- 메인이 유지 전달한 최근 결정: 10-22 경량 경로(외부 구현자 한 명이 준비·실행·자체 확인·범위 안 수정, PR 직전 신규 독립 Opus 1회), 빠르게 기반 먼저. 기존 시험의 준비 코드·기대값 변경은 사용자 결정이며 조용히 고치지 않는다.
- 10-19 라우팅 유지 전달: 구현자 `gpt-6.1-sol` max, 같은 계약 두 번째 실패만 신규 `gpt-6-astra` xhigh. DB·위험 검증은 신규 Opus `--autocompact 250000`, 그 밖 검증 Sol xhigh, Gardener Opus. 모든 새 Codex는 `-c service_tier="fast"`와 첫 설정의 `service_tier: priority` 확인을 적용한다. 구현자와 독립 검증자는 분리하며 작업 하나 뒤 정산·종료한다.
- 밤사이 병합·새 사용자 결정·사용자 손·범위 확대가 필요하면 메인이 현황판에 올려 아침까지 기다린다. 사용자는 Unity Editor를 열지 않는다. 실제 실행 창은 메인이 READY 도착 순으로 하나씩 연다. Core가 타 파트 실행을 선점하거나 과거 창을 재사용하지 않는다.
- DB는 기존 승인과 같은 종류의 **기존 로컬 SQL Express 개발 DB**만 쓴다. 새 DB·DDL·관리자 권한은 아침 사용자 결정 대상이다. 원문 수신 사본은 근거 폴더 `entry-authority.json`에 보존한다.
- 메인 승인 `msg_b88e88c29142`(2026-10-23T18:25:13Z): 범위 v1·기존 후보/seed0/DB 쓰기 종류·조건3/8 제외를 그대로 승인했다. 앞 goal의 `msg_b48d51625b82`가 받은 「서버 기동 두 번만 사용자 직접 실행」 경계를 유지하므로 무인 credential 소비는 권한 밖이다. 밤사이 신규 Sol max Fast 준비→READY, 실제 합동은 아침 사용자 창이다. 메인 판단으로 World 최신 main 선통합→합동 입력 고정 순서로 바뀌었다. 승인 원문은 `inbox-after-confirmation.json`, 당시 문서는 `preparation-01/approved-goal-v1.md`에 보존했다.
- 메인 `msg_21058a23fe35`(2026-10-23T18:26:18Z)가 전달한 사용자 원문: 「대시보드 결정 응답: 1) 아침 사용자 손 일정 - 서버 두 번 직접 켜는 Core 합동 창과 Content 하우징 직접 플레이 창 중 무엇을 먼저 할지 → A Core 합동 창 먼저」. 이 pane 직접 입력으로 격상하지 않는다. 아침 Core 합동→Content 직접 플레이 순서이며 확정 시작 시각은 아직 없다. 원문은 `preparation-01/inbox-approval-ack.json`이다. World 통합 hash 인계가 늦으면 그 대기만 별도 막힘으로 보고한다.
- 메인 `msg_cc960ee0b998`(2026-10-23T19:27:09Z)는 정적 READY를 접수하고 아침 첫 사용자 창60분을 Core에 확정했다. Level A75 창19:28Z~20:43Z(05:43 KST 반환 예정) 뒤 사용자가 준비됐다고 말하면 메인이 fresh 자원 확인 후 정확 T를 통지한다. 그 전 정적 계약 초안만 고정하며 **발행은 T 뒤**다. 원시 `preparation-01/inbox-after-ready-request.json`이다. 사용자 준비·실제 Level 반환·자원 여유를 예정시각으로 대신하지 않는다.

### 아침까지 메인 판단 위임

메인 `msg_30e804b99028`(2026-10-23T19:51:26Z)이 사용자 부재 중 판단 위임을 전달했다. 메인 pane Enter 제출 원문이며 이 Core pane 직접 사용자 입력으로 격상하지 않는다. 메인이 적은 원문 제출 시각은 「2026-10-24 04:4x KST」로 보존하고 정확한 분을 추정하지 않는다.

> 나 이제 자러갈게, 나 자리 비울테니까 물어보지말고 메인 세션의 판단과 세컨드 브레인의 내 성향 기준으로 작업 마무리 이행해줘

사용자가 돌아올 때까지 goal 안 범위 판단, 기존 시험 준비 코드·기대값 변경, 관문·판정기 수리 방향 등 원래 사용자에게 올리던 판단은 **메인**에 요청한다. 메인은 개별 결정마다 위 원문과 근거 성향 번호를 함께 보내며 Core는 그 결정 원문/msg ID·근거 번호·영향을 기록한다. 이 전달 자체를 Core의 자율 범위 확대나 아직 오지 않은 개별 결정으로 사용하지 않는다. 현재 Core에 새로 적용할 개별 판단/성향 번호는 수신하지 않았다.

PR 병합의 사용자 「병합 승인」 한 줄, 사용자 손이 필요한 Core 두 서버 직접 기동/각 Enter·Content 직접 플레이, 전역 설정·권한 확인 우회, 새 파트/새 goal/승인 밖 공간은 그대로 사용자 차례를 기다린다. 실행 창도 메인이 READY 순서로 하나씩 배정한다. 따라서 현재 Core의 사용자 직접 실행·정확 T 대기는 유지되며 무인 기동/자동 Enter로 대신하지 않는다. 원시는 `joint-runtime-01/inbox-after-draft-fixed.json`, 미발행 실행 계약의 권한 보충은 `joint-runtime-01/authority-note-v1.md`다. 동결 초안은 바꾸지 않고 T 뒤 최종 계약에 이 보충과 실제 개별 결정을 반영한다.

### OBS-S9-01 — 사용자 A로 Content 이관

메인 `msg_ab3cbc6c5a65`(2026-10-23T18:58:51Z)가 Town/풀HP 관측 공백을 현황판 `core-town-hp-1024` 사용자 결정으로 올렸다. 이후 메인 `msg_85c64266bdb2`(2026-10-23T19:02:09Z)가 메인 pane Enter 제출 원문을 전달했다. 이 pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) Core 합동 확인 - 재접속 뒤 마을 입장·풀 HP까지 이번 합동에서 볼지, 직업 유지만 보고 마을·HP는 Content 실제 화면 확인으로 넘길지 → A 직업 유지만 보고 마을·HP는 Content로

Core 완료조건2에서 Town·풀HP 직접 관측을 제외하고 같은 CharacterId·저장 직업(Mage)의 목록·불러오기 일치와 별도 서버 프로세스 재기동을 확인한다. Town·현재/최대HP 실제 Unity 입장은 Content로 직접 인계한다. OBS-S9-01의 상태는 **Content 이관**이며 실제HP 관측 성공/제품 수리로 닫은 것이 아니다. 새 World 관측 코드/현재489 변경은 하지 않는다. 원문은 `preparation-01/inbox-before-conditional-a-dispatch.json`이다.

추천 A 보충v2 초안은 작성했으나 작업자 발행 전 위 사용자 결정이 도착했다. 미발행 v2와 이전후보를 보존하고 `preparation-01/supplement-v3-approved-a.md`를 남은 정적 문서 단계에 명시 적용한다. 준비 Task 완료와 실제 합동/독립 판정/전체goal 완료는 구분한다.

Content 경로는 상대 worktree `01_Phases/goals/2026-10-24-character-select-create-ui/goal.md`다. 현재3·4는 마을 입장·같은ID 재접속을 요구하며 사용자 A로 Town·현재/최대HP 실제 화면 관측을 명시 이관한다. 실제 수행/결과는 Content goal에 두고 Core가 미실행을 완료로 대신하지 않는다.

Content가 `msg_d1195a8a1274`로 인계를 접수하고 원천·완료조건3·4와 후보 DB 잔존 미확인을 연결했다. 리드가 상대 goal의 해당 문구와 실제 SHA256 `EC31D6CF800AFDEDDC2BCB6C06FCBD33FBBC091B0660007B837D06ADD7A3122B`를 대조했다. 실제 Unity/HP 관측은 아직 미실행이며, Core 합동 결과가 생기면 같은 후보 identity/list/load·서버교체 원시와 실제 확인 수준을 다시 Content에 전달한다. 접수 원시는 `preparation-01/inbox-v3-checkpoint-ack.json`이다.

## 범위

### 만들 것

1. 기존 Core 저장 API·실제 SQL Express·World Stored 통신 경로를 함께 통과하는 재기동 전후 실증. 같은 런의 계정/캐릭터 identity를 고정하고 **서로 다른 서버 프로세스**를 증명한다.
2. 실행 단계·입력 hash·DB 공개 대상·프로세스/포트 소유·서버 정상 종료와 제한시간·실행 창을 고정한 계약과 원시 근거. 사용자 손 없이 가능한 기존 기동 경로를 우선한다.
3. 이 경로를 막는 Core 소유의 기존 저장/host 결함이 실제 발견되면 번호와 귀속을 남기고 승인 범위 안에서 최소 수정·관련 회귀를 수행한다. 새 저장 기능·스키마·공개 계약 변경은 포함하지 않는다.
4. PR 직전 신규 Opus의 강 독립 판정, 결과·미실행 범위·장면 9 촬영에 전달할 실제 관측의 정리. Core의 봇/DB 실증을 영상·Unity 화면 완료로 보고하지 않는다.

### 건드릴 곳

- 기본 산출물은 이 goal와 필요한 실행 안내, `00_Document/operations/CURRENT.md`의 Core 줄, 근거 폴더의 계약·실행 입력/로그·판정이다.
- 범위 안 결함이 있을 때만 `02_Server/GameServer/Persistence/`, `02_Server/GameServer/Hosting/PersistenceRuntime/`의 해당 기존 경로와 `02_Server/GameServer.Tests/Persistence/` 관련 시험을 한정한다. 실제 수정 파일은 발행 계약에 기계 확인·고정한다.
- 기존 `99_Tools/headless-bot/Scenarios/Persistence/`의 **Stored 봇 확장은 World 소유**다. Core는 계약·관측을 합의하고 고정된 World 산출물을 사용한다. Core에서 중복 봇·새 정본 DB 관리 도구를 만들지 않는다.
- 빌드/통합 입력은 Core와 World의 실제 SHA 및 파일 hash로 묶는다. 미병합 World 코드를 main 반영으로 표현하지 않는다. 합동 snapshot이 필요하면 파일 소유·입력 manifest를 먼저 고정하고 checkout 사이의 무단 복사·브랜치 전환을 하지 않는다.

### 하지 않을 것

- 완료조건3의 검사~적용 사이 동시 writer 재현과 완료조건8 중 Resume Verify·Cleanup은 **장면 9 재기동 경로와 독립이므로 제외**한다. 필요성이 새로 확인되면 이유·영향을 메인에 올리고 의존 진행을 멈춘다. 기존 미실행 한계는 남긴다.
- 새 DB 생성, migration/DDL/module/release/profile 계약 변경, 데이터 이관·행 삭제·조용한 초기화, 인증서·서비스·로그인·방화벽·전역 설정 변경, 관리자/UAC 우회, 타 소유 프로세스 중지.
- 경제/길드/무기 저장, 캐릭터 삭제·이름 변경·새 직업, 프로토콜·PDL·생성 코드·Shared/ClientNet 공개 계약 변경, Content 화면·씬·Unity 에셋 수정.
- 기존 시험 준비 코드·기대값의 임의 변경, 새 검증 도구 체계, Linux SQL offline 호환성 및 선택 Close 가용성 등 다른 BACKLOG 후보 수리.
- 사용자 손이 필요한 실행 대리, 승인 전 제품·시험 코드/DB/실행, 실행 창 밖의 빌드·SQL·서버·봇·Unity, Core의 PR 병합·자동 병합.

### 관찰 가능한 완료조건

1. **고정 입력과 소유권:** 메인 범위 대조 승인, World의 Stored 목록→CharacterId 선택 계약, 실제 DB 공개 식별자·서버/봇 argv·cwd·환경·입력 hash·시간 제한·정상 종료 방법·소유자가 실행 계약에 고정돼 있다. 정책/권한과 충돌하면 READY로 포장하지 않는다.
2. **재기동 전 실제 진입:** 기존 개발 DB에서 지정된 기존 시험 계정 `w_2fbe2e9a`의 목록에 CharacterId `959c259f-5cad-4fd3-9840-5c1cd39fea79`·Mage가 실제 남아 있는지 확인한다. 목록에서 그 ID를 선택해 같은 ID·저장 직업의 load 성공을 패킷/서버 근거로 관측한다. 후보가 없거나 다르면 중단하며 신규 생성/seed/다른 ID 대체0이다. 사용자 A(`msg_85c64266bdb2`)로 Town·풀HP 직접 관측은 Core 합동에서 제외하고 Content의 같은ID 실제 Unity 입장·현재/최대HP 확인으로 이관한다. World의 ID 관측 계약에 없는 값을 추정으로 채우지 않는다.
3. **프로세스 교체:** 봇 연결 종료·저장 settlement 뒤 첫 서버가 정상 종료하고 PID와 시작 시각·종료/포트 해제 근거가 있다. 같은 고정 DB/profile·통합 입력으로 별도 서버 프로세스를 시작해 새 PID/시작 시각·Listening을 기록한다. 같은 프로세스의 재접속만으로 충족하지 않는다.
4. **재기동 후 불러오기:** 같은 계정으로 목록을 다시 받아 전후 동일 CharacterId·저장 직업을 대조하고 같은 ID를 골라 load→입장까지 성공한다. 생성 재수행이나 클라이언트 기본 직업 전송만으로 성공 처리하지 않는다. 이 단일 후보 실증을 다른 캐릭터 전체의 오염 부재나 앞 goal 조건5 전체의 충족으로 확대하지 않는다.
5. **근거와 보존:** list/select/acquire·checkpoint/종료·reload의 실제 수행 범위를 기존 Core API/SQL/World 관측에 연결한다. 지정 시험 캐릭터 밖 기존 데이터 불변, 신규 생성/새 DB/DDL/행 삭제0을 확인한다. 코드 변경 시 관련 기존 회귀의 전 실패를 분류하고, Skip·미실행·독립 확인 범위를 구분한다.
6. **독립 판정과 인계:** PR 직전 신규 Opus 한 명이 실제 diff·구현 보고·원시와 현재 DB/재기동 경로를 강 기준으로 독립 확인한다. 실제 진입 미실행은 판정 보류다. 사용자 화면/촬영, 동시 writer, Resume Verify·Cleanup, 기타 제외 후보의 미실행 한계를 명시해 메인·World·Content에 전달한다.

## 필요한 DB 쓰기와 실행 경계

| 종류 | 허용할 최소 내용 | 금지/관문 |
|---|---|---|
| 대상 | 기존 공개 profile의 `tcp:localhost,15433` / `Dawnholder_Char_8256bcde8c80` 하나. 실제 공개 파일/현재 release는 실행 준비 때 재대조 | 새 대상 자동 선택/생성0. 비밀·private receipt 내용 출력0 |
| 시험 입력 | 위 기존 계정/CharacterId·Mage를 Stored 목록에서 먼저 확인, 독립 검증도 별도 실행에서 같은 후보를 재검사 | 신규 account/Character/CharacterCreateOperation 생성·seed·다른 ID 대체·직업 변경·직접 SQL0 |
| 정상 저장 수명 | 그 CharacterId에 한정한 acquire/fence·checkpoint·정상 종료/재입장의 기존 `dh.CharacterAuthority`/`CharacterProgress`/`CharacterOperation` receipt 갱신 | 새 DDL/관리 작업0, 임의 실패 유도·강제 프로세스 종료0 |
| 종료 후 | 기존 시험행과 이번 operation receipt를 보존하고 식별자와 실제 쓰기 종류를 보고 | 행 DELETE·DB DROP·자동 Cleanup0 |

실제 연결·쓰기·SDK/build/runtime 호출 전 메인의 새 실행 창과 고정 계약이 필요하다. SDK `10.0.301` 및 복제본/빌드의 DLL 복사 부작용을 확인하고 원본 Unity DLL 변경을 피한다. 실행 직전 프로세스·7777 포트·해당 물리/commit 여유 관문을 실제로 확인한다. Unity와 다른 서버/WSL 소유를 추정하지 않는다. 기존 정책이 무인 기동을 막거나 UAC/수동 Enter가 필요하면 상태를 보존해 메인에 보고하고 아침까지 의존 실행을 보류한다.

앞 goal의 `joint-restart-prep-32/preparation.md` v3와 사용자 직접 실행 분리 기록은 기존 `99_Tools/Start-PersistenceServer.ps1`이 Import-Clixml→자식 stdin 및 Enter 정상 종료를 사용한다고 명시한다. 공개 profile/CA의 복사와 비밀 credential 본문/hash 열람을 구분하며, 새 A를 근거로 AI가 credential을 직접 읽거나 기존 사용자 실행 단계를 자동 대리하지 않는다. 메인 범위 승인으로 무인 소비는 권한 밖임이 확인됐다. 현재는 사용자 직접 두 기동·각 Enter를 전제로 한 정적 준비만 READY이며 실제 실행은 새 창/T를 기다린다.

## 설계 선택과 파트 계약

- 선택: World가 기존 Stored 봇을 최소 보완하고 Core가 저장 DB·서버 두 프로세스 및 결과 연결을 맡는다. 대안인 Core 새 봇은 구현 중복과 이중 wire 소유가 생겨 채택하지 않는다.
- 선택: 기본은 정상 종료 후 재기동이다. crash/kill·동시 writer·Resume 관리 경로까지 넣으면 장면 9 범위를 넘어 별도 실패·보호 수명 검증이 필요해 제외한다.
- World: Stored 봇·서버 세션/handler·통합 wire, 관측 파일/봇 argv와 선택 ID 근거. Core: 저장 API·개발 DB 경계·서버 host 수명·합동 원시 연결. Content: 캐릭터 선택/생성 화면 및 최종 시연 장면. 각 파트의 제품 쓰기는 자기 goal/계약으로 수행한다.
- World 회신 `msg_f473f4c61347`: `01_Phases/goals/2026-10-18-character-server/goal.md`, branch `feat/character-server-20261018`, 회신 `run:run_90d18490c825`. 기존 Stored 봇은 정적 READY이며 compile/실재기동0이다. 공개 profile/CA 공급·서버 launcher·정상 flush·실제 Linux PID/birth와 포트·DB 쓰기/보존은 Core 소유로 합의했다.
- 같은 World 근거 폴더 `approval-01/stored-bot-extension-01/lead-ready-handoff-v1.md`의 before/after 직접 DLL argv를 사용한다. 입력은 같은 account/CharacterId이며 `--mode Stored --phase before|after`, 각 단계5초/전체30초다. `sync-wsl.sh bot`은 자체 서버를 켜므로 쓰지 않고 기존 build 뒤 고정 DLL을 별도 서버에 붙인다. Windows wsl.exe wrapper PID만 다른 것은 재기동 증거가 아니다. `close-requested`도 저장완료 영수증이 아니다.
- World의 기존 보호63소스는 역사 입력이다. 메인 `msg_b88e88c29142`의 변경 순서에 따라 World 최신 main 선통합 인계 뒤 현재 SHA·미커밋 입력·public profile/CA hash와 정확 실행 명령을 실제 파일과 대조해 새로 고정한다. 합동 뒤 입력이 다시 바뀌면 재검증 필요 범위를 판정하며 앞 합동의 SHA를 새 HEAD 실증으로 바꾸지 않는다. World C1~C9는 이 Core 좁은 범위로 축소하지 않는다.

## PR 경계와 검증

- **Core PR 1:** 재기동·불러오기 합동 결과, 관련 실행 안내와 필요한 최소 Core 결함/회귀 수정만 포함한다. 제품 수정이 없으면 문서/검증 결과 PR로 제한한다. World 봇·캐릭터 서버와 Content 화면은 각 PR 소유다.
- **종료 기록 PR 1:** 제품/결과 PR 병합과 로컬 결과 기록 뒤 Gardener 점검·종료 기록·종료 점검·R-8. 각 PR 병합은 메인의 정확 head 사용자 승인 관문을 따른다.
- 등급 **강**: 영속 DB·서버 재기동 실제 I/O 경로이기 때문이다. 최근 경량 결정에 따라 구현자 한 명의 자체 준비/실행/수정 뒤 PR 직전 신규 독립 Opus 한 번으로 진행한다. 선행 시험/검증 세션을 임의 추가하지 않는다. 독립 세션·실제 진입·원시 대조와 기존 시험 변경의 사용자 결정 경계는 유지한다.
- 실행 창 **추정**: 준비 뒤 1차 합동 창 45~60분(고정 build·관문 약15분, 첫 서버/봇·정상 종료/새 서버·봇 약25~30분, 종료/원시 정리 약10~15분), 신규 Opus 독립 확인은 별도 30~45분을 예상한다. 실제 argv와 World 준비 뒤 단계별 timeout·관문을 고정해 READY로 새 창을 요청한다. 이 추정은 자원 예약·확정 시작 시각이 아니다.
- 중간 점검: 범위 승인, World 입력 고정, 무인 기동·실행 READY, 실제 결과/범위 안 결함, 독립 판정/PR 직전. 같은 산출물 수정 3회 초과는 메인 체크포인트이며 같은 계약/결함 확정 실패 집계와 구분한다.

## 진척 단계

- [x] 범위 초안
- [x] 범위 승인
- [x] 합동 입력 고정
- [x] 실행 준비
- [>] 재기동 확인 — 세션 마감, 다음 세션 첫 사용자 창 Core60분·새 정확 T 대기, 실제 실행0
- [ ] 독립 검증
- [ ] 결과 PR 병합
- [ ] 종료 기록 병합
- [ ] 종료 점검

## 현재 보고와 남은 위험

- **지금 실제로 보여 줄 것:** 승인 A 기준 최종 `preparation-01/worker/user-card.md`·`execution-plan.md`와 `lead-ready-review.md`. 직접 두 기동/각 Enter·즉시 보고·정상 flush·실제 Linux PID/birth/7777를 포함한다. 정적21항목 불일치0, 작업자 World489/Core9/public4 대조 일치, 리드 파일39개 해시·길이 일치다. 실제 장면9·DB/재기동 성공 근거는 아직 없다.
- **가장 빠른 다음 동작과 예상 KST:** 사용자 지시에 따라 세션을 마감하고 다음 세션의 첫 사용자 창으로 Core60분 합동을 이어간다. 새 메인의 사용자 준비/현재 자원 확인과 새 정확 T 뒤 신규 실행자·최종계약 발행→직전 관문/build 약15~20분→사용자 직접 두 기동·각 Enter 약25~30분→정리 순서다. 사용자 능동2~4분을 포함해 모두 미측정 추정이며 확정 KST는 없다. 준비자는 정산·종료했고 열린 Task/Dispatch·작업자pane0이다. 기동/정산/입력 인계는 `preparation-01/launch-ledger.md`, 계약초안·현재 마감근거는 `joint-runtime-01/`에 있다.
- **막는 것 하나:** 새 세션에서의 사용자 직접 실행 창/정확 T다. 오늘 실행/발행0이다. OBS-S9-01은 사용자 A로 Content 이관했으며 접수·조건3·4 연결을 대조했다. 실행 담당자는 실제 Stored 로그의 Mage를 명시 대조하고 합동 뒤 같은ID 원시를 Content에 추가 전달한다. World 자체838을 Core runtime/DB 실증으로 대신하지 않는다.
- 미실행: 모든 신규 build/test/SQL/서버/봇/Unity. 기존 goal의 성공 로그는 이번 실행 증거가 아니다. 완전한 화면/촬영 성공은 Content·메인의 별도 확인이다.
