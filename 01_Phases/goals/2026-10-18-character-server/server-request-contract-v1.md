# 서버 메뉴 요청 경계·수명 계약 v1

Main `msg_4dc4f7f5bdd5`의 사용자 위임 결정에 따른 첫 Core 독립 시험 묶음이다. [전송 v1](packet-contract-v1.md)과 [Core 소비 v1](core-consumer-contract-v1.md)의 의미는 보존한다. Core 구현/타입을 복사하지 않으며 번호49..54/protocol22를 새로 배정하거나 생성하지 않는다. 기존 전송217/Unity313 자체 GREEN과 구분한다.

## 1 - 실제 서버 요청 경계

실제 `GameSession.OnRecvPacket → HandlerRegistry → IPacketHandler.Handle`를 통과한다. 소켓 송신만 기록하고 실제 handshake/login/world tick을 기존 시험 fixture로 준비한다. HandlerRegistry의49/51/53 등록과 `RequiresSelectedClass=false`를 확인한다. unknown drop 때문에 malformed 시험이 우연히 통과하지 않도록 등록/정상 안내 응답의 양성 대조를 둔다.

- 정상 완전 프레임은 List49/Create51/Select53이며 각 응답50/52/54의 requestId를 보존한다. header 포함 길이·offset·reserved0·UTF16LE byte prefix/짝수/상한/정상 surrogate·정확 소비를 큰 문자열/업무 호출 전에 검사한다. 4바이트 미만을 실제 GameSession에 직접 넣어 framer 책임을 handler 결함으로 만들지 않는다. 프레임 내부의 길이 불일치/끝 잉여/잘림은 검증한다.
- 구조 위반과 requestId0은 응답0·상태/등록 변경0이다. 구조가 깨진 높은 ID는 high-water를 소비하지 않으며 뒤의 낮은 정상 ID를 막지 않는다. frame 생성/기대 byte는 고정 literal/전송 v1에서 오며 제품 serializer 결과를 oracle로 복사하지 않는다.
- handshake 전 메뉴는 기존 first-packet 정책으로 종료하고 메뉴 응답/등록0이다. handshake 뒤 로그인 전 정상 메뉴는 InvalidState3/field0/실패빈필드이며 플레이어/경제/길드 등록0이다.
- 로그인 뒤 미선택·명시 Legacy인 기존 `new GameWorld` 경로의 정상 요청은 LegacyFlowOnly16/field0/실패빈필드다. 프레임이 정상일 때 모드/상태 판정은 사용자 값/Core prepare보다 앞선다. 이번 Legacy 사례 입력은 유효 slot/class/name/ID를 써 경합 우선순위를 추정하지 않는다.
- 같은/낮은 nonzero ID는 종류/내용을 바꿔도 응답0이다. 정상 구조·유효 종류의 새 높은 ID는 상태/모드 거부를 포함해 관측된 값으로 남긴다. malformed/0은 제외한다. 새로운 연결은 새 high-water로 시작하고 uint 끝에서 wrap하지 않는다.
- 선택돼 Entering/Active인 세션의 메뉴는 InvalidState3/field0이며 기존 class/player/Guild를 교체하지 않는다. 종료 뒤 메뉴 요청은 응답/새 등록0이다. 기존 Legacy C_CharacterSelect16의 Knight/Mage→권위48→실제 입장/기존 Guild 동작은 보존 대조다. 새 메뉴 안내만으로 class/입장/seed가 생기지 않는다.
- 실패 응답 literal은 전송 v1 그대로다. 50은26bytes/mask0/빈3슬롯,52는16bytes/slot0/빈ID·name/class0,54는12bytes/빈ID다. 입력 결과3/16 이외 값을 이름 오류로 추정하지 않는다.

## 2 - World 요청 수명 객체

소유 책임은 한 연결의 requestId high-water·단일 pending·서버 token·완료/close 검증이다. storage mode/catalog/이름 정규화/OperationId/SQL/host recovery를 구현하지 않는다. 모든 연산은 세션 owner의 직렬 큐에서 호출하며 이 계약은 동시 호출의 lock 정책을 추가하지 않는다.

제품 타입은 `Dawnholder.Server.GameServer.Sessions.CharacterMenuRequestLifetime`이다. 신규 타입 부재는 reflection assertion으로 명시 실패시켜 현재 시험 어셈블리의 컴파일을 막지 않는다. public 또는 internal 접근은 허용하며 테스트 전용 product hook은 추가하지 않는다.

| 접점 | 고정 형태/의미 |
|---|---|
| 생성 | 인자 없는 생성자. 새 연결은 pending/high-water 없음, open |
| `Begin` | `(uint requestId, ushort requestPacketId, PersistenceDeadline deadline)` → `CharacterMenuRequestBeginResult` |
| begin result | get-only `Status`(이름 Started/Ignored/InProgress/Closed), `Ticket`(Started에서만 non-null) |
| ticket | `CharacterMenuRequestTicket`: get-only RequestId:uint/RequestPacketId:ushort/Generation:long/Deadline:PersistenceDeadline. 생성은 Lifetime만 소유하며 외부가 임의 구성하지 않음 |
| `IsCurrent` | `(CharacterMenuRequestTicket ticket)` → bool. 같은 lifetime의 현재 pending token만 true |
| `Complete` | `(CharacterMenuRequestTicket ticket)` → bool. 현재 token일 때만 pending을 해제하고 한 번 true |
| `Close` | 인자 없음. 멱등·영구 closed, pending token은 모두 무효 |

`PersistenceDeadline`은 이미 main에 있는 monotonic 타입을 그대로 사용한다. Core v2 Catalog 타입은 필요하지 않다. 결과/상태의 enum ordinal을 가정하지 않고 위 이름/의미로 검사한다.

1. 요청 종류49/51/53, nonzero ID, non-null 원 deadline을 받는다. 유효하지 않은 종류/0은 Ignored/ticket null, high-water/pending 불변이다. null deadline은 ArgumentNullException이며 상태를 바꾸지 않는다.
2. open에서 이미 관측한 ID 이하이면 Ignored다. 정상 새 높은 ID는 high-water를 올린다. pending이 없으면 Started이며 새 token/양수·이전보다 큰 Generation/원 ID·종류·**동일 deadline 객체**를 보존한다.
3. pending 중 높은 새 ID는 InProgress/ticket null이다. 원 pending token의 ID/종류/Generation/deadline·IsCurrent가 그대로다. 거부된 높은 ID도 재실행되지 않으며 원 요청 완료 뒤 더 높은 ID로만 새 요청을 시작한다.
4. 원 token의 Complete는 한 번만 true, 중복/오래된/다른 lifetime token은 false이며 새 pending을 지우지 않는다. 같은 숫자 ID/Generation을 가진 다른 lifetime의 token도 current가 아니다.
5. Close 뒤 IsCurrent/Complete는 false, Begin은 Closed/ticket null이고 다시 열리지 않는다. Close/늦은 Complete가 새 lifetime을 오염시키지 않는다. uint.MaxValue를 소비한 연결은 작은 ID로 wrap하지 않는다.
6. 대기·새 요청 거부·Complete는 원 deadline을 갱신하지 않는다. 가상 clock으로 경과/만료와 원 object/remaining을 관찰한다. 이 객체는 timeout 완료/Unknown 송신/DB 취소를 자동 수행하지 않는다. 원 token은 owner가 결과/timeout을 정산하기 전까지 pending이며 만료를 새 예산으로 바꾸지 않는다.

테스트 namespace의 작은 probe interface는 reflection으로 **제품 연산을 호출하는 시험 driver**만 표현할 수 있다. 그 안에 high-water/pending/close 계산을 구현하거나 제품 타입 부재 때 가짜 구현으로 대체하지 않는다. 이는 Core Catalog 대역 interface가 아니며 Core API v2의 가짜 namespace/constructor를 만들지 않는다. 향후 Catalog 대역은 그 공개 형태를 별도 좁은 접점으로 연결한다.

## 3 - 실제 실행·보존과 다음 묶음

신규 외부 선행 작성자는 위 두 경계와 기존 Legacy/login/전송 회귀를 준비한다. 파일 쓰기·정적 확인 뒤 실제 RED/발견수/전실패 분류는 새 메인 실행창의 동일 WSL 필터에서 측정한다. 제품 없는 World 타입/handler 부재와 뒤 semantic 미도달, 기존 보존 통과를 구분하며 skip/compile실패/발견0을 RED 성공으로 보고하지 않는다.

Stored mode의 실제 bridge/기존16 우회 금지, 목록/생성/선택 Core 응답 대역, 원 descriptor/operationId·Unknown/handle close,54→48→입장→Guild/account seed와 틱 I/O 없음은 다음 World 대역·실제 통합 묶음에 남는다. 이 계약으로 전체 C1~C9를 축소하지 않으며 두 경계의 성공을 그 미실행 근거로 쓰지 않는다. 이후 시험을 Core 인계가 없다는 이유만으로 중단하지 않고 공개 API 문서에 맞는 대역 접점을 이어 준비한다. 최종 실제DB/저장재접속·양쪽 소비자/전체 독립강은 별도다.
