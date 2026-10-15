# 실제 DB 연결·로딩 게이트·재접속 직업 복원

상태: 기존 11시험 수정과 S1~S4 메모 대조의 독립 강 재검증 범위가 통과했다. 무필터 전체 시험은 1,469통과·실패0·건너뜀5다. 다음 첫 일은 Windows DLL 반영·Unity 양쪽 소비자 검증과 실제 경로 관측이다. 실제 DB/DPAPI·정상 서버·봇/CI 및 최종 사용자 병합 승인은 남아 있으며 전체 goal 완료는 아니다.

## 진척 단계

- [x] 목표·경계 승인
- [x] 공통 계약 확정
- [x] 선행 시험 실행
- [x] 저장·입장 구현
- [>] 독립 검증
- [ ] 실제 경로 관측
- [x] PR 생성(draft)
- [ ] 통합·병합
## 적용 중인 사용자 결정
메인 msg_fcb2a973fede가 전달한 사용자 원문(메인 pane Enter 제출, 2026-10-14T14:44:08Z/23:44 KST): 「그리고 영속성 부분은 너가 더 전문가니까 Goal 초안 작성하고 진행까지 계속 진행해줘, Core쪽 진도를 좀 빠르게 뺴보자」. 메인 전달이며 이 pane의 직접 사용자 입력으로 격상하지 않는다. 시각은 정정 msg_d8a5ccb79390을 적용했다.
메인 초안 C:/Dev/DawnHolder_Dashboard/main-notes/2026-10-14/core-next-goal-draft.md SHA055A04FF697EC14A0DF17B4D1656777D8F54717BC04ACE5135CAC03A18DFBCDE와 리드 초안 lead-next-goal-draft.md SHAF9A38BBEC62A4273F9FF5E9F3812C813396F77BAE92A6FF36B36EAAC2B586D1F를 통합했다. 원문은 .backups/verification/2026-10-14-persistence-runtime-core/lead-next-goal-approval-source.json에 보존한다.
10-28 기능 동결까지 시연 우선 msg_beda86f6bc2f, 리드/새 Sol배정 작업자는 Sol xhigh인 msg_18c90b33c9a1을 적용한다. 비동작지적은 동결뒤 정리, 운영미준수는 공개·제품비차단, 독립재검증1회뒤차단이면메인판단이다. 동작결함/위장시험·측정/보호경계·구현검증분리/실제실행·정확head 사용자병합승인은 유지한다.

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
- G2 실행과 이 goal의 DB 실행은 겹치지 않는다. Unity 실행은 기존 순서(Art→World→Content) 뒤에 Core 차례로 한다.

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

1. 지금 일(A/B 통합 검증, SQL #1 수정→재실사→G2)을 먼저 끝낸다. 이 goal의 문서 작업(goal.md, 공통 계약)은 병행해도 된다.
2. A/B 검증 통과 뒤 선행 시험 작성자와 writer 둘을 연다. DB를 쓰는 실행은 G2가 끝난 뒤다.

## 동결 뒤 정리
- 계정별 영속 slot/스키마 확대는 이번범위밖후보다. 실제착수 때 BACKLOG의 기존후보와대조해중복없이기록한다.
- 독립판정의 비동작지적은 원번호/근거와 함께 여기에 인계한다. 현재확정항목0이다.

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
