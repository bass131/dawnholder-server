# Unity 캐릭터 메뉴의 실제 세션 연결 계약 v1

[승인 goal](goal.md)의 C4/C7과 [Unity 소비 v1의 2단계](unity-consumer-contract-v1.md#2단계-실제-unity-session-연결--공용2파일-인계-뒤)를 구체화한다. 전송49..54/v22·결과 의미와 기존48의 권위·입장 barrier는 바꾸지 않는다. 화면·새 timeout·Core 저장 정책은 이 계약 밖이다.

현재 선행시험 기준은 World `70a908fcc6864a4de3546b41e8ae3ebbf577afa3`이다. Shared22는 원본 Plugins에 배포됐다. Content `msg_2917dc56748f`의 공용2파일 쓰기종료·정확hash/diff를 인계받았으며 후속 UI는 이 두 파일을 읽기만 한다. Content Session의 Guild 의존성은 World에 아직 없으므로 두 파일만 복사하지 않는다. 실제 구현·통합 때 Content PR2 결과/의존성과 최신 기준을 다시 고정하고 그 동작을 보존한다. 이 문서와 선행시험이 Content PR2나 Core의 통합 완료를 뜻하지 않는다.

## 세션과 facade의 접점

- `UnityClientSession`은 public getter `CharacterMenuClient CharacterMenu`를 제공한다. 명시 메뉴 문맥이 열리기 전에는 null이다. 메뉴 client의 공개 setter는 없다.
- 같은 세션의 internal `bool BeginCharacterMenu()`는 현재 연결·handshake 성공·그 뒤 수락된 로그인·미선택 상태에서 한 번만 메뉴 문맥과 새 client를 만든다. 성공한 최초 호출만 true, 이전세대·닫힘·중복·로그인 전·legacy 선택 뒤에는 false다. 이 메서드 자체는 목록이나 legacy 선택을 송신하지 않는다.
- 로그인 수락 기록은 current main-thread 적용 경계 안에서 handshake 뒤 Accepted를 받은 사실이다. handshake 전 Accepted를 나중에 재사용해 메뉴를 열지 않는다. 기존 legacy의 API·로그인/선택·실패 처리 계약은 보존한다.
- `NetworkService`는 public `void ConnectCharacterMenu(string hostOverride = null)`와 public getter `CharacterMenuClient CharacterMenu`를 제공한다. getter는 현재 수명의 Unity session이 소유한 메뉴를 보여주며 연결이 없으면 null이다. UI 정책이나 폼을 만들지 않는다.
- 명시 메뉴 접속은 기존 endpoint/port·계정 정규화와 Lifetime의 attempt/generation/cleanup을 재사용한다. 로컬 직업 선택은 요구하거나 송신하지 않는다. 기존 `Connect(...)`는 legacy 접속으로 유지한다. `IClientConnectionSession` interface는 바꾸지 않는다.
- facade는 기존 Lifetime의 `CharacterMenuLoginAccepted`를 구독하고 해당 수명의 현재 Unity session만 받는다. `BeginCharacterMenu()`가 true일 때만 `CharacterMenu.TryList()`를 한 번 호출한다. 중복/이전세대/종료 이벤트는 새 client·추가목록·legacy 송신을 만들지 않는다. facade 종료 때 구독을 해제한다.

이 접점은 비화면 통신 API다. 화면에서 client를 획득·표시하는 추가 UI 계약은 Content와 조율하며 여기서 임의 씬 전환이나 자동 legacy→menu 전환을 추가하지 않는다.

## 수신·상관·직업 권위

세 결과 handler는 `Network/Handlers/CharacterMenu/`에 목록·생성·ID선택의 책임별로 놓고 실제 Session 테이블의50/52/54에 등록한다. 정확 파일명은 다음 구현 계약에서 주변 명명과 대조한다. 기존 Core48 handler/authority 파일은 수정하지 않는다.

`OnRecvPacket`에서 받은 실제 frame을 기존 strict reader로 판독한다. 인식된 메뉴 응답의 구조가 잘못됐으면 current main-thread 큐에서 해당 session을 종료한다. 정상 응답도 `EnqueueApply`의 실행 시점 current gate를 거쳐 해당 session의 메뉴 client에 전달한다. helper를 직접 호출한 시험만으로 handler 등록·큐 연결을 증명하지 않는다. 이전세대·닫힘·cleanup 뒤의 정상/비정상 수신은 새 연결을 변경하거나 끊지 않는다.

pending 종류·requestId와 생성slot/선택ID가 일치한 응답만 기존 client가 소비한다. 잘못된 상관값·중복·미요청 응답은 pending/LastReply/권위를 바꾸지 않는다. 서버 실패는 실패 그대로, OutcomeUnknown은 원 결과를 보인 뒤 기존 client의 종료 경로를 따른다. handler가 새 성공·재시도·타임아웃을 만들지 않는다.

일치한54 Success callback만 같은 current 메뉴 session의 기존 `_characterSelectSent` 문맥을 연다. 이때 `C_CharacterSelect`를 송신하지 않는다. 메뉴 문맥 중 직접 legacy 선택 송신 요청도 새 legacy 패킷을 만들지 않는다. 목록/생성 preview class·로컬 선택class·54 자체로 `ClassLoadout`을 적용하지 않는다.

54 뒤 큐에 온 첫 유효48만 기존 `ApplyCharacterLoad`/`CharacterLoadAuthority`를 통해 서버 직업을 적용한다.54 전48·선택실패 뒤48·이전세대48은 권위를 열지 않는다. 첫48 뒤의 중복·상충 결과는 기존의 첫 결과 보존을 따른다.54나48만으로 gameplay ready가 되지 않으며 실제 entry barrier가 계속 필요하다. 기존 legacy 선택→48의 성공/실패·닫힘 동작도 보존한다.

`Cleanup`은 자신이 만든 메뉴 client를 Dispose하고 참조를 비운다. 외부에 남은 옛 client는 pending/Changed/송신을 정리하고 새 연결에 영향을 주지 않는다. session의 existing close/Inventory/Entry/authority 정리를 대체하거나 global disconnect를 중복시키지 않는다.

## 선행시험과 실행 판정

신규 선행 작성자는 기존 `CharacterMenuClientTests.cs` 안에 session/facade 사례와 필요한 좁은 harness를 추가한다. 기존48개 pure client 사례·literal byte helper·나머지 기존253 이름/단정·meta는 보존한다. 새 fixture 전역 SetUp에 부재 API 단정을 넣어 기존 사례를 함께 실패시키지 않는다. 미구현 접점은 reflection의 테스트별 assertion으로 RED를 내며 직접 참조 compile 오류·Ignore/skip으로 대체하지 않는다.

우선 요구는 실제 메뉴 활성/최초목록·50/52/54 dispatch→queue→상관·malformed/current·늦은generation/cleanup·실패/Unknown·54→48과 legacy 보존이다. 재사용 예시는 `ClientSessionGenerationTests.cs`의 queue/socket/current 도구와 `CharacterLoadAuthorityContractTests.cs`의 실제48/권위·entry 경계다. loopback socket은 시험 소유의 임시 endpoint이며 게임 서버·DB를 기동하지 않는다. 필요하면 같은 파일 안의 작은 test double을 사용하고 무엇을 대역으로 대체했는지 보고한다.

준비 단계는 시험 작성과 정확 Unity READY까지만 허용한다. 실제 RED는 메인 실행창과 보충 계약으로 입력/hash·fresh snapshot·정확 argv·단일백업·Prefs/프로세스 반환을 고정한 뒤 수행한다. 이 단계에는 Shared 재빌드·WSL·dotnet·서버/DB 실행이 필요하지 않다. 선택할 기존48 회귀와 기존253·신규 사례의 실제 이름/결과를 원 XML에서 구분한다. 제품 구현 전 요구 RED, 같은 명령의 구현 후 GREEN, PR 전 신규 세션의 독립 강 검증은 각각 다른 근거다.
