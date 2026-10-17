# 재접속 직업 복원의 공통 계약 v2

[Core 리드 Sol]. 승인 goal/Main msg_1d6c04a97368의 결과 상태·패킷·파일 소유 계약이다. 기준은 PR240 `f4d80d256a3041acd15624d804300157be05548b`, 새 branch `feat/persistence-runtime-entry-20261015`의 첫 goal commit `6e398abe85dc3a5f486738c0e4a3ae6413f464c3`이다. 서버/프로토콜/Unity 실제 동작은 아직 구현·실행되지 않았다. 기존 A/B의 결과 코드·receipt/authority/token·unknown/close 계약은 그대로다.

## 패킷과 공개 결과

`S_CharacterLoadResult`는 ID **48**, 고정 전체 길이 **6 bytes**(LittleEndian ushort size + ushort id + byte result + byte characterClass)다. 공개 enum은 `Shared.Protocol.CharacterLoadStatus`: Success=0, Busy=1, RecoveryRequired=2, Unavailable=3. C_CharacterSelect의 Knight=0/Mage=1 의도 값은 유지한다. 성공의 characterClass는 실제 서버 권위 Knight0/Mage1, 실패에서는255(미지정)다. GUID·owner/fence/token·SQL reason/비밀/원문 예외는 보낼 필드가 없다.

| 공개 결과 | 서버가 확인한 조건 | 진입/클라이언트 동작 |
|---|---|---|
| Success(0) | 저장 대상: 현재 권위를 확인한 acquire 완료의 저장 class. 비저장 계정: 유효 메뉴 class | LoadResult → EnterMap → full HP → roster, 기존 Ready barrier 뒤 플레이 |
| Busy(1) | 관측된 다른 현재 owner·release/handoff 충돌 또는 acquire Conflict | 월드 진입0, 잠시 뒤 재접속 안내. 자동 takeover/retry0 |
| RecoveryRequired(2) | recovery owner/복구 필요 상태를 내부 계약으로 확인 | 진입0, 복구 필요 안내. recovery RPC/관리 권한 노출0 |
| Unavailable(3) | admission 읽기 실패·NotDispatched/Rejected·unknown/Unresolved·불완전 증빙·기동/저장소 오류 | 진입0, 저장 데이터 확인 실패 안내. 메모리 fallback0 |

보존할 판정: ReadAdmission의 Probe는 입장 권위가 아니다. historical Acquired나 `Code` 하나로 성공을 만들지 않는다. 실제 current Runtime owner/OwnerId/fence·sequence0·두 token이 receipt와 일치한 현재 acquire만 Success다. 전송/취소 예외는 “DB 미실행”으로 재분류하지 않는다. 실측되지 않은 owner 상태는 Busy나RecoveryRequired로 추측하지 않고 Unavailable로 닫는다.

패킷은 저장 대상이 아닌 계정에도 동일하게 보낸다. 실패 길이/unknown result/Success의잘못된class/실패의잘못된255형식은 클라이언트가 권위 상태·spawn·입력을 변경하지 않고 거부한다. 성공 중복은 같은 lifetime/class이면 멱등하게 처리하고 다른 class·닫힌/지난 connection의 결과는 적용하지 않는다. UI 문구는 고정된 짧은 안내이며 DB 상세를 출력하지 않는다. 올바르지 않은 클라이언트 CharacterSelect 입력은 기존 silent drop 규칙을 보존한다.

## ID 43~47과 판 번호

World 예약 인정은 msg_9242ab697902, 고정 working tree source 제공은 msg_c48ebefa9ccb다. World PDL SHA `0EA439BC5D0AD8CBE85E11C24FF74570AB50EA28010E8D4CAFB277F2F3411C99`, 미커밋 `feat/guild-server-20261014`/HEAD `c8ea1890785acb8feeb560e6bc16732b6e838f4b`는 source 식별 근거이며 HEAD에 정의가 있다는 뜻이 아니다. 스냅샷은 N/inputs/world/PDL.xml, 대조는 N/lead-world-pdl-source-audit.json이다. Core 42개와 World 앞42개 의미 불일치0, World43~47은 C_GuildRequest/S_GuildSnapshot/C_StrongholdUpgrade/C_WeaponEnhance/S_GuildActionResult다.

현재 generator는 순서 자동 배정이다. Core 42개 끝에 바로 LoadResult를 붙여43을 차지하지 않는다. writer2는 **고정 World source의43~47 정의를 필드·순서 그대로 유지한 선행5개**와 Core48을 단일 PDL에 둔다. 이 전제는 World의 새 서버/Unity 기능을 대신 구현할 권한이 아니다. source drift는 리드와 World에 반환하고 임의 변경0이다. Core 작업용 판은 기존v19/Worldv20과 같은 번호로 다른 schema를 내보내지 않도록 잠정21, 최종판은 Main 병합순서/후행 rebase에서 다시 확정한다. 앞42개 ID/byte 계약과 World43~47 field/ID는 보존한다. generator의 explicit-id 신기능이나 placeholder packet을 추가하지 않는다. 이 선행 정의 인계는 Main/World에 기술 조율로 통지한다.

## 저장 대상 설정 한 곳

writer1이 Persistence의 **한 설정 지점** `PersistenceOptions.PersistedAccountName`을 받는다. 기존 `Shared.GameData.AccountNameRule.TryNormalize`의 길이3~16/ASCII소문자·숫자·underscore 규칙과 Accounts의 기존 `AccountIdResolver.Resolve`(UUIDv5)를 재사용한다. Accounts·AccountRegistry 코드는 readonly다. 정규화한 이름의UUIDv5는 Options.AccountId와 같아야 기동이 성공한다. AccountId/CharacterId는 nonempty, SlotId=1, exact endpoint/DB·5초 연결/명령·2초 lock·pool4 등 technical-spec2/7절을 지킨다.

`PersistenceAccountSelector`는 설정을 동결해 `NormalizedAccountName`, `AccountId`, `IsPersistent(Guid accountId)`를 제공한다. 같은 계정의 로그인 중복 거부는 기존 AccountRegistry다. 설정 이름이 아침에 바뀌면 이한지점/정확바인딩값만 바꾸며 모든 handler/Unity에 이름을 분산하지 않는다. 불일치/없는 설정을 다른 계정이나 memory 저장으로 대체하지 않는다. 테스트용/기존 비영속 기동 경로가 persistence를 주입하지 않는 경우는 명시적 비저장 경로이며, 설정된 저장 대상의 실패 fallback으로 사용하지 않는다. 실제 대상 이름은 아침 사용자에게 다시 보여 줄 값이므로 여기서 임의 사용자 이름을 확정하지 않는다.

SQLwriter가 공개 설정/정규화선택자/options·runtime factory를 제공하고 admission writer가 readonly로 소비한다. repository는 options와 connection factory만 의존하고 GameSession/DPAPI/환경변수/전역 singleton을 읽지 않는다. 서버기동은 credentials를stdin/익명pipe로 받고 저장소에 password를넣지 않으며, 기동 DB/schema/slot/GUID gate를 listening전에 수행한다. 실제 DB gate 실행은 이번 오프라인 단계 승인밖이다. TLS 기본은 Mandatory/TrustServerCertificate=false이고 self-signed 예외는 별도 명시 승인 없이는 선택하지 않는다.

## 서버 입장·owner 출력·종료

상태는 HandshakePending → Selecting(로그인 완료) → Loading → Resolved → Entering → Active다. session generation/closing이 모든 지연 결과 적용을 제한한다. 현재 `_stats`/HasSelectedClass와 `_worldEntryRequested`를 권위 완료로 오인하지 않게 구분한다. CharacterSelect 의도는 한 번만 접수하고 Loading에서 중복 선택·move/attack/skill/portal/party/cheat/economy 입력은 상태를 바꾸지 않는다. Repository 기다림/Task.Result/Wait를 게임틱 안에 두지 않는다. IOCP 결과는 게임 상태를 직접 변경하지 않고 현재 owner tick에 큐로 돌린다.

A/B `PersistenceSubmission.Completion`은 현재 `PersistenceWorkResult.Status`만 노출하고 stored class가 없다. admission이 owner의 private field/동일 RPC 재호출/다른 GUID 생성으로 우회하지 않도록 writer2가 **immutable `PersistenceWorkResult.LoadedClass`(nullable CharacterClass)** 출력 한 접점을 추가한다. current acquire가 owner의 기존 검증을 통과해 Status=Acquired로 확정한 경우에만 값이 있고 다른 work/실패/이력-only에는null이다. `CharacterPersistenceOwner`가 token/authority 검사를 먼저 하고 이값을 한 번 publish한다. 기존enum 숫자·receipt의Code·owner/fence/queue/deadline/unknown분기·close 책임은 바꾸지 않는다. 이 좁은 접점이 저장 class를 기존 `_stats`에 안전하게 전달한다.

비저장 계정은 DB/owner RPC0이며 메뉴class로동일 Success 패킷을보낸다. 저장계정은 owner의 현재 LoadedClass가 메뉴보다우선한다. 유효class를서버PlayerStats에적용한뒤에만Town의기존안전spawn/최대HP/roster로진입한다. 기존client Ready barrier의 scene/player/registry/HP readiness를보존한다. Unity는전connection load result의class override를끊을때초기화하고ClassLoadout 소비부가현재서버값을쓰도록한다. scene/prefab 재저장은승인밖이다.

닫힌세션의늦은성공은packet/spawn에적용0이지만owner가진행중acquire 증빙/조건부release를계속종결한다. map/entity/partycleanup은DB를기다리지않는다. EOF/Host종료 capture는틱에서immutable CloseCaptureTicket을한번만만든다. 변한것없으면checkpoint=null/NoWriteNeeded(CheckpointRPC0), owner조건부release만접수한다. 변한 safe projection만checkpoint를전달한다. Host종료는기존기본2초를전체monotonic budget으로공유하고단계마다예산을새로주지않는다. Unresolved/RecoveryRequired/ShutdownIncomplete는Released로보고0이다.

## 두 writer 소유권

| owner | 쓰기 범위 | 다른 쪽에 주는 접점 |
|---|---|---|
| writer1(SQL/config/secret) | Persistence/Sql 새구현, PersistenceOptions.cs·PersistenceAccountSelector.cs·기동factory 새파일, Hosting의설정/기동과ServerHost.cs·Program.cs·GameServer.csproj, 새launcher/공개설정예시 | 고정options/selector/ICharacterRepository factory·startupgate·종료시admission이제공하는close완료Task를기다리는Host접점 |
| writer2(admission/packet/client/bot) | PersistenceWorkResult.cs·CharacterPersistenceOwner.cs의LoadedClass출력만, Persistence/Admission 새코드, GameSession.cs·CharacterSelectHandler.cs·GameWorld.cs의입장/close접점, PDL/ProtocolVersion/생성코드·서버/클라등록, Unity Network/Bootstrap와직접ClassLoadout소비부·meta, 봇Persistence시나리오 | session/owner lifecycle·readonlyloadedclass·GameWorld의closeflush진행/완료접점 |
| 선행/독립 Opus | 신규 server/protocol 시험, 이후 필요Unity순수단위시험+meta | 제품수정0. 신규시험범위/메모/RED·동일GREEN원시 |
| 리드 | goal/api-contract·실행/위임문서·근거·Git | 공통계약/소유권·현재조율·Main판단 |

개별 파일 목록·새타입 상세signature는writer계약에서기계확인해같은파일동시쓰기0으로고정한다. GameWorld는writer2,ServerHost/Program은writer1이며각자의readonly접점을먼저문서로대조한다. 기존A/B의다른DTO/ownerbehavior·Accounts·SQLmigration/procedure·Worldsource는readonly다. 새의존성Microsoft.Data.SqlClient6.1.7은승인값이고병합요청때새의존성으로명시한다. 새패키지판이나영구/전역설정은추가하지않는다.

## 선행시험 경계와 실제 실행 미허가

Opus는이계약과existingICharacterRepository fake를써서Loading입력거부/지연중틱비차단/비저장RPC0/메뉴와저장class차이/lateclose/no-write release/hostdeadline·패킷48 직렬화/기존ID·Worldfield보존을요구별RED로작성한다. 아직없는타입은계약의public필드/메서드contract를반영하되테스트에서제품계산을복제하지않고제품stub을작성하지않는다. 새생산자/SQLDB/UnityEditor·Player/봇연결·7777는실행0이다. RED서버build/test·generator는G2#1수정/독립검증의중량반환뒤현재fresh/공식ask를받아1회범위로실행한다. 선행시험writing은병행가능하고part독립검증자는동시1이다.

실제DB/Unity/같은process·processrestart봇관측은G2 새수명과사용자아침결정뒤다. SQL/프로토콜신규독립강판정은코드/테스트·실제진입범위를구분하며오프라인성공을scene9실증으로격상0이다. 같은산출물재검증1회상한, 비동작가독성/문서지적은동결뒤·측정위장/제품계산복제/권한/현재authority/기존packet·DLL/GUID경계는유지한다.

## 원천

approvedgoal/Main msg_fcb2a973fede·msg_1d6c04a97368, technical-spec2/6/7절·기존A/Bapi-contract, CharacterPersistenceOwner.cs(제출/검증/close)·PersistenceWorkResult.cs(현재출력없음), GameSession.cs:494~586(Load→EnterMap/HP/roster기존순서), MapEntryCoordinator.cs(Ready barrier), AccountNameRule/AccountIdResolver(정규화/UUIDv5), World43~47 frozenPDL와N source-audit를읽어선택했다. 이문서는소스인터페이스인계이며구현코드나검증판정이아니다.

## 선행시험과 두 writer가 공유할 최소 타입 접점(v2)

새 제품 타입을 시험 작성자가 임시로 구현하지 않는다. 아래 이름/signature는 요구 contract이며 제품은 두 writer가 만든다. 이미 존재하는 GameSession/ServerHost 생성 사용처는 optional injection으로 호환을 보존한다. 아직 없는 접점의 RED는 부재/불일치 실패로 명시하며 실제 Loading 동작을 관측했다고 과장하지 않는다. 테스트는 필요시 reflection으로 기존 타입에 연결해 compile 부재와 행동 실패를 분리한다.

- writer1 `PersistenceAccountSelector(string configuredName, Guid accountId)` / `NormalizedAccountName`, `AccountId`, `bool IsPersistent(Guid accountId)`. 정규화한 configuredName의 기존UUIDv5와accountId 일치/empty·invalid거부는 constructor/config gate에서 보장한다. AccountNameRule/AccountIdResolver 제품쓰기0이다.
- writer2 `CharacterAdmissionServices(PersistenceAccountSelector selector, PersistenceIdentity identity, ICharacterRepository repository, TimeProvider timeProvider, int queueCapacity=4)`는 한 고정 저장 주체의 owner/admission/close lifetime을 소유한다. repository에 owner/세션을 역주입하지 않는다. 생성만으로 DB를 열지 않는다.
- writer2 `GameSession(GameWorld? world=null, CharacterAdmissionServices? persistence=null)`의 기존첫인자는보존한다. 주입없음은명시적기존비저장경로다. `internal CharacterAdmissionState AdmissionState`는관찰가능한현재Loading/Resolved/Entering/Active/Rejected/Closed를보여주며wireenum으로보내지않는다. 타입의internal구현숫자는시험에서단정하지않는다.
- writer2 `GameWorld.AttachPersistence(CharacterAdmissionServices services)`는Start/listen전에한번만주입하고readonly한서비스를session에전달한다. 시작뒤/중복주입은거부한다. writer1 Program/Host는이접점만호출하고GameWorld.cs를쓰지않는다. `Task GameWorld.FlushPersistenceClosuresAsync(PersistenceDeadline deadline)`는틱에서잡힌최종capture 이후owner책임완료를보고하며대기중tick을막지않는다. Host기계wait는틱밖에서한전체기한만쓴다.
- writer2가주는 `PersistenceWorkResult.LoadedClass`는 기존 Status=Acquired일때만 current확정값이다. DB receipt/token이아니며session에추가SQL호출/ownerprivate복제를요구하지않는다.

이접점은다른저장주체·관리복구·추가플러그인대상을열지않는다. source를읽어signature가보존조건을만족못하면위임중조용히갈라쓰지말고리드에한줄대안/영향을올린다. SQLwriter와admissionwriter가같은파일을쓰기전에v2기준을대조한다.
