# 실제 DB 연결·로딩 게이트·재접속 직업 복원

상태: 시연 경로의 독립 검증을 마쳤고 메인 msg_6794d5227945가 #3 NUL 입력 변경과 운영 편차를 비차단으로 수용했다. 동결 뒤 항목은 미실행 상태로 남는다. 들여쓰기 한정 교정 뒤 Changed 로컬 검사 위반0과 기존 시험 동일을 확인했다. PR242 갱신·새 head CI 진행 단계다. 사용자 정확 head 승인과 메인 병합은 미완료다.

## 진척 단계

- [x] 목표·경계 승인
- [x] 공통 계약 확정
- [x] 선행 시험 실행
- [x] 저장·입장 구현
- [x] 독립 검증(메인 수용·동결 뒤 범위 별도)
- [x] 시연 실제 경로 관측(미실행 항목은 동결 뒤 목록)
- [x] PR 생성(draft)
- [ ] 통합·병합
## 적용 중인 사용자 결정
### PR242 뒤 노트북 SQL Express 설치 도구 — 기록만

메인 `msg_9fa3df979283`가 전달한 사용자 원문(메인 pane Enter 제출):

> 대시보드 결정 응답: 1) 노트북 이전 - 시연용 DB를 SQL Express 직접 설치로 할지 → A Express 도구 만든다

메인 전달이며 이 pane의 직접 사용자 입력으로 격상하지 않는다. 현재 작업 순서를 유지하며 새 goal 범위가 전달되기 전 착수·계약 발행은 하지 않는다.

메인 msg_fcb2a973fede가 전달한 사용자 원문(메인 pane Enter 제출, 2026-10-14T14:44:08Z/23:44 KST): 「그리고 영속성 부분은 너가 더 전문가니까 Goal 초안 작성하고 진행까지 계속 진행해줘, Core쪽 진도를 좀 빠르게 뺴보자」. 메인 전달이며 이 pane의 직접 사용자 입력으로 격상하지 않는다. 시각은 정정 msg_d8a5ccb79390을 적용했다.
메인 초안 C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-14/core-next-goal-draft.md SHA055A04FF697EC14A0DF17B4D1656777D8F54717BC04ACE5135CAC03A18DFBCDE와 리드 초안 lead-next-goal-draft.md SHAF9A38BBEC62A4273F9FF5E9F3812C813396F77BAE92A6FF36B36EAAC2B586D1F를 통합했다. 원문은 .backups/verification/2026-10-14-persistence-runtime-core/lead-next-goal-approval-source.json에 보존한다.
10-28 기능 동결까지 시연 우선 msg_beda86f6bc2f, 리드/새 Sol배정 작업자는 Sol xhigh인 msg_18c90b33c9a1을 적용한다. 비동작지적은 동결뒤 정리, 운영미준수는 공개·제품비차단, 독립재검증1회뒤차단이면메인판단이다. 동작결함/위장시험·측정/보호경계·구현검증분리/실제실행·정확head 사용자병합승인은 유지한다.

### Core 리드 Astra medium 교체 시범(2026-10-15)

메인 msg_71c662398b5a(2026-10-15T06:34:44Z, from term_e2961048-266e-4503-b52b-dfb986d8c9b5)가 전달한 사용자 원문 두 줄이다. 메인 pane의 제출 결정 전달이며 이 pane의 직접 사용자 입력으로 격상하지 않는다.

> 그러면 Core쪽에 Astra Medium 리드를 하나 테스트로 사용해보면서 비교해볼까? 1. 리드가 모델별로 지속적으로 맞는지 2. 토큰 가성비 차이가 얼마나 나는지
>
> 대시보드 결정 응답: 1) Core 리드 Astra medium 시범을 지금 리드 교체로 할지, 두 번째 리드 추가로 할지 → A 교체

- 리드만 `gpt-6-astra` / `medium`으로 교체하고 태그는 `[Core 리드 Astra]`다. 두 번째 리드를 추가하지 않는다. 구현자·검증자 배정은 바꾸지 않는다. 새 리드는 같은 core-active 탭·같은 Run `run_b36cc92a4cf4`를 `run-use`로 잇되 현재 runtime/handle/incarnation과 화면·최초 명령의 모델을 새로 확인한다. 실제 backend 모델 미확인은 `unknown`으로 기록한다. 문서 기록만으로 현재 Sol 런타임이 바뀐 것은 아니다.
- 시범은 Core 실제 DB 단계 끝 또는 2026-10-16 저녁 중 먼저까지다. 보호 자원·권한·push·병합 규칙에 닿는 실수1회 또는 메인이 잡은 계약 오류2회면 Sol xhigh로 원복한다. 비교 목적은 리드 적합성의 지속 여부와 토큰 가성비다. 기존 수치와 다른 작업량을 같은 조건의 성능 측정으로 표현하지 않는다.
- R-8의 문맥 비우기 목적 교체와 구분한 사용자 결정에 따른 모델 시범이다. 기존 Sol은 새 작업 없이 정산·인계 기록을 commit/normal push하고 status 한 통 후 턴을 끝낸다. 기존 pane 종료·새 리드 기동은 메인이 맡는다. 제품·자원·Git 권한 확대와 새 목표 착수는 없다.

### DB 단계 자원·경계 승인 — 실행은 다음 재개 때(2026-10-15)

메인 `msg_2c59021a4f48`(2026-10-15T07:07:39Z, from `term_e2961048-266e-4503-b52b-dfb986d8c9b5`)가 전달한 메인 pane 제출 사용자 결정이다. 이 pane의 직접 사용자 입력으로 격상하지 않는다. 기록 시각은 `Thu Oct 15 07:08:00 UTC 2026` (`date.exe -u` 출력)이다.

> 대시보드 결정 응답: 1) Core 재접속 복원의 실제 DB 단계: 새 시험용 SQL 컨테이너·DB·로그인·비밀·TLS 자원 이름과 경계를 승인할지 → A 이 이름·경계로 승인 , 대신 다음 작업재개때 진행

- 승인 범위: container `dawnholder-sqlserver-runtime-entry-r1`, volume `dawnholder-sqlserver-runtime-entry-r1-data`, DB `Dawnholder_Test_RuntimeEntry_R1`, login `dh_runtime_entry_r1`(허용5RPC), `127.0.0.1:14335`, privateRoot `LOCALAPPDATA/Dawnholder/runtime-entry-r1`, 새 CurrentUser DPAPI, 실행 전용 TLS 키·인증서, 자식 프로세스 한정 CA 신뢰, 요청한 고정 image·pull never·메모리 한도·restart no다. 아래 준비안과 요청 `msg_4174d0256746`의 이름·경계 안에서만 새 자원을 만든다.
- **실행은 다음 작업 재개 때다.** 오늘 작업자·검증자·DB·컨테이너·WSL 실행을 열지 않는다. 다음 재개는 새 작업자 실행 카드 고정 → 새 독립 정적 검증 → 실제 실행 순서다. 기존 보호 자원은 건드리지 않으며 삭제는 별도 승인이다. 자원 승인으로 미작성 실행 카드나 실제 성공을 승인·확인한 것으로 표현하지 않는다.
- 메인 R-2는 마감 head `369482ccc92a9ca312610163b73993b41c3d9af4`, 두 checkout clean, rollout 존재, 재개 블록을 대조했다. 이번 변경은 이 결정을 기록하는 문서 작업뿐이다.

### sa bootstrap 한정 예외와 TLS 연결 호스트(2026-10-15)

메인 `msg_f4eac9748454`(2026-10-15T11:37:15Z)가 전달한 메인 pane 제출 사용자 원문이다. 이 pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) Core DB 시험 컨테이너 - 관리자 비밀번호 전달 방식 예외 → A 컨테이너 안 한정 예외 허용

- 예외는 새 `dawnholder-sqlserver-runtime-entry-r1`의 sa 비밀번호 한 개다. 파일로 마운트하고 entrypoint 시작 스크립트가 읽어 컨테이너 안 sqlservr 프로세스 환경에만 넘긴다. 호스트 argv·env·로그·docker inspect 출력과 시작 스크립트 argv의 비밀값은 0이다. bootstrap 뒤 sa를 비활성화하고 시험 뒤 승인된 새 container·volume·secret 파일만 삭제한다. 기존 보호 자원과 G4 삭제 승인으로 확대하지 않는다.
- 고정 image·pull never·loopback14335·메모리·restart no는 유지한다. runtime 로그인 비밀은 CurrentUser DPAPI/env0, TLS는 Mandatory/TrustServerCertificate=false·전역 신뢰 변경0이다. 방식의 고정 image 실제 동작은 미확인이며 새 카드·독립 정적 검증·B04 해소 뒤 실제 실행한다.
- 메인 판단 `msg_459a8dbf0825`(11:39:35Z): TLS SQL 연결을 Windows System.Data.SqlClient 밖의 Linux 자식 CA 신뢰 호스트로 옮기는 설계를 이번 범위에 허용한다. chain/name 검증을 pin으로 대체하지 않는다. 도구·카드는 리드 판단, 새 의존성이나 게임 서버 제품 코드 변경은 구현 전 메인 판단이 필요하다. 사용자 결정과 구분한다.
- 결정 원시는 B/tls-readonly-research-1/inbox-ack.json 및 B/runtime-entry-secure-bootstrap-1/inbox-check-5.json이다.

### 계획 효율 점검 채택(2026-10-15)

메인 `msg_b5ffde55ffd7`(2026-10-15T13:46:03Z)가 전달한 사용자 원문(메인 pane 제출)이다. 이 pane의 직접 입력으로 격상하지 않는다.

> 너무 작업이 오래걸리면 Fable한테 한번 현재 Core Goal 계획표에서 불필요한 부분이 있는지 체크해달라고 이야기 하고, 플랜 수정하고 진입해줘, 해야할 부분이랑 하지 않을 부분에서 비효율적인게 있나본데

아래 채택은 사용자가 맡긴 계획 수정에 대한 메인 판단이며 새 사용자 결정이 아니다. Fable 읽기 전용 원문은 `C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-15/fable-core-plan-review.md`, SHA256 `8ADD96484BBFB44D78DE99BF30A184B49C23427A3208D9F7E6A0788D03C20A42`다. R-7/확정 실패 Advisor나 독립 검증 대체가 아니다.

- 실행 계약 하나에 빌드·publish → Prepare → Start → Bootstrap → Runtime을 일괄 승인한다. 빌드·동결 별도 승인 라운드는 없고 자동 관문 실패·계획 밖만 공식 ask다.
- publish는 LinuxSqlHost.dll SHA256·dotnet 버전을 기록한다. 전체 inventory hash·realpath/link 관문은 동결 뒤 후보로 보낸다.
- 이번 컨테이너는 `--cap-drop=ALL`·`--security-opt=no-new-privileges`를 제외한다. `--user 10001:0`은 고정 image의 기본 사용자와 같음을 해당 image inspect로 확인할 때만 유지한다.
- TLS 음성은 서버 인증서를 유지하고 무관한 클라이언트 CA 묶음으로 wrong CA 거부 1회만 한다. hostname·만료 음성은 후속이다. catalog004 literal 교정·해당 시험은 이번 독립 검증자가 수행한다.
- 작업자는 Runtime까지 수행하고 새 컨테이너를 켠 채 종료한다. 신규 Opus 한 세션에서 실제 검증·정상 서버·봇·Play·정확 반환·감사·새 자원 삭제를 수행한다. 5RPC는 봇 실제 호출로 확인하며 별도 기능 시험을 중복하지 않는다.
- Audit는 사용자 결정 범위인 호스트 argv·env·로그·docker inspect·컨테이너 stdout·시작 스크립트 argv의 sa 값 검색 1회다. volume SQL 로그 tar 메모리 검사는 제외한다.
- 재검토2가 차단이면 세 번째 정적 루프 없이 메인 판단을 받는다. 좁은 수정은 실행 계약에 포함하고 실제 검증 계약의 독립 확인 대상으로 적는다.
- Core 전용 Unity·WSL·SQL 창에서 타 파트 통지·대기·양보를 생략한다. fresh 물리6GiB/commitfree4GiB/C10GiB·단일Editor·사용자 프로세스 비접촉은 유지한다. heartbeat 초과는 구간·이유 한 줄과 기존 집계만 기록한다.
- Trust=true 관리자 bootstrap 대비안은 채택하지 않았다. 실행 전제가 막히면 임의 수정 루프 대신 메인에 보고한다. 승인 자원·비밀·보호 경계·독립 역할·정확 head 사용자 병합 승인 규칙은 유지한다.
### Docker 마지막 시도와 중단 조건(2026-10-15)

메인 `msg_7f807adc4a54`(2026-10-15T14:30:35Z)가 전달한 사용자 원문(메인 pane Enter 제출)이다. 이 pane 직접 입력으로 격상하지 않는다.

> 만약에 Docker로 나누는걸 실패하면 그냥 Docker로 컨테이너세팅하지 말고 진행하는거로 바꾸자, 시간 너무 많이 잡아먹는다

- 메인 적용 판단: v2의 `logging driver invalid size -1`은 create 인자 문제로 보이므로 신규 Astra xhigh v3 계약에서 Docker 마지막 1회만 수행한다. 현재 작업자는 실패 원시를 보존하고 정산·종료한다.
- v3는 로그 옵션 최소 수정 → 실패한 정확 새 container 제거 → Create/Start → Bootstrap → Runtime 일괄 승인이다. volume·privateRoot는 계약이 허용하면 재사용하고 아니면 같은 승인 이름으로 다시 만든다. 기존 보호 자원·Windows SQL 접근0이다.
- Docker CLI stderr는 sa 값을 치환한 사본과 치환 사실을 근거에 남긴다. 비밀 원문을 출력하지 않으며 길이/hash만으로 원인 기록을 대신하지 않는다.
- v3에서 create·start·logging·mount·entrypoint·sa bootstrap 등 Docker 전제가 다시 실패하면 추가 수정 없이 보존·정산하고 **Docker 방식 중단**으로 메인에 보고한다. Docker 없는 방식은 메인이 사용자와 정해 새 계약으로 전달한다. 그 전 대안 설계·Windows SQL 조회는 하지 않는다.
### Docker 실패 시 Windows SQL Express 조건부 허용(2026-10-15)

메인 `msg_0ed06132bca4`(2026-10-15T14:31:34Z)가 전달한 사용자 원문(메인 pane Enter 제출)이다. 이 pane 직접 입력으로 격상하지 않는다.

> 대시보드 결정 응답: 1) Core DB - Docker 마지막 시도 실패 시 Windows SQL Express 사용 허용 → A 미리 허용

- v3가 Docker 전제로 실패한 경우에만 로컬 SQL Express `MSSQL$SQLEXPRESS` / `D:\MSSQL2025`의 새 `Dawnholder_Test_RuntimeEntry_R1` DB·`dh_runtime_entry_r1` 로그인으로 전환한다. 관리자 인증은 사용자 Windows 계정이며 sa 비밀번호는 쓰지 않는다.
- 허용 범위는 시험 서버 인증서의 인스턴스 설치·SQL 서비스 재시작을 포함한다. 게임 서버의 TrustServerCertificate=false와 chain/name 검증, WSL 자식 한정 CA 전달을 유지하며 전역 CA 신뢰 저장소 추가0이다.
- 메인 보호 조건: 기존 DB·로그인·데이터 읽기/쓰기 및 전체 목록 조회0. 변경 전 원래 인증서 설정을 기록하고 시험 뒤 되돌림·새 시험 DB/로그인 삭제를 한다. 되돌림 실패 또는 과거 master 파일 접근 거부 시작 실패 재발은 임의 수정 없이 보존·보고한다.
- `C:\myVHDX.vhdx`의 D: 마운트는 사용자가 한다. v3 실패 보존·정산과 Docker 방식 중단 보고 뒤 메인이 마운트를 요청하며, 확인을 받은 후에만 새 실행 계약을 발행한다. 지금은 v3 우선, SQL Express 조회/서비스 조작0이며 대안의 정적 계약 초안만 허용된다. v3 성공 시 이 허용은 사용하지 않는다.
## 재개 지점

- 작업 위치 `core-parallel`, branch `feat/persistence-runtime-entry-20261015`, PR242. 기준 HEAD `5f1e4017d0f35656d1a03341430bc43565d95506`. 승인 범위 변경의 커밋·normal push·PR 갱신·새 head CI 확인을 진행한다. Codex 병합 금지다.
- 최신 메인 결정 `msg_6794d5227945`는 아래 「최종 독립 판정 수용」에 반영했다. 최종 검증자 `task_8c779ad6257e`/`ctx_07c1b9579f55`는 정산·release·동일 incarnation/idle 확인 뒤 종료했다. 서식 후속 작업자도 정산·종료했으며 현재 작업자·검증자 없음.
- 시연 필수 경로 완료, 동결 뒤 미실행은 별도 유지. 로컬 Changed 최초38건/7파일 실패는 메인에 원인 보고(`msg_9772389312d8`) 후 신규 Astra가 줄앞 공백만 교정했고 같은 검사 위반0과 기존3시험을 확인했다. 최초 실패 원시는 보존한다. 새 head CI 결과 확인 후 메인에 정확 head와 근거를 전달한다.
- `B=P2/.backups/verification/2026-10-14-persistence-runtime-entry`, `R=B/runtime-entry-launcher-final-verification-1`, `V=B/runtime-entry-linux-sql-live-verification-1`. 이전 재개 블록은 `R/goal-before-finalization.md`에 보존한다.
- 승인된 신규 SQL container/volume/private 자원은 정리됐고 서버·Unity·7777 잔존0이다. 옛 보호 자원 3쌍은 유지했다. 추가 DB 실행·삭제·다음 노트북 SQL Express goal은 착수하지 않는다.
## 메인 결정(사용자 위임)
1. 저장 대상은 서버 설정의 정규화된 계정 이름 하나다. 그 UUIDv5가 PersistenceOptions.AccountId와 같아야 서버가 시작한다. 해당 계정만 DB 경로, 다른 계정은 기존 메뉴직업/비저장 플레이다. 저장 대상 DB 실패는 입장 거부이며 메모리fallback은 없다. 판정은 Persistence 안에 두며 Accounts/AccountRegistry를 수정하지 않는다. World 새기술계약은 필요 없고 계정별저장은 동결뒤 BACKLOG 후보다.
2. PDL·Unity writer는 Core다. 새 S_CharacterLoadResult는 ID48부터 사용하며 World43~47/v20과 기존ID를 보존한다. 프로토콜판은 늦게병합되는쪽이rebase때정한다. 같은파일단일writer·양쪽소비자 검증을 유지한다. Unity scene/prefab재저장필요는 먼저메인에올린다.
3. A/B PR 병합 전이면 그 branch/head 위에서 시작할 수 있고 병합뒤 main으로rebase한다. 현재문서만core-parallel에두며 실제착수에서 후속branch를 확정한다.
4. Microsoft.Data.SqlClient6.1.7은 merged technical-spec대로 허용하며 병합승인요청에 새의존성으로 명시한다. 다른판/패키지는메인판단이다.
5. 새격리DB/runtime login/비밀 위치의 정확이름은 실제실행전에메인에올린다. G2같은DPAPI범위이면메인위임승인, 다른방식은아침사용자결정이다. G2와동시DB실행0, 옛보호자원읽기쓰기0.
6. Unity실행은 Art→World→Content 뒤 Core차례다. WSL은 오늘밤메인판단의fresh물리6GiB·WSL비중복/Core우선을 적용한다.

## 원천 대조
- 실제 migration은 99_Tools/database/migrations/002_persistence_metadata.sql이다. dh.CharacterAuthority SlotId PK/CHECK1을 확인했다.
- Accounts/AccountIdResolver.cs의UUIDv5와 AccountRegistry.cs의AlreadyOnline, modules/procedures 아래runtime5파일, ClassLoadout를읽는 LocalPlayerSpawner를대조했다. 메인원천과의범위불일치지적은없다.
- 실제구현/설치/DB/화면 검증은 이 대조에 포함되지 않는다. 정확파일소유·packet공통필드·options/secret수명 계약은리드가착수전에고정한다.
## 목적

10-13 승인 시연 시나리오의 장면 9 「접속을 끊었다 다시 들어와도 직업이 유지된다」를 실제 SQL Server와 실제 서버·클라이언트로 돌게 한다. implementation-plan의 D1b 잔여(C# repository), D3(권위 입장), D4의 정상 경로(release 뒤 재접속 복원)를 한 goal로 묶는다.

## 근거(읽을 원천)

- `01_Phases/goals/2026-09-29-persistence-design/implementation-plan.md` 「단계·의존성」 D1b·D3·D4, 「파일 소유 후보」 D3·D4 줄, 「반증 TestCode·완료조건」의 admission·권위 class/Unity·실제 policy/durability 줄, 「실행 전제·부작용」 전체.
- `01_Phases/goals/2026-10-01-persistence-technical-design/technical-spec.md` 2절(연결·비밀), 3.3절(payload v1), 6절(결과와 인계), 7절(timeout).
- core-parallel goal `2026-10-14-persistence-runtime-core`의 ICharacterRepository·CharacterPersistenceOwner API(A/B PR).
- core-active SQL goal의 G2 결과(정식 catalog 판정)와 컨테이너·비밀 경계.
- `01_Phases/goals/2026-10-11-multi-account-login/goal.md` 85-87행(계정별 slot 확장과 저장 class 사용을 Core 별도 goal로 미룬 기록), 112·154·159행(AccountNameRule·UUID v5).

## 메인이 확인한 사실

1. DB 저장 권위는 하나뿐이다. migration 002의 `dh.CharacterAuthority`는 `SlotId`가 PK이고 `CHECK([SlotId]=1)`이다. technical-spec 2절의 `PersistenceOptions`도 고정 AccountId/CharacterId를 받고 시작 gate에서 DB와 대조한다.
2. 로그인은 계정 이름마다 `AccountIdResolver`(UUID v5)로 AccountId를 만든다. 같은 계정의 중복 접속은 `AccountRegistry`가 새 연결을 `AlreadyOnline`으로 거부한다.
3. C# 쪽 SqlClient 사용처는 없다. runtime 프로시저 5개(`dh.ReadAdmission`·`dh.AcquireAndLoad`·`dh.WriteSafeCheckpoint`·`dh.ReleaseRuntime`·`dh.ResolveRuntimeOperation`)와 `dh_runtime` 권한은 main에 이미 있다.
4. 입장 때 자기 직업을 알려 주는 패킷이 없다. `S_EnterMap`은 entityId·좌표뿐이고 Unity는 `ClassLoadout`(메뉴 선택·PlayerPrefs)로 직업을 정한다. 이 값을 직접 읽는 곳이 여럿이다(LocalPlayerSpawner·LocalPlayerInput·HudController·SkillHudController·PlayerHpHandler·EnemyAttackHandler·LocalPlayerMovement).
5. 패킷: main은 v19, 마지막 ID 42. World 길드 branch가 커밋 전 PDL에서 ID 43~47과 v20을 쓰고 있다. Content branch는 PDL 변경이 없다.

## 채택된 동작 기준

- **저장 대상 계정은 서버 설정의 계정 하나다.** 승인된 설계(slot 1 하나)를 넓히지 않는다. 서버 설정에 정규화된 계정 이름 하나를 두고, 그 이름의 UUID v5가 `PersistenceOptions.AccountId`와 같아야 시작한다. 그 계정만 DB 경로를 탄다. 다른 계정은 지금처럼 메뉴 직업으로 플레이하고 저장하지 않는다. 이것은 DB 실패를 숨기는 fallback이 아니다. 저장 대상 계정의 DB 실패는 입장 거부로 드러낸다. 계정별 저장(새 migration·프로시저 변경·catalog 재판정)은 동결 뒤 후보로 BACKLOG에 남긴다.
- 저장된 직업이 메뉴 선택보다 우선한다(technical-spec 6절). 처음 들어온 계정은 메뉴 직업으로 생성된다.
- 복원 위치·체력은 Town 안전 지점·최대 체력이다. 퀘스트·보스 해금·인벤토리·길드 금고는 복원하지 않는다.
- 새 의존성 `Microsoft.Data.SqlClient` 6.1.7은 merged technical-spec이 고른 판이다. 그대로 쓰고, 병합 승인 요청에 「새 의존성」으로 명시한다. 다른 판이나 다른 패키지가 필요하면 메인에 올린다.

## 만들 것

1. **SQL repository**: `ICharacterRepository`의 SqlClient 구현과 connection factory. typed RPC로 runtime 프로시저 5개를 부른다. 연결 문자열 옵션·timeout은 technical-spec 2·7절 값 그대로다. 자동 재시도는 없다. 결과 해석은 A/B의 결과 타입과 PersistenceResultCode에 맞춘다.
2. **비밀 주입과 기동**: 승인된 로컬 launcher가 DPAPI 자격증명을 읽어 서버 프로세스에 stdin이나 익명 pipe로 넘긴다(technical-spec 2절). CLI 인수·환경 변수·로그·저장소에 비밀을 두지 않는다. 서버는 WSL2에서 돈다. 전달 방식의 정확한 형태는 리드가 기존 도구를 보고 고정한다.
3. **권위 입장(서버)**: CharacterSelect 뒤 Loading 상태로 두고, 저장 대상 계정이면 owner에 acquire를 접수한다. 결과가 오기 전 이동·공격·스킬·포털·파티·치트 입력을 거부한다. 결과로 직업·체력을 정하고 기존 EnterGameWorld 순서로 들어간다. 게임 틱은 DB 완료를 기다리지 않는다.
4. **새 패킷 `S_CharacterLoadResult`**(가칭): 결과 상태와 권위 직업을 보낸다. 기존 ID를 재사용하지 않는다. 번호는 World 리드와 지금 조율해 예약한다(메인 제안: 48, 판 번호는 병합 순서에 맞춰 rebase 때 확정). 저장하지 않는 계정에도 같은 패킷을 보내 클라이언트 경로를 하나로 둔다.
5. **종료 저장**: 연결 종료·서버 정상 종료에서 안전 checkpoint와 release를 owner로 넘긴다. 바뀐 게 없으면 NoWriteNeeded와 조건부 release다.
6. **Unity**(Core writer가 쓴다. Content는 지금 goal을 끝내면 멈추므로 배정하지 않는다): 새 결과 handler가 권위 직업을 받은 뒤에만 로컬 플레이어를 spawn한다. 위 4번 사실의 직접 읽기 지점이 서버 값을 쓰게 한다. 결과 전 입력·예측은 없다. 로딩 안내 UI 추가는 기존 화면 문구 한 줄 수준으로 제한하고, scene·prefab 재저장이 필요하면 먼저 메인에 올린다.
7. **봇 시나리오**: 저장 대상 계정으로 Mage 생성 → 종료 → Knight를 골라 재접속 → 서버가 Mage를 돌려준다. 같은 process 재접속과 process 재시작 뒤 재접속 둘 다.

## 건드릴 곳(후보, 정확 목록은 리드가 고정)

- `02_Server/GameServer/Persistence/` 아래 SQL 구현 새 파일과 저장 대상 판정(로그인 결과 AccountId를 options와 대조), `Sessions/GameSession.cs`, `Handlers/Session/CharacterSelectHandler.cs`, `Hosting/ServerHost`의 좁은 접점, `GameServer.csproj`(패키지), 서버 기동 설정과 launcher. 로그인 쪽 `Accounts/` 파일과 `AccountRegistry` 동작은 바꾸지 않는다.
- `99_Tools/PacketGenerator/PDL.xml`, `98_Shared/Protocol/ProtocolVersion.cs`, 생성 코드와 등록(단일 writer).
- `03_Client/Assets/Scripts/Network/`의 새 handler+meta, `Bootstrap/ClassLoadout.cs`, `LocalPlayerSpawner.cs`와 위 직접 읽기 지점.
- `99_Tools/headless-bot/Scenarios/Persistence/`, 서버·Unity 시험.
- 문서: goal, FEATURE_MAP 35행(저장·복원 연동은 후속), `domains/server.md` 17행, protocol 문서.

## 하지 않을 것

- 계정별 저장 slot, migration·프로시저·catalog 변경(core-active SQL goal 영역).
- `ICharacterRecoveryRepository`의 SQL 구현과 관리 복구 도구. 비정상 종료 뒤 Unknown·RecoveryRequired는 입장 거부와 안내로만 드러낸다.
- 보존 DB와 옛 실패 자원 접근, Docker 설정 변경, sudo, 전역 설정 변경.
- 인벤토리·길드·퀘스트·위치 저장, 서버 화면 연출 변경.

## 실행 자원(메인 승인 범위)

- DB는 **새 격리 database**를 쓴다. SQL goal의 판정 컨테이너 안에 둘지, 같은 이름 규칙의 새 컨테이너를 둘지는 G2 뒤 그 컨테이너의 수명 계획을 보고 리드가 정한다. 정확한 이름을 메인에 올리면 메인이 위임으로 승인한다. 보존 DB·옛 컨테이너·옛 volume은 읽지도 쓰지도 않는다.
- runtime login(`dh_runtime` 역할)과 비밀은 SQL goal G2에서 승인된 DPAPI 방식과 같은 범위면 메인이 위임으로 승인한다. 다른 방식이면 아침 사용자 결정으로 미룬다.
- G2 실행은 완료됐다. 현재는 메인 msg_b5ffde55ffd7의 Core 전용 창이며 타 파트 통지·대기·양보를 생략한다. fresh 자원·단일Editor·사용자 프로세스 비접촉 관문은 유지한다.

## 검증·관찰 가능한 완료조건

- 강 등급, 정본 TDD 순서. 시연 우선 모드를 적용한다(비동작 지적은 동결 뒤 정리, 같은 산출물 재검증 1회 상한).
- 실제 SQL로 관측: 처음 들어온 저장 대상 계정의 원자 생성, 종료 뒤 release, 재접속 때 저장 직업 load. 메뉴 직업이 반대여도 저장 직업이 이긴다.
- 실제 서버로 관측: Loading 동안 입력 거부와 결과 뒤 정상 입력, DB 지연 중 틱 비차단, 저장 대상 계정의 DB 실패는 입장 거부, 다른 계정은 지금처럼 동작.
- 끊은 직후 release가 끝나기 전 재접속하는 경우의 결과를 관측하고 goal에 적는다. 거부라면 사용자가 볼 안내가 있어야 한다.
- 봇 시나리오 두 경우(같은 process, process 재시작) 통과.
- Unity 실제 실행으로 장면 9를 확인한다. 같은 계정 재접속 때 화면 직업·스킬 HUD가 저장 직업이다.
- 기존 로그인·직업 선택·입장·파티·퀘스트 회귀 통과, 패킷 기존 ID 불변, 공유 DLL 서버·Unity 양쪽 검증.

## PR 경계

- PR 하나로 간다. writer는 둘이다. 하나는 SQL repository·기동·비밀 주입, 다른 하나는 패킷·서버 입장·Unity·봇이다. 서로 다른 파일만 쓴다. 공통 계약(결과 상태·패킷 필드)은 리드가 먼저 문서로 고정한다. diff가 커져 한 번에 읽기 어렵다고 리드가 판단하면 SQL repository를 앞 PR로 떼어도 된다. 그때는 메인에 한 줄로 알린다.
- A/B PR이 아직 병합 전이면 그 branch 위에서 시작하고, A/B 병합 뒤 main으로 rebase한다.
- 독립 강 검증 한 번 뒤 정확 head로 사용자 병합 승인을 받는다. World 길드 PR과 패킷 판 번호가 겹치면 병합 순서는 메인이 정하고 늦게 병합되는 쪽이 rebase한다.

## 착수 순서

1. 현재 정적 재검토2 결과를 정산하고 이 계획을 goal에 commit·normal push한다. 통과면 메인 추가 회신 없이 새 실행 계약을 발행한다. 차단이면 목록을 메인에 보고하고 좁은 수정 포함 여부를 따른다.
2. 신규 작업자 한 계약에서 채택된 최소 도구 수정 → 빌드·publish → Prepare → Start → Bootstrap → Runtime을 수행한다. 단계별 승인을 반복하지 않으며 fresh 입력·자원·소유 관문은 스크립트가 확인한다. 관문 실패·계획 밖은 공식 ask, E1~E5 전제 실패는 메인에 보고하고 임의 수정 루프0. Runtime 뒤 컨테이너를 켠 채 정확 인계하고 작업자를 종료한다.
3. 신규 Opus 독립 검증 계약 하나·한 실행 창에서 sa disabled → 직접 DML 거부1쿼리 → wrong CA1회 → DPAPI launcher → 서버 → 봇 두 경우(같은 process/서버 재시작)와 release 미완료 재접속1회 → Unity Play 장면9·HUD → Stop → Audit → Remove → catalog004 literal 교정·해당 시험을 수행한다. 봇의 실제 5RPC와 원래 완료조건(Loading·tick·실패 nofallback 포함)을 연결하고 미실행을 숨기지 않는다.
4. 보고는 계획 반영 commit, 실행 계약 발행, Runtime 결과, 독립 판정에 각각 한 통이다. 최종 판정·새 head CI 뒤 메인에 병합 승인 요청 자료를 전달하며 리드는 병합하지 않는다.
## 동결 뒤 정리
- 메인 msg_b5ffde55ffd7 채택: publish 전체 inventory hash·realpath/link 관문(E8), cap-drop/no-new-privileges 강화 옵션 재도입, TLS hostname·만료 음성은 동결 뒤 후보로 남긴다. volume SQL 로그 tar 감사와 별도 중복5RPC 시험은 이번 계획에서 제외한다. 정적 비차단 #4~#8/D1~D9는 실제 실행에서 동작 결함으로 드러나지 않는 한 일괄 정리하지 않는다.
- 계정별 영속 slot/스키마 확대는 이번범위밖후보다. 실제착수 때 BACKLOG의 기존후보와대조해중복없이기록한다.
- 메인 msg_a1a519ca00b8 지시에 따라 U 검증자의 O-2(미ack 재독), O-4(거짓 후손), O-C(반환 관측)를 harness·우편함 후속 후보로 남긴다. 원천은 U/verdict.md 운영 공개·설계 관찰이다. 공용 BACKLOG 안정적 ID 대조·연결은 다음 기록 정리 때 수행하며 이번 마감에서 공용 후보를 중복 생성하지 않았다.

## 문서 준비 기록
- 2026-10-14: 메인 승인 msg_fcb2a973fede와정정 msg_d8a5ccb79390에따라goal문서작성. 제품/PDL/Unity/시험쓰기·새writer0, 실제실행0. A/B검증통과뒤선행시험작성자와writer2명을기동하되DB실행은G2종료뒤다.

## 2026-10-15 - 오프라인 2단계 착수 지시
메인 msg_1d6c04a97368(2026-10-14T18:44:27Z)이 사용자 기존23:44 KST 위임과 Core 우선 /loop 지시를 전달했다. 이는 승인goal의 A/B 독립통과 조건에 따른 실제착수이며 새goal범위확대/DB실행승인이아니다. P2는PR240 head위새branch로착수·PR240병합뒤main으로rebase한다. goal첫기록뒤공통결과상태/S_CharacterLoadResult필드를고정하고World ID48예약회신msg를여기에남긴다(요청msg_885ff66b40fe,아직회신대기).
신규Opus선행시험은DB없이관찰가능한Loading입력거부·지연fake repository 틱비차단·비저장계정기존동작·패킷직렬화/ID보존·종료NoWriteNeeded/조건부release를고정공통계약으로작성한다. RED뒤writer1(SQL repository·기동·비밀주입)/writer2(패킷·서버입장·Unity소스·봇소스)는겹치지않는파일소유이며Solxhigh다. 저장대상계정판정은서버설정한지점으로고정해아침변경에대응한다.
G2오프라인수정(RED→Sol→독립검증1회)을먼저끝까지이어가고Core검증자동시1, 서버빌드/시험중량은G2와비중복/현재메모리조율후다. DB연결/새SQL자원·login·secret/UnityEditor·Player/봇실관측/보존자원/전역설정0, 실제경로는사용자아침선택과G2새수명뒤별도실행한다. 같은산출물재검증1회/비동작동결뒤·각PR exacthead사용자병합승인·새의존성SqlClient6.1.7고지는유지한다.
- World 회신 msg_9242ab697902(18:49:11Z, from term_c009ba76-a4ef-4a0f-b77b-1e306621751c): 현재47개/길드43~47/v20을직접확인·World사용/예약48없음, Core S_CharacterLoadResult ID48예약인정. 판은후행rebase때Main병합순서로확정한다. 이것은예약조율이며새protocol 실행/제품/Unity변경승인이아니다.
- 공통 api-contract.md v1을고정했다: ID48/6bytes(result,class), Success0/Busy1/RecoveryRequired2/Unavailable3·failureclass255, 저장/비저장동일패킷·Load→EnterMap/HP/roster/Ready보존·Loading현재generation/closing검사·저장계정설정한지점. 현재A/B ownercompletion의storedclass출력부재를확인해writer2가immutable PersistenceWorkResult.LoadedClass 한접점을보완하고다른DTO/owner분기·숫자계약은보존한다. World frozen43~47 정의를순서그대로PDL의선행5개로보존해48을확보하며잠정protocol21/최종판은Main후행rebase에서확정한다. 이기술선행인계를Main/World에통지하며새World기능구현권한아님, source변경은조율한다.
- 공통계약 v2는 wire/ownership v1을보존하고선행시험과2writer 사이의최소signature(selector constructor·CharacterAdmissionServices·GameSession optional주입/AdmissionState·GameWorld Attach/closeflush·LoadedClass)를첫제품/시험발행전에고정했다. api-contract.md SHA8CB73EF0893A3159D4CDE68F0C7249124739A7DB2348015BBFBFDB286C905959. 기존A/B IRepo/DTO/owner분기/숫자·Accounts는보존하며없는API부재/compile RED와실제행동실패를구분한다. R7는Main지정시범goal이아니므로한정pilot agent를자동추가하지않는다.
- 신규Opus 선행시험 task_1023078d0ea9/ctx_865977db5e26, term_31bb4f7f-b581-4080-a5fd-a1128934c244/incarnation543a3aa9-5fea-4616-a651-1233ce253a9a를다른worktree직접create로발행했다. 최초claude-opus-5-5/화면Opus5.5 xhigh·Claude2.1.300/backendunknown, tui-idle satisfied·선택창0·ready/input_accepted/turn_started observed. R=N/runtime-entry-red-1 계약SHA F9274AD517CBAEC3D4B7783E0DB94EA107BAC1515091FF769E03BAA1264DEDA9·고정88입력/누락0·신규출력부재3. 지금PhaseA 신규GameServer.Tests/Persistence/RuntimeEntry 시험쓰기만, 제품/기존시험readonly·dotnet버전/WSL/build/test/generator/DB/Unity/봇0. G2환경교정GREEN/독립판정우선·중량반환뒤같은Task공식ask로RED1회만허가한다. RED뒤두Solxhigh writer는별도겹치지않는파일계약으로후속발행한다.

- 2026-10-14T19:58:11.6891436Z 선행시험 첫 context 쓰기는 SessionGuard memo-first/checkout 밖 경로로 거부돼 성공 쓰기0이었다(msg_6d4f900edfdc). 리드는 실제 directory인 P2/.backups와 gitignore를 확인하고 일차 근거를 P2/.backups/verification/2026-10-14-persistence-runtime-entry/runtime-entry-red-1로 보완계약1 SHA32C31A5CC4F6834B8077517F8452E380B80DF7742A73F2B21E62279F6947AFED에 고정/reply msg_eaa9fb911d99했다. 원계약/입력88 불일치0·junction/hook/설정변경0이며 worker의context 첫쓰기와 PhaseA static-only를 유지한다. world.AttachPersistence+base(world) 소비는APIv2 기존 의도로 확인했으며 새API변경0이다.

- 2026-10-14T20:31:06.8109918Z Main msg_dda32697d783(20:25:32Z)이순차조건을실제실행창비중복으로명확히했다. G2정적실사/시험쓰기동안같은Task PhaseB compile/RED1을허용하며G2native공식ask가오면우선조율·직전fresh물리6GiB/commit4GiB·Unity/SQL0·DB/Unity/봇0이다. 기존전체G2정산까지RED대기해석을이Main결정으로대체하고원문/보완계약2를고정했다. 새제품writer는RED관측뒤이고컴파일/가정/행동RED를구분한다.

- 2026-10-14T20:35:33.5062577Z 공식 PhaseB 질문msg_dd2cc0d0dcbc에reply msg_f349e5fb0882로compile/RED1지금1회허가했다. Main msg_dda32697d783·보완계약2 SHAD5A3F37C82288792543DC79A554C967FD0939AC38BDC64FD0112CCDF5A12C556/원계약88보존·신규3상태정확단정·Wincommit정확측정/WSL dotnet·ownPID한정조회·outertimeout≠전후손종료·G2native비중복조건이다. 리드fresh20:32:07Z physical15,229,767,680/commit33,922,772,992/C56,701,886,464bytes·Unity0, 시작전본인fresh다시확인. 현재actual시작/SDK/compile/RED수치미관측이며WSL예약1·DB/Unity/제품0이다.

- 2026-10-14T20:51:28.8433745Z RED시작전argv실측에서wsl --가loggersemicolon을셸로분리해오류뒤exit0을가리는문제를발견했다(msg_1f8c189bc39e). dotnet0·기존probe원시보존, --exec의6인자/cwd보존을리드가직접읽고보완계약3에transport교체만수용했다. G2nativeask msg_81fb145f2a3b에우선reply msg_5dc714344b6c를주고현재P2 RED는같은질문pending/실행0이며그정확반환뒤fresh공식시작답으로계속한다. 새genericrunner/guard/product0이다.

- 2026-10-14T21:16:45.0044428Z G2 native실제20:49:36.509~20:50:34.789Z/exit0·154PASS/0FAIL/1OBSERVED, root27704/launcher29428/관측3child부재·직접child0을리드가raw및현재조회로읽었다. Main msg_ce63d772e954의순서대로같은RED질문msg_1f8c189bc39e에reply msg_ac2102c77136로--exec/보완계약3/새fresh6/4/10이상조건의지금1회시작을허가했다. actualP2시작/compile/SDK/RED수치는아직공식실행원시수용전이며제품·DB·Unity0이다.

## 신규 구현 Task 발행

- W1: task_47af7c710417 / ctx_08214a897f47, SQL/config/startup/secret transport. 계약 SHA256 6B25C765AE59C27DC1C0C675F37478DBFB20A1E5E4F9C62F1BF3F1D2247C2A2F.
- W2: task_29cef42bfe6f / ctx_5c9e367a7c29, admission/LoadedClass/PDL/Unity/bot. 계약 SHA256 C36E6029FCD893C080296D17CA06D2AB3C42CB03D8C886716A19D30FCDEB909F.
- 두 최초 실행 명령은 codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh, 화면은 GPT-6.1-Sol xhigh/Codex0.162.1/core-parallel이며 backend 실제 모델은 unknown이다. attach receipt launch=null을 모델 근거로 사용하지 않는다.
- 현재 source/static-only이며 own context 먼저, tests readonly다. 두 쓰기 종료 후 W2 단일 공유 GREEN, 그 뒤 새 Opus 독립 검증을 수행한다. generator도 공식 ask 전 실행0. Main 보고 msg_59de7fff040f, World/Content/Art에 R 자원 반환·현재 heavy0를 통지했다.
- 원문 근거는 core-parallel/.backups/verification/2026-10-14-persistence-runtime-entry/runtime-entry-sql-1 및 runtime-entry-admission-1의 contract.md, write-scope.json, first-screen.json, worker-start.json이다. 실제 DB/새 life/Unity/bot/server/병합은 0이다.

## 운영 관찰 — W2 사전 메모의 helper 이름 누락

W2 task_29cef42bfe6f/ctx_5c9e367a7c29가 새 PersistenceCharacterProbe.cs/type을 최초 context 배치 계획에서 빠뜨린 채 작성하고, 22:15:19Z msg_33aecc7acee0로 사후 보완을 공개했다. 실제 파일은 승인된 99_Tools/headless-bot/Scenarios/Persistence prefix 안에 있으며 metadata CreationTimeUtc는22:10:55.5412017Z다. 리드는 최초 context 계획에 해당 이름이 없고 사후 절에서 누락을 명시한 원문을 직접 읽었다. 사전 준수로 소급하지 않는다.

이는 첫 운영 관찰이며 기존 메인의 동결 전 운영미준수 공개·제품 비차단 결정에 따라 기록한다. 새 반복 규칙/helper/guard는 만들지 않는다. 독립 검증자는 context·실제 diff·범위·순서를 별도로 대조하고, 제품 결함/측정 위장/범위 밖 쓰기를 이 운영 처리로 통과시키지 않는다. Main 공개 msg_48d358ae720b, local S/context.md SHA F0423A7BDDF8DF1422DB693E5F90798E42FEE5C50B4AE2527FBD2F1D2AD04707(이 읽기 시점).

Generator 공식 ask msg_bc4c593b220b에는 reply msg_e6eb56d0f689로 fresh 읽기 preflight만 허용했다. sync/dotnet/generator는 다음 공식 now-start 전0이며 W1/W2 source/static만 진행한다. 프로젝트 참조0/no-manager 기본과 PDL frozen World1..47 대조는 lead-generator-source-audit.json에 보존한다.

## 생성 소스 줄 끝·DLL 출처

Main msg_e307b2623f71의 10-11 도구 사실을 수령했다. 참조 dungeon goal430~431줄·Shared EmbedAllSources/embedded PDB·gitattributes *.cs LF를 직접 대조하고 W2에 contract-supplement-eol.md SHA AC2846A810CFAE4E19E0DEA285B2A0649C3B667DEF07594700449481CC4766C3을 전달했다(msg_25420ea81e19). generator 직후 node 실제 byte 줄끝·SHA를 남기고 mixed면 공유 GREEN/Shared build 전에 owned GenPackets만 LF로 맞추며 원본·복사본·정규화본 hash를 구분한다. Linux 실행 예상은 실측이 아니다.

생성기 자체 수정은 범위 밖이며 Windows Plugins DLL 쓰기는 이 보완으로 추가되지 않는다. 이후 승인된 DLL 반입은 intended LF commit source bytes의 빌드 출처를 독립 검증 입력에 연결한다. 아직 실제 생성 결과/EOL/compile 성공을 판정하지 않았다. Main 회신 원문은 lead-send-main-generator-eol-applied.json이다.

### 생성기 1회 실제 결과

W2 msg_77a95e8ba535와 local raw/generator-*를 직접 대조했다. sync1 exit0, generator1 native22:21:55.3840325Z→22:22:03.1741072Z exit0/7.7901초,6 input strict hash 일치/no-manager. 추가 UseSharedCompilation=false는 수신 전에 원래 승인 argv 실행이 끝나 재실행하지 않았다. WSL narrow dotnet/testhost/MSBuild0/PGID3010와 exact Windows11676/30596 현재0을 확인해 generator 창을 반환했다. 메모리 peak/역사 전수 transient descendants/hard cap은 미측정이다.

리드 node 실제 byte 계측은141450bytes/LF3763/CRLF0/loneCR0/BOM0/끝LFtrue다. GenPackets clone/Windows SHA5AF9E8A62658923E9D6E8520CAE0D01185190598C6F7E0126F57185C15AACADB, LF 정규화 쓰기는 불필요했다. 원시 N/lead-generated-eol-node.json와 localW2/raw/generator-copy-hashes.txt를 독립 입력에 넣는다. 최종 실행 plan의 NowStartApproval 질문ID는 실제 reply msg_ee99e9f780a0로 별도 교정 기록을 요청했으며 원본SHA068BFAAF889ECE4935DF31C0306125C86F7E6DB581472EDBE0774DCA6588625E를 보존한다.

W1 source15 freeze/hash 일치, W2 source-ended 보고의 최종manifest/diff는 수신 대기다. 공유 GREEN/독립 제품 통과·실DB/Unity/bot는 아직0이다.

### 두 소스 동결·단일 공유 GREEN 창

W1 task_47af7c710417/ctx_08214a897f47 source15, W2 task_29cef42bfe6f/ctx_5c9e367a7c29 source31(신규13)을 동결했다. W2 manifestSHA137DDB3B13600B6B602AAB52167AD62850018C3931F243F4BB3533A2518BE8EF/diffSHA91B3CC16E7C392FBA84E7FA18BEABED119B8DCB31889EA71ABF52F321D5672FC. 리드22:29:29.865Z actual46 path/46unique/hash mismatch0·W2ownscope밖0를 확인했다. 이는 manifest/bytes의 대조이며 제품 독립 PASS가 아니다.

공식 ask msg_3a4dc7b2c690/planSHA2C7397F367A8B99F3E1CC6AC16E674EB5C8A5930EA86690DAC4F06F5B09842A4에 reply msg_a24e432e2755로 single GREEN now-start를 승인했다. 리드 fresh22:29:30~32Z physical14595997696/commitfree34162962432/Cfree56370589696 bytes·Unity0·narrow WSL0, Worker 시작 직전 재확인, World/Content/Art 창 예약이다. --exec env invocation-only UseSharedCompilation=false·isolated state로 기존 sync-wsl whole slnx build→RuntimeEntry filter/TRX 단1회만 수행하며 양쪽source/tests 쓰기는0이다. W1은 같은 원시를받아 중복실행하지 않는다. 현재 actual결과/독립판정/실DB/Unity/bot는 대기다.

## 메인 의존성 보안 수정 결정

Main msg_692503ec9733(22:50:53Z)은밤샘위임내 메인판단으로 별도 신규 Sol xhigh가 GameServer.csproj에 System.Security.Cryptography.Pkcs9.0.21 PackageReference 한줄을추가하도록허용했다. SqlClient6.1.7/SDK고정,Nu190x억제/NuGetAudit끄기/전역Nuget·SDK설정변경0. 실제restore전후assets graph에서9.0.21 해결과다른패키지버전변동을원시로대조하고동일build에서NU1902/3 해소를관측한다. 다른새경고자동pin0,메인에보고한다.

GameSession오류수정자는GameSession.cs만,package수정자는GameServer.csproj만 쓴다. 두쓰기종료뒤공유GREEN2 단1회에함께넣고새Opus가의존성변경/전후graph를독립입력으로검토한다. 병합요청의새의존성은Microsoft.Data.SqlClient6.1.7 및System.Security.Cryptography.Pkcs9.0.21(SqlClient하위의존성보안pin) 둘이며, https://github.com/dotnet/runtime/security/advisories/GHSA-2fmv-rv2c-59x7 와 https://github.com/dotnet/runtime/security/advisories/GHSA-846f-f764-76mx 를연결한다. 실제우리연결설정의취약경로도달성은미검토이며,최종의존성승인은사용자각PR정확head병합승인때다.

GREEN1 판정/정산: W2 task_29cef42bfe6f reportSHA AE49372B6781DA77B3EAADD0580815F651520CBACF438F0BFAFD04498FC987FD/done msg_eb8eed76c2c9, W1 task_47af7c710417 reportSHA65B8068A243DCDD07C364F934960692C0C840581C23FDE0D8D85B60D8E7835D1/done msg_a27f1fceb08b. 둘 원문전체/실제진단/원시/미실행을읽고 release→exactinc/idle→literalclose(둘ptyKilledtrue)했다. W2-G1~G3 각FAILED1, 같은산출물의W1peerblocked는이중집계하지않는다. independentNOTPASS/Advisor3회조건은도달0이다. W1소유style warning24는별도공개·미수정이다.

### 실제 새 수정 Task

GameSession task_199a68b875bf/ctx_6e284af24364, contractv2SHA A76CF0D1378105B5B11BDF164A684F99F963ED77B8C87A8DFC12E29587847796(83inputs/missing0). Pkcs task_4ca97c8522be/ctx_b9fc7cf5408e, contractSHA6A7ABFBA00B0833368E50F414DD7351CA9320C6AC498722EB9395C279D336F4E(25inputs/missing0). 두최초명령codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh,UI GPT-6.1-Sol xhigh/Codex0.162.1/P2/backendunknown,모두ready/input_accepted/turn_started다. S3draftv1을보존하고Main승인peer분리·공유GREEN2운영을v2로발행전에반영했다.

새소스쓰기만진행하며currentheavy0,두freeze뒤S3단일GREEN2/S4같은raw,tests읽기전용/새Opus독립검증예정이다. 각primary근거는P2/.backups/verification/2026-10-14-persistence-runtime-entry/runtime-entry-session-order-fix-1 및runtime-entry-pkcs-pin-1이다.

### 리드 baseline hash 전사오류(첫 관찰)

새 GameSession task_199a68b875bf/ctx_6e284af24364가 제품쓰기0에서 공식ask msg_d44c09d2b29c로 literal계약baseline과입력불일치를막았다. 제가첫manifest행의CharacterSelectHandler SHA FF98111111BE49BC41F4BE1192177EFDEDFE2F90E06B4BFA61571BB9C01D52A1을GameSession으로잘못옮긴계약/spec 전사오류다. W2frozen exactPath·S3inputs·currentGameSession는3E4EF4A50F6CA6C78F654C7AA45C6DAD46107CD6CB88E95DA03E32F2401AC9EA(33080bytes/LF721/CRLF0/BOM0)로일치해실제source drift0이다.

원본contractSHA A76CF0D1378105B5B11BDF164A684F99F963ED77B8C87A8DFC12E29587847796/spec을보존하고contract-supplement-baseline.md SHA4219B64E75C5AD50E915D2CEDD62ADFAFF6832846057F48BACF60FD917AECA3E + lead-baseline-correction-source.json의경로결합실측으로자료만수리했다. 공식reply msg_8e1b7a14ae6f,Main즉시공개msg_17934f500515. SAME Task/SAME파일/SAME5오류이며source쓰기전owncontext에정정연결을요구했다. 기준source가새로바뀐것처럼소급보고0/새productFAILcount0이다.

교정층은기존manifest의Path→SHA 자료추출경계로해당전사값을수리했다. 첫관찰은이goal에기록하며새generalguard/반복규칙/global설정은추가하지않았다. 이후독립검증이원본/정정/실제diff를대조한다.

### 수정2 쓰기 종료·GREEN2 단일 승인

GameSession write-finished msg_046d27e118c1/SHA41D9839C4684CB9AB3F9BADDA5CE591E0F27BF0E2C53D67C963ACB1499A93823, csproj write-finished msg_cbe7021665d4/SHAF34B7FFD642555021D987256E8B72BE93B8CF9CB32E54170062D210E983119A6. 리드83전수current대조는승인2delta/다른81보존/unexpected0,한줄87byte추가의inverse가기존1C9CBF...C9C8/1854byte로맞고UTF8BOM/CRLF/SqlClient6.1.7을보존한다. Source점검이며build/behaviorPASS가아니다.

S4XMLstringcast의BOM해석오류뒤read-onlyXmlLoad성공·S3초기aggregatefilter원시와corrected counts·leadNode측정후PS exit 문자열명령오류(outerexit1/nativeNode0)를서로구분해보존했다. source재쓰기/테스트retry0. N/lead-green2-input83-check.json,lead-pkcs-one-line-inverse.json,lead-pkcs-inverse-exec-note.json 및각작업raw가근거다.

공식ask msg_2c19831b9bdd/planSHA831531A41EFFC36E2CAF02CDC7C1CBF14094E347CDBD05B1C503005918B3C051에reply msg_9493b697e04f로단일GREEN2를승인했다. World/Art/Content23:18실행·예약·예정0원문,leadfresh23:20:36~40 physical15865876480/commitfree35468267520/Cfree55654305792 bytes·Unity0·narrowWSL0,WorkerStart직전재확인/세파트창예약이다. sameRuntimeEntry filter/TRX한회와assetsGraph전후/actualNU190x출력을같은원시로두worker가소비한다. actual결과/독립제품판정/실DB/Unity/bot/최종사용자병합은미완료다.

## 2026-10-15 - GREEN2 실제 결과·보정 작업자 종료와 독립 검증 준비
- S3 task_199a68b875bf/ctx_6e284af24364 done msg_6a285c442ed2, report SHA AF1A2EA153A53321F67BB7956E96EDE898D4CE8C1C92B5E209D6E3D3C47DD87B. S4 task_4ca97c8522be/ctx_b9fc7cf5408e done msg_4496d38f1b41, report SHA D5305FC73952450877DB9DE6C2D46EE55EFB2FCD815EFA5337A048924A0028EB. 리드가 전문·context·diff·raw를 실사하고 자체점검 succeeded로 정산; release external retained/processnone 뒤 exactTask·handle·incarnation·tui-idle를 대조해 각각 literalclose ptyKilledtrue, reclaimable0. 전 세션 재사용0/독립 PASS 아님.
- 공유 GREEN2는 ask msg_2c19831b9bdd / 실제 now-start reply msg_9493b697e04f 아래 단일1회. native6528 birth23:22:19.1033865Z→exit23:22:39.5275287Z, exit0/20.4241422s. 시작 시 ownWSL PID308/PGID308 관측. 실제 build48warnings/0errors, TRX total/executed/passed122·failed/notExecuted0, 122 result 모두Passed·SHA00757734F664FB6873CEC69929FBB80D4DD90277879B5C527F0439BBFA0650AE. 선택 분석기 error0, NU1902/3 실제출력0. 반환23:23:33~36Z native없음/ownPGID3080/narrow0/Unity0·physical14360784896/commitfree33717825536/C55001649152bytes, 세 파트에 실행창 반환 통지. Main msg_7f086ba155db로 근거와 다음 독립검증 보고.
- GameServer/Tests actual assets package31→31·45→45에서 Pkcs9.0.11→9.0.21만 version delta, SqlClient6.1.7 고정/다른version0·after logs[]. 새의존성2개는 사용자 exacthead 최종승인 때 같이 표시한다. exploitability/연결 취약경로 도달성 미검토. 잔여48경고는 StyleCop/IDE42+xUnit6; GREEN1은 Tests compile 전 실패하여 신규관측6을 pin회귀라고 단정하지 않는다. 미실행DB/Unity/bot/server 및 새 독립 판정을 통과로 확대하지 않는다.
- 신규검증 근거는 core-parallel/.backups/verification/2026-10-14-persistence-runtime-entry/runtime-entry-verification-1/. 강 등급 전체46제품+10선행시험, 실제 Task 연결·요구사항/보존동작·제품계산복제/측정위장·상태/수명/취소/실패·양쪽패킷/자산 보존을 판정한다. 보정 전후 메타오류/aggregate교정/CLI wrapper exit 오류 원문도 보존한다. actual SQL·DPAPI·Unity·bot 실행권한은 아직0이며 코드 범위와 전체goal 완료조건을 구분한다.
- 신규 독립검증 task_41cd6443cc67/ctx_2320cad7278c, term_a6c224da-94dd-43b7-ad75-dfacb570a4a6/incarnation7152465a-5d56-4890-abaa-daa049c879a0. 최초claude-opus-5-5·화면Opus5.5 xhigh/Claude2.1.300/backendunknown, ready/input_accepted/turn_started·선택창0. 계약SHA2A12E9782A05217F85ADA9690E7220D4727C5973D394E9272D862CEF3989DB82·입력160/누락0·관련CODE9절전체·실제4구현Task연결. AccountNameRule 경로오기1개는발행전기계확인에서정정했으며제품/Task추가변경0. 현재정적실사/NEW독립시험쓰기만, 실제중량은저장계획/fresh/exclusive공식now-start뒤; actualDB/DPAPI/Unity/bot/server0 및전체goal未완료. Main 보고 msg_03bd5b91b377.

- 독립검증 진입편차 원시대조: msg_98795daa0252/정정msg_4a6ebc1534f9, 쓰기거부2+비쓰기mailbox-output-loss1=unique3호출/중복원시5occurrence. 리드가당시transcript prefix3657930bytes SHA33CFD5EE7DEC5E4B194DEBF0BCF5485CE4BA140E1811315C2698294927A5EC45 일치·3toolID를직접대조. 첫성공context Write tool23:39:29.012Z/result23:39:29.314Z, 메모시계23:38:40Z와구분하여앞선시각표현정정. scratchpad/check1.json은checkout내할당밖, /dev/stdin특수경로; hook PreToolUse DENIED 원문확인/밖byte부재독립미관측. 첫추출의5중복count→3수정·같은출력덮어쓴사실은context/원transcript로보존. 현재제품/기존시험쓰기0·실행0·정적실사계속, 기존G2같은부류반복관측/새rule·guard0. V/raw/entry-denied-attempts.json SHA04A3EDFAEDF98C45C85BA2639F054A0C0F56EB22E89B4AAE2643F1E2130A9D37 및lead-entry-denial-source-audit.json, Main보고 msg_dd60bed0c895.

- 독립검증 실행ask msg_18ea3737d1cd(plan4D9F9C367418B2521161F1B180789F88D1F6EC51356D6544F922CFD2FAA18A06/harness7F2D2704602EE6802020E34EA4FF01F9E1B2AAF0103BF19C43DB81BBA10761E7)에 actualreply msg_c10b9659b6f6로 Phase Test whole-slnx restore/Debugbuild·무필터전체시험 정확1회만 now-start 승인. World msg_955315cc1339/Content msg_3648ff74da1e/Art msg_dffde24f512d freshheavy·예약·곧예정0/반환전보류, 리드00:15:59~16:03Z physical15733309440/commitfree35077914624/C54996516864bytes/Unity0/WSLnarrow header-only(exit1) 및160입력+NEW5+Plugins2해시불일치0. 자기fresh검사·Plugin고정hash명시수동대조원시를조건으로부여(현재harnessPlugin은기록만, 자동gate아님). Entries E1~6/L1~2는未승인·Test반환후case시작PID/birth원시·fresh입력/자원·실제DLL출처보강계획으로진행. actualSQL/DPAPI/Unity/bot/listen7777·제품/기존시험수정0, retry/kill0. Main보고msg_d44e714f7342. HB23:52:25~23:58:11=346초운영편차는정산전구간원시대조로분리하며정상소급0.

- Test 실제시작 native22576 birth00:18:34.4597204Z/WSLPID430·PGID430, 자기freshphysical14038106112/commitfree33142345728/C54948552704bytes·160입력Bad0·NEW5/Plugins수동gatepass 확인. 현재결과/최종exit/TRX대기, Entries未시작. HB346초구간은actualtimestamped12toolcalls(허용NEW시험5Write/context1Edit/읽기·mailbox6Bash)·현재hash/mtime를대조, 관측밖역사wholefilesystem보장은하지않음. Main밤msg_ce63d772e954 조건부운영비차단/공개유지·정상소급0, V/lead-heartbeat-gap346-calls.json·review.json. Main보고msg_8733c4b5e02f.

- 독립Test1 실제nativeexit1/137.1495s, build48warnings0errors, TRX SHA E9EFA0D82192396F1D5A87ADBD2729BEA6320DDCECAC57C7C7528B4E7AF086BA/1474결과=1458pass+11fail+5skip·executed1469. RuntimeEntry기존122+NEW55 전부PASS, 실패11은기존protocolversion/count/maxID·입장순서/StatsSet단정이며전체실패분류/보존단정미실행은최종verdict로받음; 기존시험수정·재실행0. Main msg_9135feaa325d.
- Entries는새askmsg_3fbd1c06a239→replymsg_692987144cfe 선택C/E1~6·L1~2 한회, 00:34:30~45Z 모두exit1·Listening/markerfalse·native8종료. E6(native34568/WSL502·PGID502)는6초시점동일DLL생존·7777빈값, stdin12초뒤닫을때12.212s에반환하여실제5초secretBudget미효력관측. L1확장거부/L2없는clixml Import거부·Startfalse·credential부재/DPAPI0. Test빌드DLL93A39B88BFF0025AFA53CBDE62E877879474C7431C726C54ABC2637A94AEC747고정. actualDB/SQL/credential/DPAPI·정상host7777/Unity/bot0, 제품/기존시험수정0·retry/kill0. ports7777/1·narrow·GameServer잔존0 및native8부재, 현재physical14333153280/commitfree32980922368/C54452670464bytes/Unity0, 세파트예약창반환msg_bc988c40ff2b/msg_ac08be2a3d12/msg_bd0d444aaf06. Main msg_060b446378c6.
- HB484초(00:10:32~18:36)는공식ask대기중이어서자동조건부수용제외·Main별도판단으로올림. bounded29toolcalls에NEW시험Edit/revert순변경0·정적자료/집계·승인후Teststart가있으며, Monitor로sleep270 heartbeat반복을요청한사실도발견해원시/실제성공·현재상태·자기observer종료receipt를요청했다(msg_cf5033036612). 새timer금지와적용관계를가리지않고최종운영판정과분리, 제품PASS로확대0.

## 2026-10-15 - 사용자 저장계정 A 확정·선행 PR240 병합 알림
- Main msg_ad7dc7556d17(00:35:58Z)이 사용자 원문 「아트쪽은 한번 시안 보여주고, 나머지는 A로 가자」를전달했다. 저장대상계정은A그대로, 서버설정에적힌정규화계정하나만DB저장/복원하며 APIv2 SHA8CB73EF0893A3159D4CDE68F0C7249124739A7DB2348015BBFBFDB286C905959를바꾸지않는다. 현재독립검증을먼저끝내며 SQL은별도G2 A새수명/비중량동시경계로진행한다. 사용자직접입력·새PR병합승인으로격상0.
- Main msg_08d90328175c: 사용자메인창 「병합 승인: PR240 head f4d80d256a3041acd15624d804300157be05548b」 제출뒤메인이PR240을00:30:38Z에 a0c48bc5ab96ed1455dbfbbd3c1bd6b16c14693d로병합했다는알림. PR238도19664796865dc8d3fb5b994c296e87bf66ce73ee로반영. 현재동결입력/진행검증은바꾸지않고안전지점후PR240 goal에승인/헤드/결과/시각을기록한다. 다음계약은latestmain의AGENTS모델라우팅정본을읽으며PR준비때feature의main merge/rebase방식·packetID재확인을명시한다. Codex병합0.

- 독립 Task task_41cd6443cc67/ctx_2320cad7278c는worker_done msg_f7d538fa2f4e outcome succeeded(검증작업종료, PASS아님)로정산됐다. 실제판정 BLOCKED #1/#2(11기존시험 a9/b2, 귀속lead계약/goal), #3/#4/#5 Unity未실행보류·WindowsDLLgap, O1~O6 관찰. verdict SHA ED8549367D40E8A211DFCD3E66FFE04E400E51B9AA90025DD0289E3AFBEB79F3/context6ED0EDDB1D50D8903D9E6B241E24EF11D8DE97C1418FE8F80E53FEEE47509062 전문을리드가직접읽었고원시1474/177/Entries8과대조. 구현메모계획↔실제준수위치세부대조는원문에未검토로남아full강PASS아님. 제품46·기존시험쓰기0/160현재입력불일치0. release→exactincarnation·idle→literalclose ptyKilledtrue·reclaimable0, 세션재사용0. 자기보조Monitor b85ahiwjm은Stopreceipt없어전프로세스0주장으로받지않고원본/30mexpiry/PTYclose와Main운영판단에따로인계. 새원문추가전달은dispatch_inactive로거부되어재발행/재기동0.
- Main Docker정정 msg_3852c92bd957은G2goal에기록했고사용자기동대기를철회했다. 현재재접속독립검증정산완료뒤G2A계약준비로진행하되이goal의#1/#2수정/Unity·DLL검증/실제DB·bot/병합관문은未완료로유지한다.

## 2026-10-15 - 묶음 수정·SQL 병행 최우선 지시
Main msg_631d31690f19(00:47:12Z)은사용자 「밤사이에 뭐 한게 없는데??」·「일단 그 사이에 밀린 작업 리드급들한테 지시해줘」를전달했다. 기존11시험+E6를새Solxhigh task_2a084c048f52/ctx_de70a64026dc에9기존파일범위로위임했고latestmain19664796을local feature merge66104100에통합했다(기존제품변경보존). source/static단계는G2A정적준비 task_38a17ed7e6be/ctx_0803c19c0c23와병행, 중량은순차resource관문이다. 새계약은전체단계와자동메모리/input/잔존관문을한번에승인·단계별ask대기0/관문실패·계획밖만ask, HB지연한줄·다른의심때만원시대조, 타파트중량은짧게통지하고회신대기로정지0(MainA~D). 독립검증자분리/미실행구분/정확head사용자병합관문은유지한다. 통과뒤commit/push/새의존성2개표시PR까지진행한다.

## 2026-10-15 - 사용자 운영 경량화 적용
Main msg_a8dd25f254a5가 전한 사용자 원문: 「대시보드 결정 응답: 1) 검증 경량화 1 - 실행 단계 승인을 계약 때 한 번에 할지 → A 계약 때 일괄 승인 · 2) 검증 경량화 2 - 생존 신호 300초 초과를 매번 원시 감사할지 → A 한 줄 기록 + 기존 집계만 · 3) 검증 경량화 3 - goal 기록을 결정·단계 결과·결함만으로 줄일지 → A 핵심만 기록 · 4) 검증 경량화 4 - 파트 간 무거운 실행 조율을 메시지 대신 공용 잠금 도구로 할지 → A 공용 잠금 도구」. 1~3은 지금 적용하고 기존 기록은 고치지 않는다. 실행단계 목록·순서·자원/input/잔존 관문은 계약 때 일괄 승인, 관문실패·계획밖만 질문한다. HB지연은 구간/이유 한줄과 기존 집계만, 다른 의심 때만 원시 대조. goal은 결정/단계결과/결함과 원시 경로만, 기록 커밋은 단계말 묶는다. 4는 별도Rules goal이며 도구 main 반영 전에는 기존 우선순위와 짧은 통지를 유지한다. 독립 세션·역할분리·등급·보호/전역경계·각PR 사용자병합승인은 유지한다.
- Main msg_580bfda12cd5 조건에따라E6의Unix/Windows native readiness 확대안은후속으로돌리고같은Sol Task의기존11시험만GREEN으로진행한다. 계약보충/원문정책/실행의존해제는 runtime-entry-correction-1/에보존했다. APIv2·E6기존제품은보존, 다음독립검증은11시험과메모세부대조를수행한다.

- Main msg_f1e22930918c·msg_0644882977f5: E6 native readiness 확대는 후속으로 확정하고 기존 11시험만 마무리한다. 수정 작업자는 7시험 파일만 동결·자체 무필터 GREEN을 끝내 정산·종료했다. 원문/수치/전수표/동일명령·동결 근거는 runtime-entry-correction-1/에 보존했으며 독립 PASS는 아직 아니다. 새 독립 재검증은 지체 없이 발행한다. Unity 프로세스 둘 중 하나는 Content Editor의 import 자식으로, Editor 두 개로 세지 않는다.

- Main msg_7bde6e4ffd82는 Content C9가 서버·Play만 실행 중이면 Core 전체시험을 fresh 물리6GiB/commit4GiB·입력/잔존 관문에서 병행해도 된다고 확정했다. Content 소유 GameServer/7777은 정확 PID·birth로 구분해 비차단이며 Core는 loopback 임시포트만 쓴다. World·Content 실제 build/test와는 비동시, 자원 관문이 막으면 C9 반환 뒤 바로 진행한다. 최신 원문/보충 및 관문 재판정은 runtime-entry-reverification-1/에 보존하고 검증자에 전달했다. SQL은 Content Unity 반환 뒤 그대로다.

- 신규 독립 재검증의 이번 범위 통과를 원문·메모 전문과 실제 TRX/현재 입력에 대조하고 정산·정확 세션 종료했다. 기존 11건은 전수 a11로 재분류했고 두 fixture의 옛 순서 단정도 같은 원천으로 설명했다. source/시험 쓰기0·기존 입력 보존, 새 차단 결함0이다. V1 #3/#4는 Unity 미실행 보류, #5는 Windows DLL 반영과 양쪽 소비자 검증 전 병합 차단으로 유지한다. O1~O6 및 D1/T1/G1~G3은 공개 후속이다. 원문·실제 구간·수치·정산: runtime-entry-reverification-1/{verdict.md,context.md,lead-final-count-and-input-check.json,lead-final-settlement.json}.
- 운영 편차는 초기 읽기 전용 Git 두 호출과 C9/메인 관문 대기·최종 판정 작성 중 신호 지연 두 구간이다. 메인에 공개했고 helper 원시는 같은 근거 폴더의 lead-liveness.json에 둔다. 판정서의 서버 전체시험은 테스트 진입으로만 집계하며 새 제품 launcher/정상 서버/DB/Unity 실행으로 넓히지 않는다.

- PR242 - 영속성 서버 진입·로딩 게이트·직업 복원 초안을 생성했다: https://github.com/bass131/dawnholder-server/pull/242. 독립 통과한 코드/시험과 관련 문서를 commit/push했고 코드·시험 bytes를 보존했다. 새 의존성 SqlClient6.1.7/Pkcs9.0.21과 알려진 O1~O6, DLL/Unity·실제 DB/봇·CI 미완료를 PR 본문에 명시했다. 병합 승인 요청이나 전체 goal 완료가 아니며 #5 DLL 병합 관문을 유지한다. 원시 Git/PR 근거는 runtime-entry-reverification-1/lead-*.json 및 lead-pr-body.md에 둔다.

## 2026-10-15 - DLL 반영과 통합 검증 준비

- DLL1차는 UNC 첫 읽기 미반환·제품쓰기0으로 환경 미완료 정산했다. 새 외부 Astra2가 정확 WSL CLI로 source50과 bin/clone Plugins/수집본 byte 출처를 대조한 뒤 Windows DLL 두 개를 각각 한 번 반영했다. 소스·PDL·meta/GUID는 보존됐고 두 DLL 밖 제품 변경은 없다.
- Task task_6d7b057bfe38/ctx_49bcdf624fc9의 보고/메모 전체와 현재 입력69의 허용DLL2 차이·실제 Git diff를 리드가 대조하고 release/정확instance/idle/literalclose로 종료했다. runtime-entry-dll-promotion-2/{report.md,context.md,lead-source-sample.json,lead-final-settlement.json}에 실제 bytes·출처·정산을 둔다. 역사 V2 당시 두 DLL SHA 미기록 한계와 초기 출력처리/exit 전사 교정을 보존했고 소비자 실행 검증은 다음 신규 세션 몫이다.
- HB 정본helper는333초 over(공식질문 미해소 중 상태 사이)로 보존했다. 실제 공식질문~회신388초와 중간status 때문에 현 인접신호 helper가 대기를 빼지 못한 범위를 한 줄로 공개하며 별도 transcript 감사는 하지 않았다.
- Main msg_818353d70c45의 PR243 정본화와 다음 안전 지점 main 통합을 따라 DLL/goal을 로컬 체크포인트로 보존한 뒤 main을 통합한다. 진행 중이던 D2 고정 입력과 과거 판정은 바꾸지 않는다. 통합 뒤 새 기준으로 서버·Unity 실제 검증을 이어가며 PR 병합은 하지 않는다.

## 2026-10-15 - DLL 반영과 최신 main 통합
- D2 구현자는 고정 Linux 소스와 Windows DLL2를 반영하고 종료했다. 기존 meta/GUID와 나머지 입력을 보존했으며 양쪽 소비자 실행 검증은 아직 아니다. 원문·정산: runtime-entry-dll-promotion-2/.
- 메인 지시에 따라 기존 PR242 head 계보를 보존하는 merge로 main6740f26을 통합했다. 로컬18c4facd는 기존61b66214의 후손이고 수동 제품 수정은 없다. 참고 rebase 결과는 별도 로컬 branch에 보존하며 force push는 하지 않는다. 근거: runtime-entry-dll-promotion-2/lead-main-merge-final.json.
- 새 강 검증은 통합 상태의 서버 전체1회와 Unity EditMode1회·실제 패킷 소비자를 다룬다. DB/DPAPI/정상 launcher/봇/실제 장면9는 후속 관측으로 남긴다. 계약·입력·결과: runtime-entry-unity-dll-verification-1/.

## 2026-10-15 - 양쪽 소비자 독립 판정과 fixture 보완
- 새 독립 강 판정은 차단이다. DLL 출처와 실제 Unity 로드 bytes·소켓→handler→메인 큐의 새 시험22건은 통과해 옛DLL 차단은 해소했다. 통합 서버 전체는1,497통과·실패0·기존제외5다. Unity 전체는477건중339통과·138실패이며128건본문은fixture누수로미실행이다.
- 차단 #1은캐릭터로드성공입력이없는기존fixture, #4~#6은HUD직업입력·옛protocol19단정·EditMode파괴/동일인스턴스재사용단정이다. #2생성실패정리/#3초기화전TimeScale복원도같은좁은보정에서다룬다. 다음은새Astra와새독립Opus각한작업이며현재수정/재실행은아직없다.
- 검증자는정산·정확종료했다. 자동설정2와IDE/UserSettings/LocalLow시험사본부작용은보존·공개하고tracked자동변경은커밋하지않는다. 초기읽기Git계약위반·생존신호초과3구간은Main에인계하며상세는근거에둔다. 원문/전수표/실제수치/정산: runtime-entry-unity-dll-verification-1/{verdict.md,context.md,lead-final-settlement.json}.

## 2026-10-15 - Unity fixture 보정 자체 GREEN·독립 재검증 인계
- 신규 Astra task_a51dd8b4e565/ctx_31f461ff129e가 기존 시험/helper11개(+296/-46)의 명시적 로드 성공 입력·생성 실패 정리·초기화 전 시간 복원·권위 HUD·literal21·reset 뒤 새 인스턴스 경계를 보정했다. 기본 fixture/공용 Handshake의 무로드 의미와 NEW22·제품/DLL/meta를 보존했고 설정2는 초기/사후 각각1회 지정 baseline으로 복원했다. 원문 보고·맥락 전문/실제 diff·입력2109/주요 근거12 hash 및 기존 실패138의 실제 PASS 대응을 리드가 확인해 구현 작업을 정산·정확 pane 종료했다.
- 원본 P2 whole EditMode 자체1회는05:21:16.7774830Z→05:22:54.2224802Z, exit0/97.4449972초, 481PASS/0FAIL/0skip이다. 기존477은 protocol 시험명19→21 대응1을 포함해 모두통과했고 추가4 및 불변NEW22도통과했다. 이전cold/이번warm Library·TEMP/동시WSL관측 차이를 공개하며 자체GREEN을 독립PASS로 소급하지 않는다. 새 독립 Opus의 강 검증·독립 시험·전체 실제 실행은 다음 단계다.
- 반환은 worker 교정 UTCticks와 리드 별도 rawbirth 대조에서 root1+관측고유자식14 모두PID부재·Editor0/자기lock0/kill0로 확인했다(생애전수아님). DateTime/문자열 혼용 반환·리드 재파싱·집계v1·늦은현재형·ask wrapper 출력 누락의 원문과 정정을 보존했다. 생존helper는05:24:36~05:30:08의332초 over1(설정복원/138대응·보고 작성)이며 추가 전사감사는 하지 않았다.
- 상세 원문·전수표·원시·정산은 `.backups/verification/2026-10-14-persistence-runtime-entry/runtime-entry-unity-fixture-correction-1/{report.md,context.md,raw/D-original138-detailed.json,lead-final-source-check.json,lead-final-settlement.json,lead-terminal-close.json}`에 둔다. Content 다음 창에 정확반환을 전달했고 현재 이 보정의 추가 실행/예약은0이다. 실제 DB/DPAPI·정상 서버/봇·Play 장면9·새 독립판정·새 head CI/사용자 병합 승인 등 전체goal 조건은 아직 남는다.

## 2026-10-15 - Unity 소비자 독립 재검증·단계 정산
- 새 독립 Opus task_583679cb0d2b/ctx_1137a2351b37는 구현 Task task_a51dd8b4e565의 Unity EditMode 소비자 범위를 통과로 판정했다(제품 차단0, Destroy 기대문구3곳 중복1 비차단 후속). 신규 독립 소유권·타연결권위·정상종료/다음연결3사례를 작성했고, 원본2541+신규2 불변/전체snapshot2081 byte동일 관문 뒤 cold whole1회를 실행했다. 484PASS/0FAIL/0skip, 기존481·이전실패138·NEW22와 신규3 모두통과, Shared/ClientNet의 snapshot 실제로딩 SHA/MVID도 고정 bytes와일치한다. DB·DPAPI·정상서버/봇·Play장면9·Play지연파괴·새headCI/사용자병합은 아직미완료다.
- 실제 root39572/birth06:12:37.6347850Z→06:16:51.6348444Z exit0/254.0000589초다. 잘못포함된10/14생성Discord6을 부모PID/birth순서로분리한 유효관측후손166+root1은 리드06:18:08~18:18 대조에서165PID부재/2다른birth재사용/동일생존0,Editor0/자기lock0/kill0이다. 총17샘플(start1+관찰16)이며생애전수아님. 이전raw172집합/자동생존6통지/교정원문을보존했다. 검증작업은원문·맥락전문/실제시험/2541+2보존·XML/R2표본을대조해정산하고정확pane종료했다. 신규시험cs/meta2파일은현재회귀자산으로보존한다.
- 환경 E-1은 snapshot PackageCache의264~274자 UXML4 가져오기실패다. 원본·snapshot해당파일존재/기존원본경로247~257자를메타데이터로확인했다. 긴경로관련원인추론이며추가인과재실행0,코드컴파일/484실행과별개로공개한다. 다음snapshot선정은생성경로길이까지검토해야하며판정서의짧은경로예시만으로해소를보장하지않는다.
- 이번검증자 운영O1~O5(초기Git환경변수사용자기보고,미ack재독,첫AST오류원시삭제1/원본bytes미회복·tool기록2보존,거짓후손교정,신호간격)는메인판단대기다. 기동명령은GIT_OPTIONAL_LOCKS=0을명시했으므로첫Bash의실제상속값은당시실측없음으로구분한다. 정본helper는322/335/314초 over3(정적실사·시험작성/snapshot·harness준비/질문직전),공식질문316초를제외했고추가전사감사0이다. 종료된dispatch후속send거부receipt도보존하고재사용하지않았다. 과거원문/판정을소급수정하지않는다.
- 판정전문 SHA5601CA4C3BC44801F1AD5E700D6FBB77C29D38DE6EAF0BE8BED4BE928E27017C·맥락SHA815270AEC8182BDE46D12A1035C51F9A1727723256AD4A9632E1333C6B636DC5 및실행/검증/정산은 `.backups/verification/2026-10-14-persistence-runtime-entry/runtime-entry-unity-fixture-reverification-1/{verdict.md,context.md,lead-final-input-source-check.json,lead-c-lineage-reclassification.json,lead-liveness-result.json,lead-final-settlement.json,lead-terminal-close.json}`이다. Content에실제반환/다음창우선을전달했고Core추가중량실행·예약0,전체goal완료가아니다.

### Docker 성공과 sa 비활성 확인 분류 정정 — v4(2026-10-16)

- 메인 `msg_a570d5d1632d`는 v3의 SQL18470을 비활성 로그인 거부의 예상 결과로 판정하고, 앞선 「sa bootstrap 실패도 Docker 방식 중단」이라는 과도한 적용을 철회했다. 실제 ready·DB bootstrap은 성공했으며 Windows SQL Express 조건부 전환은 발동하지 않는다. 이 정정은 위 v3 중단 조건에 우선한다.
- v3 `task_2c0abf8f0254`/`ctx_870df428434b`는 failed 정산했다. 정정 수신 전 계약상 Stop 1회를 완료해 exact container `cd0ecec488960a9e388a034c7098641ba92f4c9462fb2b5070e272ac6ee6c364`를 exited로 보존했다. 보고 원문은 `runtime-entry-linux-sql-execution-3/report.md`이며 release·동일 incarnation/idle 확인·종료 완료다. Step Runtime은 미실행이다.
- 신규 Astra xhigh v4는 `SqlLifecycle.cs` 한 곳에서 sa-disabled의 18456/18470만 인정하고 실제 번호를 반환하도록 최소 수정한다. 순수 재현·새 DLL 빌드 뒤 동일 container 재시작 1회, sa-disabled 확인 1회→Runtime을 수행한다. Bootstrap/Prepare/Create 재실행은 없다.
- 메인 `msg_8984257dce85`는 기존 plan·소유·자원 사슬을 보존·검증하면서 변경 소스/DLL을 별도 v4 plan과 근거용 harness로 잇는 방식을 동의했다. 기존 owner/prepared/증거 덮어쓰기0, 보호 구자원과 Windows SQL 접근0, 입력/TLS/비밀/자원 관문은 유지한다. 계약·경로 검사·입력 hash는 `runtime-entry-linux-sql-execution-4/`에 둔다. 실제 Runtime·독립 강검증·게임 실행은 아직 미완료다.

### 독립 실검증 판정과 후속 완료 기준(2026-10-16)

메인 판단 `msg_9c601e45e190`(2026-10-15T17:12:33Z), 원문 `.backups/verification/2026-10-14-persistence-runtime-entry/runtime-entry-launcher-sdk-fix-1/main-expanded-decision.json`. 사용자 직접 입력으로 격상하지 않는다.

- 독립 판정은 BLOCK #1이며 원문 `runtime-entry-linux-sql-live-verification-1/verdict.md` SHA `3D436BBD2B36479F2F97013564D7AD9CBB9A7A4A37BB71AF74BE31BD82786C8E`다. 원제품 런처의 이름 dotnet 호출이 실패했다. 사본의 서버·봇·Play 관측은 원제품 PASS가 아니다. 메인 R-2는 catalog004 변경1건 및 실제 DB checksum/SQL literal 대조에 일치 판정을 전달했다. 검증자 정산·종료 완료.
- 런처 후속 최소 수정은 #1 `sdk.sh`/`DAWNHOLDER_DOTNET` 규칙의 절대 SDK 경로·버전 확인과 #2 자식 종료 감지 뒤 0이 아닌 종료를 포함한다. 순수 시험은 자식 argv와 즉시 종료 가짜 자식을 관측한다. 그 밖 런처 동작·제품 코드 변경은 없다.
- 신규 독립 Opus는 수정 쓰기 종료 뒤 한 명만 연다. 원제품 런처→서버/7777/SQL→같은 process 재접속 봇→정상 종료를 판정한다. 사본에서 실제 관측한 재시작 봇·release 경쟁·장면9는 메인 판단으로 반복하지 않는다.
- 추가 필수: origin/main 대비 PDL 기존 ID·필드 불변 및 새 ID 재사용0을 정적으로 확인한다. Unity 기존 EditMode 전체 및 PlayMode 로그인·직업선택·입장·파티·퀘스트 관련 시험을 batch1회 수행하고 필요 서버는 원제품 런처를 쓴다. Plugins Shared.dll/ClientNet DLL과 현재 소스 빌드 hash를 대조한다. 생성기 CRLF 부작용을 확인하며 미승인 제품 쓰기는 하지 않는다.
- DB 실패 입장 거부는 원제품 서버가 실행 중일 때 승인 exact SQL container를 Stop하고 저장 대상 계정 로그인1회→거부/메모리 대체0을 확인한다. 그 뒤 서버 정상 종료→Audit→Remove다. 기존 Stop 정리 순서를 앞당기며 재시작을 추가하지 않는다. 이미 멈춘 동일 컨테이너의 후속 시작1회는 기존 메인 승인 범위다.
- plan 만료 2026-10-16 10:47KST와 서버 인증서11:47KST 전 완료 순서로 진행한다. 맞추지 못하면 재준비하지 않고 상태를 보고한다. 위 수정·필수 검증 통과 시 「시연 경로 완료, 아래 항목은 동결 뒤」로 PR242 병합 승인 요청까지 진행한다. Codex 병합 금지/사용자 exact head 승인 관문은 유지한다.

### 동결 뒤 정리 — 메인 msg_9c601e45e190

다음은 메인 시연 우선 판단으로 10-28 동결 뒤로 미루며 이번 PASS로 바꾸지 않는다: DB 지연 주입 중 틱 비차단의 실제 서버 관측, Loading 중 입력 거부의 실제 서버 관측, release 전 재접속 거부의 Unity 화면 안내(O5), 5 RPC 전수 직접 관측(Checkpoint·Resolve 이번 호출0), hostname·만료 인증서 음성, 독립 판정 O1~O8. 원문 근거는 위 `main-expanded-decision.json` 4번이다.

Loading 입력 거부·DB 지연 비차단의 기존 서버 시험 이름/통과 근거는 후속 판정에서 좁게 찾아 연결한다. 현재 이 두 항목의 기존 시험 대응은 미확인이다. 존재하지 않거나 근거가 없으면 「미검증」으로 유지하며 새 harness는 만들지 않는다. 이번 실검증의 원시상 실제 DB 실패 주입·Loading 거부·화면 안내는 미실행이었고, 그중 DB 실패 입장 거부만 위 후속 필수에 포함한다.

### 최종 독립 판정 수용과 PR242 마감(2026-10-16)

메인 판단 `msg_6794d5227945`(2026-10-15T18:50:37Z), 원문 `R/main-final-decision.json`. 사용자 직접 지시로 격상하지 않는다. 아래 R/V는 재개 지점의 근거 경로다.

- 최종 독립 원문 `R/verdict.md` SHA256 `5186D37DFE32AA3EF2F0D3BE419A18738FFBCF603FBEB32310280BCD13F0250E`, 맥락 `R/context.md`. 판정 원문은 조건부 통과로 보존하고 메인이 남은 #3과 운영 편차를 비차단으로 수용했다. 메인 R-2는 17/17 XML·fixture diff·메모리 봇·SQL 자원 제거·커밋된 Plugins DLL을 대조했다.
- #1 SDK 경로와 #2 자식 종료 감지는 원제품 런처 실제 진입과 독립 입력 시험으로 해소. 원제품→DPAPI/stdin→자식 CA→서버/SQL→같은 process 재접속 봇→정상 종료 exit0. 저장 대상 DB 실패는 Unavailable·EnterMap 없음으로 관측했다. PDL 기존42개 이름/필드/순서 불변·새43~48, Shared/ClientNet 실제 빌드와 Plugins hash 일치다.
- #3은 메인 수용이다. NUL 문자 장치 입력은 지원하지 않는 입력이며 이전 즉시 정상 종료 exit0 대신 예외 exit1·자식 EOF가 적절하다고 판단했다. 「다른 런처 동작 변경0」의 명시 예외다. 콘솔·파일·pipe 보존은 확인됐다. NUL의 OBSERVED→Assert 변경은 동결 뒤로 남긴다.
- #4는 `msg_a18f8123ed9d` 조건에 따라 현재 검증자가 기존 `MapEntryPlayFixture.cs`만 +13줄 보완했다. 실제 서버의 유효 클래스/성공 응답을 따른 입력 보완이며 assertion·timeout 변경0. 리드·메인 diff 대조 완료. 같은 PlayMode17건이 7통과/10실패→17통과/0실패, EditMode 전체484/484 통과다. 메모리 모드 실제 서버+기존 봇 seed1회가 Success/Mage 뒤 EnterMap을 관측했고 정상 종료했다. 저장·재시작 검증으로 확대하지 않는다.
- 이전 V의 서버 전체1502건은1497통과/0실패/기존제외5. catalog checksum literal 한 줄만 수정한 전후 원시는 `V/raw/G/trx-compare.json`이다. 사본 런처로 한 과거 장면9·재시작 봇·release 관측은 메인 결정으로 재사용하며 원제품 런처 성공은 이번 R의 D 원시만 근거로 한다. 화면 HUD 직접 픽셀 관측 한계는 과거 판정 그대로다.
- 관찰 O1~O5는 R 원문 「설계 관찰」에 남긴다: 콘솔 echo, 시험 이름, runner 미등록, DB 실패 원인 로그, 메모리 seed 성공 문구. 이전 V O1~O8은 catalog 주석 위치, sa 오류18456 일반성, 옛 clone DLL 기동, snapshot 긴 경로, 거부 화면 안내 미관측, Free 상태 null 관측 경계, sa 비활성 후 ready, runner 미등록이다. 동결 뒤 후보이며 지금 고치지 않는다.
- Loading/tick 기존 시험은 `CharacterAdmissionLoadingGateTests.InputDuringLoading_ChangesNoAuthorityOrGameState` 및 `CharacterAdmissionTickTests.PendingReadAdmission_LeavesTheTickFreeForOtherWork`, `PendingAcquire_LeavesTheTickFree_AndTheResultEntersOnlyInsideATick` 등 16건이다. V의 실제 trx Passed 및 현재 소스 동일 hash 연결은 `R/raw/H/existing-tests.json`. 이 시험 근거는 아래 실제 서버 지연주입·Loading 관측을 대신하지 않는다.
- 메인 `msg_9c601e45e190` 4번의 동결 뒤 목록은 그대로 유지한다: DB 지연 주입 중 틱 비차단 실제 서버 관측, Loading 중 입력 거부 실제 서버 관측, release 전 재접속 거부 Unity 화면 안내(O5), 5 RPC 전수 직접 관측(Checkpoint·Resolve 이번 호출0), hostname·만료 인증서 음성, 이전 독립 판정 O1~O8. 위 「동결 뒤 정리」와 함께 미실행 상태다.
- 운영 편차8건과 허용 밖 임시쓰기 `R/tmp/dbfail-state`, `.backups/tmp/lfv/unity`는 R 판정 「운영 편차」「결론」에 공개돼 있다. 메인은 Git 밖 근거·임시 폴더라 시연 제품 판정을 막지 않는다고 판단했다. 삭제하거나 위반 없었던 것으로 소급하지 않는다. 새 SQL 자원은 Audit/Remove 완료, 서버·Unity·7777 잔존0 원시는 `R/raw/G/after-remove.txt`, `R/raw/M/after-shutdown.txt`다.
- 노트북 SQL Express 설치 도구 사용자 결정 `msg_9fa3df979283`은 위 적용 결정대로 기록만 유지한다. 이번 PR 뒤 새 goal 범위가 전달되기 전 자동 착수하지 않는다.
- 로컬 Changed `--base origin/main`은 `.backups/code-rules/run-2026-10-15T18-51-57.016Z-37568/results.json`에서 PS 대상9개 중7파일 서식38건으로 exit1. 첫 실행은 예시 모듈 경로 부재로 미실행이고 별도 원시를 보존했다. 규칙 검사·새 head CI와 시연 동작 판정을 구분하며 `merge_ready`는 아직 아니다.

### 들여쓰기 한정 교정과 기계 증명(2026-10-16)

메인 `msg_a9a84b992869`는 Changed 실패를 수용하지 않고 신규 Astra xhigh 한 명에게 7파일의 줄앞 공백만 교정하도록 했다. 새 독립 검증자는 열지 않으며, 시연 우선 재검증 상한 뒤 좁은 수정 판단으로 토큰 동일·같은 Changed·기존3시험을 기계 증명으로 삼았다. 원문은 `B/runtime-entry-indentation-fix-1/main-decision.json`이다.

- 작업 `task_04cb3aa58fa7`/`ctx_55eebead5c86`의 보고·맥락 전문과 `raw/proof-summary.json`, 전후 proof JSON, 현재7파일 SHA·선행공백 제외원문, 실제 시험 JSON을 리드가 대조했다. 38행만 교정, 토큰12233개·문자열AST2530개·줄순서/개행/BOM 차이0. 런처의 새 SHA는 `1C16C6D1786635967D4532462A1AA8A8F43A34C0839625C77FE0C9DB2EE3328C`이며 원제품 실제 진입 당시 SHA와의 차이는 공백뿐이다.
- 같은 Changed `--base origin/main`은 `.backups/code-rules/run-2026-10-15T19-00-27.489Z-9448/results.json`에서 대상9/위반0/실패0/exit0. 오프라인 Boundary47통과, Input6통과+NUL1관측, Sdk5통과로 이전 개수와 같다. 제품 동작/시험 기대 변경0, SQL·Unity·게임 재실행0이다.
- 근거 `B/runtime-entry-indentation-fix-1/{report.md,context.md,lead-source-check.json,raw/completion-summary.json}`. 보조 감사의 Changed/Boundary 과거 자식 이력 일부 누락은 UTC 비교 오류로 공개했으며 Input/Sdk와 마지막 잔존0·Boundary journal4개로 보완했다. 생애전수 반환 증명으로 확대하지 않는다. 작업자는 쓰기 종료·release·동일 incarnation/idle 확인 후 종료했다. heartbeat350초1회는 검토/보고 구간으로 기록한다.
- 「검증자의 서식 지적 없음은 규칙 검사 통과를 뜻하지 않았다」는 관찰을 남긴다. 다음 검증 계약에 Changed 실행을 명시하는 것은 동결 뒤 후보이며 이번에 운영 정본을 임의 수정하지 않는다. 서식 기계 증명은 자체점검과 리드 원시 대조이며 신규 독립 판정으로 부르지 않는다.

### C# 공백 교정·로컬 한계와 원격 판정 결정(2026-10-16)

메인 msg_7327a57f1fff의 신규 Astra 작업 task_918b81c34dbd/ctx_2330ae93808b는 HostPlan.cs·SqlLifecycle.cs 두 파일의 17곳만 교정했다. 직전 head a184835d25ab44b78281894722558cf3cd2d545f의 dotnet-tests는 WHITESPACE17건으로 실패했고 다른3CI는 성공했다. 최초 보고18건은 오기이며17건으로 정정했다.

- 근거 C는 B/runtime-entry-csharp-whitespace-fix-1이다. C/report.md·context.md 전문, 실제 diff(+28/-11), raw/preservation.json의 Debug/Release 토큰·문자열·주석·directive 보존 및 현재2파일 SHA를 리드가 대조했다(C/lead-source-check.json). 신규 독립 판정이 아니라 메인의 기계 교정 예외에 따른 자체검사와 리드 실사다.
- 원제품 full format 재실행 exit0, Formatting.Tests244/244·420개C# 보존. Changed 대상9/위반0/실패0, Host restore/build0(기존 IDE0011 경고4), module production 위반0·회귀34실행/60skip, Architecture199중93pass/103skip/3expected failure exit0. Orca72/72·MergeGate126/126 성공. 각 명령·환경·종료코드·원시는 C/report.md 실행표 및 raw/*-command.json에 연결한다.
- 최초 fullformat의 DrvFs 비실행 fixture1실패는 승인된 native Linux TMPDIR 재실행1회로 해소했다. Architecture Git Filename too long 오류12건은 원형 probe stderr 확인 뒤 짧은 Windows TEMP의 승인 재실행1회로 해소했다. 최초 실패 원시는 보존하며 재실행으로 덮어쓰지 않았다.
- CodeRules23중20pass/3fail은 Windows 로컬 환경 한계, 미해결이다. PSSA8.3 alias/canonical 경계2건은 환경 실패, TypeScript ERR_PACKAGE_IMPORT_NOT_DEFINED 1건은 원인미확정으로 남긴다. 경로 길이 인과는 입증하지 않았다. 원시 C/raw/FCode-stdout.txt·code-failures.json. SessionGuard167중165pass/2fail은 junction 실제 대상의 memo-first/write-outside-checkout 거부이며 원인미확정이다(C/raw/FSessionGuard-stdout.txt·session-guard-failures.json). 합계 미해결5건이지 통과가 아니다.
- SessionGuard 실행은 중단 지시 msg_59e709d50097(20:26:08Z) 응답을 읽기 전에 mailbox check와 다음 실행을 한 도구 호출에 연속 배치한 운영 편차다. PID39092, 시작20:26:39.5218942Z·종료20:26:45.0417210Z/exit1. 정확 수신시각은 미기록이나 다음 wrapper birth20:26:34.3357903Z 이전 check 반환이며 수신 전 시작으로 소급하지 않는다. Backlog 미실행. 사전 lock probe 누락 뒤 B선행 실행 편차와 승인 경위는 C/raw/operational-deviations.md에 보존한다.
- 메인 msg_3be15baefcea 및 msg_8b2ef7504a1e는 도구 시험에 영향 없는 C#공백 교정임을 근거로 추가 진단·재시도0, 로컬 실패·편차를 동결 뒤 정리 후보로 남기고 commit/push 후 새 head 원격4CI로 판정하도록 명시했다. SessionGuard의 한 번 실행은 실패한 시험2건을 포함한다. 실패를 통과로 바꾸거나 편차를 소급 승인하지 않는다.
- C/raw/final-audit.json은 tracked3328 중 승인2파일만 변경·의도밖drift0·보호1117불변이며 리드 현재 diff/hash와 일치한다. process-return-audit.json은 기록된 최상위 PID/birth 잔존0·현재Linux소유후보0, 생애 후손 전수추적은 아니다. 작업자는 outcome=failed로 정산했고 release→동일incarnation653f9b1b-e15f-45fc-b0cf-1a80eddf7f0a/idle확인→pane종료했다.
- 서버1502건은 소스 동일 근거로 재사용했고 이번 재실행이 아니다. SQL/Unity/게임 재실행0, 이전 독립 판정과 메인 수용을 유지한다. 다음은 새 head 원격4CI이며 사용자 exact head 승인·메인 병합은 미완료다.
