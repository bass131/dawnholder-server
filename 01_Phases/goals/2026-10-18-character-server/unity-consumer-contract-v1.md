# Unity 비화면 캐릭터 메뉴 소비 계약 v1

승인 goal의 C4/C7과 packet-contract-v1을 Unity에서 소비하는 계약이다. wire49..54/v22·Core 이름 정책을 바꾸지 않는다. 메인 `msg_165321b60e07`과 Content/Core 소유 회신이 확정한 파일만 World 자기 checkout에서 쓴다. 화면·폼·버튼·씬 정책은 Content의 별도 목표다.

현재 기반은 전송 정의 구현 checkpoint `dcf906a0bfbdce887dd9dd20a2f4df53036806ef`다. 서버 패킷 시험217GREEN은 Unity DLL·컴파일·서버 입장 검증을 대신하지 않는다. 아래 1단계는 독립적인 비화면 client/reader와 기존 Lifetime 최소 분기이고, 2단계의 실제 세션 등록/연결은 Content 공용2파일 인계 뒤 수행한다. 문서의 API는 단계별 선행 시험과 구현자 사이의 고정 접점이다.

## 소유와 배치

신규 제품은 `03_Client/Assets/Scripts/Network/CharacterMenu/`의 `CharacterMenuClient.cs`, `CharacterMenuReply.cs`, `CharacterMenuReplyReader.cs`와 `Network/Handlers/CharacterMenu/`의 목록/생성/선택 결과 handler3개·각meta/폴더meta다. 일반 C# 타입은 `Dawnholder.Client.Network.CharacterMenu` namespace에 둔다. 기존 `Network/ClientConnectionLifetime.cs`는 명시 메뉴 접속 최소 분기만 바꾼다. 선행 시험은 신규 `Assets/Tests/EditMode/CharacterMenuClientTests.cs`, `CharacterMenuPacketContractTests.cs`와meta, 기존 `ClientConnectionLifetimeTests.cs`의 좁은 보완이다. 기존meta/GUID는 보존한다.

`UnityClientSession.cs`·`NetworkService.cs`는 Content PR2 실제 쓰기 종료·기준/diff 인계 후 World가 갱신한 기준에서 최소 접점을 연결한다. 지금 쓰기 대상이 아니다. Core48의 CharacterLoadAuthority/CharacterLoadResultHandler는 수정하지 않는다. Content checkout/실행 snapshot에도 쓰지 않는다.

## 1단계: 불변 응답과 wire reader

`CharacterMenuReplyReader`는 public static 타입이며 `public static bool TryRead(ArraySegment<byte> frame, out CharacterMenuReply reply)`를 제공한다. 실패는 false와null을 반환하고 malformed frame에 정상 응답을 만들거나 큰 길이 할당/예외로 탈출하지 않는다. 새 공용 검사 framework를 만들지 않고 이 세 응답만 다룬다.

`CharacterMenuReply`는 public sealed 불변 타입이다. 공개 getter는 `ushort PacketId`, `uint RequestId`, `byte Result`, `byte InvalidField`, `byte OccupiedMask`, `IReadOnlyList<CharacterMenuCharacter> Slots`, `CharacterMenuCharacter Character`, `string SelectedCharacterIdD`다. 목록의 Slots는 정확3칸이며 빈칸은null, 다른 응답의 Slots는 빈 읽기전용 목록이다. 성공 생성만 Character를 갖고 다른 경우null이다. 성공 선택만 SelectedCharacterIdD를 갖고 나머지는empty다. 공개 setter·외부에서 바꿀 수 있는 슬롯 배열을 노출하지 않는다. 생성 방식은 reader가 소유하며 시험 때문에 공개 생성자를 강제하지 않는다.

같은 Reply 파일의 `CharacterMenuCharacter`도 public sealed 불변 타입이고 `byte Slot`(1..3), `string CharacterIdD`, `string Name`, `byte ClassId` getter를 갖는다. 이 class는 목록 preview/생성 결과이며 권위 ClassLoadout을 적용하지 않는다.

Reader는 packet-contract-v1의 고정50/52/54 필드 순서를 explicit LE로 읽는다. ArraySegment의 offset/count, header size/ID와 실제 정확 길이·최대314/112/84bytes·requestId nonzero, UTF-16의 짝수 byte 길이/필드 상한/정상 surrogate를 확인하고 초과·잘림·trailing data를 거부한다. 결과0..16과 invalidField0..4, InvalidInput 외 invalidField0을 지킨다. 실패50의 mask/3슬롯과 실패52/54의 모든 부가 필드는 계약의 비어 있는 값이어야 한다.

성공50은 mask0..7·고정3칸을 그대로 해석한다. occupied 슬롯만 유효 nonempty Guid D·이름2..12 UTF-16 units/최대24bytes·class0/1을 가지며 빈칸은 empty/empty/0이다. 같은 ID의 중복 슬롯은 거부한다. 성공52는 유효 slot/ID/이름/class, 성공54는 유효 선택ID다. 응답 ID는 canonical 소문자 D형이며 잘못된 ID를 정상값으로 보정하지 않는다. 출력 이름을 trim/정규화하거나 Core 정규화/유일성 계산을 복제하지 않는다. 정상 UTF-16·길이와 wire 구조를 검증한다. 실패 목록을 성공 빈 목록으로 변환하지 않는다.

## 1단계: 연결 소유 메뉴 client

`CharacterMenuClient`는 public sealed IDisposable 일반 C# 타입이다. 생성자는 `CharacterMenuClient(Func<bool> canSend, Action<ArraySegment<byte>> send, Action disconnect, Action<string> selectionAccepted)`이며 각 delegate는 null을 거부한다. 생성/호출/수신/Dispose는 main-thread 소유다. 실제 session 객체·세대의 current 판정은 canSend의 캡처된 소유자가 제공한다. 새 연결마다 새 client를 만들고 이전 client를 재사용하지 않는다.

공개 API는 `bool TryList()`, `bool TryCreate(byte slot, byte classId, string rawName)`, `bool TrySelect(string characterIdD)`, `bool TryReceive(CharacterMenuReply reply)`, `void Dispose()`다. 상태는 읽기전용 `bool HasPending`, `uint PendingRequestId`, `CharacterMenuReply LastReply`와 `event Action Changed`로 노출한다. 아직 응답이 없으면 LastReply는null이다. 성공/실패는 LastReply.Result로 구분하며 실패의 빈 슬롯을 동기화된 정상 목록으로 알리지 않는다.

- canSend=false·disposed·요청중·선택 성공 뒤에는 새 송신을 하지 않고 false다. 목록/생성/선택 전체에 pending 하나만 둔다. 추가 요청 거절은 기존 ID/종류/payload를 바꾸지 않는다.
- 연결마다 nonzero uint requestId를 단조 증가시킨다. 송신 요청은49/51/53의 정확 필드와 reserved0이며 생성 rawName은 원문 그대로다. slot1..3/class0..1, rawName 최대36units/72bytes·정상 UTF-16, 선택Guid D/nonempty의 로컬 구조를 검사한다. 이름의 NFC·2..12자 정책/중복 판정은 서버/Core 책임이다. 선택에 class를 보내지 않는다. uint wrap/0 재사용은 하지 않고 연결을 종료한다.
- 새 pending의 ID/종류/원문 payload는 송신 전에 캡처한다. send 예외는 해당 연결을 끝내고 false를 반환하며 성공 응답·자동 재시도·새ID 재생성을 만들지 않는다. 이 단계는 새 timeout/timer나 취소 패킷을 추가하지 않는다.
- TryReceive는 현재 canSend·미종료 상태와 pending 종류/ID가 모두 맞을 때만 소비한다. 이전/중복/다른ID/다른종류 응답은 false이고 기존 pending/LastReply/선택 callback을 바꾸지 않는다. 성공 생성의slot, 성공 선택의ID도 원래 요청과 맞아야 한다. 대소문자 다른 유효 입력ID는 같은 Guid로 상관시키고 응답의canonical ID를 전달한다.
- 일치한 응답만 pending을 완료하고 LastReply/Changed로 결과를 알린다. 정상 실패를 성공·빈 목록으로 바꾸지 않는다. OutcomeUnknown12는 원래 결과를 알린 뒤 해당 연결을 끝내고 재시도/새 요청을 금지한다. 이 client가 서버의 descriptor/deadline/rollback을 새로 만들지 않는다.
- 일치한54 Success만 selectionAccepted를 정확 한 번 호출한다. 그 뒤 메뉴 송신을 닫고 기존48/실제 입장 흐름을 기다린다. 실패·이전세대·중복54는 callback0이다. callback에는 server canonical 선택ID만 전달하고 preview class/로컬 class로 ClassLoadout·입장/gameplay ready를 만들지 않는다.
- Dispose/이전세대의 canSend=false 뒤에는 수신/송신/통지/선택 callback이 새 연결에 영향을 주지 않는다. Dispose는 네트워크 global 상태를 정리하는 대신 자기 pending/subscription만 소유한다. 세션 cleanup이 소유한 disconnect를 중복시키지 않는다.

## 1단계: 명시 메뉴 접속과 legacy 보존

기존 `ClientConnectionLifetime.Connect(IPEndPoint, byte, string)`는 Accepted 로그인 뒤 캡처된 class로 legacy `SendCharacterSelect`를 한 번 보내는 계약을 유지한다. 신규 `public bool ConnectCharacterMenu(IPEndPoint endPoint, string accountName)`와 `public event Action<IClientConnectionSession> CharacterMenuLoginAccepted`를 추가한다. 기존 IClientConnectionSession interface에 메뉴 메서드를 억지로 추가하지 않는다.

명시 메뉴 접속도 기존 Connect의 attempt·generation/current-session·handshake 뒤 login1회·중복connect·실패/취소/닫힘·rejection/cleanup 순서를 재사용한다. Accepted는 current generation/session의 login 이후 한 번만 이벤트로 통지하고 legacy 선택 송신은0이다. legacy 접속은 이 새 이벤트0이다. 중복Accepted·handshake 전Accepted·이전/닫힌/정리된session의 늦은Accepted는 새 메뉴/선택 문맥을 열지 않는다. 재접속 때 모드/accepted 플래그를 재설정해 메뉴→legacy와 legacy→메뉴가 서로 오염되지 않는다.

## 2단계: 실제 Unity session 연결 — 공용2파일 인계 뒤

Content 인계 뒤 UnityClientSession이 메뉴client를 연결 수명에 소유하고 helper의 canSend에 자신의 CanApply/Handshake/login·명시 메뉴 문맥을 캡처한다. NetworkService의 명시 메뉴 접속이 위 Lifetime 이벤트를 받아 현재 메뉴의 최초 목록을 한 번 요청한다. 세 handler는 strict reader 실패를 current session main-thread에서 종료하고 정상 reply는 EnqueueApply/current generation을 거쳐 TryReceive로 전달한다. registration/cleanup/접속 hook은 이 단계의 별도 선행 시험과 최소 diff로 수행한다.

Core가 의미 대조한 대로 current pending과 맞는54성공의 selectionAccepted callback만 기존 `_characterSelectSent` 문맥을 열며 legacy 패킷은 실제 송신하지 않는다.54→48와 main-thread 적용 순서를 보존하고 연결당 첫48 권위를 reset/reuse하지 않는다. ClassLoadout은48만 적용하고 실제 entry barrier 전 gameplay를 허용하지 않는다. 실제 OnRecvPacket→queue→helper→48/입장과 늦은generation/cleanup은2단계·최종 강 검증에서 확인한다.

## 시험과 실행 경계

1단계 선행 시험은 신규타입/API 부재를 reflection assertion RED로 관측하며 미구현 타입의 직접 참조 compile 오류·Ignore/skip으로 대체하지 않는다. byte 기대값은 계약의 독립literal/offset에서 만들고 제품 Write/Read를 기대값 생성기로 사용하지 않는다. 정상50/52/54·목록0/1/3·빈Knight구분·failures·잘린/홀수/과대/비정상UTF-16/header/unknown enum/invalidField·상관/중복/pending/close와 legacy 보존을 검사한다. 한계는 단계별로 명시하고 실제 handler/session 연결의 미구현을 순수 helper 통과로 숨기지 않는다.

Unity 시험 작성/실제 컴파일·EditMode·DLL 갱신은 구분한다. 실제 실행은 별도 READY/독점 자원 창과 승인된 snapshot·정확 argv·백업/원본 보존 뒤만 수행한다. 기존 meta/GUID/설정과 원본 DLL을 보존하며 실행하지 않은 항목을 통과로 쓰지 않는다. PR 전 신규 독립 검증은 전체 C1~C9/실제 진입/양쪽소비와 원시를 다시 판정한다.
