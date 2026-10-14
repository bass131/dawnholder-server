# 실제 DB 연결·로딩 게이트·재접속 직업 복원

상태: 메인 msg_1d6c04a97368 지시로 오프라인 2단계 착수. branch feat/persistence-runtime-entry-20261015, 기준 PR240 head f4d80d256a3041acd15624d804300157be05548b 위에 goal을 첫 기록으로 둔다. 공통 결과/패킷 계약·ID48 예약 확인 → 신규 Opus 선행 시험 → RED 뒤 신규 Sol xhigh 두 writer 순서이며 아직 작업자 발행/제품·시험쓰기0이다. G2 #1 오프라인 수정·독립 검증을 우선하고 중량 실행은 순차, DB·Unity·봇 실관측/새SQL자원/보존자원 접근0이다. 저장 대상 계정 설정은 아침 사용자에게 다시 보일 결정이다.

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
